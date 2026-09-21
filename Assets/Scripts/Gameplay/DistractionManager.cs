using UnityEngine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Centralized Distraction & Noise Propagation Manager.
    /// Handles stone throwing, noise wave dispatching to enemy AI, and ripple effects.
    /// </summary>
    public class DistractionManager : MonoBehaviour
    {
        private static DistractionManager _instance;
        public static DistractionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Object.FindAnyObjectByType<DistractionManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("DistractionManager");
                        _instance = go.AddComponent<DistractionManager>();
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Distraction Settings")]
        [SerializeField] private float defaultNoiseRadius = 9.0f;
        [SerializeField] private Material pebbleMaterial;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            EnsurePebbleMaterial();
        }

        private void EnsurePebbleMaterial()
        {
            if (pebbleMaterial == null)
            {
                Shader lit = Shader.Find("Universal Render Pipeline/Lit") 
                          ?? Shader.Find("Standard") 
                          ?? Shader.Find("Diffuse");
                pebbleMaterial = new Material(lit) { name = "Mat_DistractionPebble", color = new Color(0.48f, 0.45f, 0.40f) };
            }
        }

        /// <summary>
        /// Emits a noise distraction wave at the given location, alerting all enemies within radius.
        /// </summary>
        public void EmitDistractionNoise(Vector3 noisePosition, float radius = -1f)
        {
            if (radius <= 0f) radius = defaultNoiseRadius;
            var enemies = Object.FindObjectsByType<EnemyController>();
            int alertedCount = 0;
            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;
                float dist = Vector3.Distance(enemy.transform.position, noisePosition);
                if (dist <= radius)
                {
                    enemy.OnDistractionNoise(noisePosition, radius);
                    alertedCount++;
                }
            }

            Debug.Log($"<color=#f59e0b>[DistractionManager]</color> Noise emitted at {noisePosition:F1} (R: {radius:F1}m), alerted {alertedCount} enemies.");
        }

        /// <summary>
        /// Spawns and launches a distraction pebble from origin with the given velocity.
        /// </summary>
        public GameObject SpawnPebble(Vector3 origin, Vector3 initialVelocity, float radius = -1f)
        {
            if (radius <= 0f) radius = defaultNoiseRadius;
            EnsurePebbleMaterial();

            GameObject pebbleGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pebbleGO.name = "Pebble_Distraction";
            pebbleGO.transform.position = origin;
            pebbleGO.transform.localScale = new Vector3(0.18f, 0.18f, 0.18f);

            var mr = pebbleGO.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial = pebbleMaterial;

            // Remove default collider so it doesn't push physics entities; DistractionPebble does raycast collision
            var col = pebbleGO.GetComponent<Collider>();
            if (col != null) Destroy(col);

            DistractionPebble pebble = pebbleGO.AddComponent<DistractionPebble>();
            pebble.Launch(initialVelocity, radius);

            return pebbleGO;
        }
    }
}
