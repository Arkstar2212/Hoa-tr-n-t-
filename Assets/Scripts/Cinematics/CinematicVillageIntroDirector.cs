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

        // 5 Calibrated Keyframes (Hình 1 -> Hình 5 theo kịch bản người dùng)
        // Hình 1: Toàn cảnh cổng làng Bắc (Gate Wide)
        private static readonly Vector3 KF1_Pos = new Vector3(-7.0f, 2.35f, 175.0f);
        private static readonly Quaternion KF1_Rot = Quaternion.Euler(6.5f, 0.0f, 0.0f);
        private const float KF1_Fov = 50.0f;

        // Hình 2: Cận cảnh khuôn mặt Bé An (Face Close-Up)
        private static readonly Vector3 KF2_Pos = new Vector3(-7.0f, 1.32f, 182.65f);
        private static readonly Quaternion KF2_Rot = Quaternion.Euler(1.0f, 0.0f, 0.0f);
        private const float KF2_Fov = 20.0f;

        // Hình 3: Góc nghiêng 90° nhìn sườn phải Bé An, cột cổng và khóm tre phía sau (Side Profile)
        private static readonly Vector3 KF3_Pos = new Vector3(-8.5f, 1.42f, 183.7f);
        private static readonly Quaternion KF3_Rot = Quaternion.Euler(6.0f, 75.0f, 0.0f);
        private const float KF3_Fov = 42.0f;

        // Hình 4: Góc cao sau vai nhìn dọc con đường xuyên qua đồng lúa (Over-The-Shoulder High Angle)
        private static readonly Vector3 KF4_Pos = new Vector3(-6.0f, 3.1f, 186.2f);
        private static readonly Quaternion KF4_Rot = Quaternion.Euler(36.0f, 186.0f, 0.0f);
        private const float KF4_Fov = 52.0f;

        // Hình 5: Đại cảnh trên sườn núi nhìn toàn bộ thung lũng, đồng lúa và ngôi làng (Grand Mountain Cliff Vista)
        private static readonly Vector3 KF5_Pos = new Vector3(-60.85f, 50.99f, 185.46f);
        private static readonly Quaternion KF5_Rot = Quaternion.Euler(36.58f, 150.01f, 0.03f);
        private const float KF5_Fov = 60.0f;

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

            // Initialize camera at Keyframe 1 (Gate Wide Shot - Hình 1)
            if (camTransform != null)
            {
                camTransform.position = KF1_Pos;
                camTransform.rotation = KF1_Rot;
            }
            if (continuousCam != null) continuousCam.Lens.FieldOfView = KF1_Fov;

            // =========================================================================
            // GIAI ĐOẠN 1: 0.0s – 2.2s | Hình 1: Toàn cảnh cổng làng & Fade In
            // =========================================================================
            while (elapsed < 2.2f)
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

                // Nhẹ nhàng trôi nhẹ tới trước 0.3m
                float tDrift = elapsed / 2.2f;
                if (camTransform != null)
                {
                    camTransform.position = Vector3.Lerp(KF1_Pos, KF1_Pos + new Vector3(0f, 0f, 0.35f), tDrift);
                    camTransform.rotation = KF1_Rot;
                }

                yield return null;
            }

            // =========================================================================
            // GIAI ĐOẠN 2: 2.2s – 4.8s | Hình 1 -> Hình 2: Trượt tới Dolly-in Zoom cận mặt Bé An
            // =========================================================================
            Vector3 startDollyPos = camTransform != null ? camTransform.position : KF1_Pos;
            bool lookAroundTriggered = false;

            while (elapsed < 4.8f)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(2.2f, 4.8f, elapsed));

                if (camTransform != null)
                {
                    camTransform.position = Vector3.Lerp(startDollyPos, KF2_Pos, u);
                    camTransform.rotation = Quaternion.Slerp(KF1_Rot, KF2_Rot, u);
                }

                if (continuousCam != null)
                {
                    continuousCam.Lens.FieldOfView = Mathf.Lerp(KF1_Fov, KF2_Fov, u);
                }

                // Bé An đưa mắt nhìn quanh phong cảnh quê hương
                if (elapsed >= 2.8f && !lookAroundTriggered)
                {
                    lookAroundTriggered = true;
                    if (playerAnimator != null) playerAnimator.CrossFadeInFixedTime("LookAround", 0.35f);
                }

                yield return null;
            }

            // =========================================================================
            // GIAI ĐOẠN 3: 4.8s – 7.2s | Hình 2 -> Hình 3: Lượn vòng cung sang góc nghiêng 90°
            // =========================================================================
            Vector3 pCtrl_2to3 = new Vector3(-8.8f, 1.36f, 182.5f);

            while (elapsed < 7.2f)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(4.8f, 7.2f, elapsed));

                if (camTransform != null)
                {
                    camTransform.position = QuadraticBezier(KF2_Pos, pCtrl_2to3, KF3_Pos, u);
                    camTransform.rotation = Quaternion.Slerp(KF2_Rot, KF3_Rot, u);
                }

                if (continuousCam != null)
                {
                    continuousCam.Lens.FieldOfView = Mathf.Lerp(KF2_Fov, KF3_Fov, u);
                }

                // Chuẩn bị về Idle trước khi xuất phát chạy
                if (elapsed >= 6.8f && playerAnimator != null)
                {
                    playerAnimator.CrossFadeInFixedTime("Idle", 0.25f);
                }

                yield return null;
            }

            // =========================================================================
            // GIAI ĐOẠN 4: 7.2s – 9.2s | Hình 3 -> Hình 4: Trôi lên cao ra sau vai Bé An
            // =========================================================================
            Vector3 pCtrl_3to4 = new Vector3(-8.0f, 2.6f, 186.2f);
            bool runTriggered = false;

            while (elapsed < 9.2f)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(7.2f, 9.2f, elapsed));

                if (camTransform != null)
                {
                    camTransform.position = QuadraticBezier(KF3_Pos, pCtrl_3to4, KF4_Pos, u);
                    camTransform.rotation = Quaternion.Slerp(KF3_Rot, KF4_Rot, u);
                }

                if (continuousCam != null)
                {
                    continuousCam.Lens.FieldOfView = Mathf.Lerp(KF3_Fov, KF4_Fov, u);
                }

                // Bé An bắt đầu bứt tốc chạy ở giây thứ 8.5
                if (elapsed >= 8.5f && !runTriggered)
                {
                    runTriggered = true;
                    if (playerAnimator != null)
                    {
                        playerAnimator.CrossFadeInFixedTime("Run", 0.18f);
                        playerAnimator.speed = 1.35f;
                    }
                }

                yield return null;
            }

            // =========================================================================
            // GIAI ĐOẠN 5: 9.2s – 14.0s | Hình 4 -> Hình 5: BÉ AN CHẠY VỀ LÀNG & CAMERA BAY LÊN ĐẠI CẢNH
            // =========================================================================
            while (elapsed < 14.0f)
            {
                elapsed += Time.deltaTime;
                float uSprint = Mathf.InverseLerp(9.2f, 14.0f, elapsed);

                // Bé An chạy dọc con đường đất từ Z = 184m về Z = 38m (gần tới làng)
                float currentAnZ = Mathf.Lerp(184.0f, 38.0f, uSprint);
                float swayX = Mathf.Sin(uSprint * 18.0f) * 0.12f;
                Vector3 currentAnPos = new Vector3(-7.0f + swayX, 0.505f, currentAnZ);
                ApplyCharacterPosition(currentAnPos, Quaternion.Euler(0f, 180f, 0f));

                // Tiếng bước chân đất nhịp nhàng
                PlayFootstepAudio(0.33f);

                // Camera từ sau vai Bé An bay lùi xa và nâng cao dần ra đúng góc đại cảnh Hình 5 của bạn!
                float kArc = Mathf.SmoothStep(0f, 1f, uSprint);
                if (camTransform != null)
                {
                    camTransform.position = Vector3.Lerp(KF4_Pos, KF5_Pos, kArc);
                    camTransform.rotation = Quaternion.Slerp(KF4_Rot, KF5_Rot, kArc);
                }

                if (continuousCam != null)
                {
                    continuousCam.Lens.FieldOfView = Mathf.Lerp(KF4_Fov, KF5_Fov, kArc);
                }

                yield return null;
            }

            // =========================================================================
            // GIAI ĐOẠN 6: 14.0s – 15.3s | KHI CHẠY GẦN TỚI LÀNG -> MÀN HÌNH TỐI ĐEN LẠI (FADE OUT)
            // =========================================================================
            float fadeOutStartTime = 14.0f;
            float fadeOutEndTime = 15.3f;

            while (elapsed < fadeOutEndTime)
            {
                elapsed += Time.deltaTime;

                // An tiếp tục chạy đến lối vào làng Z = 26m
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

        private static Vector3 QuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            float u = 1.0f - t;
            return (u * u * p0) + (2.0f * u * t * p1) + (t * t * p2);
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
