using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using HoaVaMau.Environment;
using HoaVaMau.Gameplay;

namespace HoaVaMau.Editor
{
    public static class BuildCaMauDock1954Scene
    {
        public const string ScenePath = "Assets/Scenes/CaMau_Dock_1954.unity";
        private const string MaterialsPath = "Assets/Materials";
        private const string LightingPath = "Assets/Lighting";
        private const string PrefabsPath = "Assets/Prefabs";

        [MenuItem("Hoa va Mau/Build Ca Mau Dock 1954 Scene (Full 3D Cinematic Level)")]
        public static void BuildScene()
        {
            if (!Directory.Exists("Assets/Scenes")) Directory.CreateDirectory("Assets/Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ==========================================
            // ROOT: CAMAU_DOCK_1954
            // ==========================================
            GameObject root = new GameObject("CAMAU_DOCK_1954");

            // ==========================================
            // 3D MATERIALS (AUTHENTIC CÀ MAU 1954)
            // ==========================================
            Material timberDarkMat = GetOrCreateMaterial("M_CaMau_TimberDark", new Color(0.24f, 0.17f, 0.11f), 0.12f);
            Material timberLightMat = GetOrCreateMaterial("M_CaMau_TimberLight", new Color(0.42f, 0.31f, 0.20f), 0.10f);
            Material pilingMat = GetOrCreateMaterial("M_CaMau_WetPiling", new Color(0.14f, 0.11f, 0.08f), 0.28f);
            Material bambooMat = GetOrCreateMaterial("M_CaMau_Bamboo", new Color(0.70f, 0.56f, 0.32f), 0.15f);
            Material thatchRoofMat = GetOrCreateMaterial("M_CaMau_ThatchRoof", new Color(0.42f, 0.31f, 0.18f), 0.06f);
            Material tileRoofMat = GetOrCreateMaterial("M_CaMau_OldTileRoof", new Color(0.50f, 0.22f, 0.16f), 0.15f);
            Material colonialWallMat = GetOrCreateMaterial("M_CaMau_ColonialWall", new Color(0.76f, 0.68f, 0.48f), 0.18f);
            Material colonialShutterMat = GetOrCreateMaterial("M_CaMau_ColonialShutter", new Color(0.15f, 0.25f, 0.20f), 0.20f);
            Material boatHullMat = GetOrCreateMaterial("M_CaMau_BoatHull", new Color(0.22f, 0.15f, 0.10f), 0.22f);
            Material boatSailMat = GetOrCreateMaterial("M_CaMau_BoatSail", new Color(0.68f, 0.28f, 0.16f), 0.05f);
            Material ropeMat = GetOrCreateMaterial("M_CaMau_Rope", new Color(0.58f, 0.48f, 0.33f), 0.05f);
            Material mangroveBarkMat = GetOrCreateMaterial("M_CaMau_MangroveBark", new Color(0.18f, 0.14f, 0.10f), 0.10f);
            Material mangroveLeafMat = GetOrCreateMaterial("M_CaMau_MangroveLeaves", new Color(0.15f, 0.26f, 0.12f), 0.18f);
            Material palmLeafMat = GetOrCreateMaterial("M_CaMau_PalmLeaves", new Color(0.14f, 0.25f, 0.11f), 0.20f);
            Material riverMudMat = GetOrCreateMaterial("M_CaMau_RiverMud", new Color(0.04f, 0.08f, 0.10f), 0.40f);

            Material lanternGlowMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/M_Boat_Lantern.mat");
            if (lanternGlowMat == null) lanternGlowMat = GetOrCreateMaterial("M_Boat_Lantern", new Color(1.0f, 0.85f, 0.45f), 0.9f);
            Material motesMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/M_GoldenMotes.mat");
            Material godRaysMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/M_StylizedGodRays.mat");

            // Configure Sky & Water Materials
            Material skyMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/M_StylizedSky_Sunset.mat");
            ConfigureSunsetSkyMaterial(skyMat);

            Material waterMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/M_StylizedWater_Sunset.mat");
            ConfigureSunsetWaterMaterial(waterMat, -3.5f);

            // ==========================================
            // 1. CAMERA (LOCKED SIDE-VIEW CINEMATIC 2.5D PERSPECTIVE)
            // ==========================================
            GameObject cameraGroup = CreateChild(root, "Camera");
            GameObject camObj = new GameObject("GameplaySideCamera");
            camObj.tag = "MainCamera";
            camObj.transform.SetParent(cameraGroup.transform, false);

            // Centered framing capturing Left Dock (Boy), Jump Gap, River Junks, Sun, and Village
            camObj.transform.position = new Vector3(-1.0f, 1.30f, -24.0f);
            camObj.transform.rotation = Quaternion.Euler(2.5f, 0f, 0f);

            Camera cam = camObj.AddComponent<Camera>();
            cam.orthographic = false;
            cam.fieldOfView = 26.5f; // Cinematic perspective framing exactly 20m horizontal level width
            cam.nearClipPlane = 0.2f;
            cam.farClipPlane = 160f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.16f, 0.14f, 0.30f);

            var urpCam = camObj.AddComponent<UniversalAdditionalCameraData>();
            urpCam.renderPostProcessing = true;
            urpCam.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            camObj.AddComponent<AudioListener>();

            var sideScrollCam = camObj.AddComponent<SideScroll2DCamera>();
            sideScrollCam.UsePerspective = true;
            sideScrollCam.FieldOfView = 26.5f;
            sideScrollCam.FixedZ = -24.0f;

            // ==========================================
            // 2. LIGHTING (WARM SUNSET SUN & AMBIENT TWILIGHT)
            // ==========================================
            GameObject lightingGroup = CreateChild(root, "Lighting");

            // Directional Light: Low sun casting long soft shadows across the river
            GameObject dirLightObj = new GameObject("DirectionalLight");
            dirLightObj.transform.SetParent(lightingGroup.transform, false);
            dirLightObj.transform.position = new Vector3(-3.5f, 12f, 25f);
            dirLightObj.transform.rotation = Quaternion.Euler(11f, 172f, 0f);

            Light dirLight = dirLightObj.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.color = new Color(1.0f, 0.76f, 0.46f);
            dirLight.intensity = 1.6f;
            dirLight.shadows = LightShadows.Soft;
            dirLight.shadowStrength = 0.70f;
            dirLight.shadowBias = 0.05f;

            // Ambient Trilight
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.30f, 0.22f, 0.42f);
            RenderSettings.ambientEquatorColor = new Color(0.66f, 0.38f, 0.30f);
            RenderSettings.ambientGroundColor = new Color(0.10f, 0.09f, 0.12f);
            RenderSettings.fog = false;

            // Reflection Probe
            GameObject probeObj = new GameObject("ReflectionProbe");
            probeObj.transform.SetParent(lightingGroup.transform, false);
            probeObj.transform.position = new Vector3(0f, 1.0f, 5.0f);
            var probe = probeObj.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Baked;
            probe.size = new Vector3(100f, 30f, 75f);

            // Post-Processing Volume
            GameObject volumeObj = new GameObject("SunsetPostProcessVolume");
            volumeObj.transform.SetParent(lightingGroup.transform, false);
            var volume = volumeObj.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;
            VolumeProfile sunsetProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>($"{LightingPath}/VolumeProfile_Sunset.asset");
            volume.profile = sunsetProfile;

            // ==========================================
            // 3. SKY, SUN, GOD RAYS, HORIZON
            // ==========================================
            GameObject bgSkyGroup = new GameObject("Sky_And_Atmosphere");
            bgSkyGroup.transform.SetParent(root.transform, false);

            // Sunset Sky Backdrop Quad (Z = 72f, resting directly above river horizon Y = -1.25)
            GameObject skyObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            skyObj.name = "Background_SkyBackdrop";
            skyObj.transform.SetParent(bgSkyGroup.transform, false);
            skyObj.transform.position = new Vector3(0f, 15.0f, 72f);
            skyObj.transform.localScale = new Vector3(190f, 34f, 1f);
            skyObj.GetComponent<Renderer>().sharedMaterial = skyMat;
            Object.DestroyImmediate(skyObj.GetComponent<Collider>());

            // Glowing Sunset Sun Disc (Z = 65f, centered in river opening at X = -3.5)
            GameObject sunObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            sunObj.name = "Background_SunsetSun";
            sunObj.transform.SetParent(bgSkyGroup.transform, false);
            sunObj.transform.position = new Vector3(-3.5f, 1.15f, 65f);
            sunObj.transform.localScale = new Vector3(9.5f, 9.5f, 1f);
            Material sunMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/M_StylizedSun_Sunset.mat");
            if (sunMat != null) sunObj.GetComponent<Renderer>().sharedMaterial = sunMat;
            Object.DestroyImmediate(sunObj.GetComponent<Collider>());
            var sunCtrl = sunObj.AddComponent<SunController>();

            // Sunset Volumetric Procedural God Rays (Soft fanned shimmering sunbeams)
            if (godRaysMat != null)
            {
                godRaysMat.SetColor("_RayColor", new Color(1.0f, 0.88f, 0.55f, 0.22f));
                godRaysMat.SetFloat("_RayIntensity", 0.85f);
                godRaysMat.SetFloat("_BeamCount", 6.0f);
                EditorUtility.SetDirty(godRaysMat);

                GameObject godRaysQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                godRaysQuad.name = "Sunbeams_GodRays";
                godRaysQuad.transform.SetParent(bgSkyGroup.transform, false);
                godRaysQuad.transform.position = new Vector3(-3.5f, 4.0f, 48f);
                godRaysQuad.transform.rotation = Quaternion.Euler(0f, 0f, -10f);
                godRaysQuad.transform.localScale = new Vector3(26f, 16f, 1f);
                godRaysQuad.GetComponent<Renderer>().sharedMaterial = godRaysMat;
                Object.DestroyImmediate(godRaysQuad.GetComponent<Collider>());
            }

