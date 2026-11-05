#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using HoaVaMau.Dialogue;
using HoaVaMau.Test3D;

namespace HoaVaMau.EditorTools
{
    public static class FixRiceVisualsAndSetupGenshinDialogue
    {
        private const string PrefabPath = "Assets/Prefabs/Environment/Rice/PF_Rice_RowStrip_4m.prefab";
        private const string MeshyLOD1MeshPath = "Assets/Models/RicePlants/SM_Rice_Plant_Meshy_LOD_1.fbx";
        private const string MatRiceMeshyPath = "Assets/Materials/RicePlants/Mat_Rice_Meshy.mat";
        private const string MatRiceTripoPath = "Assets/Materials/RicePlants/Mat_Rice_Tripo.mat";

        [MenuItem("Hoa va Mau/Cutscene & Rice/Apply Genshin Dialogue & Fix Rice Visuals", false, 0)]
        public static void Execute()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (!activeScene.path.Contains("CaMau_1954_Village"))
            {
                EditorSceneManager.OpenScene("Assets/Scenes/CaMau_1954_Village.unity");
            }

            Debug.Log("<color=#3b82f6>[Cutscene & Rice]</color> Bắt đầu xử lý: Thay thế lúa vàng nhẹ & Cài đặt Genshin Conversation...");

            // 1. SỬA RENDER LÚA -> LÚA VÀNG NHẸ (SOFT GOLDEN RICE)
            FixRiceVisuals();

            // 2. THIẾT LẬP HỆ THỐNG GENSHIN / BOTW DIALOGUE CONVERSATION
            SetupGenshinDialogueSystem();

            // 3. GẮN HỘI THOẠI & CUTSCENE CHO 12 NPC TRONG LÀNG
            ConfigureAll12VillageNPCs();

            // 4. LƯU SCENE VÀ ASSETS
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();

            Debug.Log("<color=#10b981>[Cutscene & Rice]</color> HOÀN TẤT THÀNH CÔNG! Đã cập nhật cánh đồng lúa vàng nhẹ và hệ thống Cutscene Genshin/BOTW cho 12 NPC.");
        }

