#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HoaVaMau.Editor
{
    /// <summary>
    /// Phase 3: Art Pass & Set Dressing cho Cảnh 1 (Rừng Cà Mau, 1954).
    /// Bố trí thảm thực vật đặc trưng rừng ngập mặn Cà Mau, chi tiết hóa 6 vùng:
    /// 1. Thảm cỏ đầm lầy 3D (PF_StylizedGrassTuft_3D) viền 2 bên bờ đường X: 0 -> 60m
    /// 2. Dừa nước Cà Mau (PF_FG_NipaPalm_Fronds) & Bụi đước (PF_FG_Mangrove_Bush)
    /// 3. Dây leo rừng rủ xuống (PF_FG_Vines_Hanging) tại các vòm cổ thụ và gờ leo trèo Vùng C
    /// 4. Hoa súng / sen nước (PF_WaterLily_Cluster) tại vũng nước Vùng B và bến sông Vùng D
    /// 5. Đạo cụ sinh hoạt làng chài Vùng D (Lưới đánh cá, thúng tre, thùng cá muối, bao tải gạo)
    /// 6. Bụi phấn vàng & đốm sáng lơ lửng (PF_Atmospheric_GoldenDust) trong luồng nắng Vùng E
    /// 7. Rễ đước cổ thụ và rêu phong bao bọc chướng ngại vật leo trèo Vùng C
    /// </summary>
    public static class Scene1ArtPassBuilder
    {
        private const string GrassTuftPrefabPath = "Assets/Prefabs/Foliage/PF_StylizedGrassTuft_3D.prefab";
        private const string VinesPrefabPath = "Assets/Prefabs/Vegetation/PF_FG_Vines_Hanging.prefab";
        private const string NipaFrondsPrefabPath = "Assets/Prefabs/Vegetation/PF_FG_NipaPalm_Fronds.prefab";
        private const string MangroveBushPrefabPath = "Assets/Prefabs/Vegetation/PF_FG_Mangrove_Bush.prefab";
        private const string MangroveRootPrefabPath = "Assets/Prefabs/Vegetation/PF_FG_Mangrove_Root.prefab";
        private const string WaterLilyPrefabPath = "Assets/Prefabs/Boats/PF_WaterLily_Cluster.prefab";
        private const string GoldenDustPrefabPath = "Assets/Prefabs/VFX/PF_Atmospheric_GoldenDust.prefab";
        private const string FishingNetPrefabPath = "Assets/Prefabs/Props/PF_Prop_Fishing_Net.prefab";
        private const string BambooBasketPrefabPath = "Assets/Prefabs/Props/PF_Prop_Bamboo_Basket.prefab";
        private const string RiceSacksPrefabPath = "Assets/Prefabs/Props/PF_Prop_RiceSacks.prefab";
        private const string SaltFishBarrelPrefabPath = "Assets/Prefabs/Props/PF_Prop_SaltFish_Barrel.prefab";
        private const string WoodenCratePrefabPath = "Assets/Prefabs/Props/PF_WoodenCrate.prefab";
        private const string MossyRockMeshPath = "Assets/Models/Environment/SM_Rock_Large_01.fbx";
        private const string MossyRockMatPath = "Assets/Materials/M_Rock_MossyBoulder.mat";

        [MenuItem("Hoa va Mau/Level Design/Phase 3 - Execute Art Pass & Set Dressing", false, 2)]
        public static void ExecuteArtPass()
        {
            Debug.Log("<color=#40b8a6>[Scene1ArtPassBuilder]</color> Bắt đầu thực thi Phase 3: Art Pass & Set Dressing...");

            // Lấy 10 thư mục gốc chuẩn
            var folders = GetRootFolders();

            // 1. Rải thảm cỏ đầm lầy 3D viền 2 bên bờ đường X: 0 -> 60m
            DressGroundFoliage(folders["03_Terrain"]);

            // 2. Bố trí Dừa nước Cà Mau & Bụi rậm ngập mặn
            DressNipaPalmsAndBushes(folders["05_Midground"]);

            // 3. Bố trí Dây leo rừng rủ từ trên cao (Hanging Vines)
            DressHangingVines(folders["04_Foreground"], folders["05_Midground"]);

            // 4. Bố trí Hoa sen/súng và thảm nước tại Vùng B & Vùng D
            DressWaterLilies(folders["06_Water"]);

            // 5. Trang trí Đạo cụ sinh hoạt bến sông Vùng D (32-42m)
            DressRiversideDockProps(folders["05_Midground"]);

            // 6. Bụi phấn vàng & Hạt sáng lơ lửng Vùng E (42-52m)
            DressAtmosphericVFXZoneE(folders["09_VFX"]);

            // 7. Điêu khắc Nghệ thuật cho Chướng ngại vật Leo trèo Vùng C (20-32m)
            DressClimbingObstacleZoneC(folders["05_Midground"]);

            // 8. Mở rộng hậu cảnh rừng ngập mặn tới 75m (Vùng F & Chuyển cảnh)
            DressBackgroundContinuity(folders["07_Background"]);

            // Lưu scene
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("<color=#40b8a6>[Scene1ArtPassBuilder]</color> <b>HOÀN TẤT PHASE 3 ART PASS THÀNH CÔNG RỰC RỠ!</b>");
        }

        private static Dictionary<string, GameObject> GetRootFolders()
        {
            string[] folderNames = new string[]
            {
                "01_Camera", "02_Gameplay", "03_Terrain", "04_Foreground", "05_Midground",
                "06_Water", "07_Background", "08_Lighting", "09_VFX", "10_EnvironmentSystems"
            };

            var dict = new Dictionary<string, GameObject>();
            foreach (var name in folderNames)
            {
                var go = GameObject.Find(name);
                if (go == null) go = new GameObject(name);
                dict[name] = go;
            }
            return dict;
        }

        private static GameObject GetOrCreateSubGroup(GameObject parent, string subGroupName)
        {
            Transform existing = parent.transform.Find(subGroupName);
            if (existing != null)
            {
                while (existing.childCount > 0)
                {
                    Undo.DestroyObjectImmediate(existing.GetChild(0).gameObject);
                }
                return existing.gameObject;
            }

            GameObject go = new GameObject(subGroupName);
            go.transform.SetParent(parent.transform, false);
            Undo.RegisterCreatedObjectUndo(go, $"Create {subGroupName}");
            return go;
        }

        // =========================================================================
        // 1. THẢM CỎ ĐẦM LẦY 3D VIỀN BỜ ĐƯỜNG (0 -> 60m)
        // =========================================================================
        private static void DressGroundFoliage(GameObject terrainRoot)
        {
            GameObject group = GetOrCreateSubGroup(terrainRoot, "Ground_Foliage_Tufts");
            GameObject grassPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GrassTuftPrefabPath);
            if (grassPrefab == null) return;

            // Rải thảm cỏ đều đặn 2 bên viền bờ đường:
            // Bờ trước: Z = -1.3 -> -2.1 (tiền cảnh)
            // Bờ sau: Z = +1.3 -> +2.1 (hậu cảnh)
            // Lối đi chính ở giữa Z = 0 luôn thông thoáng cho Minh
            System.Random rnd = new System.Random(1954);

            for (float x = -1.5f; x <= 61.0f; x += 1.8f)
            {
                // Bỏ qua đoạn cầu tàu gỗ ở Vùng D (X: 33.5 -> 38.5)
                if (x >= 33.5f && x <= 38.5f) continue;

                // Xác định cao độ mặt đất theo từng phân vùng
                float groundY = -1.85f;
                if (x >= 12.0f && x <= 18.0f) groundY = -2.12f; // Vũng nước Vùng B
                else if (x >= 23.5f && x <= 27.2f) continue;    // Thân cây leo trèo Vùng C (sẽ trang trí riêng)
                else if (x >= 27.2f && x <= 30.0f) groundY = -1.25f; // Bậc hạ chân
                else if (x >= 44.0f && x <= 50.0f) groundY = -1.65f; // Gò đất cao Vùng E
                else if (x >= 52.0f) groundY = -1.80f;          // Lối khuất Vùng F

                // 1. Bụi cỏ bờ trước (Z âm)
                float frontZ = -1.45f - (float)rnd.NextDouble() * 0.65f;
                float frontX = x + ((float)rnd.NextDouble() - 0.5f) * 0.8f;
                SpawnGrassTuft(group, grassPrefab, new Vector3(frontX, groundY, frontZ), rnd);

                // 2. Bụi cỏ bờ sau (Z dương)
                float backZ = 1.45f + (float)rnd.NextDouble() * 0.65f;
                float backX = x + ((float)rnd.NextDouble() - 0.5f) * 0.8f;
                SpawnGrassTuft(group, grassPrefab, new Vector3(backX, groundY, backZ), rnd);
            }
        }

        private static void SpawnGrassTuft(GameObject parent, GameObject prefab, Vector3 pos, System.Random rnd)
        {
            GameObject tuft = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (tuft == null) return;

            tuft.transform.SetParent(parent.transform, false);
            tuft.transform.position = pos;
            float rotY = (float)rnd.NextDouble() * 360f;
            tuft.transform.rotation = Quaternion.Euler(0f, rotY, 0f);

            // Tỷ lệ vừa phải (bounds gốc ~3.7m -> scale 0.28 ~ 0.42 cho cụm cỏ cao ~0.7-1.0m)
            float scale = 0.28f + (float)rnd.NextDouble() * 0.14f;
            tuft.transform.localScale = new Vector3(scale, scale, scale);

            // Bỏ collider để không cản trở di chuyển
            var col = tuft.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.DestroyImmediate(col);

            Undo.RegisterCreatedObjectUndo(tuft, "Spawn Grass Tuft");
        }

        // =========================================================================
        // 2. DỪA NƯỚC CÀ MAU & BỤI RẬM NGẬP MẶN
        // =========================================================================
        private static void DressNipaPalmsAndBushes(GameObject midgroundRoot)
        {
            GameObject group = GetOrCreateSubGroup(midgroundRoot, "Midground_Vegetation");
            GameObject nipaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(NipaFrondsPrefabPath);
            GameObject bushPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MangroveBushPrefabPath);

            // Vị trí cắm các bụi dừa nước và đước tại các khúc ngoặt & bờ nước
            var nipaSpots = new (float x, float y, float z, float rotZ, float scale)[]
            {
                (1.5f, -1.2f, 1.8f, -15f, 1.1f),    // Vùng A cửa rừng
                (7.5f, -1.3f, -1.6f, 12f, 1.0f),    // Vùng A bờ trước
                (11.2f, -1.4f, 1.7f, -8f, 1.15f),   // Đầu Vùng B vũng nước
                (18.8f, -1.4f, 1.6f, 14f, 1.1f),    // Cuối Vùng B
                (21.5f, -1.3f, -1.5f, -10f, 1.05f), // Trước chướng ngại vật Vùng C
                (31.5f, -1.4f, 1.9f, 6f, 1.2f),     // Cạnh lối rẽ bến sông Vùng D
                (42.5f, -1.2f, 1.7f, -12f, 1.1f),   // Chân dốc đón nắng Vùng E
                (51.5f, -1.2f, -1.5f, 15f, 1.15f),  // Cuối khoảng đón nắng
                (55.5f, -1.1f, 1.8f, -6f, 1.25f),   // Cổng rừng Vùng F
                (59.0f, -1.1f, -1.6f, 10f, 1.2f)    // Lối thoát chuyển cảnh
            };

            foreach (var spot in nipaSpots)
            {
                if (nipaPrefab != null)
                {
                    GameObject go = PrefabUtility.InstantiatePrefab(nipaPrefab) as GameObject;
                    if (go != null)
                    {
                        go.transform.SetParent(group.transform, false);
                        go.transform.position = new Vector3(spot.x, spot.y, spot.z);
                        go.transform.rotation = Quaternion.Euler(0f, 0f, spot.rotZ);
                        go.transform.localScale = Vector3.one * spot.scale;
                        var sr = go.GetComponent<SpriteRenderer>();
                        if (sr != null) sr.sortingOrder = 12;
                        Undo.RegisterCreatedObjectUndo(go, "Spawn Nipa Fronds");
                    }
                }
            }

            // Bụi đước rải xen kẽ
            var bushSpots = new (float x, float y, float z, float scale)[]
            {
                (5.2f, -1.5f, 1.5f, 0.95f),
                (13.8f, -1.8f, -1.7f, 0.9f),
                (17.2f, -1.8f, 1.5f, 1.0f),
                (28.5f, -1.4f, -1.4f, 1.1f),
                (45.5f, -1.3f, 1.6f, 1.05f),
                (48.2f, -1.3f, -1.5f, 0.9f),
                (57.2f, -1.4f, 1.6f, 1.15f)
            };

            foreach (var spot in bushSpots)
            {
                if (bushPrefab != null)
                {
                    GameObject go = PrefabUtility.InstantiatePrefab(bushPrefab) as GameObject;
                    if (go != null)
                    {
                        go.transform.SetParent(group.transform, false);
                        go.transform.position = new Vector3(spot.x, spot.y, spot.z);
                        go.transform.localScale = Vector3.one * spot.scale;
                        var sr = go.GetComponent<SpriteRenderer>();
                        if (sr != null) sr.sortingOrder = 11;
                        Undo.RegisterCreatedObjectUndo(go, "Spawn Mangrove Bush");
                    }
                }
            }
        }

        // =========================================================================
        // 3. DÂY LEO RỪNG RỦ TỪ TRÊN CAO (HANGING VINES)
        // =========================================================================
        private static void DressHangingVines(GameObject fgRoot, GameObject mgRoot)
        {
            GameObject fgGroup = GetOrCreateSubGroup(fgRoot, "Foreground_Vines_Drape");
            GameObject mgGroup = GetOrCreateSubGroup(mgRoot, "Midground_Vines");
            GameObject vinesPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VinesPrefabPath);
            if (vinesPrefab == null) return;

            // 1. Dây leo Tiền cảnh (Z < -2.0m, buông từ góc trên màn hình)
            var fgVineSpots = new (float x, float y, float z, float scale)[]
            {
                (2.0f, 3.8f, -2.4f, 1.25f),
                (8.5f, 4.2f, -2.5f, 1.15f),
                (23.5f, 3.6f, -2.3f, 1.35f), // Rủ xuống ngay phía trước gờ leo trèo Vùng C
                (36.0f, 4.0f, -2.5f, 1.2f),
                (53.0f, 4.2f, -2.4f, 1.3f),  // Rủ xuống ở cổng rừng Vùng F
                (58.5f, 3.9f, -2.3f, 1.25f)
            };

            foreach (var spot in fgVineSpots)
            {
                GameObject vine = PrefabUtility.InstantiatePrefab(vinesPrefab) as GameObject;
                if (vine != null)
                {
                    vine.transform.SetParent(fgGroup.transform, false);
                    vine.transform.position = new Vector3(spot.x, spot.y, spot.z);
                    vine.transform.localScale = Vector3.one * spot.scale;
                    var sr = vine.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.sortingOrder = 28;
                        sr.color = new Color(0.15f, 0.22f, 0.12f, 0.95f); // Tiền cảnh xanh sẫm silhouette
                    }
                    Undo.RegisterCreatedObjectUndo(vine, "Spawn FG Vine");
                }
            }

            // 2. Dây leo Trung cảnh (Z: 0.5 -> 2.5m, bám vào cành cây trung cảnh)
            var mgVineSpots = new (float x, float y, float z, float scale)[]
            {
                (3.8f, 2.8f, 1.2f, 0.95f),
                (14.2f, 2.6f, 1.4f, 0.9f),
                (25.2f, 2.4f, 0.8f, 1.05f), // Dây leo rủ ngay trên đỉnh thân cây Vùng C
                (47.8f, 2.9f, 1.3f, 1.0f),  // Dây leo đung đưa trong luồng nắng Vùng E
                (54.5f, 3.1f, 1.2f, 1.1f)
            };

            foreach (var spot in mgVineSpots)
            {
                GameObject vine = PrefabUtility.InstantiatePrefab(vinesPrefab) as GameObject;
                if (vine != null)
                {
                    vine.transform.SetParent(mgGroup.transform, false);
                    vine.transform.position = new Vector3(spot.x, spot.y, spot.z);
                    vine.transform.localScale = Vector3.one * spot.scale;
                    var sr = vine.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.sortingOrder = 16;
                    Undo.RegisterCreatedObjectUndo(vine, "Spawn MG Vine");
                }
            }
        }

        // =========================================================================
        // 4. HOA SEN / SÚNG & THẢM NƯỚC (VÙNG B & D)
        // =========================================================================
        private static void DressWaterLilies(GameObject waterRoot)
        {
            GameObject group = GetOrCreateSubGroup(waterRoot, "Water_Vegetation");
            GameObject lilyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WaterLilyPrefabPath);
            if (lilyPrefab == null) return;

            // 1. Hoa súng ven vũng nước nông Vùng B (Y = -1.97)
            var puddleLilies = new (float x, float z, float scale)[]
            {
                (13.2f, 1.2f, 1.0f),
                (14.8f, -1.3f, 0.85f),
                (16.5f, 1.4f, 1.1f),
                (17.3f, -1.1f, 0.95f)
            };

            foreach (var spot in puddleLilies)
            {
                GameObject lily = PrefabUtility.InstantiatePrefab(lilyPrefab) as GameObject;
                if (lily != null)
                {
                    lily.transform.SetParent(group.transform, false);
                    lily.transform.position = new Vector3(spot.x, -1.97f, spot.z);
                    lily.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                    lily.transform.localScale = Vector3.one * spot.scale;
                    var sr = lily.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.sortingOrder = 8;
                    Undo.RegisterCreatedObjectUndo(lily, "Spawn Water Lily Puddle");
                }
            }

            // 2. Hoa súng ven mép sông Vùng D (Y = -2.04)
            var riverLilies = new (float x, float z, float scale)[]
            {
                (32.8f, 6.5f, 1.2f),
                (35.2f, 8.2f, 1.35f),
                (39.5f, 7.8f, 1.15f),
                (41.2f, 6.0f, 1.25f)
            };

            foreach (var spot in riverLilies)
            {
                GameObject lily = PrefabUtility.InstantiatePrefab(lilyPrefab) as GameObject;
                if (lily != null)
                {
                    lily.transform.SetParent(group.transform, false);
                    lily.transform.position = new Vector3(spot.x, -2.04f, spot.z);
                    lily.transform.localScale = Vector3.one * spot.scale;
                    var sr = lily.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.sortingOrder = 7;
                    Undo.RegisterCreatedObjectUndo(lily, "Spawn Water Lily River");
                }
            }
        }

        // =========================================================================
        // 5. ĐẠO CỤ BẾN SÔNG VÙNG D (32 - 42m)
        // =========================================================================
        private static void DressRiversideDockProps(GameObject midgroundRoot)
        {
            GameObject group = GetOrCreateSubGroup(midgroundRoot, "Settlement_Props_Life");

            GameObject netPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FishingNetPrefabPath);
            GameObject basketPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BambooBasketPrefabPath);
            GameObject sacksPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RiceSacksPrefabPath);
            GameObject barrelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SaltFishBarrelPrefabPath);
            GameObject cratePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WoodenCratePrefabPath);

            // 1. Thúng tre & Bao tải gạo ven nhà sàn (X = 33.2m, Y = -1.82m)
            if (basketPrefab != null)
            {
                var b1 = PrefabUtility.InstantiatePrefab(basketPrefab) as GameObject;
                if (b1 != null)
                {
                    b1.transform.SetParent(group.transform, false);
                    b1.transform.position = new Vector3(33.2f, -1.72f, 2.2f);
                    b1.transform.localScale = Vector3.one * 0.38f;
                    Undo.RegisterCreatedObjectUndo(b1, "Spawn Basket 1");
                }

                var b2 = PrefabUtility.InstantiatePrefab(basketPrefab) as GameObject;
                if (b2 != null)
                {
                    b2.transform.SetParent(group.transform, false);
                    b2.transform.position = new Vector3(37.4f, -1.75f, 2.4f);
                    b2.transform.rotation = Quaternion.Euler(0, 45, 12);
                    b2.transform.localScale = Vector3.one * 0.32f;
                    Undo.RegisterCreatedObjectUndo(b2, "Spawn Basket 2");
                }
            }

            // 2. Bao tải gạo & thùng gỗ gần bến thuyền (X = 38.2m)
            if (sacksPrefab != null)
            {
                var s1 = PrefabUtility.InstantiatePrefab(sacksPrefab) as GameObject;
                if (s1 != null)
                {
                    s1.transform.SetParent(group.transform, false);
                    s1.transform.position = new Vector3(38.2f, -1.78f, 2.1f);
                    s1.transform.localScale = Vector3.one * 0.45f;
                    Undo.RegisterCreatedObjectUndo(s1, "Spawn Rice Sacks");
                }
            }

            // 3. Thùng ủ cá muối (X = 34.6m)
            if (barrelPrefab != null)
            {
                var br = PrefabUtility.InstantiatePrefab(barrelPrefab) as GameObject;
                if (br != null)
                {
                    br.transform.SetParent(group.transform, false);
                    br.transform.position = new Vector3(34.6f, -1.70f, 2.3f);
                    br.transform.localScale = Vector3.one * 0.42f;
                    Undo.RegisterCreatedObjectUndo(br, "Spawn Barrel");
                }
            }

            // 4. Lưới đánh cá phơi ven bờ (X = 35.8m)
            if (netPrefab != null)
            {
                var net = PrefabUtility.InstantiatePrefab(netPrefab) as GameObject;
                if (net != null)
                {
                    net.transform.SetParent(group.transform, false);
                    net.transform.position = new Vector3(35.8f, -1.65f, 2.2f);
                    net.transform.rotation = Quaternion.Euler(15, 0, 0);
                    net.transform.localScale = Vector3.one * 0.65f;
                    Undo.RegisterCreatedObjectUndo(net, "Spawn Fishing Net");
                }
            }
        }

        // =========================================================================
        // 6. BỤI PHẤN VÀNG & HẠT SÁNG VÙNG E (42 - 52m)
        // =========================================================================
        private static void DressAtmosphericVFXZoneE(GameObject vfxRoot)
        {
            GameObject group = GetOrCreateSubGroup(vfxRoot, "ZoneE_Sunbeam_Dust");
            GameObject dustPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GoldenDustPrefabPath);
            if (dustPrefab != null)
            {
                GameObject dust = PrefabUtility.InstantiatePrefab(dustPrefab) as GameObject;
                if (dust != null)
                {
                    dust.name = "Pollen_GoldenDust_ZoneE";
                    dust.transform.SetParent(group.transform, false);
                    dust.transform.position = new Vector3(47.0f, 1.2f, 0.5f);
                    dust.transform.localScale = new Vector3(3.5f, 3.5f, 3.5f);

                    var ps = dust.GetComponent<ParticleSystem>();
                    if (ps != null)
                    {
                        var main = ps.main;
                        main.startColor = new Color(1.0f, 0.95f, 0.72f, 0.65f);
                        main.startSize = 0.08f;
                    }
                    Undo.RegisterCreatedObjectUndo(dust, "Spawn Golden Dust");
                }
            }
        }

        // =========================================================================
        // 7. NGHỆ THUẬT GỜ LEO TRÈO VÙNG C (20 - 32m)
        // =========================================================================
        private static void DressClimbingObstacleZoneC(GameObject midgroundRoot)
        {
            GameObject group = GetOrCreateSubGroup(midgroundRoot, "ClimbObstacle_ArtDressing");

            // Bổ sung các cụm rễ đước lớn bám chặt vào vách đứng X = 23.5m
            GameObject rootPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MangroveRootPrefabPath);
            if (rootPrefab != null)
            {
                // Rễ vươn từ bờ đất bám lên mép vách
                var r1 = PrefabUtility.InstantiatePrefab(rootPrefab) as GameObject;
                if (r1 != null)
                {
                    r1.transform.SetParent(group.transform, false);
                    r1.transform.position = new Vector3(23.3f, -1.05f, -0.85f);
                    r1.transform.rotation = Quaternion.Euler(0, 0, -15);
                    r1.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);
                    var sr = r1.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.sortingOrder = 16;
                    Undo.RegisterCreatedObjectUndo(r1, "Spawn Obstacle Root 1");
                }

                // Rễ bám bên bờ hậu cảnh
                var r2 = PrefabUtility.InstantiatePrefab(rootPrefab) as GameObject;
                if (r2 != null)
                {
                    r2.transform.SetParent(group.transform, false);
                    r2.transform.position = new Vector3(23.6f, -0.95f, 1.15f);
                    r2.transform.rotation = Quaternion.Euler(0, 180, 12);
                    r2.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
                    var sr = r2.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.sortingOrder = 14;
                    Undo.RegisterCreatedObjectUndo(r2, "Spawn Obstacle Root 2");
                }
            }

            // Bổ sung tảng đá rêu phong tự nhiên trên mặt gờ đỉnh (X = 25.8m, Y = -0.15m)
            Mesh rockMesh = AssetDatabase.LoadAssetAtPath<Mesh>(MossyRockMeshPath);
            Material rockMat = AssetDatabase.LoadAssetAtPath<Material>(MossyRockMatPath);
            if (rockMesh != null && rockMat != null)
            {
                var topRock = new GameObject("MossyRock_TopRidge");
                topRock.transform.SetParent(group.transform, false);
                topRock.transform.position = new Vector3(25.8f, 0.05f, 1.25f);
                topRock.transform.rotation = Quaternion.Euler(10, 45, -5);
                topRock.transform.localScale = new Vector3(0.85f, 0.7f, 0.85f);
                var mf = topRock.AddComponent<MeshFilter>();
                mf.sharedMesh = rockMesh;
                var mr = topRock.AddComponent<MeshRenderer>();
                mr.sharedMaterial = rockMat;
                Undo.RegisterCreatedObjectUndo(topRock, "Spawn Top Rock");
            }
        }

        // =========================================================================
        // 8. HẬU CẢNH RỪNG NGẬP MẶN TỚI 75M (VÙNG F & CHUYỂN CẢNH)
        // =========================================================================
        private static void DressBackgroundContinuity(GameObject bgRoot)
        {
            GameObject group = GetOrCreateSubGroup(bgRoot, "Background_Extension_60_75m");
            GameObject bgTreePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Vegetation/PF_BG_Mangrove_Forest.prefab");

            var extSpots = new (float x, float y, float z, float scale)[]
            {
                (62.5f, 0.2f, 13.0f, 3.2f),
                (67.0f, 0.5f, 15.0f, 3.4f),
                (72.5f, 0.2f, 14.0f, 3.2f),
                (77.0f, 0.6f, 16.0f, 3.5f)
            };

            foreach (var spot in extSpots)
            {
                if (bgTreePrefab != null)
                {
                    GameObject go = PrefabUtility.InstantiatePrefab(bgTreePrefab) as GameObject;
                    if (go != null)
                    {
                        go.transform.SetParent(group.transform, false);
                        go.transform.position = new Vector3(spot.x, spot.y, spot.z);
                        go.transform.localScale = Vector3.one * spot.scale;
                        var sr = go.GetComponentInChildren<SpriteRenderer>();
                        if (sr != null) sr.sortingOrder = -5;
                        Undo.RegisterCreatedObjectUndo(go, "Spawn BG Forest Ext");
                    }
                }
            }
        }
    }
}
#endif