            // Distant Clouds
            Material cloudMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/M_DistantClouds.mat");
            CreateSunsetClouds(bgSkyGroup.transform, cloudMat, 56f);

            // Distant Mangrove Horizon Forest Ridge
            CreateMangroveHorizonRidge(bgSkyGroup.transform, mangroveLeafMat, 54f);

            // ==========================================
            // 4. WATER (HORIZONTAL CALM MEKONG DELTA RIVER)
            // ==========================================
            GameObject waterGroup = CreateChild(root, "Water");

            // River Water Plane (Horizontal at Y = -1.25, extending from Z = -30 to Z = 80)
            GameObject riverObj = CreateHorizontalWaterPlane("MekongDelta_RiverWater", 140f, 110f, -30f, -1.25f);
            riverObj.transform.SetParent(waterGroup.transform, false);
            riverObj.GetComponent<Renderer>().sharedMaterial = waterMat;
            var waterCtrl = riverObj.AddComponent<WaterController>();
            waterCtrl.SetSunTransform(sunObj.transform);
            waterCtrl.Configure(
                shallow: new Color(0.10f, 0.28f, 0.32f, 1.0f),
                deep: new Color(0.04f, 0.12f, 0.16f, 1.0f),
                horizon: new Color(0.80f, 0.48f, 0.28f, 1.0f),
                reflection: new Color(1.5f, 1.05f, 0.55f, 1.0f),
                inNearZ: -24.0f,
                inFarZ: 60.0f,
                inReflectionWidth: 6.0f,
                inReflectionIntensity: 2.2f,
                inFoamDepth: 0.0f,
                inFoamColor: new Color(1f, 1f, 1f, 0f)
            );

            // Deep Opaque Riverbed (At Y = -4.5, so looking down reveals deep dark water)
            CreateDeepRiverbed(waterGroup.transform, riverMudMat);

            // ==========================================
            // 5. GAMEPLAY PLATFORMS & COLLIDERS (Z = 0)
            // ==========================================
            GameObject gameplayGroup = CreateChild(root, "Gameplay");
            GameObject playerSpawnObj = new GameObject("PlayerSpawn");
            playerSpawnObj.transform.SetParent(gameplayGroup.transform, false);
            // Hero Spawn: on the left dock running toward the jump gap
            playerSpawnObj.transform.position = new Vector3(-7.5f, -0.48f, 0f);

            GameObject platformsGroup = CreateChild(gameplayGroup, "Platforms");
            GameObject jumpSectionsGroup = CreateChild(gameplayGroup, "JumpSections");

            // Section 1: Left Pier Deck (-11.5 to -4.5, Y = -0.58)
            CreatePlatformCollider(platformsGroup.transform, "Platform_Section_Left", new Vector3(-8.0f, -0.68f, 0f), new Vector3(7.0f, 0.40f, 2.8f));

            // JUMP GAP at X = -4.5 to -3.1 (1.4m Jump Gap over open water!)
            CreateJumpSection(jumpSectionsGroup.transform, "JumpGap_OverWater", new Vector3(-3.80f, -1.40f, 0f), new Vector3(1.4f, 1.6f, 3.5f));

            // Section 2: Central Raised Dock (-3.1 to +3.5, Y = -0.46)
            CreatePlatformCollider(platformsGroup.transform, "Platform_Section_Central", new Vector3(0.20f, -0.56f, 0f), new Vector3(6.6f, 0.40f, 2.8f));

            // Section 3: Village Ramp & Boardwalk (+3.5 to +11.0, Y = -0.38)
            CreatePlatformCollider(platformsGroup.transform, "Platform_Section_Village", new Vector3(7.25f, -0.48f, 0f), new Vector3(7.5f, 0.40f, 3.0f));

            // ==========================================
            // 6. DOCK GEOMETRY (MODULAR PLANKS, PILINGS, MOORINGS)
            // ==========================================
            GameObject dockGroup = CreateChild(root, "Dock");
            GameObject dockPlanks = CreateChild(dockGroup, "Dock_Planks");
            GameObject dockSupports = CreateChild(dockGroup, "Dock_Supports");
            GameObject dockDetails = CreateChild(dockGroup, "Dock_Details");

            // Main Longitudinal Timber Girders under planks
            CreateDockGirders(dockSupports.transform, timberDarkMat);

            // Modular Wooden Planks with Jitter & Broken Ends at Jump Gap
            BuildModularPlanksWithGap(dockPlanks.transform, timberDarkMat, timberLightMat);

            // Heavy Wooden Pilings driven deep into the river
            BuildStiltPilings(dockSupports.transform, pilingMat);

            // Rustic Mooring Bollards, Coiled Ropes, and Lantern Posts
            BuildMooringPostsAndLanterns(dockDetails.transform, timberDarkMat, ropeMat, lanternGlowMat);

            // ==========================================
            // 7. PROPS (BARRELS, CRATES, FISHING NETS, BASKETS)
            // ==========================================
            GameObject propsGroup = CreateChild(root, "Props");
            GameObject barrelsGroup = CreateChild(propsGroup, "Barrels");
            GameObject cratesGroup = CreateChild(propsGroup, "Crates");
            GameObject netsGroup = CreateChild(propsGroup, "FishingNets");
            GameObject basketsGroup = CreateChild(propsGroup, "Baskets");

            PopulateHistorical1954Props(barrelsGroup.transform, cratesGroup.transform, netsGroup.transform, basketsGroup.transform, timberDarkMat, bambooMat, ropeMat);

            // ==========================================
            // 8. BOATS (TRADITIONAL VIETNAMESE WATERCRAFT)
            // ==========================================
            GameObject boatsGroup = CreateChild(root, "Boats");
            GameObject heroBoatGroup = CreateChild(boatsGroup, "HeroBoats");
            GameObject midgroundBoats = CreateChild(boatsGroup, "MidgroundBoats");
            GameObject distantBoats = CreateChild(boatsGroup, "DistantBoats");
            GameObject sampansGroup = CreateChild(boatsGroup, "Sampans");

            // HERO VIETNAMESE JUNK (Ghe Bầu Nam Bộ với buồm cánh dơi màu nâu)
            // Moored on the left river behind the dock (X = -6.5, Y = -1.25, Z = 5.5)
            CreateVietnameseJunkBoat(heroBoatGroup.transform, "Hero_Junk_GheBau", new Vector3(-6.5f, -1.25f, 5.5f), new Vector3(0f, 22f, 0f), 1.05f, true, boatHullMat, boatSailMat, bambooMat, lanternGlowMat);

            // SECOND SAILING JUNK (Mid-distance, catching golden sunset)
            CreateVietnameseJunkBoat(midgroundBoats.transform, "Mid_Junk_Sailing", new Vector3(-12.5f, -1.25f, 13.0f), new Vector3(0f, -18f, 0f), 0.90f, true, boatHullMat, boatSailMat, bambooMat, lanternGlowMat);

            // DISTANT SAILING JUNKS (Drifting along sunset horizon)
            CreateVietnameseJunkBoat(distantBoats.transform, "Distant_Junk_1", new Vector3(-1.0f, -1.25f, 26.0f), new Vector3(0f, 15f, 0f), 0.60f, true, boatHullMat, boatSailMat, bambooMat, null);
            CreateVietnameseJunkBoat(distantBoats.transform, "Distant_Junk_2", new Vector3(-17.0f, -1.25f, 36.0f), new Vector3(0f, -32f, 0f), 0.42f, true, boatHullMat, boatSailMat, bambooMat, null);

            // ROWING SAMPANS (Xuồng Ba Lá) tied to dock pilings
            CreateSampanXuongBaLa(sampansGroup.transform, "Sampan_Tied_Dock_1", new Vector3(-2.2f, -1.25f, 1.8f), new Vector3(0f, 12f, 0f), 0.95f, boatHullMat, bambooMat);
            CreateSampanXuongBaLa(sampansGroup.transform, "Sampan_Tied_Dock_2", new Vector3(4.8f, -1.25f, 2.2f), new Vector3(0f, -8f, 0f), 0.90f, boatHullMat, bambooMat);

            // ==========================================
            // 9. VILLAGE ARCHITECTURE (AUTHENTIC CÀ MAU 1954 RIVERSIDE PORT)
            // ==========================================
            GameObject villageGroup = CreateChild(root, "Village");
            GameObject housesGroup = CreateChild(villageGroup, "Houses");
            GameObject shopsGroup = CreateChild(villageGroup, "Shops");
            GameObject colonialGroup = CreateChild(villageGroup, "ColonialBuilding");
            GameObject walkwaysGroup = CreateChild(villageGroup, "Walkways");

            // 1. Fisherman's Stilt Dwelling with veranda and hanging lantern (X = 4.2, Z = 4.2)
            CreateStiltHouse(housesGroup.transform, "House_Fisherman_Dwelling", new Vector3(4.2f, 0.02f, 4.2f), new Vector3(3.6f, 2.1f, 3.0f), timberDarkMat, thatchRoofMat, lanternGlowMat);

