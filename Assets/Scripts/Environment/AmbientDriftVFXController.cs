using UnityEngine;

namespace HoaTrenDatDo.Environment
{
    /// <summary>
    /// Smoothly keeps the ambient particle emitters framed around the active camera/player in 2.5D,
    /// while particles simulate in World Space for realistic parallax and wind drift.
    /// Strictly clamps Y elevation to ground/canopy level so leaves NEVER spawn in the high sky or clouds during entry cutscenes.
    /// Automatically suppresses emission while the camera is high in the sky/clouds.
    /// </summary>
    [ExecuteAlways]
    public class AmbientDriftVFXController : MonoBehaviour
    {
        [Tooltip("Target camera to follow. If null, automatically resolves Camera.main.")]
        [SerializeField] private Camera targetCamera;

        [Header("Offset Settings")]
        [Tooltip("Horizontal offset (spawning slightly upwind / to the left)")]
        [SerializeField] private float offsetX = -2.0f;

        [Tooltip("Vertical offset")]
        [SerializeField] private float offsetY = 1.0f;

        [Header("Ground & Altitude Clamping")]
        [Tooltip("Minimum Y height for the emitter.")]
        [SerializeField] private float minEmitterY = 0.5f;

        [Tooltip("Maximum Y height for the emitter. Prevents leaves from following camera into the sky.")]
        [SerializeField] private float maxEmitterY = 3.5f;

        [Header("Sky Cutscene Suppression")]
        [Tooltip("Altitude above which the camera is considered high in the sky/clouds. VFX emission is suppressed while camera Y is above this threshold.")]
        [SerializeField] private float skyCutoffAltitude = 6.0f;

        private ParticleSystem[] cachedParticleSystems;
        private bool isEmittingSuppressed = false;

        private void Awake()
        {
            CacheParticleSystems();
        }

        private void OnEnable()
        {
            CacheParticleSystems();
            UpdateVFXPositionAndEmission(true);
        }

        private void CacheParticleSystems()
        {
            if (cachedParticleSystems == null || cachedParticleSystems.Length == 0)
            {
                cachedParticleSystems = GetComponentsInChildren<ParticleSystem>(true);
            }
        }

        private void LateUpdate()
        {
            UpdateVFXPositionAndEmission(false);
        }

        private void UpdateVFXPositionAndEmission(bool forceImmediate)
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
                if (targetCamera == null) return;
            }

            Vector3 camPos = targetCamera.transform.position;

            // 1. Clamp Y strictly to ground/canopy elevation (leaves belong near trees/ground, never in high sky)
            float clampedY = Mathf.Clamp(camPos.y + offsetY, minEmitterY, maxEmitterY);

            // Keep Z centered on the 2.5D gameplay plane (Z = 0)
            transform.position = new Vector3(camPos.x + offsetX, clampedY, 0f);

            // 2. Check if camera is currently high in the sky / descending through clouds
            bool cameraInSky = camPos.y > skyCutoffAltitude;

            if (cameraInSky)
            {
                if (!isEmittingSuppressed || forceImmediate)
                {
                    isEmittingSuppressed = true;
                    SetEmissionActive(false);

                    // Clear any lingering particles so sky entry remains completely pristine
                    ClearAllParticles();
                }
            }
            else
            {
                if (isEmittingSuppressed || forceImmediate)
                {
                    isEmittingSuppressed = false;
                    SetEmissionActive(true);
                }
            }
        }

        private void SetEmissionActive(bool active)
        {
            CacheParticleSystems();
            if (cachedParticleSystems == null) return;

            for (int i = 0; i < cachedParticleSystems.Length; i++)
            {
                if (cachedParticleSystems[i] != null)
                {
                    var emission = cachedParticleSystems[i].emission;
                    emission.enabled = active;
                }
            }
        }

        public void ClearAllParticles()
        {
            CacheParticleSystems();
            if (cachedParticleSystems == null) return;

            for (int i = 0; i < cachedParticleSystems.Length; i++)
            {
                if (cachedParticleSystems[i] != null)
                {
                    cachedParticleSystems[i].Clear();
                }
            }
        }
    }
}
