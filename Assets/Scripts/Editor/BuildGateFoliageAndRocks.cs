using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace HoaVaMau.EditorScripts
{
    public static class BuildGateFoliageAndRocks
    {
        private const string RootName = "Gate_Village_Environment";

        [MenuItem("Tools/CaMau/Build Gate Foliage And Rocks")]
        public static void Build()
        {
            Debug.Log("<color=#40b8a6>[BuildGateFoliageAndRocks]</color> Bắt đầu bổ sung cây và đá nghệ thuật khu vực Cổng Làng Bắc...");

            var activeScene = SceneManager.GetActiveScene();

            // 1. Dọn dẹp root cũ nhanh gọn bằng DestroyImmediate
            var existingRoot = GameObject.Find(RootName);
            if (existingRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(existingRoot);
            }

            var root = new GameObject(RootName);

            var gate = GameObject.Find("Gate_Village_North");
            if (gate != null)
            {
                root.transform.position = gate.transform.position;
            }
            else
            {
                root.transform.position = new Vector3(-7f, 0.5f, 184f);
            }

            // Load Rock Prefabs & Material
            var rockMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Rocks/M_Mountain_Crag_Rock.mat");
            var prefabMossyBoulder = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/PF_MossyBoulder.prefab");
            var prefabRockLarge = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/PF_Rock_Large_01.prefab");
            var prefabRockOutcrop = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/PF_Rock_Outcrop.prefab");

            // Load Tree Prefabs
            var prefabCoconut = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_06_Coconut.prefab");
            var prefabBamboo = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_07_BambooGrove.prefab");
            var prefabHeroTreeSmall = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Trees/PF_Lana_Hero_Tree_Small.prefab");
            var prefabHeroTreeLayered = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Trees/PF_Lana_Hero_Tree_Layered.prefab");
            var prefabTreeSlenderTall = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Trees/PF_Lana_Tree_Var01_SlenderTall.prefab");

            // Load Bush Prefabs
            var prefabBushBroad1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_Concept_Assets/Bushes/PF_T_HD_Bush_Broadleaf_01.prefab");
            var prefabBushBroad2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_Concept_Assets/Bushes/PF_T_HD_Bush_Broadleaf_02.prefab");
            var prefabBushFlower = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_Concept_Assets/Bushes/PF_T_HD_Bush_Low_02_Flower.prefab");
            var prefabBushLow = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_Concept_Assets/Bushes/PF_T_HD_Bush_Low_01.prefab");

            // Sub-groups
            var groupWest = CreateSubGroup(root.transform, "West_Flank_Elements");
            var groupEast = CreateSubGroup(root.transform, "East_Flank_Elements");
            var groupBackdrop = CreateSubGroup(root.transform, "North_Backdrop_Elements");

            // =========================================================================
            // 2. WEST FLANK (Cánh Tây Cổng Làng - Bên trái đường nhìn vào cổng, X <= -10.0)
            // Tuyệt đối không lấn vào hành lang đường đi (X = -9.2 đến -4.8)
            // =========================================================================
            // A. Đá ôm chân cổng & vách đá nối đồi
            SpawnRock(prefabMossyBoulder, groupWest, "Rock_GateWest_Anchor", new Vector3(-10.8f, 0.45f, 184.2f), Quaternion.Euler(0f, 35f, 0f), new Vector3(1.3f, 1.3f, 1.3f), rockMat);
            SpawnRock(prefabRockOutcrop, groupWest, "Rock_GateWest_StepDown", new Vector3(-10.4f, 0.45f, 181.0f), Quaternion.Euler(0f, 110f, 5f), new Vector3(1.2f, 1.2f, 1.2f), rockMat);
            SpawnRock(prefabRockLarge, groupWest, "Rock_GateWest_Outcrop", new Vector3(-14.5f, 0.50f, 185.5f), Quaternion.Euler(0f, -25f, 0f), new Vector3(2.0f, 2.4f, 2.0f), rockMat);
            SpawnRock(prefabMossyBoulder, groupWest, "Rock_GateWest_HillBase", new Vector3(-18.0f, 0.70f, 188.0f), Quaternion.Euler(0f, 60f, -4f), new Vector3(2.6f, 3.0f, 2.6f), rockMat);
            SpawnRock(prefabRockOutcrop, groupWest, "Rock_GateWest_ForeBoulder", new Vector3(-11.8f, 0.45f, 178.0f), Quaternion.Euler(0f, -40f, 0f), new Vector3(1.4f, 1.4f, 1.4f), rockMat);

            // B. Cây cối & tre trúc Tây Cổng Làng
            SpawnPrefab(prefabCoconut, groupWest, "Tree_GateWest_Coconut01", new Vector3(-13.0f, 0.50f, 183.5f), Quaternion.Euler(0f, 40f, -5f), new Vector3(1.15f, 1.2f, 1.15f));
            SpawnPrefab(prefabCoconut, groupWest, "Tree_GateWest_Coconut02", new Vector3(-16.5f, 0.50f, 180.0f), Quaternion.Euler(0f, 115f, 6f), new Vector3(1.05f, 1.1f, 1.05f));
            SpawnPrefab(prefabBamboo, groupWest, "Tree_GateWest_BambooThicket", new Vector3(-12.5f, 0.50f, 176.5f), Quaternion.Euler(0f, -15f, 0f), new Vector3(1.2f, 1.2f, 1.2f));
            SpawnPrefab(prefabHeroTreeSmall, groupWest, "Tree_GateWest_HeroLana", new Vector3(-16.0f, 0.50f, 190.0f), Quaternion.Euler(0f, 85f, 0f), new Vector3(1.35f, 1.35f, 1.35f));

            // C. Bụi cây & hoa dại Tây
            SpawnPrefab(prefabBushBroad1, groupWest, "Bush_GateWest_Broad01", new Vector3(-11.2f, 0.50f, 183.2f), Quaternion.Euler(0f, 20f, 0f), new Vector3(1.0f, 1.0f, 1.0f));
            SpawnPrefab(prefabBushFlower, groupWest, "Bush_GateWest_Flower01", new Vector3(-10.8f, 0.50f, 180.5f), Quaternion.Euler(0f, -30f, 0f), new Vector3(0.9f, 0.9f, 0.9f));
            SpawnPrefab(prefabBushBroad2, groupWest, "Bush_GateWest_Broad02", new Vector3(-13.0f, 0.50f, 178.5f), Quaternion.Euler(0f, 65f, 0f), new Vector3(1.0f, 1.0f, 1.0f));
            SpawnPrefab(prefabBushLow, groupWest, "Bush_GateWest_Low01", new Vector3(-14.0f, 0.50f, 181.5f), Quaternion.Euler(0f, 110f, 0f), new Vector3(0.9f, 0.9f, 0.9f));

            // =========================================================================
            // 3. EAST FLANK (Cánh Đông Cổng Làng - Bên phải đường nhìn vào cổng, X >= -4.0)
            // =========================================================================
            // A. Đá ôm chân cổng & vách đá nối đồi
            SpawnRock(prefabMossyBoulder, groupEast, "Rock_GateEast_Anchor", new Vector3(-3.2f, 0.45f, 184.2f), Quaternion.Euler(0f, -45f, 0f), new Vector3(1.3f, 1.3f, 1.3f), rockMat);
            SpawnRock(prefabRockOutcrop, groupEast, "Rock_GateEast_StepDown", new Vector3(-3.6f, 0.45f, 181.0f), Quaternion.Euler(0f, -80f, -5f), new Vector3(1.2f, 1.2f, 1.2f), rockMat);
            SpawnRock(prefabRockLarge, groupEast, "Rock_GateEast_Outcrop", new Vector3(1.0f, 0.50f, 185.5f), Quaternion.Euler(0f, 30f, 0f), new Vector3(2.0f, 2.4f, 2.0f), rockMat);
            SpawnRock(prefabMossyBoulder, groupEast, "Rock_GateEast_HillBase", new Vector3(5.0f, 0.70f, 188.5f), Quaternion.Euler(0f, -70f, 5f), new Vector3(2.6f, 3.0f, 2.6f), rockMat);
            SpawnRock(prefabRockOutcrop, groupEast, "Rock_GateEast_ForeBoulder", new Vector3(-2.6f, 0.45f, 178.0f), Quaternion.Euler(0f, 55f, 0f), new Vector3(1.4f, 1.4f, 1.4f), rockMat);

            // B. Cây cối & tre trúc Đông Cổng Làng
            SpawnPrefab(prefabCoconut, groupEast, "Tree_GateEast_Coconut01", new Vector3(-1.6f, 0.50f, 183.5f), Quaternion.Euler(0f, 210f, 5f), new Vector3(1.15f, 1.2f, 1.15f));
            SpawnPrefab(prefabCoconut, groupEast, "Tree_GateEast_Coconut02", new Vector3(2.5f, 0.50f, 179.5f), Quaternion.Euler(0f, -50f, -6f), new Vector3(1.1f, 1.15f, 1.1f));
            SpawnPrefab(prefabBamboo, groupEast, "Tree_GateEast_BambooThicket", new Vector3(-2.6f, 0.50f, 176.5f), Quaternion.Euler(0f, 25f, 0f), new Vector3(1.2f, 1.2f, 1.2f));
            SpawnPrefab(prefabHeroTreeSmall, groupEast, "Tree_GateEast_HeroLana", new Vector3(2.2f, 0.50f, 190.5f), Quaternion.Euler(0f, -110f, 0f), new Vector3(1.35f, 1.35f, 1.35f));

            // C. Bụi cây & hoa dại Đông
            SpawnPrefab(prefabBushBroad1, groupEast, "Bush_GateEast_Broad01", new Vector3(-3.0f, 0.50f, 183.2f), Quaternion.Euler(0f, -15f, 0f), new Vector3(1.0f, 1.0f, 1.0f));
            SpawnPrefab(prefabBushFlower, groupEast, "Bush_GateEast_Flower01", new Vector3(-3.2f, 0.50f, 180.5f), Quaternion.Euler(0f, 40f, 0f), new Vector3(0.9f, 0.9f, 0.9f));
            SpawnPrefab(prefabBushBroad2, groupEast, "Bush_GateEast_Broad02", new Vector3(-1.4f, 0.50f, 178.5f), Quaternion.Euler(0f, -55f, 0f), new Vector3(1.0f, 1.0f, 1.0f));
            SpawnPrefab(prefabBushLow, groupEast, "Bush_GateEast_Low01", new Vector3(-0.4f, 0.50f, 181.5f), Quaternion.Euler(0f, -100f, 0f), new Vector3(0.9f, 0.9f, 0.9f));

            // =========================================================================
            // 4. NORTH BACKDROP (Hậu cảnh phía sau Cổng Làng - Nhìn từ trong làng ra cổng)
            // =========================================================================
            SpawnRock(prefabRockLarge, groupBackdrop, "Rock_Backdrop_WestMonolith", new Vector3(-12.5f, 1.0f, 196.5f), Quaternion.Euler(0f, 15f, 0f), new Vector3(3.0f, 3.8f, 2.8f), rockMat);
            SpawnRock(prefabRockLarge, groupBackdrop, "Rock_Backdrop_EastMonolith", new Vector3(-1.5f, 1.0f, 197.0f), Quaternion.Euler(0f, -35f, 0f), new Vector3(2.8f, 3.6f, 2.6f), rockMat);
            SpawnRock(prefabMossyBoulder, groupBackdrop, "Rock_Backdrop_FarWestPeak", new Vector3(-20.0f, 1.5f, 202.0f), Quaternion.Euler(0f, 45f, 0f), new Vector3(3.8f, 4.8f, 3.6f), rockMat);
            SpawnRock(prefabMossyBoulder, groupBackdrop, "Rock_Backdrop_FarEastPeak", new Vector3(7.0f, 1.5f, 203.0f), Quaternion.Euler(0f, -50f, 0f), new Vector3(3.6f, 4.6f, 3.4f), rockMat);

            SpawnPrefab(prefabTreeSlenderTall, groupBackdrop, "Tree_Backdrop_Slender01", new Vector3(-14.0f, 0.8f, 199.5f), Quaternion.identity, new Vector3(1.5f, 1.7f, 1.5f));
            SpawnPrefab(prefabTreeSlenderTall, groupBackdrop, "Tree_Backdrop_Slender02", new Vector3(0.5f, 0.8f, 200.0f), Quaternion.Euler(0f, 180f, 0f), new Vector3(1.5f, 1.7f, 1.5f));
            SpawnPrefab(prefabHeroTreeLayered, groupBackdrop, "Tree_Backdrop_Layered01", new Vector3(-9.5f, 0.6f, 203.0f), Quaternion.Euler(0f, 45f, 0f), new Vector3(1.6f, 1.8f, 1.6f));
            SpawnPrefab(prefabHeroTreeLayered, groupBackdrop, "Tree_Backdrop_Layered02", new Vector3(-4.5f, 0.6f, 204.0f), Quaternion.Euler(0f, -30f, 0f), new Vector3(1.6f, 1.8f, 1.6f));

            EditorSceneManager.MarkSceneDirty(activeScene);
            Debug.Log("<color=#40b8a6>[BuildGateFoliageAndRocks]</color> Hoàn tất! Đã thêm cây, tre trúc, dừa, bụi hoa và vách đá tự nhiên tại Cổng Làng Bắc.");
        }

        private static GameObject CreateSubGroup(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static GameObject SpawnRock(GameObject prefab, GameObject parent, string name, Vector3 pos, Quaternion rot, Vector3 scale, Material mat)
        {
            if (prefab == null) return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform);
            if (go != null)
            {
                go.name = name;
                go.transform.position = pos;
                go.transform.rotation = rot;
                go.transform.localScale = scale;
                if (mat != null)
                {
                    var rends = go.GetComponentsInChildren<Renderer>();
                    foreach (var r in rends) r.sharedMaterial = mat;
                }
            }
            return go;
        }

        private static GameObject SpawnPrefab(GameObject prefab, GameObject parent, string name, Vector3 pos, Quaternion rot, Vector3 scale)
        {
            if (prefab == null) return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform);
            if (go != null)
            {
                go.name = name;
                go.transform.position = pos;
                go.transform.rotation = rot;
                go.transform.localScale = scale;
            }
            return go;
        }
    }
}
