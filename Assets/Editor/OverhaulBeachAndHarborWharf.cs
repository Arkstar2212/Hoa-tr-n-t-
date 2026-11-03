using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

namespace HoaDo.Editor
{
    public static class OverhaulBeachAndHarborWharf
    {
        private const float MESH_BOTTOM_OFFSET = 0.413f;

        [MenuItem("Tools/Hoa Do/Overhaul Wider Beach And Boat Waiting Pier")]
        public static void Execute()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (!activeScene.path.Contains("CaMau_1954_Village"))
            {
                Debug.Log("[OverhaulBeach] Opening CaMau_1954_Village.unity...");
                EditorSceneManager.OpenScene("Assets/Scenes/CaMau_1954_Village.unity");
                activeScene = EditorSceneManager.GetActiveScene();
            }

            Debug.Log("[OverhaulBeach] Starting overhaul of wider beach and enlarged boat waiting area...");

            // 1. Materials setup (High quality authentic textures)
            var matSand = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Seabed_Sand.mat");
            var matPierWood = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/GhibliModular/M_Ghibli_PierPlank.mat");
            var matPierStilt = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/GhibliModular/M_Ghibli_PierStilt.mat");
            if (matPierStilt == null) matPierStilt = matPierWood;
            var matThatched = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Architecture/M_StiltedHouse_Thatched.mat");

            var texPlank = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Props_3D/Meshy_AI_Wooden_Slat_Cube_0927180014_texture.png");
            if (matPierWood != null && texPlank != null)
            {
                matPierWood.shader = Shader.Find("Universal Render Pipeline/Lit");
                matPierWood.mainTexture = texPlank;
                matPierWood.mainTextureScale = new Vector2(8f, 6f);
                matPierWood.SetColor("_BaseColor", new Color(0.72f, 0.58f, 0.42f, 1.0f));
                EditorUtility.SetDirty(matPierWood);
            }

            // 2. Overhaul Beach Slope (Wider, natural 20m depth beach)
            var beachSlopeObj = GameObject.Find("CaMau_1954_Village/Environment/Terrain_3D/Terrain_Beach_Slope");
            if (beachSlopeObj == null)
            {
                var terrainRoot = GameObject.Find("CaMau_1954_Village/Environment/Terrain_3D");
                beachSlopeObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                beachSlopeObj.name = "Terrain_Beach_Slope";
                if (terrainRoot != null) beachSlopeObj.transform.parent = terrainRoot.transform;
            }
            beachSlopeObj.transform.position = new Vector3(0.0f, -0.15f, -18.0f);
            beachSlopeObj.transform.rotation = Quaternion.Euler(3.2f, 0f, 0f);
            beachSlopeObj.transform.localScale = new Vector3(160.0f, 0.50f, 20.0f);
            if (matSand != null) beachSlopeObj.GetComponent<MeshRenderer>().sharedMaterial = matSand;

            // Remove duplicate House_09 from Village_Houses_Network to prevent overlap with the waiting pavilion
            var h9 = GameObject.Find("CaMau_1954_Village/Environment/Village_Houses_Network/House_09_Harbor_StiltHut");
            if (h9 != null) Object.DestroyImmediate(h9);

            // 3. Clear previous pier objects (except NPCs) and boats
            var pierArea = GameObject.Find("CaMau_1954_Village/Village_Areas/08_Ben_Xuong");
            if (pierArea == null)
            {
                var areas = GameObject.Find("CaMau_1954_Village/Village_Areas");
                pierArea = new GameObject("08_Ben_Xuong");
                if (areas != null) pierArea.transform.parent = areas.transform;
            }

            for (int i = pierArea.transform.childCount - 1; i >= 0; i--)
            {
                var child = pierArea.transform.GetChild(i);
                if (!child.name.StartsWith("NPC_"))
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }

            var boatsObj = GameObject.Find("CaMau_1954_Village/Environment/WaterSystem_3D/Harbor_Boats_And_Sampans");
            if (boatsObj != null) Object.DestroyImmediate(boatsObj);

            // 4. Build Coastal Piers & Waiting Wharf
            BuildCoastalPiersAndWaitingWharf(pierArea.transform, matPierWood, matPierStilt, matThatched);

