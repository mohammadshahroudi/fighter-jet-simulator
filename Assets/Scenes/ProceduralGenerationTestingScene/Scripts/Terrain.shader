Shader "Custom/Terrain8LayerURP"
{
    Properties
    {
        _LayerTex0("Layer Tex 0", 2D) = "white" {}
        _LayerTex1("Layer Tex 1", 2D) = "white" {}
        _LayerTex2("Layer Tex 2", 2D) = "white" {}
        _LayerTex3("Layer Tex 3", 2D) = "white" {}
        _LayerTex4("Layer Tex 4", 2D) = "white" {}
        _LayerTex5("Layer Tex 5", 2D) = "white" {}
        _LayerTex6("Layer Tex 6", 2D) = "white" {}
        _LayerTex7("Layer Tex 7", 2D) = "white" {}

        _Color0("Color 0", Color) = (1,1,1,1)
        _Color1("Color 1", Color) = (1,1,1,1)
        _Color2("Color 2", Color) = (1,1,1,1)
        _Color3("Color 3", Color) = (1,1,1,1)
        _Color4("Color 4", Color) = (1,1,1,1)
        _Color5("Color 5", Color) = (1,1,1,1)
        _Color6("Color 6", Color) = (1,1,1,1)
        _Color7("Color 7", Color) = (1,1,1,1)

        _ColorStrength0("Color Strength 0", Range(0,1)) = 0
        _ColorStrength1("Color Strength 1", Range(0,1)) = 0
        _ColorStrength2("Color Strength 2", Range(0,1)) = 0
        _ColorStrength3("Color Strength 3", Range(0,1)) = 0
        _ColorStrength4("Color Strength 4", Range(0,1)) = 0
        _ColorStrength5("Color Strength 5", Range(0,1)) = 0
        _ColorStrength6("Color Strength 6", Range(0,1)) = 0
        _ColorStrength7("Color Strength 7", Range(0,1)) = 0

        _StartHeight0("Start Height 0", Range(0,1)) = 0
        _StartHeight1("Start Height 1", Range(0,1)) = 0.15
        _StartHeight2("Start Height 2", Range(0,1)) = 0.3
        _StartHeight3("Start Height 3", Range(0,1)) = 0.45
        _StartHeight4("Start Height 4", Range(0,1)) = 0.6
        _StartHeight5("Start Height 5", Range(0,1)) = 0.75
        _StartHeight6("Start Height 6", Range(0,1)) = 0.9
        _StartHeight7("Start Height 7", Range(0,1)) = 1

        _Blend0("Blend 0", Range(0.001,1)) = 0.1
        _Blend1("Blend 1", Range(0.001,1)) = 0.1
        _Blend2("Blend 2", Range(0.001,1)) = 0.1
        _Blend3("Blend 3", Range(0.001,1)) = 0.1
        _Blend4("Blend 4", Range(0.001,1)) = 0.1
        _Blend5("Blend 5", Range(0.001,1)) = 0.1
        _Blend6("Blend 6", Range(0.001,1)) = 0.1
        _Blend7("Blend 7", Range(0.001,1)) = 0.1

        _TextureScale0("Texture Scale 0", Float) = 20
        _TextureScale1("Texture Scale 1", Float) = 20
        _TextureScale2("Texture Scale 2", Float) = 20
        _TextureScale3("Texture Scale 3", Float) = 20
        _TextureScale4("Texture Scale 4", Float) = 20
        _TextureScale5("Texture Scale 5", Float) = 20
        _TextureScale6("Texture Scale 6", Float) = 20
        _TextureScale7("Texture Scale 7", Float) = 20

        _LayerCount("Layer Count", Int) = 0
        _MinHeight("Min Height", Float) = 0
        _MaxHeight("Max Height", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "Queue"="Geometry"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode"="SRPDefaultUnlit" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            static const int MAX_LAYERS = 8;
            static const float EPSILON = 1e-4;

            TEXTURE2D(_LayerTex0); SAMPLER(sampler_LayerTex0);
            TEXTURE2D(_LayerTex1); SAMPLER(sampler_LayerTex1);
            TEXTURE2D(_LayerTex2); SAMPLER(sampler_LayerTex2);
            TEXTURE2D(_LayerTex3); SAMPLER(sampler_LayerTex3);
            TEXTURE2D(_LayerTex4); SAMPLER(sampler_LayerTex4);
            TEXTURE2D(_LayerTex5); SAMPLER(sampler_LayerTex5);
            TEXTURE2D(_LayerTex6); SAMPLER(sampler_LayerTex6);
            TEXTURE2D(_LayerTex7); SAMPLER(sampler_LayerTex7);

            CBUFFER_START(UnityPerMaterial)
                float4 _Color0, _Color1, _Color2, _Color3, _Color4, _Color5, _Color6, _Color7;
                float _ColorStrength0, _ColorStrength1, _ColorStrength2, _ColorStrength3, _ColorStrength4, _ColorStrength5, _ColorStrength6, _ColorStrength7;
                float _StartHeight0, _StartHeight1, _StartHeight2, _StartHeight3, _StartHeight4, _StartHeight5, _StartHeight6, _StartHeight7;
                float _Blend0, _Blend1, _Blend2, _Blend3, _Blend4, _Blend5, _Blend6, _Blend7;
                float _TextureScale0, _TextureScale1, _TextureScale2, _TextureScale3, _TextureScale4, _TextureScale5, _TextureScale6, _TextureScale7;
                int _LayerCount;
                float _MinHeight;
                float _MaxHeight;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float3 worldPos    : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = posInputs.positionCS;
                OUT.worldPos = posInputs.positionWS;
                OUT.worldNormal = normalize(normalInputs.normalWS);
                return OUT;
            }

            float inverseLerp(float a, float b, float value)
            {
                return saturate((value - a) / max(b - a, 1e-6));
            }

            float3 getBlendAxes(float3 worldNormal)
            {
                float3 blendAxes = abs(worldNormal);
                float sum = max(blendAxes.x + blendAxes.y + blendAxes.z, 1e-6);
                return blendAxes / sum;
            }

            float4 GetLayerColor(int i)
            {
                if (i == 0) return _Color0;
                if (i == 1) return _Color1;
                if (i == 2) return _Color2;
                if (i == 3) return _Color3;
                if (i == 4) return _Color4;
                if (i == 5) return _Color5;
                if (i == 6) return _Color6;
                return _Color7;
            }

            float GetLayerColorStrength(int i)
            {
                if (i == 0) return _ColorStrength0;
                if (i == 1) return _ColorStrength1;
                if (i == 2) return _ColorStrength2;
                if (i == 3) return _ColorStrength3;
                if (i == 4) return _ColorStrength4;
                if (i == 5) return _ColorStrength5;
                if (i == 6) return _ColorStrength6;
                return _ColorStrength7;
            }

            float GetLayerStartHeight(int i)
            {
                if (i == 0) return _StartHeight0;
                if (i == 1) return _StartHeight1;
                if (i == 2) return _StartHeight2;
                if (i == 3) return _StartHeight3;
                if (i == 4) return _StartHeight4;
                if (i == 5) return _StartHeight5;
                if (i == 6) return _StartHeight6;
                return _StartHeight7;
            }

            float GetLayerBlend(int i)
            {
                if (i == 0) return _Blend0;
                if (i == 1) return _Blend1;
                if (i == 2) return _Blend2;
                if (i == 3) return _Blend3;
                if (i == 4) return _Blend4;
                if (i == 5) return _Blend5;
                if (i == 6) return _Blend6;
                return _Blend7;
            }

            float GetLayerScale(int i)
            {
                if (i == 0) return _TextureScale0;
                if (i == 1) return _TextureScale1;
                if (i == 2) return _TextureScale2;
                if (i == 3) return _TextureScale3;
                if (i == 4) return _TextureScale4;
                if (i == 5) return _TextureScale5;
                if (i == 6) return _TextureScale6;
                return _TextureScale7;
            }

            float3 SampleLayerTexture(int i, float3 worldPos, float3 blendAxes)
            {
                float scale = max(GetLayerScale(i), 1e-6);
                float3 scaled = worldPos / scale;

                if (i == 0)
                    return SAMPLE_TEXTURE2D(_LayerTex0, sampler_LayerTex0, scaled.yz).rgb * blendAxes.x
                         + SAMPLE_TEXTURE2D(_LayerTex0, sampler_LayerTex0, scaled.xz).rgb * blendAxes.y
                         + SAMPLE_TEXTURE2D(_LayerTex0, sampler_LayerTex0, scaled.xy).rgb * blendAxes.z;

                if (i == 1)
                    return SAMPLE_TEXTURE2D(_LayerTex1, sampler_LayerTex1, scaled.yz).rgb * blendAxes.x
                         + SAMPLE_TEXTURE2D(_LayerTex1, sampler_LayerTex1, scaled.xz).rgb * blendAxes.y
                         + SAMPLE_TEXTURE2D(_LayerTex1, sampler_LayerTex1, scaled.xy).rgb * blendAxes.z;

                if (i == 2)
                    return SAMPLE_TEXTURE2D(_LayerTex2, sampler_LayerTex2, scaled.yz).rgb * blendAxes.x
                         + SAMPLE_TEXTURE2D(_LayerTex2, sampler_LayerTex2, scaled.xz).rgb * blendAxes.y
                         + SAMPLE_TEXTURE2D(_LayerTex2, sampler_LayerTex2, scaled.xy).rgb * blendAxes.z;

                if (i == 3)
                    return SAMPLE_TEXTURE2D(_LayerTex3, sampler_LayerTex3, scaled.yz).rgb * blendAxes.x
                         + SAMPLE_TEXTURE2D(_LayerTex3, sampler_LayerTex3, scaled.xz).rgb * blendAxes.y
                         + SAMPLE_TEXTURE2D(_LayerTex3, sampler_LayerTex3, scaled.xy).rgb * blendAxes.z;

                if (i == 4)
                    return SAMPLE_TEXTURE2D(_LayerTex4, sampler_LayerTex4, scaled.yz).rgb * blendAxes.x
                         + SAMPLE_TEXTURE2D(_LayerTex4, sampler_LayerTex4, scaled.xz).rgb * blendAxes.y
                         + SAMPLE_TEXTURE2D(_LayerTex4, sampler_LayerTex4, scaled.xy).rgb * blendAxes.z;

                if (i == 5)
                    return SAMPLE_TEXTURE2D(_LayerTex5, sampler_LayerTex5, scaled.yz).rgb * blendAxes.x
                         + SAMPLE_TEXTURE2D(_LayerTex5, sampler_LayerTex5, scaled.xz).rgb * blendAxes.y
                         + SAMPLE_TEXTURE2D(_LayerTex5, sampler_LayerTex5, scaled.xy).rgb * blendAxes.z;

                if (i == 6)
                    return SAMPLE_TEXTURE2D(_LayerTex6, sampler_LayerTex6, scaled.yz).rgb * blendAxes.x
                         + SAMPLE_TEXTURE2D(_LayerTex6, sampler_LayerTex6, scaled.xz).rgb * blendAxes.y
                         + SAMPLE_TEXTURE2D(_LayerTex6, sampler_LayerTex6, scaled.xy).rgb * blendAxes.z;

                return SAMPLE_TEXTURE2D(_LayerTex7, sampler_LayerTex7, scaled.yz).rgb * blendAxes.x
                     + SAMPLE_TEXTURE2D(_LayerTex7, sampler_LayerTex7, scaled.xz).rgb * blendAxes.y
                     + SAMPLE_TEXTURE2D(_LayerTex7, sampler_LayerTex7, scaled.xy).rgb * blendAxes.z;
            }

            float LayerDrawStrength(float heightPercent, float startHeight, float blendStrength)
            {
                return inverseLerp(
                    -blendStrength * 0.5 - EPSILON,
                     blendStrength * 0.5,
                     heightPercent - startHeight
                );
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float heightPercent = inverseLerp(_MinHeight, _MaxHeight, IN.worldPos.y);
                float3 blendAxes = getBlendAxes(normalize(IN.worldNormal));

                float3 albedo = 0;

                [loop]
                for (int i = 0; i < _LayerCount && i < MAX_LAYERS; i++)
                {
                    float4 tintColor = GetLayerColor(i);
                    float colorStrength = GetLayerColorStrength(i);
                    float startHeight = GetLayerStartHeight(i);
                    float blendStrength = GetLayerBlend(i);

                    float drawStrength = LayerDrawStrength(heightPercent, startHeight, blendStrength);
                    float3 tint = tintColor.rgb * colorStrength;
                    float3 tex = SampleLayerTexture(i, IN.worldPos, blendAxes) * (1 - colorStrength);

                    albedo = lerp(albedo, tint + tex, drawStrength);
                }

                return half4(albedo, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}