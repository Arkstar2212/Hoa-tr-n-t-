using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HoaVaMau
{
    /// <summary>
    /// Camera góc nhìn thứ 3 (Third-Person Orbit / Follow Camera) chuẩn mực cho game 3D phiêu lưu.
    /// Cho phép người chơi tự do xoay chuột 360 độ (Pitch/Yaw), cuộn chuột zoom xa gần,
    /// tự động tránh va chạm địa hình/vật cản (SphereCast collision avoidance)
    /// và khóa/mở chuột linh hoạt giống mọi game 3D hiện đại (Genshin Impact, Zelda BOTW, Witcher 3).
    /// </summary>
    public class ThirdPersonOrbitCamera : MonoBehaviour
    {
        [Header("Target Tracking")]
        [Tooltip("Mục tiêu theo dõi (Bé An)")]
        [SerializeField] private Transform target;

        [Tooltip("Điểm ngắm trên thân nhân vật (ngang ngực/đầu)")]
        [SerializeField] private Vector3 pivotOffset = new Vector3(0f, 1.30f, 0f);

        [Tooltip("Độ lệch sang phải vai nhân vật (Over-the-shoulder) tạo góc nhìn 3D điện ảnh")]
        [SerializeField] private float shoulderOffset = 0.25f;

        [Header("Distance & Zoom")]
        [Tooltip("Khoảng cách mặc định từ Camera tới nhân vật")]
        [SerializeField] private float defaultDistance = 3.8f;
        [SerializeField] private float minDistance = 1.2f;
        [SerializeField] private float maxDistance = 7.5f;
        [SerializeField] private float zoomSpeed = 2.5f;

        [Header("Rotation & Sensitivity")]
        [SerializeField] private float sensitivityX = 2.4f;
        [SerializeField] private float sensitivityY = 1.8f;
        [SerializeField] private float minPitch = -22.0f; // Nhìn hất lên từ chân
        [SerializeField] private float maxPitch = 72.0f;  // Nhìn từ trên cao xuống
        [SerializeField] private float smoothRotationTime = 0.04f;
        [SerializeField] private float smoothFollowTime = 0.06f;

        [Header("Collision Avoidance (Chống xuyên tường/đất)")]
        [SerializeField] private bool enableCollisionAvoidance = true;
        [SerializeField] private float collisionRadius = 0.22f;
        [SerializeField] private float collisionOffset = 0.15f;
        [SerializeField] private LayerMask obstacleLayers = ~0; // Mặc định quét mọi layer trừ Player

        [Header("Cursor Control")]
        [SerializeField] private bool lockCursorOnStart = true;
        [SerializeField] private KeyCode toggleCursorKey = KeyCode.Escape;

        // Nội bộ trạng thái
        private float yaw = 0f;
        private float pitch = 15.0f;
        private float currentYaw;
        private float currentPitch;
        private float yawVelocity;
        private float pitchVelocity;

        private float targetDistance;
        private float currentDistance;
        private float distanceVelocity;

        private Vector3 smoothFollowVelocity;
        private Vector3 currentPivotPosition;

        private Camera cam;

        public Transform Target
        {
            get => target;
            set => target = value;
        }

        public float Yaw => yaw;
        public float Pitch => pitch;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam != null)
            {
                cam.orthographic = false;
                cam.fieldOfView = 60.0f; // Góc nhìn 3D chuẩn
            }

            // Loại bỏ Layer của Player khỏi obstacleLayers để tránh va vào chính nhân vật
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer != -1)
            {
                obstacleLayers &= ~(1 << playerLayer);
            }
            int ignoreRaycast = LayerMask.NameToLayer("Ignore Raycast");
            if (ignoreRaycast != -1)
            {
                obstacleLayers &= ~(1 << ignoreRaycast);
            }
            int transparentFX = LayerMask.NameToLayer("TransparentFX");
            if (transparentFX != -1)
            {
                obstacleLayers &= ~(1 << transparentFX);
            }
        }

        private void Start()
        {
            if (target == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null) target = player.transform;
            }

            targetDistance = defaultDistance;
            currentDistance = defaultDistance;

            if (target != null)
            {
                currentPivotPosition = target.position + pivotOffset;
                // Khởi tạo góc nhìn hướng ra sau lưng nhân vật
                yaw = target.eulerAngles.y;
                pitch = 18.0f;
                currentYaw = yaw;
                currentPitch = pitch;

                // Căn chỉnh vị trí tức thì ngay frame đầu tiên (không lerp từ 0,0,0)
                Quaternion initRot = Quaternion.Euler(currentPitch, currentYaw, 0f);
                Vector3 initPivot = currentPivotPosition + initRot * Vector3.right * shoulderOffset;
                transform.position = initPivot + (initRot * Vector3.back) * defaultDistance;
                transform.rotation = initRot;
            }

            if (lockCursorOnStart)
            {
                SetCursorLock(true);
            }
        }

        private void Update()
        {
            // Bấm Escape hoặc click chuột để bật/tắt con trỏ
            if (Input.GetKeyDown(toggleCursorKey))
            {
                SetCursorLock(Cursor.lockState != CursorLockMode.Locked);
            }

            if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
            {
                SetCursorLock(true);
            }

            // Đọc Input xoay chuột (khi chuột đang khóa hoặc khi giữ chuột phải)
            bool canLook = (Cursor.lockState == CursorLockMode.Locked) || Input.GetMouseButton(1);
            if (canLook)
            {
                float mouseX = 0f;
                float mouseY = 0f;

#if ENABLE_INPUT_SYSTEM
                var mouse = Mouse.current;
                if (mouse != null)
                {
                    Vector2 delta = mouse.delta.ReadValue();
                    mouseX = delta.x * 0.1f * sensitivityX;
                    mouseY = delta.y * 0.1f * sensitivityY;
                }
#else
                mouseX = Input.GetAxis("Mouse X") * sensitivityX;
                mouseY = Input.GetAxis("Mouse Y") * sensitivityY;
#endif

                yaw += mouseX;
                pitch -= mouseY;
                pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            }

            // Cuộn chuột zoom cự ly camera
            float scroll = 0f;
#if ENABLE_INPUT_SYSTEM
            var m = Mouse.current;
            if (m != null) scroll = m.scroll.ReadValue().y * 0.001f;
#else
            scroll = Input.GetAxis("Mouse ScrollWheel");
#endif
            if (Mathf.Abs(scroll) > 0.001f)
            {
                targetDistance = Mathf.Clamp(targetDistance - scroll * zoomSpeed * 3.0f, minDistance, maxDistance);
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null) target = player.transform;
                else return;
            }

            // Điểm tựa (Pivot) bám theo nhân vật mượt mà
            Vector3 desiredPivot = target.position + pivotOffset;
            currentPivotPosition = Vector3.SmoothDamp(currentPivotPosition, desiredPivot, ref smoothFollowVelocity, smoothFollowTime);

            // Xoay mượt mà theo góc Yaw/Pitch
            currentYaw = Mathf.SmoothDampAngle(currentYaw, yaw, ref yawVelocity, smoothRotationTime);
            currentPitch = Mathf.SmoothDampAngle(currentPitch, pitch, ref pitchVelocity, smoothRotationTime);

            Quaternion cameraRotation = Quaternion.Euler(currentPitch, currentYaw, 0f);

            // Tính điểm lệch vai (Shoulder Offset)
            Vector3 rightOffset = cameraRotation * Vector3.right * shoulderOffset;
            Vector3 finalPivot = currentPivotPosition + rightOffset;

            // Tính hướng nhìn lùi về sau
            Vector3 backDirection = cameraRotation * Vector3.back;

            // Xử lý chống xuyên vật cản (Collision Avoidance)
            float finalDistance = targetDistance;
            if (enableCollisionAvoidance)
            {
                if (Physics.SphereCast(finalPivot, collisionRadius, backDirection, out RaycastHit hit, targetDistance, obstacleLayers, QueryTriggerInteraction.Ignore))
                {
                    // Tránh va vào collider của chính player
                    if (hit.transform != target && !hit.transform.IsChildOf(target))
                    {
                        finalDistance = Mathf.Clamp(hit.distance - collisionOffset, minDistance, targetDistance);
                    }
                }
            }

            currentDistance = Mathf.SmoothDamp(currentDistance, finalDistance, ref distanceVelocity, 0.05f);

            // Đặt vị trí và góc quay cho Camera
            Vector3 cameraPosition = finalPivot + backDirection * currentDistance;
            transform.position = cameraPosition;
            transform.rotation = cameraRotation;
        }

        public void SetCursorLock(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
