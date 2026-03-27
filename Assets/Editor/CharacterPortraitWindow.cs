using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.IO;

public class CharacterPortraitWindow : EditorWindow
{
    private const string NpcsFolder = "Assets/03_Prefabs/NPCs";
    private const string IgnoredPrefab = "StudentNpc.prefab";
    private const float OffscreenY = -10000f;

    [SerializeField] private GameObject _characterPrefab;
    [SerializeField] private string _outputFolder = "Assets/PortraitOutput";
    [SerializeField] private int _resolution = 512;
    [SerializeField] private float _cameraDistance = 2f;
    [SerializeField] private float _cameraHeight = 1.1f;
    [SerializeField] private float _fieldOfView = 35f;
    [SerializeField] private float _characterRotationY = 0f;
    [SerializeField] private Vector3 _characterOffset = Vector3.zero;

    private Texture2D _preview;

    [MenuItem("Game Tools/Character Portrait Tool")]
    public static void OpenWindow()
    {
        var window = GetWindow<CharacterPortraitWindow>("Character Portrait");
        window.minSize = new Vector2(300, 420);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Character Portrait Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space(6);

        _characterPrefab = (GameObject)EditorGUILayout.ObjectField("Character Prefab", _characterPrefab, typeof(GameObject), false);
        _outputFolder = EditorGUILayout.TextField("Output Folder", _outputFolder);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Camera", EditorStyles.miniBoldLabel);
        _resolution = EditorGUILayout.IntField("Resolution", _resolution);
        _cameraDistance = EditorGUILayout.FloatField("Distance", _cameraDistance);
        _cameraHeight = EditorGUILayout.FloatField("Height", _cameraHeight);
        _fieldOfView = EditorGUILayout.FloatField("Field of View", _fieldOfView);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Character", EditorStyles.miniBoldLabel);
        _characterRotationY = EditorGUILayout.Slider("Rotation Y", _characterRotationY, -180f, 180f);
        _characterOffset = EditorGUILayout.Vector3Field("Position Offset", _characterOffset);

        EditorGUILayout.Space(10);

        EditorGUI.BeginDisabledGroup(_characterPrefab == null);
        if (GUILayout.Button("Render Single", GUILayout.Height(32)))
            RenderSingle(_characterPrefab);
        EditorGUI.EndDisabledGroup();

        if (GUILayout.Button("Render All", GUILayout.Height(32)))
            RenderAll();

        DrawPreview();
    }

    private void DrawPreview()
    {
        if (_preview == null)
            return;

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Preview", EditorStyles.miniBoldLabel);

        float size = Mathf.Min(position.width - 16, 256);
        Rect previewRect = GUILayoutUtility.GetRect(size, size);
        previewRect.width = size;
        previewRect.x = (position.width - size) * 0.5f;

        // Checkerboard to show transparency
        EditorGUI.DrawTextureTransparent(previewRect, _preview);
    }

    private void RenderSingle(GameObject prefab)
    {
        EnsureOutputFolder();
        string outputPath = $"{_outputFolder}/{prefab.name}.png";
        Texture2D tex = RenderPrefab(prefab);
        SaveTexture(tex, outputPath);
        SetPreview(tex);
        AssetDatabase.Refresh();
        Debug.Log($"[CharacterPortrait] Saved: {outputPath}");
    }

    private void RenderAll()
    {
        EnsureOutputFolder();
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { NpcsFolder });
        int count = 0;
        Texture2D lastTex = null;

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileName(assetPath) == IgnoredPrefab)
                continue;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
                continue;

            if (lastTex != null)
                DestroyImmediate(lastTex);

            string outputPath = $"{_outputFolder}/{prefab.name}.png";
            lastTex = RenderPrefab(prefab);
            SaveTexture(lastTex, outputPath);
            count++;
        }

        SetPreview(lastTex);
        AssetDatabase.Refresh();
        Debug.Log($"[CharacterPortrait] Rendered {count} portraits to '{_outputFolder}'.");
    }

    private Texture2D RenderPrefab(GameObject prefab)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.SetPositionAndRotation(new Vector3(0, OffscreenY, 0), Quaternion.Euler(0f, _characterRotationY, 0f));
        instance.hideFlags = HideFlags.HideAndDontSave;

        // Compute bounds before applying offset so the camera angle is unaffected by position changes
        Bounds bounds = GetRendererBounds(instance);
        Vector3 center = bounds.center;

        instance.transform.position += _characterOffset;

        // Camera — character faces +Z so we approach from -Z
        GameObject cameraGO = EditorUtility.CreateGameObjectWithHideFlags(
            "PortraitCamera", HideFlags.HideAndDontSave, typeof(Camera));
        Camera camera = cameraGO.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.clear;
        camera.fieldOfView = _fieldOfView;
        camera.nearClipPlane = 0.01f;
        camera.farClipPlane = 100f;
        camera.allowHDR = false;
        camera.allowMSAA = false;
        camera.enabled = false;

        // Disable post-processing so URP doesn't strip alpha
        var urpData = camera.GetUniversalAdditionalCameraData();
        urpData.renderPostProcessing = false;
        urpData.antialiasing = AntialiasingMode.None;

        Vector3 lookTarget = new Vector3(center.x, center.y - bounds.extents.y * 0.1f, center.z);
        Vector3 camPos = new Vector3(center.x, OffscreenY + _cameraHeight, center.z - _cameraDistance);
        camera.transform.SetPositionAndRotation(camPos, Quaternion.identity);
        camera.transform.LookAt(lookTarget);

        // Directional light
        GameObject lightGO = EditorUtility.CreateGameObjectWithHideFlags(
            "PortraitLight", HideFlags.HideAndDontSave, typeof(Light));
        Light light = lightGO.GetComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // Render
        RenderTexture rt = new RenderTexture(_resolution, _resolution, 32, RenderTextureFormat.ARGB32)
        {
            antiAliasing = 1
        };

        camera.targetTexture = rt;
        camera.Render();

        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(_resolution, _resolution, TextureFormat.ARGB32, false);
        tex.ReadPixels(new Rect(0, 0, _resolution, _resolution), 0, 0);
        tex.Apply();
        RenderTexture.active = null;

        DestroyImmediate(instance);
        DestroyImmediate(cameraGO);
        DestroyImmediate(lightGO);
        DestroyImmediate(rt);

        return tex;
    }

    private void SaveTexture(Texture2D tex, string outputPath)
    {
        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(outputPath, bytes);
    }

    private void SetPreview(Texture2D tex)
    {
        if (_preview != null)
            DestroyImmediate(_preview);
        _preview = tex;
        Repaint();
    }

    private void EnsureOutputFolder()
    {
        if (!Directory.Exists(_outputFolder))
            Directory.CreateDirectory(_outputFolder);
    }

    private static Bounds GetRendererBounds(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return new Bounds(go.transform.position, Vector3.one);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private void OnDestroy()
    {
        if (_preview != null)
            DestroyImmediate(_preview);
    }
}
