#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace HoaVaMau.EditorTools
{
    /// <summary>
    /// Script tạo các phương án hình ảnh Preview nghệ thuật cho Cảnh Rừng Đầu Tiên (Section 1: Forest Clearing)
    /// CHỈ XUẤT HÌNH ẢNH (720P JPG), TUYỆT ĐỐI KHÔNG GHI ĐÈ / LÀM THAY ĐỔI SCENE HIỆN CÓ!
    /// 
    /// Tiêu chuẩn nghệ thuật:
    /// 1. "chia layer rõ ràng: cây nào đậm thì phía sau, nhạt thì phía trước"
    /// 2. "thêm nhiều cái khác nữa nhưng đừng có là thay tất cả vào cùng một chỗ chứ dàn trải ra đều"
    /// 3. Phân bổ đồng đều từ bờ dốc trái (X = -8.5m) -> khoảng rừng giữa (X = -1m) -> chân vách đá phải (X = +5.5m).
    /// </summary>
    public static class GenerateForestLayeringPreviews
    {
        private const string ArtifactDir = @"C:\Users\Thanh Tài\.gemini\antigravity\brain\8f0de94b-4312-45db-8a61-1ed2ab228bb4";
        private const string PackTreesDir = "Assets/Textures/Environment/Lana_VN_Pack/Trees";
        private const string PackBushesDir = "Assets/Textures/Environment/Lana_VN_Pack/Bushes";
        private const string UnifiedTreesDir = "Assets/Textures/Lana_Unified_Art/Trees";
        private const string UnifiedBushesDir = "Assets/Textures/Lana_Unified_Art/Bushes";
        private const string UnifiedFoliageDir = "Assets/Textures/Lana_Unified_Art/Foliage";
        private const string ConceptWildflowersDir = "Assets/Textures/Environment/Lana_Concept_Assets/Wildflowers";
        private const string ConceptGrassDir = "Assets/Textures/Environment/Lana_Concept_Assets/Grass";
        private const string ConceptRocksDir = "Assets/Textures/Environment/Lana_Concept_Assets/Rocks";

        [MenuItem("Hoa va Mau/Level Design/Generate Forest Layering Previews (Images Only)", false, 0)]
        public static void GenerateAllOptions()
        {
            Debug.Log("<color=#3b82f6>[ForestLayeringPreviews]</color> Bắt đầu xuất 3 phương án hình ảnh dàn trải đều...");

            // 1. Tạm thời ẩn các cây và bụi cũ ở Section 1 (-12m đến +5.5m)
            var hiddenList = new List<GameObject>();
            var allSrs = UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude);
            foreach (var sr in allSrs)
            {
                if (sr.transform.position.x >= -12f && sr.transform.position.x <= 5.5f)
                {
                    string pName = sr.transform.parent != null ? sr.transform.parent.name : "";
                    if (pName.Contains("HeroTrees") || pName.Contains("Bushes") || pName.Contains("MeadowGrass"))
                    {
                        sr.gameObject.SetActive(false);
                        hiddenList.Add(sr.gameObject);
                    }
                }
            }

            // Chuẩn bị vật liệu SpriteLit
            Material spriteMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/LanaOverhaul/M_Lana_SpriteDefault.mat");
            if (spriteMat == null)
            {
                var s = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default") ?? Shader.Find("Sprites/Default");
                spriteMat = new Material(s);
            }

            // Tải Sprites đa dạng chính thức từ Project Asset
            Sprite sprBigCanopy     = AssetDatabase.LoadAssetAtPath<Sprite>($"{PackTreesDir}/T_LanaVN_Tree_01_LargeCanopy.png");
            Sprite sprMedCanopy     = AssetDatabase.LoadAssetAtPath<Sprite>($"{PackTreesDir}/T_LanaVN_Tree_02_Medium.png");
            Sprite sprSlender       = AssetDatabase.LoadAssetAtPath<Sprite>($"{PackTreesDir}/T_LanaVN_Tree_04_SlenderTall.png");
            Sprite sprBanana        = AssetDatabase.LoadAssetAtPath<Sprite>($"{PackTreesDir}/T_LanaVN_Tree_05_Banana.png");
            Sprite sprBamboo        = AssetDatabase.LoadAssetAtPath<Sprite>($"{PackTreesDir}/T_LanaVN_Tree_07_BambooGrove.png");
            Sprite sprBanyan        = AssetDatabase.LoadAssetAtPath<Sprite>($"{UnifiedTreesDir}/T_Lana_Tree_01_HeroBanyan.png");
            Sprite sprCurvedTree    = AssetDatabase.LoadAssetAtPath<Sprite>($"{UnifiedTreesDir}/T_Lana_Tree_02_WindblownCurved.png");
            Sprite sprForestSlender = AssetDatabase.LoadAssetAtPath<Sprite>($"{UnifiedTreesDir}/T_Lana_Tree_03_SlenderForest.png");

            Sprite sprBushBroad     = AssetDatabase.LoadAssetAtPath<Sprite>($"{PackBushesDir}/T_LanaVN_Bush_Broadleaf_01.png");
            Sprite sprBushFlowerLow = AssetDatabase.LoadAssetAtPath<Sprite>($"{PackBushesDir}/T_LanaVN_Bush_Low_02_Flower.png");
            Sprite sprBushWide      = AssetDatabase.LoadAssetAtPath<Sprite>($"{UnifiedBushesDir}/T_Lana_Bush_02_WideCluster.png");
            Sprite sprBushLush      = AssetDatabase.LoadAssetAtPath<Sprite>($"{UnifiedBushesDir}/T_Lana_Bush_01_LushMound.png");
            Sprite sprBushBloom     = AssetDatabase.LoadAssetAtPath<Sprite>($"{UnifiedBushesDir}/T_Lana_Bush_03_Flowering.png");

            Sprite sprDaisy         = AssetDatabase.LoadAssetAtPath<Sprite>($"{ConceptWildflowersDir}/T_HD_Wildflower_03_WhiteDaisy.png");
            Sprite sprReeds         = AssetDatabase.LoadAssetAtPath<Sprite>($"{ConceptWildflowersDir}/T_HD_Wildflower_02_Reeds.png");
            Sprite sprGrassTuft     = AssetDatabase.LoadAssetAtPath<Sprite>($"{UnifiedFoliageDir}/T_Lana_Grass_01_SoftTuft.png");
            Sprite sprGrassShort    = AssetDatabase.LoadAssetAtPath<Sprite>($"{ConceptGrassDir}/T_HD_GrassCarpet_01_Short.png");

            Sprite sprRockDome      = AssetDatabase.LoadAssetAtPath<Sprite>($"{ConceptRocksDir}/T_HD_Rock_02_MediumMossyDome.png");
            Sprite sprRockCorner    = AssetDatabase.LoadAssetAtPath<Sprite>($"{ConceptRocksDir}/T_HD_Rock_03_MediumMossyCorner.png");
            Sprite sprRockSlab      = AssetDatabase.LoadAssetAtPath<Sprite>($"{ConceptRocksDir}/T_HD_Rock_04_FlatSlab.png");
            Sprite sprRockSmall     = AssetDatabase.LoadAssetAtPath<Sprite>($"{ConceptRocksDir}/T_HD_Rock_05_SmallMossy.png");
            Sprite sprPebbles       = AssetDatabase.LoadAssetAtPath<Sprite>($"{ConceptRocksDir}/T_HD_Rock_06_PebbleCluster.png");

            try
            {
                // PHƯƠNG ÁN 1: NẮNG SỚM VEN RỪNG CHUỐI NAM BỘ (Bụi chuối bờ dốc, thảm cúc trắng, rặng tre chân vách)
                BuildOption1_BananaAndClearing(
                    "phuong_an_1_rung_nang_som_tuong_phan_manh.jpg",
                    sprBigCanopy, sprMedCanopy, sprSlender, sprBanana, sprBamboo, sprBanyan, sprCurvedTree, sprForestSlender,
                    sprBushBroad, sprBushFlowerLow, sprBushWide, sprBushLush, sprBushBloom,
                    sprDaisy, sprReeds, sprGrassTuft, sprGrassShort,
                    sprRockDome, sprRockCorner, sprRockSlab, sprRockSmall, sprPebbles, spriteMat);

                // PHƯƠNG ÁN 2: CHIỀU SÂU ĐIỆN ẢNH RỪNG GIÀ & TRE TRÚC (Banyan đại thụ, rặng tre bạt ngàn, đá bậc thang)
                BuildOption2_BanyanAndBamboo(
                    "phuong_an_2_chieu_sau_dien_anh_3_tang.jpg",
                    sprBigCanopy, sprMedCanopy, sprSlender, sprBanana, sprBamboo, sprBanyan, sprCurvedTree, sprForestSlender,
                    sprBushBroad, sprBushFlowerLow, sprBushWide, sprBushLush, sprBushBloom,
                    sprDaisy, sprReeds, sprGrassTuft, sprGrassShort,
                    sprRockDome, sprRockCorner, sprRockSlab, sprRockSmall, sprPebbles, spriteMat);

                // PHƯƠNG ÁN 3: THUNG LŨNG HOA DẠI & SUỐI ĐÁ RÊU PHONG (Hoa cúc trắng nở rộ, cỏ mềm đung đưa, đá rêu dịu dàng)
                BuildOption3_WildflowerMeadow(
                    "phuong_an_3_mau_nuoc_nam_bo_diu_dang.jpg",
                    sprBigCanopy, sprMedCanopy, sprSlender, sprBanana, sprBamboo, sprBanyan, sprCurvedTree, sprForestSlender,
                    sprBushBroad, sprBushFlowerLow, sprBushWide, sprBushLush, sprBushBloom,
                    sprDaisy, sprReeds, sprGrassTuft, sprGrassShort,
                    sprRockDome, sprRockCorner, sprRockSlab, sprRockSmall, sprPebbles, spriteMat);

                Debug.Log("<color=#10b981>[ForestLayeringPreviews]</color> ĐÃ XUẤT THÀNH CÔNG 3 PHƯƠNG ÁN HÌNH ẢNH DÀN TRẢI ĐỀU 720P!");
            }
            finally
            {
                // Khôi phục trạng thái ban đầu của Scene 100%
                foreach (var go in hiddenList)
                {
                    if (go != null) go.SetActive(true);
                }
            }
        }

        // =========================================================================================
        // PHƯƠNG ÁN 1: NẮNG SỚM VEN RỪNG CHUỐI NAM BỘ (Rừng chuối bên dốc, thảm hoa cúc trắng, rặng trúc chân vách)
        // =========================================================================================
        private static void BuildOption1_BananaAndClearing(
            string fileName,
            Sprite sprBigCanopy, Sprite sprMedCanopy, Sprite sprSlender, Sprite sprBanana, Sprite sprBamboo,
            Sprite sprBanyan, Sprite sprCurvedTree, Sprite sprForestSlender,
            Sprite sprBushBroad, Sprite sprBushFlowerLow, Sprite sprBushWide, Sprite sprBushLush, Sprite sprBushBloom,
            Sprite sprDaisy, Sprite sprReeds, Sprite sprGrassTuft, Sprite sprGrassShort,
            Sprite sprRockDome, Sprite sprRockCorner, Sprite sprRockSlab, Sprite sprRockSmall, Sprite sprPebbles,
            Material mat)
        {
            var container = new GameObject("__TEMP_FOREST_LAYER_PREVIEW__");
            try
            {
                Color bgWallColor = new Color(0.12f, 0.18f, 0.11f, 1.0f); // Xanh rừng già sẫm
                Color bgTreeColor = new Color(0.18f, 0.26f, 0.15f, 1.0f); // Cổ thụ tối màu
                Color fgLightColor= new Color(0.86f, 0.95f, 0.72f, 1.0f); // Xanh non nhạt đón nắng
                Color fgSunColor  = new Color(0.90f, 0.97f, 0.75f, 1.0f); // Vàng non rực rỡ
                Color rockColor   = new Color(0.70f, 0.74f, 0.66f, 1.0f); // Đá rêu xám tự nhiên

                // 1. HẬU CẢNH RỪNG GIÀ SẪM CHE KÍN ĐƯỜNG CHÂN TRỜI (X = -10m đến +7.5m, Z = 5.5m .. 7.5m)
                SpawnSprite(container.transform, "BG_Canopy1", sprBigCanopy, new Vector3(-8.8f, 0.8f, 7.5f), new Vector2(2.1f, 2.1f), bgWallColor, 2, mat);
                SpawnSprite(container.transform, "BG_Canopy2", sprBigCanopy, new Vector3(-4.5f, 0.5f, 7.2f), new Vector2(2.1f, 2.1f), bgWallColor, 2, mat, true);
                SpawnSprite(container.transform, "BG_Canopy3", sprBigCanopy, new Vector3(0.0f, 0.6f, 7.4f), new Vector2(2.2f, 2.2f), bgWallColor, 2, mat);
                SpawnSprite(container.transform, "BG_Canopy4", sprBigCanopy, new Vector3(4.8f, 0.8f, 7.6f), new Vector2(2.1f, 2.1f), bgWallColor, 2, mat, true);

                SpawnSprite(container.transform, "BG_Foliage1", sprBushWide ?? sprBushBroad, new Vector3(-8.5f, -1.4f, 6.2f), new Vector2(2.7f, 2.3f), bgWallColor, 3, mat);
                SpawnSprite(container.transform, "BG_Foliage2", sprBushWide ?? sprBushBroad, new Vector3(-4.0f, -1.8f, 6.0f), new Vector2(2.8f, 2.4f), bgWallColor, 3, mat);
                SpawnSprite(container.transform, "BG_Foliage3", sprBushWide ?? sprBushBroad, new Vector3(0.5f, -1.8f, 6.0f), new Vector2(2.7f, 2.3f), bgWallColor, 3, mat);
                SpawnSprite(container.transform, "BG_Foliage4", sprBushWide ?? sprBushBroad, new Vector3(5.2f, -1.5f, 6.2f), new Vector2(2.7f, 2.3f), bgWallColor, 3, mat);

                // Cây cổ thụ & rặng tre hậu cảnh
                SpawnSprite(container.transform, "BG_Banyan_Left", sprBanyan, new Vector3(-7.5f, -1.15f, 4.8f), new Vector2(1.3f, 1.3f), bgTreeColor, 4, mat);
                SpawnSprite(container.transform, "BG_Bamboo_Mid", sprBamboo, new Vector3(-3.8f, -2.10f, 4.6f), new Vector2(1.35f, 1.35f), bgTreeColor, 4, mat);
                SpawnSprite(container.transform, "BG_Tree_Right", sprBamboo, new Vector3(3.5f, -2.10f, 4.7f), new Vector2(1.35f, 1.35f), bgTreeColor, 4, mat, true);
                SpawnSprite(container.transform, "BG_MedTree_Cliff", sprMedCanopy, new Vector3(5.8f, -1.80f, 4.5f), new Vector2(1.3f, 1.3f), bgTreeColor, 4, mat);

                // 2. TIỀN CẢNH DÀN TRẢI ĐỀU TỪ BỜ DỐC TRÁI ĐẾN CHÂN VÁCH PHẢI
                // Zone 1: Bờ Dốc (X = -8.5m đến -5.0m)
                SpawnSprite(container.transform, "Z1_Banana_Hero", sprBanana, new Vector3(-7.6f, -0.95f, 1.5f), new Vector2(1.15f, 1.15f), fgLightColor, 13, mat);
                SpawnSprite(container.transform, "Z1_Rock_Corner", sprRockCorner ?? sprRockDome, new Vector3(-6.5f, -1.25f, 0.8f), new Vector2(0.55f, 0.55f), rockColor, 14, mat);
                SpawnSprite(container.transform, "Z1_Bush_Flower", sprBushFlowerLow ?? sprBushBroad, new Vector3(-7.0f, -1.05f, 0.6f), new Vector2(0.65f, 0.65f), fgSunColor, 15, mat);
                SpawnSprite(container.transform, "Z1_Daisy_Slope", sprDaisy, new Vector3(-5.8f, -1.45f, 0.3f), new Vector2(0.55f, 0.55f), Color.white, 16, mat);
                SpawnSprite(container.transform, "Z1_Grass", sprGrassTuft ?? sprGrassShort, new Vector3(-7.2f, -0.95f, 0.2f), new Vector2(0.60f, 0.60f), fgSunColor, 16, mat);

                // Zone 2: Chân Dốc Chuyển Tiếp (X = -5.0m đến -1.8m)
                SpawnSprite(container.transform, "Z2_Slender_Tree", sprSlender, new Vector3(-4.2f, -1.95f, 1.2f), new Vector2(1.0f, 1.0f), fgLightColor, 14, mat);
                SpawnSprite(container.transform, "Z2_Bush_Mound", sprBushLush ?? sprBushBroad, new Vector3(-4.6f, -1.95f, 0.7f), new Vector2(0.70f, 0.70f), fgLightColor, 15, mat);
                SpawnSprite(container.transform, "Z2_Rock_Dome", sprRockDome, new Vector3(-3.4f, -2.10f, 0.6f), new Vector2(0.50f, 0.50f), rockColor, 14, mat);
                SpawnSprite(container.transform, "Z2_Daisy_Group", sprDaisy, new Vector3(-2.9f, -1.95f, 0.3f), new Vector2(0.55f, 0.55f), Color.white, 16, mat);
                SpawnSprite(container.transform, "Z2_Grass", sprGrassShort, new Vector3(-3.8f, -1.95f, 0.2f), new Vector2(0.65f, 0.65f), fgSunColor, 16, mat);

                // Zone 3: Khoảng Rừng Thưa Trung Tâm (X = -1.8m đến +1.8m)
                SpawnSprite(container.transform, "Z3_Sunlit_Slender", sprSlender, new Vector3(-0.4f, -1.95f, 0.9f), new Vector2(1.08f, 1.08f), fgSunColor, 14, mat);
                SpawnSprite(container.transform, "Z3_Bush_Bloom", sprBushBloom ?? sprBushFlowerLow, new Vector3(0.5f, -1.95f, 0.6f), new Vector2(0.65f, 0.65f), fgSunColor, 15, mat);
                SpawnSprite(container.transform, "Z3_Daisy_Center", sprDaisy, new Vector3(-1.3f, -1.95f, 0.3f), new Vector2(0.58f, 0.58f), Color.white, 16, mat);
                SpawnSprite(container.transform, "Z3_Daisy_Right", sprDaisy, new Vector3(1.2f, -1.95f, 0.3f), new Vector2(0.55f, 0.55f), Color.white, 16, mat);
                SpawnSprite(container.transform, "Z3_Pebble", sprPebbles ?? sprRockSmall, new Vector3(-0.2f, -2.05f, 0.2f), new Vector2(0.40f, 0.40f), rockColor, 16, mat);

                // Zone 4: Lối Dẫn Lên Chân Vách Đá (X = +1.8m đến +5.8m)
                SpawnSprite(container.transform, "Z4_Tree_MedRight", sprMedCanopy, new Vector3(2.5f, -1.95f, 1.1f), new Vector2(0.95f, 0.95f), new Color(0.42f, 0.54f, 0.36f), 14, mat);
                SpawnSprite(container.transform, "Z4_Reeds_Tall", sprReeds, new Vector3(2.0f, -1.95f, 0.5f), new Vector2(0.65f, 0.65f), fgSunColor, 15, mat);
                SpawnSprite(container.transform, "Z4_Rock_Slab", sprRockSlab ?? sprRockDome, new Vector3(3.2f, -2.05f, 0.4f), new Vector2(0.55f, 0.55f), rockColor, 15, mat);
                SpawnSprite(container.transform, "Z4_Reeds_Cliff", sprReeds, new Vector3(3.8f, -1.95f, 0.5f), new Vector2(0.60f, 0.60f), fgSunColor, 15, mat);
                SpawnSprite(container.transform, "Z4_Banana_Cliff", sprBanana, new Vector3(4.8f, -1.55f, 1.2f), new Vector2(0.85f, 0.85f), fgLightColor, 14, mat);
                SpawnSprite(container.transform, "Z4_Bush_Corner", sprBushBroad, new Vector3(4.3f, -1.85f, 0.6f), new Vector2(0.65f, 0.65f), fgLightColor, 15, mat);

                // Vòm lá viền góc trên
                SpawnSprite(container.transform, "Vignette_TopLeft", sprBigCanopy, new Vector3(-6.2f, 5.2f, -1.8f), new Vector2(1.8f, 1.8f), new Color(0.22f, 0.30f, 0.18f, 0.95f), 28, mat);

                CaptureForestViewWide(Path.Combine(ArtifactDir, fileName));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(container);
            }
        }

        // =========================================================================================
        // PHƯƠNG ÁN 2: CHIỀU SÂU ĐIỆN ẢNH RỪNG GIÀ & TRE TRÚC (Banyan đại thụ, rặng tre bạt ngàn, đá bậc thang)
        // =========================================================================================
        private static void BuildOption2_BanyanAndBamboo(
            string fileName,
            Sprite sprBigCanopy, Sprite sprMedCanopy, Sprite sprSlender, Sprite sprBanana, Sprite sprBamboo,
            Sprite sprBanyan, Sprite sprCurvedTree, Sprite sprForestSlender,
            Sprite sprBushBroad, Sprite sprBushFlowerLow, Sprite sprBushWide, Sprite sprBushLush, Sprite sprBushBloom,
            Sprite sprDaisy, Sprite sprReeds, Sprite sprGrassTuft, Sprite sprGrassShort,
            Sprite sprRockDome, Sprite sprRockCorner, Sprite sprRockSlab, Sprite sprRockSmall, Sprite sprPebbles,
            Material mat)
        {
            var container = new GameObject("__TEMP_FOREST_LAYER_PREVIEW__");
            try
            {
                Color bgWallColor = new Color(0.10f, 0.18f, 0.15f, 1.0f); // Xanh chàm rừng sâu
                Color bgTreeColor = new Color(0.15f, 0.23f, 0.17f, 1.0f); // Hậu cảnh tối
                Color midTreeColor= new Color(0.42f, 0.54f, 0.35f, 1.0f); // Trung cảnh râm mát
                Color fgLightColor= new Color(0.92f, 0.97f, 0.72f, 1.0f); // Tiền cảnh vàng non
                Color fgSunColor  = new Color(0.95f, 0.98f, 0.78f, 1.0f);
                Color rockColor   = new Color(0.66f, 0.72f, 0.68f, 1.0f);

                // 1. TƯỜNG RỪNG HẬU CẢNH BẠT NGÀN PHỦ KÍN
                SpawnSprite(container.transform, "BG2_Canopy1", sprBigCanopy, new Vector3(-8.8f, 0.8f, 7.5f), new Vector2(2.1f, 2.1f), bgWallColor, 2, mat);
                SpawnSprite(container.transform, "BG2_Canopy2", sprBigCanopy, new Vector3(-4.2f, 0.5f, 7.2f), new Vector2(2.1f, 2.1f), bgWallColor, 2, mat, true);
                SpawnSprite(container.transform, "BG2_Canopy3", sprBigCanopy, new Vector3(0.5f, 0.6f, 7.4f), new Vector2(2.2f, 2.2f), bgWallColor, 2, mat);
                SpawnSprite(container.transform, "BG2_Canopy4", sprBigCanopy, new Vector3(5.2f, 0.8f, 7.6f), new Vector2(2.1f, 2.1f), bgWallColor, 2, mat, true);

                SpawnSprite(container.transform, "BG2_Foliage1", sprBushWide ?? sprBushBroad, new Vector3(-8.5f, -1.4f, 6.2f), new Vector2(2.7f, 2.3f), bgWallColor, 3, mat);
                SpawnSprite(container.transform, "BG2_Foliage2", sprBushWide ?? sprBushBroad, new Vector3(-3.8f, -1.8f, 6.0f), new Vector2(2.8f, 2.4f), bgWallColor, 3, mat);
                SpawnSprite(container.transform, "BG2_Foliage3", sprBushWide ?? sprBushBroad, new Vector3(1.2f, -1.8f, 6.0f), new Vector2(2.7f, 2.3f), bgWallColor, 3, mat);
                SpawnSprite(container.transform, "BG2_Foliage4", sprBushWide ?? sprBushBroad, new Vector3(5.5f, -1.5f, 6.2f), new Vector2(2.7f, 2.3f), bgWallColor, 3, mat);

                // Rừng tre trúc và Banyan đại thụ xen kẽ hậu cảnh
                SpawnSprite(container.transform, "BG2_Bamboo_Left", sprBamboo, new Vector3(-7.2f, -1.15f, 4.8f), new Vector2(1.35f, 1.35f), bgTreeColor, 4, mat);
                SpawnSprite(container.transform, "BG2_Banyan_MidLeft", sprBanyan, new Vector3(-3.5f, -2.10f, 4.6f), new Vector2(1.35f, 1.35f), bgTreeColor, 4, mat);
                SpawnSprite(container.transform, "BG2_Bamboo_Center", sprBamboo, new Vector3(0.8f, -2.10f, 4.8f), new Vector2(1.35f, 1.35f), bgTreeColor, 4, mat, true);
                SpawnSprite(container.transform, "BG2_Bamboo_Right", sprBamboo, new Vector3(4.2f, -2.10f, 4.7f), new Vector2(1.35f, 1.35f), bgTreeColor, 4, mat);

                // 2. TIỀN CẢNH DÀN TRẢI ĐỀU THEO PHONG CÁCH RỪNG GIÀ ĐIỆN ẢNH
                // Zone 1: Bờ dốc trái - Cây uốn lượn cổ thụ & đá tảng
                SpawnSprite(container.transform, "Z1_CurvedTree", sprCurvedTree ?? sprSlender, new Vector3(-7.5f, -0.95f, 1.5f), new Vector2(1.1f, 1.1f), fgLightColor, 13, mat);
                SpawnSprite(container.transform, "Z1_Rock_Large", sprRockDome, new Vector3(-6.2f, -1.25f, 0.8f), new Vector2(0.65f, 0.65f), rockColor, 14, mat);
                SpawnSprite(container.transform, "Z1_Bush_Lush", sprBushLush ?? sprBushBroad, new Vector3(-6.8f, -1.05f, 0.6f), new Vector2(0.70f, 0.70f), fgLightColor, 15, mat);
                SpawnSprite(container.transform, "Z1_FernGrass", sprGrassTuft, new Vector3(-5.5f, -1.45f, 0.3f), new Vector2(0.65f, 0.65f), fgSunColor, 16, mat);

                // Zone 2: Chân dốc - Thảm hoa cúc trắng & phiến đá
                SpawnSprite(container.transform, "Z2_MedTree", sprMedCanopy, new Vector3(-4.0f, -1.95f, 1.2f), new Vector2(1.05f, 1.05f), midTreeColor, 14, mat);
                SpawnSprite(container.transform, "Z2_Rock_Slab", sprRockSlab ?? sprRockDome, new Vector3(-3.2f, -2.05f, 0.5f), new Vector2(0.55f, 0.55f), rockColor, 15, mat);
                SpawnSprite(container.transform, "Z2_Daisy_Cluster", sprDaisy, new Vector3(-2.6f, -1.95f, 0.3f), new Vector2(0.60f, 0.60f), Color.white, 16, mat);
                SpawnSprite(container.transform, "Z2_Grass", sprGrassShort, new Vector3(-3.6f, -1.95f, 0.2f), new Vector2(0.65f, 0.65f), fgSunColor, 16, mat);

                // Zone 3: Khoảng rừng thưa trung tâm - Cây thân mảnh thanh thoát đón nắng
                SpawnSprite(container.transform, "Z3_Slender_Center", sprSlender, new Vector3(-0.8f, -1.95f, 0.9f), new Vector2(1.10f, 1.10f), fgSunColor, 14, mat);
                SpawnSprite(container.transform, "Z3_Bush_Bloom", sprBushBloom ?? sprBushFlowerLow, new Vector3(-0.1f, -1.95f, 0.6f), new Vector2(0.65f, 0.65f), fgSunColor, 15, mat);
                SpawnSprite(container.transform, "Z3_Daisy_Center", sprDaisy, new Vector3(0.6f, -1.95f, 0.3f), new Vector2(0.58f, 0.58f), Color.white, 16, mat);
                SpawnSprite(container.transform, "Z3_Pebbles", sprPebbles ?? sprRockSmall, new Vector3(1.2f, -2.05f, 0.2f), new Vector2(0.45f, 0.45f), rockColor, 16, mat);

                // Zone 4: Lối dẫn lên chân vách phải - Cụm lau sậy cao & phiến đá bậc thang
                SpawnSprite(container.transform, "Z4_Slender_Right", sprSlender, new Vector3(2.2f, -1.95f, 1.1f), new Vector2(0.95f, 0.95f), fgLightColor, 14, mat);
                SpawnSprite(container.transform, "Z4_Reeds_Tall1", sprReeds, new Vector3(1.8f, -1.95f, 0.5f), new Vector2(0.65f, 0.65f), fgSunColor, 15, mat);
                SpawnSprite(container.transform, "Z4_Rock_Steps", sprRockSlab ?? sprRockDome, new Vector3(3.0f, -2.05f, 0.4f), new Vector2(0.60f, 0.60f), rockColor, 15, mat);
                SpawnSprite(container.transform, "Z4_Reeds_Tall2", sprReeds, new Vector3(3.8f, -1.95f, 0.5f), new Vector2(0.65f, 0.65f), fgSunColor, 15, mat);
                SpawnSprite(container.transform, "Z4_Bush_Cliff", sprBushBroad, new Vector3(4.8f, -1.75f, 0.6f), new Vector2(0.70f, 0.70f), midTreeColor, 15, mat);
                SpawnSprite(container.transform, "Z4_Rock_SmallCliff", sprRockSmall ?? sprRockDome, new Vector3(5.3f, -1.30f, 0.3f), new Vector2(0.40f, 0.40f), rockColor, 16, mat);

                // Vòm lá viền góc trên
                SpawnSprite(container.transform, "Vignette_TopLeft", sprBigCanopy, new Vector3(-6.2f, 5.2f, -1.8f), new Vector2(1.8f, 1.8f), new Color(0.20f, 0.28f, 0.18f, 0.95f), 28, mat);

                CaptureForestViewWide(Path.Combine(ArtifactDir, fileName));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(container);
            }
        }

        // =========================================================================================
        // PHƯƠNG ÁN 3: THUNG LŨNG HOA DẠI & SUỐI ĐÁ RÊU PHONG (Hoa cúc trắng nở rộ, cỏ mềm đung đưa, đá rêu dịu dàng)
        // =========================================================================================
        private static void BuildOption3_WildflowerMeadow(
            string fileName,
            Sprite sprBigCanopy, Sprite sprMedCanopy, Sprite sprSlender, Sprite sprBanana, Sprite sprBamboo,
            Sprite sprBanyan, Sprite sprCurvedTree, Sprite sprForestSlender,
            Sprite sprBushBroad, Sprite sprBushFlowerLow, Sprite sprBushWide, Sprite sprBushLush, Sprite sprBushBloom,
            Sprite sprDaisy, Sprite sprReeds, Sprite sprGrassTuft, Sprite sprGrassShort,
            Sprite sprRockDome, Sprite sprRockCorner, Sprite sprRockSlab, Sprite sprRockSmall, Sprite sprPebbles,
            Material mat)
        {
            var container = new GameObject("__TEMP_FOREST_LAYER_PREVIEW__");
            try
            {
                Color bgWallColor = new Color(0.18f, 0.26f, 0.16f, 1.0f); // Xanh rêu cổ kính
                Color bgTreeColor = new Color(0.22f, 0.30f, 0.18f, 1.0f);
                Color fgLightColor= new Color(0.78f, 0.88f, 0.68f, 1.0f); // Xanh sage êm dịu
                Color fgSunColor  = new Color(0.86f, 0.93f, 0.74f, 1.0f);
                Color paleWhite   = new Color(0.96f, 0.95f, 0.88f, 1.0f); // Trắng kem ấm áp
                Color rockColor   = new Color(0.72f, 0.76f, 0.68f, 1.0f);

                // 1. TƯỜNG RÊU HẬU CẢNH ÊM DỊU
                SpawnSprite(container.transform, "BG3_Canopy1", sprBigCanopy, new Vector3(-8.8f, 0.8f, 7.5f), new Vector2(2.1f, 2.1f), bgWallColor, 2, mat);
                SpawnSprite(container.transform, "BG3_Canopy2", sprBigCanopy, new Vector3(-4.0f, 0.5f, 7.2f), new Vector2(2.1f, 2.1f), bgWallColor, 2, mat, true);
                SpawnSprite(container.transform, "BG3_Canopy3", sprBigCanopy, new Vector3(0.5f, 0.6f, 7.4f), new Vector2(2.2f, 2.2f), bgWallColor, 2, mat);
                SpawnSprite(container.transform, "BG3_Canopy4", sprBigCanopy, new Vector3(5.0f, 0.8f, 7.6f), new Vector2(2.1f, 2.1f), bgWallColor, 2, mat, true);

                SpawnSprite(container.transform, "BG3_Foliage1", sprBushWide ?? sprBushBroad, new Vector3(-8.5f, -1.4f, 6.2f), new Vector2(2.7f, 2.3f), bgWallColor, 3, mat);
                SpawnSprite(container.transform, "BG3_Foliage2", sprBushWide ?? sprBushBroad, new Vector3(-3.8f, -1.8f, 6.0f), new Vector2(2.8f, 2.4f), bgWallColor, 3, mat);
                SpawnSprite(container.transform, "BG3_Foliage3", sprBushWide ?? sprBushBroad, new Vector3(1.0f, -1.8f, 6.0f), new Vector2(2.7f, 2.3f), bgWallColor, 3, mat);
                SpawnSprite(container.transform, "BG3_Foliage4", sprBushWide ?? sprBushBroad, new Vector3(5.2f, -1.5f, 6.2f), new Vector2(2.7f, 2.3f), bgWallColor, 3, mat);

                // Cây cổ thụ râm mát hậu cảnh
                SpawnSprite(container.transform, "BG3_Banyan_Left", sprBanyan, new Vector3(-7.0f, -1.15f, 4.8f), new Vector2(1.3f, 1.3f), bgTreeColor, 4, mat);
                SpawnSprite(container.transform, "BG3_Bamboo_Mid", sprBamboo, new Vector3(-3.0f, -2.10f, 4.6f), new Vector2(1.35f, 1.35f), bgTreeColor, 4, mat);
                SpawnSprite(container.transform, "BG3_Tree_Right", sprMedCanopy, new Vector3(4.0f, -1.95f, 4.5f), new Vector2(1.3f, 1.3f), bgTreeColor, 4, mat);

                // 2. TIỀN CẢNH TRÀN NGẬP HOA DẠI VÀ ĐÁ RÊU ÊM DỊU
                // Zone 1: Bờ dốc trái - Hoa cúc dại trắng nở rộ khắp sườn dốc
                SpawnSprite(container.transform, "Z1_Daisy_High", sprDaisy, new Vector3(-7.8f, -0.95f, 0.3f), new Vector2(0.60f, 0.60f), paleWhite, 16, mat);
                SpawnSprite(container.transform, "Z1_Bush_Lush", sprBushLush ?? sprBushBroad, new Vector3(-7.2f, -1.05f, 0.6f), new Vector2(0.65f, 0.65f), fgLightColor, 15, mat);
                SpawnSprite(container.transform, "Z1_Rock_Corner", sprRockCorner ?? sprRockDome, new Vector3(-6.2f, -1.25f, 0.8f), new Vector2(0.55f, 0.55f), rockColor, 14, mat);
                SpawnSprite(container.transform, "Z1_Daisy_MidSlope", sprDaisy, new Vector3(-5.5f, -1.45f, 0.3f), new Vector2(0.55f, 0.55f), paleWhite, 16, mat);
                SpawnSprite(container.transform, "Z1_Grass", sprGrassShort, new Vector3(-6.6f, -1.15f, 0.2f), new Vector2(0.60f, 0.60f), fgSunColor, 16, mat);

                // Zone 2: Chân dốc - Cây sage non, hoa cúc và tảng đá phủ rêu
                SpawnSprite(container.transform, "Z2_Slender_Sage", sprSlender, new Vector3(-4.0f, -1.95f, 1.1f), new Vector2(1.0f, 1.0f), fgLightColor, 14, mat);
                SpawnSprite(container.transform, "Z2_Daisy_Group1", sprDaisy, new Vector3(-3.3f, -1.95f, 0.3f), new Vector2(0.60f, 0.60f), paleWhite, 16, mat);
                SpawnSprite(container.transform, "Z2_Rock_Dome", sprRockDome, new Vector3(-2.6f, -2.10f, 0.5f), new Vector2(0.50f, 0.50f), rockColor, 15, mat);
                SpawnSprite(container.transform, "Z2_Grass", sprGrassTuft, new Vector3(-2.1f, -1.95f, 0.2f), new Vector2(0.60f, 0.60f), fgSunColor, 16, mat);

                // Zone 3: Khoảng rừng thưa trung tâm - Thảm cỏ hoa êm đềm
                SpawnSprite(container.transform, "Z3_Slender_Center", sprForestSlender ?? sprSlender, new Vector3(-0.6f, -1.95f, 0.9f), new Vector2(1.05f, 1.05f), fgSunColor, 14, mat);
                SpawnSprite(container.transform, "Z3_Daisy_Center1", sprDaisy, new Vector3(-1.2f, -1.95f, 0.3f), new Vector2(0.60f, 0.60f), paleWhite, 16, mat);
                SpawnSprite(container.transform, "Z3_Bush_Bloom", sprBushBloom ?? sprBushFlowerLow, new Vector3(0.2f, -1.95f, 0.6f), new Vector2(0.65f, 0.65f), fgLightColor, 15, mat);
                SpawnSprite(container.transform, "Z3_Daisy_Center2", sprDaisy, new Vector3(0.9f, -1.95f, 0.3f), new Vector2(0.58f, 0.58f), paleWhite, 16, mat);
                SpawnSprite(container.transform, "Z3_Pebbles", sprPebbles ?? sprRockSmall, new Vector3(-0.1f, -2.05f, 0.2f), new Vector2(0.40f, 0.40f), rockColor, 16, mat);

                // Zone 4: Lối dẫn chân vách phải - Khóm hoa lau sậy và đá phẳng bậc thềm
                SpawnSprite(container.transform, "Z4_Tree_MedRight", sprMedCanopy, new Vector3(2.5f, -1.95f, 1.1f), new Vector2(0.95f, 0.95f), fgLightColor, 14, mat);
                SpawnSprite(container.transform, "Z4_Reeds1", sprReeds, new Vector3(1.8f, -1.95f, 0.5f), new Vector2(0.60f, 0.60f), fgSunColor, 15, mat);
                SpawnSprite(container.transform, "Z4_Daisy_Right", sprDaisy, new Vector3(2.4f, -1.95f, 0.3f), new Vector2(0.55f, 0.55f), paleWhite, 16, mat);
                SpawnSprite(container.transform, "Z4_Rock_Slab", sprRockSlab ?? sprRockDome, new Vector3(3.3f, -2.05f, 0.4f), new Vector2(0.55f, 0.55f), rockColor, 15, mat);
                SpawnSprite(container.transform, "Z4_Reeds2", sprReeds, new Vector3(4.0f, -1.95f, 0.5f), new Vector2(0.65f, 0.65f), fgSunColor, 15, mat);
                SpawnSprite(container.transform, "Z4_Rock_Small", sprRockSmall ?? sprRockDome, new Vector3(5.0f, -1.30f, 0.3f), new Vector2(0.40f, 0.40f), rockColor, 16, mat);

                // Vòm lá viền góc trên
                SpawnSprite(container.transform, "Vignette_TopLeft", sprBigCanopy, new Vector3(-6.2f, 5.2f, -1.8f), new Vector2(1.8f, 1.8f), new Color(0.22f, 0.30f, 0.18f, 0.95f), 28, mat);

                CaptureForestViewWide(Path.Combine(ArtifactDir, fileName));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(container);
            }
        }

        private static void SpawnSprite(
            Transform parent, string name, Sprite spr, Vector3 pos, Vector2 scale,
            Color color, int sortingOrder, Material mat, bool flipX = false)
        {
            if (spr == null) return;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(flipX ? -scale.x : scale.x, scale.y, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = spr;
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            sr.sharedMaterial = mat;
        }

        private static void CaptureForestViewWide(string outputPath)
        {
            var cam = Camera.main;
            if (cam == null) return;

            Vector3 oldPos = cam.transform.position;
            // Đặt camera góc rộng bao quát trọn vẹn toàn bộ Section 1 từ X = -8.5m đến +5.5m
            cam.transform.position = new Vector3(-1.8f, 0.45f, -16.5f);

            int width = 1280;
            int height = 720;
            var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var prevTarget = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            byte[] bytes = tex.EncodeToJPG(80);
            File.WriteAllBytes(outputPath, bytes);

            cam.targetTexture = prevTarget;
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);
            UnityEngine.Object.DestroyImmediate(tex);

            cam.transform.position = oldPos;
        }
    }
}
#endif
