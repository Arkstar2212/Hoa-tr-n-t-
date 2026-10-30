#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using HoaVaMau.Gameplay;
using HoaVaMau.Test3D;

namespace HoaVaMau.Editor
{
    /// <summary>
    /// Single unified batch Editor setup script (adhering strictly to AGENTS.md Rules 4.1, 4.2, and 4.3):
    /// 1. Registers Scene_MainMenu, New Scene 1_Overhaul, CaMau_1954_Village, and canh1 in Build Settings.
    /// 2. ADDITIVE-ONLY on New Scene 1_Overhaul.unity: creates [Additive_EndOfMap_To_CaMau3D] at the
    ///    exact rightmost walkable end of the map with OverhaulEndMapTransitionTrigger + SceneTransitionBotWLoader
    ///    (using the exact post-Start-Menu BotW Loading Screen & verified sprites).
    /// 3. Upgrades CaMau_1954_Village.unity with:
    ///    - Continuous multi-layered Nature Background Audio (CaMauNatureAmbienceController)
    ///    - 3D Planet-of-Lana UI Guides & Arrival Vignette (CaMau3DUIGuideController + PlanetOfLanaPromptSystem)
    ///    - Ported Overhaul Functional Mechanics: PauseMenuController ([Esc]), CheckpointManager + CheckpointZones,
    ///      Player3DController Jump/Crouch/Footsteps, VillageNPCInteractable 3D dialogues, InteractableCrate3D,
    ///      PortableItem3D, BushHideZone 3D stealth bushes, and ContextualPromptTriggers.
    /// 4. Renders 720p (1280x720 .jpg quality 80) visual verification snapshots into Logs/.
    /// </summary>
    public static class SetupOverhaulToCaMau3DTransition
    {
        private const string MainMenuScenePath = "Assets/Scenes/Scene_MainMenu.unity";
        private const string OverhaulScenePath = "Assets/Scenes/New Scene 1_Overhaul.unity";
        private const string CaMau3DScenePath  = "Assets/Scenes/CaMau_1954_Village.unity";
        private const string Canh1ScenePath    = "Assets/Scenes/canh1.unity";

        private const string AdditiveEndRootName = "[Additive_EndOfMap_To_CaMau3D]";
        private const string Ported3DRootName    = "[Overhaul_Ported_Systems_3D]";

        [MenuItem("Tools/HoaTrenDatDo/SetupOverhaulToCaMau3D")]
        public static void ExecuteAllMenuNoSpaces() => ExecuteAll();

        [MenuItem("Tools/HoaTrenDatDo/FixCaMau3DCameraAndPlayer")]
        public static void ExecuteFixCaMau3DCameraAndPlayerOnly()
        {
            SanitizeCargoCratePrefab();
            SetupCaMau3DVillageScene();
            CleanupTempVerificationFiles();
            AssetDatabase.SaveAssets();
            Debug.Log("<color=#10b981><b>[SetupOverhaulToCaMau3DTransition]</b></color> Fixed CaMau_1954_Village 3D Camera & Player visibility!");
        }

        [MenuItem("Tools/Hoa Tren Dat Do/Setup Overhaul End Transition & Ca Mau 3D Mechanics")]
        public static void ExecuteAll()
        {
            SanitizeCargoCratePrefab();
            RegisterBuildSettings();
            SetupOverhaulAdditiveEndTransition();
            SetupCaMau3DVillageScene();
            CleanupTempVerificationFiles();
            AssetDatabase.SaveAssets();
            Debug.Log("<color=#10b981><b>[SetupOverhaulToCaMau3DTransition]</b></color> Completed full end-of-Overhaul -> BotW Loading Screen -> 3D Ca Mau 1954 Village setup with zero errors!");
        }

