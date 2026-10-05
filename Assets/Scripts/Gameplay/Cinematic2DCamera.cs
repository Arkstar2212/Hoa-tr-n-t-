using UnityEngine;

namespace HoaVaMau.Gameplay
{
    public enum CameraPerspectiveMode
    {
        SideScroll2D,
        FreeRoom3D
    }

    /// <summary>
    /// Hybrid 2D/3D Cinematic Camera Controller (NieR: Automata Style).
    /// Dynamically transitions between locked 2.5D side-scrolling traversal
    /// and 3D room/camp interior perspective with smooth positional and rotational interpolation.
    /// </summary>
    public class Cinematic2DCamera : MonoBehaviour
    {
        [Header("Target Tracking")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector2 cameraOffset2D = new Vector2(0f, 1.5f);
        [SerializeField] private float lookAheadDistance = 1.2f;
        [SerializeField] private float smoothTimeX = 0.10f;
        [SerializeField] private float smoothTimeY = 0.16f;
        [SerializeField] private float lookAheadSmoothTime = 0.20f;
        [SerializeField] private float fixedZ = -11.5f;

        [Header("Cinematic Frame Constraints (2D)")]
        [Tooltip("When enabled, clamps camera between minCameraX and maxCameraX.")]
        [SerializeField] private bool clampCameraBounds = false;
        [Tooltip("Minimum camera Y height (prevents camera from plunging down when player falls into pits)")]
        [SerializeField] private float minCameraY = 1.0f;
        [Tooltip("Minimum and maximum camera X world coordinates (used only when clampCameraBounds is enabled)")]
        [SerializeField] private float minCameraX = -35.0f;
        [SerializeField] private float maxCameraX = 120.0f;

        [Header("Interior 3D Room Camera (NieR: Automata Style)")]
        [SerializeField] private CameraPerspectiveMode currentMode = CameraPerspectiveMode.SideScroll2D;
        public CameraPerspectiveMode CurrentMode => currentMode;

        [SerializeField] private Vector3 roomOffset3D = new Vector3(0f, 5.8f, -7.2f);
        [SerializeField] private Vector3 roomRotationEuler = new Vector3(30f, 0f, 0f);
        [SerializeField] private float roomFov = 44f;
        [SerializeField] private float defaultFov = 36f;
        [SerializeField] private float transitionSpeedPos = 4.0f;
        [SerializeField] private float transitionSpeedRot = 3.5f;

        // Interior room camera clamping bounds - covers whole shelter up to pier exit
        [SerializeField] private Vector3 roomMinCamPos = new Vector3(34.0f, 4.5f, -8.0f);
        [SerializeField] private Vector3 roomMaxCamPos = new Vector3(56.0f, 10.0f, 8.0f);

        private float currentLookAheadX = 0f;
        private float lookAheadVelocity = 0f;
        private float velocityX = 0f;
        private float velocityY = 0f;
        private float velocityZ = 0f;

        private PlayerController playerCtrl;
        private Camera camComponent;

        public Transform Target
        {
            get => target;
            set { target = value; FindPlayerReference(); }
        }

        private void Awake()
        {
            camComponent = GetComponent<Camera>();
            FindPlayerReference();
            if (target != null && target.position.x < 33.5f)
            {
                currentMode = CameraPerspectiveMode.SideScroll2D;
            }
            if (camComponent != null)
            {
                camComponent.orthographic = false;
                camComponent.fieldOfView = (currentMode == CameraPerspectiveMode.SideScroll2D) ? defaultFov : roomFov;
            }
        }

        private void OnEnable()
        {
            FindPlayerReference();
            if (target != null)
            {
                SnapToTarget();
            }
        }

        private void Start()
        {
            FindPlayerReference();
            if (target != null)
            {
                SnapToTarget();
            }
        }

        /// <summary>
        /// Self-healing player target finder.
        /// Resolves the moving character even if target was set to a parent container (e.g. "Player")
        /// or an inactive/destroyed reference.
        /// </summary>
        public void FindPlayerReference()
        {
            if (target == null || !target.gameObject.activeInHierarchy || playerCtrl == null || target.GetComponent<PlayerController>() == null)
            {
                var player = Object.FindAnyObjectByType<PlayerController>();
                if (player != null)
                {
                    target = player.transform;
                    playerCtrl = player;
                }
                else
                {
                    var playerGo = GameObject.Find("Player_Minh");
                    if (playerGo != null)
                    {
                        target = playerGo.transform;
                        playerCtrl = playerGo.GetComponent<PlayerController>();
                    }
                }
            }
            else
            {
                playerCtrl = target.GetComponent<PlayerController>();
            }
        }

        public void SnapToTarget()
        {
            FindPlayerReference();
            if (target == null) return;

            // Reset current mode if target is in outdoor side-scrolling area
            if (target.position.x < 33.5f)
            {
                currentMode = CameraPerspectiveMode.SideScroll2D;
            }

            float facing = (playerCtrl != null) ? playerCtrl.FacingDirection : 1f;
            currentLookAheadX = facing * lookAheadDistance;
            lookAheadVelocity = 0f;
            velocityX = 0f;
            velocityY = 0f;
            velocityZ = 0f;

            Vector3 trackPos = (playerCtrl != null) ? playerCtrl.CameraTargetPosition : target.position;
            if (currentMode == CameraPerspectiveMode.SideScroll2D)
            {
                float startX = trackPos.x + cameraOffset2D.x + currentLookAheadX;
                float startY = Mathf.Max(trackPos.y + cameraOffset2D.y, minCameraY);
                if (clampCameraBounds)
                {
                    startX = Mathf.Clamp(startX, minCameraX, maxCameraX);
                }

                transform.position = new Vector3(startX, startY, fixedZ);
                transform.rotation = Quaternion.identity;
            }
            else
            {
                Vector3 idealPos = trackPos + roomOffset3D;
                idealPos.x = Mathf.Clamp(idealPos.x, roomMinCamPos.x, roomMaxCamPos.x);
                idealPos.y = Mathf.Clamp(idealPos.y, roomMinCamPos.y, roomMaxCamPos.y);
                idealPos.z = Mathf.Clamp(idealPos.z, roomMinCamPos.z, roomMaxCamPos.z);
                transform.position = idealPos;
                transform.rotation = Quaternion.Euler(roomRotationEuler);
            }
        }

        public void ConfigureLevelBounds(float minX, float maxX, float minY, bool enableClamp = false)
        {
            minCameraX = minX;
            maxCameraX = maxX;
            minCameraY = minY;
            clampCameraBounds = enableClamp;
        }

        public void SetCameraMode(CameraPerspectiveMode mode)
        {
            currentMode = mode;
        }

        public void ConfigureRoomCamera(Vector3 offset, Vector3 rotationEuler, float fov, Vector3 minPos, Vector3 maxPos)
        {
            roomOffset3D = offset;
            roomRotationEuler = rotationEuler;
            roomFov = fov;
            roomMinCamPos = minPos;
            roomMaxCamPos = maxPos;
        }

        private void LateUpdate()
        {
            if (target == null || playerCtrl == null)
            {
                FindPlayerReference();
                if (target == null) return;
            }

            float dt = Time.deltaTime;
            if (dt <= 0f)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) dt = 0.02f;
                else return;
#else
                return;
#endif
            }

