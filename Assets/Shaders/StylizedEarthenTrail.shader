Shader "HoaVaMau/StylizedEarthenTrail"
{
    Properties
    {
        [Header(Three Layer Soil Colors)]
        _CenterPathColor    ("Center Compacted Dirt (Nâu Sáng Nén)", Color) = (0.55, 0.40, 0.26, 1.0)
        _EdgeDampColor      ("Edge Damp Dirt (Nâu Sẫm Ẩm Mép)",       Color) = (0.35, 0.24, 0.14, 1.0)
        _BankEarthColor     ("Bank Ridge Earth (Nâu Đỏ Thành Đất)",   Color) = (0.43, 0.25, 0.16, 1.0)

        [Header(Path Profile and Width Settings)]
        _TrailCenterZ       ("Trail Center World Z", Float) = 0.0
        _CenterPathRadius   ("Center Path Half-Width", Range(0.1, 1.2)) = 0.45
        _EdgeTransition     ("Edge Transition Softness", Range(0.05, 0.6)) = 0.25
        _BankSlopeBlend     ("Bank Slope Down Blend", Range(0.1, 1.0)) = 0.45

        [Header(Natural Meandering Noise)]
        _WiggleFrequency    ("Wiggle Frequency (X)", Float) = 0.15
        _WiggleAmplitude    ("Wiggle Amplitude (Z)", Range(0.0, 0.35)) = 0.12
        _MicroNoiseScale    ("Micro Surface Noise Scale", Float) = 3.5
        _MicroNoiseStrength ("Micro Noise Variation", Range(0.0, 0.3)) = 0.08

        [Header(Surface Smoothness)]
        _CenterSmoothness   ("Center Path Smoothness", Range(0.0, 0.35)) = 0.18
        _EdgeSmoothness     ("Edge Soil Smoothness",   Range(0.0, 0.35)) = 0.08

        [Header(Stylized Lighting)]
        _ShadowWarmthTint   ("Shadow Warmth Tint", Color) = (0.35, 0.22, 0.18, 1.0)
        _HalfLambertPower   ("Soft Light Wrap (Half Lambert)", Range(0.5, 1.0)) = 0.75

        [Header(Optional Detail Texture)]
        _MainTex            ("Optional Detail Texture", 2D) = "white" {}
        _DetailStrength     ("Texture Overlay Strength", Range(0.0, 1.0)) = 0.20
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

        // ------------------------------------------------------------------
        // Forward Lit Pass
        // ------------------------------------------------------------------
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // URP keywords for shadows and lighting
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ LIGHTMAP_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float2 uv           : TEXCOORD2;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _CenterPathColor;
                float4 _EdgeDampColor;
                float4 _BankEarthColor;
                float4 _ShadowWarmthTint;
                float4 _MainTex_ST;
                float  _TrailCenterZ;
                float  _CenterPathRadius;
                float  _EdgeTransition;
                float  _BankSlopeBlend;
                float  _WiggleFrequency;
                float  _WiggleAmplitude;
                float  _MicroNoiseScale;
                float  _MicroNoiseStrength;
                float  _CenterSmoothness;
                float  _EdgeSmoothness;
                float  _HalfLambertPower;
                float  _DetailStrength;
            CBUFFER_END

            // Simple fast procedural noise for subtle organic color variation
            float hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float smoothNoise(float2 uv)
            {
                float2 lv = frac(uv);
                float2 id = floor(uv);
                lv = lv * lv * (3.0 - 2.0 * lv);

                float bl = hash(id);
                float br = hash(id + float2(1.0, 0.0));
                float tl = hash(id + float2(0.0, 1.0));
                float tr = hash(id + float2(1.0, 1.0));

                float b = lerp(bl, br, lv.x);
                float t = lerp(tl, tr, lv.y);
                return lerp(b, t, lv.y);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS   = normInputs.normalWS;
                output.uv         = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 1. Calculate trail center line with gentle meandering along X
                float wiggleOffset = sin(input.positionWS.x * _WiggleFrequency) * _WiggleAmplitude;
                float effectiveCenterZ = _TrailCenterZ + wiggleOffset;
                float distFromCenter = abs(input.positionWS.z - effectiveCenterZ);

                // 2. Micro surface variation noise
                float microNoise = (smoothNoise(input.positionWS.xz * _MicroNoiseScale) - 0.5) * _MicroNoiseStrength;
                distFromCenter += microNoise;

                // 3. Blend Layer 1 (Center) -> Layer 2 (Edge)
                float edgeStart = _CenterPathRadius;
                float edgeEnd   = _CenterPathRadius + _EdgeTransition;
                float centerToEdgeFac = smoothstep(edgeStart, edgeEnd, distFromCenter);

                half3 groundColor = lerp(_CenterPathColor.rgb, _EdgeDampColor.rgb, centerToEdgeFac);

                // 4. Blend Layer 2 (Edge) -> Layer 3 (Bank Ridge Earth)
                // Downward slopes (where normal points slightly sideways or down) blend more towards Bank Earth
                float bankStart = edgeEnd;
                float bankEnd   = edgeEnd + _BankSlopeBlend;
                float edgeToBankFac = smoothstep(bankStart, bankEnd, distFromCenter);

                // Normal-based bank detection: vertical/slant faces receive richer earth tone
                float slopeFac = 1.0 - saturate(input.normalWS.y);
                float totalBankFac = saturate(edgeToBankFac + slopeFac * 0.7);

                groundColor = lerp(groundColor, _BankEarthColor.rgb, totalBankFac);

                // 5. Blend optional detail texture
                half4 detailTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                groundColor = lerp(groundColor, groundColor * detailTex.rgb * 1.5, _DetailStrength);

                // 6. Smoothness interpolation
                float smoothness = lerp(_CenterSmoothness, _EdgeSmoothness, centerToEdgeFac);

                // 7. Lighting calculation
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                // Stylized Half-Lambert light response
                float NdotL = dot(normalize(input.normalWS), mainLight.direction);
                float halfLambert = pow(saturate(NdotL * 0.5 + 0.5), _HalfLambertPower);

                // Shadow attenuation with warm ambient shadow tint
                float shadowAtten = mainLight.shadowAttenuation * mainLight.distanceAttenuation;
                half3 litLight = mainLight.color * halfLambert;
                half3 finalLight = lerp(_ShadowWarmthTint.rgb * 0.5, litLight, shadowAtten);

                // Ambient light (Spherical Harmonics)
                half3 ambient = SampleSH(input.normalWS);

                // Final color composition
                half3 finalColor = groundColor * (finalLight + ambient);

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------
        // Shadow Caster Pass
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
        // DepthOnly Pass for URP Prepass & Depth of Field
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
    }

    FallBack "Universal Render Pipeline/Lit"
}
