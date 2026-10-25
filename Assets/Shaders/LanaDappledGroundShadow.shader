Shader "HoaVaMau/LanaDappledGroundShadow"
{
    Properties
    {
        _ShadowColor ("Dappled Shadow Color", Color) = (0.07, 0.19, 0.16, 0.62)
        _SwaySpeed ("Canopy Dapple Sway Speed", Range(0.1, 3.0)) = 1.15
        _DappleScale ("Leaf Cluster Dapple Scale", Range(1.0, 10.0)) = 4.2
        _Softness ("Edge Feather Softness", Range(0.1, 1.0)) = 0.48
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent-20"
        }

        Pass
        {
            Name "DappledShadowForward"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            Offset -1, -1

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float4 color : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _ShadowColor;
                float _SwaySpeed;
                float _DappleScale;
                float _Softness;
            CBUFFER_END

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i);
                float b = Hash21(i + float2(1.0, 0.0));
                float c = Hash21(i + float2(0.0, 1.0));
                float d = Hash21(i + float2(1.0, 1.0));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.uv = IN.uv;
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Elliptical distance from center (0.5, 0.5)
                float2 centered = (IN.uv - 0.5) * 2.0;

                // Animated wind sway offset for dappled leaf clusters
                float t = _Time.y * _SwaySpeed;
                float2 windShift = float2(sin(t + IN.positionWS.x * 0.7) * 0.08, cos(t * 0.8 + IN.positionWS.z) * 0.05);
                float2 sampleUV = (IN.uv + windShift) * _DappleScale;

                float n1 = ValueNoise(sampleUV);
                float n2 = ValueNoise(sampleUV * 2.1 - float2(t * 0.35, 0.0));
                float dapple = n1 * 0.65 + n2 * 0.35;

                // Organic multi-lobed tree canopy + trunk base silhouette
                float dist = length(centered * float2(0.92, 1.08)) - (dapple - 0.5) * 0.32;
                float radialMask = 1.0 - smoothstep(1.0 - _Softness, 1.0, dist);

                // Sunbeam gaps filtering through leaves (dappled light holes inside the canopy shadow)
                float sunGap = smoothstep(0.28, 0.74, dapple + (1.0 - dist) * 0.35);

                half finalAlpha = (half)(radialMask * (0.45 + 0.55 * sunGap)) * _ShadowColor.a * IN.color.a;
                return half4(_ShadowColor.rgb, finalAlpha);
            }
            ENDHLSL
        }
    }
}
