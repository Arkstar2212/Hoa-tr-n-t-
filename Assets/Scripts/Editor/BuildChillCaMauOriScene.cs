using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Animations;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using HoaVaMau.Gameplay;
using HoaVaMau.Environment;

namespace HoaVaMau.Editor
{
    public static class BuildChillCaMauOriScene
    {
        public const string ScenePath = "Assets/Scenes/Scene_CaMau_OriWater_Chill.unity";
        private const string MaterialsPath = "Assets/Materials";
        private const string PrefabsPath = "Assets/Prefabs";

        [MenuItem("Hoa va Mau/Build Chill Ca Mau Ori Scene")]
        public static void BuildScene()
        {
            if (!Directory.Exists("Assets/Scenes")) Directory.CreateDirectory("Assets/Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ==========================================
            // ROOT GAMEOBJECT
            // ==========================================
            GameObject root = new GameObject("SCENE_CAMAU_ORI_CHILL");

            // ==========================================
            // 1. LIGHTING (SUNSET SUN & SOFT AMBIENT)
            // ==========================================
            GameObject lightingGroup = CreateChild(root, "Lighting");

            GameObject dirLightObj = new GameObject("Sun_DirectionalLight");
            dirLightObj.transform.SetParent(lightingGroup.transform, false);
            dirLightObj.transform.rotation = Quaternion.Euler(18f, -28f, 0f);

            Light dirLight = dirLightObj.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.color = new Color(1.0f, 0.92f, 0.82f);
            dirLight.intensity = 1.35f;
            dirLight.shadows = LightShadows.Soft;
            dirLight.shadowStrength = 0.65f;
            dirLight.shadowNormalBias = 0.4f;

            var urpLight = dirLightObj.AddComponent<UniversalAdditionalLightData>();
            urpLight.usePipelineSettings = true;

            // Ambient Twilight
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.25f, 0.52f, 0.70f); // Bright blue sky
            RenderSettings.ambientEquatorColor = new Color(0.85f, 0.65f, 0.45f); // Warm golden horizon
            RenderSettings.ambientGroundColor = new Color(0.08f, 0.22f, 0.28f); // Deep oceanic ground
            RenderSettings.ambientIntensity = 1.1f;

            // ==========================================
            // 2. CAMERA (CINEMATIC 2.5D MENU VIEW)
            // ==========================================
            GameObject cameraGroup = CreateChild(root, "Camera");
            GameObject camObj = new GameObject("CinematicMainCamera");
            camObj.tag = "MainCamera";
            camObj.transform.SetParent(cameraGroup.transform, false);
            camObj.transform.position = new Vector3(-0.6f, 1.45f, -16.5f);
            camObj.transform.rotation = Quaternion.Euler(2.8f, 0f, 0f);

            Camera cam = camObj.AddComponent<Camera>();
            cam.orthographic = false;
            cam.fieldOfView = 27.5f;
            cam.nearClipPlane = 0.2f;
            cam.farClipPlane = 180f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.20f, 0.50f, 0.68f);

            var urpCam = camObj.AddComponent<UniversalAdditionalCameraData>();
            urpCam.renderPostProcessing = true;
            urpCam.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            camObj.AddComponent<AudioListener>();
            camObj.AddComponent<CinematicMenuCamera>();

            // ==========================================
            // 3. BACKGROUND (CA MAU SKY, GIANT CLOUD, SUN & GOD RAYS)
            // ==========================================
            GameObject bgGroup = CreateChild(root, "Background_Atmosphere");

            // Sky Gradient Backdrop
            Material skyMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/M_StylizedSky_Sunset.mat");
            if (skyMat != null)
            {
                skyMat.SetColor("_HorizonColor", new Color(0.85f, 0.92f, 0.75f, 1.0f));
                skyMat.SetColor("_MidSkyColor", new Color(0.32f, 0.62f, 0.82f, 1.0f));
                skyMat.SetColor("_ZenithColor", new Color(0.12f, 0.35f, 0.65f, 1.0f));
                skyMat.SetColor("_HorizonBandColor", new Color(1.0f, 0.95f, 0.80f, 1.0f));
                EditorUtility.SetDirty(skyMat);

                GameObject skyBackdrop = GameObject.CreatePrimitive(PrimitiveType.Quad);
                skyBackdrop.name = "Sky_Backdrop_Quad";
                skyBackdrop.transform.SetParent(bgGroup.transform, false);
                skyBackdrop.transform.position = new Vector3(0f, 10f, 95f);
                skyBackdrop.transform.localScale = new Vector3(180f, 80f, 1f);
                skyBackdrop.GetComponent<Renderer>().sharedMaterial = skyMat;
                Object.DestroyImmediate(skyBackdrop.GetComponent<Collider>());
            }

