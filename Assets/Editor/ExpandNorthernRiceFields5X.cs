using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using HoaVaMau.Gameplay;

namespace HoaDo.Editor
{
    /// <summary>
    /// Expands the northern rice fields 5X towards the village gate.
    /// Extends the unified ground base from Z=86m up to Z=206m (+120m new fertile land),
    /// relocates Gate_Village_North from Z=73.5m to Z=184.0m (+110.5m),
    /// extends the central road to Z=188.5m,
    /// constructs 16 new lush & golden rice paddy plots (Tiers 2, 3, 4, 5) with mud dikes,
    /// swaying rice stalks, traditional scarecrows, and irrigation pools,
    /// and extends the flanking mountain ranges & coconut groves up to Z=200m.
    /// </summary>
    public static class ExpandNorthernRiceFields5X
    {
        private const float UNIFORM_GROUND_Y = 0.50f;
        private const float Y_PLOT = 0.51f;
        private const float NEW_GATE_Z = 184.0f;
        private const float OLD_GATE_Z = 73.5f;

        [MenuItem("Tools/Hoa Do/Expand Northern Rice Fields 5X To Village Gate")]
        public static void ExecuteExpansion()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (!activeScene.name.Equals("CaMau_1954_Village"))
            {
                EditorSceneManager.OpenScene("Assets/Scenes/CaMau_1954_Village.unity");
            }

            Debug.Log("<color=#4CAF50>[RiceExpansion5X]</color> Starting 5X rice field expansion towards Village Gate...");

            // 1. Resolve Materials & Prefabs
            var matRiceGolden = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/LanaOverhaul/M_RiceCrop_GoldenRipe.mat");
            var matRiceGreen  = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/LanaOverhaul/M_RiceCrop_LushGreen.mat");
            var matWater      = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_RiverWater_Canh1.mat");
            
            // Sample existing dike material and pole/hat materials from scene if available
            Material matDike = null;
            var sampleDike = GameObject.Find("Paddy_Plot_01_Golden_DikeN");
            if (sampleDike != null) matDike = sampleDike.GetComponent<MeshRenderer>()?.sharedMaterial;
            if (matDike == null) matDike = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/LanaOverhaul/M_Ground_Grass_Stylized.mat");

            Material matBamboo = null;
            Material matThatched = null;
            var sampleSC = GameObject.Find("Scarecrow_01");
            if (sampleSC != null)
            {
                var pole = sampleSC.transform.Find("Pole");
                if (pole != null) matBamboo = pole.GetComponent<MeshRenderer>()?.sharedMaterial;
                var hat = sampleSC.transform.Find("NonLa_Hat");
                if (hat != null) matThatched = hat.GetComponent<MeshRenderer>()?.sharedMaterial;
            }

            Material matRock = null;
            var sampleRock = GameObject.Find("West_Platform_01");
            if (sampleRock != null) matRock = sampleRock.GetComponentInChildren<MeshRenderer>()?.sharedMaterial;

            // Prefabs
            var riceRowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Rice/PF_Rice_RowStrip_4m.prefab");
            var lilyPrefab    = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Boats/PF_WaterLily_Cluster.prefab");
            var rockPlatformPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/PF_Rock_Platform_CliffLedge.prefab");
            var rockCragPrefab     = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TripoModels/rock_outcrop_3d_model/rock_outcrop_3d_model.fbx");
            var boulderPrefab      = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Optimized/SM_Rock_Medium_02_BlenderOptimized.fbx");
            var coconutPrefab      = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_06_Coconut.prefab");
            var bambooPrefab       = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_07_BambooGrove.prefab");

            // 2. Expand Ground Base seamlessly northwards (Z: -10m to +206m)
            var groundBase = GameObject.Find("Terrain_Unified_Ground_Base");
            if (groundBase != null)
            {
                Undo.RecordObject(groundBase.transform, "Expand Ground Base 5X");
                // Center Z = (-10 + 206) / 2 = 98.0m, Length Z = 216.0m
                // Bounds remain exactly Z = -10.0m on South (seamless beach connection) and extend to +206.0m on North!
                groundBase.transform.position = new Vector3(0f, 0.25f, 98.0f);
                groundBase.transform.localScale = new Vector3(152.0f, 0.50f, 216.0f);
                Debug.Log("<color=#4CAF50>[RiceExpansion5X]</color> Ground Base extended seamlessly: Z [-10m, +206m].");
            }

