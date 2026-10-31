using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using HoaVaMau.Gameplay;

namespace HoaDo.Editor
{
    public static class RebuildMapFromReferenceArtwork
    {
        private const float UNIFORM_GROUND_Y = 0.50f;

        [MenuItem("Tools/Hoa Do/Rebuild Ca Mau Village (1:1 Reference Artwork)")]
        public static void Execute()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (!activeScene.path.Contains("CaMau_1954_Village"))
            {
                Debug.LogWarning("[RebuildMap] Opening CaMau_1954_Village.unity...");
                EditorSceneManager.OpenScene("Assets/Scenes/CaMau_1954_Village.unity");
            }

            Debug.Log("[RebuildMap] Starting 1:1 reconstruction with sky, straight transition road, village gate, and clean beach...");

            // 1. Materials setup (Unified Seamless 2K Textures - ZERO checkerboard / ZERO square patches)
            var matGrass = GetOrCreateMaterial("Assets/Materials/M_CaMau_Landscape_Grass.mat", new Color(0.52f, 0.72f, 0.30f, 1.0f), "Assets/Shaders/Uber Stylized Water/Demo/Terrain/ground_05_baseColor_2k.png", new Vector2(8f, 3f));
            var matRedDirt = GetOrCreateMaterial("Assets/Materials/M_Village_Road_NamBo.mat", new Color(0.72f, 0.54f, 0.38f, 1.0f), "Assets/Shaders/Uber Stylized Water/Demo/Terrain/sand_01_color_2k.png", new Vector2(4f, 4f));
            var matSand = GetOrCreateMaterial("Assets/Materials/M_Seabed_Sand.mat", new Color(0.88f, 0.80f, 0.62f, 1.0f), "Assets/Shaders/Uber Stylized Water/Demo/Terrain/sand_01_color_2k.png", new Vector2(8f, 3f));
            var matRiceGreen = GetOrCreateMaterial("Assets/Materials/LanaOverhaul/M_RiceCrop_LushGreen.mat", new Color(0.46f, 0.78f, 0.22f, 1.0f), "Assets/Shaders/Uber Stylized Water/Demo/Terrain/ground_05_baseColor_2k.png", new Vector2(1.5f, 1.0f));
            var matRiceGolden = GetOrCreateMaterial("Assets/Materials/LanaOverhaul/M_RiceCrop_GoldenRipe.mat", new Color(0.88f, 0.74f, 0.22f, 1.0f), "Assets/Shaders/Uber Stylized Water/Demo/Terrain/ground_05_baseColor_2k.png", new Vector2(1.5f, 1.0f));

            var matWater = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_RiverWater_Canh1.mat");
            if (matWater == null) matWater = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/LanaOverhaul/M_LivingOcean_WaterSurface.mat");
            if (matWater != null)
            {
                if (matWater.HasProperty("_BaseColor")) matWater.SetColor("_BaseColor", new Color(0.12f, 0.68f, 0.74f, 0.88f));
                if (matWater.HasProperty("_Color")) matWater.SetColor("_Color", new Color(0.12f, 0.68f, 0.74f, 0.88f));
                EditorUtility.SetDirty(matWater);
            }

            var matPierWood = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/GhibliModular/M_Ghibli_PierPlank.mat");
            var matBamboo = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/CaMau/M_CaMau_Bamboo.mat");
            var matRock = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Rocks/M_Rock_Platform_01.mat");
            if (matRock == null) matRock = AssetDatabase.LoadAssetAtPath<Material>("Assets/TripoModels/rock_outcrop_3d_model/Materials/rock_outcrop_3d_model.mat");
            if (matRock == null) matRock = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/GhibliModular/M_Ghibli_PebbleRock.mat");

            var matThatched = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Nhà tranh.mat");
            if (matThatched != null)
            {
                matThatched.SetFloat("_Cull", 0f); // Two-sided
                if (matThatched.HasProperty("_BaseColor")) matThatched.SetColor("_BaseColor", Color.white);
                EditorUtility.SetDirty(matThatched);
            }

            var matSkySurround = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Backdrop_Sky/M_Sky_Backdrop_Surround.mat");

            // Configure Rice shader materials for automatic wind sway
            ConfigureRiceMaterials();

            // 2. Clear previous dynamic layout
            ClearOldSceneLayout();

            // 3. Build 3D Sky Enclosure & Northern Panoramic Canvas
            BuildSkyBackdropAndSurround(matSkySurround);

            // 4. Build 3D Terrain (Pristine clean beach, zero rocks on sand, uniform Y = 0.50m)
            Build3DTerrain(matGrass, matSand, matRock);

            // 5. Build Turquoise Ocean & Underwater Reefs (Clean sandy beach)
            BuildTurquoiseOceanAndReefs(matWater, matRock);

            // 6. Build Earthen Road Network including Central Straight Road through Rice Fields
            BuildEarthenRoadNetwork(matRedDirt);

            // 7. Build the 10 Thatched Cottages (grounded firmly, tight wall colliders)
            Build10ThatchedCottages(matThatched);

            // 8. Build Central Sacred Shade Tree & Courtyard (volumetric 3D, upright, zero animation)
            BuildCentralSacredTreeAndPlaza(matRock);

            // 9. Build Coastal Piers, Harbor Complex & Traditional Boats (NO CRATES / NO BARRELS)
            BuildCoastalPiersAndBoats(matPierWood, matThatched);

            // 10. Build Rice Paddies with straight parallel rows & old sway animation, plus duck pond
            BuildRicePaddiesAndDuckPond(matRiceGreen, matRiceGolden, matWater, matGrass, matBamboo, matThatched);

            // 11. Build Village Gate (Cổng Làng) & Scene Transition Trigger at North Exit
            BuildVillageGateAndSceneTransition();

            // 12. Place Tropical Foliage & Living Village Props (3D volumetric upright trees, colored clothesline)
            BuildTropicalFoliageAndProps(matBamboo, matRedDirt, matPierWood);

            // 13. Position Spawn Points & NPCs (Grounded firmly, zero sinking)
            PositionSpawnPointsAndNPCs();

            // 14. Update Dialogue Triggers (Concise, authentic Southern Vietnamese dialogues)
            UpdateDialogueTriggersConcise();

            // 15. Setup Lighting & Atmosphere
            SetupLightingAndAtmosphere();

            // 16. Setup Cameras (Overview with Sky + Gameplay)
            SetupCameras();

            Physics.SyncTransforms();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log("[RebuildMap] Ca Mau Coastal Village Rebuilt Successfully: Sky added, beach rocks removed, straight road with Village Gate and scene transition trigger established!");
        }

