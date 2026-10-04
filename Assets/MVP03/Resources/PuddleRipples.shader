Shader "Hidden/MVP03/Puddle Ripples"
{
    SubShader
    {
        HLSLINCLUDE
        #pragma target 3.5
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        TEXTURE2D(_State); SAMPLER(sampler_State);
        TEXTURE2D(_Coverage); SAMPLER(sampler_Coverage);
        float4 _Atlas, _Texel, _Wave, _Wetness, _Impact;
        struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
        struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float2 local:TEXCOORD1; };
        float4 Clip(float2 uv)
        {
            float2 p=uv*2-1;
            #if UNITY_UV_STARTS_AT_TOP
                p.y=-p.y;
            #endif
            return float4(p,0,1);
        }
        V FullVert(A i) { V o; o.positionCS=Clip(i.uv); o.uv=i.uv; o.local=0; return o; }
        V ImpactVert(A i)
        {
            V o; o.local=i.positionOS.xz;
            o.uv=(_Impact.xy+o.local*_Impact.z-_Atlas.xy)/_Atlas.zw;
            o.positionCS=Clip(o.uv); return o;
        }
        float Wet(float2 uv)
        {
            float mask=SAMPLE_TEXTURE2D_LOD(_Coverage,sampler_Coverage,uv,0).r;
            return smoothstep(_Wetness.x-_Wetness.y,_Wetness.x+_Wetness.y,mask);
        }
        half4 Integrate(V i):SV_Target
        {
            float2 state=SAMPLE_TEXTURE2D_LOD(_State,sampler_State,i.uv,0).rg;
            float left=SAMPLE_TEXTURE2D_LOD(_State,sampler_State,i.uv-float2(_Texel.x,0),0).r;
            float right=SAMPLE_TEXTURE2D_LOD(_State,sampler_State,i.uv+float2(_Texel.x,0),0).r;
            float down=SAMPLE_TEXTURE2D_LOD(_State,sampler_State,i.uv-float2(0,_Texel.y),0).r;
            float up=SAMPLE_TEXTURE2D_LOD(_State,sampler_State,i.uv+float2(0,_Texel.y),0).r;
            float wet=Wet(i.uv), shore=smoothstep(.05,.65,wet);
            float velocity=(state.r-state.g)*lerp(.55,_Wave.z,shore);
            float next=(state.r+velocity+_Wave.x*(left+right-2*state.r)+_Wave.y*(down+up-2*state.r))*lerp(.65,_Wave.w,shore);
            // Absorbing shoreline. Dry holes and different stair levels never share waves.
            float valid=step(.015,wet);
            return half4(clamp(next,-.06,.06)*valid,state.r*valid,0,0);
        }
        half4 Impulse(V i):SV_Target
        {
            float r2=dot(i.local,i.local);
            float gaussian=(exp(-r2*5)-exp(-5.0))*step(r2,1);
            return half4(_Impact.w*gaussian*Wet(i.uv),0,0,0);
        }
        ENDHLSL
        Pass
        {
            Name "Damped shallow water height"
            ZTest Always ZWrite Off Cull Off Blend Off
            HLSLPROGRAM
            #pragma vertex FullVert
            #pragma fragment Integrate
            ENDHLSL
        }
        Pass
        {
            Name "Contact displacement"
            ZTest Always ZWrite Off Cull Off
            Blend One One
            ColorMask R
            HLSLPROGRAM
            #pragma vertex ImpactVert
            #pragma fragment Impulse
            ENDHLSL
        }
    }
}