            // 3. Relocate Village Gate, Entrance Transition, and North Backdrop Pass
            var gate = GameObject.Find("Gate_Village_North");
            float deltaZ = 0f;
            if (gate != null)
            {
                if (gate.transform.position.z < 100.0f)
                {
                    deltaZ = NEW_GATE_Z - gate.transform.position.z;
                    Undo.RecordObject(gate.transform, "Relocate Village Gate");
                    gate.transform.position = new Vector3(-7.0f, 0.50f, NEW_GATE_Z);
                    Debug.Log($"<color=#4CAF50>[RiceExpansion5X]</color> Village Gate relocated to Z = {NEW_GATE_Z:F1}m (deltaZ = {deltaZ:F1}m).");
                }
                else
                {
                    Debug.Log($"<color=#FFC107>[RiceExpansion5X]</color> Village Gate already at Z = {gate.transform.position.z:F1}m.");
                }
            }

            if (deltaZ > 0.1f)
            {
                // Relocate Village Exit Transition
                var exit = GameObject.Find("VillageExit_NorthGate_Transition");
                if (exit != null)
                {
                    Undo.RecordObject(exit.transform, "Relocate Exit Transition");
                    exit.transform.position = new Vector3(-7.0f, 0.50f, NEW_GATE_Z + 4.0f);
                }

                // Relocate North Gate Pass Rocks & Crags
                string[] northBackdropObjects = new string[]
                {
                    "North_Gate_Pass_West", "North_Gate_Pass_East",
                    "North_Gate_Pass_Crag_West", "North_Gate_Pass_Crag_East",
                    "Hill_North_West", "Hill_North_East"
                };
                foreach (var objName in northBackdropObjects)
                {
                    var obj = GameObject.Find(objName);
                    if (obj != null)
                    {
                        Undo.RecordObject(obj.transform, "Shift North Backdrop Pass");
                        var p = obj.transform.position;
                        p.z += deltaZ;
                        obj.transform.position = p;
                    }
                }

                // Relocate Northern Boundary Foliage
                string[] northVeg = new string[]
                {
                    "Bamboo_NorthCanal_GateWest", "Bamboo_NorthCanal_GateEast",
                    "Bamboo_NorthCanal_FarWest", "Bamboo_NorthCanal_FarEast",
                    "Canopy_NorthCanal_West", "Canopy_NorthCanal_East"
                };
                foreach (var vName in northVeg)
                {
                    var v = GameObject.Find(vName);
                    if (v != null)
                    {
                        Undo.RecordObject(v.transform, "Shift North Gate Foliage");
                        var p = v.transform.position;
                        p.z += deltaZ;
                        v.transform.position = p;
                    }
                }
            }

            // 4. Extend Central Straight Road leading directly to the new Gate (Z: 35.5m to 188.5m)
            var road = GameObject.Find("Road_North_StraightPaddyAisle");
            if (road != null)
            {
                Undo.RecordObject(road.transform, "Extend Central Road");
                // Road starts at Z = 35.5m and extends through the new gate up to Z = 188.5m (Length = 153.0m, Center = 112.0m)
                road.transform.position = new Vector3(-7.0f, 0.51f, 112.0f);
                road.transform.localScale = new Vector3(4.8f, 153.0f, 1.0f);
                Debug.Log("<color=#4CAF50>[RiceExpansion5X]</color> Central Road extended: Z [35.5m, 188.5m], width 4.8m.");
            }

            // 5. Construct 16 New Paddy Plots across 4 Tiers (5X Rice Field Extension)
            var terrain3D = GameObject.Find("Terrain_3D") ?? GameObject.Find("CaMau_1954_Village/Environment/Terrain_3D");
            if (terrain3D == null)
            {
                Debug.LogError("[RiceExpansion5X] Terrain_3D not found!");
                return;
            }

            var extensionRoot = GameObject.Find("Paddy_5X_Extension_Root");
            if (extensionRoot != null)
            {
                Undo.DestroyObjectImmediate(extensionRoot);
            }
            extensionRoot = new GameObject("Paddy_5X_Extension_Root");
            Undo.RegisterCreatedObjectUndo(extensionRoot, "Create Paddy 5X Extension Root");
            extensionRoot.transform.parent = terrain3D.transform;
            extensionRoot.transform.position = Vector3.zero;

