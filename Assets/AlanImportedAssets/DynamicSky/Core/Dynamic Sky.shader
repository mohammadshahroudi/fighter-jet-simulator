// Copyright (c) 2016 Unity Technologies. MIT license - license_unity.txt
// #NVJOB Dynamic Sky. MIT license - license_nvjob.txt
// #NVJOB Dynamic Sky v2.5.1 - https://nvjob.github.io/unity/nvjob-dynamic-sky-lite
// #NVJOB Nicholas Veselov - https://nvjob.github.io

/*
 * TO ANYONE READING THIS!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
 *
 * This file had to be modiffied because it just wouldn't work even after doing the whole render converter pipeline trick.
 * Some phrases or terms were replaced with newer ones for the specific version of Unity used for the project.
 * Some functions were straight up cut out because they used terms that were changed. Even if the terms were replaced
 * the whole function just wouldn't be working. I have never worked in a shader file before. At any point this project
 * can break. If it does, then hope the bug isn't easy to trigger in the demonstration.
 *
 * 4/18/2026 - 8:25 PM
 */


Shader "#NVJOB/Dynamic Sky"
{
Properties
{
    
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
[HideInInspector][NoScaleOffset]_Texture1("Texture 1", 2D) = "white" {}
[HideInInspector]_TextureUv1("Texture 1 Tiling", Float) = 1
[HideInInspector]_IntensityT1("Intensity", float) = 1.5
[HideInInspector]_VectorX1("Motion Vector X", float) = 0.9
[HideInInspector]_VectorY1("Motion Vector Y", float) = 1.0
[HideInInspector][NoScaleOffset]_Texture2("Texture 2", 2D) = "gray" {}
[HideInInspector]_TextureUv2("Texture 2 Tiling", Float) = 1
[HideInInspector]_IntensityT2("Intensity", float) = 1.5
[HideInInspector]_VectorX2("Motion Vector X", float) = 1.3
[HideInInspector]_VectorY2("Motion Vector Y", float) = 1.2
[HideInInspector][NoScaleOffset]_Texture3("Texture 3", 2D) = "gray" {}
[HideInInspector]_TextureUv3("Texture 3 Tiling", Float) = 1
[HideInInspector]_IntensityT3("Intensity", float) = -0.5
[HideInInspector]_VectorX3("Motion Vector X", float) = -1
[HideInInspector]_VectorY3("Motion Vector Y", float) = -1
[HideInInspector][HDR]_Color("Color", Color) = (1,1,1,1)
[HideInInspector]_IntensityInput("Intensity Input", float) = 1.6
[HideInInspector]_Fluffiness("Fluffiness", float) = 0.75
[HideInInspector]_IntensityOutput("Intensity Output", float) = 1
[HideInInspector][HDR]_Level1Color("Top Horizon Color", Color) = (0.65,0.86,0.63,1)
[HideInInspector]_Level1("Top Horizon Level", Float) = 10
[HideInInspector][HDR]_Level0Color("Bottom Horizon Color", Color) = (0.37,0.78,0.92,1)
[HideInInspector]_Level0("Bottom Horizon Level", Float) = 0
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


SubShader
{
Tags
{
"Queue"="Geometry+501"
"IgnoreProjector"="True"
"RenderType"="Transparent"
"RenderPipeline"="UniversalPipeline"
}

LOD 400

Pass
{
Name "Forward"
Tags { "LightMode"="UniversalForward" }

Blend SrcAlpha OneMinusSrcAlpha
ZWrite Off
Cull Back

HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma shader_feature_local DSKY_CLOUD_1 DSKY_CLOUD_2 DSKY_HORIZON

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D(_Texture1);
SAMPLER(sampler_Texture1);

TEXTURE2D(_Texture2);
SAMPLER(sampler_Texture2);

TEXTURE2D(_Texture3);
SAMPLER(sampler_Texture3);

CBUFFER_START(UnityPerMaterial)
half4 _Color;
float _TextureUv1;
float _VectorX1;
float _VectorY1;
float _IntensityT1;

float _TextureUv2;
float _VectorX2;
float _VectorY2;
float _IntensityT2;

float _TextureUv3;
float _VectorX3;
float _VectorY3;
float _IntensityT3;

float _IntensityInput;
float _IntensityOutput;
float _Fluffiness;

float _SkyShaderUvX;
float _SkyShaderUvZ;

float _Level0;
float _Level1;
half4 _Level0Color;
half4 _Level1Color;
CBUFFER_END

struct Attributes
{
float4 positionOS : POSITION;
float3 normalOS   : NORMAL;
float2 uv         : TEXCOORD0;
};

struct Varyings
{
float4 positionHCS : SV_POSITION;
float2 posTex      : TEXCOORD0;
float3 worldPos    : TEXCOORD1;
};

Varyings vert(Attributes IN)
{
Varyings OUT;

VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
OUT.positionHCS = posInputs.positionCS;
OUT.worldPos = TransformObjectToWorld(IN.positionOS.xyz);
OUT.posTex = IN.uv * 1.0 - 1.0;

return OUT;
}

half4 frag(Varyings IN) : SV_Target
{
float4 tex = 1.0;
float3 albedoEnd = float3(1.0, 1.0, 1.0);
float alphaEnd = 1.0;

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

float4 cloud = normalize((tex - 0.5) * _Fluffiness + 0.5);
cloud *= _IntensityOutput;

albedoEnd = cloud.rgb;
alphaEnd = tex.a;
#endif

#if defined(DSKY_CLOUD_2)
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

float4 cloud = normalize((tex - 0.5) * _Fluffiness + 0.5);
cloud *= _IntensityOutput;

albedoEnd = cloud.rgb;
alphaEnd = tex.a;
#endif

#if defined(DSKY_HORIZON)   
float pixelWpY = IN.worldPos.y;
float t = saturate((pixelWpY - _Level0) / max(_Level1 - _Level0, 0.0001));

if (pixelWpY < _Level0)
tex = _Level0Color;
else
tex = lerp(_Level0Color, _Level1Color, t);

albedoEnd = tex.rgb;
alphaEnd = tex.a;
#endif

return half4(albedoEnd, alphaEnd);
}

ENDHLSL
}
}

CustomEditor "NVDSkyMaterials"
}