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
        [SerializeField, Range(0.1f, 1f)] private float walkVolume = 0.50f;
        [SerializeField, Range(0.1f, 1f)] private float runVolume = 0.85f;
        [SerializeField] private float walkPitch = 0.82f;
        [SerializeField] private float runPitch = 1.08f;

        private PlayerController player;
        private PlayerState previousState = PlayerState.Grounded;

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

            // Setup separate Footstep AudioSource as a looping player
            if (footstepAudioSource == null)
            {
                var fsGo = new GameObject("Audio_Footsteps");
                fsGo.transform.SetParent(transform, false);
                footstepAudioSource = fsGo.AddComponent<AudioSource>();
                footstepAudioSource.playOnAwake = false;
                footstepAudioSource.loop = true;
                footstepAudioSource.spatialBlend = 0.15f;
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
                footstepDirtSfx = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_An_Footsteps_Dirt.mp3");
#endif
        }

        private void OnDisable()
        {
            if (footstepAudioSource != null && footstepAudioSource.isPlaying)
            {
                footstepAudioSource.Stop();
            }
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

        private void HandleFootstepLoop()
        {
            if (footstepAudioSource == null || footstepDirtSfx == null) return;

            // Nếu không đứng trên đất hoặc đang đu dây/bám vách -> NGẮT NGAY
            if (player.CurrentState != PlayerState.Grounded)
            {
                if (footstepAudioSource.isPlaying)
                {
                    footstepAudioSource.Stop();
                }
                return;
            }

            float speedX = Mathf.Abs(player.Velocity.x);

            // Nếu đứng yên hoặc không có input di chuyển -> NGẮT NGAY
            if (speedX < 0.25f || Mathf.Abs(player.HorizontalInput) < 0.05f)
            {
                if (footstepAudioSource.isPlaying)
                {
                    footstepAudioSource.Stop();
                }
                return;
            }

            // Đang di chuyển trên mặt đất:
            bool isRunning = speedX > 3.0f;
            float targetVol = isRunning ? runVolume : walkVolume;
            float targetPitch = isRunning ? runPitch : walkPitch;

            // Đảm bảo clip được gán và loop
            if (footstepAudioSource.clip != footstepDirtSfx)
            {
                footstepAudioSource.clip = footstepDirtSfx;
            }
            footstepAudioSource.loop = true;

            // Nếu chưa phát -> phát
            if (!footstepAudioSource.isPlaying)
            {
                footstepAudioSource.volume = targetVol;
                footstepAudioSource.pitch = targetPitch;
                footstepAudioSource.Play();
            }
            else
            {
                // Điều chỉnh mượt mà theo vận tốc
                footstepAudioSource.volume = Mathf.MoveTowards(footstepAudioSource.volume, targetVol, Time.deltaTime * 3.5f);
                footstepAudioSource.pitch = Mathf.MoveTowards(footstepAudioSource.pitch, targetPitch, Time.deltaTime * 3.0f);
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
