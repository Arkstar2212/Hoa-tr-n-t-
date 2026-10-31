using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

namespace HoaVaMau.EditorTools
{
    public static class FixTreeAnimationHouseModelsAndRiceFieldTraversal
    {
        [MenuItem("Tools/Hoa Tren Dat Do/Execute Fix Tree House Rice Field")]
        public static void Execute()
        {
            Debug.Log("[FixTreeHouseRice] Bắt đầu sửa lỗi cây, model nhà và cánh đồng...");

            // 1. Sửa vật liệu nhà thành Two-Sided (Cull Off) để các mặt mái, hiên, tường đều render đầy đủ không lỗi
            FixHouseMaterials();

            // 2. Chỉnh lại tất cả 6 model nhà trong Scene theo đúng tỷ lệ, vị trí chân cột chạm đất và đứng thẳng 100%
            FixAllHouseModels();

            // 3. Bỏ toàn bộ animation cây (TreeWindSway3D, PlanetOfLanaFoliageSway) và chỉnh tất cả cây đứng thẳng 100% (X=0, Z=0)
            FixTreesUprightAndRemoveAnimations();

            // 4. Mở rộng cánh đồng để bé An có thể đi hết cánh đồng (dời Barrier_North từ Z=32 ra Z=78)
            FixRiceFieldTraversal();

            // 5. Cập nhật vật lý và lưu Scene
            Physics.SyncTransforms();
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("[FixTreeHouseRice] HOÀN TẤT THÀNH CÔNG: Đã bỏ animation cây, chỉnh tất cả cây đứng thẳng, sửa model nhà chuẩn đẹp, và mở thông toàn bộ cánh đồng cho bé An!");
        }

