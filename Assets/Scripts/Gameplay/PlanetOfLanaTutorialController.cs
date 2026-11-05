using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Authentic Planet of Lana Minimalist Diegetic Key Glyph Tutorial Controller.
    /// Strictly follows user specification:
    /// - ONLY a translucent single-color key symbol/icon ("ch? c� bi?u t�?ng ch? trong su?t m?t m�u").
    /// - ZERO text boxes, ZERO instruction paragraphs, ZERO dark background pills ("kh�ng hi?n m?t c�i box text �? guide ng�?i ch�i").
    /// - Guides the player through the complete Forest Manual Obstacle Course:
    ///   1. Walk (A / D)
    ///   2. Grab & Climb/Swing Rope up the Cliff (F -> W / A D / SPACE)
    ///   3. Jump across the Rocky Ravine Gap (SPACE)
    ///   4. Climb down & Crawl under the Fallen Tree Trunk (C / S)
    ///   5. Push/Pull the Wooden Crate to the cliff corner & Climb to the High Platform (E -> A / D -> SPACE)
    /// </summary>
    [DisallowMultipleComponent]
    public class PlanetOfLanaTutorialController : MonoBehaviour
    {
        public static PlanetOfLanaTutorialController Instance { get; private set; }

        private Canvas tutorialCanvas;
        private CanvasGroup glyphCanvasGroup;
        private RectTransform glyphClusterRect;
        private readonly List<GameObject> activeKeyCapObjects = new List<GameObject>();
        private Font glyphFont;

        private PlayerController player;
        private RopeController forestRope;
        private InteractableCrate forestCrate;

        private string currentDisplayedSignature = "";
        private Vector3 currentWorldAnchorOffset = new Vector3(0f, 2.25f, 0f);
        private Transform currentCustomWorldTarget = null;

        private float walkTimer = 0f;
        private bool walkDone = false;

        private float cratePromptTimer = 0f;
        private bool cratePromptCompleted = false;
        private const float CratePromptMaxDuration = 1.8f; // Hien thi trong 1 - 2 giay (1.8s) khi toi thung go

        // Monochromatic translucent off-white palette (Planet of Lana signature)
        private static readonly Color GlyphSingleColor = new Color(1.0f, 1.0f, 1.0f, 0.72f);
        private static readonly Color GlyphBorderColor = new Color(1.0f, 1.0f, 1.0f, 0.48f);
        private static readonly Color GlyphFillColor   = new Color(1.0f, 1.0f, 1.0f, 0.08f);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            BuildMinimalistGlyphCanvas();
        }

        private void Start()
        {
            player = Object.FindAnyObjectByType<PlayerController>();
            forestRope = Object.FindAnyObjectByType<RopeController>();
            forestCrate = Object.FindAnyObjectByType<InteractableCrate>();
        }

        private void LateUpdate()
        {
            if (player == null)
            {
                player = Object.FindAnyObjectByType<PlayerController>();
                if (player == null) return;
            }

            // Hide all tutorial glyphs while cutscene is running or player is disabled
            if (!player.enabled || (IntroCutsceneControllerExistsAndRunning()))
            {
                if (glyphCanvasGroup != null) glyphCanvasGroup.alpha = 0f;
                currentDisplayedSignature = "";
                return;
            }

            if (forestRope == null) forestRope = Object.FindAnyObjectByType<RopeController>();
            if (forestCrate == null) forestCrate = Object.FindAnyObjectByType<InteractableCrate>();

            EvaluateContextualGlyphState( out string[] keysToDisplay, out Transform anchorTarget, out Vector3 worldOffset);

            if (keysToDisplay == null || keysToDisplay.Length == 0)
            {
                FadeGlyphAlpha(0f);
                currentDisplayedSignature = "";
            }
            else
            {
                string sig = string.Join("|", keysToDisplay);
                if (sig != currentDisplayedSignature)
                {
                    currentDisplayedSignature = sig;
                    RebuildKeyCapGlyphs(keysToDisplay);
                }

                currentCustomWorldTarget = anchorTarget != null ? anchorTarget : player.transform;
                currentWorldAnchorOffset = worldOffset;

                UpdateWorldToScreenPosition();
                FadeGlyphAlpha(1f);
            }
        }

        private bool IntroCutsceneControllerExistsAndRunning()
        {
            var intro = Object.FindAnyObjectByType<IntroCutsceneController>();
            return intro != null && intro.IsCutsceneRunning;
        }

        private void EvaluateContextualGlyphState(out string[] keys, out Transform target, out Vector3 offset)
        {
            keys = null;
            target = player.transform;
            offset = new Vector3(0f, 2.35f, 0f);

            Vector3 pPos = player.transform.position;
            float x = pPos.x;

            // 1. Movement (A / D) at Forest Clearing Start
            if (!walkDone)
            {
                if (Mathf.Abs(player.HorizontalInput) > 0.1f)
                {
                    walkTimer += Time.deltaTime;
                    if (walkTimer >= 1.1f) walkDone = true;
                }

                if (!walkDone && x < 4.5f)
                {
                    string left = KeybindingManager.GetKeyDisplayName(KeybindingManager.GetKey(GameAction.MoveLeft));
                    string right = KeybindingManager.GetKeyDisplayName(KeybindingManager.GetKey(GameAction.MoveRight));
                    keys = new[] { left, right };
                    return;
                }
            }

            // 1.5. Ledge Grab: show [W] to climb up (Chi W hoac Mui ten len moi len duoc)
            if (player.CurrentState == PlayerState.LedgeGrab)
            {
                string up = KeybindingManager.GetKeyDisplayName(KeybindingManager.GetKey(GameAction.MoveUp));
                if (string.IsNullOrEmpty(up)) up = "W";
                keys = new[] { up };
                offset = new Vector3(0f, 1.85f, 0f);
                return;
            }

            // 2. Rope Swing & Climb up Cliff (X ~ 4.5m .. 11.2m)
            if (x >= 4.2f && x <= 11.2f && pPos.y < 0.85f)
            {
                if (player.CurrentState == PlayerState.RopeGrab || player.CurrentState == PlayerState.RopeClimb)
                {
                    string up = KeybindingManager.GetKeyDisplayName(KeybindingManager.GetKey(GameAction.MoveUp));
                    string left = KeybindingManager.GetKeyDisplayName(KeybindingManager.GetKey(GameAction.MoveLeft));
                    string right = KeybindingManager.GetKeyDisplayName(KeybindingManager.GetKey(GameAction.MoveRight));
                    string jump = KeybindingManager.GetKeyDisplayName(KeybindingManager.GetKey(GameAction.Jump));
                    keys = new[] { up, left, right, jump };
                    offset = new Vector3(0f, 2.25f, 0f);
                    return;
                }
                else
                {
                    string ropeKey = KeybindingManager.GetKeyDisplayName(KeybindingManager.GetKey(GameAction.RopeGrab));
                    keys = new[] { ropeKey };
                    if (forestRope != null)
                    {
                        target = forestRope.transform;
                        offset = new Vector3(0f, -2.0f, 0f);
                    }
                    return;
                }
            }

            // 3. Jump across Rocky Ravine Gap (X ~ 12.2m .. 17.5m)
            if (x >= 12.2f && x <= 17.4f)
            {
                string jump = KeybindingManager.GetKeyDisplayName(KeybindingManager.GetKey(GameAction.Jump));
                keys = new[] { jump };
                offset = new Vector3(0f, 2.35f, 0f);
                return;
            }

            // 4. Crawl under Fallen Tree Trunk (X ~ 21.5m .. 27.4m)
            if (x >= 21.5f && x <= 27.4f)
            {
                string crouchKey = KeybindingManager.GetKeyDisplayName(KeybindingManager.GetKey(GameAction.Crouch));
                keys = new[] { crouchKey };
                offset = new Vector3(0f, 1.95f, 0f);
                return;
            }

            // 5. Push/Pull Crate to Cliff Corner & Climb to High Platform (X ~ 27.5m .. 36.2m)
            if (x >= 27.5f && x <= 36.2f && pPos.y < 1.35f)
            {
                if (player.CurrentState == PlayerState.PushPull)
                {
                    cratePromptCompleted = true;
                    string left = KeybindingManager.GetKeyDisplayName(KeybindingManager.GetKey(GameAction.MoveLeft));
                    string right = KeybindingManager.GetKeyDisplayName(KeybindingManager.GetKey(GameAction.MoveRight));
                    keys = new[] { left, right };
                    offset = new Vector3(0f, 2.35f, 0f);
                    return;
                }
                else if (forestCrate != null)
                {
                    float crateX = forestCrate.transform.position.x;
                    // Once crate is pushed near the cliff wall (X >= 33.6m), show SPACE to jump onto crate & ledge
                    if (crateX >= 33.6f)
                    {
                        string jump = KeybindingManager.GetKeyDisplayName(KeybindingManager.GetKey(GameAction.Jump));
                        keys = new[] { jump };
                        target = forestCrate.transform;
                        offset = new Vector3(0f, 1.95f, 0f);
                        return;
                    }
                    else if (!cratePromptCompleted)
                    {
                        // Chi hien khi toi cho thung go (khoang cach gan <= 2.0m) va chi hien 1 - 2 giay (1.8s)
                        float distToCrate = Mathf.Abs(pPos.x - crateX);
                        float distYToCrate = Mathf.Abs(pPos.y - forestCrate.transform.position.y);
                        bool isAtCrate = distToCrate <= 2.0f && distYToCrate <= 1.3f;

                        if (isAtCrate)
                        {
                            if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0))
                            {
                                cratePromptCompleted = true;
                                return;
                            }

                            cratePromptTimer += Time.deltaTime;
                            if (cratePromptTimer <= CratePromptMaxDuration)
                            {
                                string interact = KeybindingManager.GetKeyDisplayName(KeybindingManager.GetKey(GameAction.Interact));
                                keys = new[] { interact };
                                target = forestCrate.transform;
                                offset = new Vector3(0f, 1.85f, 0f);
                                return;
                            }
                            else
                            {
                                cratePromptCompleted = true;
                            }
                        }
                    }
                }
            }

            // 6. Crawl / Crouch under Vine-Covered Rock Cave Pass (PF_Rock_Mountain_CavePass & Creeping Vines, X ~ 130.0m .. 136.5m)
            if (x >= 130.0f && x <= 136.5f && pPos.y < 0.5f)
            {
                if (!player.IsCrawling && !player.IsCrouching)
                {
                    string crouchKey = KeybindingManager.GetKeyDisplayName(KeybindingManager.GetKey(GameAction.Crouch));
                    if (string.IsNullOrEmpty(crouchKey)) crouchKey = "C";
                    keys = new[] { crouchKey };
                    offset = new Vector3(0f, 1.95f, 0f);
                    return;
                }
            }

            // 7. Talk / Interact with Villager Child Thang Minh (X ~ 147.5m .. 151.0m)
            if (x >= 147.5f && x <= 151.0f)
            {
                string interact = KeybindingManager.GetKeyDisplayName(KeybindingManager.GetKey(GameAction.Interact));
                if (string.IsNullOrEmpty(interact)) interact = "E";
                keys = new[] { interact };
                var npc = GameObject.Find("VillageChild_01_ThangMinh");
                if (npc != null) target = npc.transform;
                offset = new Vector3(0f, 1.85f, 0f);
                return;
            }

            // 8. Move forward towards Ca Mau 3D Village at End of Map (X ~ 177.0m .. 182.5m)
            if (x >= 177.0f && x <= 182.5f)
            {
                string right = KeybindingManager.GetKeyDisplayName(KeybindingManager.GetKey(GameAction.MoveRight));
                if (string.IsNullOrEmpty(right)) right = "D";
                keys = new[] { right };
                offset = new Vector3(0f, 2.25f, 0f);
                return;
            }
        }

        private void UpdateWorldToScreenPosition()
        {
            if (glyphClusterRect == null) return;
            Camera cam = Camera.main;
            if (cam != null && currentCustomWorldTarget != null)
            {
                float bobY = Mathf.Sin(Time.unscaledTime * 2.4f) * 0.08f;
                Vector3 worldPos = currentCustomWorldTarget.position + currentWorldAnchorOffset + new Vector3(0f, bobY, 0f);
                Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
                if (screenPos.z > 0f)
                {
                    glyphClusterRect.position = screenPos;
                }
            }
        }

        private void FadeGlyphAlpha(float target)
        {
            if (glyphCanvasGroup == null) return;
            float pulse = (target > 0.5f) ? (0.88f + Mathf.Sin(Time.unscaledTime * 2.8f) * 0.12f) : 0f;
            glyphCanvasGroup.alpha = Mathf.MoveTowards(glyphCanvasGroup.alpha, target * pulse, Time.unscaledDeltaTime * 3.2f);
        }

        // Legacy compatibility API
        public void ShowPrompt(string title, string keys, string desc)
        {
            string cleanKey = keys.Replace("[", "").Replace("]", "").Trim();
            RebuildKeyCapGlyphs(new[] { cleanKey });
        }

        public void HidePrompt()
        {
            currentDisplayedSignature = "";
            if (glyphCanvasGroup != null) glyphCanvasGroup.alpha = 0f;
        }

        private void BuildMinimalistGlyphCanvas()
        {
            var existingCanvas = transform.Find("Canvas_PlanetOfLana_MinimalistGlyphs");
            if (existingCanvas != null)
            {
                if (Application.isPlaying) Destroy(existingCanvas.gameObject);
                else DestroyImmediate(existingCanvas.gameObject);
            }

            var canvasGO = new GameObject("Canvas_PlanetOfLana_MinimalistGlyphs");
            canvasGO.hideFlags = HideFlags.DontSave;
            canvasGO.transform.SetParent(transform, false);

            tutorialCanvas = canvasGO.AddComponent<Canvas>();
            tutorialCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            tutorialCanvas.sortingOrder = 450;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            glyphFont = Font.CreateDynamicFontFromOSFont("Segoe UI", 28) ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var clusterGO = new GameObject("GlyphCluster", typeof(RectTransform), typeof(CanvasGroup), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            clusterGO.transform.SetParent(canvasGO.transform, false);
            glyphClusterRect = clusterGO.GetComponent<RectTransform>();
            glyphClusterRect.anchorMin = glyphClusterRect.anchorMax = new Vector2(0.5f, 0.5f);
            glyphClusterRect.pivot = new Vector2(0.5f, 0.5f);

            glyphCanvasGroup = clusterGO.GetComponent<CanvasGroup>();
            glyphCanvasGroup.alpha = 0f;
            glyphCanvasGroup.blocksRaycasts = false;
            glyphCanvasGroup.interactable = false;

            var hlg = clusterGO.GetComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 12f;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            var csf = clusterGO.GetComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private void RebuildKeyCapGlyphs(string[] keyLabels)
        {
            for (int i = 0; i < activeKeyCapObjects.Count; i++)
            {
                if (activeKeyCapObjects[i] != null) Destroy(activeKeyCapObjects[i]);
            }
            activeKeyCapObjects.Clear();

            if (glyphClusterRect == null || keyLabels == null) return;

            foreach (string rawKey in keyLabels)
            {
                string label = rawKey.Trim().ToUpperInvariant();
                if (label == "LEFTCONTROL" || label == "LCTRL") label = "CTRL";
                if (label == "LEFTSHIFT" || label == "LSHIFT") label = "SHIFT";
                if (label == "SPACE" || label.Contains("SPACE") || label.Contains("C�CH")) label = "SPACE";

                bool isSpace = label == "SPACE";
                bool isWideKey = isSpace || label.Length > 2;
                float width = isSpace ? 104f : (isWideKey ? Mathf.Max(82f, label.Length * 15f + 22f) : 46f);
                float height = 46f;

                var capGO = new GameObject($"KeyGlyph_{label}", typeof(RectTransform), typeof(LayoutElement));
                capGO.hideFlags = HideFlags.DontSave;
                capGO.transform.SetParent(glyphClusterRect, false);
                var capRect = capGO.GetComponent<RectTransform>();
                capRect.sizeDelta = new Vector2(width, height);

                var le = capGO.GetComponent<LayoutElement>();
                le.minWidth = width;
                le.preferredWidth = width;
                le.minHeight = height;
                le.preferredHeight = height;

                // Ultra-subtle single-color translucent fill
                var fillImg = capGO.AddComponent<Image>();
                fillImg.color = GlyphFillColor;
                fillImg.raycastTarget = false;

                // Thin single-color translucent keycap border (4 thin 1.5px edges, zero dark box!)
                CreateThinBorderEdge(capRect, "Top",    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -1.5f), Vector2.zero);
                CreateThinBorderEdge(capRect, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 1.5f));
                CreateThinBorderEdge(capRect, "Left",   new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(1.5f, 0f));
                CreateThinBorderEdge(capRect, "Right",  new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-1.5f, 0f), Vector2.zero);

                // Translucent single-color key character
                var txtGO = new GameObject("GlyphChar", typeof(RectTransform));
                txtGO.transform.SetParent(capRect, false);
                var txtRect = txtGO.GetComponent<RectTransform>();
                txtRect.anchorMin = Vector2.zero;
                txtRect.anchorMax = Vector2.one;
                txtRect.offsetMin = Vector2.zero;
                txtRect.offsetMax = Vector2.zero;

                var txt = txtGO.AddComponent<Text>();
                txt.font = glyphFont;
                txt.fontSize = isSpace ? 16 : (isWideKey ? 17 : 21);
                txt.fontStyle = FontStyle.Normal;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.horizontalOverflow = HorizontalWrapMode.Overflow;
                txt.verticalOverflow = VerticalWrapMode.Overflow;
                txt.color = GlyphSingleColor;
                txt.text = label;
                txt.raycastTarget = false;

                activeKeyCapObjects.Add(capGO);
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
    }
}
