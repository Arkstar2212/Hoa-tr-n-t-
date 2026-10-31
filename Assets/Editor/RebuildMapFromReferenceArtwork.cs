using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

namespace HoaDo.Editor
{
    public static class RebuildMapFromReferenceArtwork
    {
        [MenuItem("Tools/Hoa Do/Rebuild Map From Reference Artwork")]
        public static void Execute()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (!activeScene.path.Contains("CaMau_1954_Village"))
            {
                Debug.LogWarning("[RebuildMap] Opening CaMau_1954_Village.unity...");
                EditorSceneManager.OpenScene("Assets/Scenes/CaMau_1954_Village.unity");
            }

            Debug.Log("[RebuildMap] Starting complete 3D map reconstruction based on reference artwork...");

            // =========================================================================
            // 1. Synchronize & Configure Materials
            // =========================================================================

            // A. Smooth Nam Bo Earthen Road (Fine-grained warm red-brown dirt, zero striping)
            var matRedDirt = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Village_Road_NamBo.mat");
            if (matRedDirt == null)
            {
                matRedDirt = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(matRedDirt, "Assets/Materials/M_Village_Road_NamBo.mat");
            }
            var texDirtFine = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Shaders/Uber Stylized Water/Demo/Terrain/sand_01_color_2k.png");
            if (texDirtFine == null) texDirtFine = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/PainterlyGround_Seamless.png");
            matRedDirt.shader = Shader.Find("Universal Render Pipeline/Lit");
            matRedDirt.mainTexture = texDirtFine;
            matRedDirt.mainTextureScale = new Vector2(6.0f, 6.0f);
            matRedDirt.color = new Color(0.66f, 0.48f, 0.34f, 1.0f); // Authentic Nam Bo alluvial red dirt
            matRedDirt.SetFloat("_Smoothness", 0.05f);
            EditorUtility.SetDirty(matRedDirt);

            // B. Vibrant Lush Rice Crop (Emerald / chartreuse green stalks)
            var matRiceCrop = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/LanaOverhaul/M_RiceCrop_LushGreen.mat");
            if (matRiceCrop == null)
            {
                matRiceCrop = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(matRiceCrop, "Assets/Materials/LanaOverhaul/M_RiceCrop_LushGreen.mat");
            }
            var texGrass = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Grass/meadow_turf.png");
            matRiceCrop.shader = Shader.Find("Universal Render Pipeline/Lit");
            matRiceCrop.mainTexture = texGrass;
            matRiceCrop.mainTextureScale = new Vector2(10.0f, 10.0f);
            matRiceCrop.color = new Color(0.52f, 0.76f, 0.22f, 1.0f); // Brilliant chartreuse rice green
            matRiceCrop.SetFloat("_Smoothness", 0.08f);
            EditorUtility.SetDirty(matRiceCrop);

            // C. Rich Village Meadow Grass
            var matGrass = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_CaMau_Landscape_Grass.mat");
            if (matGrass != null)
            {
                matGrass.color = new Color(0.46f, 0.66f, 0.28f, 1.0f);
                if (texGrass != null) matGrass.mainTexture = texGrass;
                EditorUtility.SetDirty(matGrass);
            }

            // D. Golden Beach Sand
            var matSand = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Seabed_Sand.mat");
            if (matSand != null)
            {
                matSand.color = new Color(0.88f, 0.82f, 0.66f, 1.0f);
                if (texDirtFine != null) matSand.mainTexture = texDirtFine;
                matSand.mainTextureScale = new Vector2(8.0f, 8.0f);
                EditorUtility.SetDirty(matSand);
            }

            // E. Clear Turquoise Ocean Bay & River Water
            var matOcean = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_RiverWater_Canh1.mat");
            if (matOcean == null) matOcean = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/LanaOverhaul/M_LivingOcean_WaterSurface.mat");
            if (matOcean != null)
            {
                if (matOcean.HasProperty("_BaseColor")) matOcean.SetColor("_BaseColor", new Color(0.18f, 0.65f, 0.72f, 0.88f));
                if (matOcean.HasProperty("_Color")) matOcean.SetColor("_Color", new Color(0.18f, 0.65f, 0.72f, 0.88f));
                EditorUtility.SetDirty(matOcean);
            }

            // F. Architectural Materials (Double-sided for open stilt houses)
            var matThatched = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Architecture/M_StiltedHouse_Thatched.mat");
            var matHouseWood = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Architecture/M_StiltedHouse_Wood.mat");
            var matNhaTranh = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Nhà tranh.mat");
            if (matThatched != null) { matThatched.SetFloat("_Cull", 0f); EditorUtility.SetDirty(matThatched); }
            if (matHouseWood != null) { matHouseWood.SetFloat("_Cull", 0f); EditorUtility.SetDirty(matHouseWood); }
            if (matNhaTranh != null) { matNhaTranh.SetFloat("_Cull", 0f); EditorUtility.SetDirty(matNhaTranh); }

            var matPierWood = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/GhibliModular/M_Ghibli_PierPlank.mat");
            var matBamboo = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/CaMau/M_CaMau_Bamboo.mat");
            var matRock = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/LanaOverhaul/M_Lana_RockUnderbelly.mat");
            if (matRock == null) matRock = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/GhibliModular/M_Ghibli_PebbleRock.mat");

            // =========================================================================
            // 2. Clear Old Scene Layout Objects
            // =========================================================================
            ClearOldSceneLayout();

            // =========================================================================
            // 3. Build 3D Terrain: Overlook Ridge, Village Plain, Western Rice Terraces, Eastern Sand Beach
            // =========================================================================
            Build3DTerrain(matGrass, matSand, matRiceCrop, matOcean, matRock);

            // =========================================================================
            // 4. Build Earthen Road Network (Nam Bo Red Dirt)
            // =========================================================================
            BuildEarthenRoadNetwork(matRedDirt);

            // =========================================================================
            // 5. Build Turquoise Ocean Bay, Sandy Shore & Wooden Pier
            // =========================================================================
            BuildTurquoiseBayAndPier(matOcean, matSand, matPierWood);

            // =========================================================================
            // 6. Place Authentic Thatched Cottages & Huts in Playable Village
            // =========================================================================
            PlaceVillageCottages(matThatched, matHouseWood, matNhaTranh);

            // =========================================================================
            // 7. Build Bamboo Yard Fences, Trellises & Net Frames
            // =========================================================================
            BuildBambooFencesAndProps(matBamboo, matRedDirt);

            // =========================================================================
            // 8. Place Native Tropical Foliage: Coconuts, Bananas, Bamboo, Shade Trees
            // =========================================================================
            PlaceTropicalFoliage();

            // =========================================================================
            // 9. Setup Distant Mountain Silhouettes & 360 Panoramic Sky
            // =========================================================================
            SetupDistantMountainsAndSky(matGrass);

            // =========================================================================
            // 10. Position All NPCs and Player firmly on the ground
            // =========================================================================
            PositionNPCsAndPlayer();

            // =========================================================================
            // 11. Frame Camera for Majestic Wide Vista matching Reference Artwork
            // =========================================================================
            SetupVistaCamera();

            Physics.SyncTransforms();

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log("[RebuildMap] Complete 3D Map Reconstruction Finished Successfully!");
        }

