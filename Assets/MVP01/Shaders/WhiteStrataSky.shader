Shader "MVP01/White Strata Sky"
{
    Properties
    {
        _ZenithColor ("Zenith White", Color) = (0.88, 0.90, 0.89, 1)
        _UpperColor ("Upper White", Color) = (0.94, 0.94, 0.91, 1)
        _HorizonColor ("Horizon White", Color) = (0.91, 0.93, 0.91, 1)
        _LowerColor ("Lower Haze", Color) = (0.83, 0.86, 0.85, 1)
        _BandStrength ("Strata Strength", Range(0, 0.03)) = 0.009
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
                half4 _ZenithColor;
                half4 _UpperColor;
                half4 _HorizonColor;
                half4 _LowerColor;
                half _BandStrength;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 directionWS : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.directionWS = TransformObjectToWorldDir(input.positionOS.xyz);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 direction = normalize(input.directionWS);
                float height = direction.y;
                half3 sky = lerp(_LowerColor.rgb, _HorizonColor.rgb,
                    smoothstep(-0.48, 0.02, height));
                sky = lerp(sky, _UpperColor.rgb, smoothstep(0.01, 0.48, height));
                sky = lerp(sky, _ZenithColor.rgb, smoothstep(0.39, 0.98, height));

                float stratum = sin(height * 24.0 + sin(direction.x * 4.0) * 0.38);
                stratum *= exp(-abs(height) * 2.6);
                float grain = frac(sin(dot(direction.xz, float2(127.1, 311.7))) * 43758.5453);
                sky += (stratum * 0.65 + (grain - 0.5) * 0.24) * _BandStrength;
                return half4(sky, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
