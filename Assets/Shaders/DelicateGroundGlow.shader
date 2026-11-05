Shader "HoaVaMau/DelicateGroundGlow"
{
    Properties
    {
        [MainColor] [HDR] _GlowColor ("Glow Color", Color) = (1.0, 0.88, 0.60, 0.40)
        _Intensity ("Intensity", Range(0.1, 4.0)) = 1.2
        _Feather ("Edge Softness", Range(0.5, 4.0)) = 2.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent+25" 
            "RenderPipeline" = "UniversalPipeline" 
        }

        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            Name "GroundGlowPass"
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
                float4 _GlowColor;
                float _Intensity;
                float _Feather;
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
                float2 centeredUV = (input.uv - 0.5) * 2.0; // [-1, 1]
                float distSq = dot(centeredUV, centeredUV);

                float falloff = exp(-distSq * _Feather);
                float edgeFade = smoothstep(1.0, 0.7, length(centeredUV));

                float alpha = falloff * edgeFade * _GlowColor.a;
                half3 col = _GlowColor.rgb * _Intensity * alpha;

                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
