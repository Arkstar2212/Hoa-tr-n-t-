#pragma warning disable 0219
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
    /// File C# Editor Script duy nhất nâng cấp chiều sâu không gian 2.5D Cánh đồng lúa & Làng quê:
    /// 1. Hạ thấp Skybox Backdrop và hiển thị Dãy núi xanh / Chân trời xa khớp sau lưng nhà tranh.
    ///    Bổ sung rặng cây bụi / rặng tre nhỏ mờ ở cự ly xa (Z = +19m .. +25m, scale 0.35x - 0.5x, màu sương mù).
    /// 2. Mở rộng Cánh đồng lúa Trung cảnh (Z = +2m .. +14m) thành 3 bậc ruộng vàng đan xen (Tier 1 Z=3.5m, Tier 2 Z=7.0m, Tier 3 Z=11.0m)
    ///    với độ cao Y tăng dần (+0.15m mỗi lớp), tạo thành biển lúa vàng ngút ngàn.
    /// 3. Lớp Lúa Tiền Cảnh sát camera (Z = -2.2m .. -2.8m, scale 1.7x - 2.1x, Y thấp, màu sẫm silhouette)
    ///    che mảng đen dưới đáy màn hình, giảm diện tích đen từ 30% xuống còn 12-15%.
    /// 4. Xóa sạch nhịp lặp "mã vạch" trên mặt đường. Thay bằng các khoảng trống 8-10m đất trần và cụm cỏ bất đối xứng tại các điểm mốc.
    /// </summary>
    public static class UpgradeRiceFieldDepth25D
    {
        public const string RootName = "[Additive_RiceField_100m]";
        private const string RiceTexDir = "Assets/Textures/Lana_Unified_Art/Rice";
        private const string GenMeshDir = "Assets/Materials/LanaOverhaul/GeneratedMeshes";
        private const string ClutterDir = "Assets/Prefabs/Clutter";
        private const string ArtifactDir = "C:/Users/Thanh Tài/.gemini/antigravity/brain/8f0de94b-4312-45db-8a61-1ed2ab228bb4";

        [MenuItem("Hoa va Mau/Level Design/Upgrade Rice Field Depth 2.5D (Master)", false, 0)]
        public static void UpgradeAndCapture()
        {
            Debug.Log("<color=#3b82f6>[UpgradeRiceFieldDepth25D]</color> Bắt đầu nâng cấp chiều sâu không gian 2.5D...");

            // 1. HẠ THẤP SKYBOX & CĂN CHỈNH DÃY NÚI XA KHỚP ĐƯỜNG CHÂN TRỜI
            ExtendAndLowerSkybox();

            // 2. DỰNG LẠI CÁNH ĐỒNG LÚA VỚI 3 BẬC TRUNG CẢNH, TIỀN CẢNH SÁT CAMERA VÀ ĐƯỜNG ĐI TỰ NHIÊN
            BuildRiceFieldDepthOverhaul(138.0f, 165.0f, -1.85f);

            // 3. LƯU SCENE
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            // 4. CHỤP LẠI 4 ẢNH VERIFICATION 720P JPG
            CaptureVerificationViews();

            Debug.Log("<color=#10b981>[UpgradeRiceFieldDepth25D]</color> HOÀN THÀNH NÂNG CẤP CHIỀU SÂU 2.5D & XUẤT 4 ẢNH 720P!");
        }

        // =========================================================================
        // MỤC 1: HẠ THẤP SKYBOX ĐỂ DÃY NÚI KHỚP ĐƯỜNG CHÂN TRỜI
        // =========================================================================
        private static void ExtendAndLowerSkybox()
        {
            var bg = GameObject.Find("Grand_Unified_PlanetOfLana_Background");
            if (bg != null)
            {
                Undo.RecordObject(bg.transform, "Lower Skybox Horizon");
                // Mở rộng Scale X = 440m để bao phủ toàn bộ cảnh chơi (-70m đến +370m)
                // Hạ localPosition.y từ 22.0m xuống 10.5m để phần chân núi và đồi xanh ở đáy texture
                // khớp chính xác vào đường chân trời phía sau nhà tranh (Y = 0m .. 2.0m)
                bg.transform.localPosition = new Vector3(150.0f, 10.5f, 18.0f);
                bg.transform.localScale = new Vector3(440.0f, 56.0f, 1.0f);

                var mr = bg.GetComponent<MeshRenderer>();
                if (mr != null && mr.sharedMaterial != null)
                {
                    Undo.RecordObject(mr.sharedMaterial, "Tune Skybox Material");
                    mr.sharedMaterial.SetFloat("_BottomFeather", 0.0f); // Không làm mờ chân núi
                }
                Debug.Log("<color=#10b981>[UpgradeRiceFieldDepth25D]</color> Đã hạ thấp Skybox Y = 10.5m để lộ rõ dãy núi chân trời!");
            }

            var skyHills = GameObject.Find("Skybox_Unified_PlainsHills");
            if (skyHills != null)
            {
                Undo.RecordObject(skyHills.transform, "Lower Skybox PlainsHills");
                skyHills.transform.localPosition = new Vector3(150.0f, 10.5f, 18.0f);
                skyHills.transform.localScale = new Vector3(440.0f, 56.0f, 1.0f);
            }
        }

        // =========================================================================
        // MỤC 2, 3, 4: DỰNG CÁNH ĐỒNG LÚA CHIỀU SÂU 2.5D
        // =========================================================================
        private static void BuildRiceFieldDepthOverhaul(float startX, float lengthMeters, float pathY = -1.85f)
        {
            float endX = startX + lengthMeters;

            GameObject root = GameObject.Find(RootName);
            if (root != null)
            {
                Undo.DestroyObjectImmediate(root);
            }
            root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Rebuild Rice Field with 2.5D Depth Master");

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

            // Foliage & Architecture Prefabs
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
            // 1. MẶT NỀN ĐẤT BÙN NÂU RỘNG (MỞ RỘNG TỚI Z = 16.0m)
            // =========================================================================
            var dioramaRoot = new GameObject("00_Continuous_BrownMud_DioramaFloor");
            dioramaRoot.transform.SetParent(root.transform, false);

            CreateContinuousMudFloor(
                dioramaRoot.transform, "RiceField165m_DeepBrownMud_16m",
                startX - 8.0f, endX + 16.0f, pathY, brownMudMat);

            // =========================================================================
            // 2. ĐƯỜNG ĐI CHÍNH & KHỐI ĐÁY ĐEN (BLACK GROUND SKIRT)
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

            // =========================================================================
            // MỤC 4: XÓA BỎ MÃ VẠCH TRÊN ĐƯỜNG - TẠO CÁC KHOẢNG TRỐNG 8-10M & CỤM CỎ ĐIỂM XUYẾT
            // Tuyệt đối không dùng vòng lặp cách đều px += 4.5f!
            // =========================================================================
            var clutterWaypointsRoot = new GameObject("01B_Pathway_NaturalClutter_Waypoints");
            clutterWaypointsRoot.transform.SetParent(pathRoot.transform, false);

            // Danh sách các điểm mốc tự nhiên (cách nhau 8 - 14m)
            float[] landmarkXs = new float[] {
                startX + 7.5f,   // ~145.5m (sau chòi sàn)
                startX + 22.0f,  // ~160.0m (giữa đồng)
                startX + 34.5f,  // ~172.5m (tiếp cận nhà tranh 1)
                startX + 48.5f,  // ~186.5m (vào crouch pocket)
                startX + 65.0f,  // ~203.0m (nhà tranh 2)
                startX + 83.0f,  // ~221.0m (tiếp cận cây đa)
                startX + 96.5f   // ~234.5m (bên phải cây đa)
            };

            foreach (float lx in landmarkXs)
            {
                // Cụm 3 bụi cỏ thấp cao không đồng đều tại mỗi mốc
                if (pfGrassShort != null)
                {
                    Color grassCol = GetAtmosphericTint(0.2f, new Color(0.92f, 0.88f, 0.65f, 1f));
                    // Bụi chính 0.28m
                    SpawnPrefab(pfGrassShort, $"LandmarkGrass_{lx:F0}_A", clutterWaypointsRoot.transform,
                        new Vector3(lx, pathY + 0.01f, 0.25f), 0.28f, false,
                        grassCol, 13, spriteMat, 0.8f, 0.9f);
                    // Bụi nhỏ nép cạnh 0.18m
                    SpawnPrefab(pfGrassShort, $"LandmarkGrass_{lx:F0}_B", clutterWaypointsRoot.transform,
                        new Vector3(lx + 0.35f, pathY + 0.01f, 0.15f), 0.18f, true,
                        grassCol, 13, spriteMat, 0.6f, 1.1f);
                }

                // Điểm xuyết phiến đá bước hoặc cành cây nhỏ
                if (pfRockSlab != null && (lx % 15f < 8f))
                {
                    Color rockCol = GetAtmosphericTint(0.15f, new Color(0.65f, 0.62f, 0.55f, 1f));
                    SpawnPrefab(pfRockSlab, $"LandmarkStone_{lx:F0}", clutterWaypointsRoot.transform,
                        new Vector3(lx - 0.40f, pathY - 0.02f, 0.18f), 0.32f, false,
                        rockCol, 12, spriteMat, 0f, 0f);
                }
            }

            // Dấu vết lao động nông thôn ven đường (chỉ 4 điểm mốc xa nhau)
            if (pfClutterRuralPath != null)
            {
                SpawnPrefab(pfClutterRuralPath, "Clutter_Story_Waypoint_146m", clutterWaypointsRoot.transform,
                    new Vector3(startX + 8.0f, pathY, 0.55f), 0.85f, false, Color.white, 14, spriteMat, 0f, 0f);

                SpawnPrefab(pfClutterRuralPath, "Clutter_Story_Waypoint_188m", clutterWaypointsRoot.transform,
                    new Vector3(startX + 50.0f, pathY, 0.60f), 0.85f, true, Color.white, 14, spriteMat, 0f, 0f);

                SpawnPrefab(pfClutterRuralPath, "Clutter_Story_Waypoint_218m", clutterWaypointsRoot.transform,
                    new Vector3(startX + 80.0f, pathY, 0.55f), 0.85f, false, Color.white, 14, spriteMat, 0f, 0f);
            }

            // =========================================================================
            // MỤC 3: LỚP LÚA TIỀN CẢNH SÁT ỐNG KÍNH (Z = -2.2m .. -2.8m)
            // Che bớt mảng đen đáy màn hình, giảm diện tích đen từ 30% xuống còn 12-15%.
            // Scale lớn 1.7x - 2.1x, Y thấp, màu tối sẫm Color(0.35f, 0.38f, 0.28f).
            // =========================================================================
            var fgRoot = new GameObject("02_Foreground_CameraSilhouette_Framing");
            fgRoot.transform.SetParent(root.transform, false);

            Color fgDarkSilhouette = new Color(0.35f, 0.38f, 0.28f, 0.98f);
            Sprite[] fgSprites = new Sprite[] { sprTallSway, sprPanicle, sprCluster, sprDenseHead };

            // Trải các khóm lúa lớn dọc đáy màn hình sát ống kính
            for (float fx = startX + 1.5f; fx <= endX - 1.5f; fx += 2.8f)
            {
                float jx = fx + Mathf.Sin(fx * 2.3f) * 0.45f;
                float fz = -2.50f + Mathf.Cos(fx * 1.7f) * 0.25f; // Z = -2.25m .. -2.75m sát camera
                float fy = -2.35f + Mathf.Sin(fx * 1.1f) * 0.08f; // Y thấp để gốc chìm ngoài đáy màn hình
                float scale = 1.75f + Mathf.Abs(Mathf.Sin(fx * 3.7f)) * 0.30f; // Scale 1.75x .. 2.05x

                int sprIdx = Mathf.Abs((int)(fx * 1.3f)) % fgSprites.Length;
                Sprite fgSpr = fgSprites[sprIdx] ?? sprTallSway ?? sprPanicle;
                if (fgSpr != null)
                {
                    bool flip = (fx % 4.0f > 2.0f);
                    Spawn2DSprite(fgSpr, $"FG_SilhouetteStalk_{jx:F0}", fgRoot.transform,
                        new Vector3(jx, fy, fz), new Vector2(flip ? -scale * 0.35f : scale * 0.35f, scale * 0.35f),
                        fgDarkSilhouette, 26, spriteMat, 1.2f, 0.75f);
                }
            }

            // Thêm các khóm che viền mép đường (Z = -0.6m .. -0.9m) ở các đoạn xen kẽ
            for (float rx = startX + 4.0f; rx <= endX - 4.0f; rx += 4.2f)
            {
                float jx = rx + Mathf.Sin(rx * 2.7f) * 0.3f;
                float rz = -0.75f + Mathf.Cos(rx * 2.1f) * 0.15f;
                float ry = pathY - 0.03f;
                Sprite lipSpr = (rx % 6.0f < 3.0f) ? (sprClump ?? sprMedium) : (sprPanicle ?? sprTallSway);
                if (lipSpr != null)
                {
                    bool flip = (rx % 3.0f > 1.5f);
                    Spawn2DSprite(lipSpr, $"RoadLip_Soft_{jx:F0}", fgRoot.transform,
                        new Vector3(jx, ry, rz), new Vector2(flip ? -0.42f : 0.42f, 0.42f),
                        new Color(0.48f, 0.50f, 0.38f, 1.0f), 22, spriteMat, 1.0f, 0.9f);
                }
            }

            // =========================================================================
            // MỤC 2: MỞ RỘNG CÁNH ĐỒNG LÚA TRUNG CẢNH THÀNH 3 THỬA RUỘNG ĐAN XEN
            // Tier 1: Z = +3.5m (Y = pathY + 0.12m)
            // Tier 2: Z = +7.0m (Y = pathY + 0.27m)
            // Tier 3: Z = +11.0m (Y = pathY + 0.42m)
            // =========================================================================
            var ricePlotsRoot = new GameObject("03_TerracedRicePlots_3Tiers");
            ricePlotsRoot.transform.SetParent(root.transform, false);

            // --- TIER 1: CẬN TRUNG CẢNH (Z = +3.2m .. +3.8m) ---
            var t1Root = new GameObject("Tier1_NearRicePaddy_Z3.5m");
            t1Root.transform.SetParent(ricePlotsRoot.transform, false);

            for (float rx = startX + 1.0f; rx <= endX - 1.0f; rx += 1.8f)
            {
                float jx = rx + Mathf.Sin(rx * 2.1f) * 0.25f;
                float jz = 3.5f + Mathf.Cos(rx * 1.5f) * 0.30f;
                float jy = GetMudFloorY(pathY, jz) + 0.12f;
                Color t1Color = GetAtmosphericTint(jz, new Color(1.00f, 0.94f, 0.58f, 1.0f));

                Sprite rSpr = (rx % 3.6f < 1.8f) ? (sprWideRow ?? sprTallSway) : (sprClump ?? sprDenseHead);
                if (rSpr != null)
                {
                    bool flip = (rx % 4.0f > 2.0f);
                    Spawn2DSprite(rSpr, $"T1_Rice_{jx:F0}", t1Root.transform,
                        new Vector3(jx, jy, jz), new Vector2(flip ? -0.46f : 0.46f, 0.46f),
                        t1Color, 10, spriteMat, 1.5f, 1.1f);
                }
            }

            // --- TIER 2: CHÍNH TRUNG CẢNH (Z = +6.8m .. +7.5m) ---
            var t2Root = new GameObject("Tier2_MidRicePaddy_Z7.0m");
            t2Root.transform.SetParent(ricePlotsRoot.transform, false);

            for (float rx = startX + 0.5f; rx <= endX - 0.5f; rx += 2.2f)
            {
                float jx = rx + Mathf.Sin(rx * 1.8f) * 0.35f;
                float jz = 7.0f + Mathf.Cos(rx * 1.3f) * 0.35f;
                float jy = GetMudFloorY(pathY, jz) + 0.27f;
                Color t2Color = GetAtmosphericTint(jz, new Color(0.92f, 0.88f, 0.58f, 1.0f));

                Sprite rSpr = (rx % 4.4f < 2.2f) ? (sprWideRow ?? sprMedium) : (sprPanicle ?? sprCluster);
                if (rSpr != null)
                {
                    bool flip = (rx % 5.0f > 2.5f);
                    Spawn2DSprite(rSpr, $"T2_Rice_{jx:F0}", t2Root.transform,
                        new Vector3(jx, jy, jz), new Vector2(flip ? -0.44f : 0.44f, 0.44f),
                        t2Color, 8, spriteMat, 1.3f, 1.0f);
                }
            }

            // --- TIER 3: HẬU TRUNG CẢNH (Z = +10.8m .. +11.6m) ---
            var t3Root = new GameObject("Tier3_DeepRicePaddy_Z11.0m");
            t3Root.transform.SetParent(ricePlotsRoot.transform, false);

            for (float rx = startX + 1.0f; rx <= endX - 1.0f; rx += 2.6f)
            {
                float jx = rx + Mathf.Sin(rx * 1.4f) * 0.40f;
                float jz = 11.0f + Mathf.Cos(rx * 1.1f) * 0.40f;
                float jy = GetMudFloorY(pathY, jz) + 0.42f;
                Color t3Color = GetAtmosphericTint(jz, new Color(0.85f, 0.84f, 0.62f, 1.0f));

                Sprite rSpr = (rx % 5.2f < 2.6f) ? (sprWideRow ?? sprDenseHead) : (sprClump ?? sprMedium);
                if (rSpr != null)
                {
                    bool flip = (rx % 6.0f > 3.0f);
                    Spawn2DSprite(rSpr, $"T3_Rice_{jx:F0}", t3Root.transform,
                        new Vector3(jx, jy, jz), new Vector2(flip ? -0.40f : 0.40f, 0.40f),
                        t3Color, 6, spriteMat, 1.1f, 0.9f);
                }
            }

            // =========================================================================
            // MỤC 1 (TIẾP): DẢI CÂY BỤI & RẶNG TRE NHỎ MỜ CHÂN TRỜI XA (Z = +19m .. +25m)
            // Nối kết giữa cánh đồng và chân dãy núi, xóa bỏ hoàn toàn dải xanh trống trải!
            // Scale nhỏ 0.35x - 0.5x, pha màu xanh sương mù Color(0.55f, 0.68f, 0.75f).
            // =========================================================================
            var horizonTreelineRoot = new GameObject("05_Distant_Horizon_Treeline");
            horizonTreelineRoot.transform.SetParent(root.transform, false);

            Color horizonFogColor = new Color(0.55f, 0.68f, 0.75f, 0.88f);

            for (float hx = startX - 4.0f; hx <= endX + 10.0f; hx += 5.5f)
            {
                float jx = hx + Mathf.Sin(hx * 1.6f) * 0.8f;
                float hz = 21.0f + Mathf.Cos(hx * 1.2f) * 2.5f; // Z = +18.5m .. +23.5m
                float hy = -1.25f + Mathf.Sin(hx * 0.9f) * 0.15f; // Khớp chân núi

                float scaleMod = 0.38f + Mathf.Abs(Mathf.Sin(hx * 2.2f)) * 0.10f; // Scale 0.38x - 0.48x

                if (hx % 16.5f < 5.5f && pfBamboo != null)
                {
                    SpawnNormalizedPrefab(pfBamboo, $"Horizon_Bamboo_{jx:F0}", horizonTreelineRoot.transform,
                        new Vector3(jx, hy, hz), 5.5f * scaleMod, (hx % 2f > 1f),
                        horizonFogColor, 3, spriteMat, 0.4f, 0.5f);
                }
                else if (hx % 16.5f < 11.0f && pfMedCanopy != null)
                {
                    SpawnNormalizedPrefab(pfMedCanopy, $"Horizon_Canopy_{jx:F0}", horizonTreelineRoot.transform,
                        new Vector3(jx, hy, hz), 4.8f * scaleMod, (hx % 3f > 1.5f),
                        horizonFogColor, 3, spriteMat, 0.3f, 0.4f);
                }
                else if (pfBanana != null)
                {
                    SpawnNormalizedPrefab(pfBanana, $"Horizon_BananaGrove_{jx:F0}", horizonTreelineRoot.transform,
                        new Vector3(jx, hy, hz), 3.5f * scaleMod, false,
                        horizonFogColor, 3, spriteMat, 0.3f, 0.4f);
                }
            }

            // =========================================================================
            // CÁC CỤM ĐIỂM NHẤN CÂU CHUYỆN (BẢO TOÀN NGUYÊN TẮC BẤT ĐỐI XỨNG ĐÃ ĐẠT CHUẨN)
            // =========================================================================
            var clusterRoot = new GameObject("04_StoryClusters_Asymmetric");
            clusterRoot.transform.SetParent(root.transform, false);

            // A. Chòi sàn nhỏ (X = 142.8m): 3 chuối gom lệch trái (3.8m, 2.5m, 1.9m), phải có củi/lu nước
            BuildZone1Shelter(clusterRoot.transform, startX, pathY, spriteMat,
                pfWaitingShelter, pfBanana, pfClutterCottage, pfClutterRuralPath, pfReeds);

            // B. Cụm nhà tranh 1 (X = 168.0m): Nhà 3.8m + giàn bầu bên phải + dừa nghiêng bên trái
            BuildZone3Cottage1(clusterRoot.transform, startX, pathY, spriteMat,
                pfThatchedHut, pfTrellis, pfCoconut, pfBanana, pfBamboo, pfClutterCottage, pfClutterCoconut);

            // C. Vùng lúa cao (Crouch / Stealth Pocket - X = 180m - 192m)
            BuildZone4CrouchPocket(clusterRoot.transform, startX, pathY, spriteMat,
                sprTallSway, sprPanicle, sprClump);

            // D. Cụm nhà tranh 2 (X = 202.0m)
            BuildZone5Cottage2(clusterRoot.transform, startX, pathY, spriteMat,
                pfThatchedHut, pfMedCanopy, pfCoconut, pfBamboo, pfClutterCottage, pfClutterCoconut);

            // E. Cây đa cổ thụ lớn (X = 232.0m): Đa 9.8m + 2 dừa lệch trái (7.5m @ 8°, 5.6m @ 14°), phải có đá dừng chân
            BuildZone7GrandBanyan(clusterRoot.transform, startX, pathY, spriteMat,
                pfBigCanopy, pfCoconut, pfRockSlab, pfClutterCoconut);
        }

        // =========================================================================
        // HELPER BUILDERS CHO CÁC VÙNG CỐT TRUYỆN
        // =========================================================================
        private static void BuildZone1Shelter(
            Transform parent, float startX, float pathY, Material spriteMat,
            GameObject pfWaitingShelter, GameObject pfBanana, GameObject pfClutterCottage,
            GameObject pfClutterRuralPath, GameObject pfReeds)
        {
            var z1Root = new GameObject("Zone1_Shelter_LeftBananas_RightClutter");
            z1Root.transform.SetParent(parent, false);

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

            // GOM 3 CÂY CHUỐI LỆCH HẲN SANG BÊN TRÁI CHÒI
            if (pfBanana != null)
            {
                Color bananaColor = GetAtmosphericTint(2.8f, new Color(0.55f, 0.68f, 0.45f, 1f));
                SpawnNormalizedPrefab(pfBanana, "Z1_Banana_Left_Tall_3.8m", z1Root.transform,
                    new Vector3(shelterX - 3.2f, GetMudFloorY(pathY, 2.9f), 2.9f), 3.8f, false,
                    bananaColor, 14, spriteMat, 1.2f, 1.0f);

                SpawnNormalizedPrefab(pfBanana, "Z1_Banana_Left_Low_2.5m", z1Root.transform,
                    new Vector3(shelterX - 1.8f, GetMudFloorY(pathY, 2.6f), 2.6f), 2.5f, true,
                    bananaColor, 14, spriteMat, 1.4f, 1.1f);

                SpawnNormalizedPrefab(pfBanana, "Z1_Banana_Left_Small_1.9m", z1Root.transform,
                    new Vector3(shelterX - 4.4f, GetMudFloorY(pathY, 3.1f), 3.1f), 1.9f, false,
                    bananaColor, 13, spriteMat, 1.0f, 0.9f);
            }

            // BÊN PHẢI CHÒI: CỎ THẤP VÀ ĐỐNG CỦI / LU NƯỚC
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
        }

        private static void BuildZone3Cottage1(
            Transform parent, float startX, float pathY, Material spriteMat,
            GameObject pfThatchedHut, GameObject pfTrellis, GameObject pfCoconut,
            GameObject pfBanana, GameObject pfBamboo, GameObject pfClutterCottage, GameObject pfClutterCoconut)
        {
            var z3Root = new GameObject("Zone3_CottageCluster1");
            z3Root.transform.SetParent(parent, false);

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
        }

        private static void BuildZone4CrouchPocket(
            Transform parent, float startX, float pathY, Material spriteMat,
            Sprite sprTallSway, Sprite sprPanicle, Sprite sprClump)
        {
            var z4Root = new GameObject("Zone4_Gameplay_CrouchPocket");
            z4Root.transform.SetParent(parent, false);

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
        }

        private static void BuildZone5Cottage2(
            Transform parent, float startX, float pathY, Material spriteMat,
            GameObject pfThatchedHut, GameObject pfMedCanopy, GameObject pfCoconut,
            GameObject pfBamboo, GameObject pfClutterCottage, GameObject pfClutterCoconut)
        {
            var z5Root = new GameObject("Zone5_CottageCluster2");
            z5Root.transform.SetParent(parent, false);

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
        }

        private static void BuildZone7GrandBanyan(
            Transform parent, float startX, float pathY, Material spriteMat,
            GameObject pfBigCanopy, GameObject pfCoconut, GameObject pfRockSlab, GameObject pfClutterCoconut)
        {
            var z7Root = new GameObject("Zone7_GrandBanyan_AsymmetricCoconuts");
            z7Root.transform.SetParent(parent, false);

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

            // GOM 2 CÂY DỪA LỆCH HẲN SANG BÊN TRÁI CÂY ĐA
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

            // BÊN PHẢI CÂY ĐA: PHIẾN ĐÁ DỪNG CHÂN MỞ RA BẾN TÀU
            if (pfRockSlab != null)
            {
                Color rockCol = GetAtmosphericTint(2.5f, new Color(0.68f, 0.65f, 0.58f, 1f));
                SpawnPrefab(pfRockSlab, "Z7_Right_RestingStone_ToDock", z7Root.transform,
                    new Vector3(banyanX + 3.8f, pathY - 0.02f, 2.5f), 0.50f, false,
                    rockCol, 12, spriteMat, 0f, 0f);
            }
        }

        // =========================================================================
        // CAPTURE 4 ẢNH VERIFICATION 720P JPG
        // Nâng nhẹ góc nhìn lên Y = 0.55m để giảm tỷ lệ dải đen đáy từ 30% xuống 12-15%
        // =========================================================================
        public static void CaptureVerificationViews()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                var cams = Object.FindObjectsByType<Camera>();
                if (cams.Length > 0) mainCam = cams[0];
            }

            if (mainCam == null)
            {
                Debug.LogError("[UpgradeRiceFieldDepth25D] Không tìm thấy Camera để chụp ảnh!");
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
                "depth25d_view1_shelter_mountains.jpg",
                "depth25d_view2_cottage1_rice_sea.jpg",
                "depth25d_view3_crouch_cottage2_depth.jpg",
                "depth25d_view4_banyan_horizon_mountains.jpg"
            };

            for (int i = 0; i < targetXs.Length; i++)
            {
                // Nâng góc nhìn lên Y = 0.55m kết hợp lúa tiền cảnh Z = -2.5m che đáy đen
                mainCam.transform.position = new Vector3(targetXs[i], 0.55f, -15.5f);
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
                Debug.Log($"<color=#10b981>[UpgradeRiceFieldDepth25D]</color> Đã xuất ảnh kiểm tra: {fileNames[i]}");
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
            float slopeAngleDeg = 0.55f;
            float tanSlope = Mathf.Tan(slopeAngleDeg * Mathf.Deg2Rad);
            return pathY + (z - 0.45f) * tanSlope - 0.06f;
        }

        public static Color GetAtmosphericTint(float z, Color baseColor)
        {
            if (z <= -1.0f)
            {
                Color fgDark = new Color(0.35f, 0.38f, 0.28f, 1.0f);
                return Color.Lerp(baseColor, fgDark, 0.40f);
            }
            else if (z <= 1.0f)
            {
                return baseColor;
            }
            else
            {
                Color riverFogTint = new Color(0.55f, 0.68f, 0.78f, 1.0f);
                float t = Mathf.Clamp01((z - 1.0f) / 16.0f) * 0.38f;
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

        private static GameObject SpawnPrefab(
            GameObject prefab, string name, Transform parent,
            Vector3 pos, float scale, bool flipX,
            Color tintColor, int sortOrder, Material mat,
            float swayAngle = 0f, float swaySpeed = 0f)
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

        private static GameObject Spawn2DSprite(
            Sprite sprite, string name, Transform parent,
            Vector3 pos, Vector2 scale, Color tintColor,
            int sortOrder, Material mat,
            float swayAngle = 0f, float swaySpeed = 0f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(scale.x, scale.y, 1.0f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            if (mat != null) sr.sharedMaterial = mat;
            sr.color = tintColor;
            sr.sortingOrder = sortOrder;

            if (swayAngle > 0.05f)
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

        private static Material GetCleanSpriteMaterial()
        {
            Shader spriteShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
            if (spriteShader == null) spriteShader = Shader.Find("Sprites/Default");
            return new Material(spriteShader);
        }

        private static Material GetOrCreateMudGroundMaterial()
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/LanaOverhaul/M_RiceField_MudGround.mat");
            if (mat == null)
            {
                Shader uLit = Shader.Find("Universal Render Pipeline/Lit");
                if (uLit == null) uLit = Shader.Find("Standard");
                mat = new Material(uLit);
                mat.name = "M_RiceField_MudGround";
                mat.color = new Color(0.28f, 0.22f, 0.16f, 1.0f); // Nâu bùn sẫm
                mat.SetFloat("_Smoothness", 0.15f);
                if (!Directory.Exists("Assets/Materials/LanaOverhaul"))
                {
                    Directory.CreateDirectory("Assets/Materials/LanaOverhaul");
                }
                AssetDatabase.CreateAsset(mat, "Assets/Materials/LanaOverhaul/M_RiceField_MudGround.mat");
            }
            return mat;
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
            // Mở rộng sâu đến Z = 16.0m
            float[] depthZs = new float[] { 0.45f, 2.0f, 4.0f, 6.5f, 9.0f, 12.0f, 16.0f };
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

                    Color baseGroundColor = new Color(0.44f, 0.40f, 0.30f, 1.0f);
                    Color vertexCol = GetAtmosphericTint(z, baseGroundColor);
                    colors.Add(vertexCol);

                    uvs.Add(new Vector2(u * 22f, v * 4.5f));
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

            Mesh mesh = new Mesh();
            mesh.name = meshName;
            mesh.SetVertices(verts);
            mesh.SetColors(colors);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void CreateLanaLedgeSection(
            Transform parent, string name,
            float startX, float endX, int segments,
            float capThickness, float capOverlap, float bottomY,
            System.Func<float, float> heightProfile,
            Material grassCapMat, Material rockUnderMat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var subUnder = new GameObject("RockUnderbelly");
            subUnder.transform.SetParent(go.transform, false);
            var subCap = new GameObject("GrassCap");
            subCap.transform.SetParent(go.transform, false);

            if (!Directory.Exists(GenMeshDir)) Directory.CreateDirectory(GenMeshDir);

            Mesh underMesh = BuildUnderbellyMesh($"{name}_Underbelly", startX, endX, heightProfile, bottomY, segments);
            AssetDatabase.CreateAsset(underMesh, $"{GenMeshDir}/{name}_Underbelly.asset");

            var mfUnder = subUnder.AddComponent<MeshFilter>();
            mfUnder.sharedMesh = underMesh;
            var mrUnder = subUnder.AddComponent<MeshRenderer>();
            mrUnder.sharedMaterial = rockUnderMat;
            mrUnder.shadowCastingMode = ShadowCastingMode.Off;

            Mesh capMesh = BuildGrassCapMesh($"{name}_GrassCap", startX, endX, heightProfile, capThickness, capOverlap, segments);
            AssetDatabase.CreateAsset(capMesh, $"{GenMeshDir}/{name}_GrassCap.asset");

            var mfCap = subCap.AddComponent<MeshFilter>();
            mfCap.sharedMesh = capMesh;
            var mrCap = subCap.AddComponent<MeshRenderer>();
            mrCap.sharedMaterial = grassCapMat;
            mrCap.shadowCastingMode = ShadowCastingMode.Off;
        }

        private static Mesh BuildGrassCapMesh(
            string meshName, float startX, float endX,
            System.Func<float, float> heightFunc, float thickness, float overhang, int segX)
        {
            int cols = segX + 1;
            int rowsY = 3;
            var verts = new List<Vector3>();
            var colors = new List<Color>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            for (int r = 0; r <= rowsY; r++)
            {
                float v = (float)r / rowsY;
                for (int c = 0; c <= segX; c++)
                {
                    float u = (float)c / segX;
                    float x = Mathf.Lerp(startX, endX, u);
                    float surfY = heightFunc(x);
                    float topY = surfY + 0.05f;
                    float botY = surfY - thickness;
                    float y = Mathf.Lerp(topY, botY, v);
                    float z = -0.05f + v * 0.40f;

                    verts.Add(new Vector3(x, y, z));
                    colors.Add(new Color(1f, 0.96f, 0.70f, 1f));
                    uvs.Add(new Vector2(u * 28f, v));
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

            Mesh m = new Mesh();
            m.name = meshName;
            m.SetVertices(verts);
            m.SetColors(colors);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        private static Mesh BuildUnderbellyMesh(
            string meshName, float startX, float endX,
            System.Func<float, float> heightFunc, float bottomY, int segX)
        {
            int cols = segX + 1;
            int rowsY = 4;
            float frontZ = -0.05f;
            float backZ = 0.55f;

            var verts = new List<Vector3>();
            var colors = new List<Color>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            for (int r = 0; r <= rowsY; r++)
            {
                float v = (float)r / rowsY;
                for (int c = 0; c <= segX; c++)
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

            Mesh m = new Mesh();
            m.name = meshName;
            m.SetVertices(verts);
            m.SetColors(colors);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
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
    }
}
#endif