            // 4 New Tiers spanning Z = 71m to 177m:
            // West Plots: X = -32.0m, Width = 40.0m (left side of road corridor X in [-9.4, -4.6])
            // East Plots: X =  15.5m, Width = 36.0m (right side of road corridor)
            var newPlots = new (string name, Vector3 pos, Vector2 size, bool isGolden)[]
            {
                // Tier 2 (Z: 71m to 96m)
                ("Paddy_Plot_07_Golden", new Vector3(-32.0f, Y_PLOT, 77.0f),  new Vector2(40.0f, 10.5f), true),
                ("Paddy_Plot_08_Green",  new Vector3(-32.0f, Y_PLOT, 89.5f),  new Vector2(40.0f, 10.5f), false),
                ("Paddy_Plot_09_Green",  new Vector3( 15.5f, Y_PLOT, 77.0f),  new Vector2(36.0f, 10.5f), false),
                ("Paddy_Plot_10_Golden", new Vector3( 15.5f, Y_PLOT, 89.5f),  new Vector2(36.0f, 10.5f), true),

                // Tier 3 (Z: 98m to 123m)
                ("Paddy_Plot_11_Green",  new Vector3(-32.0f, Y_PLOT, 104.0f), new Vector2(40.0f, 10.5f), false),
                ("Paddy_Plot_12_Golden", new Vector3(-32.0f, Y_PLOT, 116.5f), new Vector2(40.0f, 10.5f), true),
                ("Paddy_Plot_13_Golden", new Vector3( 15.5f, Y_PLOT, 104.0f), new Vector2(36.0f, 10.5f), true),
                ("Paddy_Plot_14_Green",  new Vector3( 15.5f, Y_PLOT, 116.5f), new Vector2(36.0f, 10.5f), false),

                // Tier 4 (Z: 125m to 150m)
                ("Paddy_Plot_15_Golden", new Vector3(-32.0f, Y_PLOT, 131.0f), new Vector2(40.0f, 10.5f), true),
                ("Paddy_Plot_16_Green",  new Vector3(-32.0f, Y_PLOT, 143.5f), new Vector2(40.0f, 10.5f), false),
                ("Paddy_Plot_17_Green",  new Vector3( 15.5f, Y_PLOT, 131.0f), new Vector2(36.0f, 10.5f), false),
                ("Paddy_Plot_18_Golden", new Vector3( 15.5f, Y_PLOT, 143.5f), new Vector2(36.0f, 10.5f), true),

                // Tier 5 (Z: 152m to 177m) - Approaching the Village Gate
                ("Paddy_Plot_19_Green",  new Vector3(-32.0f, Y_PLOT, 158.0f), new Vector2(40.0f, 10.5f), false),
                ("Paddy_Plot_20_Golden", new Vector3(-32.0f, Y_PLOT, 170.5f), new Vector2(40.0f, 10.5f), true),
                ("Paddy_Plot_21_Golden", new Vector3( 15.5f, Y_PLOT, 158.0f), new Vector2(36.0f, 10.5f), true),
                ("Paddy_Plot_22_Green",  new Vector3( 15.5f, Y_PLOT, 170.5f), new Vector2(36.0f, 10.5f), false)
            };

