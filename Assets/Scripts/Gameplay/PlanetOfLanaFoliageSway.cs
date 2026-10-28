using UnityEngine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Chuyển động đung đưa tán lá và ngọn cỏ phong cách Planet of Lana.
    /// - 100% Bảo toàn tỉ lệ hình học (Asset Integrity): KHÔNG co giãn, KHÔNG làm biến dạng texture 2D.
    /// - Rotation đa tần số quanh gốc (Pivot Rotation) tạo cảm giác đung đưa hữu cơ theo gió.
    /// - Tương tác nhân vật đàn hồi (Interactive Tilt): Cỏ nghiêng tự nhiên khi An/Minh đi qua rồi hồi vị êm ái.
    /// - Hoạt động an toàn trong Play Mode, KHÔNG khóa hay ghi đè Transform trong Edit Mode.
    /// </summary>
    public class PlanetOfLanaFoliageSway : MonoBehaviour
    {
        [Header("--- Wind Sway (Góc lắc gió) ---")]
        [Tooltip("Góc lắc xoay tối đa do gió (độ)")]
        [SerializeField] private float maxRotationAngle = 3.0f;

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

        [Header("--- Pivot Anchoring (Neo gốc tại chân vật thể) ---")]
        [Tooltip("Tự động neo gốc ở chân sprite để khi xoay chân cây/ngọn cỏ bám chặt mặt đất")]
        [SerializeField] private bool anchorBottomPivot = true;

        [Header("--- Character Interaction (Nghiêng đàn hồi khi đi qua) ---")]
        [Tooltip("Bật tương tác khi nhân vật đi qua (thích hợp cho cụm cỏ, hoa dại, bụi cây thấp)")]
        [SerializeField] private bool enableCharacterInteraction = false;

        [Tooltip("Bán kính nhận diện nhân vật (mét)")]
        [SerializeField] private float interactionRadius = 0.85f;

        [Tooltip("Góc nghiêng tối đa khi nhân vật chạm vào (độ)")]
        [SerializeField] private float maxPushAngle = 14f;

        [Tooltip("Thời gian đáp ứng khi bị đẩy")]
        [SerializeField] private float pushSmoothTime = 0.08f;

        [Tooltip("Thời gian hồi vị đàn hồi sau khi nhân vật đi qua")]
        [SerializeField] private float reboundSmoothTime = 0.22f;

        // Lưu trữ transform ban đầu khi bắt đầu Play Mode
        private Vector3 initialLocalPos;
        private Quaternion initialLocalRot;
        private Vector3 initialLocalScale;
        private Vector3 baseAnchorLocalPos;
        private float spriteHalfHeight = 0f;
        private float computedPhase;
        private bool isInitialized = false;

        // Trạng thái tương tác nhân vật
        private float currentPushAngle = 0f;
        private float pushAngleVelocity = 0f;

        // Cache tham chiếu nhân vật dùng chung toàn bộ cây cỏ để tối ưu hiệu năng
        private static Transform s_CachedPlayer = null;
        private static Vector3 s_LastPlayerPos = Vector3.zero;
        private static float s_PlayerVelX = 0f;
        private static float s_LastPlayerSampleTime = 0f;

        public bool EnableCharacterInteraction
        {
            get => enableCharacterInteraction;
            set => enableCharacterInteraction = value;
        }

        public float MaxRotationAngle
        {
            get => maxRotationAngle;
            set => maxRotationAngle = value;
        }

        public float MaxPushAngle
        {
            get => maxPushAngle;
            set => maxPushAngle = value;
        }

        public float InteractionRadius
        {
            get => interactionRadius;
            set => interactionRadius = value;
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                InitializeTransform();
            }
        }

        private void Start()
        {
            if (Application.isPlaying)
            {
                InitializeTransform();
            }
        }

        public void InitializeTransform()
        {
            initialLocalPos = transform.localPosition;
            initialLocalRot = transform.localRotation;
            initialLocalScale = transform.localScale;

            // Tính toán tâm xoay tại đáy Sprite để gốc cây/cỏ luôn cắm chặt vào đất
            spriteHalfHeight = 0f;
            if (anchorBottomPivot)
            {
                var sr = GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                {
                    spriteHalfHeight = sr.bounds.extents.y;
                }
            }

            baseAnchorLocalPos = initialLocalPos - new Vector3(0f, spriteHalfHeight, 0f);
            currentPushAngle = 0f;
            pushAngleVelocity = 0f;
            isInitialized = true;
            UpdatePhase();
        }

        public void UpdatePhase()
        {
            if (useWorldPositionOffset)
            {
                computedPhase = (transform.position.x * 0.85f) + (transform.position.y * 0.4f) + manualPhaseOffset;
            }
            else
            {
                computedPhase = manualPhaseOffset;
            }
        }

        private void Update()
        {
            // BẢO VỆ CHẾ ĐỘ EDIT: Tuyệt đối không can thiệp Transform khi đang Edit trong Scene!
            if (!Application.isPlaying) return;

            if (!isInitialized)
            {
                InitializeTransform();
            }

            float dt = Time.deltaTime;
            if (dt <= 0.0001f) return;

            // 1. Tổ hợp sóng gió tự nhiên (Sin + Cos đa hài)
            float t = Time.time;
            float windTime = (t * swaySpeed) + computedPhase;
            float wave1 = Mathf.Sin(windTime);
            float wave2 = Mathf.Cos(windTime * 0.63f + 1.2f) * harmonicNoise;
            float totalWave = (wave1 + wave2) / (1f + harmonicNoise);
            float windAngle = totalWave * maxRotationAngle;

            // 2. Tương tác va quẹt với nhân vật (Interactive Tilt)
            float targetPushAngle = 0f;
            if (enableCharacterInteraction)
            {
                Transform player = GetPlayerTransform();
                if (player != null)
                {
                    Vector3 myWorldPos = transform.position;
                    Vector3 playerWorldPos = player.position;

                    float dx = myWorldPos.x - playerWorldPos.x;
                    float dy = Mathf.Abs(myWorldPos.y - playerWorldPos.y);

                    if (dy < 1.6f && Mathf.Abs(dx) < interactionRadius)
                    {
                        float proximity = 1f - (Mathf.Abs(dx) / interactionRadius);
                        float smoothProx = proximity * proximity; // Rơi mượt phi tuyến

                        // Xác định hướng đẩy nghiêng: Nếu nhân vật di chuyển thì ngả theo chiều di chuyển
                        float moveDir = (Mathf.Abs(s_PlayerVelX) > 0.15f) ? Mathf.Sign(s_PlayerVelX) : Mathf.Sign(dx);
                        if (Mathf.Abs(moveDir) < 0.01f) moveDir = 1f;

                        // Góc đẩy ngược dấu để ngả theo chiều nhân vật lướt qua
                        targetPushAngle = -moveDir * maxPushAngle * smoothProx;
                    }
                }

                // Chuyển đổi mượt giữa trạng thái bị đẩy và hồi vị đàn hồi
                float smoothTime = (Mathf.Abs(targetPushAngle) > Mathf.Abs(currentPushAngle)) ? pushSmoothTime : reboundSmoothTime;
                currentPushAngle = Mathf.SmoothDamp(currentPushAngle, targetPushAngle, ref pushAngleVelocity, smoothTime, 180f, dt);
            }

            // 3. Tổng hợp góc nghiêng (Gió + Tương tác)
            float finalAngle = windAngle + currentPushAngle;

            // 4. Áp dụng góc xoay quanh gốc: BẢO TOÀN 100% SCALE GỐC (KHÔNG CO GIÃN HÌNH HỌC)
            transform.localRotation = initialLocalRot * Quaternion.Euler(0f, 0f, finalAngle);

            if (anchorBottomPivot && spriteHalfHeight > 0.001f)
            {
                Vector3 rotatedOffset = Quaternion.Euler(0f, 0f, finalAngle) * new Vector3(0f, spriteHalfHeight, 0f);
                transform.localPosition = baseAnchorLocalPos + rotatedOffset;
            }

            // Đảm bảo tuyệt đối không bị co giãn vật thể
            transform.localScale = initialLocalScale;
        }

        private static Transform GetPlayerTransform()
        {
            if (s_CachedPlayer != null && s_CachedPlayer.gameObject.activeInHierarchy)
            {
                UpdatePlayerVelocity();
                return s_CachedPlayer;
            }

            var p = GameObject.FindWithTag("Player");
            if (p != null)
            {
                s_CachedPlayer = p.transform;
                s_LastPlayerPos = s_CachedPlayer.position;
                return s_CachedPlayer;
            }

            var pObj = GameObject.Find("PF_Player_Minh") ?? GameObject.Find("Character_An") ?? GameObject.Find("Player");
            if (pObj != null)
            {
                s_CachedPlayer = pObj.transform;
                s_LastPlayerPos = s_CachedPlayer.position;
                return s_CachedPlayer;
            }

            return null;
        }

        private static void UpdatePlayerVelocity()
        {
            float now = Time.time;
            if (now - s_LastPlayerSampleTime < 0.02f) return;

            float dt = now - s_LastPlayerSampleTime;
            s_LastPlayerSampleTime = now;

            if (s_CachedPlayer != null && dt > 0.0001f)
            {
                Vector3 currentPos = s_CachedPlayer.position;
                float rawVelX = (currentPos.x - s_LastPlayerPos.x) / dt;
                s_PlayerVelX = Mathf.Lerp(s_PlayerVelX, rawVelX, dt * 15f);
                s_LastPlayerPos = currentPos;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (isInitialized && Application.isPlaying)
            {
                UpdatePhase();
            }
        }
#endif
    }
}
