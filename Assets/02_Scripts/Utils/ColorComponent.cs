using System;
using System.Collections.Generic;
using UnityEngine;

public class ColorComponent : MonoBehaviour
{
    [Serializable]
    public class RendererData
    {
        public MeshRenderer Renderer;
        public string ColorPropertyName = DEFUALT_COLOR_PROPERTY_NAME;
        public bool Ignore;

        public void SetColor(Color color)
        {
            MaterialPropertyBlock block = new();
            Renderer.GetPropertyBlock(block);
            block.SetColor(ColorPropertyName, color);
            Renderer.SetPropertyBlock(block);
        }
    }

    private const string DEFUALT_COLOR_PROPERTY_NAME = "_Color";

    [SerializeField] private List<RendererData> _renderersData = new();
    [Header("EDITOR_ONLY")]
    [SerializeField] private Transform EDITOR_RenderesParent;
    [SerializeField] private bool EDITOR_IncludeInactive = true;

    public void SetColor(Color color)
    {
        foreach (var rendererData in _renderersData)
        {
            if (rendererData.Ignore) continue;
            rendererData.SetColor(color);
        }
    }

    [Button("Add Missing Renderers Data")]
    private void EDITOR_AddMissingRenderersData()
    {
        Transform parent = EDITOR_RenderesParent ?? transform;
        MeshRenderer[] meshRenderers = transform.GetComponentsInChildren<MeshRenderer>(EDITOR_IncludeInactive);
        foreach (var meshRenderer in meshRenderers)
        {
            if (_renderersData.Exists(x => x.Renderer == meshRenderer)) continue;

            _renderersData.Add(new RendererData() { Renderer = meshRenderer });
        }
    }
}
