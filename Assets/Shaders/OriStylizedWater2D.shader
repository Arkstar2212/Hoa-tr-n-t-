Shader "HoaVaMau/OriStylizedWater2D"
{
    Properties
    {
        [Header(Water Colors Ca Mau Palette)]
        _ShallowColor ("Surface / Shallow Color", Color) = (0.10, 0.72, 0.82, 0.88)
        _MidColor ("Mid-Depth Color", Color) = (0.05, 0.42, 0.58, 0.94)
        _DeepColor ("Abyssal / Deep Color", Color) = (0.02, 0.12, 0.20, 0.98)
        _DepthGradientPower ("Depth Gradient Power", Range(0.5, 3.0)) = 1.3

        [Header(Ori Luminous Water Line)]
        _SurfaceLineColor ("Glow Crest Line Color", Color) = (0.45, 1.80, 1.95, 1.0)
        _SurfaceLineWidth ("Glow Line Width", Range(0.005, 0.20)) = 0.045
        _SurfaceGlowIntensity ("Glow Intensity", Range(1.0, 5.0)) = 2.6
        _SurfacePulseSpeed ("Line Pulse Speed", Float) = 1.5

        [Header(Sun Reflection Highlight)]
        _SunReflectionColor ("Sun Reflection Color", Color) = (2.2, 1.90, 1.35, 1.0)
        _SunWorldX ("Sun World X Position", Float) = 4.5
        _ReflectionWidth ("Reflection Pillar Width", Range(0.5, 15.0)) = 5.0
        _ReflectionIntensity ("Reflection Intensity", Range(0.0, 8.0)) = 3.8
        _ReflectionFalloff ("Reflection Falloff Power", Range(0.5, 5.0)) = 2.2
        _SparkleScale ("Sparkle Scale", Float) = 18.0
        _SparkleSpeed ("Sparkle Speed", Float) = 3.2
        _SparkleStrength ("Sparkle Strength", Range(0.0, 1.0)) = 0.65

        [Header(Underwater Caustics And Sun Shafts)]
        _CausticColor ("Underwater Sunbeam Color", Color) = (0.35, 1.10, 1.25, 0.45)
        _CausticScale ("Caustic Width Scale", Float) = 4.5
        _CausticSpeed ("Caustic Shift Speed", Float) = 1.1
        _CausticDepthFade ("Caustic Depth Fade", Range(0.2, 3.0)) = 1.5

        [Header(Wave Vertex Animation)]
        _WaveSpeed ("Wave Speed", Float) = 1.4
        _WaveFrequency ("Wave Frequency", Float) = 0.85
        _WaveHeight ("Wave Height", Range(0.0, 0.3)) = 0.035
        _SecondaryWaveSpeed ("Secondary Wave Speed", Float) = 2.1
        _SecondaryWaveFreq ("Secondary Wave Frequency", Float) = 1.7
        _SecondaryWaveHeight ("Secondary Wave Height", Range(0.0, 0.15)) = 0.015
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent-10" 
            "RenderPipeline" = "UniversalPipeline" 
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "OriWaterForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 worldPos   : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor;
                float4 _MidColor;
                float4 _DeepColor;
                float _DepthGradientPower;

                float4 _SurfaceLineColor;
                float _SurfaceLineWidth;
                float _SurfaceGlowIntensity;
                float _SurfacePulseSpeed;

                float4 _SunReflectionColor;
                float _SunWorldX;
                float _ReflectionWidth;
                float _ReflectionIntensity;
                float _ReflectionFalloff;
                float _SparkleScale;
                float _SparkleSpeed;
                float _SparkleStrength;

                float4 _CausticColor;
                float _CausticScale;
                float _CausticSpeed;
                float _CausticDepthFade;

                float _WaveSpeed;
                float _WaveFrequency;
                float _WaveHeight;
                float _SecondaryWaveSpeed;
                float _SecondaryWaveFreq;
                float _SecondaryWaveHeight;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);

                // Displace upper vertices to create organic undulating crests (Ori water line)
                float crestWeight = smoothstep(0.4, 1.0, input.uv.y);
                float waveMain = sin(worldPos.x * _WaveFrequency + _Time.y * _WaveSpeed) * _WaveHeight;
                float waveSec  = cos(worldPos.x * _SecondaryWaveFreq + _Time.y * _SecondaryWaveSpeed + 1.2) * _SecondaryWaveHeight;
                float totalWave = (waveMain + waveSec) * crestWeight;

                worldPos.y += totalWave;

                output.positionCS = TransformWorldToHClip(worldPos);
                output.worldPos   = worldPos;
                output.uv         = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Vertical gradient (1.0 = surface, 0.0 = deep bottom)
                float surfaceT = saturate(input.uv.y);
                float depthFactor = 1.0 - surfaceT;
                float curvedDepth = pow(depthFactor, _DepthGradientPower);

                // Tri-color oceanic gradient (matching Ca Mau crystal waters)
                half3 waterColor = lerp(_ShallowColor.rgb, _MidColor.rgb, saturate(curvedDepth * 1.6));
                waterColor = lerp(waterColor, _DeepColor.rgb, smoothstep(0.4, 1.0, curvedDepth));

                // -------------------------------------------------------------
                // 1. UNDERWATER CAUSTICS & SUN SHAFTS (Ori Subsurface Shafts)
                // -------------------------------------------------------------
                float causticTime = _Time.y * _CausticSpeed;
                float shaftA = sin(input.worldPos.x * _CausticScale + input.uv.y * 2.2 + causticTime);
                float shaftB = cos(input.worldPos.x * (_CausticScale * 0.7) - input.uv.y * 3.1 - causticTime * 1.3);
                float caustics = saturate((shaftA * shaftB + 1.0) * 0.5);
                caustics = pow(caustics, 3.0); // sharp sun rays
                float causticFade = pow(surfaceT, _CausticDepthFade); // stronger near surface, fades downwards
                half3 underwaterRays = _CausticColor.rgb * (caustics * _CausticColor.a * causticFade * 1.8);
                waterColor += underwaterRays;

                // -------------------------------------------------------------
                // 2. SUN REFLECTION PILLAR (Reflecting the Sun on Water)
                // -------------------------------------------------------------
                float xDist = abs(input.worldPos.x - _SunWorldX);
                float reflectionDist = saturate(1.0 - (xDist / max(0.01, _ReflectionWidth)));
                float reflectionPillar = pow(reflectionDist, _ReflectionFalloff);

                // Sparkling high frequency noise along the reflection
                float spkTime = _Time.y * _SparkleSpeed;
                float spkA = sin(input.worldPos.x * _SparkleScale + spkTime * 2.5);
                float spkB = cos(input.worldPos.y * (_SparkleScale * 1.5) - spkTime * 1.8);
                float sparkleNoise = saturate((spkA * spkB + 1.0) * 0.5);
                sparkleNoise = pow(sparkleNoise, 4.0);

                float sparkleFactor = lerp(1.0, sparkleNoise * 2.5, _SparkleStrength);
                // Reflection is most brilliant along the top water surface and spreads down gently
                float surfaceReflectionBias = pow(surfaceT, 1.5);
                half3 sunReflection = _SunReflectionColor.rgb * (reflectionPillar * _ReflectionIntensity * sparkleFactor * surfaceReflectionBias);
                waterColor += sunReflection;

                // -------------------------------------------------------------
                // 3. ORI SIGNATURE LUMINOUS WATER SURFACE LINE (Glowing Crest)
                // -------------------------------------------------------------
                float lineMask = smoothstep(1.0 - _SurfaceLineWidth, 1.0, surfaceT);
                if (lineMask > 0.001)
                {
                    // Gentle breathing pulse on the crest glow
                    float pulse = 1.0 + 0.15 * sin(_Time.y * _SurfacePulseSpeed + input.worldPos.x * 0.5);
                    half3 glowColor = _SurfaceLineColor.rgb * (_SurfaceGlowIntensity * pulse);

                    // Add sparkling highlights directly on the line where sun hits
                    glowColor += _SunReflectionColor.rgb * (reflectionPillar * 2.0);

                    waterColor = lerp(waterColor, glowColor, lineMask);
                }

                // Final alpha blend
                float alpha = lerp(_DeepColor.a, _ShallowColor.a, surfaceT);
                alpha = max(alpha, lineMask); // Always make the glow crest 100% solid/vibrant

                return half4(waterColor, alpha);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
