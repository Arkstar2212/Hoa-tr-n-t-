#pragma warning disable 0414
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;
using TMPro;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Production-grade Cinematic Wide-Zoom Village Arrival Cutscene (Planet of Lana standard).
    /// Triggered when An emerges from the Golden Rice Field and enters the living Nam Bộ village.
    /// Features:
    /// - Cinematic 21:9 Letterbox bars with smooth quadratic ease.
    /// - TextMeshPro typography with historical gold/cream watercolor palette.
    /// - Multi-stage movement: Running entry -> scenic jog -> wonder-filled curious walk -> warm stop.
    /// - Living village interactions: Bác Ba and village children wave warmly to welcome An.
    /// - An waves back and takes in the breathtaking vista of stilted houses and golden harvest paddies.
    /// - Glitch-free, silky-smooth camera blend and player control handover.
    /// </summary>
    [DisallowMultipleComponent]
    public class VillageArrivalCutscene : MonoBehaviour
    {
        [Header("Cutscene Trigger & Path")]
        [SerializeField] private float triggerX = 94.0f;
        [SerializeField] private float targetRunToX = 109.5f;
        [SerializeField] private float autoRunSpeed = 3.2f;
        [SerializeField] private string villageTitle = "XÓM RẠCH GỐC  •  MÙA LÚA CHÍN 1954";
        [SerializeField] private string villageSubtitle = "Hương lúa mới trải dài quanh những mái nhà sàn bình yên trước ngày tập kết.";

        [Header("Cinemachine Cameras")]
        [SerializeField] private CinemachineCamera vcamVillageWide;
        [SerializeField] private CinemachineCamera vcamGameplay;

        [Header("Living Villagers")]
        [SerializeField] private Animator welcomingVillagerAnimator;

        [Header("Typography")]
        [SerializeField] private TMP_FontAsset fontAsset;

        private bool hasTriggered = false;
        private bool isRunningCutscene = false;
        private PlayerController player;

        // Runtime Letterbox UI
        private Canvas cutsceneCanvas;
        private CanvasGroup canvasGroup;
        private RectTransform topBar;
        private RectTransform bottomBar;
        private CanvasGroup captionGroup;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI subText;

        public bool IsRunningCutscene => isRunningCutscene;

        private void Start()
        {
            player = Object.FindAnyObjectByType<PlayerController>();

            if (vcamVillageWide == null)
            {
                var found = GameObject.Find("VCam_VillageArrivalWide");
                if (found != null) vcamVillageWide = found.GetComponent<CinemachineCamera>();
            }

            if (vcamGameplay == null)
            {
                var found = GameObject.Find("CM_VirtualCamera_2.5D");
                if (found != null) vcamGameplay = found.GetComponent<CinemachineCamera>();
            }

            // Calibrate wide camera framing according to Planet of Lana standards
            if (vcamVillageWide != null)
            {
                var cf = vcamVillageWide.GetComponent<CinemachineFollow>();
                if (cf != null)
                {
                    cf.FollowOffset = new Vector3(4.50f, 2.20f, -22.50f);
                }
                vcamVillageWide.Lens.FieldOfView = 37.5f;
            }

            BuildLetterboxUI();
        }

        private void Update()
        {
            if (hasTriggered || isRunningCutscene) return;

            if (player == null)
            {
                player = Object.FindAnyObjectByType<PlayerController>();
                if (player == null) return;
            }

            if (!player.enabled) return;

            if (player.transform.position.x >= triggerX && player.transform.position.x <= triggerX + 4.5f)
            {
                hasTriggered = true;
                StartCoroutine(PlayVillageArrivalRoutine());
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasTriggered || isRunningCutscene) return;
            var pc = other.GetComponent<PlayerController>() ?? other.GetComponentInParent<PlayerController>();
            if (pc != null && pc.enabled)
            {
                player = pc;
                hasTriggered = true;
                StartCoroutine(PlayVillageArrivalRoutine());
            }
        }

        private IEnumerator PlayVillageArrivalRoutine()
        {
            isRunningCutscene = true;

            // 1. Lock player input cleanly and align facing right
            player.SetFacingDirection(1f);
            player.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            player.enabled = false;

            // Đồng bộ vận tốc motor ban đầu theo autoRunSpeed để PlayerAudioController khớp nhịp chạy
            if (player.Motor != null) player.Motor.Velocity = new Vector3(autoRunSpeed, 0f, 0f);

            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false; // Disable CC during cinematic spline movement to eliminate physics jitter

            AnAnimationController anAnimCtrl = player.GetComponent<AnAnimationController>();
            if (anAnimCtrl != null)
            {
                anAnimCtrl.IsCutsceneActive = true;
            }

            Animator anAnim = player.GetComponentInChildren<Animator>();

            // 2. Activate Wide-Zoom Village Panorama Camera (Priority 60 overrides Gameplay camera Priority 50)
            if (vcamVillageWide != null)
            {
                vcamVillageWide.gameObject.SetActive(true);
                vcamVillageWide.Priority = 60;
            }

            // 3. Slide in cinematic 21:9 letterbox bars & fade in warm gold title
            if (cutsceneCanvas != null) cutsceneCanvas.gameObject.SetActive(true);
            StartCoroutine(AnimateLetterboxRoutine(true, 1.2f));

            // Find child villagers for responsive ambient greeting
            Animator tiAnim = GameObject.Find("VillageChild_01_Ti")?.GetComponentInChildren<Animator>();
            Animator betuAnim = GameObject.Find("VillageChild_02_BeTu")?.GetComponentInChildren<Animator>();

            bool bacBaWaved = false;
            bool tiWaved = false;
            bool betuWaved = false;

            // 4. Multi-Stage Movement (Planet of Lana Standard)
            // Phase A: Entry Jog -> Phase B: Wonderstruck Scenic Walk -> Phase C: Ease to Stop
            float startX = player.transform.position.x;
            float totalDist = Mathf.Max(1.0f, targetRunToX - startX);
            float elapsedMove = 0f;
            float maxDuration = 12.0f;

            if (anAnimCtrl != null)
            {
                anAnimCtrl.PlayCutsceneAnimation("Run", 0.20f);
                anAnimCtrl.SetCutsceneSpeed(autoRunSpeed);
            }
            else if (anAnim != null)
            {
                anAnim.SetBool("IsGrounded", true);
                anAnim.SetBool("IsCrouching", false);
                anAnim.SetFloat("Speed", autoRunSpeed);
                anAnim.CrossFadeInFixedTime("Run", 0.20f);
            }

            bool switchedToWalk = false;
            bool switchedToIdle = false;

            while (player.transform.position.x < targetRunToX && elapsedMove < maxDuration)
            {
                elapsedMove += Time.deltaTime;
                float currentX = player.transform.position.x;
                float progress = Mathf.Clamp01((currentX - startX) / totalDist);

                // Speed curve:
                // progress 0.00 -> 0.35: Entry Jog (autoRunSpeed -> 2.2m/s)
                // progress 0.35 -> 0.75: Wonderstruck Scenic Walk (2.2m/s -> 1.1m/s)
                // progress 0.75 -> 1.00: Smooth deceleration to full stop (1.1m/s -> 0.0m/s)
                float currentSpeed;
                if (progress < 0.35f)
                {
                    float k = progress / 0.35f;
                    currentSpeed = Mathf.Lerp(autoRunSpeed, 2.2f, k);
                    if (anAnimCtrl != null) anAnimCtrl.SetCutsceneSpeed(currentSpeed);
                    else if (anAnim != null) anAnim.SetFloat("Speed", currentSpeed);
                }
                else if (progress < 0.75f)
                {
                    float k = (progress - 0.35f) / 0.40f;
                    currentSpeed = Mathf.Lerp(2.2f, 1.1f, k);

                    if (!switchedToWalk)
                    {
                        switchedToWalk = true;
                        if (anAnimCtrl != null) anAnimCtrl.PlayCutsceneAnimation("Walk", 0.35f);
                        else if (anAnim != null) anAnim.CrossFadeInFixedTime("Walk", 0.35f);
                    }

                    if (anAnimCtrl != null) anAnimCtrl.SetCutsceneSpeed(currentSpeed);
                    else if (anAnim != null) anAnim.SetFloat("Speed", currentSpeed);
                }
                else
                {
                    float k = (progress - 0.75f) / 0.25f;
                    // Ease smoothly to 0.0m/s
                    currentSpeed = Mathf.Lerp(1.1f, 0.0f, Mathf.SmoothStep(0f, 1f, k));

                    if (progress >= 0.90f && !switchedToIdle)
                    {
                        switchedToIdle = true;
                        if (anAnimCtrl != null) anAnimCtrl.PlayCutsceneAnimation("Idle", 0.35f);
                        else if (anAnim != null) anAnim.CrossFadeInFixedTime("Idle", 0.35f);
                    }

                    if (anAnimCtrl != null) anAnimCtrl.SetCutsceneSpeed(currentSpeed);
                    else if (anAnim != null) anAnim.SetFloat("Speed", currentSpeed);
                }

                // Move smoothly along X with ground snap
                Vector3 pos = player.transform.position;
                pos.x += currentSpeed * Time.deltaTime;
                pos.z = 0f;

                // Ground snap raycast ignoring trigger zones to eliminate all vertical collision jitter
                if (Physics.Raycast(new Vector3(pos.x, pos.y + 1.2f, 0f), Vector3.down, out RaycastHit hit, 4.0f, ~0, QueryTriggerInteraction.Ignore))
                {
                    pos.y = hit.point.y;
                }
                player.transform.position = pos;

                // Cập nhật vận tốc motor theo đúng tốc độ cutscene để PlayerAudioController đồng bộ nhịp chân
                if (player.Motor != null)
                {
                    player.Motor.Velocity = new Vector3(currentSpeed, 0f, 0f);
                }

                // Khi nhân vật đi chậm lại tới ngưỡng dừng (progress >= 0.88f hoặc currentSpeed < 0.25f), ngắt tiếng bước chân ngay
                if (progress >= 0.88f || currentSpeed < 0.25f)
                {
                    if (player.Motor != null) player.Motor.Velocity = Vector3.zero;
                    var audioCtrl = player.GetComponent<PlayerAudioController>();
                    if (audioCtrl != null) audioCtrl.StopFootsteps(immediate: false);
                }

                // Responsive Villager Greeting Triggers (Based on village positions: Bác Ba ~149.2, Bé Tí ~151.5, Bé Tư ~161.0)
                if (!bacBaWaved && (pos.x >= 147.5f || progress >= 0.35f))
                {
                    bacBaWaved = true;
                    if (welcomingVillagerAnimator != null) welcomingVillagerAnimator.CrossFadeInFixedTime("Wave", 0.35f);
                }

                if (!tiWaved && (pos.x >= 150.0f || progress >= 0.65f))
                {
                    tiWaved = true;
                    if (tiAnim != null) tiAnim.CrossFadeInFixedTime("Wave", 0.35f);
                }

                if (!betuWaved && (pos.x >= 152.0f || progress >= 0.85f))
                {
                    betuWaved = true;
                    if (betuAnim != null) betuAnim.CrossFadeInFixedTime("Wave", 0.35f);
                }

                yield return null;
            }

            // Ensure player is cleanly positioned at target square
            Vector3 finalPos = player.transform.position;
            finalPos.x = targetRunToX;
            finalPos.z = 0f;
            if (Physics.Raycast(new Vector3(finalPos.x, finalPos.y + 1.2f, 0f), Vector3.down, out RaycastHit finalHit, 4.0f, ~0, QueryTriggerInteraction.Ignore))
            {
                finalPos.y = finalHit.point.y;
            }
            player.transform.position = finalPos;

            // Ngắt hoàn toàn tiếng bước chân và triệt tiêu vận tốc ngay khi dừng lại ở quảng trường làng
            if (player.Motor != null) player.Motor.Velocity = Vector3.zero;
            var villageAudioCtrl = player.GetComponent<PlayerAudioController>();
            if (villageAudioCtrl != null) villageAudioCtrl.StopFootsteps(immediate: true);

            // 5. Village Square Wonder & Greeting Phase (~5.5s)
            // Settle in Idle first (0.6s)
            if (anAnimCtrl != null)
            {
                anAnimCtrl.PlayCutsceneAnimation("Idle", 0.20f);
                anAnimCtrl.SetCutsceneSpeed(0f);
            }
            else if (anAnim != null)
            {
                anAnim.SetFloat("Speed", 0f);
                anAnim.CrossFadeInFixedTime("Idle", 0.20f);
            }
            if (villageAudioCtrl != null) villageAudioCtrl.StopFootsteps(immediate: true);

            yield return new WaitForSeconds(0.6f);

            // An waves back warmly to Bác Ba and the children (2.4s)
            if (anAnimCtrl != null)
            {
                anAnimCtrl.PlayCutsceneAnimation("Wave", 0.30f);
            }
            else if (anAnim != null && anAnim.HasState(0, Animator.StringToHash("Wave")))
            {
                anAnim.CrossFadeInFixedTime("Wave", 0.30f);
            }

            yield return new WaitForSeconds(2.4f);

            // An looks around in wonder at the harvest paddies and stilted houses (2.2s)
            if (anAnimCtrl != null)
            {
                anAnimCtrl.PlayCutsceneAnimation("LookAround", 0.35f);
            }
            else if (anAnim != null && anAnim.HasState(0, Animator.StringToHash("LookAround")))
            {
                anAnim.CrossFadeInFixedTime("LookAround", 0.35f);
            }

            // Fade out caption text gently
            StartCoroutine(FadeCanvasGroupRoutine(captionGroup, 1f, 0f, 1.0f));

            yield return new WaitForSeconds(2.2f);

            // Crossfade An back to natural Idle before handover (0.6s)
            if (anAnimCtrl != null)
            {
                anAnimCtrl.PlayCutsceneAnimation("Idle", 0.35f);
            }
            else if (anAnim != null)
            {
                anAnim.CrossFadeInFixedTime("Idle", 0.35f);
            }

            yield return new WaitForSeconds(0.6f);

            // 6. Seamless Control Handover (Planet of Lana Standard: ZERO POP / ZERO GLITCH)
            // Retract letterbox bars smoothly
            StartCoroutine(AnimateLetterboxRoutine(false, 1.2f));

            // Return camera gently to gameplay camera with smooth Cinemachine ease
            if (vcamVillageWide != null)
            {
                vcamVillageWide.Priority = 0;
            }

            // Wait 1.0s for the letterbox bars and camera blend to ease into comfortable gameplay view
            yield return new WaitForSeconds(1.0f);

            if (cutsceneCanvas != null) cutsceneCanvas.gameObject.SetActive(false);

            // Reset motor velocity, cutscene flag, and restore player control
            if (player.Motor != null) player.Motor.Velocity = Vector3.zero;
            if (anAnimCtrl != null) anAnimCtrl.IsCutsceneActive = false;
            if (cc != null) cc.enabled = true;
            player.enabled = true;
            isRunningCutscene = false;

            // Save checkpoint at the Village Square
            if (CheckpointManager.Instance != null)
            {
                CheckpointManager.Instance.SetCheckpoint(player.transform.position, 2);
            }

            Debug.Log("<color=#10b981>[VillageArrivalCutscene]</color> Smooth village arrival complete. Control restored to An.");
        }

        private IEnumerator AnimateLetterboxRoutine(bool show, float duration)
        {
            float t = 0f;
            float startAlpha = show ? 0f : 1f;
            float endAlpha = show ? 1f : 0f;
            float startBarH = show ? 0f : 96f;
            float endBarH = show ? 96f : 0f;

            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / Mathf.Max(0.01f, duration));
                if (canvasGroup != null) canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, k);
                if (show && captionGroup != null) captionGroup.alpha = Mathf.Lerp(0f, 1f, Mathf.Clamp01((t - 0.3f) / (duration - 0.3f)));
                float bh = Mathf.Lerp(startBarH, endBarH, k);
                if (topBar != null) topBar.sizeDelta = new Vector2(0f, bh);
                if (bottomBar != null) bottomBar.sizeDelta = new Vector2(0f, bh);
                yield return null;
            }

            if (canvasGroup != null) canvasGroup.alpha = endAlpha;
            if (show && captionGroup != null) captionGroup.alpha = 1f;
        }

        private IEnumerator FadeCanvasGroupRoutine(CanvasGroup group, float from, float to, float duration)
        {
            if (group == null) yield break;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                group.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            group.alpha = to;
        }

        private void BuildLetterboxUI()
        {
            var go = new GameObject("Canvas_VillageArrivalCutscene", typeof(Canvas), typeof(CanvasScaler));
            go.transform.SetParent(transform, false);
            cutsceneCanvas = go.GetComponent<Canvas>();
            cutsceneCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cutsceneCanvas.sortingOrder = 880;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGroup = go.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;

            topBar = CreateBar("TopLetterbox", go.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));
            bottomBar = CreateBar("BottomLetterbox", go.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f));

            var capGO = new GameObject("CaptionCluster", typeof(RectTransform), typeof(CanvasGroup));
            capGO.transform.SetParent(go.transform, false);
            var capRect = capGO.GetComponent<RectTransform>();
            capRect.anchorMin = new Vector2(0.12f, 0.015f);
            capRect.anchorMax = new Vector2(0.88f, 0.082f);
            capRect.offsetMin = Vector2.zero;
            capRect.offsetMax = Vector2.zero;
            captionGroup = capGO.GetComponent<CanvasGroup>();
            captionGroup.alpha = 0f;

            if (fontAsset == null)
            {
#if UNITY_EDITOR
                fontAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/SegoeUI_SDF_Dynamic.asset");
#endif
                if (fontAsset == null) fontAsset = TMP_Settings.defaultFontAsset;
            }

            // Title: Bold, warm golden sunlight
            var titleGO = new GameObject("VillageTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(capRect, false);
            var tr = titleGO.GetComponent<RectTransform>();
            tr.anchorMin = new Vector2(0f, 0.52f);
            tr.anchorMax = new Vector2(1f, 1f);
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;
            titleText = titleGO.GetComponent<TextMeshProUGUI>();
            if (fontAsset != null) titleText.font = fontAsset;
            titleText.fontSize = 20;
            titleText.fontStyle = FontStyles.Bold;
            titleText.characterSpacing = 2.5f;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = new Color(0.99f, 0.89f, 0.60f, 0.96f);
            titleText.text = villageTitle;

            // Subtitle: Italic, warm historical cream
            var subGO = new GameObject("VillageSubtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            subGO.transform.SetParent(capRect, false);
            var sr = subGO.GetComponent<RectTransform>();
            sr.anchorMin = new Vector2(0f, 0f);
            sr.anchorMax = new Vector2(1f, 0.52f);
            sr.offsetMin = Vector2.zero;
            sr.offsetMax = Vector2.zero;
            subText = subGO.GetComponent<TextMeshProUGUI>();
            if (fontAsset != null) subText.font = fontAsset;
            subText.fontSize = 17;
            subText.fontStyle = FontStyles.Italic;
            subText.alignment = TextAlignmentOptions.Center;
            subText.color = new Color(0.95f, 0.94f, 0.89f, 0.90f);
            subText.text = villageSubtitle;

            go.SetActive(false);
        }

        private static RectTransform CreateBar(string name, Transform parent, Vector2 min, Vector2 max, Vector2 pivot)
        {
            var barGO = new GameObject(name, typeof(RectTransform), typeof(Image));
            barGO.transform.SetParent(parent, false);
            var rt = barGO.GetComponent<RectTransform>();
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = pivot;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, 96f);
            var img = barGO.GetComponent<Image>();
            img.color = new Color(0.02f, 0.02f, 0.03f, 0.96f);
            img.raycastTarget = false;
            return rt;
        }
    }
}