            int totalRowsInstantiated = 0;
            foreach (var plot in newPlots)
            {
                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = plot.name;
                quad.transform.parent = extensionRoot.transform;
                quad.transform.position = plot.pos;
                quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                quad.transform.localScale = new Vector3(plot.size.x, plot.size.y, 1f);

                var mr = quad.GetComponent<MeshRenderer>();
                mr.sharedMaterial = plot.isGolden ? matRiceGolden : matRiceGreen;

                // Build Dikes
                BuildPaddyDikes(extensionRoot.transform, plot.name, plot.pos, plot.size, matDike);

                // Build Animated Swaying Rice Rows
                if (riceRowPrefab != null)
                {
                    float halfX = plot.size.x * 0.5f - 1.2f;
                    float halfZ = plot.size.y * 0.5f - 0.8f;
                    float zSpacing = 1.35f;

                    int rowIdx = 0;
                    for (float zSub = plot.pos.z - halfZ; zSub <= plot.pos.z + halfZ + 0.1f; zSub += zSpacing)
                    {
                        for (float xSub = plot.pos.x - halfX + 2.0f; xSub <= plot.pos.x + halfX; xSub += 4.0f)
                        {
                            var row = (GameObject)PrefabUtility.InstantiatePrefab(riceRowPrefab, extensionRoot.transform);
                            row.name = $"{plot.name}_R{rowIdx:D2}_X{xSub:F0}";
                            row.transform.position = new Vector3(xSub, UNIFORM_GROUND_Y + 0.01f, zSub);
                            row.transform.rotation = Quaternion.identity;
                            row.transform.localScale = Vector3.one;

                            var sway = row.GetComponent<PlanetOfLanaFoliageSway>();
                            if (sway == null) sway = row.AddComponent<PlanetOfLanaFoliageSway>();
                            sway.MaxRotationAngle = 2.6f;
                            sway.EnableCharacterInteraction = true;
                            sway.InteractionRadius = 1.2f;
                            sway.MaxPushAngle = 12f;

                            totalRowsInstantiated++;
                        }
                        rowIdx++;
                    }
                }
            }
            Debug.Log($"<color=#4CAF50>[RiceExpansion5X]</color> 16 new paddy plots created with {totalRowsInstantiated} animated rice row strips.");

            // 6. Traditional Scarecrows across the vast fields
            BuildScarecrow(extensionRoot.transform, "Scarecrow_03", new Vector3(-25.0f, UNIFORM_GROUND_Y, 89.5f), matBamboo, matThatched);
            BuildScarecrow(extensionRoot.transform, "Scarecrow_04", new Vector3( 18.0f, UNIFORM_GROUND_Y, 116.5f), matBamboo, matThatched);
            BuildScarecrow(extensionRoot.transform, "Scarecrow_05", new Vector3(-20.0f, UNIFORM_GROUND_Y, 143.5f), matBamboo, matThatched);
            BuildScarecrow(extensionRoot.transform, "Scarecrow_06", new Vector3( 12.0f, UNIFORM_GROUND_Y, 170.5f), matBamboo, matThatched);

            // 7. Middle Irrigation Water Basin & Lily Clusters (Đầm nước bờ ruộng)
            var canalRoot = new GameObject("Paddy_Irrigation_Basin_East");
            canalRoot.transform.parent = extensionRoot.transform;
            canalRoot.transform.position = new Vector3(46.0f, UNIFORM_GROUND_Y - 0.04f, 116.0f);

            var canalWater = GameObject.CreatePrimitive(PrimitiveType.Quad);
            canalWater.name = "Canal_Water_Surface";
            canalWater.transform.parent = canalRoot.transform;
            canalWater.transform.localPosition = Vector3.zero;
            canalWater.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            canalWater.transform.localScale = new Vector3(14.0f, 22.0f, 1f);
            if (matWater != null) canalWater.GetComponent<MeshRenderer>().sharedMaterial = matWater;

            if (lilyPrefab != null)
            {
                var l1 = (GameObject)PrefabUtility.InstantiatePrefab(lilyPrefab, canalRoot.transform);
                l1.name = "WaterLily_Basin_01";
                l1.transform.position = new Vector3(43.0f, UNIFORM_GROUND_Y - 0.02f, 112.0f);
                l1.transform.localScale = Vector3.one * 1.3f;

                var l2 = (GameObject)PrefabUtility.InstantiatePrefab(lilyPrefab, canalRoot.transform);
                l2.name = "WaterLily_Basin_02";
                l2.transform.position = new Vector3(48.0f, UNIFORM_GROUND_Y - 0.02f, 119.0f);
                l2.transform.localScale = Vector3.one * 1.4f;
            }

            // 8. Extend Flanking Mountains along West & East Boundaries (Z: 85m to 200m)
            BuildFlankingMountainExtensions(matDike, matRock, rockPlatformPrefab, rockCragPrefab, boulderPrefab, coconutPrefab, bambooPrefab);

            // 9. Mark Scene Dirty and Save
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log("<color=#4CAF50>[RiceExpansion5X]</color> SUCCESS: Rice fields expanded 5X northward, Gate relocated, and scene saved cleanly!");
        }

