Shader "MVP01/Uncanny Flora"
{
    Properties
    {
        _GlowColor("Petal glow color", Color) = (0.65, 0.75, 1, 1)
        _GlowStrength("Petal glow strength", Range(0, 1)) = 0.28
        _WindStrength("Wind sway", Range(0, 0.2)) = 0.045
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }
        Cull Off
        ZWrite On
        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _GlowColor;
                half _GlowStrength;
                half _WindStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half3 normalWS : TEXCOORD0;
                half4 color : COLOR;
                half fogFactor : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float sway = sin(_Time.y * 0.72 + input.uv.x + positionWS.x * 0.22)
                    + sin(_Time.y * 0.39 + input.uv.x * 1.37 + positionWS.z * 0.18) * 0.35;
                positionWS.x += sway * _WindStrength * input.uv.y;
                positionWS.z += sway * _WindStrength * input.uv.y * 0.42;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.color = input.color;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                Light sun = GetMainLight();
                half diffuse = abs(dot(normalize(input.normalWS), sun.direction));
                half3 reflected = input.color.rgb * (0.32h + diffuse * 0.72h) * sun.color;
                half3 glow = _GlowColor.rgb * input.color.a * _GlowStrength;
                half3 color = MixFog(reflected + glow, input.fogFactor);
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
