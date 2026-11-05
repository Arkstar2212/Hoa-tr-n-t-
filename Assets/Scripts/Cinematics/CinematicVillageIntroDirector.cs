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
        [SerializeField] private float cutsceneDuration = 21.2f;
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
        private static readonly Vector3 NorthGatePos = new Vector3(-7.0f, 0.505f, 185.0f);
        private static readonly Vector3 VillageApproachPos = new Vector3(-7.0f, 0.505f, 38.0f);
        // Vị trí kết thúc cutscene & bắt đầu gameplay: Ngay trong sân làng trước cây đa cổ thụ
        private static readonly Vector3 GameplayStartPos = new Vector3(-6.2f, 0.505f, 18.0f);

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

            // 2. Position character inside cave mouth (Bé An xuất phát từ trong hang núi đá chạy ra qua cổng)
            Vector3 startRunPos = new Vector3(-7.0f, 0.505f, 186.5f);
            ApplyCharacterPosition(startRunPos, Quaternion.Euler(0f, 180f, 0f));

            // 3. Prepare initial animation & UI fade - BÉ AN CHẠY NGAY TỪ ĐẦU (Không đứng chờ)
            if (playerAnimator != null)
            {
                playerAnimator.Play("Run", 0, 0f);
                playerAnimator.speed = 1.0f; // Tốc độ chạy tự nhiên, giảm 50% so với trước
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

            // Đảm bảo Bé An chạy ngay lập tức khi cutscene bắt đầu (tốc độ tự nhiên, giảm 30% so với trước)
            if (playerAnimator != null)
            {
                playerAnimator.Play("Run", 0, 0f);
                playerAnimator.speed = 0.95f;
            }

            Vector3 camPosAtPhase3End = KF4_Pos;
            Quaternion camRotAtPhase3End = KF4_Rot;

            // =========================================================================
            // CHUỖI GÓC QUAY: CỔNG LÀNG -> CẬN MẶT -> ĐÚNG HÌNH 4 (SAU LƯNG) -> ZOOM RA XA (HÌNH 5)
            // Tốc độ chạy chậm lại thêm 30% (~7.35 m/s) và chạy xuyên suốt vào tận trong làng
            // =========================================================================
            const float runDuration = 18.5f;

            while (elapsed < runDuration)
            {
                elapsed += Time.deltaTime;

                // 1. VỊ TRÍ BÉ AN: Chạy liên tục từ trong hang đá (Z = 186.5m) xuyên qua cổng về Z = 24m (trong làng)
                float uRun = Mathf.Clamp01(elapsed / runDuration);
                float currentAnZ = Mathf.Lerp(186.5f, 24.0f, uRun);
                // Đường đất hơi uốn nhẹ vào sân làng từ X = -7.0m về X = -6.2m
                float currentAnX = Mathf.Lerp(-7.0f, -6.2f, uRun) + Mathf.Sin(uRun * 20.0f) * 0.06f;
                Vector3 anPos = new Vector3(currentAnX, 0.505f, currentAnZ);
                ApplyCharacterPosition(anPos, Quaternion.Euler(0f, 180f, 0f));

                // Tiếng bước chân nhịp nhàng liên tục theo nhịp chạy trẻ nhỏ
                PlayFootstepAudio(0.36f);

                // 2. ĐIỀU PHỐI CAMERA LIÊN TỤC
                if (elapsed < 2.2f)
                {
                    // GIAI ĐOẠN 1 (0.0s – 2.2s) | Hình 1: Toàn cảnh cổng làng & Fade In khi Bé An chạy qua cổng
                    if (fadeCanvasGroup != null && elapsed <= fadeInDuration)
                    {
                        fadeCanvasGroup.alpha = Mathf.Clamp01(1.0f - (elapsed / fadeInDuration));
                    }
                    else if (fadeCanvasGroup != null && fadeCanvasGroup.alpha > 0f)
                    {
                        fadeCanvasGroup.alpha = 0f;
                    }

                    float t1 = elapsed / 2.2f;
                    Vector3 camGatePos = Vector3.Lerp(KF1_Pos, new Vector3(-7.0f, 2.15f, 172.5f), Mathf.SmoothStep(0f, 1f, t1));
                    Quaternion camGateRot = Quaternion.Euler(Mathf.Lerp(6.5f, 4.0f, t1), 0f, 0f);

                    if (camTransform != null)
                    {
                        camTransform.position = camGatePos;
                        camTransform.rotation = camGateRot;
                    }
                    if (continuousCam != null) continuousCam.Lens.FieldOfView = KF1_Fov;
                }
                else if (elapsed < 4.6f)
                {
                    // GIAI ĐOẠN 2 (2.2s – 4.6s) | Hình 1 -> Hình 2: Dolly Zoom cận mặt Bé An khi đang chạy
                    float u2 = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(2.2f, 4.6f, elapsed));

                    Vector3 camGateEndPos = new Vector3(-7.0f, 2.15f, 172.5f);
                    Vector3 faceCamTargetPos = anPos + new Vector3(0.0f, 0.82f, -1.85f);
                    Vector3 currentCamPos = Vector3.Lerp(camGateEndPos, faceCamTargetPos, u2);
                    Quaternion currentCamRot = Quaternion.Slerp(Quaternion.Euler(4.0f, 0f, 0f), KF2_Rot, u2);
                    float currentFov = Mathf.Lerp(KF1_Fov, KF2_Fov, u2);

                    if (camTransform != null)
                    {
                        camTransform.position = currentCamPos;
                        camTransform.rotation = currentCamRot;
                    }
                    if (continuousCam != null) continuousCam.Lens.FieldOfView = currentFov;
                }
                else if (elapsed < 7.0f)
                {
                    // GIAI ĐOẠN 3 (4.6s – 7.0s) | Từ mặt quay qua vai đến ĐÚNG HÌNH THỨ 4 (sau lưng nhìn dọc đường)
                    float u3 = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(4.6f, 7.0f, elapsed));

                    Vector3 offsetFace = new Vector3(0.0f, 0.82f, -1.85f);
                    Vector3 offsetCtrl = new Vector3(-2.2f, 1.8f, 0.2f);
                    Vector3 offsetBehind = new Vector3(0.9f, 2.6f, +2.2f); // Offset chuẩn xác của Hình 4
                    Vector3 currentOffset = QuadraticBezier(offsetFace, offsetCtrl, offsetBehind, u3);

                    Vector3 currentCamPos = anPos + currentOffset;
                    Quaternion currentCamRot = Quaternion.Slerp(KF2_Rot, KF4_Rot, u3);
                    float currentFov = Mathf.Lerp(KF2_Fov, KF4_Fov, u3);

                    if (camTransform != null)
                    {
                        camTransform.position = currentCamPos;
                        camTransform.rotation = currentCamRot;
                    }
                    if (continuousCam != null) continuousCam.Lens.FieldOfView = currentFov;

                    // Lưu lại góc máy chính xác của Hình 4 để chuẩn bị zoom ra xa
                    camPosAtPhase3End = currentCamPos;
                    camRotAtPhase3End = currentCamRot;
                }
                else
                {
                    // GIAI ĐOẠN 4 (7.0s – 18.5s) | TỪ ĐÚNG HÌNH 4 ZOOM RA XA LÊN ĐẠI CẢNH (HÌNH 5)
                    // Trong lúc này Bé An tiếp tục chạy dọc theo đường đất băng qua ruộng lúa và tiến thẳng vào làng
                    float u4 = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(7.0f, 14.5f, elapsed));

                    Vector3 currentCamPos = Vector3.Lerp(camPosAtPhase3End, KF5_Pos, u4);
                    Quaternion currentCamRot = Quaternion.Slerp(camRotAtPhase3End, KF5_Rot, u4);
                    float currentFov = Mathf.Lerp(KF4_Fov, KF5_Fov, u4);

                    if (camTransform != null)
                    {
                        camTransform.position = currentCamPos;
                        camTransform.rotation = currentCamRot;
                    }
                    if (continuousCam != null) continuousCam.Lens.FieldOfView = currentFov;
                }

                yield return null;
            }

            // =========================================================================
            // GIAI ĐOẠN 5: 18.5s – 20.0s | KHI CHẠY VÀO TRONG LÀNG -> MÀN HÌNH TỐI ĐEN LẠI (FADE OUT)
            // =========================================================================
            float fadeOutStartTime = 18.5f;
            float fadeOutEndTime = 20.0f;

            while (elapsed < fadeOutEndTime)
            {
                elapsed += Time.deltaTime;

                // An tiếp tục chạy đến sân làng (Z = 18m)
                float uApproach = Mathf.InverseLerp(fadeOutStartTime, fadeOutEndTime, elapsed);
                float currentAnZ = Mathf.Lerp(24.0f, 18.0f, uApproach);
                ApplyCharacterPosition(new Vector3(-6.2f, 0.505f, currentAnZ), Quaternion.Euler(0f, 180f, 0f));
                PlayFootstepAudio(0.36f);

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

            // 4. Mở khóa Input & kích hoạt lại Player Controller
            if (playerController != null)
            {
                playerController.enabled = true;
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

            // 5. Unlock Player Input & kích hoạt lại Player Controller
            if (playerController != null)
            {
                playerController.enabled = true;
                playerController.IsInputLocked = false;
            }

            Debug.Log("[Cinematics] Cutscene instantly handed over to player.");
        }
    }
}
