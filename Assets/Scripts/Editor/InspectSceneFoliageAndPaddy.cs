using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public static class InspectSceneFoliageAndPaddy
{
    [MenuItem("Tools/Hoa Do/Inspect Foliage And Paddy")]
    public static void Execute()
    {
        Debug.Log("=== HIERARCHY ROOTS ===");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();
        foreach (var r in roots)
        {
            Debug.Log($"Root: {r.name} (children: {r.transform.childCount})");
        }

        Debug.Log("=== TREES & FOLIAGE IN SCENE ===");
        var allTransforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int treeCount = 0;
        Dictionary<string, int> treeGroups = new Dictionary<string, int>();

        foreach (var t in allTransforms)
        {
            string n = t.name.ToLower();
            if (n.Contains("tree") || n.Contains("palm") || n.Contains("coconut") || n.Contains("bamboo") || n.Contains("banana") || n.Contains("foliage") || n.Contains("bush"))
            {
                // Only count parent or prominent objects
                if (t.parent != null && (t.parent.name.ToLower().Contains("tree") || t.parent.name.ToLower().Contains("bamboo")))
                    continue;

                Vector3 pos = t.position;
                // If it's near the paddy area: X roughly -60 to 50, Z roughly 40 to 195
                if (pos.z >= 30f && pos.z <= 200f && pos.x >= -60f && pos.x <= 60f)
                {
                    string pName = t.parent != null ? t.parent.name : "ROOT";
                    if (!treeGroups.ContainsKey(pName)) treeGroups[pName] = 0;
                    treeGroups[pName]++;
                    treeCount++;
                    if (treeCount <= 30)
                    {
                        Debug.Log($"Paddy Area Foliage: '{t.name}' under '{pName}' at pos {pos}");
                    }
                }
            }
        }
        Debug.Log($"Total foliage objects near paddy: {treeCount}");
        foreach (var kvp in treeGroups)
        {
            Debug.Log($"Parent group '{kvp.Key}': {kvp.Value} foliage objects");
        }
    }
}
