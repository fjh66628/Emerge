Shader "MVP04/Window Volume"
{
    Properties
    {
        _RoseMask ("Stained glass transmission", 2D) = "white" {}
        _SideMask ("Side window transmission", 2D) = "black" {}
        _SideDensity ("Side window dust density", Range(0,.2)) = .11
        [HDR] _ScatterColor ("Scattered radiance", Color) = (1.3,1.5,1.9,1)
        _WindowOrigin ("Window centre", Vector) = (0,8.4,17.5,0)
        _LightDirection ("Direction into nave", Vector) = (0,-0.42,-0.91,0)
        _Radius ("Window radius", Float) = 2.6
        _Length ("Beam length", Float) = 26
        _Density ("Dust density", Range(0,.2)) = .045
        _NoiseScale ("Dust scale", Float) = .48
        _BeamEdge ("Beam edge softness", Range(.003,.15)) = .008
        _BeamSpread ("Beam spread per metre", Range(0,.03)) = .001
        _BeamContrast ("Transmission contrast", Range(1,3)) = 2.05
        _ShadowSharpness ("Shadow edge definition", Range(0,1)) = .9
        _BeamIntensity ("Beam brightness", Range(0,2)) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            ZWrite Off ZTest Always Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_RoseMask); SAMPLER(sampler_RoseMask);
            TEXTURE2D(_SideMask); SAMPLER(sampler_SideMask);
            CBUFFER_START(UnityPerMaterial)
                float4 _ScatterColor, _WindowOrigin, _LightDirection;
                float _Radius, _Length, _Density, _NoiseScale;
                float _SideDensity;
                float _BeamEdge, _BeamSpread, _BeamContrast, _ShadowSharpness;
                float _BeamIntensity;
                int _SideCount;
                float4 _SideSources[8], _SideWindows[8];
            CBUFFER_END
            float Hash(float3 p)
            {
                p=frac(p*.1031); p+=dot(p,p.yzx+33.33); return frac((p.x+p.y)*p.z);
            }
            float Noise(float3 p)
            {
                float3 i=floor(p), f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(lerp(Hash(i),Hash(i+float3(1,0,0)),f.x),lerp(Hash(i+float3(0,1,0)),Hash(i+float3(1,1,0)),f.x),f.y),
                    lerp(lerp(Hash(i+float3(0,0,1)),Hash(i+float3(1,0,1)),f.x),lerp(Hash(i+float3(0,1,1)),Hash(i+1),f.x),f.y),f.z);
            }
            float TransmissionMask(half3 glass)
            {
                float value=max(glass.r,max(glass.g,glass.b));
                // Suppress dark lead leakage while keeping the brighter glass segments distinct.
                return .45*pow(saturate(value/.45),_BeamContrast)*smoothstep(.012,.055,value);
            }
            float DefinedShadow(float shadow)
            {
                float sharpness=saturate(_ShadowSharpness);
                float width=lerp(.5,.045,sharpness);
                return lerp(shadow,smoothstep(.5-width,.5+width,shadow),sharpness);
            }
            float SideAperture(float2 uv)
            {
                // Same straight jambs and elliptical crown as the side-window geometry.
                // Use the shared edge control for both the rose and side-window silhouettes.
                float x=abs(uv.x*2-1), y=uv.y*4.5;
                float edge=y>3.15 ? 1-length(float2(x,(y-3.15)/1.35)) : min(1-x,y/1.22);
                return smoothstep(0,max(_BeamEdge,.001),edge);
            }
            half4 Frag(Varyings input):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv=input.texcoord;
                half4 original=SAMPLE_TEXTURE2D_X_LOD(_BlitTexture,sampler_LinearClamp,uv,0);
                float depth=SampleSceneDepth(uv);
                #if !UNITY_REVERSED_Z
                    depth=lerp(UNITY_NEAR_CLIP_VALUE,1,depth);
                #endif
                float3 surface=ComputeWorldSpacePosition(uv,depth,UNITY_MATRIX_I_VP);
                float3 origin=_WorldSpaceCameraPos;
                float sceneDistance=min(distance(origin,surface),100);
                float3 ray=normalize(surface-origin);
                // Bound the ray march to the nave: no samples outside the lit air volume.
                float3 safeRay=sign(ray+1e-7)*max(abs(ray),1e-5);
                float3 t0=(float3(-8.65,-.1,-12)-origin)/safeRay;
                float3 t1=(float3(8.65,12,18)-origin)/safeRay;
                float3 nearT=min(t0,t1), farT=max(t0,t1);
                float begin=max(0,max(nearT.x,max(nearT.y,nearT.z)));
                float end=min(sceneDistance,min(farT.x,min(farT.y,farT.z)));
                if(end<=begin || (_Density<=0 && _SideDensity<=0)) return original;
                float3 axis=normalize(_LightDirection.xyz);
                float3 right=normalize(cross(axis,float3(0,1,0)));
                float3 up=normalize(cross(right,axis));
                const int STEPS=72;
                float stepSize=(end-begin)/STEPS;
                // Stable per-cell jitter avoids moving screen noise and reduces slice banding.
                float jitter=Hash(float3(floor(input.positionCS.xy/2),2.73));
                half transmittance=1;
                half3 lightSum=0;
                float phase=.5+.5*pow(saturate(dot(ray,-axis)),3);
                [loop] for(int s=0;s<STEPS;s++)
                {
                    float3 p=origin+ray*(begin+(s+jitter)*stepSize);
                    float3 delta=p-_WindowOrigin.xyz;
                    float along=dot(delta,axis);
                    float radius=_Radius*(1+along*_BeamSpread);
                    float2 radial=float2(dot(delta,right),dot(delta,up))/radius;
                    float edge=(1-smoothstep(1-_BeamEdge,1,length(radial)))*step(0,along)*step(along,_Length);
                    float dust=.42+.58*Noise(p*_NoiseScale+float3(_Time.y*.025,0,0));
                    float extinction=0;
                    half3 radiance=0;
                    if(edge>0 && _Density>0)
                    {
                        half3 glass=SAMPLE_TEXTURE2D_LOD(_RoseMask,sampler_RoseMask,radial*.5+.5,0).rgb;
                        float mask=TransmissionMask(glass);
                        float shadow=DefinedShadow(MainLightRealtimeShadow(TransformWorldToShadowCoord(p)));
                        float weight=_Density*edge*mask;
                        extinction+=weight;
                        radiance+=weight*glass*_ScatterColor.rgb*shadow*phase;
                    }
                    // Intersect each spotlight ray with its real glass plane. The same aperture
                    // projection is used for the surface cookie; side light shadows occlude the air.
                    [loop] for(int w=0;w<_SideCount;w++)
                    {
                        float3 source=_SideSources[w].xyz, centre=_SideWindows[w].xyz;
                        float3 fromLight=p-source;
                        float projection=(centre.x-source.x)/fromLight.x;
                        if(projection<=0 || projection>=1) continue;
                        float3 hit=source+fromLight*projection;
                        float2 paneUV=float2(.5+sign(centre.x)*(hit.z-centre.z)/2.44,(hit.y-3)/4.5);
                        if(any(paneUV<=0) || any(paneUV>=1)) continue;
                        half4 glass=SAMPLE_TEXTURE2D_LOD(_SideMask,sampler_SideMask,paneUV,0);
                        float mask=TransmissionMask(glass.rgb)*smoothstep(.15,.85,glass.a)*SideAperture(paneUV);
                        if(mask<.015) continue;
                        float dist=length(fromLight);
                        float shadow=DefinedShadow(AdditionalLightRealtimeShadow((int)_SideSources[w].w,p,-fromLight/dist));
                        float fade=(1-smoothstep(16,23,dist))*projection;
                        float weight=_SideDensity*mask*fade*_SideWindows[w].w*shadow;
                        float sidePhase=.65+.35*pow(saturate(dot(ray,-fromLight/dist)),3);
                        extinction+=weight;
                        radiance+=weight*glass.rgb*half3(3.6,4.2,5.25)*sidePhase;
                    }
                    half opacity=1-exp(-extinction*dust*stepSize);
                    lightSum+=transmittance*opacity*radiance/max(extinction,1e-5);
                    transmittance*=1-opacity;
                }
                return half4(original.rgb*transmittance+lightSum*max(0,_BeamIntensity),original.a);
            }
            ENDHLSL
        }
    }
}
