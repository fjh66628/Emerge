Shader "MVP04/Window Volume"
{
    Properties
    {
        _Density ("Extinction / metre", Range(0,.2)) = .028
        _ScatteringAlbedo ("Single scattering albedo", Range(0,1)) = .9
        _Anisotropy ("Henyey Greenstein g", Range(-.8,.8)) = .25
        _NoiseAmount ("Density variation", Range(0,1)) = .2
        _NoiseScale ("Density noise frequency", Range(.05,2)) = .28
        _FlowSpeed ("Air flow speed / metres per second", Range(0,2)) = .28
        _FlowDirection ("Air flow direction", Vector) = (.8,.2,.35,0)
        _FlowWarp ("Air flow warp / metres", Range(0,3)) = 1.1
        _FlowDetail ("Fine air layers", Range(0,1)) = .45
        _DensityNoise ("Cached air flow RGB / density A", 3D) = "gray" {}
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            ZWrite Off ZTest Always Cull Off
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float _Density, _ScatteringAlbedo, _Anisotropy, _NoiseAmount, _NoiseScale;
                float _FlowSpeed, _FlowWarp, _FlowDetail;
                float4 _FlowDirection;
            CBUFFER_END
            TEXTURE3D(_DensityNoise); SAMPLER(sampler_DensityNoise);
            int _SourceCount, _ViewSteps, _LightSteps, _UseMainDirectionalLight;
            float _MainVolumeShadowStrength;
            float4 _Sources[3];
            float3 _AirFlowOffset;
            float3 _RoomMin, _RoomMax;
            float4 AirNoise(float3 grid,float footprint)
            {
                // Average sub-step structures rather than undersampling them into sparkling dots.
                float lod=clamp(log2(max(1,footprint)),0,5);
                return SAMPLE_TEXTURE3D_LOD(_DensityNoise,sampler_DensityNoise,grid/32,lod);
            }
            float SigmaT(float3 p,float footprint)
            {
                float density=max(0,_Density);
                if(_NoiseAmount<.001)return density;
                float frequency=max(.05,_NoiseScale);
                // Integrated on the renderer so changing speed/direction never jumps the field.
                float3 drift=_AirFlowOffset;
                float3 advected=p-drift;
                // A slower, broad vector field bends the drifting density into evolving wisps.
                // RGB and A are independent cached noise channels; no screen-space animation.
                float3 warp=AirNoise((p-drift*.38)*frequency*.4+float3(7,13,3),footprint*frequency*.4).rgb*2-1;
                float3 grid=(advected+warp*max(0,_FlowWarp))*frequency*float3(.8,1.35,.9);
                float baseNoise=AirNoise(grid,footprint*frequency*1.35).a;
                float detail=baseNoise;
                if(_FlowDetail>.001)
                    detail=AirNoise(grid.zxy*2.03+float3(17,5,11)+drift.zxy*frequency*.17,
                        footprint*frequency*2.75).a;
                float variation=(lerp(baseNoise,detail,saturate(_FlowDetail)*.45)-.5)*2.4;
                return density*max(0,1+saturate(_NoiseAmount)*variation);
            }
            float2 Bounds(float3 origin,float3 ray)
            {
                float3 safeRay=float3(ray.x<0?-1:1,ray.y<0?-1:1,ray.z<0?-1:1)*max(abs(ray),1e-6);
                float3 a=(_RoomMin-origin)/safeRay,b=(_RoomMax-origin)/safeRay;
                float3 nearT=min(a,b),farT=max(a,b);
                return float2(max(0,max(nearT.x,max(nearT.y,nearT.z))),min(farT.x,min(farT.y,farT.z)));
            }
            float LightTransmittance(float3 p,float3 toLight,float distanceToLight)
            {
                // Only the interior contains participating medium. Integrate back to its boundary.
                float lengthInAir=max(0,min(distanceToLight,Bounds(p,toLight).y));
                if(_NoiseAmount<.001)return exp(-max(0,_Density)*lengthInAir);
                float stepSize=lengthInAir/_LightSteps,depth=0;
                [loop] for(int i=0;i<_LightSteps;i++)depth+=SigmaT(p+toLight*((i+.5)*stepSize),stepSize)*stepSize;
                return exp(-depth);
            }
            float Phase(float cosine)
            {
                // dot(camera->sample, sample->light) equals dot(incoming propagation,
                // outgoing propagation); positive g is forward scattering.
                float g=clamp(_Anisotropy,-.8,.8);
                float denominator=max(1+g*g-2*g*cosine,.001);
                return (1-g*g)/(4*PI*denominator*sqrt(denominator));
            }
            half4 Frag(Varyings input):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv=input.texcoord;
                if(_Density<=0)return half4(0,0,0,1);
                float depth=SampleSceneDepth(uv);
                #if !UNITY_REVERSED_Z
                    depth=lerp(UNITY_NEAR_CLIP_VALUE,1,depth);
                #endif
                float3 surface=ComputeWorldSpacePosition(uv,depth,UNITY_MATRIX_I_VP);
                float3 origin=_WorldSpaceCameraPos,ray=normalize(surface-origin);
                float2 interval=Bounds(origin,ray);
                float begin=interval.x,end=min(distance(origin,surface),interval.y);
                if(end<=begin)return half4(0,0,0,1);
                int steps=min(_ViewSteps,max(8,(int)ceil((end-begin)/.3)));
                float stepSize=(end-begin)/steps;
                // Interleaved offsets cover the step interval evenly in a small pixel
                // neighbourhood, avoiding clumps from independent white-noise offsets.
                float jitter=frac(52.9829189*frac(dot(input.positionCS.xy,float2(.06711056,.00583715))));
                float transmittance=1;
                float3 scattered=0;
                [loop] for(int s=0;s<steps;s++)
                {
                    float3 p=origin+ray*(begin+(s+jitter)*stepSize);
                    float segmentT=exp(-SigmaT(p,stepSize)*stepSize);
                    float3 incident=0;
                    if(_UseMainDirectionalLight != 0)
                    {
                        // Sunlight is parallel and does not have inverse-square falloff inside the courtyard.
                        // Sample the same cascaded world-space shadow atlas as the stone and foliage.
                        Light sunlight=GetMainLight();
                        float visibility=1;
                        #if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                            float4 shadowCoord=TransformWorldToShadowCoord(p);
                            // Snapshot strength per camera: an empty shadow pass can retain the cascade keyword.
                            if(_MainVolumeShadowStrength>0 && !BEYOND_SHADOW_FAR(shadowCoord))
                                visibility=lerp(1,SAMPLE_TEXTURE2D_SHADOW(_MainLightShadowmapTexture,
                                    sampler_LinearClampCompare,shadowCoord.xyz),_MainVolumeShadowStrength);
                        #endif
                        float3 transmission=1;
                        #if defined(_LIGHT_COOKIES)
                            transmission=SampleMainLightCookie(p);
                        #endif
                        incident+=sunlight.color*visibility*transmission*
                            LightTransmittance(p,sunlight.direction,1e5)*Phase(dot(ray,sunlight.direction));
                    }
                    [loop] for(int source=0;source<_SourceCount;source++)
                    {
                        int index=(int)_Sources[source].w;
                        // Identical colour, inverse square, range and cone attenuation as surfaces.
                        Light light=GetAdditionalPerObjectLight(index,p);
                        float3 glass=0;
                        #if defined(_LIGHT_COOKIES)
                            glass=SampleAdditionalLightCookie(index,p);
                        #endif
                        if(max(glass.r,max(glass.g,glass.b))>.00001)
                        {
                            float visibility=AdditionalLightRealtimeShadow(index,p,light.direction);
                            if(visibility>0)
                                incident+=light.color*light.distanceAttenuation*glass*visibility*
                                    LightTransmittance(p,light.direction,distance(p,_Sources[source].xyz))*Phase(dot(ray,light.direction));
                        }
                    }
                    // Exact constant-density segment integral; camera extinction applies once.
                    scattered+=transmittance*(1-segmentT)*saturate(_ScatteringAlbedo)*incident;
                    transmittance*=segmentT;
                }
                return half4(scattered,transmittance);
            }
            ENDHLSL
        }
        Pass
        {
            Name "Edge aware volume denoise"
            ZWrite Off ZTest Always Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Denoise
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            float4 _VolumeSize;
            float _FilterStride, _DenoiseStrength;
            half4 Denoise(Varyings input):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv=input.texcoord;
                float4 centre=SAMPLE_TEXTURE2D_X_LOD(_BlitTexture,sampler_PointClamp,uv,0);
                float depth=LinearEyeDepth(SampleSceneDepth(uv),_ZBufferParams);
                float4 sum=0;float weightSum=0;
                [unroll] for(int y=-1;y<=1;y++)[unroll] for(int x=-1;x<=1;x++)
                {
                    float2 tap=clamp(uv+float2(x,y)*_VolumeSize.zw*_FilterStride,
                        _VolumeSize.zw*.5,1-_VolumeSize.zw*.5);
                    float4 value=SAMPLE_TEXTURE2D_X_LOD(_BlitTexture,sampler_PointClamp,tap,0);
                    float tapDepth=LinearEyeDepth(SampleSceneDepth(tap),_ZBufferParams);
                    float depthDifference=abs(tapDepth-depth)/max(.03,depth*.015);
                    float radianceDifference=length(value.rgb-centre.rgb)/max(.0001,length(value.rgb)+length(centre.rgb));
                    float spatial=(x==0?1:.5)*(y==0?1:.5);
                    float weight=spatial*exp2(-depthDifference*depthDifference-16*radianceDifference*radianceDifference);
                    sum+=value*weight;weightSum+=weight;
                }
                return lerp(centre,sum/max(weightSum,.0001),saturate(_DenoiseStrength));
            }
            ENDHLSL
        }
        Pass
        {
            Name "Depth aware upsample"
            ZWrite Off ZTest Always Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Composite
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            TEXTURE2D_X(_ChapelVolumeTexture);
            float4 _VolumeSize;
            half4 Composite(Varyings input):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv=input.texcoord;
                half4 original=SAMPLE_TEXTURE2D_X_LOD(_BlitTexture,sampler_LinearClamp,uv,0);
                float centreDepth=LinearEyeDepth(SampleSceneDepth(uv),_ZBufferParams);
                float2 grid=uv*_VolumeSize.xy-.5,base=floor(grid),f=frac(grid);
                float4 sum=0;float weightSum=0;
                [unroll] for(int y=0;y<2;y++)[unroll] for(int x=0;x<2;x++)
                {
                    float2 tap=clamp((base+float2(x,y)+.5)*_VolumeSize.zw,_VolumeSize.zw*.5,1-_VolumeSize.zw*.5);
                    float z=LinearEyeDepth(SampleSceneDepth(tap),_ZBufferParams);
                    float difference=abs(z-centreDepth)/max(.03,centreDepth*.015);
                    float weight=(x?f.x:1-f.x)*(y?f.y:1-f.y)*exp2(-difference*difference);
                    sum+=SAMPLE_TEXTURE2D_X_LOD(_ChapelVolumeTexture,sampler_PointClamp,tap,0)*weight;
                    weightSum+=weight;
                }
                // Thin foreground silhouettes may have no corresponding low-resolution depth.
                // Keep them clear instead of pulling background shafts across the character.
                if(weightSum<.0001)return original;
                sum/=weightSum;
                return half4(original.rgb*sum.a+sum.rgb,original.a);
            }
            ENDHLSL
        }
    }
}
