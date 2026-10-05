using UnityEngine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Interactive zone component attached to tall grass / stealth reeds.
    /// Emits floating prairie seed / pollen fluff particles and triggers reactive sway when characters wade through.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TallGrassInteractionZone : MonoBehaviour
    {
        private ParticleSystem reedFluffSystem;
        private float lastInteractionTime = 0f;
        private const float InteractionCooldown = 0.25f;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            CreateReedFluffSystem();
        }

        private void CreateReedFluffSystem()
        {
            GameObject psGO = new GameObject("TallGrass_SeedFluff_FX");
            psGO.transform.SetParent(transform);
            psGO.transform.localPosition = new Vector3(0f, 1.2f, 0f);

            reedFluffSystem = psGO.AddComponent<ParticleSystem>();
            var main = reedFluffSystem.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.6f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.09f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.95f, 0.95f, 0.80f, 0.85f), 
                new Color(0.85f, 0.95f, 0.65f, 0.75f)
            );
            main.gravityModifier = -0.04f; // Floats slightly upwards like dandelion fluff

            var velocityOverLifetime = reedFluffSystem.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(0.3f, 1.0f); // Drifts with wind
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(0.0f, 0.0f);
            velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(0.0f, 0.0f);

            var emission = reedFluffSystem.emission;
            emission.rateOverTime = 0;

            var shape = reedFluffSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(1.6f, 0.8f, 0.8f);

            var renderer = psGO.GetComponent<ParticleSystemRenderer>();
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Sprites/Default");
            renderer.material = new Material(unlitShader) { color = new Color(0.95f, 0.95f, 0.80f, 0.85f) };
        }

        [SerializeField] private bool enableFluffEmission = false;

        private void OnTriggerStay(Collider other)
        {
            if (!enableFluffEmission) return;
            if (Time.time - lastInteractionTime < InteractionCooldown) return;

            var player = other.GetComponent<PlayerController>();
            if (player != null && Mathf.Abs(player.HorizontalInput) > 0.1f)
            {
                lastInteractionTime = Time.time;
                if (reedFluffSystem != null)
                {
                    reedFluffSystem.Emit(Random.Range(2, 5));
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!enableFluffEmission) return;
            var player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                lastInteractionTime = Time.time;
                if (reedFluffSystem != null)
                {
                    reedFluffSystem.Emit(Random.Range(4, 7));
                }
            }
        }
    }
}
