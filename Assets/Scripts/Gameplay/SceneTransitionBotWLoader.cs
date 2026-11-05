using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Standalone Transition & Loading Screen System replicating the exact
    /// post-Start-Menu BotW-style Loading Screen from PlanetOfLanaStartMenuSystem:
    /// 1. Smoothly fades gameplay view + audio to a pure black screen (#020205).
    /// 2. Reveals the split Cream (#FAF7ED) / Dark Slate (#0C0E12) BotW Loading Screen
    ///    with hopping silhouette of Cậu Bé An, sailing 1954 Transport Ship silhouette,
    ///    6 pulsing Sheikah-style dots, and interactive Vietnamese gameplay/lore tips.
    /// 3. Loads the target scene asynchronously (e.g., "CaMau_1954_Village"), fades back
    ///    to black for seamless activation, and gently lifts the black curtain in the new 3D scene.
    /// </summary>
    [DisallowMultipleComponent]
    public class SceneTransitionBotWLoader : MonoBehaviour
    {
        public static SceneTransitionBotWLoader Instance { get; private set; }

        [Header("Target Scene & Timing")]
        [SerializeField] private string defaultTargetScene = "CaMau_1954_Village";
        [SerializeField] private string loadingCornerLabel = "ĐANG TẢI LÀNG CÀ MAU 1954...";
        [SerializeField] private float fadeToBlackDuration = 0.85f;
        [SerializeField] private float revealLoadingDuration = 0.45f;
        [SerializeField] private float minLoadingDuration = 3.20f;
        [SerializeField] private float exitToNewSceneFadeDuration = 0.60f;
        [SerializeField] private float newSceneFadeInDuration = 1.15f;

        [Header("Verified Project UI Sprites (Same as Start Menu)")]
        [SerializeField] private Sprite softDotSprite;
        [SerializeField] private Sprite boySilhouetteSprite;
        [SerializeField] private Sprite shipSilhouetteSprite;

        private Canvas rootCanvas;
        private CanvasGroup blackCurtainGroup;
        private CanvasGroup botwLoadingGroup;
        private RectTransform botwAnRect;
        private RectTransform botwShipRect;
        private Text botwTipCategoryText;
        private Text botwTipBodyText;
        private CanvasGroup botwTipTextGroup;
        private Text botwLoadingLabelText;
        private readonly List<Image> botwSheikahDots = new List<Image>();

        private bool isTransitioning = false;
        private bool isAnimatingLoading = false;
        private float animTimer = 0f;
        private float tipCycleTimer = 0f;
        private int currentTipIndex = 0;

        private struct LoadingTip
        {
            public string Category;
            public string Body;
            public LoadingTip(string cat, string body) { Category = cat; Body = body; }
        }

        private static readonly LoadingTip[] BotWTips = new LoadingTip[]
        {
            new LoadingTip(
                "XÓM RẠCH GỐC — CÀ MAU NĂM 1954",
                "Sau chặng đường băng rừng vượt ruộng, An đã trở về xóm nhỏ ven sông Cà Mau. Hãy dùng [W][A][S][D] để tự do dạo bước trong không gian 3D và xoay chuột quan sát làng quê."
            ),
            new LoadingTip(
                "TƯƠNG TÁC CÙNG BÀ CON & KỶ VẬT",
                "Khi lại gần Mẹ, bà con hàng xóm hay các vật phẩm kỷ niệm như Gói Đất Đỏ Cà Mau, biểu tượng phím [E] tối giản sẽ hiện lên để bạn trò chuyện hoặc tương tác."
            ),
            new LoadingTip(
                "DI CHUYỂN LINH HOẠT & ẨN NẤP",
                "Giữ [Shift] để chạy nhanh trên đường làng, nhấn [Space] để nhảy qua bậc thềm hay cầu khỉ, và nhấn [C] để cúi mình ẩn nấp trong các bụi cây ven rạch."
            ),
            new LoadingTip(
                "ĐẨY THÙNG GỖ & MANG VẬT PHẨM",
                "An có thể nhấn [E] bên cạnh các kiện hàng gỗ trên bến xuồng để đẩy/kéo tạo điểm tựa, hoặc nhặt rổ tre, cặp lồng cơm mang giúp bà con trong xóm."
            ),
            new LoadingTip(
                "LƯU TIẾN TRÌNH & TÙY CHỈNH",
                "Nhấn [Esc] bất cứ lúc nào để tạm dừng trò chơi, điều chỉnh âm lượng tiếng gió đồng quê, đổi phím điều khiển hoặc lưu lại hành trình hiện tại."
            )
        };

        public bool IsTransitioning => isTransitioning;

        public void ConfigureSprites(Sprite softDot, Sprite boySil, Sprite shipSil, string targetScene = "CaMau_1954_Village", string cornerLabel = "ĐANG TẢI LÀNG CÀ MAU 1954...")
        {
            softDotSprite = softDot;
            boySilhouetteSprite = boySil;
            shipSilhouetteSprite = shipSil;
            if (!string.IsNullOrEmpty(targetScene)) defaultTargetScene = targetScene;
            if (!string.IsNullOrEmpty(cornerLabel)) loadingCornerLabel = cornerLabel;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                if (Instance.isTransitioning)
                {
                    Destroy(gameObject);
                    return;
                }
            }
            Instance = this;
            EnsureUI();
        }

        private void Update()
        {
            if (!isAnimatingLoading) return;

            float dt = Time.unscaledDeltaTime;
            animTimer += dt;
            UpdateBotWLoadingVisuals(dt);
        }

        /// <summary>
        /// Static entry point to trigger Fade-to-Black -> Start Menu BotW Loading Screen -> Async Scene Load.
        /// </summary>
        public static void LoadSceneWithBotWScreen(string sceneName, string customCornerLabel = null)
        {
            SceneTransitionBotWLoader loader = Instance;
            if (loader == null)
            {
                loader = Object.FindAnyObjectByType<SceneTransitionBotWLoader>();
            }
            if (loader == null)
            {
                var go = new GameObject("SceneTransitionBotWLoader");
                loader = go.AddComponent<SceneTransitionBotWLoader>();
            }

            loader.BeginTransition(sceneName, customCornerLabel);
        }

        public void BeginTransition(string targetSceneName = null, string customCornerLabel = null)
        {
            if (isTransitioning) return;

            string sceneToLoad = string.IsNullOrEmpty(targetSceneName) ? defaultTargetScene : targetSceneName;
            EnsureUI();

            if (!string.IsNullOrEmpty(customCornerLabel) && botwLoadingLabelText != null)
            {
                botwLoadingLabelText.text = customCornerLabel;
            }

            // Detach from any scene-local additive group so DontDestroyOnLoad preserves this loader across scenes
            transform.SetParent(null, false);
            DontDestroyOnLoad(gameObject);

            isTransitioning = true;
            Time.timeScale = 1.0f;
            StartCoroutine(TransitionSequenceRoutine(sceneToLoad));
        }

        public void SetEditorPreviewVisible(bool visible, float sampleTime = 8.8f)
        {
            EnsureUI();
            if (rootCanvas != null) rootCanvas.gameObject.SetActive(visible);
            if (!visible)
            {
                if (blackCurtainGroup != null) blackCurtainGroup.alpha = 0f;
                if (botwLoadingGroup != null)
                {
                    botwLoadingGroup.alpha = 0f;
                    botwLoadingGroup.gameObject.SetActive(false);
                }
                return;
            }

            if (blackCurtainGroup != null) blackCurtainGroup.alpha = 0f;
            if (botwLoadingGroup != null)
            {
                botwLoadingGroup.gameObject.SetActive(true);
                botwLoadingGroup.alpha = 1f;
            }
            currentTipIndex = 0;
            ApplyTipTextImmediate();
            animTimer = sampleTime;
            UpdateBotWLoadingVisuals(0.016f);
        }

        private IEnumerator TransitionSequenceRoutine(string sceneToLoad)
        {
            rootCanvas.gameObject.SetActive(true);

            // Cache active scene audio sources to fade them out smoothly with the black screen
            AudioSource[] activeAudio = Object.FindObjectsByType<AudioSource>();
            float[] startVolumes = new float[activeAudio.Length];
            for (int i = 0; i < activeAudio.Length; i++)
            {
                startVolumes[i] = activeAudio[i] != null ? activeAudio[i].volume : 0f;
            }

            if (botwLoadingGroup != null)
            {
                botwLoadingGroup.alpha = 0f;
                botwLoadingGroup.gameObject.SetActive(false);
            }
            if (blackCurtainGroup != null)
            {
                blackCurtainGroup.alpha = 0f;
                blackCurtainGroup.blocksRaycasts = true;
            }

            // 1. Fade current Overhaul scene to pure black screen
            float t = 0f;
            float fadeDur = Mathf.Max(0.15f, fadeToBlackDuration);
            while (t < fadeDur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / fadeDur));
                if (blackCurtainGroup != null) blackCurtainGroup.alpha = k;

                for (int i = 0; i < activeAudio.Length; i++)
                {
                    if (activeAudio[i] != null)
                    {
                        activeAudio[i].volume = Mathf.Lerp(startVolumes[i], 0f, k);
                    }
                }
                yield return null;
            }
            if (blackCurtainGroup != null) blackCurtainGroup.alpha = 1f;

            // Brief cinematic beat in black before revealing the BotW Loading Screen
            yield return new WaitForSecondsRealtime(0.18f);

            // 2. Activate BotW Loading Screen underneath black curtain & reveal it
            currentTipIndex = Random.Range(0, BotWTips.Length);
            tipCycleTimer = 0f;
            animTimer = 7.8f; // Start with An & 1954 Ship centered along the horizon track
            ApplyTipTextImmediate();

            if (botwLoadingGroup != null)
            {
                botwLoadingGroup.gameObject.SetActive(true);
                botwLoadingGroup.alpha = 1f;
            }
            isAnimatingLoading = true;

            // Start asynchronous load of 3D Ca Mau 1954 Village scene
            float loadStartTime = Time.unscaledTime;
            AsyncOperation asyncOp = SceneManager.LoadSceneAsync(sceneToLoad);
            if (asyncOp != null)
            {
                asyncOp.allowSceneActivation = false;
            }

            // Lift black curtain to reveal the BotW Loading Screen
            t = 0f;
            float revDur = Mathf.Max(0.15f, revealLoadingDuration);
            while (t < revDur)
            {
                t += Time.unscaledDeltaTime;
                if (blackCurtainGroup != null)
                {
                    blackCurtainGroup.alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / revDur));
                }
                yield return null;
            }
            if (blackCurtainGroup != null) blackCurtainGroup.alpha = 0f;

            // 3. Keep animating BotW silhouettes, Sheikah dots, and tips while loading
            if (asyncOp != null)
            {
                while (asyncOp.progress < 0.9f || (Time.unscaledTime - loadStartTime) < minLoadingDuration)
                {
                    yield return null;
                }
            }
            else
            {
                Debug.LogWarning($"[SceneTransitionBotWLoader] Async load returned null for '{sceneToLoad}', waiting min duration then synchronous load.");
                yield return new WaitForSecondsRealtime(minLoadingDuration);
            }

            // 4. Smoothly fade BotW Loading Screen to black before activating 3D scene
            t = 0f;
            float outDur = Mathf.Max(0.2f, exitToNewSceneFadeDuration);
            while (t < outDur)
            {
                t += Time.unscaledDeltaTime;
                if (blackCurtainGroup != null)
                {
                    blackCurtainGroup.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / outDur));
                }
                yield return null;
            }
            if (blackCurtainGroup != null) blackCurtainGroup.alpha = 1f;

            isAnimatingLoading = false;
            if (botwLoadingGroup != null)
            {
                botwLoadingGroup.alpha = 0f;
                botwLoadingGroup.gameObject.SetActive(false);
            }

            Time.timeScale = 1.0f;
            if (asyncOp != null)
            {
                asyncOp.allowSceneActivation = true;
                while (!asyncOp.isDone)
                {
                    yield return null;
                }
            }
            else
            {
                SceneManager.LoadScene(sceneToLoad);
                yield return null;
            }

            // Let the 3D CaMau_1954_Village scene settle camera & lighting for 2 frames
            yield return null;
            yield return null;

            // 5. Gently lift the black curtain to reveal CaMau_1954_Village 3D vista
            if (blackCurtainGroup != null) blackCurtainGroup.blocksRaycasts = false;
            t = 0f;
            float inDur = Mathf.Max(0.25f, newSceneFadeInDuration);
            while (t < inDur)
            {
                t += Time.unscaledDeltaTime;
                if (blackCurtainGroup != null)
                {
                    blackCurtainGroup.alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / inDur));
                }
                yield return null;
            }

            isTransitioning = false;
            if (Instance == this) Instance = null;
            Destroy(gameObject);
        }

        private void UpdateBotWLoadingVisuals(float dt)
        {
            tipCycleTimer += dt;
            if (tipCycleTimer >= 6.0f || IsNextTipKeyPressed())
            {
                NextTip();
            }

            // 1. Hopping Silhouette of Cậu Bé An along the Horizon Line (matches PlanetOfLanaStartMenuSystem)
            if (botwAnRect != null)
            {
                float screenW = 1920f;
                float runSpeed = 125f;
                float trackX = ((animTimer * runSpeed) % (screenW + 500f)) - (screenW / 2f + 250f);

                float hop = Mathf.Abs(Mathf.Sin(animTimer * 8.5f)) * 18f;
                botwAnRect.anchoredPosition = new Vector2(trackX, hop + 2f);

                float stride = Mathf.Sin(animTimer * 8.5f) * 0.08f;
                botwAnRect.localScale = new Vector3(1f - stride, 1f + stride, 1f);
            }

            // 2. Sailing Silhouette of the 1954 Transport Ship behind An
            if (botwShipRect != null && botwAnRect != null)
            {
                float shipX = botwAnRect.anchoredPosition.x - 280f;
                float wave = Mathf.Sin(animTimer * 2.8f) * 6.0f;
                float pitch = Mathf.Sin(animTimer * 2.8f + 0.8f) * 2.5f;

                botwShipRect.anchoredPosition = new Vector2(shipX, wave - 2f);
                botwShipRect.localRotation = Quaternion.Euler(0f, 0f, pitch);
            }

            // 3. Pulse 6 Sheikah-Style Dots in Bottom-Right
            for (int i = 0; i < botwSheikahDots.Count; i++)
            {
                var dot = botwSheikahDots[i];
                if (dot != null)
                {
                    float angle = (float)i / botwSheikahDots.Count * Mathf.PI * 2f;
                    float wave = (Mathf.Sin(animTimer * 4.2f - angle) + 1f) * 0.5f;
                    dot.color = new Color(0.99f, 0.95f, 0.88f, Mathf.Lerp(0.20f, 0.95f, wave));
                }
            }

            if (botwTipTextGroup != null)
            {
                botwTipTextGroup.alpha = Mathf.MoveTowards(botwTipTextGroup.alpha, 1f, dt * 3.5f);
            }
        }

        private void NextTip()
        {
            tipCycleTimer = 0f;
            currentTipIndex = (currentTipIndex + 1) % BotWTips.Length;
            if (botwTipTextGroup != null) botwTipTextGroup.alpha = 0.15f;
            ApplyTipTextImmediate();
        }

        private void ApplyTipTextImmediate()
        {
            if (BotWTips.Length == 0) return;
            var tip = BotWTips[Mathf.Clamp(currentTipIndex, 0, BotWTips.Length - 1)];
            if (botwTipCategoryText != null) botwTipCategoryText.text = tip.Category;
            if (botwTipBodyText != null) botwTipBodyText.text = tip.Body;
        }

        public void EnsureUI()
        {
            if (rootCanvas != null) return;

            var existing = transform.Find("Canvas_BotW_SceneTransition");
            if (existing != null)
            {
                if (Application.isPlaying) Destroy(existing.gameObject);
                else DestroyImmediate(existing.gameObject);
            }

            var canvasGO = new GameObject("Canvas_BotW_SceneTransition");
            canvasGO.transform.SetParent(transform, false);
            rootCanvas = canvasGO.AddComponent<Canvas>();
            rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            rootCanvas.sortingOrder = 990;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            Font font = Font.CreateDynamicFontFromOSFont("Segoe UI", 36);
            if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 36);
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // 1. BotW Split Loading Panel (Built first so Black Curtain sits on top for clean fade-in/out)
                        if (boySilhouetteSprite == null)
            {
#if UNITY_EDITOR
                boySilhouetteSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/PierScene/running_boy_sprite.png");
#endif
            }
            if (shipSilhouetteSprite == null)
            {
#if UNITY_EDITOR
                shipSilhouetteSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Canh1_Sprites/TransportShip_1954_Card.png")
                    ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Environment/T_Camau_Boat_Hero.png");
#endif
            }
            if (softDotSprite == null)
            {
#if UNITY_EDITOR
                softDotSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/T_Loading_SoftDot.png");
#endif
            }

            BuildBotWLoadingPanel(canvasGO.transform, font);

            // 2. Topmost Pure Black Curtain (#020205)
            var curtainRect = CreateRect("Black_Boot_Curtain", canvasGO.transform);
            StretchFull(curtainRect);
            var curtainImg = curtainRect.gameObject.AddComponent<Image>();
            curtainImg.color = new Color(0.01f, 0.01f, 0.02f, 1.0f);
            curtainImg.raycastTarget = true;
            blackCurtainGroup = curtainRect.gameObject.AddComponent<CanvasGroup>();
            blackCurtainGroup.alpha = 0f;
            blackCurtainGroup.blocksRaycasts = false;
            blackCurtainGroup.interactable = false;
        }

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
            topImg.color = new Color(0.98f, 0.96f, 0.91f, 1.0f);

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

            // Horizon Silhouettes Track
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
            anGO.anchoredPosition = Vector2.zero;
            anGO.sizeDelta = new Vector2(80f, 80f);
            botwAnRect = anGO;
            var anImg = anGO.gameObject.AddComponent<Image>();
            anImg.sprite = boySilhouetteSprite;
            anImg.preserveAspect = true;
            anImg.color = new Color(0.08f, 0.09f, 0.12f, 1.0f);

            // 2. Sailing Silhouette of 1954 Transport Ship
            var shipGO = CreateRect("Silhouette_1954_Ship", trackRect);
            shipGO.anchorMin = shipGO.anchorMax = new Vector2(0.5f, 0f);
            shipGO.pivot = new Vector2(0.5f, 0.2f);
            shipGO.anchoredPosition = new Vector2(-280f, 0f);
            shipGO.sizeDelta = new Vector2(170f, 100f);
            botwShipRect = shipGO;
            var shipImg = shipGO.gameObject.AddComponent<Image>();
            shipImg.sprite = shipSilhouetteSprite;
            shipImg.preserveAspect = true;
            shipImg.color = new Color(0.12f, 0.14f, 0.18f, 1.0f);

            // Tips & Tricks Cluster (Lower-Left)
            var tipsContainer = CreateRect("Tips_And_Tricks_Cluster", panelRect);
            tipsContainer.anchorMin = new Vector2(0f, 0f);
            tipsContainer.anchorMax = new Vector2(0f, 0.50f);
            tipsContainer.pivot = new Vector2(0f, 1f);
            tipsContainer.anchoredPosition = new Vector2(100f, -40f);
            tipsContainer.sizeDelta = new Vector2(780f, 220f);

            var tipGroupGO = CreateRect("TipContentGroup", tipsContainer);
            StretchFull(tipGroupGO);
            botwTipTextGroup = tipGroupGO.gameObject.AddComponent<CanvasGroup>();

            var catRect = CreateRect("Tip_Category", tipGroupGO);
            catRect.anchorMin = catRect.anchorMax = new Vector2(0f, 1f);
            catRect.pivot = new Vector2(0f, 1f);
            catRect.anchoredPosition = Vector2.zero;
            catRect.sizeDelta = new Vector2(780f, 34f);
            botwTipCategoryText = catRect.gameObject.AddComponent<Text>();
            botwTipCategoryText.font = font;
            botwTipCategoryText.fontSize = 20;
            botwTipCategoryText.fontStyle = FontStyle.Bold;
            botwTipCategoryText.alignment = TextAnchor.MiddleLeft;
            botwTipCategoryText.color = new Color(0.99f, 0.95f, 0.88f, 0.98f);
            botwTipCategoryText.text = BotWTips[0].Category;

            var bodyRect = CreateRect("Tip_Body", tipGroupGO);
            bodyRect.anchorMin = bodyRect.anchorMax = new Vector2(0f, 1f);
            bodyRect.pivot = new Vector2(0f, 1f);
            bodyRect.anchoredPosition = new Vector2(0f, -40f);
            bodyRect.sizeDelta = new Vector2(760f, 96f);
            botwTipBodyText = bodyRect.gameObject.AddComponent<Text>();
            botwTipBodyText.font = font;
            botwTipBodyText.fontSize = 15;
            botwTipBodyText.lineSpacing = 1.35f;
            botwTipBodyText.alignment = TextAnchor.UpperLeft;
            botwTipBodyText.color = new Color(0.85f, 0.88f, 0.92f, 0.85f);
            botwTipBodyText.text = BotWTips[0].Body;

            var hintRect = CreateRect("Tip_Hint", tipsContainer);
            hintRect.anchorMin = hintRect.anchorMax = new Vector2(0f, 0f);
            hintRect.pivot = new Vector2(0f, 0f);
            hintRect.anchoredPosition = new Vector2(0f, 20f);
            hintRect.sizeDelta = new Vector2(540f, 26f);
            var hintTxt = hintRect.gameObject.AddComponent<Text>();
            hintTxt.font = font;
            hintTxt.fontSize = 12;
            hintTxt.alignment = TextAnchor.MiddleLeft;
            hintTxt.color = new Color(0.70f, 0.74f, 0.82f, 0.60f);
            hintTxt.text = "Nhấn [Space] / [Enter] để đổi mẹo   •   Mẹo tự chuyển đổi sau 6s";

            // Bottom-Right Corner Sheikah Pulsing Dots & Label
            var brCluster = CreateRect("BotW_BottomRight_Cluster", panelRect);
            brCluster.anchorMin = brCluster.anchorMax = new Vector2(1.0f, 0f);
            brCluster.pivot = new Vector2(1.0f, 0f);
            brCluster.anchoredPosition = new Vector2(-100f, 50f);
            brCluster.sizeDelta = new Vector2(340f, 120f);

            var dotsRect = CreateRect("Sheikah_Dots_Circle", brCluster);
            dotsRect.anchorMin = dotsRect.anchorMax = new Vector2(1.0f, 0.25f);
            dotsRect.pivot = new Vector2(0.5f, 0.5f);
            dotsRect.anchoredPosition = new Vector2(-25f, 0f);
            dotsRect.sizeDelta = new Vector2(36f, 36f);

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
            brTextRect.sizeDelta = new Vector2(280f, 26f);
            botwLoadingLabelText = brTextRect.gameObject.AddComponent<Text>();
            botwLoadingLabelText.font = font;
            botwLoadingLabelText.fontSize = 13;
            botwLoadingLabelText.alignment = TextAnchor.MiddleRight;
            botwLoadingLabelText.color = new Color(0.92f, 0.88f, 0.78f, 0.80f);
            botwLoadingLabelText.text = loadingCornerLabel;
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

        private static bool IsNextTipKeyPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
                return true;
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                return true;
#else
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
                return true;
#endif
            return false;
        }
    }
}
