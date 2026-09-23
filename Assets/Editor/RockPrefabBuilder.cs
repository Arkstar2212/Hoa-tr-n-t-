using UnityEngine;
using UnityEditor;
using System.IO;

public static class RockPrefabBuilder
{
    [MenuItem("Tools/Rocks/Generate Rock Prefabs")]
    public static void GenerateAllRockPrefabs()
    {
        string modelsFolder = "Assets/Models/Rocks";
        string prefabsFolder = "Assets/Prefabs/Rocks";
        string materialsFolder = "Assets/Materials/Rocks";

        if (!Directory.Exists(prefabsFolder))
            Directory.CreateDirectory(prefabsFolder);

        Material rockMat = AssetDatabase.LoadAssetAtPath<Material>($"{materialsFolder}/M_Rock_Base.mat");
        Material mossMat = AssetDatabase.LoadAssetAtPath<Material>($"{materialsFolder}/M_Moss_Top.mat");

        string[] rockNames = new string[]
        {
            "Rock_Platform_Wide_01",
            "Rock_Platform_Moss_01",
            "Rock_Platform_Sharp_01",
            "Rock_Big_Boulder_01",
            "Rock_Big_Boulder_Moss_01",
            "Rock_Sharp_Spire_01",
            "Rock_Path_SteppingStone_01",
            "Rock_Path_PebbleCluster_01",
            "Rock_Background_Cliff_01"
        };

        foreach (string name in rockNames)
        {
            string fbxPath = $"{modelsFolder}/{name}.fbx";
            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (modelPrefab == null)
            {
                Debug.LogWarning($"Could not find model at {fbxPath}");
                continue;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab);
            instance.name = name;

            MeshRenderer[] renderers = instance.GetComponentsInChildren<MeshRenderer>();
            foreach (var renderer in renderers)
            {
                Material[] mats = renderer.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] != null && mats[i].name.ToLower().Contains("moss"))
                        mats[i] = mossMat;
                    else
                        mats[i] = rockMat;
                }
                renderer.sharedMaterials = mats;

                MeshFilter mf = renderer.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    MeshCollider col = renderer.gameObject.GetComponent<MeshCollider>();
                    if (col == null)
                        col = renderer.gameObject.AddComponent<MeshCollider>();
                    col.sharedMesh = mf.sharedMesh;
                    col.convex = true;
                }
            }

            string prefabPath = $"{prefabsFolder}/PF_{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            GameObject.DestroyImmediate(instance);
            Debug.Log($"Generated prefab: {prefabPath}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("All rock prefabs successfully generated!");
    }
}
