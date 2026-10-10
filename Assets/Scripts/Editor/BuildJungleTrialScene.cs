using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;

namespace HoaVaMau.Editor
{
    public static class BuildJungleTrialScene
    {
        public const string ScenePath = "Assets/Scenes/Scene_Tutorial_JungleTrial.unity";

        [MenuItem("Hoa va Mau/Build Jungle Tutorial Scene (Planet of Lana Style)")]
        public static void Build()
        {
            Debug.Log("<color=#38bdf8>[BuildJungleTrialScene]</color> Creating refined trial scene at: " + ScenePath);

            // 1. Create a brand new clean scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 2. Load Assets & Materials (Authentic stylized palette)
            var skySprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Sky/Sky_Cloud_Krita_5120x2160.png");
            var groundMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Ground/M_Foreground_Ground.mat")
                            ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Ground/M_Playground_Ground.mat");
            var cliffMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Rocks/M_StylizedRock_Silhouette.mat")
                           ?? groundMat;
            var timberMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/CaMau/M_CaMau_TimberDark.mat")
                            ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Props/M_Playground_Timber.mat");
            var crateMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Props/M_WoodenCrate_01.mat")
                           ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Props/M_Playground_Crate.mat");
            var ironMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Props/M_Playground_Iron.mat");
            var orbMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Props/M_Playground_Orb.mat");
            var altarMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Rocks/M_Playground_Altar.mat");
            var waterMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Water/M_StylizedWater_Sunset.mat");
            var silMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Characters/M_Character_Silhouette.mat");

            // Models & Prefabs (Strictly authentic project assets)
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/PF_Player_An.prefab");
            var heroTreePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Trees/PF_Lana_Artistic_Hero_Tree.prefab");
            var clusterTreePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Trees/PF_Branch_Cluster_Tree.prefab");
            var gocCay2Prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Trees/PF_Tree_GocCay2_Artistic.prefab");
            var stiltedThatchedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/PF_StiltedHouse_Thatched.prefab");
            var stiltedWoodPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/PF_StiltedHouse_Wood.prefab");
            var boatPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Vehicles/PF_StylizedBoat.prefab");
            var mossyBoulderPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/PF_MossyBoulder.prefab");
            var slatCubePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Props/PF_WoodenSlatCube.prefab");

            // Bush sprites
            var bushSprites = new Sprite[]
            {
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Tree/cay 1.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Tree/cay 2.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Tree/cay 3.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Tree/cay 4.png")
            };

            // 3. Setup Camera & Lighting
            GameObject camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            camGO.transform.position = new Vector3(-18f, 2.0f, -11.5f);
            var cam = camGO.AddComponent<Camera>();
            cam.fieldOfView = 36f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 120f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.18f, 0.44f, 0.72f, 1.0f);
            camGO.AddComponent<AudioListener>();
            var cineCam = camGO.AddComponent<HoaVaMau.Gameplay.Cinematic2DCamera>();
            cineCam.ConfigureLevelBounds(-26f, 85f, 0.5f, true);

