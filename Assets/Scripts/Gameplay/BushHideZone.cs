using UnityEngine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Defines a stealth bush hide zone.
    /// Reduces player visibility to enemy patrol. Standing inside gives partial cover; crouching gives full concealment.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class BushHideZone : MonoBehaviour
    {
        [Header("Concealment Factors")]
        [Range(0f, 1f)] [SerializeField] private float standingConcealment = 0.80f; // 80% hidden
        [Range(0f, 1f)] [SerializeField] private float crouchingConcealment = 1.00f; // 100% hidden (fully concealed)

        [Header("Interactive Foliage Physics")]
        [SerializeField] private float springStiffness = 38.0f;
        [SerializeField] private float springDamping = 7.5f;
        [SerializeField] private float impulseStrength = 14.0f;

        private PlayerController playerInside;
        private HoaVaMau.Test3D.Player3DController player3DInside;
        private Transform visualChild;
        private Vector3 initialChildScale = Vector3.one;
        private Quaternion initialChildRot = Quaternion.identity;
        private float currentWobbleAngle = 0f;
        private float wobbleVelocity = 0f;
        private ParticleSystem leafPuffSystem;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            visualChild = transform.Find("Foliage_Core");
            if (visualChild != null)
            {
                initialChildScale = visualChild.localScale;
                initialChildRot = visualChild.localRotation;
            }
            CreateLeafPuffParticleSystem();
        }

        private void CreateLeafPuffParticleSystem()
        {
            GameObject psGO = new GameObject("Leaf_Puff_FX");
            psGO.transform.SetParent(transform);
            psGO.transform.localPosition = new Vector3(0f, 0.7f, 0f);

            leafPuffSystem = psGO.AddComponent<ParticleSystem>();
            leafPuffSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = leafPuffSystem.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.40f, 0.75f, 0.25f, 0.9f), new Color(0.85f, 0.92f, 0.40f, 0.7f));
            main.gravityModifier = 0.15f;

            var emission = leafPuffSystem.emission;
            emission.rateOverTime = 0;

            var shape = leafPuffSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.5f;

            var renderer = psGO.GetComponent<ParticleSystemRenderer>();
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Sprites/Default");
            renderer.material = new Material(unlitShader) { color = new Color(0.55f, 0.85f, 0.35f, 0.85f) };
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0.0001f) return;

            // Damped harmonic spring physics for foliage rustle
            float springForce = -springStiffness * currentWobbleAngle;
            float dampingForce = -springDamping * wobbleVelocity;
            wobbleVelocity += (springForce + dampingForce) * dt;
            currentWobbleAngle += wobbleVelocity * dt;

            if (visualChild != null)
            {
                // Apply subtle tilt and squash-stretch during rustle
                visualChild.localRotation = initialChildRot * Quaternion.Euler(0f, 0f, currentWobbleAngle);
                float squash = 1.0f - Mathf.Abs(currentWobbleAngle) * 0.015f;
                visualChild.localScale = new Vector3(initialChildScale.x * (2f - squash), initialChildScale.y * squash, initialChildScale.z);
            }
        }

        public void TriggerRustle(float directionSign = 1f, float intensity = 1f)
        {
            wobbleVelocity += directionSign * impulseStrength * intensity;
            if (leafPuffSystem != null)
            {
                leafPuffSystem.Emit(Random.Range(5, 9));
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                playerInside = player;
                player.SetHidingInBush(true);
                float dir = player.FacingDirection != 0 ? player.FacingDirection : 1f;
                TriggerRustle(dir, 1.0f);
                return;
            }

            var p3d = other.GetComponentInParent<HoaVaMau.Test3D.Player3DController>();
            if (p3d != null)
            {
                player3DInside = p3d;
                p3d.SetHidingInBush(true);
                TriggerRustle(1f, 1.0f);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (playerInside != null)
            {
                // Full concealment when crouching inside bush
                playerInside.Concealment = playerInside.IsCrouching ? crouchingConcealment : standingConcealment;

                // Occasional subtle rustle if player is moving inside
                if (Mathf.Abs(playerInside.HorizontalInput) > 0.15f && Mathf.Abs(wobbleVelocity) < 2.0f)
                {
                    TriggerRustle(playerInside.FacingDirection, 0.45f);
                }
            }
            else if (player3DInside != null)
            {
                player3DInside.Concealment = player3DInside.IsCrouching ? crouchingConcealment : standingConcealment;
                if (player3DInside.PlanarSpeed > 0.25f && Mathf.Abs(wobbleVelocity) < 2.0f)
                {
                    TriggerRustle(Mathf.Sign(player3DInside.transform.forward.x + 0.01f), 0.45f);
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var player = other.GetComponentInParent<PlayerController>();
            if (player != null && player == playerInside)
            {
                player.SetHidingInBush(false);
                playerInside.Concealment = 0f;
                float dir = player.FacingDirection != 0 ? player.FacingDirection : 1f;
                TriggerRustle(dir, 0.75f);
                playerInside = null;
                return;
            }

            var p3d = other.GetComponentInParent<HoaVaMau.Test3D.Player3DController>();
            if (p3d != null && p3d == player3DInside)
            {
                p3d.SetHidingInBush(false);
                p3d.Concealment = 0f;
                TriggerRustle(1f, 0.75f);
                player3DInside = null;
            }
        }
    }
}
