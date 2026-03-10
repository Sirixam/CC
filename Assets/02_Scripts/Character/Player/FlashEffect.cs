using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlashEffect : MonoBehaviour
{
    [SerializeField] private Color _flashColor = Color.red;
    [SerializeField] private float _flashDuration = 0.1f;
    [SerializeField] private int _flashCount = 3;

    // Only holds renderers whose materials have a supported color property
    private Renderer[] _renderers;
    private Color[] _originalColors;
    private string[] _colorPropertyNames;
    private Coroutine _flashCoroutine;

    private void Awake()
    {
        Renderer[] allRenderers = GetComponentsInChildren<Renderer>();

        //Filter out renderers whose materials don't support color flashing
        var validRenderers = new List<Renderer>();
        var validColors = new List<Color>();
        var validPropNames = new List<string>();

        foreach (var r in allRenderers)
        {
            string propName = GetColorPropertyName(r.material);
            if (propName == null) continue; // skip unsupported materials

            validRenderers.Add(r);
            validPropNames.Add(propName);
            validColors.Add(r.material.GetColor(propName));
        }

        _renderers = validRenderers.ToArray();
        _colorPropertyNames = validPropNames.ToArray();
        _originalColors = validColors.ToArray();
    }

    public void Flash()
    {
        if (_flashCoroutine != null)
            StopCoroutine(_flashCoroutine);

        _flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        for (int i = 0; i < _flashCount; i++)
        {
            SetAllColors(_flashColor);
            yield return new WaitForSeconds(_flashDuration);
            RestoreAllColors();
            yield return new WaitForSeconds(_flashDuration);
        }

        RestoreAllColors();
        _flashCoroutine = null;
    }

    private void SetAllColors(Color color)
    {
        for (int i = 0; i < _renderers.Length; i++)
            _renderers[i].material.SetColor(_colorPropertyNames[i], color);
    }

    private void RestoreAllColors()
    {
        for (int i = 0; i < _renderers.Length; i++)
            _renderers[i].material.SetColor(_colorPropertyNames[i], _originalColors[i]);
    }

    /// <summary>
    /// Returns the correct color property name for this material, or null if unsupported.
    /// Skips properties that exist but are declared as a non-Color type (e.g. Vector).
    /// </summary>
    private string GetColorPropertyName(Material mat)
    {
        foreach (string prop in new[] { "_BaseColor", "_Color", "_TintColor" })
        {
            if (!mat.HasProperty(prop)) continue;

#if UNITY_EDITOR
            // Verify the shader property is actually a Color type, not Vector/Float/etc.
            var shader = mat.shader;
            int idx = shader.FindPropertyIndex(prop);
            if (idx >= 0 && shader.GetPropertyType(idx) != UnityEngine.Rendering.ShaderPropertyType.Color)
                continue;
#endif
            return prop;
        }

        // No valid color property — this material will be silently skipped
        return null;
    }
}