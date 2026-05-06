using UnityEngine;

[ExecuteAlways]
public class TextureDataMaterialBridge : MonoBehaviour
{
    private const int MaxSupportedLayers = 8;

    [SerializeField] private TextureData textureData;
    [SerializeField] private Material targetMaterial;

    [Header("Optional automatic height sync")]
    [SerializeField] private bool syncHeightFromRendererBounds = false;
    [SerializeField] private Renderer targetRenderer;
    
    [Header("Force manual height change")]
    [SerializeField] private bool useManualHeightRange = true;
    [SerializeField] private float manualMinHeight = 0f;
    [SerializeField] private float manualMaxHeight = 40f;

    [Header("Apply automatically")]
    [SerializeField] private bool applyOnEnable = true;
    [SerializeField] private bool watchForChanges = true;

    private int _lastFingerprint;

    private void OnEnable()
    {
        if (applyOnEnable)
        {
            Apply();
        }
    }

    private void Update()
    {
        if (!watchForChanges)
            return;

        int currentFingerprint = ComputeFingerprint();
        if (currentFingerprint != _lastFingerprint)
        {
            Apply();
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        Apply();
    }
#endif

    [ContextMenu("Apply Texture Data To Material")]
    public void Apply()
    {
        if (textureData == null || targetMaterial == null)
            return;

        int layerCount = textureData.layers != null ? textureData.layers.Length : 0;
        int appliedLayerCount = Mathf.Min(layerCount, MaxSupportedLayers);

        for (int i = 0; i < MaxSupportedLayers; i++)
        {
            ApplyLayer(i, i < appliedLayerCount);
        }

        targetMaterial.SetInt("_LayerCount", appliedLayerCount);

        if (syncHeightFromRendererBounds && targetRenderer != null)
        {
            Bounds b = targetRenderer.bounds;
            targetMaterial.SetFloat("_MinHeight", b.min.y);
            targetMaterial.SetFloat("_MaxHeight", b.max.y);
        }
        else if (useManualHeightRange)
        {
            targetMaterial.SetFloat("_MinHeight", manualMinHeight);
            targetMaterial.SetFloat("_MaxHeight", manualMaxHeight);
        }

        _lastFingerprint = ComputeFingerprint();
    }

    private void ApplyLayer(int layerIndex, bool useRealLayer)
    {
        string texProp = "_LayerTex" + layerIndex;
        string colorProp = "_Color" + layerIndex;
        string colorStrengthProp = "_ColorStrength" + layerIndex;
        string startHeightProp = "_StartHeight" + layerIndex;
        string blendProp = "_Blend" + layerIndex;
        string scaleProp = "_TextureScale" + layerIndex;

        if (useRealLayer && textureData.layers != null && layerIndex < textureData.layers.Length)
        {
            var layer = textureData.layers[layerIndex];

            targetMaterial.SetTexture(texProp, layer.texture != null ? layer.texture : Texture2D.whiteTexture);
            targetMaterial.SetColor(colorProp, layer.tint);
            targetMaterial.SetFloat(colorStrengthProp, Mathf.Clamp01(layer.tintStrength));
            targetMaterial.SetFloat(startHeightProp, Mathf.Clamp01(layer.startHeight));
            targetMaterial.SetFloat(blendProp, Mathf.Max(0.001f, layer.blendStrength));
            targetMaterial.SetFloat(scaleProp, Mathf.Max(0.001f, layer.textureScale));
        }
        else
        {
            targetMaterial.SetTexture(texProp, Texture2D.whiteTexture);
            targetMaterial.SetColor(colorProp, Color.black);
            targetMaterial.SetFloat(colorStrengthProp, 0f);
            targetMaterial.SetFloat(startHeightProp, 2f);
            targetMaterial.SetFloat(blendProp, 0.001f);
            targetMaterial.SetFloat(scaleProp, 1f);
        }
    }

    private int ComputeFingerprint()
    {
        unchecked
        {
            int hash = 17;

            hash = hash * 23 + (textureData != null ? textureData.GetInstanceID() : 0);
            hash = hash * 23 + (targetMaterial != null ? targetMaterial.GetInstanceID() : 0);

            if (textureData != null && textureData.layers != null)
            {
                hash = hash * 23 + textureData.layers.Length;

                for (int i = 0; i < textureData.layers.Length; i++)
                {
                    var layer = textureData.layers[i];
                    if (layer == null)
                    {
                        hash = hash * 23;
                        continue;
                    }

                    hash = hash * 23 + (layer.texture != null ? layer.texture.GetInstanceID() : 0);
                    hash = hash * 23 + layer.tint.GetHashCode();
                    hash = hash * 23 + layer.tintStrength.GetHashCode();
                    hash = hash * 23 + layer.startHeight.GetHashCode();
                    hash = hash * 23 + layer.blendStrength.GetHashCode();
                    hash = hash * 23 + layer.textureScale.GetHashCode();
                }
            }

            if (syncHeightFromRendererBounds && targetRenderer != null)
            {
                Bounds b = targetRenderer.bounds;
                hash = hash * 23 + b.min.y.GetHashCode();
                hash = hash * 23 + b.max.y.GetHashCode();
            }

            return hash;
        }
    }
}