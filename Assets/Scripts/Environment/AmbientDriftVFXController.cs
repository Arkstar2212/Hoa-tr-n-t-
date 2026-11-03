using UnityEngine;

namespace HoaTrenDatDo.Environment
{
    /// <summary>
    /// Smoothly keeps the ambient particle emitters framed around the active camera/player in 2.5D,
    /// while particles simulate in World Space for realistic parallax and wind drift.
    /// </summary>
    [ExecuteAlways]
    public class AmbientDriftVFXController : MonoBehaviour
    {
        [Tooltip("Target camera to follow. If null, automatically resolves Camera.main.")]
        [SerializeField] private Camera targetCamera;

        [Tooltip("Horizontal offset (spawning slightly upwind / to the left)")]
        [SerializeField] private float offsetX = -2.0f;

        [Tooltip("Vertical offset")]
        [SerializeField] private float offsetY = 1.0f;

        private void LateUpdate()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
                if (targetCamera == null) return;
            }

            Vector3 camPos = targetCamera.transform.position;
            // Keep Z centered on the 2.5D gameplay plane so leaves float organically around player depth
            transform.position = new Vector3(camPos.x + offsetX, camPos.y + offsetY, 0f);
        }
    }
}
