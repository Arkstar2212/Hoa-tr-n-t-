using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace HoaDo.EditorTools
{
    /// <summary>
    /// Công cụ cân đối tỉ lệ (Scale & Crop Size) cho toàn bộ hoa cỏ trong Scene:
    /// - Chuẩn hóa kích thước hoa dại (Wildflowers) đạt 0.18m - 0.38m (ngang bắp chân / đầu gối Minh - 1.31m).
    /// - Chuẩn hóa cỏ viền (Grass Tufts) đạt 0.24m - 0.32m ôm sát mặt đường.
    /// - Chuẩn hóa cỏ cao (High Grass) đạt 0.65m - 0.85m (ngang hông / ngực Minh).
    /// - Chuẩn hóa cỏ dại lá to (Broadleaf Weed) đạt 0.30m - 0.40m bám mặt đất.
    /// - Chuẩn hóa bụi cỏ & cây con (Bushes & Saplings) đạt 0.50m - 0.95m.
    /// - Khắc phục triệt để lỗi "crop" ăn sâu vào đất do tâm xoay Pivot Center bằng cách tính lại cao độ neo mặt đất.
    /// </summary>
    public static class FixFoliageProportionsAndCrop
    {
        [MenuItem("Tools/Planet of Lana/Balance Foliage Proportions & Ground Crop", priority = 25)]
        public static void BalanceAllFoliage()
        {
            Debug.Log("[FixFoliageProportionsAndCrop] === Bắt đầu cân đối kích thước hoa cỏ và khắc phục lỗi crop đất ===");
            Undo.SetCurrentGroupName("Balance Foliage Proportions & Ground Crop");
            int undoGroup = Undo.GetCurrentGroup();

            int adjustedMeadowFlowers = 0;
            int adjustedMeadowGrass = 0;
            int adjustedMeadowSaplings = 0;
            int adjustedMeadowBushes = 0;
            int adjustedLedgeFoliage = 0;
            int adjustedStandalone = 0;

            // 1. XỬ LÝ NHÓM THẢM HOA CỎ ĐỒNG CỎ ADDITIVE: [Official_Lana_Foliage_Meadow_Additive]
            GameObject meadowRoot = GameObject.Find("[Official_Lana_Foliage_Meadow_Additive]");
            if (meadowRoot != null)
            {
                // 1.1. Cân đối Hoa dại (02_Wildflower_Fields)
                Transform flowerGroup = meadowRoot.transform.Find("02_Wildflower_Fields");
                if (flowerGroup != null)
                {
                    for (int i = 0; i < flowerGroup.childCount; i++)
                    {
                        Transform flower = flowerGroup.GetChild(i);
                        var sr = flower.GetComponentInChildren<SpriteRenderer>();
                        if (sr == null || sr.sprite == null) continue;

                        float rawH = sr.sprite.bounds.size.y;
                        float rawW = sr.sprite.bounds.size.x;
                        float posX = flower.position.x;

                        // Mục tiêu: Chiều cao hoa dao động 0.18m - 0.36m (tỉ lệ hoàn hảo với Minh cao 1.31m)
                        float targetH = Mathf.Lerp(0.20f, 0.35f, Mathf.Abs(Mathf.Sin(posX * 3.7f + i)));
                        float scale = Mathf.Clamp(targetH / rawH, 0.20f, 0.36f);

                        float signX = Mathf.Sign(flower.localScale.x);
                        Undo.RecordObject(flower, "Scale Meadow Flower");
                        flower.localScale = new Vector3(scale * signX, scale, 1f);
                        adjustedMeadowFlowers++;
                    }
                }

                // 1.2. Cân đối Cỏ viền & Cỏ cao (01_Grass_Tufts_And_Edges)
                Transform grassGroup = meadowRoot.transform.Find("01_Grass_Tufts_And_Edges");
                if (grassGroup != null)
                {
                    for (int i = 0; i < grassGroup.childCount; i++)
                    {
                        Transform grass = grassGroup.GetChild(i);
                        var sr = grass.GetComponentInChildren<SpriteRenderer>();
                        if (sr == null || sr.sprite == null) continue;

                        string spriteName = sr.sprite.name.ToLowerInvariant();
                        float rawH = sr.sprite.bounds.size.y;
                        float posX = grass.position.x;
                        float signX = Mathf.Sign(grass.localScale.x);

                        float scale;
                        if (spriteName.Contains("highgrass"))
                        {
                            // Cỏ cao (High Grass): Cao 0.65m - 0.85m (ngang hông Minh)
                            float targetH = Mathf.Lerp(0.68f, 0.82f, Mathf.Abs(Mathf.Sin(posX * 4.1f + i)));
                            scale = Mathf.Clamp(targetH / rawH, 0.36f, 0.48f);
                        }
                        else
                        {
                            // Cỏ thấp viền đường (Grass Forest): Cao 0.24m - 0.32m (ngang bắp chân Minh)
                            float targetH = Mathf.Lerp(0.24f, 0.32f, Mathf.Abs(Mathf.Sin(posX * 5.3f + i)));
                            scale = Mathf.Clamp(targetH / rawH, 0.28f, 0.38f);
                        }

                        Undo.RecordObject(grass, "Scale Meadow Grass");
                        grass.localScale = new Vector3(scale * signX, scale, 1f);
                        adjustedMeadowGrass++;
                    }
                }

                // 1.3. Cân đối Cây con (03_Saplings_And_YoungTrees)
                Transform saplingGroup = meadowRoot.transform.Find("03_Saplings_And_YoungTrees");
                if (saplingGroup != null)
                {
                    for (int i = 0; i < saplingGroup.childCount; i++)
                    {
                        Transform sapling = saplingGroup.GetChild(i);
                        var sr = sapling.GetComponentInChildren<SpriteRenderer>();
                        if (sr == null || sr.sprite == null) continue;

                        float rawH = sr.sprite.bounds.size.y;
                        float targetH = Mathf.Lerp(0.70f, 0.95f, Mathf.Abs(Mathf.Sin(sapling.position.x * 2.5f)));
                        float scale = Mathf.Clamp(targetH / rawH, 0.35f, 0.50f);
                        float signX = Mathf.Sign(sapling.localScale.x);

                        Undo.RecordObject(sapling, "Scale Meadow Sapling");
                        sapling.localScale = new Vector3(scale * signX, scale, 1f);
                        adjustedMeadowSaplings++;
                    }
                }

                // 1.4. Cân đối Bụi cây thấp (04_Bushes_And_Shrubs)
                Transform bushGroup = meadowRoot.transform.Find("04_Bushes_And_Shrubs");
                if (bushGroup != null)
                {
                    for (int i = 0; i < bushGroup.childCount; i++)
                    {
                        Transform bush = bushGroup.GetChild(i);
                        var sr = bush.GetComponentInChildren<SpriteRenderer>();
                        if (sr == null || sr.sprite == null) continue;

                        float rawH = sr.sprite.bounds.size.y;
                        float targetH = Mathf.Lerp(0.48f, 0.70f, Mathf.Abs(Mathf.Cos(bush.position.x * 2.9f)));
                        float scale = Mathf.Clamp(targetH / rawH, 0.28f, 0.42f);
                        float signX = Mathf.Sign(bush.localScale.x);

                        Undo.RecordObject(bush, "Scale Meadow Bush");
                        bush.localScale = new Vector3(scale * signX, scale, 1f);
                        adjustedMeadowBushes++;
                    }
                }
            }

            // 2. XỬ LÝ DẢI THỰC VẬT ĐOẠN KHỞI ĐẦU: [Official_Lana_Foliage] DƯỚI Ledge_01_ForestStart
            GameObject ledgeFoliage = GameObject.Find("[Official_Lana_Foliage]");
            if (ledgeFoliage != null)
            {
                for (int i = 0; i < ledgeFoliage.transform.childCount; i++)
                {
                    Transform item = ledgeFoliage.transform.GetChild(i);
                    var sr = item.GetComponent<SpriteRenderer>();
                    if (sr == null || sr.sprite == null) continue;

                    string name = item.name.ToLowerInvariant();
                    float rawH = sr.sprite.bounds.size.y;
                    float posX = item.position.x;
                    float signX = Mathf.Sign(item.localScale.x);

                    // Cao độ mặt đất thực tế tại toạ độ X
                    float groundY = PlanetOfLanaAssetPipeline.GetPlatformSurfaceY(posX);

                    float targetScale = 1f;

                    if (name.StartsWith("wildflower"))
                    {
                        // Hoa dại: 0.20m - 0.35m
                        float targetH = Mathf.Lerp(0.20f, 0.35f, Mathf.Abs(Mathf.Sin(posX * 4.2f + i)));
                        targetScale = Mathf.Clamp(targetH / rawH, 0.22f, 0.36f);
                    }
                    else if (name.StartsWith("grasstuft"))
                    {
                        // Khóm cỏ viền: 0.24m - 0.32m
                        float targetH = Mathf.Lerp(0.24f, 0.32f, Mathf.Abs(Mathf.Sin(posX * 5.1f + i)));
                        targetScale = Mathf.Clamp(targetH / rawH, 0.28f, 0.38f);
                    }
                    else if (name.StartsWith("broadleafweed"))
                    {
                        // Cỏ dại lá to (raw ~3.5m): Thu nhỏ xuống 0.32m - 0.42m
                        targetScale = Mathf.Clamp(0.35f / rawH, 0.10f, 0.15f);
                    }
                    else
                    {
                        // Rêu (LipMoss) và sỏi (PebbleCluster) giữ nguyên tỉ lệ nhỏ
                        continue;
                    }

                    Undo.RecordObject(item, "Balance Foliage Crop & Scale");
                    item.localScale = new Vector3(targetScale * signX, targetScale, 1f);

                    // FIX LỖI CROP ĐẤT:
                    // Vì sprite pivot ở Center (0.5, 0.5), đặt posY = groundY + (halfHeight * targetScale) - 0.015f
                    // Nhờ đó gốc cây/cỏ cắm ngập nhẹ 1.5cm vào lòng đất, không bị cắt cụt một nửa thân vào đá!
                    float halfHeight = rawH * 0.5f;
                    float newPosY = groundY + (halfHeight * targetScale) - 0.015f;
                    item.position = new Vector3(posX, newPosY, item.position.z);

                    adjustedLedgeFoliage++;
                }
            }

            // 3. XỬ LÝ CÁC BỤI HOA LẺ RẢI RÁC TRONG SCENE
            // 3.1. Opening_Flower_SmallSprig (ngay gần Minh)
            var openingFlower = GameObject.Find("Opening_Flower_SmallSprig");
            if (openingFlower != null)
            {
                Undo.RecordObject(openingFlower.transform, "Scale Opening Flower");
                openingFlower.transform.localScale = new Vector3(0.12f, 0.12f, 1f);
                openingFlower.transform.position = new Vector3(1.51f, -1.82f, 1.05f);
                adjustedStandalone++;
            }

            // 3.2. Zone2_Flower_Slope1
            var zone2Flower = GameObject.Find("Zone2_Flower_Slope1");
            if (zone2Flower != null)
            {
                Undo.RecordObject(zone2Flower.transform, "Scale Zone2 Flower");
                zone2Flower.transform.localScale = new Vector3(0.11f, 0.11f, 1f);
                zone2Flower.transform.position = new Vector3(47.93f, 1.62f, 1.10f);
                adjustedStandalone++;
            }

            // 3.3. T_HD_Bush_Low_02_Flower
            var hdFlowerBush = GameObject.Find("T_HD_Bush_Low_02_Flower");
            if (hdFlowerBush != null)
            {
                Undo.RecordObject(hdFlowerBush.transform, "Scale HD Flower Bush");
                hdFlowerBush.transform.localScale = new Vector3(0.24f, 0.24f, 1f);
                hdFlowerBush.transform.position = new Vector3(-6.04f, -1.55f, 2.82f);
                adjustedStandalone++;
            }

            // 3.4. Foliage_Lana_WildFlowers
            var lanaWildflowers = GameObject.Find("Foliage_Lana_WildFlowers");
            if (lanaWildflowers != null)
            {
                Undo.RecordObject(lanaWildflowers.transform, "Scale Lana Wildflowers");
                lanaWildflowers.transform.localScale = new Vector3(0.28f, 0.32f, 1f);
                lanaWildflowers.transform.position = new Vector3(8.09f, -0.40f, 0.65f);
                adjustedStandalone++;
            }

            // 3.5. Trunk_Wildflower_01 & Trunk_Wildflower_02 trên thân cây gãy
            var trunkFlower1 = GameObject.Find("Trunk_Wildflower_01");
            if (trunkFlower1 != null)
            {
                Undo.RecordObject(trunkFlower1.transform, "Scale Trunk Wildflower 1");
                trunkFlower1.transform.localScale = new Vector3(0.20f, 0.20f, 1f);
                adjustedStandalone++;
            }
            var trunkFlower2 = GameObject.Find("Trunk_Wildflower_02");
            if (trunkFlower2 != null)
            {
                Undo.RecordObject(trunkFlower2.transform, "Scale Trunk Wildflower 2");
                trunkFlower2.transform.localScale = new Vector3(0.18f, 0.18f, 1f);
                adjustedStandalone++;
            }

            // 4. LƯU SCENE VÀ GHI NHẬN KẾT QUẢ
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            Undo.CollapseUndoOperations(undoGroup);

            Debug.Log($"[FixFoliageProportionsAndCrop] HOÀN TẤT CÂN ĐỐI TỈ LỆ HOA CỎ VÀ SỬA LỖI CROP ĐẤT:\n" +
                      $"- Hoa dại Meadow: {adjustedMeadowFlowers} cây (cao ~0.20m - 0.35m)\n" +
                      $"- Cỏ viền & cỏ cao Meadow: {adjustedMeadowGrass} khóm\n" +
                      $"- Cây con Meadow: {adjustedMeadowSaplings} cây\n" +
                      $"- Bụi cây Meadow: {adjustedMeadowBushes} bụi\n" +
                      $"- Dải thực vật Ledge [Official_Lana_Foliage]: {adjustedLedgeFoliage} vật thể (đã chỉnh pivot không chôn đất)\n" +
                      $"- Hoa lẻ độc lập: {adjustedStandalone} cụm\n" +
                      $"=> Tỉ lệ toàn bộ hoa cỏ giờ đây hoàn toàn cân đối, hài hòa với nhân vật Minh (1.31m)!");
        }
    }
}
