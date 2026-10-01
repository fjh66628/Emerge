Shader "MVP01/Camera Radial Fog"
{
    Properties
    {
        _FogColor("Fog color", Color) = (0.83, 0.88, 0.89, 1)
        _FogStart("Clear radius", Float) = 7
        _FogEnd("Opaque radius", Float) = 48
        _NoiseScale("Noise scale", Float) = 0.11
        _NoiseStrength("Distance distortion", Float) = 14
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }
        Pass
        {
            Name "CameraRadialFog"
            ZTest Always ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _FogColor;
                float _FogStart;
                float _FogEnd;
                float _NoiseScale;
                float _NoiseStrength;
            CBUFFER_END

            float Hash31(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float ValueNoise(float3 position)
            {
                float3 cell = floor(position);
                float3 blend = frac(position);
                blend = blend * blend * (3.0 - 2.0 * blend);

                float near00 = lerp(Hash31(cell), Hash31(cell + float3(1, 0, 0)), blend.x);
                float near10 = lerp(Hash31(cell + float3(0, 1, 0)), Hash31(cell + float3(1, 1, 0)), blend.x);
                float far00 = lerp(Hash31(cell + float3(0, 0, 1)), Hash31(cell + float3(1, 0, 1)), blend.x);
                float far10 = lerp(Hash31(cell + float3(0, 1, 1)), Hash31(cell + 1.0), blend.x);
                return lerp(lerp(near00, near10, blend.y), lerp(far00, far10, blend.y), blend.z);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                half4 sceneColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                float rawDepth = SampleSceneDepth(uv);

                // Sky pixels have no surface distance; preserve the layered skybox.
                #if UNITY_REVERSED_Z
                    if (rawDepth <= 0.00001) return sceneColor;
                    float deviceDepth = rawDepth;
                #else
                    if (rawDepth >= 0.99999) return sceneColor;
                    float deviceDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, rawDepth);
                #endif

                float3 worldPosition = ComputeWorldSpacePosition(uv, deviceDepth, UNITY_MATRIX_I_VP);
                float radius = distance(worldPosition, _WorldSpaceCameraPos);
                float3 noisePosition = worldPosition * _NoiseScale + float3(_Time.y * 0.018, 0, _Time.y * 0.009);
                float broad = ValueNoise(noisePosition);
                float detail = ValueNoise(noisePosition * 2.47 + 13.1);
                float distortion = ((broad * 0.72 + detail * 0.28) - 0.5) * _NoiseStrength;
                float fog = smoothstep(_FogStart, _FogEnd, radius + distortion);
                sceneColor.rgb = lerp(sceneColor.rgb, _FogColor.rgb, fog * 0.96);
                return sceneColor;
            }
            ENDHLSL
        }
    }
}
