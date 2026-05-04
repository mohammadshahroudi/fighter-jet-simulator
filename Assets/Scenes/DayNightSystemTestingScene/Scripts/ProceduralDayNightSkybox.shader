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

            half4 frag(Varyings input) : SV_Target
            {
                float3 dir = normalize(input.directionWS);

                // Sky gradient
                float height01 = saturate(dir.y * 0.5 + 0.5);
                float gradient = smoothstep(0.0, 1.0, height01);

                half4 dayColor = lerp(_DayHorizonColor, _DayTopColor, gradient);
                half4 nightColor = lerp(_NightHorizonColor, _NightTopColor, gradient);

                half4 finalColor = lerp(dayColor, nightColor, saturate(_Blend));

                // Sun disk and glow
                float3 sunDir = normalize(_SunDirection.xyz);
                float sunDot = saturate(dot(dir, sunDir));

                float sunDisk = smoothstep(1.0 - _SunSize, 1.0, sunDot);

                float sunGlow = smoothstep(1.0 - _SunGlowSize, 1.0, sunDot);
                sunGlow *= sunGlow;

                float sunVisibility = 1.0 - saturate(_Blend);

                half3 sunColor = _SunColor.rgb * _SunIntensity * sunVisibility;

                finalColor.rgb += sunColor * sunGlow * 0.35;
                finalColor.rgb += sunColor * sunDisk;

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