        private static void SanitizeCargoCratePrefab()
        {
            string[] prefabPaths =
            {
                "Assets/Prefabs/Props/PF_Prop_CargoCrate_3D.prefab",
                "Assets/Prefabs/Props/PF_WoodenSlatCube.prefab"
            };
            foreach (string path in prefabPaths)
            {
                if (!File.Exists(path)) continue;
                GameObject contents = PrefabUtility.LoadPrefabContents(path);
                if (contents == null) continue;
                bool changed = StripStrayBlenderCamerasAndLights(contents);
                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                    Debug.Log($"[SetupOverhaulToCaMau3DTransition] Stripped embedded Blender Camera/Light from prefab: {path}");
                }
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static bool StripStrayBlenderCamerasAndLights(GameObject root)
        {
            bool modified = false;
            var cams = root.GetComponentsInChildren<Camera>(true);
            foreach (var c in cams)
            {
                if (c == null) continue;
                if (c.gameObject != root)
                {
                    Object.DestroyImmediate(c.gameObject);
                }
                else
                {
                    var al = c.GetComponent<AudioListener>();
                    if (al != null) Object.DestroyImmediate(al);
                    Object.DestroyImmediate(c);
                }
                modified = true;
            }

            var lights = root.GetComponentsInChildren<Light>(true);
            foreach (var l in lights)
            {
                if (l == null) continue;
                if (l.intensity > 50f || l.gameObject.name == "Light")
                {
                    if (l.gameObject != root && l.GetComponents<Component>().Length <= 3)
                        Object.DestroyImmediate(l.gameObject);
                    else
                        Object.DestroyImmediate(l);
                    modified = true;
                }
            }
            return modified;
        }

        private static void CleanupTempVerificationFiles()
        {
            string[] tempPaths =
            {
                "Assets/Logs/Verify_PlayMode_3D.png",
                "Assets/Logs/Verify_CaMau3D_LivePlay.png",
                "Assets/Logs/Verify_CaMau3D_Fixed.png"
            };
            foreach (string p in tempPaths)
            {
                if (AssetDatabase.LoadAssetAtPath<Object>(p) != null || File.Exists(p))
                {
                    AssetDatabase.DeleteAsset(p);
                }
            }
            if (Directory.Exists("Assets/Logs") && Directory.GetFiles("Assets/Logs").Length == 0)
            {
                AssetDatabase.DeleteAsset("Assets/Logs");
            }
        }

        private static void RegisterBuildSettings()
        {
            string[] orderedScenePaths = new[]
            {
                MainMenuScenePath,
                OverhaulScenePath,
                CaMau3DScenePath,
                Canh1ScenePath
            };

            var buildScenes = new List<EditorBuildSettingsScene>();
            foreach (string path in orderedScenePaths)
            {
                if (File.Exists(path))
                {
                    buildScenes.Add(new EditorBuildSettingsScene(path, true));
                }
            }

            // Preserve any other scenes already registered in Build Settings
            foreach (var existing in EditorBuildSettings.scenes)
            {
                bool alreadyAdded = false;
                foreach (var s in buildScenes)
                {
                    if (s.path == existing.path) { alreadyAdded = true; break; }
                }
                if (!alreadyAdded && File.Exists(existing.path))
                {
                    buildScenes.Add(existing);
                }
            }

            EditorBuildSettings.scenes = buildScenes.ToArray();
            Debug.Log($"[SetupOverhaulToCaMau3DTransition] Registered {buildScenes.Count} scenes in EditorBuildSettings (including '{CaMau3DScenePath}').");
        }

        private static Sprite LoadVerifiedUISprite(string assetPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        // =========================================================================================
        // PART 1: NEW SCENE 1_OVERHAUL.UNITY (STRICTLY ADDITIVE ONLY - RULE 4.3)
        // =========================================================================================
        private static void SetupOverhaulAdditiveEndTransition()
        {
            if (!File.Exists(OverhaulScenePath))
            {
                Debug.LogError($"[SetupOverhaulToCaMau3DTransition] Missing scene: {OverhaulScenePath}");
                return;
            }

            var scene = EditorSceneManager.OpenScene(OverhaulScenePath, OpenSceneMode.Single);

            // Idempotency: ONLY remove our own dedicated additive group if re-running; NEVER touch existing scene objects!
            GameObject existingAdditive = GameObject.Find(AdditiveEndRootName);
            if (existingAdditive != null)
            {
                Object.DestroyImmediate(existingAdditive);
            }

            // Inspect live scene colliders to locate the exact rightmost walkable end of New Scene 1_Overhaul
            DetermineOverhaulRightmostWalkablePoint(out float endTriggerX, out float groundY);

            GameObject additiveRoot = new GameObject(AdditiveEndRootName);
            additiveRoot.transform.position = Vector3.zero;

            // 1. End-of-Map Trigger Volume
            GameObject triggerGO = new GameObject("EndOfMap_CaMau3D_TransitionZone");
            triggerGO.transform.SetParent(additiveRoot.transform, false);
            triggerGO.transform.position = new Vector3(endTriggerX, groundY + 2.5f, 0f);

            var boxCol = triggerGO.AddComponent<BoxCollider>();
            boxCol.isTrigger = true;
            boxCol.size = new Vector3(3.8f, 7.5f, 6.0f);

            var endTrigger = triggerGO.AddComponent<OverhaulEndMapTransitionTrigger>();
            endTrigger.ConfigureTrigger(endTriggerX - 0.6f, "CaMau_1954_Village");

            // 2. Post-Start-Menu BotW Loading Screen System (wired with verified Start Menu sprites)
            Sprite softDotSpr = LoadVerifiedUISprite("Assets/Textures/UI/T_Loading_SoftDot.png");
            Sprite boySpr     = LoadVerifiedUISprite("Assets/Textures/PierScene/running_boy_sprite.png");
            Sprite shipSpr    = LoadVerifiedUISprite("Assets/Textures/Canh1_Sprites/TransportShip_1954_Card.png");

            GameObject loaderGO = new GameObject("PostStartMenu_BotW_LoadingSystem");
            loaderGO.transform.SetParent(triggerGO.transform, false);
            var botwLoader = loaderGO.AddComponent<SceneTransitionBotWLoader>();
            botwLoader.ConfigureSprites(softDotSpr, boySpr, shipSpr, "CaMau_1954_Village", "ĐANG TẢI LÀNG CÀ MAU 1954...");

            // 3. Subtle Diegetic Wooden Village Milestone Post beside the path (Z = +1.55m background, non-blocking)
            BuildAdditiveVillageWaypost(additiveRoot.transform, new Vector3(endTriggerX - 1.2f, groundY, 1.55f));

            // 4. Invisible Safety Stopper 1.8m behind the trigger so An can never walk past the painted vista
            GameObject safetyWall = new GameObject("EndOfMap_SafetyStopper");
            safetyWall.transform.SetParent(additiveRoot.transform, false);
            safetyWall.transform.position = new Vector3(endTriggerX + 1.8f, groundY + 3.0f, 0f);
            var wallCol = safetyWall.AddComponent<BoxCollider>();
            wallCol.size = new Vector3(1.0f, 8.0f, 6.0f);

            // Save clean Overhaul scene first (without temporary preview UI)
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, OverhaulScenePath);
            Debug.Log($"[SetupOverhaulToCaMau3DTransition] Added '{AdditiveEndRootName}' to '{OverhaulScenePath}' at X = {endTriggerX:F2}m, Y = {groundY:F2}m.");

            // Render 720p (.jpg quality 80) visual verification snapshots without dirtying the saved scene
            CaptureOverhaulVerificationSnapshots(endTriggerX, groundY, botwLoader);
        }

        private static void DetermineOverhaulRightmostWalkablePoint(out float triggerX, out float groundY)
        {
            triggerX = 135.0f;
            groundY = -1.85f;

            // Check if the 100m Rice Field additive extension (X = 138m -> 238.5m) is present in New Scene 1_Overhaul
            GameObject endWall238 = GameObject.Find("Pathway_3DEndWall_238m");
            if (endWall238 != null && endWall238.activeInHierarchy)
            {
                triggerX = endWall238.transform.position.x - 2.6f; // ~235.9m right before the invisible end wall
            }
            else
            {
                // Scan walkable solid colliders along Z = 0
                float maxWalkableX = 130f;
                var allCols = Object.FindObjectsByType<Collider>();
                foreach (var c in allCols)
                {
                    if (c == null || c.isTrigger || !c.gameObject.activeInHierarchy) continue;
                    Bounds b = c.bounds;
                    if (Mathf.Abs(b.center.z) > 4.0f) continue;
                    // Ignore tall invisible boundary walls (height > 6.5m and thin X < 2.0m)
                    if (b.size.y > 6.5f && b.size.x < 2.2f) continue;
                    if (b.max.x > maxWalkableX && b.max.x < 400f)
                    {
                        maxWalkableX = b.max.x;
                    }
                }
                triggerX = maxWalkableX - 5.2f;
            }

            // Raycast downward to find exact pathway surface Y at triggerX
            if (Physics.Raycast(new Vector3(triggerX, 12f, 0f), Vector3.down, out RaycastHit hit, 30f, ~0, QueryTriggerInteraction.Ignore))
            {
                groundY = hit.point.y;
            }
        }

        private static void BuildAdditiveVillageWaypost(Transform parent, Vector3 pos)
        {
            GameObject postRoot = new GameObject("Diegetic_VillageWaypost_1954");
            postRoot.transform.SetParent(parent, false);
            postRoot.transform.position = pos;

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/CaMau1954/M_Timber_Dark.mat");
            if (woodMat == null)
            {
                woodMat = new Material(urpLit) { color = new Color(0.32f, 0.22f, 0.14f) };
            }

            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "WoodenPole";
            pole.transform.SetParent(postRoot.transform, false);
            pole.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            pole.transform.localScale = new Vector3(0.11f, 0.95f, 0.11f);
            pole.GetComponent<Renderer>().sharedMaterial = woodMat;
            Object.DestroyImmediate(pole.GetComponent<Collider>());

            GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "DirectionSign_CaMau";
            board.transform.SetParent(postRoot.transform, false);
            board.transform.localPosition = new Vector3(0.22f, 1.62f, 0f);
            board.transform.localRotation = Quaternion.Euler(0f, 0f, -4f);
            board.transform.localScale = new Vector3(0.85f, 0.24f, 0.06f);
            board.GetComponent<Renderer>().sharedMaterial = woodMat;
            Object.DestroyImmediate(board.GetComponent<Collider>());

            GameObject lanternLightGO = new GameObject("WarmLanternBeacon");
            lanternLightGO.transform.SetParent(postRoot.transform, false);
            lanternLightGO.transform.localPosition = new Vector3(0.35f, 1.42f, -0.2f);
            var ptLight = lanternLightGO.AddComponent<Light>();
            ptLight.type = LightType.Point;
            ptLight.color = new Color(1.0f, 0.82f, 0.48f);
            ptLight.intensity = 1.6f;
            ptLight.range = 5.5f;
        }

        private static void CaptureOverhaulVerificationSnapshots(float endX, float groundY, SceneTransitionBotWLoader botwLoader)
        {
            Directory.CreateDirectory("Logs");
            Camera mainCam = Camera.main;
            if (mainCam == null) return;

            Vector3 origPos = mainCam.transform.position;
            Quaternion origRot = mainCam.transform.rotation;

            // Snapshot 1: End of Overhaul Map Trigger Area (1280x720 JPG quality 80)
            mainCam.transform.position = new Vector3(endX - 5.5f, groundY + 2.2f, -10.5f);
            mainCam.transform.rotation = Quaternion.Euler(3f, 0f, 0f);
            RenderCameraToJpg720p(mainCam, "Logs/Verify_01_Overhaul_EndOfMap_Trigger.jpg", null);

            // Snapshot 2: Post-Start-Menu BotW Loading Screen Preview (1280x720 JPG quality 80)
            if (botwLoader != null)
            {
                botwLoader.SetEditorPreviewVisible(true, 9.4f);
                var canvas = botwLoader.GetComponentInChildren<Canvas>(true);
                RenderCameraToJpg720p(mainCam, "Logs/Verify_02_BotW_LoadingScreen_Transition.jpg", canvas);
                // Clean up temporary UI preview objects so nothing extra remains in memory
                if (canvas != null) Object.DestroyImmediate(canvas.gameObject);
            }

            mainCam.transform.position = origPos;
            mainCam.transform.rotation = origRot;
        }

        // =========================================================================================
        // PART 2: CA MAU 1954 3D VILLAGE SCENE (NATURE AUDIO + UI GUIDES + OVERHAUL MECHANICS)
        // =========================================================================================
        private static void SetupCaMau3DVillageScene()
        {
            if (!File.Exists(CaMau3DScenePath))
            {
                Debug.LogError($"[SetupOverhaulToCaMau3DTransition] Missing scene: {CaMau3DScenePath}");
                return;
            }

            var scene = EditorSceneManager.OpenScene(CaMau3DScenePath, OpenSceneMode.Single);

            GameObject managersGroup = GameObject.Find("06_Managers") ?? new GameObject("06_Managers");
            GameObject audioGroup    = GameObject.Find("07_Audio")    ?? new GameObject("07_Audio");

            // Remove previous ported group if re-running idempotently
            GameObject oldPorted = GameObject.Find(Ported3DRootName);
            if (oldPorted != null) Object.DestroyImmediate(oldPorted);
            GameObject portedRoot = new GameObject(Ported3DRootName);

            // 1. Background Nature Soundscape (CaMauNatureAmbienceController)
            AudioClip riceFieldWindClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_Ambience_RiceField_Wind.mp3");
            AudioClip breezeClip        = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/soundreality-wind-blowing-457954.mp3");
            AudioClip gentleMusicClip   = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Breath of the World.mp3");

            Transform existingNature = audioGroup.transform.Find("CaMauNatureAmbience");
            if (existingNature != null) Object.DestroyImmediate(existingNature.gameObject);

            GameObject natureGO = new GameObject("CaMauNatureAmbience");
            natureGO.transform.SetParent(audioGroup.transform, false);
            var natureCtrl = natureGO.AddComponent<CaMauNatureAmbienceController>();
            natureCtrl.ConfigureAudioClips(riceFieldWindClip, breezeClip, gentleMusicClip);

            // Keep IntroAudioController disabled/quiet so it doesn't double-play wind over CaMauNatureAmbienceController
            var legacyIntroAudio = Object.FindAnyObjectByType<IntroAudioController>();
            if (legacyIntroAudio != null)
            {
                if (legacyIntroAudio.windAudioSource != null) legacyIntroAudio.windAudioSource.playOnAwake = false;
                if (legacyIntroAudio.lofiAudioSource != null) legacyIntroAudio.lofiAudioSource.playOnAwake = false;
                legacyIntroAudio.enabled = false;
            }

            // 2. Upgrade An_Player with Barefoot Dirt Footsteps, 3D Visibility, & ThirdPersonOrbitCamera binding
            FixScenePlayerAndMainCamera();

            // 3. Ported UI Guides & Core Managers from Overhaul -> 06_Managers
            SetupManagerComponent<CaMau3DUIGuideController>(managersGroup.transform, "CaMau3DUIGuideController");
            SetupManagerComponent<PlanetOfLanaPromptSystem>(managersGroup.transform, "PlanetOfLanaPromptSystem");

            var pauseMenu = SetupManagerComponent<PauseMenuController>(managersGroup.transform, "PauseMenuController");
            Sprite logoSpr = LoadVerifiedUISprite("Assets/Textures/UI/T_Logo_HoaTrenDatDo.png");
            if (pauseMenu != null && logoSpr != null)
            {
                var so = new SerializedObject(pauseMenu);
                var prop = so.FindProperty("officialLogoSprite");
                if (prop != null) prop.objectReferenceValue = logoSpr;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var cpManager = SetupManagerComponent<CheckpointManager>(managersGroup.transform, "CheckpointManager");
            if (cpManager != null)
            {
                cpManager.ConfigureDefaults(new Vector3(0.5f, 0.45f, 13.0f), -1.45f);
                EditorUtility.SetDirty(cpManager);
            }

            // 4. Ported 3D Checkpoint Zones across the Village
            GameObject cpGroup = new GameObject("01_Checkpoints_3D");
            cpGroup.transform.SetParent(portedRoot.transform, false);
            Create3DCheckpointZone(cpGroup.transform, "CP_01_NhaAn_Spawn",   new Vector3(0.5f, 0.45f, 13.0f),  new Vector3(4.5f, 3.0f, 4.5f), 1);
            Create3DCheckpointZone(cpGroup.transform, "CP_02_SanLang_Plaza", new Vector3(0.5f, 0.05f, 1.5f),   new Vector3(5.5f, 3.0f, 5.5f), 2);
            Create3DCheckpointZone(cpGroup.transform, "CP_03_CanhDong_Dike", new Vector3(22.5f, 0.15f, 6.0f),  new Vector3(4.5f, 3.0f, 4.5f), 3);
            Create3DCheckpointZone(cpGroup.transform, "CP_04_BenXuong_Pier", new Vector3(6.0f, 0.08f, -11.5f), new Vector3(4.5f, 3.0f, 4.5f), 4);

            // 5. Ported Living Villager NPC Dialogues & [E] Key Glyph (VillageNPCInteractable)
            ConfigureVillage3DNPCs();
            PolishVillage3DInteractableObjects();

            // 6. Ported 3D Push/Pull Crates (InteractableCrate3D)
            AudioClip crateScrapeClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_Crate_Push.mp3");
            GameObject cratesGroup = new GameObject("02_PushableCrates_3D");
            cratesGroup.transform.SetParent(portedRoot.transform, false);
            SpawnPushableCrate3D(cratesGroup.transform, "PushCrate_SanLang_01", new Vector3(4.2f, 0.42f, 4.2f), 12f, crateScrapeClip, "Thùng Gỗ Sân Làng");
            SpawnPushableCrate3D(cratesGroup.transform, "PushCrate_BenXuong_02", new Vector3(4.6f, 0.42f, -10.8f), -18f, crateScrapeClip, "Kiện Hàng Bến Xuồng");

            // 7. Ported 3D Portable Carry/Toss Items (PortableItem3D)
            GameObject itemsGroup = new GameObject("03_PortableItems_3D");
            itemsGroup.transform.SetParent(portedRoot.transform, false);
            SpawnPortableBasket3D(itemsGroup.transform, "Portable_BambooBasket_NhaAn", new Vector3(2.1f, 0.52f, 14.2f), "Rổ Tre Đựng Rau", "Đã nhặt Rổ Tre — Nhấn [E] đặt xuống hoặc [Q] ném nhẹ.");
            SpawnPortableBasket3D(itemsGroup.transform, "Portable_BambooBasket_VuonCay", new Vector3(-15.8f, 0.30f, 4.2f), "Sọt Trái Cây Vườn", "Đã nhặt Sọt Trái Cây — Mang về sân nhà hoặc nhấn [E] đặt xuống.");

            // 8. Ported 3D Stealth Bush Concealment Zones (BushHideZone)
            GameObject stealthGroup = new GameObject("04_StealthBushes_3D");
            stealthGroup.transform.SetParent(portedRoot.transform, false);
            SpawnStealthBush3D(stealthGroup.transform, "StealthBush_VillagePath_01", new Vector3(-2.6f, 0.15f, 8.2f), 25f);
            SpawnStealthBush3D(stealthGroup.transform, "StealthBush_Riverbank_02",   new Vector3(-8.8f, 0.05f, -7.8f), -35f);
            SpawnStealthBush3D(stealthGroup.transform, "StealthBush_CanalEdge_03",   new Vector3(13.2f, 0.08f, 3.2f), 60f);

            // 9. Ported Contextual Diegetic Prompt Triggers (Jump at Bridges, Crouch at Bushes)
            GameObject promptsGroup = new GameObject("05_ContextualPromptTriggers_3D");
            promptsGroup.transform.SetParent(portedRoot.transform, false);
            CreateContextualPromptZone(promptsGroup.transform, "Prompt_Jump_MonkeyBridge", new Vector3(-8.2f, 0.8f, 0.8f), new Vector3(3.2f, 2.5f, 3.0f), PlanetOfLanaPromptSystem.PromptType.Jump);
            CreateContextualPromptZone(promptsGroup.transform, "Prompt_Jump_ArchedBridge", new Vector3(14.5f, 0.8f, -2.0f), new Vector3(3.2f, 2.5f, 3.2f), PlanetOfLanaPromptSystem.PromptType.Jump);
            CreateContextualPromptZone(promptsGroup.transform, "Prompt_Crouch_StealthBush", new Vector3(-2.6f, 0.8f, 8.2f), new Vector3(3.0f, 2.5f, 3.0f), PlanetOfLanaPromptSystem.PromptType.Crawl);

            // 10. Final pass to guarantee zero stray Blender Cameras/Lights exist anywhere in the scene
            FixScenePlayerAndMainCamera();

            // Save clean 3D Ca Mau 1954 Village scene
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, CaMau3DScenePath);
            Debug.Log($"[SetupOverhaulToCaMau3DTransition] Upgraded and saved '{CaMau3DScenePath}' with Nature Audio, 3D UI Guides, and Overhaul Mechanics.");

            // Capture 720p verification screenshot of CaMau_1954_Village with 3D UI Guide
            CaptureCaMau3DVerificationSnapshot();
        }

        private static void FixScenePlayerAndMainCamera()
        {
            // 1. Locate Main Camera & strip every other stray Camera/Blender Light in the scene
            GameObject mainCamGO = GameObject.Find("Main Camera");
            Camera mainCam = mainCamGO != null ? mainCamGO.GetComponent<Camera>() : Camera.main;

            var allCams = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include);
            foreach (var c in allCams)
            {
                if (c == null) continue;
                if (c == mainCam || c.gameObject.name == "Main Camera")
                {
                    mainCam = c;
                    mainCamGO = c.gameObject;
                    continue;
                }
                Debug.Log($"[SetupOverhaulToCaMau3DTransition] Removing stray embedded Camera '{c.gameObject.name}' (parent: {(c.transform.parent != null ? c.transform.parent.name : "root")})");
                if (c.transform.parent != null && c.gameObject.name == "Camera")
                {
                    Object.DestroyImmediate(c.gameObject);
                }
                else
                {
                    var al = c.GetComponent<AudioListener>();
                    if (al != null) Object.DestroyImmediate(al);
                    Object.DestroyImmediate(c);
                }
            }

            var allLights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include);
            foreach (var l in allLights)
            {
                if (l == null) continue;
                if (l.intensity > 50f || (l.gameObject.name == "Light" && l.transform.parent != null && l.transform.parent.name.Contains("Crate")))
                {
                    Debug.Log($"[SetupOverhaulToCaMau3DTransition] Removing stray embedded Blender Light '{l.gameObject.name}' (intensity={l.intensity})");
                    if (l.transform.parent != null && l.gameObject.name == "Light")
                        Object.DestroyImmediate(l.gameObject);
                    else
                        Object.DestroyImmediate(l);
                }
            }

