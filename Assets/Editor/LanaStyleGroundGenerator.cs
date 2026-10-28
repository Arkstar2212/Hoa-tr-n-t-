using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

public class LanaStyleGroundGenerator
{
#if UNITY_EDITOR
    [MenuItem("Tools/Generate Lana-Style Ground Texture (.PNG)")]
    public static void GenerateTexture()
    {
        int width = 2048;
        int height = 512;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);

        // Bảng màu chuẩn phong cách Painterly / Planet of Lana
        Color grassTop    = new Color(0.48f, 0.72f, 0.26f, 1f); // Xanh non sáng (Đón ánh sáng)
        Color grassMid    = new Color(0.28f, 0.52f, 0.18f, 1f); // Xanh lá trung gian
        Color grassDark   = new Color(0.14f, 0.32f, 0.10f, 1f); // Xanh đậm gầm cỏ
        
        Color shadowSoft  = new Color(0.06f, 0.15f, 0.08f, 1f); // Bóng đổ râm dưới cỏ

        Color dirtLight   = new Color(0.45f, 0.30f, 0.18f, 1f); // Nâu đất bắt sáng
        Color dirtMid     = new Color(0.30f, 0.18f, 0.10f, 1f); // Nâu đất trầm
        Color dirtDark    = new Color(0.18f, 0.10f, 0.05f, 1f); // Nâu hốc đất sâu
        Color deepBottom  = new Color(0.04f, 0.03f, 0.03f, 1f); // Đen thẫm đáy màn hình

        Color mossColor   = new Color(0.22f, 0.44f, 0.22f, 1f); // Rêu xanh ngọc
        Color rootColor   = new Color(0.15f, 0.08f, 0.04f, 1f); // Rễ cây tối màu

        Color[] pixels = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            float v = (float)y / height; // 0 = Đáy đất, 1 = Đỉnh cỏ

            for (int x = 0; x < width; x++)
            {
                float u = (float)x / width; // 0 -> 1 Lặp ngang Seamless

                // Tạo các lớp nhiễu (Noise) mượt mà dạng cọ vẽ
                float nLarge  = SeamlessFBM(u, v, 3, 2.0f, 15.2f);
                float nMedium = SeamlessFBM(u, v, 7, 5.0f, 42.8f);
                float nDetail = SeamlessFBM(u, v, 16, 12.0f, 88.4f);

                // Đường viền thảm cỏ lượn sóng hữu cơ
                float grassHeight = 0.82f + (nLarge - 0.5f) * 0.10f + (nMedium - 0.5f) * 0.05f;

                Color finalColor;

                if (v >= grassHeight)
                {
                    // --- THẢM CỎ PHÍA TRÊN ---
                    float depthInGrass = (v - grassHeight) / Mathf.Max(0.001f, 1.0f - grassHeight);
                    float grassShade = depthInGrass * 0.7f + nDetail * 0.3f;

                    if (grassShade > 0.65f)
                        finalColor = grassTop;
                    else if (grassShade > 0.30f)
                        finalColor = grassMid;
                    else
                        finalColor = grassDark;
                }
                else
                {
                    // --- VÁCH ĐẤT NÂU PHONG CÁCH PAINTERLY ---
                    float dirtNoise = nLarge * 0.5f + nMedium * 0.3f + nDetail * 0.2f;

                    if (dirtNoise > 0.60f)
                        finalColor = dirtLight;
                    else if (dirtNoise > 0.38f)
                        finalColor = dirtMid;
                    else
                        finalColor = dirtDark;

                    // --- RỄ CÂY MỀM MẠI ---
                    float rootPattern = Mathf.Abs(Mathf.Sin((u + nMedium * 0.2f) * Mathf.PI * 8.0f));
                    if (rootPattern < 0.08f && v > 0.25f && v < grassHeight - 0.1f)
                    {
                        finalColor = rootColor;
                    }

                    // --- CÁC MẢNG RÊU BÁM ---
                    if (v > 0.45f && v < grassHeight - 0.05f)
                    {
                        float mossMask = SeamlessFBM(u + 0.4f, v, 5, 4.0f, 91.3f);
                        if (mossMask > 0.68f)
                        {
                            finalColor = Color.Lerp(finalColor, mossColor, 0.7f);
                        }
                    }

                    // --- BÓNG ĐỔ MỀM (SOFT AMBIENT OCCLUSION) DƯỚI GẦM CỎ ---
                    float distToGrass = grassHeight - v;
                    if (distToGrass < 0.08f)
                    {
                        float shadowFactor = distToGrass / 0.08f;
                        finalColor = Color.Lerp(shadowSoft, finalColor, shadowFactor);
                    }
                }

                // --- LÀM TỐI MƯỢT DẦN XUỐNG ĐÁY (VIGNETTE GRADIENT) ---
                if (v < 0.40f)
                {
                    float fade = Mathf.SmoothStep(0f, 1f, v / 0.40f);
                    finalColor = Color.Lerp(deepBottom, finalColor, fade);
                }

                pixels[y * width + x] = finalColor;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        string folderPath = "Assets/Textures";
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        string filePath = Path.Combine(folderPath, "LanaStyle_Ground_Seamless.png");
        File.WriteAllBytes(filePath, tex.EncodeToPNG());
        AssetDatabase.Refresh();

        // Tự động cấu hình texture lặp ngang (Repeat U) và khóa dọc (Clamp V)
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

        Debug.Log("Đã tạo xong Texture phong cách Planet of Lana tại: " + filePath);
    }

    private static float SeamlessNoise(float u, float v, int periodX, float freqY, float seed)
    {
        float angle = u * Mathf.PI * 2.0f * periodX;
        float nx = Mathf.Cos(angle) * 0.5f + seed;
        float nz = Mathf.Sin(angle) * 0.5f + seed;
        float ny = v * freqY + seed;

        float n1 = Mathf.PerlinNoise(nx + ny * 0.2f, nz + ny);
        float n2 = Mathf.PerlinNoise(nz - ny * 0.2f + 19.3f, nx + ny + 37.1f);
        return (n1 + n2) * 0.5f;
    }

    private static float SeamlessFBM(float u, float v, int basePeriodX, float baseFreqY, float seed)
    {
        float total = 0f, amplitude = 0.5f, maxVal = 0f;
        for (int i = 0; i < 3; i++)
        {
            total += SeamlessNoise(u, v, basePeriodX * (1 << i), baseFreqY * (1 << i), seed + i * 13.7f) * amplitude;
            maxVal += amplitude;
            amplitude *= 0.5f;
        }
        return total / maxVal;
    }
#endif
}
