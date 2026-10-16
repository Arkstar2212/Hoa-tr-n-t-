Shader "HoaVaMau/Dredge_FacetedOcean"
{
    Properties
    {
        [Header(Dredge Deep Ocean Colors)]
        _DeepColor ("Deep Ocean Navy", Color) = (0.04, 0.08, 0.16, 1.0)
        _ShallowColor ("Near Shore Cyan/Teal", Color) = (0.10, 0.28, 0.38, 0.95)
        _DepthDistance ("Depth Blend Distance", Range(0.1, 30.0)) = 6.0

        [Header(Faceted Gerstner Waves)]
        _WaveSpeed ("Wave Speed", Float) = 1.0
        _WaveHeight ("Wave Height Multiplier", Range(0.0, 3.0)) = 0.55
        _WaveA ("Wave A (DirX, DirZ, Steepness, Wavelength)", Vector) = (0.9, 0.4, 0.55, 14.0)
        _WaveB ("Wave B (DirX, DirZ, Steepness, Wavelength)", Vector) = (-0.3, 0.9, 0.40, 8.5)
        _WaveC ("Wave C (DirX, DirZ, Steepness, Wavelength)", Vector) = (0.6, -0.8, 0.30, 5.0)

        [Header(Stylized Crest Foam)]
        _FoamColor ("Foam Tint", Color) = (0.92, 0.96, 1.0, 1.0)
        _CrestFoamThreshold ("Crest Foam Height Threshold", Range(-1.0, 2.0)) = 0.22
        _CrestFoamWidth ("Crest Foam Thickness", Range(0.01, 1.0)) = 0.35
        _CrestFoamNoiseScale ("Foam Noise Scale", Float) = 4.0
        _CrestFoamNoiseCutoff ("Foam Noise Cutoff", Range(0.0, 1.0)) = 0.45

        [Header(Contact Edge Foam)]
        _ContactFoamDistance ("Contact Edge Foam Distance", Range(0.01, 2.0)) = 0.65

        [Header(Sunset Golden Specular)]
        _SpecularColor ("Sunset Sun Specular Tint", Color) = (1.0, 0.78, 0.38, 1.0)
        _SpecularPower ("Gloss Power", Range(4.0, 256.0)) = 32.0
        _SpecularIntensity ("Specular Intensity", Range(0.0, 8.0)) = 4.5
        _SpecularStep ("Cel-Shaded Specular Hardness", Range(0.0, 1.0)) = 0.25

        [Header(Flat Shading)]
        _FlatShadingStrength ("Faceted Shading Blend (0=Smooth, 1=LowPoly)", Range(0.0, 1.0)) = 1.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent-100" 
            "RenderPipeline" = "UniversalPipeline" 
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float4 screenPos    : TEXCOORD1;
                float3 worldPos     : TEXCOORD2;
                float3 smoothNormal : TEXCOORD3;
                float waveElevation : TEXCOORD4;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _DeepColor;
                float4 _ShallowColor;
                float _DepthDistance;

                float _WaveSpeed;
                float _WaveHeight;
                float4 _WaveA;
                float4 _WaveB;
                float4 _WaveC;

                float4 _FoamColor;
                float _CrestFoamThreshold;
                float _CrestFoamWidth;
                float _CrestFoamNoiseScale;
                float _CrestFoamNoiseCutoff;
                float _ContactFoamDistance;

                float4 _SpecularColor;
                float _SpecularPower;
                float _SpecularIntensity;
                float _SpecularStep;
                float _FlatShadingStrength;
            CBUFFER_END

            float3 GerstnerWave(float4 wave, float3 p, inout float3 tangent, inout float3 binormal, float timeVal)
            {
                float2 dir = normalize(wave.xy);
                float wavelength = max(0.01, wave.w);
                float k = 2.0 * PI / wavelength;
                float c = sqrt(9.8 / k);
                float f = k * (dot(dir, p.xz) - c * timeVal * _WaveSpeed);
                float a = wave.z / k * _WaveHeight;

                tangent += float3(
                    -dir.x * dir.x * (wave.z * sin(f)),
                    dir.x * (wave.z * cos(f)),
                    -dir.x * dir.y * (wave.z * sin(f))
                );

                binormal += float3(
                    -dir.x * dir.y * (wave.z * sin(f)),
                    dir.y * (wave.z * cos(f)),
                    -dir.y * dir.y * (wave.z * sin(f))
                );

                return float3(
                    dir.x * (a * cos(f)),
                    a * sin(f),
                    dir.y * (a * cos(f))
                );
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);

                float3 tangent = float3(1, 0, 0);
                float3 binormal = float3(0, 0, 1);
                float3 p = worldPos;

                float timeVal = _Time.y;
                float3 disp = float3(0, 0, 0);
                disp += GerstnerWave(_WaveA, worldPos, tangent, binormal, timeVal);
                disp += GerstnerWave(_WaveB, worldPos, tangent, binormal, timeVal);
                disp += GerstnerWave(_WaveC, worldPos, tangent, binormal, timeVal);
                p += disp;

                float3 normal = normalize(cross(binormal, tangent));

                output.positionCS = TransformWorldToHClip(p);
                output.screenPos  = ComputeScreenPos(output.positionCS);
                output.worldPos   = p;
                output.smoothNormal = normal;
                output.waveElevation = disp.y;
                output.uv         = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 1. Calculate Faceted / Flat-Shaded Normal across polygon faces
                float3 ddxWorld = ddx(input.worldPos);
                float3 ddyWorld = ddy(input.worldPos);
                float3 facetedNormal = normalize(cross(ddyWorld, ddxWorld));
                float3 N = normalize(lerp(input.smoothNormal, facetedNormal, _FlatShadingStrength));

                // 2. Depth Calculation & Gradient
                float2 screenUV = input.screenPos.xy / max(0.0001, input.screenPos.w);
                #if UNITY_REVERSED_Z
                    float rawDepth = SampleSceneDepth(screenUV);
                #else
                    float rawDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, SampleSceneDepth(screenUV));
                #endif

                float linearSceneDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                float surfaceDepth = input.screenPos.w;
                float depthDifference = linearSceneDepth - surfaceDepth;
                bool isValidDepth = (depthDifference > 0.001 && depthDifference < 300.0);

                float depthFactor = isValidDepth ? saturate(depthDifference / max(0.01, _DepthDistance)) : 0.9;
                half3 waterColor = lerp(_ShallowColor.rgb, _DeepColor.rgb, depthFactor);

                // 3. Faceted Ambient Diffuse Shading (Enhances 3D Low-Poly Wave Chisel)
                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(N, mainLight.direction));
                // Stepped lighting band for stylized cel look
                float celDiffuse = smoothstep(0.1, 0.35, NdotL) * 0.4 + 0.6;
                waterColor *= celDiffuse;

                // 4. Dredge Signature Wave Crest Foam
                // Stylized painterly foam glints on wave crests
                float crestT = saturate((input.waveElevation - _CrestFoamThreshold) / max(0.01, _CrestFoamWidth));
                float2 fUV = input.worldPos.xz * (_CrestFoamNoiseScale * 0.25);
                float n1 = sin(fUV.x * 2.5 + fUV.y * 1.8 + _Time.y * 0.6);
                float n2 = cos(fUV.x * 1.3 - fUV.y * 2.9 + _Time.y * 0.4);
                float patchNoise = saturate((n1 + n2) * 0.5 + 0.5);
                float crestFoamMask = smoothstep(_CrestFoamNoiseCutoff, _CrestFoamNoiseCutoff + 0.2, crestT * patchNoise);

                // 5. Shoreline & Boat Hull Contact Edge Foam
                float contactFoamMask = 0.0;
                if (isValidDepth && depthDifference < _ContactFoamDistance)
                {
                    float edgeT = 1.0 - (depthDifference / _ContactFoamDistance);
                    contactFoamMask = smoothstep(0.25, 0.65, edgeT);
                }

                float totalFoam = saturate(crestFoamMask * 0.75 + contactFoamMask);
                waterColor = lerp(waterColor, _FoamColor.rgb, totalFoam * _FoamColor.a);

                // 6. Sunset Golden Specular Reflection
                float3 viewDir = normalize(GetCameraPositionWS() - input.worldPos);
                float3 halfVec = normalize(mainLight.direction + viewDir);
                float NdotH = saturate(dot(N, halfVec));
                float rawSpec = pow(NdotH, _SpecularPower);

                // Cel-shaded stepped specular cutoff for painterly glints
                float specCutoff = smoothstep(_SpecularStep, _SpecularStep + 0.3, rawSpec);
                float specAmount = lerp(rawSpec, specCutoff, 0.7) * _SpecularIntensity;
                half3 specularGlint = _SpecularColor.rgb * (specAmount * mainLight.color * (1.0 - totalFoam * 0.8));
                waterColor += specularGlint;

                // 7. Alpha
                float alpha = lerp(_ShallowColor.a, _DeepColor.a, depthFactor);
                alpha = max(alpha, totalFoam * _FoamColor.a);

                return half4(waterColor, alpha);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
