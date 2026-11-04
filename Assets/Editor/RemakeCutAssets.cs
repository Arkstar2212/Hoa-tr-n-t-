using UnityEngine;
using UnityEditor;
using System.IO;

public static class RemakeCutAssets
{
    [MenuItem("Tools/HoaDo/Remake Cut Assets")]
    public static void Execute()
    {
        string newPath = @"C:\Users\Thanh Tài\.gemini\antigravity\brain\1c44fbae-764e-472a-aec6-c9c375d88a28\.user_uploaded\media_1793763244185_6e90eecb.jpg";
        var srcTex = new Texture2D(2, 2);
        srcTex.LoadImage(File.ReadAllBytes(newPath));

        string projectDir = @"Assets/Textures/Cut_Assets";
        string artifactDir = @"C:\Users\Thanh Tài\.gemini\antigravity\brain\1c44fbae-764e-472a-aec6-c9c375d88a28\cut_assets";

        if (!Directory.Exists(projectDir)) Directory.CreateDirectory(projectDir);
        if (!Directory.Exists(artifactDir)) Directory.CreateDirectory(artifactDir);

        float Cubic(float x)
        {
            float a = -0.5f;
            x = Mathf.Abs(x);
            if (x <= 1.0f) return (a + 2.0f) * x * x * x - (a + 3.0f) * x * x + 1.0f;
            if (x < 2.0f) return a * x * x * x - 5.0f * a * x * x + 8.0f * a * x - 4.0f * a;
            return 0.0f;
        }

        Color SampleBicubic(int minX, int maxX, int minY, int maxY, float u, float v)
        {
            int px = Mathf.FloorToInt(u);
            int py = Mathf.FloorToInt(v);
            float fx = u - px;
            float fy = v - py;
            Color sum = Color.clear;
            float weightSum = 0;
            for (int m = -1; m <= 2; m++)
            {
                float wy = Cubic(fy - m);
                int sy = Mathf.Clamp(py + m, minY, maxY);
                for (int n = -1; n <= 2; n++)
                {
                    float wx = Cubic(fx - n);
                    int sx = Mathf.Clamp(px + n, minX, maxX);
                    float w = wx * wy;
                    sum += srcTex.GetPixel(sx, sy) * w;
                    weightSum += w;
                }
            }
            return (weightSum > 0) ? (sum / weightSum) : srcTex.GetPixel(Mathf.Clamp(px, minX, maxX), Mathf.Clamp(py, minY, maxY));
        }

        Texture2D ProcessSprite(int minX, int maxX, int minY, int maxY)
        {
            int srcW = maxX - minX + 1;
            int srcH = maxY - minY + 1;
            int scale = 2;
            int targetW = srcW * scale;
            int targetH = srcH * scale;

            Color[] highRes = new Color[targetW * targetH];
            float[] alpha = new float[targetW * targetH];
            bool[] isCore = new bool[targetW * targetH];

            for (int y = 0; y < targetH; y++)
            {
                float v = minY + (y + 0.5f) / scale - 0.5f;
                for (int x = 0; x < targetW; x++)
                {
                    float u = minX + (x + 0.5f) / scale - 0.5f;
                    Color c = SampleBicubic(minX, maxX, minY, maxY, u, v);
                    highRes[y * targetW + x] = c;

                    Color.RGBToHSV(c, out float h, out float s, out float val);
                    float lum = 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;

                    // Rigorous paper wash suppression:
                    // Any pixel with high luminance and (low saturation or high blue) is paper wash!
                    bool isPaperWash = (lum > 0.82f && (s < 0.35f || c.b > 0.62f)) || lum > 0.90f;

                    float a = 0f;
                    if (!isPaperWash)
                    {
                        float density = Mathf.Max(1f - lum, s * 0.85f);
                        a = Mathf.Clamp01((density - 0.12f) / 0.12f);
                        a = a * a * (3f - 2f * a);
                    }

                    alpha[y * targetW + x] = a;
                    isCore[y * targetW + x] = (a > 0.90f && (s > 0.25f || lum < 0.80f));
                }
            }

            // Unsharp Mask for ultra-crisp edges
            Color[] sharp = new Color[targetW * targetH];
            for (int y = 0; y < targetH; y++)
            {
                for (int x = 0; x < targetW; x++)
                {
                    Color c = highRes[y * targetW + x];
                    Color blur = Color.clear;
                    float wSum = 0;
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int ny = Mathf.Clamp(y + dy, 0, targetH - 1);
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = Mathf.Clamp(x + dx, 0, targetW - 1);
                            float w = (dx == 0 && dy == 0) ? 4f : ((dx == 0 || dy == 0) ? 2f : 1f);
                            blur += highRes[ny * targetW + nx] * w;
                            wSum += w;
                        }
                    }
                    blur /= wSum;
                    Color s = c + (c - blur) * 0.75f;
                    s.r = Mathf.Clamp01(s.r);
                    s.g = Mathf.Clamp01(s.g);
                    s.b = Mathf.Clamp01(s.b);
                    sharp[y * targetW + x] = s;
                }
            }

            // Replace edge pixel RGB with pure saturated plant color from core
            Color[] outColors = new Color[targetW * targetH];
            int searchRadius = 16;

