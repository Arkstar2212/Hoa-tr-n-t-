#pragma warning disable 0414
using System.Collections.Generic;
using UnityEngine;

namespace HoaVaMau
{
    /// <summary>
    /// Authentic Planet of Lana Multi-Layer Parallax Background Follower.
    /// Ensures that distant vistas, cobalt skies, and cloud layers dynamically travel WITH
    /// the player and camera across the entire level, rather than stretching across miles as static billboards.
    /// Features:
    /// - Smooth camera tracking in LateUpdate (Cinemachine compatible).
    /// - Calibrated horizontal & vertical parallax depth factors.
    /// - Multi-layer registration (Distant Sky, Far Mountains, Horizon Hills, Clouds).
    /// - Edit-mode preview support.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class LanaParallaxBackgroundFollower : MonoBehaviour
    {
        [System.Serializable]
        public class LayerSettings
        {
            public string name;
            public Transform layerTransform;
            [Range(0f, 1.05f)]
            [Tooltip("1.0 = moves 100% with camera (infinite distance). 0.88 = moves at 88% speed (12% subtle parallax drift).")]
            public float followFactorX = 0.88f;

            [Range(0f, 1.05f)]
            [Tooltip("Vertical camera follow factor for seamless intro pans & cliff climbs.")]
            public float followFactorY = 0.82f;

            public Vector3 offset = Vector3.zero;

            [HideInInspector] public Vector3 initialLocalPos;
            [HideInInspector] public Vector3 initialWorldPos;
        }

        [Header("Target Tracking")]
        [SerializeField] private Transform targetCamera;
        [SerializeField] private bool autoFindMainCamera = true;

        [Header("Global Settings")]
        [Tooltip("Base reference camera position where backgrounds are perfectly aligned.")]
        [SerializeField] private Vector3 referenceCameraPosition = new Vector3(0.0f, 0.35f, -13.5f);
        [SerializeField] private bool useCurrentCameraAsReferenceOnStart = false;

        [Header("Parallax Layers (Sky, Vistas, Clouds)")]
        [SerializeField] private List<LayerSettings> layers = new List<LayerSettings>();

        private Vector3 lastCameraPos;
        private bool isInitialized = false;

        public Transform TargetCamera
        {
            get => targetCamera;
            set => targetCamera = value;
        }

        public List<LayerSettings> Layers => layers;

        private void OnEnable()
        {
            InitializeLayers();
        }

        private void Start()
        {
            InitializeLayers();
        }

        public void InitializeLayers()
        {
            if (autoFindMainCamera && targetCamera == null)
            {
                Camera cam = Camera.main;
                if (cam != null) targetCamera = cam.transform;
            }

            if (targetCamera != null)
            {
                if (useCurrentCameraAsReferenceOnStart)
                {
                    referenceCameraPosition = targetCamera.position;
                }
                lastCameraPos = targetCamera.position;
            }

            foreach (var layer in layers)
            {
                if (layer != null && layer.layerTransform != null)
                {
                    // Keep initialWorldPos anchored to offset if already configured
                    if (layer.initialWorldPos == Vector3.zero && layer.offset != Vector3.zero)
                    {
                        layer.initialWorldPos = layer.offset;
                    }
                }
            }

            isInitialized = true;
        }

        public void RegisterLayer(string layerName, Transform t, float factorX = 0.88f, float factorY = 0.82f, Vector3 customOffset = default)
        {
            if (t == null) return;
            if (layers == null) layers = new List<LayerSettings>();

            t.position = customOffset;

            // Avoid duplicates
            for (int i = 0; i < layers.Count; i++)
            {
                if (layers[i].layerTransform == t)
                {
                    layers[i].name = layerName;
                    layers[i].followFactorX = factorX;
                    layers[i].followFactorY = factorY;
                    layers[i].offset = customOffset;
                    layers[i].initialWorldPos = customOffset;
                    layers[i].initialLocalPos = customOffset;
                    return;
                }
            }

            var newLayer = new LayerSettings
            {
                name = layerName,
                layerTransform = t,
                followFactorX = factorX,
                followFactorY = factorY,
                offset = customOffset,
                initialWorldPos = customOffset,
                initialLocalPos = customOffset
            };
            layers.Add(newLayer);
        }

        public void ClearLayers()
        {
            if (layers != null) layers.Clear();
        }

        public void LateUpdate()
        {
            if (targetCamera == null)
            {
                if (autoFindMainCamera)
                {
                    Camera cam = Camera.main;
                    if (cam != null) targetCamera = cam.transform;
                }
                if (targetCamera == null) return;
            }

            Vector3 currentCamPos = targetCamera.position;
            Vector3 camDeltaFromRef = currentCamPos - referenceCameraPosition;

            for (int i = 0; i < layers.Count; i++)
            {
                var layer = layers[i];
                if (layer == null || layer.layerTransform == null) continue;

                // Authentic Planet of Lana Parallax Formula:
                // Pos = InitialPos + (CamDelta * followFactor) + Offset
                Vector3 newPos = layer.initialWorldPos + layer.offset;
                newPos.x += camDeltaFromRef.x * layer.followFactorX;
                newPos.y += camDeltaFromRef.y * layer.followFactorY;

                layer.layerTransform.position = newPos;
            }

            lastCameraPos = currentCamPos;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying && targetCamera != null)
            {
                LateUpdate();
            }
        }
#endif
    }
}
