#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;

namespace HoaVaMau.Editor
{
    public static class BuildLivingOceanCamau
    {
        private const string SourceImagePath = "Assets/Textures/References/ocean-camau.jpg";
        private const string OutDir = "Assets/Textures/Environment";

        [MenuItem("Hoa va Mau/Ocean/Extract Living Ocean Layers")]
        public static void ExtractLayers()
        {
            if (!Directory.Exists(OutDir)) Directory.CreateDirectory(OutDir);

            // Ensure source is readable
            var srcImporter = AssetImporter.GetAtPath(SourceImagePath) as TextureImporter;
            if (srcImporter != null)
            {
                srcImporter.isReadable = true;
                srcImporter.npotScale = TextureImporterNPOTScale.None;
                srcImporter.maxTextureSize = 4096;
                srcImporter.textureCompression = TextureImporterCompression.Uncompressed;
                srcImporter.SaveAndReimport();
            }

            var srcTex = AssetDatabase.LoadAssetAtPath<Texture2D>(SourceImagePath);
            if (srcTex == null)
            {
                Debug.LogError($"Could not load source texture at {SourceImagePath}");
                return;
            }

            int w = srcTex.width;
            int h = srcTex.height;
            var srcPixels = srcTex.GetPixels();

            // 1. EXTRACT HERO BOAT (Right side: large beige sail, wooden hull, fisherman)
            int hx0 = 1845;
            int hy0 = 330;
            int hw = 290;
            int hh = 200;

            ExtractSingleBoat(srcTex, srcPixels, w, h, hx0, hy0, hw, hh, "T_Camau_Boat_Hero.png", isHero: true);

            // 2. EXTRACT LEFT BOAT (Left side: smaller sail, wooden hull)
            int lx0 = 1172;
            int ly0 = 365;
            int lw = 180;
            int lh = 150;

            ExtractSingleBoat(srcTex, srcPixels, w, h, lx0, ly0, lw, lh, "T_Camau_Boat_Left.png", isHero: false);

            // 3. GENERATE CLEAN INPAINTED BACKGROUND (Water/sky filled behind the boats)
            GenerateCleanBackground(srcTex, srcPixels, w, h, hx0, hy0, hw, hh, lx0, ly0, lw, lh);

            // 4. GENERATE VOLUMETRIC SUNBEAMS TEXTURE (Additive god-ray shafts)
            GenerateSunbeamsTexture();

            // 5. GENERATE WATER SHIMMER / CAUSTIC GLINTS TEXTURE
            GenerateWaterShimmerTexture();

            // 6. GENERATE BOAT WATER WAKE RIPPLE TEXTURE
            GenerateBoatWakeTexture();

            AssetDatabase.Refresh();
            Debug.Log("<color=#10b981>[LivingOcean]</color> <b>All Living Ocean layers extracted & generated successfully!</b>");
        }

        private static void ExtractSingleBoat(Texture2D srcTex, Color[] srcPixels, int fullW, int fullH, int x0, int y0, int w, int h, string outFileName, bool isHero)
        {
            var boatTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var boatPixels = new Color[w * h];

            Vector2[] poly;
            if (isHero)
            {
                // Exact tight silhouette polygon around Hero boat
                poly = new Vector2[]
                {
                    new Vector2(188, 185), // Mast peak
                    new Vector2(145, 183),
                    new Vector2(100, 172),
                    new Vector2(60, 150),
                    new Vector2(30, 115),
                    new Vector2(18, 70),   // Tack
                    new Vector2(10, 42),   // Bow
                    new Vector2(2, 22),    // Forefoot
                    new Vector2(35, 4),    // Fore keel
                    new Vector2(110, 0),   // Mid keel
                    new Vector2(210, 6),   // Aft keel
                    new Vector2(278, 24),  // Stern
                    new Vector2(275, 52),  // Rudder / fisherman
                    new Vector2(250, 78),  // Helm / cabin
                    new Vector2(220, 105),
                    new Vector2(202, 140), // Aft shroud
                    new Vector2(190, 182)
                };
            }
            else
            {
                // Exact tight silhouette polygon around Left boat
                poly = new Vector2[]
                {
                    new Vector2(105, 135), // Mast peak
                    new Vector2(76, 128),
                    new Vector2(44, 110),
                    new Vector2(22, 78),
                    new Vector2(16, 52),   // Tack
                    new Vector2(4, 26),    // Bow
                    new Vector2(40, 6),    // Fore keel
                    new Vector2(95, 2),    // Mid keel
                    new Vector2(135, 10),  // Aft keel
                    new Vector2(164, 32),  // Stern
                    new Vector2(152, 52),  // Helm
                    new Vector2(118, 58),  // Foot
                    new Vector2(110, 95),  // Leech
                    new Vector2(105, 132)
                };
            }

            for (int ly = 0; ly < h; ly++)
            {
                int gy = y0 + ly;
                for (int lx = 0; lx < w; lx++)
                {
                    int gx = x0 + lx;
                    Color c = srcPixels[gy * fullW + gx];

                    Vector2 pt = new Vector2(lx, ly);
                    float polyDist = SignedDistanceToPolygon(pt, poly);

                    // Anti-aliased boundary feather
                    float alpha = Mathf.Clamp01(0.5f - polyDist / 1.5f);

                    boatPixels[ly * w + lx] = new Color(c.r, c.g, c.b, alpha);
                }
            }

            boatTex.SetPixels(boatPixels);
            boatTex.Apply();

            string outPath = Path.Combine(OutDir, outFileName);
            File.WriteAllBytes(outPath, boatTex.EncodeToPNG());
            Object.DestroyImmediate(boatTex);

            ConfigureImportedSprite(outPath, isHero ? new Vector2(0.50f, 0.08f) : new Vector2(0.50f, 0.12f));
            Debug.Log($"Extracted authentic boat sprite: {outFileName}");
        }