            for (int y = 0; y < targetH; y++)
            {
                for (int x = 0; x < targetW; x++)
                {
                    int idx = y * targetW + x;
                    float a = alpha[idx];

                    if (isCore[idx])
                    {
                        outColors[idx] = new Color(sharp[idx].r, sharp[idx].g, sharp[idx].b, a);
                    }
                    else if (a > 0.001f)
                    {
                        float bestDSq = float.MaxValue;
                        Color bestC = sharp[idx];

                        int rMin = Mathf.Max(0, y - searchRadius);
                        int rMax = Mathf.Min(targetH - 1, y + searchRadius);
                        int cMin = Mathf.Max(0, x - searchRadius);
                        int cMax = Mathf.Min(targetW - 1, x + searchRadius);

                        for (int ny = rMin; ny <= rMax; ny++)
                        {
                            for (int nx = cMin; nx <= cMax; nx++)
                            {
                                int nIdx = ny * targetW + nx;
                                if (isCore[nIdx])
                                {
                                    float dSq = (nx - x) * (nx - x) + (ny - y) * (ny - y);
                                    if (dSq < bestDSq)
                                    {
                                        bestDSq = dSq;
                                        bestC = sharp[nIdx];
                                    }
                                }
                            }
                        }
                        outColors[idx] = new Color(bestC.r, bestC.g, bestC.b, a);
                    }
                    else
                    {
                        outColors[idx] = new Color(0, 0, 0, 0);
                    }
                }
            }

            // Alpha bleed into transparent border (radius 3)
            for (int y = 0; y < targetH; y++)
            {
                for (int x = 0; x < targetW; x++)
                {
                    int idx = y * targetW + x;
                    if (outColors[idx].a == 0f)
                    {
                        for (int dy = -3; dy <= 3; dy++)
                        {
                            int ny = y + dy;
                            if (ny < 0 || ny >= targetH) continue;
                            for (int dx = -3; dx <= 3; dx++)
                            {
                                int nx = x + dx;
                                if (nx < 0 || nx >= targetW) continue;
                                int nIdx = ny * targetW + nx;
                                if (outColors[nIdx].a > 0.15f)
                                {
                                    outColors[idx] = new Color(outColors[nIdx].r, outColors[nIdx].g, outColors[nIdx].b, 0f);
                                    goto BleedDone;
                                }
                            }
                        }
                    }
                    BleedDone:;
                }
            }

            // Tight bounding box crop with 2px padding
            int minTrimX = targetW, maxTrimX = 0, minTrimY = targetH, maxTrimY = 0;
            bool hasContent = false;
            for (int y = 0; y < targetH; y++)
            {
                for (int x = 0; x < targetW; x++)
                {
                    if (outColors[y * targetW + x].a > 0.01f)
                    {
                        hasContent = true;
                        if (x < minTrimX) minTrimX = x;
                        if (x > maxTrimX) maxTrimX = x;
                        if (y < minTrimY) minTrimY = y;
                        if (y > maxTrimY) maxTrimY = y;
                    }
                }
            }

            if (!hasContent)
            {
                minTrimX = 0; maxTrimX = targetW - 1;
                minTrimY = 0; maxTrimY = targetH - 1;
            }

            int pad = 2;
            minTrimX = Mathf.Max(0, minTrimX - pad);
            maxTrimX = Mathf.Min(targetW - 1, maxTrimX + pad);
            minTrimY = Mathf.Max(0, minTrimY - pad);
            maxTrimY = Mathf.Min(targetH - 1, maxTrimY + pad);

            int finalW = maxTrimX - minTrimX + 1;
            int finalH = maxTrimY - minTrimY + 1;
            var outTex = new Texture2D(finalW, finalH, TextureFormat.RGBA32, false);
            Color[] trimmedColors = new Color[finalW * finalH];

            for (int y = 0; y < finalH; y++)
            {
                for (int x = 0; x < finalW; x++)
                {
                    trimmedColors[y * finalW + x] = outColors[(minTrimY + y) * targetW + (minTrimX + x)];
                }
            }
            outTex.SetPixels(trimmedColors);
            outTex.Apply();
            return outTex;
        }

        var items = new (string name, int x0, int x1, int y0, int y1)[]
        {
            ("Sprite_01_CoLua_BuiVangLon", 32, 226, 180, 355),
            ("Sprite_02_CoLua_BuiVangVua", 232, 337, 180, 310),
            ("Sprite_03_CoLua_NhanhBongVang", 338, 396, 180, 315),
            ("Sprite_04_CoLua_BuiCoXanh", 405, 505, 180, 320),
            ("Sprite_05_CumCo_BuiVangXanh", 535, 715, 180, 310),
            ("Sprite_06_CumCo_BuiCoThap", 715, 868, 180, 300),
            ("Sprite_07_CumCo_BuiLuaGio", 868, 985, 180, 330),
            ("Sec_01_CoLua_Dai4Bui", 32, 505, 180, 355),
            ("Sec_02_CumCo_Dai3Bui", 535, 985, 180, 330),
            ("Sec_03_CoLua_Va_CumCo_TongHop", 32, 985, 180, 355)
        };

        foreach (var item in items)
        {
            var processed = ProcessSprite(item.x0, item.x1, item.y0, item.y1);
            byte[] png = processed.EncodeToPNG();

            string pFile = Path.Combine(projectDir, item.name + ".png");
            string aFile = Path.Combine(artifactDir, item.name + ".png");

            File.WriteAllBytes(pFile, png);
            File.WriteAllBytes(aFile, png);
            Debug.Log($"[RemakeCutAssets] Cleaned {item.name}: {processed.width}x{processed.height}");
        }

        AssetDatabase.Refresh();

        foreach (var item in items)
        {
            string assetPath = "Assets/Textures/Cut_Assets/" + item.name + ".png";
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
        }

        Debug.Log("[RemakeCutAssets] Zero white fringe, zero black fringe processing COMPLETE!");
    }
}
