Shader "HoaVaMau/LanaAtmosphericHaze"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _HazeColor ("Haze Tint (Pale Cyan/Cobalt)", Color) = (0.72, 0.86, 0.92, 0.35)
        _BottomFade ("Bottom Ground Fade", Range(0.0, 0.5)) = 0.15
        _TopFade ("Top Sky Fade", Range(0.5, 1.0)) = 0.85
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+10"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Forward"
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
                float4 _HazeColor;
                float  _BottomFade;
                float  _TopFade;
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
                float v = input.uv.y;
                float alpha = smoothstep(0.0, _BottomFade, v) * smoothstep(1.0, _TopFade, v) * _HazeColor.a;
                return half4(_HazeColor.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
