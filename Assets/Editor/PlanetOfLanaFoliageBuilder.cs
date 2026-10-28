using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace HoaDo.EditorTools
{
    public static class PlanetOfLanaFoliageBuilder
    {
        private const string OfficialSpritesBase = "Assets/Art/PlanetOfLana_Official";
        private const string BasePrefabDir = "Assets/Prefabs/PlanetOfLana_Foliage";

        [MenuItem("Tools/Planet of Lana/3. Generate Foliage Prefabs & Stage Meadow Scene", priority = 30)]
        public static void GeneratePrefabsAndStageMeadow()
        {
            Debug.Log("[PlanetOfLanaFoliageBuilder] === Bắt đầu quy trình đóng gói Prefabs & dàn dựng Additive ===");

            // 1. Tạo các thư mục Prefab chuẩn hóa
            EnsureFolder("Assets/Prefabs");
            EnsureFolder(BasePrefabDir);
            EnsureFolder($"{BasePrefabDir}/01_Co_Grass");
            EnsureFolder($"{BasePrefabDir}/02_HoaCon_Flowers");
            EnsureFolder($"{BasePrefabDir}/03_CayCon_Saplings");
            EnsureFolder($"{BasePrefabDir}/04_BuiCo_Bushes");

            // 2. Tải Material chuẩn
            Material defaultSpriteMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/LanaOverhaul/M_Lana_SpriteDefault.mat");
            if (defaultSpriteMat == null)
            {
                var guids = AssetDatabase.FindAssets("M_Lana_SpriteDefault t:Material");
                if (guids.Length > 0)
                {
                    defaultSpriteMat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guids[0]));
                }
            }

            Material interactiveGrassMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Lana_InteractiveGrass.mat");

            // 3. Đóng gói Prefabs (có lọc kích thước chuẩn để tránh tán cây khổng lồ)
            var grassPrefabs = BuildPrefabsForCategory("Grass", $"{BasePrefabDir}/01_Co_Grass", "PF_Lana_Grass", defaultSpriteMat, interactiveGrassMat, 18, 1.8f);
            var flowerPrefabs = BuildPrefabsForCategory("Flora", $"{BasePrefabDir}/02_HoaCon_Flowers", "PF_Lana_Flower", defaultSpriteMat, null, 24, 1.6f);
            var saplingPrefabs = BuildPrefabsForCategory("Saplings", $"{BasePrefabDir}/03_CayCon_Saplings", "PF_Lana_Sapling", defaultSpriteMat, null, 21, 2.5f);
            var bushPrefabs = BuildPrefabsForCategory("Bushes", $"{BasePrefabDir}/04_BuiCo_Bushes", "PF_Lana_Bush", defaultSpriteMat, null, 19, 2.2f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[PlanetOfLanaFoliageBuilder] Đã hoàn thành thư viện Prefabs chuẩn hóa:\n" +
                      $"- Cỏ (Grass): {grassPrefabs.Count} prefabs\n" +
                      $"- Hoa con (Flowers): {flowerPrefabs.Count} prefabs\n" +
                      $"- Cây con (Saplings): {saplingPrefabs.Count} prefabs\n" +
                      $"- Bụi cỏ / Bụi thấp (Bushes): {bushPrefabs.Count} prefabs");

            // 4. Dàn dựng Additive lên Scene New Scene 1_Overhaul
            StageAdditiveMeadowInScene(grassPrefabs, flowerPrefabs, saplingPrefabs, bushPrefabs);
        }

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path).Replace("\\", "/");
                string folderName = Path.GetFileName(path);
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }

        private static List<GameObject> BuildPrefabsForCategory(string categoryKey, string targetFolder, string namePrefix, Material defaultMat, Material grassMat, int sortingOrder, float maxHeight)
        {
            var createdPrefabs = new List<GameObject>();

            string searchFolder = OfficialSpritesBase;
            if (categoryKey == "Grass") searchFolder = $"{OfficialSpritesBase}/Grass";
            else if (categoryKey == "Flora") searchFolder = $"{OfficialSpritesBase}/Flora";
            else if (categoryKey == "Bushes") searchFolder = $"{OfficialSpritesBase}/Bushes";

            string[] guids = AssetDatabase.FindAssets("t:Sprite", new string[] { searchFolder });
            var processedNames = new HashSet<string>();

            foreach (var guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileNameWithoutExtension(assetPath);
                string lower = fileName.ToLowerInvariant();

                // Loại trừ ngay các tán cây lớn, skybox, cây cháy khổng lồ
                if (lower.Contains("treeleaves") || lower.Contains("tree_0") || lower.Contains("skybox") || lower.Contains("forestsky") || lower.Contains("burnt"))
                {
                    continue;
                }

                bool isMatch = false;
                if (categoryKey == "Grass")
                {
                    isMatch = lower.Contains("grass") || lower.Contains("reed");
                }
                else if (categoryKey == "Flora")
                {
                    isMatch = lower.Contains("flower") || lower.Contains("weed") || lower.Contains("petal");
                }
                else if (categoryKey == "Saplings")
                {
                    isMatch = lower.Contains("oaksmall") || lower.Contains("shorttree") || lower.Contains("smallplant") ||
                              lower.Contains("branchplant") || lower.Contains("plant_00") || lower.Contains("seed-leaf");
                }
                else if (categoryKey == "Bushes")
                {
                    isMatch = (lower.Contains("bush") || lower.Contains("rockplant") || lower.Contains("leaf") || lower.Contains("leaves")) &&
                              !lower.Contains("oaksmall") && !lower.Contains("shorttree") && !lower.Contains("plant_00");
                }

                if (!isMatch || processedNames.Contains(fileName)) continue;
                processedNames.Add(fileName);

                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                if (sprite == null) continue;

                // Lọc chiều cao tối đa của Sprite để bảo đảm là thảm cỏ/hoa/cây con/bụi thấp
                if (sprite.bounds.size.y > maxHeight) continue;

                // Chuẩn hóa tên Prefab sạch sẽ
                string cleanName = $"{namePrefix}_{fileName}";
                string prefabPath = $"{targetFolder}/{cleanName}.prefab";

                // Tạo cấu trúc Root (Ground Pivot) -> Visual (Offset y)
                GameObject root = new GameObject(cleanName);
                GameObject visual = new GameObject("Visual");
                visual.transform.SetParent(root.transform, false);

                // Đặt pivot chuẩn ở đáy gốc cây/khóm cỏ
                float heightOffset = sprite.bounds.extents.y - 0.03f;
                visual.transform.localPosition = new Vector3(0f, Mathf.Max(0.01f, heightOffset), 0f);

                SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingLayerName = "Default";
                sr.sortingOrder = sortingOrder;

                // Gán material
                if (categoryKey == "Grass" && grassMat != null && (lower.Contains("highgrass") || lower.Contains("forest_001") || lower.Contains("forest_006")))
                {
                    sr.sharedMaterial = grassMat;
                }
                else if (defaultMat != null)
                {
                    sr.sharedMaterial = defaultMat;
                }

                GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                GameObject.DestroyImmediate(root);

                if (prefabAsset != null)
                {
                    createdPrefabs.Add(prefabAsset);
                }
            }

            return createdPrefabs;
        }

        private static void StageAdditiveMeadowInScene(List<GameObject> grassPrefabs, List<GameObject> flowerPrefabs, List<GameObject> saplingPrefabs, List<GameObject> bushPrefabs)
        {
            var walkway = GameObject.Find("03_Terrain/PhysicsWalkway_And_Cliffs_145m");
            if (walkway == null)
            {
                Debug.LogError("[PlanetOfLanaFoliageBuilder] Không tìm thấy PhysicsWalkway_And_Cliffs_145m để lấy cao độ mặt đất!");
                return;
            }

            var colliders = walkway.GetComponentsInChildren<BoxCollider>();
            var validCols = new List<BoxCollider>();
            foreach (var c in colliders)
            {
                if (!c.name.StartsWith("Wall_")) validCols.Add(c);
            }

            float SampleGroundY(float x)
            {
                float bestY = -999f;
                foreach (var c in validCols)
                {
                    if (x >= c.bounds.min.x - 0.05f && x <= c.bounds.max.x + 0.05f)
                    {
                        if (c.bounds.max.y > bestY) bestY = c.bounds.max.y;
                    }
                }
                return bestY;
            }

            // Nguyên tắc Rule 4.3: Bảo tồn scene gốc, chỉ thêm vào parent riêng biệt
            const string AdditiveRootName = "[Official_Lana_Foliage_Meadow_Additive]";
            GameObject existingAdditive = GameObject.Find(AdditiveRootName);
            if (existingAdditive != null)
            {
                Undo.DestroyObjectImmediate(existingAdditive);
            }

            GameObject additiveRoot = new GameObject(AdditiveRootName);
            Undo.RegisterCreatedObjectUndo(additiveRoot, "Stage Lana Meadow Additive");
            additiveRoot.transform.position = Vector3.zero;

            GameObject grassGroup = new GameObject("01_Grass_Tufts_And_Edges");
            grassGroup.transform.SetParent(additiveRoot.transform, false);

            GameObject flowerGroup = new GameObject("02_Wildflower_Fields");
            flowerGroup.transform.SetParent(additiveRoot.transform, false);

            GameObject saplingGroup = new GameObject("03_Saplings_And_YoungTrees");
            saplingGroup.transform.SetParent(additiveRoot.transform, false);

            GameObject bushGroup = new GameObject("04_Bushes_And_Shrubs");
            bushGroup.transform.SetParent(additiveRoot.transform, false);

            // Phân loại nhóm hoa theo màu sắc
            var yellowFlowers = flowerPrefabs.FindAll(p => p.name.ToLower().Contains("yellow"));
            var whiteFlowers = flowerPrefabs.FindAll(p => p.name.ToLower().Contains("white") || p.name.ToLower().Contains("daisy") || p.name.ToLower().Contains("ball"));
            var purpleFlowers = flowerPrefabs.FindAll(p => p.name.ToLower().Contains("purple") || p.name.ToLower().Contains("pink"));
            var blueFlowers = flowerPrefabs.FindAll(p => p.name.ToLower().Contains("blue"));
            var weedFlowers = flowerPrefabs.FindAll(p => p.name.ToLower().Contains("weed"));

            // Phân loại nhóm cỏ theo sắc thái
            var darkGrass = grassPrefabs.FindAll(p => p.name.ToLower().Contains("dark"));
            var forestGrass = grassPrefabs.FindAll(p => p.name.ToLower().Contains("forest"));
            var highGrass = grassPrefabs.FindAll(p => p.name.ToLower().Contains("highgrass"));

            int spawnedGrass = 0;
            int spawnedFlowers = 0;
            int spawnedSaplings = 0;
            int spawnedBushes = 0;

            // Phạm vi dàn dựng: từ X = -10.0m đến X = 95.0m
            float startX = -10.0f;
            float endX = 95.0f;
            float step = 0.38f;

            int stepIndex = 0;
            for (float x = startX; x <= endX; x += step)
            {
                stepIndex++;
                float groundY = SampleGroundY(x);
                if (groundY <= -900f) continue;

                // Bỏ qua khe vực Ravine Floor dốc đứng nếu muốn chừa khoảng nhảy
                bool isRavineCliff = (x >= 14.5f && x <= 14.8f) || (x >= 17.3f && x <= 17.6f);
                if (isRavineCliff) continue;

                // 1. Phủ viền Cỏ (Grass Tufts) dọc mép đất
                if (grassPrefabs.Count > 0)
                {
                    float jitterX = Mathf.Sin(x * 12.3f) * 0.08f;
                    float posX = x + jitterX;
                    float posY = SampleGroundY(posX);

                    GameObject selectedGrassPrefab = null;
                    if (stepIndex % 3 == 0 && darkGrass.Count > 0) selectedGrassPrefab = darkGrass[stepIndex % darkGrass.Count];
                    else if (stepIndex % 2 == 0 && forestGrass.Count > 0) selectedGrassPrefab = forestGrass[stepIndex % forestGrass.Count];
                    else if (highGrass.Count > 0) selectedGrassPrefab = highGrass[stepIndex % highGrass.Count];
                    else selectedGrassPrefab = grassPrefabs[stepIndex % grassPrefabs.Count];

                    if (selectedGrassPrefab != null)
                    {
                        GameObject grassInstance = (GameObject)PrefabUtility.InstantiatePrefab(selectedGrassPrefab, grassGroup.transform);
                        bool isHighGrass = selectedGrassPrefab.name.ToLower().Contains("highgrass");
                        float scaleJitter = isHighGrass 
                            ? (0.40f + Mathf.Abs(Mathf.Sin(x * 5.7f)) * 0.08f)
                            : (0.32f + Mathf.Abs(Mathf.Sin(x * 5.7f)) * 0.06f);
                        float flip = (stepIndex % 2 == 0) ? -1f : 1f;
                        grassInstance.transform.position = new Vector3(posX, posY, -0.08f);
                        grassInstance.transform.localScale = new Vector3(scaleJitter * flip, scaleJitter, 1f);
                        grassInstance.transform.rotation = Quaternion.Euler(0, 0, Mathf.Sin(x * 7.9f) * 4f);
                        spawnedGrass++;
                    }
                }

                // 2. Điểm xuyết Hoa Dại (Wildflowers) theo từng cụm tự nhiên
                if (flowerPrefabs.Count > 0)
                {
                    List<GameObject> colorPool = null;
                    if (x < 5.0f) colorPool = (stepIndex % 3 == 0) ? yellowFlowers : whiteFlowers;
                    else if (x < 20.0f) colorPool = (stepIndex % 2 == 0) ? whiteFlowers : weedFlowers;
                    else if (x < 42.0f) colorPool = (stepIndex % 3 == 0) ? purpleFlowers : yellowFlowers;
                    else if (x < 62.0f) colorPool = (stepIndex % 4 == 0) ? blueFlowers : yellowFlowers;
                    else colorPool = (stepIndex % 2 == 0) ? whiteFlowers : yellowFlowers;

                    if (colorPool != null && colorPool.Count > 0 && (stepIndex % 2 == 0))
                    {
                        GameObject flowerPrefab = colorPool[(stepIndex / 2) % colorPool.Count];
                        float flowerX = x + Mathf.Cos(x * 9.1f) * 0.10f;
                        float flowerY = SampleGroundY(flowerX);

                        GameObject flowerInstance = (GameObject)PrefabUtility.InstantiatePrefab(flowerPrefab, flowerGroup.transform);
                        float flScale = 0.26f + Mathf.Abs(Mathf.Cos(x * 6.3f)) * 0.08f;
                        float flFlip = (stepIndex % 3 == 0) ? -1f : 1f;
                        flowerInstance.transform.position = new Vector3(flowerX, flowerY, -0.14f);
                        flowerInstance.transform.localScale = new Vector3(flScale * flFlip, flScale, 1f);
                        flowerInstance.transform.rotation = Quaternion.Euler(0, 0, Mathf.Sin(x * 5.2f) * 6f);
                        spawnedFlowers++;
                    }
                }

                // 3. Cây Con (Saplings) mọc thưa thớt ở chân dốc, gờ đá và thềm đất
                if (saplingPrefabs.Count > 0 && (stepIndex % 8 == 3))
                {
                    GameObject saplingPrefab = saplingPrefabs[(stepIndex / 8) % saplingPrefabs.Count];
                    float saplingX = x + 0.12f;
                    float saplingY = SampleGroundY(saplingX);

                    GameObject saplingInstance = (GameObject)PrefabUtility.InstantiatePrefab(saplingPrefab, saplingGroup.transform);
                    float spScale = 0.38f + Mathf.Abs(Mathf.Sin(x * 3.4f)) * 0.10f;
                    float spFlip = (stepIndex % 2 == 0) ? -1f : 1f;
                    saplingInstance.transform.position = new Vector3(saplingX, saplingY, -0.05f);
                    saplingInstance.transform.localScale = new Vector3(spScale * spFlip, spScale, 1f);
                    spawnedSaplings++;
                }

                // 4. Bụi Cỏ / Bụi thấp (Bushes) ôm chân đá và các góc phẳng
                if (bushPrefabs.Count > 0 && (stepIndex % 6 == 1))
                {
                    GameObject bushPrefab = bushPrefabs[(stepIndex / 6) % bushPrefabs.Count];
                    float bushX = x - 0.10f;
                    float bushY = SampleGroundY(bushX);

                    GameObject bushInstance = (GameObject)PrefabUtility.InstantiatePrefab(bushPrefab, bushGroup.transform);
                    float bScale = 0.32f + Mathf.Abs(Mathf.Cos(x * 4.1f)) * 0.08f;
                    float bFlip = (stepIndex % 2 == 0) ? -1f : 1f;
                    bushInstance.transform.position = new Vector3(bushX, bushY, -0.07f);
                    bushInstance.transform.localScale = new Vector3(bScale * bFlip, bScale, 1f);
                    spawnedBushes++;
                }
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            Debug.Log($"[PlanetOfLanaFoliageBuilder] DÀN DỰNG THÀNH CÔNG ADDITIVE LÊN SCENE:\n" +
                      $"- Khóm cỏ viền đất: {spawnedGrass} objects\n" +
                      $"- Thảm hoa dại tự nhiên: {spawnedFlowers} objects\n" +
                      $"- Cây con dáng thanh mảnh: {spawnedSaplings} objects\n" +
                      $"- Bụi cỏ thấp & đá: {spawnedBushes} objects\n" +
                      $"=> Toàn bộ được gom trong nhóm riêng biệt: {AdditiveRootName}");
        }
    }
}