        private static void BuildPaddyDikes(Transform parent, string baseName, Vector3 pos, Vector2 size, Material matDike)
        {
            float halfX = size.x * 0.5f;
            float halfZ = size.y * 0.5f;
            float dikeY = UNIFORM_GROUND_Y + 0.04f;

            var dN = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dN.name = $"{baseName}_DikeN";
            dN.transform.parent = parent;
            dN.transform.position = new Vector3(pos.x, dikeY, pos.z + halfZ);
            dN.transform.localScale = new Vector3(size.x + 0.6f, 0.08f, 0.6f);
            if (matDike != null) dN.GetComponent<MeshRenderer>().sharedMaterial = matDike;

            var dS = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dS.name = $"{baseName}_DikeS";
            dS.transform.parent = parent;
            dS.transform.position = new Vector3(pos.x, dikeY, pos.z - halfZ);
            dS.transform.localScale = new Vector3(size.x + 0.6f, 0.08f, 0.6f);
            if (matDike != null) dS.GetComponent<MeshRenderer>().sharedMaterial = matDike;

            var dE = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dE.name = $"{baseName}_DikeE";
            dE.transform.parent = parent;
            dE.transform.position = new Vector3(pos.x + halfX, dikeY, pos.z);
            dE.transform.localScale = new Vector3(0.6f, 0.08f, size.y);
            if (matDike != null) dE.GetComponent<MeshRenderer>().sharedMaterial = matDike;

            var dW = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dW.name = $"{baseName}_DikeW";
            dW.transform.parent = parent;
            dW.transform.position = new Vector3(pos.x - halfX, dikeY, pos.z);
            dW.transform.localScale = new Vector3(0.6f, 0.08f, size.y);
            if (matDike != null) dW.GetComponent<MeshRenderer>().sharedMaterial = matDike;
        }

        private static void BuildScarecrow(Transform parent, string name, Vector3 pos, Material matBamboo, Material matThatched)
        {
            var sc = new GameObject(name);
            sc.transform.parent = parent;
            sc.transform.position = pos;

            var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = "Pole";
            post.transform.parent = sc.transform;
            post.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            post.transform.localScale = new Vector3(0.12f, 0.85f, 0.12f);
            if (matBamboo != null) post.GetComponent<MeshRenderer>().sharedMaterial = matBamboo;

            var arms = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            arms.name = "CrossArms";
            arms.transform.parent = sc.transform;
            arms.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            arms.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            arms.transform.localScale = new Vector3(0.09f, 0.65f, 0.09f);
            if (matBamboo != null) arms.GetComponent<MeshRenderer>().sharedMaterial = matBamboo;

            var hat = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hat.name = "NonLa_Hat";
            hat.transform.parent = sc.transform;
            hat.transform.localPosition = new Vector3(0f, 1.72f, 0f);
            hat.transform.localScale = new Vector3(0.70f, 0.12f, 0.70f);
            if (matThatched != null) hat.GetComponent<MeshRenderer>().sharedMaterial = matThatched;
        }

