Shader "MVP03/Pixel Cutout"
{
    Properties
    {
        _BaseMap("Sprite",2D)="white"{}
        _BaseColor("Tint",Color)=(1,1,1,1)
        _Cutoff("Alpha cutoff",Float)=.5
        _Cull("Cull",Float)=0
    }
    SubShader
    {
        Tags {"RenderType"="TransparentCutout" "Queue"="AlphaTest" "RenderPipeline"="UniversalPipeline"}
        Cull Off ZWrite On
        Pass
        {
            Name "PixelColor"
            Tags {"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;float4 _BaseMap_ST;float _Cutoff;float _Cull;
            CBUFFER_END
            struct A{float4 positionOS:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
            struct V{float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;float3 positionWS:TEXCOORD1;float4 color:COLOR;};
            V Vert(A i){V o;VertexPositionInputs p=GetVertexPositionInputs(i.positionOS.xyz);o.positionCS=p.positionCS;o.positionWS=p.positionWS;o.uv=i.uv;o.color=i.color;return o;}
            half4 Frag(V i):SV_Target
            {
                half4 c=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv)*_BaseColor*i.color;
                clip(c.a-_Cutoff);
                Light sun=GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                c.rgb*=.85h+.15h*sun.shadowAttenuation;
                return half4(c.rgb,1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Unlit/DepthOnly"
        UsePass "Universal Render Pipeline/Unlit/DepthNormalsOnly"
    }
}
