Shader "UI/WaterWaveRipple"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Base Glass Color", Color) = (0.98, 0.96, 0.93, 0.94)
        _WaveColor ("Wave Crest Color", Color) = (1.0, 0.86, 0.58, 0.95)
        _RippleColor ("Ripple Glow Color", Color) = (1.0, 0.98, 0.88, 1.0)
        
        _HoverBlend ("Hover Blend", Range(0, 1)) = 0
        _RippleProgress ("Ripple Progress", Range(0, 1)) = 0
        _RippleCenter ("Ripple Center", Vector) = (0.5, 0.5, 0, 0)
        
        _WaveSpeed ("Wave Speed", Float) = 3.6
        _WaveFrequency ("Wave Frequency", Float) = 14.0
        _WaveAmplitude ("Wave Amplitude", Float) = 0.40
        
        // UI Mask / Stencil Support
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _WaveColor;
            fixed4 _RippleColor;
            float4 _ClipRect;
            float _HoverBlend;
            float _RippleProgress;
            float4 _RippleCenter;
            float _WaveSpeed;
            float _WaveFrequency;
            float _WaveAmplitude;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                if (_HoverBlend <= 0.001)
                {
                    discard;
                    return fixed4(0, 0, 0, 0);
                }

                float2 uv = IN.texcoord;
                float t = _Time.y * _WaveSpeed;

                // 1. Organic Flowing River Wave
                float wave1 = sin(uv.x * _WaveFrequency + uv.y * 3.5 - t * 2.5);
                float wave2 = cos(uv.x * (_WaveFrequency * 0.65) - uv.y * 5.0 + t * 1.8);
                float waveNorm = (wave1 + wave2) * 0.25 + 0.5;

                // 2. Expanding Concentric Circular Ripple from Pointer
                float2 rippleVector = uv - _RippleCenter.xy;
                rippleVector.x *= 7.6; // Correct for 365x48 aspect ratio
                float dist = length(rippleVector);

                // Multiple radiating ripples (rings)
                float ringPhase = dist * 10.0 - _RippleProgress * 22.0;
                float ringPattern = sin(ringPhase);
                float wavefront = saturate(1.0 - abs(dist - _RippleProgress * 2.2) * 4.0);
                float rippleStrength = saturate(ringPattern * wavefront * (1.0 - _RippleProgress * 0.75));

                // 3. Soft rounded frame mask
                float edgeX = smoothstep(0.0, 0.025, uv.x) * smoothstep(1.0, 0.975, uv.x);
                float edgeY = smoothstep(0.0, 0.10, uv.y) * smoothstep(1.0, 0.90, uv.y);
                float frameMask = edgeX * edgeY;

                // Subtle glowing meniscus rim
                float rim = saturate((1.0 - frameMask) * smoothstep(0.005, 0.02, min(uv.x * 2.0, min((1.0 - uv.x) * 2.0, min(uv.y, 1.0 - uv.y)))));

                // 4. Color Compositing
                fixed4 col = _Color;
                // Golden river shimmer along undulating wave crests
                col = lerp(col, _WaveColor, waveNorm * _WaveAmplitude);
                // Radiating ripple shockwaves from cursor
                col = lerp(col, _RippleColor, rippleStrength * 0.95);
                // Rim accent
                col = lerp(col, _RippleColor, rim * 0.65);

                // Apply opacity and frame softening
                col.a *= _HoverBlend * frameMask;

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
                #endif

                return col;
            }
            ENDCG
        }
    }
}
