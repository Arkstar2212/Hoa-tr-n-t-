Shader "HoaVaMau/LivingOceanWaterSurface"
{
    Properties
    {
        [HDR] _GlintColor ("Sunlight Glint Color", Color) = (0.75, 0.95, 1.0, 0.32)
        _GlintScale ("Glint Wave Frequency", Float) = 18.0
        _GlintSpeed ("Drift Speed", Float) = 0.45
        _SparkleSharpness ("Sparkle Sharpness", Range(2.0, 12.0)) = 6.0
        _Intensity ("Overall Intensity", Range(0.1, 2.0)) = 0.85
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent+15" 
            "RenderPipeline" = "UniversalPipeline" 
        }

        Blend SrcAlpha One // Pure additive blending for sparkling sunlight on sea water
        ZWrite Off
        Cull Off

        Pass
        {
            Name "WaterSurfaceGlints"
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
                float3 worldPos   : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _GlintColor;
                float _GlintScale;
                float _GlintSpeed;
                float _SparkleSharpness;
                float _Intensity;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.worldPos = TransformObjectToWorld(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float t = _Time.y * _GlintSpeed;

                // 1. Dual wave interference for shimmering sea specular ripples
                float w1 = sin((uv.x * 2.4 + uv.y * 0.8) * _GlintScale + t * 2.2);
                float w2 = cos((uv.x * 1.8 - uv.y * 1.2) * (_GlintScale * 1.35) - t * 1.8);
                float w3 = sin(uv.x * (_GlintScale * 3.2) + cos(uv.y * 12.0 + t) * 1.5);

                float causticComb = saturate((w1 * 0.45 + w2 * 0.35 + w3 * 0.20) * 0.5 + 0.5);
                float glint = pow(causticComb, _SparkleSharpness);

                // Secondary delicate sun sparkles (lấp lánh ánh mặt trời)
                float sparkle = pow(saturate(w1 * w2), 4.0) * 1.4;
                float combinedGlint = glint * 0.65 + sparkle * 0.35;

                // 2. Ultra-soft feathering: ZERO hard edges anywhere
                // Vertical fade: fades out near horizon (top) and foreground shore (bottom)
                float vFade = smoothstep(0.0, 0.25, uv.y) * smoothstep(1.0, 0.65, uv.y);
                // Horizontal fade: fades out softly at screen edges
                float hFade = smoothstep(0.0, 0.08, uv.x) * smoothstep(1.0, 0.92, uv.x);
                float edgeMask = vFade * hFade;

                // 3. Subtle breathing pulsation
                float pulse = 0.88 + 0.12 * sin(t * 1.4);

                float alpha = combinedGlint * edgeMask * pulse * _GlintColor.a * _Intensity;
                half3 finalRGB = _GlintColor.rgb * alpha;

                return half4(finalRGB, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
