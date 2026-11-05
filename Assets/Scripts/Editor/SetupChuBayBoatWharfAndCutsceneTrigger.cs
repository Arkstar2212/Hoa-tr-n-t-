using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using HoaVaMau.Dialogue;
using HoaVaMau.Test3D;

namespace HoaVaMau.EditorScripts
{
    public static class SetupChuBayBoatWharfAndCutsceneTrigger
    {
        [MenuItem("Tools/Hoa Đất Đỏ/Bến Xuồng - Thiết lập Chú Bảy & Kích hoạt Cutscene Thuyền", false, 110)]
        public static void ExecuteSetup()
        {
            // 1. Ensure Cutscene_EndGame is registered in EditorBuildSettings
            EnsureCutsceneInBuildSettings();

            // 2. Ensure CaMau_1954_Village scene is active
            var currentScene = EditorSceneManager.GetActiveScene();
            if (currentScene.name != "CaMau_1954_Village")
            {
                currentScene = EditorSceneManager.OpenScene("Assets/Scenes/CaMau_1954_Village.unity");
            }

            // 3. Move Chú Bảy to Pier Dock Tip & configure Genshin dialogue
            SetupChuBayNPC();

            // 4. Place 3D Xuồng Ba Lá moored in water at dock landing
            SetupMooredXuongBaLa();

            // 5. Mark scene dirty & save
            EditorSceneManager.MarkSceneDirty(currentScene);
            EditorSceneManager.SaveScene(currentScene);

            Debug.Log("<color=#34d399>[SetupChuBay]</color> Bến xuồng Cà Mau 1954: Đã đưa Chú Bảy ra đầu cầu tàu, neo xuồng ba lá nổi trên mặt nước, và kết nối lựa chọn chuyển cảnh Cutscene_EndGame thành công!");
        }

        private static void EnsureCutsceneInBuildSettings()
        {
            string cutscenePath = "Assets/Scenes/Cutscene_EndGame.unity";
            var existingScenes = EditorBuildSettings.scenes;
            bool alreadyExists = false;

            for (int i = 0; i < existingScenes.Length; i++)
            {
                if (existingScenes[i].path == cutscenePath)
                {
                    alreadyExists = true;
                    if (!existingScenes[i].enabled)
                    {
                        existingScenes[i].enabled = true;
                        EditorBuildSettings.scenes = existingScenes;
                    }
                    break;
                }
            }

            if (!alreadyExists)
            {
                var newScenes = new List<EditorBuildSettingsScene>(existingScenes)
                {
                    new EditorBuildSettingsScene(cutscenePath, true)
                };
                EditorBuildSettings.scenes = newScenes.ToArray();
                Debug.Log($"<color=#38bdf8>[BuildSettings]</color> Đã đăng ký '{cutscenePath}' vào EditorBuildSettings.");
            }
        }

        private static void SetupChuBayNPC()
        {
            var npcGO = GameObject.Find("NPC_04_ChuBayCheoDo");
            if (npcGO == null)
            {
                Debug.LogError("[SetupChuBay] Không tìm thấy 'NPC_04_ChuBayCheoDo' trong scene!");
                return;
            }

            Undo.RecordObject(npcGO.transform, "Move Chu Bay to Pier");
            // Pier dock tip is at Z = -40.5m, Y = 0.89m, overlooking open ocean
            npcGO.transform.position = new Vector3(56.0f, 0.89f, -40.5f);
            npcGO.transform.rotation = Quaternion.Euler(0f, 10f, 0f); // Facing north towards Bé An walking down the walkway

            var interactable = npcGO.GetComponent<GenshinNPCInteractable>();
            if (interactable == null)
            {
                interactable = Undo.AddComponent<GenshinNPCInteractable>(npcGO);
            }

            Undo.RecordObject(interactable, "Configure Chu Bay Dialogue");

            string[] openings = new string[]
            {
                "Xuồng ba lá chuẩn bị tách bến sang cồn dừa nước rồi đây con ơi!",
                "Gió biển Cà Mau thổi lồng lộng, con nước ròng đưa ghe thuyền tấp nập về cửa biển."
            };

            var choiceList = new DialogueChoice[]
            {
                new DialogueChoice()
                {
                    choiceText = "Chú Bảy cho con quá giang qua bên kia sông với!",
                    responseLine = "Lên xuồng ngồi im nghe con, sông sâu nước xiết, chớ thò tay quơ nước kẻo trượt chân!",
                    responseAnim = "Male_Talking_01",
                    targetSceneName = ""
                },
                new DialogueChoice()
                {
                    choiceText = "Chú ơi, ngoài biển Sông Đốc tàu tập kết to lắm hả chú?",
                    responseLine = "To như quả núi con à! Tàu chở cán bộ đồng bào ra Bắc, cờ đỏ sao vàng bay rực rỡ cả góc trời!",
                    responseAnim = "Male_Talking_02",
                    targetSceneName = ""
                },
                new DialogueChoice()
                {
                    choiceText = "Chú Bảy ơi, chở con ra đoàn tàu tập kết ra khơi với!",
                    responseLine = "Được rồi con, lên xuồng mau kẻo tàu nhổ neo! Chuyến tàu này chở theo bao niềm tin và ước vọng của đồng bào miền Nam!",
                    responseAnim = "Male_Waving",
                    targetSceneName = "Cutscene_EndGame"
                }
            };

            interactable.Configure(
                name: "Chú Bảy Chèo Đò",
                title: "Chủ xuồng ba lá - Bến Sông Đốc",
                openings: openings,
                choiceList: choiceList,
                defaultAnim: "Male_Talking_02",
                talkAnim: "Male_Talking_01"
            );

            // Configure custom conversation camera offset so camera captures Chu Bay, open sea, and boat
            var so = new SerializedObject(interactable);
            var customCamProp = so.FindProperty("customCamOffset");
            if (customCamProp != null)
            {
                customCamProp.vector3Value = new Vector3(-0.6f, 0.45f, 0.3f);
            }
            var radiusProp = so.FindProperty("interactRadius");
            if (radiusProp != null)
            {
                radiusProp.floatValue = 3.8f;
            }
            var promptProp = so.FindProperty("promptOffset");
            if (promptProp != null)
            {
                promptProp.vector3Value = new Vector3(0f, 2.05f, 0f);
            }
            so.ApplyModifiedProperties();

            // Ensure collider is enabled
            var col = npcGO.GetComponent<CapsuleCollider>();
            if (col != null)
            {
                col.center = new Vector3(0f, 0.9f, 0f);
                col.radius = 0.35f;
                col.height = 1.8f;
            }

            Debug.Log($"<color=#34d399>[SetupChuBay]</color> Đã đặt Chú Bảy tại {npcGO.transform.position} và gán 3 lựa chọn thoại (Choice 2 -> Cutscene_EndGame).");
        }

