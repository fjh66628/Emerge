Shader "MVP03/Ground Fire"
{
    Properties
    {
        _StainMap("Ground footprint", 2D) = "black" {}
        _FlameHeight("Flame height (m)", Range(.2,1.8)) = .55
        _FlameWidth("Flame width (m)", Range(.2,.9)) = .44
        _ParticlePivot("Particle pivot (local XYZ)", Vector) = (0,-.42,0,0)
        _FlameDensity("Flame density", Range(.2,1)) = .82
        _FlameSpeed("Rise speed", Range(.2,3)) = 1.2
        _AnimationFPS("Flipbook frames per second", Range(4,32)) = 16
        _Turbulence("Turbulence intensity", Range(0,4)) = 1.1
        _TurbulenceFrequency("Turbulence frequency", Range(.2,4)) = 1.6
        _Emission("Flame brightness", Range(.3,4)) = 1.65
        _GroundGlow("Ember glow", Range(0,1)) = .38
        _ScorchStrength("Scorched ground darkness", Range(0,1)) = .78
        _ScorchScale("Scorch grain scale", Range(1,12)) = 6
        _FireThreshold("Footprint threshold", Range(.1,.8)) = .32
        _EdgeSoftness("Footprint edge", Range(.02,.3)) = .14
        _LightIntensity("Local light intensity", Range(0,6)) = 2.8
        _LightRange("Local light range (m)", Range(1,6)) = 3.6
        [HideInInspector] _SupportMap("Merged footprint", 2D) = "black" {}
        [HideInInspector] _SupportAtlas("World XZ bounds", Vector) = (0,0,1,1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-20" "RenderType"="Transparent" }
        Pass
        {
            Name "Scorched ground and embers below VFX particles"
            Tags { "LightMode"="UniversalForward" }
            Blend One OneMinusSrcAlpha
            ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_SupportMap); SAMPLER(sampler_SupportMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _SupportAtlas;
                float _FlameHeight, _FlameWidth, _FlameDensity, _FlameSpeed, _Emission, _GroundGlow;
                float _FireThreshold, _EdgeSoftness, _LightIntensity, _LightRange, _Turbulence, _TurbulenceFrequency;
                float _ScorchStrength, _ScorchScale;
                float _AnimationFPS;
                float4 _ParticlePivot;
            CBUFFER_END
            struct A { float4 positionOS:POSITION; };
            struct V { float4 positionCS:SV_POSITION; float3 world:TEXCOORD0; };
            float Hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float Noise(float2 p)
            {
                float2 c=floor(p), f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(Hash(c),Hash(c+float2(1,0)),f.x),lerp(Hash(c+float2(0,1)),Hash(c+1),f.x),f.y);
            }
            float Footprint(float2 world)
            {
                float2 uv=(world-_SupportAtlas.xy)/_SupportAtlas.zw;
                float raw=SAMPLE_TEXTURE2D_LOD(_SupportMap,sampler_SupportMap,uv,0).r;
                return smoothstep(_FireThreshold-_EdgeSoftness,_FireThreshold+_EdgeSoftness,raw);
            }
            V Vert(A i)
            {
                V o; o.world=TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS=TransformWorldToHClip(o.world); return o;
            }
            half4 Frag(V i):SV_Target
            {
                float time=_Time.y*_FlameSpeed;
                float mask=Footprint(i.world.xz); clip(mask-.015);
                // Static charcoal grain preserves the masonry below through premultiplied blending.
                // Only the sparse embers flicker; the burned footprint never swims with the flame.
                float2 p=i.world.xz*_ScorchScale;
                float grain=Noise(p)*.6+Noise(p*2.13+7.1)*.28+Noise(p*4.31)*.12;
                float a=mask*_ScorchStrength*lerp(.7,1,grain);
                float3 ash=lerp(float3(.004,.0025,.0015),float3(.018,.014,.01),grain);
                float ridges=1-smoothstep(.025,.09,abs(Noise(p*.63)-.5));
                float embers=ridges*smoothstep(.55,.78,Noise(p*1.73+9.4));
                float flicker=.6+.4*Noise(i.world.xz*2-float2(0,time*.65));
                float3 glow=float3(1,.12,.008)*embers*mask*_GroundGlow*_Emission*flicker;
                return half4(ash*a+glow,a);
            }
            ENDHLSL
        }
    }
}
