using UnityEngine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Simulates buoyant oceanic floating and wind sway for the sailing boats in the Living Cà Mau painting:
    /// - Heave: Vertical wave bobbing (lên xuống theo nhịp sóng)
    /// - Roll: Lateral rocking angle (lắc lư đung đưa theo sóng biển)
    /// - Pitch: Longitudinal bow dip & rise (nhấp nhô mũi thuyền)
    /// - Drift: Subtle ocean current sway (trôi dạt êm ả)
    /// - Sail Billow: Gentle wind breathing in the fabric of the sails (buồm đón gió)
    /// </summary>
    public class LivingOceanBoatBobbing : MonoBehaviour
    {
        [Header("Buoyancy Settings")]
        [SerializeField] private float heaveAmplitude = 0.045f;
        [SerializeField] private float heavePeriod = 3.4f;
        [SerializeField] private float rollAngleMax = 1.65f;
        [SerializeField] private float rollPeriod = 3.9f;
        [SerializeField] private float pitchAngleMax = 0.65f;
        [SerializeField] private float pitchPeriod = 2.7f;
        [SerializeField] private float driftSwayAmount = 0.035f;
        [SerializeField] private float driftPeriod = 6.2f;

        [Header("Wind & Sail Breathing")]
        [SerializeField] private float sailBreathingAmount = 0.016f;
        [SerializeField] private float sailBreathingPeriod = 4.2f;

        [Header("Water Ripple")]
        [SerializeField] private SpriteRenderer wakeRippleRenderer;
        [SerializeField] private float phaseOffset = 0f;

        private Vector3 initialLocalPos;
        private Quaternion initialLocalRot;
        private Vector3 initialLocalScale;

        public void Configure(float heave, float roll, float pitch, float phase)
        {
            heaveAmplitude = heave;
            rollAngleMax = roll;
            pitchAngleMax = pitch;
            phaseOffset = phase;
        }

        public void SetWakeRenderer(SpriteRenderer wake)
        {
            wakeRippleRenderer = wake;
        }

        private void Awake()
        {
            initialLocalPos = transform.localPosition;
            initialLocalRot = transform.localRotation;
            initialLocalScale = transform.localScale;
        }

        private void Update()
        {
            float t = Time.time + phaseOffset;

            // 1. Dual-frequency sinusoidal heave (wave rise and fall)
            float heave1 = Mathf.Sin(t * (2f * Mathf.PI / heavePeriod));
            float heave2 = Mathf.Sin(t * 1.45f) * 0.25f;
            float currentHeave = (heave1 + heave2) * heaveAmplitude;

            // 2. Lateral ocean drift sway
            float drift = Mathf.Cos(t * (2f * Mathf.PI / driftPeriod)) * driftSwayAmount;

            transform.localPosition = initialLocalPos + new Vector3(drift, currentHeave, 0f);

            // 3. Roll tilt (rocking on waves, lagged by 45 degrees relative to heave)
            float rollAngle = Mathf.Sin(t * (2f * Mathf.PI / rollPeriod) - 0.785f) * rollAngleMax;
            float pitchAngle = Mathf.Cos(t * (2f * Mathf.PI / pitchPeriod)) * pitchAngleMax;

            transform.localRotation = initialLocalRot * Quaternion.Euler(pitchAngle, 0f, rollAngle);

            // 4. Sail breathing (subtle fabric billowing in the tropical sea breeze)
            float sailScaleX = 1.0f + Mathf.Sin(t * (2f * Mathf.PI / sailBreathingPeriod)) * sailBreathingAmount;
            float sailScaleY = 1.0f + Mathf.Cos(t * 1.8f) * (sailBreathingAmount * 0.75f);
            transform.localScale = new Vector3(initialLocalScale.x * sailScaleX, initialLocalScale.y * sailScaleY, initialLocalScale.z);

            // 5. Water wake ripple pulsing at the waterline
            if (wakeRippleRenderer != null)
            {
                float rippleAlpha = 0.35f + Mathf.Abs(heave1) * 0.45f;
                Color col = wakeRippleRenderer.color;
                col.a = rippleAlpha;
                wakeRippleRenderer.color = col;

                float rippleScale = 1.0f + (heave1 * 0.5f + 0.5f) * 0.22f;
                wakeRippleRenderer.transform.localScale = new Vector3(rippleScale, rippleScale * 0.45f, 1f);
            }
        }
    }
}