        private static void FixRiceVisuals()
        {
            var matMeshy = AssetDatabase.LoadAssetAtPath<Material>(MatRiceMeshyPath);
            if (matMeshy == null)
            {
                Debug.LogError($"[FixRice] Không tìm thấy {MatRiceMeshyPath}");
                return;
            }

            Mesh meshyLOD1Mesh = null;
            var subAssets = AssetDatabase.LoadAllAssetsAtPath(MeshyLOD1MeshPath);
            foreach (var asset in subAssets)
            {
                if (asset is Mesh m)
                {
                    meshyLOD1Mesh = m;
                    break;
                }
            }

            if (meshyLOD1Mesh == null)
            {
                Debug.LogError($"[FixRice] Không tìm thấy Mesh trong {MeshyLOD1MeshPath}");
                return;
            }

            var matTripo = AssetDatabase.LoadAssetAtPath<Material>(MatRiceTripoPath);
            if (matTripo != null)
            {
                Undo.RecordObject(matTripo, "Sync Rice Tripo Material to Soft Golden");
                if (matMeshy.mainTexture != null) matTripo.mainTexture = matMeshy.mainTexture;
                if (matMeshy.HasProperty("_BaseShadowColor") && matTripo.HasProperty("_BaseShadowColor"))
                    matTripo.SetColor("_BaseShadowColor", matMeshy.GetColor("_BaseShadowColor"));
                if (matMeshy.HasProperty("_MidColor") && matTripo.HasProperty("_MidColor"))
                    matTripo.SetColor("_MidColor", matMeshy.GetColor("_MidColor"));
                if (matMeshy.HasProperty("_TipColor") && matTripo.HasProperty("_TipColor"))
                    matTripo.SetColor("_TipColor", matMeshy.GetColor("_TipColor"));
                if (matMeshy.HasProperty("_ShimmerColor") && matTripo.HasProperty("_ShimmerColor"))
                    matTripo.SetColor("_ShimmerColor", matMeshy.GetColor("_ShimmerColor"));
                if (matMeshy.HasProperty("_ColorStrength") && matTripo.HasProperty("_ColorStrength"))
                    matTripo.SetFloat("_ColorStrength", matMeshy.GetFloat("_ColorStrength"));
                EditorUtility.SetDirty(matTripo);
            }

            // 1. CẬP NHẬT PREFAB
            var prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (prefabRoot != null)
            {
                int modifiedTufts = 0;
                for (int i = 0; i < prefabRoot.transform.childCount; i++)
                {
                    var tuft = prefabRoot.transform.GetChild(i);
                    for (int j = 0; j < tuft.childCount; j++)
                    {
                        var lod = tuft.GetChild(j);
                        if (lod.name == "LOD1")
                        {
                            var mf = lod.GetComponent<MeshFilter>();
                            var mr = lod.GetComponent<MeshRenderer>();
                            if (mf != null) mf.sharedMesh = meshyLOD1Mesh;
                            if (mr != null) mr.sharedMaterial = matMeshy;
                            modifiedTufts++;
                        }
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
                PrefabUtility.UnloadPrefabContents(prefabRoot);
                Debug.Log($"<color=#10b981>[FixRice]</color> Đã cập nhật Prefab {PrefabPath} ({modifiedTufts} khóm lúa chuyển sang Meshy LOD1 vàng nhẹ)!");
            }

            // 2. CẬP NHẬT TẤT CẢ INSTANCES TRONG SCENE
            var terrain = GameObject.Find("CaMau_1954_Village/Environment/Terrain_3D");
            int updatedSceneCount = 0;
            if (terrain != null)
            {
                for (int i = 0; i < terrain.transform.childCount; i++)
                {
                    var plot = terrain.transform.GetChild(i);
                    if (!plot.name.StartsWith("Paddy_Plot")) continue;
                    for (int j = 0; j < plot.childCount; j++)
                    {
                        var tuft = plot.GetChild(j);
                        if (!tuft.name.StartsWith("Tuft_Row")) continue;
                        for (int k = 0; k < tuft.childCount; k++)
                        {
                            var lod = tuft.GetChild(k);
                            if (lod.name == "LOD1")
                            {
                                var mf = lod.GetComponent<MeshFilter>();
                                var mr = lod.GetComponent<MeshRenderer>();
                                if (mf != null) mf.sharedMesh = meshyLOD1Mesh;
                                if (mr != null) mr.sharedMaterial = matMeshy;
                                updatedSceneCount++;
                            }
                        }
                    }
                }
            }
            Debug.Log($"<color=#10b981>[FixRice]</color> Đã cập nhật {updatedSceneCount} đối tượng LOD1 trong Scene sang lúa vàng nhẹ hoàn chỉnh!");
        }

        private static void SetupGenshinDialogueSystem()
        {
            var systemGO = GameObject.Find("InteractionSystem_Genshin_Dialogue");
            if (systemGO == null)
            {
                systemGO = new GameObject("InteractionSystem_Genshin_Dialogue");
            }

            var parent = GameObject.Find("CaMau_1954_Village/Gameplay_Systems") ?? GameObject.Find("CaMau_1954_Village");
            if (parent != null) systemGO.transform.parent = parent.transform;

            var convo = systemGO.GetComponent<GenshinConversationSystem>();
            if (convo == null) convo = systemGO.AddComponent<GenshinConversationSystem>();

            var canvasGO = GameObject.Find("Canvas_Genshin_Dialogue_Master");
            if (canvasGO != null) Object.DestroyImmediate(canvasGO);

            canvasGO = new GameObject("Canvas_Genshin_Dialogue_Master", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            canvasGO.transform.parent = systemGO.transform;

            var dialogueCanvas = canvasGO.GetComponent<Canvas>();
            dialogueCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            dialogueCanvas.sortingOrder = 500;

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var canvasGroup = canvasGO.GetComponent<CanvasGroup>();

            Font mainFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // 1. Cinematic Black Letterboxes
            var topBoxGO = new GameObject("Cinematic_Letterbox_Top", typeof(RectTransform), typeof(Image));
            topBoxGO.transform.SetParent(canvasGO.transform, false);
            var topLetterbox = topBoxGO.GetComponent<RectTransform>();
            topLetterbox.anchorMin = new Vector2(0f, 1f);
            topLetterbox.anchorMax = new Vector2(1f, 1f);
            topLetterbox.pivot = new Vector2(0.5f, 1f);
            topLetterbox.sizeDelta = new Vector2(0f, 95f);
            topLetterbox.anchoredPosition = new Vector2(0f, 120f);
            var topImg = topBoxGO.GetComponent<Image>();
            topImg.color = Color.black;
            topImg.raycastTarget = false;

            var botBoxGO = new GameObject("Cinematic_Letterbox_Bottom", typeof(RectTransform), typeof(Image));
            botBoxGO.transform.SetParent(canvasGO.transform, false);
            var bottomLetterbox = botBoxGO.GetComponent<RectTransform>();
            bottomLetterbox.anchorMin = new Vector2(0f, 0f);
            bottomLetterbox.anchorMax = new Vector2(1f, 0f);
            bottomLetterbox.pivot = new Vector2(0.5f, 0f);
            bottomLetterbox.sizeDelta = new Vector2(0f, 95f);
            bottomLetterbox.anchoredPosition = new Vector2(0f, -120f);
            var botImg = botBoxGO.GetComponent<Image>();
            botImg.color = Color.black;
            botImg.raycastTarget = false;

            // 2. Dialogue Card
            var cardGO = new GameObject("Genshin_DialogueCard", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            cardGO.transform.SetParent(canvasGO.transform, false);
            var dialogueCard = cardGO.GetComponent<RectTransform>();
            dialogueCard.anchorMin = new Vector2(0.5f, 0f);
            dialogueCard.anchorMax = new Vector2(0.5f, 0f);
            dialogueCard.pivot = new Vector2(0.5f, 0f);
            dialogueCard.sizeDelta = new Vector2(1280f, 230f);
            dialogueCard.anchoredPosition = new Vector2(0f, 75f);

            var cardImg = cardGO.GetComponent<Image>();
            cardImg.color = new Color(0.07f, 0.09f, 0.13f, 0.94f);
            cardImg.raycastTarget = true;

            var dialogueCardGroup = cardGO.GetComponent<CanvasGroup>();
            dialogueCardGroup.alpha = 0f;

            // Gold Trim
            var goldTrimGO = new GameObject("Gold_Accent_Trim", typeof(RectTransform), typeof(Image));
            goldTrimGO.transform.SetParent(cardGO.transform, false);
            var trimRT = goldTrimGO.GetComponent<RectTransform>();
            trimRT.anchorMin = new Vector2(0f, 1f);
            trimRT.anchorMax = new Vector2(1f, 1f);
            trimRT.sizeDelta = new Vector2(0f, 3.5f);
            trimRT.anchoredPosition = new Vector2(0f, -1.75f);
            var trimImg = goldTrimGO.GetComponent<Image>();
            trimImg.color = new Color(0.92f, 0.78f, 0.38f, 0.92f);
            trimImg.raycastTarget = false;

            // Speaker Name
            var nameTagGO = new GameObject("Speaker_Nameplate", typeof(RectTransform));
            nameTagGO.transform.SetParent(cardGO.transform, false);
            var nameRT = nameTagGO.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0f, 1f);
            nameRT.anchorMax = new Vector2(0f, 1f);
            nameRT.pivot = new Vector2(0f, 0f);
            nameRT.anchoredPosition = new Vector2(38f, -42f);
            nameRT.sizeDelta = new Vector2(500f, 38f);

            var spkTxtGO = new GameObject("NameText", typeof(RectTransform), typeof(Text), typeof(Shadow));
            spkTxtGO.transform.SetParent(nameTagGO.transform, false);
            var spkRT = spkTxtGO.GetComponent<RectTransform>();
            spkRT.anchorMin = Vector2.zero;
            spkRT.anchorMax = Vector2.one;
            var speakerNameText = spkTxtGO.GetComponent<Text>();
            speakerNameText.font = mainFont;
            speakerNameText.fontSize = 24;
            speakerNameText.fontStyle = FontStyle.Bold;
            speakerNameText.color = new Color(0.98f, 0.85f, 0.42f, 1.0f);
            speakerNameText.text = "【 TRƯỞNG THÔN NĂM 】";
            var spkShad = spkTxtGO.GetComponent<Shadow>();
            spkShad.effectColor = new Color(0f, 0f, 0f, 0.85f);
            spkShad.effectDistance = new Vector2(1.5f, -1.5f);

            // Speaker Title
            var titleTxtGO = new GameObject("TitleText", typeof(RectTransform), typeof(Text));
            titleTxtGO.transform.SetParent(cardGO.transform, false);
            var titleRT = titleTxtGO.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0f, 1f);
            titleRT.anchorMax = new Vector2(0f, 1f);
            titleRT.pivot = new Vector2(0f, 0f);
            titleRT.anchoredPosition = new Vector2(40f, -68f);
            titleRT.sizeDelta = new Vector2(500f, 24f);
            var speakerTitleText = titleTxtGO.GetComponent<Text>();
            speakerTitleText.font = mainFont;
            speakerTitleText.fontSize = 16;
            speakerTitleText.color = new Color(0.82f, 0.80f, 0.74f, 0.88f);
            speakerTitleText.text = "Bậc cao niên xóm Rạch Gốc";

            // Dialogue Content
            var contentGO = new GameObject("Dialogue_Content", typeof(RectTransform), typeof(Text), typeof(Shadow));
            contentGO.transform.SetParent(cardGO.transform, false);
            var contRT = contentGO.GetComponent<RectTransform>();
            contRT.anchorMin = new Vector2(0f, 0f);
            contRT.anchorMax = new Vector2(1f, 1f);
            contRT.offsetMin = new Vector2(45f, 38f);
            contRT.offsetMax = new Vector2(-45f, -76f);
            var dialogueContentText = contentGO.GetComponent<Text>();
            dialogueContentText.font = mainFont;
            dialogueContentText.fontSize = 22;
            dialogueContentText.lineSpacing = 1.35f;
            dialogueContentText.color = new Color(0.98f, 0.97f, 0.93f, 0.98f);
            dialogueContentText.text = "";
            var contShad = contentGO.GetComponent<Shadow>();
            contShad.effectColor = new Color(0f, 0f, 0f, 0.85f);
            contShad.effectDistance = new Vector2(1.5f, -1.5f);

            // Continue Prompt
            var contPromptGO = new GameObject("Continue_Prompt", typeof(RectTransform), typeof(Text));
            contPromptGO.transform.SetParent(cardGO.transform, false);
            var cpRT = contPromptGO.GetComponent<RectTransform>();
            cpRT.anchorMin = new Vector2(1f, 0f);
            cpRT.anchorMax = new Vector2(1f, 0f);
            cpRT.pivot = new Vector2(1f, 0f);
            cpRT.anchoredPosition = new Vector2(-36f, 18f);
            cpRT.sizeDelta = new Vector2(200f, 30f);
            var continuePromptText = contPromptGO.GetComponent<Text>();
            continuePromptText.font = mainFont;
            continuePromptText.fontSize = 18;
            continuePromptText.alignment = TextAnchor.MiddleRight;
            continuePromptText.color = new Color(0.92f, 0.78f, 0.38f, 0.95f);
            continuePromptText.text = "[E] Tiếp tục ▼";

            // 3. Choice Options Container
            var choiceContainer = new GameObject("Choice_Options_Container", typeof(RectTransform), typeof(VerticalLayoutGroup));
            choiceContainer.transform.SetParent(canvasGO.transform, false);
            var chcRT = choiceContainer.GetComponent<RectTransform>();
            chcRT.anchorMin = new Vector2(1f, 0f);
            chcRT.anchorMax = new Vector2(1f, 0f);
            chcRT.pivot = new Vector2(1f, 0f);
            chcRT.anchoredPosition = new Vector2(-80f, 320f);
            chcRT.sizeDelta = new Vector2(620f, 200f);

            var vlg = choiceContainer.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 14f;
            vlg.childAlignment = TextAnchor.MiddleRight;
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;
            choiceContainer.SetActive(false);

            // 4. Floating In-World Prompt
            var promptGO = new GameObject("NPC_InWorld_Prompt_Genshin", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            promptGO.transform.SetParent(canvasGO.transform, false);
            var worldPromptRect = promptGO.GetComponent<RectTransform>();
            worldPromptRect.sizeDelta = new Vector2(230f, 44f);

            var pFill = promptGO.GetComponent<Image>();
            pFill.color = new Color(0.08f, 0.11f, 0.16f, 0.88f);
            pFill.raycastTarget = false;

            var pBorder = new GameObject("P_Border", typeof(RectTransform), typeof(Outline));
            pBorder.transform.SetParent(promptGO.transform, false);
            var pOut = pBorder.GetComponent<Outline>();
            pOut.effectColor = new Color(0.85f, 0.72f, 0.35f, 0.75f);
            pOut.effectDistance = new Vector2(1.2f, 1.2f);

            var keyBox = new GameObject("KeyGlyph", typeof(RectTransform), typeof(Image));
            keyBox.transform.SetParent(promptGO.transform, false);
            var kbRT = keyBox.GetComponent<RectTransform>();
            kbRT.anchorMin = new Vector2(0f, 0.5f);
            kbRT.anchorMax = new Vector2(0f, 0.5f);
            kbRT.pivot = new Vector2(0f, 0.5f);
            kbRT.anchoredPosition = new Vector2(8f, 0f);
            kbRT.sizeDelta = new Vector2(28f, 28f);
            var kbImg = keyBox.GetComponent<Image>();
            kbImg.color = new Color(0.92f, 0.78f, 0.38f, 0.95f);

            var kTxtGO = new GameObject("E_Char", typeof(RectTransform), typeof(Text));
            kTxtGO.transform.SetParent(keyBox.transform, false);
            var ktRT = kTxtGO.GetComponent<RectTransform>();
            ktRT.anchorMin = Vector2.zero;
            ktRT.anchorMax = Vector2.one;
            var worldPromptKeyText = kTxtGO.GetComponent<Text>();
            worldPromptKeyText.font = mainFont;
            worldPromptKeyText.fontSize = 18;
            worldPromptKeyText.fontStyle = FontStyle.Bold;
            worldPromptKeyText.alignment = TextAnchor.MiddleCenter;
            worldPromptKeyText.color = Color.black;
            worldPromptKeyText.text = "E";

            var nameLblGO = new GameObject("NPC_Name", typeof(RectTransform), typeof(Text));
            nameLblGO.transform.SetParent(promptGO.transform, false);
            var nlRT = nameLblGO.GetComponent<RectTransform>();
            nlRT.anchorMin = new Vector2(0f, 0.5f);
            nlRT.anchorMax = new Vector2(1f, 1f);
            nlRT.offsetMin = new Vector2(44f, 0f);
            nlRT.offsetMax = new Vector2(-10f, 0f);
            var worldPromptNameText = nameLblGO.GetComponent<Text>();
            worldPromptNameText.font = mainFont;
            worldPromptNameText.fontSize = 14;
            worldPromptNameText.fontStyle = FontStyle.Bold;
            worldPromptNameText.alignment = TextAnchor.MiddleLeft;
            worldPromptNameText.color = new Color(0.98f, 0.85f, 0.42f, 1f);
            worldPromptNameText.text = "Người Dân Làng";

            var actLblGO = new GameObject("Action_Name", typeof(RectTransform), typeof(Text));
            actLblGO.transform.SetParent(promptGO.transform, false);
            var alRT = actLblGO.GetComponent<RectTransform>();
            alRT.anchorMin = new Vector2(0f, 0f);
            alRT.anchorMax = new Vector2(1f, 0.5f);
            alRT.offsetMin = new Vector2(44f, 0f);
            alRT.offsetMax = new Vector2(-10f, 0f);
            var worldPromptActionText = actLblGO.GetComponent<Text>();
            worldPromptActionText.font = mainFont;
            worldPromptActionText.fontSize = 13;
            worldPromptActionText.alignment = TextAnchor.MiddleLeft;
            worldPromptActionText.color = new Color(0.85f, 0.85f, 0.85f, 0.85f);
            worldPromptActionText.text = "Trò chuyện";

            var worldPromptGroup = promptGO.GetComponent<CanvasGroup>();
            worldPromptGroup.alpha = 0f;
            worldPromptGroup.blocksRaycasts = false;

            convo.BindUI(
                dialogueCanvas, canvasGroup, topLetterbox, bottomLetterbox,
                dialogueCard, dialogueCardGroup, speakerNameText, speakerTitleText,
                dialogueContentText, continuePromptText, choiceContainer,
                worldPromptRect, worldPromptGroup, worldPromptKeyText, worldPromptNameText, worldPromptActionText
            );

            EditorUtility.SetDirty(convo);
            EditorUtility.SetDirty(systemGO);
            Debug.Log("<color=#10b981>[GenshinDialogue]</color> Đã khởi tạo và liên kết hoàn hảo Canvas_Genshin_Dialogue_Master!");
        }

        private static void ConfigureAll12VillageNPCs()
        {
            var npcsRoot = GameObject.Find("CaMau_1954_Village/NPCs") ?? GameObject.Find("NPCs");
            if (npcsRoot == null)
            {
                Debug.LogWarning("[GenshinDialogue] Không tìm thấy thư mục NPCs trong Scene!");
                return;
            }

            // 1. Trưởng Thôn Năm
            SetupNPC(npcsRoot, "NPC_Elderly_VillageChief",
                "Trưởng Thôn Năm",
                "Bậc cao niên xóm Rạch Gốc",
                new string[]
                {
                    "Chào cháu An! Lúa năm nay trúng mùa lớn, hạt nào hạt nấy mẩy vàng óng ả khắp đồng.",
                    "Xóm mình đang phơi lúa mới để đóng bao gửi ra miền Bắc theo đoàn tàu tập kết ngoài cửa Sông Đốc đó."
                },
                new DialogueChoice[]
                {
                    new DialogueChoice
                    {
                        choiceText = "Cháu thấy đồng lúa vàng trải dài đẹp quá thưa bác!",
                        responseLine = "Đúng rồi cháu, đất đai Nam Bộ phù sa màu mỡ nuôi nấng người dân mình. Giữ lấy hạt ngọc trời này nghe con.",
                        responseAnim = "OldMan_Talking"
                    },
                    new DialogueChoice
                    {
                        choiceText = "Bác cho cháu hỏi tàu tập kết bao giờ rời bến?",
                        responseLine = "Sắp rồi con à. Chú Bảy ngoài bến đò đang hối hả chuyển những bao gạo cuối cùng lên ghe đấy.",
                        responseAnim = "OldMan_Talking"
                    },
                    new DialogueChoice
                    {
                        choiceText = "Cháu xin phép đi dạo tiếp quanh xóm ạ.",
                        responseLine = "Đi cẩn thận nghe con, nhớ ghé qua má dặn chuyện nhà trước khi trời tối nghen.",
                        responseAnim = "OldMan_Talking"
                    }
                },
                "OldMan_Idle", "OldMan_Talking");

            // 2. Má của Bé An
            SetupNPC(npcsRoot, "NPC_Mother_An",
                "Má của An",
                "Má Năm - Xóm Bờ Kênh",
                new string[]
                {
                    "An đó hả con? Đi chơi đâu từ sáng tới giờ, lúa chín vàng cả cánh đồng rồi kìa.",
                    "Về nhà ăn chén cơm gạo mới má vừa nấu, dẻo và thơm lừng luôn nè con."
                },
                new DialogueChoice[]
                {
                    new DialogueChoice
                    {
                        choiceText = "Dạ má, cơm gạo mới thơm quá má ơi!",
                        responseLine = "Ừ, ăn no rồi phụ các bác ngoài sân phơi lúa nghe con. Nhớ đừng chạy nhảy làm đổ nia thóc.",
                        responseAnim = "Female_Talking_01"
                    },
                    new DialogueChoice
                    {
                        choiceText = "Má ơi, chiều nay con ra bến xem tàu tập kết nghen?",
                        responseLine = "Được chớ, nhớ rủ thằng Minh đi chung cho có bạn, chớ có nhảy giỡn dưới mé nước sâu nghe chưa!",
                        responseAnim = "Female_Talking_02"
                    }
                },
                "Female_Waving", "Female_Talking_01");

            // 3. Bác Ba Phi
            SetupNPC(npcsRoot, "NPC_Elderly_UncleBa",
                "Bác Ba Phi",
                "Huyền thoại kể chuyện rừng U Minh",
                new string[]
                {
                    "Hahaha! Thằng nhỏ An lại đây bác Ba kể chuyện bắt cá sấu khổng lồ ở lung Tràm cho nghe!",
                    "Bữa đó sấu nổi đặc như bèo, bác chỉ cần quăng một sợi dây mây là lôi thẳng lên bờ một con dài cả chục thước!"
                },
                new DialogueChoice[]
                {
                    new DialogueChoice
                    {
                        choiceText = "Bác Ba kể thiệt không hay bác lại nói dóc đó?",
                        responseLine = "Tổ cha mày! Bác Ba mày mà thèm nói dóc hả! Hỏi cả xóm Rạch Gốc này ai mà không phục tài bác!",
                        responseAnim = "OldMan_Talking"
                    },
                    new DialogueChoice
                    {
                        choiceText = "Con thích nghe chuyện bác Ba cưỡi rùa vàng hơn!",
                        responseLine = "Hè hè, để bữa nào rảnh bác dẫn mày vô lung Tràm, chỉ cho con rùa to bằng cái nia phơi lúa!",
                        responseAnim = "OldMan_Talking"
                    }
                },
                "OldMan_Idle", "OldMan_Talking");

            // 4. Chú Bảy Chèo Đò
            SetupNPC(npcsRoot, "NPC_Ferryman_UncleBay",
                "Chú Bảy Chèo Đò",
                "Chủ xuồng ba lá - Bến Sông Đốc",
                new string[]
                {
                    "Xuồng ba lá chuẩn bị tách bến sang cồn dừa nước rồi đây con ơi!",
                    "Gió biển Cà Mau thổi lồng lộng, con nước ròng đưa ghe thuyền tấp nập về cửa biển."
                },
                new DialogueChoice[]
                {
                    new DialogueChoice
                    {
                        choiceText = "Chú Bảy cho con quá giang qua bên kia sông với!",
                        responseLine = "Lên xuồng ngồi im nghe con, sông sâu nước xiết, chớ thò tay quơ nước kẻo trượt chân!",
                        responseAnim = "Waving_01"
                    },
                    new DialogueChoice
                    {
                        choiceText = "Chú ơi, ngoài biển Sông Đốc tàu tập kết to lắm hả chú?",
                        responseLine = "To như quả núi con à! Tàu chở cán bộ đồng bào ra Bắc, cờ đỏ sao vàng bay rực rỡ cả góc trời!",
                        responseAnim = "Waving_02"
                    }
                },
                "Waving_01", "Waving_02");

            // 5. Thằng Minh Bạn Thân
            SetupNPC(npcsRoot, "NPC_Friend_Minh",
                "Thằng Minh",
                "Bạn nối khố của Bé An",
                new string[]
                {
                    "Ê An! Đi bắt cào cào trong ruộng lúa vàng với tao không?",
                    "Hôm qua tao thấy tổ chim sẻ đồng núp dưới gốc lúa bự chà bá luôn á!"
                },
                new DialogueChoice[]
                {
                    new DialogueChoice
                    {
                        choiceText = "Đi liền! Nhưng coi chừng dẫm gãy lúa của bác Năm nha!",
                        responseLine = "Biết rồi, tao đi men theo bờ mẫu ruộng thôi, không có dẫm bậy bạ đâu mà sợ!",
                        responseAnim = "Waving_02"
                    },
                    new DialogueChoice
                    {
                        choiceText = "Má tao đang gọi về ăn cơm rồi, lát gặp nghen!",
                        responseLine = "Ờ, ăn lẹ rồi chạy ra đầu cầu khỉ đầu xóm hú tao một tiếng!",
                        responseAnim = "Waving_01"
                    }
                },
                "Waving_01", "Waving_03");

            // 6. Chị Tư Gánh Nước
            SetupNPC(npcsRoot, "NPC_Female_WaterCarrier",
                "Chị Tư Gánh Nước",
                "Người gánh nước giếng ngọt xóm",
                new string[]
                {
                    "Tránh đường cho chị gánh đôi thùng nước mưa ngọt về đổ lu nghe An!",
                    "Nước mưa mùa này trong veo ngọt lịm, nấu trà cho các cụ uống là số một."
                },
                new DialogueChoice[]
                {
                    new DialogueChoice
                    {
                        choiceText = "Để em phụ chị xách một thùng nghen!",
                        responseLine = "Thôi khỏi chú em nhỏ xíu, xách nặng đau lưng má la chị chết! Lo đi chơi ngoan đi.",
                        responseAnim = "Female_Talking_01"
                    }
                },
                "Female_Waving", "Female_Talking_01");

            // 7. Chú Sáu Làm Vườn
            SetupNPC(npcsRoot, "NPC_Villager_Gardener",
                "Chú Sáu Làm Vườn",
                "Nông dân vườn chuối & dừa nước",
                new string[]
                {
                    "Mấy quày dừa nước bên kia mương ngọt nước cơm dày dữ lắm An ơi.",
                    "Chiều nay ghé chú chặt cho một trái dừa nước ăn cho mát dạ."
                },
                new DialogueChoice[]
                {
                    new DialogueChoice
                    {
                        choiceText = "Dạ, dừa nước chấm đường ăn ngon hết sẩy!",
                        responseLine = "Hahaha, đúng điệu dân Cà Mau rồi đó cháu!",
                        responseAnim = "Male_Talking_01"
                    }
                },
                "Male_Waving", "Male_Talking_01");

            // 8. Bé Tèo (Trẻ con chơi đùa - Rủ chạy đua)
            SetupNPC(npcsRoot, "NPC_Child_Playing_Boy",
                "Bé Tèo",
                "Trẻ xóm trong",
                new string[]
                {
                    "Anh An ơi! Chạy đua ra đầu ngõ với em không?",
                    "Em chạy nhanh như gió thoảng, đố anh bắt kịp em đó!"
                },
                new DialogueChoice[]
                {
                    new DialogueChoice
                    {
                        choiceText = "Chạy đua thì chạy đua, anh chấp em ba bước luôn!",
                        responseLine = "Hi hi, chuẩn bị nghen anh An: Một... Hai... Ba... Chạy!",
                        responseAnim = "Waving_01"
                    },
                    new DialogueChoice
                    {
                        choiceText = "Để lát nữa nghen, anh đang có việc cần ra bến đò.",
                        responseLine = "Dạ, lát nữa anh về nhớ chạy đua với em đó nha!",
                        responseAnim = "Waving_02"
                    }
                },
                "Waving_01", "Waving_02");

            // 9. Bé Mận
            SetupNPC(npcsRoot, "NPC_Child_Playing_Girl",
                "Bé Mận",
                "Bé gái xóm chợ Rạch Gốc",
                new string[]
                {
                    "Anh An coi em kết hoa dại cài lên nón lá đẹp chưa nè!",
                    "Hoa màu vàng như màu hạt lúa chín ngoài đồng vậy đó."
                },
                new DialogueChoice[]
                {
                    new DialogueChoice
                    {
                        choiceText = "Khéo tay quá, như nón công chúa miền Tây!",
                        responseLine = "Hi hi, em cảm ơn anh An nhiều nghen!",
                        responseAnim = "Waving_03"
                    }
                },
                "Waving_02", "Waving_03");

            // 10. Bé Út Chập Chững
            SetupNPC(npcsRoot, "NPC_Baby_Toddler",
                "Bé Út",
                "Em bé ngoan của xóm",
                new string[]
                {
                    "A... anh An... bế Út... bế Út ra xem chim sẻ đồng!",
                    "Út thích hoa vàng lúa vàng lắm!"
                },
                new DialogueChoice[]
                {
                    new DialogueChoice
                    {
                        choiceText = "Út đứng ngoan kẻo ngã nghen, lát anh hái hoa cho!",
                        responseLine = "Dạ... Út ngoan mà!",
                        responseAnim = "Waving_01"
                    }
                },
                "Waving_01", "Waving_02");

            // 11. Dì Hai Vẫy Tay
            SetupNPC(npcsRoot, "NPC_Villager_Waving_Mother",
                "Dì Hai",
                "Hậu phương tiễn đoàn tập kết",
                new string[]
                {
                    "Bà con mang thêm bánh tét, gạo mới ra bến Sông Đốc gửi các anh bộ đội nghen!",
                    "Đi mạnh giỏi, ngày thống nhất hòa bình lại về với quê hương Cà Mau!"
                },
                new DialogueChoice[]
                {
                    new DialogueChoice
                    {
                        choiceText = "Dì Hai ơi, mọi người sẽ sớm trở về sum họp phải không dì?",
                        responseLine = "Chắc chắn rồi cháu! Đồng bào miền Nam lúc nào cũng hướng về ngày hội ngộ.",
                        responseAnim = "Female_Talking_01"
                    }
                },
                "Female_Waving", "Female_Talking_01");

            // 12. Cụ Ông Tiễn Đưa
            SetupNPC(npcsRoot, "NPC_Villager_Farewell_Elder",
                "Cụ Bảy Già",
                "Lão thành cách mạng xóm kênh",
                new string[]
                {
                    "Cây tràm, cây đước Cà Mau bám rễ sâu như tấm lòng người dân Nam Bộ vậy cháu à.",
                    "Dù có đi đâu xa, ngửi mùi rơm rạ lúa chín là nhớ da diết quê nhà."
                },
                new DialogueChoice[]
                {
                    new DialogueChoice
                    {
                        choiceText = "Cháu sẽ luôn ghi nhớ lời cụ dạy ạ.",
                        responseLine = "Cháu là đứa trẻ ngoan. Lớn lên phụ giúp quê hương đất nước nghen cháu.",
                        responseAnim = "OldMan_Talking"
                    }
                },
                "OldMan_Idle", "OldMan_Talking");

            Debug.Log("<color=#10b981>[GenshinDialogue]</color> Đã thiết lập hoàn chỉnh tính cách, hội thoại và cutscene cho toàn bộ 12 NPC!");
        }

        private static void SetupNPC(GameObject root, string goName, string name, string title, string[] openings, DialogueChoice[] choices, string defAnim, string talkAnim)
        {
            var child = root.transform.Find(goName);
            if (child == null)
            {
                Debug.LogWarning($"[GenshinDialogue] Không tìm thấy {goName}");
                return;
            }

            var comp = child.GetComponent<GenshinNPCInteractable>();
            if (comp == null) comp = child.gameObject.AddComponent<GenshinNPCInteractable>();

            comp.Configure(name, title, openings, choices, defAnim, talkAnim);
            EditorUtility.SetDirty(child.gameObject);
        }
    }
}
#endif
