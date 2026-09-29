using UnityEngine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Chuyển động đung đưa tán lá và cành cây phong cách Planet of Lana.
    /// Kết hợp: Rotation đa tần số + Micro-Position + Micro-Scale (breathing) + Spatial Phase Offset.
    /// Tự động chạy cả trong Edit Mode và Play Mode.
    /// </summary>
    [ExecuteAlways]
    [SelectionBase]
    public class PlanetOfLanaFoliageSway : MonoBehaviour
    {
        [Header("--- Rotation (Góc lắc chính) ---")]
        [Tooltip("Góc lắc xoay tối đa (độ)")]
        [SerializeField] private float maxRotationAngle = 3.5f;

        [Tooltip("Tốc độ đung đưa của gió")]
        [SerializeField] private float swaySpeed = 1.6f;

        [Header("--- Spatial Phase (Độ lệch pha gió) ---")]
        [Tooltip("Tự động tính độ trễ theo toạ độ thế giới (X/Y) để các tán và cây lệch pha tự nhiên")]
        [SerializeField] private bool useWorldPositionOffset = true;

        [Tooltip("Gia số lệch pha thủ công (nếu muốn chỉnh tay cho từng tán)")]
        [SerializeField] private float manualPhaseOffset = 0f;

        [Header("--- Wind Dynamics (Độ bất định của gió) ---")]
        [Range(0f, 1f)]
        [Tooltip("Cường độ sóng hài phụ tạo cảm giác gió tự nhiên bất quy tắc")]
        [SerializeField] private float harmonicNoise = 0.35f;

        [Header("--- Micro Billow / Breathing (Phập phồng phồng xẹp) ---")]
        [Tooltip("Biên độ co giãn rất nhẹ của tán lá (0.99 - 1.01) theo nhịp gió")]
        [SerializeField] private float scaleBreathing = 0.015f;

        [Tooltip("Độ dịch chuyển vị trí cực nhỏ theo nhịp lắc")]
        [SerializeField] private Vector2 positionJitter = new Vector2(0.015f, 0.008f);

        // Lưu trữ transform ban đầu
        private Vector3 initialLocalPos;
        private Quaternion initialLocalRot;
        private Vector3 initialLocalScale;
        private float computedPhase;
        private bool isInitialized = false;

        private void OnEnable()
        {
            InitializeTransform();
        }

        private void Start()
        {
            InitializeTransform();
        }

        private void InitializeTransform()
        {
            if (!isInitialized)
            {
                initialLocalPos = transform.localPosition;
                initialLocalRot = transform.localRotation;
                initialLocalScale = transform.localScale;
                isInitialized = true;
            }

            UpdatePhase();
        }

        public void UpdatePhase()
        {
            if (useWorldPositionOffset)
            {
                // Gió thổi từ trái sang phải: X tạo độ trễ sóng gió, Y tạo độ trễ độ cao
                computedPhase = (transform.position.x * 0.85f) + (transform.position.y * 0.4f) + manualPhaseOffset;
            }
            else
            {
                computedPhase = manualPhaseOffset;
            }
        }

        private void Update()
        {
            if (!isInitialized)
            {
                InitializeTransform();
            }

            // Lấy thời gian (hỗ trợ cả Editor Scene View khi không Play)
            float t;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                t = (float)UnityEditor.EditorApplication.timeSinceStartup;
            }
            else
            {
                t = Time.time;
            }
#else
            t = Time.time;
#endif

            float windTime = (t * swaySpeed) + computedPhase;

            // 1. Tổ hợp 2 sóng Sin & Cos có tần số không tỷ lệ nguyên để không bị lặp tuần hoàn robot
            float wave1 = Mathf.Sin(windTime);
            float wave2 = Mathf.Cos(windTime * 0.63f + 1.2f) * harmonicNoise;
            float totalWave = (wave1 + wave2) / (1f + harmonicNoise);

            // 2. Rotation (Điểm xoay tại Pivot của Object)
            float currentAngle = totalWave * maxRotationAngle;
            transform.localRotation = initialLocalRot * Quaternion.Euler(0f, 0f, currentAngle);

            // 3. Micro-displace (Position nhích nhẹ theo quán tính)
            if (positionJitter != Vector2.zero)
            {
                float posX = totalWave * positionJitter.x;
                float posY = -Mathf.Abs(totalWave) * positionJitter.y; // Khi lắc mạnh thì hơi sụt xuống
                transform.localPosition = initialLocalPos + new Vector3(posX, posY, 0f);
            }

            // 4. Micro-scale (Tán lá phập phồng như thở trong gió)
            if (scaleBreathing > 0f)
            {
                float scaleFactor = 1f + (Mathf.Sin(windTime * 1.35f) * scaleBreathing);
                transform.localScale = new Vector3(initialLocalScale.x * scaleFactor, initialLocalScale.y * (2f - scaleFactor), initialLocalScale.z);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (isInitialized && !Application.isPlaying)
            {
                UpdatePhase();
            }
        }
#endif
    }
}