        private static void BuildFlankingMountainExtensions(Material matGrass, Material matRock, GameObject rockPlatformPrefab, GameObject rockCragPrefab, GameObject boulderPrefab, GameObject coconutPrefab, GameObject bambooPrefab)
        {
            // West Mountain Range Extension
            var westRange = GameObject.Find("MountainRange_West_Flank");
            if (westRange != null)
            {
                var oldWestExt = westRange.transform.Find("Mountain_5X_West_Ext");
                if (oldWestExt != null) Undo.DestroyObjectImmediate(oldWestExt.gameObject);

                var westExt = new GameObject("Mountain_5X_West_Ext");
                Undo.RegisterCreatedObjectUndo(westExt, "Create West Mountain Extension");
                westExt.transform.parent = westRange.transform;
                westExt.transform.position = Vector3.zero;

                // Organic rolling hill meshes
                CreateOrganicHillMesh(westExt.transform, "Hill_West_Ext_01", new Vector3(-80.0f, UNIFORM_GROUND_Y, 110.0f), 50.0f, 70.0f, 6.2f, matGrass);
                CreateOrganicHillMesh(westExt.transform, "Hill_West_Ext_02", new Vector3(-80.0f, UNIFORM_GROUND_Y, 165.0f), 52.0f, 70.0f, 6.5f, matGrass);

                // Volumetric rock platforms & crags
                InstantiateRock(rockPlatformPrefab, westExt.transform, "West_Platform_Ext_01", new Vector3(-61.0f, 1.2f, 95.0f), new Vector3(3.4f, 2.8f, 3.2f), new Vector3(0f, 20f, 0f), matRock);
                InstantiateRock(rockCragPrefab, westExt.transform, "West_Crag_Ext_01", new Vector3(-72.0f, 3.8f, 105.0f), new Vector3(8.0f, 12.0f, 7.5f), new Vector3(0f, -30f, 0f), matRock);
                InstantiateRock(rockPlatformPrefab, westExt.transform, "West_Platform_Ext_02", new Vector3(-63.0f, 1.5f, 125.0f), new Vector3(3.6f, 3.0f, 3.4f), new Vector3(0f, -40f, 0f), matRock);
                InstantiateRock(rockCragPrefab, westExt.transform, "West_Crag_Ext_02", new Vector3(-74.0f, 4.2f, 135.0f), new Vector3(8.5f, 13.5f, 8.0f), new Vector3(0f, 55f, 0f), matRock);
                InstantiateRock(rockPlatformPrefab, westExt.transform, "West_Platform_Ext_03", new Vector3(-62.0f, 1.3f, 155.0f), new Vector3(3.5f, 2.8f, 3.2f), new Vector3(0f, 15f, 0f), matRock);
                InstantiateRock(rockCragPrefab, westExt.transform, "West_Crag_Ext_03", new Vector3(-73.0f, 4.0f, 165.0f), new Vector3(8.2f, 12.5f, 7.8f), new Vector3(0f, -60f, 0f), matRock);
                InstantiateRock(rockPlatformPrefab, westExt.transform, "West_Platform_Ext_04", new Vector3(-60.0f, 1.1f, 180.0f), new Vector3(3.2f, 2.5f, 3.0f), new Vector3(0f, 30f, 0f), matRock);
                InstantiateRock(rockCragPrefab, westExt.transform, "West_Crag_Ext_Peak", new Vector3(-82.0f, 5.8f, 140.0f), new Vector3(11.0f, 17.5f, 10.5f), new Vector3(0f, 10f, 0f), matRock);

                // Boulders along footing
                InstantiateRock(boulderPrefab, westExt.transform, "West_Boulder_Ext_01", new Vector3(-58.0f, UNIFORM_GROUND_Y + 0.3f, 100.0f), new Vector3(2.4f, 1.8f, 2.2f), new Vector3(10f, 30f, 0f));
                InstantiateRock(boulderPrefab, westExt.transform, "West_Boulder_Ext_02", new Vector3(-57.5f, UNIFORM_GROUND_Y + 0.3f, 130.0f), new Vector3(2.2f, 1.6f, 2.0f), new Vector3(-10f, 60f, 5f));
                InstantiateRock(boulderPrefab, westExt.transform, "West_Boulder_Ext_03", new Vector3(-58.2f, UNIFORM_GROUND_Y + 0.3f, 160.0f), new Vector3(2.5f, 1.9f, 2.3f), new Vector3(5f, -45f, 0f));

                // Flanking foliage framing the fields
                if (bambooPrefab != null)
                {
                    InstantiateTree(bambooPrefab, westExt.transform, "West_Bamboo_Ext_01", new Vector3(-56.0f, UNIFORM_GROUND_Y, 115.0f), 1.35f);
                    InstantiateTree(bambooPrefab, westExt.transform, "West_Bamboo_Ext_02", new Vector3(-56.0f, UNIFORM_GROUND_Y, 170.0f), 1.40f);
                }
                if (coconutPrefab != null)
                {
                    InstantiateTree(coconutPrefab, westExt.transform, "West_Coconut_Ext_01", new Vector3(-55.0f, UNIFORM_GROUND_Y, 145.0f), 1.25f);
                }
            }

            // East Mountain Range Extension
            var eastRange = GameObject.Find("MountainRange_East_Flank");
            if (eastRange != null)
            {
                var oldEastExt = eastRange.transform.Find("Mountain_5X_East_Ext");
                if (oldEastExt != null) Undo.DestroyObjectImmediate(oldEastExt.gameObject);

                var eastExt = new GameObject("Mountain_5X_East_Ext");
                Undo.RegisterCreatedObjectUndo(eastExt, "Create East Mountain Extension");
                eastExt.transform.parent = eastRange.transform;
                eastExt.transform.position = Vector3.zero;

                // Organic rolling hill meshes
                CreateOrganicHillMesh(eastExt.transform, "Hill_East_Ext_01", new Vector3(80.0f, UNIFORM_GROUND_Y, 110.0f), 50.0f, 70.0f, 6.2f, matGrass);
                CreateOrganicHillMesh(eastExt.transform, "Hill_East_Ext_02", new Vector3(80.0f, UNIFORM_GROUND_Y, 165.0f), 52.0f, 70.0f, 6.5f, matGrass);

                // Volumetric rock platforms & crags
                InstantiateRock(rockPlatformPrefab, eastExt.transform, "East_Platform_Ext_01", new Vector3(60.0f, 1.2f, 95.0f), new Vector3(3.4f, 2.8f, 3.2f), new Vector3(0f, -20f, 0f), matRock);
                InstantiateRock(rockCragPrefab, eastExt.transform, "East_Crag_Ext_01", new Vector3(72.0f, 3.8f, 105.0f), new Vector3(8.0f, 12.0f, 7.5f), new Vector3(0f, 35f, 0f), matRock);
                InstantiateRock(rockPlatformPrefab, eastExt.transform, "East_Platform_Ext_02", new Vector3(62.0f, 1.5f, 125.0f), new Vector3(3.6f, 3.0f, 3.4f), new Vector3(0f, 40f, 0f), matRock);
                InstantiateRock(rockCragPrefab, eastExt.transform, "East_Crag_Ext_02", new Vector3(74.0f, 4.2f, 135.0f), new Vector3(8.5f, 13.5f, 8.0f), new Vector3(0f, -50f, 0f), matRock);
                InstantiateRock(rockPlatformPrefab, eastExt.transform, "East_Platform_Ext_03", new Vector3(61.0f, 1.3f, 155.0f), new Vector3(3.5f, 2.8f, 3.2f), new Vector3(0f, -15f, 0f), matRock);
                InstantiateRock(rockCragPrefab, eastExt.transform, "East_Crag_Ext_03", new Vector3(73.0f, 4.0f, 165.0f), new Vector3(8.2f, 12.5f, 7.8f), new Vector3(0f, 65f, 0f), matRock);
                InstantiateRock(rockPlatformPrefab, eastExt.transform, "East_Platform_Ext_04", new Vector3(58.0f, 1.1f, 180.0f), new Vector3(3.2f, 2.5f, 3.0f), new Vector3(0f, -30f, 0f), matRock);
                InstantiateRock(rockCragPrefab, eastExt.transform, "East_Crag_Ext_Peak", new Vector3(84.0f, 5.8f, 140.0f), new Vector3(11.5f, 17.5f, 10.5f), new Vector3(0f, -10f, 0f), matRock);

                // Boulders along footing
                InstantiateRock(boulderPrefab, eastExt.transform, "East_Boulder_Ext_01", new Vector3(56.0f, UNIFORM_GROUND_Y + 0.3f, 100.0f), new Vector3(2.4f, 1.8f, 2.2f), new Vector3(10f, -30f, 0f));
                InstantiateRock(boulderPrefab, eastExt.transform, "East_Boulder_Ext_02", new Vector3(55.5f, UNIFORM_GROUND_Y + 0.3f, 130.0f), new Vector3(2.2f, 1.6f, 2.0f), new Vector3(-10f, -60f, 5f));
                InstantiateRock(boulderPrefab, eastExt.transform, "East_Boulder_Ext_03", new Vector3(56.2f, UNIFORM_GROUND_Y + 0.3f, 160.0f), new Vector3(2.5f, 1.9f, 2.3f), new Vector3(5f, 45f, 0f));

                // Flanking foliage framing the fields
                if (bambooPrefab != null)
                {
                    InstantiateTree(bambooPrefab, eastExt.transform, "East_Bamboo_Ext_01", new Vector3(45.0f, UNIFORM_GROUND_Y, 115.0f), 1.35f);
                    InstantiateTree(bambooPrefab, eastExt.transform, "East_Bamboo_Ext_02", new Vector3(45.0f, UNIFORM_GROUND_Y, 170.0f), 1.40f);
                }
                if (coconutPrefab != null)
                {
                    InstantiateTree(coconutPrefab, eastExt.transform, "East_Coconut_Ext_01", new Vector3(42.0f, UNIFORM_GROUND_Y, 145.0f), 1.25f);
                }
            }
        }

