#if UNITY_EDITOR
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using HoaVaMau.Gameplay;
using HoaVaMau.Environment;

namespace HoaVaMau.EditorTools
{
    /// <summary>
    /// Script dàn dựng ADDITIVE Cánh đồng lúa 2D/2.5D trải dài 100 mét với 4 TRỤ CỘT THIẾT KẾ CÓ CHIỀU SÂU (Planet of Lana standard):
    /// 
    /// TRỤ CỘT 1: ĐẾ HỘP DIORAMA LIÊN TỤC & KHÓA CHÂN TRỜI (Continuous Sloped Diorama Floor):
    ///    - Mặt nền địa hình 3D kéo dài liên tục từ Z = 0.45m đến Z = 12.0m, độ dốc bù thị sai Y(Z).
    ///    - Triệt tiêu 100% "Lỗi hở chân trời" (The Horizon Gap) dưới gốc cây và nhà ở xa khi camera nhìn phối cảnh.
    /// 
    /// TRỤ CỘT 2: LUẬT QUẦN TỤ SINH HỌC 3 TẦNG & KHÓA TRẦN KÍCH THƯỚC (3-Tier Clustering & Scale Normalizer):
    ///    - Chuẩn hóa kích thước tuyệt đối theo mét (Nhà tranh 3.8m, Cây chuối 2.6m, Dừa 7.2m, Đa 9.8m, Tre 6.2m, Minh 1.5m).
    ///    - Lũy tre làng và cây đại thụ (Z = 6.5m - 8.5m) ôm bọc sau lưng nhà tranh tạo chiều sâu xôm tụ, chân thực.
    /// 
    /// TRỤ CỘT 3: SẮC ĐỘ & KHÍ QUYỂN THEO TRỤC Z (Depth Color Grading):
    ///    - Tiền cảnh (Z <= -1.0m) tối sẫm 38% tạo silhouette framing điện ảnh.
    ///    - Lối đi chính (Z = 0m) ấm áp rực rỡ nắng vàng.
    ///    - Hậu cảnh viễn (Z = 2m .. 12m) pha dần sắc lam sương khói sông nước Nam Bộ.
    /// 
    /// TRỤ CỘT 4: BỐ CỤC 7 VÙNG NHỊP ĐIỆU & GAMEPLAY CROUCH POCKET:
    ///    - Xen kẽ 2 Khoảng Thở (Open Scenic Dikes) tạo cảm xúc bao la, không nhét asset máy móc.
    ///    - Vùng lúa cao 1.25m tràn bờ đê (X = 180m - 192m) kẹp 2 bên đường phục vụ cơ chế Crouch / Nấp.
    /// 
    /// AN TOÀN & KHÔI PHỤC:
    ///    - Có menu 1-click "Restore Backup Rice Field 100m" để quay về trạng thái cũ bất cứ khi nào!
    /// </summary>
    public static class BuildRiceField100mAdditive
    {
        public const string RootName = "[Additive_RiceField_100m]";
        public const string BackupPrefabPath = "Assets/Prefabs/Backup_Additive_RiceField_100m.prefab";

        private const string RiceTexDir = "Assets/Textures/Lana_Unified_Art/Rice";
        private const string GenMeshDir = "Assets/Materials/LanaOverhaul/GeneratedMeshes";
        private const string ClutterDir = "Assets/Prefabs/Clutter";

        [MenuItem("Hoa va Mau/Level Design/Build 100m Rice Field Additive (After Village Road - X=138m)", false, 0)]
        public static void BuildAfterVillage()
        {
            BuildRiceField(138.0f, 100.0f, -1.85f);
        }

        [MenuItem("Hoa va Mau/Level Design/Restore Backup Rice Field 100m", false, 1)]
        public static void RestoreBackup()
        {
            Debug.Log("<color=#3b82f6>[RiceField100m]</color> Đang khôi phục Cánh đồng lúa từ Backup...");
            GameObject current = GameObject.Find(RootName);
            if (current != null)
            {
                Undo.DestroyObjectImmediate(current);
            }

            GameObject backupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BackupPrefabPath);
            if (backupPrefab == null)
            {
                Debug.LogError($"<color=#ef4444>[RiceField100m]</color> Không tìm thấy file backup tại: {BackupPrefabPath}");
                return;
            }

            GameObject restored = (GameObject)PrefabUtility.InstantiatePrefab(backupPrefab);
            restored.name = RootName;
            Undo.RegisterCreatedObjectUndo(restored, "Restore Backup Rice Field 100m");
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("<color=#10b981>[RiceField100m]</color> ĐÃ KHÔI PHỤC THÀNH CÔNG từ Backup Prefab!");
        }

