#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace HoaVaMau.EditorTools
{
    /// <summary>
    /// Script tạo các phương án hình ảnh Preview nghệ thuật cho TOÀN BỘ KHU RỪNG (CẢ PHẦN ĐẦU VÀ PHẦN SAU)
    /// - PHẦN ĐẦU (X = -10.5m đến +15.0m): Rừng thưa chuyển tiếp, triền dốc & vách đá leo dây.
    /// - PHẦN SAU (X = +15.0m đến +38.5m): Hẻm vực đu dây, CÂY ĐỔ BÒ QUA (CHUẨN GAMEPLAY 100% THÔNG THOÁNG) & Đỉnh đồi ngắm cảnh.
    /// </summary>
    public static class GenerateEntireForestLayeringPreviews
    {
        private const string ArtifactDir = @"C:\Users\Thanh Tài\.gemini\antigravity\brain\8f0de94b-4312-45db-8a61-1ed2ab228bb4";
        private const string PackTreesDir = "Assets/Textures/Environment/Lana_VN_Pack/Trees";
        private const string PackBushesDir = "Assets/Textures/Environment/Lana_VN_Pack/Bushes";
        private const string PackGrassDir = "Assets/Textures/Environment/Lana_VN_Pack/Grass_Vines";
        private const string PackWildflowersDir = "Assets/Textures/Environment/Lana_VN_Pack/Wildflowers";
        private const string PackRocksDir = "Assets/Textures/Environment/Lana_VN_Pack/Rocks";

        [MenuItem("Hoa va Mau/Level Design/Generate Entire Forest Previews (Both Front and Back)", false, 1)]
        public static void GenerateAllOptions()
        {
            Debug.Log("<color=#3b82f6>[EntireForestPreviews]</color> Đang khởi tạo 3 Phương án với Cây Đổ chuẩn Gameplay Bò Qua...");

            // 1. Tạm thời ẩn các nhóm cây cối cũ trong scene để dựng preview sạch sẽ
            var hiddenList = new List<GameObject>();
            string[] targetGroupNames = new string[]
            {
                "Obstacle03_FallenTreeTrunk_Crawl",
                "HandDrawn_HeroTrees",
                "HandDrawn_Bushes",
                "HandDrawn_MeadowGrass"
            };

            foreach (var gName in targetGroupNames)
            {
                var go = GameObject.Find(gName);
                if (go != null && go.activeSelf)
                {
                    go.SetActive(false);
                    hiddenList.Add(go);
                }
            }

            // Dùng Shader Sprite-Unlit-Default
            Material spriteMat = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default"));

            // Tải Sprites chuẩn từ gói Lana_VN_Pack
            Sprite sprBigCanopy       = AssetDatabase.LoadAssetAtPath<Sprite>($"{PackTreesDir}/T_LanaVN_Tree_01_LargeCanopy.png");
            Sprite sprMedCanopy       = AssetDatabase.LoadAssetAtPath<Sprite>($"{PackTreesDir}/T_LanaVN_Tree_02_Medium.png");
            Sprite sprSmallBushTree   = AssetDatabase.LoadAssetAtPath<Sprite>($"{PackTreesDir}/T_LanaVN_Tree_03_SmallBushTree.png");
            Sprite sprSlender         = AssetDatabase.LoadAssetAtPath<Sprite>($"{PackTreesDir}/T_LanaVN_Tree_04_SlenderTall.png");
            Sprite sprBanana          = AssetDatabase.LoadAssetAtPath<Sprite>($"{PackTreesDir}/T_LanaVN_Tree_05_Banana.png");
            Sprite sprCoconut         = AssetDatabase.LoadAssetAtPath<Sprite>($"{PackTreesDir}/T_LanaVN_Tree_06_Coconut.png");
            Sprite sprBamboo          = AssetDatabase.LoadAssetAtPath<Sprite>($"{PackTreesDir}/T_LanaVN_Tree_07_BambooGrove.png");

            // CÂY ĐỔ MẪU CHUẨN GAMEPLAY BÒ QUA (Thân nằm ngang thấp ~0.85m, thông thoáng 100%, lối ra thùng gỗ mở rộng)
            Sprite sprCrawlHero       = AssetDatabase.LoadAssetAtPath<Sprite>($"{PackTreesDir}/T_LanaVN_CrawlTunnel_01.png");

            Sprite sprBushBroad1      = AssetDatabase.LoadAssetAtPath<Sprite>($"{PackBushesDir}/T_LanaVN_Bush_Broadleaf_01.png");
            Sprite sprBushBroad2      = AssetDatabase.LoadAssetAtPath<Sprite>($"{PackBushesDir}/T_LanaVN_Bush_Broadleaf_02.png");
            Sprite sprBushLow1        = AssetDatabase.LoadAssetAtPath<Sprite>($"{PackBushesDir}/T_LanaVN_Bush_Low_01.png");
            Sprite sprBushLowFlower   = AssetDatabase.LoadAssetAtPath<Sprite>($"{PackBushesDir}/T_LanaVN_Bush_Low_02_Flower.png");

            Sprite sprGrassTuft1      = AssetDatabase.LoadAssetAtPath<Sprite>($"{PackGrassDir}/T_LanaVN_GrassTuft_01.png");
            Sprite sprGrassTuft2      = AssetDatabase.LoadAssetAtPath<Sprite>($"{PackGrassDir}/T_LanaVN_GrassTuft_02.png");

            Sprite sprReeds           = AssetDatabase.LoadAssetAtPath<Sprite>($"{PackWildflowersDir}/T_LanaVN_Wildflower_01_Reeds.png");
            Sprite sprTallGrass       = AssetDatabase.LoadAssetAtPath<Sprite>($"{PackWildflowersDir}/T_LanaVN_Wildflower_02_TallGrass.png");
            Sprite sprWhiteBlossom    = AssetDatabase.LoadAssetAtPath<Sprite>($"{PackWildflowersDir}/T_LanaVN_Wildflower_03_WhiteBlossom.png");
            Sprite sprDaisySprig      = AssetDatabase.LoadAssetAtPath<Sprite>($"{PackWildflowersDir}/T_LanaVN_Wildflower_04_DaisySprig.png");
            Sprite sprYellowBlossom   = AssetDatabase.LoadAssetAtPath<Sprite>($"{PackWildflowersDir}/T_LanaVN_Wildflower_05_YellowBlossom.png");

            Sprite sprRockCliff       = AssetDatabase.LoadAssetAtPath<Sprite>($"{PackRocksDir}/T_LanaVN_Rock_01_LargeCliffOutcrop.png");
            Sprite sprRockDome        = AssetDatabase.LoadAssetAtPath<Sprite>($"{PackRocksDir}/T_LanaVN_Rock_02_MediumMossyDome.png");
            Sprite sprRockCorner      = AssetDatabase.LoadAssetAtPath<Sprite>($"{PackRocksDir}/T_LanaVN_Rock_03_MediumMossyCorner.png");
            Sprite sprRockSlab        = AssetDatabase.LoadAssetAtPath<Sprite>($"{PackRocksDir}/T_LanaVN_Rock_04_FlatSlab.png");
            Sprite sprRockSmall       = AssetDatabase.LoadAssetAtPath<Sprite>($"{PackRocksDir}/T_LanaVN_Rock_05_SmallMossy.png");
            Sprite sprPebbles         = AssetDatabase.LoadAssetAtPath<Sprite>($"{PackRocksDir}/T_LanaVN_Rock_06_PebbleCluster.png");

            try
            {
                // PHƯƠNG ÁN 1: NẮNG SỚM VEN RỪNG NAM BỘ
                BuildEntireOption1(
                    sprBigCanopy, sprMedCanopy, sprSmallBushTree, sprSlender, sprBanana, sprCoconut, sprBamboo, sprCrawlHero,
                    sprBushBroad1, sprBushBroad2, sprBushLow1, sprBushLowFlower,
                    sprGrassTuft1, sprGrassTuft2,
                    sprReeds, sprTallGrass, sprWhiteBlossom, sprDaisySprig, sprYellowBlossom,
                    sprRockCliff, sprRockDome, sprRockCorner, sprRockSlab, sprRockSmall, sprPebbles, spriteMat);

                // PHƯƠNG ÁN 2: CHIỀU SÂU RỪNG GIÀ ĐA TẦNG
                BuildEntireOption2(
                    sprBigCanopy, sprMedCanopy, sprSmallBushTree, sprSlender, sprBanana, sprCoconut, sprBamboo, sprCrawlHero,
                    sprBushBroad1, sprBushBroad2, sprBushLow1, sprBushLowFlower,
                    sprGrassTuft1, sprGrassTuft2,
                    sprReeds, sprTallGrass, sprWhiteBlossom, sprDaisySprig, sprYellowBlossom,
                    sprRockCliff, sprRockDome, sprRockCorner, sprRockSlab, sprRockSmall, sprPebbles, spriteMat);

                // PHƯƠNG ÁN 3: THUNG LŨNG RÊU PHONG & HOA DẠI DỊU MẮT
                BuildEntireOption3(
                    sprBigCanopy, sprMedCanopy, sprSmallBushTree, sprSlender, sprBanana, sprCoconut, sprBamboo, sprCrawlHero,
                    sprBushBroad1, sprBushBroad2, sprBushLow1, sprBushLowFlower,
                    sprGrassTuft1, sprGrassTuft2,
                    sprReeds, sprTallGrass, sprWhiteBlossom, sprDaisySprig, sprYellowBlossom,
                    sprRockCliff, sprRockDome, sprRockCorner, sprRockSlab, sprRockSmall, sprPebbles, spriteMat);

                Debug.Log("<color=#10b981>[EntireForestPreviews]</color> ĐÃ XUẤT THÀNH CÔNG 6 ẢNH CHUẨN GAMEPLAY!");
            }
            finally
            {
                // Khôi phục 100% Scene hiện có
                foreach (var go in hiddenList)
                {
                    if (go != null) go.SetActive(true);
                }
            }
        }

        // =========================================================================================
        // PHƯƠNG ÁN 1: NẮNG SỚM VEN RỪNG NAM BỘ
        // =========================================================================================
        private static void BuildEntireOption1(
            Sprite sprBigCanopy, Sprite sprMedCanopy, Sprite sprSmallBushTree, Sprite sprSlender, Sprite sprBanana, Sprite sprCoconut, Sprite sprBamboo, Sprite sprCrawlHero,
            Sprite sprBushBroad1, Sprite sprBushBroad2, Sprite sprBushLow1, Sprite sprBushLowFlower,
            Sprite sprGrassTuft1, Sprite sprGrassTuft2,
            Sprite sprReeds, Sprite sprTallGrass, Sprite sprWhiteBlossom, Sprite sprDaisySprig, Sprite sprYellowBlossom,
            Sprite sprRockCliff, Sprite sprRockDome, Sprite sprRockCorner, Sprite sprRockSlab, Sprite sprRockSmall, Sprite sprPebbles, Material mat)
        {
            var container = new GameObject("__TEMP_ENTIRE_FOREST_OP1__");
            try
            {
                Color bgWallColor = new Color(0.35f, 0.48f, 0.32f, 0.90f);
                Color bgTreeColor = new Color(0.50f, 0.62f, 0.42f, 1.0f);
                Color fgSunColor  = Color.white;
                Color rockColor   = new Color(0.85f, 0.88f, 0.82f, 1.0f);

                // 1. HẬU CẢNH: DÀN TRẢI ĐỀU TỪ X = -10m ĐẾN X = 40m
                for (float x = -9.5f; x <= 40.5f; x += 3.8f)
                {
                    SpawnSprite(container.transform, $"BG_Canopy_{x}", sprBigCanopy, new Vector3(x, 1.2f + (x > 25f ? 1.6f : 0f), 7.5f), new Vector2(1.8f, 1.8f), bgWallColor, 2, mat, (int)x % 2 == 0);
                    SpawnSprite(container.transform, $"BG_Bush_{x}", sprBushBroad1, new Vector3(x + 1.2f, -1.2f + (x > 25f ? 1.6f : 0f), 6.5f), new Vector2(2.2f, 2.0f), bgWallColor, 3, mat);
                }

                SpawnSprite(container.transform, "BG_Bamboo_1", sprBamboo, new Vector3(-8.0f, -1.1f, 4.6f), new Vector2(1.3f, 1.3f), bgTreeColor, 4, mat);
                SpawnSprite(container.transform, "BG_Tree_1", sprMedCanopy, new Vector3(-4.5f, -2.0f, 4.8f), new Vector2(1.2f, 1.2f), bgTreeColor, 4, mat);
                SpawnSprite(container.transform, "BG_Bamboo_2", sprBamboo, new Vector3(0.0f, -2.0f, 4.6f), new Vector2(1.3f, 1.3f), bgTreeColor, 4, mat, true);
                SpawnSprite(container.transform, "BG_Bamboo_3", sprBamboo, new Vector3(5.5f, -1.0f, 4.6f), new Vector2(1.25f, 1.25f), bgTreeColor, 4, mat);
                SpawnSprite(container.transform, "BG_Tree_2", sprBigCanopy, new Vector3(10.5f, -0.6f, 4.8f), new Vector2(1.2f, 1.2f), bgTreeColor, 4, mat);
                SpawnSprite(container.transform, "BG_Tree_3", sprMedCanopy, new Vector3(16.5f, -0.6f, 4.8f), new Vector2(1.3f, 1.3f), bgTreeColor, 4, mat);
                SpawnSprite(container.transform, "BG_Bamboo_4", sprBamboo, new Vector3(21.0f, -1.0f, 4.6f), new Vector2(1.3f, 1.3f), bgTreeColor, 4, mat, true);
                SpawnSprite(container.transform, "BG_Bamboo_5", sprBamboo, new Vector3(29.0f, -1.1f, 4.6f), new Vector2(1.3f, 1.3f), bgTreeColor, 4, mat);
                SpawnSprite(container.transform, "BG_Tree_Overlook", sprBigCanopy, new Vector3(36.5f, 2.2f, 4.8f), new Vector2(1.35f, 1.35f), bgTreeColor, 4, mat);

                // 2. PHẦN ĐẦU: DÀN TRẢI ĐỀU (X = -10.5m đến +15.0m)
                SpawnSprite(container.transform, "Z1_Banana_1", sprBanana, new Vector3(-8.5f, -0.85f, 1.4f), new Vector2(1.15f, 1.15f), fgSunColor, 13, mat);
                SpawnSprite(container.transform, "Z1_Rock_1", sprRockDome, new Vector3(-7.2f, -1.15f, 0.7f), new Vector2(0.6f, 0.6f), rockColor, 14, mat);
                SpawnSprite(container.transform, "Z1_Daisy_1", sprWhiteBlossom, new Vector3(-6.2f, -1.35f, 0.3f), new Vector2(0.6f, 0.6f), Color.white, 16, mat);

                SpawnSprite(container.transform, "Z2_Slender_1", sprSlender, new Vector3(-4.5f, -1.85f, 1.1f), new Vector2(1.05f, 1.05f), fgSunColor, 14, mat);
                SpawnSprite(container.transform, "Z2_Bush_1", sprBushLowFlower, new Vector3(-3.5f, -1.85f, 0.6f), new Vector2(0.7f, 0.7f), fgSunColor, 15, mat);
                SpawnSprite(container.transform, "Z2_Daisy_2", sprDaisySprig, new Vector3(-2.5f, -1.85f, 0.3f), new Vector2(0.6f, 0.6f), Color.white, 16, mat);

                SpawnSprite(container.transform, "Z3_Bamboo_Hero", sprBamboo, new Vector3(-0.8f, -1.90f, 1.3f), new Vector2(1.2f, 1.2f), fgSunColor, 13, mat);
                SpawnSprite(container.transform, "Z3_Banana_2", sprBanana, new Vector3(1.2f, -1.85f, 1.2f), new Vector2(1.0f, 1.0f), fgSunColor, 14, mat);
                SpawnSprite(container.transform, "Z3_Reeds_1", sprReeds, new Vector3(2.5f, -1.85f, 0.4f), new Vector2(0.65f, 0.65f), fgSunColor, 15, mat);

                SpawnSprite(container.transform, "Z4_Rock_Steps", sprRockSlab, new Vector3(3.8f, -1.95f, 0.4f), new Vector2(0.65f, 0.65f), rockColor, 15, mat);
                SpawnSprite(container.transform, "Z4_Cliff_Bush", sprBushBroad2, new Vector3(5.2f, -1.45f, 1.0f), new Vector2(0.75f, 0.75f), fgSunColor, 14, mat);

                SpawnSprite(container.transform, "Plateau_Slender", sprSlender, new Vector3(7.5f, -0.60f, 1.1f), new Vector2(1.1f, 1.1f), fgSunColor, 14, mat);
                SpawnSprite(container.transform, "Plateau_Daisy", sprWhiteBlossom, new Vector3(9.5f, -0.60f, 0.3f), new Vector2(0.6f, 0.6f), Color.white, 16, mat);
                SpawnSprite(container.transform, "Plateau_SmallTree", sprSmallBushTree, new Vector3(11.5f, -0.60f, 1.0f), new Vector2(1.0f, 1.0f), fgSunColor, 14, mat);
                SpawnSprite(container.transform, "Plateau_EdgeRock", sprRockCorner, new Vector3(13.5f, -0.70f, 0.4f), new Vector2(0.6f, 0.6f), rockColor, 15, mat);

                // 3. PHẦN SAU (X = +15.0m đến +38.5m): VỰC SÂU, CÂY ĐỔ BÒ QUA & ĐỈNH ĐỒI
                SpawnSprite(container.transform, "Ravine_BigTree", sprBigCanopy, new Vector3(15.2f, -0.55f, 1.5f), new Vector2(1.25f, 1.25f), fgSunColor, 13, mat);
                SpawnSprite(container.transform, "Ravine_East_Banana", sprBanana, new Vector3(18.2f, -0.55f, 1.2f), new Vector2(1.05f, 1.05f), fgSunColor, 14, mat);
                SpawnSprite(container.transform, "Ravine_Grass", sprGrassTuft1, new Vector3(19.8f, -0.55f, 0.3f), new Vector2(0.7f, 0.7f), fgSunColor, 16, mat);

                // === CÂY ĐỔ CHUẨN GAMEPLAY BÒ QUA: ĐƯỜNG HẦM THẤP (~0.85m), BÊN DƯỚI 100% THÔNG SUỐT, LỐI SANG THÙNG GỖ TRỐNG TRÃI ===
                // Gốc cây bên trái bám bờ dốc đất (X = 23.3m), thân nằm ngang lài lài vừa tầm chui bò (Y = -0.30m đến -1.10m), cành đỡ bên phải chạm đất tại X = 26.9m
                // Từ X = 26.9m đến X = 30.6m (Thùng gỗ) là 3.7m đất phẳng hoàn toàn thông thoáng để nhân vật đứng dậy đi lại tự do
                SpawnSprite(container.transform, "Hero_CrawlTunnel", sprCrawlHero, new Vector3(25.10f, 0.08f, 0.0f), new Vector2(0.38f, 0.38f), Color.white, 20, mat);

                // Cỏ và hoa dại điểm nhẹ ở gờ đất bên ngoài phía trái, TUYỆT ĐỐI KHÔNG CHẮN LỖ CHUI
                SpawnSprite(container.transform, "Crawl_Base_Rock", sprRockDome, new Vector3(22.6f, -1.25f, 0.4f), new Vector2(0.55f, 0.55f), rockColor, 14, mat);
                SpawnSprite(container.transform, "Crawl_Daisy_Base", sprWhiteBlossom, new Vector3(22.9f, -1.20f, -0.2f), new Vector2(0.55f, 0.55f), Color.white, 22, mat);

                SpawnSprite(container.transform, "Crate_Bamboo", sprBamboo, new Vector3(30.0f, -1.15f, 1.2f), new Vector2(1.25f, 1.25f), fgSunColor, 13, mat);
                SpawnSprite(container.transform, "Crate_RockWall", sprRockCliff, new Vector3(33.2f, -1.05f, 0.8f), new Vector2(0.9f, 0.9f), rockColor, 14, mat);

                SpawnSprite(container.transform, "Overlook_Tree", sprMedCanopy, new Vector3(36.5f, 2.15f, 1.1f), new Vector2(1.2f, 1.2f), fgSunColor, 14, mat);
                SpawnSprite(container.transform, "Overlook_Daisy", sprWhiteBlossom, new Vector3(38.2f, 2.10f, 0.3f), new Vector2(0.7f, 0.7f), Color.white, 16, mat);

                CaptureView(new Vector3(2.5f, 0.8f, -19.5f), Path.Combine(ArtifactDir, "phuong_an_1_phan_dau_rung_thua_va_vach_da.jpg"));
                CaptureView(new Vector3(26.5f, 0.8f, -19.5f), Path.Combine(ArtifactDir, "phuong_an_1_phan_sau_hem_vuc_cay_do_dinh_doi.jpg"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(container);
            }
        }

        // =========================================================================================
        // PHƯƠNG ÁN 2: CHIỀU SÂU RỪNG GIÀ ĐA TẦNG
        // =========================================================================================
        private static void BuildEntireOption2(
            Sprite sprBigCanopy, Sprite sprMedCanopy, Sprite sprSmallBushTree, Sprite sprSlender, Sprite sprBanana, Sprite sprCoconut, Sprite sprBamboo, Sprite sprCrawlHero,
            Sprite sprBushBroad1, Sprite sprBushBroad2, Sprite sprBushLow1, Sprite sprBushLowFlower,
            Sprite sprGrassTuft1, Sprite sprGrassTuft2,
            Sprite sprReeds, Sprite sprTallGrass, Sprite sprWhiteBlossom, Sprite sprDaisySprig, Sprite sprYellowBlossom,
            Sprite sprRockCliff, Sprite sprRockDome, Sprite sprRockCorner, Sprite sprRockSlab, Sprite sprRockSmall, Sprite sprPebbles, Material mat)
        {
            var container = new GameObject("__TEMP_ENTIRE_FOREST_OP2__");
            try
            {
                Color bgWallColor = new Color(0.20f, 0.30f, 0.18f, 0.95f);
                Color bgTreeColor = new Color(0.35f, 0.48f, 0.28f, 1.0f);
                Color midTreeColor= new Color(0.65f, 0.78f, 0.52f, 1.0f);
                Color fgSunColor  = Color.white;
                Color rockColor   = new Color(0.78f, 0.82f, 0.75f, 1.0f);

                for (float x = -9.5f; x <= 40.5f; x += 3.8f)
                {
                    SpawnSprite(container.transform, $"BG2_Canopy_{x}", sprBigCanopy, new Vector3(x, 1.3f + (x > 25f ? 1.6f : 0f), 7.5f), new Vector2(2.0f, 2.0f), bgWallColor, 2, mat, (int)x % 2 == 0);
                    SpawnSprite(container.transform, $"BG2_Bush_{x}", sprBushBroad2, new Vector3(x + 1.5f, -1.1f + (x > 25f ? 1.6f : 0f), 6.5f), new Vector2(2.4f, 2.2f), bgWallColor, 3, mat);
                }

                SpawnSprite(container.transform, "BG2_Tree_1", sprBigCanopy, new Vector3(-7.5f, -0.95f, 4.8f), new Vector2(1.35f, 1.35f), bgTreeColor, 4, mat);
                SpawnSprite(container.transform, "BG2_Tree_2", sprMedCanopy, new Vector3(-3.0f, -1.95f, 4.8f), new Vector2(1.25f, 1.25f), bgTreeColor, 4, mat);
                SpawnSprite(container.transform, "BG2_Tree_3", sprBigCanopy, new Vector3(2.5f, -1.95f, 4.8f), new Vector2(1.3f, 1.3f), bgTreeColor, 4, mat, true);
                SpawnSprite(container.transform, "BG2_Tree_4", sprMedCanopy, new Vector3(8.0f, -0.75f, 4.8f), new Vector2(1.25f, 1.25f), bgTreeColor, 4, mat);
                SpawnSprite(container.transform, "BG2_Tree_5", sprBigCanopy, new Vector3(15.5f, -0.55f, 4.8f), new Vector2(1.4f, 1.4f), bgTreeColor, 4, mat);
                SpawnSprite(container.transform, "BG2_Tree_6", sprMedCanopy, new Vector3(23.0f, -0.95f, 4.8f), new Vector2(1.3f, 1.3f), bgTreeColor, 4, mat);
                SpawnSprite(container.transform, "BG2_Tree_7", sprBigCanopy, new Vector3(31.0f, -1.05f, 4.8f), new Vector2(1.35f, 1.35f), bgTreeColor, 4, mat);
                SpawnSprite(container.transform, "BG2_Overlook", sprBigCanopy, new Vector3(37.0f, 2.2f, 4.8f), new Vector2(1.4f, 1.4f), bgTreeColor, 4, mat);

                SpawnSprite(container.transform, "Z1_Tree_Hero", sprMedCanopy, new Vector3(-8.0f, -0.90f, 1.3f), new Vector2(1.15f, 1.15f), midTreeColor, 13, mat);
                SpawnSprite(container.transform, "Z1_Rock_Large", sprRockCliff, new Vector3(-6.8f, -1.20f, 0.8f), new Vector2(0.7f, 0.7f), rockColor, 14, mat);
                SpawnSprite(container.transform, "Z1_Grass_Tuft", sprGrassTuft2, new Vector3(-5.8f, -1.40f, 0.3f), new Vector2(0.7f, 0.7f), fgSunColor, 16, mat);

                SpawnSprite(container.transform, "Z2_Slender_1", sprSlender, new Vector3(-4.0f, -1.85f, 1.1f), new Vector2(1.1f, 1.1f), fgSunColor, 14, mat);
                SpawnSprite(container.transform, "Z2_Bush_Broad", sprBushBroad1, new Vector3(-2.8f, -1.85f, 0.6f), new Vector2(0.75f, 0.75f), fgSunColor, 15, mat);
                SpawnSprite(container.transform, "Z2_Yellow_Blossom", sprYellowBlossom, new Vector3(-1.8f, -1.85f, 0.3f), new Vector2(0.6f, 0.6f), Color.white, 16, mat);

                SpawnSprite(container.transform, "Z3_MedTree", sprMedCanopy, new Vector3(-0.2f, -1.85f, 1.2f), new Vector2(1.15f, 1.15f), midTreeColor, 13, mat);
                SpawnSprite(container.transform, "Z3_Daisy", sprWhiteBlossom, new Vector3(1.2f, -1.85f, 0.3f), new Vector2(0.6f, 0.6f), Color.white, 16, mat);
                SpawnSprite(container.transform, "Z3_Reeds", sprTallGrass, new Vector3(2.6f, -1.85f, 0.5f), new Vector2(0.7f, 0.7f), fgSunColor, 15, mat);

                SpawnSprite(container.transform, "Z4_Rock_Slab", sprRockSlab, new Vector3(3.9f, -1.95f, 0.4f), new Vector2(0.7f, 0.7f), rockColor, 15, mat);
                SpawnSprite(container.transform, "Z4_Slender", sprSlender, new Vector3(5.5f, -1.45f, 1.1f), new Vector2(1.0f, 1.0f), fgSunColor, 14, mat);

                SpawnSprite(container.transform, "Plateau_Tree", sprSmallBushTree, new Vector3(7.8f, -0.60f, 1.1f), new Vector2(1.15f, 1.15f), fgSunColor, 14, mat);
                SpawnSprite(container.transform, "Plateau_Grass", sprGrassTuft1, new Vector3(9.8f, -0.60f, 0.3f), new Vector2(0.65f, 0.65f), fgSunColor, 16, mat);
                SpawnSprite(container.transform, "Plateau_Slender_2", sprSlender, new Vector3(12.0f, -0.60f, 1.1f), new Vector2(1.1f, 1.1f), fgSunColor, 14, mat);

                SpawnSprite(container.transform, "Ravine_Tree", sprBigCanopy, new Vector3(14.8f, -0.55f, 1.4f), new Vector2(1.3f, 1.3f), midTreeColor, 13, mat);
                SpawnSprite(container.transform, "Ravine_Slender", sprSlender, new Vector3(18.5f, -0.55f, 1.1f), new Vector2(1.1f, 1.1f), fgSunColor, 14, mat);
                SpawnSprite(container.transform, "Ravine_Fern", sprGrassTuft2, new Vector3(20.0f, -0.55f, 0.3f), new Vector2(0.7f, 0.7f), fgSunColor, 16, mat);

                // === CÂY ĐỔ CHUẨN GAMEPLAY BÒ QUA: ĐƯỜNG HẦM THẤP (~0.85m), 100% THÔNG SUỐT ===
                SpawnSprite(container.transform, "Hero_CrawlTunnel2", sprCrawlHero, new Vector3(25.10f, 0.08f, 0.0f), new Vector2(0.38f, 0.38f), Color.white, 20, mat);
                SpawnSprite(container.transform, "Crawl_Rock_Left", sprRockCliff, new Vector3(22.6f, -1.25f, 0.5f), new Vector2(0.65f, 0.65f), rockColor, 14, mat);

                SpawnSprite(container.transform, "Crate_Tree", sprMedCanopy, new Vector3(30.0f, -1.15f, 1.2f), new Vector2(1.2f, 1.2f), midTreeColor, 13, mat);
                SpawnSprite(container.transform, "Crate_RockWall", sprRockCliff, new Vector3(33.2f, -1.05f, 0.7f), new Vector2(0.95f, 0.95f), rockColor, 14, mat);

                SpawnSprite(container.transform, "Overlook_Tree_Grand", sprBigCanopy, new Vector3(36.8f, 2.15f, 1.2f), new Vector2(1.35f, 1.35f), fgSunColor, 14, mat);
                SpawnSprite(container.transform, "Overlook_Yellow", sprYellowBlossom, new Vector3(38.5f, 2.10f, 0.3f), new Vector2(0.7f, 0.7f), Color.white, 16, mat);

                CaptureView(new Vector3(2.5f, 0.8f, -19.5f), Path.Combine(ArtifactDir, "phuong_an_2_phan_dau_rung_thua_va_vach_da.jpg"));
                CaptureView(new Vector3(26.5f, 0.8f, -19.5f), Path.Combine(ArtifactDir, "phuong_an_2_phan_sau_hem_vuc_cay_do_dinh_doi.jpg"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(container);
            }
        }

        // =========================================================================================
        // PHƯƠNG ÁN 3: THUNG LŨNG RÊU PHONG & HOA DẠI DỊU MẮT
        // =========================================================================================
        private static void BuildEntireOption3(
            Sprite sprBigCanopy, Sprite sprMedCanopy, Sprite sprSmallBushTree, Sprite sprSlender, Sprite sprBanana, Sprite sprCoconut, Sprite sprBamboo, Sprite sprCrawlHero,
            Sprite sprBushBroad1, Sprite sprBushBroad2, Sprite sprBushLow1, Sprite sprBushLowFlower,
            Sprite sprGrassTuft1, Sprite sprGrassTuft2,
            Sprite sprReeds, Sprite sprTallGrass, Sprite sprWhiteBlossom, Sprite sprDaisySprig, Sprite sprYellowBlossom,
            Sprite sprRockCliff, Sprite sprRockDome, Sprite sprRockCorner, Sprite sprRockSlab, Sprite sprRockSmall, Sprite sprPebbles, Material mat)
        {
            var container = new GameObject("__TEMP_ENTIRE_FOREST_OP3__");
            try
            {
                Color bgWallColor = new Color(0.38f, 0.50f, 0.38f, 0.90f);
                Color bgTreeColor = new Color(0.52f, 0.64f, 0.46f, 1.0f);
                Color fgSunColor  = Color.white;
                Color rockColor   = new Color(0.86f, 0.89f, 0.84f, 1.0f);

                for (float x = -9.5f; x <= 40.5f; x += 3.8f)
                {
                    SpawnSprite(container.transform, $"BG3_Canopy_{x}", sprBigCanopy, new Vector3(x, 1.2f + (x > 25f ? 1.6f : 0f), 7.5f), new Vector2(1.85f, 1.85f), bgWallColor, 2, mat, (int)x % 2 == 0);
                    SpawnSprite(container.transform, $"BG3_Bush_{x}", sprBushLowFlower, new Vector3(x + 1.2f, -1.2f + (x > 25f ? 1.6f : 0f), 6.5f), new Vector2(2.2f, 2.0f), bgWallColor, 3, mat);
                }

                SpawnSprite(container.transform, "BG3_Tree_1", sprMedCanopy, new Vector3(-7.5f, -0.95f, 4.8f), new Vector2(1.25f, 1.25f), bgTreeColor, 4, mat);
                SpawnSprite(container.transform, "BG3_Bamboo_1", sprBamboo, new Vector3(-3.0f, -1.95f, 4.6f), new Vector2(1.3f, 1.3f), bgTreeColor, 4, mat);
                SpawnSprite(container.transform, "BG3_Tree_2", sprSmallBushTree, new Vector3(2.5f, -1.95f, 4.8f), new Vector2(1.2f, 1.2f), bgTreeColor, 4, mat);
                SpawnSprite(container.transform, "BG3_Bamboo_2", sprBamboo, new Vector3(8.0f, -0.75f, 4.6f), new Vector2(1.3f, 1.3f), bgTreeColor, 4, mat);
                SpawnSprite(container.transform, "BG3_Tree_3", sprBigCanopy, new Vector3(15.5f, -0.55f, 4.8f), new Vector2(1.35f, 1.35f), bgTreeColor, 4, mat);
                SpawnSprite(container.transform, "BG3_Bamboo_3", sprBamboo, new Vector3(22.5f, -0.95f, 4.6f), new Vector2(1.3f, 1.3f), bgTreeColor, 4, mat);
                SpawnSprite(container.transform, "BG3_Tree_4", sprMedCanopy, new Vector3(30.0f, -1.05f, 4.8f), new Vector2(1.3f, 1.3f), bgTreeColor, 4, mat);
                SpawnSprite(container.transform, "BG3_Overlook", sprBigCanopy, new Vector3(37.0f, 2.2f, 4.8f), new Vector2(1.35f, 1.35f), bgTreeColor, 4, mat);

                SpawnSprite(container.transform, "Z1_Daisy_High", sprWhiteBlossom, new Vector3(-8.2f, -0.85f, 0.3f), new Vector2(0.65f, 0.65f), Color.white, 16, mat);
                SpawnSprite(container.transform, "Z1_Rock_1", sprRockDome, new Vector3(-7.0f, -1.15f, 0.7f), new Vector2(0.6f, 0.6f), rockColor, 14, mat);
                SpawnSprite(container.transform, "Z1_Daisy_Slope", sprDaisySprig, new Vector3(-6.0f, -1.35f, 0.3f), new Vector2(0.6f, 0.6f), Color.white, 16, mat);

                SpawnSprite(container.transform, "Z2_Slender_Sage", sprSlender, new Vector3(-4.2f, -1.85f, 1.1f), new Vector2(1.05f, 1.05f), fgSunColor, 14, mat);
                SpawnSprite(container.transform, "Z2_Bush_Flower", sprBushLowFlower, new Vector3(-3.2f, -1.85f, 0.6f), new Vector2(0.7f, 0.7f), fgSunColor, 15, mat);
                SpawnSprite(container.transform, "Z2_Daisy_Group", sprWhiteBlossom, new Vector3(-2.2f, -1.85f, 0.3f), new Vector2(0.65f, 0.65f), Color.white, 16, mat);

                SpawnSprite(container.transform, "Z3_SmallTree", sprSmallBushTree, new Vector3(-0.5f, -1.85f, 1.1f), new Vector2(1.1f, 1.1f), fgSunColor, 14, mat);
                SpawnSprite(container.transform, "Z3_Daisy_Center", sprDaisySprig, new Vector3(0.8f, -1.85f, 0.3f), new Vector2(0.6f, 0.6f), Color.white, 16, mat);
                SpawnSprite(container.transform, "Z3_Reeds_Soft", sprReeds, new Vector3(2.2f, -1.85f, 0.4f), new Vector2(0.7f, 0.7f), fgSunColor, 15, mat);

                SpawnSprite(container.transform, "Z4_Rock_Slab", sprRockSlab, new Vector3(3.8f, -1.95f, 0.4f), new Vector2(0.65f, 0.65f), rockColor, 15, mat);
                SpawnSprite(container.transform, "Z4_Bush_Low", sprBushLow1, new Vector3(5.2f, -1.45f, 0.8f), new Vector2(0.7f, 0.7f), fgSunColor, 14, mat);

                SpawnSprite(container.transform, "Plateau_Tree_Sage", sprMedCanopy, new Vector3(7.5f, -0.60f, 1.2f), new Vector2(1.1f, 1.1f), fgSunColor, 14, mat);
                SpawnSprite(container.transform, "Plateau_Daisy_Field", sprWhiteBlossom, new Vector3(9.5f, -0.60f, 0.3f), new Vector2(0.65f, 0.65f), Color.white, 16, mat);
                SpawnSprite(container.transform, "Plateau_Rock_Dome", sprRockDome, new Vector3(12.5f, -0.70f, 0.4f), new Vector2(0.6f, 0.6f), rockColor, 15, mat);

                SpawnSprite(container.transform, "Ravine_Tree_Bough", sprMedCanopy, new Vector3(15.0f, -0.55f, 1.3f), new Vector2(1.25f, 1.25f), fgSunColor, 13, mat);
                SpawnSprite(container.transform, "Ravine_Daisy_East", sprWhiteBlossom, new Vector3(18.2f, -0.55f, 0.3f), new Vector2(0.65f, 0.65f), Color.white, 16, mat);
                SpawnSprite(container.transform, "Ravine_Reeds_East", sprReeds, new Vector3(19.8f, -0.55f, 0.4f), new Vector2(0.7f, 0.7f), fgSunColor, 15, mat);

                // === CÂY ĐỔ CHUẨN GAMEPLAY BÒ QUA: ĐƯỜNG HẦM THẤP (~0.85m), 100% THÔNG SUỐT ===
                SpawnSprite(container.transform, "Hero_CrawlTunnel3", sprCrawlHero, new Vector3(25.10f, 0.08f, 0.0f), new Vector2(0.38f, 0.38f), Color.white, 20, mat);
                SpawnSprite(container.transform, "Crawl_Daisy_1", sprWhiteBlossom, new Vector3(23.0f, -1.20f, -0.2f), new Vector2(0.6f, 0.6f), Color.white, 22, mat);

                SpawnSprite(container.transform, "Crate_Tree_Sage", sprSlender, new Vector3(29.8f, -1.15f, 1.1f), new Vector2(1.1f, 1.1f), fgSunColor, 14, mat);
                SpawnSprite(container.transform, "Crate_Rock_Slab", sprRockSlab, new Vector3(33.0f, -1.20f, 0.5f), new Vector2(0.65f, 0.65f), rockColor, 15, mat);

                SpawnSprite(container.transform, "Overlook_Tree_Sage", sprMedCanopy, new Vector3(36.8f, 2.15f, 1.15f), new Vector2(1.2f, 1.2f), fgSunColor, 14, mat);
                SpawnSprite(container.transform, "Overlook_Daisy_Endless", sprWhiteBlossom, new Vector3(38.5f, 2.10f, 0.3f), new Vector2(0.7f, 0.7f), Color.white, 16, mat);

                CaptureView(new Vector3(2.5f, 0.8f, -19.5f), Path.Combine(ArtifactDir, "phuong_an_3_phan_dau_rung_thua_va_vach_da.jpg"));
                CaptureView(new Vector3(26.5f, 0.8f, -19.5f), Path.Combine(ArtifactDir, "phuong_an_3_phan_sau_hem_vuc_cay_do_dinh_doi.jpg"));
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

        private static void CaptureView(Vector3 camPos, string outputPath)
        {
            var cam = Camera.main;
            if (cam == null) return;

            Vector3 oldPos = cam.transform.position;
            cam.transform.position = camPos;

            int width = 1280;
            int height = 720;
            var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var oldTarget = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            byte[] bytes = tex.EncodeToJPG(85);
            File.WriteAllBytes(outputPath, bytes);

            cam.targetTexture = oldTarget;
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);
            UnityEngine.Object.DestroyImmediate(tex);

            cam.transform.position = oldPos;
        }
    }
}
#endif