            // Giant Cumulus Cloud Formation (Authentic Ca Mau seascape)
            Texture2D cumulusTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Clouds/cumulus_cloud_02.png");
            if (cumulusTex == null) cumulusTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Clouds/lana_cloud_01.png");
            if (cumulusTex != null)
            {
                Material cloudMat = CreateTransparentSpriteMat("M_CaMau_CumulusCloud", cumulusTex);
                GameObject cloudObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
                cloudObj.name = "CaMau_Giant_CumulusCloud";
                cloudObj.transform.SetParent(bgGroup.transform, false);
                cloudObj.transform.position = new Vector3(4.5f, 7.5f, 62f);
                cloudObj.transform.localScale = new Vector3(48f, 26f, 1f);
                cloudObj.GetComponent<Renderer>().sharedMaterial = cloudMat;
                Object.DestroyImmediate(cloudObj.GetComponent<Collider>());
            }

            // Glowing Sun Disc (Centered at X = 3.5, casting the golden reflection path)
            GameObject sunObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            sunObj.name = "Sunset_Sun_Disc";
            sunObj.transform.SetParent(bgGroup.transform, false);
            sunObj.transform.position = new Vector3(3.5f, 4.8f, 60f);
            sunObj.transform.localScale = new Vector3(7.5f, 7.5f, 1f);
            Material sunMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/M_StylizedSun_Sunset.mat");
            if (sunMat != null) sunObj.GetComponent<Renderer>().sharedMaterial = sunMat;
            Object.DestroyImmediate(sunObj.GetComponent<Collider>());

            // Crepuscular God Rays (Sunbeams streaming down onto water)
            Material godRaysMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/M_StylizedGodRays.mat");
            if (godRaysMat != null)
            {
                godRaysMat.SetColor("_RayColor", new Color(1.0f, 0.94f, 0.75f, 0.32f));
                godRaysMat.SetFloat("_RayIntensity", 1.15f);
                godRaysMat.SetFloat("_BeamCount", 5.0f);
                EditorUtility.SetDirty(godRaysMat);

                GameObject raysObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
                raysObj.name = "Sunbeams_GodRays";
                raysObj.transform.SetParent(bgGroup.transform, false);
                raysObj.transform.position = new Vector3(3.5f, 4.2f, 48f);
                raysObj.transform.rotation = Quaternion.Euler(0f, 0f, -8f);
                raysObj.transform.localScale = new Vector3(32f, 18f, 1f);
                raysObj.GetComponent<Renderer>().sharedMaterial = godRaysMat;
                Object.DestroyImmediate(raysObj.GetComponent<Collider>());
            }

            // Distant Mangrove Horizon Ridge
            Material mangroveLeafMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/Environment/CaMau/M_CaMau_MangroveLeaves.mat");
            if (mangroveLeafMat != null)
            {
                GameObject ridge = new GameObject("Distant_Mangrove_Horizon");
                ridge.transform.SetParent(bgGroup.transform, false);
                ridge.transform.position = new Vector3(0f, -0.6f, 52f);
                for (float x = -35f; x <= 35f; x += 4.5f)
                {
                    GameObject clump = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    clump.name = "Horizon_Tree";
                    clump.transform.SetParent(ridge.transform, false);
                    clump.transform.localPosition = new Vector3(x, Random.Range(-0.1f, 0.2f), 0f);
                    clump.transform.localScale = new Vector3(Random.Range(5f, 7f), Random.Range(1.2f, 1.8f), 2f);
                    clump.GetComponent<Renderer>().sharedMaterial = mangroveLeafMat;
                    Object.DestroyImmediate(clump.GetComponent<Collider>());
                }
            }

