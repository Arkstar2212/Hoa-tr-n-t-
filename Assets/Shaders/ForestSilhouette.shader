// ForestSilhouette.shader
// Shader chuyên dụng cho lớp tiền cảnh đóng khung (Foreground Silhouette / Repoussoir).
// Hỗ trợ texture PNG trong suốt + màu đen nghệ thuật (xanh rêu đen) + vi dao động gió nhẹ.
// Chuẩn URP, SRP Batcher, 100% không tốn CPU.

Shader "HoaVaMau/ForestSilhouette"
{
    Properties
    {
        [Header(Texture and Silhouette Color)]
        _MainTex            ("Silhouette Texture (PNG)", 2D) = "white" {}
        _SilhouetteColor    ("Silhouette Tint (Nen dung xanh den)", Color) = (0.04, 0.08, 0.06, 1.0)
        _Cutoff             ("Alpha Cutoff (0 = Alpha Blend)", Range(0.0, 1.0)) = 0.05

        [Header(Subtle Wind Sway)]
        _WindSway           ("Subtle Wind Amplitude", Range(0.0, 0.2)) = 0.04
        _WindSpeed          ("Wind Frequency", Range(0.1, 3.0)) = 0.8
    }

    SubShader
    {
        Tags
        {
            "Queue"             = "Transparent+50"
            "IgnoreProjector"   = "True"
            "RenderType"        = "Transparent"
            "PreviewType"       = "Plane"
            "CanUseSpriteAtlas" = "True"
            "RenderPipeline"    = "UniversalPipeline"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "SilhouetteForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _SilhouetteColor;
                float  _Cutoff;
                float  _WindSway;
                float  _WindSpeed;
            CBUFFER_END

            // Globals from WindManager
            float4 _GlobalWind;

            Varyings vert(Attributes input)
            {
                Varyings output;

                // Vi dao động gió nhẹ trên đỉnh ngọn cỏ/cành cây tiền cảnh
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                float h = saturate(input.uv.y);
                float bend = h * h;

                float t = _Time.y * _WindSpeed + worldPos.x * 0.3;
                float wave = sin(t) * _WindSway * bend;

                float3 displacedOS = input.positionOS.xyz;
                displacedOS.x += wave;

                output.positionCS = TransformObjectToHClip(displacedOS);
                output.uv         = TRANSFORM_TEX(input.uv, _MainTex);
                output.color      = input.color * _SilhouetteColor;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 texCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                if (_Cutoff > 0.001)
                {
                    clip(texCol.a - _Cutoff);
                }

                // Giữ nguyên hình khối Alpha của Texture, nhưng nhuộm màu Silhouette tối sẫm
                half4 finalCol;
                finalCol.rgb = input.color.rgb;
                finalCol.a   = texCol.a * input.color.a;

                return finalCol;
            }
            ENDHLSL
        }
    }
    FallBack "Sprites/Default"
}
