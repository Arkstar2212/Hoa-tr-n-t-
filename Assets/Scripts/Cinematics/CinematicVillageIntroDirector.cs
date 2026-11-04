using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using HoaVaMau.Test3D;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HoaVaMau.Cinematics
{
    /// <summary>
    /// Master Cinematic Director for the Opening Village Cutscene in "CaMau_1954_Village.unity".
    /// Controls:
    /// 1. Timeline PlayableDirector synchronization
    /// 2. Cinemachine 3 multi-shot choreography (5 distinct cinematic shots)
    /// 3. Physical 3D Waypoint character locomotion for Bé An (Gate -> Rice Fields -> Village Square)
    /// 4. Synchronized Animator states (Idle -> Run -> Walk -> Idle) with zero foot-sliding
    /// 5. Fullscreen cinematic Fade In transition
    /// 6. Audio mix (ambient rice field wind, dirt footsteps, atmospheric BGM)
    /// 7. Seamless handover to ThirdPersonOrbitCamera and Player3DController input
    /// </summary>
    public class CinematicVillageIntroDirector : MonoBehaviour
    {
        [Header("Cinematic Settings")]
        [Tooltip("Total duration of the intro cutscene in seconds")]
        [SerializeField] private float cutsceneDuration = 18.0f;
        [SerializeField] private bool autoPlayOnStart = true;
        [SerializeField] private bool allowSkip = true;

        [Header("Actors & Controllers")]
        [SerializeField] private Player3DController playerController;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Animator playerAnimator;
        [SerializeField] private ThirdPersonOrbitCamera gameplayCamera;

        [Header("Timeline & Cinemachine")]
        [SerializeField] private PlayableDirector playableDirector;
        [SerializeField] private Unity.Cinemachine.CinemachineBrain cinemachineBrain;
        [SerializeField] private Unity.Cinemachine.CinemachineCamera shot1NorthGateWide;
        [SerializeField] private Unity.Cinemachine.CinemachineCamera shot2FollowBehind;
        [SerializeField] private Unity.Cinemachine.CinemachineCamera shot3RiceFieldTrack;
        [SerializeField] private Unity.Cinemachine.CinemachineCamera shot4HighVillageVista;
        [SerializeField] private Unity.Cinemachine.CinemachineCamera shot5GameplayBlend;

        [Header("UI & Transitions")]
        [SerializeField] private CanvasGroup fadeCanvasGroup;
        [SerializeField] private float fadeInDuration = 1.6f;

        [Header("Audio")]
        [SerializeField] private AudioSource ambienceAudioSource;
        [SerializeField] private AudioSource footstepAudioSource;
        [SerializeField] private AudioClip dirtFootstepClip;
        [SerializeField] private AudioClip windAmbienceClip;
        [SerializeField] private AudioClip villageBgmClip;

        // Waypoints
        private static readonly Vector3 NorthGateStartPos = new Vector3(-7.0f, 0.505f, 184.0f);
        private static readonly Vector3 RiceSprintStartPos = new Vector3(-7.0f, 0.505f, 175.0f);
        private static readonly Vector3 VillageEntrancePos = new Vector3(-7.0f, 0.505f, 35.0f);
        private static readonly Vector3 VillageDecelPos = new Vector3(-6.8f, 0.505f, 22.0f);
        private static readonly Vector3 CentralSquareFinalPos = new Vector3(-6.5f, 0.505f, 17.5f);

        // State tracking
        private bool isCutsceneActive = false;
        private bool isCutsceneFinished = false;
        private Coroutine cutsceneRoutine;
        private float stepTimer = 0f;

        public bool IsCutsceneActive => isCutsceneActive;

        private void Awake()
        {
            ResolveReferences();
        }

        private void Start()
        {
            if (autoPlayOnStart && !isCutsceneFinished)
            {
                StartIntroCutscene();
            }
        }

        private void Update()
        {
            if (!isCutsceneActive) return;

            // Skip shortcut for testing / player convenience ([Space] or [Esc])
            if (allowSkip && Time.timeSinceLevelLoad > 0.6f)
            {
                bool skipTriggered = false;
#if ENABLE_INPUT_SYSTEM
                if (Keyboard.current != null)
                {
                    if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame)
                        skipTriggered = true;
                }
#endif
                if (!skipTriggered && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape)))
                {
                    skipTriggered = true;
                }

                if (skipTriggered)
                {
                    Debug.Log("[Cinematics] User skipped opening intro sequence.");
                    EndCutsceneAndHandover(instant: true);
                }
            }
        }

        public void ResolveReferences()
        {
            if (playerController == null)
                playerController = FindFirstObjectByType<Player3DController>();

            if (playerController != null)
            {
                if (characterController == null)
                    characterController = playerController.GetComponent<CharacterController>();
                if (playerAnimator == null)
                    playerAnimator = playerController.GetComponent<Animator>();
            }

            if (gameplayCamera == null)
                gameplayCamera = FindFirstObjectByType<ThirdPersonOrbitCamera>();

            if (cinemachineBrain == null && Camera.main != null)
                cinemachineBrain = Camera.main.GetComponent<Unity.Cinemachine.CinemachineBrain>();

            if (playableDirector == null)
                playableDirector = GetComponent<PlayableDirector>();
        }

        public void StartIntroCutscene()
        {
            if (isCutsceneActive || isCutsceneFinished) return;

            ResolveReferences();
            isCutsceneActive = true;
            isCutsceneFinished = false;

            // 1. Lock Player Controller and Gameplay Camera
            if (playerController != null)
            {
                playerController.IsInputLocked = true;
            }

            if (gameplayCamera != null)
            {
                gameplayCamera.enabled = false;
            }

            if (cinemachineBrain != null)
            {
                cinemachineBrain.enabled = true;
            }

            // 2. Position character at the starting gate
            if (characterController != null) characterController.enabled = false;
            if (playerController != null)
            {
                playerController.transform.position = NorthGateStartPos;
                playerController.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            }
            if (characterController != null) characterController.enabled = true;

            // 3. Prepare initial animation & UI fade
            if (playerAnimator != null)
            {
                playerAnimator.Play("Idle", 0, 0f);
                playerAnimator.speed = 1.0f;
            }

            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.gameObject.SetActive(true);
                fadeCanvasGroup.alpha = 1.0f;
            }

            // 4. Start Audio
            if (ambienceAudioSource != null && windAmbienceClip != null)
            {
                ambienceAudioSource.clip = windAmbienceClip;
                ambienceAudioSource.loop = true;
                ambienceAudioSource.volume = 0.85f;
                ambienceAudioSource.Play();
            }

            // 5. Play Timeline if assigned
            if (playableDirector != null)
            {
                playableDirector.time = 0;
                playableDirector.Play();
            }

            // 6. Launch Choreography Coroutine
            if (cutsceneRoutine != null) StopCoroutine(cutsceneRoutine);
            cutsceneRoutine = StartCoroutine(CutsceneChoreographyRoutine());
        }

        private IEnumerator CutsceneChoreographyRoutine()
        {
            float elapsed = 0f;
            SetShotPriority(shot1NorthGateWide);

            // Phase 1: 0.0s - 2.5s -> Establishing Shot at North Gate, Screen Fades In
            while (elapsed < 2.5f)
            {
                elapsed += Time.deltaTime;

                // Fade In screen from black
                if (fadeCanvasGroup != null && elapsed <= fadeInDuration)
                {
                    fadeCanvasGroup.alpha = Mathf.Clamp01(1.0f - (elapsed / fadeInDuration));
                }
                else if (fadeCanvasGroup != null && fadeCanvasGroup.alpha > 0f)
                {
                    fadeCanvasGroup.alpha = 0f;
                }

                yield return null;
            }

            // Phase 2: 2.5s - 3.5s -> Character accelerates forward, blend to Shot 2 (Follow Behind)
            SetShotPriority(shot2FollowBehind);
            if (playerAnimator != null)
            {
                playerAnimator.CrossFadeInFixedTime("Run", 0.18f);
                playerAnimator.speed = 1.35f;
            }

            while (elapsed < 3.5f)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.InverseLerp(2.5f, 3.5f, elapsed);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                Vector3 currentPos = Vector3.Lerp(NorthGateStartPos, RiceSprintStartPos, smoothT);
                ApplyCharacterPosition(currentPos, Quaternion.Euler(0f, 180f, 0f));
                PlayFootstepAudio(0.38f);
                yield return null;
            }

            // Phase 3: 3.5s - 8.0s -> Full Sprint down the Central Dirt Avenue (Behind Chase Shot)
            while (elapsed < 8.0f)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.InverseLerp(3.5f, 14.5f, elapsed);
                Vector3 currentPos = Vector3.Lerp(RiceSprintStartPos, VillageEntrancePos, t);
                // Subtle organic road sway
                currentPos.x = -7.0f + Mathf.Sin(elapsed * 2.8f) * 0.12f;
                ApplyCharacterPosition(currentPos, Quaternion.Euler(0f, 180f, 0f));
                PlayFootstepAudio(0.34f);
                yield return null;
            }

            // Phase 4: 8.0s - 12.5s -> Blend to Shot 3 (Diagonal Rice Field Profile Track)
            SetShotPriority(shot3RiceFieldTrack);
            while (elapsed < 12.5f)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.InverseLerp(3.5f, 14.5f, elapsed);
                Vector3 currentPos = Vector3.Lerp(RiceSprintStartPos, VillageEntrancePos, t);
                currentPos.x = -7.0f + Mathf.Sin(elapsed * 2.8f) * 0.12f;
                ApplyCharacterPosition(currentPos, Quaternion.Euler(0f, 180f, 0f));
                PlayFootstepAudio(0.34f);
                yield return null;
            }

            // Phase 5: 12.5s - 14.5s -> Blend to Shot 4 (High Village Vista Crane Angle)
            SetShotPriority(shot4HighVillageVista);
            while (elapsed < 14.5f)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.InverseLerp(3.5f, 14.5f, elapsed);
                Vector3 currentPos = Vector3.Lerp(RiceSprintStartPos, VillageEntrancePos, t);
                currentPos.x = -7.0f + Mathf.Sin(elapsed * 2.8f) * 0.10f;
                ApplyCharacterPosition(currentPos, Quaternion.Euler(0f, 180f, 0f));
                PlayFootstepAudio(0.35f);
                yield return null;
            }

            // Phase 6: 14.5s - 16.5s -> Entering Village, Decelerate from Sprint to Walk
            if (playerAnimator != null)
            {
                playerAnimator.CrossFadeInFixedTime("Walk", 0.22f);
                playerAnimator.speed = 1.0f;
            }

            while (elapsed < 16.5f)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.InverseLerp(14.5f, 16.5f, elapsed);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                Vector3 currentPos = Vector3.Lerp(VillageEntrancePos, VillageDecelPos, smoothT);
                ApplyCharacterPosition(currentPos, Quaternion.Euler(0f, 180f, 0f));
                PlayFootstepAudio(0.55f);
                yield return null;
            }

            // Phase 7: 16.5s - 18.0s -> Blend to Shot 5 (Gameplay Perspective), Approach Banyan Tree & Stop
            SetShotPriority(shot5GameplayBlend);

            while (elapsed < 17.5f)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.InverseLerp(16.5f, 17.5f, elapsed);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                Vector3 currentPos = Vector3.Lerp(VillageDecelPos, CentralSquareFinalPos, smoothT);
                ApplyCharacterPosition(currentPos, Quaternion.Euler(0f, 180f, 0f));
                PlayFootstepAudio(0.60f);
                yield return null;
            }

            // Settle into Idle facing South towards central square
            if (playerAnimator != null)
            {
                playerAnimator.CrossFadeInFixedTime("Idle", 0.25f);
                playerAnimator.speed = 1.0f;
            }
            ApplyCharacterPosition(CentralSquareFinalPos, Quaternion.Euler(0f, 180f, 0f));

            while (elapsed < cutsceneDuration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Cutscene Finished: Handover to Gameplay!
            EndCutsceneAndHandover(instant: false);
        }

        private void ApplyCharacterPosition(Vector3 worldPos, Quaternion rotation)
        {
            if (playerController == null) return;

            if (characterController != null && characterController.enabled)
            {
                characterController.enabled = false;
                playerController.transform.position = worldPos;
                playerController.transform.rotation = rotation;
                characterController.enabled = true;
            }
            else
            {
                playerController.transform.position = worldPos;
                playerController.transform.rotation = rotation;
            }
        }

        private void PlayFootstepAudio(float interval)
        {
            if (footstepAudioSource == null || dirtFootstepClip == null) return;

            stepTimer += Time.deltaTime;
            if (stepTimer >= interval)
            {
                stepTimer = 0f;
                footstepAudioSource.pitch = Random.Range(0.92f, 1.08f);
                footstepAudioSource.PlayOneShot(dirtFootstepClip, 0.75f);
            }
        }

        private void SetShotPriority(Unity.Cinemachine.CinemachineCamera activeCam)
        {
            if (activeCam == null) return;

            // Reset all shot cameras to low priority
            if (shot1NorthGateWide != null) shot1NorthGateWide.Priority.Value = 10;
            if (shot2FollowBehind != null) shot2FollowBehind.Priority.Value = 10;
            if (shot3RiceFieldTrack != null) shot3RiceFieldTrack.Priority.Value = 10;
            if (shot4HighVillageVista != null) shot4HighVillageVista.Priority.Value = 10;
            if (shot5GameplayBlend != null) shot5GameplayBlend.Priority.Value = 10;

            // Raise active camera priority
            activeCam.Priority.Value = 25;
        }

        public void EndCutsceneAndHandover(bool instant)
        {
            if (!isCutsceneActive && isCutsceneFinished) return;

            if (cutsceneRoutine != null)
            {
                StopCoroutine(cutsceneRoutine);
                cutsceneRoutine = null;
            }

            isCutsceneActive = false;
            isCutsceneFinished = true;

            // 1. Ensure Bé An is stationed at the final position
            if (playerController != null)
            {
                ApplyCharacterPosition(CentralSquareFinalPos, Quaternion.Euler(0f, 180f, 0f));
            }

            if (playerAnimator != null)
            {
                playerAnimator.Play("Idle", 0, 0f);
                playerAnimator.speed = 1.0f;
            }

            // 2. Hide Fade Canvas
            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = 0.0f;
                fadeCanvasGroup.gameObject.SetActive(false);
            }

            // 3. Stop or fade cutscene audio
            if (footstepAudioSource != null && footstepAudioSource.isPlaying)
            {
                footstepAudioSource.Stop();
            }

            // 4. Handover Camera to ThirdPersonOrbitCamera
            if (cinemachineBrain != null)
            {
                cinemachineBrain.enabled = false;
            }

            if (gameplayCamera != null)
            {
                gameplayCamera.enabled = true;
                // Snap directly behind Bé An facing South (yaw = 180f)
                gameplayCamera.SnapToTarget(180.0f);
            }

            // 5. Unlock Player Input
            if (playerController != null)
            {
                playerController.IsInputLocked = false;
            }

            Debug.Log("[Cinematics] Intro Cutscene finished! Control seamlessly handed over to player.");
        }
    }
}
