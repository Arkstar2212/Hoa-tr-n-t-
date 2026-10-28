using UnityEditor;
using UnityEngine;

namespace HoaVaMau.Editor
{
    public static class FixObsoleteModelMaterials
    {
        [MenuItem("Tools/Fix Obsolete Model Materials")]
        public static void FixAllModelMaterials()
        {
            string[] guids = AssetDatabase.FindAssets("t:Model");
            int fixedCount = 0;

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (string.IsNullOrEmpty(path) || (!path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase) && !path.EndsWith(".obj", System.StringComparison.OrdinalIgnoreCase)))
                        continue;

                    ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
                    if (importer != null)
                    {
#pragma warning disable CS0618
                        if (importer.materialLocation == ModelImporterMaterialLocation.External)
                        {
                            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
                            EditorUtility.SetDirty(importer);
                            fixedCount++;
                        }
#pragma warning restore CS0618
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            if (fixedCount > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"<color=#10b981>[FixObsoleteModelMaterials]</color> Successfully updated {fixedCount} model(s) from obsolete External to InPrefab.");
            }
            else
            {
                Debug.Log("<color=#10b981>[FixObsoleteModelMaterials]</color> All models are already up to date.");
            }
        }
    }
}
