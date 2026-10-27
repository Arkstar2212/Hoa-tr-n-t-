using UnityEngine;

namespace HoaVaMau
{
    /// <summary>
    /// Simulates gentle, organic cloth fluttering in the breeze for banners and pennants (Planet of Lana aesthetic).
    /// </summary>
    public class WindClothFlutter : MonoBehaviour
    {
        [Header("Flutter Parameters")]
        [SerializeField] private float swayAmplitude = 5.5f;
        [SerializeField] private float flutterSpeed = 3.2f;
        [SerializeField] private float windTurbulence = 2.0f;

        private float baseAngle;
        private float randomPhase;

        private void Start()
        {
            randomPhase = Random.Range(0f, 100f);
            baseAngle = transform.localEulerAngles.z;
            if (baseAngle > 180f) baseAngle -= 360f;
        }

        private void Update()
        {
            float time = Time.time * flutterSpeed + randomPhase;
            float sway = Mathf.Sin(time) * swayAmplitude + Mathf.Sin(time * 2.3f) * (swayAmplitude * 0.4f);
            float noise = (Mathf.PerlinNoise(Time.time * windTurbulence, randomPhase) - 0.5f) * (swayAmplitude * 0.8f);

            transform.localRotation = Quaternion.Euler(0f, 0f, baseAngle + sway + noise);
        }
    }
}
