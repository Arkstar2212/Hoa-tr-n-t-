using UnityEngine;
using HoaVaMau;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Master Level Builder for the 2.5D Traversal & Stealth Graybox Prototype.
    /// Builds a clean, cinematic level featuring:
    /// - Checkpoint system (no health bar, instant checkpoint respawn upon capture or fall)
    /// - Overhead timber crossbeam supporting the rope (recessed to Z = 0.6, no player collision)
    /// - Planet of Lana Depth Stealth Setup (Enemy perched in midground overlooking an open gap, periodically looking back)
    /// - Interactive Bending Grass (sways in wind and pushes aside when player runs through)
    /// - Global Wind & Atmospheric Pollen Particle system
    /// - Swaying environmental background trees
    /// </summary>
    [ExecuteInEditMode]
    public class GameplayGrayboxBuilder : MonoBehaviour
    {
#if UNITY_EDITOR
        private const string SceneAssetPath = "Assets/Scenes/SampleScene.unity";

        [MenuItem("Hoa va Mau/Build Gameplay Graybox Level")]
        public static void BuildMenuCommand()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(scene.path) || scene.path != SceneAssetPath)
            {
                scene = EditorSceneManager.OpenScene(SceneAssetPath, OpenSceneMode.Single);
            }
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);

            // 1. Purge all legacy clutter & old graybox in memory (do NOT save empty scene to disk!)
            PurgeLegacyEnvironment(true, false);

            // 2. Build fresh graybox level
            GameObject builderGO = new GameObject("Temp_GrayboxBuilder");
            GameplayGrayboxBuilder builder = builderGO.AddComponent<GameplayGrayboxBuilder>();
            bool buildSuccess = false;
            try
            {
                builder.BuildLevel(false);
                buildSuccess = true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError("<color=#ff4d4f>[Hoa và Máu]</color> Critical error during BuildLevel: " + ex);
            }
            finally
            {
                DestroyImmediate(builderGO);
            }

            if (buildSuccess)
            {
                // 3. Save clean scene
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, SceneAssetPath);
                Debug.Log("<color=#40b8a6>[Hoa và Máu]</color> Fresh Gameplay Graybox Level built and saved to: " + SceneAssetPath);
            }
        }

        public static void PurgeLegacyEnvironment(bool removeOldGraybox = false, bool saveScene = true)
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(scene.path) || scene.path != SceneAssetPath)
            {
                scene = EditorSceneManager.OpenScene(SceneAssetPath, OpenSceneMode.Single);
            }
            var roots = scene.GetRootGameObjects();

            bool dirty = false;
            foreach (var root in roots)
            {
                if (root == null) continue;

                if (root.name == "--- Hoa va Mau Graybox Prototype ---")
                {
                    if (removeOldGraybox)
                    {
                        DestroyImmediate(root);
                        dirty = true;
                    }
                    continue;
                }

                // Preserve standard scene essentials
                if (root.name == "Main Camera" || root.name == "Directional Light" || root.name == "Global Volume")
                {
                    continue;
                }

                // Destroy any legacy 2D roots, builders, old characters, or temporary objects
                string n = root.name;
                if (n.Contains("Hoa va Mau Environment") ||
                    n.Contains("Planet of Lana") ||
                    n.Contains("Environment") ||
                    n.Contains("Builder") ||
                    n.Contains("Player_Character") ||
                    n.Contains("Player_Companion") ||
                    n.Contains("Grass_") ||
                    n.Contains("Cloud") ||
                    n.Contains("Pier") ||
                    n.Contains("Red_Pennant") ||
                    n.Contains("Celestial") ||
                    n.Contains("Mountains") ||
                    n.Contains("Lake") ||
                    n.Contains("Temp"))
                {
                    DestroyImmediate(root);
                    dirty = true;
                }
            }

            // Also clean up any loose particle systems or floating legacy sprites
            foreach (var ps in Object.FindObjectsByType<ParticleSystem>())
            {
                if (ps != null && (ps.gameObject.name.Contains("Ambient") || ps.gameObject.name.Contains("Footstep")))
                {
                    DestroyImmediate(ps.gameObject);
                    dirty = true;
                }
            }

            // Purge all old spiky procedural InteractiveGrass blades in scene
            foreach (var ig in Object.FindObjectsByType<InteractiveGrass>(FindObjectsInactive.Include))
            {
                if (ig != null && ig.gameObject != null)
                {
                    DestroyImmediate(ig.gameObject);
                    dirty = true;
                }
            }

            if (dirty && saveScene)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("<color=#40b8a6>[Hoa và Máu]</color> Purged all legacy objects from scene.");
            }
        }
#endif

        public void BuildLevel(bool saveScene = false)
        {
            Debug.Log("<color=#40b8a6>[Hoa và Máu]</color> Building 2.5D Traversal & Stealth Graybox Level...");

            // Root
            GameObject rootGO = new GameObject("--- Hoa va Mau Graybox Prototype ---");

            // Setup Materials
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");

            Material groundMat = new Material(litShader) { name = "Mat_Cliff_Stone", color = new Color(0.36f, 0.40f, 0.38f) };
            if (groundMat.HasProperty("_BaseColor")) groundMat.SetColor("_BaseColor", new Color(0.36f, 0.40f, 0.38f));
            Material meadowMat = new Material(litShader) { name = "Mat_Meadow_Turf", color = new Color(0.28f, 0.58f, 0.22f) };
            if (meadowMat.HasProperty("_BaseColor")) meadowMat.SetColor("_BaseColor", new Color(0.28f, 0.58f, 0.22f));
            if (meadowMat.HasProperty("_Smoothness")) meadowMat.SetFloat("_Smoothness", 0.12f);
            Material woodMat = new Material(litShader) { name = "Mat_Wood_Timber", color = new Color(0.55f, 0.38f, 0.24f) };
            if (woodMat.HasProperty("_BaseColor")) woodMat.SetColor("_BaseColor", new Color(0.55f, 0.38f, 0.24f));
            Material bushMat = new Material(litShader) { name = "Mat_Bush_Green", color = new Color(0.18f, 0.48f, 0.20f) };
            if (bushMat.HasProperty("_BaseColor")) bushMat.SetColor("_BaseColor", new Color(0.18f, 0.48f, 0.20f));
            Material crateMat = new Material(litShader) { name = "Mat_Crate_Brown", color = new Color(0.62f, 0.44f, 0.28f) };
            if (crateMat.HasProperty("_BaseColor")) crateMat.SetColor("_BaseColor", new Color(0.62f, 0.44f, 0.28f));
            Material playerMat = new Material(litShader) { name = "Mat_Player_Blue", color = new Color(0.18f, 0.48f, 0.84f) };
            if (playerMat.HasProperty("_BaseColor")) playerMat.SetColor("_BaseColor", new Color(0.18f, 0.48f, 0.84f));
            Material enemyMat = new Material(litShader) { name = "Mat_Enemy_Dark", color = new Color(0.35f, 0.22f, 0.25f) };
            if (enemyMat.HasProperty("_BaseColor")) enemyMat.SetColor("_BaseColor", new Color(0.35f, 0.22f, 0.25f));

            // 3D Refugee Shelter Room Materials
            Material floorMat = new Material(litShader) { name = "Mat_Shelter_WoodFloor", color = new Color(0.42f, 0.32f, 0.22f) };
            if (floorMat.HasProperty("_BaseColor")) floorMat.SetColor("_BaseColor", new Color(0.42f, 0.32f, 0.22f));
            Material bambooMat = new Material(litShader) { name = "Mat_Bamboo_Mat", color = new Color(0.82f, 0.76f, 0.52f) };
            if (bambooMat.HasProperty("_BaseColor")) bambooMat.SetColor("_BaseColor", new Color(0.82f, 0.76f, 0.52f));
            Material blanketMat = new Material(litShader) { name = "Mat_Blanket_Green", color = new Color(0.24f, 0.36f, 0.24f) };
            if (blanketMat.HasProperty("_BaseColor")) blanketMat.SetColor("_BaseColor", new Color(0.24f, 0.36f, 0.24f));
            Material brickMat = new Material(litShader) { name = "Mat_Firepit_Brick", color = new Color(0.48f, 0.26f, 0.20f) };
            if (brickMat.HasProperty("_BaseColor")) brickMat.SetColor("_BaseColor", new Color(0.48f, 0.26f, 0.20f));
            Material flagMat = new Material(litShader) { name = "Mat_Flag_Red", color = new Color(0.92f, 0.16f, 0.16f) };
            if (flagMat.HasProperty("_BaseColor")) flagMat.SetColor("_BaseColor", new Color(0.92f, 0.16f, 0.16f));
            Material goldStarMat = new Material(litShader) { name = "Mat_Gold_Star", color = new Color(0.98f, 0.85f, 0.10f) };
            if (goldStarMat.HasProperty("_BaseColor")) goldStarMat.SetColor("_BaseColor", new Color(0.98f, 0.85f, 0.10f));
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            Material lanternMat = new Material(unlitShader) { name = "Mat_Lantern_Amber", color = new Color(1.0f, 0.75f, 0.25f) };
            if (lanternMat.HasProperty("_BaseColor")) lanternMat.SetColor("_BaseColor", new Color(1.0f, 0.75f, 0.25f));
            Material ironMat = new Material(litShader) { name = "Mat_Iron_Dark", color = new Color(0.28f, 0.30f, 0.34f) };
            if (ironMat.HasProperty("_BaseColor")) ironMat.SetColor("_BaseColor", new Color(0.28f, 0.30f, 0.34f));
            Material skinMat = new Material(litShader) { name = "Mat_Skin_Warm", color = new Color(0.92f, 0.74f, 0.58f) };
            if (skinMat.HasProperty("_BaseColor")) skinMat.SetColor("_BaseColor", new Color(0.92f, 0.74f, 0.58f));
            Material companionMat = new Material(litShader) { name = "Mat_Companion_Slate", color = new Color(0.32f, 0.36f, 0.42f) };
            if (companionMat.HasProperty("_BaseColor")) companionMat.SetColor("_BaseColor", new Color(0.32f, 0.36f, 0.42f));
            Material eyeGlowMat = new Material(unlitShader) { name = "Mat_Eye_Glow", color = new Color(0.20f, 0.95f, 0.90f) };
            if (eyeGlowMat.HasProperty("_BaseColor")) eyeGlowMat.SetColor("_BaseColor", new Color(0.20f, 0.95f, 0.90f));

            Shader transparentShader = Shader.Find("HoaVaMau/VisionConeFade");
            if (transparentShader == null) transparentShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (transparentShader == null) transparentShader = Shader.Find("Sprites/Default");
            Material coneMat = new Material(transparentShader) { name = "Mat_VisionCone", color = new Color(0.28f, 0.80f, 0.95f, 0.45f) };
            if (coneMat.HasProperty("_BaseColor")) coneMat.SetColor("_BaseColor", new Color(0.28f, 0.80f, 0.95f, 0.45f));

            // 1. DIRECTIONAL LIGHTING & AMBIENT ENVIRONMENT
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.72f, 0.82f, 0.94f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.65f, 0.58f);
            RenderSettings.ambientGroundColor = new Color(0.38f, 0.44f, 0.36f);
            RenderSettings.ambientIntensity = 1.15f;

            GameObject lightGO = GameObject.Find("Directional Light");
            if (lightGO == null)
            {
                lightGO = new GameObject("Directional Light");
                Light l = lightGO.AddComponent<Light>();
                l.type = LightType.Directional;
            }
            lightGO.transform.rotation = Quaternion.Euler(32f, 25f, 0f);
            Light dirLight = lightGO.GetComponent<Light>();
            dirLight.color = new Color(1.0f, 0.97f, 0.92f);
            dirLight.intensity = 1.25f;

            // 2. CAMERA SETUP
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camGO = new GameObject("Main Camera");
                cam = camGO.AddComponent<Camera>();
                camGO.tag = "MainCamera";
            }
            cam.orthographic = false;
            cam.fieldOfView = 36f;
            cam.backgroundColor = new Color(0.48f, 0.72f, 0.90f);
            cam.transform.position = new Vector3(-8f, 2.5f, -11.5f);

            foreach (var comp in cam.GetComponents<MonoBehaviour>())
            {
                if (comp.GetType().Name.Contains("Parallax") || comp.GetType().Name.Contains("HUD"))
                {
                    DestroyImmediate(comp);
                }
            }
            Cinematic2DCamera camCtrl = cam.GetComponent<Cinematic2DCamera>();
            if (camCtrl == null) camCtrl = cam.gameObject.AddComponent<Cinematic2DCamera>();

            // 3. GLOBAL MANAGERS (WIND, GRASS, CHECKPOINTS, & AI DDA)
            GameObject managersGO = new GameObject("Managers");
            managersGO.transform.SetParent(rootGO.transform);
            managersGO.AddComponent<WindManager>();
            managersGO.AddComponent<GrassManager>();
            CheckpointManager cpMgr = managersGO.AddComponent<CheckpointManager>();
            managersGO.AddComponent<DynamicDifficultyManager>();
            managersGO.AddComponent<DistractionManager>();
            EnvironmentManager envMgr = managersGO.AddComponent<EnvironmentManager>();
            SetPrivateField(envMgr, "directionalLight", dirLight);

            // 4. CLEAN CINEMATIC BACKGROUND
            GameObject bgGroup = new GameObject("Background_Sky");
            bgGroup.transform.SetParent(rootGO.transform);

            // Distant painterly sky backdrop
            GameObject skyQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            skyQuad.name = "Sky_Backdrop";
            skyQuad.transform.SetParent(bgGroup.transform);
            skyQuad.transform.position = new Vector3(12f, 20f, 48f);
            skyQuad.transform.localScale = new Vector3(150f, 84f, 1f);
            DestroyImmediate(skyQuad.GetComponent<Collider>());

            // Load painted game background material or texture
            Material skyMat = null;
