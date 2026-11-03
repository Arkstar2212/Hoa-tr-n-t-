#pragma warning disable 0414
using UnityEngine;
using HoaVaMau.Gameplay;
using HoaVaMau.Test3D;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HoaVaMau
{
    /// <summary>
    /// Camera góc nhìn thứ 3 (Third-Person Orbit / Follow Camera) chuẩn mực cho game 3D phiêu lưu.
    /// Cho phép người chơi tự do xoay chuột 360 độ (Pitch/Yaw), cuộn chuột zoom xa gần,
    /// tự động tránh va chạm địa hình/vật cản (SphereCast collision avoidance),
    /// và tự động nhường quyền con trỏ chuột mượt mà cho PauseMenuController ([Esc]).
    /// </summary>
    public class ThirdPersonOrbitCamera : MonoBehaviour
    {
        [Header("Target Tracking")]
        [Tooltip("Mục tiêu theo dõi (Bé An)")]
        [SerializeField] private Transform target;

        [Tooltip("Điểm ngắm trên thân nhân vật (ngang ngực/đầu)")]
        [SerializeField] private Vector3 pivotOffset = new Vector3(0f, 1.05f, 0f);

        [Tooltip("Độ lệch sang phải vai nhân vật (0 = căn giữa nhân vật hoàn hảo)")]
        [SerializeField] private float shoulderOffset = 0.0f;

        [Header("Distance & Zoom")]
        [Tooltip("Khoảng cách mặc định từ Camera tới nhân vật")]
        [SerializeField] private float defaultDistance = 4.0f;
        [SerializeField] private float minDistance = 1.4f;
        [SerializeField] private float maxDistance = 8.5f;
        [SerializeField] private float zoomSpeed = 2.5f;

        [Header("Rotation & Sensitivity")]
        [SerializeField] private float sensitivityX = 2.4f;
        [SerializeField] private float sensitivityY = 1.8f;
        [SerializeField] private float minPitch = -10.0f;
        [SerializeField] private float maxPitch = 62.0f;
        [SerializeField] private float smoothRotationTime = 0.04f;
        [SerializeField] private float smoothFollowTime = 0.05f;

        [Header("Collision Avoidance (Chống xuyên tường/đất)")]
        [SerializeField] private bool enableCollisionAvoidance = true;
        [SerializeField] private float collisionRadius = 0.18f;
        [SerializeField] private float collisionOffset = 0.15f;
        [SerializeField] private LayerMask obstacleLayers = ~0;

        [Header("Cursor Control")]
        [SerializeField] private bool lockCursorOnStart = true;
        [SerializeField] private KeyCode toggleCursorKey = KeyCode.Tab;

        private float yaw = 0f;
        private float pitch = 18.0f;
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

        private bool inDialogueFocus = false;
        private Vector3 dialogueCamTargetPos;
        private Quaternion dialogueCamTargetRot;
        private float dialogueFOV = 46f;
        private float normalFOV = 56f;

        public bool InDialogueFocus => inDialogueFocus;

        public void StartDialogueFocus(Vector3 cameraPos, Quaternion cameraRot, float fov = 46f)
        {
            inDialogueFocus = true;
            dialogueCamTargetPos = cameraPos;
            dialogueCamTargetRot = cameraRot;
            dialogueFOV = fov;
            SetCursorLock(false);
        }

        public void UpdateDialogueFocus(Vector3 cameraPos, Quaternion cameraRot)
        {
            dialogueCamTargetPos = cameraPos;
            dialogueCamTargetRot = cameraRot;
        }

        public void EndDialogueFocus()
        {
            inDialogueFocus = false;
            SnapToTarget();
            SetCursorLock(true);
        }

        private void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam != null)
            {
                cam.orthographic = false;
                cam.fieldOfView = 56.0f;
                cam.nearClipPlane = 0.12f;
                cam.depth = 10f;
            }
            if (!gameObject.CompareTag("MainCamera"))
            {
                gameObject.tag = "MainCamera";
            }

            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer != -1) obstacleLayers &= ~(1 << playerLayer);
            int ignoreRaycast = LayerMask.NameToLayer("Ignore Raycast");
            if (ignoreRaycast != -1) obstacleLayers &= ~(1 << ignoreRaycast);
            int transparentFX = LayerMask.NameToLayer("TransparentFX");
            if (transparentFX != -1) obstacleLayers &= ~(1 << transparentFX);

            DisableStraySceneCameras();
        }

        private void Start()
        {
            ResolveTarget();
            DisableStraySceneCameras();

            targetDistance = defaultDistance;
            currentDistance = defaultDistance;

            SnapToTarget();

            if (lockCursorOnStart && Application.isPlaying)
            {
                SetCursorLock(true);
            }
        }

        private void ResolveTarget()
        {
            if (target != null && target.gameObject.activeInHierarchy) return;

            var p3d = Object.FindAnyObjectByType<Player3DController>();
            if (p3d != null)
            {
                target = p3d.transform;
                return;
            }

            GameObject player = GameObject.Find("An_Player") ?? GameObject.FindWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }

        private void DisableStraySceneCameras()
        {
            var allCams = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (var c in allCams)
            {
                if (c == null || c == cam) continue;
                // Disable any embedded FBX/prefab Blender cameras (e.g., inside PF_Prop_CargoCrate_3D)
                if (c.targetTexture == null)
                {
                    c.enabled = false;
                    var listener = c.GetComponent<AudioListener>();
                    if (listener != null) listener.enabled = false;
                }
            }
        }

        /// <summary>
        /// Lập tức đặt Camera ngay phía sau nhân vật mà không bị trễ nội suy (dùng khi vừa vào Scene hoặc Respawn).
        /// </summary>
        public void SnapToTarget(float? overrideYaw = null)
        {
            ResolveTarget();
            if (target == null) return;

            if (pivotOffset.y < 0.7f) pivotOffset = new Vector3(0f, 1.05f, 0f);
            if (defaultDistance < 2.5f) defaultDistance = 4.0f;

            currentPivotPosition = target.position + pivotOffset;
            smoothFollowVelocity = Vector3.zero;
            yawVelocity = 0f;
            pitchVelocity = 0f;
            distanceVelocity = 0f;

            yaw = overrideYaw.HasValue ? overrideYaw.Value : target.eulerAngles.y;
            pitch = 18.0f;
            currentYaw = yaw;
            currentPitch = pitch;
            targetDistance = defaultDistance;
            currentDistance = defaultDistance;

            Quaternion initRot = Quaternion.Euler(currentPitch, currentYaw, 0f);
            Vector3 initPivot = currentPivotPosition + initRot * Vector3.right * shoulderOffset;
            transform.position = initPivot + (initRot * Vector3.back) * currentDistance;
            transform.rotation = initRot;
        }

        private void Update()
        {
            // Yield cursor cleanly whenever PauseMenuController is open or game is paused
            if ((PauseMenuController.Instance != null && PauseMenuController.Instance.IsPaused) || Time.timeScale <= 0.001f)
            {
                if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
                {
                    SetCursorLock(false);
                }
                return;
            }

            if (inDialogueFocus)
            {
                if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
                {
                    SetCursorLock(false);
                }
                return;
            }

            // Optional free-cursor toggle key (Tab / Alt; Escape is handled by PauseMenuController if present)
            bool togglePressed = false;
            bool leftClickPressed = false;
            bool rightHold = false;

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.tabKey.wasPressedThisFrame || kb.leftAltKey.wasPressedThisFrame) togglePressed = true;
                if (PauseMenuController.Instance == null && kb.escapeKey.wasPressedThisFrame) togglePressed = true;
            }
            var m = Mouse.current;
            if (m != null)
            {
                leftClickPressed = m.leftButton.wasPressedThisFrame;
                rightHold = m.rightButton.isPressed;
            }
