#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Cinemachine;
using HoaVaMau.Gameplay;

namespace HoaVaMau.Editor
{
    /// <summary>
    /// Complete, Public-Ready Planet of Lana Level 1 Builder ("New Scene 1_Overhaul.unity").
    /// Implements the full 145-meter narrative & gameplay progression requested by the user:
    /// - Entry: Sky-to-Forest Cloud Descent over official background ("22ffd054-1ca7-406c-9d11-0b93ba28b71a.png")
    ///   through multi-layer parallax clouds and foreground bushes into the forest clearing where An is playing with companions.
    /// - Zone 1 (X: -10m .. +35.5m): Forest Clearing & 4-Step Planet of Lana Manual Obstacle Course:
    ///     1. 3D Verlet Rope climb & swing up a 3D rock cliff (X = 4.88m, Y = -1.95m -> -0.55m), hanging in free air with zero platform obstruction.
    ///     2. Jump / Swing across the deep rocky ravine chasm (X = 14.6m -> 17.6m, floor at Y = -3.80m) via a 4.6m swing rope.
    ///     3. Climb down stepped ledges & crawl under a low fallen tree trunk (X = 23.8m -> 26.4m, Y = -1.10m).
    ///     4. Push/pull 3D wooden crate (InteractableCrate) to the cliff corner at X = 34.6m to climb onto the High Platform (Y = +2.15m).
    /// - Zone 2 (X: +35.5m .. +62.0m): High Cliff Overlook (seeing the hill & village below) -> Smooth Downhill Slope (Y: +2.15m -> -1.85m).
    /// - Zone 3 (X: +62.0m .. +95.0m): Perfectly Flat Horizontal 3D Platform (Y = -1.85m) running through a vast 2D Golden Rice Field.
    /// - Zone 4 (X: +94.0m .. +135.0m): Wide-Zoom Village Arrival Cutscene + Living 3D Stilted-House Village surrounded by golden rice fields,
    ///   populated with rigged & animated 3D Villager NPCs that An can interact with ([E]) to talk.
    /// - 100% Unified Borderless Planet of Lana 2D Botanical Art (Lana_VN_Pack + Lana_Unified_Art), ZERO outlined/Krita mismatch assets.
    /// </summary>
    public static class OverhaulScene1Visuals
    {
        public const string ScenePath = "Assets/Scenes/New Scene 1_Overhaul.unity";
        private const string GenAssetsDir = "Assets/Materials/LanaOverhaul";
        private const string PackTexRoot = "Assets/Textures/Environment/Lana_VN_Pack";
        private const string UnifiedTexRoot = "Assets/Textures/Lana_Unified_Art";

        [MenuItem("Hoa va Mau/Level Design/Execute Planet of Lana Scene Overhaul")]
        public static void ExecuteOverhaul()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Debug.Log($"<color=#38bdf8>[LanaOverhaul]</color> Building complete 145m public-ready Planet of Lana Level 1 in: {scene.name}");

            if (!Directory.Exists(GenAssetsDir)) Directory.CreateDirectory(GenAssetsDir);

            // 1. Purge all legacy 60m prototype objects, mismatched trees, and old colliders
            PurgeLegacyPrototypeAndMismatchedAssets();

            // 2. Create unified Planet of Lana materials (3D Ledges, Boulders, Timber, and Default Sprite Material)
            var mats = CreateOrUpdateLanaMaterials();

            // 3. Build Official Painted Backgrounds ("22ffd054-1ca7-406c-9d11-0b93ba28b71a.png") & Seamless Sky + Multi-Layer Cloudscape
            BuildOfficialBackgroundsAndCloudDescent(mats);

            // 4. Build 3D Sculpted Terrain Ledges & Exact Physics Colliders across all 4 Zones (-12m to +138m)
            BuildSculpted3DLedgesAndPhysics(mats);

            // 5. Build Forest Manual Obstacle Course (Free-Air Rope Cliff, Deep Ravine Swing, Fallen Tree Crawl, Push/Pull Crate, Panorama Overlook)
            BuildForestManualObstaclesAndGameplay(mats);

            // 6. Populate 100% Unified Borderless Planet of Lana 2D Ecosystem (Forest, Hill Slope, Golden Rice Field, Village)
            PopulateUnifiedPlanetOfLanaEcosystem(mats);

            // 7. Build Living 3D Stilted-House Village, Wide-Zoom Cutscene & Rigged Animated 3D NPCs
            BuildLivingVillageAndRiggedNPCs(mats);

            // 8. Configure Cameras (VCam_IntroSky at Y=+36.5m, CM_VirtualCamera_2.5D, VCam_VillageArrivalWide), Cutscene & Lighting
            ConfigureLightingCamerasAndCutscenes();

            // Save Scene & Assets
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("<color=#4ade80>[LanaOverhaul]</color> <b>COMPLETE 145M PLANET OF LANA LEVEL 1 BUILT & SAVED SUCCESSFULLY!</b>");

            // 9. Capture High-Res 1280x720 Verification Screenshots across all zones
            CaptureCinematicViews();
        }

        [MenuItem("Hoa va Mau/Cutscene/Update Atmosphere and Cutscene Only")]
        public static void UpdateAtmosphereAndIntroCutscene()
        {
            var scene = EditorSceneManager.GetActiveScene();
            Debug.Log($"<color=#38bdf8>[LanaOverhaul]</color> Fast updating Atmosphere, Seamless Sky, Clouds, and Cutscene in: {scene.name}");

            var bgRoot = GameObject.Find("07_Background");
            if (bgRoot != null)
            {
                var vista = bgRoot.transform.Find("Lana_Painted_Vistas");
                if (vista != null) UnityEngine.Object.DestroyImmediate(vista.gameObject);
                var clouds = bgRoot.transform.Find("PlanetOfLana_RichCloudscape");
                if (clouds != null) UnityEngine.Object.DestroyImmediate(clouds.gameObject);
                var oldClouds = bgRoot.transform.Find("IntroSky_CloudDescentGroup");
                if (oldClouds != null) UnityEngine.Object.DestroyImmediate(oldClouds.gameObject);
            }

            var mats = CreateOrUpdateLanaMaterials();
            BuildOfficialBackgroundsAndCloudDescent(mats);
            ConfigureLightingCamerasAndCutscenes();

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("<color=#4ade80>[LanaOverhaul]</color> <b>Atmosphere, Seamless Sky, Rich Cloudscape, and Cutscene updated successfully!</b>");
        }

        // =========================================================================================
        // 1. PURGE LEGACY 60M PROTOTYPE & MISMATCHED ASSETS
        // =========================================================================================
        private static void PurgeLegacyPrototypeAndMismatchedAssets()
        {
            string[] rootsToClear = new[] { "04_Foreground", "05_Midground", "06_Water", "07_Background", "10_EnvironmentSystems" };
            foreach (var rName in rootsToClear)
            {
                var rGO = GameObject.Find(rName);
                if (rGO != null)
                {
                    while (rGO.transform.childCount > 0)
                    {
                        UnityEngine.Object.DestroyImmediate(rGO.transform.GetChild(0).gameObject);
                    }
                }
            }

            var terrainRoot = GameObject.Find("03_Terrain");
            if (terrainRoot != null)
            {
                while (terrainRoot.transform.childCount > 0)
                {
                    UnityEngine.Object.DestroyImmediate(terrainRoot.transform.GetChild(0).gameObject);
                }
            }

            var vfxRoot = GameObject.Find("09_VFX");
            if (vfxRoot != null)
            {
                string[] vfxRemove = new[] { "SunShaft_ZoneE_Clearing", "GodRays_Canopy" };
                foreach (var n in vfxRemove)
                {
                    var t = vfxRoot.transform.Find(n);
                    if (t != null) UnityEngine.Object.DestroyImmediate(t.gameObject);
                }
            }

            // Clean old dynamic obstacles under 02_Gameplay while preserving PF_Player_Minh & Exit trigger
            var gpRoot = GameObject.Find("02_Gameplay");
            if (gpRoot != null)
            {
                var toDelete = new List<GameObject>();
                foreach (Transform child in gpRoot.transform)
                {
                    if (child.name != "PF_Player_Minh" && child.name != "Character_An" && !child.name.Contains("ExitGoal"))
                    {
                        toDelete.Add(child.gameObject);
                    }
                }
                foreach (var go in toDelete) UnityEngine.Object.DestroyImmediate(go);
            }

            var oldWideCam = GameObject.Find("VCam_VillageArrivalWide");
            if (oldWideCam != null) UnityEngine.Object.DestroyImmediate(oldWideCam);
        }

        // =========================================================================================
        // 2. MATERIALS
        // =========================================================================================
        private class LanaMaterialSet
        {
            public Material GrassCapMat;
            public Material RockUnderbellyMat;
            public Material MossyBoulderMat;
            public Material SpriteDefaultMat;
            public Material TimberDarkMat;
        }

        private static LanaMaterialSet CreateOrUpdateLanaMaterials()
        {
            var set = new LanaMaterialSet();
            Shader ledgeShader = Shader.Find("HoaVaMau/LanaStylizedLedge") ?? Shader.Find("Universal Render Pipeline/Lit");

            // 1. Grass Cap Material (Transitions from lush forest green to warm golden harvest at X = 52m..62m)
            set.GrassCapMat = GetOrCreateMat($"{GenAssetsDir}/M_Lana_GrassCap.mat", ledgeShader);
            set.GrassCapMat.SetFloat("_IsGrassCap", 1.0f);
            set.GrassCapMat.SetColor("_GrassTopColor", new Color(0.40f, 0.61f, 0.27f, 1f));
            set.GrassCapMat.SetColor("_GrassMidColor", new Color(0.25f, 0.42f, 0.18f, 1f));
            set.GrassCapMat.SetColor("_GrassGoldTopColor", new Color(0.78f, 0.67f, 0.28f, 1f));
            set.GrassCapMat.SetColor("_GrassGoldMidColor", new Color(0.52f, 0.45f, 0.19f, 1f));
            set.GrassCapMat.SetFloat("_GoldenStartX", 50.0f);
            set.GrassCapMat.SetFloat("_GoldenEndX", 62.0f);
            set.GrassCapMat.SetColor("_RockDarkColor", new Color(0.05f, 0.07f, 0.11f, 1f));
            EditorUtility.SetDirty(set.GrassCapMat);

            // 2. Stratified Slate Rock Cross-Section & Cliff Material
            set.RockUnderbellyMat = GetOrCreateMat($"{GenAssetsDir}/M_Lana_RockUnderbelly.mat", ledgeShader);
            set.RockUnderbellyMat.SetFloat("_IsGrassCap", 0.0f);
            set.RockUnderbellyMat.SetColor("_RockLightColor", new Color(0.12f, 0.15f, 0.20f, 1f));
            set.RockUnderbellyMat.SetColor("_RockMidColor", new Color(0.08f, 0.10f, 0.14f, 1f));
            set.RockUnderbellyMat.SetColor("_RockDarkColor", new Color(0.04f, 0.05f, 0.08f, 1f));
            EditorUtility.SetDirty(set.RockUnderbellyMat);

            // 3. Moss-Crowned Slate Boulder Material
            set.MossyBoulderMat = GetOrCreateMat($"{GenAssetsDir}/M_Lana_MossyBoulder.mat", ledgeShader);
            set.MossyBoulderMat.SetFloat("_IsGrassCap", 0.5f);
            set.MossyBoulderMat.SetFloat("_GrassNormalThreshold", 0.42f);
            set.MossyBoulderMat.SetColor("_GrassTopColor", new Color(0.40f, 0.61f, 0.27f, 1f));
            set.MossyBoulderMat.SetColor("_GrassMidColor", new Color(0.25f, 0.42f, 0.18f, 1f));
            set.MossyBoulderMat.SetColor("_GrassGoldTopColor", new Color(0.78f, 0.67f, 0.28f, 1f));
            set.MossyBoulderMat.SetColor("_GrassGoldMidColor", new Color(0.52f, 0.45f, 0.19f, 1f));
            set.MossyBoulderMat.SetFloat("_GoldenStartX", 50.0f);
            set.MossyBoulderMat.SetFloat("_GoldenEndX", 62.0f);
            set.MossyBoulderMat.SetColor("_RockLightColor", new Color(0.24f, 0.30f, 0.38f, 1f));
            set.MossyBoulderMat.SetColor("_RockMidColor", new Color(0.15f, 0.20f, 0.27f, 1f));
            set.MossyBoulderMat.SetColor("_RockDarkColor", new Color(0.08f, 0.11f, 0.16f, 1f));
            EditorUtility.SetDirty(set.MossyBoulderMat);

            // 4. URP 2D Sprite Unlit Material
            Shader urpSpriteShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
            set.SpriteDefaultMat = GetOrCreateMat($"{GenAssetsDir}/M_Lana_URPSpriteDefault.mat", urpSpriteShader);

            // 5. Dark Timber Material for Fallen Trunk & Village Structures
            set.TimberDarkMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Scene1_CaMau/M_CaMau_TimberDark.mat");
            if (set.TimberDarkMat == null)
            {
                set.TimberDarkMat = GetOrCreateMat($"{GenAssetsDir}/M_Lana_TimberDark.mat", Shader.Find("Universal Render Pipeline/Lit"));
                set.TimberDarkMat.SetColor("_BaseColor", new Color(0.26f, 0.18f, 0.12f, 1f));
            }

            return set;
        }

        private static readonly Dictionary<Texture2D, Material> _spriteMatCache = new Dictionary<Texture2D, Material>();

        private static Material GetOrCreateSpriteMat(Sprite spr, Material fallbackMat = null)
        {
            if (spr == null || spr.texture == null) return fallbackMat;
            if (_spriteMatCache.TryGetValue(spr.texture, out Material cached) && cached != null)
            {
                return cached;
            }

            string spriteMatDir = $"{GenAssetsDir}/SpriteMats";
            if (!Directory.Exists(spriteMatDir)) Directory.CreateDirectory(spriteMatDir);

            Shader urpSpriteShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
            string matPath = $"{spriteMatDir}/M_Spr_{spr.texture.name}.mat";
            Material mat = GetOrCreateMat(matPath, urpSpriteShader);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", spr.texture);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", spr.texture);
            mat.mainTexture = spr.texture;
            EditorUtility.SetDirty(mat);

            _spriteMatCache[spr.texture] = mat;
            return mat;
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

