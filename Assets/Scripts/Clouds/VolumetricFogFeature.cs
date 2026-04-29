using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// VolumetricFogFeature
///
/// HOW TO ADD TO YOUR PROJECT
/// ──────────────────────────
/// 1. Select your URP Renderer asset
///    (Project window → Assets/Settings/UniversalRenderer or similar).
/// 2. In the Inspector click "Add Renderer Feature" → VolumetricFogFeature.
/// 3. Assign the VolumetricFog shader to the Shader field that appears.
/// 4. Make sure "Depth Texture" is ticked on the same Renderer asset.
/// 5. Tweak all fog parameters directly in the Renderer Feature Inspector.
///
/// NO MonoBehaviour needs to be attached to any camera.
/// </summary>
[Serializable]
public class VolumetricFogFeature : ScriptableRendererFeature
{
    // ─────────────────────────────────────────────────────────────────────────
    // Public settings (shown in the Renderer Feature Inspector)
    // ─────────────────────────────────────────────────────────────────────────
    [Serializable]
    public class Settings
    {
        [Header("Shader")]
        [Tooltip("Assign the VolumetricFog shader here.")]
        public Shader shader;

        [Header("Visibility")]
        public bool renderInSceneView = true;

        [Header("Fog Appearance")]
        public Color fogColor = new Color(0.8f, 0.85f, 0.9f, 1f);
        [Range(0f,  1f)]  public float fogDensity      = 0.05f;
        public float fogStart   = 5f;
        public float fogEnd     = 50f;

        [Header("Ray Marching Quality")]
        [Range(8, 128)]   public int   stepCount       = 64;
        public float stepSize   = 0.5f;

        [Header("Lighting")]
        [Range(0f, 10f)]  public float lightAbsorption  = 1f;
        [Range(0f,  1f)]  public float darknessThreshold = 0.1f;
        [Range(0f,  2f)]  public float scatterCoeff      = 0.5f;
        /// <summary>Henyey-Greenstein g: +1 = strong forward scatter (god-rays), -1 = backscatter.</summary>
        [Range(-1f, 1f)]  public float phaseAnisotropy  = 0.2f;

        [Header("Noise / Shape")]
        public float noiseScale    = 0.1f;
        public float noiseSpeed    = 0.1f;
        [Range(0f,  1f)]  public float noiseStrength   = 0.3f;
        public float heightFalloff = 0.1f;
        public float baseHeight    = 0f;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Fields
    // ─────────────────────────────────────────────────────────────────────────
    public Settings settings = new Settings();

    Material          _material;
    VolumetricFogPass _pass;

    // ─────────────────────────────────────────────────────────────────────────
    // ScriptableRendererFeature API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Called once when the feature is created or its settings change.</summary>
    public override void Create()
    {
        // Clean up any previous material
        CoreUtils.Destroy(_material);

        if (settings.shader == null)
        {
            // Try to find it by name as a fallback
            settings.shader = Shader.Find("Custom/VolumetricFog");
        }

        if (settings.shader == null)
        {
            Debug.LogWarning("[VolumetricFogFeature] Shader not assigned and " +
                             "'Custom/VolumetricFog' not found in the project.");
            return;
        }

        _material = CoreUtils.CreateEngineMaterial(settings.shader);
        _pass     = new VolumetricFogPass(_material, settings);
    }

    /// <summary>Called every frame for each camera. Queue the pass here.</summary>
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_material == null || _pass == null) return;

        // Don't render for overlay cameras
        if (renderingData.cameraData.renderType == CameraRenderType.Overlay) return;

        _pass.UpdateSettings(settings);
        renderer.EnqueuePass(_pass);
    }

    /// <summary>Release GPU resources when the feature is destroyed.</summary>
    protected override void Dispose(bool disposing)
    {
        _pass?.Dispose();
        CoreUtils.Destroy(_material);
    }
}
