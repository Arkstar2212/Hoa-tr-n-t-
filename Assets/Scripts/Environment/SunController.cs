using UnityEngine;

namespace HoaVaMau.Environment
{
    /// <summary>
    /// Controls the glowing sun object, inner core, outer corona glow, and subtle cinematic breathing.
    /// Exposes all parameters to the Inspector.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Renderer))]
    public class SunController : MonoBehaviour
    {
        [Header("Sun Colors (HDR)")]
        [ColorUsage(true, true)]
        [SerializeField] private Color coreColor = new Color(2.5f, 2.2f, 1.6f, 1f);
        [ColorUsage(true, true)]
        [SerializeField] private Color glowColor = new Color(1.8f, 0.7f, 0.15f, 1f);
        [SerializeField] private Color horizonHazeColor = new Color(0.95f, 0.5f, 0.25f, 0.5f);

        [Header("Radii & Softness")]
        [Range(0.05f, 0.6f)]
        [SerializeField] private float coreRadius = 0.22f;
        [Range(0.01f, 0.4f)]
        [SerializeField] private float coreSoftness = 0.08f;
        [Range(0.2f, 1.0f)]
        [SerializeField] private float glowRadius = 0.85f;
        [Range(0.5f, 5.0f)]
        [SerializeField] private float glowFalloff = 2.2f;

        [Header("Horizon Atmospheric Absorption")]
        [Range(0.0f, 0.5f)]
        [SerializeField] private float horizonFadeHeight = 0.15f;
        [Range(0.01f, 0.4f)]
        [SerializeField] private float horizonFadeSoftness = 0.12f;

        [Header("Cinematic Breathing Pulse")]
        [SerializeField] private bool enablePulse = true;
        [SerializeField] private float pulseSpeed = 0.5f;
        [Range(0.0f, 0.2f)]
        [SerializeField] private float pulseAmplitude = 0.03f;

        private Renderer sunRenderer;
        private MaterialPropertyBlock propBlock;

        private static readonly int CoreColorID = Shader.PropertyToID("_CoreColor");
        private static readonly int GlowColorID = Shader.PropertyToID("_GlowColor");
        private static readonly int HorizonHazeColorID = Shader.PropertyToID("_HorizonHazeColor");
        private static readonly int CoreRadiusID = Shader.PropertyToID("_CoreRadius");
        private static readonly int CoreSoftnessID = Shader.PropertyToID("_CoreSoftness");
        private static readonly int GlowRadiusID = Shader.PropertyToID("_GlowRadius");
        private static readonly int GlowFalloffID = Shader.PropertyToID("_GlowFalloff");
        private static readonly int HorizonFadeHeightID = Shader.PropertyToID("_HorizonFadeHeight");
        private static readonly int HorizonFadeSoftnessID = Shader.PropertyToID("_HorizonFadeSoftness");

        private void Awake()
        {
            sunRenderer = GetComponent<Renderer>();
            propBlock = new MaterialPropertyBlock();
            ApplyProperties();
        }

        private void Update()
        {
            ApplyProperties();
        }

        private void OnValidate()
        {
            if (sunRenderer == null) sunRenderer = GetComponent<Renderer>();
            if (propBlock == null) propBlock = new MaterialPropertyBlock();
            ApplyProperties();
        }

        public void ApplyProperties()
        {
            if (sunRenderer == null) return;
            if (propBlock == null) propBlock = new MaterialPropertyBlock();

            sunRenderer.GetPropertyBlock(propBlock);

            float time = Application.isPlaying ? Time.time : (float)Time.realtimeSinceStartup;
            float pulse = enablePulse ? Mathf.Sin(time * pulseSpeed) * pulseAmplitude : 0f;

            propBlock.SetColor(CoreColorID, coreColor * (1.0f + pulse));
            propBlock.SetColor(GlowColorID, glowColor * (1.0f + pulse * 0.5f));
            propBlock.SetColor(HorizonHazeColorID, horizonHazeColor);
            propBlock.SetFloat(CoreRadiusID, Mathf.Clamp01(coreRadius + pulse * 0.5f));
            propBlock.SetFloat(CoreSoftnessID, coreSoftness);
            propBlock.SetFloat(GlowRadiusID, Mathf.Clamp01(glowRadius + pulse));
            propBlock.SetFloat(GlowFalloffID, glowFalloff);
            propBlock.SetFloat(HorizonFadeHeightID, horizonFadeHeight);
            propBlock.SetFloat(HorizonFadeSoftnessID, horizonFadeSoftness);

            sunRenderer.SetPropertyBlock(propBlock);
        }

        public void SetColors(Color core, Color glow)
        {
            coreColor = core;
            glowColor = glow;
            ApplyProperties();
        }
    }
}
