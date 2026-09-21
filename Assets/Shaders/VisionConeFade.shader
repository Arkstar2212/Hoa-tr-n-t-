Shader "HoaVaMau/VisionConeFade"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.28, 0.80, 0.95, 0.35)
    }
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "RenderPipeline"="UniversalPipeline" 
        }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color * _BaseColor;
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // input.uv.y is 0 at enemy eye (origin), 1 at perimeter/obstacles
                // Smooth volumetric distance fade: brightest near eye, smoothly fading to transparent at perimeter
                float distFade = 1.0 - smoothstep(0.05, 0.98, input.uv.y);

                // Angular side edge fade: soft lateral boundaries
                float lateralDist = abs(input.uv.x - 0.5) * 2.0; // 0 at center axis, 1 at side edges
                float sideFade = 1.0 - smoothstep(0.4, 0.98, lateralDist);

                half4 finalColor = input.color;
                finalColor.a *= (distFade * sideFade);
                return finalColor;
            }
            ENDHLSL
        }
    }
    FallBack "Sprites/Default"
}
