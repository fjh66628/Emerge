Shader "MVP03/Magic Glow"
{
    Properties
    {
        [HDR] _BaseColor ("Radiance", Color) = (0.3, 2, 3, 1)
        _Opacity ("Opacity", Range(0,1)) = 1
        _RimWeight ("Fresnel Weight", Range(0,1)) = 1
        _RimPower ("Fresnel Power", Range(.1,8)) = 2.5
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Name "MagicGlow"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha One
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _Opacity, _RimWeight, _RimPower;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; half3 normalWS : TEXCOORD1; half4 color : COLOR; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.color = input.color;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half facing = saturate(dot(normalize(input.normalWS), GetWorldSpaceNormalizeViewDir(input.positionWS)));
                half rim = lerp(1.0h, .08h + .92h * pow(1.0h - facing, _RimPower), _RimWeight);
                return half4(_BaseColor.rgb * input.color.rgb, _BaseColor.a * input.color.a * _Opacity * rim);
            }
            ENDHLSL
        }
    }
}