        private static void SetupMooredXuongBaLa()
        {
            var boatsGroup = GameObject.Find("CaMau_1954_Village/Environment/WaterSystem_3D/Harbor_Boats_And_Sampans");
            if (boatsGroup == null)
            {
                var waterSys = GameObject.Find("CaMau_1954_Village/Environment/WaterSystem_3D");
                if (waterSys != null)
                {
                    boatsGroup = new GameObject("Harbor_Boats_And_Sampans");
                    boatsGroup.transform.SetParent(waterSys.transform, false);
                }
                else
                {
                    boatsGroup = GameObject.Find("08_Ben_Xuong");
                }
            }

            string boatName = "XuongBaLa_Moored_ChuBay";
            GameObject boatGO = GameObject.Find(boatName);
            if (boatGO == null && boatsGroup != null)
            {
                var existing = boatsGroup.transform.Find(boatName);
                if (existing != null) boatGO = existing.gameObject;
            }

            var optimizedModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Optimized/SM_Xuong_go_BlenderOptimized.fbx");
            var woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Xuong go.mat");

            if (boatGO == null)
            {
                if (optimizedModel != null)
                {
                    boatGO = (GameObject)PrefabUtility.InstantiatePrefab(optimizedModel);
                    boatGO.name = boatName;
                    Undo.RegisterCreatedObjectUndo(boatGO, "Create Moored Boat");
                }
                else
                {
                    var fallbackPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Xuong_go.prefab");
                    if (fallbackPrefab != null)
                    {
                        boatGO = (GameObject)PrefabUtility.InstantiatePrefab(fallbackPrefab);
                        boatGO.name = boatName;
                        Undo.RegisterCreatedObjectUndo(boatGO, "Create Moored Boat");
                    }
                }
            }

            if (boatGO == null)
            {
                Debug.LogError("[SetupChuBay] Không tìm thấy model hay prefab xuồng ba lá!");
                return;
            }

            Undo.RecordObject(boatGO.transform, "Configure Moored Boat Transform");
            if (boatsGroup != null)
            {
                boatGO.transform.SetParent(boatsGroup.transform, true);
            }

            // Moored directly in water at open dock landing tip (Z = -44.8m)
            // Water Y is -0.58m; boat floats at Y = -0.28m with gunwales rising above water
            boatGO.transform.position = new Vector3(56.0f, -0.28f, -44.8f);
            boatGO.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // Upright orientation (X is boat length across dock)
            boatGO.transform.localScale = new Vector3(4.8f, 4.8f, 4.8f);

            // Assign proper wood material
            if (woodMat != null)
            {
                var rends = boatGO.GetComponentsInChildren<Renderer>();
                foreach (var r in rends)
                {
                    r.sharedMaterial = woodMat;
                }
            }

            // Add gentle floating & bobbing
            var floater = boatGO.GetComponent<FloatingBoat3D>();
            if (floater == null)
            {
                floater = Undo.AddComponent<FloatingBoat3D>(boatGO);
            }

            // Configure bobbing parameters for gentle mooring
            var soFloater = new SerializedObject(floater);
            var bobHeightProp = soFloater.FindProperty("bobHeight");
            if (bobHeightProp != null) bobHeightProp.floatValue = 0.04f;
            var bobSpeedProp = soFloater.FindProperty("bobSpeed");
            if (bobSpeedProp != null) bobSpeedProp.floatValue = 1.1f;
            var pitchProp = soFloater.FindProperty("pitchAngle");
            if (pitchProp != null) pitchProp.floatValue = 1.2f;
            var rollProp = soFloater.FindProperty("rollAngle");
            if (rollProp != null) rollProp.floatValue = 1.8f;
            soFloater.ApplyModifiedProperties();

            Debug.Log($"<color=#34d399>[SetupChuBay]</color> Đã neo xuồng ba lá {boatGO.name} tại {boatGO.transform.position} (scale={boatGO.transform.localScale}).");
        }
    }
}