            // 2. Locate and repair An_Player (including replacing broken Missing Prefab shells)
            GameObject playerGO = null;
            if (playerGO == null) playerGO = GameObject.Find("An_Player");
            if (playerGO == null)
            {
                var p3dCheck = Object.FindAnyObjectByType<Player3DController>();
                if (p3dCheck != null) playerGO = p3dCheck.gameObject;
            }
            Transform playerParent = playerGO != null ? playerGO.transform.parent : null;
            if (playerGO != null && (PrefabUtility.IsPrefabAssetMissing(playerGO) || playerGO.GetComponentInChildren<SkinnedMeshRenderer>(true) == null))
            {
                Object.DestroyImmediate(playerGO);
                playerGO = null;
            }
            if (playerGO == null)
            {
                GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TripoModels/Boy_Character/Boy_Character_Model.prefab")
                                      ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TripoModels/Boy_Character/Boy_Character.fbx");
                if (modelPrefab != null)
                {
                    playerGO = playerParent != null
                        ? (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, playerParent)
                        : (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab);
                    playerGO.name = "An_Player";
                }
            }
            if (playerGO == null) playerGO = GameObject.Find("An_Player");
            if (playerGO == null)
            {
                var p3dExisting = Object.FindAnyObjectByType<Player3DController>();
                if (p3dExisting != null) playerGO = p3dExisting.gameObject;
            }