#if UNITY_EDITOR
            skyMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Sky_Background.mat");
#endif
            if (skyMat == null)
            {
                Texture2D bgTex = null;
#if UNITY_EDITOR
                bgTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/game_background_sky.png");
#endif
                if (bgTex == null)
                {
                    bgTex = Resources.Load<Texture2D>("game_background_sky");
                }

                Shader bgShader = Shader.Find("Universal Render Pipeline/Unlit");
                if (bgShader == null) bgShader = Shader.Find("Unlit/Texture");
                if (bgShader == null) bgShader = Shader.Find("Standard");

                skyMat = new Material(bgShader) { name = "Mat_Sky_Background" };
                if (bgTex != null)
                {
                    skyMat.mainTexture = bgTex;
                    if (skyMat.HasProperty("_BaseMap")) skyMat.SetTexture("_BaseMap", bgTex);
                }
                skyMat.color = Color.white;
                if (skyMat.HasProperty("_BaseColor")) skyMat.SetColor("_BaseColor", Color.white);
            }
            
            MeshRenderer skyRenderer = skyQuad.GetComponent<MeshRenderer>();
            skyRenderer.sharedMaterial = skyMat;
            skyRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            skyRenderer.receiveShadows = false;
            SetPrivateField(envMgr, "skyRenderer", skyRenderer);

            // Environmental Swaying Trees
            CreateSwayingTree(bgGroup.transform, "Swaying_Tree_Cliff", new Vector3(6.5f, 3.2f, 4.5f), woodMat, bushMat);
            CreateSwayingTree(bgGroup.transform, "Swaying_Tree_Plateau", new Vector3(29.0f, 4.2f, 5.0f), woodMat, bushMat);

            // Atmospheric Drifting Pollen Particles
            GameObject pollenGO = CreateAtmosphericPollen(bgGroup.transform);
            if (pollenGO != null)
            {
                SetPrivateField(envMgr, "ambientParticleSystem", pollenGO.GetComponent<ParticleSystem>());
            }

            // ==========================================
            // 5. LEVEL GEOMETRY & PLATFORMS
            // ==========================================
            GameObject geoGroup = new GameObject("Geometry");
            geoGroup.transform.SetParent(rootGO.transform);

            // Section 1: Start Platform (X: -11 to -3, Top Y: 0)
            // Left World Barrier prevents accidentally falling off world bounds
            CreateBlock(geoGroup.transform, "Boundary_Wall_Left", new Vector3(-11.8f, 3.5f, 0f), new Vector3(1.6f, 14.0f, 3.5f), groundMat);

            CreateMeadowPlatform(geoGroup.transform, "Platform_Start", new Vector3(-7f, -2.0f, 0f), new Vector3(8f, 4.0f, 3f), groundMat, meadowMat);
            CreateStealthGrassHideZone(geoGroup.transform, "Stealth_Practice", new Vector3(-7.5f, 0.28f, 0f), 1.8f, bushMat);
            CreateCrate(geoGroup.transform, "Crate_Practice_Cover", new Vector3(-4.5f, 0.73f, 0f), new Vector3(0.9f, 0.9f, 0.9f), crateMat);
            CreateCheckpoint(geoGroup.transform, "Checkpoint_Start", new Vector3(-8.5f, 0.88f, 0f), new Vector3(2.5f, 3f, 4f), 0);

            // Hazard Zone beneath first gap (between X = -3 and X = -1)
            GameObject pitHazard1 = new GameObject("Pit_Hazard_Gap1");
            pitHazard1.transform.SetParent(geoGroup.transform);
            pitHazard1.transform.position = new Vector3(-2.0f, -2.5f, 0f);
            BoxCollider pitCol1 = pitHazard1.AddComponent<BoxCollider>();
            pitCol1.isTrigger = true;
            pitCol1.size = new Vector3(2.2f, 3.5f, 3.5f);
            pitHazard1.AddComponent<PitHazardZone>();

            // Section 2: Low Stepping Platform (X: -1 to 3, Top Y: 0)
            CreateMeadowPlatform(geoGroup.transform, "Platform_Low", new Vector3(1f, -2.0f, 0f), new Vector3(4f, 4.0f, 3f), groundMat, meadowMat);

            // Section 3: Cliff Wall with Ledge (X: 3 to 11, Top Y: 3.2)
            CreateMeadowPlatform(geoGroup.transform, "Platform_High_Ledge", new Vector3(7.0f, -0.4f, 0f), new Vector3(8.0f, 7.2f, 3f), groundMat, meadowMat);
            CreateCrate(geoGroup.transform, "Crate_High_Platform", new Vector3(9.5f, 3.93f, 0f), new Vector3(0.9f, 0.9f, 0.9f), crateMat);
            CreateCheckpoint(geoGroup.transform, "Checkpoint_LedgeClimb", new Vector3(7.0f, 4.08f, 0f), new Vector3(2.5f, 3f, 4f), 1);

            // Section 4: Physics Rope & Pit (Gap between X: 11 and X: 16)
            // Non-blocking Overhead Timber Crossbeam (Recessed to Z = 0.6, Collider removed, purely overhead mounting visual)
            GameObject overheadBeam = CreateBlock(geoGroup.transform, "Overhead_Rope_Timber_Beam", new Vector3(13.5f, 8.5f, 0.6f), new Vector3(6.5f, 0.45f, 0.6f), woodMat);
            DestroyImmediate(overheadBeam.GetComponent<Collider>());

            // Free-hanging Rope
            GameObject ropeGO = new GameObject("Physics_Rope");
            ropeGO.transform.SetParent(geoGroup.transform);
            ropeGO.transform.position = new Vector3(13.5f, 8.3f, 0f);
            RopeController ropeCtrl = ropeGO.AddComponent<RopeController>();
            ropeCtrl.BuildRope();

            // Pit floor beneath rope
            CreateBlock(geoGroup.transform, "Pit_Floor", new Vector3(13.5f, -5.0f, 0f), new Vector3(7.0f, 2.0f, 3f), groundMat);

            // Pit Hazard Trigger Zone (guarantees instantaneous respawn upon touching pit floor)
            GameObject pitHazard2 = new GameObject("Pit_Hazard_RopePit");
            pitHazard2.transform.SetParent(geoGroup.transform);
            pitHazard2.transform.position = new Vector3(13.5f, -3.2f, 0f);
            BoxCollider pitCol2 = pitHazard2.AddComponent<BoxCollider>();
            pitCol2.isTrigger = true;
            pitCol2.size = new Vector3(6.8f, 3.5f, 3.5f);
            pitHazard2.AddComponent<PitHazardZone>();

            // Section 5: High Plateau with Planet of Lana Depth Stealth Setup (X: 16 to 30, Top Y: 4.2)
            CreateMeadowPlatform(geoGroup.transform, "Platform_Enemy_Plateau", new Vector3(23.0f, -0.4f, 0f), new Vector3(14.0f, 9.2f, 3f), groundMat, meadowMat);
            CreateCheckpoint(geoGroup.transform, "Checkpoint_Plateau", new Vector3(16.5f, 4.8f, 0f), new Vector3(2.0f, 3f, 4f), 2);

            // Left Cover: Tall Stealth Reeds + Crate (Safe hide spot 1)
            CreateStealthGrassHideZone(geoGroup.transform, "Bush_Stealth_Left", new Vector3(18.2f, 4.48f, 0f), 2.2f, bushMat);
            CreateCrate(geoGroup.transform, "Crate_Cover_Left", new Vector3(17.2f, 4.95f, 0f), new Vector3(0.95f, 0.95f, 0.95f), crateMat);

            // Open Exposed Gap between X = 19.5 and X = 23.8 (Short trampled grass, no cover!)

            // Right Cover: Tall Stealth Reeds + Crate (Safe hide spot 2)
            CreateStealthGrassHideZone(geoGroup.transform, "Bush_Stealth_Right", new Vector3(25.0f, 4.48f, 0f), 2.2f, bushMat);
            CreateCrate(geoGroup.transform, "Crate_Cover_Right", new Vector3(26.0f, 4.95f, 0f), new Vector3(0.95f, 0.95f, 0.95f), crateMat);

            // Plateau Exit Cover before Bridge (Safe hide spot 3)
            CreateStealthGrassHideZone(geoGroup.transform, "Bush_Stealth_BridgeExit", new Vector3(29.6f, 4.48f, 0f), 1.8f, bushMat);
            CreateCheckpoint(geoGroup.transform, "Checkpoint_Bridge", new Vector3(29.5f, 5.1f, 0f), new Vector3(2.0f, 3f, 4f), 3);

            // Midground Perch Rock for Depth Watcher Enemy (built using Rock0022)
            GameObject perchRock = SpawnRock(geoGroup.transform, "Perch_Rock_Midground", new Vector3(21.5f, 2.2f, 1.8f), new Vector3(8f, 110f, -5f), new Vector3(3.2f, 4.4f, 2.2f), groundMat);

            // Section 6: Bridge & Gap (X: 30 to 34)
            CreateBlock(geoGroup.transform, "Bridge_Walkway", new Vector3(32.0f, 4.22f, 0f), new Vector3(4.0f, 0.3f, 2.5f), woodMat);

            // Section 7: Outpost Threshold & 3D Refugee Shelter Sanctuary Room (NieR: Automata Style 2D/3D Hybrid)
            CreateRefugeeShelter3D(geoGroup.transform, woodMat, floorMat, bambooMat, blanketMat, brickMat, flagMat, goldStarMat, lanternMat, ironMat, crateMat, playerMat);

            // ==========================================
            // 5.5. ROCK ENVIRONMENT FORMATIONS (Built using Rock0022.fbx)
            // ==========================================
            CreateRockEnvironment(geoGroup.transform, groundMat);

            // ==========================================
            // 6. VOLUMETRIC 3D PAINTERLY GRASS MEADOWS (MeshRenderer + 3D Wind Sway + Player Push)
            // Replaces legacy procedural spiky grass with authentic 3D painterly tufts (Planet of Lana aesthetic)
            // ==========================================
            GameObject meadowGroup = new GameObject("Stylized_3D_Grass_Meadows");
            meadowGroup.transform.SetParent(geoGroup.transform);

            CreateStylizedMeadowZone(meadowGroup.transform, "Meadow_Start",         new Vector3(-7.0f, 0.02f, 0f), 8.0f);
            CreateStylizedMeadowZone(meadowGroup.transform, "Meadow_Low",           new Vector3( 1.0f, 0.02f, 0f), 4.2f);
            CreateStylizedMeadowZone(meadowGroup.transform, "Meadow_HighLedge",     new Vector3( 7.0f, 3.22f, 0f), 8.0f);
            CreateStylizedMeadowZone(meadowGroup.transform, "Meadow_Plateau_Entry", new Vector3(16.5f, 4.22f, 0f), 2.4f);
            CreateStylizedMeadowZone(meadowGroup.transform, "Meadow_Plateau_Mid",   new Vector3(21.5f, 4.22f, 0f), 6.0f);
            CreateStylizedMeadowZone(meadowGroup.transform, "Meadow_Plateau_Exit",  new Vector3(28.0f, 4.22f, 0f), 3.8f);

            // ==========================================
            // 7. PLAYER CHARACTER (MINH) & COMPANION (AN)
            // ==========================================
            GameObject playerGroup = new GameObject("Player");
            playerGroup.transform.SetParent(rootGO.transform);

            GameObject playerGO = new GameObject("Player_Minh");
            playerGO.transform.SetParent(playerGroup.transform);
            playerGO.transform.position = new Vector3(-8.5f, 0.02f, 0f);

            CharacterController cc = playerGO.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.32f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.stepOffset = 0.35f;

            // Visual hierarchy for Minh (centered at Y = 0.90m matching CharacterController center)
            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(playerGO.transform);
            visual.transform.localPosition = new Vector3(0f, 0.9f, 0f);

            // Boots (Cube: centered at local Y = -0.75m, height = 0.30m -> bottom touches local Y = -0.90m, exactly at player root Y = 0.00m)
            GameObject boots = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boots.name = "Boots";
            boots.transform.SetParent(visual.transform);
            boots.transform.localPosition = new Vector3(0f, -0.75f, 0f);
            boots.transform.localScale = new Vector3(0.46f, 0.30f, 0.40f);
            boots.GetComponent<MeshRenderer>().sharedMaterial = ironMat;
            DestroyImmediate(boots.GetComponent<Collider>());

            // Torso / Vietnamese Indigo Peasant Tunic (Capsule: centered at local Y = -0.15m, height = 0.75m -> spans -0.525m to +0.225m)
            GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            torso.name = "Torso";
            torso.transform.SetParent(visual.transform);
            torso.transform.localPosition = new Vector3(0f, -0.15f, 0f);
            torso.transform.localScale = new Vector3(0.58f, 0.75f, 0.45f);
            torso.GetComponent<MeshRenderer>().sharedMaterial = playerMat;
            DestroyImmediate(torso.GetComponent<Collider>());

            // Flowing Red Cloth Scarf / Pennant trailing behind Minh (at neckline local Y = 0.22m)
            GameObject scarf = GameObject.CreatePrimitive(PrimitiveType.Cube);
            scarf.name = "Red_Scarf_Ribbon";
            scarf.transform.SetParent(visual.transform);
            scarf.transform.localPosition = new Vector3(-0.25f, 0.22f, 0f);
            scarf.transform.localRotation = Quaternion.Euler(0f, 0f, 25f);
            scarf.transform.localScale = new Vector3(0.55f, 0.12f, 0.04f);
            scarf.GetComponent<MeshRenderer>().sharedMaterial = flagMat;
            DestroyImmediate(scarf.GetComponent<Collider>());
            scarf.AddComponent<WindClothFlutter>();

            // Head (Sphere: centered at local Y = 0.45m, diameter = 0.38m -> rests neatly on torso neckline)
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(visual.transform);
            head.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            head.transform.localScale = new Vector3(0.38f, 0.38f, 0.38f);
            head.GetComponent<MeshRenderer>().sharedMaterial = skinMat;
            DestroyImmediate(head.GetComponent<Collider>());

            // Headband (Khăn Rằn peasant headband at forehead local Y = 0.48m)
            GameObject headband = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            headband.name = "Headband";
            headband.transform.SetParent(visual.transform);
            headband.transform.localPosition = new Vector3(0f, 0.48f, 0f);
            headband.transform.localScale = new Vector3(0.40f, 0.05f, 0.40f);
            headband.GetComponent<MeshRenderer>().sharedMaterial = bambooMat;
            DestroyImmediate(headband.GetComponent<Collider>());

            playerGO.AddComponent<LedgeGrabDetector>();
            PlayerController playerCtrl = playerGO.AddComponent<PlayerController>();
            playerGO.AddComponent<GrassInteraction>();

            // Companion Creature "An" (inspired by Mui in Planet of Lana)
            GameObject companionGO = new GameObject("Player_Companion");
            companionGO.transform.SetParent(playerGroup.transform);
            companionGO.transform.position = new Vector3(-9.75f, 0.02f, 0f);

            CharacterController compCC = companionGO.AddComponent<CharacterController>();
            compCC.height = 0.52f;
            compCC.radius = 0.26f;
            compCC.center = new Vector3(0f, 0.26f, 0f);
            compCC.stepOffset = 0.3f;

            GameObject compVisual = new GameObject("Visual");
            compVisual.transform.SetParent(companionGO.transform);
            compVisual.transform.localPosition = Vector3.zero;

            // Rounded body (resting flush on turf)
            GameObject compBody = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            compBody.name = "Body";
            compBody.transform.SetParent(compVisual.transform);
            compBody.transform.localPosition = new Vector3(0f, 0.24f, 0f);
            compBody.transform.localScale = new Vector3(0.52f, 0.48f, 0.50f);
            compBody.GetComponent<MeshRenderer>().sharedMaterial = companionMat;
            DestroyImmediate(compBody.GetComponent<Collider>());

            // Glowing Eyes (turquiose / teal unlit glow)
            GameObject eyeL = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eyeL.name = "Eye_L";
            eyeL.transform.SetParent(compVisual.transform);
            eyeL.transform.localPosition = new Vector3(0.18f, 0.30f, 0.15f);
            eyeL.transform.localScale = new Vector3(0.12f, 0.12f, 0.08f);
            eyeL.GetComponent<MeshRenderer>().sharedMaterial = eyeGlowMat;
            DestroyImmediate(eyeL.GetComponent<Collider>());

            GameObject eyeR = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eyeR.name = "Eye_R";
            eyeR.transform.SetParent(compVisual.transform);
            eyeR.transform.localPosition = new Vector3(0.18f, 0.30f, -0.15f);
            eyeR.transform.localScale = new Vector3(0.12f, 0.12f, 0.08f);
            eyeR.GetComponent<MeshRenderer>().sharedMaterial = eyeGlowMat;
            DestroyImmediate(eyeR.GetComponent<Collider>());

            // Ears / Small antennae
            GameObject earL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            earL.name = "Ear_L";
            earL.transform.SetParent(compVisual.transform);
            earL.transform.localPosition = new Vector3(-0.05f, 0.45f, 0.15f);
            earL.transform.localRotation = Quaternion.Euler(0f, 0f, -25f);
            earL.transform.localScale = new Vector3(0.08f, 0.16f, 0.08f);
            earL.GetComponent<MeshRenderer>().sharedMaterial = companionMat;
            DestroyImmediate(earL.GetComponent<Collider>());

            GameObject earR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            earR.name = "Ear_R";
            earR.transform.SetParent(compVisual.transform);
            earR.transform.localPosition = new Vector3(-0.05f, 0.45f, -0.15f);
            earR.transform.localRotation = Quaternion.Euler(0f, 0f, -25f);
            earR.transform.localScale = new Vector3(0.08f, 0.16f, 0.08f);
            earR.GetComponent<MeshRenderer>().sharedMaterial = companionMat;
            DestroyImmediate(earR.GetComponent<Collider>());

            CompanionController compCtrl = companionGO.AddComponent<CompanionController>();
            compCtrl.SetTarget(playerGO.transform);
            companionGO.AddComponent<GrassInteraction>();

            // Wire Camera target directly to Minh and configure level bounds
            if (camCtrl != null)
            {
                camCtrl.ConfigureLevelBounds(-15.0f, 58.0f, 1.0f, false);
                camCtrl.Target = playerGO.transform;
                camCtrl.SnapToTarget();
            }

            // ==========================================
            // 8. ENEMIES (DEPTH WATCHER & PATROL UNIT)
            // ==========================================
            GameObject enemyGroup = new GameObject("Enemies");
            enemyGroup.transform.SetParent(rootGO.transform);

            // Planet of Lana Depth Watcher overlooking the open gap
            CreateDepthWatcherEnemy(enemyGroup.transform, new Vector3(21.5f, 4.35f, 1.8f), enemyMat, coneMat);

            // Secondary Patrol Enemy at Bridge Approach (Patrols bridge before the refugee outpost)
            CreatePatrolEnemy(enemyGroup.transform, new Vector3(32.5f, 4.25f, 0f), new float[] { 30.5f, 34.0f }, enemyMat, coneMat);

            // Hook references in CheckpointManager
            cpMgr.FindReferences();

