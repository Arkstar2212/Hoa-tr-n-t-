Shader "HoaVaMau/Stylized3DGrassTuft"
{
    Properties
    {
        [Header(Main Texture and Cutout)]
        _MainTex ("Grass Tuft Texture", 2D) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.18

        [Header(Stylized Painterly Tints)]
        _BaseShadowColor ("Base Shadow Color", Color) = (0.12, 0.25, 0.12, 1.0)
        _MidColor        ("Mid Blade Color",   Color) = (0.55, 0.72, 0.28, 1.0)
        _TipColor        ("Sunlit Tip Color",  Color) = (0.88, 0.98, 0.42, 1.0)
        _ShimmerColor    ("Wind Glint Color",  Color) = (0.96, 1.00, 0.78, 1.0)
        _ColorStrength   ("Color Gradient Strength", Range(0.0, 1.0)) = 0.45

        [Header(Wind Physics and Sway)]
        _WindSwayStrength ("Wind Sway Strength", Range(0.0, 1.0)) = 0.25
        _WindFrequency    ("Wind Frequency", Float) = 1.85
        _GrassStiffness   ("Grass Spring Stiffness", Range(0.2, 3.0)) = 1.0
        _Turbulence       ("Tip Flutter Turbulence", Range(0.0, 1.0)) = 0.35

        [Header(Player Interaction)]
        [Toggle] _EnablePlayerPush ("Enable Player Push Reaction", Float) = 1.0
        _PushStrength ("Player Push Strength", Range(0.0, 2.0)) = 0.85
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
            Name "Stylized3DGrassForward"
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
                float3 normalOS   : NORMAL;
                float4 color      : COLOR;
                float3 rootOS     : TEXCOORD1; // Root anchor point in object space
            };

            struct Varyings
            {
                float4 positionCS    : SV_POSITION;
                float2 uv            : TEXCOORD0;
                float3 normalWS      : NORMAL;
                float4 vertexColor   : COLOR;
                float  heightMask    : TEXCOORD1;
                float  shimmerFactor : TEXCOORD2;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float  _Cutoff;
                float4 _BaseShadowColor;
                float4 _MidColor;
                float4 _TipColor;
                float4 _ShimmerColor;
                float  _ColorStrength;
                float  _WindSwayStrength;
                float  _WindFrequency;
                float  _GrassStiffness;
                float  _Turbulence;
                float  _EnablePlayerPush;
                float  _PushStrength;
            CBUFFER_END

            // Environment & Character interaction globals (from GrassManager / WindManager)
            float4 _GlobalWind;
            float4 _PlayerPosition;          // (pos.x, pos.y, pos.z, radius)
            float4 _PlayerMovement;          // (smoothVel.x, smoothVel.y, speed, dirFactor)
            float4 _PlayerTrail0;            // (trail0.x, trail0.y, trail0.z, ageNorm)
            float4 _GrassSpringParams;       // (bendStrength, recoverySpeed, springFreq, springDamping)
            float4 _GrassInteractionParams;  // (dragWeight, partingWeight, trampleWeight, idleDisp)
            float  _GlobalWindStrength;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.vertexColor = input.color;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);

                // Root anchoring: 0 at base (UV.y = 0), 1 at tips (UV.y = 1)
                float h = saturate(input.uv.y);
                float bendCurve = h * h; // Quadratic curve: roots strictly stationary

                float3 rootWS = TransformObjectToWorld(input.rootOS);
                float3 vertWS = TransformObjectToWorld(input.positionOS.xyz);

                // 1. MULTI-OCTAVE COHERENT 3D WIND SWAY
                float time = (_Time.y * _WindFrequency) + frac(rootWS.x * 0.173 + rootWS.z * 0.317) * 6.28;
                float eddy1 = sin(rootWS.x * 0.35 - time * 1.5);
                float eddy2 = cos(rootWS.x * 0.85 + time * 2.1) * sin(rootWS.z * 0.5 - time * 1.1);
                float gustEnvelope = saturate(sin(time * 0.6 + sin(rootWS.x * 0.25)) * 1.5 - 0.25);
                float microFlutter = sin(_Time.y * 8.5 + rootWS.x * 4.2 + rootWS.z * 2.8) * _Turbulence * 0.12;

                float windPower = _GlobalWindStrength > 0.01 ? _GlobalWindStrength : 0.85;
                if (length(_GlobalWind.xyz) > 0.01)
                {
                    windPower *= (0.6 + abs(_GlobalWind.w) * 0.8);
                }

                float totalWindForce = (eddy1 * 0.35 + eddy2 * 0.45 + gustEnvelope * 1.15 + microFlutter) * (windPower / max(0.2, _GrassStiffness));
                float displaceAmt = totalWindForce * _WindSwayStrength;

                // 3D wind displacement vector in world space:
                // Primary horizontal sway along X, subtle Z drift, and slight natural compression along Y
                float3 totalDisplaceWS = float3(displaceAmt, -abs(displaceAmt) * 0.14, displaceAmt * 0.22) * bendCurve;

                // 2. CHARACTER INTERACTION (DRAG + TRAMPLE + SPRING REBOUND)
                if (_EnablePlayerPush > 0.5 && _PlayerPosition.w > 0.05)
                {
                    float3 toGrass = rootWS - _PlayerPosition.xyz;
                    float distXZ = length(float2(toGrass.x, toGrass.z));
                    float distY  = abs(toGrass.y);

                    float pushRadius = _PlayerPosition.w > 0.1 ? _PlayerPosition.w : 1.45;
                    float footRadius = 0.55;

                    float dragWeight    = _GrassInteractionParams.x > 0.01 ? _GrassInteractionParams.x : 0.85;
                    float partingWeight = _GrassInteractionParams.y > 0.01 ? _GrassInteractionParams.y : 0.45;
                    float trampleWeight = _GrassInteractionParams.z > 0.01 ? _GrassInteractionParams.z : 0.85;
                    float idleDisp      = _GrassInteractionParams.w > 0.01 ? _GrassInteractionParams.w : 0.20;

                    float springBend = _GrassSpringParams.x > 0.01 ? _GrassSpringParams.x : _PushStrength;
                    float springFreq = _GrassSpringParams.z > 0.1 ? _GrassSpringParams.z : 10.5;
                    float springDamp = _GrassSpringParams.w > 0.1 ? _GrassSpringParams.w : 5.2;

                    float3 pushWS = float3(0, 0, 0);

                    if (distY < 1.7 && distXZ < pushRadius)
                    {
                        float smoothDirX = -_PlayerMovement.w;
                        float speed = _PlayerMovement.z;
                        float moveIntensity = saturate(speed / 2.5);

                        float prox = saturate(1.0 - (distXZ / pushRadius));
                        float smoothProx = smoothstep(0.0, 1.0, prox);

                        // Parting direction in 3D
                        float splitSignZ = (toGrass.z >= 0.0) ? 1.0 : -1.0;
                        float zBias = max(abs(toGrass.z), 0.25);
                        float2 partDir = normalize(float2(toGrass.x, splitSignZ * zBias));

                        // Stepped-on trample directly under player
                        float trample = saturate(1.0 - (distXZ / footRadius));
                        float flattenDown = trample * trample * 0.35 * trampleWeight;

                        float horizDrag = smoothDirX * (moveIntensity * dragWeight * 1.2 + (1.0 - moveIntensity) * idleDisp * 0.15);
                        float horizPart = partDir.x * partingWeight * 0.35;
                        float pushX = (horizDrag + horizPart) * smoothProx * springBend;
                        float pushZ = partDir.y * partingWeight * 0.45 * smoothProx * springBend;

                        pushWS = float3(pushX, -flattenDown, pushZ);
                    }

                    // Damped elastic trailing wake
                    if (_PlayerTrail0.w < 0.98)
                    {
                        float3 toTrail = rootWS - _PlayerTrail0.xyz;
                        float distTrail = length(float2(toTrail.x, toTrail.z));
                        float trailRadius = pushRadius * 0.85;

                        if (distTrail < trailRadius)
                        {
                            float trailProx = saturate(1.0 - (distTrail / trailRadius));
                            float age = _PlayerTrail0.w;
                            float springOsc = exp(-age * springDamp) * cos(age * springFreq);
                            float trailDragX = -sign(_PlayerMovement.x);
                            pushWS.x += trailDragX * trailProx * springOsc * springBend * 0.40;
                        }
                    }

                    totalDisplaceWS += pushWS * bendCurve;
                }

                // Apply world displacement to vertex position
                float3 finalWS = vertWS + totalDisplaceWS;
                output.positionCS = TransformWorldToHClip(finalWS);
                output.heightMask = h;
                output.shimmerFactor = saturate((abs(displaceAmt) * h - 0.06) * 5.5);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                if (tex.a < _Cutoff) discard;

                float h = input.heightMask;

                // Atmospheric 3-stop gradient tint
                half3 baseCol = _BaseShadowColor.rgb;
                half3 midCol  = _MidColor.rgb;
                half3 tipCol  = _TipColor.rgb;

                half3 gradientTint = lerp(baseCol, midCol, saturate(h * 1.6));
                gradientTint = lerp(gradientTint, tipCol, saturate((h - 0.4) * 1.8));

                // Shimmer glint on wind crests
                gradientTint = lerp(gradientTint, _ShimmerColor.rgb, input.shimmerFactor * 0.35);

                // Blend original painterly texture with gradient
                half3 col = lerp(tex.rgb, tex.rgb * gradientTint * 1.4, _ColorStrength);

                // Modulate by vertex color if provided
                if (any(input.vertexColor.rgb > 0.02))
                {
                    col *= input.vertexColor.rgb;
                }

                // Upward-biased painterly soft diffuse lighting
                half3 safeNormal = (dot(input.normalWS, input.normalWS) > 0.01) ? normalize(input.normalWS) : half3(0, 1, 0);
                float NdotL = saturate(dot(safeNormal, normalize(float3(0.2, 0.95, 0.25))));
                col *= (0.78 + 0.22 * NdotL);

                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
