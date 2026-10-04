Shader "Hidden/MVP03/Puddle Coverage"
{
    SubShader
    {
        Pass
        {
            Name "Max union of textured stains"
            ZTest Always ZWrite Off Cull Off
            Blend One One
            BlendOp Max
            ColorMask R
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_StainMap); SAMPLER(sampler_StainMap);
            TEXTURE2D(_SupportMap); SAMPLER(sampler_SupportMap);
            float4 _Atlas, _Stamp, _Rotation;
            struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
            V Vert(A i)
            {
                V o;
                float2 local=i.positionOS.xz*_Stamp.zw;
                float2 world=_Stamp.xy+float2(_Rotation.x*local.x+_Rotation.y*local.y,-_Rotation.y*local.x+_Rotation.x*local.y);
                float2 clip=(world-_Atlas.xy)/_Atlas.zw*2-1;
                #if UNITY_UV_STARTS_AT_TOP
                    clip.y=-clip.y;
                #endif
                o.positionCS=float4(clip,0,1); o.uv=i.uv; return o;
            }
            half4 Frag(V i):SV_Target
            {
                half mask=SAMPLE_TEXTURE2D(_StainMap,sampler_StainMap,i.uv).r*
                    SAMPLE_TEXTURE2D(_SupportMap,sampler_SupportMap,i.uv).r;
                return half4(mask,0,0,1);
            }
            ENDHLSL
        }
    }
}
