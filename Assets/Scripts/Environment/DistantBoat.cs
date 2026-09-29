using UnityEngine;

namespace HoaVaMau.Environment
{
    /// <summary>
    /// Animates a distant boat on the water surface with gentle bobbing, rocking rotation, and slow horizontal drift.
    /// Fully exposed to the Inspector for quick cinematic tweaking.
    /// </summary>
    [ExecuteAlways]
    public class DistantBoat : MonoBehaviour
    {
        [Header("Bobbing (Up & Down)")]
        [Tooltip("Frequency/speed of vertical water bobbing")]
        [SerializeField] private float bobbingSpeed = 1.1f;
        [Tooltip("Height amplitude of bobbing motion")]
        [SerializeField] private float bobbingAmplitude = 0.08f;

        [Header("Rocking (Rotation)")]
        [Tooltip("Speed of pitch/roll rocking on water ripples")]
        [SerializeField] private float rockingSpeed = 0.85f;
        [Tooltip("Maximum tilt angle in degrees")]
        [SerializeField] private float rockingAngle = 3.5f;
        [Tooltip("Secondary roll offset angle")]
        [SerializeField] private float secondaryRoll = 1.8f;

        [Header("Drift (Horizontal Movement)")]
        [Tooltip("Slow horizontal drifting speed")]
        [SerializeField] private float driftSpeed = 0.15f;
        [Tooltip("Maximum drift distance before smoothly reversing")]
        [SerializeField] private float driftRange = 3.0f;

        [Header("Base Reference")]
        [SerializeField] private Vector3 basePosition;
        [SerializeField] private Quaternion baseRotation;
        [SerializeField] private bool captureCurrentAsBase = false;

        private void Awake()
        {
            InitializeBase();
        }

        private void OnEnable()
        {
            if (basePosition == Vector3.zero && baseRotation == Quaternion.identity)
            {
                InitializeBase();
            }
        }

        private void OnValidate()
        {
            if (captureCurrentAsBase)
            {
                captureCurrentAsBase = false;
                InitializeBase();
            }
        }

        public void InitializeBase()
        {
            basePosition = transform.localPosition;
            baseRotation = transform.localRotation;
        }

        private void Update()
        {
            float time = Application.isPlaying ? Time.time : (float)Time.realtimeSinceStartup;

            // 1. Vertical bobbing
            float bobY = Mathf.Sin(time * bobbingSpeed) * bobbingAmplitude 
                       + Mathf.Sin(time * bobbingSpeed * 0.45f + 1.2f) * (bobbingAmplitude * 0.3f);

            // 2. Horizontal drift (smooth sine oscillation across water)
            float driftX = Mathf.Sin(time * (driftSpeed / Mathf.Max(0.1f, driftRange))) * driftRange;

            transform.localPosition = new Vector3(
                basePosition.x + driftX,
                basePosition.y + bobY,
                basePosition.z
            );

            // 3. Rocking rotation (Z roll and slight Y yaw/X pitch)
            float rollZ = Mathf.Sin(time * rockingSpeed) * rockingAngle;
            float pitchX = Mathf.Cos(time * rockingSpeed * 0.7f) * secondaryRoll;

            transform.localRotation = baseRotation * Quaternion.Euler(pitchX, 0f, rollZ);
        }
    }
}
