// ForestSilhouette.shader
// Near-black unlit shader for extreme foreground tree trunks, ferns, and branch silhouettes.
// The dark foreground framing (like the Planet of Lana massive trunks) is achieved by
// rendering these as almost pure black silhouettes with very subtle depth-based softening.
// A hint of very dark green-grey prevents them looking like solid black rectangles.

Shader "HoaVaMau/ForestSilhouette"
{
    Properties
    {
        _SilhouetteColor ("Silhouette Color",  Color) = (0.04, 0.06, 0.05, 1)
        _EdgeSoften      ("Edge Soften",       Range(0.0, 1.0)) = 0.18
        _DepthDarken     ("Depth Darken",      Range(0.0, 1.0)) = 0.25
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "Queue"          = "Geometry-1"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        ZWrite On

        Pass
        {
            Name "SilhouetteForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _SilhouetteColor;
                float  _EdgeSoften;
                float  _DepthDarken;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.uv         = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Very subtle edge variation to avoid perfectly flat silhouette look
                float edgeFactor = sin(input.uv.x * 6.283 * 2.0) * _EdgeSoften * 0.05;
                half3 col = _SilhouetteColor.rgb + edgeFactor;

                // Nearly invisible — this is foreground FRAMING, not a detail object
                return half4(saturate(col), 1.0);
            }
            ENDHLSL
        }
    }
}
