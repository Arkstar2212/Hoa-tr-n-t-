Shader "HoaVaMau/Dredge_SkyGradient"
{
    Properties
    {
        _ZenithColor ("Zenith Color (Top Indigo)", Color) = (0.16, 0.10, 0.26, 1.0)
        _UpperMidColor ("Upper Mid (Dusk Plum)", Color) = (0.50, 0.18, 0.32, 1.0)
        _LowerMidColor ("Lower Mid (Sunset Orange)", Color) = (0.92, 0.36, 0.16, 1.0)
        _HorizonColor ("Horizon Color (Sunset Gold)", Color) = (1.00, 0.68, 0.28, 1.0)
        _HorizonUV ("Horizon UV Y", Range(0.0, 1.0)) = 0.42
        _CurvePower ("Gradient Curve", Range(0.5, 3.0)) = 1.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Opaque" 
            "Queue"="Geometry-500" 
            "RenderPipeline"="UniversalPipeline" 
        }

        Cull Off
        ZWrite Off

        Pass
        {
            Name "SkyPass"
            Tags { "LightMode"="UniversalForward" }

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
                float4 _ZenithColor;
                float4 _UpperMidColor;
                float4 _LowerMidColor;
                float4 _HorizonColor;
                float _HorizonUV;
                float _CurvePower;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv         = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float y = input.uv.y;
                if (y < _HorizonUV)
                {
                    return _HorizonColor;
                }

                float t = saturate((y - _HorizonUV) / max(0.001, 1.0 - _HorizonUV));
                t = pow(t, _CurvePower);

                half3 col;
                if (t < 0.28)
                {
                    col = lerp(_HorizonColor.rgb, _LowerMidColor.rgb, t / 0.28);
                }
                else if (t < 0.65)
                {
                    col = lerp(_LowerMidColor.rgb, _UpperMidColor.rgb, (t - 0.28) / 0.37);
                }
                else
                {
                    col = lerp(_UpperMidColor.rgb, _ZenithColor.rgb, (t - 0.65) / 0.35);
                }

                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
