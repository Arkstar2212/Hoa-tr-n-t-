using UnityEngine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Controls dynamic volumetric sunbeam shafts (tia nắng qua mây) streaming down on the Cà Mau ocean:
    /// - Multi-octave intensity breathing (ánh nắng lấp lánh xuyên qua tầng mây)
    /// - Subtle sweeping angular drift (chuyển động nghiêng nhẹ nhàng theo góc mặt trời)
    /// - Airborne golden sun dust motes drifting across the sunbeams
    /// </summary>
    public class LivingOceanSunbeams : MonoBehaviour
    {
        [Header("Sunbeam Intensity")]
        [SerializeField] private SpriteRenderer sunbeamRenderer;
        [SerializeField] private float baseAlpha = 0.65f;
        [SerializeField] private float pulseAmplitude = 0.22f;
        [SerializeField] private float pulseSpeed = 0.42f;

        [Header("Angular Sweep")]
        [SerializeField] private float maxSweepAngle = 0.85f;
        [SerializeField] private float sweepSpeed = 0.28f;

        [Header("Sun Motes (Bụi Nắng)")]
        [SerializeField] private ParticleSystem sunMotesParticles;

        private Quaternion initialRotation;
        private Color initialColor;

        public void Configure(SpriteRenderer renderer, ParticleSystem motes = null)
        {
            sunbeamRenderer = renderer;
            sunMotesParticles = motes;
            if (sunbeamRenderer != null)
            {
                initialColor = sunbeamRenderer.color;
            }
        }

        private void Awake()
        {
            if (sunbeamRenderer == null) sunbeamRenderer = GetComponent<SpriteRenderer>();
            if (sunbeamRenderer != null) initialColor = sunbeamRenderer.color;
            initialRotation = transform.localRotation;
        }

        private void Update()
        {
            float t = Time.time;

            // 1. Multi-octave natural breathing of light intensity
            float pulse1 = Mathf.Sin(t * pulseSpeed * 2f * Mathf.PI);
            float pulse2 = Mathf.Sin(t * (pulseSpeed * 1.62f) + 1.2f) * 0.4f;
            float pulse3 = Mathf.Cos(t * (pulseSpeed * 0.73f) - 0.5f) * 0.25f;
            float combinedPulse = (pulse1 + pulse2 + pulse3) / 1.65f;

            if (sunbeamRenderer != null)
            {
                float targetAlpha = Mathf.Clamp01(baseAlpha + combinedPulse * pulseAmplitude);
                Color c = initialColor;
                c.a = targetAlpha;
                sunbeamRenderer.color = c;
            }

            // 2. Slow angular sweeping drift across the sky
            float sweepAngle = Mathf.Sin(t * sweepSpeed * 2f * Mathf.PI) * maxSweepAngle;
            transform.localRotation = initialRotation * Quaternion.Euler(0f, 0f, sweepAngle);
        }
    }
}
