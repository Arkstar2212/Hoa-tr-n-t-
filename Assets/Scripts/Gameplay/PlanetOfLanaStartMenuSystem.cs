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
        [Header("Save Slot Fallback Thumbnails (Concept Art)")]
        [SerializeField] private Texture2D[] fallbackSlotThumbnails;

        [Header("Audio (Gentle BGM, Wind & UI Chimes)")]
        [SerializeField] private AudioClip mainMenuMusicClip;
        [SerializeField] private AudioClip[] randomMenuMusicClips;
        [SerializeField] private AudioClip ambientWindClip;
        [SerializeField] private AudioClip hoverSfxClip;
        [SerializeField] private AudioClip confirmSfxClip;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.65f;
        [SerializeField, Range(0f, 1f)] private float windVolume = 0.22f;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.45f;
        [SerializeField] private float musicSmoothInDuration = 3.5f;
        [SerializeField] private float autoCrossfadeLeadDuration = 4.0f;
        [SerializeField] private float musicCrossfadeDuration = 3.0f;

        [Header("Scenic Camera Settings")]
        [SerializeField] private Vector3 menuCameraPosition = new Vector3(0f, 0f, -10.0f);
        [SerializeField] private Vector3 menuCameraEuler = Vector3.zero;
        [SerializeField] private float menuCameraFOV = 35.5f;

        private MenuPhase currentPhase = MenuPhase.InitialStartup;
        private float phaseTimer = 0f;
        private float currentMusicFadeVol = 0f;
        private bool isMusicPlaying = false;

        // Dual Music Sources for True Seamless DJ Crossfading & Shuffle
        private AudioSource musicSourceA;
        private AudioSource musicSourceB;
        private bool isUsingSourceA = true;
        private Coroutine activeCrossfadeCoroutine;
        private bool isCrossfading = false;
        private readonly List<AudioClip> activePlaylist = new List<AudioClip>();
        private int currentTrackIndex = -1;

        private AudioSource windSource;
        private AudioSource sfxSource;

        private AudioSource ActiveMusicSource => isUsingSourceA ? musicSourceA : musicSourceB;
        private AudioSource InactiveMusicSource => isUsingSourceA ? musicSourceB : musicSourceA;

        // Now Playing Toast Widget
        private CanvasGroup toastGroup;
        private Text toastText;
        private float toastTimer = 0f;

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
        private Text fullscreenBtnText;
        private Text vsyncBtnText;
        private GameAction? rebindingAction = null;
        private readonly Dictionary<GameAction, Text> keybindButtonTexts = new Dictionary<GameAction, Text>();

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
            public Image SelectionBox;
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
            public RectTransform CardFrameRect;
            public Button ButtonComp;
            public Text HeaderText;             // "SLOT 1", "SLOT 2", "SLOT 3"
            public Image CardBorderImg;         // White outline border when selected
            public Image CardInnerBg;           // Dark container bg
            public RawImage ThumbnailImg;       // Screenshot thumbnail texture or fallback concept art
            public GameObject EmptyContainer;   // Dark navy panel for empty slot
            public Text EmptyLabelText;         // "NEW GAME"
            public Text TimestampText;          // e.g. "10/26/2026 10:08:03 AM"
            public Text PlaytimeText;           // e.g. "PLAYTIME 09:03:44"
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
            if (mainCam != null)
            {
                // Ensure no leftover CinemachineBrain in the Start Menu scene can override mainCam!
                var brain = mainCam.GetComponent("CinemachineBrain");
                if (brain != null) Destroy(brain);
                mainCam.transform.position = menuCameraPosition;
                mainCam.transform.eulerAngles = menuCameraEuler;
                mainCam.fieldOfView = menuCameraFOV;
            }
            EnsureEventSystem();
            RebuildUIImmediate();

            // Set initial state: Pure Black Screen (State A)
            SetPhase(MenuPhase.InitialStartup);
        }

        private void SetupAudioSources()
        {
            var sources = GetComponents<AudioSource>();

            // Assign or create exactly 4 AudioSources:
            // 0: musicSourceA (DJ deck 1)
            // 1: musicSourceB (DJ deck 2)
            // 2: windSource (ambient wind loop)
            // 3: sfxSource (UI clicks & chimes)
            musicSourceA = sources.Length > 0 ? sources[0] : gameObject.AddComponent<AudioSource>();
            musicSourceB = sources.Length > 1 ? sources[1] : gameObject.AddComponent<AudioSource>();
            windSource   = sources.Length > 2 ? sources[2] : gameObject.AddComponent<AudioSource>();
            sfxSource    = sources.Length > 3 ? sources[3] : gameObject.AddComponent<AudioSource>();

            // Clean up any extra redundant AudioSources if count > 4 to prevent clutter
            if (sources.Length > 4)
            {
                for (int i = 4; i < sources.Length; i++)
                {
                    if (Application.isPlaying) Destroy(sources[i]);
                    else DestroyImmediate(sources[i]);
                }
            }

            ConfigureAudioSource(musicSourceA, loop: false, volume: 0f);
            ConfigureAudioSource(musicSourceB, loop: false, volume: 0f);
            ConfigureAudioSource(windSource, loop: true, volume: 0f);
            ConfigureAudioSource(sfxSource, loop: false, volume: sfxVolume);

            if (ambientWindClip != null) windSource.clip = ambientWindClip;

            BuildActivePlaylist();
            PrepareNextTrackImmediate(isFirstStartup: true);
        }

        private void ConfigureAudioSource(AudioSource src, bool loop, float volume)
        {
            if (src == null) return;
            src.loop = loop;
            src.playOnAwake = false;
            src.volume = volume;
        }

        public void BuildActivePlaylist()
        {
            activePlaylist.Clear();

            // 1. Add explicitly serialized randomMenuMusicClips
            if (randomMenuMusicClips != null && randomMenuMusicClips.Length > 0)
            {
                for (int i = 0; i < randomMenuMusicClips.Length; i++)
                {
                    var clip = randomMenuMusicClips[i];
                    if (clip != null && !activePlaylist.Contains(clip))
                    {
                        activePlaylist.Add(clip);
                    }
                }
            }

#if UNITY_EDITOR
            // 2. Editor & Play Mode Guarantee: Automatically discover official tracks if missing
            if (activePlaylist.Count < 2)
            {
                string[] searchPaths = new[]
                {
                    "Assets/Audio/Phat_Hien_Binh_Yen.mp3",
                    "Assets/Audio/Pastoral_Alien.mp3",
                    "Assets/Audio/Breath of the World.mp3",
                    "Assets/Audio/Haru Haru (Piano Cover).mp3"
                };
                foreach (var path in searchPaths)
                {
                    var c = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                    if (c != null && !activePlaylist.Contains(c))
                    {
                        activePlaylist.Add(c);
                    }
                }
                if (randomMenuMusicClips == null || randomMenuMusicClips.Length == 0)
                {
                    randomMenuMusicClips = activePlaylist.ToArray();
                }
            }
#endif

            // Fallback to mainMenuMusicClip if playlist still empty
            if (activePlaylist.Count == 0 && mainMenuMusicClip != null)
            {
                activePlaylist.Add(mainMenuMusicClip);
            }
        }

        public void SetRandomMenuMusicClips(AudioClip[] clips)
        {
            randomMenuMusicClips = clips;
            BuildActivePlaylist();
            PrepareNextTrackImmediate(isFirstStartup: true);
        }

        private AudioClip GetNextRandomClip(out int newIndex, bool isInitial = false)
        {
            if (activePlaylist.Count == 0) BuildActivePlaylist();

            if (activePlaylist.Count == 0)
            {
                newIndex = -1;
                return mainMenuMusicClip;
            }

            if (activePlaylist.Count == 1)
            {
                newIndex = 0;
                return activePlaylist[0];
            }

            // Shuffle rule: Must NEVER play the same track back-to-back!
            int nextIdx;
            int attempts = 0;
            do
            {
                nextIdx = UnityEngine.Random.Range(0, activePlaylist.Count);
                attempts++;
            } while (nextIdx == currentTrackIndex && attempts < 30);

            if (nextIdx == currentTrackIndex)
            {
                nextIdx = (currentTrackIndex + 1) % activePlaylist.Count;
            }

            newIndex = nextIdx;
            return activePlaylist[nextIdx];
        }

        private void PrepareNextTrackImmediate(bool isFirstStartup = false)
        {
            AudioClip clip = GetNextRandomClip(out currentTrackIndex, isInitial: isFirstStartup);
            if (clip != null)
            {
                mainMenuMusicClip = clip;
                var src = ActiveMusicSource;
                if (src != null)
                {
                    src.clip = clip;
                    src.time = 0f;
                    src.volume = 0f;
                    src.loop = false;
                }
            }
        }

        public void StartPlayingMenuMusic()
        {
            if (isMusicPlaying) return;

            var activeSrc = ActiveMusicSource;
            if (activeSrc == null) return;

            if (activeSrc.clip == null)
            {
                PrepareNextTrackImmediate(isFirstStartup: true);
            }

            if (activeSrc.clip != null)
            {
                activeSrc.volume = 0f;
                activeSrc.time = 0f;
                activeSrc.loop = false;
                activeSrc.Play();
                isMusicPlaying = true;
                currentMusicFadeVol = 0f;

                ShowNowPlayingToast(GetTrackDisplayName(activeSrc.clip));
                Debug.Log($"<color=#38bdf8>[StartMenu Audio]</color> <b>Playing Initial Random Track:</b> {activeSrc.clip.name} ({activeSrc.clip.length:F1}s)");
            }
        }

        public void CrossfadeToNextRandomTrack(float duration = -1f)
        {
            if (duration <= 0f) duration = musicCrossfadeDuration;

            if (!gameObject.activeInHierarchy || !enabled)
            {
                var nextClip = GetNextRandomClip(out currentTrackIndex);
                var activeSrc = ActiveMusicSource;
                if (activeSrc != null && nextClip != null)
                {
                    activeSrc.clip = nextClip;
                    activeSrc.Play();
                    activeSrc.volume = musicVolume;
                    mainMenuMusicClip = nextClip;
                }
                return;
            }

            if (activeCrossfadeCoroutine != null)
            {
                StopCoroutine(activeCrossfadeCoroutine);
            }
            activeCrossfadeCoroutine = StartCoroutine(CrossfadeRoutine(duration));
        }

        private IEnumerator CrossfadeRoutine(float duration)
        {
            isCrossfading = true;

            var fromSource = ActiveMusicSource;
            var toSource = InactiveMusicSource;

            AudioClip nextClip = GetNextRandomClip(out currentTrackIndex);
            if (nextClip == null || toSource == null)
            {
                isCrossfading = false;
                activeCrossfadeCoroutine = null;
                yield break;
            }

            mainMenuMusicClip = nextClip;
            toSource.clip = nextClip;
            toSource.time = 0f;
            toSource.volume = 0f;
            toSource.loop = false;
            toSource.Play();

            ShowNowPlayingToast(GetTrackDisplayName(nextClip));
            Debug.Log($"<color=#10b981>[StartMenu Audio DJ Mix]</color> <b>Crossfading To Next Random Track:</b> {nextClip.name} ({nextClip.length:F1}s) over {duration:F1}s");

            float timer = 0f;
            float fromStartVol = fromSource != null ? fromSource.volume : 0f;

            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(timer / duration);

                if (fromSource != null && fromSource.isPlaying)
                {
                    fromSource.volume = Mathf.Lerp(fromStartVol, 0f, t);
                }

                if (toSource != null)
                {
                    toSource.volume = Mathf.Lerp(0f, musicVolume, Mathf.SmoothStep(0f, 1f, t));
                }

                yield return null;
            }

            if (fromSource != null)
            {
                fromSource.Stop();
                fromSource.volume = 0f;
            }

            if (toSource != null)
            {
                toSource.volume = musicVolume;
            }

            // Swap active source
            isUsingSourceA = !isUsingSourceA;
            isCrossfading = false;
            activeCrossfadeCoroutine = null;
        }

        private void StopAllMenuMusicImmediate()
        {
            if (activeCrossfadeCoroutine != null)
            {
                StopCoroutine(activeCrossfadeCoroutine);
                activeCrossfadeCoroutine = null;
            }
            isCrossfading = false;

            if (musicSourceA != null)
            {
                musicSourceA.Stop();
                musicSourceA.volume = 0f;
            }
            if (musicSourceB != null)
            {
                musicSourceB.Stop();
                musicSourceB.volume = 0f;
            }
            isMusicPlaying = false;
            currentMusicFadeVol = 0f;
        }

        private void ApplyMusicVolumeToSources()
        {
            if (musicSourceA != null && musicSourceA.isPlaying)
            {
                musicSourceA.volume = isCrossfading ? musicSourceA.volume : (isUsingSourceA ? musicVolume * currentMusicFadeVol : 0f);
            }
            if (musicSourceB != null && musicSourceB.isPlaying)
            {
                musicSourceB.volume = isCrossfading ? musicSourceB.volume : (!isUsingSourceA ? musicVolume * currentMusicFadeVol : 0f);
            }
        }

        public void ShowNowPlayingToast(string title)
        {
            if (toastText != null)
            {
                toastText.text = $"♪  {title}   <size=11><color=#a3a3a3>[N: Đổi bài]</color></size>";
            }
            toastTimer = 5.0f;
        }

        private string GetTrackDisplayName(AudioClip clip)
        {
            if (clip == null) return "";
            string n = clip.name;
            if (n.Contains("Phat_Hien_Binh_Yen") || n.Contains("Phát Hiện Bình Yên")) return "Phát Hiện Bình Yên";
            if (n.Contains("Pastoral_Alien") || n.Contains("Pastoral Alien")) return "Pastoral Alien";
            if (n.Contains("Breath of the World")) return "Breath of the World";
            if (n.Contains("Haru Haru")) return "Haru Haru (Piano Cover)";
            return n.Replace('_', ' ');
        }

        private bool IsNextTrackHotkeyPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.nKey.wasPressedThisFrame || Keyboard.current.mKey.wasPressedThisFrame) return true;
            }
            if (Gamepad.current != null)
            {
                if (Gamepad.current.rightShoulder.wasPressedThisFrame) return true;
            }
