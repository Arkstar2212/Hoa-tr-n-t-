#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace HoaVaMau.Editor
{
    public static class LanaBorderlessCleaner
    {
        private const string PackRoot = "Assets/Textures/Environment/Lana_VN_Pack";
        private const string UnifiedRoot = "Assets/Textures/Lana_Unified_Art";

        [MenuItem("Hoa va Mau/Art Pipeline/Clean All Borders & Build Unified Lana Pack")]
        public static void CleanAndBuildAll()
        {
            Directory.CreateDirectory(UnifiedRoot);
            Directory.CreateDirectory($"{UnifiedRoot}/Trees");
            Directory.CreateDirectory($"{UnifiedRoot}/Bushes");
            Directory.CreateDirectory($"{UnifiedRoot}/Foliage");
            Directory.CreateDirectory($"{UnifiedRoot}/Branches");
            Directory.CreateDirectory($"{UnifiedRoot}/Rice");
            Directory.CreateDirectory($"{UnifiedRoot}/Clouds");

            // 1. Clean all 54 Lana_VN_Pack textures in-place so EVERY prefab in Lana_VN_Pack has ZERO border ("không có viền")
            string[] subfolders = new[] { "Trees", "Bushes", "Wildflowers", "Grass_Vines", "Rocks", "GourdTrellis" };
            int cleanedCount = 0;
            foreach (var sub in subfolders)
            {
                string dir = Path.Combine(Application.dataPath, "..", PackRoot, sub);
                if (!Directory.Exists(dir)) continue;
                bool isGourdSheet = (sub == "GourdTrellis");
                foreach (var file in Directory.GetFiles(dir, "*.png"))
                {
                    CleanSpriteBorderInPlace(file, isGourdSheet);
                    cleanedCount++;
                }
            }

            // 2. Create Golden Harvest Rice Field & Village foliage variations directly from the cleaned Planet of Lana sprites
            //    so that Forest, Hill, Rice Field, and Village share 100% identical Planet of Lana brushwork!
            GenerateGoldenHarvestVariations();

            // 3. Extract clean Planet of Lana cloud layers directly from BG_Lana_MainSkyHills_Official.png
            GenerateAuthenticCloudsFromMainBackground();

            // 4. Configure BG_Lana_MainSkyHills_Official.png & all sprites
            ConfigureMainBackgroundTexture();
            AssetDatabase.Refresh();
            LanaVNAssetPackBuilder.BuildAllAssetsAndPrefabs();
            LanaUnifiedArtGenerator.ConfigureAllSpritesImportSettings();
            AssetDatabase.SaveAssets();

            Debug.Log($"<color=#4ade80>[LanaBorderlessCleaner]</color> Cleaned {cleanedCount} Lana_VN_Pack sprites (zero borders/fringes) and generated unified golden/cloud variations!");
        }

        private static void CleanSpriteBorderInPlace(string fullFilePath, bool isGourdSheet)
        {
            byte[] raw = File.ReadAllBytes(fullFilePath);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(raw))
            {
                UnityEngine.Object.DestroyImmediate(tex);
                return;
            }

            int w = tex.width;
            int h = tex.height;
            Color[] px = tex.GetPixels();
            UnityEngine.Object.DestroyImmediate(tex);

            // If this is a GrassCarpet that had old horizontal seam-blending in the outer 88px, crop to the crisp center region
            if (fullFilePath.Contains("GrassCarpet") && w > 420)
            {
                int trimX = 88;
                int newW = w - trimX * 2;
                Color[] cropped = new Color[newW * h];
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < newW; x++)
                    {
                        Color c = px[y * w + (x + trimX)];
                        // Natural diagonal grass-blade taper at left and right ends (no vertical box edge)
                        int distEdgeX = Mathf.Min(x, newW - 1 - x);
                        float heightRatio = (float)y / Mathf.Max(1, h);
                        float bladeLimit = distEdgeX / 18f;
                        if (heightRatio > bladeLimit + 0.15f)
                        {
                            c.a = 0f;
                        }
                        cropped[y * newW + x] = c;
                    }
                }
                w = newW;
                px = cropped;
            }

            Color sheet1Bg = new Color(0.13f, 0.185f, 0.215f, 1f);
            Color sheet2Bg = new Color(0.91f, 0.898f, 0.855f, 1f);

            // Step 0: Immediately kill all semi-transparent ghost/drop-shadow pixels (a < 0.82f)
            // so there is NEVER any translucent grey halo ring around trees, bushes, or grass!
            for (int i = 0; i < px.Length; i++)
            {
                if (px[i].a < 0.82f)
                {
                    px[i] = new Color(0f, 0f, 0f, 0f);
                }
            }

            // Step 1: Compute distance (in pixels) from every pixel to the nearest transparent pixel (a < 0.82)
            int[] dist = new int[w * h];
            const int INF = 9999;

            for (int y = 0; y < h; y++)
            {
                int row = y * w;
                for (int x = 0; x < w; x++)
                {
                    int idx = row + x;
                    bool isBorderOrTrans = (px[idx].a < 0.82f) || x == 0 || x == w - 1 || y == 0 || y == h - 1;
                    dist[idx] = isBorderOrTrans ? 0 : INF;
                }
            }

            // Forward pass
            for (int y = 1; y < h - 1; y++)
            {
                int row = y * w;
                for (int x = 1; x < w - 1; x++)
                {
                    int idx = row + x;
                    int d = dist[idx];
                    if (d == 0) continue;
                    int dLeft = dist[idx - 1] + 3;
                    int dDown = dist[idx - w] + 3;
                    int dDL   = dist[idx - w - 1] + 4;
                    int dDR   = dist[idx - w + 1] + 4;
                    int m = Mathf.Min(Mathf.Min(dLeft, dDown), Mathf.Min(dDL, dDR));
                    if (m < d) dist[idx] = m;
                }
            }

            // Backward pass
            for (int y = h - 2; y >= 1; y--)
            {
                int row = y * w;
                for (int x = w - 2; x >= 1; x--)
                {
                    int idx = row + x;
                    int d = dist[idx];
                    if (d == 0) continue;
                    int dRight = dist[idx + 1] + 3;
                    int dUp    = dist[idx + w] + 3;
                    int dUL    = dist[idx + w - 1] + 4;
                    int dUR    = dist[idx + w + 1] + 4;
                    int m = Mathf.Min(Mathf.Min(dRight, dUp), Mathf.Min(dUL, dUR));
                    if (m < d) dist[idx] = m;
                }
            }

            // Step 2: Completely zero out any dark slate or cream/grey border pixel within 7px of the edge (NO partial alpha ghost!)
            for (int i = 0; i < px.Length; i++)
            {
                Color c = px[i];
                if (c.a <= 0.01f)
                {
                    px[i] = new Color(0f, 0f, 0f, 0f);
                    continue;
                }

                int d = dist[i]; // ~3 units per pixel
                if (d <= 24) // Within 8 pixels of outer alpha boundary
                {
                    float lum = 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
                    float maxCh = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
                    float minCh = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
                    float sat = (maxCh > 0.01f) ? (maxCh - minCh) / maxCh : 0f;
                    float greenDom = c.g - Mathf.Max(c.r, c.b);
                    float warmDom = (c.r + c.g) * 0.5f - c.b;

                    float distBg1 = Mathf.Sqrt((c.r - sheet1Bg.r) * (c.r - sheet1Bg.r) + (c.g - sheet1Bg.g) * (c.g - sheet1Bg.g) + (c.b - sheet1Bg.b) * (c.b - sheet1Bg.b));
                    float distBg2 = Mathf.Sqrt((c.r - sheet2Bg.r) * (c.r - sheet2Bg.r) + (c.g - sheet2Bg.g) * (c.g - sheet2Bg.g) + (c.b - sheet2Bg.b) * (c.b - sheet2Bg.b));

                    bool isDarkBorderHalo = (distBg1 < 0.18f) || (lum < 0.25f && greenDom < 0.04f && warmDom < 0.05f);
                    bool isGreyOrWhiteHalo = (distBg2 < 0.20f) || (sat < 0.16f && greenDom < 0.03f && !fullFilePath.Contains("Rock") && !fullFilePath.Contains("White") && !fullFilePath.Contains("Daisy"));

                    if (d <= 3 || isDarkBorderHalo || isGreyOrWhiteHalo)
                    {
                        c = new Color(0f, 0f, 0f, 0f);
                    }
                    else
                    {
                        c.a = 1f;
                    }
                    px[i] = c;
                }
                else
                {
                    c.a = 1f;
                    px[i] = c;
                }
            }

            // Step 2b: Apply a clean 1-pixel anti-aliased edge on the newly cleaned solid silhouette
            Color[] solidSnapshot = (Color[])px.Clone();
            for (int y = 1; y < h - 1; y++)
            {
                int row = y * w;
                for (int x = 1; x < w - 1; x++)
                {
                    int idx = row + x;
                    if (solidSnapshot[idx].a < 0.5f) continue;
                    int emptyNeighbors = 0;
                    if (solidSnapshot[idx - 1].a < 0.5f) emptyNeighbors++;
                    if (solidSnapshot[idx + 1].a < 0.5f) emptyNeighbors++;
                    if (solidSnapshot[idx - w].a < 0.5f) emptyNeighbors++;
                    if (solidSnapshot[idx + w].a < 0.5f) emptyNeighbors++;

                    if (emptyNeighbors >= 3)
                    {
                        // Isolated single protruding pixel -> remove
                        px[idx] = new Color(0f, 0f, 0f, 0f);
                    }
                    else if (emptyNeighbors >= 1)
                    {
                        Color c = px[idx];
                        c.a = 0.88f;
                        px[idx] = c;
                    }
                }
            }

            // Step 3: Recompute distance to clean interior pixels (where alpha > 0.85 and not a dark/white border)
            // and propagate clean interior RGB outward into the 4px boundary and alpha=0 pixels (Color Decontamination & Dilation)!
            Color[] cleanPx = (Color[])px.Clone();
            Color[] tempPx  = new Color[px.Length];

            // Mark boundary pixels (within 3px of alpha < 0.5) to receive clean interior RGB
            bool[] isCore = new bool[px.Length];
            for (int y = 2; y < h - 2; y++)
            {
                int row = y * w;
                for (int x = 2; x < w - 2; x++)
                {
                    int idx = row + x;
                    Color c = cleanPx[idx];
                    if (c.a < 0.85f) continue;

                    // Check if all 8 neighbors also have high alpha
                    bool allSolid = true;
                    for (int dy = -2; dy <= 2 && allSolid; dy++)
                    {
                        for (int dx = -2; dx <= 2; dx++)
                        {
                            if (cleanPx[(y + dy) * w + (x + dx)].a < 0.65f)
                            {
                                allSolid = false;
                                break;
                            }
                        }
                    }
                    float lum = 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
                    float greenDom = c.g - Mathf.Max(c.r, c.b);
                    bool notSlate = (lum > 0.22f || greenDom > 0.03f);
                    if (allSolid && notSlate)
                    {
                        isCore[idx] = true;
                    }
                }
            }

            // Propagate core interior RGB outward for 8 passes so edge pixels and transparent neighbors have pure leaf/bark RGB
            bool[] hasValidRgb = (bool[])isCore.Clone();
            bool[] nextValidRgb = new bool[px.Length];

            for (int pass = 0; pass < 8; pass++)
            {
                Array.Copy(cleanPx, tempPx, cleanPx.Length);
                Array.Copy(hasValidRgb, nextValidRgb, hasValidRgb.Length);

                for (int y = 0; y < h; y++)
                {
                    int row = y * w;
                    for (int x = 0; x < w; x++)
                    {
                        int idx = row + x;
                        if (hasValidRgb[idx]) continue;

                        float sr = 0f, sg = 0f, sb = 0f, sw = 0f;
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            int ny = y + dy;
                            if ((uint)ny >= (uint)h) continue;
                            int nRow = ny * w;
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                if (dx == 0 && dy == 0) continue;
                                int nx = x + dx;
                                if ((uint)nx >= (uint)w) continue;
                                int nIdx = nRow + nx;
                                if (hasValidRgb[nIdx])
                                {
                                    Color nc = cleanPx[nIdx];
                                    float wgt = (dx == 0 || dy == 0) ? 1.0f : 0.7f;
                                    sr += nc.r * wgt;
                                    sg += nc.g * wgt;
                                    sb += nc.b * wgt;
                                    sw += wgt;
                                }
                            }
                        }

                        if (sw > 0f)
                        {
                            float origA = cleanPx[idx].a;
                            tempPx[idx] = new Color(sr / sw, sg / sw, sb / sw, origA);
                            nextValidRgb[idx] = true;
                        }
                    }
                }

                Array.Copy(tempPx, cleanPx, cleanPx.Length);
                Array.Copy(nextValidRgb, hasValidRgb, hasValidRgb.Length);
            }

            Texture2D outTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            outTex.SetPixels(cleanPx);
            outTex.Apply();
            byte[] outPng = outTex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(outTex);
            File.WriteAllBytes(fullFilePath, outPng);
        }

        // =========================================================================================
        // GENERATE GOLDEN HARVEST RICE FIELD & VILLAGE VARIATIONS FROM CLEANED LANA_VN_PACK
        // =========================================================================================
        private static void GenerateGoldenHarvestVariations()
        {
            // Transform emerald foliage into warm Planet of Lana golden harvest amber/olive while preserving wood trunks!
            void CreateGoldenVariant(string srcRelPath, string dstRelPath, float goldStrength = 1.0f)
            {
                string srcFull = Path.Combine(Application.dataPath, "..", srcRelPath);
                if (!File.Exists(srcFull)) return;

                byte[] raw = File.ReadAllBytes(srcFull);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.LoadImage(raw);
                int w = tex.width, h = tex.height;
                Color[] px = tex.GetPixels();
                UnityEngine.Object.DestroyImmediate(tex);

                for (int i = 0; i < px.Length; i++)
                {
                    Color c = px[i];
                    if (c.a <= 0.001f) continue;

                    // Detect green foliage vs brown bark/trunk
                    float greenness = Mathf.Clamp01((c.g - c.b) * 2.2f);
                    float foliageMask = Mathf.Clamp01((c.g - c.r * 0.78f) * 3.5f) * greenness;
                    float t = foliageMask * goldStrength;

                    if (t > 0.01f)
                    {
                        float lum = 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
                        // Warm golden-amber harvest palette matching Planet of Lana golden fields
                        Color goldShadow = new Color(0.38f, 0.36f, 0.15f, c.a);
                        Color goldMid    = new Color(0.76f, 0.64f, 0.22f, c.a);
                        Color goldBright = new Color(0.98f, 0.85f, 0.34f, c.a);

                        Color targetGold = (lum < 0.42f)
                            ? Color.Lerp(goldShadow, goldMid, Mathf.InverseLerp(0.12f, 0.42f, lum))
                            : Color.Lerp(goldMid, goldBright, Mathf.InverseLerp(0.42f, 0.78f, lum));

                        px[i] = new Color(
                            Mathf.Lerp(c.r, targetGold.r, t),
                            Mathf.Lerp(c.g, targetGold.g, t),
                            Mathf.Lerp(c.b, targetGold.b, t),
                            c.a
                        );
                    }
                }

                Texture2D outTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                outTex.SetPixels(px);
                outTex.Apply();
                string dstFull = Path.Combine(Application.dataPath, "..", dstRelPath);
                Directory.CreateDirectory(Path.GetDirectoryName(dstFull));
                File.WriteAllBytes(dstFull, outTex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(outTex);
            }

            // Golden Rice Carpets & Reeds (from cleaned GrassCarpet & Wildflower Reeds)
            CreateGoldenVariant($"{PackRoot}/Grass_Vines/T_LanaVN_GrassCarpet_02_Tall.png",   $"{UnifiedRoot}/Rice/T_Lana_Rice_Carpet_01_TallSway.png", 1.0f);
            CreateGoldenVariant($"{PackRoot}/Grass_Vines/T_LanaVN_GrassCarpet_03.png",        $"{UnifiedRoot}/Rice/T_Lana_Rice_Carpet_02_Medium.png", 1.0f);
            CreateGoldenVariant($"{PackRoot}/Grass_Vines/T_LanaVN_GrassCarpet_05_Dense.png",  $"{UnifiedRoot}/Rice/T_Lana_Rice_Carpet_03_DenseHead.png", 1.0f);
            CreateGoldenVariant($"{PackRoot}/Wildflowers/T_LanaVN_Wildflower_02_TallGrass.png", $"{UnifiedRoot}/Rice/T_Lana_Rice_Panicle_04_Tall.png", 1.0f);
            CreateGoldenVariant($"{PackRoot}/Wildflowers/T_LanaVN_Wildflower_01_Reeds.png",   $"{UnifiedRoot}/Rice/T_Lana_Rice_Panicle_05_Cluster.png", 0.95f);

            // Golden-Olive Village Trees & Bushes
            CreateGoldenVariant($"{PackRoot}/Trees/T_LanaVN_Tree_01_LargeCanopy.png", $"{UnifiedRoot}/Trees/T_Lana_Tree_GoldenVillage_01.png", 0.65f);
            CreateGoldenVariant($"{PackRoot}/Trees/T_LanaVN_Tree_02_Medium.png",      $"{UnifiedRoot}/Trees/T_Lana_Tree_GoldenVillage_02.png", 0.65f);
            CreateGoldenVariant($"{PackRoot}/Bushes/T_LanaVN_Bush_Low_01.png",        $"{UnifiedRoot}/Bushes/T_Lana_Bush_GoldenVillage_01.png", 0.72f);
            CreateGoldenVariant($"{PackRoot}/Bushes/T_LanaVN_Bush_Low_03.png",        $"{UnifiedRoot}/Bushes/T_Lana_Bush_GoldenVillage_02.png", 0.72f);
        }

        // =========================================================================================
        // EXTRACT AUTHENTIC CLOUDS FROM USER'S OFFICIAL BACKGROUND (22ffd054-1ca7-406c-9d11-0b93ba28b71a.png)
        // =========================================================================================
        private static void GenerateAuthenticCloudsFromMainBackground()
        {
            string bgPath = Path.Combine(Application.dataPath, "Textures/Backgrounds_Lana/BG_Lana_MainSkyHills_Official.png");
            if (!File.Exists(bgPath)) return;

            byte[] raw = File.ReadAllBytes(bgPath);
            Texture2D bgTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            bgTex.LoadImage(raw);
            int bw = bgTex.width, bh = bgTex.height;
            Color[] bgPx = bgTex.GetPixels();
            UnityEngine.Object.DestroyImmediate(bgTex);

            // Extract a region [u0..u1, v0..v1] of the sky and convert the blue sky into smooth transparency
            // so the cloud brush strokes from the user's background become a floating parallax cloud sprite!
            void ExtractCloudRegion(string outRelPath, float u0, float u1, float v0, float v1, bool flipX, Color tint)
            {
                int x0 = Mathf.Clamp(Mathf.RoundToInt(u0 * bw), 0, bw - 1);
                int x1 = Mathf.Clamp(Mathf.RoundToInt(u1 * bw), x0 + 1, bw);
                int y0 = Mathf.Clamp(Mathf.RoundToInt(v0 * bh), 0, bh - 1);
                int y1 = Mathf.Clamp(Mathf.RoundToInt(v1 * bh), y0 + 1, bh);

                int cw = x1 - x0;
                int ch = y1 - y0;
                Color[] outPx = new Color[cw * ch];

                for (int y = 0; y < ch; y++)
                {
                    float vy = (float)y / Mathf.Max(1, ch - 1);
                    float envY = Mathf.Sin(vy * Mathf.PI);
                    envY = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(envY * 1.6f));

                    int srcY = y0 + y;
                    for (int x = 0; x < cw; x++)
                    {
                        float vx = (float)x / Mathf.Max(1, cw - 1);
                        float envX = Mathf.Sin(vx * Mathf.PI);
                        envX = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(envX * 1.6f));

                        int srcX = flipX ? (x1 - 1 - x) : (x0 + x);
                        Color c = bgPx[srcY * bw + srcX];

                        // In the user's background, the sky has strong blue excess (c.b - c.r > 0.25),
                        // while the cloud brushwork has higher red/green luminance and lower blue excess.
                        float blueExcess = Mathf.Max(0f, c.b - c.r * 0.95f);
                        float lum = 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
                        float cloudMask = Mathf.Clamp01((lum - 0.42f) * 2.5f - blueExcess * 2.8f);
                        float a = cloudMask * envX * envY;

                        // Decontaminate blue sky color from semi-transparent cloud edges
                        Color pureCloud = new Color(
                            Mathf.Lerp(0.76f, Mathf.Max(c.r, 0.62f), cloudMask) * tint.r,
                            Mathf.Lerp(0.84f, Mathf.Max(c.g, 0.72f), cloudMask) * tint.g,
                            Mathf.Lerp(0.92f, Mathf.Max(c.b, 0.82f), cloudMask) * tint.b,
                            a * tint.a
                        );
                        outPx[y * cw + x] = pureCloud;
                    }
                }

                Texture2D outTex = new Texture2D(cw, ch, TextureFormat.RGBA32, false);
                outTex.SetPixels(outPx);
                outTex.Apply();
                string fullOut = Path.Combine(Application.dataPath, "..", outRelPath);
                File.WriteAllBytes(fullOut, outTex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(outTex);
            }

            // 1. Center Hero Cumulus Cloud from the user's background (u: 0.26..0.74, v: 0.34..0.84)
            ExtractCloudRegion($"{UnifiedRoot}/Clouds/T_Lana_Cloud_01_CumulusHero.png", 0.25f, 0.74f, 0.34f, 0.84f, false, Color.white);

            // 2. Flipped & Wide Cumulus Variation for Parallax Layering
            ExtractCloudRegion($"{UnifiedRoot}/Clouds/T_Lana_Cloud_02_WideBank.png", 0.22f, 0.98f, 0.32f, 0.82f, true, new Color(0.96f, 0.98f, 1.0f, 0.92f));

            // 3. Left & Right Mid-Sky Cloud Wisps for Sky Descent Pass-Through
            ExtractCloudRegion($"{UnifiedRoot}/Clouds/T_Lana_Cloud_03_HighWisps.png", 0.72f, 1.00f, 0.40f, 0.72f, false, new Color(0.95f, 0.98f, 1.0f, 0.88f));

            // 4. Warm Golden-Tinted Cumulus for Rice Field & Village Horizon
            ExtractCloudRegion($"{UnifiedRoot}/Clouds/T_Lana_Cloud_04_GoldenHorizon.png", 0.25f, 0.74f, 0.34f, 0.84f, true, new Color(1.00f, 0.95f, 0.84f, 0.95f));
        }

        private static void ConfigureMainBackgroundTexture()
        {
            string path = "Assets/Textures/Backgrounds_Lana/BG_Lana_MainSkyHills_Official.png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null)
            {
                imp.textureType = TextureImporterType.Default;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.filterMode = FilterMode.Bilinear;
                imp.mipmapEnabled = false;
                imp.maxTextureSize = 4096;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.SaveAndReimport();
            }
        }
    }
}
#endif
