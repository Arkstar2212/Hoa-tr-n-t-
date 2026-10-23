using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Production-grade Startup Flow, Main Menu, Interactive Settings, and BotW-style Loading System for "Hoa trên Đất Đỏ".
    /// Strictly adheres to:
    /// - State A: Initial Startup (pure black curtain, no background, no music).
    /// - State B: Team Loading Screen (Five Cows logo centered, "PRESENTS", Circular Orbit 2.5D Dove Spinner).
    /// - State C: Main Menu (Smooth crossfade into living vista, enlarged official calligraphy logo without Five Cows above it, BGM fades in).
    /// - Menu Buttons: BẮT ĐẦU, TIẾP TỤC (with save status), TÙY CHỈNH (interactive settings modal), THOÁT.
    /// - Interactive Settings Modal: Audio sliders (Master, Music, SFX), Graphics quality selector, Fullscreen toggle, Controls guide.
    /// - Save Slot Selection Modal: Slot cards with playtime and chapter status, overwrite confirmation dialog.
    /// - BotW Loading Screen: Zelda Breath of the Wild aesthetic with cream parchment upper section, horizon divider line,
    ///   hopping silhouette of cậu bé An, sailing silhouette of the 1954 Transport Ship, rotating tip cards, Sheikah-style spinner, and async loading.
    /// - Safe UI anchoring across 16:9, 16:10, and 21:9 ultrawide screens.
    /// </summary>
    public class PlanetOfLanaStartMenuSystem : MonoBehaviour
    {
        public enum MenuPhase
        {
            InitialStartup,       // State A: Pure black screen
            TeamLoadingScreen,    // State B: Five Cows centered, Circular Orbit Dove Spinner
            HeadphoneScreen,      // Bước 2: Headphone Advisory (Fade-in 1s -> Hold 2s -> Fade-out 1s)
            StartMenuOpenVista,   // State C: Living vista, larger logo, BGM starts fading in
            SaveSlotSelection,    // Modal: New Game / Continue slot selection
            Options,              // Modal: Interactive Audio / Graphics / Controls settings
            BotWLoadingScreen,    // State E: Zelda Breath of the Wild style loading screen with An & 1954 Ship
            TransitionToGameplay, // Legacy / Fade to scene activation
            StudioSplash = TeamLoadingScreen,
            DoveLoadingScreen = TeamLoadingScreen
        }

        public enum SaveModalMode
        {
            NewGame,
            Continue
        }

        [Header("Startup & Timing Settings")]
        [SerializeField] private float initialStartupDuration = 0.5f;
        [SerializeField] private float teamLogoFadeInDuration = 1.0f;
        [SerializeField] private float teamLogoHoldDuration = 1.5f;
        [SerializeField] private float teamLogoFadeOutDuration = 1.0f;
        [SerializeField] private float headphoneFadeInDuration = 1.0f;
        [SerializeField] private float headphoneHoldDuration = 2.0f;
        [SerializeField] private float headphoneFadeOutDuration = 1.0f;
        [SerializeField] private float teamLoadingDuration = 3.5f;
        [SerializeField] private float botwMinLoadingDuration = 3.4f;
        [SerializeField] private string targetGameplaySceneName = "New Scene 1_Overhaul";

        [Header("Art Assets")]
        [SerializeField] private Sprite studioLogoSprite;
        [SerializeField] private Sprite headphoneSprite;
        [SerializeField] private Sprite gameOfficialLogoSprite;
        [SerializeField] private Sprite doveSilhouetteSprite;
        [SerializeField] private Sprite flowerBloomSprite;
        [SerializeField] private Sprite softDotSprite;
        [SerializeField] private Sprite boySilhouetteSprite;
        [SerializeField] private Sprite shipSilhouetteSprite;

        [Header("Audio (Gentle BGM, Wind & UI Chimes)")]
        [SerializeField] private AudioClip mainMenuMusicClip;
        [SerializeField] private AudioClip ambientWindClip;
        [SerializeField] private AudioClip hoverSfxClip;
        [SerializeField] private AudioClip confirmSfxClip;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.65f;
        [SerializeField, Range(0f, 1f)] private float windVolume = 0.22f;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.45f;
        [SerializeField] private float musicSmoothInDuration = 4.5f;

        [Header("Scenic Camera Settings")]
        [SerializeField] private Vector3 menuCameraPosition = new Vector3(0f, 0f, -10.0f);
        [SerializeField] private Vector3 menuCameraEuler = Vector3.zero;
        [SerializeField] private float menuCameraFOV = 35.5f;

        private MenuPhase currentPhase = MenuPhase.InitialStartup;
        private float phaseTimer = 0f;
        private float currentMusicFadeVol = 0f;
        private bool isMusicPlaying = false;

        // Audio Sources
        private AudioSource musicSource;
        private AudioSource windSource;
        private AudioSource sfxSource;

        // Runtime Canvas & Panels
        private Canvas rootCanvas;
        private CanvasGroup blackCurtainGroup;

        // State B Panel (Team Loading Screen)
        private CanvasGroup teamLoadingGroup;
        private RectTransform teamLogoRect;
        private RectTransform teamBirdLoaderRect;
        private Image teamBirdLoaderImg;
        private readonly List<RectTransform> teamOrbitDots = new List<RectTransform>();

        // Step 2 Panel (Headphone Advisory Screen)
        private CanvasGroup headphoneGroup;
        private RectTransform headphoneIconRect;
        private Text headphoneText;

        // State C Panel (Main Menu)
        private CanvasGroup startMenuGroup;
        private RectTransform menuClusterRect;
        private Image mainLogoImg;
        private readonly List<MenuButtonItem> menuButtons = new List<MenuButtonItem>();
        private int selectedMenuIndex = 0;
        private MenuButtonItem continueButtonItem;

        // Save Slot Selection Modal
        private CanvasGroup saveModalGroup;
        private SaveModalMode currentSaveMode = SaveModalMode.NewGame;
        private Text saveModalTitleText;
        private Text saveModalNoticeText;
        private readonly List<SaveSlotUIItem> slotUIItems = new List<SaveSlotUIItem>();
        private int selectedSlotIndex = 0;

        // Overwrite Confirmation Dialog
        private CanvasGroup overwriteDialogGroup;
        private Text overwriteDialogText;
        private int pendingOverwriteSlot = -1;

        // Options Panel (Tùy Chỉnh)
        private CanvasGroup optionsModalGroup;
        private Slider masterVolSlider;
        private Slider musicVolSlider;
        private Slider sfxVolSlider;
        private Text masterVolValText;
        private Text musicVolValText;
        private Text sfxVolValText;
        private Button[] qualityButtons = new Button[4];
        private Text[] qualityBtnTexts = new Text[4];
        private Text fullscreenBtnText;

        // Zelda Breath of the Wild Style Loading Screen
        private CanvasGroup botwLoadingGroup;
        private RectTransform botwAnRect;
        private RectTransform botwShipRect;
        private Image botwAnImg;
        private Image botwShipImg;
        private Text botwTipCategoryText;
        private Text botwTipBodyText;
        private CanvasGroup botwTipTextGroup;
        private RectTransform botwDotsClusterRect;
        private readonly List<Image> botwSheikahDots = new List<Image>();
        private int currentTipIndex = 0;
        private float tipCycleTimer = 0f;
        private AsyncOperation asyncSceneLoadOp;
        private bool isAsyncLoadingStarted = false;

        // Autosave Corner Widget
        private CanvasGroup autosaveWidgetGroup;
        private RectTransform autosaveSpinnerRect;
        private Text autosaveStatusText;
        private float autosaveTimer = 0f;

        // Scene Camera Reference
        private Camera mainCam;

        private static PlanetOfLanaStartMenuSystem instance;

        // Breath of the Wild Style Tips & Lore
        private readonly (string Category, string Body)[] botwTips = new[]
        {
            (
                "ẨN MÌNH TRONG LAU SẬY",
                "Kẻ địch và lính tuần tra có tầm nhìn theo hướng quay mặt. Khi phát hiện nguy hiểm, hãy nhấn [C] để cúi mình ẩn náu trong các bụi cỏ lau rậm rạp."
            ),
            (
                "TIẾNG ĐỘNG ĐÁNH LẠC HƯỚNG",
                "Nhặt các viên sỏi ven bờ bằng phím [E] và canh góc ném để tạo tiếng động, dụ lính gác rời xa khỏi đường di chuyển của bạn."
            ),
            (
                "CHUYẾN TÀU TẬP KẾT 1954",
                "Chuyến tàu tập kết năm 1954 tại bến Sông Đốc, Cà Mau mang theo những nắm đất miền Nam cùng lời ước hẹn sắt son ngày trở về."
            ),
            (
                "CHẠY NƯỚC RÚT VƯỢT HIỂM NGUY",
                "Giữ phím [Left Shift] khi di chuyển để bứt tốc vượt qua các khoảng trống nguy hiểm, nhưng hãy cẩn thận vì tiếng bước chân sẽ vang xa hơn."
            ),
            (
                "VƯỢT SÔNG NƯỚC CÀ MAU",
                "Mặt nước rừng ngập mặn ẩn chứa nhiều dòng xoáy và chông gỗ gãy. Hãy quan sát nhịp sóng và căn nhịp nhảy [Space] thật chuẩn xác."
            ),
            (
                "NGƯỜI BẠN ĐỒNG HÀNH",
                "An luôn có một người bạn nhỏ trung thành bên cạnh. Hãy phối hợp để luồn qua những lối đi hẹp mà người lớn không thể với tới."
            )
        };

        private class MenuButtonItem
        {
            public RectTransform RootRect;
            public Button ButtonComp;
            public Text LabelText;
            public Text LeftDots;
            public Text RightDots;
            public string Title;
            public float HoverBlend;
            public bool IsInteractable = true;
        }

        private class SaveSlotUIItem
        {
            public RectTransform RootRect;
            public Button ButtonComp;
            public Text SlotTitleText;
            public Text SlotDetailsText;
            public Text StatusBadgeText;
            public Image CardBg;
            public int SlotIndex;
            public bool IsOccupied;
        }

        public static void TriggerAutosave(string location = "Khởi hành")
        {
            if (instance != null)
            {
                instance.StartAutosaveAnimation(location);
            }
        }

        public void StartAutosaveAnimation(string location)
        {
            autosaveTimer = 2.4f;
            if (autosaveWidgetGroup != null) autosaveWidgetGroup.alpha = 1f;
            if (autosaveStatusText != null)
            {
                autosaveStatusText.text = "ĐANG TỰ ĐỘNG LƯU...";
                autosaveStatusText.color = new Color(0.98f, 0.94f, 0.85f, 0.92f);
            }
        }

        private void Awake()
        {
            instance = this;
            SetupAudioSources();
            LoadPersistedSettings();
        }

        private void Start()
        {
            mainCam = Camera.main;
            EnsureEventSystem();
            RebuildUIImmediate();

            // Set initial state: Pure Black Screen (State A)
            SetPhase(MenuPhase.InitialStartup);
        }

        private void SetupAudioSources()
        {
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
                musicSource.volume = 0f;
            }

            if (windSource == null)
            {
                windSource = gameObject.AddComponent<AudioSource>();
                windSource.loop = true;
                windSource.playOnAwake = false;
                windSource.volume = 0f;
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }

            if (mainMenuMusicClip != null) musicSource.clip = mainMenuMusicClip;
            if (ambientWindClip != null) windSource.clip = ambientWindClip;
        }

        public void ConfigureAssets(
            Sprite studioSprite,
            Sprite officialGameLogo,
            Sprite flowerSprite,
            Sprite doveSprite,
            Sprite dotSprite,
            AudioClip bgmClip,
            AudioClip windClip,
            string gameplayScene = "",
            Sprite headphoneSpr = null)
        {
            ConfigureAssets(studioSprite, officialGameLogo, flowerSprite, doveSprite, dotSprite, null, null, bgmClip, windClip, gameplayScene);
        }

        public void ConfigureAssets(
            Sprite studioSprite,
            Sprite officialGameLogo,
            Sprite flowerSprite,
            Sprite doveSprite,
            Sprite dotSprite,
            Sprite boySprite,
            Sprite shipSprite,
            AudioClip bgmClip,
            AudioClip windClip,
            string gameplayScene = "",
            Sprite headphoneSpr = null)
        {
            studioLogoSprite = studioSprite;
            gameOfficialLogoSprite = officialGameLogo;
            flowerBloomSprite = flowerSprite;
            doveSilhouetteSprite = doveSprite ?? flowerSprite;
            softDotSprite = dotSprite;
            boySilhouetteSprite = boySprite;
            shipSilhouetteSprite = shipSprite;
            if (headphoneSpr != null) headphoneSprite = headphoneSpr;
            mainMenuMusicClip = bgmClip;
            ambientWindClip = windClip;
            if (!string.IsNullOrEmpty(gameplayScene)) targetGameplaySceneName = gameplayScene;

            SetupAudioSources();
        }

        public void SetMenuCameraSettings(Vector3 pos, Vector3 euler, float fov)
        {
            menuCameraPosition = pos;
            menuCameraEuler = euler;
            menuCameraFOV = fov;
        }

        public void RebuildUIImmediate()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.name.StartsWith("Canvas_PlanetOfLana"))
                {
                    child.gameObject.SetActive(false);
                    if (Application.isPlaying)
                    {
                        Destroy(child.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(child.gameObject);
                    }
                }
            }
            EnsureEventSystem();
            BuildRuntimeUI();
        }

        public void SetPreviewState(MenuPhase phase)
        {
            SetPhase(phase);
        }

        public void SetPreviewState(MenuPhase phase, float alpha)
        {
            SetPhase(phase);
            if (phase == MenuPhase.TeamLoadingScreen && teamLoadingGroup != null)
            {
                teamLoadingGroup.alpha = alpha;
            }
            else if (phase == MenuPhase.HeadphoneScreen && headphoneGroup != null)
            {
                headphoneGroup.alpha = alpha;
                if (blackCurtainGroup != null) blackCurtainGroup.alpha = 1f;
            }
            else if (phase == MenuPhase.StartMenuOpenVista && startMenuGroup != null)
            {
                startMenuGroup.alpha = alpha;
                if (blackCurtainGroup != null) blackCurtainGroup.alpha = 1f - alpha;
            }
            else if (phase == MenuPhase.Options && optionsModalGroup != null)
            {
                optionsModalGroup.alpha = alpha;
                if (blackCurtainGroup != null) blackCurtainGroup.alpha = 0f;
                if (autosaveWidgetGroup != null) autosaveWidgetGroup.alpha = 0f;
            }
            else if (phase == MenuPhase.BotWLoadingScreen && botwLoadingGroup != null)
            {
                botwLoadingGroup.alpha = alpha;
                if (blackCurtainGroup != null) blackCurtainGroup.alpha = 0f;
                if (autosaveWidgetGroup != null) autosaveWidgetGroup.alpha = 0f;
            }
        }

        // =====================================================================================
        // STATE MACHINE
        // =====================================================================================
        public void SetPhase(MenuPhase newPhase)
        {
            currentPhase = newPhase;
            phaseTimer = 0f;

            switch (newPhase)
            {
                case MenuPhase.InitialStartup:
                    if (blackCurtainGroup != null) blackCurtainGroup.alpha = 1f;
                    if (teamLoadingGroup != null) teamLoadingGroup.alpha = 0f;
                    if (startMenuGroup != null)
                    {
                        startMenuGroup.alpha = 0f;
                        startMenuGroup.interactable = false;
                        startMenuGroup.blocksRaycasts = false;
                    }
                    if (saveModalGroup != null) saveModalGroup.gameObject.SetActive(false);
                    if (optionsModalGroup != null) optionsModalGroup.gameObject.SetActive(false);
                    if (overwriteDialogGroup != null) overwriteDialogGroup.gameObject.SetActive(false);
                    if (botwLoadingGroup != null) botwLoadingGroup.gameObject.SetActive(false);

                    if (musicSource != null)
                    {
                        musicSource.Stop();
                        musicSource.volume = 0f;
                    }
                    isMusicPlaying = false;
                    currentMusicFadeVol = 0f;
                    break;

                case MenuPhase.TeamLoadingScreen:
                    if (blackCurtainGroup != null) blackCurtainGroup.alpha = 1f;
                    if (teamLoadingGroup != null)
                    {
                        teamLoadingGroup.gameObject.SetActive(true);
                        teamLoadingGroup.alpha = 1f;
                    }
                    if (startMenuGroup != null)
                    {
                        startMenuGroup.alpha = 0f;
                        startMenuGroup.interactable = false;
                        startMenuGroup.blocksRaycasts = false;
                    }
                    if (saveModalGroup != null) saveModalGroup.gameObject.SetActive(false);
                    if (optionsModalGroup != null) optionsModalGroup.gameObject.SetActive(false);
                    if (botwLoadingGroup != null) botwLoadingGroup.gameObject.SetActive(false);

                    if (musicSource != null) musicSource.Stop();
                    break;

                case MenuPhase.HeadphoneScreen:
                    if (blackCurtainGroup != null)
                    {
                        blackCurtainGroup.alpha = 1f;
                        blackCurtainGroup.blocksRaycasts = false;
                    }
                    if (teamLoadingGroup != null)
                    {
                        teamLoadingGroup.alpha = 0f;
                        teamLoadingGroup.blocksRaycasts = false;
                        teamLoadingGroup.interactable = false;
                        teamLoadingGroup.gameObject.SetActive(false);
                    }
                    if (headphoneGroup != null)
                    {
                        headphoneGroup.gameObject.SetActive(true);
                        headphoneGroup.alpha = 1f;
                        headphoneGroup.blocksRaycasts = true;
                        headphoneGroup.interactable = true;
                    }
                    if (startMenuGroup != null)
                    {
                        startMenuGroup.alpha = 0f;
                        startMenuGroup.interactable = false;
                        startMenuGroup.blocksRaycasts = false;
                    }
                    if (saveModalGroup != null) saveModalGroup.gameObject.SetActive(false);
                    if (optionsModalGroup != null) optionsModalGroup.gameObject.SetActive(false);
                    if (botwLoadingGroup != null) botwLoadingGroup.gameObject.SetActive(false);

                    if (musicSource != null) musicSource.Stop();
                    break;
                case MenuPhase.StartMenuOpenVista:
                    if (teamLoadingGroup != null)
                    {
                        teamLoadingGroup.alpha = 0f;
                        teamLoadingGroup.blocksRaycasts = false;
                        teamLoadingGroup.interactable = false;
                        teamLoadingGroup.gameObject.SetActive(false);
                    }
                    if (headphoneGroup != null)
                    {
                        headphoneGroup.alpha = 0f;
                        headphoneGroup.blocksRaycasts = false;
                        headphoneGroup.interactable = false;
                        headphoneGroup.gameObject.SetActive(false);
                    }
                    if (blackCurtainGroup != null)
                    {
                        blackCurtainGroup.blocksRaycasts = false;
                    }
                    if (saveModalGroup != null) saveModalGroup.gameObject.SetActive(false);
                    if (optionsModalGroup != null) optionsModalGroup.gameObject.SetActive(false);
                    if (overwriteDialogGroup != null) overwriteDialogGroup.gameObject.SetActive(false);
                    if (botwLoadingGroup != null) botwLoadingGroup.gameObject.SetActive(false);

                    if (startMenuGroup != null)
                    {
                        startMenuGroup.gameObject.SetActive(true);
                        startMenuGroup.interactable = true;
                        startMenuGroup.blocksRaycasts = true;
                    }

                    RefreshContinueButtonState();

                    if (musicSource != null && !musicSource.isPlaying && mainMenuMusicClip != null)
                    {
                        musicSource.volume = 0f;
                        musicSource.Play();
                        isMusicPlaying = true;
                    }
                    if (windSource != null && !windSource.isPlaying && ambientWindClip != null)
                    {
                        windSource.volume = windVolume * 0.7f;
                        windSource.Play();
                    }
                    break;

                case MenuPhase.SaveSlotSelection:
                    if (saveModalGroup != null)
                    {
                        saveModalGroup.gameObject.SetActive(true);
                        saveModalGroup.alpha = 1f;
                        PopulateSaveSlotCards();
                    }
                    if (optionsModalGroup != null) optionsModalGroup.gameObject.SetActive(false);
                    if (startMenuGroup != null)
                    {
                        startMenuGroup.interactable = false;
                    }
                    break;

                case MenuPhase.Options:
                    if (optionsModalGroup != null)
                    {
                        optionsModalGroup.gameObject.SetActive(true);
                        optionsModalGroup.alpha = 1f;
                        optionsModalGroup.interactable = true;
                        optionsModalGroup.blocksRaycasts = true;
                    }
                    if (saveModalGroup != null) saveModalGroup.gameObject.SetActive(false);
                    if (startMenuGroup != null)
                    {
                        startMenuGroup.interactable = false;
                    }
                    if (autosaveWidgetGroup != null)
                    {
                        autosaveWidgetGroup.alpha = 0f;
                        autosaveTimer = 0f;
                    }
                    break;

                case MenuPhase.BotWLoadingScreen:
                    if (startMenuGroup != null)
                    {
                        startMenuGroup.interactable = false;
                        startMenuGroup.blocksRaycasts = false;
                        startMenuGroup.gameObject.SetActive(false);
                    }
                    if (saveModalGroup != null) saveModalGroup.gameObject.SetActive(false);
                    if (optionsModalGroup != null) optionsModalGroup.gameObject.SetActive(false);
                    if (overwriteDialogGroup != null) overwriteDialogGroup.gameObject.SetActive(false);

                    if (blackCurtainGroup != null) blackCurtainGroup.alpha = 0f;
                    if (autosaveWidgetGroup != null)
                    {
                        autosaveWidgetGroup.alpha = 0f;
                        autosaveTimer = 0f;
                    }

                    if (botwLoadingGroup != null)
                    {
                        botwLoadingGroup.gameObject.SetActive(true);
                        botwLoadingGroup.alpha = 1f;
                    }

                    currentTipIndex = UnityEngine.Random.Range(0, botwTips.Length);
                    tipCycleTimer = 0f;
                    UpdateBotWTipContentImmediate();

                    // Start actual asynchronous scene load
                    if (!isAsyncLoadingStarted && Application.isPlaying)
                    {
                        StartCoroutine(AsyncLoadGameplaySceneRoutine());
                    }
                    break;

                case MenuPhase.TransitionToGameplay:
                    if (startMenuGroup != null)
                    {
                        startMenuGroup.interactable = false;
                        startMenuGroup.blocksRaycasts = false;
                    }
                    if (saveModalGroup != null) saveModalGroup.gameObject.SetActive(false);
                    if (optionsModalGroup != null) optionsModalGroup.gameObject.SetActive(false);
                    break;
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            phaseTimer += dt;

            switch (currentPhase)
            {
                case MenuPhase.InitialStartup:
                    UpdateInitialStartup(dt);
                    break;
                case MenuPhase.TeamLoadingScreen:
                    UpdateTeamLoadingScreen(dt);
                    break;
                case MenuPhase.HeadphoneScreen:
                    UpdateHeadphoneScreen(dt);
                    break;
                case MenuPhase.StartMenuOpenVista:
                    UpdateStartMenuVista(dt);
                    break;
                case MenuPhase.SaveSlotSelection:
                    UpdateSaveSlotSelection(dt);
                    break;
                case MenuPhase.Options:
                    UpdateOptionsModal(dt);
                    break;
                case MenuPhase.BotWLoadingScreen:
                    UpdateBotWLoadingScreen(dt);
                    break;
                case MenuPhase.TransitionToGameplay:
                    UpdateTransitionToGameplay(dt);
                    break;
            }

            UpdateAudioLifecycle(dt);
            UpdateAutosaveWidget(dt);
            UpdateScenicCamera(dt);
        }

        private void UpdateInitialStartup(float dt)
        {
            if (blackCurtainGroup != null) blackCurtainGroup.alpha = 1f;

            if (phaseTimer >= initialStartupDuration)
            {
                SetPhase(MenuPhase.TeamLoadingScreen);
            }
        }

        private void UpdateTeamLoadingScreen(float dt)
        {
            float fadeIn = teamLogoFadeInDuration;
            float hold = teamLogoHoldDuration;
            float fadeOut = teamLogoFadeOutDuration;
            float total = fadeIn + hold + fadeOut;

            float alpha = 0f;
            if (phaseTimer < fadeIn)
            {
                alpha = Mathf.Clamp01(phaseTimer / fadeIn);
            }
            else if (phaseTimer < fadeIn + hold)
            {
                alpha = 1.0f;
            }
            else if (phaseTimer < total)
            {
                alpha = Mathf.Clamp01(1.0f - (phaseTimer - fadeIn - hold) / fadeOut);
            }
            else
            {
                alpha = 0f;
            }

            if (teamLoadingGroup != null)
            {
                teamLoadingGroup.alpha = alpha;
            }

            // Subtle breathing of the centered team logo
            if (teamLogoRect != null)
            {
                float s = 1.0f + Mathf.Sin(phaseTimer * 2.2f) * 0.025f;
                teamLogoRect.localScale = new Vector3(s, s, 1f);
            }

            // Allow skip via keypress or click after 0.15s
            if (phaseTimer >= 0.15f && (IsConfirmPressed() || IsAnyKeyPressed()))
            {
                SetPhase(MenuPhase.HeadphoneScreen);
                return;
            }

            if (phaseTimer >= total)
            {
                SetPhase(MenuPhase.HeadphoneScreen);
            }
        }

        private void UpdateHeadphoneScreen(float dt)
        {
            float fadeIn = headphoneFadeInDuration;
            float hold = headphoneHoldDuration;
            float fadeOut = headphoneFadeOutDuration;
            float total = fadeIn + hold + fadeOut;

            float alpha = 0f;
            if (phaseTimer < fadeIn)
            {
                alpha = Mathf.Clamp01(phaseTimer / fadeIn);
            }
            else if (phaseTimer < fadeIn + hold)
            {
                alpha = 1.0f;
            }
            else if (phaseTimer < total)
            {
                alpha = Mathf.Clamp01(1.0f - (phaseTimer - fadeIn - hold) / fadeOut);
            }
            else
            {
                alpha = 0f;
            }

            if (headphoneGroup != null)
            {
                headphoneGroup.alpha = alpha;
            }

            // Allow skip via keypress or click after 0.15s
            if (phaseTimer >= 0.15f && (IsConfirmPressed() || IsAnyKeyPressed()))
            {
                SetPhase(MenuPhase.StartMenuOpenVista);
                return;
            }

            if (phaseTimer >= total)
            {
                SetPhase(MenuPhase.StartMenuOpenVista);
            }
        }


        private void UpdateStartMenuVista(float dt)
        {
            if (blackCurtainGroup != null)
            {
                blackCurtainGroup.alpha = Mathf.MoveTowards(blackCurtainGroup.alpha, 0f, dt * 1.5f);
            }

            if (startMenuGroup != null)
            {
                startMenuGroup.alpha = Mathf.MoveTowards(startMenuGroup.alpha, 1f, dt * 1.5f);
            }

            // Menu Keyboard Navigation
            if (IsNavDownPressed())
            {
                int next = (selectedMenuIndex + 1) % menuButtons.Count;
                if (!menuButtons[next].IsInteractable) next = (next + 1) % menuButtons.Count;
                selectedMenuIndex = next;
                PlayHover();
            }
            else if (IsNavUpPressed())
            {
                int prev = (selectedMenuIndex - 1 + menuButtons.Count) % menuButtons.Count;
                if (!menuButtons[prev].IsInteractable) prev = (prev - 1 + menuButtons.Count) % menuButtons.Count;
                selectedMenuIndex = prev;
                PlayHover();
            }
            else if (IsConfirmPressed())
            {
                TriggerMenuSelection(selectedMenuIndex);
            }

            // Animate button hover visual blends
            for (int i = 0; i < menuButtons.Count; i++)
            {
                var item = menuButtons[i];
                if (!item.IsInteractable) continue;
                bool isSel = (i == selectedMenuIndex);
                item.HoverBlend = Mathf.MoveTowards(item.HoverBlend, isSel ? 1f : 0f, dt * 7.0f);
                UpdateMenuItemVisual(item);
            }
        }

        private void UpdateSaveSlotSelection(float dt)
        {
            if (IsCancelPressed())
            {
                if (overwriteDialogGroup != null && overwriteDialogGroup.gameObject.activeSelf)
                {
                    HideOverwriteConfirmation();
                }
                else
                {
                    CloseSaveModal();
                }
                return;
            }

            if (overwriteDialogGroup != null && overwriteDialogGroup.gameObject.activeSelf) return;

            if (IsNavDownPressed())
            {
                selectedSlotIndex = (selectedSlotIndex + 1) % slotUIItems.Count;
                PlayHover();
                HighlightSelectedSlot();
            }
            else if (IsNavUpPressed())
            {
                selectedSlotIndex = (selectedSlotIndex - 1 + slotUIItems.Count) % slotUIItems.Count;
                PlayHover();
                HighlightSelectedSlot();
            }
            else if (IsConfirmPressed())
            {
                if (selectedSlotIndex >= 0 && selectedSlotIndex < slotUIItems.Count)
                {
                    OnSlotCardClicked(slotUIItems[selectedSlotIndex].SlotIndex);
                }
            }
        }

        private void UpdateOptionsModal(float dt)
        {
            if (IsCancelPressed())
            {
                CloseOptionsDialog();
            }
        }

        private void UpdateBotWLoadingScreen(float dt)
        {
            tipCycleTimer += dt;
            if (tipCycleTimer >= 6.0f)
            {
                NextTip();
            }
            else if (IsConfirmPressed() || IsAnyKeyPressed())
            {
                NextTip();
            }

            // 1. Hopping Silhouette of Cậu Bé An along the Horizon Line
            if (botwAnRect != null)
            {
                float screenW = 1920f;
                float runSpeed = 125f;
                float trackX = ((phaseTimer * runSpeed) % (screenW + 500f)) - (screenW / 2f + 250f);

                // Hopping cadence (jumping stride)
                float hop = Mathf.Abs(Mathf.Sin(phaseTimer * 8.5f)) * 18f;
                botwAnRect.anchoredPosition = new Vector2(trackX, hop + 2f);

                // Subtle squash and stretch during strides
                float stride = Mathf.Sin(phaseTimer * 8.5f) * 0.08f;
                botwAnRect.localScale = new Vector3(1f - stride, 1f + stride, 1f);
            }

            // 2. Sailing Silhouette of the 1954 Transport Ship behind An
            if (botwShipRect != null && botwAnRect != null)
            {
                float shipX = botwAnRect.anchoredPosition.x - 280f;
                float wave = Mathf.Sin(phaseTimer * 2.8f) * 6.0f;
                float pitch = Mathf.Sin(phaseTimer * 2.8f + 0.8f) * 2.5f;

                botwShipRect.anchoredPosition = new Vector2(shipX, wave - 2f);
                botwShipRect.localRotation = Quaternion.Euler(0f, 0f, pitch);
            }

            // 3. Pulse Sheikah-Style Dots in Bottom-Right
            for (int i = 0; i < botwSheikahDots.Count; i++)
            {
                var dot = botwSheikahDots[i];
                if (dot != null)
                {
                    float angle = (float)i / botwSheikahDots.Count * Mathf.PI * 2f;
                    float wave = (Mathf.Sin(phaseTimer * 4.2f - angle) + 1f) * 0.5f;
                    dot.color = new Color(0.99f, 0.95f, 0.88f, Mathf.Lerp(0.20f, 0.95f, wave));
                }
            }

            // Smooth tip text alpha
            if (botwTipTextGroup != null)
            {
                botwTipTextGroup.alpha = Mathf.MoveTowards(botwTipTextGroup.alpha, 1f, dt * 3.5f);
            }
        }

        private void UpdateTransitionToGameplay(float dt)
        {
            if (blackCurtainGroup != null)
            {
                blackCurtainGroup.alpha = Mathf.MoveTowards(blackCurtainGroup.alpha, 1f, dt * 1.8f);
            }

            if (musicSource != null)
            {
                musicSource.volume = Mathf.MoveTowards(musicSource.volume, 0f, dt * (musicVolume / 1.2f));
            }
        }

        private void UpdateAudioLifecycle(float dt)
        {
            if (currentPhase == MenuPhase.StartMenuOpenVista || currentPhase == MenuPhase.SaveSlotSelection || currentPhase == MenuPhase.Options)
            {
                if (isMusicPlaying && musicSource != null)
                {
                    currentMusicFadeVol = Mathf.MoveTowards(currentMusicFadeVol, 1f, dt / Mathf.Max(0.1f, musicSmoothInDuration));
                    musicSource.volume = musicVolume * Mathf.SmoothStep(0f, 1f, currentMusicFadeVol);
                }

                if (windSource != null && windSource.isPlaying)
                {
                    windSource.volume = windVolume;
                }
            }
            else if (currentPhase == MenuPhase.BotWLoadingScreen)
            {
                if (musicSource != null)
                {
                    musicSource.volume = Mathf.MoveTowards(musicSource.volume, 0f, dt * 0.6f);
                }
                if (windSource != null)
                {
                    windSource.volume = Mathf.MoveTowards(windSource.volume, 0f, dt * 0.6f);
                }
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
                    autosaveSpinnerRect.Rotate(0f, 0f, -dt * 180f);
                }
            }
            else
            {
                autosaveWidgetGroup.alpha = Mathf.MoveTowards(autosaveWidgetGroup.alpha, 0f, dt * 2.5f);
            }
        }

        private void UpdateScenicCamera(float dt)
        {
            if (mainCam == null) return;
            float t = Time.unscaledTime;
            Vector3 drift = new Vector3(
                Mathf.Sin(t * 0.22f) * 0.12f,
                Mathf.Cos(t * 0.18f) * 0.06f,
                0f
            );
            mainCam.transform.position = menuCameraPosition + drift;
            mainCam.fieldOfView = menuCameraFOV;
        }

        // =====================================================================================
        // TIPS & ASYNC LOADING ROUTINE
        // =====================================================================================
        public void NextTip()
        {
            currentTipIndex = (currentTipIndex + 1) % botwTips.Length;
            tipCycleTimer = 0f;
            if (botwTipTextGroup != null)
            {
                botwTipTextGroup.alpha = 0f;
            }
            UpdateBotWTipContentImmediate();
        }

        private void UpdateBotWTipContentImmediate()
        {
            if (botwTipCategoryText != null)
            {
                botwTipCategoryText.text = botwTips[currentTipIndex].Category;
            }
            if (botwTipBodyText != null)
            {
                botwTipBodyText.text = botwTips[currentTipIndex].Body;
            }
        }

        private IEnumerator AsyncLoadGameplaySceneRoutine()
        {
            isAsyncLoadingStarted = true;
            float startTime = Time.unscaledTime;

            asyncSceneLoadOp = SceneManager.LoadSceneAsync(targetGameplaySceneName);
            if (asyncSceneLoadOp != null)
            {
                asyncSceneLoadOp.allowSceneActivation = false;

                while (asyncSceneLoadOp.progress < 0.9f || (Time.unscaledTime - startTime) < botwMinLoadingDuration)
                {
                    yield return null;
                }

                // Smooth fade out to black
                float fadeDur = 0.6f;
                float t = 0f;
                while (t < fadeDur)
                {
                    t += Time.unscaledDeltaTime;
                    if (blackCurtainGroup != null)
                    {
                        blackCurtainGroup.alpha = Mathf.Clamp01(t / fadeDur);
                    }
                    yield return null;
                }

                Time.timeScale = 1.0f;
                asyncSceneLoadOp.allowSceneActivation = true;
            }
            else
            {
                Debug.LogWarning($"[StartMenu] Scene '{targetGameplaySceneName}' not found in Build Settings! Loading via synchronous fallback.");
                yield return new WaitForSecondsRealtime(botwMinLoadingDuration);
                SceneManager.LoadScene(targetGameplaySceneName);
            }
        }

        // =====================================================================================
        // BUTTON ACTIONS
        // =====================================================================================
        private void TriggerMenuSelection(int index)
        {
            PlayConfirm();
            switch (index)
            {
                case 0: // BẮT ĐẦU (New Game)
                    OpenSaveModal(SaveModalMode.NewGame);
                    break;
                case 1: // TIẾP TỤC (Continue)
                    OpenSaveModal(SaveModalMode.Continue);
                    break;
                case 2: // TÙY CHỈNH (Options)
                    OpenOptionsDialog();
                    break;
                case 3: // THOÁT (Quit)
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                    break;
            }
        }

        private void OpenSaveModal(SaveModalMode mode)
        {
            currentSaveMode = mode;
            if (saveModalTitleText != null)
            {
                saveModalTitleText.text = (mode == SaveModalMode.NewGame)
                    ? "C H Ọ N   V Ị   T R Í   L Ư U   ( B Ắ T   Đ Ầ U   M Ớ I )"
                    : "C H Ọ N   B Ả N   L Ư U   ( T I Ế P   T Ụ C )";
            }
            if (saveModalNoticeText != null)
            {
                saveModalNoticeText.text = "";
            }
            SetPhase(MenuPhase.SaveSlotSelection);
        }

        public void CloseSaveModal()
        {
            PlayConfirm();
            SetPhase(MenuPhase.StartMenuOpenVista);
        }

        public void OpenOptionsDialog()
        {
            PlayConfirm();
            SetPhase(MenuPhase.Options);
        }

        public void CloseOptionsDialog()
        {
            PlayConfirm();
            SavePersistedSettings();
            SetPhase(MenuPhase.StartMenuOpenVista);
        }

        public void ShowOptionsDialog(bool show)
        {
            if (show) OpenOptionsDialog();
            else CloseOptionsDialog();
        }

        private void PopulateSaveSlotCards()
        {
            for (int i = 0; i < slotUIItems.Count; i++)
            {
                var card = slotUIItems[i];
                int slot = card.SlotIndex;
                var data = HoaVaMauSaveSystem.GetSlotData(slot);
                card.IsOccupied = data.isOccupied;

                card.SlotTitleText.text = $"VỊ TRÍ {slot}";
                if (data.isOccupied)
                {
                    card.SlotDetailsText.text = $"{data.chapterTitle} • {data.locationName}\nThời gian: {data.GetFormattedPlayTime()}   |   Ngày: {data.saveTimestamp}";
                    card.StatusBadgeText.text = (currentSaveMode == SaveModalMode.NewGame) ? "[ ĐÃ CÓ TIẾN TRÌNH ]" : "[ TIẾP TỤC ]";
                    card.StatusBadgeText.color = new Color(0.98f, 0.88f, 0.65f, 0.95f);
                }
                else
                {
                    card.SlotDetailsText.text = "Vị trí chưa có dữ liệu lưu. Bắt đầu từ đầu chương 1.";
                    card.StatusBadgeText.text = (currentSaveMode == SaveModalMode.NewGame) ? "[ TRỐNG - BẮT ĐẦU ]" : "[ TRỐNG ]";
                    card.StatusBadgeText.color = new Color(0.70f, 0.80f, 0.90f, 0.65f);
                }
            }
            selectedSlotIndex = 0;
            HighlightSelectedSlot();
        }

        private void HighlightSelectedSlot()
        {
            for (int i = 0; i < slotUIItems.Count; i++)
            {
                var card = slotUIItems[i];
                bool isSel = (i == selectedSlotIndex);
                if (card.CardBg != null)
                {
                    card.CardBg.color = isSel ? new Color(0.18f, 0.22f, 0.32f, 0.90f) : new Color(0.06f, 0.08f, 0.12f, 0.78f);
                }
                card.RootRect.localScale = isSel ? new Vector3(1.02f, 1.02f, 1f) : Vector3.one;
            }
        }

        public void OnSlotCardClicked(int slotIndex)
        {
            PlayConfirm();
            var data = HoaVaMauSaveSystem.GetSlotData(slotIndex);

            if (currentSaveMode == SaveModalMode.NewGame)
            {
                if (data.isOccupied)
                {
                    ShowOverwriteConfirmation(slotIndex);
                }
                else
                {
                    // Initialize clean slot data and transition to BotW loading screen
                    IntroCutsceneState.MarkForNewGame();
                    HoaVaMauSaveSystem.ActiveSlotIndex = slotIndex;
                    HoaVaMauSaveSystem.SessionPlayTimeCounter = 0f;
                    HoaVaMauSaveSystem.SaveCurrentGameState(slotIndex, "Chương 1: Đất Đỏ Cà Mau", "Khởi hành", 0, new Vector3(-8.5f, 0.05f, 0f));
                    SetPhase(MenuPhase.BotWLoadingScreen);
                }
            }
            else // Continue Mode
            {
                if (data.isOccupied)
                {
                    HoaVaMauSaveSystem.ActiveSlotIndex = slotIndex;
                    HoaVaMauSaveSystem.SessionPlayTimeCounter = 0f;
                    if (data.checkpointIndex == 0)
                    {
                        IntroCutsceneState.MarkForNewGame();
                    }
                    else
                    {
                        IntroCutsceneState.MarkForContinue(data.checkpointIndex);
                    }
                    SetPhase(MenuPhase.BotWLoadingScreen);
                }
                else
                {
                    PlayHover();
                    if (saveModalNoticeText != null)
                    {
                        saveModalNoticeText.text = $"⚠️ Vị trí {slotIndex} chưa có dữ liệu lưu. Hãy chọn vị trí đã có tiến trình hoặc Bắt đầu mới!";
                    }
                }
            }
        }

        private void ShowOverwriteConfirmation(int slotIndex)
        {
            pendingOverwriteSlot = slotIndex;
            if (overwriteDialogGroup != null)
            {
                overwriteDialogGroup.gameObject.SetActive(true);
                overwriteDialogGroup.alpha = 1f;
            }
            if (overwriteDialogText != null)
            {
                overwriteDialogText.text = $"Vị trí lưu [VỊ TRÍ {slotIndex}] đã có tiến trình chơi.\nBắt đầu trò chơi mới sẽ ghi đè lên dữ liệu cũ.\nBạn có chắc chắn muốn ghi đè không?";
            }
        }

        public void HideOverwriteConfirmation()
        {
            PlayConfirm();
            pendingOverwriteSlot = -1;
            if (overwriteDialogGroup != null)
            {
                overwriteDialogGroup.gameObject.SetActive(false);
            }
        }

        public void ConfirmOverwriteSlot()
        {
            if (pendingOverwriteSlot > 0)
            {
                PlayConfirm();
                IntroCutsceneState.MarkForNewGame();
                HoaVaMauSaveSystem.ActiveSlotIndex = pendingOverwriteSlot;
                HoaVaMauSaveSystem.SessionPlayTimeCounter = 0f;
                HoaVaMauSaveSystem.SaveCurrentGameState(pendingOverwriteSlot, "Chương 1: Đất Đỏ Cà Mau", "Khởi hành", 0, new Vector3(-8.5f, 0.05f, 0f));
                SetPhase(MenuPhase.BotWLoadingScreen);
            }
        }

        private void RefreshContinueButtonState()
        {
            bool hasSaves = HoaVaMauSaveSystem.HasAnySaveData();
            if (continueButtonItem != null)
            {
                continueButtonItem.IsInteractable = hasSaves;
                if (continueButtonItem.ButtonComp != null)
                {
                    continueButtonItem.ButtonComp.interactable = hasSaves;
                }
                if (continueButtonItem.LabelText != null)
                {
                    continueButtonItem.LabelText.color = hasSaves
                        ? new Color(0.92f, 0.95f, 0.98f, 0.75f)
                        : new Color(0.60f, 0.65f, 0.70f, 0.32f);
                }
            }
        }

        private static void UpdateMenuItemVisual(MenuButtonItem item)
        {
            if (item == null || item.LabelText == null) return;
            if (!item.IsInteractable) return;

            Color col = Color.Lerp(new Color(0.92f, 0.95f, 0.98f, 0.75f), new Color(1.0f, 0.98f, 0.88f, 1.0f), item.HoverBlend);
            item.LabelText.color = col;

            float scale = Mathf.Lerp(1.0f, 1.05f, item.HoverBlend);
            item.RootRect.localScale = new Vector3(scale, scale, 1f);

            Color dotCol = new Color(1.0f, 0.95f, 0.80f, item.HoverBlend * 0.92f);
            if (item.LeftDots != null) item.LeftDots.color = dotCol;
            if (item.RightDots != null) item.RightDots.color = dotCol;
        }

        private void PlayHover()
        {
            if (sfxSource != null && hoverSfxClip != null) sfxSource.PlayOneShot(hoverSfxClip, sfxVolume * 0.75f);
        }

        private void PlayConfirm()
        {
            if (sfxSource != null && confirmSfxClip != null) sfxSource.PlayOneShot(confirmSfxClip, sfxVolume);
        }

        // =====================================================================================
        // PERSISTED SETTINGS (AUDIO & GRAPHICS)
        // =====================================================================================
        private void LoadPersistedSettings()
        {
            float mVol = PlayerPrefs.GetFloat("Settings_MasterVol", 1.0f);
            float bgmVol = PlayerPrefs.GetFloat("Settings_MusicVol", 0.65f);
            float sVol = PlayerPrefs.GetFloat("Settings_SFXVol", 0.45f);

            AudioListener.volume = mVol;
            musicVolume = bgmVol;
            windVolume = sVol * 0.5f;
            sfxVolume = sVol;
        }

        private void SavePersistedSettings()
        {
            PlayerPrefs.SetFloat("Settings_MasterVol", AudioListener.volume);
            PlayerPrefs.SetFloat("Settings_MusicVol", musicVolume);
            PlayerPrefs.SetFloat("Settings_SFXVol", sfxVolume);
            PlayerPrefs.Save();
        }

        private void OnMasterVolumeChanged(float val)
        {
            AudioListener.volume = val;
            if (masterVolValText != null) masterVolValText.text = $"{Mathf.RoundToInt(val * 100f)}%";
        }

        private void OnMusicVolumeChanged(float val)
        {
            musicVolume = val;
            if (musicSource != null) musicSource.volume = musicVolume * currentMusicFadeVol;
            if (musicVolValText != null) musicVolValText.text = $"{Mathf.RoundToInt(val * 100f)}%";
        }

        private void OnSfxVolumeChanged(float val)
        {
            sfxVolume = val;
            windVolume = val * 0.5f;
            if (windSource != null) windSource.volume = windVolume;
            if (sfxVolValText != null) sfxVolValText.text = $"{Mathf.RoundToInt(val * 100f)}%";
        }

        private void SetQualityLevelUI(int level)
        {
            QualitySettings.SetQualityLevel(level, true);
            PlayerPrefs.SetInt("Settings_Quality", level);
            PlayerPrefs.Save();
            HighlightSelectedQualityButton(level);
        }

        private void HighlightSelectedQualityButton(int activeLevel)
        {
            for (int i = 0; i < qualityButtons.Length; i++)
            {
                if (qualityButtons[i] == null) continue;
                var img = qualityButtons[i].GetComponent<Image>();
                bool isCur = (i == activeLevel);
                if (img != null)
                {
                    img.color = isCur ? new Color(0.85f, 0.42f, 0.18f, 0.90f) : new Color(0.12f, 0.15f, 0.22f, 0.85f);
                }
                if (qualityBtnTexts[i] != null)
                {
                    qualityBtnTexts[i].color = isCur ? Color.white : new Color(0.75f, 0.80f, 0.88f, 0.80f);
                }
            }
        }

        private void ToggleFullscreenMode()
        {
            Screen.fullScreen = !Screen.fullScreen;
            if (fullscreenBtnText != null)
            {
                fullscreenBtnText.text = Screen.fullScreen ? "TOÀN MÀN HÌNH: [ BẬT ]" : "TOÀN MÀN HÌNH: [ TẮT ]";
            }
        }

        // =====================================================================================
        // UI CONSTRUCTION & CANVAS HIERARCHY
        // =====================================================================================
        private void BuildRuntimeUI()
        {
            var canvasGO = new GameObject("Canvas_PlanetOfLana_StartMenu");
            canvasGO.transform.SetParent(transform, false);
            rootCanvas = canvasGO.AddComponent<Canvas>();
            rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            rootCanvas.sortingOrder = 100;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            Font font = Font.CreateDynamicFontFromOSFont("Segoe UI", 36);
            if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 36);
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // 1. Black Curtain
            var curtainGO = CreateRect("Black_Boot_Curtain", canvasGO.transform);
            StretchFull(curtainGO);
            var cImg = curtainGO.gameObject.AddComponent<Image>();
            cImg.color = new Color(0.01f, 0.01f, 0.02f, 1.0f);
            cImg.raycastTarget = false;
            blackCurtainGroup = curtainGO.gameObject.AddComponent<CanvasGroup>();
            blackCurtainGroup.alpha = 1f;
            blackCurtainGroup.blocksRaycasts = false;

            // 2. State B Panel: Team Loading Screen (Five Cows Centered, Circular 2.5D Orbit Dove Loader)
            BuildTeamLoadingPanel(canvasGO.transform, font);
            // 2b. Step 2 Panel: Headphone Advisory Screen
            BuildHeadphonePanel(canvasGO.transform, font);

            // 3. State C Panel: Main Menu (Top-Left Anchored, enlarged logo, NO Five Cows above it)
            BuildMainMenuPanel(canvasGO.transform, font);

            // 4. Save Slot Selection Modal
            BuildSaveSlotModal(canvasGO.transform, font);

            // 5. Options Modal (Tùy Chỉnh)
            BuildOptionsModal(canvasGO.transform, font);

            // 6. Overwrite Confirmation Dialog
            BuildOverwriteConfirmationDialog(canvasGO.transform, font);

            // 7. Legend of Zelda Breath of the Wild Style Loading Screen
            BuildBotWLoadingPanel(canvasGO.transform, font);

            // 8. Autosave Bottom-Right Corner Widget
            BuildAutosaveWidget(canvasGO.transform, font);
        }

        private void BuildHeadphonePanel(Transform parent, Font font)
        {
            var panelRect = CreateRect("Panel_Step2_HeadphoneAdvisory", parent);
            StretchFull(panelRect);
            var bgImg = panelRect.gameObject.AddComponent<Image>();
            bgImg.color = Color.clear;
            bgImg.raycastTarget = true;
            var clickBtn = panelRect.gameObject.AddComponent<Button>();
            clickBtn.transition = Selectable.Transition.None;
            clickBtn.onClick.AddListener(() =>
            {
                if (currentPhase == MenuPhase.HeadphoneScreen)
                {
                    SetPhase(MenuPhase.StartMenuOpenVista);
                }
            });
            headphoneGroup = panelRect.gameObject.AddComponent<CanvasGroup>();
            headphoneGroup.alpha = 0f;
            headphoneGroup.blocksRaycasts = false;
            headphoneGroup.interactable = false;
            panelRect.gameObject.SetActive(false);

            var centerCluster = CreateRect("Center_HeadphoneCluster", panelRect);
            centerCluster.anchorMin = centerCluster.anchorMax = new Vector2(0.5f, 0.5f);
            centerCluster.pivot = new Vector2(0.5f, 0.5f);
            centerCluster.sizeDelta = new Vector2(900f, 260f);

            var iconGO = CreateRect("HeadphoneIcon", centerCluster);
            iconGO.anchorMin = iconGO.anchorMax = new Vector2(0.5f, 0.68f);
            iconGO.pivot = new Vector2(0.5f, 0.5f);
            iconGO.sizeDelta = new Vector2(100f, 100f);
            headphoneIconRect = iconGO;
            var iconImg = iconGO.gameObject.AddComponent<Image>();
            iconImg.sprite = headphoneSprite;
            iconImg.preserveAspect = true;
            iconImg.color = new Color(0.96f, 0.95f, 0.90f, 0.95f);
            iconImg.raycastTarget = false;

            var textGO = CreateRect("HeadphoneText", centerCluster);
            textGO.anchorMin = textGO.anchorMax = new Vector2(0.5f, 0.22f);
            textGO.pivot = new Vector2(0.5f, 0.5f);
            textGO.sizeDelta = new Vector2(850f, 50f);
            headphoneText = textGO.gameObject.AddComponent<Text>();
            headphoneText.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            headphoneText.fontSize = 22;
            headphoneText.fontStyle = FontStyle.Normal;
            headphoneText.alignment = TextAnchor.MiddleCenter;
            headphoneText.color = new Color(0.96f, 0.95f, 0.90f, 0.95f);
            headphoneText.text = "Trải nghiệm trọn vẹn hơn khi đeo tai nghe";
            headphoneText.raycastTarget = false;

            var shadow = textGO.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
        }

        private void BuildTeamLoadingPanel(Transform parent, Font font)
        {
            var panelRect = CreateRect("Panel_StateB_TeamLoading", parent);
            StretchFull(panelRect);
            var bgImg = panelRect.gameObject.AddComponent<Image>();
            bgImg.color = Color.clear;
            bgImg.raycastTarget = true;
            var clickBtn = panelRect.gameObject.AddComponent<Button>();
            clickBtn.transition = Selectable.Transition.None;
            clickBtn.onClick.AddListener(() =>
            {
                if (currentPhase == MenuPhase.TeamLoadingScreen)
                {
                    SetPhase(MenuPhase.HeadphoneScreen);
                }
            });
            teamLoadingGroup = panelRect.gameObject.AddComponent<CanvasGroup>();
            teamLoadingGroup.alpha = 0f;
            teamLoadingGroup.blocksRaycasts = false;
            teamLoadingGroup.interactable = false;
            panelRect.gameObject.SetActive(false);

            // Centered Team Emblem & Title
            var centerCluster = CreateRect("Center_TeamCluster", panelRect);
            centerCluster.anchorMin = centerCluster.anchorMax = new Vector2(0.5f, 0.5f);
            centerCluster.pivot = new Vector2(0.5f, 0.5f);
            centerCluster.sizeDelta = new Vector2(500f, 360f);

            var logoGO = CreateRect("TeamLogo_FiveCowsInTheFire", centerCluster);
            logoGO.anchorMin = logoGO.anchorMax = new Vector2(0.5f, 0.65f);
            logoGO.pivot = new Vector2(0.5f, 0.5f);
            logoGO.sizeDelta = new Vector2(230f, 230f);
            teamLogoRect = logoGO;
            var logoImg = logoGO.gameObject.AddComponent<Image>();
            logoImg.sprite = studioLogoSprite;
            logoImg.preserveAspect = true;
            logoImg.color = Color.white;

            var titleGO = CreateRect("Team_Title_Text", centerCluster);
            titleGO.anchorMin = titleGO.anchorMax = new Vector2(0.5f, 0.18f);
            titleGO.pivot = new Vector2(0.5f, 0.5f);
            titleGO.sizeDelta = new Vector2(650f, 40f);
            var titleTxt = titleGO.gameObject.AddComponent<Text>();
            titleTxt.font = font;
            titleTxt.fontSize = 22;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.color = new Color(0.99f, 0.96f, 0.88f, 0.98f);
            titleTxt.text = "F I V E   C O W S   I N   T H E   F I R E";

            var presGO = CreateRect("Team_Presents_Text", centerCluster);
            presGO.anchorMin = presGO.anchorMax = new Vector2(0.5f, 0.06f);
            presGO.pivot = new Vector2(0.5f, 0.5f);
            presGO.sizeDelta = new Vector2(400f, 24f);
            var presTxt = presGO.gameObject.AddComponent<Text>();
            presTxt.font = font;
            presTxt.fontSize = 13;
            presTxt.alignment = TextAnchor.MiddleCenter;
            presTxt.color = new Color(0.95f, 0.88f, 0.72f, 0.85f);
            presTxt.text = "P R E S E N T S";

            // CIRCULAR 2.5D ORBIT DOVE LOADER (Bottom-Right Corner)
            var loaderCorner = CreateRect("Loader_BottomRight_Corner", panelRect);
            loaderCorner.anchorMin = loaderCorner.anchorMax = new Vector2(1.0f, 0.0f);
            loaderCorner.pivot = new Vector2(1.0f, 0.0f);
            loaderCorner.anchoredPosition = new Vector2(-70f, 48f);
            loaderCorner.sizeDelta = new Vector2(220f, 60f);

            var orbitRoot = CreateRect("OrbitRoot", loaderCorner);
            orbitRoot.anchorMin = orbitRoot.anchorMax = new Vector2(1.0f, 0.5f);
            orbitRoot.pivot = new Vector2(0.5f, 0.5f);
            orbitRoot.anchoredPosition = new Vector2(-30f, 0f);
            orbitRoot.sizeDelta = new Vector2(60f, 60f);

            // Circular ring / orbital dots track
            teamOrbitDots.Clear();
            int dotCount = 8;
            for (int i = 0; i < dotCount; i++)
            {
                var dotRect = CreateRect($"TrackDot_{i}", orbitRoot);
                dotRect.anchorMin = dotRect.anchorMax = new Vector2(0.5f, 0.5f);
                dotRect.pivot = new Vector2(0.5f, 0.5f);
                float rad = (float)i / dotCount * Mathf.PI * 2f;
                dotRect.anchoredPosition = new Vector2(Mathf.Cos(rad) * 24f, Mathf.Sin(rad) * 13f);
                dotRect.sizeDelta = new Vector2(5f, 5f);
                var dotImg = dotRect.gameObject.AddComponent<Image>();
                dotImg.sprite = softDotSprite;
                dotImg.color = new Color(1f, 0.95f, 0.85f, 0.35f);
                teamOrbitDots.Add(dotRect);
            }

            // The Orbiting Dove Sprite (flying gracefully in elliptical orbit)
            var birdGO = CreateRect("Loader_OrbitingDove", orbitRoot);
            birdGO.anchorMin = birdGO.anchorMax = new Vector2(0.5f, 0.5f);
            birdGO.pivot = new Vector2(0.5f, 0.5f);
            birdGO.sizeDelta = new Vector2(28f, 28f);
            teamBirdLoaderRect = birdGO;
            teamBirdLoaderImg = birdGO.gameObject.AddComponent<Image>();
            teamBirdLoaderImg.sprite = doveSilhouetteSprite ?? flowerBloomSprite;
            teamBirdLoaderImg.preserveAspect = true;
            teamBirdLoaderImg.color = new Color(1.0f, 0.98f, 0.90f, 0.95f);

            var loadTxtGO = CreateRect("Loader_Text", loaderCorner);
            loadTxtGO.anchorMin = loadTxtGO.anchorMax = new Vector2(1.0f, 0.5f);
            loadTxtGO.pivot = new Vector2(1.0f, 0.5f);
            loadTxtGO.anchoredPosition = new Vector2(-70f, 0f);
            loadTxtGO.sizeDelta = new Vector2(130f, 26f);
            var loadTxt = loadTxtGO.gameObject.AddComponent<Text>();
            loadTxt.font = font;
            loadTxt.fontSize = 13;
            loadTxt.alignment = TextAnchor.MiddleRight;
            loadTxt.color = new Color(0.95f, 0.92f, 0.85f, 0.85f);
            loadTxt.text = "ĐANG TẢI...";

            // Headphone Recommendation at Bottom-Center
            var hpGO = CreateRect("Headphone_Recommendation", panelRect);
            hpGO.anchorMin = hpGO.anchorMax = new Vector2(0.5f, 0.0f);
            hpGO.pivot = new Vector2(0.5f, 0.0f);
            hpGO.anchoredPosition = new Vector2(0f, 48f);
            hpGO.sizeDelta = new Vector2(720f, 36f);
            var hpTxt = hpGO.gameObject.AddComponent<Text>();
            hpTxt.font = font;
            hpTxt.fontSize = 13;
            hpTxt.alignment = TextAnchor.MiddleCenter;
            hpTxt.color = new Color(0.92f, 0.88f, 0.75f, 0.75f);
            hpTxt.text = "🎧   Trải nghiệm âm thanh và không gian sẽ sống động hơn khi đeo tai nghe.";
        }

        private void BuildMainMenuPanel(Transform parent, Font font)
        {
            var panelRect = CreateRect("Panel_StateC_OpenStartMenu", parent);
            StretchFull(panelRect);
            startMenuGroup = panelRect.gameObject.AddComponent<CanvasGroup>();
            startMenuGroup.alpha = 0f;

            // Top-Left Anchored Cluster with safe margins (never clips)
            var cluster = CreateRect("Title_And_Menu_Cluster", panelRect);
            cluster.anchorMin = new Vector2(0f, 1f);
            cluster.anchorMax = new Vector2(0f, 1f);
            cluster.pivot = new Vector2(0f, 1f);
            cluster.anchoredPosition = new Vector2(120f, -65f);
            cluster.sizeDelta = new Vector2(520f, 700f);
            menuClusterRect = cluster;

            // NOTE: "FIVE COWS IN THE FIRE" text above the logo on Start Menu is REMOVED per user request!

            // 1. Official Calligraphy Logo (Enlarged by ~28% per user request: "làm to hơn một tí")
            if (gameOfficialLogoSprite != null)
            {
                var logoGO = CreateRect("Official_Calligraphy_Logo", cluster);
                logoGO.anchorMin = logoGO.anchorMax = new Vector2(0f, 1f);
                logoGO.pivot = new Vector2(0f, 1f);
                logoGO.anchoredPosition = new Vector2(0f, 0f);
                logoGO.sizeDelta = new Vector2(460f, 255f);
                mainLogoImg = logoGO.gameObject.AddComponent<Image>();
                mainLogoImg.sprite = gameOfficialLogoSprite;
                mainLogoImg.preserveAspect = true;
                mainLogoImg.color = Color.white;
            }
            else
            {
                var titleGO = CreateRect("MainTitle_Text", cluster);
                titleGO.anchorMin = titleGO.anchorMax = new Vector2(0f, 1f);
                titleGO.pivot = new Vector2(0f, 1f);
                titleGO.anchoredPosition = new Vector2(0f, 0f);
                titleGO.sizeDelta = new Vector2(480f, 70f);
                var tTxt = titleGO.gameObject.AddComponent<Text>();
                tTxt.font = font;
                tTxt.fontSize = 40;
                tTxt.fontStyle = FontStyle.Bold;
                tTxt.alignment = TextAnchor.MiddleLeft;
                tTxt.color = Color.white;
                tTxt.text = "H O A   T R Ê N   Đ Ấ T   Đ Ỏ";
            }

            // 2. Thin Horizon Divider
            var divGO = CreateRect("Horizon_Divider", cluster);
            divGO.anchorMin = divGO.anchorMax = new Vector2(0f, 1f);
            divGO.pivot = new Vector2(0f, 1f);
            divGO.anchoredPosition = new Vector2(0f, -270f);
            divGO.sizeDelta = new Vector2(320f, 2f);
            var divImg = divGO.gameObject.AddComponent<Image>();
            divImg.color = new Color(1.0f, 0.95f, 0.80f, 0.60f);

            // 3. English Subtitle (FLOWER ON RED SOIL)
            var subGO = CreateRect("Subtitle_Text", cluster);
            subGO.anchorMin = subGO.anchorMax = new Vector2(0f, 1f);
            subGO.pivot = new Vector2(0f, 1f);
            subGO.anchoredPosition = new Vector2(0f, -285f);
            subGO.sizeDelta = new Vector2(420f, 26f);
            var subTxt = subGO.gameObject.AddComponent<Text>();
            subTxt.font = font;
            subTxt.fontSize = 13;
            subTxt.alignment = TextAnchor.MiddleLeft;
            subTxt.color = new Color(0.98f, 0.92f, 0.75f, 0.92f);
            subTxt.text = "• • •   F L O W E R   O N   R E D   S O I L   • • •";

            // 4. Menu Buttons (BẮT ĐẦU, TIẾP TỤC, TÙY CHỈNH, THOÁT)
            var btnContainer = CreateRect("MainButtons_Container", cluster);
            btnContainer.anchorMin = btnContainer.anchorMax = new Vector2(0f, 1f);
            btnContainer.pivot = new Vector2(0f, 1f);
            btnContainer.anchoredPosition = new Vector2(0f, -340f);
            btnContainer.sizeDelta = new Vector2(360f, 260f);

            string[] labels = { "BẮT ĐẦU", "TIẾP TỤC", "TÙY CHỈNH", "THOÁT" };
            menuButtons.Clear();

            for (int i = 0; i < labels.Length; i++)
            {
                int idx = i;
                var btnRect = CreateRect($"Btn_{i}_{labels[i]}", btnContainer);
                btnRect.anchorMin = btnRect.anchorMax = new Vector2(0f, 1f);
                btnRect.pivot = new Vector2(0f, 1f);
                btnRect.anchoredPosition = new Vector2(0f, -i * 54f);
                btnRect.sizeDelta = new Vector2(320f, 44f);

                var btn = btnRect.gameObject.AddComponent<Button>();
                var colors = btn.colors;
                colors.normalColor = Color.clear;
                colors.highlightedColor = Color.clear;
                colors.pressedColor = Color.clear;
                btn.colors = colors;

                var txtRect = CreateRect("Label", btnRect);
                txtRect.anchorMin = new Vector2(0f, 0f);
                txtRect.anchorMax = new Vector2(1f, 1f);
                txtRect.offsetMin = new Vector2(24f, 0f);
                txtRect.offsetMax = Vector2.zero;
                var txt = txtRect.gameObject.AddComponent<Text>();
                txt.font = font;
                txt.fontSize = 18;
                txt.alignment = TextAnchor.MiddleLeft;
                txt.color = new Color(0.92f, 0.95f, 0.98f, 0.75f);
                txt.text = labels[i];

                var lDots = CreateRect("LeftDot", btnRect);
                lDots.anchorMin = new Vector2(0f, 0.5f);
                lDots.anchorMax = new Vector2(0f, 0.5f);
                lDots.pivot = new Vector2(0.5f, 0.5f);
                lDots.anchoredPosition = new Vector2(8f, 0f);
                lDots.sizeDelta = new Vector2(16f, 16f);
                var lTxt = lDots.gameObject.AddComponent<Text>();
                lTxt.font = font;
                lTxt.fontSize = 14;
                lTxt.alignment = TextAnchor.MiddleCenter;
                lTxt.color = Color.clear;
                lTxt.text = "•";

                btn.onClick.AddListener(() => TriggerMenuSelection(idx));

                var item = new MenuButtonItem
                {
                    RootRect = btnRect,
                    ButtonComp = btn,
                    LabelText = txt,
                    LeftDots = lTxt,
                    RightDots = null,
                    Title = labels[i],
                    HoverBlend = 0f,
                    IsInteractable = true
                };

                if (i == 1) continueButtonItem = item;
                menuButtons.Add(item);
            }
        }

        private void BuildSaveSlotModal(Transform parent, Font font)
        {
            var modalRect = CreateRect("Modal_SaveSlot_Selection", parent);
            StretchFull(modalRect);
            saveModalGroup = modalRect.gameObject.AddComponent<CanvasGroup>();
            saveModalGroup.alpha = 0f;
            modalRect.gameObject.SetActive(false);

            var bgImg = modalRect.gameObject.AddComponent<Image>();
            bgImg.color = new Color(0.02f, 0.03f, 0.06f, 0.88f);

            var centerBox = CreateRect("Center_Dialog_Box", modalRect);
            centerBox.anchorMin = centerBox.anchorMax = new Vector2(0.5f, 0.5f);
            centerBox.pivot = new Vector2(0.5f, 0.5f);
            centerBox.sizeDelta = new Vector2(820f, 580f);

            var titleRect = CreateRect("SaveModal_Title", centerBox);
            titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 0.93f);
            titleRect.sizeDelta = new Vector2(740f, 40f);
            saveModalTitleText = titleRect.gameObject.AddComponent<Text>();
            saveModalTitleText.font = font;
            saveModalTitleText.fontSize = 20;
            saveModalTitleText.fontStyle = FontStyle.Bold;
            saveModalTitleText.alignment = TextAnchor.MiddleCenter;
            saveModalTitleText.color = new Color(1.0f, 0.98f, 0.90f, 0.95f);
            saveModalTitleText.text = "C H Ọ N   V Ị   T R Í   L Ư U";

            // Notice text for empty slots / warnings
            var noticeRect = CreateRect("SaveModal_Notice", centerBox);
            noticeRect.anchorMin = noticeRect.anchorMax = new Vector2(0.5f, 0.85f);
            noticeRect.sizeDelta = new Vector2(700f, 30f);
            saveModalNoticeText = noticeRect.gameObject.AddComponent<Text>();
            saveModalNoticeText.font = font;
            saveModalNoticeText.fontSize = 13;
            saveModalNoticeText.alignment = TextAnchor.MiddleCenter;
            saveModalNoticeText.color = new Color(0.98f, 0.45f, 0.35f, 0.95f);
            saveModalNoticeText.text = "";

            // 3 Save Slot Cards
            slotUIItems.Clear();
            for (int i = 0; i < 3; i++)
            {
                int slot = i + 1;
                var cardRect = CreateRect($"Slot_Card_{slot}", centerBox);
                cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.68f - i * 0.22f);
                cardRect.sizeDelta = new Vector2(720f, 96f);

                var cardBg = cardRect.gameObject.AddComponent<Image>();
                cardBg.color = new Color(0.06f, 0.08f, 0.12f, 0.78f);

                var btn = cardRect.gameObject.AddComponent<Button>();
                btn.onClick.AddListener(() => OnSlotCardClicked(slot));

                var sTitleRect = CreateRect("SlotTitle", cardRect);
                sTitleRect.anchorMin = new Vector2(0.04f, 0.55f);
                sTitleRect.anchorMax = new Vector2(0.40f, 0.92f);
                var sTitle = sTitleRect.gameObject.AddComponent<Text>();
                sTitle.font = font;
                sTitle.fontSize = 17;
                sTitle.fontStyle = FontStyle.Bold;
                sTitle.alignment = TextAnchor.MiddleLeft;
                sTitle.color = new Color(0.99f, 0.96f, 0.88f, 0.98f);
                sTitle.text = $"VỊ TRÍ {slot}";

                var badgeRect = CreateRect("Badge", cardRect);
                badgeRect.anchorMin = new Vector2(0.60f, 0.55f);
                badgeRect.anchorMax = new Vector2(0.96f, 0.92f);
                var badge = badgeRect.gameObject.AddComponent<Text>();
                badge.font = font;
                badge.fontSize = 13;
                badge.alignment = TextAnchor.MiddleRight;
                badge.color = new Color(0.98f, 0.88f, 0.65f, 0.95f);
                badge.text = "[ TRỐNG ]";

                var detailsRect = CreateRect("Details", cardRect);
                detailsRect.anchorMin = new Vector2(0.04f, 0.08f);
                detailsRect.anchorMax = new Vector2(0.96f, 0.52f);
                var details = detailsRect.gameObject.AddComponent<Text>();
                details.font = font;
                details.fontSize = 13;
                details.alignment = TextAnchor.MiddleLeft;
                details.color = new Color(0.85f, 0.88f, 0.92f, 0.75f);
                details.text = "Chưa có dữ liệu";

                slotUIItems.Add(new SaveSlotUIItem
                {
                    RootRect = cardRect,
                    ButtonComp = btn,
                    SlotTitleText = sTitle,
                    SlotDetailsText = details,
                    StatusBadgeText = badge,
                    CardBg = cardBg,
                    SlotIndex = slot,
                    IsOccupied = false
                });
            }

            // Back Button
            var backBtnRect = CreateRect("Btn_Back_From_SaveModal", centerBox);
            backBtnRect.anchorMin = backBtnRect.anchorMax = new Vector2(0.5f, 0.06f);
            backBtnRect.sizeDelta = new Vector2(160f, 40f);
            var backBtn = backBtnRect.gameObject.AddComponent<Button>();
            var backImg = backBtnRect.gameObject.AddComponent<Image>();
            backImg.color = new Color(0.20f, 0.24f, 0.32f, 0.85f);
            var backTxtRect = CreateRect("Label", backBtnRect);
            StretchFull(backTxtRect);
            var backTxt = backTxtRect.gameObject.AddComponent<Text>();
            backTxt.font = font;
            backTxt.fontSize = 14;
            backTxt.alignment = TextAnchor.MiddleCenter;
            backTxt.color = Color.white;
            backTxt.text = "QUAY LẠI";
            backBtn.onClick.AddListener(CloseSaveModal);
        }

        private void BuildOptionsModal(Transform parent, Font font)
        {
            var modalRect = CreateRect("Modal_Options_Settings", parent);
            StretchFull(modalRect);
            optionsModalGroup = modalRect.gameObject.AddComponent<CanvasGroup>();
            optionsModalGroup.alpha = 0f;
            modalRect.gameObject.SetActive(false);

            var bgImg = modalRect.gameObject.AddComponent<Image>();
            bgImg.color = new Color(0.02f, 0.03f, 0.06f, 0.90f);

            var box = CreateRect("Center_Options_Box", modalRect);
            box.anchorMin = box.anchorMax = new Vector2(0.5f, 0.5f);
            box.pivot = new Vector2(0.5f, 0.5f);
            box.sizeDelta = new Vector2(800f, 620f);

            var boxBg = box.gameObject.AddComponent<Image>();
            boxBg.color = new Color(0.07f, 0.09f, 0.14f, 0.95f);

            // Title
            var titleRect = CreateRect("Title", box);
            titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 0.94f);
            titleRect.sizeDelta = new Vector2(700f, 40f);
            var title = titleRect.gameObject.AddComponent<Text>();
            title.font = font;
            title.fontSize = 22;
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = new Color(1.0f, 0.98f, 0.90f, 0.95f);
            title.text = "T Ù Y   C H Ỉ N H   H Ệ   T H Ố N G";

            // SECTION 1: ÂM THANH (AUDIO)
            float startY = -70f;
            CreateSectionHeader(box, "1. ÂM LƯỢNG (AUDIO)", new Vector2(40f, startY), font);

            startY -= 40f;
            masterVolSlider = CreateSliderRow(box, "Âm lượng Tổng", AudioListener.volume, new Vector2(40f, startY), font, out masterVolValText);
            masterVolSlider.onValueChanged.AddListener(OnMasterVolumeChanged);

            startY -= 42f;
            musicVolSlider = CreateSliderRow(box, "Âm lượng Nhạc nền", musicVolume, new Vector2(40f, startY), font, out musicVolValText);
            musicVolSlider.onValueChanged.AddListener(OnMusicVolumeChanged);

            startY -= 42f;
            sfxVolSlider = CreateSliderRow(box, "Hiệu ứng & Gió", sfxVolume, new Vector2(40f, startY), font, out sfxVolValText);
            sfxVolSlider.onValueChanged.AddListener(OnSfxVolumeChanged);

            // SECTION 2: ĐỒ HỌA & HIỂN THỊ (GRAPHICS)
            startY -= 52f;
            CreateSectionHeader(box, "2. ĐỒ HỌA & HIỂN THỊ (GRAPHICS)", new Vector2(40f, startY), font);

            startY -= 42f;
            var qualContainer = CreateRect("QualityButtons_Row", box);
            qualContainer.anchorMin = qualContainer.anchorMax = new Vector2(0f, 1f);
            qualContainer.pivot = new Vector2(0f, 1f);
            qualContainer.anchoredPosition = new Vector2(40f, startY);
            qualContainer.sizeDelta = new Vector2(720f, 36f);

            string[] qLabels = { "TIẾT KIỆM", "CHUẨN (URP)", "CAO", "SIÊU ĐẸP" };
            int curQuality = PlayerPrefs.GetInt("Settings_Quality", QualitySettings.GetQualityLevel());
            if (curQuality >= qLabels.Length) curQuality = qLabels.Length - 1;

            for (int q = 0; q < qLabels.Length; q++)
            {
                int qIdx = q;
                var qBtnRect = CreateRect($"Btn_Quality_{q}", qualContainer);
                qBtnRect.anchorMin = qBtnRect.anchorMax = new Vector2(0f, 1f);
                qBtnRect.pivot = new Vector2(0f, 1f);
                qBtnRect.anchoredPosition = new Vector2(q * 180f, 0f);
                qBtnRect.sizeDelta = new Vector2(170f, 34f);

                var qBtn = qBtnRect.gameObject.AddComponent<Button>();
                var qBg = qBtnRect.gameObject.AddComponent<Image>();
                qBg.color = (q == curQuality) ? new Color(0.85f, 0.42f, 0.18f, 0.90f) : new Color(0.12f, 0.15f, 0.22f, 0.85f);

                var qLblRect = CreateRect("Text", qBtnRect);
                StretchFull(qLblRect);
                var qLbl = qLblRect.gameObject.AddComponent<Text>();
                qLbl.font = font;
                qLbl.fontSize = 13;
                qLbl.alignment = TextAnchor.MiddleCenter;
                qLbl.color = Color.white;
                qLbl.text = qLabels[q];

                qualityButtons[q] = qBtn;
                qualityBtnTexts[q] = qLbl;
                qBtn.onClick.AddListener(() => SetQualityLevelUI(qIdx));
            }

            startY -= 44f;
            var fsBtnRect = CreateRect("Btn_Toggle_Fullscreen", box);
            fsBtnRect.anchorMin = fsBtnRect.anchorMax = new Vector2(0f, 1f);
            fsBtnRect.pivot = new Vector2(0f, 1f);
            fsBtnRect.anchoredPosition = new Vector2(40f, startY);
            fsBtnRect.sizeDelta = new Vector2(260f, 34f);

            var fsBtn = fsBtnRect.gameObject.AddComponent<Button>();
            var fsImg = fsBtnRect.gameObject.AddComponent<Image>();
            fsImg.color = new Color(0.18f, 0.22f, 0.32f, 0.85f);

            var fsTxtRect = CreateRect("Label", fsBtnRect);
            StretchFull(fsTxtRect);
            fullscreenBtnText = fsTxtRect.gameObject.AddComponent<Text>();
            fullscreenBtnText.font = font;
            fullscreenBtnText.fontSize = 13;
            fullscreenBtnText.alignment = TextAnchor.MiddleCenter;
            fullscreenBtnText.color = Color.white;
            fullscreenBtnText.text = Screen.fullScreen ? "TOÀN MÀN HÌNH: [ BẬT ]" : "TOÀN MÀN HÌNH: [ TẮT ]";
            fsBtn.onClick.AddListener(ToggleFullscreenMode);

            // SECTION 3: HƯỚNG DẪN ĐIỀU KHIỂN (CONTROLS GUIDE)
            startY -= 52f;
            CreateSectionHeader(box, "3. BẢNG PHÍM ĐIỀU KHIỂN (CONTROLS)", new Vector2(40f, startY), font);

            startY -= 34f;
            var ctrlCard = CreateRect("Controls_Card", box);
            ctrlCard.anchorMin = ctrlCard.anchorMax = new Vector2(0f, 1f);
            ctrlCard.pivot = new Vector2(0f, 1f);
            ctrlCard.anchoredPosition = new Vector2(40f, startY);
            ctrlCard.sizeDelta = new Vector2(720f, 68f);
            var ctrlBg = ctrlCard.gameObject.AddComponent<Image>();
            ctrlBg.color = new Color(0.04f, 0.05f, 0.08f, 0.65f);

            var ctrlTxtRect = CreateRect("Text", ctrlCard);
            StretchFull(ctrlTxtRect);
            ctrlTxtRect.offsetMin = new Vector2(16f, 8f);
            ctrlTxtRect.offsetMax = new Vector2(-16f, -8f);
            var ctrlTxt = ctrlTxtRect.gameObject.AddComponent<Text>();
            ctrlTxt.font = font;
            ctrlTxt.fontSize = 13;
            ctrlTxt.lineSpacing = 1.25f;
            ctrlTxt.alignment = TextAnchor.MiddleLeft;
            ctrlTxt.color = new Color(0.88f, 0.92f, 0.96f, 0.88f);
            ctrlTxt.text = "[A] / [D] : Di chuyển trái / phải   •   [Space] : Nhảy cao / Bám gờ đá\n[C] : Cúi người / Ẩn náu bụi lau   •   [Left Shift] : Chạy nước rút (Sprint)\n[E] : Tương tác / Ném sỏi đánh lạc hướng   •   [Esc] : Tạm dừng";

            // BACK BUTTON
            var backBtnRect = CreateRect("Btn_Back_Options", box);
            backBtnRect.anchorMin = backBtnRect.anchorMax = new Vector2(0.5f, 0.055f);
            backBtnRect.sizeDelta = new Vector2(180f, 42f);
            var backBtn = backBtnRect.gameObject.AddComponent<Button>();
            var backImg = backBtnRect.gameObject.AddComponent<Image>();
            backImg.color = new Color(0.85f, 0.42f, 0.18f, 0.90f);
            var backTxtRect = CreateRect("Label", backBtnRect);
            StretchFull(backTxtRect);
            var backTxt = backTxtRect.gameObject.AddComponent<Text>();
            backTxt.font = font;
            backTxt.fontSize = 14;
            backTxt.fontStyle = FontStyle.Bold;
            backTxt.alignment = TextAnchor.MiddleCenter;
            backTxt.color = Color.white;
            backTxt.text = "LƯU & QUAY LẠI";
            backBtn.onClick.AddListener(CloseOptionsDialog);
        }

        private static void CreateSectionHeader(RectTransform parent, string title, Vector2 pos, Font font)
        {
            var headerRect = CreateRect("Section_Header", parent);
            headerRect.anchorMin = headerRect.anchorMax = new Vector2(0f, 1f);
            headerRect.pivot = new Vector2(0f, 1f);
            headerRect.anchoredPosition = pos;
            headerRect.sizeDelta = new Vector2(720f, 26f);
            var t = headerRect.gameObject.AddComponent<Text>();
            t.font = font;
            t.fontSize = 14;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleLeft;
            t.color = new Color(0.98f, 0.88f, 0.65f, 0.95f);
            t.text = title;
        }

        private Slider CreateSliderRow(RectTransform parent, string label, float initVal, Vector2 pos, Font font, out Text valTxt)
        {
            var rowRect = CreateRect($"Row_{label}", parent);
            rowRect.anchorMin = rowRect.anchorMax = new Vector2(0f, 1f);
            rowRect.pivot = new Vector2(0f, 1f);
            rowRect.anchoredPosition = pos;
            rowRect.sizeDelta = new Vector2(720f, 32f);

            var lblRect = CreateRect("Label", rowRect);
            lblRect.anchorMin = new Vector2(0f, 0f);
            lblRect.anchorMax = new Vector2(0.28f, 1f);
            lblRect.offsetMin = Vector2.zero;
            lblRect.offsetMax = Vector2.zero;
            var l = lblRect.gameObject.AddComponent<Text>();
            l.font = font;
            l.fontSize = 13;
            l.alignment = TextAnchor.MiddleLeft;
            l.color = new Color(0.88f, 0.92f, 0.96f, 0.85f);
            l.text = label;

            // Slider Track Container (slim horizontal bar 8px high)
            var sliderRect = CreateRect("Slider", rowRect);
            sliderRect.anchorMin = new Vector2(0.30f, 0.5f);
            sliderRect.anchorMax = new Vector2(0.85f, 0.5f);
            sliderRect.pivot = new Vector2(0.5f, 0.5f);
            sliderRect.anchoredPosition = Vector2.zero;
            sliderRect.sizeDelta = new Vector2(0f, 8f);
            sliderRect.offsetMin = new Vector2(0f, -4f);
            sliderRect.offsetMax = new Vector2(0f, 4f);

            var bg = sliderRect.gameObject.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.15f, 0.22f, 0.95f);

            var fillArea = CreateRect("Fill Area", sliderRect);
            fillArea.anchorMin = Vector2.zero;
            fillArea.anchorMax = Vector2.one;
            fillArea.offsetMin = Vector2.zero;
            fillArea.offsetMax = Vector2.zero;

            var fill = CreateRect("Fill", fillArea);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(initVal, 1f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            var fillImg = fill.gameObject.AddComponent<Image>();
            fillImg.color = new Color(0.92f, 0.55f, 0.20f, 0.95f);

            var handleArea = CreateRect("Handle Slide Area", sliderRect);
            handleArea.anchorMin = Vector2.zero;
            handleArea.anchorMax = Vector2.one;
            handleArea.offsetMin = Vector2.zero;
            handleArea.offsetMax = Vector2.zero;

            var handle = CreateRect("Handle", handleArea);
            handle.anchorMin = new Vector2(initVal, 0.5f);
            handle.anchorMax = new Vector2(initVal, 0.5f);
            handle.pivot = new Vector2(0.5f, 0.5f);
            handle.sizeDelta = new Vector2(14f, 18f);
            handle.offsetMin = new Vector2(-7f, -9f);
            handle.offsetMax = new Vector2(7f, 9f);
            var handleImg = handle.gameObject.AddComponent<Image>();
            handleImg.color = new Color(0.99f, 0.96f, 0.90f, 1.0f);

            var slider = sliderRect.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImg;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = initVal;

            var valRect = CreateRect("ValueText", rowRect);
            valRect.anchorMin = new Vector2(0.88f, 0f);
            valRect.anchorMax = new Vector2(1.0f, 1f);
            valRect.offsetMin = Vector2.zero;
            valRect.offsetMax = Vector2.zero;
            valTxt = valRect.gameObject.AddComponent<Text>();
            valTxt.font = font;
            valTxt.fontSize = 13;
            valTxt.alignment = TextAnchor.MiddleRight;
            valTxt.color = new Color(0.98f, 0.95f, 0.88f, 0.90f);
            valTxt.text = $"{Mathf.RoundToInt(initVal * 100f)}%";

            return slider;
        }

        private void BuildOverwriteConfirmationDialog(Transform parent, Font font)
        {
            var dialogRect = CreateRect("Modal_Overwrite_Confirmation", parent);
            dialogRect.anchorMin = dialogRect.anchorMax = new Vector2(0.5f, 0.5f);
            dialogRect.pivot = new Vector2(0.5f, 0.5f);
            dialogRect.sizeDelta = new Vector2(560f, 260f);
            overwriteDialogGroup = dialogRect.gameObject.AddComponent<CanvasGroup>();
            overwriteDialogGroup.alpha = 0f;
            dialogRect.gameObject.SetActive(false);

            var bg = dialogRect.gameObject.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.06f, 0.10f, 0.96f);

            var warnTextRect = CreateRect("Warn_Text", dialogRect);
            warnTextRect.anchorMin = warnTextRect.anchorMax = new Vector2(0.5f, 0.68f);
            warnTextRect.sizeDelta = new Vector2(500f, 90f);
            overwriteDialogText = warnTextRect.gameObject.AddComponent<Text>();
            overwriteDialogText.font = font;
            overwriteDialogText.fontSize = 15;
            overwriteDialogText.alignment = TextAnchor.MiddleCenter;
            overwriteDialogText.color = new Color(0.98f, 0.92f, 0.85f, 0.95f);
            overwriteDialogText.text = "Vị trí lưu đã có tiến trình chơi.\nBắt đầu trò chơi mới sẽ ghi đè lên dữ liệu cũ.\nBạn có chắc chắn muốn tiếp tục?";

            var confirmBtnRect = CreateRect("Btn_Confirm_Overwrite", dialogRect);
            confirmBtnRect.anchorMin = confirmBtnRect.anchorMax = new Vector2(0.30f, 0.20f);
            confirmBtnRect.sizeDelta = new Vector2(150f, 40f);
            var cBtn = confirmBtnRect.gameObject.AddComponent<Button>();
            var cImg = confirmBtnRect.gameObject.AddComponent<Image>();
            cImg.color = new Color(0.85f, 0.28f, 0.28f, 0.88f);
            var cTxtRect = CreateRect("Label", confirmBtnRect);
            StretchFull(cTxtRect);
            var cTxt = cTxtRect.gameObject.AddComponent<Text>();
            cTxt.font = font;
            cTxt.fontSize = 14;
            cTxt.alignment = TextAnchor.MiddleCenter;
            cTxt.color = Color.white;
            cTxt.text = "GHI ĐÈ";
            cBtn.onClick.AddListener(ConfirmOverwriteSlot);

            var cancelBtnRect = CreateRect("Btn_Cancel_Overwrite", dialogRect);
            cancelBtnRect.anchorMin = cancelBtnRect.anchorMax = new Vector2(0.70f, 0.20f);
            cancelBtnRect.sizeDelta = new Vector2(150f, 40f);
            var canBtn = cancelBtnRect.gameObject.AddComponent<Button>();
            var canImg = cancelBtnRect.gameObject.AddComponent<Image>();
            canImg.color = new Color(0.24f, 0.28f, 0.38f, 0.88f);
            var canTxtRect = CreateRect("Label", cancelBtnRect);
            StretchFull(canTxtRect);
            var canTxt = canTxtRect.gameObject.AddComponent<Text>();
            canTxt.font = font;
            canTxt.fontSize = 14;
            canTxt.alignment = TextAnchor.MiddleCenter;
            canTxt.color = Color.white;
            canTxt.text = "HỦY";
            canBtn.onClick.AddListener(HideOverwriteConfirmation);
        }

        // =====================================================================================
        // ZELDA: BREATH OF THE WILD STYLE LOADING SCREEN
        // =====================================================================================
        private void BuildBotWLoadingPanel(Transform parent, Font font)
        {
            var panelRect = CreateRect("Panel_BotW_LoadingScreen", parent);
            StretchFull(panelRect);
            botwLoadingGroup = panelRect.gameObject.AddComponent<CanvasGroup>();
            botwLoadingGroup.alpha = 0f;
            panelRect.gameObject.SetActive(false);

            // Top Section (Authentic Cream/Parchment tone #FAF7ED)
            var topBgRect = CreateRect("Top_Cream_Section", panelRect);
            topBgRect.anchorMin = new Vector2(0f, 0.50f);
            topBgRect.anchorMax = new Vector2(1f, 1f);
            topBgRect.offsetMin = topBgRect.offsetMax = Vector2.zero;
            var topImg = topBgRect.gameObject.AddComponent<Image>();
            topImg.color = new Color(0.98f, 0.96f, 0.91f, 1.0f); // Warm parchment cream

            // Bottom Section (Deep Minimalist Black/Slate #0C0E12)
            var botBgRect = CreateRect("Bottom_Dark_Section", panelRect);
            botBgRect.anchorMin = new Vector2(0f, 0f);
            botBgRect.anchorMax = new Vector2(1f, 0.50f);
            botBgRect.offsetMin = botBgRect.offsetMax = Vector2.zero;
            var botImg = botBgRect.gameObject.AddComponent<Image>();
            botImg.color = new Color(0.05f, 0.06f, 0.08f, 1.0f);

            // Crisp Horizontal Horizon Dividing Line
            var lineRect = CreateRect("BotW_Horizon_Divider_Line", panelRect);
            lineRect.anchorMin = new Vector2(0f, 0.50f);
            lineRect.anchorMax = new Vector2(1f, 0.50f);
            lineRect.pivot = new Vector2(0.5f, 0.5f);
            lineRect.anchoredPosition = Vector2.zero;
            lineRect.sizeDelta = new Vector2(0f, 3f);
            var lineImg = lineRect.gameObject.AddComponent<Image>();
            lineImg.color = new Color(0.12f, 0.14f, 0.18f, 0.95f);

            // HORIZON SILHOUETTES TRACK
            var trackRect = CreateRect("Silhouette_Horizon_Track", panelRect);
            trackRect.anchorMin = new Vector2(0f, 0.50f);
            trackRect.anchorMax = new Vector2(1f, 0.50f);
            trackRect.pivot = new Vector2(0.5f, 0f);
            trackRect.anchoredPosition = Vector2.zero;
            trackRect.sizeDelta = new Vector2(0f, 140f);

            // 1. Hopping Silhouette of Cậu Bé An
            var anGO = CreateRect("Silhouette_An_Boy", trackRect);
            anGO.anchorMin = anGO.anchorMax = new Vector2(0.5f, 0f);
            anGO.pivot = new Vector2(0.5f, 0f);
            anGO.anchoredPosition = new Vector2(0f, 0f);
            anGO.sizeDelta = new Vector2(80f, 80f);
            botwAnRect = anGO;
            botwAnImg = anGO.gameObject.AddComponent<Image>();
            botwAnImg.sprite = boySilhouetteSprite;
            botwAnImg.preserveAspect = true;
            botwAnImg.color = new Color(0.08f, 0.09f, 0.12f, 1.0f); // Solid dark silhouette

            // 2. Sailing Silhouette of 1954 Transport Ship
            var shipGO = CreateRect("Silhouette_1954_Ship", trackRect);
            shipGO.anchorMin = shipGO.anchorMax = new Vector2(0.5f, 0f);
            shipGO.pivot = new Vector2(0.5f, 0.2f);
            shipGO.anchoredPosition = new Vector2(-280f, 0f);
            shipGO.sizeDelta = new Vector2(170f, 100f);
            botwShipRect = shipGO;
            botwShipImg = shipGO.gameObject.AddComponent<Image>();
            botwShipImg.sprite = shipSilhouetteSprite ?? flowerBloomSprite;
            botwShipImg.preserveAspect = true;
            botwShipImg.color = new Color(0.12f, 0.14f, 0.18f, 1.0f); // Solid dark ship silhouette

            // TIPS & TRICKS SECTION (BotW style, lower left area)
            var tipsContainer = CreateRect("Tips_And_Tricks_Cluster", panelRect);
            tipsContainer.anchorMin = new Vector2(0f, 0f);
            tipsContainer.anchorMax = new Vector2(0f, 0.50f);
            tipsContainer.pivot = new Vector2(0f, 1f);
            tipsContainer.anchoredPosition = new Vector2(100f, -40f);
            tipsContainer.sizeDelta = new Vector2(760f, 220f);

            var tipGroupGO = CreateRect("TipContentGroup", tipsContainer);
            StretchFull(tipGroupGO);
            botwTipTextGroup = tipGroupGO.gameObject.AddComponent<CanvasGroup>();

            // Category Title (e.g. "ẨN MÌNH TRONG LAU SẬY")
            var catRect = CreateRect("Tip_Category", tipGroupGO);
            catRect.anchorMin = catRect.anchorMax = new Vector2(0f, 1f);
            catRect.pivot = new Vector2(0f, 1f);
            catRect.anchoredPosition = new Vector2(0f, 0f);
            catRect.sizeDelta = new Vector2(760f, 34f);
            botwTipCategoryText = catRect.gameObject.AddComponent<Text>();
            botwTipCategoryText.font = font;
            botwTipCategoryText.fontSize = 20;
            botwTipCategoryText.fontStyle = FontStyle.Bold;
            botwTipCategoryText.alignment = TextAnchor.MiddleLeft;
            botwTipCategoryText.color = new Color(0.99f, 0.95f, 0.88f, 0.98f);
            botwTipCategoryText.text = "ẨN MÌNH TRONG LAU SẬY";

            // Body Tip Text
            var bodyRect = CreateRect("Tip_Body", tipGroupGO);
            bodyRect.anchorMin = bodyRect.anchorMax = new Vector2(0f, 1f);
            bodyRect.pivot = new Vector2(0f, 1f);
            bodyRect.anchoredPosition = new Vector2(0f, -40f);
            bodyRect.sizeDelta = new Vector2(740f, 90f);
            botwTipBodyText = bodyRect.gameObject.AddComponent<Text>();
            botwTipBodyText.font = font;
            botwTipBodyText.fontSize = 15;
            botwTipBodyText.lineSpacing = 1.35f;
            botwTipBodyText.alignment = TextAnchor.UpperLeft;
            botwTipBodyText.color = new Color(0.85f, 0.88f, 0.92f, 0.85f);
            botwTipBodyText.text = "Kẻ địch và lính tuần tra có tầm nhìn theo hướng quay mặt. Khi phát hiện nguy hiểm, hãy nhấn [C] để cúi mình ẩn náu trong các bụi cỏ lau rậm rạp.";

            // Prompt Hint (Next tip [Space / Enter])
            var hintRect = CreateRect("Tip_Hint", tipsContainer);
            hintRect.anchorMin = hintRect.anchorMax = new Vector2(0f, 0f);
            hintRect.pivot = new Vector2(0f, 0f);
            hintRect.anchoredPosition = new Vector2(0f, 20f);
            hintRect.sizeDelta = new Vector2(500f, 26f);
            var hintTxt = hintRect.gameObject.AddComponent<Text>();
            hintTxt.font = font;
            hintTxt.fontSize = 12;
            hintTxt.alignment = TextAnchor.MiddleLeft;
            hintTxt.color = new Color(0.70f, 0.74f, 0.82f, 0.60f);
            hintTxt.text = "Nhấn [Space] / [Enter] để đổi mẹo   •   Mẹo tự chuyển đổi sau 6s";

            // BOTTOM-RIGHT CORNER EMBLEM & SHEIKAH PULSING DOTS
            var brCluster = CreateRect("BotW_BottomRight_Cluster", panelRect);
            brCluster.anchorMin = new Vector2(1.0f, 0f);
            brCluster.anchorMax = new Vector2(1.0f, 0f);
            brCluster.pivot = new Vector2(1.0f, 0f);
            brCluster.anchoredPosition = new Vector2(-100f, 50f);
            brCluster.sizeDelta = new Vector2(280f, 120f);

            // Sheikah slate dots circle
            var dotsRect = CreateRect("Sheikah_Dots_Circle", brCluster);
            dotsRect.anchorMin = dotsRect.anchorMax = new Vector2(1.0f, 0.25f);
            dotsRect.pivot = new Vector2(0.5f, 0.5f);
            dotsRect.anchoredPosition = new Vector2(-25f, 0f);
            dotsRect.sizeDelta = new Vector2(36f, 36f);
            botwDotsClusterRect = dotsRect;

            botwSheikahDots.Clear();
            int sheikahCount = 6;
            for (int d = 0; d < sheikahCount; d++)
            {
                var sDotRect = CreateRect($"Dot_{d}", dotsRect);
                sDotRect.anchorMin = sDotRect.anchorMax = new Vector2(0.5f, 0.5f);
                sDotRect.pivot = new Vector2(0.5f, 0.5f);
                float rad = (float)d / sheikahCount * Mathf.PI * 2f;
                sDotRect.anchoredPosition = new Vector2(Mathf.Cos(rad) * 14f, Mathf.Sin(rad) * 14f);
                sDotRect.sizeDelta = new Vector2(4.5f, 4.5f);
                var sImg = sDotRect.gameObject.AddComponent<Image>();
                sImg.sprite = softDotSprite;
                sImg.color = new Color(0.99f, 0.95f, 0.88f, 0.85f);
                botwSheikahDots.Add(sImg);
            }

            var brTextRect = CreateRect("Loading_Text", brCluster);
            brTextRect.anchorMin = brTextRect.anchorMax = new Vector2(1.0f, 0.25f);
            brTextRect.pivot = new Vector2(1.0f, 0.5f);
            brTextRect.anchoredPosition = new Vector2(-55f, 0f);
            brTextRect.sizeDelta = new Vector2(220f, 26f);
            var brTxt = brTextRect.gameObject.AddComponent<Text>();
            brTxt.font = font;
            brTxt.fontSize = 13;
            brTxt.alignment = TextAnchor.MiddleRight;
            brTxt.color = new Color(0.92f, 0.88f, 0.78f, 0.80f);
            brTxt.text = "ĐANG TẢI VÙNG ĐẤT ĐỎ...";
        }

        private void BuildAutosaveWidget(Transform parent, Font font)
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

            var iconRect = CreateRect("Autosave_Icon", autoRoot);
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(1.0f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(-16f, 0f);
            iconRect.sizeDelta = new Vector2(28f, 28f);
            autosaveSpinnerRect = iconRect;

            var iconImg = iconRect.gameObject.AddComponent<Image>();
            iconImg.sprite = doveSilhouetteSprite ?? flowerBloomSprite;
            iconImg.preserveAspect = true;
            iconImg.color = new Color(1.0f, 0.98f, 0.90f, 0.95f);

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
            if (es.GetComponent<InputSystemUIInputModule>() == null)
            {
                es.gameObject.AddComponent<InputSystemUIInputModule>();
            }
#else
            if (es.GetComponent<StandaloneInputModule>() == null)
            {
                es.gameObject.AddComponent<StandaloneInputModule>();
            }
#endif
        }

        // =====================================================================================
        // INPUT ABSTRACTION (Input System package & Legacy fallback)
        // =====================================================================================
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
            if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)) return true;
#endif
            try
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space)) return true;
            }
            catch {}
            return false;
        }

        private static bool IsCancelPressed()
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

        private static bool IsAnyKeyPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.anyKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame)) return true;
            var mouse = Mouse.current;
            if (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame || mouse.middleButton.wasPressedThisFrame)) return true;
#endif
            try
            {
                if (Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Escape)) return true;
            }
            catch {}
            return false;
        }
    }
}