        private static void ClearOldSceneLayout()
        {
            // Clear existing houses and test houses
            string[] houseNames = new string[]
            {
                "House_An_StiltedHouse_3D", "Nha_HangXom_NhaAn", "Nha_BacBa_KhuLayNuoc",
                "Nha_XomVuon_BacHai", "Nha_XomVuon_Go", "Nha_NongDan_CanhDong", "WaterHut_RiverEdge_3D",
                "House_Luoica_East", "House_An_Central", "House_BacHai_Orchard", "House_BacBa_Beachside",
                "House_North_Neighbor", "WaterHut_Beachside", "House_Farmer_Paddies",
                "House_1_ForegroundRight", "House_2_NhaAn_Central", "House_3_CenterBack",
                "House_4_Orchard_BacHai", "House_5_North_Neighbor", "House_6_Beachside_BacBa",
                "House_7_Paddy_FarmerHut", "House_8_Far_North", "House_9_Beachhead", "Test_House",
                "House1_NetPergola", "House1_Pergola_Awning", "House1_SunShelter_Pergola",
                "House_Paddy_Far_02", "Paddy_Hut_01", "Paddy_Hut_02", "Paddy_Hut_03_Far"
            };
            foreach (var n in houseNames)
            {
                var go = GameObject.Find(n);
                if (go != null) Object.DestroyImmediate(go);
            }

            // Clear old children under 06_Canh_Dong except NPCs
            var areaCanhDong = GameObject.Find("CaMau_1954_Village/Village_Areas/06_Canh_Dong");
            if (areaCanhDong != null)
            {
                for (int i = areaCanhDong.transform.childCount - 1; i >= 0; i--)
                {
                    var c = areaCanhDong.transform.GetChild(i);
                    if (c.name.StartsWith("NPC_")) continue;
                    Object.DestroyImmediate(c.gameObject);
                }
            }

            // Clear old children under 07_Bo_Song except NPCs
            var areaBoSong = GameObject.Find("CaMau_1954_Village/Village_Areas/07_Bo_Song");
            if (areaBoSong != null)
            {
                for (int i = areaBoSong.transform.childCount - 1; i >= 0; i--)
                {
                    var c = areaBoSong.transform.GetChild(i);
                    if (c.name.StartsWith("NPC_")) continue;
                    Object.DestroyImmediate(c.gameObject);
                }
            }

            // Clear old pier children under 08_Ben_Xuong except NPCs
            var areaWharf = GameObject.Find("CaMau_1954_Village/Village_Areas/08_Ben_Xuong");
            if (areaWharf != null)
            {
                for (int i = areaWharf.transform.childCount - 1; i >= 0; i--)
                {
                    var c = areaWharf.transform.GetChild(i);
                    if (c.name.StartsWith("NPC_")) continue;
                    Object.DestroyImmediate(c.gameObject);
                }
            }

            // Clear old paths under Paths_3D_Network
            var pathsRoot = GameObject.Find("CaMau_1954_Village/Environment/Paths_3D_Network");
            if (pathsRoot != null)
            {
                for (int i = pathsRoot.transform.childCount - 1; i >= 0; i--)
                {
                    Object.DestroyImmediate(pathsRoot.transform.GetChild(i).gameObject);
                }
            }

            // Clear old base slabs under Terrain_3D
            var terrainRoot = GameObject.Find("CaMau_1954_Village/Environment/Terrain_3D");
            if (terrainRoot != null)
            {
                for (int i = terrainRoot.transform.childCount - 1; i >= 0; i--)
                {
                    Object.DestroyImmediate(terrainRoot.transform.GetChild(i).gameObject);
                }
            }

            // Clear old water & dynamic objects under WaterSystem_3D
            var waterRoot = GameObject.Find("CaMau_1954_Village/Environment/WaterSystem_3D");
            if (waterRoot != null)
            {
                for (int i = waterRoot.transform.childCount - 1; i >= 0; i--)
                {
                    var c = waterRoot.transform.GetChild(i);
                    if (c.name.StartsWith("Ocean_") || c.name.StartsWith("Sea") || c.name.StartsWith("Bay_") ||
                        c.name.StartsWith("Beach_") || c.name.StartsWith("Water_") || c.name.StartsWith("Mangrove_") ||
                        c.name.StartsWith("PF_Mod_MudBank") || c.name.StartsWith("PF_Mod_Mangrove") ||
                        c.name.StartsWith("PF_MossyBoulder") || c.name.StartsWith("PF_Rock_Large") ||
                        c.name.StartsWith("PF_WaterLily") || c.name.StartsWith("WaterLily_") || c.name.StartsWith("Ship_") ||
                        c.name.StartsWith("Boat_") || c.name.StartsWith("Headland_"))
                    {
                        Object.DestroyImmediate(c.gameObject);
                    }
                }
            }

            // Clear foliage under Vegetation_3D
            var vegRoot = GameObject.Find("CaMau_1954_Village/Environment/Vegetation_3D");
            if (vegRoot != null)
            {
                for (int i = vegRoot.transform.childCount - 1; i >= 0; i--)
                {
                    var c = vegRoot.transform.GetChild(i);
                    Object.DestroyImmediate(c.gameObject);
                }
            }

            // Clear dynamic fences under Village_Fences_Network
            var fencesRoot = GameObject.Find("CaMau_1954_Village/Environment/Village_Fences_Network");
            if (fencesRoot != null)
            {
                for (int i = fencesRoot.transform.childCount - 1; i >= 0; i--)
                {
                    Object.DestroyImmediate(fencesRoot.transform.GetChild(i).gameObject);
                }
            }

            // Clear old background vista quads under 09_Natural_Painterly_Background
            var bgRoot = GameObject.Find("CaMau_1954_Village/Environment/09_Natural_Painterly_Background");
            if (bgRoot != null)
            {
                for (int i = bgRoot.transform.childCount - 1; i >= 0; i--)
                {
                    Object.DestroyImmediate(bgRoot.transform.GetChild(i).gameObject);
                }
            }

            // Disable or clear Background_Atmosphere if present
            var bgAtm = GameObject.Find("CaMau_1954_Village/Environment/Background_Atmosphere");
            if (bgAtm != null)
            {
                bgAtm.SetActive(false);
            }
        }

