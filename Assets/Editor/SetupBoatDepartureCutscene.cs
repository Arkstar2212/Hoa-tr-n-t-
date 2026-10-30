using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using HoaVaMau.Cutscene;

namespace HoaVaMau.Editor
{
    public static class SetupBoatDepartureCutscene
    {
        private const string VILLAGE_SCENE_PATH = "Assets/Scenes/CaMau_1954_Village.unity";
        private const string CUTSCENE_SCENE_PATH = "Assets/Scenes/Cutscene_BoatSunset.unity";
        private const string SHIP_HORN_PATH = "Assets/Audio/SFX/SFX_Vintage_ShipHorn_1950s.mp3";
        private const string SUNSET_BG_PATH = "Assets/Textures/Backgrounds_Lana/BG_Lana_SeaHorizon_Sunset.png";
        private const string WATER_SHIMMER_PATH = "Assets/Textures/Environment/T_Camau_Water_Shimmer.png";
        private const string WATER_WAKE_PATH = "Assets/Textures/Environment/T_Camau_Boat_WaterWake.png";
        private const string SUNBEAMS_PATH = "Assets/Textures/Environment/T_Camau_Sunbeams_Volumetric.png";
        private const string SEAGULL_PATH = "Assets/Textures/Environment/Seagulls/Seagull_Frame_Glide.png";
        private const string SHIP_PREFAB_PATH = "Assets/Prefabs/Boats/PF_TransportShip_1954.prefab";
        private const string MUSIC_PATH = "Assets/Audio/Breath of the World.mp3";
        private const string FONT_BOLD_PATH = "Assets/Fonts/SegoeUI_Bold_SDF.asset";
        private const string FONT_REGULAR_PATH = "Assets/Fonts/SegoeUI_SDF_Dynamic.asset";

        [MenuItem("Tools/Hoa Tren Dat Do/Setup Departure Cutscene & Trigger", false, 100)]
        public static void RunSetup()
        {
            Debug.Log("[SetupBoatDepartureCutscene] Starting automated setup...");

            // Step 1: Ensure audio file is imported
            AssetDatabase.ImportAsset(SHIP_HORN_PATH, ImportAssetOptions.ForceUpdate);
            var shipHornClip = AssetDatabase.LoadAssetAtPath<AudioClip>(SHIP_HORN_PATH);
            if (shipHornClip == null)
            {
                Debug.LogError($"[SetupBoatDepartureCutscene] Ship horn clip not found at {SHIP_HORN_PATH}");
            }

            // Step 2: Build and configure Cutscene_BoatSunset.unity
            CreateAndConfigureCutsceneScene(shipHornClip);

            // Step 3: Add Cutscene_BoatSunset to Build Settings
            AddSceneToBuildSettings(CUTSCENE_SCENE_PATH);

            // Step 4: Setup Trigger in CaMau_1954_Village.unity
            SetupPierTriggerInVillageScene(shipHornClip);

            Debug.Log("[SetupBoatDepartureCutscene] Setup completed successfully!");
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(s => s.path == scenePath))
            {
                scenes.Add(new EditorBuildSettingsScene(scenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
                Debug.Log($"[SetupBoatDepartureCutscene] Added '{scenePath}' to EditorBuildSettings.");
            }
        }

        private static void SetupPierTriggerInVillageScene(AudioClip shipHornClip)
        {
            // Open Village scene if not currently open
            if (SceneManager.GetActiveScene().path != VILLAGE_SCENE_PATH)
            {
                EditorSceneManager.OpenScene(VILLAGE_SCENE_PATH, OpenSceneMode.Single);
            }

            // Additive Only: find or create [Additive_Pier_Cutscene_Trigger]
            string rootName = "[Additive_Pier_Cutscene_Trigger]";
            var existingRoot = GameObject.Find(rootName);
            if (existingRoot != null)
            {
                Object.DestroyImmediate(existingRoot);
            }

            var triggerRoot = new GameObject(rootName);
            // Position at pier tip: WalkablePier_Wooden is at (6, -0.1, -15.5) with pile ends around z = -20.45.
            // Trigger volume comfortably covers z = -17.5 to -20.5
            triggerRoot.transform.position = new Vector3(6.0f, 0.4f, -18.8f);

            var boxCol = triggerRoot.AddComponent<BoxCollider>();
            boxCol.isTrigger = true;
            boxCol.size = new Vector3(3.4f, 2.5f, 3.8f);

            var audioSource = triggerRoot.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0.0f; // 2D immersion

            var departureTrigger = triggerRoot.AddComponent<PierDepartureTrigger>();
            // Use SerializedObject to hook up fields cleanly
            var so = new SerializedObject(departureTrigger);
            so.FindProperty("targetSceneName").stringValue = "Cutscene_BoatSunset";
            so.FindProperty("shipHornClip").objectReferenceValue = shipHornClip;
            so.FindProperty("hornVolume").floatValue = 1.0f;
            so.FindProperty("waitBeforeFadeSeconds").floatValue = 1.2f;
            so.FindProperty("fadeDurationSeconds").floatValue = 2.0f;
            so.ApplyModifiedProperties();

            // Save Village scene
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log($"[SetupBoatDepartureCutscene] Successfully added '{rootName}' at {triggerRoot.transform.position} in {VILLAGE_SCENE_PATH}.");
        }

        private static void CreateAndConfigureCutsceneScene(AudioClip shipHornClip)
        {
            // Create a brand new clean scene
            var cutsceneScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Lighting & RenderSettings
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.75f, 0.55f, 0.45f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.008f;
            RenderSettings.fogColor = new Color(0.95f, 0.65f, 0.48f);

            // Directional Light (Warm Golden Hour Sunset)
            var lightGo = new GameObject("Sunset_DirectionalLight");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1.0f, 0.65f, 0.35f); // Rich warm sunset orange-gold
            light.intensity = 1.4f;
            lightGo.transform.rotation = Quaternion.Euler(12f, -30f, 0f);

            // 2. Camera Setup
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.96f, 0.62f, 0.42f); // Sunset sky background
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 500f;
            camGo.AddComponent<AudioListener>();
            camGo.transform.position = new Vector3(-2f, 3.2f, -16f);
            camGo.transform.rotation = Quaternion.Euler(5.5f, 0f, 0f);

