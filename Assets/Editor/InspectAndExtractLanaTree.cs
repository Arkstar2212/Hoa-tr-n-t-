using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;

public class InspectAndExtractLanaTree
{
    [MenuItem("Tools/Inspect Lana Image")]
    public static void Inspect()
    {
        string path = @"C:\Users\Thanh Tài\.gemini\antigravity\brain\b29f4ebe-9f51-48b4-bcbd-551f2179ff88\.user_uploaded\media_1793066555269_4c8e018e.jpg";
        if (!File.Exists(path))
        {
            Debug.LogError("Not found: " + path);
            return;
        }

        byte[] bytes = File.ReadAllBytes(path);
        Texture2D tex = new Texture2D(2, 2);
        tex.LoadImage(bytes);
        Debug.Log($"[InspectLanaImage] Kích thước ảnh gốc: {tex.width} x {tex.height} px");
        Object.DestroyImmediate(tex);
    }
}
#endif
