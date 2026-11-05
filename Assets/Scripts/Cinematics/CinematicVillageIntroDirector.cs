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
    /// Kịch bản chuẩn xác theo yêu cầu người dùng (MỘT CÚ MÁY LIỀN MẠCH DUY NHẤT - ONE-SHOT CINEMATOGRAPHY):
    /// 1. Chiếu camera từ cổng làng (Fade In toàn cảnh cổng làng Bắc, Bé An đứng ở ngưỡng cổng).
    /// 2. Camera mượt mà trượt tới (dolly-in) zoom vào cận cảnh khuôn mặt Bé An (Bé An nhìn quanh).
    /// 3. Camera lượn vòng cung 180 độ quanh vai ra sau lưng Bé An (Over-the-shoulder).
    /// 4. ĐOẠN CHẠY VỀ PHÍA LÀNG: Bé An bứt tốc chạy nước rút dọc con đường đất xuyên qua đồng lúa.
    ///    Trong lúc An chạy, camera từ sau lưng trôi lùi ra xa và từ từ nâng cao lên (crane-up / flycam đại cảnh)
    ///    thu trọn cảnh An chạy giữa cánh đồng lúa vàng bao la hướng về làng.
    /// 5. Khi An vào làng, camera hạ dần độ cao xuống sau lưng và kết thúc cutscene,
    ///    chuyển mượt về camera gameplay ThirdPersonOrbitCamera và mở khóa điều khiển cho người chơi.
    /// </summary>
    public class CinematicVillageIntroDirector : MonoBehaviour
    {
        [Header("Cinematic Settings")]
        [Tooltip("Tổng thời lượng cutscene liền mạch (giây)")]
        [SerializeField] private float cutsceneDuration = 17.5f;
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
        [Tooltip("Camera điện ảnh duy nhất di chuyển liên tục không chuyển khung hình (One-Shot)")]
        [SerializeField] private Unity.Cinemachine.CinemachineCamera continuousCam;

        [Header("UI & Transitions")]
        [SerializeField] private CanvasGroup fadeCanvasGroup;
        [SerializeField] private float fadeInDuration = 1.5f;

        [Header("Audio")]
        [SerializeField] private AudioSource ambienceAudioSource;
        [SerializeField] private AudioSource footstepAudioSource;
        [SerializeField] private AudioClip dirtFootstepClip;
        [SerializeField] private AudioClip windAmbienceClip;
        [SerializeField] private AudioClip villageBgmClip;

        // Key Locations
        private static readonly Vector3 NorthGatePos = new Vector3(-7.0f, 0.505f, 184.0f);
        private static readonly Vector3 VillageEntrancePos = new Vector3(-7.0f, 0.505f, 35.0f);
        private static readonly Vector3 VillageDecelPos = new Vector3(-6.8f, 0.505f, 22.0f);
        private static readonly Vector3 CentralSquarePos = new Vector3(-6.5f, 0.505f, 17.5f);

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

            // =========================================================================
            // GIAI ĐOẠN 4: 7.5s – 14.8s | BÉ AN CHẠY VỀ PHÍA LÀNG & CAMERA CAO LÊN TỪ TỪ
            // =========================================================================
            // Bé An bứt tốc chạy nước rút!
            if (playerAnimator != null)
            {
                playerAnimator.CrossFadeInFixedTime("Run", 0.16f);
                playerAnimator.speed = 1.35f;
            }

            while (elapsed < 14.8f)
            {
                elapsed += Time.deltaTime;
                float uSprint = Mathf.InverseLerp(7.5f, 14.8f, elapsed);

                // Tọa độ Bé An chạy dọc con đường đất từ Z = 184m về Z = 35m (Cổng vào làng)
                float currentAnZ = Mathf.Lerp(184.0f, 35.0f, uSprint);
                float swayX = Mathf.Sin(uSprint * 18.0f) * 0.12f;
                Vector3 currentAnPos = new Vector3(-7.0f + swayX, 0.505f, currentAnZ);
                ApplyCharacterPosition(currentAnPos, Quaternion.Euler(0f, 180f, 0f));

                // Âm thanh bước chân dồn dập
                PlayFootstepAudio(0.34f);

                // Camera từ sau lưng lùi xa ra và từ từ nâng cao lên (Flycam / Crane-up)
                float kHeight = Mathf.SmoothStep(0f, 1f, uSprint);
                float camDistBehind = Mathf.Lerp(1.8f, 12.0f, kHeight);
                float camAltitude = Mathf.Lerp(1.55f, 9.5f, kHeight);
                float camSideOffset = Mathf.Lerp(0.3f, 0.0f, kHeight);

                if (camTransform != null)
                {
                    camTransform.position = new Vector3(-7.0f + camSideOffset, camAltitude, currentAnZ + camDistBehind);
                    // Camera nghiêng nhìn xuống Bé An và bao quát phong cảnh cánh đồng phía trước
                    Vector3 lookAheadTarget = new Vector3(-7.0f, Mathf.Lerp(1.1f, 0.5f, kHeight), currentAnZ - Mathf.Lerp(6.0f, 18.0f, kHeight));
                    camTransform.LookAt(lookAheadTarget);
                }

                if (continuousCam != null)
                {
                    continuousCam.Lens.FieldOfView = Mathf.Lerp(44f, 52f, kHeight);
                }

                yield return null;
            }

            // =========================================================================
            // GIAI ĐOẠN 5: 14.8s – 17.0s | TIẾN VÀO LÀNG, GIẢM TỐC & CAMERA HẠ CÁNH MƯỢT MÀ
            // =========================================================================
            // Bé An vào làng, giảm tốc từ Chạy sang Đi bộ
            if (playerAnimator != null)
            {
                playerAnimator.CrossFadeInFixedTime("Walk", 0.22f);
                playerAnimator.speed = 1.0f;
            }

            while (elapsed < 16.8f)
            {
                elapsed += Time.deltaTime;
                float uVillage = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(14.8f, 16.8f, elapsed));

                // Bé An đi bộ từ cổng làng (Z = 35) vào quảng trường cây đa (Z = 17.5)
                Vector3 currentAnPos = Vector3.Lerp(VillageEntrancePos, CentralSquarePos, uVillage);
                ApplyCharacterPosition(currentAnPos, Quaternion.Euler(0f, 180f, 0f));
                PlayFootstepAudio(0.55f);

                // Camera từ trên cao hạ dần độ cao về sau lưng Bé An (Góc nhìn gameplay ThirdPerson)
                float camAltitude = Mathf.Lerp(9.5f, 2.1f, uVillage);
                float camDistBehind = Mathf.Lerp(12.0f, 2.6f, uVillage);
                float camSideOffset = Mathf.Lerp(0.0f, 0.0f, uVillage);

                if (camTransform != null)
                {
                    camTransform.position = new Vector3(currentAnPos.x + camSideOffset, camAltitude, currentAnPos.z + camDistBehind);
                    camTransform.LookAt(new Vector3(currentAnPos.x, 1.2f, currentAnPos.z - 4.0f));
                }

                if (continuousCam != null)
                {
                    continuousCam.Lens.FieldOfView = Mathf.Lerp(52f, 56f, uVillage);
                }

                yield return null;
            }

            // Bé An dừng lại đứng nghiêm trang trước cây đa cổ thụ (Idle)
            if (playerAnimator != null)
            {
                playerAnimator.CrossFadeInFixedTime("Idle", 0.25f);
                playerAnimator.speed = 1.0f;
            }
            ApplyCharacterPosition(CentralSquarePos, Quaternion.Euler(0f, 180f, 0f));

            while (elapsed < cutsceneDuration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // =========================================================================
            // GIAI ĐOẠN 6: KẾT THÚC CUTSCENE & BÀN GIAO QUYỀN ĐIỀU KHIỂN
            // =========================================================================
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

            // 1. Station Bé An at final position in the village square
            ApplyCharacterPosition(CentralSquarePos, Quaternion.Euler(0f, 180f, 0f));

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
                // Snap directly behind Bé An facing South (yaw = 180f)
                gameplayCamera.SnapToTarget(180.0f);
            }

            // 5. Unlock Player Input
            if (playerController != null)
            {
                playerController.IsInputLocked = false;
            }

            Debug.Log("[Cinematics] One-Shot Continuous Cutscene finished! Gameplay handed over to player.");
        }
    }
}
