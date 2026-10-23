using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Director điều khiển đoạn Cutscene Mở Đầu (Prologue Intro Cinematic) cho nhân vật chính An:
    /// - Nhịp 1 (Entrance): An đi bộ thư thả từ ngoài khung hình tiến vào (Walk Casual).
    /// - Nhịp 2 (Deceleration): Giảm tốc và dừng lại tự nhiên sát mép ngắm cảnh (Stop Walk).
    /// - Nhịp 3 (Observation): Dừng chân ngắm nhìn cảnh đồng lúa / bến sông, xoay đầu quan sát với nhịp thở (Observation / Idle Casual).
    /// - Nhịp 4 (Seamless Handoff): Chuyển mượt về thế đứng sẵn sàng (Idle), camera chuyển êm về góc chơi game, mở lại quyền điều khiển cho người chơi.
    /// - Hỗ trợ cả 2 chế độ: Chạy tự động (Procedural Smooth Engine) và Chạy qua Unity Timeline (PlayableDirector).
    /// </summary>
    [DisallowMultipleComponent]
    public class PrologueCutsceneDirector : MonoBehaviour
    {
        [Header("Nhân vật chính An")]
        [SerializeField] private GameObject playerObject;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private AnAnimationController anAnimController;
        [SerializeField] private Animator animator;

        [Header("Tọa độ & Lộ trình Cutscene")]
        [Tooltip("Vị trí xuất hiện của An lúc bắt đầu đoạn cutscene")]
        [SerializeField] private Vector3 startPosition = new Vector3(-8.0f, 0.0f, 0.0f);
        [Tooltip("Điểm dừng chân quan sát phong cảnh (Vantage Point)")]
        [SerializeField] private Vector3 vantagePosition = new Vector3(0.0f, 0.0f, 0.0f);
        [SerializeField] private float walkSpeed = 2.2f;

        [Header("Thời lượng từng nhịp diễn xuất (Giây)")]
        [SerializeField] private float walkDuration = 3.2f;
        [SerializeField] private float stopSettleDuration = 0.8f;
        [SerializeField] private float lookAroundDuration = 4.0f;
        [SerializeField] private float cameraBlendBackDuration = 1.5f;

        [Header("Camera & Điện ảnh")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Cinematic2DCamera cinematicCamera;
        [SerializeField] private Transform cinematicShotTransform;
        [SerializeField] private bool enableCinematicBars = true;
        [Range(0.05f, 0.20f)]
        [SerializeField] private float barScreenRatio = 0.12f;

        [Header("Tích hợp Unity Timeline (Tùy chọn)")]
        [SerializeField] private PlayableDirector playableDirector;

        [Header("Tự động kích hoạt khi vào Scene")]
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private float initialDelay = 0.5f;

        // Trạng thái nội bộ
        private bool isCutsceneRunning = false;
        private Coroutine activeCutsceneRoutine;
        private Canvas letterboxCanvas;
        private RectTransform topBar;
        private RectTransform bottomBar;
        private CanvasGroup letterboxCanvasGroup;

        // Animation State Hashes
        private static readonly int WalkCasualHash = Animator.StringToHash("Walk_Casual");
        private static readonly int StopWalkHash = Animator.StringToHash("Stop_Walk");
        private static readonly int IdleCasualHash = Animator.StringToHash("Idle_Casual");
        private static readonly int IdleHash = Animator.StringToHash("Idle");
        private static readonly int ObservationHash = Animator.StringToHash("Observation");

        public bool IsRunning => isCutsceneRunning;

        private void Awake()
        {
            FindAllReferences();
            if (enableCinematicBars)
            {
                CreateCinematicLetterboxUI();
            }
        }

        private void Start()
        {
            FindAllReferences();
            if (playOnStart)
            {
                StartCoroutine(DelayedPlay());
            }
        }

        private IEnumerator DelayedPlay()
        {
            yield return new WaitForSeconds(initialDelay);
            PlayCutscene();
        }

        private void Update()
        {
            // Phím tắt Skip Cutscene nhanh: Giữ hoặc bấm [Space] / [Escape]
            if (isCutsceneRunning && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape)))
            {
                SkipCutscene();
            }
        }

        /// <summary>
        /// Bắt đầu phát đoạn phim cắt cảnh mở đầu mượt mà
        /// </summary>
        [ContextMenu("Play Cutscene")]
        public void PlayCutscene()
        {
            if (isCutsceneRunning) return;
            FindAllReferences();

            if (playableDirector != null && playableDirector.playableAsset != null)
            {
                StartCoroutine(PlayTimelineRoutine());
            }
            else
            {
                if (activeCutsceneRoutine != null) StopCoroutine(activeCutsceneRoutine);
                activeCutsceneRoutine = StartCoroutine(PlayProceduralCutsceneRoutine());
            }
        }

        private IEnumerator PlayTimelineRoutine()
        {
            isCutsceneRunning = true;
            LockPlayerControl();
            ShowLetterbox(true);

            playableDirector.Play();
            while (playableDirector.state == PlayState.Playing)
            {
                yield return null;
            }

            ShowLetterbox(false);
            UnlockPlayerControl();
            isCutsceneRunning = false;
        }

        private IEnumerator PlayProceduralCutsceneRoutine()
        {
            isCutsceneRunning = true;
            LockPlayerControl();
            ShowLetterbox(true);

            // 1. Định vị An tại điểm xuất phát
            if (playerObject != null)
            {
                playerObject.transform.position = startPosition;
                playerObject.transform.rotation = Quaternion.Euler(0f, 90f, 0f); // Hướng nhìn về bên phải
            }

            // Thiết lập vị trí camera ban đầu
            Vector3 camStartPos = Vector3.zero;
            Quaternion camStartRot = Quaternion.identity;
            if (mainCamera != null)
            {
                camStartPos = mainCamera.transform.position;
                camStartRot = mainCamera.transform.rotation;
                if (cinematicShotTransform != null)
                {
                    mainCamera.transform.position = cinematicShotTransform.position;
                    mainCamera.transform.rotation = cinematicShotTransform.rotation;
                }
            }

            // ----------------------------------------------------
            // NHỊP 1: AN ĐI BỘ VÀO KHUNG HÌNH (Walk Casual)
            // ----------------------------------------------------
            CrossFadeAnimation(WalkCasualHash, 0.25f);
            float elapsed = 0f;
            Vector3 pStart = startPosition;
            Vector3 pTarget = vantagePosition;

            while (elapsed < walkDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / walkDuration);
                // Di chuyển đều, mượt mà
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                if (playerObject != null)
                {
                    playerObject.transform.position = Vector3.Lerp(pStart, pTarget, smoothT);
                }

                // Camera dolly nhẹ theo bước chân
                if (mainCamera != null && cinematicShotTransform != null)
                {
                    Vector3 trackOffset = new Vector3(smoothT * 1.5f, 0f, 0f);
                    mainCamera.transform.position = cinematicShotTransform.position + trackOffset;
                }
                yield return null;
            }

            if (playerObject != null)
            {
                playerObject.transform.position = pTarget;
            }

            // ----------------------------------------------------
            // NHỊP 2: DỪNG LẠI TỰ NHIÊN & GIẢM TỐC (Stop Walk)
            // ----------------------------------------------------
            CrossFadeAnimation(StopWalkHash, 0.20f);
            yield return new WaitForSeconds(stopSettleDuration);

            // ----------------------------------------------------
            // NHỊP 3: DỪNG CHÂN QUAN SÁT PHONG CẢNH (Observation)
            // ----------------------------------------------------
            // Ưu tiên chạy Observation nếu có state, hoặc Idle_Casual
            if (HasAnimatorState(ObservationHash))
            {
                CrossFadeAnimation(ObservationHash, 0.35f);
            }
            else
            {
                CrossFadeAnimation(IdleCasualHash, 0.35f);
            }

            yield return new WaitForSeconds(lookAroundDuration);

            // ----------------------------------------------------
            // NHỊP 4: BẬT SẴN SÀNG (Idle) & CAMERA CHUYỂN VỀ GAMEPLAY
            // ----------------------------------------------------
            CrossFadeAnimation(IdleHash, 0.35f);

            // Chuyển êm camera về góc nhìn 2.5D của game
            if (mainCamera != null && cinematicCamera != null)
            {
                Vector3 currentCamPos = mainCamera.transform.position;
                Quaternion currentCamRot = mainCamera.transform.rotation;
                Vector3 gameplayCamPos = new Vector3(pTarget.x, pTarget.y + 1.5f, -11.5f);
                Quaternion gameplayCamRot = Quaternion.Euler(0f, 0f, 0f);

                float camElapsed = 0f;
                while (camElapsed < cameraBlendBackDuration)
                {
                    camElapsed += Time.deltaTime;
                    float blendT = Mathf.SmoothStep(0f, 1f, camElapsed / cameraBlendBackDuration);

                    mainCamera.transform.position = Vector3.Lerp(currentCamPos, gameplayCamPos, blendT);
                    mainCamera.transform.rotation = Quaternion.Slerp(currentCamRot, gameplayCamRot, blendT);
                    yield return null;
                }
            }

            // Ẩn thanh đen điện ảnh
            ShowLetterbox(false);
            yield return new WaitForSeconds(0.4f);

            // Trả lại quyền điều khiển
            UnlockPlayerControl();
            isCutsceneRunning = false;
        }

        /// <summary>
        /// Bỏ qua Cutscene ngay lập tức (Skip) một cách an toàn không lỗi vị trí
        /// </summary>
        public void SkipCutscene()
        {
            if (!isCutsceneRunning) return;

            if (activeCutsceneRoutine != null)
            {
                StopCoroutine(activeCutroutineSafe());
            }

            if (playableDirector != null && playableDirector.state == PlayState.Playing)
            {
                playableDirector.time = playableDirector.duration;
                playableDirector.Evaluate();
                playableDirector.Stop();
            }

            // Đặt An tại vị trí hoàn thành
            if (playerObject != null)
            {
                playerObject.transform.position = vantagePosition;
                playerObject.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            }

            CrossFadeAnimation(IdleHash, 0.1f);
            ShowLetterbox(false);
            UnlockPlayerControl();
            isCutsceneRunning = false;
        }

        private IEnumerator activeCutroutineSafe()
        {
            if (activeCutsceneRoutine != null)
            {
                StopCoroutine(activeCutsceneRoutine);
                activeCutsceneRoutine = null;
            }
            yield return null;
        }

        private void LockPlayerControl()
        {
            if (playerController != null)
            {
                playerController.enabled = false;
            }

            if (anAnimController != null)
            {
                anAnimController.IsCutsceneActive = true;
            }

            if (cinematicCamera != null)
            {
                cinematicCamera.enabled = false;
            }
        }

        private void UnlockPlayerControl()
        {
            if (playerController != null)
            {
                playerController.enabled = true;
                playerController.SetFacingDirection(1f);
            }

            if (anAnimController != null)
            {
                anAnimController.IsCutsceneActive = false;
            }

            if (cinematicCamera != null)
            {
                cinematicCamera.enabled = true;
                cinematicCamera.SnapToTarget();
            }
        }

        private void CrossFadeAnimation(int stateHash, float transitionDuration)
        {
            if (animator != null && animator.isActiveAndEnabled)
            {
                if (animator.HasState(0, stateHash))
                {
                    animator.CrossFadeInFixedTime(stateHash, transitionDuration, 0);
                }
            }
        }

        private bool HasAnimatorState(int stateHash)
        {
            return animator != null && animator.HasState(0, stateHash);
        }

        private void FindAllReferences()
        {
            if (playerObject == null)
            {
                playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject == null)
                {
                    var pCtrl = FindFirstObjectByType<PlayerController>();
                    if (pCtrl != null) playerObject = pCtrl.gameObject;
                }
            }

            if (playerObject != null)
            {
                if (playerController == null) playerController = playerObject.GetComponent<PlayerController>();
                if (anAnimController == null) anAnimController = playerObject.GetComponent<AnAnimationController>();
                if (animator == null) animator = playerObject.GetComponentInChildren<Animator>();
            }

            if (mainCamera == null) mainCamera = Camera.main;
            if (cinematicCamera == null && mainCamera != null)
            {
                cinematicCamera = mainCamera.GetComponent<Cinematic2DCamera>();
            }

            if (playableDirector == null)
            {
                playableDirector = GetComponent<PlayableDirector>();
            }
        }

        private void CreateCinematicLetterboxUI()
        {
            if (letterboxCanvas != null) return;

            GameObject canvasGO = new GameObject("Canvas_Cinematic_Letterbox");
            canvasGO.transform.SetParent(transform);
            letterboxCanvas = canvasGO.AddComponent<Canvas>();
            letterboxCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            letterboxCanvas.sortingOrder = 999; // Luôn nằm trên cùng

            var scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            letterboxCanvasGroup = canvasGO.AddComponent<CanvasGroup>();
            letterboxCanvasGroup.alpha = 0f;
            letterboxCanvasGroup.blocksRaycasts = false;

            // Thanh đen trên
            GameObject topGO = new GameObject("TopBar");
            topGO.transform.SetParent(canvasGO.transform, false);
            var topImg = topGO.AddComponent<UnityEngine.UI.Image>();
            topImg.color = Color.black;
            topBar = topGO.GetComponent<RectTransform>();
            topBar.anchorMin = new Vector2(0f, 1f - barScreenRatio);
            topBar.anchorMax = new Vector2(1f, 1f);
            topBar.offsetMin = Vector2.zero;
            topBar.offsetMax = Vector2.zero;

            // Thanh đen dưới
            GameObject botGO = new GameObject("BottomBar");
            botGO.transform.SetParent(canvasGO.transform, false);
            var botImg = botGO.AddComponent<UnityEngine.UI.Image>();
            botImg.color = Color.black;
            bottomBar = botGO.GetComponent<RectTransform>();
            bottomBar.anchorMin = new Vector2(0f, 0f);
            bottomBar.anchorMax = new Vector2(1f, barScreenRatio);
            bottomBar.offsetMin = Vector2.zero;
            bottomBar.offsetMax = Vector2.zero;
        }

        private void ShowLetterbox(bool show)
        {
            if (letterboxCanvasGroup == null) return;
            StopCoroutine("FadeLetterboxRoutine");
            StartCoroutine(FadeLetterboxRoutine(show ? 1f : 0f, 0.5f));
        }

        private IEnumerator FadeLetterboxRoutine(float targetAlpha, float duration)
        {
            float startAlpha = letterboxCanvasGroup.alpha;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                letterboxCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
                yield return null;
            }
            letterboxCanvasGroup.alpha = targetAlpha;
        }
    }
}
