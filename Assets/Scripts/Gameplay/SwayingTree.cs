using UnityEngine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Background environmental tree whose canopy harmoniously sways with WindManager wind waves.
    /// Positioned in the background layer (Z > 3.0) to enrich the cinematic 2.5D composition.
    /// </summary>
    public class SwayingTree : MonoBehaviour
    {
        [Header("Sway Configuration")]
        [SerializeField] private float swayAngle = 3.5f;
        [SerializeField] private float canopyFlutter = 1.2f;
        [SerializeField] private float swaySpeed = 1.2f;

        [Header("References")]
        [SerializeField] private Transform trunkTransform;
        [SerializeField] private Transform canopyTransform;

        private Quaternion initialTrunkRot;
        private Quaternion initialCanopyRot;
        private float phaseOffset;

        private void Start()
        {
            if (trunkTransform == null) trunkTransform = transform.Find("Trunk");
            if (canopyTransform == null) canopyTransform = transform.Find("Canopy");

            initialTrunkRot = trunkTransform != null ? trunkTransform.localRotation : transform.localRotation;
            initialCanopyRot = canopyTransform != null ? canopyTransform.localRotation : Quaternion.identity;

            phaseOffset = transform.position.x * 0.3f;
        }

        private void Update()
        {
            float windStrength = EnvironmentManager.Instance != null ? EnvironmentManager.Instance.CurrentWindStrength : 1.0f;
            float t = Time.time * swaySpeed * Mathf.Clamp(0.85f + windStrength * 0.2f, 0.85f, 1.8f) + phaseOffset;
            float windFactor = WindManager.Instance != null ? WindManager.WindWave : Mathf.Sin(t);

            float trunkAngle = (Mathf.Sin(t) * swayAngle + windFactor * 1.6f) * (0.6f + windStrength * 0.45f);
            float canopyExtra = (Mathf.Cos(t * 1.3f) * canopyFlutter + windFactor * 0.8f) * (0.6f + windStrength * 0.45f);

            if (trunkTransform != null)
            {
                trunkTransform.localRotation = initialTrunkRot * Quaternion.Euler(0f, 0f, -trunkAngle * 0.5f);
            }

            if (canopyTransform != null)
            {
                canopyTransform.localRotation = initialCanopyRot * Quaternion.Euler(0f, 0f, -(trunkAngle + canopyExtra));
            }
        }
    }
}
