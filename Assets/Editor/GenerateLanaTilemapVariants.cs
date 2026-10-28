#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace HoaVaMau.EditorTools
{
    /// <summary>
    /// Master Generator for Authentic "Planet of Lana" Stylized Ground Cross-Sections & Tilemaps.
    /// Perfectly replicates the hand-painted Ghibli / Planet of Lana gouache aesthetic:
    /// - Crisp, faceted cel-shaded earth clods with zero blurry generic noise.
    /// - Rich horizontal geological stratification (sedimentary earth bands).
    /// - Distinct, twisting organic roots with sharp rim highlights and cast shadows.
    /// - Embedded stones and pebbles with light/shadow facets.
    /// - Ragged painterly grass tufts with deep ambient occlusion pockets.
    /// - 100% Seamless horizontal repeating (Cylindrical noise math).
    /// </summary>
    public static class GenerateLanaTilemapVariants
    {
        private const string OutputFolder = "Assets/Textures/Ground_Tiles";
        private const string MatFolder = "Assets/Materials/LanaOverhaul/GroundVariants";

        [MenuItem("Tools/Hoa Tren Dat Do/Generate All 4 Lana Ground Texture Variants (.PNG)")]
        public static void GenerateAllVariants()
        {
            if (!Directory.Exists(OutputFolder))
                Directory.CreateDirectory(OutputFolder);
            if (!Directory.Exists(MatFolder))
                Directory.CreateDirectory(MatFolder);

            int width = 2048;
            int height = 512;

            Debug.Log("<color=#38bdf8>[LanaTilemap]</color> Đang khởi tạo 4 Biome Ground Textures chuẩn Planet of Lana (2048x512)...");

            // Variant 1: Rừng Rậm & Rễ Cây Nam Bộ (Lush Forest & Winding Roots - Planet of Lana Signature)
            GenerateVariant(
                "LanaGround_01_LushForest_Roots.png",
                width, height,
                GroundBiomeType.LushForest,
                new Color(0.62f, 0.82f, 0.28f, 1f), // grassHigh (vibrant sunny lime)
                new Color(0.36f, 0.62f, 0.20f, 1f), // grassMid
                new Color(0.16f, 0.35f, 0.10f, 1f), // grassDark
                new Color(0.56f, 0.38f, 0.22f, 1f), // dirtHigh (màu đất son bắt nắng)
                new Color(0.40f, 0.26f, 0.15f, 1f), // dirtMid (nâu đất ấm)
                new Color(0.24f, 0.15f, 0.08f, 1f), // dirtDark (nâu hốc tối)
                new Color(0.12f, 0.08f, 0.04f, 1f), // dirtDeep (nâu đen kẽ nứt)
                new Color(0.58f, 0.40f, 0.24f, 1f), // rootLight (vỏ rễ cây sáng)
                new Color(0.18f, 0.10f, 0.05f, 1f), // rootDark (bóng đổ rễ)
                new Color(0.50f, 0.72f, 0.28f, 1f), // mossLight
                new Color(0.68f, 0.70f, 0.66f, 1f)  // pebbleLight
            );

            // Variant 2: Đất Đỏ Bazan Cà Mau (Red Dirt Earth - Signature Hoa Trên Đất Đỏ)
            GenerateVariant(
                "LanaGround_02_RedDirt_NamBo.png",
                width, height,
                GroundBiomeType.RedDirtNamBo,
                new Color(0.68f, 0.84f, 0.24f, 1f), // grassHigh
                new Color(0.44f, 0.64f, 0.18f, 1f), // grassMid
                new Color(0.22f, 0.38f, 0.10f, 1f), // grassDark
                new Color(0.78f, 0.38f, 0.20f, 1f), // dirtHigh (đất đỏ tươi)
                new Color(0.58f, 0.24f, 0.12f, 1f), // dirtMid (đất đỏ bazan)
                new Color(0.38f, 0.15f, 0.07f, 1f), // dirtDark (đỏ thẫm hốc)
                new Color(0.20f, 0.08f, 0.04f, 1f), // dirtDeep
                new Color(0.62f, 0.40f, 0.22f, 1f), // rootLight (rễ đước vàng sẫm)
                new Color(0.24f, 0.12f, 0.05f, 1f), // rootDark
                new Color(0.54f, 0.72f, 0.24f, 1f), // moss
                new Color(0.74f, 0.62f, 0.52f, 1f)  // pebble
            );

            // Variant 3: Vách Đá Rêu Phong & Đá Phiến (Rocky Karst Cliff & Slate)
            GenerateVariant(
                "LanaGround_03_RockyKarst_Cliff.png",
                width, height,
                GroundBiomeType.RockyKarst,
                new Color(0.58f, 0.74f, 0.34f, 1f), // grassHigh
                new Color(0.38f, 0.54f, 0.24f, 1f), // grassMid
                new Color(0.20f, 0.32f, 0.14f, 1f), // grassDark
                new Color(0.72f, 0.74f, 0.70f, 1f), // rockHigh (đá phiến sáng)
                new Color(0.52f, 0.55f, 0.52f, 1f), // rockMid (đá xám xanh)
                new Color(0.32f, 0.36f, 0.34f, 1f), // rockDark (khe nứt đá)
                new Color(0.15f, 0.18f, 0.17f, 1f), // rockDeep
                new Color(0.48f, 0.40f, 0.30f, 1f), // rootLight
                new Color(0.20f, 0.15f, 0.10f, 1f), // rootDark
                new Color(0.44f, 0.62f, 0.38f, 1f), // moss (địa y)
                new Color(0.80f, 0.82f, 0.78f, 1f)  // pebble
            );

            // Variant 4: Đất Bùn Phù Sa Ven Sông (Alluvial Riverbank Silt & Wetland)
            GenerateVariant(
                "LanaGround_04_Alluvial_MudBank.png",
                width, height,
                GroundBiomeType.AlluvialMud,
                new Color(0.56f, 0.78f, 0.38f, 1f), // grassHigh (cỏ năng, cỏ lác)
                new Color(0.34f, 0.56f, 0.24f, 1f), // grassMid
                new Color(0.16f, 0.34f, 0.12f, 1f), // grassDark
                new Color(0.50f, 0.42f, 0.32f, 1f), // dirtHigh (phù sa khô)
                new Color(0.32f, 0.26f, 0.18f, 1f), // dirtMid (bùn phù sa ẩm)
                new Color(0.18f, 0.14f, 0.10f, 1f), // dirtDark (bùn đen sông)
                new Color(0.09f, 0.07f, 0.05f, 1f), // dirtDeep
                new Color(0.44f, 0.34f, 0.20f, 1f), // rootLight
                new Color(0.15f, 0.11f, 0.06f, 1f), // rootDark
                new Color(0.38f, 0.56f, 0.30f, 1f), // moss
                new Color(0.65f, 0.62f, 0.58f, 1f)  // river pebble
            );

            AssetDatabase.Refresh();
            ConfigureImportAndCreateMaterials();

            Debug.Log("<color=#4ade80><b>[LanaTilemap] ĐÃ TẠO XONG 4 TEXTURE CHUẨN PLANET OF LANA!</b></color>");
        }

        public enum GroundBiomeType
        {
            LushForest,
            RedDirtNamBo,
            RockyKarst,
            AlluvialMud
        }

        private static void GenerateVariant(
            string filename, int width, int height,
            GroundBiomeType biome,
            Color grassHigh, Color grassMid, Color grassDark,
            Color dirtHigh, Color dirtMid, Color dirtDark, Color dirtDeep,
            Color rootLight, Color rootShadow, Color mossCol, Color pebbleCol)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
            Color bottomBlack = new Color(0.04f, 0.05f, 0.05f, 1f);
            Color[] pixels = new Color[width * height];

            float seed = (int)biome * 41.3f;

            for (int y = 0; y < height; y++)
            {
                float v = (float)y / height; // 0 = đáy tối ngầm, 1 = đỉnh thảm cỏ

                for (int x = 0; x < width; x++)
                {
                    float u = (float)x / width; // 0..1 tuần hoàn (Seamless ngang)

                    // 1. Multi-scale seamless noise
                    float nLarge = SeamlessFBM(u, v, 3, 2.5f, 11.7f + seed);
                    float nMed   = SeamlessFBM(u, v, 7, 5.5f, 43.1f + seed);
                    float nDet   = SeamlessFBM(u, v, 15, 11.0f, 87.9f + seed);
                    float nMicro = SeamlessFBM(u, v, 30, 22.0f, 129.5f + seed);

                    // 2. Viền răng cưa cỏ rủ đặc trưng Planet of Lana (Sawtooth Ragged Clumps)
                    float clumpFrequency = (biome == GroundBiomeType.RockyKarst) ? 36.0f : 28.0f;
                    float sawtooth = Mathf.Abs(Mathf.Sin(u * Mathf.PI * clumpFrequency + nLarge * 3.0f));
                    float grassWave = (nLarge - 0.5f) * 0.12f + (nMed - 0.5f) * 0.06f - sawtooth * 0.07f;
                    float grassBorder = (biome == GroundBiomeType.RockyKarst ? 0.81f : 0.76f) + grassWave;

                    Color finalColor;

                    if (v >= grassBorder)
                    {
                        // =========================================================================
                        // LỚP 1: THẢM CỎ XANH TRÊN MẶT ĐẤT (VIBRANT CEL-SHADED LAWN)
                        // =========================================================================
                        float grassDepth = (v - grassBorder) / Mathf.Max(0.001f, 1.0f - grassBorder);
                        
                        // Nét cọ cỏ vẽ tay (Gouache brush blades)
                        float bladeStrokes = grassDepth + (nDet - 0.5f) * 0.32f + (nMicro - 0.5f) * 0.15f;

                        if (bladeStrokes > 0.58f)
                            finalColor = grassHigh;
                        else if (bladeStrokes > 0.22f)
                            finalColor = grassMid;
                        else
                            finalColor = grassDark;

                        // Chút ánh sáng viền ở mép trên cùng
                        if (v > 0.94f)
                        {
                            finalColor = Color.Lerp(finalColor, grassHigh, 0.45f);
                        }
                    }
                    else
                    {
                        // =========================================================================
                        // LỚP 2: VÁCH ĐẤT / ĐÁ CẮT NGANG CHUẨN PLANET OF LANA (STRATIFIED GOUACHE)
                        // =========================================================================
                        
                        // A. Phân tầng địa chất nằm ngang (Horizontal Sedimentary Strata)
                        float stratumWarp = (nLarge - 0.5f) * 0.08f;
                        float stratum = Mathf.Sin((v + stratumWarp) * Mathf.PI * 9.0f + nMed * 1.8f) * 0.5f + 0.5f;

                        // B. Mảng miếng vát khối Cel-Shaded (Faceted Painterly Clods)
                        float warpU = u + (nLarge - 0.5f) * 0.09f + (nMed - 0.5f) * 0.04f;
                        float warpV = v + (nMed - 0.5f) * 0.11f;

                        float clodNoise;
                        if (biome == GroundBiomeType.RockyKarst)
                        {
                            // Đá phiến: vỉa ngang sắc cạnh
                            clodNoise = SeamlessFBM(warpU, warpV * 3.0f, 5, 14.0f, 33.3f + seed) * 0.68f + stratum * 0.32f;
                        }
                        else
                        {
                            // Đất hữu cơ / Đất đỏ bazan: mảng khối gồ ghề có vân cọ
                            clodNoise = SeamlessFBM(warpU, warpV, 6, 4.8f, 25.1f + seed) * 0.65f + stratum * 0.22f + nDet * 0.13f;
                        }

                        // Lượng tử hóa 5 bậc màu tạo nhát cọ vẽ tay sắc nét
                        if (clodNoise > 0.68f)
                            finalColor = dirtHigh;
                        else if (clodNoise > 0.52f)
                            finalColor = Color.Lerp(dirtHigh, dirtMid, 0.45f);
                        else if (clodNoise > 0.38f)
                            finalColor = dirtMid;
                        else if (clodNoise > 0.24f)
                            finalColor = dirtDark;
                        else
                            finalColor = dirtDeep;

                        // C. Sỏi đá vỡ & Viên cuội nhúng trong đất (Embedded Pebbles with Facets)
                        float pebbleGridU = u * 48.0f + nLarge * 2.0f;
                        float pebbleGridV = v * 22.0f + nMed * 2.0f;
                        float pebbleCellX = Mathf.Floor(pebbleGridU);
                        float pebbleCellY = Mathf.Floor(pebbleGridV);
                        float pFracX = (pebbleGridU - pebbleCellX) - 0.5f;
                        float pFracY = (pebbleGridV - pebbleCellY) - 0.5f;
                        float pebbleDist = Mathf.Sqrt(pFracX * pFracX + pFracY * pFracY * 1.6f);

                        float pebbleChance = SeamlessNoise(pebbleCellX * 0.1f, pebbleCellY * 0.1f, 3, 2f, 77.1f + seed);
                        if (pebbleChance > 0.68f && v > 0.28f && v < grassBorder - 0.05f)
                        {
                            if (pebbleDist < 0.26f)
                            {
                                // Mặt trên bắt sáng, mặt dưới tối
                                float pLight = (-pFracY + 0.2f) * 2.2f;
                                finalColor = pLight > 0.5f ? pebbleCol : Color.Lerp(pebbleCol, dirtDeep, 0.65f);
                            }
                            else if (pebbleDist < 0.34f && pFracY < 0f)
                            {
                                // Hốc tối bao quanh chân sỏi
                                finalColor = Color.Lerp(finalColor, dirtDeep, 0.85f);
                            }
                        }

                        // D. Rễ cây hữu cơ uốn lượn (Artistic Twisting Roots - Đặc trưng Ghibli/Lana)
                        if (v > 0.18f && v < grassBorder + 0.02f)
                        {
                            // Rễ chính to uốn lượn
                            float rootTilt1 = (biome == GroundBiomeType.RedDirtNamBo) ? 0.28f : 0.18f;
                            float rootU1 = u + (1.0f - v) * rootTilt1 + (nLarge - 0.5f) * 0.10f;
                            float rootDist1 = Mathf.Abs(Mathf.Sin(rootU1 * Mathf.PI * 10.0f + nMed * 2.2f));

                            // Rễ nhánh phụ đan chéo
                            float rootTilt2 = -0.15f;
                            float rootU2 = u + (1.0f - v) * rootTilt2 + (nMed - 0.5f) * 0.08f;
                            float rootDist2 = Mathf.Abs(Mathf.Sin(rootU2 * Mathf.PI * 18.0f + nDet * 2.5f));

                            float rootThick1 = Mathf.Lerp(0.012f, 0.11f, Mathf.Clamp01((v - 0.20f) / 0.58f));
                            float rootThick2 = Mathf.Lerp(0.008f, 0.06f, Mathf.Clamp01((v - 0.35f) / 0.45f));

                            bool isMainRoot = rootDist1 < rootThick1;
                            bool isSubRoot  = (rootDist2 < rootThick2) && (v > 0.35f);

                            if (isMainRoot || isSubRoot)
                            {
                                float curDist = isMainRoot ? rootDist1 : rootDist2;
                                float curThick = isMainRoot ? rootThick1 : rootThick2;
                                float normPos = curDist / Mathf.Max(0.001f, curThick);

                                // Vát sáng cạnh trên, đổ bóng cạnh dưới
                                if (normPos < 0.45f)
                                    finalColor = rootLight;
                                else
                                    finalColor = rootShadow;
                            }
                            else if (rootDist1 < rootThick1 + 0.038f)
                            {
                                // Đổ bóng râm sâu ngay dưới thân rễ cây
                                finalColor = Color.Lerp(finalColor, dirtDeep, 0.78f);
                            }
                        }

                        // E. Đốm rêu xanh bám vách đất ẩm (Under-Canopy Moss)
                        if (v > 0.34f && v < grassBorder)
                        {
                            float mossNoise = SeamlessFBM(u + 0.25f, v, 8, 5.2f, 61.4f + seed);
                            float heightBias = Mathf.Clamp01((v - 0.34f) / 0.42f);
                            if (mossNoise + heightBias * 0.28f > 0.71f)
                            {
                                float mossShade = (mossNoise > 0.60f) ? 1.0f : 0.70f;
                                finalColor = Color.Lerp(finalColor, mossCol * mossShade, 0.88f);
                            }
                        }

                        // F. Vùng bóng đổ sâu gầm cỏ (Deep Ambient Occlusion Under Turf)
                        float distBelowGrass = grassBorder - v;
                        if (distBelowGrass > 0f && distBelowGrass < 0.065f)
                        {
                            float ao = distBelowGrass / 0.065f;
                            finalColor = Color.Lerp(dirtDeep, finalColor, Mathf.SmoothStep(0f, 1f, ao));
                        }
                    }

                    // =========================================================================
                    // LỚP 3: CHUYỂN DẦN VỀ BÓNG ĐEN ĐÁY MÀN HÌNH (BOTTOM VIGNETTE FADE)
                    // =========================================================================
                    if (v < 0.32f)
                    {
                        float fade = Mathf.SmoothStep(0f, 1f, v / 0.32f);
                        finalColor = Color.Lerp(bottomBlack, finalColor, fade);
                    }

                    pixels[y * width + x] = finalColor;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            string fullPath = Path.Combine(OutputFolder, filename);
            File.WriteAllBytes(fullPath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        private static void ConfigureImportAndCreateMaterials()
        {
            Shader roadShader = Shader.Find("HoaVaMau/LanaRoad3DDualSurface") ?? Shader.Find("Universal Render Pipeline/Lit");

            string[] files = Directory.GetFiles(OutputFolder, "*.png");
            foreach (var path in files)
            {
                string unityPath = path.Replace("\\", "/");
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
                    mat.SetVector("_SoilTexTiling", new Vector4(1f, 1f, 0f, 0f));
                    mat.SetFloat("_UseMeshUV", 1.0f);
                    mat.SetFloat("_ShadowStrength", 0.52f);
                    mat.SetFloat("_WaveDisplacement", 0.035f);

                    if (tex.name.Contains("RedDirt"))
                    {
                        mat.SetColor("_WornTrailColor", new Color(0.72f, 0.40f, 0.22f, 1f));
                    }
                    else if (tex.name.Contains("RockyKarst"))
                    {
                        mat.SetColor("_WornTrailColor", new Color(0.62f, 0.64f, 0.60f, 1f));
                    }
                    else if (tex.name.Contains("Alluvial"))
                    {
                        mat.SetColor("_WornTrailColor", new Color(0.50f, 0.42f, 0.32f, 1f));
                    }
                    else
                    {
                        mat.SetColor("_WornTrailColor", new Color(0.60f, 0.46f, 0.28f, 1f));
                    }

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

        private static float SeamlessFBM(float u, float v, int basePeriodX, float baseFreqY, float seed)
        {
            float total = 0f;
            float amplitude = 0.5f;
            float maxVal = 0f;

            for (int i = 0; i < 3; i++)
            {
                int pX = basePeriodX * (1 << i);
                float fY = baseFreqY * (1 << i);
                total += SeamlessNoise(u, v, pX, fY, seed + i * 19.3f) * amplitude;
                maxVal += amplitude;
                amplitude *= 0.5f;
            }
            return total / maxVal;
        }
    }
}
#endif
