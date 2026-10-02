Shader "MVP04/Star Mote"
{
    Properties
    {
        [HDR] _BaseColor ("Starlight", Color) = (2,2.5,3,1)
        _Opacity ("Opacity", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+5" "RenderType"="Transparent" }
        Blend SrcAlpha One ZWrite Off Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor; float _Opacity;
            CBUFFER_END
            struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; half4 colour:COLOR; };
            struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; half4 colour:COLOR; };
            V Vert(A input) { V output; output.positionCS=TransformObjectToHClip(input.positionOS.xyz); output.uv=input.uv; output.colour=input.colour; return output; }
            half4 Frag(V input):SV_Target
            {
                float2 p=input.uv*2-1;
                float core=exp(-dot(p,p)*24);
                float spikes=exp(-abs(p.x)*36)*pow(saturate(1-abs(p.y)),3)
                    +exp(-abs(p.y)*36)*pow(saturate(1-abs(p.x)),3);
                float star=saturate(core+spikes*.6);
                return half4(_BaseColor.rgb*input.colour.rgb,star*_Opacity*_BaseColor.a*input.colour.a);
            }
            ENDHLSL
        }
    }
}
