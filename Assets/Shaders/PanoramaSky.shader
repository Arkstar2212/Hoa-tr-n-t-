Shader "HoaVaMau/PanoramaSky"
{
    Properties
    {
        _MainTex ("Panorama Texture (2D)", 2D) = "white" {}
        _Tint ("Tint Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _Exposure ("Exposure Boost", Range(0.5, 2.5)) = 1.0
        _Vignette ("Top Zenith Vignette", Range(0.0, 1.0)) = 0.15
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Background"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        ZWrite Off
        ZTest LEqual

        Pass
        {
            Name "PanoramaForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Tint;
                float  _Exposure;
                float  _Vignette;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Tint * _Exposure;
                
                // Subtle darkening toward top of sky for extra drama
                half topDim = lerp(1.0, 0.85, saturate(input.uv.y * _Vignette));
                col.rgb *= topDim;

                return col;
            }
            ENDHLSL
        }
    }
}