            if (playerGO != null)
            {
                playerGO.SetActive(true);
                playerGO.tag = "Player";

                // Remove any 2D leftover scripts on 3D village player
                var meshy2D = playerGO.GetComponent("MeshyBoyAnimationController") as MonoBehaviour;
                if (meshy2D != null) Object.DestroyImmediate(meshy2D);
                var pc2D = playerGO.GetComponent<PlayerController>();
                if (pc2D != null) Object.DestroyImmediate(pc2D);
                var anim2D = playerGO.GetComponent<AnAnimationController>();
                if (anim2D != null) Object.DestroyImmediate(anim2D);
                var ledge2D = playerGO.GetComponent<LedgeGrabDetector>();
                if (ledge2D != null) Object.DestroyImmediate(ledge2D);

                // Set clean spawn position & forward rotation (facing North 0° toward An's house & Mother)
                playerGO.transform.position = new Vector3(0.5f, 0.45f, 13.0f);
                playerGO.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                playerGO.transform.localScale = Vector3.one * 1.25f;

                // Ensure SkinnedMeshRenderer is active, visible, and assigned M_Boy_Character.mat
                Material boyMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/TripoModels/Boy_Character/Materials/M_Boy_Character.mat")
                               ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/TripoModels/Boy_Character/M_Boy_Character.mat");
                foreach (var smr in playerGO.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    smr.gameObject.SetActive(true);
                    smr.enabled = true;
                    smr.updateWhenOffscreen = true;
                    if (boyMat != null)
                    {
                        smr.sharedMaterial = boyMat;
                    }
                    EditorUtility.SetDirty(smr);
                }

                // Ensure Animator has valid controller & avatar
                var anim = playerGO.GetComponentInChildren<Animator>();
                if (anim != null)
                {
                    anim.enabled = true;
                    anim.applyRootMotion = false;
                    anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    var boyCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/Controllers/AC_Player_An.controller");

                    if (boyCtrl != null) anim.runtimeAnimatorController = boyCtrl;
                    EditorUtility.SetDirty(anim);
                }

                // Ensure CharacterController
                var cc = playerGO.GetComponent<CharacterController>();
                if (cc == null) cc = playerGO.AddComponent<CharacterController>();
                cc.height = 1.15f;
                cc.radius = 0.25f;
                cc.center = new Vector3(0f, 0.575f, 0f);
                cc.stepOffset = 0.32f;
                cc.slopeLimit = 50f;
                cc.skinWidth = 0.02f;

                var interSys = Object.FindAnyObjectByType<InteractionSystem3D>();
                if (interSys != null)
                {
                    SerializedObject interSO = new SerializedObject(interSys);
                    var pTransProp = interSO.FindProperty("playerTransform");
                    if (pTransProp != null) pTransProp.objectReferenceValue = playerGO.transform;
                    interSO.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(interSys);
                }

                // Ensure Player3DController & wire references
                var p3d = playerGO.GetComponent<Player3DController>();
                if (p3d == null) p3d = playerGO.AddComponent<Player3DController>();
                AudioClip footstepDirt = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_An_Footsteps_Dirt.mp3");
                p3d.ConfigureFootstepAudio(footstepDirt);

                SerializedObject p3dSO = new SerializedObject(p3d);
                var animProp = p3dSO.FindProperty("animator");
                if (animProp != null) animProp.objectReferenceValue = anim;
                var camProp = p3dSO.FindProperty("cameraTransform");
                if (camProp != null && mainCamGO != null) camProp.objectReferenceValue = mainCamGO.transform;
                p3dSO.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(p3d);
                EditorUtility.SetDirty(playerGO);
            }

