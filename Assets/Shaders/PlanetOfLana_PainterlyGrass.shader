Shader "HoaVaMau/PlanetOfLana_PainterlyGrass"
{
    Properties
    {
        _MainTex ("Grass Sprite or Texture", 2D) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.08

        _BaseShadowColor ("Root Shadow (Dark Forest Green)", Color) = (0.09, 0.18, 0.09, 1.0)
        _MidBladeColor   ("Mid Blade (Meadow Green)", Color)        = (0.38, 0.58, 0.20, 1.0)
        _TipSunColor     ("Sunlit Tips (Creamy Sunlight Gold)", Color) = (0.86, 0.95, 0.52, 1.0)
        _SunbeamGlintColor ("Dappled Sunbeam Glint", Color)         = (1.00, 0.98, 0.72, 1.0)
        _ColorGradientStrength ("Gradient Tint Strength", Range(0.0, 1.0)) = 0.75

        _DappleScale     ("Sunbeam Wave Scale", Float) = 0.12
        _DappleSpeed     ("Sunbeam Wave Speed", Float) = 0.70
        _DappleIntensity ("Sunbeam Ripple Intensity", Range(0.0, 1.0)) = 0.45

        _WindSpeed       ("Wind Wave Speed", Float) = 1.60
        _WindFrequency   ("Wind Wave Frequency", Float) = 0.50
        _WindSwayStrength ("Wind Sway Strength", Range(0.0, 1.0)) = 0.16
        _TipFlutterSpeed ("Tip Flutter Speed", Float) = 7.50
        _TipFlutterStrength ("Tip Flutter Strength", Range(0.0, 0.2)) = 0.035

        _NormalUpFactor  ("Normal Upward Smoothness", Range(0.0, 1.0)) = 0.85
        _AmbientLightMin ("Ambient Light Floor", Range(0.2, 1.0)) = 0.65
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest+50"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        ZWrite On

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalOS   : NORMAL;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 normalWS     : NORMAL;
                float3 positionWS   : TEXCOORD1;
                float4 vertexColor  : COLOR;
                float  height       : TEXCOORD2;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float  _Cutoff;
                float4 _BaseShadowColor;
                float4 _MidBladeColor;
                float4 _TipSunColor;
                float4 _SunbeamGlintColor;
                float  _ColorGradientStrength;
                float  _DappleScale;
                float  _DappleSpeed;
                float  _DappleIntensity;
                float  _WindSpeed;
                float  _WindFrequency;
                float  _WindSwayStrength;
                float  _TipFlutterSpeed;
                float  _TipFlutterStrength;
                float  _NormalUpFactor;
                float  _AmbientLightMin;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.vertexColor = input.color;

                // Height anchor: 0 at roots, 1 at blade tips
                float h = saturate(input.uv.y);
                output.height = h;
                float bend = h * h; // Quadratic anchoring

                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);

                // Coherent wind wave propagating across terrain
                float wavePhase = worldPos.x * _WindFrequency + worldPos.z * (_WindFrequency * 0.4) - _Time.y * _WindSpeed;
                float macroWave = sin(wavePhase);

                // Tip micro-flutter
                float microFlutter = sin(_Time.y * _TipFlutterSpeed + worldPos.x * 2.8) * _TipFlutterStrength;

                // Total displacement
                float totalDisplacement = (macroWave * _WindSwayStrength + microFlutter) * bend;

                worldPos.x += totalDisplacement;
                worldPos.y -= abs(totalDisplacement) * 0.12 * bend;
                worldPos.z += totalDisplacement * 0.25;

                output.positionCS = TransformWorldToHClip(worldPos);
                output.positionWS = worldPos;

                // Upward-biased velvet normal
                float3 worldNormal = TransformObjectToWorldNormal(input.normalOS);
                output.normalWS = normalize(lerp(worldNormal, float3(0, 1, 0), _NormalUpFactor));

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                clip(texColor.a * input.vertexColor.a - _Cutoff);

                float h = input.height;

                // 3-Stop Painterly Gradient Tint
                half3 gradientColor;
                if (h < 0.45)
                {
                    gradientColor = lerp(_BaseShadowColor.rgb, _MidBladeColor.rgb, h / 0.45);
                }
                else
                {
                    gradientColor = lerp(_MidBladeColor.rgb, _TipSunColor.rgb, (h - 0.45) / 0.55);
                }

                // Procedural dappled sunlight wave drifting across meadow
                float dapplePhase = input.positionWS.x * _DappleScale + input.positionWS.z * (_DappleScale * 0.6) - _Time.y * _DappleSpeed;
                float dappleWave = sin(dapplePhase);
                float dappleMask = smoothstep(-0.2, 0.8, dappleWave) * _DappleIntensity * h;

                // Apply sunbeam glint
                gradientColor = lerp(gradientColor, _SunbeamGlintColor.rgb, dappleMask);

                // Blend original sprite texture with the painterly gradient
                half3 blendedColor = lerp(texColor.rgb, texColor.rgb * gradientColor * 1.5, _ColorGradientStrength);

                // Modulate by vertex color
                blendedColor *= input.vertexColor.rgb;

                // Soft wrap lighting from main light
                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(input.normalWS, mainLight.direction));
                half3 lightFactor = mainLight.color * lerp(_AmbientLightMin, 1.0, NdotL);

                half3 finalColor = blendedColor * lightFactor;
                return half4(finalColor, texColor.a * input.vertexColor.a);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