            // 5. Reposition Ferryman & Trigger onto elevated deck
            var ferryman = GameObject.Find("CaMau_1954_Village/NPCs/NPC_Ferryman_UncleBay");
            if (ferryman == null) ferryman = GameObject.Find("CaMau_1954_Village/Village_Areas/08_Ben_Xuong/NPC_Ferryman_UncleBay");
            if (ferryman != null)
            {
                ferryman.transform.position = new Vector3(56.0f, 1.00f, -24.5f);
                ferryman.transform.rotation = Quaternion.Euler(0f, 195f, 0f);
            }

            var trigger16 = GameObject.Find("CaMau_1954_Village/Gameplay_Triggers/Trigger_16_Ferryman");
            if (trigger16 != null)
            {
                trigger16.transform.position = new Vector3(56.0f, 1.20f, -24.5f);
            }

            Physics.SyncTransforms();
            EditorSceneManager.MarkSceneDirty(activeScene);
            Debug.Log("[OverhaulBeach] Beach expanded and Boat Waiting Pier overhauled successfully!");
        }

        private static void BuildCoastalPiersAndWaitingWharf(Transform pierRoot, Material matPierWood, Material matPierStilt, Material matThatched)
        {
            var waterSys = GameObject.Find("CaMau_1954_Village/Environment/WaterSystem_3D");

            // ---------------------------------------------------------
            // A. WEST PIER (Cầu Tàu Phía Tây)
            // ---------------------------------------------------------
            var westJetty = GameObject.CreatePrimitive(PrimitiveType.Cube);
            westJetty.name = "Pier_West_Jetty_Deck";
            westJetty.transform.parent = pierRoot;
            westJetty.transform.position = new Vector3(-20.0f, 0.18f, -18.5f);
            westJetty.transform.localScale = new Vector3(3.6f, 0.24f, 22.0f);
            if (matPierWood != null) westJetty.GetComponent<MeshRenderer>().sharedMaterial = matPierWood;

            float[] westPilingsZ = new float[] { -9.5f, -14.0f, -18.5f, -23.0f, -27.5f };
            foreach (var pz in westPilingsZ)
            {
                BuildPiling(pierRoot, $"WestPiling_L_{pz:F0}", new Vector3(-21.4f, -0.6f, pz), matPierStilt, 0.26f, 1.8f);
                BuildPiling(pierRoot, $"WestPiling_R_{pz:F0}", new Vector3(-18.6f, -0.6f, pz), matPierStilt, 0.26f, 1.8f);
            }

            var westFishPlatform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            westFishPlatform.name = "Pier_West_FishDrying_Platform";
            westFishPlatform.transform.parent = pierRoot;
            westFishPlatform.transform.position = new Vector3(-30.0f, 0.20f, -12.5f);
            westFishPlatform.transform.localScale = new Vector3(10.0f, 0.24f, 5.0f);
            if (matPierWood != null) westFishPlatform.GetComponent<MeshRenderer>().sharedMaterial = matPierWood;

            var awningFrame = new GameObject("Pier_West_AwningFrame");
            awningFrame.transform.parent = pierRoot;
            awningFrame.transform.position = new Vector3(-30.0f, 1.85f, -12.5f);
            var awningRoof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            awningRoof.name = "Awning_Roof";
            awningRoof.transform.parent = awningFrame.transform;
            awningRoof.transform.localPosition = Vector3.zero;
            awningRoof.transform.localScale = new Vector3(9.5f, 0.08f, 4.5f);
            if (matThatched != null) awningRoof.GetComponent<MeshRenderer>().sharedMaterial = matThatched;

            // ---------------------------------------------------------
            // B. EAST HARBOR (Bến Chờ Thuyền Đông - Grand, Elevated & Detailed)
            // ---------------------------------------------------------
            // 1. Main Elevated Passenger Waiting Deck (To & Cao: 24m x 18m, elevated to Y = 0.85m, deck surface Y = 1.00m)
            var eastDeck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            eastDeck.name = "Pier_East_Harbor_MainDeck";
            eastDeck.transform.parent = pierRoot;
            eastDeck.transform.position = new Vector3(60.0f, 0.85f, -17.5f);
            eastDeck.transform.localScale = new Vector3(24.0f, 0.30f, 18.0f);
            if (matPierWood != null) eastDeck.GetComponent<MeshRenderer>().sharedMaterial = matPierWood;

            // 2. Transition Entry Ramp from Village Road (Y=0.50m) to Elevated Deck (Y=1.00m)
            var entryRamp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            entryRamp.name = "Pier_East_Entry_Ramp";
            entryRamp.transform.parent = pierRoot;
            entryRamp.transform.position = new Vector3(60.0f, 0.75f, -7.8f);
            entryRamp.transform.rotation = Quaternion.Euler(-10.0f, 0f, 0f);
            entryRamp.transform.localScale = new Vector3(10.0f, 0.22f, 3.2f);
            if (matPierWood != null) entryRamp.GetComponent<MeshRenderer>().sharedMaterial = matPierWood;

            // 3. Boarding Pier Walkway Arm extending deep into turquoise water (5m wide, 17m long to Z = -42.5m)
            var eastArm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            eastArm.name = "Pier_East_WalkwayArm";
            eastArm.transform.parent = pierRoot;
            eastArm.transform.position = new Vector3(56.0f, 0.75f, -34.0f);
            eastArm.transform.localScale = new Vector3(5.0f, 0.28f, 17.0f);
            if (matPierWood != null) eastArm.GetComponent<MeshRenderer>().sharedMaterial = matPierWood;

            // 4. Elevated Timber Pilings (Cọc Trụ Gỗ Đỡ Sàn)
            float[] deckPilingsX = new float[] { 49.5f, 55.0f, 60.5f, 66.0f, 71.0f };
            float[] deckPilingsZ = new float[] { -9.5f, -14.0f, -18.5f, -23.0f, -26.0f };
            int pIndex = 1;
            foreach (var px in deckPilingsX)
            {
                foreach (var pz in deckPilingsZ)
                {
                    BuildPiling(pierRoot, $"EastDeckPiling_{pIndex++:D2}", new Vector3(px, -0.4f, pz), matPierStilt, 0.32f, 2.5f);
                }
            }

            float[] armPilingsZ = new float[] { -29.0f, -33.5f, -38.0f, -42.0f };
            foreach (var pz in armPilingsZ)
            {
                BuildPiling(pierRoot, $"EastArmPiling_L_{pz:F0}", new Vector3(53.8f, -0.4f, pz), matPierStilt, 0.32f, 2.5f);
                BuildPiling(pierRoot, $"EastArmPiling_R_{pz:F0}", new Vector3(58.2f, -0.4f, pz), matPierStilt, 0.32f, 2.5f);
            }

            // 5. Grand Waiting Pavilion (Chòi Chờ Đò Nam Bộ To & Cao Mái Tranh)
            var fbxCottage = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Optimized/SM_NhaTranh_BlenderOptimized.fbx");
            if (fbxCottage != null)
            {
                var pavilion = (GameObject)PrefabUtility.InstantiatePrefab(fbxCottage, pierRoot);
                pavilion.name = "Harbor_Waiting_Pavilion_Main";
                float pavScale = 4.0f;
                float yPos = 1.00f + (MESH_BOTTOM_OFFSET * pavScale); // Exactly seated on deck floor
                pavilion.transform.position = new Vector3(64.5f, yPos, -16.0f);
                pavilion.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
                pavilion.transform.localScale = Vector3.one * pavScale; // Lofty ~5.6m roof peak!
                if (matThatched != null)
                {
                    foreach (var r in pavilion.GetComponentsInChildren<MeshRenderer>()) r.sharedMaterial = matThatched;
                }
            }

            // 6. Open Waiting Canopy (Mái Che Chờ Thuyền Thoáng Mát Phía Tây Sàn Chờ)
            var canopyRoot = new GameObject("Harbor_Waiting_Canopy_West");
            canopyRoot.transform.parent = pierRoot;
            canopyRoot.transform.position = new Vector3(54.0f, 1.00f, -16.5f);

            Vector3[] canopyCols = new Vector3[] {
                new Vector3(-3.2f, 1.5f, -3.2f),
                new Vector3( 3.2f, 1.5f, -3.2f),
                new Vector3(-3.2f, 1.5f,  3.2f),
                new Vector3( 3.2f, 1.5f,  3.2f)
            };
            for (int i = 0; i < canopyCols.Length; i++)
            {
                var col = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                col.name = $"Canopy_Col_{i + 1}";
                col.transform.parent = canopyRoot.transform;
                col.transform.localPosition = canopyCols[i];
                col.transform.localScale = new Vector3(0.28f, 1.5f, 0.28f);
                if (matPierStilt != null) col.GetComponent<MeshRenderer>().sharedMaterial = matPierStilt;
            }

            // Horizontal crossbeams
            var beamNS1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beamNS1.name = "Beam_NS_Left";
            beamNS1.transform.parent = canopyRoot.transform;
            beamNS1.transform.localPosition = new Vector3(-3.2f, 3.0f, 0f);
            beamNS1.transform.localScale = new Vector3(0.22f, 0.22f, 6.8f);
            if (matPierWood != null) beamNS1.GetComponent<MeshRenderer>().sharedMaterial = matPierWood;

            var beamNS2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beamNS2.name = "Beam_NS_Right";
            beamNS2.transform.parent = canopyRoot.transform;
            beamNS2.transform.localPosition = new Vector3(3.2f, 3.0f, 0f);
            beamNS2.transform.localScale = new Vector3(0.22f, 0.22f, 6.8f);
            if (matPierWood != null) beamNS2.GetComponent<MeshRenderer>().sharedMaterial = matPierWood;

            // Thatched canopy roof
            var canopyRoof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            canopyRoof.name = "Canopy_Thatched_Roof";
            canopyRoof.transform.parent = canopyRoot.transform;
            canopyRoof.transform.localPosition = new Vector3(0f, 3.25f, 0f);
            canopyRoof.transform.localScale = new Vector3(7.8f, 0.28f, 7.8f);
            if (matThatched != null) canopyRoof.GetComponent<MeshRenderer>().sharedMaterial = matThatched;

            // 7. Rustic Wooden Waiting Benches (Ghế Gỗ Dài Cho Khách Chờ Đò)
            BuildWaitingBench(pierRoot, "Harbor_Bench_01", new Vector3(54.0f, 1.00f, -14.0f), 4.4f, 0f, matPierWood);
            BuildWaitingBench(pierRoot, "Harbor_Bench_02", new Vector3(54.0f, 1.00f, -19.0f), 4.4f, 0f, matPierWood);
            BuildWaitingBench(pierRoot, "Harbor_Bench_Pavilion", new Vector3(64.5f, 1.00f, -14.0f), 3.8f, 0f, matPierWood);

            // 8. Wooden Perimeter Railings (Lan Can Gỗ Bến Đò - Top & Mid Rails)
            var railingsRoot = new GameObject("Harbor_Perimeter_Railings");
            railingsRoot.transform.parent = pierRoot;

            BuildRailingSegment(railingsRoot.transform, "Railing_East", new Vector3(71.8f, 1.55f, -17.5f), new Vector3(0.20f, 0.90f, 17.5f), matPierWood);
            BuildRailingSegment(railingsRoot.transform, "Railing_NorthEast", new Vector3(68.5f, 1.55f, -8.7f), new Vector3(6.5f, 0.90f, 0.20f), matPierWood);
            BuildRailingSegment(railingsRoot.transform, "Railing_NorthWest", new Vector3(51.5f, 1.55f, -8.7f), new Vector3(6.5f, 0.90f, 0.20f), matPierWood);
            BuildRailingSegment(railingsRoot.transform, "Railing_West", new Vector3(48.2f, 1.55f, -17.5f), new Vector3(0.20f, 0.90f, 17.5f), matPierWood);
            BuildRailingSegment(railingsRoot.transform, "Railing_South_East", new Vector3(65.0f, 1.55f, -26.3f), new Vector3(13.0f, 0.90f, 0.20f), matPierWood);
            BuildRailingSegment(railingsRoot.transform, "Railing_South_West", new Vector3(50.8f, 1.55f, -26.3f), new Vector3(5.2f, 0.90f, 0.20f), matPierWood);
            BuildRailingSegment(railingsRoot.transform, "Railing_Walkway_L", new Vector3(53.6f, 1.45f, -34.5f), new Vector3(0.18f, 0.85f, 14.5f), matPierWood);
            BuildRailingSegment(railingsRoot.transform, "Railing_Walkway_R", new Vector3(58.4f, 1.45f, -34.5f), new Vector3(0.18f, 0.85f, 14.5f), matPierWood);

            // 9. Mooring Posts / Cọc Buộc Tàu (6 sturdy wooden bollards)
            float[,] bollardPositions = new float[,] {
                { 53.4f, -26.8f }, { 58.6f, -26.8f },
                { 53.4f, -33.5f }, { 58.6f, -33.5f },
                { 53.4f, -40.5f }, { 58.6f, -40.5f }
            };
            for (int i = 0; i < bollardPositions.GetLength(0); i++)
            {
                var bp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                bp.name = $"Mooring_Bollard_{i + 1:D2}";
                bp.transform.parent = pierRoot;
                bp.transform.position = new Vector3(bollardPositions[i, 0], 1.40f, bollardPositions[i, 1]);
                bp.transform.localScale = new Vector3(0.40f, 0.80f, 0.40f);
                if (matPierStilt != null) bp.GetComponent<MeshRenderer>().sharedMaterial = matPierStilt;
            }

            // 10. Harbor Lantern Beacon Post (Cột Đèn Bão Bến Đò tại đầu cầu tàu)
            var beaconPost = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beaconPost.name = "Harbor_Lantern_Beacon_Post";
            beaconPost.transform.parent = pierRoot;
            beaconPost.transform.position = new Vector3(53.6f, 2.70f, -42.0f);
            beaconPost.transform.localScale = new Vector3(0.24f, 2.2f, 0.24f);
            if (matPierStilt != null) beaconPost.GetComponent<MeshRenderer>().sharedMaterial = matPierStilt;

            var lanternArm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lanternArm.name = "Beacon_Lantern_Arm";
            lanternArm.transform.parent = beaconPost.transform;
            lanternArm.transform.localPosition = new Vector3(0.8f, 0.85f, 0f);
            lanternArm.transform.localScale = new Vector3(1.2f, 0.10f, 0.15f);
            if (matPierWood != null) lanternArm.GetComponent<MeshRenderer>().sharedMaterial = matPierWood;

            var lanternCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lanternCube.name = "Harbor_Hanging_Lantern";
            lanternCube.transform.parent = beaconPost.transform;
            lanternCube.transform.localPosition = new Vector3(1.3f, 0.65f, 0f);
            lanternCube.transform.localScale = new Vector3(0.35f, 0.50f, 0.35f);
            var matBamboo = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/CaMau/M_CaMau_Bamboo.mat");
            if (matBamboo != null) lanternCube.GetComponent<MeshRenderer>().sharedMaterial = matBamboo;

            // 11. Boarding Gangplank into docked sampan
            var gangplank = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gangplank.name = "Harbor_Boarding_Gangplank";
            gangplank.transform.parent = pierRoot;
            gangplank.transform.position = new Vector3(59.35f, 0.45f, -34.0f);
            gangplank.transform.rotation = Quaternion.Euler(0f, 0f, -22.0f);
            gangplank.transform.localScale = new Vector3(2.2f, 0.08f, 1.4f);
            if (matPierWood != null) gangplank.GetComponent<MeshRenderer>().sharedMaterial = matPierWood;

            // 12. Boats & Sampans (Docked neatly along the long boarding pier)
            if (waterSys != null)
            {
                var boatsRoot = new GameObject("Harbor_Boats_And_Sampans");
                boatsRoot.transform.parent = waterSys.transform;

                var sampanOpen = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Boats/PF_Sampan_Open_Hat_Floating.prefab");
                var sampanCovered = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Boats/PF_Sampan_Covered_Floating.prefab");
                if (sampanOpen == null) sampanOpen = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Vehicles/PF_StylizedBoat.prefab");
                if (sampanCovered == null) sampanCovered = sampanOpen;

                // Left beach boats
                InstantiateBoat(sampanOpen, boatsRoot.transform, "Boat_LeftBeach_01", new Vector3(-58.0f, -0.15f, -16.0f), 1.15f, 15.0f);
                InstantiateBoat(sampanCovered, boatsRoot.transform, "Boat_LeftBeach_02", new Vector3(-51.5f, -0.22f, -17.5f), 1.20f, 22.0f);
                InstantiateBoat(sampanOpen, boatsRoot.transform, "Boat_LeftBeach_03", new Vector3(-44.0f, -0.32f, -19.5f), 1.10f, 32.0f);
                InstantiateBoat(sampanOpen, boatsRoot.transform, "Boat_LeftBeach_Skiff", new Vector3(-36.0f, -0.45f, -20.5f), 0.85f, 40.0f);
                InstantiateBoat(sampanCovered, boatsRoot.transform, "Boat_CenterBay_Moored", new Vector3(2.5f, -0.55f, -23.0f), 1.25f, -18.0f);

                // East Harbor: Docked Sampan alongside the long pier arm (easy boarding!)
                InstantiateBoat(sampanCovered, boatsRoot.transform, "Boat_RightHarbor_Docked", new Vector3(60.2f, -0.42f, -34.0f), 1.30f, 92.0f);
                // Approaching boat
                InstantiateBoat(sampanOpen, boatsRoot.transform, "Boat_RightHarbor_Approach", new Vector3(40.0f, -0.55f, -38.0f), 1.35f, -35.0f);
            }

            // 13. Props: 3D Bamboo Baskets & Fishing Nets (100% Volumetric 3D, NO flat 2D cards, ZERO crates/barrels)
            var basketPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Props/PF_Prop_Bamboo_Basket.prefab");
            var netPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Props/PF_Prop_Fishing_Net.prefab");

            if (basketPrefab != null)
            {
                InstantiateProp(basketPrefab, pierRoot, "Basket_01", new Vector3(58.0f, 1.05f, -21.0f), 1.20f);
                InstantiateProp(basketPrefab, pierRoot, "Basket_02", new Vector3(58.8f, 1.05f, -20.2f), 1.05f);
                InstantiateProp(basketPrefab, pierRoot, "Basket_03", new Vector3(52.0f, 1.05f, -14.0f), 1.15f);
                InstantiateProp(basketPrefab, pierRoot, "Basket_04", new Vector3(52.2f, 1.05f, -18.5f), 1.10f);
                InstantiateProp(basketPrefab, pierRoot, "Basket_WestPier", new Vector3(-31.5f, 0.40f, -12.5f), 1.1f);
            }

            if (netPrefab != null)
            {
                var netWest = (GameObject)PrefabUtility.InstantiatePrefab(netPrefab, pierRoot);
                netWest.name = "DryingNet_WestPlatform";
                netWest.transform.position = new Vector3(-28.0f, 0.85f, -12.5f);
                netWest.transform.localScale = Vector3.one * 1.35f;

                var netHarbor = (GameObject)PrefabUtility.InstantiatePrefab(netPrefab, pierRoot);
                netHarbor.name = "DryingNet_EastHarbor";
                netHarbor.transform.position = new Vector3(71.5f, 1.55f, -17.5f);
                netHarbor.transform.localScale = Vector3.one * 1.30f;
            }
        }

