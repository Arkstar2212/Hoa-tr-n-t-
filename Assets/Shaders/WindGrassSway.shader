Shader "HoaVaMau/WindGrassSway"
{
    Properties
    {
        _MainTex ("Grass Sprite Texture", 2D) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.12

        [Header(Layer Silhouette vs Vibrant Gradient)]
        [Toggle] _SingleColorMode ("Single Color / Silhouette Mode", Float) = 0.0
        _SingleColor ("Silhouette Tint", Color) = (0.08, 0.18, 0.10, 1.0)

        [Header(Atmospheric Gradient Shading)]
        _BaseShadowColor ("Base Shadow Color", Color) = (0.09, 0.22, 0.11, 1.0)
        _MidBladeColor ("Mid Blade Color", Color) = (0.24, 0.56, 0.20, 1.0)
        _SunlitTipColor ("Sunlit Tip Highlight", Color) = (0.72, 0.94, 0.35, 1.0)
        _SpecularShimmerColor ("Wind Shimmer Glint", Color) = (0.92, 0.99, 0.75, 1.0)

        [Header(Wind Physics & Turbulence)]
        _WindSwayStrength ("Wind Sway Amplitude", Range(0.0, 0.6)) = 0.18
        _WindFrequency ("Base Wind Frequency", Float) = 1.8
        _GrassStiffness ("Grass Spring Stiffness", Range(0.5, 3.0)) = 1.0

        [Header(Player Interaction)]
        [Toggle] _EnablePlayerPush ("Enable Player Push", Float) = 1.0
        _PushStrength ("Push Response Strength", Range(0.0, 2.0)) = 0.85
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "TransparentCutout" 
            "Queue" = "AlphaTest+50" 
            "RenderPipeline" = "UniversalPipeline" 
        }

        Cull Off
        ZWrite On

        Pass
        {
            Name "WindGrassForward"
            Tags { "LightMode" = "UniversalForwardOnly" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float4 vertexColor  : COLOR;
                float  shimmerFactor: TEXCOORD1;
                float3 normalWS     : NORMAL;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float  _Cutoff;
                float  _SingleColorMode;
                float4 _SingleColor;
                float4 _BaseShadowColor;
                float4 _MidBladeColor;
                float4 _SunlitTipColor;
                float4 _SpecularShimmerColor;
                float  _WindSwayStrength;
                float  _WindFrequency;
                float  _GrassStiffness;
                float  _EnablePlayerPush;
                float  _PushStrength;
            CBUFFER_END

            // Global environment vectors fed by WindManager, GrassManager & EnvironmentManager
            float4 _GlobalWind;
            float4 _PlayerPosition;          // (pos.x, pos.y, pos.z, radius)
            float4 _PlayerMovement;          // (smoothVel.x, smoothVel.y, speed, dirFactor)
            float4 _PlayerTrail0;            // (trail0.x, trail0.y, trail0.z, ageNorm)
            float4 _GrassSpringParams;       // (bendStrength, recoverySpeed, springFreq, springDamping)
            float4 _GrassInteractionParams;  // (dragWeight, partingWeight, trampleWeight, idleDisp)
            float4 _GrassLayerWeights;        // (backWeight, middleWeight, frontWeight, 1.0)
            float  _GlobalWindStrength;
            float4 _GlobalGrassBase;
            float4 _GlobalGrassTip;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.vertexColor = input.color;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);

                // Root anchoring: 0.0 at soil (bottom uv.y = 0), 1.0 at blade tips (uv.y = 1)
                float tipMask = saturate(input.uv.y);
                float bendCurve = tipMask * tipMask;

                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                float time = (_Time.y * _WindFrequency) + frac(worldPos.x * 0.173 + worldPos.z * 0.317) * 6.28;

                // 1. MULTI-OCTAVE AMBIENT WIND
                float eddy1 = sin(worldPos.x * 0.35 - time * 1.5);
                float eddy2 = cos(worldPos.x * 0.9 + time * 2.2) * sin(worldPos.x * 0.5 - time * 1.1);
                float gustEnvelope = saturate(sin(time * 0.6 + sin(worldPos.x * 0.25)) * 1.5 - 0.3);
                float microFlutter = sin(_Time.y * 7.5 + worldPos.x * 4.0) * 0.08;

                float windStrength = _GlobalWindStrength > 0.01 ? _GlobalWindStrength : 0.85;
                if (length(_GlobalWind.xyz) > 0.01)
                {
                    windStrength *= (0.6 + abs(_GlobalWind.w) * 0.8);
                }

                float totalWindForce = (eddy1 * 0.35 + eddy2 * 0.45 + gustEnvelope * 1.1 + microFlutter) * (windStrength + 0.25);
                float displacement = totalWindForce * _WindSwayStrength / max(0.2, _GrassStiffness);

                float3 displacedOS = input.positionOS.xyz;
                displacedOS.x += displacement * bendCurve;
                displacedOS.y -= abs(displacement) * 0.14 * bendCurve;

                // 2. CHARACTER INTERACTION (DRAG + TRAMPLE + SPRING TRAIL)
                if (_EnablePlayerPush > 0.5 && _PlayerPosition.w > 0.05)
                {
                    float3 toGrass = worldPos - _PlayerPosition.xyz;
                    float distXZ = length(float2(toGrass.x, toGrass.z));
                    float distY  = abs(toGrass.y);

                    float pushRadius = _PlayerPosition.w > 0.1 ? _PlayerPosition.w : 1.45;
                    float footRadius = 0.55;

                    // Layer weight selection
                    float layerWeight = (_SingleColorMode > 0.5) ? _GrassLayerWeights.x : _GrassLayerWeights.y;
                    if (layerWeight < 0.001) layerWeight = 1.0;

                    float dragWeight    = _GrassInteractionParams.x > 0.01 ? _GrassInteractionParams.x : 0.85;
                    float partingWeight = _GrassInteractionParams.y > 0.01 ? _GrassInteractionParams.y : 0.45;
                    float trampleWeight = _GrassInteractionParams.z > 0.01 ? _GrassInteractionParams.z : 0.85;
                    float idleDisp      = _GrassInteractionParams.w > 0.01 ? _GrassInteractionParams.w : 0.20;

                    float springBend = _GrassSpringParams.x > 0.01 ? _GrassSpringParams.x : _PushStrength;
                    float springFreq = _GrassSpringParams.z > 0.1 ? _GrassSpringParams.z : 10.5;
                    float springDamp = _GrassSpringParams.w > 0.1 ? _GrassSpringParams.w : 5.2;

                    float3 pushWorld = float3(0, 0, 0);

                    // Active Proximity
                    if (distY < 1.7 && distXZ < pushRadius)
                    {
                        float smoothDirX = -_PlayerMovement.w;
                        float speed      = _PlayerMovement.z;
                        float moveIntensity = saturate(speed / 3.0);

                        float splitSignZ = (toGrass.z >= 0.0) ? 1.0 : -1.0;
                        float zBias = max(abs(toGrass.z), 0.25);
                        float2 partDir = normalize(float2(toGrass.x, splitSignZ * zBias));

                        float prox = saturate(1.0 - (distXZ / pushRadius));
                        float smoothProx = smoothstep(0.0, 1.0, prox);

                        // Stepped-on trample underfoot
                        float trample = saturate(1.0 - (distXZ / footRadius));
                        float flattenDown = trample * trample * 0.45 * trampleWeight;

                        float horizDrag = smoothDirX * (moveIntensity * dragWeight + (1.0 - moveIntensity) * idleDisp);
                        float horizPart = partDir.x * partingWeight * (1.0 - moveIntensity * 0.4);
                        float pushX = (horizDrag + horizPart) * smoothProx * springBend;
                        float pushZ = partDir.y * partingWeight * smoothProx * springBend;

                        pushWorld = float3(pushX, -flattenDown, pushZ);
                    }

                    // Trailing Wake & Damped Rebound
                    if (_PlayerTrail0.w < 0.98)
                    {
                        float3 toTrail = worldPos - _PlayerTrail0.xyz;
                        float distTrail = length(float2(toTrail.x, toTrail.z));
                        float trailRadius = pushRadius * 0.85;

                        if (distTrail < trailRadius)
                        {
                            float trailProx = saturate(1.0 - (distTrail / trailRadius));
                            float age = _PlayerTrail0.w;
                            float springOsc = exp(-age * springDamp) * cos(age * springFreq);
                            float trailDragX = -sign(_PlayerMovement.x);
                            pushWorld.x += trailDragX * trailProx * springOsc * springBend * 0.45;
                        }
                    }

                    displacedOS += TransformWorldToObjectDir(pushWorld * (layerWeight * bendCurve));
                }

                output.positionCS = TransformObjectToHClip(displacedOS);
                output.shimmerFactor = saturate((abs(displacement) * tipMask - 0.05) * 5.0);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                if (baseTex.a < _Cutoff) discard;

                float verticalGrad = saturate(input.uv.y);

                half3 col;
                if (_SingleColorMode > 0.5)
                {
                    // Silhouette mode with subtle height nuance
                    col = _SingleColor.rgb * (0.82 + 0.18 * verticalGrad);
                }
                else
                {
                    // Atmospheric synced 3-stop gradient
                    half3 globalBase = length(_GlobalGrassBase.rgb) > 0.05 ? _GlobalGrassBase.rgb : half3(1, 1, 1);
                    half3 globalTip  = length(_GlobalGrassTip.rgb)  > 0.05 ? _GlobalGrassTip.rgb  : half3(1, 1, 1);

                    half3 baseCol = _BaseShadowColor.rgb * globalBase;
                    half3 midCol  = _MidBladeColor.rgb  * lerp(globalBase, globalTip, 0.45);
                    half3 tipCol  = _SunlitTipColor.rgb  * globalTip;

                    half3 grassTint = lerp(baseCol, midCol, saturate(verticalGrad * 1.5));
                    grassTint = lerp(grassTint, tipCol, saturate((verticalGrad - 0.4) * 1.8));

                    // Shimmer glint on wind crests
                    grassTint = lerp(grassTint, _SpecularShimmerColor.rgb, input.shimmerFactor * 0.45);

                    // Combine texture details with painterly gradient
                    col = lerp(baseTex.rgb * 0.85, baseTex.rgb * grassTint * 1.6, 0.72);
                }

                col *= input.vertexColor.rgb;

                // Subtle directional lighting touch
                half3 safeNormal = (dot(input.normalWS, input.normalWS) > 0.01) ? normalize(input.normalWS) : half3(0, 1, 0);
                float NdotL = saturate(dot(safeNormal, normalize(float3(0.25, 0.95, 0.25))));
                col *= 0.82 + 0.18 * NdotL;

                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
