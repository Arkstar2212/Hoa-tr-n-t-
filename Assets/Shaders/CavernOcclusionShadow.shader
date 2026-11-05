Shader "HoaVaMau/CavernOcclusionShadow"
{
    Properties
    {
        _ShadowTint ("Shadow Tint (Multiply)", Color) = (0.18, 0.20, 0.25, 1.0)
        _EntranceStartX ("Entrance Start X", Float) = 126.0
        _EntranceEndX ("Entrance End X", Float) = 128.5
        _ExitStartX ("Exit Start X", Float) = 140.0
        _ExitEndX ("Exit End X", Float) = 142.5
        
        _CreviceCenterX ("Crevice Center X", Float) = 134.25
        _CreviceWidth ("Crevice Opening Width", Float) = 0.85
        _CreviceSlant ("Crevice Slant Factor", Float) = 0.42
        _CreviceSoftness ("Crevice Edge Softness", Float) = 0.25
        _BaseY ("Base Reference Y", Float) = -1.85
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent+10" 
            "RenderPipeline" = "UniversalPipeline" 
        }

        Blend DstColor Zero // Multiply blend mode
        ZWrite Off
        Cull Off

        Pass
        {
            Name "CavernShadowPass"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 uv         : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _ShadowTint;
                float _EntranceStartX;
                float _EntranceEndX;
                float _ExitStartX;
                float _ExitEndX;
                float _CreviceCenterX;
                float _CreviceWidth;
                float _CreviceSlant;
                float _CreviceSoftness;
                float _BaseY;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float worldX = input.positionWS.x;
                float worldY = input.positionWS.y;

                // 1. Entrance and Exit horizontal fade (1.0 = outside/bright, 0.0 = deep cave interior)
                float entranceFade = smoothstep(_EntranceStartX, _EntranceEndX, worldX);
                float exitFade = 1.0 - smoothstep(_ExitStartX, _ExitEndX, worldX);
                float interiorShadowAmount = entranceFade * exitFade; // 1.0 inside cave, 0.0 outside

                // 2. Vertical fade at top and bottom of quad (using local UV)
                float verticalFade = smoothstep(0.0, 0.15, input.uv.y) * smoothstep(1.0, 0.85, input.uv.y);
                interiorShadowAmount *= verticalFade;

                // 3. Crevice Light Opening ("khoảng nhỏ ánh sáng le lói")
                // Slanted crevice axis in world space:
                float slantedCreviceX = _CreviceCenterX + (worldY - _BaseY) * _CreviceSlant;
                float distFromCrevice = abs(worldX - slantedCreviceX);
                
                // Inside crevice opening, light penetrates -> shadow is cancelled (amount drops to 0)
                float halfWidth = _CreviceWidth * 0.5;
                float creviceLightMask = 1.0 - smoothstep(halfWidth - _CreviceSoftness, halfWidth + _CreviceSoftness, distFromCrevice);

                // Final shadow strength: 1.0 = full shadow, 0.0 = bright daylight/crevice light
                float finalShadowStrength = interiorShadowAmount * (1.0 - creviceLightMask);

                // Multiply blend: 1.0 = untouched, _ShadowTint.rgb = shadowed
                half3 outputRGB = lerp(half3(1.0, 1.0, 1.0), _ShadowTint.rgb, finalShadowStrength);

                return half4(outputRGB, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
