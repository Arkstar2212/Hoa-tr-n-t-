using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace HoaDo.EditorTools
{
    public static class PlanetOfLanaAssetPipeline
    {
        private const string ExtractedSourcePath = @"C:\Users\Arkstarz\Downloads\Lana_Extracted_Test\Filtered\Sprite";
        private const string TargetProjectBasePath = "Assets/Art/PlanetOfLana_Official";

        [MenuItem("Tools/Planet of Lana/1. Import & Organize Official Assets", priority = 10)]
        public static void ImportAndOrganizeAssets()
        {
            if (!Directory.Exists(ExtractedSourcePath))
            {
                Debug.LogError($"[PlanetOfLanaAssetPipeline] Could not find extracted assets at {ExtractedSourcePath}");
                return;
            }

            string[] categoryFolders = new string[]
            {
                "Flora",
                "Grass",
                "Bushes",
                "Trees",
                "Ground_Rocks",
                "Foreground"
            };

            foreach (var cat in categoryFolders)
            {
                string targetFolder = Path.Combine(TargetProjectBasePath, cat).Replace("\\", "/");
                if (!AssetDatabase.IsValidFolder(targetFolder))
                {
                    if (!AssetDatabase.IsValidFolder(TargetProjectBasePath))
                    {
                        if (!AssetDatabase.IsValidFolder("Assets/Art"))
                        {
                            AssetDatabase.CreateFolder("Assets", "Art");
                        }
                        AssetDatabase.CreateFolder("Assets/Art", "PlanetOfLana_Official");
                    }
                    AssetDatabase.CreateFolder(TargetProjectBasePath, cat);
                }
            }

            string[] sourceFiles = Directory.GetFiles(ExtractedSourcePath, "*.png");
            int copiedCount = 0;

            foreach (string filePath in sourceFiles)
            {
                string fileName = Path.GetFileName(filePath);
                string category = ClassifyAsset(fileName);
                string destRelativePath = $"{TargetProjectBasePath}/{category}/{fileName}".Replace("\\", "/");
                string destFullPath = Path.Combine(Application.dataPath.Replace("/Assets", ""), destRelativePath);

                if (!File.Exists(destFullPath))
                {
                    File.Copy(filePath, destFullPath, true);
                    copiedCount++;
                }
            }

            AssetDatabase.Refresh();

            Debug.Log($"[PlanetOfLanaAssetPipeline] Successfully imported and configured {copiedCount} authentic Planet of Lana sprites across categories.");
        }

        private static string ClassifyAsset(string fileName)
        {
            string lower = fileName.ToLowerInvariant();

            if (lower.Contains("fgplant") || lower.Contains("_black") || lower.Contains("foreground"))
            {
                return "Foreground";
            }
            if (lower.Contains("flower") || lower.Contains("weed") || lower.Contains("petal"))
            {
                return "Flora";
            }
            if (lower.Contains("grass") || lower.Contains("reed") || lower.Contains("carpet"))
            {
                return "Grass";
            }
            if (lower.Contains("bush") || lower.Contains("rockplant") || lower.Contains("plant") || lower.Contains("leaf") || lower.Contains("leaves") || lower.Contains("oaksmall"))
            {
                return "Bushes";
            }
            if (lower.Contains("tree") || lower.Contains("trunk") || lower.Contains("stem") || lower.Contains("branch"))
            {
                return "Trees";
            }
            if (lower.Contains("ground") || lower.Contains("rock") || lower.Contains("moss") || lower.Contains("plane") || lower.Contains("cliff"))
            {
                return "Ground_Rocks";
            }

            return "Bushes";
        }

        public static float GetPlatformSurfaceY(float x)
        {
            // Samples the top profile of Ledge_01_ForestStart
            if (x <= -5.0f)
            {
                return -1.03f;
            }
            else if (x <= -2.0f)
            {
                float t = (x - (-5.0f)) / 3.0f;
                return Mathf.Lerp(-1.03f, -1.96f, t);
            }
            else if (x <= 4.2f)
            {
                float t = (x - (-2.0f)) / 6.2f;
                return Mathf.Lerp(-1.96f, -2.09f, t);
            }
            else if (x <= 5.5f)
            {
                float t = (x - 4.2f) / 1.3f;
                return Mathf.Lerp(-2.09f, -0.70f, t);
            }
            else
            {
                return -0.70f;
            }
        }

        [MenuItem("Tools/Planet of Lana/2. Stage Authentic Forest Platform & Wildflowers", priority = 20)]
        public static void StageAuthenticPlatform()
        {
            GameObject ledge01 = GameObject.Find("Ledge_01_ForestStart");
            if (ledge01 == null)
            {
                Debug.LogError("[PlanetOfLanaAssetPipeline] Ledge_01_ForestStart not found in scene!");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(ledge01, "Stage Lana Authentic Platform");

            // Clean up old obsolete duplicate grass carpets
            var oldCarpets = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
            int cleanedCount = 0;
            foreach (var sr in oldCarpets)
            {
                if (sr != null && sr.name.StartsWith("T_LanaVN_GrassCarpet_02_Tall") && (sr.name.Contains("(") || sr.transform.parent == ledge01.transform))
                {
                    Undo.DestroyObjectImmediate(sr.gameObject);
                    cleanedCount++;
                }
            }

            // Clean up existing foliage parent
            Transform oldFoliage = ledge01.transform.Find("[Official_Lana_Foliage]");
            if (oldFoliage != null)
            {
                Undo.DestroyObjectImmediate(oldFoliage.gameObject);
            }

            GameObject foliageGO = new GameObject("[Official_Lana_Foliage]");
            Undo.RegisterCreatedObjectUndo(foliageGO, "Create [Official_Lana_Foliage]");
            foliageGO.transform.SetParent(ledge01.transform, true);
            foliageGO.transform.position = Vector3.zero;

            // Load official sprites
            Sprite yellowButtercup = LoadSpriteByPattern("RockFlowerYellow01") ?? LoadSpriteByPattern("RockFlowerYellow04");
            Sprite yellowButtercupAlt = LoadSpriteByPattern("RockFlowerYellow07") ?? LoadSpriteByPattern("RockFlowerYellow05");
            Sprite whiteDaisy = LoadSpriteByPattern("RockFlowerWhitePetal01") ?? LoadSpriteByPattern("RockFlowerWhite01");
            Sprite whiteSmallSprig = LoadSpriteByPattern("Std_Pg_WhiteSmallFlower02") ?? LoadSpriteByPattern("RockFlowerWhite02");
            Sprite broadleafWeed = LoadSpriteByPattern("Std_RockPlantWeed01") ?? LoadSpriteByPattern("Std_RockPlantWeed02");
            Sprite rockWeedAlt = LoadSpriteByPattern("Std_RockPlantWeed03") ?? LoadSpriteByPattern("Std_RockPlantWeed04");
            Sprite forestGrassLight = LoadSpriteByPattern("Std_Pg_NHL_GrassForest_001") ?? LoadSpriteByPattern("Std_Pg_NHL_GrassForest_002");
            Sprite forestGrassMed = LoadSpriteByPattern("Std_Pg_NHL_GrassForest_006") ?? LoadSpriteByPattern("Std_Pg_NHL_GrassForest_012");
            Sprite forestGrassDark = LoadSpriteByPattern("Std_Pg_NHL_GrassForest_013_dark") ?? LoadSpriteByPattern("Std_Pg_NHL_GrassForest_011_dark");
            Sprite groundMoss = LoadSpriteByPattern("GroundMoss_004") ?? LoadSpriteByPattern("GroundMossLight_008");

            // Platform walking range: from X: -6.5 (hill slope) to X: +4.2 (rock shelf edge)
            float startX = -6.5f;
            float endX = 4.2f;
            float step = 0.42f;

            int index = 0;
            for (float x = startX; x <= endX; x += step)
            {
                index++;
                float jitterX = (Mathf.Sin(x * 13.7f) * 0.08f);
                float posX = x + jitterX;
                float baseY = GetPlatformSurfaceY(posX);

                // Forest Grass tufts along the walking lip
                Sprite grassSprite = (index % 3 == 0) ? forestGrassDark : ((index % 3 == 1) ? forestGrassMed : forestGrassLight);
                if (grassSprite != null)
                {
                    GameObject grassObj = new GameObject($"GrassTuft_{index:D2}");
                    grassObj.transform.SetParent(foliageGO.transform, true);
                    grassObj.transform.position = new Vector3(posX, baseY + 0.08f, -0.1f);
                    float scale = 0.9f + (Mathf.Sin(x * 4.7f) * 0.18f);
                    grassObj.transform.localScale = new Vector3(scale * ((index % 2 == 0) ? 1f : -1f), scale, 1f);

                    SpriteRenderer sr = grassObj.AddComponent<SpriteRenderer>();
                    sr.sprite = grassSprite;
                    sr.sortingLayerName = "Environment";
                    sr.sortingOrder = 18;
                }

                // Scatter authentic Wildflower clusters (Yellow buttercups & White daisies)
                // Exactly matching media_1793066390425_ce7d9347.png
                if (index % 2 == 0)
                {
                    bool isYellow = (index % 4 == 0);
                    Sprite flowerSprite = isYellow ? ((index % 8 == 0) ? yellowButtercupAlt : yellowButtercup)
                                                   : ((index % 6 == 0) ? whiteDaisy : whiteSmallSprig);
                    if (flowerSprite != null)
                    {
                        GameObject flowerObj = new GameObject($"Wildflower_{(isYellow ? "Yellow" : "White")}_{index:D2}");
                        flowerObj.transform.SetParent(foliageGO.transform, true);
                        float offsetX = (isYellow ? 0.06f : -0.06f);
                        flowerObj.transform.position = new Vector3(posX + offsetX, baseY + 0.06f, -0.15f);
                        float flScale = 1.1f + (Mathf.Sin(x * 7.1f) * 0.25f);
                        flowerObj.transform.localScale = new Vector3(flScale * ((index % 3 == 0) ? -1f : 1f), flScale, 1f);
                        flowerObj.transform.rotation = Quaternion.Euler(0, 0, Mathf.Sin(x * 6.3f) * 8f);

                        SpriteRenderer sr = flowerObj.AddComponent<SpriteRenderer>();
                        sr.sprite = flowerSprite;
                        sr.sortingLayerName = "Environment";
                        sr.sortingOrder = 25;
                    }
                }

                // Broadleaf weed clusters (Plantain weed matching media_1793066371338_007bfdfb.png)
                if (index % 5 == 2)
                {
                    Sprite weedSprite = (index % 10 == 2) ? broadleafWeed : rockWeedAlt;
                    if (weedSprite != null)
                    {
                        GameObject weedObj = new GameObject($"BroadleafWeed_{index:D2}");
                        weedObj.transform.SetParent(foliageGO.transform, true);
                        weedObj.transform.position = new Vector3(posX - 0.1f, baseY + 0.05f, -0.12f);
                        weedObj.transform.localScale = new Vector3(0.65f * ((index % 2 == 0) ? 1f : -1f), 0.65f, 1f);

                        SpriteRenderer sr = weedObj.AddComponent<SpriteRenderer>();
                        sr.sprite = weedSprite;
                        sr.sortingLayerName = "Environment";
                        sr.sortingOrder = 22;
                    }
                }

                // Moss patches clinging to the lip
                if (index % 4 == 1 && groundMoss != null)
                {
                    GameObject mossObj = new GameObject($"LipMoss_{index:D2}");
                    mossObj.transform.SetParent(foliageGO.transform, true);
                    mossObj.transform.position = new Vector3(posX, baseY - 0.05f, -0.08f);
                    mossObj.transform.localScale = new Vector3(0.75f, 0.65f, 1f);

                    SpriteRenderer sr = mossObj.AddComponent<SpriteRenderer>();
                    sr.sprite = groundMoss;
                    sr.sortingLayerName = "Environment";
                    sr.sortingOrder = 16;
                }
            }

            // 3. Foreground Silhouette Framing at camera screen borders
            // Camera is at pos=(0.0, 0.27, -15.5)
            // Left corner framing: X = -7.5, Y = -2.8, Z = -3.0
            // Right corner framing: X = +7.8, Y = -2.9, Z = -3.0
            GameObject fgParent = GameObject.Find("[Official_Lana_Foreground_Framing]");
            if (fgParent != null)
            {
                Undo.DestroyObjectImmediate(fgParent);
            }

            fgParent = new GameObject("[Official_Lana_Foreground_Framing]");
            Undo.RegisterCreatedObjectUndo(fgParent, "Create [Official_Lana_Foreground_Framing]");
            fgParent.transform.position = Vector3.zero;

            Sprite fgLeaf14 = LoadSpriteByPattern("FGPlant14") ?? LoadSpriteByPattern("FGPlant16");
            Sprite fgLeaf26 = LoadSpriteByPattern("FGPlant26") ?? LoadSpriteByPattern("FGPlant24");
            Sprite fgLeaf04 = LoadSpriteByPattern("FGPlant04") ?? LoadSpriteByPattern("FGPlant05");

            // Bottom-left framing foliage
            if (fgLeaf26 != null)
            {
                GameObject fgLeft = new GameObject("FG_Silhouette_BottomLeft");
                fgLeft.transform.SetParent(fgParent.transform, true);
                fgLeft.transform.position = new Vector3(-7.2f, -2.6f, -3f);
                fgLeft.transform.localScale = new Vector3(1.3f, 1.3f, 1f);
                fgLeft.transform.rotation = Quaternion.Euler(0, 0, 18f);

                SpriteRenderer sr = fgLeft.AddComponent<SpriteRenderer>();
                sr.sprite = fgLeaf26;
                sr.sortingLayerName = "Foreground";
                sr.sortingOrder = 50;
                sr.color = new Color(0.06f, 0.10f, 0.08f, 0.96f);
            }

            // Extra corner fern leaf
            if (fgLeaf04 != null)
            {
                GameObject fgCorner = new GameObject("FG_Silhouette_CornerFern");
                fgCorner.transform.SetParent(fgParent.transform, true);
                fgCorner.transform.position = new Vector3(-8.1f, -3.0f, -2.8f);
                fgCorner.transform.localScale = new Vector3(1.1f, 1.1f, 1f);
                fgCorner.transform.rotation = Quaternion.Euler(0, 0, 35f);

                SpriteRenderer sr = fgCorner.AddComponent<SpriteRenderer>();
                sr.sprite = fgLeaf04;
                sr.sortingLayerName = "Foreground";
                sr.sortingOrder = 51;
                sr.color = new Color(0.05f, 0.08f, 0.07f, 0.98f);
            }

            // Bottom-right framing foliage
            if (fgLeaf14 != null)
            {
                GameObject fgRight = new GameObject("FG_Silhouette_BottomRight");
                fgRight.transform.SetParent(fgParent.transform, true);
                fgRight.transform.position = new Vector3(7.6f, -2.7f, -3f);
                fgRight.transform.localScale = new Vector3(-1.4f, 1.4f, 1f);
                fgRight.transform.rotation = Quaternion.Euler(0, 0, -12f);

                SpriteRenderer sr = fgRight.AddComponent<SpriteRenderer>();
                sr.sprite = fgLeaf14;
                sr.sortingLayerName = "Foreground";
                sr.sortingOrder = 50;
                sr.color = new Color(0.06f, 0.10f, 0.08f, 0.96f);
            }

            EditorUtility.SetDirty(ledge01);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(ledge01.scene);

            Debug.Log($"[PlanetOfLanaAssetPipeline] Staged authentic Planet of Lana platform foliage, wildflowers, and corner foreground framing! Cleaned {cleanedCount} obsolete raw sprites.");
        }

        private static Sprite LoadSpriteByPattern(string pattern)
        {
            string[] guids = AssetDatabase.FindAssets($"{pattern} t:Sprite", new string[] { TargetProjectBasePath });
            if (guids != null && guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            return null;
        }
    }
}
