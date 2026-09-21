using UnityEngine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Centralized Wind Manager providing unified wind vector, gust waves, and ambient motion
    /// across the rope, swaying trees, interactive grass, and atmospheric particle systems.
    /// </summary>
    public class WindManager : MonoBehaviour
    {
        public static WindManager Instance { get; private set; }

        [Header("Wind Parameters")]
        [SerializeField] private float baseSpeed = 1.2f;
        [SerializeField] private float baseStrength = 0.8f;
        [SerializeField] private float gustFrequency = 0.35f;
        [SerializeField] private float gustStrength = 0.6f;
        [SerializeField] private Vector3 windDirection = new Vector3(1f, 0f, 0f);

        // Global properties updated every frame
        public static float WindWave { get; private set; }
        public static Vector3 WindVector { get; private set; }
        public static Vector3 PlayerWorldPosition { get; set; }

        private static readonly int GlobalWindId = Shader.PropertyToID("_GlobalWind");

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (EnvironmentManager.Instance != null)
            {
                baseStrength = EnvironmentManager.Instance.CurrentWindStrength;
            }

            float t = Time.time * baseSpeed;
            float gust = Mathf.Sin(Time.time * gustFrequency) * gustStrength;
            float wave = (Mathf.Sin(t) + Mathf.Cos(t * 0.73f) * 0.5f + gust) * baseStrength;

            WindWave = wave;
            WindVector = windDirection.normalized * wave;

            // Wind owns only the wind globals.  GrassManager is the single owner of
            // _PlayerPosition; writing it here used the default origin before the player
            // was ready and raced the interaction manager every frame.
            Shader.SetGlobalVector(GlobalWindId, new Vector4(WindVector.x, WindVector.y, WindVector.z, wave));
        }

        public static float GetSampledWind(Vector3 worldPos, float frequency = 1.0f)
        {
            float phase = worldPos.x * 0.25f + worldPos.y * 0.15f;
            return Mathf.Sin(Time.time * (Instance != null ? Instance.baseSpeed : 1.2f) * frequency + phase);
        }
    }
}