        public static void BuildRiceField(float startX, float lengthMeters, float pathY = -1.85f)
        {
            float endX = startX + lengthMeters;
            Debug.Log($"<color=#f59e0b>[RiceField100m]</color> Dựng Cánh đồng lúa 100m chuẩn 4 TRỤ CỘT ĐỘ SÂU 2.5D (X={startX:F1}m -> {endX:F1}m)...");

            GameObject root = GameObject.Find(RootName);
            if (root != null)
            {
                Undo.DestroyObjectImmediate(root);
            }
            root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Create 100m Terraced Rice Field 4-Pillars Overhaul");

            // Materials
            Material spriteMat = GetCleanSpriteMaterial();
            Material grassCapMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/LanaOverhaul/M_Lana_GrassCap.mat");
            Material rockUnderMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/LanaOverhaul/M_Lana_RockUnderbelly.mat");
            Material waterMat = GetOrCreateSkyWaterMaterial();

            // 7 Official 2D Rice Sprites
            Sprite sprClump     = AssetDatabase.LoadAssetAtPath<Sprite>($"{RiceTexDir}/T_Lana_Rice_01_GoldenClump.png");
            Sprite sprWideRow   = AssetDatabase.LoadAssetAtPath<Sprite>($"{RiceTexDir}/T_Lana_Rice_02_WideGoldenRow.png");
            Sprite sprTallSway  = AssetDatabase.LoadAssetAtPath<Sprite>($"{RiceTexDir}/T_Lana_Rice_Carpet_01_TallSway.png");
            Sprite sprMedium    = AssetDatabase.LoadAssetAtPath<Sprite>($"{RiceTexDir}/T_Lana_Rice_Carpet_02_Medium.png");
            Sprite sprDenseHead = AssetDatabase.LoadAssetAtPath<Sprite>($"{RiceTexDir}/T_Lana_Rice_Carpet_03_DenseHead.png");
            Sprite sprPanicle   = AssetDatabase.LoadAssetAtPath<Sprite>($"{RiceTexDir}/T_Lana_Rice_Panicle_04_Tall.png");
            Sprite sprCluster   = AssetDatabase.LoadAssetAtPath<Sprite>($"{RiceTexDir}/T_Lana_Rice_Panicle_05_Cluster.png");

            // Scenery Prefabs
            GameObject pfWaitingShelter = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment_Dock/PF_Dock_Waiting_Shelter.prefab");
            GameObject pfThatchedHut    = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment_Dock/PF_Village_ThatchedHut.prefab");
            GameObject pfBanana         = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_05_Banana.prefab");
            GameObject pfCoconut        = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_06_Coconut.prefab");
            GameObject pfBigCanopy      = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_01_LargeCanopy.prefab");
            GameObject pfMedCanopy      = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_02_Medium.prefab");
            GameObject pfBamboo         = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_07_BambooGrove.prefab");
            GameObject pfReeds          = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_Concept_Assets/Wildflowers/PF_T_HD_Wildflower_02_Reeds.prefab");
            GameObject pfGrassShort     = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_Concept_Assets/Grass/PF_T_HD_GrassCarpet_01_Short.prefab");
            GameObject pfRockSlab       = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_Concept_Assets/Rocks/PF_T_HD_Rock_04_FlatSlab.prefab");
            GameObject pfTrellis        = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/GourdTrellis/PF_LanaVN_GourdTrellis_25D_Composite.prefab");

            // 4 Modular Clutter Prefabs
            EnsureModularClutterPrefabs(spriteMat);
            GameObject pfClutterCottage   = AssetDatabase.LoadAssetAtPath<GameObject>($"{ClutterDir}/PF_Clutter_ChanCot_NhaTranh.prefab");
            GameObject pfClutterCoconut   = AssetDatabase.LoadAssetAtPath<GameObject>($"{ClutterDir}/PF_Clutter_BoDe_GocDua.prefab");
            GameObject pfClutterRuralPath = AssetDatabase.LoadAssetAtPath<GameObject>($"{ClutterDir}/PF_Clutter_DauVet_NongThon.prefab");
            GameObject pfClutterOverhead  = AssetDatabase.LoadAssetAtPath<GameObject>($"{ClutterDir}/PF_Clutter_TienCanh_Overhead.prefab");

            // =========================================================================
            // TRỤ CỘT 1: ĐẾ HỘP DIORAMA LIÊN TỤC (CONTINUOUS SLOPED DIORAMA FLOOR)
            // Kéo dài từ Z = 0.45m đến Z = 12.0m, lấp kín hoàn toàn mặt đất dưới chân nhà và cây
            // =========================================================================
            var dioramaRoot = new GameObject("00_Continuous_Diorama_PaddyFloor");
            dioramaRoot.transform.SetParent(root.transform, false);

            CreateContinuousDioramaFloor(
                dioramaRoot.transform, "RiceField100m_DioramaPaddyBase",
                startX - 6.0f, endX + 35.0f, pathY, grassCapMat);

            // =========================================================================
            // CON ĐƯỜNG BỜ ĐÊ CHÍNH (LEDGE_06 EXTENSION KHỚP CẢNH TRƯỚC)
            // =========================================================================
            var pathRoot = new GameObject("01_Gameplay_Pathway_LanaLedge_Extension");
            pathRoot.transform.SetParent(root.transform, false);

            CreateLanaLedgeSection(
                pathRoot.transform, "Ledge_06_VastRicePaddyField_100m",
                startX, endX, 320, 0.05f, 1.10f, -9.5f,
                x => pathY, grassCapMat, rockUnderMat);

            var colGo = new GameObject("Col_10_VastRicePaddyPlatform_100m");
            colGo.transform.SetParent(pathRoot.transform, false);
            colGo.transform.position = new Vector3(startX + lengthMeters * 0.5f, -4.93f, 0f);
            var col3D = colGo.AddComponent<BoxCollider>();
            col3D.size = new Vector3(lengthMeters + 2f, 6.15f, 4.0f);

            var endWall = new GameObject("Pathway_3DEndWall_238m");
            endWall.transform.SetParent(pathRoot.transform, false);
            endWall.transform.position = new Vector3(endX + 0.5f, pathY + 3.0f, 0f);
            var endCol3D = endWall.AddComponent<BoxCollider>();
            endCol3D.size = new Vector3(1.0f, 7.0f, 4.0f);

            // Điểm xuyết mặt đường với khoảng cách tự nhiên
            for (float px = startX + 3f; px <= endX - 3f; px += 4.5f)
            {
                float jx = px + Mathf.Sin(px * 2.1f) * 0.4f;
                if (pfGrassShort != null)
                {
                    Color grassCol = GetAtmosphericTint(0.35f, new Color(0.92f, 0.88f, 0.65f, 1f));
                    SpawnPrefab(pfGrassShort, $"Path_GrassTuft_{jx:F0}", pathRoot.transform,
                        new Vector3(jx, pathY + 0.01f, 0.35f), 0.35f, (px % 5f > 2.5f),
                        grassCol, 13, spriteMat, 1.0f, 1.0f);
                }
                if (px % 9.0f < 4.0f && pfRockSlab != null)
                {
                    Color rockCol = GetAtmosphericTint(0.15f, new Color(0.65f, 0.62f, 0.55f, 1f));
                    SpawnPrefab(pfRockSlab, $"Path_SteppingStone_{jx:F0}", pathRoot.transform,
                        new Vector3(jx, pathY - 0.02f, 0.15f), 0.35f, false,
                        rockCol, 12, spriteMat, 0f, 0f);
                }
            }

            // Dấu vết lao động nông thôn tại các điểm dừng chân cố định
            if (pfClutterRuralPath != null)
            {
                SpawnPrefab(pfClutterRuralPath, "Clutter_Story_Waypoint_146m", pathRoot.transform,
                    new Vector3(startX + 8.0f, pathY, 0.60f), 0.85f, false, Color.white, 14, spriteMat, 0f, 0f);

                SpawnPrefab(pfClutterRuralPath, "Clutter_Story_Waypoint_188m", pathRoot.transform,
                    new Vector3(startX + 50.0f, pathY, 0.65f), 0.85f, true, Color.white, 14, spriteMat, 0f, 0f);

                SpawnPrefab(pfClutterRuralPath, "Clutter_Story_Waypoint_218m", pathRoot.transform,
                    new Vector3(startX + 80.0f, pathY, 0.60f), 0.85f, false, Color.white, 14, spriteMat, 0f, 0f);
            }

            // =========================================================================
            // TRỤ CỘT 3: TIỀN CẢNH RỦ BÓNG (FOREGROUND FRAMING - Z = -1.65m .. -1.8m)
            // Màu sắc tối sẫm 38% tạo cảm giác góc nhìn lén lút, chiều sâu tách biệt
            // =========================================================================
            var fgRoot = new GameObject("02_Foreground_Cinematic_Framing");
            fgRoot.transform.SetParent(root.transform, false);

            if (pfClutterOverhead != null)
            {
                Color fgCanopyColor = GetAtmosphericTint(-1.65f, new Color(0.85f, 0.88f, 0.80f, 0.95f));
                SpawnPrefab(pfClutterOverhead, "FG_Canopy_Framing_148m", fgRoot.transform,
                    new Vector3(startX + 10.0f, 8.1f, -1.65f), 1.35f, false,
                    fgCanopyColor, 32, spriteMat, 0.8f, 0.7f);

                SpawnPrefab(pfClutterOverhead, "FG_Canopy_Framing_176m", fgRoot.transform,
                    new Vector3(startX + 38.0f, 8.1f, -1.70f), 1.40f, true,
                    fgCanopyColor, 32, spriteMat, 0.7f, 0.6f);

                SpawnPrefab(pfClutterOverhead, "FG_Canopy_Framing_210m", fgRoot.transform,
                    new Vector3(startX + 72.0f, 8.1f, -1.65f), 1.35f, false,
                    fgCanopyColor, 32, spriteMat, 0.8f, 0.7f);

                SpawnPrefab(pfClutterOverhead, "FG_Canopy_Framing_236m", fgRoot.transform,
                    new Vector3(startX + 98.0f, 8.1f, -1.65f), 1.45f, true,
                    fgCanopyColor, 32, spriteMat, 0.8f, 0.7f);
            }

            // Mép đáy màn hình: Vài nhánh lúa tiền cảnh mọc lướt qua
            Sprite fgSpr = sprPanicle ?? sprCluster ?? sprMedium;
            for (float fx = startX + 6f; fx <= endX - 6f; fx += 8.5f)
            {
                float jx = fx + Mathf.Sin(fx * 3.1f) * 0.5f;
                float fy = pathY - 0.72f;
                Color fgStalkColor = GetAtmosphericTint(-1.8f, new Color(0.95f, 0.85f, 0.45f, 0.95f));
                Spawn2DSprite(fgSpr, $"FG_LowStalk_{fx:F0}", fgRoot.transform,
                    new Vector3(jx, fy, -1.8f), new Vector2((fx % 4.0f > 2.0f) ? -0.32f : 0.32f, 0.32f),
                    fgStalkColor, 25, spriteMat, 1.4f, 1.1f);
            }

            // =========================================================================
            // RUỘNG BẬC THANG CẬN CẢNH (TIER 1 - CẬN: Z = 1.8m, Y = -1.95m)
            // =========================================================================
            var tier1Root = new GameObject("03_TerracedPlots_Tier1_Near");
            tier1Root.transform.SetParent(root.transform, false);

            float t1Z = 1.8f;
            float t1Y = pathY - 0.10f;
            Color t1Gold = GetAtmosphericTint(t1Z, new Color(1.00f, 0.94f, 0.58f, 1.0f));

            BuildTerracedPlot(tier1Root.transform, "Tier1_Plot1_GoldRice", startX + 6f, startX + 24f, t1Z, t1Y, false, sprWideRow, sprClump, waterMat, spriteMat, t1Gold, 10);
            BuildTerracedPlot(tier1Root.transform, "Tier1_Plot2_WaterMirror", startX + 24f, startX + 46f, t1Z, t1Y - 0.05f, true, sprClump, sprPanicle, waterMat, spriteMat, t1Gold, 10);
            BuildTerracedPlot(tier1Root.transform, "Tier1_Plot3_GoldRice", startX + 46f, startX + 68f, t1Z, t1Y, false, sprWideRow, sprMedium, waterMat, spriteMat, t1Gold, 10);
            BuildTerracedPlot(tier1Root.transform, "Tier1_Plot4_WaterMirror", startX + 68f, startX + 88f, t1Z, t1Y - 0.05f, true, sprClump, sprPanicle, waterMat, spriteMat, t1Gold, 10);
            BuildTerracedPlot(tier1Root.transform, "Tier1_Plot5_GoldRice", startX + 88f, endX - 2f, t1Z, t1Y, false, sprWideRow, sprDenseHead, waterMat, spriteMat, t1Gold, 10);

            // =========================================================================
            // RUỘNG BẬC THANG TRUNG CẢNH (TIER 2 - GIỮA: Z = 3.6m, Y = -1.75m)
            // =========================================================================
            var tier2Root = new GameObject("04_TerracedPlots_Tier2_Mid");
            tier2Root.transform.SetParent(root.transform, false);

            float t2Z = 3.6f;
            float t2Y = pathY + 0.15f;
            Color t2Gold = GetAtmosphericTint(t2Z, new Color(0.94f, 0.90f, 0.60f, 1.0f));

            BuildTerracedPlot(tier2Root.transform, "Tier2_Plot1_WaterMirror", startX + 5f, startX + 26f, t2Z, t2Y - 0.05f, true, sprClump, sprPanicle, waterMat, spriteMat, t2Gold, 8);
            BuildTerracedPlot(tier2Root.transform, "Tier2_Plot2_GoldRice", startX + 26f, startX + 48f, t2Z, t2Y, false, sprWideRow, sprMedium, waterMat, spriteMat, t2Gold, 8);
            BuildTerracedPlot(tier2Root.transform, "Tier2_Plot3_WaterMirror", startX + 48f, startX + 70f, t2Z, t2Y - 0.05f, true, sprClump, sprPanicle, waterMat, spriteMat, t2Gold, 8);
            BuildTerracedPlot(tier2Root.transform, "Tier2_Plot4_GoldRice", startX + 70f, startX + 90f, t2Z, t2Y, false, sprWideRow, sprDenseHead, waterMat, spriteMat, t2Gold, 8);
            BuildTerracedPlot(tier2Root.transform, "Tier2_Plot5_WaterMirror", startX + 90f, endX - 1f, t2Z, t2Y - 0.05f, true, sprClump, sprPanicle, waterMat, spriteMat, t2Gold, 8);

            // =========================================================================
            // BỐ CỤC 7 VÙNG NHỊP ĐIỆU CÓ Ý THỨC & 3 TẦNG QUẦN TỤ CHUẨN KÍCH THƯỚC (Pillars 2, 4)
            // =========================================================================
            var clusterRoot = new GameObject("05_StoryClusters_And_NegativeSpaces");
            clusterRoot.transform.SetParent(root.transform, false);

            // -------------------------------------------------------------------------
            // VÙNG 1 (X = 138m - 148m): CỤM LÁN NGHỈ ĐẦU RUỘNG
            // -------------------------------------------------------------------------
            var z1Root = new GameObject("Zone1_RestShelterCluster_138m_148m");
            z1Root.transform.SetParent(clusterRoot.transform, false);

            float z1BaseZ = 2.4f;
            float z1GroundY = GetDioramaFloorY(pathY, z1BaseZ);

            if (pfWaitingShelter != null)
            {
                Color shelterColor = GetAtmosphericTint(z1BaseZ, new Color(0.95f, 0.92f, 0.85f, 1f));
                // Target height: 3.2m
                SpawnNormalizedPrefab(pfWaitingShelter, "Z1_WoodenWaitingShelter", z1Root.transform,
                    new Vector3(startX + 4.2f, z1GroundY, z1BaseZ), 3.2f, false,
                    shelterColor, 15, spriteMat, 0f, 0f);

                if (pfClutterCottage != null)
                {
                    SpawnNormalizedPrefab(pfClutterCottage, "Z1_Shelter_FootClutter", z1Root.transform,
                        new Vector3(startX + 4.2f, z1GroundY, z1BaseZ - 0.35f), 0.75f, false,
                        Color.white, 16, spriteMat, 0f, 0f);
                }
            }

            // Bụi chuối chuẩn 2.6m ôm phía sau lán nghỉ
            if (pfBanana != null)
            {
                Color bananaColor = GetAtmosphericTint(3.0f, new Color(0.55f, 0.68f, 0.45f, 1f));
                SpawnNormalizedPrefab(pfBanana, "Z1_Banana_Back1", z1Root.transform,
                    new Vector3(startX + 1.8f, GetDioramaFloorY(pathY, 3.0f), 3.0f), 2.6f, false,
                    bananaColor, 14, spriteMat, 1.4f, 1.1f);

                SpawnNormalizedPrefab(pfBanana, "Z1_Banana_Back2", z1Root.transform,
                    new Vector3(startX + 6.8f, GetDioramaFloorY(pathY, 2.8f), 2.8f), 2.4f, true,
                    bananaColor, 14, spriteMat, 1.3f, 1.0f);
            }

            if (pfReeds != null)
            {
                Color reedColor = GetAtmosphericTint(1.4f, new Color(0.85f, 0.82f, 0.65f, 1f));
                SpawnPrefab(pfReeds, "Z1_Dike_Reeds", z1Root.transform,
                    new Vector3(startX + 5.8f, pathY + 0.12f, 1.4f), 0.38f, false,
                    reedColor, 14, spriteMat, 1.6f, 1.3f);
            }

            // -------------------------------------------------------------------------
            // VÙNG 2 (X = 148m - 164m): KHOẢNG THỞ 1 (OPEN SCENIC DIKE)
            // Không gian khoáng đạt, ngắm trọn đồng lúa nước tráng gương, không nhét cây to
            // -------------------------------------------------------------------------
            var z2Root = new GameObject("Zone2_OpenScenicDike_148m_164m");
            z2Root.transform.SetParent(clusterRoot.transform, false);

            for (float ox = startX + 12f; ox <= startX + 24f; ox += 3.5f)
            {
                float jx = ox + Mathf.Sin(ox * 1.7f) * 0.4f;
                float oz = 5.5f + Mathf.Cos(ox * 1.2f) * 0.8f;
                Color farRiceColor = GetAtmosphericTint(oz, new Color(0.92f, 0.88f, 0.62f, 1f));
                Spawn2DSprite(sprWideRow ?? sprMedium, $"Z2_FarScenicRice_{jx:F0}", z2Root.transform,
                    new Vector3(jx, GetDioramaFloorY(pathY, oz) + 0.15f, oz), new Vector2(0.45f, 0.45f),
                    farRiceColor, 7, spriteMat, 1.2f, 1.0f);
            }

            // -------------------------------------------------------------------------
            // VÙNG 3 (X = 164m - 180m): CỤM QUẦN TỤ NHÀ TRANH 1 (PRIMARY CLUSTER)
            // Nhà tranh 3.8m + Giàn bầu 2.2m + Cây dừa 7.2m nghiêng ôm nhà + Bụi chuối 2.6m + Rặng tre làng 6.2m
            // -------------------------------------------------------------------------
            var z3Root = new GameObject("Zone3_CottageCluster1_164m_180m");
            z3Root.transform.SetParent(clusterRoot.transform, false);

            float z3HutZ = 5.2f;
            float z3HutGroundY = GetDioramaFloorY(pathY, z3HutZ);

            if (pfThatchedHut != null)
            {
                Color hut1Color = GetAtmosphericTint(z3HutZ, new Color(0.94f, 0.90f, 0.84f, 1f));
                // Target height: 3.8m (Chuẩn tỉ lệ 2.5x Minh)
                SpawnNormalizedPrefab(pfThatchedHut, "Z3_Cottage_VillageThatched_01", z3Root.transform,
                    new Vector3(startX + 30.0f, z3HutGroundY, z3HutZ), 3.8f, false,
                    hut1Color, 7, spriteMat, 0f, 0f);

                if (pfClutterCottage != null)
                {
                    // Đặt trực tiếp trước hiên nhà tranh (Z = 4.6m) để lộ thúng tre và bao thóc rõ ràng
                    SpawnNormalizedPrefab(pfClutterCottage, "Z3_Hut1_FrontPorch_Clutter", z3Root.transform,
                        new Vector3(startX + 29.5f, GetDioramaFloorY(pathY, 4.6f), 4.6f), 0.85f, false,
                        Color.white, 9, spriteMat, 0f, 0f);
                }

                if (pfTrellis != null)
                {
                    Color trellisColor = GetAtmosphericTint(4.8f, new Color(0.92f, 0.94f, 0.88f, 1f));
                    // Target height: 2.2m
                    SpawnNormalizedPrefab(pfTrellis, "Z3_Hut1_GourdTrellis", z3Root.transform,
                        new Vector3(startX + 34.8f, GetDioramaFloorY(pathY, 4.8f), 4.8f), 2.2f, false,
                        trellisColor, 8, spriteMat, 0.6f, 0.6f);
                }

                if (pfCoconut != null)
                {
                    Color cocoColor = GetAtmosphericTint(6.2f, new Color(0.60f, 0.70f, 0.55f, 1f));
                    // Target height: 7.2m
                    SpawnNormalizedPrefab(pfCoconut, "Z3_Hut1_Coconut_Tilting", z3Root.transform,
                        new Vector3(startX + 27.2f, GetDioramaFloorY(pathY, 6.2f), 6.2f), 7.2f, false,
                        cocoColor, 6, spriteMat, 1.1f, 0.9f);

                    if (pfClutterCoconut != null)
                    {
                        SpawnNormalizedPrefab(pfClutterCoconut, "Z3_Hut1_Coconut_BaseClutter", z3Root.transform,
                            new Vector3(startX + 27.2f, GetDioramaFloorY(pathY, 6.2f), 6.0f), 0.75f, false,
                            Color.white, 7, spriteMat, 0f, 0f);
                    }
                }

                if (pfBanana != null)
                {
                    Color bananaColor = GetAtmosphericTint(5.8f, new Color(0.55f, 0.65f, 0.50f, 1f));
                    // Chuẩn 2.6m: KHÔNG BAO GIỜ CAO GẤP 3 LẦN NHÀ NỮA!
                    SpawnNormalizedPrefab(pfBanana, "Z3_Hut1_Banana_Back", z3Root.transform,
                        new Vector3(startX + 33.2f, GetDioramaFloorY(pathY, 5.8f), 5.8f), 2.6f, true,
                        bananaColor, 6, spriteMat, 1.3f, 1.0f);
                }

                // Rặng tre làng ôm bọc sau lưng nhà tranh 1 (Z = 7.5m)
                if (pfBamboo != null)
                {
                    Color bambooColor = GetAtmosphericTint(7.5f, new Color(0.50f, 0.64f, 0.46f, 1f));
                    SpawnNormalizedPrefab(pfBamboo, "Z3_Hut1_BackBamboo", z3Root.transform,
                        new Vector3(startX + 25.5f, GetDioramaFloorY(pathY, 7.5f), 7.5f), 6.5f, false,
                        bambooColor, 5, spriteMat, 1.1f, 0.9f);
                }
            }

            // -------------------------------------------------------------------------
            // VÙNG 4 (X = 180m - 192m): VÙNG LÚA CAO TRÀN BỜ ĐÊ (GAMEPLAY CROUCH POCKET)
            // Khóm lúa cao 1.25m kẹp sát 2 bên mép đường, phục vụ gameplay ẩn nấp / khom người
            // -------------------------------------------------------------------------
            var z4Root = new GameObject("Zone4_Gameplay_CrouchPocket_180m_192m");
            z4Root.transform.SetParent(clusterRoot.transform, false);

            for (float cx = startX + 42f; cx <= startX + 54f; cx += 1.6f)
            {
                float jx = cx + Mathf.Sin(cx * 2.8f) * 0.25f;
                // Mép trước đường (Z = -0.35m)
                Color fgRiceCol = GetAtmosphericTint(-0.35f, new Color(1.0f, 0.95f, 0.65f, 1f));
                Spawn2DSprite(sprTallSway ?? sprPanicle, $"CrouchPocket_FrontRice_{jx:F0}", z4Root.transform,
                    new Vector3(jx, pathY - 0.05f, -0.35f), new Vector2((cx % 3f > 1.5f) ? -0.52f : 0.52f, 0.52f),
                    fgRiceCol, 18, spriteMat, 1.8f, 1.3f);

                // Mép sau đường (Z = +0.55m)
                Color bgRiceCol = GetAtmosphericTint(0.55f, new Color(0.98f, 0.92f, 0.58f, 1f));
                Spawn2DSprite(sprTallSway ?? sprClump, $"CrouchPocket_BackRice_{jx:F0}", z4Root.transform,
                    new Vector3(jx + 0.35f, pathY + 0.02f, 0.55f), new Vector2((cx % 4f > 2f) ? -0.48f : 0.48f, 0.48f),
                    bgRiceCol, 12, spriteMat, 1.7f, 1.2f);
            }

            // -------------------------------------------------------------------------
            // VÙNG 5 (X = 192m - 208m): CỤM QUẦN TỤ NHÀ TRANH 2 (VILLAGE SHADE CLUSTER)
            // Nhà tranh 3.8m + Rặng tre làng 6.2m + Cây bóng mát 4.5m + Dừa 7.2m
            // -------------------------------------------------------------------------
            var z5Root = new GameObject("Zone5_CottageCluster2_192m_208m");
            z5Root.transform.SetParent(clusterRoot.transform, false);

            float z5HutZ = 5.6f;
            float z5HutGroundY = GetDioramaFloorY(pathY, z5HutZ);

            if (pfThatchedHut != null)
            {
                Color hut2Color = GetAtmosphericTint(z5HutZ, new Color(0.92f, 0.88f, 0.82f, 1f));
                // Target height: 3.8m
                SpawnNormalizedPrefab(pfThatchedHut, "Z5_Cottage_VillageThatched_02", z5Root.transform,
                    new Vector3(startX + 64.0f, z5HutGroundY, z5HutZ), 3.8f, false,
                    hut2Color, 7, spriteMat, 0f, 0f);

                if (pfClutterCottage != null)
                {
                    // Đặt trước hiên nhà tranh 2 (Z = 5.0m)
                    SpawnNormalizedPrefab(pfClutterCottage, "Z5_Hut2_FrontPorch_Clutter", z5Root.transform,
                        new Vector3(startX + 63.5f, GetDioramaFloorY(pathY, 5.0f), 5.0f), 0.85f, false,
                        Color.white, 9, spriteMat, 0f, 0f);
                }

                if (pfBamboo != null)
                {
                    Color bambooColor = GetAtmosphericTint(6.4f, new Color(0.52f, 0.66f, 0.48f, 1f));
                    // Rặng tre: 6.2m
                    SpawnNormalizedPrefab(pfBamboo, "Z5_Hut2_BambooGrove", z5Root.transform,
                        new Vector3(startX + 68.5f, GetDioramaFloorY(pathY, 6.4f), 6.4f), 6.2f, false,
                        bambooColor, 6, spriteMat, 1.2f, 1.0f);
                }

                if (pfMedCanopy != null)
                {
                    Color shadeTreeColor = GetAtmosphericTint(6.5f, new Color(0.58f, 0.68f, 0.52f, 1f));
                    // Cây bóng mát: 4.5m
                    SpawnNormalizedPrefab(pfMedCanopy, "Z5_Hut2_ShadeTree", z5Root.transform,
                        new Vector3(startX + 59.8f, GetDioramaFloorY(pathY, 6.5f), 6.5f), 4.5f, false,
                        shadeTreeColor, 6, spriteMat, 0.9f, 0.8f);
                }

                if (pfCoconut != null)
                {
                    Color coco2Color = GetAtmosphericTint(6.8f, new Color(0.56f, 0.66f, 0.50f, 1f));
                    // Cây dừa: 7.2m
                    SpawnNormalizedPrefab(pfCoconut, "Z5_Hut2_Coconut_Tilting", z5Root.transform,
                        new Vector3(startX + 62.0f, GetDioramaFloorY(pathY, 6.8f), 6.8f), 7.2f, true,
                        coco2Color, 5, spriteMat, 1.1f, 0.9f);

                    if (pfClutterCoconut != null)
                    {
                        SpawnNormalizedPrefab(pfClutterCoconut, "Z5_Hut2_Coconut_BaseClutter", z5Root.transform,
                            new Vector3(startX + 62.0f, GetDioramaFloorY(pathY, 6.8f), 6.6f), 0.75f, true,
                            Color.white, 7, spriteMat, 0f, 0f);
                    }
                }
            }

            // -------------------------------------------------------------------------
            // VÙNG 6 (X = 208m - 224m): KHOẢNG THỞ 2 (ĐÊ LỘNG GIÓ)
            // Gió thổi gợn sóng qua thảm lúa vàng và mặt nước, tạo cảm xúc thư thái trước điểm mốc
            // -------------------------------------------------------------------------
            var z6Root = new GameObject("Zone6_BreezyOpenDike_208m_224m");
            z6Root.transform.SetParent(clusterRoot.transform, false);

            for (float bx = startX + 72f; bx <= startX + 84f; bx += 3.5f)
            {
                float jx = bx + Mathf.Sin(bx * 2.2f) * 0.4f;
                float bz = 5.2f + Mathf.Cos(bx * 1.5f) * 0.7f;
                Color breezeRiceColor = GetAtmosphericTint(bz, new Color(0.90f, 0.86f, 0.58f, 1f));
                Spawn2DSprite(sprWideRow ?? sprDenseHead, $"Z6_BreezyRice_{jx:F0}", z6Root.transform,
                    new Vector3(jx, GetDioramaFloorY(pathY, bz) + 0.12f, bz), new Vector2(0.46f, 0.46f),
                    breezeRiceColor, 7, spriteMat, 1.5f, 1.2f);
            }

            // -------------------------------------------------------------------------
            // VÙNG 7 (X = 224m - 238m): CỤM CỘT MỐC CUỐI (GRAND BANYAN LANDMARK)
            // Cây đa cổ thụ 9.8m + Cặp dừa đôi 7.2m + Phiến đá dừng chân
            // -------------------------------------------------------------------------
            var z7Root = new GameObject("Zone7_GrandBanyanLandmark_224m_238m");
            z7Root.transform.SetParent(clusterRoot.transform, false);

            float z7BanyanZ = 4.2f;
            float z7BanyanGroundY = GetDioramaFloorY(pathY, z7BanyanZ);

            if (pfBigCanopy != null)
            {
                Color banyanColor = GetAtmosphericTint(z7BanyanZ, new Color(0.58f, 0.68f, 0.52f, 1f));
                // Cây đa cổ thụ đại thụ: 9.8m
                SpawnNormalizedPrefab(pfBigCanopy, "Z7_EndField_AncientBanyan", z7Root.transform,
                    new Vector3(startX + 94.0f, z7BanyanGroundY, z7BanyanZ), 9.8f, false,
                    banyanColor, 7, spriteMat, 0.7f, 0.7f);

                if (pfClutterCoconut != null)
                {
                    SpawnNormalizedPrefab(pfClutterCoconut, "Z7_EndField_Banyan_BaseClutter", z7Root.transform,
                        new Vector3(startX + 94.0f, z7BanyanGroundY, z7BanyanZ - 0.25f), 1.0f, false,
                        Color.white, 8, spriteMat, 0f, 0f);
                }
            }

            if (pfCoconut != null)
            {
                Color cocoTwin1 = GetAtmosphericTint(4.8f, new Color(0.55f, 0.65f, 0.48f, 1f));
                SpawnNormalizedPrefab(pfCoconut, "Z7_EndField_CoconutLeft", z7Root.transform,
                    new Vector3(startX + 87.5f, GetDioramaFloorY(pathY, 4.8f), 4.8f), 7.2f, false,
                    cocoTwin1, 6, spriteMat, 1.1f, 0.9f);

                Color cocoTwin2 = GetAtmosphericTint(5.2f, new Color(0.50f, 0.60f, 0.45f, 1f));
                SpawnNormalizedPrefab(pfCoconut, "Z7_EndField_CoconutRight", z7Root.transform,
                    new Vector3(startX + 100.5f, GetDioramaFloorY(pathY, 5.2f), 5.2f), 7.5f, true,
                    cocoTwin2, 5, spriteMat, 1.0f, 0.8f);

                if (pfClutterCoconut != null)
                {
                    SpawnNormalizedPrefab(pfClutterCoconut, "Z7_EndField_LeftCoconut_BaseClutter", z7Root.transform,
                        new Vector3(startX + 87.5f, GetDioramaFloorY(pathY, 4.8f), 4.6f), 0.85f, false,
                        Color.white, 7, spriteMat, 0f, 0f);

                    SpawnNormalizedPrefab(pfClutterCoconut, "Z7_EndField_RightCoconut_BaseClutter", z7Root.transform,
                        new Vector3(startX + 100.5f, GetDioramaFloorY(pathY, 5.2f), 5.0f), 0.85f, true,
                        Color.white, 7, spriteMat, 0f, 0f);
                }
            }

            // Save and Log
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"<color=#10b981>[RiceField100m]</color> ĐÃ DỰNG THÀNH CÔNG Cánh đồng lúa 100m chuẩn 4 TRỤ CỘT ĐỘ SÂU 2.5D!");
        }

        // =========================================================================
        // HELPER: TÍNH ĐỘ CAO MẶT NỀN DIORAMA Y THEO CHIỀU SÂU Z (Bù thị sai Camera)
        // =========================================================================
        public static float GetDioramaFloorY(float pathY, float z)
        {
            // Độ dốc nhẹ 0.5 độ tạo cảm giác ruộng bậc thang thoải dần
            float slopeAngleDeg = 0.50f;
            float tanSlope = Mathf.Tan(slopeAngleDeg * Mathf.Deg2Rad);
            return pathY + (z - 0.45f) * tanSlope - 0.08f;
        }

        // =========================================================================
        // TRỤ CỘT 3: HỆ THỐNG SẮC ĐỘ & KHÍ QUYỂN THEO CHIỀU SÂU Z (Atmospheric Depth Color)
        // =========================================================================
        public static Color GetAtmosphericTint(float z, Color baseColor)
        {
            if (z <= -1.0f)
            {
                // Tiền cảnh cận camera (Foreground silhouette): Tối sẫm 38%
                Color fgDark = new Color(0.38f, 0.42f, 0.38f, 1.0f);
                return Color.Lerp(baseColor, fgDark, 0.38f);
            }
            else if (z <= 1.0f)
            {
                // Lối đi nhân vật (Hero Gameplay Path): Ấm áp rực rỡ nắng vàng
                return baseColor;
            }
            else
            {
                // Hậu cảnh trung & viễn (Midground to Deep Background):
                // Pha dần sắc lam sương khói sông nước Nam Bộ theo chiều sâu Z
                Color riverFogTint = new Color(0.58f, 0.70f, 0.82f, 1.0f);
                float t = Mathf.Clamp01((z - 1.0f) / 14.0f) * 0.35f;
                return Color.Lerp(baseColor, riverFogTint, t);
            }
        }

        // =========================================================================
        // TRỤ CỘT 2: KHÓA TRẦN KÍCH THƯỚC TUYỆT ĐỐI THEO MÉT (Scale Normalizer)
        // =========================================================================
        private static GameObject SpawnNormalizedPrefab(
            GameObject prefab, string name, Transform parent,
            Vector3 targetPos, float targetHeightMeters,
            bool flipX, Color tintColor, int sortOrder, Material mat,
            float swayAngle = 0f, float swaySpeed = 0f)
        {
            if (prefab == null) return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;

            // Đo chiều cao thực tế qua Bounds của tất cả Renderers
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            float currentHeight = 1.0f;
            if (renderers.Length > 0)
            {
                Bounds b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    b.Encapsulate(renderers[i].bounds);
                currentHeight = Mathf.Max(b.size.y, 0.1f);
            }

            // Tỉ lệ scale chuẩn xác theo mét
            float scaleRatio = targetHeightMeters / currentHeight;
            Vector3 s = go.transform.localScale * scaleRatio;
            if (flipX) s.x = -Mathf.Abs(s.x);
            go.transform.localScale = s;

            // Chôn chân đế cắm sâu 0.12m vào lòng đất
            renderers = go.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length > 0)
            {
                Bounds b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    b.Encapsulate(renderers[i].bounds);
                float bottomOffset = go.transform.position.y - b.min.y;
                targetPos.y = targetPos.y + bottomOffset - 0.12f;
            }
            go.transform.position = targetPos;

            // Gán Tint và Sorting
            var srs = go.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in srs)
            {
                if (mat != null) sr.sharedMaterial = mat;
                sr.color = tintColor;
                sr.sortingOrder = sortOrder;
            }

            // Re-initialize Foliage Sway
            var sways = go.GetComponentsInChildren<PlanetOfLanaFoliageSway>(true);
            foreach (var sway in sways)
            {
                ReinitializeSway(sway);
                if (swayAngle > 0.05f)
                    ConfigureSway(sway, swayAngle, swaySpeed, 0.15f);
            }

            if (sways.Length == 0 && swayAngle > 0.05f)
            {
                var sway = go.AddComponent<PlanetOfLanaFoliageSway>();
                ConfigureSway(sway, swayAngle, swaySpeed, 0.15f);
            }

            return go;
        }

        // =========================================================================
        // TRỤ CỘT 1: TẠO MESH ĐẾ HỘP DIORAMA LIÊN TỤC (Continuous Sloped Diorama Mesh)
        // =========================================================================
        private static void CreateContinuousDioramaFloor(
            Transform parent, string name, float startX, float endX, float pathY, Material floorMat)
        {
            var floorGO = new GameObject(name);
            floorGO.transform.SetParent(parent, false);

            if (!Directory.Exists(GenMeshDir)) Directory.CreateDirectory(GenMeshDir);

            Mesh dioramaMesh = BuildDioramaFloorMesh($"{name}_Mesh", startX, endX, pathY);
            AssetDatabase.CreateAsset(dioramaMesh, $"{GenMeshDir}/{name}_Mesh.asset");

            var mf = floorGO.AddComponent<MeshFilter>();
            mf.sharedMesh = dioramaMesh;

            var mr = floorGO.AddComponent<MeshRenderer>();
            mr.sharedMaterial = floorMat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        private static Mesh BuildDioramaFloorMesh(string meshName, float startX, float endX, float pathY)
        {
            int cols = 120;
            float[] depthZs = new float[] { 0.45f, 1.8f, 3.6f, 5.5f, 7.5f, 9.5f, 12.0f };
            int rows = depthZs.Length;

            var verts = new List<Vector3>();
            var colors = new List<Color>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            for (int r = 0; r < rows; r++)
            {
                float z = depthZs[r];
                float y = GetDioramaFloorY(pathY, z);
                float v = (float)r / (rows - 1);

                for (int c = 0; c <= cols; c++)
                {
                    float u = (float)c / cols;
                    float x = Mathf.Lerp(startX, endX, u);

                    // Gợn sóng địa hình đất ruộng mềm mại
                    float undulation = Mathf.Sin(x * 0.45f + r * 1.2f) * 0.04f + Mathf.Cos(x * 1.1f) * 0.02f;
                    verts.Add(new Vector3(x, y + undulation, z));

                    // Vertex color: Màu đất cỏ mềm mại pha sương lam
                    Color baseGroundColor = new Color(0.48f, 0.52f, 0.36f, 1.0f);
                    Color vertexCol = GetAtmosphericTint(z, baseGroundColor);
                    colors.Add(vertexCol);

                    uvs.Add(new Vector2(u * 16f, v * 3f));
                }
            }

            int colStride = cols + 1;
            for (int r = 0; r < rows - 1; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    int i0 = r * colStride + c;
                    int i1 = i0 + 1;
                    int i2 = (r + 1) * colStride + c;
                    int i3 = i2 + 1;

                    tris.Add(i0); tris.Add(i2); tris.Add(i1);
                    tris.Add(i1); tris.Add(i2); tris.Add(i3);
                }
            }

            Mesh m = new Mesh { name = meshName };
            m.SetVertices(verts);
            m.SetColors(colors);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        // =========================================================================
        // MODULAR CLUTTER PREFABS (4 Cụm trang trí chuẩn hóa)
        // =========================================================================
        private static void EnsureModularClutterPrefabs(Material spriteMat)
        {
            if (!Directory.Exists(ClutterDir)) Directory.CreateDirectory(ClutterDir);

            // 1. PF_Clutter_ChanCot_NhaTranh
            string pathCottage = $"{ClutterDir}/PF_Clutter_ChanCot_NhaTranh.prefab";
            if (!File.Exists(pathCottage))
            {
                var go = new GameObject("PF_Clutter_ChanCot_NhaTranh");
                AddChildPrefab("Assets/Prefabs/Environment/Lana_VN_Pack/Grass_Vines/PF_LanaVN_GrassCarpet_05_Dense.prefab", "Grass_Dense", go.transform, new Vector3(0f, 0.05f, 0f), 0.50f, false, spriteMat);
                AddChildPrefab("Assets/Prefabs/Props/PF_Prop_RiceSacks.prefab", "Prop_RiceSack", go.transform, new Vector3(-0.65f, 0f, -0.10f), 0.75f, false, spriteMat);
                AddChildPrefab("Assets/Prefabs/Props/PF_Prop_Bamboo_Basket.prefab", "Prop_Basket", go.transform, new Vector3(-0.15f, 0f, -0.12f), 0.60f, false, spriteMat);
                AddChildPrefab("Assets/Prefabs/Props/PF_Prop_SaltFish_Barrel.prefab", "Prop_Jar", go.transform, new Vector3(0.75f, 0f, -0.08f), 0.65f, false, spriteMat);
                AddChildPrefab("Assets/Prefabs/Environment/Lana_VN_Pack/Wildflowers/PF_LanaVN_Wildflower_05_YellowBlossom.prefab", "Flowers", go.transform, new Vector3(0.55f, 0.05f, -0.15f), 0.40f, false, spriteMat);
                PrefabUtility.SaveAsPrefabAsset(go, pathCottage);
                Object.DestroyImmediate(go);
            }

            // 2. PF_Clutter_BoDe_GocDua
            string pathCoconut = $"{ClutterDir}/PF_Clutter_BoDe_GocDua.prefab";
            if (!File.Exists(pathCoconut))
            {
                var go = new GameObject("PF_Clutter_BoDe_GocDua");
                AddChildPrefab("Assets/Prefabs/Environment/Lana_VN_Pack/Grass_Vines/PF_LanaVN_GrassCarpet_05_Dense.prefab", "Grass_Mound", go.transform, new Vector3(0f, 0.05f, 0f), 0.65f, false, spriteMat);
                AddChildPrefab("Assets/Prefabs/Environment/Lana_VN_Pack/Grass_Vines/PF_LanaVN_GrassCarpet_01.prefab", "Grass_Tuft", go.transform, new Vector3(0.40f, 0.05f, -0.05f), 0.40f, true, spriteMat);
                AddChildPrefab("Assets/Prefabs/Environment/Lana_VN_Pack/Wildflowers/PF_LanaVN_Wildflower_01_Reeds.prefab", "Reeds", go.transform, new Vector3(0.55f, 0.10f, -0.10f), 0.35f, false, spriteMat);
                AddChildPrefab("Assets/Prefabs/Environment/Lana_VN_Pack/Wildflowers/PF_LanaVN_Wildflower_05_YellowBlossom.prefab", "YellowFlower", go.transform, new Vector3(-0.40f, 0.05f, -0.08f), 0.40f, false, spriteMat);
                PrefabUtility.SaveAsPrefabAsset(go, pathCoconut);
                Object.DestroyImmediate(go);
            }

            // 3. PF_Clutter_DauVet_NongThon
            string pathRural = $"{ClutterDir}/PF_Clutter_DauVet_NongThon.prefab";
            if (!File.Exists(pathRural))
            {
                var go = new GameObject("PF_Clutter_DauVet_NongThon");
                AddChildPrefab("Assets/Prefabs/Props/PF_Prop_Fishing_Net.prefab", "FishingNet_Racks", go.transform, new Vector3(-0.65f, 0f, 0f), 0.80f, false, spriteMat);
                AddChildPrefab("Assets/Prefabs/Props/PF_Prop_RiceSacks.prefab", "RiceSacks_Pile", go.transform, new Vector3(0.20f, 0f, -0.05f), 0.85f, false, spriteMat);
                AddChildPrefab("Assets/Prefabs/Props/PF_Prop_Bamboo_Basket.prefab", "BambooBasket", go.transform, new Vector3(0.75f, 0f, -0.08f), 0.65f, false, spriteMat);
                AddChildPrefab("Assets/Prefabs/Environment/Lana_Concept_Assets/Rocks/PF_T_HD_Rock_04_FlatSlab.prefab", "RestingStone", go.transform, new Vector3(-0.15f, -0.02f, 0.05f), 0.40f, false, spriteMat);
                PrefabUtility.SaveAsPrefabAsset(go, pathRural);
                Object.DestroyImmediate(go);
            }

            // 4. PF_Clutter_TienCanh_Overhead
            string pathOverhead = $"{ClutterDir}/PF_Clutter_TienCanh_Overhead.prefab";
            if (!File.Exists(pathOverhead))
            {
                var go = new GameObject("PF_Clutter_TienCanh_Overhead");
                AddChildPrefab("Assets/Prefabs/Vegetation/PF_FG_NipaPalm_Fronds.prefab", "NipaPalm_Fronds", go.transform, new Vector3(0f, -0.6f, 0f), 1.45f, false, spriteMat);
                AddChildPrefab("Assets/Prefabs/Vegetation/PF_FG_Vines_Hanging.prefab", "Hanging_Vines", go.transform, new Vector3(-0.85f, -0.9f, -0.05f), 1.20f, false, spriteMat);
                PrefabUtility.SaveAsPrefabAsset(go, pathOverhead);
                Object.DestroyImmediate(go);
            }
        }

        private static void AddChildPrefab(string prefabPath, string childName, Transform parent, Vector3 localPos, float localScale, bool flipX, Material mat)
        {
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (pf == null) return;
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(pf, parent);
            inst.name = childName;
            inst.transform.localPosition = localPos;
            Vector3 s = inst.transform.localScale * localScale;
            if (flipX) s.x = -Mathf.Abs(s.x);
            inst.transform.localScale = s;

            if (mat != null)
            {
                var srs = inst.GetComponentsInChildren<SpriteRenderer>(true);
                foreach (var sr in srs) sr.sharedMaterial = mat;
            }
        }

        // =========================================================================
        // HELPER: TẠO MESH LEDGE CHUẨN CẢNH TRƯỚC
        // =========================================================================
        private static void CreateLanaLedgeSection(
            Transform parent, string name, float startX, float endX,
            int segmentsX, float frontZ, float backZ, float bottomY,
            System.Func<float, float> heightFunc,
            Material grassMat, Material rockMat)
        {
            var sectionRoot = new GameObject(name);
            sectionRoot.transform.SetParent(parent, false);

            if (!Directory.Exists(GenMeshDir)) Directory.CreateDirectory(GenMeshDir);

            Mesh rockMesh = BuildChiseledRockMesh($"{name}_RockMesh", startX, endX, segmentsX, frontZ, backZ, bottomY, heightFunc);
            AssetDatabase.CreateAsset(rockMesh, $"{GenMeshDir}/{name}_RockMesh.asset");

            var rockGO = new GameObject($"{name}_RockUnderbelly");
            rockGO.transform.SetParent(sectionRoot.transform, false);
            rockGO.AddComponent<MeshFilter>().sharedMesh = rockMesh;
            var rockMR = rockGO.AddComponent<MeshRenderer>();
            rockMR.sharedMaterial = rockMat;
            rockMR.shadowCastingMode = ShadowCastingMode.Off;
            rockMR.receiveShadows = false;

            Mesh grassMesh = BuildOverhangingGrassCapMesh($"{name}_GrassMesh", startX, endX, segmentsX, frontZ - 0.05f, backZ, heightFunc);
            AssetDatabase.CreateAsset(grassMesh, $"{GenMeshDir}/{name}_GrassMesh.asset");

            var grassGO = new GameObject($"{name}_GrassTurfCap");
            grassGO.transform.SetParent(sectionRoot.transform, false);
            grassGO.AddComponent<MeshFilter>().sharedMesh = grassMesh;
            var grassMR = grassGO.AddComponent<MeshRenderer>();
            grassMR.sharedMaterial = grassMat;
            grassMR.shadowCastingMode = ShadowCastingMode.Off;
            grassMR.receiveShadows = false;
        }

        private static Mesh BuildChiseledRockMesh(
            string meshName, float startX, float endX, int segX,
            float frontZ, float backZ, float bottomY,
            System.Func<float, float> heightFunc)
        {
            int rowsY = 6;
            int cols = segX + 1;

            var verts = new List<Vector3>();
            var colors = new List<Color>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            for (int r = 0; r <= rowsY; r++)
            {
                float v = (float)r / rowsY;
                for (int c = 0; c < cols; c++)
                {
                    float u = (float)c / segX;
                    float x = Mathf.Lerp(startX, endX, u);
                    float surfY = heightFunc(x);
                    float topY = surfY - 0.18f;
                    float y = Mathf.Lerp(topY, bottomY, v);

                    float facet = Mathf.Sin(x * 1.45f + r * 1.9f) * 0.10f
                                + Mathf.Cos(x * 3.1f - r * 2.4f) * 0.05f;
                    float zOffset = facet * Mathf.Sin(v * Mathf.PI);
                    float z = frontZ + zOffset;

                    if (c == 0 || c == segX) z += 0.25f;

                    verts.Add(new Vector3(x, y, z));

                    float cliffFlag = Mathf.InverseLerp(-1.65f, 1.50f, surfY) * Mathf.Clamp01(1.0f - v * 1.8f);
                    float lipAO = (r == 0) ? 0.45f : ((r == 1) ? 0.85f : 1.0f);
                    float botAO = Mathf.Lerp(1.0f, 0.55f, Mathf.Pow(v, 1.5f));
                    colors.Add(new Color(0f, lipAO * botAO, cliffFlag, 1f));
                    uvs.Add(new Vector2(u * 14f, v * 2f));
                }
            }

            for (int r = 0; r < rowsY; r++)
            {
                for (int c = 0; c < segX; c++)
                {
                    int i0 = r * cols + c;
                    int i1 = i0 + 1;
                    int i2 = (r + 1) * cols + c;
                    int i3 = i2 + 1;
                    tris.Add(i0); tris.Add(i1); tris.Add(i2);
                    tris.Add(i1); tris.Add(i3); tris.Add(i2);
                }
            }

            float[] sideXs = new[] { startX, endX };
            for (int s = 0; s < 2; s++)
            {
                float sx = sideXs[s];
                float topY = heightFunc(sx) - 0.15f;
                int bIdx = verts.Count;
                verts.Add(new Vector3(sx, topY, frontZ + 0.25f));
                verts.Add(new Vector3(sx, topY, backZ));
                verts.Add(new Vector3(sx, bottomY, frontZ + 0.25f));
                verts.Add(new Vector3(sx, bottomY, backZ));
                for (int k = 0; k < 4; k++)
                {
                    colors.Add(new Color(0f, 0.80f, 0.4f, 1f));
                    uvs.Add(Vector2.zero);
                }
                if (s == 0)
                {
                    tris.Add(bIdx + 0); tris.Add(bIdx + 2); tris.Add(bIdx + 1);
                    tris.Add(bIdx + 1); tris.Add(bIdx + 2); tris.Add(bIdx + 3);
                }
                else
                {
                    tris.Add(bIdx + 0); tris.Add(bIdx + 1); tris.Add(bIdx + 2);
                    tris.Add(bIdx + 1); tris.Add(bIdx + 3); tris.Add(bIdx + 2);
                }
            }

            Mesh m = new Mesh { name = meshName };
            m.SetVertices(verts);
            m.SetColors(colors);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        private static Mesh BuildOverhangingGrassCapMesh(
            string meshName, float startX, float endX, int segX,
            float frontZ, float backZ,
            System.Func<float, float> heightFunc)
        {
            int profileCount = 6;
            int cols = segX + 1;

            var verts = new List<Vector3>();
            var colors = new List<Color>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            for (int c = 0; c < cols; c++)
            {
                float u = (float)c / segX;
                float x = Mathf.Lerp(startX, endX, u);
                float y = heightFunc(x);

                float scallop = Mathf.Sin(x * 2.4f) * 0.055f + Mathf.Cos(x * 5.2f) * 0.032f;

                verts.Add(new Vector3(x, y - 0.22f, backZ));
                verts.Add(new Vector3(x, y + 0.04f, backZ));
                verts.Add(new Vector3(x, y + 0.01f, 0.0f));
                verts.Add(new Vector3(x, y + 0.03f + scallop * 0.20f, frontZ + 0.16f));
                verts.Add(new Vector3(x, y - 0.11f + scallop * 0.60f, frontZ));
                verts.Add(new Vector3(x, y - 0.27f + scallop, frontZ + 0.15f));

                colors.Add(new Color(0.65f, 1f, 0f, 1f));
                colors.Add(new Color(1.00f, 1f, 0f, 1f));
                colors.Add(new Color(0.96f, 1f, 0f, 1f));
                colors.Add(new Color(0.92f, 1f, 0f, 1f));
                colors.Add(new Color(0.62f, 1f, 0f, 1f));
                colors.Add(new Color(0.30f, 1f, 0f, 1f));

                for (int p = 0; p < profileCount; p++)
                {
                    uvs.Add(new Vector2(u * 14f, (float)p / (profileCount - 1)));
                }
            }

            for (int c = 0; c < segX; c++)
            {
                int baseC = c * profileCount;
                int nextC = (c + 1) * profileCount;
                for (int p = 0; p < profileCount - 1; p++)
                {
                    int i0 = baseC + p;
                    int i1 = baseC + p + 1;
                    int i2 = nextC + p;
                    int i3 = nextC + p + 1;
                    tris.Add(i0); tris.Add(i2); tris.Add(i1);
                    tris.Add(i1); tris.Add(i2); tris.Add(i3);
                }
            }

            Mesh m = new Mesh { name = meshName };
            m.SetVertices(verts);
            m.SetColors(colors);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        // =========================================================================
        // RUỘNG BẬC THANG: Ô LÚA VÀNG HOẶC Ô NƯỚC TRÁNG GƯƠNG IN BÓNG TRỜI XANH
        // =========================================================================
        private static void BuildTerracedPlot(Transform parent, string plotName, float plotStartX, float plotEndX, float z, float y, bool isWaterMirror, Sprite riceSpr, Sprite borderSpr, Material waterMat, Material spriteMat, Color tint, int sortOrder)
        {
            var plotGo = new GameObject(plotName);
            plotGo.transform.SetParent(parent, false);

            float plotWidth = plotEndX - plotStartX;
            float centerX = plotStartX + plotWidth * 0.5f;

            if (isWaterMirror)
            {
                var mirrorQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                mirrorQuad.name = "Paddy_WaterMirror_SkyReflect";
                mirrorQuad.transform.SetParent(plotGo.transform, false);
                mirrorQuad.transform.position = new Vector3(centerX, y + 0.30f, z);
                mirrorQuad.transform.rotation = Quaternion.Euler(15f, 0f, 0f);
                mirrorQuad.transform.localScale = new Vector3(plotWidth, 0.75f, 1.0f);
                Object.DestroyImmediate(mirrorQuad.GetComponent<Collider>());

                var ren = mirrorQuad.GetComponent<MeshRenderer>();
                ren.sharedMaterial = waterMat;
                ren.sortingOrder = sortOrder;

                // Gieo các khóm mạ nhỏ đứng giữa và ven mặt ruộng nước
                if (borderSpr != null)
                {
                    for (float bx = plotStartX + 1f; bx <= plotEndX - 1f; bx += 2.4f)
                    {
                        float jx = bx + Mathf.Sin(bx * 2f) * 0.3f;
                        float jz = z - 0.15f + Mathf.Cos(bx * 1.5f) * 0.2f;
                        Spawn2DSprite(borderSpr, $"WaterEdge_Seedling_{jx:F0}", plotGo.transform,
                            new Vector3(jx, y + 0.08f, jz), new Vector2(0.36f, 0.36f),
                            tint, sortOrder + 1, spriteMat, 1.4f, 1.1f);
                    }
                }

                // Bờ ruộng đất nung bo 2 bên mép nước
                SpawnPaddyBund(plotGo.transform, "Bund_Left", plotStartX, y + 0.05f, z, sortOrder);
                SpawnPaddyBund(plotGo.transform, "Bund_Right", plotEndX, y + 0.05f, z, sortOrder);
            }
            else
            {
                if (riceSpr != null)
                {
                    for (float rx = plotStartX; rx <= plotEndX; rx += 2.0f)
                    {
                        float jx = rx + Mathf.Sin(rx * 1.8f) * 0.25f;
                        float jz = z + Mathf.Cos(rx * 1.2f) * 0.18f;
                        bool flip = (rx % 4.0f > 2.0f);
                        Spawn2DSprite(riceSpr, $"Plot_RiceRow_{jx:F0}", plotGo.transform,
                            new Vector3(jx, y + 0.05f, jz), new Vector2(flip ? -0.42f : 0.42f, 0.42f),
                            tint, sortOrder, spriteMat, 1.8f, 1.2f);
                    }
                }
                SpawnPaddyBund(plotGo.transform, "Bund_Left", plotStartX, y + 0.05f, z, sortOrder);
            }
        }

        private static void SpawnPaddyBund(Transform parent, string name, float x, float y, float z, int sortOrder)
        {
            var ridgeGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ridgeGo.name = name;
            ridgeGo.transform.SetParent(parent, false);
            ridgeGo.transform.position = new Vector3(x, y, z);
            ridgeGo.transform.localScale = new Vector3(0.35f, 0.25f, 1.8f);
            Object.DestroyImmediate(ridgeGo.GetComponent<Collider>());
            var ridgeRen = ridgeGo.GetComponent<MeshRenderer>();
            ridgeRen.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            ridgeRen.sharedMaterial.color = new Color(0.44f, 0.38f, 0.28f, 1.0f);
            ridgeRen.sortingOrder = sortOrder;
        }

        private static GameObject Spawn2DSprite(Sprite spr, string name, Transform parent, Vector3 pos, Vector2 scale, Color color, int sortOrder, Material mat, float swayAngle, float swaySpeed)
        {
            if (spr == null) return null;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(scale.x, scale.y, 1.0f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = spr;
            if (mat != null) sr.sharedMaterial = mat;
            sr.color = color;
            sr.sortingOrder = sortOrder;

            if (swayAngle > 0.05f)
            {
                var sway = go.AddComponent<PlanetOfLanaFoliageSway>();
                ConfigureSway(sway, swayAngle, swaySpeed, 0.15f);
            }
            return go;
        }

        private static GameObject SpawnPrefab(GameObject prefab, string name, Transform parent, Vector3 pos, float scale, bool flipX, Color color, int sortOrder, Material mat, float swayAngle, float swaySpeed)
        {
            if (prefab == null) return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            go.transform.position = pos;
            Vector3 s = go.transform.localScale * scale;
            if (flipX) s.x = -Mathf.Abs(s.x);
            go.transform.localScale = s;

            var srs = go.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in srs)
            {
                if (mat != null) sr.sharedMaterial = mat;
                sr.color = color;
                sr.sortingOrder = sortOrder;
            }

            var sways = go.GetComponentsInChildren<PlanetOfLanaFoliageSway>(true);
            foreach (var sway in sways)
            {
                ReinitializeSway(sway);
                if (swayAngle > 0.05f)
                {
                    ConfigureSway(sway, swayAngle, swaySpeed, 0.15f);
                }
            }

            if (sways.Length == 0 && swayAngle > 0.05f)
            {
                var sway = go.AddComponent<PlanetOfLanaFoliageSway>();
                ConfigureSway(sway, swayAngle, swaySpeed, 0.15f);
            }
            return go;
        }

        private static void ReinitializeSway(PlanetOfLanaFoliageSway sway)
        {
            if (sway == null) return;
            var t = typeof(PlanetOfLanaFoliageSway);
            var fPos = t.GetField("initialLocalPos", BindingFlags.NonPublic | BindingFlags.Instance);
            var fRot = t.GetField("initialLocalRot", BindingFlags.NonPublic | BindingFlags.Instance);
            var fScale = t.GetField("initialLocalScale", BindingFlags.NonPublic | BindingFlags.Instance);
            var fInit = t.GetField("isInitialized", BindingFlags.NonPublic | BindingFlags.Instance);

            if (fPos != null) fPos.SetValue(sway, sway.transform.localPosition);
            if (fRot != null) fRot.SetValue(sway, sway.transform.localRotation);
            if (fScale != null) fScale.SetValue(sway, sway.transform.localScale);
            if (fInit != null) fInit.SetValue(sway, true);
            sway.UpdatePhase();
        }

        private static void ConfigureSway(PlanetOfLanaFoliageSway sway, float angle, float speed, float phaseOffset)
        {
            if (sway == null) return;
            var so = new SerializedObject(sway);
            var pMaxAngle = so.FindProperty("maxRotationAngle");
            var pSpeed    = so.FindProperty("swaySpeed");
            var pPhase    = so.FindProperty("phaseOffset");
            if (pMaxAngle != null) pMaxAngle.floatValue = angle;
            if (pSpeed != null) pSpeed.floatValue = speed;
            if (pPhase != null) pPhase.floatValue = phaseOffset;
            so.ApplyModifiedProperties();
        }

        private static Material GetOrCreateSkyWaterMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/LanaOverhaul/M_Lana_PaddySkyMirror.mat");
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                mat = new Material(shader) { name = "M_Lana_PaddySkyMirror" };
                mat.color = new Color(0.40f, 0.62f, 0.78f, 0.88f);
                if (!Directory.Exists("Assets/Materials/LanaOverhaul"))
                    Directory.CreateDirectory("Assets/Materials/LanaOverhaul");
                AssetDatabase.CreateAsset(mat, "Assets/Materials/LanaOverhaul/M_Lana_PaddySkyMirror.mat");
            }
            else
            {
                mat.color = new Color(0.40f, 0.62f, 0.78f, 0.88f);
            }
            return mat;
        }

        private static Material GetCleanSpriteMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/LanaOverhaul/M_Lana_SpriteDefault.mat");
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default")
                          ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                          ?? Shader.Find("Sprites/Default");
                mat = new Material(shader) { name = "M_Lana_SpriteDefault" };
                if (!Directory.Exists("Assets/Materials/LanaOverhaul"))
                    Directory.CreateDirectory("Assets/Materials/LanaOverhaul");
                AssetDatabase.CreateAsset(mat, "Assets/Materials/LanaOverhaul/M_Lana_SpriteDefault.mat");
            }
            return mat;
        }
    }
}
#endif
