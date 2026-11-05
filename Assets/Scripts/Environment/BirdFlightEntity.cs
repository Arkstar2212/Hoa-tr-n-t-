using UnityEngine;

namespace HoaTrenDatDo.Environment
{
    /// <summary>
    /// Controls an individual ambient bird flying across the sky.
    /// Simulates natural wing-flapping flight with sinusoidal altitude bobbing,
    /// dynamic banking along velocity, and occasional gliding periods.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class BirdFlightEntity : MonoBehaviour
    {
        [Header("Flight Speed & Direction")]
        [Tooltip("Horizontal speed in units per second.")]
        [SerializeField] private float flightSpeed = 4.2f;

        [Tooltip("True = flying left-to-right (+X), False = right-to-left (-X).")]
        [SerializeField] private bool flyRight = true;

        [Header("Natural Wave Motion")]
        [Tooltip("Amplitude of vertical bobbing during flight.")]
        [SerializeField] private float bobAmplitude = 0.35f;

        [Tooltip("Frequency of vertical bobbing in radians/sec.")]
        [SerializeField] private float bobFrequency = 2.8f;

        [Tooltip("Max pitch tilt angle in degrees when ascending or descending.")]
        [SerializeField] private float maxBankAngle = 14f;

        [Header("Gliding")]
        [Tooltip("Enable occasional gliding periods between flapping cycles.")]
        [SerializeField] private bool enableGliding = true;

        [SerializeField] private float minGlideInterval = 4f;
        [SerializeField] private float maxGlideInterval = 8f;
        [SerializeField] private float glideDuration = 1.8f;

        [Header("Boundaries & Cleanup")]
        [Tooltip("World X coordinate at which this bird is destroyed.")]
        [SerializeField] private float exitX = 120f;

        [Tooltip("Safety timeout in seconds before auto-destroy.")]
        [SerializeField] private float maxLifetime = 45f;

        private SpriteRenderer spriteRenderer;
        private Animator animator;

        private float spawnTime;
        private float startY;
        private float phaseOffset;
        private bool isGliding = false;
        private float nextGlideTime;
        private float glideEndTime;

        public void Initialize(bool moveRight, float speed, float startAlt, float targetExitX, float zDepth, float scale)
        {
            flyRight = moveRight;
            flightSpeed = speed;
            startY = startAlt;
            exitX = targetExitX;
            phaseOffset = Random.Range(0f, Mathf.PI * 2f);

            transform.position = new Vector3(transform.position.x, startAlt, zDepth);
            transform.localScale = Vector3.one * scale;

            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                // Source sprite faces right: flipX when flying left
                spriteRenderer.flipX = !flyRight;
            }

            ScheduleNextGlide();
        }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
            startY = transform.position.y;
            spawnTime = Time.time;
            phaseOffset = Random.Range(0f, Mathf.PI * 2f);
            ScheduleNextGlide();
        }

        private void ScheduleNextGlide()
        {
            if (!enableGliding) return;
            nextGlideTime = Time.time + Random.Range(minGlideInterval, maxGlideInterval);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            // Lifetime check
            if (Time.time - spawnTime > maxLifetime)
            {
                Destroy(gameObject);
                return;
            }

            // Check glide state
            if (enableGliding)
            {
                if (!isGliding && Time.time >= nextGlideTime)
                {
                    isGliding = true;
                    glideEndTime = Time.time + glideDuration;
                    if (animator != null) animator.speed = 0.15f; // Slow to near-glide pose
                }
                else if (isGliding && Time.time >= glideEndTime)
                {
                    isGliding = false;
                    ScheduleNextGlide();
                    if (animator != null) animator.speed = 1.0f; // Resume normal flapping
                }
            }

            // Horizontal motion
            float directionSign = flyRight ? 1f : -1f;
            float currentSpeed = isGliding ? flightSpeed * 1.15f : flightSpeed;
            float dx = directionSign * currentSpeed * dt;

            // Vertical bobbing (undulating wave mimicking bird flap & drop)
            float t = (Time.time - spawnTime) * bobFrequency + phaseOffset;
            float targetY = startY + Mathf.Sin(t) * (isGliding ? bobAmplitude * 0.4f : bobAmplitude);
            float dy = targetY - transform.position.y;

            // Apply position
            Vector3 pos = transform.position;
            pos.x += dx;
            pos.y += dy * Mathf.Clamp01(dt * 5f);
            transform.position = pos;

            // Pitch tilt banking into flight vector
            float slope = (dx != 0f) ? (dy / Mathf.Abs(dx)) : 0f;
            float targetAngle = Mathf.Clamp(Mathf.Rad2Deg * Mathf.Atan(slope) * 0.5f, -maxBankAngle, maxBankAngle);
            if (!flyRight) targetAngle = -targetAngle;
            transform.rotation = Quaternion.Euler(0f, 0f, targetAngle);

            // Exit boundary check
            if (flyRight && pos.x >= exitX)
            {
                Destroy(gameObject);
            }
            else if (!flyRight && pos.x <= exitX)
            {
                Destroy(gameObject);
            }
        }
    }
}