        private static float SignedDistanceToPolygon(Vector2 p, Vector2[] poly)
        {
            // Point-in-polygon test (ray casting) + distance to closest segment
            bool inside = false;
            float minDistSq = float.MaxValue;

            int n = poly.Length;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                Vector2 a = poly[j];
                Vector2 b = poly[i];

                if (((b.y > p.y) != (a.y > p.y)) &&
                    (p.x < (a.x - b.x) * (p.y - b.y) / (a.y - b.y) + b.x))
                {
                    inside = !inside;
                }

                // Distance to segment AB
                Vector2 ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                Vector2 closest = a + ab * t;
                float distSq = (p - closest).sqrMagnitude;
                if (distSq < minDistSq) minDistSq = distSq;
            }

            float dist = Mathf.Sqrt(minDistSq);
            return inside ? -dist : dist;
        }

        private static void GenerateCleanBackground(Texture2D srcTex, Color[] srcPixels, int fullW, int fullH,
            int hx0, int hy0, int hw, int hh, int lx0, int ly0, int lw, int lh)
        {
            var cleanBg = new Texture2D(fullW, fullH, TextureFormat.RGB24, false);
            var cleanPixels = (Color[])srcPixels.Clone();

            // Precise polygon-bounded inpainting for Hero Boat
            InpaintBoatPolygon(cleanPixels, fullW, fullH, hx0, hy0, hw, hh, true);

            // Precise polygon-bounded inpainting for Left Boat
            InpaintBoatPolygon(cleanPixels, fullW, fullH, lx0, ly0, lw, lh, false);

            cleanBg.SetPixels(cleanPixels);
            cleanBg.Apply();

            string outPath = Path.Combine(OutDir, "T_Camau_Ocean_Background_Clean.png");
            File.WriteAllBytes(outPath, cleanBg.EncodeToPNG());
            Object.DestroyImmediate(cleanBg);

            ConfigureImportedSprite(outPath, new Vector2(0.5f, 0.5f));
            Debug.Log($"Generated clean in-painted background: {outPath}");
        }

        private static void InpaintBoatPolygon(Color[] pixels, int fullW, int fullH, int x0, int y0, int w, int h, bool isHero)
        {
            // Sample from adjacent open water/sky with soft feathered boundary
            int cloneOffset = isHero ? 360 : 250;

            for (int ly = 0; ly < h; ly++)
            {
                int gy = y0 + ly;
                if (gy < 0 || gy >= fullH) continue;

                int minX = -1;
                int maxX = -1;

                if (isHero)
                {
                    if (ly < 30) { minX = 2; maxX = 278; }
                    else if (ly < 75) { minX = 10; maxX = 265; }
                    else if (ly < 130) { minX = (int)Mathf.Lerp(18f, 60f, (ly - 75f) / 55f); maxX = (int)Mathf.Lerp(250f, 205f, (ly - 75f) / 55f); }
                    else if (ly < 170) { minX = (int)Mathf.Lerp(60f, 135f, (ly - 130f) / 40f); maxX = (int)Mathf.Lerp(205f, 192f, (ly - 130f) / 40f); }
                    else if (ly < 190) { minX = 145; maxX = 192; }
                }
                else
                {
                    if (ly < 30) { minX = 2; maxX = 164; }
                    else if (ly < 60) { minX = 10; maxX = 150; }
                    else if (ly < 110) { minX = (int)Mathf.Lerp(16f, 65f, (ly - 60f) / 50f); maxX = (int)Mathf.Lerp(145f, 115f, (ly - 60f) / 50f); }
                    else if (ly < 138) { minX = 75; maxX = 108; }
                }

                if (minX < 0 || maxX < 0 || minX >= maxX) continue;

                int startX = Mathf.Clamp(x0 + minX - 4, 0, fullW - 1);
                int endX   = Mathf.Clamp(x0 + maxX + 4, 0, fullW - 1);
                int spanW  = endX - startX;
                if (spanW <= 0) continue;

                for (int gx = startX; gx <= endX; gx++)
                {
                    int srcX = Mathf.Clamp(gx + cloneOffset, 0, fullW - 1);
                    Color srcCol = pixels[gy * fullW + srcX];

                    // Feather blend at edges
                    float distToEdge = Mathf.Min(gx - startX, endX - gx);
                    float blend = Mathf.SmoothStep(0f, 1f, distToEdge / 6.0f);

                    Color orig = pixels[gy * fullW + gx];
                    pixels[gy * fullW + gx] = Color.Lerp(orig, srcCol, blend);
                }
            }
        }

