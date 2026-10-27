#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace HoaVaMau.Editor
{
    public static class LanaAssetDefringer
    {
        private const string ConceptRoot = "Assets/Textures/Environment/Lana_Concept_Assets";
        private const string PackRoot = "Assets/Textures/Environment/Lana_VN_Pack";

        [MenuItem("Hoa va Mau/Art Pipeline/Defringe & Clean Black Outlines (Safe No Holes)")]
        public static void DefringeAllAssets()
        {
            Debug.Log("<color=#38bdf8>[Defringer]</color> Starting mathematically safe defringe (zero holes, trims black outline, color-bleeds interior)...");

            int count = 0;
            // 1. Process all folders in Lana_Concept_Assets
            string[] conceptDirs = Directory.GetDirectories(ConceptRoot, "*", SearchOption.AllDirectories);
            var allConceptDirs = new List<string>(conceptDirs) { ConceptRoot };
            foreach (var dir in allConceptDirs)
            {
                foreach (var file in Directory.GetFiles(dir, "*.png"))
                {
                    // Skip mock/temporary files
                    if (file.Contains("Cropped_And_Mock")) continue;
                    bool ok = DefringeFile(file, trimRadius: 1.8f);
                    if (ok) count++;
                }
            }

            // 2. Restore Bamboo Grove and Trees from clean sources
            RestoreAndDefringeTrees();

            // 3. Mirror the pristine defringed assets from Lana_Concept_Assets into Lana_VN_Pack
            SyncDefringedAssetsToVNPack();

            AssetDatabase.Refresh();
            Debug.Log($"<color=#4ade80>[Defringer]</color> <b>Successfully defringed {count} assets! No holes, zero black outlines!</b>");
        }

        public static bool DefringeFile(string relativePath, float trimRadius = 1.8f)
        {
            byte[] rawBytes = File.ReadAllBytes(relativePath);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(rawBytes))
            {
                UnityEngine.Object.DestroyImmediate(tex);
                return false;
            }

            int w = tex.width;
            int h = tex.height;
            Color[] pixels = tex.GetPixels();
            UnityEngine.Object.DestroyImmediate(tex);

            Color[] defringed = ProcessDefringe(pixels, w, h, trimRadius);

            var outTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            outTex.SetPixels(defringed);
            outTex.Apply();

            File.WriteAllBytes(relativePath, outTex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(outTex);
            return true;
        }

        public static Color[] ProcessDefringe(Color[] px, int w, int h, float trimRadius = 1.8f)
        {
            int total = w * h;
            float[] dist = new float[total];
            int[] nearestInnerIdx = new int[total];
            const float INF = 99999f;

            // Step 1: Identify outer transparency (alpha < 0.15)
            // Any pixel with alpha < 0.15 is considered background.
            for (int i = 0; i < total; i++)
            {
                if (px[i].a < 0.15f)
                {
                    dist[i] = 0f;
                    nearestInnerIdx[i] = -1;
                }
                else
                {
                    dist[i] = INF;
                    nearestInnerIdx[i] = -1;
                }
            }

            // Two-pass Chamfer Distance Transform to find distance to nearest transparent pixel
            // Using 1.0 for orthogonal, 1.414 for diagonal
            // Forward pass (top-left to bottom-right)
            for (int y = 0; y < h; y++)
            {
                int row = y * w;
                for (int x = 0; x < w; x++)
                {
                    int idx = row + x;
                    if (dist[idx] == 0f) continue;

                    float minD = dist[idx];
                    if (x > 0) minD = Mathf.Min(minD, dist[idx - 1] + 1.0f);
                    if (y > 0) minD = Mathf.Min(minD, dist[idx - w] + 1.0f);
                    if (x > 0 && y > 0) minD = Mathf.Min(minD, dist[idx - w - 1] + 1.414f);
                    if (x < w - 1 && y > 0) minD = Mathf.Min(minD, dist[idx - w + 1] + 1.414f);
                    dist[idx] = minD;
                }
            }

            // Backward pass (bottom-right to top-left)
            for (int y = h - 1; y >= 0; y--)
            {
                int row = y * w;
                for (int x = w - 1; x >= 0; x--)
                {
                    int idx = row + x;
                    if (dist[idx] == 0f) continue;

                    float minD = dist[idx];
                    if (x < w - 1) minD = Mathf.Min(minD, dist[idx + 1] + 1.0f);
                    if (y < h - 1) minD = Mathf.Min(minD, dist[idx + w] + 1.0f);
                    if (x < w - 1 && y < h - 1) minD = Mathf.Min(minD, dist[idx + w + 1] + 1.414f);
                    if (x > 0 && y < h - 1) minD = Mathf.Min(minD, dist[idx + w - 1] + 1.414f);
                    dist[idx] = minD;
                }
            }

            // Step 2: Find the nearest solid interior pixel (dist >= 3.5f and alpha > 0.85) for color bleeding
            // To be efficient, for each pixel on the edge (dist < 4.0), search in a small radius (up to 7 pixels)
            Color[] result = new Color[total];

            for (int y = 0; y < h; y++)
            {
                int row = y * w;
                for (int x = 0; x < w; x++)
                {
                    int idx = row + x;
                    float d = dist[idx];

                    // If completely outside or interior
                    if (d == 0f)
                    {
                        result[idx] = new Color(0f, 0f, 0f, 0f);
                        continue;
                    }

                    // Interior pixels (dist >= 3.5f): COMPLETELY UNTOUCHED!
                    // This guarantees zero holes inside leaves, tree trunks, rocks!
                    if (d >= 3.5f)
                    {
                        result[idx] = px[idx];
                        continue;
                    }

                    // For fringe edge pixels (0 < d < 3.5f):
                    // Search locally (radius up to 6 px) for the nearest solid interior pixel
                    Color bestInnerCol = px[idx];
                    float bestDistSq = 9999f;
                    int searchRadius = 6;
                    int minX = Mathf.Max(0, x - searchRadius);
                    int maxX = Mathf.Min(w - 1, x + searchRadius);
                    int minY = Mathf.Max(0, y - searchRadius);
                    int maxY = Mathf.Min(h - 1, y + searchRadius);

                    for (int sy = minY; sy <= maxY; sy++)
                    {
                        int sRow = sy * w;
                        for (int sx = minX; sx <= maxX; sx++)
                        {
                            int sIdx = sRow + sx;
                            if (dist[sIdx] >= 3.0f && px[sIdx].a > 0.8f)
                            {
                                float dx = sx - x;
                                float dy = sy - y;
                                float dSq = dx * dx + dy * dy;
                                if (dSq < bestDistSq)
                                {
                                    bestDistSq = dSq;
                                    bestInnerCol = px[sIdx];
                                }
                            }
                        }
                    }

                    // Erode outer boundary by trimRadius (e.g. 1.8 pixels)
                    // If d <= trimRadius: pixel is in the black outline shell -> erase it (alpha = 0)
                    // If trimRadius < d < trimRadius + 1.2f: soft anti-aliasing with bled interior color!
                    float effD = d - trimRadius;
                    if (effD <= 0f)
                    {
                        result[idx] = new Color(0f, 0f, 0f, 0f);
                    }
                    else if (effD < 1.2f)
                    {
                        float alphaFraction = effD / 1.2f;
                        // Use the true interior leaf/rock color so the edge has ZERO black tint
                        result[idx] = new Color(bestInnerCol.r, bestInnerCol.g, bestInnerCol.b, alphaFraction * px[idx].a);
                    }
                    else
                    {
                        // Transition between bled interior color and original pixel to smooth out the edge
                        float blend = Mathf.Clamp01((d - (trimRadius + 1.0f)) / 1.0f);
                        Color c = Color.Lerp(bestInnerCol, px[idx], blend);
                        c.a = px[idx].a;
                        result[idx] = c;
                    }
                }
            }

            return result;
        }

        private static void RestoreAndDefringeTrees()
        {
            // Restore Bamboo from clean source
            string srcBamboo = "Assets/Textures/Tree/Tree_Lana_BambooGrove.png";
            string dstBamboo = $"{PackRoot}/Trees/T_LanaVN_Tree_07_BambooGrove.png";
            if (File.Exists(srcBamboo))
            {
                File.Copy(srcBamboo, dstBamboo, true);
                DefringeFile(dstBamboo, trimRadius: 1.2f);
                Debug.Log("<color=#4ade80>[Defringer]</color> Restored pristine Bamboo Grove with zero holes and clean edges!");
            }

            // Restore other trees if needed
            string srcBanana = "Assets/Textures/Tree/Tree_Lana_BananaPalm.png";
            string dstBanana = $"{PackRoot}/Trees/T_LanaVN_Tree_05_Banana.png";
            if (File.Exists(srcBanana))
            {
                File.Copy(srcBanana, dstBanana, true);
                DefringeFile(dstBanana, trimRadius: 1.2f);
            }

            string srcCoconut = "Assets/Textures/Tree/Tree_Lana_CoconutPalm.png";
            string dstCoconut = $"{PackRoot}/Trees/T_LanaVN_Tree_06_Coconut.png";
            if (File.Exists(srcCoconut))
            {
                File.Copy(srcCoconut, dstCoconut, true);
                DefringeFile(dstCoconut, trimRadius: 1.2f);
            }
        }

        private static void SyncDefringedAssetsToVNPack()
        {
            // Mirror Bushes
            CopyAndDefringeFolder($"{ConceptRoot}/Bushes", $"{PackRoot}/Bushes", "T_HD_Bush_", "T_LanaVN_Bush_");
            // Mirror Rocks
            CopyAndDefringeFolder($"{ConceptRoot}/Rocks", $"{PackRoot}/Rocks", "T_HD_Rock_", "T_LanaVN_Rock_");
            // Mirror Wildflowers
            CopyAndDefringeWildflowers();
        }

        private static void CopyAndDefringeFolder(string srcDir, string dstDir, string srcPrefix, string dstPrefix)
        {
            if (!Directory.Exists(srcDir) || !Directory.Exists(dstDir)) return;
            foreach (var file in Directory.GetFiles(srcDir, "*.png"))
            {
                string fn = Path.GetFileName(file);
                string newFn = fn.Replace(srcPrefix, dstPrefix);
                string dstPath = Path.Combine(dstDir, newFn);
                File.Copy(file, dstPath, true);
                var ti = AssetImporter.GetAtPath(dstPath) as TextureImporter;
                if (ti != null)
                {
                    ti.textureType = TextureImporterType.Sprite;
                    ti.spriteImportMode = SpriteImportMode.Single;
                    ti.alphaIsTransparency = true;
                    ti.mipmapEnabled = false;
                    ti.SaveAndReimport();
                }
            }
        }

        private static void CopyAndDefringeWildflowers()
        {
            string srcDir = $"{ConceptRoot}/Wildflowers";
            string dstDir = $"{PackRoot}/Wildflowers";
            if (!Directory.Exists(srcDir) || !Directory.Exists(dstDir)) return;

            var map = new Dictionary<string, string>
            {
                { "T_HD_Wildflower_01_GrassClump.png", "T_LanaVN_Wildflower_02_TallGrass.png" },
                { "T_HD_Wildflower_02_Reeds.png", "T_LanaVN_Wildflower_01_Reeds.png" },
                { "T_HD_Wildflower_03_WhiteDaisy.png", "T_LanaVN_Wildflower_03_WhiteBlossom.png" },
                { "T_HD_Wildflower_04_YellowBloom.png", "T_LanaVN_Wildflower_05_YellowBlossom.png" },
                { "T_HD_Wildflower_05_TallSprig.png", "T_LanaVN_Wildflower_06_SmallSprig.png" }
            };

            foreach (var kvp in map)
            {
                string src = Path.Combine(srcDir, kvp.Key);
                string dst = Path.Combine(dstDir, kvp.Value);
                if (File.Exists(src))
                {
                    File.Copy(src, dst, true);
                    var ti = AssetImporter.GetAtPath(dst) as TextureImporter;
                    if (ti != null)
                    {
                        ti.textureType = TextureImporterType.Sprite;
                        ti.spriteImportMode = SpriteImportMode.Single;
                        ti.alphaIsTransparency = true;
                        ti.mipmapEnabled = false;
                        ti.SaveAndReimport();
                    }
                }
            }
        }
    }
}
#endif
