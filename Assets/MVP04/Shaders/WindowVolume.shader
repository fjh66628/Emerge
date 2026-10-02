Shader "MVP04/Window Volume"
{
    Properties
    {
        _RoseMask ("Stained glass transmission", 2D) = "white" {}
        [HDR] _ScatterColor ("Scattered radiance", Color) = (1.3,1.5,1.9,1)
        _WindowOrigin ("Window centre", Vector) = (0,8.4,17.5,0)
        _LightDirection ("Direction into nave", Vector) = (0,-0.42,-0.91,0)
        _Radius ("Window radius", Float) = 2.6
        _Length ("Beam length", Float) = 26
        _Density ("Dust density", Range(0,.2)) = .045
        _NoiseScale ("Dust scale", Float) = .48
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_RoseMask); SAMPLER(sampler_RoseMask);
            CBUFFER_START(UnityPerMaterial)
                float4 _ScatterColor, _WindowOrigin, _LightDirection;
                float _Radius, _Length, _Density, _NoiseScale;
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
                float3 t0=(float3(-5,-.1,-9)-origin)/safeRay;
                float3 t1=(float3(5,12,18)-origin)/safeRay;
                float3 nearT=min(t0,t1), farT=max(t0,t1);
                float begin=max(0,max(nearT.x,max(nearT.y,nearT.z)));
                float end=min(sceneDistance,min(farT.x,min(farT.y,farT.z)));
                if(end<=begin || _Density<=0) return original;
                float3 axis=normalize(_LightDirection.xyz);
                float3 right=normalize(cross(axis,float3(0,1,0)));
                float3 up=normalize(cross(right,axis));
                const int STEPS=48;
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
                    if(along<0 || along>_Length) continue;
                    float radius=_Radius*(1+along*.012);
                    float2 radial=float2(dot(delta,right),dot(delta,up))/radius;
                    float edge=1-smoothstep(.82,1,dot(radial,radial));
                    if(edge<=0) continue;
                    half3 glass=SAMPLE_TEXTURE2D_LOD(_RoseMask,sampler_RoseMask,radial*.5+.5,0).rgb;
                    float mask=max(glass.r,max(glass.g,glass.b));
                    float dust=.42+.58*Noise(p*_NoiseScale+float3(_Time.y*.025,0,0));
                    float shadow=MainLightRealtimeShadow(TransformWorldToShadowCoord(p));
                    float extinction=_Density*edge*dust*mask*stepSize;
                    half opacity=1-exp(-extinction);
                    lightSum+=transmittance*opacity*glass*_ScatterColor.rgb*shadow*phase;
                    transmittance*=1-opacity;
                }
                return half4(original.rgb*transmittance+lightSum,original.a);
            }
            ENDHLSL
        }
    }
}
