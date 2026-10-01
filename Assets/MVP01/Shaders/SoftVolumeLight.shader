Shader "MVP01/Soft Volume Light"
{
    Properties
    {
        _LightColor ("Scattered Light", Color) = (1, 0.96, 0.86, 1)
        _Density ("Density", Range(0, 0.1)) = 0.025
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+40" "RenderType"="Transparent" }
        Pass
        {
            Name "Local Volumetric Light"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha One
            ZWrite Off
            ZTest Always
            Cull Front

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _LightColor;
                half _Density;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 cameraWS = GetCameraPositionWS();
                float3 rayWS = normalize(input.positionWS - cameraWS);
                float3 originOS = TransformWorldToObject(cameraWS);
                float3 rayOS = mul((float3x3)unity_WorldToObject, rayWS);
                float3 safeRayOS = rayOS + sign(rayOS + 1e-6) * 1e-5;

                float3 nearPlane = (-0.5 - originOS) / safeRayOS;
                float3 farPlane = (0.5 - originOS) / safeRayOS;
                float3 nearAxis = min(nearPlane, farPlane);
                float3 farAxis = max(nearPlane, farPlane);
                float entry = max(0.0, max(nearAxis.x, max(nearAxis.y, nearAxis.z)));
                float exit = min(farAxis.x, min(farAxis.y, farAxis.z));

                float2 screenUV = input.positionHCS.xy / _ScaledScreenParams.xy;
                float rawDepth = SampleSceneDepth(screenUV);
                float3 opaqueWS = ComputeWorldSpacePosition(screenUV, rawDepth, UNITY_MATRIX_I_VP);
                exit = min(exit, distance(cameraWS, opaqueWS) - 0.03);
                if (exit <= entry) return half4(0, 0, 0, 0);

                const int steps = 16;
                float stepLength = (exit - entry) / steps;
                float scattering = 0.0;
                [unroll]
                for (int i = 0; i < steps; i++)
                {
                    float distanceAlongRay = entry + (i + 0.5) * stepLength;
                    float3 sampleWS = cameraWS + rayWS * distanceAlongRay;
                    float3 sampleOS = TransformWorldToObject(sampleWS);
                    float height = saturate(sampleOS.y + 0.5);
                    float radius = lerp(0.48, 0.08, height);
                    float radial = 1.0 - smoothstep(radius * 0.55, radius, length(sampleOS.xz));
                    float vertical = smoothstep(0.0, 0.15, height) * smoothstep(0.0, 0.18, 1.0 - height);
                    float haze = 0.9 + 0.1 * sin(dot(sampleWS.xz, float2(0.83, 1.27)) + sampleWS.y * 0.41);
                    scattering += radial * vertical * haze * stepLength;
                }

                return half4(_LightColor.rgb, saturate(scattering * _Density));
            }
            ENDHLSL
        }
    }
    FallBack Off
}
