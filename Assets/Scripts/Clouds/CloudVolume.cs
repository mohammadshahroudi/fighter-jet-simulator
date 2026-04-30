using UnityEngine;

/// <summary>
/// Attach to a GameObject with a MeshRenderer (e.g. a Cube scaled to your cloud region).
/// Assign the Clouds_Stylized_URP material and your noise textures in the Inspector.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(MeshRenderer))]
public class CloudVolume : MonoBehaviour
{
    [Header("Noise Textures")]
    public Texture3D shapeNoiseTex;
    public Texture3D detailNoiseTex;
    public Texture2D blueNoiseTex;

    [Header("Cloud Shape")]
    [Range(0f, 4f)]    public float densityMultiplier = 1f;
    [Range(-1f, 1f)]   public float densityOffset     = -0.3f;
    [Range(0.1f, 10f)] public float scale             = 1f;
    [Range(0.1f, 10f)] public float detailScale       = 3f;
    [Range(0f, 1f)]    public float detailWeight      = 0.3f;
    public Vector4 shapeNoiseWeights = new Vector4(1f, 0.5f, 0.25f, 0.125f);

    [Header("Cloud Colour")]
    public Color cloudColorLight = Color.white;
    public Color cloudColorDark  = new Color(0.4f, 0.5f, 0.6f);
    [Range(0f, 1f)] public float darknessThreshold = 0.15f;
    [Range(1f, 8f)] public float posterizeSteps    = 3f;

    [Header("Lighting")]
    [Range(0f, 5f)]    public float lightAbsorptionSun   = 1f;
    [Range(0f, 5f)]    public float lightAbsorptionCloud = 1f;
    [Range(0f, 0.99f)] public float phaseG               = 0.3f;
    [Range(1, 16)]     public int   numStepsLight         = 6;

    [Header("Ray March")]
    [Range(4, 128)] public int   numStepsCloud     = 48;
    [Range(0f, 2f)] public float rayOffsetStrength = 1f;

    [Header("Animation")]
    [Range(0f, 2f)] public float timeScale   = 0.5f;
    [Range(0f, 2f)] public float baseSpeed   = 0.5f;
    [Range(0f, 2f)] public float detailSpeed = 0.8f;
    public Vector3 shapeOffset;
    public Vector3 detailOffset;

    // ── Cached references ──
    MeshRenderer _mr;
    Material     _mat;

    // ── Shader property IDs (cached for performance) ──
    static readonly int P_NoiseTex             = Shader.PropertyToID("_NoiseTex");
    static readonly int P_DetailNoiseTex       = Shader.PropertyToID("_DetailNoiseTex");
    static readonly int P_BlueNoise            = Shader.PropertyToID("_BlueNoise");
    static readonly int P_DensityMultiplier    = Shader.PropertyToID("_DensityMultiplier");
    static readonly int P_DensityOffset        = Shader.PropertyToID("_DensityOffset");
    static readonly int P_Scale                = Shader.PropertyToID("_Scale");
    static readonly int P_DetailScale          = Shader.PropertyToID("_DetailScale");
    static readonly int P_DetailWeight         = Shader.PropertyToID("_DetailWeight");
    static readonly int P_ShapeNoiseWeights    = Shader.PropertyToID("_ShapeNoiseWeights");
    static readonly int P_CloudColorLight      = Shader.PropertyToID("_CloudColorLight");
    static readonly int P_CloudColorDark       = Shader.PropertyToID("_CloudColorDark");
    static readonly int P_DarknessThreshold    = Shader.PropertyToID("_DarknessThreshold");
    static readonly int P_Posterize            = Shader.PropertyToID("_Posterize");
    static readonly int P_LightAbsorptionSun   = Shader.PropertyToID("_LightAbsorptionSun");
    static readonly int P_LightAbsorptionCloud = Shader.PropertyToID("_LightAbsorptionCloud");
    static readonly int P_PhaseG               = Shader.PropertyToID("_PhaseG");
    static readonly int P_NumStepsLight        = Shader.PropertyToID("_NumStepsLight");
    static readonly int P_NumStepsCloud        = Shader.PropertyToID("_NumStepsCloud");
    static readonly int P_RayOffsetStrength    = Shader.PropertyToID("_RayOffsetStrength");
    static readonly int P_TimeScale            = Shader.PropertyToID("_TimeScale");
    static readonly int P_BaseSpeed            = Shader.PropertyToID("_BaseSpeed");
    static readonly int P_DetailSpeed          = Shader.PropertyToID("_DetailSpeed");
    static readonly int P_ShapeOffset          = Shader.PropertyToID("_ShapeOffset");
    static readonly int P_DetailOffset         = Shader.PropertyToID("_DetailOffset");

    void OnEnable()
    {
        _mr  = GetComponent<MeshRenderer>();
        _mat = _mr.sharedMaterial;

        if (_mat == null)
            Debug.LogWarning("CloudVolume: No material assigned to the MeshRenderer.", this);
    }

    void Update()
    {
        if (_mat == null) return;
        PushToMaterial(_mat);
    }

    void PushToMaterial(Material mat)
    {
        // Textures
        if (shapeNoiseTex  != null) mat.SetTexture(P_NoiseTex,       shapeNoiseTex);
        if (detailNoiseTex != null) mat.SetTexture(P_DetailNoiseTex, detailNoiseTex);
        if (blueNoiseTex   != null) mat.SetTexture(P_BlueNoise,      blueNoiseTex);

        // Shape
        mat.SetFloat(P_DensityMultiplier, densityMultiplier);
        mat.SetFloat(P_DensityOffset,     densityOffset);
        mat.SetFloat(P_Scale,             scale);
        mat.SetFloat(P_DetailScale,       detailScale);
        mat.SetFloat(P_DetailWeight,      detailWeight);
        mat.SetVector(P_ShapeNoiseWeights, shapeNoiseWeights);

        // Colour
        mat.SetColor(P_CloudColorLight,   cloudColorLight);
        mat.SetColor(P_CloudColorDark,    cloudColorDark);
        mat.SetFloat(P_DarknessThreshold, darknessThreshold);
        mat.SetFloat(P_Posterize,         posterizeSteps);

        // Lighting
        mat.SetFloat(P_LightAbsorptionSun,   lightAbsorptionSun);
        mat.SetFloat(P_LightAbsorptionCloud, lightAbsorptionCloud);
        mat.SetFloat(P_PhaseG,               phaseG);
        mat.SetInt(P_NumStepsLight,           numStepsLight);

        // Ray march
        mat.SetInt(P_NumStepsCloud,       numStepsCloud);
        mat.SetFloat(P_RayOffsetStrength, rayOffsetStrength);

        // Animation
        mat.SetFloat(P_TimeScale,   timeScale);
        mat.SetFloat(P_BaseSpeed,   baseSpeed);
        mat.SetFloat(P_DetailSpeed, detailSpeed);
        mat.SetVector(P_ShapeOffset,  new Vector4(shapeOffset.x,  shapeOffset.y,  shapeOffset.z,  0));
        mat.SetVector(P_DetailOffset, new Vector4(detailOffset.x, detailOffset.y, detailOffset.z, 0));
    }
}