using UnityEngine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Smooth cinematic camera drift for an idle title / chill screen.
    /// Simulates a slow, gentle floating camera capturing the calm ocean scenery.
    /// </summary>
    public class CinematicMenuCamera : MonoBehaviour
    {
        [Header("Framing Target (Optional)")]
        [SerializeField] private Transform targetPoint;

        [Header("Floating Drift Tuning")]
        [SerializeField] private float positionDriftAmplitude = 0.12f;
        [SerializeField] private float positionDriftFrequency = 0.25f;
        [SerializeField] private float rotationDriftAmplitude = 0.35f;
        [SerializeField] private float rotationDriftFrequency = 0.20f;

        private Vector3 originPosition;
        private Quaternion originRotation;

        private void Start()
        {
            originPosition = transform.position;
            originRotation = transform.rotation;
        }

        private void LateUpdate()
        {
            float t = Time.time;

            // Multi-frequency smooth drift
            float posX = Mathf.Sin(t * positionDriftFrequency * 1.0f) * positionDriftAmplitude;
            float posY = Mathf.Cos(t * positionDriftFrequency * 0.7f) * (positionDriftAmplitude * 0.7f);
            float posZ = Mathf.Sin(t * positionDriftFrequency * 0.5f + 1.0f) * (positionDriftAmplitude * 0.5f);

            transform.position = originPosition + new Vector3(posX, posY, posZ);

            float rotY = Mathf.Sin(t * rotationDriftFrequency * 0.9f) * rotationDriftAmplitude;
            float rotX = Mathf.Cos(t * rotationDriftFrequency * 0.6f) * (rotationDriftAmplitude * 0.5f);

            transform.rotation = originRotation * Quaternion.Euler(rotX, rotY, 0f);
        }
    }
}
