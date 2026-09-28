Shader "HoaVaMau/StylizedSky2D"
{
    Properties
    {
        [Header(Sky Colors)]
        _ZenithColor ("Zenith Color (Top)", Color) = (0.16, 0.14, 0.46, 1.0)
        _MidSkyColor ("Mid Sky Color", Color) = (0.84, 0.36, 0.30, 1.0)
        _HorizonColor ("Horizon Color (Bottom)", Color) = (1.0, 0.72, 0.35, 1.0)

        [Header(World Height Bounds)]
        _HorizonY ("Horizon World Y", Float) = 1.35
        _ZenithY ("Zenith World Y", Float) = 6.60
        _MidPosition ("Mid Color Position (0-1)", Range(0.1, 0.9)) = 0.40
        _ZenithPower ("Zenith Falloff Power", Range(0.2, 4.0)) = 1.2
        _HorizonPower ("Horizon Falloff Power", Range(0.2, 4.0)) = 1.3

        [Header(Horizon Atmospheric Band)]
        _HorizonBandColor ("Horizon Warm Glow Band", Color) = (1.0, 0.88, 0.52, 1.0)
        _HorizonBandHeight ("Band Center (World Y)", Float) = 1.55
        _HorizonBandWidth ("Band Width", Range(0.1, 2.0)) = 0.85
        _HorizonBandIntensity ("Band Intensity", Range(0.0, 2.0)) = 0.75
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "Queue" = "Background" 
            "RenderPipeline" = "UniversalPipeline" 
        }

        Cull Off
        ZWrite Off

        Pass
        {
            Name "SkyForward"
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
                float3 worldPos   : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _ZenithColor;
                float4 _MidSkyColor;
                float4 _HorizonColor;
                float _HorizonY;
                float _ZenithY;
                float _MidPosition;
                float _ZenithPower;
                float _HorizonPower;
                float4 _HorizonBandColor;
                float _HorizonBandHeight;
                float _HorizonBandWidth;
                float _HorizonBandIntensity;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(worldPos);
                output.worldPos   = worldPos;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float t = saturate((input.worldPos.y - _HorizonY) / max(0.001, _ZenithY - _HorizonY));

                half3 col;
                if (t < _MidPosition)
                {
                    float localT = t / max(0.001, _MidPosition);
                    localT = pow(localT, _HorizonPower);
                    col = lerp(_HorizonColor.rgb, _MidSkyColor.rgb, localT);
                }
                else
                {
                    float localT = (t - _MidPosition) / max(0.001, 1.0 - _MidPosition);
                    localT = pow(localT, _ZenithPower);
                    col = lerp(_MidSkyColor.rgb, _ZenithColor.rgb, localT);
                }

                // Extra warm glow band along the horizon
                float bandDist = abs(input.worldPos.y - _HorizonBandHeight);
                float bandFactor = saturate(1.0 - (bandDist / max(0.001, _HorizonBandWidth)));
                bandFactor = smoothstep(0.0, 1.0, bandFactor);
                col += _HorizonBandColor.rgb * (bandFactor * _HorizonBandIntensity);

                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
