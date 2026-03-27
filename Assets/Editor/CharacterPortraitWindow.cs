using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.IO;

public class CharacterPortraitWindow : EditorWindow
{
    private const string IgnoredPrefab = "StudentNpc.prefab";
    private const float OffscreenY = -10000f;
    private const string ConfigPrefKey = "CharacterPortraitWindow.ConfigPath";

    [SerializeField] private GameObject _characterPrefab;
    [SerializeField] private CharacterPortraitConfig _config;

    private Texture2D _preview;

    [MenuItem("Game Tools/Character Portrait Tool")]
    public static void OpenWindow()
    {
        var window = GetWindow<CharacterPortraitWindow>("Character Portrait");
        window.minSize = new Vector2(300, 460);
    }

    private void OnEnable()
    {
        string savedPath = EditorPrefs.GetString(ConfigPrefKey, string.Empty);
        if (_config == null && !string.IsNullOrEmpty(savedPath))
            _config = AssetDatabase.LoadAssetAtPath<CharacterPortraitConfig>(savedPath);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Character Portrait Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space(6);

        EditorGUI.BeginChangeCheck();
        _config = (CharacterPortraitConfig)EditorGUILayout.ObjectField("Config", _config, typeof(CharacterPortraitConfig), false);
        if (EditorGUI.EndChangeCheck())
            EditorPrefs.SetString(ConfigPrefKey, _config != null ? AssetDatabase.GetAssetPath(_config) : string.Empty);

        if (_config == null)
        {
            EditorGUILayout.HelpBox("Assign or create a config asset to get started.", MessageType.Info);
            if (GUILayout.Button("Create Config"))
                CreateConfig();
            return;
        }

        EditorGUILayout.Space(4);

        SerializedObject so = new SerializedObject(_config);
        so.Update();

        _characterPrefab = (GameObject)EditorGUILayout.ObjectField("Character Prefab", _characterPrefab, typeof(GameObject), false);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Folders", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(so.FindProperty(nameof(CharacterPortraitConfig.SourceFolder)));
        EditorGUILayout.PropertyField(so.FindProperty(nameof(CharacterPortraitConfig.IncludeNestedFolders)));
        EditorGUILayout.PropertyField(so.FindProperty(nameof(CharacterPortraitConfig.OutputFolder)));

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Camera", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(so.FindProperty(nameof(CharacterPortraitConfig.ResolutionWidth)));
        EditorGUILayout.PropertyField(so.FindProperty(nameof(CharacterPortraitConfig.ResolutionHeight)));
        EditorGUILayout.PropertyField(so.FindProperty(nameof(CharacterPortraitConfig.CameraDistance)));
        EditorGUILayout.PropertyField(so.FindProperty(nameof(CharacterPortraitConfig.CameraHeight)));
        EditorGUILayout.PropertyField(so.FindProperty(nameof(CharacterPortraitConfig.FieldOfView)));

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Character", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(so.FindProperty(nameof(CharacterPortraitConfig.CharacterRotationY)));
        EditorGUILayout.PropertyField(so.FindProperty(nameof(CharacterPortraitConfig.CharacterOffset)));
        EditorGUILayout.PropertyField(so.FindProperty(nameof(CharacterPortraitConfig.CharacterOffsetFemale)));

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Light", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(so.FindProperty(nameof(CharacterPortraitConfig.LightIntensity)));
        EditorGUILayout.PropertyField(so.FindProperty(nameof(CharacterPortraitConfig.LightRotation)));

        if (so.ApplyModifiedProperties())
            EditorUtility.SetDirty(_config);

        EditorGUILayout.Space(10);

        EditorGUI.BeginDisabledGroup(_characterPrefab == null);
        if (GUILayout.Button("Render Single", GUILayout.Height(32)))
            RenderSingle(_characterPrefab);
        EditorGUI.EndDisabledGroup();

        if (GUILayout.Button("Render All", GUILayout.Height(32)))
            RenderAll();

        DrawPreview();
    }

