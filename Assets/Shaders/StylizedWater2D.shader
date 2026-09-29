Shader "HoaVaMau/StylizedWater2D"
{
    Properties
    {
        [Header(Water Gradient Colors)]
        _ShallowColor ("Shallow / Near Shore Color", Color) = (0.16, 0.42, 0.48, 1.0)
        _DeepColor ("Deep Mid-Water Color", Color) = (0.08, 0.20, 0.30, 1.0)
        _HorizonWaterColor ("Horizon Water Color", Color) = (0.62, 0.38, 0.26, 1.0)
        _GradientPower ("Depth Gradient Power", Range(0.2, 4.0)) = 1.2
        _NearZ ("Gradient Near Z", Float) = 0.0
        _FarZ ("Gradient Far Z", Float) = 25.0

        [Header(Sun Reflection Highlight)]
        _SunReflectionColor ("Sun Reflection Color", Color) = (1.0, 0.82, 0.55, 1.0)
        _SunWorldX ("Sun World X", Float) = 0.0
        _ReflectionWidth ("Reflection Width", Range(0.2, 10.0)) = 3.0
        _ReflectionIntensity ("Reflection Intensity", Range(0.0, 5.0)) = 2.0
        _ReflectionFalloff ("Reflection Falloff", Range(0.5, 5.0)) = 2.0

        [Header(Procedural Wave And Ripple)]
        _WaveSpeed ("Wave Speed", Float) = 1.2
        _WaveFrequency ("Wave Frequency", Float) = 1.5
        _WaveHeight ("Wave Height", Range(0.0, 0.2)) = 0.02
        _RippleScale ("Ripple Scale", Float) = 6.0
        _RippleSpeed ("Ripple Speed", Float) = 1.0
        _RippleStrength ("Ripple Specular Breakup", Range(0.0, 1.0)) = 0.45

        [Header(Shoreline Foam)]
        _FoamColor ("Shore Foam Color", Color) = (0.95, 0.98, 1.0, 0.9)
        _FoamDepth ("Foam Edge Width", Range(0.0, 0.3)) = 0.08
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
        Cull Back

        Pass
        {
            Name "UniversalForward"
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
                float4 _DeepColor;
                float4 _HorizonWaterColor;
                float _GradientPower;
                float _NearZ;
                float _FarZ;

                float4 _SunReflectionColor;
                float _SunWorldX;
                float _ReflectionWidth;
                float _ReflectionIntensity;
                float _ReflectionFalloff;

                float _WaveSpeed;
                float _WaveFrequency;
                float _WaveHeight;
                float _RippleScale;
                float _RippleSpeed;
                float _RippleStrength;

                float4 _FoamColor;
                float _FoamDepth;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);

                // Gentle vertical wave animation
                float wave = sin(worldPos.x * _WaveFrequency + _Time.y * _WaveSpeed) 
                           * cos(worldPos.z * (_WaveFrequency * 0.4) + _Time.y * (_WaveSpeed * 0.7)) 
                           * _WaveHeight;
                
                worldPos.y += wave;

                output.positionCS = TransformWorldToHClip(worldPos);
                output.worldPos   = worldPos;
                output.uv         = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Depth progress (0 = near shore, 1 = far horizon)
                float depthFactor = saturate((input.worldPos.z - _NearZ) / max(0.001, _FarZ - _NearZ));
                float curveDepth = pow(depthFactor, _GradientPower);

                // Gradient from shallow to deep to horizon warm tint
                half3 waterColor = lerp(_ShallowColor.rgb, _DeepColor.rgb, saturate(curveDepth * 1.5));
                waterColor = lerp(waterColor, _HorizonWaterColor.rgb, smoothstep(0.35, 1.0, curveDepth));

                // Ripple procedural motion
                float timeVal = _Time.y * _RippleSpeed;
                float rippleA = sin((input.worldPos.x * _RippleScale) + timeVal * 1.8 + input.worldPos.z * 0.5);
                float rippleB = cos((input.worldPos.z * _RippleScale * 0.8) - timeVal * 1.4 + input.worldPos.x * 0.4);
                float ripplePattern = saturate((rippleA * rippleB + 1.0) * 0.5);

                // Sun reflection column along X
                float xDiff = abs(input.worldPos.x - _SunWorldX);
                float reflectionDistFactor = saturate(1.0 - (xDiff / max(0.01, _ReflectionWidth)));
                float reflectionColumn = pow(reflectionDistFactor, _ReflectionFalloff);

                // Modulate reflection with ripples and distance (more intense towards horizon)
                float rippleHighlight = lerp(1.0, ripplePattern * 1.8, _RippleStrength);
                float horizonGlance = smoothstep(0.1, 0.9, depthFactor);
                half3 sunHighlight = _SunReflectionColor.rgb * (reflectionColumn * _ReflectionIntensity * rippleHighlight * horizonGlance);

                // Combine water color and reflection
                half3 finalColor = waterColor + sunHighlight;

                // Shoreline foam edge
                if (_FoamDepth > 0.001 && _FoamColor.a > 0.001)
                {
                    float shoreT = saturate(input.uv.y / _FoamDepth);
                    float shoreWash = sin(input.worldPos.x * 2.5 + _Time.y * _WaveSpeed) * 0.12;
                    float foamFade = saturate((1.0 - shoreT) + shoreWash);
                    float foamAlpha = smoothstep(0.4, 0.95, foamFade) * _FoamColor.a;
                    finalColor = lerp(finalColor, _FoamColor.rgb, foamAlpha);
                }

                float alpha = lerp(_ShallowColor.a, _DeepColor.a, depthFactor);
                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
