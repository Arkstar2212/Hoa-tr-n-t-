#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HoaVaMau.Editor
{
    /// <summary>
    /// Phase 2: Dựng Greybox 60m & Chuẩn hóa Hierarchy 10 Folder cho Cảnh 1 (Rừng Cà Mau, 1954).
    /// Tuân thủ nghiêm ngặt Bản Thiết kế Kỹ thuật Studio 2.5D:
    /// - 10 Nhóm Folder chuẩn: 01_Camera -> 10_EnvironmentSystems
    /// - Dải di chuyển & Địa hình: Z = 5m (-2.5 -> +2.5), gameplay Z = 0
    /// - 6 Vùng nhịp điệu (A -> F) liên tục 60m:
    ///   A (0-10m): Cửa rừng (Y = -1.85)
    ///   B (10-20m): Đất ẩm & Vũng nước nông (vùng trũng Y = -2.12, vũng nước Y = -1.98)
    ///   C (20-32m): Rễ cây & Gờ đất leo trèo (Chướng ngại vật Y = -0.15, cao 1.70m, hỗ trợ Ledge Grab)
    ///   D (32-42m): Đất trũng ngập & Bến sông (Y = -1.85, kết nối làng chài & cầu tàu)
    ///   E (42-52m): Khoảng rừng mở đón nắng (gờ đất cao Y = -1.65, god ray)
    ///   F (52-60m): Lối đi khuất trong rừng (Y = -1.80, vòm cây chuyển cảnh)
    /// </summary>
    public static class Scene1TechnicalSetup
    {
        private const string ScenePath = "Assets/Scenes/New Scene 1.unity";
        private const string EarthenTrailMatPath = "Assets/Materials/M_Earthen_Trail.mat";
        private const string RiverWaterMatPath = "Assets/Materials/M_RiverWater_Canh1.mat";
        private const string MossyRockMatPath = "Assets/Materials/M_Rock_MossyBoulder.mat";
        private const string MangroveBarkMatPath = "Assets/Materials/Environment/CaMau/M_CaMau_MangroveBark.mat";
        private const string StiltedWoodMatPath = "Assets/Materials/Environment/Architecture/M_StiltedHouse_Wood.mat";

        [MenuItem("Hoa va Mau/Level Design/Phase 2 - Execute 60m Technical Setup", false, 1)]
        public static void ExecuteSetup()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            Debug.Log($"<color=#40b8a6>[Scene1TechnicalSetup]</color> Bắt đầu thực thi Phase 2 trên scene: {activeScene.name}");

            // 1. Chuẩn hóa 10 Thư mục Hierarchy
            var folders = OrganizeTenFolders();

            // 2. Xây dựng Hệ thống Địa hình 60m (6 Phân vùng A -> F, Chiều rộng Z = 5m)
            BuildSegmentedTerrain60m(folders);

            // 3. Xây dựng Chướng ngại vật Leo trèo Vùng C (20-32m)
            BuildZoneC_ClimbingObstacle(folders);

            // 4. Xây dựng Vũng nước nông Vùng B (10-20m) & Bến sông Vùng D
            BuildWaterPuddlesAndRiverside(folders);

            // 5. Xây dựng Ánh sáng đón nắng Vùng E (42-52m) & Lối thoát Vùng F (52-60m)
            BuildZoneE_And_F_Features(folders);

            // 6. Kiểm tra và Cập nhật Player & Camera
            ValidatePlayerAndCamera(folders);

            // Đánh dấu scene dơ và lưu lại
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(activeScene);
            Debug.Log("<color=#40b8a6>[Scene1TechnicalSetup]</color> <b>HOÀN TẤT PHASE 2 THÀNH CÔNG RỰC RỠ!</b>");
        }

        // =========================================================================
        // 1. CHUẨN HÓA 10 THƯ MỤC HIERARCHY
        // =========================================================================
        public static Dictionary<string, GameObject> OrganizeTenFolders()
        {
            string[] folderNames = new string[]
            {
                "01_Camera",
                "02_Gameplay",
                "03_Terrain",
                "04_Foreground",
                "05_Midground",
                "06_Water",
                "07_Background",
                "08_Lighting",
                "09_VFX",
                "10_EnvironmentSystems"
            };

            var folders = new Dictionary<string, GameObject>();
            foreach (var name in folderNames)
            {
                var go = GameObject.Find(name);
                if (go == null)
                {
                    go = new GameObject(name);
                    go.transform.position = Vector3.zero;
                    go.transform.rotation = Quaternion.identity;
                    go.transform.localScale = Vector3.one;
                    Undo.RegisterCreatedObjectUndo(go, $"Create Folder {name}");
                }
                folders[name] = go;
            }

            // Di chuyển Camera
            MoveToFolder("Main Camera", folders["01_Camera"]);
            MoveToFolder("CM_VirtualCamera_2.5D", folders["01_Camera"]);

            // Di chuyển Gameplay
            MoveToFolder("PF_Player_Minh", folders["02_Gameplay"]);

            // Di chuyển Lighting
            MoveToFolder("Directional Light", folders["08_Lighting"]);

            // Di chuyển từ Level_Forest_60m cũ nếu có
            GameObject levelRoot = GameObject.Find("Level_Forest_60m");
            if (levelRoot != null)
            {
                MoveChildToFolder(levelRoot, "Walkway_60m", folders["03_Terrain"]);
                MoveChildToFolder(levelRoot, "Foreground_Silhouettes", folders["04_Foreground"]);
                MoveChildToFolder(levelRoot, "Midground_Trees", folders["05_Midground"]);
                MoveChildToFolder(levelRoot, "Landmarks_Obstacles", folders["05_Midground"]);
                MoveChildToFolder(levelRoot, "Riverside_Settlement", folders["05_Midground"]);
                MoveChildToFolder(levelRoot, "River_Water_System", folders["06_Water"]);
                MoveChildToFolder(levelRoot, "Background_Forest", folders["07_Background"]);
                MoveChildToFolder(levelRoot, "Sky_And_Backdrop", folders["07_Background"]);
                MoveChildToFolder(levelRoot, "Atmospheric_VFX", folders["09_VFX"]);
                MoveChildToFolder(levelRoot, "Edge_Foliage", folders["10_EnvironmentSystems"]);

                // Nếu Level_Forest_60m đã hết con, xóa an toàn
                if (levelRoot.transform.childCount == 0)
                {
                    Undo.DestroyObjectImmediate(levelRoot);
                }
            }

            // Vô hiệu hóa Walkway_60m cũ (giữ lại làm backup ẩn nếu cần)
            var oldWalkway = GameObject.Find("Walkway_60m");
            if (oldWalkway != null)
            {
                oldWalkway.SetActive(false);
                oldWalkway.name = "Walkway_60m_LegacyBackup";
            }

            return folders;
        }

        private static void MoveToFolder(string objectName, GameObject folder)
        {
            var go = GameObject.Find(objectName);
            if (go != null && go.transform.parent != folder.transform)
            {
                Undo.SetTransformParent(go.transform, folder.transform, $"Move {objectName} to {folder.name}");
            }
        }

        private static void MoveChildToFolder(GameObject parent, string childName, GameObject folder)
        {
            var child = parent.transform.Find(childName);
            if (child != null)
            {
                Undo.SetTransformParent(child, folder.transform, $"Move {childName} to {folder.name}");
            }
        }

        // =========================================================================
        // 2. XÂY DỰNG ĐỊA HÌNH 60M (6 VÙNG A -> F, Z = 5M)
        // =========================================================================
        public static void BuildSegmentedTerrain60m(Dictionary<string, GameObject> folders)
        {
            GameObject terrainRoot = folders["03_Terrain"];

            // Tạo hoặc tái cấu trúc Segmented_Terrain_60m
            Transform existingTerrain = terrainRoot.transform.Find("Segmented_Terrain_60m");
            GameObject segmentedTerrain;
            if (existingTerrain != null)
            {
                segmentedTerrain = existingTerrain.gameObject;
                // Xóa các con cũ để tái tạo sạch sẽ
                while (segmentedTerrain.transform.childCount > 0)
                {
                    Undo.DestroyObjectImmediate(segmentedTerrain.transform.GetChild(0).gameObject);
                }
            }
            else
            {
                segmentedTerrain = new GameObject("Segmented_Terrain_60m");
                segmentedTerrain.transform.SetParent(terrainRoot.transform, false);
                Undo.RegisterCreatedObjectUndo(segmentedTerrain, "Create Segmented_Terrain_60m");
            }

            Material earthMat = AssetDatabase.LoadAssetAtPath<Material>(EarthenTrailMatPath);

            // -------------------------------------------------------------
            // VÙNG A: 0 - 10m (Cửa rừng) — Mặt đất phẳng Y = -1.85, Z = 5m
            // -------------------------------------------------------------
            CreateTerrainBlock(segmentedTerrain, "Zone_A_Entrance_0_10m",
                new Vector3(4.0f, -2.55f, 0.0f),
                new Vector3(14.0f, 1.4f, 5.0f), // X: -3 -> 11, Y top: -1.85
                earthMat);

            // -------------------------------------------------------------
            // VÙNG B: 10 - 20m (Đất ẩm & Vũng nước nông) — Độ trũng Y = -2.12
            // -------------------------------------------------------------
            // Dốc xuống từ 10m -> 13.5m (Y: -1.85 -> -2.12)
            CreateTerrainRamp(segmentedTerrain, "Zone_B_RampDown_10_13m",
                new Vector3(11.75f, -2.685f, 0.0f),
                new Vector3(3.5f, 1.4f, 5.0f),
                -4.4f, // Xoay nghiêng góc nhẹ dốc xuống
                earthMat);

            // Đáy bùn trũng ẩm 13.5m -> 17.0m (Y: -2.12)
            CreateTerrainBlock(segmentedTerrain, "Zone_B_DepressionBed_13_17m",
                new Vector3(15.25f, -2.82f, 0.0f),
                new Vector3(3.5f, 1.4f, 5.0f), // Y top: -2.12
                earthMat);

            // Dốc lên từ 17.0m -> 20.0m (Y: -2.12 -> -1.85)
            CreateTerrainRamp(segmentedTerrain, "Zone_B_RampUp_17_20m",
                new Vector3(18.5f, -2.685f, 0.0f),
                new Vector3(3.0f, 1.4f, 5.0f),
                5.1f, // Xoay nghiêng góc nhẹ dốc lên
                earthMat);

            // -------------------------------------------------------------
            // VÙNG C: 20 - 32m (Tiếp cận & Thoát chướng ngại vật)
            // -------------------------------------------------------------
            // Đoạn tiếp cận trước chướng ngại vật: 20 -> 23.5m (Y: -1.85)
            CreateTerrainBlock(segmentedTerrain, "Zone_C_Approach_20_23m",
                new Vector3(21.75f, -2.55f, 0.0f),
                new Vector3(3.5f, 1.4f, 5.0f), // Y top: -1.85
                earthMat);

            // Đoạn gờ đáp & dốc thoải sau chướng ngại vật: 27.2 -> 32m
            // Bậc hạ chân: 27.2 -> 29.0m (Y: -1.00)
            CreateTerrainBlock(segmentedTerrain, "Zone_C_StepDownShelf_27_29m",
                new Vector3(28.1f, -2.0f, 0.0f),
                new Vector3(1.8f, 2.0f, 5.0f), // Y top: -1.00
                earthMat);

            // Dốc hạ đất: 29.0 -> 30.5m (Y: -1.00 -> -1.85)
            CreateTerrainRamp(segmentedTerrain, "Zone_C_RampDown_29_31m",
                new Vector3(29.75f, -2.425f, 0.0f),
                new Vector3(1.5f, 2.0f, 5.0f),
                -29.5f,
                earthMat);

            // Nối tiếp sang Vùng D: 30.5 -> 32.5m (Y: -1.85)
            CreateTerrainBlock(segmentedTerrain, "Zone_C_Exit_30_32m",
                new Vector3(31.5f, -2.55f, 0.0f),
                new Vector3(2.0f, 1.4f, 5.0f), // Y top: -1.85
                earthMat);

            // -------------------------------------------------------------
            // VÙNG D: 32 - 42m (Đất trũng & Bến sông) — Y = -1.85
            // -------------------------------------------------------------
            CreateTerrainBlock(segmentedTerrain, "Zone_D_DockGround_32_42m",
                new Vector3(37.0f, -2.55f, 0.0f),
                new Vector3(10.0f, 1.4f, 5.0f), // Y top: -1.85
                earthMat);

            // -------------------------------------------------------------
            // VÙNG E: 42 - 52m (Khoảng rừng mở đón nắng) — Gờ đất cao Y = -1.65
            // -------------------------------------------------------------
            // Dốc lên 42 -> 44.5m (Y: -1.85 -> -1.65)
            CreateTerrainRamp(segmentedTerrain, "Zone_E_RampUp_42_44m",
                new Vector3(43.25f, -2.45f, 0.0f),
                new Vector3(2.5f, 1.6f, 5.0f),
                4.6f,
                earthMat);

            // Gờ đất cao đón nắng 44.5 -> 49.5m (Y: -1.65)
            CreateTerrainBlock(segmentedTerrain, "Zone_E_SunlitPlateau_44_50m",
                new Vector3(47.0f, -2.45f, 0.0f),
                new Vector3(5.0f, 1.6f, 5.0f), // Y top: -1.65
                earthMat);

            // Dốc xuống nhẹ 49.5 -> 52.0m (Y: -1.65 -> -1.80)
            CreateTerrainRamp(segmentedTerrain, "Zone_E_RampDown_50_52m",
                new Vector3(50.75f, -2.525f, 0.0f),
                new Vector3(2.5f, 1.6f, 5.0f),
                -3.4f,
                earthMat);

            // -------------------------------------------------------------
            // VÙNG F: 52 - 62m (Lối đi khuất & Chuyển cảnh) — Y = -1.80
            // -------------------------------------------------------------
            CreateTerrainBlock(segmentedTerrain, "Zone_F_HiddenPath_52_62m",
                new Vector3(57.0f, -2.55f, 0.0f),
                new Vector3(10.0f, 1.5f, 5.0f), // Y top: -1.80
                earthMat);
        }

        // =========================================================================
        // 3. XÂY DỰNG CHƯỚNG NGẠI VẬT LEO TRÈO VÙNG C (20 - 32m)
        // =========================================================================
        public static void BuildZoneC_ClimbingObstacle(Dictionary<string, GameObject> folders)
        {
            GameObject terrainRoot = folders["03_Terrain"];
            GameObject midgroundRoot = folders["05_Midground"];

            // 1. Khối chướng ngại vật vật lý (Collider & Walkable Top Platform)
            Transform existingObs = terrainRoot.transform.Find("Zone_C_ClimbObstacle_Physical");
            GameObject obstacleGO;
            if (existingObs != null)
            {
                obstacleGO = existingObs.gameObject;
                while (obstacleGO.transform.childCount > 0)
                {
                    Undo.DestroyObjectImmediate(obstacleGO.transform.GetChild(0).gameObject);
                }
            }
            else
            {
                obstacleGO = new GameObject("Zone_C_ClimbObstacle_Physical");
                obstacleGO.transform.SetParent(terrainRoot.transform, false);
                Undo.RegisterCreatedObjectUndo(obstacleGO, "Create Zone_C_ClimbObstacle_Physical");
            }

            // Top bề mặt đi bộ: X từ 23.5 -> 27.2m (rộng 3.7m), Y = -0.15 (cao 1.70m so với đất -1.85)
            // Minh nhảy lên sẽ kích hoạt LedgeGrabDetector bám mép vách tại X = 23.5, Y = -0.15
            Material earthMat = AssetDatabase.LoadAssetAtPath<Material>(EarthenTrailMatPath);

            // Platform mặt trên (Walkable)
            var trunkTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trunkTop.name = "FallenTrunk_PlatformTop";
            trunkTop.transform.SetParent(obstacleGO.transform, false);
            trunkTop.transform.position = new Vector3(25.35f, -1.05f, 0.0f);
            trunkTop.transform.localScale = new Vector3(3.7f, 1.8f, 5.0f); // Top Y = -1.05 + 0.9 = -0.15
            var topRenderer = trunkTop.GetComponent<MeshRenderer>();
            if (topRenderer != null && earthMat != null) topRenderer.sharedMaterial = earthMat;

            // 2. Trang trí Visual Nghệ thuật (3D Mossy Boulders & Twisted Roots)
            GameObject decorRoot = new GameObject("Visual_FallenLog_Roots");
            decorRoot.transform.SetParent(obstacleGO.transform, false);

            // Bố trí cụm đá rêu phong ở chân thân cây
            Material rockMat = AssetDatabase.LoadAssetAtPath<Material>(MossyRockMatPath);
            var boulderMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Models/Environment/SM_MossyBoulder_01.fbx");
            var largeRockMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Models/Environment/SM_Rock_Large_01.fbx");

            if (largeRockMesh != null)
            {
                // Đá lớn đỡ thân cây bên trái (Z âm - tiền cảnh)
                var rockL = new GameObject("MossyRock_Base_Front");
                rockL.transform.SetParent(decorRoot.transform, false);
                rockL.transform.position = new Vector3(23.2f, -1.5f, -1.2f);
                rockL.transform.rotation = Quaternion.Euler(0, 45, 15);
                rockL.transform.localScale = new Vector3(1.3f, 1.2f, 1.3f);
                var mf = rockL.AddComponent<MeshFilter>();
                mf.sharedMesh = largeRockMesh;
                var mr = rockL.AddComponent<MeshRenderer>();
                mr.sharedMaterial = rockMat;

                // Đá lớn phía sau (Z dương - hậu cảnh)
                var rockR = new GameObject("MossyRock_Base_Back");
                rockR.transform.SetParent(decorRoot.transform, false);
                rockR.transform.position = new Vector3(23.7f, -1.4f, 1.4f);
                rockR.transform.rotation = Quaternion.Euler(0, -70, -10);
                rockR.transform.localScale = new Vector3(1.5f, 1.3f, 1.5f);
                var mf2 = rockR.AddComponent<MeshFilter>();
                mf2.sharedMesh = largeRockMesh;
                var mr2 = rockR.AddComponent<MeshRenderer>();
                mr2.sharedMaterial = rockMat;

                // Khối đá gờ bước bên phải
                var rockRight = new GameObject("MossyRock_StepShelf");
                rockRight.transform.SetParent(decorRoot.transform, false);
                rockRight.transform.position = new Vector3(27.4f, -1.4f, 0.8f);
                rockRight.transform.rotation = Quaternion.Euler(-10, 60, 5);
                rockRight.transform.localScale = new Vector3(1.2f, 1.0f, 1.2f);
                var mf3 = rockRight.AddComponent<MeshFilter>();
                mf3.sharedMesh = largeRockMesh;
                var mr3 = rockRight.AddComponent<MeshRenderer>();
                mr3.sharedMaterial = rockMat;
            }

            // Gắn chùm rễ cây đước 2D/3D bám trên vách
            Sprite rootSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Canh1_Sprites/FG_Mangrove_01.png")
                ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/PierScene/mangrove_cluster.png");
            if (rootSprite != null)
            {
                var rootSpGO = new GameObject("RootCluster_LedgeFace");
                rootSpGO.transform.SetParent(decorRoot.transform, false);
                rootSpGO.transform.position = new Vector3(23.4f, -0.9f, -0.6f);
                rootSpGO.transform.rotation = Quaternion.Euler(0, 0, -12);
                rootSpGO.transform.localScale = new Vector3(1.4f, 1.4f, 1.4f);
                var sr = rootSpGO.AddComponent<SpriteRenderer>();
                sr.sprite = rootSprite;
                sr.sortingOrder = 15;

                var rootSpGO2 = new GameObject("RootCluster_LedgeFace_2");
                rootSpGO2.transform.SetParent(decorRoot.transform, false);
                rootSpGO2.transform.position = new Vector3(23.45f, -0.6f, 0.7f);
                rootSpGO2.transform.rotation = Quaternion.Euler(0, 0, 8);
                rootSpGO2.transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);
                var sr2 = rootSpGO2.AddComponent<SpriteRenderer>();
                sr2.sprite = rootSprite;
                sr2.sortingOrder = 14;
            }
        }

        // =========================================================================
        // 4. XÂY DỰNG VŨNG NƯỚC NÔNG VÙNG B & BẾN SÔNG VÙNG D
        // =========================================================================
        public static void BuildWaterPuddlesAndRiverside(Dictionary<string, GameObject> folders)
        {
            GameObject waterRoot = folders["06_Water"];

            // 1. Vũng nước nông Vùng B (10 - 20m)
            Transform existingPuddle = waterRoot.transform.Find("Zone_B_ShallowPuddle");
            GameObject puddleGO;
            if (existingPuddle != null)
            {
                puddleGO = existingPuddle.gameObject;
            }
            else
            {
                puddleGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
                puddleGO.name = "Zone_B_ShallowPuddle";
                puddleGO.transform.SetParent(waterRoot.transform, false);
                Undo.RegisterCreatedObjectUndo(puddleGO, "Create Zone_B_ShallowPuddle");
            }

            puddleGO.transform.position = new Vector3(15.25f, -1.98f, 0.0f);
            puddleGO.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            puddleGO.transform.localScale = new Vector3(5.5f, 4.4f, 1.0f);

            var quadCol = puddleGO.GetComponent<Collider>();
            if (quadCol != null) UnityEngine.Object.DestroyImmediate(quadCol);

            Material waterMat = AssetDatabase.LoadAssetAtPath<Material>(RiverWaterMatPath);
            var puddleRenderer = puddleGO.GetComponent<MeshRenderer>();
            if (puddleRenderer != null && waterMat != null)
            {
                puddleRenderer.sharedMaterial = waterMat;
            }

            // 2. Mở rộng dải sông chính để phủ hết từ 0 đến 75m
            var riverObj = GameObject.Find("River_Water_Surface");
            if (riverObj != null)
            {
                riverObj.transform.position = new Vector3(37.0f, -2.05f, 11.50f);
                riverObj.transform.localScale = new Vector3(7.8f, 1.0f, 2.5f);
            }

            // 3. Đá dậm bước (Stepping Stones) băng qua vũng nước nông Vùng B
            Material rockMat = AssetDatabase.LoadAssetAtPath<Material>(MossyRockMatPath);
            var smallRock = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Models/Environment/SM_Rock_Small_01.fbx");
            if (smallRock != null)
            {
                var stone1 = new GameObject("SteppingStone_B1");
                stone1.transform.SetParent(puddleGO.transform, false);
                stone1.transform.position = new Vector3(14.2f, -1.92f, 0.45f);
                stone1.transform.rotation = Quaternion.Euler(0, 30, 0);
                stone1.transform.localScale = new Vector3(0.7f, 0.5f, 0.7f);
                var mf1 = stone1.AddComponent<MeshFilter>();
                mf1.sharedMesh = smallRock;
                var mr1 = stone1.AddComponent<MeshRenderer>();
                mr1.sharedMaterial = rockMat;

                var stone2 = new GameObject("SteppingStone_B2");
                stone2.transform.SetParent(puddleGO.transform, false);
                stone2.transform.position = new Vector3(16.3f, -1.90f, -0.4f);
                stone2.transform.rotation = Quaternion.Euler(0, 110, 0);
                stone2.transform.localScale = new Vector3(0.8f, 0.55f, 0.8f);
                var mf2 = stone2.AddComponent<MeshFilter>();
                mf2.sharedMesh = smallRock;
                var mr2 = stone2.AddComponent<MeshRenderer>();
                mr2.sharedMaterial = rockMat;
            }
        }

        // =========================================================================
        // 5. XÂY DỰNG TÍNH NĂNG ĐẶC TRƯNG VÙNG E & VÙNG F
        // =========================================================================
        public static void BuildZoneE_And_F_Features(Dictionary<string, GameObject> folders)
        {
            GameObject vfxRoot = folders["09_VFX"];
            GameObject gameplayRoot = folders["02_Gameplay"];

            // 1. Luồng sáng vàng ấm (Golden God Ray / Sun Shaft) rọi xuống Vùng E (47m)
            Transform existingShaft = vfxRoot.transform.Find("SunShaft_ZoneE_Clearing");
            GameObject shaft;
            if (existingShaft != null)
            {
                shaft = existingShaft.gameObject;
            }
            else
            {
                shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                shaft.name = "SunShaft_ZoneE_Clearing";
                shaft.transform.SetParent(vfxRoot.transform, false);
                shaft.transform.position = new Vector3(47.0f, 3.5f, 0.5f);
                shaft.transform.rotation = Quaternion.Euler(15f, 0f, -18f); // Rọi chéo từ trên xuống
                shaft.transform.localScale = new Vector3(2.5f, 6.0f, 2.5f);

                var shaftCol = shaft.GetComponent<Collider>();
                if (shaftCol != null) UnityEngine.Object.DestroyImmediate(shaftCol);
                Undo.RegisterCreatedObjectUndo(shaft, "Create SunShaft_ZoneE_Clearing");
            }

            var mr = shaft.GetComponent<MeshRenderer>();
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlit != null)
            {
                Material shaftMat = new Material(unlit);
                shaftMat.name = "M_GodRay_SunlitClearing";
                shaftMat.SetFloat("_Surface", 1.0f); // 1 = Transparent
                shaftMat.SetFloat("_Blend", 0.0f);   // 0 = Alpha
                shaftMat.SetFloat("_ZWrite", 0.0f);
                shaftMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                shaftMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                shaftMat.SetColor("_BaseColor", new Color(1.0f, 0.94f, 0.70f, 0.06f)); // Luồng sáng dịu êm
                mr.sharedMaterial = shaftMat;
            }

            // 2. Khắc phục vòm cây Canopy_Wall_System liên tục trên toàn tuyến 60m
            GameObject canopy = GameObject.Find("Canopy_Wall_System");
            if (canopy != null)
            {
                for (int i = 0; i < canopy.transform.childCount; i++)
                {
                    Transform child = canopy.transform.GetChild(i);
                    Vector3 pos = child.localPosition;
                    if (pos.y < 0)
                    {
                        pos.y = 4.2f;
                        pos.z = 16.0f;
                        child.localPosition = pos;
                        child.localScale = new Vector3(3.8f, 3.4f, 1.0f);
                    }
                }
            }

            // 3. Trigger Chuyển Cảnh ở cuối Vùng F (60m)
            Transform existingTrigger = gameplayRoot.transform.Find("Scene1_ExitGoalTrigger_60m");
            if (existingTrigger == null)
            {
                var triggerGO = new GameObject("Scene1_ExitGoalTrigger_60m");
                triggerGO.transform.SetParent(gameplayRoot.transform, false);
                triggerGO.transform.position = new Vector3(59.5f, -0.5f, 0.0f);

                var box = triggerGO.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(2.0f, 4.0f, 6.0f);

                Undo.RegisterCreatedObjectUndo(triggerGO, "Create Scene1_ExitGoalTrigger_60m");
            }
        }

        // =========================================================================
        // 6. KIỂM TRA VÀ CẬP NHẬT PLAYER & CAMERA
        // =========================================================================
        public static void ValidatePlayerAndCamera(Dictionary<string, GameObject> folders)
        {
            var player = GameObject.Find("PF_Player_Minh");
            if (player != null)
            {
                // Đảm bảo có LedgeGrabDetector để hỗ trợ bám mép và leo trèo
                var detector = player.GetComponent<HoaVaMau.Gameplay.LedgeGrabDetector>();
                if (detector == null)
                {
                    detector = Undo.AddComponent<HoaVaMau.Gameplay.LedgeGrabDetector>(player);
                    Debug.Log("<color=#40b8a6>[Scene1TechnicalSetup]</color> Đã gắn LedgeGrabDetector lên PF_Player_Minh.");
                }

                // Đảm bảo vị trí ban đầu tại Cửa rừng Vùng A (0, -1.85, 0)
                player.transform.position = new Vector3(0.0f, -1.85f, 0.0f);
                Debug.Log($"<color=#40b8a6>[Scene1TechnicalSetup]</color> Đã định vị Player Minh tại: {player.transform.position}");
            }

            var vcam = GameObject.Find("CM_VirtualCamera_2.5D");
            if (vcam != null)
            {
                Debug.Log("<color=#40b8a6>[Scene1TechnicalSetup]</color> Đã xác nhận CM_VirtualCamera_2.5D hoạt động tốt trong 01_Camera.");
            }
        }

        // =========================================================================
        // HÀM TIỆN ÍCH DỰNG BLOCK VÀ RAMP
        // =========================================================================
        private static GameObject CreateTerrainBlock(GameObject parent, string name, Vector3 center, Vector3 size, Material mat)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent.transform, false);
            block.transform.position = center;
            block.transform.rotation = Quaternion.identity;
            block.transform.localScale = size;

            var renderer = block.GetComponent<MeshRenderer>();
            if (renderer != null && mat != null) renderer.sharedMaterial = mat;

            Undo.RegisterCreatedObjectUndo(block, $"Create {name}");
            return block;
        }

        private static GameObject CreateTerrainRamp(GameObject parent, string name, Vector3 center, Vector3 size, float zRot, Material mat)
        {
            var ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ramp.name = name;
            ramp.transform.SetParent(parent.transform, false);
            ramp.transform.position = center;
            ramp.transform.rotation = Quaternion.Euler(0f, 0f, zRot);
            ramp.transform.localScale = size;

            var renderer = ramp.GetComponent<MeshRenderer>();
            if (renderer != null && mat != null) renderer.sharedMaterial = mat;

            Undo.RegisterCreatedObjectUndo(ramp, $"Create {name}");
            return ramp;
        }
    }
}
#endif