            // 3. Sunset Backdrop Quad (Panorama)
            var bgTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(SUNSET_BG_PATH);
            var bgGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bgGo.name = "BG_Sunset_Panorama";
            bgGo.transform.position = new Vector3(0f, 7.5f, 42f);
            bgGo.transform.localScale = new Vector3(110f, 40f, 1f);
            Object.DestroyImmediate(bgGo.GetComponent<Collider>());

            var bgMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            if (bgTexture != null)
            {
                bgMat.mainTexture = bgTexture;
            }
            bgMat.color = new Color(1.05f, 0.95f, 0.90f); // slight warm bloom
            bgGo.GetComponent<Renderer>().sharedMaterial = bgMat;

            // 4. Volumetric Sunbeams
            var sunbeamsTex = AssetDatabase.LoadAssetAtPath<Texture2D>(SUNBEAMS_PATH);
            if (sunbeamsTex != null)
            {
                var sunbeamsGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
                sunbeamsGo.name = "VFX_Sunset_Sunbeams";
                sunbeamsGo.transform.position = new Vector3(-4f, 8f, 38f);
                sunbeamsGo.transform.rotation = Quaternion.Euler(10f, -15f, -12f);
                sunbeamsGo.transform.localScale = new Vector3(70f, 30f, 1f);
                Object.DestroyImmediate(sunbeamsGo.GetComponent<Collider>());

                var sbMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                sbMat.mainTexture = sunbeamsTex;
                sbMat.SetFloat("_Surface", 1); // Transparent
                sbMat.SetFloat("_Blend", 0); // Alpha blend
                sbMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                sbMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One); // Additive shimmer
                sbMat.SetInt("_ZWrite", 0);
                sbMat.renderQueue = 3000;
                sbMat.color = new Color(1.0f, 0.8f, 0.5f, 0.35f);
                sunbeamsGo.GetComponent<Renderer>().sharedMaterial = sbMat;
            }

            // 5. Ocean Water Surface
            var waterGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            waterGo.name = "Ocean_Water_Surface";
            waterGo.transform.position = new Vector3(0f, -0.45f, 15f);
            waterGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            waterGo.transform.localScale = new Vector3(180f, 120f, 1f);
            Object.DestroyImmediate(waterGo.GetComponent<Collider>());

            var waterTex = AssetDatabase.LoadAssetAtPath<Texture2D>(WATER_SHIMMER_PATH);
            var waterMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (waterTex != null)
            {
                waterMat.mainTexture = waterTex;
                waterMat.mainTextureScale = new Vector2(25f, 18f);
            }
            waterMat.color = new Color(0.12f, 0.30f, 0.42f); // Deep emerald-blue delta sea
            waterMat.SetFloat("_Smoothness", 0.85f);
            var waterRenderer = waterGo.GetComponent<Renderer>();
            waterRenderer.sharedMaterial = waterMat;

