using UnityEngine;

namespace HoaVaMau.Water
{
    [ExecuteAlways]
    public class GerstnerWaterManager : MonoBehaviour
    {
        public static GerstnerWaterManager Instance { get; private set; }

        [Header("Target Water Material")]
        [Tooltip("Material using StylizedWater_DredgeDave shader to synchronize wave parameters.")]
        public Material waterMaterial;

        [Header("Water Base Level")]
        public float waterBaseHeight = 0f;

        [Header("Default Wave Parameters (Fallback if no Material)")]
        public float waveSpeed = 1.2f;
        public float waveHeightMultiplier = 0.35f;
        public Vector4 waveA = new Vector4(1.0f, 0.3f, 0.45f, 12.0f);
        public Vector4 waveB = new Vector4(0.4f, 0.9f, 0.35f, 7.5f);
        public Vector4 waveC = new Vector4(-0.6f, 0.8f, 0.25f, 4.0f);

        private void OnEnable()
        {
            Instance = this;
            SyncParametersFromMaterial();
        }

        private void Awake()
        {
            Instance = this;
            SyncParametersFromMaterial();
        }

        private void Update()
        {
            if (waterMaterial != null && !Application.isPlaying)
            {
                SyncParametersFromMaterial();
            }
        }

        public void SyncParametersFromMaterial()
        {
            if (waterMaterial == null) return;

            if (waterMaterial.HasProperty("_WaveSpeed"))
                waveSpeed = waterMaterial.GetFloat("_WaveSpeed");
            if (waterMaterial.HasProperty("_WaveHeight"))
                waveHeightMultiplier = waterMaterial.GetFloat("_WaveHeight");
            if (waterMaterial.HasProperty("_WaveA"))
                waveA = waterMaterial.GetVector("_WaveA");
            if (waterMaterial.HasProperty("_WaveB"))
                waveB = waterMaterial.GetVector("_WaveB");
            if (waterMaterial.HasProperty("_WaveC"))
                waveC = waterMaterial.GetVector("_WaveC");
        }

        public static float GetWaterHeight(Vector3 worldPos)
        {
            float t = Application.isPlaying ? Time.time : (float)Time.realtimeSinceStartup;
            if (Instance == null)
            {
                return 0f;
            }

            Vector3 disp = Instance.CalculateWaveOffset(worldPos, t);
            return Instance.waterBaseHeight + disp.y;
        }

        public static Vector3 GetWaterNormal(Vector3 worldPos)
        {
            float t = Application.isPlaying ? Time.time : (float)Time.realtimeSinceStartup;
            float step = 0.2f;
            float hL = GetWaterHeight(worldPos - Vector3.right * step);
            float hR = GetWaterHeight(worldPos + Vector3.right * step);
            float hD = GetWaterHeight(worldPos - Vector3.forward * step);
            float hU = GetWaterHeight(worldPos + Vector3.forward * step);

            Vector3 normal = new Vector3(hL - hR, 2f * step, hD - hU).normalized;
            return normal;
        }

        public Vector3 CalculateWaveOffset(Vector3 p, float t)
        {
            Vector3 offset = Vector3.zero;
            offset += CalculateSingleGerstner(waveA, p, t);
            offset += CalculateSingleGerstner(waveB, p, t);
            offset += CalculateSingleGerstner(waveC, p, t);
            return offset;
        }

        private Vector3 CalculateSingleGerstner(Vector4 wave, Vector3 p, float t)
        {
            Vector2 dir = new Vector2(wave.x, wave.y).normalized;
            float wavelength = Mathf.Max(0.01f, wave.w);
            float k = 2.0f * Mathf.PI / wavelength;
            float c = Mathf.Sqrt(9.8f / k);
            float f = k * (Vector2.Dot(dir, new Vector2(p.x, p.z)) - c * t * waveSpeed);
            float a = wave.z / k * waveHeightMultiplier;

            return new Vector3(
                dir.x * (a * Mathf.Cos(f)),
                a * Mathf.Sin(f),
                dir.y * (a * Mathf.Cos(f))
            );
        }
    }
}