        private static void BuildPiling(Transform parent, string name, Vector3 pos, Material mat, float radius = 0.24f, float height = 1.2f)
        {
            var p = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            p.name = name;
            p.transform.parent = parent;
            p.transform.position = pos;
            p.transform.localScale = new Vector3(radius, height, radius);
            if (mat != null) p.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private static void BuildWaitingBench(Transform parent, string name, Vector3 pos, float length, float rotY, Material mat)
        {
            var benchRoot = new GameObject(name);
            benchRoot.transform.parent = parent;
            benchRoot.transform.position = pos;
            benchRoot.transform.rotation = Quaternion.Euler(0f, rotY, 0f);

            // Seat plank
            var seat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seat.name = "Seat";
            seat.transform.parent = benchRoot.transform;
            seat.transform.localPosition = new Vector3(0f, 0.32f, 0f);
            seat.transform.localScale = new Vector3(length, 0.10f, 0.70f);
            if (mat != null) seat.GetComponent<MeshRenderer>().sharedMaterial = mat;

            // Backrest
            var back = GameObject.CreatePrimitive(PrimitiveType.Cube);
            back.name = "Backrest";
            back.transform.parent = benchRoot.transform;
            back.transform.localPosition = new Vector3(0f, 0.70f, 0.30f);
            back.transform.localScale = new Vector3(length, 0.45f, 0.10f);
            if (mat != null) back.GetComponent<MeshRenderer>().sharedMaterial = mat;

            // 2 sturdy support legs
            float halfLen = length * 0.40f;
            foreach (var legX in new float[] { -halfLen, halfLen })
            {
                var leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leg.name = $"Leg_{(legX < 0 ? "L" : "R")}";
                leg.transform.parent = benchRoot.transform;
                leg.transform.localPosition = new Vector3(legX, 0.16f, 0f);
                leg.transform.localScale = new Vector3(0.18f, 0.32f, 0.60f);
                if (mat != null) leg.GetComponent<MeshRenderer>().sharedMaterial = mat;
            }
        }

        private static void BuildRailingSegment(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
        {
            var r = GameObject.CreatePrimitive(PrimitiveType.Cube);
            r.name = name;
            r.transform.parent = parent;
            r.transform.position = pos;
            r.transform.localScale = size;
            if (mat != null) r.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private static void InstantiateBoat(GameObject prefab, Transform parent, string name, Vector3 pos, float scale, float rotY)
        {
            if (prefab == null) return;
            var b = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            b.name = name;
            b.transform.position = pos;
            b.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
            b.transform.localScale = Vector3.one * scale;
        }

        private static void InstantiateProp(GameObject prefab, Transform parent, string name, Vector3 pos, float scale)
        {
            if (prefab == null) return;
            var p = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            p.name = name;
            p.transform.position = pos;
            p.transform.localScale = Vector3.one * scale;
        }
    }
}