    private void CreateConfig()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Save Character Portrait Config",
            "CharacterPortraitConfig",
            "asset",
            "Choose where to save the config asset");

        if (string.IsNullOrEmpty(path))
            return;

        var config = CreateInstance<CharacterPortraitConfig>();
        AssetDatabase.CreateAsset(config, path);
        AssetDatabase.SaveAssets();
        _config = config;
        EditorPrefs.SetString(ConfigPrefKey, path);
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

        EditorGUI.DrawTextureTransparent(previewRect, _preview);
    }

    private void RenderSingle(GameObject prefab)
    {
        EnsureOutputFolder();
        string outputPath = $"{_config.OutputFolder}/{prefab.name}.png";
        Texture2D tex = RenderPrefab(prefab);
        SaveTexture(tex, outputPath);
        SetPreview(tex);
        Debug.Log($"[CharacterPortrait] Saved: {outputPath}");
    }

    private void RenderAll()
    {
        EnsureOutputFolder();
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { _config.SourceFolder });
        int count = 0;
        Texture2D lastTex = null;

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileName(assetPath) == IgnoredPrefab)
                continue;
            if (!_config.IncludeNestedFolders && Path.GetDirectoryName(assetPath).Replace('\\', '/') != _config.SourceFolder.TrimEnd('/'))
                continue;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
                continue;

            if (lastTex != null)
                DestroyImmediate(lastTex);

            string outputPath = $"{_config.OutputFolder}/{prefab.name}.png";
            lastTex = RenderPrefab(prefab);
            SaveTexture(lastTex, outputPath);
            count++;
        }

        SetPreview(lastTex);
        Debug.Log($"[CharacterPortrait] Rendered {count} portraits to '{_config.OutputFolder}'.");
    }

    private Texture2D RenderPrefab(GameObject prefab)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.SetPositionAndRotation(new Vector3(0, OffscreenY, 0), Quaternion.Euler(0f, _config.CharacterRotationY, 0f));
        instance.hideFlags = HideFlags.HideAndDontSave;

        // Deactivate all children except the capsule and its ancestors
        GameObject capsule = FindCapsuleChild(instance);
        foreach (Transform child in instance.transform)
        {
            bool keepActive = capsule != null &&
                (child.gameObject == capsule || capsule.transform.IsChildOf(child));
            if (!keepActive)
            {
                foreach (var r in child.GetComponentsInChildren<Renderer>())
                    r.enabled = false;
                child.gameObject.SetActive(false);
            }
        }

        // Deactivate siblings of the capsule within its parent
        if (capsule != null)
        {
            foreach (Transform sibling in capsule.transform.parent)
            {
                if (sibling.gameObject != capsule)
                {
                    foreach (var r in sibling.GetComponentsInChildren<Renderer>())
                        r.enabled = false;
                    sibling.gameObject.SetActive(false);
                }
            }
        }

        // Compute bounds before applying offset so the camera angle is unaffected by position changes
        Bounds bounds = capsule != null ? GetRendererBounds(capsule) : GetRendererBounds(instance);
        Vector3 center = bounds.center;

        bool isFemale = prefab.name.Contains("Female");
        instance.transform.position += isFemale ? _config.CharacterOffsetFemale : _config.CharacterOffset;

        // Camera — character faces +Z so we approach from -Z
        GameObject cameraGO = EditorUtility.CreateGameObjectWithHideFlags(
            "PortraitCamera", HideFlags.HideAndDontSave, typeof(Camera));
        Camera camera = cameraGO.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.clear;
        camera.fieldOfView = _config.FieldOfView;
        camera.nearClipPlane = 0.01f;
        camera.farClipPlane = 100f;
        camera.allowHDR = false;
        camera.allowMSAA = false;
        camera.enabled = false;

        var urpData = camera.GetUniversalAdditionalCameraData();
        urpData.renderPostProcessing = false;
        urpData.antialiasing = AntialiasingMode.None;

        Vector3 lookTarget = new Vector3(center.x, center.y - bounds.extents.y * 0.1f, center.z);
        Vector3 camPos = new Vector3(center.x, OffscreenY + _config.CameraHeight, center.z - _config.CameraDistance);
        camera.transform.SetPositionAndRotation(camPos, Quaternion.identity);
        camera.transform.LookAt(lookTarget);

        // Directional light
        GameObject lightGO = EditorUtility.CreateGameObjectWithHideFlags(
            "PortraitLight", HideFlags.HideAndDontSave, typeof(Light));
        Light light = lightGO.GetComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = _config.LightIntensity;
        lightGO.transform.rotation = Quaternion.Euler(_config.LightRotation);

        RenderTexture rt = new RenderTexture(_config.ResolutionWidth, _config.ResolutionHeight, 32, RenderTextureFormat.ARGB32)
        {
            antiAliasing = 1
        };

        camera.targetTexture = rt;
        camera.Render();

        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(_config.ResolutionWidth, _config.ResolutionHeight, TextureFormat.ARGB32, false);
        tex.ReadPixels(new Rect(0, 0, _config.ResolutionWidth, _config.ResolutionHeight), 0, 0);
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

        AssetDatabase.ImportAsset(outputPath);
        var importer = AssetImporter.GetAtPath(outputPath) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.SaveAndReimport();
        }
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
        if (!Directory.Exists(_config.OutputFolder))
            Directory.CreateDirectory(_config.OutputFolder);
    }

    private static GameObject FindCapsuleChild(GameObject instance)
    {
        Transform rendererParent = instance.transform.Find("Renderer");
        if (rendererParent == null)
            return null;

        foreach (Transform child in rendererParent)
        {
            if (child.name.StartsWith("capsule_"))
                return child.gameObject;
        }

        return null;
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
