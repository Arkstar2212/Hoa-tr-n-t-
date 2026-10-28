using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;

public class GenerateFoliageSpriteSheetPreview
{
    private const string INPUT_FOLDER = "Assets/Textures/Environment/Extracted_Foliage";

    [MenuItem("Tools/Generate Foliage Preview")]
    public static void GeneratePreview()
    {
        string[] spriteFiles = Directory.GetFiles(INPUT_FOLDER, "*.png");
        if (spriteFiles.Length == 0) return;

        int canvasW = 1920;
        int canvasH = 1080;
        Texture2D canvas = new Texture2D(canvasW, canvasH, TextureFormat.RGBA32, false);

        Color darkChecker = new Color(0.18f, 0.20f, 0.22f, 1f);
        Color lightChecker = new Color(0.25f, 0.27f, 0.30f, 1f);
        Color[] bgPixels = new Color[canvasW * canvasH];

        // Vẽ bàn cờ nền tối để thấy rõ tuyệt đối viền alpha có bị viền trắng hay không
        int checkSize = 32;
        for (int y = 0; y < canvasH; y++)
        {
            for (int x = 0; x < canvasW; x++)
            {
                bool isDark = ((x / checkSize) + (y / checkSize)) % 2 == 0;
                bgPixels[y * canvasW + x] = isDark ? darkChecker : lightChecker;
            }
        }
        canvas.SetPixels(bgPixels);

        // Blit các sprite lên canvas
        void BlitSprite(string path, int targetX, int targetY, int maxW, int maxH)
        {
            if (!File.Exists(path)) return;
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D s = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            s.LoadImage(bytes);

            float scale = Mathf.Min((float)maxW / s.width, (float)maxH / s.height);
            int drawW = Mathf.RoundToInt(s.width * scale);
            int drawH = Mathf.RoundToInt(s.height * scale);

            for (int dy = 0; dy < drawH; dy++)
            {
                int py = targetY + dy;
                if (py < 0 || py >= canvasH) continue;

                for (int dx = 0; dx < drawW; dx++)
                {
                    int px = targetX + dx;
                    if (px < 0 || px >= canvasW) continue;

                    float u = (float)dx / drawW;
                    float v = (float)dy / drawH;
                    Color sc = s.GetPixelBilinear(u, v);

                    if (sc.a > 0.001f)
                    {
                        Color cur = canvas.GetPixel(px, py);
                        Color blended = Color.Lerp(cur, new Color(sc.r, sc.g, sc.b, 1f), sc.a);
                        canvas.SetPixel(px, py, blended);
                    }
                }
            }
            Object.DestroyImmediate(s);
        }

        // Hàng trên: 2 Cây lớn (Tree_Lana_HeroLeaning & Tree_Lana_SlenderUpright)
        BlitSprite(Path.Combine(INPUT_FOLDER, "Tree_Lana_HeroLeaning.png"), 100, 360, 650, 650);
        BlitSprite(Path.Combine(INPUT_FOLDER, "Tree_Lana_SlenderUpright.png"), 850, 360, 400, 650);

        // Hàng giữa: 4 Bụi cỏ & cây bụi
        BlitSprite(Path.Combine(INPUT_FOLDER, "Bush_Lana_GrassTuft_Large_01.png"), 80, 80, 260, 240);
        BlitSprite(Path.Combine(INPUT_FOLDER, "Bush_Lana_MeadowShrub.png"), 380, 80, 260, 240);
        BlitSprite(Path.Combine(INPUT_FOLDER, "Bush_Lana_WildGrass_Tall.png"), 680, 80, 260, 240);
        BlitSprite(Path.Combine(INPUT_FOLDER, "Bush_Lana_GrassTuft_LowFlower.png"), 980, 80, 260, 240);

        // Cột phải: 5 Cây cỏ nhỏ
        BlitSprite(Path.Combine(INPUT_FOLDER, "Plant_Lana_WildGrass_Sprig.png"), 1350, 720, 200, 280);
        BlitSprite(Path.Combine(INPUT_FOLDER, "Plant_Lana_CurvedBlade.png"), 1620, 720, 200, 280);
        BlitSprite(Path.Combine(INPUT_FOLDER, "Plant_Lana_CloverHerb.png"), 1350, 380, 200, 280);
        BlitSprite(Path.Combine(INPUT_FOLDER, "Plant_Lana_WildflowerStem.png"), 1620, 380, 200, 280);
        BlitSprite(Path.Combine(INPUT_FOLDER, "Plant_Lana_CreepingHerb.png"), 1480, 80, 240, 240);

        canvas.Apply();

        string brainDir = @"C:\Users\Thanh Tài\.gemini\antigravity\brain\b29f4ebe-9f51-48b4-bcbd-551f2179ff88";
        string outPreview = Path.Combine(brainDir, "Foliage_Sprites_Transparency_QA.png");
        File.WriteAllBytes(outPreview, canvas.EncodeToPNG());
        Object.DestroyImmediate(canvas);

        Debug.Log("[GenerateFoliageSpriteSheetPreview] Đã tạo ảnh kiểm tra alpha tại: " + outPreview);
    }
}
#endif
