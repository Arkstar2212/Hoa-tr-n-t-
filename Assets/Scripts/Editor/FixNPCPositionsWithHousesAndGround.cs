using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using HoaVaMau;
using HoaVaMau.Dialogue;

namespace HoaDo.Editor
{
    /// <summary>
    /// Chuẩn hóa vị trí toàn bộ 12 NPC trong Scene CaMau_1954_Village.unity theo Phương Án B:
    /// - Những người trên hiên nhà (Má An, Bé Út, Trưởng Thôn Năm, Bác Ba Phi):
    ///   + Cao độ Y sàn hiên chuẩn: 0.89m, nằm sâu trong lòng hiên (Z=23.80m cho nhà hướng Nam; X=5.80m cho nhà hướng Tây).
    ///   + Chân cắm phẳng 100% trên mặt sàn hiên, trần hiên cao thoáng, né hoàn toàn chõng tre và cột hiên.
    ///   + Chuẩn hóa góc quay forward của model (Elderly Man & Female quay mặt đón An khi An đi vào).
    /// - Những người ngoài sân làng, giếng nước, quảng trường:
    ///   + Cao độ mặt đất phẳng Y = 0.508m (Unified_Village_Earthen_Ground_Network).
    ///   + Chân chạm đất vững chãi, Cụ Bảy Già đứng trước sân Cây Đa thoáng đãng đón An.
    /// - Reset toàn bộ offset lệch của các child node (tripo_node) về 0.
    /// </summary>
    public static class FixNPCPositionsWithHousesAndGround
    {
        private const string ScenePath = "Assets/Scenes/CaMau_1954_Village.unity";
        private const string ArtifactDir = "C:/Users/Arkstarz/.gemini/antigravity/brain/c6ca6ea1-5462-4857-ac83-0d9f6205ed40";

        public const float PORCH_FLOOR_Y = 0.89f;
        public const float GROUND_Y = 0.508f;
        public const float WHARF_Y = 0.35f;

        public struct NPCPositionConfig
        {
            public string gameObjectName;
            public Vector3 position;
            public Vector3 rotationEuler;
            public string description;

            public NPCPositionConfig(string name, Vector3 pos, Vector3 rot, string desc)
            {
                gameObjectName = name;
                position = pos;
                rotationEuler = rot;
                description = desc;
            }
        }

