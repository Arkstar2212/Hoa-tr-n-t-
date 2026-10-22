Shader "HoaVaMau/LanaBackgroundBlend"
{
    Properties
    {
        _MainTex ("Background Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1, 1, 1, 1)
        _LeftFeather ("Left Edge Feather Width", Range(0.0, 0.45)) = 0.0
        _RightFeather ("Right Edge Feather Width", Range(0.0, 0.45)) = 0.0
        _BottomFeather ("Bottom Edge Feather Width", Range(0.0, 0.35)) = 0.0

        [Header(Cloud Radiance and Sky Clarity)]
        _CloudRadianceColor ("Cloud Rim Radiance Color", Color) = (1.0, 0.98, 0.90, 1.0)
        _CloudRadianceIntensity ("Cloud Radiance Intensity", Range(0.0, 1.5)) = 0.38
        _SunbeamIntensity ("Crepuscular Sunbeam Intensity", Range(0.0, 1.0)) = 0.20
        _SunbeamOriginUV ("Sunbeam Origin UV", Vector) = (0.52, 0.72, 0.0, 0.0)
        _SkyClarity ("Sky Clarity Boost", Range(0.9, 1.25)) = 1.05
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
                half4 _CloudRadianceColor;
                half _CloudRadianceIntensity;
                half _SunbeamIntensity;
                float4 _SunbeamOriginUV;
                half _SkyClarity;
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

                // 1. Subtle Sky Clarity & Luminous Cumulus Cloud Radiance (Silver-Lining Glow)
                half lum = dot(col.rgb, half3(0.299h, 0.587h, 0.114h));
                half maxC = max(col.r, max(col.g, col.b));
                half minC = min(col.r, min(col.g, col.b));
                half sat = (maxC - minC) / max(maxC, 0.001h);

                // Cloud crests live in upper sky (uv.y > 0.35) with high luminance and low-to-warm saturation
                half skyRegion = smoothstep(0.30h, 0.48h, IN.uv.y);
                half cloudCrestMask = smoothstep(0.66h, 0.92h, lum) * smoothstep(0.38h, 0.08h, sat) * skyRegion;

                // Gentle breathing shimmer on the glowing cloud crests
                float breathe = 0.90 + 0.10 * sin(_Time.y * 0.85 + IN.uv.x * 5.0);
                half3 cloudGlow = _CloudRadianceColor.rgb * (cloudCrestMask * _CloudRadianceIntensity * (half)breathe);

                // 2. Soft Diagonal Parallel Crepuscular Sunbeams (Streaming gently across upper atmosphere)
                float rayCoord = IN.uv.x * 0.78 + IN.uv.y * 0.48;
                float rayPattern1 = sin(rayCoord * 24.0 + _Time.y * 0.20) * 0.5 + 0.5;
                float rayPattern2 = sin(rayCoord * 42.0 - _Time.y * 0.14) * 0.5 + 0.5;
                float rays = pow(rayPattern1 * 0.62 + rayPattern2 * 0.38, 2.6);
                float skySunbeamFade = smoothstep(0.32, 0.62, IN.uv.y) * smoothstep(0.98, 0.70, IN.uv.y) * smoothstep(0.05, 0.45, IN.uv.x);
                half3 sunbeamAdd = _CloudRadianceColor.rgb * ((half)(rays * skySunbeamFade) * _SunbeamIntensity);

                // Apply clarity & cloud radiance while keeping darks/midtones natural
                half3 enhancedRGB = pow(max(col.rgb, 0.0001h), 1.0h / max(_SkyClarity, 0.9h));
                col.rgb = saturate(enhancedRGB + cloudGlow + sunbeamAdd);

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
