using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace HoaDo.Editor
{
    public static class FixCanalWaterTerrainAndBridgePassage
    {
        [MenuItem("Tools/Hoa Do/Fix Canal Water, Terrain & Bridge Passage")]
        public static void ExecuteFix()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (!activeScene.path.Contains("CaMau_1954_Village"))
            {
                Debug.LogWarning("[FixCanalWater] Active scene is not CaMau_1954_Village. Opening scene...");
                EditorSceneManager.OpenScene("Assets/Scenes/CaMau_1954_Village.unity");
            }

            Debug.Log("[FixCanalWater] Starting Canal Water, Terrain & Bridge Passage Fix...");

            FixTerrainSplitsAndCanalBed();
            FixWaterMaterialsAndPlanes();
            FixBridgeAndApproaches();
            FixSampanPositions();
            AlignBackgroundLayers();
            VerifyHouseModelsAndStilts();

            Physics.SyncTransforms();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("[FixCanalWater] All fixes completed and scene saved successfully!");
        }

        private static void FixTerrainSplitsAndCanalBed()
        {
            var terrainGroup = GameObject.Find("CaMau_1954_Village/Environment/Terrain_3D");
            if (terrainGroup == null)
            {
                Debug.LogError("[FixCanalWater] Terrain_3D group not found!");
                return;
            }

            Material grassMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Terrain/M_CaMau_Landscape_Grass.mat");
            if (grassMat == null)
            {
                grassMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Terrain/M_Terrain_WetMud.mat");
            }
            Material mudMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Terrain/M_CaMau_RiverMud.mat");

            // 1. Remove or retarget the old monolithic Terrain_Base_Expansive
            var oldBase = terrainGroup.transform.Find("Terrain_Base_Expansive");
            if (oldBase != null)
            {
                oldBase.name = "Terrain_Base_North_Expansive";
                oldBase.position = new Vector3(0f, -1.0f, 60.5f);
                oldBase.localScale = new Vector3(130f, 2.0f, 49f); // X [-65, 65], Z [36, 85]
                EditorUtility.SetDirty(oldBase);
            }

            // 2. Ensure Terrain_Base_Village_West exists (X from -65 to +14.50, Z from -8 to 36)
            var westBaseT = terrainGroup.transform.Find("Terrain_Base_Village_West");
            GameObject westBase = westBaseT != null ? westBaseT.gameObject : null;
            if (westBase == null)
            {
                westBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
                westBase.name = "Terrain_Base_Village_West";
                westBase.transform.SetParent(terrainGroup.transform, false);
            }
            // Width = 14.50 - (-65) = 79.5m, Center X = (-65 + 14.50)/2 = -25.25m
            // Depth = 36 - (-8) = 44m, Center Z = (-8 + 36)/2 = 14.0m
            westBase.transform.position = new Vector3(-25.25f, -1.0f, 14.0f);
            westBase.transform.localScale = new Vector3(79.5f, 2.0f, 44.0f);
            if (grassMat != null && westBase.GetComponent<MeshRenderer>() != null)
            {
                westBase.GetComponent<MeshRenderer>().sharedMaterial = grassMat;
            }
            EditorUtility.SetDirty(westBase);

            // 3. Ensure Terrain_Base_Paddies_East exists (X from +20.50 to +65, Z from -8 to 36)
            var eastBaseT = terrainGroup.transform.Find("Terrain_Base_Paddies_East");
            GameObject eastBase = eastBaseT != null ? eastBaseT.gameObject : null;
            if (eastBase == null)
            {
                eastBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
                eastBase.name = "Terrain_Base_Paddies_East";
                eastBase.transform.SetParent(terrainGroup.transform, false);
            }
            // Width = 65 - 20.50 = 44.5m, Center X = (20.50 + 65)/2 = 42.75m
            // Depth = 44m, Center Z = 14.0m
            eastBase.transform.position = new Vector3(42.75f, -1.0f, 14.0f);
            eastBase.transform.localScale = new Vector3(44.5f, 2.0f, 44.0f);
            if (grassMat != null && eastBase.GetComponent<MeshRenderer>() != null)
            {
                eastBase.GetComponent<MeshRenderer>().sharedMaterial = grassMat;
            }
            EditorUtility.SetDirty(eastBase);

            // 4. Adjust Terrain_SanLang_Main so it ends cleanly at X = 14.50
            var sanLangKnoll = terrainGroup.transform.Find("Terrain_SanLang_Main");
            if (sanLangKnoll != null)
            {
                // X from -16.0 to 14.50 (width = 30.5m, center = -0.75m)
                sanLangKnoll.position = new Vector3(-0.75f, -1.0f, 4.0f);
                sanLangKnoll.localScale = new Vector3(30.5f, 2.0f, 24.0f);
                EditorUtility.SetDirty(sanLangKnoll);
            }

            // 5. Adjust Terrain_KhuLayNuoc_Knoll so it ends cleanly at X = 14.50
            var khuLayNuocKnoll = terrainGroup.transform.Find("Terrain_KhuLayNuoc_Knoll");
            if (khuLayNuocKnoll != null)
            {
                // X from 7.0 to 14.50 (width = 7.5m, center = 10.75m)
                khuLayNuocKnoll.position = new Vector3(10.75f, -1.0f, 6.0f);
                khuLayNuocKnoll.localScale = new Vector3(7.5f, 2.0f, 18.0f);
                EditorUtility.SetDirty(khuLayNuocKnoll);
            }

            // 6. Adjust Terrain_CanhDong_Paddies so it starts cleanly at X = 20.50
            var paddiesKnoll = terrainGroup.transform.Find("Terrain_CanhDong_Paddies");
            if (paddiesKnoll != null)
            {
                // X from 20.50 to 41.00 (width = 20.5m, center = 30.75m)
                paddiesKnoll.position = new Vector3(30.75f, -1.05f, 14.0f);
                paddiesKnoll.localScale = new Vector3(20.5f, 2.0f, 34.0f);
                EditorUtility.SetDirty(paddiesKnoll);
            }

            // 7. Adjust Dike_Main_EastWest so it starts at X = 20.50 (east of bridge) and extends to X = 40.00
            var dikeEastWest = terrainGroup.transform.Find("Dike_Main_EastWest");
            if (dikeEastWest != null)
            {
                dikeEastWest.position = new Vector3(30.25f, 0.08f, 10.0f);
                dikeEastWest.localScale = new Vector3(19.5f, 0.25f, 2.0f);
                EditorUtility.SetDirty(dikeEastWest);
            }

            // 8. Shape the CanalBed_Mud cleanly along the canal channel (X=17.50, width=7.2m, length=48m)
            var canalBed = terrainGroup.transform.Find("CanalBed_Mud");
            if (canalBed != null)
            {
                canalBed.position = new Vector3(17.50f, -1.65f, 14.0f);
                canalBed.localScale = new Vector3(7.2f, 1.5f, 48.0f); // Top face at Y = -1.65 + 0.75 = -0.90m
                if (mudMat != null && canalBed.GetComponent<MeshRenderer>() != null)
                {
                    canalBed.GetComponent<MeshRenderer>().sharedMaterial = mudMat;
                }
                EditorUtility.SetDirty(canalBed);
            }

            // 9. Clean up obsolete leftover ramps from ArchedWoodenBridge_3D at Z = -2.0m
            var archedBridge = GameObject.Find("ArchedWoodenBridge_3D");
            if (archedBridge != null)
            {
                var ramps = archedBridge.transform.Find("Bridge_Walkable_Ramps");
                if (ramps != null)
                {
                    Object.DestroyImmediate(ramps.gameObject);
                }
            }

            // 10. Ensure Southern Riverbed is low and expansive
            var riverBed = terrainGroup.transform.Find("RiverBed_South_Mud");
            if (riverBed != null)
            {
                riverBed.position = new Vector3(0f, -6.0f, -60f);
                riverBed.localScale = new Vector3(250f, 2.0f, 120f);
                EditorUtility.SetDirty(riverBed);
            }
        }

        private static void FixWaterMaterialsAndPlanes()
        {
            var waterGroup = GameObject.Find("CaMau_1954_Village/Environment/WaterSystem_3D");
            if (waterGroup == null)
            {
                Debug.LogError("[FixCanalWater] WaterSystem_3D not found!");
                return;
            }

            // Load the canonical project river water material: M_RiverWater_Canh1.mat
            Material riverWaterMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_RiverWater_Canh1.mat");
            if (riverWaterMat == null)
            {
                riverWaterMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Water/M_Test3D_Water.mat");
            }

            // 1. East Canal Water Plane (flows from southern sea Z = -8m up to northern paddies Z = 38m)
            var eastCanal = waterGroup.transform.Find("Water_East_Canal");
            if (eastCanal != null)
            {
                // Width = 6.6m (X from 14.20 to 20.80), Length = 48m (Z from -9 to 39)
                // In Unity Plane, scale 1.0 = 10m x 10m
                eastCanal.position = new Vector3(17.50f, -0.45f, 15.0f);
                eastCanal.localScale = new Vector3(0.66f, 1.0f, 4.80f);
                eastCanal.rotation = Quaternion.identity;
                if (riverWaterMat != null && eastCanal.GetComponent<MeshRenderer>() != null)
                {
                    eastCanal.GetComponent<MeshRenderer>().sharedMaterial = riverWaterMat;
                }
                EditorUtility.SetDirty(eastCanal);
            }

            // 2. Wharf Water Plane (connects the river edge under the pier to the expansive sea)
            var wharfWater = waterGroup.transform.Find("Water_BoSong_Wharf");
            if (wharfWater != null)
            {
                wharfWater.position = new Vector3(-6.0f, -0.45f, -9.5f);
                wharfWater.localScale = new Vector3(2.5f, 1.0f, 0.8f);
                wharfWater.rotation = Quaternion.identity;
                if (riverWaterMat != null && wharfWater.GetComponent<MeshRenderer>() != null)
                {
                    wharfWater.GetComponent<MeshRenderer>().sharedMaterial = riverWaterMat;
                }
                EditorUtility.SetDirty(wharfWater);
            }

            // 3. Expansive Southern Sea (covers Z from -8m out to -168m, X from -160m to 160m)
            var southSea = waterGroup.transform.Find("Water_South_Sea_Expansive");
            if (southSea != null)
            {
                southSea.position = new Vector3(0f, -0.48f, -88.0f);
                southSea.localScale = new Vector3(32.0f, 1.0f, 16.0f);
                southSea.rotation = Quaternion.identity;
                if (riverWaterMat != null && southSea.GetComponent<MeshRenderer>() != null)
                {
                    southSea.GetComponent<MeshRenderer>().sharedMaterial = riverWaterMat;
                }
                EditorUtility.SetDirty(southSea);
            }

            // 4. West Creek Water Plane
            var westCreek = waterGroup.transform.Find("Water_West_Creek");
            if (westCreek != null)
            {
                westCreek.position = new Vector3(-25.0f, -0.45f, -6.0f);
                westCreek.localScale = new Vector3(1.5f, 1.0f, 2.8f);
                westCreek.rotation = Quaternion.identity;
                if (riverWaterMat != null && westCreek.GetComponent<MeshRenderer>() != null)
                {
                    westCreek.GetComponent<MeshRenderer>().sharedMaterial = riverWaterMat;
                }
                EditorUtility.SetDirty(westCreek);
            }
        }

        private static void FixBridgeAndApproaches()
        {
            var bridgeGroup = GameObject.Find("CaMau_1954_Village/Environment/Paths_3D_Network/CauVan_Kenh_CanhDong");
            if (bridgeGroup == null)
            {
                Debug.LogWarning("[FixCanalWater] CauVan_Kenh_CanhDong not found!");
                return;
            }

            // The canal runs along X = 17.50m (from X = 14.50 to X = 20.50).
            // Bridge is at Z = 10.0m. Planks span from X = 14.50 to 20.50 at Y = 0.04m.
            var planks = bridgeGroup.transform.Find("Bridge_Planks");
            if (planks != null)
            {
                planks.position = new Vector3(17.50f, 0.04f, 10.00f);
                planks.localScale = new Vector3(5.40f, 0.08f, 2.00f);
                EditorUtility.SetDirty(planks);
            }

            // West ramp connecting ground (X=14.50, Y=0.015m) to bridge planks (Y=0.04m)
            var westRamp = bridgeGroup.transform.Find("Bridge_Plank_Ramp_West");
            if (westRamp != null)
            {
                westRamp.position = new Vector3(14.65f, 0.025f, 10.00f);
                westRamp.localScale = new Vector3(0.60f, 0.05f, 2.00f);
                EditorUtility.SetDirty(westRamp);
            }

            // East ramp connecting bridge planks (Y=0.04m) to dike ground (X=20.50, Y=0.015m)
            var eastRamp = bridgeGroup.transform.Find("Bridge_Plank_Ramp_East");
            if (eastRamp != null)
            {
                eastRamp.position = new Vector3(20.35f, 0.025f, 10.00f);
                eastRamp.localScale = new Vector3(0.60f, 0.05f, 2.00f);
                EditorUtility.SetDirty(eastRamp);
            }

            // Path approach on West bank (firmly planted on dry ground at Y = 0.015m)
            var pathsNetwork = GameObject.Find("CaMau_1954_Village/Environment/Paths_3D_Network");
            if (pathsNetwork != null)
            {
                var westApproach = pathsNetwork.transform.Find("Path_Canal_Approach_West");
                if (westApproach != null)
                {
                    westApproach.position = new Vector3(13.75f, 0.015f, 10.00f);
                    westApproach.localScale = new Vector3(1.50f, 0.03f, 2.00f);
                    EditorUtility.SetDirty(westApproach);
                }

                var eastApproach = pathsNetwork.transform.Find("Path_Canal_Approach_East");
                if (eastApproach != null)
                {
                    eastApproach.position = new Vector3(21.25f, 0.015f, 10.00f);
                    eastApproach.localScale = new Vector3(1.50f, 0.03f, 2.00f);
                    EditorUtility.SetDirty(eastApproach);
                }
            }

            // Adjust handrails so they provide a generous 2.0m clearance
            var northRail = bridgeGroup.transform.Find("Handrail_North");
            if (northRail != null)
            {
                northRail.position = new Vector3(17.50f, 0.55f, 11.00f);
                northRail.localScale = new Vector3(5.60f, 0.08f, 0.08f);
                EditorUtility.SetDirty(northRail);
            }

            var southRail = bridgeGroup.transform.Find("Handrail_South");
            if (southRail != null)
            {
                southRail.position = new Vector3(17.50f, 0.55f, 9.00f);
                southRail.localScale = new Vector3(5.60f, 0.08f, 0.08f);
                EditorUtility.SetDirty(southRail);
            }
        }

        private static void FixSampanPositions()
        {
            // Move Sampan_Covered_Canal_North out from inside the bridge to a natural mooring post at Z = 14.8m
            var sampanNorth = GameObject.Find("Sampan_Covered_Canal_North");
            if (sampanNorth != null)
            {
                sampanNorth.transform.position = new Vector3(17.50f, -0.42f, 14.80f);
                sampanNorth.transform.rotation = Quaternion.Euler(0f, 10f, 0f);
                EditorUtility.SetDirty(sampanNorth);
            }
        }

        private static void AlignBackgroundLayers()
        {
            var bgGroup = GameObject.Find("CaMau_1954_Village/Environment/09_Natural_Painterly_Background");
            if (bgGroup == null) return;

            // 1. Panoramic misty mountain backdrop: nest its base naturally along the northern horizon (Z = 83m, Y = 7.5m)
            var bgMtn = bgGroup.transform.Find("BG_Layer_01_MistyMountains_Panorama");
            if (bgMtn != null)
            {
                bgMtn.position = new Vector3(0f, 8.5f, 83.0f);
                bgMtn.localScale = new Vector3(260f, 24f, 1f);
                var mr = bgMtn.GetComponent<MeshRenderer>();
                if (mr != null && mr.sharedMaterial != null)
                {
                    mr.sharedMaterial.color = new Color(0.96f, 0.93f, 0.88f, 0.85f);
                }
                EditorUtility.SetDirty(bgMtn);
            }

            // 2. Distant karst peak flanks
            var bgWest = bgGroup.transform.Find("BG_Layer_02_KarstPeaks_West");
            if (bgWest != null)
            {
                bgWest.position = new Vector3(-50f, 7.5f, 85.0f);
                bgWest.localScale = new Vector3(120f, 20f, 1f);
                var mr = bgWest.GetComponent<MeshRenderer>();
                if (mr != null && mr.sharedMaterial != null)
                {
                    mr.sharedMaterial.color = new Color(0.93f, 0.90f, 0.87f, 0.75f);
                }
                EditorUtility.SetDirty(bgWest);
            }

            var bgEast = bgGroup.transform.Find("BG_Layer_02_KarstPeaks_East");
            if (bgEast != null)
            {
                bgEast.position = new Vector3(50f, 7.5f, 85.0f);
                bgEast.localScale = new Vector3(120f, 20f, 1f);
                var mr = bgEast.GetComponent<MeshRenderer>();
                if (mr != null && mr.sharedMaterial != null)
                {
                    mr.sharedMaterial.color = new Color(0.93f, 0.90f, 0.87f, 0.75f);
                }
                EditorUtility.SetDirty(bgEast);
            }

            // 3. Golden sunset clouds
            var bgClouds = bgGroup.transform.Find("BG_Layer_03_GoldenClouds_Center");
            if (bgClouds != null)
            {
                bgClouds.position = new Vector3(0f, 22.0f, 87.0f);
                bgClouds.localScale = new Vector3(160f, 18f, 1f);
                var mr = bgClouds.GetComponent<MeshRenderer>();
                if (mr != null && mr.sharedMaterial != null)
                {
                    mr.sharedMaterial.color = new Color(1.0f, 0.95f, 0.86f, 0.65f);
                }
                EditorUtility.SetDirty(bgClouds);
            }
        }

        private static void VerifyHouseModelsAndStilts()
        {
            // Ensure Uncle Ba's wooden house and veranda stairs touch the ground cleanly at Y = 3.08m
            var uncleBaHouse = GameObject.Find("Nha_BacBa_KhuLayNuoc");
            if (uncleBaHouse != null)
            {
                uncleBaHouse.transform.position = new Vector3(12.50f, 3.08f, 14.50f);
                uncleBaHouse.transform.localScale = new Vector3(320f, 320f, 320f);
                EditorUtility.SetDirty(uncleBaHouse);
            }

            var orchardWoodHouse = GameObject.Find("Nha_XomVuon_Go");
            if (orchardWoodHouse != null)
            {
                orchardWoodHouse.transform.position = new Vector3(-25.50f, 3.08f, 23.50f);
                orchardWoodHouse.transform.localScale = new Vector3(320f, 320f, 320f);
                EditorUtility.SetDirty(orchardWoodHouse);
            }
        }
    }
}