            // 2. Riverside Trading Post / Grocery with aged tile roof (X = 7.0, Z = 5.6)
            CreateStiltHouse(shopsGroup.transform, "Shop_Riverside_Store", new Vector3(7.0f, 0.22f, 5.6f), new Vector3(4.0f, 2.3f, 3.4f), timberDarkMat, tileRoofMat, lanternGlowMat);

            // 3. FRENCH COLONIAL 2-STORY BUILDING (Nhà Phố Cổ Thời Pháp 1954) (X = 9.8, Z = 7.8)
            CreateColonialTwoStoryBuilding(colonialGroup.transform, "French_Colonial_Building_1954", new Vector3(9.8f, 0.38f, 7.8f), colonialWallMat, colonialShutterMat, tileRoofMat, lanternGlowMat);

            // Connecting boardwalk ramps from dock to village
            CreateWalkwayRamp(walkwaysGroup.transform, "Walkway_To_House_1", new Vector3(4.0f, -0.40f, 2.4f), new Vector3(2.0f, 0.16f, 2.4f), timberDarkMat);
            CreateWalkwayRamp(walkwaysGroup.transform, "Walkway_To_Shop_2", new Vector3(6.8f, -0.30f, 3.0f), new Vector3(2.4f, 0.16f, 3.0f), timberDarkMat);

            // ==========================================
            // 10. VEGETATION (MANGROVES, COCONUT PALMS, 3D HERO TREES)
            // ==========================================
            GameObject vegGroup = CreateChild(root, "Vegetation");
            GameObject mgMangroves = CreateChild(vegGroup, "MidgroundMangroves");
            GameObject villagePalms = CreateChild(vegGroup, "VillageCoconutPalms");
            GameObject heroTrees = CreateChild(vegGroup, "HeroTrees");

            // Midground Mangrove Island (Rừng đước giữa sông, rễ cọc chằng chịt)
            CreateMidgroundMangroveCluster(mgMangroves.transform, new Vector3(-0.8f, -1.25f, 8.8f), mangroveBarkMat, mangroveLeafMat);

            // Artistic Riverside Trees behind village
            GameObject treeArtPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Tree_GocCay2_Artistic.prefab");
            if (treeArtPrefab != null)
            {
                var t1 = (GameObject)PrefabUtility.InstantiatePrefab(treeArtPrefab, villagePalms.transform);
                t1.name = "Artistic_Tree_Village_1";
                t1.transform.position = new Vector3(5.6f, 0.20f, 6.5f);
                t1.transform.localScale = Vector3.one * 0.85f;

                var t2 = (GameObject)PrefabUtility.InstantiatePrefab(treeArtPrefab, villagePalms.transform);
                t2.name = "Artistic_Tree_Village_2";
                t2.transform.position = new Vector3(12.5f, 0.40f, 8.8f);
                t2.transform.localScale = Vector3.one * 1.05f;
            }

            // 3D HERO TREE (Lana_Artistic_Hero_Tree) on the Left Pier Entrance
            GameObject heroTreePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Lana_Artistic_Hero_Tree.prefab");
            if (heroTreePrefab != null)
            {
                var ht = (GameObject)PrefabUtility.InstantiatePrefab(heroTreePrefab, heroTrees.transform);
                ht.name = "Hero_Framing_Tree_Left";
                ht.transform.position = new Vector3(-11.5f, -0.40f, 1.5f);
                ht.transform.rotation = Quaternion.Euler(0f, 25f, 4f);
                ht.transform.localScale = Vector3.one * 1.05f;
            }

            // Natural Muddy Shoreline Embankment on Left Shore
            GameObject mudBank = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mudBank.name = "Shore_Mud_Bank";
            mudBank.transform.SetParent(heroTrees.transform, false);
            mudBank.transform.position = new Vector3(-12.5f, -0.78f, 1.2f);
            mudBank.transform.localScale = new Vector3(4.5f, 0.45f, 3.5f);
            mudBank.GetComponent<Renderer>().sharedMaterial = riverMudMat;
            Object.DestroyImmediate(mudBank.GetComponent<Collider>());

            // Subtle textured shore boulder
            GameObject boulderPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Meshy_AI_Lone_Boulder_0928161951_texture.prefab");
            if (boulderPrefab != null)
            {
                var b1 = (GameObject)PrefabUtility.InstantiatePrefab(boulderPrefab, heroTrees.transform);
                b1.name = "Shore_Boulder_1";
                b1.transform.position = new Vector3(-11.6f, -0.68f, 1.5f);
                b1.transform.rotation = Quaternion.Euler(5f, 35f, 10f);
                b1.transform.localScale = Vector3.one * 0.32f;
            }

            // 3D Grass Tufts along the dock edge and shoreline
            GameObject grassPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/PF_StylizedGrassTuft_3D.prefab");
            if (grassPrefab != null)
            {
                float[] gx = { -11.0f, -10.2f, 4.2f, 6.5f, 9.2f };
                for (int i = 0; i < gx.Length; i++)
                {
                    var g = (GameObject)PrefabUtility.InstantiatePrefab(grassPrefab, heroTrees.transform);
                    g.name = $"Shore_Grass_{i}";
                    g.transform.position = new Vector3(gx[i], -0.65f, 1.1f);
                    g.transform.rotation = Quaternion.Euler(0f, (i * 55f) % 360f, 0f);
                    g.transform.localScale = Vector3.one * 0.9f;
                }
            }

            // ==========================================
            // 11. VFX (GOLDEN DUST MOTES, RIVER SURFACE MIST)
            // ==========================================
            GameObject vfxGroup = CreateChild(root, "VFX");
            BuildAtmosphericParticles(vfxGroup.transform, motesMat);

            // ==========================================
            // 12. PROTAGONIST HERO CHARACTER
            // ==========================================
            GameObject hero = SpawnHeroCharacter(gameplayGroup.transform, playerSpawnObj.transform.position);
            if (hero != null)
            {
                sideScrollCam.Target = hero.transform;
            }

            // Save Scene
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("<color=#10b981>[Ca Mau 1954]</color> Full 3D Cinematic Level successfully generated in: " + ScenePath);
        }

