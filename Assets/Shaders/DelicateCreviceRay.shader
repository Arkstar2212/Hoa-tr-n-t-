Shader "HoaVaMau/DelicateCreviceRay"
{
    Properties
    {
        [MainColor] [HDR] _RayColor ("Ray Color", Color) = (1.0, 0.90, 0.65, 0.45)
        _RayIntensity ("Intensity", Range(0.1, 5.0)) = 1.5
        _Feather ("Edge Feather", Range(0.1, 1.0)) = 0.6
        _ShimmerSpeed ("Shimmer Speed", Float) = 0.8
        _BottomSpread ("Bottom Spread", Range(1.0, 4.0)) = 2.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent+30" 
            "RenderPipeline" = "UniversalPipeline" 
        }

        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            Name "DelicateRayPass"
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
                float4 _RayColor;
                float _RayIntensity;
                float _Feather;
                float _ShimmerSpeed;
                float _BottomSpread;
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
                float2 uv = input.uv;

                // uv.y = 1.0 is top (crevice source), uv.y = 0.0 is bottom (ground hit)
                // Width gently expands downwards: narrow fissure at top, soft spread at ground
                float widthFactor = lerp(_BottomSpread, 1.0, uv.y);
                float distFromCenter = abs(uv.x - 0.5) * 2.0 / max(0.001, widthFactor);

                // Deep Gaussian falloff across beam width for pure soft watercolor gradient
                float horizontalFalloff = exp(-distFromCenter * distFromCenter * 5.0);

                // Smooth organic vertical falloff (zero hard edges at top or bottom)
                float topFade = smoothstep(0.0, 0.30, 1.0 - uv.y);
                float bottomFade = smoothstep(0.0, 0.25, uv.y);
                float verticalFalloff = topFade * bottomFade;

                // Gentle watercolor atmosphere shimmer
                float shimmer = 0.94 + 0.06 * sin(_Time.y * _ShimmerSpeed * 1.5 + uv.y * 4.0 + uv.x * 3.0);

                float alpha = horizontalFalloff * verticalFalloff * shimmer * _RayColor.a;
                half3 finalColor = _RayColor.rgb * _RayIntensity * alpha;

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
