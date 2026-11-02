using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

namespace HoaVaMau.EditorTools
{
    public static class OverhaulVillageLayoutAndEnvironment
    {
        private const string ScenePath = "Assets/Scenes/CaMau_1954_Village.unity";

        [MenuItem("Tools/Hoa Tren Dat Do/Overhaul Village Layout And Environment")]
        public static void Execute()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != ScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                activeScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            Debug.Log("[OverhaulVillage] Starting comprehensive village layout and environment overhaul...");

            // =========================================================================
            // 1. CẢNH NỀN KHÔNG CÒN MÀU TRẮNG (RICH SUNSET & TWILIGHT SKY)
            // =========================================================================
            SetupVibrantSkyAndAtmosphere();

            // =========================================================================
            // 2. CHỈNH LẠI NHÀ ĐỨNG THẲNG LÊN (UPRIGHT HOUSES & HUTS)
            // =========================================================================
            StraightenAllHousesAndHuts();

            // =========================================================================
            // 3. CHỈNH LẠI CÂY ĐỨNG THẲNG LÊN (UPRIGHT TREES & FOLIAGE)
            // =========================================================================
            StraightenAllTreesAndFoliage();

            // =========================================================================
            // 4. CHỈNH LẠI PHẦN ĐẤT KHÔNG BỊ CHÊNH (FLAT, LEVEL GROUND & PATHS)
            // =========================================================================
            LevelAllVillageGroundAndPaths();

            // =========================================================================
            // 5. BIỂN RỘNG HƠN & PHẦN BIỂN KHÔNG CÓ ĐẤT (VAST SOUTH SEA WITHOUT LAND)
            // =========================================================================
            SetupExpansiveSouthSea();

            // =========================================================================
            // 6. THIẾT KẾ LẠI CÁNH ĐỒNG LÚA DÀY HƠN, NHIỀU HƠN PHÍA SAU LÀNG (NORTH)
            // =========================================================================
            BuildExtensiveNorthRiceFields();

            // Save Scene
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log("[OverhaulVillage] Successfully completed comprehensive layout and environment overhaul!");
        }

        private static void SetupVibrantSkyAndAtmosphere()
        {
            string skyboxMatPath = "Assets/Materials/Environment/Backdrop_Sky/M_Skybox_CaMau_Panorama.mat";
            Material skyboxMat = AssetDatabase.LoadAssetAtPath<Material>(skyboxMatPath);
            var skyTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Sky/Sky_Cloud_Krita_5120x2160.png");

            if (skyboxMat != null)
            {
                skyboxMat.mainTexture = skyTex;
                // Rich golden sunset tint to completely eliminate washed-out white
                skyboxMat.SetColor("_Tint", new Color(0.96f, 0.76f, 0.52f, 1.0f));
                skyboxMat.SetFloat("_Exposure", 0.92f);
                EditorUtility.SetDirty(skyboxMat);
            }

            string cylMatPath = "Assets/Materials/Environment/Backdrop_Sky/M_Sky_Backdrop_Surround.mat";
            Material cylinderMat = AssetDatabase.LoadAssetAtPath<Material>(cylMatPath);
            if (cylinderMat != null)
            {
                cylinderMat.mainTexture = skyTex;
                cylinderMat.SetColor("_Tint", new Color(0.94f, 0.78f, 0.56f, 1.0f));
                cylinderMat.SetFloat("_Exposure", 0.92f);
                if (cylinderMat.HasProperty("_Vignette")) cylinderMat.SetFloat("_Vignette", 0.25f);
                EditorUtility.SetDirty(cylinderMat);
            }

            // RenderSettings ambient & fog
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.95f, 0.75f, 0.55f);
            RenderSettings.ambientEquatorColor = new Color(0.75f, 0.55f, 0.40f);
            RenderSettings.ambientGroundColor = new Color(0.32f, 0.26f, 0.18f);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0028f;
            RenderSettings.fogColor = new Color(0.88f, 0.68f, 0.48f); // Warm golden sunset glow

            // Cameras clear color
            var cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (var cam in cameras)
            {
                cam.clearFlags = CameraClearFlags.Skybox;
                cam.backgroundColor = new Color(0.18f, 0.28f, 0.48f); // Deep dusk blue
                if (cam.farClipPlane < 400f) cam.farClipPlane = 400f;
                EditorUtility.SetDirty(cam);
            }

