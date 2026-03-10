using System.Collections;
using UnityEngine;

public class FlashEffect : MonoBehaviour
{
    [SerializeField] private Color _flashColor = Color.red;
    [SerializeField] private float _flashDuration = 0.1f;
    [SerializeField] private int _flashCount = 3;

    private Renderer[] _renderers; // ✅ base class works for both Skinned and Mesh
    private Color[] _originalColors;
    private Coroutine _flashCoroutine;

    private void Awake()
    {
        // ✅ auto-grabs ALL renderer types in children
        _renderers = GetComponentsInChildren<Renderer>();

        _originalColors = new Color[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
        {
            _originalColors[i] = _renderers[i].material.GetColor("_BaseColor");
        }
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
        foreach (var r in _renderers)
            r.material.SetColor("_BaseColor", color);
    }

    private void RestoreAllColors()
    {
        for (int i = 0; i < _renderers.Length; i++)
            _renderers[i].material.SetColor("_BaseColor", _originalColors[i]);
    }
}