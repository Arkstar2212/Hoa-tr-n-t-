Shader "HoaVaMau/StylizedSun"
{
    Properties
    {
        [Header(Sun Colors)]
        [HDR] _CoreColor ("Core Bright Color (HDR)", Color) = (2.0, 1.8, 1.3, 1.0)
        [HDR] _GlowColor ("Outer Glow Color (HDR)", Color) = (1.5, 0.6, 0.15, 1.0)
        _HorizonHazeColor ("Horizon Haze Blend Color", Color) = (0.9, 0.45, 0.25, 0.5)

        [Header(Size and Radii)]
        _CoreRadius ("Core Radius", Range(0.05, 0.6)) = 0.22
        _CoreSoftness ("Core Edge Softness", Range(0.01, 0.4)) = 0.08
        _GlowRadius ("Outer Corona Radius", Range(0.2, 1.0)) = 0.85
        _GlowFalloff ("Glow Falloff Exponent", Range(0.5, 5.0)) = 2.2

        [Header(Atmospheric Horizon Clip)]
        _HorizonFadeHeight ("Horizon Fade Center (UV.Y)", Range(0.0, 0.5)) = 0.15
        _HorizonFadeSoftness ("Horizon Fade Softness", Range(0.01, 0.4)) = 0.12
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline" 
        }

        Blend One One // Additive for gorgeous glowing bloom
        Cull Off
        ZWrite Off

        Pass
        {
            Name "UniversalForward"
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

            CBUFFER_START(UnityPerMaterial)
                float4 _CoreColor;
                float4 _GlowColor;
                float4 _HorizonHazeColor;
                float _CoreRadius;
                float _CoreSoftness;
                float _GlowRadius;
                float _GlowFalloff;
                float _HorizonFadeHeight;
                float _HorizonFadeSoftness;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Centered UV (-0.5 to 0.5)
                float2 centered = input.uv - float2(0.5, 0.5);
                float dist = length(centered) * 2.0; // 0 at center, 1 at quad edge

                // Core mask (inner bright sun disc)
                float coreMask = 1.0 - smoothstep(_CoreRadius - _CoreSoftness, _CoreRadius + _CoreSoftness, dist);

                // Glow corona (outer soft radiation)
                float glowMask = saturate(1.0 - (dist / max(0.001, _GlowRadius)));
                glowMask = pow(glowMask, _GlowFalloff);

                // Subtle lower horizon atmospheric absorption
                float horizonFade = smoothstep(_HorizonFadeHeight - _HorizonFadeSoftness, _HorizonFadeHeight + _HorizonFadeSoftness, input.uv.y);
                float horizonFactor = lerp(0.55, 1.0, horizonFade);

                // Combine core and corona
                half3 col = (_CoreColor.rgb * coreMask + _GlowColor.rgb * glowMask) * horizonFactor;

                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
