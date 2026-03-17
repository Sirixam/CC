using System;
using System.Collections.Generic;
using UnityEngine;

public class ColorComponent : MonoBehaviour
{
    [Serializable]
    public class RendererData
    {
        public MeshRenderer Renderer;
        public string ColorPropertyName = DEFAULT_COLOR_PROPERTY_NAME;
        public string tag = DEFAULT_TAG;
        public bool Ignore;
        

        public void SetColor(Color color, string tag)
        {
            MaterialPropertyBlock block = new();
            Renderer.GetPropertyBlock(block);
            block.SetColor(ColorPropertyName, color);
            Renderer.SetPropertyBlock(block);
        }
    }

    private const string DEFAULT_COLOR_PROPERTY_NAME = "_Color";
    private const string DEFAULT_TAG = "_Tag";

    [SerializeField] private List<RendererData> _renderersData = new();
    [Header("EDITOR_ONLY")]
    [SerializeField] private Transform EDITOR_RenderersParent;
    [SerializeField] private bool EDITOR_IncludeInactive = true;

    public void SetColor(Color color, string tag)
    {
        foreach (var rendererData in _renderersData)
        {
            if (rendererData.Ignore) continue;
            if (rendererData.tag != tag) continue;
            MaterialPropertyBlock block = new();
            rendererData.Renderer.GetPropertyBlock(block);
            block.SetTexture("_BaseMap", Texture2D.whiteTexture);
            block.SetColor(rendererData.ColorPropertyName, color);
            rendererData.Renderer.SetPropertyBlock(block);
        }
    }

    [Button("Add Missing Renderers Data")]
    private void EDITOR_AddMissingRenderersData()
    {
        Transform parent = EDITOR_RenderersParent ?? transform;
        MeshRenderer[] meshRenderers = transform.GetComponentsInChildren<MeshRenderer>(EDITOR_IncludeInactive);
        foreach (var meshRenderer in meshRenderers)
        {
            if (_renderersData.Exists(x => x.Renderer == meshRenderer)) continue;

            _renderersData.Add(new RendererData() { Renderer = meshRenderer });
        }
    }
    

}
