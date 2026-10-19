#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HoaVaMau.Editor
{
    public static class OverhaulScene1Visuals
    {
        public const string ScenePath = "Assets/Scenes/New Scene 1_Overhaul.unity";
        private const string GenAssetsDir = "Assets/Materials/LanaOverhaul";

        [MenuItem("Hoa va Mau/Level Design/Execute Planet of Lana Scene Overhaul")]
        public static void ExecuteOverhaul()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Debug.Log($"<color=#38bdf8>[LanaOverhaul]</color> Starting authentic Planet of Lana level design overhaul on: {scene.name}");

            if (!Directory.Exists(GenAssetsDir)) Directory.CreateDirectory(GenAssetsDir);

            // 1. Purge all AI clouds, AI mountains, AI trees, repeating tree-leaf lines, black cutouts, and previous passes
            PurgeAllAIAndMisplacedAssets();

            // 2. Create clean Planet of Lana materials (Ledge, Rock Cross-Section, Mossy Boulder, Sprite Material)
            var mats = CreateOrUpdateLanaMaterials();

            // 3. Build Seamless Uncropped Painted Backgrounds (using the 4 official backgrounds)
            BuildSeamlessPaintedBackgrounds(mats);

            // 4. Build Unified Sculpted Planet of Lana Ledges (Single Continuous Grass Lip + Deep Navy-Slate Silhouette)
            BuildSculptedLanaLedges(mats);

            // 5. Refine Zone D River & Wooden Pier (Integrated wooden mooring pilings + Mid-water dark rock islet matching Ref 4)
            OverhaulZoneDRiverAndPier(mats);

            // 6. Populate Authentic Hand-Drawn Hero Trees, Bushes ("cay 1..5"), Crisp Grass Tufts & Moss-Crowned Boulders
            PopulateAuthenticHandDrawnEcosystem(mats);

            // 7. Configure Clean Lighting, Post-Processing & Camera Framing
            ConfigureLightingAndCamera();

            // Save Scene & Assets
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("<color=#40b8a6>[LanaOverhaul]</color> <b>PLANET OF LANA HAND-DRAWN LEVEL DESIGN COMPLETED SUCCESSFULLY!</b>");

            // 8. Capture 4 High-Res 1280x720 Verification Screenshots across the level
            CaptureCinematicViews();
        }

        private static void PurgeAllAIAndMisplacedAssets()
        {
            // A. Clean 04_Foreground completely
            var fgRoot = GameObject.Find("04_Foreground");
            if (fgRoot != null)
            {
                while (fgRoot.transform.childCount > 0)
                {
                    Object.DestroyImmediate(fgRoot.transform.GetChild(0).gameObject);
                }
            }

            // B. Clean 05_Midground of AI trees, black cutout bushes/palms/vines, and previous passes
            string[] mgRemove = new[]
            {
                "Midground_Trees",
                "Midground_Vegetation",
                "Midground_Vines",
                "ClimbObstacle_ArtDressing",
                "Settlement_Props_Life",
                "Opening_Vista_Midground",
                "Canopy_Wall_System",
                "Landmarks_Obstacles",
                "Riverside_Settlement",
                "Lana_HandDrawn_Ecosystem"
            };
            var mgRoot = GameObject.Find("05_Midground");
            if (mgRoot != null)
            {
                foreach (var name in mgRemove)
                {
                    var t = mgRoot.transform.Find(name);
                    if (t != null) Object.DestroyImmediate(t.gameObject);
                }
            }

            // C. Clean 06_Water of cutout water lilies and duplicate stepping stones
            var waterRoot = GameObject.Find("06_Water");
            if (waterRoot != null)
            {
                var waterVeg = waterRoot.transform.Find("Water_Vegetation");
                if (waterVeg != null) Object.DestroyImmediate(waterVeg.gameObject);

                var puddle = waterRoot.transform.Find("Zone_B_ShallowPuddle");
                if (puddle != null)
                {
                    while (puddle.childCount > 0)
                    {
                        Object.DestroyImmediate(puddle.GetChild(0).gameObject);
                    }
                }
            }

            // D. Clean 07_Background of AI clouds, AI mountains, AI mangrove cutouts, and the tree-leaf row
            var bgRoot = GameObject.Find("07_Background");
            if (bgRoot != null)
            {
                while (bgRoot.transform.childCount > 0)
                {
                    Object.DestroyImmediate(bgRoot.transform.GetChild(0).gameObject);
                }
            }

            // E. Clean 09_VFX of harsh rectangle sunshafts / dirty godrays
            var vfxRoot = GameObject.Find("09_VFX");
            if (vfxRoot != null)
            {
                var sunShaft = vfxRoot.transform.Find("SunShaft_ZoneE_Clearing");
                if (sunShaft != null) Object.DestroyImmediate(sunShaft.gameObject);
                var godRays = vfxRoot.transform.Find("GodRays_Canopy");
                if (godRays != null) Object.DestroyImmediate(godRays.gameObject);
            }

            // F. Clean 10_EnvironmentSystems of blurry green brush-blob tufts
            var envRoot = GameObject.Find("10_EnvironmentSystems");
            if (envRoot != null)
            {
                while (envRoot.transform.childCount > 0)
                {
                    Object.DestroyImmediate(envRoot.transform.GetChild(0).gameObject);
                }
            }

            // G. Clean 03_Terrain of procedural geometric flora, legacy walkways, and brown foundation cubes
            var terrainRoot = GameObject.Find("03_Terrain");
            if (terrainRoot != null)
            {
                string[] terrainRemove = new[]
                {
                    "Walkway_60m_LegacyBackup",
                    "Ground_Foliage_Tufts",
                    "Earthen_Underbelly_Foundation",
                    "Zone_C_ClimbObstacle_Ledge",
                    "Lana_Sculpted_Ledges",
                    "Lana_Ground_Flora",
                    "Lana_HandDrawn_Flora"
                };
                foreach (var name in terrainRemove)
                {
                    var t = terrainRoot.transform.Find(name);
                    if (t != null) Object.DestroyImmediate(t.gameObject);
                }

                var pierRoot = terrainRoot.transform.Find("Visual_Dock_Pier_ZoneD");
                if (pierRoot != null)
                {
                    var oldPoles = pierRoot.Find("Lana_Pier_MooringPoles");
                    if (oldPoles != null) Object.DestroyImmediate(oldPoles.gameObject);
                    var oldIslet = pierRoot.Find("Lana_MidWater_RockIslet");
                    if (oldIslet != null) Object.DestroyImmediate(oldIslet.gameObject);
                }

                var segTerrain = terrainRoot.transform.Find("Segmented_Terrain_60m");
                if (segTerrain != null)
                {
                    foreach (var mr in segTerrain.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        mr.enabled = false;
                    }
                }

                var climbPhys = terrainRoot.transform.Find("Zone_C_ClimbObstacle_Physical");
                if (climbPhys != null)
                {
                    var fallenRoots = climbPhys.Find("Visual_FallenLog_Roots");
                    if (fallenRoots != null) Object.DestroyImmediate(fallenRoots.gameObject);

                    foreach (var mr in climbPhys.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        mr.enabled = false;
                    }
                    foreach (var sr in climbPhys.GetComponentsInChildren<SpriteRenderer>(true))
                    {
                        sr.enabled = false;
                    }
                }
            }
        }

        private class LanaMaterialSet
        {
            public Material GrassCapMat;
            public Material RockUnderbellyMat;
            public Material MossyBoulderMat;
            public Material TreeSpriteMat;
            public Material TimberDarkMat;
        }

        private static LanaMaterialSet CreateOrUpdateLanaMaterials()
        {
            var set = new LanaMaterialSet();
            Shader ledgeShader = Shader.Find("HoaVaMau/LanaStylizedLedge");
            if (ledgeShader == null) ledgeShader = Shader.Find("Universal Render Pipeline/Lit");

            // 1. Grass Cap Material (Planet of Lana sunlit lush meadow green -> warm golden-green in Zone E/F)
            set.GrassCapMat = GetOrCreateMat($"{GenAssetsDir}/M_Lana_GrassCap.mat", ledgeShader);
            set.GrassCapMat.SetFloat("_IsGrassCap", 1.0f);
            set.GrassCapMat.SetColor("_GrassTopColor", new Color(0.42f, 0.62f, 0.28f, 1f));     // Lush sunlit meadow green
            set.GrassCapMat.SetColor("_GrassMidColor", new Color(0.26f, 0.43f, 0.19f, 1f));     // Deeper emerald lip
            set.GrassCapMat.SetColor("_GrassGoldTopColor", new Color(0.73f, 0.65f, 0.30f, 1f)); // Warm golden-green top
            set.GrassCapMat.SetColor("_GrassGoldMidColor", new Color(0.48f, 0.44f, 0.20f, 1f)); // Deeper golden-olive lip
            set.GrassCapMat.SetFloat("_GoldenStartX", 43.0f);
            set.GrassCapMat.SetFloat("_GoldenEndX", 50.0f);
            set.GrassCapMat.SetColor("_RockDarkColor", new Color(0.05f, 0.07f, 0.11f, 1f));
            EditorUtility.SetDirty(set.GrassCapMat);

            // 2. Deep Navy-Slate Cross-Section & Cliff Material (matching Reference 1 & Reference 2)
            set.RockUnderbellyMat = GetOrCreateMat($"{GenAssetsDir}/M_Lana_RockUnderbelly.mat", ledgeShader);
            set.RockUnderbellyMat.SetFloat("_IsGrassCap", 0.0f);
            set.RockUnderbellyMat.SetColor("_RockLightColor", new Color(0.22f, 0.28f, 0.36f, 1f));
            set.RockUnderbellyMat.SetColor("_RockMidColor", new Color(0.12f, 0.16f, 0.22f, 1f));
            set.RockUnderbellyMat.SetColor("_RockDarkColor", new Color(0.05f, 0.07f, 0.11f, 1f));
            EditorUtility.SetDirty(set.RockUnderbellyMat);

            // 3. Moss-Crowned Slate Boulder Material (for PF_MossyBoulder — fixes missing material pink shader!)
            set.MossyBoulderMat = GetOrCreateMat($"{GenAssetsDir}/M_Lana_MossyBoulder.mat", ledgeShader);
            set.MossyBoulderMat.SetFloat("_IsGrassCap", 0.5f);
            set.MossyBoulderMat.SetFloat("_GrassNormalThreshold", 0.42f);
            set.MossyBoulderMat.SetColor("_GrassTopColor", new Color(0.42f, 0.62f, 0.28f, 1f));
            set.MossyBoulderMat.SetColor("_GrassMidColor", new Color(0.26f, 0.43f, 0.19f, 1f));
            set.MossyBoulderMat.SetColor("_GrassGoldTopColor", new Color(0.73f, 0.65f, 0.30f, 1f));
            set.MossyBoulderMat.SetColor("_GrassGoldMidColor", new Color(0.48f, 0.44f, 0.20f, 1f));
            set.MossyBoulderMat.SetFloat("_GoldenStartX", 43.0f);
            set.MossyBoulderMat.SetFloat("_GoldenEndX", 50.0f);
            set.MossyBoulderMat.SetColor("_RockLightColor", new Color(0.24f, 0.30f, 0.38f, 1f));
            set.MossyBoulderMat.SetColor("_RockMidColor", new Color(0.15f, 0.20f, 0.27f, 1f));
            set.MossyBoulderMat.SetColor("_RockDarkColor", new Color(0.08f, 0.11f, 0.16f, 1f));
            EditorUtility.SetDirty(set.MossyBoulderMat);

            // Permanently repair PF_MossyBoulder.prefab's missing material reference so it never renders pink
            var boulderPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/PF_MossyBoulder.prefab");
            if (boulderPrefab != null)
            {
                foreach (var mr in boulderPrefab.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (mr.sharedMaterial == null)
                    {
                        mr.sharedMaterial = set.MossyBoulderMat;
                        EditorUtility.SetDirty(mr);
                    }
                }
                EditorUtility.SetDirty(boulderPrefab);
            }

            // 4. Hand-Drawn Tree & Bush Sprite Material (Unity's native built-in Sprites-Default.mat)
            var treeShaderMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Foliage/M_PlanetOfLana_Tree.mat");
            if (treeShaderMat != null)
            {
                treeShaderMat.SetTexture("_MainTex", null);
                EditorUtility.SetDirty(treeShaderMat);
            }
            set.TreeSpriteMat = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");

            // 5. Rustic Dark Timber Material for Pier Mooring Pilings
            set.TimberDarkMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Scene1_CaMau/M_CaMau_TimberDark.mat");
            if (set.TimberDarkMat == null)
            {
                set.TimberDarkMat = GetOrCreateMat($"{GenAssetsDir}/M_Lana_TimberDark.mat", Shader.Find("Universal Render Pipeline/Lit"));
                set.TimberDarkMat.SetColor("_BaseColor", new Color(0.22f, 0.16f, 0.12f, 1f));
            }

            return set;
        }

        private static Material GetOrCreateMat(string path, Shader shader)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (shader != null)
            {
                mat.shader = shader;
            }
            return mat;
        }

        private static Sprite LoadOrEnsureSpriteAsset(string path, Vector2 pivot)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = pivot;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }

            var spr = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (spr != null) return spr;

            var all = AssetDatabase.LoadAllAssetsAtPath(path);
            if (all != null)
            {
                foreach (var obj in all)
                {
                    if (obj is Sprite s) return s;
                }
            }
            return null;
        }

        // =========================================================================================
        // 3. SEAMLESS UNCROPPED PAINTED BACKGROUNDS (4 USER-PROVIDED BACKGROUNDS)
        // =========================================================================================
        private static void BuildSeamlessPaintedBackgrounds(LanaMaterialSet mats)
        {
            var bgRoot = GameObject.Find("07_Background");
            if (bgRoot == null) bgRoot = new GameObject("07_Background");

            var vistaGroup = new GameObject("Lana_Painted_Vistas");
            vistaGroup.transform.SetParent(bgRoot.transform, false);

            Shader bgShader = Shader.Find("HoaVaMau/LanaBackgroundBlend");
            if (bgShader == null) bgShader = Shader.Find("Universal Render Pipeline/Unlit");

            var texDayPlains = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Backgrounds_Lana/BG_Lana_PlainsHills_Day.png");
            var texDayCoastal = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Backgrounds_Lana/BG_Lana_CoastalBay_Day.jpg");
            var texSunsetHills = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Backgrounds_Lana/BG_Lana_HillsFields_Sunset.jpg");
            var texSunsetSea = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Backgrounds_Lana/BG_Lana_SeaHorizon_Sunset.png");

            void SpawnVistaPanel(string name, Texture2D tex, float centerX, float centerY, float z, float height, float leftFeather, float rightFeather, int renderQueueOffset)
            {
                if (tex == null) return;
                float aspect = (float)tex.width / tex.height;
                float width = height * aspect; // 100% EXACT native aspect ratio, zero cropping

                var mat = GetOrCreateMat($"{GenAssetsDir}/M_{name}.mat", bgShader);
                mat.SetTexture("_MainTex", tex);
                mat.SetColor("_Color", Color.white);
                mat.SetFloat("_LeftFeather", leftFeather);
                mat.SetFloat("_RightFeather", rightFeather);
                mat.SetFloat("_BottomFeather", 0.0f);
                mat.renderQueue = 2900 + renderQueueOffset;
                EditorUtility.SetDirty(mat);

                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = name;
                quad.transform.SetParent(vistaGroup.transform, false);
                quad.transform.position = new Vector3(centerX, centerY, z);
                quad.transform.localScale = new Vector3(width, height, 1.0f);

                var mr = quad.GetComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;

                var col = quad.GetComponent<Collider>();
                if (col != null) Object.DestroyImmediate(col);
            }

            // Panel 1 (Zone A - B: Daytime Cumulus Sky & Distant Green Hills/Plains)
            SpawnVistaPanel("Vista_01_PlainsHills_Day", texDayPlains,
                centerX: 2.0f, centerY: 2.40f, z: 9.4f, height: 12.6f,
                leftFeather: 0.0f, rightFeather: 0.0f, renderQueueOffset: 1);

            // Panel 2 (Zone C - D: Coastal Mangrove Bay, Turquoise Water & Sailboats)
            SpawnVistaPanel("Vista_02_CoastalBay_Day", texDayCoastal,
                centerX: 27.0f, centerY: 2.30f, z: 9.2f, height: 12.8f,
                leftFeather: 0.17f, rightFeather: 0.0f, renderQueueOffset: 2);

            // Panel 3 (Zone E: Golden Sunset Hills & Rolling Fields)
            SpawnVistaPanel("Vista_03_HillsFields_Sunset", texSunsetHills,
                centerX: 48.0f, centerY: 2.55f, z: 9.0f, height: 13.0f,
                leftFeather: 0.24f, rightFeather: 0.0f, renderQueueOffset: 3);

            // Panel 4 (Zone F: Golden Sunset Sea Horizon & Distant Boats)
            SpawnVistaPanel("Vista_04_SeaHorizon_Sunset", texSunsetSea,
                centerX: 68.0f, centerY: 2.35f, z: 8.8f, height: 12.6f,
                leftFeather: 0.23f, rightFeather: 0.0f, renderQueueOffset: 4);
        }

        // =========================================================================================
        // 4. UNIFIED SCULPTED PLANET OF LANA LEDGES (SINGLE CONTINUOUS GRASS LIP + DARK CROSS-SECTION)
        // =========================================================================================
        private static float SampleTerrainSurfaceY(float x)
        {
            float baseH = -1.85f;

            // Zone A Right-Side Rocky Knoll (5.2m..10.2m — echoes Reference 1's right rocky rise!)
            if (x >= 5.2f && x < 7.2f)
            {
                float t = (x - 5.2f) / 2.0f;
                baseH = Mathf.Lerp(-1.85f, -1.15f, SmoothCos(t));
            }
            else if (x >= 7.2f && x < 8.8f)
            {
                baseH = -1.15f;
            }
            else if (x >= 8.8f && x < 10.6f)
            {
                float t = (x - 8.8f) / 1.8f;
                baseH = Mathf.Lerp(-1.15f, -1.85f, SmoothCos(t));
            }
            // Zone B Shallow Meadow Dip (10.6m..18.5m)
            else if (x >= 10.6f && x < 13.5f)
            {
                float t = (x - 10.6f) / 2.9f;
                baseH = Mathf.Lerp(-1.85f, -2.10f, SmoothCos(t));
            }
            else if (x >= 13.5f && x < 16.8f)
            {
                baseH = -2.10f;
            }
            else if (x >= 16.8f && x < 18.8f)
            {
                float t = (x - 16.8f) / 2.0f;
                baseH = Mathf.Lerp(-2.10f, -1.85f, SmoothCos(t));
            }
            // Zone C Unified Stepped Rocky Cliff Overlook (18.8m..30.2m — matches Reference 2!)
            else if (x >= 18.8f && x < 21.0f)
            {
                float t = (x - 18.8f) / 2.2f;
                baseH = Mathf.Lerp(-1.85f, -0.35f, SmoothCos(t));
            }
            else if (x >= 21.0f && x < 25.8f)
            {
                baseH = -0.35f; // High rocky cliff overlook plateau on the left of Zone C
            }
            else if (x >= 25.8f && x < 27.2f)
            {
                float t = (x - 25.8f) / 1.4f;
                baseH = Mathf.Lerp(-0.35f, -1.15f, SmoothCos(t));
            }
            else if (x >= 27.2f && x < 28.6f)
            {
                baseH = -1.15f; // Mid terrace step
            }
            else if (x >= 28.6f && x < 30.2f)
            {
                float t = (x - 28.6f) / 1.6f;
                baseH = Mathf.Lerp(-1.15f, -1.85f, SmoothCos(t));
            }
            // Zone D West Bank slope into water (31.8m..33.8m)
            else if (x >= 31.8f && x < 33.8f)
            {
                float t = (x - 31.8f) / 2.0f;
                baseH = Mathf.Lerp(-1.85f, -2.35f, SmoothCos(t));
            }
            else if (x >= 33.8f && x < 40.2f)
            {
                baseH = -2.35f;
            }
            else if (x >= 40.2f && x < 42.2f)
            {
                float t = (x - 40.2f) / 2.0f;
                baseH = Mathf.Lerp(-2.35f, -1.85f, SmoothCos(t));
            }
            // Zone E & F Golden Coastal Spit (42.2m..65m — matches Reference 3!)
            else if (x >= 42.2f && x < 45.0f)
            {
                float t = (x - 42.2f) / 2.8f;
                baseH = Mathf.Lerp(-1.85f, -1.70f, SmoothCos(t));
            }
            else if (x >= 45.0f && x < 55.5f)
            {
                baseH = -1.70f;
            }
            else if (x >= 55.5f && x < 57.5f)
            {
                float t = (x - 55.5f) / 2.0f;
                baseH = Mathf.Lerp(-1.70f, -1.35f, SmoothCos(t)); // Gentle rise under Zone F Hero Tree rocks
            }
            else if (x >= 57.5f && x < 61.0f)
            {
                baseH = -1.35f;
            }
            else if (x >= 61.0f)
            {
                float t = Mathf.Clamp01((x - 61.0f) / 2.5f);
                baseH = Mathf.Lerp(-1.35f, -1.75f, SmoothCos(t));
            }

            if (x < 31.5f || x > 42.5f)
            {
                baseH += Mathf.Sin(x * 0.55f) * 0.03f + Mathf.Cos(x * 1.35f) * 0.018f;
            }
            return baseH;
        }

        private static float SmoothCos(float t)
        {
            t = Mathf.Clamp01(t);
            return 0.5f - 0.5f * Mathf.Cos(t * Mathf.PI);
        }

        private static void BuildSculptedLanaLedges(LanaMaterialSet mats)
        {
            var terrainRoot = GameObject.Find("03_Terrain");
            if (terrainRoot == null) terrainRoot = new GameObject("03_Terrain");

            var ledgesGroup = new GameObject("Lana_Sculpted_Ledges");
            ledgesGroup.transform.SetParent(terrainRoot.transform, false);

            // 1. Unified West Ledge (-12m to 32.3m: Zones A, B, C — single continuous grass lip & dark cross-section!)
            CreateLanaLedgeSection(ledgesGroup.transform, "Lana_Ledge_West_ZonesABC",
                startX: -12.0f, endX: 32.3f, segmentsX: 220,
                frontZ: -1.75f, backZ: 2.15f, bottomY: -9.5f,
                SampleTerrainSurfaceY, mats, includeGrassCap: true);

            // 2. Unified East Ledge (41.7m to 72.0m: Zones E, F)
            CreateLanaLedgeSection(ledgesGroup.transform, "Lana_Ledge_East_ZonesEF",
                startX: 41.7f, endX: 72.0f, segmentsX: 140,
                frontZ: -1.75f, backZ: 2.15f, bottomY: -9.5f,
                SampleTerrainSurfaceY, mats, includeGrassCap: true);

            // 3. Zone D Seamless Under-Water Silhouette Base (31.5m to 42.5m, flush with water surface Y = -2.23f)
            CreateLanaLedgeSection(ledgesGroup.transform, "Lana_Riverbed_Cutaway_ZoneD",
                startX: 31.5f, endX: 42.5f, segmentsX: 40,
                frontZ: -1.75f, backZ: 2.15f, bottomY: -9.5f,
                x => -2.06f, mats, includeGrassCap: false);
        }

        private static void CreateLanaLedgeSection(
            Transform parent, string name,
            float startX, float endX, int segmentsX,
            float frontZ, float backZ, float bottomY,
            System.Func<float, float> heightFunc,
            LanaMaterialSet mats,
            bool includeGrassCap = true)
        {
            var sectionRoot = new GameObject(name);
            sectionRoot.transform.SetParent(parent, false);

            string meshDir = $"{GenAssetsDir}/GeneratedMeshes";
            if (!Directory.Exists(meshDir)) Directory.CreateDirectory(meshDir);

            // --- A. DEEP NAVY-SLATE UNDERBELLY / CLIFF MESH ---
            Mesh rockMesh = BuildChiseledRockMesh($"{name}_RockMesh", startX, endX, segmentsX, frontZ, backZ, bottomY, heightFunc);
            AssetDatabase.CreateAsset(rockMesh, $"{meshDir}/{name}_RockMesh.asset");

            var rockGO = new GameObject($"{name}_RockUnderbelly");
            rockGO.transform.SetParent(sectionRoot.transform, false);
            rockGO.AddComponent<MeshFilter>().sharedMesh = rockMesh;
            var rockMR = rockGO.AddComponent<MeshRenderer>();
            rockMR.sharedMaterial = mats.RockUnderbellyMat;
            rockMR.shadowCastingMode = ShadowCastingMode.On;
            rockMR.receiveShadows = true;

            // --- B. OVERHANGING LUSH GRASS TURF CAP MESH ---
            if (includeGrassCap)
            {
                Mesh grassMesh = BuildOverhangingGrassCapMesh($"{name}_GrassMesh", startX, endX, segmentsX, frontZ - 0.18f, backZ + 0.10f, heightFunc);
                AssetDatabase.CreateAsset(grassMesh, $"{meshDir}/{name}_GrassMesh.asset");

                var grassGO = new GameObject($"{name}_GrassTurfCap");
                grassGO.transform.SetParent(sectionRoot.transform, false);
                grassGO.AddComponent<MeshFilter>().sharedMesh = grassMesh;
                var grassMR = grassGO.AddComponent<MeshRenderer>();
                grassMR.sharedMaterial = mats.GrassCapMat;
                grassMR.shadowCastingMode = ShadowCastingMode.On;
                grassMR.receiveShadows = true;
            }
        }

        private static Mesh BuildChiseledRockMesh(
            string meshName, float startX, float endX, int segX,
            float frontZ, float backZ, float bottomY,
            System.Func<float, float> heightFunc)
        {
            int rowsY = 6;
            int cols = segX + 1;

            var verts = new List<Vector3>();
            var colors = new List<Color>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            for (int r = 0; r <= rowsY; r++)
            {
                float v = (float)r / rowsY;
                for (int c = 0; c < cols; c++)
                {
                    float u = (float)c / segX;
                    float x = Mathf.Lerp(startX, endX, u);
                    float surfY = heightFunc(x);
                    float topY = surfY - 0.18f;
                    float y = Mathf.Lerp(topY, bottomY, v);

                    float facet = Mathf.Sin(x * 1.45f + r * 1.9f) * 0.10f
                                + Mathf.Cos(x * 3.1f - r * 2.4f) * 0.05f;
                    float zOffset = facet * Mathf.Sin(v * Mathf.PI);
                    float z = frontZ + zOffset;

                    if (c == 0 || c == segX) z += 0.25f;

                    verts.Add(new Vector3(x, y, z));

                    // Mark raised rocky cliffs (surfY > -1.45f) in blue channel for subtle slate cliff strata
                    float cliffFlag = Mathf.InverseLerp(-1.65f, -0.45f, surfY) * Mathf.Clamp01(1.0f - v * 1.8f);
                    float lipAO = (r == 0) ? 0.45f : ((r == 1) ? 0.85f : 1.0f);
                    float botAO = Mathf.Lerp(1.0f, 0.55f, Mathf.Pow(v, 1.5f));
                    colors.Add(new Color(0f, lipAO * botAO, cliffFlag, 1f));
                    uvs.Add(new Vector2(u * 10f, v * 2f));
                }
            }

            for (int r = 0; r < rowsY; r++)
            {
                for (int c = 0; c < segX; c++)
                {
                    int i0 = r * cols + c;
                    int i1 = i0 + 1;
                    int i2 = (r + 1) * cols + c;
                    int i3 = i2 + 1;

                    tris.Add(i0); tris.Add(i1); tris.Add(i2);
                    tris.Add(i1); tris.Add(i3); tris.Add(i2);
                }
            }

            // Left & Right Side Walls
            float[] sideXs = new[] { startX, endX };
            for (int s = 0; s < 2; s++)
            {
                float sx = sideXs[s];
                float topY = heightFunc(sx) - 0.15f;
                int bIdx = verts.Count;
                verts.Add(new Vector3(sx, topY, frontZ + 0.25f));
                verts.Add(new Vector3(sx, topY, backZ));
                verts.Add(new Vector3(sx, bottomY, frontZ + 0.25f));
                verts.Add(new Vector3(sx, bottomY, backZ));
                for (int k = 0; k < 4; k++)
                {
                    colors.Add(new Color(0f, 0.80f, 0f, 1f));
                    uvs.Add(Vector2.zero);
                }
                if (s == 0)
                {
                    tris.Add(bIdx + 0); tris.Add(bIdx + 2); tris.Add(bIdx + 1);
                    tris.Add(bIdx + 1); tris.Add(bIdx + 2); tris.Add(bIdx + 3);
                }
                else
                {
                    tris.Add(bIdx + 0); tris.Add(bIdx + 1); tris.Add(bIdx + 2);
                    tris.Add(bIdx + 1); tris.Add(bIdx + 3); tris.Add(bIdx + 2);
                }
            }

            // Top Cap
            int topStartIdx = verts.Count;
            for (int c = 0; c < cols; c++)
            {
                float u = (float)c / segX;
                float x = Mathf.Lerp(startX, endX, u);
                float topY = heightFunc(x) - 0.18f;
                verts.Add(new Vector3(x, topY, frontZ));
                verts.Add(new Vector3(x, topY, backZ));
                colors.Add(new Color(0f, 0.88f, 0f, 1f));
                colors.Add(new Color(0f, 0.88f, 0f, 1f));
                uvs.Add(new Vector2(u, 0f));
                uvs.Add(new Vector2(u, 1f));
            }
            for (int c = 0; c < segX; c++)
            {
                int i0 = topStartIdx + c * 2;
                int i1 = i0 + 1;
                int i2 = i0 + 2;
                int i3 = i0 + 3;
                tris.Add(i0); tris.Add(i1); tris.Add(i2);
                tris.Add(i2); tris.Add(i1); tris.Add(i3);
            }

            Mesh m = new Mesh { name = meshName };
            m.SetVertices(verts);
            m.SetColors(colors);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        private static Mesh BuildOverhangingGrassCapMesh(
            string meshName, float startX, float endX, int segX,
            float frontZ, float backZ,
            System.Func<float, float> heightFunc)
        {
            int profileCount = 6;
            int cols = segX + 1;

            var verts = new List<Vector3>();
            var colors = new List<Color>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            for (int c = 0; c < cols; c++)
            {
                float u = (float)c / segX;
                float x = Mathf.Lerp(startX, endX, u);
                float y = heightFunc(x);

                float scallop = Mathf.Sin(x * 2.4f) * 0.055f + Mathf.Cos(x * 5.2f) * 0.032f;

                verts.Add(new Vector3(x, y - 0.22f, backZ));
                verts.Add(new Vector3(x, y + 0.04f, backZ));
                verts.Add(new Vector3(x, y + 0.01f, 0.0f));
                verts.Add(new Vector3(x, y + 0.03f + scallop * 0.20f, frontZ + 0.16f));
                verts.Add(new Vector3(x, y - 0.11f + scallop * 0.60f, frontZ));
                verts.Add(new Vector3(x, y - 0.27f + scallop, frontZ + 0.15f));

                colors.Add(new Color(0.65f, 1f, 0f, 1f));
                colors.Add(new Color(1.00f, 1f, 0f, 1f));
                colors.Add(new Color(0.96f, 1f, 0f, 1f));
                colors.Add(new Color(0.92f, 1f, 0f, 1f));
                colors.Add(new Color(0.62f, 1f, 0f, 1f));
                colors.Add(new Color(0.30f, 1f, 0f, 1f));

                for (int p = 0; p < profileCount; p++)
                {
                    uvs.Add(new Vector2(u * 12f, (float)p / (profileCount - 1)));
                }
            }

            for (int c = 0; c < segX; c++)
            {
                int baseC = c * profileCount;
                int nextC = (c + 1) * profileCount;
                for (int p = 0; p < profileCount - 1; p++)
                {
                    int i0 = baseC + p;
                    int i1 = baseC + p + 1;
                    int i2 = nextC + p;
                    int i3 = nextC + p + 1;

                    tris.Add(i0); tris.Add(i2); tris.Add(i1);
                    tris.Add(i1); tris.Add(i2); tris.Add(i3);
                }
            }

            Mesh m = new Mesh { name = meshName };
            m.SetVertices(verts);
            m.SetColors(colors);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        // =========================================================================================
        // 5. REFINE ZONE D RIVER & WOODEN PIER (MATCHING PLANET OF LANA REFERENCE 4)
        // =========================================================================================
        private static void OverhaulZoneDRiverAndPier(LanaMaterialSet mats)
        {
            var waterGroup = GameObject.Find("06_Water/River_Water_System");
            if (waterGroup != null)
            {
                var bw = waterGroup.transform.Find("Bank_West_Slope");
                if (bw != null) Object.DestroyImmediate(bw.gameObject);
                var be = waterGroup.transform.Find("Bank_East_Slope");
                if (be != null) Object.DestroyImmediate(be.gameObject);

                var water = waterGroup.transform.Find("River_Water_Surface");
                if (water != null)
                {
                    water.position = new Vector3(37.0f, -2.22f, -0.05f);
                    water.localScale = new Vector3(1.08f, 1.0f, 0.34f);
                }
            }

            var riverBed = GameObject.Find("03_Terrain/RiverBed_SandyBottom_ZoneD");
            if (riverBed != null)
            {
                riverBed.transform.position = new Vector3(37.0f, -3.50f, 0.25f);
                riverBed.transform.localScale = new Vector3(11.0f, 1.0f, 3.0f);
                var mr = riverBed.GetComponent<MeshRenderer>();
                if (mr != null) mr.enabled = false;
            }

            var pierRoot = GameObject.Find("03_Terrain/Visual_Dock_Pier_ZoneD");
            if (pierRoot != null)
            {
                var r1 = pierRoot.transform.Find("RiverRock_ZoneD_1");
                if (r1 != null) Object.DestroyImmediate(r1.gameObject);
                var r2 = pierRoot.transform.Find("RiverRock_ZoneD_2");
                if (r2 != null) Object.DestroyImmediate(r2.gameObject);

                // Extend pier pilings upward above the boardwalk at natural irregular heights
                // aligned directly with the pier's existing stilts so they form continuous wooden pilings!
                var polesGroup = new GameObject("Lana_Pier_MooringPoles");
                polesGroup.transform.SetParent(pierRoot.transform, false);

                float[] topHeightsBack  = new[] { -1.85f, -0.45f, -0.15f, -0.55f, -0.05f, -0.35f, -1.85f };
                float[] topHeightsFront = new[] { -1.85f, -0.75f, -1.85f, -0.30f, -1.85f, -0.60f, -1.85f };
                float[] tilts           = new[] {  0.0f,   1.8f,  -2.2f,   1.5f,  -1.8f,   2.1f,   0.0f  };

                for (int i = 1; i <= 5; i++)
                {
                    var backStilt = pierRoot.transform.Find($"DockStilt_Back_{i}");
                    if (backStilt != null && topHeightsBack[i] > -1.70f)
                    {
                        Vector3 sPos = backStilt.position;
                        Material stiltMat = backStilt.GetComponent<MeshRenderer>()?.sharedMaterial ?? mats.TimberDarkMat;
                        float bottomY = -2.25f;
                        float topY = topHeightsBack[i];
                        float h = topY - bottomY;

                        var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        post.name = $"PierPiling_Back_{i}";
                        post.transform.SetParent(polesGroup.transform, true);
                        post.transform.position = new Vector3(sPos.x, (topY + bottomY) * 0.5f, sPos.z);
                        post.transform.rotation = Quaternion.Euler(0f, 0f, tilts[i]);
                        post.transform.localScale = new Vector3(backStilt.localScale.x, h * 0.5f, backStilt.localScale.z);
                        var mr = post.GetComponent<MeshRenderer>();
                        if (mr != null && stiltMat != null) mr.sharedMaterial = stiltMat;
                        var col = post.GetComponent<Collider>();
                        if (col != null) Object.DestroyImmediate(col);
                    }

                    var frontStilt = pierRoot.transform.Find($"DockStilt_Front_{i}");
                    if (frontStilt != null && topHeightsFront[i] > -1.70f)
                    {
                        Vector3 sPos = frontStilt.position;
                        Material stiltMat = frontStilt.GetComponent<MeshRenderer>()?.sharedMaterial ?? mats.TimberDarkMat;
                        float bottomY = -2.25f;
                        float topY = topHeightsFront[i];
                        float h = topY - bottomY;

                        var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        post.name = $"PierPiling_Front_{i}";
                        post.transform.SetParent(polesGroup.transform, true);
                        post.transform.position = new Vector3(sPos.x, (topY + bottomY) * 0.5f, sPos.z);
                        post.transform.rotation = Quaternion.Euler(0f, 0f, -tilts[i] * 0.8f);
                        post.transform.localScale = new Vector3(frontStilt.localScale.x, h * 0.5f, frontStilt.localScale.z);
                        var mr = post.GetComponent<MeshRenderer>();
                        if (mr != null && stiltMat != null) mr.sharedMaterial = stiltMat;
                        var col = post.GetComponent<Collider>();
                        if (col != null) Object.DestroyImmediate(col);
                    }
                }
            }
        }

        // =========================================================================================
        // 6. POPULATE AUTHENTIC HAND-DRAWN HERO TREES, BUSHES ("cay 1..5"), CRISP GRASS & BOULDERS
        // =========================================================================================
        private static void PopulateAuthenticHandDrawnEcosystem(LanaMaterialSet mats)
        {
            var mgRoot = GameObject.Find("05_Midground");
            if (mgRoot == null) mgRoot = new GameObject("05_Midground");

            var ecoRoot = new GameObject("Lana_HandDrawn_Ecosystem");
            ecoRoot.transform.SetParent(mgRoot.transform, false);

            var treesGroup = new GameObject("HandDrawn_HeroTrees");
            treesGroup.transform.SetParent(ecoRoot.transform, false);

            var rocksGroup = new GameObject("Sculpted_MossyBoulders");
            rocksGroup.transform.SetParent(ecoRoot.transform, false);

            var bushesGroup = new GameObject("HandDrawn_Bushes_Cay1To5");
            bushesGroup.transform.SetParent(ecoRoot.transform, false);

            var grassGroup = new GameObject("HandDrawn_MeadowGrass");
            grassGroup.transform.SetParent(ecoRoot.transform, false);

            // Load clean hand-drawn tree prefabs & rock prefab
            var prefabCurvedTreeV2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Trees/PF_Curved_Hero_Tree_V2.prefab");
            var prefabGocCay2Tree  = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Trees/PF_Tree_GocCay2_Artistic.prefab");
            var prefabMossyBoulder = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/PF_MossyBoulder.prefab");

            // Load 100% fringe-free hand-drawn tree & bush sprites
            Sprite sprGocCay2       = LoadOrEnsureSpriteAsset("Assets/Textures/Tree/goc cay 2.png", new Vector2(0.5f, 0.5f));
            Sprite sprSau1          = LoadOrEnsureSpriteAsset("Assets/Textures/Tree/sau 1.png", new Vector2(0.5f, 0.5f));
            Sprite sprTrung1        = LoadOrEnsureSpriteAsset("Assets/Textures/Tree/trung 1.png", new Vector2(0.5f, 0.5f));

            Sprite sprWindTrunk     = LoadOrEnsureSpriteAsset("Assets/Textures/Tree/Tree_Windblown_Trunk.png", new Vector2(0.5f, 0.1f));
            Sprite sprWindBack      = LoadOrEnsureSpriteAsset("Assets/Textures/Tree/Tree_Windblown_BackLeaves.png", new Vector2(0.5f, 0.5f));
            Sprite sprWindMid       = LoadOrEnsureSpriteAsset("Assets/Textures/Tree/Tree_Windblown_MidLeaves.png", new Vector2(0.5f, 0.5f));
            Sprite sprWindFront     = LoadOrEnsureSpriteAsset("Assets/Textures/Tree/Tree_Windblown_FrontLeaves.png", new Vector2(0.5f, 0.5f));

            Sprite[] bushSprites = new[]
            {
                LoadOrEnsureSpriteAsset("Assets/Textures/Tree/cay 1.png", new Vector2(0.5f, 0.5f)),
                LoadOrEnsureSpriteAsset("Assets/Textures/Tree/cay 2.png", new Vector2(0.5f, 0.5f)),
                LoadOrEnsureSpriteAsset("Assets/Textures/Tree/cay 3.png", new Vector2(0.5f, 0.5f)),
                LoadOrEnsureSpriteAsset("Assets/Textures/Tree/cay 4.png", new Vector2(0.5f, 0.5f)),
                LoadOrEnsureSpriteAsset("Assets/Textures/Tree/cay 5.png", new Vector2(0.5f, 0.5f))
            };

            // Load crisp grass tuft sprites (avoiding giant blurry olive smudges)
            Sprite sprMeadowGrassTuft = LoadOrEnsureSpriteAsset("Assets/Textures/Grass/meadow_grass_tuft.png", new Vector2(0.5f, 0.1f));
            Sprite sprRealGrassTuft   = LoadOrEnsureSpriteAsset("Assets/Textures/Grass/real_grass_tuft.png", new Vector2(0.5f, 0.1f));
            Sprite sprFoliageTuft     = LoadOrEnsureSpriteAsset("Assets/Textures/Grass/foliage_tuft.png", new Vector2(0.5f, 0.05f));

            void AddSway(GameObject go, float maxAngle, float speed, float phase = 0f, float breathing = 0.012f)
            {
                var sway = go.AddComponent<HoaVaMau.Gameplay.PlanetOfLanaFoliageSway>();
                var so = new SerializedObject(sway);
                so.FindProperty("maxRotationAngle").floatValue = maxAngle;
                so.FindProperty("swaySpeed").floatValue = speed;
                so.FindProperty("useWorldPositionOffset").boolValue = true;
                so.FindProperty("manualPhaseOffset").floatValue = phase;
                so.FindProperty("harmonicNoise").floatValue = 0.35f;
                so.FindProperty("scaleBreathing").floatValue = breathing;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // --- Helper 1: Spawn Lush Multi-Canopy Hand-Drawn Hero Tree (goc cay 2 + sau 1 + trung 1 + cay 5/3/4, 0% white fringe!) ---
            GameObject SpawnLushHandDrawnHeroTree(string name, Vector3 pos, float scale, bool flipX, Color foliageTint)
            {
                var root = new GameObject(name);
                root.transform.SetParent(treesGroup.transform, false);
                root.transform.position = pos;
                root.transform.localScale = new Vector3(flipX ? -scale : scale, scale, 1.0f);

                // Trunk (goc cay 2.png)
                var trunkGO = new GameObject("Trunk_GocCay2");
                trunkGO.transform.SetParent(root.transform, false);
                trunkGO.transform.localPosition = new Vector3(0f, 2.55f, 0f);
                var tSr = trunkGO.AddComponent<SpriteRenderer>();
                tSr.sprite = sprGocCay2;
                if (mats.TreeSpriteMat != null) tSr.sharedMaterial = mats.TreeSpriteMat;
                tSr.sortingOrder = 10;
                AddSway(trunkGO, 0.6f, 1.1f, 0f, 0.003f);

                // Canopy Pivot
                var canopyPivot = new GameObject("Canopy_Root");
                canopyPivot.transform.SetParent(trunkGO.transform, false);
                canopyPivot.transform.localPosition = new Vector3(0.05f, 1.35f, 0f);
                AddSway(canopyPivot, 1.5f, 1.3f, 0.4f, 0.008f);

                void AddCanopyLayer(string lName, Sprite spr, Vector3 lPos, Vector2 lScale, Color col, int order, float swayAngle, float phase)
                {
                    if (spr == null) return;
                    var lGO = new GameObject(lName);
                    lGO.transform.SetParent(canopyPivot.transform, false);
                    lGO.transform.localPosition = lPos;
                    lGO.transform.localScale = new Vector3(lScale.x, lScale.y, 1f);
                    var sr = lGO.AddComponent<SpriteRenderer>();
                    sr.sprite = spr;
                    if (mats.TreeSpriteMat != null) sr.sharedMaterial = mats.TreeSpriteMat;
                    sr.color = col;
                    sr.sortingOrder = order;
                    AddSway(lGO, swayAngle, 1.55f, phase, 0.012f);
                }

                // Deep shadow back canopy (sau 1 + cay 5)
                AddCanopyLayer("Canopy_Back_Shadow", sprSau1, new Vector3(-0.15f, -0.25f, 0.04f), new Vector2(1.05f, 1.05f), foliageTint * new Color(0.78f, 0.85f, 0.78f, 1f), 9, 1.8f, 0.2f);
                AddCanopyLayer("Canopy_Mid_Body", bushSprites[4], new Vector3(0.05f, 0.35f, 0.01f), new Vector2(1.12f, 1.10f), foliageTint * new Color(0.92f, 0.96f, 0.90f, 1f), 11, 2.1f, 0.9f);
                AddCanopyLayer("Canopy_Upper_Crown", bushSprites[2], new Vector3(-0.10f, 1.05f, -0.01f), new Vector2(0.95f, 0.92f), foliageTint, 12, 2.4f, 1.6f);
                AddCanopyLayer("Canopy_Front_Highlight", bushSprites[3], new Vector3(0.32f, 0.55f, -0.03f), new Vector2(0.88f, 0.85f), foliageTint, 14, 2.6f, 2.2f);

                return root;
            }

            // --- Helper 2: Spawn Windblown Hand-Drawn Hero Tree (Tree_Windblown_Trunk + Back/Mid/FrontLeaves) ---
            GameObject SpawnWindblownHeroTree(string name, Vector3 pos, float scale, bool flipX, Color foliageTint)
            {
                var root = new GameObject(name);
                root.transform.SetParent(treesGroup.transform, false);
                root.transform.position = pos;
                root.transform.localScale = new Vector3(flipX ? -scale : scale, scale, 1.0f);

                var trunkGO = new GameObject("Windblown_Trunk");
                trunkGO.transform.SetParent(root.transform, false);
                trunkGO.transform.localPosition = new Vector3(0f, 1.85f, 0f);
                var tSr = trunkGO.AddComponent<SpriteRenderer>();
                tSr.sprite = sprWindTrunk;
                if (mats.TreeSpriteMat != null) tSr.sharedMaterial = mats.TreeSpriteMat;
                tSr.sortingOrder = 10;
                AddSway(trunkGO, 0.7f, 1.15f, 0f, 0.003f);

                void AddWindLayer(string lName, Sprite spr, Vector3 lPos, Color col, int order, float swayAngle, float phase)
                {
                    if (spr == null) return;
                    var lGO = new GameObject(lName);
                    lGO.transform.SetParent(trunkGO.transform, false);
                    lGO.transform.localPosition = lPos;
                    var sr = lGO.AddComponent<SpriteRenderer>();
                    sr.sprite = spr;
                    if (mats.TreeSpriteMat != null) sr.sharedMaterial = mats.TreeSpriteMat;
                    sr.color = col;
                    sr.sortingOrder = order;
                    AddSway(lGO, swayAngle, 1.5f, phase, 0.012f);
                }

                AddWindLayer("Windblown_BackLeaves",  sprWindBack,  new Vector3(0.05f, 1.15f,  0.03f), foliageTint * new Color(0.85f, 0.90f, 0.85f, 1f),  9, 1.8f, 0.3f);
                AddWindLayer("Windblown_MidLeaves",   sprWindMid,   new Vector3(0.08f, 1.25f, -0.01f), foliageTint * new Color(0.94f, 0.98f, 0.92f, 1f), 11, 2.2f, 1.1f);
                AddWindLayer("Windblown_FrontLeaves", sprWindFront, new Vector3(0.12f, 1.32f, -0.03f), foliageTint, 13, 2.5f, 1.9f);

                return root;
            }

            // --- Helper 3: Spawn PF_Curved_Hero_Tree_V2 ---
            GameObject SpawnCurvedHeroTreeV2(string name, Vector3 pos, float scale, bool flipX, Color foliageTint)
            {
                if (prefabCurvedTreeV2 == null) return null;
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefabCurvedTreeV2, treesGroup.transform);
                inst.name = name;
                inst.transform.position = pos;
                inst.transform.localScale = new Vector3(flipX ? -scale : scale, scale, 1.0f);

                foreach (var sr in inst.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (mats.TreeSpriteMat != null) sr.sharedMaterial = mats.TreeSpriteMat;
                    if (sr.gameObject.name.Contains("Canopy"))
                    {
                        sr.color = foliageTint;
                    }
                }
                return inst;
            }

            // --- Helper 4: Spawn PF_Tree_GocCay2_Artistic ---
            GameObject SpawnGocCay2Tree(string name, Vector3 pos, float scale, bool flipX, Color foliageTint)
            {
                if (prefabGocCay2Tree == null) return null;
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefabGocCay2Tree, treesGroup.transform);
                inst.name = name;
                inst.transform.position = pos;
                inst.transform.localScale = new Vector3(flipX ? -scale : scale, scale, 1.0f);

                foreach (var sr in inst.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (mats.TreeSpriteMat != null) sr.sharedMaterial = mats.TreeSpriteMat;
                    if (sr.gameObject.name.Contains("Canopy"))
                    {
                        sr.color = foliageTint;
                    }
                }
                return inst;
            }

            // --- Helper 5: Spawn Hand-Drawn Bush ("cay 1..5") ---
            void SpawnHandDrawnBush(string name, int bushIdx, Vector3 pos, Vector2 scale, Color tint, int sortingOrder = 12, bool flipX = false)
            {
                if (bushSprites == null || bushSprites.Length == 0) return;
                Sprite spr = bushSprites[Mathf.Abs(bushIdx) % bushSprites.Length];
                if (spr == null) return;

                var go = new GameObject(name);
                go.transform.SetParent(bushesGroup.transform, false);
                go.transform.position = pos;
                go.transform.localScale = new Vector3(flipX ? -scale.x : scale.x, scale.y, 1.0f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = spr;
                if (mats.TreeSpriteMat != null) sr.sharedMaterial = mats.TreeSpriteMat;
                sr.color = tint;
                sr.sortingOrder = sortingOrder;

                AddSway(go, 2.0f, 1.85f, 0f, 0.014f);
            }

            // --- Helper 6: Spawn Crisp Ankle-High Grass Tuft (meadow_grass_tuft / real_grass_tuft / foliage_tuft) ---
            void SpawnCrispGrassTuft(string name, Sprite spr, Vector3 pos, Vector2 scale, Color tint, int sortingOrder = 14, bool flipX = false, float rotZ = 0f)
            {
                if (spr == null) return;

                var go = new GameObject(name);
                go.transform.SetParent(grassGroup.transform, false);
                go.transform.position = pos;
                go.transform.localRotation = Quaternion.Euler(0f, 0f, rotZ);
                go.transform.localScale = new Vector3(flipX ? -scale.x : scale.x, scale.y, 1.0f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = spr;
                if (mats.TreeSpriteMat != null) sr.sharedMaterial = mats.TreeSpriteMat;
                sr.color = tint;
                sr.sortingOrder = sortingOrder;

                AddSway(go, 3.0f, 2.1f, 0f, 0.012f);
            }

            // --- Helper 7: Spawn Moss-Crowned Slate Boulder (PF_MossyBoulder with M_Lana_MossyBoulder.mat!) ---
            GameObject SpawnMossyBoulder(string name, Vector3 pos, Vector3 scale, float rotY, bool enableCollider = false)
            {
                if (prefabMossyBoulder == null) return null;
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefabMossyBoulder, rocksGroup.transform);
                inst.name = name;
                inst.transform.position = pos;
                inst.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
                inst.transform.localScale = scale;

                foreach (var mr in inst.GetComponentsInChildren<MeshRenderer>(true))
                {
                    mr.sharedMaterial = mats.MossyBoulderMat;
                    mr.shadowCastingMode = ShadowCastingMode.On;
                    mr.receiveShadows = true;
                }
                foreach (var col in inst.GetComponentsInChildren<Collider>(true))
                {
                    col.enabled = enableCollider;
                }
                return inst;
            }

            Color dayFoliageTint    = new Color(0.95f, 1.00f, 0.92f, 1f);
            Color bushLightGreen    = new Color(0.88f, 0.98f, 0.80f, 1f);
            Color bushMidGreen      = new Color(0.72f, 0.88f, 0.66f, 1f);
            Color sunsetFoliageTint = new Color(1.00f, 0.95f, 0.78f, 1f);
            Color sunsetBushLight   = new Color(0.96f, 0.90f, 0.66f, 1f);
            Color sunsetBushMid     = new Color(0.80f, 0.76f, 0.52f, 1f);

            // =====================================================================================
            // ZONE A & B (-6m to 18m): MEADOW, HERO TREES & RIGHT ROCKY OUTCROP (Reference 1 & Scene_Tree_Test)
            // =====================================================================================
            // 1. Left Entrance Framing Grove (-5.5m to -1.5m)
            SpawnLushHandDrawnHeroTree("ZoneA_HeroTree_LeftMain", new Vector3(-3.2f, SampleTerrainSurfaceY(-3.2f) - 0.25f, 1.45f), 0.88f, false, dayFoliageTint);
            SpawnCurvedHeroTreeV2("ZoneA_CurvedTree_LeftBack",    new Vector3(-5.4f, SampleTerrainSurfaceY(-5.4f) + 0.10f, 1.85f), 0.58f, true, bushMidGreen);

            SpawnMossyBoulder("ZoneA_LeftBoulder_Main", new Vector3(-2.4f, SampleTerrainSurfaceY(-2.4f) - 0.02f, 1.18f), new Vector3(1.35f, 0.65f, 1.15f), 25f);
            SpawnMossyBoulder("ZoneA_LeftBoulder_Sub",  new Vector3(-1.4f, SampleTerrainSurfaceY(-1.4f) - 0.02f, 1.25f), new Vector3(0.85f, 0.42f, 0.75f), 110f);

            SpawnHandDrawnBush("ZoneA_LeftBush_1", 4, new Vector3(-3.8f, SampleTerrainSurfaceY(-3.8f) + 0.35f, 1.20f), new Vector2(0.42f, 0.38f), bushLightGreen, 13);
            SpawnHandDrawnBush("ZoneA_LeftBush_2", 2, new Vector3(-2.1f, SampleTerrainSurfaceY(-2.1f) + 0.30f, 1.30f), new Vector2(0.36f, 0.32f), bushMidGreen, 11);
            SpawnHandDrawnBush("ZoneA_LeftBush_3", 0, new Vector3(-1.1f, SampleTerrainSurfaceY(-1.1f) + 0.26f, 1.38f), new Vector2(0.30f, 0.26f), bushLightGreen, 12);

            // 2. Open Meadow Low Mossy Stones (-0.5m to 5.0m — keeping sky & Boy silhouette crystal clear like Ref 1!)
            SpawnMossyBoulder("ZoneA_MeadowStone_1", new Vector3(1.1f, SampleTerrainSurfaceY(1.1f) - 0.02f, 1.32f), new Vector3(0.75f, 0.36f, 0.65f), 40f);
            SpawnMossyBoulder("ZoneA_MeadowStone_2", new Vector3(3.8f, SampleTerrainSurfaceY(3.8f) - 0.02f, 1.36f), new Vector3(0.90f, 0.42f, 0.75f), 155f);
            SpawnHandDrawnBush("ZoneA_MeadowBush_1", 1, new Vector3(4.3f, SampleTerrainSurfaceY(4.3f) + 0.25f, 1.48f), new Vector2(0.28f, 0.25f), bushLightGreen, 9);

            // 3. Right Side Rocky Outcrop & Tree (6.0m to 9.5m — matching Ref 1's right rocky rise!)
            SpawnMossyBoulder("ZoneA_RightOutcrop_Base",  new Vector3(6.6f, SampleTerrainSurfaceY(6.6f) - 0.02f, 1.35f), new Vector3(1.75f, 0.72f, 1.25f), 195f);
            SpawnMossyBoulder("ZoneA_RightOutcrop_Upper", new Vector3(7.8f, SampleTerrainSurfaceY(7.8f) - 0.02f, 1.45f), new Vector3(1.95f, 0.85f, 1.35f), 15f);
            SpawnGocCay2Tree("ZoneA_RightOutcrop_Tree",   new Vector3(7.6f, SampleTerrainSurfaceY(7.6f) - 0.22f, 1.60f), 0.72f, true, dayFoliageTint);
            SpawnHandDrawnBush("ZoneA_RightOutcrop_Bush1", 2, new Vector3(6.3f, SampleTerrainSurfaceY(6.3f) + 0.30f, 1.30f), new Vector2(0.36f, 0.32f), bushLightGreen, 13);
            SpawnHandDrawnBush("ZoneA_RightOutcrop_Bush2", 4, new Vector3(7.9f, SampleTerrainSurfaceY(7.9f) + 0.34f, 1.42f), new Vector2(0.40f, 0.36f), bushMidGreen, 11);

            // 4. Zone B Shallow Dip Stepping Stones & Knoll Tree (11m to 18m)
            SpawnMossyBoulder("ZoneB_StepStone_1", new Vector3(14.0f, SampleTerrainSurfaceY(14.0f) - 0.02f, 0.0f), new Vector3(0.95f, 0.36f, 0.85f), 10f);
            SpawnMossyBoulder("ZoneB_StepStone_2", new Vector3(16.2f, SampleTerrainSurfaceY(16.2f) - 0.02f, 0.1f), new Vector3(0.95f, 0.36f, 0.85f), 100f);
            SpawnCurvedHeroTreeV2("ZoneB_CurvedTree", new Vector3(12.2f, SampleTerrainSurfaceY(12.2f) + 0.10f, 1.65f), 0.60f, false, dayFoliageTint);
            SpawnHandDrawnBush("ZoneB_Bush_1", 0, new Vector3(11.4f, SampleTerrainSurfaceY(11.4f) + 0.30f, 1.45f), new Vector2(0.35f, 0.30f), bushLightGreen, 12);
            SpawnHandDrawnBush("ZoneB_Bush_2", 3, new Vector3(17.8f, SampleTerrainSurfaceY(17.8f) + 0.30f, 1.45f), new Vector2(0.36f, 0.32f), bushMidGreen, 11);

            // =====================================================================================
            // ZONE C (19m to 31m): STEPPED ROCKY CLIFF OVERLOOK & COASTAL EXPANSE (Matching Reference 2!)
            // =====================================================================================
            // 1. High Cliff Overlook on the Left (20.2m to 25.8m, Y = -0.35m) with Hero Trees & Low Mossy Slabs
            SpawnMossyBoulder("ZoneC_CliffSlab_1", new Vector3(20.6f, SampleTerrainSurfaceY(20.6f) - 0.02f, 1.30f), new Vector3(1.65f, 0.62f, 1.20f), 30f);
            SpawnMossyBoulder("ZoneC_CliffSlab_2", new Vector3(23.2f, SampleTerrainSurfaceY(23.2f) - 0.02f, 1.35f), new Vector3(1.85f, 0.68f, 1.25f), 140f);
            SpawnMossyBoulder("ZoneC_CliffSlab_3", new Vector3(25.4f, SampleTerrainSurfaceY(25.4f) - 0.02f, 1.25f), new Vector3(1.55f, 0.58f, 1.15f), 215f);
            SpawnMossyBoulder("ZoneC_StepSlab_4",  new Vector3(27.8f, SampleTerrainSurfaceY(27.8f) - 0.02f, 1.28f), new Vector3(1.45f, 0.52f, 1.10f), 85f);

            // Clean, fringe-free hand-drawn trees perched on the left cliff overlook & coastal terrace (matching Ref 2!)
            SpawnLushHandDrawnHeroTree("ZoneC_Cliff_HeroTree", new Vector3(20.8f, SampleTerrainSurfaceY(20.8f) - 0.25f, 1.55f), 0.85f, false, dayFoliageTint);
            SpawnWindblownHeroTree("ZoneC_Cliff_Windblown",    new Vector3(23.6f, SampleTerrainSurfaceY(23.6f) - 0.15f, 1.50f), 0.72f, false, dayFoliageTint);
            SpawnCurvedHeroTreeV2("ZoneC_Terrace_CurvedTree",  new Vector3(28.0f, SampleTerrainSurfaceY(28.0f) + 0.08f, 1.60f), 0.56f, false, dayFoliageTint);

            // Layered hand-drawn bushes ("cay 1..5") along the cliff overlook and terrace steps
            SpawnHandDrawnBush("ZoneC_CliffBush_1", 4, new Vector3(21.4f, SampleTerrainSurfaceY(21.4f) + 0.34f, 1.28f), new Vector2(0.42f, 0.36f), bushLightGreen, 13);
            SpawnHandDrawnBush("ZoneC_CliffBush_2", 1, new Vector3(22.8f, SampleTerrainSurfaceY(22.8f) + 0.30f, 1.35f), new Vector2(0.36f, 0.32f), bushMidGreen, 11);
            SpawnHandDrawnBush("ZoneC_CliffBush_3", 2, new Vector3(24.6f, SampleTerrainSurfaceY(24.6f) + 0.32f, 1.26f), new Vector2(0.40f, 0.34f), bushLightGreen, 13);
            SpawnHandDrawnBush("ZoneC_CliffBush_4", 0, new Vector3(25.8f, SampleTerrainSurfaceY(25.8f) + 0.28f, 1.22f), new Vector2(0.34f, 0.30f), bushMidGreen, 12);
            SpawnHandDrawnBush("ZoneC_StepBush_5",  3, new Vector3(27.6f, SampleTerrainSurfaceY(27.6f) + 0.30f, 1.28f), new Vector2(0.38f, 0.33f), bushLightGreen, 12);

            // =====================================================================================
            // ZONE D (29.5m to 43m): COASTAL BANK GROVE, PIER & MID-WATER ROCK ISLET (Matching Reference 4!)
            // =====================================================================================
            // 1. Left Bank Coastal Grove cascading to the water's edge (29.8m to 32.0m)
            SpawnCurvedHeroTreeV2("ZoneD_WestBank_CurvedTree", new Vector3(30.2f, SampleTerrainSurfaceY(30.2f) + 0.10f, 1.55f), 0.62f, false, dayFoliageTint);
            SpawnGocCay2Tree("ZoneD_WestBank_GocCay2",         new Vector3(31.4f, SampleTerrainSurfaceY(31.4f) - 0.22f, 1.70f), 0.66f, false, bushMidGreen);

            SpawnMossyBoulder("ZoneD_WestBank_Rock1", new Vector3(30.8f, SampleTerrainSurfaceY(30.8f) - 0.02f, 1.25f), new Vector3(1.35f, 0.55f, 1.10f), 45f);
            SpawnMossyBoulder("ZoneD_WestBank_Rock2", new Vector3(31.8f, -1.92f, 1.15f), new Vector3(1.15f, 0.48f, 1.00f), 130f);

            SpawnHandDrawnBush("ZoneD_WestBank_Bush1", 4, new Vector3(29.8f, SampleTerrainSurfaceY(29.8f) + 0.34f, 1.30f), new Vector2(0.42f, 0.36f), bushLightGreen, 13);
            SpawnHandDrawnBush("ZoneD_WestBank_Bush2", 0, new Vector3(30.9f, SampleTerrainSurfaceY(30.9f) + 0.30f, 1.18f), new Vector2(0.38f, 0.33f), bushMidGreen, 14);
            SpawnHandDrawnBush("ZoneD_WestBank_Bush3", 2, new Vector3(31.6f, -1.90f + 0.28f, 1.24f), new Vector2(0.34f, 0.30f), bushLightGreen, 13);

            // 2. Mid-Water Low Dark Rock Islet jutting out behind the pier (38.0m to 40.2m — exact match to Reference 4!)
            SpawnMossyBoulder("ZoneD_MidWater_IsletMain", new Vector3(38.6f, -2.14f, 2.55f), new Vector3(2.45f, 0.62f, 1.35f), 15f);
            SpawnMossyBoulder("ZoneD_MidWater_IsletSub",  new Vector3(39.9f, -2.18f, 2.68f), new Vector3(1.65f, 0.48f, 1.10f), 195f);

            // 3. East Bank Shore Rocks & Bushes (42.0m to 44.2m)
            SpawnMossyBoulder("ZoneD_EastBank_Rock1", new Vector3(42.4f, -1.88f, 1.25f), new Vector3(1.40f, 0.52f, 1.10f), 210f);
            SpawnHandDrawnBush("ZoneD_EastBank_Bush1", 1, new Vector3(42.8f, SampleTerrainSurfaceY(42.8f) + 0.30f, 1.30f), new Vector2(0.36f, 0.32f), sunsetBushLight, 13);
            SpawnHandDrawnBush("ZoneD_EastBank_Bush2", 3, new Vector3(43.8f, SampleTerrainSurfaceY(43.8f) + 0.28f, 1.40f), new Vector2(0.34f, 0.29f), sunsetBushMid, 11);

            // =====================================================================================
            // ZONE E & F (45m to 62m): GOLDEN COASTAL SPIT & HERO TREE ON ROCKS (Matching Reference 3!)
            // =====================================================================================
            // 1. Mid-Meadow Low Mossy Outcrops & Golden-Green Bushes (47m to 54.5m)
            SpawnMossyBoulder("ZoneE_SpitRock_1", new Vector3(48.5f, SampleTerrainSurfaceY(48.5f) - 0.02f, 1.32f), new Vector3(1.15f, 0.45f, 0.90f), 60f);
            SpawnMossyBoulder("ZoneE_SpitRock_2", new Vector3(52.4f, SampleTerrainSurfaceY(52.4f) - 0.02f, 1.36f), new Vector3(0.95f, 0.38f, 0.80f), 145f);
            SpawnHandDrawnBush("ZoneE_SpitBush_1", 2, new Vector3(48.0f, SampleTerrainSurfaceY(48.0f) + 0.28f, 1.42f), new Vector2(0.34f, 0.30f), sunsetBushLight, 12);
            SpawnHandDrawnBush("ZoneE_SpitBush_2", 0, new Vector3(53.0f, SampleTerrainSurfaceY(53.0f) + 0.26f, 1.42f), new Vector2(0.32f, 0.28f), sunsetBushMid, 11);

            // 2. Right Coastal Rock Formation & Grand Hero Tree (56.2m to 61.5m — exact match to Reference 3!)
            SpawnMossyBoulder("ZoneF_CoastalRock_Left",   new Vector3(56.6f, SampleTerrainSurfaceY(56.6f) - 0.02f, 1.28f), new Vector3(1.95f, 0.72f, 1.30f), 25f);
            SpawnMossyBoulder("ZoneF_CoastalRock_Center", new Vector3(58.5f, SampleTerrainSurfaceY(58.5f) - 0.01f, 1.38f), new Vector3(2.55f, 0.92f, 1.45f), 170f);
            SpawnMossyBoulder("ZoneF_CoastalRock_Right",  new Vector3(60.3f, SampleTerrainSurfaceY(60.3f) - 0.02f, 1.32f), new Vector3(1.95f, 0.70f, 1.35f), 95f);

            // Fringe-free Lush Hand-Drawn Hero Tree + Curved Tree V2 crowning the right coastal rocks (matching Reference 3!)
            SpawnLushHandDrawnHeroTree("ZoneF_Coastal_HeroTree", new Vector3(58.6f, SampleTerrainSurfaceY(58.6f) - 0.20f, 1.52f), 0.92f, true, sunsetFoliageTint);
            SpawnCurvedHeroTreeV2("ZoneF_Coastal_Curved",        new Vector3(56.4f, SampleTerrainSurfaceY(56.4f) + 0.12f, 1.72f), 0.58f, true, sunsetBushLight);

            // Lush hand-drawn bushes nestled around the coastal rocks and tree roots
            SpawnHandDrawnBush("ZoneF_CoastalBush_1", 4, new Vector3(56.0f, SampleTerrainSurfaceY(56.0f) + 0.30f, 1.22f), new Vector2(0.40f, 0.34f), sunsetBushLight, 14);
            SpawnHandDrawnBush("ZoneF_CoastalBush_2", 1, new Vector3(57.5f, SampleTerrainSurfaceY(57.5f) + 0.34f, 1.26f), new Vector2(0.44f, 0.38f), sunsetBushMid, 13);
            SpawnHandDrawnBush("ZoneF_CoastalBush_3", 3, new Vector3(59.4f, SampleTerrainSurfaceY(59.4f) + 0.34f, 1.24f), new Vector2(0.42f, 0.36f), sunsetBushLight, 14);
            SpawnHandDrawnBush("ZoneF_CoastalBush_4", 0, new Vector3(60.8f, SampleTerrainSurfaceY(60.8f) + 0.28f, 1.30f), new Vector2(0.36f, 0.32f), sunsetBushMid, 12);

            // =====================================================================================
            // CRISP ANKLE-HIGH MEADOW GRASS TUFTS ACROSS ALL ZONES (Never obscuring the Boy character!)
            // =====================================================================================
            System.Random rnd = new System.Random(1954);
            int grassIdx = 0;

            for (float x = -5.5f; x <= 62.5f; x += 1.45f)
            {
                if (x >= 31.8f && x <= 42.2f) continue; // Skip water channel

                bool isGolden = (x >= 43.0f);
                float goldBlend = Mathf.InverseLerp(41.5f, 48.0f, x);
                Color lipTint = Color.Lerp(new Color(0.68f, 0.88f, 0.52f, 1f), new Color(0.94f, 0.84f, 0.44f, 1f), goldBlend);
                Color midTint = Color.Lerp(new Color(0.78f, 0.95f, 0.62f, 1f), new Color(0.98f, 0.88f, 0.48f, 1f), goldBlend);

                // 1. Front lip crisp ankle-high grass tuft (0.28m - 0.38m tall so Boy's legs & body are 100% clear)
                float fx = x + ((float)rnd.NextDouble() - 0.5f) * 0.5f;
                float fy = SampleTerrainSurfaceY(fx) - 0.04f;
                Sprite fSpr = (grassIdx % 3 == 0) ? sprFoliageTuft : ((grassIdx % 2 == 0) ? sprMeadowGrassTuft : sprRealGrassTuft);
                float fScale = (fSpr == sprRealGrassTuft) ? 0.085f : ((fSpr == sprMeadowGrassTuft) ? 0.14f : 0.13f);
                fScale *= (0.85f + (float)rnd.NextDouble() * 0.30f);

                SpawnCrispGrassTuft($"GrassLip_Front_{grassIdx:D3}", fSpr,
                    new Vector3(fx, fy, -1.48f - (float)rnd.NextDouble() * 0.15f),
                    new Vector2(fScale * 1.15f, fScale), lipTint, sortingOrder: 18,
                    flipX: (rnd.NextDouble() > 0.5), rotZ: ((float)rnd.NextDouble() - 0.5f) * 8f);

                // 2. Back meadow crisp grass tuft along the rear edge (Z = 1.10..1.50)
                float bx = x + ((float)rnd.NextDouble() - 0.5f) * 0.6f;
                float by = SampleTerrainSurfaceY(bx) - 0.03f;
                Sprite bSpr = (grassIdx % 2 == 0) ? sprMeadowGrassTuft : sprRealGrassTuft;
                float bScale = (bSpr == sprRealGrassTuft) ? 0.105f : 0.17f;
                bScale *= (0.88f + (float)rnd.NextDouble() * 0.32f);

                SpawnCrispGrassTuft($"GrassMeadow_Back_{grassIdx:D3}", bSpr,
                    new Vector3(bx, by, 1.15f + (float)rnd.NextDouble() * 0.35f),
                    new Vector2(bScale * 1.15f, bScale), midTint, sortingOrder: 8,
                    flipX: isGolden ? false : (rnd.NextDouble() > 0.5),
                    rotZ: isGolden ? -6f : ((float)rnd.NextDouble() - 0.5f) * 7f);

                grassIdx++;
            }
        }

        // =========================================================================================
        // 7. CONFIGURE CLEAN LIGHTING, POST-PROCESSING & CAMERA FRAMING
        // =========================================================================================
        private static void ConfigureLightingAndCamera()
        {
            RenderSettings.fog = false;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.48f, 0.68f, 0.84f);
            RenderSettings.ambientEquatorColor = new Color(0.86f, 0.80f, 0.65f);
            RenderSettings.ambientGroundColor = new Color(0.16f, 0.20f, 0.25f);
            RenderSettings.ambientIntensity = 1.12f;

            var lightObj = GameObject.Find("08_Lighting/Directional Light");
            if (lightObj != null)
            {
                var l = lightObj.GetComponent<Light>();
                if (l != null)
                {
                    lightObj.transform.rotation = Quaternion.Euler(28.0f, 32.0f, 0.0f);
                    l.color = new Color(1.0f, 0.96f, 0.88f);
                    l.intensity = 1.25f;
                    l.shadows = LightShadows.Soft;
                    l.shadowStrength = 0.42f;
                    l.shadowNormalBias = 0.35f;
                }
            }

            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/VolumeProfile_Scene1_CaMau1954.asset");
            if (profile != null)
            {
                while (profile.components.Count > 0) profile.Remove(profile.components[0].GetType());

                var tonemapping = profile.Add<Tonemapping>(true);
                tonemapping.mode.Override(TonemappingMode.Neutral);

                var colorAdj = profile.Add<ColorAdjustments>(true);
                colorAdj.postExposure.Override(0.06f);
                colorAdj.contrast.Override(7.0f);
                colorAdj.saturation.Override(7.0f);

                var bloom = profile.Add<Bloom>(true);
                bloom.threshold.Override(0.95f);
                bloom.intensity.Override(0.20f);
                bloom.scatter.Override(0.65f);

                var vignette = profile.Add<Vignette>(true);
                vignette.intensity.Override(0.12f);
                vignette.smoothness.Override(0.45f);
                vignette.color.Override(new Color(0.04f, 0.06f, 0.09f));

                EditorUtility.SetDirty(profile);
            }

            var vcam = GameObject.Find("01_Camera/CM_VirtualCamera_2.5D");
            if (vcam != null)
            {
                vcam.transform.position = new Vector3(1.5f, 0.35f, -13.5f);
                vcam.transform.rotation = Quaternion.identity;
            }

            var mainCam = GameObject.Find("01_Camera/Main Camera");
            if (mainCam != null)
            {
                mainCam.transform.position = new Vector3(1.5f, 0.35f, -13.5f);
                mainCam.transform.rotation = Quaternion.identity;
                var camComp = mainCam.GetComponent<Camera>();
                if (camComp != null)
                {
                    camComp.fieldOfView = 38.0f;
                    camComp.clearFlags = CameraClearFlags.SolidColor;
                    camComp.backgroundColor = new Color(0.22f, 0.48f, 0.72f);
                }
            }
        }

        // =========================================================================================
        // 8. CAPTURE HIGH-RES CINEMATIC VIEWS ACROSS ALL 4 PAINTED BACKGROUND ZONES
        // =========================================================================================
        public static void CaptureCinematicViews()
        {
            var cam = Camera.main;
            if (cam == null) return;

            string outDir = Path.Combine(Application.dataPath, "../Screenshots/Showcase_Presentation");
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

            Vector3 origPos = cam.transform.position;
            Quaternion origRot = cam.transform.rotation;
            float origFov = cam.fieldOfView;

            System.Action<string, Vector3, Vector3, float> capture = (fileName, pos, euler, fov) =>
            {
                cam.transform.position = pos;
                cam.transform.eulerAngles = euler;
                cam.fieldOfView = fov;

                int width = 1280;
                int height = 720;
                var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
                var prevRt = cam.targetTexture;
                cam.targetTexture = rt;
                cam.Render();
                cam.Render();

                RenderTexture.active = rt;
                var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();

                cam.targetTexture = prevRt;
                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(rt);

                byte[] bytes = tex.EncodeToPNG();
                Object.DestroyImmediate(tex);

                string fullPath = Path.Combine(outDir, fileName);
                File.WriteAllBytes(fullPath, bytes);
                Debug.Log($"<color=#38bdf8>[Capture]</color> Saved cinematic view: {fileName}");
            };

            // 1. Zone A Entrance (Matches Reference 1: Open meadow, left Hero Tree, right rocky outcrop)
            capture("LanaBlendOut_ZoneA_DayPlains.png", new Vector3(1.5f, 0.35f, -13.5f), Vector3.zero, 38f);

            // 2. Zone C Stepped Rocky Cliff Overlook (Matches Reference 2: Left cliff with tree, right coastal expanse)
            capture("LanaBlendOut_ZoneC_SteppedOutcrop.png", new Vector3(24.5f, 0.35f, -13.5f), Vector3.zero, 38f);

            // 3. Zone D Coastal Bay & Wooden Pier (Matches Reference 4: Left tree bank, pier with tall mooring poles, mid-water rock islet)
            capture("LanaBlendOut_ZoneD_CoastalBayPier.png", new Vector3(35.0f, 0.35f, -13.5f), Vector3.zero, 38f);

            // 4. Zone E/F Golden Sunset Spit & Hero Tree on Rocks (Matches Reference 3: Windswept golden grass, right Hero Tree on dark coastal boulders)
            capture("LanaBlendOut_ZoneEF_GoldenSunset.png", new Vector3(55.0f, 0.35f, -13.5f), Vector3.zero, 38f);

            cam.transform.position = origPos;
            cam.transform.rotation = origRot;
            cam.fieldOfView = origFov;
        }
    }
}
#endif
