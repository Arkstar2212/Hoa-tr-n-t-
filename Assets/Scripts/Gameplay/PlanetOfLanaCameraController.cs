using System;
using UnityEngine;
using Unity.Cinemachine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Planet of Lana Style 2.5D Cinematic Camera Controller.
    /// Mang lại trải nghiệm camera êm ái, thư giãn và đậm chất điện ảnh:
    /// 1. Dynamic Directional Lead (Lookahead): Luôn mở rộng không gian phía trước hướng nhìn (+2.4m khi nhìn phải, -2.4m khi nhìn trái) với độ trôi mềm mại.
    /// 2. Ground-Anchored Vertical Tracking (Triệt tiêu rung giật khi nhảy): Khóa cao độ ngắm theo mặt đất khi nhảy, không bị giật camera lên xuống theo từng bước nhảy.
    /// 3. Speed-Responsive Breathing (Tự động nới rộng góc nhìn khi chạy): Tự động lùi xa (Distance 16m, FOV 39.5) khi chạy nhanh qua đồng cỏ, và thu gần (Distance 13m) khi đi chậm/ngồi khom.
    /// 4. Heavy Cinematic Damping: Độ hãm mềm mại (Damping Y 1.8s, Damping X 0.85s, DeadZone Y 0.28) tạo cảm giác ống kính điện ảnh trôi tự nhiên.
    /// </summary>
    [RequireComponent(typeof(CinemachineCamera))]
    [RequireComponent(typeof(CinemachinePositionComposer))]
    public class PlanetOfLanaCameraController : MonoBehaviour
    {
        [Header("Target Tracking")]
        [SerializeField] private PlayerController player;
        [SerializeField] private Transform trackingTarget;

        [Header("1. Directional Lookahead (Mở rộng tầm nhìn trước mặt)")]
        [Tooltip("Khoảng cách mở rộng tầm nhìn về phía trước nhân vật")]
        [SerializeField] private float horizontalLeadDistance = 2.4f;
        [Tooltip("Thời gian chuyển hướng tầm nhìn (càng lớn càng trôi êm ái, khuyến nghị 0.75s - 1.0s)")]
        [SerializeField] private float leadSmoothTime = 0.85f;
        [Tooltip("Tốc độ tối đa khi đảo chiều nhìn")]
        [SerializeField] private float maxLeadSpeed = 4.5f;

        [Header("2. Vertical Stability (Triệt tiêu xóc nảy khi nhảy)")]
        [Tooltip("Cao độ ngắm mặc định so với gốc nhân vật")]
        [SerializeField] private float defaultOffsetY = 1.95f;
        [Tooltip("Tỷ lệ triệt tiêu độ nảy camera khi nhảy (0.85 - 1.0: camera gần như đứng yên trên mặt đất khi nhảy)")]
        [Range(0f, 1f)]
        [SerializeField] private float jumpSuppressionFactor = 0.88f;
        [Tooltip("Thời gian làm mượt cao độ khi tiếp đất ở bậc cao/thấp mới")]
        [SerializeField] private float verticalElevationSmoothTime = 0.60f;

        [Header("3. Speed-Responsive Dynamic Zoom (Ống kính thở tự nhiên)")]
        [Tooltip("Khoảng cách camera khi đứng yên / đi bộ")]
        [SerializeField] private float idleCameraDistance = 13.2f;
        [Tooltip("Khoảng cách camera khi chạy nước rút")]
        [SerializeField] private float sprintCameraDistance = 16.0f;
        [Tooltip("Khoảng cách camera khi ngồi khom / bò")]
        [SerializeField] private float crouchCameraDistance = 12.0f;
        [Tooltip("Thời gian chuyển đổi khoảng cách camera")]
        [SerializeField] private float zoomSmoothTime = 1.5f;

        [Header("Field of View")]
        [SerializeField] private float idleFov = 36.5f;
        [SerializeField] private float sprintFov = 39.5f;
        [SerializeField] private float crouchFov = 34.5f;

        [Header("4. Cinematic Damping & DeadZone Preset")]
        [SerializeField] private Vector3 cinemachineDamping = new Vector3(0.85f, 1.85f, 0.0f);
        [SerializeField] private Vector2 cinemachineDeadZone = new Vector2(0.12f, 0.28f);

        // Internal State
        private CinemachineCamera virtualCamera;
        private CinemachinePositionComposer positionComposer;
        private CharacterController charController;

        private float currentLeadX = 0f;
        private float leadVelocityX = 0f;

        private float currentTargetOffsetY = 1.95f;
        private float offsetYVelocity = 0f;

        private float smoothedGroundY = 0f;
        private float groundYVelocity = 0f;

        private float currentDistance = 13.5f;
        private float distanceVelocity = 0f;

        private float currentFov = 38f;
        private float fovVelocity = 0f;

        private void Awake()
        {
            virtualCamera = GetComponent<CinemachineCamera>();
            positionComposer = GetComponent<CinemachinePositionComposer>();
            FindPlayerReferences();
            ApplyCinemachinePreset();
        }

        private void Start()
        {
            FindPlayerReferences();
            if (player != null)
            {
                smoothedGroundY = player.transform.position.y;
                currentLeadX = (player.FacingDirection >= 0 ? 1f : -1f) * horizontalLeadDistance;
            }
            ApplyCinemachinePreset();
        }

        public void ApplyCinemachinePreset()
        {
            if (positionComposer != null)
            {
                positionComposer.Damping = cinemachineDamping;
                positionComposer.Composition.DeadZone.Size = cinemachineDeadZone;
                positionComposer.Lookahead.IgnoreY = true;
                positionComposer.Lookahead.Time = 0f; // Chúng ta tự tính toán lead offset mượt mà và thông minh hơn Cinemachine mặc định
            }
        }

        private void LateUpdate()
        {
            if (virtualCamera == null || positionComposer == null) return;
            if (player == null)
            {
                FindPlayerReferences();
                if (player == null) return;
            }

            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            // 1. Tính toán Directional Lead (Lookahead theo hướng mặt và vận tốc)
            float facingDir = player.FacingDirection;
            if (Mathf.Abs(player.HorizontalInput) > 0.1f)
            {
                facingDir = Mathf.Sign(player.HorizontalInput);
            }
            else if (Mathf.Abs(player.Velocity.x) > 0.2f)
            {
                facingDir = Mathf.Sign(player.Velocity.x);
            }

            float targetLeadX = facingDir * horizontalLeadDistance;
            currentLeadX = Mathf.SmoothDamp(currentLeadX, targetLeadX, ref leadVelocityX, leadSmoothTime, maxLeadSpeed, dt);

            // 2. Tính toán Ground-Anchored Vertical Tracking (Triệt tiêu rung giật khi nhảy)
            bool isGrounded = (charController != null && charController.isGrounded) || (player.CurrentState == PlayerState.Grounded);
            float playerY = player.transform.position.y;

            if (isGrounded)
            {
                smoothedGroundY = Mathf.SmoothDamp(smoothedGroundY, playerY, ref groundYVelocity, verticalElevationSmoothTime * 0.5f, 15f, dt);
            }
            else
            {
                // Khi đang ở trên không, smoothedGroundY giữ ổn định hoặc trôi nhẹ nếu rơi sâu
                if (playerY < smoothedGroundY - 2.5f)
                {
                    smoothedGroundY = Mathf.SmoothDamp(smoothedGroundY, playerY, ref groundYVelocity, verticalElevationSmoothTime, 10f, dt);
                }
            }

            // Tính toán khoảng cách bật nhảy trên không
            float airborneOffset = Mathf.Max(0f, playerY - smoothedGroundY);
            float targetOffsetY = defaultOffsetY - (airborneOffset * jumpSuppressionFactor);
            currentTargetOffsetY = Mathf.SmoothDamp(currentTargetOffsetY, targetOffsetY, ref offsetYVelocity, 0.15f, 20f, dt);

            // Cập nhật TargetOffset lên Cinemachine PositionComposer
            positionComposer.TargetOffset = new Vector3(currentLeadX, currentTargetOffsetY, 0f);

            // 3. Tính toán Speed-Responsive Dynamic Zoom (Ống kính thở tự nhiên)
            float speed = Mathf.Abs(player.Velocity.x);
            float targetDist = idleCameraDistance;
            float targetFovVal = idleFov;

            if (player.IsCrouching || player.IsCrawling)
            {
                targetDist = crouchCameraDistance;
                targetFovVal = crouchFov;
            }
            else if (player.IsSprinting || speed > 3.2f)
            {
                targetDist = sprintCameraDistance;
                targetFovVal = sprintFov;
            }
            else if (speed > 1.2f)
            {
                // Đi bộ nhanh
                float t = Mathf.InverseLerp(1.2f, 3.2f, speed);
                targetDist = Mathf.Lerp(idleCameraDistance, sprintCameraDistance * 0.85f, t);
                targetFovVal = Mathf.Lerp(idleFov, sprintFov * 0.9f, t);
            }

            currentDistance = Mathf.SmoothDamp(positionComposer.CameraDistance, targetDist, ref distanceVelocity, zoomSmoothTime, 8f, dt);
            positionComposer.CameraDistance = currentDistance;

            var lens = virtualCamera.Lens;
            currentFov = Mathf.SmoothDamp(lens.FieldOfView, targetFovVal, ref fovVelocity, zoomSmoothTime, 8f, dt);
            lens.FieldOfView = currentFov;
            virtualCamera.Lens = lens;
        }

        private void FindPlayerReferences()
        {
            if (player == null)
            {
                player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            }

            if (player != null)
            {
                charController = player.GetComponent<CharacterController>();
                trackingTarget = player.transform;

                if (virtualCamera != null && virtualCamera.Target.TrackingTarget == null)
                {
                    virtualCamera.Target.TrackingTarget = trackingTarget;
                }
            }
        }

        private void OnValidate()
        {
            if (virtualCamera == null) virtualCamera = GetComponent<CinemachineCamera>();
            if (positionComposer == null) positionComposer = GetComponent<CinemachinePositionComposer>();
            ApplyCinemachinePreset();
        }
    }
}
