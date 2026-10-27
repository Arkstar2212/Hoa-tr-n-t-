using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

public class PainterlyGroundTextureGenerator
{
#if UNITY_EDITOR
    [MenuItem("Tools/Generate 2.5D Ground Texture (.PNG)")]
    public static void GenerateGroundTexture()
    {
        int width = 2048;
        int height = 512;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);

        // Bảng màu Painterly Cel-Shaded trích xuất từ ảnh mẫu
        Color grassHighlight = new Color(0.58f, 0.76f, 0.28f, 1f); // Xanh non có nắng
        Color grassMid       = new Color(0.32f, 0.55f, 0.20f, 1f); // Xanh lá trung gian
        Color grassDark      = new Color(0.15f, 0.30f, 0.11f, 1f); // Xanh đậm gầm cỏ
        Color mossLight      = new Color(0.42f, 0.62f, 0.24f, 1f); // Rêu sáng
        Color mossDark       = new Color(0.24f, 0.40f, 0.15f, 1f); // Rêu tối

        Color dirtLight      = new Color(0.49f, 0.32f, 0.18f, 1f); // Nâu đất bắt sáng
        Color dirtMid        = new Color(0.36f, 0.22f, 0.12f, 1f); // Nâu đất trung gian
        Color dirtDark       = new Color(0.22f, 0.13f, 0.07f, 1f); // Nâu hốc đất tối
        Color dirtDeep       = new Color(0.10f, 0.07f, 0.05f, 1f); // Nâu đen kẽ sâu
        Color bottomBlack    = new Color(0.05f, 0.06f, 0.06f, 1f); // Đen đáy màn hình

        Color rootLight      = new Color(0.42f, 0.26f, 0.14f, 1f); // Thân rễ cây sáng
        Color rootShadow     = new Color(0.14f, 0.08f, 0.04f, 1f); // Bóng rễ cây

        Color[] pixels = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            float v = (float)y / height; // 0 = đáy dưới, 1 = đỉnh cỏ trên cùng

            for (int x = 0; x < width; x++)
            {
                float u = (float)x / width; // 0 -> 1 lặp ngang Seamless

                // 1. Nhiễu tuần hoàn theo trục X (đảm bảo Seamless 100% khi lặp ngang)
                float nLarge  = SeamlessFBM(u, v, 4, 3.0f, 12.3f);
                float nMedium = SeamlessFBM(u, v, 8, 6.0f, 45.7f);
                float nDetail = SeamlessFBM(u, v, 16, 12.0f, 89.1f);

                // 2. Tính đường viền mép cỏ rủ răng cưa (Ragged Grass Edge)
                float grassWave = (nLarge - 0.5f) * 0.14f + (nMedium - 0.5f) * 0.08f + (nDetail - 0.5f) * 0.03f;
                float grassBorder = 0.78f + grassWave;

                Color finalColor;

                if (v >= grassBorder)
                {
                    // --- LỚP CỎ TRÊN MẶT ĐƯỜNG & RỦ XUỐNG ---
                    float grassDepth = (v - grassBorder) / Mathf.Max(0.001f, 1.0f - grassBorder);
                    float bladeNoise = grassDepth + (nDetail - 0.5f) * 0.35f;

                    if (bladeNoise > 0.62f)
                        finalColor = grassHighlight;
                    else if (bladeNoise > 0.25f)
                        finalColor = grassMid;
                    else
                        finalColor = grassDark;
                }
                else
                {
                    // --- LỚP VÁCH ĐẤT NÂU PHÂN MẢNG CEL-SHADED ---
                    float warpU = u + (nLarge - 0.5f) * 0.08f;
                    float warpV = v + (nMedium - 0.5f) * 0.12f;
                    float clodVal = SeamlessFBM(warpU, warpV, 6, 4.5f, 23.4f) * 0.7f + nDetail * 0.3f;

                    // Chia bậc màu (Step/Posterize) tạo khối đất vẽ tay
                    if (clodVal > 0.62f)
                        finalColor = dirtLight;
                    else if (clodVal > 0.46f)
                        finalColor = dirtMid;
                    else if (clodVal > 0.32f)
                        finalColor = dirtDark;
                    else
                        finalColor = dirtDeep;

                    // --- LỚP RỄ CÂY ĐÂM CHÉO TỪ GẦM CỎ XUỐNG ---
                    if (v > 0.22f)
                    {
                        // Nghiêng tọa độ U theo độ sâu V để rễ cây đâm chéo tự nhiên
                        float rootU = u + (1.0f - v) * 0.18f + (nLarge - 0.5f) * 0.09f;
                        float rootPattern = Mathf.Abs(Mathf.Sin(rootU * Mathf.PI * 10.0f + nMedium * 2.5f));
                        float subRoot     = Mathf.Abs(Mathf.Sin((u - (1.0f - v) * 0.12f) * Mathf.PI * 22.0f + nLarge * 4.0f));

                        float rootThickness = Mathf.Lerp(0.02f, 0.14f, Mathf.Clamp01((v - 0.25f) / 0.55f));

                        if (rootPattern < rootThickness || (subRoot < rootThickness * 0.55f && v > 0.42f))
                        {
                            finalColor = (nDetail > 0.45f) ? rootLight : rootShadow;
                        }
                        else if (rootPattern < rootThickness + 0.05f)
                        {
                            // Bóng đổ sát dưới nhánh rễ
                            finalColor = Color.Lerp(finalColor, dirtDeep, 0.75f);
                        }
                    }

                    // --- LỚP ĐỐM RÊU XANH BÁM VÁCH ĐẤT ---
                    if (v > 0.38f && v < grassBorder)
                    {
                        float mossMask = SeamlessFBM(u + 0.3f, v, 7, 5.5f, 67.8f);
                        float heightBias = Mathf.Clamp01((v - 0.38f) / 0.40f);
                        if (mossMask + heightBias * 0.25f > 0.74f)
                        {
                            finalColor = (mossMask > 0.65f) ? mossLight : mossDark;
                        }
                    }

                    // --- BÓNG ĐỔ DƯỚI GẦM THẢM CỎ (AMBIENT OCCLUSION) ---
                    float distBelowGrass = grassBorder - v;
                    if (distBelowGrass > 0f && distBelowGrass < 0.06f)
                    {
                        float ao = distBelowGrass / 0.06f;
                        finalColor = Color.Lerp(dirtDeep, finalColor, ao);
                    }
                }

                // --- LÀM TỐI DẦN XUỐNG ĐÁY MÀN HÌNH (BOTTOM FADE) ---
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

        // Lưu ra file PNG trong thư mục Assets/Textures
        string folderPath = "Assets/Textures";
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        string filePath = Path.Combine(folderPath, "PainterlyGround_Seamless.png");
        File.WriteAllBytes(filePath, tex.EncodeToPNG());
        AssetDatabase.Refresh();

        // Cấu hình tự động Repeat ngang (U) và Clamp dọc (V)
        TextureImporter importer = AssetImporter.GetAtPath(filePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.wrapModeU = TextureWrapMode.Repeat;
            importer.wrapModeV = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }

        Debug.Log("Đã tạo xong Texture mặt đất 2.5D tại: " + filePath);
    }

    // Hàm Perlin Noise tuần hoàn theo hình trụ giúp khớp mép Trái - Phải 100%
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
#endif
}
