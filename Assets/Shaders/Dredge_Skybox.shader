Shader "HoaVaMau/Dredge_Skybox"
{
    Properties
    {
        _ZenithColor ("Zenith Color (Twilight Indigo)", Color) = (0.16, 0.10, 0.26, 1.0)
        _UpperMidColor ("Upper Mid (Dusk Plum)", Color) = (0.50, 0.18, 0.32, 1.0)
        _LowerMidColor ("Lower Mid (Sunset Orange)", Color) = (0.92, 0.36, 0.16, 1.0)
        _HorizonColor ("Horizon Color (Sunset Gold)", Color) = (1.00, 0.68, 0.28, 1.0)
        _Exponent ("Gradient Curve", Range(0.5, 3.0)) = 1.0
    }

    SubShader
    {
        Tags 
        { 
            "Queue" = "Background" 
            "RenderType" = "Background" 
            "PreviewType" = "Skybox" 
        }

        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 texcoord   : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _ZenithColor;
                float4 _UpperMidColor;
                float4 _LowerMidColor;
                float4 _HorizonColor;
                float _Exponent;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.texcoord   = input.positionOS.xyz;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 dir = normalize(input.texcoord);
                float y = saturate(dir.y);
                y = pow(y, _Exponent);

                half3 col;
                if (y < 0.22)
                {
                    col = lerp(_HorizonColor.rgb, _LowerMidColor.rgb, y / 0.22);
                }
                else if (y < 0.58)
                {
                    col = lerp(_LowerMidColor.rgb, _UpperMidColor.rgb, (y - 0.22) / 0.36);
                }
                else
                {
                    col = lerp(_UpperMidColor.rgb, _ZenithColor.rgb, (y - 0.58) / 0.42);
                }

                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
