using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class ProcessIsolatedFoliageSprites
{
    private const string OUTPUT_FOLDER = "Assets/Textures/Environment/Extracted_Foliage";

    [MenuItem("Tools/Extract and Defringe Foliage Sprites")]
    public static void RunExtraction()
    {
        string brainDir = @"C:\Users\Thanh Tài\.gemini\antigravity\brain\b29f4ebe-9f51-48b4-bcbd-551f2179ff88";
        
        if (!Directory.Exists(OUTPUT_FOLDER))
        {
            Directory.CreateDirectory(OUTPUT_FOLDER);
        }

        string treeFile = Path.Combine(brainDir, "artistic_trees_isolated_1793065848725.jpg");
        string tuftFile = Path.Combine(brainDir, "grass_tufts_bushes_isolated_1793065849780.jpg");
        string smallFile = Path.Combine(brainDir, "small_wild_grasses_isolated_1793065858377.jpg");

        int totalCount = 0;

        // 1. Tách cây cổ thụ & cây thân thẳng (2 cây)
        string[] treeNames = new string[] {
            "Tree_Lana_HeroLeaning",
            "Tree_Lana_SlenderUpright"
        };
        totalCount += ProcessImageProjection(treeFile, treeNames, minIntervalWidth: 50);

        // 2. Tách các bụi cỏ & lùm cây (4 bụi cỏ)
        string[] tuftNames = new string[] {
            "Bush_Lana_GrassTuft_Large_01",
            "Bush_Lana_MeadowShrub",
            "Bush_Lana_WildGrass_Tall",
            "Bush_Lana_GrassTuft_LowFlower"
        };
        totalCount += ProcessImageProjection(tuftFile, tuftNames, minIntervalWidth: 30);

        // 3. Tách các cây cỏ nhỏ & hoa dại (5 cây cỏ nhỏ)
        string[] smallNames = new string[] {
            "Plant_Lana_WildGrass_Sprig",
            "Plant_Lana_CurvedBlade",
            "Plant_Lana_CloverHerb",
            "Plant_Lana_WildflowerStem",
            "Plant_Lana_CreepingHerb"
        };
        totalCount += ProcessImageProjection(smallFile, smallNames, minIntervalWidth: 20);

        AssetDatabase.Refresh();
        Debug.Log($"<color=green>[ProcessIsolatedFoliageSprites]</color> Đã xuất thành công tổng cộng {totalCount} sprites nguyên vẹn không viền vào {OUTPUT_FOLDER}!");
    }

    private static int ProcessImageProjection(string filePath, string[] itemNames, int minIntervalWidth)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError("Không tìm thấy file: " + filePath);
            return 0;
        }

        byte[] raw = File.ReadAllBytes(filePath);
        Texture2D srcTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        srcTex.LoadImage(raw);

        int w = srcTex.width;
        int h = srcTex.height;
        Color32[] pixels = srcTex.GetPixels32();

        // Bước 1: Tính Alpha và Khử viền trắng (Defringe) trên toàn bộ ảnh
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

        // Bước 2: Tìm các dải interval [startX, endX] dựa trên khoảng trống giữa các đối tượng
        List<Vector2Int> intervals = new List<Vector2Int>();
        bool inObject = false;
        int startX = 0;

        for (int x = 0; x < w; x++)
        {
            if (colPixelCount[x] > 5) // Có pixel thực chất
            {
                if (!inObject)
                {
                    inObject = true;
                    startX = x;
                }
            }
            else
            {
                if (inObject)
                {
                    inObject = false;
                    int endX = x - 1;
                    if ((endX - startX + 1) >= minIntervalWidth)
                    {
                        intervals.Add(new Vector2Int(startX, endX));
                    }
                }
            }
        }
        if (inObject)
        {
            int endX = w - 1;
            if ((endX - startX + 1) >= minIntervalWidth)
            {
                intervals.Add(new Vector2Int(startX, endX));
            }
        }

        // Bước 3: Crop từng interval với bounding box dọc chuẩn xác
        int exported = 0;
        for (int i = 0; i < intervals.Count; i++)
        {
            Vector2Int iv = intervals[i];
            int objMinX = iv.x;
            int objMaxX = iv.y;

            // Tìm minY và maxY trong khoảng X này
            int minY = h - 1;
            int maxY = 0;
            bool hasValidPixel = false;

            for (int y = 0; y < h; y++)
            {
                for (int x = objMinX; x <= objMaxX; x++)
                {
                    if (processed[y * w + x].a > 10)
                    {
                        hasValidPixel = true;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            }

            if (!hasValidPixel) continue;

            int pad = 4;
            int cropX = Mathf.Max(0, objMinX - pad);
            int cropY = Mathf.Max(0, minY - pad);
            int cropMaxX = Mathf.Min(w - 1, objMaxX + pad);
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

            string name = (i < itemNames.Length) ? itemNames[i] : $"Foliage_{i + 1:D2}";
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

            exported++;
            Debug.Log($"[ProcessIsolatedFoliageSprites] Xuất Sprite hoàn chỉnh: {assetPath} ({cropW}x{cropH}px)");
        }

        Object.DestroyImmediate(srcTex);
        return exported;
    }
}
#endif
