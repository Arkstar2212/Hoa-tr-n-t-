using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;

public class BuildLanaAuthenticTexturedTrees
{
    private const string OUTPUT_FOLDER = "Assets/Textures/Environment/Extracted_Foliage";

    [MenuItem("Tools/Build Authentic Lana Foliage Trees")]
    public static void BuildTrees()
    {
        string folPath = "Assets/Textures/Tree/Lana_Tree_Foliage.png";
        byte[] folBytes = File.ReadAllBytes(folPath);
        Texture2D folTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        folTex.LoadImage(folBytes);

        // Load 4 cây hiện tại để lấy thân cây chuẩn
        string[] currentFiles = new string[] {
            "Tree_Lana_Slender_01_Straight.png",
            "Tree_Lana_Slender_02_Curved.png",
            "Tree_Lana_Slender_03_Forked.png",
            "Tree_Lana_Slender_04_Layered.png"
        };

        string[] newNames = new string[] {
            "Tree_Lana_FineLeaf_01_Straight",
            "Tree_Lana_FineLeaf_02_Curved",
            "Tree_Lana_FineLeaf_03_Forked",
            "Tree_Lana_FineLeaf_04_Layered"
        };

        for (int i = 0; i < currentFiles.Length; i++)
        {
            string treePath = Path.Combine(OUTPUT_FOLDER, currentFiles[i]);
            byte[] treeBytes = File.ReadAllBytes(treePath);
            Texture2D treeTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            treeTex.LoadImage(treeBytes);

            int tw = treeTex.width;
            int th = treeTex.height;

            // Tách riêng thân cây (lọc các pixel có màu gỗ nâu/xám: r ~ g ~ b hoặc r > g)
            // Màu gỗ thân cây Lana: R: 50..180, G: 50..170, B: 40..150 với |R - G| < 35 và G > B - 10
            // Màu lá: G > R + 25 hoặc G > B + 25 (màu xanh lá rõ rệt)
            Texture2D trunkOnly = new Texture2D(tw, th, TextureFormat.RGBA32, false);
            Color32[] treePix = treeTex.GetPixels32();
            Color32[] trunkPix = new Color32[tw * th];

            int minTrunkY = th;
            int maxTrunkY = 0;

            for (int y = 0; y < th; y++)
            {
                for (int x = 0; x < tw; x++)
                {
                    int idx = y * tw + x;
                    Color32 c = treePix[idx];

                    if (c.a > 20)
                    {
                        // Kiểm tra có phải màu gỗ thân cây không
                        bool isGreenLeaf = (c.g > c.r + 15) && (c.g > c.b + 10);
                        if (!isGreenLeaf)
                        {
                            trunkPix[idx] = c;
                            if (y < minTrunkY) minTrunkY = y;
                            if (y > maxTrunkY) maxTrunkY = y;
                        }
                        else
                        {
                            trunkPix[idx] = new Color32(0, 0, 0, 0);
                        }
                    }
                    else
                    {
                        trunkPix[idx] = new Color32(0, 0, 0, 0);
                    }
                }
            }
            trunkOnly.SetPixels32(trunkPix);
            trunkOnly.Apply();

            // Tạo canvas mới với chiều cao đủ chứa thân + tán lá chi tiết
            int targetW = Mathf.Max(tw + 80, 420);
            int targetH = th + 60;
            Texture2D resultTex = new Texture2D(targetW, targetH, TextureFormat.RGBA32, false);

            Color32[] clearPix = new Color32[targetW * targetH];
            for (int k = 0; k < clearPix.Length; k++) clearPix[k] = new Color32(0, 0, 0, 0);
            resultTex.SetPixels32(clearPix);

            // 1. Vẽ thân cây lên canvas
            int trunkOffsetX = (targetW - tw) / 2;
            int trunkOffsetY = 10;
            for (int y = 0; y < th; y++)
            {
                for (int x = 0; x < tw; x++)
                {
                    Color32 c = trunkPix[y * tw + x];
                    if (c.a > 10)
                    {
                        resultTex.SetPixel(trunkOffsetX + x, trunkOffsetY + y, c);
                    }
                }
            }

            // 2. Ghép các cụm lá chi tiết (notched leaf silhouette) từ Lana_Tree_Foliage
            // Thiết kế cụm lá cho từng cây:
            if (i == 0) // Cây 1: Thân thẳng - 2 tầng tán lá tơi xốp vươn cao
            {
                BlitFoliagePatch(folTex, resultTex, 
                    srcRect: new Rect(0.1f, 0.45f, 0.8f, 0.5f), 
                    dstCenter: new Vector2(targetW * 0.5f, targetH * 0.78f), 
                    scale: 0.42f, alphaMult: 1f);
                BlitFoliagePatch(folTex, resultTex, 
                    srcRect: new Rect(0.05f, 0.1f, 0.65f, 0.4f), 
                    dstCenter: new Vector2(targetW * 0.42f, targetH * 0.65f), 
                    scale: 0.30f, alphaMult: 0.95f);
            }
            else if (i == 1) // Cây 2: Thân cong - Tán lá nghiêng theo đường cong thân
            {
                BlitFoliagePatch(folTex, resultTex, 
                    srcRect: new Rect(0.15f, 0.4f, 0.75f, 0.55f), 
                    dstCenter: new Vector2(targetW * 0.48f, targetH * 0.80f), 
                    scale: 0.40f, alphaMult: 1f);
                BlitFoliagePatch(folTex, resultTex, 
                    srcRect: new Rect(0.3f, 0.1f, 0.65f, 0.45f), 
                    dstCenter: new Vector2(targetW * 0.62f, targetH * 0.68f), 
                    scale: 0.32f, alphaMult: 0.95f);
            }
            else if (i == 2) // Cây 3: Nhánh đôi chữ V - 2 cụm tán lá tròn đối xứng
            {
                BlitFoliagePatch(folTex, resultTex, 
                    srcRect: new Rect(0.05f, 0.35f, 0.5f, 0.55f), 
                    dstCenter: new Vector2(targetW * 0.36f, targetH * 0.76f), 
                    scale: 0.38f, alphaMult: 1f);
                BlitFoliagePatch(folTex, resultTex, 
                    srcRect: new Rect(0.45f, 0.35f, 0.5f, 0.55f), 
                    dstCenter: new Vector2(targetW * 0.66f, targetH * 0.75f), 
                    scale: 0.36f, alphaMult: 1f);
            }
            else if (i == 3) // Cây 4: Phân tầng đón gió - 3 cụm lá sole
            {
                BlitFoliagePatch(folTex, resultTex, 
                    srcRect: new Rect(0.15f, 0.5f, 0.7f, 0.45f), 
                    dstCenter: new Vector2(targetW * 0.52f, targetH * 0.82f), 
                    scale: 0.38f, alphaMult: 1f);
                BlitFoliagePatch(folTex, resultTex, 
                    srcRect: new Rect(0.05f, 0.2f, 0.55f, 0.4f), 
                    dstCenter: new Vector2(targetW * 0.38f, targetH * 0.70f), 
                    scale: 0.30f, alphaMult: 0.95f);
                BlitFoliagePatch(folTex, resultTex, 
                    srcRect: new Rect(0.4f, 0.05f, 0.55f, 0.4f), 
                    dstCenter: new Vector2(targetW * 0.64f, targetH * 0.60f), 
                    scale: 0.28f, alphaMult: 0.95f);
            }

            resultTex.Apply();

            // 3. Crop chặt viền trong suốt và lưu thành PNG
            SaveTightSprite(resultTex, newNames[i]);

            Object.DestroyImmediate(trunkOnly);
            Object.DestroyImmediate(treeTex);
            Object.DestroyImmediate(resultTex);
        }

        Object.DestroyImmediate(folTex);
        AssetDatabase.Refresh();

        // Tạo preview tổng hợp
        GenerateQAComparison();
        Debug.Log("<color=green>[BuildLanaAuthenticTexturedTrees]</color> Đã tạo xong 4 cây thân mảnh với tán lá tơi xốp chuẩn Lana!");
    }

