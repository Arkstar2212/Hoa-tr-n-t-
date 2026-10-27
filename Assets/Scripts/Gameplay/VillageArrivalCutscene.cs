using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Cinematic Wide-Zoom Cutscene triggered when An finishes crossing the Golden Rice Field
    /// and runs into the living village surrounded by golden harvest paddies.
    /// - Zooms camera out wide (VCam_VillageArrivalWide) to frame the golden rice fields and stilted houses.
    /// - Automatically runs An into the village entrance (X: 94m -> 104.5m) while villagers wave in welcome.
    /// - Smoothly hands control back to the player inside the village.
    /// </summary>
    [DisallowMultipleComponent]
    public class VillageArrivalCutscene : MonoBehaviour
    {
        [Header("Cutscene Trigger & Path")]
        [SerializeField] private float triggerX = 94.0f;
        [SerializeField] private float targetRunToX = 104.5f;
        [SerializeField] private float autoRunSpeed = 3.5f;
        [SerializeField] private string villageTitle = "XÓM RẠCH GỐC  •  MÙA LÚA CHÍN 1954";
        [SerializeField] private string villageSubtitle = "Hương lúa mới trải dài quanh những mái nhà sàn bình yên trước ngày tập kết.";

        [Header("References")]
        [SerializeField] private CinemachineCamera vcamVillageWide;
        [SerializeField] private Animator welcomingVillagerAnimator;

        private bool hasTriggered = false;
        private bool isRunningCutscene = false;
        private PlayerController player;

        // Runtime Letterbox UI
        private Canvas cutsceneCanvas;
        private CanvasGroup canvasGroup;
        private RectTransform topBar;
        private RectTransform bottomBar;
        private CanvasGroup captionGroup;
        private Text titleText;
        private Text subText;

        private void Start()
        {
            player = Object.FindAnyObjectByType<PlayerController>();
            if (vcamVillageWide == null)
            {
                var found = GameObject.Find("VCam_VillageArrivalWide");
                if (found != null) vcamVillageWide = found.GetComponent<CinemachineCamera>();
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

            if (player.transform.position.x >= triggerX && player.transform.position.x <= triggerX + 4.0f)
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

            // 1. Lock player input while driving An's character controller into the village
            CharacterController cc = player.GetComponent<CharacterController>();
            Animator anAnim = player.GetComponentInChildren<Animator>();
            player.SetFacingDirection(1f);
            player.enabled = false;

            // 2. Activate Wide-Zoom Village Panorama Camera
            if (vcamVillageWide != null)
            {
                vcamVillageWide.gameObject.SetActive(true);
                vcamVillageWide.Priority = 35;
            }

            // 3. Trigger welcoming villager wave animation
            if (welcomingVillagerAnimator != null)
            {
                welcomingVillagerAnimator.CrossFadeInFixedTime("Wave", 0.25f);
            }

            // 4. Show cinematic letterbox bars & village title
            if (cutsceneCanvas != null) cutsceneCanvas.gameObject.SetActive(true);
            StartCoroutine(AnimateLetterboxRoutine(true, 0.75f));

            // 5. Auto-run An into the village surrounded by golden rice fields
            if (anAnim != null)
            {
                anAnim.SetBool("IsGrounded", true);
                anAnim.SetBool("IsCrouching", false);
                anAnim.SetFloat("Speed", autoRunSpeed);
                anAnim.CrossFadeInFixedTime("Run", 0.2f);
            }

            float timeout = 5.5f;
            float elapsed = 0f;
            while (player.transform.position.x < targetRunToX && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                float stepX = autoRunSpeed * Time.deltaTime;
                if (cc != null && cc.enabled)
                {
                    cc.Move(new Vector3(stepX, -9.8f * Time.deltaTime, 0f));
                }
                else
                {
                    player.transform.position += new Vector3(stepX, 0f, 0f);
                }

                // Keep Z strictly at 0
                Vector3 pos = player.transform.position;
                pos.z = 0f;
                player.transform.position = pos;

                yield return null;
            }

            // 6. An stops in the village square and waves / looks around warmly
            if (anAnim != null)
            {
                anAnim.SetFloat("Speed", 0f);
                anAnim.CrossFadeInFixedTime("Idle", 0.25f);
            }

            yield return new WaitForSeconds(1.35f);

            // 7. Return camera smoothly to gameplay & hide letterbox bars
            if (vcamVillageWide != null)
            {
                vcamVillageWide.Priority = 0;
            }

            yield return StartCoroutine(AnimateLetterboxRoutine(false, 0.75f));
            if (cutsceneCanvas != null) cutsceneCanvas.gameObject.SetActive(false);

            // 8. Restore player control
            player.enabled = true;
            isRunningCutscene = false;

            // Save checkpoint at the Village Entrance
            if (CheckpointManager.Instance != null)
            {
                CheckpointManager.Instance.SetCheckpoint(player.transform.position, 2);
            }
        }

        private IEnumerator AnimateLetterboxRoutine(bool show, float duration)
        {
            float t = 0f;
            float startAlpha = show ? 0f : 1f;
            float endAlpha = show ? 1f : 0f;
            float startBarH = show ? 0f : 88f;
            float endBarH = show ? 88f : 0f;

            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / Mathf.Max(0.01f, duration));
                if (canvasGroup != null) canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, k);
                if (captionGroup != null) captionGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, k);
                float bh = Mathf.Lerp(startBarH, endBarH, k);
                if (topBar != null) topBar.sizeDelta = new Vector2(0f, bh);
                if (bottomBar != null) bottomBar.sizeDelta = new Vector2(0f, bh);
                yield return null;
            }

            if (canvasGroup != null) canvasGroup.alpha = endAlpha;
            if (captionGroup != null) captionGroup.alpha = endAlpha;
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
            capRect.anchorMin = new Vector2(0.15f, 0.01f);
            capRect.anchorMax = new Vector2(0.85f, 0.08f);
            capRect.offsetMin = Vector2.zero;
            capRect.offsetMax = Vector2.zero;
            captionGroup = capGO.GetComponent<CanvasGroup>();
            captionGroup.alpha = 0f;

            Font uiFont = Font.CreateDynamicFontFromOSFont("Segoe UI", 24) ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var titleGO = new GameObject("VillageTitle", typeof(RectTransform), typeof(Text));
            titleGO.transform.SetParent(capRect, false);
            var tr = titleGO.GetComponent<RectTransform>();
            tr.anchorMin = new Vector2(0f, 0.52f);
            tr.anchorMax = new Vector2(1f, 1f);
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;
            titleText = titleGO.GetComponent<Text>();
            titleText.font = uiFont;
            titleText.fontSize = 16;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new Color(0.98f, 0.88f, 0.62f, 0.95f);
            titleText.text = villageTitle;

            var subGO = new GameObject("VillageSubtitle", typeof(RectTransform), typeof(Text));
            subGO.transform.SetParent(capRect, false);
            var sr = subGO.GetComponent<RectTransform>();
            sr.anchorMin = new Vector2(0f, 0f);
            sr.anchorMax = new Vector2(1f, 0.52f);
            sr.offsetMin = Vector2.zero;
            sr.offsetMax = Vector2.zero;
            subText = subGO.GetComponent<Text>();
            subText.font = uiFont;
            subText.fontSize = 18;
            subText.fontStyle = FontStyle.Italic;
            subText.alignment = TextAnchor.MiddleCenter;
            subText.color = new Color(0.96f, 0.95f, 0.90f, 0.92f);
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
            rt.sizeDelta = new Vector2(0f, 88f);
            var img = barGO.GetComponent<Image>();
            img.color = new Color(0.02f, 0.02f, 0.03f, 0.96f);
            img.raycastTarget = false;
            return rt;
        }
    }
}
