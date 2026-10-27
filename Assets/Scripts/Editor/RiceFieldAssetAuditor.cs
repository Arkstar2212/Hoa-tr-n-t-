using UnityEngine;
using UnityEditor;
using System.Linq;

namespace HoaVaMau.EditorTools
{
    public static class RiceFieldAssetAuditor
    {
        [MenuItem("Tools/Hoa Do/Audit Rice Assets")]
        public static void Audit()
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D");
            var matches = guids
                .Select(g => AssetDatabase.GUIDToAssetPath(g))
                .Where(p => {
                    var lower = p.ToLower();
                    return lower.Contains("rice") || lower.Contains("field") || lower.Contains("lua") || lower.Contains("grass") || lower.Contains("wildflower") || lower.Contains("crop");
                })
                .Distinct()
                .OrderBy(p => p)
                .ToList();

            Debug.Log($"[RiceFieldAssetAuditor] Found {matches.Count} potential foliage/crop/grass assets:\n" + string.Join("\n", matches));
        }
    }
}
