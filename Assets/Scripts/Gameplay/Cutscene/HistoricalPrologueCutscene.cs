using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Historical Prologue Cutscene System for "Hoa trên Đất Đỏ".
    /// Presents authentic 1954 documentary photographs with subtle Ken Burns pan/zoom,
    /// evocative historical subtitles, letterboxing, and a graceful skip mechanic (Planet of Lana style).
    /// Triggers only on first-time New Game play.
    /// </summary>
    [DisallowMultipleComponent]
    public class HistoricalPrologueCutscene : MonoBehaviour
    {
        [System.Serializable]
        public class HistorySlide
        {
            public Sprite photoSprite;
            [TextArea(2, 4)]
            public string historicalCaption;
            public float duration = 6.5f;
            public Vector2 panStart = Vector2.zero;
            public Vector2 panEnd = Vector2.zero;
            public float zoomStart = 1.0f;
            public float zoomEnd = 1.08f;
        }

        [Header("Historical Slideshow Settings")]
        [SerializeField] private List<HistorySlide> slides = new List<HistorySlide>();
        [SerializeField] private float fadeDuration = 1.2f;
        [SerializeField] private string targetGameplayScene = "New Scene 1_Overhaul";
        [SerializeField] private bool autoPlayOnStart = false;

        [Header("Audio (Optional)")]
        [SerializeField] private AudioClip historicalBgmClip;
        [SerializeField] private AudioClip oceanWindAmbienceClip;
        [Range(0f, 1f)] [SerializeField] private float bgmVolume = 0.5f;

        [Header("Skip Controls")]
        [SerializeField] private float holdToSkipDuration = 0.8f;

        // Runtime UI elements
        private Canvas cutsceneCanvas;
        private CanvasGroup cutsceneCanvasGroup;
        private Image slideImageA;
        private Image slideImageB;
        private CanvasGroup groupA;
        private CanvasGroup groupB;
        private Text captionText;
        private CanvasGroup captionGroup;
        private Slider skipProgressSlider;
        private Text skipPromptText;
        private AudioSource bgmSource;
        private AudioSource ambienceSource;

        private int currentSlideIndex = 0;
        private bool isRunning = false;
        private float skipHoldTimer = 0f;
        private Coroutine presentationCoroutine;

        public static event Action OnCutsceneFinished;

        private void Awake()
        {
            BuildRuntimeUI();
            SetupDefaultSlidesIfEmpty();
        }

        private void Start()
        {
            if (autoPlayOnStart)
            {
                PlayHistoricalCutscene();
            }
        }

        private void Update()
        {
            if (!isRunning) return;

            // Hold Space or Escape to Skip
            bool isSkipPressed = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.Escape) || Input.GetKey(KeyCode.Return);
            if (isSkipPressed)
            {
                skipHoldTimer += Time.unscaledDeltaTime;
                if (skipProgressSlider != null)
                {
                    skipProgressSlider.gameObject.SetActive(true);
                    skipProgressSlider.value = Mathf.Clamp01(skipHoldTimer / holdToSkipDuration);
                }

                if (skipHoldTimer >= holdToSkipDuration)
                {
                    SkipCutscene();
                }
            }
            else
            {
                skipHoldTimer = Mathf.Max(0f, skipHoldTimer - Time.unscaledDeltaTime * 2.5f);
                if (skipProgressSlider != null)
                {
                    skipProgressSlider.value = Mathf.Clamp01(skipHoldTimer / holdToSkipDuration);
                    if (skipHoldTimer <= 0.01f)
                    {
                        skipProgressSlider.gameObject.SetActive(false);
                    }
                }
            }
        }

        public void PlayHistoricalCutscene(Action onCompleteCallback = null)
        {
            if (isRunning) return;
            isRunning = true;
            skipHoldTimer = 0f;

            if (cutsceneCanvasGroup != null)
            {
                cutsceneCanvasGroup.alpha = 1f;
                cutsceneCanvasGroup.blocksRaycasts = true;
            }

            PlayAudio();
            presentationCoroutine = StartCoroutine(PresentationRoutine(onCompleteCallback));
        }

        public void SkipCutscene()
        {
            if (!isRunning) return;
            if (presentationCoroutine != null) StopCoroutine(presentationCoroutine);
            StartCoroutine(FinishAndTransition(null));
        }

        private IEnumerator PresentationRoutine(Action onCompleteCallback)
        {
            bool useA = true;

            for (int i = 0; i < slides.Count; i++)
            {
                currentSlideIndex = i;
                HistorySlide slide = slides[i];

                Image activeImage = useA ? slideImageA : slideImageB;
                CanvasGroup activeGroup = useA ? groupA : groupB;
                CanvasGroup inactiveGroup = useA ? groupB : groupA;

                if (slide.photoSprite != null)
                {
                    activeImage.sprite = slide.photoSprite;
                    activeImage.preserveAspect = true;
                }

                // Show caption
                if (captionText != null)
                {
                    captionText.text = slide.historicalCaption;
                }

                // Fade In active slide
                float elapsed = 0f;
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / fadeDuration);
                    activeGroup.alpha = t;
                    inactiveGroup.alpha = 1f - t;
                    if (captionGroup != null) captionGroup.alpha = Mathf.SmoothStep(0f, 1f, t);
                    yield return null;
                }

                activeGroup.alpha = 1f;
                inactiveGroup.alpha = 0f;

                // Ken Burns pan and zoom
                float slideElapsed = 0f;
                RectTransform rect = activeImage.rectTransform;
                while (slideElapsed < slide.duration)
                {
                    slideElapsed += Time.unscaledDeltaTime;
                    float progress = Mathf.Clamp01(slideElapsed / slide.duration);
                    float smoothP = Mathf.SmoothStep(0f, 1f, progress);

                    rect.localScale = Vector3.one * Mathf.Lerp(slide.zoomStart, slide.zoomEnd, smoothP);
                    rect.anchoredPosition = Vector2.Lerp(slide.panStart, slide.panEnd, smoothP);
                    yield return null;
                }

                useA = !useA;
            }

            yield return FinishAndTransition(onCompleteCallback);
        }

        private IEnumerator FinishAndTransition(Action onCompleteCallback)
        {
            isRunning = false;

            // Fade out audio and UI
            float elapsed = 0f;
            float dur = 1.0f;
            while (elapsed < dur)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / dur);
                if (cutsceneCanvasGroup != null) cutsceneCanvasGroup.alpha = 1f - t;
                if (bgmSource != null) bgmSource.volume = Mathf.Lerp(bgmVolume, 0f, t);
                yield return null;
            }

            if (cutsceneCanvasGroup != null)
            {
                cutsceneCanvasGroup.alpha = 0f;
                cutsceneCanvasGroup.blocksRaycasts = false;
            }

            OnCutsceneFinished?.Invoke();
            onCompleteCallback?.Invoke();

            if (!string.IsNullOrEmpty(targetGameplayScene))
            {
                SceneManager.LoadScene(targetGameplayScene);
            }
        }

        private void SetupDefaultSlidesIfEmpty()
        {
            if (slides.Count > 0) return;

            // Try to load historical textures from Assets/Textures/UI/HistoryCutscene
            Sprite soldierSprite = LoadSpriteAsset("Textures/UI/HistoryCutscene/History_1954_CaMau_Regroupment_Soldiers");
            Sprite carrierSprite = LoadSpriteAsset("Textures/UI/HistoryCutscene/History_1954_Carrier_Escort");
            Sprite voyageSprite = LoadSpriteAsset("Textures/UI/HistoryCutscene/History_1954_Truong_Xuan_Voyage");
            Sprite frigateSprite = LoadSpriteAsset("Textures/UI/HistoryCutscene/History_1954_Naval_Frigate");

            slides.Add(new HistorySlide
            {
                photoSprite = carrierSprite ?? frigateSprite,
                historicalCaption = "Tháng 7 năm 1954 • Hiệp định Genève được ký kết.\nĐất nước tạm thời phân định ranh giới... Vận mệnh non sông đứng trước những bước ngoặt lịch sử.",
                duration = 6.0f,
                panStart = new Vector2(-15f, 0f),
                panEnd = new Vector2(15f, 0f),
                zoomStart = 1.0f,
                zoomEnd = 1.06f
            });

            slides.Add(new HistorySlide
            {
                photoSprite = soldierSprite,
                historicalCaption = "Vùng đất cực Nam – Cửa Sông Đốc, Cà Mau trở thành khu tập kết 200 ngày đêm.\nHàng vạn chiến sĩ, đồng bào miền Nam bịn rịn chia tay mang theo lời hẹn ước non sông thống nhất.",
                duration = 6.5f,
                panStart = new Vector2(0f, -10f),
                panEnd = new Vector2(0f, 10f),
                zoomStart = 1.0f,
                zoomEnd = 1.08f
            });

            slides.Add(new HistorySlide
            {
                photoSprite = voyageSprite,
                historicalCaption = "Những chuyến tàu viễn xứ xuôi ngược giữa ngàn trùng sóng gió.\nNgười ra đi dặn lòng gìn giữ niềm tin, người ở lại kiên cường bám trụ mảnh đất quê hương.",
                duration = 6.0f,
                panStart = new Vector2(10f, 0f),
                panEnd = new Vector2(-10f, 0f),
                zoomStart = 1.02f,
                zoomEnd = 1.07f
            });

            slides.Add(new HistorySlide
            {
                photoSprite = soldierSprite,
                historicalCaption = "Dưới bóng dừa rợp mát và những dòng kinh đỏ nặng phù sa Cà Mau...\nCâu chuyện về An – người con của vùng Đất Đỏ bắt đầu.",
                duration = 5.5f,
                panStart = Vector2.zero,
                panEnd = Vector2.zero,
                zoomStart = 1.0f,
                zoomEnd = 1.04f
            });
        }

        private Sprite LoadSpriteAsset(string path)
        {
#if UNITY_EDITOR
            string fullPath = "Assets/" + path + ".jpg";
            Texture2D tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(fullPath);
            if (tex != null)
            {
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
#endif
            return Resources.Load<Sprite>(path);
        }

        private void BuildRuntimeUI()
        {
            var existing = GetComponentInChildren<Canvas>();
            if (existing != null)
            {
                cutsceneCanvas = existing;
                cutsceneCanvasGroup = cutsceneCanvas.GetComponent<CanvasGroup>();
                return;
            }

            GameObject canvasGo = new GameObject("HistoricalCutsceneCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            canvasGo.transform.SetParent(transform, false);

            cutsceneCanvas = canvasGo.GetComponent<Canvas>();
            cutsceneCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cutsceneCanvas.sortingOrder = 999;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            cutsceneCanvasGroup = canvasGo.GetComponent<CanvasGroup>();
            cutsceneCanvasGroup.alpha = 0f;
            cutsceneCanvasGroup.blocksRaycasts = false;

            // Pure black background
            GameObject bgGo = new GameObject("SolidBlackBG", typeof(Image));
            bgGo.transform.SetParent(canvasGo.transform, false);
            var bgImg = bgGo.GetComponent<Image>();
            bgImg.color = Color.black;
            var bgRect = bgGo.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            // Slide Viewport
            GameObject viewGo = new GameObject("PhotoViewport", typeof(RectTransform));
            viewGo.transform.SetParent(canvasGo.transform, false);
            var viewRect = viewGo.GetComponent<RectTransform>();
            viewRect.anchorMin = new Vector2(0.08f, 0.16f);
            viewRect.anchorMax = new Vector2(0.92f, 0.88f);
            viewRect.sizeDelta = Vector2.zero;

            // Slide Images A & B for Crossfading
            slideImageA = CreateSlideImage(viewGo.transform, "Photo_A", out groupA);
            slideImageB = CreateSlideImage(viewGo.transform, "Photo_B", out groupB);

            // Cinematic Top and Bottom Letterbox Bars
            CreateLetterboxBar(canvasGo.transform, "Letterbox_Top", new Vector2(0, 0.88f), Vector2.one);
            CreateLetterboxBar(canvasGo.transform, "Letterbox_Bottom", Vector2.zero, new Vector2(1, 0.16f));

            // Subtitle Caption
            GameObject captionGo = new GameObject("HistoricalCaption", typeof(Text), typeof(CanvasGroup));
            captionGo.transform.SetParent(canvasGo.transform, false);
            var capRect = captionGo.GetComponent<RectTransform>();
            capRect.anchorMin = new Vector2(0.12f, 0.02f);
            capRect.anchorMax = new Vector2(0.88f, 0.14f);
            capRect.sizeDelta = Vector2.zero;

            captionText = captionGo.GetComponent<Text>();
            captionText.alignment = TextAnchor.MiddleCenter;
            captionText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            captionText.fontSize = 24;
            captionText.color = new Color(0.95f, 0.94f, 0.90f, 0.96f);
            captionText.lineSpacing = 1.35f;
            captionGroup = captionGo.GetComponent<CanvasGroup>();

            // Skip Prompt (Planet of Lana Style: [ESC / ENTER] GIỮ ĐỂ BỎ QUA)
            GameObject skipGo = new GameObject("SkipPrompt", typeof(Text));
            skipGo.transform.SetParent(canvasGo.transform, false);
            var skipRect = skipGo.GetComponent<RectTransform>();
            skipRect.anchorMin = new Vector2(0.78f, 0.90f);
            skipRect.anchorMax = new Vector2(0.97f, 0.96f);
            skipRect.sizeDelta = Vector2.zero;

            skipPromptText = skipGo.GetComponent<Text>();
            skipPromptText.text = "GIỮ [ESC / SPACE] BỎ QUA";
            skipPromptText.alignment = TextAnchor.MiddleRight;
            skipPromptText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            skipPromptText.fontSize = 15;
            skipPromptText.color = new Color(0.85f, 0.85f, 0.85f, 0.65f);
        }

        private Image CreateSlideImage(Transform parent, string name, out CanvasGroup group)
        {
            GameObject go = new GameObject(name, typeof(Image), typeof(CanvasGroup));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.preserveAspect = true;
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            group = go.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            return img;
        }

        private void CreateLetterboxBar(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject bar = new GameObject(name, typeof(Image));
            bar.transform.SetParent(parent, false);
            var img = bar.GetComponent<Image>();
            img.color = Color.black;
            var rect = bar.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = Vector2.zero;
        }

        private void PlayAudio()
        {
            if (historicalBgmClip != null)
            {
                if (bgmSource == null) bgmSource = gameObject.AddComponent<AudioSource>();
                bgmSource.clip = historicalBgmClip;
                bgmSource.loop = true;
                bgmSource.volume = bgmVolume;
                bgmSource.Play();
            }
        }
    }
}