    private static void BlitFoliagePatch(Texture2D srcFol, Texture2D dstCanvas, Rect srcRect, Vector2 dstCenter, float scale, float alphaMult)
    {
        int sw = srcFol.width;
        int sh = srcFol.height;

        int srcX = Mathf.RoundToInt(srcRect.x * sw);
        int srcY = Mathf.RoundToInt(srcRect.y * sh);
        int srcW = Mathf.RoundToInt(srcRect.width * sw);
        int srcH = Mathf.RoundToInt(srcRect.height * sh);

        int dstW = Mathf.RoundToInt(srcW * scale);
        int dstH = Mathf.RoundToInt(srcH * scale);

        int dstStartX = Mathf.RoundToInt(dstCenter.x - dstW * 0.5f);
        int dstStartY = Mathf.RoundToInt(dstCenter.y - dstH * 0.5f);

        for (int dy = 0; dy < dstH; dy++)
        {
            int py = dstStartY + dy;
            if (py < 0 || py >= dstCanvas.height) continue;

            float v = (float)dy / dstH;
            int sy = srcY + Mathf.RoundToInt(v * srcH);
            if (sy < 0 || sy >= sh) continue;

            for (int dx = 0; dx < dstW; dx++)
            {
                int px = dstStartX + dx;
                if (px < 0 || px >= dstCanvas.width) continue;

                float u = (float)dx / dstW;
                int sx = srcX + Mathf.RoundToInt(u * srcW);
                if (sx < 0 || sx >= sw) continue;

                Color sc = srcFol.GetPixel(sx, sy);
                float a = sc.a * alphaMult;

                if (a > 0.01f)
                {
                    Color cur = dstCanvas.GetPixel(px, py);
                    Color blended = Color.Lerp(cur, sc, a);
                    blended.a = Mathf.Max(cur.a, a);
                    dstCanvas.SetPixel(px, py, blended);
                }
            }
        }
    }