        private static Sprite LoadSprite(string path, Vector2? customPivot = null)
        {
            if (!File.Exists(path)) return null;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                importer = AssetImporter.GetAtPath(path) as TextureImporter;
            }
            if (importer != null)
            {
                bool changed = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    changed = true;
                }
                if (importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                    changed = true;
                }
                if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; changed = true; }
                if (importer.mipmapEnabled) { importer.mipmapEnabled = false; changed = true; }

                if (customPivot.HasValue || changed)
                {
                    var settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                    settings.textureType = TextureImporterType.Sprite;
                    settings.spriteMode = (int)SpriteImportMode.Single;
                    if (settings.spriteMeshType != SpriteMeshType.FullRect) { settings.spriteMeshType = SpriteMeshType.FullRect; changed = true; }
                    if (customPivot.HasValue && (settings.spriteAlignment != (int)SpriteAlignment.Custom || Vector2.Distance(settings.spritePivot, customPivot.Value) > 0.002f))
                    {
                        settings.spriteAlignment = (int)SpriteAlignment.Custom;
                        settings.spritePivot = customPivot.Value;
                        changed = true;
                    }
                    if (changed)
                    {
                        importer.SetTextureSettings(settings);
                        importer.SaveAndReimport();
                    }
                }
            }
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s != null) return s;
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (obj is Sprite spr) return spr;
            }
            return null;
        }

        // =========================================================================================
        // 3. SEAMLESS UNIFIED SKY, PAINTED VISTAS & RICH MULTI-LAYER CLOUDSCAPE
        // =========================================================================================
        private static void BuildOfficialBackgroundsAndCloudDescent(LanaMaterialSet mats)
        {
            var bgRoot = GameObject.Find("07_Background") ?? new GameObject("07_Background");
            var vistaGroup = new GameObject("Lana_Painted_Vistas");
            vistaGroup.transform.SetParent(bgRoot.transform, false);

            Shader bgShader = Shader.Find("HoaVaMau/LanaBackgroundBlend") ?? Shader.Find("Universal Render Pipeline/Unlit");

            var texOfficialMain = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Backgrounds_Lana/BG_Lana_MainSkyHills_Official.png");
            if (texOfficialMain == null)
            {
                texOfficialMain = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Backgrounds_Lana/BG_Lana_PlainsHills_Day.png");
            }
            var texPlains = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Backgrounds_Lana/BG_Lana_PlainsHills_Day.png");

            void SpawnVistaPanel(string name, Texture2D tex, float centerX, float centerY, float z, float width, float height, float leftFeather, float rightFeather, int queueOffset, Color tint, bool flipY = false, float topFeather = 0.0f)
            {
                if (tex == null) return;
                var mat = GetOrCreateMat($"{GenAssetsDir}/M_{name}.mat", bgShader);
                mat.SetTexture("_MainTex", tex);
                mat.SetColor("_Color", tint);
                mat.SetFloat("_LeftFeather", leftFeather);
                mat.SetFloat("_RightFeather", rightFeather);
                mat.SetFloat("_BottomFeather", 0.0f);
                mat.SetFloat("_TopFeather", topFeather);
                mat.renderQueue = 2900 + queueOffset;
                EditorUtility.SetDirty(mat);

                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = name;
                quad.transform.SetParent(vistaGroup.transform, false);
                quad.transform.position = new Vector3(centerX, centerY, z);
                quad.transform.localScale = new Vector3(width, flipY ? -Mathf.Abs(height) : Mathf.Abs(height), 1.0f);

                var mr = quad.GetComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
                UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());
            }

            var follower = bgRoot.GetComponent<LanaParallaxBackgroundFollower>() ?? bgRoot.AddComponent<LanaParallaxBackgroundFollower>();
            follower.ClearLayers();

            // 1. SINGLE CONTINUOUS SEAMLESS SKY BACKDROP QUAD SPANNING 220 METERS HORIZONTALLY!
            // Generated with 100% horizontally-uniform pixel rows: mathematically ZERO horizontal color differences and ZERO vertical seams!
            Texture2D texSeamlessSky = BuildOrLoadSeamlessSkyTexture();
            SpawnVistaPanel("Sky_Continuous_Backdrop", texSeamlessSky, 65.0f, 21.0f, 18.0f, 220.0f, 52.0f, 0.0f, 0.0f, 0, Color.white);

            // 2. ZONE 2 & 3 ROLLING HILLS & PLAINS VISTA (Distant soft hazy watercolor hills at horizon, X = 80.0m)
            const float bgDepthZ = 16.5f;
            float panelHeight = 18.0f;
            if (texPlains != null)
            {
                float plainsAspect = (float)texPlains.width / Mathf.Max(1, texPlains.height);
                float plainsWidth = panelHeight * plainsAspect;
                SpawnVistaPanel("Vista_RollingHills_Plains", texPlains, 80.0f, 3.20f, bgDepthZ, plainsWidth, panelHeight, 0.25f, 0.25f, 1, new Color(0.96f, 0.98f, 1.0f, 0.95f), false, 0.28f);
            }

            follower.RegisterLayer("Lana_Painted_Vistas", vistaGroup.transform, 0.92f, 0.02f);

            // 4. POPULATE RICH 25-CLOUD MULTI-LAYER CLOUDSCAPE
            PopulateRichPlanetOfLanaCloudscape(bgRoot.transform, mats);
        }

        private static Texture2D BuildOrLoadSeamlessSkyTexture()
        {
            string outPath = "Assets/Textures/Backgrounds_Lana/T_Lana_ContinuousCosmicSky_Seamless.png";
            int w = 256;
            int h = 1024;
            var skyTex = new Texture2D(w, h, TextureFormat.RGB24, false);

            // Planet of Lana / Studio Ghibli vertical sky gradient palette:
            // v = 0.00 .. 0.12: Warm horizon pale cyan-white (matching distant haze / mountains)
            Color cHorizonMist  = new Color(0.68f, 0.78f, 0.86f, 1f);
            // v = 0.12 .. 0.32: Soft daytime cyan-cerulean
            Color cLowerSky     = new Color(0.38f, 0.62f, 0.82f, 1f);
            // v = 0.32 .. 0.58: Pure Planet of Lana cobalt blue
            Color cMidSky       = new Color(0.12f, 0.38f, 0.66f, 1f);
            // v = 0.58 .. 0.82: Deep cosmic midnight cobalt
            Color cUpperSky     = new Color(0.045f, 0.145f, 0.300f, 1f);
            // v = 0.82 .. 1.00: Cosmic zenith galaxy black-blue
            Color cGalaxyZenith = new Color(0.015f, 0.045f, 0.115f, 1f);

            Color[] pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float v = (float)y / Mathf.Max(1, h - 1);
                Color rowCol;
                if (v < 0.12f)
                {
                    float t = Mathf.SmoothStep(0f, 1f, v / 0.12f);
                    rowCol = Color.Lerp(cHorizonMist, cLowerSky, t);
                }
                else if (v < 0.32f)
                {
                    float t = Mathf.SmoothStep(0f, 1f, (v - 0.12f) / 0.20f);
                    rowCol = Color.Lerp(cLowerSky, cMidSky, t);
                }
                else if (v < 0.58f)
                {
                    float t = Mathf.SmoothStep(0f, 1f, (v - 0.32f) / 0.26f);
                    rowCol = Color.Lerp(cMidSky, cUpperSky, t);
                }
                else
                {
                    float t = Mathf.SmoothStep(0f, 1f, (v - 0.58f) / 0.42f);
                    rowCol = Color.Lerp(cUpperSky, cGalaxyZenith, t);
                }

                // 100% identical horizontal pixels -> zero horizontal seam!
                for (int x = 0; x < w; x++)
                {
                    pixels[y * w + x] = rowCol;
                }
            }
            skyTex.SetPixels(pixels);
            skyTex.Apply();

            File.WriteAllBytes(outPath, skyTex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(skyTex);
            AssetDatabase.ImportAsset(outPath, ImportAssetOptions.ForceUpdate);

            var ti = AssetImporter.GetAtPath(outPath) as TextureImporter;
            if (ti != null)
            {
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.mipmapEnabled = false;
                ti.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(outPath);
        }

        private static void PopulateRichPlanetOfLanaCloudscape(Transform bgParent, LanaMaterialSet mats)
        {
            var cloudGroup = new GameObject("PlanetOfLana_RichCloudscape");
            cloudGroup.transform.SetParent(bgParent, false);

            const string CloudBaseDir = "Assets/Art/Environment/Sky/Clouds";
            const string LanaCloudDir = "Assets/Textures/Lana_Unified_Art/Clouds";
            const string TexturesCloudDir = "Assets/Textures/Clouds";

            Sprite sprHeroDescent01 = LoadSprite($"{CloudBaseDir}/Foreground/T_Cloud_Hero_Descent01.png");
            Sprite sprHeroDescent02 = LoadSprite($"{CloudBaseDir}/Foreground/T_Cloud_Hero_Descent02.png");
            Sprite sprMidCumulusBank= LoadSprite($"{CloudBaseDir}/Midground/T_Cloud_Mid_CumulusBank01.png");
            Sprite sprMidCluster02  = LoadSprite($"{CloudBaseDir}/Midground/T_Cloud_Mid_Cluster02.png");
            Sprite sprMidSoftBank03 = LoadSprite($"{CloudBaseDir}/Midground/T_Cloud_Mid_SoftBank03.png");
            Sprite sprDistantHorizon= LoadSprite($"{CloudBaseDir}/Distant/T_Cloud_Distant_Horizon03.png");
            Sprite sprDistantLayer02= LoadSprite($"{CloudBaseDir}/Distant/T_Cloud_Distant_Layer02.png");
            Sprite sprDistantWisp01 = LoadSprite($"{CloudBaseDir}/Distant/T_Cloud_Distant_Wisp01.png");

            Sprite sprLanaHeroCumulus= LoadSprite($"{LanaCloudDir}/T_Lana_Cloud_01_CumulusHero.png") ?? sprHeroDescent01;
            Sprite sprLanaWideBank   = LoadSprite($"{LanaCloudDir}/T_Lana_Cloud_02_WideBank.png") ?? sprMidCumulusBank;
            Sprite sprLanaHighWisps  = LoadSprite($"{LanaCloudDir}/T_Lana_Cloud_03_HighWisps.png") ?? sprDistantWisp01;
            Sprite sprLanaGoldenHoriz= LoadSprite($"{LanaCloudDir}/T_Lana_Cloud_04_GoldenHorizon.png") ?? sprDistantHorizon;

            Sprite sprLanaCloud01    = LoadSprite($"{TexturesCloudDir}/lana_cloud_01.png") ?? sprHeroDescent01;
            Sprite sprLanaCloud02    = LoadSprite($"{TexturesCloudDir}/lana_cloud_02.png") ?? sprMidCluster02;
            Sprite sprLanaCloud03    = LoadSprite($"{TexturesCloudDir}/lana_cloud_03.png") ?? sprMidSoftBank03;

            void SpawnCloud(string name, Sprite spr, Vector3 pos, Vector2 scale, Color col, int sortOrder, bool flipX = false, float driftSpeed = 0.35f)
            {
                if (spr == null) return;
                var go = new GameObject(name);
                go.transform.SetParent(cloudGroup.transform, false);
                go.transform.position = pos;
                go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = spr;
                sr.sharedMaterial = GetOrCreateSpriteMat(spr, mats.SpriteDefaultMat);
                sr.color = col;
                sr.sortingOrder = sortOrder;
                sr.flipX = flipX;

                var sway = go.AddComponent<PlanetOfLanaFoliageSway>();
                var so = new SerializedObject(sway);
                so.FindProperty("maxRotationAngle").floatValue = 0.15f;
                so.FindProperty("swaySpeed").floatValue = driftSpeed;
                so.FindProperty("useWorldPositionOffset").boolValue = true;
                so.FindProperty("manualPhaseOffset").floatValue = pos.x * 0.1f;
                so.FindProperty("scaleBreathing").floatValue = 0.015f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // 1. HIGH STRATOSPHERE WISPS (Y = 28.0m .. 38.0m, Z = 11.0m .. 14.0m)
            SpawnCloud("Cloud_Zenith_Wisp01", sprLanaHighWisps,  new Vector3(-12.0f, 35.5f, 13.0f), new Vector2(3.2f, 2.5f), new Color(0.92f, 0.95f, 1.0f, 0.85f), 1);
            SpawnCloud("Cloud_Zenith_Wisp02", sprDistantWisp01,  new Vector3( -3.0f, 36.5f, 12.5f), new Vector2(3.6f, 2.8f), new Color(0.95f, 0.97f, 1.0f, 0.90f), 1);
            SpawnCloud("Cloud_Zenith_Wisp03", sprDistantLayer02, new Vector3(  8.5f, 34.0f, 13.5f), new Vector2(4.0f, 2.6f), new Color(0.88f, 0.93f, 1.0f, 0.80f), 1, true);
            SpawnCloud("Cloud_Zenith_Wisp04", sprLanaHighWisps,  new Vector3( 24.0f, 35.0f, 13.0f), new Vector2(3.5f, 2.5f), new Color(0.90f, 0.94f, 1.0f, 0.85f), 1);
            SpawnCloud("Cloud_Zenith_Wisp05", sprDistantWisp01,  new Vector3( 45.0f, 36.0f, 13.0f), new Vector2(3.8f, 2.7f), new Color(0.90f, 0.94f, 1.0f, 0.85f), 1);

            // 2. MID-SKY BILLOWY CUMULUS (Y = 14.0m .. 26.0m, Z = 9.5m .. 12.0m)
            SpawnCloud("Cloud_Mid_HeroDescent01", sprHeroDescent01,   new Vector3( -2.5f, 22.5f, 10.5f), new Vector2(2.8f, 2.8f), new Color(0.98f, 0.99f, 1.0f, 0.96f), 2);
            SpawnCloud("Cloud_Mid_HeroDescent02", sprHeroDescent02,   new Vector3(  6.0f, 20.0f, 11.0f), new Vector2(3.0f, 2.6f), new Color(0.96f, 0.98f, 1.0f, 0.92f), 2, true);
            SpawnCloud("Cloud_Mid_Cumulus01",     sprLanaHeroCumulus, new Vector3(-16.0f, 18.5f, 10.5f), new Vector2(3.2f, 2.8f), new Color(0.97f, 0.98f, 1.0f, 0.94f), 2);
            SpawnCloud("Cloud_Mid_Cumulus02",     sprMidCumulusBank,  new Vector3( 18.0f, 17.5f, 11.2f), new Vector2(3.6f, 2.7f), new Color(0.95f, 0.97f, 1.0f, 0.92f), 2);
            SpawnCloud("Cloud_Mid_Cumulus03",     sprMidCluster02,    new Vector3( 32.0f, 21.0f, 10.8f), new Vector2(3.4f, 2.6f), new Color(0.96f, 0.98f, 1.0f, 0.93f), 2, true);
            SpawnCloud("Cloud_Mid_Cumulus04",     sprLanaCloud01,     new Vector3( 50.0f, 18.0f, 11.5f), new Vector2(3.5f, 2.8f), new Color(0.95f, 0.97f, 1.0f, 0.92f), 2);
            SpawnCloud("Cloud_Mid_Cumulus05",     sprHeroDescent01,   new Vector3( 68.0f, 19.5f, 10.5f), new Vector2(3.2f, 2.7f), new Color(0.96f, 0.98f, 1.0f, 0.94f), 2, true);
            SpawnCloud("Cloud_Mid_Cumulus06",     sprMidCumulusBank,  new Vector3( 88.0f, 17.0f, 11.2f), new Vector2(3.8f, 2.9f), new Color(0.95f, 0.97f, 1.0f, 0.92f), 2);
            SpawnCloud("Cloud_Mid_Cumulus07",     sprLanaHeroCumulus, new Vector3(108.0f, 20.0f, 10.8f), new Vector2(3.4f, 2.8f), new Color(0.96f, 0.98f, 1.0f, 0.93f), 2);
            SpawnCloud("Cloud_Mid_Cumulus08",     sprMidCluster02,    new Vector3(128.0f, 18.5f, 11.5f), new Vector2(3.6f, 2.7f), new Color(0.95f, 0.97f, 1.0f, 0.92f), 2, true);

            // 3. LOW HORIZON CUMULUS BANKS (Y = 2.0m .. 9.0m, Z = 12.0m .. 15.0m)
            SpawnCloud("Cloud_Horizon_Opening01", sprLanaHeroCumulus, new Vector3(  3.6f,  4.8f, 12.5f), new Vector2(3.6f, 2.6f), new Color(1.0f, 1.0f, 1.0f, 0.96f), 3, true);
            SpawnCloud("Cloud_Horizon_Bank02",    sprLanaWideBank,    new Vector3( 14.5f,  6.2f, 13.5f), new Vector2(4.2f, 2.8f), new Color(0.98f, 0.99f, 1.0f, 0.92f), 3);
            SpawnCloud("Cloud_Horizon_Bank03",    sprDistantHorizon,  new Vector3( 28.0f,  5.5f, 14.0f), new Vector2(4.5f, 2.9f), new Color(0.96f, 0.98f, 1.0f, 0.90f), 3, true);
            SpawnCloud("Cloud_Horizon_Bank04",    sprMidSoftBank03,   new Vector3( 42.0f,  6.8f, 13.2f), new Vector2(3.9f, 2.7f), new Color(0.97f, 0.98f, 1.0f, 0.92f), 3);
            SpawnCloud("Cloud_Horizon_Bank05",    sprLanaGoldenHoriz, new Vector3( 58.0f,  5.8f, 14.2f), new Vector2(4.6f, 3.0f), new Color(0.98f, 0.97f, 0.92f, 0.92f), 3);
            SpawnCloud("Cloud_Horizon_Bank06",    sprLanaWideBank,    new Vector3( 74.0f,  6.5f, 13.8f), new Vector2(4.4f, 2.8f), new Color(1.0f, 0.98f, 0.90f, 0.94f), 3, true);
            SpawnCloud("Cloud_Horizon_Bank07",    sprDistantHorizon,  new Vector3( 90.0f,  5.8f, 14.5f), new Vector2(4.8f, 3.1f), new Color(1.0f, 0.97f, 0.88f, 0.93f), 3);
            SpawnCloud("Cloud_Horizon_Bank08",    sprLanaGoldenHoriz, new Vector3(106.0f,  6.2f, 14.0f), new Vector2(4.5f, 2.9f), new Color(1.0f, 0.98f, 0.94f, 0.94f), 3, true);
            SpawnCloud("Cloud_Horizon_Bank09",    sprLanaWideBank,    new Vector3(122.0f,  6.8f, 13.8f), new Vector2(4.4f, 2.8f), new Color(1.0f, 0.97f, 0.88f, 0.92f), 3);
            SpawnCloud("Cloud_Horizon_Bank10",    sprMidSoftBank03,   new Vector3(138.0f,  5.5f, 14.2f), new Vector2(4.2f, 2.7f), new Color(0.98f, 0.97f, 0.90f, 0.92f), 3, true);

            var follower = bgParent.GetComponent<LanaParallaxBackgroundFollower>();
            if (follower != null)
            {
                follower.RegisterLayer("PlanetOfLana_RichCloudscape", cloudGroup.transform, 0.88f, 0.04f);
            }
        }

        // =========================================================================================
        // 4. 3D SCULPTED TERRAIN LEDGES & ROCK-SOLID PHYSICS COLLIDERS (-12m to +138m)
        // =========================================================================================
        public static float SampleTerrainSurfaceY(float x)
        {
            // Zone 1A: Forest Clearing (-12.0m to +5.2m) — matches exact reference 50_User_Reference_Forest_Vista.jpg
            if (x < -3.8f)
            {
                float t = Mathf.Clamp01((x - (-5.5f)) / 1.7f);
                return Mathf.Lerp(-0.85f, -1.75f, t);
            }
            if (x >= -3.8f && x < 5.2f)
            {
                float t = (x - (-3.8f)) / 9.0f;
                return Mathf.Lerp(-1.75f, -1.95f, t);
            }
            // Zone 1B: Right Cliff Plateau (+5.2m to +14.6m) -> Y = -0.55m
            if (x >= 5.2f && x <= 14.6f)
            {
                return -0.55f;
            }
            // Zone 1C: Rocky Ravine Chasm (+14.6m to +17.6m) -> Floor at Y = -3.80m (deep chasm for swing rope puzzle)
            if (x > 14.6f && x < 17.6f)
            {
                return -3.80f;
            }
            // Zone 1D: East Ravine Landing Plateau (+17.6m to +20.6m) -> Y = -0.55m
            if (x >= 17.6f && x < 20.6f)
            {
                return -0.55f;
            }
            // Zone 1E: Stepped Descent to Fallen Tree Trunk (+20.6m to +22.0m) -> Steps from -0.55m to -1.10m
            if (x >= 20.6f && x < 22.0f)
            {
                float t = (x - 20.6f) / 1.4f;
                return Mathf.Lerp(-0.55f, -1.10f, SmoothCos(t));
            }
            // Zone 1F: Fallen Tree Crawl & Crate Push/Pull Arena (+22.0m to +35.5m) -> Strictly flat at Y = -1.10m for crate physics
            if (x >= 22.0f && x < 35.5f)
            {
                return -1.10f;
            }
            // Zone 2A: High Cliff Vista Overlook (+35.5m to +43.5m) -> Y = +2.15m
            if (x >= 35.5f && x < 43.5f)
            {
                return 2.15f;
            }
            // Zone 2B: Downhill Slope ("đi xuống dốc đồi", +43.5m to +62.0m) -> Smoothly descends from Y = +2.15m down to Y = -1.85m
            if (x >= 43.5f && x < 62.0f)
            {
                float t = (x - 43.5f) / (62.0f - 43.5f);
                return Mathf.Lerp(2.15f, -1.85f, SmoothCos(t));
            }
            // Zone 3 & Zone 4: Golden Rice Field (+62.0m to +95.0m) & Living Village (+95.0m to +138.0m)
            // Strictly 100% flat & horizontal at Y = -1.85m
            return -1.85f;
        }

        private static float SmoothCos(float t)
        {
            t = Mathf.Clamp01(t);
            return 0.5f - 0.5f * Mathf.Cos(t * Mathf.PI);
        }

        private static void BuildSculpted3DLedgesAndPhysics(LanaMaterialSet mats)
        {
            var terrainRoot = GameObject.Find("03_Terrain") ?? new GameObject("03_Terrain");
            var ledgesGroup = new GameObject("Lana_Sculpted_Ledges");
            ledgesGroup.transform.SetParent(terrainRoot.transform, false);

            CreateLanaLedgeSection(ledgesGroup.transform, "Ledge_01_ForestStart",
                -12.0f, 5.20f, 75, 0.05f, 1.10f, -9.5f, SampleTerrainSurfaceY, mats, true);

            CreateLanaLedgeSection(ledgesGroup.transform, "Ledge_02_RopeCliffPlateau",
                5.20f, 14.60f, 48, 0.05f, 1.10f, -9.5f, SampleTerrainSurfaceY, mats, true);

            CreateLanaLedgeSection(ledgesGroup.transform, "Ledge_03_RavineFloor",
                14.65f, 17.55f, 12, 0.05f, 1.10f, -9.5f, SampleTerrainSurfaceY, mats, false);

            // Dark rocky chasm backdrop wall closing the ravine gorge
            var gorgeBackGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
            gorgeBackGO.name = "Ravine_Chasm_BackdropWall";
            gorgeBackGO.transform.SetParent(terrainRoot.transform, false);
            gorgeBackGO.transform.position = new Vector3(16.10f, -2.50f, 2.50f);
            gorgeBackGO.transform.localScale = new Vector3(4.80f, 4.20f, 1.0f);
            gorgeBackGO.GetComponent<MeshRenderer>().sharedMaterial = mats.RockUnderbellyMat;
            UnityEngine.Object.DestroyImmediate(gorgeBackGO.GetComponent<Collider>());

            CreateLanaLedgeSection(ledgesGroup.transform, "Ledge_04_RavineEastToCrate",
                17.60f, 35.52f, 96, 0.05f, 1.10f, -9.5f, SampleTerrainSurfaceY, mats, true);

            CreateLanaLedgeSection(ledgesGroup.transform, "Ledge_05_HighOverlook_Hill_Rice_And_Village",
                35.50f, 138.0f, 420, 0.05f, 1.10f, -9.5f, SampleTerrainSurfaceY, mats, true);

            // Build precision physics colliders
            var physGroup = new GameObject("PhysicsWalkway_And_Cliffs_145m");
            physGroup.transform.SetParent(terrainRoot.transform, false);

            void AddBoxCol(string name, float minX, float maxX, float bottomY, float topY, float zSize = 4.0f)
            {
                var go = new GameObject(name);
                go.transform.SetParent(physGroup.transform, false);
                go.transform.position = new Vector3((minX + maxX) * 0.5f, (bottomY + topY) * 0.5f, 0f);
                var bc = go.AddComponent<BoxCollider>();
                bc.size = new Vector3(Mathf.Max(0.1f, maxX - minX), Mathf.Max(0.1f, topY - bottomY), zSize);
            }

            // Left boundary wall at X = -10.5m & Right boundary wall at X = +134.5m
            AddBoxCol("Wall_LeftBoundary",  -11.5f, -10.2f, -4.0f, 8.0f);
            AddBoxCol("Wall_RightBoundary", 134.5f, 136.0f, -4.0f, 8.0f);

            // 1. Forest Start Floor (-10.5m .. +5.20m, matches 50_User_Reference_Forest_Vista.jpg)
            AddBoxCol("Col_01_ForestStart", -10.5f, 5.20f, -8.0f, -1.95f);
            AddBoxCol("Col_01_LeftEmbankment", -10.5f, -4.50f, -8.0f, -0.85f);

            // 2. Right Cliff Plateau (+5.20m .. +14.60m, topY = -0.55m — vertical wall starts at X = 5.20m)
            AddBoxCol("Col_02_RopeCliffPlateau", 5.20f, 14.60f, -8.0f, -0.55f);

            // 3. Ravine Chasm Floor (+14.60m .. +17.60m, topY = -3.80m)
            AddBoxCol("Col_03_RavineFloor", 14.60f, 17.60f, -8.0f, -3.80f);

            // 4. East Ravine Plateau (+17.60m .. +20.60m, topY = -0.55m — vertical wall at X = 17.60m)
            AddBoxCol("Col_04_RavineEastPlateau", 17.60f, 20.60f, -8.0f, -0.55f);

            // 5. Stepped Descent to Fallen Tree (+20.60m .. +22.00m)
            AddBoxCol("Col_05_StepDown", 20.60f, 22.00f, -8.0f, -0.85f);

            // 6. Fallen Tree Crawl & Crate Push/Pull Floor (+22.00m .. +35.50m, topY = -1.10m)
            AddBoxCol("Col_06_CrawlAndCrateFloor", 22.00f, 35.50f, -8.0f, -1.10f);

            // 7. High Cliff Overlook Plateau (+35.50m .. +43.50m, topY = +2.15m)
            AddBoxCol("Col_07_HighOverlookPlateau", 35.50f, 43.50f, -8.0f, 2.15f);

            // 8. Smooth Downhill Slope (+43.50m .. +62.00m, Y: +2.15m -> -1.85m)
            int slopeSteps = 24;
            for (int s = 0; s < slopeSteps; s++)
            {
                float u0 = (float)s / slopeSteps;
                float u1 = (float)(s + 1) / slopeSteps;
                float x0 = Mathf.Lerp(43.50f, 62.00f, u0);
                float x1 = Mathf.Lerp(43.50f, 62.00f, u1);
                float y0 = SampleTerrainSurfaceY(x0);
                float y1 = SampleTerrainSurfaceY(x1);

                var segGO = new GameObject($"Col_08_HillSlopeSeg_{s:D2}");
                segGO.transform.SetParent(physGroup.transform, false);
                Vector3 p0 = new Vector3(x0, y0, 0f);
                Vector3 p1 = new Vector3(x1, y1, 0f);
                Vector3 mid = (p0 + p1) * 0.5f;
                float len = Vector3.Distance(p0, p1);
                float angleZ = Mathf.Atan2(y1 - y0, x1 - x0) * Mathf.Rad2Deg;

                segGO.transform.position = mid + new Vector3(0f, -0.5f, 0f);
                segGO.transform.rotation = Quaternion.Euler(0f, 0f, angleZ);
                var bc = segGO.AddComponent<BoxCollider>();
                bc.size = new Vector3(len + 0.08f, 1.0f, 4.0f);
            }

            // 9. Flat Horizontal Golden Rice Field & Living Village Platform (+62.00m .. +138.00m, topY = -1.85m)
            AddBoxCol("Col_09_FlatRiceAndVillagePlatform", 61.90f, 138.0f, -8.0f, -1.85f);
        }

        // =========================================================================================
        // 5. FOREST MANUAL OBSTACLES & GAMEPLAY (UNBLOCKED CLIFF ROPE, RAVINE SWING, CRAWL, CRATE)
        // =========================================================================================
        private static void BuildForestManualObstaclesAndGameplay(LanaMaterialSet mats)
        {
            var gpRoot = GameObject.Find("02_Gameplay") ?? new GameObject("02_Gameplay");

            // Position Player (An) at Forest Start Clearing (X = -2.2m, Y = -1.82m, Z = 0m)
            var playerGO = GameObject.Find("PF_Player_Minh") ?? GameObject.Find("Character_An");
            if (playerGO != null)
            {
                var cc = playerGO.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                playerGO.transform.position = new Vector3(-2.2f, -1.82f, 0f);
                if (cc != null) cc.enabled = true;

                var pAnim = playerGO.GetComponentInChildren<Animator>();
                if (pAnim != null)
                {
                    pAnim.Play("Idle", 0, 0.15f);
                    pAnim.Update(0.05f);
                }
            }

            var exitTrig = GameObject.Find("Scene1_ExitGoalTrigger_60m");
            if (exitTrig != null)
            {
                exitTrig.transform.position = new Vector3(132.5f, -0.50f, 0f);
            }

            var oldTut = GameObject.Find("00_UI_PlanetOfLana_TutorialController");
            if (oldTut != null) UnityEngine.Object.DestroyImmediate(oldTut);

            var oldObstacles = gpRoot.transform.Find("ForestManual_Obstacles");
            if (oldObstacles != null) UnityEngine.Object.DestroyImmediate(oldObstacles.gameObject);

            var obstaclesRoot = new GameObject("ForestManual_Obstacles");
            obstaclesRoot.transform.SetParent(gpRoot.transform, false);

            var prefabBoulder = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/PF_MossyBoulder.prefab");

            GameObject Spawn3DBoulder(string name, Transform parent, Vector3 pos, Vector3 scale, float rotY, bool hasCollider = false)
            {
                if (prefabBoulder == null) return null;
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefabBoulder, parent);
                inst.name = name;
                inst.transform.position = pos;
                inst.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
                inst.transform.localScale = scale;
                foreach (var mr in inst.GetComponentsInChildren<MeshRenderer>(true))
                {
                    mr.sharedMaterial = mats.MossyBoulderMat;
                }
                foreach (var col in inst.GetComponentsInChildren<Collider>(true))
                {
                    col.enabled = hasCollider;
                }
                return inst;
            }

            // --- OBSTACLE 1: NATURAL TREE BOUGH ROPE CLIMB IN FRONT OF ROCK CLIFF (X = 4.88m) ---
            // As in Planet of Lana: An ancient banyan tree stands on the cliff ridge (X = 5.75m, Y = -0.55m).
            // A heavy curved bough reaches out over the cliff at Y = +3.65m.
            // The climbing rope hangs naturally in free air in front of the rock cliff with 32cm clearance.
            Spawn3DBoulder("RopeCliff_TopLipBoulder", obstaclesRoot.transform, new Vector3(5.60f, -0.48f, 0.85f), new Vector3(1.35f, 0.60f, 1.15f), 25f, false);
            Spawn3DBoulder("RopeCliff_WallSlab_Lower", obstaclesRoot.transform, new Vector3(5.45f, -1.35f, 0.85f), new Vector3(1.25f, 0.95f, 1.05f), 205f, false);
            Spawn3DBoulder("RopeCliff_BaseOutcrop", obstaclesRoot.transform, new Vector3(5.15f, -1.90f, 0.85f), new Vector3(1.10f, 0.65f, 0.95f), 15f, false);

            // Natural hand-painted watercolor bough extending from cliff plateau to hold the climbing rope
            var cliffBoughGO = new GameObject("Obstacle01_CliffBough_Watercolor");
            cliffBoughGO.transform.SetParent(obstaclesRoot.transform, false);
            cliffBoughGO.transform.position = new Vector3(5.75f, 3.80f, -0.15f);
            cliffBoughGO.transform.rotation = Quaternion.Euler(0f, 0f, 10f);
            cliffBoughGO.transform.localScale = new Vector3(-1.25f, 1.15f, 1f); // Flipped horizontally to arch westward over cliff
            var cliffBoughSR = cliffBoughGO.AddComponent<SpriteRenderer>();
            cliffBoughSR.sprite = LoadSprite($"{UnifiedTexRoot}/Branches/T_Lana_Branch_01_Overhanging.png", new Vector2(0.5f, 0.5f));
            cliffBoughSR.sharedMaterial = GetOrCreateSpriteMat(cliffBoughSR.sprite, mats.SpriteDefaultMat);
            cliffBoughSR.sortingOrder = 25;
            cliffBoughSR.color = new Color(0.92f, 0.96f, 0.90f, 1f);

            var ropeAnchorGO = new GameObject("RopeAnchor_Transform");
            ropeAnchorGO.transform.SetParent(obstaclesRoot.transform, false);
            ropeAnchorGO.transform.position = new Vector3(4.88f, 3.65f, -0.15f);

            var ropeGO = new GameObject("Obstacle01_CliffRope");
            ropeGO.transform.SetParent(obstaclesRoot.transform, false);
            ropeGO.transform.position = new Vector3(4.88f, 3.65f, -0.15f);
            var ropeCtrl = ropeGO.AddComponent<RopeController>();
            var ropeSO = new SerializedObject(ropeCtrl);
            ropeSO.FindProperty("totalLength").floatValue = 4.60f;
            ropeSO.FindProperty("nodeCount").intValue = 18;
            ropeSO.FindProperty("ropeVisualWidth").floatValue = 0.085f;
            ropeSO.ApplyModifiedPropertiesWithoutUndo();
            ropeCtrl.InitializeRope();

            // --- OBSTACLE 2: ROCKY RAVINE CHASM & 4.6M SWING ROPE (X = 14.6m .. 17.6m) ---
            // West lip at X = 14.60m, Y = -0.55m. East lip at X = 17.60m, Y = -0.55m.
            // Ravine chasm floor is deep at Y = -3.80m.
            // A living Hero Banyan Tree rooted firmly on the west cliff lip extends its massive watercolor bough across the gorge to hold the swing rope!
            var ravineTreeGO = new GameObject("Obstacle02_Ravine_Hero_AnchorBanyan");
            ravineTreeGO.transform.SetParent(obstaclesRoot.transform, false);
            ravineTreeGO.transform.position = new Vector3(13.60f, -0.95f, 0.40f);

            // 1. Rooted Hero Banyan Tree on the west ravine lip
            var ravineTreeVisualGO = new GameObject("Ravine_Hero_Banyan_TreeSprite");
            ravineTreeVisualGO.transform.SetParent(ravineTreeGO.transform, false);
            ravineTreeVisualGO.transform.localPosition = new Vector3(0f, 0f, 0f);
            ravineTreeVisualGO.transform.localScale = new Vector3(1.40f, 1.45f, 1f);
            var ravineTreeSR = ravineTreeVisualGO.AddComponent<SpriteRenderer>();
            ravineTreeSR.sprite = LoadSprite($"{UnifiedTexRoot}/Trees/T_Lana_Tree_01_HeroBanyan.png", new Vector2(0.5f, 0.02f))
                               ?? LoadSprite("Assets/Textures/Tree/Curved_Tree_V2_Full.png", new Vector2(0.5f, 0.02f));
            ravineTreeSR.sharedMaterial = GetOrCreateSpriteMat(ravineTreeSR.sprite, mats.SpriteDefaultMat);
            ravineTreeSR.sortingOrder = 11;
            ravineTreeSR.color = new Color(0.92f, 0.96f, 0.90f, 1f);

            // 2. Majestic watercolor overhanging bough arching eastwards across the ravine chasm
            var ravineBoughGO = new GameObject("Ravine_OverhangingBough_Watercolor");
            ravineBoughGO.transform.SetParent(ravineTreeGO.transform, false);
            ravineBoughGO.transform.position = new Vector3(15.20f, 3.65f, -0.15f);
            ravineBoughGO.transform.rotation = Quaternion.Euler(0f, 0f, -6f);
            ravineBoughGO.transform.localScale = new Vector3(1.40f, 1.30f, 1f);
            var ravineBoughSR = ravineBoughGO.AddComponent<SpriteRenderer>();
            ravineBoughSR.sprite = LoadSprite($"{UnifiedTexRoot}/Branches/T_Lana_Branch_01_Overhanging.png", new Vector2(0.5f, 0.5f));
            ravineBoughSR.sharedMaterial = GetOrCreateSpriteMat(ravineBoughSR.sprite, mats.SpriteDefaultMat);
            ravineBoughSR.sortingOrder = 25;
            ravineBoughSR.color = new Color(0.92f, 0.96f, 0.90f, 1f);

            var swingRopeGO = new GameObject("Obstacle02_RavineSwingRope");
            swingRopeGO.transform.SetParent(obstaclesRoot.transform, false);
            swingRopeGO.transform.position = new Vector3(16.10f, 3.50f, -0.15f);
            var swingRopeCtrl = swingRopeGO.AddComponent<RopeController>();
            var swingRopeSO = new SerializedObject(swingRopeCtrl);
            swingRopeSO.FindProperty("totalLength").floatValue = 4.60f;
            swingRopeSO.FindProperty("nodeCount").intValue = 16;
            swingRopeSO.FindProperty("ropeVisualWidth").floatValue = 0.085f;
            swingRopeSO.ApplyModifiedPropertiesWithoutUndo();
            swingRopeCtrl.InitializeRope();

            Spawn3DBoulder("Obstacle02_RavineWestLipRock", obstaclesRoot.transform, new Vector3(14.40f, -0.55f, 0.45f), new Vector3(1.10f, 0.55f, 0.90f), 15f, false);
            Spawn3DBoulder("Obstacle02_RavineEastLipRock", obstaclesRoot.transform, new Vector3(17.70f, -0.55f, 0.45f), new Vector3(1.10f, 0.55f, 0.90f), 165f, false);

            // --- OBSTACLE 3: AUTHENTIC SEGMENTED FALLEN TREE TRUNK & ROOTS CRAWL TUNNEL (X = 23.4m .. 27.2m) ---
            var fallenTreeRoot = new GameObject("Obstacle03_FallenTreeTrunk_Crawl");
            fallenTreeRoot.transform.SetParent(obstaclesRoot.transform, false);
            fallenTreeRoot.transform.position = new Vector3(25.10f, -1.10f, 0f);

            // Segment 1: Broken jagged root stump on the left
            var stumpGO = new GameObject("FallenTrunk_Segment01_BrokenStump");
            stumpGO.transform.SetParent(fallenTreeRoot.transform, false);
            stumpGO.transform.localPosition = new Vector3(-1.65f, 0.35f, 0.15f);
            stumpGO.transform.localRotation = Quaternion.Euler(0f, 0f, 18f);
            stumpGO.transform.localScale = new Vector3(1.15f, 1.15f, 1f);
            var stumpSR = stumpGO.AddComponent<SpriteRenderer>();
            stumpSR.sprite = LoadSprite("Assets/Textures/Tree/goc cay 2.png", new Vector2(0.5f, 0.05f))
                          ?? LoadSprite("Assets/Textures/Tree/goc cay.png", new Vector2(0.5f, 0.05f))
                          ?? LoadSprite($"{UnifiedTexRoot}/Trees/T_Lana_Tree_Modular_Trunk.png", new Vector2(0.5f, 0.05f));
            stumpSR.sharedMaterial = GetOrCreateSpriteMat(stumpSR.sprite, mats.SpriteDefaultMat);
            stumpSR.sortingOrder = 15;
            stumpSR.color = new Color(0.92f, 0.96f, 0.90f, 1f);

            // Segment 2: Main hollow fallen tree trunk arching across the crawl path
            var logGO = new GameObject("FallenTrunk_Segment02_HollowTrunk");
            logGO.transform.SetParent(fallenTreeRoot.transform, false);
            logGO.transform.localPosition = new Vector3(0f, 0.92f, 0.05f);
            logGO.transform.localRotation = Quaternion.Euler(0f, 0f, -3f);
            logGO.transform.localScale = new Vector3(1.30f, 1.15f, 1f);
            var logSR = logGO.AddComponent<SpriteRenderer>();
            logSR.sprite = LoadSprite($"{UnifiedTexRoot}/Branches/T_Lana_FallenTrunk_Mossy.png", new Vector2(0.5f, 0.1f));
            logSR.sharedMaterial = GetOrCreateSpriteMat(logSR.sprite, mats.SpriteDefaultMat);
            logSR.sortingOrder = 24;
            logSR.color = new Color(0.92f, 0.96f, 0.90f, 1f);

            // Segment 3: Shattered crown branch fork smashing into the right boulders
            var crownGO = new GameObject("FallenTrunk_Segment03_ShatteredCrown");
            crownGO.transform.SetParent(fallenTreeRoot.transform, false);
            crownGO.transform.localPosition = new Vector3(1.70f, 0.55f, 0.12f);
            crownGO.transform.localRotation = Quaternion.Euler(0f, 0f, -38f);
            crownGO.transform.localScale = new Vector3(1.05f, 1.05f, 1f);
            var crownSR = crownGO.AddComponent<SpriteRenderer>();
            crownSR.sprite = LoadSprite($"{UnifiedTexRoot}/Branches/T_Lana_Branch_01_Overhanging.png", new Vector2(0.5f, 0.5f));
            crownSR.sharedMaterial = GetOrCreateSpriteMat(crownSR.sprite, mats.SpriteDefaultMat);
            crownSR.sortingOrder = 16;
            crownSR.color = new Color(0.90f, 0.94f, 0.88f, 1f);

            // Low-ceiling obstacle collider: clearance height = 0.92m (forces An into crawl posture!)
            var trunkColGO = new GameObject("FallenTrunk_LowCeilingCollider");
            trunkColGO.transform.SetParent(fallenTreeRoot.transform, false);
            trunkColGO.transform.localPosition = new Vector3(0f, 1.45f, 0f);
            var trunkBC = trunkColGO.AddComponent<BoxCollider>();
            trunkBC.size = new Vector3(3.20f, 1.30f, 3.50f);

            // Mossy footing boulders on each side supporting the trunk ends
            Spawn3DBoulder("FallenTrunk_RootRock_LeftBase",  fallenTreeRoot.transform, new Vector3(23.60f, -1.10f,  0.45f), new Vector3(1.35f, 0.75f, 1.15f),  25f, false);
            Spawn3DBoulder("FallenTrunk_RootRock_RightBase", fallenTreeRoot.transform, new Vector3(26.60f, -1.10f,  0.45f), new Vector3(1.35f, 0.75f, 1.15f), 195f, false);

            // --- OBSTACLE 4: 3D PUSH/PULL WOODEN CRATE (X = 30.6m, Y = -1.10m -> Push to Cliff Corner at X = 34.6m) ---
            var crateModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Props/SM_WoodenSlatCube.fbx")
                          ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Props/SM_WoodenCrate.fbx")
                          ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Props/PF_Prop_CargoCrate_3D.prefab");
            var crateMat   = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Props/M_WoodenCrate_Square.mat")
                          ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Props/M_WoodenCrate_01.mat")
                          ?? mats.TimberDarkMat;

            var crateGO = new GameObject("Obstacle04_PushPull_WoodenCrate");
            crateGO.transform.SetParent(obstaclesRoot.transform, false);
            crateGO.transform.position = new Vector3(30.60f, -0.71f, 0f);
            crateGO.transform.rotation = Quaternion.identity;

            if (crateModel != null)
            {
                var visualInst = (GameObject)PrefabUtility.InstantiatePrefab(crateModel, crateGO.transform);
                if (PrefabUtility.IsPartOfPrefabInstance(visualInst))
                {
                    PrefabUtility.UnpackPrefabInstance(visualInst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                }
                visualInst.name = "WoodenCrate_3DMesh";
                visualInst.transform.localRotation = Quaternion.identity;

                // Proportional puzzle crate: strictly normalized to 0.78m height (waist-height to An)
                float targetH = 0.78f;
                var mf = visualInst.GetComponentInChildren<MeshFilter>();
                if (mf != null && mf.sharedMesh != null && mf.sharedMesh.bounds.size.y > 0.0001f)
                {
                    float meshH = mf.sharedMesh.bounds.size.y;
                    float s = targetH / meshH;
                    visualInst.transform.localScale = new Vector3(s, s, s);
                    visualInst.transform.localPosition = Vector3.zero;
                }
                else
                {
                    visualInst.transform.localScale = new Vector3(0.78f, 0.78f, 0.78f);
                    visualInst.transform.localPosition = Vector3.zero;
                }
                foreach (var embCam in visualInst.GetComponentsInChildren<Camera>(true)) UnityEngine.Object.DestroyImmediate(embCam.gameObject);
                foreach (var embLight in visualInst.GetComponentsInChildren<Light>(true)) UnityEngine.Object.DestroyImmediate(embLight.gameObject);
                foreach (var col in visualInst.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(col);
                foreach (var mr in visualInst.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (crateMat != null) mr.sharedMaterial = crateMat;
                }
            }
            else
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.SetParent(crateGO.transform, false);
                cube.transform.localScale = new Vector3(0.78f, 0.78f, 0.78f);
                UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>());
                if (crateMat != null) cube.GetComponent<MeshRenderer>().sharedMaterial = crateMat;
            }

            var crateBC = crateGO.AddComponent<BoxCollider>();
            crateBC.size = new Vector3(0.78f, 0.78f, 0.78f);
            crateBC.center = Vector3.zero;

            var crateRB = crateGO.AddComponent<Rigidbody>();
            crateRB.mass = 22f;
            crateRB.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;

            crateGO.AddComponent<InteractableCrate>();

            // --- ZONE 2 VISTA OVERLOOK PANORAMA TRIGGER (X = 36.0m .. 45.0m, Y = +2.15m) ---
            var panoGO = new GameObject("Zone2_HillAndVillageOverlook_PanoramaTrigger");
            panoGO.transform.SetParent(obstaclesRoot.transform, false);
            panoGO.transform.position = new Vector3(40.0f, 3.5f, 0f);
            var panoCol = panoGO.AddComponent<BoxCollider>();
            panoCol.isTrigger = true;
            panoCol.size = new Vector3(9.5f, 5.0f, 4.0f);
            var panoZone = panoGO.AddComponent<CameraPanoramaZone>();
            var panoSO = new SerializedObject(panoZone);
            var propFov = panoSO.FindProperty("panoramaFov");
            if (propFov != null) propFov.floatValue = 47.0f;
            var propTargetY = panoSO.FindProperty("targetOffsetY");
            if (propTargetY != null) propTargetY.floatValue = 2.65f;
            var propSpeed = panoSO.FindProperty("transitionSpeed");
            if (propSpeed != null) propSpeed.floatValue = 1.8f;
            panoSO.ApplyModifiedPropertiesWithoutUndo();

            // --- ZONE 3 VAST GOLDEN RICE FIELD BREATHTAKING PANORAMA TRIGGER (X = 62.0m .. 98.0m, Y = -1.85m) ---
            var ricePanoGO = new GameObject("Zone3_GoldenRiceField_BreathtakingPanoramaTrigger");
            ricePanoGO.transform.SetParent(obstaclesRoot.transform, false);
            ricePanoGO.transform.position = new Vector3(78.5f, 1.50f, 0f);
            var ricePanoCol = ricePanoGO.AddComponent<BoxCollider>();
            ricePanoCol.isTrigger = true;
            ricePanoCol.size = new Vector3(36.0f, 6.0f, 4.0f);
            var ricePanoZone = ricePanoGO.AddComponent<CameraPanoramaZone>();
            var ricePanoSO = new SerializedObject(ricePanoZone);
            var propRiceFov = ricePanoSO.FindProperty("panoramaFov");
            if (propRiceFov != null) propRiceFov.floatValue = 44.5f;
            var propRiceTargetY = ricePanoSO.FindProperty("targetOffsetY");
            if (propRiceTargetY != null) propRiceTargetY.floatValue = 2.40f;
            var propRiceSpeed = ricePanoSO.FindProperty("transitionSpeed");
            if (propRiceSpeed != null) propRiceSpeed.floatValue = 1.35f;
            ricePanoSO.ApplyModifiedPropertiesWithoutUndo();

            // --- PLANET OF LANA CONTEXTUAL DIEGETIC PROMPT SYSTEM & 5-STEP MANUAL TRIGGERS ---
            SetupPlanetOfLanaContextualPrompts(obstaclesRoot);
        }

        private static void SetupPlanetOfLanaContextualPrompts(GameObject obstaclesRoot)
        {
            var uiRoot = GameObject.Find("02_UI") ?? new GameObject("02_UI");
            var promptSys = uiRoot.GetComponentInChildren<PlanetOfLanaPromptSystem>();
            if (promptSys == null)
            {
                var promptSysGO = new GameObject("PlanetOfLanaPromptSystem");
                promptSysGO.transform.SetParent(uiRoot.transform, false);
                promptSysGO.AddComponent<PlanetOfLanaPromptSystem>();
            }

            var promptsGroup = new GameObject("PlanetOfLana_ContextualPrompts");
            promptsGroup.transform.SetParent(obstaclesRoot.transform, false);

            void SpawnPromptTrigger(string name, Vector3 pos, Vector3 size, PlanetOfLanaPromptSystem.PromptType type)
            {
                var go = new GameObject(name);
                go.transform.SetParent(promptsGroup.transform, false);
                go.transform.position = pos;
                var col = go.AddComponent<BoxCollider>();
                col.isTrigger = true;
                col.size = size;

                var promptTrig = go.AddComponent<ContextualPromptTrigger>();
                promptTrig.Type = type;
                promptTrig.WorldAnchor = go.transform;
                promptTrig.ShowOnceOnly = true;
            }

            // 0. Forest Clearing Movement
            SpawnPromptTrigger("Prompt_00_Move_ForestClearing", new Vector3(-3.0f, -1.20f, 0f), new Vector3(4.5f, 3.5f, 4.0f), PlanetOfLanaPromptSystem.PromptType.Move);

            // 1. Cliff Rope Grab (Centered at X = 4.80m, Y = -0.85m in free air)
            SpawnPromptTrigger("Prompt_01_RopeGrab_Cliff", new Vector3(4.80f, -0.85f, 0f), new Vector3(3.2f, 3.5f, 4.0f), PlanetOfLanaPromptSystem.PromptType.RopeGrab);

            // 2. Rocky Ravine Jump / Swing (West lip at X = 14.0m)
            SpawnPromptTrigger("Prompt_02_Jump_RockyRavine", new Vector3(14.0f, -0.20f, 0f), new Vector3(3.2f, 3.5f, 4.0f), PlanetOfLanaPromptSystem.PromptType.Jump);

            // 3. Fallen Tree Trunk Crawl
            SpawnPromptTrigger("Prompt_03_Crawl_FallenTrunk", new Vector3(22.80f, -0.40f, 0f), new Vector3(3.2f, 3.5f, 4.0f), PlanetOfLanaPromptSystem.PromptType.Crawl);

            // 4. Push/Pull Wooden Crate
            SpawnPromptTrigger("Prompt_04_PushPull_WoodenCrate", new Vector3(30.20f, -0.45f, 0f), new Vector3(3.5f, 3.5f, 4.0f), PlanetOfLanaPromptSystem.PromptType.PushPull);

            // 5. High Cliff Ledge Climb
            SpawnPromptTrigger("Prompt_05_ClimbLedge_HighCliff", new Vector3(34.20f, 1.25f, 0f), new Vector3(2.5f, 3.5f, 4.0f), PlanetOfLanaPromptSystem.PromptType.ClimbLedge);

            // 6. Zone 4 Village Interaction
            SpawnPromptTrigger("Prompt_06_Talk_VillagerNPC", new Vector3(99.50f, -0.80f, 0f), new Vector3(3.5f, 3.5f, 4.0f), PlanetOfLanaPromptSystem.PromptType.Interact);
        }

        private static void SetupPeacefulForestPollenVFX()
        {
            var vfxRoot = GameObject.Find("09_VFX") ?? new GameObject("09_VFX");
            var oldEmber = vfxRoot.transform.Find("WarScorched_EmberMotes");
            if (oldEmber != null) UnityEngine.Object.DestroyImmediate(oldEmber.gameObject);
            var oldPollen = vfxRoot.transform.Find("Peaceful_ForestPollen_VFX");
            if (oldPollen != null) UnityEngine.Object.DestroyImmediate(oldPollen.gameObject);

            var pollenGO = new GameObject("Peaceful_ForestPollen_VFX");
            pollenGO.transform.SetParent(vfxRoot.transform, false);
            pollenGO.transform.position = new Vector3(-0.5f, -1.20f, 0.5f);

            var ps = pollenGO.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 6.0f;
            main.startSpeed = 0.20f;
            main.startSize = 0.05f;
            // Soft peaceful golden morning forest dust/pollen - zero fiery embers!
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1.0f, 0.96f, 0.82f, 0.35f), new Color(1.0f, 1.0f, 0.90f, 0.45f));
            main.maxParticles = 40;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 8f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(12.0f, 2.5f, 3.5f);

            var velOverLifetime = ps.velocityOverLifetime;
            velOverLifetime.enabled = true;
            velOverLifetime.x = new ParticleSystem.MinMaxCurve(0.04f, 0.18f);
            velOverLifetime.y = new ParticleSystem.MinMaxCurve(-0.04f, 0.08f);
            velOverLifetime.z = new ParticleSystem.MinMaxCurve(-0.04f, 0.04f);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.98f, 0.88f), 0f), new GradientColorKey(new Color(1f, 0.95f, 0.78f), 0.5f), new GradientColorKey(new Color(1f, 1f, 1f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.40f, 0.3f), new GradientAlphaKey(0.35f, 0.7f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = grad;

            var psr = pollenGO.GetComponent<ParticleSystemRenderer>();
            psr.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");
            psr.sortingOrder = 22;
        }

        // =========================================================================================
        // 6. POPULATE 100% UNIFIED BORDERLESS PLANET OF LANA 2D ECOSYSTEM
        //    (Strictly grounded via SampleTerrainSurfaceY, standardized proportional scales, zero floating props)
        // =========================================================================================
        private static void PopulateUnifiedPlanetOfLanaEcosystem(LanaMaterialSet mats)
        {
            var mgRoot = GameObject.Find("05_Midground") ?? new GameObject("05_Midground");
            var fgRoot = GameObject.Find("04_Foreground") ?? new GameObject("04_Foreground");

            var ecoRoot = new GameObject("Lana_HandDrawn_Ecosystem");
            ecoRoot.transform.SetParent(mgRoot.transform, false);

            var treesGroup   = new GameObject("HandDrawn_HeroTrees");   treesGroup.transform.SetParent(ecoRoot.transform, false);
            var bushesGroup  = new GameObject("HandDrawn_Bushes");      bushesGroup.transform.SetParent(ecoRoot.transform, false);
            var grassGroup   = new GameObject("HandDrawn_MeadowGrass"); grassGroup.transform.SetParent(ecoRoot.transform, false);
            var riceGroup    = new GameObject("Golden_RiceField_2D");   riceGroup.transform.SetParent(ecoRoot.transform, false);
            var rocksGroup   = new GameObject("Sculpted_MossyBoulders");rocksGroup.transform.SetParent(ecoRoot.transform, false);

            var fgDescentGroup = new GameObject("FG_SkyToForest_DescentBushes");
            fgDescentGroup.transform.SetParent(fgRoot.transform, false);

            // Tree Sprites - Dedicated 3-Depth Layering System (Planet of Lana Standard)
            Sprite sprTreeForeground1 = LoadSprite("Assets/Textures/Tree/truoc 1.png", new Vector2(0.5f, 0.02f));
            Sprite sprTreeMidground1  = LoadSprite("Assets/Textures/Tree/trung 1.png", new Vector2(0.5f, 0.02f));
            Sprite sprTreeBackground1 = LoadSprite("Assets/Textures/Tree/sau 1.png", new Vector2(0.5f, 0.05f));
            Sprite sprCay1            = LoadSprite("Assets/Textures/Tree/cay 1.png", new Vector2(0.5f, 0.02f));
            Sprite sprCay2            = LoadSprite("Assets/Textures/Tree/cay 2.png", new Vector2(0.5f, 0.02f));
            Sprite sprCay3            = LoadSprite("Assets/Textures/Tree/cay 3.png", new Vector2(0.5f, 0.02f));
            Sprite sprCay4            = LoadSprite("Assets/Textures/Tree/cay 4.png", new Vector2(0.5f, 0.02f));
            Sprite sprCay5            = LoadSprite("Assets/Textures/Tree/cay 5.png", new Vector2(0.5f, 0.02f));

            Sprite sprTreeFullIsolated = LoadSprite("Assets/Textures/Tree/Tree_Full_Isolated.png", new Vector2(0.5f, 0.02f));
            Sprite sprCurvedTreeV2Full = LoadSprite("Assets/Textures/Tree/Curved_Tree_V2_Full.png", new Vector2(0.5f, 0.02f));
            Sprite sprTreeSlenderHigh  = LoadSprite("Assets/Textures/Tree/Tree_Lana_SlenderHigh.png", new Vector2(0.5f, 0.02f));
            Sprite sprTreeWideCanopy   = LoadSprite("Assets/Textures/Tree/Tree_Lana_WideCanopy.png", new Vector2(0.5f, 0.02f));
            Sprite sprTreePoplarTall   = LoadSprite("Assets/Textures/Tree/Tree_Lana_PoplarTall.png", new Vector2(0.5f, 0.02f));
            Sprite sprMidgroundHills   = sprTreeBackground1 ?? LoadSprite("Assets/Textures/Tree/sau 1.png", new Vector2(0.5f, 0.10f));
            Sprite sprRepoussoirTop    = LoadSprite("Assets/Textures/Tree/repoussoir_tree_framing.png", new Vector2(0.5f, 0.5f));

            Sprite sprBranchOver       = LoadSprite($"{UnifiedTexRoot}/Branches/T_Lana_Branch_01_Overhanging.png", new Vector2(0.5f, 0.5f));
            Sprite sprFallenTrunkMossy = LoadSprite($"{UnifiedTexRoot}/Branches/T_Lana_FallenTrunk_Mossy.png", new Vector2(0.5f, 0.1f));

            // Lana VN Pack Trees matching 50_User_Reference_Forest_Vista.jpg 1:1
            Sprite sprTree01Large   = LoadSprite($"{PackTexRoot}/Trees/T_LanaVN_Tree_01_LargeCanopy.png", new Vector2(0.5f, 0.02f))
                                   ?? LoadSprite($"{UnifiedTexRoot}/Trees/T_Lana_Tree_01_HeroBanyan.png", new Vector2(0.5f, 0.02f))
                                   ?? sprTreeFullIsolated;
            Sprite sprTree02Medium  = LoadSprite($"{PackTexRoot}/Trees/T_LanaVN_Tree_02_Medium.png", new Vector2(0.5f, 0.02f)) ?? sprCurvedTreeV2Full;
            Sprite sprTree03Small   = LoadSprite($"{PackTexRoot}/Trees/T_LanaVN_Tree_03_SmallBushTree.png", new Vector2(0.5f, 0.02f)) ?? sprTreeSlenderHigh;
            Sprite sprTree04Tall    = LoadSprite($"{PackTexRoot}/Trees/T_LanaVN_Tree_04_SlenderTall.png", new Vector2(0.5f, 0.02f)) ?? sprTreeSlenderHigh;
            Sprite sprTree05Banana  = LoadSprite($"{PackTexRoot}/Trees/T_LanaVN_Tree_05_Banana.png", new Vector2(0.5f, 0.02f));
            Sprite sprTree06Coconut = LoadSprite($"{PackTexRoot}/Trees/T_LanaVN_Tree_06_Coconut.png", new Vector2(0.45f, 0.02f));
            Sprite sprTree07Bamboo  = LoadSprite($"{PackTexRoot}/Trees/T_LanaVN_Tree_07_BambooGrove.png", new Vector2(0.5f, 0.02f));

            // Lana VN Rocks matching 50_User_Reference_Forest_Vista.jpg 1:1
            Sprite sprRock01Large   = LoadSprite($"{PackTexRoot}/Rocks/T_LanaVN_Rock_01_LargeOutcrop.png", new Vector2(0.5f, 0.10f));
            Sprite sprRock02MossyTop= LoadSprite($"{PackTexRoot}/Rocks/T_LanaVN_Rock_02_MossyMediumTop.png", new Vector2(0.5f, 0.10f));
            Sprite sprRock03MossyBot= LoadSprite($"{PackTexRoot}/Rocks/T_LanaVN_Rock_03_MossyMediumBot.png", new Vector2(0.5f, 0.10f));
            Sprite sprRock04FlatSlab= LoadSprite($"{PackTexRoot}/Rocks/T_LanaVN_Rock_04_FlatSlab.png", new Vector2(0.5f, 0.10f));
            Sprite sprRock05Small   = LoadSprite($"{PackTexRoot}/Rocks/T_LanaVN_Rock_05_SmallMossy.png", new Vector2(0.5f, 0.10f));
            Sprite sprRock06PebblesTop = LoadSprite($"{PackTexRoot}/Rocks/T_LanaVN_Rock_06_PebbleGroupTop.png", new Vector2(0.5f, 0.10f));
            Sprite sprRock07PebblesBot = LoadSprite($"{PackTexRoot}/Rocks/T_LanaVN_Rock_07_PebbleGroupBot.png", new Vector2(0.5f, 0.10f));

            Sprite sprTreeGold01    = LoadSprite($"{UnifiedTexRoot}/Trees/T_Lana_Tree_GoldenVillage_01.png", new Vector2(0.5f, 0.02f)) ?? sprTreeFullIsolated;
            Sprite sprTreeGold02    = LoadSprite($"{UnifiedTexRoot}/Trees/T_Lana_Tree_GoldenVillage_02.png", new Vector2(0.5f, 0.02f)) ?? sprCurvedTreeV2Full;

            // Authentic Planet of Lana War-Scorched, Cliff & Repoussoir Assets
            Sprite sprTreeDefoliated = LoadSprite("Assets/Textures/unsure/Cropped_And_Mock/T_Tree_Defoliated_Alpha.png", new Vector2(0.5f, 0.02f));
            Sprite sprTrunkModular   = LoadSprite($"{UnifiedTexRoot}/Trees/T_Lana_Tree_Modular_Trunk.png", new Vector2(0.5f, 0.02f));
            Sprite sprCutoutBranch1  = LoadSprite("Assets/Textures/unsure/Cropped_And_Mock/T_Cutout_FramingBranch_01.png", new Vector2(0.5f, 0.5f));
            Sprite sprCutoutBranch2  = LoadSprite("Assets/Textures/unsure/Cropped_And_Mock/T_Cutout_FramingBranch_02.png", new Vector2(0.5f, 0.5f));
            Sprite sprCliffShelf     = LoadSprite("Assets/Textures/Environment/cliff_shelf.png", new Vector2(0.5f, 0.5f));
            Sprite sprHDRock01Large  = LoadSprite("Assets/Textures/Environment/Lana_Concept_Assets/Rocks/T_HD_Rock_01_LargeCliffOutcrop.png", new Vector2(0.5f, 0.10f)) ?? sprRock01Large;
            Sprite sprHDRock04Flat   = LoadSprite("Assets/Textures/Environment/Lana_Concept_Assets/Rocks/T_HD_Rock_04_FlatSlab.png", new Vector2(0.5f, 0.10f)) ?? sprRock04FlatSlab;

            Sprite[] sprBushes = new[]
            {
                LoadSprite($"{PackTexRoot}/Bushes/T_LanaVN_Bush_Low_01.png", new Vector2(0.5f, 0.04f)),
                LoadSprite($"{PackTexRoot}/Bushes/T_LanaVN_Bush_Low_02_Flower.png", new Vector2(0.5f, 0.04f)),
                LoadSprite($"{PackTexRoot}/Bushes/T_LanaVN_Bush_Low_03.png", new Vector2(0.5f, 0.04f)),
                LoadSprite($"{PackTexRoot}/Bushes/T_LanaVN_Bush_Low_04.png", new Vector2(0.5f, 0.04f)),
                LoadSprite($"{PackTexRoot}/Bushes/T_LanaVN_Bush_Broadleaf_01.png", new Vector2(0.5f, 0.04f)),
                LoadSprite($"{PackTexRoot}/Bushes/T_LanaVN_Bush_Broadleaf_02.png", new Vector2(0.5f, 0.04f))
            };
            Sprite sprBushBroad1 = LoadSprite($"{PackTexRoot}/Bushes/T_LanaVN_Bush_Broadleaf_01.png", new Vector2(0.5f, 0.04f));
            Sprite sprBushBroad2 = LoadSprite($"{PackTexRoot}/Bushes/T_LanaVN_Bush_Broadleaf_02.png", new Vector2(0.5f, 0.04f));
            Sprite sprBushGold01 = LoadSprite($"{UnifiedTexRoot}/Bushes/T_Lana_Bush_GoldenVillage_01.png", new Vector2(0.5f, 0.04f)) ?? sprBushes[0];
            Sprite sprBushGold02 = LoadSprite($"{UnifiedTexRoot}/Bushes/T_Lana_Bush_GoldenVillage_02.png", new Vector2(0.5f, 0.04f)) ?? sprBushes[2];

            // Only tapered organic grass carpets so there are zero hard rectangular cutoffs
            Sprite[] sprGrassCarpets = new[]
            {
                LoadSprite($"{PackTexRoot}/Grass_Vines/T_LanaVN_GrassCarpet_03.png", new Vector2(0.5f, 0.05f)),
                LoadSprite($"{PackTexRoot}/Grass_Vines/T_LanaVN_GrassCarpet_04.png", new Vector2(0.5f, 0.05f)),
                LoadSprite($"{PackTexRoot}/Grass_Vines/T_LanaVN_GrassCarpet_05_Dense.png", new Vector2(0.5f, 0.05f)),
                LoadSprite($"{PackTexRoot}/Grass_Vines/T_LanaVN_GrassCarpet_06_Shrubby.png", new Vector2(0.5f, 0.05f))
            };

            Sprite[] sprFlowers = new[]
            {
                LoadSprite($"{PackTexRoot}/Wildflowers/T_LanaVN_Wildflower_01_Reeds.png", new Vector2(0.5f, 0.04f)),
                LoadSprite($"{PackTexRoot}/Wildflowers/T_LanaVN_Wildflower_02_TallGrass.png", new Vector2(0.5f, 0.04f)),
                LoadSprite($"{PackTexRoot}/Wildflowers/T_LanaVN_Wildflower_03_WhiteBlossom.png", new Vector2(0.5f, 0.04f)),
                LoadSprite($"{PackTexRoot}/Wildflowers/T_LanaVN_Wildflower_04_DaisySprig.png", new Vector2(0.5f, 0.04f)),
                LoadSprite($"{PackTexRoot}/Wildflowers/T_LanaVN_Wildflower_05_YellowBlossom.png", new Vector2(0.5f, 0.04f)),
                LoadSprite($"{PackTexRoot}/Wildflowers/T_LanaVN_Wildflower_06_SmallSprig.png", new Vector2(0.5f, 0.04f))
            };

            Sprite sprVineCreep   = LoadSprite($"{PackTexRoot}/Grass_Vines/T_LanaVN_VineLeaf_04_CreepingVine.png", new Vector2(0.5f, 0.75f));
            Sprite sprVineCluster = LoadSprite($"{PackTexRoot}/Grass_Vines/T_LanaVN_VineLeaf_02_Cluster.png", new Vector2(0.5f, 0.50f));

            // Golden Rice Field Sprites
            Sprite sprRiceCarpet1 = LoadSprite($"{UnifiedTexRoot}/Rice/T_Lana_Rice_Carpet_01_TallSway.png", new Vector2(0.5f, 0.05f));
            Sprite sprRiceCarpet2 = LoadSprite($"{UnifiedTexRoot}/Rice/T_Lana_Rice_Carpet_02_Medium.png", new Vector2(0.5f, 0.05f));
            Sprite sprRiceCarpet3 = LoadSprite($"{UnifiedTexRoot}/Rice/T_Lana_Rice_Carpet_03_DenseHead.png", new Vector2(0.5f, 0.05f));
            Sprite sprRicePanicle1= LoadSprite($"{UnifiedTexRoot}/Rice/T_Lana_Rice_Panicle_04_Tall.png", new Vector2(0.5f, 0.04f));
            Sprite sprRicePanicle2= LoadSprite($"{UnifiedTexRoot}/Rice/T_Lana_Rice_Panicle_05_Cluster.png", new Vector2(0.5f, 0.04f));
            Sprite sprRiceClump1  = LoadSprite($"{UnifiedTexRoot}/Rice/T_Lana_Rice_01_GoldenClump.png", new Vector2(0.5f, 0.06f));
            Sprite sprRiceRow2    = LoadSprite($"{UnifiedTexRoot}/Rice/T_Lana_Rice_02_WideGoldenRow.png", new Vector2(0.5f, 0.06f));

            var prefabBoulder = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/PF_MossyBoulder.prefab");

            GameObject SpawnSprite(string name, Transform parent, Sprite spr, Vector3 pos, Vector2 scale, Color tint, int order, float swayAngle = 1.4f, float swaySpeed = 1.3f, bool flipX = false, float rotZ = 0f)
            {
                if (spr == null) return null;
                var go = new GameObject(name);
                go.transform.SetParent(parent, false);
                go.transform.position = pos;
                go.transform.localRotation = Quaternion.Euler(0f, 0f, rotZ);
                go.transform.localScale = new Vector3(scale.x, scale.y, 1f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = spr;
                sr.sharedMaterial = GetOrCreateSpriteMat(spr, mats.SpriteDefaultMat);
                sr.color = tint;
                sr.sortingOrder = order;
                sr.flipX = flipX;

                if (swayAngle > 0.01f)
                {
                    var sway = go.AddComponent<PlanetOfLanaFoliageSway>();
                    var so = new SerializedObject(sway);
                    so.FindProperty("maxRotationAngle").floatValue = swayAngle;
                    so.FindProperty("swaySpeed").floatValue = swaySpeed;
                    so.FindProperty("useWorldPositionOffset").boolValue = true;
                    so.FindProperty("manualPhaseOffset").floatValue = pos.x * 0.37f + pos.z * 0.23f;
                    so.FindProperty("harmonicNoise").floatValue = 0.35f;
                    so.FindProperty("scaleBreathing").floatValue = 0.010f;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                return go;
            }

            void SpawnBoulder(string name, Vector3 pos, Vector3 scale, float rotY)
            {
                if (prefabBoulder == null) return;
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefabBoulder, rocksGroup.transform);
                inst.name = name;
                inst.transform.position = pos;
                inst.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
                inst.transform.localScale = scale;
                foreach (var mr in inst.GetComponentsInChildren<MeshRenderer>(true)) mr.sharedMaterial = mats.MossyBoulderMat;
                foreach (var col in inst.GetComponentsInChildren<Collider>(true)) col.enabled = false;
            }

            // Calibrated muted watercolor historical palette: soft atmospheric haze, no harsh neon greens
            Color farForestCol  = new Color(0.65f, 0.78f, 0.78f, 0.94f);
            Color midForestCol  = new Color(0.78f, 0.88f, 0.80f, 1.00f);
            Color nearForestCol = new Color(0.92f, 0.96f, 0.90f, 1.00f);
            Color fgShadowCol   = new Color(0.12f, 0.20f, 0.18f, 0.96f);

            // =====================================================================================
            // A. SKY-TO-FOREST ENTRY CORNER FRAMING (REPOUSSOIR - FOREGROUND LAYER)
            // =====================================================================================
            // 1. MASSIVE FOREGROUND DARK SILHOUETTE FRAMING TRUNKS (media_1792981597148_cf8bf183.png & media_1792982003302_435c07e3.jpg)
            // Left towering dark silhouette trunk framing left boundary cleanly
            SpawnSprite("Repoussoir_Left_MassiveHeroTrunk", fgDescentGroup.transform, sprTreeForeground1 ?? sprTrunkModular ?? sprCurvedTreeV2Full,
                new Vector3(-8.50f, -2.20f, -1.85f), new Vector2(1.35f, 2.20f), fgShadowCol, 29, 0.3f, 0.5f);

            // Slender organic framing branch framing An in near foreground
            SpawnSprite("Repoussoir_Near_FramingBranch", fgDescentGroup.transform, sprBranchOver ?? sprTreeSlenderHigh,
                new Vector3(-5.10f, -1.25f, -1.65f), new Vector2(0.85f, 1.15f), fgShadowCol, 28, 0.4f, 0.5f);

            // Right massive dark silhouette trunk framing right boundary beyond cliff
            SpawnSprite("Repoussoir_Right_MassiveHeroTrunk", fgDescentGroup.transform, sprTreeForeground1 ?? sprTrunkModular ?? sprTreeFullIsolated,
                new Vector3(8.50f, -2.00f, -1.85f), new Vector2(1.40f, 2.20f), fgShadowCol, 29, 0.3f, 0.5f, true);

            // Overhead framing foliage canopy (top corners)
            SpawnSprite("Repoussoir_TopLeft_HighCanopy", fgDescentGroup.transform, sprRepoussoirTop ?? sprBranchOver,
                new Vector3(-6.80f, 4.80f, -1.55f), new Vector2(1.25f, 1.15f), fgShadowCol, 28, 0.6f, 0.6f);
            SpawnSprite("Repoussoir_TopRight_HighCanopy", fgDescentGroup.transform, sprBranchOver,
                new Vector3(6.80f, 4.80f, -1.55f), new Vector2(1.25f, 1.15f), fgShadowCol, 28, 0.6f, 0.6f, true);

            // Dark silhouette foreground ground accents on the corners (below character walking plane)
            SpawnSprite("Repoussoir_Bottom_LeftRock", fgDescentGroup.transform, sprHDRock04Flat ?? sprRock04FlatSlab,
                new Vector3(-8.20f, -2.75f, -1.85f), new Vector2(1.40f, 0.50f), fgShadowCol, 29, 0f, 0f);
            SpawnSprite("Repoussoir_Bottom_RightRock", fgDescentGroup.transform, sprHDRock04Flat ?? sprRock04FlatSlab,
                new Vector3(8.20f, -2.75f, -1.85f), new Vector2(1.40f, 0.50f), fgShadowCol, 29, 0f, 0f, true);

            if (sprBranchOver != null)
            {
                SpawnSprite("Cliff_Framing_Branch", fgDescentGroup.transform, sprBranchOver,
                    new Vector3(12.5f, 5.8f, -1.50f), new Vector2(1.05f, 1.00f), fgShadowCol, 28, 0.9f, 0.8f);
                SpawnSprite("Crawl_Framing_Branch", fgDescentGroup.transform, sprBranchOver,
                    new Vector3(26.0f, 5.20f, -1.50f), new Vector2(1.00f, 0.95f), fgShadowCol, 28, 0.9f, 0.8f, true);
            }

            // =====================================================================================
            // B. ZONE 1 (-12m to +35.5m): TRANQUIL MORNING FOREST CLEARING (PLANET OF LANA DEPTH)
            // =====================================================================================
            // 0. Distant Background Forest Ridge (sau 1.png / Z = 6.5m, Order 4)
            float[] ridgeXs = new float[] { -12.0f, -8.5f, 18.5f, 25.5f, 33.5f };
            for (int rIdx = 0; rIdx < ridgeXs.Length; rIdx++)
            {
                float rx = ridgeXs[rIdx];
                float ry = SampleTerrainSurfaceY(rx) + 0.90f;
                float rz = 6.5f + (rIdx % 2) * 0.3f;
                SpawnSprite($"Forest_MidRidge_{rIdx:D2}", treesGroup.transform, sprTreeBackground1 ?? sprMidgroundHills,
                    new Vector3(rx, ry, rz), new Vector2(2.5f, 2.1f), farForestCol, 4, 0.5f, 0.6f, (rIdx % 2 == 1));
            }

            // 1. Atmospheric Background Trees (Z = 5.2m .. 5.8m, proportional scale 1.35f - 1.55f)
            float[] bgTreeXs = new float[] { -11.5f, -8.0f, -3.5f, 1.5f, 7.5f, 12.5f, 18.5f, 24.0f, 30.0f };
            for (int tIdx = 0; tIdx < bgTreeXs.Length; tIdx++)
            {
                float bx = bgTreeXs[tIdx];
                float by = SampleTerrainSurfaceY(bx) - 0.05f;
                Sprite bTreeSpr = (tIdx % 3 == 0) ? sprTreeWideCanopy : ((tIdx % 3 == 1) ? sprTreePoplarTall : sprTreeSlenderHigh);
                Vector2 bScale = new Vector2(1.45f, 1.45f);
                float bZ = 5.2f + (tIdx % 3) * 0.25f;
                SpawnSprite($"Forest_BG_Tree_{tIdx:D2}", treesGroup.transform, bTreeSpr,
                    new Vector3(bx, by, bZ), bScale, farForestCol, 5, 0.8f, 0.9f, (tIdx % 2 == 1));
            }

            // 2. HERO COMPOSITION - TRANQUIL MORNING FOREST BASIN & STEPPING STONES:
            // --- A. LEFT EMBANKMENT (X = -8.5m .. -4.0m) ---
            float bgLeft1Y = SampleTerrainSurfaceY(-8.2f) + 0.10f;
            SpawnSprite("Opening_BG_Tree_Left1", treesGroup.transform, sprTreeWideCanopy,
                new Vector3(-8.2f, bgLeft1Y, 4.2f), new Vector2(1.45f, 1.45f), farForestCol, 5, 0.7f, 0.8f);

            float bgLeft2Y = SampleTerrainSurfaceY(-6.8f) + 0.10f;
            SpawnSprite("Opening_BG_Tree_Left2", treesGroup.transform, sprTreeSlenderHigh,
                new Vector3(-6.8f, bgLeft2Y, 4.5f), new Vector2(1.40f, 1.40f), farForestCol, 4, 0.7f, 0.8f);

            // Stacked mossy rock outcrop at the base of the embankment
            SpawnSprite("Opening_Rock01_LargeOutcrop", rocksGroup.transform, sprRock01Large,
                new Vector3(-5.80f, SampleTerrainSurfaceY(-5.80f) - 0.05f, 0.85f), new Vector2(1.15f, 1.15f), nearForestCol, 12, 0f, 0f);
            SpawnSprite("Opening_Rock02_MossyMediumTop", rocksGroup.transform, sprRock02MossyTop,
                new Vector3(-4.50f, SampleTerrainSurfaceY(-4.50f) - 0.04f, 0.65f), new Vector2(0.95f, 0.95f), nearForestCol, 12, 0f, 0f);
            SpawnSprite("Opening_Rock05_SmallMossy_Embankment", rocksGroup.transform, sprRock05Small,
                new Vector3(-3.80f, SampleTerrainSurfaceY(-3.80f) - 0.04f, 0.45f), new Vector2(0.85f, 0.85f), nearForestCol, 12, 0f, 0f);

            // --- B. TRANQUIL MORNING FOREST CLEARING & MOSSY EARTH EMBANKMENTS (X = -4.0m .. +5.2m) ---
            float[] slopeXs = new float[] { -4.2f, -2.6f, -0.8f, 1.1f, 2.8f, 4.2f };
            for (int ci = 0; ci < slopeXs.Length; ci++)
            {
                float cx = slopeXs[ci];
                float cy = -0.75f + Mathf.Abs(cx) * 0.28f;
                float cz = 2.8f + (ci % 2) * 0.35f;
                Sprite cSpr = (ci % 2 == 0) ? (sprHDRock01Large ?? sprRock01Large) : (sprCliffShelf ?? sprRock01Large);
                SpawnSprite($"Forest_MossySlope_{ci:D2}", rocksGroup.transform, cSpr,
                    new Vector3(cx, cy, cz), new Vector2(2.10f, 1.65f), (ci == 2 || ci == 3) ? midForestCol : nearForestCol, 6, 0f, 0f, (ci % 2 == 1));
            }

            // 1. Natural stepping stone slabs leading An through the forest path to the cliff
            float[] pathSlabXs = new float[] { -3.40f, -2.10f, -0.80f, 0.60f, 1.90f, 3.20f, 4.30f };
            for (int sIdx = 0; sIdx < pathSlabXs.Length; sIdx++)
            {
                float sx = pathSlabXs[sIdx];
                float sy = SampleTerrainSurfaceY(sx) - 0.14f;
                SpawnSprite($"Path_SteppingStone_{sIdx:D2}", rocksGroup.transform, sprHDRock04Flat ?? sprRock04FlatSlab,
                    new Vector3(sx, sy, 0.10f), new Vector2(0.85f, 0.15f), nearForestCol, 12, 0f, 0f, (sIdx % 2 == 1));
            }

            // 2. Lush, tranquil morning trees rising with atmospheric depth (Midground Layer)
            SpawnSprite("Tranquil_HeroBanyan_Left", treesGroup.transform, sprTreeMidground1 ?? sprTree01Large,
                new Vector3(-4.40f, SampleTerrainSurfaceY(-4.40f) - 0.05f, 1.65f), new Vector2(1.25f, 1.25f), nearForestCol, 9, 0.6f, 0.7f);

            SpawnSprite("Tranquil_SlenderTree_Center", treesGroup.transform, sprCay1 ?? sprTreeSlenderHigh,
                new Vector3(-0.90f, SampleTerrainSurfaceY(-0.90f) - 0.05f, 2.80f), new Vector2(1.35f, 1.40f), midForestCol, 8, 0.5f, 0.6f);

            SpawnSprite("Tranquil_CurvedTree_Right", treesGroup.transform, sprCurvedTreeV2Full,
                new Vector3(1.85f, SampleTerrainSurfaceY(1.85f) - 0.05f, 1.80f), new Vector2(1.15f, 1.15f), nearForestCol, 9, 0.6f, 0.7f, true);

            // 3. Peaceful Sun-Dappled Forest Dust & Pollen VFX (Zero fiery embers!)
            SetupPeacefulForestPollenVFX();

            // Ground pebbles firmly touching the surface
            SpawnSprite("Opening_Pebbles_01", rocksGroup.transform, sprRock06PebblesTop,
                new Vector3(-2.80f, SampleTerrainSurfaceY(-2.80f) - 0.02f, 0.35f), new Vector2(0.65f, 0.65f), nearForestCol, 13, 0f, 0f);
            SpawnSprite("Opening_Pebbles_02", rocksGroup.transform, sprRock07PebblesBot,
                new Vector3( 0.80f, SampleTerrainSurfaceY( 0.80f) - 0.02f, 0.35f), new Vector2(0.65f, 0.65f), nearForestCol, 13, 0f, 0f);
            SpawnSprite("Opening_Rock05_PathDip", rocksGroup.transform, sprRock05Small,
                new Vector3( 2.20f, SampleTerrainSurfaceY( 2.20f) - 0.02f, 0.35f), new Vector2(0.60f, 0.60f), nearForestCol, 13, 0f, 0f);

            // --- C. STRATIFIED ROCK CLIFF FACE & OVERLOOK (X >= 5.0m, media_1792981610035_0a37bbf7.jpg) ---
            SpawnSprite("Cliff_Stratified_LargeOutcrop", rocksGroup.transform, sprHDRock01Large ?? sprRock01Large,
                new Vector3(5.45f, -1.25f, 0.08f), new Vector2(1.10f, 1.15f), nearForestCol, 24, 0f, 0f);
            SpawnSprite("Cliff_Shelf_MidTier", rocksGroup.transform, sprCliffShelf ?? sprRock01Large,
                new Vector3(5.75f, -0.90f, 0.12f), new Vector2(1.10f, 0.85f), nearForestCol, 24, 0f, 0f);
            SpawnSprite("Cliff_Rock_BaseFooting", rocksGroup.transform, sprRock03MossyBot,
                new Vector3(5.15f, -1.82f, 0.10f), new Vector2(0.95f, 0.85f), nearForestCol, 24, 0f, 0f);
            SpawnSprite("Cliff_Rock_TopLedgeCap", rocksGroup.transform, sprHDRock04Flat ?? sprRock04FlatSlab,
                new Vector3(5.45f, -0.48f, 0.06f), new Vector2(1.05f, 0.45f), nearForestCol, 25, 0f, 0f);

            // Overhanging Ivy Vines cascading down the rock face
            SpawnSprite("Cliff_Ivy_Vines_01", bushesGroup.transform, sprVineCreep ?? sprBranchOver,
                new Vector3(5.30f, -0.55f, 0.04f), new Vector2(1.05f, 1.15f), nearForestCol, 26, 1.2f, 1.0f);
            SpawnSprite("Cliff_Ivy_Vines_02", bushesGroup.transform, sprVineCluster ?? sprVineCreep,
                new Vector3(5.65f, -0.75f, 0.04f), new Vector2(0.95f, 1.05f), nearForestCol, 26, 1.0f, 0.9f);

            // Ancient Hero Tree rooted on the plateau ridge holding the rope bough (media_1792981759724_01eb70a4.jpg)
            SpawnSprite("Cliff_RopeHeroTree_Plateau", treesGroup.transform, sprTree01Large ?? sprTreeFullIsolated,
                new Vector3(6.20f, SampleTerrainSurfaceY(6.20f) - 0.05f, 0.65f), new Vector2(1.50f, 1.50f), nearForestCol, 11, 0.7f, 0.8f);

            // Plateau edge trees firmly grounded at Y = -0.55m
            SpawnSprite("Cliff_Tree03_PlateauEdge", treesGroup.transform, sprCay2 ?? sprTree03Small,
                new Vector3(7.80f, SampleTerrainSurfaceY(7.80f) - 0.04f, 1.80f), new Vector2(1.05f, 1.05f), nearForestCol, 10, 0.9f, 0.9f);
            SpawnSprite("Cliff_Tree04_SlenderTall", treesGroup.transform, sprCay3 ?? sprTree04Tall,
                new Vector3(11.50f, SampleTerrainSurfaceY(11.50f) - 0.04f, 1.50f), new Vector2(1.10f, 1.10f), nearForestCol, 10, 1.0f, 1.0f);

            // Zone 1 Landmark Trees: Cliff, Ravine, Crawlway (All strictly grounded on SampleTerrainSurfaceY with proportional scale!)
            SpawnSprite("Ravine_HeroTree_Edge", treesGroup.transform, sprCurvedTreeV2Full,
                new Vector3(19.2f, SampleTerrainSurfaceY(19.2f) - 0.05f, 2.30f), new Vector2(1.10f, 1.10f), nearForestCol, 10, 0.9f, 0.9f, true);
            SpawnSprite("Crawl_HeroTree_Overhang", treesGroup.transform, sprCay4 ?? sprTreeWideCanopy,
                new Vector3(27.8f, SampleTerrainSurfaceY(27.8f) - 0.05f, 2.20f), new Vector2(1.30f, 1.30f), nearForestCol, 9, 0.9f, 1.0f);
            SpawnSprite("CrateWall_HeroTree_End", treesGroup.transform, sprCay5 ?? sprTreeSlenderHigh,
                new Vector3(33.5f, SampleTerrainSurfaceY(33.5f) - 0.05f, 2.20f), new Vector2(1.30f, 1.30f), nearForestCol, 10, 1.0f, 1.0f);

            // Grounded Mossy Boulders (No floating props!)
            SpawnBoulder("Forest_ClearBoulder_Ravine", new Vector3(12.5f, SampleTerrainSurfaceY(12.5f) - 0.08f, 1.35f), new Vector3(1.35f, 0.60f, 1.0f), 30f);
            SpawnBoulder("Forest_ClearBoulder_Crawl",  new Vector3(21.8f, SampleTerrainSurfaceY(21.8f) - 0.08f, 1.35f), new Vector3(1.20f, 0.52f, 0.95f), 140f);

            // Wall Creeping Vines firmly anchored to cliff & ravine lips (Zero floating leaves in chasm void)
            SpawnSprite("Cliff_Vine_RopeWall", bushesGroup.transform, sprVineCreep,   new Vector3( 5.35f, SampleTerrainSurfaceY( 5.35f), -0.2f), new Vector2(0.95f, 0.95f), nearForestCol, 16, 1.8f, 1.5f);
            SpawnSprite("Cliff_Vine_RavineW",  bushesGroup.transform, sprVineCreep,   new Vector3(13.90f, SampleTerrainSurfaceY(13.90f), -0.2f), new Vector2(0.85f, 0.85f), nearForestCol, 16, 1.8f, 1.5f);
            SpawnSprite("Cliff_Vine_RavineE",  bushesGroup.transform, sprVineCreep,   new Vector3(18.25f, SampleTerrainSurfaceY(18.25f), -0.2f), new Vector2(0.85f, 0.85f), nearForestCol, 16, 1.8f, 1.5f, true);

            // Fallen Tree Crawl dressing
            SpawnSprite("FallenTree_TopBush1",  bushesGroup.transform, sprBushBroad1,  new Vector3(24.5f,  0.42f, -0.25f), new Vector2(0.70f, 0.68f), nearForestCol, 16, 1.5f, 1.4f);
            SpawnSprite("FallenTree_TopBush2",  bushesGroup.transform, sprBushes[1],   new Vector3(25.6f,  0.48f, -0.20f), new Vector2(0.75f, 0.72f), nearForestCol, 17, 1.6f, 1.5f);
            SpawnSprite("FallenTree_HangVine1", bushesGroup.transform, sprVineCreep,   new Vector3(24.3f,  0.15f, -0.45f), new Vector2(0.75f, 0.70f), nearForestCol, 18, 2.0f, 1.7f);
            SpawnSprite("FallenTree_HangVine2", bushesGroup.transform, sprVineCluster, new Vector3(25.4f,  0.22f, -0.45f), new Vector2(0.72f, 0.70f), nearForestCol, 18, 2.0f, 1.7f);
            SpawnSprite("CrateWall_HangVine",   bushesGroup.transform, sprVineCreep,   new Vector3(35.45f, 2.05f, -0.8f), new Vector2(1.10f, 1.10f), nearForestCol, 16, 1.8f, 1.5f);

            // =====================================================================================
            // C. ZONE 2 (+35.5m to +62.0m): HIGH CLIFF OVERLOOK & DOWNHILL SLOPE
            // =====================================================================================
            SpawnBoulder("Overlook_CliffBoulder_1", new Vector3(37.0f, SampleTerrainSurfaceY(37.0f) - 0.08f, 1.35f), new Vector3(1.75f, 0.68f, 1.20f), 25f);
            SpawnBoulder("Overlook_CliffBoulder_2", new Vector3(41.8f, SampleTerrainSurfaceY(41.8f) - 0.08f, 1.40f), new Vector3(1.45f, 0.55f, 1.10f), 145f);
            SpawnSprite("Overlook_HeroTree_Crown",  treesGroup.transform, sprTree01Large, new Vector3(37.4f, SampleTerrainSurfaceY(37.4f) - 0.05f, 2.2f), new Vector2(1.15f, 1.15f), nearForestCol, 10, 0.9f, 1.0f);

            SpawnBoulder("HillSlope_Boulder_1", new Vector3(47.5f, SampleTerrainSurfaceY(47.5f) - 0.08f, 1.45f), new Vector3(1.35f, 0.52f, 1.05f), 35f);
            SpawnBoulder("HillSlope_Boulder_2", new Vector3(53.5f, SampleTerrainSurfaceY(53.5f) - 0.08f, 1.45f), new Vector3(1.20f, 0.48f, 0.95f), 120f);
            SpawnBoulder("HillSlope_Boulder_3", new Vector3(59.0f, SampleTerrainSurfaceY(59.0f) - 0.08f, 1.45f), new Vector3(1.40f, 0.55f, 1.10f), 200f);

            SpawnSprite("HillSlope_Tree_1", treesGroup.transform, sprTreeSlenderHigh,  new Vector3(48.2f, SampleTerrainSurfaceY(48.2f) - 0.10f, 3.0f), new Vector2(1.35f, 1.35f), midForestCol, 7, 1.2f, 1.2f);
            SpawnSprite("HillSlope_Tree_2", treesGroup.transform, sprTreeGold02,       new Vector3(55.8f, SampleTerrainSurfaceY(55.8f) - 0.10f, 3.2f), new Vector2(1.05f, 1.05f), nearForestCol, 7, 1.1f, 1.1f);
            SpawnSprite("HillSlope_Palm_3", treesGroup.transform, sprTree05Banana,     new Vector3(60.5f, SampleTerrainSurfaceY(60.5f) - 0.10f, 2.6f), new Vector2(0.95f, 0.95f), nearForestCol, 9, 1.5f, 1.3f);

            // =====================================================================================
            // D. ORGANIC RIM TUFTS & GROUND DETAIL (-11.5m to +62.0m)
            // =====================================================================================
            // Zero flat cardboard carpets! The 3D procedural ledge mesh provides the continuous painterly grass.
            // Only delicate, subtle tufts placed strictly on the rim edge (Z = 0.05m) to feather the silhouette into air.
            int tuftIdx = 0;
            for (float x = -11.5f; x <= 61.5f; x += 3.2f)
            {
                if (x > 14.0f && x < 18.0f) continue; // Skip ravine chasm entirely

                float y = SampleTerrainSurfaceY(x);
                float slopeRotZ = 0f;
                if (x >= 43.5f && x < 62.0f) slopeRotZ = -11.5f;

                float goldBlend = Mathf.InverseLerp(48.0f, 62.0f, x);
                Color grassCol = Color.Lerp(new Color(0.92f, 0.98f, 0.88f, 1f), new Color(0.98f, 0.92f, 0.72f, 1f), goldBlend);
                float scaleJitter = 0.95f + Mathf.Abs(Mathf.Sin(x * 3.7f)) * 0.15f;

                Sprite tuftSpr = (tuftIdx % 2 == 0) ? LoadSprite($"{UnifiedTexRoot}/Grass/T_Lana_Grass_01_SoftTuft.png", new Vector2(0.5f, 0.05f))
                                                    : LoadSprite($"{UnifiedTexRoot}/Grass/T_Lana_Grass_02_WindsweptClump.png", new Vector2(0.5f, 0.05f));
                if (tuftSpr == null) tuftSpr = sprGrassCarpets[0];

                SpawnSprite($"Platform_RimTuft_{tuftIdx:D2}", grassGroup.transform, tuftSpr,
                    new Vector3(x, y - 0.03f, 0.05f), new Vector2(0.55f * scaleJitter, 0.45f * scaleJitter), grassCol, 14, 1.4f, 1.2f, (tuftIdx % 2 == 0), slopeRotZ);

                tuftIdx++;
            }

            // Proportional bushes (scale 0.45f - 0.55f) grounded on SampleTerrainSurfaceY
            void SpawnAnchoredBush(string name, float bx, float bz, Sprite spr, float scale, Color col, float rotZ = 0f)
            {
                if (spr == null) return;
                float by = SampleTerrainSurfaceY(bx);
                SpawnSprite(name, bushesGroup.transform, spr,
                    new Vector3(bx, by - 0.04f, bz), new Vector2(scale, scale), col, 11, 1.4f, 1.2f, false, rotZ);
            }

            SpawnAnchoredBush("Opening_Bush_EmbankmentFoot",-6.4f, 1.60f, sprBushBroad1, 0.52f, nearForestCol);
            SpawnAnchoredBush("Opening_Bush_MidLeftTree",   -3.6f, 1.55f, sprBushBroad1, 0.46f, nearForestCol);
            SpawnAnchoredBush("Opening_Bush_CenterHero",    -1.1f, 1.60f, sprBushBroad1, 0.45f, nearForestCol);
            SpawnAnchoredBush("Opening_Bush_DeadwoodStump",  3.8f, 1.55f, sprBushes[0],  0.46f, nearForestCol);
            SpawnAnchoredBush("Opening_Bush_CliffBase",      5.2f, 1.60f, sprBushBroad2, 0.50f, nearForestCol);
            SpawnAnchoredBush("Zone1_Bush_CliffPlateau",     6.6f, 1.55f, sprBushes[0],  0.48f, nearForestCol);
            SpawnAnchoredBush("Zone1_Bush_RavineEdge",       19.0f, 1.55f, sprBushes[2],  0.48f, nearForestCol);
            SpawnAnchoredBush("Zone1_Bush_CrawlEntrance",    27.2f, 1.55f, sprBushBroad1, 0.48f, nearForestCol);
            SpawnAnchoredBush("Zone2_Bush_SlopeUpper",       46.5f, 1.55f, sprBushGold01, 0.48f, nearForestCol, -11.5f);
            SpawnAnchoredBush("Zone2_Bush_SlopeMid",         53.0f, 1.55f, sprBushGold02, 0.46f, nearForestCol, -11.5f);
            SpawnAnchoredBush("Zone2_Bush_SlopeLower",       58.5f, 1.55f, sprBushGold01, 0.48f, nearForestCol, -11.5f);

            // Delicate wild vegetation (scale 0.35f - 0.45f, zero giant buttercups!)
            void SpawnAnchoredFlower(string name, float fx, float fz, Sprite spr, float scale, float rotZ = 0f)
            {
                if (spr == null) return;
                float fy = SampleTerrainSurfaceY(fx);
                SpawnSprite(name, grassGroup.transform, spr,
                    new Vector3(fx, fy - 0.02f, fz), new Vector2(scale, scale), Color.white, 14, 2.2f, 1.8f, false, rotZ);
            }

            SpawnAnchoredFlower("Opening_GrassTuft_Left",    -3.6f, 1.05f, sprFlowers[1], 0.42f);
            SpawnAnchoredFlower("Opening_GrassTuft_Center",  -1.1f, 1.05f, sprFlowers[0], 0.40f);
            SpawnAnchoredFlower("Opening_Flower_SmallSprig",  1.5f, 1.05f, sprFlowers[5], 0.36f);
            SpawnAnchoredFlower("Opening_Flower_CliffBloss",  5.6f, 1.10f, sprFlowers[2], 0.40f);
            SpawnAnchoredFlower("Zone1_Flower_Cliff",        12.0f, 1.10f, sprFlowers[3], 0.42f);
            SpawnAnchoredFlower("Zone1_Flower_Ravine",       20.2f, 1.10f, sprFlowers[4], 0.42f);
            SpawnAnchoredFlower("Zone2_Flower_Slope1",       48.0f, 1.10f, sprFlowers[4], 0.42f, -11.5f);
            SpawnAnchoredFlower("Zone2_Flower_Slope2",       54.5f, 1.10f, sprFlowers[3], 0.42f, -11.5f);

            // =====================================================================================
            // E. ZONE 3 (+62.0m to +95.0m) & ZONE 4 SURROUNDINGS (+95.0m to +134.0m):
            //    VAST GOLDEN RICE FIELD
            // =====================================================================================
            int riceIdx = 0;
            Color goldFrontCol = new Color(0.88f, 0.74f, 0.28f, 1f);
            Color goldMidCol   = new Color(1.00f, 0.94f, 0.54f, 1f);
            Color goldBackCol  = new Color(0.95f, 0.85f, 0.42f, 0.96f);

            for (float rx = 61.5f; rx <= 133.5f; rx += 1.35f)
            {
                bool inMainRiceField = (rx <= 96.5f);

                // Layer 1: Front Verge Low Golden Rice (Z = -1.96f)
                if (inMainRiceField || riceIdx % 2 == 0)
                {
                    Sprite frontRice = (riceIdx % 2 == 0) ? sprRiceCarpet1 : sprRiceCarpet2;
                    SpawnSprite($"Rice_FrontLip_{riceIdx:D3}", riceGroup.transform, frontRice,
                        new Vector3(rx, -1.92f, -1.96f), new Vector2(0.38f, 0.35f), goldFrontCol, 22, 2.8f, 2.1f, (riceIdx % 2 == 0));
                }

                // Layer 2: Midground Golden Rice Paddy Carpet behind walking path
                Sprite midRice = (riceIdx % 3 == 0) ? sprRiceCarpet3 : ((riceIdx % 2 == 0) ? sprRiceCarpet1 : sprRiceCarpet2);
                SpawnSprite($"Rice_MidPaddy_{riceIdx:D3}", riceGroup.transform, midRice,
                    new Vector3(rx + 0.25f, -1.82f, 0.78f), new Vector2(0.42f, 0.40f), goldMidCol, 14, 2.6f, 2.0f, (riceIdx % 3 == 0));

                // Layer 3: Swaying Golden Rice Panicles
                Sprite panSpr = (riceIdx % 3 == 0 && sprRiceClump1 != null) ? sprRiceClump1 : ((riceIdx % 2 == 0) ? sprRicePanicle1 : sprRicePanicle2);
                float panScale = (panSpr == sprRiceClump1) ? 0.30f : 0.38f;
                SpawnSprite($"Rice_TallPanicle_{riceIdx:D3}", riceGroup.transform, panSpr,
                    new Vector3(rx - 0.20f, -1.80f, 1.25f), new Vector2(panScale, panScale), goldMidCol, 13, 3.1f, 2.2f, (riceIdx % 2 == 1));

                // Layer 4: Deep Background Golden Rice Sea (Z = +2.10m)
                Sprite deepRice = (riceIdx % 2 == 0 && sprRiceRow2 != null) ? sprRiceRow2 : sprRiceCarpet3;
                float deepScale = (deepRice == sprRiceRow2) ? 0.35f : 0.42f;
                SpawnSprite($"Rice_DeepSea_{riceIdx:D3}", riceGroup.transform, deepRice,
                    new Vector3(rx + 0.4f, -1.78f, 2.10f), new Vector2(deepScale, deepScale), goldBackCol, 9, 2.2f, 1.8f, (riceIdx % 2 == 0));

                riceIdx++;
            }

            // Distant Horizon Coconut Palms, Bamboo Groves & Golden Trees along the Rice Field (Z = 3.6m .. 4.4m, scale 0.85f - 1.05f)
            SpawnSprite("RiceField_Horizon_Coconut1", treesGroup.transform, sprTree06Coconut, new Vector3(67.5f, -2.05f, 3.8f), new Vector2(0.90f, 0.90f), farForestCol, 5, 1.4f, 1.1f);
            SpawnSprite("RiceField_Horizon_Bamboo1",  treesGroup.transform, sprTree07Bamboo,  new Vector3(76.0f, -2.05f, 4.0f), new Vector2(0.95f, 0.95f), farForestCol, 5, 1.4f, 1.2f);
            SpawnSprite("RiceField_Horizon_GoldTree", treesGroup.transform, sprTreeGold01,    new Vector3(85.5f, -2.05f, 3.8f), new Vector2(1.05f, 1.05f), midForestCol, 6, 1.0f, 1.0f);
            SpawnSprite("RiceField_Horizon_Coconut2", treesGroup.transform, sprTree06Coconut, new Vector3(92.0f, -2.05f, 3.8f), new Vector2(0.92f, 0.92f), midForestCol, 6, 1.4f, 1.1f, true);
        }

        // =========================================================================================
        // 7. LIVING 3D STILTED-HOUSE VILLAGE, WIDE-ZOOM CUTSCENE & RIGGED ANIMATED 3D NPCs
        // =========================================================================================
        private static void BuildLivingVillageAndRiggedNPCs(LanaMaterialSet mats)
        {
            var mgRoot = GameObject.Find("05_Midground") ?? new GameObject("05_Midground");
            var gpRoot = GameObject.Find("02_Gameplay") ?? new GameObject("02_Gameplay");

            var villageRoot = new GameObject("Zone4_LivingVillage_RachGoc");
            villageRoot.transform.SetParent(mgRoot.transform, false);

            RuntimeAnimatorController npcAnimController = BuildOrLoadVillagerAnimatorController();

            var pfHouseThatched1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/PF_StiltedHouse_Thatched.prefab");
            var pfHouseWood1     = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/PF_StiltedHouse_Wood.prefab");
            var pfNhaSanGo       = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/NhaSanGo.prefab");
            var pfThatchedHut    = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment_Dock/PF_Village_ThatchedHut.prefab");
            var pfWaitingShelter = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment_Dock/PF_Dock_Waiting_Shelter.prefab");
            var pfGourdTrellis   = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/GourdTrellis/PF_LanaVN_GourdTrellis_25D_Composite.prefab");
            var pfBoulder        = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/PF_MossyBoulder.prefab");
            var pfBambooBasket   = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Props/PF_Prop_Bamboo_Basket.prefab");
            var pfRiceSacks      = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Props/PF_Prop_RiceSacks.prefab");
            var pfSaltBarrel     = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Props/PF_Prop_SaltFish_Barrel.prefab");

            void SpawnNormalizedVillageStructure(string name, GameObject prefab, Vector3 groundPos, float yaw, float targetHeightMeters)
            {
                if (prefab == null) return;
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, villageRoot.transform);
                inst.name = name;
                inst.transform.position = Vector3.zero;
                inst.transform.rotation = Quaternion.Euler(0f, yaw, 0f) * prefab.transform.localRotation;

                foreach (var col in inst.GetComponentsInChildren<Collider>(true))
                {
                    col.enabled = false;
                }

                var renderers = inst.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length > 0)
                {
                    Bounds b = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);

                    if (b.size.y > 0.05f && targetHeightMeters > 0.05f)
                    {
                        float factor = targetHeightMeters / b.size.y;
                        inst.transform.localScale = inst.transform.localScale * factor;
                    }

                    b = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);

                    Vector3 shift = new Vector3(
                        groundPos.x - b.center.x,
                        (groundPos.y - 0.08f) - b.min.y,
                        groundPos.z - b.center.z
                    );
                    inst.transform.position += shift;
                }
                else
                {
                    inst.transform.position = groundPos;
                }
            }

            SpawnNormalizedVillageStructure("Village_House01_EntranceThatched", pfHouseThatched1, new Vector3(100.8f, -1.85f, 2.8f), 175f, 4.45f);
            SpawnNormalizedVillageStructure("Village_House02_CentralWood",      pfHouseWood1,     new Vector3(109.2f, -1.85f, 3.1f), 184f, 4.65f);
            SpawnNormalizedVillageStructure("Village_House03_MaAnThatchedHome", pfHouseThatched1, new Vector3(117.8f, -1.85f, 2.9f), 178f, 4.55f);
            SpawnNormalizedVillageStructure("Village_House04_EastNhaSanGo",     pfNhaSanGo ?? pfHouseWood1, new Vector3(125.2f, -1.85f, 3.0f), 172f, 4.50f);
            SpawnNormalizedVillageStructure("Village_House05_ThatchedPavilion", pfWaitingShelter ?? pfThatchedHut, new Vector3(131.5f, -1.85f, 2.8f), 180f, 3.90f);

            if (pfGourdTrellis != null)
            {
                var tr1 = (GameObject)PrefabUtility.InstantiatePrefab(pfGourdTrellis, villageRoot.transform);
                tr1.name = "Village_GourdTrellis_West";
                tr1.transform.position = new Vector3(104.6f, -1.85f, 1.85f);

                var tr2 = (GameObject)PrefabUtility.InstantiatePrefab(pfGourdTrellis, villageRoot.transform);
                tr2.name = "Village_GourdTrellis_East";
                tr2.transform.position = new Vector3(121.4f, -1.85f, 1.85f);
            }

            SpawnNormalizedVillageStructure("Village_RiceSacks_Entrance", pfRiceSacks,    new Vector3( 98.0f, -1.85f, 1.45f),  15f, 0.85f);
            SpawnNormalizedVillageStructure("Village_BambooBasket_1",     pfBambooBasket, new Vector3(103.2f, -1.85f, 1.35f),   0f, 0.65f);
            SpawnNormalizedVillageStructure("Village_RiceSacks_Center",   pfRiceSacks,    new Vector3(112.5f, -1.85f, 1.45f), -20f, 0.90f);
            SpawnNormalizedVillageStructure("Village_SaltBarrel_1",       pfSaltBarrel,   new Vector3(116.2f, -1.85f, 1.55f),  35f, 0.88f);
            SpawnNormalizedVillageStructure("Village_BambooBasket_2",     pfBambooBasket, new Vector3(123.0f, -1.85f, 1.38f), -12f, 0.68f);

            if (pfBoulder != null)
            {
                float[] vRockXs = new[] { 96.2f, 105.5f, 113.8f, 122.2f, 129.2f };
                for (int i = 0; i < vRockXs.Length; i++)
                {
                    var b = (GameObject)PrefabUtility.InstantiatePrefab(pfBoulder, villageRoot.transform);
                    b.name = $"Village_Boulder_{i + 1:D2}";
                    b.transform.position = new Vector3(vRockXs[i], -1.86f, 1.45f);
                    b.transform.rotation = Quaternion.Euler(0f, i * 65f + 20f, 0f);
                    b.transform.localScale = new Vector3(1.25f, 0.52f, 1.00f);
                    foreach (var mr in b.GetComponentsInChildren<MeshRenderer>(true)) mr.sharedMaterial = mats.MossyBoulderMat;
                    foreach (var col in b.GetComponentsInChildren<Collider>(true)) col.enabled = false;
                }
            }

            Sprite sprTreeGold1 = LoadSprite($"{UnifiedTexRoot}/Trees/T_Lana_Tree_GoldenVillage_01.png", new Vector2(0.5f, 0.02f));
            Sprite sprTreeGold2 = LoadSprite($"{UnifiedTexRoot}/Trees/T_Lana_Tree_GoldenVillage_02.png", new Vector2(0.5f, 0.02f));
            Sprite sprBanana    = LoadSprite($"{PackTexRoot}/Trees/T_LanaVN_Tree_05_Banana.png", new Vector2(0.5f, 0.02f));
            Sprite sprCoconut   = LoadSprite($"{PackTexRoot}/Trees/T_LanaVN_Tree_06_Coconut.png", new Vector2(0.45f, 0.02f));
            Sprite sprBamboo    = LoadSprite($"{PackTexRoot}/Trees/T_LanaVN_Tree_07_BambooGrove.png", new Vector2(0.5f, 0.02f));
            Sprite sprBushG1    = LoadSprite($"{UnifiedTexRoot}/Bushes/T_Lana_Bush_GoldenVillage_01.png", new Vector2(0.5f, 0.04f));

            void SpawnVillageFoliage(string name, Sprite spr, Vector3 pos, Vector2 scale, int order, bool flipX = false)
            {
                if (spr == null) return;
                var go = new GameObject(name);
                go.transform.SetParent(villageRoot.transform, false);
                go.transform.position = pos;
                go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = spr;
                sr.sharedMaterial = GetOrCreateSpriteMat(spr, mats.SpriteDefaultMat);
                sr.sortingOrder = order;
                sr.flipX = flipX;
                var sway = go.AddComponent<PlanetOfLanaFoliageSway>();
                var so = new SerializedObject(sway);
                so.FindProperty("maxRotationAngle").floatValue = 1.2f;
                so.FindProperty("swaySpeed").floatValue = 1.2f;
                so.FindProperty("useWorldPositionOffset").boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            SpawnVillageFoliage("Village_Tree_EntranceGold", sprTreeGold1, new Vector3( 97.2f, -2.05f, 3.8f), new Vector2(1.05f, 1.05f), 6);
            SpawnVillageFoliage("Village_Tree_Banana1",      sprBanana,    new Vector3(105.0f, -2.05f, 3.6f), new Vector2(0.95f, 0.95f), 7);
            SpawnVillageFoliage("Village_Tree_CenterGold",   sprTreeGold2, new Vector3(113.5f, -2.05f, 4.0f), new Vector2(1.05f, 1.05f), 6, true);
            SpawnVillageFoliage("Village_Tree_Coconut1",     sprCoconut,   new Vector3(121.5f, -2.05f, 3.9f), new Vector2(0.95f, 0.95f), 6);
            SpawnVillageFoliage("Village_Tree_BambooEast",   sprBamboo,    new Vector3(128.5f, -2.05f, 3.8f), new Vector2(0.95f, 0.95f), 6);
            SpawnVillageFoliage("Village_Tree_EastCanopy",   sprTreeGold1, new Vector3(134.0f, -2.05f, 3.8f), new Vector2(1.05f, 1.05f), 6);

            for (float bx = 98.0f; bx <= 131.0f; bx += 5.5f)
            {
                SpawnVillageFoliage($"Village_Bush_{bx:F0}", sprBushG1, new Vector3(bx, -1.90f, 1.65f), new Vector2(0.70f, 0.70f), 11);
            }

            var npcsRoot = new GameObject("Living_Rigged_3D_NPCs");
            npcsRoot.transform.SetParent(gpRoot.transform, false);

            GameObject SpawnRigged3DNPC(
                string name, string fbxPath, string texPath,
                Vector3 pos, float yaw, float targetHeightMeters,
                string speakerTitle, string[] lines, string defaultState, float patrolRange = 0f)
            {
                EnsureModelIsHumanoid(fbxPath);
                var modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
                if (modelPrefab == null) return null;

                var inst = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, npcsRoot.transform);
                inst.name = name;
                inst.transform.position = pos;
                inst.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

                var rend = inst.GetComponentInChildren<Renderer>();
                if (rend != null && rend.bounds.size.y > 0.1f)
                {
                    float currentH = rend.bounds.size.y;
                    float s = targetHeightMeters / currentH;
                    inst.transform.localScale = Vector3.one * s;
                }

                if (!string.IsNullOrEmpty(texPath))
                {
                    Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
                    if (tex != null)
                    {
                        Shader lit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                        string matPath = $"{GenAssetsDir}/M_NPC_{Path.GetFileNameWithoutExtension(fbxPath)}.mat";
                        Material npcMat = GetOrCreateMat(matPath, lit);
                        if (npcMat.HasProperty("_BaseMap")) npcMat.SetTexture("_BaseMap", tex);
                        npcMat.mainTexture = tex;
                        npcMat.SetColor("_BaseColor", Color.white);
                        npcMat.SetFloat("_Smoothness", 0.18f);
                        EditorUtility.SetDirty(npcMat);

                        foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
                        {
                            r.sharedMaterial = npcMat;
                        }
                    }
                }

                var anim = inst.GetComponentInChildren<Animator>();
                if (anim == null) anim = inst.AddComponent<Animator>();
                if (npcAnimController != null)
                {
                    anim.runtimeAnimatorController = npcAnimController;
                    anim.applyRootMotion = false;
                    anim.Play(defaultState, 0, 0.25f);
                    anim.Update(0.05f);
                }

                var rendAfter = inst.GetComponentInChildren<Renderer>();
                if (rendAfter != null)
                {
                    float feetOffset = pos.y - rendAfter.bounds.min.y;
                    if (Mathf.Abs(feetOffset) < 1.0f)
                    {
                        inst.transform.position += new Vector3(0f, feetOffset, 0f);
                    }
                }

                var interact = inst.AddComponent<VillageNPCInteractable>();
                interact.ConfigureNPC(speakerTitle, lines, defaultState, patrolRange);

                return inst;
            }

            // Children playing in village
            SpawnRigged3DNPC(
                "VillageChild_01_Ti",
                "Assets/Models/Characters/NPCs/Character_NPC_06.fbx",
                "Assets/Models/Characters/NPCs/T_Character_NPC_06_BaseColor.jpg",
                new Vector3(101.5f, -1.85f, 0.55f), 110f, 1.32f,
                "Cu Tí",
                new[]
                {
                    "Anh An về rồi kìa! Má anh đang nấu cơm trong nhà sàn đằng kia đó!",
                    "Chuyến tàu chuyển quân ngoài bến sông Đốc sắp rời bến rồi!"
                },
                "Wave"
            );

            SpawnRigged3DNPC(
                "VillageChild_02_BeTu",
                "Assets/TripoModels/Meshy_AI_Quiet_Village_Child_1008013430_texture/Meshy_AI_Quiet_Village_Child_1008013430_texture.fbx",
                "Assets/TripoModels/Meshy_AI_Quiet_Village_Child_1008013430_texture/Meshy_AI_Quiet_Village_Child_1008013430_texture.fbm/Meshy_AI_Quiet_Village_Child_1008013430_texture_fbx_basecolor.JPEG",
                new Vector3(111.0f, -1.85f, 0.62f), -105f, 1.26f,
                "Bé Tư",
                new[]
                {
                    "Anh An ơi, giàn bầu nhà em hôm nay nở hoa vàng đẹp lắm!",
                    "Anh qua thăm má với cả nhà mau đi nha!"
                },
                "Idle2"
            );

            // 7 Living Villager NPCs
            var welcomerGO = SpawnRigged3DNPC(
                "VillageNPC_01_BacBaWelcomer",
                "Assets/Models/Characters/NPCs/Character_NPC_01.fbx",
                "Assets/Models/Characters/NPCs/T_Character_NPC_01_BaseColor.jpg",
                new Vector3(99.2f, -1.85f, 0.58f), -105f, 1.65f,
                "Bác Ba Nông Dân",
                new[]
                {
                    "Về tới xóm rồi hả An! Nhìn cánh đồng lúa chín vàng rực cả góc trời thấy ấm lòng quá con hen.",
                    "Năm nay trúng mùa, bà con mình gặt lúa đem góp gạo tiễn đoàn quân tập kết ngoài bến Sông Đốc."
                },
                "Wave"
            );

            SpawnRigged3DNPC(
                "VillageNPC_02_CoSauGourdTrellis",
                "Assets/TripoModels/female_Vietnamese/female_Vietnamese.fbx",
                "Assets/TripoModels/female_Vietnamese/female_Vietnamese.fbm/female_Vietnamese_basecolor.JPEG",
                new Vector3(104.6f, -1.85f, 0.65f), -95f, 1.60f,
                "Cô Sáu",
                new[]
                {
                    "Mấy trái bầu dưới giàn năm nay sai trái lắm An ơi.",
                    "Cô hái sẵn mấy bầu nước ngọt mát để dành cho mấy chú bộ đội mang lên tàu."
                },
                "Talk"
            );

            SpawnRigged3DNPC(
                "VillageNPC_03_OngBayElder",
                "Assets/Models/Characters/NPCs/Character_NPC_07.fbx",
                "Assets/Models/Characters/NPCs/T_Character_NPC_07_BaseColor.jpg",
                new Vector3(108.8f, -1.85f, 0.62f), 115f, 1.62f,
                "Ông Bảy",
                new[]
                {
                    "Đất đỏ, phù sa và rừng đước Cà Mau đã nuôi lớn bao thế hệ người dân miền Nam mình.",
                    "Dù ai đi xa tới đâu, chỉ cần nhớ màu lúa vàng và bóng nhà sàn này là nhớ đường về quê hương."
                },
                "Idle"
            );

            SpawnRigged3DNPC(
                "VillageNPC_04_ChuTuStrolling",
                "Assets/Models/Characters/NPCs/Character_NPC_03.fbx",
                "Assets/Models/Characters/NPCs/T_Character_NPC_03_BaseColor.jpg",
                new Vector3(114.0f, -1.85f, 0.72f), 90f, 1.66f,
                "Chú Tư Dân Làng",
                new[]
                {
                    "Chú đang kiểm tra lại mấy bao thóc mới phơi khô ngoài sân.",
                    "Má con đang đứng đợi ngay trước hiên nhà sàn đằng kia kìa An!"
                },
                "Walk",
                patrolRange: 2.8f
            );

            SpawnRigged3DNPC(
                "VillageNPC_05_MaAnMother",
                "Assets/Models/Characters/NPCs/Character_AnMother_35.fbx",
                "Assets/Models/Characters/NPCs/T_Character_AnMother_35_BaseColor.jpg",
                new Vector3(118.6f, -1.85f, 0.55f), -105f, 1.62f,
                "Má Của An",
                new[]
                {
                    "An của má về rồi đó hả con! Đi chơi trên rừng có mệt không con?",
                    "Bà con trong xóm đang chuẩn bị ra bến sông tiễn đoàn tàu năm 1954. Con giữ gìn sức khỏe, nhớ lời má dặn nghen!"
                },
                "Wave"
            );

            SpawnRigged3DNPC(
                "VillageNPC_06_AnhHai",
                "Assets/Models/Characters/NPCs/Character_NPC_04.fbx",
                "Assets/Models/Characters/NPCs/T_Character_NPC_04_BaseColor.jpg",
                new Vector3(123.8f, -1.85f, 0.65f), 105f, 1.66f,
                "Anh Hai Thanh Niên",
                new[]
                {
                    "Hẹn hai năm nữa thống nhất nước nhà, anh Hai sẽ về lại xóm Rạch Gốc gặt lúa cùng tụi em!",
                    "Ở nhà ráng học hành và phụ má nghe An!"
                },
                "Talk"
            );

            SpawnRigged3DNPC(
                "VillageNPC_07_CoUt",
                "Assets/Models/Characters/NPCs/Character_NPC_05.fbx",
                "Assets/Models/Characters/NPCs/T_Character_NPC_05_BaseColor.jpg",
                new Vector3(128.5f, -1.85f, 0.62f), -110f, 1.60f,
                "Cô Út",
                new[]
                {
                    "Ngoài bến sông Đốc cờ hoa rợp trời rồi đó An ơi.",
                    "Đi thêm một đoạn nữa qua rặng dừa là tới bến thuyền của xóm mình rồi."
                },
                "Idle2"
            );

            var camRoot = GameObject.Find("01_Camera") ?? new GameObject("01_Camera");
            var playerGO = GameObject.Find("PF_Player_Minh") ?? GameObject.Find("Character_An");

            var wideCamGO = new GameObject("VCam_VillageArrivalWide");
            wideCamGO.transform.SetParent(camRoot.transform, false);
            wideCamGO.transform.position = new Vector3(101.0f, 1.85f, -23.5f);
            wideCamGO.transform.rotation = Quaternion.Euler(4.0f, 0f, 0f);

            var vcamWide = wideCamGO.AddComponent<CinemachineCamera>();
            vcamWide.Priority = 0;
            var lens = vcamWide.Lens;
            lens.FieldOfView = 42.0f;
            vcamWide.Lens = lens;

            if (playerGO != null)
            {
                vcamWide.Follow = playerGO.transform;
                vcamWide.LookAt = null;
                var follow = wideCamGO.AddComponent<CinemachineFollow>();
                follow.FollowOffset = new Vector3(4.5f, 2.85f, -22.5f);
                var tracker = follow.TrackerSettings;
                tracker.BindingMode = Unity.Cinemachine.TargetTracking.BindingMode.WorldSpace;
                follow.TrackerSettings = tracker;
            }

            var cutsceneTrigGO = new GameObject("Zone4_VillageArrivalCutscene_Trigger");
            cutsceneTrigGO.transform.SetParent(gpRoot.transform, false);
            cutsceneTrigGO.transform.position = new Vector3(94.0f, -0.5f, 0f);
            var trigCol = cutsceneTrigGO.AddComponent<BoxCollider>();
            trigCol.isTrigger = true;
            trigCol.size = new Vector3(2.5f, 6.0f, 4.0f);

            var arrivalCutscene = cutsceneTrigGO.AddComponent<VillageArrivalCutscene>();
            var acSO = new SerializedObject(arrivalCutscene);
            acSO.FindProperty("triggerX").floatValue = 94.0f;
            acSO.FindProperty("targetRunToX").floatValue = 103.5f;
            acSO.FindProperty("vcamVillageWide").objectReferenceValue = vcamWide;
            if (welcomerGO != null)
            {
                acSO.FindProperty("welcomingVillagerAnimator").objectReferenceValue = welcomerGO.GetComponentInChildren<Animator>();
            }
            acSO.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureModelIsHumanoid(string fbxPath)
        {
            var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer != null && importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.SaveAndReimport();
            }
        }

        private static RuntimeAnimatorController BuildOrLoadVillagerAnimatorController()
        {
            string controllerPath = "Assets/Animations/AC_Lana_LivingVillager.controller";
            AnimationClip LoadClip(string fbxPath)
            {
                EnsureModelIsHumanoid(fbxPath);
                foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
                {
                    if (obj is AnimationClip c && !c.name.StartsWith("__preview__"))
                    {
                        var settings = AnimationUtility.GetAnimationClipSettings(c);
                        if (!settings.loopTime)
                        {
                            settings.loopTime = true;
                            AnimationUtility.SetAnimationClipSettings(c, settings);
                        }
                        return c;
                    }
                }
                return null;
            }

            AnimationClip clipIdle1 = LoadClip("Assets/Animations/Hero/Loco/Hero_Loco_Idle_Casual_Loop_IP.fbx");
            AnimationClip clipIdle2 = LoadClip("Assets/Animations/Hero/Loco/Hero_Loco_Idle_Variation_02_Loop_IP.fbx") ?? clipIdle1;
            AnimationClip clipWave  = LoadClip("Assets/Animations/Hero/Reaction/Hero_Social_VN_Farewell_Wave_Loop_IP.fbx") ?? clipIdle1;
            AnimationClip clipTalk  = LoadClip("Assets/Animations/Hero/Reaction/Hero_Social_Talk_Gesture_01.fbx") ?? clipWave;
            AnimationClip clipSit   = LoadClip("Assets/Animations/Hero/Stealth/Hero_Post_Sit_Idle_Loop.fbx") ?? clipIdle1;
            AnimationClip clipWalk  = LoadClip("Assets/Animations/Hero/Loco/Hero_Loco_Walk_Casual_Fwd_IP.fbx") ?? clipIdle1;

            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var sm = controller.layers[0].stateMachine;

            var stIdle1 = sm.AddState("Idle");  stIdle1.motion = clipIdle1;
            var stIdle2 = sm.AddState("Idle2"); stIdle2.motion = clipIdle2;
            var stWave  = sm.AddState("Wave");  stWave.motion  = clipWave;
            var stTalk  = sm.AddState("Talk");  stTalk.motion  = clipTalk;
            var stSit   = sm.AddState("Sit");   stSit.motion   = clipSit;
            var stWalk  = sm.AddState("Walk");  stWalk.motion  = clipWalk;

            sm.defaultState = stIdle1;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        // =========================================================================================
        // 8. LIGHTING, CAMERAS & CUTSCENE CONFIGURATION
        // =========================================================================================
        private static void ConfigureLightingCamerasAndCutscenes()
        {
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.50f, 0.70f, 0.86f);
            RenderSettings.ambientEquatorColor = new Color(0.88f, 0.82f, 0.66f);
            RenderSettings.ambientGroundColor = new Color(0.18f, 0.22f, 0.26f);
            RenderSettings.ambientIntensity = 1.15f;

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
                    l.shadowStrength = 0.40f;
                }
            }

            var camRootObj = GameObject.Find("01_Camera");
            if (camRootObj != null)
            {
                camRootObj.transform.position = Vector3.zero;
                camRootObj.transform.rotation = Quaternion.identity;
                camRootObj.transform.localScale = Vector3.one;
            }

            // VCam_IntroSky at zenith cosmic sky (X = -3.2m, Y = +36.5m, Z = -13.5m)
            var vcamSky = GameObject.Find("VCam_IntroSky");
            if (vcamSky != null)
            {
                vcamSky.transform.position = new Vector3(-3.20f, 36.5f, -13.5f);
                vcamSky.transform.rotation = Quaternion.identity;
                var skyCm = vcamSky.GetComponent<CinemachineCamera>();
                if (skyCm != null)
                {
                    skyCm.Follow = null;
                    skyCm.LookAt = null;
                    var skyLens = skyCm.Lens;
                    skyLens.FieldOfView = 38.0f;
                    skyCm.Lens = skyLens;
                }
            }

            var playerGO = GameObject.Find("PF_Player_Minh") ?? GameObject.Find("Character_An");

            var vcam = GameObject.Find("01_Camera/CM_VirtualCamera_2.5D") ?? GameObject.Find("CM_VirtualCamera_2.5D");
            if (vcam != null)
            {
                vcam.transform.position = new Vector3(-0.20f, 0.35f, -13.5f);
                vcam.transform.rotation = Quaternion.identity;
                var cmCam = vcam.GetComponent<CinemachineCamera>();
                if (cmCam != null)
                {
                    if (playerGO != null) cmCam.Follow = playerGO.transform;
                    cmCam.LookAt = null;
                    var lens = cmCam.Lens;
                    lens.FieldOfView = 38.0f;
                    cmCam.Lens = lens;
                }
                var posComposer = vcam.GetComponent<CinemachinePositionComposer>();
                if (posComposer != null)
                {
                    posComposer.CameraDistance = 13.5f;
                    posComposer.TargetOffset = new Vector3(2.0f, 2.17f, 0.0f);
                    var comp = posComposer.Composition;
                    comp.ScreenPosition = Vector2.zero;
                    posComposer.Composition = comp;
                    EditorUtility.SetDirty(posComposer);
                }
                var rotComposer = vcam.GetComponent<CinemachineRotationComposer>();
                if (rotComposer != null) UnityEngine.Object.DestroyImmediate(rotComposer);
                var hardLook = vcam.GetComponent<CinemachineHardLookAt>();
                if (hardLook != null) UnityEngine.Object.DestroyImmediate(hardLook);
                EditorUtility.SetDirty(vcam);
            }

            var mainCam = GameObject.Find("01_Camera/Main Camera") ?? (Camera.main != null ? Camera.main.gameObject : null);
            if (mainCam != null)
            {
                mainCam.transform.position = new Vector3(-0.20f, 0.35f, -13.5f);
                mainCam.transform.rotation = Quaternion.identity;
                var camComp = mainCam.GetComponent<Camera>();
                if (camComp != null)
                {
                    camComp.fieldOfView = 38.0f;
                    camComp.depth = 10.0f;
                    camComp.clearFlags = CameraClearFlags.SolidColor;
                    camComp.backgroundColor = new Color(0.16f, 0.38f, 0.64f);
                }
            }

            foreach (var cam in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
            {
                if (mainCam != null && cam.gameObject == mainCam) continue;
                var rootPrefab = PrefabUtility.GetOutermostPrefabInstanceRoot(cam.gameObject);
                if (rootPrefab != null)
                {
                    PrefabUtility.UnpackPrefabInstance(rootPrefab, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                }
                cam.enabled = false;
                UnityEngine.Object.DestroyImmediate(cam.gameObject);
            }

            var pauseCtrl = UnityEngine.Object.FindAnyObjectByType<PauseMenuController>();
            if (pauseCtrl != null)
            {
                Sprite logoSpr = LoadSprite("Assets/Textures/UI/T_Logo_HoaTrenDatDo.png");
                var pSO = new SerializedObject(pauseCtrl);
                var propLogo = pSO.FindProperty("officialLogoSprite");
                if (propLogo != null && logoSpr != null) propLogo.objectReferenceValue = logoSpr;
                pSO.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(pauseCtrl);
            }

            HoaVaMauSaveSystem.SaveCurrentGameState(1, "Chương 1: Đất Đỏ Cà Mau", "Rừng Sớm Cà Mau", 0, new Vector3(-2.2f, -1.82f, 0f));
            IntroCutsceneState.MarkForNewGame();

            var introCtrl = UnityEngine.Object.FindAnyObjectByType<IntroCutsceneController>();
            if (introCtrl != null)
            {
                Sprite s1 = LoadSprite("Assets/Textures/Cinematics/HistorySlides/history_1.jpg")
                         ?? LoadSprite("Assets/Textures/UI/HistoryCutscene/History_1954_CaMau_Regroupment_Soldiers.jpg");
                Sprite s2 = LoadSprite("Assets/Textures/Cinematics/HistorySlides/history_2.jpg")
                         ?? LoadSprite("Assets/Textures/UI/HistoryCutscene/History_1954_Carrier_Escort.jpg");
                Sprite s3 = LoadSprite("Assets/Textures/Cinematics/HistorySlides/history_3.jpg")
                         ?? LoadSprite("Assets/Textures/UI/HistoryCutscene/History_1954_Naval_Frigate.jpg");
                Sprite s4 = LoadSprite("Assets/Textures/Cinematics/HistorySlides/history_4.jpg")
                         ?? LoadSprite("Assets/Textures/UI/HistoryCutscene/History_1954_Truong_Xuan_Voyage.jpg");
                Sprite vig = LoadSprite("Assets/Textures/UI/T_Film_Vignette.png");
                Sprite scr = LoadSprite("Assets/Textures/UI/T_Film_Scratches.png");
                introCtrl.AssignSprites(s1, s2, s3, s4, vig, scr);
                introCtrl.EnsureCutsceneCanvasExists();

                var introSO = new SerializedObject(introCtrl);
                var propForce = introSO.FindProperty("forcePlayIntro");
                if (propForce != null) propForce.boolValue = true;
                var propPan = introSO.FindProperty("cameraPanDuration");
                if (propPan != null) propPan.floatValue = 11.0f;
                introSO.ApplyModifiedPropertiesWithoutUndo();

                EditorUtility.SetDirty(introCtrl);
            }
        }

        // =========================================================================================
        // 3D MESH GENERATION HELPERS
        // =========================================================================================
        private static void CreateLanaLedgeSection(
            Transform parent, string name,
            float startX, float endX, int segmentsX,
            float frontZ, float backZ, float bottomY,
            Func<float, float> heightFunc,
            LanaMaterialSet mats,
            bool includeGrassCap = true)
        {
            var sectionRoot = new GameObject(name);
            sectionRoot.transform.SetParent(parent, false);

            string meshDir = $"{GenAssetsDir}/GeneratedMeshes";
            if (!Directory.Exists(meshDir)) Directory.CreateDirectory(meshDir);

            Mesh rockMesh = BuildChiseledRockMesh($"{name}_RockMesh", startX, endX, segmentsX, frontZ, backZ, bottomY, heightFunc);
            AssetDatabase.CreateAsset(rockMesh, $"{meshDir}/{name}_RockMesh.asset");

            var rockGO = new GameObject($"{name}_RockUnderbelly");
            rockGO.transform.SetParent(sectionRoot.transform, false);
            rockGO.AddComponent<MeshFilter>().sharedMesh = rockMesh;
            var rockMR = rockGO.AddComponent<MeshRenderer>();
            rockMR.sharedMaterial = mats.RockUnderbellyMat;
            rockMR.shadowCastingMode = ShadowCastingMode.Off;
            rockMR.receiveShadows = false;

            if (includeGrassCap)
            {
                Mesh grassMesh = BuildOverhangingGrassCapMesh($"{name}_GrassMesh", startX, endX, segmentsX, frontZ - 0.05f, backZ, heightFunc);
                AssetDatabase.CreateAsset(grassMesh, $"{meshDir}/{name}_GrassMesh.asset");

                var grassGO = new GameObject($"{name}_GrassTurfCap");
                grassGO.transform.SetParent(sectionRoot.transform, false);
                grassGO.AddComponent<MeshFilter>().sharedMesh = grassMesh;
                var grassMR = grassGO.AddComponent<MeshRenderer>();
                grassMR.sharedMaterial = mats.GrassCapMat;
                grassMR.shadowCastingMode = ShadowCastingMode.Off;
                grassMR.receiveShadows = false;
            }
        }

        private static Mesh BuildChiseledRockMesh(
            string meshName, float startX, float endX, int segX,
            float frontZ, float backZ, float bottomY,
            Func<float, float> heightFunc)
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

                    float cliffFlag = Mathf.InverseLerp(-1.65f, 1.50f, surfY) * Mathf.Clamp01(1.0f - v * 1.8f);
                    float lipAO = (r == 0) ? 0.45f : ((r == 1) ? 0.85f : 1.0f);
                    float botAO = Mathf.Lerp(1.0f, 0.55f, Mathf.Pow(v, 1.5f));
                    colors.Add(new Color(0f, lipAO * botAO, cliffFlag, 1f));
                    uvs.Add(new Vector2(u * 14f, v * 2f));
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
                    colors.Add(new Color(0f, 0.80f, 0.4f, 1f));
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
            Func<float, float> heightFunc)
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
                    uvs.Add(new Vector2(u * 14f, (float)p / (profileCount - 1)));
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
        // 9. CAPTURE HIGH-RES VERIFICATION SCREENSHOTS ACROSS ALL ZONES
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

            Action<string, Vector3, Vector3, float> capture = (fileName, pos, euler, fov) =>
            {
                cam.transform.position = pos;
                cam.transform.eulerAngles = euler;
                cam.fieldOfView = fov;

                var bgFollower = UnityEngine.Object.FindAnyObjectByType<LanaParallaxBackgroundFollower>();
                if (bgFollower != null)
                {
                    bgFollower.TargetCamera = cam.transform;
                    bgFollower.LateUpdate();
                }

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
                UnityEngine.Object.DestroyImmediate(tex);

                string fullPath = Path.Combine(outDir, fileName);
                File.WriteAllBytes(fullPath, bytes);
                Debug.Log($"<color=#38bdf8>[Capture]</color> Saved verification view: {fileName}");
            };

            // 0. Cosmic Zenith Stratosphere (Deep Galaxy Blue & High Wisps)
            capture("Verify_00_ZenithCosmicSky.png", new Vector3(-3.2f, 34.5f, -13.5f), Vector3.zero, 38f);

            // 1. Sky-to-Forest Cloud Descent View (Descending through billowy cumulus clouds)
            capture("Verify_01_SkyToCloudsDescent.png", new Vector3(-3.2f, 22.5f, -13.5f), Vector3.zero, 38f);

            // 2. Forest Clearing Start & Cliff Rope (Recreated 1:1 to exact user reference painting 50_User_Reference_Forest_Vista.jpg)
            capture("Verify_02_ForestClearingAndRope.png", new Vector3(-0.20f, 0.35f, -13.5f), Vector3.zero, 38f);

            // 2B. Peaceful Forest Basin Hero Shot (Morning sun, lush greenery, peaceful dust)
            capture("Verify_02B_PeacefulForestBasin_HeroShot.png", new Vector3(-1.80f, 0.15f, -12.5f), Vector3.zero, 38f);

            // 3. Cliff Rope Close-Up (Clearance in free air in front of platform collider)
            capture("Verify_03_CliffRopeClearance.png", new Vector3(4.50f, -1.00f, -7.5f), Vector3.zero, 36f);

            // 4. Rocky Ravine Chasm & 4.6m Swing Rope
            capture("Verify_04_RavineChasmSwingRope.png", new Vector3(16.10f, 0.20f, -11.5f), Vector3.zero, 38f);

            // 5. Fallen Tree Crawl & Push/Pull Crate to High Platform
            capture("Verify_05_FallenTreeAndCratePuzzle.png", new Vector3(28.0f, 0.85f, -14.5f), Vector3.zero, 38f);

            // 6. High Cliff Overlook & Downhill Slope
            capture("Verify_06_HillOverlookAndDescent.png", new Vector3(45.0f, 2.25f, -16.0f), Vector3.zero, 38f);

            // 7. Flat Horizontal Golden Rice Field
            capture("Verify_07_FlatGoldenRiceField.png", new Vector3(78.0f, 0.15f, -13.5f), Vector3.zero, 38f);

            // 8. Wide-Zoom Village Arrival Cutscene & Living Animated NPC Village
            capture("Verify_08_LivingVillageWideZoom.png", new Vector3(108.0f, 1.85f, -21.5f), new Vector3(4f, 0f, 0f), 42f);

            cam.transform.position = origPos;
            cam.transform.rotation = origRot;
            cam.fieldOfView = origFov;

            var endFollower = UnityEngine.Object.FindAnyObjectByType<LanaParallaxBackgroundFollower>();
            if (endFollower != null)
            {
                endFollower.TargetCamera = cam.transform;
                endFollower.LateUpdate();
            }
        }
    }
}
#endif
