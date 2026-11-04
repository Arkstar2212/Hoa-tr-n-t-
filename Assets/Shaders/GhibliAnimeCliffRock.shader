Shader "HoaVaMau/GhibliAnimeCliffRock"
{
    Properties
    {
        [Header(Ghibli Anime Rock Palette)]
        _BaseColor          ("Sunlit Rock Face (Lit)", Color) = (0.64, 0.67, 0.58, 1.0)
        _MidtoneColor       ("Midtone Slate Stone", Color) = (0.42, 0.49, 0.45, 1.0)
        _ShadowColor        ("Deep Crevice Shadow (Cool Slate)", Color) = (0.12, 0.22, 0.24, 1.0)

        [Header(Chiseled Anime Facets)]
        _FacetStrength      ("Chiseled Facet Sharpness", Range(0.0, 1.0)) = 0.72

        [Header(Stone Surface and World Triplanar)]
        [MainTexture] _MainTex ("Stone Detail Map", 2D) = "white" {}
        _DetailScale        ("World Triplanar Scale", Float) = 0.22
        _DetailStrength     ("Texture Influence", Range(0.0, 1.0)) = 0.55

        [Header(Vertical Cliff Weathering and Cracks)]
        _VerticalBandingFreq    ("Striation Frequency", Float) = 0.45
        _VerticalBandingStrength("Striation Strength", Range(0.0, 0.5)) = 0.22
        _CrackScale             ("Crevice Crack Frequency", Float) = 0.35
        _CrackStrength          ("Crevice Crack Depth", Range(0.0, 0.6)) = 0.28

        [Header(Top Shelf Lichen Ledge)]
        [Toggle] _UseTopOverlay ("Enable Top Shelf Lichen", Float) = 1.0
        _TopColor           ("Top Shelf Lichen (Lit)", Color) = (0.52, 0.58, 0.44, 1.0)
        _TopShadowColor     ("Top Shelf Lichen (Shadow)", Color) = (0.24, 0.32, 0.25, 1.0)
        _TopNormalThreshold ("Top Normal Y Threshold", Range(0.3, 0.95)) = 0.68
        _TopBlendSmoothness ("Top Blend Softness", Range(0.02, 0.4)) = 0.12

        [Header(Cel Shading and Rim Light)]
        _CelRampThreshold   ("Cel Highlight Threshold", Range(0.0, 0.9)) = 0.36
        _CelRampSmoothness  ("Cel Ramp Softness", Range(0.02, 0.5)) = 0.18
        _RimColor           ("Sunlit Rim Color", Color) = (0.85, 0.88, 0.75, 1.0)
        _RimPower           ("Rim Sharpness", Range(1.0, 8.0)) = 4.0
        _RimStrength        ("Rim Strength", Range(0.0, 0.6)) = 0.24

        [Header(Painterly Noise Variation)]
        _NoiseScale         ("Painterly Brush Scale", Float) = 2.8
        _NoiseStrength      ("Painterly Variation", Range(0.0, 0.35)) = 0.10
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
            float4 _RimColor;
            float4 _MainTex_ST;
            float  _FacetStrength;
            float  _DetailScale;
            float  _DetailStrength;
            float  _VerticalBandingFreq;
            float  _VerticalBandingStrength;
            float  _CrackScale;
            float  _CrackStrength;
            float  _UseTopOverlay;
            float  _TopNormalThreshold;
            float  _TopBlendSmoothness;
            float  _CelRampThreshold;
            float  _CelRampSmoothness;
            float  _RimPower;
            float  _RimStrength;
            float  _NoiseScale;
            float  _NoiseStrength;
        CBUFFER_END

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);

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

        half3 sampleTriplanar(float3 posWS, float3 normalWS)
        {
            float3 blendWeights = pow(abs(normalWS), 4.0);
            blendWeights /= max(blendWeights.x + blendWeights.y + blendWeights.z, 0.0001);

            float2 uvX = posWS.zy * _DetailScale;
            float2 uvY = posWS.xz * _DetailScale;
            float2 uvZ = posWS.xy * _DetailScale;

            half3 colX = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvX).rgb;
            half3 colY = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvY).rgb;
            half3 colZ = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvZ).rgb;

            return colX * blendWeights.x + colY * blendWeights.y + colZ * blendWeights.z;
        }
        ENDHLSL

        // ------------------------------------------------------------------
        // 1. Forward Lit Pass (Anime Ghibli 3-Band Cel Shading + Chiseled Facets)
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
                // Chiseled Anime Facets via screen-space derivatives
                float3 ddxPos = ddx(input.positionWS);
                float3 ddyPos = ddy(input.positionWS);
                float3 facetN = normalize(cross(ddyPos, ddxPos)); // geometric triangle normal
                if (dot(facetN, input.normalWS) < 0.0) facetN = -facetN;

                float3 N = normalize(lerp(input.normalWS, facetN, _FacetStrength));
                float3 V = GetWorldSpaceNormalizeViewDir(input.positionWS);

                // World-space painterly noise variation
                float brush = fbmPainterly(input.positionWS) * _NoiseStrength;

                // Main light & shadow
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                float shadowAtten = mainLight.shadowAttenuation * mainLight.distanceAttenuation;

                // Half-Lambert with painterly noise modulation
                float NdotL = dot(N, mainLight.direction);
                float halfLambert = saturate(NdotL * 0.5 + 0.5 + brush * 0.5);
                float litFactor = halfLambert * lerp(0.35, 1.0, shadowAtten);

                // 3-band Ghibli Cel Shading
                float midBand = smoothstep(0.18, 0.18 + _CelRampSmoothness, litFactor);
                float highBand = smoothstep(_CelRampThreshold, _CelRampThreshold + _CelRampSmoothness, litFactor);

                half3 bodyColor = lerp(_ShadowColor.rgb, _MidtoneColor.rgb, midBand);
                bodyColor = lerp(bodyColor, _BaseColor.rgb, highBand);

                // Triplanar stone detail modulation
                half3 stoneDetail = sampleTriplanar(input.positionWS, N);
                float detailLuma = dot(stoneDetail, half3(0.299, 0.587, 0.114));
                bodyColor *= lerp(1.0, detailLuma * 1.45, _DetailStrength);

                // Vertical cliff weathering striations (anime watercolor vertical streaks)
                float vertStreak = sin(input.positionWS.y * _VerticalBandingFreq + valueNoise(input.positionWS.xz * 0.6) * 2.2);
                float vertDetail = vertStreak * 0.5 + 0.5;
                bodyColor *= lerp(1.0, vertDetail * 0.35 + 0.825, _VerticalBandingStrength);

                // Crevice crack lines (dark umber fissures running down the cliff)
                float crackNoise = abs(valueNoise(input.positionWS.xz * _CrackScale + float2(0, input.positionWS.y * _CrackScale * 0.25)) * 2.0 - 1.0);
                float crackMask = 1.0 - smoothstep(0.0, 0.25, crackNoise);
                bodyColor = lerp(bodyColor, _ShadowColor.rgb * 0.75, crackMask * _CrackStrength);

                // Top Surface Lichen Overlay on upward facing ledges
                if (_UseTopOverlay > 0.5)
                {
                    float topMask = smoothstep(
                        _TopNormalThreshold - _TopBlendSmoothness,
                        _TopNormalThreshold + _TopBlendSmoothness,
                        N.y + brush * 0.35
                    );
                    half3 topSurfaceCol = lerp(_TopShadowColor.rgb, _TopColor.rgb, highBand * 0.75 + midBand * 0.25);
                    topSurfaceCol *= (1.0 + brush * 0.35);
                    bodyColor = lerp(bodyColor, topSurfaceCol, topMask * 0.85);
                }

                // Sunlit Rim Light on upward edges
                float fresnel = pow(1.0 - saturate(dot(N, V)), _RimPower);
                float sunRimMask = saturate(NdotL + 0.35) * saturate(N.y + 0.5) * shadowAtten;
                bodyColor += _RimColor.rgb * (fresnel * sunRimMask * _RimStrength);

                // Ambient SH + directional light tone
                half3 ambient = SampleSH(N) * 0.25;
                half3 finalColor = bodyColor * (lerp(half3(0.85, 0.85, 0.88), mainLight.color, 0.40) + ambient);

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
        // 3. DepthOnly Pass
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
        // 4. DepthNormals Pass
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
