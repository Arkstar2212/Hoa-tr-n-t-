#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using HoaVaMau.Gameplay;

namespace HoaVaMau.Editor
{
    [CustomEditor(typeof(Stylized3DGrassTuft))]
    [CanEditMultipleObjects]
    public class Stylized3DGrassTuftEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            Stylized3DGrassTuft tuft = (Stylized3DGrassTuft)target;

            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            TuftEditorExtensions.LabelMeshInfo(tuft);

            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Rebuild 3D Mesh", GUILayout.Height(28)))
            {
                foreach (var t in targets)
                {
                    if (t is Stylized3DGrassTuft gt)
                    {
                        Undo.RecordObject(gt, "Rebuild 3D Grass Mesh");
                        gt.BuildTuftMesh();
                        gt.EnsureMeshAndMaterial();
                        EditorUtility.SetDirty(gt);
                    }
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Preset: 360° Star"))
            {
                tuft.Setup(TuftMeshStyle.StarCards, TuftTextureVariant.Upright, 1.2f, 0.85f);
                EditorUtility.SetDirty(tuft);
            }
            if (GUILayout.Button("Preset: Curved Fan"))
            {
                tuft.Setup(TuftMeshStyle.CurvedFan, TuftTextureVariant.Upright, 1.35f, 0.90f);
                EditorUtility.SetDirty(tuft);
            }
            if (GUILayout.Button("Preset: Meadow Patch"))
            {
                tuft.Setup(TuftMeshStyle.ClusteredPatch, TuftTextureVariant.Upright, 1.1f, 0.80f);
                EditorUtility.SetDirty(tuft);
            }

            EditorGUILayout.EndHorizontal();
        }

        [MenuItem("GameObject/3D Object/Stylized 3D Grass Tuft", false, 10)]
        public static void CreateGrassTuftGameObject(MenuCommand menuCommand)
        {
            GameObject go = new GameObject("Stylized_3D_Grass_Tuft");
            Stylized3DGrassTuft tuft = go.AddComponent<Stylized3DGrassTuft>();
            tuft.Setup(TuftMeshStyle.StarCards, TuftTextureVariant.Upright, 1.25f, 0.85f);

            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create Stylized 3D Grass Tuft");
            Selection.activeObject = go;
        }

        [MenuItem("Hoa va Mau/Generate 3D Grass Prefabs")]
        public static void GeneratePrefabsMenuCommand()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }

            // 1. Single 3D Tuft Prefab
            GameObject singleGO = new GameObject("PF_StylizedGrassTuft_3D");
            var tuftSingle = singleGO.AddComponent<Stylized3DGrassTuft>();
            tuftSingle.Setup(TuftMeshStyle.StarCards, TuftTextureVariant.Upright, 1.25f, 0.85f);
            PrefabUtility.SaveAsPrefabAsset(singleGO, "Assets/Prefabs/Foliage/PF_StylizedGrassTuft_3D.prefab");
            DestroyImmediate(singleGO);

            // 2. Meadow Cluster Prefab
            GameObject clusterGO = new GameObject("PF_StylizedGrassTuft_MeadowCluster");
            var tuftCluster = clusterGO.AddComponent<Stylized3DGrassTuft>();
            tuftCluster.Setup(TuftMeshStyle.ClusteredPatch, TuftTextureVariant.Upright, 1.15f, 0.85f);
            PrefabUtility.SaveAsPrefabAsset(clusterGO, "Assets/Prefabs/Foliage/PF_StylizedGrassTuft_MeadowCluster.prefab");
            DestroyImmediate(clusterGO);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=#40b8a6>[Hoa va Mau]</color> Successfully generated 3D grass prefabs in Assets/Prefabs/!");
        }
    }

    internal static class TuftEditorExtensions
    {
        public static void LabelMeshInfo(Stylized3DGrassTuft tuft)
        {
            MeshFilter mf = tuft.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                Mesh m = mf.sharedMesh;
                EditorGUILayout.HelpBox(
                    $"3D Volumetric Mesh: {m.vertexCount} vertices, {m.triangles.Length / 3} triangles ({tuft.Style} style)",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("No mesh built yet. Click 'Rebuild 3D Mesh'.", MessageType.Warning);
            }
        }
    }
}
#endif
