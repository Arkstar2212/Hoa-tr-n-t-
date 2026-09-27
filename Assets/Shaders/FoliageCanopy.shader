// FoliageCanopy.shader
// Stylized low-poly tree canopy for Hoa va Mau.
// Features:
//   - Two-tone green gradient (shadow base -> sunlit tip)
//   - GPU vertex wind sway driven by _GlobalWind and _FoliageWindMult
//   - Atmospheric depth fade toward _GlobalHorizonHaze color
//   - Reads EnvironmentManager globals for day/dusk/night color shifts
//   - Cull Off (double-sided) so canopy clusters look full from all angles

Shader "HoaVaMau/FoliageCanopy"
{
    Properties
    {
        [Header(Canopy Colors)]
        _ShadowColor    ("Shadow / Base Color",   Color) = (0.06, 0.20, 0.08, 1)
        _MidColor       ("Mid Foliage Color",     Color) = (0.16, 0.42, 0.18, 1)
        _SunlitColor    ("Sunlit Tip Color",      Color) = (0.38, 0.72, 0.22, 1)
        _ColorVariation ("Color Variation",       Range(0.0, 0.5)) = 0.12

        [Header(Wind Sway)]
        _WindAmplitude  ("Wind Amplitude",        Range(0.0, 1.0)) = 0.35
        _WindFrequency  ("Wind Frequency",        Range(0.1, 4.0)) = 1.2
        _WindTurbulence ("Wind Turbulence",       Range(0.0, 2.0)) = 0.6

        [Header(Atmosphere)]
        _DepthFogStart  ("Fog Start Depth (Z)",   Float) = 2.0
        _DepthFogEnd    ("Fog End Depth (Z)",     Float) = 18.0
        _DepthFogColor  ("Fog Color Override",    Color) = (0.25, 0.52, 0.58, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "Queue"          = "Geometry+1"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off

        Pass
        {
            Name "FoliageForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalOS   : NORMAL;
                float4 color      : COLOR;     // Vertex color: R=height 0-1, G=cluster ID phase
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float  height     : TEXCOORD1;
                float  fogFactor  : TEXCOORD2;
                float3 normalWS   : TEXCOORD3;
                float  colorSeed  : TEXCOORD4;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _ShadowColor;
                float4 _MidColor;
                float4 _SunlitColor;
                float  _ColorVariation;
                float  _WindAmplitude;
                float  _WindFrequency;
                float  _WindTurbulence;
                float  _DepthFogStart;
                float  _DepthFogEnd;
                float4 _DepthFogColor;
            CBUFFER_END

            // Globals from WindManager
            float4 _GlobalWind;
            float  _FoliageWindMult;

            // Globals from EnvironmentManager
            float4 _GlobalHorizonHaze;
            float4 _GlobalAtmosphereTint;

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);

                // Height parameter: use UV.y (0 = cluster center / base, 1 = outer tips)
                float h = saturate(input.uv.y);
                float bend = h * h;   // tips sway more

                float time = _Time.y * _WindFrequency;
                float windMult = max(0.0, _FoliageWindMult > 0.01 ? _FoliageWindMult : 0.6);

                // Position-seeded phase offset so each cluster is unique
                float phase = worldPos.x * 0.18 + worldPos.z * 0.31;

                float wave1 = sin(time * 1.0  + phase)             * 1.0;
                float wave2 = cos(time * 0.73 + phase * 1.4)       * 0.5;
                float turb  = sin(time * 2.8  + phase * 0.9) * _WindTurbulence * 0.25;
                float gust  = saturate(sin(time * 0.45 + sin(worldPos.x * 0.3)) * 2.0 - 0.5) * 0.8;

                float totalSway = (wave1 + wave2 + turb + gust) * windMult;

                // Apply wind globally from WindManager
                float windPower = length(_GlobalWind.xyz) > 0.01 ? (0.5 + abs(_GlobalWind.w) * 1.0) : 1.0;
                float displace = totalSway * _WindAmplitude * windPower;

                float3 displaced = input.positionOS.xyz;
                displaced.x += displace * bend;
                displaced.z += displace * bend * 0.3;
                displaced.y -= abs(displace) * 0.08 * bend;

                output.positionCS = TransformObjectToHClip(displaced);
                output.positionWS = TransformObjectToWorld(displaced);
                output.normalWS   = TransformObjectToWorldNormal(input.normalOS);
                output.height     = h;
                output.colorSeed  = input.color.g; // cluster phase stored in vertex green

                // Depth-based atmospheric fog factor
                float depth = abs(worldPos.z);
                output.fogFactor = saturate((depth - _DepthFogStart) / max(0.01, _DepthFogEnd - _DepthFogStart));

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float h = input.height;

                // Three-stop canopy gradient
                half3 col = lerp(_ShadowColor.rgb, _MidColor.rgb, saturate(h * 1.8));
                col        = lerp(col, _SunlitColor.rgb, saturate((h - 0.4) * 2.5));

                // Per-cluster color variation using colorSeed
                float variation = (input.colorSeed - 0.5) * _ColorVariation;
                col.g += variation;
                col    = saturate(col);

                // Simple lighting using world normal
                float3 lightDir = normalize(float3(0.4, 1.0, 0.3));
                float  NdotL    = saturate(dot(normalize(input.normalWS), lightDir));
                col *= 0.65 + 0.35 * NdotL;

                // Apply EnvironmentManager atmosphere tint
                half3 atmoTint = length(_GlobalAtmosphereTint.rgb) > 0.05
                    ? _GlobalAtmosphereTint.rgb : half3(1,1,1);
                col *= lerp(half3(1,1,1), atmoTint, 0.25);

                // Depth atmospheric fog toward horizon haze
                half3 fogColor = length(_GlobalHorizonHaze.rgb) > 0.05
                    ? _GlobalHorizonHaze.rgb : _DepthFogColor.rgb;
                col = lerp(col, fogColor, input.fogFactor * 0.75);

                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
