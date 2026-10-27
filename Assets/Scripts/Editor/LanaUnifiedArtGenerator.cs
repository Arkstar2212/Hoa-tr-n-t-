#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace HoaVaMau.Editor
{
    /// <summary>
    /// Generates a 100% unified, lineless, zero-fringe Planet of Lana 2D botanical & cloud asset library
    /// (Trees, Modular Canopies, Trunks, Branches, Bushes, Grass Tufts, Wildflowers, Golden Rice, Clouds).
    /// All textures are painted directly onto a transparent buffer with edge-color dilation so that
    /// bilinear filtering never produces white or dark outlines/fringes ("không có viền").
    /// </summary>
    public static class LanaUnifiedArtGenerator
    {
        public const string OutputRoot = "Assets/Textures/Lana_Unified_Art";

        [MenuItem("Hoa va Mau/Art Pipeline/Generate Unified Planet of Lana Botanical & Cloud Pack")]
        public static void GenerateAllAssets()
        {
            Directory.CreateDirectory(OutputRoot);
            Directory.CreateDirectory($"{OutputRoot}/Trees");
            Directory.CreateDirectory($"{OutputRoot}/Bushes");
            Directory.CreateDirectory($"{OutputRoot}/Foliage");
            Directory.CreateDirectory($"{OutputRoot}/Branches");
            Directory.CreateDirectory($"{OutputRoot}/Rice");
            Directory.CreateDirectory($"{OutputRoot}/Clouds");

            // 1. Generate Clouds based on the official Krita cloud background + Planet of Lana style
            GenerateCloudAssets();

            // 2. Generate Modular Tree Layers (Trunks, Branches, Canopies) & Complete Hero Trees
            GenerateTreeAssets();

            // 3. Generate Small & Medium Bushes (Lush Emerald & Golden Harvest)
            GenerateBushAssets();

            // 4. Generate Grass Tufts & Wildflowers (Lineless Gouache)
            GenerateGrassAndFlowerAssets();

            // 5. Generate Branches & Mossy Fallen Tree Log (for Crawl Obstacle & Canopy Framing)
            GenerateBranchAndLogAssets();

            // 6. Generate Golden Rice Field Stalks & Rows
            GenerateGoldenRiceAssets();

            AssetDatabase.Refresh();
            ConfigureAllSpritesImportSettings();
            AssetDatabase.SaveAssets();

            Debug.Log("<color=#4ade80>[LanaUnifiedArtGenerator]</color> Successfully generated all unified, lineless Planet of Lana 2D assets!");
        }

        // =========================================================================================
        // CORE PAINTERLY CANVAS WITH ZERO-FRINGE ALPHA DILATION
        // =========================================================================================
        private class PainterlyCanvas
        {
            public readonly int Width;
            public readonly int Height;
            public readonly Color[] Pixels;

            public PainterlyCanvas(int width, int height)
            {
                Width = width;
                Height = height;
                Pixels = new Color[width * height];
            }

            public void BlendPixel(int x, int y, Color src)
            {
                if ((uint)x >= (uint)Width || (uint)y >= (uint)Height) return;
                if (src.a <= 0.001f) return;

                int idx = y * Width + x;
                Color dst = Pixels[idx];

                float outA = src.a + dst.a * (1f - src.a);
                if (outA <= 0.0001f) return;

                float invOutA = 1f / outA;
                float r = (src.r * src.a + dst.r * dst.a * (1f - src.a)) * invOutA;
                float g = (src.g * src.a + dst.g * dst.a * (1f - src.a)) * invOutA;
                float b = (src.b * src.a + dst.b * dst.a * (1f - src.a)) * invOutA;

                Pixels[idx] = new Color(r, g, b, outA);
            }

            /// <summary>
            /// Paints a soft, lineless gouache leaf-lobe / bristle dab oriented along angleDeg.
            /// </summary>
            public void PaintGouacheDab(float cx, float cy, float rx, float ry, float angleDeg, Color col, float edgeSoftness = 0.28f, float bristleGrain = 0.03f)
            {
                float rad = angleDeg * Mathf.Deg2Rad;
                float cos = Mathf.Cos(rad);
                float sin = Mathf.Sin(rad);
                float maxR = Mathf.Max(rx, ry) + 2f;

                int minX = Mathf.Max(0, Mathf.FloorToInt(cx - maxR));
                int maxX = Mathf.Min(Width - 1, Mathf.CeilToInt(cx + maxR));
                int minY = Mathf.Max(0, Mathf.FloorToInt(cy - maxR));
                int maxY = Mathf.Min(Height - 1, Mathf.CeilToInt(cy + maxR));

                float invRx = 1f / Mathf.Max(0.5f, rx);
                float invRy = 1f / Mathf.Max(0.5f, ry);

                for (int y = minY; y <= maxY; y++)
                {
                    float dy = y - cy;
                    for (int x = minX; x <= maxX; x++)
                    {
                        float dx = x - cx;
                        float lx = (dx * cos + dy * sin) * invRx;
                        float ly = (-dx * sin + dy * cos) * invRy;

                        // Slight teardrop organic shaping for leaf/brush dabs
                        float pinch = 1f - 0.18f * Mathf.Clamp(lx, -1f, 1f);
                        float distSq = lx * lx + (ly * pinch) * (ly * pinch);
                        if (distSq >= 1f) continue;

                        float dist = Mathf.Sqrt(distSq);
                        float inner = 1f - Mathf.Clamp01(edgeSoftness);
                        float alpha = (dist <= inner) ? 1f : 1f - Mathf.SmoothStep(0f, 1f, (dist - inner) / Mathf.Max(0.01f, 1f - inner));

                        if (bristleGrain > 0f)
                        {
                            float n = (Mathf.PerlinNoise(x * 0.08f + cx * 0.01f, y * 0.08f + cy * 0.01f) - 0.5f) * 2f * bristleGrain;
                            Color c = new Color(
                                Mathf.Clamp01(col.r + n),
                                Mathf.Clamp01(col.g + n),
                                Mathf.Clamp01(col.b + n * 0.8f),
                                col.a * alpha
                            );
                            BlendPixel(x, y, c);
                        }
                        else
                        {
                            BlendPixel(x, y, new Color(col.r, col.g, col.b, col.a * alpha));
                        }
                    }
                }
            }

            /// <summary>
            /// Paints a cluster of overlapping gouache leaf lobes (Planet of Lana signature foliage cluster).
            /// </summary>
            public void PaintFoliageCluster(System.Random rnd, float cx, float cy, float radiusX, float radiusY, Color baseCol, int dabCount, float dabScale = 1f)
            {
                for (int i = 0; i < dabCount; i++)
                {
                    float angle = (float)rnd.NextDouble() * Mathf.PI * 2f;
                    float dist = Mathf.Sqrt((float)rnd.NextDouble());
                    float ox = Mathf.Cos(angle) * dist * radiusX;
                    float oy = Mathf.Sin(angle) * dist * radiusY;

                    // Subtle vertical light gradient within the cluster itself
                    float vShift = Mathf.Clamp(oy / Mathf.Max(1f, radiusY), -1f, 1f) * 0.045f;
                    float randShift = ((float)rnd.NextDouble() - 0.5f) * 0.03f;
                    float delta = vShift + randShift;

                    Color c = new Color(
                        Mathf.Clamp01(baseCol.r + delta * 0.9f),
                        Mathf.Clamp01(baseCol.g + delta * 1.1f),
                        Mathf.Clamp01(baseCol.b + delta * 0.5f),
                        0.96f
                    );

                    float dabRx = (18f + (float)rnd.NextDouble() * 20f) * dabScale;
                    float dabRy = (11f + (float)rnd.NextDouble() * 13f) * dabScale;
                    float dabAngle = Mathf.Atan2(oy, ox) * Mathf.Rad2Deg + ((float)rnd.NextDouble() - 0.5f) * 35f;

                    PaintGouacheDab(cx + ox, cy + oy, dabRx, dabRy, dabAngle, c, 0.30f, 0.025f);
                }
            }

            /// <summary>
            /// Paints a tapered cubic Bezier stroke (used for trunks, branches, grass blades, stems, rice stalks)
            /// with cross-sectional light/shadow shading and zero outline.
            /// </summary>
            public void PaintTaperedBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float rStart, float rEnd, Color shadowCol, Color litCol, int steps = 140, float edgeSoftness = 0.25f)
            {
                for (int i = 0; i <= steps; i++)
                {
                    float t = (float)i / steps;
                    Vector2 pos = EvalBezier(p0, p1, p2, p3, t);
                    Vector2 tan = EvalBezierTangent(p0, p1, p2, p3, t).normalized;
                    Vector2 norm = new Vector2(tan.y, -tan.x); // Points right/up for sunlit rim

                    float r = Mathf.Lerp(rStart, rEnd, Mathf.Pow(t, 0.85f));
                    if (r < 0.6f) continue;

                    int minX = Mathf.Max(0, Mathf.FloorToInt(pos.x - r - 2f));
                    int maxX = Mathf.Min(Width - 1, Mathf.CeilToInt(pos.x + r + 2f));
                    int minY = Mathf.Max(0, Mathf.FloorToInt(pos.y - r - 2f));
                    int maxY = Mathf.Min(Height - 1, Mathf.CeilToInt(pos.y + r + 2f));

                    for (int y = minY; y <= maxY; y++)
                    {
                        for (int x = minX; x <= maxX; x++)
                        {
                            Vector2 d = new Vector2(x - pos.x, y - pos.y);
                            float dist = d.magnitude;
                            if (dist > r) continue;

                            float u = dist / r;
                            float inner = 1f - edgeSoftness;
                            float alpha = (u <= inner) ? 1f : 1f - Mathf.SmoothStep(0f, 1f, (u - inner) / Mathf.Max(0.01f, 1f - inner));

                            // Cross-section shading (left/bottom shadow -> right/top sunlit)
                            float side = Mathf.Clamp01((Vector2.Dot(d, norm) / Mathf.Max(1f, r)) * 0.5f + 0.5f);
                            float barkStreak = (Mathf.PerlinNoise(t * 14f, side * 6f) - 0.5f) * 0.05f;
                            Color c = Color.Lerp(shadowCol, litCol, Mathf.SmoothStep(0.15f, 0.85f, side + barkStreak));
                            c.a *= alpha * 0.55f;
                            BlendPixel(x, y, c);
                        }
                    }
                }
            }

            /// <summary>
            /// Dilates RGB values into alpha=0 border pixels so bilinear texture filtering NEVER shows dark/white halos.
            /// </summary>
            public void SaveToPng(string assetPath, int dilationPasses = 6)
            {
                Color[] current = (Color[])Pixels.Clone();
                Color[] next = new Color[Pixels.Length];

                // Un-premultiply / threshold tiny alpha noise and dilate RGB outward into transparent pixels
                for (int i = 0; i < current.Length; i++)
                {
                    if (current[i].a < 0.015f)
                    {
                        current[i] = new Color(0f, 0f, 0f, 0f);
                    }
                }

                for (int pass = 0; pass < dilationPasses; pass++)
                {
                    Array.Copy(current, next, current.Length);
                    for (int y = 0; y < Height; y++)
                    {
                        int row = y * Width;
                        for (int x = 0; x < Width; x++)
                        {
                            int idx = row + x;
                            if (current[idx].a > 0.05f) continue;

                            float sumR = 0f, sumG = 0f, sumB = 0f, sumW = 0f;
                            for (int dy = -1; dy <= 1; dy++)
                            {
                                int ny = y + dy;
                                if ((uint)ny >= (uint)Height) continue;
                                int nRow = ny * Width;
                                for (int dx = -1; dx <= 1; dx++)
                                {
                                    if (dx == 0 && dy == 0) continue;
                                    int nx = x + dx;
                                    if ((uint)nx >= (uint)Width) continue;
                                    Color nb = current[nRow + nx];
                                    if (nb.a > 0.05f)
                                    {
                                        float w = nb.a;
                                        sumR += nb.r * w;
                                        sumG += nb.g * w;
                                        sumB += nb.b * w;
                                        sumW += w;
                                    }
                                }
                            }

                            if (sumW > 0f)
                            {
                                next[idx] = new Color(sumR / sumW, sumG / sumW, sumB / sumW, current[idx].a);
                            }
                        }
                    }
                    Array.Copy(next, current, current.Length);
                }

                Texture2D tex = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
                tex.SetPixels(current);
                tex.Apply();

                byte[] png = tex.EncodeToPNG();
                UnityEngine.Object.DestroyImmediate(tex);

                string fullPath = Path.Combine(Application.dataPath, "..", assetPath);
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                File.WriteAllBytes(fullPath, png);
            }

            private static Vector2 EvalBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
            {
                float u = 1f - t;
                return u * u * u * p0 + 3f * u * u * t * p1 + 3f * u * t * t * p2 + t * t * t * p3;
            }

            private static Vector2 EvalBezierTangent(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
            {
                float u = 1f - t;
                return 3f * u * u * (p1 - p0) + 6f * u * t * (p2 - p1) + 3f * t * t * (p3 - p2);
            }
        }

        // =========================================================================================
        // 1. CLOUD ASSETS (PLANET OF LANA KRITA BRUSH STYLE)
        // =========================================================================================
        private static void GenerateCloudAssets()
        {
            Color cloudShadowDeep = new Color(0.54f, 0.68f, 0.82f, 1f);
            Color cloudShadowMid  = new Color(0.72f, 0.82f, 0.91f, 1f);
            Color cloudBodyWarm   = new Color(0.92f, 0.95f, 0.97f, 1f);
            Color cloudTopSunlit  = new Color(0.99f, 0.98f, 0.95f, 1f);

            // Cloud 01: Hero Cumulus Billow (1024x640)
            {
                var c = new PainterlyCanvas(1024, 640);
                var rnd = new System.Random(101);
                // Flat-bottomed cumulus silhouette built from soft Krita horizontal & billowing dabs
                c.PaintFoliageCluster(rnd, 512, 230, 340, 95, cloudShadowDeep, 55, 2.2f);
                c.PaintFoliageCluster(rnd, 512, 290, 310, 115, cloudShadowMid, 65, 2.1f);
                c.PaintFoliageCluster(rnd, 480, 360, 260, 125, cloudBodyWarm, 70, 2.0f);
                c.PaintFoliageCluster(rnd, 530, 420, 210, 110, cloudTopSunlit, 60, 1.8f);
                c.PaintFoliageCluster(rnd, 340, 310, 150, 85, cloudBodyWarm, 40, 1.7f);
                c.PaintFoliageCluster(rnd, 680, 300, 160, 80, cloudBodyWarm, 40, 1.7f);
                c.SaveToPng($"{OutputRoot}/Clouds/T_Lana_Cloud_01_CumulusHero.png");
            }

            // Cloud 02: Wide Drifting Stratocumulus Layer for Sky Descent (1280x512)
            {
                var c = new PainterlyCanvas(1280, 512);
                var rnd = new System.Random(202);
                c.PaintFoliageCluster(rnd, 640, 190, 480, 75, cloudShadowDeep, 75, 2.4f);
                c.PaintFoliageCluster(rnd, 420, 250, 320, 90, cloudShadowMid, 60, 2.2f);
                c.PaintFoliageCluster(rnd, 820, 240, 340, 85, cloudShadowMid, 60, 2.2f);
                c.PaintFoliageCluster(rnd, 580, 300, 380, 95, cloudBodyWarm, 75, 2.0f);
                c.PaintFoliageCluster(rnd, 540, 345, 280, 80, cloudTopSunlit, 55, 1.8f);
                c.SaveToPng($"{OutputRoot}/Clouds/T_Lana_Cloud_02_WideBank.png");
            }

            // Cloud 03: High-Altitude Soft Cloud Wisps (1024x384)
            {
                var c = new PainterlyCanvas(1024, 384);
                var rnd = new System.Random(303);
                Color wispBase = new Color(0.78f, 0.88f, 0.96f, 0.72f);
                Color wispTop  = new Color(0.96f, 0.98f, 1.00f, 0.82f);
                c.PaintFoliageCluster(rnd, 512, 175, 380, 55, wispBase, 55, 2.3f);
                c.PaintFoliageCluster(rnd, 490, 210, 310, 50, wispTop, 45, 2.0f);
                c.SaveToPng($"{OutputRoot}/Clouds/T_Lana_Cloud_03_HighWisps.png");
            }

            // Cloud 04: Warm Golden Sunset/Harvest Cloud Bank (1024x512)
            {
                var c = new PainterlyCanvas(1024, 512);
                var rnd = new System.Random(404);
                Color goldShadow = new Color(0.66f, 0.64f, 0.74f, 1f);
                Color goldMid    = new Color(0.88f, 0.80f, 0.72f, 1f);
                Color goldTop    = new Color(1.00f, 0.94f, 0.80f, 1f);
                c.PaintFoliageCluster(rnd, 512, 195, 360, 80, goldShadow, 60, 2.2f);
                c.PaintFoliageCluster(rnd, 512, 255, 320, 95, goldMid, 65, 2.0f);
                c.PaintFoliageCluster(rnd, 495, 315, 250, 85, goldTop, 55, 1.8f);
                c.SaveToPng($"{OutputRoot}/Clouds/T_Lana_Cloud_04_GoldenHorizon.png");
            }
        }

        // =========================================================================================
        // 2. UNIFIED PLANET OF LANA TREES (COMPLETE & MODULAR MULTI-LAYER)
        // =========================================================================================
        private static void GenerateTreeAssets()
        {
            // Unified Planet of Lana Gouache Foliage Palette (Zero Outlines!)
            Color tier0DeepTeal   = new Color(0.09f, 0.21f, 0.22f, 1f); // Deep interior canopy shadow
            Color tier1Forest     = new Color(0.15f, 0.34f, 0.25f, 1f); // Lower canopy rich viridian
            Color tier2Emerald    = new Color(0.26f, 0.49f, 0.27f, 1f); // Mid canopy lush sage-emerald
            Color tier3Chartreuse = new Color(0.45f, 0.66f, 0.29f, 1f); // Upper sunlit foliage
            Color tier4SunRim     = new Color(0.62f, 0.77f, 0.35f, 1f); // Sun-kissed top crown rim

            Color barkShadow      = new Color(0.16f, 0.13f, 0.13f, 1f); // Deep warm slate-umber bark
            Color barkLit         = new Color(0.38f, 0.28f, 0.20f, 1f); // Warm sunlit sienna bark

            // Golden Harvest Tree Palette (for Rice Field & Village)
            Color gold0Deep       = new Color(0.16f, 0.24f, 0.18f, 1f);
            Color gold1Olive      = new Color(0.30f, 0.40f, 0.21f, 1f);
            Color gold2Warm       = new Color(0.52f, 0.56f, 0.24f, 1f);
            Color gold3Amber      = new Color(0.74f, 0.68f, 0.28f, 1f);
            Color gold4SunCrown   = new Color(0.88f, 0.80f, 0.36f, 1f);

            // A. Modular Layer 1: Gnarled Hero Banyan Trunk + Branches (1024x1024)
            {
                var c = new PainterlyCanvas(1024, 1024);
                PaintHeroBanyanTrunk(c, barkShadow, barkLit);
                c.SaveToPng($"{OutputRoot}/Trees/T_Lana_Tree_Modular_Trunk.png");
            }

            // B. Modular Layer 2: Back Shadow Canopy (1024x1024)
            {
                var c = new PainterlyCanvas(1024, 1024);
                var rnd = new System.Random(1101);
                c.PaintFoliageCluster(rnd, 480, 560, 310, 150, tier0DeepTeal, 110, 1.35f);
                c.PaintFoliageCluster(rnd, 320, 530, 190, 115, tier0DeepTeal, 65, 1.25f);
                c.PaintFoliageCluster(rnd, 660, 520, 210, 120, tier0DeepTeal, 70, 1.25f);
                c.PaintFoliageCluster(rnd, 490, 620, 280, 130, tier1Forest, 85, 1.20f);
                c.SaveToPng($"{OutputRoot}/Trees/T_Lana_Tree_Modular_CanopyBack.png");
            }

            // C. Modular Layer 3: Mid Lush Emerald Canopy (1024x1024)
            {
                var c = new PainterlyCanvas(1024, 1024);
                var rnd = new System.Random(1102);
                c.PaintFoliageCluster(rnd, 500, 640, 290, 145, tier1Forest, 95, 1.25f);
                c.PaintFoliageCluster(rnd, 330, 600, 185, 110, tier2Emerald, 70, 1.15f);
                c.PaintFoliageCluster(rnd, 675, 595, 195, 115, tier2Emerald, 75, 1.15f);
                c.PaintFoliageCluster(rnd, 505, 700, 250, 125, tier2Emerald, 90, 1.15f);
                c.SaveToPng($"{OutputRoot}/Trees/T_Lana_Tree_Modular_CanopyMid.png");
            }

            // D. Modular Layer 4: Front Sunlit Crown Canopy (1024x1024)
            {
                var c = new PainterlyCanvas(1024, 1024);
                var rnd = new System.Random(1103);
                c.PaintFoliageCluster(rnd, 520, 735, 235, 115, tier3Chartreuse, 80, 1.10f);
                c.PaintFoliageCluster(rnd, 360, 675, 145, 85, tier3Chartreuse, 50, 1.05f);
                c.PaintFoliageCluster(rnd, 685, 665, 155, 90, tier3Chartreuse, 55, 1.05f);
                c.PaintFoliageCluster(rnd, 545, 790, 180, 85, tier4SunRim, 60, 1.00f);
                c.PaintFoliageCluster(rnd, 700, 715, 110, 65, tier4SunRim, 35, 0.95f);
                c.SaveToPng($"{OutputRoot}/Trees/T_Lana_Tree_Modular_CanopyFront.png");
            }

            // E. Complete Tree 01: Grand Hero Banyan Tree (1024x1024)
            {
                var c = new PainterlyCanvas(1024, 1024);
                var rnd = new System.Random(1201);
                // 1. Back shadow canopy behind upper branches
                c.PaintFoliageCluster(rnd, 485, 555, 315, 145, tier0DeepTeal, 115, 1.35f);
                c.PaintFoliageCluster(rnd, 315, 525, 195, 110, tier0DeepTeal, 65, 1.25f);
                c.PaintFoliageCluster(rnd, 665, 515, 210, 115, tier0DeepTeal, 70, 1.25f);
                // 2. Gnarled trunk & branches
                PaintHeroBanyanTrunk(c, barkShadow, barkLit);
                // 3. Mid & Front sunlit canopy layers overlapping branches naturally
                c.PaintFoliageCluster(rnd, 495, 615, 295, 140, tier1Forest, 100, 1.25f);
                c.PaintFoliageCluster(rnd, 330, 595, 185, 105, tier2Emerald, 70, 1.15f);
                c.PaintFoliageCluster(rnd, 675, 590, 195, 110, tier2Emerald, 75, 1.15f);
                c.PaintFoliageCluster(rnd, 505, 690, 255, 125, tier2Emerald, 90, 1.15f);
                c.PaintFoliageCluster(rnd, 525, 745, 225, 110, tier3Chartreuse, 80, 1.10f);
                c.PaintFoliageCluster(rnd, 365, 670, 140, 80, tier3Chartreuse, 45, 1.05f);
                c.PaintFoliageCluster(rnd, 685, 660, 150, 85, tier3Chartreuse, 50, 1.05f);
                c.PaintFoliageCluster(rnd, 550, 795, 175, 80, tier4SunRim, 55, 1.00f);
                c.SaveToPng($"{OutputRoot}/Trees/T_Lana_Tree_01_HeroBanyan.png");
            }

            // F. Complete Tree 02: Windblown Curved Cliff Tree (1024x1024)
            {
                var c = new PainterlyCanvas(1024, 1024);
                var rnd = new System.Random(1202);
                c.PaintFoliageCluster(rnd, 560, 580, 270, 130, tier0DeepTeal, 95, 1.30f);
                // Gracefully curved windblown trunk
                c.PaintTaperedBezier(new Vector2(390, 70), new Vector2(370, 250), new Vector2(430, 430), new Vector2(540, 590), 42f, 11f, barkShadow, barkLit);
                c.PaintTaperedBezier(new Vector2(360, 65), new Vector2(385, 130), new Vector2(395, 180), new Vector2(400, 230), 26f, 14f, barkShadow, barkLit);
                c.PaintTaperedBezier(new Vector2(425, 65), new Vector2(405, 125), new Vector2(398, 175), new Vector2(395, 220), 24f, 12f, barkShadow, barkLit);
                c.PaintTaperedBezier(new Vector2(445, 430), new Vector2(380, 500), new Vector2(330, 555), new Vector2(290, 595), 18f, 6f, barkShadow, barkLit);
                c.PaintTaperedBezier(new Vector2(485, 495), new Vector2(580, 535), new Vector2(665, 565), new Vector2(730, 590), 18f, 6f, barkShadow, barkLit);

                c.PaintFoliageCluster(rnd, 555, 625, 260, 125, tier1Forest, 90, 1.20f);
                c.PaintFoliageCluster(rnd, 345, 595, 155, 90, tier2Emerald, 55, 1.15f);
                c.PaintFoliageCluster(rnd, 690, 585, 185, 95, tier2Emerald, 65, 1.15f);
                c.PaintFoliageCluster(rnd, 565, 685, 230, 110, tier2Emerald, 80, 1.12f);
                c.PaintFoliageCluster(rnd, 585, 735, 200, 95, tier3Chartreuse, 70, 1.05f);
                c.PaintFoliageCluster(rnd, 610, 775, 155, 70, tier4SunRim, 45, 0.98f);
                c.SaveToPng($"{OutputRoot}/Trees/T_Lana_Tree_02_WindblownCurved.png");
            }

            // G. Complete Tree 03: Slender Tall Forest Canopy Tree (800x1024)
            {
                var c = new PainterlyCanvas(800, 1024);
                var rnd = new System.Random(1203);
                c.PaintFoliageCluster(rnd, 400, 650, 210, 155, tier0DeepTeal, 85, 1.25f);
                c.PaintTaperedBezier(new Vector2(395, 60), new Vector2(385, 280), new Vector2(410, 480), new Vector2(400, 690), 32f, 9f, barkShadow, barkLit);
                c.PaintTaperedBezier(new Vector2(398, 460), new Vector2(335, 535), new Vector2(285, 590), new Vector2(250, 635), 15f, 5f, barkShadow, barkLit);
                c.PaintTaperedBezier(new Vector2(404, 510), new Vector2(465, 575), new Vector2(515, 625), new Vector2(550, 665), 15f, 5f, barkShadow, barkLit);

                c.PaintFoliageCluster(rnd, 400, 690, 200, 145, tier1Forest, 80, 1.18f);
                c.PaintFoliageCluster(rnd, 395, 745, 180, 125, tier2Emerald, 75, 1.12f);
                c.PaintFoliageCluster(rnd, 410, 800, 155, 100, tier3Chartreuse, 60, 1.05f);
                c.PaintFoliageCluster(rnd, 425, 845, 120, 75, tier4SunRim, 40, 0.95f);
                c.SaveToPng($"{OutputRoot}/Trees/T_Lana_Tree_03_SlenderForest.png");
            }

            // H. Complete Tree 04: Golden Harvest Village Banyan Tree (1024x1024)
            {
                var c = new PainterlyCanvas(1024, 1024);
                var rnd = new System.Random(1204);
                c.PaintFoliageCluster(rnd, 485, 555, 315, 145, gold0Deep, 115, 1.35f);
                c.PaintFoliageCluster(rnd, 315, 525, 195, 110, gold0Deep, 65, 1.25f);
                c.PaintFoliageCluster(rnd, 665, 515, 210, 115, gold0Deep, 70, 1.25f);
                PaintHeroBanyanTrunk(c, barkShadow, barkLit);
                c.PaintFoliageCluster(rnd, 495, 615, 295, 140, gold1Olive, 100, 1.25f);
                c.PaintFoliageCluster(rnd, 330, 595, 185, 105, gold2Warm, 70, 1.15f);
                c.PaintFoliageCluster(rnd, 675, 590, 195, 110, gold2Warm, 75, 1.15f);
                c.PaintFoliageCluster(rnd, 505, 690, 255, 125, gold2Warm, 90, 1.15f);
                c.PaintFoliageCluster(rnd, 525, 745, 225, 110, gold3Amber, 80, 1.10f);
                c.PaintFoliageCluster(rnd, 550, 795, 175, 80, gold4SunCrown, 55, 1.00f);
                c.SaveToPng($"{OutputRoot}/Trees/T_Lana_Tree_04_GoldenHarvestBanyan.png");
            }
        }

        private static void PaintHeroBanyanTrunk(PainterlyCanvas c, Color barkShadow, Color barkLit)
        {
            // Main trunk + flared roots + primary boughs
            c.PaintTaperedBezier(new Vector2(490, 65), new Vector2(475, 230), new Vector2(485, 390), new Vector2(500, 560), 54f, 16f, barkShadow, barkLit);
            // Left root flare
            c.PaintTaperedBezier(new Vector2(420, 55), new Vector2(455, 115), new Vector2(472, 180), new Vector2(480, 250), 32f, 18f, barkShadow, barkLit);
            // Right root flare
            c.PaintTaperedBezier(new Vector2(565, 55), new Vector2(525, 115), new Vector2(505, 180), new Vector2(495, 250), 30f, 18f, barkShadow, barkLit);
            // Left primary bough
            c.PaintTaperedBezier(new Vector2(482, 360), new Vector2(410, 440), new Vector2(340, 505), new Vector2(275, 560), 26f, 8f, barkShadow, barkLit);
            c.PaintTaperedBezier(new Vector2(375, 470), new Vector2(355, 535), new Vector2(335, 585), new Vector2(315, 625), 14f, 5f, barkShadow, barkLit);
            // Right primary bough
            c.PaintTaperedBezier(new Vector2(492, 380), new Vector2(575, 455), new Vector2(650, 515), new Vector2(720, 565), 25f, 8f, barkShadow, barkLit);
            c.PaintTaperedBezier(new Vector2(610, 485), new Vector2(635, 545), new Vector2(660, 595), new Vector2(680, 635), 14f, 5f, barkShadow, barkLit);
            // Center upper bough
            c.PaintTaperedBezier(new Vector2(495, 460), new Vector2(480, 545), new Vector2(465, 615), new Vector2(450, 675), 18f, 6f, barkShadow, barkLit);
        }

        // =========================================================================================
        // 3. BUSH ASSETS (SMALL & MEDIUM LINELESS GOUACHE BUSHES)
        // =========================================================================================
        private static void GenerateBushAssets()
        {
            Color tier0DeepTeal   = new Color(0.08f, 0.19f, 0.20f, 1f);
            Color tier1Forest     = new Color(0.15f, 0.34f, 0.25f, 1f);
            Color tier2Emerald    = new Color(0.26f, 0.49f, 0.27f, 1f);
            Color tier3Chartreuse = new Color(0.45f, 0.66f, 0.29f, 1f);
            Color tier4SunRim     = new Color(0.60f, 0.75f, 0.34f, 1f);

            // Bush 01: Lush Rounded Forest Bush (512x384)
            {
                var c = new PainterlyCanvas(512, 384);
                var rnd = new System.Random(2101);
                c.PaintFoliageCluster(rnd, 256, 125, 175, 70, tier0DeepTeal, 65, 0.95f);
                c.PaintFoliageCluster(rnd, 256, 165, 165, 80, tier1Forest, 70, 0.90f);
                c.PaintFoliageCluster(rnd, 250, 205, 145, 75, tier2Emerald, 65, 0.85f);
                c.PaintFoliageCluster(rnd, 265, 240, 115, 60, tier3Chartreuse, 50, 0.80f);
                c.PaintFoliageCluster(rnd, 280, 265, 85, 42, tier4SunRim, 30, 0.72f);
                c.SaveToPng($"{OutputRoot}/Bushes/T_Lana_Bush_01_LushMound.png");
            }

            // Bush 02: Wide Low Shrub Cluster (640x360)
            {
                var c = new PainterlyCanvas(640, 360);
                var rnd = new System.Random(2102);
                c.PaintFoliageCluster(rnd, 320, 115, 230, 65, tier0DeepTeal, 80, 0.95f);
                c.PaintFoliageCluster(rnd, 220, 155, 145, 70, tier1Forest, 55, 0.88f);
                c.PaintFoliageCluster(rnd, 420, 150, 150, 68, tier1Forest, 55, 0.88f);
                c.PaintFoliageCluster(rnd, 315, 185, 195, 72, tier2Emerald, 70, 0.85f);
                c.PaintFoliageCluster(rnd, 295, 220, 155, 58, tier3Chartreuse, 55, 0.78f);
                c.PaintFoliageCluster(rnd, 335, 242, 110, 42, tier4SunRim, 35, 0.70f);
                c.SaveToPng($"{OutputRoot}/Bushes/T_Lana_Bush_02_WideCluster.png");
            }

            // Bush 03: Flowering Meadow Bush with Subtle Blossoms (512x384)
            {
                var c = new PainterlyCanvas(512, 384);
                var rnd = new System.Random(2103);
                c.PaintFoliageCluster(rnd, 256, 120, 170, 68, tier0DeepTeal, 60, 0.92f);
                c.PaintFoliageCluster(rnd, 256, 160, 160, 76, tier1Forest, 65, 0.88f);
                c.PaintFoliageCluster(rnd, 256, 200, 140, 70, tier2Emerald, 60, 0.82f);
                c.PaintFoliageCluster(rnd, 265, 235, 110, 55, tier3Chartreuse, 45, 0.75f);

                // Delicate Planet of Lana cream & warm coral flower dabs nestled in the upper foliage
                Color creamFlower = new Color(0.98f, 0.95f, 0.84f, 0.95f);
                Color coralFlower = new Color(0.92f, 0.44f, 0.36f, 0.95f);
                for (int i = 0; i < 22; i++)
                {
                    float fx = 140f + (float)rnd.NextDouble() * 230f;
                    float fy = 155f + (float)rnd.NextDouble() * 115f;
                    Color fc = (i % 3 == 0) ? coralFlower : creamFlower;
                    c.PaintGouacheDab(fx, fy, 6.5f, 5.0f, (float)rnd.NextDouble() * 180f, fc, 0.35f, 0f);
                    c.PaintGouacheDab(fx + 3f, fy + 2f, 5.0f, 4.0f, (float)rnd.NextDouble() * 180f, fc, 0.35f, 0f);
                }
                c.SaveToPng($"{OutputRoot}/Bushes/T_Lana_Bush_03_Flowering.png");
            }

            // Bush 04: Golden Harvest Field Bush (512x384)
            {
                var c = new PainterlyCanvas(512, 384);
                var rnd = new System.Random(2104);
                Color g0 = new Color(0.16f, 0.24f, 0.18f, 1f);
                Color g1 = new Color(0.30f, 0.40f, 0.21f, 1f);
                Color g2 = new Color(0.52f, 0.56f, 0.24f, 1f);
                Color g3 = new Color(0.74f, 0.68f, 0.28f, 1f);
                Color g4 = new Color(0.88f, 0.80f, 0.36f, 1f);
                c.PaintFoliageCluster(rnd, 256, 125, 175, 70, g0, 65, 0.95f);
                c.PaintFoliageCluster(rnd, 256, 165, 165, 80, g1, 70, 0.90f);
                c.PaintFoliageCluster(rnd, 250, 205, 145, 75, g2, 65, 0.85f);
                c.PaintFoliageCluster(rnd, 265, 240, 115, 60, g3, 50, 0.80f);
                c.PaintFoliageCluster(rnd, 280, 265, 85, 42, g4, 30, 0.72f);
                c.SaveToPng($"{OutputRoot}/Bushes/T_Lana_Bush_04_GoldenHarvest.png");
            }
        }

        // =========================================================================================
        // 4. GRASS TUFTS & WILDFLOWERS (CLEAN LINELESS GOUACHE SILHOUETTES)
        // =========================================================================================
        private static void GenerateGrassAndFlowerAssets()
        {
            Color grassBaseShadow = new Color(0.16f, 0.34f, 0.22f, 1f);
            Color grassMidEmerald = new Color(0.28f, 0.52f, 0.26f, 1f);
            Color grassTipSunlit  = new Color(0.48f, 0.68f, 0.30f, 1f);

            Color goldGrassBase   = new Color(0.36f, 0.42f, 0.20f, 1f);
            Color goldGrassTip    = new Color(0.78f, 0.70f, 0.30f, 1f);

            // Grass 01: Soft Painterly Meadow Tuft (256x256)
            {
                var c = new PainterlyCanvas(256, 256);
                var rnd = new System.Random(3101);
                PaintGrassCluster(c, rnd, 128f, 24f, 22, 95f, 145f, 0.2f, grassBaseShadow, grassTipSunlit);
                c.SaveToPng($"{OutputRoot}/Foliage/T_Lana_Grass_01_SoftTuft.png");
            }

            // Grass 02: Windswept Meadow Clump (384x256)
            {
                var c = new PainterlyCanvas(384, 256);
                var rnd = new System.Random(3102);
                PaintGrassCluster(c, rnd, 180f, 24f, 34, 105f, 165f, 0.45f, grassBaseShadow, grassTipSunlit);
                c.SaveToPng($"{OutputRoot}/Foliage/T_Lana_Grass_02_WindsweptClump.png");
            }

            // Grass 03: Golden Harvest Windswept Grass (384x256)
            {
                var c = new PainterlyCanvas(384, 256);
                var rnd = new System.Random(3103);
                PaintGrassCluster(c, rnd, 180f, 24f, 34, 105f, 165f, 0.45f, goldGrassBase, goldGrassTip);
                c.SaveToPng($"{OutputRoot}/Foliage/T_Lana_Grass_03_GoldenMeadow.png");
            }

            // Wildflower 01: Red & Cream Wildflower Tuft ("Hoa trên Đất Đỏ" signature motif, 256x256)
            {
                var c = new PainterlyCanvas(256, 256);
                var rnd = new System.Random(3104);
                PaintGrassCluster(c, rnd, 128f, 22f, 18, 85f, 135f, 0.25f, grassBaseShadow, grassMidEmerald);

                Color redPetal   = new Color(0.88f, 0.30f, 0.25f, 0.98f);
                Color creamPetal = new Color(0.98f, 0.94f, 0.82f, 0.98f);
                Color goldCenter = new Color(0.96f, 0.78f, 0.28f, 0.98f);

                Vector2[] blooms = new[]
                {
                    new Vector2(92f, 155f), new Vector2(126f, 178f), new Vector2(158f, 148f),
                    new Vector2(108f, 132f), new Vector2(145f, 168f), new Vector2(174f, 128f)
                };
                for (int i = 0; i < blooms.Length; i++)
                {
                    Vector2 b = blooms[i];
                    Color pCol = (i % 2 == 0) ? redPetal : creamPetal;
                    for (int p = 0; p < 4; p++)
                    {
                        float a = p * 90f + 20f;
                        float rad = a * Mathf.Deg2Rad;
                        c.PaintGouacheDab(b.x + Mathf.Cos(rad) * 5f, b.y + Mathf.Sin(rad) * 5f, 6.5f, 4.2f, a, pCol, 0.30f, 0f);
                    }
                    c.PaintGouacheDab(b.x, b.y, 3.8f, 3.8f, 0f, goldCenter, 0.30f, 0f);
                }
                c.SaveToPng($"{OutputRoot}/Foliage/T_Lana_Wildflower_01_RedAndCream.png");
            }
        }

        private static void PaintGrassCluster(PainterlyCanvas c, System.Random rnd, float centerX, float baseY, int bladeCount, float minH, float maxH, float windLean, Color baseCol, Color tipCol)
        {
            for (int i = 0; i < bladeCount; i++)
            {
                float u = (float)i / Mathf.Max(1, bladeCount - 1) - 0.5f;
                float startX = centerX + u * (bladeCount * 3.2f) + ((float)rnd.NextDouble() - 0.5f) * 6f;
                float h = Mathf.Lerp(minH, maxH, (float)rnd.NextDouble()) * (1f - Mathf.Abs(u) * 0.35f);
                float lean = (u * 55f + windLean * 55f) + ((float)rnd.NextDouble() - 0.5f) * 28f;

                Vector2 p0 = new Vector2(startX, baseY);
                Vector2 p1 = new Vector2(startX + lean * 0.25f, baseY + h * 0.38f);
                Vector2 p2 = new Vector2(startX + lean * 0.65f, baseY + h * 0.74f);
                Vector2 p3 = new Vector2(startX + lean, baseY + h);

                float rStart = 4.8f + (float)rnd.NextDouble() * 2.2f;
                c.PaintTaperedBezier(p0, p1, p2, p3, rStart, 0.8f, baseCol, tipCol, 70, 0.30f);
            }
        }

        // =========================================================================================
        // 5. BRANCHES & MOSSY FALLEN LOG (FOR CANOPY REVEAL & CRAWL UNDER OBSTACLE)
        // =========================================================================================
        private static void GenerateBranchAndLogAssets()
        {
            Color barkShadow = new Color(0.15f, 0.12f, 0.12f, 1f);
            Color barkLit    = new Color(0.36f, 0.26f, 0.18f, 1f);
            Color mossDeep   = new Color(0.14f, 0.32f, 0.24f, 1f);
            Color mossMid    = new Color(0.26f, 0.49f, 0.27f, 1f);
            Color mossSun    = new Color(0.45f, 0.66f, 0.29f, 1f);

            // Branch 01: Overhanging Leafy Branch (768x384)
            {
                var c = new PainterlyCanvas(768, 384);
                var rnd = new System.Random(4101);
                c.PaintTaperedBezier(new Vector2(20, 260), new Vector2(220, 250), new Vector2(440, 210), new Vector2(680, 150), 28f, 6f, barkShadow, barkLit);
                c.PaintTaperedBezier(new Vector2(280, 235), new Vector2(360, 175), new Vector2(430, 125), new Vector2(490, 90), 14f, 4f, barkShadow, barkLit);
                c.PaintFoliageCluster(rnd, 520, 185, 165, 75, mossDeep, 55, 0.90f);
                c.PaintFoliageCluster(rnd, 620, 160, 130, 65, mossMid, 50, 0.85f);
                c.PaintFoliageCluster(rnd, 460, 120, 115, 55, mossMid, 45, 0.80f);
                c.PaintFoliageCluster(rnd, 560, 215, 135, 55, mossSun, 45, 0.78f);
                c.SaveToPng($"{OutputRoot}/Branches/T_Lana_Branch_01_Overhanging.png");
            }

            // Fallen Log 02: Mossy Fallen Tree Trunk for Crawl Obstacle (896x384)
            {
                var c = new PainterlyCanvas(896, 384);
                var rnd = new System.Random(4102);
                // Thick gnarled horizontal trunk arch
                c.PaintTaperedBezier(new Vector2(45, 140), new Vector2(280, 215), new Vector2(610, 215), new Vector2(850, 135), 48f, 36f, barkShadow, barkLit, 180);
                // Exposed roots on left
                c.PaintTaperedBezier(new Vector2(30, 80), new Vector2(85, 120), new Vector2(130, 155), new Vector2(180, 180), 26f, 14f, barkShadow, barkLit);
                c.PaintTaperedBezier(new Vector2(25, 195), new Vector2(80, 175), new Vector2(130, 170), new Vector2(180, 175), 22f, 12f, barkShadow, barkLit);
                // Lush moss cushion along top of the fallen trunk
                c.PaintFoliageCluster(rnd, 280, 235, 150, 42, mossDeep, 45, 0.75f);
                c.PaintFoliageCluster(rnd, 460, 240, 170, 45, mossMid, 55, 0.75f);
                c.PaintFoliageCluster(rnd, 630, 225, 140, 40, mossMid, 45, 0.75f);
                c.PaintFoliageCluster(rnd, 450, 260, 155, 34, mossSun, 45, 0.68f);
                c.SaveToPng($"{OutputRoot}/Branches/T_Lana_FallenTrunk_Mossy.png");
            }
        }

        // =========================================================================================
        // 6. GOLDEN RICE FIELD ASSETS (HEAVY GOLDEN PANICLES & SWAYING STALKS)
        // =========================================================================================
        private static void GenerateGoldenRiceAssets()
        {
            Color stalkOliveShadow = new Color(0.32f, 0.38f, 0.16f, 1f);
            Color stalkGoldenGreen = new Color(0.58f, 0.56f, 0.22f, 1f);
            Color panicleGoldDeep  = new Color(0.76f, 0.58f, 0.18f, 1f);
            Color panicleAmberLit  = new Color(0.95f, 0.79f, 0.28f, 1f);
            Color panicleSunRim    = new Color(1.00f, 0.90f, 0.46f, 1f);

            // Rice 01: Detailed Golden Rice Clump with Drooping Heavy Panicles (384x384)
            {
                var c = new PainterlyCanvas(384, 384);
                var rnd = new System.Random(5101);
                PaintRiceClump(c, rnd, 192f, 22f, 24, 180f, 265f, stalkOliveShadow, stalkGoldenGreen, panicleGoldDeep, panicleAmberLit, panicleSunRim);
                c.SaveToPng($"{OutputRoot}/Rice/T_Lana_Rice_01_GoldenClump.png");
            }

            // Rice 02: Wide Golden Rice Row for Midground / Foreground Layering (768x384)
            {
                var c = new PainterlyCanvas(768, 384);
                var rnd = new System.Random(5102);
                PaintRiceClump(c, rnd, 160f, 20f, 22, 175f, 255f, stalkOliveShadow, stalkGoldenGreen, panicleGoldDeep, panicleAmberLit, panicleSunRim);
                PaintRiceClump(c, rnd, 384f, 20f, 26, 185f, 270f, stalkOliveShadow, stalkGoldenGreen, panicleGoldDeep, panicleAmberLit, panicleSunRim);
                PaintRiceClump(c, rnd, 608f, 20f, 22, 175f, 255f, stalkOliveShadow, stalkGoldenGreen, panicleGoldDeep, panicleAmberLit, panicleSunRim);
                c.SaveToPng($"{OutputRoot}/Rice/T_Lana_Rice_02_WideGoldenRow.png");
            }
        }

        private static void PaintRiceClump(PainterlyCanvas c, System.Random rnd, float centerX, float baseY, int stalkCount, float minH, float maxH,
            Color stalkShadow, Color stalkLit, Color grainDeep, Color grainLit, Color grainRim)
        {
            for (int i = 0; i < stalkCount; i++)
            {
                float u = (float)i / Mathf.Max(1, stalkCount - 1) - 0.5f;
                float sx = centerX + u * (stalkCount * 4.2f) + ((float)rnd.NextDouble() - 0.5f) * 8f;
                float h = Mathf.Lerp(minH, maxH, (float)rnd.NextDouble());
                float dir = (u >= -0.1f) ? 1f : -0.7f; // Predominantly windswept to the right
                float arch = dir * (48f + (float)rnd.NextDouble() * 42f);

                Vector2 p0 = new Vector2(sx, baseY);
                Vector2 p1 = new Vector2(sx + arch * 0.20f, baseY + h * 0.45f);
                Vector2 p2 = new Vector2(sx + arch * 0.65f, baseY + h * 0.88f);
                Vector2 p3 = new Vector2(sx + arch * 1.15f, baseY + h * 0.72f); // Drooping heavy rice head!

                c.PaintTaperedBezier(p0, p1, p2, p3, 4.2f, 1.4f, stalkShadow, stalkLit, 80, 0.28f);

                // Paint heavy golden rice grains along the upper drooping panicle (t = 0.55..1.0)
                for (int g = 0; g < 9; g++)
                {
                    float gt = Mathf.Lerp(0.55f, 1.0f, (float)g / 8f);
                    float gu = 1f - gt;
                    Vector2 gPos = gu * gu * gu * p0 + 3f * gu * gu * gt * p1 + 3f * gu * gt * gt * p2 + gt * gt * gt * p3;
                    Color gCol = (g >= 6) ? grainRim : ((g >= 3) ? grainLit : grainDeep);
                    float gAngle = -25f * dir + ((float)rnd.NextDouble() - 0.5f) * 20f;
                    c.PaintGouacheDab(gPos.x, gPos.y + ((float)rnd.NextDouble() - 0.5f) * 6f, 8.5f, 4.2f, gAngle, gCol, 0.32f, 0.02f);
                }
            }
        }

        // =========================================================================================
        // CONFIGURE SPRITE IMPORT SETTINGS (CLAMP, BILINEAR, ALPHA IS TRANSPARENCY, FULL RECT)
        // =========================================================================================
        public static void ConfigureAllSpritesImportSettings()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { OutputRoot });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;

                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                // Bottom-center pivot for grounded trees/bushes/grass/rice; center pivot for clouds/modular canopies
                bool isCenterPivot = path.Contains("/Clouds/") || path.Contains("Modular_Canopy") || path.Contains("Branch_01") || path.Contains("FallenTrunk");
                settings.spritePivot = isCenterPivot ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, 0.06f);

                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
        }
    }
}
#endif