        private static void Build3DTerrain(Material matGrass, Material matSand, Material matRiceCrop, Material matWater, Material matRock)
        {
            var terrainRoot = GameObject.Find("CaMau_1954_Village/Environment/Terrain_3D");
            if (terrainRoot == null)
            {
                var env = GameObject.Find("CaMau_1954_Village/Environment");
                terrainRoot = new GameObject("Terrain_3D");
                terrainRoot.transform.parent = env != null ? env.transform : null;
            }

            // =========================================================================
            // A. Foreground Overlook Ridge (High Viewpoint Plateau at Y = 4.2m, Z: [-20m, -40m])
            // Perfectly positioned for the scenic vista overview & wide camera angles
            // =========================================================================
            var overlookRidge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            overlookRidge.name = "Terrain_Overlook_Ridge";
            overlookRidge.transform.parent = terrainRoot.transform;
            overlookRidge.transform.position = new Vector3(-1.5f, 2.10f, -30.0f);
            overlookRidge.transform.localScale = new Vector3(85.0f, 4.20f, 20.0f);
            overlookRidge.GetComponent<MeshRenderer>().sharedMaterial = matGrass;

            // Overlook slope smoothly descending from Y = 4.2m at Z = -20.0m to village ground level Y = 0.0m at Z = -13.5m
            var overlookSlope = GameObject.CreatePrimitive(PrimitiveType.Cube);
            overlookSlope.name = "Terrain_Overlook_Slope";
            overlookSlope.transform.parent = terrainRoot.transform;
            overlookSlope.transform.position = new Vector3(-1.5f, 2.10f, -16.75f);
            overlookSlope.transform.rotation = Quaternion.Euler(32.87f, 0f, 0f);
            overlookSlope.transform.localScale = new Vector3(80.0f, 0.40f, 7.74f);
            overlookSlope.GetComponent<MeshRenderer>().sharedMaterial = matGrass;

            // =========================================================================
            // B. Central Village Grass Plain (Ground Level Y = 0.00m, X: [-14m, +18m], Z: [-17m, +45m])
            // =========================================================================
            var villageBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            villageBase.name = "Terrain_Village_Central";
            villageBase.transform.parent = terrainRoot.transform;
            villageBase.transform.position = new Vector3(2.0f, -0.25f, 12.0f);
            villageBase.transform.localScale = new Vector3(34.0f, 0.50f, 62.0f);
            villageBase.GetComponent<MeshRenderer>().sharedMaterial = matGrass;

            // =========================================================================
            // C. Western Vast Emerald Rice Terraces (X: [-14m, -110m], Z: [-18m, +85m])
            // Vast mosaic of terraced plots matching the expansive valley in the artwork
            // =========================================================================
            var paddyBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            paddyBase.name = "Terrain_RiceFields_VastWest";
            paddyBase.transform.parent = terrainRoot.transform;
            paddyBase.transform.position = new Vector3(-62.0f, -0.25f, 32.0f);
            paddyBase.transform.localScale = new Vector3(96.0f, 0.50f, 105.0f);
            paddyBase.GetComponent<MeshRenderer>().sharedMaterial = matRiceCrop;

            // 3D Rice Plant Prefabs for Volumetric Rice Rows
            var riceRowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Rice/PF_Rice_RowStrip_4m.prefab");
            var hutPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/GhibliModular/PF_Ghibli_WaterHut.prefab");

            // Generate 24 Terraced Rice Paddy Plots with Chartreuse Rice Crops & Water Channels
            float[] plotX = new float[] { -22f, -38f, -54f, -70f, -86f, -102f };
            float[] plotZ = new float[] { -10f, 8f, 26f, 44f };

            int plotIndex = 1;
            for (int ix = 0; ix < plotX.Length; ix++)
            {
                for (int iz = 0; iz < plotZ.Length; iz++)
                {
                    float rx = plotX[ix];
                    float rz = plotZ[iz];

                    // Lush Green Rice Plot surface
                    var cropPlot = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    cropPlot.name = $"Paddy_Plot_Rice_{plotIndex:D2}";
                    cropPlot.transform.parent = terrainRoot.transform;
                    cropPlot.transform.position = new Vector3(rx + 7.0f, 0.02f, rz + 8.0f);
                    cropPlot.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    cropPlot.transform.localScale = new Vector3(14.0f, 16.0f, 1f);
                    cropPlot.GetComponent<MeshRenderer>().sharedMaterial = matRiceCrop;

                    // Narrow water channel reflecting sunny sky
                    var pool = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    pool.name = $"Paddy_Water_Channel_{plotIndex:D2}";
                    pool.transform.parent = terrainRoot.transform;
                    pool.transform.position = new Vector3(rx + 13.0f, 0.025f, rz + 8.0f);
                    pool.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    pool.transform.localScale = new Vector3(1.6f, 14.5f, 1f);
                    pool.GetComponent<MeshRenderer>().sharedMaterial = matWater;

                    // North grassy dike
                    var dikeN = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    dikeN.name = $"Dike_N_{plotIndex:D2}";
                    dikeN.transform.parent = terrainRoot.transform;
                    dikeN.transform.position = new Vector3(rx + 7.0f, 0.06f, rz + 16.0f);
                    dikeN.transform.localScale = new Vector3(15.0f, 0.12f, 1.2f);
                    dikeN.GetComponent<MeshRenderer>().sharedMaterial = matGrass;

                    // East grassy dike
                    var dikeE = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    dikeE.name = $"Dike_E_{plotIndex:D2}";
                    dikeE.transform.parent = terrainRoot.transform;
                    dikeE.transform.position = new Vector3(rx + 14.0f, 0.06f, rz + 8.0f);
                    dikeE.transform.localScale = new Vector3(1.2f, 0.12f, 16.0f);
                    dikeE.GetComponent<MeshRenderer>().sharedMaterial = matGrass;

                    // Place 3D Rice Tuft Rows on near plots - Parent to terrainRoot with upright rotation (NO 90-degree tilt!)
                    if (ix < 3 && iz < 2)
                    {
                        if (riceRowPrefab != null)
                        {
                            for (float rzSub = rz + 3.0f; rzSub <= rz + 13.0f; rzSub += 4.5f)
                            {
                                var row = (GameObject)PrefabUtility.InstantiatePrefab(riceRowPrefab, terrainRoot.transform);
                                row.name = $"RiceRow_P{plotIndex:D2}_Z{rzSub:F0}";
                                row.transform.position = new Vector3(rx + 7.0f, 0.04f, rzSub);
                                row.transform.rotation = Quaternion.identity; // Upright!
                                row.transform.localScale = new Vector3(1.0f, 1.1f, 1.0f);
                            }
                        }
                    }

                    plotIndex++;
                }
            }

            // Thatched Farmer Watch-Huts scattered in the Rice Valley
            if (hutPrefab != null)
            {
                var h1 = (GameObject)PrefabUtility.InstantiatePrefab(hutPrefab, terrainRoot.transform);
                h1.name = "Paddy_Hut_01";
                h1.transform.position = new Vector3(-28.0f, -0.15f, 9.0f);
                h1.transform.rotation = Quaternion.Euler(0f, 35f, 0f);
                h1.transform.localScale = Vector3.one * 1.1f;

                var h2 = (GameObject)PrefabUtility.InstantiatePrefab(hutPrefab, terrainRoot.transform);
                h2.name = "Paddy_Hut_02";
                h2.transform.position = new Vector3(-58.0f, -0.15f, 32.0f);
                h2.transform.rotation = Quaternion.Euler(0f, -20f, 0f);
                h2.transform.localScale = Vector3.one * 1.05f;

                var h3 = (GameObject)PrefabUtility.InstantiatePrefab(hutPrefab, terrainRoot.transform);
                h3.name = "Paddy_Hut_03_Far";
                h3.transform.position = new Vector3(-84.0f, -0.15f, 52.0f);
                h3.transform.rotation = Quaternion.Euler(0f, 15f, 0f);
                h3.transform.localScale = Vector3.one * 0.95f;
            }

            // Towering Green Karst Mountains behind the Western Rice Valley
            var westMountain1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            westMountain1.name = "Mountain_Karst_FarWest_01";
            westMountain1.transform.parent = terrainRoot.transform;
            westMountain1.transform.position = new Vector3(-105.0f, 14.0f, 20.0f);
            westMountain1.transform.localScale = new Vector3(65.0f, 38.0f, 75.0f);
            westMountain1.GetComponent<MeshRenderer>().sharedMaterial = matGrass;

            var westMountain2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            westMountain2.name = "Mountain_Karst_FarWest_02";
            westMountain2.transform.parent = terrainRoot.transform;
            westMountain2.transform.position = new Vector3(-115.0f, 18.0f, 60.0f);
            westMountain2.transform.localScale = new Vector3(75.0f, 44.0f, 85.0f);
            westMountain2.GetComponent<MeshRenderer>().sharedMaterial = matGrass;

            var westMountain3 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            westMountain3.name = "Mountain_Karst_FarWest_03";
            westMountain3.transform.parent = terrainRoot.transform;
            westMountain3.transform.position = new Vector3(-125.0f, 22.0f, 105.0f);
            westMountain3.transform.localScale = new Vector3(90.0f, 52.0f, 95.0f);
            westMountain3.GetComponent<MeshRenderer>().sharedMaterial = matGrass;

            // =========================================================================
            // D. Eastern Curved Golden Beach Shoreline (X: [+18m, +32m], Z: [-25m, +80m])
            // =========================================================================
            var beachMain = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beachMain.name = "Terrain_Beach_Eastern";
            beachMain.transform.parent = terrainRoot.transform;
            beachMain.transform.position = new Vector3(24.5f, -0.30f, 28.0f);
            beachMain.transform.localScale = new Vector3(14.0f, 0.40f, 105.0f);
            beachMain.GetComponent<MeshRenderer>().sharedMaterial = matSand;

            // Curved southern shoreline towards overlook
            var beachSouth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beachSouth.name = "Terrain_Beach_SouthCurve";
            beachSouth.transform.parent = terrainRoot.transform;
            beachSouth.transform.position = new Vector3(27.5f, -0.32f, -19.0f);
            beachSouth.transform.rotation = Quaternion.Euler(0f, 22f, 0f);
            beachSouth.transform.localScale = new Vector3(16.0f, 0.40f, 26.0f);
            beachSouth.GetComponent<MeshRenderer>().sharedMaterial = matSand;

            // =========================================================================
            // E. Prominent Rocky Green Headland / Cape at the far end of the beach
            // (Mũi đá xanh nhô ra biển - Key feature from reference artwork)
            // =========================================================================
            var headlandBase = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            headlandBase.name = "Headland_Cape_NorthBeach";
            headlandBase.transform.parent = terrainRoot.transform;
            headlandBase.transform.position = new Vector3(42.0f, 6.0f, 68.0f);
            headlandBase.transform.localScale = new Vector3(36.0f, 18.0f, 42.0f);
            headlandBase.GetComponent<MeshRenderer>().sharedMaterial = matGrass;

            var headlandPoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            headlandPoint.name = "Headland_Cape_SeaTip";
            headlandPoint.transform.parent = terrainRoot.transform;
            headlandPoint.transform.position = new Vector3(48.0f, 4.0f, 76.0f);
            headlandPoint.transform.localScale = new Vector3(24.0f, 12.0f, 28.0f);
            headlandPoint.GetComponent<MeshRenderer>().sharedMaterial = matGrass;
        }

        private static void BuildEarthenRoadNetwork(Material matRedDirt)
        {
            var pathsRoot = GameObject.Find("CaMau_1954_Village/Environment/Paths_3D_Network");
            if (pathsRoot == null)
            {
                var env = GameObject.Find("CaMau_1954_Village/Environment");
                pathsRoot = new GameObject("Paths_3D_Network");
                pathsRoot.transform.parent = env != null ? env.transform : null;
            }

            // 1. Village Central Hub / Crossroad
            var pathCross = GameObject.CreatePrimitive(PrimitiveType.Quad);
            pathCross.name = "Path_Village_Crossroad";
            pathCross.transform.parent = pathsRoot.transform;
            pathCross.transform.position = new Vector3(0.0f, 0.02f, -1.0f);
            pathCross.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            pathCross.transform.localScale = new Vector3(3.6f, 12.0f, 1f);
            pathCross.GetComponent<MeshRenderer>().sharedMaterial = matRedDirt;

            // 2. Main Village Street running north past Nha An to House 5 & 8
            var pathNorth = GameObject.CreatePrimitive(PrimitiveType.Quad);
            pathNorth.name = "Path_North_Street";
            pathNorth.transform.parent = pathsRoot.transform;
            pathNorth.transform.position = new Vector3(-0.5f, 0.02f, 13.5f);
            pathNorth.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            pathNorth.transform.localScale = new Vector3(3.4f, 20.0f, 1f);
            pathNorth.GetComponent<MeshRenderer>().sharedMaterial = matRedDirt;

            // 3. East Road curving around House 1 to Beach & Wooden Pier
            var pathEastBeach = GameObject.CreatePrimitive(PrimitiveType.Quad);
            pathEastBeach.name = "Path_East_To_Beach_Pier";
            pathEastBeach.transform.parent = pathsRoot.transform;
            pathEastBeach.transform.position = new Vector3(10.5f, 0.02f, 1.2f);
            pathEastBeach.transform.rotation = Quaternion.Euler(90f, 0f, -8f);
            pathEastBeach.transform.localScale = new Vector3(15.0f, 3.4f, 1f);
            pathEastBeach.GetComponent<MeshRenderer>().sharedMaterial = matRedDirt;

            // 4. West Road curving into Western Rice Terraces
            var pathWestRice = GameObject.CreatePrimitive(PrimitiveType.Quad);
            pathWestRice.name = "Path_West_To_RiceFields";
            pathWestRice.transform.parent = pathsRoot.transform;
            pathWestRice.transform.position = new Vector3(-9.5f, 0.02f, 0.5f);
            pathWestRice.transform.rotation = Quaternion.Euler(90f, 0f, 6f);
            pathWestRice.transform.localScale = new Vector3(14.0f, 3.4f, 1f);
            pathWestRice.GetComponent<MeshRenderer>().sharedMaterial = matRedDirt;

            // 5. Southern approach path curving past House 1 towards village center
            var pathHouse1Approach = GameObject.CreatePrimitive(PrimitiveType.Quad);
            pathHouse1Approach.name = "Path_House1_Approach";
            pathHouse1Approach.transform.parent = pathsRoot.transform;
            pathHouse1Approach.transform.position = new Vector3(2.5f, 0.02f, -8.5f);
            pathHouse1Approach.transform.rotation = Quaternion.Euler(90f, 0f, -20f);
            pathHouse1Approach.transform.localScale = new Vector3(3.4f, 10.0f, 1f);
            pathHouse1Approach.GetComponent<MeshRenderer>().sharedMaterial = matRedDirt;

            // 6. Beachside stroll road heading north past House 6 to beachhead
            var pathBeachStroll = GameObject.CreatePrimitive(PrimitiveType.Quad);
            pathBeachStroll.name = "Path_Beachside_Stroll";
            pathBeachStroll.transform.parent = pathsRoot.transform;
            pathBeachStroll.transform.position = new Vector3(19.5f, 0.02f, 17.0f);
            pathBeachStroll.transform.rotation = Quaternion.Euler(90f, 0f, 10f);
            pathBeachStroll.transform.localScale = new Vector3(3.2f, 18.0f, 1f);
            pathBeachStroll.GetComponent<MeshRenderer>().sharedMaterial = matRedDirt;

            // 7. Rice paddy inspection dike path leading into western fields
            var pathRiceWalkway = GameObject.CreatePrimitive(PrimitiveType.Quad);
            pathRiceWalkway.name = "Path_Rice_Dike_Walkway";
            pathRiceWalkway.transform.parent = pathsRoot.transform;
            pathRiceWalkway.transform.position = new Vector3(-24.0f, 0.02f, 15.0f);
            pathRiceWalkway.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            pathRiceWalkway.transform.localScale = new Vector3(2.2f, 28.0f, 1f);
            pathRiceWalkway.GetComponent<MeshRenderer>().sharedMaterial = matRedDirt;
        }

