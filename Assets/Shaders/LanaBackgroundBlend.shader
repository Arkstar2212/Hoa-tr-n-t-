Shader "HoaVaMau/LanaBackgroundBlend"
{
    Properties
    {
        _MainTex ("Background Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1, 1, 1, 1)
        _LeftFeather ("Left Edge Feather Width", Range(0.0, 0.45)) = 0.0
        _RightFeather ("Right Edge Feather Width", Range(0.0, 0.45)) = 0.0
        _BottomFeather ("Bottom Edge Feather Width", Range(0.0, 0.35)) = 0.0
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent-100"
        }

        Pass
        {
            Name "UnlitBlend"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half _LeftFeather;
                half _RightFeather;
                half _BottomFeather;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * _Color;

                half alpha = col.a;
                if (_LeftFeather > 0.001h)
                {
                    alpha *= smoothstep(0.0h, _LeftFeather, IN.uv.x);
                }
                if (_RightFeather > 0.001h)
                {
                    alpha *= smoothstep(0.0h, _RightFeather, 1.0h - IN.uv.x);
                }
                if (_BottomFeather > 0.001h)
                {
                    alpha *= smoothstep(0.0h, _BottomFeather, IN.uv.y);
                }

                col.a = alpha;
                return col;
            }
            ENDHLSL
        }
    }
}
