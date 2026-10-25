using UnityEngine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Controls the living water surface overlay in the Cà Mau ocean:
    /// - Scrolling caustic ripples across the turquoise sea (sóng lăn tăn)
    /// - Pulsating sunlight specular sparkles (ánh sáng mặt trời lấp lánh trên sóng nước)
    /// - Subtle vertical wave breathing
    /// </summary>
    public class LivingOceanWaterShimmer : MonoBehaviour
    {
        [Header("Water Shimmer Settings")]
        [SerializeField] private SpriteRenderer shimmerRenderer;
        [SerializeField] private float scrollSpeedX = 0.035f;
        [SerializeField] private float waveBreatheY = 0.015f;
        [SerializeField] private float waveBreatheSpeed = 0.65f;
        [SerializeField] private float sparkleFrequency = 1.8f;
        [SerializeField] private float baseAlpha = 0.45f;

        private Material shimmerMat;
        private Vector3 initialPos;
        private Vector2 uvOffset = Vector2.zero;

        public void Configure(SpriteRenderer renderer)
        {
            shimmerRenderer = renderer;
        }

        private void Awake()
        {
            if (shimmerRenderer == null) shimmerRenderer = GetComponent<SpriteRenderer>();
            initialPos = transform.localPosition;

            if (shimmerRenderer != null)
            {
                // Instantiate unique material instance for UV scrolling
                shimmerMat = new Material(shimmerRenderer.sharedMaterial);
                shimmerRenderer.material = shimmerMat;
            }
        }

        private void Update()
        {
            float t = Time.time;

            // 1. Scroll UV across the water surface
            uvOffset.x += scrollSpeedX * Time.deltaTime;
            if (uvOffset.x > 1f) uvOffset.x -= 1f;

            if (shimmerMat != null)
            {
                if (shimmerMat.HasProperty("_BaseMap")) shimmerMat.SetTextureOffset("_BaseMap", uvOffset);
                else shimmerMat.mainTextureOffset = uvOffset;
            }

            // 2. Vertical wave breathing (sóng nhấp nhô)
            float waveY = Mathf.Sin(t * waveBreatheSpeed * 2f * Mathf.PI) * waveBreatheY;
            transform.localPosition = initialPos + new Vector3(0f, waveY, 0f);

            // 3. Specular sunlight sparkle pulsation
            if (shimmerRenderer != null)
            {
                float sparkle = Mathf.Sin(t * sparkleFrequency * 2f * Mathf.PI);
                float sparkle2 = Mathf.Cos(t * 3.1f) * 0.35f;
                float alpha = Mathf.Clamp01(baseAlpha + (sparkle + sparkle2) * 0.18f);

                Color c = shimmerRenderer.color;
                c.a = alpha;
                shimmerRenderer.color = c;
            }
        }

        private void OnDestroy()
        {
            if (shimmerMat != null) Destroy(shimmerMat);
        }
    }
}