        private static Material GetOrCreateMaterial(string path, Color col, string texPath, Vector2 tiling)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!string.IsNullOrEmpty(texPath))
            {
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
                if (tex != null) mat.mainTexture = tex;
            }
            mat.mainTextureScale = tiling;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", col);
            mat.SetFloat("_Smoothness", 0.05f);

            if (path.Contains("Grass"))
            {
                var normTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Shaders/Uber Stylized Water/Demo/Terrain/ground_05_normal_gl_2k.png");
                if (normTex != null && mat.HasProperty("_BumpMap"))
                {
                    mat.SetTexture("_BumpMap", normTex);
                    mat.EnableKeyword("_NORMALMAP");
                }
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material GetOrCreateUnlitMaterial(string path, string texPath, bool transparent, int renderQueue)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (!string.IsNullOrEmpty(texPath))
            {
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
                if (tex != null) mat.mainTexture = tex;
            }
            mat.SetFloat("_Surface", transparent ? 1f : 0f);
            mat.SetFloat("_Blend", 0f); // Alpha blend
            mat.SetFloat("_Cull", 0f);  // Double-sided
            mat.SetFloat("_ZWrite", transparent ? 0f : 1f);
            mat.renderQueue = renderQueue;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);

            if (transparent)
            {
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.DisableKeyword("_SURFACE_TYPE_OPAQUE");
            }
            else
            {
                mat.SetOverrideTag("RenderType", "Opaque");
                mat.EnableKeyword("_SURFACE_TYPE_OPAQUE");
                mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void ConfigureRiceMaterials()
        {
            var matNames = new string[] { "Mat_Rice_Meshy", "Mat_Rice_Tripo" };
            foreach (var name in matNames)
            {
                var guids = AssetDatabase.FindAssets($"{name} t:Material");
                foreach (var g in guids)
                {
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g));
                    if (mat != null)
                    {
                        if (mat.HasProperty("_WindSwayStrength")) mat.SetFloat("_WindSwayStrength", 0.25f);
                        if (mat.HasProperty("_WindFrequency")) mat.SetFloat("_WindFrequency", 1.6f);
                        if (mat.HasProperty("_Turbulence")) mat.SetFloat("_Turbulence", 0.35f);
                        if (mat.HasProperty("_GrassStiffness")) mat.SetFloat("_GrassStiffness", 1.0f);
                        if (mat.HasProperty("_EnablePlayerPush")) mat.SetFloat("_EnablePlayerPush", 1.0f);
                        if (mat.HasProperty("_PushStrength")) mat.SetFloat("_PushStrength", 0.85f);
                        EditorUtility.SetDirty(mat);
                    }
                }
            }
        }

        private static void ClearOldSceneLayout()
        {
            string[] roots = new string[]
            {
                "CaMau_1954_Village/Environment/Terrain_3D",
                "CaMau_1954_Village/Environment/WaterSystem_3D",
                "CaMau_1954_Village/Environment/Paths_3D_Network",
                "CaMau_1954_Village/Environment/Village_Houses_Network",
                "CaMau_1954_Village/Environment/Village_Fences_Network",
                "CaMau_1954_Village/Environment/Vegetation_3D",
                "CaMau_1954_Village/Environment/Background_Atmosphere",
                "CaMau_1954_Village/Environment/09_Natural_Painterly_Background",
                "CaMau_1954_Village/Village_Areas/08_Ben_Xuong"
            };

            foreach (var rPath in roots)
            {
                var go = GameObject.Find(rPath);
                if (go != null)
                {
                    for (int i = go.transform.childCount - 1; i >= 0; i--)
                    {
                        var child = go.transform.GetChild(i);
                        if (child.name.StartsWith("NPC_")) continue;
                        Object.DestroyImmediate(child.gameObject);
                    }
                }
            }

            string[] standalone = new string[]
            {
                "Cliff_West_Coastal", "Cliff_East_HarborHead", "Gate_Village_North",
                "Road_North_StraightPaddyAisle", "VillageExit_NorthGate_Transition",
                "Sky_Surround_Cylinder_360", "Sky_Surround_Dome_360", "Backdrop_SkyNorth_Panoramic",
                "Clouds_Atmosphere_3D",
                "Crate_01", "Crate_02", "Crate_WestPier", "Barrel_01", "Barrel_02",
                "Terrain_West_Headland", "Terrain_West_BeachSlope",
                "Terrain_East_Headland", "Terrain_East_BeachSlope",
                "Cliff_West_Rice_North", "Cliff_West_Rice_South",
                "Cliff_East_Rice_North", "Cliff_East_Rice_South",
                "Terrain_North_Backdrop_Hill", "MountainRanges_3D_Flanking",
                "MountainRange_West_Flank", "MountainRange_East_Flank", "MountainRange_North_Backdrop",
                "Backdrop_Western_MistyHills", "Backdrop_Eastern_MistyHills"
            };
            foreach (var name in standalone)
            {
                var obj = GameObject.Find(name);
                if (obj != null) Object.DestroyImmediate(obj);
            }

            // Aggressive purge: Completely eradicate ANY crate, barrel, or thùng from entire scene
            var allSceneObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            var toPurge = new System.Collections.Generic.List<GameObject>();
            foreach (var go in allSceneObjects)
            {
                if (go == null) continue;
                string n = go.name.ToLower();
                if (n.Contains("crate") || n.Contains("barrel") || n.Contains("thung") || n.Contains("cargo") ||
                    n.Contains("cliff_west_rice") || n.Contains("cliff_east_rice") || n.Contains("terrain_north_backdrop"))
                {
                    toPurge.Add(go);
                }
            }
            foreach (var go in toPurge)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
        }

        // =========================================================================
        // 3. Sky Backdrop & Atmospheric Surround
        // =========================================================================
        private static void BuildSkyBackdropAndSurround(Material matSky)
        {
            var bgRoot = GetOrCreateRoot("CaMau_1954_Village/Environment/Background_Atmosphere");
            bgRoot.SetActive(true);

            // Clean up any old cylinder enclosure or previous mesh domes to guarantee ZERO stitching seams
            var oldDome = bgRoot.transform.Find("Sky_Surround_Dome_360");
            if (oldDome != null) Object.DestroyImmediate(oldDome.gameObject);
            var oldCyl = bgRoot.transform.Find("Sky_Surround_Cylinder_360");
            if (oldCyl != null) Object.DestroyImmediate(oldCyl.gameObject);
            var oldClouds = bgRoot.transform.Find("Clouds_Atmosphere_3D");
            if (oldClouds != null) Object.DestroyImmediate(oldClouds.gameObject);

            // 1. Configure Authentic Daylight 360 Cubemap Skybox (sky_22_2k: 100% Seamless, Zero Seam Lines, Warm Daylight)
            var skyMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Backdrop_Sky/M_Skybox_Village_CubemapDaylight.mat");
            if (skyMat != null)
            {
                skyMat.SetFloat("_Rotation", 180f);
                skyMat.SetFloat("_Exposure", 1.25f);
                skyMat.SetColor("_Tint", new Color(0.80f, 0.92f, 1.15f, 1.0f));
                EditorUtility.SetDirty(skyMat);
                RenderSettings.skybox = skyMat;
            }

            // 2. Southern Far Horizon - Deep Ocean Meeting Sky
            var matSeaHorizon = GetOrCreateUnlitMaterial(
                "Assets/Materials/Environment/Backdrop_Sky/M_Backdrop_SeaHorizon.mat",
                "Assets/Textures/Backgrounds_Lana/BG_Lana_SeaHorizon_Sunset.png",
                false, 2985);

            var seaHorizon = GameObject.CreatePrimitive(PrimitiveType.Quad);
            seaHorizon.name = "Backdrop_Southern_SeaHorizon";
            seaHorizon.transform.parent = bgRoot.transform;
            seaHorizon.transform.position = new Vector3(0.0f, 16.0f, -280.0f);
            seaHorizon.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // Facing North towards village & camera
            seaHorizon.transform.localScale = new Vector3(600.0f, 44.0f, 1f);
            seaHorizon.GetComponent<MeshRenderer>().sharedMaterial = matSeaHorizon;
            var cSea = seaHorizon.GetComponent<Collider>();
            if (cSea != null) Object.DestroyImmediate(cSea);
        }

        // =========================================================================
        // 4. 3D Terrain Construction (Pristine clean beach, zero rocks on sand)
        // =========================================================================
        private static void Build3DTerrain(Material matGrass, Material matSand, Material matRock)
        {
            var terrainRoot = GetOrCreateRoot("CaMau_1954_Village/Environment/Terrain_3D");

            // A. Single Unified Continuous Ground Plain (Top surface exactly at Y = 0.50m, trimmed cleanly to 152m width matching village bounds)
            var unifiedPlain = GameObject.CreatePrimitive(PrimitiveType.Cube);
            unifiedPlain.name = "Terrain_Unified_Ground_Base";
            unifiedPlain.transform.parent = terrainRoot.transform;
            unifiedPlain.transform.position = new Vector3(0.0f, UNIFORM_GROUND_Y - 0.25f, 38.0f);
            unifiedPlain.transform.localScale = new Vector3(152.0f, 0.50f, 96.0f);
            unifiedPlain.GetComponent<MeshRenderer>().sharedMaterial = matGrass;

            // B. Single Unified Continuous Sandy Shoreline Slope (Trimmed cleanly to 152m width, zero excess land wings)
            var beachSlope = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beachSlope.name = "Terrain_Beach_Slope";
            beachSlope.transform.parent = terrainRoot.transform;
            beachSlope.transform.position = new Vector3(0.0f, -0.04f, -14.0f);
            beachSlope.transform.rotation = Quaternion.Euler(7.7f, 0f, 0f);
            beachSlope.transform.localScale = new Vector3(152.0f, 0.50f, 8.5f);
            beachSlope.GetComponent<MeshRenderer>().sharedMaterial = matSand;

            // C. Sea Bed Floor (Extending deeply under the sea)
            var seaBed = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seaBed.name = "Terrain_Sea_Bed";
            seaBed.transform.parent = terrainRoot.transform;
            seaBed.transform.position = new Vector3(0.0f, -3.2f, -140.0f);
            seaBed.transform.localScale = new Vector3(600.0f, 0.6f, 280.0f);
            seaBed.GetComponent<MeshRenderer>().sharedMaterial = matSand;

            // D. 3D Mountain Ranges Flanking Both Sides of Rice Fields & Northern Mountain Pass
            BuildFlanking3DMountainRanges(terrainRoot.transform, matGrass, matRock);

            // E. Stone Retaining Wall along upper beach border
            BuildBeachRetainingWall(terrainRoot.transform, matRock);
        }

        private static void BuildBeachRetainingWall(Transform parent, Material matRock)
        {
            var wallRoot = new GameObject("Beach_Stone_RetainingWall");
            wallRoot.transform.parent = parent;

            CreateWallSegment(wallRoot.transform, "Wall_West", new Vector3(-50.5f, UNIFORM_GROUND_Y + 0.08f, -8.5f), new Vector3(29.0f, 0.35f, 0.8f), matRock);
            CreateWallSegment(wallRoot.transform, "Wall_Center", new Vector3(12.0f, UNIFORM_GROUND_Y + 0.08f, -8.5f), new Vector3(48.0f, 0.35f, 0.8f), matRock);
        }

        private static void CreateWallSegment(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.parent = parent;
            wall.transform.position = pos;
            wall.transform.localScale = size;
            if (mat != null) wall.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        // =========================================================================
        // 4.1. 3D Mountain Ranges Flanking Rice Fields & North Mountain Pass
        // =========================================================================
        private static void BuildFlanking3DMountainRanges(Transform parent, Material matGrass, Material matRock)
        {
            var mountainRoot = new GameObject("MountainRanges_3D_Flanking");
            mountainRoot.transform.parent = parent;

            // Load rich 3D rock models (100% volumetric 3D meshes, ZERO flat 2D cutouts)
            var rockPlatformPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/PF_Rock_Platform_CliffLedge.prefab");
            if (rockPlatformPrefab == null) rockPlatformPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Environment/SM_Rock_Platform_01.fbx");

            var rockCragPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TripoModels/rock_outcrop_3d_model/rock_outcrop_3d_model.fbx");
            if (rockCragPrefab == null) rockCragPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/PF_Rock_Outcrop.prefab");

            var rockLargeBoulder = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Optimized/SM_Rock_Large_BlenderOptimized.fbx");
            if (rockLargeBoulder == null) rockLargeBoulder = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/PF_Rock_Large_01.prefab");

            var rockMediumBoulder = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Optimized/SM_Rock_Medium_02_BlenderOptimized.fbx");
            if (rockMediumBoulder == null) rockMediumBoulder = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Environment/SM_Rock_Medium_01.fbx");

            var heroBanyanPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Foliage/PF_LanaVN_Tree_01_HeroBanyan.prefab");
            if (heroBanyanPrefab == null) heroBanyanPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Foliage/PF_Foliage_Canopy_Banyan.prefab");

            var coconutPalmPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Foliage/PF_Foliage_Coconut_Palm.prefab");
            var bananaPalmPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Foliage/PF_Foliage_Banana_Palm.prefab");

            // -------------------------------------------------------------
            // A. WEST FLANK MOUNTAIN RANGE (Dãy Núi Tây Bao Quanh Cánh Đồng Lúa)
            // Spanning X: -60 to -115, Z: 20 to 92
            // -------------------------------------------------------------
            var westRoot = new GameObject("MountainRange_West_Flank");
            westRoot.transform.parent = mountainRoot.transform;

            // Organic smooth hill meshes (Zero flat box steps, smooth zero-tangent connection to Y = 0.50m)
            CreateOrganicHillMesh(westRoot.transform, "Hill_West_RiceFlank", new Vector3(-80.0f, UNIFORM_GROUND_Y, 58.0f), 48.0f, 68.0f, 5.8f, matGrass);
            CreateOrganicHillMesh(westRoot.transform, "Hill_West_VillageFlank", new Vector3(-76.0f, UNIFORM_GROUND_Y, 20.0f), 44.0f, 42.0f, 4.8f, matGrass);

            // Tier 1 & 2: 3D Rock Platforms along the inner edge facing rice paddies
            InstantiateRock(rockPlatformPrefab, westRoot.transform, "West_Platform_01", new Vector3(-60.0f, 0.9f, 72.0f), new Vector3(3.0f, 2.4f, 2.8f), new Vector3(0f, 25f, 0f), matRock);
            InstantiateRock(rockPlatformPrefab, westRoot.transform, "West_Platform_02", new Vector3(-62.5f, 1.5f, 62.0f), new Vector3(3.4f, 2.8f, 3.2f), new Vector3(0f, -15f, 0f), matRock);
            InstantiateRock(rockPlatformPrefab, westRoot.transform, "West_Platform_03", new Vector3(-59.0f, 1.0f, 50.0f), new Vector3(2.8f, 2.2f, 2.6f), new Vector3(0f, 40f, 0f), matRock);
            InstantiateRock(rockPlatformPrefab, westRoot.transform, "West_Platform_04", new Vector3(-63.0f, 2.0f, 40.0f), new Vector3(3.6f, 3.0f, 3.4f), new Vector3(0f, 10f, 0f), matRock);
            InstantiateRock(rockPlatformPrefab, westRoot.transform, "West_Platform_05", new Vector3(-68.0f, 3.0f, 70.0f), new Vector3(4.0f, 3.6f, 3.8f), new Vector3(0f, -35f, 0f), matRock);
            InstantiateRock(rockPlatformPrefab, westRoot.transform, "West_Platform_06", new Vector3(-70.0f, 3.4f, 56.0f), new Vector3(4.2f, 4.0f, 3.8f), new Vector3(0f, 60f, 0f), matRock);
            InstantiateRock(rockPlatformPrefab, westRoot.transform, "West_Platform_07", new Vector3(-72.0f, 3.2f, 42.0f), new Vector3(3.8f, 3.4f, 3.4f), new Vector3(0f, -20f, 0f), matRock);
            InstantiateRock(rockPlatformPrefab, westRoot.transform, "West_Platform_08", new Vector3(-66.0f, 2.4f, 28.0f), new Vector3(3.4f, 2.6f, 3.2f), new Vector3(0f, 15f, 0f), matRock);

            // Tier 3: Towering 3D Rock Crags / Monoliths (Towering stone karsts rising into sky)
            InstantiateRock(rockCragPrefab, westRoot.transform, "West_Crag_01", new Vector3(-67.0f, 2.6f, 74.0f), new Vector3(7.0f, 10.0f, 6.5f), new Vector3(0f, 30f, 0f), matRock);
            InstantiateRock(rockCragPrefab, westRoot.transform, "West_Crag_02", new Vector3(-74.0f, 4.0f, 64.0f), new Vector3(8.5f, 13.0f, 8.0f), new Vector3(0f, -45f, 0f), matRock);
            InstantiateRock(rockCragPrefab, westRoot.transform, "West_Crag_03", new Vector3(-68.0f, 3.2f, 54.0f), new Vector3(7.5f, 11.0f, 7.0f), new Vector3(0f, 75f, 0f), matRock);
            InstantiateRock(rockCragPrefab, westRoot.transform, "West_Crag_04", new Vector3(-75.0f, 4.2f, 44.0f), new Vector3(9.0f, 13.5f, 8.5f), new Vector3(0f, -10f, 0f), matRock);
            InstantiateRock(rockCragPrefab, westRoot.transform, "West_Crag_05", new Vector3(-71.0f, 3.4f, 34.0f), new Vector3(8.0f, 11.5f, 7.5f), new Vector3(0f, 110f, 0f), matRock);
            InstantiateRock(rockCragPrefab, westRoot.transform, "West_Crag_HighPeak", new Vector3(-84.0f, 5.5f, 58.0f), new Vector3(11.0f, 17.0f, 10.0f), new Vector3(0f, 15f, 0f), matRock);

            // 3D Boulders along footing
            InstantiateRock(rockLargeBoulder, westRoot.transform, "West_Boulder_01", new Vector3(-58.5f, UNIFORM_GROUND_Y + 0.3f, 67.0f), new Vector3(2.4f, 1.8f, 2.2f), new Vector3(10f, 45f, 0f));
            InstantiateRock(rockMediumBoulder, westRoot.transform, "West_Boulder_02", new Vector3(-57.8f, UNIFORM_GROUND_Y + 0.2f, 46.0f), new Vector3(2.0f, 1.6f, 2.0f), new Vector3(-15f, 80f, 5f));
            InstantiateRock(rockLargeBoulder, westRoot.transform, "West_Boulder_03", new Vector3(-64.0f, UNIFORM_GROUND_Y + 0.5f, 34.0f), new Vector3(2.6f, 2.0f, 2.4f), new Vector3(5f, -30f, 10f));

            // Coastal headland rocks extending West mountain down to water (capping the West border naturally)
            InstantiateRock(rockPlatformPrefab, westRoot.transform, "West_Coast_Platform_01", new Vector3(-70.0f, 0.8f, 12.0f), new Vector3(3.5f, 2.6f, 3.2f), new Vector3(0f, 30f, 0f));
            InstantiateRock(rockPlatformPrefab, westRoot.transform, "West_Coast_Platform_02", new Vector3(-72.0f, 0.6f, -2.0f), new Vector3(3.8f, 2.8f, 3.4f), new Vector3(0f, -15f, 0f));
            InstantiateRock(rockPlatformPrefab, westRoot.transform, "West_Coast_Headland_01", new Vector3(-74.0f, 0.5f, -9.0f), new Vector3(3.5f, 2.5f, 3.2f), new Vector3(0f, 25f, 0f));
            InstantiateRock(rockCragPrefab, westRoot.transform, "West_Coast_Crag_01", new Vector3(-73.0f, 2.0f, 8.0f), new Vector3(7.0f, 10.0f, 6.5f), new Vector3(0f, 45f, 0f));
            InstantiateRock(rockCragPrefab, westRoot.transform, "West_Coast_Crag_02", new Vector3(-74.0f, 1.8f, -4.0f), new Vector3(6.5f, 9.5f, 6.0f), new Vector3(0f, -20f, 0f));

            // Crowning Vegetation on West Ridge
            if (heroBanyanPrefab != null)
            {
                InstantiateVolumetricTree(heroBanyanPrefab, westRoot.transform, "West_Ridge_Banyan_01", new Vector3(-77.0f, 5.2f, 68.0f), 1.60f);
                InstantiateVolumetricTree(heroBanyanPrefab, westRoot.transform, "West_Ridge_Banyan_02", new Vector3(-78.0f, 4.8f, 40.0f), 1.50f);
            }
            if (coconutPalmPrefab != null)
            {
                InstantiateVolumetricTree(coconutPalmPrefab, westRoot.transform, "West_Cliff_Palm_01", new Vector3(-64.0f, 2.4f, 66.0f), 1.25f);
                InstantiateVolumetricTree(coconutPalmPrefab, westRoot.transform, "West_Cliff_Palm_02", new Vector3(-63.0f, 2.0f, 46.0f), 1.20f);
                InstantiateVolumetricTree(coconutPalmPrefab, westRoot.transform, "West_Cliff_Palm_03", new Vector3(-69.0f, 3.8f, 55.0f), 1.30f);
            }
            if (bananaPalmPrefab != null)
            {
                InstantiateVolumetricTree(bananaPalmPrefab, westRoot.transform, "West_Cliff_Banana_01", new Vector3(-59.0f, 0.7f, 58.0f), 1.35f);
                InstantiateVolumetricTree(bananaPalmPrefab, westRoot.transform, "West_Cliff_Banana_02", new Vector3(-61.0f, 1.2f, 38.0f), 1.40f);
            }

            // -------------------------------------------------------------
            // B. EAST FLANK MOUNTAIN RANGE (Dãy Núi Đông Bao Quanh Đầm Vịt & Đồng Lúa)
            // Spanning X: 55 to 115, Z: 20 to 92
            // -------------------------------------------------------------
            var eastRoot = new GameObject("MountainRange_East_Flank");
            eastRoot.transform.parent = mountainRoot.transform;

            // Organic smooth hill meshes
            CreateOrganicHillMesh(eastRoot.transform, "Hill_East_RiceFlank", new Vector3(80.0f, UNIFORM_GROUND_Y, 58.0f), 48.0f, 68.0f, 5.8f, matGrass);
            CreateOrganicHillMesh(eastRoot.transform, "Hill_East_VillageFlank", new Vector3(76.0f, UNIFORM_GROUND_Y, 20.0f), 44.0f, 42.0f, 4.8f, matGrass);

            // Tier 1 & 2: 3D Rock Platforms (including Duck Pond rocky basin)
            InstantiateRock(rockPlatformPrefab, eastRoot.transform, "East_Platform_Pond_01", new Vector3(56.0f, 0.5f, 74.0f), new Vector3(3.2f, 2.0f, 2.8f), new Vector3(0f, -40f, 0f), matRock);
            InstantiateRock(rockPlatformPrefab, eastRoot.transform, "East_Platform_Pond_02", new Vector3(58.0f, 0.7f, 64.0f), new Vector3(3.0f, 2.2f, 2.6f), new Vector3(0f, 15f, 0f), matRock);
            InstantiateRock(rockPlatformPrefab, eastRoot.transform, "East_Platform_03", new Vector3(62.0f, 1.4f, 52.0f), new Vector3(3.4f, 2.6f, 3.0f), new Vector3(0f, -25f, 0f), matRock);
            InstantiateRock(rockPlatformPrefab, eastRoot.transform, "East_Platform_04", new Vector3(64.0f, 1.8f, 40.0f), new Vector3(3.6f, 2.8f, 3.2f), new Vector3(0f, 35f, 0f), matRock);
            InstantiateRock(rockPlatformPrefab, eastRoot.transform, "East_Platform_05", new Vector3(70.0f, 3.0f, 70.0f), new Vector3(4.0f, 3.6f, 3.8f), new Vector3(0f, 50f, 0f), matRock);
            InstantiateRock(rockPlatformPrefab, eastRoot.transform, "East_Platform_06", new Vector3(72.0f, 3.4f, 56.0f), new Vector3(4.2f, 4.0f, 3.8f), new Vector3(0f, -55f, 0f), matRock);
            InstantiateRock(rockPlatformPrefab, eastRoot.transform, "East_Platform_07", new Vector3(71.0f, 3.2f, 42.0f), new Vector3(3.8f, 3.4f, 3.4f), new Vector3(0f, 20f, 0f), matRock);
            InstantiateRock(rockPlatformPrefab, eastRoot.transform, "East_Platform_08", new Vector3(67.0f, 2.4f, 28.0f), new Vector3(3.4f, 2.6f, 3.2f), new Vector3(0f, -10f, 0f), matRock);

            // Tier 3: Towering 3D Rock Crags / Monoliths
            InstantiateRock(rockCragPrefab, eastRoot.transform, "East_Crag_01", new Vector3(68.0f, 2.8f, 74.0f), new Vector3(7.2f, 10.2f, 6.6f), new Vector3(0f, -35f, 0f), matRock);
            InstantiateRock(rockCragPrefab, eastRoot.transform, "East_Crag_02", new Vector3(75.0f, 4.1f, 64.0f), new Vector3(8.8f, 13.2f, 8.2f), new Vector3(0f, 50f, 0f), matRock);
            InstantiateRock(rockCragPrefab, eastRoot.transform, "East_Crag_03", new Vector3(69.0f, 3.3f, 54.0f), new Vector3(7.8f, 11.2f, 7.2f), new Vector3(0f, -80f, 0f), matRock);
            InstantiateRock(rockCragPrefab, eastRoot.transform, "East_Crag_04", new Vector3(76.0f, 4.4f, 44.0f), new Vector3(9.2f, 13.8f, 8.8f), new Vector3(0f, 20f, 0f), matRock);
            InstantiateRock(rockCragPrefab, eastRoot.transform, "East_Crag_05", new Vector3(72.0f, 3.5f, 34.0f), new Vector3(8.2f, 11.8f, 7.8f), new Vector3(0f, -115f, 0f), matRock);
            InstantiateRock(rockCragPrefab, eastRoot.transform, "East_Crag_HighPeak", new Vector3(85.0f, 5.6f, 58.0f), new Vector3(11.5f, 17.5f, 10.5f), new Vector3(0f, -25f, 0f), matRock);

            // 3D Boulders along footing
            InstantiateRock(rockLargeBoulder, eastRoot.transform, "East_Boulder_01", new Vector3(54.0f, UNIFORM_GROUND_Y + 0.3f, 68.0f), new Vector3(2.4f, 1.8f, 2.2f), new Vector3(5f, -40f, 10f));
            InstantiateRock(rockMediumBoulder, eastRoot.transform, "East_Boulder_02", new Vector3(56.5f, UNIFORM_GROUND_Y + 0.2f, 48.0f), new Vector3(2.0f, 1.6f, 2.0f), new Vector3(15f, 60f, -5f));
            InstantiateRock(rockLargeBoulder, eastRoot.transform, "East_Boulder_03", new Vector3(62.0f, UNIFORM_GROUND_Y + 0.4f, 35.0f), new Vector3(2.6f, 2.0f, 2.4f), new Vector3(-10f, 25f, 0f));

            // Coastal headland rocks extending East mountain down behind the harbor (capping the East border naturally)
            InstantiateRock(rockPlatformPrefab, eastRoot.transform, "East_Coast_Platform_01", new Vector3(70.0f, 0.8f, 12.0f), new Vector3(3.5f, 2.6f, 3.2f), new Vector3(0f, -30f, 0f));
            InstantiateRock(rockPlatformPrefab, eastRoot.transform, "East_Coast_Platform_02", new Vector3(72.0f, 0.6f, -2.0f), new Vector3(3.8f, 2.8f, 3.4f), new Vector3(0f, 15f, 0f));
            InstantiateRock(rockPlatformPrefab, eastRoot.transform, "East_Coast_Headland_01", new Vector3(74.0f, 0.5f, -9.0f), new Vector3(3.5f, 2.5f, 3.2f), new Vector3(0f, -25f, 0f));
            InstantiateRock(rockCragPrefab, eastRoot.transform, "East_Coast_Crag_01", new Vector3(73.0f, 2.0f, 8.0f), new Vector3(7.0f, 10.0f, 6.5f), new Vector3(0f, -45f, 0f));
            InstantiateRock(rockCragPrefab, eastRoot.transform, "East_Coast_Crag_02", new Vector3(74.0f, 1.8f, -4.0f), new Vector3(6.5f, 9.5f, 6.0f), new Vector3(0f, 20f, 0f));

            // Crowning Vegetation on East Ridge
            if (heroBanyanPrefab != null)
            {
                InstantiateVolumetricTree(heroBanyanPrefab, eastRoot.transform, "East_Ridge_Banyan_01", new Vector3(78.0f, 5.2f, 68.0f), 1.60f);
                InstantiateVolumetricTree(heroBanyanPrefab, eastRoot.transform, "East_Ridge_Banyan_02", new Vector3(79.0f, 4.8f, 40.0f), 1.50f);
            }
            if (coconutPalmPrefab != null)
            {
                InstantiateVolumetricTree(coconutPalmPrefab, eastRoot.transform, "East_Cliff_Palm_01", new Vector3(60.0f, 1.8f, 72.0f), 1.25f);
                InstantiateVolumetricTree(coconutPalmPrefab, eastRoot.transform, "East_Cliff_Palm_02", new Vector3(64.0f, 2.4f, 52.0f), 1.20f);
                InstantiateVolumetricTree(coconutPalmPrefab, eastRoot.transform, "East_Cliff_Palm_03", new Vector3(70.0f, 3.8f, 56.0f), 1.30f);
            }
            if (bananaPalmPrefab != null)
            {
                InstantiateVolumetricTree(bananaPalmPrefab, eastRoot.transform, "East_Cliff_Banana_01", new Vector3(55.0f, 0.7f, 60.0f), 1.35f);
                InstantiateVolumetricTree(bananaPalmPrefab, eastRoot.transform, "East_Cliff_Banana_02", new Vector3(59.0f, 1.2f, 42.0f), 1.40f);
            }

            // -------------------------------------------------------------
            // C. NORTHERN BACKDROP MOUNTAIN PASS (Đèo Núi Phía Bắc Khung Cổng Làng)
            // Continuous rolling mountain ridge behind the paddies with a central pass for the Village Gate road
            // -------------------------------------------------------------
            var northRoot = new GameObject("MountainRange_North_Backdrop");
            northRoot.transform.parent = mountainRoot.transform;

            CreateOrganicHillMesh(northRoot.transform, "Hill_North_West", new Vector3(-50.0f, UNIFORM_GROUND_Y, 92.0f), 78.0f, 24.0f, 5.2f, matGrass);
            CreateOrganicHillMesh(northRoot.transform, "Hill_North_East", new Vector3(45.0f, UNIFORM_GROUND_Y, 92.0f), 86.0f, 24.0f, 5.2f, matGrass);

            // Framing rock monoliths and platforms flanking the Village Gate pass
            InstantiateRock(rockPlatformPrefab, northRoot.transform, "North_Gate_Pass_West", new Vector3(-16.0f, 1.2f, 80.0f), new Vector3(3.0f, 2.4f, 2.8f), new Vector3(0f, 35f, 0f), matRock);
            InstantiateRock(rockPlatformPrefab, northRoot.transform, "North_Gate_Pass_East", new Vector3(3.0f, 1.2f, 80.0f), new Vector3(3.0f, 2.4f, 2.8f), new Vector3(0f, -35f, 0f), matRock);
            InstantiateRock(rockCragPrefab, northRoot.transform, "North_Gate_Pass_Crag_West", new Vector3(-20.0f, 2.5f, 85.0f), new Vector3(6.5f, 9.5f, 6.0f), new Vector3(0f, 20f, 0f), matRock);
            InstantiateRock(rockCragPrefab, northRoot.transform, "North_Gate_Pass_Crag_East", new Vector3(7.0f, 2.5f, 85.0f), new Vector3(6.5f, 9.5f, 6.0f), new Vector3(0f, -20f, 0f), matRock);
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

                    // Organic bell dome smoothly meeting Y = 0 on all 4 boundaries
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
                    int row1 = z * (segmentsX + 1) + x;
                    int row2 = (z + 1) * (segmentsX + 1) + x;

                    triangles[triIndex++] = row1;
                    triangles[triIndex++] = row2;
                    triangles[triIndex++] = row1 + 1;

                    triangles[triIndex++] = row1 + 1;
                    triangles[triIndex++] = row2;
                    triangles[triIndex++] = row2 + 1;
                }
            }

            var mesh = new Mesh();
            mesh.name = $"{name}_Mesh";
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var hillObj = new GameObject(name);
            hillObj.transform.parent = parent;
            hillObj.transform.position = new Vector3(center.x, groundY, center.z);
            hillObj.transform.rotation = Quaternion.identity;
            hillObj.transform.localScale = Vector3.one;

            var mf = hillObj.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            var mr = hillObj.AddComponent<MeshRenderer>();
            if (mat != null) mr.sharedMaterial = mat;

            var mc = hillObj.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;

            return hillObj;
        }

        // =========================================================================
        // 5. Turquoise Ocean & Underwater Coral Reefs (Pristine clean beach sand)
        // =========================================================================
        private static void BuildTurquoiseOceanAndReefs(Material matWater, Material matRock)
        {
            var waterRoot = GetOrCreateRoot("CaMau_1954_Village/Environment/WaterSystem_3D");

            // Vast Ocean Surface Quad: Extends from shoreline Z=-12m down to far horizon Z=-280m, width 600m
            var ocean = GameObject.CreatePrimitive(PrimitiveType.Quad);
            ocean.name = "Ocean_Turquoise_Surface";
            ocean.transform.parent = waterRoot.transform;
            ocean.transform.position = new Vector3(0.0f, -0.58f, -145.0f);
            ocean.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            ocean.transform.localScale = new Vector3(600.0f, 275.0f, 1f);
            ocean.GetComponent<MeshRenderer>().sharedMaterial = matWater;

            // Submerged Rocks & Reefs in deep water (Gently recessed under water for turquoise depth)
            float[,] reefCoords = new float[,]
            {
                { -65f, -1.8f, -28f, 2.8f },
                { -48f, -1.6f, -32f, 3.2f },
                { -35f, -1.5f, -24f, 2.4f },
                { -15f, -1.5f, -30f, 3.6f },
                {   0f, -1.4f, -26f, 2.9f },
                {  18f, -1.6f, -32f, 3.5f },
                {  36f, -1.5f, -28f, 2.7f },
                {  50f, -1.7f, -34f, 3.8f },
                { -55f, -1.6f, -42f, 4.0f },
                { -10f, -1.8f, -44f, 4.5f },
                {  25f, -1.7f, -42f, 4.2f },
                {  70f, -1.6f, -38f, 3.5f }
            };

            for (int i = 0; i < reefCoords.GetLength(0); i++)
            {
                var rk = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                rk.name = $"Submerged_Reef_{i + 1:D2}";
                rk.transform.parent = waterRoot.transform;
                rk.transform.position = new Vector3(reefCoords[i, 0], reefCoords[i, 1], reefCoords[i, 2]);
                rk.transform.localScale = new Vector3(reefCoords[i, 3] * 3.5f, 0.45f, reefCoords[i, 3] * 3.0f);
                if (matRock != null) rk.GetComponent<MeshRenderer>().sharedMaterial = matRock;
            }
        }

        // =========================================================================
        // 6. Earthen Road Network including Central Straight Road through Rice Fields
        // =========================================================================
        private static void BuildEarthenRoadNetwork(Material matRedDirt)
        {
            var pathsRoot = GetOrCreateRoot("CaMau_1954_Village/Environment/Paths_3D_Network");
            float yPath = UNIFORM_GROUND_Y + 0.005f;

            // 1. Central Tree Courtyard Plaza
            var centralPlaza = GameObject.CreatePrimitive(PrimitiveType.Quad);
            centralPlaza.name = "Plaza_Central_TreeCourt";
            centralPlaza.transform.parent = pathsRoot.transform;
            centralPlaza.transform.position = new Vector3(-6.0f, yPath, 13.0f);
            centralPlaza.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            centralPlaza.transform.localScale = new Vector3(26.0f, 24.0f, 1f);
            centralPlaza.GetComponent<MeshRenderer>().sharedMaterial = matRedDirt;

            // 2. Main Horizontal Road running below the rice fields (Z = +36m, X: [-62, +55])
            var northRoad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            northRoad.name = "Road_North_Horizontal";
            northRoad.transform.parent = pathsRoot.transform;
            northRoad.transform.position = new Vector3(-5.0f, yPath, 36.0f);
            northRoad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            northRoad.transform.localScale = new Vector3(115.0f, 4.2f, 1f);
            northRoad.GetComponent<MeshRenderer>().sharedMaterial = matRedDirt;

            // 3. Central-North connector path (connecting Central Tree Plaza to North Road)
            var northConnector = GameObject.CreatePrimitive(PrimitiveType.Quad);
            northConnector.name = "Road_Center_To_North";
            northConnector.transform.parent = pathsRoot.transform;
            northConnector.transform.position = new Vector3(-6.5f, yPath, 25.0f);
            northConnector.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            northConnector.transform.localScale = new Vector3(4.8f, 20.0f, 1f);
            northConnector.GetComponent<MeshRenderer>().sharedMaterial = matRedDirt;

            // 4. STRAIGHT CENTRAL ROAD THROUGH RICE FIELDS (CON ĐƯỜNG THẲNG NỐI CỔNG LÀNG SANG SCENE KHÁC)
            // Running straight North from Z = 36m to Z = 82m through the rice paddies
            var straightPaddyRoad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            straightPaddyRoad.name = "Road_North_StraightPaddyAisle";
            straightPaddyRoad.transform.parent = pathsRoot.transform;
            straightPaddyRoad.transform.position = new Vector3(-7.0f, yPath + 0.002f, 58.0f);
            straightPaddyRoad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            straightPaddyRoad.transform.localScale = new Vector3(4.8f, 45.0f, 1f);
            straightPaddyRoad.GetComponent<MeshRenderer>().sharedMaterial = matRedDirt;

            // 5. West Road to House 1, House 2, House 6 & West Pier
            var westRoad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            westRoad.name = "Road_West_To_House6_And_Pier";
            westRoad.transform.parent = pathsRoot.transform;
            westRoad.transform.position = new Vector3(-32.0f, yPath, 14.0f);
            westRoad.transform.rotation = Quaternion.Euler(90f, 0f, 8f);
            westRoad.transform.localScale = new Vector3(32.0f, 4.8f, 1f);
            westRoad.GetComponent<MeshRenderer>().sharedMaterial = matRedDirt;

            // 6. West Pier Approach Path
            var westPierPath = GameObject.CreatePrimitive(PrimitiveType.Quad);
            westPierPath.name = "Road_West_Pier_Approach";
            westPierPath.transform.parent = pathsRoot.transform;
            westPierPath.transform.position = new Vector3(-24.0f, 0.35f, -2.0f);
            westPierPath.transform.rotation = Quaternion.Euler(90f, 0f, -22f);
            westPierPath.transform.localScale = new Vector3(4.2f, 18.0f, 1f);
            westPierPath.GetComponent<MeshRenderer>().sharedMaterial = matRedDirt;

            // 7. East Road passing between House 4 & House 7 towards East Harbor
            var eastRoad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            eastRoad.name = "Road_East_Village_Street";
            eastRoad.transform.parent = pathsRoot.transform;
            eastRoad.transform.position = new Vector3(22.0f, yPath, 15.0f);
            eastRoad.transform.rotation = Quaternion.Euler(90f, 0f, -6f);
            eastRoad.transform.localScale = new Vector3(34.0f, 4.8f, 1f);
            eastRoad.GetComponent<MeshRenderer>().sharedMaterial = matRedDirt;

            // 8. East Harbor Approach Path
            var eastHarborPath = GameObject.CreatePrimitive(PrimitiveType.Quad);
            eastHarborPath.name = "Road_East_Harbor_Approach";
            eastHarborPath.transform.parent = pathsRoot.transform;
            eastHarborPath.transform.position = new Vector3(46.0f, 0.35f, -1.0f);
            eastHarborPath.transform.rotation = Quaternion.Euler(90f, 0f, 28f);
            eastHarborPath.transform.localScale = new Vector3(4.5f, 20.0f, 1f);
            eastHarborPath.GetComponent<MeshRenderer>().sharedMaterial = matRedDirt;

            // 9. Beach Promenade Path
            var beachPromenade = GameObject.CreatePrimitive(PrimitiveType.Quad);
            beachPromenade.name = "Road_Beach_Promenade";
            beachPromenade.transform.parent = pathsRoot.transform;
            beachPromenade.transform.position = new Vector3(0.0f, 0.42f, -8.0f);
            beachPromenade.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            beachPromenade.transform.localScale = new Vector3(90.0f, 3.8f, 1f);
            beachPromenade.GetComponent<MeshRenderer>().sharedMaterial = matRedDirt;
        }

        // =========================================================================
        // 7. Build the 10 Thatched Cottages
        // =========================================================================
        private static void Build10ThatchedCottages(Material matThatched)
        {
            var housesRoot = GetOrCreateRoot("CaMau_1954_Village/Environment/Village_Houses_Network");
            var fbxCottage = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Optimized/SM_NhaTranh_BlenderOptimized.fbx");
            if (fbxCottage == null) return;

            const float MESH_BOTTOM_OFFSET = 0.413f;

            var cottages = new (string name, Vector2 posXZ, float scale, float rotY)[]
            {
                // Upper Row (North of Village, below rice fields):
                ("House_01_NW_Corner",         new Vector2(-52.0f, 26.0f), 3.8f,  15.0f),
                ("House_02_North_MidLeft",     new Vector2(-28.0f, 28.0f), 4.2f,  -5.0f),
                ("House_03_North_Center",      new Vector2( -8.0f, 29.0f), 3.6f,   0.0f),
                ("House_04_North_LargeMaster", new Vector2( 24.0f, 26.0f), 5.0f,  -8.0f),
                ("House_05_NE_Orchard",        new Vector2( 52.0f, 24.0f), 3.8f,  20.0f),

                // Lower Row & Central Village:
                ("House_06_West_Beachside",    new Vector2(-42.0f,  6.0f), 4.5f,  12.0f),
                ("House_07_Center_Right",      new Vector2( 20.0f,  4.0f), 4.0f, -12.0f),
                ("House_08_SE_Coastal",        new Vector2( 54.0f,  6.0f), 4.4f, -40.0f),
                ("House_09_Harbor_StiltHut",   new Vector2( 60.0f,-14.0f), 3.2f,  85.0f),
                ("House_10_Far_NW_Storage",    new Vector2(-68.0f, 32.0f), 3.0f,  25.0f)
            };

            foreach (var c in cottages)
            {
                var house = (GameObject)PrefabUtility.InstantiatePrefab(fbxCottage, housesRoot.transform);
                house.name = c.name;

                float yPos = UNIFORM_GROUND_Y + (MESH_BOTTOM_OFFSET * c.scale);
                house.transform.position = new Vector3(c.posXZ.x, yPos, c.posXZ.y);
                house.transform.rotation = Quaternion.Euler(0f, c.rotY, 0f);
                house.transform.localScale = Vector3.one * c.scale;

                if (matThatched != null)
                {
                    var renderers = house.GetComponentsInChildren<MeshRenderer>();
                    foreach (var r in renderers) r.sharedMaterial = matThatched;
                }

                var col = house.GetComponent<BoxCollider>();
                if (col == null) col = house.AddComponent<BoxCollider>();
                col.center = new Vector3(0f, -0.05f, 0f);
                col.size = new Vector3(0.85f, 0.70f, 0.70f);
            }

            // House 7 kitchen shed
            {
                var shed = (GameObject)PrefabUtility.InstantiatePrefab(fbxCottage, housesRoot.transform);
                shed.name = "House_07_KitchenShed";
                float shedScale = 2.6f;
                float yShed = UNIFORM_GROUND_Y + (MESH_BOTTOM_OFFSET * shedScale);
                shed.transform.position = new Vector3(27.0f, yShed, 3.5f);
                shed.transform.rotation = Quaternion.Euler(0f, -10f, 0f);
                shed.transform.localScale = Vector3.one * shedScale;

                if (matThatched != null)
                {
                    foreach (var r in shed.GetComponentsInChildren<MeshRenderer>()) r.sharedMaterial = matThatched;
                }

                var col = shed.GetComponent<BoxCollider>();
                if (col == null) col = shed.AddComponent<BoxCollider>();
                col.center = new Vector3(0f, -0.05f, 0f);
                col.size = new Vector3(0.85f, 0.70f, 0.70f);
            }
        }

        // =========================================================================
        // 8. Central Sacred Shade Tree & Circular Courtyard
        // =========================================================================
        private static void BuildCentralSacredTreeAndPlaza(Material matRock)
        {
            var vegRoot = GetOrCreateRoot("CaMau_1954_Village/Environment/Vegetation_3D");
            Vector3 centerTreePos = new Vector3(-6.0f, UNIFORM_GROUND_Y, 13.0f);

            var largeTreePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_01_LargeCanopy.prefab");
            if (largeTreePrefab == null) largeTreePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Trees/PF_Branch_Cluster_Tree.prefab");
            if (largeTreePrefab != null)
            {
                var treeGroup = new GameObject("Tree_Hero_CentralSacredBanyan");
                treeGroup.transform.parent = vegRoot.transform;
                treeGroup.transform.position = centerTreePos;
                treeGroup.transform.rotation = Quaternion.identity;

                var card1 = (GameObject)PrefabUtility.InstantiatePrefab(largeTreePrefab, treeGroup.transform);
                card1.name = "Canopy_Card_0";
                card1.transform.localPosition = Vector3.zero;
                card1.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                card1.transform.localScale = Vector3.one * 1.6f;
                StripAnimationComponents(card1);

                var card2 = (GameObject)PrefabUtility.InstantiatePrefab(largeTreePrefab, treeGroup.transform);
                card2.name = "Canopy_Card_90";
                card2.transform.localPosition = Vector3.zero;
                card2.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                card2.transform.localScale = Vector3.one * 1.55f;
                StripAnimationComponents(card2);
            }

            var stoneRingRoot = new GameObject("Tree_Central_StoneRing");
            stoneRingRoot.transform.parent = vegRoot.transform;
            stoneRingRoot.transform.position = centerTreePos;

            int stoneCount = 18;
            float radius = 3.6f;
            for (int i = 0; i < stoneCount; i++)
            {
                float angle = i * Mathf.PI * 2f / stoneCount;
                Vector3 stonePos = centerTreePos + new Vector3(Mathf.Cos(angle) * radius, 0.15f, Mathf.Sin(angle) * radius);

                var stone = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stone.name = $"Stone_Block_{i + 1:D2}";
                stone.transform.parent = stoneRingRoot.transform;
                stone.transform.position = stonePos;
                stone.transform.rotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg + 90f, 0f);
                stone.transform.localScale = new Vector3(1.1f, 0.35f, 0.55f);
                if (matRock != null) stone.GetComponent<MeshRenderer>().sharedMaterial = matRock;
            }

            var matWood = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/GhibliModular/M_Ghibli_PierPlank.mat");
            var bench = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bench.name = "Bench_Under_CentralTree";
            bench.transform.parent = stoneRingRoot.transform;
            bench.transform.position = centerTreePos + new Vector3(4.8f, 0.25f, -0.5f);
            bench.transform.localScale = new Vector3(1.8f, 0.5f, 0.9f);
            if (matWood != null) bench.GetComponent<MeshRenderer>().sharedMaterial = matWood;
        }

        // =========================================================================
        // 9. Coastal Piers, Harbor Complex & Traditional Boats
        // =========================================================================
        private static void BuildCoastalPiersAndBoats(Material matPierWood, Material matThatched)
        {
            var pierRoot = GetOrCreateRoot("CaMau_1954_Village/Village_Areas/08_Ben_Xuong");
            var waterRoot = GetOrCreateRoot("CaMau_1954_Village/Environment/WaterSystem_3D");

            var westJetty = GameObject.CreatePrimitive(PrimitiveType.Cube);
            westJetty.name = "Pier_West_Jetty_Deck";
            westJetty.transform.parent = pierRoot.transform;
            westJetty.transform.position = new Vector3(-20.0f, 0.12f, -16.5f);
            westJetty.transform.localScale = new Vector3(3.4f, 0.22f, 17.0f);
            if (matPierWood != null) westJetty.GetComponent<MeshRenderer>().sharedMaterial = matPierWood;

            float[] westPilingsZ = new float[] { -9.5f, -14.0f, -18.5f, -23.0f };
            foreach (var pz in westPilingsZ)
            {
                BuildPiling(pierRoot.transform, $"WestPiling_L_{pz:F0}", new Vector3(-21.3f, -0.6f, pz), matPierWood);
                BuildPiling(pierRoot.transform, $"WestPiling_R_{pz:F0}", new Vector3(-18.7f, -0.6f, pz), matPierWood);
            }

            var westFishPlatform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            westFishPlatform.name = "Pier_West_FishDrying_Platform";
            westFishPlatform.transform.parent = pierRoot.transform;
            westFishPlatform.transform.position = new Vector3(-30.0f, 0.15f, -11.5f);
            westFishPlatform.transform.localScale = new Vector3(10.0f, 0.22f, 4.2f);
            if (matPierWood != null) westFishPlatform.GetComponent<MeshRenderer>().sharedMaterial = matPierWood;

            var awningFrame = new GameObject("Pier_West_AwningFrame");
            awningFrame.transform.parent = pierRoot.transform;
            awningFrame.transform.position = new Vector3(-30.0f, 1.8f, -11.5f);
            var awningRoof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            awningRoof.name = "Awning_Roof";
            awningRoof.transform.parent = awningFrame.transform;
            awningRoof.transform.localPosition = Vector3.zero;
            awningRoof.transform.localScale = new Vector3(9.5f, 0.08f, 3.8f);
            if (matThatched != null) awningRoof.GetComponent<MeshRenderer>().sharedMaterial = matThatched;

            var eastDeck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            eastDeck.name = "Pier_East_Harbor_MainDeck";
            eastDeck.transform.parent = pierRoot.transform;
            eastDeck.transform.position = new Vector3(60.0f, 0.15f, -18.0f);
            eastDeck.transform.localScale = new Vector3(15.0f, 0.24f, 11.0f);
            if (matPierWood != null) eastDeck.GetComponent<MeshRenderer>().sharedMaterial = matPierWood;

            var eastArm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            eastArm.name = "Pier_East_WalkwayArm";
            eastArm.transform.parent = pierRoot.transform;
            eastArm.transform.position = new Vector3(56.0f, 0.12f, -27.5f);
            eastArm.transform.localScale = new Vector3(3.2f, 0.22f, 10.0f);
            if (matPierWood != null) eastArm.GetComponent<MeshRenderer>().sharedMaterial = matPierWood;

            float[,] eastPilings = new float[,]
            {
                { 53f, -14f }, { 60f, -14f }, { 67f, -14f },
                { 53f, -22f }, { 60f, -22f }, { 67f, -22f },
                { 54.5f, -28f }, { 57.5f, -28f }, { 54.5f, -32f }, { 57.5f, -32f }
            };
            for (int i = 0; i < eastPilings.GetLength(0); i++)
            {
                BuildPiling(pierRoot.transform, $"EastPiling_{i + 1:D2}", new Vector3(eastPilings[i, 0], -0.6f, eastPilings[i, 1]), matPierWood);
            }

            var fbxCottage = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Optimized/SM_NhaTranh_BlenderOptimized.fbx");
            if (fbxCottage != null)
            {
                var hut1 = (GameObject)PrefabUtility.InstantiatePrefab(fbxCottage, pierRoot.transform);
                hut1.name = "Harbor_Shelter_Hut_01";
                hut1.transform.position = new Vector3(56.0f, 0.25f, -15.0f);
                hut1.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                hut1.transform.localScale = Vector3.one * 2.2f;
                if (matThatched != null)
                {
                    foreach (var r in hut1.GetComponentsInChildren<MeshRenderer>()) r.sharedMaterial = matThatched;
                }

                var hut2 = (GameObject)PrefabUtility.InstantiatePrefab(fbxCottage, pierRoot.transform);
                hut2.name = "Harbor_Shelter_Hut_02";
                hut2.transform.position = new Vector3(64.0f, 0.25f, -15.0f);
                hut2.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                hut2.transform.localScale = Vector3.one * 2.0f;
                if (matThatched != null)
                {
                    foreach (var r in hut2.GetComponentsInChildren<MeshRenderer>()) r.sharedMaterial = matThatched;
                }
            }

            var boatsRoot = new GameObject("Harbor_Boats_And_Sampans");
            boatsRoot.transform.parent = waterRoot.transform;

            var sampanOpen = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Boats/PF_Sampan_Open_Hat_Floating.prefab");
            var sampanCovered = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Boats/PF_Sampan_Covered_Floating.prefab");
            if (sampanOpen == null) sampanOpen = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Vehicles/PF_StylizedBoat.prefab");
            if (sampanCovered == null) sampanCovered = sampanOpen;

            InstantiateBoat(sampanOpen, boatsRoot.transform, "Boat_LeftBeach_01", new Vector3(-58.0f, -0.15f, -16.0f), 1.15f, 15.0f);
            InstantiateBoat(sampanCovered, boatsRoot.transform, "Boat_LeftBeach_02", new Vector3(-51.5f, -0.22f, -17.5f), 1.20f, 22.0f);
            InstantiateBoat(sampanOpen, boatsRoot.transform, "Boat_LeftBeach_03", new Vector3(-44.0f, -0.32f, -19.5f), 1.10f, 32.0f);
            InstantiateBoat(sampanOpen, boatsRoot.transform, "Boat_LeftBeach_Skiff", new Vector3(-36.0f, -0.45f, -20.5f), 0.85f, 40.0f);
            InstantiateBoat(sampanCovered, boatsRoot.transform, "Boat_CenterBay_Moored", new Vector3(2.5f, -0.55f, -23.0f), 1.25f, -18.0f);
            InstantiateBoat(sampanOpen, boatsRoot.transform, "Boat_RightHarbor_Approach", new Vector3(32.0f, -0.55f, -26.5f), 1.30f, -35.0f);
            InstantiateBoat(sampanCovered, boatsRoot.transform, "Boat_RightHarbor_Docked", new Vector3(68.5f, -0.55f, -22.0f), 1.20f, 95.0f);

            // STRICT RULE: ZERO CRATES / ZERO BARRELS / NO "THÙNG" PROPS ALLOWED
            var basketPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Props/PF_Prop_Bamboo_Basket.prefab");
            var netPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Props/PF_Prop_Fishing_Net.prefab");

            if (basketPrefab != null)
            {
                InstantiateProp(basketPrefab, pierRoot.transform, "Basket_01", new Vector3(57.0f, 0.35f, -20.5f), 1.15f);
                InstantiateProp(basketPrefab, pierRoot.transform, "Basket_WestPier", new Vector3(-31.5f, 0.35f, -12.0f), 1.1f);
            }
            if (netPrefab != null)
            {
                var net1 = (GameObject)PrefabUtility.InstantiatePrefab(netPrefab, pierRoot.transform);
                net1.name = "DryingNet_WestPlatform";
                net1.transform.position = new Vector3(-28.0f, 0.8f, -11.5f);
                net1.transform.localScale = Vector3.one * 1.35f;

                var net2 = (GameObject)PrefabUtility.InstantiatePrefab(netPrefab, pierRoot.transform);
                net2.name = "DryingNet_EastHarbor";
                net2.transform.position = new Vector3(57.5f, 0.7f, -18.5f);
                net2.transform.localScale = Vector3.one * 1.25f;
            }
        }

        private static void BuildPiling(Transform parent, string name, Vector3 pos, Material mat)
        {
            var p = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            p.name = name;
            p.transform.parent = parent;
            p.transform.position = pos;
            p.transform.localScale = new Vector3(0.24f, 1.2f, 0.24f);
            if (mat != null) p.GetComponent<MeshRenderer>().sharedMaterial = mat;
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

        // =========================================================================
        // 10. Rice Paddies with Straight Parallel Rows & Old Sway Animation
        // Separated by the Central Straight Road Corridor (X in [-9.4, -4.6])
        // =========================================================================
        private static void BuildRicePaddiesAndDuckPond(Material matRiceGreen, Material matRiceGolden, Material matWater, Material matGrass, Material matBamboo, Material matThatched)
        {
            var terrainRoot = GetOrCreateRoot("CaMau_1954_Village/Environment/Terrain_3D");
            var riceRowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Rice/PF_Rice_RowStrip_4m.prefab");
            var lilyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Boats/PF_WaterLily_Cluster.prefab");

            float yPlot = UNIFORM_GROUND_Y + 0.008f;

            // 6 Plots cleanly divided by the central straight road (X = -7.0m, width 4.8m):
            // Left plots end at X <= -11.0m; Right plots start at X >= -3.0m
            var paddyPlots = new (string name, Vector3 pos, Vector2 size, int matType)[]
            {
                // Left Quadrants (North & South):
                ("Paddy_Plot_01_Golden", new Vector3(-43.0f, yPlot, 62.0f), new Vector2(20.0f, 12.0f), 0),
                ("Paddy_Plot_02_Golden", new Vector3(-21.0f, yPlot, 62.0f), new Vector2(19.0f, 12.0f), 0),
                ("Paddy_Plot_05_Green",  new Vector3(-32.0f, yPlot, 48.0f), new Vector2(38.0f, 11.0f), 1),

                // Right Quadrants (North & South):
                ("Paddy_Plot_03_Golden", new Vector3(  6.0f, yPlot, 62.0f), new Vector2(17.0f, 12.0f), 0),
                ("Paddy_Plot_04_Green",  new Vector3( 25.0f, yPlot, 62.0f), new Vector2(19.0f, 12.0f), 1),
                ("Paddy_Plot_06_Green",  new Vector3( 14.5f, yPlot, 48.0f), new Vector2(34.0f, 11.0f), 1)
            };

            foreach (var plot in paddyPlots)
            {
                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = plot.name;
                quad.transform.parent = terrainRoot.transform;
                quad.transform.position = plot.pos;
                quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                quad.transform.localScale = new Vector3(plot.size.x, plot.size.y, 1f);
                quad.GetComponent<MeshRenderer>().sharedMaterial = (plot.matType == 0) ? matRiceGolden : matRiceGreen;

                BuildPaddyDikes(terrainRoot.transform, plot.name, plot.pos, plot.size, matGrass);

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
                            var row = (GameObject)PrefabUtility.InstantiatePrefab(riceRowPrefab, terrainRoot.transform);
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
                        }
                        rowIdx++;
                    }
                }
            }

            BuildScarecrow(terrainRoot.transform, "Scarecrow_01", new Vector3(-35.0f, UNIFORM_GROUND_Y, 62.0f), matBamboo, matThatched);
            BuildScarecrow(terrainRoot.transform, "Scarecrow_02", new Vector3( 15.0f, UNIFORM_GROUND_Y, 62.0f), matBamboo, matThatched);

            var pondRoot = new GameObject("Duck_Pond_TopRight");
            pondRoot.transform.parent = terrainRoot.transform;
            pondRoot.transform.position = new Vector3(60.0f, UNIFORM_GROUND_Y - 0.05f, 60.0f);

            var pondWater = GameObject.CreatePrimitive(PrimitiveType.Quad);
            pondWater.name = "Pond_Water_Surface";
            pondWater.transform.parent = pondRoot.transform;
            pondWater.transform.localPosition = Vector3.zero;
            pondWater.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            pondWater.transform.localScale = new Vector3(18.0f, 16.0f, 1f);
            if (matWater != null) pondWater.GetComponent<MeshRenderer>().sharedMaterial = matWater;

            if (lilyPrefab != null)
            {
                var l1 = (GameObject)PrefabUtility.InstantiatePrefab(lilyPrefab, pondRoot.transform);
                l1.name = "WaterLily_Pond_01";
                l1.transform.position = new Vector3(57.0f, UNIFORM_GROUND_Y - 0.02f, 58.0f);
                l1.transform.localScale = Vector3.one * 1.2f;

                var l2 = (GameObject)PrefabUtility.InstantiatePrefab(lilyPrefab, pondRoot.transform);
                l2.name = "WaterLily_Pond_02";
                l2.transform.position = new Vector3(63.0f, UNIFORM_GROUND_Y - 0.02f, 62.0f);
                l2.transform.localScale = Vector3.one * 1.4f;
            }
        }

        private static void BuildPaddyDikes(Transform parent, string baseName, Vector3 pos, Vector2 size, Material matGrass)
        {
            float halfX = size.x * 0.5f;
            float halfZ = size.y * 0.5f;
            float dikeY = UNIFORM_GROUND_Y + 0.04f;

            var dN = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dN.name = $"{baseName}_DikeN";
            dN.transform.parent = parent;
            dN.transform.position = new Vector3(pos.x, dikeY, pos.z + halfZ);
            dN.transform.localScale = new Vector3(size.x + 0.6f, 0.08f, 0.6f);
            if (matGrass != null) dN.GetComponent<MeshRenderer>().sharedMaterial = matGrass;

            var dS = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dS.name = $"{baseName}_DikeS";
            dS.transform.parent = parent;
            dS.transform.position = new Vector3(pos.x, dikeY, pos.z - halfZ);
            dS.transform.localScale = new Vector3(size.x + 0.6f, 0.08f, 0.6f);
            if (matGrass != null) dS.GetComponent<MeshRenderer>().sharedMaterial = matGrass;

            var dE = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dE.name = $"{baseName}_DikeE";
            dE.transform.parent = parent;
            dE.transform.position = new Vector3(pos.x + halfX, dikeY, pos.z);
            dE.transform.localScale = new Vector3(0.6f, 0.08f, size.y);
            if (matGrass != null) dE.GetComponent<MeshRenderer>().sharedMaterial = matGrass;

            var dW = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dW.name = $"{baseName}_DikeW";
            dW.transform.parent = parent;
            dW.transform.position = new Vector3(pos.x - halfX, dikeY, pos.z);
            dW.transform.localScale = new Vector3(0.6f, 0.08f, size.y);
            if (matGrass != null) dW.GetComponent<MeshRenderer>().sharedMaterial = matGrass;
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
            post.transform.localScale = new Vector3(0.08f, 0.85f, 0.08f);
            if (matBamboo != null) post.GetComponent<MeshRenderer>().sharedMaterial = matBamboo;

            var cross = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cross.name = "CrossArms";
            cross.transform.parent = sc.transform;
            cross.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            cross.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            cross.transform.localScale = new Vector3(0.06f, 0.6f, 0.06f);
            if (matBamboo != null) cross.GetComponent<MeshRenderer>().sharedMaterial = matBamboo;

            var hat = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hat.name = "NonLa_Hat";
            hat.transform.parent = sc.transform;
            hat.transform.localPosition = new Vector3(0f, 1.72f, 0f);
            hat.transform.localScale = new Vector3(0.45f, 0.04f, 0.45f);
            if (matThatched != null) hat.GetComponent<MeshRenderer>().sharedMaterial = matThatched;
        }

        // =========================================================================
        // 11. Village Gate (Cổng Làng) & Scene Transition Trigger at North Exit
        // =========================================================================
        private static void BuildVillageGateAndSceneTransition()
        {
            var envRoot = GetOrCreateRoot("CaMau_1954_Village/Environment");
            var gateFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Optimized/SM_Cong_lang_BlenderOptimized.fbx");
            Vector3 gatePos = new Vector3(-7.0f, UNIFORM_GROUND_Y, 73.5f);

            // A. Traditional Village Gate Structure
            var gateGroup = new GameObject("Gate_Village_North");
            gateGroup.transform.parent = envRoot.transform;
            gateGroup.transform.position = gatePos;
            gateGroup.transform.rotation = Quaternion.identity;

            if (gateFbx != null)
            {
                var gateModel = (GameObject)PrefabUtility.InstantiatePrefab(gateFbx, gateGroup.transform);
                gateModel.name = "Model_CongLang";
                // Stand 100% upright facing South across the straight road
                gateModel.transform.localPosition = Vector3.zero;
                gateModel.transform.localRotation = Quaternion.Euler(89.98f, 0f, 0f);
                gateModel.transform.localScale = Vector3.one * 5.5f;

                // Calibrate grounding so gate pillars sit firmly on UNIFORM_GROUND_Y
                var r = gateModel.GetComponentInChildren<Renderer>();
                if (r != null)
                {
                    float diff = UNIFORM_GROUND_Y - r.bounds.min.y;
                    gateModel.transform.position += new Vector3(0f, diff, 0f);
                }
            }

            // Left Post Collider
            var colLeft = gateGroup.AddComponent<BoxCollider>();
            colLeft.center = new Vector3(-1.85f, 2.0f, 0f);
            colLeft.size = new Vector3(1.1f, 4.0f, 1.4f);

            // Right Post Collider
            var colRight = gateGroup.AddComponent<BoxCollider>();
            colRight.center = new Vector3(1.85f, 2.0f, 0f);
            colRight.size = new Vector3(1.1f, 4.0f, 1.4f);

            // Center Archway is completely open for walking through!

            // B. Scene Transition Trigger Point (Connecting to the next scene)
            // Placed just past the Village Gate at Z = 77.5m
            var triggerObj = new GameObject("VillageExit_NorthGate_Transition");
            triggerObj.transform.parent = envRoot.transform;
            triggerObj.transform.position = new Vector3(-7.0f, UNIFORM_GROUND_Y, 77.5f);
            triggerObj.transform.rotation = Quaternion.identity;

            var triggerCol = triggerObj.AddComponent<BoxCollider>();
            triggerCol.isTrigger = true;
            triggerCol.center = new Vector3(0f, 1.8f, 0f);
            triggerCol.size = new Vector3(5.0f, 3.6f, 2.5f);

            var transition = triggerObj.AddComponent<SceneTransitionPoint>();
            transition.TargetSceneName = ""; // Configurable in Inspector for any destination scene
            transition.TransitionPrompt = "Rời Làng Cà Mau (1954) - Tiến sang Cảnh tiếp theo...";
        }

        // =========================================================================
        // 12. Tropical Foliage, Living Gardens & Village Props
        // =========================================================================
        private static void BuildTropicalFoliageAndProps(Material matBamboo, Material matRedDirt, Material matWood)
        {
            var vegRoot = GetOrCreateRoot("CaMau_1954_Village/Environment/Vegetation_3D");
            var fencesRoot = GetOrCreateRoot("CaMau_1954_Village/Environment/Village_Fences_Network");

            var coconutPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_06_Coconut.prefab");
            var bananaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_05_Banana.prefab");
            var fruitTreePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_02_Medium.prefab");
            var canopyTreePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_01_LargeCanopy.prefab");
            var bambooGrovePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_07_BambooGrove.prefab");
            var gourdPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_Concept_Assets/GourdTrellis/PF_LanaVN_GourdTrellis_Main.prefab");
            if (gourdPrefab == null) gourdPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/GourdTrellis/PF_LanaVN_GourdTrellis_Main.prefab");

            // A. Coastal Coconut Palms (100% Upright, Multi-angle 3D canopy)
            if (coconutPrefab != null)
            {
                InstantiateVolumetricTree(coconutPrefab, vegRoot.transform, "Coconut_Beach_01", new Vector3(-65.0f, UNIFORM_GROUND_Y - 0.2f, -8.0f), 1.35f);
                InstantiateVolumetricTree(coconutPrefab, vegRoot.transform, "Coconut_Beach_02", new Vector3(-35.0f, UNIFORM_GROUND_Y - 0.2f, -6.5f), 1.25f);
                InstantiateVolumetricTree(coconutPrefab, vegRoot.transform, "Coconut_Beach_03", new Vector3( -8.0f, UNIFORM_GROUND_Y - 0.1f, -5.0f), 1.40f);
                InstantiateVolumetricTree(coconutPrefab, vegRoot.transform, "Coconut_Beach_04", new Vector3( 14.0f, UNIFORM_GROUND_Y - 0.2f, -6.0f), 1.30f);
                InstantiateVolumetricTree(coconutPrefab, vegRoot.transform, "Coconut_Beach_05", new Vector3( 38.0f, UNIFORM_GROUND_Y - 0.1f, -5.5f), 1.45f);

                InstantiateVolumetricTree(coconutPrefab, vegRoot.transform, "Coconut_Rice_01", new Vector3(-55.0f, UNIFORM_GROUND_Y, 52.0f), 1.20f);
                InstantiateVolumetricTree(coconutPrefab, vegRoot.transform, "Coconut_Rice_02", new Vector3(-16.0f, UNIFORM_GROUND_Y, 74.0f), 1.30f);
                InstantiateVolumetricTree(coconutPrefab, vegRoot.transform, "Coconut_Rice_03", new Vector3( 35.0f, UNIFORM_GROUND_Y, 52.0f), 1.25f);
                InstantiateVolumetricTree(coconutPrefab, vegRoot.transform, "Coconut_PondSide", new Vector3( 52.0f, UNIFORM_GROUND_Y, 66.0f), 1.35f);
            }

            // B. Banana Groves around Thatched Cottages
            if (bananaPrefab != null)
            {
                InstantiateVolumetricTree(bananaPrefab, vegRoot.transform, "Banana_House01", new Vector3(-58.0f, UNIFORM_GROUND_Y, 22.0f), 1.20f);
                InstantiateVolumetricTree(bananaPrefab, vegRoot.transform, "Banana_House02", new Vector3(-22.0f, UNIFORM_GROUND_Y, 28.0f), 1.15f);
                InstantiateVolumetricTree(bananaPrefab, vegRoot.transform, "Banana_House04", new Vector3( 32.0f, UNIFORM_GROUND_Y, 28.0f), 1.25f);
                InstantiateVolumetricTree(bananaPrefab, vegRoot.transform, "Banana_House06", new Vector3(-48.0f, UNIFORM_GROUND_Y,  9.0f), 1.30f);
                InstantiateVolumetricTree(bananaPrefab, vegRoot.transform, "Banana_House07", new Vector3( 26.0f, UNIFORM_GROUND_Y,  8.0f), 1.10f);
            }

            // C. Fruit Trees (Mango & Orange)
            if (fruitTreePrefab != null)
            {
                InstantiateVolumetricTree(fruitTreePrefab, vegRoot.transform, "FruitTree_House05_01", new Vector3(46.0f, UNIFORM_GROUND_Y, 26.0f), 1.25f);
                InstantiateVolumetricTree(fruitTreePrefab, vegRoot.transform, "FruitTree_House05_02", new Vector3(58.0f, UNIFORM_GROUND_Y, 22.0f), 1.35f);
                InstantiateVolumetricTree(fruitTreePrefab, vegRoot.transform, "FruitTree_House08_Side", new Vector3(48.0f, UNIFORM_GROUND_Y, 10.0f), 1.20f);
                InstantiateVolumetricTree(fruitTreePrefab, vegRoot.transform, "FruitTree_Village_01", new Vector3(-18.0f, UNIFORM_GROUND_Y,  8.0f), 1.30f);
            }

            // D. Bamboo Groves behind Village
            if (bambooGrovePrefab != null)
            {
                InstantiateVolumetricTree(bambooGrovePrefab, vegRoot.transform, "Bamboo_House02_Back", new Vector3(-35.0f, UNIFORM_GROUND_Y, 32.0f), 1.25f);
                InstantiateVolumetricTree(bambooGrovePrefab, vegRoot.transform, "Bamboo_House04_Back", new Vector3( 15.0f, UNIFORM_GROUND_Y, 32.0f), 1.30f);
                InstantiateVolumetricTree(bambooGrovePrefab, vegRoot.transform, "Bamboo_House05_Back", new Vector3( 60.0f, UNIFORM_GROUND_Y, 30.0f), 1.20f);
            }

            // E. Dense Canopy Trees on Left and Right Cliffs (Framing)
            if (canopyTreePrefab != null)
            {
                InstantiateVolumetricTree(canopyTreePrefab, vegRoot.transform, "CliffTree_West_01", new Vector3(-72.0f, 3.5f, 55.0f), 1.50f);
                InstantiateVolumetricTree(canopyTreePrefab, vegRoot.transform, "CliffTree_West_02", new Vector3(-68.0f, 2.5f, 10.0f), 1.40f);
                InstantiateVolumetricTree(canopyTreePrefab, vegRoot.transform, "CliffTree_East_01", new Vector3( 72.0f, 3.5f, 55.0f), 1.45f);
                InstantiateVolumetricTree(canopyTreePrefab, vegRoot.transform, "CliffTree_East_02", new Vector3( 75.0f, 3.0f, 12.0f), 1.50f);
                InstantiateVolumetricTree(canopyTreePrefab, vegRoot.transform, "NorthTree_01", new Vector3(-45.0f, 2.2f, 80.0f), 1.60f);
                InstantiateVolumetricTree(canopyTreePrefab, vegRoot.transform, "NorthTree_02", new Vector3( 25.0f, 2.2f, 80.0f), 1.55f);
            }

            // F. Bamboo Fences for House Yards
            BuildStraightFence(fencesRoot.transform, "Fence_House01_Front", new Vector3(-58.0f, UNIFORM_GROUND_Y, 20.0f), new Vector3(-46.0f, UNIFORM_GROUND_Y, 20.0f), matBamboo);
            BuildStraightFence(fencesRoot.transform, "Fence_House04_Yard", new Vector3(14.0f, UNIFORM_GROUND_Y, 20.0f), new Vector3(34.0f, UNIFORM_GROUND_Y, 20.0f), matBamboo);
            BuildStraightFence(fencesRoot.transform, "Fence_House06_Front", new Vector3(-48.0f, UNIFORM_GROUND_Y,  0.5f), new Vector3(-35.0f, UNIFORM_GROUND_Y,  0.5f), matBamboo);
            BuildStraightFence(fencesRoot.transform, "Fence_House07_Yard", new Vector3(14.0f, UNIFORM_GROUND_Y, -1.0f), new Vector3(26.0f, UNIFORM_GROUND_Y, -1.0f), matBamboo);
            BuildStraightFence(fencesRoot.transform, "Fence_House08_Yard", new Vector3(48.0f, UNIFORM_GROUND_Y,  0.5f), new Vector3(62.0f, UNIFORM_GROUND_Y,  0.5f), matBamboo);

            // G. Gourd Trellis
            if (gourdPrefab != null)
            {
                var tr = (GameObject)PrefabUtility.InstantiatePrefab(gourdPrefab, fencesRoot.transform);
                tr.name = "GourdTrellis_House04";
                tr.transform.position = new Vector3(34.0f, UNIFORM_GROUND_Y, 24.0f);
                tr.transform.rotation = Quaternion.Euler(0f, -15f, 0f);
                tr.transform.localScale = Vector3.one * 1.0f;
            }

            BuildClothesline(fencesRoot.transform, "Clothesline_House04", new Vector3(18.0f, UNIFORM_GROUND_Y, 31.0f), new Vector3(28.0f, UNIFORM_GROUND_Y, 31.0f), matBamboo);

            // H. Ceramic Water Jars
            BuildCeramicWaterJar(fencesRoot.transform, "LuNuoc_House02_1", new Vector3(-24.5f, UNIFORM_GROUND_Y, 26.5f));
            BuildCeramicWaterJar(fencesRoot.transform, "LuNuoc_House02_2", new Vector3(-23.8f, UNIFORM_GROUND_Y, 26.8f));
            BuildCeramicWaterJar(fencesRoot.transform, "LuNuoc_House04_1", new Vector3( 19.5f, UNIFORM_GROUND_Y, 24.5f));
            BuildCeramicWaterJar(fencesRoot.transform, "LuNuoc_House04_2", new Vector3( 18.8f, UNIFORM_GROUND_Y, 24.8f));
            BuildCeramicWaterJar(fencesRoot.transform, "LuNuoc_House06_1", new Vector3(-37.5f, UNIFORM_GROUND_Y,  4.5f));
            BuildCeramicWaterJar(fencesRoot.transform, "LuNuoc_House06_2", new Vector3(-36.8f, UNIFORM_GROUND_Y,  4.8f));
            BuildCeramicWaterJar(fencesRoot.transform, "LuNuoc_House07_1", new Vector3( 15.5f, UNIFORM_GROUND_Y,  2.5f));

            // I. Buffalo & Resting Dog
            var buffaloFBX = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Buffalo.fbx");
            if (buffaloFBX != null)
            {
                var bf = (GameObject)PrefabUtility.InstantiatePrefab(buffaloFBX, fencesRoot.transform);
                bf.name = "Buffalo_Village_Orchard";
                bf.transform.position = new Vector3(42.0f, UNIFORM_GROUND_Y, 18.0f);
                bf.transform.rotation = Quaternion.Euler(0f, -65f, 0f);
                bf.transform.localScale = Vector3.one * 1.2f;
            }

            var dogFBX = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Model_Dog.fbx");
            if (dogFBX != null)
            {
                var dg = (GameObject)PrefabUtility.InstantiatePrefab(dogFBX, fencesRoot.transform);
                dg.name = "Dog_House06_Porch";
                dg.transform.position = new Vector3(-36.5f, UNIFORM_GROUND_Y, 8.5f);
                dg.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
                dg.transform.localScale = Vector3.one * 1.1f;
            }
        }

        private static void InstantiateVolumetricTree(GameObject prefab, Transform parent, string name, Vector3 pos, float scale)
        {
            var treeRoot = new GameObject(name);
            treeRoot.transform.parent = parent;
            treeRoot.transform.position = pos;
            treeRoot.transform.rotation = Quaternion.identity;

            var card1 = (GameObject)PrefabUtility.InstantiatePrefab(prefab, treeRoot.transform);
            card1.name = "Card_Front";
            card1.transform.localPosition = Vector3.zero;
            card1.transform.localRotation = Quaternion.Euler(0f, Random.Range(-25f, 25f), 0f);
            card1.transform.localScale = Vector3.one * scale;
            StripAnimationComponents(card1);

            var card2 = (GameObject)PrefabUtility.InstantiatePrefab(prefab, treeRoot.transform);
            card2.name = "Card_Cross";
            card2.transform.localPosition = Vector3.zero;
            card2.transform.localRotation = Quaternion.Euler(0f, card1.transform.localEulerAngles.y + 90f, 0f);
            card2.transform.localScale = Vector3.one * (scale * 0.95f);
            StripAnimationComponents(card2);
        }

        private static void StripAnimationComponents(GameObject go)
        {
            var monos = go.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var m in monos)
            {
                if (m == null) continue;
                string tName = m.GetType().Name;
                if (tName.Contains("Sway") || tName.Contains("Wind") || tName.Contains("Foliage"))
                {
                    Object.DestroyImmediate(m);
                }
            }
        }

        private static void BuildCeramicWaterJar(Transform parent, string name, Vector3 pos)
        {
            var jar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            jar.name = name;
            jar.transform.parent = parent;
            jar.transform.position = pos + Vector3.up * 0.45f;
            jar.transform.localScale = new Vector3(0.65f, 0.45f, 0.65f);

            var matJar = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_CeramicJar_NamBo.mat");
            if (matJar == null)
            {
                matJar = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                matJar.color = new Color(0.52f, 0.32f, 0.18f, 1.0f);
                matJar.SetFloat("_Smoothness", 0.45f);
                AssetDatabase.CreateAsset(matJar, "Assets/Materials/M_CeramicJar_NamBo.mat");
            }
            jar.GetComponent<MeshRenderer>().sharedMaterial = matJar;
        }

        private static void BuildStraightFence(Transform parent, string name, Vector3 start, Vector3 end, Material mat)
        {
            var fence = new GameObject(name);
            fence.transform.parent = parent;

            float dist = Vector3.Distance(start, end);
            int count = Mathf.Max(2, Mathf.RoundToInt(dist / 0.8f));
            Vector3 dir = (end - start).normalized;

            for (int i = 0; i <= count; i++)
            {
                Vector3 p = Vector3.Lerp(start, end, (float)i / count);
                var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                post.name = $"Picket_{i}";
                post.transform.parent = fence.transform;
                post.transform.position = new Vector3(p.x, p.y + 0.45f, p.z);
                post.transform.localScale = new Vector3(0.06f, 0.45f, 0.06f);
                if (mat != null) post.GetComponent<MeshRenderer>().sharedMaterial = mat;
            }

            var rail = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rail.name = "CrossRail";
            rail.transform.parent = fence.transform;
            rail.transform.position = (start + end) * 0.5f + Vector3.up * 0.55f;
            rail.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir);
            rail.transform.localScale = new Vector3(0.04f, dist * 0.5f, 0.04f);
            if (mat != null) rail.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private static void BuildClothesline(Transform parent, string name, Vector3 start, Vector3 end, Material matPost)
        {
            var cl = new GameObject(name);
            cl.transform.parent = parent;

            var p1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            p1.name = "Post1";
            p1.transform.parent = cl.transform;
            p1.transform.position = start + Vector3.up * 0.75f;
            p1.transform.localScale = new Vector3(0.08f, 0.75f, 0.08f);
            if (matPost != null) p1.GetComponent<MeshRenderer>().sharedMaterial = matPost;

            var p2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            p2.name = "Post2";
            p2.transform.parent = cl.transform;
            p2.transform.position = end + Vector3.up * 0.75f;
            p2.transform.localScale = new Vector3(0.08f, 0.75f, 0.08f);
            if (matPost != null) p2.GetComponent<MeshRenderer>().sharedMaterial = matPost;

            float dist = Vector3.Distance(start, end);
            Vector3 dir = (end - start).normalized;
            var line = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            line.name = "Rope";
            line.transform.parent = cl.transform;
            line.transform.position = (start + end) * 0.5f + Vector3.up * 1.4f;
            line.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir);
            line.transform.localScale = new Vector3(0.02f, dist * 0.5f, 0.02f);
            if (matPost != null) line.GetComponent<MeshRenderer>().sharedMaterial = matPost;

            var matBrown = GetOrCreateMaterial("Assets/Materials/M_CaMau_ClothBrown.mat", new Color(0.42f, 0.30f, 0.20f, 1.0f), "", Vector2.one);
            var matScarf = GetOrCreateMaterial("Assets/Materials/M_CaMau_KhanRan.mat", new Color(0.70f, 0.25f, 0.20f, 1.0f), "", Vector2.one);
            var matIndigo = GetOrCreateMaterial("Assets/Materials/M_CaMau_ClothIndigo.mat", new Color(0.24f, 0.35f, 0.48f, 1.0f), "", Vector2.one);

            Material[] fabrics = new Material[] { matBrown, matScarf, matIndigo, matBrown };
            for (int i = 0; i < 4; i++)
            {
                float t = (i + 1f) / 5f;
                Vector3 clothPos = Vector3.Lerp(start, end, t) + Vector3.up * 1.05f;
                var cloth = GameObject.CreatePrimitive(PrimitiveType.Quad);
                cloth.name = $"Cloth_{i + 1}";
                cloth.transform.parent = cl.transform;
                cloth.transform.position = clothPos;
                cloth.transform.localScale = new Vector3(1.2f, 0.75f, 1f);
                cloth.GetComponent<MeshRenderer>().sharedMaterial = fabrics[i % fabrics.Length];
            }
        }

        // =========================================================================
        // 13. Spawn Points & NPCs (Grounded firmly, zero sinking, precise foot calibration)
        // =========================================================================
        private static void PositionSpawnPointsAndNPCs()
        {
            var playerSpawn = GameObject.Find("PlayerSpawn");
            if (playerSpawn == null) playerSpawn = new GameObject("PlayerSpawn");
            playerSpawn.transform.position = new Vector3(-6.0f, UNIFORM_GROUND_Y + 0.10f, 8.0f);
            playerSpawn.transform.rotation = Quaternion.identity;

            var player = GameObject.Find("CaMau_1954_Village/Player");
            if (player == null) player = GameObject.Find("Player");
            if (player != null) player.transform.position = playerSpawn.transform.position;

            // Calibrate An_Player CharacterController center & height to align precisely with mesh feet
            var anPlayer = GameObject.Find("An_Player");
            if (anPlayer != null)
            {
                var cc = anPlayer.GetComponent<CharacterController>();
                if (cc != null)
                {
                    cc.center = new Vector3(0f, 0.48f, 0f);
                    cc.height = 1.16f;
                    cc.radius = 0.25f;
                    cc.stepOffset = 0.30f;
                    EditorUtility.SetDirty(cc);
                }
            }

            // Explicitly position all known NPCs to authentic, lively village locations
            SetNpcPosition("NPC_Mother_An", new Vector3(20.0f, UNIFORM_GROUND_Y + 0.10f, 22.0f));
            SetNpcPosition("NPC_Villager_Waving_Mother", new Vector3(26.0f, UNIFORM_GROUND_Y + 0.10f, 1.2f));
            SetNpcPosition("NPC_Elderly_VillageChief", new Vector3(-4.0f, UNIFORM_GROUND_Y + 0.10f, 10.0f));
            SetNpcPosition("NPC_Elderly_UncleBa", new Vector3(13.5f, UNIFORM_GROUND_Y + 0.10f, 4.5f));
            SetNpcPosition("NPC_Villager_Farewell_Elder", new Vector3(27.0f, UNIFORM_GROUND_Y + 0.10f, 3.8f));
            SetNpcPosition("NPC_Villager_Carrier_Tu", new Vector3(4.0f, UNIFORM_GROUND_Y + 0.10f, 0.5f));
            SetNpcPosition("NPC_Baby_Toddler", new Vector3(-11.0f, UNIFORM_GROUND_Y + 0.10f, 1.5f));
            SetNpcPosition("NPC_Female_WaterCarrier", new Vector3(14.5f, UNIFORM_GROUND_Y + 0.10f, 3.5f));
            SetNpcPosition("NPC_Villager_Gardener", new Vector3(48.0f, UNIFORM_GROUND_Y + 0.10f, 22.0f));
            SetNpcPosition("NPC_Child_Playing_Boy", new Vector3(-8.0f, UNIFORM_GROUND_Y + 0.10f, 11.0f));
            SetNpcPosition("NPC_Child_Playing_Girl", new Vector3(-10.0f, UNIFORM_GROUND_Y + 0.10f, 10.0f));
            SetNpcPosition("NPC_Ferryman_UncleBay", new Vector3(56.0f, 0.25f, -22.0f));

            // Dynamically ground Player and EVERY NPC in the scene with exact foot calibration
            var allSceneObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var go in allSceneObjects)
            {
                if (go == null) continue;
                if (go.name.StartsWith("NPC_") || go.name.Contains("Player"))
                {
                    float targetY = UNIFORM_GROUND_Y;
                    if (go.name.Contains("Ferryman"))
                    {
                        targetY = 0.20f; // On pier / boat deck
                    }
                    GroundCharacterPrecisely(go, targetY);
                }
            }

            if (player != null && playerSpawn != null)
            {
                playerSpawn.transform.position = player.transform.position;
            }
        }

        private static void SetNpcPosition(string name, Vector3 pos)
        {
            var go = GameObject.Find(name);
            if (go != null)
            {
                go.transform.position = pos;
                EditorUtility.SetDirty(go);
            }
        }

        private static void GroundCharacterPrecisely(GameObject character, float targetGroundY)
        {
            if (character == null) return;

            var renderers = character.GetComponentsInChildren<Renderer>();
            if (renderers == null || renderers.Length == 0) return;

            Bounds combinedBounds = new Bounds();
            bool hasValidBounds = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (!renderers[i].enabled || renderers[i] is ParticleSystemRenderer) continue;
                if (!hasValidBounds)
                {
                    combinedBounds = renderers[i].bounds;
                    hasValidBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(renderers[i].bounds);
                }
            }

            if (!hasValidBounds) return;

            // Align lowest vertex of feet exactly with targetGroundY + 0.005m
            float currentLowestY = combinedBounds.min.y;
            float desiredLowestY = targetGroundY + 0.005f;
            float deltaY = desiredLowestY - currentLowestY;

            character.transform.position += new Vector3(0f, deltaY, 0f);

            // Also synchronize CapsuleCollider if present so collider bottom aligns with feet
            var cap = character.GetComponent<CapsuleCollider>();
            if (cap != null)
            {
                float localBottom = (desiredLowestY - character.transform.position.y);
                cap.center = new Vector3(cap.center.x, localBottom + (cap.height * 0.5f), cap.center.z);
                EditorUtility.SetDirty(cap);
            }

            EditorUtility.SetDirty(character);
        }

        // =========================================================================
        // 14. Concise & Authentic Southern Vietnamese Game Dialogues
        // =========================================================================
        private static void UpdateDialogueTriggersConcise()
        {
            var conciseDialogues = new Dictionary<string, (string title, string prompt, string action)>
            {
                { "Trigger_01_Mother", ("Má", "[E] Trò chuyện", "Má: 'Về sớm ăn cơm nghen con!'") },
                { "Trigger_02_DogVang", ("Chó Vàng", "[E] Xoa đầu", "Gâu! Chú chó mừng rỡ vẫy đuôi.") },
                { "Trigger_03_ChickenCoop", ("Ổ Trứng", "[E] Nhặt trứng", "Nhặt được 3 quả trứng gà ấm.") },
                { "Trigger_04_SoilPouch", ("Túi Đất", "[E] Mở xem", "Nắm đất phù sa Cà Mau ấm áp.") },
                { "Trigger_05_VillageWell", ("Giếng Làng", "[E] Múc nước", "Nước giếng mát rượi, ngọt lịm.") },
                { "Trigger_06_ElderBa", ("Ông Ba", "[E] Chào hỏi", "Ông Ba: 'Đi đứng cẩn thận nghe con!'") },
                { "Trigger_07_RoadVillagerTu", ("Bác Tư", "[E] Trò chuyện", "Bác Tư: 'Rau tươi má gửi nè con.'") },
                { "Trigger_08_VillageChief", ("Bác Trưởng Xóm", "[E] Hỏi thăm", "Bác Hai: 'Năm nay xóm mình trúng lúa!'") },
                { "Trigger_09_VillageKids", ("Lũ Bạn", "[E] Bắn bi", "Tụi nhỏ: 'An ơi, vô bắn bi nè!'") },
                { "Trigger_10_GourdTrellis", ("Giàn Bầu", "[E] Ngắm", "Trái bầu non xanh mướt đu đưa.") },
                { "Trigger_11_BabyToddler", ("Bé Út", "[E] Chơi đùa", "Bé Út: Cười toe toét vẫy tay.") },
                { "Trigger_12_Gardener", ("Chú Năm", "[E] Trò chuyện", "Chú Năm: 'Bầu ngọt lắm, hái một trái đi con.'") },
                { "Trigger_13_WaterBuffalo", ("Trâu Mộng", "[E] Xoa đầu", "Nghé ọ! Chú trâu nhai cỏ hiền lành.") },
                { "Trigger_14_FarmerRiceField", ("Bác Ba Nông Dân", "[E] Đưa cơm", "Bác Ba: 'Cảm ơn con, cơm má nấu ngon quá!'") },
                { "Trigger_15_RiverBench", ("Ghế Gỗ", "[E] Hóng mát", "Gió sông thổi mát rượi.") },
                { "Trigger_16_Ferryman", ("Bác Lái Đò", "[E] Hỏi đò", "Bác Ba: 'Lên mau, chuẩn bị nhổ sào rồi!'") }
            };

            var allTriggers = Object.FindObjectsByType<HoaVaMau.Test3D.InteractableObject3D>(FindObjectsSortMode.None);
            foreach (var trigger in allTriggers)
            {
                if (trigger == null) continue;
                if (conciseDialogues.TryGetValue(trigger.gameObject.name, out var data))
                {
                    var so = new SerializedObject(trigger);
                    so.FindProperty("objectTitle").stringValue = data.title;
                    so.FindProperty("promptMessage").stringValue = data.prompt;
                    so.FindProperty("actionMessage").stringValue = data.action;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(trigger);
                }
            }
        }

        // =========================================================================
        // 14. Lighting & Atmosphere
        // =========================================================================
        private static void SetupLightingAndAtmosphere()
        {
            var sun = RenderSettings.sun;
            if (sun != null)
            {
                sun.color = new Color(1.0f, 0.96f, 0.88f, 1.0f);
                sun.intensity = 1.25f;
                sun.transform.rotation = Quaternion.Euler(52.0f, -38.0f, 0.0f);
                sun.shadows = LightShadows.Soft;
            }

            // Authentic daylight 360 cubemap skybox (sky_22_2k with clouds & soft sun)
            var skyMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Backdrop_Sky/M_Skybox_Village_CubemapDaylight.mat");
            if (skyMat != null)
            {
                RenderSettings.skybox = skyMat;
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.85f, 0.92f, 1.0f, 1.0f);
            RenderSettings.ambientEquatorColor = new Color(0.72f, 0.82f, 0.65f, 1.0f);
            RenderSettings.ambientGroundColor = new Color(0.55f, 0.45f, 0.35f, 1.0f);
            RenderSettings.ambientIntensity = 1.15f;

            RenderSettings.fog = false;
            DynamicGI.UpdateEnvironment();
        }

        // =========================================================================
        // 15. Camera Systems (Overview with Sky & Gameplay)
        // =========================================================================
        [MenuItem("Tools/Hoa Do/Camera: Overview (Reference Match with Sky)")]
        public static void SetOverviewCamera()
        {
            var cam = Camera.main;
            if (cam != null)
            {
                // Elevated Overhead Perspective framing Village, Sea, Rice Fields, Village Gate, AND the Sky:
                // Camera at (-4.5, 40, -58), pitched 28 deg down looking north for expansive seamless sky framing
                cam.transform.position = new Vector3(-4.5f, 40.0f, -58.0f);
                cam.transform.rotation = Quaternion.Euler(28.0f, 0.0f, 0.0f);
                cam.fieldOfView = 58.0f;
                cam.nearClipPlane = 0.3f;
                cam.farClipPlane = 1200.0f;
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                Debug.Log("[RebuildMap] Camera set to Overview with Sky Framing!");
            }
        }

        [MenuItem("Tools/Hoa Do/Camera: Gameplay (Player View)")]
        public static void SetGameplayCamera()
        {
            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(-6.0f, 7.5f, 3.0f);
                cam.transform.rotation = Quaternion.Euler(32.0f, 0.0f, 0.0f);
                cam.fieldOfView = 60.0f;
                cam.nearClipPlane = 0.3f;
                cam.farClipPlane = 450.0f;
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                Debug.Log("[RebuildMap] Camera set to Gameplay (Player View)!");
            }
        }

        private static void SetupCameras()
        {
            SetOverviewCamera();
        }

        private static GameObject GetOrCreateRoot(string path)
        {
            var existing = GameObject.Find(path);
            if (existing != null)
            {
                existing.SetActive(true);
                return existing;
            }

            string[] parts = path.Split('/');
            GameObject current = null;
            string currentPath = "";

            for (int i = 0; i < parts.Length; i++)
            {
                currentPath = (i == 0) ? parts[i] : $"{currentPath}/{parts[i]}";
                var go = GameObject.Find(currentPath);
                if (go == null)
                {
                    go = new GameObject(parts[i]);
                    if (current != null) go.transform.parent = current.transform;
                }
                go.SetActive(true);
                current = go;
            }
            return current;
        }
    }
}
