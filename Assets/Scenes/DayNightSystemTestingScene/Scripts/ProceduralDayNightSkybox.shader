Shader "Skybox/Procedural Day Night URP"
{
    Properties
    {
        [Header(Day Colors)]
        _DayTopColor("Day Top Color", Color) = (0.05, 0.35, 0.9, 1)
        _DayHorizonColor("Day Horizon Color", Color) = (0.6, 0.85, 1.0, 1)

        [Header(Night Colors)]
        _NightTopColor("Night Top Color", Color) = (0.005, 0.01, 0.04, 1)
        _NightHorizonColor("Night Horizon Color", Color) = (0.02, 0.04, 0.10, 1)

        [Header(Sun)]
        _SunColor("Sun Color", Color) = (1.0, 0.85, 0.45, 1)
        _SunSize("Sun Size", Range(0.0001, 0.1)) = 0.015
        _SunGlowSize("Sun Glow Size", Range(0.01, 1.0)) = 0.25
        _SunIntensity("Sun Intensity", Range(0, 20)) = 4
        _SunDirection("Sun Direction", Vector) = (0, 1, 0, 0)
        
        [Header(Sun Horizon Fade)]
        _SunFadeStart("Sun Fade Start", Range(-1, 1)) = 0.05
        _SunFadeEnd("Sun Fade End", Range(-1, 1)) = -0.10
        
        [Header(Sunrise Sunset)]
        _SunsetColor("Sunset Color", Color) = (1.0, 0.35, 0.08, 1)
        _SunsetIntensity("Sunset Intensity", Range(0, 5)) = 1.5
        _SunsetHeight("Sunset Height", Range(0.01, 1.0)) = 0.35
        _SunsetWidth("Sunset Width", Range(0.01, 1.0)) = 0.45
        
        [Header(Moon)]
        [NoScaleOffset] _MoonTex("Moon Texture", 2D) = "white" {}
        _MoonTextureStrength("Moon Texture Strength", Range(0, 2)) = 1
        _MoonTextureRotation("Moon Texture Rotation", Range(0, 360)) = 0
        
        _MoonColor("Moon Color", Color) = (0.75, 0.85, 1.0, 1)
        _MoonSize("Moon Size", Range(0.0001, 0.1)) = 0.025
        _MoonGlowSize("Moon Glow Size", Range(0.01, 1.0)) = 0.15
        _MoonIntensity("Moon Intensity", Range(0, 10)) = 2
        _MoonDirection("Moon Direction", Vector) = (0, -1, 0, 0)
        
        [Header(Moon Horizon Fade)]
        _MoonFadeStart("Moon Fade Start", Range(-1, 1)) = 0.05
        _MoonFadeEnd("Moon Fade End", Range(-1, 1)) = -0.10
        
        [Header(Stars)]
        _StarColor("Star Color", Color) = (1, 1, 1, 1)
        _StarDensity("Star Density", Range(0, 1)) = 0.04
        _StarScale("Star Scale", Range(50, 2000)) = 700
        _StarIntensity("Star Intensity", Range(0, 5)) = 1.5
        _StarTwinkleSpeed("Star Twinkle Speed", Range(0, 10)) = 2
        _StarTwinkleAmount("Star Twinkle Amount", Range(0, 1)) = 0.5

        [Header(General)]
        _Tint("Tint Color", Color) = (1, 1, 1, 1)
        _Exposure("Exposure", Range(0, 8)) = 1
        _Rotation("Rotation", Range(0, 360)) = 0
        _Blend("Blend", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Background"
            "RenderType" = "Background"
            "PreviewType" = "Skybox"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        ZWrite Off

        Pass
        {
            Name "ProceduralSky"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 directionWS : TEXCOORD0;
            };
            
            TEXTURE2D(_MoonTex);
            SAMPLER(sampler_MoonTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _DayTopColor;
                half4 _DayHorizonColor;
                half4 _NightTopColor;
                half4 _NightHorizonColor;

                half4 _SunColor;
                half _SunSize;
                half _SunGlowSize;
                half _SunIntensity;
                float4 _SunDirection;
            
                half _SunFadeStart;
                half _SunFadeEnd;
            
                half4 _SunsetColor;
                half _SunsetIntensity;
                half _SunsetHeight;
                half _SunsetWidth;
            
                half _MoonTextureStrength;
                float _MoonTextureRotation;
            
                half4 _MoonColor;
                half _MoonSize;
                half _MoonGlowSize;
                half _MoonIntensity;
                float4 _MoonDirection;
            
                half _MoonFadeStart;
                half _MoonFadeEnd;
            
                half4 _StarColor;
                half _StarDensity;
                float _StarScale;
                half _StarIntensity;
                half _StarTwinkleSpeed;
                half _StarTwinkleAmount;

                half4 _Tint;
                half _Exposure;
                float _Rotation;
                half _Blend;
            CBUFFER_END

            float3 RotateAroundYInDegrees(float3 dir, float degrees)
            {
                float radians = degrees * PI / 180.0;
                float s;
                float c;
                sincos(radians, s, c);

                float2x2 rotationMatrix = float2x2(c, -s, s, c);
                float2 rotatedXZ = mul(rotationMatrix, dir.xz);

                return normalize(float3(rotatedXZ.x, dir.y, rotatedXZ.y));
            }

            Varyings vert(Attributes input)
            {
                Varyings output;

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.directionWS = RotateAroundYInDegrees(input.positionOS.xyz, _Rotation);

                return output;
            }
            
            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }
            
            float2 RotateUV(float2 uv, float degrees)
            {
                float radians = degrees * PI / 180.0;
                float s;
                float c;
                sincos(radians, s, c);

                uv -= 0.5;

                float2x2 rotationMatrix = float2x2(c, -s, s, c);
                uv = mul(rotationMatrix, uv);

                uv += 0.5;
                return uv;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 dir = normalize(input.directionWS);

                // Sky gradient
                float height01 = saturate(dir.y * 0.5 + 0.5);
                float gradient = smoothstep(0.0, 1.0, height01);

                half4 dayColor = lerp(_DayHorizonColor, _DayTopColor, gradient);
                half4 nightColor = lerp(_NightHorizonColor, _NightTopColor, gradient);

                half blend = saturate(_Blend);
                half4 finalColor = lerp(dayColor, nightColor, blend);

                // Sun direction
                float3 sunDir = normalize(_SunDirection.xyz);

                // Sunrise / sunset horizon glow
                float sunNearHorizon = 1.0 - smoothstep(0.0, _SunsetWidth, abs(sunDir.y));
                float sunAboveOrNearHorizon = smoothstep(-0.25, 0.15, sunDir.y);

                // Strongest near the visual horizon, weaker overhead and below.
                float horizonMask = 1.0 - smoothstep(0.0, _SunsetHeight, abs(dir.y));

                // Stronger looking toward the sun, but still wraps around the horizon a bit.
                float3 flatDir = normalize(float3(dir.x, 0.0, dir.z) + float3(0.0001, 0.0, 0.0001));
                float3 flatSunDir = normalize(float3(sunDir.x, 0.0, sunDir.z) + float3(0.0001, 0.0, 0.0001));
                float towardSun = saturate(dot(flatDir, flatSunDir));
                float sunSideMask = lerp(0.35, 1.0, pow(towardSun, 2.0));

                float sunsetAmount = sunNearHorizon * sunAboveOrNearHorizon * horizonMask * sunSideMask;

                // Reduce sunset color once the scene is mostly night.
                sunsetAmount *= 1.0 - smoothstep(0.65, 1.0, blend);

                finalColor.rgb = lerp(finalColor.rgb, _SunsetColor.rgb, sunsetAmount * _SunsetIntensity);

                // Sun disk and glow
                float sunDot = saturate(dot(dir, sunDir));

                float sunDisk = smoothstep(1.0 - _SunSize, 1.0, sunDot);
                float sunGlow = smoothstep(1.0 - _SunGlowSize, 1.0, sunDot);
                sunGlow *= sunGlow;

                // Fade based on whether the sun is above or below the horizon
                float sunVisibility = smoothstep(_SunFadeEnd, _SunFadeStart, sunDir.y);
                
                half3 sunColor = _SunColor.rgb * _SunIntensity * sunVisibility;

                finalColor.rgb += sunColor * sunGlow * 0.35;
                finalColor.rgb += sunColor * sunDisk;

                // Moon disk, texture, and glow
                float3 moonDir = normalize(_MoonDirection.xyz);
                float moonDot = saturate(dot(dir, moonDir));

                float moonDisk = smoothstep(1.0 - _MoonSize, 1.0, moonDot);
                float moonGlow = smoothstep(1.0 - _MoonGlowSize, 1.0, moonDot);
                moonGlow *= moonGlow;

                // Fade based on whether the moon is above or below the horizon
                float moonVisibility = smoothstep(_MoonFadeEnd, _MoonFadeStart, moonDir.y);

                half3 moonColor = _MoonColor.rgb * _MoonIntensity * moonVisibility;

                // Build a local 2D coordinate system around the moon direction.
                // This lets the shader project a 2D moon texture onto the sky direction.
                float3 moonRight = normalize(cross(float3(0.0, 1.0, 0.0), moonDir));

                // Safety fallback in case the moon is almost straight up/down.
                moonRight = length(moonRight) < 0.001 ? float3(1.0, 0.0, 0.0) : moonRight;

                float3 moonUp = normalize(cross(moonDir, moonRight));

                float2 moonLocal;
                moonLocal.x = dot(dir, moonRight);
                moonLocal.y = dot(dir, moonUp);

                // Convert local direction around moon into 0-1 UV.
                // _MoonSize controls the visible radius.
                float2 moonUV = moonLocal / _MoonSize * 0.5 + 0.5;
                moonUV = RotateUV(moonUV, _MoonTextureRotation);

                // Only sample/show texture inside the disk area.
                float insideMoonUV =
                    step(0.0, moonUV.x) *
                    step(moonUV.x, 1.0) *
                    step(0.0, moonUV.y) *
                    step(moonUV.y, 1.0);

                half4 moonTex = SAMPLE_TEXTURE2D(_MoonTex, sampler_MoonTex, moonUV);

                // Use texture alpha if available.
                // The circular disk also helps mask the moon.
                float moonTextureMask = moonTex.a * insideMoonUV * moonDisk;

                // Glow stays procedural.
                finalColor.rgb += moonColor * moonGlow * 0.20;

                // Texture becomes the moon surface.
                half3 texturedMoon = moonTex.rgb * _MoonColor.rgb * _MoonIntensity;

                finalColor.rgb += texturedMoon * moonTextureMask * moonVisibility * _MoonTextureStrength;

                // Static procedural stars
                float2 starUV = dir.xz / max(dir.y + 1.0, 0.001);
                starUV *= _StarScale;

                // Divide the sky into tiny cells
                float2 starCell = floor(starUV);
                float2 starLocal = frac(starUV) - 0.5;

                // Random value per cell
                float starRandom = Hash21(starCell);
                
                float starBrightnessRandom = Hash21(starCell + 12.45);
                float starBaseBrightness = lerp(0.4, 1.0, starBrightnessRandom);

                // Only some cells contain stars
                float starExists = step(1.0 - _StarDensity, starRandom);

                // Small circular star shape inside the cell
                float starDistance = length(starLocal);
                float starShape = smoothstep(0.035, 0.0, starDistance);

                // Only show stars above the horizon
                float aboveHorizon = smoothstep(0.05, 0.25, dir.y);

                // Fade stars in at night
                float nightVisibility = smoothstep(0.35, 1.0, blend);

                // Each star gets a slightly different twinkle phase
                float twinkleSeed = Hash21(starCell + 37.17);

                // Twinkle value from 0 to 1
                float twinkle = sin(_Time.y * _StarTwinkleSpeed + twinkleSeed * 100.0);
                twinkle = twinkle * 0.5 + 0.5;

                // Make the bright/dim difference more noticeable
                twinkle = smoothstep(0.15, 1.0, twinkle);

                // Blend between steady brightness and twinkling brightness
                float twinkleBrightness = lerp(1.0, twinkle, _StarTwinkleAmount);

                // Let strong twinkle get much dimmer during testing
                twinkleBrightness = lerp(0.08, 1.0, twinkleBrightness);

                half3 starColor = _StarColor.rgb * _StarIntensity;

                finalColor.rgb += starColor * starExists * starShape * aboveHorizon * nightVisibility * twinkleBrightness * starBaseBrightness;
                
                // Final color controls
                finalColor.rgb *= _Tint.rgb;
                finalColor.rgb *= _Exposure;
                finalColor.a = 1.0;

                return finalColor;
            }

            ENDHLSL
        }
    }

    FallBack Off
}