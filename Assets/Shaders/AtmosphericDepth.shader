// AtmosphericDepth.shader
// Used for distant background forest silhouette strips (Z = 8 to 25).
// Blends a near-color (dark forest green) toward a far-color (atmospheric blue-green haze)
// based on the object's Z distance. Picks up EnvironmentManager global haze color.
// Renders as Opaque Background geometry — very cheap, no lighting.

Shader "HoaVaMau/AtmosphericDepth"
{
    Properties
    {
        _NearColor     ("Near Forest Color",   Color) = (0.10, 0.22, 0.14, 1)
        _FarColor      ("Far Haze Color",      Color) = (0.25, 0.48, 0.55, 1)
        _DepthNear     ("Near Depth (Z)",      Float) = 6.0
        _DepthFar      ("Far Depth (Z)",       Float) = 25.0
        _VerticalFade  ("Vertical Sky Fade",   Range(0.0, 1.0)) = 0.35
        _WaveAmplitude ("Subtle Wave",         Range(0.0, 0.05)) = 0.015
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "Queue"          = "Background+1"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Back
        ZWrite On

        Pass
        {
            Name "AtmosphericForward"
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
                float  depthFactor: TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _NearColor;
                float4 _FarColor;
                float  _DepthNear;
                float  _DepthFar;
                float  _VerticalFade;
                float  _WaveAmplitude;
            CBUFFER_END

            // Globals from EnvironmentManager
            float4 _GlobalHorizonHaze;
            float4 _GlobalSkyHorizon;

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);

                // Gentle wave on top silhouette edge
                float wave = sin(worldPos.x * 0.4 + _Time.y * 0.3) * _WaveAmplitude
                           + sin(worldPos.x * 0.9 + _Time.y * 0.18 + 1.4) * _WaveAmplitude * 0.5;
                float3 displaced = input.positionOS.xyz;
                if (input.uv.y > 0.7)   // only top edge waves
                    displaced.y += wave;

                output.positionCS  = TransformObjectToHClip(displaced);
                output.uv          = input.uv;
                output.depthFactor = saturate((worldPos.z - _DepthNear) / max(0.01, _DepthFar - _DepthNear));
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Pick up environment haze if available
                half3 hazeFar = length(_GlobalHorizonHaze.rgb) > 0.05
                    ? _GlobalHorizonHaze.rgb : _FarColor.rgb;
                half3 skyHorizon = length(_GlobalSkyHorizon.rgb) > 0.05
                    ? _GlobalSkyHorizon.rgb : hazeFar;

                // Blend near forest dark → far atmospheric haze
                half3 col = lerp(_NearColor.rgb, hazeFar, input.depthFactor);

                // Vertical fade at top — blends into sky
                float topFade = saturate((input.uv.y - (1.0 - _VerticalFade)) / max(0.001, _VerticalFade));
                col = lerp(col, skyHorizon, topFade * topFade);

                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
