using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Planet of Lana Style Diegetic / Cinematic Tutorial Prompts for Scene 1.
    /// Characteristics:
    /// - Minimalist, non-intrusive floating HUD capsules at bottom-center.
    /// - Beautiful key glyphs [A] [D] [Space] [Shift] [E] matching KeybindingManager.
    /// - Smooth Alpha fade-in (0.8s), hold, and fade-out (0.8s).
    /// - Naturally dismisses once the player executes the instructed action.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlanetOfLanaTutorialController : MonoBehaviour
    {
        public static PlanetOfLanaTutorialController Instance { get; private set; }

        private Canvas tutorialCanvas;
        private CanvasGroup promptCanvasGroup;
        private Text promptTitleText;
        private Text promptKeysText;
        private Text promptDescText;

        private PlayerController player;

        // Tutorial Progression State
        private bool hasMoved = false;
        private float moveTime = 0f;
        private bool moveTutorialDismissed = false;

        private bool jumpTutorialTriggered = false;
        private bool jumpTutorialDismissed = false;

        private bool interactTutorialTriggered = false;
        private bool interactTutorialDismissed = false;

        private Coroutine activeFadeRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            BuildTutorialUI();
        }

        private void Start()
        {
            player = Object.FindAnyObjectByType<PlayerController>();
            KeybindingManager.OnKeybindingsChanged += RefreshCurrentPromptKeys;

            // Wait a small delay after scene start / intro cutscene before showing the first move hint
            StartCoroutine(InitialPromptRoutine());
        }

        private void OnDestroy()
        {
            KeybindingManager.OnKeybindingsChanged -= RefreshCurrentPromptKeys;
        }

        private IEnumerator InitialPromptRoutine()
        {
            // Give time for intro cutscene or player to settle
            yield return new WaitForSeconds(1.5f);

            // Wait until player controller is enabled (cutscene done or bypassed)
            while (player != null && !player.enabled)
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.6f);

            if (!moveTutorialDismissed && !hasMoved)
            {
                ShowPrompt(
                    "HƯỚNG DẪN DI CHUYỂN",
                    $"[ {KeybindingManager.GetKeyDisplayName(KeybindingManager.GetKey(GameAction.MoveLeft))} ]   [ {KeybindingManager.GetKeyDisplayName(KeybindingManager.GetKey(GameAction.MoveRight))} ]",
                    "Nhấn để bước đi trong bóng râm"
                );
            }
        }

        private void Update()
        {
            if (player == null)
            {
                player = Object.FindAnyObjectByType<PlayerController>();
                if (player == null) return;
            }

            if (!player.enabled) return;

            float x = player.transform.position.x;

            // 1. Monitor Movement Tutorial
            if (!moveTutorialDismissed)
            {
                if (Mathf.Abs(player.HorizontalInput) > 0.1f)
                {
                    moveTime += Time.deltaTime;
                    if (moveTime > 1.4f)
                    {
                        hasMoved = true;
                        moveTutorialDismissed = true;
                        HidePrompt();
                    }
                }
            }

            // 2. Monitor Sprint & Leap Tutorial (around X: 10m to 18m natural dip)
            if (!jumpTutorialTriggered && !jumpTutorialDismissed && x >= 9.5f && x <= 22f)
            {
                jumpTutorialTriggered = true;
                string shiftKey = KeybindingManager.GetKeyDisplayName(KeybindingManager.GetKey(GameAction.Sprint));
                string spaceKey = KeybindingManager.GetKeyDisplayName(KeybindingManager.GetKey(GameAction.Jump));
                ShowPrompt(
                    "CHẠY LẤY ĐÀ & BẬT NHẢY",
                    $"[ {shiftKey} ] + [ {spaceKey} ]",
                    "Giữ chạy nước rút và nhấn nhảy để phóng xa vượt trội"
                );
            }

            if (jumpTutorialTriggered && !jumpTutorialDismissed)
            {
                if (player.CurrentState == PlayerState.Airborne || player.IsSprinting || x > 23f)
                {
                    jumpTutorialDismissed = true;
                    HidePrompt();
                }
            }

            // 3. Monitor Interaction Tutorial (around X: 22m Gourd Trellis garden)
            if (!interactTutorialTriggered && !interactTutorialDismissed && x >= 22.5f && x <= 29f)
            {
                interactTutorialTriggered = true;
                string eKey = KeybindingManager.GetKeyDisplayName(KeybindingManager.GetKey(GameAction.Interact));
                ShowPrompt(
                    "TƯƠNG TÁC KHÁM PHÁ",
                    $"[ {eKey} ]",
                    "Nhặt đồ vật / Đẩy kéo thùng gỗ dưới giàn bầu"
                );
            }

            if (interactTutorialTriggered && !interactTutorialDismissed)
            {
                if (x > 31f)
                {
                    interactTutorialDismissed = true;
                    HidePrompt();
                }
            }
        }

        private void RefreshCurrentPromptKeys()
        {
            // Update keys dynamically if rebinding happens mid-game
            if (promptCanvasGroup != null && promptCanvasGroup.alpha > 0.05f)
            {
                if (jumpTutorialTriggered && !jumpTutorialDismissed)
                {
                    string shiftKey = KeybindingManager.GetKeyDisplayName(KeybindingManager.GetKey(GameAction.Sprint));
                    string spaceKey = KeybindingManager.GetKeyDisplayName(KeybindingManager.GetKey(GameAction.Jump));
                    promptKeysText.text = $"[ {shiftKey} ] + [ {spaceKey} ]";
                }
                else if (!moveTutorialDismissed)
                {
                    promptKeysText.text = $"[ {KeybindingManager.GetKeyDisplayName(KeybindingManager.GetKey(GameAction.MoveLeft))} ]   [ {KeybindingManager.GetKeyDisplayName(KeybindingManager.GetKey(GameAction.MoveRight))} ]";
                }
            }
        }

        public void ShowPrompt(string title, string keys, string desc)
        {
            if (promptTitleText != null) promptTitleText.text = title;
            if (promptKeysText != null) promptKeysText.text = keys;
            if (promptDescText != null) promptDescText.text = desc;

            if (activeFadeRoutine != null) StopCoroutine(activeFadeRoutine);
            activeFadeRoutine = StartCoroutine(FadePromptRoutine(1f, 0.8f));
        }

        public void HidePrompt()
        {
            if (activeFadeRoutine != null) StopCoroutine(activeFadeRoutine);
            activeFadeRoutine = StartCoroutine(FadePromptRoutine(0f, 0.8f));
        }

        private IEnumerator FadePromptRoutine(float targetAlpha, float duration)
        {
            if (promptCanvasGroup == null) yield break;
            float startAlpha = promptCanvasGroup.alpha;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                promptCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t / duration);
                yield return null;
            }
            promptCanvasGroup.alpha = targetAlpha;
        }

        // =====================================================================================
        // UI BLUEPRINT
        // =====================================================================================
        private void BuildTutorialUI()
        {
            var canvasGO = new GameObject("Canvas_PlanetOfLana_Tutorial");
            canvasGO.transform.SetParent(transform, false);

            tutorialCanvas = canvasGO.AddComponent<Canvas>();
            tutorialCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            tutorialCanvas.sortingOrder = 450; // Just under Pause Menu

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            Font font = Font.CreateDynamicFontFromOSFont("Segoe UI", 36);
            if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 36);
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Pill Container (Bottom Center)
            var pillRect = CreateRect("Tutorial_Pill_Container", canvasGO.transform);
            pillRect.anchorMin = pillRect.anchorMax = new Vector2(0.5f, 0.14f);
            pillRect.pivot = new Vector2(0.5f, 0.5f);
            pillRect.sizeDelta = new Vector2(620f, 96f);

            promptCanvasGroup = pillRect.gameObject.AddComponent<CanvasGroup>();
            promptCanvasGroup.alpha = 0f;
            promptCanvasGroup.blocksRaycasts = false;
            promptCanvasGroup.interactable = false;

            // Matte Frosted Pill Background
            var pillBg = pillRect.gameObject.AddComponent<Image>();
            pillBg.color = new Color(0.04f, 0.06f, 0.09f, 0.82f);

            var outline = pillRect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1.0f, 0.95f, 0.80f, 0.18f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);

            // Small Category / Header
            var titleGO = CreateRect("Title", pillRect);
            titleGO.anchorMin = titleGO.anchorMax = new Vector2(0.5f, 0.80f);
            titleGO.sizeDelta = new Vector2(580f, 22f);
            promptTitleText = titleGO.gameObject.AddComponent<Text>();
            promptTitleText.font = font;
            promptTitleText.fontSize = 11;
            promptTitleText.fontStyle = FontStyle.Bold;
            promptTitleText.alignment = TextAnchor.MiddleCenter;
            promptTitleText.color = new Color(0.85f, 0.75f, 0.55f, 0.92f);
            promptTitleText.text = "HƯỚNG DẪN";

            // Centered Key Glyphs
            var keysGO = CreateRect("Keys", pillRect);
            keysGO.anchorMin = keysGO.anchorMax = new Vector2(0.5f, 0.48f);
            keysGO.sizeDelta = new Vector2(580f, 32f);
            promptKeysText = keysGO.gameObject.AddComponent<Text>();
            promptKeysText.font = font;
            promptKeysText.fontSize = 20;
            promptKeysText.fontStyle = FontStyle.Bold;
            promptKeysText.alignment = TextAnchor.MiddleCenter;
            promptKeysText.color = new Color(1.0f, 0.98f, 0.92f, 1.0f);
            promptKeysText.text = "[ A ]   [ D ]";

            // Description / Action Subtitle
            var descGO = CreateRect("Desc", pillRect);
            descGO.anchorMin = descGO.anchorMax = new Vector2(0.5f, 0.20f);
            descGO.sizeDelta = new Vector2(580f, 20f);
            promptDescText = descGO.gameObject.AddComponent<Text>();
            promptDescText.font = font;
            promptDescText.fontSize = 12;
            promptDescText.fontStyle = FontStyle.Normal;
            promptDescText.alignment = TextAnchor.MiddleCenter;
            promptDescText.color = new Color(0.82f, 0.88f, 0.94f, 0.85f);
            promptDescText.text = "Nhấn để bước đi trong bóng râm";
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<RectTransform>();
        }
    }
}
