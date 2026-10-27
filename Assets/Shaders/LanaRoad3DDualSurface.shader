Shader "HoaVaMau/LanaRoad3DDualSurface"
{
    Properties
    {
        _MainTex ("Texture Mat Dat Painterly (PNG)", 2D) = "white" {}
        _SoilTexTiling ("Tiling (X, Y) Offset (Z, W)", Vector) = (1.0, 1.0, 0, 0)
        _UseMeshUV ("Dung Mesh UV (1=Co, 0=World UV)", Float) = 1.0

        _WornTrailColor ("Mau Dat Mon Loi Di (Worn Trail)", Color) = (0.58, 0.44, 0.26, 1)
        _TrailBlend ("Do Mon Loi Di (Trail Blend)", Range(0, 1)) = 0.30

        _GradientTopY ("Cao Do Dinh Co (World Top Y)", Float) = -1.95
        _GradientBottomY ("Cao Do Day Den (World Bottom Y)", Float) = -4.50

        _WaveDisplacement ("Do Nhap Nho Mat Duong (Vertex Wave)", Range(0, 0.12)) = 0.035
        _ShadowStrength ("Do Dam Bong Do URP", Range(0, 1)) = 0.52
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

            sampler2D _MainTex;

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _SoilTexTiling;
                float _UseMeshUV;
                half4 _WornTrailColor;
                half _TrailBlend;
                float _GradientTopY;
                float _GradientBottomY;
                half _WaveDisplacement;
                half _ShadowStrength;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(IN.normalOS);

                float3 worldPos = posInputs.positionWS;

                // Biến dạng nhẹ các đỉnh mặt đường theo sóng tự nhiên
                float isTopFace = saturate(normInputs.normalWS.y * 1.5);
                float wave = sin(worldPos.x * 0.85) * 0.035 + cos(worldPos.x * 2.3 + worldPos.z * 1.5) * 0.020;
                worldPos.y += wave * isTopFace * _WaveDisplacement * 20.0;

                OUT.positionWS = worldPos;
                OUT.positionHCS = TransformWorldToHClip(worldPos);
                OUT.normalWS = normInputs.normalWS;
                OUT.color = IN.color;
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // 1. Tính toán tọa độ UV
                float2 sampleUV;
                if (_UseMeshUV > 0.5)
                {
                    sampleUV = IN.uv * _SoilTexTiling.xy + _SoilTexTiling.zw;
                }
                else
                {
                    float u = IN.positionWS.x * _SoilTexTiling.x * 0.25;
                    float v = saturate((IN.positionWS.y - _GradientBottomY) / max(0.01, _GradientTopY - _GradientBottomY));
                    sampleUV = float2(u, v);
                }

                // 2. Lấy mẫu texture tranh vẽ Painterly Cel-Shaded
                half4 texCol = tex2D(_MainTex, sampleUV);
                half3 groundColor = texCol.rgb;

                // 3. Vết đất mòn dọc theo tim đường (chỉ ảnh hưởng mặt trên Normal.y > 0.5)
                float3 N = normalize(IN.normalWS);
                float isTopSurface = saturate((N.y - 0.4) * 3.0);
                float trailMask = smoothstep(0.48, 0.08, abs(IN.positionWS.z)) * isTopSurface;
                groundColor = lerp(groundColor, _WornTrailColor.rgb, trailMask * _TrailBlend);

                // 4. Nhận ánh sáng và bóng đổ từ URP Directional Light
                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half shadowAtten = mainLight.shadowAttenuation;
                half shadowMask = (1.0h - shadowAtten) * saturate(_ShadowStrength);
                groundColor *= lerp(1.0h, 0.68h, shadowMask);

                return half4(groundColor, 1.0);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