#else
            KeyCode effectiveToggle = (PauseMenuController.Instance != null && toggleCursorKey == KeyCode.Escape) ? KeyCode.Tab : toggleCursorKey;
            if (Input.GetKeyDown(effectiveToggle)) togglePressed = true;
            leftClickPressed = Input.GetMouseButtonDown(0);
            rightHold = Input.GetMouseButton(1);
#endif

            if (togglePressed)
            {
                SetCursorLock(Cursor.lockState != CursorLockMode.Locked);
            }
            else if (leftClickPressed && Cursor.lockState != CursorLockMode.Locked)
            {
                SetCursorLock(true);
            }

            bool canLook = (Cursor.lockState == CursorLockMode.Locked) || rightHold;
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

            float scroll = 0f;
#if ENABLE_INPUT_SYSTEM
            var ms = Mouse.current;
            if (ms != null) scroll = ms.scroll.ReadValue().y * 0.001f;
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
            if (target == null || !target.gameObject.activeInHierarchy)
            {
                ResolveTarget();
                if (target == null) return;
            }

            if (Time.deltaTime <= 0.0001f) return;

            if (inDialogueFocus)
            {
                transform.position = Vector3.Lerp(transform.position, dialogueCamTargetPos, Time.deltaTime * 6.0f);
                transform.rotation = Quaternion.Slerp(transform.rotation, dialogueCamTargetRot, Time.deltaTime * 6.0f);
                if (cam != null) cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, dialogueFOV, Time.deltaTime * 4.0f);
                return;
            }
            else if (cam != null && Mathf.Abs(cam.fieldOfView - normalFOV) > 0.1f)
            {
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, normalFOV, Time.deltaTime * 4.0f);
            }

            Vector3 desiredPivot = target.position + pivotOffset;
            if (Vector3.Distance(currentPivotPosition, desiredPivot) > 6.0f)
            {
                currentPivotPosition = desiredPivot;
                smoothFollowVelocity = Vector3.zero;
            }
            else
            {
                currentPivotPosition = Vector3.SmoothDamp(currentPivotPosition, desiredPivot, ref smoothFollowVelocity, smoothFollowTime);
            }

            currentYaw = Mathf.SmoothDampAngle(currentYaw, yaw, ref yawVelocity, smoothRotationTime);
            currentPitch = Mathf.SmoothDampAngle(currentPitch, pitch, ref pitchVelocity, smoothRotationTime);

            Quaternion cameraRotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
            Vector3 rightOffset = cameraRotation * Vector3.right * shoulderOffset;
            Vector3 finalPivot = currentPivotPosition + rightOffset;
            Vector3 backDirection = cameraRotation * Vector3.back;

            float finalDistance = targetDistance;
            if (enableCollisionAvoidance)
            {
                if (Physics.SphereCast(finalPivot, collisionRadius, backDirection, out RaycastHit hit, targetDistance, obstacleLayers, QueryTriggerInteraction.Ignore))
                {
                    // Ignore player hierarchy, CharacterControllers, and thin tree/NPC CapsuleColliders so camera never clips inside the player
                    bool ignoreHit = hit.transform == target ||
                                     hit.transform.IsChildOf(target) ||
                                     hit.collider is CharacterController ||
                                     hit.collider is CapsuleCollider ||
                                     hit.distance < 0.65f;

                    if (!ignoreHit)
                    {
                        finalDistance = Mathf.Clamp(hit.distance - collisionOffset, minDistance, targetDistance);
                    }
                }
            }

            currentDistance = Mathf.SmoothDamp(currentDistance, finalDistance, ref distanceVelocity, 0.05f);

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
