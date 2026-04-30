Shader "Custom/Clouds_URP"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            // ── URP requires a named pass for Renderer Features ──
            Name "CloudsPass"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            // ── URP core replaces UnityCG.cginc ──
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // ── URP depth texture helpers (replaces manual _CameraDepthTexture) ──
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            // Your existing debug include (keep as-is, but see note below)
            // #include "Assets/Scripts/Clouds/Shaders/CloudDebug.cginc"
            // NOTE: If CloudDebug.cginc uses CG macros, rename to .hlsl and
            //       replace any CG-specific includes inside it.

            // -------------------------------------------------------
            // Structs
            // -------------------------------------------------------
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 viewVector  : TEXCOORD1;
            };

            // -------------------------------------------------------
            // Textures & Samplers
            // -------------------------------------------------------
            Texture3D<float4>   NoiseTex;
            Texture3D<float4>   DetailNoiseTex;
            Texture2D<float4>   WeatherMap;
            Texture2D<float4>   BlueNoise;

            SamplerState samplerNoiseTex;
            SamplerState samplerDetailNoiseTex;
            SamplerState samplerWeatherMap;
            SamplerState samplerBlueNoise;

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            // -------------------------------------------------------
            // Constant Buffer  (URP best-practice: wrap in CBUFFER)
            // -------------------------------------------------------
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;

                // Shape settings
                float4 params;
                int3   mapSize;
                float  densityMultiplier;
                float  densityOffset;
                float  scale;
                float  detailNoiseScale;
                float  detailNoiseWeight;
                float3 detailWeights;
                float4 shapeNoiseWeights;
                float4 phaseParams;

                // March settings
                int   numStepsLight;
                float rayOffsetStrength;

                float3 boundsMin;
                float3 boundsMax;

                float3 shapeOffset;
                float3 detailOffset;

                // Light settings
                float  lightAbsorptionTowardSun;
                float  lightAbsorptionThroughCloud;
                float  darknessThreshold;
                float4 colA;
                float4 colB;

                // Animation settings
                float timeScale;
                float baseSpeed;
                float detailSpeed;

                // Debug settings
                int   debugViewMode;
                int   debugGreyscale;
                int   debugShowAllChannels;
                float debugNoiseSliceDepth;
                float4 debugChannelWeight;
                float debugTileAmount;
                float viewerSize;
            CBUFFER_END

            // -------------------------------------------------------
            // Vertex Shader
            // -------------------------------------------------------
            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // ── URP replacement for UnityObjectToClipPos ──
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;

                // Reconstruct world-space view vector
                // unity_CameraInvProjection and unity_CameraToWorld are still
                // available in URP via UnityInput.hlsl (included via Core.hlsl)
                float3 viewVector = mul(unity_CameraInvProjection,
                                        float4(IN.uv * 2.0 - 1.0, 0.0, -1.0)).xyz;
                OUT.viewVector = mul(unity_CameraToWorld,
                                     float4(viewVector, 0.0)).xyz;
                return OUT;
            }

            // -------------------------------------------------------
            // Helper Functions  (unchanged logic, URP-safe)
            // -------------------------------------------------------
            float remap(float v, float minOld, float maxOld, float minNew, float maxNew)
            {
                return minNew + (v - minOld) * (maxNew - minNew) / (maxOld - minOld);
            }

            float remap01(float v, float low, float high)
            {
                return (v - low) / (high - low);
            }

            float2 squareUV(float2 uv)
            {
                float width  = _ScreenParams.x;
                float height = _ScreenParams.y;
                float s      = 1000.0;
                return float2((uv.x * width) / s, (uv.y * height) / s);
            }

            // Returns (dstToBox, dstInsideBox)
            float2 rayBoxDst(float3 bMin, float3 bMax, float3 rayOrigin, float3 invRayDir)
            {
                float3 t0 = (bMin - rayOrigin) * invRayDir;
                float3 t1 = (bMax - rayOrigin) * invRayDir;
                float3 tmin = min(t0, t1);
                float3 tmax = max(t0, t1);

                float dstA = max(max(tmin.x, tmin.y), tmin.z);
                float dstB = min(tmax.x, min(tmax.y, tmax.z));

                float dstToBox     = max(0.0, dstA);
                float dstInsideBox = max(0.0, dstB - dstToBox);
                return float2(dstToBox, dstInsideBox);
            }

            // Henyey-Greenstein phase function
            float hg(float a, float g)
            {
                float g2 = g * g;
                return (1.0 - g2) / (4.0 * PI * pow(abs(1.0 + g2 - 2.0 * g * a), 1.5));
            }

            float phase(float a)
            {
                float blend    = 0.5;
                float hgBlend  = hg(a, phaseParams.x) * (1.0 - blend)
                               + hg(a, -phaseParams.y) * blend;
                return phaseParams.z + hgBlend * phaseParams.w;
            }

            float sampleDensity(float3 rayPos)
            {
                const int   mipLevel    = 0;
                const float baseScale   = 1.0 / 1000.0;
                const float offsetSpeed = 1.0 / 100.0;

                float  time          = _Time.x * timeScale;
                float3 size          = boundsMax - boundsMin;
                float3 uvw           = (size * 0.5 + rayPos) * baseScale * scale;
                float3 shapeSamplePos = uvw
                                      + shapeOffset * offsetSpeed
                                      + float3(time, time * 0.1, time * 0.2) * baseSpeed;

                // Edge fade
                const float containerEdgeFadeDst = 50.0;
                float dstFromEdgeX = min(containerEdgeFadeDst,
                                         min(rayPos.x - boundsMin.x, boundsMax.x - rayPos.x));
                float dstFromEdgeZ = min(containerEdgeFadeDst,
                                         min(rayPos.z - boundsMin.z, boundsMax.z - rayPos.z));
                float edgeWeight = min(dstFromEdgeZ, dstFromEdgeX) / containerEdgeFadeDst;

                // Height gradient
                float gMin          = 0.2;
                float gMax          = 0.7;
                float heightPercent = (rayPos.y - boundsMin.y) / size.y;
                float heightGradient = saturate(remap(heightPercent, 0.0, gMin, 0.0, 1.0))
                                     * saturate(remap(heightPercent, 1.0, gMax, 0.0, 1.0));
                heightGradient *= edgeWeight;

                // Base shape
                float4 shapeNoise             = NoiseTex.SampleLevel(samplerNoiseTex, shapeSamplePos, mipLevel);
                float4 normalizedShapeWeights = shapeNoiseWeights / dot(shapeNoiseWeights, 1.0);
                float  shapeFBM               = dot(shapeNoise, normalizedShapeWeights) * heightGradient;
                float  baseShapeDensity       = shapeFBM + densityOffset * 0.1;

                if (baseShapeDensity > 0.0)
                {
                    // Detail erosion
                    float3 detailSamplePos = uvw * detailNoiseScale
                                           + detailOffset * offsetSpeed
                                           + float3(time * 0.4, -time, time * 0.1) * detailSpeed;
                    float4 detailNoise             = DetailNoiseTex.SampleLevel(samplerDetailNoiseTex, detailSamplePos, mipLevel);
                    float3 normalizedDetailWeights = detailWeights / dot(detailWeights, 1.0);
                    float  detailFBM               = dot(detailNoise, normalizedDetailWeights);

                    float oneMinusShape    = 1.0 - shapeFBM;
                    float detailErodeWeight = oneMinusShape * oneMinusShape * oneMinusShape;
                    float cloudDensity     = baseShapeDensity
                                           - (1.0 - detailFBM) * detailErodeWeight * detailNoiseWeight;

                    return cloudDensity * densityMultiplier * 0.1;
                }
                return 0.0;
            }

            float lightmarch(float3 position)
            {
                // ── URP: get main light direction via GetMainLight() ──
                Light mainLight    = GetMainLight();
                float3 dirToLight  = mainLight.direction;

                float dstInsideBox = rayBoxDst(boundsMin, boundsMax, position, 1.0 / dirToLight).y;
                float stepSize     = dstInsideBox / numStepsLight;
                float totalDensity = 0.0;

                for (int step = 0; step < numStepsLight; step++)
                {
                    position   += dirToLight * stepSize;
                    totalDensity += max(0.0, sampleDensity(position) * stepSize);
                }

                float transmittance = exp(-totalDensity * lightAbsorptionTowardSun);
                return darknessThreshold + transmittance * (1.0 - darknessThreshold);
            }

            float4 debugDrawNoise(float2 uv)
            {
                float4 channels   = 0;
                float3 samplePos  = float3(uv.x, uv.y, debugNoiseSliceDepth);

                if      (debugViewMode == 1) channels = NoiseTex.SampleLevel(samplerNoiseTex, samplePos, 0);
                else if (debugViewMode == 2) channels = DetailNoiseTex.SampleLevel(samplerDetailNoiseTex, samplePos, 0);
                else if (debugViewMode == 3) channels = WeatherMap.SampleLevel(samplerWeatherMap, samplePos.xy, 0);

                if (debugShowAllChannels) return channels;

                float4 maskedChannels = channels * debugChannelWeight;
                if (debugGreyscale || debugChannelWeight.w == 1)
                    return dot(maskedChannels, 1.0);
                else
                    return maskedChannels;
            }

            // -------------------------------------------------------
            // Fragment Shader
            // -------------------------------------------------------
            float4 frag(Varyings IN) : SV_Target
            {
                // Debug view
                #if DEBUG_MODE == 1
                if (debugViewMode != 0)
                {
                    float width  = _ScreenParams.x;
                    float height = _ScreenParams.y;
                    float minDim = min(width, height);
                    float x = IN.uv.x * width;
                    float y = (1.0 - IN.uv.y) * height;

                    if (x < minDim * viewerSize && y < minDim * viewerSize)
                    {
                        return debugDrawNoise(float2(x / (minDim * viewerSize) * debugTileAmount,
                                                     y / (minDim * viewerSize) * debugTileAmount));
                    }
                }
                #endif

                // ── Ray setup ──
                float3 rayPos   = _WorldSpaceCameraPos;
                float  viewLen  = length(IN.viewVector);
                float3 rayDir   = IN.viewVector / viewLen;

                // ── Depth reconstruction (URP) ──
                // SampleSceneDepth comes from DeclareDepthTexture.hlsl
                float rawDepth = SampleSceneDepth(IN.uv);
                float depth    = LinearEyeDepth(rawDepth, _ZBufferParams) * viewLen;

                // Cloud container intersection
                float2 rayToContainerInfo = rayBoxDst(boundsMin, boundsMax, rayPos, 1.0 / rayDir);
                float  dstToBox           = rayToContainerInfo.x;
                float  dstInsideBox       = rayToContainerInfo.y;

                float3 entryPoint   = rayPos + rayDir * dstToBox;

                // Blue noise offset
                float randomOffset  = BlueNoise.SampleLevel(samplerBlueNoise, squareUV(IN.uv * 3.0), 0).r;
                randomOffset       *= rayOffsetStrength;

                // Phase
                // ── URP: use GetMainLight().direction instead of _WorldSpaceLightPos0 ──
                Light  mainLight = GetMainLight();
                float  cosAngle  = dot(rayDir, mainLight.direction);
                float  phaseVal  = phase(cosAngle);

                float dstTravelled = randomOffset;
                float dstLimit     = min(depth - dstToBox, dstInsideBox);

                const float stepSize = 11.0;

                float  transmittance = 1.0;
                float3 lightEnergy   = 0.0;

                while (dstTravelled < dstLimit)
                {
                    rayPos = entryPoint + rayDir * dstTravelled;
                    float density = sampleDensity(rayPos);

                    if (density > 0.0)
                    {
                        float lightTransmittance = lightmarch(rayPos);
                        lightEnergy   += density * stepSize * transmittance * lightTransmittance * phaseVal;
                        transmittance *= exp(-density * stepSize * lightAbsorptionThroughCloud);

                        if (transmittance < 0.01) break;
                    }
                    dstTravelled += stepSize;
                }

                // Composite clouds over background
                // ── URP: use SAMPLE_TEXTURE2D instead of tex2D ──
                float3 backgroundCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).rgb;

                // ── URP: get light colour from GetMainLight().color ──
                float3 cloudCol = lightEnergy * mainLight.color;
                float3 col      = backgroundCol * transmittance + cloudCol;

                return float4(col, 0.0);
            }

            ENDHLSL
        }
    }
}
