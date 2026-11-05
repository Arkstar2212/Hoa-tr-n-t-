using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public static class ListTreesInPaddyZone
{
    [MenuItem("Tools/Hoa Do/List Trees In Paddy Zone")]
    public static void Execute()
    {
        var allT = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Debug.Log("=== TREES IN PADDY ZONE (Z 50 to 182, X -50 to 42) ===");
        int count = 0;
        foreach (var t in allT)
        {
            if (t == null) continue;
            string n = t.name.ToLower();
            if (n.Contains("tree") || n.Contains("palm") || n.Contains("coconut") || n.Contains("bamboo") || n.Contains("banana"))
            {
                // check if it is a parent tree, not a leaf sub-mesh
                if (t.parent != null && (t.parent.name.ToLower().Contains("tree") || t.parent.name.ToLower().Contains("coconut") || t.parent.name.ToLower().Contains("bamboo")))
                    continue;

                Vector3 p = t.position;
                if (p.z >= 50f && p.z <= 182f && p.x >= -50f && p.x <= 42f)
                {
                    Debug.Log($"PaddyTree #{++count}: '{t.name}' (Parent: '{t.parent?.name}') at pos ({p.x:F1}, {p.y:F1}, {p.z:F1}) active={t.gameObject.activeSelf}");
                }
            }
        }
        Debug.Log($"Total trees in paddy zone: {count}");
    }
}
