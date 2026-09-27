// PlanetOfLanaTree.shader
// Vertex Shader chuyên dụng cho cây 2D / 2.5D phong cách Planet of Lana.
// Chuyển động 3 tầng: Gốc (khóa cứng) -> Thân (uốn đòn bẩy + nhún đỉnh) -> Tán (rung lá + phồng xẹp).
// Hoàn toàn chạy trên GPU (Vertex Shader), 100% không cần C# script.

Shader "HoaVaMau/PlanetOfLanaTree"
{
    Properties
    {
        [Header(Texture and Tint)]
        _MainTex            ("Sprite Texture", 2D) = "white" {}
        _Color              ("Tint Color", Color) = (1, 1, 1, 1)
        _Cutoff             ("Alpha Cutoff (0 = Alpha Blend)", Range(0.0, 1.0)) = 0.01

        [Header(Height Bands)]
        _RootHeight         ("Root Deadzone Height (Goc cay khoa cung)", Range(0.0, 0.4)) = 0.12
        _CanopyStart        ("Canopy Start Height (Diem bat dau tan la)", Range(0.2, 0.95)) = 0.50

        [Header(Trunk Sway)]
        _TrunkSpeed         ("Trunk Sway Speed (Toc do lac than)", Range(0.1, 4.0)) = 1.2
        _TrunkAmp           ("Trunk Sway Amplitude (Bien do lac than)", Range(0.0, 0.8)) = 0.18
        _TrunkBendPower     ("Trunk Bend Curve (1=Thang, 2=Uon cong)", Range(1.0, 3.5)) = 2.0
        _TrunkArcDrop       ("Trunk Arc Drop (Nhun dinh bao toan chieu dai)", Range(0.0, 0.4)) = 0.12

        [Header(Canopy Flutter)]
        _CanopySpeed        ("Canopy Flutter Speed (Toc do rung la)", Range(0.5, 8.0)) = 3.2
        _CanopyAmp          ("Canopy Flutter Amplitude (Bien do rung la)", Range(0.0, 0.3)) = 0.08
        _CanopyBillow       ("Canopy Billow Y (Do phong xep cum la)", Range(0.0, 0.2)) = 0.04

        [Header(Gust and Phase)]
        _GustStrength       ("Wind Gust Strength (Luc gio giat)", Range(0.0, 0.5)) = 0.15
        _GustSpeed          ("Wind Gust Frequency (Tan suat gio giat)", Range(0.1, 1.5)) = 0.35
        _PhaseOffset        ("Layer Phase Offset (Lech pha giua cac lop la)", Float) = 0.0

        [Header(Wind Direction and Global Sync)]
        _WindDirection      ("Manual Wind Direction (-1=Left, 1=Right)", Range(-1.0, 1.0)) = 1.0
        [Toggle] _UseGlobalWind ("Sync with Global WindManager (0=Tat, 1=Bat)", Float) = 1.0
    }

    SubShader
    {
        Tags
        {
            "Queue"             = "Transparent"
            "IgnoreProjector"   = "True"
            "RenderType"        = "Transparent"
            "PreviewType"       = "Plane"
            "CanUseSpriteAtlas" = "True"
            "RenderPipeline"    = "UniversalPipeline"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "PlanetOfLanaTreeForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float  _Cutoff;
                float  _RootHeight;
                float  _CanopyStart;
                float  _TrunkSpeed;
                float  _TrunkAmp;
                float  _TrunkBendPower;
                float  _TrunkArcDrop;
                float  _CanopySpeed;
                float  _CanopyAmp;
                float  _CanopyBillow;
                float  _GustStrength;
                float  _GustSpeed;
                float  _PhaseOffset;
                float  _WindDirection;
                float  _UseGlobalWind;
            CBUFFER_END

            // Globals from WindManager (tự động nhận từ GPU nếu có)
            float4 _GlobalWind;

            Varyings vert(Attributes input)
            {
                Varyings output;

                // 1. Chiều cao chuẩn hóa theo UV (0.0 ở đáy -> 1.0 ở đỉnh)
                float h = saturate(input.uv.y);

                // 2. TẦNG 1: GỐC CÂY (Root Deadzone)
                // Dưới ngưỡng _RootHeight hoàn toàn = 0, trên ngưỡng này tăng dần mềm mại
                float rootMask = smoothstep(_RootHeight, _RootHeight + 0.1, h);

                // 3. TẦNG 2: THÂN CÂY (Trunk Bending)
                // Uốn cong phi tuyến tính theo hàm lũy thừa h^_TrunkBendPower
                float trunkBendWeight = pow(h, _TrunkBendPower) * rootMask;

                // Tạo hạt giống vị trí thế giới để nhiều cây cạnh nhau không lắc đồng bộ như robot
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                float spatialSeed = worldPos.x * 0.25 + worldPos.z * 0.18;
                float t = _Time.y;

                // Tính toán hướng gió: Ưu tiên nhận từ WindManager nếu bật _UseGlobalWind
                float windDir = _WindDirection;
                float windPowerMult = 1.0;

                if (_UseGlobalWind > 0.5 && length(_GlobalWind.xyz) > 0.01)
                {
                    windDir = _GlobalWind.x >= 0.0 ? 1.0 : -1.0;
                    windPowerMult = clamp(length(_GlobalWind.xyz) + abs(_GlobalWind.w) * 0.5, 0.4, 2.5);
                }

                // Dao động chính của thân: sóng sin chậm + sóng hài phụ
                float trunkWave1 = sin(t * _TrunkSpeed + spatialSeed + _PhaseOffset);
                float trunkWave2 = cos(t * (_TrunkSpeed * 0.65) + spatialSeed * 1.3) * 0.35;
                float trunkSwayX = (trunkWave1 + trunkWave2) * _TrunkAmp * trunkBendWeight;

                // Gió giật bất định (Wind Gusting)
                float gustRaw = sin(t * _GustSpeed + spatialSeed * 0.4);
                float gust = saturate(gustRaw * 1.8 - 0.8);
                gust = gust * gust * _GustStrength * trunkBendWeight;

                // 4. TẦNG 3: TÁN CÂY (Canopy Flutter & Billow)
                // Kích hoạt từ độ cao _CanopyStart trở lên
                float canopyMask = smoothstep(_CanopyStart, 1.0, h);
                float flutterTime = t * _CanopySpeed + spatialSeed * 1.5 + _PhaseOffset;
                
                // Rung rinh lá: kết hợp 3 sóng sin tần số cao
                float leafWave1 = sin(flutterTime);
                float leafWave2 = sin(flutterTime * 1.73 + 1.1) * 0.45;
                float leafWave3 = cos(flutterTime * 2.87 + worldPos.y * 0.5) * 0.25;
                float canopyFlutterX = (leafWave1 + leafWave2 + leafWave3) * _CanopyAmp * canopyMask;

                // Độ phồng xẹp theo trục Y (billowing / breathing) của tán lá
                float canopyBillowY = cos(flutterTime * 1.25 + 0.7) * _CanopyBillow * canopyMask;

                // 5. TỔNG HỢP CHUYỂN ĐỘNG (Nhân với hướng gió và lực gió)
                float totalDisplaceX = (trunkSwayX + gust + canopyFlutterX) * windDir * windPowerMult;

                // Bảo toàn chiều dài cung (Arc Drop): khi nghiêng sang X thì ngọn hơi sụt xuống theo Y
                float arcDropY = -(totalDisplaceX * totalDisplaceX) * _TrunkArcDrop * h;

                // Áp dụng biến dạng đỉnh vào toạ độ Object Space
                float3 displacedOS = input.positionOS.xyz;
                displacedOS.x += totalDisplaceX;
                displacedOS.y += arcDropY + canopyBillowY;

                output.positionCS = TransformObjectToHClip(displacedOS);
                output.uv         = TRANSFORM_TEX(input.uv, _MainTex);
                output.color      = input.color * _Color;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;

                if (_Cutoff > 0.001)
                {
                    clip(col.a - _Cutoff);
                }

                return col;
            }
            ENDHLSL
        }
    }
    FallBack "Sprites/Default"
}
