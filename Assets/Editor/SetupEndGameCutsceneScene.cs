using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using TMPro;
using HoaVaMau.Cutscenes;

namespace HoaVaMau.Editor
{
    public static class SetupEndGameCutsceneScene
    {
        private const string SCENE_PATH = "Assets/Scenes/Cutscene_EndGame.unity";
        private const string MATERIAL_DIR = "Assets/Materials/Cutscene";

        [MenuItem("Tools/Hoa/Setup End Game Cutscene Scene", false, 10)]
        public static void CreateCutsceneScene()
        {
            // 1. Ensure material directory exists
            if (!AssetDatabase.IsValidFolder(MATERIAL_DIR))
            {
                AssetDatabase.CreateFolder("Assets/Materials", "Cutscene");
            }

            // 2. Prepare Steamship Material
            Material shipMat = CreateOrGetShipMaterial();

            // 3. Prepare Uber Water Material
            Material waterMat = CreateOrGetWaterMaterial();

            // 4. Prepare Smoke Particle Material
            Material smokeMat = CreateOrGetSmokeMaterial();

            // 5. Prepare Horizon Cloud Material
            Material cloudMat = CreateOrGetHorizonCloudMaterial();

            // 6. Create New Scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 7. Build Environment & Lighting
            GameObject envRoot = new GameObject("=== ENVIRONMENT & LIGHTING ===");

            // Directional Light (Authentic Low Sunset Angle aligned directly with the sun)
            // Sun is at X = 18m, Y = 4.2m, Z = 120m; Camera side rest is at X = 10m, Z = -17m
            // Yaw = 183.3 degrees aligns the reflection column directly under the sun!
            GameObject dirLightGo = new GameObject("Sunset_DirectionalLight");
            dirLightGo.transform.SetParent(envRoot.transform);
            dirLightGo.transform.position = new Vector3(18f, 20f, 120f);
            dirLightGo.transform.rotation = Quaternion.Euler(7.5f, 183.3f, 0f);
            var lightComp = dirLightGo.AddComponent<Light>();
            lightComp.type = LightType.Directional;
            lightComp.color = new Color(1.0f, 0.74f, 0.48f);
            lightComp.intensity = 1.15f;
            lightComp.shadows = LightShadows.Soft;

            // Global Volume (URP Post-Processing)
            GameObject volumeGo = new GameObject("Sunset_GlobalVolume");
            volumeGo.transform.SetParent(envRoot.transform);
            var volumeComp = volumeGo.AddComponent<Volume>();
            volumeComp.isGlobal = true;
            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "Profile_Cutscene_Sunset";

            // Add Bloom (Soft golden glow around the sun and water crests)
            var bloom = profile.Add<Bloom>();
            if (bloom != null)
            {
                bloom.threshold.Override(0.92f);
                bloom.intensity.Override(0.60f);
                bloom.scatter.Override(0.68f);
            }

            // Add Tonemapping
            var tonemapping = profile.Add<Tonemapping>();
            if (tonemapping != null)
            {
                tonemapping.mode.Override(TonemappingMode.ACES);
            }

            // Add Vignette
            var vignette = profile.Add<Vignette>();
            if (vignette != null)
            {
                vignette.intensity.Override(0.18f);
                vignette.smoothness.Override(0.45f);
            }

            // Add Color Adjustments
            var colorAdj = profile.Add<ColorAdjustments>();
            if (colorAdj != null)
            {
                colorAdj.postExposure.Override(0.08f);
                colorAdj.contrast.Override(10f);
                colorAdj.colorFilter.Override(new Color(1f, 0.97f, 0.94f));
                colorAdj.saturation.Override(6f);
            }

            string profilePath = MATERIAL_DIR + "/Profile_Cutscene_Sunset.asset";
            AssetDatabase.CreateAsset(profile, profilePath);
            volumeComp.profile = profile;

            // Full 360-degree Seamless Panoramic Sky Cylinder
            // Encompasses the entire world horizon with zero seams or corners in any direction!
            var panoMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Backdrop_Sky/M_Sunset_Sea_Horizon_Panorama.mat");
            Mesh curvedSkyMesh = CreateOrGetCurvedSkyMesh(150f, -28f, 88f, 0f, 360f, 48);

            GameObject skyCylinder = new GameObject("Sky_Panoramic_CurvedBackdrop");
            skyCylinder.transform.SetParent(envRoot.transform);
            skyCylinder.transform.position = Vector3.zero;
            // Rotate cylinder so that the magnificent sunset vista faces North/North-East towards the camera
            skyCylinder.transform.rotation = Quaternion.Euler(0f, -80f, 0f);
            var skyMf = skyCylinder.AddComponent<MeshFilter>();
            skyMf.sharedMesh = curvedSkyMesh;
            var skyMr = skyCylinder.AddComponent<MeshRenderer>();
            skyMr.sharedMaterial = panoMat;
            skyMr.shadowCastingMode = ShadowCastingMode.Off;
            skyMr.receiveShadows = false;

            // The Setting Sun (Single majestic sun on the North-East horizon)
            // Positioned low at Y = 4.2m so it rests gently above the ocean horizon
            var sunMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Backdrop_Sky/M_StylizedSun_Sunset.mat");
            if (sunMat != null)
            {
                sunMat.SetColor("_CoreColor", new Color(2.4f, 2.1f, 1.6f, 1.0f));
                sunMat.SetColor("_GlowColor", new Color(1.6f, 0.75f, 0.20f, 1.0f));
                sunMat.SetFloat("_CoreRadius", 0.16f);
                sunMat.SetFloat("_CoreSoftness", 0.15f);
                sunMat.SetFloat("_GlowRadius", 0.95f);
                sunMat.SetFloat("_GlowFalloff", 2.0f);
                sunMat.SetFloat("_HorizonFadeHeight", 0.16f);
                sunMat.SetFloat("_HorizonFadeSoftness", 0.14f);
                EditorUtility.SetDirty(sunMat);
            }

            GameObject sunDiscNorth = GameObject.CreatePrimitive(PrimitiveType.Quad);
            sunDiscNorth.name = "Sun_Disc_Sunset";
            sunDiscNorth.transform.SetParent(envRoot.transform);
            sunDiscNorth.transform.position = new Vector3(18f, 4.2f, 120f);
            sunDiscNorth.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            sunDiscNorth.transform.localScale = new Vector3(34f, 34f, 1f);
            Object.DestroyImmediate(sunDiscNorth.GetComponent<Collider>());
            if (sunMat != null) sunDiscNorth.GetComponent<MeshRenderer>().sharedMaterial = sunMat;

            // Veiling Sunset Watercolor Clouds (Layered horizontally across the sun)
            if (cloudMat != null)
            {
                GameObject cloudWisp1 = GameObject.CreatePrimitive(PrimitiveType.Quad);
                cloudWisp1.name = "Horizon_SunsetCloud_Lower";
                cloudWisp1.transform.SetParent(envRoot.transform);
                cloudWisp1.transform.position = new Vector3(18f, 2.4f, 117f);
                cloudWisp1.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                cloudWisp1.transform.localScale = new Vector3(54f, 11f, 1f);
                Object.DestroyImmediate(cloudWisp1.GetComponent<Collider>());
                cloudWisp1.GetComponent<MeshRenderer>().sharedMaterial = cloudMat;

                GameObject cloudWisp2 = GameObject.CreatePrimitive(PrimitiveType.Quad);
                cloudWisp2.name = "Horizon_SunsetCloud_Upper";
                cloudWisp2.transform.SetParent(envRoot.transform);
                cloudWisp2.transform.position = new Vector3(23f, 4.8f, 118f);
                cloudWisp2.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                cloudWisp2.transform.localScale = new Vector3(42f, 7.5f, 1f);
                Object.DestroyImmediate(cloudWisp2.GetComponent<Collider>());
                cloudWisp2.GetComponent<MeshRenderer>().sharedMaterial = cloudMat;
            }

            // 8. Build 3D Uber Water Surface
            GameObject waterRoot = new GameObject("=== 3D UBER WATER ===");
            var waterPlaneFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Shaders/Uber Stylized Water/Demo/Mesh/Water Plane.fbx");
            Mesh waterMesh = null;
            if (waterPlaneFbx != null)
            {
                var mf = waterPlaneFbx.GetComponentInChildren<MeshFilter>();
                if (mf != null) waterMesh = mf.sharedMesh;
            }

            // Create 4 expansive water planes with slight overlapping to avoid any seams
            float[] posX = new float[] { -120f, 120f, -120f, 120f };
            float[] posZ = new float[] { -20f, -20f, 140f, 140f };
            for (int i = 0; i < 4; i++)
            {
                GameObject waterTile = new GameObject($"UberWater_Surface_{i}");
                waterTile.transform.SetParent(waterRoot.transform);
                waterTile.transform.position = new Vector3(posX[i], -0.05f, posZ[i]);
                waterTile.transform.localScale = new Vector3(18f, 1f, 18f); // ~360m x 360m per quad
                var wmf = waterTile.AddComponent<MeshFilter>();
                wmf.sharedMesh = waterMesh;
                var wmr = waterTile.AddComponent<MeshRenderer>();
                wmr.sharedMaterial = waterMat;
                wmr.shadowCastingMode = ShadowCastingMode.Off;
                wmr.receiveShadows = true;
            }

            // 9. Build 3D Steamship
            GameObject shipRoot = new GameObject("Ship_Root");
            var shipFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Battleship/tripo_convert_a0f3e7e5-4dc9-4da6-8e6a-f58a7fe2a979.fbx");
            GameObject shipModel = null;
            if (shipFbx != null)
            {
                shipModel = Object.Instantiate(shipFbx, shipRoot.transform);
                shipModel.name = "Model_Ship_Mesh";
                // Scale 15x, raised waterline position and level trim so stern sits comfortably above water
                shipModel.transform.localPosition = new Vector3(0f, -0.05f, 0f);
                shipModel.transform.localRotation = Quaternion.Euler(0f, 0f, 1.5f);
                shipModel.transform.localScale = new Vector3(15f, 15f, 15f);

                var shipRenderer = shipModel.GetComponentInChildren<Renderer>();
                if (shipRenderer != null)
                {
                    shipRenderer.sharedMaterial = shipMat;
                    shipRenderer.shadowCastingMode = ShadowCastingMode.On;
                    shipRenderer.receiveShadows = true;
                }
            }

            // 10. Smokestack Smoke Particle System
            GameObject smokeGo = new GameObject("VFX_ChimneySmoke");
            smokeGo.transform.SetParent(shipRoot.transform);
            smokeGo.transform.localPosition = new Vector3(0f, 6.85f, 0f);
            smokeGo.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

            var ps = smokeGo.AddComponent<ParticleSystem>();
            var psr = smokeGo.GetComponent<ParticleSystemRenderer>();
            psr.sharedMaterial = smokeMat;
            psr.renderMode = ParticleSystemRenderMode.Billboard;
            psr.shadowCastingMode = ShadowCastingMode.Off;
            psr.receiveShadows = false;

            var mainModule = ps.main;
            mainModule.duration = 5f;
            mainModule.loop = true;
            mainModule.startLifetime = new ParticleSystem.MinMaxCurve(4.5f, 6.5f);
            mainModule.startSpeed = new ParticleSystem.MinMaxCurve(2.2f, 3.2f);
            mainModule.startSize = new ParticleSystem.MinMaxCurve(1.2f, 2.0f);
            mainModule.simulationSpace = ParticleSystemSimulationSpace.World;
            mainModule.maxParticles = 180;
            mainModule.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.96f, 0.90f, 0.82f, 0.85f),
                new Color(0.82f, 0.76f, 0.72f, 0.90f)
            );

