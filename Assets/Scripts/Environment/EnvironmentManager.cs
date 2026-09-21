using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HoaVaMau
{
    public enum TimeOfDayState
    {
        Dawn,
        Day,
        Dusk,
        Night
    }

    public enum WeatherState
    {
        ClearBreeze,
        PollenBurst,
        RainShower,
        GaleStorm
    }

    /// <summary>
    /// Centralized Atmospheric & Environment Hub for "Hoa và Máu" (Flower and Blood).
    /// Drives unified Time-of-Day atmospheric lighting across Shaders, Sprites, Directional Sun/Moon, VFX, and Camera.
    /// Operates in both Edit Mode ([ExecuteAlways]) and Play Mode for real-time visual feedback.
    /// </summary>
    [ExecuteAlways]
    public class EnvironmentManager : MonoBehaviour
    {
        public static EnvironmentManager Instance { get; private set; }

        [System.Serializable]
        public struct AtmosphereProfile
        {
            public string name;
            [Header("Sky Dome Gradient")]
            public Color skyZenithColor;
            public Color skyHorizonColor;
            public Color horizonHazeColor;

            [Header("Directional Sun / Moon Light")]
            public Color sunLightColor;
            public float sunLightIntensity;
            public Vector3 sunRotationEuler;

            [Header("Cloud Volumetric Lighting")]
            public Color cloudAtmosphereTint;
            public Color cloudRimColor;
            public Color cloudShadowTint;

            [Header("Karst Mountains & Terrain")]
            public Color mountainFarColor;
            public Color mountainMidColor;
            public Color cliffPlateauColor;
            public Color grassBaseColor;
            public Color grassTipColor;

            [Header("Celestial & VFX")]
            public Color particleColor;
            public float celestialAlpha;
            public Color celestialTint;
        }

        [Header("Active State")]
        [SerializeField] private TimeOfDayState currentTimeOfDay = TimeOfDayState.Day;
        [SerializeField] private WeatherState currentWeather = WeatherState.ClearBreeze;

        public TimeOfDayState CurrentTimeOfDay => currentTimeOfDay;
        public WeatherState CurrentWeather => currentWeather;

        [Range(0f, 3.5f)] public float targetWindStrength = 0.85f;
        [Range(0f, 2f)] public float targetCloudSpeed = 0.35f;

        public float CurrentWindStrength => currentWindStrength;

        [Header("Transition Smoothing")]
        [SerializeField] private float transitionSpeed = 2.5f;

        [Header("Atmosphere Profiles")]
        [SerializeField] private AtmosphereProfile dawnProfile;
        [SerializeField] private AtmosphereProfile dayProfile;
        [SerializeField] private AtmosphereProfile duskProfile;
        [SerializeField] private AtmosphereProfile nightProfile;

        [Header("Lighting & Scene Renderers")]
        [SerializeField] private Light directionalLight;
        [SerializeField] private Renderer skyRenderer;
        [SerializeField] private SpriteRenderer celestialRingRenderer;
        [SerializeField] private SpriteRenderer farMountainRenderer;
        [SerializeField] private SpriteRenderer midMountainRenderer;
        [SerializeField] private SpriteRenderer lakeRenderer;
        [SerializeField] private SpriteRenderer stiltRenderer;
        [SerializeField] private SpriteRenderer cliffPlateauRenderer;

        [Header("VFX & Camera")]
        [SerializeField] private ParticleSystem ambientParticleSystem;
        [SerializeField] private Transform cameraRig;
        [SerializeField] private float galeThreshold = 2.2f;

        // Current interpolated values
        private float currentWindStrength = 0.85f;
        private float currentCloudSpeed = 0.35f;
        private Color currentSkyZenith;
        private Color currentSkyHorizon;
        private Color currentHazeColor;
        private Color currentAtmoTint;
        private Color currentRimColor;
        private Color currentShadowTint;
        private Color currentFarMountain;
        private Color currentMidMountain;
        private Color currentCliffColor;
        private Color currentGrassBase;
        private Color currentGrassTip;
        private Color currentParticleColor;
        private float currentCelestialAlpha;
        private Color currentCelestialTint;

        private Vector3 originalCameraLocalPos;

        // Shader property IDs
        private static readonly int GlobalWindId = Shader.PropertyToID("_GlobalWindStrength");
        private static readonly int GlobalCloudSpeedId = Shader.PropertyToID("_GlobalCloudSpeed");
        private static readonly int GlobalAtmoTintId = Shader.PropertyToID("_GlobalAtmosphereTint");
        private static readonly int GlobalRimColorId = Shader.PropertyToID("_GlobalRimColor");
        private static readonly int GlobalShadowTintId = Shader.PropertyToID("_GlobalShadowTint");
        private static readonly int GlobalHorizonHazeId = Shader.PropertyToID("_GlobalHorizonHaze");
        private static readonly int GlobalGrassBaseId = Shader.PropertyToID("_GlobalGrassBase");
        private static readonly int GlobalGrassTipId = Shader.PropertyToID("_GlobalGrassTip");
        private static readonly int GlobalSkyZenithId = Shader.PropertyToID("_GlobalSkyZenith");
        private static readonly int GlobalSkyHorizonId = Shader.PropertyToID("_GlobalSkyHorizon");

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                if (Application.isPlaying)
                {
                    Destroy(gameObject);
                    return;
                }
            }
            Instance = this;

            AutoFindReferences();

            if (cameraRig != null) originalCameraLocalPos = cameraRig.localPosition;

            InitializeDefaultProfiles();
            SetInstantValues(GetProfile(currentTimeOfDay));
        }

        private void OnEnable()
        {
            Instance = this;
            AutoFindReferences();
            InitializeDefaultProfiles();
            SetInstantValues(GetProfile(currentTimeOfDay));
            PushGlobalShaderVariables();
        }

        private void AutoFindReferences()
        {
            if (directionalLight == null)
            {
                GameObject lightGO = GameObject.Find("Directional Light");
                if (lightGO != null) directionalLight = lightGO.GetComponent<Light>();
                if (directionalLight == null) directionalLight = Object.FindAnyObjectByType<Light>();
            }

            if (ambientParticleSystem == null)
            {
                GameObject pGO = GameObject.Find("Atmospheric_Pollen_Particles");
                if (pGO != null) ambientParticleSystem = pGO.GetComponent<ParticleSystem>();
                if (ambientParticleSystem == null) ambientParticleSystem = Object.FindAnyObjectByType<ParticleSystem>();
            }

            if (skyRenderer == null)
            {
                GameObject skyGO = GameObject.Find("Sky_Backdrop");
                if (skyGO != null) skyRenderer = skyGO.GetComponent<Renderer>();
            }

            if (cameraRig == null && Camera.main != null)
            {
                cameraRig = Camera.main.transform;
                originalCameraLocalPos = cameraRig.localPosition;
            }
        }

        public void SetTimeOfDay(TimeOfDayState state)
        {
            currentTimeOfDay = state;
            Debug.Log($"<color=#40b8a6>[Hoa và Máu Environment]</color> Time of Day: {currentTimeOfDay}");
        }

        public void CycleTimeOfDay()
        {
            int next = ((int)currentTimeOfDay + 1) % 4;
            SetTimeOfDay((TimeOfDayState)next);
        }

        public void SetWeather(WeatherState state)
        {
            currentWeather = state;
            switch (state)
            {
                case WeatherState.ClearBreeze:
                    targetWindStrength = 0.85f;
                    targetCloudSpeed = 0.35f;
                    break;
                case WeatherState.PollenBurst:
                    targetWindStrength = 1.25f;
                    targetCloudSpeed = 0.50f;
                    break;
                case WeatherState.RainShower:
                    targetWindStrength = 1.80f;
                    targetCloudSpeed = 0.85f;
                    break;
                case WeatherState.GaleStorm:
                    targetWindStrength = 2.65f;
                    targetCloudSpeed = 1.50f;
                    break;
            }
            Debug.Log($"<color=#40b8a6>[Hoa và Máu Environment]</color> Weather changed to: {currentWeather} (Target Wind: {targetWindStrength:F2})");
        }

        public void CycleWeather()
        {
            int next = ((int)currentWeather + 1) % 4;
            SetWeather((WeatherState)next);
        }

        public void ToggleWindBoost()
        {
            if (currentWindStrength > 2.0f)
            {
                SetWeather(WeatherState.ClearBreeze);
            }
            else
            {
                SetWeather(WeatherState.GaleStorm);
            }
        }

        private void Update()
        {
            float dt = Application.isPlaying ? Time.deltaTime : 0.033f;

            if (Application.isPlaying)
            {
                HandleInput();
            }

            AtmosphereProfile target = GetProfile(currentTimeOfDay);

            // Interpolate wind & cloud drift
            currentWindStrength = Mathf.Lerp(currentWindStrength, targetWindStrength, dt * 3.0f);
            currentCloudSpeed = Mathf.Lerp(currentCloudSpeed, targetCloudSpeed, dt * 3.0f);

            // Interpolate unified atmospheric colors
            float t = Mathf.Clamp01(dt * transitionSpeed);
            currentSkyZenith = Color.Lerp(currentSkyZenith, target.skyZenithColor, t);
            currentSkyHorizon = Color.Lerp(currentSkyHorizon, target.skyHorizonColor, t);
            currentHazeColor = Color.Lerp(currentHazeColor, target.horizonHazeColor, t);
            currentAtmoTint = Color.Lerp(currentAtmoTint, target.cloudAtmosphereTint, t);
            currentRimColor = Color.Lerp(currentRimColor, target.cloudRimColor, t);
            currentShadowTint = Color.Lerp(currentShadowTint, target.cloudShadowTint, t);
            currentFarMountain = Color.Lerp(currentFarMountain, target.mountainFarColor, t);
            currentMidMountain = Color.Lerp(currentMidMountain, target.mountainMidColor, t);
            currentCliffColor = Color.Lerp(currentCliffColor, target.cliffPlateauColor, t);
            currentGrassBase = Color.Lerp(currentGrassBase, target.grassBaseColor, t);
            currentGrassTip = Color.Lerp(currentGrassTip, target.grassTipColor, t);
            currentParticleColor = Color.Lerp(currentParticleColor, target.particleColor, t);
            currentCelestialAlpha = Mathf.Lerp(currentCelestialAlpha, target.celestialAlpha, t);
            currentCelestialTint = Color.Lerp(currentCelestialTint, target.celestialTint, t);

            // Interpolate Directional Sun/Moon Light
            if (directionalLight != null)
            {
                directionalLight.color = Color.Lerp(directionalLight.color, target.sunLightColor, t);

                // Dim light during rain or heavy storm
                float weatherDim = 1.0f;
                if (currentWeather == WeatherState.RainShower) weatherDim = 0.70f;
                else if (currentWeather == WeatherState.GaleStorm) weatherDim = 0.55f;

                float targetIntensity = target.sunLightIntensity * weatherDim;
                directionalLight.intensity = Mathf.Lerp(directionalLight.intensity, targetIntensity, t);

                Quaternion targetRot = Quaternion.Euler(target.sunRotationEuler);
                directionalLight.transform.rotation = Quaternion.Slerp(directionalLight.transform.rotation, targetRot, t);
            }

            // Push to shaders
            PushGlobalShaderVariables();

            // Update Sky Renderer
            if (skyRenderer != null)
            {
                Color blendedSky = Color.Lerp(currentSkyHorizon, currentSkyZenith, 0.45f);
                if (skyRenderer is SpriteRenderer sr)
                {
                    sr.color = blendedSky;
                }
                else if (skyRenderer.sharedMaterial != null)
                {
                    if (skyRenderer.sharedMaterial.HasProperty("_BaseColor"))
                        skyRenderer.sharedMaterial.SetColor("_BaseColor", blendedSky);
                    else
                        skyRenderer.sharedMaterial.color = blendedSky;
                }
            }

            if (celestialRingRenderer != null)
            {
                Color ringCol = currentCelestialTint;
                ringCol.a = currentCelestialAlpha;
                celestialRingRenderer.color = ringCol;
            }
            if (farMountainRenderer != null) farMountainRenderer.color = currentFarMountain;
            if (midMountainRenderer != null) midMountainRenderer.color = currentMidMountain;
            if (lakeRenderer != null) lakeRenderer.color = currentMidMountain;
            if (stiltRenderer != null) stiltRenderer.color = currentCliffColor;
            if (cliffPlateauRenderer != null) cliffPlateauRenderer.color = currentCliffColor;

            // Update VFX & Camera
            UpdateAmbientParticles();
            UpdateGaleCameraShake(dt);
        }

        private void HandleInput()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.f2Key.wasPressedThisFrame) CycleTimeOfDay();
                if (kb.f3Key.wasPressedThisFrame) CycleWeather();
                if (kb.f4Key.wasPressedThisFrame) ToggleWindBoost();
            }
