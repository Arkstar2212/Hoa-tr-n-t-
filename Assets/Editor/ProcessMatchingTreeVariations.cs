using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class ProcessMatchingTreeVariations
{
    private const string OUTPUT_FOLDER = "Assets/Textures/Environment/Extracted_Foliage";

    [MenuItem("Tools/Process Matching Tree Variations")]
    public static void RunProcess()
    {
        string brainDir = @"C:\Users\Thanh Tài\.gemini\antigravity\brain\b29f4ebe-9f51-48b4-bcbd-551f2179ff88";
        string userUploadPath = Path.Combine(brainDir, @".user_uploaded\media_1793066088848_44355b46.png");

        if (!Directory.Exists(OUTPUT_FOLDER))
        {
            Directory.CreateDirectory(OUTPUT_FOLDER);
        }

        // 1. Xử lý cây gốc người dùng tải lên
        if (File.Exists(userUploadPath))
        {
            ProcessSingleTree(userUploadPath, "Tree_Ghibli_Style_01_Original");
        }

        // 2. Tìm các file batch 1 và batch 2 mới nhất
        string[] allFiles = Directory.GetFiles(brainDir, "*.jpg");
        List<string> b1Files = new List<string>();
        List<string> b2Files = new List<string>();

        foreach (var f in allFiles)
        {
            string fn = Path.GetFileName(f).ToLower();
            if (fn.Contains("anime_ghibli_tree_variations_batch1")) b1Files.Add(f);
            if (fn.Contains("anime_ghibli_tree_variations_batch2")) b2Files.Add(f);
        }

        b1Files.Sort((a, b) => File.GetLastWriteTime(b).CompareTo(File.GetLastWriteTime(a)));
        b2Files.Sort((a, b) => File.GetLastWriteTime(b).CompareTo(File.GetLastWriteTime(a)));

        if (b1Files.Count > 0)
        {
            string[] names = new string[] {
                "Tree_Ghibli_Style_02_WideCrown",
                "Tree_Ghibli_Style_03_SlenderUpright"
            };
            ProcessProjectionBatch(b1Files[0], names);
        }

        if (b2Files.Count > 0)
        {
            string[] names = new string[] {
                "Tree_Ghibli_Style_04_LeaningCurved",
                "Tree_Ghibli_Style_05_CompactLush"
            };
            ProcessProjectionBatch(b2Files[0], names);
        }

        AssetDatabase.Refresh();
        GenerateCompositePreview(brainDir);
        Debug.Log("<color=green>[ProcessMatchingTreeVariations]</color> Hoàn tất xử lý toàn bộ các biến thể cây!");
    }

    private static void ProcessSingleTree(string filePath, string outName)
    {
        byte[] raw = File.ReadAllBytes(filePath);
        Texture2D srcTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        srcTex.LoadImage(raw);

        int w = srcTex.width;
        int h = srcTex.height;
        Color32[] pixels = srcTex.GetPixels32();
        Color32[] processed = new Color32[w * h];

        int minX = w - 1, maxX = 0, minY = h - 1, maxY = 0;
        bool hasValid = false;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x;
                Color32 c = pixels[i];

                float dr = 255f - c.r;
                float dg = 255f - c.g;
                float db = 255f - c.b;
                float dist = Mathf.Sqrt(dr * dr + dg * dg + db * db);

                float alphaNorm = 0f;
                if (dist >= 45f)
                {
                    alphaNorm = 1f;
                }
                else if (dist > 18f)
                {
                    alphaNorm = (dist - 18f) / 27f;
                    alphaNorm = Mathf.SmoothStep(0f, 1f, alphaNorm);
                }

                // Nếu ảnh gốc đã có kênh alpha thì kết hợp
                if (c.a < 250)
                {
                    alphaNorm = Mathf.Min(alphaNorm, c.a / 255f);
                }

                if (alphaNorm > 0.03f)
                {
                    float invA = 1f - alphaNorm;
                    float trueR = Mathf.Clamp((c.r - invA * 255f) / alphaNorm, 0f, 255f);
                    float trueG = Mathf.Clamp((c.g - invA * 255f) / alphaNorm, 0f, 255f);
                    float trueB = Mathf.Clamp((c.b - invA * 255f) / alphaNorm, 0f, 255f);

                    processed[i] = new Color32((byte)trueR, (byte)trueG, (byte)trueB, (byte)(alphaNorm * 255f));
                    hasValid = true;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
                else
                {
                    processed[i] = new Color32(0, 0, 0, 0);
                }
            }
        }

        if (hasValid)
        {
            SaveCroppedSprite(processed, w, h, minX, maxX, minY, maxY, outName);
        }
        Object.DestroyImmediate(srcTex);
    }

    private static void ProcessProjectionBatch(string filePath, string[] names)
    {
        byte[] raw = File.ReadAllBytes(filePath);
        Texture2D srcTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        srcTex.LoadImage(raw);

        int w = srcTex.width;
        int h = srcTex.height;
        Color32[] pixels = srcTex.GetPixels32();
        Color32[] processed = new Color32[w * h];
        int[] colPixelCount = new int[w];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x;
                Color32 c = pixels[i];

                float dr = 255f - c.r;
                float dg = 255f - c.g;
                float db = 255f - c.b;
                float dist = Mathf.Sqrt(dr * dr + dg * dg + db * db);

                float alphaNorm = 0f;
                if (dist >= 45f)
                {
                    alphaNorm = 1f;
                }
                else if (dist > 18f)
                {
                    alphaNorm = (dist - 18f) / 27f;
                    alphaNorm = Mathf.SmoothStep(0f, 1f, alphaNorm);
                }

                if (alphaNorm > 0.03f)
                {
                    float invA = 1f - alphaNorm;
                    float trueR = Mathf.Clamp((c.r - invA * 255f) / alphaNorm, 0f, 255f);
                    float trueG = Mathf.Clamp((c.g - invA * 255f) / alphaNorm, 0f, 255f);
                    float trueB = Mathf.Clamp((c.b - invA * 255f) / alphaNorm, 0f, 255f);

                    processed[i] = new Color32((byte)trueR, (byte)trueG, (byte)trueB, (byte)(alphaNorm * 255f));
                    colPixelCount[x]++;
                }
                else
                {
                    processed[i] = new Color32(0, 0, 0, 0);
                }
            }
        }

        // Tìm intervals
        List<Vector2Int> intervals = new List<Vector2Int>();
        bool inObj = false;
        int startX = 0;
        for (int x = 0; x < w; x++)
        {
            if (colPixelCount[x] > 10)
            {
                if (!inObj) { inObj = true; startX = x; }
            }
            else
            {
                if (inObj)
                {
                    inObj = false;
                    int endX = x - 1;
                    if ((endX - startX + 1) >= 60)
                        intervals.Add(new Vector2Int(startX, endX));
                }
            }
        }
        if (inObj && (w - startX) >= 60)
        {
            intervals.Add(new Vector2Int(startX, w - 1));
        }

        for (int i = 0; i < intervals.Count && i < names.Length; i++)
        {
            Vector2Int iv = intervals[i];
            int minY = h - 1, maxY = 0;
            bool found = false;

            for (int y = 0; y < h; y++)
            {
                for (int x = iv.x; x <= iv.y; x++)
                {
                    if (processed[y * w + x].a > 10)
                    {
                        found = true;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            }

            if (found)
            {
                SaveCroppedSprite(processed, w, h, iv.x, iv.y, minY, maxY, names[i]);
            }
        }

        Object.DestroyImmediate(srcTex);
    }

    private static void SaveCroppedSprite(Color32[] processed, int w, int h, int minX, int maxX, int minY, int maxY, string name)
    {
        int pad = 4;
        int cropX = Mathf.Max(0, minX - pad);
        int cropY = Mathf.Max(0, minY - pad);
        int cropMaxX = Mathf.Min(w - 1, maxX + pad);
        int cropMaxY = Mathf.Min(h - 1, maxY + pad);

        int cropW = cropMaxX - cropX + 1;
        int cropH = cropMaxY - cropY + 1;

        Texture2D spriteTex = new Texture2D(cropW, cropH, TextureFormat.RGBA32, false);
        Color32[] cropPixels = new Color32[cropW * cropH];

        for (int cy = 0; cy < cropH; cy++)
        {
            for (int cx = 0; cx < cropW; cx++)
            {
                int srcX = cropX + cx;
                int srcY = cropY + cy;
                cropPixels[cy * cropW + cx] = processed[srcY * w + srcX];
            }
        }

        spriteTex.SetPixels32(cropPixels);
        spriteTex.Apply();

        string outPath = Path.Combine(OUTPUT_FOLDER, $"{name}.png");
        File.WriteAllBytes(outPath, spriteTex.EncodeToPNG());
        Object.DestroyImmediate(spriteTex);

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
        Debug.Log($"[ProcessMatchingTreeVariations] Đã xuất Sprite: {assetPath} ({cropW}x{cropH}px)");
    }

    public static void GenerateCompositePreview(string brainDir)
    {
        string[] treeFiles = new string[] {
            "Tree_Ghibli_Style_01_Original.png",
            "Tree_Ghibli_Style_02_WideCrown.png",
            "Tree_Ghibli_Style_03_SlenderUpright.png",
            "Tree_Ghibli_Style_04_LeaningCurved.png",
            "Tree_Ghibli_Style_05_CompactLush.png"
        };

        int canvasW = 2000;
        int canvasH = 750;
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

        int slotWidth = canvasW / 5;
        for (int i = 0; i < treeFiles.Length; i++)
        {
            string fullPath = Path.Combine(OUTPUT_FOLDER, treeFiles[i]);
            if (!File.Exists(fullPath)) continue;

            byte[] b = File.ReadAllBytes(fullPath);
            Texture2D t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            t.LoadImage(b);

            int maxW = slotWidth - 30;
            int maxH = canvasH - 60;
            float scale = Mathf.Min((float)maxW / t.width, (float)maxH / t.height);
            int drawW = Mathf.RoundToInt(t.width * scale);
            int drawH = Mathf.RoundToInt(t.height * scale);

            int startX = i * slotWidth + (slotWidth - drawW) / 2;
            int startY = 30; // Chân đế tiếp đất

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
        string outPreview = Path.Combine(brainDir, "Matching_Trees_QA_Preview.png");
        File.WriteAllBytes(outPreview, canvas.EncodeToPNG());
        Object.DestroyImmediate(canvas);
        Debug.Log("[ProcessMatchingTreeVariations] Đã tạo ảnh preview bộ cây tại: " + outPreview);
    }
}
#endif