            // Distant Sailing Boats (Vietnamese Junks in Ca Mau)
            GameObject boatPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Vehicles/PF_StylizedBoat.prefab");
            if (boatPrefab != null)
            {
                GameObject boatsGroup = CreateChild(bgGroup, "Distant_SailBoats");
                Vector3[] boatPos = new Vector3[] {
                    new Vector3(4.2f, -0.72f, 26f),   // Right in the golden sun path!
                    new Vector3(10.5f, -0.68f, 38f),  // Gliding to the right
                    new Vector3(-8.0f, -0.70f, 34f)   // Distant left
                };
                float[] boatScales = new float[] { 0.75f, 0.55f, 0.60f };
                for (int i = 0; i < boatPos.Length; i++)
                {
                    var b = (GameObject)PrefabUtility.InstantiatePrefab(boatPrefab, boatsGroup.transform);
                    b.name = $"SailBoat_{i + 1}";
                    b.transform.position = boatPos[i];
                    b.transform.rotation = Quaternion.Euler(0f, 95f, 0f);
                    b.transform.localScale = Vector3.one * boatScales[i];
                    var floatComp = b.AddComponent<FloatingBoatSimple>();
                }
            }

            // ==========================================
            // 4. WATER (ORI-STYLE 2.5D WATER + CA MAU SUN REFLECTION)
            // ==========================================
            GameObject waterGroup = CreateChild(root, "Water_Ori_CaMau");

            // Setup Ori Water Material
            Shader oriWaterShader = Shader.Find("HoaVaMau/OriStylizedWater2D");
            if (oriWaterShader == null) oriWaterShader = Shader.Find("Universal Render Pipeline/Lit");

            string oriWaterMatPath = $"{MaterialsPath}/M_OriWater_CaMau.mat";
            Material oriWaterMat = AssetDatabase.LoadAssetAtPath<Material>(oriWaterMatPath);
            if (oriWaterMat == null)
            {
                oriWaterMat = new Material(oriWaterShader) { name = "M_OriWater_CaMau" };
                AssetDatabase.CreateAsset(oriWaterMat, oriWaterMatPath);
            }
            else
            {
                oriWaterMat.shader = oriWaterShader;
            }

            // Configure vibrant Ori + Ca Mau palette
            oriWaterMat.SetColor("_ShallowColor", new Color(0.12f, 0.76f, 0.86f, 0.90f)); // Bright tropical turquoise
            oriWaterMat.SetColor("_MidColor", new Color(0.06f, 0.44f, 0.62f, 0.95f));     // Coastal azure
            oriWaterMat.SetColor("_DeepColor", new Color(0.02f, 0.14f, 0.22f, 0.98f));    // Oceanic navy
            oriWaterMat.SetFloat("_DepthGradientPower", 1.35f);

            // Signature Ori Luminous Line
            oriWaterMat.SetColor("_SurfaceLineColor", new Color(0.40f, 1.95f, 2.10f, 1.0f)); // Radiant glowing cyan
            oriWaterMat.SetFloat("_SurfaceLineWidth", 0.042f);
            oriWaterMat.SetFloat("_SurfaceGlowIntensity", 2.85f);
            oriWaterMat.SetFloat("_SurfacePulseSpeed", 1.4f);

            // Sun Reflection Column
            oriWaterMat.SetColor("_SunReflectionColor", new Color(2.35f, 1.98f, 1.35f, 1.0f)); // Golden liquid light
            oriWaterMat.SetFloat("_SunWorldX", 3.5f);
            oriWaterMat.SetFloat("_ReflectionWidth", 5.2f);
            oriWaterMat.SetFloat("_ReflectionIntensity", 4.2f);
            oriWaterMat.SetFloat("_ReflectionFalloff", 2.2f);
            oriWaterMat.SetFloat("_SparkleStrength", 0.70f);

            // Underwater Caustic Rays
            oriWaterMat.SetColor("_CausticColor", new Color(0.40f, 1.25f, 1.45f, 0.50f));
            oriWaterMat.SetFloat("_CausticScale", 4.2f);
            oriWaterMat.SetFloat("_CausticSpeed", 1.15f);

            // Wave vertex displacement
            oriWaterMat.SetFloat("_WaveSpeed", 1.4f);
            oriWaterMat.SetFloat("_WaveFrequency", 0.85f);
            oriWaterMat.SetFloat("_WaveHeight", 0.038f);
            EditorUtility.SetDirty(oriWaterMat);

