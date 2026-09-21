using UnityEngine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Distraction pebble projectile thrown by the player.
    /// Traverses in an arc, impacts the ground, emits a sound distraction wave to nearby enemies,
    /// and spawns a visual sound ripple ring.
    /// </summary>
    public class DistractionPebble : MonoBehaviour
    {
        [Header("Distraction Parameters")]
        [SerializeField] private float noiseRadius = 9.0f;
        [SerializeField] private float gravity = 20f;
        [SerializeField] private LayerMask collisionMask = ~0;

        private Vector3 velocity;
        private bool hasLanded = false;
        private float lifeTimer = 0f;

        public void Launch(Vector3 initialVelocity, float radius = 9.0f)
        {
            velocity = initialVelocity;
            noiseRadius = radius;
            hasLanded = false;
            lifeTimer = 0f;
        }

        private void Update()
        {
            if (hasLanded)
            {
                lifeTimer += Time.deltaTime;
                if (lifeTimer >= 1.5f)
                {
                    Destroy(gameObject);
                }
                return;
            }

            float dt = Time.deltaTime;
            velocity.y -= gravity * dt;
            Vector3 step = velocity * dt;
            float stepDist = step.magnitude;

            if (stepDist > 0.001f)
            {
                // Raycast ahead to detect precise ground / platform impact
                if (Physics.Raycast(transform.position, step.normalized, out RaycastHit hit, stepDist + 0.12f, collisionMask, QueryTriggerInteraction.Ignore))
                {
                    // Ignore collision with player itself
                    if (hit.collider.GetComponentInParent<PlayerController>() == null)
                    {
                        OnImpact(hit.point, hit.normal);
                        return;
                    }
                }
            }

            transform.position += step;
            transform.Rotate(Vector3.forward, 360f * dt);

            // Failsafe lifetime (5 seconds in flight)
            lifeTimer += dt;
            if (lifeTimer >= 5.0f)
            {
                Destroy(gameObject);
            }
        }

        private void OnImpact(Vector3 impactPoint, Vector3 normal)
        {
            hasLanded = true;
            transform.position = impactPoint + normal * 0.05f;

            // Spawn visual sound ripple ring
            GameObject rippleGO = new GameObject("Distraction_Ripple");
            rippleGO.transform.position = impactPoint;
            DistractionNoiseRipple ripple = rippleGO.AddComponent<DistractionNoiseRipple>();
            ripple.Initialize(noiseRadius, 0.85f, new Color(1.0f, 0.82f, 0.35f, 0.85f));

            // Notify global distraction manager
            if (DistractionManager.Instance != null)
            {
                DistractionManager.Instance.EmitDistractionNoise(impactPoint, noiseRadius);
            }

            // Play small audio click / thump if audio source is present
            var audio = GetComponent<AudioSource>();
            if (audio != null) audio.Play();
        }
    }
}
