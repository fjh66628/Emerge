Shader "MVP02/Cut Paper Sky"
{
    Properties
    {
        _UpperColor ("Upper White", Color) = (0.975, 0.971, 0.945, 1)
        _MiddleColor ("Middle White", Color) = (0.947, 0.946, 0.920, 1)
        _HorizonColor ("Horizon Cream", Color) = (0.902, 0.909, 0.864, 1)
        _LowerColor ("Lower White", Color) = (0.976, 0.974, 0.949, 1)
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _UpperColor;
                half4 _MiddleColor;
                half4 _HorizonColor;
                half4 _LowerColor;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 directionWS : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.directionWS = TransformObjectToWorldDir(input.positionOS.xyz);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half height = normalize(input.directionWS).y;
                half3 color = lerp(_LowerColor.rgb, _HorizonColor.rgb, step(-0.13, height));
                color = lerp(color, _MiddleColor.rgb, step(0.025, height));
                color = lerp(color, _UpperColor.rgb, step(0.42, height));
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
