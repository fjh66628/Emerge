Shader "MVP04/Astral Trail"
{
    Properties
    {
        [HDR] _BaseColor ("Nebula tint", Color) = (.65,.7,1.6,.32)
        _Opacity ("Opacity", Range(0,1)) = .65
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
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
            float Hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float Noise(float2 p)
            {
                float2 cell=floor(p),f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(Hash(cell),Hash(cell+float2(1,0)),f.x),lerp(Hash(cell+float2(0,1)),Hash(cell+1),f.x),f.y);
            }
            half4 Frag(V input):SV_Target
            {
                float side=abs(input.uv.y*2-1);
                float softness=pow(saturate(1-side),2);
                float cloud=Noise(input.uv*float2(17,3)+float2(-_Time.y*.2,0));
                half3 colour=lerp(half3(.42,.58,1),half3(.75,.4,1),cloud)*_BaseColor.rgb*input.colour.rgb;
                return half4(colour,softness*(.3+.7*cloud)*_Opacity*_BaseColor.a*input.colour.a);
            }
            ENDHLSL
        }
    }
}
