#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace HoaVaMau.EditorTools
{
    /// <summary>
    /// Utility dựng vách núi đá bao bọc làng (Mountain Amphitheater) theo ảnh tham chiếu của người dùng.
    /// - Chỉ dựng THÊM núi đá ở 3 góc mé ngoài (Tây, Đông, Bắc), KHÔNG tràn vào làng hay cánh đồng.
    /// - Giữ khoảng đệm rộng rãi (buffer zone > 25m) giữa chân núi và khu dân cư/ruộng lúa.
    /// - Mép bờ biển chỉ đặt đá ở 2 góc xa (X <= -52, X >= 52), để thoáng toàn bộ bãi cát và bến tàu.
    /// </summary>
    public static class BuildMountainAmphitheaterRocks
    {
        [MenuItem("Hoa va Mau/Environment/Build Mountain Amphitheater Rocks", false, 20)]
        public static void BuildAll()
        {
            Debug.Log("<color=#3b82f6>[MountainRocks]</color> Bắt đầu dựng núi đá ở 3 góc mé ngoài (không tràn vào làng)...");

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

            Transform westParent    = new GameObject("West_Outer_Mountains").transform;
            Transform eastParent    = new GameObject("East_Outer_Mountains").transform;
            Transform northParent   = new GameObject("North_Backdrop_Mountains").transform;
            Transform coastalParent = new GameObject("Coastal_Corner_Rocks").transform;

            westParent.SetParent(rootObj.transform, false);
            eastParent.SetParent(rootObj.transform, false);
            northParent.SetParent(rootObj.transform, false);
            coastalParent.SetParent(rootObj.transform, false);

            int totalCreated = 0;

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
            // 1. HAI GÓC MÉ BIỂN XA (Extreme Outer Coastal Beach Corners: X <= -52 & X >= 52)
            // Không xâm lấn bãi cát trung tâm (X: -45 -> 45) hay bến tàu
            // -------------------------------------------------------------------------------------------------
            // Góc biển Tây xa
            spawnRock(coastalParent, "CoastRock_West_Corner_01", 0, new Vector3(-54f, 1.0f, -12f), new Vector3(12f, 8.5f, 10f), new Vector3(10f, 40f, -5f));
            spawnRock(coastalParent, "CoastRock_West_Corner_02", 1, new Vector3(-62f, 2.5f, -18f), new Vector3(15f, 11f, 12f),  new Vector3(-8f, 115f, 8f));
            spawnRock(coastalParent, "CoastRock_West_Water_01",  2, new Vector3(-58f, -0.4f, -28f), new Vector3(11f, 6.0f, 9.5f), new Vector3(12f, 75f, -6f));
            spawnRock(coastalParent, "CoastRock_West_Water_02",  0, new Vector3(-70f, -0.2f, -32f), new Vector3(16f, 9.0f, 13f),  new Vector3(5f, 150f, 10f));

            // Góc biển Đông xa
            spawnRock(coastalParent, "CoastRock_East_Corner_01", 0, new Vector3(54f, 1.0f, -12f),  new Vector3(12f, 8.5f, 10f), new Vector3(10f, -40f, 5f));
            spawnRock(coastalParent, "CoastRock_East_Corner_02", 1, new Vector3(62f, 2.5f, -18f),  new Vector3(15f, 11f, 12f),  new Vector3(-8f, -115f, -8f));
            spawnRock(coastalParent, "CoastRock_East_Water_01",  2, new Vector3(58f, -0.4f, -28f),  new Vector3(11f, 6.0f, 9.5f), new Vector3(12f, -75f, 6f));
            spawnRock(coastalParent, "CoastRock_East_Water_02",  0, new Vector3(70f, -0.2f, -32f),  new Vector3(16f, 9.0f, 13f),  new Vector3(5f, -150f, -10f));

            // -------------------------------------------------------------------------------------------------
            // 2. GÓC MÉ NGOÀI PHÍA TÂY (West Outer Flank Mountain Wall: X <= -70m, Z: 0 -> 195m)
            // Đặt hoàn toàn ở rìa mé ngoài, cách xa ranh giới làng và ruộng lúa > 25m
            // -------------------------------------------------------------------------------------------------
            // Đoạn mé ngoài giáp Làng (Z: 0 -> 60)
            spawnRock(westParent, "WestOuter_Ridge_01", 0, new Vector3(-72f, 7.0f, 6f),   new Vector3(20f, 18f, 18f), new Vector3(12f, 35f, -6f));
            spawnRock(westParent, "WestOuter_Peak_01",  1, new Vector3(-88f, 13f, 12f),  new Vector3(26f, 26f, 24f), new Vector3(-10f, 105f, 10f));
            spawnRock(westParent, "WestOuter_Deep_01",  0, new Vector3(-102f, 18f, 18f), new Vector3(30f, 32f, 28f), new Vector3(15f, 155f, -12f));

            spawnRock(westParent, "WestOuter_Ridge_02", 2, new Vector3(-74f, 8.5f, 32f),  new Vector3(21f, 20f, 19f), new Vector3(-8f, 65f, 12f));
            spawnRock(westParent, "WestOuter_Peak_02",  0, new Vector3(-90f, 15f, 38f),  new Vector3(28f, 28f, 25f), new Vector3(14f, 135f, -8f));
            spawnRock(westParent, "WestOuter_Deep_02",  1, new Vector3(-104f, 20f, 44f), new Vector3(32f, 34f, 28f), new Vector3(-12f, 195f, 10f));

            spawnRock(westParent, "WestOuter_Ridge_03", 1, new Vector3(-75f, 9.0f, 58f),  new Vector3(22f, 22f, 20f), new Vector3(10f, 45f, -10f));
            spawnRock(westParent, "WestOuter_Peak_03",  2, new Vector3(-92f, 16f, 64f),  new Vector3(29f, 30f, 26f), new Vector3(-15f, 120f, 8f));
            spawnRock(westParent, "WestOuter_Deep_03",  0, new Vector3(-106f, 22f, 70f), new Vector3(34f, 36f, 30f), new Vector3(8f, 175f, -14f));

            // Đoạn mé ngoài giáp Ruộng lúa (Z: 60 -> 185)
            spawnRock(westParent, "WestOuter_Ridge_04", 0, new Vector3(-76f, 10f, 84f),   new Vector3(23f, 23f, 21f), new Vector3(-6f, 85f, 12f));
            spawnRock(westParent, "WestOuter_Peak_04",  1, new Vector3(-93f, 17f, 90f),   new Vector3(30f, 32f, 27f), new Vector3(16f, 145f, -6f));
            spawnRock(westParent, "WestOuter_Deep_04",  2, new Vector3(-108f, 24f, 96f),  new Vector3(34f, 38f, 30f), new Vector3(-10f, 215f, 8f));

            spawnRock(westParent, "WestOuter_Ridge_05", 2, new Vector3(-77f, 11f, 110f),  new Vector3(24f, 25f, 22f), new Vector3(12f, 55f, -8f));
            spawnRock(westParent, "WestOuter_Peak_05",  0, new Vector3(-95f, 18f, 116f),  new Vector3(31f, 34f, 28f), new Vector3(-14f, 130f, 12f));
            spawnRock(westParent, "WestOuter_Deep_05",  1, new Vector3(-110f, 25f, 122f), new Vector3(35f, 40f, 32f), new Vector3(10f, 190f, -10f));

            spawnRock(westParent, "WestOuter_Ridge_06", 1, new Vector3(-78f, 12f, 136f),  new Vector3(25f, 26f, 23f), new Vector3(-8f, 95f, 10f));
            spawnRock(westParent, "WestOuter_Peak_06",  2, new Vector3(-96f, 19f, 142f),  new Vector3(32f, 35f, 29f), new Vector3(15f, 165f, -12f));
            spawnRock(westParent, "WestOuter_Deep_06",  0, new Vector3(-112f, 26f, 148f), new Vector3(36f, 42f, 32f), new Vector3(-12f, 75f, 8f));

            spawnRock(westParent, "WestOuter_Ridge_07", 0, new Vector3(-79f, 13f, 162f),  new Vector3(26f, 27f, 24f), new Vector3(14f, 40f, -15f));
            spawnRock(westParent, "WestOuter_Peak_07",  1, new Vector3(-97f, 20f, 168f),  new Vector3(33f, 36f, 30f), new Vector3(-10f, 125f, 10f));
            spawnRock(westParent, "WestOuter_Deep_07",  2, new Vector3(-114f, 28f, 174f), new Vector3(38f, 44f, 34f), new Vector3(8f, 195f, -8f));

            spawnRock(westParent, "WestOuter_Ridge_08", 2, new Vector3(-80f, 14f, 188f),  new Vector3(27f, 28f, 25f), new Vector3(-6f, 80f, 14f));
            spawnRock(westParent, "WestOuter_Peak_08",  0, new Vector3(-98f, 22f, 194f),  new Vector3(34f, 38f, 30f), new Vector3(16f, 150f, -10f));

            // -------------------------------------------------------------------------------------------------
            // 3. GÓC MÉ NGOÀI PHÍA ĐÔNG (East Outer Flank Mountain Wall: X >= 70m, Z: 0 -> 195m)
            // Đặt hoàn toàn ở rìa mé ngoài, cách xa ranh giới làng và ruộng lúa > 25m
            // -------------------------------------------------------------------------------------------------
            // Đoạn mé ngoài giáp Làng (Z: 0 -> 60)
            spawnRock(eastParent, "EastOuter_Ridge_01", 0, new Vector3(72f, 7.0f, 6f),   new Vector3(20f, 18f, 18f), new Vector3(12f, -35f, 6f));
            spawnRock(eastParent, "EastOuter_Peak_01",  1, new Vector3(88f, 13f, 12f),  new Vector3(26f, 26f, 24f), new Vector3(-10f, -105f, -10f));
            spawnRock(eastParent, "EastOuter_Deep_01",  0, new Vector3(102f, 18f, 18f), new Vector3(30f, 32f, 28f), new Vector3(15f, -155f, 12f));

            spawnRock(eastParent, "EastOuter_Ridge_02", 2, new Vector3(74f, 8.5f, 32f),  new Vector3(21f, 20f, 19f), new Vector3(-8f, -65f, -12f));
            spawnRock(eastParent, "EastOuter_Peak_02",  0, new Vector3(90f, 15f, 38f),  new Vector3(28f, 28f, 25f), new Vector3(14f, -135f, 8f));
            spawnRock(eastParent, "EastOuter_Deep_02",  1, new Vector3(104f, 20f, 44f), new Vector3(32f, 34f, 28f), new Vector3(-12f, -195f, -10f));

            spawnRock(eastParent, "EastOuter_Ridge_03", 1, new Vector3(75f, 9.0f, 58f),  new Vector3(22f, 22f, 20f), new Vector3(10f, -45f, 10f));
            spawnRock(eastParent, "EastOuter_Peak_03",  2, new Vector3(92f, 16f, 64f),  new Vector3(29f, 30f, 26f), new Vector3(-15f, -120f, -8f));
            spawnRock(eastParent, "EastOuter_Deep_03",  0, new Vector3(106f, 22f, 70f), new Vector3(34f, 36f, 30f), new Vector3(8f, -175f, 14f));

            // Đoạn mé ngoài giáp Ruộng lúa (Z: 60 -> 185)
            spawnRock(eastParent, "EastOuter_Ridge_04", 0, new Vector3(76f, 10f, 84f),   new Vector3(23f, 23f, 21f), new Vector3(-6f, -85f, -12f));
            spawnRock(eastParent, "EastOuter_Peak_04",  1, new Vector3(93f, 17f, 90f),   new Vector3(30f, 32f, 27f), new Vector3(16f, -145f, 6f));
            spawnRock(eastParent, "EastOuter_Deep_04",  2, new Vector3(108f, 24f, 96f),  new Vector3(34f, 38f, 30f), new Vector3(-10f, -215f, -8f));

            spawnRock(eastParent, "EastOuter_Ridge_05", 2, new Vector3(77f, 11f, 110f),  new Vector3(24f, 25f, 22f), new Vector3(12f, -55f, 8f));
            spawnRock(eastParent, "EastOuter_Peak_05",  0, new Vector3(95f, 18f, 116f),  new Vector3(31f, 34f, 28f), new Vector3(-14f, -130f, -12f));
            spawnRock(eastParent, "EastOuter_Deep_05",  1, new Vector3(110f, 25f, 122f), new Vector3(35f, 40f, 32f), new Vector3(10f, -190f, 10f));

            spawnRock(eastParent, "EastOuter_Ridge_06", 1, new Vector3(78f, 12f, 136f),  new Vector3(25f, 26f, 23f), new Vector3(-8f, -95f, -10f));
            spawnRock(eastParent, "EastOuter_Peak_06",  2, new Vector3(96f, 19f, 142f),  new Vector3(32f, 35f, 29f), new Vector3(15f, -165f, 12f));
            spawnRock(eastParent, "EastOuter_Deep_06",  0, new Vector3(112f, 26f, 148f), new Vector3(36f, 42f, 32f), new Vector3(-12f, -75f, -8f));

            spawnRock(eastParent, "EastOuter_Ridge_07", 0, new Vector3(79f, 13f, 162f),  new Vector3(26f, 27f, 24f), new Vector3(14f, -40f, 15f));
            spawnRock(eastParent, "EastOuter_Peak_07",  1, new Vector3(97f, 20f, 168f),  new Vector3(33f, 36f, 30f), new Vector3(-10f, -125f, -10f));
            spawnRock(eastParent, "EastOuter_Deep_07",  2, new Vector3(114f, 28f, 174f), new Vector3(38f, 44f, 34f), new Vector3(8f, -195f, 8f));

            spawnRock(eastParent, "EastOuter_Ridge_08", 2, new Vector3(80f, 14f, 188f),  new Vector3(27f, 28f, 25f), new Vector3(-6f, -80f, -14f));
            spawnRock(eastParent, "EastOuter_Peak_08",  0, new Vector3(98f, 22f, 194f),  new Vector3(34f, 38f, 30f), new Vector3(16f, -150f, 10f));

            // -------------------------------------------------------------------------------------------------
            // 4. GÓC MÉ NGOÀI PHÍA BẮC (North Backdrop Mountain Peaks: Z >= 202m)
            // Đặt xa phía sau Cổng Bắc (Z = 184m), hở lối đi giữa (X: [-10, 10])
            // -------------------------------------------------------------------------------------------------
            // Đỉnh góc Tây-Bắc xa
            spawnRock(northParent, "NorthBackdrop_West_Corner_01", 0, new Vector3(-35f, 12f, 205f), new Vector3(26f, 26f, 24f), new Vector3(8f, 35f, -6f));
            spawnRock(northParent, "NorthBackdrop_West_Corner_02", 1, new Vector3(-58f, 18f, 212f), new Vector3(32f, 34f, 28f), new Vector3(-12f, 105f, 10f));
            spawnRock(northParent, "NorthBackdrop_West_Peak_Deep", 2, new Vector3(-85f, 25f, 224f), new Vector3(38f, 42f, 34f), new Vector3(14f, 165f, -8f));

            // Đỉnh góc Đông-Bắc xa
            spawnRock(northParent, "NorthBackdrop_East_Corner_01", 0, new Vector3(35f, 12f, 205f),  new Vector3(26f, 26f, 24f), new Vector3(8f, -35f, 6f));
            spawnRock(northParent, "NorthBackdrop_East_Corner_02", 1, new Vector3(58f, 18f, 212f),  new Vector3(32f, 34f, 28f), new Vector3(-12f, -105f, -10f));
            spawnRock(northParent, "NorthBackdrop_East_Peak_Deep", 2, new Vector3(85f, 25f, 224f),  new Vector3(38f, 42f, 34f), new Vector3(14f, -165f, 8f));

            // Dãy phông nền xa nhất phía sau
            spawnRock(northParent, "NorthBackdrop_Center_West_Far", 0, new Vector3(-20f, 16f, 222f), new Vector3(28f, 32f, 26f), new Vector3(6f, 50f, -8f));
            spawnRock(northParent, "NorthBackdrop_Center_East_Far", 1, new Vector3(20f, 16f, 222f),  new Vector3(28f, 32f, 26f), new Vector3(6f, -50f, 8f));

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log($"<color=#10b981>[MountainRocks]</color> HOÀN TẤT! Đã dựng {totalCreated} khối núi đá ở 3 góc mé ngoài, ranh giới làng và ruộng lúa hoàn toàn thoáng đãng!");
        }
    }
}
#endif
