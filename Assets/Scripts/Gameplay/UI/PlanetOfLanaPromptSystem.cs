using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Minimalist, contextual in-game prompt UI for "Hoa trên Đất Đỏ" strictly adhering to Planet of Lana aesthetic.
    /// Displays elegant rounded keycap icons with simplified contextual action indicators.
    /// Never clutters the screen with static text boxes.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlanetOfLanaPromptSystem : MonoBehaviour
    {
        public static PlanetOfLanaPromptSystem Instance { get; private set; }

        public enum PromptType
        {
            Move,       // [A] [D]
            Jump,       // [Space]
            Crawl,      // [S] / [C]
            ClimbLedge, // [W]
            Interact,   // [E]
            RopeGrab,   // [F]
            PushPull    // [Hold LMB]
        }

        [Header("Appearance & Timing")]
        [SerializeField] private float fadeDuration = 0.35f;
        [SerializeField] private float autoHideDuration = 4.5f;
        [SerializeField] private Vector3 screenOffset = new Vector3(0f, 85f, 0f);

        // Internal Runtime UI
        private Canvas promptCanvas;
        private CanvasGroup promptCanvasGroup;
        private RectTransform promptRootRect;
        private Text keyCapText;
        private Text actionLabelText;
        private Image keyCapBg;
        private Image actionPillBg;

        private Coroutine activePromptRoutine;
        private Transform trackedWorldTarget;
        private bool isShowing = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            BuildRuntimeUI();
        }

        private void Start()
        {
            // Show initial movement prompt at game start
            StartCoroutine(ShowInitialMovementPrompt());
        }

        private IEnumerator ShowInitialMovementPrompt()
        {
            yield return new WaitForSeconds(1.5f);
            ShowPrompt(PromptType.Move, "DI CHUYỂN", null, 6f);
        }

        private void Update()
        {
            if (!isShowing) return;

            // If tracking an in-world object, project to screen coordinates
            if (trackedWorldTarget != null && Camera.main != null)
            {
                Vector3 screenPos = Camera.main.WorldToScreenPoint(trackedWorldTarget.position + screenOffset * 0.01f);
                if (screenPos.z > 0)
                {
                    promptRootRect.position = screenPos;
                }
            }

            // Dismiss Move prompt as soon as player walks
            if (keyCapText != null && keyCapText.text.Contains("A") && (Input.GetAxisRaw("Horizontal") != 0))
            {
                HidePrompt();
            }
        }

        /// <summary>
        /// Display a minimalist key prompt badge in Planet of Lana style.
        /// </summary>
        public void ShowPrompt(PromptType type, string actionLabel, Transform worldAnchor = null, float duration = 4.0f)
        {
            if (activePromptRoutine != null) StopCoroutine(activePromptRoutine);
            activePromptRoutine = StartCoroutine(ShowPromptRoutine(type, actionLabel, worldAnchor, duration));
        }

        public void HidePrompt()
        {
            if (!isShowing) return;
            if (activePromptRoutine != null) StopCoroutine(activePromptRoutine);
            activePromptRoutine = StartCoroutine(FadeOutRoutine());
        }

        private IEnumerator ShowPromptRoutine(PromptType type, string actionLabel, Transform worldAnchor, float duration)
        {
            isShowing = true;
            trackedWorldTarget = worldAnchor;

            // Configure Keycap Label according to type
            string keyString = "[ A ] [ D ]";
            switch (type)
            {
                case PromptType.Move: keyString = "[ A ]  [ D ]"; break;
                case PromptType.Jump: keyString = "[ SPACE ]"; break;
                case PromptType.Crawl: keyString = "[ S ]"; break;
                case PromptType.ClimbLedge: keyString = "[ W ]"; break;
                case PromptType.Interact: keyString = "[ E ]"; break;
                case PromptType.RopeGrab: keyString = "[ F ]"; break;
                case PromptType.PushPull: keyString = "[ GIỮ CHUỘT TRÁI ]"; break;
            }

            if (keyCapText != null) keyCapText.text = keyString;
            if (actionLabelText != null) actionLabelText.text = actionLabel.ToUpper();

            // Default to bottom center if no world anchor
            if (trackedWorldTarget == null)
            {
                promptRootRect.anchorMin = new Vector2(0.5f, 0.12f);
                promptRootRect.anchorMax = new Vector2(0.5f, 0.12f);
                promptRootRect.anchoredPosition = Vector2.zero;
            }

            // Fade In
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                promptCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }
            promptCanvasGroup.alpha = 1f;

            // Hold
            yield return new WaitForSeconds(duration);

            // Fade Out
            yield return FadeOutRoutine();
        }

        private IEnumerator FadeOutRoutine()
        {
            float elapsed = 0f;
            float startAlpha = promptCanvasGroup.alpha;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                promptCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeDuration);
                yield return null;
            }
            promptCanvasGroup.alpha = 0f;
            isShowing = false;
            trackedWorldTarget = null;
        }

        private void BuildRuntimeUI()
        {
            GameObject canvasGo = new GameObject("PlanetOfLanaPromptCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            promptCanvas = canvasGo.GetComponent<Canvas>();
            promptCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            promptCanvas.sortingOrder = 950;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // Root Badge Container
            GameObject rootGo = new GameObject("PromptBadgeRoot", typeof(RectTransform), typeof(CanvasGroup), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            rootGo.transform.SetParent(canvasGo.transform, false);

            promptRootRect = rootGo.GetComponent<RectTransform>();
            promptRootRect.anchorMin = new Vector2(0.5f, 0.12f);
            promptRootRect.anchorMax = new Vector2(0.5f, 0.12f);
            promptRootRect.pivot = new Vector2(0.5f, 0.5f);
            promptRootRect.sizeDelta = new Vector2(260f, 44f);

            promptCanvasGroup = rootGo.GetComponent<CanvasGroup>();
            promptCanvasGroup.alpha = 0f;
            promptCanvasGroup.blocksRaycasts = false;

            var layout = rootGo.GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var fitter = rootGo.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Font arialFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // 1. Key Badge (Solid White Pill with Dark Key Icon)
            GameObject keyBadgeGo = new GameObject("KeyBadge", typeof(Image), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            keyBadgeGo.transform.SetParent(rootGo.transform, false);
            keyCapBg = keyBadgeGo.GetComponent<Image>();
            keyCapBg.color = new Color(1f, 1f, 1f, 0.95f); // Crisp white keycap

            var keyLayout = keyBadgeGo.GetComponent<HorizontalLayoutGroup>();
            keyLayout.padding = new RectOffset(12, 12, 6, 6);
            keyLayout.childAlignment = TextAnchor.MiddleCenter;
            var keyFitter = keyBadgeGo.GetComponent<ContentSizeFitter>();
            keyFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            keyFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject keyTextGo = new GameObject("KeyText", typeof(Text));
            keyTextGo.transform.SetParent(keyBadgeGo.transform, false);
            keyCapText = keyTextGo.GetComponent<Text>();
            keyCapText.font = arialFont;
            keyCapText.fontSize = 15;
            keyCapText.fontStyle = FontStyle.Bold;
            keyCapText.alignment = TextAnchor.MiddleCenter;
            keyCapText.color = new Color(0.05f, 0.08f, 0.14f, 1.0f); // Dark background navy contrast
            keyCapText.text = "[ A ]  [ D ]";

            // 2. Action Label (Translucent Dark Pill with White Text)
            GameObject actionGo = new GameObject("ActionBadge", typeof(Image), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            actionGo.transform.SetParent(rootGo.transform, false);
            actionPillBg = actionGo.GetComponent<Image>();
            actionPillBg.color = new Color(0.04f, 0.07f, 0.12f, 0.75f); // Deep navy translucent pill

            var actLayout = actionGo.GetComponent<HorizontalLayoutGroup>();
            actLayout.padding = new RectOffset(14, 14, 6, 6);
            actLayout.childAlignment = TextAnchor.MiddleCenter;
            var actFitter = actionGo.GetComponent<ContentSizeFitter>();
            actFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            actFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject actTextGo = new GameObject("ActionText", typeof(Text));
            actTextGo.transform.SetParent(actionGo.transform, false);
            actionLabelText = actTextGo.GetComponent<Text>();
            actionLabelText.font = arialFont;
            actionLabelText.fontSize = 14;
            actionLabelText.alignment = TextAnchor.MiddleCenter;
            actionLabelText.color = new Color(0.95f, 0.95f, 0.95f, 0.95f);
            actionLabelText.text = "DI CHUYỂN";
        }
    }
}
