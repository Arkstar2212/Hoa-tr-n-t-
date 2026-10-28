#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace HoaVaMau.EditorTools
{
    /// <summary>
    /// Specialized Platform Surface Texture Generator matching the authentic Planet of Lana aesthetic.
    /// Directly inspired by the golden standard Planet of Lana title screen meadow:
    /// - Painterly waving grass blades with directional brushwork (slanted sunlit strokes).
    /// - Cel-shaded gouache layers with rich lime, emerald, and golden-green tones.
    /// - Scattered micro wildflowers (creamy white and soft yellow buttercups).
    /// - Ragged grass overhang lip with deep silhouette dark teal base.
    /// - 100% Seamless horizontal repeating (wrapMode = Repeat).
    /// </summary>
    public static class PlanetOfLanaPlatformTextureGenerator
    {
        private const string OutputFolder = "Assets/Textures/Ground_Tiles";
        private const string MatFolder = "Assets/Materials/LanaOverhaul/PlatformVariants";

        [MenuItem("Tools/Hoa Tren Dat Do/Generate Planet of Lana Platform Surface Textures (.PNG)")]
        public static void GenerateAllPlatformTextures()
        {
            if (!Directory.Exists(OutputFolder)) Directory.CreateDirectory(OutputFolder);
            if (!Directory.Exists(MatFolder)) Directory.CreateDirectory(MatFolder);

            int width = 2048;
            int height = 512;

            Debug.Log("<color=#38bdf8>[LanaPlatform]</color> Khởi tạo bộ Texture Platform bề mặt bước chân chuẩn Planet of Lana...");

            // Variant 1: Summer Meadow (Khớp 100% Title Screen người dùng gửi)
            GeneratePlatformVariant(
                "LanaPlatform_01_SummerMeadow.png", width, height,
                PlatformType.SummerMeadow,
                new Color(0.84f, 0.92f, 0.44f, 1f), // grassTip (warm golden sunlit lime #D6EB70)
                new Color(0.66f, 0.82f, 0.30f, 1f), // grassHigh (vibrant meadow lime #A8D14D)
                new Color(0.42f, 0.64f, 0.24f, 1f), // grassMid (rich emerald green #6BA33D)
                new Color(0.18f, 0.36f, 0.16f, 1f), // grassDark (deep foliage shade #2E5C29)
                new Color(0.06f, 0.14f, 0.11f, 1f), // shadowLip (dark teal-black silhouette #0F241C)
                new Color(0.03f, 0.07f, 0.05f, 1f), // bottomBlack (#08120D)
                true // enableWildflowers
            );

            // Variant 2: Lush Forest Meadow (Cỏ rừng rậm mát mẻ, ngọc bích sẫm)
            GeneratePlatformVariant(
                "LanaPlatform_02_LushForestMeadow.png", width, height,
                PlatformType.ForestLush,
                new Color(0.72f, 0.88f, 0.42f, 1f),
                new Color(0.50f, 0.74f, 0.28f, 1f),
                new Color(0.28f, 0.52f, 0.22f, 1f),
                new Color(0.12f, 0.28f, 0.14f, 1f),
                new Color(0.05f, 0.12f, 0.09f, 1f),
                new Color(0.02f, 0.06f, 0.04f, 1f),
                true
            );

            // Variant 3: Golden Hillside Trail (Đồi cỏ vàng nắng chiều Nam Bộ)
            GeneratePlatformVariant(
                "LanaPlatform_03_GoldenHillside.png", width, height,
                PlatformType.GoldenHill,
                new Color(0.92f, 0.86f, 0.46f, 1f), // cỏ vàng nắng
                new Color(0.76f, 0.74f, 0.32f, 1f),
                new Color(0.54f, 0.52f, 0.22f, 1f),
                new Color(0.28f, 0.28f, 0.12f, 1f),
                new Color(0.10f, 0.12f, 0.08f, 1f),
                new Color(0.04f, 0.05f, 0.03f, 1f),
                true
            );

            // Variant 4: Mossy Stone Ledge (Bề mặt phiến đá phủ thảm cỏ mỏng)
            GeneratePlatformVariant(
                "LanaPlatform_04_MossyStoneLedge.png", width, height,
                PlatformType.MossyStone,
                new Color(0.76f, 0.86f, 0.48f, 1f),
                new Color(0.55f, 0.68f, 0.34f, 1f),
                new Color(0.38f, 0.46f, 0.32f, 1f), // đá xám xanh lẫn cỏ
                new Color(0.20f, 0.26f, 0.20f, 1f),
                new Color(0.07f, 0.11f, 0.10f, 1f),
                new Color(0.03f, 0.05f, 0.05f, 1f),
                false
            );

            AssetDatabase.Refresh();
            ConfigureImportAndMaterials();

            Debug.Log("<color=#4ade80><b>[LanaPlatform] HOÀN TẤT! Đã tạo 4 texture platform chuẩn Planet of Lana!</b></color>");
        }

        public enum PlatformType
        {
            SummerMeadow,
            ForestLush,
            GoldenHill,
            MossyStone
        }

        private static void GeneratePlatformVariant(
            string filename, int width, int height,
            PlatformType pType,
            Color grassTip, Color grassHigh, Color grassMid, Color grassDark,
            Color shadowLip, Color bottomBlack,
            bool enableWildflowers)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
            Color[] pixels = new Color[width * height];

            float seed = (int)pType * 53.7f;

            // 1. Khởi tạo mảng màu nền Gouache (Soft watercolor background wash)
            for (int y = 0; y < height; y++)
            {
                float v = (float)y / height; // 0 = đáy tối, 1 = đỉnh ngọn cỏ

                for (int x = 0; x < width; x++)
                {
                    float u = (float)x / width; // 0..1 tuần hoàn

                    // Nền màu chuyển mượt từ cỏ xanh trên đỉnh xuống bóng đen dưới đáy
                    Color baseCol;
                    if (v > 0.65f)
                    {
                        float t = (v - 0.65f) / 0.35f;
                        baseCol = Color.Lerp(grassMid, grassHigh, Mathf.SmoothStep(0f, 1f, t));
                    }
                    else if (v > 0.35f)
                    {
                        float t = (v - 0.35f) / 0.30f;
                        baseCol = Color.Lerp(grassDark, grassMid, Mathf.SmoothStep(0f, 1f, t));
                    }
                    else
                    {
                        float t = v / 0.35f;
                        baseCol = Color.Lerp(bottomBlack, shadowLip, Mathf.SmoothStep(0f, 1f, t));
                    }

                    // Thêm đốm cọ màu nước mềm (Soft watercolor gouache mottling)
                    float nGouache = SeamlessNoise(u, v, 6, 4.0f, seed + 12.3f) * 0.12f - 0.06f;
                    baseCol = Color.Lerp(baseCol, grassTip, Mathf.Max(0f, nGouache * 2.0f));

                    pixels[y * width + x] = baseCol;
                }
            }

            // 2. Vẽ các cụm cọ cỏ nghiêng theo chiều gió (Directional Waving Painterly Blades)
            // Giống hệt nét vẽ tay của Adam Stålspets trong menu Planet of Lana
            int bladeCount = 1400; // Mật độ dày tạo thành thảm cỏ ngút ngàn
            System.Random rand = new System.Random(42 + (int)pType * 100);

            for (int b = 0; b < bladeCount; b++)
            {
                // Tọa độ gốc cỏ ngẫu nhiên
                float u0 = (float)rand.NextDouble();
                float v0 = 0.38f + (float)rand.NextDouble() * 0.45f; // Gốc nằm ở vùng giữa

                float bladeHeight = 0.18f + (float)rand.NextDouble() * 0.26f; // Chiều dài ngọn cỏ
                float leanAngle = 0.025f + (float)rand.NextDouble() * 0.035f; // Độ nghiêng sang phải theo gió

                int bladeSteps = 40;
                for (int step = 0; step < bladeSteps; step++)
                {
                    float t = (float)step / bladeSteps;
                    float curV = v0 + t * bladeHeight;
                    if (curV >= 1.0f) break;

                    // Đường cong uốn lượn tự nhiên theo gió
                    float windSway = Mathf.Sin(t * Mathf.PI * 0.7f) * leanAngle;
                    float curU = (u0 + windSway + 1.0f) % 1.0f; // Wrap 0..1 seamless

                    int px = Mathf.Clamp((int)(curU * width), 0, width - 1);
                    int py = Mathf.Clamp((int)(curV * height), 0, height - 1);

                    // Độ dày ngọn cỏ vuốt nhọn từ gốc lên ngọn
                    int thickness = Mathf.Max(1, (int)((1.0f - t * 0.75f) * 4.0f));

                    // Màu sắc ngọn cỏ: gốc đậm -> giữa xanh ngọc -> ngọn vàng nắng
                    Color bladeCol;
                    if (t > 0.65f)
                    {
                        bladeCol = Color.Lerp(grassHigh, grassTip, (t - 0.65f) / 0.35f);
                    }
                    else if (t > 0.30f)
                    {
                        bladeCol = Color.Lerp(grassMid, grassHigh, (t - 0.30f) / 0.35f);
                    }
                    else
                    {
                        bladeCol = Color.Lerp(grassDark, grassMid, t / 0.30f);
                    }

                    for (int dx = -thickness / 2; dx <= thickness / 2; dx++)
                    {
                        int targetX = (px + dx + width) % width;
                        int idx = py * width + targetX;

                        // Blend alpha mềm vào canvas
                        pixels[idx] = Color.Lerp(pixels[idx], bladeCol, 0.75f);
                    }
                }
            }

            // 3. Rải các bông hoa dại nhỏ li ti (Micro Wildflower Flecks: White & Buttercup Yellow)
            if (enableWildflowers)
            {
                int flowerCount = 180;
                for (int f = 0; f < flowerCount; f++)
                {
                    float fu = (float)rand.NextDouble();
                    float fv = 0.68f + (float)rand.NextDouble() * 0.26f; // Nằm ở đỉnh ngọn cỏ

                    bool isYellow = rand.NextDouble() > 0.55;
                    Color flowerCol = isYellow ? new Color(1.0f, 0.94f, 0.55f, 0.95f) : new Color(0.96f, 0.98f, 0.92f, 0.95f);

                    int fx = (int)(fu * width);
                    int fy = (int)(fv * height);

                    // Chấm hoa 2x2 hoặc 3x3 pixel mềm
                    int radius = rand.NextDouble() > 0.7 ? 2 : 1;
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        for (int dy = -radius; dy <= radius; dy++)
                        {
                            if (dx * dx + dy * dy <= radius * radius)
                            {
                                int targetX = (fx + dx + width) % width;
                                int targetY = Mathf.Clamp(fy + dy, 0, height - 1);
                                int idx = targetY * width + targetX;
                                pixels[idx] = Color.Lerp(pixels[idx], flowerCol, 0.85f);
                            }
                        }
                    }
                }
            }

            // 4. Viền mép dưới vát bóng râm tự nhiên (Platform Lip Undercut Silhouette)
            for (int y = 0; y < (int)(height * 0.36f); y++)
            {
                float v = (float)y / height;
                float fadeAlpha = Mathf.SmoothStep(0f, 1f, v / 0.36f);

                for (int x = 0; x < width; x++)
                {
                    int idx = y * width + x;
                    pixels[idx] = Color.Lerp(bottomBlack, pixels[idx], fadeAlpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            string fullPath = Path.Combine(OutputFolder, filename);
            File.WriteAllBytes(fullPath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        private static void ConfigureImportAndMaterials()
        {
            Shader roadShader = Shader.Find("HoaVaMau/LanaRoad3DDualSurface") ?? Shader.Find("Universal Render Pipeline/Lit");

            string[] filenames = new string[]
            {
                "LanaPlatform_01_SummerMeadow.png",
                "LanaPlatform_02_LushForestMeadow.png",
                "LanaPlatform_03_GoldenHillside.png",
                "LanaPlatform_04_MossyStoneLedge.png"
            };

            foreach (var fn in filenames)
            {
                string unityPath = $"{OutputFolder}/{fn}";
                var importer = AssetImporter.GetAtPath(unityPath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Default;
                    importer.wrapModeU = TextureWrapMode.Repeat;
                    importer.wrapModeV = TextureWrapMode.Clamp;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.maxTextureSize = 2048;
                    importer.mipmapEnabled = true;
                    importer.SaveAndReimport();
                }

                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(unityPath);
                if (tex != null && roadShader != null)
                {
                    string matName = "M_" + Path.GetFileNameWithoutExtension(unityPath);
                    string matPath = $"{MatFolder}/{matName}.mat";

                    Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if (mat == null)
                    {
                        mat = new Material(roadShader);
                        AssetDatabase.CreateAsset(mat, matPath);
                    }
                    else
                    {
                        mat.shader = roadShader;
                    }

                    mat.SetTexture("_MainTex", tex);
                    mat.SetVector("_SoilTexTiling", new Vector4(0.45f, 1.0f, 0f, 0f));
                    mat.SetFloat("_UseMeshUV", 1.0f);
                    mat.SetFloat("_ShadowStrength", 0.52f);
                    mat.SetFloat("_WaveDisplacement", 0.035f);
                    mat.SetColor("_WornTrailColor", new Color(0.72f, 0.82f, 0.40f, 1f));
                    mat.SetFloat("_TrailBlend", 0.20f);

                    EditorUtility.SetDirty(mat);
                }
            }

            AssetDatabase.SaveAssets();
        }

        private static float SeamlessNoise(float u, float v, int periodX, float freqY, float seed)
        {
            float angle = u * Mathf.PI * 2.0f * periodX;
            float nx = Mathf.Cos(angle) * 0.5f + seed;
            float nz = Mathf.Sin(angle) * 0.5f + seed;
            float ny = v * freqY + seed;

            float n1 = Mathf.PerlinNoise(nx + ny * 0.3f, nz + ny);
            float n2 = Mathf.PerlinNoise(nz - ny * 0.3f + 17.1f, nx + ny + 31.7f);
            return (n1 + n2) * 0.5f;
        }
    }
}
#endif
