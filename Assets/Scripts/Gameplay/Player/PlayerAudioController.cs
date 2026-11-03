using UnityEngine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// SOLID - Single Responsibility Principle:
    /// Quản lý phát âm thanh hiệu ứng (SFX) cho nhân vật cậu bé An:
    /// - Tiếng bước chân chạy chân trần trên đất đỏ (Loop liên tục, tự dừng khi dừng hoặc nhảy)
    /// - Tiếng bám và rướn người trèo vách đá (Ledge Grab / Climb) của cậu bé 12 tuổi
    /// - Tiếng bám và đu dây thừng (Rope Grab / Tension)
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerAudioController : MonoBehaviour
    {
        [Header("Audio Source")]
        [SerializeField] private AudioSource sfxAudioSource;
        [SerializeField] private AudioSource footstepAudioSource;

        [Header("SFX Clips")]
        [Tooltip("Âm thanh cậu bé 12 tuổi bám mép đá và rướn người trèo lên")]
        [SerializeField] private AudioClip ledgeClimbSfx;

        [Tooltip("Âm thanh bám và căng dây thừng")]
        [SerializeField] private AudioClip ropeGrabSfx;

        [Tooltip("File âm thanh chuỗi bước chân chạy chân trần trên đất đỏ")]
        [SerializeField] private AudioClip footstepDirtSfx;

        [Header("Footstep Volume & Pitch")]
        [SerializeField, Range(0.1f, 1f)] private float walkVolume = 0.95f;
        [SerializeField, Range(0.1f, 1f)] private float runVolume = 1.00f;
        [SerializeField] private float walkPitch = 0.82f;
        [SerializeField] private float runPitch = 1.08f;
        [Tooltip("Hệ số tăng cường âm lượng tiếng bước chân chạy chân trần")]
        [SerializeField, Range(1.0f, 1.8f)] private float volumeBoost = 1.25f;

        private PlayerController player;
        private PlayerState previousState = PlayerState.Grounded;
        private AudioSource footstepBoosterSource;

        private void Awake()
        {
            player = GetComponent<PlayerController>();

            // Setup main SFX AudioSource
            if (sfxAudioSource == null)
            {
                sfxAudioSource = gameObject.AddComponent<AudioSource>();
                sfxAudioSource.playOnAwake = false;
                sfxAudioSource.loop = false;
                sfxAudioSource.spatialBlend = 0.15f;
            }

            // Setup separate Footstep AudioSource as a looping player (2D direct audio for crisp presence)
            if (footstepAudioSource == null)
            {
                var fsGo = new GameObject("Audio_Footsteps");
                fsGo.transform.SetParent(transform, false);
                footstepAudioSource = fsGo.AddComponent<AudioSource>();
                footstepAudioSource.playOnAwake = false;
                footstepAudioSource.loop = true;
                footstepAudioSource.spatialBlend = 0.0f;
            }
            else
            {
                footstepAudioSource.spatialBlend = 0.0f;
            }

            // Setup secondary booster source to deliver clean extra volume beyond Unity's single-source 1.0 clamp
            if (footstepBoosterSource == null)
            {
                var boostGo = new GameObject("Audio_Footsteps_Booster");
                boostGo.transform.SetParent(transform, false);
                footstepBoosterSource = boostGo.AddComponent<AudioSource>();
                footstepBoosterSource.playOnAwake = false;
                footstepBoosterSource.loop = true;
                footstepBoosterSource.spatialBlend = 0.0f;
            }

            AutoLoadDefaultClips();
        }

        private void Reset()
        {
            AutoLoadDefaultClips();
        }

        public void AutoLoadDefaultClips()
        {
#if UNITY_EDITOR
            if (ledgeClimbSfx == null)
                ledgeClimbSfx = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_An_LedgeClimb.mp3");

            if (ropeGrabSfx == null)
                ropeGrabSfx = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_An_RopeGrab.mp3");

            if (footstepDirtSfx == null)
                footstepDirtSfx = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_An_Footsteps_Dirt.wav");
            if (footstepDirtSfx == null)
                footstepDirtSfx = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_An_Footsteps_Dirt.mp3");
#endif
        }

        private void OnDisable()
        {
            StopFootsteps(immediate: true);
        }

        private void Update()
        {
            if (player == null) return;

            PlayerState currentState = player.CurrentState;

            // Xử lý chuyển trạng thái (State Transition SFX)
            HandleStateTransitionAudio(previousState, currentState);
            previousState = currentState;

            // Xử lý tiếng bước chân dạng Loop mượt mà
            HandleFootstepLoop();
        }

        private void HandleStateTransitionAudio(PlayerState oldState, PlayerState newState)
        {
            if (oldState == newState) return;

            // Khi bắt đầu bám hoặc trèo vách đá (Ledge Grab / Climb)
            if (newState == PlayerState.LedgeGrab || newState == PlayerState.LedgeClimb)
            {
                if (oldState != PlayerState.LedgeGrab && oldState != PlayerState.LedgeClimb)
                {
                    PlaySFX(ledgeClimbSfx, 0.95f, 0.98f, 1.05f);
                }
            }

            // Khi bắt đầu bám dây thừng (Rope Grab)
            if (newState == PlayerState.RopeGrab || newState == PlayerState.RopeClimb)
            {
                if (oldState != PlayerState.RopeGrab && oldState != PlayerState.RopeClimb)
                {
                    PlaySFX(ropeGrabSfx, 0.85f, 0.95f, 1.05f);
                }
            }
        }

        /// <summary>
        /// Dừng âm thanh bước chân lập tức (immediate = true) hoặc giảm dần êm dịu (immediate = false).
        /// Hỗ trợ cả trong gameplay thông thường lẫn khi cutscene làm chậm / dừng nhân vật.
        /// </summary>
        public void StopFootsteps(bool immediate = true)
        {
            if (immediate)
            {
                if (footstepAudioSource != null && footstepAudioSource.isPlaying) footstepAudioSource.Stop();
                if (footstepBoosterSource != null && footstepBoosterSource.isPlaying) footstepBoosterSource.Stop();
            }
            else
            {
                if (footstepAudioSource != null && footstepAudioSource.isPlaying)
                {
                    footstepAudioSource.volume = Mathf.MoveTowards(footstepAudioSource.volume, 0f, Time.deltaTime * 6.0f);
                    if (footstepAudioSource.volume <= 0.01f)
                    {
                        footstepAudioSource.Stop();
                    }
                }
                if (footstepBoosterSource != null && footstepBoosterSource.isPlaying)
                {
                    footstepBoosterSource.volume = Mathf.MoveTowards(footstepBoosterSource.volume, 0f, Time.deltaTime * 6.0f);
                    if (footstepBoosterSource.volume <= 0.01f)
                    {
                        footstepBoosterSource.Stop();
                    }
                }
            }
        }

        private void HandleFootstepLoop()
        {
            if (footstepAudioSource == null || footstepDirtSfx == null) return;

            // Nếu không đứng trên đất hoặc đang đu dây/bám vách -> NGẮT NGAY
            if (player.CurrentState != PlayerState.Grounded)
            {
                StopFootsteps(immediate: true);
                return;
            }

            float speedX = Mathf.Abs(player.Velocity.x);

            // Kiểm tra trạng thái cutscene nếu AnAnimationController đang kích hoạt
            var anAnim = player.GetComponent<AnAnimationController>();
            bool isCutscene = anAnim != null && anAnim.IsCutsceneActive;

            // Nếu không trong cutscene và không có input di chuyển hoặc vận tốc quá nhỏ -> NGẮT NGAY
            if (!isCutscene && (speedX < 0.25f || Mathf.Abs(player.HorizontalInput) < 0.05f))
            {
                StopFootsteps(immediate: false);
                return;
            }

            // Nếu trong cutscene mà đứng yên -> NGẮT MƯỢT MÀ
            if (isCutscene && speedX < 0.25f)
            {
                StopFootsteps(immediate: false);
                return;
            }

            // Nếu đứng yên -> NGẮT MƯỢT MÀ
            if (speedX < 0.25f)
            {
                StopFootsteps(immediate: false);
                return;
            }

            // Đang di chuyển trên mặt đất:
            bool isRunning = speedX > 3.0f;
            float baseVol = isRunning ? runVolume : walkVolume;
            float targetPitch = isRunning ? runPitch : walkPitch;

            // Nếu vận tốc giảm dần dưới 2.0m/s (như khi nhân vật đi chậm lại trong cutscene), hạ âm lượng và pitch tương ứng
            if (speedX < 2.0f)
            {
                float t = Mathf.Clamp01((speedX - 0.25f) / 1.75f);
                baseVol = Mathf.Lerp(walkVolume * 0.35f, walkVolume, t);
                targetPitch = Mathf.Lerp(walkPitch * 0.85f, walkPitch, t);
            }

            float totalTargetVol = baseVol * volumeBoost;
            float mainTargetVol = Mathf.Clamp01(totalTargetVol);
            float boostTargetVol = Mathf.Clamp01(totalTargetVol - 1.0f);

            // Đảm bảo clip được gán và loop
            if (footstepAudioSource.clip != footstepDirtSfx)
            {
                footstepAudioSource.clip = footstepDirtSfx;
            }
            footstepAudioSource.loop = true;

            if (footstepBoosterSource != null)
            {
                if (footstepBoosterSource.clip != footstepDirtSfx)
                {
                    footstepBoosterSource.clip = footstepDirtSfx;
                }
                footstepBoosterSource.loop = true;
            }

            // Nếu chưa phát -> phát đồng bộ
            if (!footstepAudioSource.isPlaying)
            {
                footstepAudioSource.volume = mainTargetVol;
                footstepAudioSource.pitch = targetPitch;
                footstepAudioSource.Play();

                if (footstepBoosterSource != null && boostTargetVol > 0.01f)
                {
                    footstepBoosterSource.volume = boostTargetVol;
                    footstepBoosterSource.pitch = targetPitch;
                    footstepBoosterSource.timeSamples = footstepAudioSource.timeSamples;
                    footstepBoosterSource.Play();
                }
            }
            else
            {
                // Điều chỉnh mượt mà theo vận tốc
                footstepAudioSource.volume = Mathf.MoveTowards(footstepAudioSource.volume, mainTargetVol, Time.deltaTime * 3.5f);
                footstepAudioSource.pitch = Mathf.MoveTowards(footstepAudioSource.pitch, targetPitch, Time.deltaTime * 3.0f);

                if (footstepBoosterSource != null)
                {
                    if (boostTargetVol > 0.01f)
                    {
                        if (!footstepBoosterSource.isPlaying)
                        {
                            footstepBoosterSource.timeSamples = footstepAudioSource.timeSamples;
                            footstepBoosterSource.Play();
                        }
                        footstepBoosterSource.volume = Mathf.MoveTowards(footstepBoosterSource.volume, boostTargetVol, Time.deltaTime * 3.5f);
                        footstepBoosterSource.pitch = Mathf.MoveTowards(footstepBoosterSource.pitch, targetPitch, Time.deltaTime * 3.0f);
                    }
                    else if (footstepBoosterSource.isPlaying)
                    {
                        footstepBoosterSource.Stop();
                    }
                }
            }
        }

        public void PlayLedgeClimbSound()
        {
            PlaySFX(ledgeClimbSfx, 1.0f, 0.98f, 1.04f);
        }

        public void PlayRopeGrabSound()
        {
            PlaySFX(ropeGrabSfx, 0.9f, 0.95f, 1.05f);
        }

        private void PlaySFX(AudioClip clip, float volume = 1f, float minPitch = 0.95f, float maxPitch = 1.05f)
        {
            if (clip == null || sfxAudioSource == null) return;
            sfxAudioSource.pitch = Random.Range(minPitch, maxPitch);
            sfxAudioSource.PlayOneShot(clip, volume);
        }
    }
}