        private static void FixHouseMaterials()
        {
            var matPaths = new string[] {
                "Assets/Materials/Environment/Architecture/M_StiltedHouse_Thatched.mat",
                "Assets/Materials/Environment/Architecture/M_StiltedHouse_Wood.mat",
                "Assets/Materials/NhaSanGo.mat"
            };

            foreach (var path in matPaths)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat != null)
                {
                    if (mat.HasProperty("_Cull"))
                    {
                        mat.SetFloat("_Cull", 0f); // 0 = Cull Off (Two-Sided)
                    }
                    if (mat.HasProperty("_BaseColor"))
                    {
                        mat.SetColor("_BaseColor", Color.white);
                    }
                    if (mat.HasProperty("_Smoothness"))
                    {
                        mat.SetFloat("_Smoothness", 0.15f);
                    }
                    EditorUtility.SetDirty(mat);
                    Debug.Log($"[FixTreeHouseRice] Đã chỉnh Material {mat.name} sang Two-Sided (Cull Off).");
                }
            }
        }

        private static void FixAllHouseModels()
        {
            // Mesh nhà lá đã tối ưu (Y-up, bounds extents: 0.66, 0.95, 0.84)
            var meshThatched = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Models/Optimized/SM_StiltedHouse_Thatched_BlenderOptimized.fbx");
            var matThatched = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Architecture/M_StiltedHouse_Thatched.mat");

            // Mesh nhà gỗ (Z-up Blender gốc, scale 100, rot 270)
            var meshWood = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Materials/Meshy_AI_Stilted_Wooden_House_0929042822_texture.fbx");
            var matWood = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Architecture/M_StiltedHouse_Wood.mat");

            // 1. House_An_StiltedHouse_3D: Nhà sàn của An (0.50, 4.44, 19.50), mặt hướng Nam (rot Y = 180)
            // Chân cột nhà sàn chạm đất gò tại Y = 0.45m
            ConfigureThatchedHouse("House_An_StiltedHouse_3D",
                meshThatched, matThatched,
                new Vector3(0.50f, 4.44f, 19.50f),
                Quaternion.Euler(0f, 180f, 0f),
                new Vector3(4.20f, 4.20f, 4.20f)
            );

            // Dọn dẹp WoodenStairs_Steps phụ nếu có để dùng cầu thang tích hợp của model
            var extraStairsAn = GameObject.Find("WoodenStairs_Steps");
            if (extraStairsAn != null)
            {
                Object.DestroyImmediate(extraStairsAn);
            }

            // 2. Nha_HangXom_NhaAn: Nhà sàn hàng xóm (-7.50, 4.25, 19.50), mặt hướng Nam
            ConfigureThatchedHouse("Nha_HangXom_NhaAn",
                meshThatched, matThatched,
                new Vector3(-7.50f, 4.25f, 19.50f),
                Quaternion.Euler(0f, 180f, 0f),
                new Vector3(4.00f, 4.00f, 4.00f)
            );

            var extraStairsNeighbor = GameObject.Find("Stairs_HangXom_NhaAn");
            if (extraStairsNeighbor != null)
            {
                Object.DestroyImmediate(extraStairsNeighbor);
            }

            // 3. Nha_BacBa_KhuLayNuoc: Nhà gỗ Bác Ba ở khu giếng nước (12.50, 1.50, 14.50)
            // Chân cột chạm mặt đất tại Y = 0.05m
            ConfigureWoodHouse("Nha_BacBa_KhuLayNuoc",
                meshWood, matWood,
                new Vector3(12.50f, 1.50f, 14.50f),
                Quaternion.Euler(0f, 180f, 0f),
                new Vector3(2.20f, 2.20f, 2.20f)
            );

            // 4. Nha_XomVuon_BacHai: Nhà sàn lá Bác Hai ở vườn cây (-19.50, 3.85, 23.50)
            // Chân cột chạm đất vườn tại Y = 0.05m
            ConfigureThatchedHouse("Nha_XomVuon_BacHai",
                meshThatched, matThatched,
                new Vector3(-19.50f, 3.85f, 23.50f),
                Quaternion.Euler(0f, 170f, 0f),
                new Vector3(4.00f, 4.00f, 4.00f)
            );

            // 5. Nha_XomVuon_Go: Nhà gỗ xóm vườn (-25.50, 1.50, 23.50)
            ConfigureWoodHouse("Nha_XomVuon_Go",
                meshWood, matWood,
                new Vector3(-25.50f, 1.50f, 23.50f),
                Quaternion.Euler(0f, 150f, 0f),
                new Vector3(2.20f, 2.20f, 2.20f)
            );

            // 6. Nha_NongDan_CanhDong: Nhà nông dân ở rìa cánh đồng (26.00, 3.50, 34.00)
            // Chân cột chạm bờ ruộng tại Y = 0.08m
            ConfigureThatchedHouse("Nha_NongDan_CanhDong",
                meshThatched, matThatched,
                new Vector3(26.00f, 3.50f, 34.00f),
                Quaternion.Euler(0f, 180f, 0f),
                new Vector3(3.60f, 3.60f, 3.60f)
            );

            // Đảm bảo kênh nước không bị xén lẹm vào chân nhà Bác Ba
            var canal = GameObject.Find("Water_East_Canal");
            if (canal != null)
            {
                canal.transform.position = new Vector3(17.50f, -0.65f, 9.00f);
                canal.transform.localScale = new Vector3(0.65f, 1.00f, 4.00f);
            }
        }

        private static void ConfigureThatchedHouse(string houseName, Mesh mesh, Material mat, Vector3 worldPos, Quaternion worldRot, Vector3 rootScale)
        {
            var house = GameObject.Find(houseName);
            if (house == null) return;

            house.transform.position = worldPos;
            house.transform.rotation = worldRot;
            house.transform.localScale = rootScale;

            Transform meshChild = house.transform.Find("Mesh_0");
            if (meshChild == null)
            {
                if (house.transform.childCount > 0)
                {
                    meshChild = house.transform.GetChild(0);
                }
                else
                {
                    var go = new GameObject("Mesh_0");
                    go.transform.SetParent(house.transform, false);
                    meshChild = go.transform;
                }
            }

            meshChild.name = "Mesh_0";
            meshChild.localPosition = Vector3.zero;
            meshChild.localRotation = Quaternion.identity;
            meshChild.localScale = Vector3.one;

            var mf = meshChild.GetComponent<MeshFilter>();
            if (mf == null) mf = meshChild.gameObject.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            var mr = meshChild.GetComponent<MeshRenderer>();
            if (mr == null) mr = meshChild.gameObject.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;

            // BoxCollider chuẩn trên Root để nhân vật không đi xuyên tường
            var col = house.GetComponent<BoxCollider>();
            if (col == null) col = house.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0f, 0f);
            col.size = new Vector3(1.4f, 1.9f, 1.7f);

            EditorUtility.SetDirty(house);
            Debug.Log($"[FixTreeHouseRice] Đã cấu hình Nhà Lá: {houseName} tại {worldPos}");
        }

        private static void ConfigureWoodHouse(string houseName, Mesh mesh, Material mat, Vector3 worldPos, Quaternion worldRot, Vector3 rootScale)
        {
            var house = GameObject.Find(houseName);
            if (house == null) return;

            house.transform.position = worldPos;
            house.transform.rotation = worldRot;
            house.transform.localScale = rootScale;

            Transform meshChild = house.transform.Find("Mesh_0");
            if (meshChild == null)
            {
                if (house.transform.childCount > 0)
                {
                    meshChild = house.transform.GetChild(0);
                }
                else
                {
                    var go = new GameObject("Mesh_0");
                    go.transform.SetParent(house.transform, false);
                    meshChild = go.transform;
                }
            }

            meshChild.name = "Mesh_0";
            meshChild.localPosition = Vector3.zero;
            meshChild.localRotation = Quaternion.Euler(270f, 0f, 0f);
            meshChild.localScale = new Vector3(100f, 100f, 100f);

            var mf = meshChild.GetComponent<MeshFilter>();
            if (mf == null) mf = meshChild.gameObject.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            var mr = meshChild.GetComponent<MeshRenderer>();
            if (mr == null) mr = meshChild.gameObject.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;

            var col = house.GetComponent<BoxCollider>();
            if (col == null) col = house.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0f, 0f);
            col.size = new Vector3(1.0f, 1.3f, 1.8f);

            EditorUtility.SetDirty(house);
            Debug.Log($"[FixTreeHouseRice] Đã cấu hình Nhà Gỗ: {houseName} tại {worldPos}");
        }

        private static void FixTreesUprightAndRemoveAnimations()
        {
            var allObjects = GameObject.FindObjectsByType<Transform>(FindObjectsSortMode.None);
            int swayCount = 0;
            int straightenedCount = 0;

            // 1. Xóa toàn bộ MonoBehaviour sway / wind animation trên cây cối
            foreach (var t in allObjects)
            {
                var monos = t.GetComponents<MonoBehaviour>();
                foreach (var m in monos)
                {
                    if (m == null) continue;
                    var typeName = m.GetType().Name;
                    if (typeName.Contains("Sway") || typeName.Contains("TreeWind") || typeName.Contains("FoliageSway"))
                    {
                        Object.DestroyImmediate(m);
                        swayCount++;
                    }
                }
            }
            Debug.Log($"[FixTreeHouseRice] Đã loại bỏ hoàn toàn {swayCount} component animation/sway trên cây cối!");

            // 2. Chỉnh TẤT CẢ cây cối đứng thẳng 100% (X=0, Z=0)
            foreach (var t in allObjects)
            {
                var lname = t.name.ToLower();
                bool isTreeLike = lname.Contains("tree") || lname.Contains("palm") || lname.Contains("banana") ||
                                  lname.Contains("bamboo") || lname.Contains("cay") || lname.Contains("foliage") ||
                                  lname.Contains("chuoi") || lname.Contains("dua");

                bool isBone = lname.Contains("bone") || lname.Contains("tripo") || lname.Contains("armature");

                if (isTreeLike && !isBone)
                {
                    // Nắn thẳng 100% trục đứng: X=0, Z=0, giữ nguyên hướng Y
                    float yRot = t.eulerAngles.y;
                    t.rotation = Quaternion.Euler(0f, yRot, 0f);
                    EditorUtility.SetDirty(t);
                    straightenedCount++;
                }
            }

            Debug.Log($"[FixTreeHouseRice] Đã nắn thẳng 100% trục đứng cho {straightenedCount} cây cối trong toàn bộ scene!");
        }

        private static void FixRiceFieldTraversal()
        {
            var t3d = GameObject.Find("CaMau_1954_Village/Environment/Terrain_3D");
            if (t3d == null) t3d = GameObject.Find("Terrain_3D");

            if (t3d != null)
            {
                // Dời Barrier_North từ Z = 32 ra Z = 78 để giải phóng toàn bộ cánh đồng lúa (Z=24 đến Z=76)
                var barrierNorth = t3d.transform.Find("Barrier_North");
                if (barrierNorth != null)
                {
                    barrierNorth.position = new Vector3(0f, 3f, 78f);
                    var col = barrierNorth.GetComponent<BoxCollider>();
                    if (col != null)
                    {
                        col.size = new Vector3(120f, 8f, 2f);
                        col.center = Vector3.zero;
                    }
                    EditorUtility.SetDirty(barrierNorth);
                    Debug.Log("[FixTreeHouseRice] Đã dời Barrier_North về Z = 78.0m (rộng 120m). Toàn bộ cánh đồng lúa đã được giải phóng!");
                }

                // Mở rộng Barrier_West và Barrier_East để bao quát toàn bộ chiều sâu làng và cánh đồng (Z=-30 đến Z=80)
                var barrierWest = t3d.transform.Find("Barrier_West");
                if (barrierWest != null)
                {
                    barrierWest.position = new Vector3(-45f, 3f, 25f);
                    var col = barrierWest.GetComponent<BoxCollider>();
                    if (col != null)
                    {
                        col.size = new Vector3(2f, 8f, 110f);
                        col.center = Vector3.zero;
                    }
                    EditorUtility.SetDirty(barrierWest);
                }

                var barrierEast = t3d.transform.Find("Barrier_East");
                if (barrierEast != null)
                {
                    barrierEast.position = new Vector3(45f, 3f, 25f);
                    var col = barrierEast.GetComponent<BoxCollider>();
                    if (col != null)
                    {
                        col.size = new Vector3(2f, 8f, 110f);
                        col.center = Vector3.zero;
                    }
                    EditorUtility.SetDirty(barrierEast);
                }
            }

            // Tạo lối đi kết nối từ sau sân Nhà An (Z=20) dẫn thẳng vào bờ ruộng chính (Z=24..74)
            var pathsRoot = GameObject.Find("Paths_3D_Network")?.transform;
            if (pathsRoot != null)
            {
                var pathAnToField = pathsRoot.Find("Path_NhaAn_To_RiceField_Walk");
                if (pathAnToField == null)
                {
                    var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.name = "Path_NhaAn_To_RiceField_Walk";
                    go.transform.SetParent(pathsRoot, false);
                    pathAnToField = go.transform;
                }
                pathAnToField.position = new Vector3(0.50f, 0.02f, 22.50f);
                pathAnToField.localScale = new Vector3(3.00f, 0.04f, 4.50f);
                pathAnToField.rotation = Quaternion.identity;

                var mr = pathAnToField.GetComponent<MeshRenderer>();
                var matTrail = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Paths/M_Earthen_Trail.mat");
                if (mr != null && matTrail != null) mr.sharedMaterial = matTrail;

                var col = pathAnToField.GetComponent<BoxCollider>();
                if (col != null)
                {
                    col.size = Vector3.one;
                    col.center = Vector3.zero;
                }
                EditorUtility.SetDirty(pathAnToField);
            }

            Debug.Log("[FixTreeHouseRice] Đã tạo đường kết nối mượt mà từ sân Nhà An dẫn thẳng vào toàn bộ cánh đồng lúa!");
        }
    }
}
