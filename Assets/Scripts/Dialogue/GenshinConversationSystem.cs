using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HoaVaMau.Test3D;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HoaVaMau.Dialogue
{
    /// <summary>
    /// Hệ thống hội thoại và Cutscene phong cách Genshin Impact / Breath of the Wild (BOTW):
    /// - Khi đến gần NPC: hiển thị HUD prompt tương tác [E] phong cách phiêu lưu.
    /// - Khi bấm [E]: Camera lượn chuyển động mượt mà (smooth cutscene framing) bắt góc qua vai (over-the-shoulder),
    ///   hai dải đen điện ảnh (cinematic letterboxes) trượt vào, An và NPC xoay mặt đối diện nhau.
    /// - Hộp thoại bán trong suốt viền chỉ vàng đặc trưng Genshin với huy hiệu Tên NPC (Nameplate Tag),
    ///   hiệu ứng chữ gõ (typewriter), và các lựa chọn rẽ nhánh câu trả lời của Bé An (◆ Choices).
    /// - Khi kết thúc: Camera trả mượt về góc nhìn tự do, gỡ khóa điều khiển lập tức cho Bé An.
    /// </summary>
    public class GenshinConversationSystem : MonoBehaviour
    {
        public static GenshinConversationSystem Instance { get; private set; }

        [Header("Runtime References")]
        [SerializeField] private Player3DController player3D;
        [SerializeField] private ThirdPersonOrbitCamera orbitCam;

        [Header("UI Canvas & Letterboxes")]
        [SerializeField] private Canvas dialogueCanvas;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform topLetterbox;
        [SerializeField] private RectTransform bottomLetterbox;

        [Header("Dialogue Card")]
        [SerializeField] private RectTransform dialogueCard;
        [SerializeField] private CanvasGroup dialogueCardGroup;
        [SerializeField] private Text speakerNameText;
        [SerializeField] private Text speakerTitleText;
        [SerializeField] private Text dialogueContentText;
        [SerializeField] private Text continuePromptText;

        [Header("Choices & World Prompt")]
        [SerializeField] private GameObject choiceContainer;
        [SerializeField] private RectTransform worldPromptRect;
        [SerializeField] private CanvasGroup worldPromptGroup;
        [SerializeField] private Text worldPromptKeyText;
        [SerializeField] private Text worldPromptNameText;
        [SerializeField] private Text worldPromptActionText;

        private Camera mainCamera;
        private GenshinNPCInteractable closestNPC;
        private GenshinNPCInteractable activeNPC;

        private bool isInConversation = false;
        private int currentOpeningLineIndex = 0;
        private bool isTypewriting = false;
        private Coroutine typewriterCoroutine;
        private string targetDialogueLine = "";
        private bool showingChoices = false;
        private bool showingResponseLine = false;
        private DialogueChoice activeSelectedChoice = null;

        private List<Button> activeChoiceButtons = new List<Button>();

        public bool IsInConversation => isInConversation;

        public void BindUI(
            Canvas canvas, CanvasGroup cGroup, RectTransform topBox, RectTransform botBox,
            RectTransform card, CanvasGroup cardGrp, Text spkName, Text spkTitle, Text content, Text continuePrompt,
            GameObject choiceCont, RectTransform promptRect, CanvasGroup promptGrp, Text pKey, Text pName, Text pAction)
        {
            dialogueCanvas = canvas;
            canvasGroup = cGroup;
            topLetterbox = topBox;
            bottomLetterbox = botBox;
            dialogueCard = card;
            dialogueCardGroup = cardGrp;
            speakerNameText = spkName;
            speakerTitleText = spkTitle;
            dialogueContentText = content;
            continuePromptText = continuePrompt;
            choiceContainer = choiceCont;
            worldPromptRect = promptRect;
            worldPromptGroup = promptGrp;
            worldPromptKeyText = pKey;
            worldPromptNameText = pName;
            worldPromptActionText = pAction;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            ResolveReferences();
        }

        private void Start()
        {
            ResolveReferences();
            if (dialogueCardGroup != null) dialogueCardGroup.alpha = 0f;
            if (topLetterbox != null) topLetterbox.anchoredPosition = new Vector2(0f, 120f);
            if (bottomLetterbox != null) bottomLetterbox.anchoredPosition = new Vector2(0f, -120f);
            if (worldPromptGroup != null) worldPromptGroup.alpha = 0f;
            if (choiceContainer != null) choiceContainer.SetActive(false);
        }

        private void ResolveReferences()
        {
            if (player3D == null) player3D = Object.FindAnyObjectByType<Player3DController>();
            if (orbitCam == null) orbitCam = Object.FindAnyObjectByType<ThirdPersonOrbitCamera>();
            if (mainCamera == null) mainCamera = Camera.main;
        }

        private void Update()
        {
            ResolveReferences();
            if (player3D == null) return;

            // Đọc Input [E], [Space], [1], [2], [3]
            bool interactPressed = false;
            bool spacePressed = false;
            bool num1Pressed = false;
            bool num2Pressed = false;
            bool num3Pressed = false;

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.eKey.wasPressedThisFrame) interactPressed = true;
                if (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame) spacePressed = true;
                if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) num1Pressed = true;
                if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) num2Pressed = true;
                if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) num3Pressed = true;
            }
            var gp = Gamepad.current;
            if (gp != null && (gp.buttonSouth.wasPressedThisFrame || gp.buttonWest.wasPressedThisFrame))
            {
                interactPressed = true;
            }
