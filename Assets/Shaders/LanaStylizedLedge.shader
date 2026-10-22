Shader "HoaVaMau/LanaStylizedLedge"
{
    Properties
    {
        _GrassTopColor ("Green Grass Top", Color) = (0.43, 0.63, 0.29, 1)
        _GrassMidColor ("Green Grass Mid", Color) = (0.28, 0.45, 0.20, 1)
        _GrassGoldTopColor ("Golden Grass Top", Color) = (0.74, 0.65, 0.30, 1)
        _GrassGoldMidColor ("Golden Grass Mid", Color) = (0.50, 0.45, 0.21, 1)
        _GoldenStartX ("Golden Blend Start X", Float) = 42.0
        _GoldenEndX ("Golden Blend End X", Float) = 49.0

        _RockLightColor ("Rock Cliff Highlight", Color) = (0.21, 0.27, 0.35, 1)
        _RockMidColor ("Rock Upper Slate", Color) = (0.12, 0.16, 0.22, 1)
        _RockDarkColor ("Rock Deep Silhouette", Color) = (0.05, 0.07, 0.11, 1)

        _IsGrassCap ("Is Grass Cap Mesh (1=Grass, 0=Rock, 0.5=Auto Normal)", Range(0, 1)) = 0.5
        _GrassNormalThreshold ("Auto Grass Normal Y Threshold", Range(0.1, 0.9)) = 0.45
        _GrassShadowColor ("Grass Canopy Shadow Tint", Color) = (0.15, 0.30, 0.24, 1)
        _ShadowStrength ("Directional Shadow Strength", Range(0.0, 1.0)) = 0.58
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 color : COLOR;
                float2 uv : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _GrassTopColor;
                half4 _GrassMidColor;
                half4 _GrassGoldTopColor;
                half4 _GrassGoldMidColor;
                float _GoldenStartX;
                float _GoldenEndX;
                half4 _RockLightColor;
                half4 _RockMidColor;
                half4 _RockDarkColor;
                half _IsGrassCap;
                half _GrassNormalThreshold;
                half4 _GrassShadowColor;
                half _ShadowStrength;
            CBUFFER_END

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i);
                float b = Hash21(i + float2(1.0, 0.0));
                float c = Hash21(i + float2(0.0, 1.0));
                float d = Hash21(i + float2(1.0, 1.0));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(IN.normalOS);
                OUT.positionHCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.normalWS = normInputs.normalWS;
                OUT.color = IN.color;
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 N = normalize(IN.normalWS);

                // Sample URP Main Light Shadows (cast by trees, rocks, and characters)
                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half shadowAtten = mainLight.shadowAttenuation;

                // Smooth horizontal painterly brush strokes (no harsh Voronoi grid)
                float brushCoarse = ValueNoise(float2(IN.positionWS.x * 0.35, IN.positionWS.z * 1.4 + IN.positionWS.y * 1.8));
                float brushFine = ValueNoise(float2(IN.positionWS.x * 1.6, IN.positionWS.z * 4.0 + IN.positionWS.y * 4.0));

                // Determine grass vs rock
                float grassMask = 0.0;
                if (_IsGrassCap > 0.75)
                {
                    grassMask = 1.0;
                }
                else if (_IsGrassCap < 0.25)
                {
                    grassMask = 0.0;
                }
                else
                {
                    float nY = N.y + (brushCoarse - 0.5) * 0.16;
                    grassMask = smoothstep(_GrassNormalThreshold - 0.05, _GrassNormalThreshold + 0.05, nY);
                }

                // Golden zone blend factor along world X
                float goldFactor = smoothstep(_GoldenStartX, _GoldenEndX, IN.positionWS.x);
                float warmPatch = smoothstep(0.58, 0.88, brushCoarse) * 0.18;
                float effectiveGold = saturate(goldFactor + warmPatch);

                half3 gTop = lerp(_GrassTopColor.rgb, _GrassGoldTopColor.rgb, effectiveGold);
                half3 gMid = lerp(_GrassMidColor.rgb, _GrassGoldMidColor.rgb, effectiveGold);

                // Painterly grass cap shading
                float lipFactor = (_IsGrassCap > 0.75) ? saturate(IN.color.r) : saturate(N.y * 0.5 + 0.5);
                float strokeBand = smoothstep(0.32, 0.68, lipFactor * 0.80 + brushCoarse * 0.20 + (brushFine - 0.5) * 0.06);
                half3 grassCol = lerp(gMid, gTop, strokeBand);

                // Apply lush Planet of Lana canopy shadow tint on sunlit grass
                half shadowMask = (1.0h - shadowAtten) * saturate(_ShadowStrength);
                half3 shadowedGrass = lerp(grassCol * half3(0.46h, 0.60h, 0.56h), _GrassShadowColor.rgb, 0.45h);
                grassCol = lerp(grassCol, shadowedGrass, shadowMask);

                // Soft dark underside on the overhanging grass lip so it blends naturally into the dark silhouette ground
                if (_IsGrassCap > 0.75 && IN.color.r < 0.72)
                {
                    grassCol = lerp(_RockDarkColor.rgb * 1.35, grassCol, smoothstep(0.22, 0.72, IN.color.r));
                }

                // Planet of Lana Deep Navy-Slate Ground Cross-Section & Subtle Cliff Strata
                float depthGrad = saturate((IN.positionWS.y + 4.2) / 2.6);
                float strata = ValueNoise(float2(IN.positionWS.x * 0.55 + brushCoarse * 0.8, IN.positionWS.y * 2.2));
                float cliffMask = saturate(IN.color.b + smoothstep(-1.4, -0.3, IN.positionWS.y) * 0.75);

                half3 baseRock = lerp(_RockDarkColor.rgb, _RockMidColor.rgb, depthGrad * 0.75 + strata * 0.25);
                half3 cliffRock = lerp(_RockMidColor.rgb, _RockLightColor.rgb, smoothstep(0.38, 0.68, strata + N.x * 0.15));
                half3 rockCol = lerp(baseRock, cliffRock, cliffMask * 0.70);
                rockCol *= lerp(1.0h, 0.68h, shadowMask);

                // Under-lip occlusion band right beneath the grass cap (encoded in IN.color.g)
                float rockAO = (_IsGrassCap < 0.25) ? saturate(IN.color.g) : 1.0;
                rockCol *= lerp(0.55, 1.0, rockAO);

                half3 finalCol = lerp(rockCol, grassCol, grassMask);
                return half4(finalCol, 1.0);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
