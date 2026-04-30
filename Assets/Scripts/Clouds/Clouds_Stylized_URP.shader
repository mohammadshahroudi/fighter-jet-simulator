Shader "Custom/Clouds_Stylized_URP"
{
    Properties
    {
        [Header(Cloud Shape)]
        _DensityMultiplier  ("Density Multiplier",      Range(0, 4))    = 1.0
        _DensityOffset      ("Density Offset",          Range(-1, 1))   = -0.3
        _Scale              ("Shape Scale",             Range(0.1, 10)) = 1.0
        _DetailScale        ("Detail Scale",            Range(0.1, 10)) = 3.0
        _DetailWeight       ("Detail Weight",           Range(0, 1))    = 0.3
        _ShapeNoiseWeights  ("Shape Noise Weights",     Vector)         = (1, 0.5, 0.25, 0.125)

        [Header(Cloud Colour)]
        [HDR] _CloudColorLight  ("Cloud Light Color",  Color) = (1, 1, 1, 1)
        [HDR] _CloudColorDark   ("Cloud Shadow Color", Color) = (0.4, 0.5, 0.6, 1)
        _DarknessThreshold      ("Darkness Threshold", Range(0, 1))   = 0.15
        _Posterize              ("Posterize Steps",     Range(1, 8))   = 3

        [Header(Lighting)]
        _LightAbsorptionSun     ("Light Absorption Sun",   Range(0, 5))    = 1.0
        _LightAbsorptionCloud   ("Light Absorption Cloud", Range(0, 5))    = 1.0
        _PhaseG                 ("Phase Forward Scatter",  Range(0, 0.99)) = 0.3
        _NumStepsLight          ("Light March Steps",      Range(1, 16))   = 6

        [Header(Ray March)]
        _NumStepsCloud      ("Cloud March Steps",   Range(4, 128)) = 48
        _RayOffsetStrength  ("Blue Noise Strength", Range(0, 2))   = 1.0

        [Header(Animation)]
        _TimeScale      ("Time Scale",   Range(0, 2)) = 0.5
        _BaseSpeed      ("Base Speed",   Range(0, 2)) = 0.5
        _DetailSpeed    ("Detail Speed", Range(0, 2)) = 0.8
        _ShapeOffset    ("Shape Offset",  Vector) = (0, 0, 0, 0)
        _DetailOffset   ("Detail Offset", Vector) = (0, 0, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent+1"
            "RenderPipeline" = "UniversalPipeline"
        }

        // Cull Front = render inner faces so the effect works when the
        // camera is both outside and inside the cloud volume
        Cull Front
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "CloudsStylizedPass"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            // ───────────────────────────────────────────────────────
            // Structs
            // ───────────────────────────────────────────────────────
            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 worldPos    : TEXCOORD0;
            };

            // ───────────────────────────────────────────────────────
            // Textures (must be outside CBUFFER)
            // ───────────────────────────────────────────────────────
            Texture3D<float4> _NoiseTex;
            Texture3D<float4> _DetailNoiseTex;
            Texture2D<float4> _BlueNoise;

            SamplerState sampler_NoiseTex;
            SamplerState sampler_DetailNoiseTex;
            SamplerState sampler_BlueNoise;

            // ───────────────────────────────────────────────────────
            // Constant Buffer
            // ───────────────────────────────────────────────────────
            CBUFFER_START(UnityPerMaterial)
                float  _DensityMultiplier;
                float  _DensityOffset;
                float  _Scale;
                float  _DetailScale;
                float  _DetailWeight;
                float4 _ShapeNoiseWeights;

                float4 _CloudColorLight;
                float4 _CloudColorDark;
                float  _DarknessThreshold;
                float  _Posterize;

                float  _LightAbsorptionSun;
                float  _LightAbsorptionCloud;
                float  _PhaseG;
                int    _NumStepsLight;

                int    _NumStepsCloud;
                float  _RayOffsetStrength;

                float  _TimeScale;
                float  _BaseSpeed;
                float  _DetailSpeed;
                float4 _ShapeOffset;
                float4 _DetailOffset;
            CBUFFER_END

            // ───────────────────────────────────────────────────────
            // Helpers
            // ───────────────────────────────────────────────────────
            float remap(float v, float lo, float hi, float newLo, float newHi)
            {
                return newLo + (v - lo) * (newHi - newLo) / (hi - lo);
            }

            float2 rayBoxDst(float3 bMin, float3 bMax, float3 ro, float3 invRd)
            {
                float3 t0   = (bMin - ro) * invRd;
                float3 t1   = (bMax - ro) * invRd;
                float3 tmin = min(t0, t1);
                float3 tmax = max(t0, t1);
                float  dstA = max(max(tmin.x, tmin.y), tmin.z);
                float  dstB = min(tmax.x, min(tmax.y, tmax.z));
                return float2(max(0.0, dstA), max(0.0, dstB - max(0.0, dstA)));
            }

            float hgPhase(float cosTheta, float g)
            {
                float g2 = g * g;
                return (1.0 - g2) / (4.0 * PI * pow(abs(1.0 + g2 - 2.0 * g * cosTheta), 1.5));
            }

            // ───────────────────────────────────────────────────────
            // Density sample
            // ───────────────────────────────────────────────────────
            float sampleDensity(float3 pos, float3 bMin, float3 bMax)
            {
                const float baseScale   = 1.0 / 1000.0;
                const float offsetSpeed = 1.0 / 100.0;
                const int   mip         = 0;

                float  t    = _Time.x * _TimeScale;
                float3 size = bMax - bMin;

                float3 uvw      = (size * 0.5 + pos) * baseScale * _Scale;
                float3 shapeUVW = uvw
                                + _ShapeOffset.xyz * offsetSpeed
                                + float3(t, t * 0.1, t * 0.2) * _BaseSpeed;

                // X/Z edge fade
                const float edgeFade = 50.0;
                float ex     = min(edgeFade, min(pos.x - bMin.x, bMax.x - pos.x));
                float ez     = min(edgeFade, min(pos.z - bMin.z, bMax.z - pos.z));
                float edgeW  = min(ex, ez) / edgeFade;

                // Height gradient
                float hp       = (pos.y - bMin.y) / size.y;
                float heightW  = saturate(remap(hp, 0.0, 0.2, 0.0, 1.0))
                               * saturate(remap(hp, 1.0, 0.7, 0.0, 1.0));
                heightW *= edgeW;

                // Shape FBM
                float4 shape     = _NoiseTex.SampleLevel(sampler_NoiseTex, shapeUVW, mip);
                float4 normW     = _ShapeNoiseWeights / dot(_ShapeNoiseWeights, 1.0);
                float  shapeFBM  = dot(shape, normW) * heightW;
                float  baseDens  = shapeFBM + _DensityOffset * 0.1;

                if (baseDens > 0.0)
                {
                    float3 detailUVW = uvw * _DetailScale
                                     + _DetailOffset.xyz * offsetSpeed
                                     + float3(t * 0.4, -t, t * 0.1) * _DetailSpeed;
                    float4 detail    = _DetailNoiseTex.SampleLevel(sampler_DetailNoiseTex, detailUVW, mip);
                    float  detailFBM = detail.r * 0.5 + detail.g * 0.3 + detail.b * 0.2;

                    float oms    = 1.0 - shapeFBM;
                    float erode  = oms * oms * oms;
                    float dens   = baseDens - (1.0 - detailFBM) * erode * _DetailWeight;

                    return max(0.0, dens * _DensityMultiplier * 0.1);
                }
                return 0.0;
            }

            // ───────────────────────────────────────────────────────
            // Light march
            // ───────────────────────────────────────────────────────
            float lightMarch(float3 pos, float3 bMin, float3 bMax, float3 lightDir)
            {
                float dstInBox  = rayBoxDst(bMin, bMax, pos, 1.0 / lightDir).y;
                float stepSize  = dstInBox / max(1, _NumStepsLight);
                float totalDens = 0.0;

                for (int i = 0; i < _NumStepsLight; i++)
                {
                    pos      += lightDir * stepSize;
                    totalDens += max(0.0, sampleDensity(pos, bMin, bMax) * stepSize);
                }

                float t = exp(-totalDens * _LightAbsorptionSun);
                return _DarknessThreshold + t * (1.0 - _DarknessThreshold);
            }

            // ───────────────────────────────────────────────────────
            // Vertex shader
            // ───────────────────────────────────────────────────────
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.worldPos    = TransformObjectToWorld(IN.positionOS.xyz);
                return OUT;
            }

            // ───────────────────────────────────────────────────────
            // Fragment shader
            // ───────────────────────────────────────────────────────
            float4 frag(Varyings IN) : SV_Target
            {
                // ── Bounding box from object transform ──
                // Scale comes from column lengths of the rotation-scale submatrix
                float3 objScale = float3(
                    length(unity_ObjectToWorld._m00_m10_m20),
                    length(unity_ObjectToWorld._m01_m11_m21),
                    length(unity_ObjectToWorld._m02_m12_m22)
                );
                float3 objCenter = unity_ObjectToWorld._m03_m13_m23;
                float3 bMin = objCenter - objScale * 0.5;
                float3 bMax = objCenter + objScale * 0.5;

                // ── Ray ──
                float3 camPos = _WorldSpaceCameraPos;
                float3 rayDir = normalize(IN.worldPos - camPos);

                // ── Screen UV — use _ScaledScreenParams for correct DPI/resolution ──
                float2 screenUV = IN.positionHCS.xy / _ScaledScreenParams.xy;

                // ── Depth — reconstruct linear eye depth, corrected for view angle ──
                float rawDepth   = SampleSceneDepth(screenUV);
                // dot with forward gives the perpendicular (eye) distance
                float3 camFwd    = -UNITY_MATRIX_V[2].xyz;
                float  sceneDepth = LinearEyeDepth(rawDepth, _ZBufferParams)
                                  / dot(rayDir, camFwd);

                // ── Box intersection ──
                float2 box      = rayBoxDst(bMin, bMax, camPos, 1.0 / rayDir);
                float  dstToBox = box.x;
                float  dstInBox = box.y;

                if (dstInBox <= 0.0) return float4(0, 0, 0, 0);

                // ── Blue noise offset ──
                float2 noiseUV    = screenUV * (_ScreenParams.xy / 128.0);
                float  randOffset = _BlueNoise.SampleLevel(sampler_BlueNoise, noiseUV, 0).r
                                  * _RayOffsetStrength;

                // ── Lighting ──
                Light  mainLight = GetMainLight();
                float  cosAngle  = dot(rayDir, mainLight.direction);
                float  phaseVal  = hgPhase(cosAngle, _PhaseG);

                // ── March setup ──
                float dstLimit  = min(sceneDepth - dstToBox, dstInBox);
                float stepSize  = dstInBox / max(1.0, (float)_NumStepsCloud);

                float3 entryPt      = camPos + rayDir * dstToBox;
                float  dstTravelled = randOffset;
                float  transmit     = 1.0;
                float3 lightEnergy  = 0.0;

                // ── Ray march ──
                [loop]
                while (dstTravelled < dstLimit)
                {
                    float3 pos    = entryPt + rayDir * dstTravelled;
                    float  dens   = sampleDensity(pos, bMin, bMax);

                    if (dens > 0.0)
                    {
                        float lightT      = lightMarch(pos, bMin, bMax, mainLight.direction);
                        float posterizedT = floor(lightT * _Posterize) / _Posterize;

                        lightEnergy += dens * stepSize * transmit * posterizedT * phaseVal;
                        transmit    *= exp(-dens * stepSize * _LightAbsorptionCloud);

                        if (transmit < 0.01) break;
                    }
                    dstTravelled += stepSize;
                }

                // ── Final colour ──
                float  energyNorm = saturate(length(lightEnergy));
                float3 cloudCol   = lerp(_CloudColorDark.rgb, _CloudColorLight.rgb, energyNorm)
                                  * mainLight.color;
                float  alpha      = 1.0 - transmit;

                return float4(cloudCol, alpha);
            }

            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
