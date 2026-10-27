using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;

public class AssembleLanaTreesFromProjectFoliage
{
    private const string OUTPUT_FOLDER = "Assets/Textures/Environment/Extracted_Foliage";

    [MenuItem("Tools/Assemble Lana Trees From Project Foliage")]
    public static void Assemble()
    {
        // 1. Load các cụm lá chuẩn trong project: cay 1 -> cay 5
        Texture2D[] foliageClumps = new Texture2D[5];
        for (int i = 0; i < 5; i++)
        {
            string p = $"Assets/Textures/Tree/cay {i + 1}.png";
            byte[] b = File.ReadAllBytes(p);
            foliageClumps[i] = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            foliageClumps[i].LoadImage(b);
        }

        string[] trunkFiles = new string[] {
            "Tree_Lana_Slender_01_Straight.png",
            "Tree_Lana_Slender_02_Curved.png",
            "Tree_Lana_Slender_03_Forked.png",
            "Tree_Lana_Slender_04_Layered.png"
        };

        for (int tIdx = 0; tIdx < trunkFiles.Length; tIdx++)
        {
            string tPath = Path.Combine(OUTPUT_FOLDER, trunkFiles[tIdx]);
            byte[] tb = File.ReadAllBytes(tPath);
            Texture2D treeTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            treeTex.LoadImage(tb);

            int tw = treeTex.width;
            int th = treeTex.height;
            Color32[] treePix = treeTex.GetPixels32();

            // Tách phần thân cây: CHỈ GIỮ LẠI PIXEL GỖ THẬT, loại bỏ hoàn toàn viền trắng/hào quang cũ
            Texture2D trunkTex = new Texture2D(tw, th, TextureFormat.RGBA32, false);
            Color32[] trunkPix = new Color32[tw * th];

            for (int y = 0; y < th; y++)
            {
                for (int x = 0; x < tw; x++)
                {
                    int idx = y * tw + x;
                    Color32 c = treePix[idx];

                    if (c.a > 80)
                    {
                        bool isFringeOrWhite = (c.r > 190 && c.g > 190 && c.b > 190);
                        bool isLeaf = (c.g > c.r + 12) && (c.g > c.b + 8);
                        bool isWood = !isFringeOrWhite && !isLeaf && (c.r < 185 && c.g < 175);

                        if (isWood)
                        {
                            trunkPix[idx] = c;
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
            trunkTex.SetPixels32(trunkPix);
            trunkTex.Apply();

            // Tạo Canvas mới rộng rãi
            int canvasW = Mathf.Max(tw + 140, 520);
            int canvasH = th + 100;
            Texture2D canvas = new Texture2D(canvasW, canvasH, TextureFormat.RGBA32, false);

            Color32[] clear = new Color32[canvasW * canvasH];
            for (int k = 0; k < clear.Length; k++) clear[k] = new Color32(0, 0, 0, 0);
            canvas.SetPixels32(clear);

            int offsetX = (canvasW - tw) / 2;
            int offsetY = 20;

            // 1. Vẽ thân cây sạch bóng lên canvas
            for (int y = 0; y < th; y++)
            {
                for (int x = 0; x < tw; x++)
                {
                    Color32 c = trunkPix[y * tw + x];
                    if (c.a > 10)
                    {
                        canvas.SetPixel(offsetX + x, offsetY + y, c);
                    }
                }
            }

            // 2. Gắn các cụm tán lá tơi xốp chuẩn dự án (cay 1..5) khớp khít với các nhánh cây
            if (tIdx == 0) // Cây 1: Thân thẳng
            {
                StampFoliage(canvas, foliageClumps[3], new Vector2(canvasW * 0.50f, canvasH * 0.81f), scale: 0.74f);
                StampFoliage(canvas, foliageClumps[1], new Vector2(canvasW * 0.42f, canvasH * 0.72f), scale: 0.60f);
                StampFoliage(canvas, foliageClumps[4], new Vector2(canvasW * 0.58f, canvasH * 0.70f), scale: 0.56f);
            }
            else if (tIdx == 1) // Cây 2: Thân cong chữ S
            {
                // Vòm lá ngọn
                StampFoliage(canvas, foliageClumps[3], new Vector2(canvasW * 0.46f, canvasH * 0.76f), scale: 0.72f);
                // Vòm lá ôm theo nhánh phụ bên phải
                StampFoliage(canvas, foliageClumps[1], new Vector2(canvasW * 0.56f, canvasH * 0.63f), scale: 0.65f);
                // Cụm nhỏ phía dưới bên trái
                StampFoliage(canvas, foliageClumps[0], new Vector2(canvasW * 0.40f, canvasH * 0.64f), scale: 0.52f);
            }
            else if (tIdx == 2) // Cây 3: Nhánh đôi chữ V
            {
                StampFoliage(canvas, foliageClumps[3], new Vector2(canvasW * 0.35f, canvasH * 0.74f), scale: 0.68f);
                StampFoliage(canvas, foliageClumps[1], new Vector2(canvasW * 0.65f, canvasH * 0.72f), scale: 0.70f);
                StampFoliage(canvas, foliageClumps[0], new Vector2(canvasW * 0.50f, canvasH * 0.68f), scale: 0.50f);
            }
            else if (tIdx == 3) // Cây 4: Phân tầng đón gió
            {
                // Tầng đỉnh cao
                StampFoliage(canvas, foliageClumps[3], new Vector2(canvasW * 0.52f, canvasH * 0.76f), scale: 0.70f);
                // Tầng giữa chĩa sang trái
                StampFoliage(canvas, foliageClumps[1], new Vector2(canvasW * 0.38f, canvasH * 0.66f), scale: 0.60f);
                // Tầng thấp ôm trọn nhánh chĩa sang phải
                StampFoliage(canvas, foliageClumps[4], new Vector2(canvasW * 0.70f, canvasH * 0.51f), scale: 0.62f);
            }

            canvas.Apply();

            // Crop khít và lưu sprite
            SaveTightSprite(canvas, trunkFiles[tIdx]);

            Object.DestroyImmediate(trunkTex);
            Object.DestroyImmediate(treeTex);
            Object.DestroyImmediate(canvas);
        }

        for (int i = 0; i < 5; i++)
        {
            Object.DestroyImmediate(foliageClumps[i]);
        }

        AssetDatabase.Refresh();
        GenerateQAComparison();
        Debug.Log("<color=green>[AssembleLanaTreesFromProjectFoliage]</color> Hoàn tất kết nối tự nhiên 100% giữa cành và lá!");
    }

    private static void StampFoliage(Texture2D canvas, Texture2D foliage, Vector2 center, float scale)
    {
        int fw = foliage.width;
        int fh = foliage.height;

        int drawW = Mathf.RoundToInt(fw * scale);
        int drawH = Mathf.RoundToInt(fh * scale);

        int startX = Mathf.RoundToInt(center.x - drawW * 0.5f);
        int startY = Mathf.RoundToInt(center.y - drawH * 0.5f);

        for (int dy = 0; dy < drawH; dy++)
        {
            int py = startY + dy;
            if (py < 0 || py >= canvas.height) continue;

            float v = (float)dy / drawH;

            for (int dx = 0; dx < drawW; dx++)
            {
                int px = startX + dx;
                if (px < 0 || px >= canvas.width) continue;

                float u = (float)dx / drawW;
                Color fc = foliage.GetPixelBilinear(u, v);

                if (fc.a > 0.01f)
                {
                    Color cur = canvas.GetPixel(px, py);
                    Color blended = Color.Lerp(cur, fc, fc.a);
                    blended.a = Mathf.Max(cur.a, fc.a);
                    canvas.SetPixel(px, py, blended);
                }
            }
        }
    }

    private static void SaveTightSprite(Texture2D canvas, string fileName)
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

        string outPath = Path.Combine(OUTPUT_FOLDER, fileName);
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
        Debug.Log($"[AssembleLanaTreesFromProjectFoliage] Đã cập nhật sprite chuẩn: {assetPath} ({cropW}x{cropH}px)");
    }

    private static void GenerateQAComparison()
    {
        string brainDir = @"C:\Users\Thanh Tài\.gemini\antigravity\brain\b29f4ebe-9f51-48b4-bcbd-551f2179ff88";
        string[] names = new string[] {
            "Tree_Lana_Slender_01_Straight",
            "Tree_Lana_Slender_02_Curved",
            "Tree_Lana_Slender_03_Forked",
            "Tree_Lana_Slender_04_Layered"
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
        string outPreview = Path.Combine(brainDir, "Lana_Authentic_Foliage_Trees_Preview.png");
        File.WriteAllBytes(outPreview, canvas.EncodeToPNG());
        Object.DestroyImmediate(canvas);
        Debug.Log("[AssembleLanaTreesFromProjectFoliage] Đã tạo preview lá chuẩn dự án tại: " + outPreview);
    }
}
#endif