        private static GameObject CreateOrganicHillMesh(Transform parent, string name, Vector3 center, float width, float length, float maxHeight, Material mat, float groundY = UNIFORM_GROUND_Y)
        {
            int segmentsX = 16;
            int segmentsZ = 16;
            int numVertices = (segmentsX + 1) * (segmentsZ + 1);
            var vertices = new Vector3[numVertices];
            var uvs = new Vector2[numVertices];
            var triangles = new int[segmentsX * segmentsZ * 6];

            float halfW = width * 0.5f;
            float halfL = length * 0.5f;

            for (int z = 0; z <= segmentsZ; z++)
            {
                float tz = (float)z / segmentsZ;
                float posZ = Mathf.Lerp(-halfL, halfL, tz);
                float bellZ = Mathf.Sin(tz * Mathf.PI);

                for (int x = 0; x <= segmentsX; x++)
                {
                    float tx = (float)x / segmentsX;
                    float posX = Mathf.Lerp(-halfW, halfW, tx);
                    float bellX = Mathf.Sin(tx * Mathf.PI);

                    float shapeFactor = Mathf.Pow(Mathf.Clamp01(bellX * bellZ), 0.75f);
                    float y = groundY + (maxHeight * shapeFactor);

                    int idx = z * (segmentsX + 1) + x;
                    vertices[idx] = new Vector3(posX, y - groundY, posZ);
                    uvs[idx] = new Vector2(tx * (width / 16f), tz * (length / 16f));
                }
            }

            int triIndex = 0;
            for (int z = 0; z < segmentsZ; z++)
            {
                for (int x = 0; x < segmentsX; x++)
                {
                    int botLeft = z * (segmentsX + 1) + x;
                    int botRight = botLeft + 1;
                    int topLeft = (z + 1) * (segmentsX + 1) + x;
                    int topRight = topLeft + 1;

                    triangles[triIndex++] = botLeft;
                    triangles[triIndex++] = topLeft;
                    triangles[triIndex++] = botRight;

                    triangles[triIndex++] = botRight;
                    triangles[triIndex++] = topLeft;
                    triangles[triIndex++] = topRight;
                }
            }

            var mesh = new Mesh();
            mesh.name = name + "_Mesh";
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var go = new GameObject(name);
            go.transform.parent = parent;
            go.transform.position = center;
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            if (mat != null) mr.sharedMaterial = mat;

            var col = go.AddComponent<MeshCollider>();
            col.sharedMesh = mesh;

            return go;
        }

