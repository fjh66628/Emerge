Shader "MVP03/Wet Puddle"
{
    Properties
    {
        _WetTint("Water tint", Color) = (.035,.055,.06,1)
        _StainMap("Ground water stain mask", 2D) = "black" {}
        _ReflectionStrength("Fresnel reflection multiplier", Range(0,2)) = .8
        _ReflectionBlur("Reflection blur", Range(0,6)) = 1.3
        _SunHighlight("Sun highlight strength", Range(0,1)) = .3
        _WetDarkening("Wet stone darkening", Range(0,.6)) = .2
        _WaterThreshold("Water core threshold", Range(.15,.9)) = .5
        _EdgeSoftness("Wetness transition", Range(.02,.3)) = .12
        _RippleStrength("Subtle surface distortion", Range(0,.02)) = .002
        _Smoothness("Water smoothness", Range(.7,.99)) = .88
        [HideInInspector] _SupportMap("Ground support", 2D) = "white" {}
        [HideInInspector] _PlanarReflection("Planar reflection", 2D) = "black" {}
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
            #define _SPECULAR_SETUP 1
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_PlanarReflection); SAMPLER(sampler_PlanarReflection);
            TEXTURE2D(_SupportMap); SAMPLER(sampler_SupportMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _WetTint;
                float _ReflectionStrength, _ReflectionBlur, _SunHighlight, _WetDarkening, _WaterThreshold, _EdgeSoftness;
                float _RippleStrength, _Smoothness, _HasReflection;
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
            half4 Frag(V i):SV_Target
            {
                // Source water-stain stamps were max-unioned in world space before shading.
                float wetness=SAMPLE_TEXTURE2D(_SupportMap,sampler_SupportMap,i.uv).r;
                float edge=max(_EdgeSoftness,fwidth(wetness));
                float water=smoothstep(_WaterThreshold-edge,_WaterThreshold+edge,wetness);
                float damp=smoothstep(.025,.2+edge,wetness);
                clip(damp-.005);
                // Stationary shoreline and only a slight, slowly moving water normal.
                float2 wave=i.positionWS.xz*2.2+float2(_Time.y*.055,-_Time.y*.04);
                float2 slope=float2(Noise(wave+6),Noise(wave+24))*_RippleStrength;
                float3 normal=normalize(float3(slope.x,1,slope.y));
                float3 view=GetWorldSpaceNormalizeViewDir(i.positionWS);
                // Air/water IOR ~= 1.333 gives F0 ~= 0.02, rising toward grazing angles.
                float fresnel=.02+.98*pow(1-saturate(dot(normal,view)),5);
                float reflectionWeight=saturate(fresnel*_ReflectionStrength)*water;
                float4 projected=mul(_ReflectionVP,float4(i.positionWS,1));
                float2 uv=projected.xy/max(projected.w,.0001)*.5+.5;
                // GPU projection matrices use the render-texture platform convention.
                #if UNITY_UV_STARTS_AT_TOP
                    uv.y=1-uv.y;
                #endif
                uv+=slope*.3;
                float inside=step(0,uv.x)*step(uv.x,1)*step(0,uv.y)*step(uv.y,1)*step(.001,projected.w);
                float mip=_ReflectionBlur+(1-water)*2;
                float3 reflected=SAMPLE_TEXTURE2D_LOD(_PlanarReflection,sampler_PlanarReflection,saturate(uv),mip).rgb;
                reflected=lerp(SampleSH(normal),reflected,_HasReflection*inside);
                Light sun=GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                BRDFData brdf;
                half surfaceAlpha=1;
                InitializeBRDFData(half3(0,0,0),0,half3(.02,.02,.02),_Smoothness,surfaceAlpha,brdf);
                half3 highlight=LightingPhysicallyBased(brdf,sun,normal,view);
                float darkening=damp*_WetDarkening;
                float alpha=saturate(darkening+reflectionWeight*(1-darkening));
                float3 colour=min(reflected,8)*reflectionWeight+_WetTint.rgb*darkening*.22+
                    min(highlight,3)*water*_SunHighlight*_ReflectionStrength;
                return half4(colour,alpha);
            }
            ENDHLSL
        }
    }
}