#if UNITY_EDITOR
            if (saveScene)
            {
                var scene = EditorSceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("<color=#40b8a6>[Hoa và Máu]</color> Graybox Level successfully saved to: " + scene.path);
            }
#endif
        }

        private GameObject CreateDepthWatcherEnemy(Transform parent, Vector3 pos, Material mat, Material coneMat)
        {
            GameObject enemyGO = new GameObject("Enemy_Depth_Watcher");
            enemyGO.transform.SetParent(parent);
            enemyGO.transform.position = pos;

            GameObject enemyVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemyVisual.name = "Visual";
            enemyVisual.transform.SetParent(enemyGO.transform);
            enemyVisual.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            enemyVisual.transform.localScale = new Vector3(0.68f, 0.95f, 0.68f);
            enemyVisual.GetComponent<MeshRenderer>().sharedMaterial = mat;
            DestroyImmediate(enemyVisual.GetComponent<Collider>());

            CapsuleCollider enemyCol = enemyGO.AddComponent<CapsuleCollider>();
            enemyCol.height = 1.9f;
            enemyCol.radius = 0.35f;
            enemyCol.center = new Vector3(0f, 0.95f, 0f);

            GameObject coneGO = new GameObject("Vision_Cone");
            coneGO.transform.SetParent(enemyGO.transform);
            coneGO.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            VisionConeMesh coneMesh = coneGO.AddComponent<VisionConeMesh>();
            coneGO.GetComponent<MeshRenderer>().sharedMaterial = coneMat;

            GameObject alertTextGO = new GameObject("Alert_Indicator");
            alertTextGO.transform.SetParent(enemyGO.transform);
            alertTextGO.transform.localPosition = new Vector3(0f, 2.3f, 0f);
            TextMesh tm = alertTextGO.AddComponent<TextMesh>();
            tm.fontSize = 36;
            tm.characterSize = 0.08f;
            tm.alignment = TextAlignment.Center;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.fontStyle = FontStyle.Bold;

            EnemyController enemyCtrl = enemyGO.AddComponent<EnemyController>();
            SetPrivateField(enemyCtrl, "isDepthWatcher", true);
            SetPrivateField(enemyCtrl, "watchScreenDuration", 4.2f);
            SetPrivateField(enemyCtrl, "lookBackDuration", 3.2f);
            SetPrivateField(enemyCtrl, "viewDistance", 9.0f);
            SetPrivateField(enemyCtrl, "viewAngle", 55.0f);
            SetPrivateField(enemyCtrl, "visionCone", coneMesh);
            SetPrivateField(enemyCtrl, "visualTransform", enemyVisual.transform);
            SetPrivateField(enemyCtrl, "alertIndicator", tm);

            return enemyGO;
        }

        private GameObject CreatePatrolEnemy(Transform parent, Vector3 pos, float[] waypoints, Material mat, Material coneMat)
        {
            GameObject enemyGO = new GameObject("Enemy_Bridge_Patrol");
            enemyGO.transform.SetParent(parent);
            enemyGO.transform.position = pos;

            GameObject enemyVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemyVisual.name = "Visual";
            enemyVisual.transform.SetParent(enemyGO.transform);
            enemyVisual.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            enemyVisual.transform.localScale = new Vector3(0.68f, 0.95f, 0.68f);
            enemyVisual.GetComponent<MeshRenderer>().sharedMaterial = mat;
            DestroyImmediate(enemyVisual.GetComponent<Collider>());

            CapsuleCollider enemyCol = enemyGO.AddComponent<CapsuleCollider>();
            enemyCol.height = 1.9f;
            enemyCol.radius = 0.35f;
            enemyCol.center = new Vector3(0f, 0.95f, 0f);

            GameObject coneGO = new GameObject("Vision_Cone");
            coneGO.transform.SetParent(enemyGO.transform);
            coneGO.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            VisionConeMesh coneMesh = coneGO.AddComponent<VisionConeMesh>();
            coneGO.GetComponent<MeshRenderer>().sharedMaterial = coneMat;

            GameObject alertTextGO = new GameObject("Alert_Indicator");
            alertTextGO.transform.SetParent(enemyGO.transform);
            alertTextGO.transform.localPosition = new Vector3(0f, 2.3f, 0f);
            TextMesh tm = alertTextGO.AddComponent<TextMesh>();
            tm.fontSize = 36;
            tm.characterSize = 0.08f;
            tm.alignment = TextAlignment.Center;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.fontStyle = FontStyle.Bold;

            EnemyController enemyCtrl = enemyGO.AddComponent<EnemyController>();
            SetPrivateField(enemyCtrl, "isDepthWatcher", false);
            SetPrivateField(enemyCtrl, "patrolXPositions", waypoints);
            SetPrivateField(enemyCtrl, "visionCone", coneMesh);
            SetPrivateField(enemyCtrl, "visualTransform", enemyVisual.transform);
            SetPrivateField(enemyCtrl, "alertIndicator", tm);

            return enemyGO;
        }

        private GameObject CreateCheckpoint(Transform parent, string name, Vector3 pos, Vector3 size, int index = 0)
        {
            GameObject cpGO = new GameObject(name);
            cpGO.transform.SetParent(parent);
            cpGO.transform.position = pos;

            BoxCollider col = cpGO.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = size;

            CheckpointZone cpz = cpGO.AddComponent<CheckpointZone>();
            cpz.checkpointIndex = index;
            return cpGO;
        }

        private GameObject CreateMeadowPlatform(Transform parent, string name, Vector3 pos, Vector3 size, Material cliffMat, Material turfMat)
        {
            GameObject platform = CreateBlock(parent, name, pos, size, cliffMat);

            float capHeight = 0.18f;
            Vector3 capPos = new Vector3(pos.x, pos.y + (size.y * 0.5f) - (capHeight * 0.5f) + 0.01f, pos.z);
            Vector3 capSize = new Vector3(size.x + 0.08f, capHeight, size.z + 0.14f);

            GameObject cap = CreateBlock(parent, $"{name}_Turf_Cap", capPos, capSize, turfMat);
            DestroyImmediate(cap.GetComponent<Collider>()); // Base platform handles physical collision

            return platform;
        }

        private GameObject CreateStealthGrassHideZone(Transform parent, string name, Vector3 pos, float width, Material bushBaseMat)
        {
            GameObject hideZoneGO = new GameObject(name);
            hideZoneGO.transform.SetParent(parent);
            hideZoneGO.transform.position = pos;

            BoxCollider col = hideZoneGO.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(width, 2.2f, 1.8f);
            col.center = new Vector3(0f, 1.1f, 0f);
            hideZoneGO.AddComponent<BushHideZone>();

            // Soft grounding foliage clump (dark green core)
            GameObject backdrop = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            backdrop.name = "Foliage_Core";
            backdrop.transform.SetParent(hideZoneGO.transform);
            backdrop.transform.localPosition = new Vector3(0f, 0.85f, 0.15f);
            backdrop.transform.localScale = new Vector3(width * 0.75f, 0.65f, 0.75f);
            backdrop.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            backdrop.GetComponent<MeshRenderer>().sharedMaterial = bushBaseMat;
            DestroyImmediate(backdrop.GetComponent<Collider>());

            // Dense, chest-high painterly 3D tufts for hiding (Planet of Lana aesthetic)
            GameObject reedsGO = new GameObject($"{name}_Reeds");
            reedsGO.transform.SetParent(hideZoneGO.transform);
            reedsGO.transform.localPosition = Vector3.zero;

            int tuftCount = Mathf.Max(3, Mathf.RoundToInt(width * 3.5f));
            float step = width / (float)tuftCount;
            float startX = -width * 0.5f + step * 0.5f;
            for (int i = 0; i < tuftCount; i++)
            {
                float tX = startX + i * step + Random.Range(-0.08f, 0.08f);
                float tZ = Random.Range(-0.25f, 0.25f);
                TuftMeshStyle style = (i % 2 == 0) ? TuftMeshStyle.CurvedFan : TuftMeshStyle.ClusteredPatch;
                TuftTextureVariant variant = (i % 3 == 0) ? TuftTextureVariant.Leaning : TuftTextureVariant.Upright;
                float tuftW = Random.Range(1.20f, 1.45f);
                float tuftH = Random.Range(1.50f, 1.80f);
                CreateStylized3DGrassTuft(reedsGO.transform, $"{name}_ReedTuft_{i:D2}", new Vector3(tX, 0f, tZ), style, variant, tuftW, tuftH);
            }

            return hideZoneGO;
        }

        private GameObject CreateStylizedMeadowZone(Transform parent, string name, Vector3 pos, float width)
        {
            GameObject meadowRoot = new GameObject(name);
            meadowRoot.transform.SetParent(parent);
            meadowRoot.transform.position = pos;

            // Density: approx 1.6 tufts per meter per layer, across 3 distinct depth layers (Back, Mid, Front)
            // Layer 1: Back (Z = +0.25 to +0.50m) - background painterly silhouettes
            // Layer 2: Middle (Z = -0.08 to +0.10m) - direct contact layer underfoot
            // Layer 3: Front (Z = -0.30 to -0.60m) - foreground framing tufts
            int count = Mathf.Max(3, Mathf.RoundToInt(width * 1.5f));
            float step = width / (float)count;
            float startX = -width * 0.5f + step * 0.5f;

            for (int i = 0; i < count; i++)
            {
                float localX = startX + i * step + Random.Range(-step * 0.22f, step * 0.22f);

                // 1. Back tuft
                TuftMeshStyle backStyle = (i % 3 == 0) ? TuftMeshStyle.StarCards : ((i % 3 == 1) ? TuftMeshStyle.CurvedFan : TuftMeshStyle.ClusteredPatch);
                TuftTextureVariant backVar = (i % 2 == 0) ? TuftTextureVariant.Upright : TuftTextureVariant.Leaning;
                float backZ = Random.Range(0.25f, 0.50f);
                float backH = Random.Range(0.90f, 1.25f);
                float backW = Random.Range(1.10f, 1.40f);
                CreateStylized3DGrassTuft(meadowRoot.transform, $"{name}_Back_{i:D2}", new Vector3(localX + Random.Range(-0.08f, 0.08f), 0f, backZ), backStyle, backVar, backW, backH);

                // 2. Middle tuft (main interactive contact)
                TuftMeshStyle midStyle = (i % 3 == 1) ? TuftMeshStyle.StarCards : ((i % 3 == 2) ? TuftMeshStyle.CurvedFan : TuftMeshStyle.ClusteredPatch);
                TuftTextureVariant midVar = (i % 2 == 1) ? TuftTextureVariant.Upright : TuftTextureVariant.Leaning;
                float midZ = Random.Range(-0.08f, 0.10f);
                float midH = Random.Range(0.85f, 1.15f);
                float midW = Random.Range(1.05f, 1.35f);
                CreateStylized3DGrassTuft(meadowRoot.transform, $"{name}_Mid_{i:D2}", new Vector3(localX, 0f, midZ), midStyle, midVar, midW, midH);

                // 3. Front framing tuft (every 2nd step to give natural rhythm)
                if (i % 2 == 0)
                {
                    TuftMeshStyle frontStyle = TuftMeshStyle.CurvedFan;
                    TuftTextureVariant frontVar = TuftTextureVariant.Leaning;
                    float frontZ = Random.Range(-0.35f, -0.60f);
                    float frontH = Random.Range(0.95f, 1.30f);
                    float frontW = Random.Range(1.15f, 1.45f);
                    CreateStylized3DGrassTuft(meadowRoot.transform, $"{name}_Front_{i:D2}", new Vector3(localX + 0.12f, 0f, frontZ), frontStyle, frontVar, frontW, frontH);
                }
            }

            return meadowRoot;
        }

        private GameObject CreateStylized3DGrassTuft(Transform parent, string name, Vector3 pos, TuftMeshStyle style, TuftTextureVariant variant, float width, float height)
        {
            GameObject tuftGO = new GameObject(name);
            tuftGO.transform.SetParent(parent);
            // Every caller supplies a position relative to its meadow or hide-zone.  Assigning
            // it in world space drops raised-platform grass to the origin, which looks like
            // floating debris around the player.  Keeping the local anchor also makes the
            // mesh root and the platform surface move together.
            tuftGO.transform.localPosition = pos;
            tuftGO.transform.localRotation = Quaternion.identity;
            tuftGO.transform.localScale = Vector3.one;
            Stylized3DGrassTuft tuft = tuftGO.AddComponent<Stylized3DGrassTuft>();
            tuft.Setup(style, variant, width, height);
            return tuftGO;
        }

        public GameObject SpawnRock(Transform parent, string name, Vector3 pos, Vector3 rotEuler, Vector3 scale, Material rockMat = null, bool addCollider = true)
        {
            GameObject rockInstance = null;
#if UNITY_EDITOR
            GameObject rockPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Rock0022.fbx");
            if (rockPrefab != null)
            {
                rockInstance = Object.Instantiate(rockPrefab, parent);
            }
#endif
            if (rockInstance == null)
            {
                rockInstance = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rockInstance.transform.SetParent(parent);
            }

            rockInstance.name = name;
            rockInstance.transform.position = pos;
            rockInstance.transform.rotation = Quaternion.Euler(rotEuler);
            rockInstance.transform.localScale = scale;

            if (rockMat != null)
            {
                foreach (var rend in rockInstance.GetComponentsInChildren<Renderer>())
                {
                    rend.sharedMaterial = rockMat;
                }
            }

            if (addCollider)
            {
                MeshFilter mf = rockInstance.GetComponentInChildren<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    MeshCollider mc = mf.gameObject.GetComponent<MeshCollider>();
                    if (mc == null) mc = mf.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                }
                else if (rockInstance.GetComponent<Collider>() == null)
                {
                    rockInstance.AddComponent<BoxCollider>();
                }
            }

            return rockInstance;
        }

        private void CreateRockEnvironment(Transform parent, Material rockMat)
        {
            GameObject rocksRoot = new GameObject("Rock_Environment_Formations");
            rocksRoot.transform.SetParent(parent);

            // 1. Platform Start Ground Accents & Meadow Borders
            SpawnRock(rocksRoot.transform, "Rock_Start_Embedded_01", new Vector3(-9.5f, 0.12f, 0.35f), new Vector3(15f, 40f, -10f), new Vector3(0.85f, 0.70f, 0.90f), rockMat);
            SpawnRock(rocksRoot.transform, "Rock_Start_Embedded_02", new Vector3(-4.2f, 0.18f, -0.30f), new Vector3(-20f, 130f, 15f), new Vector3(1.10f, 0.85f, 0.95f), rockMat);
            SpawnRock(rocksRoot.transform, "Rock_Start_Edge_Crag",   new Vector3(-3.1f, -0.15f, 0.15f), new Vector3(45f, 210f, -25f), new Vector3(1.35f, 1.15f, 1.25f), rockMat);

            // 2. Chasm Gap 1 Stepping Foundation & Deep Ravine Rocks
            SpawnRock(rocksRoot.transform, "Rock_Gap1_SteppingStone", new Vector3(-2.0f, -0.55f, 0.05f), new Vector3(10f, 75f, 5f), new Vector3(1.6f, 1.4f, 1.4f), rockMat);
            SpawnRock(rocksRoot.transform, "Rock_Gap1_DeepBury",      new Vector3(-1.8f, -2.6f, 0.35f), new Vector3(-35f, 15f, 40f), new Vector3(2.2f, 2.8f, 2.0f), rockMat, false);

            // 3. Low Platform Foot Stones
            SpawnRock(rocksRoot.transform, "Rock_Low_Foot_01", new Vector3(2.4f, 0.20f, 0.25f), new Vector3(5f, 160f, -12f), new Vector3(0.95f, 0.80f, 0.90f), rockMat);

            // 4. Cliff Climb Wall Formations (Fusing Rock0022 variations for a natural climbing cliff)
            SpawnRock(rocksRoot.transform, "Rock_Cliff_Buttress_Base", new Vector3(3.2f, 0.75f, 0.20f), new Vector3(12f, 85f, -8f), new Vector3(2.1f, 2.3f, 1.7f), rockMat);
            SpawnRock(rocksRoot.transform, "Rock_Cliff_Ledge_Ascent",  new Vector3(3.4f, 2.15f, -0.15f), new Vector3(-18f, 215f, 22f), new Vector3(1.85f, 2.1f, 1.6f), rockMat);
            SpawnRock(rocksRoot.transform, "Rock_Cliff_Top_Overhang",  new Vector3(3.1f, 3.15f, 0.25f), new Vector3(30f, -45f, 15f), new Vector3(1.4f, 1.25f, 1.5f), rockMat);

            // 5. High Ledge & Rope Pit Chasm Ravine
            SpawnRock(rocksRoot.transform, "Rock_HighLedge_Corner", new Vector3(10.7f, 3.35f, 0.20f), new Vector3(-10f, 50f, 20f), new Vector3(1.35f, 1.2f, 1.15f), rockMat);
            SpawnRock(rocksRoot.transform, "Rock_Pit_Ravine_Left",  new Vector3(12.2f, -3.9f, 0.20f), new Vector3(25f, 65f, -15f), new Vector3(2.6f, 3.2f, 2.4f), rockMat, false);
            SpawnRock(rocksRoot.transform, "Rock_Pit_Ravine_Right", new Vector3(14.8f, -3.7f, -0.25f), new Vector3(-15f, 175f, 30f), new Vector3(2.8f, 3.6f, 2.5f), rockMat, false);
            SpawnRock(rocksRoot.transform, "Rock_Pit_SteppingCrag", new Vector3(13.5f, -2.6f, 0.05f), new Vector3(5f, 290f, -10f), new Vector3(1.7f, 1.9f, 1.6f), rockMat);

            // 6. High Plateau Wall & Plateau Scenery
            SpawnRock(rocksRoot.transform, "Rock_Plateau_Ascent_Wall", new Vector3(16.1f, 2.1f, 0.10f), new Vector3(-12f, 140f, 18f), new Vector3(2.2f, 4.2f, 1.9f), rockMat);
            SpawnRock(rocksRoot.transform, "Rock_Plateau_CoverCrag",   new Vector3(22.8f, 4.42f, 0.35f), new Vector3(-25f, 200f, 15f), new Vector3(1.2f, 0.95f, 1.05f), rockMat);

            // 7. Bridge Anchors & Threshold
            SpawnRock(rocksRoot.transform, "Rock_Bridge_Anchor_Left",  new Vector3(29.8f, 4.05f, 0.25f), new Vector3(15f, 70f, -10f), new Vector3(1.4f, 1.3f, 1.25f), rockMat);
            SpawnRock(rocksRoot.transform, "Rock_Bridge_Anchor_Right", new Vector3(34.2f, 4.05f, -0.20f), new Vector3(-20f, 250f, 15f), new Vector3(1.5f, 1.35f, 1.3f), rockMat);
            SpawnRock(rocksRoot.transform, "Rock_Shelter_Arch",        new Vector3(34.8f, 6.2f, 0.40f), new Vector3(35f, 150f, -20f), new Vector3(2.2f, 2.5f, 1.9f), rockMat, false);

            // 8. Distant Karst Silhouette Pillars (Painterly Parallax Depth, Z = 12 to 20)
            SpawnRock(rocksRoot.transform, "Karst_Spire_FarLeft",  new Vector3(-6.5f, 8.5f, 16.0f), new Vector3(6f, 70f, 0f), new Vector3(5.5f, 14.0f, 4.5f), rockMat, false);
            SpawnRock(rocksRoot.transform, "Karst_Spire_Mid",      new Vector3(11.0f, 11.0f, 22.0f), new Vector3(-8f, 195f, 5f), new Vector3(8.0f, 18.0f, 6.0f), rockMat, false);
            SpawnRock(rocksRoot.transform, "Karst_Spire_FarRight", new Vector3(28.5f, 10.0f, 18.0f), new Vector3(10f, 285f, -8f), new Vector3(7.0f, 15.5f, 5.5f), rockMat, false);
        }

        private GameObject CreateSwayingTree(Transform parent, string name, Vector3 pos, Material trunkMat, Material canopyMat)
        {
            GameObject treeGO = new GameObject(name);
            treeGO.transform.SetParent(parent);
            treeGO.transform.position = pos;

            // Trunk
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(treeGO.transform);
            trunk.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            trunk.transform.localScale = new Vector3(0.45f, 2.2f, 0.45f);
            trunk.GetComponent<MeshRenderer>().sharedMaterial = trunkMat;
            DestroyImmediate(trunk.GetComponent<Collider>());

            // Canopy
            GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            canopy.name = "Canopy";
            canopy.transform.SetParent(treeGO.transform);
            canopy.transform.localPosition = new Vector3(0f, 4.5f, 0f);
            canopy.transform.localScale = new Vector3(3.5f, 2.8f, 3.2f);
            canopy.GetComponent<MeshRenderer>().sharedMaterial = canopyMat;
            DestroyImmediate(canopy.GetComponent<Collider>());

            treeGO.AddComponent<SwayingTree>();
            return treeGO;
        }

        private GameObject CreateAtmosphericPollen(Transform parent)
        {
            GameObject pollenGO = new GameObject("Atmospheric_Pollen_Particles");
            pollenGO.transform.SetParent(parent);
            pollenGO.transform.position = new Vector3(15f, 3.5f, -1.0f);

            ParticleSystem ps = pollenGO.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.startLifetime = 12f;
            main.startSpeed = 0.35f;
            main.startSize = 0.08f;
            main.maxParticles = 65;
            main.startColor = new Color(0.95f, 0.98f, 0.85f, 0.45f);

            var emission = ps.emission;
            emission.rateOverTime = 6f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(50f, 10f, 6f);

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.x = new ParticleSystem.MinMaxCurve(0.4f, 0.85f);
            vel.y = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);
            vel.z = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);

            ParticleSystemRenderer rend = pollenGO.GetComponent<ParticleSystemRenderer>();
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlitShader != null)
            {
                Material pollenMat = new Material(unlitShader) { name = "Mat_Pollen_Soft" };
                pollenMat.color = new Color(0.95f, 0.98f, 0.85f, 0.45f);
                rend.sharedMaterial = pollenMat;
            }
            return pollenGO;
        }

        private GameObject CreateBlock(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent);
            cube.transform.position = pos;
            cube.transform.localScale = size;
            cube.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return cube;
        }

        private GameObject CreateCrate(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
        {
            GameObject crate = CreateBlock(parent, name, pos, size, mat);
            return crate;
        }

        private GameObject CreateBush(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
        {
            GameObject bush = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bush.name = name;
            bush.transform.SetParent(parent);
            bush.transform.position = pos;
            bush.transform.localScale = size;
            bush.GetComponent<MeshRenderer>().sharedMaterial = mat;

            BoxCollider col = bush.GetComponent<BoxCollider>();
            col.isTrigger = true;
            bush.AddComponent<BushHideZone>();

            return bush;
        }

        private void CreateRefugeeShelter3D(Transform parent, Material woodMat, Material floorMat, Material bambooMat, Material blanketMat, Material brickMat, Material flagMat, Material goldStarMat, Material lanternMat, Material ironMat, Material crateMat, Material playerMat)
        {
            GameObject shelterRoot = new GameObject("3D_Refugee_Shelter_Sanctuary");
            shelterRoot.transform.SetParent(parent);

            // 1. OUTPOST APPROACH & ENTRANCE ARCHWAY (Threshold at X = 35.2)
            // Approach ramp connecting bridge (X: 34 to 35.2)
            CreateBlock(shelterRoot.transform, "Approach_Platform", new Vector3(34.6f, 4.05f, 0f), new Vector3(1.4f, 0.4f, 3.2f), woodMat);

            // Timber Archway Posts & Lintel Beam
            CreateBlock(shelterRoot.transform, "Arch_Post_South", new Vector3(35.2f, 5.8f, -1.8f), new Vector3(0.5f, 3.6f, 0.5f), woodMat);
            CreateBlock(shelterRoot.transform, "Arch_Post_North", new Vector3(35.2f, 5.8f,  1.8f), new Vector3(0.5f, 3.6f, 0.5f), woodMat);
            CreateBlock(shelterRoot.transform, "Arch_Lintel_Beam", new Vector3(35.2f, 7.5f, 0f), new Vector3(0.5f, 0.5f, 4.3f), woodMat);

            // Entrance Sandbags & Barricades
            CreateBlock(shelterRoot.transform, "Entrance_Sandbag_S", new Vector3(35.2f, 4.5f, -2.4f), new Vector3(0.8f, 0.9f, 0.8f), crateMat);
            CreateBlock(shelterRoot.transform, "Entrance_Sandbag_N", new Vector3(35.2f, 4.5f,  2.4f), new Vector3(0.8f, 0.9f, 0.8f), crateMat);

            // Signboard over Archway
            GameObject signGO = new GameObject("Archway_Signboard");
            signGO.transform.SetParent(shelterRoot.transform);
            signGO.transform.position = new Vector3(35.15f, 7.85f, 0f);
            signGO.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            TextMesh signText = signGO.AddComponent<TextMesh>();
            signText.text = "TRẠM TIẾP ĐÓN ĐỒNG BÀO TẬP KẾT - 1954";
            signText.fontSize = 28;
            signText.characterSize = 0.07f;
            signText.anchor = TextAnchor.MiddleCenter;
            signText.alignment = TextAlignment.Center;
            signText.color = new Color(0.98f, 0.88f, 0.45f);

            // NieR: Automata Perspective Transition Zone Trigger
            GameObject transZoneGO = new GameObject("Trigger_NieR_Perspective_Transition");
            transZoneGO.transform.SetParent(shelterRoot.transform);
            transZoneGO.transform.position = new Vector3(35.2f, 5.6f, 0f);
            BoxCollider transCol = transZoneGO.AddComponent<BoxCollider>();
            transCol.isTrigger = true;
            transCol.size = new Vector3(2.6f, 4.2f, 4.5f);
            transZoneGO.AddComponent<PerspectiveTransitionZone>();

            // 2. ROOM FLOOR & STRUCTURAL TIMBER WALLS
            // Wooden plank floor (Floor surface at Y = 4.2)
            CreateBlock(shelterRoot.transform, "Shelter_Plank_Floor", new Vector3(43.5f, 3.95f, 4.0f), new Vector3(17.2f, 0.5f, 16.5f), floorMat);

            // West Wall (Entrance Wall with opening between Z: -1.5 and +1.5)
            CreateBlock(shelterRoot.transform, "Wall_West_SouthWing", new Vector3(35.0f, 6.2f, -2.8f), new Vector3(0.4f, 4.2f, 2.5f), woodMat);
            CreateBlock(shelterRoot.transform, "Wall_West_NorthWing", new Vector3(35.0f, 6.2f,  6.8f), new Vector3(0.4f, 4.2f, 10.5f), woodMat);
            CreateBlock(shelterRoot.transform, "Wall_West_TopLintel",  new Vector3(35.0f, 8.0f,  0.0f), new Vector3(0.4f, 0.6f,  3.6f), woodMat);

            // East Wall (Deep Interior Wall)
            CreateBlock(shelterRoot.transform, "Wall_East", new Vector3(52.0f, 6.2f, 4.0f), new Vector3(0.4f, 4.2f, 16.5f), woodMat);

            // North Wall (Back Perimeter)
            CreateBlock(shelterRoot.transform, "Wall_North", new Vector3(43.5f, 6.2f, 12.0f), new Vector3(17.2f, 4.2f, 0.4f), woodMat);

            // South Wall (Side Perimeter)
            CreateBlock(shelterRoot.transform, "Wall_South_Solid", new Vector3(43.5f, 6.2f, -4.0f), new Vector3(17.2f, 4.2f, 0.4f), woodMat);

            // Ceiling Rafters across the room (Y = 8.2)
            CreateBlock(shelterRoot.transform, "Ceiling_Rafter_1", new Vector3(39.0f, 8.2f, 4.0f), new Vector3(0.35f, 0.35f, 16.2f), woodMat);
            CreateBlock(shelterRoot.transform, "Ceiling_Rafter_2", new Vector3(43.5f, 8.2f, 4.0f), new Vector3(0.35f, 0.35f, 16.2f), woodMat);
            CreateBlock(shelterRoot.transform, "Ceiling_Rafter_3", new Vector3(48.0f, 8.2f, 4.0f), new Vector3(0.35f, 0.35f, 16.2f), woodMat);

            // 3. CENTRAL CAMPFIRE & HEARTH (The Heart of the Shelter)
            GameObject hearthGO = new GameObject("Central_Hearth_Firepit");
            hearthGO.transform.SetParent(shelterRoot.transform);
            hearthGO.transform.position = new Vector3(42.5f, 4.2f, 4.5f);

            // Brick base circle
            GameObject brickRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            brickRing.name = "Brick_Ring";
            brickRing.transform.SetParent(hearthGO.transform);
            brickRing.transform.localPosition = new Vector3(0f, 0.12f, 0f);
            brickRing.transform.localScale = new Vector3(2.4f, 0.22f, 2.4f);
            brickRing.GetComponent<MeshRenderer>().sharedMaterial = brickMat;

            // Burning logs inside firepit
            GameObject logsGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            logsGO.name = "Firewood_Logs";
            logsGO.transform.SetParent(hearthGO.transform);
            logsGO.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            logsGO.transform.localScale = new Vector3(1.6f, 0.15f, 1.6f);
            logsGO.GetComponent<MeshRenderer>().sharedMaterial = woodMat;
            DestroyImmediate(logsGO.GetComponent<Collider>());

            // Brass tea kettle on wooden tripod
            GameObject kettleGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            kettleGO.name = "Brass_Tea_Kettle";
            kettleGO.transform.SetParent(hearthGO.transform);
            kettleGO.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            kettleGO.transform.localScale = new Vector3(0.65f, 0.55f, 0.65f);
            kettleGO.GetComponent<MeshRenderer>().sharedMaterial = ironMat;
            DestroyImmediate(kettleGO.GetComponent<Collider>());

            // Warm flickering campfire light
            GameObject fireLightGO = new GameObject("Fire_PointLight");
            fireLightGO.transform.SetParent(hearthGO.transform);
            fireLightGO.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            Light fLight = fireLightGO.AddComponent<Light>();
            fLight.type = LightType.Point;
            fLight.color = new Color(1.0f, 0.58f, 0.18f);
            fLight.intensity = 2.8f;
            fLight.range = 9.0f;
            fireLightGO.AddComponent<CampfireFlicker>();

            // Narrative interactable on campfire
            ShelterInteractable hearthInteract = hearthGO.AddComponent<ShelterInteractable>();
            hearthInteract.Configure(
                "Bếp Lửa Sưởi Ấm & Nước Chè Xanh",
                "[E] Sưởi ấm & Ngồi nghỉ",
                "Nồi nước chè xanh ấm cúng do bà con Cà Mau nấu phục vụ đồng bào chờ ngày lên tàu tập kết ra Bắc. Mùi khói củi và hơi chè mang lại cảm giác bình yên, xoa dịu nỗi âu lo giữa những ngày biến động lịch sử 1954."
            );

            // 4. REFUGEE SLEEPING AREA (Bunk Cots & Woven Rush Mats)
            CreateRefugeeBed(shelterRoot.transform, "Refugee_Cot_1", new Vector3(38.5f, 4.2f,  9.0f), woodMat, bambooMat, blanketMat, null, null, null);
            CreateRefugeeBed(shelterRoot.transform, "Refugee_Cot_2", new Vector3(38.5f, 4.2f,  5.5f), woodMat, bambooMat, blanketMat, null, null, null);
            
            // Bed 3 has family keepsake luggage with interaction
            CreateRefugeeBed(shelterRoot.transform, "Refugee_Cot_Keepsake", new Vector3(38.5f, 4.2f, -1.5f), woodMat, bambooMat, blanketMat,
                "Hành Lý & Kỷ Vật Cà Mau",
                "[E] Mở túi kỷ vật",
                "Chiếc túi vải thô chứa chiếc khăn rằn Nam Bộ và nắm đất phù sa cha mẹ vội trao trước giờ chia ly tại bến Sông Đốc: 'Ra ngoài Bắc ráng học cho giỏi, ngày đất nước hòa bình gia đình mình lại sum họp con nhé!'"
            );

            // 5. COMMAND & MARITIME LOGISTICS DESK (East Side)
            GameObject deskGO = new GameObject("Logistics_Command_Desk");
            deskGO.transform.SetParent(shelterRoot.transform);
            deskGO.transform.position = new Vector3(47.5f, 4.2f, 6.0f);

            // Table slab & legs
            CreateBlock(deskGO.transform, "Table_Top", new Vector3(47.5f, 4.95f, 6.0f), new Vector3(2.2f, 0.12f, 1.4f), woodMat);
            CreateBlock(deskGO.transform, "Table_Leg_1", new Vector3(46.6f, 4.55f, 5.5f), new Vector3(0.14f, 0.75f, 0.14f), woodMat);
            CreateBlock(deskGO.transform, "Table_Leg_2", new Vector3(48.4f, 4.55f, 5.5f), new Vector3(0.14f, 0.75f, 0.14f), woodMat);
            CreateBlock(deskGO.transform, "Table_Leg_3", new Vector3(46.6f, 4.55f, 6.5f), new Vector3(0.14f, 0.75f, 0.14f), woodMat);
            CreateBlock(deskGO.transform, "Table_Leg_4", new Vector3(48.4f, 4.55f, 6.5f), new Vector3(0.14f, 0.75f, 0.14f), woodMat);

            // Rolled paper maps on desk
            GameObject mapPaper = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mapPaper.name = "Maritime_Chart_Roll";
            mapPaper.transform.SetParent(deskGO.transform);
            mapPaper.transform.position = new Vector3(47.4f, 5.06f, 6.0f);
            mapPaper.transform.localScale = new Vector3(0.9f, 0.02f, 0.6f);
            mapPaper.GetComponent<MeshRenderer>().sharedMaterial = bambooMat;
            DestroyImmediate(mapPaper.GetComponent<Collider>());

            // Vintage radio transceiver
            GameObject radioGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            radioGO.name = "Vintage_Radio_Transceiver";
            radioGO.transform.SetParent(deskGO.transform);
            radioGO.transform.position = new Vector3(48.1f, 5.25f, 6.2f);
            radioGO.transform.localScale = new Vector3(0.45f, 0.42f, 0.35f);
            radioGO.GetComponent<MeshRenderer>().sharedMaterial = ironMat;
            DestroyImmediate(radioGO.GetComponent<Collider>());

            // Desk Oil Lamp
            GameObject deskLamp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            deskLamp.name = "Desk_Oil_Lamp";
            deskLamp.transform.SetParent(deskGO.transform);
            deskLamp.transform.position = new Vector3(46.8f, 5.25f, 6.2f);
            deskLamp.transform.localScale = new Vector3(0.2f, 0.35f, 0.2f);
            deskLamp.GetComponent<MeshRenderer>().sharedMaterial = lanternMat;
            DestroyImmediate(deskLamp.GetComponent<Collider>());

            GameObject deskLightGO = new GameObject("Desk_PointLight");
            deskLightGO.transform.SetParent(deskLamp.transform);
            deskLightGO.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            Light dLight = deskLightGO.AddComponent<Light>();
            dLight.type = LightType.Point;
            dLight.color = new Color(1.0f, 0.78f, 0.32f);
            dLight.intensity = 1.6f;
            dLight.range = 5.0f;

            // Narrative interactable on desk
            ShelterInteractable deskInteract = deskGO.AddComponent<ShelterInteractable>();
            deskInteract.Configure(
                "Bản Đồ Hải Trình Sông Đốc - Hải Phòng",
                "[E] Xem hải trình tập kết",
                "Hải trình vượt biển Đông hơn 1.000 cây số. Con tàu Ba Lan Kilinski và các chiến hạm vận tải chuẩn bị neo đậu tại cửa biển Sông Đốc, đón những chuyến tàu đầu tiên trong đợt chuyển quân 200 ngày theo hiệp định Genève 1954."
            );

            // 6. RATION CRATES & MEDICAL RED CROSS AID CORNER (North Area)
            GameObject supplyGO = new GameObject("Supply_Aid_Corner");
            supplyGO.transform.SetParent(shelterRoot.transform);
            supplyGO.transform.position = new Vector3(44.0f, 4.2f, 10.5f);

            CreateBlock(supplyGO.transform, "Crate_Ration_1", new Vector3(43.2f, 4.7f, 10.5f), new Vector3(1.1f, 1.0f, 1.1f), crateMat);
            CreateBlock(supplyGO.transform, "Crate_Ration_2", new Vector3(44.4f, 4.7f, 10.5f), new Vector3(1.1f, 1.0f, 1.1f), crateMat);
            CreateBlock(supplyGO.transform, "Crate_Ration_3", new Vector3(43.8f, 5.7f, 10.5f), new Vector3(1.0f, 0.9f, 1.0f), crateMat);
            CreateBlock(supplyGO.transform, "Medical_FirstAid_Box", new Vector3(45.5f, 4.6f, 10.2f), new Vector3(0.9f, 0.75f, 0.8f), floorMat);

            ShelterInteractable supplyInteract = supplyGO.AddComponent<ShelterInteractable>();
            supplyInteract.Configure(
                "Rương Tiếp Tế Y Tế & Thuốc Men",
                "[E] Kiểm tra tiếp tế",
                "Lương khô 702, gạo sấy và hàng ngàn liều ký ninh chống sốt rét rừng U Minh. Đội ngũ quân y túc trực ngày đêm để bảo đảm sức khỏe cho hàng vạn đồng bào, thương bệnh binh và học sinh trước ngày xuất dương."
            );

            // 7. PATRIOTIC RED BANNER WITH GOLD STAR (Ceiling Rafter)
            GameObject bannerGO = CreateBlock(shelterRoot.transform, "Patriotic_Banner_Red", new Vector3(43.5f, 7.4f, 4.5f), new Vector3(1.8f, 1.2f, 0.04f), flagMat);
            bannerGO.AddComponent<WindClothFlutter>();
            GameObject goldStar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            goldStar.name = "Gold_Star_Emblem";
            goldStar.transform.SetParent(bannerGO.transform);
            goldStar.transform.localPosition = new Vector3(0f, 0f, -0.04f);
            goldStar.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            goldStar.transform.localScale = new Vector3(0.35f, 0.02f, 0.35f);
            goldStar.GetComponent<MeshRenderer>().sharedMaterial = goldStarMat;
            DestroyImmediate(goldStar.GetComponent<Collider>());

            // 8. HANGING HURRICANE LANTERNS
            CreateHangingLantern(shelterRoot.transform, "Lantern_West", new Vector3(39.0f, 7.5f, 2.0f), ironMat, lanternMat);
            CreateHangingLantern(shelterRoot.transform, "Lantern_East", new Vector3(47.5f, 7.5f, 2.0f), ironMat, lanternMat);

            // 9. CHECKPOINT & PIER EXIT GOAL
            CreateCheckpoint(shelterRoot.transform, "Checkpoint_ShelterRoom", new Vector3(37.5f, 4.8f, 0f), new Vector3(2.5f, 3.0f, 4.0f), 4);

            // Goal Marker at the Pier Exit Doorway (X: 51.5)
            GameObject pierExit = new GameObject("Pier_Exit_Threshold");
            pierExit.transform.SetParent(shelterRoot.transform);
            pierExit.transform.position = new Vector3(51.2f, 5.2f, 4.0f);
            BoxCollider goalCol = pierExit.AddComponent<BoxCollider>();
            goalCol.isTrigger = true;
            goalCol.size = new Vector3(1.5f, 3.0f, 3.0f);
            pierExit.AddComponent<LevelGoalTrigger>();
        }

        private GameObject CreateRefugeeBed(Transform parent, string name, Vector3 pos, Material woodMat, Material bambooMat, Material blanketMat, string title, string key, string desc)
        {
            GameObject bedGO = new GameObject(name);
            bedGO.transform.SetParent(parent);
            bedGO.transform.position = pos;

            // Wooden cot frame
            CreateBlock(bedGO.transform, "Bed_Frame", new Vector3(pos.x, pos.y + 0.22f, pos.z), new Vector3(2.1f, 0.42f, 1.05f), woodMat);

            // Bamboo rush mat (Chiếu cói)
            GameObject matGO = CreateBlock(bedGO.transform, "Rush_Mat", new Vector3(pos.x, pos.y + 0.44f, pos.z), new Vector3(2.05f, 0.04f, 1.0f), bambooMat);
            DestroyImmediate(matGO.GetComponent<Collider>());

            // Folded military green blanket
            GameObject blanketGO = CreateBlock(bedGO.transform, "Folded_Blanket", new Vector3(pos.x + 0.65f, pos.y + 0.52f, pos.z), new Vector3(0.6f, 0.12f, 0.85f), blanketMat);
            DestroyImmediate(blanketGO.GetComponent<Collider>());

            if (!string.IsNullOrEmpty(title))
            {
                // Keepsake travel bundle
                GameObject luggage = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                luggage.name = "Keepsake_Luggage";
                luggage.transform.SetParent(bedGO.transform);
                luggage.transform.position = new Vector3(pos.x - 0.55f, pos.y + 0.55f, pos.z);
                luggage.transform.localScale = new Vector3(0.45f, 0.35f, 0.4f);
                luggage.GetComponent<MeshRenderer>().sharedMaterial = woodMat;
                DestroyImmediate(luggage.GetComponent<Collider>());

                ShelterInteractable interact = bedGO.AddComponent<ShelterInteractable>();
                interact.Configure(title, key, desc);
            }

            return bedGO;
        }

        private GameObject CreateHangingLantern(Transform parent, string name, Vector3 pos, Material ironMat, Material lanternMat)
        {
            GameObject lanternGO = new GameObject(name);
            lanternGO.transform.SetParent(parent);
            lanternGO.transform.position = pos;

            // Hanging cord
            CreateBlock(lanternGO.transform, "Cord", new Vector3(pos.x, pos.y + 0.35f, pos.z), new Vector3(0.04f, 0.7f, 0.04f), ironMat);

            // Lantern body
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "Body";
            body.transform.SetParent(lanternGO.transform);
            body.transform.position = pos;
            body.transform.localScale = new Vector3(0.26f, 0.35f, 0.26f);
            body.GetComponent<MeshRenderer>().sharedMaterial = lanternMat;
            DestroyImmediate(body.GetComponent<Collider>());

            // Warm Point Light
            GameObject lightGO = new GameObject("Light");
            lightGO.transform.SetParent(lanternGO.transform);
            lightGO.transform.position = pos;
            Light l = lightGO.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1.0f, 0.78f, 0.32f);
            l.intensity = 1.3f;
            l.range = 5.5f;
            lightGO.AddComponent<CampfireFlicker>();

            return lanternGO;
        }

        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(obj, value);
        }
    }
}