            // Directional Sunlight (Warm Golden Tone)
            GameObject lightGO = new GameObject("Directional Light");
            lightGO.transform.position = new Vector3(0f, 15f, -10f);
            lightGO.transform.rotation = Quaternion.Euler(32f, 325f, 0f);
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1.0f, 0.96f, 0.90f);
            light.intensity = 1.05f;
            light.shadows = LightShadows.Soft;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.65f, 0.75f);

            // Global Volume
            GameObject volGO = new GameObject("Global Volume");
            var vol = volGO.AddComponent<Volume>();
            vol.isGlobal = true;
            var prof = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/SampleSceneProfile.asset");
            if (prof != null) vol.sharedProfile = prof;

            // Managers
            GameObject mgrGO = new GameObject("Gameplay_Managers");
            var cpMgr = mgrGO.AddComponent<HoaVaMau.Gameplay.CheckpointManager>();
            cpMgr.SetCheckpoint(new Vector3(-18f, 0.05f, 0f), 0);
            mgrGO.AddComponent<HoaVaMau.Gameplay.WindManager>();
            mgrGO.AddComponent<HoaVaMau.Gameplay.GrassManager>();

            // 4. Parallax Background System (Multi-depth layers after layers)
            GameObject parallaxRoot = new GameObject("--- Parallax_Background_System ---");
            var parallaxCtrl = parallaxRoot.AddComponent<HoaVaMau.ParallaxController>();
            parallaxCtrl.enableAutoPan = false;

            // Layer 0: User Hero Sky Art (Z = 45) - Wide, magnificent single hero canvas
            GameObject skyLayer = new GameObject("Layer_0_Sky_UserArt");
            skyLayer.transform.SetParent(parallaxRoot.transform);
            skyLayer.transform.position = new Vector3(25f, 15.0f, 45f);
            if (skySprite != null)
            {
                GameObject skySlice = new GameObject("Sky_Cloud_HeroCanvas");
                skySlice.transform.SetParent(skyLayer.transform);
                skySlice.transform.localPosition = Vector3.zero;
                skySlice.transform.localScale = new Vector3(3.6f, 2.8f, 1f);
                var sr = skySlice.AddComponent<SpriteRenderer>();
                sr.sprite = skySprite;
                sr.sortingOrder = -500;
            }
            parallaxCtrl.AddLayer("Sky_Clouds", skyLayer.transform, 0.015f, 0.002f, 130f);

            // Layer 2: Midground River & Stilted Village Backdrop (Z = 16)
            GameObject midJungleLayer = new GameObject("Layer_2_Midground_River_Village");
            midJungleLayer.transform.SetParent(parallaxRoot.transform);
            midJungleLayer.transform.position = new Vector3(25f, 0f, 16f);

            // Water surface
            GameObject waterPlane = GameObject.CreatePrimitive(PrimitiveType.Cube);
            waterPlane.name = "Distant_River_Surface";
            waterPlane.transform.SetParent(midJungleLayer.transform);
            waterPlane.transform.localPosition = new Vector3(0f, -2.6f, 0f);
            waterPlane.transform.localScale = new Vector3(160f, 0.2f, 18f);
            if (waterMat != null) waterPlane.GetComponent<MeshRenderer>().sharedMaterial = waterMat;
            Object.DestroyImmediate(waterPlane.GetComponent<Collider>());

            // Distant Stilted Houses & Boats along the river
            if (stiltedThatchedPrefab != null)
            {
                var h0 = (GameObject)PrefabUtility.InstantiatePrefab(stiltedThatchedPrefab, midJungleLayer.transform);
                h0.transform.localPosition = new Vector3(-43f, -0.6f, 2.0f);
                h0.transform.localScale = new Vector3(1.7f, 1.7f, 1.7f);

                var h1 = (GameObject)PrefabUtility.InstantiatePrefab(stiltedThatchedPrefab, midJungleLayer.transform);
                h1.transform.localPosition = new Vector3(-8f, -0.6f, 2.0f);
                h1.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);

                var h2 = (GameObject)PrefabUtility.InstantiatePrefab(stiltedThatchedPrefab, midJungleLayer.transform);
                h2.transform.localPosition = new Vector3(45f, -0.4f, 1.5f);
                h2.transform.localScale = new Vector3(1.6f, 1.6f, 1.6f);
            }
            if (stiltedWoodPrefab != null)
            {
                var h3 = (GameObject)PrefabUtility.InstantiatePrefab(stiltedWoodPrefab, midJungleLayer.transform);
                h3.transform.localPosition = new Vector3(-18f, -0.5f, 1.0f);
                h3.transform.localScale = new Vector3(2.0f, 2.0f, 2.0f);

                var h4 = (GameObject)PrefabUtility.InstantiatePrefab(stiltedWoodPrefab, midJungleLayer.transform);
                h4.transform.localPosition = new Vector3(18f, -0.5f, 1.0f);
                h4.transform.localScale = new Vector3(2.0f, 2.0f, 2.0f);
            }
            if (boatPrefab != null)
            {
                var b0 = (GameObject)PrefabUtility.InstantiatePrefab(boatPrefab, midJungleLayer.transform);
                b0.transform.localPosition = new Vector3(-38f, -2.2f, -1.0f);
                b0.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);

                var b1 = (GameObject)PrefabUtility.InstantiatePrefab(boatPrefab, midJungleLayer.transform);
                b1.transform.localPosition = new Vector3(5f, -2.2f, -1.0f);
                b1.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);

                var b2 = (GameObject)PrefabUtility.InstantiatePrefab(boatPrefab, midJungleLayer.transform);
                b2.transform.localPosition = new Vector3(32f, -2.0f, 1.5f);
                b2.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
            }
            // Swaying trees along the river bank
            if (clusterTreePrefab != null)
            {
                var t0 = (GameObject)PrefabUtility.InstantiatePrefab(clusterTreePrefab, midJungleLayer.transform);
                t0.transform.localPosition = new Vector3(-35f, -2.4f, -0.5f);
                t0.transform.localScale = new Vector3(1.1f, 1.1f, 1f);

                var t1 = (GameObject)PrefabUtility.InstantiatePrefab(clusterTreePrefab, midJungleLayer.transform);
                t1.transform.localPosition = new Vector3(8f, -2.4f, -0.5f);
                t1.transform.localScale = new Vector3(1.0f, 1.0f, 1f);

                var t2 = (GameObject)PrefabUtility.InstantiatePrefab(clusterTreePrefab, midJungleLayer.transform);
                t2.transform.localPosition = new Vector3(48f, -2.4f, -0.5f);
                t2.transform.localScale = new Vector3(1.1f, 1.1f, 1f);
            }
            if (gocCay2Prefab != null)
            {
                var t3 = (GameObject)PrefabUtility.InstantiatePrefab(gocCay2Prefab, midJungleLayer.transform);
                t3.transform.localPosition = new Vector3(-16f, -2.4f, -1.0f);
                t3.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

                var t4 = (GameObject)PrefabUtility.InstantiatePrefab(gocCay2Prefab, midJungleLayer.transform);
                t4.transform.localPosition = new Vector3(28f, -2.4f, -1.0f);
                t4.transform.localScale = new Vector3(0.95f, 0.95f, 1f);
            }
            parallaxCtrl.AddLayer("Midground_River_Village", midJungleLayer.transform, 0.22f, 0.03f, 40f);

            // 5. Playable Gameplay Stage (Z = 0.0)
            GameObject stageRoot = new GameObject("--- Playable_Jungle_Stage ---");

            // STATION 1: PEACEFUL STARTING CLEARING (X = -22 to -10)
            BuildPlatformBox(stageRoot.transform, "Station1_Ground", new Vector3(-15f, -2.0f, 0f), new Vector3(18f, 4.0f, 4f), groundMat);
            BuildSign(stageRoot.transform, "Sign_Station1", new Vector3(-19.5f, 0.05f, 0.2f),
                "TRẠM 1: LÀNG QUÊ THANH BÌNH\n[A]/[D] Đi lại | [Shift] Chạy nhanh (Sprint)\n[Space] Nhảy | Chạy lấy đà + [Space] nhảy xa vượt trội!", timberMat);
            BuildGrassStrip(stageRoot.transform, "St1_Grass", -21f, -11f, 0.05f);
            BuildBushes(stageRoot.transform, "St1_Bushes", -20f, -12f, 0.1f, bushSprites);
            BuildCheckpoint(stageRoot.transform, "CP_Station1", new Vector3(-18f, 0.05f, 0f), 0);

            if (heroTreePrefab != null)
            {
                var startTree = (GameObject)PrefabUtility.InstantiatePrefab(heroTreePrefab, stageRoot.transform);
                startTree.transform.position = new Vector3(-21f, -0.2f, 1.8f);
                startTree.transform.localScale = new Vector3(0.85f, 0.85f, 1f);
            }
            if (mossyBoulderPrefab != null)
            {
                var rock1 = (GameObject)PrefabUtility.InstantiatePrefab(mossyBoulderPrefab, stageRoot.transform);
                rock1.transform.position = new Vector3(-12.5f, 0.0f, 0.8f);
                rock1.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);

                var rock2 = (GameObject)PrefabUtility.InstantiatePrefab(mossyBoulderPrefab, stageRoot.transform);
                rock2.transform.position = new Vector3(-16.0f, 0.0f, 1.1f);
                rock2.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);
            }

            // STATION 2: TIERED NATURAL LEDGE CLIMB (X = -8 to 4)
            BuildPlatformBox(stageRoot.transform, "Station2_Ledge_Tier1", new Vector3(-4f, -0.6f, 0f), new Vector3(6f, 4.8f, 4f), cliffMat);
            BuildPlatformBox(stageRoot.transform, "Station2_Ledge_Tier2", new Vector3(1.5f, 0.4f, 0f), new Vector3(5f, 6.8f, 4f), cliffMat);
            BuildSign(stageRoot.transform, "Sign_Station2", new Vector3(-6.5f, 1.85f, 0.2f),
                "TRẠM 2: BÁM VÁCH ĐÁ TỰ NHIÊN\nNhảy bắt mép vách -> Bấm [W] để leo trèo lên\n[S] Buông tay rơi xuống", timberMat);
            BuildGrassStrip(stageRoot.transform, "St2_GrassT1", -6.5f, -1.5f, 1.85f);
            BuildGrassStrip(stageRoot.transform, "St2_GrassT2", -0.5f, 3.5f, 3.85f);
            BuildBushes(stageRoot.transform, "St2_Bushes", -1.0f, 3.0f, 3.9f, bushSprites);
            BuildCheckpoint(stageRoot.transform, "CP_Station2", new Vector3(1.5f, 3.85f, 0f), 1);

            if (mossyBoulderPrefab != null)
            {
                var rockBase = (GameObject)PrefabUtility.InstantiatePrefab(mossyBoulderPrefab, stageRoot.transform);
                rockBase.transform.position = new Vector3(-7.2f, 0.2f, 0.6f);
                rockBase.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);

                var rockTier = (GameObject)PrefabUtility.InstantiatePrefab(mossyBoulderPrefab, stageRoot.transform);
                rockTier.transform.position = new Vector3(-1.1f, 2.0f, 0.6f);
                rockTier.transform.localScale = new Vector3(0.95f, 0.95f, 0.95f);
            }
            if (gocCay2Prefab != null)
            {
                var ledgeRoots = (GameObject)PrefabUtility.InstantiatePrefab(gocCay2Prefab, stageRoot.transform);
                ledgeRoots.transform.position = new Vector3(3.8f, 3.8f, 1.0f);
                ledgeRoots.transform.localScale = new Vector3(0.85f, 0.85f, 1.0f);
            }

            // STATION 3: LOW JUNGLE ROOT CAVE (X = 4 to 16)
            BuildPlatformBox(stageRoot.transform, "Tunnel_Floor", new Vector3(10f, 0.4f, 0f), new Vector3(12f, 6.8f, 4f), groundMat);
            BuildPlatformBox(stageRoot.transform, "Tunnel_Ceiling", new Vector3(10f, 6.35f, 0f), new Vector3(8.5f, 3.0f, 4f), cliffMat);
            BuildPlatformBox(stageRoot.transform, "Tunnel_Arch_L", new Vector3(5.75f, 5.5f, 0.5f), new Vector3(1.2f, 3.6f, 2.5f), cliffMat);
            BuildPlatformBox(stageRoot.transform, "Tunnel_Arch_R", new Vector3(14.25f, 5.5f, 0.5f), new Vector3(1.2f, 3.6f, 2.5f), cliffMat);
            BuildSign(stageRoot.transform, "Sign_Station3", new Vector3(-0.2f, 3.85f, 0.2f),
                "TRẠM 3: HẦM RỄ CÂY RỪNG SÂU\n(Không tự động ngồi)\nBấm [S] / [C] / [Ctrl] để ngồi xổm & bò trườn qua hầm thấp!", timberMat);
            BuildCheckpoint(stageRoot.transform, "CP_Station3", new Vector3(15.0f, 3.85f, 0f), 2);

            if (gocCay2Prefab != null)
            {
                var caveRootsIn = (GameObject)PrefabUtility.InstantiatePrefab(gocCay2Prefab, stageRoot.transform);
                caveRootsIn.transform.position = new Vector3(5.6f, 3.8f, 1.0f);
                caveRootsIn.transform.localScale = new Vector3(0.9f, 0.9f, 1.0f);

                var caveRootsOut = (GameObject)PrefabUtility.InstantiatePrefab(gocCay2Prefab, stageRoot.transform);
                caveRootsOut.transform.position = new Vector3(14.4f, 3.8f, 1.0f);
                caveRootsOut.transform.localScale = new Vector3(0.9f, 0.9f, 1.0f);
            }
            if (mossyBoulderPrefab != null)
            {
                var rockCaveIn = (GameObject)PrefabUtility.InstantiatePrefab(mossyBoulderPrefab, stageRoot.transform);
                rockCaveIn.transform.position = new Vector3(5.2f, 3.8f, 0.7f);
                rockCaveIn.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);

                var rockCaveOut = (GameObject)PrefabUtility.InstantiatePrefab(mossyBoulderPrefab, stageRoot.transform);
                rockCaveOut.transform.position = new Vector3(14.8f, 3.8f, 0.7f);
                rockCaveOut.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);
            }

            // STATION 4: HEAVY CRATE DRAG PUZZLE (X = 18 to 34)
            BuildPlatformBox(stageRoot.transform, "CrateZone_Floor", new Vector3(23f, -1.5f, 0f), new Vector3(10f, 4.0f, 4f), groundMat);
            BuildPlatformBox(stageRoot.transform, "CrateZone_HighCliff", new Vector3(31f, 0.5f, 0f), new Vector3(6f, 8.0f, 4f), cliffMat);
            BuildSign(stageRoot.transform, "Sign_Station4", new Vector3(18.5f, 0.55f, 0.2f),
                "TRẠM 4: KÉO & ĐẨY THÙNG GỖ\nGiữ [Chuột Trái] + [A]/[D] để kéo/đẩy thùng áp sát vách\nNhảy lên mặt thùng để leo qua vách đá cao!", timberMat);
            BuildGrassStrip(stageRoot.transform, "St4_Grass", 18.5f, 27.5f, 0.55f);
            BuildBushes(stageRoot.transform, "St4_Bushes", 19f, 26f, 0.6f, bushSprites);

            BuildInteractiveCrate(stageRoot.transform, new Vector3(21.5f, 0.55f, 0f), slatCubePrefab);
            BuildCheckpoint(stageRoot.transform, "CP_Station4", new Vector3(31f, 4.55f, 0f), 3);

            if (mossyBoulderPrefab != null)
            {
                var cliffRock1 = (GameObject)PrefabUtility.InstantiatePrefab(mossyBoulderPrefab, stageRoot.transform);
                cliffRock1.transform.position = new Vector3(27.8f, 0.6f, 0.8f);
                cliffRock1.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);

                var cliffRock2 = (GameObject)PrefabUtility.InstantiatePrefab(mossyBoulderPrefab, stageRoot.transform);
                cliffRock2.transform.position = new Vector3(28.2f, 2.5f, 0.9f);
                cliffRock2.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
            }
            if (gocCay2Prefab != null)
            {
                var cliffTree = (GameObject)PrefabUtility.InstantiatePrefab(gocCay2Prefab, stageRoot.transform);
                cliffTree.transform.position = new Vector3(33.6f, 4.5f, 1.1f);
                cliffTree.transform.localScale = new Vector3(0.95f, 0.95f, 1.0f);
            }

            // STATION 5: JUNGLE RAVINE & SWINGING ROPE (X = 34 to 52)
            BuildPlatformBox(stageRoot.transform, "Rope_OverheadBeam", new Vector3(41f, 10.2f, 0f), new Vector3(10f, 0.5f, 1.6f), timberMat);
            BuildPlatformBox(stageRoot.transform, "Rope_Pillar_L", new Vector3(36.5f, 7.3f, 0.6f), new Vector3(0.5f, 5.5f, 0.5f), timberMat);
            BuildPlatformBox(stageRoot.transform, "Rope_Pillar_R", new Vector3(45.5f, 7.3f, 0.6f), new Vector3(0.5f, 5.5f, 0.5f), timberMat);
            BuildPlatformBox(stageRoot.transform, "Rope_LandingCliff", new Vector3(49f, 0.5f, 0f), new Vector3(6f, 8.0f, 4f), cliffMat);

            BuildPitHazard(stageRoot.transform, "Ravine_PitHazard", new Vector3(41f, -3.5f, 0f), new Vector3(12f, 3.0f, 6f));
            BuildVerletRope(stageRoot.transform, "VerletRope_Ravine", new Vector3(41f, 10.0f, 0f), 5.6f);
            BuildSign(stageRoot.transform, "Sign_Station5", new Vector3(33.5f, 4.55f, 0.2f),
                "TRẠM 5: ĐU DÂY THỪNG QUA VỰC\nTiến gần/nhảy đến bấm [F] để bám dây | [W]/[S] trèo lên xuống\n[A]/[D] đu nhấp lấy đà quán tính | [Space] phóng qua vực / [F] thả rơi", timberMat);
            BuildGrassStrip(stageRoot.transform, "St5_Grass", 46.5f, 51.5f, 4.55f);
            BuildCheckpoint(stageRoot.transform, "CP_Station5", new Vector3(48.5f, 4.55f, 0f), 4);

            if (mossyBoulderPrefab != null)
            {
                var rockTakeoff = (GameObject)PrefabUtility.InstantiatePrefab(mossyBoulderPrefab, stageRoot.transform);
                rockTakeoff.transform.position = new Vector3(33.8f, 4.2f, 0.7f);
                rockTakeoff.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);

                var rockLanding = (GameObject)PrefabUtility.InstantiatePrefab(mossyBoulderPrefab, stageRoot.transform);
                rockLanding.transform.position = new Vector3(46.2f, 4.2f, 0.7f);
                rockLanding.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
            }

            // STATION 6: AIM & THROW RANGE + DESTRUCTIBLE TARGET (X = 52 to 68)
            BuildPlatformBox(stageRoot.transform, "Throw_Floor", new Vector3(58f, 0.5f, 0f), new Vector3(12f, 8.0f, 4f), groundMat);
            BuildPlatformBox(stageRoot.transform, "Throw_Pedestal", new Vector3(54f, 4.8f, 0f), new Vector3(1.6f, 0.6f, 1.2f), altarMat);

            BuildThrowableItem(stageRoot.transform, "Throwable_Orb_1", new Vector3(53.6f, 5.35f, 0f), orbMat);
            BuildThrowableItem(stageRoot.transform, "Throwable_Orb_2", new Vector3(54.4f, 5.35f, 0f), orbMat);

            BuildPlatformBox(stageRoot.transform, "Throw_TargetPedestal", new Vector3(63.5f, 4.2f, 0f), new Vector3(3.2f, 7.4f, 3.5f), cliffMat);
            BuildDestructibleCrateTarget(stageRoot.transform, new Vector3(63.5f, 7.95f, 0f), slatCubePrefab);

            BuildSign(stageRoot.transform, "Sign_Station6", new Vector3(52.2f, 4.55f, 0.2f),
                "TRẠM 6: CẦM & NÉM ĐỒ VẬT\n[E] Nhặt vật | Giữ [Chuột Trái] canh quỹ đạo Parabol (xa/gần)\nThả để Ném | Ném trúng 3 lần thùng gỗ sẽ vỡ vụn mảnh PhysX!", timberMat);
            BuildGrassStrip(stageRoot.transform, "St6_Grass", 52.5f, 59.5f, 4.55f);
            BuildBushes(stageRoot.transform, "St6_Bushes", 53f, 59f, 4.6f, bushSprites);
            BuildCheckpoint(stageRoot.transform, "CP_Station6", new Vector3(57.0f, 4.55f, 0f), 5);

            if (mossyBoulderPrefab != null)
            {
                var rockTargetBase = (GameObject)PrefabUtility.InstantiatePrefab(mossyBoulderPrefab, stageRoot.transform);
                rockTargetBase.transform.position = new Vector3(63.5f, 1.5f, 0.8f);
                rockTargetBase.transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);
            }

            // STATION 7: SCENIC JUNGLE LOOKOUT & VICTORY (X = 68 to 82)
            BuildPlatformBox(stageRoot.transform, "Goal_Cliff", new Vector3(74f, 0.5f, 0f), new Vector3(16f, 8.0f, 4f), groundMat);
            if (heroTreePrefab != null)
            {
                var goalTree = (GameObject)PrefabUtility.InstantiatePrefab(heroTreePrefab, stageRoot.transform);
                goalTree.transform.position = new Vector3(77f, 4.3f, 1.2f);
                goalTree.transform.localScale = new Vector3(1.0f, 1.0f, 1f);
            }
            BuildGrassStrip(stageRoot.transform, "St7_Grass", 66.5f, 80.5f, 4.55f);
            BuildBushes(stageRoot.transform, "St7_Bushes", 67f, 79f, 4.6f, bushSprites);

            GameObject goalTriggerGO = new GameObject("Tutorial_Goal_Trigger");
            goalTriggerGO.transform.SetParent(stageRoot.transform);
            goalTriggerGO.transform.position = new Vector3(75f, 5.5f, 0f);
            var goalCol = goalTriggerGO.AddComponent<BoxCollider>();
            goalCol.isTrigger = true;
            goalCol.size = new Vector3(3.0f, 4.0f, 4.0f);
            goalTriggerGO.AddComponent<HoaVaMau.Gameplay.LevelGoalTrigger>();

            BuildSign(stageRoot.transform, "Sign_Goal", new Vector3(69.0f, 4.55f, 0.2f),
                "HOÀN THÀNH BÀI TẬP LUYỆN (TRIAL SCENE)\nChúc mừng bạn đã làm chủ toàn bộ kỹ năng:\nĐi, Chạy, Nhảy xa, Bám trèo vách, Ngồi bò trườn,\nKéo/Đẩy thùng gỗ, Đu dây thừng & Canh ném phá vỡ!", timberMat);

            if (mossyBoulderPrefab != null)
            {
                var goalRock1 = (GameObject)PrefabUtility.InstantiatePrefab(mossyBoulderPrefab, stageRoot.transform);
                goalRock1.transform.position = new Vector3(66.8f, 4.2f, 0.8f);
                goalRock1.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);

                var goalRock2 = (GameObject)PrefabUtility.InstantiatePrefab(mossyBoulderPrefab, stageRoot.transform);
                goalRock2.transform.position = new Vector3(81.5f, 4.2f, 0.8f);
                goalRock2.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
            }

            // PROTAGONIST AN
            if (playerPrefab != null)
            {
                var playerInstance = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, stageRoot.transform);
                playerInstance.name = "Player_An";
                playerInstance.transform.position = new Vector3(-18f, 0.05f, 0f);
                cineCam.Target = playerInstance.transform;

                var smr = playerInstance.GetComponentInChildren<SkinnedMeshRenderer>();
                if (smr != null && silMat != null)
                {
                    smr.sharedMaterial = silMat;
                }

                var pCtrl = playerInstance.GetComponent<HoaVaMau.Gameplay.PlayerController>();
                if (pCtrl != null)
                {
                    var so = new SerializedObject(pCtrl);
                    var autoClimbProp = so.FindProperty("autoClimbOnLedgeGrab");
                    if (autoClimbProp != null) autoClimbProp.boolValue = false;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            // Foreground framing foliage (Z = -2.2)
            GameObject fgRoot = new GameObject("Layer_4_Foreground_Framing");
            fgRoot.transform.SetParent(parallaxRoot.transform);
            fgRoot.transform.position = new Vector3(25f, 0f, -2.2f);
            parallaxCtrl.AddLayer("Foreground_Framing", fgRoot.transform, 1.15f, 0.02f, 40f);

            // Save
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log("<color=#4ade80>[BuildJungleTrialScene]</color> SUCCESS: Scene created at " + ScenePath);
        }

        private static void BuildPlatformBox(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.transform.localScale = size;
            if (mat != null) go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private static void BuildSign(Transform parent, string name, Vector3 pos, string text, Material timberMat)
        {
            GameObject signRoot = new GameObject(name);
            signRoot.transform.SetParent(parent);
            signRoot.transform.position = pos;

            // Clean rustic wooden board backing
            GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "Sign_Board";
            board.transform.SetParent(signRoot.transform);
            board.transform.localPosition = new Vector3(2.5f, 0.65f, 0.15f);
            board.transform.localScale = new Vector3(5.2f, 1.35f, 0.08f);
            if (timberMat != null) board.GetComponent<MeshRenderer>().sharedMaterial = timberMat;
            Object.DestroyImmediate(board.GetComponent<Collider>());

            // Wooden posts
            GameObject postL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            postL.name = "Sign_Post_L";
            postL.transform.SetParent(signRoot.transform);
            postL.transform.localPosition = new Vector3(0.3f, 0.1f, 0.15f);
            postL.transform.localScale = new Vector3(0.09f, 1.4f, 0.09f);
            if (timberMat != null) postL.GetComponent<MeshRenderer>().sharedMaterial = timberMat;
            Object.DestroyImmediate(postL.GetComponent<Collider>());

            GameObject postR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            postR.name = "Sign_Post_R";
            postR.transform.SetParent(signRoot.transform);
            postR.transform.localPosition = new Vector3(4.7f, 0.1f, 0.15f);
            postR.transform.localScale = new Vector3(0.09f, 1.4f, 0.09f);
            if (timberMat != null) postR.GetComponent<MeshRenderer>().sharedMaterial = timberMat;
            Object.DestroyImmediate(postR.GetComponent<Collider>());

            // Clear high-contrast text on board
            GameObject textGO = new GameObject("Sign_Text");
            textGO.transform.SetParent(signRoot.transform);
            textGO.transform.localPosition = new Vector3(0.15f, 1.18f, 0.05f);

            var tm = textGO.AddComponent<TextMesh>();
            tm.text = text;
            tm.fontSize = 26;
            tm.characterSize = 0.038f;
            tm.anchor = TextAnchor.UpperLeft;
            tm.alignment = TextAlignment.Left;
            tm.color = new Color(0.98f, 0.96f, 0.90f, 1.0f);
        }

        private static void BuildBushes(Transform parent, string groupName, float startX, float endX, float y, Sprite[] bushSprites)
        {
            if (bushSprites == null || bushSprites.Length == 0) return;
            GameObject group = new GameObject(groupName);
            group.transform.SetParent(parent);
            int idx = 0;
            for (float x = startX; x <= endX; x += 2.4f)
            {
                var sprite = bushSprites[idx % bushSprites.Length];
                if (sprite == null) continue;

                GameObject bush = new GameObject($"{groupName}_{idx}");
                bush.transform.SetParent(group.transform);
                bush.transform.position = new Vector3(x + Random.Range(-0.3f, 0.3f), y, Random.Range(0.2f, 0.7f));
                bush.transform.localScale = new Vector3(0.35f, 0.35f, 1f);

                var sr = bush.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingOrder = -10;

                var sway = bush.AddComponent<HoaVaMau.Gameplay.PlanetOfLanaFoliageSway>();
                var so = new SerializedObject(sway);
                so.FindProperty("maxRotationAngle").floatValue = 2.2f;
                so.FindProperty("swaySpeed").floatValue = 1.9f;
                so.FindProperty("useWorldPositionOffset").boolValue = true;
                so.FindProperty("harmonicNoise").floatValue = 0.35f;
                so.FindProperty("scaleBreathing").floatValue = 0.015f;
                so.ApplyModifiedPropertiesWithoutUndo();

                idx++;
            }
        }

        private static void BuildGrassStrip(Transform parent, string prefix, float startX, float endX, float y)
        {
            GameObject group = new GameObject(prefix + "_Patch");
            group.transform.SetParent(parent);
            int idx = 0;
            for (float x = startX; x <= endX; x += 0.65f)
            {
                GameObject tuftGO = new GameObject($"{prefix}_{idx:D2}");
                tuftGO.transform.SetParent(group.transform);
                tuftGO.transform.position = new Vector3(x + Random.Range(-0.15f, 0.15f), y, Random.Range(-0.35f, 0.35f));
                tuftGO.transform.localRotation = Quaternion.Euler(0f, Random.Range(-40f, 40f), 0f);

                var tuft = tuftGO.AddComponent<HoaVaMau.Gameplay.Stylized3DGrassTuft>();
                var style = (idx % 2 == 0) ? HoaVaMau.Gameplay.TuftMeshStyle.CurvedFan : HoaVaMau.Gameplay.TuftMeshStyle.StarCards;
                var variant = (idx % 3 == 0) ? HoaVaMau.Gameplay.TuftTextureVariant.Leaning : HoaVaMau.Gameplay.TuftTextureVariant.Upright;
                tuft.Setup(style, variant, Random.Range(0.55f, 0.72f), Random.Range(0.40f, 0.55f));
                idx++;
            }
        }

        private static void BuildCheckpoint(Transform parent, string name, Vector3 pos, int index)
        {
            GameObject cp = new GameObject(name);
            cp.transform.SetParent(parent);
            cp.transform.position = pos;

            var box = cp.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(2.5f, 4.0f, 4.0f);
            box.center = new Vector3(0f, 2.0f, 0f);

            var zone = cp.AddComponent<HoaVaMau.Gameplay.CheckpointZone>();
            zone.checkpointIndex = index;
        }

        private static void BuildPitHazard(Transform parent, string name, Vector3 pos, Vector3 size)
        {
            GameObject pit = new GameObject(name);
            pit.transform.SetParent(parent);
            pit.transform.position = pos;
            var box = pit.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = size;
            pit.AddComponent<HoaVaMau.Gameplay.PitHazardZone>();
        }

        private static void BuildVerletRope(Transform parent, string name, Vector3 pos, float length)
        {
            GameObject ropeGO = new GameObject(name);
            ropeGO.transform.SetParent(parent);
            ropeGO.transform.position = pos;

            var rope = ropeGO.AddComponent<HoaVaMau.Gameplay.RopeController>();
            var so = new SerializedObject(rope);
            so.FindProperty("totalLength").floatValue = length;
            so.FindProperty("nodeCount").intValue = Mathf.Max(12, Mathf.RoundToInt(length * 3.2f));
            so.FindProperty("ropeVisualWidth").floatValue = 0.11f;
            so.ApplyModifiedPropertiesWithoutUndo();

            rope.BuildRope();
        }

        private static void BuildInteractiveCrate(Transform parent, Vector3 pos, GameObject visualPrefab)
        {
            GameObject crateGO = new GameObject("Interactable_Crate");
            crateGO.transform.SetParent(parent);
            crateGO.transform.position = pos;

            var col = crateGO.AddComponent<BoxCollider>();
            col.size = new Vector3(1.4f, 1.4f, 1.0f);
            col.center = new Vector3(0f, 0.7f, 0f);

            var rb = crateGO.AddComponent<Rigidbody>();
            rb.mass = 60f;
            rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;

            crateGO.AddComponent<HoaVaMau.Gameplay.InteractableCrate>();
            crateGO.AddComponent<HoaVaMau.Gameplay.DestructibleCrate>();

            if (visualPrefab != null)
            {
                var vis = (GameObject)PrefabUtility.InstantiatePrefab(visualPrefab, crateGO.transform);
                vis.name = "Visual_Body";
                vis.transform.localPosition = new Vector3(0f, 0.7f, 0f);
                vis.transform.localScale = new Vector3(70f, 70f, 70f);
                var colliders = vis.GetComponentsInChildren<Collider>();
                foreach (var c in colliders) Object.DestroyImmediate(c);
            }
        }

        private static void BuildDestructibleCrateTarget(Transform parent, Vector3 pos, GameObject visualPrefab)
        {
            GameObject crateGO = new GameObject("Destructible_Crate_Target");
            crateGO.transform.SetParent(parent);
            crateGO.transform.position = pos;

            var col = crateGO.AddComponent<BoxCollider>();
            col.size = new Vector3(1.4f, 1.4f, 1.0f);
            col.center = new Vector3(0f, 0.7f, 0f);

            var rb = crateGO.AddComponent<Rigidbody>();
            rb.mass = 45f;
            rb.constraints = RigidbodyConstraints.FreezePositionZ;

            crateGO.AddComponent<HoaVaMau.Gameplay.DestructibleCrate>();
            crateGO.AddComponent<HoaVaMau.Gameplay.InteractableCrate>();

            if (visualPrefab != null)
            {
                var vis = (GameObject)PrefabUtility.InstantiatePrefab(visualPrefab, crateGO.transform);
                vis.name = "Visual_Body";
                vis.transform.localPosition = new Vector3(0f, 0.7f, 0f);
                vis.transform.localScale = new Vector3(70f, 70f, 70f);
                var colliders = vis.GetComponentsInChildren<Collider>();
                foreach (var c in colliders) Object.DestroyImmediate(c);
            }
        }

        private static void BuildThrowableItem(Transform parent, string name, Vector3 pos, Material orbMat)
        {
            GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.name = name;
            orb.transform.SetParent(parent);
            orb.transform.position = pos;
            orb.transform.localScale = new Vector3(0.38f, 0.38f, 0.38f);
            if (orbMat != null) orb.GetComponent<MeshRenderer>().sharedMaterial = orbMat;

            var rb = orb.AddComponent<Rigidbody>();
            rb.mass = 2.0f;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            orb.AddComponent<HoaVaMau.Gameplay.PortableItem>();
        }
    }
}
