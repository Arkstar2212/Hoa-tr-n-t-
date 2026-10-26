using UnityEngine;
using Unity.Cinemachine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Dynamic Camera Framing & Panorama Reveal Zone (Planet of Lana 0:01:10 Style).
    /// When the player enters this zone (e.g. cresting a hill or standing on a high dock),
    /// the camera smoothly pulls back into a wide-angle vista shot to emphasize the grand scale
    /// and atmospheric depth of the world, then returns to standard framing on exit.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class CameraPanoramaZone : MonoBehaviour
    {
        [Header("Framing & Field of View")]
        [Tooltip("Expanded field of view to reveal the grand vista (Planet of Lana wide tracking)")]
        [SerializeField] private float panoramaFov = 45f;
        [Tooltip("Standard traversal field of view")]
        [SerializeField] private float defaultFov = 38f;
        [Tooltip("Additional camera height offset (Y) to reveal sky clouds or lower river reflections")]
        [SerializeField] private float targetOffsetY = 2.4f;
        [Tooltip("Default camera height offset (Y)")]
        [SerializeField] private float defaultOffsetY = 2.0f;

        [Header("Transition Smoothing")]
        [SerializeField] private float transitionSpeed = 1.8f;

        [Header("Cinemachine Integration (Optional)")]
        [SerializeField] private CinemachineCamera virtualCamera;

        private Cinematic2DCamera customCamera;
        private Camera unityCamera;
        private bool isPlayerInside = false;

        private void Awake()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            customCamera = UnityEngine.Object.FindAnyObjectByType<Cinematic2DCamera>();
            unityCamera = Camera.main;
            if (virtualCamera == null)
            {
                var vcamGO = GameObject.Find("01_Camera/CM_VirtualCamera_2.5D") ?? GameObject.Find("CM_VirtualCamera_2.5D");
                if (vcamGO != null) virtualCamera = vcamGO.GetComponent<CinemachineCamera>();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsPlayer(other))
            {
                isPlayerInside = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (IsPlayer(other))
            {
                isPlayerInside = false;
            }
        }

        private bool IsPlayer(Collider col)
        {
            return col.CompareTag("Player") || col.GetComponent<PlayerController>() != null;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            float targetFov = isPlayerInside ? panoramaFov : defaultFov;

            // Update Cinemachine Lens FOV on the 2.5D follow camera so CinemachineBrain blends cleanly
            if (virtualCamera != null)
            {
                var lens = virtualCamera.Lens;
                if (Mathf.Abs(lens.FieldOfView - targetFov) > 0.01f)
                {
                    lens.FieldOfView = Mathf.Lerp(lens.FieldOfView, targetFov, dt * transitionSpeed);
                    virtualCamera.Lens = lens;
                }
            }
            else if (unityCamera != null)
            {
                unityCamera.fieldOfView = Mathf.Lerp(unityCamera.fieldOfView, targetFov, dt * transitionSpeed);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f);
            var col = GetComponent<BoxCollider>();
            if (col != null)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(col.center, col.size);
                Gizmos.DrawWireCube(col.center, col.size);
            }
        }
    }
}