            // Configure Vistas with warm rich colors
            var matVistaSouth = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/LanaOverhaul/M_Vista_04_SeaHorizon_Sunset.mat");
            if (matVistaSouth != null)
            {
                matVistaSouth.SetColor("_Color", new Color(1.0f, 0.92f, 0.80f, 1.0f));
                matVistaSouth.SetFloat("_TopFeather", 0.38f);
                matVistaSouth.SetFloat("_LeftFeather", 0.25f);
                matVistaSouth.SetFloat("_RightFeather", 0.25f);
                EditorUtility.SetDirty(matVistaSouth);
            }

            var matVistaNorth = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/LanaOverhaul/M_Vista_03_HillsFields_Sunset.mat");
            if (matVistaNorth != null)
            {
                matVistaNorth.SetColor("_Color", new Color(1.0f, 0.94f, 0.82f, 1.0f));
                matVistaNorth.SetFloat("_TopFeather", 0.38f);
                matVistaNorth.SetFloat("_LeftFeather", 0.25f);
                matVistaNorth.SetFloat("_RightFeather", 0.25f);
                EditorUtility.SetDirty(matVistaNorth);
            }
        }

        private static void StraightenAllHousesAndHuts()
        {
            var allTransforms = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
            foreach (var t in allTransforms)
            {
                string nameLower = t.name.ToLower();
                bool isHouseOrHut = nameLower.Contains("house") || nameLower.Contains("nha_") || 
                                    nameLower.Contains("hut") || nameLower.Contains("shelter") || 
                                    nameLower.Contains("choila") || nameLower.Contains("choi_");

                if (isHouseOrHut)
                {
                    // Ensure upright orientation on parent
                    Vector3 euler = t.rotation.eulerAngles;
                    t.rotation = Quaternion.Euler(0f, euler.y, 0f);

                    // Normalize Mesh_0 child
                    var m0 = t.Find("Mesh_0");
                    if (m0 != null)
                    {
                        m0.localRotation = Quaternion.identity;
                        m0.localScale = Vector3.one;
                        EditorUtility.SetDirty(m0);
                    }
                    EditorUtility.SetDirty(t);
                }
            }
        }

        private static void StraightenAllTreesAndFoliage()
        {
            var allTransforms = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
            int count = 0;
            foreach (var t in allTransforms)
            {
                string n = t.name.ToLower();
                bool isPlant = n.Contains("tree") || n.Contains("banana") || n.Contains("coconut") ||
                               n.Contains("bamboo") || n.Contains("bush") || n.Contains("foliage") ||
                               n.Contains("flower") || n.Contains("plant") || n.Contains("reed") ||
                               n.Contains("paddy") || n.Contains("wildflower");

                if (isPlant)
                {
                    Vector3 euler = t.rotation.eulerAngles;
                    if (Mathf.Abs(Mathf.DeltaAngle(euler.x, 0f)) > 0.1f || Mathf.Abs(Mathf.DeltaAngle(euler.z, 0f)) > 0.1f)
                    {
                        t.rotation = Quaternion.Euler(0f, euler.y, 0f);
                        EditorUtility.SetDirty(t);
                        count++;
                    }

                    // Also check immediate children
                    for (int i = 0; i < t.childCount; i++)
                    {
                        var c = t.GetChild(i);
                        Vector3 ceuler = c.localRotation.eulerAngles;
                        if (Mathf.Abs(Mathf.DeltaAngle(ceuler.x, 0f)) > 0.1f || Mathf.Abs(Mathf.DeltaAngle(ceuler.z, 0f)) > 0.1f)
                        {
                            c.localRotation = Quaternion.Euler(0f, ceuler.y, 0f);
                            EditorUtility.SetDirty(c);
                        }
                    }
                }
            }
            Debug.Log($"[OverhaulVillage] Straightened {count} tilted trees/plants.");
        }

        private static void LevelAllVillageGroundAndPaths()
        {
            // Level terrain knolls to top surface Y = 0.0m
            // A cube of scale Y=2.0 centered at Y=-1.0 has its top surface at exactly Y = 0.0m!
            var terrainGroup = GameObject.Find("CaMau_1954_Village/Environment/Terrain_3D");
            if (terrainGroup != null)
            {
                // San Lang
                var sanLang = terrainGroup.transform.Find("Terrain_SanLang_Main");
                if (sanLang != null)
                {
                    sanLang.position = new Vector3(0f, -1.0f, 4f);
                    sanLang.localScale = new Vector3(32f, 2.0f, 24f);
                }

                // Nha An
                var nhaAnKnoll = terrainGroup.transform.Find("Terrain_NhaAn_Knoll");
                if (nhaAnKnoll != null)
                {
                    nhaAnKnoll.position = new Vector3(0.5f, -1.0f, 17f);
                    nhaAnKnoll.localScale = new Vector3(24f, 2.0f, 14f);
                }

                // Vuon Cay
                var vuonCayKnoll = terrainGroup.transform.Find("Terrain_VuonCay_Knoll");
                if (vuonCayKnoll != null)
                {
                    vuonCayKnoll.position = new Vector3(-18f, -1.0f, 10f);
                    vuonCayKnoll.localScale = new Vector3(22f, 2.0f, 26f);
                }

                // Khu Lay Nuoc
                var khuLayNuocKnoll = terrainGroup.transform.Find("Terrain_KhuLayNuoc_Knoll");
                if (khuLayNuocKnoll != null)
                {
                    khuLayNuocKnoll.position = new Vector3(15f, -1.0f, 6f);
                    khuLayNuocKnoll.localScale = new Vector3(16f, 2.0f, 18f);
                }

                // Remove uneven intermediate slope ramps that caused height discrepancies
                var ramp1 = terrainGroup.transform.Find("Ramp_SanLang_To_NhaAn");
                if (ramp1 != null) Object.DestroyImmediate(ramp1.gameObject);

                var ramp2 = terrainGroup.transform.Find("Ramp_SanLang_To_VuonCay");
                if (ramp2 != null) Object.DestroyImmediate(ramp2.gameObject);

                // Base terrain: offset so it ONLY covers village land (Z from -8m to 85m)
                // Center Z = 38.5m, size in Z = 93m. Stops cleanly at Z = -8.0m!
                var baseTerrain = terrainGroup.transform.Find("Terrain_Base_Expansive");
                if (baseTerrain != null)
                {
                    baseTerrain.position = new Vector3(0f, -1.0f, 38.5f);
                    baseTerrain.localScale = new Vector3(130f, 2.0f, 93f);
                }

                // Riverbed under sea: lower to Y = -6.0m so it never protrudes
                var riverBed = terrainGroup.transform.Find("RiverBed_South_Mud");
                if (riverBed != null)
                {
                    riverBed.position = new Vector3(0f, -6.0f, -60f);
                    riverBed.localScale = new Vector3(250f, 2.0f, 120f);
                }
            }

            // Level all Paths in Paths_3D_Network to flush Y = 0.015m
            var pathsGroup = GameObject.Find("CaMau_1954_Village/Environment/Paths_3D_Network");
            if (pathsGroup != null)
            {
                for (int i = 0; i < pathsGroup.transform.childCount; i++)
                {
                    var p = pathsGroup.transform.GetChild(i);
                    Vector3 pos = p.position;
                    p.position = new Vector3(pos.x, 0.015f, pos.z);
                    EditorUtility.SetDirty(p);
                }
            }

            // Adjust An Player spawn position to exact ground level
            var player = GameObject.Find("An_Player");
            if (player != null)
            {
                player.transform.position = new Vector3(player.transform.position.x, 0.0f, player.transform.position.z);
            }
        }

        private static void SetupExpansiveSouthSea()
        {
            var waterGroup = GameObject.Find("CaMau_1954_Village/Environment/WaterSystem_3D");
            if (waterGroup == null) return;

            Material waterMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Water/M_StylizedWater_Sunset.mat");
            if (waterMat == null) waterMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_OriWater_CaMau.mat");

            // Remove old tiny river plane
            var oldRiver = waterGroup.transform.Find("Water_South_River");
            if (oldRiver != null) Object.DestroyImmediate(oldRiver.gameObject);

            // Create vast expansive sea: 320m wide x 160m deep, stretching from Z = -8m to Z = -168m
            Transform seaTrans = waterGroup.transform.Find("Water_South_Sea_Expansive");
            GameObject seaObj = (seaTrans != null) ? seaTrans.gameObject : GameObject.CreatePrimitive(PrimitiveType.Plane);
            seaObj.name = "Water_South_Sea_Expansive";
            seaObj.transform.SetParent(waterGroup.transform, false);
            // In Unity Plane (10x10), scale (32, 1, 16) = 320m x 160m
            seaObj.transform.position = new Vector3(0f, -0.65f, -88f);
            seaObj.transform.rotation = Quaternion.identity;
            seaObj.transform.localScale = new Vector3(32f, 1f, 16f);

            var mr = seaObj.GetComponent<MeshRenderer>();
            if (mr != null && waterMat != null) mr.sharedMaterial = waterMat;

            // Remove any land blocks south of Z = -8.0m in Environment
            var env = GameObject.Find("CaMau_1954_Village/Environment");
            if (env != null)
            {
                var renderers = env.GetComponentsInChildren<MeshRenderer>();
                foreach (var r in renderers)
                {
                    if (r.gameObject == seaObj || r.name.Contains("Water") || r.name.Contains("Sky") || r.name.Contains("Vista")) continue;
                    // If center is well in the sea zone (Z < -10m) and near water level, remove or push deep
                    if (r.bounds.center.z < -10f && r.bounds.max.y > -0.65f)
                    {
                        if (r.name.Contains("Terrain") || r.name.Contains("Mud") || r.name.Contains("Chunk"))
                        {
                            Object.DestroyImmediate(r.gameObject);
                        }
                    }
                }
            }

            // Position Pier & Dock on the shoreline (Z = -8.0m) reaching over the sea
            var pierArea = GameObject.Find("CaMau_1954_Village/Village_Areas/08_Ben_Xuong");
            if (pierArea != null)
            {
                // Ensure pier components sit on water level
                for (int i = 0; i < pierArea.transform.childCount; i++)
                {
                    var c = pierArea.transform.GetChild(i);
                    Vector3 pos = c.position;
                    if (pos.z > -7.5f) pos.z = -8.5f;
                    c.position = pos;
                }
            }

            Debug.Log("[OverhaulVillage] Configured vast 320m x 160m southern sea without land obstruction.");
        }

        private static void BuildExtensiveNorthRiceFields()
        {
            var villageAreas = GameObject.Find("CaMau_1954_Village/Village_Areas");
            if (villageAreas == null) return;

            Transform cdTrans = villageAreas.transform.Find("06_Canh_Dong");
            GameObject cdGroup = (cdTrans != null) ? cdTrans.gameObject : new GameObject("06_Canh_Dong");
            cdGroup.transform.SetParent(villageAreas.transform, false);
            cdGroup.transform.position = Vector3.zero;

            // Clean old sparse children inside 06_Canh_Dong to re-layout densely in the North
            while (cdGroup.transform.childCount > 0)
            {
                Object.DestroyImmediate(cdGroup.transform.GetChild(0).gameObject);
            }

            // Materials
            Material mudMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_CaMau_PaddyMud.mat");
            if (mudMat == null) mudMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/CaMau/M_CaMau_RiverMud.mat");

            Material strawMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_CaMau_StrawStack.mat");
            Material timberMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/CaMau/M_CaMau_TimberLight.mat");
            Material waterMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Water/M_StylizedWater_Sunset.mat");

            // Prefabs
            var riceRowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Rice/PF_Rice_RowStrip_4m.prefab");
            var riceClusterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Rice/PF_Rice_Cluster_3in1.prefab");
            var riceClumpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Rice/PF_RiceClump_Mature.prefab");
            var farmerHousePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/PF_StiltedHouse_Thatched.prefab");
            var waterBuffaloPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/NPCs/PF_NPC_WaterBuffalo.prefab");
            var riceSacksPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Props/PF_Prop_RiceSacks.prefab");

            // 1. Vast Northern Paddy Terraces Base (Z from 24m to 76m, X from -42m to +42m)
            // Center (0, -0.05, 50), Scale (84, 0.2, 52)
            GameObject paddyBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            paddyBase.name = "PaddyField_Northern_Base";
            paddyBase.transform.SetParent(cdGroup.transform, false);
            paddyBase.transform.position = new Vector3(0f, -0.08f, 50f);
            paddyBase.transform.localScale = new Vector3(84f, 0.2f, 52f);
            if (mudMat != null) paddyBase.GetComponent<Renderer>().sharedMaterial = mudMat;

            // 2. Network of Elevated Dikes (Bờ đê ruộng lúa)
            // Main Central Dike (North-South) from Z = 22m to 75m
            CreatePaddyDike(cdGroup.transform, "Dike_Main_NorthSouth", new Vector3(0f, 0.05f, 48.5f), new Vector3(2.2f, 0.15f, 53f), mudMat);
            // East-West Dike 1 at Z = 36m
            CreatePaddyDike(cdGroup.transform, "Dike_Cross_01", new Vector3(0f, 0.05f, 36f), new Vector3(82f, 0.15f, 1.8f), mudMat);
            // East-West Dike 2 at Z = 52m
            CreatePaddyDike(cdGroup.transform, "Dike_Cross_02", new Vector3(0f, 0.05f, 52f), new Vector3(82f, 0.15f, 1.8f), mudMat);
            // East-West Dike 3 at Z = 68m
            CreatePaddyDike(cdGroup.transform, "Dike_Cross_03", new Vector3(0f, 0.05f, 68f), new Vector3(82f, 0.15f, 1.8f), mudMat);

            // Lateral Dikes (Left and Right)
            CreatePaddyDike(cdGroup.transform, "Dike_Lateral_West", new Vector3(-22f, 0.05f, 48.5f), new Vector3(1.6f, 0.15f, 53f), mudMat);
            CreatePaddyDike(cdGroup.transform, "Dike_Lateral_East", new Vector3(22f, 0.05f, 48.5f), new Vector3(1.6f, 0.15f, 53f), mudMat);

            // 3. Dense 3D Rice Plant Rows across all 6 Large Paddy Quadrants
            float[] xSections = new float[] { -32f, -11f, 11f, 32f };
            float[] zSections = new float[] { 28f, 32f, 40f, 44f, 48f, 56f, 60f, 64f, 72f };

            int spawnedRows = 0;
            if (riceRowPrefab != null)
            {
                foreach (float z in zSections)
                {
                    for (float x = -38f; x <= 38f; x += 4.5f)
                    {
                        // Skip dikes
                        if (Mathf.Abs(x) < 2.0f || Mathf.Abs(x - 22f) < 1.5f || Mathf.Abs(x + 22f) < 1.5f) continue;

                        GameObject row = (GameObject)PrefabUtility.InstantiatePrefab(riceRowPrefab, cdGroup.transform);
                        row.name = $"Paddy_Row_Z{z:F0}_X{x:F0}";
                        row.transform.position = new Vector3(x, 0.0f, z);
                        row.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                        row.transform.localScale = new Vector3(1.1f, 1.15f, 1.1f);
                        spawnedRows++;
                    }
                }
            }

            // Fill Clumps in between rows for exceptional density
            if (riceClusterPrefab != null)
            {
                for (float z = 30f; z <= 70f; z += 6f)
                {
                    for (float x = -36f; x <= 36f; x += 5.5f)
                    {
                        if (Mathf.Abs(x) < 2.2f) continue;
                        GameObject clump = (GameObject)PrefabUtility.InstantiatePrefab(riceClusterPrefab, cdGroup.transform);
                        clump.name = $"Paddy_Clump_Z{z:F0}_X{x:F0}";
                        clump.transform.position = new Vector3(x + Random.Range(-0.8f, 0.8f), 0.0f, z + Random.Range(-0.8f, 0.8f));
                        clump.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                        clump.transform.localScale = Vector3.one * Random.Range(1.0f, 1.25f);
                    }
                }
            }

            // 4. Farmer Stilted House overlooking fields
            if (farmerHousePrefab != null)
            {
                GameObject hut = (GameObject)PrefabUtility.InstantiatePrefab(farmerHousePrefab, cdGroup.transform);
                hut.name = "Nha_NongDan_CanhDong";
                hut.transform.position = new Vector3(26f, 0.0f, 34f);
                hut.transform.rotation = Quaternion.Euler(0f, 200f, 0f);
                hut.transform.localScale = Vector3.one * 3.8f;
                var m0 = hut.transform.Find("Mesh_0");
                if (m0 != null)
                {
                    m0.localRotation = Quaternion.identity;
                    m0.localScale = Vector3.one;
                }
            }

            // 5. Watch Hut on Stilts (Chòi canh lúa)
            GameObject watchHut = GameObject.CreatePrimitive(PrimitiveType.Cube);
            watchHut.name = "ChoiCanhLua_NorthFields";
            watchHut.transform.SetParent(cdGroup.transform, false);
            watchHut.transform.position = new Vector3(-12f, 1.5f, 44f);
            watchHut.transform.localScale = new Vector3(3.5f, 2.5f, 3.5f);
            if (timberMat != null) watchHut.GetComponent<Renderer>().sharedMaterial = timberMat;

            // 6. Water Buffalo in Mud
            if (waterBuffaloPrefab != null)
            {
                GameObject buffalo = (GameObject)PrefabUtility.InstantiatePrefab(waterBuffaloPrefab, cdGroup.transform);
                buffalo.name = "WaterBuffalo_NorthPaddy";
                buffalo.transform.position = new Vector3(-4f, 0.0f, 36f);
                buffalo.transform.rotation = Quaternion.Euler(0f, 75f, 0f);
                buffalo.transform.localScale = Vector3.one * 1.3f;
            }

            // 7. Haystacks on Dikes
            CreateHaystack3D(cdGroup.transform, "DongRom_Field_01", new Vector3(-1f, 0.1f, 38f), 2.2f, strawMat);
            CreateHaystack3D(cdGroup.transform, "DongRom_Field_02", new Vector3(1f, 0.1f, 54f), 2.5f, strawMat);
            CreateHaystack3D(cdGroup.transform, "DongRom_Field_03", new Vector3(23f, 0.1f, 38f), 2.0f, strawMat);

            // 8. Rice Sacks at Field Edge
            if (riceSacksPrefab != null)
            {
                GameObject sacks = (GameObject)PrefabUtility.InstantiatePrefab(riceSacksPrefab, cdGroup.transform);
                sacks.name = "RiceSacks_PaddyCorner";
                sacks.transform.position = new Vector3(23f, 0.15f, 32f);
                sacks.transform.rotation = Quaternion.Euler(0f, 30f, 0f);
                sacks.transform.localScale = Vector3.one * 1.1f;
            }

            // 9. Position Northern Vista Quad directly behind the rice fields at Z = 80m
            var bgGroup = GameObject.Find("CaMau_1954_Village/Environment/Background_Atmosphere");
            if (bgGroup != null)
            {
                var northVista = bgGroup.transform.Find("Backdrop_Vista_North");
                if (northVista != null)
                {
                    northVista.position = new Vector3(0f, 16f, 82f);
                    northVista.rotation = Quaternion.Euler(0f, 180f, 0f);
                    northVista.localScale = new Vector3(240f, 65f, 1f);
                    var matFields = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/LanaOverhaul/M_Vista_03_HillsFields_Sunset.mat");
                    if (matFields != null) northVista.GetComponent<Renderer>().sharedMaterial = matFields;
                }
            }

            Debug.Log($"[OverhaulVillage] Built extensive, dense northern rice fields with {spawnedRows} rice rows, dikes, huts, and haystacks.");
        }

        private static void CreatePaddyDike(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject dike = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dike.name = name;
            dike.transform.SetParent(parent, false);
            dike.transform.position = pos;
            dike.transform.localScale = scale;
            if (mat != null) dike.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static void CreateHaystack3D(Transform parent, string name, Vector3 pos, float radius, Material mat)
        {
            GameObject haystack = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            haystack.name = name;
            haystack.transform.SetParent(parent, false);
            haystack.transform.position = new Vector3(pos.x, pos.y + radius * 0.5f, pos.z);
            haystack.transform.localScale = new Vector3(radius, radius * 0.8f, radius);
            if (mat != null) haystack.GetComponent<Renderer>().sharedMaterial = mat;
        }
    }
}
