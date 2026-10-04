Shader "MVP03/World Stone PBR"
{
    Properties
    {
        _BaseMap("Limestone albedo", 2D) = "white" {}
        _BumpMap("Stone relief", 2D) = "bump" {}
        _BaseColor("Tint", Color) = (1,1,1,1)
        _WorldScale("Repeats per metre", Float) = .7
        _BumpScale("Relief", Range(0,2)) = .65
        _Smoothness("Smoothness", Range(0,1)) = .16
        _Cutoff("Cutoff", Float) = .5
        _Cull("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float _WorldScale, _BumpScale, _Smoothness, _Cutoff, _Cull;
            CBUFFER_END
            struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; float4 color:COLOR; };
            struct V { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; float3 normalWS:TEXCOORD1; float4 color:COLOR; float fog:TEXCOORD2; };
            V Vert(A i)
            {
                V o;
                VertexPositionInputs p=GetVertexPositionInputs(i.positionOS.xyz);
                o.positionCS=p.positionCS; o.positionWS=p.positionWS;
                o.normalWS=TransformObjectToWorldNormal(i.normalOS);
                o.color=i.color; o.fog=ComputeFogFactor(p.positionCS.z);
                return o;
            }
            half4 Frag(V i):SV_Target
            {
                float3 n=normalize(i.normalWS);
                float3 w=pow(abs(n),4); w/=max(dot(w,1),.0001);
                float3 p=i.positionWS*_WorldScale;
                half3 a=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p.zy).rgb*w.x
                    +SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p.xz).rgb*w.y
                    +SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,p.xy).rgb*w.z;
                half3 nx=UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,p.zy),_BumpScale);
                half3 ny=UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,p.xz),_BumpScale);
                half3 nz=UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,p.xy),_BumpScale);
                float3 bump=float3(nx.z*sign(n.x),nx.y,nx.x)*w.x
                    +float3(ny.x,ny.z*sign(n.y),ny.y)*w.y
                    +float3(nz.x,nz.y,nz.z*sign(n.z))*w.z;
                InputData d=(InputData)0;
                d.positionWS=i.positionWS; d.normalWS=normalize(bump);
                d.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS);
                d.shadowCoord=TransformWorldToShadowCoord(i.positionWS);
                d.bakedGI=SampleSH(d.normalWS);
                d.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);
                d.shadowMask=half4(1,1,1,1);
                SurfaceData s=(SurfaceData)0;
                s.albedo=a*_BaseColor.rgb*i.color.rgb;
                s.alpha=1; s.smoothness=_Smoothness; s.occlusion=1;
                s.normalTS=half3(0,0,1);
                half4 col=UniversalFragmentPBR(d,s);
                col.rgb=MixFog(col.rgb,i.fog);
                return col;
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
}