            // 3. Configure Main Camera & ThirdPersonOrbitCamera directly behind An_Player
            if (mainCamGO != null && mainCam != null)
            {
                mainCamGO.SetActive(true);
                mainCamGO.tag = "MainCamera";
                mainCam.enabled = true;
                mainCam.orthographic = false;
                mainCam.fieldOfView = 56f;
                mainCam.nearClipPlane = 0.12f;
                mainCam.farClipPlane = 450f;
                mainCam.depth = 10f;

                var orbit = mainCamGO.GetComponent<ThirdPersonOrbitCamera>();
                if (orbit == null) orbit = mainCamGO.AddComponent<ThirdPersonOrbitCamera>();
                orbit.enabled = true;

                if (playerGO != null)
                {
                    orbit.Target = playerGO.transform;
                    SerializedObject camSO = new SerializedObject(orbit);
                    var targetProp = camSO.FindProperty("target");
                    if (targetProp != null) targetProp.objectReferenceValue = playerGO.transform;
                    var pivotProp = camSO.FindProperty("pivotOffset");
                    if (pivotProp != null) pivotProp.vector3Value = new Vector3(0f, 1.05f, 0f);
                    var shoulderProp = camSO.FindProperty("shoulderOffset");
                    if (shoulderProp != null) shoulderProp.floatValue = 0f;
                    var distProp = camSO.FindProperty("defaultDistance");
                    if (distProp != null) distProp.floatValue = 4.0f;
                    var minDistProp = camSO.FindProperty("minDistance");
                    if (minDistProp != null) minDistProp.floatValue = 1.4f;
                    camSO.ApplyModifiedPropertiesWithoutUndo();

                    orbit.SnapToTarget(0f);
                }
                EditorUtility.SetDirty(orbit);
                EditorUtility.SetDirty(mainCam);
                EditorUtility.SetDirty(mainCamGO);
            }
        }

