Shader "HoaVaMau/LanaStylizedFlora"
{
    Properties
    {
        _BaseColor ("Base Color (Root / Shadow)", Color) = (0.26, 0.39, 0.23, 1)
        _TipColor ("Tip Color (Sunlit Top / Seed)", Color) = (0.48, 0.64, 0.36, 1)
        _WindSpeed ("Wind Sway Speed", Float) = 1.6
        _WindStrength ("Wind Sway Amplitude", Float) = 0.08
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry+10"
        }

        Pass
        {
            Name "ForwardFlora"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float4 color : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _TipColor;
                float _WindSpeed;
                float _WindStrength;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);

                // Gentle Planet of Lana breeze sway based on vertex color R (height factor 0..1)
                float swayMask = IN.color.r;
                float wave = sin(_Time.y * _WindSpeed + posWS.x * 0.85 + posWS.z * 0.6) * _WindStrength * swayMask;
                posWS.x += wave;
                posWS.y -= abs(wave) * 0.15;

                OUT.positionHCS = TransformWorldToHClip(posWS);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                Light mainLight = GetMainLight();
                float3 N = normalize(IN.normalWS);
                float3 L = normalize(mainLight.direction);
                float ndotl = saturate(dot(N, L) * 0.4 + 0.65);

                // IN.color.g blends between _BaseColor (0) and _TipColor (1)
                half3 col = lerp(_BaseColor.rgb, _TipColor.rgb, saturate(IN.color.g));
                // If IN.color.b > 0.5, use petal/accent override (white daisy / lavender bloom)
                if (IN.color.b > 0.6)
                {
                    col = lerp(col, half3(0.98, 0.96, 0.88), smoothstep(0.6, 0.85, IN.color.b));
                }
                else if (IN.color.b > 0.25)
                {
                    col = lerp(col, half3(0.62, 0.54, 0.82), smoothstep(0.25, 0.5, IN.color.b));
                }

                return half4(col * ndotl, 1.0);
            }
            ENDHLSL
        }
    }
}
