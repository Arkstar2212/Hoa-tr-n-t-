using UnityEngine;

namespace HoaVaMau
{
    /// <summary>
    /// Camera 3D phiêu lưu điện ảnh (Isometric / Third-Person Adventure)
    /// Tự động bám theo Bé An trong không gian 3D, tái hiện góc nhìn toàn cảnh làng quê Cà Mau 1954
    /// </summary>
    public class Cinematic3DAdventureCamera : MonoBehaviour
    {
        [Header("Target Tracking")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.0f, 0f);

        [Header("Camera Geometry")]
        [Tooltip("Khoảng cách và góc nâng của Camera so với nhân vật")]
        [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 15.0f, -18.5f);
        [SerializeField] private float smoothTime = 0.22f;
        [SerializeField] private float lookAtDampTime = 0.15f;

        [Header("Camera Angle & FOV")]
        [SerializeField] private float fieldOfView = 38.0f;
        [SerializeField] private float lookAheadFactor = 1.2f;

        [Header("World Bounds")]
        [SerializeField] private bool useBounds = true;
        [SerializeField] private float minX = -32.0f;
        [SerializeField] private float maxX = 34.0f;
        [SerializeField] private float minZ = -22.0f;
        [SerializeField] private float maxZ = 24.0f;

        [Header("Zoom Controls")]
        [SerializeField] private bool enableZoom = true;
        [SerializeField] private float minZoom = 0.7f;
        [SerializeField] private float maxZoom = 1.4f;
        private float currentZoom = 1.0f;

        private Vector3 currentVelocity;
        private Vector3 currentLookTarget;
        private Vector3 lookVelocity;
        private Camera cam;

        public Transform Target
        {
            get => target;
            set => target = value;
        }

        private void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam != null)
            {
                cam.orthographic = false;
                cam.fieldOfView = fieldOfView;
            }
        }

        private void Start()
        {
            if (target == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null) target = player.transform;
            }

            if (target != null)
            {
                currentLookTarget = target.position + targetOffset;
                transform.position = currentLookTarget + cameraOffset;
                transform.LookAt(currentLookTarget);
            }
        }

        private void Update()
        {
            if (enableZoom)
            {
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    currentZoom = Mathf.Clamp(currentZoom - scroll * 1.5f, minZoom, maxZoom);
                }
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

            // Điểm ngắm nhìn nhân vật có tính toán Look-ahead
            Vector3 lookAhead = Vector3.zero;
            var cc = target.GetComponent<CharacterController>();
            if (cc != null && cc.velocity.sqrMagnitude > 0.1f)
            {
                lookAhead = cc.velocity.normalized * lookAheadFactor;
                lookAhead.y = 0;
            }

            Vector3 desiredLookTarget = target.position + targetOffset + lookAhead;
            currentLookTarget = Vector3.SmoothDamp(currentLookTarget, desiredLookTarget, ref lookVelocity, lookAtDampTime);

            // Vị trí mục tiêu của Camera
            Vector3 effectiveOffset = cameraOffset * currentZoom;
            Vector3 desiredPosition = currentLookTarget + effectiveOffset;

            if (useBounds)
            {
                desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
                desiredPosition.z = Mathf.Clamp(desiredPosition.z, minZ, maxZ);
            }

            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, smoothTime);
            transform.LookAt(currentLookTarget);
        }

        public void SnapToTarget()
        {
            if (target == null) return;
            currentLookTarget = target.position + targetOffset;
            transform.position = currentLookTarget + cameraOffset * currentZoom;
            transform.LookAt(currentLookTarget);
        }
    }
}
