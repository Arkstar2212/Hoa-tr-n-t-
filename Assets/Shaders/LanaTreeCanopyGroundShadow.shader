Shader "HoaVaMau/LanaTreeCanopyGroundShadow"
{
    Properties
    {
        _MainTex ("Tree Sprite Silhouette", 2D) = "white" {}
        _SpriteUVRect ("Sprite UV Rect (X,Y,W,H)", Vector) = (0, 0, 1, 1)
        _ShadowColor ("Watercolor Shadow Tint", Color) = (0.05, 0.16, 0.14, 0.62)
        _SwaySpeed ("Canopy Sway Speed", Range(0.1, 3.0)) = 1.15
        _SwayAmp ("Canopy Sway UV Amplitude", Range(0.0, 0.08)) = 0.020
        _BlurRadius ("Watercolor Edge Softness", Range(0.002, 0.06)) = 0.018
        _DappleScale ("Sunbeam Leaf Dapple Scale", Range(1.5, 8.0)) = 4.5
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent-20"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "CanopyGroundShadowForward"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            Offset -2, -2

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _SpriteUVRect;
                half4 _ShadowColor;
                float _SwaySpeed;
                float _SwayAmp;
                float _BlurRadius;
                float _DappleScale;
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
                OUT.positionHCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.uv = IN.uv;
                OUT.color = IN.color;
                return OUT;
            }

            half SampleSpriteAlpha(float2 localUV)
            {
                if (localUV.x < 0.0 || localUV.x > 1.0 || localUV.y < 0.0 || localUV.y > 1.0)
                    return 0.0h;
                float2 atlasUV = _SpriteUVRect.xy + localUV * _SpriteUVRect.zw;
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, atlasUV).a;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float t = _Time.y * _SwaySpeed;
                float swayWeight = smoothstep(0.22, 0.92, IN.uv.y);
                float swayU = sin(t + IN.positionWS.x * 0.45 + IN.uv.y * 2.2) * _SwayAmp * swayWeight;
                float swayV = cos(t * 0.82 + IN.positionWS.x * 0.7) * (_SwayAmp * 0.42) * swayWeight;
                float2 baseUV = IN.uv + float2(swayU, swayV);

                // 5-tap soft watercolor brush kernel preserving recognizable leaf/frond clusters
                float r = _BlurRadius;
                half aCenter = SampleSpriteAlpha(baseUV);
                half a1 = SampleSpriteAlpha(baseUV + float2( r,  r * 0.65));
                half a2 = SampleSpriteAlpha(baseUV + float2(-r,  r * 0.65));
                half a3 = SampleSpriteAlpha(baseUV + float2( r, -r * 0.65));
                half a4 = SampleSpriteAlpha(baseUV + float2(-r, -r * 0.65));
                half spriteMask = aCenter * 0.40h + (a1 + a2 + a3 + a4) * 0.15h;
                spriteMask = smoothstep(0.08h, 0.55h, spriteMask);

                // Sunbeam leaf-gap dapple inside dense canopy areas
                float2 noiseUV = float2(IN.positionWS.x * 0.65 + swayU * 4.0, (IN.positionWS.z - IN.positionWS.y * 0.85) * 0.95) * (_DappleScale * 0.42);
                float n1 = ValueNoise(noiseUV);
                float n2 = ValueNoise(noiseUV * 2.15 - float2(t * 0.28, 0.0));
                float dapple = n1 * 0.65 + n2 * 0.35;

                // Soft central canopy grounding so trunk/crown connection stays cohesive
                float2 coreVec = float2((IN.uv.x - 0.5) * 2.35, (IN.uv.y - 0.52) * 2.2);
                float coreAnchor = 1.0 - smoothstep(0.25, 0.95, length(coreVec) - (dapple - 0.5) * 0.25);
                half combinedMask = saturate(max(spriteMask, (half)coreAnchor * 0.42h));

                // Horizontal & vertical border feathering (drape fade is also in IN.color.a)
                float edgeFadeX = smoothstep(0.0, 0.10, IN.uv.x) * smoothstep(1.0, 0.90, IN.uv.x);
                float edgeFadeV = smoothstep(0.16, 0.26, IN.uv.y) * smoothstep(1.0, 0.91, IN.uv.y);

                // Carve subtle sunlit dapple holes inside the shadow while keeping silhouette readable
                half sunFiltered = (half)(0.58 + 0.42 * smoothstep(0.22, 0.76, dapple));
                half finalAlpha = combinedMask * sunFiltered * (half)(edgeFadeX * edgeFadeV) * _ShadowColor.a * IN.color.a;

                return half4(_ShadowColor.rgb, finalAlpha);
            }
            ENDHLSL
        }
    }
}
