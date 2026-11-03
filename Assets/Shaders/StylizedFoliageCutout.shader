Shader "HoaVaMau/StylizedFoliageCutout"
{
    Properties
    {
        [Header(Base Texture and Cutout)]
        _MainTex ("Foliage Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1, 1, 1, 1)
        _Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.32

        [Header(Stylized Lighting and Shadows)]
        _ShadowColor ("Shadow / Shade Tint", Color) = (0.35, 0.48, 0.30, 1.0)
        _SunlitColor ("Sunlit Highlight Tint", Color) = (1.08, 1.05, 0.95, 1.0)
        _ShadowSoftness ("Shadow Band Softness", Range(0.01, 0.6)) = 0.25
        _AmbientBoost ("Ambient Fill Light", Range(0.0, 1.0)) = 0.45

        [Header(Gentle Wind Sway)]
        _WindStrength ("Wind Sway Strength", Range(0.0, 0.5)) = 0.06
        _WindSpeed ("Wind Sway Speed", Range(0.1, 4.0)) = 1.6
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest+10"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Cull Off
        ZWrite On
        ZTest LEqual

        // --------------------------------------------------------------------
        // PASS 1: UNIVERSAL FORWARD (Stylized Lighting & Realtime Shadow Receiving)
        // --------------------------------------------------------------------
        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // Universal Pipeline keywords for shadows
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float2 uv           : TEXCOORD2;
                float4 color        : COLOR;
                float4 shadowCoord  : TEXCOORD3;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float4 _ShadowColor;
                float4 _SunlitColor;
                float  _Cutoff;
                float  _ShadowSoftness;
                float  _AmbientBoost;
                float  _WindStrength;
                float  _WindSpeed;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);

                // Subtle wind sway based on UV.y (height)
                float h = saturate(input.uv.y);
                float sway = sin(_Time.y * _WindSpeed + worldPos.x * 0.4 + worldPos.z * 0.4) * _WindStrength * (h * h);
                worldPos.x += sway;
                worldPos.z += sway * 0.5;

                output.positionWS = worldPos;
                output.positionCS = TransformWorldToHClip(worldPos);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;

                // Compute shadow coordinates for realtime shadow sampling
                output.shadowCoord = TransformWorldToShadowCoord(worldPos);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half alpha = tex.a * input.color.a * _Color.a;
                clip(alpha - _Cutoff);

                // Stylized Cel-Shaded lighting with Realtime Shadows
                Light mainLight = GetMainLight(input.shadowCoord);
                float3 N = normalize(input.normalWS);
                // Ensure double-sided normal always faces camera or light comfortably
                if (dot(N, mainLight.direction) < -0.15) N = -N;

                float NdotL = saturate(dot(N, mainLight.direction) * 0.4 + 0.6);
                half shadowAtten = mainLight.shadowAttenuation;

                // Combine diffuse lighting and shadow attenuation
                half lightVal = saturate(NdotL * shadowAtten);
                half tone = smoothstep(0.25 - _ShadowSoftness, 0.25 + _ShadowSoftness, lightVal);

                // Blend between stylized shadow tint and sunlit highlight
                half3 litMod = lerp(_ShadowColor.rgb, _SunlitColor.rgb, tone);
                litMod = max(litMod, half3(_AmbientBoost, _AmbientBoost, _AmbientBoost));

                half3 finalColor = tex.rgb * _Color.rgb * input.color.rgb * litMod;
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }

        // --------------------------------------------------------------------
        // PASS 2: SHADOW CASTER (Casts 3D Tree Shadows Onto Ground and Props)
        // --------------------------------------------------------------------
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float4 _ShadowColor;
                float4 _SunlitColor;
                float  _Cutoff;
                float  _ShadowSoftness;
                float  _AmbientBoost;
                float  _WindStrength;
                float  _WindSpeed;
            CBUFFER_END

            float3 _LightDirection;

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                float h = saturate(input.uv.y);
                float sway = sin(_Time.y * _WindSpeed + worldPos.x * 0.4 + worldPos.z * 0.4) * _WindStrength * (h * h);
                worldPos.x += sway;
                worldPos.z += sway * 0.5;

                output.positionCS = TransformWorldToHClip(ApplyShadowBias(worldPos, normalWS, _LightDirection));
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half alpha = tex.a * input.color.a * _Color.a;
                clip(alpha - _Cutoff);
                return 0;
            }
            ENDHLSL
        }

        // --------------------------------------------------------------------
        // PASS 3: DEPTH ONLY (Ensures Full Occlusion & Accurate Depth Buffer)
        // --------------------------------------------------------------------
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float4 _ShadowColor;
                float4 _SunlitColor;
                float  _Cutoff;
                float  _ShadowSoftness;
                float  _AmbientBoost;
                float  _WindStrength;
                float  _WindSpeed;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(worldPos);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half alpha = tex.a * input.color.a * _Color.a;
                clip(alpha - _Cutoff);
                return 0;
            }
            ENDHLSL
        }
    }
}
