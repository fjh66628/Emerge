Shader "MVP04/Lit Pixel Character"
{
    Properties
    {
        _BaseMap ("Sprite atlas", 2D) = "white" {}
        _BaseColor ("Albedo tint", Color) = (1,1,1,1)
        _Cutoff ("Alpha cutoff", Range(0,1)) = .5
        _Smoothness ("Cloth smoothness", Range(0,1)) = .12
        _NormalBend ("Rounded sprite normals", Range(0,3)) = 1.5
        _BackLight ("Back-facing diffuse response", Range(0,1)) = .35
        _AmbientStrength ("Environment light multiplier", Range(0,2)) = 1
        [Toggle] _LightFacingShadow ("Light-facing sprite shadow", Float) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout"
               "Queue"="AlphaTest" "UniversalMaterialType"="Lit" "DisableBatching"="True" }
        Cull Off ZWrite On

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST, _BaseColor;
            float _Cutoff, _Smoothness, _NormalBend, _BackLight, _AmbientStrength, _LightFacingShadow;
        CBUFFER_END
        struct Attributes
        {
            float4 positionOS : POSITION;
            float2 uv : TEXCOORD0;
            half4 color : COLOR;
        };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
            float2 uv : TEXCOORD1;
            float2 localPosition : TEXCOORD2;
            half4 color : COLOR;
            float fog : TEXCOORD3;
        };
        Varyings CharacterVertex(Attributes input)
        {
            Varyings output;
            VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
            output.positionCS = position.positionCS;
            output.positionWS = position.positionWS;
            output.localPosition = input.positionOS.xy;
            output.uv = input.uv;
            output.color = input.color;
            output.fog = ComputeFogFactor(position.positionCS.z);
            return output;
        }
        half4 CharacterAlbedo(Varyings input)
        {
            half4 colour = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor * input.color;
            clip(colour.a - _Cutoff);
            return colour;
        }
        float3 CharacterNormal(Varyings input)
        {
            // A soft cylindrical profile gives the flat sprite readable side lighting.
            // Position is relative to the common foot pivot, so atlas frames stay aligned.
            float3 normalOS = normalize(float3(clamp(input.localPosition.x * _NormalBend, -.9, .9), .15, -1));
            float3 normalWS = TransformObjectToWorldNormal(normalOS);
            float3 view = GetWorldSpaceNormalizeViewDir(input.positionWS);
            return dot(normalWS, view) < 0 ? -normalWS : normalWS;
        }
        ENDHLSL

        Pass
        {
            Name "Character lighting"
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex CharacterVertex
            #pragma fragment CharacterFragment
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile_fragment _ _LIGHT_LAYERS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            half3 CharacterLight(BRDFData brdf, Light light, float3 normal, float3 view)
            {
                #if defined(_LIGHT_LAYERS)
                    if (!IsMatchingLightLayer(light.layerMask, GetMeshRenderingLayer())) return 0;
                #endif
                half3 reflected = LightingPhysicallyBased(brdf, light, normal, view);
                // A billboard stands in for a volume. Back-facing light contributes
                // diffuse colour, with the same cookie, falloff and shadow visibility.
                half back = saturate(-dot(normal, light.direction)) * _BackLight;
                return reflected + brdf.diffuse * light.color *
                    (back * light.distanceAttenuation * light.shadowAttenuation);
            }
            half4 CharacterFragment(Varyings input) : SV_Target
            {
                half4 albedo = CharacterAlbedo(input);
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = CharacterNormal(input);
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = half4(1,1,1,1);
                inputData.bakedGI = SampleSH(inputData.normalWS) * _AmbientStrength;
                SurfaceData surface = (SurfaceData)0;
                surface.albedo = albedo.rgb;
                surface.alpha = 1;
                surface.smoothness = _Smoothness;
                surface.occlusion = 1;
                surface.normalTS = half3(0,0,1);
                BRDFData brdf;
                InitializeBRDFData(surface, brdf);
                AmbientOcclusionFactor ao = CreateAmbientOcclusionFactor(inputData, surface);
                half4 shadowMask = CalculateShadowMask(inputData);
                Light main = GetMainLight(inputData, shadowMask, ao);
                MixRealtimeAndBakedGI(main, inputData.normalWS, inputData.bakedGI);
                half3 colour = GlobalIllumination(brdf, (BRDFData)0, 0, inputData.bakedGI,
                    ao.indirectAmbientOcclusion, inputData.positionWS, inputData.normalWS,
                    inputData.viewDirectionWS, inputData.normalizedScreenSpaceUV);
                colour += CharacterLight(brdf, main, inputData.normalWS, inputData.viewDirectionWS);

                #if defined(_ADDITIONAL_LIGHTS) || defined(_ADDITIONAL_LIGHTS_VERTEX)
                    #if USE_CLUSTER_LIGHT_LOOP
                        [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                        {
                            CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
                            Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, ao);
                            colour += CharacterLight(brdf, light, inputData.normalWS, inputData.viewDirectionWS);
                        }
                    #endif
                    uint count = GetAdditionalLightsCount();
                    LIGHT_LOOP_BEGIN(count)
                        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, ao);
                        colour += CharacterLight(brdf, light, inputData.normalWS, inputData.viewDirectionWS);
                    LIGHT_LOOP_END
                #endif
                return half4(MixFog(colour, input.fog), 1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ColorMask 0
            HLSLPROGRAM
            #pragma vertex ShadowVertex
            #pragma fragment ShadowFragment
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection, _LightPosition;
            Varyings ShadowVertex(Attributes input)
            {
                Varyings output = CharacterVertex(input);
                float3 normal = TransformObjectToWorldNormal(float3(0,0,-1));
                if (_LightFacingShadow > .5)
                {
                    // A camera-facing card otherwise collapses under side illumination.
                    // Rotate only its shadow silhouette about the foot pivot for each light.
                    float3 pivot = TransformObjectToWorld(float3(0,0,0));
                    #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                        float3 towardsLight = _LightPosition - pivot;
                    #else
                        float3 towardsLight = _LightDirection;
                    #endif
                    float3 horizontal = float3(towardsLight.x, 0, towardsLight.z);
                    float lengthSquared = dot(horizontal, horizontal);
                    float3 facing = lengthSquared > 1e-6 ? horizontal * rsqrt(lengthSquared)
                        : normalize(float3(normal.x, 0, normal.z));
                    float3 right = cross(facing, float3(0,1,0));
                    float scaleX = length(TransformObjectToWorldDir(float3(1,0,0), false));
                    float scaleY = length(TransformObjectToWorldDir(float3(0,1,0), false));
                    output.positionWS = pivot + right * input.positionOS.x * scaleX
                        + float3(0,1,0) * input.positionOS.y * scaleY;
                    normal = facing;
                }
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 direction = normalize(_LightPosition - output.positionWS);
                #else
                    float3 direction = _LightDirection;
                #endif
                normal = dot(normal, direction) < 0 ? -normal : normal;
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(output.positionWS, normal, direction));
                output.positionCS = ApplyShadowClamping(output.positionCS);
                return output;
            }
            half4 ShadowFragment(Varyings input) : SV_Target { CharacterAlbedo(input); return 0; }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ColorMask R
            HLSLPROGRAM
            #pragma vertex CharacterVertex
            #pragma fragment DepthFragment
            half4 DepthFragment(Varyings input) : SV_Target { CharacterAlbedo(input); return 0; }
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormalsOnly" }
            HLSLPROGRAM
            #pragma vertex CharacterVertex
            #pragma fragment NormalFragment
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            half4 NormalFragment(Varyings input) : SV_Target
            {
                CharacterAlbedo(input);
                float3 normal = CharacterNormal(input);
                #if defined(_GBUFFER_NORMALS_OCT)
                    float2 octNormal = saturate(PackNormalOctQuadEncode(normal) * .5 + .5);
                    return half4(PackFloat2To888(octNormal), 0);
                #else
                    return half4(normal, 0);
                #endif
            }
            ENDHLSL
        }
    }
}
