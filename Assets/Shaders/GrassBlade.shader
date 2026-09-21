Shader "HoaVaMau/GrassBlade"
{
    Properties
    {
        [Header(Blade Colors)]
        _RootColor ("Root Shadow Color", Color) = (0.08, 0.22, 0.10, 1.0)
        _MidColor  ("Mid Blade Color",   Color) = (0.20, 0.50, 0.20, 1.0)
        _TipColor  ("Tip Highlight",     Color) = (0.52, 0.82, 0.30, 1.0)

        [Header(Wind Sway)]
        _SwayStrength  ("Sway Amplitude",  Range(0.0, 0.5)) = 0.18
        _SwayFrequency ("Sway Frequency",  Float) = 1.9

        [Header(Layer Configuration)]
        [Toggle] _EnablePlayerPush ("Enable Player Push Reaction", Float) = 1.0
        _PushStrength              ("Push Aside Strength", Range(0.0, 2.0)) = 0.75
        [Toggle] _SingleColorMode  ("Single Color Mode (Back Layer)", Float) = 0.0
        _SingleColor               ("Single Silhouette Color", Color) = (0.11, 0.23, 0.13, 1.0)
    }

    SubShader
    {
        Tags
        {
            "RenderType"  = "Opaque"
            "Queue"       = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off   // double-sided so blades show from all angles
        ZWrite On

        Pass
        {
            Name "GrassBladeForward"
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
                float3 rootOS     : TEXCOORD1;
                float4 uv2        : TEXCOORD2; // Deterministic randomness: (phase, heightScale, stiffness, leanOffset)
                float3 normalOS   : NORMAL;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float  bladeHeight : TEXCOORD0;  // 0 = root, 1 = tip
                float3 normalWS    : TEXCOORD1;
                float4 vertexColor : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _RootColor;
                float4 _MidColor;
                float4 _TipColor;
                float  _SwayStrength;
                float  _SwayFrequency;
                float  _EnablePlayerPush;
                float  _PushStrength;
                float  _SingleColorMode;
                float4 _SingleColor;
            CBUFFER_END

            // Global environment vectors fed by WindManager & GrassManager
            float4 _GlobalWind;
            float4 _PlayerPosition;          // (pos.x, pos.y, pos.z, radius)
            float4 _PlayerMovement;          // (smoothVel.x, smoothVel.y, speed, dirFactor)
            float4 _PlayerTrail0;            // (trail0.x, trail0.y, trail0.z, ageNorm)
            float4 _PlayerTrail1;            // (trail1.x, trail1.y, trail1.z, ageNorm)
            float4 _GrassSpringParams;       // (bendStrength, recoverySpeed, springFreq, springDamping)
            float4 _GrassInteractionParams;  // (dragWeight, partingWeight, trampleWeight, idleDisp)
            float4 _GrassLayerWeights;        // (backWeight, middleWeight, frontWeight, 1.0)

            Varyings vert(Attributes input)
            {
                Varyings output;

                float h    = saturate(input.uv.y);        // 0 root .. 1 tip
                float bend = h * h;                       // quadratic: roots strictly stationary

                float3 bladeRootOS = input.rootOS;
                float3 rootWS = TransformObjectToWorld(bladeRootOS);

                // Deterministic per-blade randomness from UV2 (or fallback hash)
                float randPhase = input.uv2.x;
                float stiffness = max(0.5, input.uv2.z > 0.01 ? input.uv2.z : 1.0);
                if (abs(randPhase) < 0.0001)
                {
                    randPhase = frac(sin(dot(rootWS.xz, float2(12.9898, 78.233))) * 43758.5453);
                }

                // 1. GLOBAL MULTI-OCTAVE AMBIENT WIND
                float time = (_Time.y * _SwayFrequency) + (randPhase * 6.28);
                float eddy1 = sin(rootWS.x * 0.35 - time * 1.5);
                float eddy2 = cos(rootWS.x * 0.9  + time * 2.2) * sin(rootWS.x * 0.5 - time * 1.1);
                float gust  = saturate(sin(time * 0.6 + sin(rootWS.x * 0.25)) * 1.5 - 0.3);
                float flutter = sin(_Time.y * 7.5 + rootWS.x * 4.0 + randPhase * 3.14) * 0.08;

                float windPower = 1.0;
                if (length(_GlobalWind.xyz) > 0.01)
                    windPower = (0.6 + abs(_GlobalWind.w) * 0.9);

                float totalSway = (eddy1 * 0.35 + eddy2 * 0.45 + gust * 1.1 + flutter) * (windPower / stiffness);
                float displaceX = totalSway * _SwayStrength;

                float3 displaced = input.positionOS.xyz;
                displaced.x += displaceX * bend;
                displaced.y -= abs(displaceX) * 0.12 * bend;

                // 2. CHARACTER INTERACTION (DIRECTIONAL DRAG + TRAMPLE + SPRING TRAIL)
                if (_EnablePlayerPush > 0.5 && _PlayerPosition.w > 0.05)
                {
                    float3 toBlade = rootWS - _PlayerPosition.xyz;
                    float  distXZ  = length(float2(toBlade.x, toBlade.z));
                    float  distY   = abs(toBlade.y);

                    float interactRadius = _PlayerPosition.w > 0.1 ? _PlayerPosition.w : 1.45;
                    float footRadius     = 0.55;

                    // Layer interaction weight (Back = x, Middle = y, Front = z)
                    float layerWeight = (_SingleColorMode > 0.5) ? _GrassLayerWeights.x : _GrassLayerWeights.y;
                    if (layerWeight < 0.001) layerWeight = 1.0;

                    // Global tuning parameters (with robust defaults)
                    float dragWeight    = _GrassInteractionParams.x > 0.01 ? _GrassInteractionParams.x : 0.85;
                    float partingWeight = _GrassInteractionParams.y > 0.01 ? _GrassInteractionParams.y : 0.45;
                    float trampleWeight = _GrassInteractionParams.z > 0.01 ? _GrassInteractionParams.z : 0.85;
                    float idleDisp      = _GrassInteractionParams.w > 0.01 ? _GrassInteractionParams.w : 0.20;

                    float springBend    = _GrassSpringParams.x > 0.01 ? _GrassSpringParams.x : _PushStrength;
                    float springFreq    = _GrassSpringParams.z > 0.1 ? _GrassSpringParams.z : 10.5;
                    float springDamp    = _GrassSpringParams.w > 0.1 ? _GrassSpringParams.w : 5.2;

                    float3 pushWorld = float3(0, 0, 0);

                    // A. ACTIVE CHARACTER PROXIMITY
                    if (distY < 1.6 && distXZ < interactRadius)
                    {
                        // Directional drag: When player moves right (+vel.x), grass bends LEFT (-X) trailing behind feet.
                        // When player moves left (-vel.x), grass bends RIGHT (+X).
                        float smoothDirX = -_PlayerMovement.w; 
                        float speed      = _PlayerMovement.z;
                        float moveIntensity = saturate(speed / 2.5);

                        // Tight natural body & foot interaction boundaries
                        float bodyRadius = 0.55;
                        float footRadius = 0.30;

                        // Spatial smooth falloff
                        float prox = saturate(1.0 - (distXZ / interactRadius));
                        float smoothProx = smoothstep(0.0, 1.0, prox);

                        // 1. Directional trailing drag is dominant when traversing
                        float horizDrag = smoothDirX * moveIntensity * dragWeight * 1.2;

                        // 2. Subtle soft parting only when directly in player's body silhouette (prevents wide circular bald rings)
                        float bodyProx = saturate(1.0 - (distXZ / bodyRadius));
                        float softPartX = sign(toBlade.x) * bodyProx * partingWeight * 0.25;
                        float splitSignZ = (toBlade.z >= 0.0) ? 1.0 : -1.0;
                        float softPartZ = splitSignZ * bodyProx * partingWeight * 0.30;

                        // 3. Underfoot gentle downward compression directly under soles
                        float trample = saturate(1.0 - (distXZ / footRadius));
                        float flattenDown = trample * trample * 0.20 * trampleWeight;

                        // 4. Subtle resting idle contact directly beneath feet
                        float idleOffset = (1.0 - moveIntensity) * idleDisp * 0.12 * sign(toBlade.x) * bodyProx;

                        float pushX = (horizDrag + softPartX + idleOffset) * smoothProx * springBend;
                        float pushZ = softPartZ * smoothProx * springBend;

                        pushWorld = float3(pushX, -flattenDown, pushZ);
                    }

                    // B. MOVEMENT WAKE & DAMPED HARMONIC SPRING RECOVERY
                    // Check trailing footsteps behind player for elastic rebound
                    if (_PlayerTrail0.w < 0.98)
                    {
                        float3 toTrail = rootWS - _PlayerTrail0.xyz;
                        float distTrail = length(float2(toTrail.x, toTrail.z));
                        float trailRadius = interactRadius * 0.85;

                        if (distTrail < trailRadius)
                        {
                            float trailProx = saturate(1.0 - (distTrail / trailRadius));
                            float age = _PlayerTrail0.w; // 0 (fresh) .. 1 (settled)

                            // Damped harmonic oscillation: e^(-damping * t) * cos(freq * t)
                            float springOsc = exp(-age * springDamp) * cos(age * springFreq);

                            float trailDragX = -sign(_PlayerMovement.x);
                            float trailPushX = trailDragX * trailProx * springOsc * springBend * 0.45;

                            pushWorld.x += trailPushX;
                        }
                    }

                    // Apply total displacement weighted by layer depth and blade bend height
                    displaced += TransformWorldToObjectDir(pushWorld * (layerWeight * bend));
                }

                output.positionCS  = TransformObjectToHClip(displaced);
                output.bladeHeight = h;
                output.normalWS    = TransformObjectToWorldNormal(input.normalOS);
                output.vertexColor = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float h = input.bladeHeight;

                half3 col;
                if (_SingleColorMode > 0.5)
                {
                    // Back layer single silhouette color
                    col = _SingleColor.rgb * (0.85 + 0.15 * h);
                }
                else
                {
                    // Three-stop gradient: root → mid → tip
                    col = lerp(_RootColor.rgb, _MidColor.rgb, saturate(h * 1.6));
                    col = lerp(col,            _TipColor.rgb, saturate((h - 0.5) * 2.0));
                }

                // Modulate by baked vertex color if valid
                if (any(input.vertexColor.rgb > 0.02))
                {
                    col = lerp(col, input.vertexColor.rgb, 0.75);
                }

                // Subtle atmospheric lighting with safe fallback
                half3 nWS = input.normalWS;
                half3 safeNormal = (dot(nWS, nWS) > 0.001) ? normalize(nWS) : half3(0.0, 1.0, 0.0);
                float NdotL = saturate(dot(safeNormal, normalize(float3(0.3, 1.0, 0.3))));
                col *= 0.75 + 0.25 * NdotL;

                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