        private static GameObject CreateChild(GameObject parent, string name)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent.transform, false);
            return obj;
        }

        // =========================================================================
        // SKY & WATER MATERIAL CONFIGURATIONS
        // =========================================================================
        private static void ConfigureSunsetSkyMaterial(Material mat)
        {
            if (mat == null) return;
            mat.SetColor("_HorizonColor", new Color(1.0f, 0.78f, 0.46f, 1.0f));
            mat.SetColor("_MidSkyColor", new Color(0.72f, 0.28f, 0.30f, 1.0f));
            mat.SetColor("_ZenithColor", new Color(0.16f, 0.14f, 0.30f, 1.0f));
            mat.SetColor("_HorizonBandColor", new Color(1.0f, 0.90f, 0.60f, 1.0f));
            mat.SetFloat("_HorizonY", -1.25f);
            mat.SetFloat("_ZenithY", 22.0f);
            mat.SetFloat("_MidPosition", 0.38f);
            mat.SetFloat("_HorizonBandHeight", -0.2f);
            mat.SetFloat("_HorizonBandWidth", 1.2f);
            mat.SetFloat("_HorizonBandIntensity", 0.65f);
            mat.SetFloat("_HorizonPower", 1.2f);
            mat.SetFloat("_ZenithPower", 1.2f);
            EditorUtility.SetDirty(mat);
        }

        private static void ConfigureSunsetWaterMaterial(Material mat, float sunX)
        {
            if (mat == null) return;
            mat.SetColor("_ShallowColor", new Color(0.10f, 0.28f, 0.32f, 1.0f));
            mat.SetColor("_DeepColor", new Color(0.04f, 0.12f, 0.16f, 1.0f));
            mat.SetColor("_HorizonWaterColor", new Color(0.80f, 0.48f, 0.28f, 1.0f));
            mat.SetColor("_SunReflectionColor", new Color(1.5f, 1.05f, 0.55f, 1.0f));
            mat.SetColor("_FoamColor", new Color(1f, 1f, 1f, 0f)); // 0 alpha eliminates edge line
            mat.SetFloat("_NearZ", -24.0f);
            mat.SetFloat("_FarZ", 60.0f);
            mat.SetFloat("_SunWorldX", sunX);
            mat.SetFloat("_ReflectionWidth", 6.0f);
            mat.SetFloat("_ReflectionIntensity", 2.2f);
            mat.SetFloat("_ReflectionFalloff", 2.8f);
            mat.SetFloat("_WaveSpeed", 1.1f);
            mat.SetFloat("_WaveFrequency", 1.4f);
            mat.SetFloat("_WaveHeight", 0.016f);
            mat.SetFloat("_RippleScale", 5.5f);
            mat.SetFloat("_RippleSpeed", 0.9f);
            mat.SetFloat("_RippleStrength", 0.40f);
            mat.SetFloat("_FoamDepth", 0.0f);
            EditorUtility.SetDirty(mat);
        }

        // =========================================================================
        // WATER & RIVERBED GEOMETRY
        // =========================================================================
        private static GameObject CreateHorizontalWaterPlane(string name, float width, float length, float startZ, float yLevel)
        {
            GameObject planeObj = new GameObject(name);
            planeObj.transform.position = Vector3.zero;

            MeshFilter mf = planeObj.AddComponent<MeshFilter>();
            MeshRenderer mr = planeObj.AddComponent<MeshRenderer>();

            int segX = 90;
            int segZ = 65;
            Mesh mesh = new Mesh { name = "HorizontalRiverMesh" };

            Vector3[] verts = new Vector3[(segX + 1) * (segZ + 1)];
            Vector2[] uvs = new Vector2[verts.Length];
            int[] tris = new int[segX * segZ * 6];

            float dx = width / segX;
            float dz = length / segZ;
            float halfW = width * 0.5f;

            for (int z = 0, i = 0; z <= segZ; z++)
            {
                float vz = startZ + z * dz;
                float v = (float)z / segZ;
                for (int x = 0; x <= segX; x++, i++)
                {
                    float vx = x * dx - halfW;
                    float u = (float)x / segX;
                    verts[i] = new Vector3(vx, yLevel, vz);
                    uvs[i] = new Vector2(u, v);
                }
            }

            for (int z = 0, ti = 0, vi = 0; z < segZ; z++, vi++)
            {
                for (int x = 0; x < segX; x++, ti += 6, vi++)
                {
                    tris[ti] = vi;
                    tris[ti + 1] = vi + segX + 1;
                    tris[ti + 2] = vi + 1;
                    tris[ti + 3] = vi + 1;
                    tris[ti + 4] = vi + segX + 1;
                    tris[ti + 5] = vi + segX + 2;
                }
            }

            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            mf.sharedMesh = mesh;
            return planeObj;
        }

        private static void CreateDeepRiverbed(Transform parent, Material mudMat)
        {
            // Opaque Riverbed plane deep under water (at Y = -4.5)
            GameObject bed = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bed.name = "Riverbed_DarkMud";
            bed.transform.SetParent(parent, false);
            bed.transform.position = new Vector3(0f, -4.5f, 25f);
            bed.transform.localScale = new Vector3(140f, 1.5f, 115f);
            bed.GetComponent<Renderer>().sharedMaterial = mudMat;
            Object.DestroyImmediate(bed.GetComponent<Collider>());
        }

        // =========================================================================
        // SKY DETAILS & ATMOSPHERE
        // =========================================================================
        private static void CreateSunsetClouds(Transform parent, Material cloudMat, float depthZ)
        {
            if (cloudMat == null) return;
            Vector3[] positions = {
                new Vector3(-10f, 5.8f, depthZ),
                new Vector3(1f, 6.8f, depthZ - 2f),
                new Vector3(12f, 5.2f, depthZ + 2f),
                new Vector3(-3f, 3.8f, depthZ - 4f)
            };
            Vector3[] scales = {
                new Vector3(15f, 3.6f, 1f),
                new Vector3(18f, 4.0f, 1f),
                new Vector3(13f, 3.2f, 1f),
                new Vector3(11f, 2.6f, 1f)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                GameObject cloud = GameObject.CreatePrimitive(PrimitiveType.Quad);
                cloud.name = $"Sunset_Cloud_{i}";
                cloud.transform.SetParent(parent, false);
                cloud.transform.position = positions[i];
                cloud.transform.localScale = scales[i];
                cloud.GetComponent<Renderer>().sharedMaterial = cloudMat;
                Object.DestroyImmediate(cloud.GetComponent<Collider>());
            }
        }

        private static void CreateMangroveHorizonRidge(Transform parent, Material leafMat, float depthZ)
        {
            GameObject ridgeRoot = new GameObject("Mangrove_Horizon_Ridge");
            ridgeRoot.transform.SetParent(parent, false);
            ridgeRoot.transform.position = new Vector3(0f, -0.7f, depthZ);

            for (float x = -45f; x <= 45f; x += 3.5f)
            {
                float h = Random.Range(1.1f, 1.9f);
                float w = Random.Range(4.0f, 6.0f);
                float yOff = Random.Range(-0.1f, 0.2f);

                GameObject clump = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                clump.name = "Horizon_Mangrove_Clump";
                clump.transform.SetParent(ridgeRoot.transform, false);
                clump.transform.localPosition = new Vector3(x, yOff, 0f);
                clump.transform.localScale = new Vector3(w, h, 2.0f);
                clump.GetComponent<Renderer>().sharedMaterial = leafMat;
                Object.DestroyImmediate(clump.GetComponent<Collider>());
            }
        }

        // =========================================================================
        // DOCK GEOMETRY & WALKWAY PLANKS
        // =========================================================================
        private static void CreateDockGirders(Transform parent, Material mat)
        {
            // Section 1: Left Pier (-11.5 to -4.5)
            CreateBeam(parent, "Girder_Front_1", new Vector3(-8.0f, -0.80f, -0.85f), new Vector3(7.0f, 0.22f, 0.24f), mat);
            CreateBeam(parent, "Girder_Back_1", new Vector3(-8.0f, -0.80f, 0.85f), new Vector3(7.0f, 0.22f, 0.24f), mat);

            // Section 2: Central Dock (-3.1 to +3.5)
            CreateBeam(parent, "Girder_Front_2", new Vector3(0.20f, -0.68f, -0.85f), new Vector3(6.6f, 0.22f, 0.24f), mat);
            CreateBeam(parent, "Girder_Back_2", new Vector3(0.20f, -0.68f, 0.85f), new Vector3(6.6f, 0.22f, 0.24f), mat);

            // Section 3: Village Pier (+3.5 to +11.0)
            CreateBeam(parent, "Girder_Front_3", new Vector3(7.25f, -0.60f, -0.90f), new Vector3(7.5f, 0.22f, 0.24f), mat);
            CreateBeam(parent, "Girder_Back_3", new Vector3(7.25f, -0.60f, 0.90f), new Vector3(7.5f, 0.22f, 0.24f), mat);
        }

        private static void BuildModularPlanksWithGap(Transform parent, Material darkMat, Material lightMat)
        {
            List<(float start, float end, float yBase)> sections = new List<(float, float, float)>
            {
                (-11.5f, -4.5f, -0.58f), // Section 1: Left Walkway Deck (Leading to Jump Gap)
                (-3.1f, 3.5f, -0.46f),   // Section 2: Central Raised Dock (after Jump Gap)
                (3.5f, 11.0f, -0.38f)    // Section 3: Village-side Pier
            };

            int counter = 0;
            float step = 0.38f;

            foreach (var sec in sections)
            {
                for (float x = sec.start; x <= sec.end; x += step)
                {
                    GameObject plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    plank.name = $"Dock_Plank_{counter:D3}";
                    plank.transform.SetParent(parent, false);

                    float yJitter = Random.Range(-0.012f, 0.012f);
                    float rotY = Random.Range(-1.5f, 1.5f);
                    float rotZ = Random.Range(-0.3f, 0.3f);
                    float lengthZ = Random.Range(2.50f, 2.70f);
                    float widthX = Random.Range(0.32f, 0.36f);

                    plank.transform.position = new Vector3(x, sec.yBase + yJitter, 0f);
                    plank.transform.rotation = Quaternion.Euler(0f, rotY, rotZ);
                    plank.transform.localScale = new Vector3(widthX, 0.10f, lengthZ);

                    plank.GetComponent<Renderer>().sharedMaterial = (counter % 3 == 0) ? lightMat : darkMat;
                    Object.DestroyImmediate(plank.GetComponent<Collider>());
                    counter++;
                }
            }

            // Broken plank splinters at Jump Gap (-4.5f and -3.1f)
            CreateBrokenPlankSpike(parent, new Vector3(-4.45f, -0.58f, -0.5f), new Vector3(0.5f, 0.09f, 0.30f), darkMat);
            CreateBrokenPlankSpike(parent, new Vector3(-4.40f, -0.57f, 0.3f), new Vector3(0.6f, 0.09f, 0.32f), darkMat);
            CreateBrokenPlankSpike(parent, new Vector3(-3.15f, -0.46f, -0.3f), new Vector3(0.55f, 0.09f, 0.28f), darkMat);
            CreateBrokenPlankSpike(parent, new Vector3(-3.20f, -0.47f, 0.4f), new Vector3(0.48f, 0.09f, 0.31f), darkMat);
        }

        private static void CreateBrokenPlankSpike(Transform parent, Vector3 pos, Vector3 size, Material mat)
        {
            GameObject sp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sp.name = "Broken_Plank_Splinter";
            sp.transform.SetParent(parent, false);
            sp.transform.position = pos;
            sp.transform.rotation = Quaternion.Euler(0f, Random.Range(-5f, 5f), Random.Range(-4f, 4f));
            sp.transform.localScale = size;
            sp.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(sp.GetComponent<Collider>());
        }

        private static void BuildStiltPilings(Transform parent, Material mat)
        {
            float[] stiltX = {
                -11.0f, -9.0f, -7.0f, -5.2f,
                // Jump Gap at -4.5 to -3.1
                -2.8f, -0.8f, 1.2f, 3.2f,
                5.5f, 7.8f, 10.0f
            };

            for (int i = 0; i < stiltX.Length; i++)
            {
                float x = stiltX[i];

                // Front Stilt driving down to riverbed (-2.8f)
                GameObject sf = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                sf.name = $"Stilt_Front_{i:D2}";
                sf.transform.SetParent(parent, false);
                sf.transform.position = new Vector3(x, -1.9f, -1.05f);
                sf.transform.localScale = new Vector3(0.24f, 1.7f, 0.24f);
                sf.GetComponent<Renderer>().sharedMaterial = mat;
                Object.DestroyImmediate(sf.GetComponent<Collider>());

                // Back Stilt
                GameObject sb = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                sb.name = $"Stilt_Back_{i:D2}";
                sb.transform.SetParent(parent, false);
                sb.transform.position = new Vector3(x, -1.9f, 1.05f);
                sb.transform.localScale = new Vector3(0.24f, 1.7f, 0.24f);
                sb.GetComponent<Renderer>().sharedMaterial = mat;
                Object.DestroyImmediate(sb.GetComponent<Collider>());
            }
        }

        private static void BuildMooringPostsAndLanterns(Transform details, Material timberMat, Material ropeMat, Material glowMat)
        {
            float[] postX = { -10.0f, -6.5f, -2.5f, 0.8f, 3.8f, 6.8f, 9.8f };

            for (int i = 0; i < postX.Length; i++)
            {
                float x = postX[i];

                // Front Mooring Post
                GameObject pf = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pf.name = $"Mooring_Post_Front_{i}";
                pf.transform.SetParent(details, false);
                pf.transform.position = new Vector3(x, -0.20f, -1.18f);
                pf.transform.localScale = new Vector3(0.20f, 0.45f, 0.20f);
                pf.GetComponent<Renderer>().sharedMaterial = timberMat;
                Object.DestroyImmediate(pf.GetComponent<Collider>());

                // Rope coil on post
                GameObject rf = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                rf.name = "Rope_Coil";
                rf.transform.SetParent(pf.transform, false);
                rf.transform.localPosition = new Vector3(0f, 0.12f, 0f);
                rf.transform.localScale = new Vector3(1.35f, 0.35f, 1.35f);
                rf.GetComponent<Renderer>().sharedMaterial = ropeMat;
                Object.DestroyImmediate(rf.GetComponent<Collider>());

                // Back Mooring Post
                GameObject pb = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pb.name = $"Mooring_Post_Back_{i}";
                pb.transform.SetParent(details, false);
                pb.transform.position = new Vector3(x + 0.35f, -0.15f, 1.18f);
                pb.transform.localScale = new Vector3(0.20f, 0.50f, 0.20f);
                pb.GetComponent<Renderer>().sharedMaterial = timberMat;
                Object.DestroyImmediate(pb.GetComponent<Collider>());
            }

            // Warm Oil Lantern Posts (Đèn Bão Gỗ Bến Cà Mau 1954)
            CreateLanternPost(details, new Vector3(-9.5f, -0.48f, 1.25f), timberMat, glowMat);
            CreateLanternPost(details, new Vector3(-1.8f, -0.44f, 1.25f), timberMat, glowMat);
            CreateLanternPost(details, new Vector3(5.5f, -0.34f, 1.25f), timberMat, glowMat);
        }

        private static void CreateLanternPost(Transform parent, Vector3 basePos, Material timberMat, Material glowMat)
        {
            GameObject postRoot = new GameObject("Dock_Lantern_Post");
            postRoot.transform.SetParent(parent, false);
            postRoot.transform.position = basePos;

            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Pole";
            pole.transform.SetParent(postRoot.transform, false);
            pole.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            pole.transform.localScale = new Vector3(0.08f, 1.15f, 0.08f);
            pole.GetComponent<Renderer>().sharedMaterial = timberMat;
            Object.DestroyImmediate(pole.GetComponent<Collider>());

            GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arm.name = "Arm";
            arm.transform.SetParent(postRoot.transform, false);
            arm.transform.localPosition = new Vector3(-0.25f, 2.22f, 0f);
            arm.transform.localScale = new Vector3(0.55f, 0.06f, 0.06f);
            arm.GetComponent<Renderer>().sharedMaterial = timberMat;
            Object.DestroyImmediate(arm.GetComponent<Collider>());

            GameObject lantern = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lantern.name = "Lantern_Glass";
            lantern.transform.SetParent(postRoot.transform, false);
            lantern.transform.localPosition = new Vector3(-0.48f, 2.02f, 0f);
            lantern.transform.localScale = Vector3.one * 0.20f;
            lantern.GetComponent<Renderer>().sharedMaterial = glowMat;
            Object.DestroyImmediate(lantern.GetComponent<Collider>());

            Light l = lantern.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1.0f, 0.80f, 0.44f);
            l.intensity = 2.2f;
            l.range = 5.5f;
            l.shadows = LightShadows.Soft;
        }

        // =========================================================================
        // PROPS (1954 DOCK MERCHANDISE)
        // =========================================================================
        private static void PopulateHistorical1954Props(Transform barrels, Transform crates, Transform nets, Transform baskets, Material timberMat, Material bambooMat, Material ropeMat)
        {
            GameObject cratePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Thùng gỗ.prefab");
            GameObject slatCubePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Meshy_AI_Wooden_Slat_Cube_0927180014_texture.prefab");

            // Crates Stack at Left Pier Entrance (-10.2f)
            if (cratePrefab != null)
            {
                var c1 = Object.Instantiate(cratePrefab, crates);
                c1.name = "Crate_Stack_L1";
                c1.transform.position = new Vector3(-10.2f, -0.42f, 0.55f);
                c1.transform.rotation = Quaternion.Euler(0f, 15f, 0f);
                c1.transform.localScale = Vector3.one * 0.65f;

                var c2 = Object.Instantiate(cratePrefab, crates);
                c2.name = "Crate_Stack_L2";
                c2.transform.position = new Vector3(-9.8f, -0.42f, 0.20f);
                c2.transform.rotation = Quaternion.Euler(0f, -12f, 0f);
                c2.transform.localScale = Vector3.one * 0.55f;
            }

            // Crates near Central Dock (+2.8f)
            if (slatCubePrefab != null)
            {
                var c3 = (GameObject)PrefabUtility.InstantiatePrefab(slatCubePrefab, crates);
                c3.name = "Crate_Mid_1";
                c3.transform.position = new Vector3(2.8f, -0.38f, 0.65f);
                c3.transform.rotation = Quaternion.Euler(0f, 22f, 0f);
                c3.transform.localScale = Vector3.one * 0.65f;
            }

            // Salt Fish Barrels
            Vector3[] barrelPos = {
                new Vector3(-6.2f, -0.50f, 0.65f),
                new Vector3(-5.6f, -0.50f, 0.60f),
                new Vector3(1.8f, -0.38f, 0.68f),
                new Vector3(8.2f, -0.28f, 0.75f)
            };

            for (int i = 0; i < barrelPos.Length; i++)
            {
                GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                barrel.name = $"Salt_Fish_Barrel_{i}";
                barrel.transform.SetParent(barrels, false);
                barrel.transform.position = barrelPos[i];
                barrel.transform.localScale = new Vector3(0.44f, 0.38f, 0.44f);
                barrel.GetComponent<Renderer>().sharedMaterial = timberMat;
                Object.DestroyImmediate(barrel.GetComponent<Collider>());
            }

            // Bamboo Round Baskets (Thúng Tre)
            Vector3[] basketPos = {
                new Vector3(-8.5f, -0.52f, -0.55f),
                new Vector3(-5.0f, -0.52f, -0.50f),
                new Vector3(-0.5f, -0.40f, -0.58f),
                new Vector3(5.8f, -0.30f, -0.58f)
            };

            for (int i = 0; i < basketPos.Length; i++)
            {
                GameObject basket = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                basket.name = $"Bamboo_Basket_{i}";
                basket.transform.SetParent(baskets, false);
                basket.transform.position = basketPos[i];
                basket.transform.localScale = new Vector3(0.42f, 0.18f, 0.42f);
                basket.GetComponent<Renderer>().sharedMaterial = bambooMat;
                Object.DestroyImmediate(basket.GetComponent<Collider>());
            }

            // Draped Fishing Nets drying on the pier edge
            Vector3[] netPos = {
                new Vector3(-7.0f, -0.20f, 1.10f),
                new Vector3(1.5f, -0.15f, 1.10f),
                new Vector3(7.5f, -0.05f, 1.10f)
            };

            for (int i = 0; i < netPos.Length; i++)
            {
                GameObject net = GameObject.CreatePrimitive(PrimitiveType.Cube);
                net.name = $"Draped_Fish_Net_{i}";
                net.transform.SetParent(nets, false);
                net.transform.position = netPos[i];
                net.transform.localScale = new Vector3(1.2f, 0.45f, 0.20f);
                net.GetComponent<Renderer>().sharedMaterial = ropeMat;
                Object.DestroyImmediate(net.GetComponent<Collider>());
            }
        }

        // =========================================================================
        // 3D VIETNAMESE WATERCRAFT WITH BILLOWING BATTENED SAILS
        // =========================================================================
        private static void CreateVietnameseJunkBoat(Transform parent, string name, Vector3 pos, Vector3 rot, float scale, bool hasSails, Material hullMat, Material sailMat, Material bambooMat, Material glowMat)
        {
            GameObject boatRoot = new GameObject(name);
            boatRoot.transform.SetParent(parent, false);
            boatRoot.transform.position = pos;
            boatRoot.transform.rotation = Quaternion.Euler(rot);
            boatRoot.transform.localScale = Vector3.one * scale;

            // 1. Boat Hull (Thân Thuyền Gỗ Uốn Cong)
            GameObject hull = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hull.name = "Hull_Main";
            hull.transform.SetParent(boatRoot.transform, false);
            hull.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            hull.transform.localScale = new Vector3(5.0f, 0.70f, 1.6f);
            hull.GetComponent<Renderer>().sharedMaterial = hullMat;
            Object.DestroyImmediate(hull.GetComponent<Collider>());

            // Raised Curved Prow (Mũi Thuyền Vểnh Cao)
            GameObject bow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bow.name = "Hull_Bow";
            bow.transform.SetParent(boatRoot.transform, false);
            bow.transform.localPosition = new Vector3(2.6f, 0.60f, 0f);
            bow.transform.localRotation = Quaternion.Euler(0f, 0f, -22f);
            bow.transform.localScale = new Vector3(1.2f, 0.65f, 1.35f);
            bow.GetComponent<Renderer>().sharedMaterial = hullMat;
            Object.DestroyImmediate(bow.GetComponent<Collider>());

            // Raised Stern (Đuôi Thuyền)
            GameObject stern = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stern.name = "Hull_Stern";
            stern.transform.SetParent(boatRoot.transform, false);
            stern.transform.localPosition = new Vector3(-2.6f, 0.55f, 0f);
            stern.transform.localRotation = Quaternion.Euler(0f, 0f, 18f);
            stern.transform.localScale = new Vector3(1.1f, 0.65f, 1.45f);
            stern.GetComponent<Renderer>().sharedMaterial = hullMat;
            Object.DestroyImmediate(stern.GetComponent<Collider>());

            // 2. Arched Bamboo Canopy Cabin (Mui Ghe Vòm Tre Lá)
            GameObject cabin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cabin.name = "Bamboo_Cabin_Canopy";
            cabin.transform.SetParent(boatRoot.transform, false);
            cabin.transform.localPosition = new Vector3(-0.6f, 0.95f, 0f);
            cabin.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            cabin.transform.localScale = new Vector3(1.8f, 1.05f, 1.25f);
            cabin.GetComponent<Renderer>().sharedMaterial = bambooMat;
            Object.DestroyImmediate(cabin.GetComponent<Collider>());

            // 3. Masts and Aerodynamic Billowing Battened Sails
            if (hasSails && sailMat != null)
            {
                // Main Mast (Cột Buồm Chính)
                GameObject mainMast = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                mainMast.name = "Main_Mast";
                mainMast.transform.SetParent(boatRoot.transform, false);
                mainMast.transform.localPosition = new Vector3(0.5f, 2.7f, 0f);
                mainMast.transform.localScale = new Vector3(0.12f, 2.4f, 0.12f);
                mainMast.GetComponent<Renderer>().sharedMaterial = hullMat;
                Object.DestroyImmediate(mainMast.GetComponent<Collider>());

                // 3D Billowing Battened Sail (Cánh Buồm Cánh Dơi Uốn Cong Đón Gió)
                CreateCurvedBattenedSail(boatRoot.transform, "Main_Curved_Sail", new Vector3(0.5f, 1.0f, 0f), new Vector3(0f, 12f, -3f), 3.2f, 3.6f, sailMat, bambooMat);

                // Fore Mast (Cột Buồm Phụ)
                GameObject foreMast = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                foreMast.name = "Fore_Mast";
                foreMast.transform.SetParent(boatRoot.transform, false);
                foreMast.transform.localPosition = new Vector3(2.2f, 1.8f, 0f);
                foreMast.transform.localScale = new Vector3(0.09f, 1.5f, 0.09f);
                foreMast.GetComponent<Renderer>().sharedMaterial = hullMat;
                Object.DestroyImmediate(foreMast.GetComponent<Collider>());

                // Fore Curved Sail
                CreateCurvedBattenedSail(boatRoot.transform, "Fore_Curved_Sail", new Vector3(2.2f, 0.8f, 0f), new Vector3(0f, 16f, -2f), 2.2f, 2.4f, sailMat, bambooMat);
            }

            // 4. Bow Oil Lantern
            if (glowMat != null)
            {
                GameObject lantern = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                lantern.name = "Bow_Lantern";
                lantern.transform.SetParent(boatRoot.transform, false);
                lantern.transform.localPosition = new Vector3(3.2f, 1.05f, 0f);
                lantern.transform.localScale = Vector3.one * 0.28f;
                lantern.GetComponent<Renderer>().sharedMaterial = glowMat;
                Object.DestroyImmediate(lantern.GetComponent<Collider>());

                Light l = lantern.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(1.0f, 0.78f, 0.40f);
                l.intensity = 2.5f;
                l.range = 5.5f;
                l.shadows = LightShadows.Soft;
            }

            var boatFloat = boatRoot.AddComponent<FloatingBoatSimple>();
            boatFloat.SetParameters(0.045f, 1.15f, 1.8f, 0.85f);
        }

        private static GameObject CreateCurvedBattenedSail(Transform parent, string name, Vector3 pos, Vector3 rot, float width, float height, Material sailMat, Material bambooMat)
        {
            GameObject sailObj = new GameObject(name);
            sailObj.transform.SetParent(parent, false);
            sailObj.transform.localPosition = pos;
            sailObj.transform.localRotation = Quaternion.Euler(rot);

            MeshFilter mf = sailObj.AddComponent<MeshFilter>();
            MeshRenderer mr = sailObj.AddComponent<MeshRenderer>();

            int segX = 8;
            int segY = 8;
            Mesh mesh = new Mesh { name = name + "_Mesh" };

            Vector3[] verts = new Vector3[(segX + 1) * (segY + 1)];
            Vector2[] uvs = new Vector2[verts.Length];
            int[] tris = new int[segX * segY * 6];

            for (int y = 0, i = 0; y <= segY; y++)
            {
                float vy = (float)y / segY;
                // Traditional Vietnamese bat-wing junk sail profile (Ghe Bầu / Ghe Lưới)
                float batWingCurve = Mathf.Sin(vy * Mathf.PI * 0.85f) * 0.22f;
                float taper = Mathf.Lerp(1.0f, 0.45f, vy) + batWingCurve;
                float currentW = width * taper;
                float yPos = vy * height;

                for (int x = 0; x <= segX; x++, i++)
                {
                    float vx = (float)x / segX;
                    float xPos = (vx - 0.5f) * currentW;
                    // Billowing cloth belly catching the wind
                    float belly = Mathf.Sin(vx * Mathf.PI) * (0.28f + vy * 0.15f);

                    verts[i] = new Vector3(xPos, yPos, belly);
                    uvs[i] = new Vector2(vx, vy);
                }
            }

            for (int y = 0, ti = 0, vi = 0; y < segY; y++, vi++)
            {
                for (int x = 0; x < segX; x++, ti += 6, vi++)
                {
                    tris[ti] = vi;
                    tris[ti + 1] = vi + segX + 1;
                    tris[ti + 2] = vi + 1;
                    tris[ti + 3] = vi + 1;
                    tris[ti + 4] = vi + segX + 1;
                    tris[ti + 5] = vi + segX + 2;
                }
            }

            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            mf.sharedMesh = mesh;
            mr.sharedMaterial = sailMat;

            // Bamboo horizontal battens across the sail
            for (int b = 1; b <= 3; b++)
            {
                float vy = (float)b / 4f;
                float batWingCurve = Mathf.Sin(vy * Mathf.PI * 0.85f) * 0.22f;
                float battenTaper = Mathf.Lerp(1.0f, 0.45f, vy) + batWingCurve;
                float battenY = vy * height;
                CreateBeam(sailObj.transform, $"Batten_{b}", new Vector3(0f, battenY, 0.18f), new Vector3(width * battenTaper, 0.045f, 0.045f), bambooMat);
            }

            return sailObj;
        }

        private static void CreateSampanXuongBaLa(Transform parent, string name, Vector3 pos, Vector3 rot, float scale, Material woodMat, Material bambooMat)
        {
            GameObject sampan = new GameObject(name);
            sampan.transform.SetParent(parent, false);
            sampan.transform.position = pos;
            sampan.transform.rotation = Quaternion.Euler(rot);
            sampan.transform.localScale = Vector3.one * scale;

            GameObject hull = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hull.name = "Sampan_Hull";
            hull.transform.SetParent(sampan.transform, false);
            hull.transform.localPosition = new Vector3(0f, 0.12f, 0f);
            hull.transform.localScale = new Vector3(2.6f, 0.32f, 0.80f);
            hull.GetComponent<Renderer>().sharedMaterial = woodMat;
            Object.DestroyImmediate(hull.GetComponent<Collider>());

            GameObject bow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bow.name = "Sampan_Bow";
            bow.transform.SetParent(sampan.transform, false);
            bow.transform.localPosition = new Vector3(1.4f, 0.20f, 0f);
            bow.transform.localRotation = Quaternion.Euler(0f, 0f, -20f);
            bow.transform.localScale = new Vector3(0.65f, 0.30f, 0.60f);
            bow.GetComponent<Renderer>().sharedMaterial = woodMat;
            Object.DestroyImmediate(bow.GetComponent<Collider>());

            GameObject stern = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stern.name = "Sampan_Stern";
            stern.transform.SetParent(sampan.transform, false);
            stern.transform.localPosition = new Vector3(-1.4f, 0.20f, 0f);
            stern.transform.localRotation = Quaternion.Euler(0f, 0f, 20f);
            stern.transform.localScale = new Vector3(0.65f, 0.30f, 0.60f);
            stern.GetComponent<Renderer>().sharedMaterial = woodMat;
            Object.DestroyImmediate(stern.GetComponent<Collider>());

            // Wooden rowing oar
            GameObject oar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            oar.name = "Rowing_Oar";
            oar.transform.SetParent(sampan.transform, false);
            oar.transform.localPosition = new Vector3(-0.35f, 0.32f, 0.2f);
            oar.transform.localRotation = Quaternion.Euler(15f, 35f, 75f);
            oar.transform.localScale = new Vector3(0.04f, 1.1f, 0.04f);
            oar.GetComponent<Renderer>().sharedMaterial = bambooMat;
            Object.DestroyImmediate(oar.GetComponent<Collider>());

            var fl = sampan.AddComponent<FloatingBoatSimple>();
            fl.SetParameters(0.055f, 1.35f, 2.5f, 1.1f);
        }

        // =========================================================================
        // VILLAGE & COLONIAL ARCHITECTURE
        // =========================================================================
        private static void CreateStiltHouse(Transform parent, string name, Vector3 pos, Vector3 size, Material wallMat, Material roofMat, Material glowMat)
        {
            GameObject hut = new GameObject(name);
            hut.transform.SetParent(parent, false);
            hut.transform.position = pos;

            // Wooden Stilts underneath
            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    GameObject stilt = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    stilt.name = "Stilt_Piling";
                    stilt.transform.SetParent(hut.transform, false);
                    stilt.transform.localPosition = new Vector3(x * (size.x * 0.42f), -size.y * 0.85f, z * (size.z * 0.42f));
                    stilt.transform.localScale = new Vector3(0.18f, size.y * 0.85f, 0.18f);
                    stilt.GetComponent<Renderer>().sharedMaterial = wallMat;
                    Object.DestroyImmediate(stilt.GetComponent<Collider>());
                }
            }

            // Main Walls (Body)
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "House_Walls";
            body.transform.SetParent(hut.transform, false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = size;
            body.GetComponent<Renderer>().sharedMaterial = wallMat;

            // Front Veranda Balcony facing water
            GameObject veranda = GameObject.CreatePrimitive(PrimitiveType.Cube);
            veranda.name = "Front_Veranda";
            veranda.transform.SetParent(hut.transform, false);
            veranda.transform.localPosition = new Vector3(0f, -size.y * 0.45f, -size.z * 0.65f);
            veranda.transform.localScale = new Vector3(size.x, 0.12f, 1.3f);
            veranda.GetComponent<Renderer>().sharedMaterial = wallMat;
            Object.DestroyImmediate(veranda.GetComponent<Collider>());

            // Pitched Roof (Double-slope)
            float halfW = size.x * 0.5f;
            float overhang = 0.55f;

            GameObject roofL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roofL.name = "Roof_L";
            roofL.transform.SetParent(hut.transform, false);
            roofL.transform.localPosition = new Vector3(-halfW * 0.5f, size.y * 0.5f + 0.45f, 0f);
            roofL.transform.localRotation = Quaternion.Euler(0f, 0f, 26f);
            roofL.transform.localScale = new Vector3(halfW + overhang, 0.12f, size.z + overhang * 1.5f);
            roofL.GetComponent<Renderer>().sharedMaterial = roofMat;
            Object.DestroyImmediate(roofL.GetComponent<Collider>());

            GameObject roofR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roofR.name = "Roof_R";
            roofR.transform.SetParent(hut.transform, false);
            roofR.transform.localPosition = new Vector3(halfW * 0.5f, size.y * 0.5f + 0.45f, 0f);
            roofR.transform.localRotation = Quaternion.Euler(0f, 0f, -26f);
            roofR.transform.localScale = new Vector3(halfW + overhang, 0.12f, size.z + overhang * 1.5f);
            roofR.GetComponent<Renderer>().sharedMaterial = roofMat;
            Object.DestroyImmediate(roofR.GetComponent<Collider>());

            // Warm Oil Lantern under veranda
            if (glowMat != null)
            {
                GameObject lantern = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                lantern.name = "Veranda_Lantern";
                lantern.transform.SetParent(hut.transform, false);
                lantern.transform.localPosition = new Vector3(-halfW * 0.45f, size.y * 0.25f, -size.z * 0.55f);
                lantern.transform.localScale = Vector3.one * 0.22f;
                lantern.GetComponent<Renderer>().sharedMaterial = glowMat;
                Object.DestroyImmediate(lantern.GetComponent<Collider>());

                Light l = lantern.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(1.0f, 0.78f, 0.40f);
                l.intensity = 2.4f;
                l.range = 5.5f;
                l.shadows = LightShadows.Soft;
            }
        }

        private static void CreateColonialTwoStoryBuilding(Transform parent, string name, Vector3 pos, Material wallMat, Material shutterMat, Material roofMat, Material glowMat)
        {
            GameObject colonial = new GameObject(name);
            colonial.transform.SetParent(parent, false);
            colonial.transform.position = pos;

            // 1. Main 2-Story Structure (Nhà Phố Kiểu Pháp 1954)
            GameObject buildingBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            buildingBody.name = "Colonial_Main_Facade";
            buildingBody.transform.SetParent(colonial.transform, false);
            buildingBody.transform.localPosition = new Vector3(0f, 2.5f, 0f);
            buildingBody.transform.localScale = new Vector3(5.6f, 5.0f, 5.0f);
            buildingBody.GetComponent<Renderer>().sharedMaterial = wallMat;

            // 2. Ground Floor: 3 Arched Entrances (Vòm Cửa Cuốn)
            float[] archX = { -1.6f, 0f, 1.6f };
            for (int i = 0; i < archX.Length; i++)
            {
                GameObject arch = GameObject.CreatePrimitive(PrimitiveType.Cube);
                arch.name = $"Ground_Archway_{i}";
                arch.transform.SetParent(colonial.transform, false);
                arch.transform.localPosition = new Vector3(archX[i], 1.10f, -2.52f);
                arch.transform.localScale = new Vector3(1.05f, 2.1f, 0.08f);
                arch.GetComponent<Renderer>().sharedMaterial = wallMat;
                Object.DestroyImmediate(arch.GetComponent<Collider>());
            }

            // 3. Second Floor: 3 French Windows with Dark Green Shutters
            for (int i = 0; i < archX.Length; i++)
            {
                GameObject win = GameObject.CreatePrimitive(PrimitiveType.Cube);
                win.name = $"Upper_French_Window_{i}";
                win.transform.SetParent(colonial.transform, false);
                win.transform.localPosition = new Vector3(archX[i], 3.65f, -2.52f);
                win.transform.localScale = new Vector3(0.85f, 1.40f, 0.08f);
                win.GetComponent<Renderer>().sharedMaterial = shutterMat;
                Object.DestroyImmediate(win.GetComponent<Collider>());
            }

            // 4. Second Floor Balcony & Cornice
            GameObject balcony = GameObject.CreatePrimitive(PrimitiveType.Cube);
            balcony.name = "Colonial_Balcony";
            balcony.transform.SetParent(colonial.transform, false);
            balcony.transform.localPosition = new Vector3(0f, 2.75f, -2.80f);
            balcony.transform.localScale = new Vector3(5.2f, 0.20f, 0.60f);
            balcony.GetComponent<Renderer>().sharedMaterial = wallMat;
            Object.DestroyImmediate(balcony.GetComponent<Collider>());

            GameObject cornice = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cornice.name = "Roof_Cornice_Moulding";
            cornice.transform.SetParent(colonial.transform, false);
            cornice.transform.localPosition = new Vector3(0f, 5.05f, 0f);
            cornice.transform.localScale = new Vector3(6.0f, 0.26f, 5.4f);
            cornice.GetComponent<Renderer>().sharedMaterial = wallMat;
            Object.DestroyImmediate(cornice.GetComponent<Collider>());

            // 5. Hip Roof (Mái Ngói Cổ)
            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "Colonial_Hip_Roof";
            roof.transform.SetParent(colonial.transform, false);
            roof.transform.localPosition = new Vector3(0f, 5.65f, 0f);
            roof.transform.localScale = new Vector3(5.8f, 1.0f, 5.2f);
            roof.GetComponent<Renderer>().sharedMaterial = roofMat;
            Object.DestroyImmediate(roof.GetComponent<Collider>());

            // Warm Lantern under colonial porch
            if (glowMat != null)
            {
                GameObject lantern = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                lantern.name = "Colonial_Porch_Lantern";
                lantern.transform.SetParent(colonial.transform, false);
                lantern.transform.localPosition = new Vector3(0f, 2.10f, -2.65f);
                lantern.transform.localScale = Vector3.one * 0.24f;
                lantern.GetComponent<Renderer>().sharedMaterial = glowMat;
                Object.DestroyImmediate(lantern.GetComponent<Collider>());

                Light l = lantern.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(1.0f, 0.82f, 0.48f);
                l.intensity = 2.8f;
                l.range = 6.5f;
                l.shadows = LightShadows.Soft;
            }
        }

        private static void CreateWalkwayRamp(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
        {
            GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ramp.name = name;
            ramp.transform.SetParent(parent, false);
            ramp.transform.position = pos;
            ramp.transform.localScale = size;
            ramp.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(ramp.GetComponent<Collider>());
        }

        // =========================================================================
        // VEGETATION & FOREGROUND CINEMATIC FRAMING
        // =========================================================================
        private static void CreateMidgroundMangroveCluster(Transform parent, Vector3 pos, Material barkMat, Material leafMat)
        {
            GameObject root = new GameObject("Midground_Mangrove_Island");
            root.transform.SetParent(parent, false);
            root.transform.position = pos;

            // Arched Aerial Stilt Roots (Rễ Đước Chống Nước)
            for (int i = 0; i < 12; i++)
            {
                float angle = i * (360f / 12f) * Mathf.Deg2Rad;
                float radius = Random.Range(0.9f, 1.8f);
                float rx = Mathf.Cos(angle) * radius;
                float rz = Mathf.Sin(angle) * radius;

                GameObject rootStrand = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                rootStrand.name = $"Mangrove_Stilt_Root_{i}";
                rootStrand.transform.SetParent(root.transform, false);
                rootStrand.transform.localPosition = new Vector3(rx * 0.5f, 0.45f, rz * 0.5f);
                rootStrand.transform.localRotation = Quaternion.Euler(rz * 15f, 0f, -rx * 15f);
                rootStrand.transform.localScale = new Vector3(0.10f, 0.95f, 0.10f);
                rootStrand.GetComponent<Renderer>().sharedMaterial = barkMat;
                Object.DestroyImmediate(rootStrand.GetComponent<Collider>());
            }

            // Central Trunk Base
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Mangrove_Central_Trunk";
            trunk.transform.SetParent(root.transform, false);
            trunk.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            trunk.transform.localScale = new Vector3(0.50f, 0.85f, 0.50f);
            trunk.GetComponent<Renderer>().sharedMaterial = barkMat;
            Object.DestroyImmediate(trunk.GetComponent<Collider>());

            // Layered Foliage Canopy Spheres
            Vector3[] canopyOffsets = {
                new Vector3(0f, 2.1f, 0f),
                new Vector3(-0.7f, 1.9f, 0.3f),
                new Vector3(0.8f, 2.0f, -0.3f),
                new Vector3(-0.3f, 2.3f, -0.5f)
            };
            Vector3[] canopyScales = {
                new Vector3(2.8f, 1.6f, 2.4f),
                new Vector3(2.0f, 1.3f, 1.8f),
                new Vector3(2.2f, 1.4f, 2.0f),
                new Vector3(1.8f, 1.2f, 1.6f)
            };

            for (int i = 0; i < canopyOffsets.Length; i++)
            {
                GameObject leafSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                leafSphere.name = $"Mangrove_Canopy_{i}";
                leafSphere.transform.SetParent(root.transform, false);
                leafSphere.transform.localPosition = canopyOffsets[i];
                leafSphere.transform.localScale = canopyScales[i];
                leafSphere.GetComponent<Renderer>().sharedMaterial = leafMat;
                Object.DestroyImmediate(leafSphere.GetComponent<Collider>());
            }
        }

        private static void CreateCoconutPalm(Transform parent, string name, Vector3 pos, float leanAngle, float scale, Material trunkMat, Material frondMat)
        {
            GameObject palm = new GameObject(name);
            palm.transform.SetParent(parent, false);
            palm.transform.position = pos;
            palm.transform.localScale = Vector3.one * scale;

            float currY = 0f;
            float currX = 0f;
            float segH = 1.3f;

            for (int i = 0; i < 4; i++)
            {
                GameObject seg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                seg.name = $"Trunk_Seg_{i}";
                seg.transform.SetParent(palm.transform, false);
                seg.transform.localPosition = new Vector3(currX, currY + segH * 0.5f, 0f);
                seg.transform.localRotation = Quaternion.Euler(0f, 0f, leanAngle * (0.3f + i * 0.25f));
                seg.transform.localScale = new Vector3(0.22f - i * 0.025f, segH * 0.5f, 0.22f - i * 0.025f);
                seg.GetComponent<Renderer>().sharedMaterial = trunkMat;
                Object.DestroyImmediate(seg.GetComponent<Collider>());

                currY += segH * 0.92f;
                currX += Mathf.Sin(leanAngle * Mathf.Deg2Rad) * 0.32f;
            }

            for (int f = 0; f < 8; f++)
            {
                float rot = f * (360f / 8f);
                GameObject frond = GameObject.CreatePrimitive(PrimitiveType.Quad);
                frond.name = $"Palm_Frond_{f}";
                frond.transform.SetParent(palm.transform, false);
                frond.transform.localPosition = new Vector3(currX, currY, 0f);
                frond.transform.localRotation = Quaternion.Euler(42f, rot, 0f);
                frond.transform.localScale = new Vector3(0.95f, 2.8f, 1f);
                frond.GetComponent<Renderer>().sharedMaterial = frondMat;
                Object.DestroyImmediate(frond.GetComponent<Collider>());
            }
        }

        // =========================================================================
        // VFX & ATMOSPHERE
        // =========================================================================
        private static void BuildAtmosphericParticles(Transform parent, Material motesMat)
        {
            // Golden Dust Motes (Bụi Nắng Hoàng Hôn)
            GameObject motesObj = new GameObject("Atmosphere_GoldenMotes");
            motesObj.transform.SetParent(parent, false);
            motesObj.transform.position = new Vector3(0f, 1.5f, 0f);

            var ps = motesObj.AddComponent<ParticleSystem>();
            var psRenderer = motesObj.GetComponent<ParticleSystemRenderer>();

            var main = ps.main;
            main.startLifetime = 7f;
            main.startSpeed = 0.22f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.16f);
            main.startColor = new Color(1.0f, 0.88f, 0.55f, 0.75f);
            main.maxParticles = 85;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 12f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(45f, 7f, 10f);

            var velOverLifetime = ps.velocityOverLifetime;
            velOverLifetime.enabled = true;
            velOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.10f, 0.25f);
            velOverLifetime.y = new ParticleSystem.MinMaxCurve(0.05f, 0.16f);
            velOverLifetime.z = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);

            var colOverLifetime = ps.colorOverLifetime;
            colOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.85f, 0.5f), 0f), new GradientColorKey(new Color(1f, 0.95f, 0.7f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.85f, 0.25f), new GradientAlphaKey(0.85f, 0.75f), new GradientAlphaKey(0f, 1f) }
            );
            colOverLifetime.color = grad;

            if (motesMat != null) psRenderer.sharedMaterial = motesMat;

            // River Water Mist (Sương Khói Sông Nước Nam Bộ)
            GameObject mistObj = new GameObject("River_Surface_Mist");
            mistObj.transform.SetParent(parent, false);
            mistObj.transform.position = new Vector3(0f, -0.8f, 4.0f);

            var psMist = mistObj.AddComponent<ParticleSystem>();
            var psMistRenderer = mistObj.GetComponent<ParticleSystemRenderer>();

            var mistMain = psMist.main;
            mistMain.startLifetime = 10f;
            mistMain.startSpeed = 0.12f;
            mistMain.startSize = new ParticleSystem.MinMaxCurve(3.0f, 5.5f);
            mistMain.startColor = new Color(0.95f, 0.82f, 0.70f, 0.08f);
            mistMain.maxParticles = 30;
            mistMain.simulationSpace = ParticleSystemSimulationSpace.World;

            var mistEmission = psMist.emission;
            mistEmission.rateOverTime = 3f;

            var mistShape = psMist.shape;
            mistShape.shapeType = ParticleSystemShapeType.Box;
            mistShape.scale = new Vector3(55f, 0.6f, 20f);

            if (motesMat != null) psMistRenderer.sharedMaterial = motesMat;
        }

        // =========================================================================
        // HERO CHARACTER
        // =========================================================================
        private static GameObject SpawnHeroCharacter(Transform parent, Vector3 spawnPos)
        {
            GameObject charPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/cha_rigged_animations.fbx");
            if (charPrefab == null) charPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/nhan vat chinh 3d.fbx");
            if (charPrefab == null) return null;

            GameObject player = Object.Instantiate(charPrefab, parent);
            player.name = "Hero_Boy_Runner";
            player.tag = "Player";
            player.transform.position = spawnPos;
            player.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            player.transform.localScale = Vector3.one * 0.95f;

            Texture2D charTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/MeshyImports/Boy Character Rig_20260926_073036/meshy_basecolor.png");
            if (charTex == null) charTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/MeshyImports/Boy Character Rig_20260926_073041/meshy_basecolor.png");

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material heroMat = new Material(urpLit) { name = "M_HeroBoy_Lit" };
            if (charTex != null)
            {
                heroMat.mainTexture = charTex;
                heroMat.SetColor("_BaseColor", Color.white);
            }

            var renderers = player.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                r.sharedMaterial = heroMat;
            }

            var cc = player.GetComponent<CharacterController>();
            if (cc == null) cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            var playerCtrl = player.GetComponent<PlayerController>();
            if (playerCtrl == null) playerCtrl = player.AddComponent<PlayerController>();

            var animator = player.GetComponent<Animator>();
            if (animator != null)
            {
                var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Models/gap bung.controller");
                if (controller != null) animator.runtimeAnimatorController = controller;
            }

            return player;
        }

        // =========================================================================
        // UTILITIES
        // =========================================================================
        private static void CreatePlatformCollider(Transform parent, string name, Vector3 pos, Vector3 size)
        {
            GameObject colObj = new GameObject(name);
            colObj.transform.SetParent(parent, false);
            colObj.transform.position = pos;
            var box = colObj.AddComponent<BoxCollider>();
            box.size = size;
        }

        private static void CreateJumpSection(Transform parent, string name, Vector3 pos, Vector3 size)
        {
            GameObject zone = new GameObject(name);
            zone.transform.SetParent(parent, false);
            zone.transform.position = pos;
            var box = zone.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = size;
        }

        private static void CreateBeam(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beam.name = name;
            beam.transform.SetParent(parent, false);
            beam.transform.localPosition = pos;
            beam.transform.localScale = scale;
            beam.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(beam.GetComponent<Collider>());
        }

        private static Material GetOrCreateMaterial(string name, Color baseColor, float smoothness)
        {
            string dir = "Assets/Materials";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            string path = $"{dir}/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            if (mat == null)
            {
                mat = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.color = baseColor;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", baseColor);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
