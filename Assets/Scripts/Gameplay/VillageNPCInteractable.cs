using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using HoaVaMau.Test3D;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Interactive 3D Rigged Villager NPC for both 2.5D Overhaul and 3D Ca Mau 1954 Village.
    /// - Drives Humanoid Animator state (Idle, Wave, Talk, Sit, Walk patrol) + natural look-at toward An.
    /// - Displays ONLY a minimalist translucent single-color [E] key glyph above the NPC when An is within range.
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
        [Header("Voice Dialogue Audio (2D)")]
        [SerializeField] private AudioClip[] voiceClips = new AudioClip[0];
        private AudioSource voiceAudioSource;
        [SerializeField] private string defaultAnimState = "Idle";
        [SerializeField] private bool facePlayerWhenClose = true;
        [SerializeField] private float interactRadius = 2.0f;
        [SerializeField] private float patrolRangeX = 0f;
        [SerializeField] private float patrolSpeed = 1.15f;

        private Animator animator;
        private PlayerController player;
        private Player3DController player3D;
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

        public static bool HasActivePromptOwner => activePromptOwner != null;
        public AudioClip[] VoiceClips => voiceClips;

        public void ConfigureNPC(string speakerName, string[] lines, string initialAnimState = "Idle", float patrolDist = 0f, AudioClip[] clips = null)
        {
            npcName = speakerName;
            dialogueLines = lines;
            defaultAnimState = initialAnimState;
            patrolRangeX = patrolDist;
            if (clips != null) voiceClips = clips;
        }

        public void SetVoiceClips(AudioClip[] clips)
        {
            voiceClips = clips;
        }

        private void Awake()
        {
            animator = GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
            startX = transform.position.x;
            baseRotation = transform.rotation;
            EnsureSharedCanvas();
        }

        private void Start()
        {
            ResolvePlayer();
            TryPlayAnimatorState(defaultAnimState, Random.Range(0f, 0.85f), false);
            if (animator != null) animator.Update(0.02f);
            SnapLowestFootToGround();
        }

        /// <summary>
        /// Ensures the rigged 3D NPC's actual posed foot vertices rest cleanly on the ground collider/terrain surface.
        /// </summary>
        public void SnapLowestFootToGround()
        {
            var smr = GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr == null) return;

            Vector3 rayOrigin = transform.position + Vector3.up * 2.5f;
            float groundY = float.MinValue;
            RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, 8.0f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                var h = hits[i];
                if (h.collider == null || h.collider.transform.IsChildOf(transform)) continue;
                if (h.point.y > groundY) groundY = h.point.y;
            }
            if (groundY == float.MinValue) return;

            Mesh baked = new Mesh();
            smr.BakeMesh(baked);
            Vector3[] verts = baked.vertices;
            float minFootY = float.MaxValue;
            Transform smrTr = smr.transform;
            for (int i = 0; i < verts.Length; i++)
            {
                float wy = smrTr.TransformPoint(verts[i]).y;
                if (wy < minFootY) minFootY = wy;
            }
            if (Application.isPlaying) Destroy(baked);
            else DestroyImmediate(baked);

            if (minFootY < float.MaxValue)
            {
                float deltaY = (groundY + 0.015f) - minFootY;
                if (Mathf.Abs(deltaY) > 0.002f && Mathf.Abs(deltaY) < 1.5f)
                {
                    transform.position += new Vector3(0f, deltaY, 0f);
                }
            }
        }

        private void TryPlayAnimatorState(string stateName, float normalizedTimeOrTransition, bool crossFade)
        {
            if (animator == null || string.IsNullOrEmpty(stateName) || animator.runtimeAnimatorController == null) return;
            int hash = Animator.StringToHash(stateName);
            if (!animator.HasState(0, hash)) return;

            if (crossFade)
                animator.CrossFadeInFixedTime(hash, normalizedTimeOrTransition);
            else
                animator.Play(hash, 0, normalizedTimeOrTransition);
        }

        private void OnDisable()
        {
            if (voiceAudioSource != null && voiceAudioSource.isPlaying)
            {
                voiceAudioSource.Stop();
            }
            if (activePromptOwner == this)
            {
                activePromptOwner = null;
                if (sharedKeyGlyphGroup != null) sharedKeyGlyphGroup.alpha = 0f;
            }
            if (activeDialogueOwner == this)
            {
                activeDialogueOwner = null;
                if (sharedDialogueGroup != null) sharedDialogueGroup.alpha = 0f;
            }
        }

        private void ResolvePlayer()
        {
            if (player == null) player = Object.FindAnyObjectByType<PlayerController>();
            if (player == null && player3D == null) player3D = Object.FindAnyObjectByType<Player3DController>();
        }

        private void Update()
        {
            if (PauseMenuController.Instance != null && PauseMenuController.Instance.IsPaused) return;

            ResolvePlayer();
            bool is3DMode = (player == null && player3D != null);
            Transform pTr = player != null ? player.transform : (player3D != null ? player3D.transform : null);
            if (pTr == null) return;

            bool playerActive = player != null ? player.enabled : player3D.enabled;

            float planarDist;
            if (is3DMode)
            {
                planarDist = Vector2.Distance(
                    new Vector2(pTr.position.x, pTr.position.z),
                    new Vector2(transform.position.x, transform.position.z)
                );
            }
            else
            {
                planarDist = Mathf.Abs(pTr.position.x - transform.position.x);
            }

            float distY = Mathf.Abs(pTr.position.y - transform.position.y);
            bool inRange = playerActive && (planarDist <= interactRadius) && (distY <= 2.2f) && (dialogueLines != null && dialogueLines.Length > 0);

            // 1. Optional gentle village patrol when not talking
            if (!isTalking && !inRange && patrolRangeX > 0.25f)
            {
                UpdatePatrol(Time.deltaTime);
            }
            else if (facePlayerWhenClose && (inRange || isTalking) && defaultAnimState != "Sit")
            {
                if (is3DMode)
                {
                    Vector3 flatDir = pTr.position - transform.position;
                    flatDir.y = 0f;
                    if (flatDir.sqrMagnitude > 0.01f)
                    {
                        // Preserve upright humanoid rotation; skip if static mesh imported with -90 X pitch
                        if (Mathf.Abs(baseRotation.eulerAngles.x) < 10f || baseRotation.eulerAngles.x > 350f)
                        {
                            Quaternion targetRot = Quaternion.LookRotation(flatDir.normalized);
                            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5.0f);
                        }
                    }
                }
                else
                {
                    float dirToPlayer = Mathf.Sign(pTr.position.x - transform.position.x);
                    if (Mathf.Abs(dirToPlayer) > 0.01f)
                    {
                        float targetYaw = (dirToPlayer > 0f) ? 105f : -105f;
                        Quaternion targetRot = Quaternion.Euler(0f, targetYaw, 0f);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5.0f);
                    }
                }
            }
            else if (!isTalking && patrolRangeX <= 0.25f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, baseRotation, Time.deltaTime * 3.0f);
            }

            // 2. Manage minimalist [E] key glyph ownership (closest NPC takes priority)
            if (inRange)
            {
                float currentOwnerDist = float.MaxValue;
                if (activePromptOwner != null)
                {
                    Transform opt = activePromptOwner.transform;
                    currentOwnerDist = is3DMode
                        ? Vector2.Distance(new Vector2(pTr.position.x, pTr.position.z), new Vector2(opt.position.x, opt.position.z))
                        : Mathf.Abs(pTr.position.x - opt.position.x);
                }

                if (activePromptOwner == null || activePromptOwner == this || planarDist < currentOwnerDist - 0.15f)
                {
                    activePromptOwner = this;
                    UpdateKeyGlyphPosition();
                }

                if (activePromptOwner == this && KeybindingManager.IsActionPressed(GameAction.Interact))
                {
                    AdvanceDialogue(pTr, is3DMode);
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
                if (dialogueDisplayTimer <= 0f || planarDist > interactRadius + 1.5f)
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
                TryPlayAnimatorState("Idle", 0.25f, true);
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

        private void AdvanceDialogue(Transform pTr, bool is3DMode)
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
            dialogueDisplayTimer = 5.2f;

            // Face An toward the NPC
            if (player != null)
            {
                float dirToNpc = Mathf.Sign(transform.position.x - player.transform.position.x);
                if (Mathf.Abs(dirToNpc) > 0.01f) player.SetFacingDirection(dirToNpc);
            }
            else if (is3DMode && pTr != null)
            {
                Vector3 toNpc = transform.position - pTr.position;
                toNpc.y = 0f;
                if (toNpc.sqrMagnitude > 0.01f)
                {
                    pTr.rotation = Quaternion.LookRotation(toNpc.normalized);
                }
            }

            if (animator != null && defaultAnimState != "Sit")
            {
                string talkState = (currentLineIndex == 0) ? "Talk" : "Wave";
                TryPlayAnimatorState(talkState, 0.25f, true);
            }

            if (sharedSpeakerText != null) sharedSpeakerText.text = npcName.ToUpperInvariant();
            if (sharedLineText != null) sharedLineText.text = $"“{dialogueLines[currentLineIndex]}”";
            if (sharedDialogueGroup != null) sharedDialogueGroup.alpha = 1f;

            PlayVoiceClip(currentLineIndex);
        }

        private void PlayVoiceClip(int index)
        {
            if (voiceClips != null && index >= 0 && index < voiceClips.Length && voiceClips[index] != null)
            {
                if (voiceAudioSource == null)
                {
                    voiceAudioSource = GetComponent<AudioSource>();
                    if (voiceAudioSource == null) voiceAudioSource = gameObject.AddComponent<AudioSource>();
                    voiceAudioSource.playOnAwake = false;
                    voiceAudioSource.spatialBlend = 0f; // Crisp 2D voice
                }
                voiceAudioSource.Stop();
                voiceAudioSource.clip = voiceClips[index];
                voiceAudioSource.volume = 1f;
                voiceAudioSource.Play();
                if (voiceClips[index].length + 0.5f > dialogueDisplayTimer)
                {
                    dialogueDisplayTimer = voiceClips[index].length + 0.5f;
                }
            }
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
            if (voiceAudioSource != null && voiceAudioSource.isPlaying)
            {
                voiceAudioSource.Stop();
            }
            TryPlayAnimatorState(defaultAnimState, 0.35f, true);
        }

        private void UpdateKeyGlyphPosition()
        {
            EnsureSharedCanvas();
            if (sharedKeyGlyphRect == null || sharedKeyGlyphGroup == null) return;
            Camera cam = Camera.main;
            if (cam == null) return;

            float bob = Mathf.Sin(Time.unscaledTime * 2.6f) * 0.06f;
            Vector3 worldPos = transform.position + new Vector3(0f, 2.05f + bob, 0f);
            Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
            if (screenPos.z > 0f)
            {
                sharedKeyGlyphRect.position = screenPos;
                sharedKeyGlyphGroup.alpha = isTalking ? 0.35f : 0.88f;
            }
        }

        private static void EnsureSharedCanvas()
        {
            if (sharedNpcCanvas != null && sharedKeyGlyphRect != null && sharedDialogueGroup != null) return;

            var existing = GameObject.Find("Canvas_VillageNPC_Interactions");
            if (existing != null)
            {
                if (Application.isPlaying) Destroy(existing);
                else DestroyImmediate(existing);
            }

            var canvasGO = new GameObject("Canvas_VillageNPC_Interactions", typeof(Canvas), typeof(CanvasScaler));
            if (Application.isPlaying) DontDestroyOnLoad(canvasGO);
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
            fill.color = new Color(0.06f, 0.09f, 0.14f, 0.25f);
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
            ktxt.fontStyle = FontStyle.Bold;
            ktxt.alignment = TextAnchor.MiddleCenter;
            ktxt.color = new Color(1f, 1f, 1f, 0.88f);
            ktxt.text = "E";
            ktxt.raycastTarget = false;

            sharedKeyGlyphGroup = glyphGO.GetComponent<CanvasGroup>();
            sharedKeyGlyphGroup.alpha = 0f;
            sharedKeyGlyphGroup.blocksRaycasts = false;

            // 2. Clean Cinematic Dialogue Subtitle at Bottom Center
            var dlgGO = new GameObject("NPC_DialogueSubtitleRoot", typeof(RectTransform), typeof(CanvasGroup));
            dlgGO.transform.SetParent(canvasGO.transform, false);
            var dlgRect = dlgGO.GetComponent<RectTransform>();
            dlgRect.anchorMin = new Vector2(0.10f, 0.05f);
            dlgRect.anchorMax = new Vector2(0.90f, 0.18f);
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
            img.color = new Color(1f, 1f, 1f, 0.54f);
            img.raycastTarget = false;
        }
    }
}
