using UnityEngine;
using HoaVaMau.Gameplay;

namespace HoaVaMau.Environment
{
    /// <summary>
    /// Hệ thống rụng lá nghệ thuật tối ưu chuẩn phong cách Planet of Lana.
    /// - Chỉ dùng 1 Particle System duy nhất đi theo Camera (Tiết kiệm 99% CPU/Draw calls).
    /// - Tự động đồng bộ hướng lá bay theo WindManager.
    /// - Hỗ trợ bắn lá rụng ào ạt (Burst) khi có rung chấn mà không sinh rác bộ nhớ (Zero GC).
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class AmbientFallingLeaves : MonoBehaviour
    {
        public static AmbientFallingLeaves Instance { get; private set; }

        [Header("Bám theo Camera (Chỉ cần 1 bộ hạt cho toàn game)")]
        [SerializeField] private bool followMainCamera = true;
        [SerializeField] private Vector3 offsetFromCamera = new Vector3(0f, 6f, 5f);

        [Header("Tương tác gió")]
        [SerializeField] private float windInfluence = 3.5f;
        [SerializeField] private float fallSpeed = 1.2f;

        private ParticleSystem leafParticleSystem;
        private ParticleSystem.VelocityOverLifetimeModule velocityModule;
        private Transform mainCamTransform;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            leafParticleSystem = GetComponent<ParticleSystem>();
            velocityModule = leafParticleSystem.velocityOverLifetime;
            velocityModule.enabled = true;
        }

        private void Start()
        {
            if (Camera.main != null)
            {
                mainCamTransform = Camera.main.transform;
            }
        }

        private void LateUpdate()
        {
            // 1. Tự động bám theo Camera để người chơi luôn nhìn thấy lá rơi ở khung hình hiện tại
            if (followMainCamera && mainCamTransform != null)
            {
                Vector3 targetPos = mainCamTransform.position + offsetFromCamera;
                transform.position = targetPos;
            }
            else if (followMainCamera && Camera.main != null)
            {
                mainCamTransform = Camera.main.transform;
            }

            // 2. Đồng bộ hướng lá bay theo hướng gió toàn cầu của WindManager
            Vector3 wind = WindManager.WindVector;
            velocityModule.x = new ParticleSystem.MinMaxCurve(wind.x * windInfluence - 0.5f, wind.x * windInfluence + 0.5f);
            velocityModule.y = new ParticleSystem.MinMaxCurve(-fallSpeed - 0.5f, -fallSpeed + 0.3f);
        }

        /// <summary>
        /// Kích hoạt rụng lá ào ạt khi có gió giật hoặc rung chấn (quái vật dậm chân, bão tới).
        /// Hoàn toàn KHÔNG sinh rác bộ nhớ (Zero GC Alloc).
        /// </summary>
        public void TriggerGustBurst(int count = 25)
        {
            if (leafParticleSystem != null)
            {
                leafParticleSystem.Emit(count);
            }
        }
    }
}