        private static void BuildTurquoiseBayAndPier(Material matOcean, Material matSand, Material matPierWood)
        {
            var waterRoot = GameObject.Find("CaMau_1954_Village/Environment/WaterSystem_3D");
            if (waterRoot == null)
            {
                var env = GameObject.Find("CaMau_1954_Village/Environment");
                waterRoot = new GameObject("WaterSystem_3D");
                waterRoot.transform.parent = env != null ? env.transform : null;
            }

            // =========================================================================
            // 1. Vast Turquoise Ocean Bay (X: [+25m, +140m], Z: [-35m, +130m], Y = -0.35m)
            // =========================================================================
            var oceanBay = GameObject.CreatePrimitive(PrimitiveType.Quad);
            oceanBay.name = "Ocean_Turquoise_Bay";
            oceanBay.transform.parent = waterRoot.transform;
            oceanBay.transform.position = new Vector3(80.0f, -0.35f, 40.0f);
            oceanBay.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            oceanBay.transform.localScale = new Vector3(120.0f, 160.0f, 1f);
            oceanBay.GetComponent<MeshRenderer>().sharedMaterial = matOcean;

            // =========================================================================
            // 2. Long Wooden Pier extending into the Turquoise Bay
            // =========================================================================
            var pierRoot = GameObject.Find("CaMau_1954_Village/Village_Areas/08_Ben_Xuong");
            if (pierRoot == null)
            {
                var areas = GameObject.Find("CaMau_1954_Village/Village_Areas");
                pierRoot = new GameObject("08_Ben_Xuong");
                pierRoot.transform.parent = areas != null ? areas.transform : null;
            }

            // Pier Main Deck: from X = 18m to X = 38m (Length 20m, Width 3.6m, Y = 0.05m)
            var pierDeck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pierDeck.name = "Wooden_Pier_MainDeck";
            pierDeck.transform.parent = pierRoot.transform;
            pierDeck.transform.position = new Vector3(28.0f, 0.05f, 2.5f);
            pierDeck.transform.localScale = new Vector3(20.0f, 0.20f, 3.6f);
            pierDeck.GetComponent<MeshRenderer>().sharedMaterial = matPierWood;

            // Pier T-Head Wharf at the end (X = 38m)
            var pierTHead = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pierTHead.name = "Wooden_Pier_THead";
            pierTHead.transform.parent = pierRoot.transform;
            pierTHead.transform.position = new Vector3(38.0f, 0.05f, 2.5f);
            pierTHead.transform.localScale = new Vector3(4.0f, 0.20f, 10.0f);
            pierTHead.GetComponent<MeshRenderer>().sharedMaterial = matPierWood;

            // Pier Pilings
            float[] pierX = new float[] { 20.0f, 24.5f, 29.0f, 33.5f, 38.0f };
            foreach (var px in pierX)
            {
                var stiltL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stiltL.name = $"Pier_Stilt_L_{px:F0}";
                stiltL.transform.parent = pierRoot.transform;
                stiltL.transform.position = new Vector3(px, -0.65f, 0.9f);
                stiltL.transform.localScale = new Vector3(0.24f, 0.75f, 0.24f);
                stiltL.GetComponent<MeshRenderer>().sharedMaterial = matPierWood;

                var stiltR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stiltR.name = $"Pier_Stilt_R_{px:F0}";
                stiltR.transform.parent = pierRoot.transform;
                stiltR.transform.position = new Vector3(px, -0.65f, 4.1f);
                stiltR.transform.localScale = new Vector3(0.24f, 0.75f, 0.24f);
                stiltR.GetComponent<MeshRenderer>().sharedMaterial = matPierWood;
            }

            // Rustic Pier Waiting Shelter with thatched roof
            var shelter = new GameObject("Pier_Dock_Shelter");
            shelter.transform.parent = pierRoot.transform;
            shelter.transform.position = new Vector3(37.5f, 0.15f, 2.5f);

            var hutPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/GhibliModular/PF_Ghibli_WaterHut.prefab");
            if (hutPrefab != null)
            {
                var h = (GameObject)PrefabUtility.InstantiatePrefab(hutPrefab, shelter.transform);
                h.transform.localPosition = Vector3.zero;
                h.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                h.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);
            }

            // =========================================================================
            // 3. Anchored 3D Sailing Fishing Ships in the Bay (PF_StylizedBoat.prefab)
            // Matching the 5-6 sailing ships scattered in the reference artwork
            // =========================================================================
            var shipsRoot = new GameObject("Bay_Fishing_Ships");
            shipsRoot.transform.parent = waterRoot.transform;

            var boatPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Vehicles/PF_StylizedBoat.prefab");
            if (boatPrefab != null)
            {
                InstantiateBoat(boatPrefab, shipsRoot.transform, "Boat_Bay_01_PierEnd", new Vector3(38.0f, -0.28f, 16.0f), 3.0f, 35.0f);
                InstantiateBoat(boatPrefab, shipsRoot.transform, "Boat_Bay_02_MidBay", new Vector3(54.0f, -0.28f, 26.0f), 3.6f, -30.0f);
                InstantiateBoat(boatPrefab, shipsRoot.transform, "Boat_Bay_03_SouthBay", new Vector3(46.0f, -0.28f, -8.0f), 2.8f, 20.0f);
                InstantiateBoat(boatPrefab, shipsRoot.transform, "Boat_Bay_04_NorthBay", new Vector3(68.0f, -0.28f, 14.0f), 3.8f, -15.0f);
                InstantiateBoat(boatPrefab, shipsRoot.transform, "Boat_Bay_05_NearCape", new Vector3(62.0f, -0.28f, 48.0f), 3.2f, 25.0f);
                InstantiateBoat(boatPrefab, shipsRoot.transform, "Boat_Bay_06_FarHorizon", new Vector3(92.0f, -0.28f, 42.0f), 2.4f, 10.0f);
            }

            // =========================================================================
            // 4. Wooden Sampans Pulled Up onto Golden Sand Shore & Moored at Pier
            // =========================================================================
            var sampansRoot = new GameObject("Beach_Sampans");
            sampansRoot.transform.parent = waterRoot.transform;

            var sampanPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Props/PF_LanaVN_Sampan_01.prefab");
            if (sampanPrefab != null)
            {
                // Beached on sand near water
                var sp1 = (GameObject)PrefabUtility.InstantiatePrefab(sampanPrefab, sampansRoot.transform);
                sp1.name = "Sampan_Beach_Resting_01";
                sp1.transform.position = new Vector3(21.5f, -0.12f, -1.5f);
                sp1.transform.rotation = Quaternion.Euler(8.0f, 115.0f, 4.0f);
                sp1.transform.localScale = Vector3.one * 1.05f;

                var sp2 = (GameObject)PrefabUtility.InstantiatePrefab(sampanPrefab, sampansRoot.transform);
                sp2.name = "Sampan_Beach_Resting_02";
                sp2.transform.position = new Vector3(23.5f, -0.15f, 9.5f);
                sp2.transform.rotation = Quaternion.Euler(6.0f, 65.0f, -3.0f);
                sp2.transform.localScale = Vector3.one * 1.0f;

                var sp3 = (GameObject)PrefabUtility.InstantiatePrefab(sampanPrefab, sampansRoot.transform);
                sp3.name = "Sampan_Pier_Moored";
                sp3.transform.position = new Vector3(39.5f, -0.32f, 4.2f);
                sp3.transform.rotation = Quaternion.Euler(0f, 95f, 0f);
                sp3.transform.localScale = Vector3.one * 1.1f;
            }

