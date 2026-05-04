Shader "Skybox/NightDay 6 Sided URP"
{
    Properties
    {
        [Header(Skybox Day Set)]
        [NoScaleOffset] _FrontTex1("Front [+Z]", 2D) = "grey" {}
        [NoScaleOffset] _BackTex1("Back [-Z]", 2D) = "grey" {}
        [NoScaleOffset] _LeftTex1("Left [+X]", 2D) = "grey" {}
        [NoScaleOffset] _RightTex1("Right [-X]", 2D) = "grey" {}
        [NoScaleOffset] _UpTex1("Up [+Y]", 2D) = "grey" {}
        [NoScaleOffset] _DownTex1("Down [-Y]", 2D) = "grey" {}

        [Header(Skybox Night Set)]
        [NoScaleOffset] _FrontTex2("Front 2 [+Z]", 2D) = "grey" {}
        [NoScaleOffset] _BackTex2("Back 2 [-Z]", 2D) = "grey" {}
        [NoScaleOffset] _LeftTex2("Left 2 [+X]", 2D) = "grey" {}
        [NoScaleOffset] _RightTex2("Right 2 [-X]", 2D) = "grey" {}
        [NoScaleOffset] _UpTex2("Up 2 [+Y]", 2D) = "grey" {}
        [NoScaleOffset] _DownTex2("Down 2 [-Y]", 2D) = "grey" {}

        [Header(Color)]
        _Tint("Tint Color", Color) = (1, 1, 1, 1)
        [Gamma] _Exposure("Exposure", Range(0, 8)) = 1
        _Rotation("Rotation", Range(0, 360)) = 0

        [Header(Blending)]
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

        HLSLINCLUDE

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        struct Attributes
        {
            float4 positionOS : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 uv : TEXCOORD0;
        };

        CBUFFER_START(UnityPerMaterial)
            half4 _Tint;
            half _Exposure;
            float _Rotation;
            half _Blend;
        CBUFFER_END

        float3 RotateAroundYInDegrees(float3 vertex, float degrees)
        {
            float alpha = degrees * PI / 180.0;
            float sina;
            float cosa;
            sincos(alpha, sina, cosa);

            float2x2 m = float2x2(cosa, -sina, sina, cosa);
            return float3(mul(m, vertex.xz), vertex.y).xzy;
        }

        Varyings vert(Attributes input)
        {
            Varyings output;

            float3 rotated = RotateAroundYInDegrees(input.positionOS.xyz, _Rotation);
            output.positionCS = TransformObjectToHClip(rotated);
            output.uv = input.uv;

            return output;
        }

        half4 ApplySkyColor(half4 color)
        {
            color.rgb *= _Tint.rgb;
            color.rgb *= _Exposure;
            color.a = 1.0;
            return color;
        }

        ENDHLSL

        Pass
        {
            Name "Front"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            TEXTURE2D(_FrontTex1);
            SAMPLER(sampler_FrontTex1);
            TEXTURE2D(_FrontTex2);
            SAMPLER(sampler_FrontTex2);

            half4 frag(Varyings input) : SV_Target
            {
                half4 color1 = SAMPLE_TEXTURE2D(_FrontTex1, sampler_FrontTex1, input.uv);
                half4 color2 = SAMPLE_TEXTURE2D(_FrontTex2, sampler_FrontTex2, input.uv);
                return ApplySkyColor(lerp(color1, color2, saturate(_Blend)));
            }
            ENDHLSL
        }

        Pass
        {
            Name "Back"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            TEXTURE2D(_BackTex1);
            SAMPLER(sampler_BackTex1);
            TEXTURE2D(_BackTex2);
            SAMPLER(sampler_BackTex2);

            half4 frag(Varyings input) : SV_Target
            {
                half4 color1 = SAMPLE_TEXTURE2D(_BackTex1, sampler_BackTex1, input.uv);
                half4 color2 = SAMPLE_TEXTURE2D(_BackTex2, sampler_BackTex2, input.uv);
                return ApplySkyColor(lerp(color1, color2, saturate(_Blend)));
            }
            ENDHLSL
        }

        Pass
        {
            Name "Left"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            TEXTURE2D(_LeftTex1);
            SAMPLER(sampler_LeftTex1);
            TEXTURE2D(_LeftTex2);
            SAMPLER(sampler_LeftTex2);

            half4 frag(Varyings input) : SV_Target
            {
                half4 color1 = SAMPLE_TEXTURE2D(_LeftTex1, sampler_LeftTex1, input.uv);
                half4 color2 = SAMPLE_TEXTURE2D(_LeftTex2, sampler_LeftTex2, input.uv);
                return ApplySkyColor(lerp(color1, color2, saturate(_Blend)));
            }
            ENDHLSL
        }

        Pass
        {
            Name "Right"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            TEXTURE2D(_RightTex1);
            SAMPLER(sampler_RightTex1);
            TEXTURE2D(_RightTex2);
            SAMPLER(sampler_RightTex2);

            half4 frag(Varyings input) : SV_Target
            {
                half4 color1 = SAMPLE_TEXTURE2D(_RightTex1, sampler_RightTex1, input.uv);
                half4 color2 = SAMPLE_TEXTURE2D(_RightTex2, sampler_RightTex2, input.uv);
                return ApplySkyColor(lerp(color1, color2, saturate(_Blend)));
            }
            ENDHLSL
        }

        Pass
        {
            Name "Up"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            TEXTURE2D(_UpTex1);
            SAMPLER(sampler_UpTex1);
            TEXTURE2D(_UpTex2);
            SAMPLER(sampler_UpTex2);

            half4 frag(Varyings input) : SV_Target
            {
                half4 color1 = SAMPLE_TEXTURE2D(_UpTex1, sampler_UpTex1, input.uv);
                half4 color2 = SAMPLE_TEXTURE2D(_UpTex2, sampler_UpTex2, input.uv);
                return ApplySkyColor(lerp(color1, color2, saturate(_Blend)));
            }
            ENDHLSL
        }

        Pass
        {
            Name "Down"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            TEXTURE2D(_DownTex1);
            SAMPLER(sampler_DownTex1);
            TEXTURE2D(_DownTex2);
            SAMPLER(sampler_DownTex2);

            half4 frag(Varyings input) : SV_Target
            {
                half4 color1 = SAMPLE_TEXTURE2D(_DownTex1, sampler_DownTex1, input.uv);
                half4 color2 = SAMPLE_TEXTURE2D(_DownTex2, sampler_DownTex2, input.uv);
                return ApplySkyColor(lerp(color1, color2, saturate(_Blend)));
            }
            ENDHLSL
        }
    }

    FallBack Off
}