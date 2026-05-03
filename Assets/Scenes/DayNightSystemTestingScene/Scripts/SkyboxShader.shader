Shader "Skybox/NightDay 6 Sided URP"
{
    Properties
    {
        [Header(Skybox Set 1)]
        _FrontTex1("Front (+Z)", 2D) = "white" {}
        _BackTex1("Back (-Z)", 2D) = "white" {}
        _LeftTex1("Left (+X)", 2D) = "white" {}
        _RightTex1("Right (-X)", 2D) = "white" {}
        _UpTex1("Up (+Y)", 2D) = "white" {}
        _DownTex1("Down (-Y)", 2D) = "white" {}

        [Header(Skybox Set 2)]
        _FrontTex2("Front (+Z)", 2D) = "white" {}
        _BackTex2("Back (-Z)", 2D) = "white" {}
        _LeftTex2("Left (+X)", 2D) = "white" {}
        _RightTex2("Right (-X)", 2D) = "white" {}
        _UpTex2("Up (+Y)", 2D) = "white" {}
        _DownTex2("Down (-Y)", 2D) = "white" {}

        [Header(Blending)]
        _Blend("Blend", Range(0, 1)) = 0.0
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
        ZTest LEqual

        Pass
        {
            Name "Skybox"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

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
                float3 texcoord : TEXCOORD0;
            };

            TEXTURE2D(_FrontTex1);
            SAMPLER(sampler_FrontTex1);

            TEXTURE2D(_BackTex1);
            SAMPLER(sampler_BackTex1);

            TEXTURE2D(_LeftTex1);
            SAMPLER(sampler_LeftTex1);

            TEXTURE2D(_RightTex1);
            SAMPLER(sampler_RightTex1);

            TEXTURE2D(_UpTex1);
            SAMPLER(sampler_UpTex1);

            TEXTURE2D(_DownTex1);
            SAMPLER(sampler_DownTex1);

            TEXTURE2D(_FrontTex2);
            SAMPLER(sampler_FrontTex2);

            TEXTURE2D(_BackTex2);
            SAMPLER(sampler_BackTex2);

            TEXTURE2D(_LeftTex2);
            SAMPLER(sampler_LeftTex2);

            TEXTURE2D(_RightTex2);
            SAMPLER(sampler_RightTex2);

            TEXTURE2D(_UpTex2);
            SAMPLER(sampler_UpTex2);

            TEXTURE2D(_DownTex2);
            SAMPLER(sampler_DownTex2);

            CBUFFER_START(UnityPerMaterial)
                half _Blend;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.texcoord = input.positionOS.xyz;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 dir = normalize(input.texcoord);
                float3 absDir = abs(dir);

                half4 color1 = half4(1.0, 1.0, 1.0, 1.0);
                half4 color2 = half4(1.0, 1.0, 1.0, 1.0);
                float2 uv = float2(0.0, 0.0);

                if (absDir.z >= absDir.x && absDir.z >= absDir.y)
                {
                    if (dir.z > 0.0)
                    {
                        uv = float2(dir.x, dir.y) / absDir.z;
                        uv = uv * 0.5 + 0.5;

                        color1 = SAMPLE_TEXTURE2D(_FrontTex1, sampler_FrontTex1, uv);
                        color2 = SAMPLE_TEXTURE2D(_FrontTex2, sampler_FrontTex2, uv);
                    }
                    else
                    {
                        uv = float2(-dir.x, dir.y) / absDir.z;
                        uv = uv * 0.5 + 0.5;

                        color1 = SAMPLE_TEXTURE2D(_BackTex1, sampler_BackTex1, uv);
                        color2 = SAMPLE_TEXTURE2D(_BackTex2, sampler_BackTex2, uv);
                    }
                }
                else if (absDir.x >= absDir.y)
                {
                    if (dir.x > 0.0)
                    {
                        uv = float2(-dir.z, dir.y) / absDir.x;
                        uv = uv * 0.5 + 0.5;

                        color1 = SAMPLE_TEXTURE2D(_LeftTex1, sampler_LeftTex1, uv);
                        color2 = SAMPLE_TEXTURE2D(_LeftTex2, sampler_LeftTex2, uv);
                    }
                    else
                    {
                        uv = float2(dir.z, dir.y) / absDir.x;
                        uv = uv * 0.5 + 0.5;

                        color1 = SAMPLE_TEXTURE2D(_RightTex1, sampler_RightTex1, uv);
                        color2 = SAMPLE_TEXTURE2D(_RightTex2, sampler_RightTex2, uv);
                    }
                }
                else
                {
                    if (dir.y > 0.0)
                    {
                        uv = float2(dir.x, -dir.z) / absDir.y;
                        uv = uv * 0.5 + 0.5;

                        color1 = SAMPLE_TEXTURE2D(_UpTex1, sampler_UpTex1, uv);
                        color2 = SAMPLE_TEXTURE2D(_UpTex2, sampler_UpTex2, uv);
                    }
                    else
                    {
                        uv = float2(dir.x, dir.z) / absDir.y;
                        uv = uv * 0.5 + 0.5;

                        color1 = SAMPLE_TEXTURE2D(_DownTex1, sampler_DownTex1, uv);
                        color2 = SAMPLE_TEXTURE2D(_DownTex2, sampler_DownTex2, uv);
                    }
                }

                return lerp(color1, color2, saturate(_Blend));
            }

            ENDHLSL
        }
    }

    FallBack Off
}