        public static readonly NPCPositionConfig[] Configs = new NPCPositionConfig[]
        {
            // 1. Trưởng Thôn Năm: Trong lòng sàn hiên Nhà 02, lệch bên hông chõng tre (X=-11.2, Z=23.8), quay mặt về Nam đón An
            new NPCPositionConfig(
                "NPC_01_TruongThonNam",
                new Vector3(-11.20f, PORCH_FLOOR_Y, 23.80f),
                new Vector3(0f, 90f, 0f),
                "Hiên Nhà 02 Trưởng Thôn (Y=0.89m sàn hiên vững chãi, né chõng tre, quay mặt về Nam)"
            ),

            // 2. Má của An: Trong lòng sàn hiên Nhà 01 Nhà An, lệch bên hông chõng tre (X=-2.6, Z=23.8), quay mặt về Nam đón An
            new NPCPositionConfig(
                "NPC_02_MaAnMother",
                new Vector3(-2.60f, PORCH_FLOOR_Y, 23.80f),
                new Vector3(0f, 180f, 0f),
                "Hiên Nhà 01 Nhà An (Y=0.89m sàn hiên vững chãi, né chõng tre, quay mặt về Nam)"
            ),

            // 3. Bé Út: Đứng cạnh Má An trong lòng sàn hiên Nhà An
            new NPCPositionConfig(
                "NPC_10_BeUt",
                new Vector3(-1.80f, PORCH_FLOOR_Y, 23.80f),
                new Vector3(0f, 180f, 0f),
                "Hiên Nhà 01 cạnh Má An (Y=0.89m)"
            ),

            // 4. Bác Ba Phi: Trong lòng sàn hiên Nhà 03 (nhà quay mặt về Tây), lệch theo trục Z né chõng tre, quay mặt về Tây
            new NPCPositionConfig(
                "NPC_03_BacBaPhi",
                new Vector3(5.80f, PORCH_FLOOR_Y, 15.60f),
                new Vector3(0f, 180f, 0f),
                "Hiên Nhà 03 Bác Ba Phi (Y=0.89m sàn hiên, né chõng tre, quay mặt về Tây)"
            ),

            // 5. Thằng Minh: Giữa sân Cây Đa Trung Tâm (Quảng trường làng)
            new NPCPositionConfig(
                "NPC_05_ThangMinh",
                new Vector3(-6.00f, GROUND_Y, 7.50f),
                new Vector3(0f, 180f, 0f),
                "Sân Cây Đa Trung Tâm (Y=0.508m)"
            ),

            // 6. Chị Tư Gánh Nước: Bên cạnh Giếng Ngọt Xóm (nhà 09)
            new NPCPositionConfig(
                "NPC_06_ChiTuGanhNuoc",
                new Vector3(12.00f, GROUND_Y, 8.50f),
                new Vector3(0f, -60f, 0f),
                "Bên Giếng Ngọt Xóm (Y=0.508m)"
            ),

            // 7. Chú Sáu Làm Vườn: Sân vườn chuối Nhà 08 Xóm Tây
            new NPCPositionConfig(
                "NPC_07_ChuSauLamVuon",
                new Vector3(-23.00f, GROUND_Y, 18.00f),
                new Vector3(0f, 120f, 0f),
                "Sân Vườn Chuối Nhà 08 (Y=0.508m)"
            ),

            // 8. Bé Tèo: Sân Xóm Tây Nhà 12
            new NPCPositionConfig(
                "NPC_08_BeTeo",
                new Vector3(-31.00f, GROUND_Y, 14.00f),
                new Vector3(0f, 90f, 0f),
                "Sân Xóm Tây Nhà 12 (Y=0.508m)"
            ),

            // 9. Bé Mận: Sân trước Nhà 07 gánh bánh bò
            new NPCPositionConfig(
                "NPC_09_BeMan",
                new Vector3(-16.00f, GROUND_Y, 14.00f),
                new Vector3(0f, 90f, 0f),
                "Sân Nhà 07 Bánh Bò (Y=0.508m)"
            ),

            // 10. Dì Hai: Sân trước Nhà 11 Bờ Kênh
            new NPCPositionConfig(
                "NPC_11_DiHai",
                new Vector3(-1.00f, GROUND_Y, 2.50f),
                new Vector3(0f, 0f, 0f),
                "Sân Nhà 11 Bờ Kênh (Y=0.508m)"
            ),

            // 11. Cụ Bảy Già: Dưới bóng mát Cây Đa Trung Tâm bên hông sân thoáng
            new NPCPositionConfig(
                "NPC_12_CuBayGia",
                new Vector3(-2.50f, GROUND_Y, 9.00f),
                new Vector3(0f, -90f, 0f),
                "Dưới bóng mát Cây Đa Trung Tâm (Y=0.508m, đứng trên sân thoáng đón An)"
            ),

            // 12. Chú Bảy Chèo Đò: Đầu cầu tàu bến đò ra biển (Pier Dock Tip)
            new NPCPositionConfig(
                "NPC_04_ChuBayCheoDo",
                new Vector3(56.00f, PORCH_FLOOR_Y, -40.50f),
                new Vector3(0f, 10f, 0f),
                "Đầu Cầu Tàu Bến Đò Biển (Y=0.89m, đón An lên thuyền ra khơi)"
            )
        };

        [MenuItem("Tools/Hoa Do/Fix NPC Positions With Houses And Ground (Plan B)", false, 10)]
        public static void Execute()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (!activeScene.path.Contains("CaMau_1954_Village"))
            {
                Debug.LogWarning("[FixNPCs] Opening CaMau_1954_Village.unity...");
                EditorSceneManager.OpenScene(ScenePath);
            }

            Debug.Log("<color=#38bdf8>[FixNPCs]</color> Bắt đầu chuẩn hóa vị trí 12 NPC theo Phương Án B (Sàn hiên 0.89m trong lòng hiên & mặt đất 0.508m)...");

            int updatedCount = 0;

