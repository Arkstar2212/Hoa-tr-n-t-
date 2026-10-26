using System;
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
    /// Production-grade In-Game Pause Menu Controller for "Hoa trên Đất Đỏ".
    /// Features:
    /// - Escape key toggle (both Input System & Legacy support).
    /// - Frosted/matte translucent backdrop revealing live gameplay.
    /// - 100% Interactive UI: TIẾP TỤC, LƯU TIẾN TRÌNH, TÙY CHỈNH, VỀ MENU CHÍNH.
    /// - Interactive In-Game Settings:
    ///     * Audio: Master Volume, Music Volume, Hiệu ứng âm thanh (SFX).
    ///     * Graphics: Toàn màn hình (Fullscreen toggle), V-Sync toggle.
    ///     * Controls: Interactive Keybinding table allowing custom key rebinding!
    /// - Unsaved progress confirmation dialog on returning to main menu.
    /// - Runs via Time.unscaledDeltaTime for smooth UI animation while Time.timeScale = 0.
    /// </summary>
    [DisallowMultipleComponent]
    public class PauseMenuController : MonoBehaviour
    {
        public static PauseMenuController Instance { get; private set; }

        [Header("Scene Configuration")]
        [SerializeField] private string mainMenuSceneName = "Scene_MainMenu";

        [Header("UI Visual Assets")]
        [SerializeField] private Sprite officialLogoSprite;
        [SerializeField] private AudioClip hoverSfx;
        [SerializeField] private AudioClip confirmSfx;

        // UI Runtime Objects
        private Canvas rootCanvas;
        private CanvasGroup pauseRootGroup;
        private CanvasGroup mainButtonsGroup;
        private RectTransform modalContainer;
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

        // Rebinding State
        private GameAction? rebindingAction = null;
        private readonly Dictionary<GameAction, Text> keybindButtonTexts = new Dictionary<GameAction, Text>();

        // Settings Elements
        private Slider masterVolSlider;
        private Slider musicVolSlider;
        private Slider sfxVolSlider;
        private Text masterVolValText;
        private Text musicVolValText;
        private Text sfxVolValText;
        private Text fullscreenBtnText;
        private Text vsyncBtnText;

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
                if (rebindingAction.HasValue)
                {
                    // Cancel rebinding on Escape
                    rebindingAction = null;
                    RefreshKeybindButtonLabels();
                    return;
                }

                if (isPaused)
                {
                    if (isConfirmingQuit)
                    {
                        HideQuitConfirmation();
                    }
                    else if (optionsDialogGroup != null && optionsDialogGroup.gameObject.activeSelf)
                    {
                        CloseOptionsDialog();
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

            // Pause Menu Keyboard Navigation (only when options dialog and quit confirm are closed)
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

        private void OnGUI()
        {
            // Detect keypress during rebinding
            if (rebindingAction.HasValue && Event.current.isKey && Event.current.type == EventType.KeyDown)
            {
                KeyCode pressed = Event.current.keyCode;
                if (pressed != KeyCode.None && pressed != KeyCode.Escape)
                {
                    KeybindingManager.SetKey(rebindingAction.Value, pressed);
                    PlayConfirm();
                }
                rebindingAction = null;
                RefreshKeybindButtonLabels();
                Event.current.Use();
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

            if (mainButtonsGroup != null)
            {
                mainButtonsGroup.gameObject.SetActive(true);
                mainButtonsGroup.alpha = 1f;
                mainButtonsGroup.interactable = true;
                mainButtonsGroup.blocksRaycasts = true;
            }
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
            if (autosaveStatusText != null)
            {
                autosaveStatusText.text = "ĐANG LƯU DỮ LIỆU...";
                autosaveStatusText.color = new Color(0.98f, 0.94f, 0.85f, 0.95f);
            }
            autosaveTimer = 2.8f;
            if (autosaveWidgetGroup != null) autosaveWidgetGroup.alpha = 1f;

            Vector3 savePos = (player != null) ? player.transform.position : new Vector3(0f, -1.77f, 0f);
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
                case 1: // LƯU TIẾN TRÌNH
                    SaveCurrentGame();
                    break;
                case 2: // TÙY CHỈNH
                    OpenOptionsDialog();
                    break;
                case 3: // VỀ MENU CHÍNH
                    ShowQuitConfirmation();
                    break;
            }
        }

        public void OpenOptionsDialog()
        {
            PlayConfirm();
            if (mainButtonsGroup != null) mainButtonsGroup.gameObject.SetActive(false);
            if (modalContainer != null) modalContainer.gameObject.SetActive(false);
            if (optionsDialogGroup != null)
            {
                optionsDialogGroup.gameObject.SetActive(true);
                optionsDialogGroup.alpha = 1f;
                optionsDialogGroup.interactable = true;
                optionsDialogGroup.blocksRaycasts = true;
                SyncSettingsUIValues();
            }
        }

        public void CloseOptionsDialog()
        {
            PlayConfirm();
            rebindingAction = null;
            if (optionsDialogGroup != null)
            {
                optionsDialogGroup.gameObject.SetActive(false);
            }
            if (mainButtonsGroup != null)
            {
                mainButtonsGroup.gameObject.SetActive(true);
            }
        }

        private void ShowQuitConfirmation()
        {
            isConfirmingQuit = true;
            if (mainButtonsGroup != null) mainButtonsGroup.gameObject.SetActive(false);
            if (modalContainer != null) modalContainer.gameObject.SetActive(false);
            if (confirmDialogGroup != null)
            {
                confirmDialogGroup.gameObject.SetActive(true);
                confirmDialogGroup.alpha = 1f;
                confirmDialogGroup.interactable = true;
                confirmDialogGroup.blocksRaycasts = true;
            }
        }

        public void HideQuitConfirmation()
        {
            PlayConfirm();
            isConfirmingQuit = false;
            if (confirmDialogGroup != null) confirmDialogGroup.gameObject.SetActive(false);
            if (mainButtonsGroup != null) mainButtonsGroup.gameObject.SetActive(true);
        }

        public void ConfirmReturnToMainMenu()
        {
            PlayConfirm();
            Time.timeScale = 1.0f;
            SceneManager.LoadScene(mainMenuSceneName);
        }

        private void AttenuateAudio(bool pause)
        {
            if (pause)
            {
                duckedSources.Clear();
                originalVolumes.Clear();
                var sources = UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
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
            player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
        }

        private void EnsureEventSystem()
        {
            var es = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
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
            modalContainer = CreateRect("Modal_Container", rootPanel);
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
                lImg.raycastTarget = false;
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
            titleTxt.raycastTarget = false;

            var divider = CreateRect("Divider_Line", modalContainer);
            divider.anchorMin = divider.anchorMax = new Vector2(0.5f, 0.63f);
            divider.sizeDelta = new Vector2(260f, 1.5f);
            var dImg = divider.gameObject.AddComponent<Image>();
            dImg.color = new Color(1.0f, 0.95f, 0.80f, 0.60f);
            dImg.raycastTarget = false;

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

                // Add transparent Image on button to receive raycasts cleanly!
                var btnImg = btnRect.gameObject.AddComponent<Image>();
                btnImg.color = new Color(0.10f, 0.14f, 0.20f, 0.65f);
                btnImg.raycastTarget = true;

                var btn = btnRect.gameObject.AddComponent<Button>();
                btn.targetGraphic = btnImg;
                var colors = btn.colors;
                colors.normalColor = new Color(0.10f, 0.14f, 0.20f, 0.65f);
                colors.highlightedColor = new Color(0.24f, 0.32f, 0.44f, 0.90f);
                colors.pressedColor = new Color(0.85f, 0.42f, 0.18f, 0.95f);
                btn.colors = colors;

                var txtRect = CreateRect("Label", btnRect);
                StretchFull(txtRect);
                var txt = txtRect.gameObject.AddComponent<Text>();
                txt.font = font;
                txt.fontSize = 18;
                txt.fontStyle = FontStyle.Normal;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color = new Color(0.92f, 0.95f, 0.98f, 0.85f);
                txt.text = labels[i];
                txt.raycastTarget = false;

                var lDots = CreateRect("LeftDots", btnRect);
                lDots.anchorMin = new Vector2(0f, 0f);
                lDots.anchorMax = new Vector2(0.12f, 1f);
                var lTxt = lDots.gameObject.AddComponent<Text>();
                lTxt.font = font;
                lTxt.fontSize = 14;
                lTxt.alignment = TextAnchor.MiddleRight;
                lTxt.color = Color.clear;
                lTxt.text = "• •";
                lTxt.raycastTarget = false;

                var rDots = CreateRect("RightDots", btnRect);
                rDots.anchorMin = new Vector2(0.88f, 0f);
                rDots.anchorMax = new Vector2(1f, 1f);
                var rTxt = rDots.gameObject.AddComponent<Text>();
                rTxt.font = font;
                rTxt.fontSize = 14;
                rTxt.alignment = TextAnchor.MiddleLeft;
                rTxt.color = Color.clear;
                rTxt.text = "• •";
                rTxt.raycastTarget = false;

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

            // In-Game Options Settings Modal
            BuildOptionsDialog(rootPanel, font);

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
            dialogBg.color = new Color(0.04f, 0.05f, 0.08f, 0.95f);

            var warnText = CreateRect("Warn_Text", dialogRect);
            warnText.anchorMin = warnText.anchorMax = new Vector2(0.5f, 0.72f);
            warnText.sizeDelta = new Vector2(460f, 60f);
            confirmDialogBodyText = warnText.gameObject.AddComponent<Text>();
            confirmDialogBodyText.font = font;
            confirmDialogBodyText.fontSize = 15;
            confirmDialogBodyText.alignment = TextAnchor.MiddleCenter;
            confirmDialogBodyText.color = new Color(0.98f, 0.92f, 0.85f, 0.95f);
            confirmDialogBodyText.text = "Bạn có chắc chắn muốn quay về Menu chính?\nMọi tiến trình chưa lưu sẽ bị mất.";
            confirmDialogBodyText.raycastTarget = false;

            // Confirm Button
            var confirmBtnRect = CreateRect("Btn_Confirm_Quit", dialogRect);
            confirmBtnRect.anchorMin = confirmBtnRect.anchorMax = new Vector2(0.30f, 0.25f);
            confirmBtnRect.sizeDelta = new Vector2(140f, 38f);
            var cImg = confirmBtnRect.gameObject.AddComponent<Image>();
            cImg.color = new Color(0.85f, 0.25f, 0.25f, 0.90f);
            var cBtn = confirmBtnRect.gameObject.AddComponent<Button>();
            cBtn.targetGraphic = cImg;

            var cTxtRect = CreateRect("Label", confirmBtnRect);
            StretchFull(cTxtRect);
            var cTxt = cTxtRect.gameObject.AddComponent<Text>();
            cTxt.font = font;
            cTxt.fontSize = 14;
            cTxt.alignment = TextAnchor.MiddleCenter;
            cTxt.color = Color.white;
            cTxt.text = "XÁC NHẬN";
            cTxt.raycastTarget = false;
            cBtn.onClick.AddListener(ConfirmReturnToMainMenu);

            // Cancel Button
            var cancelBtnRect = CreateRect("Btn_Cancel_Quit", dialogRect);
            cancelBtnRect.anchorMin = cancelBtnRect.anchorMax = new Vector2(0.70f, 0.25f);
            cancelBtnRect.sizeDelta = new Vector2(140f, 38f);
            var canImg = cancelBtnRect.gameObject.AddComponent<Image>();
            canImg.color = new Color(0.25f, 0.30f, 0.38f, 0.90f);
            var canBtn = cancelBtnRect.gameObject.AddComponent<Button>();
            canBtn.targetGraphic = canImg;

            var canTxtRect = CreateRect("Label", cancelBtnRect);
            StretchFull(canTxtRect);
            var canTxt = canTxtRect.gameObject.AddComponent<Text>();
            canTxt.font = font;
            canTxt.fontSize = 14;
            canTxt.alignment = TextAnchor.MiddleCenter;
            canTxt.color = Color.white;
            canTxt.text = "HỦY";
            canTxt.raycastTarget = false;
            canBtn.onClick.AddListener(HideQuitConfirmation);
        }

        private void BuildOptionsDialog(RectTransform parent, Font font)
        {
            var dialogRect = CreateRect("Modal_InGame_Options", parent);
            StretchFull(dialogRect);
            optionsDialogGroup = dialogRect.gameObject.AddComponent<CanvasGroup>();
            optionsDialogGroup.alpha = 0f;
            optionsDialogGroup.interactable = false;
            optionsDialogGroup.blocksRaycasts = false;
            dialogRect.gameObject.SetActive(false);

            var bgImg = dialogRect.gameObject.AddComponent<Image>();
            bgImg.color = new Color(0.02f, 0.03f, 0.05f, 0.88f);

            var box = CreateRect("Center_Box", dialogRect);
            box.anchorMin = box.anchorMax = new Vector2(0.5f, 0.5f);
            box.pivot = new Vector2(0.5f, 0.5f);
            box.sizeDelta = new Vector2(860f, 660f);

            var boxBg = box.gameObject.AddComponent<Image>();
            boxBg.color = new Color(0.06f, 0.08f, 0.12f, 0.96f);

            // Title
            var titleRect = CreateRect("Title", box);
            titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 0.94f);
            titleRect.sizeDelta = new Vector2(760f, 36f);
            var title = titleRect.gameObject.AddComponent<Text>();
            title.font = font;
            title.fontSize = 20;
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = new Color(1.0f, 0.98f, 0.90f, 0.95f);
            title.text = "T Ù Y   C H Ỉ N H   I N - G A M E";
            title.raycastTarget = false;

            float startY = -60f;

            // SECTION 1: ÂM LƯỢNG (AUDIO)
            CreateSectionHeader(box, "1. ÂM LƯỢNG (AUDIO)", new Vector2(40f, startY), font);
            startY -= 34f;
            masterVolSlider = CreateSliderRow(box, "Âm lượng Tổng", AudioListener.volume, new Vector2(40f, startY), font, out masterVolValText);
            masterVolSlider.onValueChanged.AddListener(v => {
                AudioListener.volume = v;
                PlayerPrefs.SetFloat("Settings_MasterVol", v);
                if (masterVolValText != null) masterVolValText.text = $"{Mathf.RoundToInt(v * 100f)}%";
            });

            startY -= 36f;
            float initMusic = PlayerPrefs.GetFloat("Settings_MusicVol", 0.75f);
            musicVolSlider = CreateSliderRow(box, "Âm lượng Nhạc nền", initMusic, new Vector2(40f, startY), font, out musicVolValText);
            musicVolSlider.onValueChanged.AddListener(v => {
                PlayerPrefs.SetFloat("Settings_MusicVol", v);
                if (musicVolValText != null) musicVolValText.text = $"{Mathf.RoundToInt(v * 100f)}%";
            });

            startY -= 36f;
            float initSfx = PlayerPrefs.GetFloat("Settings_SfxVol", 0.85f);
            sfxVolSlider = CreateSliderRow(box, "Hiệu ứng âm thanh (SFX)", initSfx, new Vector2(40f, startY), font, out sfxVolValText);
            sfxVolSlider.onValueChanged.AddListener(v => {
                PlayerPrefs.SetFloat("Settings_SfxVol", v);
                if (sfxVolValText != null) sfxVolValText.text = $"{Mathf.RoundToInt(v * 100f)}%";
            });

            // SECTION 2: ĐỒ HỌA (GRAPHICS - SIMPLIFIED)
            startY -= 46f;
            CreateSectionHeader(box, "2. ĐỒ HỌA & HIỂN THỊ", new Vector2(40f, startY), font);
            startY -= 36f;

            var fsBtnRect = CreateRect("Btn_Toggle_Fullscreen", box);
            fsBtnRect.anchorMin = fsBtnRect.anchorMax = new Vector2(0f, 1f);
            fsBtnRect.pivot = new Vector2(0f, 1f);
            fsBtnRect.anchoredPosition = new Vector2(40f, startY);
            fsBtnRect.sizeDelta = new Vector2(240f, 32f);
            var fsImg = fsBtnRect.gameObject.AddComponent<Image>();
            fsImg.color = new Color(0.16f, 0.20f, 0.30f, 0.85f);
            var fsBtn = fsBtnRect.gameObject.AddComponent<Button>();
            fsBtn.targetGraphic = fsImg;
            var fsTxtRect = CreateRect("Label", fsBtnRect);
            StretchFull(fsTxtRect);
            fullscreenBtnText = fsTxtRect.gameObject.AddComponent<Text>();
            fullscreenBtnText.font = font;
            fullscreenBtnText.fontSize = 13;
            fullscreenBtnText.alignment = TextAnchor.MiddleCenter;
            fullscreenBtnText.color = Color.white;
            fullscreenBtnText.text = Screen.fullScreen ? "TOÀN MÀN HÌNH: [ BẬT ]" : "TOÀN MÀN HÌNH: [ TẮT ]";
            fullscreenBtnText.raycastTarget = false;
            fsBtn.onClick.AddListener(() => {
                Screen.fullScreen = !Screen.fullScreen;
                fullscreenBtnText.text = Screen.fullScreen ? "TOÀN MÀN HÌNH: [ BẬT ]" : "TOÀN MÀN HÌNH: [ TẮT ]";
                PlayConfirm();
            });

            var vsyncBtnRect = CreateRect("Btn_Toggle_VSync", box);
            vsyncBtnRect.anchorMin = vsyncBtnRect.anchorMax = new Vector2(0f, 1f);
            vsyncBtnRect.pivot = new Vector2(0f, 1f);
            vsyncBtnRect.anchoredPosition = new Vector2(300f, startY);
            vsyncBtnRect.sizeDelta = new Vector2(240f, 32f);
            var vsImg = vsyncBtnRect.gameObject.AddComponent<Image>();
            vsImg.color = new Color(0.16f, 0.20f, 0.30f, 0.85f);
            var vsBtn = vsyncBtnRect.gameObject.AddComponent<Button>();
            vsBtn.targetGraphic = vsImg;
            var vsTxtRect = CreateRect("Label", vsyncBtnRect);
            StretchFull(vsTxtRect);
            vsyncBtnText = vsTxtRect.gameObject.AddComponent<Text>();
            vsyncBtnText.font = font;
            vsyncBtnText.fontSize = 13;
            vsyncBtnText.alignment = TextAnchor.MiddleCenter;
            vsyncBtnText.color = Color.white;
            vsyncBtnText.text = (QualitySettings.vSyncCount > 0) ? "V-SYNC: [ BẬT ]" : "V-SYNC: [ TẮT ]";
            vsyncBtnText.raycastTarget = false;
            vsBtn.onClick.AddListener(() => {
                QualitySettings.vSyncCount = (QualitySettings.vSyncCount > 0) ? 0 : 1;
                vsyncBtnText.text = (QualitySettings.vSyncCount > 0) ? "V-SYNC: [ BẬT ]" : "V-SYNC: [ TẮT ]";
                PlayConfirm();
            });

            // SECTION 3: BẢNG PHÍM ĐIỀU KHIỂN (KEYBINDINGS)
            startY -= 46f;
            CreateSectionHeader(box, "3. BẢNG PHÍM ĐIỀU KHIỂN (BẤM VÀO ĐỂ GÁN PHÍM)", new Vector2(40f, startY), font);
            startY -= 32f;

            GameAction[] keyActions = {
                GameAction.MoveLeft,
                GameAction.MoveRight,
                GameAction.MoveUp,
                GameAction.MoveDown,
                GameAction.Jump,
                GameAction.Sprint,
                GameAction.Interact,
                GameAction.RopeGrab
            };

            for (int k = 0; k < keyActions.Length; k++)
            {
                var action = keyActions[k];
                float colX = (k % 2 == 0) ? 40f : 440f;
                float rowY = startY - (k / 2) * 36f;

                var row = CreateRect($"KeyRow_{action}", box);
                row.anchorMin = row.anchorMax = new Vector2(0f, 1f);
                row.pivot = new Vector2(0f, 1f);
                row.anchoredPosition = new Vector2(colX, rowY);
                row.sizeDelta = new Vector2(380f, 32f);

                var lbl = CreateRect("Label", row);
                lbl.anchorMin = lbl.anchorMax = new Vector2(0f, 0.5f);
                lbl.pivot = new Vector2(0f, 0.5f);
                lbl.anchoredPosition = new Vector2(0f, 0f);
                lbl.sizeDelta = new Vector2(210f, 30f);
                var lTxt = lbl.gameObject.AddComponent<Text>();
                lTxt.font = font;
                lTxt.fontSize = 12;
                lTxt.alignment = TextAnchor.MiddleLeft;
                lTxt.color = new Color(0.85f, 0.90f, 0.95f, 0.85f);
                lTxt.text = KeybindingManager.GetActionDisplayName(action);
                lTxt.raycastTarget = false;

                var btnRect = CreateRect("Btn", row);
                btnRect.anchorMin = btnRect.anchorMax = new Vector2(1f, 0.5f);
                btnRect.pivot = new Vector2(1f, 0.5f);
                btnRect.anchoredPosition = new Vector2(0f, 0f);
                btnRect.sizeDelta = new Vector2(160f, 30f);
                var bImg = btnRect.gameObject.AddComponent<Image>();
                bImg.color = new Color(0.12f, 0.16f, 0.24f, 0.85f);
                var btn = btnRect.gameObject.AddComponent<Button>();
                btn.targetGraphic = bImg;

                var bTxtRect = CreateRect("Text", btnRect);
                StretchFull(bTxtRect);
                var bTxt = bTxtRect.gameObject.AddComponent<Text>();
                bTxt.font = font;
                bTxt.fontSize = 12;
                bTxt.alignment = TextAnchor.MiddleCenter;
                bTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
                bTxt.verticalOverflow = VerticalWrapMode.Overflow;
                bTxt.color = new Color(1f, 0.95f, 0.80f, 0.95f);
                bTxt.text = $"[ {KeybindingManager.GetKeyDisplayName(KeybindingManager.GetKey(action))} ]";
                bTxt.raycastTarget = false;

                keybindButtonTexts[action] = bTxt;

                btn.onClick.AddListener(() => StartRebindingAction(action));
            }

            // RESET TO DEFAULTS BUTTON
            var resetBtnRect = CreateRect("Btn_Reset_Defaults", box);
            resetBtnRect.anchorMin = resetBtnRect.anchorMax = new Vector2(0.28f, 0.055f);
            resetBtnRect.sizeDelta = new Vector2(160f, 38f);
            var rImg = resetBtnRect.gameObject.AddComponent<Image>();
            rImg.color = new Color(0.24f, 0.28f, 0.36f, 0.85f);
            var rBtn = resetBtnRect.gameObject.AddComponent<Button>();
            rBtn.targetGraphic = rImg;
            var rTxtRect = CreateRect("Label", resetBtnRect);
            StretchFull(rTxtRect);
            var rTxt = rTxtRect.gameObject.AddComponent<Text>();
            rTxt.font = font;
            rTxt.fontSize = 13;
            rTxt.alignment = TextAnchor.MiddleCenter;
            rTxt.color = Color.white;
            rTxt.text = "MẶC ĐỊNH";
            rTxt.raycastTarget = false;
            rBtn.onClick.AddListener(() => {
                KeybindingManager.ResetDefaults();
                RefreshKeybindButtonLabels();
                PlayConfirm();
            });

            // CLOSE BUTTON
            var backBtnRect = CreateRect("Btn_Close_Options", box);
            backBtnRect.anchorMin = backBtnRect.anchorMax = new Vector2(0.72f, 0.055f);
            backBtnRect.sizeDelta = new Vector2(160f, 38f);
            var backImg = backBtnRect.gameObject.AddComponent<Image>();
            backImg.color = new Color(0.85f, 0.42f, 0.18f, 0.90f);
            var backBtn = backBtnRect.gameObject.AddComponent<Button>();
            backBtn.targetGraphic = backImg;
            var backTxtRect = CreateRect("Label", backBtnRect);
            StretchFull(backTxtRect);
            var backTxt = backTxtRect.gameObject.AddComponent<Text>();
            backTxt.font = font;
            backTxt.fontSize = 13;
            backTxt.fontStyle = FontStyle.Bold;
            backTxt.alignment = TextAnchor.MiddleCenter;
            backTxt.color = Color.white;
            backTxt.text = "LƯU & ĐÓNG";
            backTxt.raycastTarget = false;
            backBtn.onClick.AddListener(CloseOptionsDialog);
        }

        private void StartRebindingAction(GameAction action)
        {
            PlayHover();
            rebindingAction = action;
            if (keybindButtonTexts.TryGetValue(action, out Text txt))
            {
                txt.text = "[ BẤM PHÍM MỚI... ]";
                txt.color = new Color(1f, 0.75f, 0.2f, 1f);
            }
        }

        private void RefreshKeybindButtonLabels()
        {
            foreach (var kvp in keybindButtonTexts)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.text = $"[ {KeybindingManager.GetKeyDisplayName(KeybindingManager.GetKey(kvp.Key))} ]";
                    kvp.Value.color = new Color(1f, 0.95f, 0.80f, 0.95f);
                }
            }
        }

        private void SyncSettingsUIValues()
        {
            if (masterVolSlider != null)
            {
                masterVolSlider.value = AudioListener.volume;
                if (masterVolValText != null) masterVolValText.text = $"{Mathf.RoundToInt(AudioListener.volume * 100f)}%";
            }
            if (musicVolSlider != null)
            {
                float v = PlayerPrefs.GetFloat("Settings_MusicVol", 0.75f);
                musicVolSlider.value = v;
                if (musicVolValText != null) musicVolValText.text = $"{Mathf.RoundToInt(v * 100f)}%";
            }
            if (sfxVolSlider != null)
            {
                float v = PlayerPrefs.GetFloat("Settings_SfxVol", 0.85f);
                sfxVolSlider.value = v;
                if (sfxVolValText != null) sfxVolValText.text = $"{Mathf.RoundToInt(v * 100f)}%";
            }
            if (fullscreenBtnText != null)
            {
                fullscreenBtnText.text = Screen.fullScreen ? "TOÀN MÀN HÌNH: [ BẬT ]" : "TOÀN MÀN HÌNH: [ TẮT ]";
            }
            if (vsyncBtnText != null)
            {
                vsyncBtnText.text = (QualitySettings.vSyncCount > 0) ? "V-SYNC: [ BẬT ]" : "V-SYNC: [ TẮT ]";
            }
            RefreshKeybindButtonLabels();
        }

        private static void CreateSectionHeader(RectTransform parent, string title, Vector2 pos, Font font)
        {
            var headerRect = CreateRect("Section_Header", parent);
            headerRect.anchorMin = headerRect.anchorMax = new Vector2(0f, 1f);
            headerRect.pivot = new Vector2(0f, 1f);
            headerRect.anchoredPosition = pos;
            headerRect.sizeDelta = new Vector2(760f, 24f);
            var t = headerRect.gameObject.AddComponent<Text>();
            t.font = font;
            t.fontSize = 13;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleLeft;
            t.color = new Color(0.98f, 0.88f, 0.65f, 0.95f);
            t.text = title;
            t.raycastTarget = false;
        }

        private Slider CreateSliderRow(RectTransform parent, string label, float initVal, Vector2 pos, Font font, out Text valTxt)
        {
            var rowRect = CreateRect($"Row_{label}", parent);
            rowRect.anchorMin = rowRect.anchorMax = new Vector2(0f, 1f);
            rowRect.pivot = new Vector2(0f, 1f);
            rowRect.anchoredPosition = pos;
            rowRect.sizeDelta = new Vector2(760f, 30f);

            var lblRect = CreateRect("Label", rowRect);
            lblRect.anchorMin = new Vector2(0f, 0f);
            lblRect.anchorMax = new Vector2(0.32f, 1f);
            var l = lblRect.gameObject.AddComponent<Text>();
            l.font = font;
            l.fontSize = 13;
            l.alignment = TextAnchor.MiddleLeft;
            l.color = new Color(0.88f, 0.92f, 0.96f, 0.85f);
            l.text = label;
            l.raycastTarget = false;

            var sliderRect = CreateRect("Slider", rowRect);
            sliderRect.anchorMin = new Vector2(0.34f, 0.5f);
            sliderRect.anchorMax = new Vector2(0.85f, 0.5f);
            sliderRect.pivot = new Vector2(0.5f, 0.5f);
            sliderRect.sizeDelta = new Vector2(0f, 8f);

            var bg = sliderRect.gameObject.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.15f, 0.22f, 0.95f);

            var fillArea = CreateRect("Fill Area", sliderRect);
            fillArea.anchorMin = Vector2.zero;
            fillArea.anchorMax = Vector2.one;
            fillArea.offsetMin = fillArea.offsetMax = Vector2.zero;

            var fill = CreateRect("Fill", fillArea);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0.5f, 1f);
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            var fillImg = fill.gameObject.AddComponent<Image>();
            fillImg.color = new Color(0.85f, 0.42f, 0.18f, 0.95f);

            var handleArea = CreateRect("Handle Slide Area", sliderRect);
            handleArea.anchorMin = Vector2.zero;
            handleArea.anchorMax = Vector2.one;
            handleArea.offsetMin = handleArea.offsetMax = Vector2.zero;

            var handle = CreateRect("Handle", handleArea);
            handle.sizeDelta = new Vector2(18f, 18f);
            var hImg = handle.gameObject.AddComponent<Image>();
            hImg.color = new Color(1.0f, 0.98f, 0.92f, 1.0f);

            var slider = sliderRect.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = hImg;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = initVal;

            var valRect = CreateRect("ValText", rowRect);
            valRect.anchorMin = new Vector2(0.88f, 0f);
            valRect.anchorMax = new Vector2(1.0f, 1f);
            var vt = valRect.gameObject.AddComponent<Text>();
            vt.font = font;
            vt.fontSize = 12;
            vt.alignment = TextAnchor.MiddleRight;
            vt.color = new Color(0.92f, 0.95f, 0.98f, 0.85f);
            vt.text = $"{Mathf.RoundToInt(initVal * 100f)}%";
            vt.raycastTarget = false;
            valTxt = vt;

            return slider;
        }

        private void BuildAutosaveWidget(RectTransform parent, Font font)
        {
            var widgetRect = CreateRect("Widget_Autosave_Indicator", parent);
            widgetRect.anchorMin = widgetRect.anchorMax = new Vector2(1.0f, 0.0f);
            widgetRect.pivot = new Vector2(1.0f, 0.0f);
            widgetRect.anchoredPosition = new Vector2(-28f, 28f);
            widgetRect.sizeDelta = new Vector2(240f, 34f);

            autosaveWidgetGroup = widgetRect.gameObject.AddComponent<CanvasGroup>();
            autosaveWidgetGroup.alpha = 0f;
            autosaveWidgetGroup.blocksRaycasts = false;
            autosaveWidgetGroup.interactable = false;

            var spinRect = CreateRect("Spinner", widgetRect);
            spinRect.anchorMin = spinRect.anchorMax = new Vector2(0.12f, 0.5f);
            spinRect.sizeDelta = new Vector2(18f, 18f);
            autosaveSpinnerRect = spinRect;
            var sImg = spinRect.gameObject.AddComponent<Image>();
            sImg.color = new Color(0.98f, 0.92f, 0.85f, 0.95f);
            sImg.raycastTarget = false;

            var txtRect = CreateRect("Text", widgetRect);
            txtRect.anchorMin = new Vector2(0.24f, 0f);
            txtRect.anchorMax = new Vector2(1.0f, 1f);
            autosaveStatusText = txtRect.gameObject.AddComponent<Text>();
            autosaveStatusText.font = font;
            autosaveStatusText.fontSize = 12;
            autosaveStatusText.fontStyle = FontStyle.Normal;
            autosaveStatusText.alignment = TextAnchor.MiddleLeft;
            autosaveStatusText.color = new Color(0.98f, 0.92f, 0.85f, 0.92f);
            autosaveStatusText.text = "ĐANG TỰ ĐỘNG LƯU...";
            autosaveStatusText.raycastTarget = false;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<RectTransform>();
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void PlayHover()
        {
            if (sfxSource != null && hoverSfx != null)
            {
                sfxSource.PlayOneShot(hoverSfx, 0.6f);
            }
        }

        private void PlayConfirm()
        {
            if (sfxSource != null && confirmSfx != null)
            {
                sfxSource.PlayOneShot(confirmSfx, 0.85f);
            }
        }

        private static bool IsEscapePressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) return true;
#endif
            try
            {
                if (Input.GetKeyDown(KeyCode.Escape)) return true;
            }
            catch {}
            return false;
        }

        private static bool IsNavDownPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame)) return true;
#endif
            try
            {
                if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) return true;
            }
            catch {}
            return false;
        }

        private static bool IsNavUpPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)) return true;
#endif
            try
            {
                if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) return true;
            }
            catch {}
            return false;
        }

        private static bool IsConfirmPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)) return true;
#endif
            try
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space)) return true;
            }
            catch {}
            return false;
        }
    }
}
