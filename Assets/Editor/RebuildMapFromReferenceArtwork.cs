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
            var matRock = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/LanaOverhaul/M_Lana_RockUnderbelly.mat");
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

            // 13. Position Spawn Points & NPCs
            PositionSpawnPointsAndNPCs();

            // 14. Setup Lighting & Atmosphere
            SetupLightingAndAtmosphere();

            // 15. Setup Cameras (Overview with Sky + Gameplay)
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
                "Sky_Surround_Cylinder_360", "Backdrop_SkyNorth_Panoramic",
                "Crate_01", "Crate_02", "Crate_WestPier", "Barrel_01", "Barrel_02",
                "Terrain_West_Headland", "Terrain_West_BeachSlope",
                "Terrain_East_Headland", "Terrain_East_BeachSlope"
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
                if (n.Contains("crate") || n.Contains("barrel") || n.Contains("thung") || n.Contains("cargo"))
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

            // 1. Grand 360-degree Sky Enclosure Material (5K Hand-painted sky with soft clouds & sun)
            var matSkyDome = GetOrCreateUnlitMaterial(
                "Assets/Materials/Environment/Backdrop_Sky/M_Sky_Backdrop_Surround.mat",
                "Assets/Textures/Sky/Sky_Cloud_Krita_5120x2160.png",
                false, 2000);

            // 2. 360-degree Inward Cylindrical Sky Enclosure (Radius 340m, Height 260m)
            var cylObj = new GameObject("Sky_Surround_Dome_360");
            cylObj.transform.parent = bgRoot.transform;
            cylObj.transform.position = new Vector3(0f, 20f, 0f);
            cylObj.transform.rotation = Quaternion.identity;
            cylObj.transform.localScale = Vector3.one;

            var mf = cylObj.AddComponent<MeshFilter>();
            mf.sharedMesh = CreateInwardCylinderMesh(340f, 260f, 20f, 64, 3);

            var mr = cylObj.AddComponent<MeshRenderer>();
            mr.sharedMaterial = matSkyDome;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            // 3. Northern Horizon - Lush Rolling Green Hills & Misty Tropical Forest (Directly behind Rice Fields & Village Gate)
            var matMistyHills = GetOrCreateUnlitMaterial(
                "Assets/Materials/Environment/Backdrop_Sky/M_Backdrop_MistyHills.mat",
                "Assets/Textures/Backgrounds_Lana/BG_Lana_MainSkyHills_Official.png",
                true, 2990);

            var hillsNorth = GameObject.CreatePrimitive(PrimitiveType.Quad);
            hillsNorth.name = "Backdrop_Northern_MistyHills_Mid";
            hillsNorth.transform.parent = bgRoot.transform;
            hillsNorth.transform.position = new Vector3(0.0f, 22.0f, 105.0f);
            hillsNorth.transform.rotation = Quaternion.Euler(0f, 0f, 0f); // Facing South towards village & camera
            hillsNorth.transform.localScale = new Vector3(320.0f, 44.0f, 1f);
            hillsNorth.GetComponent<MeshRenderer>().sharedMaterial = matMistyHills;
            var cNorth = hillsNorth.GetComponent<Collider>();
            if (cNorth != null) Object.DestroyImmediate(cNorth);

            // 4. Northern Far Distance - Towering Misty Mountain Peaks
            var matFarMountains = GetOrCreateUnlitMaterial(
                "Assets/Materials/Environment/Backdrop_Sky/M_Backdrop_FarMountains.mat",
                "Assets/Textures/Backgrounds_Lana/T_Lana_GrandUnified_Background.png",
                true, 2980);

            var mountainsNorth = GameObject.CreatePrimitive(PrimitiveType.Quad);
            mountainsNorth.name = "Backdrop_Northern_FarMountains";
            mountainsNorth.transform.parent = bgRoot.transform;
            mountainsNorth.transform.position = new Vector3(0.0f, 38.0f, 145.0f);
            mountainsNorth.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            mountainsNorth.transform.localScale = new Vector3(440.0f, 70.0f, 1f);
            mountainsNorth.GetComponent<MeshRenderer>().sharedMaterial = matFarMountains;
            var cMnt = mountainsNorth.GetComponent<Collider>();
            if (cMnt != null) Object.DestroyImmediate(cMnt);

            // 5. Southern Far Horizon - Deep Ocean Meeting Sky
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

            // 6. Physical 3D Clouds Flock (Clouds_Atmosphere_3D)
            BuildPhysical3DClouds(bgRoot.transform);
        }

        private static void BuildPhysical3DClouds(Transform parent)
        {
            var cloudsRoot = new GameObject("Clouds_Atmosphere_3D");
            cloudsRoot.transform.parent = parent;

            string[] cloudMats = new string[]
            {
                "Assets/Materials/LanaOverhaul/SpriteMats/M_Spr_T_Lana_Cloud_01_CumulusHero.mat",
                "Assets/Materials/LanaOverhaul/SpriteMats/M_Spr_T_Lana_Cloud_02_WideBank.mat",
                "Assets/Materials/LanaOverhaul/SpriteMats/M_Spr_T_Lana_Cloud_03_HighWisps.mat",
                "Assets/Materials/LanaOverhaul/SpriteMats/M_Spr_T_Lana_Cloud_04_GoldenHorizon.mat",
                "Assets/Materials/LanaOverhaul/SpriteMats/M_Spr_T_Cloud_Mid_CumulusBank01.mat",
                "Assets/Materials/LanaOverhaul/SpriteMats/M_Spr_T_Cloud_Mid_SoftBank03.mat"
            };

            // Cloud 3D billboarding data: (X, Y, Z, scaleX, scaleY, rotX, rotY, matIdx)
            float[,] cloudSpecs = new float[,]
            {
                // North Horizon / Over Rice Fields & Gate
                { -25f, 34f,  70f, 68f, 26f, 18f,   0f, 0 }, // Hero Cumulus
                {  38f, 38f,  85f, 85f, 30f, 16f,   0f, 1 }, // Wide Bank
                {   0f, 26f, 115f, 110f, 32f, 12f,   0f, 3 }, // Golden Horizon
                { -60f, 44f,  50f, 60f, 20f, 22f,   0f, 2 }, // High Wisps
                {  65f, 32f,  40f, 75f, 24f, 20f,   0f, 5 }, // Soft Bank

                // Center Sky / Above Village & Sacred Tree
                { -10f, 42f,  15f, 80f, 32f, 25f,   0f, 0 }, // Big Cumulus Over Center
                {  30f, 46f,  10f, 55f, 22f, 25f,   0f, 4 }, // East Fluff
                { -35f, 48f,   5f, 58f, 24f, 25f,   0f, 2 }, // West Fluff
                {   0f, 54f,  35f, 90f, 34f, 30f,   0f, 1 }, // Zenith Cloud Bank

                // Flanks & Coastal Sea
                { -70f, 36f, -20f, 65f, 22f, 22f,   0f, 5 }, // West Shore Wisp
                {  70f, 36f, -20f, 65f, 22f, 22f,   0f, 4 }, // East Shore Cluster
                { -95f, 35f,  95f, 80f, 28f, 15f,   0f, 1 }, // North-West Distant Bank
                {  95f, 35f,  95f, 80f, 28f, 15f,   0f, 3 }, // North-East Golden Bank

                // South Horizon / Over Sea
                { -50f, 26f, -90f, 90f, 28f, -15f, 180f, 1 }, // Sea Cloud Bank West
                {  45f, 28f,-100f, 95f, 30f, -15f, 180f, 0 }, // Sea Cloud Bank East
                {   5f, 24f,-125f, 105f, 30f, -12f, 180f, 3 }  // Sea Horizon Bank Center
            };

            for (int i = 0; i < cloudSpecs.GetLength(0); i++)
            {
                var cObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
                cObj.name = $"Cloud_3D_Atmo_{i + 1:D2}";
                cObj.transform.parent = cloudsRoot.transform;
                cObj.transform.position = new Vector3(cloudSpecs[i, 0], cloudSpecs[i, 1], cloudSpecs[i, 2]);
                cObj.transform.rotation = Quaternion.Euler(cloudSpecs[i, 5], cloudSpecs[i, 6], 0f);
                cObj.transform.localScale = new Vector3(cloudSpecs[i, 3], cloudSpecs[i, 4], 1f);

                int mIdx = (int)cloudSpecs[i, 7] % cloudMats.Length;
                var cMat = AssetDatabase.LoadAssetAtPath<Material>(cloudMats[mIdx]);
                if (cMat != null) cObj.GetComponent<MeshRenderer>().sharedMaterial = cMat;

                var col = cObj.GetComponent<Collider>();
                if (col != null) Object.DestroyImmediate(col);
            }
        }

        private static Mesh CreateInwardCylinderMesh(float radius, float height, float yCenter, int segments, int uRepeats)
        {
            Mesh mesh = new Mesh();
            mesh.name = "Inward_Sky_Cylinder_Mesh";

            int vertCount = (segments + 1) * 2;
            Vector3[] vertices = new Vector3[vertCount];
            Vector3[] normals = new Vector3[vertCount];
            Vector2[] uvs = new Vector2[vertCount];
            int[] triangles = new int[segments * 6];

            float yBottom = yCenter - height * 0.35f;
            float yTop = yCenter + height * 0.65f;

            for (int i = 0; i <= segments; i++)
            {
                float ratio = (float)i / segments;
                float angle = ratio * Mathf.PI * 2f;
                float x = Mathf.Sin(angle) * radius;
                float z = Mathf.Cos(angle) * radius;

                int idxBottom = i * 2;
                int idxTop = i * 2 + 1;

                vertices[idxBottom] = new Vector3(x, yBottom, z);
                vertices[idxTop] = new Vector3(x, yTop, z);

                normals[idxBottom] = new Vector3(-x, 0f, -z).normalized;
                normals[idxTop] = new Vector3(-x, 0f, -z).normalized;

                uvs[idxBottom] = new Vector2(ratio * uRepeats, 0f);
                uvs[idxTop] = new Vector2(ratio * uRepeats, 1f);

                if (i < segments)
                {
                    int triIdx = i * 6;
                    triangles[triIdx + 0] = idxBottom;
                    triangles[triIdx + 1] = idxTop;
                    triangles[triIdx + 2] = idxBottom + 2;

                    triangles[triIdx + 3] = idxBottom + 2;
                    triangles[triIdx + 4] = idxTop;
                    triangles[triIdx + 5] = idxTop + 2;
                }
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        // =========================================================================
        // 4. 3D Terrain Construction (Pristine clean beach, zero rocks on sand)
        // =========================================================================
        private static void Build3DTerrain(Material matGrass, Material matSand, Material matRock)
        {
            var terrainRoot = GetOrCreateRoot("CaMau_1954_Village/Environment/Terrain_3D");

            // A. Single Unified Continuous Ground Plain (Top surface exactly at Y = 0.50m, 320m wide continuous)
            var unifiedPlain = GameObject.CreatePrimitive(PrimitiveType.Cube);
            unifiedPlain.name = "Terrain_Unified_Ground_Base";
            unifiedPlain.transform.parent = terrainRoot.transform;
            unifiedPlain.transform.position = new Vector3(0.0f, UNIFORM_GROUND_Y - 0.25f, 38.0f);
            unifiedPlain.transform.localScale = new Vector3(320.0f, 0.50f, 96.0f);
            unifiedPlain.GetComponent<MeshRenderer>().sharedMaterial = matGrass;

            // B. Single Unified Continuous Sandy Shoreline Slope (Smooth transition down to sea, 320m wide continuous)
            var beachSlope = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beachSlope.name = "Terrain_Beach_Slope";
            beachSlope.transform.parent = terrainRoot.transform;
            beachSlope.transform.position = new Vector3(0.0f, -0.04f, -14.0f);
            beachSlope.transform.rotation = Quaternion.Euler(7.7f, 0f, 0f);
            beachSlope.transform.localScale = new Vector3(320.0f, 0.50f, 8.5f);
            beachSlope.GetComponent<MeshRenderer>().sharedMaterial = matSand;

            // C. Sea Bed Floor (Extending deeply under the sea)
            var seaBed = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seaBed.name = "Terrain_Sea_Bed";
            seaBed.transform.parent = terrainRoot.transform;
            seaBed.transform.position = new Vector3(0.0f, -3.2f, -140.0f);
            seaBed.transform.localScale = new Vector3(600.0f, 0.6f, 280.0f);
            seaBed.GetComponent<MeshRenderer>().sharedMaterial = matSand;

            // D. Northern Backdrop Hill (Low gentle hill behind paddies, 320m wide continuous)
            var northHill = GameObject.CreatePrimitive(PrimitiveType.Cube);
            northHill.name = "Terrain_North_Backdrop_Hill";
            northHill.transform.parent = terrainRoot.transform;
            northHill.transform.position = new Vector3(0.0f, 1.45f, 88.0f);
            northHill.transform.localScale = new Vector3(320.0f, 1.9f, 16.0f);
            northHill.GetComponent<MeshRenderer>().sharedMaterial = matGrass;

            // G. ROCK CLIFFS FLANKING THE RICE FIELDS ONLY (Mounds blend with grass, real rock outcrops on top)
            // Left (West) Flank: X ~ -65 to -78, Z ~ 42 to 74
            BuildRockyCliff(terrainRoot.transform, "Cliff_West_Rice_North", new Vector3(-68.0f, 2.5f, 66.0f), new Vector3(20.0f, 4.0f, 22.0f), matGrass);
            BuildRockyCliff(terrainRoot.transform, "Cliff_West_Rice_South", new Vector3(-64.0f, 2.0f, 48.0f), new Vector3(18.0f, 3.2f, 18.0f), matGrass);

            // Right (East) Flank: X ~ +56 to +74, Z ~ 42 to 74
            BuildRockyCliff(terrainRoot.transform, "Cliff_East_Rice_North", new Vector3(68.0f, 2.5f, 68.0f), new Vector3(20.0f, 4.0f, 22.0f), matGrass);
            BuildRockyCliff(terrainRoot.transform, "Cliff_East_Rice_South", new Vector3(66.0f, 2.0f, 48.0f), new Vector3(18.0f, 3.2f, 18.0f), matGrass);

            // H. Stone Retaining Wall along upper beach border
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

        private static void BuildRockyCliff(Transform parent, string name, Vector3 pos, Vector3 size, Material matGrass)
        {
            var cliffMound = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cliffMound.name = name;
            cliffMound.transform.parent = parent;
            cliffMound.transform.position = pos;
            cliffMound.transform.localScale = size;
            if (matGrass != null) cliffMound.GetComponent<MeshRenderer>().sharedMaterial = matGrass;

            var rockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Rocks/PF_LanaVN_Rock_01_LargeOutcrop.prefab");
            if (rockPrefab == null) rockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Rocks/PF_Rock_Cliff_Large.prefab");
            if (rockPrefab != null)
            {
                var r1 = (GameObject)PrefabUtility.InstantiatePrefab(rockPrefab, parent);
                r1.name = $"{name}_Outcrop1";
                r1.transform.position = pos + new Vector3(Random.Range(-1.5f, 1.5f), size.y * 0.4f, Random.Range(-3f, 3f));
                r1.transform.localScale = Vector3.one * 1.6f;

                var r2 = (GameObject)PrefabUtility.InstantiatePrefab(rockPrefab, parent);
                r2.name = $"{name}_Outcrop2";
                r2.transform.position = pos + new Vector3(Random.Range(-1.5f, 1.5f), size.y * 0.2f, Random.Range(-6f, 6f));
                r2.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
                r2.transform.localScale = Vector3.one * 1.3f;
            }
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
                // Blender FBX Z-up needs -90X rotation to stand upright facing South
                gateModel.transform.localPosition = new Vector3(0f, 4.85f, 0f);
                gateModel.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                gateModel.transform.localScale = Vector3.one * 5.0f;
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
        // 13. Spawn Points & NPCs (Grounded cleanly on UNIFORM_GROUND_Y)
        // =========================================================================
        private static void PositionSpawnPointsAndNPCs()
        {
            var playerSpawn = GameObject.Find("PlayerSpawn");
            if (playerSpawn == null) playerSpawn = new GameObject("PlayerSpawn");
            playerSpawn.transform.position = new Vector3(-6.0f, UNIFORM_GROUND_Y + 0.05f, 8.0f);
            playerSpawn.transform.rotation = Quaternion.identity;

            var player = GameObject.Find("CaMau_1954_Village/Player");
            if (player != null) player.transform.position = playerSpawn.transform.position;

            var mother = GameObject.Find("NPC_Mother_An");
            if (mother != null) mother.transform.position = new Vector3(20.0f, UNIFORM_GROUND_Y + 0.05f, 22.0f);

            var chief = GameObject.Find("NPC_Elderly_VillageChief");
            if (chief != null) chief.transform.position = new Vector3(-4.0f, UNIFORM_GROUND_Y + 0.05f, 10.0f);

            var ferryman = GameObject.Find("NPC_Ferryman_UncleBay");
            if (ferryman != null) ferryman.transform.position = new Vector3(56.0f, 0.25f, -22.0f);

            var gardener = GameObject.Find("NPC_Villager_Gardener");
            if (gardener != null) gardener.transform.position = new Vector3(48.0f, UNIFORM_GROUND_Y + 0.05f, 22.0f);

            var childrenBoy = GameObject.Find("NPC_Child_Playing_Boy");
            if (childrenBoy != null) childrenBoy.transform.position = new Vector3(-8.0f, UNIFORM_GROUND_Y + 0.05f, 11.0f);

            var childrenGirl = GameObject.Find("NPC_Child_Playing_Girl");
            if (childrenGirl != null) childrenGirl.transform.position = new Vector3(-10.0f, UNIFORM_GROUND_Y + 0.05f, 10.0f);
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
                // Camera at (-4.5, 48, -50), pitched 36 deg down looking north
                cam.transform.position = new Vector3(-4.5f, 48.0f, -50.0f);
                cam.transform.rotation = Quaternion.Euler(36.0f, 0.0f, 0.0f);
                cam.fieldOfView = 54.0f;
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