            // =========================================================================
            // 5. Rocky Islets & Sea Boulders in Bay & Shore
            // =========================================================================
            var seaRockNorth = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Rocks/PF_Rock_Large_01.prefab");
            if (seaRockNorth != null)
            {
                var rk1 = (GameObject)PrefabUtility.InstantiatePrefab(seaRockNorth, waterRoot.transform);
                rk1.name = "SeaRock_Islet_NorthBay";
                rk1.transform.position = new Vector3(52.0f, -0.2f, 46.0f);
                rk1.transform.localScale = Vector3.one * 3.4f;

                var rk2 = (GameObject)PrefabUtility.InstantiatePrefab(seaRockNorth, waterRoot.transform);
                rk2.name = "SeaRock_BeachShore_South";
                rk2.transform.position = new Vector3(29.5f, -0.25f, -17.5f);
                rk2.transform.localScale = Vector3.one * 2.4f;

                var rk3 = (GameObject)PrefabUtility.InstantiatePrefab(seaRockNorth, waterRoot.transform);
                rk3.name = "SeaRock_Headland_Cliff_01";
                rk3.transform.position = new Vector3(45.0f, 0.5f, 65.0f);
                rk3.transform.localScale = Vector3.one * 4.2f;

                var rk4 = (GameObject)PrefabUtility.InstantiatePrefab(seaRockNorth, waterRoot.transform);
                rk4.name = "SeaRock_Headland_Cliff_02";
                rk4.transform.position = new Vector3(52.0f, 0.8f, 78.0f);
                rk4.transform.localScale = Vector3.one * 4.8f;

                var rk5 = (GameObject)PrefabUtility.InstantiatePrefab(seaRockNorth, waterRoot.transform);
                rk5.name = "SeaRock_Bay_Far_Islet";
                rk5.transform.position = new Vector3(84.0f, 0.2f, 62.0f);
                rk5.transform.localScale = Vector3.one * 5.0f;
            }
        }

        private static void InstantiateBoat(GameObject prefab, Transform parent, string name, Vector3 pos, float scale, float rotY)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
            go.transform.localScale = Vector3.one * scale;
        }

        private static void PlaceVillageCottages(Material matThatched, Material matWood, Material matNhaTranh)
        {
            var nhaTranhFBX = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Optimized/SM_NhaTranh_BlenderOptimized.fbx");
            var nhaSanGoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/NhaSanGo.prefab");
            var thattchedHouseFBX = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Environment/SM_StiltedHouse_Thatched.fbx");

            // Use the authentic, fully textured SM_NhaTranh_BlenderOptimized.fbx
            var primaryPrefab = nhaTranhFBX != null ? nhaTranhFBX : thattchedHouseFBX;
            var primaryMat = matNhaTranh != null ? matNhaTranh : matThatched;

            // =========================================================================
            // House 1: Foreground-Right Thatched Cottage with Net Drying Yard (Nearest to Overlook)
            // =========================================================================
            if (primaryPrefab != null)
            {
                BuildCottage(primaryPrefab, "House_1_ForegroundRight", new Vector3(10.5f, 0.0f, -8.5f), -15.0f, 4.4f, primaryMat);
            }

            // =========================================================================
            // House 2: Central Village Thatched Cottage (Nhà An)
            // Fully textured authentic thatched cottage facing main road
            // =========================================================================
            if (primaryPrefab != null)
            {
                BuildCottage(primaryPrefab, "House_2_NhaAn_Central", new Vector3(-2.0f, 0.0f, 3.5f), 12.0f, 4.2f, primaryMat);
            }

            // =========================================================================
            // House 3: Center-Right Village Thatched Cottage
            // =========================================================================
            if (primaryPrefab != null)
            {
                BuildCottage(primaryPrefab, "House_3_CenterBack", new Vector3(5.5f, 0.0f, 16.5f), -22.0f, 3.9f, primaryMat);
            }

            // =========================================================================
            // House 4: Orchard Cottage bordering Rice Fields (Nhà Bác Hai)
            // =========================================================================
            if (primaryPrefab != null)
            {
                BuildCottage(primaryPrefab, "House_4_Orchard_BacHai", new Vector3(-12.0f, 0.0f, 4.0f), 26.0f, 3.8f, primaryMat);
            }

            // =========================================================================
            // House 5: North Neighbor Thatched Cottage
            // =========================================================================
            if (primaryPrefab != null)
            {
                BuildCottage(primaryPrefab, "House_5_North_Neighbor", new Vector3(-5.5f, 0.0f, 21.0f), 5.0f, 3.6f, primaryMat);
            }

            // =========================================================================
            // House 6: Beachside Fisherman's Cottage (Nhà Bác Ba)
            // =========================================================================
            if (nhaSanGoPrefab != null)
            {
                var h6 = (GameObject)PrefabUtility.InstantiatePrefab(nhaSanGoPrefab);
                h6.name = "House_6_Beachside_BacBa";
                h6.transform.position = new Vector3(16.5f, 0.0f, 7.5f);
                h6.transform.rotation = Quaternion.Euler(0f, -35.0f, 0f);
                h6.transform.localScale = Vector3.one * 3.8f;
            }
            else if (primaryPrefab != null)
            {
                BuildCottage(primaryPrefab, "House_6_Beachside_BacBa", new Vector3(16.5f, 0.0f, 7.5f), -35.0f, 3.8f, primaryMat);
            }

            // =========================================================================
            // House 8: Far North Thatched Cottage (Nested in trees)
            // =========================================================================
            if (primaryPrefab != null)
            {
                BuildCottage(primaryPrefab, "House_8_Far_North", new Vector3(-1.0f, 0.0f, 32.0f), -10.0f, 3.4f, primaryMat);
            }

            // =========================================================================
            // House 9: Beachhead Fisherman Hut
            // =========================================================================
            if (primaryPrefab != null)
            {
                BuildCottage(primaryPrefab, "House_9_Beachhead", new Vector3(22.0f, 0.0f, 38.0f), -45.0f, 3.2f, primaryMat);
            }
        }

        private static GameObject BuildCottage(GameObject fbxPrefab, string name, Vector3 pos, float rotY, float scale, Material mat)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(fbxPrefab);
            go.name = name;
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
            go.transform.localScale = Vector3.one * scale;

            var r = go.GetComponentInChildren<MeshRenderer>();
            if (r != null && mat != null)
            {
                r.sharedMaterial = mat;
            }

            // Add box collider for navigation / player collision
            var col = go.GetComponent<BoxCollider>();
            if (col == null)
            {
                col = go.AddComponent<BoxCollider>();
                col.center = new Vector3(0f, 0.9f, 0f);
                col.size = new Vector3(1.3f, 1.8f, 1.6f);
            }

            return go;
        }

        private static void BuildBambooFencesAndProps(Material matBamboo, Material matRedDirt)
        {
            var fencesRoot = GameObject.Find("CaMau_1954_Village/Environment/Village_Fences_Network");
            if (fencesRoot == null)
            {
                var env = GameObject.Find("CaMau_1954_Village/Environment");
                fencesRoot = new GameObject("Village_Fences_Network");
                fencesRoot.transform.parent = env != null ? env.transform : null;
            }

            // =========================================================================
            // A. Rustic Wooden Overlook Railing across Foreground Edge (Z = -20.2m, Y = 4.2m)
            // Directly framing the wide overview camera view
            // =========================================================================
            var overlookFence = new GameObject("Fence_Overlook_Railing");
            overlookFence.transform.parent = fencesRoot.transform;

            var matPierWood = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/GhibliModular/M_Ghibli_PierPlank.mat");

            // Build rustic log posts along Z = -20.2m from X = -26m to X = +26m
            for (float x = -26.0f; x <= 26.0f; x += 1.8f)
            {
                var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                post.name = $"Overlook_LogPost_{x:F1}";
                post.transform.parent = overlookFence.transform;
                post.transform.position = new Vector3(x, 4.85f, -20.2f);
                post.transform.localScale = new Vector3(0.20f, 0.65f, 0.20f);
                post.GetComponent<MeshRenderer>().sharedMaterial = matPierWood;
            }

            // Horizontal top rail log
            var topRail = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            topRail.name = "Overlook_TopRail";
            topRail.transform.parent = overlookFence.transform;
            topRail.transform.position = new Vector3(0.0f, 5.40f, -20.2f);
            topRail.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            topRail.transform.localScale = new Vector3(0.16f, 26.5f, 0.16f);
            topRail.GetComponent<MeshRenderer>().sharedMaterial = matPierWood;

            // Horizontal mid rail log
            var midRail = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            midRail.name = "Overlook_MidRail";
            midRail.transform.parent = overlookFence.transform;
            midRail.transform.position = new Vector3(0.0f, 4.80f, -20.2f);
            midRail.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            midRail.transform.localScale = new Vector3(0.12f, 26.5f, 0.12f);
            midRail.GetComponent<MeshRenderer>().sharedMaterial = matPierWood;

            // Boulders and rocks framing foreground overlook corners
            var mossyBoulderPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Rocks/PF_MossyBoulder.prefab");
            if (mossyBoulderPrefab != null)
            {
                var rL1 = (GameObject)PrefabUtility.InstantiatePrefab(mossyBoulderPrefab, overlookFence.transform);
                rL1.name = "Overlook_Rock_Left_01";
                rL1.transform.position = new Vector3(-18.5f, 4.2f, -20.0f);
                rL1.transform.localScale = Vector3.one * 2.2f;

                var rR1 = (GameObject)PrefabUtility.InstantiatePrefab(mossyBoulderPrefab, overlookFence.transform);
                rR1.name = "Overlook_Rock_Right_01";
                rR1.transform.position = new Vector3(18.5f, 4.2f, -20.0f);
                rR1.transform.localScale = Vector3.one * 2.3f;
            }

            // =========================================================================
            // B. House 1 Yard: Earthen Drying Yard, Net Pergola, Fishing Nets & Ceramic Jars
            // =========================================================================
            var yardHouse1 = GameObject.CreatePrimitive(PrimitiveType.Quad);
            yardHouse1.name = "Terrain_Yard_House1";
            yardHouse1.transform.parent = fencesRoot.transform;
            yardHouse1.transform.position = new Vector3(9.0f, 0.015f, -9.5f);
            yardHouse1.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            yardHouse1.transform.localScale = new Vector3(12.5f, 9.5f, 1f);
            yardHouse1.GetComponent<MeshRenderer>().sharedMaterial = matRedDirt;

            // Bamboo fence enclosing House 1 yard
            BuildStraightFence(fencesRoot.transform, "Fence_House1_Front", new Vector3(2.5f, 0.0f, -14.2f), new Vector3(15.2f, 0.0f, -14.2f), matBamboo);
            BuildStraightFence(fencesRoot.transform, "Fence_House1_West", new Vector3(2.5f, 0.0f, -14.2f), new Vector3(2.5f, 0.0f, -4.5f), matBamboo);

            // Sun shelter / Pergola for drying fish nets in House 1 yard
            var pergola = new GameObject("House1_SunShelter_Pergola");
            pergola.transform.parent = fencesRoot.transform;
            pergola.transform.position = new Vector3(6.5f, 0.0f, -8.0f);

            // 4 bamboo posts
            BuildPost(pergola.transform, "Pergola_Post1", new Vector3(5.0f, 0.9f, -9.5f), 1.8f, matBamboo);
            BuildPost(pergola.transform, "Pergola_Post2", new Vector3(8.0f, 0.9f, -9.5f), 1.8f, matBamboo);
            BuildPost(pergola.transform, "Pergola_Post3", new Vector3(5.0f, 0.9f, -6.5f), 1.8f, matBamboo);
            BuildPost(pergola.transform, "Pergola_Post4", new Vector3(8.0f, 0.9f, -6.5f), 1.8f, matBamboo);

            // Thatched sun shade roof
            var shadeRoof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shadeRoof.name = "Pergola_ThatchedRoof";
            shadeRoof.transform.parent = pergola.transform;
            shadeRoof.transform.position = new Vector3(6.5f, 1.82f, -8.0f);
            shadeRoof.transform.localScale = new Vector3(3.5f, 0.08f, 3.5f);
            var matThatched = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Architecture/M_StiltedHouse_Thatched.mat");
            if (matThatched != null) shadeRoof.GetComponent<MeshRenderer>().sharedMaterial = matThatched;

            // Hanging Fishing Nets draped across the drying frame
            var netPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Props/PF_Prop_Fishing_Net.prefab");
            if (netPrefab != null)
            {
                var net1 = (GameObject)PrefabUtility.InstantiatePrefab(netPrefab, pergola.transform);
                net1.name = "Net_Hanging_01";
                net1.transform.position = new Vector3(6.5f, 0.85f, -9.5f);
                net1.transform.localScale = Vector3.one * 1.35f;

                var net2 = (GameObject)PrefabUtility.InstantiatePrefab(netPrefab, pergola.transform);
                net2.name = "Net_Hanging_02";
                net2.transform.position = new Vector3(5.0f, 0.85f, -8.0f);
                net2.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                net2.transform.localScale = Vector3.one * 1.25f;
            }

            // Large Ceramic Water Jars (Lu Nước Sành) in House 1 yard
            var jugPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Props/PF_Mod_WaterJug.prefab");
            if (jugPrefab != null)
            {
                float[,] jugPos = new float[,]
                {
                    { 8.8f, -7.0f },
                    { 9.4f, -6.8f },
                    { 9.1f, -7.5f },
                    { 4.2f, -12.5f },
                    { 4.8f, -12.8f }
                };
                for (int i = 0; i < jugPos.GetLength(0); i++)
                {
                    var jug = (GameObject)PrefabUtility.InstantiatePrefab(jugPrefab, fencesRoot.transform);
                    jug.name = $"LuNuoc_House1_{i + 1}";
                    jug.transform.position = new Vector3(jugPos[i, 0], 0.0f, jugPos[i, 1]);
                    jug.transform.localScale = Vector3.one * Random.Range(1.0f, 1.25f);
                }

                // Water jars near Nha An
                var jugAn1 = (GameObject)PrefabUtility.InstantiatePrefab(jugPrefab, fencesRoot.transform);
                jugAn1.name = "LuNuoc_NhaAn_01";
                jugAn1.transform.position = new Vector3(0.8f, 0.0f, 5.0f);
                jugAn1.transform.localScale = Vector3.one * 1.15f;

                var jugAn2 = (GameObject)PrefabUtility.InstantiatePrefab(jugPrefab, fencesRoot.transform);
                jugAn2.name = "LuNuoc_NhaAn_02";
                jugAn2.transform.position = new Vector3(1.4f, 0.0f, 5.2f);
                jugAn2.transform.localScale = Vector3.one * 1.2f;
            }

            // Authentic Gourd Trellis in Nha An garden
            var trellisPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/GourdTrellis/PF_LanaVN_GourdTrellis_Main.prefab");
            if (trellisPrefab != null)
            {
                var tr = (GameObject)PrefabUtility.InstantiatePrefab(trellisPrefab, fencesRoot.transform);
                tr.name = "GourdTrellis_NhaAn";
                tr.transform.position = new Vector3(-6.0f, 0.0f, 5.5f);
                tr.transform.rotation = Quaternion.Euler(0f, 15f, 0f);
                tr.transform.localScale = Vector3.one * 1.0f;
            }

            // Buffalo in pen beside Orchard
            var buffaloFBX = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Buffalo.fbx");
            if (buffaloFBX != null)
            {
                var bf = (GameObject)PrefabUtility.InstantiatePrefab(buffaloFBX, fencesRoot.transform);
                bf.name = "Buffalo_Village_Grazing";
                bf.transform.position = new Vector3(-9.0f, 0.0f, -4.5f);
                bf.transform.rotation = Quaternion.Euler(0f, 75f, 0f);
                bf.transform.localScale = Vector3.one * 1.15f;
            }

            // Dog resting in House 1 yard
            var dogFBX = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Model_Dog.fbx");
            if (dogFBX != null)
            {
                var dg = (GameObject)PrefabUtility.InstantiatePrefab(dogFBX, fencesRoot.transform);
                dg.name = "Dog_House1_Yard";
                dg.transform.position = new Vector3(7.5f, 0.0f, -11.0f);
                dg.transform.rotation = Quaternion.Euler(0f, -45f, 0f);
                dg.transform.localScale = Vector3.one * 1.1f;
            }

            // =========================================================================
            // C. Bamboo Yard Fences for Nha An & Orchard
            // =========================================================================
            BuildStraightFence(fencesRoot.transform, "Fence_NhaAn_Front", new Vector3(-7.0f, 0.0f, 0.5f), new Vector3(2.0f, 0.0f, 0.5f), matBamboo);
            BuildStraightFence(fencesRoot.transform, "Fence_NhaAn_East", new Vector3(2.0f, 0.0f, 0.5f), new Vector3(2.0f, 0.0f, 8.5f), matBamboo);
            BuildStraightFence(fencesRoot.transform, "Fence_Orchard_Front", new Vector3(-16.0f, 0.0f, -0.5f), new Vector3(-8.0f, 0.0f, -0.5f), matBamboo);
            BuildStraightFence(fencesRoot.transform, "Fence_House3_Front", new Vector3(1.5f, 0.0f, 13.0f), new Vector3(8.5f, 0.0f, 13.0f), matBamboo);
            BuildStraightFence(fencesRoot.transform, "Fence_House3_West", new Vector3(1.5f, 0.0f, 13.0f), new Vector3(1.5f, 0.0f, 18.0f), matBamboo);
            BuildStraightFence(fencesRoot.transform, "Fence_House6_Yard", new Vector3(13.0f, 0.0f, 4.0f), new Vector3(19.0f, 0.0f, 4.0f), matBamboo);
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
                post.transform.position = new Vector3(p.x, 0.45f, p.z);
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

        private static void BuildPost(Transform parent, string name, Vector3 pos, float height, Material mat)
        {
            var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = name;
            post.transform.parent = parent;
            post.transform.position = pos;
            post.transform.localScale = new Vector3(0.12f, height * 0.5f, 0.12f);
            if (mat != null) post.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private static void PlaceTropicalFoliage()
        {
            var vegRoot = GameObject.Find("CaMau_1954_Village/Environment/Vegetation_3D");
            if (vegRoot == null)
            {
                var env = GameObject.Find("CaMau_1954_Village/Environment");
                vegRoot = new GameObject("Vegetation_3D");
                vegRoot.transform.parent = env != null ? env.transform : null;
            }

            var coconutPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_06_Coconut.prefab");
            if (coconutPrefab == null) coconutPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_03_Coconut.prefab");

            var bananaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_05_Banana.prefab");
            var bambooPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_07_BambooGrove.prefab");
            if (bambooPrefab == null) bambooPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_01_Bamboo.prefab");

            var largeTreePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_01_LargeCanopy.prefab");
            if (largeTreePrefab == null) largeTreePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_02_Large.prefab");

            var mediumTreePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_02_Medium.prefab");
            var bushFlowerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Bushes/PF_LanaVN_Bush_Low_02_Flower.prefab");
            var bushBroadleaf = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Bushes/PF_LanaVN_Bush_Broadleaf_01.prefab");

            var yellowFlower = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Wildflowers/PF_LanaVN_Wildflower_05_YellowBlossom.prefab");
            var whiteFlower = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Wildflowers/PF_LanaVN_Wildflower_03_WhiteBlossom.prefab");
            var reedsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Wildflowers/PF_LanaVN_Wildflower_01_Reeds.prefab");

            // =========================================================================
            // 1. Coconut Palms: Naturally clustered along Beach & Framing Overlook
            // =========================================================================
            if (coconutPrefab != null)
            {
                // Along golden beach (Natural curving cluster on the right)
                InstantiateFoliage(coconutPrefab, vegRoot.transform, "Coconut_Beach_01", new Vector3(24.5f, 0.0f, -6.0f), 1.25f);
                InstantiateFoliage(coconutPrefab, vegRoot.transform, "Coconut_Beach_02", new Vector3(27.0f, 0.0f, 2.0f), 1.35f);
                InstantiateFoliage(coconutPrefab, vegRoot.transform, "Coconut_Beach_03", new Vector3(26.0f, 0.0f, 12.0f), 1.30f);
                InstantiateFoliage(coconutPrefab, vegRoot.transform, "Coconut_Beach_04", new Vector3(29.0f, 0.0f, 22.0f), 1.40f);
                InstantiateFoliage(coconutPrefab, vegRoot.transform, "Coconut_Beach_05", new Vector3(31.0f, 0.0f, 34.0f), 1.35f);
                InstantiateFoliage(coconutPrefab, vegRoot.transform, "Coconut_Headland_01", new Vector3(40.0f, 5.0f, 62.0f), 1.45f);
                InstantiateFoliage(coconutPrefab, vegRoot.transform, "Coconut_Headland_02", new Vector3(46.0f, 4.0f, 72.0f), 1.35f);

                // Overlook framing palm in top-left corner
                InstantiateFoliage(coconutPrefab, vegRoot.transform, "Coconut_Overlook_WestFraming", new Vector3(-18.0f, 4.2f, -23.0f), 1.35f);
                InstantiateFoliage(coconutPrefab, vegRoot.transform, "Coconut_Overlook_EastFraming", new Vector3(21.0f, 3.5f, -23.0f), 1.40f);

                // Tall Coconut palms rising in Village Center & Orchard (Placed behind cottages)
                InstantiateFoliage(coconutPrefab, vegRoot.transform, "Coconut_Village_Back", new Vector3(-2.0f, 0.0f, 22.0f), 1.45f);
                InstantiateFoliage(coconutPrefab, vegRoot.transform, "Coconut_Orchard_West", new Vector3(-14.0f, 0.0f, 15.0f), 1.35f);

                // Coconut trees along rice field dikes
                InstantiateFoliage(coconutPrefab, vegRoot.transform, "Coconut_Dike_01", new Vector3(-24.0f, 0.0f, 12.0f), 1.2f);
                InstantiateFoliage(coconutPrefab, vegRoot.transform, "Coconut_Dike_02", new Vector3(-38.0f, 0.0f, 28.0f), 1.25f);
                InstantiateFoliage(coconutPrefab, vegRoot.transform, "Coconut_Dike_03", new Vector3(-54.0f, 0.0f, -6.0f), 1.15f);
                InstantiateFoliage(coconutPrefab, vegRoot.transform, "Coconut_Dike_04", new Vector3(-70.0f, 0.0f, 35.0f), 1.20f);
            }

            // =========================================================================
            // 2. Banana Tree Clusters around Houses & Orchard (Placed beside & behind cottages)
            // =========================================================================
            if (bananaPrefab != null)
            {
                InstantiateFoliage(bananaPrefab, vegRoot.transform, "Banana_House1_Back", new Vector3(15.0f, 0.0f, -6.5f), 1.15f);
                InstantiateFoliage(bananaPrefab, vegRoot.transform, "Banana_House1_Corner", new Vector3(15.5f, 0.0f, -11.5f), 1.10f);
                InstantiateFoliage(bananaPrefab, vegRoot.transform, "Banana_NhaAn_West", new Vector3(-7.0f, 0.0f, 8.5f), 1.20f);
                InstantiateFoliage(bananaPrefab, vegRoot.transform, "Banana_Orchard_Cluster", new Vector3(-15.5f, 0.0f, 6.5f), 1.25f);
                InstantiateFoliage(bananaPrefab, vegRoot.transform, "Banana_Overlook_RightCorner", new Vector3(17.5f, 2.5f, -22.0f), 1.30f);
            }

            // =========================================================================
            // 3. Bamboo Groves behind Nha An & House 3
            // =========================================================================
            if (bambooPrefab != null)
            {
                InstantiateFoliage(bambooPrefab, vegRoot.transform, "Bamboo_NhaAn_Backdrop", new Vector3(-1.0f, 0.0f, 13.0f), 1.25f);
                InstantiateFoliage(bambooPrefab, vegRoot.transform, "Bamboo_House3_Backdrop", new Vector3(8.5f, 0.0f, 23.0f), 1.30f);
                InstantiateFoliage(bambooPrefab, vegRoot.transform, "Bamboo_Orchard_Hedge", new Vector3(-16.5f, 0.0f, 9.5f), 1.20f);
            }

            // =========================================================================
            // 4. Large Tropical Canopy Trees behind Village (Shading Cottages from behind)
            // =========================================================================
            if (largeTreePrefab != null)
            {
                InstantiateFoliage(largeTreePrefab, vegRoot.transform, "ShadeTree_Village_North", new Vector3(3.5f, 0.0f, 28.0f), 1.45f);
                InstantiateFoliage(largeTreePrefab, vegRoot.transform, "ShadeTree_Orchard_West", new Vector3(-20.0f, 0.0f, 18.0f), 1.35f);
                InstantiateFoliage(largeTreePrefab, vegRoot.transform, "ShadeTree_East_Beach_Back", new Vector3(21.0f, 0.0f, 22.0f), 1.30f);
                InstantiateFoliage(largeTreePrefab, vegRoot.transform, "ShadeTree_Headland", new Vector3(44.0f, 6.0f, 70.0f), 1.40f);
            }

            if (mediumTreePrefab != null)
            {
                InstantiateFoliage(mediumTreePrefab, vegRoot.transform, "MediumTree_House3_Side", new Vector3(2.0f, 0.0f, 20.0f), 1.15f);
                InstantiateFoliage(mediumTreePrefab, vegRoot.transform, "MediumTree_North_Road", new Vector3(-2.5f, 0.0f, 36.0f), 1.20f);
            }

            // =========================================================================
            // 5. Shrubs, Bushes & Flowers along Roads, Yards & Overlook Railing
            // =========================================================================
            if (bushFlowerPrefab != null)
            {
                InstantiateFoliage(bushFlowerPrefab, vegRoot.transform, "Bush_House1_Fence", new Vector3(3.0f, 0.0f, -13.8f), 1.0f);
                InstantiateFoliage(bushFlowerPrefab, vegRoot.transform, "Bush_NhaAn_Gate", new Vector3(-2.5f, 0.0f, 0.3f), 1.05f);
                InstantiateFoliage(bushFlowerPrefab, vegRoot.transform, "Bush_Beach_Border", new Vector3(18.0f, 0.0f, 3.5f), 1.1f);
            }

            if (bushBroadleaf != null)
            {
                InstantiateFoliage(bushBroadleaf, vegRoot.transform, "Bush_Crossroad_01", new Vector3(-2.2f, 0.0f, -4.5f), 1.0f);
                InstantiateFoliage(bushBroadleaf, vegRoot.transform, "Bush_Rice_Entry", new Vector3(-12.0f, 0.0f, 0.0f), 1.1f);
            }

            if (yellowFlower != null)
            {
                float[] flowerX = new float[] { -18f, -14f, -10f, -6f, -2f, 2f, 6f, 10f, 14f, 18f };
                foreach (var fx in flowerX)
                {
                    InstantiateFoliage(yellowFlower, vegRoot.transform, $"Flowers_Overlook_Y_{fx:F0}", new Vector3(fx, 4.22f, -20.0f), 1.15f);
                }
                InstantiateFoliage(yellowFlower, vegRoot.transform, "Flowers_Yard_House1", new Vector3(11.0f, 0.02f, -13.5f), 1.0f);
            }

            if (whiteFlower != null)
            {
                float[] flowerW = new float[] { -16f, -12f, -8f, -4f, 0f, 4f, 8f, 12f, 16f };
                foreach (var fw in flowerW)
                {
                    InstantiateFoliage(whiteFlower, vegRoot.transform, $"Flowers_Overlook_W_{fw:F0}", new Vector3(fw, 4.22f, -20.0f), 1.10f);
                }
                InstantiateFoliage(whiteFlower, vegRoot.transform, "Flowers_NhaAn_Yard", new Vector3(-2.0f, 0.02f, 1.0f), 1.0f);
            }

            if (reedsPrefab != null)
            {
                InstantiateFoliage(reedsPrefab, vegRoot.transform, "Reeds_Paddy_Canal_01", new Vector3(-16.0f, 0.0f, 6.0f), 1.1f);
                InstantiateFoliage(reedsPrefab, vegRoot.transform, "Reeds_Paddy_Canal_02", new Vector3(-16.0f, 0.0f, -4.0f), 1.1f);
            }
        }

        private static void InstantiateFoliage(GameObject prefab, Transform parent, string name, Vector3 pos, float scale)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * scale;
        }

        private static void SetupDistantMountainsAndSky(Material matGrass)
        {
            // Configure 360 Daylight Panoramic Skybox with rich cobalt blue & painterly clouds
            var skyMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Backdrop_Sky/M_Skybox_CaMau_Panorama.mat");
            var skyTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Backgrounds_Lana/T_Lana_UpperCobaltSky_Seamless.png");

            if (skyMat != null)
            {
                if (skyTex != null) skyMat.mainTexture = skyTex;
                skyMat.SetColor("_Tint", Color.white);
                skyMat.SetFloat("_Exposure", 1.05f);
                skyMat.SetFloat("_Rotation", 0f);
                EditorUtility.SetDirty(skyMat);
                RenderSettings.skybox = skyMat;
            }

            // Disable fog to keep sky crystal clear
            RenderSettings.fog = false;

            // Warm Daylight Ambient & Sun
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.85f, 0.92f, 1.0f, 1.0f);
            RenderSettings.ambientEquatorColor = new Color(0.72f, 0.80f, 0.65f, 1.0f);
            RenderSettings.ambientGroundColor = new Color(0.55f, 0.45f, 0.35f, 1.0f);
            RenderSettings.ambientIntensity = 1.15f;

            var sun = RenderSettings.sun;
            if (sun != null)
            {
                sun.color = new Color(1.0f, 0.96f, 0.88f, 1.0f);
                sun.intensity = 1.25f;
                sun.transform.rotation = Quaternion.Euler(38.0f, -42.0f, 0.0f);
            }

            // 3D Rolling Green Mountains Chain across Northern Horizon (Zero flat billboard boxes!)
            var terrainRoot = GameObject.Find("CaMau_1954_Village/Environment/Terrain_3D");
            if (terrainRoot != null)
            {
                var hillsRoot = GameObject.Find("Distant_Green_Hills_Chain");
                if (hillsRoot == null)
                {
                    hillsRoot = new GameObject("Distant_Green_Hills_Chain");
                    hillsRoot.transform.parent = terrainRoot.transform;
                }

                // Clear any existing children
                for (int i = hillsRoot.transform.childCount - 1; i >= 0; i--)
                {
                    Object.DestroyImmediate(hillsRoot.transform.GetChild(i).gameObject);
                }

                // Chain of rolling green mountains across the northern horizon
                float[,] hillData = new float[,]
                {
                    // X, Y, Z, ScaleX, ScaleY, ScaleZ
                    { -85.0f, 12.0f, 125.0f, 85.0f, 36.0f, 75.0f }, // Far Northwest Mountain
                    { -50.0f, 10.0f, 135.0f, 80.0f, 32.0f, 70.0f }, // West-Center Mountain
                    { -18.0f,  9.0f, 140.0f, 75.0f, 28.0f, 65.0f }, // North Center Mountain (Behind Village)
                    {  18.0f,  8.0f, 142.0f, 70.0f, 26.0f, 60.0f }, // North-East Mountain
                    {  55.0f,  7.0f, 135.0f, 65.0f, 24.0f, 55.0f }, // Bay Coastal Mountain
                    {  88.0f,  5.5f, 120.0f, 60.0f, 20.0f, 50.0f }, // Far Bay Islet Mountain
                    { 115.0f,  4.5f, 105.0f, 50.0f, 16.0f, 45.0f }  // Ocean Distant Island
                };

                for (int i = 0; i < hillData.GetLength(0); i++)
                {
                    var hill = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    hill.name = $"Horizon_Hill_{i + 1:D2}";
                    hill.transform.parent = hillsRoot.transform;
                    hill.transform.position = new Vector3(hillData[i, 0], hillData[i, 1], hillData[i, 2]);
                    hill.transform.localScale = new Vector3(hillData[i, 3], hillData[i, 4], hillData[i, 5]);
                    hill.GetComponent<MeshRenderer>().sharedMaterial = matGrass;
                }
            }
        }

        private static void PositionNPCsAndPlayer()
        {
            // Player
            var player = GameObject.Find("CaMau_1954_Village/Player");
            if (player != null) player.transform.position = new Vector3(0.0f, 0.0f, -2.0f);

            // Mother An at Nha An front yard
            var mother = GameObject.Find("NPC_Mother_An");
            if (mother != null) mother.transform.position = new Vector3(0.5f, 0.0f, 2.5f);

            // Chief & Kids at Village Square
            var chief = GameObject.Find("NPC_Elderly_VillageChief");
            if (chief != null) chief.transform.position = new Vector3(-2.0f, 0.0f, -1.0f);

            var boy = GameObject.Find("NPC_Child_Playing_Boy");
            if (boy != null) boy.transform.position = new Vector3(1.2f, 0.0f, -1.5f);

            var girl = GameObject.Find("NPC_Child_Playing_Girl");
            if (girl != null) girl.transform.position = new Vector3(2.2f, 0.0f, -1.0f);

            // Carrier Tu along Road
            var tu = GameObject.Find("NPC_Villager_Carrier_Tu");
            if (tu != null) tu.transform.position = new Vector3(4.0f, 0.0f, 0.5f);

            // Uncle Ba & Water Carrier at Well
            var bacBa = GameObject.Find("NPC_Elderly_UncleBa");
            if (bacBa != null) bacBa.transform.position = new Vector3(13.5f, 0.0f, 4.5f);

            var waterCarrier = GameObject.Find("NPC_Female_WaterCarrier");
            if (waterCarrier != null) waterCarrier.transform.position = new Vector3(14.5f, 0.0f, 3.5f);

            // Orchard Gardner & Toddler
            var gardener = GameObject.Find("NPC_Villager_Gardener");
            if (gardener != null) gardener.transform.position = new Vector3(-9.5f, 0.0f, 2.0f);

            var toddler = GameObject.Find("NPC_Baby_Toddler");
            if (toddler != null) toddler.transform.position = new Vector3(-11.0f, 0.0f, 1.5f);

            // Wharf NPCs
            var ferryman = GameObject.Find("NPC_Ferryman_UncleBay");
            if (ferryman != null) ferryman.transform.position = new Vector3(36.0f, 0.05f, 2.5f);

            var wavingMother = GameObject.Find("NPC_Villager_Waving_Mother");
            if (wavingMother != null) wavingMother.transform.position = new Vector3(26.0f, 0.02f, 1.2f);

            var farewellElder = GameObject.Find("NPC_Villager_Farewell_Elder");
            if (farewellElder != null) farewellElder.transform.position = new Vector3(27.0f, 0.02f, 3.8f);
        }

        [MenuItem("Tools/Hoa Do/Camera: Scenic Vista (Reference Match)")]
        public static void SetScenicVistaCamera()
        {
            var cam = Camera.main;
            if (cam != null)
            {
                // Elevated Overlook Panoramic framing matching Reference Artwork perfectly:
                // Camera at (-1.0, 9.2, -27.0) looking down across the village with rustic log railing along foreground bottom
                cam.transform.position = new Vector3(-1.0f, 9.2f, -27.0f);
                cam.transform.rotation = Quaternion.Euler(16.5f, 9.5f, 0.0f);
                cam.fieldOfView = 64.0f;
                cam.nearClipPlane = 0.3f;
                cam.farClipPlane = 450.0f;
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                Debug.Log("[RebuildMap] Camera set to Scenic Vista (Reference Match)!");
            }
        }

        [MenuItem("Tools/Hoa Do/Camera: Wide Overview (Map Design)")]
        public static void SetWideOverviewCamera()
        {
            var cam = Camera.main;
            if (cam != null)
            {
                // Pulled-back elevated wide overview for comprehensive map design
                cam.transform.position = new Vector3(-1.5f, 14.5f, -32.0f);
                cam.transform.rotation = Quaternion.Euler(22.0f, 10.0f, 0.0f);
                cam.fieldOfView = 66.0f;
                cam.nearClipPlane = 0.3f;
                cam.farClipPlane = 450.0f;
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                Debug.Log("[RebuildMap] Camera set to Wide Overview (Map Design)!");
            }
        }

        private static void SetupVistaCamera()
        {
            SetScenicVistaCamera();
        }
    }
}