            // 6. The 1954 Transport Ship
            var shipPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SHIP_PREFAB_PATH);
            GameObject shipInstance = null;
            if (shipPrefab != null)
            {
                shipInstance = (GameObject)PrefabUtility.InstantiatePrefab(shipPrefab);
                shipInstance.name = "PF_TransportShip_1954";
                shipInstance.transform.position = new Vector3(-14f, 0.35f, 6f);
                shipInstance.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
            }
            else
            {
                // Fallback stylized ship
                shipInstance = new GameObject("Stylized_Ship");
                shipInstance.transform.position = new Vector3(-14f, 0.35f, 6f);
            }

            // 7. Water Wake trailing behind the stern
            var wakeTex = AssetDatabase.LoadAssetAtPath<Texture2D>(WATER_WAKE_PATH);
            GameObject wakeGo = null;
            if (wakeTex != null)
            {
                wakeGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
                wakeGo.name = "Ship_Water_Wake";
                wakeGo.transform.position = new Vector3(-17f, -0.44f, 6f);
                wakeGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                wakeGo.transform.localScale = new Vector3(8f, 3.5f, 1f);
                Object.DestroyImmediate(wakeGo.GetComponent<Collider>());

                var wakeMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                wakeMat.mainTexture = wakeTex;
                wakeMat.SetFloat("_Surface", 1);
                wakeMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                wakeMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                wakeMat.SetInt("_ZWrite", 0);
                wakeMat.renderQueue = 3001;
                wakeMat.color = new Color(1f, 1f, 1f, 0.8f);
                wakeGo.GetComponent<Renderer>().sharedMaterial = wakeMat;
            }

            // 8. Distant Seagulls
            var seagullTex = AssetDatabase.LoadAssetAtPath<Texture2D>(SEAGULL_PATH);
            var seagullTransforms = new Transform[3];
            for (int i = 0; i < 3; i++)
            {
                var sgGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
                sgGo.name = $"Seagull_Gliding_{i}";
                sgGo.transform.position = new Vector3(-10f + i * 8f, 6.5f + (i % 2) * 1.5f, 20f + i * 5f);
                sgGo.transform.localScale = new Vector3(1.2f, 0.8f, 1f);
                Object.DestroyImmediate(sgGo.GetComponent<Collider>());

                var sgMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                if (seagullTex != null) sgMat.mainTexture = seagullTex;
                sgMat.SetFloat("_Surface", 1);
                sgMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                sgMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                sgMat.SetInt("_ZWrite", 0);
                sgMat.renderQueue = 3000;
                sgMat.color = new Color(0.95f, 0.9f, 0.85f, 0.85f);
                sgGo.GetComponent<Renderer>().sharedMaterial = sgMat;
                seagullTransforms[i] = sgGo.transform;
            }

            // 9. Audio Sources & Music
            var audioHolder = new GameObject("Audio_Cutscene_Manager");
            var ambientAudio = audioHolder.AddComponent<AudioSource>();
            var musicClip = AssetDatabase.LoadAssetAtPath<AudioClip>(MUSIC_PATH);
            ambientAudio.clip = musicClip;
            ambientAudio.loop = true;
            ambientAudio.playOnAwake = true;
            ambientAudio.volume = 0.5f;
            ambientAudio.spatialBlend = 0.0f;
            ambientAudio.Play();

            var hornAudio = audioHolder.AddComponent<AudioSource>();
            hornAudio.playOnAwake = false;
            hornAudio.spatialBlend = 0.0f;
            hornAudio.volume = 0.9f;

            // 10. UI Canvas Setup (Fade Overlay + "Còn tiếp...")
            var canvasGo = new GameObject("Cutscene_UI_Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // Black Fade Overlay
            var blackOverlayGo = new GameObject("Black_Fade_Overlay", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            blackOverlayGo.transform.SetParent(canvasGo.transform, false);
            var boRect = blackOverlayGo.GetComponent<RectTransform>();
            boRect.anchorMin = Vector2.zero;
            boRect.anchorMax = Vector2.one;
            boRect.offsetMin = Vector2.zero;
            boRect.offsetMax = Vector2.zero;
            blackOverlayGo.GetComponent<Image>().color = Color.black;
            var blackFadeGroup = blackOverlayGo.GetComponent<CanvasGroup>();
            blackFadeGroup.alpha = 1f;

            // "Còn tiếp..." Title Panel
            var titleGroupGo = new GameObject("Title_Panel", typeof(RectTransform), typeof(CanvasGroup));
            titleGroupGo.transform.SetParent(canvasGo.transform, false);
            var titleRect = titleGroupGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.sizeDelta = new Vector2(1200, 400);
            titleRect.anchoredPosition = Vector2.zero;
            var titleGroup = titleGroupGo.GetComponent<CanvasGroup>();
            titleGroup.alpha = 0f;

            // Load TMP Fonts
            var fontBold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_BOLD_PATH);
            var fontRegular = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_REGULAR_PATH);

            // Main "CÒN TIẾP..." Text
            var mainTextGo = new GameObject("MainTitle_Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            mainTextGo.transform.SetParent(titleGroupGo.transform, false);
            var mainRect = mainTextGo.GetComponent<RectTransform>();
            mainRect.anchoredPosition = new Vector2(0f, 40f);
            mainRect.sizeDelta = new Vector2(1000f, 120f);
            var tmpMain = mainTextGo.GetComponent<TextMeshProUGUI>();
            if (fontBold != null) tmpMain.font = fontBold;
            tmpMain.text = "CÒN TIẾP...";
            tmpMain.fontSize = 64;
            tmpMain.alignment = TextAlignmentOptions.Center;
            tmpMain.color = new Color(1.0f, 0.95f, 0.88f, 1.0f);
            tmpMain.characterSpacing = 8f;

            // Subtitle Text
            var subTextGo = new GameObject("Subtitle_Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            subTextGo.transform.SetParent(titleGroupGo.transform, false);
            var subRect = subTextGo.GetComponent<RectTransform>();
            subRect.anchoredPosition = new Vector2(0f, -40f);
            subRect.sizeDelta = new Vector2(1000f, 60f);
            var tmpSub = subTextGo.GetComponent<TextMeshProUGUI>();
            if (fontRegular != null) tmpSub.font = fontRegular;
            tmpSub.text = "Hoa trên đất đỏ — Chuyến tàu 1954";
            tmpSub.fontSize = 24;
            tmpSub.fontStyle = FontStyles.Italic;
            tmpSub.alignment = TextAlignmentOptions.Center;
            tmpSub.color = new Color(0.85f, 0.78f, 0.70f, 0.9f);
            tmpSub.characterSpacing = 4f;

            // Prompt Text
            var promptGo = new GameObject("Prompt_Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            promptGo.transform.SetParent(titleGroupGo.transform, false);
            var pRect = promptGo.GetComponent<RectTransform>();
            pRect.anchoredPosition = new Vector2(0f, -140f);
            pRect.sizeDelta = new Vector2(800f, 50f);
            var tmpPrompt = promptGo.GetComponent<TextMeshProUGUI>();
            if (fontRegular != null) tmpPrompt.font = fontRegular;
            tmpPrompt.text = "Nhấn phím bất kỳ để quay lại làng";
            tmpPrompt.fontSize = 18;
            tmpPrompt.alignment = TextAlignmentOptions.Center;
            tmpPrompt.color = new Color(0.65f, 0.60f, 0.55f, 0.8f);

            // 11. Cutscene Director Setup
            var directorGo = new GameObject("Cutscene_Director");
            var director = directorGo.AddComponent<BoatSunsetCutsceneDirector>();

            var dirSo = new SerializedObject(director);
            dirSo.FindProperty("shipTransform").objectReferenceValue = shipInstance.transform;
            if (wakeGo != null) dirSo.FindProperty("waterWakeTransform").objectReferenceValue = wakeGo.transform;
            dirSo.FindProperty("cameraTransform").objectReferenceValue = camGo.transform;
            dirSo.FindProperty("waterSurfaceRenderer").objectReferenceValue = waterRenderer;

            var seagullsProp = dirSo.FindProperty("seagulls");
            seagullsProp.arraySize = seagullTransforms.Length;
            for (int i = 0; i < seagullTransforms.Length; i++)
            {
                seagullsProp.GetArrayElementAtIndex(i).objectReferenceValue = seagullTransforms[i];
            }

            dirSo.FindProperty("ambientAudioSource").objectReferenceValue = ambientAudio;
            dirSo.FindProperty("hornAudioSource").objectReferenceValue = hornAudio;
            dirSo.FindProperty("shipHornClip").objectReferenceValue = shipHornClip;
            dirSo.FindProperty("blackFadeGroup").objectReferenceValue = blackFadeGroup;
            dirSo.FindProperty("titleTextGroup").objectReferenceValue = titleGroup;
            dirSo.FindProperty("mainTitleText").objectReferenceValue = tmpMain;
            dirSo.FindProperty("subtitleText").objectReferenceValue = tmpSub;
            dirSo.FindProperty("promptText").objectReferenceValue = tmpPrompt;
            dirSo.ApplyModifiedProperties();

            // Save cutscene scene
            EditorSceneManager.SaveScene(cutsceneScene, CUTSCENE_SCENE_PATH);
            Debug.Log($"[SetupBoatDepartureCutscene] Saved Cutscene Scene at '{CUTSCENE_SCENE_PATH}'.");
        }
    }
}
