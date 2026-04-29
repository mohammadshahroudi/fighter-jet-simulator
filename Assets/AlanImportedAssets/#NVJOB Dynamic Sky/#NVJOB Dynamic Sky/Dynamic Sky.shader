// Copyright (c) 2016 Unity Technologies. MIT license - license_unity.txt
// #NVJOB Dynamic Sky. MIT license - license_nvjob.txt
// #NVJOB Dynamic Sky v2.5.1 - https://nvjob.github.io/unity/nvjob-dynamic-sky-lite
// #NVJOB Nicholas Veselov - https://nvjob.github.io


// Converted for URP from the original Built-in Surface Shader version.
// Original asset: #NVJOB Dynamic Sky
Shader "#NVJOB/Dynamic Sky"
{
    Properties
    {
        [HideInInspector][NoScaleOffset]_Texture1("Texture 1", 2D) = "white" {}
        [HideInInspector]_TextureUv1("Texture 1 Tiling", Float) = 1
        [HideInInspector]_IntensityT1("Intensity", Float) = 1.5
        [HideInInspector]_VectorX1("Motion Vector X", Float) = 0.9
        [HideInInspector]_VectorY1("Motion Vector Y", Float) = 1.0

        [HideInInspector][NoScaleOffset]_Texture2("Texture 2", 2D) = "gray" {}
        [HideInInspector]_TextureUv2("Texture 2 Tiling", Float) = 1
        [HideInInspector]_IntensityT2("Intensity", Float) = 1.5
        [HideInInspector]_VectorX2("Motion Vector X", Float) = 1.3
        [HideInInspector]_VectorY2("Motion Vector Y", Float) = 1.2

        [HideInInspector][NoScaleOffset]_Texture3("Texture 3", 2D) = "gray" {}
        [HideInInspector]_TextureUv3("Texture 3 Tiling", Float) = 1
        [HideInInspector]_IntensityT3("Intensity", Float) = -0.5
        [HideInInspector]_VectorX3("Motion Vector X", Float) = -1
        [HideInInspector]_VectorY3("Motion Vector Y", Float) = -1

        [HideInInspector][HDR]_Color("Color", Color) = (1,1,1,1)
        [HideInInspector]_IntensityInput("Intensity Input", Float) = 1.6
        [HideInInspector]_Fluffiness("Fluffiness", Float) = 0.75
        [HideInInspector]_IntensityOutput("Intensity Output", Float) = 1

        [HideInInspector][HDR]_Level1Color("Top Horizon Color", Color) = (0.65,0.86,0.63,1)
        [HideInInspector]_Level1("Top Horizon Level", Float) = 10
        [HideInInspector][HDR]_Level0Color("Bottom Horizon Color", Color) = (0.37,0.78,0.92,1)
        [HideInInspector]_Level0("Bottom Horizon Level", Float) = 0

        // These were referenced by the original shader code at runtime.
        [HideInInspector]_SkyShaderUvX("Sky Shader UV X", Float) = 0
        [HideInInspector]_SkyShaderUvZ("Sky Shader UV Z", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Geometry+501"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForwardOnly" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local_fragment _ DSKY_CLOUD_1 DSKY_CLOUD_2 DSKY_HORIZON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 posTex : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            TEXTURE2D(_Texture1);
            SAMPLER(sampler_Texture1);
            TEXTURE2D(_Texture2);
            SAMPLER(sampler_Texture2);
            TEXTURE2D(_Texture3);
            SAMPLER(sampler_Texture3);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _Level0Color;
                half4 _Level1Color;

                float _TextureUv1;
                float _IntensityT1;
                float _VectorX1;
                float _VectorY1;

                float _TextureUv2;
                float _IntensityT2;
                float _VectorX2;
                float _VectorY2;

                float _TextureUv3;
                float _IntensityT3;
                float _VectorX3;
                float _VectorY3;

                float _IntensityInput;
                float _Fluffiness;
                float _IntensityOutput;

                float _Level1;
                float _Level0;

                float _SkyShaderUvX;
                float _SkyShaderUvZ;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.posTex = IN.uv - 1.0;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float4 tex = 0;
                float4 colorOut = float4(0, 0, 0, 0);

                #if defined(DSKY_CLOUD_1)
                    tex = _Color;

                    float2 uv = IN.posTex;
                    uv += float2(_SkyShaderUvX * _VectorX1, _SkyShaderUvZ * _VectorY1);
                    tex *= SAMPLE_TEXTURE2D(_Texture1, sampler_Texture1, uv * _TextureUv1) * _IntensityT1;

                    float2 uvd = IN.posTex;
                    uvd += float2(_SkyShaderUvX * _VectorX2, _SkyShaderUvZ * _VectorY2);
                    tex *= SAMPLE_TEXTURE2D(_Texture2, sampler_Texture2, uvd * _TextureUv2).r * _IntensityT2;

                    float2 uvdd = IN.posTex;
                    uvdd += float2(_SkyShaderUvX * _VectorX3, _SkyShaderUvZ * _VectorY3);
                    tex *= SAMPLE_TEXTURE2D(_Texture3, sampler_Texture3, uvdd * _TextureUv3).r * _IntensityT3;

                    tex *= _IntensityInput;
                    colorOut = normalize((tex - 0.5) * _Fluffiness + 0.5);
                    colorOut *= _IntensityOutput;
                    colorOut.a = tex.a;
                #elif defined(DSKY_CLOUD_2)
                    tex = _Color;

                    float2 uv = IN.posTex;
                    uv += float2(_SkyShaderUvX * _VectorX1, _SkyShaderUvZ * _VectorY1);
                    tex *= SAMPLE_TEXTURE2D(_Texture1, sampler_Texture1, uv * _TextureUv1).r * _IntensityT1;

                    float2 uvd = IN.posTex;
                    uvd += float2(_SkyShaderUvX * _VectorX2, _SkyShaderUvZ * _VectorY2);
                    tex *= SAMPLE_TEXTURE2D(_Texture2, sampler_Texture2, uvd * _TextureUv2).g * _IntensityT2;

                    float2 uvdd = IN.posTex;
                    uvdd += float2(_SkyShaderUvX * _VectorX3, _SkyShaderUvZ * _VectorY3);
                    tex *= SAMPLE_TEXTURE2D(_Texture3, sampler_Texture3, uvdd * _TextureUv3).b * _IntensityT3;

                    tex *= _IntensityInput;
                    colorOut = normalize((tex - 0.5) * _Fluffiness + 0.5);
                    colorOut *= _IntensityOutput;
                    colorOut.a = tex.a;
                #elif defined(DSKY_HORIZON)
                    float pixelWpY = IN.worldPos.y;
                    if (pixelWpY >= _Level1)
                    {
                        tex = lerp(_Level0Color, _Level1Color, (pixelWpY - _Level0) / max(_Level1 - _Level0, 0.0001));
                    }
                    else if (pixelWpY < _Level0)
                    {
                        tex = _Level0Color;
                    }
                    else
                    {
                        tex = lerp(_Level0Color, _Level1Color, (pixelWpY - _Level0) / max(_Level1 - _Level0, 0.0001));
                    }

                    colorOut = tex;
                #endif

                return half4(colorOut.rgb, colorOut.a);
            }
            ENDHLSL
        }
    }

    FallBack Off
    CustomEditor "NVDSkyMaterials"
}