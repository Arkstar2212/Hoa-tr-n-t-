Shader "HoaVaMau/StylizedWater_DredgeDave"
{
    Properties
    {
        [Header(Water Gradient Colors)]
        _ShallowColor ("Shallow Color (Turquoise)", Color) = (0.16, 0.72, 0.80, 0.75)
        _DeepColor ("Deep Color (Ocean Navy)", Color) = (0.05, 0.22, 0.38, 0.95)
        _DepthDistance ("Depth Fade Distance", Range(0.1, 20.0)) = 4.0

        [Header(Dredge Stylized Gerstner Waves)]
        _WaveSpeed ("Wave Speed", Float) = 1.2
        _WaveHeight ("Wave Height Multiplier", Range(0.0, 2.0)) = 0.35
        _WaveA ("Wave A (DirX, DirZ, Steepness, Wavelength)", Vector) = (1.0, 0.3, 0.45, 12.0)
        _WaveB ("Wave B (DirX, DirZ, Steepness, Wavelength)", Vector) = (0.4, 0.9, 0.35, 7.5)
        _WaveC ("Wave C (DirX, DirZ, Steepness, Wavelength)", Vector) = (-0.6, 0.8, 0.25, 4.0)

        [Header(Contact Foam Edge)]
        _FoamColor ("Foam Color", Color) = (0.92, 0.98, 1.0, 0.95)
        _FoamDistance ("Foam Contact Width", Range(0.01, 2.0)) = 0.45
        _FoamCutoff ("Foam Noise Modulation", Range(0.0, 1.0)) = 0.35
        _FoamScale ("Foam Texture Scale", Float) = 3.5

        [Header(Sun Specular And Sparkle)]
        _SpecularColor ("Sun Specular Color", Color) = (1.0, 0.95, 0.80, 1.0)
        _SpecularPower ("Specular Gloss Power", Range(4.0, 256.0)) = 64.0
        _SpecularIntensity ("Specular Intensity", Range(0.0, 5.0)) = 2.5
        _SparkleScale ("Sparkle Scale", Float) = 15.0
        _SparkleSpeed ("Sparkle Speed", Float) = 1.5

        [Header(Surface Caustics and Normal)]
        _CausticColor ("Surface Ripple Tint", Color) = (0.4, 0.9, 1.0, 0.3)
        _RippleScale ("Ripple Distortion Scale", Float) = 2.5
        _RippleSpeed ("Ripple Distortion Speed", Float) = 0.8
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent-100" 
            "RenderPipeline" = "UniversalPipeline" 
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float4 screenPos    : TEXCOORD1;
                float3 worldPos     : TEXCOORD2;
                float3 worldNormal  : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor;
                float4 _DeepColor;
                float _DepthDistance;

                float _WaveSpeed;
                float _WaveHeight;
                float4 _WaveA;
                float4 _WaveB;
                float4 _WaveC;

                float4 _FoamColor;
                float _FoamDistance;
                float _FoamCutoff;
                float _FoamScale;

                float4 _SpecularColor;
                float _SpecularPower;
                float _SpecularIntensity;
                float _SparkleScale;
                float _SparkleSpeed;

                float4 _CausticColor;
                float _RippleScale;
                float _RippleSpeed;
            CBUFFER_END

            // Gerstner Wave math matching C# Buoyancy Controller
            float3 GerstnerWave(float4 wave, float3 p, inout float3 tangent, inout float3 binormal, float timeVal)
            {
                float2 dir = normalize(wave.xy);
                float wavelength = max(0.01, wave.w);
                float k = 2.0 * PI / wavelength;
                float c = sqrt(9.8 / k);
                float f = k * (dot(dir, p.xz) - c * timeVal * _WaveSpeed);
                float a = wave.z / k * _WaveHeight;

                tangent += float3(
                    -dir.x * dir.x * (wave.z * sin(f)),
                    dir.x * (wave.z * cos(f)),
                    -dir.x * dir.y * (wave.z * sin(f))
                );

                binormal += float3(
                    -dir.x * dir.y * (wave.z * sin(f)),
                    dir.y * (wave.z * cos(f)),
                    -dir.y * dir.y * (wave.z * sin(f))
                );

                return float3(
                    dir.x * (a * cos(f)),
                    a * sin(f),
                    dir.y * (a * cos(f))
                );
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);

                float3 tangent = float3(1, 0, 0);
                float3 binormal = float3(0, 0, 1);
                float3 p = worldPos;

                float timeVal = _Time.y;
                p += GerstnerWave(_WaveA, worldPos, tangent, binormal, timeVal);
                p += GerstnerWave(_WaveB, worldPos, tangent, binormal, timeVal);
                p += GerstnerWave(_WaveC, worldPos, tangent, binormal, timeVal);

                float3 normal = normalize(cross(binormal, tangent));

                output.positionCS = TransformWorldToHClip(p);
                output.screenPos  = ComputeScreenPos(output.positionCS);
                output.worldPos   = p;
                output.worldNormal = normal;
                output.uv         = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 1. Water Depth Calculation (Screen Depth vs Surface Depth)
                float2 screenUV = input.screenPos.xy / max(0.0001, input.screenPos.w);
                #if UNITY_REVERSED_Z
                    float rawDepth = SampleSceneDepth(screenUV);
                #else
                    float rawDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, SampleSceneDepth(screenUV));
                #endif

                float linearSceneDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                float surfaceDepth = input.screenPos.w;
                float depthDifference = linearSceneDepth - surfaceDepth;

                // Handle sky or unwritten depth buffer gracefully
                bool isValidDepth = (depthDifference > 0.001 && depthDifference < 250.0);
                float depthFactor = isValidDepth ? saturate(depthDifference / max(0.01, _DepthDistance)) : 0.85;
                half3 waterColor = lerp(_ShallowColor.rgb, _DeepColor.rgb, depthFactor);

                // 2. Dredge-Style Shoreline and Object Contact Foam (Only near intersections!)
                float foamMask = 0.0;
                if (isValidDepth && depthDifference < _FoamDistance)
                {
                    float foamGradient = 1.0 - (depthDifference / _FoamDistance);
                    float foamNoiseA = sin(input.worldPos.x * _FoamScale + _Time.y * 1.5);
                    float foamNoiseB = cos(input.worldPos.z * _FoamScale * 1.2 - _Time.y * 1.2);
                    float foamNoise = (foamNoiseA * foamNoiseB + 1.0) * 0.5;
                    foamMask = smoothstep(0.3, 0.7, foamGradient + (foamNoise - 0.5) * _FoamCutoff);
                    waterColor = lerp(waterColor, _FoamColor.rgb, foamMask * _FoamColor.a);
                }

                // 3. Stylized Caustic Ripples on Surface
                float2 rippleUV1 = input.worldPos.xz * _RippleScale + float2(_Time.y * _RippleSpeed * 0.4, _Time.y * _RippleSpeed * 0.3);
                float2 rippleUV2 = input.worldPos.xz * (_RippleScale * 1.3) - float2(_Time.y * _RippleSpeed * 0.3, _Time.y * _RippleSpeed * 0.5);
                float caust1 = sin(rippleUV1.x + sin(rippleUV1.y));
                float caust2 = cos(rippleUV2.x - cos(rippleUV2.y));
                float caustPattern = saturate(pow((caust1 + caust2) * 0.5 + 0.5, 3.0));
                waterColor += _CausticColor.rgb * (caustPattern * _CausticColor.a * (1.0 - foamMask));

                // 4. Sun Specular & Glitter on Wave Crests
                Light mainLight = GetMainLight();
                float3 viewDir = normalize(GetCameraPositionWS() - input.worldPos);
                float3 halfVec = normalize(mainLight.direction + viewDir);

                // Sharp stylized specular highlight
                float NdotH = saturate(dot(normalize(input.worldNormal), halfVec));
                float specular = pow(NdotH, _SpecularPower);
                
                // Fine water sparkles
                float sparkleNoise = sin(input.worldPos.x * _SparkleScale + _Time.y * _SparkleSpeed) 
                                   * cos(input.worldPos.z * _SparkleScale - _Time.y * _SparkleSpeed * 1.2);
                float sparkle = saturate(pow(saturate(sparkleNoise), 6.0) * 3.0);
                
                half3 specFinal = (specular * _SpecularIntensity + sparkle * 0.6) * _SpecularColor.rgb * mainLight.color;
                waterColor += specFinal;

                // 5. Overall Alpha
                float alpha = lerp(_ShallowColor.a, _DeepColor.a, depthFactor);
                alpha = max(alpha, foamMask * _FoamColor.a);

                return half4(waterColor, alpha);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
