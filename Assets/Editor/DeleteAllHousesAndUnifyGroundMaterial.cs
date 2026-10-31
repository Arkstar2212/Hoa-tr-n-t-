using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace HoaDo.Editor
{
    public static class DeleteAllHousesAndUnifyGroundMaterial
    {
        [MenuItem("Tools/Hoa Do/Delete All Houses And Unify Ground Material")]
        public static void Execute()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (!activeScene.path.Contains("CaMau_1954_Village"))
            {
                Debug.LogWarning("[DeleteAllHouses] Active scene is not CaMau_1954_Village. Opening scene...");
                EditorSceneManager.OpenScene("Assets/Scenes/CaMau_1954_Village.unity");
            }

            Debug.Log("[DeleteAllHouses] Starting house deletion and ground material unification...");

            DeleteAllHouses();
            UnifyGroundMaterialAndRemoveZFighting();

            Physics.SyncTransforms();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("[DeleteAllHouses] All houses deleted and ground unified successfully!");
        }

        private static void DeleteAllHouses()
        {
            // List of all houses, huts, and attached stairs to delete
            string[] houseAndStairNames = new string[]
            {
                "House_An_StiltedHouse_3D",
                "Nha_HangXom_NhaAn",
                "Nha_BacBa_KhuLayNuoc",
                "Nha_XomVuon_BacHai",
                "Nha_XomVuon_Go",
                "Nha_NongDan_CanhDong",
                "WaterHut_RiverEdge_3D",
                "ChoiCanhLua_NorthFields",
                "Stairs_BacBa_KhuLayNuoc",
                "Stairs_XomVuon_BacHai",
                "Stairs_XomVuon_Go",
                "Flower_NhaAn_Stairs_L",
                "Flower_NhaAn_Stairs_R"
            };

            int count = 0;
            foreach (var name in houseAndStairNames)
            {
                var go = GameObject.Find(name);
                if (go != null)
                {
                    Debug.Log($"[DeleteAllHouses] Destroying house/stair: {name}");
                    Object.DestroyImmediate(go);
                    count++;
                }
            }

            Debug.Log($"[DeleteAllHouses] Successfully removed {count} houses and related stair objects.");
        }

        private static void UnifyGroundMaterialAndRemoveZFighting()
        {
            var terrainGroup = GameObject.Find("CaMau_1954_Village/Environment/Terrain_3D");
            if (terrainGroup == null)
            {
                Debug.LogError("[DeleteAllHouses] Terrain_3D group not found!");
                return;
            }

            // Load the unified landscape grass material
            Material grassMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_CaMau_Landscape_Grass.mat");
            if (grassMat == null)
            {
                grassMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Terrain/M_CaMau_Landscape_Grass.mat");
            }
            if (grassMat == null)
            {
                Debug.LogError("[DeleteAllHouses] Could not load M_CaMau_Landscape_Grass.mat!");
                return;
            }

            // 1. Remove redundant overlapping knoll cubes that cause co-planar Z-fighting
            string[] redundantKnollNames = new string[]
            {
                "Terrain_SanLang_Main",
                "Terrain_NhaAn_Knoll",
                "Terrain_VuonCay_Knoll",
                "Terrain_KhuLayNuoc_Knoll",
                "Terrain_CanhDong_Paddies"
            };

            foreach (var knollName in redundantKnollNames)
            {
                var knoll = terrainGroup.transform.Find(knollName);
                if (knoll != null)
                {
                    Debug.Log($"[DeleteAllHouses] Removing redundant overlapping knoll to prevent Z-fighting: {knollName}");
                    Object.DestroyImmediate(knoll.gameObject);
                }
            }

            // Also remove overlapping PaddyField_Northern_Base if present
            var paddyFieldBase = GameObject.Find("PaddyField_Northern_Base");
            if (paddyFieldBase != null)
            {
                Debug.Log("[DeleteAllHouses] Removing overlapping PaddyField_Northern_Base to eliminate Z-fighting.");
                Object.DestroyImmediate(paddyFieldBase);
            }

            // 2. Ensure the 3 clean, non-overlapping base ground blocks exist and have M_CaMau_Landscape_Grass
            // Block 1: Terrain_Base_Village_West (X from -65 to +14.50, Z from -8 to 36)
            var westBaseT = terrainGroup.transform.Find("Terrain_Base_Village_West");
            GameObject westBase = westBaseT != null ? westBaseT.gameObject : null;
            if (westBase == null)
            {
                westBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
                westBase.name = "Terrain_Base_Village_West";
                westBase.transform.SetParent(terrainGroup.transform, false);
            }
            westBase.transform.position = new Vector3(-25.25f, -1.0f, 14.0f);
            westBase.transform.localScale = new Vector3(79.5f, 2.0f, 44.0f);
            var mrWest = westBase.GetComponent<MeshRenderer>();
            if (mrWest != null)
            {
                mrWest.sharedMaterial = grassMat;
            }
            EditorUtility.SetDirty(westBase);

            // Block 2: Terrain_Base_Paddies_East (X from +20.50 to +65, Z from -8 to 36)
            var eastBaseT = terrainGroup.transform.Find("Terrain_Base_Paddies_East");
            GameObject eastBase = eastBaseT != null ? eastBaseT.gameObject : null;
            if (eastBase == null)
            {
                eastBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
                eastBase.name = "Terrain_Base_Paddies_East";
                eastBase.transform.SetParent(terrainGroup.transform, false);
            }
            eastBase.transform.position = new Vector3(42.75f, -1.0f, 14.0f);
            eastBase.transform.localScale = new Vector3(44.5f, 2.0f, 44.0f);
            var mrEast = eastBase.GetComponent<MeshRenderer>();
            if (mrEast != null)
            {
                mrEast.sharedMaterial = grassMat;
            }
            EditorUtility.SetDirty(eastBase);

            // Block 3: Terrain_Base_North_Expansive (X from -65 to +65, Z from 36 to 85)
            var northBaseT = terrainGroup.transform.Find("Terrain_Base_North_Expansive");
            GameObject northBase = northBaseT != null ? northBaseT.gameObject : null;
            if (northBase == null)
            {
                northBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
                northBase.name = "Terrain_Base_North_Expansive";
                northBase.transform.SetParent(terrainGroup.transform, false);
            }
            northBase.transform.position = new Vector3(0f, -1.0f, 60.5f);
            northBase.transform.localScale = new Vector3(130f, 2.0f, 49.0f);
            var mrNorth = northBase.GetComponent<MeshRenderer>();
            if (mrNorth != null)
            {
                mrNorth.sharedMaterial = grassMat;
            }
            EditorUtility.SetDirty(northBase);

            // 3. Ensure any path with default 'Lit' material is updated to M_Earthen_Trail
            Material trailMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Paths/M_Earthen_Trail.mat");
            var pathsGroup = GameObject.Find("CaMau_1954_Village/Environment/Paths_3D_Network");
            if (pathsGroup != null && trailMat != null)
            {
                foreach (Transform path in pathsGroup.transform)
                {
                    var mr = path.GetComponent<MeshRenderer>();
                    if (mr != null && (mr.sharedMaterial == null || mr.sharedMaterial.name == "Lit"))
                    {
                        mr.sharedMaterial = trailMat;
                        EditorUtility.SetDirty(path);
                    }
                }
            }
        }
    }
}