            var emissionModule = ps.emission;
            emissionModule.rateOverTime = 18f;

            var shapeModule = ps.shape;
            shapeModule.shapeType = ParticleSystemShapeType.Cone;
            shapeModule.angle = 12f;
            shapeModule.radius = 0.35f;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve(
                new Keyframe(0f, 0.6f),
                new Keyframe(0.25f, 2.0f),
                new Keyframe(0.7f, 4.2f),
                new Keyframe(1f, 6.0f)
            );
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient colorGrad = new Gradient();
            colorGrad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(1.0f, 0.92f, 0.84f), 0.0f), // Golden sun highlight
                    new GradientColorKey(new Color(0.88f, 0.82f, 0.78f), 0.35f),
                    new GradientColorKey(new Color(0.70f, 0.64f, 0.62f), 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0.0f, 0.0f),
                    new GradientAlphaKey(0.90f, 0.12f),
                    new GradientAlphaKey(0.65f, 0.60f),
                    new GradientAlphaKey(0.0f, 1.0f)
                }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(colorGrad);

            var velOverLifetime = ps.velocityOverLifetime;
            velOverLifetime.enabled = true;
            velOverLifetime.space = ParticleSystemSimulationSpace.World;
            velOverLifetime.x = new ParticleSystem.MinMaxCurve(-1.2f, -0.4f); // Drift backwards (west)
            velOverLifetime.y = new ParticleSystem.MinMaxCurve(0.8f, 1.8f);   // Rise upwards
            velOverLifetime.z = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);

            // Water Wake Particles (Stern)
            GameObject sternWakeGo = new GameObject("VFX_SternWake");
            sternWakeGo.transform.SetParent(shipRoot.transform);
            sternWakeGo.transform.localPosition = new Vector3(-7.2f, 0.02f, 0f);
            var sternPs = sternWakeGo.AddComponent<ParticleSystem>();
            var sternPsr = sternWakeGo.GetComponent<ParticleSystemRenderer>();
            sternPsr.sharedMaterial = smokeMat;
            var sternMain = sternPs.main;
            sternMain.duration = 3f;
            sternMain.loop = true;
            sternMain.startLifetime = 3.0f;
            sternMain.startSpeed = 0.6f;
            sternMain.startSize = new ParticleSystem.MinMaxCurve(1.5f, 2.8f);
            sternMain.simulationSpace = ParticleSystemSimulationSpace.World;
            sternMain.startColor = new Color(1f, 0.95f, 0.88f, 0.45f);
            var sternEmission = sternPs.emission;
            sternEmission.rateOverTime = 10f;

            // Audio Sources
            var hornClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_Vintage_ShipHorn_1950s.mp3");
            var hornSource = shipRoot.AddComponent<AudioSource>();
            hornSource.clip = hornClip;
            hornSource.playOnAwake = false;
            hornSource.volume = 0.9f;
            hornSource.spatialBlend = 0.2f;

            var bgmClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Breath of the World.mp3");
            var bgmSource = shipRoot.AddComponent<AudioSource>();
            bgmSource.clip = bgmClip;
            bgmSource.playOnAwake = true;
            bgmSource.loop = true;
            bgmSource.volume = 0.65f;
            bgmSource.spatialBlend = 0f;

            // 11. Main Camera & Director Setup
            // SolidColor clear flags with muted watercolor zenith blue prevents any default skybox leaks
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.48f, 0.60f, 0.68f);
            cam.fieldOfView = 48f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 1000f;
            camGo.AddComponent<UniversalAdditionalCameraData>();
            camGo.AddComponent<AudioListener>();

            var director = camGo.AddComponent<EndGameCutsceneDirector>();

            // 12. UI Canvas ("TO BE CONTINUED")
            GameObject canvasGo = new GameObject("Cutscene_UI_Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1.0f;
            canvasGo.AddComponent<GraphicRaycaster>();
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // Letterbox Top
            GameObject lbTop = new GameObject("Letterbox_Top", typeof(RectTransform));
            lbTop.transform.SetParent(canvasGo.transform, false);
            var lbTopImg = lbTop.AddComponent<Image>();
            lbTopImg.color = Color.black;
            var lbTopRt = lbTop.GetComponent<RectTransform>();
            lbTopRt.anchorMin = new Vector2(0f, 1f);
            lbTopRt.anchorMax = new Vector2(1f, 1f);
            lbTopRt.pivot = new Vector2(0.5f, 1f);
            lbTopRt.sizeDelta = new Vector2(0f, 65f);
            lbTopRt.anchoredPosition = Vector2.zero;

            // Letterbox Bottom
            GameObject lbBottom = new GameObject("Letterbox_Bottom", typeof(RectTransform));
            lbBottom.transform.SetParent(canvasGo.transform, false);
            var lbBottomImg = lbBottom.AddComponent<Image>();
            lbBottomImg.color = Color.black;
            var lbBottomRt = lbBottom.GetComponent<RectTransform>();
            lbBottomRt.anchorMin = new Vector2(0f, 0f);
            lbBottomRt.anchorMax = new Vector2(1f, 0f);
            lbBottomRt.pivot = new Vector2(0.5f, 0f);
            lbBottomRt.sizeDelta = new Vector2(0f, 65f);
            lbBottomRt.anchoredPosition = Vector2.zero;

            // Title & Subtitle Panel (Positioned high in clear sky for maximum contrast)
            GameObject panelGo = new GameObject("Panel_ToBeContinued", typeof(RectTransform));
            panelGo.transform.SetParent(canvasGo.transform, false);
            var panelCg = panelGo.AddComponent<CanvasGroup>();
            panelCg.alpha = 0f;
            var panelRt = panelGo.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(1200f, 320f);
            panelRt.anchoredPosition = new Vector2(0f, 220f);

            // Feathered Cinematic Vignette Backdrop behind text (smooth contrast without harsh edges)
            Sprite backdropSprite = CreateOrGetCinematicBackdropSprite();
            if (backdropSprite != null)
            {
                GameObject bgGo = new GameObject("Cinematic_Text_Backdrop", typeof(RectTransform));
                bgGo.transform.SetParent(panelGo.transform, false);
                var bgImg = bgGo.AddComponent<Image>();
                bgImg.sprite = backdropSprite;
                bgImg.color = Color.white;
                var bgRt = bgGo.GetComponent<RectTransform>();
                bgRt.anchorMin = new Vector2(0.5f, 0.5f);
                bgRt.anchorMax = new Vector2(0.5f, 0.5f);
                bgRt.pivot = new Vector2(0.5f, 0.5f);
                bgRt.sizeDelta = new Vector2(1400f, 260f);
                bgRt.anchoredPosition = new Vector2(0f, -25f);
            }

            // Dynamic Font Asset with full Vietnamese diacritics support
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/SegoeUI_SDF_Dynamic.asset");
            if (fontAsset == null)
            {
                fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/SegoeUI_Bold_SDF.asset");
            }
            if (fontAsset == null)
            {
                fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            }

            // Material with solid dark outline and soft cinematic underlay shadow
            Material titleMat = CreateOrGetTitleMaterial(fontAsset);

            // Main Text: "CÒN TIẾP..." (Radiant warm gold, high-contrast, authentic Vietnamese)
            GameObject mainTextGo = new GameObject("Text_ToBeContinued", typeof(RectTransform));
            mainTextGo.transform.SetParent(panelGo.transform, false);
            var mainTmp = mainTextGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) mainTmp.font = fontAsset;
            if (titleMat != null) mainTmp.fontSharedMaterial = titleMat;
            mainTmp.text = "CÒN TIẾP...";
            mainTmp.fontSize = 80f;
            mainTmp.fontStyle = FontStyles.Bold;
            mainTmp.alignment = TextAlignmentOptions.Center;
            mainTmp.color = new Color(1.0f, 0.90f, 0.38f, 1.0f); // Radiant Imperial Gold
            mainTmp.characterSpacing = 10f;
            var mainRt = mainTextGo.GetComponent<RectTransform>();
            mainRt.anchorMin = new Vector2(0.5f, 0.5f);
            mainRt.anchorMax = new Vector2(0.5f, 0.5f);
            mainRt.pivot = new Vector2(0.5f, 0.5f);
            mainRt.sizeDelta = new Vector2(1000f, 100f);
            mainRt.anchoredPosition = new Vector2(0f, 30f);

            // Subtitle Text: "Hoa trên đất đỏ" (Crisp ivory white, bold, 100% legible)
            GameObject subTextGo = new GameObject("Text_Subtitle", typeof(RectTransform));
            subTextGo.transform.SetParent(panelGo.transform, false);
            var subTmp = subTextGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) subTmp.font = fontAsset;
            if (titleMat != null) subTmp.fontSharedMaterial = titleMat;
            subTmp.text = "HOA TRÊN ĐẤT ĐỎ  —  Hẹn gặp lại ở chặng đường phía trước";
            subTmp.fontSize = 25f;
            subTmp.fontStyle = FontStyles.Bold;
            subTmp.alignment = TextAlignmentOptions.Center;
            subTmp.color = new Color(1.0f, 0.98f, 0.94f, 1.0f);
            subTmp.characterSpacing = 3f;
            var subRt = subTextGo.GetComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0.5f, 0.5f);
            subRt.anchorMax = new Vector2(0.5f, 0.5f);
            subRt.pivot = new Vector2(0.5f, 0.5f);
            subRt.sizeDelta = new Vector2(1000f, 50f);
            subRt.anchoredPosition = new Vector2(0f, -46f);

            // Replay Hint (Clear warm white with bold weight)
            GameObject hintGo = new GameObject("Text_ReplayHint", typeof(RectTransform));
            hintGo.transform.SetParent(panelGo.transform, false);
            var hintTmp = hintGo.AddComponent<TextMeshProUGUI>();
            if (fontAsset != null) hintTmp.font = fontAsset;
            if (titleMat != null) hintTmp.fontSharedMaterial = titleMat;
            hintTmp.text = "[ Nhấn Space hoặc R để xem lại ]";
            hintTmp.fontSize = 17f;
            hintTmp.fontStyle = FontStyles.Bold;
            hintTmp.alignment = TextAlignmentOptions.Center;
            hintTmp.color = new Color(1.0f, 0.96f, 0.88f, 0.95f);
            hintTmp.characterSpacing = 2f;
            var hintRt = hintGo.GetComponent<RectTransform>();
            hintRt.anchorMin = new Vector2(0.5f, 0.5f);
            hintRt.anchorMax = new Vector2(0.5f, 0.5f);
            hintRt.pivot = new Vector2(0.5f, 0.5f);
            hintRt.sizeDelta = new Vector2(600f, 35f);
            hintRt.anchoredPosition = new Vector2(0f, -86f);

            // Wire Director Inspector References via SerializedObject
            SerializedObject so = new SerializedObject(director);
            so.FindProperty("shipTransform").objectReferenceValue = shipRoot.transform;
            so.FindProperty("smokeParticleSystem").objectReferenceValue = ps;
            so.FindProperty("sternWakeParticles").objectReferenceValue = sternPs;
            so.FindProperty("cutsceneCamera").objectReferenceValue = cam;
            so.FindProperty("shipHornSource").objectReferenceValue = hornSource;
            so.FindProperty("bgmSource").objectReferenceValue = bgmSource;
            so.FindProperty("uiCanvasGroup").objectReferenceValue = panelCg;
            so.FindProperty("letterboxTop").objectReferenceValue = lbTopRt;
            so.FindProperty("letterboxBottom").objectReferenceValue = lbBottomRt;
            so.FindProperty("toBeContinuedText").objectReferenceValue = mainTmp;
            so.FindProperty("subtitleText").objectReferenceValue = subTmp;
            so.FindProperty("restartHintText").objectReferenceValue = hintTmp;
            so.ApplyModifiedProperties();

            // Evaluate starting pose
            director.EvaluateCutscene(0f);

            // 13. Save Scene
            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            Debug.Log($"<color=green>[Cutscene Setup] Successfully created End Game Cutscene Scene at: {SCENE_PATH}</color>");
        }

        private static Mesh CreateOrGetCurvedSkyMesh(float radius, float heightBottom, float heightTop, float startAngleDeg, float endAngleDeg, int segments)
        {
            string meshAssetPath = MATERIAL_DIR + "/CurvedSkyMesh.asset";
            Mesh existingMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshAssetPath);
            if (existingMesh != null)
            {
                AssetDatabase.DeleteAsset(meshAssetPath);
            }

            Mesh mesh = new Mesh();
            mesh.name = "CurvedSkyMesh";

            Vector3[] vertices = new Vector3[(segments + 1) * 2];
            Vector2[] uvs = new Vector2[(segments + 1) * 2];
            int[] triangles = new int[segments * 6];

            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float angleRad = Mathf.Lerp(startAngleDeg, endAngleDeg, t) * Mathf.Deg2Rad;
                float x = Mathf.Sin(angleRad) * radius;
                float z = Mathf.Cos(angleRad) * radius;

                int vBottom = i * 2;
                int vTop = i * 2 + 1;

                vertices[vBottom] = new Vector3(x, heightBottom, z);
                vertices[vTop] = new Vector3(x, heightTop, z);

                uvs[vBottom] = new Vector2(t, 0f);
                uvs[vTop] = new Vector2(t, 1f);

                if (i < segments)
                {
                    int triIdx = i * 6;
                    // Inward facing triangles
                    triangles[triIdx + 0] = vBottom;
                    triangles[triIdx + 1] = vTop;
                    triangles[triIdx + 2] = vBottom + 2;

                    triangles[triIdx + 3] = vTop;
                    triangles[triIdx + 4] = vTop + 2;
                    triangles[triIdx + 5] = vBottom + 2;
                }
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            AssetDatabase.CreateAsset(mesh, meshAssetPath);
            return mesh;
        }

        private static Material CreateOrGetShipMaterial()
        {
            string matPath = MATERIAL_DIR + "/M_Cutscene_SteamShip.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, matPath);
            }

            var baseTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Models/Battleship/tripo_convert_a0f3e7e5-4dc9-4da6-8e6a-f58a7fe2a979.fbm/tripo_node_01e4edec-41ad-402e-a67f-a395c55a5e3d_BaseColor.png");
            var normTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Models/Battleship/tripo_convert_a0f3e7e5-4dc9-4da6-8e6a-f58a7fe2a979.fbm/tripo_node_01e4edec-41ad-402e-a67f-a395c55a5e3d_Normal_Bake.png");
            var metTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Models/Battleship/tripo_convert_a0f3e7e5-4dc9-4da6-8e6a-f58a7fe2a979.fbm/battleship_3d_model_metallic.PNG");

            if (baseTex != null) mat.SetTexture("_BaseMap", baseTex);
            if (normTex != null)
            {
                mat.SetTexture("_BumpMap", normTex);
                mat.EnableKeyword("_NORMALMAP");
            }
            if (metTex != null)
            {
                mat.SetTexture("_MetallicGlossMap", metTex);
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            }
            mat.SetFloat("_Smoothness", 0.35f);
            mat.SetColor("_BaseColor", new Color(0.95f, 0.88f, 0.82f));
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateOrGetWaterMaterial()
        {
            string matPath = MATERIAL_DIR + "/M_Cutscene_UberWater_Sunset.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                var template = AssetDatabase.LoadAssetAtPath<Material>("Assets/Shaders/Uber Stylized Water/Template Materials/UWa-Template-Wavy.mat");
                if (template != null)
                {
                    mat = new Material(template);
                }
                else
                {
                    mat = new Material(Shader.Find("UberStylizedWater"));
                }
                AssetDatabase.CreateAsset(mat, matPath);
            }

            // Tune Uber Water properties for serene, painterly watercolor sunset aesthetic
            mat.SetColor("_Color_Shallow", new Color(0.76f, 0.48f, 0.30f, 0.85f)); // Warm golden peach reflection
            mat.SetColor("_Color_Deep", new Color(0.16f, 0.20f, 0.24f, 1.0f));     // Serene twilight slate-indigo
            mat.SetColor("_Wave_Top_Color", new Color(0.85f, 0.60f, 0.40f, 0.0f));  // Disabled harsh top stripe lines (alpha 0)
            mat.SetColor("_Specular_Color", new Color(0.92f, 0.70f, 0.42f, 1.0f));  // Soft warm golden sun shimmer (no harsh white blowout)
            mat.SetColor("_SurfFoam_Color", new Color(1.0f, 0.92f, 0.80f, 0.35f)); // Soft translucent foam
            mat.SetColor("_InterSec_Color", new Color(1.0f, 0.88f, 0.65f, 0.75f)); // Hull contact foam

            // Disable caustics (which create noisy zebra lines) and surface foam dots
            mat.SetFloat("_ENABLECAUSTICS", 0.0f);
            mat.SetFloat("_Enable_SurfaceFoam", 0.0f);
            mat.SetFloat("_ENABLEWAVE", 0.0f); // Clean, natural undulating ocean surface without artificial sine stripes

            // Tightly focused, gentle specular reflection column directly beneath the sun
            mat.SetFloat("_Specular_Hardness", 12.0f);
            mat.SetFloat("_Specular_Spread", 0.18f);
            mat.SetFloat("_Specular_Size", 0.70f);

            // Gentle, broad rolling swells
            mat.SetFloat("_ENABLENORMAL", 1.0f);
            mat.SetFloat("_Normal_Scale", 1.5f);
            mat.SetFloat("_Normal_Strength", 0.15f);
            mat.SetFloat("_Normal_Pan", 1.5f);

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateOrGetSmokeMaterial()
        {
            string matPath = MATERIAL_DIR + "/M_Cutscene_SoftSmoke.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
                mat = new Material(shader);

                var smokeTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Sky/soft_particle.png");
                if (smokeTex == null) smokeTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Clouds/cumulus_cloud_02.png");
                if (smokeTex != null) mat.SetTexture("_BaseMap", smokeTex);

                mat.SetFloat("_Surface", 1f); // Transparent
                mat.SetFloat("_Blend", 0f);   // Alpha blend
                mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = (int)RenderQueue.Transparent;
                mat.SetColor("_BaseColor", new Color(1f, 1f, 1f, 1f));

                AssetDatabase.CreateAsset(mat, matPath);
            }
            return mat;
        }

        private static Material CreateOrGetHorizonCloudMaterial()
        {
            string matPath = MATERIAL_DIR + "/M_Cutscene_GoldenHorizonCloud.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                mat = new Material(shader);
                var cloudTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Lana_Unified_Art/Clouds/T_Lana_Cloud_04_GoldenHorizon.png");
                if (cloudTex != null) mat.SetTexture("_BaseMap", cloudTex);

                mat.SetFloat("_Surface", 1f); // Transparent
                mat.SetFloat("_Blend", 0f);   // Alpha blend
                mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = 3000;
                mat.SetColor("_BaseColor", new Color(1.0f, 0.85f, 0.68f, 0.80f));

                AssetDatabase.CreateAsset(mat, matPath);
            }
            else
            {
                mat.SetColor("_BaseColor", new Color(1.0f, 0.85f, 0.68f, 0.80f));
                EditorUtility.SetDirty(mat);
            }
            return mat;
        }

        private static Sprite CreateOrGetCinematicBackdropSprite()
        {
            string gradPath = "Assets/Textures/UI/T_Cinematic_Title_Backdrop.png";
            Sprite existingSprite = AssetDatabase.LoadAssetAtPath<Sprite>(gradPath);
            if (existingSprite != null) return existingSprite;

            if (!Directory.Exists("Assets/Textures/UI"))
            {
                Directory.CreateDirectory("Assets/Textures/UI");
            }

            int width = 512;
            int height = 128;
            Texture2D gradTex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
            {
                float normY = (float)y / (height - 1);
                float alphaY = Mathf.Sin(normY * Mathf.PI);
                alphaY = Mathf.Pow(alphaY, 1.5f);

                for (int x = 0; x < width; x++)
                {
                    float normX = (float)x / (width - 1);
                    float alphaX = Mathf.Sin(normX * Mathf.PI);
                    alphaX = Mathf.Pow(alphaX, 0.4f);

                    float a = alphaY * alphaX * 0.45f;
                    gradTex.SetPixel(x, y, new Color(0.04f, 0.02f, 0.02f, a));
                }
            }
            gradTex.Apply();

            File.WriteAllBytes(gradPath, gradTex.EncodeToPNG());
            AssetDatabase.ImportAsset(gradPath);

            TextureImporter importer = AssetImporter.GetAtPath(gradPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(gradPath);
        }

        private static Material CreateOrGetTitleMaterial(TMP_FontAsset font)
        {
            if (font == null) return null;
            string matPath = MATERIAL_DIR + "/M_TMP_Title_Cinematic.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(font.material);
                mat.name = "M_TMP_Title_Cinematic";
                AssetDatabase.CreateAsset(mat, matPath);
            }

            mat.EnableKeyword("OUTLINE_ON");
            mat.EnableKeyword("UNDERLAY_ON");
            mat.SetColor("_OutlineColor", new Color(0.06f, 0.03f, 0.01f, 1.0f));
            mat.SetFloat("_OutlineWidth", 0.16f);
            mat.SetFloat("_FaceDilate", 0.16f);
            mat.SetColor("_UnderlayColor", new Color(0f, 0f, 0f, 0.92f));
            mat.SetFloat("_UnderlayOffsetX", 0.8f);
            mat.SetFloat("_UnderlayOffsetY", -0.8f);
            mat.SetFloat("_UnderlayDilate", 0.25f);
            mat.SetFloat("_UnderlaySoftness", 0.35f);

            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}