#else
            if (Input.GetKeyDown(KeyCode.N) || Input.GetKeyDown(KeyCode.M)) return true;
#endif
            return false;
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
            ConfigureAssets(studioSprite, officialGameLogo, flowerSprite, doveSprite, dotSprite, null, null, bgmClip, windClip, gameplayScene, headphoneSpr, null);
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
            Sprite headphoneSpr = null,
            AudioClip[] playlistClips = null)
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
            if (playlistClips != null && playlistClips.Length > 0) randomMenuMusicClips = playlistClips;
            if (!string.IsNullOrEmpty(gameplayScene)) targetGameplaySceneName = gameplayScene;

            SetupAudioSources();
        }

        public void SetMenuCameraSettings(Vector3 pos, Vector3 euler, float fov)
        {
            menuCameraPosition = pos;
            menuCameraEuler = euler;
            menuCameraFOV = fov;
        }

        public void SetFallbackSlotThumbnails(Texture2D[] thumbnails)
        {
            fallbackSlotThumbnails = thumbnails;
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
            SetPreviewState(phase, 1.0f);
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
            else if (phase == MenuPhase.SaveSlotSelection && saveModalGroup != null)
            {
                saveModalGroup.alpha = alpha;
                if (blackCurtainGroup != null) blackCurtainGroup.alpha = 0f;
                if (autosaveWidgetGroup != null) autosaveWidgetGroup.alpha = 0f;
                PopulateSaveSlotCards();
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

                    StopAllMenuMusicImmediate();
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

                    StopAllMenuMusicImmediate();
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

                    StopAllMenuMusicImmediate();
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

                    StartPlayingMenuMusic();
                    if (windSource != null && !windSource.isPlaying && ambientWindClip != null)
                    {
                        windSource.volume = windVolume * 0.7f;
                        windSource.Play();
                    }
                    break;

                case MenuPhase.SaveSlotSelection:
                    if (blackCurtainGroup != null)
                    {
                        blackCurtainGroup.alpha = 0f;
                        blackCurtainGroup.blocksRaycasts = false;
                    }
                    if (saveModalGroup != null)
                    {
                        saveModalGroup.gameObject.SetActive(true);
                        saveModalGroup.alpha = 1f;
                        saveModalGroup.interactable = true;
                        saveModalGroup.blocksRaycasts = true;
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
            UpdateMusicToast(dt);
            UpdateAutosaveWidget(dt);
            UpdateScenicCamera(dt);
        }

        private void LateUpdate()
        {
            UpdateScenicCamera(Time.unscaledDeltaTime);
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

            if (IsNavRightPressed() || IsNavDownPressed())
            {
                selectedSlotIndex = (selectedSlotIndex + 1) % slotUIItems.Count;
                PlayHover();
                HighlightSelectedSlot();
            }
            else if (IsNavLeftPressed() || IsNavUpPressed())
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
            if (rebindingAction.HasValue)
            {
                if (IsCancelPressed())
                {
                    rebindingAction = null;
                    RefreshKeybindButtonLabels();
                }
                return;
            }

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

            if (musicSourceA != null) musicSourceA.volume = Mathf.MoveTowards(musicSourceA.volume, 0f, dt * (musicVolume / 1.2f));
            if (musicSourceB != null) musicSourceB.volume = Mathf.MoveTowards(musicSourceB.volume, 0f, dt * (musicVolume / 1.2f));
        }

        private void UpdateAudioLifecycle(float dt)
        {
            if (currentPhase == MenuPhase.StartMenuOpenVista || currentPhase == MenuPhase.SaveSlotSelection || currentPhase == MenuPhase.Options)
            {
                if (isMusicPlaying)
                {
                    var currentSource = ActiveMusicSource;
                    if (currentSource != null && currentSource.clip != null)
                    {
                        // Auto-advance check: when nearing end of track or if playback stalled/ended
                        if (!isCrossfading)
                        {
                            if (currentSource.isPlaying)
                            {
                                float remaining = currentSource.clip.length - currentSource.time;
                                if (remaining <= autoCrossfadeLeadDuration && currentSource.time > 10.0f)
                                {
                                    CrossfadeToNextRandomTrack(musicCrossfadeDuration);
                                }
                            }
                            else if (currentSource.time >= currentSource.clip.length - 0.5f || currentSource.time == 0f)
                            {
                                CrossfadeToNextRandomTrack(1.5f);
                            }
                        }

                        // Smooth initial volume fade in
                        if (!isCrossfading)
                        {
                            currentMusicFadeVol = Mathf.MoveTowards(currentMusicFadeVol, 1f, dt / Mathf.Max(0.1f, musicSmoothInDuration));
                            currentSource.volume = musicVolume * Mathf.SmoothStep(0f, 1f, currentMusicFadeVol);
                        }
                    }
                }

                if (windSource != null && windSource.isPlaying)
                {
                    windSource.volume = windVolume;
                }

                // Hotkey: Check for 'N' or 'M' to skip to next random track anytime!
                if (IsNextTrackHotkeyPressed())
                {
                    CrossfadeToNextRandomTrack(1.5f);
                }
            }
            else if (currentPhase == MenuPhase.BotWLoadingScreen || currentPhase == MenuPhase.TransitionToGameplay)
            {
                if (musicSourceA != null) musicSourceA.volume = Mathf.MoveTowards(musicSourceA.volume, 0f, dt * 0.8f);
                if (musicSourceB != null) musicSourceB.volume = Mathf.MoveTowards(musicSourceB.volume, 0f, dt * 0.8f);
                if (windSource != null) windSource.volume = Mathf.MoveTowards(windSource.volume, 0f, dt * 0.8f);
            }
        }

        private void UpdateMusicToast(float dt)
        {
            if (toastGroup == null) return;
            if (toastTimer > 0f)
            {
                toastTimer -= dt;
                toastGroup.alpha = Mathf.MoveTowards(toastGroup.alpha, 0.95f, dt * 3.5f);
            }
            else
            {
                toastGroup.alpha = Mathf.MoveTowards(toastGroup.alpha, 0f, dt * 1.5f);
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
            rebindingAction = null;
            RefreshKeybindButtonLabels();
            if (fullscreenBtnText != null)
                fullscreenBtnText.text = Screen.fullScreen ? "TOÀN MÀN HÌNH: [ BẬT ]" : "TOÀN MÀN HÌNH: [ TẮT ]";
            if (vsyncBtnText != null)
                vsyncBtnText.text = (QualitySettings.vSyncCount > 0) ? "ĐỒNG BỘ DỌC: [ BẬT ]" : "ĐỒNG BỘ DỌC: [ TẮT ]";
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

                if (card.HeaderText != null)
                {
                    card.HeaderText.text = $"SLOT {slot}";
                }

                if (data.isOccupied)
                {
                    // Try to load custom captured screenshot thumbnail
                    Texture2D thumbTex = HoaVaMauSaveSystem.LoadThumbnailTexture(slot);

                    // If not yet captured, use fallback concept art thumbnail
                    if (thumbTex == null && fallbackSlotThumbnails != null && fallbackSlotThumbnails.Length > 0)
                    {
                        int fallbackIdx = (slot - 1) % fallbackSlotThumbnails.Length;
                        thumbTex = fallbackSlotThumbnails[fallbackIdx];
                    }

                    if (thumbTex != null && card.ThumbnailImg != null)
                    {
                        card.ThumbnailImg.texture = thumbTex;
                        card.ThumbnailImg.gameObject.SetActive(true);
                    }
                    else if (card.ThumbnailImg != null)
                    {
                        card.ThumbnailImg.gameObject.SetActive(false);
                    }

                    if (card.EmptyContainer != null)
                    {
                        card.EmptyContainer.SetActive(false);
                    }

                    // Format matching Planet of Lana:
                    // Line 1: Timestamp (e.g. 10/26/2026 10:08:03 AM)
                    // Line 2: PLAYTIME 09:03:44
                    if (card.TimestampText != null)
                    {
                        card.TimestampText.text = string.IsNullOrEmpty(data.saveTimestamp) ? DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss tt") : data.saveTimestamp;
                    }
                    if (card.PlaytimeText != null)
                    {
                        card.PlaytimeText.text = $"PLAYTIME {data.GetFormattedPlayTime()}";
                    }
                }
                else
                {
                    if (card.ThumbnailImg != null)
                    {
                        card.ThumbnailImg.gameObject.SetActive(false);
                    }
                    if (card.EmptyContainer != null)
                    {
                        card.EmptyContainer.SetActive(true);
                    }
                    if (card.EmptyLabelText != null)
                    {
                        card.EmptyLabelText.text = "NEW GAME";
                    }
                    if (card.TimestampText != null)
                    {
                        card.TimestampText.text = "";
                    }
                    if (card.PlaytimeText != null)
                    {
                        card.PlaytimeText.text = "";
                    }
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

                // Pure crisp white outline border when selected
                if (card.CardBorderImg != null)
                {
                    card.CardBorderImg.color = isSel ? Color.white : new Color(0.12f, 0.16f, 0.24f, 0.40f);
                }

                if (card.HeaderText != null)
                {
                    card.HeaderText.color = isSel ? Color.white : new Color(0.70f, 0.75f, 0.85f, 0.75f);
                }

                if (card.CardFrameRect != null)
                {
                    card.CardFrameRect.localScale = isSel ? new Vector3(1.025f, 1.025f, 1f) : Vector3.one;
                }
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
                    HoaVaMauSaveSystem.SaveCurrentGameState(slotIndex, "Chương 1: Đất Đỏ Cà Mau", "Rừng Sớm Cà Mau", 0, new Vector3(-4.0f, -1.85f, 0f));
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
                HoaVaMauSaveSystem.SaveCurrentGameState(pendingOverwriteSlot, "Chương 1: Đất Đỏ Cà Mau", "Rừng Sớm Cà Mau", 0, new Vector3(-4.0f, -1.85f, 0f));
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

            // Planet of Lana iconic selection behavior:
            // Unselected: Dimmed semi-transparent silver text (alpha 0.60), no background box.
            // Selected: Solid white rectangle overlays behind text, text font color inverts to dark background navy (#07101C).
            if (item.SelectionBox != null)
            {
                item.SelectionBox.color = new Color(1f, 1f, 1f, item.HoverBlend);
            }

            Color unselTextColor = new Color(0.92f, 0.95f, 0.98f, 0.60f);
            Color selTextColor = new Color(0.04f, 0.08f, 0.14f, 1.0f); // Deep dark background navy
            item.LabelText.color = Color.Lerp(unselTextColor, selTextColor, item.HoverBlend);

            float scale = Mathf.Lerp(1.0f, 1.02f, item.HoverBlend);
            item.RootRect.localScale = new Vector3(scale, scale, 1f);

            Color dotCol = new Color(0.04f, 0.08f, 0.14f, item.HoverBlend * 0.92f);
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

        private void SetupButtonSolidWhiteHover(RectTransform btnRect, Image bgImg, Text labelText, Color? normalBg = null, Color? normalText = null)
        {
            if (btnRect == null) return;
            Color defaultBg = normalBg ?? (bgImg != null ? bgImg.color : new Color(0.18f, 0.22f, 0.32f, 0.85f));
            Color defaultText = normalText ?? (labelText != null ? labelText.color : Color.white);
            Color selBg = new Color(0.98f, 0.98f, 0.98f, 0.96f);
            Color selText = new Color(0.04f, 0.08f, 0.14f, 1f);

            var trigger = btnRect.gameObject.GetComponent<EventTrigger>() ?? btnRect.gameObject.AddComponent<EventTrigger>();
            var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enterEntry.callback.AddListener((data) =>
            {
                PlayHover();
                if (bgImg != null) bgImg.color = selBg;
                if (labelText != null) labelText.color = selText;
                btnRect.localScale = new Vector3(1.02f, 1.02f, 1f);
            });
            trigger.triggers.Add(enterEntry);

            var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exitEntry.callback.AddListener((data) =>
            {
                if (bgImg != null) bgImg.color = defaultBg;
                if (labelText != null) labelText.color = defaultText;
                btnRect.localScale = Vector3.one;
            });
            trigger.triggers.Add(exitEntry);
        }

        // =====================================================================================
        // PERSISTED SETTINGS (AUDIO & GRAPHICS)
        // =====================================================================================
        private void LoadPersistedSettings()
        {
            float mVol = PlayerPrefs.GetFloat("Settings_MasterVol", 1.0f);
            float bgmVol = PlayerPrefs.GetFloat("Settings_MusicVol", 0.65f);
            float sVol = PlayerPrefs.GetFloat("Settings_SFXVol", 0.45f);

            int vsync = PlayerPrefs.GetInt("Settings_VSync", 1);

            AudioListener.volume = mVol;
            musicVolume = bgmVol;
            windVolume = sVol * 0.5f;
            sfxVolume = sVol;
            QualitySettings.vSyncCount = vsync;
        }

        private void SavePersistedSettings()
        {
            PlayerPrefs.SetFloat("Settings_MasterVol", AudioListener.volume);
            PlayerPrefs.SetFloat("Settings_MusicVol", musicVolume);
            PlayerPrefs.SetFloat("Settings_SFXVol", sfxVolume);
            PlayerPrefs.SetInt("Settings_VSync", QualitySettings.vSyncCount);
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
            ApplyMusicVolumeToSources();
            if (musicVolValText != null) musicVolValText.text = $"{Mathf.RoundToInt(val * 100f)}%";
        }

        private void OnSfxVolumeChanged(float val)
        {
            sfxVolume = val;
            windVolume = val * 0.5f;
            if (windSource != null) windSource.volume = windVolume;
            if (sfxVolValText != null) sfxVolValText.text = $"{Mathf.RoundToInt(val * 100f)}%";
        }

        private void ToggleVSyncMode()
        {
            PlayConfirm();
            QualitySettings.vSyncCount = (QualitySettings.vSyncCount > 0) ? 0 : 1;
            PlayerPrefs.SetInt("Settings_VSync", QualitySettings.vSyncCount);
            PlayerPrefs.Save();
            if (vsyncBtnText != null)
            {
                vsyncBtnText.text = (QualitySettings.vSyncCount > 0) ? "ĐỒNG BỘ DỌC: [ BẬT ]" : "ĐỒNG BỘ DỌC: [ TẮT ]";
            }
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

        private void OnGUI()
        {
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
            iconImg.sprite = headphoneSprite ?? flowerBloomSprite;
            iconImg.preserveAspect = true;
            iconImg.color = (iconImg.sprite != null) ? new Color(0.96f, 0.95f, 0.90f, 0.95f) : Color.clear;
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
            cluster.anchoredPosition = new Vector2(115f, -52f);
            cluster.sizeDelta = new Vector2(650f, 840f);
            menuClusterRect = cluster;

            // NOTE: "FIVE COWS IN THE FIRE" text above the logo on Start Menu is REMOVED per user request!

            // 1. Official Calligraphy Logo (Enlarged prominently per user request: "làm to hơn một tí")
            if (gameOfficialLogoSprite != null)
            {
                if (softDotSprite != null)
                {
                    var glowGO = CreateRect("Official_Logo_WarmGlow", cluster);
                    glowGO.anchorMin = glowGO.anchorMax = new Vector2(0f, 1f);
                    glowGO.pivot = new Vector2(0f, 1f);
                    glowGO.anchoredPosition = new Vector2(-20f, 14f);
                    glowGO.sizeDelta = new Vector2(510f, 275f);
                    var glowImg = glowGO.gameObject.AddComponent<Image>();
                    glowImg.sprite = softDotSprite;
                    glowImg.color = new Color(0.04f, 0.09f, 0.16f, 0.24f);
                    glowImg.raycastTarget = false;
                }

                var logoGO = CreateRect("Official_Calligraphy_Logo", cluster);
                logoGO.anchorMin = logoGO.anchorMax = new Vector2(0f, 1f);
                logoGO.pivot = new Vector2(0f, 1f);
                logoGO.anchoredPosition = new Vector2(0f, 0f);
                logoGO.sizeDelta = new Vector2(465f, 250f);
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
                titleGO.sizeDelta = new Vector2(580f, 80f);
                var tTxt = titleGO.gameObject.AddComponent<Text>();
                tTxt.font = font;
                tTxt.fontSize = 46;
                tTxt.fontStyle = FontStyle.Bold;
                tTxt.alignment = TextAnchor.MiddleLeft;
                tTxt.color = Color.white;
                tTxt.text = "H O A   T R Ê N   Đ Ấ T   Đ Ỏ";
            }

            // 2. Thin Horizon Divider
            var divGO = CreateRect("Horizon_Divider", cluster);
            divGO.anchorMin = divGO.anchorMax = new Vector2(0f, 1f);
            divGO.pivot = new Vector2(0f, 1f);
            divGO.anchoredPosition = new Vector2(8f, -248f);
            divGO.sizeDelta = new Vector2(380f, 2f);
            var divImg = divGO.gameObject.AddComponent<Image>();
            divImg.color = new Color(1.0f, 0.95f, 0.80f, 0.65f);

            // 3. English Subtitle (FLOWER ON RED SOIL)
            var subGO = CreateRect("Subtitle_Text", cluster);
            subGO.anchorMin = subGO.anchorMax = new Vector2(0f, 1f);
            subGO.pivot = new Vector2(0f, 1f);
            subGO.anchoredPosition = new Vector2(8f, -264f);
            subGO.sizeDelta = new Vector2(460f, 28f);
            var subTxt = subGO.gameObject.AddComponent<Text>();
            subTxt.font = font;
            subTxt.fontSize = 14;
            subTxt.alignment = TextAnchor.MiddleLeft;
            subTxt.color = new Color(0.98f, 0.92f, 0.75f, 0.94f);
            subTxt.text = "• • •   F L O W E R   O N   R E D   S O I L   • • •";

            // 4. Menu Buttons (BẮT ĐẦU, TIẾP TỤC, TÙY CHỈNH, THOÁT)
            var btnContainer = CreateRect("MainButtons_Container", cluster);
            btnContainer.anchorMin = btnContainer.anchorMax = new Vector2(0f, 1f);
            btnContainer.pivot = new Vector2(0f, 1f);
            btnContainer.anchoredPosition = new Vector2(8f, -312f);
            btnContainer.sizeDelta = new Vector2(380f, 270f);

            string[] labels = { "BẮT ĐẦU", "TIẾP TỤC", "TÙY CHỈNH", "THOÁT" };
            menuButtons.Clear();

            for (int i = 0; i < labels.Length; i++)
            {
                int idx = i;
                var btnRect = CreateRect($"Btn_{i}_{labels[i]}", btnContainer);
                btnRect.anchorMin = btnRect.anchorMax = new Vector2(0f, 1f);
                btnRect.pivot = new Vector2(0f, 1f);
                btnRect.anchoredPosition = new Vector2(0f, -i * 56f);
                btnRect.sizeDelta = new Vector2(345f, 46f);

                var btn = btnRect.gameObject.AddComponent<Button>();
                var colors = btn.colors;
                colors.normalColor = Color.clear;
                colors.highlightedColor = Color.clear;
                colors.pressedColor = Color.clear;
                btn.colors = colors;

                var hitImg = btnRect.gameObject.AddComponent<Image>();
                hitImg.color = Color.clear;
                hitImg.raycastTarget = true;
                btn.targetGraphic = hitImg;

                var trigger = btnRect.gameObject.AddComponent<EventTrigger>();
                var hoverEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                hoverEntry.callback.AddListener((data) =>
                {
                    if (selectedMenuIndex != idx)
                    {
                        selectedMenuIndex = idx;
                        PlayHover();
                    }
                });
                trigger.triggers.Add(hoverEntry);

                // Planet of Lana selection box: Solid white rectangle overlay behind text
                var boxRect = CreateRect("SelectionBox", btnRect);
                boxRect.anchorMin = new Vector2(0f, 0.1f);
                boxRect.anchorMax = new Vector2(0.92f, 0.9f);
                boxRect.offsetMin = Vector2.zero;
                boxRect.offsetMax = Vector2.zero;
                var boxImg = boxRect.gameObject.AddComponent<Image>();
                boxImg.color = new Color(1f, 1f, 1f, 0f); // Hidden until selected
                boxImg.raycastTarget = false;

                var txtRect = CreateRect("Label", btnRect);
                txtRect.anchorMin = new Vector2(0f, 0f);
                txtRect.anchorMax = new Vector2(1f, 1f);
                txtRect.offsetMin = new Vector2(24f, 0f);
                txtRect.offsetMax = Vector2.zero;
                var txt = txtRect.gameObject.AddComponent<Text>();
                txt.font = font;
                txt.fontSize = 18;
                txt.alignment = TextAnchor.MiddleLeft;
                txt.color = new Color(0.92f, 0.95f, 0.98f, 0.60f);
                txt.text = labels[i];
                txt.raycastTarget = false;

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
                lTxt.raycastTarget = false;

                btn.onClick.AddListener(() => TriggerMenuSelection(idx));

                var item = new MenuButtonItem
                {
                    RootRect = btnRect,
                    ButtonComp = btn,
                    SelectionBox = boxImg,
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

            // Now Playing Toast Widget (Bottom-Left Corner)
            var toastGO = CreateRect("Music_NowPlaying_Toast", panelRect);
            toastGO.anchorMin = toastGO.anchorMax = new Vector2(0f, 0f);
            toastGO.pivot = new Vector2(0f, 0f);
            toastGO.anchoredPosition = new Vector2(40f, 32f);
            toastGO.sizeDelta = new Vector2(500f, 36f);

            toastGroup = toastGO.gameObject.AddComponent<CanvasGroup>();
            toastGroup.alpha = 0f;
            toastGroup.blocksRaycasts = false;
            toastGroup.interactable = false;

            var toastTxtGO = CreateRect("ToastText", toastGO);
            StretchFull(toastTxtGO);
            toastText = toastTxtGO.gameObject.AddComponent<Text>();
            toastText.font = font;
            toastText.fontSize = 13;
            toastText.fontStyle = FontStyle.Normal;
            toastText.alignment = TextAnchor.MiddleLeft;
            toastText.color = new Color(0.96f, 0.94f, 0.88f, 0.90f);
            toastText.text = "";
            var toastShadow = toastTxtGO.gameObject.AddComponent<Shadow>();
            toastShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            toastShadow.effectDistance = new Vector2(1f, -1f);
        }

        private void BuildSaveSlotModal(Transform parent, Font font)
        {
            var modalRect = CreateRect("Modal_SaveSlot_Selection", parent);
            StretchFull(modalRect);
            saveModalGroup = modalRect.gameObject.AddComponent<CanvasGroup>();
            saveModalGroup.alpha = 0f;
            modalRect.gameObject.SetActive(false);

            var bgImg = modalRect.gameObject.AddComponent<Image>();
            bgImg.color = new Color(0.02f, 0.04f, 0.07f, 0.95f);

            var centerBox = CreateRect("Center_Dialog_Box", modalRect);
            centerBox.anchorMin = centerBox.anchorMax = new Vector2(0.5f, 0.5f);
            centerBox.pivot = new Vector2(0.5f, 0.5f);
            centerBox.sizeDelta = new Vector2(1150f, 750f);

            var titleRect = CreateRect("SaveModal_Title", centerBox);
            titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 0.96f);
            titleRect.sizeDelta = new Vector2(800f, 32f);
            saveModalTitleText = titleRect.gameObject.AddComponent<Text>();
            saveModalTitleText.font = font;
            saveModalTitleText.fontSize = 17;
            saveModalTitleText.fontStyle = FontStyle.Bold;
            saveModalTitleText.alignment = TextAnchor.MiddleCenter;
            saveModalTitleText.color = new Color(0.88f, 0.92f, 0.98f, 0.85f);
            saveModalTitleText.text = "C H Ọ N   V Ị   T R Í   L Ư U";

            // Notice text for warnings
            var noticeRect = CreateRect("SaveModal_Notice", centerBox);
            noticeRect.anchorMin = noticeRect.anchorMax = new Vector2(0.5f, 0.92f);
            noticeRect.sizeDelta = new Vector2(700f, 24f);
            saveModalNoticeText = noticeRect.gameObject.AddComponent<Text>();
            saveModalNoticeText.font = font;
            saveModalNoticeText.fontSize = 12;
            saveModalNoticeText.alignment = TextAnchor.MiddleCenter;
            saveModalNoticeText.color = new Color(0.98f, 0.45f, 0.35f, 0.95f);
            saveModalNoticeText.text = "";

            // 3 Vertical Save Slot Cards (Planet of Lana Style)
            slotUIItems.Clear();
            float[] posXs = new float[] { -340f, 0f, 340f };

            for (int i = 0; i < 3; i++)
            {
                int slot = i + 1;
                var slotColRect = CreateRect($"Slot_Column_{slot}", centerBox);
                slotColRect.anchorMin = slotColRect.anchorMax = new Vector2(0.5f, 0.5f);
                slotColRect.anchoredPosition = new Vector2(posXs[i], 12f);
                slotColRect.sizeDelta = new Vector2(280f, 620f);

                // 1. Header above card: "SLOT 1", "SLOT 2", "SLOT 3"
                var headerRect = CreateRect($"Slot_Header_{slot}", slotColRect);
                headerRect.anchorMin = headerRect.anchorMax = new Vector2(0.5f, 0.5f);
                headerRect.anchoredPosition = new Vector2(0f, 275f);
                headerRect.sizeDelta = new Vector2(280f, 30f);
                var headerTxt = headerRect.gameObject.AddComponent<Text>();
                headerTxt.font = font;
                headerTxt.fontSize = 20;
                headerTxt.fontStyle = FontStyle.Bold;
                headerTxt.alignment = TextAnchor.MiddleCenter;
                headerTxt.color = new Color(0.96f, 0.96f, 0.98f, 0.95f);
                headerTxt.text = $"SLOT {slot}";

                // 2. The Card Frame (280 x 480)
                var cardFrameRect = CreateRect($"Card_Frame_{slot}", slotColRect);
                cardFrameRect.anchorMin = cardFrameRect.anchorMax = new Vector2(0.5f, 0.5f);
                cardFrameRect.anchoredPosition = new Vector2(0f, 0f);
                cardFrameRect.sizeDelta = new Vector2(280f, 480f);

                // Border Image (Active highlight)
                var borderImg = cardFrameRect.gameObject.AddComponent<Image>();
                borderImg.color = (i == 0) ? Color.white : new Color(0.12f, 0.16f, 0.24f, 0.40f);

                // Button Component for clicking the card
                var btn = cardFrameRect.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() => OnSlotCardClicked(slot));

                int cardIdx = i;
                var trigger = cardFrameRect.gameObject.AddComponent<EventTrigger>();
                var hoverEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                hoverEntry.callback.AddListener((data) =>
                {
                    if (selectedSlotIndex != cardIdx)
                    {
                        selectedSlotIndex = cardIdx;
                        PlayHover();
                        HighlightSelectedSlot();
                    }
                });
                trigger.triggers.Add(hoverEntry);

                // Inner Container (274 x 474 with 3px border padding)
                var innerRect = CreateRect("Inner_Area", cardFrameRect);
                innerRect.anchorMin = innerRect.anchorMax = new Vector2(0.5f, 0.5f);
                innerRect.sizeDelta = new Vector2(274f, 474f);
                var innerBg = innerRect.gameObject.AddComponent<Image>();
                innerBg.color = new Color(0.04f, 0.07f, 0.13f, 1.0f);

                // Thumbnail RawImage (Displays live screenshot thumbnail or fallback concept art)
                var thumbRect = CreateRect("Thumbnail_RawImage", innerRect);
                StretchFull(thumbRect);
                var thumbRawImg = thumbRect.gameObject.AddComponent<RawImage>();
                thumbRawImg.color = Color.white;
                thumbRawImg.raycastTarget = false;

                // Empty Slot Container (Displays dark navy box with centered "NEW GAME")
                var emptyContainerRect = CreateRect("Empty_Slot_Container", innerRect);
                StretchFull(emptyContainerRect);
                var emptyBg = emptyContainerRect.gameObject.AddComponent<Image>();
                emptyBg.color = new Color(0.05f, 0.09f, 0.16f, 1.0f);
                emptyBg.raycastTarget = false;

                var emptyLabelRect = CreateRect("NewGame_Label", emptyContainerRect);
                StretchFull(emptyLabelRect);
                var emptyLabelTxt = emptyLabelRect.gameObject.AddComponent<Text>();
                emptyLabelTxt.font = font;
                emptyLabelTxt.fontSize = 18;
                emptyLabelTxt.fontStyle = FontStyle.Bold;
                emptyLabelTxt.alignment = TextAnchor.MiddleCenter;
                emptyLabelTxt.color = new Color(0.96f, 0.96f, 0.98f, 0.95f);
                emptyLabelTxt.text = "NEW GAME";

                // 3. Metadata Below Card: Timestamp & Playtime
                var metaRect = CreateRect("Metadata_Area", slotColRect);
                metaRect.anchorMin = metaRect.anchorMax = new Vector2(0.5f, 0.5f);
                metaRect.anchoredPosition = new Vector2(0f, -270f);
                metaRect.sizeDelta = new Vector2(280f, 46f);

                var timeRect = CreateRect("Timestamp", metaRect);
                timeRect.anchorMin = timeRect.anchorMax = new Vector2(0.5f, 0.70f);
                timeRect.sizeDelta = new Vector2(280f, 20f);
                var timeTxt = timeRect.gameObject.AddComponent<Text>();
                timeTxt.font = font;
                timeTxt.fontSize = 12;
                timeTxt.fontStyle = FontStyle.Bold;
                timeTxt.alignment = TextAnchor.MiddleCenter;
                timeTxt.color = new Color(0.94f, 0.96f, 0.98f, 0.95f);
                timeTxt.text = "";

                var playTimeRect = CreateRect("Playtime", metaRect);
                playTimeRect.anchorMin = playTimeRect.anchorMax = new Vector2(0.5f, 0.25f);
                playTimeRect.sizeDelta = new Vector2(280f, 20f);
                var playTimeTxt = playTimeRect.gameObject.AddComponent<Text>();
                playTimeTxt.font = font;
                playTimeTxt.fontSize = 11;
                playTimeTxt.fontStyle = FontStyle.Bold;
                playTimeTxt.alignment = TextAnchor.MiddleCenter;
                playTimeTxt.color = new Color(0.85f, 0.88f, 0.92f, 0.85f);
                playTimeTxt.text = "";

                slotUIItems.Add(new SaveSlotUIItem
                {
                    RootRect = slotColRect,
                    CardFrameRect = cardFrameRect,
                    ButtonComp = btn,
                    HeaderText = headerTxt,
                    CardBorderImg = borderImg,
                    CardInnerBg = innerBg,
                    ThumbnailImg = thumbRawImg,
                    EmptyContainer = emptyContainerRect.gameObject,
                    EmptyLabelText = emptyLabelTxt,
                    TimestampText = timeTxt,
                    PlaytimeText = playTimeTxt,
                    SlotIndex = slot,
                    IsOccupied = false
                });
            }

            // Bottom Navigation Keycaps: [Esc] BACK    [Enter] SELECT
            BuildBottomNavKeycaps(centerBox, font);
        }

        private void BuildBottomNavKeycaps(Transform parent, Font font)
        {
            var navRect = CreateRect("Bottom_Nav_Hints", parent);
            navRect.anchorMin = navRect.anchorMax = new Vector2(0.5f, 0.05f);
            navRect.sizeDelta = new Vector2(420f, 36f);

            // Back item: [Esc] BACK
            var backItem = CreateRect("Nav_Back_Cluster", navRect);
            backItem.anchorMin = backItem.anchorMax = new Vector2(0.28f, 0.5f);
            backItem.sizeDelta = new Vector2(150f, 32f);

            var escKeycap = CreateRect("Keycap_Esc", backItem);
            escKeycap.anchorMin = escKeycap.anchorMax = new Vector2(0.22f, 0.5f);
            escKeycap.sizeDelta = new Vector2(38f, 24f);
            var escBorder = escKeycap.gameObject.AddComponent<Image>();
            escBorder.color = new Color(0.95f, 0.95f, 0.98f, 0.85f);

            var escInner = CreateRect("Keycap_Inner", escKeycap);
            escInner.anchorMin = escInner.anchorMax = new Vector2(0.5f, 0.5f);
            escInner.sizeDelta = new Vector2(34f, 20f);
            var escInnerImg = escInner.gameObject.AddComponent<Image>();
            escInnerImg.color = new Color(0.04f, 0.07f, 0.13f, 1f);

            var escLabel = CreateRect("Label", escInner);
            StretchFull(escLabel);
            var escTxt = escLabel.gameObject.AddComponent<Text>();
            escTxt.font = font;
            escTxt.fontSize = 11;
            escTxt.fontStyle = FontStyle.Bold;
            escTxt.alignment = TextAnchor.MiddleCenter;
            escTxt.color = Color.white;
            escTxt.text = "Esc";

            var backLabel = CreateRect("Label_Back", backItem);
            backLabel.anchorMin = backLabel.anchorMax = new Vector2(0.72f, 0.5f);
            backLabel.sizeDelta = new Vector2(75f, 25f);
            var backLabelTxt = backLabel.gameObject.AddComponent<Text>();
            backLabelTxt.font = font;
            backLabelTxt.fontSize = 13;
            backLabelTxt.fontStyle = FontStyle.Bold;
            backLabelTxt.alignment = TextAnchor.MiddleLeft;
            backLabelTxt.color = new Color(0.95f, 0.95f, 0.98f, 0.90f);
            backLabelTxt.text = "BACK";

            var backBtn = backItem.gameObject.AddComponent<Button>();
            backBtn.onClick.AddListener(CloseSaveModal);

            // Select item: [Enter] SELECT
            var selectItem = CreateRect("Nav_Select_Cluster", navRect);
            selectItem.anchorMin = selectItem.anchorMax = new Vector2(0.76f, 0.5f);
            selectItem.sizeDelta = new Vector2(170f, 32f);

            var enterKeycap = CreateRect("Keycap_Enter", selectItem);
            enterKeycap.anchorMin = enterKeycap.anchorMax = new Vector2(0.22f, 0.5f);
            enterKeycap.sizeDelta = new Vector2(48f, 24f);
            var enterBorder = enterKeycap.gameObject.AddComponent<Image>();
            enterBorder.color = new Color(0.95f, 0.95f, 0.98f, 0.85f);

            var enterInner = CreateRect("Keycap_Inner", enterKeycap);
            enterInner.anchorMin = enterInner.anchorMax = new Vector2(0.5f, 0.5f);
            enterInner.sizeDelta = new Vector2(44f, 20f);
            var enterInnerImg = enterInner.gameObject.AddComponent<Image>();
            enterInnerImg.color = new Color(0.04f, 0.07f, 0.13f, 1f);

            var enterLabel = CreateRect("Label", enterInner);
            StretchFull(enterLabel);
            var enterTxt = enterLabel.gameObject.AddComponent<Text>();
            enterTxt.font = font;
            enterTxt.fontSize = 11;
            enterTxt.fontStyle = FontStyle.Bold;
            enterTxt.alignment = TextAnchor.MiddleCenter;
            enterTxt.color = Color.white;
            enterTxt.text = "Enter";

            var selLabel = CreateRect("Label_Select", selectItem);
            selLabel.anchorMin = selLabel.anchorMax = new Vector2(0.74f, 0.5f);
            selLabel.sizeDelta = new Vector2(85f, 25f);
            var selLabelTxt = selLabel.gameObject.AddComponent<Text>();
            selLabelTxt.font = font;
            selLabelTxt.fontSize = 13;
            selLabelTxt.fontStyle = FontStyle.Bold;
            selLabelTxt.alignment = TextAnchor.MiddleLeft;
            selLabelTxt.color = new Color(0.95f, 0.95f, 0.98f, 0.90f);
            selLabelTxt.text = "SELECT";

            var selBtn = selectItem.gameObject.AddComponent<Button>();
            selBtn.onClick.AddListener(() =>
            {
                if (selectedSlotIndex >= 0 && selectedSlotIndex < slotUIItems.Count)
                {
                    OnSlotCardClicked(slotUIItems[selectedSlotIndex].SlotIndex);
                }
            });
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
            box.sizeDelta = new Vector2(860f, 660f);

            var boxBg = box.gameObject.AddComponent<Image>();
            boxBg.color = new Color(0.07f, 0.09f, 0.14f, 0.95f);

            // Title
            var titleRect = CreateRect("Title", box);
            titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 0.95f);
            titleRect.sizeDelta = new Vector2(700f, 36f);
            var title = titleRect.gameObject.AddComponent<Text>();
            title.font = font;
            title.fontSize = 20;
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = new Color(1.0f, 0.98f, 0.90f, 0.95f);
            title.text = "T Ù Y   C H Ỉ N H   H Ệ   T H Ố N G";

            // SECTION 1: ÂM LƯỢNG (AUDIO)
            float startY = -62f;
            CreateSectionHeader(box, "1. ÂM LƯỢNG (AUDIO)", new Vector2(40f, startY), font);

            startY -= 34f;
            masterVolSlider = CreateSliderRow(box, "Âm lượng Tổng", AudioListener.volume, new Vector2(40f, startY), font, out masterVolValText);
            masterVolSlider.onValueChanged.AddListener(OnMasterVolumeChanged);

            startY -= 36f;
            musicVolSlider = CreateSliderRow(box, "Âm lượng Nhạc nền", musicVolume, new Vector2(40f, startY), font, out musicVolValText);
            musicVolSlider.onValueChanged.AddListener(OnMusicVolumeChanged);

            startY -= 36f;
            sfxVolSlider = CreateSliderRow(box, "Hiệu ứng âm thanh (SFX)", sfxVolume, new Vector2(40f, startY), font, out sfxVolValText);
            sfxVolSlider.onValueChanged.AddListener(OnSfxVolumeChanged);

            // SECTION 2: ĐỒ HỌA & HIỂN THỊ (GRAPHICS)
            startY -= 46f;
            CreateSectionHeader(box, "2. ĐỒ HỌA & HIỂN THỊ (GRAPHICS)", new Vector2(40f, startY), font);

            startY -= 36f;
            // Fullscreen Button
            var fsBtnRect = CreateRect("Btn_Toggle_Fullscreen", box);
            fsBtnRect.anchorMin = fsBtnRect.anchorMax = new Vector2(0f, 1f);
            fsBtnRect.pivot = new Vector2(0f, 1f);
            fsBtnRect.anchoredPosition = new Vector2(40f, startY);
            fsBtnRect.sizeDelta = new Vector2(360f, 34f);

            var fsBtn = fsBtnRect.gameObject.AddComponent<Button>();
            var fsImg = fsBtnRect.gameObject.AddComponent<Image>();
            fsImg.color = new Color(0.18f, 0.22f, 0.32f, 0.85f);
            fsBtn.targetGraphic = fsImg;

            var fsTxtRect = CreateRect("Label", fsBtnRect);
            StretchFull(fsTxtRect);
            fullscreenBtnText = fsTxtRect.gameObject.AddComponent<Text>();
            fullscreenBtnText.font = font;
            fullscreenBtnText.fontSize = 13;
            fullscreenBtnText.alignment = TextAnchor.MiddleCenter;
            fullscreenBtnText.color = Color.white;
            fullscreenBtnText.raycastTarget = false;
            fullscreenBtnText.text = Screen.fullScreen ? "TOÀN MÀN HÌNH: [ BẬT ]" : "TOÀN MÀN HÌNH: [ TẮT ]";
            fsBtn.onClick.AddListener(ToggleFullscreenMode);
            SetupButtonSolidWhiteHover(fsBtnRect, fsImg, fullscreenBtnText);

            // VSync Button
            var vsyncBtnRect = CreateRect("Btn_Toggle_VSync", box);
            vsyncBtnRect.anchorMin = vsyncBtnRect.anchorMax = new Vector2(0f, 1f);
            vsyncBtnRect.pivot = new Vector2(0f, 1f);
            vsyncBtnRect.anchoredPosition = new Vector2(440f, startY);
            vsyncBtnRect.sizeDelta = new Vector2(360f, 34f);

            var vsyncBtn = vsyncBtnRect.gameObject.AddComponent<Button>();
            var vsyncImg = vsyncBtnRect.gameObject.AddComponent<Image>();
            vsyncImg.color = new Color(0.18f, 0.22f, 0.32f, 0.85f);
            vsyncBtn.targetGraphic = vsyncImg;

            var vsyncTxtRect = CreateRect("Label", vsyncBtnRect);
            StretchFull(vsyncTxtRect);
            vsyncBtnText = vsyncTxtRect.gameObject.AddComponent<Text>();
            vsyncBtnText.font = font;
            vsyncBtnText.fontSize = 13;
            vsyncBtnText.alignment = TextAnchor.MiddleCenter;
            vsyncBtnText.color = Color.white;
            vsyncBtnText.raycastTarget = false;
            vsyncBtnText.text = (QualitySettings.vSyncCount > 0) ? "ĐỒNG BỘ DỌC: [ BẬT ]" : "ĐỒNG BỘ DỌC: [ TẮT ]";
            vsyncBtn.onClick.AddListener(ToggleVSyncMode);
            SetupButtonSolidWhiteHover(vsyncBtnRect, vsyncImg, vsyncBtnText);

            // SECTION 3: BẢNG PHÍM ĐIỀU KHIỂN (CONTROLS REBINDING)
            startY -= 48f;
            CreateSectionHeader(box, "3. BẢNG PHÍM ĐIỀU KHIỂN (CONTROLS - BẤM ĐỂ ĐỔI PHÍM)", new Vector2(40f, startY), font);

            startY -= 34f;
            keybindButtonTexts.Clear();
            var keyActions = new[]
            {
                GameAction.MoveLeft,
                GameAction.MoveRight,
                GameAction.Jump,
                GameAction.Sprint,
                GameAction.Crouch,
                GameAction.Interact
            };

            for (int k = 0; k < keyActions.Length; k++)
            {
                var action = keyActions[k];
                float colX = (k % 2 == 0) ? 40f : 440f;
                float rowY = startY - (k / 2) * 38f;

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
                SetupButtonSolidWhiteHover(btnRect, bImg, bTxt);
            }

            // RESET TO DEFAULTS BUTTON
            var resetBtnRect = CreateRect("Btn_Reset_Defaults", box);
            resetBtnRect.anchorMin = resetBtnRect.anchorMax = new Vector2(0.30f, 0.055f);
            resetBtnRect.sizeDelta = new Vector2(170f, 40f);
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
            SetupButtonSolidWhiteHover(resetBtnRect, rImg, rTxt);

            // BACK / SAVE BUTTON
            var backBtnRect = CreateRect("Btn_Back_Options", box);
            backBtnRect.anchorMin = backBtnRect.anchorMax = new Vector2(0.70f, 0.055f);
            backBtnRect.sizeDelta = new Vector2(170f, 40f);
            var backBtn = backBtnRect.gameObject.AddComponent<Button>();
            var backImg = backBtnRect.gameObject.AddComponent<Image>();
            backImg.color = new Color(0.85f, 0.42f, 0.18f, 0.90f);
            backBtn.targetGraphic = backImg;
            var backTxtRect = CreateRect("Label", backBtnRect);
            StretchFull(backTxtRect);
            var backTxt = backTxtRect.gameObject.AddComponent<Text>();
            backTxt.font = font;
            backTxt.fontSize = 14;
            backTxt.fontStyle = FontStyle.Bold;
            backTxt.alignment = TextAnchor.MiddleCenter;
            backTxt.color = Color.white;
            backTxt.text = "LƯU & QUAY LẠI";
            backTxt.raycastTarget = false;
            backBtn.onClick.AddListener(CloseOptionsDialog);
            SetupButtonSolidWhiteHover(backBtnRect, backImg, backTxt, new Color(0.85f, 0.42f, 0.18f, 0.90f), Color.white);
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

        private static bool IsNavRightPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            return kb != null && (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D);
#endif
        }

        private static bool IsNavLeftPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            return kb != null && (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A);
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
