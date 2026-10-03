Shader "MVP03/Wet Puddle"
{
    Properties
    {
        _WetTint("Water tint", Color) = (.035,.055,.06,1)
        _ReflectionStrength("Mirror reflection", Range(0,1)) = .48
        _WetDarkening("Wet stone darkening", Range(0,1)) = .32
        _EdgeNoise("Irregular shoreline", Range(0,1)) = .8
        _RippleStrength("Subtle surface distortion", Range(0,.04)) = .006
        _Smoothness("Water smoothness", Range(.7,1)) = .97
        [HideInInspector] _SupportMap("Ground support", 2D) = "white" {}
        [HideInInspector] _PlanarReflection("Planar reflection", 2D) = "black" {}
        [HideInInspector] _Seed("Shape seed", Float) = 0
        [HideInInspector] _HasReflection("Reflection ready", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-30" "RenderType"="Transparent" }
        Pass
        {
            Name "Wet surface"
            Tags { "LightMode"="UniversalForward" }
            Blend One OneMinusSrcAlpha
            ZWrite Off Cull Back
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_PlanarReflection); SAMPLER(sampler_PlanarReflection);
            TEXTURE2D(_SupportMap); SAMPLER(sampler_SupportMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _WetTint;
                float _ReflectionStrength, _WetDarkening, _EdgeNoise, _RippleStrength, _Smoothness, _Seed, _HasReflection;
                float4x4 _ReflectionVP;
            CBUFFER_END
            struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct V { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; float2 uv:TEXCOORD1; };
            V Vert(A i)
            {
                V o; VertexPositionInputs p=GetVertexPositionInputs(i.positionOS.xyz);
                o.positionCS=p.positionCS; o.positionWS=p.positionWS; o.uv=i.uv; return o;
            }
            float2 Gradient(float2 p)
            {
                float a=frac(sin(dot(p,float2(127.1,311.7)))*43758.5453)*6.283185;
                return float2(cos(a),sin(a));
            }
            float Noise(float2 p)
            {
                float2 cell=floor(p), f=frac(p), u=f*f*f*(f*(f*6-15)+10);
                return lerp(lerp(dot(Gradient(cell),f),dot(Gradient(cell+float2(1,0)),f-float2(1,0)),u.x),
                    lerp(dot(Gradient(cell+float2(0,1)),f-float2(0,1)),dot(Gradient(cell+1),f-1),u.x),u.y);
            }
            float Fractal(float2 p) { return Noise(p)*.58+Noise(p*2.07+7.8)*.28+Noise(p*4.13+19.2)*.14; }
            half4 Frag(V i):SV_Target
            {
                float2 p=i.uv*2-1;
                float2 seed=float2(_Seed,_Seed*.371);
                float2 warp=float2(Noise(p*2.1+seed),Noise(p*2.1+seed+38))*.13;
                float shoreline=.71-length(p+warp)+Fractal(p*3.2+seed)*_EdgeNoise*.48;
                float aa=max(fwidth(shoreline)*1.2,.006);
                float support=SAMPLE_TEXTURE2D(_SupportMap,sampler_SupportMap,i.uv).r;
                float water=smoothstep(-aa,aa,shoreline)*support;
                float damp=smoothstep(-.13,-.015,shoreline)*support;
                clip(damp-.005);
                // Stationary shoreline and only a slight, slowly moving water normal.
                float2 wave=i.positionWS.xz*2.2+float2(_Time.y*.055,-_Time.y*.04);
                float2 slope=float2(Noise(wave+6),Noise(wave+24))*_RippleStrength;
                float3 normal=normalize(float3(slope.x,1,slope.y));
                float3 view=GetWorldSpaceNormalizeViewDir(i.positionWS);
                float grazing=pow(1-saturate(dot(normal,view)),3);
                float reflectionWeight=lerp(_ReflectionStrength,.92,grazing)*water;
                float4 projected=mul(_ReflectionVP,float4(i.positionWS,1));
                float2 uv=projected.xy/max(projected.w,.0001)*.5+.5;
                // GPU projection matrices use the render-texture platform convention.
                #if UNITY_UV_STARTS_AT_TOP
                    uv.y=1-uv.y;
                #endif
                uv+=slope*.3;
                float inside=step(0,uv.x)*step(uv.x,1)*step(0,uv.y)*step(uv.y,1)*step(.001,projected.w);
                float3 reflected=SAMPLE_TEXTURE2D_LOD(_PlanarReflection,sampler_PlanarReflection,saturate(uv),.6).rgb;
                reflected=lerp(SampleSH(normal),reflected,_HasReflection*inside);
                Light sun=GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                BRDFData brdf;
                half surfaceAlpha=1;
                InitializeBRDFData(half3(0,0,0),0,half3(.02,.02,.02),_Smoothness,surfaceAlpha,brdf);
                half3 highlight=LightingPhysicallyBased(brdf,sun,normal,view);
                float darkening=damp*_WetDarkening;
                float alpha=saturate(darkening+reflectionWeight*(1-darkening));
                float3 colour=min(reflected,8)*reflectionWeight+_WetTint.rgb*darkening*.22+min(highlight,5)*water;
                return half4(colour,alpha);
            }
            ENDHLSL
        }
    }
}
