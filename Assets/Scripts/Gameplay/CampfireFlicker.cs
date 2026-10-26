using UnityEngine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Subtle, organic light flicker for the interior campfire and oil lanterns.
    /// Uses Perlin noise for zero-allocation, natural warm light breathing.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class CampfireFlicker : MonoBehaviour
    {
        [SerializeField] private float minIntensity = 1.8f;
        [SerializeField] private float maxIntensity = 2.6f;
        [SerializeField] private float flickerSpeed = 4.2f;
        [SerializeField] private float positionJitter = 0.04f;

        private Light lightSource;
        private Vector3 initialLocalPos;
        private float seed;

        private void Awake()
        {
            lightSource = GetComponent<Light>();
            initialLocalPos = transform.localPosition;
            seed = Random.Range(0f, 100f);
        }

        private void Update()
        {
            if (lightSource == null) return;

            float t = Time.time * flickerSpeed + seed;
            float noise = Mathf.PerlinNoise(t, 0f);
            lightSource.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);

            if (positionJitter > 0f)
            {
                float xOffset = (Mathf.PerlinNoise(t, 10f) - 0.5f) * positionJitter;
                float zOffset = (Mathf.PerlinNoise(t, 20f) - 0.5f) * positionJitter;
                transform.localPosition = initialLocalPos + new Vector3(xOffset, 0f, zOffset);
            }
        }
    }
}