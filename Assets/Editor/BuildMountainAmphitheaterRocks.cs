#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace HoaVaMau.EditorTools
{
    /// <summary>
    /// Utility dựng vách núi đá bao bọc làng (Mountain Amphitheater) theo ảnh tham chiếu của người dùng.
    /// - Chỉ dựng THÊM núi đá (Additive Only), tuyệt đối không đụng chạm đến bất kỳ vật thể có sẵn nào.
    /// - Phủ từ mép nước biển (coastal sea rocks), dọc sườn Tây và sườn Đông thành vách núi đá sừng sững, 
    ///   và hội tụ về hẻm núi Cổng Bắc tạo thành lòng chảo núi đá tự nhiên hùng vĩ.
    /// </summary>
    public static class BuildMountainAmphitheaterRocks
    {
        [MenuItem("Hoa va Mau/Environment/Build Mountain Amphitheater Rocks", false, 20)]
        public static void BuildAll()
        {
            Debug.Log("<color=#3b82f6>[MountainRocks]</color> Bắt đầu dựng dãy núi đá lòng chảo theo ảnh tham chiếu...");

            // 1. Tải Material và Meshes
            Material matRock = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Rocks/M_Mountain_Crag_Rock.mat");
            if (matRock == null)
            {
                matRock = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Rocks/M_Rock_Medium_02_Platform.mat");
            }

            GameObject fbxLarge = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Optimized/SM_Rock_Large_BlenderOptimized.fbx");
            GameObject fbxMed2  = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Optimized/SM_Rock_Medium_02_BlenderOptimized.fbx");
            GameObject fbxMed1  = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Environment/SM_Rock_Medium_01.fbx");
            GameObject fbxPlat  = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Environment/SM_Rock_Platform_01.fbx");

            Mesh meshLarge = fbxLarge.GetComponentInChildren<MeshFilter>().sharedMesh;
            Mesh meshMed2  = fbxMed2.GetComponentInChildren<MeshFilter>().sharedMesh;
            Mesh meshMed1  = fbxMed1.GetComponentInChildren<MeshFilter>().sharedMesh;
            Mesh meshPlat  = fbxPlat.GetComponentInChildren<MeshFilter>().sharedMesh;

            Mesh[] meshes = new Mesh[] { meshLarge, meshMed2, meshMed1, meshPlat };

            // 2. Tìm hoặc tạo root container riêng biệt
            GameObject env3D = GameObject.Find("Environment_3D");
            if (env3D == null)
            {
                env3D = new GameObject("Environment_3D");
            }

            Transform rootTrans = env3D.transform.Find("Mountain_Rocks_Amphitheater");
            if (rootTrans != null)
            {
                Object.DestroyImmediate(rootTrans.gameObject);
            }

            GameObject rootObj = new GameObject("Mountain_Rocks_Amphitheater");
            rootObj.transform.SetParent(env3D.transform, false);

            Transform westParent   = new GameObject("West_Flank_Mountains").transform;
            Transform eastParent   = new GameObject("East_Flank_Mountains").transform;
            Transform coastalParent = new GameObject("Coastal_Sea_Rocks").transform;
            Transform northParent  = new GameObject("North_Pass_Backdrop_Crags").transform;

            westParent.SetParent(rootObj.transform, false);
            eastParent.SetParent(rootObj.transform, false);
            coastalParent.SetParent(rootObj.transform, false);
            northParent.SetParent(rootObj.transform, false);

            int totalCreated = 0;

            // Helper đặt 1 khối núi đá
            System.Action<Transform, string, int, Vector3, Vector3, Vector3> spawnRock = (parent, name, meshIdx, pos, scale, rot) =>
            {
                GameObject rock = new GameObject(name);
                rock.transform.SetParent(parent, false);
                rock.transform.position = pos;
                rock.transform.localScale = scale;
                rock.transform.eulerAngles = rot;

                var mf = rock.AddComponent<MeshFilter>();
                mf.sharedMesh = meshes[meshIdx % meshes.Length];

                var mr = rock.AddComponent<MeshRenderer>();
                mr.sharedMaterial = matRock;

                totalCreated++;
            };

            // -------------------------------------------------------------------------------------------------
            // A. ĐÁ BỜ BIỂN & MẶT NƯỚC (Coastal Sea Rocks & Beach Outcrops)
            // Tương tự ảnh mẫu: các mỏm đá nhô lên từ nước biển xanh và mép cát hai bên bến tàu
            // -------------------------------------------------------------------------------------------------
            // Sườn bờ biển Tây (Bên trái bến tàu)
            spawnRock(coastalParent, "SeaRock_West_Water_01", 0, new Vector3(-26f, -0.6f, -22f), new Vector3(6f, 3.5f, 5.5f), new Vector3(8f, 35f, -6f));
            spawnRock(coastalParent, "SeaRock_West_Water_02", 1, new Vector3(-34f, -0.4f, -25f), new Vector3(7.5f, 4.5f, 7f), new Vector3(-5f, 110f, 8f));
            spawnRock(coastalParent, "SeaRock_West_Water_03", 2, new Vector3(-42f, -0.2f, -28f), new Vector3(9f, 6.0f, 8.5f), new Vector3(12f, 70f, -4f));
            spawnRock(coastalParent, "SeaRock_West_Shore_01", 3, new Vector3(-24f, 0.2f, -14f), new Vector3(6.5f, 3.0f, 5.5f), new Vector3(0f, 45f, 0f));
            spawnRock(coastalParent, "SeaRock_West_Shore_02", 0, new Vector3(-32f, 0.5f, -12f), new Vector3(8.5f, 5.5f, 7.5f), new Vector3(10f, 135f, -5f));
            spawnRock(coastalParent, "SeaRock_West_Shore_03", 1, new Vector3(-40f, 1.0f, -8f),  new Vector3(11f, 7.5f, 10f), new Vector3(-8f, 205f, 6f));
            spawnRock(coastalParent, "SeaRock_West_Shore_04", 2, new Vector3(-48f, 1.5f, -15f), new Vector3(13f, 9.0f, 11f), new Vector3(15f, 80f, 10f));

            // Sườn bờ biển Đông (Bên phải bến tàu)
            spawnRock(coastalParent, "SeaRock_East_Water_01", 0, new Vector3(26f, -0.6f, -22f),  new Vector3(6f, 3.5f, 5.5f), new Vector3(6f, -35f, 8f));
            spawnRock(coastalParent, "SeaRock_East_Water_02", 1, new Vector3(34f, -0.4f, -25f),  new Vector3(8f, 5.0f, 7.5f), new Vector3(-10f, -115f, -6f));
            spawnRock(coastalParent, "SeaRock_East_Water_03", 2, new Vector3(44f, -0.2f, -27f),  new Vector3(10f, 6.5f, 9f), new Vector3(14f, -65f, 5f));
            spawnRock(coastalParent, "SeaRock_East_Shore_01", 3, new Vector3(24f, 0.2f, -14f),  new Vector3(6f, 3.2f, 5.5f), new Vector3(0f, -40f, 0f));
            spawnRock(coastalParent, "SeaRock_East_Shore_02", 0, new Vector3(33f, 0.5f, -11f),  new Vector3(9f, 6.0f, 8f),   new Vector3(8f, -145f, 7f));
            spawnRock(coastalParent, "SeaRock_East_Shore_03", 1, new Vector3(42f, 1.0f, -6f),   new Vector3(12f, 8.0f, 10f), new Vector3(-5f, -220f, -8f));
            spawnRock(coastalParent, "SeaRock_East_Shore_04", 2, new Vector3(50f, 1.5f, -16f),  new Vector3(14f, 9.5f, 12f), new Vector3(12f, -85f, -10f));

            // -------------------------------------------------------------------------------------------------
            // B. SƯỜN NÚI ĐÁ PHÍA TÂY (West Flank Mountain Wall - Z: 0 -> 190)
            // 2 tầng núi: tầng thấp giáp làng (scale 12-18), tầng đỉnh cao phía sau (scale 18-28, cao 15-32m)
            // -------------------------------------------------------------------------------------------------
            // Đoạn giáp Làng (Z: 0 -> 60)
            spawnRock(westParent, "WestMtn_Village_Inner_01", 0, new Vector3(-50f, 2.0f, 5f),   new Vector3(13f, 9.5f, 12f), new Vector3(10f, 25f, -5f));
            spawnRock(westParent, "WestMtn_Village_Outer_01", 1, new Vector3(-68f, 5.0f, 2f),   new Vector3(18f, 16f, 17f),  new Vector3(-8f, 85f, 12f));
            spawnRock(westParent, "WestMtn_Village_Peak_01",  0, new Vector3(-82f, 10f, 10f),   new Vector3(24f, 24f, 22f),  new Vector3(14f, 140f, -8f));

            spawnRock(westParent, "WestMtn_Village_Inner_02", 2, new Vector3(-52f, 2.5f, 24f),  new Vector3(14f, 11f, 13f),  new Vector3(-5f, 65f, 8f));
            spawnRock(westParent, "WestMtn_Village_Outer_02", 0, new Vector3(-70f, 6.0f, 20f),  new Vector3(20f, 18f, 18f),  new Vector3(12f, 120f, -6f));
            spawnRock(westParent, "WestMtn_Village_Peak_02",  1, new Vector3(-86f, 12f, 28f),   new Vector3(26f, 26f, 24f),  new Vector3(-10f, 195f, 10f));

            spawnRock(westParent, "WestMtn_Village_Inner_03", 3, new Vector3(-54f, 2.5f, 44f),  new Vector3(15f, 12f, 14f),  new Vector3(8f, 40f, -10f));
            spawnRock(westParent, "WestMtn_Village_Outer_03", 2, new Vector3(-72f, 7.0f, 42f),  new Vector3(21f, 19f, 19f),  new Vector3(-12f, 160f, 5f));
            spawnRock(westParent, "WestMtn_Village_Peak_03",  0, new Vector3(-88f, 13f, 48f),   new Vector3(27f, 28f, 25f),  new Vector3(15f, 75f, -12f));

            // Đoạn giáp Cánh đồng lúa mở rộng (Z: 60 -> 185)
            spawnRock(westParent, "WestMtn_Rice_Inner_01", 0, new Vector3(-55f, 2.8f, 66f),   new Vector3(15f, 13f, 14f),  new Vector3(6f, 95f, -8f));
            spawnRock(westParent, "WestMtn_Rice_Outer_01", 1, new Vector3(-73f, 7.5f, 65f),   new Vector3(22f, 21f, 20f),  new Vector3(-14f, 130f, 10f));
            spawnRock(westParent, "WestMtn_Rice_Peak_01",  2, new Vector3(-90f, 14f, 72f),   new Vector3(28f, 30f, 26f),  new Vector3(10f, 210f, -5f));

            spawnRock(westParent, "WestMtn_Rice_Inner_02", 1, new Vector3(-56f, 3.0f, 88f),   new Vector3(16f, 14f, 15f),  new Vector3(-10f, 55f, 12f));
            spawnRock(westParent, "WestMtn_Rice_Outer_02", 0, new Vector3(-75f, 8.5f, 86f),   new Vector3(23f, 23f, 21f),  new Vector3(15f, 165f, -14f));
            spawnRock(westParent, "WestMtn_Rice_Peak_02",  1, new Vector3(-92f, 15f, 94f),   new Vector3(29f, 32f, 27f),  new Vector3(-8f, 85f, 8f));

            spawnRock(westParent, "WestMtn_Rice_Inner_03", 2, new Vector3(-57f, 3.0f, 110f),  new Vector3(16f, 15f, 15f),  new Vector3(8f, 115f, -6f));
            spawnRock(westParent, "WestMtn_Rice_Outer_03", 3, new Vector3(-76f, 9.0f, 112f),  new Vector3(24f, 24f, 22f),  new Vector3(-12f, 185f, 12f));
            spawnRock(westParent, "WestMtn_Rice_Peak_03",  0, new Vector3(-94f, 16f, 118f),  new Vector3(30f, 34f, 28f),  new Vector3(14f, 45f, -10f));

            spawnRock(westParent, "WestMtn_Rice_Inner_04", 0, new Vector3(-58f, 3.2f, 134f),  new Vector3(17f, 15f, 16f),  new Vector3(-6f, 75f, 14f));
            spawnRock(westParent, "WestMtn_Rice_Outer_04", 1, new Vector3(-78f, 9.5f, 136f),  new Vector3(24f, 25f, 22f),  new Vector3(16f, 145f, -8f));
            spawnRock(westParent, "WestMtn_Rice_Peak_04",  2, new Vector3(-95f, 17f, 142f),  new Vector3(31f, 35f, 29f),  new Vector3(-10f, 225f, 6f));

            spawnRock(westParent, "WestMtn_Rice_Inner_05", 1, new Vector3(-58f, 3.2f, 158f),  new Vector3(17f, 16f, 16f),  new Vector3(12f, 105f, -12f));
            spawnRock(westParent, "WestMtn_Rice_Outer_05", 2, new Vector3(-79f, 10f, 160f),   new Vector3(25f, 26f, 23f),  new Vector3(-15f, 175f, 10f));
            spawnRock(westParent, "WestMtn_Rice_Peak_05",  0, new Vector3(-96f, 18f, 166f),  new Vector3(32f, 36f, 30f),  new Vector3(8f, 65f, -6f));

            spawnRock(westParent, "WestMtn_Rice_Inner_06", 2, new Vector3(-56f, 3.5f, 180f),  new Vector3(18f, 17f, 17f),  new Vector3(-8f, 45f, 8f));
            spawnRock(westParent, "WestMtn_Rice_Outer_06", 0, new Vector3(-78f, 11f, 182f),   new Vector3(26f, 27f, 24f),  new Vector3(14f, 135f, -15f));
            spawnRock(westParent, "WestMtn_Rice_Peak_06",  1, new Vector3(-94f, 19f, 188f),  new Vector3(32f, 36f, 30f),  new Vector3(-12f, 205f, 10f));

            // -------------------------------------------------------------------------------------------------
            // C. SƯỜN NÚI ĐÁ PHÍA ĐÔNG (East Flank Mountain Wall - Z: 0 -> 190)
            // Đối xứng sừng sững bên phải khép lại lòng chảo núi đá
            // -------------------------------------------------------------------------------------------------
            // Đoạn giáp Làng (Z: 0 -> 60)
            spawnRock(eastParent, "EastMtn_Village_Inner_01", 0, new Vector3(50f, 2.0f, 5f),   new Vector3(13f, 9.5f, 12f), new Vector3(10f, -25f, 5f));
            spawnRock(eastParent, "EastMtn_Village_Outer_01", 1, new Vector3(68f, 5.0f, 2f),   new Vector3(18f, 16f, 17f),  new Vector3(-8f, -85f, -12f));
            spawnRock(eastParent, "EastMtn_Village_Peak_01",  0, new Vector3(82f, 10f, 10f),   new Vector3(24f, 24f, 22f),  new Vector3(14f, -140f, 8f));

            spawnRock(eastParent, "EastMtn_Village_Inner_02", 2, new Vector3(52f, 2.5f, 24f),  new Vector3(14f, 11f, 13f),  new Vector3(-5f, -65f, -8f));
            spawnRock(eastParent, "EastMtn_Village_Outer_02", 0, new Vector3(70f, 6.0f, 20f),  new Vector3(20f, 18f, 18f),  new Vector3(12f, -120f, 6f));
            spawnRock(eastParent, "EastMtn_Village_Peak_02",  1, new Vector3(86f, 12f, 28f),   new Vector3(26f, 26f, 24f),  new Vector3(-10f, -195f, -10f));

            spawnRock(eastParent, "EastMtn_Village_Inner_03", 3, new Vector3(54f, 2.5f, 44f),  new Vector3(15f, 12f, 14f),  new Vector3(8f, -40f, 10f));
            spawnRock(eastParent, "EastMtn_Village_Outer_03", 2, new Vector3(72f, 7.0f, 42f),  new Vector3(21f, 19f, 19f),  new Vector3(-12f, -160f, -5f));
            spawnRock(eastParent, "EastMtn_Village_Peak_03",  0, new Vector3(88f, 13f, 48f),   new Vector3(27f, 28f, 25f),  new Vector3(15f, -75f, 12f));

            // Đoạn giáp Cánh đồng lúa mở rộng (Z: 60 -> 185)
            spawnRock(eastParent, "EastMtn_Rice_Inner_01", 0, new Vector3(55f, 2.8f, 66f),   new Vector3(15f, 13f, 14f),  new Vector3(6f, -95f, 8f));
            spawnRock(eastParent, "EastMtn_Rice_Outer_01", 1, new Vector3(73f, 7.5f, 65f),   new Vector3(22f, 21f, 20f),  new Vector3(-14f, -130f, -10f));
            spawnRock(eastParent, "EastMtn_Rice_Peak_01",  2, new Vector3(90f, 14f, 72f),   new Vector3(28f, 30f, 26f),  new Vector3(10f, -210f, 5f));

            spawnRock(eastParent, "EastMtn_Rice_Inner_02", 1, new Vector3(56f, 3.0f, 88f),   new Vector3(16f, 14f, 15f),  new Vector3(-10f, -55f, -12f));
            spawnRock(eastParent, "EastMtn_Rice_Outer_02", 0, new Vector3(75f, 8.5f, 86f),   new Vector3(23f, 23f, 21f),  new Vector3(15f, -165f, 14f));
            spawnRock(eastParent, "EastMtn_Rice_Peak_02",  1, new Vector3(92f, 15f, 94f),   new Vector3(29f, 32f, 27f),  new Vector3(-8f, -85f, -8f));

            spawnRock(eastParent, "EastMtn_Rice_Inner_03", 2, new Vector3(57f, 3.0f, 110f),  new Vector3(16f, 15f, 15f),  new Vector3(8f, -115f, 6f));
            spawnRock(eastParent, "EastMtn_Rice_Outer_03", 3, new Vector3(76f, 9.0f, 112f),  new Vector3(24f, 24f, 22f),  new Vector3(-12f, -185f, -12f));
            spawnRock(eastParent, "EastMtn_Rice_Peak_03",  0, new Vector3(94f, 16f, 118f),  new Vector3(30f, 34f, 28f),  new Vector3(14f, -45f, 10f));

            spawnRock(eastParent, "EastMtn_Rice_Inner_04", 0, new Vector3(58f, 3.2f, 134f),  new Vector3(17f, 15f, 16f),  new Vector3(-6f, -75f, -14f));
            spawnRock(eastParent, "EastMtn_Rice_Outer_04", 1, new Vector3(78f, 9.5f, 136f),  new Vector3(24f, 25f, 22f),  new Vector3(16f, -145f, 8f));
            spawnRock(eastParent, "EastMtn_Rice_Peak_04",  2, new Vector3(95f, 17f, 142f),  new Vector3(31f, 35f, 29f),  new Vector3(-10f, -225f, -6f));

            spawnRock(eastParent, "EastMtn_Rice_Inner_05", 1, new Vector3(58f, 3.2f, 158f),  new Vector3(17f, 16f, 16f),  new Vector3(12f, -105f, 12f));
            spawnRock(eastParent, "EastMtn_Rice_Outer_05", 2, new Vector3(79f, 10f, 160f),   new Vector3(25f, 26f, 23f),  new Vector3(-15f, -175f, -10f));
            spawnRock(eastParent, "EastMtn_Rice_Peak_05",  0, new Vector3(96f, 18f, 166f),  new Vector3(32f, 36f, 30f),  new Vector3(8f, -65f, 6f));

            spawnRock(eastParent, "EastMtn_Rice_Inner_06", 2, new Vector3(56f, 3.5f, 180f),  new Vector3(18f, 17f, 17f),  new Vector3(-8f, -45f, -8f));
            spawnRock(eastParent, "EastMtn_Rice_Outer_06", 0, new Vector3(78f, 11f, 182f),   new Vector3(26f, 27f, 24f),  new Vector3(14f, -135f, 15f));
            spawnRock(eastParent, "EastMtn_Rice_Peak_06",  1, new Vector3(94f, 19f, 188f),  new Vector3(32f, 36f, 30f),  new Vector3(-12f, -205f, -10f));

            // -------------------------------------------------------------------------------------------------
            // D. VÁCH NÚI HẺM CỔNG BẮC (North Pass Backdrop Crags - Z: 188 -> 225)
            // Khung cảnh núi đá hùng vĩ bao bọc phía sau cổng làng
            // -------------------------------------------------------------------------------------------------
            // Cánh gà Tây Cổng Bắc
            spawnRock(northParent, "NorthPass_West_Bastion_01", 0, new Vector3(-18f, 3.5f, 192f), new Vector3(16f, 14f, 15f), new Vector3(8f, 30f, -5f));
            spawnRock(northParent, "NorthPass_West_Bastion_02", 1, new Vector3(-32f, 7.0f, 196f), new Vector3(22f, 20f, 19f), new Vector3(-10f, 95f, 12f));
            spawnRock(northParent, "NorthPass_West_Peak_01",    2, new Vector3(-50f, 12f, 204f),  new Vector3(28f, 28f, 25f), new Vector3(14f, 160f, -8f));

            // Cánh gà Đông Cổng Bắc
            spawnRock(northParent, "NorthPass_East_Bastion_01", 0, new Vector3(18f, 3.5f, 192f),  new Vector3(16f, 14f, 15f), new Vector3(8f, -30f, 5f));
            spawnRock(northParent, "NorthPass_East_Bastion_02", 1, new Vector3(32f, 7.0f, 196f),  new Vector3(22f, 20f, 19f), new Vector3(-10f, -95f, -12f));
            spawnRock(northParent, "NorthPass_East_Peak_01",    2, new Vector3(50f, 12f, 204f),   new Vector3(28f, 28f, 25f), new Vector3(14f, -160f, 8f));

            // Dãy núi phông nền xa phía sau Cổng Bắc (Z: 210 -> 228, hở lối đi giữa X: [-7, 7])
            spawnRock(northParent, "NorthPass_Backdrop_West_Deep", 0, new Vector3(-24f, 14f, 215f), new Vector3(28f, 30f, 26f), new Vector3(6f, 65f, -10f));
            spawnRock(northParent, "NorthPass_Backdrop_East_Deep", 1, new Vector3(24f, 14f, 215f),  new Vector3(28f, 30f, 26f), new Vector3(6f, -65f, 10f));
            spawnRock(northParent, "NorthPass_Backdrop_FarWest",   2, new Vector3(-65f, 18f, 222f), new Vector3(34f, 36f, 30f), new Vector3(-12f, 125f, 14f));
            spawnRock(northParent, "NorthPass_Backdrop_FarEast",   0, new Vector3(65f, 18f, 222f),  new Vector3(34f, 36f, 30f), new Vector3(-12f, -125f, -14f));

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log($"<color=#10b981>[MountainRocks]</color> HOÀN TẤT! Đã dựng thành công {totalCreated} khối núi đá lòng chảo bao quanh làng và bờ biển!");
        }
    }
}
#endif
