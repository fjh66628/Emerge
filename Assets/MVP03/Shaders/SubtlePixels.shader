Shader "MVP03/Subtle Pixels"
{
    Properties
    {
        [IntRange] _PixelSize ("Pixel Size (screen pixels)", Range(1, 6)) = 2
        _Blend ("Pixel Blend", Range(0, 1)) = 0.55
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "SubtlePixels"
            ZWrite Off
            ZTest Always
            Cull Off
            Blend Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _PixelSize;
                float _Blend;
            CBUFFER_END

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                float2 size = max(_BlitTexture_TexelSize.zw, float2(1, 1));
                float block = max(1.0, round(_PixelSize));
                float2 cellUV = (floor(uv * size / block) + .5) * block / size;
                // Clamp partial cells at odd target sizes to the final texel centre.
                cellUV = clamp(cellUV, .5 / size, 1.0 - .5 / size);
                half4 original = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv, 0);
                // Bilinear sampling at a 2x2 cell centre averages its four texels.
                // Mixing in the original keeps thin rings, stone pores and sprite faces legible.
                half3 pixels = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, cellUV, 0).rgb;
                return half4(lerp(original.rgb, pixels, saturate(_Blend)), original.a);
            }
            ENDHLSL
        }
    }
}
