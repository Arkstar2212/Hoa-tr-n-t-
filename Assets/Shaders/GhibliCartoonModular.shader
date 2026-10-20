Shader "HoaVaMau/GhibliCartoonModular"
{
    Properties
    {
        [Header(Ghibli Color Palette)]
        _BaseColor          ("Main Surface Color (Lit)", Color) = (0.58, 0.39, 0.25, 1.0)
        _MidtoneColor       ("Midtone Warm Color", Color) = (0.45, 0.29, 0.19, 1.0)
        _ShadowColor        ("Ghibli Shadow Color (Warm/Teal)", Color) = (0.26, 0.18, 0.22, 1.0)

        [Header(Top Cap Overlay (Grass or Sunlit Wood))]
        [Toggle] _UseTopOverlay ("Enable Top Surface Overlay", Float) = 1.0
        _TopColor           ("Top Cap Lit Color", Color) = (0.36, 0.64, 0.26, 1.0)
        _TopShadowColor     ("Top Cap Shadow Color", Color) = (0.16, 0.36, 0.24, 1.0)
        _TopNormalThreshold ("Top Normal Y Threshold", Range(0.1, 0.95)) = 0.52
        _TopBlendSmoothness ("Top Blend Softness", Range(0.02, 0.5)) = 0.18

        [Header(Waterline Wetness and Moss)]
        [Toggle] _UseWetness ("Enable Waterline Wetness", Float) = 1.0
        _WaterLevelY        ("World Water Level Y", Float) = 0.0
        _WetHeight          ("Wetness Transition Height", Range(0.05, 1.5)) = 0.45
        _WetColor           ("Wet / Mossy Dark Color", Color) = (0.15, 0.20, 0.16, 1.0)

        [Header(Cel Shading and Rim Light)]
        _CelRampThreshold   ("Cel Highlight Threshold", Range(0.0, 0.9)) = 0.42
        _CelRampSmoothness  ("Cel Ramp Softness (Painterly)", Range(0.02, 0.5)) = 0.18
        _RimColor           ("Sunlit Rim Color", Color) = (0.95, 0.85, 0.55, 1.0)
        _RimPower           ("Rim Sharpness", Range(1.0, 8.0)) = 3.5
        _RimStrength        ("Rim Strength", Range(0.0, 0.6)) = 0.22

        [Header(Painterly Brush Variation)]
        _NoiseScale         ("Painterly Brush Scale", Float) = 2.8
        _NoiseStrength      ("Painterly Variation Strength", Range(0.0, 0.35)) = 0.10
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "Queue"          = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        LOD 200
        Cull Back
        ZWrite On

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _MidtoneColor;
            float4 _ShadowColor;
            float4 _TopColor;
            float4 _TopShadowColor;
            float4 _WetColor;
            float4 _RimColor;
            float  _UseTopOverlay;
            float  _TopNormalThreshold;
            float  _TopBlendSmoothness;
            float  _UseWetness;
            float  _WaterLevelY;
            float  _WetHeight;
            float  _CelRampThreshold;
            float  _CelRampSmoothness;
            float  _RimPower;
            float  _RimStrength;
            float  _NoiseScale;
            float  _NoiseStrength;
        CBUFFER_END
        ENDHLSL

        // ------------------------------------------------------------------
        // 1. Forward Lit Pass (Ghibli 3-Band Painterly Cel Shading)
        // ------------------------------------------------------------------
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ LIGHTMAP_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float4 vertexColor  : COLOR;
                float2 uv           : TEXCOORD2;
            };

            float hash21(float2 p)
            {
                p = frac(p * float2(234.34, 435.345));
                p += dot(p, p + 34.23);
                return frac(p.x * p.y);
            }

            float valueNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f);

                float a = hash21(i);
                float b = hash21(i + float2(1.0, 0.0));
                float c = hash21(i + float2(0.0, 1.0));
                float d = hash21(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float fbmPainterly(float3 posWS)
            {
                float2 p = posWS.xy * _NoiseScale + posWS.zy * (_NoiseScale * 0.6);
                float n = valueNoise(p) * 0.65 + valueNoise(p * 2.3 + 4.1) * 0.35;
                return n - 0.5;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS  = posInputs.positionCS;
                output.positionWS  = posInputs.positionWS;
                output.normalWS    = normInputs.normalWS;
                output.vertexColor = input.color;
                output.uv          = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 N = normalize(input.normalWS);
                float3 V = GetWorldSpaceNormalizeViewDir(input.positionWS);

                // Painterly brush variation
                float brush = fbmPainterly(input.positionWS) * _NoiseStrength;

                // Main light & shadow
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                float shadowAtten = mainLight.shadowAttenuation * mainLight.distanceAttenuation;

                // Half-Lambert with painterly edge modulation
                float NdotL = dot(N, mainLight.direction);
                float halfLambert = saturate(NdotL * 0.5 + 0.5 + brush * 0.6);
                float litFactor = halfLambert * lerp(0.35, 1.0, shadowAtten);

                // 3-band Ghibli Cel Shading
                float midBand = smoothstep(0.18, 0.18 + _CelRampSmoothness, litFactor);
                float highBand = smoothstep(_CelRampThreshold, _CelRampThreshold + _CelRampSmoothness, litFactor);

                half3 bodyColor = lerp(_ShadowColor.rgb, _MidtoneColor.rgb, midBand);
                bodyColor = lerp(bodyColor, _BaseColor.rgb, highBand);

                // Apply subtle vertex color tint if present (R channel = brightness variation)
                bodyColor *= lerp(0.88, 1.08, input.vertexColor.r + brush);

                // Optional Top Surface Overlay (e.g. Ghibli green grass lip or sunlit plank top)
                if (_UseTopOverlay > 0.5)
                {
                    float topMask = smoothstep(
                        _TopNormalThreshold - _TopBlendSmoothness,
                        _TopNormalThreshold + _TopBlendSmoothness,
                        N.y + brush * 0.45
                    );
                    half3 topSurfaceCol = lerp(_TopShadowColor.rgb, _TopColor.rgb, highBand * 0.7 + midBand * 0.3);
                    topSurfaceCol *= (1.0 + brush * 0.5);
                    bodyColor = lerp(bodyColor, topSurfaceCol, topMask);
                }

                // Optional Waterline Wetness & Moss darkening near Y = _WaterLevelY
                if (_UseWetness > 0.5)
                {
                    float heightAboveWater = input.positionWS.y - _WaterLevelY + brush * 0.15;
                    float wetMask = 1.0 - smoothstep(-0.1, _WetHeight, heightAboveWater);
                    bodyColor = lerp(bodyColor, _WetColor.rgb, wetMask * 0.78);
                }

                // Soft Sunlit Rim Light on upward/side edges
                float fresnel = pow(1.0 - saturate(dot(N, V)), _RimPower);
                float sunRimMask = saturate(NdotL + 0.35) * saturate(N.y + 0.5) * shadowAtten;
                bodyColor += _RimColor.rgb * (fresnel * sunRimMask * _RimStrength);

                // Combine with subtle ambient + main light color
                half3 ambient = SampleSH(N) * 0.28;
                half3 finalColor = bodyColor * (lerp(half3(0.82, 0.80, 0.85), mainLight.color, 0.45) + ambient);

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------
        // 2. ShadowCaster Pass
        // ------------------------------------------------------------------
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
            };

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                return output;
            }

            half4 ShadowPassFragment() : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------
        // 3. DepthOnly Pass (Required for Uber Stylized Water Intersection Foam!)
        // ------------------------------------------------------------------
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
            };

            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 DepthOnlyFragment() : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------
        // 4. DepthNormals Pass (Required for URP SSAO & Water Refraction)
        // ------------------------------------------------------------------
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On

            HLSLPROGRAM
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 normalWS     : TEXCOORD0;
            };

            Varyings DepthNormalsVertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 DepthNormalsFragment(Varyings input) : SV_Target
            {
                return half4(NormalizeNormalPerPixel(input.normalWS), 0.0);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