            // Ori Water Vertical Cross-Section Plane (Facing Camera at Z = 0)
            // Surface line at Y = -0.8, bottom at Y = -4.5, width from -30 to +30
            GameObject oriWaterCrossSection = CreateSubdividedWaterQuad("OriWater_CrossSection_2D", 60f, 3.8f, 40, 6);
            oriWaterCrossSection.transform.SetParent(waterGroup.transform, false);
            oriWaterCrossSection.transform.position = new Vector3(0f, -2.7f, 0.05f);
            oriWaterCrossSection.GetComponent<Renderer>().sharedMaterial = oriWaterMat;

            // Horizontal Top Water Surface extending into the depth (Z = 0 to 65)
            Material surfaceWaterMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/M_StylizedWater_Sunset.mat");
            if (surfaceWaterMat != null)
            {
                surfaceWaterMat.SetColor("_ShallowColor", new Color(0.12f, 0.76f, 0.86f, 0.88f));
                surfaceWaterMat.SetColor("_DeepColor", new Color(0.06f, 0.44f, 0.62f, 0.95f));
                surfaceWaterMat.SetColor("_HorizonWaterColor", new Color(0.92f, 0.82f, 0.60f, 1.0f));
                surfaceWaterMat.SetColor("_SunReflectionColor", new Color(2.35f, 1.98f, 1.35f, 1.0f));
                surfaceWaterMat.SetFloat("_SunWorldX", 3.5f);
                surfaceWaterMat.SetFloat("_NearZ", -2.0f);
                surfaceWaterMat.SetFloat("_FarZ", 60.0f);
                surfaceWaterMat.SetFloat("_ReflectionWidth", 5.5f);
                surfaceWaterMat.SetFloat("_ReflectionIntensity", 3.2f);
                surfaceWaterMat.SetFloat("_FoamDepth", 0.0f);
                EditorUtility.SetDirty(surfaceWaterMat);

                GameObject waterTopPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
                waterTopPlane.name = "Water_TopSurface_3D";
                waterTopPlane.transform.SetParent(waterGroup.transform, false);
                waterTopPlane.transform.position = new Vector3(0f, -0.80f, 30f);
                waterTopPlane.transform.localScale = new Vector3(6.5f, 1f, 6.5f);
                waterTopPlane.GetComponent<Renderer>().sharedMaterial = surfaceWaterMat;
                Object.DestroyImmediate(waterTopPlane.GetComponent<Collider>());
            }

            // Underwater Dark Riverbed (Deep floor at Y = -4.5)
            Material riverMudMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/Environment/CaMau/M_CaMau_RiverMud.mat");
            if (riverMudMat == null) riverMudMat = GetOrCreateMaterial("M_CaMau_RiverMud", new Color(0.04f, 0.10f, 0.14f), 0.3f);
            GameObject seabed = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seabed.name = "Undersea_Seabed_Floor";
            seabed.transform.SetParent(waterGroup.transform, false);
            seabed.transform.position = new Vector3(0f, -4.8f, 25f);
            seabed.transform.localScale = new Vector3(120f, 1.2f, 90f);
            seabed.GetComponent<Renderer>().sharedMaterial = riverMudMat;
            Object.DestroyImmediate(seabed.GetComponent<Collider>());

            // Submerged Mangrove Roots / Estuary Plants emerging from the water
            GameObject mangroveRootPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Vegetation/PF_FG_Mangrove_Root.prefab");
            if (mangroveRootPrefab != null)
            {
                float[] rootX = new float[] { -9.5f, -8.2f, 12.0f, 13.5f };
                for (int i = 0; i < rootX.Length; i++)
                {
                    var r = (GameObject)PrefabUtility.InstantiatePrefab(mangroveRootPrefab, waterGroup.transform);
                    r.name = $"Submerged_MangroveRoot_{i + 1}";
                    r.transform.position = new Vector3(rootX[i], -1.15f, Random.Range(1.5f, 3.5f));
                    r.transform.rotation = Quaternion.Euler(0f, (i * 65f) % 360f, 0f);
                    r.transform.localScale = Vector3.one * Random.Range(0.85f, 1.15f);
                }
            }

