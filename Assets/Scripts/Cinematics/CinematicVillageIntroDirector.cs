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
    /// Cutscene Narrative (theo yêu cầu người dùng):
    /// 1. Chiếu camera từ cổng làng (Wide gate establishing shot, screen fades in)
    /// 2. Zoom mượt mà vào mặt nhân vật (Portrait close-up, Bé An nhìn quanh)
    /// 3. Từ mặt quay vòng qua sau lưng nhân vật (Orbital camera pan to over-the-shoulder view)
    /// 4. Zoom camera ra xa từ sau lưng và từ từ nâng cao lên (Elevated crane pull-back vista)
    /// 5. Chuyển mượt về camera gameplay và mở khóa điều khiển cho người chơi.
    /// </summary>
    public class CinematicVillageIntroDirector : MonoBehaviour
    {
        [Header("Cinematic Settings")]
        [Tooltip("Total duration of the intro cutscene in seconds")]
        [SerializeField] private float cutsceneDuration = 14.0f;
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
        [SerializeField] private Unity.Cinemachine.CinemachineCamera shot1GateWide;
        [SerializeField] private Unity.Cinemachine.CinemachineCamera shot2FaceCloseUp;
        [SerializeField] private Unity.Cinemachine.CinemachineCamera shot3BehindBack;
        [SerializeField] private Unity.Cinemachine.CinemachineCamera shot4CranePullBackHigh;
        [SerializeField] private Unity.Cinemachine.CinemachineCamera shot5GameplayBlend;

        [Header("UI & Transitions")]
        [SerializeField] private CanvasGroup fadeCanvasGroup;
        [SerializeField] private float fadeInDuration = 1.6f;

        [Header("Audio")]
        [SerializeField] private AudioSource ambienceAudioSource;
        [SerializeField] private AudioClip windAmbienceClip;
        [SerializeField] private AudioClip villageBgmClip;

        // An's established post at the North Village Gate
        private static readonly Vector3 NorthGatePos = new Vector3(-7.0f, 0.505f, 184.0f);
        private static readonly Quaternion NorthGateRot = Quaternion.Euler(0f, 180f, 0f);

        // State tracking
        private bool isCutsceneActive = false;
        private bool isCutsceneFinished = false;
        private Coroutine cutsceneRoutine;

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
                cinemachineBrain.DefaultBlend = new Unity.Cinemachine.CinemachineBlendDefinition(
                    Unity.Cinemachine.CinemachineBlendDefinition.Styles.EaseInOut, 1.8f);
            }

            // 2. Position character at North Gate threshold
            if (characterController != null) characterController.enabled = false;
            if (playerController != null)
            {
                playerController.transform.position = NorthGatePos;
                playerController.transform.rotation = NorthGateRot;
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

            // 4. Start Ambiance Audio
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

            // -------------------------------------------------------------
            // Shot 1: 0.0s – 2.8s -> Chiếu camera từ cổng làng (Fade In)
            // -------------------------------------------------------------
            SetShotPriority(shot1GateWide);

            while (elapsed < 2.8f)
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

            // -------------------------------------------------------------
            // Shot 2: 2.8s – 6.2s -> Zoom mượt mà vào mặt nhân vật
            // -------------------------------------------------------------
            SetShotPriority(shot2FaceCloseUp);

            // Bé An thể hiện cảm xúc, nhìn ngắm phong cảnh quê hương
            if (playerAnimator != null)
            {
                playerAnimator.CrossFadeInFixedTime("LookAround", 0.35f);
            }

            while (elapsed < 6.2f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // -------------------------------------------------------------
            // Shot 3: 6.2s – 9.2s -> Từ mặt quay qua sau lưng nhân vật
            // -------------------------------------------------------------
            SetShotPriority(shot3BehindBack);

            // Chuyển mượt về Idle
            if (playerAnimator != null)
            {
                playerAnimator.CrossFadeInFixedTime("Idle", 0.25f);
            }

            while (elapsed < 9.2f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // -------------------------------------------------------------
            // Shot 4: 9.2s – 12.2s -> Zoom camera ra xa từ sau lưng và cao lên từ từ
            // -------------------------------------------------------------
            SetShotPriority(shot4CranePullBackHigh);

            while (elapsed < 12.2f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // -------------------------------------------------------------
            // Shot 5: 12.2s – 14.0s -> Hạ mượt xuống góc nhìn gameplay qua vai
            // -------------------------------------------------------------
            SetShotPriority(shot5GameplayBlend);

            while (elapsed < cutsceneDuration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // -------------------------------------------------------------
            // Kết thúc cutscene: Bàn giao quyền điều khiển ngay tại cổng làng
            // -------------------------------------------------------------
            EndCutsceneAndHandover(instant: false);
        }

        private void SetShotPriority(Unity.Cinemachine.CinemachineCamera activeCam)
        {
            if (activeCam == null) return;

            // Reset all shot cameras to low priority
            if (shot1GateWide != null) shot1GateWide.Priority.Value = 10;
            if (shot2FaceCloseUp != null) shot2FaceCloseUp.Priority.Value = 10;
            if (shot3BehindBack != null) shot3BehindBack.Priority.Value = 10;
            if (shot4CranePullBackHigh != null) shot4CranePullBackHigh.Priority.Value = 10;
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

            // 1. Ensure Bé An is stationed at the North Gate
            if (characterController != null) characterController.enabled = false;
            if (playerController != null)
            {
                playerController.transform.position = NorthGatePos;
                playerController.transform.rotation = NorthGateRot;
            }
            if (characterController != null) characterController.enabled = true;

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

            // 3. Handover Camera to ThirdPersonOrbitCamera
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

            // 4. Unlock Player Input
            if (playerController != null)
            {
                playerController.IsInputLocked = false;
            }

            Debug.Log("[Cinematics] Cutscene finished! Player now has full gameplay control at the Village Gate.");
        }
    }
}
