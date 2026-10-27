using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;

public class CropLanaLeaves
{
    [MenuItem("Tools/Crop Lana Leaves")]
    public static void Crop()
    {
        string path = @"C:\Users\Thanh Tài\.gemini\antigravity\brain\b29f4ebe-9f51-48b4-bcbd-551f2179ff88\.user_uploaded\media_1793066555269_4c8e018e.jpg";
        byte[] bytes = File.ReadAllBytes(path);
        Texture2D src = new Texture2D(2, 2);
        src.LoadImage(bytes);

        // Crop vùng tán lá góc trên bên trái của cây bên trái (x: 0..300, y: src.height - 350 .. src.height)
        int cropW = 320;
        int cropH = 350;
        int startX = 0;
        int startY = src.height - cropH;

        Texture2D crop = new Texture2D(cropW, cropH, TextureFormat.RGBA32, false);
        for (int y = 0; y < cropH; y++)
        {
            for (int x = 0; x < cropW; x++)
            {
                crop.SetPixel(x, y, src.GetPixel(startX + x, startY + y));
            }
        }
        crop.Apply();

        string outP = @"C:\Users\Thanh Tài\.gemini\antigravity\brain\b29f4ebe-9f51-48b4-bcbd-551f2179ff88\lana_tree_leaves_zoom.png";
        File.WriteAllBytes(outP, crop.EncodeToPNG());
        Object.DestroyImmediate(crop);
        Object.DestroyImmediate(src);
        Debug.Log("Cropped leaves zoom!");
    }
}
#endif
