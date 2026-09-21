Shader "HoaVaMau/SkyGradient"
{
    Properties
    {
        _MainTex ("Gradient Texture (Optional)", 2D) = "white" {}
        _SkyZenithColor ("Sky Zenith Color (Top)", Color) = (0.11, 0.56, 0.64, 1.0)
        _SkyHorizonColor ("Sky Horizon Color (Bottom)", Color) = (0.60, 0.86, 0.90, 1.0)
        _HorizonOffset ("Horizon Offset", Range(-0.5, 0.5)) = 0.0
        _GradientPower ("Gradient Power", Range(0.2, 3.0)) = 1.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Opaque" 
            "Queue"="Background" 
            "RenderPipeline"="UniversalPipeline" 
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
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 vertexColor : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _SkyZenithColor;
                float4 _SkyHorizonColor;
                float _HorizonOffset;
                float _GradientPower;
            CBUFFER_END

            // Global environment overrides from EnvironmentManager
            float4 _GlobalSkyZenith;
            float4 _GlobalSkyHorizon;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.vertexColor = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float t = saturate(input.uv.y - _HorizonOffset);
                t = pow(t, _GradientPower);

                half4 zenith = length(_GlobalSkyZenith.rgb) > 0.05 ? _GlobalSkyZenith : _SkyZenithColor;
                half4 horizon = length(_GlobalSkyHorizon.rgb) > 0.05 ? _GlobalSkyHorizon : _SkyHorizonColor;

                half4 skyCol = lerp(horizon, zenith, t);
                skyCol.rgb *= input.vertexColor.rgb;

                return skyCol;
            }
            ENDHLSL
        }
    }
}