        private static void GenerateSunbeamsTexture()
        {
            int w = 1024;
            int h = 1024;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color[w * h];

            // Radial god rays emanating from top-middle (origin at x ~ 0.52, y ~ 1.08)
            Vector2 origin = new Vector2(w * 0.50f, h * 1.06f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Vector2 pos = new Vector2(x, y);
                    Vector2 dir = pos - origin;
                    float dist = dir.magnitude / h;
                    float angle = Mathf.Atan2(dir.y, dir.x);

                    // Multi-frequency radial shafts
                    float beam1 = Mathf.Pow(Mathf.Sin(angle * 14.5f + 0.4f) * 0.5f + 0.5f, 2.5f);
                    float beam2 = Mathf.Pow(Mathf.Sin(angle * 28.0f - 1.2f) * 0.5f + 0.5f, 3.2f) * 0.6f;
                    float beam3 = Mathf.Pow(Mathf.Sin(angle * 44.0f + 2.1f) * 0.5f + 0.5f, 4.0f) * 0.35f;

                    float beamIntensity = Mathf.Clamp01(beam1 + beam2 + beam3);

                    // Fade out near bottom and edges
                    float distFalloff = Mathf.SmoothStep(1.35f, 0.25f, dist);
                    float angleWindow = Mathf.SmoothStep(-1.85f, -1.25f, angle) * Mathf.SmoothStep(-0.75f, -1.15f, angle);

                    float alpha = beamIntensity * distFalloff * angleWindow;
                    Color sunColor = new Color(1.0f, 0.96f, 0.84f, alpha * 0.85f);
                    pixels[y * w + x] = sunColor;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            string outPath = Path.Combine(OutDir, "T_Camau_Sunbeams_Volumetric.png");
            File.WriteAllBytes(outPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            ConfigureImportedSprite(outPath, new Vector2(0.5f, 1.0f));
            Debug.Log($"Generated sunbeams texture: {outPath}");
        }

        private static void GenerateWaterShimmerTexture()
        {
            int w = 512;
            int h = 256;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float u = (float)x / w;
                    float v = (float)y / h;

                    // Elongated horizontal sea caustic ripples
                    float wave1 = Mathf.Sin(u * 38.0f + Mathf.Cos(v * 16.0f) * 2.2f);
                    float wave2 = Mathf.Sin(u * 72.0f - Mathf.Sin(v * 28.0f) * 3.4f);
                    float wave3 = Mathf.Sin((u + v) * 44.0f);

                    float caustic = Mathf.Pow((wave1 * 0.45f + wave2 * 0.35f + wave3 * 0.20f) * 0.5f + 0.5f, 3.5f);
                    float sparkle = Mathf.Pow(Mathf.Clamp01(wave1 * wave2), 4.0f) * 1.5f;

                    float alpha = Mathf.Clamp01((caustic * 0.55f + sparkle * 0.45f) * Mathf.Sin(v * Mathf.PI));
                    pixels[y * w + x] = new Color(0.85f, 1.0f, 0.98f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            string outPath = Path.Combine(OutDir, "T_Camau_Water_Shimmer.png");
            File.WriteAllBytes(outPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            ConfigureImportedSprite(outPath, new Vector2(0.5f, 0.5f));
            Debug.Log($"Generated water shimmer texture: {outPath}");
        }

        private static void GenerateBoatWakeTexture()
        {
            int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];

            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - center.x) / (size * 0.45f);
                    float dy = (y - center.y) / (size * 0.22f); // elliptical
                    float d = Mathf.Sqrt(dx * dx + dy * dy);

                    // Double concentric water ripple ring
                    float ring1 = Mathf.Sin(d * 14.0f);
                    float ring2 = Mathf.Sin(d * 24.0f) * 0.5f;
                    float ringPattern = Mathf.Clamp01((ring1 + ring2) * 0.5f + 0.5f);

                    float fade = Mathf.SmoothStep(1.0f, 0.2f, d) * Mathf.SmoothStep(0.0f, 0.15f, d);
                    float alpha = ringPattern * fade * 0.65f;

                    pixels[y * size + x] = new Color(0.92f, 0.98f, 1.0f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            string outPath = Path.Combine(OutDir, "T_Camau_Boat_WaterWake.png");
            File.WriteAllBytes(outPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            ConfigureImportedSprite(outPath, new Vector2(0.5f, 0.5f));
            Debug.Log($"Generated boat wake texture: {outPath}");
        }

        private static void ConfigureImportedSprite(string path, Vector2 pivot)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = pivot;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
        }
    }
}
#endif
