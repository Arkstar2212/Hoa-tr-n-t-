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
    /// File C# Editor Script duy nhất sửa triệt để 6 lỗi hiển thị khu vực Cánh đồng lúa & Làng quê:
    /// 1. Kéo dài Skybox Backdrop & Đáy đất đen (Black Ground Skirt) + Đường đi phủ dư ra thêm ít nhất 60m về bên phải.
    /// 2. Xóa vật thể lạ Y > 6.0m treo lơ lửng trên bầu trời & Xóa các khóm cỏ đơn lẻ trên mặt phẳng cắt màu đen.
    ///    Thay bằng dải cỏ & lúa tiền cảnh sát mép đường (Z = -0.5m .. -1.0m), nhuộm màu tối 35% che vết cắt sắc lẹm.
    /// 3. Lấp kín dải khe hở màu xanh nhạt: Thay toàn bộ quads tráng gương bằng mặt phẳng bùn đất nâu liên tục.
    /// 4. Phá bỏ bố cục 2 cây đối xứng gác cổng:
    ///    - Chòi sàn: 2-3 cây chuối gom lệch sang bên trái (3.8m & 2.5m), bên phải chỉ có cỏ thấp và đống củi/lu nước.
    ///    - Cây đa cổ thụ: 2 cây dừa gom lệch sang 1 phía với chiều cao khác nhau (7.5m nghiêng 8 độ, 5.6m nghiêng 14 độ), bọc bụi rậm che gốc.
    /// </summary>
    public static class FixRiceFieldComposition
    {
        public const string RootName = "[Additive_RiceField_100m]";
        private const string RiceTexDir = "Assets/Textures/Lana_Unified_Art/Rice";
        private const string GenMeshDir = "Assets/Materials/LanaOverhaul/GeneratedMeshes";
        private const string ClutterDir = "Assets/Prefabs/Clutter";
        private const string ArtifactDir = "C:/Users/Thanh Tài/.gemini/antigravity/brain/8f0de94b-4312-45db-8a61-1ed2ab228bb4";

        [MenuItem("Hoa va Mau/Level Design/Fix Rice Field Composition (1-Click All 6 Issues)", false, 0)]
        public static void FixAllIssuesAndCapture()
        {
            Debug.Log("<color=#3b82f6>[FixRiceField]</color> Bắt đầu xử lý triệt để 6 lỗi hiển thị...");

            // 1. MỞ RỘNG TẤM NỀN BẦU TRỜI (SKYBOX / BACKDROP)
            ExtendSkyboxBackdrop();

            // 2. DỰNG LẠI CÁNH ĐỒNG LÚA MỞ RỘNG (165m) VỚI CÁC SỬA ĐỔI TOÀN DIỆN
            BuildRefinedRiceField(138.0f, 165.0f, -1.85f);

            // 3. LƯU SCENE
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            // 4. CHỤP LẠI 4 ẢNH VERIFICATION 720P JPG
            CaptureVerificationViews();

            Debug.Log("<color=#10b981>[FixRiceField]</color> HOÀN THÀNH TOÀN BỘ 6 MỤC SỬA LỖI & ĐÃ XUẤT 4 ẢNH PREVIEW 720P!");
        }

        // =========================================================================
        // MỤC 1: KÉO DÀI TẤM NỀN BẦU TRỜI (SKYBOX / BACKDROP)
        // =========================================================================
        private static void ExtendSkyboxBackdrop()
        {
            var bg = GameObject.Find("Grand_Unified_PlanetOfLana_Background");
            if (bg != null)
            {
                Undo.RecordObject(bg.transform, "Extend Skybox Backdrop");
                // Mở rộng Scale X lên 440m và dịch tâm về X = 150m
                // Bao phủ hoàn hảo từ X = -70m đến tận X = +370m (vượt xa bến tàu)
                bg.transform.localPosition = new Vector3(150.0f, 22.0f, 18.0f);
                bg.transform.localScale = new Vector3(440.0f, 56.0f, 1.0f);
                Debug.Log("<color=#10b981>[FixRiceField]</color> Đã mở rộng Skybox Backdrop phủ kín từ X=-70m đến X=+370m!");
            }
            else
            {
                Debug.LogWarning("[FixRiceField] Không tìm thấy Grand_Unified_PlanetOfLana_Background!");
            }
        }

        // =========================================================================
        // MỤC 2, 3, 4: DỰNG CÁNH ĐỒNG LÚA MỞ RỘNG VỚI NỀN ĐẤT BÙN VÀ BỐ CỤC BẤT ĐỐI XỨNG
        // =========================================================================
        private static void BuildRefinedRiceField(float startX, float lengthMeters, float pathY = -1.85f)
        {
            float endX = startX + lengthMeters;

            GameObject root = GameObject.Find(RootName);
            if (root != null)
            {
                Undo.DestroyObjectImmediate(root);
            }
            root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Rebuild Rice Field with Master Composition Fixes");

            // Materials
            Material spriteMat = GetCleanSpriteMaterial();
            Material grassCapMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/LanaOverhaul/M_Lana_GrassCap.mat");
            Material rockUnderMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/LanaOverhaul/M_Lana_RockUnderbelly.mat");
            Material brownMudMat = GetOrCreateMudGroundMaterial();

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

            // Modular Clutter Prefabs
            EnsureModularClutterPrefabs(spriteMat);
            GameObject pfClutterCottage   = AssetDatabase.LoadAssetAtPath<GameObject>($"{ClutterDir}/PF_Clutter_ChanCot_NhaTranh.prefab");
            GameObject pfClutterCoconut   = AssetDatabase.LoadAssetAtPath<GameObject>($"{ClutterDir}/PF_Clutter_BoDe_GocDua.prefab");
            GameObject pfClutterRuralPath = AssetDatabase.LoadAssetAtPath<GameObject>($"{ClutterDir}/PF_Clutter_DauVet_NongThon.prefab");

            // =========================================================================
            // MỤC 3: MẶT NỀN ĐẤT BÙN NÂU LIÊN TỤC (XUYÊN SUỐT TOÀN BỘ CÁNH ĐỒNG)
            // Lấp kín 100% khe hở màu xanh nhạt. Gốc lúa cắm vững chắc vào đất bùn màu nâu!
            // =========================================================================
            var dioramaRoot = new GameObject("00_Continuous_BrownMud_DioramaFloor");
            dioramaRoot.transform.SetParent(root.transform, false);

            CreateContinuousMudFloor(
                dioramaRoot.transform, "RiceField165m_ContinuousBrownMud",
                startX - 6.0f, endX + 15.0f, pathY, brownMudMat);

            // =========================================================================
            // MỤC 1: ĐƯỜNG ĐI CHÍNH & KHỐI ĐÁY ĐEN (BLACK GROUND SKIRT) MỞ RỘNG THÊM 65M
            // Kéo dài từ X = 138m đến X = 303m, phủ sâu xuống bottomY = -14.0m
            // =========================================================================
            var pathRoot = new GameObject("01_Gameplay_Pathway_Extended");
            pathRoot.transform.SetParent(root.transform, false);

            CreateLanaLedgeSection(
                pathRoot.transform, "Ledge_06_VastRicePaddyField_Extended",
                startX, endX, 420, 0.05f, 1.10f, -14.0f,
                x => pathY, grassCapMat, rockUnderMat);

            var colGo = new GameObject("Col_10_VastRicePaddyPlatform_Extended");
            colGo.transform.SetParent(pathRoot.transform, false);
            colGo.transform.position = new Vector3(startX + lengthMeters * 0.5f, -6.5f, 0f);
            var col3D = colGo.AddComponent<BoxCollider>();
            col3D.size = new Vector3(lengthMeters + 4f, 9.5f, 4.0f);

            var endWall = new GameObject("Pathway_3DEndWall_303m");
            endWall.transform.SetParent(pathRoot.transform, false);
            endWall.transform.position = new Vector3(endX + 0.5f, pathY + 3.0f, 0f);
            var endCol3D = endWall.AddComponent<BoxCollider>();
            endCol3D.size = new Vector3(1.0f, 8.0f, 4.0f);

            // Điểm xuyết mặt đường với thảm cỏ và phiến đá bước tự nhiên
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

            // Dấu vết lao động nông thôn ven đường
            if (pfClutterRuralPath != null)
            {
                SpawnPrefab(pfClutterRuralPath, "Clutter_Story_Waypoint_146m", pathRoot.transform,
                    new Vector3(startX + 8.0f, pathY, 0.60f), 0.85f, false, Color.white, 14, spriteMat, 0f, 0f);

                SpawnPrefab(pfClutterRuralPath, "Clutter_Story_Waypoint_188m", pathRoot.transform,
                    new Vector3(startX + 50.0f, pathY, 0.65f), 0.85f, true, Color.white, 14, spriteMat, 0f, 0f);

                SpawnPrefab(pfClutterRuralPath, "Clutter_Story_Waypoint_218m", pathRoot.transform,
                    new Vector3(startX + 80.0f, pathY, 0.60f), 0.85f, false, Color.white, 14, spriteMat, 0f, 0f);

                SpawnPrefab(pfClutterRuralPath, "Clutter_Story_Waypoint_256m", pathRoot.transform,
                    new Vector3(startX + 118.0f, pathY, 0.60f), 0.85f, true, Color.white, 14, spriteMat, 0f, 0f);
            }

            // =========================================================================
            // MỤC 2: TIỀN CẢNH SÁT MÉP ĐƯỜNG CHE ĐƯỜNG KẺ SẮC LẸM
            // - Xóa sạch các khóm cỏ gắn trên nền đen.
            // - Đặt cỏ & lúa sát mép đường (Z = -0.5m .. -1.0m), nhuộm màu tối 35% Color(0.6f, 0.62f, 0.5f).
            // - TUYỆT ĐỐI KHÔNG CÓ BẤT KỲ VẬT THỂ NÀO TREO Ở Y > 6.0M!
            // =========================================================================
            var fgRoot = new GameObject("02_Foreground_RoadLip_Cover");
            fgRoot.transform.SetParent(root.transform, false);

            Color fgCoverColor = new Color(0.60f, 0.62f, 0.50f, 1.0f); // Tối 35% theo đúng yêu cầu
            for (float fx = startX + 2f; fx <= endX - 2f; fx += 2.2f)
            {
                float jx = fx + Mathf.Sin(fx * 3.1f) * 0.35f;
                float fz = -0.65f + Mathf.Cos(fx * 1.9f) * 0.25f; // Sát mép đường Z = -0.5m .. -0.9m
                float fy = pathY - 0.02f; // Nằm ngay mép tiếp giáp giữa mặt đường vàng và đáy đen

                Sprite lipSpr = (fx % 4.4f < 2.2f) ? (sprClump ?? sprMedium) : (sprPanicle ?? sprTallSway);
                if (lipSpr != null)
                {
                    bool flip = (fx % 3.0f > 1.5f);
                    Spawn2DSprite(lipSpr, $"RoadLip_Cover_{jx:F0}", fgRoot.transform,
                        new Vector3(jx, fy, fz), new Vector2(flip ? -0.40f : 0.40f, 0.40f),
                        fgCoverColor, 22, spriteMat, 1.2f, 1.0f);
                }
            }

            // =========================================================================
            // MỤC 3: DẢI RUỘNG LÚA VÀNG BẬC THANG CẮM TRỰC TIẾP TRÊN ĐẤT BÙN
            // (Không còn bất kỳ quad màu xanh nhạt cắt ngang!)
            // =========================================================================
            var ricePlotsRoot = new GameObject("03_TerracedRicePlots_OnMud");
            ricePlotsRoot.transform.SetParent(root.transform, false);

            // Dải ruộng cận (Tier 1: Z = 1.8m)
            for (float rx = startX + 2f; rx <= endX - 2f; rx += 1.9f)
            {
                float jx = rx + Mathf.Sin(rx * 1.8f) * 0.3f;
                float jz = 1.8f + Mathf.Cos(rx * 1.3f) * 0.25f;
                float jy = GetMudFloorY(pathY, jz);
                Color t1Color = GetAtmosphericTint(jz, new Color(1.00f, 0.94f, 0.58f, 1.0f));

                Sprite rSpr = (rx % 3.8f < 1.9f) ? (sprWideRow ?? sprTallSway) : (sprClump ?? sprDenseHead);
                if (rSpr != null)
                {
                    bool flip = (rx % 4.0f > 2.0f);
                    Spawn2DSprite(rSpr, $"Tier1_Rice_{jx:F0}", ricePlotsRoot.transform,
                        new Vector3(jx, jy + 0.05f, jz), new Vector2(flip ? -0.44f : 0.44f, 0.44f),
                        t1Color, 10, spriteMat, 1.6f, 1.2f);
                }
            }

            // Dải ruộng trung (Tier 2: Z = 3.6m)
            for (float rx = startX + 1f; rx <= endX - 1f; rx += 2.4f)
            {
                float jx = rx + Mathf.Sin(rx * 1.5f) * 0.35f;
                float jz = 3.6f + Mathf.Cos(rx * 1.1f) * 0.30f;
                float jy = GetMudFloorY(pathY, jz);
                Color t2Color = GetAtmosphericTint(jz, new Color(0.94f, 0.90f, 0.60f, 1.0f));

                Sprite rSpr = (rx % 4.8f < 2.4f) ? (sprWideRow ?? sprMedium) : (sprPanicle ?? sprCluster);
                if (rSpr != null)
                {
                    bool flip = (rx % 5.0f > 2.5f);
                    Spawn2DSprite(rSpr, $"Tier2_Rice_{jx:F0}", ricePlotsRoot.transform,
                        new Vector3(jx, jy + 0.05f, jz), new Vector2(flip ? -0.42f : 0.42f, 0.42f),
                        t2Color, 8, spriteMat, 1.4f, 1.1f);
                }
            }

            // =========================================================================
            // MỤC 4: BỐ CỤC BẤT ĐỐI XỨNG & CỤM QUẦN TỤ CHUẨN
            // =========================================================================
            var clusterRoot = new GameObject("04_StoryClusters_Asymmetric");
            clusterRoot.transform.SetParent(root.transform, false);

            // -------------------------------------------------------------------------
            // A. CHÒI SÀN NHỎ (X = 138m - 148m):
            // - 2-3 cây chuối gom lệch hẳn sang BÊN TRÁI chòi (3.8m & 2.5m).
            // - BÊN PHẢI chòi: TUYỆT ĐỐI KHÔNG CÓ CÂY CHUỐI NÀO, chỉ có cỏ thấp và đống củi/lu nước.
            // -------------------------------------------------------------------------
            var z1Root = new GameObject("Zone1_Shelter_LeftBananas_RightClutter");
            z1Root.transform.SetParent(clusterRoot.transform, false);

            float shelterX = startX + 4.8f; // ~142.8m
            float shelterZ = 2.4f;
            float shelterGroundY = GetMudFloorY(pathY, shelterZ);

            if (pfWaitingShelter != null)
            {
                Color shelterColor = GetAtmosphericTint(shelterZ, new Color(0.95f, 0.92f, 0.85f, 1f));
                SpawnNormalizedPrefab(pfWaitingShelter, "Z1_WoodenWaitingShelter", z1Root.transform,
                    new Vector3(shelterX, shelterGroundY, shelterZ), 3.2f, false,
                    shelterColor, 15, spriteMat, 0f, 0f);

                if (pfClutterCottage != null)
                {
                    SpawnNormalizedPrefab(pfClutterCottage, "Z1_Shelter_FootClutter", z1Root.transform,
                        new Vector3(shelterX, shelterGroundY, shelterZ - 0.25f), 0.75f, false,
                        Color.white, 16, spriteMat, 0f, 0f);
                }
            }

            // GOM 3 CÂY CHUỐI LỆCH HẲN SANG BÊN TRÁI CHÒI (X < 142.8m)
            if (pfBanana != null)
            {
                Color bananaColor = GetAtmosphericTint(2.8f, new Color(0.55f, 0.68f, 0.45f, 1f));
                // Cây chuối 1: Cao 3.8m lệch sang trái
                SpawnNormalizedPrefab(pfBanana, "Z1_Banana_Left_Tall_3.8m", z1Root.transform,
                    new Vector3(shelterX - 3.2f, GetMudFloorY(pathY, 2.9f), 2.9f), 3.8f, false,
                    bananaColor, 14, spriteMat, 1.2f, 1.0f);

                // Cây chuối 2: Thấp 2.5m nép cạnh cây 1
                SpawnNormalizedPrefab(pfBanana, "Z1_Banana_Left_Low_2.5m", z1Root.transform,
                    new Vector3(shelterX - 1.8f, GetMudFloorY(pathY, 2.6f), 2.6f), 2.5f, true,
                    bananaColor, 14, spriteMat, 1.4f, 1.1f);

                // Cây chuối 3 nhỏ: 1.9m bọc sát mép ngoài
                SpawnNormalizedPrefab(pfBanana, "Z1_Banana_Left_Small_1.9m", z1Root.transform,
                    new Vector3(shelterX - 4.4f, GetMudFloorY(pathY, 3.1f), 3.1f), 1.9f, false,
                    bananaColor, 13, spriteMat, 1.0f, 0.9f);
            }

            // BÊN PHẢI CHÒI: CHỈ ĐẶT BỤI CỎ THẤP VÀ ĐỐNG CỦI / LU NƯỚC / THÚNG TRE
            if (pfClutterRuralPath != null)
            {
                SpawnPrefab(pfClutterRuralPath, "Z1_Right_Firewood_And_Jars", z1Root.transform,
                    new Vector3(shelterX + 2.8f, pathY, 1.2f), 0.80f, false, Color.white, 14, spriteMat, 0f, 0f);
            }
            if (pfReeds != null)
            {
                Color reedColor = GetAtmosphericTint(1.4f, new Color(0.85f, 0.82f, 0.65f, 1f));
                SpawnPrefab(pfReeds, "Z1_Right_LowGrass_Reeds", z1Root.transform,
                    new Vector3(shelterX + 4.2f, pathY + 0.10f, 1.4f), 0.38f, false,
                    reedColor, 14, spriteMat, 1.5f, 1.2f);
            }

            // -------------------------------------------------------------------------
            // B. KHOẢNG THỞ 1 (X = 148m - 164m): RUỘNG LÚA MÊNH MÔNG
            // -------------------------------------------------------------------------
            var z2Root = new GameObject("Zone2_OpenScenicDike");
            z2Root.transform.SetParent(clusterRoot.transform, false);

            for (float ox = startX + 12f; ox <= startX + 24f; ox += 3.5f)
            {
                float jx = ox + Mathf.Sin(ox * 1.7f) * 0.4f;
                float oz = 5.2f + Mathf.Cos(ox * 1.2f) * 0.6f;
                Color farRiceColor = GetAtmosphericTint(oz, new Color(0.92f, 0.88f, 0.62f, 1f));
                Spawn2DSprite(sprWideRow ?? sprMedium, $"Z2_FarScenicRice_{jx:F0}", z2Root.transform,
                    new Vector3(jx, GetMudFloorY(pathY, oz) + 0.12f, oz), new Vector2(0.45f, 0.45f),
                    farRiceColor, 7, spriteMat, 1.2f, 1.0f);
            }

            // -------------------------------------------------------------------------
            // C. CỤM NHÀ TRANH 1 (X = 164m - 180m): NHÀ 3.8M + TRE + DỪA + CHUỐI + GIÀN BẦU
            // -------------------------------------------------------------------------
            var z3Root = new GameObject("Zone3_CottageCluster1");
            z3Root.transform.SetParent(clusterRoot.transform, false);

            float hut1X = startX + 30.0f; // 168.0m
            float hut1Z = 5.2f;
            float hut1GroundY = GetMudFloorY(pathY, hut1Z);

            if (pfThatchedHut != null)
            {
                Color hut1Color = GetAtmosphericTint(hut1Z, new Color(0.94f, 0.90f, 0.84f, 1f));
                SpawnNormalizedPrefab(pfThatchedHut, "Z3_Cottage_VillageThatched_01", z3Root.transform,
                    new Vector3(hut1X, hut1GroundY, hut1Z), 3.8f, false,
                    hut1Color, 7, spriteMat, 0f, 0f);

                if (pfClutterCottage != null)
                {
                    SpawnNormalizedPrefab(pfClutterCottage, "Z3_Hut1_FrontPorch_Clutter", z3Root.transform,
                        new Vector3(hut1X - 0.5f, GetMudFloorY(pathY, 4.6f), 4.6f), 0.85f, false,
                        Color.white, 9, spriteMat, 0f, 0f);
                }

                if (pfTrellis != null)
                {
                    Color trellisColor = GetAtmosphericTint(4.8f, new Color(0.92f, 0.94f, 0.88f, 1f));
                    SpawnNormalizedPrefab(pfTrellis, "Z3_Hut1_GourdTrellis", z3Root.transform,
                        new Vector3(hut1X + 4.8f, GetMudFloorY(pathY, 4.8f), 4.8f), 2.2f, false,
                        trellisColor, 8, spriteMat, 0.6f, 0.6f);
                }

                if (pfCoconut != null)
                {
                    Color cocoColor = GetAtmosphericTint(6.2f, new Color(0.60f, 0.70f, 0.55f, 1f));
                    SpawnNormalizedPrefab(pfCoconut, "Z3_Hut1_Coconut_Tilting", z3Root.transform,
                        new Vector3(hut1X - 2.8f, GetMudFloorY(pathY, 6.2f), 6.2f), 7.2f, false,
                        cocoColor, 6, spriteMat, 1.1f, 0.9f);

                    if (pfClutterCoconut != null)
                    {
                        SpawnNormalizedPrefab(pfClutterCoconut, "Z3_Hut1_Coconut_BaseClutter", z3Root.transform,
                            new Vector3(hut1X - 2.8f, GetMudFloorY(pathY, 6.2f), 6.0f), 0.75f, false,
                            Color.white, 7, spriteMat, 0f, 0f);
                    }
                }

                if (pfBanana != null)
                {
                    Color bananaColor = GetAtmosphericTint(5.8f, new Color(0.55f, 0.65f, 0.50f, 1f));
                    SpawnNormalizedPrefab(pfBanana, "Z3_Hut1_Banana_Back", z3Root.transform,
                        new Vector3(hut1X + 3.2f, GetMudFloorY(pathY, 5.8f), 5.8f), 2.6f, true,
                        bananaColor, 6, spriteMat, 1.3f, 1.0f);
                }

                if (pfBamboo != null)
                {
                    Color bambooColor = GetAtmosphericTint(7.5f, new Color(0.50f, 0.64f, 0.46f, 1f));
                    SpawnNormalizedPrefab(pfBamboo, "Z3_Hut1_BackBamboo", z3Root.transform,
                        new Vector3(hut1X - 4.5f, GetMudFloorY(pathY, 7.5f), 7.5f), 6.5f, false,
                        bambooColor, 5, spriteMat, 1.1f, 0.9f);
                }
            }

            // -------------------------------------------------------------------------
            // D. VÙNG LÚA CAO TRÀN ĐƯỜNG (CROUCH / STEALTH POCKET - X = 180m - 192m)
            // -------------------------------------------------------------------------
            var z4Root = new GameObject("Zone4_Gameplay_CrouchPocket");
            z4Root.transform.SetParent(clusterRoot.transform, false);

            for (float cx = startX + 42f; cx <= startX + 54f; cx += 1.6f)
            {
                float jx = cx + Mathf.Sin(cx * 2.8f) * 0.25f;
                Color fgRiceCol = GetAtmosphericTint(-0.35f, new Color(1.0f, 0.95f, 0.65f, 1f));
                Spawn2DSprite(sprTallSway ?? sprPanicle, $"CrouchPocket_FrontRice_{jx:F0}", z4Root.transform,
                    new Vector3(jx, pathY - 0.05f, -0.35f), new Vector2((cx % 3f > 1.5f) ? -0.52f : 0.52f, 0.52f),
                    fgRiceCol, 18, spriteMat, 1.8f, 1.3f);

                Color bgRiceCol = GetAtmosphericTint(0.55f, new Color(0.98f, 0.92f, 0.58f, 1f));
                Spawn2DSprite(sprTallSway ?? sprClump, $"CrouchPocket_BackRice_{jx:F0}", z4Root.transform,
                    new Vector3(jx + 0.35f, pathY + 0.02f, 0.55f), new Vector2((cx % 4f > 2f) ? -0.48f : 0.48f, 0.48f),
                    bgRiceCol, 12, spriteMat, 1.7f, 1.2f);
            }

            // -------------------------------------------------------------------------
            // E. CỤM NHÀ TRANH 2 (X = 192m - 208m)
            // -------------------------------------------------------------------------
            var z5Root = new GameObject("Zone5_CottageCluster2");
            z5Root.transform.SetParent(clusterRoot.transform, false);

            float hut2X = startX + 64.0f; // 202.0m
            float hut2Z = 5.6f;
            float hut2GroundY = GetMudFloorY(pathY, hut2Z);

            if (pfThatchedHut != null)
            {
                Color hut2Color = GetAtmosphericTint(hut2Z, new Color(0.92f, 0.88f, 0.82f, 1f));
                SpawnNormalizedPrefab(pfThatchedHut, "Z5_Cottage_VillageThatched_02", z5Root.transform,
                    new Vector3(hut2X, hut2GroundY, hut2Z), 3.8f, false,
                    hut2Color, 7, spriteMat, 0f, 0f);

                if (pfClutterCottage != null)
                {
                    SpawnNormalizedPrefab(pfClutterCottage, "Z5_Hut2_FrontPorch_Clutter", z5Root.transform,
                        new Vector3(hut2X - 0.5f, GetMudFloorY(pathY, 5.0f), 5.0f), 0.85f, false,
                        Color.white, 9, spriteMat, 0f, 0f);
                }

                if (pfBamboo != null)
                {
                    Color bambooColor = GetAtmosphericTint(6.4f, new Color(0.52f, 0.66f, 0.48f, 1f));
                    SpawnNormalizedPrefab(pfBamboo, "Z5_Hut2_BambooGrove", z5Root.transform,
                        new Vector3(hut2X + 4.5f, GetMudFloorY(pathY, 6.4f), 6.4f), 6.2f, false,
                        bambooColor, 6, spriteMat, 1.2f, 1.0f);
                }

                if (pfMedCanopy != null)
                {
                    Color shadeTreeColor = GetAtmosphericTint(6.5f, new Color(0.58f, 0.68f, 0.52f, 1f));
                    SpawnNormalizedPrefab(pfMedCanopy, "Z5_Hut2_ShadeTree", z5Root.transform,
                        new Vector3(hut2X - 4.2f, GetMudFloorY(pathY, 6.5f), 6.5f), 4.5f, false,
                        shadeTreeColor, 6, spriteMat, 0.9f, 0.8f);
                }

                if (pfCoconut != null)
                {
                    Color coco2Color = GetAtmosphericTint(6.8f, new Color(0.56f, 0.66f, 0.50f, 1f));
                    SpawnNormalizedPrefab(pfCoconut, "Z5_Hut2_Coconut_Tilting", z5Root.transform,
                        new Vector3(hut2X - 2.0f, GetMudFloorY(pathY, 6.8f), 6.8f), 7.2f, true,
                        coco2Color, 5, spriteMat, 1.1f, 0.9f);

                    if (pfClutterCoconut != null)
                    {
                        SpawnNormalizedPrefab(pfClutterCoconut, "Z5_Hut2_Coconut_BaseClutter", z5Root.transform,
                            new Vector3(hut2X - 2.0f, GetMudFloorY(pathY, 6.8f), 6.6f), 0.75f, true,
                            Color.white, 7, spriteMat, 0f, 0f);
                    }
                }
            }

            // -------------------------------------------------------------------------
            // F. CÂY ĐA CỔ THỤ LỚN (X = 224m - 238m):
            // - BỎ BỐ CỤC ĐỐI XỨNG CỔNG CHÀO!
            // - Gom 2 cây dừa LỆCH HẲN SANG BÊN TRÁI:
            //   + Cây dừa 1 cao 7.5m, nghiêng 8 độ.
            //   + Cây dừa 2 thấp 5.6m, nghiêng 14 độ.
            //   + Lót bụi rậm cỏ dày che kín hoàn toàn gốc rễ.
            // - BÊN PHẢI CÂY ĐA: Phiến đá dừng chân phẳng + hoa dại, mở thoáng tầm nhìn ra bến tàu!
            // -------------------------------------------------------------------------
            var z7Root = new GameObject("Zone7_GrandBanyan_AsymmetricCoconuts");
            z7Root.transform.SetParent(clusterRoot.transform, false);

            float banyanX = startX + 94.0f; // 232.0m
            float banyanZ = 4.2f;
            float banyanGroundY = GetMudFloorY(pathY, banyanZ);

            if (pfBigCanopy != null)
            {
                Color banyanColor = GetAtmosphericTint(banyanZ, new Color(0.58f, 0.68f, 0.52f, 1f));
                SpawnNormalizedPrefab(pfBigCanopy, "Z7_EndField_AncientBanyan", z7Root.transform,
                    new Vector3(banyanX, banyanGroundY, banyanZ), 9.8f, false,
                    banyanColor, 7, spriteMat, 0.7f, 0.7f);

                if (pfClutterCoconut != null)
                {
                    SpawnNormalizedPrefab(pfClutterCoconut, "Z7_EndField_Banyan_BaseClutter", z7Root.transform,
                        new Vector3(banyanX, banyanGroundY, banyanZ - 0.25f), 1.0f, false,
                        Color.white, 8, spriteMat, 0f, 0f);
                }
            }

            // GOM 2 CÂY DỪA LỆCH HẲN SANG BÊN TRÁI CÂY ĐA (X < 232.0m)
            if (pfCoconut != null)
            {
                // Cây dừa 1: Cao 7.5m nghiêng 8 độ
                Color coco1Color = GetAtmosphericTint(5.0f, new Color(0.56f, 0.66f, 0.49f, 1f));
                var coco1 = SpawnNormalizedPrefab(pfCoconut, "Z7_Coconut_Left_Tall_7.5m_Tilt8Deg", z7Root.transform,
                    new Vector3(banyanX - 5.5f, GetMudFloorY(pathY, 5.0f), 5.0f), 7.5f, false,
                    coco1Color, 6, spriteMat, 1.1f, 0.9f);
                if (coco1 != null) coco1.transform.rotation = Quaternion.Euler(0f, 0f, 8.0f);

                // Cây dừa 2: Thấp 5.6m nghiêng 14 độ
                Color coco2Color = GetAtmosphericTint(4.6f, new Color(0.50f, 0.60f, 0.45f, 1f));
                var coco2 = SpawnNormalizedPrefab(pfCoconut, "Z7_Coconut_Left_Low_5.6m_Tilt14Deg", z7Root.transform,
                    new Vector3(banyanX - 3.2f, GetMudFloorY(pathY, 4.6f), 4.6f), 5.6f, true,
                    coco2Color, 5, spriteMat, 1.0f, 0.8f);
                if (coco2 != null) coco2.transform.rotation = Quaternion.Euler(0f, 0f, 14.0f);

                // Lót bụi rậm cỏ dày che kín gốc cả 2 cây dừa
                if (pfClutterCoconut != null)
                {
                    SpawnNormalizedPrefab(pfClutterCoconut, "Z7_CoconutTwin_DenseBushBase", z7Root.transform,
                        new Vector3(banyanX - 4.4f, GetMudFloorY(pathY, 4.8f), 4.5f), 1.15f, false,
                        Color.white, 8, spriteMat, 0f, 0f);
                }
            }

            // BÊN PHẢI CÂY ĐA: PHIẾN ĐÁ DỪNG CHÂN VÀ THẢM HOA VÀNG MỞ RA BẾN TÀU
            if (pfRockSlab != null)
            {
                Color rockCol = GetAtmosphericTint(2.5f, new Color(0.68f, 0.65f, 0.58f, 1f));
                SpawnPrefab(pfRockSlab, "Z7_Right_RestingStone_ToDock", z7Root.transform,
                    new Vector3(banyanX + 3.8f, pathY - 0.02f, 2.5f), 0.50f, false,
                    rockCol, 12, spriteMat, 0f, 0f);
            }
        }

        // =========================================================================
        // CAPTURE 4 ẢNH VERIFICATION 720P JPG THEO ĐÚNG TIÊU CHUẨN
        // =========================================================================
        public static void CaptureVerificationViews()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                var cams = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include);
                if (cams.Length > 0) mainCam = cams[0];
            }

            if (mainCam == null)
            {
                Debug.LogError("[FixRiceField] Không tìm thấy Camera để chụp ảnh!");
                return;
            }

            int width = 1280;
            int height = 720;

            Vector3 originalPos = mainCam.transform.position;
            Quaternion originalRot = mainCam.transform.rotation;

            RenderTexture rt = new RenderTexture(width, height, 24);
            RenderTexture prevRT = RenderTexture.active;
            mainCam.targetTexture = rt;

            float[] targetXs = new float[] { 148.0f, 172.0f, 196.0f, 232.0f };
            string[] fileNames = new string[] {
                "fix_ricefield_view1_shelter_left_bananas.jpg",
                "fix_ricefield_view2_cottage1_trellis.jpg",
                "fix_ricefield_view3_crouch_cottage2_extended.jpg",
                "fix_ricefield_view4_banyan_asymmetric_coconuts.jpg"
            };

            for (int i = 0; i < targetXs.Length; i++)
            {
                mainCam.transform.position = new Vector3(targetXs[i], 0.35f, -15.5f);
                mainCam.transform.rotation = Quaternion.identity;
                mainCam.Render();

                RenderTexture.active = rt;
                Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();

                byte[] bytes = tex.EncodeToJPG(80);
                string outPath = Path.Combine(ArtifactDir, fileNames[i]);
                File.WriteAllBytes(outPath, bytes);
                Object.DestroyImmediate(tex);
                Debug.Log($"<color=#10b981>[FixRiceField]</color> Đã xuất ảnh kiểm tra: {fileNames[i]}");
            }

            mainCam.targetTexture = null;
            RenderTexture.active = prevRT;
            rt.Release();
            Object.DestroyImmediate(rt);

            mainCam.transform.position = originalPos;
            mainCam.transform.rotation = originalRot;
        }

        // =========================================================================
        // HELPER FUNCTIONS
        // =========================================================================
        public static float GetMudFloorY(float pathY, float z)
        {
            float slopeAngleDeg = 0.50f;
            float tanSlope = Mathf.Tan(slopeAngleDeg * Mathf.Deg2Rad);
            return pathY + (z - 0.45f) * tanSlope - 0.08f;
        }

        public static Color GetAtmosphericTint(float z, Color baseColor)
        {
            if (z <= -1.0f)
            {
                Color fgDark = new Color(0.38f, 0.42f, 0.38f, 1.0f);
                return Color.Lerp(baseColor, fgDark, 0.38f);
            }
            else if (z <= 1.0f)
            {
                return baseColor;
            }
            else
            {
                Color riverFogTint = new Color(0.58f, 0.70f, 0.82f, 1.0f);
                float t = Mathf.Clamp01((z - 1.0f) / 14.0f) * 0.35f;
                return Color.Lerp(baseColor, riverFogTint, t);
            }
        }

        private static GameObject SpawnNormalizedPrefab(
            GameObject prefab, string name, Transform parent,
            Vector3 targetPos, float targetHeightMeters,
            bool flipX, Color tintColor, int sortOrder, Material mat,
            float swayAngle = 0f, float swaySpeed = 0f)
        {
            if (prefab == null) return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;

            var renderers = go.GetComponentsInChildren<Renderer>(true);
            float currentHeight = 1.0f;
            if (renderers.Length > 0)
            {
                Bounds b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    b.Encapsulate(renderers[i].bounds);
                currentHeight = Mathf.Max(b.size.y, 0.1f);
            }

            float scaleRatio = targetHeightMeters / currentHeight;
            Vector3 s = go.transform.localScale * scaleRatio;
            if (flipX) s.x = -Mathf.Abs(s.x);
            go.transform.localScale = s;

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

            var srs = go.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in srs)
            {
                if (mat != null) sr.sharedMaterial = mat;
                sr.color = tintColor;
                sr.sortingOrder = sortOrder;
            }

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

        private static void CreateContinuousMudFloor(
            Transform parent, string name, float startX, float endX, float pathY, Material floorMat)
        {
            var floorGO = new GameObject(name);
            floorGO.transform.SetParent(parent, false);

            if (!Directory.Exists(GenMeshDir)) Directory.CreateDirectory(GenMeshDir);

            Mesh dioramaMesh = BuildMudFloorMesh($"{name}_Mesh", startX, endX, pathY);
            AssetDatabase.CreateAsset(dioramaMesh, $"{GenMeshDir}/{name}_Mesh.asset");

            var mf = floorGO.AddComponent<MeshFilter>();
            mf.sharedMesh = dioramaMesh;

            var mr = floorGO.AddComponent<MeshRenderer>();
            mr.sharedMaterial = floorMat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        private static Mesh BuildMudFloorMesh(string meshName, float startX, float endX, float pathY)
        {
            int cols = 150;
            float[] depthZs = new float[] { 0.45f, 1.8f, 3.6f, 5.5f, 7.5f, 9.5f, 12.0f };
            int rows = depthZs.Length;

            var verts = new List<Vector3>();
            var colors = new List<Color>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            for (int r = 0; r < rows; r++)
            {
                float z = depthZs[r];
                float y = GetMudFloorY(pathY, z);
                float v = (float)r / (rows - 1);

                for (int c = 0; c <= cols; c++)
                {
                    float u = (float)c / cols;
                    float x = Mathf.Lerp(startX, endX, u);

                    float undulation = Mathf.Sin(x * 0.45f + r * 1.2f) * 0.04f + Mathf.Cos(x * 1.1f) * 0.02f;
                    verts.Add(new Vector3(x, y + undulation, z));

                    // Màu đất bùn màu nước hữu cơ
                    Color baseGroundColor = new Color(0.44f, 0.40f, 0.30f, 1.0f);
                    Color vertexCol = GetAtmosphericTint(z, baseGroundColor);
                    colors.Add(vertexCol);

                    uvs.Add(new Vector2(u * 22f, v * 3.5f));
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
                    uvs.Add(new Vector2(u * 18f, v * 2f));
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
                    uvs.Add(new Vector2(u * 18f, (float)p / (profileCount - 1)));
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

        private static void EnsureModularClutterPrefabs(Material spriteMat)
        {
            if (!Directory.Exists(ClutterDir)) Directory.CreateDirectory(ClutterDir);

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

        private static Material GetOrCreateMudGroundMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/LanaOverhaul/M_Lana_ContinuousMudGround.mat");
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                mat = new Material(shader) { name = "M_Lana_ContinuousMudGround" };
                mat.color = new Color(0.44f, 0.38f, 0.28f, 1.0f);
                if (!Directory.Exists("Assets/Materials/LanaOverhaul"))
                    Directory.CreateDirectory("Assets/Materials/LanaOverhaul");
                AssetDatabase.CreateAsset(mat, "Assets/Materials/LanaOverhaul/M_Lana_ContinuousMudGround.mat");
            }
            else
            {
                mat.color = new Color(0.44f, 0.38f, 0.28f, 1.0f);
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
