using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Interactive 3D Rigged Villager NPC for the living Planet of Lana style village.
    /// - Drives Humanoid Animator state (Idle, Wave, Talk, Sit, Walk patrol) + subtle breathing/look-at toward An.
    /// - Displays ONLY a minimalist translucent single-color [E] key glyph above the NPC when An is within interaction range.
    /// - Plays warm conversational animation and displays clean dialogue subtitles when An presses [E].
    /// </summary>
    [DisallowMultipleComponent]
    public class VillageNPCInteractable : MonoBehaviour
    {
        [Header("NPC Identity & Dialogue")]
        [SerializeField] private string npcName = "Người Dân Xóm Rạch Gốc";
        [SerializeField]
        [TextArea(2, 4)]
        private string[] dialogueLines = new string[]
        {
            "Lúa năm nay chín vàng khắp cánh đồng Rạch Gốc rồi đó con.",
            "Bà con trong xóm đang chuẩn bị gạo mới gửi theo đoàn tàu tập kết ngoài bến Sông Đốc."
        };

        [Header("Animation & Living Behavior")]
        [SerializeField] private string defaultAnimState = "Idle";
        [SerializeField] private bool facePlayerWhenClose = true;
        [SerializeField] private float interactRadius = 2.6f;
        [SerializeField] private float patrolRangeX = 0f;
        [SerializeField] private float patrolSpeed = 1.15f;

        private Animator animator;
        private PlayerController player;
        private float startX;
        private int patrolDir = 1;
        private float patrolPauseTimer = 0f;
        private Quaternion baseRotation;

        private int currentLineIndex = -1;
        private float dialogueDisplayTimer = 0f;
        private bool isTalking = false;

        // Shared Runtime UI for NPC Key Glyph & Dialogue Subtitle
        private static Canvas sharedNpcCanvas;
        private static RectTransform sharedKeyGlyphRect;
        private static CanvasGroup sharedKeyGlyphGroup;
        private static VillageNPCInteractable activePromptOwner;

        private static CanvasGroup sharedDialogueGroup;
        private static Text sharedSpeakerText;
        private static Text sharedLineText;
        private static VillageNPCInteractable activeDialogueOwner;

        public void ConfigureNPC(string speakerName, string[] lines, string initialAnimState = "Idle", float patrolDist = 0f)
        {
            npcName = speakerName;
            dialogueLines = lines;
            defaultAnimState = initialAnimState;
            patrolRangeX = patrolDist;
        }

        private void Awake()
        {
            animator = GetComponentInChildren<Animator>();
            startX = transform.position.x;
            baseRotation = transform.rotation;
            EnsureSharedCanvas();
        }

        private void Start()
        {
            player = Object.FindAnyObjectByType<PlayerController>();
            if (animator != null && !string.IsNullOrEmpty(defaultAnimState))
            {
                animator.Play(defaultAnimState, 0, Random.Range(0f, 0.85f));
            }
        }

        private void Update()
        {
            if (player == null)
            {
                player = Object.FindAnyObjectByType<PlayerController>();
                if (player == null) return;
            }

            float distX = Mathf.Abs(player.transform.position.x - transform.position.x);
            float distY = Mathf.Abs(player.transform.position.y - transform.position.y);
            bool inRange = player.enabled && (distX <= interactRadius) && (distY <= 2.0f) && (dialogueLines != null && dialogueLines.Length > 0);

            // 1. Optional gentle village patrol when not talking
            if (!isTalking && !inRange && patrolRangeX > 0.25f)
            {
                UpdatePatrol(Time.deltaTime);
            }
            else if (facePlayerWhenClose && (inRange || isTalking) && defaultAnimState != "Sit")
            {
                float dirToPlayer = Mathf.Sign(player.transform.position.x - transform.position.x);
                if (Mathf.Abs(dirToPlayer) > 0.01f)
                {
                    float targetYaw = (dirToPlayer > 0f) ? 105f : -105f;
                    Quaternion targetRot = Quaternion.Euler(0f, targetYaw, 0f);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5.0f);
                }
            }
            else if (!isTalking && patrolRangeX <= 0.25f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, baseRotation, Time.deltaTime * 3.0f);
            }

            // 2. Manage minimalist [E] key glyph ownership
            if (inRange)
            {
                if (activePromptOwner == null || activePromptOwner == this)
                {
                    activePromptOwner = this;
                    UpdateKeyGlyphPosition();
                }

                if (KeybindingManager.IsActionPressed(GameAction.Interact))
                {
                    AdvanceDialogue();
                }
            }
            else if (activePromptOwner == this)
            {
                activePromptOwner = null;
                if (sharedKeyGlyphGroup != null) sharedKeyGlyphGroup.alpha = 0f;
            }

            // 3. Manage active dialogue timer
            if (isTalking && activeDialogueOwner == this)
            {
                dialogueDisplayTimer -= Time.deltaTime;
                if (dialogueDisplayTimer <= 0f || distX > interactRadius + 1.4f)
                {
                    EndDialogue();
                }
            }
        }

        private void UpdatePatrol(float dt)
        {
            if (patrolPauseTimer > 0f)
            {
                patrolPauseTimer -= dt;
                if (animator != null) animator.CrossFadeInFixedTime("Idle", 0.25f);
                return;
            }

            float nextX = transform.position.x + patrolDir * patrolSpeed * dt;
            if (Mathf.Abs(nextX - startX) >= patrolRangeX)
            {
                patrolDir = -patrolDir;
                patrolPauseTimer = Random.Range(1.8f, 3.4f);
                return;
            }

            transform.position = new Vector3(nextX, transform.position.y, transform.position.z);
            float yaw = (patrolDir > 0) ? 90f : -90f;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0f, yaw, 0f), dt * 6f);
        }

        private void AdvanceDialogue()
        {
            if (dialogueLines == null || dialogueLines.Length == 0) return;

            currentLineIndex++;
            if (currentLineIndex >= dialogueLines.Length)
            {
                EndDialogue();
                return;
            }

            isTalking = true;
            activeDialogueOwner = this;
            dialogueDisplayTimer = 4.8f;

            // Face An toward the NPC
            if (player != null)
            {
                float dirToNpc = Mathf.Sign(transform.position.x - player.transform.position.x);
                if (Mathf.Abs(dirToNpc) > 0.01f) player.SetFacingDirection(dirToNpc);
            }

            // Trigger expressive NPC talking or waving animation
            if (animator != null && defaultAnimState != "Sit")
            {
                string talkState = (currentLineIndex == 0) ? "Talk" : "Wave";
                animator.CrossFadeInFixedTime(talkState, 0.25f);
            }

            if (sharedSpeakerText != null) sharedSpeakerText.text = npcName.ToUpperInvariant();
            if (sharedLineText != null) sharedLineText.text = $"“{dialogueLines[currentLineIndex]}”";
            if (sharedDialogueGroup != null) sharedDialogueGroup.alpha = 1f;
        }

        private void EndDialogue()
        {
            isTalking = false;
            currentLineIndex = -1;
            if (activeDialogueOwner == this)
            {
                activeDialogueOwner = null;
                if (sharedDialogueGroup != null) sharedDialogueGroup.alpha = 0f;
            }
            if (animator != null && !string.IsNullOrEmpty(defaultAnimState))
            {
                animator.CrossFadeInFixedTime(defaultAnimState, 0.35f);
            }
        }

        private void UpdateKeyGlyphPosition()
        {
            if (sharedKeyGlyphRect == null || sharedKeyGlyphGroup == null) return;
            Camera cam = Camera.main;
            if (cam == null) return;

            float bob = Mathf.Sin(Time.unscaledTime * 2.6f) * 0.06f;
            Vector3 worldPos = transform.position + new Vector3(0f, 2.15f + bob, 0f);
            Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
            if (screenPos.z > 0f)
            {
                sharedKeyGlyphRect.position = screenPos;
                sharedKeyGlyphGroup.alpha = isTalking ? 0.35f : 0.85f;
            }
        }

        private static void EnsureSharedCanvas()
        {
            if (sharedNpcCanvas != null) return;

            var existing = GameObject.Find("Canvas_VillageNPC_Interactions");
            if (existing != null)
            {
                sharedNpcCanvas = existing.GetComponent<Canvas>();
                return;
            }

            var canvasGO = new GameObject("Canvas_VillageNPC_Interactions", typeof(Canvas), typeof(CanvasScaler));
            DontDestroyOnLoad(canvasGO);
            sharedNpcCanvas = canvasGO.GetComponent<Canvas>();
            sharedNpcCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            sharedNpcCanvas.sortingOrder = 460;

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            Font uiFont = Font.CreateDynamicFontFromOSFont("Segoe UI", 24) ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // 1. Minimalist Translucent Single-Color [E] Key Glyph (Planet of Lana style: NO text box!)
            var glyphGO = new GameObject("NPC_MinimalistKeyGlyph_E", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            glyphGO.transform.SetParent(canvasGO.transform, false);
            sharedKeyGlyphRect = glyphGO.GetComponent<RectTransform>();
            sharedKeyGlyphRect.anchorMin = sharedKeyGlyphRect.anchorMax = new Vector2(0.5f, 0.5f);
            sharedKeyGlyphRect.pivot = new Vector2(0.5f, 0.5f);
            sharedKeyGlyphRect.sizeDelta = new Vector2(44f, 44f);

            var fill = glyphGO.GetComponent<Image>();
            fill.color = new Color(1f, 1f, 1f, 0.08f);
            fill.raycastTarget = false;

            CreateEdge(sharedKeyGlyphRect, "T", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -1.5f), Vector2.zero);
            CreateEdge(sharedKeyGlyphRect, "B", new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 1.5f));
            CreateEdge(sharedKeyGlyphRect, "L", new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(1.5f, 0f));
            CreateEdge(sharedKeyGlyphRect, "R", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-1.5f, 0f), Vector2.zero);

            var keyTxtGO = new GameObject("Char", typeof(RectTransform), typeof(Text));
            keyTxtGO.transform.SetParent(sharedKeyGlyphRect, false);
            var ktr = keyTxtGO.GetComponent<RectTransform>();
            ktr.anchorMin = Vector2.zero;
            ktr.anchorMax = Vector2.one;
            ktr.offsetMin = Vector2.zero;
            ktr.offsetMax = Vector2.zero;
            var ktxt = keyTxtGO.GetComponent<Text>();
            ktxt.font = uiFont;
            ktxt.fontSize = 20;
            ktxt.alignment = TextAnchor.MiddleCenter;
            ktxt.color = new Color(1f, 1f, 1f, 0.74f);
            ktxt.text = "E";
            ktxt.raycastTarget = false;

            sharedKeyGlyphGroup = glyphGO.GetComponent<CanvasGroup>();
            sharedKeyGlyphGroup.alpha = 0f;
            sharedKeyGlyphGroup.blocksRaycasts = false;

            // 2. Clean Cinematic Dialogue Subtitle at Bottom Center
            var dlgGO = new GameObject("NPC_DialogueSubtitleRoot", typeof(RectTransform), typeof(CanvasGroup));
            dlgGO.transform.SetParent(canvasGO.transform, false);
            var dlgRect = dlgGO.GetComponent<RectTransform>();
            dlgRect.anchorMin = new Vector2(0.18f, 0.06f);
            dlgRect.anchorMax = new Vector2(0.82f, 0.17f);
            dlgRect.offsetMin = Vector2.zero;
            dlgRect.offsetMax = Vector2.zero;

            sharedDialogueGroup = dlgGO.GetComponent<CanvasGroup>();
            sharedDialogueGroup.alpha = 0f;
            sharedDialogueGroup.blocksRaycasts = false;

            var spkGO = new GameObject("SpeakerName", typeof(RectTransform), typeof(Text), typeof(Shadow));
            spkGO.transform.SetParent(dlgRect, false);
            var sr = spkGO.GetComponent<RectTransform>();
            sr.anchorMin = new Vector2(0f, 0.65f);
            sr.anchorMax = new Vector2(1f, 1f);
            sr.offsetMin = Vector2.zero;
            sr.offsetMax = Vector2.zero;
            sharedSpeakerText = spkGO.GetComponent<Text>();
            sharedSpeakerText.font = uiFont;
            sharedSpeakerText.fontSize = 15;
            sharedSpeakerText.fontStyle = FontStyle.Bold;
            sharedSpeakerText.alignment = TextAnchor.MiddleCenter;
            sharedSpeakerText.color = new Color(0.98f, 0.86f, 0.52f, 0.96f);
            var spkShad = spkGO.GetComponent<Shadow>();
            spkShad.effectColor = new Color(0f, 0f, 0f, 0.9f);
            spkShad.effectDistance = new Vector2(1.5f, -1.5f);

            var lineGO = new GameObject("DialogueLine", typeof(RectTransform), typeof(Text), typeof(Shadow));
            lineGO.transform.SetParent(dlgRect, false);
            var lr = lineGO.GetComponent<RectTransform>();
            lr.anchorMin = new Vector2(0f, 0f);
            lr.anchorMax = new Vector2(1f, 0.65f);
            lr.offsetMin = Vector2.zero;
            lr.offsetMax = Vector2.zero;
            sharedLineText = lineGO.GetComponent<Text>();
            sharedLineText.font = uiFont;
            sharedLineText.fontSize = 22;
            sharedLineText.alignment = TextAnchor.UpperCenter;
            sharedLineText.color = new Color(0.98f, 0.97f, 0.93f, 0.98f);
            var lineShad = lineGO.GetComponent<Shadow>();
            lineShad.effectColor = new Color(0f, 0f, 0f, 0.92f);
            lineShad.effectDistance = new Vector2(2f, -2f);
        }

        private static void CreateEdge(RectTransform parent, string name, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            var img = go.GetComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.48f);
            img.raycastTarget = false;
        }
    }
}
