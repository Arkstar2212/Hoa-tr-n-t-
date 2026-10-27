using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

public class ForestBackdropGenerator
{
#if UNITY_EDITOR
    [MenuItem("Tools/Generate 2.5D Forest Backdrop Layers (.PNG)")]
    public static void GenerateForestBackdrops()
    {
        string folderPath = "Assets/Textures";
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        // 1. Tạo lớp Bụi rậm tối màu chắn chân cây (Near-Background Bush Wall - Có Alpha trong suốt ở trên)
        Texture2D bushTex = GenerateDarkBushWall(2048, 512);
        string bushPath = Path.Combine(folderPath, "ForestBushWall_Seamless.png");
        File.WriteAllBytes(bushPath, bushTex.EncodeToPNG());

        // 2. Tạo lớp Rừng sương mù khóa nền phía xa (Deep Misty Forest Canopy)
        Texture2D canopyTex = GenerateMistyForestWall(2048, 1024);
        string canopyPath = Path.Combine(folderPath, "ForestMistyCanopy_Seamless.png");
        File.WriteAllBytes(canopyPath, canopyTex.EncodeToPNG());

        AssetDatabase.Refresh();
        ConfigureBackdropTexture(bushPath, true);
        ConfigureBackdropTexture(canopyPath, false);

        Debug.Log("<color=#4ade80><b>[ForestBackdropGenerator] Đã tạo xong 2 lớp phông nền rừng 2.5D tại:</b></color> " + folderPath);
    }

    // Lớp 1: Tường bụi rậm xanh đen (Đặt ở Z = 8 -> 12 để che kín khe hở dưới gốc cây)
    private static Texture2D GenerateDarkBushWall(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
        Color[] pixels = new Color[width * height];

        Color deepShadow = new Color(0.05f, 0.13f, 0.10f, 1f); // Xanh đen đáy bụi cây
        Color darkFoliage = new Color(0.09f, 0.22f, 0.16f, 1f); // Xanh rừng tối
        Color midFoliage  = new Color(0.15f, 0.32f, 0.21f, 1f); // Xanh tán bụi giữa
        Color rimLeaf     = new Color(0.24f, 0.43f, 0.25f, 1f); // Viền lá bắt sáng nhẹ

        for (int y = 0; y < height; y++)
        {
            float v = (float)y / height;
            for (int x = 0; x < width; x++)
            {
                float u = (float)x / width;

                float nDome   = SeamlessFBM(u, v, 5, 2.5f, 11.4f);
                float nCluster = SeamlessFBM(u, v, 14, 6.0f, 37.9f);
                float nLeaf   = SeamlessFBM(u, v, 32, 14.0f, 73.2f);

                // Đường viền ngọn bụi cây lượn sóng cao thấp
                float bushTop = 0.58f + (nDome - 0.5f) * 0.28f + (nCluster - 0.5f) * 0.14f + (nLeaf - 0.5f) * 0.06f;

                if (v > bushTop)
                {
                    pixels[y * width + x] = Color.clear; // Trong suốt phía trên ngọn bụi cây
                }
                else
                {
                    float depth = v / Mathf.Max(0.01f, bushTop);
                    float val = depth * 0.65f + nCluster * 0.25f + nLeaf * 0.10f;

                    Color col;
                    if (val > 0.76f) col = rimLeaf;
                    else if (val > 0.54f) col = midFoliage;
                    else if (val > 0.30f) col = darkFoliage;
                    else col = deepShadow;

                    pixels[y * width + x] = col;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    // Lớp 2: Phông nền rừng sâu + thân cây mờ + sương mù (Đặt ở Z = 18 -> 25 che toàn bộ nền xanh dương)
    private static Texture2D GenerateMistyForestWall(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
        Color[] pixels = new Color[width * height];

        Color skyMistTop   = new Color(0.62f, 0.76f, 0.65f, 1f); // Xanh sương mù ấm trên đỉnh
        Color midHaze      = new Color(0.38f, 0.57f, 0.47f, 1f); // Sương mù rừng tầng giữa
        Color distantTrees = new Color(0.18f, 0.35f, 0.28f, 1f); // Bóng cây rừng xa
        Color deepCanopy   = new Color(0.10f, 0.23f, 0.18f, 1f); // Tán lá đặc dưới thấp

        for (int y = 0; y < height; y++)
        {
            float v = (float)y / height;
            for (int x = 0; x < width; x++)
            {
                float u = (float)x / width;

                // Nền sương mù chuyển sắc mượt từ trên xuống dưới
                Color bgCol = Color.Lerp(distantTrees, skyMistTop, Mathf.SmoothStep(0.15f, 0.95f, v));

                // Tạo bóng các thân cây và bụi tre dọc ở xa
                float trunkWave1 = Mathf.Abs(Mathf.Sin(u * Mathf.PI * 18f + SeamlessNoise(u, v, 6, 2f, 5.1f) * 1.5f));
                float trunkWave2 = Mathf.Abs(Mathf.Sin(u * Mathf.PI * 34f + SeamlessNoise(u, v, 12, 3f, 21.7f) * 2.0f));

                if (trunkWave1 < 0.16f && v < 0.85f)
                {
                    bgCol = Color.Lerp(bgCol, distantTrees, 0.55f * (1f - v * 0.5f));
                }
                else if (trunkWave2 < 0.12f && v < 0.78f)
                {
                    bgCol = Color.Lerp(bgCol, midHaze, 0.45f);
                }

                // Tán lá rừng xa che phủ nửa trên và nửa dưới
                float canopyNoise = SeamlessFBM(u, v, 8, 5.0f, 54.3f);
                if (v > 0.62f && canopyNoise + (v - 0.62f) * 0.8f > 0.62f)
                {
                    bgCol = Color.Lerp(bgCol, distantTrees, 0.65f);
                }

                // Khóa chặt phần đáy bằng màu rừng thẫm để không bao giờ lộ màu sáng sát đất
                if (v < 0.35f)
                {
                    float bottomLock = Mathf.SmoothStep(0f, 1f, v / 0.35f);
                    bgCol = Color.Lerp(deepCanopy, bgCol, bottomLock);
                }

                pixels[y * width + x] = bgCol;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private static void ConfigureBackdropTexture(string path, bool alphaTransparency)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.alphaIsTransparency = alphaTransparency;
            importer.wrapModeU = TextureWrapMode.Repeat;
            importer.wrapModeV = TextureWrapMode.Clamp;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }
    }

    private static float SeamlessNoise(float u, float v, int periodX, float freqY, float seed)
    {
        float angle = u * Mathf.PI * 2.0f * periodX;
        float nx = Mathf.Cos(angle) * 0.5f + seed;
        float nz = Mathf.Sin(angle) * 0.5f + seed;
        float ny = v * freqY + seed;
        return (Mathf.PerlinNoise(nx + ny * 0.25f, nz + ny) + Mathf.PerlinNoise(nz - ny * 0.25f + 13.7f, nx + ny + 29.1f)) * 0.5f;
    }

    private static float SeamlessFBM(float u, float v, int basePeriodX, float baseFreqY, float seed)
    {
        float total = 0f, amplitude = 0.5f, maxVal = 0f;
        for (int i = 0; i < 3; i++)
        {
            total += SeamlessNoise(u, v, basePeriodX * (1 << i), baseFreqY * (1 << i), seed + i * 17.3f) * amplitude;
            maxVal += amplitude;
            amplitude *= 0.5f;
        }
        return total / maxVal;
    }
#endif
}