#else
            if (Input.GetKeyDown(KeyCode.F2)) CycleTimeOfDay();
            if (Input.GetKeyDown(KeyCode.F3)) CycleWeather();
            if (Input.GetKeyDown(KeyCode.F4)) ToggleWindBoost();
#endif
        }

        private void PushGlobalShaderVariables()
        {
            Shader.SetGlobalFloat(GlobalWindId, currentWindStrength);
            Shader.SetGlobalFloat(GlobalCloudSpeedId, currentCloudSpeed);
            Shader.SetGlobalColor(GlobalAtmoTintId, currentAtmoTint);
            Shader.SetGlobalColor(GlobalRimColorId, currentRimColor);
            Shader.SetGlobalColor(GlobalShadowTintId, currentShadowTint);
            Shader.SetGlobalColor(GlobalHorizonHazeId, currentHazeColor);
            Shader.SetGlobalColor(GlobalGrassBaseId, currentGrassBase);
            Shader.SetGlobalColor(GlobalGrassTipId, currentGrassTip);
            Shader.SetGlobalColor(GlobalSkyZenithId, currentSkyZenith);
            Shader.SetGlobalColor(GlobalSkyHorizonId, currentSkyHorizon);
        }

        private void UpdateAmbientParticles()
        {
            if (ambientParticleSystem == null) return;
            var main = ambientParticleSystem.main;

            Color pCol = currentParticleColor;
            if (currentWeather == WeatherState.RainShower)
            {
                pCol = new Color(0.72f, 0.85f, 0.98f, 0.70f);
                main.startSize = 0.05f;
                main.startSpeed = 8.0f;
            }
            else if (currentWeather == WeatherState.GaleStorm)
            {
                pCol = Color.Lerp(currentParticleColor, new Color(0.70f, 0.72f, 0.65f, 0.80f), 0.5f);
                main.startSize = 0.09f;
                main.startSpeed = 4.5f;
            }
            else
            {
                main.startSize = 0.08f;
                main.startSpeed = 0.35f;
            }
            main.startColor = pCol;

            var vel = ambientParticleSystem.velocityOverLifetime;
            if (vel.enabled)
            {
                if (currentWeather == WeatherState.RainShower)
                {
                    vel.x = new ParticleSystem.MinMaxCurve(-3.5f * currentWindStrength, -1.8f * currentWindStrength);
                    vel.y = new ParticleSystem.MinMaxCurve(-12f, -8f);
                    vel.z = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
                }
                else if (currentWeather == WeatherState.GaleStorm)
                {
                    vel.x = new ParticleSystem.MinMaxCurve(-6.5f * currentWindStrength, -3.8f * currentWindStrength);
                    vel.y = new ParticleSystem.MinMaxCurve(-1.8f, 0.8f);
                    vel.z = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
                }
                else
                {
                    float driftXMin = -1.5f * currentWindStrength;
                    float driftXMax = -0.4f * currentWindStrength;
                    vel.x = new ParticleSystem.MinMaxCurve(driftXMin, driftXMax);
                    vel.y = new ParticleSystem.MinMaxCurve(0.35f, 0.95f);
                    vel.z = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);
                }
            }

            var emission = ambientParticleSystem.emission;
            float baseRate = 14f;
            if (currentWeather == WeatherState.PollenBurst) baseRate = 38f;
            else if (currentWeather == WeatherState.RainShower) baseRate = 70f;
            else if (currentWeather == WeatherState.GaleStorm) baseRate = 100f;
            emission.rateOverTime = baseRate * Mathf.Clamp(currentWindStrength, 0.6f, 2.5f);
        }

        private void UpdateGaleCameraShake(float dt)
        {
            if (cameraRig == null || !Application.isPlaying) return;
            if (currentWindStrength > galeThreshold)
            {
                float excess = (currentWindStrength - galeThreshold) / (3.5f - galeThreshold);
                float shakeX = (Mathf.PerlinNoise(Time.time * 22f, 0f) - 0.5f) * excess * 0.12f;
                float shakeY = (Mathf.PerlinNoise(0f, Time.time * 22f) - 0.5f) * excess * 0.08f;
                cameraRig.localPosition = originalCameraLocalPos + new Vector3(shakeX, shakeY, 0f);
            }
            else
            {
                cameraRig.localPosition = Vector3.Lerp(cameraRig.localPosition, originalCameraLocalPos, dt * 6f);
            }
        }

        private void OnGUI()
        {
            if (!Application.isPlaying) return;

            float width = 330f;
            float height = 58f;
            float x = 16f;
            float y = 16f;

            GUIStyle bgStyle = new GUIStyle(GUI.skin.box);
            bgStyle.normal.background = Texture2D.whiteTexture;
            Color oldBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.04f, 0.08f, 0.12f, 0.78f);
            GUI.Box(new Rect(x, y, width, height), GUIContent.none, bgStyle);
            GUI.backgroundColor = oldBg;

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 11;
            labelStyle.normal.textColor = new Color(0.92f, 0.95f, 0.98f, 0.95f);
            labelStyle.padding = new RectOffset(6, 6, 4, 4);

            string galeTag = currentWindStrength > galeThreshold ? " <color=#ff4d4d><b>(GALE SHAKE)</b></color>" : "";
            string statusText = $"<b>[Hoa và Máu] Môi Trường & Khí Quyển</b>\n" +
                                $"[F2] Giờ: <color=#ffe066>{currentTimeOfDay}</color> | " +
                                $"[F3] Thời Tiết: <color=#66d9ff>{currentWeather}</color>\n" +
                                $"[F4] Gió: <color=#a3e635>{currentWindStrength:F2} m/s</color>{galeTag}";

            GUI.Label(new Rect(x + 4, y + 2, width - 8, height - 4), statusText, labelStyle);
        }

        private void SetInstantValues(AtmosphereProfile p)
        {
            currentSkyZenith = p.skyZenithColor;
            currentSkyHorizon = p.skyHorizonColor;
            currentHazeColor = p.horizonHazeColor;
            currentAtmoTint = p.cloudAtmosphereTint;
            currentRimColor = p.cloudRimColor;
            currentShadowTint = p.cloudShadowTint;
            currentFarMountain = p.mountainFarColor;
            currentMidMountain = p.mountainMidColor;
            currentCliffColor = p.cliffPlateauColor;
            currentGrassBase = p.grassBaseColor;
            currentGrassTip = p.grassTipColor;
            currentParticleColor = p.particleColor;
            currentCelestialAlpha = p.celestialAlpha;
            currentCelestialTint = p.celestialTint;

            if (directionalLight != null)
            {
                directionalLight.color = p.sunLightColor;
                directionalLight.intensity = p.sunLightIntensity;
                directionalLight.transform.rotation = Quaternion.Euler(p.sunRotationEuler);
            }
        }

        public AtmosphereProfile GetProfile(TimeOfDayState state)
        {
            switch (state)
            {
                case TimeOfDayState.Dawn: return dawnProfile;
                case TimeOfDayState.Dusk: return duskProfile;
                case TimeOfDayState.Night: return nightProfile;
                default: return dayProfile;
            }
        }

        private void InitializeDefaultProfiles()
        {
            // DAWN: Warm peach, mauve-pink horizon with soft golden sunbeams
            dawnProfile = new AtmosphereProfile
            {
                name = "Dawn",
                skyZenithColor = new Color(0.25f, 0.16f, 0.35f, 1f),
                skyHorizonColor = new Color(0.98f, 0.72f, 0.52f, 1f),
                horizonHazeColor = new Color(0.98f, 0.72f, 0.52f, 0.40f),
                sunLightColor = new Color(1.0f, 0.82f, 0.65f, 1f),
                sunLightIntensity = 0.95f,
                sunRotationEuler = new Vector3(20f, -45f, 0f),
                cloudAtmosphereTint = new Color(1.0f, 0.90f, 0.82f, 1f),
                cloudRimColor = new Color(1.0f, 0.86f, 0.62f, 1f),
                cloudShadowTint = new Color(0.48f, 0.35f, 0.50f, 1f),
                mountainFarColor = new Color(0.38f, 0.28f, 0.42f, 1f),
                mountainMidColor = new Color(0.44f, 0.35f, 0.32f, 1f),
                cliffPlateauColor = new Color(0.24f, 0.20f, 0.22f, 1f),
                grassBaseColor = new Color(0.18f, 0.25f, 0.15f, 1f),
                grassTipColor = new Color(0.92f, 0.74f, 0.42f, 1f),
                particleColor = new Color(1.0f, 0.88f, 0.65f, 0.85f),
                celestialAlpha = 0.20f,
                celestialTint = new Color(1.0f, 0.90f, 0.80f, 1f)
            };

            // DAY: Crisp turquoise & cerulean sky, radiant pure cumulus billows, pink horizon haze band
            dayProfile = new AtmosphereProfile
            {
                name = "Day",
                skyZenithColor = new Color(0.10f, 0.45f, 0.58f, 1f),
                skyHorizonColor = new Color(0.28f, 0.68f, 0.76f, 1f),
                horizonHazeColor = new Color(0.92f, 0.78f, 0.82f, 0.55f),
                sunLightColor = new Color(1.0f, 0.98f, 0.92f, 1f),
                sunLightIntensity = 1.20f,
                sunRotationEuler = new Vector3(45f, -30f, 0f),
                cloudAtmosphereTint = new Color(1.0f, 1.0f, 1.0f, 1f),
                cloudRimColor = new Color(1.0f, 0.98f, 0.92f, 1f),
                cloudShadowTint = new Color(0.48f, 0.60f, 0.62f, 1f),
                mountainFarColor = new Color(0.25f, 0.52f, 0.65f, 1f),
                mountainMidColor = new Color(0.18f, 0.46f, 0.52f, 1f),
                cliffPlateauColor = new Color(0.22f, 0.30f, 0.28f, 1f),
                grassBaseColor = new Color(0.14f, 0.28f, 0.15f, 1f),
                grassTipColor = new Color(0.72f, 0.92f, 0.40f, 1f),
                particleColor = new Color(1.0f, 1.0f, 0.92f, 0.85f),
                celestialAlpha = 0.35f,
                celestialTint = new Color(0.90f, 0.95f, 1.0f, 1f)
            };

            // DUSK: Fiery amber-orange horizon melting into rich violet-magenta zenith
            duskProfile = new AtmosphereProfile
            {
                name = "Dusk",
                skyZenithColor = new Color(0.12f, 0.17f, 0.20f, 1f),
                skyHorizonColor = new Color(0.72f, 0.41f, 0.33f, 1f),
                horizonHazeColor = new Color(0.85f, 0.48f, 0.35f, 0.45f),
                sunLightColor = new Color(0.98f, 0.52f, 0.28f, 1f),
                sunLightIntensity = 0.85f,
                sunRotationEuler = new Vector3(14f, -65f, 0f),
                cloudAtmosphereTint = new Color(1.0f, 0.78f, 0.68f, 1f),
                cloudRimColor = new Color(0.96f, 0.65f, 0.50f, 1f),
                cloudShadowTint = new Color(0.32f, 0.25f, 0.31f, 1f),
                mountainFarColor = new Color(0.45f, 0.32f, 0.35f, 1f),
                mountainMidColor = new Color(0.52f, 0.38f, 0.32f, 1f),
                cliffPlateauColor = new Color(0.18f, 0.14f, 0.18f, 1f),
                grassBaseColor = new Color(0.12f, 0.16f, 0.10f, 1f),
                grassTipColor = new Color(0.90f, 0.58f, 0.28f, 1f),
                particleColor = new Color(1.0f, 0.72f, 0.38f, 0.90f),
                celestialAlpha = 0.70f,
                celestialTint = new Color(1.0f, 0.85f, 0.65f, 1f)
            };

            // NIGHT: Authentic Planet of Lana Title Screen — Electric Cobalt & Midnight Azure Monochrome
            nightProfile = new AtmosphereProfile
            {
                name = "Night",
                skyZenithColor = new Color(0.00f, 0.12f, 0.38f, 1f),
                skyHorizonColor = new Color(0.00f, 0.35f, 0.82f, 1f),
                horizonHazeColor = new Color(0.00f, 0.48f, 0.95f, 0.55f),
                sunLightColor = new Color(0.35f, 0.68f, 0.98f, 1f),
                sunLightIntensity = 0.40f,
                sunRotationEuler = new Vector3(55f, 135f, 0f),
                cloudAtmosphereTint = new Color(0.05f, 0.32f, 0.72f, 1f),
                cloudRimColor = new Color(0.20f, 0.72f, 1.0f, 1f),
                cloudShadowTint = new Color(0.00f, 0.10f, 0.35f, 1f),
                mountainFarColor = new Color(0.00f, 0.16f, 0.48f, 1f),
                mountainMidColor = new Color(0.00f, 0.24f, 0.62f, 1f),
                cliffPlateauColor = new Color(0.00f, 0.20f, 0.45f, 1f),
                grassBaseColor = new Color(0.00f, 0.12f, 0.32f, 1f),
                grassTipColor = new Color(0.10f, 0.68f, 0.98f, 1f),
                particleColor = new Color(0.40f, 0.85f, 1.0f, 0.92f),
                celestialAlpha = 1.0f,
                celestialTint = new Color(0.25f, 0.78f, 1.0f, 1f)
            };
        }
    }
}