    private static void SaveTightSprite(Texture2D canvas, string name)
    {
        int w = canvas.width;
        int h = canvas.height;
        Color[] pixels = canvas.GetPixels();

        int minX = w - 1, maxX = 0, minY = h - 1, maxY = 0;
        bool hasPixel = false;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (pixels[y * w + x].a > 0.02f)
                {
                    hasPixel = true;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
        }

        if (!hasPixel) return;

        int pad = 4;
        int cropX = Mathf.Max(0, minX - pad);
        int cropY = Mathf.Max(0, minY - pad);
        int cropMaxX = Mathf.Min(w - 1, maxX + pad);
        int cropMaxY = Mathf.Min(h - 1, maxY + pad);

        int cropW = cropMaxX - cropX + 1;
        int cropH = cropMaxY - cropY + 1;

        Texture2D tight = new Texture2D(cropW, cropH, TextureFormat.RGBA32, false);
        Color[] cropPixels = new Color[cropW * cropH];

        for (int cy = 0; cy < cropH; cy++)
        {
            for (int cx = 0; cx < cropW; cx++)
            {
                cropPixels[cy * cropW + cx] = canvas.GetPixel(cropX + cx, cropY + cy);
            }
        }

        tight.SetPixels(cropPixels);
        tight.Apply();

        string outPath = Path.Combine(OUTPUT_FOLDER, $"{name}.png");
        File.WriteAllBytes(outPath, tight.EncodeToPNG());
        Object.DestroyImmediate(tight);

        string assetPath = outPath.Replace('\\', '/');
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            settings.spritePivot = new Vector2(0.5f, 0.0f);
            importer.SetTextureSettings(settings);

            importer.SaveAndReimport();
        }
        Debug.Log($"[BuildLanaAuthenticTexturedTrees] Đã lưu sprite: {assetPath} ({cropW}x{cropH}px)");
    }

    private static void GenerateQAComparison()
    {
        string brainDir = @"C:\Users\Thanh Tài\.gemini\antigravity\brain\b29f4ebe-9f51-48b4-bcbd-551f2179ff88";
        string[] names = new string[] {
            "Tree_Lana_FineLeaf_01_Straight",
            "Tree_Lana_FineLeaf_02_Curved",
            "Tree_Lana_FineLeaf_03_Forked",
            "Tree_Lana_FineLeaf_04_Layered"
        };

        int canvasW = 1920;
        int canvasH = 900;
        Texture2D canvas = new Texture2D(canvasW, canvasH, TextureFormat.RGBA32, false);

        Color darkChecker = new Color(0.16f, 0.18f, 0.20f, 1f);
        Color lightChecker = new Color(0.24f, 0.26f, 0.28f, 1f);
        Color[] bg = new Color[canvasW * canvasH];
        int checkSize = 32;

        for (int y = 0; y < canvasH; y++)
        {
            for (int x = 0; x < canvasW; x++)
            {
                bool isDark = ((x / checkSize) + (y / checkSize)) % 2 == 0;
                bg[y * canvasW + x] = isDark ? darkChecker : lightChecker;
            }
        }
        canvas.SetPixels(bg);

        int slotWidth = canvasW / names.Length;

        for (int i = 0; i < names.Length; i++)
        {
            string fullPath = Path.Combine(OUTPUT_FOLDER, $"{names[i]}.png");
            if (!File.Exists(fullPath)) continue;

            byte[] b = File.ReadAllBytes(fullPath);
            Texture2D t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            t.LoadImage(b);

            int maxW = slotWidth - 40;
            int maxH = canvasH - 80;
            float scale = Mathf.Min((float)maxW / t.width, (float)maxH / t.height);
            int drawW = Mathf.RoundToInt(t.width * scale);
            int drawH = Mathf.RoundToInt(t.height * scale);

            int startX = i * slotWidth + (slotWidth - drawW) / 2;
            int startY = 40;

            for (int dy = 0; dy < drawH; dy++)
            {
                int py = startY + dy;
                if (py < 0 || py >= canvasH) continue;
                for (int dx = 0; dx < drawW; dx++)
                {
                    int px = startX + dx;
                    if (px < 0 || px >= canvasW) continue;

                    Color sc = t.GetPixelBilinear((float)dx / drawW, (float)dy / drawH);
                    if (sc.a > 0.001f)
                    {
                        Color cur = canvas.GetPixel(px, py);
                        canvas.SetPixel(px, py, Color.Lerp(cur, new Color(sc.r, sc.g, sc.b, 1f), sc.a));
                    }
                }
            }
            Object.DestroyImmediate(t);
        }

        canvas.Apply();
        string outPreview = Path.Combine(brainDir, "Lana_FineLeaf_Trees_QA_Preview.png");
        File.WriteAllBytes(outPreview, canvas.EncodeToPNG());
        Object.DestroyImmediate(canvas);
        Debug.Log("[BuildLanaAuthenticTexturedTrees] Đã tạo preview lá chi tiết tại: " + outPreview);
    }
}
#endif
