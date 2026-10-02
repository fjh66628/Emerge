Shader "MVP04/Baroque Sigil"
{
    Properties
    {
        _Pattern ("Baroque ornament", 2D) = "white" {}
        [HDR] _Radiance ("Gold light", Color) = (2,1.65,.9,1)
        _Reveal ("Radial reveal", Range(0,1)) = 0
        _Opacity ("Opacity", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+10" "RenderType"="Transparent" }
        Blend SrcAlpha One ZWrite Off Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_Pattern); SAMPLER(sampler_Pattern);
            CBUFFER_START(UnityPerMaterial)
                float4 _Radiance; float _Reveal, _Opacity;
            CBUFFER_END
            struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
            V Vert(A input) { V output; output.positionCS=TransformObjectToHClip(input.positionOS.xyz); output.uv=input.uv; return output; }
            half4 Frag(V input):SV_Target
            {
                half4 pattern=SAMPLE_TEXTURE2D(_Pattern,sampler_Pattern,input.uv);
                float radius=length(input.uv-.5)*2;
                float edge=_Reveal*1.12;
                float revealed=1-smoothstep(edge-.1,edge+.02,radius);
                float tracing=exp(-abs(radius-edge)*45)*(1-smoothstep(.9,1,_Reveal));
                // Reject the source image's faint transparent fringe; mipmaps retain fine filigree.
                float alpha=smoothstep(.06,.8,pattern.a)*revealed*_Opacity;
                return half4(pattern.rgb*_Radiance.rgb*(1+tracing*.4),alpha);
            }
            ENDHLSL
        }
    }
}
