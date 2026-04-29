using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;

/// <summary>
/// VolumetricFogPass — URP 17+ / Unity 6
/// Blits the fog shader from the camera colour texture into a temp,
/// then blits the temp back using a second material pass so RenderGraph
/// never sees a texture as both input and output in the same pass.
/// </summary>
public class VolumetricFogPass : ScriptableRenderPass
{
    // ── Property IDs ─────────────────────────────────────────────────────────
    static readonly int ID_FogColor          = Shader.PropertyToID("_FogColor");
    static readonly int ID_FogDensity        = Shader.PropertyToID("_FogDensity");
    static readonly int ID_FogStart          = Shader.PropertyToID("_FogStart");
    static readonly int ID_FogEnd            = Shader.PropertyToID("_FogEnd");
    static readonly int ID_StepCount         = Shader.PropertyToID("_StepCount");
    static readonly int ID_StepSize          = Shader.PropertyToID("_StepSize");
    static readonly int ID_LightAbsorption   = Shader.PropertyToID("_LightAbsorption");
    static readonly int ID_DarknessThreshold = Shader.PropertyToID("_DarknessThreshold");
    static readonly int ID_ScatterCoeff      = Shader.PropertyToID("_ScatterCoeff");
    static readonly int ID_NoiseScale        = Shader.PropertyToID("_NoiseScale");
    static readonly int ID_NoiseSpeed        = Shader.PropertyToID("_NoiseSpeed");
    static readonly int ID_NoiseStrength     = Shader.PropertyToID("_NoiseStrength");
    static readonly int ID_HeightFalloff     = Shader.PropertyToID("_HeightFalloff");
    static readonly int ID_BaseHeight        = Shader.PropertyToID("_BaseHeight");
    static readonly int ID_PhaseG            = Shader.PropertyToID("_PhaseG");

    Material                      _material;
    Material                      _copyMaterial;   // plain blit-back, no fog logic
    VolumetricFogFeature.Settings _settings;

    public VolumetricFogPass(Material material, VolumetricFogFeature.Settings settings)
    {
        _material = material;
        _settings = settings;

        // Plain copy-back material using URP's built-in blit shader
        _copyMaterial = CoreUtils.CreateEngineMaterial(
            Shader.Find("Hidden/Universal Render Pipeline/Blit"));

        renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
        ConfigureInput(ScriptableRenderPassInput.Depth);
    }

    public void UpdateSettings(VolumetricFogFeature.Settings s) => _settings = s;

    // ═════════════════════════════════════════════════════════════════════════
    // RenderGraph path  (Unity 6 / URP 17+)
    // ═════════════════════════════════════════════════════════════════════════
    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        if (_material == null) return;

        UploadProperties();

        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
        if (resourceData.isActiveTargetBackBuffer) return;

        TextureHandle cameraColor = resourceData.activeColorTexture;

        // --- Pass 1: camera → temp  (fog shader) ----------------------------
        // Temp has identical format to the camera colour target.
        var tempDesc         = renderGraph.GetTextureDesc(cameraColor);
        tempDesc.name        = "_VolumetricFogTemp";
        tempDesc.clearBuffer = false;
        TextureHandle temp   = renderGraph.CreateTexture(tempDesc);

        var fogParams = new RenderGraphUtils.BlitMaterialParameters(
            cameraColor, temp, _material, 0);
        renderGraph.AddBlitPass(fogParams, passName: "VolumetricFog_Apply");

        // --- Pass 2: temp → camera  (plain copy) ----------------------------
        // Using a separate material means the two passes have distinct
        // read/write sets, so RenderGraph can schedule them correctly.
        var copyParams = new RenderGraphUtils.BlitMaterialParameters(
            temp, cameraColor, _copyMaterial, 0);
        renderGraph.AddBlitPass(copyParams, passName: "VolumetricFog_CopyBack");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Legacy / Compatibility Mode path  (URP 14-16 or RenderGraph disabled)
    // ═════════════════════════════════════════════════════════════════════════
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (_material == null) return;

        UploadProperties();

        var desc = renderingData.cameraData.cameraTargetDescriptor;
        desc.depthBufferBits = 0;

        RTHandle temp = RTHandles.Alloc(desc.width, desc.height, name: "_VolumetricFogTemp");
        RTHandle src  = renderingData.cameraData.renderer.cameraColorTargetHandle;

        CommandBuffer cmd = CommandBufferPool.Get("VolumetricFog");
        Blitter.BlitCameraTexture(cmd, src, temp, _material, 0);
        Blitter.BlitCameraTexture(cmd, temp, src);
        context.ExecuteCommandBuffer(cmd);

        CommandBufferPool.Release(cmd);
        temp.Release();
    }

    // ── Push all settings to the material ────────────────────────────────────
    void UploadProperties()
    {
        _material.SetColor(ID_FogColor,          _settings.fogColor);
        _material.SetFloat(ID_FogDensity,        _settings.fogDensity);
        _material.SetFloat(ID_FogStart,          _settings.fogStart);
        _material.SetFloat(ID_FogEnd,            _settings.fogEnd);
        _material.SetInt  (ID_StepCount,         _settings.stepCount);
        _material.SetFloat(ID_StepSize,          _settings.stepSize);
        _material.SetFloat(ID_LightAbsorption,   _settings.lightAbsorption);
        _material.SetFloat(ID_DarknessThreshold, _settings.darknessThreshold);
        _material.SetFloat(ID_ScatterCoeff,      _settings.scatterCoeff);
        _material.SetFloat(ID_NoiseScale,        _settings.noiseScale);
        _material.SetFloat(ID_NoiseSpeed,        _settings.noiseSpeed);
        _material.SetFloat(ID_NoiseStrength,     _settings.noiseStrength);
        _material.SetFloat(ID_HeightFalloff,     _settings.heightFalloff);
        _material.SetFloat(ID_BaseHeight,        _settings.baseHeight);
        _material.SetFloat(ID_PhaseG,            _settings.phaseAnisotropy);
    }

    public void Dispose()
    {
        CoreUtils.Destroy(_copyMaterial);
    }
}