            // ==========================================
            // 5. ROCK PLATFORM (VÁCH NÚI / MỎM ĐÁ AN NGỒI CHILL)
            // ==========================================
            GameObject cliffGroup = CreateChild(root, "Cliff_Platform_An");

            // Main Cliff Platform Boulders
            GameObject rockLargePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Environment/PF_Rock_Large_01.prefab");
            GameObject mossyRockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Environment/PF_MossyBoulder_01.prefab");
            GameObject loneRockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Meshy_AI_Lone_Boulder_0928161951_texture.prefab");

            // Base Plateau Rock (Forming the overlooking cliff)
            if (rockLargePrefab != null)
            {
                var mainCliff = (GameObject)PrefabUtility.InstantiatePrefab(rockLargePrefab, cliffGroup.transform);
                mainCliff.name = "Main_Cliff_Plateau";
                mainCliff.transform.position = new Vector3(-4.2f, -0.2f, 0.2f);
                mainCliff.transform.rotation = Quaternion.Euler(5f, 140f, -8f);
                mainCliff.transform.localScale = new Vector3(2.4f, 1.8f, 2.2f);
            }

            // Upper Perch Rock (Where NPC stands looking out)
            if (mossyRockPrefab != null)
            {
                var upperPerch = (GameObject)PrefabUtility.InstantiatePrefab(mossyRockPrefab, cliffGroup.transform);
                upperPerch.name = "Upper_Lookout_Rock";
                upperPerch.transform.position = new Vector3(-5.8f, 0.8f, 0.6f);
                upperPerch.transform.rotation = Quaternion.Euler(-10f, 45f, 12f);
                upperPerch.transform.localScale = new Vector3(1.6f, 1.4f, 1.5f);
            }

            // Front Sitting Ledge Rock (Where An sits with legs relaxing)
            if (loneRockPrefab != null)
            {
                var frontLedge = (GameObject)PrefabUtility.InstantiatePrefab(loneRockPrefab, cliffGroup.transform);
                frontLedge.name = "An_Sitting_Ledge_Rock";
                frontLedge.transform.position = new Vector3(-2.8f, 0.35f, -0.1f);
                frontLedge.transform.rotation = Quaternion.Euler(4f, 65f, 5f);
                frontLedge.transform.localScale = new Vector3(0.55f, 0.45f, 0.50f);
            }

            // Decorative 3D Grass Tufts on the cliff ledge
            GameObject grassPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Foliage/PF_StylizedGrassTuft_3D.prefab");
            if (grassPrefab != null)
            {
                Vector3[] grassPositions = new Vector3[] {
                    new Vector3(-3.8f, 1.38f, 0.1f),
                    new Vector3(-4.6f, 1.45f, -0.3f),
                    new Vector3(-2.2f, 0.72f, 0.4f),
                    new Vector3(-5.1f, 2.25f, 0.5f),
                    new Vector3(-6.2f, 2.15f, 0.9f)
                };
                for (int i = 0; i < grassPositions.Length; i++)
                {
                    var g = (GameObject)PrefabUtility.InstantiatePrefab(grassPrefab, cliffGroup.transform);
                    g.name = $"Cliff_Grass_{i + 1}";
                    g.transform.position = grassPositions[i];
                    g.transform.rotation = Quaternion.Euler(0f, (i * 70f) % 360f, 0f);
                    g.transform.localScale = Vector3.one * Random.Range(0.8f, 1.1f);
                }
            }

            // Framing Hero Tree Branch leaning over the cliff
            GameObject treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Environment/Trees/PF_Curved_Hero_Tree_Knoll.prefab");
            if (treePrefab == null) treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Environment/Trees/PF_Lana_Artistic_Hero_Tree.prefab");
            if (treePrefab != null)
            {
                var framingTree = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab, cliffGroup.transform);
                framingTree.name = "Framing_Hero_Tree";
                framingTree.transform.position = new Vector3(-7.5f, 0.6f, 1.8f);
                framingTree.transform.rotation = Quaternion.Euler(0f, 25f, -6f);
                framingTree.transform.localScale = Vector3.one * 0.95f;
            }

            // ==========================================
            // 6. HERO AN (SITTING ON PLATFORM CHILLING)
            // ==========================================
            GameObject heroAn = SpawnHeroAnChill(root.transform, new Vector3(-2.85f, 1.05f, 0.0f));

