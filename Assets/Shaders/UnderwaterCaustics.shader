Shader "HoaVaMau/UnderwaterCaustics"
{
    Properties
    {
        _CausticColor ("Caustic Color", Color) = (0.35, 0.85, 1.0, 0.7)
        _CausticScale ("Caustic Scale", Float) = 1.2
        _CausticSpeed ("Caustic Speed", Float) = 0.6
        _CausticPower ("Sharpness", Range(1.0, 8.0)) = 3.5
        _DepthFade ("Depth Fade Distance", Float) = 8.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent+10" 
            "RenderPipeline" = "UniversalPipeline" 
        }

        Blend One One // Additive for glowing light webs
        ZWrite Off
        Cull Off

        Pass
        {
            Name "CausticForward"
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
                float2 uv         : TEXCOORD0;
                float3 worldPos   : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _CausticColor;
                float _CausticScale;
                float _CausticSpeed;
                float _CausticPower;
                float _DepthFade;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.worldPos   = TransformObjectToWorld(input.positionOS.xyz);
                output.uv         = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float t = _Time.y * _CausticSpeed;
                float2 uv1 = input.worldPos.xz * _CausticScale + float2(t * 0.4, t * 0.25);
                float2 uv2 = input.worldPos.xz * (_CausticScale * 1.15) - float2(t * 0.3, t * 0.45);

                float c1 = sin(uv1.x + sin(uv1.y * 1.2));
                float c2 = cos(uv2.y + cos(uv2.x * 1.3));
                float caustic = saturate((c1 + c2) * 0.5 + 0.5);
                caustic = pow(caustic, _CausticPower);

                // Depth fade so it fades out as it gets too deep
                float depthMod = saturate(1.0 - (abs(input.worldPos.y) / max(0.1, _DepthFade)));

                half3 finalColor = _CausticColor.rgb * (caustic * _CausticColor.a * depthMod);
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
