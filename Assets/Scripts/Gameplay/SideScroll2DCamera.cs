using UnityEngine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Fixed Orthographic Side-Scrolling Camera Controller.
    /// Locks camera orientation to pure side-view, locks Z axis, and exposes orthographic framing,
    /// target tracking, deadzone, and bounds in the Inspector.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public class SideScroll2DCamera : MonoBehaviour
    {
        [Header("Orthographic View Settings")]
        [Tooltip("Half the vertical size of the camera viewing volume")]
        [SerializeField] private float orthographicSize = 6.0f;
        [Tooltip("Fixed camera Z depth (distance back from the 2.5D gameplay plane)")]
        [SerializeField] private float fixedZ = -15.0f;

        [Header("Target Tracking")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector2 targetOffset = new Vector2(0f, 1.5f);
        [SerializeField] private float smoothTimeX = 0.15f;
        [SerializeField] private float smoothTimeY = 0.25f;

        [Header("Camera World Bounds (Optional)")]
        [SerializeField] private bool useBounds = false;
        [SerializeField] private float minX = -50f;
        [SerializeField] private float maxX = 50f;
        [SerializeField] private float minY = 0f;
        [SerializeField] private float maxY = 20f;

        private Camera cam;
        private float velocityX;
        private float velocityY;

        public float OrthographicSize
        {
            get => orthographicSize;
            set
            {
                orthographicSize = value;
                ApplyCameraSettings();
            }
        }

        public Transform Target
        {
            get => target;
            set => target = value;
        }

        private void Awake()
        {
            cam = GetComponent<Camera>();
            ApplyCameraSettings();
        }

        private void OnValidate()
        {
            if (cam == null) cam = GetComponent<Camera>();
            ApplyCameraSettings();
        }

        public void ApplyCameraSettings()
        {
            if (cam == null) return;

            cam.orthographic = true;
            cam.orthographicSize = orthographicSize;

            // Enforce pure side-view (facing directly down +Z axis)
            transform.rotation = Quaternion.identity;

            // Enforce fixed Z
            Vector3 pos = transform.position;
            pos.z = fixedZ;
            transform.position = pos;
        }

        private void LateUpdate()
        {
            ApplyCameraSettings();

            if (target == null) return;

            Vector3 currentPos = transform.position;
            Vector3 targetGoal = target.position + (Vector3)targetOffset;

            float newX = Mathf.SmoothDamp(currentPos.x, targetGoal.x, ref velocityX, smoothTimeX);
            float newY = Mathf.SmoothDamp(currentPos.y, targetGoal.y, ref velocityY, smoothTimeY);

            if (useBounds)
            {
                newX = Mathf.Clamp(newX, minX, maxX);
                newY = Mathf.Clamp(newY, minY, maxY);
            }

            transform.position = new Vector3(newX, newY, fixedZ);
        }
    }
}