            // ==========================================
            // 7. SECONDARY NPC (OBSERVING IN IDLE MENU FASHION)
            // ==========================================
            GameObject npcObserver = SpawnNPCObserver(root.transform, new Vector3(-5.5f, 2.15f, 0.7f));

            // ==========================================
            // 8. ATMOSPHERIC PARTICLES (GOLDEN DUST & SEA MIST)
            // ==========================================
            GameObject goldenDustPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/VFX/PF_Atmospheric_GoldenDust.prefab");
            if (goldenDustPrefab != null)
            {
                var dust = (GameObject)PrefabUtility.InstantiatePrefab(goldenDustPrefab, root.transform);
                dust.name = "Atmospheric_GoldenDust_VFX";
                dust.transform.position = new Vector3(0f, 2.5f, 5.0f);
            }

            // Save and finalize scene
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log($"<color=#10b981>[Chill Scene]</color> Scene successfully created at: {ScenePath}");
        }

        private static GameObject SpawnHeroAnChill(Transform parent, Vector3 spawnPos)
        {
            GameObject charPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Characters/Character_Rigged_Animations.fbx");
            if (charPrefab == null) charPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Characters/Character_An.fbx");
            if (charPrefab == null) return null;

            GameObject anObj = Object.Instantiate(charPrefab, parent);
            anObj.name = "Hero_An_Chill";
            anObj.transform.position = spawnPos;
            anObj.transform.rotation = Quaternion.Euler(0f, 75f, 0f); // Looking towards the sea & sun
            anObj.transform.localScale = Vector3.one * 0.95f;

            // Character Material
            ApplyCharacterMaterial(anObj, "M_HeroBoy_Lit");

            // Setup Animator with Sit Idle Loop
            var animator = anObj.GetComponent<Animator>();
            if (animator == null) animator = anObj.AddComponent<Animator>();

            RuntimeAnimatorController anController = GetOrCreateAnChillController();
            if (anController != null) animator.runtimeAnimatorController = anController;

            // Add An Chill Controller (procedural looking around & breathing)
            anObj.AddComponent<AnChillIdleController>();

            return anObj;
        }

        private static GameObject SpawnNPCObserver(Transform parent, Vector3 spawnPos)
        {
            GameObject npcModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Characters/Character_Test.fbx");
            if (npcModel == null) npcModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Characters/Character_Rigged_Animations.fbx");
            if (npcModel == null) return null;

            GameObject npcObj = Object.Instantiate(npcModel, parent);
            npcObj.name = "NPC_Observer_Companion";
            npcObj.transform.position = spawnPos;
            npcObj.transform.rotation = Quaternion.Euler(0f, 85f, 0f); // Standing looking out at the ocean
            npcObj.transform.localScale = Vector3.one * 0.95f;

            ApplyCharacterMaterial(npcObj, "M_NPC_Observer");

            var animator = npcObj.GetComponent<Animator>();
            if (animator == null) animator = npcObj.AddComponent<Animator>();

            RuntimeAnimatorController npcController = GetOrCreateNPCObserverController();
            if (npcController != null) animator.runtimeAnimatorController = npcController;

            // Add NPC Observer Controller (weight shifting & horizon scanning)
            npcObj.AddComponent<NPCMenuObserver>();

            return npcObj;
        }

        private static RuntimeAnimatorController GetOrCreateAnChillController()
        {
            string dir = "Assets/Animations/Controllers";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            string path = $"{dir}/AC_ChillMenu_An.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(path);

                // Load Sit Idle Clip
                AnimationClip sitClip = LoadClipFromFbx("Assets/Animations/Hero/Stealth/Hero_Post_Sit_Idle_Loop.fbx");
                if (sitClip != null)
                {
                    var rootSM = controller.layers[0].stateMachine;
                    var sitState = rootSM.AddState("Sit_Idle_Chill");
                    sitState.motion = sitClip;
                    rootSM.defaultState = sitState;
                }
                EditorUtility.SetDirty(controller);
            }
            return controller;
        }

        private static RuntimeAnimatorController GetOrCreateNPCObserverController()
        {
            string dir = "Assets/Animations/Controllers";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            string path = $"{dir}/AC_ChillMenu_NPC.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(path);

                AnimationClip idleCasual = LoadClipFromFbx("Assets/Animations/Hero/Loco/Hero_Loco_Idle_Casual_Loop_IP.fbx");
                AnimationClip idleLook = LoadClipFromFbx("Assets/Animations/Hero/Loco/Hero_Loco_Idle_Variation_02_Loop_IP.fbx");

                var rootSM = controller.layers[0].stateMachine;

                var stateCasual = rootSM.AddState("Idle_Casual");
                stateCasual.motion = idleCasual;
                rootSM.defaultState = stateCasual;

                if (idleLook != null)
                {
                    var stateLook = rootSM.AddState("Idle_LookAround");
                    stateLook.motion = idleLook;

                    // Casual -> LookAround transition after 10 seconds
                    var toLook = stateCasual.AddTransition(stateLook);
                    toLook.hasExitTime = true;
                    toLook.exitTime = 3.0f; // 3 loops of casual
                    toLook.duration = 0.5f;

                    // LookAround -> Casual transition
                    var toCasual = stateLook.AddTransition(stateCasual);
                    toCasual.hasExitTime = true;
                    toCasual.exitTime = 1.0f;
                    toCasual.duration = 0.5f;
                }
                EditorUtility.SetDirty(controller);
            }
            return controller;
        }

        private static AnimationClip LoadClipFromFbx(string fbxPath)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (var asset in assets)
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    return clip;
                }
            }
            return null;
        }

        private static void ApplyCharacterMaterial(GameObject target, string matName)
        {
            Texture2D charTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/MeshyImports/Boy Character Rig_20260926_073036/meshy_basecolor.png");
            if (charTex == null) charTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/MeshyImports/Boy Character Rig_20260926_073041/meshy_basecolor.png");

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material heroMat = new Material(urpLit) { name = matName };
            if (charTex != null)
            {
                heroMat.mainTexture = charTex;
                heroMat.SetColor("_BaseColor", Color.white);
            }

            var renderers = target.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                r.sharedMaterial = heroMat;
            }
        }

        private static GameObject CreateSubdividedWaterQuad(string name, float width, float height, int segX, int segY)
        {
            GameObject obj = new GameObject(name);
            var mf = obj.AddComponent<MeshFilter>();
            var mr = obj.AddComponent<MeshRenderer>();

            Mesh mesh = new Mesh { name = name };

            int vertCount = (segX + 1) * (segY + 1);
            Vector3[] verts = new Vector3[vertCount];
            Vector2[] uvs = new Vector2[vertCount];
            Vector3[] norms = new Vector3[vertCount];

            float dx = width / segX;
            float dy = height / segY;

            for (int y = 0; y <= segY; y++)
            {
                for (int x = 0; x <= segX; x++)
                {
                    int i = y * (segX + 1) + x;
                    verts[i] = new Vector3((x * dx) - (width * 0.5f), (y * dy) - (height * 0.5f), 0f);
                    uvs[i] = new Vector2((float)x / segX, (float)y / segY);
                    norms[i] = -Vector3.forward;
                }
            }

            int triCount = segX * segY * 6;
            int[] tris = new int[triCount];
            int ti = 0;

            for (int y = 0; y < segY; y++)
            {
                for (int x = 0; x < segX; x++)
                {
                    int vi = y * (segX + 1) + x;
                    tris[ti++] = vi;
                    tris[ti++] = vi + segX + 1;
                    tris[ti++] = vi + 1;

                    tris[ti++] = vi + 1;
                    tris[ti++] = vi + segX + 1;
                    tris[ti++] = vi + segX + 2;
                }
            }

            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.normals = norms;
            mesh.triangles = tris;
            mesh.RecalculateBounds();

            mf.sharedMesh = mesh;
            return obj;
        }

        private static Material CreateTransparentSpriteMat(string name, Texture2D tex)
        {
            string path = $"{MaterialsPath}/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
                mat = new Material(shader) { name = name };
                mat.mainTexture = tex;
                if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1); // Transparent
                if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0); // Alpha
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }

        private static Material GetOrCreateMaterial(string name, Color baseColor, float smoothness)
        {
            string dir = "Assets/Materials";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            string path = $"{dir}/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            if (mat == null)
            {
                mat = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.color = baseColor;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", baseColor);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static GameObject CreateChild(GameObject parent, string name)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent.transform, false);
            return obj;
        }
    }
}
