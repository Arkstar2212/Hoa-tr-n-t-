#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;

namespace HoaVaMau.EditorTools
{
    /// <summary>
    /// Script tự động bóc tách (Slice) bức tranh Concept thành từng Layer và từng Prop lẻ
    /// lưu vào thư mục Assets/Textures/Concept_Slices để người dùng tự do kéo thả vào Scene.
    /// </summary>
    public static class SliceConceptLayers
    {
        public const string SourceImagePath = @"C:\Users\Thanh Tài\.gemini\antigravity\brain\2d2bb4a1-16ed-41b7-ba92-9bb355dfc687\.user_uploaded\media_1793103937537_b72eb122.jpg";
        public const string OutputFolder = "Assets/Textures/Concept_Slices";

        [MenuItem("Hoa va Mau/Level Design/Cắt Tách Toàn Bộ Layer Từ Tranh Gốc (Slice Concept Layers)", false, 5)]
        public static void ExecuteSlice()
        {
            if (!File.Exists(SourceImagePath))
            {
                Debug.LogError($"[SliceConceptLayers] Không tìm thấy ảnh gốc tại: {SourceImagePath}");
                return;
            }

            if (!Directory.Exists(OutputFolder))
            {
                Directory.CreateDirectory(OutputFolder);
            }

            byte[] rawBytes = File.ReadAllBytes(SourceImagePath);
            Texture2D srcTex = new Texture2D(2, 2);
            srcTex.LoadImage(rawBytes);

            int w = srcTex.width;
            int h = srcTex.height;
            Debug.Log($"<color=#38bdf8>[SliceConceptLayers]</color> Đang đọc ảnh gốc kích thước: {w}x{h} px...");

            // 1. CẮT CÁC TẦNG DẢI CẢNH (PARALLAX HORIZONTAL LAYERS)
            // Layer 1: Toàn bộ thảm ruộng bậc thang màu nước (từ mép đường đến chân núi, có chòi lá & đống rơm)
            ExtractHorizontalStrip(srcTex, "01_RuongBacThang_ToanCanh", 0.32f, 0.64f, 0.05f, 0.06f);

            // Layer 2: Ruộng bậc thang tầng gần (ngay sau lưng nhân vật)
            ExtractHorizontalStrip(srcTex, "02_RuongBacThang_TangGan", 0.32f, 0.48f, 0.04f, 0.05f);

            // Layer 3: Ruộng bậc thang tầng xa (chòi lá, đống rơm, các nếp ruộng thoai thoải)
            ExtractHorizontalStrip(srcTex, "03_RuongBacThang_TangXa_KemChoiLa", 0.45f, 0.64f, 0.04f, 0.05f);

            // Layer 4: Dãy núi mờ sương phía xa
            ExtractHorizontalStrip(srcTex, "04_DayNui_MoSuong", 0.56f, 0.76f, 0.05f, 0.06f);

            // Layer 5: Bầu trời & Mây trắng
            ExtractHorizontalStrip(srcTex, "05_BauTroi_VaMay", 0.68f, 1.00f, 0.00f, 0.06f);

            // Layer 6: Mặt đê đất đỏ & Vách đất cắt lát
            ExtractHorizontalStrip(srcTex, "06_MatDeDatDo_VachDat", 0.12f, 0.36f, 0.04f, 0.04f);

            // Layer 7: Tiền cảnh bụi cỏ tối sát mép dưới màn hình
            ExtractHorizontalStrip(srcTex, "07_TienCanh_BuiCoToi", 0.00f, 0.16f, 0.00f, 0.04f);

            // 2. CẮT CÁC ĐẠO CỤ LẺ (INDIVIDUAL PROPS)
            // Chòi lá trên ruộng (Thatched Hut)
            ExtractSubRect(srcTex, "Prop_ChoiLa_Nho", 0.29f, 0.38f, 0.47f, 0.57f, 0.02f);

            // Đống rơm bên trái (Haystack Left)
            ExtractSubRect(srcTex, "Prop_DongRom_Trai", 0.245f, 0.30f, 0.47f, 0.55f, 0.02f);

            // Đống rơm bên phải (Haystack Right)
            ExtractSubRect(srcTex, "Prop_DongRom_Phai", 0.68f, 0.74f, 0.49f, 0.56f, 0.02f);

            AssetDatabase.Refresh();

            // Cấu hình tất cả thành Sprite 2D
            ConfigureAllAsSprites();

            EditorUtility.FocusProjectWindow();
            var folderObj = AssetDatabase.LoadAssetAtPath<Object>(OutputFolder);
            if (folderObj != null)
            {
                Selection.activeObject = folderObj;
                EditorGUIUtility.PingObject(folderObj);
            }

            Debug.Log("<color=#10b981>[SliceConceptLayers]</color> <b>HOÀN TẤT CẮT TÁCH 10 LAYER & PROP THÀNH CÔNG!</b> Xem thư mục: " + OutputFolder);
        }