        private static T SetupManagerComponent<T>(Transform parent, string objectName) where T : MonoBehaviour
        {
            T existing = Object.FindAnyObjectByType<T>();
            if (existing != null) return existing;

            GameObject go = new GameObject(objectName);
            go.transform.SetParent(parent, false);
            return go.AddComponent<T>();
        }

        private static void Create3DCheckpointZone(Transform parent, string name, Vector3 pos, Vector3 size, int index)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = size;
            var zone = go.AddComponent<CheckpointZone>();
            zone.checkpointIndex = index;
        }

        private static void ConfigureVillage3DNPCs()
        {
            RuntimeAnimatorController villagerCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/AC_Lana_LivingVillager.controller");

            BindNPC("NPC_Mother_An", "Má Của An", new[]
            {
                "An về tới nhà rồi đó hả con! Rửa mặt cho mát rồi dạo quanh xóm thăm bà con nghen.",
                "Trên bậc thềm má có để sẵn Gói Đất Đỏ Cà Mau bọc khăn rằn để chiều nay mang ra bến xuồng gửi các anh đi tập kết."
            }, "Wave", villagerCtrl);

            BindNPC("NPC_Female_WaterCarrier", "Cô Ba Gánh Nước", new[]
            {
                "Nước giếng làng mình trong và ngọt mát lắm An ơi.",
                "Cô gánh thêm đôi thùng nữa về nấu nồi chè đậu đen đãi mấy chú bộ đội trước giờ xuống tàu."
            }, "Talk", villagerCtrl);

            BindNPC("NPC_Elderly_VillageChief", "Bác Hai Trưởng Xóm", new[]
            {
                "Năm 1954 lịch sử này, xóm Rạch Gốc mình tiễn bao người con ưu tú ra Bắc hẹn ngày thống nhất.",
                "Nhìn lá cờ đỏ sao vàng tung bay giữa sân làng mà lòng bác rưng rưng tự hào quá con ạ."
            }, "Idle", villagerCtrl);

            BindNPC("NPC_Child_Playing_Boy", "Cu Tí", new[]
            {
                "Anh An ơi! Ra sân làng chơi bắn bi với tụi em đi!",
                "Chiều nay tụi em rủ nhau chạy ra cầu tàu xem đoàn xuồng ba lá chở gạo lớn lắm!"
            }, "Wave", villagerCtrl);

            BindNPC("NPC_Child_Playing_Girl", "Bé Tư", new[]
            {
                "Gió thổi từ sông Cà Mau mát rượi cả sân đình luôn anh An hen!",
                "Em mới hái mấy bông hoa dại trắng ven rạch tặng má nè."
            }, "Idle2", villagerCtrl);

            BindNPC("NPC_Villager_Gardener", "Bác Sáu Làm Vườn", new[]
            {
                "Giàn bầu với vườn chuối năm nay tốt tươi nhờ phù sa sông Cà Mau bồi đắp.",
                "Con cứ lấy rổ tre hái vài trái bầu non đem về cho má nấu canh nghe An!"
            }, "Idle", villagerCtrl);

            BindNPC("NPC_Farmer_RiceField", "Chú Ba Nông Dân", new[]
            {
                "Lúa chín vàng rực cả cánh đồng rồi An ơi! Tiếng gió đồng thổi nghe bình yên quá đỗi.",
                "Cảm ơn hai má con lúc nào cũng nhớ mang cơm trưa ra đồng tiếp sức cho chú nghen!"
            }, "Wave", villagerCtrl);

            BindNPC("NPC_Ferryman_UncleBay", "Bác Bảy Lái Đò", new[]
            {
                "Xuồng ba lá bác đã buộc chèo sẵn sàng nơi bến nước rồi đây con.",
                "Khi nào thăm hết bà con trong xóm, ra đây ngắm con nước chiều Cà Mau cùng bác nghen!"
            }, "Talk", villagerCtrl);
        }

        private static void BindNPC(string objName, string speakerTitle, string[] lines, string animState, RuntimeAnimatorController ctrl)
        {
            GameObject npc = GameObject.Find(objName);
            if (npc == null) return;

            var anim = npc.GetComponentInChildren<Animator>();
            if (anim != null && anim.runtimeAnimatorController == null && ctrl != null && anim.isHuman)
            {
                anim.runtimeAnimatorController = ctrl;
                anim.applyRootMotion = false;
            }

            var interact = npc.GetComponent<VillageNPCInteractable>();
            if (interact == null) interact = npc.AddComponent<VillageNPCInteractable>();
            interact.ConfigureNPC(speakerTitle, lines, animState, 0f);
            EditorUtility.SetDirty(interact);
        }

        private static void PolishVillage3DInteractableObjects()
        {
            // Disable overlapping INT_* triggers where an NPC now directly hosts VillageNPCInteractable [E] dialogue
            string[] npcDuplicateTriggers = { "INT_TalkToMother", "INT_TalkToVillager_01", "INT_InspectPier" };
            foreach (string dup in npcDuplicateTriggers)
            {
                GameObject go = GameObject.Find(dup);
                if (go != null) go.SetActive(false);
            }

            // Polish object-based 3D interactions with warm Vietnamese titles & messages
            ConfigureInteractable3D("INT_InspectSoilPouch", "Gói Đất Đỏ Cà Mau 1954",
                "Nắm đất đỏ quê hương bọc trong chiếc khăn rằn Nam Bộ — kỷ vật thiêng liêng gửi theo người đi tập kết ra Bắc.");
            ConfigureInteractable3D("INT_FetchWater", "Giếng Nước Quê",
                "An múc một gáo nước giếng trong vắt, mát lạnh giữa trưa nắng miền Tây.");
            ConfigureInteractable3D("INT_PlayWithKids", "Trò Chơi Tuổi Thơ",
                "Tiếng cười giòn tan của đám trẻ bắn bi vang vọng dưới bóng cây đại thụ sân làng.");
            ConfigureInteractable3D("INT_PickFruit", "Giàn Bầu Xanh Mát",
                "Những trái bầu non mơn mởn đung đưa dưới nắng — hương vị ngọt lành của mảnh vườn Cà Mau.");
            ConfigureInteractable3D("INT_DeliverMeal", "Cặp Lồng Cơm Quê",
                "Mùi cơm gạo mới thơm lừng hòa cùng hương rơm rạ trên cánh đồng lúa chín.");
            ConfigureInteractable3D("INT_SitByRiver", "Bến Nước Chiều Tà",
                "Sóng nước Cà Mau vỗ nhẹ vào mạn xuồng ba lá, gió chiều thổi xào xạc qua rặng dừa nước.");
        }

        private static void ConfigureInteractable3D(string name, string title, string actionMsg)
        {
            GameObject go = GameObject.Find(name);
            if (go == null) return;
            var inter = go.GetComponent<InteractableObject3D>();
            if (inter == null) return;
            inter.objectTitle = title;
            inter.promptMessage = "Nhấn [E] tương tác";
            inter.actionMessage = actionMsg;
            EditorUtility.SetDirty(inter);
        }

        private static void SpawnPushableCrate3D(Transform parent, string name, Vector3 pos, float yaw, AudioClip pushClip, string label)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Props/PF_Prop_CargoCrate_3D.prefab")
                             ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Props/PF_WoodenSlatCube.prefab");

            GameObject crateGO;
            if (prefab != null)
            {
                crateGO = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                crateGO.transform.SetParent(parent, false);
                StripStrayBlenderCamerasAndLights(crateGO);
            }
            else
            {
                crateGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
                crateGO.transform.SetParent(parent, false);
            }

            crateGO.name = name;
            crateGO.transform.position = pos;
            crateGO.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            crateGO.transform.localScale = Vector3.one * 0.92f;

            if (crateGO.GetComponent<Collider>() == null)
            {
                var box = crateGO.AddComponent<BoxCollider>();
                box.size = new Vector3(0.85f, 0.85f, 0.85f);
                box.center = new Vector3(0f, 0.425f, 0f);
            }

            var crate3D = crateGO.GetComponent<InteractableCrate3D>() ?? crateGO.AddComponent<InteractableCrate3D>();
            crate3D.ConfigureAudio(pushClip, label);
        }

        private static void SpawnPortableBasket3D(Transform parent, string name, Vector3 pos, string title, string msg)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Props/PF_Prop_Bamboo_Basket.prefab");
            GameObject itemGO;
            if (prefab != null)
            {
                itemGO = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            }
            else
            {
                itemGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                itemGO.transform.SetParent(parent, false);
                itemGO.transform.localScale = new Vector3(0.45f, 0.30f, 0.45f);
            }

            itemGO.name = name;
            itemGO.transform.position = pos;

            var col = itemGO.GetComponent<Collider>();
            if (col == null)
            {
                var sphere = itemGO.AddComponent<SphereCollider>();
                sphere.radius = 0.32f;
                sphere.center = new Vector3(0f, 0.18f, 0f);
            }

            var portable = itemGO.GetComponent<PortableItem3D>() ?? itemGO.AddComponent<PortableItem3D>();
            portable.ConfigureItem(title, msg);
        }

        private static void SpawnStealthBush3D(Transform parent, string name, Vector3 pos, float yaw)
        {
            GameObject bushRoot = new GameObject(name);
            bushRoot.transform.SetParent(parent, false);
            bushRoot.transform.position = pos;
            bushRoot.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            var triggerBox = bushRoot.AddComponent<BoxCollider>();
            triggerBox.isTrigger = true;
            triggerBox.size = new Vector3(1.85f, 1.5f, 1.85f);
            triggerBox.center = new Vector3(0f, 0.75f, 0f);

            GameObject foliageCore = new GameObject("Foliage_Core");
            foliageCore.transform.SetParent(bushRoot.transform, false);

            GameObject bushPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_Concept_Assets/Bushes/PF_T_HD_Bush_Broadleaf_01.prefab")
                                 ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Foliage/PF_StylizedGrassTuft_MeadowCluster.prefab");
            if (bushPrefab != null)
            {
                var b1 = (GameObject)PrefabUtility.InstantiatePrefab(bushPrefab, foliageCore.transform);
                b1.transform.localPosition = Vector3.zero;
                b1.transform.localScale = Vector3.one * 1.15f;

                var b2 = (GameObject)PrefabUtility.InstantiatePrefab(bushPrefab, foliageCore.transform);
                b2.transform.localPosition = new Vector3(0.35f, 0f, -0.25f);
                b2.transform.localRotation = Quaternion.Euler(0f, 75f, 0f);
                b2.transform.localScale = Vector3.one * 1.05f;
            }

            bushRoot.AddComponent<BushHideZone>();
        }

        private static void CreateContextualPromptZone(Transform parent, string name, Vector3 pos, Vector3 size, PlanetOfLanaPromptSystem.PromptType type)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = size;
            var trig = go.AddComponent<ContextualPromptTrigger>();
            trig.Type = type;
            trig.ShowOnceOnly = false;
        }

        private static void CaptureCaMau3DVerificationSnapshot()
        {
            Directory.CreateDirectory("Logs");
            Camera mainCam = Camera.main;
            if (mainCam == null) return;

            var uiGuide = Object.FindAnyObjectByType<CaMau3DUIGuideController>();
            Canvas tempCanvas = null;
            if (uiGuide != null)
            {
                uiGuide.SetEditorPreviewState();
                tempCanvas = uiGuide.GetComponentInChildren<Canvas>(true);
            }

            RenderCameraToJpg720p(mainCam, "Logs/Verify_03_CaMau1954_3DVillage_UIGuide.jpg", tempCanvas);

            if (tempCanvas != null)
            {
                Object.DestroyImmediate(tempCanvas.gameObject);
            }
        }

        private static void RenderCameraToJpg720p(Camera cam, string outputPath, Canvas overlayCanvas)
        {
            const int width = 1280;
            const int height = 720;

            RenderMode prevMode = RenderMode.ScreenSpaceOverlay;
            Camera prevWorldCam = null;
            float prevPlaneDist = 1f;

            if (overlayCanvas != null)
            {
                prevMode = overlayCanvas.renderMode;
                prevWorldCam = overlayCanvas.worldCamera;
                prevPlaneDist = overlayCanvas.planeDistance;

                overlayCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                overlayCanvas.worldCamera = cam;
                overlayCanvas.planeDistance = 0.5f;
                Canvas.ForceUpdateCanvases();
            }

            RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture prevCamTarget = cam.targetTexture;
            RenderTexture prevActive = RenderTexture.active;

            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            cam.targetTexture = prevCamTarget;
            RenderTexture.active = prevActive;
            Object.DestroyImmediate(rt);

            if (overlayCanvas != null)
            {
                overlayCanvas.renderMode = prevMode;
                overlayCanvas.worldCamera = prevWorldCam;
                overlayCanvas.planeDistance = prevPlaneDist;
            }

            byte[] jpgBytes = tex.EncodeToJPG(80);
            Object.DestroyImmediate(tex);
            File.WriteAllBytes(outputPath, jpgBytes);
            Debug.Log($"[SetupOverhaulToCaMau3DTransition] Saved 720p verification snapshot: {outputPath}");
        }
    }
}
#endif
