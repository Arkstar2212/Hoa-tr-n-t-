#pragma warning disable CS0618
#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using HoaVaMau.Gameplay;

namespace HoaVaMau.Editor
{
    [InitializeOnLoad]
    public static class BuildPlanetOfLanaStartMenu
    {
        private const string OverhaulScenePath = "Assets/Scenes/New Scene 1_Overhaul.unity";
        private const string MainMenuScenePath = "Assets/Scenes/Scene_MainMenu.unity";
        private const string GenAssetsDir = "Assets/Materials/LanaOverhaul";
        private const string AutoBuildSessionKey = "HoaVaMau_StartMenu_Built_v2";

        static BuildPlanetOfLanaStartMenu()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
                if (!SessionState.GetBool(AutoBuildSessionKey, false))
                {
                    SessionState.SetBool(AutoBuildSessionKey, true);
                    var activeScene = EditorSceneManager.GetActiveScene();
                    if (activeScene.path == MainMenuScenePath && GameObject.Find("00_UI_PlanetOfLana_StartMenu") == null)
                    {
                        ExecuteBuildStartMenu();
                    }
                }
            };
        }

        [MenuItem("Hoa va Mau/Start Menu/Build Planet of Lana Start Menu & Loading System")]
        public static void ExecuteBuildStartMenu()
        {
            ExecuteBuildStartMenu(false);
        }

        [MenuItem("Hoa va Mau/Start Menu/Capture Start Menu Screenshots")]
        public static void UpdateStartMenuAndCaptureScreenshots()
        {
            Sprite studioLogoSpr   = ImportAndLoadUISprite("Assets/Textures/UI/T_Logo_FiveCowsInTheFire_BW.png");
            Sprite officialLogoSpr = ImportAndLoadUISprite("Assets/Textures/UI/T_Logo_HoaTrenDatDo.png");
            Sprite flowerSpr       = ImportAndLoadUISprite("Assets/Textures/UI/T_Flower_Minimalist_BW.png");
            Sprite doveSpr         = ImportAndLoadUISprite("Assets/Textures/UI/T_Dove_Minimalist_BW.png");
            Sprite softDotSpr      = ImportAndLoadUISprite("Assets/Textures/UI/T_Loading_SoftDot.png");
            Sprite boySpr          = ImportAndLoadUISprite("Assets/Textures/PierScene/running_boy_sprite.png");
            Sprite shipSpr         = ImportAndLoadUISprite("Assets/Textures/Canh1_Sprites/TransportShip_1954_Card.png");

            AudioClip bgmClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Breath of the World.mp3");
                        AudioClip windClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/soundreality-wind-blowing-457954.mp3");

            // Decorate scene with the new Lana trees if in overhaul scene
            if (GameObject.Find("05_Midground") != null)
            {
                DecorateLanaTreesInScene();
            }

            var sys = Object.FindAnyObjectByType<PlanetOfLanaStartMenuSystem>();
            if (sys == null)
            {
                sys = SetupStartMenuSystemInScene(studioLogoSpr, officialLogoSpr, flowerSpr, doveSpr, softDotSpr, boySpr, shipSpr, bgmClip, windClip, "New Scene 1_Overhaul");
            }
            else
            {
                Texture2D[] fallbackThumbs = new Texture2D[] {
                    AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Backgrounds_Lana/BG_Lana_MainSkyHills_Official.png"),
                    AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Backgrounds_Lana/BG_Lana_CoastalBay_Day.jpg"),
                    AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Backgrounds_Lana/BG_Lana_HillsFields_Sunset.jpg")
                };
                sys.SetFallbackSlotThumbnails(fallbackThumbs);
                sys.ConfigureAssets(studioLogoSpr, officialLogoSpr, flowerSpr, doveSpr, softDotSpr, boySpr, shipSpr, bgmClip, windClip, "New Scene 1_Overhaul");
                sys.RebuildUIImmediate();
            }

            AudioClip track1 = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Phat_Hien_Binh_Yen.mp3");
            AudioClip track2 = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Pastoral_Alien.mp3");
            AudioClip track3 = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Breath of the World.mp3");
                        var musicList = new List<AudioClip>();
            if (track1 != null) musicList.Add(track1);
            if (track2 != null) musicList.Add(track2);
            if (track3 != null) musicList.Add(track3);
                        sys.SetRandomMenuMusicClips(musicList.ToArray());

            CaptureStartMenuVerificationScreenshots(sys);
            sys.SetPreviewState(PlanetOfLanaStartMenuSystem.MenuPhase.StartMenuOpenVista);
            Debug.Log("<color=#10b981>[StartMenu]</color> Captured fresh screenshots!");
        }

        [MenuItem("Hoa va Mau/Start Menu/Build Living Ocean Cà Mau Start Menu")]
        public static void BuildLivingOceanMainMenuScene()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != MainMenuScenePath)
            {
                if (File.Exists(MainMenuScenePath))
                {
                    scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
                }
                else
                {
                    scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                }
            }

            // Remove any old 3D level objects (Minh, An, terrain, old lights, Cinemachine virtual cameras, pause UI, etc.)
            string[] objectsToRemove = {
                "02_Gameplay", "03_Terrain", "04_Foreground", "05_Midground",
                "06_Water", "07_Background", "08_Lighting", "09_VFX", "10_EnvironmentSystems",
                "PF_Player_Minh", "Character_An", "CinematicVirtualCameras", "Directional Light",
                "01_Camera/CM_VirtualCamera_2.5D", "CM_VirtualCamera_2.5D", "VCam_IntroSky",
                "00_UI_PauseMenuController", "PlanetOfLana_Prompt_System"
            };
            foreach (var name in objectsToRemove)
            {
                var go = GameObject.Find(name);
                if (go != null) Object.DestroyImmediate(go);
            }

            // Load UI Sprites and Audio
            Sprite studioLogoSpr   = ImportAndLoadUISprite("Assets/Textures/UI/T_Logo_FiveCowsInTheFire_BW.png");
            Sprite headphoneSpr    = ImportAndLoadUISprite("Assets/Textures/UI/T_Icon_Headphone_BW.png");
            Sprite officialLogoSpr = ImportAndLoadUISprite("Assets/Textures/UI/T_Logo_HoaTrenDatDo.png");
            Sprite flowerSpr       = ImportAndLoadUISprite("Assets/Textures/UI/T_Flower_Minimalist_BW.png");
            Sprite doveSpr         = ImportAndLoadUISprite("Assets/Textures/UI/T_Dove_Minimalist_BW.png");
            Sprite softDotSpr      = ImportAndLoadUISprite("Assets/Textures/UI/T_Loading_SoftDot.png");
            Sprite boySpr          = ImportAndLoadUISprite("Assets/Textures/PierScene/running_boy_sprite.png");
            Sprite shipSpr         = ImportAndLoadUISprite("Assets/Textures/Canh1_Sprites/TransportShip_1954_Card.png");

            AudioClip bgmClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Breath of the World.mp3");
                        AudioClip windClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/soundreality-wind-blowing-457954.mp3");

            // Load Living Ocean Textures
            Sprite boatHeroSpr  = ImportAndLoadOceanSprite("Assets/Textures/Environment/T_Camau_Boat_Hero.png", 215.0f);
            Sprite boatLeftSpr  = ImportAndLoadOceanSprite("Assets/Textures/Environment/T_Camau_Boat_Left.png", 215.0f);
            Sprite sunbeamsSpr  = ImportAndLoadOceanSprite("Assets/Textures/Environment/T_Camau_Sunbeams_Volumetric.png", 100.0f);
            Sprite waterShimSpr = ImportAndLoadOceanSprite("Assets/Textures/Environment/T_Camau_Water_Shimmer.png", 100.0f, TextureWrapMode.Repeat);
            Sprite waterWakeSpr = ImportAndLoadOceanSprite("Assets/Textures/Environment/T_Camau_Boat_WaterWake.png", 100.0f);

            // Setup Cinematic Camera (Remove any leftover CinemachineBrain so nothing overrides the Start Menu camera!)
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                var camGO = GameObject.Find("01_Camera/CinematicMainCamera") ?? GameObject.Find("01_Camera/Main Camera") ?? GameObject.Find("Main Camera");
                if (camGO == null) camGO = new GameObject("CinematicMainCamera");
                mainCam = camGO.GetComponent<Camera>() ?? camGO.AddComponent<Camera>();
                camGO.tag = "MainCamera";
                if (camGO.GetComponent<AudioListener>() == null) camGO.AddComponent<AudioListener>();
            }
            var leftoverBrain = mainCam.GetComponent("CinemachineBrain");
            if (leftoverBrain != null) Object.DestroyImmediate(leftoverBrain);
            for (int c = mainCam.transform.childCount - 1; c >= 0; c--)
            {
                Object.DestroyImmediate(mainCam.transform.GetChild(c).gameObject);
            }
            mainCam.transform.position = new Vector3(0f, 0f, -10.0f);
            mainCam.transform.rotation = Quaternion.identity;
            mainCam.fieldOfView = 35.5f;
            mainCam.depth = 10f;
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.20f, 0.48f, 0.68f, 1.0f);

            // Setup 01_LivingOcean_CaMau_Master
            var existingOcean = GameObject.Find("01_LivingOcean_CaMau_Master");
            if (existingOcean != null) Object.DestroyImmediate(existingOcean);

            var oceanMaster = new GameObject("01_LivingOcean_CaMau_Master");
            oceanMaster.transform.position = Vector3.zero;

            // Background Quad (16.2m x 6.85m — preserves exact aspect ratio while providing generous bleed for camera drift)
            var bgQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bgQuad.name = "Background_Ocean_Clean";
            bgQuad.transform.SetParent(oceanMaster.transform, false);
            bgQuad.transform.localPosition = new Vector3(0f, 0f, 0f);
            bgQuad.transform.localScale = new Vector3(16.2f, 6.85f, 1.0f);
            Object.DestroyImmediate(bgQuad.GetComponent<Collider>());

            string bgMatPath = $"{GenAssetsDir}/M_Camau_Ocean_Background_Clean.mat";
            var bgMat = AssetDatabase.LoadAssetAtPath<Material>(bgMatPath);
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture");
            if (bgMat == null)
            {
                bgMat = new Material(unlitShader);
                AssetDatabase.CreateAsset(bgMat, bgMatPath);
            }
            bgMat.shader = unlitShader;
            var bgTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/References/ocean-camau.jpg");
            if (bgMat.HasProperty("_BaseMap")) bgMat.SetTexture("_BaseMap", bgTex);
            else bgMat.mainTexture = bgTex;
            bgQuad.GetComponent<MeshRenderer>().sharedMaterial = bgMat;

            // 2. Procedural Volumetric Sunbeams Streaming from Clouds
            var sunbeamsGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
            sunbeamsGO.name = "Living_Sunbeams";
            sunbeamsGO.transform.SetParent(oceanMaster.transform, false);
            sunbeamsGO.transform.localPosition = new Vector3(0.6f, 1.35f, -0.02f);
            sunbeamsGO.transform.localRotation = Quaternion.Euler(0f, 0f, -14f);
            sunbeamsGO.transform.localScale = new Vector3(10.5f, 5.2f, 1.0f);
            Object.DestroyImmediate(sunbeamsGO.GetComponent<Collider>());

            string sunbeamsMatPath = $"{GenAssetsDir}/M_LivingOcean_Sunbeams.mat";
            var sunbeamsMat = AssetDatabase.LoadAssetAtPath<Material>(sunbeamsMatPath);
            Shader godRaysShader = Shader.Find("HoaVaMau/StylizedGodRays");
            if (sunbeamsMat == null && godRaysShader != null)
            {
                sunbeamsMat = new Material(godRaysShader);
                AssetDatabase.CreateAsset(sunbeamsMat, sunbeamsMatPath);
            }
            if (sunbeamsMat != null && godRaysShader != null)
            {
                sunbeamsMat.shader = godRaysShader;
                sunbeamsMat.SetColor("_RayColor", new Color(1.0f, 0.96f, 0.86f, 0.28f));
                sunbeamsMat.SetFloat("_RayIntensity", 1.20f);
                sunbeamsMat.SetFloat("_ShimmerSpeed", 0.42f);
                sunbeamsMat.SetFloat("_FalloffPower", 1.8f);
                sunbeamsMat.SetFloat("_BeamCount", 4.5f);
                EditorUtility.SetDirty(sunbeamsMat);
            }
            if (sunbeamsMat != null) sunbeamsGO.GetComponent<MeshRenderer>().sharedMaterial = sunbeamsMat;

            // 3. Procedural Shimmering Sunlight Water Surface (Pure additive, zero artifacts)
            var shimmerGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
            shimmerGO.name = "Water_Living_Shimmer";
            shimmerGO.transform.SetParent(oceanMaster.transform, false);
            shimmerGO.transform.localPosition = new Vector3(0.0f, -2.15f, -0.03f);
            shimmerGO.transform.localScale = new Vector3(15.2f, 1.7f, 1.0f);
            Object.DestroyImmediate(shimmerGO.GetComponent<Collider>());

            string shimmerMatPath = $"{GenAssetsDir}/M_LivingOcean_WaterSurface.mat";
            var shimmerMat = AssetDatabase.LoadAssetAtPath<Material>(shimmerMatPath);
            Shader waterShader = Shader.Find("HoaVaMau/LivingOceanWaterSurface");
            if (shimmerMat == null && waterShader != null)
            {
                shimmerMat = new Material(waterShader);
                AssetDatabase.CreateAsset(shimmerMat, shimmerMatPath);
            }
            if (shimmerMat != null && waterShader != null)
            {
                shimmerMat.shader = waterShader;
                shimmerMat.SetColor("_GlintColor", new Color(0.75f, 0.95f, 1.0f, 0.32f));
                shimmerMat.SetFloat("_GlintScale", 18.0f);
                shimmerMat.SetFloat("_GlintSpeed", 0.45f);
                shimmerMat.SetFloat("_SparkleSharpness", 6.0f);
                shimmerMat.SetFloat("_Intensity", 0.85f);
                EditorUtility.SetDirty(shimmerMat);
            }
            if (shimmerMat != null) shimmerGO.GetComponent<MeshRenderer>().sharedMaterial = shimmerMat;

            // Golden Sun Motes
            var dustPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/VFX/PF_Atmospheric_GoldenDust.prefab");
            if (dustPrefab != null)
            {
                var dust = (GameObject)PrefabUtility.InstantiatePrefab(dustPrefab, oceanMaster.transform);
                dust.name = "LivingOcean_GoldenSunMotes";
                dust.transform.localPosition = new Vector3(0.5f, -0.2f, -1.8f);
            }

            // 4. Unified Planet of Lana Borderless Framing Foliage & Drifting Sky Clouds on Start Menu
            DecorateStartMenuVistaWithLanaFoliage(oceanMaster.transform);

            // Setup Start Menu System UI with Verified Random DJ Mix Playlist
            AudioClip track1 = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Phat_Hien_Binh_Yen.mp3");
            AudioClip track2 = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Pastoral_Alien.mp3");
            AudioClip track3 = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Breath of the World.mp3");
                        var menuPlaylist = new List<AudioClip>();
            if (track1 != null) menuPlaylist.Add(track1);
            if (track2 != null) menuPlaylist.Add(track2);
            if (track3 != null) menuPlaylist.Add(track3);
            
            var menuGO = GameObject.Find("00_UI_PlanetOfLana_StartMenu");
            if (menuGO != null) Object.DestroyImmediate(menuGO);
            menuGO = new GameObject("00_UI_PlanetOfLana_StartMenu");

            var sys = menuGO.AddComponent<PlanetOfLanaStartMenuSystem>();
            sys.ConfigureAssets(studioLogoSpr, officialLogoSpr, flowerSpr, doveSpr, softDotSpr, boySpr, shipSpr, bgmClip, windClip, "New Scene 1_Overhaul", headphoneSpr, menuPlaylist.ToArray());
            sys.SetRandomMenuMusicClips(menuPlaylist.ToArray());
            sys.SetMenuCameraSettings(new Vector3(0f, 0f, -10.0f), Vector3.zero, 35.5f);
            sys.RebuildUIImmediate();

            // Capture Verification Screenshots first, then rebuild clean ScreenSpaceOverlay UI & save scene!
            CaptureStartMenuVerificationScreenshots(sys);

            sys.RebuildUIImmediate();
            sys.SetPreviewState(PlanetOfLanaStartMenuSystem.MenuPhase.StartMenuOpenVista, 1.0f);

            // Save Scene AFTER screenshots so Canvas is saved cleanly in ScreenSpaceOverlay mode with scale (1,1,1)
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, MainMenuScenePath);
            RegisterScenesInBuildSettings();

            Debug.Log("<color=#10b981>[LivingOcean StartMenu]</color> <b>LIVING OCEAN CÀ MAU SCENE BUILT & VERIFIED SUCCESSFULLY!</b>");
        }

        [MenuItem("Hoa va Mau/Setup/Import TMP Essential Resources")]
        public static void ImportTMPEssentialResourcesIfMissing()
        {
            string pkgPath = "Library/PackageCache/com.unity.ugui@23caec89ae27/Package Resources/TMP Essential Resources.unitypackage";
            if (File.Exists(pkgPath))
            {
                AssetDatabase.ImportPackage(pkgPath, false);
                Debug.Log("<color=#10b981>[TMP]</color> Imported TMP Essential Resources automatically!");
            }
        }

        [MenuItem("Hoa va Mau/Setup/Setup Pause Menu In Gameplay Scene")]
        public static void SetupPauseMenuInGameplayScene()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != OverhaulScenePath)
            {
                scene = EditorSceneManager.OpenScene(OverhaulScenePath, OpenSceneMode.Single);
            }

            // Remove any leftover start menu in gameplay scene
            var oldMenu = GameObject.Find("00_UI_PlanetOfLana_StartMenu");
            if (oldMenu != null) Object.DestroyImmediate(oldMenu);

            var existingPause = GameObject.Find("00_UI_PauseMenuController");
            if (existingPause != null) Object.DestroyImmediate(existingPause);

            var pauseGO = new GameObject("00_UI_PauseMenuController");
            pauseGO.transform.SetAsFirstSibling();
            var ctrl = pauseGO.AddComponent<PauseMenuController>();

            EditorUtility.SetDirty(pauseGO);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, OverhaulScenePath);
            Debug.Log("<color=#10b981>[PauseMenu]</color> Pause Menu Controller configured in gameplay scene!");
        }

        [MenuItem("Hoa va Mau/Start Menu/Capture Pause Menu Screenshot")]
        public static void CapturePauseMenuScreenshot()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != OverhaulScenePath)
            {
                scene = EditorSceneManager.OpenScene(OverhaulScenePath, OpenSceneMode.Single);
            }

            var pauseCtrl = Object.FindAnyObjectByType<PauseMenuController>();
            if (pauseCtrl == null)
            {
                SetupPauseMenuInGameplayScene();
                pauseCtrl = Object.FindAnyObjectByType<PauseMenuController>();
            }

            if (pauseCtrl != null)
            {
                pauseCtrl.EnsureUI();
                pauseCtrl.PauseGame();
                var cam = Camera.main;
                if (cam != null)
                {
                    var canvas = pauseCtrl.GetComponentInChildren<Canvas>(true);
                    var prevMode = canvas.renderMode;
                    var prevMask = cam.cullingMask;

                    cam.cullingMask = -1; // Ensure all layers render
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = cam;
                    canvas.planeDistance = 0.5f;
                    Canvas.ForceUpdateCanvases();

                    int width = 1280;
                    int height = 720;
                    var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
                    var prevRt = cam.targetTexture;
                    cam.targetTexture = rt;
                    cam.Render();

                    RenderTexture.active = rt;
                    var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                    tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    tex.Apply();

                    cam.targetTexture = prevRt;
                    cam.cullingMask = prevMask;
                    RenderTexture.active = null;
                    RenderTexture.ReleaseTemporary(rt);

                    string outDir = Path.Combine(Application.dataPath, "../Screenshots/Showcase_Presentation");
                    File.WriteAllBytes(Path.Combine(outDir, "StartMenu_08_PauseMenu_Gameplay.png"), tex.EncodeToPNG());
                    Object.DestroyImmediate(tex);

                    canvas.renderMode = prevMode;
                    pauseCtrl.ResumeGame();
                    Debug.Log("<color=#38bdf8>[PauseMenu Capture]</color> Captured StartMenu_08_PauseMenu_Gameplay.png");
                }
            }
        }

        public static void ExecuteBuildStartMenu(bool runFullLevelOverhaul)
        {
            // 1. Run base level overhaul if explicitly requested
            if (runFullLevelOverhaul)
            {
                OverhaulScene1Visuals.ExecuteOverhaul();
            }

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != OverhaulScenePath)
            {
                scene = EditorSceneManager.OpenScene(OverhaulScenePath, OpenSceneMode.Single);
            }

            // 2. Ensure UI Sprites (Five Cows Logo, Official Calligraphy Logo, Flower Emblem, Soft Dot, Boy, Ship) are imported
            Sprite studioLogoSpr   = ImportAndLoadUISprite("Assets/Textures/UI/T_Logo_FiveCowsInTheFire_BW.png");
            Sprite officialLogoSpr = ImportAndLoadUISprite("Assets/Textures/UI/T_Logo_HoaTrenDatDo.png");
            Sprite flowerSpr       = ImportAndLoadUISprite("Assets/Textures/UI/T_Flower_Minimalist_BW.png");
            Sprite doveSpr         = ImportAndLoadUISprite("Assets/Textures/UI/T_Dove_Minimalist_BW.png");
            Sprite softDotSpr      = ImportAndLoadUISprite("Assets/Textures/UI/T_Loading_SoftDot.png");
            Sprite boySpr          = ImportAndLoadUISprite("Assets/Textures/PierScene/running_boy_sprite.png");
            Sprite shipSpr         = ImportAndLoadUISprite("Assets/Textures/Canh1_Sprites/TransportShip_1954_Card.png");

            // 3. Load Looping Music ("Breath of the World.mp3") and Wind Audio
            AudioClip bgmClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Breath of the World.mp3");
                        AudioClip windClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/soundreality-wind-blowing-457954.mp3");

            // 4. Enhance Vista Sky with Cloud Radiance & Crepuscular Sunbeams
            SetupCloudRadianceAndSunbeams();

            // 5. Add Shadow-Casting Trees & Conformed Dappled Canopy Ground Shadows
            SetupShadowCastingTreesAndDappledGroundShadows();

            // 6. Decorate with the new Lana trees (Wide Canopy, Banana Palm, Bamboo, Flowers)
            DecorateLanaTreesInScene();

            // 7. Ensure Overhaul Gameplay Scene does NOT contain Start Menu
            var menuInGameplay = GameObject.Find("00_UI_PlanetOfLana_StartMenu");
            if (menuInGameplay != null)
            {
                Object.DestroyImmediate(menuInGameplay);
                Debug.Log("<color=#38bdf8>[StartMenu]</color> Cleaned up StartMenu from gameplay scene.");
            }

            // Save the Gameplay Scene (New Scene 1_Overhaul.unity) with An and gameplay triggers active
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, OverhaulScenePath);

            // 8. Open Dedicated Standalone Start Menu Scene (Scene_MainMenu.unity)
            var menuScene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);

            // REMOVE player and gameplay hazards from Scene_MainMenu so it is a pure, tranquil nature vista!
            var playerGO = GameObject.Find("PF_Player_Minh") ?? GameObject.Find("Character_An");
            if (playerGO != null)
            {
                Object.DestroyImmediate(playerGO);
                Debug.Log("<color=#38bdf8>[StartMenu Scene]</color> Removed player character from dedicated Start Menu scene.");
            }

            var menuSystem = SetupStartMenuSystemInScene(studioLogoSpr, officialLogoSpr, flowerSpr, doveSpr, softDotSpr, boySpr, shipSpr, bgmClip, windClip, targetScene: "New Scene 1_Overhaul");

            // Capture Verification Screenshots from the pure scenic vista without player
            CaptureStartMenuVerificationScreenshots(menuSystem);

            // Restore preview state to Open Vista
            if (menuSystem != null)
            {
                menuSystem.SetPreviewState(PlanetOfLanaStartMenuSystem.MenuPhase.StartMenuOpenVista);
            }

            // Save as dedicated Scene_MainMenu.unity
            EditorSceneManager.MarkSceneDirty(menuScene);
            EditorSceneManager.SaveScene(menuScene, MainMenuScenePath);

            // Re-open OverhaulScenePath as the primary active scene in Editor
            EditorSceneManager.OpenScene(OverhaulScenePath, OpenSceneMode.Single);
            RegisterScenesInBuildSettings();

            Debug.Log("<color=#10b981>[StartMenu]</color> <b>PLANET OF LANA DEDICATED START MENU SCENE, FLOWER LOADER, BREATH OF THE WORLD BGM, & AUTOSAVE COMPLETED!</b>");
        }

        private static Sprite ImportAndLoadUISprite(string assetPath)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (existing != null) return existing;

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                bool changed = false;
                if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; changed = true; }
                if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; changed = true; }
                if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; changed = true; }
                if (importer.mipmapEnabled) { importer.mipmapEnabled = false; changed = true; }
                if (settings.spriteMeshType != SpriteMeshType.FullRect) { settings.spriteMeshType = SpriteMeshType.FullRect; changed = true; }
                if (changed)
                {
                    importer.SetTextureSettings(settings);
                    importer.SaveAndReimport();
                }
            }

            var spr = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (spr != null) return spr;

            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(assetPath))
            {
                if (obj is Sprite s) return s;
            }
            return null;
        }

        private static Sprite ImportAndLoadFoliageSprite(string assetPath, Vector2 pivot)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (existing != null) return existing;

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                bool changed = false;
                if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; changed = true; }
                if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; changed = true; }
                if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; changed = true; }
                if (importer.mipmapEnabled) { importer.mipmapEnabled = false; changed = true; }
                if (settings.spriteMeshType != SpriteMeshType.FullRect) { settings.spriteMeshType = SpriteMeshType.FullRect; changed = true; }
                if (settings.spriteAlignment != (int)SpriteAlignment.Custom || Vector2.Distance(settings.spritePivot, pivot) > 0.001f)
                {
                    settings.spriteAlignment = (int)SpriteAlignment.Custom;
                    settings.spritePivot = pivot;
                    changed = true;
                }
                if (changed)
                {
                    importer.SetTextureSettings(settings);
                    importer.SaveAndReimport();
                }
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static Sprite ImportAndLoadOceanSprite(string assetPath, float ppu = 215.0f, TextureWrapMode wrapMode = TextureWrapMode.Clamp)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                bool changed = false;
                if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; changed = true; }
                if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; changed = true; }
                if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; changed = true; }
                if (importer.mipmapEnabled) { importer.mipmapEnabled = false; changed = true; }
                if (Mathf.Abs(importer.spritePixelsPerUnit - ppu) > 0.1f) { importer.spritePixelsPerUnit = ppu; changed = true; }
                if (importer.wrapMode != wrapMode) { importer.wrapMode = wrapMode; changed = true; }
                if (changed)
                {
                    importer.SetTextureSettings(settings);
                    importer.SaveAndReimport();
                }
            }

            var spr = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (spr != null) return spr;

            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(assetPath))
            {
                if (obj is Sprite s) return s;
            }
            return null;
        }

        // =========================================================================================
        // DECORATE START MENU VISTA WITH BORDERLESS PLANET OF LANA FOLIAGE & CLOUDS
        // =========================================================================================
        private static void DecorateStartMenuVistaWithLanaFoliage(Transform parent)
        {
            var existing = parent.Find("02_StartMenu_LanaFramingFoliage");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            var root = new GameObject("02_StartMenu_LanaFramingFoliage");
            root.transform.SetParent(parent, false);

            string spriteMatDir = $"{GenAssetsDir}/SpriteMats";
            if (!Directory.Exists(spriteMatDir)) Directory.CreateDirectory(spriteMatDir);
            Shader urpSpriteShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");

            Material GetMenuSpriteMat(Sprite s)
            {
                if (s == null || s.texture == null) return AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
                string mPath = $"{spriteMatDir}/M_Spr_{s.texture.name}.mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(mPath);
                if (m == null)
                {
                    m = new Material(urpSpriteShader);
                    AssetDatabase.CreateAsset(m, mPath);
                }
                else if (urpSpriteShader != null) m.shader = urpSpriteShader;
                if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", s.texture);
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", s.texture);
                m.mainTexture = s.texture;
                EditorUtility.SetDirty(m);
                return m;
            }

            Sprite sprCloud1  = ImportAndLoadFoliageSprite("Assets/Textures/Lana_Unified_Art/Clouds/T_Lana_Cloud_01_CumulusHero.png", new Vector2(0.5f, 0.5f));
            Sprite sprCloud3  = ImportAndLoadFoliageSprite("Assets/Textures/Lana_Unified_Art/Clouds/T_Lana_Cloud_03_HighWisps.png", new Vector2(0.5f, 0.5f));
            Sprite sprCoconut = ImportAndLoadFoliageSprite("Assets/Textures/Environment/Lana_VN_Pack/Trees/T_LanaVN_Tree_06_Coconut.png", new Vector2(0.45f, 0.02f));
            Sprite sprBush1   = ImportAndLoadFoliageSprite("Assets/Textures/Environment/Lana_VN_Pack/Bushes/T_LanaVN_Bush_Low_01.png", new Vector2(0.5f, 0.04f));
            Sprite sprBush2   = ImportAndLoadFoliageSprite("Assets/Textures/Environment/Lana_VN_Pack/Bushes/T_LanaVN_Bush_Low_02_Flower.png", new Vector2(0.5f, 0.04f));
            Sprite sprGrass1  = ImportAndLoadFoliageSprite("Assets/Textures/Environment/Lana_VN_Pack/Grass_Vines/T_LanaVN_GrassCarpet_03.png", new Vector2(0.5f, 0.05f));
            Sprite sprGrass2  = ImportAndLoadFoliageSprite("Assets/Textures/Environment/Lana_VN_Pack/Grass_Vines/T_LanaVN_GrassCarpet_05_Dense.png", new Vector2(0.5f, 0.05f));
            Sprite sprReeds   = ImportAndLoadFoliageSprite("Assets/Textures/Environment/Lana_VN_Pack/Wildflowers/T_LanaVN_Wildflower_01_Reeds.png", new Vector2(0.5f, 0.04f));
            Sprite sprFlYellow= ImportAndLoadFoliageSprite("Assets/Textures/Environment/Lana_VN_Pack/Wildflowers/T_LanaVN_Wildflower_05_YellowBlossom.png", new Vector2(0.5f, 0.04f));

            void SpawnMenuSprite(string name, Sprite spr, Vector3 localPos, Vector2 scale, Color tint, int order, float swayAngle, float speed, bool flipX = false)
            {
                if (spr == null) return;
                var go = new GameObject(name);
                go.transform.SetParent(root.transform, false);
                go.transform.localPosition = localPos;
                go.transform.localScale = new Vector3(scale.x, scale.y, 1f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = spr;
                sr.sharedMaterial = GetMenuSpriteMat(spr);
                sr.color = tint;
                sr.sortingOrder = order;
                sr.flipX = flipX;

                if (swayAngle > 0.01f)
                {
                    var sway = go.AddComponent<PlanetOfLanaFoliageSway>();
                    var so = new SerializedObject(sway);
                    so.FindProperty("maxRotationAngle").floatValue = swayAngle;
                    so.FindProperty("swaySpeed").floatValue = speed;
                    so.FindProperty("useWorldPositionOffset").boolValue = true;
                    so.FindProperty("manualPhaseOffset").floatValue = localPos.x * 0.5f;
                    so.FindProperty("scaleBreathing").floatValue = 0.01f;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            // Soft drifting upper-right clouds
            SpawnMenuSprite("Menu_Cloud_UpperRight", sprCloud1, new Vector3(3.6f, 2.05f, -0.05f), new Vector2(0.85f, 0.80f), new Color(1f, 0.99f, 0.95f, 0.52f), 4, 0.4f, 0.5f);
            SpawnMenuSprite("Menu_Cloud_HighWisp",   sprCloud3, new Vector3(0.4f, 2.40f, -0.05f), new Vector2(0.75f, 0.70f), new Color(1f, 0.99f, 0.96f, 0.40f), 3, 0.3f, 0.4f);

            // Right-side coastal bank framing (Coconut palm, bushes, reeds, grass carpets)
            SpawnMenuSprite("Menu_FG_Coconut_Right",  sprCoconut,  new Vector3(5.65f, -3.25f, -0.45f), new Vector2(0.58f, 0.58f), new Color(0.14f, 0.25f, 0.22f, 0.96f), 20, 1.4f, 1.1f, true);
            SpawnMenuSprite("Menu_FG_Bush_Right1",    sprBush1,    new Vector3(5.10f, -3.10f, -0.48f), new Vector2(0.62f, 0.62f), new Color(0.16f, 0.28f, 0.24f, 0.96f), 22, 1.8f, 1.4f);
            SpawnMenuSprite("Menu_FG_Bush_Right2",    sprBush2,    new Vector3(3.95f, -3.18f, -0.50f), new Vector2(0.52f, 0.52f), new Color(0.20f, 0.34f, 0.26f, 0.95f), 23, 1.9f, 1.5f);
            SpawnMenuSprite("Menu_FG_Reeds_Right",    sprReeds,    new Vector3(4.55f, -3.12f, -0.52f), new Vector2(0.56f, 0.56f), new Color(0.24f, 0.38f, 0.28f, 0.95f), 24, 2.5f, 1.8f);
            SpawnMenuSprite("Menu_FG_Grass_Right",    sprGrass1,   new Vector3(4.80f, -3.22f, -0.54f), new Vector2(0.65f, 0.60f), new Color(0.18f, 0.32f, 0.24f, 0.96f), 25, 2.2f, 1.7f);
            SpawnMenuSprite("Menu_FG_Flower_Right",   sprFlYellow, new Vector3(3.55f, -3.15f, -0.55f), new Vector2(0.50f, 0.50f), new Color(0.95f, 0.92f, 0.78f, 0.95f), 26, 2.4f, 1.9f);

            // Bottom-left subtle coastal reeds & grass carpet below the menu buttons
            SpawnMenuSprite("Menu_FG_Bush_Left",      sprBush1,    new Vector3(-5.55f, -3.18f, -0.48f), new Vector2(0.56f, 0.56f), new Color(0.14f, 0.25f, 0.22f, 0.95f), 22, 1.7f, 1.4f, true);
            SpawnMenuSprite("Menu_FG_Reeds_Left",     sprReeds,    new Vector3(-4.75f, -3.15f, -0.52f), new Vector2(0.52f, 0.52f), new Color(0.20f, 0.34f, 0.26f, 0.94f), 24, 2.4f, 1.8f, true);
            SpawnMenuSprite("Menu_FG_Grass_Left",     sprGrass2,   new Vector3(-5.15f, -3.24f, -0.54f), new Vector2(0.62f, 0.58f), new Color(0.16f, 0.29f, 0.22f, 0.96f), 25, 2.1f, 1.6f);
        }

        // =========================================================================================
        // DECORATE LANA TREES FOR DEPTH ("CHIỀU SÂU & RỪNG RẬP")
        // =========================================================================================
        private static void DecorateLanaTreesInScene()
        {
            var mgRoot = GameObject.Find("05_Midground");
            if (mgRoot == null) return;
            var ecoRoot = mgRoot.transform.Find("Lana_HandDrawn_Ecosystem");
            if (ecoRoot == null) return;

            var treesGroup = ecoRoot.Find("HandDrawn_HeroTrees");
            if (treesGroup == null) return;

            var oldLeftTree = treesGroup.Find("ZoneA_HeroTree_LeftMain");
            if (oldLeftTree != null) oldLeftTree.gameObject.SetActive(false);
            var oldLeftBack = treesGroup.Find("ZoneA_CurvedTree_LeftBack");
            if (oldLeftBack != null) oldLeftBack.gameObject.SetActive(false);

            var treeMat = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");

            Sprite sprWideCanopy  = ImportAndLoadFoliageSprite("Assets/Textures/Environment/Lana_VN_Pack/Trees/T_LanaVN_Tree_01_LargeCanopy.png", new Vector2(0.5f, 0.02f));
            Sprite sprBananaPalm  = ImportAndLoadFoliageSprite("Assets/Textures/Environment/Lana_VN_Pack/Trees/T_LanaVN_Tree_05_Banana.png", new Vector2(0.5f, 0.02f));
            Sprite sprBambooGrove = ImportAndLoadFoliageSprite("Assets/Textures/Environment/Lana_VN_Pack/Trees/T_LanaVN_Tree_07_BambooGrove.png", new Vector2(0.5f, 0.02f));
            Sprite sprCoconutPalm = ImportAndLoadFoliageSprite("Assets/Textures/Environment/Lana_VN_Pack/Trees/T_LanaVN_Tree_06_Coconut.png", new Vector2(0.45f, 0.02f));
            Sprite sprPoplarTall  = ImportAndLoadFoliageSprite("Assets/Textures/Environment/Lana_VN_Pack/Trees/T_LanaVN_Tree_04_SlenderTall.png", new Vector2(0.5f, 0.02f));
            Sprite sprFlowers     = ImportAndLoadFoliageSprite("Assets/Textures/Environment/Lana_VN_Pack/Wildflowers/T_LanaVN_Wildflower_04_DaisySprig.png", new Vector2(0.5f, 0.04f));

            void SpawnTree(string name, Sprite spr, Vector3 pos, Vector2 scale, Color tint, int order, float swayAngle, float speed)
            {
                if (spr == null) return;
                var existing = treesGroup.Find(name);
                if (existing != null) Object.DestroyImmediate(existing.gameObject);

                var go = new GameObject(name);
                go.transform.SetParent(treesGroup, false);
                go.transform.position = pos;
                go.transform.localScale = new Vector3(scale.x, scale.y, 1f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = spr;
                if (treeMat != null) sr.sharedMaterial = treeMat;
                sr.color = tint;
                sr.sortingOrder = order;

                var sway = go.AddComponent<PlanetOfLanaFoliageSway>();
                var so = new SerializedObject(sway);
                so.FindProperty("maxRotationAngle").floatValue = swayAngle;
                so.FindProperty("swaySpeed").floatValue = speed;
                so.FindProperty("useWorldPositionOffset").boolValue = true;
                so.FindProperty("manualPhaseOffset").floatValue = pos.x * 0.45f;
                so.FindProperty("scaleBreathing").floatValue = 0.012f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // 1. Wide Canopy Hero Tree in the Left Background (-4.2m)
            SpawnTree("Lana_HeroTree_WideCanopy", sprWideCanopy,
                new Vector3(-4.4f, SampleTerrainSurfaceY(-4.4f) - 0.02f, 2.35f),
                new Vector2(1.18f, 1.18f),
                new Color(0.96f, 0.98f, 0.92f, 1f), 10, 1.3f, 1.1f);

            // 2. Bamboo Grove rustling behind left knoll (-6.5m)
            SpawnTree("Lana_BambooGrove_Left", sprBambooGrove,
                new Vector3(-6.6f, SampleTerrainSurfaceY(-6.6f) - 0.05f, 3.85f),
                new Vector2(1.10f, 1.10f),
                new Color(0.85f, 0.92f, 0.88f, 1f), 8, 1.8f, 1.4f);

            // 3. Banana Palm framing the right knoll (6.8m)
            SpawnTree("Lana_BananaPalm_Right", sprBananaPalm,
                new Vector3(6.8f, SampleTerrainSurfaceY(6.8f) - 0.05f, 2.15f),
                new Vector2(1.0f, 1.0f),
                new Color(0.95f, 1.0f, 0.92f, 1f), 11, 2.2f, 1.5f);

            // 4. Coconut Palm in the distance on the right (10.2m)
            SpawnTree("Lana_CoconutPalm_Distant", sprCoconutPalm,
                new Vector3(10.2f, SampleTerrainSurfaceY(10.2f) - 0.05f, 4.25f),
                new Vector2(0.88f, 0.88f),
                new Color(0.82f, 0.90f, 0.86f, 1f), 7, 2.0f, 1.25f);

            // 5. Poplar tree in mid-right (8.6m)
            SpawnTree("Lana_Poplar_MidRight", sprPoplarTall,
                new Vector3(8.6f, SampleTerrainSurfaceY(8.6f) - 0.05f, 3.15f),
                new Vector2(0.92f, 0.92f),
                new Color(0.88f, 0.95f, 0.90f, 1f), 9, 1.5f, 1.2f);

            // 6. Wild Flower tufts on the meadow verge
            var grassGroup = ecoRoot.Find("HandDrawn_MeadowGrass") ?? treesGroup;
            if (sprFlowers != null)
            {
                var oldFl1 = grassGroup.Find("Lana_WildFlowers_Verge1");
                if (oldFl1 != null) Object.DestroyImmediate(oldFl1.gameObject);
                var fl1 = new GameObject("Lana_WildFlowers_Verge1");
                fl1.transform.SetParent(grassGroup, false);
                fl1.transform.position = new Vector3(-1.2f, SampleTerrainSurfaceY(-1.2f) + 0.02f, 1.22f);
                fl1.transform.localScale = new Vector3(0.55f, 0.55f, 1f);
                var sr1 = fl1.AddComponent<SpriteRenderer>();
                sr1.sprite = sprFlowers;
                if (treeMat != null) sr1.sharedMaterial = treeMat;
                sr1.sortingOrder = 14;

                var oldFl2 = grassGroup.Find("Lana_WildFlowers_Verge2");
                if (oldFl2 != null) Object.DestroyImmediate(oldFl2.gameObject);
                var fl2 = new GameObject("Lana_WildFlowers_Verge2");
                fl2.transform.SetParent(grassGroup, false);
                fl2.transform.position = new Vector3(3.2f, SampleTerrainSurfaceY(3.2f) + 0.02f, 1.25f);
                fl2.transform.localScale = new Vector3(0.52f, 0.52f, 1f);
                var sr2 = fl2.AddComponent<SpriteRenderer>();
                sr2.sprite = sprFlowers;
                if (treeMat != null) sr2.sharedMaterial = treeMat;
                sr2.sortingOrder = 14;
            }
        }

        // =========================================================================================
        // CLOUD RADIANCE, CREPUSCULAR SUNBEAMS & CRYSTALLINE SKY ATMOSPHERE
        // =========================================================================================
        private static void SetupCloudRadianceAndSunbeams()
        {
            string[] vistaMatNames =
            {
                "M_Vista_01_PlainsHills_Day.mat",
                "M_Vista_02_CoastalBay_Day.mat",
                "M_Vista_03_HillsFields_Sunset.mat",
                "M_Vista_04_SeaHorizon_Sunset.mat"
            };

            foreach (var matName in vistaMatNames)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>($"{GenAssetsDir}/{matName}");
                if (mat != null)
                {
                    mat.SetColor("_CloudRadianceColor", new Color(1.0f, 0.99f, 0.92f, 1.0f));
                    mat.SetFloat("_CloudRadianceIntensity", 0.44f);
                    mat.SetFloat("_SunbeamIntensity", 0.24f);
                    mat.SetVector("_SunbeamOriginUV", new Vector4(0.53f, 0.70f, 0f, 0f));
                    mat.SetFloat("_SkyClarity", 1.06f);
                    EditorUtility.SetDirty(mat);
                }
            }

            var vfxRoot = GameObject.Find("09_VFX");
            if (vfxRoot == null) vfxRoot = new GameObject("09_VFX");

            var oldCloudRadiance = vfxRoot.transform.Find("Lana_CloudRadiance_And_Sunbeams");
            if (oldCloudRadiance != null) Object.DestroyImmediate(oldCloudRadiance.gameObject);

            var radianceGroup = new GameObject("Lana_CloudRadiance_And_Sunbeams");
            radianceGroup.transform.SetParent(vfxRoot.transform, false);

            Shader godRaysShader = Shader.Find("HoaVaMau/StylizedGodRays");
            if (godRaysShader != null)
            {
                string rayMatPath = $"{GenAssetsDir}/M_Lana_CloudSunbeams.mat";
                var rayMat = AssetDatabase.LoadAssetAtPath<Material>(rayMatPath);
                if (rayMat == null)
                {
                    rayMat = new Material(godRaysShader);
                    AssetDatabase.CreateAsset(rayMat, rayMatPath);
                }
                rayMat.shader = godRaysShader;
                rayMat.SetColor("_RayColor", new Color(1.0f, 0.98f, 0.88f, 0.26f));
                rayMat.SetFloat("_RayIntensity", 1.25f);
                rayMat.SetFloat("_ShimmerSpeed", 0.55f);
                rayMat.SetFloat("_FalloffPower", 1.75f);
                rayMat.SetFloat("_BeamCount", 5.0f);
                EditorUtility.SetDirty(rayMat);

                var rayQuad1 = GameObject.CreatePrimitive(PrimitiveType.Quad);
                rayQuad1.name = "Cumulus_Cloud_Sunbeams_Main";
                rayQuad1.transform.SetParent(radianceGroup.transform, false);
                rayQuad1.transform.position = new Vector3(2.8f, 3.35f, 8.2f);
                rayQuad1.transform.rotation = Quaternion.Euler(0f, 0f, -22f);
                rayQuad1.transform.localScale = new Vector3(14.5f, 9.5f, 1f);
                var mr1 = rayQuad1.GetComponent<MeshRenderer>();
                mr1.sharedMaterial = rayMat;
                mr1.shadowCastingMode = ShadowCastingMode.Off;
                mr1.receiveShadows = false;
                Object.DestroyImmediate(rayQuad1.GetComponent<Collider>());

                var rayQuad2 = GameObject.CreatePrimitive(PrimitiveType.Quad);
                rayQuad2.name = "Cumulus_Cloud_Sunbeams_Secondary";
                rayQuad2.transform.SetParent(radianceGroup.transform, false);
                rayQuad2.transform.position = new Vector3(4.5f, 3.85f, 8.3f);
                rayQuad2.transform.rotation = Quaternion.Euler(0f, 0f, 16f);
                rayQuad2.transform.localScale = new Vector3(12.0f, 7.8f, 1f);
                var mr2 = rayQuad2.GetComponent<MeshRenderer>();
                mr2.sharedMaterial = rayMat;
                mr2.shadowCastingMode = ShadowCastingMode.Off;
                mr2.receiveShadows = false;
                Object.DestroyImmediate(rayQuad2.GetComponent<Collider>());
            }

            var dustPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/VFX/PF_Atmospheric_GoldenDust.prefab");
            if (dustPrefab != null)
            {
                var dust = (GameObject)PrefabUtility.InstantiatePrefab(dustPrefab, radianceGroup.transform);
                dust.name = "MenuVista_Sunlit_SkyMotes";
                dust.transform.position = new Vector3(1.8f, 1.2f, -2.0f);
            }
        }

        // =========================================================================================
        // SHADOW-CASTING TREES & DAPPLED GROUND SHADOWS
        // =========================================================================================
        private static void SetupShadowCastingTreesAndDappledGroundShadows()
        {
            var ecoRoot = GameObject.Find("05_Midground/Lana_HandDrawn_Ecosystem");
            if (ecoRoot == null) return;

            var treesGroup = ecoRoot.transform.Find("HandDrawn_HeroTrees");
            if (treesGroup == null) return;

            var prefabCurvedTreeV2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Trees/PF_Curved_Hero_Tree_V2.prefab");
            var treeSpriteMat = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");

            if (prefabCurvedTreeV2 != null && treesGroup.Find("ZoneA_RightMeadow_ShadowTree") == null)
            {
                var rightMeadowTree = (GameObject)PrefabUtility.InstantiatePrefab(prefabCurvedTreeV2, treesGroup);
                rightMeadowTree.name = "ZoneA_RightMeadow_ShadowTree";
                rightMeadowTree.transform.position = new Vector3(5.6f, -1.52f - 1.22f * 0.66f, 1.62f);
                rightMeadowTree.transform.localScale = new Vector3(-0.66f, 0.66f, 1.0f);
                foreach (var tr in rightMeadowTree.GetComponentsInChildren<Transform>(true))
                {
                    if (tr != null && tr.gameObject != null && tr.gameObject.name.Contains("Mound"))
                    {
                        Object.DestroyImmediate(tr.gameObject);
                    }
                }
                foreach (var sr in rightMeadowTree.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (treeSpriteMat != null) sr.sharedMaterial = treeSpriteMat;
                }
            }

            Shader dappledShader = Shader.Find("HoaVaMau/LanaDappledGroundShadow");
            if (dappledShader == null) dappledShader = Shader.Find("Universal Render Pipeline/Unlit");

            string dappledMatPath = $"{GenAssetsDir}/M_Lana_DappledGroundShadow.mat";
            var dappledMat = AssetDatabase.LoadAssetAtPath<Material>(dappledMatPath);
            if (dappledMat == null)
            {
                dappledMat = new Material(dappledShader);
                AssetDatabase.CreateAsset(dappledMat, dappledMatPath);
            }
            dappledMat.shader = dappledShader;
            dappledMat.SetColor("_ShadowColor", new Color(0.06f, 0.18f, 0.15f, 0.64f));
            dappledMat.SetFloat("_SwaySpeed", 1.2f);
            dappledMat.SetFloat("_DappleScale", 4.4f);
            dappledMat.SetFloat("_Softness", 0.50f);
            EditorUtility.SetDirty(dappledMat);

            string shadowProxyMatPath = $"{GenAssetsDir}/M_Lana_ShadowProxy.mat";
            var shadowProxyMat = AssetDatabase.LoadAssetAtPath<Material>(shadowProxyMatPath);
            if (shadowProxyMat == null)
            {
                shadowProxyMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                AssetDatabase.CreateAsset(shadowProxyMat, shadowProxyMatPath);
            }

            var oldShadowsGroup = ecoRoot.transform.Find("Lana_Tree_GroundShadows");
            if (oldShadowsGroup != null) Object.DestroyImmediate(oldShadowsGroup.gameObject);

            var shadowsGroup = new GameObject("Lana_Tree_GroundShadows");
            shadowsGroup.transform.SetParent(ecoRoot.transform, false);

            string meshDir = $"{GenAssetsDir}/GeneratedMeshes";
            if (!Directory.Exists(meshDir)) Directory.CreateDirectory(meshDir);

            int treeIdx = 0;
            foreach (Transform tree in treesGroup)
            {
                Vector3 tPos = tree.position;
                float scaleMag = Mathf.Abs(tree.localScale.x);

                var oldCaster = tree.Find("URP_3D_ShadowCaster_Proxy");
                if (oldCaster != null) Object.DestroyImmediate(oldCaster.gameObject);

                var casterRoot = new GameObject("URP_3D_ShadowCaster_Proxy");
                casterRoot.transform.SetParent(tree, false);
                casterRoot.transform.localPosition = new Vector3(0f, 0f, -1.1f);

                var trunkCaster = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                trunkCaster.name = "ShadowCaster_Trunk";
                trunkCaster.transform.SetParent(casterRoot.transform, false);
                trunkCaster.transform.localPosition = new Vector3(0f, 1.4f, 0f);
                trunkCaster.transform.localScale = new Vector3(0.38f, 1.4f, 0.38f);
                ConfigureShadowsOnlyRenderer(trunkCaster, shadowProxyMat);

                Vector3[] canopyOffsets =
                {
                    new Vector3(0.0f, 3.1f, 0.0f),
                    new Vector3(-0.95f, 2.65f, 0.1f),
                    new Vector3(0.95f, 2.75f, -0.1f),
                    new Vector3(0.15f, 3.65f, 0.0f)
                };
                Vector3[] canopyScales =
                {
                    new Vector3(2.6f, 1.55f, 2.2f),
                    new Vector3(1.95f, 1.25f, 1.8f),
                    new Vector3(2.05f, 1.30f, 1.8f),
                    new Vector3(2.15f, 1.35f, 1.9f)
                };

                for (int c = 0; c < canopyOffsets.Length; c++)
                {
                    var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    sphere.name = $"ShadowCaster_Canopy_{c}";
                    sphere.transform.SetParent(casterRoot.transform, false);
                    sphere.transform.localPosition = canopyOffsets[c];
                    sphere.transform.localScale = canopyScales[c];
                    ConfigureShadowsOnlyRenderer(sphere, shadowProxyMat);
                }

                float shadowWidth = Mathf.Clamp(scaleMag * 5.8f, 3.4f, 5.6f);
                float shadowCenterX = tPos.x + 0.85f * scaleMag;
                Mesh shadowMesh = BuildConformedGroundShadowMesh(
                    $"TreeGroundShadow_{treeIdx:D2}",
                    shadowCenterX - shadowWidth * 0.5f,
                    shadowCenterX + shadowWidth * 0.5f,
                    24,
                    frontZ: -1.68f,
                    backZ: 1.55f
                );
                AssetDatabase.CreateAsset(shadowMesh, $"{meshDir}/TreeGroundShadow_{treeIdx:D2}.asset");

                var shadowGO = new GameObject($"DappledShadow_{tree.name}");
                shadowGO.transform.SetParent(shadowsGroup.transform, false);
                shadowGO.AddComponent<MeshFilter>().sharedMesh = shadowMesh;
                var smr = shadowGO.AddComponent<MeshRenderer>();
                smr.sharedMaterial = dappledMat;
                smr.shadowCastingMode = ShadowCastingMode.Off;
                smr.receiveShadows = false;

                treeIdx++;
            }
        }

        private static void ConfigureShadowsOnlyRenderer(GameObject go, Material mat)
        {
            var col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                mr.receiveShadows = false;
            }
        }

        private static Mesh BuildConformedGroundShadowMesh(
            string name, float startX, float endX, int segX, float frontZ, float backZ)
        {
            int segZ = 8;
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var colors = new List<Color>();
            var tris = new List<int>();

            for (int z = 0; z <= segZ; z++)
            {
                float vz = (float)z / segZ;
                float worldZ = Mathf.Lerp(frontZ, backZ, vz);
                float zArcLift = Mathf.Sin(vz * Mathf.PI) * 0.035f;

                for (int x = 0; x <= segX; x++)
                {
                    float ux = (float)x / segX;
                    float worldX = Mathf.Lerp(startX, endX, ux);
                    float surfY = SampleTerrainSurfaceY(worldX) + 0.042f + zArcLift;

                    verts.Add(new Vector3(worldX, surfY, worldZ));
                    uvs.Add(new Vector2(ux, vz));
                    colors.Add(Color.white);
                }
            }

            int cols = segX + 1;
            for (int z = 0; z < segZ; z++)
            {
                for (int x = 0; x < segX; x++)
                {
                    int i0 = z * cols + x;
                    int i1 = i0 + 1;
                    int i2 = (z + 1) * cols + x;
                    int i3 = i2 + 1;

                    tris.Add(i0); tris.Add(i2); tris.Add(i1);
                    tris.Add(i1); tris.Add(i2); tris.Add(i3);
                }
            }

            var mesh = new Mesh { name = name };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static float SampleTerrainSurfaceY(float x)
        {
            float baseH = -1.85f;
            if (x >= 5.2f && x < 7.2f) baseH = Mathf.Lerp(-1.85f, -1.15f, SmoothCos((x - 5.2f) / 2.0f));
            else if (x >= 7.2f && x < 8.8f) baseH = -1.15f;
            else if (x >= 8.8f && x < 10.6f) baseH = Mathf.Lerp(-1.15f, -1.85f, SmoothCos((x - 8.8f) / 1.8f));
            else if (x >= 10.6f && x < 13.5f) baseH = Mathf.Lerp(-1.85f, -2.10f, SmoothCos((x - 10.6f) / 2.9f));
            else if (x >= 13.5f && x < 16.8f) baseH = -2.10f;
            else if (x >= 16.8f && x < 18.8f) baseH = Mathf.Lerp(-2.10f, -1.85f, SmoothCos((x - 16.8f) / 2.0f));
            else if (x >= 18.8f && x < 21.0f) baseH = Mathf.Lerp(-1.85f, -0.35f, SmoothCos((x - 18.8f) / 2.2f));
            else if (x >= 21.0f && x < 25.8f) baseH = -0.35f;
            else if (x >= 25.8f && x < 27.2f) baseH = Mathf.Lerp(-0.35f, -1.15f, SmoothCos((x - 25.8f) / 1.4f));
            else if (x >= 27.2f && x < 28.6f) baseH = -1.15f;
            else if (x >= 28.6f && x < 30.2f) baseH = Mathf.Lerp(-1.15f, -1.85f, SmoothCos((x - 28.6f) / 1.6f));
            else if (x >= 31.8f && x < 33.8f) baseH = Mathf.Lerp(-1.85f, -2.35f, SmoothCos((x - 31.8f) / 2.0f));
            else if (x >= 33.8f && x < 40.2f) baseH = -2.35f;
            else if (x >= 40.2f && x < 42.2f) baseH = Mathf.Lerp(-2.35f, -1.85f, SmoothCos((x - 40.2f) / 2.0f));
            else if (x >= 42.2f && x < 45.0f) baseH = Mathf.Lerp(-1.85f, -1.70f, SmoothCos((x - 42.2f) / 2.8f));
            else if (x >= 45.0f && x < 55.5f) baseH = -1.70f;
            else if (x >= 55.5f && x < 57.5f) baseH = Mathf.Lerp(-1.70f, -1.35f, SmoothCos((x - 55.5f) / 2.0f));
            else if (x >= 57.5f && x < 61.0f) baseH = -1.35f;
            else if (x >= 61.0f) baseH = Mathf.Lerp(-1.35f, -1.75f, SmoothCos(Mathf.Clamp01((x - 61.0f) / 2.5f)));

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

        // =========================================================================================
        // SETUP START MENU SYSTEM IN SCENE
        // =========================================================================================
        private static PlanetOfLanaStartMenuSystem SetupStartMenuSystemInScene(
            Sprite studioLogoSpr, Sprite officialLogoSpr, Sprite flowerSpr, Sprite doveSpr, Sprite softDotSpr, Sprite boySpr, Sprite shipSpr, AudioClip bgmClip, AudioClip windClip, string targetScene)
        {
            var existing = GameObject.Find("00_UI_PlanetOfLana_StartMenu");
            if (existing != null) Object.DestroyImmediate(existing);

            var rootGO = new GameObject("00_UI_PlanetOfLana_StartMenu");
            rootGO.transform.SetAsFirstSibling();

            var sys = rootGO.AddComponent<PlanetOfLanaStartMenuSystem>();
            Texture2D[] fallbackThumbs = new Texture2D[] {
                AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Backgrounds_Lana/BG_Lana_MainSkyHills_Official.png"),
                AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Backgrounds_Lana/BG_Lana_CoastalBay_Day.jpg"),
                AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Backgrounds_Lana/BG_Lana_HillsFields_Sunset.jpg")
            };
            sys.SetFallbackSlotThumbnails(fallbackThumbs);
            AudioClip track1 = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Phat_Hien_Binh_Yen.mp3");
            AudioClip track2 = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Pastoral_Alien.mp3");
            AudioClip track3 = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Breath of the World.mp3");
                        var menuPlaylist = new List<AudioClip>();
            if (track1 != null) menuPlaylist.Add(track1);
            if (track2 != null) menuPlaylist.Add(track2);
            if (track3 != null) menuPlaylist.Add(track3);
            
            sys.ConfigureAssets(studioLogoSpr, officialLogoSpr, flowerSpr, doveSpr, softDotSpr, boySpr, shipSpr, bgmClip, windClip, targetScene, null, menuPlaylist.ToArray());
            sys.SetRandomMenuMusicClips(menuPlaylist.ToArray());
            sys.RebuildUIImmediate();
            sys.SetPreviewState(PlanetOfLanaStartMenuSystem.MenuPhase.StartMenuOpenVista);

            var mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.transform.position = new Vector3(1.6f, 0.38f, -13.5f);
                mainCam.transform.rotation = Quaternion.identity;
                mainCam.fieldOfView = 37.5f;
            }

            return sys;
        }

        private static PlanetOfLanaStartMenuSystem SetupStartMenuSystemInScene(
            Sprite studioLogoSpr, Sprite officialLogoSpr, Sprite flowerSpr, Sprite doveSpr, Sprite softDotSpr, AudioClip bgmClip, AudioClip windClip, string targetScene)
        {
            Sprite boySpr  = ImportAndLoadUISprite("Assets/Textures/PierScene/running_boy_clean_silhouette.png");
            Sprite shipSpr = ImportAndLoadUISprite("Assets/Textures/Canh1_Sprites/TransportShip_1954_Card.png");
            return SetupStartMenuSystemInScene(studioLogoSpr, officialLogoSpr, flowerSpr, doveSpr, softDotSpr, boySpr, shipSpr, bgmClip, windClip, targetScene);
        }

        private static PlanetOfLanaStartMenuSystem SetupStartMenuSystemInScene(
            Sprite studioLogoSpr, Sprite flowerSpr, Sprite doveSpr, Sprite softDotSpr, AudioClip bgmClip, AudioClip windClip, string targetScene)
        {
            return SetupStartMenuSystemInScene(studioLogoSpr, null, flowerSpr, doveSpr, softDotSpr, bgmClip, windClip, targetScene);
        }

        // =========================================================================================
        // CAPTURE VERIFICATION SCREENSHOTS
        // =========================================================================================
        private static void CaptureStartMenuVerificationScreenshots(PlanetOfLanaStartMenuSystem menuSystem, int onlyPhase = -1)
        {
            var cam = Camera.main;
            if (cam == null || menuSystem == null) return;

            // Temporarily hide Player character if present so start menu vista is 100% serene nature
            var playerGO = GameObject.Find("PF_Player_Minh") ?? GameObject.Find("Character_An");
            bool playerWasActive = false;
            if (playerGO != null)
            {
                playerWasActive = playerGO.activeSelf;
                playerGO.SetActive(false);
            }

            // Elegantly hide old hero trees so only authentic Lana trees are visible
            var oldLeftTree = GameObject.Find("ZoneA_HeroTree_LeftMain");
            if (oldLeftTree != null) oldLeftTree.SetActive(false);
            var oldLeftBack = GameObject.Find("ZoneA_CurvedTree_LeftBack");
            if (oldLeftBack != null) oldLeftBack.SetActive(false);

            string outDir = Path.Combine(Application.dataPath, "../Screenshots/Showcase_Presentation");
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

            var canvas = menuSystem.GetComponentInChildren<Canvas>(true);
            RenderMode prevMode = RenderMode.ScreenSpaceOverlay;
            if (canvas != null)
            {
                prevMode = canvas.renderMode;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1.0f;
            }

            System.Action<int, string, PlanetOfLanaStartMenuSystem.MenuPhase, bool, int, int, System.Action> capturePhase = (idx, fileName, phase, showCanvas, w, h, customAction) =>
            {
                if (onlyPhase >= 1 && onlyPhase != idx) return;

                if (canvas != null) canvas.gameObject.SetActive(showCanvas);
                if (showCanvas) menuSystem.SetPreviewState(phase, 1.0f);
                customAction?.Invoke();
                Canvas.ForceUpdateCanvases();

                int width = w > 0 ? w : 1280;
                int height = h > 0 ? h : 720;
                var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
                var prevRt = cam.targetTexture;
                cam.targetTexture = rt;
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
                Debug.Log($"<color=#38bdf8>[StartMenu Capture]</color> Saved verification screenshot: {fileName}");
            };

            // Ensure dummy save slot data exists for realistic Continue and Save selection preview (without capturing in edit mode)
            HoaVaMauSaveSystem.SaveCurrentGameState(1, "Chương 1: Đất Đỏ Cà Mau", "Rừng Sớm Cà Mau", 0, new Vector3(-4.0f, -1.85f, 0f), captureThumbnail: false);

            // 1. State B: Team Loading Screen (Five Cows centered, Bird loader in bottom-right corner)
            capturePhase(1, "StartMenu_01_TeamLoading_FiveCowsInTheFire.png", PlanetOfLanaStartMenuSystem.MenuPhase.TeamLoadingScreen, true, 1280, 720, null);

            // 2. State C: Open Scenic Start Menu (Living vista, Top-Left calligraphy logo, buttons: BẮT ĐẦU, TIẾP TỤC, TÙY CHỈNH, THOÁT)
            capturePhase(2, "StartMenu_02_MainMenu_OpenVista_UI.png", PlanetOfLanaStartMenuSystem.MenuPhase.StartMenuOpenVista, true, 1280, 720, null);

            // 3. Save Slot Selection Modal (3 real slots, metadata, chapter, playtime, date)
            capturePhase(3, "StartMenu_03_SaveSlotSelection_Modal.png", PlanetOfLanaStartMenuSystem.MenuPhase.SaveSlotSelection, true, 1280, 720, null);

            // 4. Overwrite Confirmation Dialog
            capturePhase(4, "StartMenu_04_OverwriteConfirmation_Modal.png", PlanetOfLanaStartMenuSystem.MenuPhase.SaveSlotSelection, true, 1280, 720, () =>
            {
                menuSystem.OnSlotCardClicked(1); // Clicking occupied slot 1 triggers explicit overwrite dialog
            });

            // 5. Autosave Corner Widget
            capturePhase(5, "StartMenu_05_Autosave_Indicator.png", PlanetOfLanaStartMenuSystem.MenuPhase.StartMenuOpenVista, true, 1280, 720, () =>
            {
                menuSystem.StartAutosaveAnimation("Bờ Sông Đất Đỏ");
            });

            // 6. UI Scaling Verification at 16:10 Aspect Ratio (1280x800)
            capturePhase(6, "StartMenu_06_16x10_Aspect_Verification.png", PlanetOfLanaStartMenuSystem.MenuPhase.StartMenuOpenVista, true, 1280, 800, null);

            // 7. UI Scaling Verification at 21:9 Ultrawide Aspect Ratio (1680x720)
            capturePhase(7, "StartMenu_07_21x9_Ultrawide_Verification.png", PlanetOfLanaStartMenuSystem.MenuPhase.StartMenuOpenVista, true, 1680, 720, null);

            // 8. Options / Settings Modal
            capturePhase(8, "StartMenu_08_OptionsSettings_Modal.png", PlanetOfLanaStartMenuSystem.MenuPhase.Options, true, 1280, 720, null);

            // 9. Zelda Breath of the Wild Style Loading Screen (An, 1954 Ship, Tips, Sheikah Dots)
            capturePhase(9, "StartMenu_09_BotW_LoadingScreen.png", PlanetOfLanaStartMenuSystem.MenuPhase.BotWLoadingScreen, true, 1280, 720, null);

            if (canvas != null)
            {
                canvas.gameObject.SetActive(true);
                canvas.renderMode = prevMode;
                canvas.transform.localScale = Vector3.one;
            }

            // Restore character state if needed
            if (playerGO != null) playerGO.SetActive(playerWasActive);
        }

        private static void RegisterScenesInBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>();
            string[] paths = { MainMenuScenePath, OverhaulScenePath, "Assets/Scenes/CaMau_1954_Village.unity", "Assets/Scenes/canh1.unity" };
            foreach (var p in paths)
            {
                if (File.Exists(p))
                {
                    scenes.Add(new EditorBuildSettingsScene(p, true));
                }
            }
            if (scenes.Count > 0)
            {
                EditorBuildSettings.scenes = scenes.ToArray();
            }
        }
    }
}
#endif
