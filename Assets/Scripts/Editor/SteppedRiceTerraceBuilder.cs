using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

public class SteppedRiceTerraceBuilder
{
#if UNITY_EDITOR
    private static string outputFolder = "Assets/Textures/RiceFieldLayers";

    [MenuItem("Tools/Rice Field 2.5D Layers/A. Generate True Stepped 3D Terraces Layer (.PNG)")]
    public static void GenerateTrueSteppedTerraces()
    {
        if (!Directory.Exists(outputFolder))
            Directory.CreateDirectory(outputFolder);

        int width = 2048;
        int height = 768;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
        Color[] pixels = new Color[width * height];

        // Bảng màu tương phản cao: Lúa vàng nắng - Lúa xanh - Vách đất bậc thang - Mặt nước phản chiếu
        Color paddyGolden   = new Color(0.84f, 0.80f, 0.30f, 1f); // Lúa chín vàng rực
        Color paddyGreen    = new Color(0.45f, 0.68f, 0.26f, 1f); // Lúa xanh tươi
        Color riserWallDark = new Color(0.24f, 0.20f, 0.12f, 1f); // Vách đất đứng của bậc thang (Tạo khối 3D)
        Color dikeTopEdge   = new Color(0.58f, 0.64f, 0.34f, 1f); // Mép cỏ sáng trên đỉnh bờ ruộng
        Color waterSkyBlue  = new Color(0.52f, 0.74f, 0.78f, 1f); // Mặt nước ruộng phản chiếu trời xanh
        Color distantMist   = new Color(0.58f, 0.76f, 0.68f, 1f); // Sương mù chân núi

        for (int y = 0; y < height; y++)
        {
            float v = (float)y / height;

            for (int x = 0; x < width; x++)
            {
                float u = (float)x / width;

                float nWave = SeamlessNoise(u, v, 3, 2.0f, 14.7f);
                float nDetail = SeamlessNoise(u, v, 12, 8.0f, 51.3f);

                // Tạo địa hình thung lũng: Hai bên (Trái & Phải) cao lên thành nhiều bậc thang, ở giữa thấp xuống làm ruộng nước
                float leftHill  = Mathf.SmoothStep(0.48f, 0.0f, u) * 0.26f;
                float rightHill = Mathf.SmoothStep(0.55f, 1.0f, u) * 0.20f;
                float maxHeight = 0.56f + leftHill + rightHill + (nWave - 0.5f) * 0.04f;

                if (v > maxHeight)
                {
                    pixels[y * width + x] = Color.clear; // Trong suốt phía trên để lộ Núi và Bầu trời
                    continue;
                }

                // Tính chỉ số bậc thang (Stepped Terraces) uốn lượn theo sườn đồi
                float hillSlope = leftHill * 1.4f + rightHill * 1.2f;
                float stepFreq = 9.5f;
                float rawStep = (v + hillSlope * 0.65f + Mathf.Sin(u * Mathf.PI * 3.0f) * 0.03f) * stepFreq;
                float stepIndex = Mathf.Floor(rawStep);
                float stepFrac  = rawStep - stepIndex; // 0.0 -> 1.0 trong mỗi bậc thang

                // Con đường bờ đê uốn chữ S ở giữa thung lũng
                float sCurveCenter = 0.56f + Mathf.Sin(v * Mathf.PI * 2.4f) * 0.09f * (1.0f - v);
                float distToPath = Mathf.Abs(u - sCurveCenter);

                Color col;

                // 1. Vẽ con đường đê uốn chữ S
                if (distToPath < 0.018f * (1.25f - v))
                {
                    col = (distToPath < 0.009f * (1.25f - v)) ? dikeTopEdge : riserWallDark;
                }
                // 2. Vẽ VÁCH ĐẤT ĐỨNG của từng bậc thang (Tạo cảm giác gờ cao thấp rõ rệt)
                else if (stepFrac < 0.22f && (u < 0.45f || u > 0.58f || v > 0.32f))
                {
                    col = riserWallDark;
                }
                // 3. Viền cỏ bắt sáng ngay trên mép bậc thang
                else if (stepFrac >= 0.22f && stepFrac < 0.30f)
                {
                    col = dikeTopEdge;
                }
                // 4. Mặt phẳng ruộng trên mỗi bậc thang
                else
                {
                    bool isWaterPaddy = (u > 0.22f && u < 0.56f && v < 0.38f);
                    if (isWaterPaddy)
                    {
                        // Ô ruộng nước phản chiếu ánh trời kèm vài khóm mạ non
                        float seedlingMask = SeamlessNoise(u * 2f, v * 4f, 16, 12f, 77.2f);
                        col = (seedlingMask > 0.64f) ? paddyGreen : Color.Lerp(waterSkyBlue, paddyGreen, 0.20f);
                    }
                    else
                    {
                        // Xen kẽ bậc lúa chín vàng rực và bậc lúa xanh theo từng tầng
                        float goldenMix = Mathf.PingPong(stepIndex * 0.45f + nDetail * 0.3f, 1.0f);
                        col = Color.Lerp(paddyGreen, paddyGolden, goldenMix);
                    }
                }

                // Phủ sương mù nhẹ cho các bậc thang ở xa tít chân núi
                float distanceFade = Mathf.SmoothStep(0.42f, maxHeight, v) * 0.65f;
                col = Color.Lerp(col, distantMist, distanceFade);

                pixels[y * width + x] = col;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        string filePath = Path.Combine(outputFolder, "Layer3_RicePaddy_Terraces_Alpha.png");
        File.WriteAllBytes(filePath, tex.EncodeToPNG());
        AssetDatabase.Refresh();

        TextureImporter importer = AssetImporter.GetAtPath(filePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.alphaIsTransparency = true;
            importer.wrapModeU = TextureWrapMode.Repeat;
            importer.wrapModeV = TextureWrapMode.Clamp;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }

        Debug.Log("Đã cập nhật xong Layer Ruộng Bậc Thang 3D sắc nét tại: " + filePath);
    }

    [MenuItem("Tools/Rice Field 2.5D Layers/B. Cut Sky From Selected Painted Rice Field Image")]
    public static void CutSkyFromSelectedPaintedImage()
    {
        Texture2D selectedTex = Selection.activeObject as Texture2D;
        if (selectedTex == null)
        {
            Debug.LogWarning("Hãy click chọn tấm ảnh Cánh Đồng Lúa vẽ tay trong Project trước khi bấm nút này!");
            return;
        }

        string path = AssetDatabase.GetAssetPath(selectedTex);
        TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp != null)
        {
            imp.isReadable = true;
            imp.SaveAndReimport();
        }

        int w = selectedTex.width;
        int h = selectedTex.height;
        Color[] pixels = selectedTex.GetPixels();
        Texture2D outTex = new Texture2D(w, h, TextureFormat.RGBA32, false);

        for (int y = 0; y < h; y++)
        {
            float v = (float)y / h;
            for (int x = 0; x < w; x++)
            {
                Color c = pixels[y * w + x];
                // Nhận diện vùng bầu trời/mây sáng màu ở nửa trên bức tranh để chuyển thành Alpha trong suốt
                float brightness = (c.r + c.g + c.b) / 3.0f;
                bool isSkyRegion = (v > 0.52f && brightness > 0.72f && c.b > c.g * 0.85f);

                if (isSkyRegion)
                {
                    c.a = Mathf.Clamp01((0.78f - brightness) * 10f);
                }
                outTex.SetPixel(x, y, c);
            }
        }

        outTex.Apply();
        string outPath = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + "_NoSky_Layer.png");
        File.WriteAllBytes(outPath, outTex.EncodeToPNG());
        AssetDatabase.Refresh();

        TextureImporter outImp = AssetImporter.GetAtPath(outPath) as TextureImporter;
        if (outImp != null)
        {
            outImp.textureType = TextureImporterType.Sprite;
            outImp.alphaIsTransparency = true;
            outImp.maxTextureSize = 2048;
            outImp.SaveAndReimport();
        }

        Debug.Log("Đã tách bầu trời khỏi bức tranh Ruộng Bậc Thang thành công tại: " + outPath);
    }

    private static float SeamlessNoise(float u, float v, int periodX, float freqY, float seed)
    {
        float angle = u * Mathf.PI * 2.0f * periodX;
        float nx = Mathf.Cos(angle) * 0.5f + seed;
        float nz = Mathf.Sin(angle) * 0.5f + seed;
        float ny = v * freqY + seed;
        return (Mathf.PerlinNoise(nx + ny * 0.2f, nz + ny) + Mathf.PerlinNoise(nz - ny * 0.2f + 17.3f, nx + ny + 29.1f)) * 0.5f;
    }
#endif
}
