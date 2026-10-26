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
    ///     1. 3D Verlet Rope climb & swing up a 3.1m 3D rock cliff (X = 7.45m -> 8.4m, Y = -1.85m -> +1.25m).
    ///     2. Jump across a 2.6m rocky ravine gap (X = 14.6m -> 17.2m, Y = +1.25m).
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

            // 3. Build Official Painted Backgrounds ("22ffd054-1ca7-406c-9d11-0b93ba28b71a.png") & Sky-to-Forest Cloud Descent Layers
            BuildOfficialBackgroundsAndCloudDescent(mats);

            // 4. Build 3D Sculpted Terrain Ledges & Exact Physics Colliders across all 4 Zones (-12m to +138m)
            BuildSculpted3DLedgesAndPhysics(mats);

            // 5. Build Forest Manual Obstacle Course (Rope Cliff, Ravine Gap, Fallen Tree Trunk Crawl, Push/Pull Crate, Panorama Overlook)
            BuildForestManualObstaclesAndGameplay(mats);

            // 6. Populate 100% Unified Borderless Planet of Lana 2D Ecosystem (Forest, Hill Slope, Golden Rice Field, Village)
            PopulateUnifiedPlanetOfLanaEcosystem(mats);

            // 7. Build Living 3D Stilted-House Village, Wide-Zoom Cutscene & Rigged Animated 3D NPCs
            BuildLivingVillageAndRiggedNPCs(mats);

            // 8. Configure Cameras (VCam_IntroSky at Y=+21.5m, CM_VirtualCamera_2.5D, VCam_VillageArrivalWide), Cutscene & Lighting
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
            Debug.Log($"<color=#38bdf8>[LanaOverhaul]</color> Fast updating Atmosphere, Cosmic Sky, and Cutscene in: {scene.name}");

            // Clean previous background vista and cloud descent groups
            var bgRoot = GameObject.Find("07_Background");
            if (bgRoot != null)
            {
                var vista = bgRoot.transform.Find("Lana_Painted_Vistas");
                if (vista != null) UnityEngine.Object.DestroyImmediate(vista.gameObject);
                var clouds = bgRoot.transform.Find("IntroSky_CloudDescentGroup");
                if (clouds != null) UnityEngine.Object.DestroyImmediate(clouds.gameObject);
            }

            var mats = CreateOrUpdateLanaMaterials();
            BuildOfficialBackgroundsAndCloudDescent(mats);
            ConfigureLightingCamerasAndCutscenes();

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("<color=#4ade80>[LanaOverhaul]</color> <b>Atmosphere, Cosmic Zenith Sky, Clouds, and Inverted Keycap Cutscene UI updated successfully!</b>");
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

            // 2. Deep Navy-Slate Rock Cross-Section & Cliff Material
            set.RockUnderbellyMat = GetOrCreateMat($"{GenAssetsDir}/M_Lana_RockUnderbelly.mat", ledgeShader);
            set.RockUnderbellyMat.SetFloat("_IsGrassCap", 0.0f);
            set.RockUnderbellyMat.SetColor("_RockLightColor", new Color(0.23f, 0.29f, 0.37f, 1f));
            set.RockUnderbellyMat.SetColor("_RockMidColor", new Color(0.13f, 0.17f, 0.23f, 1f));
            set.RockUnderbellyMat.SetColor("_RockDarkColor", new Color(0.05f, 0.07f, 0.11f, 1f));
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

            // 4. URP 2D Sprite Unlit Material so every 2D sprite renders its own clean texture without legacy batch bleeding
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
        // 3. OFFICIAL USER BACKGROUND ("22ffd054-1ca7-406c-9d11-0b93ba28b71a.png") & SKY-TO-FOREST CLOUD DESCENT
        // =========================================================================================
        private static void BuildOfficialBackgroundsAndCloudDescent(LanaMaterialSet mats)
        {
            var bgRoot = GameObject.Find("07_Background") ?? new GameObject("07_Background");
            var vistaGroup = new GameObject("Lana_Painted_Vistas");
            vistaGroup.transform.SetParent(bgRoot.transform, false);

            Shader bgShader = Shader.Find("HoaVaMau/LanaBackgroundBlend") ?? Shader.Find("Universal Render Pipeline/Unlit");

            // Load user's official Planet of Lana background ("C:\Users\Arkstarz\Pictures\22ffd054-1ca7-406c-9d11-0b93ba28b71a.png")
            var texOfficialMain = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Backgrounds_Lana/BG_Lana_MainSkyHills_Official.png");
            if (texOfficialMain == null)
            {
                texOfficialMain = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Backgrounds_Lana/BG_Lana_PlainsHills_Day.png");
            }

            void SpawnVistaPanel(string name, Texture2D tex, float centerX, float centerY, float z, float height, float leftFeather, float rightFeather, int queueOffset, Color tint, bool flipY = false)
            {
                if (tex == null) return;
                float aspect = (float)tex.width / Mathf.Max(1, tex.height);
                float width = Mathf.Abs(height) * aspect; // 100% native aspect ratio, zero cropping!

                var mat = GetOrCreateMat($"{GenAssetsDir}/M_{name}.mat", bgShader);
                mat.SetTexture("_MainTex", tex);
                mat.SetColor("_Color", tint);
                mat.SetFloat("_LeftFeather", leftFeather);
                mat.SetFloat("_RightFeather", rightFeather);
                mat.SetFloat("_BottomFeather", 0.0f);
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

            // Attach dynamic Parallax Background Follower so the painted background naturally follows the character!
            // Background moves WITH the camera across 145m horizontally (0.88f), with near-zero vertical drift (0.02f)
            // so mountains and horizon stay anchored realistically, without exposing lower bounds or water during vertical pans!
            var follower = bgRoot.GetComponent<LanaParallaxBackgroundFollower>() ?? bgRoot.AddComponent<LanaParallaxBackgroundFollower>();
            follower.ClearLayers();

            // Exact 1:1 background framing for reference painting (50_User_Reference_Forest_Vista.jpg)
            // BG_Lana_MainSkyHills_Official.png (2560x1080 / 1940x810).
            // panelHeight = 16.0f -> panelWidth = panelHeight * aspect.
            // Center is aligned at X = +2.50m so that the distant river delta, rolling mountains, and open sky
            // are framed DEAD-CENTER in the opening vista between the hero center tree (X = -1.1m) and the right cliff (X = +5.2m)!
            // vistaCenterY = 3.65f calibrates mountain peaks (V = 0.18) right at Y = -0.75m above the meadow dip!
            const float bgDepthZ = 16.0f;
            float panelHeight = 16.0f;
            float aspect = (float)texOfficialMain.width / Mathf.Max(1, texOfficialMain.height);
            float panelWidth = panelHeight * aspect;
            float vistaCenterY = 3.65f;

            // 1. Lower Mountain & River Delta Vista Panels across the full 145m level
            SpawnVistaPanel("Vista_Center", texOfficialMain, 2.50f, vistaCenterY, bgDepthZ, panelHeight, 0.00f, 0.00f, 1, Color.white);
            SpawnVistaPanel("Vista_Right1", texOfficialMain, 2.50f + panelWidth, vistaCenterY, bgDepthZ, panelHeight, 0.05f, 0.00f, 1, Color.white);
            SpawnVistaPanel("Vista_Right2", texOfficialMain, 2.50f + panelWidth * 2f, vistaCenterY, bgDepthZ, panelHeight, 0.05f, 0.00f, 1, Color.white);
            SpawnVistaPanel("Vista_Left",   texOfficialMain, 2.50f - panelWidth, vistaCenterY, bgDepthZ, panelHeight, 0.00f, 0.05f, 1, Color.white);

            // 2. Seamless Upper Cosmic Sky Extension directly touching top edge at Y = +11.65m
            float skyPanelHeight = 36.0f;
            float skyCenterY = vistaCenterY + (panelHeight * 0.5f) + (skyPanelHeight * 0.5f); // 3.65 + 8.0 + 18.0 = 29.65f
            Texture2D texUpperSky = BuildOrLoadSeamlessUpperSkyTexture(texOfficialMain);
            float skyAspect = (float)texUpperSky.width / Mathf.Max(1, texUpperSky.height);
            float skyPanelWidth = skyPanelHeight * skyAspect;
            SpawnVistaPanel("UpperSky_Center", texUpperSky, 2.50f, skyCenterY, bgDepthZ, skyPanelHeight, 0.00f, 0.00f, 0, Color.white, false);
            SpawnVistaPanel("UpperSky_Right",  texUpperSky, 2.50f + skyPanelWidth, skyCenterY, bgDepthZ, skyPanelHeight, 0.05f, 0.00f, 0, Color.white, false);
            SpawnVistaPanel("UpperSky_Left",   texUpperSky, 2.50f - skyPanelWidth, skyCenterY, bgDepthZ, skyPanelHeight, 0.00f, 0.05f, 0, Color.white, false);

            // Register with dynamic parallax follower:
            // 0.92f followFactorX keeps the mountain panorama moving smoothly with the camera across the level
            // 0.02f followFactorY keeps the horizon steady during jumps and vertical descent
            follower.RegisterLayer("Lana_Painted_Vistas", vistaGroup.transform, 0.92f, 0.02f);

            // Multi-layered Parallax Cloud Descent Group (Only high stratosphere and upper cumulus for the intro descent!)
            var cloudGroup = new GameObject("IntroSky_CloudDescentGroup");
            cloudGroup.transform.SetParent(bgRoot.transform, false);

            const string CloudBaseDir = "Assets/Art/Environment/Sky/Clouds";
            Sprite sprHero01     = LoadSprite($"{CloudBaseDir}/Foreground/T_Cloud_Hero_Descent01.png", new Vector2(0.5f, 0.5f));
            Sprite sprDistLayer  = LoadSprite($"{CloudBaseDir}/Distant/T_Cloud_Distant_Layer02.png", new Vector2(0.5f, 0.5f));

            void SpawnCloudLayer(string name, Sprite spr, Vector3 pos, Vector2 scale, Color col, int sortOrder, bool flipX = false)
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
            }

            // 1. High Stratosphere Wisps for Cutscene Zenith (Y = +33.0m)
            SpawnCloudLayer("Descent_01_ZenithWisp", sprDistLayer, new Vector3(-3.2f, 33.5f, 10.0f), new Vector2(2.5f, 2.5f), new Color(0.90f, 0.94f, 1.0f, 0.85f), 1);

            // 2. Hero Upper Sky Cumulus passed through during camera descent (Y = +22.0m)
            SpawnCloudLayer("Descent_02_HeroHigh", sprHero01, new Vector3(-2.0f, 22.0f, 9.5f), new Vector2(2.6f, 2.6f), new Color(0.98f, 0.99f, 1.0f, 0.95f), 2);

            // Register clouds with parallax follower
            follower.RegisterLayer("IntroSky_CloudDescentGroup", cloudGroup.transform, 0.88f, 0.05f);
        }

        private static Texture2D BuildOrLoadSeamlessUpperSkyTexture(Texture2D officialBg)
        {
            string outPath = "Assets/Textures/Backgrounds_Lana/T_Lana_ContinuousCosmicSky_Seamless.png";
            if (officialBg == null) return null;

            string bgAssetPath = AssetDatabase.GetAssetPath(officialBg);
            if (string.IsNullOrEmpty(bgAssetPath) || !File.Exists(bgAssetPath)) return officialBg;

            var rawTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            rawTex.LoadImage(File.ReadAllBytes(bgAssetPath));

            int w = rawTex.width;
            int h = rawTex.height;
            int sampleY = Mathf.Clamp(h - 3, 0, h - 1);

            int outH = 1024;
            var skyTex = new Texture2D(w, outH, TextureFormat.RGB24, false);
            Color[] rowCols = new Color[w];
            for (int x = 0; x < w; x++)
            {
                rowCols[x] = rawTex.GetPixel(x, sampleY);
            }
            UnityEngine.Object.DestroyImmediate(rawTex);

            Color cCerulean       = new Color(0.075f, 0.235f, 0.440f, 1f);
            Color cMidnightCobalt = new Color(0.045f, 0.145f, 0.300f, 1f);
            Color cGalaxyZenith   = new Color(0.015f, 0.045f, 0.115f, 1f);

            Color[] outPixels = new Color[w * outH];
            for (int y = 0; y < outH; y++)
            {
                float v = (float)y / Mathf.Max(1, outH - 1);
                for (int x = 0; x < w; x++)
                {
                    Color col;
                    if (v < 0.28f)
                    {
                        float t = Mathf.SmoothStep(0f, 1f, v / 0.28f);
                        col = Color.Lerp(rowCols[x], cCerulean, t);
                    }
                    else if (v < 0.65f)
                    {
                        float t = Mathf.SmoothStep(0f, 1f, (v - 0.28f) / 0.37f);
                        col = Color.Lerp(cCerulean, cMidnightCobalt, t);
                    }
                    else
                    {
                        float t = Mathf.SmoothStep(0f, 1f, (v - 0.65f) / 0.35f);
                        col = Color.Lerp(cMidnightCobalt, cGalaxyZenith, t);
                    }
                    outPixels[y * w + x] = col;
                }
            }
            skyTex.SetPixels(outPixels);
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

        // =========================================================================================
        // 4. 3D SCULPTED TERRAIN LEDGES & ROCK-SOLID PHYSICS COLLIDERS (-12m to +138m)
        // =========================================================================================
        public static float SampleTerrainSurfaceY(float x)
        {
            // Zone 1A: Forest Clearing (-12.0m to +5.2m) — matches exact reference 50_User_Reference_Forest_Vista.jpg
            // Left embankment stepped at Y = -0.85m, sloping down into sunlit meadow (-1.75m .. -2.05m at the cliff base)
            if (x < -3.8f)
            {
                float t = Mathf.Clamp01((x - (-5.5f)) / 1.7f);
                return Mathf.Lerp(-0.85f, -1.75f, t);
            }
            if (x >= -3.8f && x < 5.2f)
            {
                float t = (x - (-3.8f)) / 9.0f;
                return Mathf.Lerp(-1.75f, -2.05f, t);
            }
            // Zone 1B: Right Cliff Plateau (+5.2m to +14.6m) -> Y = -0.55m (overlooking the vast valley)
            if (x >= 5.2f && x <= 14.6f)
            {
                return -0.55f + Mathf.Sin(x * 0.8f) * 0.02f;
            }
            // Zone 1C: Rocky Ravine Gap (+14.6m to +17.2m) -> Ravine floor at Y = -1.10m
            if (x > 14.6f && x < 17.2f)
            {
                return -1.10f;
            }
            // Zone 1D: East Ravine Plateau (+17.2m to +20.6m) -> Y = +1.25m
            if (x >= 17.2f && x < 20.6f)
            {
                return 1.25f;
            }
            // Zone 1E: Stepped Descent to Fallen Tree Trunk (+20.6m to +22.0m) -> Steps from +1.25m to -1.10m
            if (x >= 20.6f && x < 22.0f)
            {
                float t = (x - 20.6f) / 1.4f;
                return Mathf.Lerp(1.25f, -1.10f, SmoothCos(t));
            }
            // Zone 1F: Fallen Tree Crawl & Crate Push/Pull Arena (+22.0m to +35.5m) -> Strictly flat at Y = -1.10m for smooth crate physics!
            if (x >= 22.0f && x < 35.5f)
            {
                return -1.10f;
            }
            // Zone 2A: High Cliff Vista Overlook (+35.5m to +43.5m) -> Y = +2.15m ("đi tới một vách thấy được đồi ở dưới, thấy được làng ở dưới")
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
            // Strictly 100% flat & horizontal ("lúc đó platform sẽ phẳng lại và ngang hàng") at Y = -1.85m!
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

            // Build discrete sculpted 3D ledge sections with backZ = 5.40f so all background trees (Z = 2.0m..4.6m) & 3D village houses
            // stand directly ON the 3D grass/golden-meadow turf with zero sky gap underneath!
            CreateLanaLedgeSection(ledgesGroup.transform, "Ledge_01_ForestStart",
                -12.0f, 5.20f, 75, -1.75f, 5.40f, -9.5f, SampleTerrainSurfaceY, mats, true);

            CreateLanaLedgeSection(ledgesGroup.transform, "Ledge_02_RopeCliffPlateau",
                5.20f, 14.60f, 48, -1.75f, 5.40f, -9.5f, x => -0.55f + Mathf.Sin(x * 0.8f) * 0.02f, mats, true);

            CreateLanaLedgeSection(ledgesGroup.transform, "Ledge_03_RavineFloor",
                14.40f, 17.40f, 16, -1.60f, 5.40f, -9.5f, x => -1.10f, mats, false);

            CreateLanaLedgeSection(ledgesGroup.transform, "Ledge_04_RavineEastToCrate",
                17.20f, 35.52f, 96, -1.75f, 5.40f, -9.5f, SampleTerrainSurfaceY, mats, true);

            CreateLanaLedgeSection(ledgesGroup.transform, "Ledge_05_HighOverlook_Hill_Rice_And_Village",
                35.50f, 138.0f, 420, -1.75f, 5.40f, -9.5f, SampleTerrainSurfaceY, mats, true);

            // Build precision physics colliders for CharacterController, LedgeGrabDetector & InteractableCrate
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

            // 2. Right Cliff Plateau (+5.20m .. +14.60m, topY = -0.55m — vertical wall at X = 5.20m for LedgeGrabDetector!)
            AddBoxCol("Col_02_RopeCliffPlateau", 5.20f, 14.60f, -8.0f, -0.55f);

            // 3. Ravine Floor (+14.60m .. +17.20m, topY = -1.10m) + Stepping Boulders inside Ravine
            AddBoxCol("Col_03_RavineFloor", 14.60f, 17.20f, -8.0f, -1.10f);
            AddBoxCol("Col_03b_RavineStepStone", 15.45f, 16.35f, -1.10f, 0.05f);

            // 4. East Ravine Plateau (+17.20m .. +20.60m, topY = +1.25m — vertical wall at X = 17.20m for LedgeGrabDetector!)
            AddBoxCol("Col_04_RavineEastPlateau", 17.20f, 20.60f, -8.0f, 1.25f);

            // 5. Stepped Descent to Fallen Tree (+20.60m .. +22.00m)
            AddBoxCol("Col_05a_StepDownMid", 20.60f, 21.35f, -8.0f, 0.45f);
            AddBoxCol("Col_05b_StepDownLow", 21.35f, 22.00f, -8.0f, -0.35f);

            // 6. Fallen Tree Crawl & Crate Push/Pull Floor (+22.00m .. +35.50m, topY = -1.10m)
            AddBoxCol("Col_06_CrawlAndCrateFloor", 22.00f, 35.50f, -8.0f, -1.10f);

            // 7. High Cliff Overlook Plateau (+35.50m .. +43.50m, topY = +2.15m — vertical wall at X = 35.50m for Crate + LedgeClimb!)
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

            // 9. Perfectly Flat Horizontal Golden Rice Field & Living Village Platform (+62.00m .. +138.00m, topY = -1.85m)
            AddBoxCol("Col_09_FlatRiceAndVillagePlatform", 61.90f, 138.0f, -8.0f, -1.85f);
        }

        // =========================================================================================
        // 5. FOREST MANUAL OBSTACLES & GAMEPLAY (ROPE, RAVINE, FALLEN TREE CRAWL, CRATE, PANORAMA)
        // =========================================================================================
        private static void BuildForestManualObstaclesAndGameplay(LanaMaterialSet mats)
        {
            var gpRoot = GameObject.Find("02_Gameplay") ?? new GameObject("02_Gameplay");

            // A. Position Player (An) at the Forest Start Clearing (X = -2.2m, Y = -1.82m, Z = 0m)
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

            // Move Exit Goal Trigger to the far eastern end of the Village (X = 132.5m)
            var exitTrig = GameObject.Find("Scene1_ExitGoalTrigger_60m");
            if (exitTrig != null)
            {
                exitTrig.transform.position = new Vector3(132.5f, -0.50f, 0f);
            }

            // Remove legacy tutorial controller to prevent duplicate prompts with PlanetOfLanaPromptSystem
            var oldTut = GameObject.Find("00_UI_PlanetOfLana_TutorialController");
            if (oldTut != null) UnityEngine.Object.DestroyImmediate(oldTut);

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

            // --- OBSTACLE 1: 3D VERLET ROPE HANGING IN FRONT OF THE X = 5.2m CLIFF (MATCHES 50_User_Reference_Forest_Vista.jpg) ---
            Spawn3DBoulder("RopeCliff_TopLipBoulder", obstaclesRoot.transform, new Vector3(5.35f, -0.45f, 0.35f), new Vector3(1.35f, 0.55f, 1.15f), 25f, false);
            Spawn3DBoulder("RopeCliff_WallSlab_Lower",obstaclesRoot.transform, new Vector3(5.15f, -1.35f, 0.30f), new Vector3(1.15f, 0.85f, 1.00f), 205f, false);

            // Anchored directly to the natural rock lip of the cliff at X = 5.25m, Y = -0.45m
            var ropeAnchorGO = new GameObject("RopeAnchor_Transform");
            ropeAnchorGO.transform.SetParent(obstaclesRoot.transform, false);
            ropeAnchorGO.transform.position = new Vector3(5.25f, -0.45f, 0.12f);

            var ropeGO = new GameObject("Obstacle01_CliffRope");
            ropeGO.transform.SetParent(obstaclesRoot.transform, false);
            ropeGO.transform.position = new Vector3(5.25f, -0.45f, 0f);
            var ropeCtrl = ropeGO.AddComponent<RopeController>();
            var ropeSO = new SerializedObject(ropeCtrl);
            ropeSO.FindProperty("totalLength").floatValue = 1.45f;
            ropeSO.FindProperty("nodeCount").intValue = 10;
            ropeSO.FindProperty("ropeVisualWidth").floatValue = 0.065f;
            ropeSO.ApplyModifiedPropertiesWithoutUndo();
            ropeCtrl.InitializeRope();

            // --- OBSTACLE 2: ROCKY RAVINE STEPPING STONE VISUALS (X = 14.6m .. 17.2m) ---
            Spawn3DBoulder("Obstacle02_RavineStepBoulder", obstaclesRoot.transform, new Vector3(15.90f, -0.35f, 0.0f), new Vector3(1.15f, 0.85f, 1.10f), 40f, false);
            Spawn3DBoulder("Obstacle02_RavineWestLipRock", obstaclesRoot.transform, new Vector3(14.35f, 1.22f, 0.85f), new Vector3(1.10f, 0.45f, 0.90f), 15f, false);
            Spawn3DBoulder("Obstacle02_RavineEastLipRock", obstaclesRoot.transform, new Vector3(17.45f, 1.22f, 0.85f), new Vector3(1.10f, 0.45f, 0.90f), 165f, false);

            // --- OBSTACLE 3: FALLEN TREE TRUNK & ROOTS CRAWL TUNNEL ("leo xuống dưới một gốc cây đổ", X = 23.8m .. 26.4m) ---
            var fallenTreeRoot = new GameObject("Obstacle03_FallenTreeTrunk_Crawl");
            fallenTreeRoot.transform.SetParent(obstaclesRoot.transform, false);
            fallenTreeRoot.transform.position = new Vector3(25.10f, -1.10f, 0f);

            // Ceiling BoxCollider blocking standing player (bottom at Y = -0.18m, which is 0.92m above the Y = -1.10m floor -> requires Crouch/Crawl!)
            var trunkColGO = new GameObject("FallenTrunk_LowCeilingCollider");
            trunkColGO.transform.SetParent(fallenTreeRoot.transform, false);
            trunkColGO.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            var trunkBC = trunkColGO.AddComponent<BoxCollider>();
            trunkBC.size = new Vector3(2.50f, 1.45f, 3.20f); // Bottom of box = -1.10 + 1.65 - 0.725 = -0.175m (0.925m clearance above -1.10m floor)

            // 3D Fallen Log nestled naturally inside 3D Mossy Rock Pillars on both ends
            var logMeshGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            logMeshGO.name = "FallenTrunk_3DLog";
            logMeshGO.transform.SetParent(fallenTreeRoot.transform, false);
            logMeshGO.transform.localPosition = new Vector3(0f, 1.28f, -0.05f);
            logMeshGO.transform.localRotation = Quaternion.Euler(8f, 0f, 86f);
            logMeshGO.transform.localScale = new Vector3(0.68f, 1.45f, 0.68f);
            logMeshGO.GetComponent<MeshRenderer>().sharedMaterial = mats.TimberDarkMat;
            UnityEngine.Object.DestroyImmediate(logMeshGO.GetComponent<Collider>());

            Spawn3DBoulder("FallenTrunk_RootRockArch_Top",   fallenTreeRoot.transform, new Vector3(25.10f,  0.42f,  0.10f), new Vector3(2.95f, 0.88f, 1.55f), 110f, false);
            Spawn3DBoulder("FallenTrunk_RockPillar_LeftTop", fallenTreeRoot.transform, new Vector3(23.75f,  0.15f, -0.05f), new Vector3(1.55f, 0.95f, 1.35f),  45f, false);
            Spawn3DBoulder("FallenTrunk_RockPillar_RightTop",fallenTreeRoot.transform, new Vector3(26.45f,  0.10f, -0.05f), new Vector3(1.55f, 0.95f, 1.35f), 215f, false);
            Spawn3DBoulder("FallenTrunk_RootRock_LeftBase",  fallenTreeRoot.transform, new Vector3(23.60f, -1.10f,  0.85f), new Vector3(1.55f, 0.95f, 1.25f),  25f, false);
            Spawn3DBoulder("FallenTrunk_RootRock_RightBase", fallenTreeRoot.transform, new Vector3(26.60f, -1.10f,  0.85f), new Vector3(1.55f, 0.95f, 1.25f), 195f, false);

            // --- OBSTACLE 4: 3D PUSH/PULL WOODEN CRATE (X = 30.8m, Y = -1.10m -> Push to Cliff Corner at X = 34.7m) ---
            var crateModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Props/SM_WoodenCrate.fbx")
                          ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Props/PF_Prop_CargoCrate_3D.prefab");
            var crateMat   = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Props/M_WoodenCrate_01.mat") ?? mats.TimberDarkMat;

            var crateGO = new GameObject("Obstacle04_PushPull_WoodenCrate");
            crateGO.transform.SetParent(obstaclesRoot.transform, false);
            crateGO.transform.position = new Vector3(30.80f, -0.50f, 0f);
            crateGO.transform.rotation = Quaternion.identity;

            if (crateModel != null)
            {
                var visualInst = (GameObject)PrefabUtility.InstantiatePrefab(crateModel, crateGO.transform);
                if (PrefabUtility.IsPartOfPrefabInstance(visualInst))
                {
                    PrefabUtility.UnpackPrefabInstance(visualInst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                }
                visualInst.name = "WoodenCrate_3DMesh";
                visualInst.transform.localPosition = new Vector3(0f, -0.58f, 0f);
                visualInst.transform.localRotation = Quaternion.identity;
                visualInst.transform.localScale = new Vector3(0.84f, 0.78f, 0.84f);
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
                cube.transform.localScale = new Vector3(1.15f, 1.15f, 1.15f);
                UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>());
                if (crateMat != null) cube.GetComponent<MeshRenderer>().sharedMaterial = crateMat;
            }

            var crateBC = crateGO.AddComponent<BoxCollider>();
            crateBC.size = new Vector3(1.15f, 1.18f, 1.15f);
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

            // 0. Initial Forest Clearing Movement
            SpawnPromptTrigger("Prompt_00_Move_ForestClearing", new Vector3(-3.0f, -1.20f, 0f), new Vector3(4.5f, 3.5f, 4.0f), PlanetOfLanaPromptSystem.PromptType.Move);

            // 1. Obstacle 1: Cliff Rope Grab
            SpawnPromptTrigger("Prompt_01_RopeGrab_Cliff", new Vector3(4.50f, -0.85f, 0f), new Vector3(3.2f, 3.5f, 4.0f), PlanetOfLanaPromptSystem.PromptType.RopeGrab);

            // 2. Obstacle 2: Rocky Ravine Jump
            SpawnPromptTrigger("Prompt_02_Jump_RockyRavine", new Vector3(13.60f, 2.05f, 0f), new Vector3(3.2f, 3.5f, 4.0f), PlanetOfLanaPromptSystem.PromptType.Jump);

            // 3. Obstacle 3: Fallen Tree Trunk Crawl
            SpawnPromptTrigger("Prompt_03_Crawl_FallenTrunk", new Vector3(22.80f, -0.40f, 0f), new Vector3(3.2f, 3.5f, 4.0f), PlanetOfLanaPromptSystem.PromptType.Crawl);

            // 4. Obstacle 4: Push/Pull Wooden Crate
            SpawnPromptTrigger("Prompt_04_PushPull_WoodenCrate", new Vector3(30.20f, -0.45f, 0f), new Vector3(3.5f, 3.5f, 4.0f), PlanetOfLanaPromptSystem.PromptType.PushPull);

            // 5. Obstacle 5: High Cliff Ledge Climb
            SpawnPromptTrigger("Prompt_05_ClimbLedge_HighCliff", new Vector3(34.20f, 1.25f, 0f), new Vector3(2.5f, 3.5f, 4.0f), PlanetOfLanaPromptSystem.PromptType.ClimbLedge);

            // 6. Zone 4 Village Interaction
            SpawnPromptTrigger("Prompt_06_Talk_VillagerNPC", new Vector3(99.50f, -0.80f, 0f), new Vector3(3.5f, 3.5f, 4.0f), PlanetOfLanaPromptSystem.PromptType.Interact);
        }

        // =========================================================================================
        // 6. POPULATE 100% UNIFIED BORDERLESS PLANET OF LANA 2D ECOSYSTEM
        //    (Strictly using Lana_VN_Pack + Lana_Unified_Art — ZERO Krita/outlined trees!)
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

            // Authentic High-Fidelity Painterly Planet of Lana Trees & Landscape Silhouettes
            Sprite sprTreeFullIsolated = LoadSprite("Assets/Textures/Tree/Tree_Full_Isolated.png", new Vector2(0.5f, 0.02f));
            Sprite sprCurvedTreeV2Full = LoadSprite("Assets/Textures/Tree/Curved_Tree_V2_Full.png", new Vector2(0.5f, 0.02f));
            Sprite sprTreeSlenderHigh  = LoadSprite("Assets/Textures/Tree/Tree_Lana_SlenderHigh.png", new Vector2(0.5f, 0.02f));
            Sprite sprTreeWideCanopy   = LoadSprite("Assets/Textures/Tree/Tree_Lana_WideCanopy.png", new Vector2(0.5f, 0.02f));
            Sprite sprTreePoplarTall   = LoadSprite("Assets/Textures/Tree/Tree_Lana_PoplarTall.png", new Vector2(0.5f, 0.02f));
            Sprite sprMidgroundHills   = LoadSprite("Assets/Textures/Tree/sau 1.png", new Vector2(0.5f, 0.10f));
            Sprite sprRepoussoirTop    = LoadSprite("Assets/Textures/Tree/repoussoir_tree_framing.png", new Vector2(0.5f, 0.5f));

            // Unified & Pack Sprites
            Sprite sprTreeHeroBanyan   = sprTreeFullIsolated ?? LoadSprite($"{UnifiedTexRoot}/Trees/T_Lana_Tree_01_HeroBanyan.png", new Vector2(0.5f, 0.02f));
            Sprite sprTreeWindblown    = sprCurvedTreeV2Full ?? LoadSprite($"{UnifiedTexRoot}/Trees/T_Lana_Tree_02_WindblownCurved.png", new Vector2(0.5f, 0.02f));
            Sprite sprTreeSlender      = sprTreeSlenderHigh  ?? LoadSprite($"{UnifiedTexRoot}/Trees/T_Lana_Tree_03_SlenderForest.png", new Vector2(0.5f, 0.02f));
            Sprite sprCurvedTreeV2     = sprCurvedTreeV2Full;
            Sprite sprTreeIsolated     = sprTreeFullIsolated;
            Sprite sprBranchOver       = LoadSprite($"{UnifiedTexRoot}/Branches/T_Lana_Branch_01_Overhanging.png", new Vector2(0.5f, 0.5f));
            Sprite sprCanopyTop        = sprRepoussoirTop ?? sprBranchOver;
            Sprite sprCanopyFraming    = sprRepoussoirTop ?? sprBranchOver;
            Sprite sprFoliageLeft      = sprRepoussoirTop ?? sprBranchOver;
            Sprite sprFoliageRight     = sprRepoussoirTop ?? sprBranchOver;
            Sprite sprFallenTrunkMossy = LoadSprite($"{UnifiedTexRoot}/Branches/T_Lana_FallenTrunk_Mossy.png", new Vector2(0.5f, 0.1f));

            // Authentic Hand-Painted Lana VN Pack Trees matching 50_User_Reference_Forest_Vista.jpg 1:1
            Sprite sprTree01Large   = LoadSprite($"{PackTexRoot}/Trees/T_LanaVN_Tree_01_LargeCanopy.png", new Vector2(0.5f, 0.02f)) ?? sprTreeFullIsolated;
            Sprite sprTree02Medium  = LoadSprite($"{PackTexRoot}/Trees/T_LanaVN_Tree_02_Medium.png", new Vector2(0.5f, 0.02f)) ?? sprCurvedTreeV2Full;
            Sprite sprTree03Small   = LoadSprite($"{PackTexRoot}/Trees/T_LanaVN_Tree_03_SmallBushTree.png", new Vector2(0.5f, 0.02f)) ?? sprTreeSlenderHigh;
            Sprite sprTree04Tall    = LoadSprite($"{PackTexRoot}/Trees/T_LanaVN_Tree_04_SlenderTall.png", new Vector2(0.5f, 0.02f)) ?? sprTreeSlenderHigh;
            Sprite sprTree05Banana  = LoadSprite($"{PackTexRoot}/Trees/T_LanaVN_Tree_05_Banana.png", new Vector2(0.5f, 0.02f));
            Sprite sprTree06Coconut = LoadSprite($"{PackTexRoot}/Trees/T_LanaVN_Tree_06_Coconut.png", new Vector2(0.45f, 0.02f));
            Sprite sprTree07Bamboo  = LoadSprite($"{PackTexRoot}/Trees/T_LanaVN_Tree_07_BambooGrove.png", new Vector2(0.5f, 0.02f));

            // Authentic Hand-Painted Lana VN Rocks matching 50_User_Reference_Forest_Vista.jpg 1:1
            Sprite sprRock01Large   = LoadSprite($"{PackTexRoot}/Rocks/T_LanaVN_Rock_01_LargeOutcrop.png", new Vector2(0.5f, 0.10f));
            Sprite sprRock02MossyTop= LoadSprite($"{PackTexRoot}/Rocks/T_LanaVN_Rock_02_MossyMediumTop.png", new Vector2(0.5f, 0.10f));
            Sprite sprRock03MossyBot= LoadSprite($"{PackTexRoot}/Rocks/T_LanaVN_Rock_03_MossyMediumBot.png", new Vector2(0.5f, 0.10f));
            Sprite sprRock04FlatSlab= LoadSprite($"{PackTexRoot}/Rocks/T_LanaVN_Rock_04_FlatSlab.png", new Vector2(0.5f, 0.10f));
            Sprite sprRock05Small   = LoadSprite($"{PackTexRoot}/Rocks/T_LanaVN_Rock_05_SmallMossy.png", new Vector2(0.5f, 0.10f));
            Sprite sprRock06PebblesTop = LoadSprite($"{PackTexRoot}/Rocks/T_LanaVN_Rock_06_PebbleGroupTop.png", new Vector2(0.5f, 0.10f));
            Sprite sprRock07PebblesBot = LoadSprite($"{PackTexRoot}/Rocks/T_LanaVN_Rock_07_PebbleGroupBot.png", new Vector2(0.5f, 0.10f));

            // Atmospheric Cloud for Opening Vista Overlook
            Sprite sprCloudHero     = LoadSprite("Assets/Textures/Lana_Unified_Art/Clouds/T_Lana_Cloud_01_CumulusHero.png", new Vector2(0.5f, 0.5f))
                                   ?? LoadSprite("Assets/Textures/Clouds/lana_cloud_01.png", new Vector2(0.5f, 0.5f));

            Sprite sprTreeGold01    = LoadSprite($"{UnifiedTexRoot}/Trees/T_Lana_Tree_GoldenVillage_01.png", new Vector2(0.5f, 0.02f)) ?? sprTreeFullIsolated;
            Sprite sprTreeGold02    = LoadSprite($"{UnifiedTexRoot}/Trees/T_Lana_Tree_GoldenVillage_02.png", new Vector2(0.5f, 0.02f)) ?? sprCurvedTreeV2Full;

            Sprite sprBushMound1    = LoadSprite($"{UnifiedTexRoot}/Bushes/T_Lana_Bush_01_LushMound.png", new Vector2(0.5f, 0.08f));
            Sprite sprBushMound2    = LoadSprite($"{UnifiedTexRoot}/Bushes/T_Lana_Bush_02_WideCluster.png", new Vector2(0.5f, 0.08f));
            Sprite sprBushFlower3   = LoadSprite($"{UnifiedTexRoot}/Bushes/T_Lana_Bush_03_Flowering.png", new Vector2(0.5f, 0.08f));

            Sprite[] sprBushes = new[]
            {
                LoadSprite($"{PackTexRoot}/Bushes/T_LanaVN_Bush_Low_01.png", new Vector2(0.5f, 0.04f)),
                LoadSprite($"{PackTexRoot}/Bushes/T_LanaVN_Bush_Low_02_Flower.png", new Vector2(0.5f, 0.04f)),
                LoadSprite($"{PackTexRoot}/Bushes/T_LanaVN_Bush_Low_03.png", new Vector2(0.5f, 0.04f)),
                LoadSprite($"{PackTexRoot}/Bushes/T_LanaVN_Bush_Low_04.png", new Vector2(0.5f, 0.04f)),
                sprBushMound1 ?? LoadSprite($"{PackTexRoot}/Bushes/T_LanaVN_Bush_Broadleaf_01.png", new Vector2(0.5f, 0.04f)),
                sprBushMound2 ?? LoadSprite($"{PackTexRoot}/Bushes/T_LanaVN_Bush_Broadleaf_02.png", new Vector2(0.5f, 0.04f))
            };
            Sprite sprBushBroad1 = LoadSprite($"{PackTexRoot}/Bushes/T_LanaVN_Bush_Broadleaf_01.png", new Vector2(0.5f, 0.04f));
            Sprite sprBushBroad2 = LoadSprite($"{PackTexRoot}/Bushes/T_LanaVN_Bush_Broadleaf_02.png", new Vector2(0.5f, 0.04f));
            Sprite sprBushGold01 = LoadSprite($"{UnifiedTexRoot}/Bushes/T_Lana_Bush_GoldenVillage_01.png", new Vector2(0.5f, 0.04f)) ?? sprBushes[0];
            Sprite sprBushGold02 = LoadSprite($"{UnifiedTexRoot}/Bushes/T_Lana_Bush_GoldenVillage_02.png", new Vector2(0.5f, 0.04f)) ?? sprBushes[2];

            Sprite sprGrassTuft1 = LoadSprite($"{UnifiedTexRoot}/Foliage/T_Lana_Grass_01_SoftTuft.png", new Vector2(0.5f, 0.08f));
            Sprite sprGrassTuft2 = LoadSprite($"{UnifiedTexRoot}/Foliage/T_Lana_Grass_02_WindsweptClump.png", new Vector2(0.5f, 0.08f));

            // Use only tapered organic grass carpets (03, 04, 05_Dense, 06_Shrubby) so there are zero hard rectangular crop edges!
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

            // Golden Rice Field Sprites (derived from cleaned Lana_VN_Pack + Lana_Unified_Art so brushwork is 100% identical!)
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

            // Parallax depth color palettes (Planet of Lana atmospheric depth layering: Far -> Mid -> Near)
            Color farForestCol  = new Color(0.70f, 0.84f, 0.80f, 0.94f);
            Color midForestCol  = new Color(0.86f, 0.95f, 0.86f, 1.00f);
            Color nearForestCol = new Color(0.98f, 1.00f, 0.95f, 1.00f);
            Color fgShadowCol   = new Color(0.14f, 0.24f, 0.22f, 0.95f);

            // =====================================================================================
            // A. SKY-TO-FOREST ENTRY CORNER FRAMING (REPOUSSOIR)
            //    Matches exact reference 50_User_Reference_Forest_Vista.jpg:
            //    - Top-Left: High overhanging framing branches and dark repoussoir foliage.
            //    - Bottom-Left: Dark repoussoir bush silhouette.
            //    - Top-Right: Framing canopy branch framing above the right cliff.
            // =====================================================================================
            // Top-Left Corner Silhouette Branch & Foliage
            SpawnSprite("Repoussoir_TopLeft_HighBranch", fgDescentGroup.transform, sprRepoussoirTop ?? sprBranchOver,
                new Vector3(-7.2f, 4.35f, -1.55f), new Vector2(1.30f, 1.25f), fgShadowCol, 28, 0.8f, 0.7f);

            // Overhanging top foliage branches hanging strictly from the upper-left leaning tree
            SpawnSprite("Overhang_Left_Branch1", treesGroup.transform, sprBranchOver,
                new Vector3(-6.2f, 4.25f, 0.85f), new Vector2(1.15f, 1.10f), nearForestCol, 15, 0.8f, 0.7f, false, -8f);

            // Left corner foreground ground bush
            SpawnSprite("Repoussoir_BottomLeft_Bush", fgDescentGroup.transform, sprBushBroad1,
                new Vector3(-7.2f, -1.90f, -1.95f), new Vector2(1.10f, 1.05f), fgShadowCol, 27, 1.4f, 1.2f);

            // Right corner foreground ground bush
            SpawnSprite("Repoussoir_BottomRight_Bush", fgDescentGroup.transform, sprBushBroad1,
                new Vector3(8.0f, -1.85f, -1.95f), new Vector2(0.95f, 0.90f), fgShadowCol, 27, 1.4f, 1.2f, true);

            // Bottom foreground shadow grass blades framing the entire lower screen edge (matches 50_User_Reference_Forest_Vista.jpg)
            float[] fgGrassXs = new float[] { -7.5f, -6.0f, -4.5f, -3.0f, -1.5f, 0.2f, 1.8f, 3.5f, 5.0f, 6.8f, 8.2f };
            for (int fgIdx = 0; fgIdx < fgGrassXs.Length; fgIdx++)
            {
                float fgX = fgGrassXs[fgIdx];
                bool isCorner = (fgX < -3.5f || fgX > 5.0f);
                float fgY = SampleTerrainSurfaceY(fgX) - (isCorner ? 0.12f : 0.28f);
                Sprite fgGrassSpr = isCorner ? ((fgIdx % 2 == 0) ? sprFlowers[1] : sprGrassCarpets[3]) : sprGrassCarpets[1];
                Vector2 fgScale = isCorner ? new Vector2(0.95f, 0.80f) : new Vector2(0.85f, 0.45f);
                SpawnSprite($"Repoussoir_Bottom_Grass_{fgIdx:D2}", fgDescentGroup.transform, fgGrassSpr,
                    new Vector3(fgX, fgY, -2.05f), fgScale, fgShadowCol, 27, 1.2f, 1.0f, (fgIdx % 2 == 1));
            }

            // Landmark framing branches at the Cliff & Ravine edges
            if (sprBranchOver != null)
            {
                SpawnSprite("Cliff_Framing_Branch", fgDescentGroup.transform, sprBranchOver,
                    new Vector3(12.5f, 5.8f, -1.50f), new Vector2(1.05f, 1.00f), fgShadowCol, 28, 0.9f, 0.8f);
                SpawnSprite("Crawl_Framing_Branch", fgDescentGroup.transform, sprBranchOver,
                    new Vector3(26.0f, 4.2f, -1.50f), new Vector2(1.00f, 0.95f), fgShadowCol, 28, 0.9f, 0.8f, true);
            }

            // =====================================================================================
            // B. ZONE 1 (-12m to +35.5m): 3-PLANE PLANET OF LANA ANCIENT FOREST
            //    RECREATED 1:1 TO MATCH EXACT REFERENCE: 50_User_Reference_Forest_Vista.jpg
            // =====================================================================================
            // 0. Distant Midground Forest Ridge (strictly skipping X in [-8.0m, +18.0m] to keep the negative space vista completely unobstructed!)
            float[] ridgeXs = new float[] { -12.0f, -8.5f, 18.5f, 25.5f, 33.5f };
            for (int rIdx = 0; rIdx < ridgeXs.Length; rIdx++)
            {
                float rx = ridgeXs[rIdx];
                float ry = (rx >= 8.0f && rx <= 21.0f) ? 0.35f : ((rx > 21.0f) ? -0.85f : -1.45f);
                float rz = 6.5f + (rIdx % 2) * 0.3f;
                SpawnSprite($"Forest_MidRidge_{rIdx:D2}", treesGroup.transform, sprMidgroundHills,
                    new Vector3(rx, ry, rz), new Vector2(2.8f, 2.3f), new Color(0.65f, 0.80f, 0.78f, 0.94f), 4, 0.5f, 0.6f, (rIdx % 2 == 1));
            }

            // 1. Soft Atmospheric Background Silhouettes (Z = 5.2m .. 5.8m, muted teal haze)
            //    Negative space between X = -8.5m and +18.0m is kept 100% CLEAR of background trees!
            float[] bgTreeXs = new float[] { -11.5f, -9.0f, 18.5f, 24.0f, 30.0f };
            for (int tIdx = 0; tIdx < bgTreeXs.Length; tIdx++)
            {
                float bx = bgTreeXs[tIdx];
                float by = (bx >= 7.5f && bx <= 21.0f) ? 0.95f : ((bx > 21.0f) ? -1.35f : -2.18f);
                Sprite bTreeSpr = (tIdx % 3 == 0) ? sprTreeWideCanopy : ((tIdx % 3 == 1) ? sprTreePoplarTall : sprTreeSlenderHigh);
                Vector2 bScale = (tIdx % 3 == 0) ? new Vector2(1.8f, 1.8f) : ((tIdx % 3 == 1) ? new Vector2(1.9f, 1.9f) : new Vector2(2.1f, 2.1f));
                float bZ = 5.2f + (tIdx % 3) * 0.25f;
                SpawnSprite($"Forest_BG_Tree_{tIdx:D2}", treesGroup.transform, bTreeSpr,
                    new Vector3(bx, by, bZ), bScale, farForestCol, 5, 0.8f, 0.9f, (tIdx % 2 == 1));
            }

            // 2. HERO COMPOSITION MATCHING 50_User_Reference_Forest_Vista.jpg 1:1:
            // --- A. LEFT EMBANKMENT (X = -8.5m .. -4.0m) ---
            // Leaning giant tree arching over the rocks into the glade (Z = 0.70m, rooted in embankment)
            SpawnSprite("Opening_Tree01_LargeCanopy_HeroLeaning", treesGroup.transform, sprTree01Large,
                new Vector3(-5.80f, -0.70f, 0.70f), new Vector2(1.35f, 1.35f), nearForestCol, 14, 0.8f, 0.8f, false, -12.0f);

            // Layered receding forest wall behind the embankment
            SpawnSprite("Opening_BG_Tree_Left1", treesGroup.transform, sprTreeWideCanopy,
                new Vector3(-8.2f, -0.20f, 3.2f), new Vector2(1.7f, 1.7f), farForestCol, 5, 0.7f, 0.8f);
            SpawnSprite("Opening_BG_Tree_Left2", treesGroup.transform, sprTreeSlenderHigh,
                new Vector3(-6.8f, 0.40f, 3.8f), new Vector2(1.8f, 1.8f), farForestCol, 4, 0.7f, 0.8f);

            // Stacked mossy rock outcrop at the base of the embankment
            SpawnSprite("Opening_Rock01_LargeOutcrop", rocksGroup.transform, sprRock01Large,
                new Vector3(-5.80f, -0.90f, 0.85f), new Vector2(1.20f, 1.20f), nearForestCol, 12, 0f, 0f);
            SpawnSprite("Opening_Rock02_MossyMediumTop", rocksGroup.transform, sprRock02MossyTop,
                new Vector3(-4.50f, -1.45f, 0.65f), new Vector2(0.95f, 0.95f), nearForestCol, 12, 0f, 0f);
            SpawnSprite("Opening_Rock05_SmallMossy_Embankment", rocksGroup.transform, sprRock05Small,
                new Vector3(-3.80f, -1.75f, 0.45f), new Vector2(0.85f, 0.85f), nearForestCol, 12, 0f, 0f);

            // --- B. CENTER MEADOW CLEARING (X = -4.0m .. +5.2m) ---
            // Tree A (left of center): Slender midground tree
            SpawnSprite("Opening_Tree03_SmallBushTree_MidLeft", treesGroup.transform, sprTree03Small,
                new Vector3(-3.60f, -1.78f, 2.20f), new Vector2(0.92f, 0.92f), nearForestCol, 10, 1.0f, 1.0f);

            // Tree B (Center Hero Tree): Iconic Dual-Bough Branching Tree with sunlit canopy against the blue sky
            SpawnSprite("Opening_Tree02_Medium_CenterHero", treesGroup.transform, sprTree02Medium,
                new Vector3(-1.10f, -1.88f, 1.85f), new Vector2(0.95f, 0.95f), nearForestCol, 10, 0.9f, 0.9f);

            // Path surface pebbles
            SpawnSprite("Opening_Pebbles_01", rocksGroup.transform, sprRock06PebblesTop,
                new Vector3(-2.80f, -1.82f, 0.35f), new Vector2(0.70f, 0.70f), nearForestCol, 13, 0f, 0f);
            SpawnSprite("Opening_Pebbles_02", rocksGroup.transform, sprRock07PebblesBot,
                new Vector3( 0.80f, -1.95f, 0.35f), new Vector2(0.70f, 0.70f), nearForestCol, 13, 0f, 0f);
            SpawnSprite("Opening_Rock05_PathDip", rocksGroup.transform, sprRock05Small,
                new Vector3( 2.20f, -2.00f, 0.35f), new Vector2(0.65f, 0.65f), nearForestCol, 13, 0f, 0f);

            // Weathered angled deadwood stump at X = 3.8m
            SpawnSprite("Opening_Deadwood_Stump", treesGroup.transform, sprFallenTrunkMossy ?? sprBranchOver,
                new Vector3(3.80f, -1.95f, 0.60f), new Vector2(0.55f, 0.55f), nearForestCol, 11, 0f, 0f, false, 18f);

            // --- C. RIGHT CLIFF PLATEAU & OVERLOOK (X >= 5.20m) ---
            // Hand-painted rock cliff face strata & base mossy rocks (Z = 0.05m..0.10m, in front of 3D ledge mesh!)
            SpawnSprite("Cliff_Rock04_FlatSlab_Face", rocksGroup.transform, sprRock04FlatSlab,
                new Vector3(5.60f, -1.15f, 0.05f), new Vector2(1.25f, 1.15f), nearForestCol, 16, 0f, 0f);
            SpawnSprite("Cliff_Rock03_MossyBase", rocksGroup.transform, sprRock03MossyBot,
                new Vector3(5.15f, -1.75f, 0.10f), new Vector2(1.10f, 1.00f), nearForestCol, 16, 0f, 0f);
            SpawnSprite("Cliff_Rock05_Small_Edge", rocksGroup.transform, sprRock05Small,
                new Vector3(5.35f, -0.45f, 0.05f), new Vector2(0.85f, 0.85f), nearForestCol, 16, 0f, 0f);

            // Plateau edge trees overlooking the valley (matching reference painting)
            SpawnSprite("Cliff_Tree03_PlateauEdge", treesGroup.transform, sprTree03Small,
                new Vector3(6.50f, -0.45f, 1.80f), new Vector2(0.85f, 0.85f), nearForestCol, 10, 0.9f, 0.9f);
            SpawnSprite("Cliff_Tree04_SlenderTall", treesGroup.transform, sprTree04Tall,
                new Vector3(7.80f, -0.45f, 1.50f), new Vector2(0.88f, 0.88f), nearForestCol, 10, 1.0f, 1.0f);

            // --- D. DISTANT NEGATIVE SPACE VISTA & PUFFY CUMULUS CLOUD (X in [0.0m, 5.2m]) ---
            // Magnificent puffy white cumulus cloud bank rising on the right side of the opening vista (matching 50_User_Reference_Forest_Vista.jpg 1:1)
            SpawnSprite("Vista_HeroCumulusCloud_Right", ecoRoot.transform, sprCloudHero,
                new Vector3(3.50f, 1.35f, 8.50f), new Vector2(3.80f, 2.60f), new Color(1.0f, 1.0f, 1.0f, 0.98f), 3, 0f, 0f, true);

            // Zone 1 Landmark Trees: Cliff, Ravine, Crawlway
            SpawnSprite("Cliff_HeroTree_Base", treesGroup.transform, sprTreeFullIsolated,
                new Vector3(11.5f, 1.02f, 2.40f), new Vector2(0.56f, 0.56f), nearForestCol, 10, 0.8f, 0.9f);
            SpawnSprite("Ravine_HeroTree_Edge", treesGroup.transform, sprCurvedTreeV2Full,
                new Vector3(19.2f, 1.02f, 2.30f), new Vector2(0.85f, 0.85f), nearForestCol, 10, 0.9f, 0.9f, true);
            SpawnSprite("Crawl_HeroTree_Overhang", treesGroup.transform, sprTreeWideCanopy,
                new Vector3(27.8f, -1.25f, 2.20f), new Vector2(1.8f, 1.8f), nearForestCol, 9, 0.9f, 1.0f);
            SpawnSprite("CrateWall_HeroTree_End", treesGroup.transform, sprTreeSlenderHigh,
                new Vector3(33.5f, -1.25f, 2.20f), new Vector2(2.1f, 2.1f), nearForestCol, 10, 1.0f, 1.0f);

            // 3. Opening Clearing Mossy Boulders
            SpawnBoulder("Forest_ClearBoulder_Ravine", new Vector3(12.5f, 1.05f, 1.35f), new Vector3(1.35f, 0.60f, 1.0f), 30f);
            SpawnBoulder("Forest_ClearBoulder_Crawl",  new Vector3(21.8f, 0.45f, 1.35f), new Vector3(1.20f, 0.52f, 0.95f), 140f);

            // 4. Upper Rope Cliff, Vines & Ravine Dressing
            SpawnSprite("Cliff_Vine_RopeWall", bushesGroup.transform, sprVineCreep,   new Vector3( 5.55f, -0.45f, -0.8f), new Vector2(1.10f, 1.10f), nearForestCol, 16, 1.8f, 1.5f);
            SpawnSprite("Cliff_Vine_RavineW",  bushesGroup.transform, sprVineCreep,   new Vector3(14.55f, 1.15f, -0.8f), new Vector2(1.00f, 1.00f), nearForestCol, 16, 1.8f, 1.5f);
            SpawnSprite("Cliff_Vine_RavineE",  bushesGroup.transform, sprVineCreep,   new Vector3(17.25f, 1.15f, -0.8f), new Vector2(1.00f, 1.00f), nearForestCol, 16, 1.8f, 1.5f, true);

            // 5. Fallen Tree Crawl & Crate Hollow (+22.0m to +35.5m, Y = -1.10m)
            SpawnSprite("FallenTree_TopBush1",  bushesGroup.transform, sprBushBroad1,  new Vector3(24.5f,  0.42f, -0.25f), new Vector2(0.75f, 0.72f), nearForestCol, 16, 1.5f, 1.4f);
            SpawnSprite("FallenTree_TopBush2",  bushesGroup.transform, sprBushes[1],   new Vector3(25.6f,  0.48f, -0.20f), new Vector2(0.85f, 0.80f), nearForestCol, 17, 1.6f, 1.5f);
            SpawnSprite("FallenTree_HangVine1", bushesGroup.transform, sprVineCreep,   new Vector3(24.3f,  0.15f, -0.45f), new Vector2(0.82f, 0.75f), nearForestCol, 18, 2.0f, 1.7f);
            SpawnSprite("FallenTree_HangVine2", bushesGroup.transform, sprVineCluster, new Vector3(25.4f,  0.22f, -0.45f), new Vector2(0.78f, 0.75f), nearForestCol, 18, 2.0f, 1.7f);
            SpawnSprite("CrateWall_HangVine",   bushesGroup.transform, sprVineCreep,   new Vector3(35.45f, 2.05f, -0.8f), new Vector2(1.15f, 1.15f), nearForestCol, 16, 1.8f, 1.5f);

            // =====================================================================================
            // C. ZONE 2 (+35.5m to +62.0m): HIGH CLIFF OVERLOOK & DOWNHILL SLOPE ("đi xuống dốc đồi")
            // =====================================================================================
            // Hero Tree crowning the High Cliff Overlook (X = 37.2m, Y = +2.15m) + Mossy Slabs
            SpawnBoulder("Overlook_CliffBoulder_1", new Vector3(37.0f, 2.13f, 1.35f), new Vector3(1.85f, 0.72f, 1.25f), 25f);
            SpawnBoulder("Overlook_CliffBoulder_2", new Vector3(41.8f, 2.13f, 1.40f), new Vector3(1.55f, 0.58f, 1.15f), 145f);
            SpawnSprite("Overlook_HeroTree_Crown",  treesGroup.transform, sprTree01Large, new Vector3(37.4f, 1.92f, 2.2f), new Vector2(0.62f, 0.62f), nearForestCol, 10, 0.9f, 1.0f);

            // Trees & Boulders along the Downhill Slope (X = 44m..61m), kept at Z >= 2.8m so the view of the hill & village below stays open!
            SpawnBoulder("HillSlope_Boulder_1", new Vector3(47.5f, SampleTerrainSurfaceY(47.5f) - 0.04f, 1.45f), new Vector3(1.40f, 0.55f, 1.10f), 35f);
            SpawnBoulder("HillSlope_Boulder_2", new Vector3(53.5f, SampleTerrainSurfaceY(53.5f) - 0.04f, 1.45f), new Vector3(1.25f, 0.50f, 1.00f), 120f);
            SpawnBoulder("HillSlope_Boulder_3", new Vector3(59.0f, SampleTerrainSurfaceY(59.0f) - 0.04f, 1.45f), new Vector3(1.50f, 0.60f, 1.15f), 200f);

            SpawnSprite("HillSlope_Tree_1", treesGroup.transform, sprTreeSlenderHigh,  new Vector3(48.2f, SampleTerrainSurfaceY(48.2f) - 0.25f, 3.0f), new Vector2(1.8f, 1.8f), midForestCol, 7, 1.2f, 1.2f);
            SpawnSprite("HillSlope_Tree_2", treesGroup.transform, sprTreeGold02,   new Vector3(55.8f, SampleTerrainSurfaceY(55.8f) - 0.28f, 3.2f), new Vector2(0.52f, 0.52f), nearForestCol, 7, 1.1f, 1.1f);
            SpawnSprite("HillSlope_Palm_3", treesGroup.transform, sprTree05Banana, new Vector3(60.5f, SampleTerrainSurfaceY(60.5f) - 0.25f, 2.6f), new Vector2(0.48f, 0.48f), nearForestCol, 9, 1.5f, 1.3f);

            // =====================================================================================
            // D. CLEAN, RHYTHMIC PLANET OF LANA MEADOW & GROUND FLORA (-11.5m to +62.0m)
            //    Spacious 2.2m step intervals for clean walkable paths, zero clutter or object spam!
            // =====================================================================================
            int carpetIdx = 0;
            for (float x = -11.5f; x <= 61.5f; x += 2.2f)
            {
                if (x > 14.0f && x < 17.8f) continue; // Skip ravine chasm

                float y = SampleTerrainSurfaceY(x);
                float slopeRotZ = 0f;
                if (x >= 43.5f && x < 62.0f) slopeRotZ = -11.5f; // Align grass carpets with hill slope

                float goldBlend = Mathf.InverseLerp(48.0f, 62.0f, x);
                Color grassCol = Color.Lerp(new Color(0.92f, 0.98f, 0.88f, 1f), new Color(0.98f, 0.92f, 0.72f, 1f), goldBlend);
                Color frontGrassCol = Color.Lerp(new Color(0.32f, 0.58f, 0.34f, 1f), new Color(0.68f, 0.62f, 0.28f, 1f), goldBlend);

                float scaleJitter = 0.95f + Mathf.Abs(Mathf.Sin(x * 3.7f)) * 0.15f;

                // 1. Midground Soft Grass Carpet behind walking path (Z = 0.88m)
                bool inOpeningClearing = (x >= -3.0f && x <= 4.8f);
                Sprite gSpr = inOpeningClearing ? sprGrassCarpets[0] : sprGrassCarpets[carpetIdx % sprGrassCarpets.Length];
                float backScaleY = inOpeningClearing ? 0.65f : 1.15f;
                SpawnSprite($"MeadowCarpet_Back_{carpetIdx:D2}", grassGroup.transform, gSpr,
                    new Vector3(x, y - 0.02f, 0.88f), new Vector2(1.10f * scaleJitter, backScaleY * scaleJitter), grassCol, 12, 1.8f, 1.6f, (carpetIdx % 2 == 0), slopeRotZ);

                // 2. Front Lip Low Grass Carpet (Z = -1.96m)
                if (x < 29.2f || x > 32.5f)
                {
                    Sprite fgSpr = inOpeningClearing ? sprGrassCarpets[1] : sprGrassCarpets[(carpetIdx + 1) % sprGrassCarpets.Length];
                    float frontScaleY = inOpeningClearing ? 0.45f : 0.85f;
                    SpawnSprite($"MeadowCarpet_Front_{carpetIdx:D2}", grassGroup.transform, fgSpr,
                        new Vector3(x + 0.3f, y - 0.16f, -1.96f), new Vector2(1.00f * scaleJitter, frontScaleY * scaleJitter), frontGrassCol, 22, 1.8f, 1.6f, (carpetIdx % 2 == 0), slopeRotZ);
                }

                carpetIdx++;
            }

            // Intentional Compositional Rounded Bushes (anchored near trees, boulders & slopes)
            void SpawnAnchoredBush(string name, float bx, float bz, Sprite spr, float scale, Color col, float rotZ = 0f)
            {
                if (spr == null) return;
                float by = SampleTerrainSurfaceY(bx);
                SpawnSprite(name, bushesGroup.transform, spr,
                    new Vector3(bx, by - 0.06f, bz), new Vector2(scale, scale), col, 11, 1.4f, 1.2f, false, rotZ);
            }

            SpawnAnchoredBush("Opening_Bush_EmbankmentFoot",-6.4f, 1.60f, sprBushBroad1,                 0.55f, nearForestCol);
            SpawnAnchoredBush("Opening_Bush_MidLeftTree",   -3.6f, 1.55f, sprBushBroad1,                 0.48f, nearForestCol);
            SpawnAnchoredBush("Opening_Bush_CenterHero",    -1.1f, 1.60f, sprBushBroad1,                 0.45f, nearForestCol);
            SpawnAnchoredBush("Opening_Bush_DeadwoodStump",  3.8f, 1.55f, sprBushes[0],                  0.48f, nearForestCol);
            SpawnAnchoredBush("Opening_Bush_CliffBase",      5.2f, 1.60f, sprBushBroad2,                 0.54f, nearForestCol);
            SpawnAnchoredBush("Zone1_Bush_CliffPlateau",     6.6f, 1.55f, sprBushes[0],                  0.55f, nearForestCol);
            SpawnAnchoredBush("Zone1_Bush_RavineEdge",       19.0f, 1.55f, sprBushMound2 ?? sprBushes[2], 0.52f, nearForestCol);
            SpawnAnchoredBush("Zone1_Bush_CrawlEntrance",    27.2f, 1.55f, sprBushBroad1,                 0.50f, nearForestCol);
            SpawnAnchoredBush("Zone2_Bush_SlopeUpper",       46.5f, 1.55f, sprBushGold01,                 0.52f, nearForestCol, -11.5f);
            SpawnAnchoredBush("Zone2_Bush_SlopeMid",         53.0f, 1.55f, sprBushGold02,                 0.50f, nearForestCol, -11.5f);
            SpawnAnchoredBush("Zone2_Bush_SlopeLower",       58.5f, 1.55f, sprBushGold01,                 0.52f, nearForestCol, -11.5f);

            // Intentional Clustered Wildflowers (daisies, chamomile, yellow blossoms nestled by rocks & tree roots)
            void SpawnAnchoredFlower(string name, float fx, float fz, Sprite spr, float scale, float rotZ = 0f)
            {
                if (spr == null) return;
                float fy = SampleTerrainSurfaceY(fx);
                SpawnSprite(name, grassGroup.transform, spr,
                    new Vector3(fx, fy - 0.02f, fz), new Vector2(scale, scale), Color.white, 14, 2.2f, 1.8f, false, rotZ);
            }

            // Natural grass tufts & subtle blossoms (neutral wild vegetation matching 50_User_Reference_Forest_Vista.jpg, zero neon yellow flower clusters)
            SpawnAnchoredFlower("Opening_GrassTuft_Left",    -3.6f, 1.05f, sprFlowers[1], 0.55f);
            SpawnAnchoredFlower("Opening_GrassTuft_Center",  -1.1f, 1.05f, sprFlowers[0], 0.50f);
            SpawnAnchoredFlower("Opening_Flower_SmallSprig",  1.5f, 1.05f, sprFlowers[5], 0.45f);
            SpawnAnchoredFlower("Opening_Flower_CliffBloss",  5.6f, 1.10f, sprFlowers[2], 0.55f);
            SpawnAnchoredFlower("Zone1_Flower_Cliff",        12.0f, 1.10f, sprFlowers[3], 0.85f);
            SpawnAnchoredFlower("Zone1_Flower_Ravine",       20.2f, 1.10f, sprFlowers[4], 0.80f);
            SpawnAnchoredFlower("Zone2_Flower_Slope1",       48.0f, 1.10f, sprFlowers[4], 0.80f, -11.5f);
            SpawnAnchoredFlower("Zone2_Flower_Slope2",       54.5f, 1.10f, sprFlowers[3], 0.85f, -11.5f);

            // =====================================================================================
            // E. ZONE 3 (+62.0m to +95.0m) & ZONE 4 SURROUNDINGS (+95.0m to +134.0m):
            //    VAST GOLDEN RICE FIELD ("đi ngang một cánh đồng lúa và lúc đó platform sẽ phẳng lại và ngang hàng")
            // =====================================================================================
            int riceIdx = 0;
            Color goldFrontCol = new Color(0.88f, 0.74f, 0.28f, 1f);
            Color goldMidCol   = new Color(1.00f, 0.94f, 0.54f, 1f);
            Color goldBackCol  = new Color(0.95f, 0.85f, 0.42f, 0.96f);

            for (float rx = 61.5f; rx <= 133.5f; rx += 1.35f)
            {
                bool inMainRiceField = (rx <= 96.5f);

                // Layer 1: Front Verge Low Golden Rice (Z = -1.96f, ankle-to-shin height ~0.45m so An is 100% visible)
                if (inMainRiceField || riceIdx % 2 == 0)
                {
                    Sprite frontRice = (riceIdx % 2 == 0) ? sprRiceCarpet1 : sprRiceCarpet2;
                    SpawnSprite($"Rice_FrontLip_{riceIdx:D3}", riceGroup.transform, frontRice,
                        new Vector3(rx, -1.92f, -1.96f), new Vector2(0.38f, 0.35f), goldFrontCol, 22, 2.8f, 2.1f, (riceIdx % 2 == 0));
                }

                // Layer 2: Midground Golden Rice Paddy Carpet behind walking path (knee-to-waist height ~0.75m)
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

            // Distant Horizon Coconut Palms, Bamboo Groves & Golden Trees along the Rice Field (Z = 3.6m .. 4.4m, scaled to 0.48..0.58)
            SpawnSprite("RiceField_Horizon_Coconut1", treesGroup.transform, sprTree06Coconut, new Vector3(67.5f, -2.15f, 3.8f), new Vector2(0.52f, 0.52f), farForestCol, 5, 1.4f, 1.1f);
            SpawnSprite("RiceField_Horizon_Bamboo1",  treesGroup.transform, sprTree07Bamboo,  new Vector3(76.0f, -2.15f, 4.0f), new Vector2(0.55f, 0.55f), farForestCol, 5, 1.4f, 1.2f);
            SpawnSprite("RiceField_Horizon_GoldTree", treesGroup.transform, sprTreeGold01,    new Vector3(85.5f, -2.15f, 3.8f), new Vector2(0.56f, 0.56f), midForestCol, 6, 1.0f, 1.0f);
            SpawnSprite("RiceField_Horizon_Coconut2", treesGroup.transform, sprTree06Coconut, new Vector3(92.0f, -2.15f, 3.8f), new Vector2(0.54f, 0.54f), midForestCol, 6, 1.4f, 1.1f, true);
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

            // 1. Build Shared Humanoid AnimatorController for all Living 3D NPCs ("AC_Lana_LivingVillager.controller")
            RuntimeAnimatorController npcAnimController = BuildOrLoadVillagerAnimatorController();

            // 2. Spawn 3D Stilted Houses, 2.5D Gourd Trellises, Boulders & Village Props (X = 96m .. 132m)
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

                // Disable any building colliders that might jut into Z=0 walking path
                foreach (var col in inst.GetComponentsInChildren<Collider>(true))
                {
                    col.enabled = false;
                }

                // Measure combined world Renderer.bounds, scale to targetHeightMeters, and snap base onto groundPos!
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

                    // Re-measure bounds after scaling and snap center X/Z & min Y to groundPos
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

            // 5 Authentic 3D Vietnamese Stilted Houses & Thatched Homes arranged with depth (Z = 2.6m .. 3.4m, height = 4.2m .. 4.8m)
            SpawnNormalizedVillageStructure("Village_House01_EntranceThatched", pfHouseThatched1, new Vector3(100.8f, -1.85f, 2.8f), 175f, 4.45f);
            SpawnNormalizedVillageStructure("Village_House02_CentralWood",      pfHouseWood1,     new Vector3(109.2f, -1.85f, 3.1f), 184f, 4.65f);
            SpawnNormalizedVillageStructure("Village_House03_MaAnThatchedHome", pfHouseThatched1, new Vector3(117.8f, -1.85f, 2.9f), 178f, 4.55f);
            SpawnNormalizedVillageStructure("Village_House04_EastNhaSanGo",     pfNhaSanGo ?? pfHouseWood1, new Vector3(125.2f, -1.85f, 3.0f), 172f, 4.50f);
            SpawnNormalizedVillageStructure("Village_House05_ThatchedPavilion", pfWaitingShelter ?? pfThatchedHut, new Vector3(131.5f, -1.85f, 2.8f), 180f, 3.90f);

            // 2.5D Gourd Trellises (Dàn Bầu Bí) in the village yards
            if (pfGourdTrellis != null)
            {
                var tr1 = (GameObject)PrefabUtility.InstantiatePrefab(pfGourdTrellis, villageRoot.transform);
                tr1.name = "Village_GourdTrellis_West";
                tr1.transform.position = new Vector3(104.6f, -1.85f, 1.85f);

                var tr2 = (GameObject)PrefabUtility.InstantiatePrefab(pfGourdTrellis, villageRoot.transform);
                tr2.name = "Village_GourdTrellis_East";
                tr2.transform.position = new Vector3(121.4f, -1.85f, 1.85f);
            }

            // Authentic 3D Village Harvest Props (Rice Sacks, Bamboo Baskets, Salt Fish Barrels, 3D Boulders)
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

            // Village Golden Trees, Banana Palms, Coconut Palms & Bamboo Groves (scaled to 0.48..0.58 and placed at Z = 3.8..4.4m behind the houses!)
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

            SpawnVillageFoliage("Village_Tree_EntranceGold", sprTreeGold1, new Vector3( 97.2f, -2.15f, 3.8f), new Vector2(0.56f, 0.56f), 6);
            SpawnVillageFoliage("Village_Tree_Banana1",      sprBanana,    new Vector3(105.0f, -2.12f, 3.6f), new Vector2(0.50f, 0.50f), 7);
            SpawnVillageFoliage("Village_Tree_CenterGold",   sprTreeGold2, new Vector3(113.5f, -2.15f, 4.0f), new Vector2(0.58f, 0.58f), 6, true);
            SpawnVillageFoliage("Village_Tree_Coconut1",     sprCoconut,   new Vector3(121.5f, -2.15f, 3.9f), new Vector2(0.54f, 0.54f), 6);
            SpawnVillageFoliage("Village_Tree_BambooEast",   sprBamboo,    new Vector3(128.5f, -2.15f, 3.8f), new Vector2(0.54f, 0.54f), 6);
            SpawnVillageFoliage("Village_Tree_EastCanopy",   sprTreeGold1, new Vector3(134.0f, -2.15f, 3.8f), new Vector2(0.58f, 0.58f), 6);

            for (float bx = 98.0f; bx <= 131.0f; bx += 5.5f)
            {
                SpawnVillageFoliage($"Village_Bush_{bx:F0}", sprBushG1, new Vector3(bx, -1.92f, 1.65f), new Vector2(0.78f, 0.78f), 11);
            }

            // 3. Spawn Rigged & Animated 3D NPCs in the Forest Clearing (2 companions playing with An) & in the Village (7 villagers)
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

                // Normalize scale so NPC matches An's world scale (~1.35m for children, ~1.65m for adults)
                var rend = inst.GetComponentInChildren<Renderer>();
                if (rend != null && rend.bounds.size.y > 0.1f)
                {
                    float currentH = rend.bounds.size.y;
                    float s = targetHeightMeters / currentH;
                    inst.transform.localScale = Vector3.one * s;
                }

                // Assign URP Lit material with verified BaseColor texture so NPC never renders untextured/grey
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
                    // Evaluate initial pose in Edit Mode so no NPC ever appears in a T-pose in the Editor or screenshots!
                    anim.Play(defaultState, 0, 0.25f);
                    anim.Update(0.05f);
                }

                // Re-snap feet to pos.y after Animator pose evaluation
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

            // --- A. Village Children playing in the Golden Rice Field Village (X = 101.5m & X = 111.0m) ---
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

            // --- B. 7 Living Villager NPCs across the Golden Rice Field Village (X = 98m .. 129m) ---
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

            // 4. Create Wide-Zoom Village Arrival Cutscene Trigger & Virtual Camera at X = 94.0m
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
        // 8. LIGHTING, CAMERAS (SKY-TO-FOREST DESCENT) & CUTSCENE CONFIGURATION
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

            // Configure VCam_IntroSky high in the deep cosmic zenith sky (X = -3.2m, Y = +36.5m, Z = -13.5m)
            // so Phase 3 pans down from galaxy edge deep blue sky -> clouds -> lower sky -> foreground bushes -> An playing in the forest!
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

            // Configure Gameplay Virtual Camera & Main Camera at An's starting clearing
            // Camera centered at X = -0.20m, Y = +0.35m, Z = -13.5m matching 50_User_Reference_Forest_Vista.jpg 1:1
            var vcam = GameObject.Find("01_Camera/CM_VirtualCamera_2.5D") ?? GameObject.Find("CM_VirtualCamera_2.5D");
            if (vcam != null)
            {
                vcam.transform.position = new Vector3(-0.20f, 0.35f, -13.5f);
                vcam.transform.rotation = Quaternion.identity;
                var cmCam = vcam.GetComponent<CinemachineCamera>();
                if (cmCam != null)
                {
                    if (playerGO != null) cmCam.Follow = playerGO.transform;
                    cmCam.LookAt = null; // Pure 2.5D Planet of Lana horizontal tracking — zero camera tilt!
                    var lens = cmCam.Lens;
                    lens.FieldOfView = 38.0f;
                    cmCam.Lens = lens;
                }
                var posComposer = vcam.GetComponent<CinemachinePositionComposer>();
                if (posComposer != null)
                {
                    posComposer.CameraDistance = 13.5f;
                    // Player at X = -2.2f, Y = -1.82f. Camera desired at X = -0.20f, Y = 0.35f -> Offset: (2.0f, 2.17f, 0f)
                    posComposer.TargetOffset = new Vector3(2.0f, 2.17f, 0.0f);
                    var comp = posComposer.Composition;
                    comp.ScreenPosition = Vector2.zero;
                    posComposer.Composition = comp;
                    EditorUtility.SetDirty(posComposer);
                }
                // Remove any rotation composer that would tilt the camera
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

            // Destroy any rogue embedded FBX cameras or lights (e.g. Blender Camera inside SM_WoodenCrate.fbx)
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

            // Ensure PauseMenuController has official logo assigned
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

            // Ensure default save slot 1 starts at the Forest Clearing (-2.2, -1.82, 0) with checkpointIndex = 0
            HoaVaMauSaveSystem.SaveCurrentGameState(1, "Chương 1: Đất Đỏ Cà Mau", "Rừng Sớm Cà Mau", 0, new Vector3(-2.2f, -1.82f, 0f));
            IntroCutsceneState.MarkForNewGame();

            // Ensure IntroCutsceneController has its upgraded 35mm Vintage Film Canvas & 6.0s Sky-to-Forest descent configured with verified historical slide sprites
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
            rockMR.shadowCastingMode = ShadowCastingMode.On;
            rockMR.receiveShadows = true;

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

            // 1. Sky-to-Forest Cloud Descent View (Descending through voluminous billowy cumulus clouds)
            capture("Verify_01_SkyToCloudsDescent.png", new Vector3(-3.2f, 22.5f, -13.5f), Vector3.zero, 38f);

            // 2. Forest Clearing Start (Recreated 1:1 to exact user reference painting 50_User_Reference_Forest_Vista.jpg)
            capture("Verify_02_ForestClearingAndRope.png", new Vector3(-0.20f, 0.35f, -13.5f), Vector3.zero, 38f);

            // 3. Ravine Gap, Fallen Tree Crawl & Push/Pull Crate to High Platform
            capture("Verify_03_FallenTreeAndCratePuzzle.png", new Vector3(28.0f, 0.85f, -14.5f), Vector3.zero, 38f);

            // 4. High Cliff Overlook & Downhill Slope ("thấy được đồi ở dưới, thấy được làng ở dưới thì đi xuống dốc đồi")
            capture("Verify_04_HillOverlookAndDescent.png", new Vector3(45.0f, 2.25f, -16.0f), Vector3.zero, 38f);

            // 5. Flat Horizontal Golden Rice Field ("đi ngang một cánh đồng lúa và lúc đó platform sẽ phẳng lại và ngang hàng")
            capture("Verify_05_FlatGoldenRiceField.png", new Vector3(78.0f, 0.15f, -13.5f), Vector3.zero, 38f);

            // 6. Wide-Zoom Village Arrival Cutscene & Living Animated NPC Village surrounded by Golden Rice Fields
            capture("Verify_06_LivingVillageWideZoom.png", new Vector3(108.0f, 1.85f, -21.5f), new Vector3(4f, 0f, 0f), 42f);

            cam.transform.position = origPos;
            cam.transform.rotation = origRot;
            cam.fieldOfView = origFov;
        }
    }
}
#endif
