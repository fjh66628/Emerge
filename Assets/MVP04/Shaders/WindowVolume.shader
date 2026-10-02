Shader "MVP04/Window Volume"
{
    Properties
    {
        _Density ("Extinction / metre", Range(0,.2)) = .028
        _ScatteringAlbedo ("Single scattering albedo", Range(0,1)) = .9
        _Anisotropy ("Henyey Greenstein g", Range(-.8,.8)) = .25
        _NoiseAmount ("Density variation", Range(0,1)) = .2
        _NoiseScale ("Density noise frequency", Range(.05,2)) = .28
        _DensityNoise ("Cached density noise", 3D) = "white" {}
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
            CBUFFER_END
            TEXTURE3D(_DensityNoise); SAMPLER(sampler_DensityNoise);
            int _SourceCount, _ViewSteps, _LightSteps;
            float4 _Sources[3];
            float3 _RoomMin, _RoomMax;
            float Hash(float3 p)
            {p=frac(p*.1031);p+=dot(p,p.yzx+33.33);return frac((p.x+p.y)*p.z);}
            float SigmaT(float3 p)
            {
                // One hardware-filtered lookup replaces repeated eight-corner noise hashing.
                float noise=SAMPLE_TEXTURE3D_LOD(_DensityNoise,sampler_DensityNoise,
                    (p*_NoiseScale+float3(_Time.y*.015,0,0))/32,0).r;
                return max(0,_Density)*(1+saturate(_NoiseAmount)*(2*noise-1));
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
                [loop] for(int i=0;i<_LightSteps;i++)depth+=SigmaT(p+toLight*((i+.5)*stepSize))*stepSize;
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
                float jitter=Hash(float3(input.positionCS.xy,2.73));
                float transmittance=1;
                float3 scattered=0;
                [loop] for(int s=0;s<steps;s++)
                {
                    float3 p=origin+ray*(begin+(s+jitter)*stepSize);
                    float segmentT=exp(-SigmaT(p)*stepSize);
                    float3 incident=0;
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
