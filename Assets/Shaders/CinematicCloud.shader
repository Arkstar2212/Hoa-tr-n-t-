Shader "HoaVaMau/CinematicCloud"
{
    Properties
    {
        [PerRendererData] _MainTex ("Cloud Sprite Texture", 2D) = "white" {}
        [Header(Atmospheric Shading)]
        _AtmosphereTint ("Atmosphere Tint", Color) = (1.0, 1.0, 1.0, 1.0)
        _RimColor ("Sunlit Rim Color", Color) = (1.0, 0.98, 0.92, 1.0)
        _RimIntensity ("Rim Intensity", Range(0.0, 2.0)) = 0.6
        _ShadowTint ("Ambient Shadow Tint", Color) = (0.55, 0.68, 0.76, 1.0)
        _CloudBrightness ("Cloud Brightness", Range(0.2, 2.0)) = 1.0
        _CloudContrast ("Cloud Contrast", Range(0.5, 2.0)) = 1.0
        
        [Header(Horizon Aerial Perspective)]
        _HorizonHazeColor ("Horizon Haze Color", Color) = (0.61, 0.86, 0.90, 1.0)
        _HorizonFadeHeight ("Horizon Fade Depth", Range(0.0, 0.5)) = 0.15
        
        [Header(Subtle Organic Breathing)]
        _BillowStrength ("Billow Amplitude", Range(0.0, 0.03)) = 0.008
        _BillowSpeed ("Billow Speed", Range(0.1, 1.5)) = 0.35
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "RenderPipeline"="UniversalPipeline" 
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
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
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 vertexColor : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _AtmosphereTint;
                float4 _RimColor;
                float4 _ShadowTint;
                float4 _HorizonHazeColor;
                float _RimIntensity;
                float _CloudBrightness;
                float _CloudContrast;
                float _HorizonFadeHeight;
                float _BillowStrength;
                float _BillowSpeed;
            CBUFFER_END

            // Global environment variables driven by EnvironmentManager
            float4 _GlobalAtmosphereTint;
            float4 _GlobalRimColor;
            float4 _GlobalShadowTint;
            float4 _GlobalHorizonHaze;
            float _GlobalCloudSpeed;

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                // Subtle organic billowing (low frequency, low amplitude)
                float time = _Time.y * _BillowSpeed;
                float breatheY = sin(input.positionOS.x * 2.2 + time) * _BillowStrength;
                float breatheX = cos(input.positionOS.y * 1.8 + time * 0.7) * (_BillowStrength * 0.6);

                float3 displacedPos = input.positionOS.xyz;
                displacedPos.x += breatheX;
                displacedPos.y += breatheY;

                output.positionCS = TransformObjectToHClip(displacedPos);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.vertexColor = input.color;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                if (baseTex.a <= 0.01) discard;

                // Safe fallbacks for global shader parameters (prevents pitch black in Edit Mode)
                half4 globalAtmo = length(_GlobalAtmosphereTint.rgb) > 0.05 ? _GlobalAtmosphereTint : half4(1, 1, 1, 1);
                half4 globalRim = length(_GlobalRimColor.rgb) > 0.05 ? _GlobalRimColor : half4(1, 0.98, 0.92, 1);
                half4 globalShadow = length(_GlobalShadowTint.rgb) > 0.05 ? _GlobalShadowTint : half4(0.55, 0.68, 0.76, 1);
                half4 globalHaze = length(_GlobalHorizonHaze.rgb) > 0.05 ? _GlobalHorizonHaze : half4(0.61, 0.86, 0.90, 1);

                half3 atmoTint = _AtmosphereTint.rgb * globalAtmo.rgb;
                half3 rimCol = _RimColor.rgb * globalRim.rgb;
                half3 shadowCol = _ShadowTint.rgb * globalShadow.rgb;
                half3 hazeCol = _HorizonHazeColor.rgb * globalHaze.rgb;

                // 1. Calculate original painted luminance
                float lum = dot(baseTex.rgb, float3(0.299, 0.587, 0.114));
                lum = saturate(pow(lum, _CloudContrast) * _CloudBrightness);

                // 2. Luminance split: separate sunlit crests from volumetric shadows
                float highlightMask = smoothstep(0.40, 0.80, lum);

                // 3. Composite volumetric atmospheric lighting
                half3 litColor = baseTex.rgb * atmoTint;
                litColor = lerp(litColor * shadowCol, litColor * rimCol, highlightMask);
                litColor += rimCol * (highlightMask * _RimIntensity * 0.4);

                // Multiply by SpriteRenderer vertex color (for depth-tier dimming)
                litColor *= input.vertexColor.rgb;
                half alpha = baseTex.a * input.vertexColor.a;

                // 4. Subtle atmospheric horizon haze fade at the base
                if (_HorizonFadeHeight > 0.001)
                {
                    float horizonFade = smoothstep(0.0, _HorizonFadeHeight, input.uv.y);
                    litColor = lerp(hazeCol, litColor, horizonFade);
                }

                return half4(litColor, alpha);
            }
            ENDHLSL
        }
    }
}
