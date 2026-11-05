Shader "HoaVaMau/CreviceWallLightProjection"
{
    Properties
    {
        [MainColor] [HDR] _ProjectionColor ("Projection Color", Color) = (1.0, 0.88, 0.60, 0.45)
        _Intensity ("Intensity", Range(0.1, 4.0)) = 1.4
        _Spread ("Beam Spread", Range(0.5, 3.0)) = 1.5
        _EdgeSoftness ("Edge Softness", Range(0.1, 1.0)) = 0.5
        _NoiseTiling ("Rock Texture Noise Tiling", Float) = 4.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent+15" 
            "RenderPipeline" = "UniversalPipeline" 
        }

        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            Name "WallLightProjectionPass"
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
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _ProjectionColor;
                float _Intensity;
                float _Spread;
                float _EdgeSoftness;
                float _NoiseTiling;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;

                // uv.y = 1.0 is top of wall projection, uv.y = 0.0 is base of wall
                float widthScale = lerp(_Spread, 1.0, uv.y);
                float distFromCenter = abs(uv.x - 0.5) * 2.0 / max(0.001, widthScale);

                // Gaussian beam profile on wall
                float horizontalFalloff = exp(-distFromCenter * distFromCenter * (3.0 / max(0.1, _EdgeSoftness)));

                // Soft vertical bounds
                float verticalFalloff = smoothstep(0.0, 0.20, uv.y) * smoothstep(1.0, 0.80, uv.y);

                // Subtle organic rock surface texture modulation
                float rockGrain = 0.90 + 0.10 * sin(uv.x * 25.0 + uv.y * 15.0);

                float alpha = horizontalFalloff * verticalFalloff * rockGrain * _ProjectionColor.a;
                half3 col = _ProjectionColor.rgb * _Intensity * alpha;

                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