        private static GameObject InstantiateRock(GameObject prefab, Transform parent, string name, Vector3 pos, Vector3 scale, Vector3 rot, Material overrideMat = null)
        {
            if (prefab == null) return null;
            GameObject obj;
            if (PrefabUtility.IsPartOfPrefabAsset(prefab))
            {
                obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            }
            else
            {
                obj = Object.Instantiate(prefab, parent);
            }
            obj.name = name;
            obj.transform.position = pos;
            obj.transform.localScale = scale;
            obj.transform.rotation = Quaternion.Euler(rot);

            if (overrideMat != null)
            {
                foreach (var r in obj.GetComponentsInChildren<Renderer>())
                {
                    r.sharedMaterial = overrideMat;
                }
            }
            return obj;
        }

        private static GameObject InstantiateTree(GameObject prefab, Transform parent, string name, Vector3 pos, float scale)
        {
            if (prefab == null) return null;
            GameObject obj;
            if (PrefabUtility.IsPartOfPrefabAsset(prefab))
            {
                obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            }
            else
            {
                obj = Object.Instantiate(prefab, parent);
            }
            obj.name = name;
            obj.transform.position = pos;
            obj.transform.localScale = Vector3.one * scale;
            obj.transform.rotation = Quaternion.identity;
            return obj;
        }
    }
}
