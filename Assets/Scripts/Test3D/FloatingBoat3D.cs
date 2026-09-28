using UnityEngine;

namespace HoaVaMau.Test3D
{
    public class FloatingBoat3D : MonoBehaviour
    {
        [Header("Bobbing (Up / Down)")]
        [SerializeField] private float bobHeight = 0.08f;
        [SerializeField] private float bobSpeed = 1.2f;

        [Header("Rocking (Rotation)")]
        [SerializeField] private float pitchAngle = 2.5f;
        [SerializeField] private float rollAngle = 4.0f;
        [SerializeField] private float rockSpeed = 1.0f;

        private Vector3 initialPosition;
        private Quaternion initialRotation;

        private void Start()
        {
            initialPosition = transform.position;
            initialRotation = transform.rotation;
        }

        private void Update()
        {
            float time = Time.time;

            // Bobbing movement
            float yOffset = Mathf.Sin(time * bobSpeed) * bobHeight;
            transform.position = initialPosition + new Vector3(0, yOffset, 0);

            // Rocking movement
            float pitch = Mathf.Sin(time * rockSpeed) * pitchAngle;
            float roll = Mathf.Cos(time * rockSpeed * 0.8f) * rollAngle;
            transform.rotation = initialRotation * Quaternion.Euler(pitch, 0, roll);
        }
    }
}
