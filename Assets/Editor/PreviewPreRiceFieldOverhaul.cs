#if UNITY_EDITOR
using System.IO;
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
    /// File C# Editor Script tái thiết kế toàn diện các cảnh phía trước ruộng lúa (X = -12m đến 138m)
    /// theo đúng chuẩn mực nghệ thuật điện ảnh Planet of Lana & Màu nước Nam Bộ 1950s:
    /// 1. Cảnh 1 - Rừng Cổ Thụ Mở Đầu (X = -12m .. +5m):
    ///    - Vòm lá cổ thụ tiền cảnh (Foreground Vignette) rủ bóng sát camera tạo cảm giác quay lén qua kẽ lá.
    ///    - Gốc đa cổ thụ đại ngàn với chùm rễ bạnh bọc đất đỏ, đá rêu và thảm cúc dại che kín mép cắt đen.
    /// 2. Cảnh 2 & 3 - Vực Thẳm Đu Dây & Hẻm Đá Hùng Vĩ (X = 14m .. 35m):
    ///    - Bậc đá giật cấp tự nhiên phá vỡ cấu trúc "hộp chữ nhật", dây leo rủ dài xuống lòng vực.
    ///    - Đáy hẻm ẩm ướt lót thảm rêu sẫm, phiến đá cuội và vạt lau sậy đón ánh sáng le lói từ trên đỉnh.
    /// 3. Cảnh 4 - Đỉnh Đồi Cỏ Lau & Cây Đại Thụ Đón Gió (The Grand Overlook Crest, X = 40m .. 90m):
    ///    - Gỡ bỏ hoàn toàn lúa vàng cũ trên đồi.
    ///    - Thay bằng Đồi Cỏ Lau trắng muốt & Thảm Hoa Dại vàng/tím rực rỡ đón gió.
    ///    - Cụm Cây Đại Thụ Đón Gió cao 11m ngự trên đỉnh cao nhất, phiến đá ngắm cảnh phóng tầm mắt xuống thung lũng.
    /// 4. Cảnh 5 - Làng Rạch Gốc Ven Đồi (X = 95m .. 138m):
    ///    - Rặng tre làng râm mát bao bọc mái tranh, giàn bầu sai quả, bờ rào chuối và bậc đá cuội nối mượt sang ruộng lúa 165m.
    /// </summary>
    public static class PreviewPreRiceFieldOverhaul
    {
        public const string PreviewRootName = "[Preview_PreRiceField_Overhaul]";
        private const string ArtifactDir = "C:/Users/Thanh Tài/.gemini/antigravity/brain/8f0de94b-4312-45db-8a61-1ed2ab228bb4";

        [MenuItem("Hoa va Mau/Level Design/Generate Cinematic Pre-Rice Redesign (V2 Master)", false, 1)]
        public static void GeneratePreviewAndCapture()
        {
            Debug.Log("<color=#3b82f6>[PreviewPreRiceField V2]</color> Bắt đầu dựng bản thiết kế điện ảnh mới từ Asset...");

            // 1. TẠO CONTAINER MỚI AN TOÀN TUYỆT ĐỐI
            GameObject previewRoot = GameObject.Find(PreviewRootName);
            if (previewRoot != null)
            {
                Undo.DestroyObjectImmediate(previewRoot);
            }
            previewRoot = new GameObject(PreviewRootName);
            Undo.RegisterCreatedObjectUndo(previewRoot, "Build Cinematic Pre-Rice Field Overhaul V2");

            // Tạm thời ẩn nhóm lúa vàng cũ trên đồi để lộ đồi hoa dại mới
            ToggleOldHillRice(false);

            // 2. TẢI CÁC ASSET CHÍNH THỨC CỦA DỰ ÁN
            Material spriteMat = GetCleanSpriteMaterial();

            // Cây cối
            GameObject pfBigCanopy  = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_01_LargeCanopy.prefab");
            GameObject pfMedCanopy  = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_02_Medium.prefab");
            GameObject pfSlender    = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_04_SlenderTall.prefab");
            GameObject pfBanana     = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_05_Banana.prefab");
            GameObject pfCoconut    = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_06_Coconut.prefab");
            GameObject pfBamboo     = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_07_BambooGrove.prefab");

            // Đá & vách
            GameObject pfRockCliff  = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_Concept_Assets/Rocks/PF_T_HD_Rock_01_LargeCliffOutcrop.prefab");
            GameObject pfRockSlab   = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_Concept_Assets/Rocks/PF_T_HD_Rock_04_FlatSlab.prefab");
            GameObject pfRockMossy  = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_Concept_Assets/Rocks/PF_T_HD_Rock_02_MediumMossyDome.prefab");

            // Cỏ, hoa dại, bụi rậm & dây leo
            GameObject pfWildDaisy  = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_Concept_Assets/Wildflowers/PF_T_HD_Wildflower_03_WhiteDaisy.prefab");
            GameObject pfWildYellow = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Wildflowers/PF_LanaVN_Wildflower_05_YellowBlossom.prefab");
            GameObject pfReeds      = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_Concept_Assets/Wildflowers/PF_T_HD_Wildflower_02_Reeds.prefab");
            GameObject pfGrassShort = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_Concept_Assets/Grass/PF_T_HD_GrassCarpet_01_Short.prefab");
            GameObject pfGrassDense = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Grass_Vines/PF_LanaVN_GrassCarpet_05_Dense.prefab");
            GameObject pfVinesFG    = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Vegetation/PF_FG_Vines_Hanging.prefab");

            // =========================================================================
            // PHÂN ĐOẠN 1: RỪNG NGUYÊN SINH MỞ ĐẦU (X = -12m .. +5m)
            // Khung cảnh vòm lá u tịch, vạt nắng rọi xuống nhân vật Minh
            // =========================================================================
            var sec1Group = new GameObject("01_ForestSanctuary_Cinematic");
            sec1Group.transform.SetParent(previewRoot.transform, false);

            // Tiền cảnh: Tán lá đại thụ vươn từ mép trên camera tạo khung điện ảnh (Vignette)
            if (pfBigCanopy != null)
            {
                var fgCanopyLeft = SpawnPrefab(pfBigCanopy, "FG_Canopy_Overhang_Left", sec1Group.transform,
                    new Vector3(-5.5f, 5.2f, -2.2f), 2.2f, false,
                    new Color(0.18f, 0.24f, 0.18f, 0.98f), 28, spriteMat, 0.5f, 0.6f);
                if (fgCanopyLeft != null) fgCanopyLeft.transform.rotation = Quaternion.Euler(0f, 0f, -25f);

                var fgCanopyRight = SpawnPrefab(pfBigCanopy, "FG_Canopy_Overhang_Right", sec1Group.transform,
                    new Vector3(2.5f, 5.5f, -2.2f), 1.9f, true,
                    new Color(0.16f, 0.22f, 0.16f, 0.98f), 28, spriteMat, 0.4f, 0.5f);
                if (fgCanopyRight != null) fgCanopyRight.transform.rotation = Quaternion.Euler(0f, 0f, 18f);
            }

            // Dây leo tiền cảnh rủ xuống từ vòm lá
            if (pfVinesFG != null)
            {
                SpawnPrefab(pfVinesFG, "FG_Vines_Left_1", sec1Group.transform,
                    new Vector3(-4.8f, 2.6f, -2.4f), 1.35f, false, new Color(0.22f, 0.28f, 0.20f, 0.95f), 27, spriteMat, 0.8f, 0.7f);
                SpawnPrefab(pfVinesFG, "FG_Vines_Left_2", sec1Group.transform,
                    new Vector3(-3.2f, 2.9f, -2.3f), 1.15f, true, new Color(0.25f, 0.32f, 0.22f, 0.95f), 27, spriteMat, 0.7f, 0.8f);
                SpawnPrefab(pfVinesFG, "FG_Vines_Right", sec1Group.transform,
                    new Vector3(1.2f, 2.8f, -2.4f), 1.25f, false, new Color(0.22f, 0.28f, 0.20f, 0.95f), 27, spriteMat, 0.6f, 0.9f);
            }

            // Gò rễ cây cổ thụ & khối đá rêu bọc kín chân vách đất đen (X = -8m .. +2m)
            if (pfRockMossy != null)
            {
                SpawnPrefab(pfRockMossy, "Forest_AncientRoot_Rock_1", sec1Group.transform,
                    new Vector3(-5.2f, -1.85f, 0.6f), 1.25f, false, new Color(0.55f, 0.62f, 0.48f, 1f), 12, spriteMat);
                SpawnPrefab(pfRockMossy, "Forest_AncientRoot_Rock_2", sec1Group.transform,
                    new Vector3(-0.5f, -1.90f, 0.7f), 1.10f, true, new Color(0.52f, 0.60f, 0.46f, 1f), 12, spriteMat);
            }

            // Thảm dương xỉ, cúc dại và cỏ rậm bọc quanh vị trí xuất phát của Minh (-2.2m)
            if (pfGrassDense != null)
            {
                SpawnPrefab(pfGrassDense, "Forest_Minh_DenseBush_Back", sec1Group.transform,
                    new Vector3(-2.6f, -1.75f, 0.8f), 0.95f, false, new Color(0.60f, 0.72f, 0.50f, 1f), 11, spriteMat, 0.8f, 0.9f);
                SpawnPrefab(pfGrassDense, "Forest_Minh_DenseBush_Front", sec1Group.transform,
                    new Vector3(-1.4f, -1.80f, 0.25f), 0.75f, true, new Color(0.70f, 0.80f, 0.58f, 1f), 14, spriteMat, 0.9f, 1.1f);
            }
            if (pfWildDaisy != null)
            {
                SpawnPrefab(pfWildDaisy, "Forest_DaisyPatch_1", sec1Group.transform,
                    new Vector3(-3.4f, -1.78f, 0.35f), 0.55f, false, Color.white, 13, spriteMat);
                SpawnPrefab(pfWildDaisy, "Forest_DaisyPatch_2", sec1Group.transform,
                    new Vector3(-1.0f, -1.82f, 0.30f), 0.50f, true, Color.white, 13, spriteMat);
            }

            // =========================================================================
            // PHÂN ĐOẠN 2 & 3: VỰC THẲM ĐU DÂY & HẺM NÚI SÂU (X = 14m .. 35m)
            // Các phiến đá giật cấp tự nhiên, rêu phong, dây leo rủ dài và lạch nước ẩm ướt
            // =========================================================================
            var sec23Group = new GameObject("02_03_RavineChasm_Cinematic");
            sec23Group.transform.SetParent(previewRoot.transform, false);

            // Bậc đá giật cấp chìa ra từ vách đứng (X = 14.2m .. 15.2m)
            if (pfRockCliff != null)
            {
                var shelfTop = SpawnPrefab(pfRockCliff, "Chasm_RockShelf_Upper", sec23Group.transform,
                    new Vector3(14.5f, -1.25f, 0.9f), 1.35f, false, new Color(0.58f, 0.62f, 0.55f, 1f), 9, spriteMat);
                if (shelfTop != null) shelfTop.transform.rotation = Quaternion.Euler(0f, 0f, -8f);

                var shelfMid = SpawnPrefab(pfRockCliff, "Chasm_RockShelf_Middle", sec23Group.transform,
                    new Vector3(15.0f, -2.40f, 0.8f), 1.15f, true, new Color(0.50f, 0.55f, 0.48f, 1f), 9, spriteMat);
                if (shelfMid != null) shelfMid.transform.rotation = Quaternion.Euler(0f, 0f, 12f);
            }

            // Dây leo rủ dài từ cành cây đỉnh vực xuống giữa không trung
            if (pfVinesFG != null)
            {
                SpawnPrefab(pfVinesFG, "Chasm_HangingVines_Long", sec23Group.transform,
                    new Vector3(14.2f, -0.2f, 0.4f), 1.65f, false, new Color(0.42f, 0.50f, 0.36f, 1f), 11, spriteMat, 1.2f, 0.8f);
                SpawnPrefab(pfVinesFG, "Chasm_HangingVines_EastWall", sec23Group.transform,
                    new Vector3(17.8f, -1.2f, 0.5f), 1.30f, true, new Color(0.40f, 0.48f, 0.35f, 1f), 11, spriteMat, 1.0f, 0.9f);
            }

            // Đáy vực ẩm ướt: Phiến đá trơn, thảm cỏ nước và lau sậy rủ bóng (Y = -3.95m)
            if (pfRockSlab != null)
            {
                SpawnPrefab(pfRockSlab, "Chasm_Floor_WetSteppingStone", sec23Group.transform,
                    new Vector3(15.9f, -3.95f, 0.6f), 0.70f, false, new Color(0.45f, 0.48f, 0.42f, 1f), 10, spriteMat);
            }
            if (pfReeds != null)
            {
                SpawnPrefab(pfReeds, "Chasm_Floor_SwampReeds_1", sec23Group.transform,
                    new Vector3(15.1f, -3.88f, 0.9f), 0.75f, false, new Color(0.55f, 0.68f, 0.50f, 1f), 10, spriteMat, 0.9f, 1.0f);
                SpawnPrefab(pfReeds, "Chasm_Floor_SwampReeds_2", sec23Group.transform,
                    new Vector3(16.7f, -3.90f, 0.8f), 0.65f, true, new Color(0.52f, 0.65f, 0.48f, 1f), 10, spriteMat, 0.8f, 1.1f);
            }

            // Dốc leo thân cây mục & thùng gỗ (X = 20m .. 32m): Neo chặt các vật thể bằng rêu và hoa dại
            if (pfGrassDense != null)
            {
                SpawnPrefab(pfGrassDense, "HollowLog_MossCover_Base", sec23Group.transform,
                    new Vector3(22.8f, -1.2f, 0.4f), 0.85f, false, new Color(0.55f, 0.65f, 0.45f, 1f), 12, spriteMat);
                SpawnPrefab(pfGrassDense, "CrateShelf_FernCover", sec23Group.transform,
                    new Vector3(29.2f, 0.4f, 0.3f), 0.75f, false, new Color(0.58f, 0.68f, 0.48f, 1f), 12, spriteMat);
            }

            // =========================================================================
            // PHÂN ĐOẠN 4: ĐỈNH ĐỒI CỎ LAU & CÂY ĐẠI THỤ ĐÓN GIÓ (THE GRAND OVERLOOK CREST, X = 40m .. 90m)
            // Khoảnh khắc đắt giá nhất: Đồi hoa dại bạt ngàn, cây đại thụ 11m đón gió ngắm thung lũng
            // =========================================================================
            var sec4Group = new GameObject("04_GrandOverlook_WildflowerCrest_Cinematic");
            sec4Group.transform.SetParent(previewRoot.transform, false);

            // CỤM CÂY ĐẠI THỤ ĐÓN GIÓ (THE GRAND WINDBLOWN GROVE, X = 64m)
            if (pfBigCanopy != null)
            {
                // Cây chính 11m vươn cành đón gió
                var heroTree = SpawnPrefab(pfBigCanopy, "Grand_Windblown_HeroTree_11m", sec4Group.transform,
                    new Vector3(64.8f, 2.30f, 3.4f), 2.25f, false,
                    new Color(0.65f, 0.75f, 0.58f, 1f), 7, spriteMat, 0.8f, 0.5f);
                if (heroTree != null) heroTree.transform.rotation = Quaternion.Euler(0f, 0f, -5.5f);

                // Cây phụ mảnh mai nép cạnh tạo cụm sinh thái tự nhiên
                if (pfSlender != null)
                {
                    var subTree = SpawnPrefab(pfSlender, "Grand_SubTree_Slender", sec4Group.transform,
                        new Vector3(61.8f, 2.10f, 4.2f), 1.65f, true,
                        new Color(0.58f, 0.68f, 0.52f, 1f), 6, spriteMat, 0.9f, 0.6f);
                    if (subTree != null) subTree.transform.rotation = Quaternion.Euler(0f, 0f, 6.0f);
                }
            }

            // Phiến đá dừng chân phẳng lì và cụm đá mộc dưới gốc cây đại thụ
            if (pfRockSlab != null)
            {
                SpawnPrefab(pfRockSlab, "Overlook_RestingStone_Slab", sec4Group.transform,
                    new Vector3(63.8f, 2.18f, 0.85f), 0.85f, false,
                    new Color(0.72f, 0.70f, 0.62f, 1f), 12, spriteMat);
            }
            if (pfRockMossy != null)
            {
                SpawnPrefab(pfRockMossy, "Overlook_TreeBase_MossyBoulder", sec4Group.transform,
                    new Vector3(66.2f, 2.20f, 1.4f), 0.95f, false,
                    new Color(0.60f, 0.66f, 0.52f, 1f), 11, spriteMat);
            }

            // RẶNG CÂY TRUNG CẢNH XA PHÍA SAU ĐỒI (Z = 8m .. 14m) TẠO ĐỘ SÂU BÁT NGÁT
            if (pfBamboo != null)
            {
                SpawnPrefab(pfBamboo, "Overlook_FarBamboo_Ridge_1", sec4Group.transform,
                    new Vector3(52.0f, 1.6f, 9.5f), 1.15f, false,
                    new Color(0.50f, 0.62f, 0.68f, 0.85f), 4, spriteMat, 0.5f, 0.6f);
                SpawnPrefab(pfBamboo, "Overlook_FarBamboo_Ridge_2", sec4Group.transform,
                    new Vector3(76.0f, 1.2f, 10.2f), 1.25f, true,
                    new Color(0.48f, 0.60f, 0.66f, 0.85f), 4, spriteMat, 0.5f, 0.6f);
            }

            // THẢM HOA DẠI VÀNG, CÚC TRẮNG & CỎ LAU BẠT NGÀN TRẢI DÀI TRIỀN ĐỒI (X = 38m .. 88m)
            // Bố cục đan xen theo vạt dày tự nhiên (Clustered Drift), không rải đều
            float[] flowerClusterXs = new float[] {
                40.0f, 43.5f, 47.0f, 51.5f, 56.0f, 60.5f, 63.5f, 67.5f, 72.0f, 76.5f, 81.0f, 85.5f
            };

            foreach (float cx in flowerClusterXs)
            {
                float cy = GetHillHeight(cx);

                // Vạt hoa vàng rực rỡ
                if (pfWildYellow != null)
                {
                    SpawnPrefab(pfWildYellow, $"Hill_YellowDrift_{cx:F0}_A", sec4Group.transform,
                        new Vector3(cx - 0.4f, cy + 0.04f, 0.45f), 0.48f, false,
                        new Color(1f, 0.94f, 0.50f, 1f), 13, spriteMat);
                    SpawnPrefab(pfWildYellow, $"Hill_YellowDrift_{cx:F0}_B", sec4Group.transform,
                        new Vector3(cx + 0.35f, cy + 0.03f, 0.35f), 0.38f, true,
                        new Color(1f, 0.92f, 0.45f, 1f), 13, spriteMat);
                }

                // Vạt cúc trắng tinh khôi
                if (pfWildDaisy != null && (cx % 8.0f < 4.5f))
                {
                    SpawnPrefab(pfWildDaisy, $"Hill_WhiteDrift_{cx:F0}", sec4Group.transform,
                        new Vector3(cx + 0.7f, cy + 0.05f, 0.55f), 0.44f, false,
                        Color.white, 13, spriteMat);
                }

                // Thảm cỏ nền xanh mướt
                if (pfGrassShort != null)
                {
                    SpawnPrefab(pfGrassShort, $"Hill_GrassBed_{cx:F0}", sec4Group.transform,
                        new Vector3(cx, cy + 0.02f, 0.20f), 0.38f, false,
                        new Color(0.88f, 0.92f, 0.68f, 1f), 12, spriteMat);
                }

                // Cỏ lau trắng vươn cao đón gió ở mép sau (Z = 1.2m .. 1.8m)
                if (pfReeds != null)
                {
                    SpawnPrefab(pfReeds, $"Hill_WhiteWindReeds_{cx:F0}", sec4Group.transform,
                        new Vector3(cx + 0.15f, cy + 0.12f, 1.45f), 0.65f, (cx % 3f > 1.5f),
                        new Color(0.92f, 0.90f, 0.75f, 1f), 11, spriteMat, 1.1f, 0.8f);
                }
            }

            // =========================================================================
            // PHÂN ĐOẠN 5: LÀNG RẠCH GỐC & ĐƯỜNG DẪN XUỐNG RUỘNG LÚA (X = 95m .. 138m)
            // Làng quê thanh bình nương bóng tre, bờ chuối và lối đá cuội nối mượt vào đồng lúa 165m
            // =========================================================================
            var sec5Group = new GameObject("05_VillageRuralPath_Cinematic");
            sec5Group.transform.SetParent(previewRoot.transform, false);

            if (pfBamboo != null)
            {
                // Rặng tre làng râm mát phủ bóng sau các mái nhà tranh
                SpawnPrefab(pfBamboo, "Village_Backdrop_TreLang_1", sec5Group.transform,
                    new Vector3(104.0f, 0.40f, 5.8f), 1.35f, false,
                    new Color(0.50f, 0.65f, 0.46f, 1f), 5, spriteMat, 0.6f, 0.5f);
                SpawnPrefab(pfBamboo, "Village_Backdrop_TreLang_2", sec5Group.transform,
                    new Vector3(118.0f, 0.35f, 5.5f), 1.25f, true,
                    new Color(0.52f, 0.66f, 0.48f, 1f), 5, spriteMat, 0.5f, 0.6f);
            }

            // Bờ chuối quê sum suê rủ bóng dọc lối đi ra ruộng lúa (X = 132m .. 137m)
            if (pfBanana != null)
            {
                SpawnPrefab(pfBanana, "Village_Exit_BoChuoi_Tall", sec5Group.transform,
                    new Vector3(133.5f, -1.60f, 2.4f), 1.15f, false,
                    new Color(0.56f, 0.70f, 0.48f, 1f), 11, spriteMat, 1.0f, 0.8f);
                SpawnPrefab(pfBanana, "Village_Exit_BoChuoi_Low", sec5Group.transform,
                    new Vector3(135.8f, -1.72f, 1.9f), 0.85f, true,
                    new Color(0.58f, 0.72f, 0.50f, 1f), 11, spriteMat, 1.1f, 0.9f);
            }

            // Lối đi lát đá cuội mộc mạc dẫn từ cổng làng xuống thềm chòi canh 138m
            if (pfRockSlab != null)
            {
                SpawnPrefab(pfRockSlab, "Village_Exit_SteppingStone_1", sec5Group.transform,
                    new Vector3(131.5f, -1.75f, 0.25f), 0.50f, false,
                    new Color(0.70f, 0.66f, 0.58f, 1f), 12, spriteMat);
                SpawnPrefab(pfRockSlab, "Village_Exit_SteppingStone_2", sec5Group.transform,
                    new Vector3(134.5f, -1.82f, 0.20f), 0.48f, false,
                    new Color(0.68f, 0.64f, 0.56f, 1f), 12, spriteMat);
                SpawnPrefab(pfRockSlab, "Village_Exit_SteppingStone_3", sec5Group.transform,
                    new Vector3(137.2f, -1.85f, 0.20f), 0.45f, false,
                    new Color(0.65f, 0.62f, 0.55f, 1f), 12, spriteMat);
            }

            // 3. CHỤP LẠI 5 ẢNH PREVIEW VERIFICATION 720P JPG
            CaptureCinematicPreviews();

            Debug.Log("<color=#10b981>[PreviewPreRiceField V2]</color> HOÀN THÀNH DỰNG BẢN ĐIỆN ẢNH V2 TỪ ASSET THẬT!");
        }

        [MenuItem("Hoa va Mau/Level Design/Revert Pre-Rice Field Redesign Preview", false, 2)]
        public static void RevertPreview()
        {
            GameObject previewRoot = GameObject.Find(PreviewRootName);
            if (previewRoot != null)
            {
                Undo.DestroyObjectImmediate(previewRoot);
                Debug.Log("<color=#eab308>[PreviewPreRiceField V2]</color> Đã gỡ bỏ container thử nghiệm.");
            }
            ToggleOldHillRice(true);
        }

        private static void ToggleOldHillRice(bool active)
        {
            var eco = GameObject.Find("Lana_HandDrawn_Ecosystem");
            if (eco != null)
            {
                var rice2D = eco.transform.Find("Golden_RiceField_2D");
                if (rice2D != null)
                {
                    Undo.RecordObject(rice2D.gameObject, "Toggle Old Hill Rice");
                    rice2D.gameObject.SetActive(active);
                }
            }
        }

        private static float GetHillHeight(float x)
        {
            if (x < 65.0f)
            {
                float t = Mathf.InverseLerp(36.0f, 65.0f, x);
                return Mathf.Lerp(1.20f, 2.19f, t);
            }
            else
            {
                float t = Mathf.InverseLerp(65.0f, 95.0f, x);
                return Mathf.Lerp(2.19f, 0.45f, t);
            }
        }

        public static void CaptureCinematicPreviews()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                var cams = Object.FindObjectsByType<Camera>();
                if (cams.Length > 0) mainCam = cams[0];
            }

            if (mainCam == null) return;

            int width = 1280;
            int height = 720;

            Vector3 originalPos = mainCam.transform.position;
            Quaternion originalRot = mainCam.transform.rotation;

            RenderTexture rt = new RenderTexture(width, height, 24);
            RenderTexture prevRT = RenderTexture.active;
            mainCam.targetTexture = rt;

            var shots = new (string name, Vector3 pos)[] {
                ("v2_sec1_forest_canopy_cinematic.jpg", new Vector3(-2.0f, 0.40f, -15.5f)),
                ("v2_sec2_stepped_chasm_cinematic.jpg", new Vector3(14.2f, -1.15f, -15.5f)),
                ("v2_sec3_mossy_canyon_crate_cinematic.jpg", new Vector3(24.5f, 0.15f, -15.5f)),
                ("v2_sec4_grand_overlook_wildflower_hill_cinematic.jpg", new Vector3(64.5f, 2.35f, -15.5f)),
                ("v2_sec5_village_stepping_stones_to_rice_cinematic.jpg", new Vector3(124.0f, 0.25f, -15.5f))
            };

            foreach (var s in shots)
            {
                mainCam.transform.position = s.pos;
                mainCam.transform.rotation = Quaternion.identity;
                mainCam.Render();

                RenderTexture.active = rt;
                Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();

                byte[] bytes = tex.EncodeToJPG(80);
                string outPath = Path.Combine(ArtifactDir, s.name);
                File.WriteAllBytes(outPath, bytes);
                Object.DestroyImmediate(tex);
                Debug.Log($"<color=#10b981>[PreviewPreRiceField V2]</color> Đã xuất ảnh điện ảnh: {s.name}");
            }

            mainCam.targetTexture = null;
            RenderTexture.active = prevRT;
            rt.Release();
            Object.DestroyImmediate(rt);

            mainCam.transform.position = originalPos;
            mainCam.transform.rotation = originalRot;
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

            var renderers = go.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                r.sortingOrder = sortOrder;
                if (r is SpriteRenderer sr)
                {
                    if (mat != null) sr.sharedMaterial = mat;
                    sr.color = tintColor;
                }
                else if (r is MeshRenderer mr)
                {
                    if (mr.sharedMaterial != null)
                    {
                        var instMat = new Material(mr.sharedMaterial);
                        if (instMat.HasProperty("_BaseColor")) instMat.SetColor("_BaseColor", tintColor);
                        else if (instMat.HasProperty("_Color")) instMat.SetColor("_Color", tintColor);
                        mr.material = instMat;
                    }
                }
            }

            if (swayAngle > 0.05f)
            {
                var sway = go.AddComponent<PlanetOfLanaFoliageSway>();
                ConfigureSway(sway, swayAngle, swaySpeed, 0.15f);
            }

            return go;
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
    }
}
#endif
