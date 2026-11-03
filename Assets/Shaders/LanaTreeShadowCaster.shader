Shader "HoaVaMau/LanaTreeShadowCaster"
{
    Properties
    {
        _BaseMap ("Tree Sprite Texture", 2D) = "white" {}
        _SpriteUVRect ("Sprite UV Rect (X,Y,W,H)", Vector) = (0, 0, 1, 1)
        _VRange ("Vertical Sample Range (MinV, MaxV)", Vector) = (0.18, 1.0, 0, 0)
        _FlipX ("Flip X (1 = Flip)", Float) = 0.0
        _Cutoff ("Alpha Cutoff", Range(0.05, 0.9)) = 0.28
        _SwaySpeed ("Wind Sway Speed", Range(0.0, 4.0)) = 1.25
        _SwayAmp ("Wind Sway Amplitude", Range(0.0, 0.5)) = 0.12
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
        }

        Pass
        {
            Name "ForwardCutout"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _SpriteUVRect;
                float4 _VRange;
                float _FlipX;
                float _Cutoff;
                float _SwaySpeed;
                float _SwayAmp;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 posOS = IN.positionOS.xyz;
                float3 baseWS = TransformObjectToWorld(float3(0.0, 0.0, 0.0));
                float h = saturate(IN.uv.y);
                float sway = sin(_Time.y * _SwaySpeed + baseWS.x * 0.35 + h * 1.6) * _SwayAmp * (0.25 + 0.75 * h);
                posOS.x += sway;

                OUT.positionHCS = TransformObjectToHClip(posOS);
                float u = (_FlipX > 0.5) ? (1.0 - IN.uv.x) : IN.uv.x;
                float v = lerp(_VRange.x, _VRange.y, IN.uv.y);
                OUT.uv = _SpriteUVRect.xy + float2(u, v) * _SpriteUVRect.zw;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                clip(col.a - _Cutoff);
                return col;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _SpriteUVRect;
                float4 _VRange;
                float _FlipX;
                float _Cutoff;
                float _SwaySpeed;
                float _SwayAmp;
            CBUFFER_END

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                float3 posOS = input.positionOS.xyz;
                float3 baseWS = TransformObjectToWorld(float3(0.0, 0.0, 0.0));
                float h = saturate(input.uv.y);

                float trunkWave = sin(_Time.y * _SwaySpeed + baseWS.x * 0.35);
                float leafFlutter = sin(_Time.y * (_SwaySpeed * 2.2) + baseWS.x * 0.9 + posOS.x * 2.4) * 0.35;
                posOS.x += (trunkWave + leafFlutter) * _SwayAmp * (0.25 + 0.75 * h);

                float3 positionWS = TransformObjectToWorld(posOS);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
            #else
                float3 lightDirectionWS = _LightDirection;
            #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
            #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #endif

                output.positionCS = positionCS;
                float u = (_FlipX > 0.5) ? (1.0 - input.uv.x) : input.uv.x;
                float v = lerp(_VRange.x, _VRange.y, input.uv.y);
                output.uv = _SpriteUVRect.xy + float2(u, v) * _SpriteUVRect.zw;
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_Target
            {
                half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a;
                clip(alpha - _Cutoff);
                return 0;
            }
            ENDHLSL
        }
    }
}
