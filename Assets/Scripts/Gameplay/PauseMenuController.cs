using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Production-grade Pause Menu Controller for "Hoa trên Đất Đỏ".
    /// Features:
    /// - Escape key toggle (both Input System & Legacy support).
    /// - Frosted/matte translucent backdrop revealing live gameplay.
    /// - Pauses player locomotion and gameplay input without destroying state.
    /// - Attenuates gameplay audio during pause; unpauses on resume.
    /// - Menu options: TIẾP TỤC, LƯU GAME (with Autosave notification), TÙY CHỈNH, VỀ MENU CHÍNH.
    /// - Unsaved progress confirmation dialog on returning to main menu.
    /// - Integrated bottom-right Autosave notification indicator.
    /// - Runs via Time.unscaledDeltaTime for smooth UI animation while Time.timeScale = 0.
    /// </summary>
    public class PauseMenuController : MonoBehaviour
    {
        public static PauseMenuController Instance { get; private set; }

        [Header("Scene Configuration")]
        [SerializeField] private string mainMenuSceneName = "Scene_MainMenu";

        [Header("UI Visual Assets")]
        [SerializeField] private Sprite officialLogoSprite;
        [SerializeField] private Sprite birdIconSprite;
        [SerializeField] private AudioClip hoverSfx;
        [SerializeField] private AudioClip confirmSfx;

        // UI Runtime Objects
        private Canvas rootCanvas;
        private CanvasGroup pauseRootGroup;
        private CanvasGroup mainButtonsGroup;
        private CanvasGroup confirmDialogGroup;
        private CanvasGroup optionsDialogGroup;
        private Text confirmDialogBodyText;

        // Autosave Widget
        private CanvasGroup autosaveWidgetGroup;
        private RectTransform autosaveSpinnerRect;
        private Text autosaveStatusText;
        private float autosaveTimer = 0f;

        // Audio State
        private AudioSource sfxSource;
        private readonly List<AudioSource> duckedSources = new List<AudioSource>();
        private readonly List<float> originalVolumes = new List<float>();

        // Navigation
        private readonly List<PauseMenuItem> menuItems = new List<PauseMenuItem>();
        private int selectedIndex = 0;
        private bool isPaused = false;
        private bool isConfirmingQuit = false;

        private PlayerController player;

        public bool IsPaused => isPaused;

        private class PauseMenuItem
        {
            public RectTransform RootRect;
            public Text LabelText;
            public Text LeftDots;
            public Text RightDots;
            public float HoverBlend;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (officialLogoSprite == null)
            {
                officialLogoSprite = Resources.Load<Sprite>("Textures/UI/T_Logo_HoaTrenDatDo");
            }
            if (birdIconSprite == null)
            {
                birdIconSprite = Resources.Load<Sprite>("Textures/UI/T_Dove_Minimalist_BW");
            }
        }

        public void EnsureUI()
        {
            if (rootCanvas == null)
            {
                EnsureEventSystem();
                BuildPauseUI();
            }
        }

        private void Start()
        {
            EnsureUI();
            FindPlayerReference();

            HoaVaMauSaveSystem.OnAutosaveCompleted += HandleAutosaveEvent;
        }

        private void OnDestroy()
        {
            HoaVaMauSaveSystem.OnAutosaveCompleted -= HandleAutosaveEvent;
            if (isPaused)
            {
                Time.timeScale = 1.0f;
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            // Track session playtime if not paused
            if (!isPaused)
            {
                HoaVaMauSaveSystem.SessionPlayTimeCounter += dt;
            }

            // Update Autosave widget animation
            UpdateAutosaveWidget(dt);

            // Handle Escape toggle
            if (IsEscapePressed())
            {
                if (isPaused)
                {
                    if (isConfirmingQuit)
                    {
                        HideQuitConfirmation();
                    }
                    else if (optionsDialogGroup != null && optionsDialogGroup.gameObject.activeSelf)
                    {
                        optionsDialogGroup.gameObject.SetActive(false);
                        mainButtonsGroup.gameObject.SetActive(true);
                    }
                    else
                    {
                        ResumeGame();
                    }
                }
                else
                {
                    PauseGame();
                }
                return;
            }

            if (!isPaused) return;

            // Pause Menu Navigation
            if (!isConfirmingQuit && (optionsDialogGroup == null || !optionsDialogGroup.gameObject.activeSelf))
            {
                if (IsNavDownPressed())
                {
                    selectedIndex = (selectedIndex + 1) % menuItems.Count;
                    PlayHover();
                }
                else if (IsNavUpPressed())
                {
                    selectedIndex = (selectedIndex - 1 + menuItems.Count) % menuItems.Count;
                    PlayHover();
                }
                else if (IsConfirmPressed())
                {
                    TriggerSelection(selectedIndex);
                }

                // Smooth highlight blends
                for (int i = 0; i < menuItems.Count; i++)
                {
                    var item = menuItems[i];
                    bool isSel = (i == selectedIndex);
                    item.HoverBlend = Mathf.MoveTowards(item.HoverBlend, isSel ? 1f : 0f, dt * 7.5f);
                    UpdateMenuItemVisual(item);
                }
            }
        }

        public void PauseGame()
        {
            if (isPaused) return;
            isPaused = true;
            Time.timeScale = 0f;

            if (player == null) FindPlayerReference();
            if (player != null) player.enabled = false;

            AttenuateAudio(true);

            if (pauseRootGroup != null)
            {
                pauseRootGroup.gameObject.SetActive(true);
                pauseRootGroup.alpha = 1f;
                pauseRootGroup.interactable = true;
                pauseRootGroup.blocksRaycasts = true;
            }

            if (mainButtonsGroup != null) mainButtonsGroup.gameObject.SetActive(true);
            if (confirmDialogGroup != null) confirmDialogGroup.gameObject.SetActive(false);
            if (optionsDialogGroup != null) optionsDialogGroup.gameObject.SetActive(false);

            selectedIndex = 0;
            for (int i = 0; i < menuItems.Count; i++)
            {
                menuItems[i].HoverBlend = (i == 0) ? 1f : 0f;
                UpdateMenuItemVisual(menuItems[i]);
            }
        }

        public void ResumeGame()
        {
            if (!isPaused) return;
            isPaused = false;
            Time.timeScale = 1.0f;

            if (player != null) player.enabled = true;

            AttenuateAudio(false);

            if (pauseRootGroup != null)
            {
                pauseRootGroup.alpha = 0f;
                pauseRootGroup.interactable = false;
                pauseRootGroup.blocksRaycasts = false;
                pauseRootGroup.gameObject.SetActive(false);
            }
            PlayConfirm();
        }

        public void SaveCurrentGame()
        {
            PlayConfirm();
            if (autosaveStatusText != null) autosaveStatusText.text = "ĐANG LƯU DỮ LIỆU...";
            autosaveTimer = 2.8f;
            if (autosaveWidgetGroup != null) autosaveWidgetGroup.alpha = 1f;

            Vector3 savePos = (player != null) ? player.transform.position : new Vector3(-8.5f, 0.05f, 0f);
            int slot = HoaVaMauSaveSystem.ActiveSlotIndex;
            HoaVaMauSaveSystem.SaveCurrentGameState(slot, "Chương 1: Đất Đỏ Cà Mau", "Bờ Sông Đất Đỏ", 0, savePos);
        }

        private void HandleAutosaveEvent(bool success, string location)
        {
            autosaveTimer = 2.2f;
            if (autosaveWidgetGroup != null) autosaveWidgetGroup.alpha = 1f;
            if (autosaveStatusText != null)
            {
                autosaveStatusText.text = success ? "ĐÃ LƯU TIẾN TRÌNH" : "LƯU THẤT BẠI";
                autosaveStatusText.color = success ? new Color(0.65f, 0.98f, 0.75f, 0.95f) : new Color(1.0f, 0.45f, 0.45f, 0.95f);
            }
        }

        private void TriggerSelection(int index)
        {
            PlayConfirm();
            switch (index)
            {
                case 0: // TIẾP TỤC
                    ResumeGame();
                    break;
                case 1: // LƯU GAME
                    SaveCurrentGame();
                    break;
                case 2: // TÙY CHỈNH
                    if (mainButtonsGroup != null) mainButtonsGroup.gameObject.SetActive(false);
                    if (optionsDialogGroup != null) optionsDialogGroup.gameObject.SetActive(true);
                    break;
                case 3: // VỀ MENU CHÍNH
                    ShowQuitConfirmation();
                    break;
            }
        }

        private void ShowQuitConfirmation()
        {
            isConfirmingQuit = true;
            if (mainButtonsGroup != null) mainButtonsGroup.gameObject.SetActive(false);
            if (confirmDialogGroup != null)
            {
                confirmDialogGroup.gameObject.SetActive(true);
                confirmDialogGroup.alpha = 1f;
            }
        }

        private void HideQuitConfirmation()
        {
            isConfirmingQuit = false;
            if (confirmDialogGroup != null) confirmDialogGroup.gameObject.SetActive(false);
            if (mainButtonsGroup != null) mainButtonsGroup.gameObject.SetActive(true);
        }

        public void ConfirmReturnToMainMenu()
        {
            Time.timeScale = 1.0f;
            SceneManager.LoadScene(mainMenuSceneName);
        }

        private void AttenuateAudio(bool pause)
        {
            if (pause)
            {
                duckedSources.Clear();
                originalVolumes.Clear();
                var sources = Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
                foreach (var s in sources)
                {
                    if (s == sfxSource || !s.isPlaying) continue;
                    duckedSources.Add(s);
                    originalVolumes.Add(s.volume);
                    s.volume *= 0.28f; // Attenuate gameplay soundscape cleanly
                }
            }
            else
            {
                for (int i = 0; i < duckedSources.Count; i++)
                {
                    if (duckedSources[i] != null && i < originalVolumes.Count)
                    {
                        duckedSources[i].volume = originalVolumes[i];
                    }
                }
                duckedSources.Clear();
                originalVolumes.Clear();
            }
        }

        private void UpdateAutosaveWidget(float dt)
        {
            if (autosaveWidgetGroup == null) return;
            if (autosaveTimer > 0f)
            {
                autosaveTimer -= dt;
                autosaveWidgetGroup.alpha = Mathf.MoveTowards(autosaveWidgetGroup.alpha, 1f, dt * 4f);
                if (autosaveSpinnerRect != null)
                {
                    autosaveSpinnerRect.Rotate(0f, 0f, -dt * 200f);
                }
            }
            else
            {
                autosaveWidgetGroup.alpha = Mathf.MoveTowards(autosaveWidgetGroup.alpha, 0f, dt * 2.5f);
            }
        }

        private void UpdateMenuItemVisual(PauseMenuItem item)
        {
            if (item == null || item.LabelText == null) return;
            Color col = Color.Lerp(new Color(0.92f, 0.95f, 0.98f, 0.75f), new Color(1.0f, 0.98f, 0.88f, 1.0f), item.HoverBlend);
            item.LabelText.color = col;

            float s = Mathf.Lerp(1.0f, 1.05f, item.HoverBlend);
            item.RootRect.localScale = new Vector3(s, s, 1f);

            Color dotCol = new Color(1.0f, 0.95f, 0.80f, item.HoverBlend * 0.92f);
            if (item.LeftDots != null) item.LeftDots.color = dotCol;
            if (item.RightDots != null) item.RightDots.color = dotCol;
        }

        private void FindPlayerReference()
        {
            player = Object.FindAnyObjectByType<PlayerController>();
        }

        private void EnsureEventSystem()
        {
            var es = Object.FindAnyObjectByType<EventSystem>();
            if (es == null)
            {
                var esGO = new GameObject("EventSystem");
                es = esGO.AddComponent<EventSystem>();
            }

#if ENABLE_INPUT_SYSTEM
            var standalone = es.GetComponent<StandaloneInputModule>();
            if (standalone != null)
            {
                if (Application.isPlaying) Destroy(standalone);
                else DestroyImmediate(standalone);
            }
            if (es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
            {
                es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
#else
            if (es.GetComponent<StandaloneInputModule>() == null)
            {
                es.gameObject.AddComponent<StandaloneInputModule>();
            }
#endif
        }

        // =====================================================================================
        // UI CONSTRUCTION
        // =====================================================================================
        private void BuildPauseUI()
        {
            var canvasGO = new GameObject("Canvas_HoaVaMau_PauseMenu");
            canvasGO.transform.SetParent(transform, false);
            rootCanvas = canvasGO.AddComponent<Canvas>();
            rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            rootCanvas.sortingOrder = 500;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;

            Font font = Font.CreateDynamicFontFromOSFont("Segoe UI", 36);
            if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 36);
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Pause Root Panel (Full Screen Frosted Matte Backdrop)
            var rootPanel = CreateRect("Pause_Root_Panel", canvasGO.transform);
            StretchFull(rootPanel);
            pauseRootGroup = rootPanel.gameObject.AddComponent<CanvasGroup>();
            pauseRootGroup.alpha = 0f;
            pauseRootGroup.interactable = false;
            pauseRootGroup.blocksRaycasts = false;
            rootPanel.gameObject.SetActive(false);

            var backdropImg = rootPanel.gameObject.AddComponent<Image>();
            backdropImg.color = new Color(0.02f, 0.03f, 0.06f, 0.72f); // Cinematic translucent matte

            // Center Modal Container
            var modalContainer = CreateRect("Modal_Container", rootPanel);
            modalContainer.anchorMin = modalContainer.anchorMax = new Vector2(0.5f, 0.5f);
            modalContainer.pivot = new Vector2(0.5f, 0.5f);
            modalContainer.sizeDelta = new Vector2(580f, 620f);

            // Title: Calligraphy Logo or "TẠM DỪNG"
            if (officialLogoSprite != null)
            {
                var logoGO = CreateRect("Pause_Calligraphy_Logo", modalContainer);
                logoGO.anchorMin = logoGO.anchorMax = new Vector2(0.5f, 0.88f);
                logoGO.sizeDelta = new Vector2(320f, 160f);
                var lImg = logoGO.gameObject.AddComponent<Image>();
                lImg.sprite = officialLogoSprite;
                lImg.preserveAspect = true;
            }

            var pauseTitle = CreateRect("Pause_Title_Text", modalContainer);
            pauseTitle.anchorMin = pauseTitle.anchorMax = new Vector2(0.5f, 0.68f);
            pauseTitle.sizeDelta = new Vector2(400f, 40f);
            var titleTxt = pauseTitle.gameObject.AddComponent<Text>();
            titleTxt.font = font;
            titleTxt.fontSize = 24;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.color = new Color(1.0f, 0.98f, 0.90f, 0.95f);
            titleTxt.text = "T Ạ M   D Ừ N G";

            var divider = CreateRect("Divider_Line", modalContainer);
            divider.anchorMin = divider.anchorMax = new Vector2(0.5f, 0.63f);
            divider.sizeDelta = new Vector2(260f, 1.5f);
            var dImg = divider.gameObject.AddComponent<Image>();
            dImg.color = new Color(1.0f, 0.95f, 0.80f, 0.60f);

            // Main Buttons Group
            var btnContainer = CreateRect("MainButtons_Group", modalContainer);
            btnContainer.anchorMin = btnContainer.anchorMax = new Vector2(0.5f, 0.38f);
            btnContainer.sizeDelta = new Vector2(440f, 260f);
            mainButtonsGroup = btnContainer.gameObject.AddComponent<CanvasGroup>();

            string[] labels = { "TIẾP TỤC", "LƯU TIẾN TRÌNH", "TÙY CHỈNH", "VỀ MENU CHÍNH" };
            for (int i = 0; i < labels.Length; i++)
            {
                int idx = i;
                var btnRect = CreateRect($"Btn_{labels[i]}", btnContainer);
                btnRect.anchorMin = btnRect.anchorMax = new Vector2(0.5f, 0.82f - i * 0.24f);
                btnRect.sizeDelta = new Vector2(360f, 44f);

                var btn = btnRect.gameObject.AddComponent<Button>();
                var colors = btn.colors;
                colors.normalColor = Color.clear;
                colors.highlightedColor = Color.clear;
                colors.pressedColor = Color.clear;
                btn.colors = colors;

                var txtRect = CreateRect("Label", btnRect);
                StretchFull(txtRect);
                var txt = txtRect.gameObject.AddComponent<Text>();
                txt.font = font;
                txt.fontSize = 18;
                txt.fontStyle = FontStyle.Normal;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color = new Color(0.92f, 0.95f, 0.98f, 0.75f);
                txt.text = labels[i];

                var lDots = CreateRect("LeftDots", btnRect);
                lDots.anchorMin = new Vector2(0f, 0f);
                lDots.anchorMax = new Vector2(0.12f, 1f);
                var lTxt = lDots.gameObject.AddComponent<Text>();
                lTxt.font = font;
                lTxt.fontSize = 14;
                lTxt.alignment = TextAnchor.MiddleRight;
                lTxt.color = Color.clear;
                lTxt.text = "• •";

                var rDots = CreateRect("RightDots", btnRect);
                rDots.anchorMin = new Vector2(0.88f, 0f);
                rDots.anchorMax = new Vector2(1f, 1f);
                var rTxt = rDots.gameObject.AddComponent<Text>();
                rTxt.font = font;
                rTxt.fontSize = 14;
                rTxt.alignment = TextAnchor.MiddleLeft;
                rTxt.color = Color.clear;
                rTxt.text = "• •";

                btn.onClick.AddListener(() => TriggerSelection(idx));

                menuItems.Add(new PauseMenuItem
                {
                    RootRect = btnRect,
                    LabelText = txt,
                    LeftDots = lTxt,
                    RightDots = rTxt,
                    HoverBlend = 0f
                });
            }

            // Quit Confirmation Modal
            BuildQuitConfirmDialog(modalContainer, font);

            // Autosave Bottom-Right Indicator
            BuildAutosaveWidget(rootPanel, font);
        }

        private void BuildQuitConfirmDialog(RectTransform parent, Font font)
        {
            var dialogRect = CreateRect("Confirm_Quit_Dialog", parent);
            dialogRect.anchorMin = dialogRect.anchorMax = new Vector2(0.5f, 0.38f);
            dialogRect.sizeDelta = new Vector2(500f, 220f);
            confirmDialogGroup = dialogRect.gameObject.AddComponent<CanvasGroup>();
            confirmDialogGroup.alpha = 0f;
            dialogRect.gameObject.SetActive(false);

            var dialogBg = dialogRect.gameObject.AddComponent<Image>();
            dialogBg.color = new Color(0.04f, 0.05f, 0.08f, 0.92f);

            var warnText = CreateRect("Warn_Text", dialogRect);
            warnText.anchorMin = warnText.anchorMax = new Vector2(0.5f, 0.72f);
            warnText.sizeDelta = new Vector2(460f, 60f);
            confirmDialogBodyText = warnText.gameObject.AddComponent<Text>();
            confirmDialogBodyText.font = font;
            confirmDialogBodyText.fontSize = 15;
            confirmDialogBodyText.alignment = TextAnchor.MiddleCenter;
            confirmDialogBodyText.color = new Color(0.98f, 0.92f, 0.85f, 0.95f);
            confirmDialogBodyText.text = "Bạn có chắc chắn muốn quay về Menu chính?\nMọi tiến trình chưa lưu sẽ bị mất.";

            // Confirm Button
            var confirmBtnRect = CreateRect("Btn_Confirm_Quit", dialogRect);
            confirmBtnRect.anchorMin = confirmBtnRect.anchorMax = new Vector2(0.30f, 0.25f);
            confirmBtnRect.sizeDelta = new Vector2(140f, 38f);
            var cBtn = confirmBtnRect.gameObject.AddComponent<Button>();
            var cImg = confirmBtnRect.gameObject.AddComponent<Image>();
            cImg.color = new Color(0.85f, 0.25f, 0.25f, 0.85f);
            var cTxtRect = CreateRect("Label", confirmBtnRect);
            StretchFull(cTxtRect);
            var cTxt = cTxtRect.gameObject.AddComponent<Text>();
            cTxt.font = font;
            cTxt.fontSize = 14;
            cTxt.alignment = TextAnchor.MiddleCenter;
            cTxt.color = Color.white;
            cTxt.text = "XÁC NHẬN";
            cBtn.onClick.AddListener(ConfirmReturnToMainMenu);

            // Cancel Button
            var cancelBtnRect = CreateRect("Btn_Cancel_Quit", dialogRect);
            cancelBtnRect.anchorMin = cancelBtnRect.anchorMax = new Vector2(0.70f, 0.25f);
            cancelBtnRect.sizeDelta = new Vector2(140f, 38f);
            var canBtn = cancelBtnRect.gameObject.AddComponent<Button>();
            var canImg = cancelBtnRect.gameObject.AddComponent<Image>();
            canImg.color = new Color(0.25f, 0.30f, 0.38f, 0.85f);
            var canTxtRect = CreateRect("Label", cancelBtnRect);
            StretchFull(canTxtRect);
            var canTxt = canTxtRect.gameObject.AddComponent<Text>();
            canTxt.font = font;
            canTxt.fontSize = 14;
            canTxt.alignment = TextAnchor.MiddleCenter;
            canTxt.color = Color.white;
            canTxt.text = "HỦY";
            canBtn.onClick.AddListener(HideQuitConfirmation);
        }

        private void BuildAutosaveWidget(RectTransform parent, Font font)
        {
            var autoRoot = CreateRect("Autosave_Corner_Widget", parent);
            autoRoot.anchorMin = autoRoot.anchorMax = new Vector2(1.0f, 0.0f);
            autoRoot.pivot = new Vector2(1.0f, 0.0f);
            autoRoot.anchoredPosition = new Vector2(-60f, 40f);
            autoRoot.sizeDelta = new Vector2(280f, 36f);

            autosaveWidgetGroup = autoRoot.gameObject.AddComponent<CanvasGroup>();
            autosaveWidgetGroup.alpha = 0f;
            autosaveWidgetGroup.interactable = false;
            autosaveWidgetGroup.blocksRaycasts = false;

            // Rotating Bird/Flower Icon
            var iconRect = CreateRect("Autosave_Icon", autoRoot);
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(1.0f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(-16f, 0f);
            iconRect.sizeDelta = new Vector2(28f, 28f);
            autosaveSpinnerRect = iconRect;

            var iconImg = iconRect.gameObject.AddComponent<Image>();
            iconImg.sprite = birdIconSprite;
            iconImg.preserveAspect = true;
            iconImg.color = new Color(1.0f, 0.98f, 0.90f, 0.95f);

            // Status Text
            var textRect = CreateRect("Autosave_Text", autoRoot);
            textRect.anchorMin = textRect.anchorMax = new Vector2(1.0f, 0.5f);
            textRect.pivot = new Vector2(1.0f, 0.5f);
            textRect.anchoredPosition = new Vector2(-42f, 0f);
            textRect.sizeDelta = new Vector2(220f, 28f);

            autosaveStatusText = textRect.gameObject.AddComponent<Text>();
            autosaveStatusText.font = font;
            autosaveStatusText.fontSize = 13;
            autosaveStatusText.alignment = TextAnchor.MiddleRight;
            autosaveStatusText.color = new Color(0.98f, 0.94f, 0.85f, 0.92f);
            autosaveStatusText.text = "ĐANG TỰ ĐỘNG LƯU...";
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private void PlayHover()
        {
            if (sfxSource != null && hoverSfx != null) sfxSource.PlayOneShot(hoverSfx, 0.45f);
        }

        private void PlayConfirm()
        {
            if (sfxSource != null && confirmSfx != null) sfxSource.PlayOneShot(confirmSfx, 0.65f);
        }

        // =====================================================================================
        // INPUT ABSTRACTION (Input System package & Legacy fallback)
        // =====================================================================================
        private static bool IsEscapePressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            return kb != null && kb.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        private static bool IsNavDownPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            return kb != null && (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S);
#endif
        }

        private static bool IsNavUpPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            return kb != null && (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W);
#endif
        }

        private static bool IsConfirmPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            return kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space);
#endif
        }
    }
}
