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
    /// Kịch bản chuẩn xác theo yêu cầu người dùng:
    /// 1. Chiếu camera từ cổng làng (Fade In toàn cảnh cổng làng Bắc, Bé An đứng ở ngưỡng cổng).
    /// 2. Camera mượt mà trượt tới (dolly-in) zoom vào cận cảnh khuôn mặt Bé An (Bé An nhìn quanh quê hương).
    /// 3. Camera lượn vòng cung 180 độ quanh vai ra sau lưng Bé An (Over-the-shoulder).
    /// 4. ĐOẠN CHẠY VỀ PHÍA LÀNG: Bé An bứt tốc chạy nước rút dọc con đường đất xuyên qua đồng lúa.
    ///    Trong lúc An chạy, camera từ sau lưng trôi lùi ra xa và từ từ bay bổng lên cao sang góc sườn núi,
    ///    zoom ra ĐÚNG GÓC ĐẠI CẢNG (Pos = -60.85, 50.99, 185.46 | Rot = 36.58, 150.01, 0.03 | FOV = 60).
    /// 5. Khi nhân vật chạy gần tới làng (Z = 38m): Màn hình tối đen lại (Fade out to black).
    /// 6. Trong bóng tối, chuyển mượt về camera gameplay (ThirdPersonOrbitCamera), mở khóa điều khiển,
    ///    và màn hình sáng dần trở lại (Fade in) để người chơi trực tiếp điều khiển Bé An!
    /// </summary>
    public class CinematicVillageIntroDirector : MonoBehaviour
    {
        [Header("Cinematic Settings")]
        [Tooltip("Tổng thời lượng cutscene (giây)")]
        [SerializeField] private float cutsceneDuration = 16.5f;
        [SerializeField] private bool autoPlayOnStart = true;
        [SerializeField] private bool allowSkip = true;

        [Header("Actors & Controllers")]
        [SerializeField] private Player3DController playerController;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Animator playerAnimator;
        [SerializeField] private ThirdPersonOrbitCamera gameplayCamera;

        [Header("Cinemachine Single Camera")]
        [SerializeField] private PlayableDirector playableDirector;
        [SerializeField] private Unity.Cinemachine.CinemachineBrain cinemachineBrain;
        [Tooltip("Camera điện ảnh duy nhất di chuyển liên tục (One-Shot)")]
        [SerializeField] private Unity.Cinemachine.CinemachineCamera continuousCam;

        [Header("UI & Transitions")]
        [SerializeField] private CanvasGroup fadeCanvasGroup;
        [SerializeField] private float fadeInDuration = 1.4f;
        [SerializeField] private float fadeOutDuration = 1.3f;

        [Header("Audio")]
        [SerializeField] private AudioSource ambienceAudioSource;
        [SerializeField] private AudioSource footstepAudioSource;
        [SerializeField] private AudioClip dirtFootstepClip;
        [SerializeField] private AudioClip windAmbienceClip;
        [SerializeField] private AudioClip villageBgmClip;

        // Key Locations
        private static readonly Vector3 NorthGatePos = new Vector3(-7.0f, 0.505f, 184.0f);
        private static readonly Vector3 VillageApproachPos = new Vector3(-7.0f, 0.505f, 38.0f);
        private static readonly Vector3 GameplayStartPos = new Vector3(-6.8f, 0.505f, 22.0f);

        // User's exact grand vista camera framing
        private static readonly Vector3 UserVistaPos = new Vector3(-60.85f, 50.99f, 185.46f);
        private static readonly Quaternion UserVistaRot = Quaternion.Euler(36.58f, 150.01f, 0.03f);
        private const float UserVistaFov = 60f;

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

            if (continuousCam == null)
            {
                var rig = GameObject.Find("Cinematic_Cutscene_Rig");
                if (rig != null)
                {
                    continuousCam = rig.GetComponentInChildren<Unity.Cinemachine.CinemachineCamera>();
                }
            }
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

            if (continuousCam != null)
            {
                continuousCam.Priority.Value = 25;
            }

            // 2. Position character at the starting gate
            ApplyCharacterPosition(NorthGatePos, Quaternion.Euler(0f, 180f, 0f));

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

            // 6. Launch Continuous One-Shot Choreography
            if (cutsceneRoutine != null) StopCoroutine(cutsceneRoutine);
            cutsceneRoutine = StartCoroutine(ContinuousCameraChoreographyRoutine());
        }

        private IEnumerator ContinuousCameraChoreographyRoutine()
        {
            float elapsed = 0f;
            Transform camTransform = continuousCam != null ? continuousCam.transform : (Camera.main != null ? Camera.main.transform : null);

            // Anchor points
            Vector3 camGatePos = new Vector3(-5.8f, 1.8f, 173.0f);
            Vector3 camFacePos = new Vector3(-6.75f, 1.34f, 182.4f);
            Vector3 camBehindPos = new Vector3(-6.7f, 1.55f, 185.8f);

            // Initialize camera at Gate wide shot
            if (camTransform != null)
            {
                camTransform.position = camGatePos;
                camTransform.LookAt(new Vector3(-7.0f, 1.25f, 184.0f));
            }
            if (continuousCam != null) continuousCam.Lens.FieldOfView = 48f;

            // =========================================================================
            // GIAI ĐOẠN 1: 0.0s – 2.5s | Chiếu camera từ cổng làng (Fade In màn hình)
            // =========================================================================
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

                // Gentle subtle drift forward
                float tDrift = elapsed / 2.5f;
                if (camTransform != null)
                {
                    camTransform.position = Vector3.Lerp(camGatePos, camGatePos + new Vector3(-0.1f, -0.05f, 1.0f), tDrift);
                    camTransform.LookAt(new Vector3(-7.0f, 1.25f, 184.0f));
                }

                yield return null;
            }

            // =========================================================================
            // GIAI ĐOẠN 2: 2.5s – 5.0s | Camera trượt tới (Dolly-in) Zoom vào mặt Bé An
            // =========================================================================
            Vector3 startDollyPos = camTransform != null ? camTransform.position : camGatePos;
            bool lookAroundTriggered = false;

            while (elapsed < 5.0f)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(2.5f, 5.0f, elapsed));

                if (camTransform != null)
                {
                    camTransform.position = Vector3.Lerp(startDollyPos, camFacePos, u);
                    Vector3 lookTarget = Vector3.Lerp(new Vector3(-7.0f, 1.25f, 184.0f), new Vector3(-7.0f, 1.34f, 184.0f), u);
                    camTransform.LookAt(lookTarget);
                }

                if (continuousCam != null)
                {
                    continuousCam.Lens.FieldOfView = Mathf.Lerp(48f, 34f, u);
                }

                // Bé An đưa mắt nhìn quanh phong cảnh quê hương
                if (elapsed >= 3.0f && !lookAroundTriggered)
                {
                    lookAroundTriggered = true;
                    if (playerAnimator != null) playerAnimator.CrossFadeInFixedTime("LookAround", 0.35f);
                }

                yield return null;
            }

            // =========================================================================
            // GIAI ĐOẠN 3: 5.0s – 7.5s | Camera lượn vòng cung 180° từ trước mặt ra sau lưng
            // =========================================================================
            Vector3 anHeadCenter = new Vector3(-7.0f, 1.35f, 184.0f);
            float radiusX = 1.3f;
            float radiusZ = 1.6f;

            while (elapsed < 7.5f)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(5.0f, 7.5f, elapsed));

                // Góc quét từ 0 đến PI (180 độ) quanh vai phải của Bé An
                float theta = u * Mathf.PI;
                float camX = -7.0f + Mathf.Sin(theta) * radiusX + (u * 0.3f);
                float camZ = 184.0f - Mathf.Cos(theta) * radiusZ;
                float camY = Mathf.Lerp(1.34f, 1.55f, u);

                if (camTransform != null)
                {
                    camTransform.position = new Vector3(camX, camY, camZ);
                    // Điểm ngắm xoay dần từ mặt An sang hướng con đường đất phía Nam
                    Vector3 lookTarget = Vector3.Lerp(anHeadCenter, new Vector3(-7.0f, 1.2f, 168.0f), Mathf.SmoothStep(0.2f, 1.0f, u));
                    camTransform.LookAt(lookTarget);
                }

                if (continuousCam != null)
                {
                    continuousCam.Lens.FieldOfView = Mathf.Lerp(34f, 44f, u);
                }

                // Chuẩn bị về Idle trước khi xuất phát
                if (elapsed >= 6.8f && playerAnimator != null)
                {
                    playerAnimator.CrossFadeInFixedTime("Idle", 0.2f);
                }

                yield return null;
            }

            // Ghi nhận vị trí và góc xoay sau lưng trước khi bắt đầu bứt tốc
            Quaternion camBehindRot = camTransform != null ? camTransform.rotation : Quaternion.Euler(6.3f, 180f, 0f);
            Vector3 actualCamBehindPos = camTransform != null ? camTransform.position : camBehindPos;

            // =========================================================================
            // GIAI ĐOẠN 4: 7.5s – 14.0s | BÉ AN CHẠY VỀ LÀNG & CAMERA BAY RA ĐÚNG GÓC ĐẠI CẢNH
            // =========================================================================
            if (playerAnimator != null)
            {
                playerAnimator.CrossFadeInFixedTime("Run", 0.16f);
                playerAnimator.speed = 1.35f;
            }

            while (elapsed < 14.0f)
            {
                elapsed += Time.deltaTime;
                float uSprint = Mathf.InverseLerp(7.5f, 14.0f, elapsed);

                // Bé An chạy dọc con đường đất từ Z = 184m về Z = 38m (gần tới làng)
                float currentAnZ = Mathf.Lerp(184.0f, 38.0f, uSprint);
                float swayX = Mathf.Sin(uSprint * 16.0f) * 0.12f;
                Vector3 currentAnPos = new Vector3(-7.0f + swayX, 0.505f, currentAnZ);
                ApplyCharacterPosition(currentAnPos, Quaternion.Euler(0f, 180f, 0f));

                // Tiếng bước chân đất dồn dập
                PlayFootstepAudio(0.34f);

                // Camera từ sau lưng Bé An bay lùi xa và nâng cao dần ra đúng góc hình của người dùng!
                float kArc = Mathf.SmoothStep(0f, 1f, uSprint);
                if (camTransform != null)
                {
                    camTransform.position = Vector3.Lerp(actualCamBehindPos, UserVistaPos, kArc);
                    camTransform.rotation = Quaternion.Slerp(camBehindRot, UserVistaRot, kArc);
                }

                if (continuousCam != null)
                {
                    continuousCam.Lens.FieldOfView = Mathf.Lerp(44f, UserVistaFov, kArc);
                }

                yield return null;
            }

            // =========================================================================
            // GIAI ĐOẠN 5: 14.0s – 15.3s | KHI CHẠY GẦN TỚI LÀNG -> MÀN HÌNH TỐI ĐEN LẠI (FADE OUT)
            // =========================================================================
            float fadeOutStartTime = 14.0f;
            float fadeOutEndTime = 15.3f;

            while (elapsed < fadeOutEndTime)
            {
                elapsed += Time.deltaTime;

                // An tiếp tục chạy đến lối vào làng Z = 30m
                float uApproach = Mathf.InverseLerp(14.0f, fadeOutEndTime, elapsed);
                float currentAnZ = Mathf.Lerp(38.0f, 26.0f, uApproach);
                ApplyCharacterPosition(new Vector3(-7.0f, 0.505f, currentAnZ), Quaternion.Euler(0f, 180f, 0f));
                PlayFootstepAudio(0.38f);

                // Màn hình tối đen dần lại
                if (fadeCanvasGroup != null)
                {
                    fadeCanvasGroup.alpha = Mathf.Clamp01((elapsed - fadeOutStartTime) / (fadeOutEndTime - fadeOutStartTime));
                }

                yield return null;
            }

            // Đảm bảo màn hình hoàn toàn đen
            if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = 1.0f;

            // =========================================================================
            // GIAI ĐOẠN 6: CHUYỂN VỀ GAMEPLAY TRONG BÓNG TỐI & MÀN HÌNH SÁNG LẠI (FADE IN)
            // =========================================================================
            // 1. Đặt Bé An tại vị trí bắt đầu gameplay ở làng
            ApplyCharacterPosition(GameplayStartPos, Quaternion.Euler(0f, 180f, 0f));

            if (playerAnimator != null)
            {
                playerAnimator.Play("Idle", 0, 0f);
                playerAnimator.speed = 1.0f;
            }

            // 2. Tắt âm bước chân cutscene
            if (footstepAudioSource != null && footstepAudioSource.isPlaying)
            {
                footstepAudioSource.Stop();
            }

            // 3. Chuyển quyền điều khiển Camera về ThirdPersonOrbitCamera
            if (cinemachineBrain != null)
            {
                cinemachineBrain.enabled = false;
            }

            if (gameplayCamera != null)
            {
                gameplayCamera.enabled = true;
                // Camera bắt góc sau vai nhìn về phía sân làng (yaw = 180f)
                gameplayCamera.SnapToTarget(180.0f);
            }

            // 4. Mở khóa Input cho người chơi
            if (playerController != null)
            {
                playerController.IsInputLocked = false;
            }

            isCutsceneActive = false;
            isCutsceneFinished = true;

            Debug.Log("[Cinematics] Cutscene completed with Fade Out at village approach. Handing over to Gameplay...");

            // 5. Màn hình sáng dần trở lại để người chơi bắt đầu gameplay (Fade in from black)
            float fadeInStartTime = elapsed;
            float fadeInEndTime = fadeInStartTime + 1.2f;

            while (elapsed < fadeInEndTime)
            {
                elapsed += Time.deltaTime;
                if (fadeCanvasGroup != null)
                {
                    fadeCanvasGroup.alpha = Mathf.Clamp01(1.0f - ((elapsed - fadeInStartTime) / 1.2f));
                }
                yield return null;
            }

            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = 0.0f;
                fadeCanvasGroup.gameObject.SetActive(false);
            }

            Debug.Log("[Cinematics] Gameplay fully active! Player has complete control of Bé An.");
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

            // 1. Station Bé An at the gameplay entrance position
            ApplyCharacterPosition(GameplayStartPos, Quaternion.Euler(0f, 180f, 0f));

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

            // 3. Stop footstep audio
            if (footstepAudioSource != null && footstepAudioSource.isPlaying)
            {
                footstepAudioSource.Stop();
            }

            // 4. Handover Camera to ThirdPersonOrbitCamera seamlessly
            if (cinemachineBrain != null)
            {
                cinemachineBrain.enabled = false;
            }

            if (gameplayCamera != null)
            {
                gameplayCamera.enabled = true;
                gameplayCamera.SnapToTarget(180.0f);
            }

            // 5. Unlock Player Input
            if (playerController != null)
            {
                playerController.IsInputLocked = false;
            }

            Debug.Log("[Cinematics] Cutscene instantly handed over to player.");
        }
    }
}
