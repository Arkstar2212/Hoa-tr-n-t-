#pragma warning disable 0414
using UnityEngine;
using Unity.Cinemachine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Dynamic Camera Framing & Panorama Reveal Zone (Planet of Lana 0:01:10 Style).
    /// When the player enters this zone (e.g. cresting a hill or entering the rice field),
    /// smoothly transitions the camera into a dedicated framing / FOV, then returns to standard framing on exit.
    /// Supports both PhysX trigger events and robust bounding-box coordinate tracking for CharacterController.
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
        [SerializeField] private CinemachineCamera dedicatedVirtualCamera;

        private Cinematic2DCamera customCamera;
        private Camera unityCamera;
        private Collider zoneCollider;
        private Transform playerTransform;
        private bool isPlayerInside = false;
        private bool wasInside = false;

        private void Awake()
        {
            zoneCollider = GetComponent<Collider>();
            if (zoneCollider != null) zoneCollider.isTrigger = true;

            if (dedicatedVirtualCamera != null) dedicatedVirtualCamera.Priority.Value = 0;
            customCamera = UnityEngine.Object.FindAnyObjectByType<Cinematic2DCamera>();
            unityCamera = Camera.main;
            ResolveVirtualCamera();
        }

        private void Start()
        {
            ResolvePlayer();
        }

        private void ResolveVirtualCamera()
        {
            if (virtualCamera == null)
            {
                var vcamGO = GameObject.Find("01_Camera/CM_VirtualCamera_2.5D") ?? GameObject.Find("CM_VirtualCamera_2.5D");
                if (vcamGO != null) virtualCamera = vcamGO.GetComponent<CinemachineCamera>();
            }
        }

        private void ResolvePlayer()
        {
            if (playerTransform != null) return;
            var playerGO = GameObject.FindWithTag("Player") ?? GameObject.Find("PF_Player_Minh") ?? GameObject.Find("Character_An");
            if (playerGO != null) playerTransform = playerGO.transform;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsPlayer(other))
            {
                SetInside(true);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (IsPlayer(other))
            {
                SetInside(false);
            }
        }

        private bool IsPlayer(Collider col)
        {
            return col.CompareTag("Player") || col.GetComponent<PlayerController>() != null;
        }

        private void SetInside(bool inside)
        {
            if (isPlayerInside == inside) return;
            isPlayerInside = inside;

            if (dedicatedVirtualCamera != null)
            {
                dedicatedVirtualCamera.Priority.Value = isPlayerInside ? 60 : 0;
            }
            if (isPlayerInside)
            {
                wasInside = true;
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // Robust bounding box check in case CharacterController didn't fire PhysX OnTriggerEnter
            if (zoneCollider != null)
            {
                if (playerTransform == null) ResolvePlayer();
                if (playerTransform != null)
                {
                    Vector3 checkPos = playerTransform.position + Vector3.up * 0.8f;
                    bool bounded = zoneCollider.bounds.Contains(checkPos);
                    if (bounded != isPlayerInside)
                    {
                        SetInside(bounded);
                    }
                }
            }

            // If this zone uses a dedicated Cinemachine Camera (e.g. Rice Field VCam)
            if (dedicatedVirtualCamera != null)
            {
                int targetPriority = isPlayerInside ? 60 : 0;
                if (dedicatedVirtualCamera.Priority.Value != targetPriority)
                {
                    dedicatedVirtualCamera.Priority.Value = targetPriority;
                }
                return;
            }

            // If this zone directly drives the main gameplay virtualCamera Lens FOV (e.g. Zone 2 Hill Overlook)
            ResolveVirtualCamera();
            if (isPlayerInside)
            {
                DriveFov(panoramaFov, dt);
                wasInside = true;
            }
            else if (wasInside)
            {
                // Smoothly return to default traversal FOV (38), then release control
                bool reachedDefault = DriveFov(defaultFov, dt);
                if (reachedDefault)
                {
                    wasInside = false; // Successfully released, no longer touches virtualCamera!
                }
            }
        }

        private bool DriveFov(float targetFov, float dt)
        {
            if (virtualCamera != null)
            {
                var lens = virtualCamera.Lens;
                if (Mathf.Abs(lens.FieldOfView - targetFov) > 0.02f)
                {
                    lens.FieldOfView = Mathf.Lerp(lens.FieldOfView, targetFov, dt * transitionSpeed);
                    virtualCamera.Lens = lens;
                    return false;
                }
                lens.FieldOfView = targetFov;
                virtualCamera.Lens = lens;
                return true;
            }
            if (unityCamera != null)
            {
                unityCamera.fieldOfView = Mathf.Lerp(unityCamera.fieldOfView, targetFov, dt * transitionSpeed);
                return Mathf.Abs(unityCamera.fieldOfView - targetFov) <= 0.05f;
            }
            return true;
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