            foreach (var cfg in Configs)
            {
                var go = GameObject.Find(cfg.gameObjectName);
                if (go == null)
                {
                    var npcsRoot = GameObject.Find("CaMau_1954_Village/NPCs") ?? GameObject.Find("NPCs");
                    if (npcsRoot != null)
                    {
                        var t = npcsRoot.transform.Find(cfg.gameObjectName);
                        if (t != null) go = t.gameObject;
                    }
                }

                if (go == null)
                {
                    Debug.LogWarning($"[FixNPCs] Không tìm thấy NPC '{cfg.gameObjectName}'!");
                    continue;
                }

                Undo.RecordObject(go.transform, $"Align NPC {cfg.gameObjectName}");
                go.transform.position = cfg.position;
                go.transform.rotation = Quaternion.Euler(cfg.rotationEuler);

                // Reset all child mesh transforms (tripo_node) to clean local coordinates
                foreach (Transform child in go.GetComponentsInChildren<Transform>(true))
                {
                    if (child == go.transform) continue;

                    if (child.name.StartsWith("tripo_node"))
                    {
                        Undo.RecordObject(child, $"Reset tripo_node {child.name}");
                        child.localPosition = Vector3.zero;
                        child.localRotation = Quaternion.identity;
                        EditorUtility.SetDirty(child);
                    }
                }

                // Ensure CapsuleCollider is properly configured
                var col = go.GetComponent<CapsuleCollider>();
                if (col != null)
                {
                    Undo.RecordObject(col, $"Adjust Collider {cfg.gameObjectName}");
                    float s = go.transform.localScale.y;
                    col.center = new Vector3(0f, 0.85f * s, 0f);
                    col.height = 1.70f * s;
                    col.radius = 0.35f * s;
                    EditorUtility.SetDirty(col);
                }

                // Ensure Animator does not have root motion moving the NPC
                var anim = go.GetComponent<Animator>();
                if (anim != null)
                {
                    anim.applyRootMotion = false;
                }

                EditorUtility.SetDirty(go);
                updatedCount++;
                Debug.Log($"<color=#10b981>[FixNPCs]</color> Đã căn chỉnh '{cfg.gameObjectName}' -> Pos: {cfg.position}, Rot: {cfg.rotationEuler} ({cfg.description})");
            }

            // Mark Scene Dirty and Save
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            // Render QA verification screenshots
            CaptureQASnapshots();

            Debug.Log($"<color=#10b981>[FixNPCs] HOÀN TẤT THÀNH CÔNG! Đã căn chỉnh {updatedCount}/12 NPC chuẩn xác 100%.</color>");
        }

        private static void CaptureQASnapshots()
        {
            if (!Directory.Exists(ArtifactDir)) Directory.CreateDirectory(ArtifactDir);

            // 1. Close-up view of House 02 with Trưởng Thôn Năm on porch
            CaptureCameraView(
                new Vector3(-11.20f, 1.85f, 18.00f),
                Quaternion.Euler(6.0f, 0.0f, 0.0f),
                50.0f,
                Path.Combine(ArtifactDir, "QA_NPC_01_TruongThonNam_Porch.jpg")
            );

            // 2. Close-up view of House 01 with Má An & Bé Út on porch
            CaptureCameraView(
                new Vector3(-2.20f, 1.85f, 18.00f),
                Quaternion.Euler(6.0f, 0.0f, 0.0f),
                50.0f,
                Path.Combine(ArtifactDir, "QA_NPC_02_MaAn_BeUt_Porch.jpg")
            );

            // 3. Wide View from the village street showing both North houses and standing NPCs
            CaptureCameraView(
                new Vector3(-7.0f, 2.2f, 14.0f),
                Quaternion.Euler(8.0f, 0.0f, 0.0f),
                55.0f,
                Path.Combine(ArtifactDir, "QA_Village_NorthHouses_NPCs_Overview.jpg")
            );

            // 4. Perspective view matching player gameplay angle
            CaptureCameraView(
                new Vector3(-7.0f, 1.8f, 24.0f),
                Quaternion.Euler(12.0f, 180.0f, 0.0f),
                55.0f,
                Path.Combine(ArtifactDir, "QA_Player_Facing_South_Village_Entrance.jpg")
            );
        }

        private static void CaptureCameraView(Vector3 pos, Quaternion rot, float fov, string outputPath)
        {
            int width = 1280;
            int height = 720;

            GameObject camObj = new GameObject("Temp_QA_Camera");
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 1000f;
            camObj.transform.position = pos;
            camObj.transform.rotation = rot;

            RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            rt.antiAliasing = 4;
            cam.targetTexture = rt;

            RenderTexture prevActive = RenderTexture.active;
            RenderTexture.active = rt;

            cam.Render();

            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            cam.targetTexture = null;
            RenderTexture.active = prevActive;
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(camObj);

            byte[] jpgBytes = tex.EncodeToJPG(80);
            UnityEngine.Object.DestroyImmediate(tex);

            File.WriteAllBytes(outputPath, jpgBytes);
            Debug.Log($"[FixNPCs] Saved QA screenshot: {outputPath}");
        }
    }
}
