using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace HoaTrenDatDo.Environment
{
    public enum WeatherType
    {
        HighNoon,     // Nắng gắt đứng bóng buổi trưa
        TropicalRain  // Cơn mưa rào trưa hè miền Tây
    }

    /// <summary>
    /// Điều khiển thời tiết và ánh sáng môi trường Nam Bộ cho Scene 1 Overhaul.
    /// Tích hợp kiến trúc:
    /// 1. Smooth Color & Intensity Lerp cho Directional Light
    /// 2. Dual-Volume Post-Processing Blending
    /// 3. Camera-Following Particle System (Rain VFX)
    /// </summary>
    [AddComponentMenu("Hoa Tren Dat Do/Environment/Nam Bo Weather Controller")]
    public class NamBoWeatherController : MonoBehaviour
    {
        [Header("=== CÀI ĐẶT THỜI TIẾT ===")]
        [SerializeField] private WeatherType currentWeather = WeatherType.HighNoon;
        [Tooltip("Thời gian chuyển đổi giữa 2 trạng thái (giây)")]
        [SerializeField] private float transitionDuration = 2.5f;

        [Header("=== PHÍM TẮT & NÚT BẤM (INPUT & UI) ===")]
        [Tooltip("Bấm phím 'I' trên bàn phím để chuyển đổi qua lại giữa Nắng Trưa và Mưa Rào")]
        [SerializeField] private bool enableKeyToggle = true;
        [Tooltip("Hiện nút bấm chuyển thời tiết trực tiếp trên màn hình Game View")]
        [SerializeField] private bool showOnScreenGuiButton = true;
        [SerializeField] private Vector2 buttonPosition = new Vector2(20f, 20f);
        [SerializeField] private Vector2 buttonSize = new Vector2(260f, 44f);

        [Header("=== ÁNH SÁNG MÔI TRƯỜNG (DIRECTIONAL LIGHT) ===")]
        [SerializeField] private Light mainDirectionalLight;

        [Tooltip("Màu vàng nhạt nhiệt đới buổi trưa đứng bóng")]
        [SerializeField] private Color noonLightColor = new Color(1.0f, 0.96f, 0.88f, 1.0f);
        [SerializeField] private float noonLightIntensity = 1.35f;
        [SerializeField] private Vector3 noonRotation = new Vector3(70f, -30f, 0f);

        [Tooltip("Màu xám xanh u trầm khi mây dông kéo đến")]
        [SerializeField] private Color rainLightColor = new Color(0.45f, 0.52f, 0.60f, 1.0f);
        [SerializeField] private float rainLightIntensity = 0.45f;
        [SerializeField] private Vector3 rainRotation = new Vector3(50f, -45f, 0f);

        [Header("=== BẦU KHÔNG KHÍ (POST-PROCESSING VOLUMES) ===")]
        [Tooltip("Volume nắng trưa (Tăng tương phản đất đỏ, sắc ấm)")]
        [SerializeField] private Volume noonVolume;
        [Tooltip("Volume trời mưa (Sương mù Haze, màu tái lạnh)")]
        [SerializeField] private Volume rainVolume;

        [Header("=== HIỆU ỨNG MƯA (CAMERA-FOLLOWING PARTICLES) ===")]
        [SerializeField] private ParticleSystem rainParticles;
        [SerializeField] private Transform targetCamera;
        [SerializeField] private float rainHeightOffset = 8f;

        [Header("=== ÂM THANH MÔI TRƯỜNG ===")]
        [SerializeField] private AudioSource rainAudioSource;
        [SerializeField] private AudioSource noonAmbienceSource;

        private Coroutine transitionCoroutine;

        public WeatherType CurrentWeather => currentWeather;

        private void Start()
        {
            if (targetCamera == null && Camera.main != null)
            {
                targetCamera = Camera.main.transform;
            }

            if (mainDirectionalLight == null)
            {
                // Tự động tìm Directional Light nếu chưa gán
                var lights = FindObjectsByType<Light>(FindObjectsInactive.Include);
                foreach (var l in lights)
                {
                    if (l.type == LightType.Directional)
                    {
                        mainDirectionalLight = l;
                        break;
                    }
                }
            }

            // Áp dụng trạng thái ban đầu ngay lập tức
            ApplyInstantState(currentWeather);
        }

        private void Update()
        {
            // Bấm phím I để chuyển đổi qua lại giữa Nắng Trưa và Mưa Rào
            if (enableKeyToggle && CheckInputKeyI())
            {
                ToggleWeather();
            }
        }

        private bool CheckInputKeyI()
        {
            // 1. Kiểm tra New Input System
            try
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null && kb.iKey.wasPressedThisFrame)
                {
                    return true;
                }
            }
            catch { }

            // 2. Kiểm tra Legacy Input (fallback dự phòng)
            try
            {
                if (Input.GetKeyDown(KeyCode.I))
                {
                    return true;
                }
            }
            catch { }

            return false;
        }

        /// <summary>
        /// Chuyển đổi qua lại giữa Nắng Trưa và Mưa Rào
        /// </summary>
        public void ToggleWeather()
        {
            if (currentWeather == WeatherType.HighNoon)
            {
                SetWeather(WeatherType.TropicalRain);
            }
            else
            {
                SetWeather(WeatherType.HighNoon);
            }
        }

        private void OnGUI()
        {
            if (!showOnScreenGuiButton) return;

            // Hiển thị nút bấm trực tiếp trên màn hình Game View
            GUIStyle btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            bool isNoon = (currentWeather == WeatherType.HighNoon);
            string icon = isNoon ? "🌧️" : "☀️";
            string label = isNoon ? " Đổi sang Mưa Rào [Phím I]" : " Đổi sang Nắng Trưa [Phím I]";

            GUI.backgroundColor = isNoon 
                ? new Color(0.25f, 0.45f, 0.65f, 0.92f) 
                : new Color(0.85f, 0.65f, 0.25f, 0.92f);

            if (GUI.Button(new Rect(buttonPosition.x, buttonPosition.y, buttonSize.x, buttonSize.y), icon + label, btnStyle))
            {
                ToggleWeather();
            }
        }

        private void LateUpdate()
        {
            // Tối ưu hiệu năng: Hạt mưa luôn bám theo camera ở tầm cao 8m
            if (rainParticles != null && targetCamera != null)
            {
                Vector3 camPos = targetCamera.position;
                rainParticles.transform.position = new Vector3(camPos.x, camPos.y + rainHeightOffset, camPos.z);
            }
        }

        /// <summary>
        /// Gọi từ gameplay, trigger hoặc timeline để đổi thời tiết.
        /// </summary>
        public void SetWeather(WeatherType newWeather)
        {
            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
            }
            transitionCoroutine = StartCoroutine(TransitionRoutine(newWeather));
        }

        private IEnumerator TransitionRoutine(WeatherType targetWeather)
        {
            currentWeather = targetWeather;

            // 1. Xác định giá trị đích
            bool isNoon = (targetWeather == WeatherType.HighNoon);
            Color targetLightColor = isNoon ? noonLightColor : rainLightColor;
            float targetLightIntensity = isNoon ? noonLightIntensity : rainLightIntensity;
            Quaternion targetLightRotation = Quaternion.Euler(isNoon ? noonRotation : rainRotation);

            float targetNoonWeight = isNoon ? 1.0f : 0.0f;
            float targetRainWeight = isNoon ? 0.0f : 1.0f;

            // 2. Điều khiển Particle mưa
            if (rainParticles != null)
            {
                if (!isNoon && !rainParticles.isPlaying)
                    rainParticles.Play();
                else if (isNoon && rainParticles.isPlaying)
                    rainParticles.Stop();
            }

            // 3. Lấy giá trị khởi điểm hiện tại
            Color startLightColor = mainDirectionalLight != null ? mainDirectionalLight.color : targetLightColor;
            float startLightIntensity = mainDirectionalLight != null ? mainDirectionalLight.intensity : targetLightIntensity;
            Quaternion startLightRotation = mainDirectionalLight != null ? mainDirectionalLight.transform.rotation : targetLightRotation;

            float startNoonWeight = noonVolume != null ? noonVolume.weight : 0.0f;
            float startRainWeight = rainVolume != null ? rainVolume.weight : 0.0f;

            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / transitionDuration);

                // A. Chuyển đổi ánh sáng
                if (mainDirectionalLight != null)
                {
                    mainDirectionalLight.color = Color.Lerp(startLightColor, targetLightColor, t);
                    mainDirectionalLight.intensity = Mathf.Lerp(startLightIntensity, targetLightIntensity, t);
                    mainDirectionalLight.transform.rotation = Quaternion.Slerp(startLightRotation, targetLightRotation, t);
                }

                // B. Blend Volumes
                if (noonVolume != null) noonVolume.weight = Mathf.Lerp(startNoonWeight, targetNoonWeight, t);
                if (rainVolume != null) rainVolume.weight = Mathf.Lerp(startRainWeight, targetRainWeight, t);

                // C. Fade Audio
                if (rainAudioSource != null)
                    rainAudioSource.volume = Mathf.Lerp(rainAudioSource.volume, targetRainWeight, t);
                if (noonAmbienceSource != null)
                    noonAmbienceSource.volume = Mathf.Lerp(noonAmbienceSource.volume, targetNoonWeight, t);

                yield return null;
            }

            // Chốt giá trị chuẩn xác ở frame cuối
            if (mainDirectionalLight != null)
            {
                mainDirectionalLight.color = targetLightColor;
                mainDirectionalLight.intensity = targetLightIntensity;
                mainDirectionalLight.transform.rotation = targetLightRotation;
            }
            if (noonVolume != null) noonVolume.weight = targetNoonWeight;
            if (rainVolume != null) rainVolume.weight = targetRainWeight;
        }

        private void ApplyInstantState(WeatherType weather)
        {
            bool isNoon = (weather == WeatherType.HighNoon);
            float noonW = isNoon ? 1f : 0f;
            float rainW = isNoon ? 0f : 1f;

            if (mainDirectionalLight != null)
            {
                mainDirectionalLight.color = isNoon ? noonLightColor : rainLightColor;
                mainDirectionalLight.intensity = isNoon ? noonLightIntensity : rainLightIntensity;
                mainDirectionalLight.transform.rotation = Quaternion.Euler(isNoon ? noonRotation : rainRotation);
            }

            if (noonVolume != null) noonVolume.weight = noonW;
            if (rainVolume != null) rainVolume.weight = rainW;

            if (rainParticles != null)
            {
                if (!isNoon) rainParticles.Play();
                else rainParticles.Stop();
            }

            if (rainAudioSource != null) rainAudioSource.volume = rainW;
            if (noonAmbienceSource != null) noonAmbienceSource.volume = noonW;
        }

        #region Inspector Test Controls
        [ContextMenu("▶ Chuyển sang Nắng Trưa (High Noon)")]
        public void TestHighNoon() => SetWeather(WeatherType.HighNoon);

        [ContextMenu("▶ Chuyển sang Mưa Rào (Tropical Rain)")]
        public void TestTropicalRain() => SetWeather(WeatherType.TropicalRain);
        #endregion
    }
}
