#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HoaVaMau.Editor
{
    /// <summary>
    /// Generates high-fidelity, hand-painted 2D transparent cloud PNG sprites and
    /// cosmic zenith sky textures matching the official Planet of Lana / Vietnamese art direction.
    /// Strictly produces clean, transparent, non-AI-slop hand-painted style assets.
    /// </summary>
    public static class GenerateLanaAtmosphereAndClouds
    {
        private const string DistantDir = "Assets/Art/Environment/Sky/Clouds/Distant";
        private const string MidgroundDir = "Assets/Art/Environment/Sky/Clouds/Midground";
        private const string ForegroundDir = "Assets/Art/Environment/Sky/Clouds/Foreground";
        private const string SkyTexDir = "Assets/Textures/Backgrounds_Lana";

        [MenuItem("Hoa va Mau/Atmosphere/Generate All Clouds and Cosmic Sky")]
        public static void GenerateAll()
        {
            EnsureDirectories();

            Debug.Log("<color=#38bdf8>[LanaAtmosphere]</color> Generating Cosmic Zenith Sky Gradient...");
            GenerateCosmicZenithSky();

            Debug.Log("<color=#38bdf8>[LanaAtmosphere]</color> Generating Distant Cloud Layers...");
            GenerateDistantClouds();

            Debug.Log("<color=#38bdf8>[LanaAtmosphere]</color> Generating Midground Cumulus Banks...");
            GenerateMidgroundClouds();

            Debug.Log("<color=#38bdf8>[LanaAtmosphere]</color> Generating Foreground Descent Hero Clouds...");
            GenerateForegroundClouds();

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("<color=#10b981>[LanaAtmosphere]</color> <b>All Cloud Sprites and Cosmic Sky textures generated & imported successfully!</b>");
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(DistantDir)) Directory.CreateDirectory(DistantDir);
            if (!Directory.Exists(MidgroundDir)) Directory.CreateDirectory(MidgroundDir);
            if (!Directory.Exists(ForegroundDir)) Directory.CreateDirectory(ForegroundDir);
            if (!Directory.Exists(SkyTexDir)) Directory.CreateDirectory(SkyTexDir);
        }

        // =========================================================================================
        // 1. COSMIC ZENITH SKY GRADIENT (DEEP SPACE / GALAXY EDGE -> CELESTIAL BLUE -> MAIN SKY)
        // =========================================================================================
        public static void GenerateCosmicZenithSky()
        {
            int w = 1024;
            int h = 512;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);

            // Palette sampled from official background top edge & cosmic reference
            Color cBottom = new Color(0.102f, 0.329f, 0.576f); // Connects seamlessly to official BG top: #1A5493
            Color cMidLow = new Color(0.075f, 0.235f, 0.440f); // #133C70 Deep Cerulean
            Color cMid    = new Color(0.045f, 0.145f, 0.300f); // #0C254D Midnight Cobalt
            Color cZenith = new Color(0.015f, 0.045f, 0.115f); // #040C1D Deep Space Galaxy Blue

            Color[] pixels = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                float v = (float)y / (h - 1);
                Color rowColor;

                if (v < 0.35f)
                {
                    float t = Mathf.SmoothStep(0f, 1f, v / 0.35f);
                    rowColor = Color.Lerp(cBottom, cMidLow, t);
                }
                else if (v < 0.70f)
                {
                    float t = Mathf.SmoothStep(0f, 1f, (v - 0.35f) / 0.35f);
                    rowColor = Color.Lerp(cMidLow, cMid, t);
                }
                else
                {
                    float t = Mathf.SmoothStep(0f, 1f, (v - 0.70f) / 0.30f);
                    rowColor = Color.Lerp(cMid, cZenith, t);
                }

                int rowOffset = y * w;
                for (int x = 0; x < w; x++)
                {
                    pixels[rowOffset + x] = rowColor;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            string path = $"{SkyTexDir}/T_Lana_DeepZenithSky_Gradient.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            ConfigureTextureImporter(path, isSprite: false, wrapClamp: true);
        }

        // =========================================================================================
        // 2. DISTANT CLOUD LAYERS (SOFT HORIZONTAL STRATOCUMULUS WISPS)
        // =========================================================================================
        public static void GenerateDistantClouds()
        {
            // T_Cloud_Distant_Wisp01: Elongated, soft horizontal wisp
            GenerateWispCloud(
                filePath: $"{DistantDir}/T_Cloud_Distant_Wisp01.png",
                w: 640, h: 180,
                baseColor: new Color(0.74f, 0.83f, 0.90f),
                shadowColor: new Color(0.48f, 0.60f, 0.72f),
                aspectStretch: 4.2f,
                softness: 0.85f,
                noiseFreq: 3.2f,
                noiseAmp: 0.18f
            );

            // T_Cloud_Distant_Layer02: Wider atmospheric haze shelf
            GenerateWispCloud(
                filePath: $"{DistantDir}/T_Cloud_Distant_Layer02.png",
                w: 720, h: 200,
                baseColor: new Color(0.80f, 0.87f, 0.92f),
                shadowColor: new Color(0.55f, 0.65f, 0.75f),
                aspectStretch: 4.8f,
                softness: 0.92f,
                noiseFreq: 2.8f,
                noiseAmp: 0.15f
            );

            // T_Cloud_Distant_Horizon03: Low distant horizon veil
            GenerateWispCloud(
                filePath: $"{DistantDir}/T_Cloud_Distant_Horizon03.png",
                w: 800, h: 180,
                baseColor: new Color(0.86f, 0.90f, 0.94f),
                shadowColor: new Color(0.62f, 0.70f, 0.79f),
                aspectStretch: 5.5f,
                softness: 0.95f,
                noiseFreq: 2.4f,
                noiseAmp: 0.12f
            );
        }

        // =========================================================================================
        // 3. MIDGROUND CUMULUS BANKS (PAINTERLY LOBED CUMULUS WITH SOFT SLATE SHADOWS)
        // =========================================================================================
        public static void GenerateMidgroundClouds()
        {
            // T_Cloud_Mid_CumulusBank01: Iconic Planet of Lana cumulus bank with multi-lobed volume
            GenerateLobedCumulusCloud(
                filePath: $"{MidgroundDir}/T_Cloud_Mid_CumulusBank01.png",
                w: 720, h: 360,
                lobes: new LobeDefinition[]
                {
                    new LobeDefinition(0.48f, 0.44f, 0.36f, 0.28f, 1.0f),
                    new LobeDefinition(0.28f, 0.38f, 0.24f, 0.22f, 0.9f),
                    new LobeDefinition(0.68f, 0.40f, 0.26f, 0.24f, 0.92f),
                    new LobeDefinition(0.40f, 0.58f, 0.20f, 0.18f, 0.88f),
                    new LobeDefinition(0.58f, 0.54f, 0.22f, 0.20f, 0.85f),
                    new LobeDefinition(0.18f, 0.30f, 0.16f, 0.15f, 0.75f),
                    new LobeDefinition(0.80f, 0.32f, 0.18f, 0.16f, 0.80f)
                },
                topHighlight: new Color(0.96f, 0.97f, 0.98f),
                bodyColor:    new Color(0.84f, 0.89f, 0.93f),
                bottomShadow: new Color(0.50f, 0.60f, 0.71f)
            );

            // T_Cloud_Mid_Cluster02: Secondary cumulus group
            GenerateLobedCumulusCloud(
                filePath: $"{MidgroundDir}/T_Cloud_Mid_Cluster02.png",
                w: 640, h: 320,
                lobes: new LobeDefinition[]
                {
                    new LobeDefinition(0.45f, 0.42f, 0.32f, 0.25f, 1.0f),
                    new LobeDefinition(0.25f, 0.36f, 0.22f, 0.19f, 0.88f),
                    new LobeDefinition(0.65f, 0.44f, 0.25f, 0.22f, 0.92f),
                    new LobeDefinition(0.52f, 0.56f, 0.19f, 0.17f, 0.85f),
                    new LobeDefinition(0.35f, 0.52f, 0.17f, 0.16f, 0.82f),
                    new LobeDefinition(0.78f, 0.32f, 0.17f, 0.15f, 0.78f)
                },
                topHighlight: new Color(0.97f, 0.96f, 0.94f),
                bodyColor:    new Color(0.86f, 0.88f, 0.92f),
                bottomShadow: new Color(0.54f, 0.62f, 0.72f)
            );

            // T_Cloud_Mid_SoftBank03: Broad calm cloud bank
            GenerateLobedCumulusCloud(
                filePath: $"{MidgroundDir}/T_Cloud_Mid_SoftBank03.png",
                w: 680, h: 300,
                lobes: new LobeDefinition[]
                {
                    new LobeDefinition(0.50f, 0.40f, 0.42f, 0.24f, 1.0f),
                    new LobeDefinition(0.24f, 0.36f, 0.26f, 0.20f, 0.90f),
                    new LobeDefinition(0.74f, 0.38f, 0.25f, 0.21f, 0.88f),
                    new LobeDefinition(0.42f, 0.52f, 0.22f, 0.18f, 0.84f),
                    new LobeDefinition(0.62f, 0.50f, 0.24f, 0.19f, 0.86f)
                },
                topHighlight: new Color(0.95f, 0.96f, 0.98f),
                bodyColor:    new Color(0.82f, 0.87f, 0.92f),
                bottomShadow: new Color(0.48f, 0.58f, 0.70f)
            );
        }

        // =========================================================================================
        // 4. FOREGROUND DESCENT HERO CLOUDS (SWEEPING ATMOSPHERIC BANKS)
        // =========================================================================================
        public static void GenerateForegroundClouds()
        {
            // T_Cloud_Hero_Descent01: Large atmospheric formation sweeping across camera during descent
            GenerateLobedCumulusCloud(
                filePath: $"{ForegroundDir}/T_Cloud_Hero_Descent01.png",
                w: 800, h: 380,
                lobes: new LobeDefinition[]
                {
                    new LobeDefinition(0.52f, 0.46f, 0.40f, 0.30f, 1.0f),
                    new LobeDefinition(0.26f, 0.40f, 0.28f, 0.24f, 0.95f),
                    new LobeDefinition(0.74f, 0.42f, 0.29f, 0.25f, 0.92f),
                    new LobeDefinition(0.40f, 0.62f, 0.24f, 0.22f, 0.88f),
                    new LobeDefinition(0.60f, 0.60f, 0.25f, 0.23f, 0.90f),
                    new LobeDefinition(0.14f, 0.32f, 0.18f, 0.16f, 0.78f),
                    new LobeDefinition(0.86f, 0.34f, 0.20f, 0.18f, 0.82f)
                },
                topHighlight: new Color(0.98f, 0.98f, 0.99f),
                bodyColor:    new Color(0.88f, 0.91f, 0.95f),
                bottomShadow: new Color(0.52f, 0.62f, 0.73f)
            );

            // T_Cloud_Hero_Descent02: Second complementary descent cloud
            GenerateLobedCumulusCloud(
                filePath: $"{ForegroundDir}/T_Cloud_Hero_Descent02.png",
                w: 760, h: 360,
                lobes: new LobeDefinition[]
                {
                    new LobeDefinition(0.46f, 0.44f, 0.38f, 0.28f, 1.0f),
                    new LobeDefinition(0.70f, 0.40f, 0.27f, 0.23f, 0.92f),
                    new LobeDefinition(0.22f, 0.38f, 0.25f, 0.22f, 0.90f),
                    new LobeDefinition(0.54f, 0.58f, 0.23f, 0.21f, 0.86f),
                    new LobeDefinition(0.36f, 0.54f, 0.21f, 0.19f, 0.84f),
                    new LobeDefinition(0.82f, 0.32f, 0.19f, 0.17f, 0.80f)
                },
                topHighlight: new Color(0.97f, 0.97f, 0.96f),
                bodyColor:    new Color(0.86f, 0.89f, 0.93f),
                bottomShadow: new Color(0.50f, 0.60f, 0.70f)
            );
        }

        // =========================================================================================
        // PROCEDURAL PAINTERLY GENERATORS
        // =========================================================================================
        private struct LobeDefinition
        {
            public float cx, cy, rx, ry, weight;
            public LobeDefinition(float cx, float cy, float rx, float ry, float weight)
            {
                this.cx = cx; this.cy = cy; this.rx = rx; this.ry = ry; this.weight = weight;
            }
        }

        private static void GenerateLobedCumulusCloud(
            string filePath, int w, int h,
            LobeDefinition[] lobes,
            Color topHighlight, Color bodyColor, Color bottomShadow)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                float v = (float)y / (h - 1);
                for (int x = 0; x < w; x++)
                {
                    float u = (float)x / (w - 1);

                    // Soft painterly edge perturbation (brush turbulence)
                    float brushWarpX = (Mathf.PerlinNoise(u * 5.5f, v * 5.5f) - 0.5f) * 0.055f;
                    float brushWarpY = (Mathf.PerlinNoise(u * 5.5f + 14.2f, v * 5.5f + 8.7f) - 0.5f) * 0.045f;
                    float pu = u + brushWarpX;
                    float pv = v + brushWarpY;

                    // Evaluate density from lobes
                    float maxDensity = 0f;
                    for (int i = 0; i < lobes.Length; i++)
                    {
                        var lobe = lobes[i];
                        float dx = (pu - lobe.cx) / lobe.rx;
                        float dy = (pv - lobe.cy) / lobe.ry;
                        float distSq = dx * dx + dy * dy;
                        if (distSq < 1.0f)
                        {
                            // Smooth cosine falloff
                            float falloff = Mathf.Cos(distSq * Mathf.PI * 0.5f);
                            float d = falloff * lobe.weight;
                            if (d > maxDensity) maxDensity = d;
                        }
                    }

                    if (maxDensity <= 0.005f)
                    {
                        pixels[y * w + x] = new Color(0, 0, 0, 0);
                        continue;
                    }

                    // Multi-frequency soft painterly bristle detail
                    float bristle = Mathf.PerlinNoise(u * 18.0f, v * 18.0f) * 0.08f - 0.04f;
                    float density = Mathf.Clamp01(maxDensity + bristle);

                    // Alpha curve: Smooth edge feathering to transparent PNG
                    float alpha = Mathf.SmoothStep(0f, 1f, density * 1.35f);

                    // Vertical lighting: top is warm ivory, mid is body, bottom is cool slate shadow
                    float verticalT = Mathf.Clamp01((v - 0.20f) / 0.55f);
                    Color litColor;
                    if (verticalT > 0.5f)
                    {
                        float t = (verticalT - 0.5f) / 0.5f;
                        litColor = Color.Lerp(bodyColor, topHighlight, Mathf.SmoothStep(0f, 1f, t));
                    }
                    else
                    {
                        float t = verticalT / 0.5f;
                        litColor = Color.Lerp(bottomShadow, bodyColor, Mathf.SmoothStep(0f, 1f, t));
                    }

                    // Soft depth modulation
                    float coreDepth = Mathf.Clamp01(density * 1.15f);
                    Color finalPixel = Color.Lerp(bottomShadow, litColor, coreDepth);
                    finalPixel.a = alpha;

                    pixels[y * w + x] = finalPixel;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            File.WriteAllBytes(filePath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(filePath, ImportAssetOptions.ForceUpdate);
            ConfigureTextureImporter(filePath, isSprite: true, wrapClamp: true);
        }

        private static void GenerateWispCloud(
            string filePath, int w, int h,
            Color baseColor, Color shadowColor,
            float aspectStretch, float softness,
            float noiseFreq, float noiseAmp)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                float v = (float)y / (h - 1);
                for (int x = 0; x < w; x++)
                {
                    float u = (float)x / (w - 1);

                    // Horizontal elongated ellipse base
                    float dx = (u - 0.5f) * 2.0f;
                    float dy = (v - 0.5f) * 2.0f * aspectStretch;
                    float baseDist = dx * dx + dy * dy;

                    if (baseDist >= 1.0f)
                    {
                        pixels[y * w + x] = new Color(0, 0, 0, 0);
                        continue;
                    }

                    // Painterly wispy noise modulation
                    float pNoise = Mathf.PerlinNoise(u * noiseFreq * 2.5f, v * noiseFreq * 0.8f + 5.2f);
                    float edgeWarp = (pNoise - 0.5f) * noiseAmp;

                    float distWarped = Mathf.Clamp01(baseDist + edgeWarp);
                    float density = Mathf.Cos(distWarped * Mathf.PI * 0.5f) * softness;

                    if (density <= 0.005f)
                    {
                        pixels[y * w + x] = new Color(0, 0, 0, 0);
                        continue;
                    }

                    float alpha = Mathf.SmoothStep(0f, 1f, density * 1.4f);
                    float verticalT = Mathf.Clamp01((v - 0.30f) / 0.40f);
                    Color c = Color.Lerp(shadowColor, baseColor, Mathf.SmoothStep(0f, 1f, verticalT));
                    c.a = alpha;

                    pixels[y * w + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            File.WriteAllBytes(filePath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(filePath, ImportAssetOptions.ForceUpdate);
            ConfigureTextureImporter(filePath, isSprite: true, wrapClamp: true);
        }

        private static void ConfigureTextureImporter(string path, bool isSprite, bool wrapClamp)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                if (isSprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;
                    var settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                    settings.spriteMeshType = SpriteMeshType.FullRect;
                    settings.spriteAlignment = (int)SpriteAlignment.Center;
                    importer.SetTextureSettings(settings);
                }
                else
                {
                    importer.textureType = TextureImporterType.Default;
                    importer.alphaIsTransparency = false;
                }

                importer.mipmapEnabled = false;
                if (wrapClamp) importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }
    }
}
#endif