#else
            if (Input.GetKeyDown(KeyCode.E)) interactPressed = true;
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) spacePressed = true;
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) num1Pressed = true;
            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) num2Pressed = true;
            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) num3Pressed = true;
#endif

            if (!isInConversation)
            {
                FindClosestNPC();
                UpdateWorldPrompt();

                if (closestNPC != null && interactPressed)
                {
                    StartConversation(closestNPC);
                }
            }
            else
            {
                UpdateCutsceneCamera();

                if (showingChoices)
                {
                    if (num1Pressed && activeChoiceButtons.Count > 0) activeChoiceButtons[0].onClick.Invoke();
                    else if (num2Pressed && activeChoiceButtons.Count > 1) activeChoiceButtons[1].onClick.Invoke();
                    else if (num3Pressed && activeChoiceButtons.Count > 2) activeChoiceButtons[2].onClick.Invoke();
                }
                else
                {
                    if (interactPressed || spacePressed)
                    {
                        AdvanceConversation();
                    }
                }
            }
        }

        private void FindClosestNPC()
        {
            var allNPCs = Object.FindObjectsByType<GenshinNPCInteractable>(FindObjectsSortMode.None);
            closestNPC = null;
            float bestDist = float.MaxValue;
            Vector3 pPos = player3D.transform.position;

            foreach (var npc in allNPCs)
            {
                if (npc == null || !npc.gameObject.activeInHierarchy) continue;
                float d = Vector3.Distance(pPos, npc.transform.position);
                if (d <= npc.InteractRadius && d < bestDist)
                {
                    bestDist = d;
                    closestNPC = npc;
                }
            }
        }

        private void UpdateWorldPrompt()
        {
            if (worldPromptGroup == null || worldPromptRect == null) return;

            if (closestNPC != null && mainCamera != null && !isInConversation)
            {
                Vector3 worldTarget = closestNPC.transform.position + closestNPC.PromptOffset;
                Vector3 screenPoint = mainCamera.WorldToScreenPoint(worldTarget);

                if (screenPoint.z > 0.1f)
                {
                    worldPromptRect.position = screenPoint;
                    worldPromptGroup.alpha = Mathf.MoveTowards(worldPromptGroup.alpha, 1.0f, Time.deltaTime * 6f);
                    if (worldPromptNameText != null) worldPromptNameText.text = closestNPC.NPCName;
                    if (worldPromptActionText != null) worldPromptActionText.text = "Trò chuyện";
                    return;
                }
            }

            worldPromptGroup.alpha = Mathf.MoveTowards(worldPromptGroup.alpha, 0.0f, Time.deltaTime * 8f);
        }

        public void StartConversation(GenshinNPCInteractable npc)
        {
            if (npc == null || isInConversation) return;

            ResolveReferences();

            isInConversation = true;
            activeNPC = npc;
            currentOpeningLineIndex = 0;
            showingChoices = false;
            showingResponseLine = false;
            activeSelectedChoice = null;

            // Khóa di chuyển Bé An
            if (player3D != null) player3D.IsInputLocked = true;

            // Ẩn HUD prompt
            if (worldPromptGroup != null) worldPromptGroup.alpha = 0f;

            // Xoay đối diện nhau
            RotateCharactersTowardsEachOther();

            // Khởi động Camera Cutscene góc qua vai
            SetupDialogueCameraFraming();

            // Bật Animation nói chuyện của NPC
            if (!string.IsNullOrEmpty(activeNPC.TalkAnimState))
            {
                activeNPC.PlayAnim(activeNPC.TalkAnimState);
            }

            // Hiện Letterboxes và Dialogue UI
            StartCoroutine(AnimateEnterCutscene());

            // Gán thông tin người nói
            if (speakerNameText != null) speakerNameText.text = $"【 {activeNPC.NPCName.ToUpper()} 】";
            if (speakerTitleText != null) speakerTitleText.text = activeNPC.NPCTitle;

            // Gõ dòng đầu tiên
            if (activeNPC.OpeningLines != null && activeNPC.OpeningLines.Length > 0)
            {
                ShowDialogueLine(activeNPC.OpeningLines[0]);
            }
            else
            {
                ShowDialogueLine("...");
            }
        }

        private void RotateCharactersTowardsEachOther()
        {
            if (player3D == null || activeNPC == null) return;

            Vector3 toNpc = (activeNPC.transform.position - player3D.transform.position);
            toNpc.y = 0f;
            if (toNpc.sqrMagnitude > 0.01f)
            {
                player3D.transform.rotation = Quaternion.LookRotation(toNpc.normalized);
            }

            Vector3 toPlayer = (player3D.transform.position - activeNPC.transform.position);
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > 0.01f)
            {
                activeNPC.transform.rotation = Quaternion.LookRotation(toPlayer.normalized);
            }
        }

        private void SetupDialogueCameraFraming()
        {
            if (orbitCam == null || player3D == null || activeNPC == null) return;

            Vector3 pPos = player3D.transform.position;
            Vector3 nPos = activeNPC.transform.position;
            Vector3 forward = (nPos - pPos).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

            Vector3 camPos = pPos + right * 1.45f - forward * 0.95f + Vector3.up * 1.45f;
            camPos += activeNPC.CustomCamOffset;

            Vector3 targetLook = nPos + Vector3.up * 1.35f;
            Quaternion camRot = Quaternion.LookRotation((targetLook - camPos).normalized);

            orbitCam.StartDialogueFocus(camPos, camRot, 46.0f);
        }

        private void UpdateCutsceneCamera()
        {
            if (orbitCam == null || player3D == null || activeNPC == null) return;

            float breathX = Mathf.Sin(Time.time * 0.85f) * 0.02f;
            float breathY = Mathf.Cos(Time.time * 0.65f) * 0.02f;

            Vector3 pPos = player3D.transform.position;
            Vector3 nPos = activeNPC.transform.position;
            Vector3 forward = (nPos - pPos).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

            Vector3 camPos = pPos + right * (1.45f + breathX) - forward * 0.95f + Vector3.up * (1.45f + breathY);
            camPos += activeNPC.CustomCamOffset;

            Vector3 targetLook = nPos + Vector3.up * 1.35f;
            Quaternion camRot = Quaternion.LookRotation((targetLook - camPos).normalized);

            orbitCam.UpdateDialogueFocus(camPos, camRot);
        }

        private void ShowDialogueLine(string line)
        {
            targetDialogueLine = line;
            if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = StartCoroutine(TypewriterRoutine(line));
        }

        private IEnumerator TypewriterRoutine(string line)
        {
            isTypewriting = true;
            if (continuePromptText != null) continuePromptText.gameObject.SetActive(false);
            if (dialogueContentText != null) dialogueContentText.text = "";

            float charsPerSecond = 38f;
            float delay = 1f / charsPerSecond;

            for (int i = 0; i <= line.Length; i++)
            {
                if (dialogueContentText != null) dialogueContentText.text = line.Substring(0, i);
                yield return new WaitForSeconds(delay);
            }

            isTypewriting = false;
            if (continuePromptText != null)
            {
                continuePromptText.gameObject.SetActive(true);
                continuePromptText.text = "[E] Tiếp tục ▼";
            }
        }

        private void AdvanceConversation()
        {
            if (isTypewriting)
            {
                if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
                isTypewriting = false;
                if (dialogueContentText != null) dialogueContentText.text = targetDialogueLine;
                if (continuePromptText != null)
                {
                    continuePromptText.gameObject.SetActive(true);
                    continuePromptText.text = "[E] Tiếp tục ▼";
                }
                return;
            }

            if (showingResponseLine)
            {
                if (activeSelectedChoice != null && !string.IsNullOrEmpty(activeSelectedChoice.targetSceneName))
                {
                    string target = activeSelectedChoice.targetSceneName;
                    activeSelectedChoice = null;
                    StartCoroutine(TransitionToSceneRoutine(target));
                    return;
                }
                EndConversation();
                return;
            }

            if (activeNPC.OpeningLines != null && currentOpeningLineIndex < activeNPC.OpeningLines.Length - 1)
            {
                currentOpeningLineIndex++;
                ShowDialogueLine(activeNPC.OpeningLines[currentOpeningLineIndex]);
                return;
            }

            if (activeNPC.Choices != null && activeNPC.Choices.Length > 0 && !showingChoices)
            {
                DisplayChoiceOptions();
                return;
            }

            EndConversation();
        }

        private void DisplayChoiceOptions()
        {
            showingChoices = true;
            if (choiceContainer != null) choiceContainer.SetActive(true);
            if (continuePromptText != null) continuePromptText.gameObject.SetActive(false);

            foreach (var b in activeChoiceButtons)
            {
                if (b != null) Destroy(b.gameObject);
            }
            activeChoiceButtons.Clear();

            Font uiFont = Font.CreateDynamicFontFromOSFont("Segoe UI", 20) ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            for (int i = 0; i < activeNPC.Choices.Length; i++)
            {
                int index = i;
                DialogueChoice choice = activeNPC.Choices[i];

                var btnGO = new GameObject($"ChoiceButton_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                btnGO.transform.SetParent(choiceContainer.transform, false);

                var rt = btnGO.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(620f, 50f);

                var img = btnGO.GetComponent<Image>();
                img.color = new Color(0.10f, 0.13f, 0.18f, 0.94f);

                var btn = btnGO.GetComponent<Button>();
                var colors = btn.colors;
                colors.normalColor = new Color(0.10f, 0.13f, 0.18f, 0.94f);
                colors.highlightedColor = new Color(0.25f, 0.30f, 0.40f, 0.98f);
                colors.pressedColor = new Color(0.92f, 0.78f, 0.38f, 0.95f);
                btn.colors = colors;

                var borderGO = new GameObject("Border", typeof(RectTransform), typeof(Outline));
                borderGO.transform.SetParent(btnGO.transform, false);
                var outl = borderGO.GetComponent<Outline>();
                outl.effectColor = new Color(0.90f, 0.76f, 0.38f, 0.75f);
                outl.effectDistance = new Vector2(1.5f, 1.5f);

                var txtGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
                txtGO.transform.SetParent(btnGO.transform, false);
                var trt = txtGO.GetComponent<RectTransform>();
                trt.anchorMin = Vector2.zero;
                trt.anchorMax = Vector2.one;
                trt.offsetMin = new Vector2(22f, 0f);
                trt.offsetMax = new Vector2(-22f, 0f);

                var txt = txtGO.GetComponent<Text>();
                txt.font = uiFont;
                txt.fontSize = 19;
                txt.alignment = TextAnchor.MiddleLeft;
                txt.color = new Color(0.98f, 0.96f, 0.90f, 1f);
                txt.text = $"◆  [{i + 1}]  {choice.choiceText}";

                btn.onClick.AddListener(() => OnChoiceSelected(index));
                activeChoiceButtons.Add(btn);
            }
        }

        private void OnChoiceSelected(int index)
        {
            if (activeNPC == null || index < 0 || index >= activeNPC.Choices.Length) return;

            showingChoices = false;
            showingResponseLine = true;
            if (choiceContainer != null) choiceContainer.SetActive(false);

            DialogueChoice choice = activeNPC.Choices[index];
            activeSelectedChoice = choice;

            if (!string.IsNullOrEmpty(choice.responseAnim))
            {
                activeNPC.PlayAnim(choice.responseAnim);
            }

            ShowDialogueLine(choice.responseLine);
        }

        private IEnumerator TransitionToSceneRoutine(string sceneName)
        {
            if (player3D != null) player3D.IsInputLocked = true;
            if (choiceContainer != null) choiceContainer.SetActive(false);

            float duration = 0.35f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

                if (topLetterbox != null) topLetterbox.anchoredPosition = new Vector2(0f, Mathf.Lerp(0f, 120f, t));
                if (bottomLetterbox != null) bottomLetterbox.anchoredPosition = new Vector2(0f, Mathf.Lerp(0f, -120f, t));
                if (dialogueCardGroup != null) dialogueCardGroup.alpha = 1f - t;

                yield return null;
            }

            isInConversation = false;

            HoaVaMau.Gameplay.SceneTransitionBotWLoader.LoadSceneWithBotWScreen(
                sceneName,
                "ĐANG RA BẾN TẬP KẾT XUỒNG BA LÁ..."
            );
        }

        public void EndConversation()
        {
            if (!isInConversation) return;

            StartCoroutine(AnimateExitCutscene());
        }

        private IEnumerator AnimateEnterCutscene()
        {
            float duration = 0.32f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

                if (topLetterbox != null) topLetterbox.anchoredPosition = new Vector2(0f, Mathf.Lerp(120f, 0f, t));
                if (bottomLetterbox != null) bottomLetterbox.anchoredPosition = new Vector2(0f, Mathf.Lerp(-120f, 0f, t));
                if (dialogueCardGroup != null) dialogueCardGroup.alpha = t;

                yield return null;
            }

            if (topLetterbox != null) topLetterbox.anchoredPosition = Vector2.zero;
            if (bottomLetterbox != null) bottomLetterbox.anchoredPosition = Vector2.zero;
            if (dialogueCardGroup != null) dialogueCardGroup.alpha = 1f;
        }

        private IEnumerator AnimateExitCutscene()
        {
            float duration = 0.28f;
            float elapsed = 0f;

            if (choiceContainer != null) choiceContainer.SetActive(false);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

                if (topLetterbox != null) topLetterbox.anchoredPosition = new Vector2(0f, Mathf.Lerp(0f, 120f, t));
                if (bottomLetterbox != null) bottomLetterbox.anchoredPosition = new Vector2(0f, Mathf.Lerp(0f, -120f, t));
                if (dialogueCardGroup != null) dialogueCardGroup.alpha = 1f - t;

                yield return null;
            }

            isInConversation = false;

            if (player3D != null) player3D.IsInputLocked = false;
            if (orbitCam != null) orbitCam.EndDialogueFocus();

            if (activeNPC != null)
            {
                activeNPC.PlayAnim(activeNPC.DefaultAnimState);
                activeNPC = null;
            }
        }
    }
}
