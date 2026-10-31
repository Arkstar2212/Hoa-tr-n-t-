using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace HoaDo.Editor
{
    public static class OverhaulVillagePaddiesWharfAndHouses
    {
        [MenuItem("Tools/Hoa Do/Overhaul Village Paddies, Wharf & Houses")]
        public static void Execute()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (!activeScene.path.Contains("CaMau_1954_Village"))
            {
                Debug.LogWarning("[Overhaul] Active scene is not CaMau_1954_Village. Opening scene...");
                EditorSceneManager.OpenScene("Assets/Scenes/CaMau_1954_Village.unity");
            }

            Debug.Log("[Overhaul] Starting comprehensive overhaul...");

            AddHousesBackCleanly();
            SetupRiceFieldPlotsAndWater();
            SetupVillagePathsAndGround();
            SetupBackgroundsOnlyAtRiceField();
            FixCharactersGroundAlignment();
            RemoveInvisibleBlockers();
            RedesignExpandedWharf();

            Physics.SyncTransforms();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("[Overhaul] Overhaul completed and scene saved successfully!");
        }

        private static void AddHousesBackCleanly()
        {
            var thatchedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/PF_StiltedHouse_Thatched.prefab");
            var woodPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/PF_StiltedHouse_Wood.prefab");
            var nhaSanGoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/NhaSanGo.prefab");

            // Enable two-sided rendering on house materials so interior/underside is solid
            string[] houseMatPaths = new string[]
            {
                "Assets/Materials/Environment/Houses/M_StiltedHouse_Thatched.mat",
                "Assets/Materials/Environment/Houses/M_StiltedHouse_Wood.mat",
                "Assets/Materials/NhaSanGo.mat"
            };
            foreach (var mp in houseMatPaths)
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(mp);
                if (m != null)
                {
                    m.SetFloat("_Cull", 0f); // Two-Sided
                    EditorUtility.SetDirty(m);
                }
            }

            // 1. House An (Nhà An)
            var areaNhaAn = GameObject.Find("CaMau_1954_Village/Village_Areas/01_Nha_An");
            if (thatchedPrefab != null && areaNhaAn != null)
            {
                var oldNhaAn = areaNhaAn.transform.Find("House_An_StiltedHouse_3D");
                if (oldNhaAn != null) Object.DestroyImmediate(oldNhaAn.gameObject);

                var houseAn = (GameObject)PrefabUtility.InstantiatePrefab(thatchedPrefab, areaNhaAn.transform);
                houseAn.name = "House_An_StiltedHouse_3D";
                houseAn.transform.position = new Vector3(0.50f, 4.00f, 19.50f);
                houseAn.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                houseAn.transform.localScale = new Vector3(4.0f, 4.0f, 4.0f);
                EditorUtility.SetDirty(houseAn);
            }

            // 2. Neighbor House near An
            if (thatchedPrefab != null && areaNhaAn != null)
            {
                var oldNeighbor = areaNhaAn.transform.Find("Nha_HangXom_NhaAn");
                if (oldNeighbor != null) Object.DestroyImmediate(oldNeighbor.gameObject);

                var neighbor = (GameObject)PrefabUtility.InstantiatePrefab(thatchedPrefab, areaNhaAn.transform);
                neighbor.name = "Nha_HangXom_NhaAn";
                neighbor.transform.position = new Vector3(-7.50f, 4.00f, 19.50f);
                neighbor.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                neighbor.transform.localScale = new Vector3(3.8f, 3.8f, 3.8f);
                EditorUtility.SetDirty(neighbor);
            }

            // 3. Uncle Ba's Wooden House
            var areaKhuLayNuoc = GameObject.Find("CaMau_1954_Village/Village_Areas/02_Khu_Lay_Nuoc");
            if (woodPrefab != null && areaKhuLayNuoc != null)
            {
                var oldUncleBa = areaKhuLayNuoc.transform.Find("Nha_BacBa_KhuLayNuoc");
                if (oldUncleBa != null) Object.DestroyImmediate(oldUncleBa.gameObject);

                var houseUncleBa = (GameObject)PrefabUtility.InstantiatePrefab(woodPrefab, areaKhuLayNuoc.transform);
                houseUncleBa.name = "Nha_BacBa_KhuLayNuoc";
                houseUncleBa.transform.position = new Vector3(12.50f, 3.08f, 14.50f);
                houseUncleBa.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                houseUncleBa.transform.localScale = new Vector3(320f, 320f, 320f);
                EditorUtility.SetDirty(houseUncleBa);
            }

            // 4. Bac Hai Orchard House (Thatched)
            var areaVuonCay = GameObject.Find("CaMau_1954_Village/Village_Areas/05_Vuon_Cay");
            if (thatchedPrefab != null && areaVuonCay != null)
            {
                var oldBacHai = areaVuonCay.transform.Find("Nha_XomVuon_BacHai");
                if (oldBacHai != null) Object.DestroyImmediate(oldBacHai.gameObject);

                var houseBacHai = (GameObject)PrefabUtility.InstantiatePrefab(thatchedPrefab, areaVuonCay.transform);
                houseBacHai.name = "Nha_XomVuon_BacHai";
                houseBacHai.transform.position = new Vector3(-19.50f, 4.00f, 23.50f);
                houseBacHai.transform.rotation = Quaternion.Euler(0f, 170f, 0f);
                houseBacHai.transform.localScale = new Vector3(3.8f, 3.8f, 3.8f);
                EditorUtility.SetDirty(houseBacHai);
            }

            // 5. Orchard Wooden House
            if (nhaSanGoPrefab != null && areaVuonCay != null)
            {
                var oldGo = areaVuonCay.transform.Find("Nha_XomVuon_Go");
                if (oldGo != null) Object.DestroyImmediate(oldGo.gameObject);

                var houseGo = (GameObject)PrefabUtility.InstantiatePrefab(nhaSanGoPrefab, areaVuonCay.transform);
                houseGo.name = "Nha_XomVuon_Go";
                houseGo.transform.position = new Vector3(-25.50f, 3.08f, 23.50f);
                houseGo.transform.rotation = Quaternion.Euler(0f, 150f, 0f);
                houseGo.transform.localScale = new Vector3(320f, 320f, 320f);
                EditorUtility.SetDirty(houseGo);
            }

            // 6. Farmer House in Rice Fields
            var areaCanhDong = GameObject.Find("CaMau_1954_Village/Village_Areas/06_Canh_Dong");
            if (thatchedPrefab != null && areaCanhDong != null)
            {
                var oldNongDan = areaCanhDong.transform.Find("Nha_NongDan_CanhDong");
                if (oldNongDan != null) Object.DestroyImmediate(oldNongDan.gameObject);

                var houseNongDan = (GameObject)PrefabUtility.InstantiatePrefab(thatchedPrefab, areaCanhDong.transform);
                houseNongDan.name = "Nha_NongDan_CanhDong";
                houseNongDan.transform.position = new Vector3(26.00f, 3.60f, 34.00f);
                houseNongDan.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                houseNongDan.transform.localScale = new Vector3(3.6f, 3.6f, 3.6f);
                EditorUtility.SetDirty(houseNongDan);
            }
        }

        private static void SetupRiceFieldPlotsAndWater()
        {
            var areaCanhDong = GameObject.Find("CaMau_1954_Village/Village_Areas/06_Canh_Dong");
            if (areaCanhDong == null) return;

            Material grassMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_CaMau_Landscape_Grass.mat");
            Material mudMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Terrain/M_CaMau_RiverMud.mat");
            Material waterMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_RiverWater_Canh1.mat");

            // 1. All dikes/paths in rice fields have the green color of the map (M_CaMau_Landscape_Grass)
            string[] dikeNames = new string[]
            {
                "Dike_Main_NorthSouth",
                "Dike_Lateral_West",
                "Dike_Lateral_East",
                "Dike_Cross_01",
                "Dike_Cross_02",
                "Dike_Cross_03",
                "Dike_Main_EastWest",
                "Dike_Cross_NorthSouth"
            };

            foreach (var dName in dikeNames)
            {
                var dike = GameObject.Find(dName);
                if (dike != null && grassMat != null)
                {
                    var mr = dike.GetComponent<MeshRenderer>();
                    if (mr != null) mr.sharedMaterial = grassMat;
                    EditorUtility.SetDirty(dike);
                }
            }

            // Also Grand Spine transition path in rice fields gets green grass borders
            var grandSpine = GameObject.Find("Path_RiceField_Transition_GrandSpine");
            if (grandSpine != null && grassMat != null)
            {
                var mr = grandSpine.GetComponent<MeshRenderer>();
                if (mr != null) mr.sharedMaterial = grassMat;
                EditorUtility.SetDirty(grandSpine);
            }

            // 2. Create distinct mud paddy field plots (thửa ruộng màu đất) and water pool inside each plot (1 khu nước nhỏ)
            var paddyGroup = areaCanhDong.transform.Find("Paddy_Plots_With_Water");
            if (paddyGroup != null) Object.DestroyImmediate(paddyGroup.gameObject);

            paddyGroup = new GameObject("Paddy_Plots_With_Water").transform;
            paddyGroup.SetParent(areaCanhDong.transform, false);

            // Define the 12 main rectangular paddy plots across the field
            // X ranges: Plot 1 [-38, -23], Plot 2 [-21, -2], Plot 3 [2, 21], Plot 4 [23, 38]
            // Z ranges: Tier 1 [27, 35], Tier 2 [37, 51], Tier 3 [53, 67]
            float[] xCenters = new float[] { -30.5f, -11.5f, 11.5f, 30.5f };
            float[] xWidths = new float[] { 14.0f, 18.0f, 18.0f, 14.0f };

            float[] zCenters = new float[] { 31.0f, 44.0f, 60.0f };
            float[] zDepths = new float[] { 7.5f, 13.0f, 13.0f };

            int plotId = 0;
            for (int r = 0; r < zCenters.Length; r++)
            {
                for (int c = 0; c < xCenters.Length; c++)
                {
                    plotId++;
                    float xc = xCenters[c];
                    float xw = xWidths[c];
                    float zc = zCenters[r];
                    float zd = zDepths[r];

                    // Mud field plot (Thửa ruộng màu đất)
                    var plotObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    plotObj.name = $"ThuaRuong_Dat_P{plotId:D2}";
                    plotObj.transform.SetParent(paddyGroup, false);
                    plotObj.transform.position = new Vector3(xc, 0.005f, zc);
                    plotObj.transform.localScale = new Vector3(xw - 1.2f, 0.01f, zd - 1.2f);
                    if (mudMat != null) plotObj.GetComponent<MeshRenderer>().sharedMaterial = mudMat;
                    // Disable collider on decorative mud surface so it doesn't interfere with walking
                    var colPlot = plotObj.GetComponent<Collider>();
                    if (colPlot != null) colPlot.enabled = false;

                    // Water pool inside this plot (1 khu nước nhỏ phản chiếu mây trời)
                    var waterObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    waterObj.name = $"KhuNuoc_Ruong_P{plotId:D2}";
                    waterObj.transform.SetParent(paddyGroup, false);
                    // Pool occupies central 60% of the plot area
                    waterObj.transform.position = new Vector3(xc, 0.012f, zc);
                    waterObj.transform.localScale = new Vector3((xw - 1.2f) * 0.65f, 0.01f, (zd - 1.2f) * 0.65f);
                    if (waterMat != null) waterObj.GetComponent<MeshRenderer>().sharedMaterial = waterMat;
                    var colWater = waterObj.GetComponent<Collider>();
                    if (colWater != null) colWater.enabled = false;
                }
            }
        }

        private static void SetupVillagePathsAndGround()
        {
            Material trailMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Paths/M_Earthen_Trail.mat");
            Material grassMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_CaMau_Landscape_Grass.mat");

            // 1. Village ground: all green grass
            var terrainGroup = GameObject.Find("CaMau_1954_Village/Environment/Terrain_3D");
            if (terrainGroup != null && grassMat != null)
            {
                string[] baseGrounds = new string[]
                {
                    "Terrain_Base_Village_West",
                    "Terrain_Base_Paddies_East",
                    "Terrain_Base_North_Expansive"
                };
                foreach (var bg in baseGrounds)
                {
                    var go = terrainGroup.transform.Find(bg);
                    if (go != null)
                    {
                        var mr = go.GetComponent<MeshRenderer>();
                        if (mr != null) mr.sharedMaterial = grassMat;
                        EditorUtility.SetDirty(go);
                    }
                }
            }

            // 2. Village paths (Paths_3D_Network) get brown earth trail (M_Earthen_Trail)
            var pathsGroup = GameObject.Find("CaMau_1954_Village/Environment/Paths_3D_Network");
            if (pathsGroup != null && trailMat != null)
            {
                foreach (Transform path in pathsGroup.transform)
                {
                    // Don't modify CauVan bridge structure itself
                    if (path.name.Contains("CauVan")) continue;

                    var mr = path.GetComponent<MeshRenderer>();
                    if (mr != null)
                    {
                        mr.sharedMaterial = trailMat;
                        EditorUtility.SetDirty(path);
                    }
                }
            }
        }

        private static void SetupBackgroundsOnlyAtRiceField()
        {
            // Only northern horizon behind rice fields has mountain backdrop. Rest is pure sky.
            var bgGroup = GameObject.Find("CaMau_1954_Village/Environment/09_Natural_Painterly_Background");
            if (bgGroup != null)
            {
                // North mountain backdrop behind rice fields: KEEP ACTIVE & POSITION CLEANLY
                var bgMtn = bgGroup.transform.Find("BG_Layer_01_MistyMountains_Panorama");
                if (bgMtn != null)
                {
                    bgMtn.gameObject.SetActive(true);
                    bgMtn.position = new Vector3(0f, 9.0f, 84.0f);
                    bgMtn.localScale = new Vector3(260f, 24f, 1f);
                    EditorUtility.SetDirty(bgMtn);
                }

                // Clouds behind rice fields: KEEP
                var bgClouds = bgGroup.transform.Find("BG_Layer_03_GoldenClouds_Center");
                if (bgClouds != null)
                {
                    bgClouds.gameObject.SetActive(true);
                    bgClouds.position = new Vector3(0f, 22.0f, 87.0f);
                    EditorUtility.SetDirty(bgClouds);
                }

                var bgCloudsTop = bgGroup.transform.Find("BG_Layer_03_DistantClouds_Top");
                if (bgCloudsTop != null)
                {
                    bgCloudsTop.gameObject.SetActive(true);
                }

                // Remove / deactivate East and West karst peak cutouts so sky is pure
                var bgWest = bgGroup.transform.Find("BG_Layer_02_KarstPeaks_West");
                if (bgWest != null) bgWest.gameObject.SetActive(false);

                var bgEast = bgGroup.transform.Find("BG_Layer_02_KarstPeaks_East");
                if (bgEast != null) bgEast.gameObject.SetActive(false);
            }

            // Remove all card vistas in Background_Atmosphere (South sea, East, West)
            var atmoGroup = GameObject.Find("CaMau_1954_Village/Environment/Background_Atmosphere");
            if (atmoGroup != null)
            {
                string[] vistasToRemove = new string[]
                {
                    "Backdrop_Vista_South",
                    "Backdrop_Vista_East",
                    "Backdrop_Vista_West",
                    "Backdrop_Vista_North"
                };
                foreach (var v in vistasToRemove)
                {
                    var t = atmoGroup.transform.Find(v);
                    if (t != null) t.gameObject.SetActive(false);
                }
            }
        }

        private static void FixCharactersGroundAlignment()
        {
            // Set all NPCs and An Player firmly on the ground at Y = 0.00m (or pier level)
            var player = GameObject.Find("An_Player");
            if (player != null)
            {
                player.transform.position = new Vector3(player.transform.position.x, 0.00f, player.transform.position.z);
                EditorUtility.SetDirty(player);
            }

            var npcs = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            foreach (var go in npcs)
            {
                if (go.name.StartsWith("NPC_"))
                {
                    Vector3 pos = go.transform.position;
                    // If NPC is on the wharf, set to pier top Y = 0.02m; otherwise ground Y = 0.00m
                    float targetY = (pos.z < -10f) ? 0.02f : 0.00f;
                    go.transform.position = new Vector3(pos.x, targetY, pos.z);
                    EditorUtility.SetDirty(go);
                }
            }
        }

        private static void RemoveInvisibleBlockers()
        {
            // Remove known invisible obstacles that blocked player movement
            string[] blockerNames = new string[]
            {
                "Haystack_RoadJunction",
                "CargoCrate_Pier_04",
                "RiceSackStack_SanLang_East",
                "BushTree_SanLang_East",
                "BushTree_SanLang_West",
                "Sampan_Covered_River_West",
                "Sampan_BaLa_Pier_Outer"
            };

            foreach (var bName in blockerNames)
            {
                var go = GameObject.Find(bName);
                if (go != null)
                {
                    Debug.Log($"[Overhaul] Removing invisible blocker: {bName}");
                    Object.DestroyImmediate(go);
                }
            }
        }

        private static void RedesignExpandedWharf()
        {
            var benXuong = GameObject.Find("CaMau_1954_Village/Village_Areas/08_Ben_Xuong");
            if (benXuong == null) return;

            Material woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Structures/M_Wood_Weathered.mat");
            if (woodMat == null)
            {
                woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Houses/M_CaMau_TimberDark.mat");
            }

            // 1. Grand Expanded Pier Platform (Bến thuyền to hơn, cách xa làng hơn)
            // Center moved further south to Z = -18.5m, size 16m wide x 9m deep
            var pier = benXuong.transform.Find("WalkablePier_Wooden");
            if (pier == null)
            {
                pier = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
                pier.name = "WalkablePier_Wooden";
                pier.SetParent(benXuong.transform, false);
            }
            pier.position = new Vector3(5.50f, -0.10f, -18.50f);
            pier.localScale = new Vector3(16.00f, 0.25f, 9.00f);
            if (woodMat != null && pier.GetComponent<MeshRenderer>() != null)
            {
                pier.GetComponent<MeshRenderer>().sharedMaterial = woodMat;
            }
            EditorUtility.SetDirty(pier);

            // 2. Connecting Boardwalk Pier (Lối dẫn từ bờ sông làng ra bến thuyền)
            var boardwalk = benXuong.transform.Find("Boardwalk_Village_To_Wharf");
            if (boardwalk == null)
            {
                boardwalk = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
                boardwalk.name = "Boardwalk_Village_To_Wharf";
                boardwalk.SetParent(benXuong.transform, false);
            }
            // Extends from village edge Z = -8.0m to wharf edge Z = -14.0m (length 6m, center Z = -11.0m)
            boardwalk.position = new Vector3(5.50f, -0.10f, -11.50f);
            boardwalk.localScale = new Vector3(4.00f, 0.25f, 7.00f);
            if (woodMat != null && boardwalk.GetComponent<MeshRenderer>() != null)
            {
                boardwalk.GetComponent<MeshRenderer>().sharedMaterial = woodMat;
            }
            EditorUtility.SetDirty(boardwalk);

            // 3. Waiting Shelter on the wharf
            var shelter = benXuong.transform.Find("Dock_WaitingShelter_3D");
            if (shelter != null)
            {
                shelter.position = new Vector3(1.50f, 0.02f, -17.50f);
                EditorUtility.SetDirty(shelter);
            }

            // 4. Ferryman Uncle Bay at the outer boat landing edge
            var uncleBay = GameObject.Find("NPC_Ferryman_UncleBay");
            if (uncleBay != null)
            {
                uncleBay.transform.position = new Vector3(5.50f, 0.02f, -22.20f);
                EditorUtility.SetDirty(uncleBay);
            }

            // 5. Waving Mother & Farewell Elder on the wharf
            var wavingMother = GameObject.Find("NPC_Villager_Waving_Mother");
            if (wavingMother != null)
            {
                wavingMother.transform.position = new Vector3(2.50f, 0.02f, -16.00f);
                EditorUtility.SetDirty(wavingMother);
            }

            var farewellElder = GameObject.Find("NPC_Villager_Farewell_Elder");
            if (farewellElder != null)
            {
                farewellElder.transform.position = new Vector3(9.50f, 0.02f, -16.00f);
                EditorUtility.SetDirty(farewellElder);
            }

            // 6. Sampans moored along the outer boat landing
            var sampan1 = benXuong.transform.Find("Sampan_BaLa_3D");
            if (sampan1 != null)
            {
                sampan1.position = new Vector3(5.50f, -0.45f, -24.00f);
                EditorUtility.SetDirty(sampan1);
            }
        }
    }
}
