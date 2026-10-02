Shader "HoaVaMau/StylizedGodRays"
{
    Properties
    {
        [MainColor] [HDR] _RayColor ("Ray Color", Color) = (1.0, 0.82, 0.45, 0.35)
        _RayIntensity ("Intensity", Range(0.1, 5.0)) = 1.0
        _ShimmerSpeed ("Shimmer Speed", Float) = 0.6
        _FalloffPower ("Falloff Power", Range(0.5, 4.0)) = 1.8
        _BeamCount ("Beam Count", Float) = 4.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent+20" 
            "RenderPipeline" = "UniversalPipeline" 
        }

        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {
            Name "GodRaysForward"
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

            CBUFFER_START(UnityPerMaterial)
                float4 _RayColor;
                float _RayIntensity;
                float _ShimmerSpeed;
                float _FalloffPower;
                float _BeamCount;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;

                // Fan out beams radiating naturally from bottom sun center
                float fanX = (uv.x - 0.5) / max(0.25, uv.y * 0.75 + 0.25) + 0.5;

                // Procedural beam stripes
                float beams = sin(fanX * _BeamCount * 3.14159) * 0.5 + 0.5;
                beams = pow(beams, 1.8);

                // Secondary subtle shimmering beams
                float secondary = sin(fanX * (_BeamCount * 2.1) + _Time.y * _ShimmerSpeed) * 0.5 + 0.5;
                beams = beams * 0.65 + secondary * 0.35;

                // Soft horizontal edge fade
                float edgeFade = smoothstep(0.0, 0.25, uv.x) * smoothstep(1.0, 0.75, uv.x);

                // Soft vertical falloff: fades in from sun at bottom, fades out smoothly into sky at top
                float yFalloff = smoothstep(0.0, 0.20, uv.y) * smoothstep(1.0, 0.55, uv.y);

                // Subtle breathing shimmer
                float shimmer = 0.92 + 0.08 * sin(_Time.y * _ShimmerSpeed * 2.0 + uv.x * 4.0);

                float alpha = beams * edgeFade * yFalloff * shimmer * _RayColor.a;

                half3 finalRGB = _RayColor.rgb * _RayIntensity * alpha;
                return half4(finalRGB, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}