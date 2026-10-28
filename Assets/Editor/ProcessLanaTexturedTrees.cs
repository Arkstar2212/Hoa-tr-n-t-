using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class ProcessLanaTexturedTrees
{
    private const string OUTPUT_FOLDER = "Assets/Textures/Environment/Extracted_Foliage";

    [MenuItem("Tools/Process Lana Textured Trees")]
    public static void RunProcess()
    {
        string brainDir = @"C:\Users\Thanh Tài\.gemini\antigravity\brain\b29f4ebe-9f51-48b4-bcbd-551f2179ff88";
        
        string[] files = Directory.GetFiles(brainDir, "planet_of_lana_trees_textured_leaves*.jpg");
        if (files.Length == 0)
        {
            Debug.LogError("Chưa tìm thấy file ảnh textured leaves!");
            return;
        }

        List<string> fileList = new List<string>(files);
        fileList.Sort((a, b) => File.GetLastWriteTime(b).CompareTo(File.GetLastWriteTime(a)));
        string sourceImg = fileList[0];

        if (!Directory.Exists(OUTPUT_FOLDER))
        {
            Directory.CreateDirectory(OUTPUT_FOLDER);
        }

        byte[] raw = File.ReadAllBytes(sourceImg);
        Texture2D srcTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        srcTex.LoadImage(raw);

        int w = srcTex.width;
        int h = srcTex.height;
        Color32[] pixels = srcTex.GetPixels32();
        Color32[] processed = new Color32[w * h];
        int[] colPixelCount = new int[w];

        // 1. Defringe và Un-premultiply nền trắng
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
                else if (dist > 16f)
                {
                    alphaNorm = (dist - 16f) / 29f;
                    alphaNorm = Mathf.SmoothStep(0f, 1f, alphaNorm);
                }

                if (alphaNorm > 0.02f)
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

        // 2. Tìm các dải interval theo trục ngang (Projection intervals)
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
                    if ((endX - startX + 1) >= 50)
                        intervals.Add(new Vector2Int(startX, endX));
                }
            }
        }
        if (inObj && (w - startX) >= 50)
        {
            intervals.Add(new Vector2Int(startX, w - 1));
        }

        string[] names = new string[] {
            "Tree_Lana_FineLeaf_01_Straight",
            "Tree_Lana_FineLeaf_02_Curved",
            "Tree_Lana_FineLeaf_03_Forked",
            "Tree_Lana_FineLeaf_04_Layered"
        };

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

            if (!found) continue;

            int pad = 4;
            int cropX = Mathf.Max(0, iv.x - pad);
            int cropY = Mathf.Max(0, minY - pad);
            int cropMaxX = Mathf.Min(w - 1, iv.y + pad);
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

            string outPath = Path.Combine(OUTPUT_FOLDER, $"{names[i]}.png");
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
            Debug.Log($"[ProcessLanaTexturedTrees] Đã xuất Sprite: {assetPath} ({cropW}x{cropH}px)");
        }

        Object.DestroyImmediate(srcTex);
        AssetDatabase.Refresh();

        // 3. Tạo ảnh Preview so sánh độ đồng bộ
        GenerateQAComparison(brainDir, names);
    }

    private static void GenerateQAComparison(string brainDir, string[] names)
    {
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
        Debug.Log("[ProcessLanaTexturedTrees] Đã tạo preview lá chi tiết tại: " + outPreview);
    }
}
#endif
