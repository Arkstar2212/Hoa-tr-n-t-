using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HoaVaMau.Test3D;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// 3D Planet-of-Lana Minimalist Diegetic UI Guide & Village Arrival Vignette Controller
    /// for "CaMau_1954_Village.unity", ported directly from Overhaul's PlanetOfLanaTutorialController,
    /// PlanetOfLanaPromptSystem, and VillageArrivalCutscene:
    /// 1. Opening 21:9 Cinematic Letterbox + Vietnamese Location Title Stamp ("XÓM RẠCH GỐC - CÀ MAU • 1954").
    /// 2. Floating Monochromatic Translucent Keycap Glyphs projected onto 3D World Targets:
    ///    - [W][A][S][D] above Bé An on arrival until walked
    ///    - [SHIFT] sprint hint -> [SPACE] jump hint -> [C] stealth bush crouch hint
    ///    - Contextual [E] floating glyph above nearby InteractableObject3D, InteractableCrate3D, & PortableItem3D
    ///      (and [E] / [Q] while carrying an item or pushing a crate).
    /// 3. Clean 1920x1080 Canvas Subtitle / Narrative Toast Banner (replaces raw debug OnGUI boxes).
    /// 4. Subtle Minimalist Control Bar at top-right (auto-fades after 9s, toggleable anytime via [H]).
    /// </summary>
    [DisallowMultipleComponent]
    public class CaMau3DUIGuideController : MonoBehaviour
    {
        public static CaMau3DUIGuideController Instance { get; private set; }

        [Header("Arrival Title Card")]
        [SerializeField] private bool showOpeningArrivalBanner = true;
        [SerializeField] private string villageTitle = "XÓM RẠCH GỐC  •  CÀ MAU 1954";
        [SerializeField] private string villageSubtitle = "Bến quê thanh bình";

        private Canvas uiCanvas;
        private Font uiFont;

        // 1. Floating World-Space Translucent Keycap Cluster (Planet of Lana signature)
        private CanvasGroup glyphCanvasGroup;
        private RectTransform glyphClusterRect;
        private Text glyphSubLabelText;
        private readonly List<GameObject> activeKeyCapObjects = new List<GameObject>();
        private string currentSignature = "";
        private Transform currentWorldTarget;
        private Vector3 currentWorldOffset = new Vector3(0f, 1.95f, 0f);

        // 2. Opening Cinematic Letterbox & Location Stamp
        private RectTransform topLetterboxBar;
        private RectTransform bottomLetterboxBar;
        private CanvasGroup arrivalTitleGroup;
        private float arrivalTimer = 0f;
        private const float ArrivalTotalDuration = 5.4f;

        // 3. Narrative Inspection Toast / Subtitle Banner (replaces InteractionSystem3D.OnGUI)
        private CanvasGroup narrativeToastGroup;
        private Text narrativeTitleText;
        private Text narrativeBodyText;
        private float narrativeToastTimer = 0f;

        // 4. Minimalist Control Strip ([H] toggle)
        private CanvasGroup controlStripGroup;
        private Text stealthStatusBadgeText;
        private bool controlStripPinned = false;
        private float controlStripAutoHideTimer = 10.0f;

        // Tutorial progression state in 3D Village
        private Player3DController player3D;
        private InteractableObject3D[] cachedInteractables3D;
        private InteractableCrate3D[] cachedCrates3D;
        private PortableItem3D[] cachedPortableItems3D;
        private BushHideZone[] cachedBushes3D;

        private float walkedTimer = 0f;
        private bool moveGuideDone = false;
        private float sprintShowTimer = 0f;
        private bool sprintGuideDone = false;
        private float jumpShowTimer = 0f;
        private bool jumpGuideDone = false;

        private static readonly Color GlyphSingleColor = new Color(1.0f, 1.0f, 1.0f, 0.86f);
        private static readonly Color GlyphBorderColor = new Color(1.0f, 1.0f, 1.0f, 0.56f);
        private static readonly Color GlyphFillColor   = new Color(0.06f, 0.09f, 0.14f, 0.28f);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Build3DUIGuideCanvas();
        }

        private void Start()
        {
            RefreshSceneTargets();
        }

        public void RefreshSceneTargets()
        {
            player3D = Object.FindAnyObjectByType<Player3DController>();
            cachedInteractables3D = Object.FindObjectsByType<InteractableObject3D>();
            cachedCrates3D = Object.FindObjectsByType<InteractableCrate3D>();
            cachedPortableItems3D = Object.FindObjectsByType<PortableItem3D>();
            cachedBushes3D = Object.FindObjectsByType<BushHideZone>();
        }

        public static void ShowNarrativeToast(string title, string message, float duration = 3.8f)
        {
            if (Instance == null) return;
            if (Instance.narrativeTitleText != null)
                Instance.narrativeTitleText.text = string.IsNullOrEmpty(title) ? "KHÁM PHÁ LÀNG QUÊ" : title.ToUpperInvariant();
            if (Instance.narrativeBodyText != null)
                Instance.narrativeBodyText.text = message;
            Instance.narrativeToastTimer = duration;
        }

        public void SetEditorPreviewState()
        {
            if (uiCanvas == null) Build3DUIGuideCanvas();
            RefreshSceneTargets();
            if (arrivalTitleGroup != null) arrivalTitleGroup.alpha = 0.96f;
            if (controlStripGroup != null) controlStripGroup.alpha = 0.90f;
            RebuildKeyCapGlyphs(new[] { "W", "A", "S", "D" }, "DI CHUYỂN");
            if (glyphCanvasGroup != null) glyphCanvasGroup.alpha = 0.92f;
            if (glyphClusterRect != null)
            {
                glyphClusterRect.anchorMin = glyphClusterRect.anchorMax = new Vector2(0.5f, 0.62f);
                glyphClusterRect.anchoredPosition = Vector2.zero;
            }
        }

        private void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;

            if (player3D == null)
            {
                player3D = Object.FindAnyObjectByType<Player3DController>();
                if (player3D == null) return;
            }

            bool isPaused = PauseMenuController.Instance != null && PauseMenuController.Instance.IsPaused;
            if (isPaused)
            {
                if (glyphCanvasGroup != null) glyphCanvasGroup.alpha = 0f;
                return;
            }

            UpdateArrivalVignette(dt);
            UpdateNarrativeToast(dt);
            UpdateControlStrip(dt);

            EvaluateContextual3DGlyph(out string[] keys, out string subLabel, out Transform anchorTarget, out Vector3 worldOffset);

            if (keys == null || keys.Length == 0)
            {
                FadeGlyphAlpha(0f);
                currentSignature = "";
            }
            else
            {
                string sig = string.Join("|", keys) + "::" + (subLabel ?? "");
                if (sig != currentSignature)
                {
                    currentSignature = sig;
                    RebuildKeyCapGlyphs(keys, subLabel);
                }

                currentWorldTarget = anchorTarget != null ? anchorTarget : player3D.transform;
                currentWorldOffset = worldOffset;
                ProjectGlyphToScreen();
                FadeGlyphAlpha(1f);
            }
        }

        private void EvaluateContextual3DGlyph(out string[] keys, out string subLabel, out Transform anchor, out Vector3 offset)
        {
            keys = null;
            subLabel = "";
            anchor = player3D.transform;
            offset = new Vector3(0f, 1.95f, 0f);

            if (!player3D.enabled) return;

            string eKey = KeybindingManager.GetKeyDisplayName(KeybindingManager.GetKey(GameAction.Interact));
            string cKey = KeybindingManager.GetKeyDisplayName(KeybindingManager.GetKey(GameAction.Crouch));
            string jumpKey = KeybindingManager.GetKeyDisplayName(KeybindingManager.GetKey(GameAction.Jump));

            // Priority 1: Currently pushing a 3D Crate or carrying a 3D Portable Item
            if (player3D.GrabbedCrate != null)
            {
                keys = new[] { "W", "A", "S", "D", eKey };
                subLabel = "ĐẨY / THẢ THÙNG GỖ";
                anchor = player3D.GrabbedCrate.transform;
                offset = new Vector3(0f, 1.35f, 0f);
                return;
            }

            if (player3D.CarriedItem != null)
            {
                keys = new[] { eKey, "Q" };
                subLabel = "ĐẶT XUỐNG / NÉM NHẸ";
                anchor = player3D.transform;
                offset = new Vector3(0f, 2.05f, 0f);
                return;
            }

            // Priority 2: Nearby 3D Portable Item, Crate, or Story Trigger (if no NPC already owns [E])
            if (!VillageNPCInteractable.HasActivePromptOwner)
            {
                // Check PortableItem3D
                if (cachedPortableItems3D != null)
                {
                    float bestDist = float.MaxValue;
                    PortableItem3D bestItem = null;
                    for (int i = 0; i < cachedPortableItems3D.Length; i++)
                    {
                        var item = cachedPortableItems3D[i];
                        if (item == null || !item.gameObject.activeInHierarchy || !item.CanPickUp(player3D.transform)) continue;
                        float d = Vector3.Distance(player3D.transform.position, item.transform.position);
                        if (d < bestDist) { bestDist = d; bestItem = item; }
                    }
                    if (bestItem != null)
                    {
                        keys = new[] { eKey };
                        subLabel = bestItem.ItemTitle;
                        anchor = bestItem.transform;
                        offset = new Vector3(0f, 1.05f, 0f);
                        return;
                    }
                }

                // Check InteractableCrate3D
                if (cachedCrates3D != null)
                {
                    float bestDist = float.MaxValue;
                    InteractableCrate3D bestCrate = null;
                    for (int i = 0; i < cachedCrates3D.Length; i++)
                    {
                        var crate = cachedCrates3D[i];
                        if (crate == null || !crate.gameObject.activeInHierarchy || !crate.CanInteract(player3D.transform)) continue;
                        float d = Vector3.Distance(player3D.transform.position, crate.transform.position);
                        if (d < bestDist) { bestDist = d; bestCrate = crate; }
                    }
                    if (bestCrate != null)
                    {
                        keys = new[] { eKey };
                        subLabel = bestCrate.DisplayName;
                        anchor = bestCrate.transform;
                        offset = new Vector3(0f, 1.25f, 0f);
                        return;
                    }
                }

                // Check InteractableObject3D (Soil Pouch, Well Water, River Bench, Orchard Gourd, etc.)
                if (cachedInteractables3D != null)
                {
                    float bestDist = float.MaxValue;
                    InteractableObject3D bestObj = null;
                    for (int i = 0; i < cachedInteractables3D.Length; i++)
                    {
                        var obj = cachedInteractables3D[i];
                        if (obj == null || !obj.gameObject.activeInHierarchy || obj.isTriggerZone) continue;
                        float d = Vector3.Distance(player3D.transform.position, obj.transform.position);
                        if (d <= obj.interactionRadius && d < bestDist)
                        {
                            bestDist = d;
                            bestObj = obj;
                        }
                    }
                    if (bestObj != null)
                    {
                        keys = new[] { eKey };
                        subLabel = bestObj.objectTitle;
                        anchor = bestObj.transform;
                        offset = new Vector3(0f, 1.35f, 0f);
                        return;
                    }
                }
            }
            else
            {
                // VillageNPCInteractable is already rendering its own [E] glyph above the NPC
                return;
            }

            // Priority 3: Near a Stealth BushHideZone -> show [C] Crouch/Hide guide
            if (cachedBushes3D != null && !player3D.IsCrouching)
            {
                for (int i = 0; i < cachedBushes3D.Length; i++)
                {
                    var bush = cachedBushes3D[i];
                    if (bush == null) continue;
                    if (Vector3.Distance(player3D.transform.position, bush.transform.position) <= 2.5f)
                    {
                        keys = new[] { cKey };
                        subLabel = "CÚI NẤP BỤI CÂY";
                        anchor = bush.transform;
                        offset = new Vector3(0f, 1.45f, 0f);
                        return;
                    }
                }
            }

            // Priority 4: Initial Sequential 3D Movement Onboarding (Walk -> Sprint -> Jump)
            if (!moveGuideDone)
            {
                if (player3D.PlanarSpeed > 0.25f)
                {
                    walkedTimer += Time.deltaTime;
                    if (walkedTimer >= 1.25f) moveGuideDone = true;
                }
                keys = new[] { "W", "A", "S", "D" };
                subLabel = "DI CHUYỂN 3D";
                return;
            }

            if (!sprintGuideDone)
            {
                sprintShowTimer += Time.deltaTime;
                if (player3D.IsSprinting || sprintShowTimer >= 3.2f)
                {
                    sprintGuideDone = true;
                }
                else
                {
                    keys = new[] { "SHIFT" };
                    subLabel = "CHẠY NHANH";
                    return;
                }
            }

            if (!jumpGuideDone)
            {
                jumpShowTimer += Time.deltaTime;
                if (!player3D.IsGrounded || jumpShowTimer >= 3.0f)
                {
                    jumpGuideDone = true;
                }
                else
                {
                    keys = new[] { jumpKey };
                    subLabel = "NHẢY VƯỢT BẬC";
                    return;
                }
            }
        }

        private void UpdateArrivalVignette(float dt)
        {
            if (!showOpeningArrivalBanner) return;
            if (arrivalTimer >= ArrivalTotalDuration) return;

            arrivalTimer += dt;
            float letterboxBlend = 0f;
            if (arrivalTimer < 0.7f) letterboxBlend = Mathf.SmoothStep(0f, 1f, arrivalTimer / 0.7f);
            else if (arrivalTimer < 4.1f) letterboxBlend = 1f;
            else letterboxBlend = 1f - Mathf.SmoothStep(0f, 1f, (arrivalTimer - 4.1f) / 1.3f);

            float barH = 68f * letterboxBlend;
            if (topLetterboxBar != null) topLetterboxBar.sizeDelta = new Vector2(0f, barH);
            if (bottomLetterboxBar != null) bottomLetterboxBar.sizeDelta = new Vector2(0f, barH);

            if (arrivalTitleGroup != null)
            {
                float titleAlpha = 0f;
                if (arrivalTimer > 0.45f && arrivalTimer < 1.35f)
                    titleAlpha = Mathf.SmoothStep(0f, 1f, (arrivalTimer - 0.45f) / 0.90f);
                else if (arrivalTimer >= 1.35f && arrivalTimer < 3.95f)
                    titleAlpha = 1f;
                else if (arrivalTimer >= 3.95f && arrivalTimer < ArrivalTotalDuration)
                    titleAlpha = 1f - Mathf.SmoothStep(0f, 1f, (arrivalTimer - 3.95f) / (ArrivalTotalDuration - 3.95f));

                arrivalTitleGroup.alpha = titleAlpha;
            }
        }

        private void UpdateNarrativeToast(float dt)
        {
            if (narrativeToastGroup == null) return;
            if (narrativeToastTimer > 0f)
            {
                narrativeToastTimer -= dt;
                narrativeToastGroup.alpha = Mathf.MoveTowards(narrativeToastGroup.alpha, 1f, dt * 4.5f);
            }
            else
            {
                narrativeToastGroup.alpha = Mathf.MoveTowards(narrativeToastGroup.alpha, 0f, dt * 2.5f);
            }
        }

        private void UpdateControlStrip(float dt)
        {
            if (IsToggleGuideKeyPressed())
            {
                controlStripPinned = !controlStripPinned;
                if (controlStripPinned) controlStripAutoHideTimer = 9999f;
                else controlStripAutoHideTimer = 0f;
            }

            if (!controlStripPinned && controlStripAutoHideTimer > 0f)
            {
                controlStripAutoHideTimer -= dt;
            }

            if (controlStripGroup != null)
            {
                float targetAlpha = (controlStripPinned || controlStripAutoHideTimer > 0f) ? 0.88f : 0.28f;
                controlStripGroup.alpha = Mathf.MoveTowards(controlStripGroup.alpha, targetAlpha, dt * 2.5f);
            }

            if (stealthStatusBadgeText != null && player3D != null)
            {
                if (player3D.IsHidingInBush)
                {
                    int pct = Mathf.RoundToInt(player3D.Concealment * 100f);
                    stealthStatusBadgeText.text = $"● ĐANG ẨN NẤP TRONG BỤI CÂY ({pct}%)";
                    stealthStatusBadgeText.color = new Color(0.68f, 0.96f, 0.74f, 0.92f);
                }
                else if (player3D.IsCrouching)
                {
                    stealthStatusBadgeText.text = "● ĐANG ĐI KHOM NHẸ CHÂN";
                    stealthStatusBadgeText.color = new Color(0.95f, 0.92f, 0.78f, 0.80f);
                }
                else
                {
                    stealthStatusBadgeText.text = "";
                }
            }
        }

        private void ProjectGlyphToScreen()
        {
            if (glyphClusterRect == null || currentWorldTarget == null) return;
            Camera cam = Camera.main;
            if (cam == null) return;

            float bob = Mathf.Sin(Time.unscaledTime * 2.5f) * 0.06f;
            Vector3 worldPos = currentWorldTarget.position + currentWorldOffset + new Vector3(0f, bob, 0f);
            Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

            if (screenPos.z > 0.1f)
            {
                screenPos.x = Mathf.Clamp(screenPos.x, 110f, Screen.width - 110f);
                screenPos.y = Mathf.Clamp(screenPos.y, 90f, Screen.height - 90f);
                glyphClusterRect.position = screenPos;
            }
            else if (glyphCanvasGroup != null)
            {
                glyphCanvasGroup.alpha = 0f;
            }
        }

        private void FadeGlyphAlpha(float target)
        {
            if (glyphCanvasGroup == null) return;
            float pulse = (target > 0.5f) ? (0.88f + Mathf.Sin(Time.unscaledTime * 2.8f) * 0.12f) : 0f;
            glyphCanvasGroup.alpha = Mathf.MoveTowards(glyphCanvasGroup.alpha, target * pulse, Time.unscaledDeltaTime * 4.0f);
        }

        private void Build3DUIGuideCanvas()
        {
            var existing = transform.Find("Canvas_CaMau3D_UIGuides");
            if (existing != null)
            {
                if (Application.isPlaying) Destroy(existing.gameObject);
                else DestroyImmediate(existing.gameObject);
            }

            var canvasGO = new GameObject("Canvas_CaMau3D_UIGuides");
            canvasGO.transform.SetParent(transform, false);
            uiCanvas = canvasGO.AddComponent<Canvas>();
            uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            uiCanvas.sortingOrder = 445;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            uiFont = Font.CreateDynamicFontFromOSFont("Segoe UI", 28)
                  ?? Font.CreateDynamicFontFromOSFont("Arial", 28)
                  ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // A. Cinematic 21:9 Arrival Letterbox Bars
            topLetterboxBar = CreateRect("Arrival_Letterbox_Top", canvasGO.transform);
            topLetterboxBar.anchorMin = new Vector2(0f, 1f);
            topLetterboxBar.anchorMax = new Vector2(1f, 1f);
            topLetterboxBar.pivot = new Vector2(0.5f, 1f);
            topLetterboxBar.sizeDelta = Vector2.zero;
            var topImg = topLetterboxBar.gameObject.AddComponent<Image>();
            topImg.color = new Color(0.02f, 0.02f, 0.03f, 0.96f);
            topImg.raycastTarget = false;

            bottomLetterboxBar = CreateRect("Arrival_Letterbox_Bottom", canvasGO.transform);
            bottomLetterboxBar.anchorMin = new Vector2(0f, 0f);
            bottomLetterboxBar.anchorMax = new Vector2(1f, 0f);
            bottomLetterboxBar.pivot = new Vector2(0.5f, 0f);
            bottomLetterboxBar.sizeDelta = Vector2.zero;
            var botImg = bottomLetterboxBar.gameObject.AddComponent<Image>();
            botImg.color = new Color(0.02f, 0.02f, 0.03f, 0.96f);
            botImg.raycastTarget = false;

            // B. Arrival Village Title Stamp (matches VillageArrivalCutscene aesthetic)
            var arrivalRoot = CreateRect("Arrival_Location_Stamp", canvasGO.transform);
            arrivalRoot.anchorMin = arrivalRoot.anchorMax = new Vector2(0.5f, 0.80f);
            arrivalRoot.pivot = new Vector2(0.5f, 0.5f);
            arrivalRoot.sizeDelta = new Vector2(920f, 110f);
            arrivalTitleGroup = arrivalRoot.gameObject.AddComponent<CanvasGroup>();
            arrivalTitleGroup.alpha = 0f;
            arrivalTitleGroup.blocksRaycasts = false;

            var titleRect = CreateRect("Title", arrivalRoot);
            titleRect.anchorMin = new Vector2(0f, 0.45f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = titleRect.offsetMax = Vector2.zero;
            var titleTxt = titleRect.gameObject.AddComponent<Text>();
            titleTxt.font = uiFont;
            titleTxt.fontSize = 30;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.color = new Color(0.99f, 0.96f, 0.88f, 0.98f);
            titleTxt.text = villageTitle;
            titleTxt.raycastTarget = false;
            var titleShadow = titleRect.gameObject.AddComponent<Shadow>();
            titleShadow.effectColor = new Color(0f, 0f, 0f, 0.72f);
            titleShadow.effectDistance = new Vector2(0f, -2f);

            var lineDiv = CreateRect("ThinDivider", arrivalRoot);
            lineDiv.anchorMin = lineDiv.anchorMax = new Vector2(0.5f, 0.44f);
            lineDiv.sizeDelta = new Vector2(320f, 1.5f);
            var lineDivImg = lineDiv.gameObject.AddComponent<Image>();
            lineDivImg.color = new Color(0.98f, 0.92f, 0.78f, 0.65f);
            lineDivImg.raycastTarget = false;

            var subRect = CreateRect("Subtitle", arrivalRoot);
            subRect.anchorMin = new Vector2(0f, 0f);
            subRect.anchorMax = new Vector2(1f, 0.42f);
            subRect.offsetMin = subRect.offsetMax = Vector2.zero;
            var subTxt = subRect.gameObject.AddComponent<Text>();
            subTxt.font = uiFont;
            subTxt.fontSize = 16;
            subTxt.fontStyle = FontStyle.Italic;
            subTxt.alignment = TextAnchor.MiddleCenter;
            subTxt.color = new Color(0.98f, 0.95f, 0.88f, 0.96f);
            subTxt.text = villageSubtitle;
            subTxt.raycastTarget = false;
            var subTitleShadow = subRect.gameObject.AddComponent<Shadow>();
            subTitleShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            subTitleShadow.effectDistance = new Vector2(0f, -1.5f);

            // C. World-Projected Minimalist Keycap Cluster
            var clusterRoot = new GameObject("GlyphWorldCluster", typeof(RectTransform), typeof(CanvasGroup), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            clusterRoot.transform.SetParent(canvasGO.transform, false);
            glyphClusterRect = clusterRoot.GetComponent<RectTransform>();
            glyphClusterRect.anchorMin = glyphClusterRect.anchorMax = new Vector2(0.5f, 0.5f);
            glyphClusterRect.pivot = new Vector2(0.5f, 0.5f);

            glyphCanvasGroup = clusterRoot.GetComponent<CanvasGroup>();
            glyphCanvasGroup.alpha = 0f;
            glyphCanvasGroup.blocksRaycasts = false;
            glyphCanvasGroup.interactable = false;

            var vlg = clusterRoot.GetComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.spacing = 6f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;

            var csf = clusterRoot.GetComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var keysRow = new GameObject("KeysRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            keysRow.transform.SetParent(glyphClusterRect, false);
            var hlg = keysRow.GetComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 8f;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            var rowCsf = keysRow.GetComponent<ContentSizeFitter>();
            rowCsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            rowCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var subLblGO = new GameObject("ContextualSubLabel", typeof(RectTransform));
            subLblGO.transform.SetParent(glyphClusterRect, false);
            glyphSubLabelText = subLblGO.AddComponent<Text>();
            glyphSubLabelText.font = uiFont;
            glyphSubLabelText.fontSize = 13;
            glyphSubLabelText.fontStyle = FontStyle.Bold;
            glyphSubLabelText.alignment = TextAnchor.MiddleCenter;
            glyphSubLabelText.color = new Color(0.98f, 0.96f, 0.90f, 0.85f);
            glyphSubLabelText.raycastTarget = false;
            var subShadow = subLblGO.AddComponent<Shadow>();
            subShadow.effectColor = new Color(0f, 0f, 0f, 0.78f);
            subShadow.effectDistance = new Vector2(0f, -1.5f);

            // D. Clean Narrative Subtitle / Inspection Banner (Bottom-Center)
            var toastRect = CreateRect("Narrative_Inspect_Banner", canvasGO.transform);
            toastRect.anchorMin = toastRect.anchorMax = new Vector2(0.5f, 0.11f);
            toastRect.pivot = new Vector2(0.5f, 0.5f);
            toastRect.sizeDelta = new Vector2(1200f, 108f);
            narrativeToastGroup = toastRect.gameObject.AddComponent<CanvasGroup>();
            narrativeToastGroup.alpha = 0f;
            narrativeToastGroup.blocksRaycasts = false;

            var toastBg = toastRect.gameObject.AddComponent<Image>();
            toastBg.color = new Color(0.04f, 0.06f, 0.10f, 0.76f);
            toastBg.raycastTarget = false;

            var tTopLine = CreateRect("AccentLine", toastRect);
            tTopLine.anchorMin = new Vector2(0.08f, 1f);
            tTopLine.anchorMax = new Vector2(0.92f, 1f);
            tTopLine.sizeDelta = new Vector2(0f, 1.5f);
            var tTopImg = tTopLine.gameObject.AddComponent<Image>();
            tTopImg.color = new Color(0.96f, 0.88f, 0.70f, 0.55f);
            tTopImg.raycastTarget = false;

            var nTitleRect = CreateRect("NarrativeTitle", toastRect);
            nTitleRect.anchorMin = new Vector2(0f, 0.56f);
            nTitleRect.anchorMax = new Vector2(1f, 0.94f);
            nTitleRect.offsetMin = new Vector2(24f, 0f);
            nTitleRect.offsetMax = new Vector2(-24f, 0f);
            narrativeTitleText = nTitleRect.gameObject.AddComponent<Text>();
            narrativeTitleText.font = uiFont;
            narrativeTitleText.fontSize = 14;
            narrativeTitleText.fontStyle = FontStyle.Bold;
            narrativeTitleText.alignment = TextAnchor.MiddleCenter;
            narrativeTitleText.color = new Color(0.98f, 0.88f, 0.65f, 0.96f);
            narrativeTitleText.raycastTarget = false;

            var nBodyRect = CreateRect("NarrativeBody", toastRect);
            nBodyRect.anchorMin = new Vector2(0f, 0.08f);
            nBodyRect.anchorMax = new Vector2(1f, 0.58f);
            nBodyRect.offsetMin = new Vector2(28f, 0f);
            nBodyRect.offsetMax = new Vector2(-28f, 0f);
            narrativeBodyText = nBodyRect.gameObject.AddComponent<Text>();
            narrativeBodyText.font = uiFont;
            narrativeBodyText.fontSize = 20;
            narrativeBodyText.alignment = TextAnchor.MiddleCenter;
            narrativeBodyText.color = new Color(0.98f, 0.97f, 0.94f, 0.96f);
            narrativeBodyText.raycastTarget = false;

            // E. Subtle Top-Right Minimalist Control Strip ([H] to pin/fade) + Stealth Indicator
            var ctrlRect = CreateRect("Minimalist_3D_Control_Guide", canvasGO.transform);
            ctrlRect.anchorMin = ctrlRect.anchorMax = new Vector2(1f, 1f);
            ctrlRect.pivot = new Vector2(1f, 1f);
            ctrlRect.anchoredPosition = new Vector2(-28f, -20f);
            ctrlRect.sizeDelta = new Vector2(890f, 54f);
            controlStripGroup = ctrlRect.gameObject.AddComponent<CanvasGroup>();
            controlStripGroup.alpha = 0.92f;
            controlStripGroup.blocksRaycasts = false;

            var ctrlPillBg = CreateRect("ControlPillBackdrop", ctrlRect);
            ctrlPillBg.anchorMin = new Vector2(0f, 0.42f);
            ctrlPillBg.anchorMax = new Vector2(1f, 1f);
            ctrlPillBg.offsetMin = new Vector2(-14f, -3f);
            ctrlPillBg.offsetMax = new Vector2(14f, 3f);
            var ctrlPillImg = ctrlPillBg.gameObject.AddComponent<Image>();
            ctrlPillImg.color = new Color(0.04f, 0.06f, 0.11f, 0.56f);
            ctrlPillImg.raycastTarget = false;

            var ctrlTxtRect = CreateRect("ControlsLine", ctrlRect);
            ctrlTxtRect.anchorMin = new Vector2(0f, 0.45f);
            ctrlTxtRect.anchorMax = new Vector2(1f, 1f);
            ctrlTxtRect.offsetMin = ctrlTxtRect.offsetMax = Vector2.zero;
            var ctrlTxt = ctrlTxtRect.gameObject.AddComponent<Text>();
            ctrlTxt.font = uiFont;
            ctrlTxt.fontSize = 13;
            ctrlTxt.alignment = TextAnchor.MiddleRight;
            ctrlTxt.color = new Color(0.96f, 0.95f, 0.90f, 0.86f);
            ctrlTxt.text = "[WASD] Di chuyển   •   [Shift] Chạy   •   [Space] Nhảy   •   [C] Cúi nấp   •   [E] Tương tác   •   [Esc] Tạm dừng   •   [H] Ẩn/Hiện";
            ctrlTxt.raycastTarget = false;
            var ctrlShadow = ctrlTxtRect.gameObject.AddComponent<Shadow>();
            ctrlShadow.effectColor = new Color(0f, 0f, 0f, 0.70f);
            ctrlShadow.effectDistance = new Vector2(0f, -1f);

            var stealthRect = CreateRect("StealthStatusLine", ctrlRect);
            stealthRect.anchorMin = new Vector2(0f, 0f);
            stealthRect.anchorMax = new Vector2(1f, 0.45f);
            stealthRect.offsetMin = stealthRect.offsetMax = Vector2.zero;
            stealthStatusBadgeText = stealthRect.gameObject.AddComponent<Text>();
            stealthStatusBadgeText.font = uiFont;
            stealthStatusBadgeText.fontSize = 13;
            stealthStatusBadgeText.fontStyle = FontStyle.Bold;
            stealthStatusBadgeText.alignment = TextAnchor.MiddleRight;
            stealthStatusBadgeText.text = "";
            stealthStatusBadgeText.raycastTarget = false;
        }

        private void RebuildKeyCapGlyphs(string[] keyLabels, string subLabel)
        {
            for (int i = 0; i < activeKeyCapObjects.Count; i++)
            {
                if (activeKeyCapObjects[i] != null)
                {
                    if (Application.isPlaying) Destroy(activeKeyCapObjects[i]);
                    else DestroyImmediate(activeKeyCapObjects[i]);
                }
            }
            activeKeyCapObjects.Clear();

            if (glyphClusterRect == null || keyLabels == null) return;
            Transform keysRow = glyphClusterRect.Find("KeysRow");
            if (keysRow == null) return;

            foreach (string rawKey in keyLabels)
            {
                string label = rawKey.Trim().ToUpperInvariant();
                if (label == "LEFTCONTROL" || label == "LCTRL") label = "CTRL";
                if (label == "LEFTSHIFT" || label == "LSHIFT") label = "SHIFT";
                if (label.Contains("SPACE") || label.Contains("CÁCH")) label = "SPACE";

                bool isSpace = label == "SPACE";
                bool isWide = isSpace || label.Length > 2;
                float width = isSpace ? 96f : (isWide ? Mathf.Max(76f, label.Length * 14f + 20f) : 44f);
                float height = 44f;

                var capGO = new GameObject($"KeyGlyph_{label}", typeof(RectTransform), typeof(LayoutElement));
                capGO.transform.SetParent(keysRow, false);
                var capRect = capGO.GetComponent<RectTransform>();
                capRect.sizeDelta = new Vector2(width, height);

                var le = capGO.GetComponent<LayoutElement>();
                le.minWidth = width;
                le.preferredWidth = width;
                le.minHeight = height;
                le.preferredHeight = height;

                var fillImg = capGO.AddComponent<Image>();
                fillImg.color = GlyphFillColor;
                fillImg.raycastTarget = false;

                CreateThinBorderEdge(capRect, "Top",    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -1.5f), Vector2.zero);
                CreateThinBorderEdge(capRect, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 1.5f));
                CreateThinBorderEdge(capRect, "Left",   new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(1.5f, 0f));
                CreateThinBorderEdge(capRect, "Right",  new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-1.5f, 0f), Vector2.zero);

                var txtGO = new GameObject("GlyphChar", typeof(RectTransform));
                txtGO.transform.SetParent(capRect, false);
                var txtRect = txtGO.GetComponent<RectTransform>();
                txtRect.anchorMin = Vector2.zero;
                txtRect.anchorMax = Vector2.one;
                txtRect.offsetMin = Vector2.zero;
                txtRect.offsetMax = Vector2.zero;

                var txt = txtGO.AddComponent<Text>();
                txt.font = uiFont;
                txt.fontSize = isSpace ? 15 : (isWide ? 16 : 20);
                txt.fontStyle = FontStyle.Bold;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color = GlyphSingleColor;
                txt.text = label;
                txt.raycastTarget = false;

                activeKeyCapObjects.Add(capGO);
            }

            if (glyphSubLabelText != null)
            {
                glyphSubLabelText.text = string.IsNullOrEmpty(subLabel) ? "" : subLabel.ToUpperInvariant();
                glyphSubLabelText.gameObject.SetActive(!string.IsNullOrEmpty(subLabel));
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(glyphClusterRect);
        }

        private static void CreateThinBorderEdge(RectTransform parent, string name, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            var edgeGO = new GameObject(name, typeof(RectTransform), typeof(Image));
            edgeGO.transform.SetParent(parent, false);
            var rt = edgeGO.GetComponent<RectTransform>();
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            var img = edgeGO.GetComponent<Image>();
            img.color = GlyphBorderColor;
            img.raycastTarget = false;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static bool IsToggleGuideKeyPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.hKey.wasPressedThisFrame) return true;
#else
            if (Input.GetKeyDown(KeyCode.H)) return true;
#endif
            return false;
        }
    }
}

