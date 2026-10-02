Shader "MVP04/Frosted Stained Glass"
{
    Properties
    {
        _BaseMap ("Stained glass pattern", 2D) = "white" {}
        _BaseColor ("Glass tint", Color) = (1,1,1,1)
        _FrostTint ("Ground glass scattering", Color) = (.68,.8,.87,1)
        _PaneSize ("Pane size in metres", Vector) = (2.44,4.5,0,0)
        _FrostAmount ("Frosted surface", Range(0,1)) = .8
        _FrostBlur ("Transmission blur in texels", Range(0,6)) = 2.4
        _GrainStrength ("Etched grain", Range(0,1)) = .38
        _Transmission ("Diffuse transmission efficiency", Range(0,1)) = 1
        _Smoothness ("Base smoothness", Range(0,1)) = .24
        _Cutoff ("Aperture cutoff", Range(0,1)) = .5
        _Cull ("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        Cull [_Cull] ZWrite On
        Pass
        {
            Name "FrostedGlass"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            float4 _BaseMap_TexelSize;
            int _ChapelSourceCount;
            float4 _ChapelSourcePositions[3], _ChapelSourceForwards[3], _ChapelSourceRadiances[3], _ChapelSourceParameters[3];
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST, _BaseColor, _FrostTint, _PaneSize;
                float _FrostAmount, _FrostBlur, _GrainStrength, _Transmission, _Smoothness, _Cutoff, _Cull;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; float3 normalWS:TEXCOORD1; float2 uv:TEXCOORD2; float fog:TEXCOORD3; };
            Varyings Vert(Attributes i)
            {
                Varyings o; VertexPositionInputs p=GetVertexPositionInputs(i.positionOS.xyz);
                o.positionCS=p.positionCS; o.positionWS=p.positionWS;
                o.normalWS=TransformObjectToWorldNormal(i.normalOS); o.uv=TRANSFORM_TEX(i.uv,_BaseMap);
                o.fog=ComputeFogFactor(p.positionCS.z); return o;
            }
            float Hash(float2 p)
            {
                float3 q=frac(float3(p.xyx)*.1031); q+=dot(q,q.yzx+33.33); return frac((q.x+q.y)*q.z);
            }
            float Noise(float2 p)
            {
                float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(Hash(i),Hash(i+float2(1,0)),f.x),lerp(Hash(i+float2(0,1)),Hash(i+1),f.x),f.y);
            }
            half4 Frag(Varyings i):SV_Target
            {
                half4 pattern=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv);
                clip(pattern.a-_Cutoff);
                float2 d=_BaseMap_TexelSize.xy*_FrostBlur;
                half3 diffused=pattern.rgb*4;
                diffused+=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv+float2(d.x,0)).rgb;
                diffused+=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv-float2(d.x,0)).rgb;
                diffused+=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv+float2(0,d.y)).rgb;
                diffused+=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv-float2(0,d.y)).rgb;
                diffused*=.125;
                // The glass is ground, while the opaque lead lines retain their dark silhouette.
                float pane=smoothstep(.004,.026,max(pattern.r,max(pattern.g,pattern.b)));
                float frost=_FrostAmount*pane;
                float2 p=i.uv*_PaneSize.xy;
                float cloud=.65*Noise(p*3.4)+.35*Noise(p*11.7);
                float2 micro=p*92;
                float resolved=1-smoothstep(.6,1.8,max(fwidth(micro.x),fwidth(micro.y)));
                float grain=lerp(.5,Noise(micro),resolved);
                float2 slope=float2(Noise(micro+float2(.2,0))-Noise(micro-float2(.2,0)),
                    Noise(micro+float2(0,.2))-Noise(micro-float2(0,.2)))*resolved;
                float3 n=normalize(i.normalWS),t=normalize(float3(-n.z,0,n.x)),b=cross(n,t);
                n=normalize(n+(t*slope.x+b*slope.y)*frost*.9);
                float3 view=GetWorldSpaceNormalizeViewDir(i.positionWS);
                float rim=pow(1-saturate(dot(n,view)),3);
                float etched=(grain-.5)*_GrainStrength;
                half3 colour=lerp(pattern.rgb,diffused,frost)*_BaseColor.rgb;
                half3 milk=_FrostTint.rgb*frost*(.065+.11*cloud+.07*rim+etched*.18);
                InputData inputData=(InputData)0;
                inputData.positionWS=i.positionWS;inputData.normalWS=n;inputData.viewDirectionWS=view;
                inputData.shadowCoord=TransformWorldToShadowCoord(i.positionWS);
                inputData.bakedGI=SampleSH(n);inputData.shadowMask=half4(1,1,1,1);
                inputData.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);
                SurfaceData surface=(SurfaceData)0;
                surface.albedo=saturate(colour*.5+milk);surface.alpha=1;surface.occlusion=1;
                surface.normalTS=half3(0,0,1);
                surface.smoothness=clamp(_Smoothness+(cloud-.5)*.12-etched*.15,.08,.4);
                // Thin rough-pane Lambert transmission from the actual exterior emitter.
                // The complementary direct fraction is projected by its RGB light cookie.
                float3 irradiance=0;
                [loop] for(int s=0;s<_ChapelSourceCount;s++)
                {
                    float3 delta=_ChapelSourcePositions[s].xyz-i.positionWS;
                    float distanceSqr=max(dot(delta,delta),.01);
                    float3 toSource=delta*rsqrt(distanceSqr);
                    float4 parameters=_ChapelSourceParameters[s];
                    float cone=saturate(dot(-toSource,_ChapelSourceForwards[s].xyz)*parameters.y+parameters.z);
                    float rangeFactor=saturate(1-pow(distanceSqr*parameters.x,2));
                    float cosine=saturate(dot(-normalize(i.normalWS),toSource));
                    irradiance+=_ChapelSourceRadiances[s].rgb*(cone*cone*rangeFactor*rangeFactor*cosine*parameters.w/distanceSqr);
                }
                surface.emission=saturate(colour*(.83+.17*cloud+etched)+milk)*saturate(_Transmission)*
                    irradiance/PI;
                half4 result=UniversalFragmentPBR(inputData,surface);
                result.rgb=MixFog(result.rgb,i.fog);return result;
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
}
