Shader "MVP02/Binary Surface"
{
    Properties
    {
        [HideInInspector] _BaseMap ("Base Map", 2D) = "white" {}
        [HideInInspector] _Cutoff ("Alpha Cutoff", Float) = 0.5
        [HideInInspector] _Cull ("Cull", Float) = 2
        _BaseColor ("Lit Color", Color) = (0.97, 0.97, 0.94, 1)
        _ShadeColor ("Side Color", Color) = (0.64, 0.64, 0.60, 1)
        _ShadowColor ("Cast Shadow Color", Color) = (0.10, 0.11, 0.11, 1)
        _Threshold ("Light Cutoff", Range(0, 1)) = 0.38
        _TransitionWidth ("Light Transition Width", Range(0.01, 0.2)) = 0.08
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            Name "BinaryForward"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _ShadeColor;
                half4 _ShadowColor;
                half _Threshold;
                half _TransitionWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                Light sun = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half orientation = saturate(dot(normalize(input.normalWS), sun.direction));
                half faceLight = smoothstep(_Threshold - _TransitionWidth,
                    _Threshold + _TransitionWidth, orientation);
                half shadowLight = smoothstep(0.15h, 0.85h, sun.shadowAttenuation);
                half3 faceColor = lerp(_ShadeColor.rgb, _BaseColor.rgb, faceLight);
                half castShadow = (1.0h - shadowLight) * smoothstep(0.06h, 0.25h, orientation);
                return half4(lerp(faceColor, _ShadowColor.rgb, castShadow), 1);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
    FallBack Off
}
