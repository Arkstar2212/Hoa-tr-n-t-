using UnityEngine;

namespace HoaVaMau.Environment
{
    /// <summary>
    /// Controls the stylized water surface shader and animation parameters.
    /// Exposes intuitive Inspector knobs for wave frequency, ripple distortion, colors, and sunset reflection.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Renderer))]
    public class WaterController : MonoBehaviour
    {
        [Header("Sun Specular Alignment")]
        [Tooltip("Transform of the Sun to dynamically align reflection highlight column")]
        [SerializeField] private Transform sunTransform;
        [SerializeField] private float manualSunWorldX = 0f;

        [Header("Gradient Colors")]
        [SerializeField] private Color shallowColor = new Color(0.16f, 0.42f, 0.48f, 1.0f);
        [SerializeField] private Color deepColor = new Color(0.08f, 0.20f, 0.30f, 1.0f);
        [SerializeField] private Color horizonWaterColor = new Color(0.62f, 0.38f, 0.26f, 1.0f);
        [Range(0.2f, 4.0f)]
        [SerializeField] private float gradientPower = 1.2f;
        [SerializeField] private float nearZ = 0.0f;
        [SerializeField] private float farZ = 25.0f;

        [Header("Sunset Reflection")]
        [SerializeField] private Color sunReflectionColor = new Color(1.0f, 0.82f, 0.55f, 1.0f);
        [Range(0.2f, 10.0f)]
        [SerializeField] private float reflectionWidth = 3.0f;
        [Range(0.0f, 5.0f)]
        [SerializeField] private float reflectionIntensity = 2.0f;
        [Range(0.5f, 5.0f)]
        [SerializeField] private float reflectionFalloff = 2.0f;

        [Header("Wave & Ripple Procedural Movement")]
        [SerializeField] private float waveSpeed = 1.2f;
        [SerializeField] private float waveFrequency = 1.5f;
        [Range(0.0f, 0.2f)]
        [SerializeField] private float waveHeight = 0.02f;
        [SerializeField] private float rippleScale = 6.0f;
        [SerializeField] private float rippleSpeed = 1.0f;
        [Range(0.0f, 1.0f)]
        [SerializeField] private float rippleStrength = 0.45f;

        [Header("Foam Edge")]
        [SerializeField] private Color foamColor = new Color(0.95f, 0.98f, 1.0f, 0.9f);
        [Range(0.0f, 0.3f)]
        [SerializeField] private float foamDepth = 0.08f;

        private Renderer waterRenderer;
        private MaterialPropertyBlock propBlock;

        // Shader property IDs
        private static readonly int ShallowColorID = Shader.PropertyToID("_ShallowColor");
        private static readonly int DeepColorID = Shader.PropertyToID("_DeepColor");
        private static readonly int HorizonWaterColorID = Shader.PropertyToID("_HorizonWaterColor");
        private static readonly int GradientPowerID = Shader.PropertyToID("_GradientPower");
        private static readonly int NearZID = Shader.PropertyToID("_NearZ");
        private static readonly int FarZID = Shader.PropertyToID("_FarZ");
        private static readonly int SunReflectionColorID = Shader.PropertyToID("_SunReflectionColor");
        private static readonly int SunWorldXID = Shader.PropertyToID("_SunWorldX");
        private static readonly int ReflectionWidthID = Shader.PropertyToID("_ReflectionWidth");
        private static readonly int ReflectionIntensityID = Shader.PropertyToID("_ReflectionIntensity");
        private static readonly int ReflectionFalloffID = Shader.PropertyToID("_ReflectionFalloff");
        private static readonly int WaveSpeedID = Shader.PropertyToID("_WaveSpeed");
        private static readonly int WaveFrequencyID = Shader.PropertyToID("_WaveFrequency");
        private static readonly int WaveHeightID = Shader.PropertyToID("_WaveHeight");
        private static readonly int RippleScaleID = Shader.PropertyToID("_RippleScale");
        private static readonly int RippleSpeedID = Shader.PropertyToID("_RippleSpeed");
        private static readonly int RippleStrengthID = Shader.PropertyToID("_RippleStrength");
        private static readonly int FoamColorID = Shader.PropertyToID("_FoamColor");
        private static readonly int FoamDepthID = Shader.PropertyToID("_FoamDepth");

        private void Awake()
        {
            waterRenderer = GetComponent<Renderer>();
            propBlock = new MaterialPropertyBlock();
            ApplyProperties();
        }

        private void Update()
        {
            ApplyProperties();
        }

        private void OnValidate()
        {
            if (waterRenderer == null) waterRenderer = GetComponent<Renderer>();
            if (propBlock == null) propBlock = new MaterialPropertyBlock();
            ApplyProperties();
        }

        public void ApplyProperties()
        {
            if (waterRenderer == null) return;
            if (propBlock == null) propBlock = new MaterialPropertyBlock();

            waterRenderer.GetPropertyBlock(propBlock);

            float sunX = (sunTransform != null) ? sunTransform.position.x : manualSunWorldX;

            propBlock.SetColor(ShallowColorID, shallowColor);
            propBlock.SetColor(DeepColorID, deepColor);
            propBlock.SetColor(HorizonWaterColorID, horizonWaterColor);
            propBlock.SetFloat(GradientPowerID, gradientPower);
            propBlock.SetFloat(NearZID, nearZ);
            propBlock.SetFloat(FarZID, farZ);

            propBlock.SetColor(SunReflectionColorID, sunReflectionColor);
            propBlock.SetFloat(SunWorldXID, sunX);
            propBlock.SetFloat(ReflectionWidthID, reflectionWidth);
            propBlock.SetFloat(ReflectionIntensityID, reflectionIntensity);
            propBlock.SetFloat(ReflectionFalloffID, reflectionFalloff);

            propBlock.SetFloat(WaveSpeedID, waveSpeed);
            propBlock.SetFloat(WaveFrequencyID, waveFrequency);
            propBlock.SetFloat(WaveHeightID, waveHeight);
            propBlock.SetFloat(RippleScaleID, rippleScale);
            propBlock.SetFloat(RippleSpeedID, rippleSpeed);
            propBlock.SetFloat(RippleStrengthID, rippleStrength);

            propBlock.SetColor(FoamColorID, foamColor);
            propBlock.SetFloat(FoamDepthID, foamDepth);

            waterRenderer.SetPropertyBlock(propBlock);
        }

        public void SetSunTransform(Transform t)
        {
            sunTransform = t;
            ApplyProperties();
        }

        public void SetColors(Color shallow, Color deep, Color horizon, Color reflection)
        {
            shallowColor = shallow;
            deepColor = deep;
            horizonWaterColor = horizon;
            sunReflectionColor = reflection;
            ApplyProperties();
        }
    }
}