            // Pure 2.5D Cinematic Traversal (Planet of Lana Style)
            currentMode = CameraPerspectiveMode.SideScroll2D;
            Update2DMode(dt);
        }

        private void Update2DMode(float dt)
        {
            // Target facing look-ahead with smooth damping
            float facing = (playerCtrl != null) ? playerCtrl.FacingDirection : 1f;
            float targetLookAhead = facing * lookAheadDistance;

            if (float.IsNaN(currentLookAheadX) || float.IsInfinity(currentLookAheadX)) currentLookAheadX = 0f;
            if (float.IsNaN(lookAheadVelocity) || float.IsInfinity(lookAheadVelocity)) lookAheadVelocity = 0f;

            currentLookAheadX = Mathf.SmoothDamp(currentLookAheadX, targetLookAhead, ref lookAheadVelocity, Mathf.Max(lookAheadSmoothTime, 0.05f), Mathf.Infinity, dt);

            Vector3 currentPos = transform.position;
            Vector3 trackPos = (playerCtrl != null) ? playerCtrl.CameraTargetPosition : target.position;
            float targetX = trackPos.x + cameraOffset2D.x + currentLookAheadX;
            float targetY = Mathf.Max(trackPos.y + cameraOffset2D.y, minCameraY);

            // Clamp camera horizontal boundaries in 2D only if requested
            if (clampCameraBounds)
            {
                targetX = Mathf.Clamp(targetX, minCameraX, maxCameraX);
            }

            if (float.IsNaN(velocityX) || float.IsInfinity(velocityX)) velocityX = 0f;
            if (float.IsNaN(velocityY) || float.IsInfinity(velocityY)) velocityY = 0f;
            if (float.IsNaN(velocityZ) || float.IsInfinity(velocityZ)) velocityZ = 0f;

            float newX = Mathf.SmoothDamp(currentPos.x, targetX, ref velocityX, Mathf.Max(smoothTimeX, 0.05f), Mathf.Infinity, dt);
            float newY = Mathf.SmoothDamp(currentPos.y, targetY, ref velocityY, Mathf.Max(smoothTimeY, 0.05f), Mathf.Infinity, dt);
            float newZ = Mathf.SmoothDamp(currentPos.z, fixedZ, ref velocityZ, Mathf.Max(smoothTimeX, 0.05f), Mathf.Infinity, dt);

            transform.position = new Vector3(newX, newY, newZ);

            // Smoothly return rotation to standard 2D side-scroll (Euler 0, 0, 0)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.identity, dt * transitionSpeedRot);

            // Smoothly interpolate FOV
            if (camComponent != null)
            {
                camComponent.fieldOfView = Mathf.Lerp(camComponent.fieldOfView, defaultFov, dt * 2.5f);
            }
        }

        private void Update3DMode(float dt)
        {
            Vector3 currentPos = transform.position;
            Vector3 trackPos = (playerCtrl != null) ? playerCtrl.CameraTargetPosition : target.position;

            // Compute ideal 3D camera position relative to player
            Vector3 idealPos = trackPos + roomOffset3D;

            // Clamp camera position within room boundaries
            idealPos.x = Mathf.Clamp(idealPos.x, roomMinCamPos.x, roomMaxCamPos.x);
            idealPos.y = Mathf.Clamp(idealPos.y, roomMinCamPos.y, roomMaxCamPos.y);
            idealPos.z = Mathf.Clamp(idealPos.z, roomMinCamPos.z, roomMaxCamPos.z);

            // Smoothly lerp position into the 3D room
            transform.position = Vector3.Lerp(currentPos, idealPos, dt * transitionSpeedPos);

            // Target rotation looking at player / room interior
            Quaternion targetRot = Quaternion.Euler(roomRotationEuler);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, dt * transitionSpeedRot);

            // Smoothly interpolate FOV for wide interior framing
            if (camComponent != null)
            {
                camComponent.fieldOfView = Mathf.Lerp(camComponent.fieldOfView, roomFov, dt * 2.5f);
            }
        }
    }
}