        private static void ExtractHorizontalStrip(Texture2D src, string name, float yMinNorm, float yMaxNorm, float featherTopNorm, float featherBotNorm)
        {
            int w = src.width;
            int h = src.height;

            int startY = Mathf.Clamp(Mathf.FloorToInt(h * yMinNorm), 0, h - 1);
            int endY = Mathf.Clamp(Mathf.CeilToInt(h * yMaxNorm), 1, h);
            int stripH = endY - startY;

            if (stripH <= 0) return;

            Texture2D outTex = new Texture2D(w, stripH, TextureFormat.RGBA32, false);
            Color[] srcPixels = src.GetPixels(0, startY, w, stripH);
            Color[] outPixels = new Color[srcPixels.Length];

            float topFeatherPx = stripH * (featherTopNorm / (yMaxNorm - yMinNorm));
            float botFeatherPx = stripH * (featherBotNorm / (yMaxNorm - yMinNorm));

            for (int y = 0; y < stripH; y++)
            {
                float alpha = 1.0f;
                if (botFeatherPx > 0 && y < botFeatherPx)
                {
                    alpha = Mathf.SmoothStep(0f, 1f, y / botFeatherPx);
                }
                else if (topFeatherPx > 0 && y > stripH - topFeatherPx)
                {
                    alpha = Mathf.SmoothStep(1f, 0f, (y - (stripH - topFeatherPx)) / topFeatherPx);
                }

                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;
                    Color c = srcPixels[idx];
                    c.a *= alpha;
                    outPixels[idx] = c;
                }
            }

            outTex.SetPixels(outPixels);
            outTex.Apply();

            byte[] png = outTex.EncodeToPNG();
            string path = $"{OutputFolder}/{name}.png";
            File.WriteAllBytes(path, png);
            Object.DestroyImmediate(outTex);
        }

        private static void ExtractSubRect(Texture2D src, string name, float xMinNorm, float xMaxNorm, float yMinNorm, float yMaxNorm, float featherNorm)
        {
            int w = src.width;
            int h = src.height;

            int startX = Mathf.Clamp(Mathf.FloorToInt(w * xMinNorm), 0, w - 1);
            int endX = Mathf.Clamp(Mathf.CeilToInt(w * xMaxNorm), 1, w);
            int startY = Mathf.Clamp(Mathf.FloorToInt(h * yMinNorm), 0, h - 1);
            int endY = Mathf.Clamp(Mathf.CeilToInt(h * yMaxNorm), 1, h);

            int subW = endX - startX;
            int subH = endY - startY;

            if (subW <= 0 || subH <= 0) return;

            Texture2D outTex = new Texture2D(subW, subH, TextureFormat.RGBA32, false);
            Color[] srcPixels = src.GetPixels(startX, startY, subW, subH);
            Color[] outPixels = new Color[srcPixels.Length];

            float featherX = subW * 0.10f;
            float featherY = subH * 0.10f;

            for (int y = 0; y < subH; y++)
            {
                float aY = 1.0f;
                if (y < featherY) aY = Mathf.SmoothStep(0f, 1f, y / featherY);
                else if (y > subH - featherY) aY = Mathf.SmoothStep(1f, 0f, (y - (subH - featherY)) / featherY);

                for (int x = 0; x < subW; x++)
                {
                    float aX = 1.0f;
                    if (x < featherX) aX = Mathf.SmoothStep(0f, 1f, x / featherX);
                    else if (x > subW - featherX) aX = Mathf.SmoothStep(1f, 0f, (x - (subW - featherX)) / featherX);

                    int idx = y * subW + x;
                    Color c = srcPixels[idx];
                    c.a *= (aX * aY);
                    outPixels[idx] = c;
                }
            }

            outTex.SetPixels(outPixels);
            outTex.Apply();

            byte[] png = outTex.EncodeToPNG();
            string path = $"{OutputFolder}/{name}.png";
            File.WriteAllBytes(path, png);
            Object.DestroyImmediate(outTex);
        }

        private static void ConfigureAllAsSprites()
        {
            string[] files = Directory.GetFiles(OutputFolder, "*.png");
            foreach (string file in files)
            {
                string assetPath = file.Replace("\\", "/");
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
                    importer.maxTextureSize = 2048;
                    importer.SaveAndReimport();
                }
            }
        }
    }
}
#endif
