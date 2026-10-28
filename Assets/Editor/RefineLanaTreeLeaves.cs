using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;

public class RefineLanaTreeLeaves
{
    [MenuItem("Tools/Test Foliage Composite")]
    public static void Test()
    {
        string foliagePath = "Assets/Textures/Tree/Lana_Tree_Foliage.png";
        string tree1Path = "Assets/Textures/Environment/Extracted_Foliage/Tree_Lana_Slender_01_Straight.png";

        Texture2D folTex = AssetDatabase.LoadAssetAtPath<Texture2D>(foliagePath);
        Texture2D tree1Tex = AssetDatabase.LoadAssetAtPath<Texture2D>(tree1Path);

        Debug.Log($"Foliage: {folTex.width}x{folTex.height}, Tree1: {tree1Tex.width}x{tree1Tex.height}");
    }
}
#endif
