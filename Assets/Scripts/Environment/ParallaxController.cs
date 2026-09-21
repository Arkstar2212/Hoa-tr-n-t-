using System.Collections.Generic;
using UnityEngine;

namespace HoaVaMau
{
    /// <summary>
    /// Cinematic 2D Multi-Depth Parallax Controller.
    /// Drives subtle atmospheric parallax layers (Clouds, Mountains, Horizon) relative to camera movement.
    /// </summary>
    public class ParallaxController : MonoBehaviour
    {
        [System.Serializable]
        public class ParallaxLayer
        {
            public string name;
            public Transform layerTransform;
            [Range(-0.5f, 1.5f)] public float parallaxFactorX = 0.05f;
            [Range(-0.5f, 1.5f)] public float parallaxFactorY = 0.01f;
            public bool infiniteHorizontalScroll = true;
            public float textureWidth = 22.0f;
            [HideInInspector] public Vector3 startPosition;
        }

        [Header("Cinematic Auto Pan")]
        public bool enableAutoPan = false;
        public float autoPanSpeed = 0.4f;

        [Header("Camera Tracking")]
        [SerializeField] private Transform targetCamera;

        [Header("Parallax Layers (Background -> Foreground)")]
        [SerializeField] private List<ParallaxLayer> layers = new List<ParallaxLayer>();

        private Vector3 previousCameraPosition;

        public void AddLayer(string layerName, Transform t, float factorX, float factorY = 0.01f, float wrapWidth = 24f)
        {
            if (layers == null) layers = new List<ParallaxLayer>();
            layers.Add(new ParallaxLayer
            {
                name = layerName,
                layerTransform = t,
                parallaxFactorX = factorX,
                parallaxFactorY = factorY,
                textureWidth = wrapWidth,
                infiniteHorizontalScroll = true
            });
        }

        public void ClearLayers()
        {
            if (layers != null) layers.Clear();
        }

        private void Start()
        {
            if (targetCamera == null)
            {
                Camera cam = Camera.main;
                if (cam != null) targetCamera = cam.transform;
            }

            if (targetCamera != null)
            {
                previousCameraPosition = targetCamera.position;
            }

            foreach (var layer in layers)
            {
                if (layer.layerTransform != null)
                {
                    layer.startPosition = layer.layerTransform.position;
                }
            }
        }

        private void LateUpdate()
        {
            if (targetCamera == null) return;

            if (enableAutoPan)
            {
                targetCamera.position += new Vector3(autoPanSpeed * Time.deltaTime, 0f, 0f);
            }

            Vector3 cameraDelta = targetCamera.position - previousCameraPosition;

            foreach (var layer in layers)
            {
                if (layer.layerTransform == null) continue;

                // Move layer by parallax factor
                Vector3 currentPos = layer.layerTransform.position;
                currentPos.x += cameraDelta.x * (1f - layer.parallaxFactorX);
                currentPos.y += cameraDelta.y * (1f - layer.parallaxFactorY);

                // Seamless horizontal infinite wrapping
                if (layer.infiniteHorizontalScroll && layer.textureWidth > 0f)
                {
                    float dist = targetCamera.position.x - layer.startPosition.x;
                    if (Mathf.Abs(dist) >= layer.textureWidth)
                    {
                        layer.startPosition.x += Mathf.Sign(dist) * layer.textureWidth;
                        currentPos.x += Mathf.Sign(dist) * layer.textureWidth;
                    }
                }

                layer.layerTransform.position = currentPos;
            }

            previousCameraPosition = targetCamera.position;
        }
    }
}
