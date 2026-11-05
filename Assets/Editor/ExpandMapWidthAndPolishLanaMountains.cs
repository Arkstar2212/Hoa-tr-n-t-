#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace HoaVaMau.EditorTools
{
    /// <summary>
    /// Utility mở rộng bề ngang map, tối ưu hiệu năng (giảm lag), chuyển màu đá sang style Planet of Lana, 
    /// và trồng cây dọc theo sườn núi đá.
    /// </summary>
    public static class ExpandMapWidthAndPolishLanaMountains
    {
        [MenuItem("Hoa va Mau/Environment/Expand Map Width & Polish Lana Mountains", false, 21)]
        public static void Execute()
        {
            Debug.Log("<color=#3b82f6>[MapExpansion]</color> Bắt đầu mở rộng bề ngang, tối ưu hiệu năng và phủ cây núi đá...");

            // -------------------------------------------------------------------------------------------------
            // 1. MỞ RỘNG BỀ NGANG CỦA MAP (Từ 152m lên 240m)
            // -------------------------------------------------------------------------------------------------
            var groundObj = GameObject.Find("Terrain_Unified_Ground_Base");
            if (groundObj != null)
            {
                // Mở rộng X từ 152m -> 240m để vừa vặn dãy núi đá 2 bên
                groundObj.transform.localScale = new Vector3(240f, 0.5f, 216f);
                EditorUtility.SetDirty(groundObj);
            }

            // Tinh chỉnh Tiling cho Material mặt đất để giữ nguyên mật độ texture
            var matGround = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/M_Ground_Grass_VillageSoil.mat");
            if (matGround != null)
            {
                // 240m / 4m = 60 tile
                matGround.SetTextureScale("_BaseMap", new Vector2(60f, 54f));
                EditorUtility.SetDirty(matGround);
            }

            // Mở rộng bãi cát ven biển tương ứng
            var beachObj = GameObject.Find("Terrain_Beach_Slope");
            if (beachObj != null)
            {
                beachObj.transform.localScale = new Vector3(1.46f, 1.0f, 1.0f);
                EditorUtility.SetDirty(beachObj);
            }

            // -------------------------------------------------------------------------------------------------
            // 2. CHỈNH MÀU NÚI ĐÁ THEO STYLE PLANET OF LANA & TỐI ƯU HIỆU NĂNG (GPU Instancing)
            // -------------------------------------------------------------------------------------------------
            var matRock = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Rocks/M_Mountain_Crag_Rock.mat");
            if (matRock != null)
            {
                var texRock = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Meshy_AI_Moss_Crowned_Stone_1007092109_texture.png");
                var normRock = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Meshy_AI_Moss_Crowned_Stone_1007092109_texture_normal.png");

                matRock.SetTexture("_BaseMap", texRock);
                matRock.SetTexture("_BumpMap", normRock);
                matRock.SetFloat("_BumpScale", 0.55f); // Làm mềm normal map cho nét vẽ tay cel-shaded
                // Sắc xanh rêu đá karst đặc trưng Planet of Lana (Soft Mossy Karst Slate)
                matRock.SetColor("_BaseColor", new Color(0.56f, 0.66f, 0.56f, 1.0f));
                matRock.SetFloat("_Smoothness", 0.02f);
                matRock.SetFloat("_Metallic", 0.0f);
                matRock.enableInstancing = true; // Bật GPU Instancing để triệt tiêu draw call, chống lag tối đa!

                EditorUtility.SetDirty(matRock);
            }

            // -------------------------------------------------------------------------------------------------
            // 3. TẢI MESH & PREFABS CÂY (PLANET OF LANA BILLBOARD FOLIAGE)
            // -------------------------------------------------------------------------------------------------
            GameObject fbxLarge = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Optimized/SM_Rock_Large_BlenderOptimized.fbx");
            GameObject fbxMed2  = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Optimized/SM_Rock_Medium_02_BlenderOptimized.fbx");
            GameObject fbxMed1  = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Environment/SM_Rock_Medium_01.fbx");
            GameObject fbxPlat  = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Environment/SM_Rock_Platform_01.fbx");

            Mesh meshLarge = fbxLarge.GetComponentInChildren<MeshFilter>().sharedMesh;
            Mesh meshMed2  = fbxMed2.GetComponentInChildren<MeshFilter>().sharedMesh;
            Mesh meshMed1  = fbxMed1.GetComponentInChildren<MeshFilter>().sharedMesh;
            Mesh meshPlat  = fbxPlat.GetComponentInChildren<MeshFilter>().sharedMesh;

            Mesh[] rockMeshes = new Mesh[] { meshLarge, meshMed2, meshMed1, meshPlat };

            // Prefabs cây Nam Bộ / Planet of Lana
            GameObject prefCoconut = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_06_Coconut.prefab");
            GameObject prefBanana  = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_05_Banana.prefab");
            GameObject prefBamboo  = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_07_BambooGrove.prefab");
            GameObject prefCanopy  = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_01_LargeCanopy.prefab");
            GameObject prefMedium  = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_02_Medium.prefab");

            // -------------------------------------------------------------------------------------------------
            // 4. QUẢN LÝ CONTAINER HIERARCHY
            // -------------------------------------------------------------------------------------------------
            GameObject env3D = GameObject.Find("Environment_3D");
            if (env3D == null) env3D = new GameObject("Environment_3D");

            Transform rootTrans = env3D.transform.Find("Mountain_Rocks_Amphitheater");
            if (rootTrans != null)
            {
                Object.DestroyImmediate(rootTrans.gameObject);
            }

            GameObject rootObj = new GameObject("Mountain_Rocks_Amphitheater");
            rootObj.transform.SetParent(env3D.transform, false);

            Transform rockParent = new GameObject("Mountain_Crags").transform;
            Transform treeParent = new GameObject("Mountain_Foliage_Trees").transform;
            rockParent.SetParent(rootObj.transform, false);
            treeParent.SetParent(rootObj.transform, false);

            int rocksSpawned = 0;
            int treesSpawned = 0;

            // Spawn Rock (Không Collider, Tối ưu shadow cho phông nền xa)
            System.Action<string, int, Vector3, Vector3, Vector3> spawnRock = (name, mIdx, pos, scale, rot) =>
            {
                GameObject rock = new GameObject(name);
                rock.transform.SetParent(rockParent, false);
                rock.transform.position = pos;
                rock.transform.localScale = scale;
                rock.transform.eulerAngles = rot;

                var mf = rock.AddComponent<MeshFilter>();
                mf.sharedMesh = rockMeshes[mIdx % rockMeshes.Length];

                var mr = rock.AddComponent<MeshRenderer>();
                mr.sharedMaterial = matRock;

                // Tối ưu hiệu năng: núi ở xa (> 200m hoặc ngoài biên > 95m) tắt đổ bóng để giảm tải GPU
                if (pos.z > 210f || Mathf.Abs(pos.x) > 100f)
                {
                    mr.shadowCastingMode = ShadowCastingMode.Off;
                }
                else
                {
                    mr.shadowCastingMode = ShadowCastingMode.On;
                }
                mr.receiveShadows = true;

                rocksSpawned++;
            };

            // Spawn Tree (Billboard/Low-Poly cây bám sườn núi)
            System.Action<GameObject, string, Vector3, Vector3, Vector3> spawnTree = (prefab, name, pos, scale, rot) =>
            {
                if (prefab == null) return;
                GameObject t = Object.Instantiate(prefab);
                t.name = name;
                t.transform.SetParent(treeParent, false);
                t.transform.position = pos;
                t.transform.localScale = scale;
                t.transform.eulerAngles = rot;
                treesSpawned++;
            };

            // -------------------------------------------------------------------------------------------------
            // A. DÃY NÚI ĐÁ VÀ CÂY SƯỜN TÂY (West Mountain Flank & Trees: X <= -72m)
            // -------------------------------------------------------------------------------------------------
            // 1. Dãy núi đá sườn Tây
            spawnRock("WestMtn_Rock_01", 0, new Vector3(-74f, 7.5f, 8f),   new Vector3(20f, 18f, 18f), new Vector3(12f, 35f, -6f));
            spawnRock("WestMtn_Rock_02", 1, new Vector3(-92f, 14f, 16f),  new Vector3(28f, 28f, 26f), new Vector3(-10f, 105f, 10f));
            spawnRock("WestMtn_Rock_03", 2, new Vector3(-76f, 8.5f, 36f),  new Vector3(22f, 20f, 20f), new Vector3(-8f, 65f, 12f));
            spawnRock("WestMtn_Rock_04", 0, new Vector3(-95f, 16f, 44f),  new Vector3(30f, 30f, 26f), new Vector3(14f, 135f, -8f));
            spawnRock("WestMtn_Rock_05", 1, new Vector3(-78f, 9.5f, 64f),  new Vector3(23f, 22f, 21f), new Vector3(10f, 45f, -10f));
            spawnRock("WestMtn_Rock_06", 2, new Vector3(-97f, 17f, 72f),  new Vector3(31f, 32f, 28f), new Vector3(-15f, 120f, 8f));
            spawnRock("WestMtn_Rock_07", 0, new Vector3(-79f, 10.5f, 92f), new Vector3(24f, 24f, 22f), new Vector3(-6f, 85f, 12f));
            spawnRock("WestMtn_Rock_08", 1, new Vector3(-98f, 18.5f, 100f), new Vector3(32f, 34f, 28f), new Vector3(16f, 145f, -6f));
            spawnRock("WestMtn_Rock_09", 2, new Vector3(-80f, 11.5f, 120f), new Vector3(25f, 25f, 23f), new Vector3(12f, 55f, -8f));
            spawnRock("WestMtn_Rock_10", 0, new Vector3(-100f, 19.5f, 128f), new Vector3(33f, 36f, 30f), new Vector3(-14f, 130f, 12f));
            spawnRock("WestMtn_Rock_11", 1, new Vector3(-81f, 12.5f, 148f), new Vector3(26f, 26f, 24f), new Vector3(-8f, 95f, 10f));
            spawnRock("WestMtn_Rock_12", 2, new Vector3(-102f, 21f, 156f), new Vector3(34f, 38f, 30f), new Vector3(15f, 165f, -12f));
            spawnRock("WestMtn_Rock_13", 0, new Vector3(-82f, 13.5f, 176f), new Vector3(27f, 27f, 25f), new Vector3(14f, 40f, -15f));
            spawnRock("WestMtn_Rock_14", 1, new Vector3(-104f, 23f, 184f), new Vector3(36f, 40f, 32f), new Vector3(-10f, 125f, 10f));

            // 2. Cây phủ bám sườn núi Tây (Tạo độ sinh động tự nhiên như ảnh tham chiếu)
            spawnTree(prefCoconut, "WestTree_Palm_01", new Vector3(-66f, 1.0f, 18f),  new Vector3(1.2f, 1.2f, 1.2f), new Vector3(0f, 35f, 0f));
            spawnTree(prefBanana,  "WestTree_Banana_01", new Vector3(-68f, 2.0f, 28f), new Vector3(1.3f, 1.3f, 1.3f), new Vector3(0f, 80f, 0f));
            spawnTree(prefCanopy,  "WestTree_Canopy_01", new Vector3(-72f, 4.0f, 48f), new Vector3(1.5f, 1.5f, 1.5f), new Vector3(0f, 120f, 0f));
            spawnTree(prefCoconut, "WestTree_Palm_02", new Vector3(-67f, 1.2f, 60f),  new Vector3(1.3f, 1.3f, 1.3f), new Vector3(0f, -40f, 0f));
            spawnTree(prefBamboo,  "WestTree_Bamboo_01", new Vector3(-70f, 3.0f, 78f), new Vector3(1.4f, 1.4f, 1.4f), new Vector3(0f, 65f, 0f));
            spawnTree(prefMedium,  "WestTree_Med_01",   new Vector3(-74f, 5.0f, 96f), new Vector3(1.4f, 1.4f, 1.4f), new Vector3(0f, 140f, 0f));
            spawnTree(prefCoconut, "WestTree_Palm_03", new Vector3(-68f, 1.5f, 115f), new Vector3(1.2f, 1.2f, 1.2f), new Vector3(0f, 15f, 0f));
            spawnTree(prefBanana,  "WestTree_Banana_02", new Vector3(-71f, 3.5f, 132f), new Vector3(1.3f, 1.3f, 1.3f), new Vector3(0f, 95f, 0f));
            spawnTree(prefCanopy,  "WestTree_Canopy_02", new Vector3(-75f, 6.0f, 152f), new Vector3(1.6f, 1.6f, 1.6f), new Vector3(0f, -80f, 0f));
            spawnTree(prefCoconut, "WestTree_Palm_04", new Vector3(-69f, 2.0f, 170f), new Vector3(1.3f, 1.3f, 1.3f), new Vector3(0f, 45f, 0f));
            spawnTree(prefBamboo,  "WestTree_Bamboo_02", new Vector3(-73f, 5.0f, 185f), new Vector3(1.5f, 1.5f, 1.5f), new Vector3(0f, 110f, 0f));

            // -------------------------------------------------------------------------------------------------
            // B. DÃY NÚI ĐÁ VÀ CÂY SƯỜN ĐÔNG (East Mountain Flank & Trees: X >= 72m)
            // -------------------------------------------------------------------------------------------------
            // 1. Dãy núi đá sườn Đông
            spawnRock("EastMtn_Rock_01", 0, new Vector3(74f, 7.5f, 8f),   new Vector3(20f, 18f, 18f), new Vector3(12f, -35f, 6f));
            spawnRock("EastMtn_Rock_02", 1, new Vector3(92f, 14f, 16f),  new Vector3(28f, 28f, 26f), new Vector3(-10f, -105f, -10f));
            spawnRock("EastMtn_Rock_03", 2, new Vector3(76f, 8.5f, 36f),  new Vector3(22f, 20f, 20f), new Vector3(-8f, -65f, -12f));
            spawnRock("EastMtn_Rock_04", 0, new Vector3(95f, 16f, 44f),  new Vector3(30f, 30f, 26f), new Vector3(14f, -135f, 8f));
            spawnRock("EastMtn_Rock_05", 1, new Vector3(78f, 9.5f, 64f),  new Vector3(23f, 22f, 21f), new Vector3(10f, -45f, 10f));
            spawnRock("EastMtn_Rock_06", 2, new Vector3(97f, 17f, 72f),  new Vector3(31f, 32f, 28f), new Vector3(-15f, -120f, -8f));
            spawnRock("EastMtn_Rock_07", 0, new Vector3(79f, 10.5f, 92f), new Vector3(24f, 24f, 22f), new Vector3(-6f, -85f, -12f));
            spawnRock("EastMtn_Rock_08", 1, new Vector3(98f, 18.5f, 100f), new Vector3(32f, 34f, 28f), new Vector3(16f, -145f, 6f));
            spawnRock("EastMtn_Rock_09", 2, new Vector3(80f, 11.5f, 120f), new Vector3(25f, 25f, 23f), new Vector3(12f, -55f, 8f));
            spawnRock("EastMtn_Rock_10", 0, new Vector3(100f, 19.5f, 128f), new Vector3(33f, 36f, 30f), new Vector3(-14f, -130f, -12f));
            spawnRock("EastMtn_Rock_11", 1, new Vector3(81f, 12.5f, 148f), new Vector3(26f, 26f, 24f), new Vector3(-8f, -95f, -10f));
            spawnRock("EastMtn_Rock_12", 2, new Vector3(102f, 21f, 156f), new Vector3(34f, 38f, 30f), new Vector3(15f, -165f, 12f));
            spawnRock("EastMtn_Rock_13", 0, new Vector3(82f, 13.5f, 176f), new Vector3(27f, 27f, 25f), new Vector3(14f, -40f, 15f));
            spawnRock("EastMtn_Rock_14", 1, new Vector3(104f, 23f, 184f), new Vector3(36f, 40f, 32f), new Vector3(-10f, -125f, -10f));

            // 2. Cây phủ bám sườn núi Đông
            spawnTree(prefCoconut, "EastTree_Palm_01", new Vector3(66f, 1.0f, 18f),  new Vector3(1.2f, 1.2f, 1.2f), new Vector3(0f, -35f, 0f));
            spawnTree(prefBanana,  "EastTree_Banana_01", new Vector3(68f, 2.0f, 28f), new Vector3(1.3f, 1.3f, 1.3f), new Vector3(0f, -80f, 0f));
            spawnTree(prefCanopy,  "EastTree_Canopy_01", new Vector3(72f, 4.0f, 48f), new Vector3(1.5f, 1.5f, 1.5f), new Vector3(0f, -120f, 0f));
            spawnTree(prefCoconut, "EastTree_Palm_02", new Vector3(67f, 1.2f, 60f),  new Vector3(1.3f, 1.3f, 1.3f), new Vector3(0f, 40f, 0f));
            spawnTree(prefBamboo,  "EastTree_Bamboo_01", new Vector3(70f, 3.0f, 78f), new Vector3(1.4f, 1.4f, 1.4f), new Vector3(0f, -65f, 0f));
            spawnTree(prefMedium,  "EastTree_Med_01",   new Vector3(74f, 5.0f, 96f), new Vector3(1.4f, 1.4f, 1.4f), new Vector3(0f, -140f, 0f));
            spawnTree(prefCoconut, "EastTree_Palm_03", new Vector3(68f, 1.5f, 115f), new Vector3(1.2f, 1.2f, 1.2f), new Vector3(0f, -15f, 0f));
            spawnTree(prefBanana,  "EastTree_Banana_02", new Vector3(71f, 3.5f, 132f), new Vector3(1.3f, 1.3f, 1.3f), new Vector3(0f, -95f, 0f));
            spawnTree(prefCanopy,  "EastTree_Canopy_02", new Vector3(75f, 6.0f, 152f), new Vector3(1.6f, 1.6f, 1.6f), new Vector3(0f, 80f, 0f));
            spawnTree(prefCoconut, "EastTree_Palm_04", new Vector3(69f, 2.0f, 170f), new Vector3(1.3f, 1.3f, 1.3f), new Vector3(0f, -45f, 0f));
            spawnTree(prefBamboo,  "EastTree_Bamboo_02", new Vector3(73f, 5.0f, 185f), new Vector3(1.5f, 1.5f, 1.5f), new Vector3(0f, -110f, 0f));

            // -------------------------------------------------------------------------------------------------
            // C. NÚI ĐÁ VÀ CÂY PHÍA BẮC (North Backdrop Mountains & Trees: Z >= 204m)
            // -------------------------------------------------------------------------------------------------
            // 1. Núi đá phông nền Cổng Bắc
            spawnRock("NorthMtn_Rock_01", 0, new Vector3(-38f, 14f, 206f), new Vector3(28f, 28f, 26f), new Vector3(8f, 35f, -6f));
            spawnRock("NorthMtn_Rock_02", 1, new Vector3(-68f, 20f, 216f), new Vector3(34f, 36f, 30f), new Vector3(-12f, 105f, 10f));
            spawnRock("NorthMtn_Rock_03", 2, new Vector3(-96f, 26f, 226f), new Vector3(40f, 44f, 36f), new Vector3(14f, 165f, -8f));

            spawnRock("NorthMtn_Rock_04", 0, new Vector3(38f, 14f, 206f),  new Vector3(28f, 28f, 26f), new Vector3(8f, -35f, 6f));
            spawnRock("NorthMtn_Rock_05", 1, new Vector3(68f, 20f, 216f),  new Vector3(34f, 36f, 30f), new Vector3(-12f, -105f, -10f));
            spawnRock("NorthMtn_Rock_06", 2, new Vector3(96f, 26f, 226f),  new Vector3(40f, 44f, 36f), new Vector3(14f, -165f, 8f));

            spawnRock("NorthMtn_Rock_Deep_01", 0, new Vector3(-20f, 18f, 225f), new Vector3(30f, 34f, 28f), new Vector3(6f, 50f, -8f));
            spawnRock("NorthMtn_Rock_Deep_02", 1, new Vector3(20f, 18f, 225f),  new Vector3(30f, 34f, 28f), new Vector3(6f, -50f, 8f));

            // 2. Cây bao quanh chân núi Cổng Bắc
            spawnTree(prefBamboo,  "NorthTree_Bamboo_01", new Vector3(-28f, 4.0f, 195f), new Vector3(1.5f, 1.5f, 1.5f), new Vector3(0f, 25f, 0f));
            spawnTree(prefCoconut, "NorthTree_Palm_01",   new Vector3(-45f, 5.5f, 198f), new Vector3(1.3f, 1.3f, 1.3f), new Vector3(0f, -50f, 0f));
            spawnTree(prefCanopy,  "NorthTree_Canopy_01", new Vector3(-55f, 7.0f, 204f), new Vector3(1.6f, 1.6f, 1.6f), new Vector3(0f, 85f, 0f));

            spawnTree(prefBamboo,  "NorthTree_Bamboo_02", new Vector3(28f, 4.0f, 195f),  new Vector3(1.5f, 1.5f, 1.5f), new Vector3(0f, -25f, 0f));
            spawnTree(prefCoconut, "NorthTree_Palm_02",   new Vector3(45f, 5.5f, 198f),  new Vector3(1.3f, 1.3f, 1.3f), new Vector3(0f, 50f, 0f));
            spawnTree(prefCanopy,  "NorthTree_Canopy_02", new Vector3(55f, 7.0f, 204f),  new Vector3(1.6f, 1.6f, 1.6f), new Vector3(0f, -85f, 0f));

            // -------------------------------------------------------------------------------------------------
            // D. HAI GÓC BIỂN XA (Coastal Sea Rocks & Palms)
            // -------------------------------------------------------------------------------------------------
            spawnRock("CoastRock_West_01", 0, new Vector3(-58f, 1.5f, -14f), new Vector3(14f, 10f, 12f), new Vector3(10f, 40f, -5f));
            spawnRock("CoastRock_West_02", 1, new Vector3(-74f, 3.5f, -22f), new Vector3(18f, 13f, 15f), new Vector3(-8f, 115f, 8f));
            spawnRock("CoastRock_West_Sea",2, new Vector3(-68f, -0.4f,-32f), new Vector3(14f, 7.0f, 11f), new Vector3(12f, 75f, -6f));

            spawnRock("CoastRock_East_01", 0, new Vector3(58f, 1.5f, -14f),  new Vector3(14f, 10f, 12f), new Vector3(10f, -40f, 5f));
            spawnRock("CoastRock_East_02", 1, new Vector3(74f, 3.5f, -22f),  new Vector3(18f, 13f, 15f), new Vector3(-8f, -115f, -8f));
            spawnRock("CoastRock_East_Sea",2, new Vector3(68f, -0.4f,-32f),  new Vector3(14f, 7.0f, 11f), new Vector3(12f, -75f, 6f));

            spawnTree(prefCoconut, "CoastTree_Palm_West_01", new Vector3(-52f, 0.8f, -10f), new Vector3(1.3f, 1.3f, 1.3f), new Vector3(0f, 60f, 0f));
            spawnTree(prefCoconut, "CoastTree_Palm_West_02", new Vector3(-62f, 1.5f, -16f), new Vector3(1.4f, 1.4f, 1.4f), new Vector3(0f, -30f, 0f));
            spawnTree(prefCoconut, "CoastTree_Palm_East_01", new Vector3(52f, 0.8f, -10f),  new Vector3(1.3f, 1.3f, 1.3f), new Vector3(0f, -60f, 0f));
            spawnTree(prefCoconut, "CoastTree_Palm_East_02", new Vector3(62f, 1.5f, -16f),  new Vector3(1.4f, 1.4f, 1.4f), new Vector3(0f, 30f, 0f));

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log($"<color=#10b981>[MapExpansion]</color> HOÀN TẤT! Đã mở rộng map lên 240m, tạo {rocksSpawned} khối núi đá style Planet of Lana và {treesSpawned} cây phủ sườn núi, GPU Instancing ON!");
        }
    }
}
#endif
