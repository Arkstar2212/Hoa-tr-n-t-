#pragma warning disable 0414
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Minimalist, contextual in-game prompt UI for "Hoa trên Đất Đỏ" strictly adhering to Planet of Lana aesthetic.
    /// Supports both 2.5D Overhaul and 3D Ca Mau 1954 Village under Input System & Legacy Input.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlanetOfLanaPromptSystem : MonoBehaviour
    {
        public static PlanetOfLanaPromptSystem Instance { get; private set; }

        public enum PromptType
        {
            Move,       // [A] [D] / [WASD]
            Jump,       // [Space]
            Crawl,      // [S] / [C]
            ClimbLedge, // [W]
            Interact,   // [E]
            RopeGrab,   // [F]
            PushPull    // [E] / [Hold LMB]
        }

        [Header("Appearance & Timing")]
        [SerializeField] private float fadeDuration = 0.35f;
        [SerializeField] private float autoHideDuration = 4.5f;
        [SerializeField] private Vector3 screenOffset = new Vector3(0f, 85f, 0f);

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
            StartCoroutine(ShowInitialMovementPrompt());
        }

        private IEnumerator ShowInitialMovementPrompt()
        {
            // Initial movement prompts are only for 2D tutorial (managed by PlanetOfLanaTutorialController).
            // Do not show in 3D Village or any scene without 2D PlayerController.
            yield break;
        }

        private void Update()
        {
            if (!isShowing) return;

            if (trackedWorldTarget != null && Camera.main != null)
            {
                Vector3 screenPos = Camera.main.WorldToScreenPoint(trackedWorldTarget.position + Vector3.up * 1.5f) + screenOffset;
                if (screenPos.z > 0f)
                {
                    screenPos.x = Mathf.Clamp(screenPos.x, 140f, Screen.width - 140f);
                    screenPos.y = Mathf.Clamp(screenPos.y, 80f, Screen.height - 80f);
                    promptRootRect.position = screenPos;
                }
            }

            if (keyCapText != null && keyCapText.text.Contains("A") && HasMoveInput())
            {
                HidePrompt();
            }
        }

        private static bool HasMoveInput()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.aKey.isPressed || kb.dKey.isPressed || kb.wKey.isPressed || kb.sKey.isPressed ||
                               kb.leftArrowKey.isPressed || kb.rightArrowKey.isPressed))
            {
                return true;
            }
            return false;
#else
            return Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.05f;
#endif
        }

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

            string keyString = "[ A ] [ D ]";
            switch (type)
            {
                case PromptType.Move: keyString = "[ A ]  [ D ]"; break;
                case PromptType.Jump: keyString = "[ SPACE ]"; break;
                case PromptType.Crawl: keyString = "[ C ]"; break;
                case PromptType.ClimbLedge: keyString = "[ W ]"; break;
                case PromptType.Interact: keyString = "[ E ]"; break;
                case PromptType.RopeGrab: keyString = "[ F ]"; break;
                case PromptType.PushPull: keyString = "[ E ]"; break;
            }

            if (keyCapText != null) keyCapText.text = keyString;
            if (actionLabelText != null)
            {
                bool hasLabel = !string.IsNullOrEmpty(actionLabel);
                actionLabelText.text = hasLabel ? actionLabel.ToUpper() : "";
                if (actionPillBg != null) actionPillBg.gameObject.SetActive(hasLabel);
            }

            if (trackedWorldTarget == null)
            {
                promptRootRect.anchorMin = new Vector2(0.5f, 0.12f);
                promptRootRect.anchorMax = new Vector2(0.5f, 0.12f);
                promptRootRect.anchoredPosition = Vector2.zero;
            }

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                promptCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }
            promptCanvasGroup.alpha = 1f;

            yield return new WaitForSeconds(Mathf.Max(0.5f, duration));
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
            var existing = transform.Find("PlanetOfLanaPromptCanvas");
            if (existing != null)
            {
                if (Application.isPlaying) Destroy(existing.gameObject);
                else DestroyImmediate(existing.gameObject);
            }

            GameObject canvasGo = new GameObject("PlanetOfLanaPromptCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.hideFlags = HideFlags.DontSave;
            canvasGo.transform.SetParent(transform, false);

            promptCanvas = canvasGo.GetComponent<Canvas>();
            promptCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            promptCanvas.overrideSorting = true;
            promptCanvas.sortingOrder = 32700; // ALWAYS ON TOP OF EVERYTHING ELSE

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

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

            Font promptFont = null;
            try { promptFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
            if (promptFont == null)
            {
                try { promptFont = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { }
            }

            GameObject keyBadgeGo = new GameObject("KeyBadge", typeof(Image), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            keyBadgeGo.transform.SetParent(rootGo.transform, false);
            keyCapBg = keyBadgeGo.GetComponent<Image>();
            keyCapBg.color = new Color(1f, 1f, 1f, 0.95f);

            var keyLayout = keyBadgeGo.GetComponent<HorizontalLayoutGroup>();
            keyLayout.padding = new RectOffset(12, 12, 6, 6);
            keyLayout.childAlignment = TextAnchor.MiddleCenter;
            var keyFitter = keyBadgeGo.GetComponent<ContentSizeFitter>();
            keyFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            keyFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject keyTextGo = new GameObject("KeyText", typeof(Text));
            keyTextGo.transform.SetParent(keyBadgeGo.transform, false);
            keyCapText = keyTextGo.GetComponent<Text>();
            keyCapText.font = promptFont;
            keyCapText.fontSize = 15;
            keyCapText.fontStyle = FontStyle.Bold;
            keyCapText.alignment = TextAnchor.MiddleCenter;
            keyCapText.color = new Color(0.05f, 0.08f, 0.14f, 1.0f);
            keyCapText.text = "[ A ]  [ D ]";

            GameObject actionGo = new GameObject("ActionBadge", typeof(Image), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            actionGo.transform.SetParent(rootGo.transform, false);
            actionPillBg = actionGo.GetComponent<Image>();
            actionPillBg.color = new Color(0.04f, 0.07f, 0.12f, 0.75f);

            var actLayout = actionGo.GetComponent<HorizontalLayoutGroup>();
            actLayout.padding = new RectOffset(14, 14, 6, 6);
            actLayout.childAlignment = TextAnchor.MiddleCenter;
            var actFitter = actionGo.GetComponent<ContentSizeFitter>();
            actFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            actFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject actTextGo = new GameObject("ActionText", typeof(Text));
            actTextGo.transform.SetParent(actionGo.transform, false);
            actionLabelText = actTextGo.GetComponent<Text>();
            actionLabelText.font = promptFont;
            actionLabelText.fontSize = 14;
            actionLabelText.alignment = TextAnchor.MiddleCenter;
            actionLabelText.color = new Color(0.95f, 0.95f, 0.95f, 0.95f);
            actionLabelText.text = "DI CHUYỂN";
        }
    }
}
