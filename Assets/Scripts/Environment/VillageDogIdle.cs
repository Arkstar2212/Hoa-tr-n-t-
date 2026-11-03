using UnityEngine;

namespace HoaVaMau.Environment
{
    /// <summary>
    /// Tạo hoạt ảnh cử động sống động và âm thanh chân thực cho chú chó làng quê Nam Bộ:
    /// - Vẫy đuôi nhẹ nhàng (Tail wagging) / vẫy nhanh khi có người lại gần
    /// - Nhịp thở phập phồng (Breathing idle)
    /// - Xoay đầu ngước nhìn (Head look around / looking towards player)
    /// - Phát âm thanh sủa 3D (3D spatial bark) định kỳ hoặc khi tương tác xoa đầu
    /// </summary>
    [ExecuteAlways]
    public class VillageDogIdle : MonoBehaviour
    {
        [Header("--- Tail Wagging ---")]
        [SerializeField] private float tailWagSpeed = 4.5f;
        [SerializeField] private float tailWagAngle = 20.0f;
        [SerializeField] private float excitedTailWagSpeed = 8.5f;

        [Header("--- Breathing Idle ---")]
        [SerializeField] private float breathSpeed = 1.8f;
        [SerializeField] private float breathAngle = 2.0f;

        [Header("--- Head Subtle Motion ---")]
        [SerializeField] private float headMotionSpeed = 0.8f;
        [SerializeField] private float headTurnAngle = 8.0f;

        [Header("--- Barking Sound (3D Spatial SFX) ---")]
        [SerializeField] private AudioClip barkClip;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private bool autoBark = true;
        [SerializeField] private float minBarkInterval = 14.0f;
        [SerializeField] private float maxBarkInterval = 28.0f;
        [SerializeField] private float barkVolume = 0.85f;
        [SerializeField] private float proximityBarkDistance = 4.5f;

        private Transform tailBone;
        private Transform spineBone;
        private Transform headBone;

        private Quaternion initialTailRot;
        private Quaternion initialSpineRot;
        private Quaternion initialHeadRot;

        private bool isInitialized = false;
        private float nextBarkTime = 0f;
        private bool hasBarkOnProximity = false;
        private Transform playerTransform;

        public void ConfigureBarkAudio(AudioClip clip, float volume = 0.85f)
        {
            barkClip = clip;
            barkVolume = volume;
            SetupAudioSource();
        }

        private void OnEnable()
        {
            InitializeBones();
            SetupAudioSource();
            ScheduleNextBark();
        }

        private void Start()
        {
            InitializeBones();
            SetupAudioSource();
            ScheduleNextBark();
        }

        public void SetupAudioSource()
        {
            if (audioSource == null)
            {
                if (!TryGetComponent<AudioSource>(out audioSource))
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            if (audioSource != null)
            {
                audioSource.playOnAwake = false;
                audioSource.loop = false;
                audioSource.spatialBlend = 1.0f; // 3D Spatial Audio
                audioSource.minDistance = 3.0f;
                audioSource.maxDistance = 28.0f;
                audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
                audioSource.volume = barkVolume;
                if (barkClip != null)
                {
                    audioSource.clip = barkClip;
                }
            }
        }

        private void ScheduleNextBark()
        {
            nextBarkTime = Time.time + Random.Range(minBarkInterval, maxBarkInterval);
        }

        public void PlayBark()
        {
            PlayBark(-1f);
        }

        public void PlayBark(float customVolume)
        {
            if (audioSource == null) SetupAudioSource();
            if (audioSource == null || barkClip == null) return;

            float vol = customVolume >= 0f ? customVolume : barkVolume;
            audioSource.pitch = Random.Range(0.95f, 1.05f); // Biến thiên tự nhiên
            audioSource.PlayOneShot(barkClip, vol);
        }

        private void InitializeBones()
        {
            var smr = GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr != null && smr.bones != null)
            {
                foreach (var b in smr.bones)
                {
                    if (b == null) continue;
                    string n = b.name.ToLower();
                    if (n.Contains("tail") && tailBone == null)
                    {
                        tailBone = b;
                        initialTailRot = b.localRotation;
                    }
                    else if ((n.Contains("head_0") || n.Contains("head")) && headBone == null)
                    {
                        headBone = b;
                        initialHeadRot = b.localRotation;
                    }
                    else if (n.Contains("spine_0") && spineBone == null)
                    {
                        spineBone = b;
                        initialSpineRot = b.localRotation;
                    }
                }
            }

            isInitialized = (tailBone != null || headBone != null);
        }

        private void Update()
        {
            if (!isInitialized)
            {
                InitializeBones();
                if (!isInitialized) return;
            }

            float t;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                t = (float)UnityEditor.EditorApplication.timeSinceStartup;
            }
            else
            {
                t = Time.time;
            }
#else
            t = Time.time;
#endif

            // Check player proximity during play mode
            bool isPlayerClose = false;
            if (Application.isPlaying)
            {
                if (playerTransform == null)
                {
                    var playerObj = GameObject.FindWithTag("Player");
                    if (playerObj != null) playerTransform = playerObj.transform;
                }

                if (playerTransform != null)
                {
                    float distSqr = (playerTransform.position - transform.position).sqrMagnitude;
                    if (distSqr <= proximityBarkDistance * proximityBarkDistance)
                    {
                        isPlayerClose = true;
                        if (!hasBarkOnProximity && autoBark)
                        {
                            hasBarkOnProximity = true;
                            PlayBark();
                        }
                    }
                    else if (distSqr > (proximityBarkDistance + 3.0f) * (proximityBarkDistance + 3.0f))
                    {
                        hasBarkOnProximity = false;
                    }
                }

                // Periodic ambient bark
                if (autoBark && Time.time >= nextBarkTime)
                {
                    ScheduleNextBark();
                    if (playerTransform != null)
                    {
                        float distSqr = (playerTransform.position - transform.position).sqrMagnitude;
                        if (distSqr <= 30.0f * 30.0f) // Trong tầm nghe
                        {
                            PlayBark(barkVolume * 0.75f);
                        }
                    }
                    else
                    {
                        PlayBark(barkVolume * 0.75f);
                    }
                }
            }

            // Vẫy đuôi nhịp nhàng (vẫy nhanh hơn khi người chơi lại gần)
            if (tailBone != null)
            {
                float currentTailSpeed = isPlayerClose ? excitedTailWagSpeed : tailWagSpeed;
                float currentTailAngle = isPlayerClose ? tailWagAngle * 1.35f : tailWagAngle;
                float wag = Mathf.Sin(t * currentTailSpeed) * currentTailAngle;
                tailBone.localRotation = initialTailRot * Quaternion.Euler(0f, wag, 0f);
            }

            // Nhịp thở
            if (spineBone != null)
            {
                float breath = Mathf.Sin(t * breathSpeed) * breathAngle;
                spineBone.localRotation = initialSpineRot * Quaternion.Euler(breath, 0f, 0f);
            }

            // Đầu ngó nghiêng tò mò
            if (headBone != null)
            {
                float headTurn = Mathf.Sin(t * headMotionSpeed) * headTurnAngle;
                float headTilt = Mathf.Cos(t * (headMotionSpeed * 0.7f)) * (headTurnAngle * 0.4f);
                headBone.localRotation = initialHeadRot * Quaternion.Euler(headTilt, headTurn, 0f);
            }
        }
    }
}
