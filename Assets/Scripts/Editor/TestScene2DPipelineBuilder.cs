using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using HoaVaMau.Environment;
using HoaVaMau.Gameplay;

namespace HoaVaMau.Editor
{
    public static class TestScene2DPipelineBuilder
    {
        private const string LightingPath = "Assets/Lighting";
        private const string MaterialsPath = "Assets/Materials";
        private const string PrefabsPath = "Assets/Prefabs";

        [MenuItem("Tools/2D Pipeline/Build Scene 1: Sunset (canh 2d)")]
        public static void BuildSunsetInActiveScene()
        {
            BuildSunsetSceneInternal("Assets/Scenes/canh 2d.unity", true);
        }

        [MenuItem("Tools/2D Pipeline/Build Scene 1: Test_Sunset_2D.unity")]
        public static void BuildSunsetDedicatedScene()
        {
            BuildSunsetSceneInternal("Assets/Scenes/Test_Sunset_2D.unity", false);
        }

        [MenuItem("Tools/2D Pipeline/Build Scene 2: Test_Twilight_2D.unity")]
        public static void BuildTwilightDedicatedScene()
        {
            BuildTwilightSceneInternal("Assets/Scenes/Test_Twilight_2D.unity");
        }

        [MenuItem("Tools/2D Pipeline/Build All Scenes")]
        public static void BuildAllScenes()
        {
            BuildSunsetSceneInternal("Assets/Scenes/canh 2d.unity", true);
            BuildSunsetSceneInternal("Assets/Scenes/Test_Sunset_2D.unity", false);
            BuildTwilightSceneInternal("Assets/Scenes/Test_Twilight_2D.unity");
            // Re-open canh 2d so the user sees it immediately
            EditorSceneManager.OpenScene("Assets/Scenes/canh 2d.unity");
            Debug.Log("[2D Pipeline] Successfully built and updated all scenes!");
        }

        public static void BuildSunsetSceneInternal(string scenePath, bool isCurrentScene)
        {
            EnsureDirectories();
            VolumeProfile sunsetProfile = GetOrCreateSunsetVolumeProfile();
            Material skyMat = GetOrCreateSunsetSkyMaterial();
            Material sunMat = GetOrCreateSunsetSunMaterial();
            Material waterMat = GetOrCreateSunsetWaterMaterial();
            Material groundMat = GetOrCreateStylizedGroundMaterial(new Color(0.10f, 0.08f, 0.12f), new Color(0.14f, 0.11f, 0.14f));
            Material mountainMat = GetOrCreateMountainMaterial(new Color(0.18f, 0.12f, 0.24f), new Color(0.70f, 0.44f, 0.38f));
            GameObject boatPrefab = GetOrCreateBoatPrefab();

            UnityEngine.SceneManagement.Scene scene;
            if (isCurrentScene && EditorSceneManager.GetActiveScene().path == scenePath)
            {
                scene = EditorSceneManager.GetActiveScene();
            }
            else
            {
                if (File.Exists(scenePath))
                    scene = EditorSceneManager.OpenScene(scenePath);
                else
                    scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }

            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                Object.DestroyImmediate(root);
            }

            // 1. Setup Camera (Orthographic Side-View)
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            Camera cam = camObj.AddComponent<Camera>();
            camObj.transform.position = new Vector3(0f, 1.8f, -15f);
            camObj.transform.rotation = Quaternion.identity;
            cam.orthographic = true;
            cam.orthographicSize = 4.8f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.15f, 0.12f, 0.35f);

            var urpCam = camObj.AddComponent<UniversalAdditionalCameraData>();
            urpCam.renderPostProcessing = true;
            urpCam.antialiasing = AntialiasingMode.FastApproximateAntialiasing;

            var sideScrollCam = camObj.AddComponent<SideScroll2DCamera>();
            sideScrollCam.OrthographicSize = 4.8f;

            camObj.AddComponent<AudioListener>();

            // 2. Setup Directional Light (Warm Sunset Sun)
            GameObject lightObj = new GameObject("Directional Light");
            Light dirLight = lightObj.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.color = new Color(1.0f, 0.72f, 0.44f);
            dirLight.intensity = 1.35f;
            dirLight.shadows = LightShadows.Soft;
            dirLight.shadowStrength = 0.65f;
            lightObj.transform.position = new Vector3(0f, 10f, -5f);
            lightObj.transform.rotation = Quaternion.Euler(14f, 18f, 0f);

            // Ambient / Environment
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.26f, 0.22f, 0.40f);
            RenderSettings.ambientEquatorColor = new Color(0.50f, 0.32f, 0.36f);
            RenderSettings.ambientGroundColor = new Color(0.10f, 0.08f, 0.12f);
            RenderSettings.fog = false;

            // 3. Post-Processing Volume
            GameObject volumeObj = new GameObject("[Global_Volume_PostProcessing]");
            var volume = volumeObj.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;
            volume.profile = sunsetProfile;

            // 4. Background: Sky Backdrop
            GameObject skyObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            skyObj.name = "Background_SkyGradient";
            skyObj.transform.position = new Vector3(0f, 6.5f, 45f);
            skyObj.transform.localScale = new Vector3(80f, 22f, 1f);
            skyObj.GetComponent<Renderer>().sharedMaterial = skyMat;
            Object.DestroyImmediate(skyObj.GetComponent<Collider>());

            // 5. Sun Quad (Low on Horizon)
            GameObject sunObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            sunObj.name = "Background_SunsetSun";
            sunObj.transform.position = new Vector3(2.5f, 2.7f, 35f);
            sunObj.transform.localScale = new Vector3(6.5f, 6.5f, 1f);
            sunObj.GetComponent<Renderer>().sharedMaterial = sunMat;
            Object.DestroyImmediate(sunObj.GetComponent<Collider>());
            var sunCtrl = sunObj.AddComponent<SunController>();

            // 6. Distant Clouds
            CreateDistantClouds(scene, 33f, new Color(1f, 0.80f, 0.68f, 0.50f));

            // 7. Distant Mountain Silhouette Ridges
            CreateDistantMountains(scene, mountainMat);

            // 8. Midground: Sloped Water Plane for Wide Depth
            GameObject waterObj = CreateSubdividedWaterPlane("Midground_StylizedWater", 60f, 28f, 0.5f, -0.6f, 1.5f);
            waterObj.GetComponent<Renderer>().sharedMaterial = waterMat;
            var waterCtrl = waterObj.AddComponent<WaterController>();
            waterCtrl.SetSunTransform(sunObj.transform);

            // 9. Distant Boat
            if (boatPrefab != null)
            {
                var boatObj = (GameObject)PrefabUtility.InstantiatePrefab(boatPrefab);
                boatObj.name = "Midground_DistantBoat";
                boatObj.transform.position = new Vector3(3.8f, 0.58f, 16.0f);
                boatObj.transform.rotation = Quaternion.Euler(0f, -15f, 0f);
                boatObj.transform.localScale = Vector3.one * 0.42f;
                var distantBoat = boatObj.GetComponent<DistantBoat>() ?? boatObj.AddComponent<DistantBoat>();
                distantBoat.InitializeBase();
            }

            // 10. Foreground: Shore Bank
            GameObject foregroundGroup = new GameObject("Foreground_Environment");
            CreateForegroundBank(foregroundGroup.transform, groundMat);
            AddForegroundRocks(foregroundGroup.transform);
            AddForegroundGrass(foregroundGroup.transform);

            // Add 3D Player Character on Shore
            GameObject characterObj = AddForegroundCharacter(foregroundGroup.transform);
            if (characterObj != null)
            {
                sideScrollCam.Target = characterObj.transform;
            }

            // Save Scene
            if (!isCurrentScene || EditorSceneManager.GetActiveScene().path != scenePath)
            {
                EditorSceneManager.SaveScene(scene, scenePath);
            }
            else
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveOpenScenes();
            }

            Debug.Log($"[2D Pipeline] Sunset scene built successfully at: {scenePath}");
        }

        public static void BuildTwilightSceneInternal(string scenePath)
        {
            EnsureDirectories();
            VolumeProfile twilightProfile = GetOrCreateTwilightVolumeProfile();
            Material skyMat = GetOrCreateTwilightSkyMaterial();
            Material sunMoonMat = GetOrCreateTwilightMoonMaterial();
            Material waterMat = GetOrCreateTwilightWaterMaterial();
            Material groundMat = GetOrCreateStylizedGroundMaterial(new Color(0.05f, 0.06f, 0.10f), new Color(0.08f, 0.10f, 0.14f));
            Material mountainMat = GetOrCreateMountainMaterial(new Color(0.07f, 0.09f, 0.16f), new Color(0.18f, 0.20f, 0.32f));
            GameObject boatPrefab = GetOrCreateBoatPrefab();

            UnityEngine.SceneManagement.Scene scene;
            if (File.Exists(scenePath))
                scene = EditorSceneManager.OpenScene(scenePath);
            else
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                Object.DestroyImmediate(root);
            }

            // Setup Camera
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            Camera cam = camObj.AddComponent<Camera>();
            camObj.transform.position = new Vector3(0f, 1.8f, -15f);
            camObj.transform.rotation = Quaternion.identity;
            cam.orthographic = true;
            cam.orthographicSize = 4.8f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.07f, 0.16f);

            var urpCam = camObj.AddComponent<UniversalAdditionalCameraData>();
            urpCam.renderPostProcessing = true;
            urpCam.antialiasing = AntialiasingMode.FastApproximateAntialiasing;

            var sideScrollCam = camObj.AddComponent<SideScroll2DCamera>();
            sideScrollCam.OrthographicSize = 4.8f;
            camObj.AddComponent<AudioListener>();

            // Setup Directional Light (Cool Moonlight / Dusk)
            GameObject lightObj = new GameObject("Directional Light");
            Light dirLight = lightObj.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.color = new Color(0.68f, 0.78f, 0.95f);
            dirLight.intensity = 0.85f;
            dirLight.shadows = LightShadows.Soft;
            dirLight.shadowStrength = 0.7f;
            lightObj.transform.position = new Vector3(0f, 10f, -5f);
            lightObj.transform.rotation = Quaternion.Euler(20f, -20f, 0f);

            // Ambient / Environment
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.12f, 0.14f, 0.26f);
            RenderSettings.ambientEquatorColor = new Color(0.16f, 0.16f, 0.28f);
            RenderSettings.ambientGroundColor = new Color(0.05f, 0.06f, 0.10f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.10f, 0.12f, 0.22f);
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.008f;

            // Volume
            GameObject volumeObj = new GameObject("[Global_Volume_PostProcessing]");
            var volume = volumeObj.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;
            volume.profile = twilightProfile;

            // Sky
            GameObject skyObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            skyObj.name = "Background_SkyGradient";
            skyObj.transform.position = new Vector3(0f, 6.5f, 45f);
            skyObj.transform.localScale = new Vector3(80f, 22f, 1f);
            skyObj.GetComponent<Renderer>().sharedMaterial = skyMat;
            Object.DestroyImmediate(skyObj.GetComponent<Collider>());

            // Moon / Dusk Glow
            GameObject moonObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            moonObj.name = "Background_TwilightMoon";
            moonObj.transform.position = new Vector3(3.0f, 3.0f, 35f);
            moonObj.transform.localScale = new Vector3(5.5f, 5.5f, 1f);
            moonObj.GetComponent<Renderer>().sharedMaterial = sunMoonMat;
            Object.DestroyImmediate(moonObj.GetComponent<Collider>());
            var sunCtrl = moonObj.AddComponent<SunController>();

            // Clouds
            CreateDistantClouds(scene, 33f, new Color(0.4f, 0.45f, 0.65f, 0.40f));

            // Mountains
            CreateDistantMountains(scene, mountainMat);

            // Water
            GameObject waterObj = CreateSubdividedWaterPlane("Midground_StylizedWater", 60f, 28f, 0.5f, -0.6f, 1.5f);
            waterObj.GetComponent<Renderer>().sharedMaterial = waterMat;
            var waterCtrl = waterObj.AddComponent<WaterController>();
            waterCtrl.SetSunTransform(moonObj.transform);

            // Boat
            if (boatPrefab != null)
            {
                var boatObj = (GameObject)PrefabUtility.InstantiatePrefab(boatPrefab);
                boatObj.name = "Midground_DistantBoat";
                boatObj.transform.position = new Vector3(3.8f, 0.58f, 16.0f);
                boatObj.transform.rotation = Quaternion.Euler(0f, -15f, 0f);
                boatObj.transform.localScale = Vector3.one * 0.42f;
                var distantBoat = boatObj.GetComponent<DistantBoat>() ?? boatObj.AddComponent<DistantBoat>();
                distantBoat.InitializeBase();
            }

            // Foreground
            GameObject foregroundGroup = new GameObject("Foreground_Environment");
            CreateForegroundBank(foregroundGroup.transform, groundMat);
            AddForegroundRocks(foregroundGroup.transform);
            AddForegroundGrass(foregroundGroup.transform);
            var charObj = AddForegroundCharacter(foregroundGroup.transform);
            if (charObj != null) sideScrollCam.Target = charObj.transform;

            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[2D Pipeline] Twilight scene built successfully at: {scenePath}");
        }

        private static void CreateForegroundBank(Transform parent, Material mat)
        {
            // Main ground bank across entire bottom
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Foreground_Ground_Main";
            ground.transform.SetParent(parent, false);
            ground.transform.position = new Vector3(0f, -1.8f, -0.2f);
            ground.transform.localScale = new Vector3(65.0f, 2.4f, 3.2f);
            ground.GetComponent<Renderer>().sharedMaterial = mat;

            // Shore slope dipping into water at Y = -0.6f
            GameObject slope = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slope.name = "Foreground_Ground_ShoreSlope";
            slope.transform.SetParent(parent, false);
            slope.transform.position = new Vector3(0f, -1.15f, 1.1f);
            slope.transform.rotation = Quaternion.Euler(-24f, 0f, 0f);
            slope.transform.localScale = new Vector3(65.0f, 1.2f, 1.4f);
            slope.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static void AddForegroundRocks(Transform parent)
        {
            GameObject rock01Prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Rock001.fbx");
            GameObject rock02Prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Rock0022.fbx");

            Material rockMat = GetOrCreateRockMaterial();

            if (rock01Prefab != null)
            {
                var r1 = Object.Instantiate(rock01Prefab, parent);
                r1.name = "Rock_Frame_Left";
                r1.transform.position = new Vector3(-7.5f, -0.6f, 0.4f);
                r1.transform.rotation = Quaternion.Euler(-5f, 40f, 5f);
                r1.transform.localScale = Vector3.one * 0.9f;
                ApplyMaterialToRenderers(r1, rockMat);
            }

            if (rock02Prefab != null)
            {
                var r2 = Object.Instantiate(rock02Prefab, parent);
                r2.name = "Rock_Frame_Right";
                r2.transform.position = new Vector3(8.5f, -0.6f, 0.4f);
                r2.transform.rotation = Quaternion.Euler(0f, 75f, 0f);
                r2.transform.localScale = Vector3.one * 0.8f;
                ApplyMaterialToRenderers(r2, rockMat);
            }
        }

        private static void AddForegroundGrass(Transform parent)
        {
            GameObject grassPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PF_StylizedGrassTuft_3D.prefab");
            if (grassPrefab == null) return;

            float[] xCoords = { -7.5f, -6.2f, -5.0f, -3.2f, -2.0f, -1.2f };
            for (int i = 0; i < xCoords.Length; i++)
            {
                var g = (GameObject)PrefabUtility.InstantiatePrefab(grassPrefab, parent);
                g.name = $"GrassTuft_{i}";
                float zOffset = (i % 2 == 0) ? 0.2f : 0.5f;
                g.transform.position = new Vector3(xCoords[i], -0.6f, zOffset);
                g.transform.rotation = Quaternion.Euler(0f, (i * 53f) % 360f, 0f);
                g.transform.localScale = Vector3.one * (0.75f + (i % 3) * 0.2f);
            }
        }

        private static GameObject AddForegroundCharacter(Transform parent)
        {
            GameObject charPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/nhan vat chinh 3d.fbx");
            if (charPrefab == null)
            {
                GameObject placeholder = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                placeholder.name = "Character_Placeholder";
                placeholder.transform.SetParent(parent, false);
                placeholder.transform.position = new Vector3(-4.0f, 0.2f, 0f);
                return placeholder;
            }

            GameObject character = Object.Instantiate(charPrefab, parent);
            character.name = "Character_Hero";
            character.transform.position = new Vector3(-4.5f, -0.6f, 0f);
            character.transform.rotation = Quaternion.Euler(0f, 40f, 0f);
            character.transform.localScale = Vector3.one * 0.95f;

            Material charMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/M_Character_Silhouette.mat");
            if (charMat == null)
            {
                charMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                charMat.name = "M_Character_Silhouette";
                charMat.SetColor("_BaseColor", new Color(0.10f, 0.08f, 0.12f, 1.0f));
                charMat.SetFloat("_Smoothness", 0.05f);
                AssetDatabase.CreateAsset(charMat, $"{MaterialsPath}/M_Character_Silhouette.mat");
            }

            ApplyMaterialToRenderers(character, charMat);
            return character;
        }

        private static void CreateDistantMountains(UnityEngine.SceneManagement.Scene scene, Material mountainMat)
        {
            // Mountain Ridge 1 (Far silhouette peaks)
            GameObject ridge1 = CreateMountainMesh("Background_MountainRidge_Far", 70f, 35, 28f, 1.4f, 1.6f, 0.14f, 1.3f);
            ridge1.GetComponent<Renderer>().sharedMaterial = mountainMat;

            // Mountain Ridge 2 (Mid-distance silhouette, slightly lower)
            GameObject ridge2 = CreateMountainMesh("Background_MountainRidge_Mid", 60f, 30, 25f, 1.35f, 1.1f, 0.20f, 3.7f);
            ridge2.GetComponent<Renderer>().sharedMaterial = mountainMat;
        }

        private static GameObject CreateMountainMesh(string name, float width, int segments, float depthZ, float baseY, float peakHeight, float freq, float phase)
        {
            GameObject obj = new GameObject(name);
            obj.transform.position = new Vector3(0f, 0f, depthZ);

            MeshFilter mf = obj.AddComponent<MeshFilter>();
            MeshRenderer mr = obj.AddComponent<MeshRenderer>();

            Mesh mesh = new Mesh();
            mesh.name = name + "_Mesh";

            Vector3[] verts = new Vector3[(segments + 1) * 2];
            Vector2[] uvs = new Vector2[verts.Length];
            int[] tris = new int[segments * 6];

            float dx = width / segments;
            float halfW = width * 0.5f;

            for (int i = 0; i <= segments; i++)
            {
                float x = i * dx - halfW;
                float u = (float)i / segments;

                // Procedural mountain peaks
                float peak = Mathf.Sin(x * freq + phase) * 0.5f 
                           + Mathf.Cos(x * freq * 2.1f + phase * 1.5f) * 0.3f
                           + Mathf.Abs(Mathf.Sin(x * freq * 0.8f + 0.4f)) * 0.4f;
                float yTop = baseY + Mathf.Max(0.1f, peak * peakHeight);
                float yBottom = baseY - 2.5f; // well below water horizon

                verts[i * 2] = new Vector3(x, yBottom, 0f);
                verts[i * 2 + 1] = new Vector3(x, yTop, 0f);

                uvs[i * 2] = new Vector2(u, 0f);
                uvs[i * 2 + 1] = new Vector2(u, 1f);
            }

            for (int i = 0, ti = 0; i < segments; i++, ti += 6)
            {
                int b0 = i * 2;
                int t0 = i * 2 + 1;
                int b1 = (i + 1) * 2;
                int t1 = (i + 1) * 2 + 1;

                tris[ti] = b0;
                tris[ti + 1] = t0;
                tris[ti + 2] = b1;

                tris[ti + 3] = b1;
                tris[ti + 4] = t0;
                tris[ti + 5] = t1;
            }

            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            mf.sharedMesh = mesh;
            return obj;
        }

        private static void CreateDistantClouds(UnityEngine.SceneManagement.Scene scene, float depthZ, Color cloudColor)
        {
            Texture2D cloudTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Clouds/cumulus_cloud_02.png") 
                ?? AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/cumulus_cloud_02.png");
            if (cloudTex == null) cloudTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Clouds/lana_cloud_01.png")
                ?? AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/lana_cloud_01.png");

            Shader cloudShader = Shader.Find("HoaVaMau/CinematicCloud") ?? Shader.Find("Universal Render Pipeline/Unlit");
            Material cloudMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/M_DistantClouds.mat");
            if (cloudMat == null)
            {
                cloudMat = new Material(cloudShader);
                cloudMat.name = "M_DistantClouds";
                AssetDatabase.CreateAsset(cloudMat, $"{MaterialsPath}/M_DistantClouds.mat");
            }
            cloudMat.shader = cloudShader;
            if (cloudTex != null) cloudMat.mainTexture = cloudTex;
            cloudMat.color = cloudColor;
            if (cloudShader.name == "HoaVaMau/CinematicCloud")
            {
                cloudMat.SetColor("_AtmosphereTint", cloudColor);
                cloudMat.SetColor("_RimColor", new Color(1f, 0.95f, 0.85f, 1f));
                cloudMat.SetFloat("_CloudBrightness", 1.25f);
                cloudMat.SetFloat("_HorizonFadeHeight", 0.05f);
            }
            EditorUtility.SetDirty(cloudMat);

            Vector3[] cloudPositions = {
                new Vector3(-9f, 5.0f, depthZ),
                new Vector3(4f, 5.6f, depthZ - 1f),
                new Vector3(13f, 4.6f, depthZ + 1f)
            };
            Vector3[] cloudScales = {
                new Vector3(9f, 3.0f, 1f),
                new Vector3(11f, 3.4f, 1f),
                new Vector3(8f, 2.6f, 1f)
            };

            for (int i = 0; i < cloudPositions.Length; i++)
            {
                GameObject cloud = GameObject.CreatePrimitive(PrimitiveType.Quad);
                cloud.name = $"Background_Cloud_{i}";
                cloud.transform.position = cloudPositions[i];
                cloud.transform.localScale = cloudScales[i];
                cloud.GetComponent<Renderer>().sharedMaterial = cloudMat;
                Object.DestroyImmediate(cloud.GetComponent<Collider>());
            }
        }

        private static GameObject CreateSubdividedWaterPlane(string name, float width, float length, float startZ, float startY, float endY)
        {
            GameObject planeObj = new GameObject(name);
            planeObj.transform.position = Vector3.zero;

            MeshFilter mf = planeObj.AddComponent<MeshFilter>();
            MeshRenderer mr = planeObj.AddComponent<MeshRenderer>();

            int segX = 60;
            int segZ = 35;
            Mesh mesh = new Mesh();
            mesh.name = "SubdividedWaterMesh";

            Vector3[] verts = new Vector3[(segX + 1) * (segZ + 1)];
            Vector2[] uvs = new Vector2[verts.Length];
            int[] tris = new int[segX * segZ * 6];

            float dx = width / segX;
            float dz = length / segZ;
            float dy = (endY - startY) / segZ;
            float halfW = width * 0.5f;

            for (int z = 0, i = 0; z <= segZ; z++)
            {
                float vz = startZ + z * dz;
                float vy = startY + z * dy;
                float v = (float)z / segZ;
                for (int x = 0; x <= segX; x++, i++)
                {
                    float vx = x * dx - halfW;
                    float u = (float)x / segX;
                    verts[i] = new Vector3(vx, vy, vz);
                    uvs[i] = new Vector2(u, v);
                }
            }

            for (int z = 0, ti = 0, vi = 0; z < segZ; z++, vi++)
            {
                for (int x = 0; x < segX; x++, ti += 6, vi++)
                {
                    tris[ti] = vi;
                    tris[ti + 1] = vi + segX + 1;
                    tris[ti + 2] = vi + 1;
                    tris[ti + 3] = vi + 1;
                    tris[ti + 4] = vi + segX + 1;
                    tris[ti + 5] = vi + segX + 2;
                }
            }

            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            mf.sharedMesh = mesh;
            return planeObj;
        }

        private static GameObject GetOrCreateBoatPrefab()
        {
            string prefabPath = $"{PrefabsPath}/PF_StylizedBoat.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existing != null) return existing;

            GameObject root = new GameObject("PF_StylizedBoat");

            GameObject hull = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hull.name = "Boat_Hull";
            hull.transform.SetParent(root.transform, false);
            hull.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            hull.transform.localScale = new Vector3(2.4f, 0.45f, 0.85f);

            GameObject bow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bow.name = "Boat_Bow";
            bow.transform.SetParent(root.transform, false);
            bow.transform.localPosition = new Vector3(1.3f, 0.22f, 0f);
            bow.transform.localRotation = Quaternion.Euler(0f, 0f, -22f);
            bow.transform.localScale = new Vector3(0.6f, 0.4f, 0.7f);

            GameObject mast = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mast.name = "Boat_Mast";
            mast.transform.SetParent(root.transform, false);
            mast.transform.localPosition = new Vector3(0.2f, 1.1f, 0f);
            mast.transform.localScale = new Vector3(0.08f, 0.9f, 0.08f);

            GameObject sail = GameObject.CreatePrimitive(PrimitiveType.Quad);
            sail.name = "Boat_Sail";
            sail.transform.SetParent(root.transform, false);
            sail.transform.localPosition = new Vector3(-0.4f, 1.2f, 0f);
            sail.transform.localScale = new Vector3(1.1f, 1.3f, 1f);

            GameObject lantern = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lantern.name = "Boat_Lantern";
            lantern.transform.SetParent(root.transform, false);
            lantern.transform.localPosition = new Vector3(1.5f, 0.42f, 0f);
            lantern.transform.localScale = Vector3.one * 0.22f;

            Light lanternLight = lantern.AddComponent<Light>();
            lanternLight.type = LightType.Point;
            lanternLight.color = new Color(1.0f, 0.75f, 0.35f);
            lanternLight.intensity = 1.8f;
            lanternLight.range = 6.0f;

            Material woodMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            woodMat.name = "M_Boat_Wood";
            woodMat.SetColor("_BaseColor", new Color(0.22f, 0.14f, 0.10f));
            AssetDatabase.CreateAsset(woodMat, $"{MaterialsPath}/M_Boat_Wood.mat");

            Material sailMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            sailMat.name = "M_Boat_Sail";
            sailMat.SetColor("_BaseColor", new Color(0.85f, 0.82f, 0.75f));
            AssetDatabase.CreateAsset(sailMat, $"{MaterialsPath}/M_Boat_Sail.mat");

            Material lanternMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            lanternMat.name = "M_Boat_Lantern";
            lanternMat.SetColor("_BaseColor", new Color(1f, 0.8f, 0.4f));
            lanternMat.EnableKeyword("_EMISSION");
            lanternMat.SetColor("_EmissionColor", new Color(2.5f, 1.8f, 0.6f));
            AssetDatabase.CreateAsset(lanternMat, $"{MaterialsPath}/M_Boat_Lantern.mat");

            hull.GetComponent<Renderer>().sharedMaterial = woodMat;
            bow.GetComponent<Renderer>().sharedMaterial = woodMat;
            mast.GetComponent<Renderer>().sharedMaterial = woodMat;
            sail.GetComponent<Renderer>().sharedMaterial = sailMat;
            lantern.GetComponent<Renderer>().sharedMaterial = lanternMat;

            Object.DestroyImmediate(hull.GetComponent<Collider>());
            Object.DestroyImmediate(bow.GetComponent<Collider>());
            Object.DestroyImmediate(mast.GetComponent<Collider>());
            Object.DestroyImmediate(sail.GetComponent<Collider>());
            Object.DestroyImmediate(lantern.GetComponent<Collider>());

            root.AddComponent<DistantBoat>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static VolumeProfile GetOrCreateSunsetVolumeProfile()
        {
            string profilePath = $"{LightingPath}/VolumeProfile_Sunset.asset";
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if (profile != null) return profile;

            profile = ScriptableObject.CreateInstance<VolumeProfile>();

            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.85f);
            bloom.intensity.Override(1.35f);
            bloom.scatter.Override(0.7f);
            bloom.tint.Override(new Color(1.0f, 0.88f, 0.72f));

            var colorAdj = profile.Add<ColorAdjustments>(true);
            colorAdj.postExposure.Override(0.12f);
            colorAdj.contrast.Override(18f);
            colorAdj.colorFilter.Override(new Color(1.0f, 0.96f, 0.90f));
            colorAdj.saturation.Override(14f);

            var tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.ACES);

            var vignette = profile.Add<Vignette>(true);
            vignette.color.Override(new Color(0.04f, 0.02f, 0.08f));
            vignette.intensity.Override(0.22f);
            vignette.smoothness.Override(0.42f);

            var whiteBalance = profile.Add<WhiteBalance>(true);
            whiteBalance.temperature.Override(14f);
            whiteBalance.tint.Override(4f);

            AssetDatabase.CreateAsset(profile, profilePath);
            return profile;
        }

        private static VolumeProfile GetOrCreateTwilightVolumeProfile()
        {
            string profilePath = $"{LightingPath}/VolumeProfile_Twilight.asset";
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if (profile != null) return profile;

            profile = ScriptableObject.CreateInstance<VolumeProfile>();

            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.9f);
            bloom.intensity.Override(1.05f);
            bloom.scatter.Override(0.65f);
            bloom.tint.Override(new Color(0.72f, 0.82f, 1.0f));

            var colorAdj = profile.Add<ColorAdjustments>(true);
            colorAdj.postExposure.Override(-0.1f);
            colorAdj.contrast.Override(14f);
            colorAdj.colorFilter.Override(new Color(0.85f, 0.90f, 1.0f));
            colorAdj.saturation.Override(6f);

            var tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.ACES);

            var vignette = profile.Add<Vignette>(true);
            vignette.color.Override(new Color(0.02f, 0.02f, 0.06f));
            vignette.intensity.Override(0.28f);
            vignette.smoothness.Override(0.5f);

            var whiteBalance = profile.Add<WhiteBalance>(true);
            whiteBalance.temperature.Override(-16f);
            whiteBalance.tint.Override(-4f);

            AssetDatabase.CreateAsset(profile, profilePath);
            return profile;
        }

        private static Material GetOrCreateSunsetSkyMaterial()
        {
            string path = $"{MaterialsPath}/M_StylizedSky_Sunset.mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("HoaVaMau/StylizedSky2D"));
                m.name = "M_StylizedSky_Sunset";
                AssetDatabase.CreateAsset(m, path);
            }
            m.shader = Shader.Find("HoaVaMau/StylizedSky2D");
            m.SetColor("_ZenithColor", new Color(0.16f, 0.14f, 0.45f));
            m.SetColor("_MidSkyColor", new Color(0.85f, 0.36f, 0.30f));
            m.SetColor("_HorizonColor", new Color(1.0f, 0.74f, 0.35f));
            m.SetFloat("_HorizonY", 1.35f);
            m.SetFloat("_ZenithY", 6.60f);
            m.SetFloat("_MidPosition", 0.45f);
            m.SetFloat("_ZenithPower", 1.2f);
            m.SetFloat("_HorizonPower", 1.4f);
            m.SetColor("_HorizonBandColor", new Color(1.0f, 0.88f, 0.50f));
            m.SetFloat("_HorizonBandHeight", 1.55f);
            m.SetFloat("_HorizonBandWidth", 0.85f);
            m.SetFloat("_HorizonBandIntensity", 0.75f);
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Material GetOrCreateTwilightSkyMaterial()
        {
            string path = $"{MaterialsPath}/M_StylizedSky_Twilight.mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("HoaVaMau/StylizedSky2D"));
                m.name = "M_StylizedSky_Twilight";
                AssetDatabase.CreateAsset(m, path);
            }
            m.shader = Shader.Find("HoaVaMau/StylizedSky2D");
            m.SetColor("_ZenithColor", new Color(0.06f, 0.08f, 0.22f));
            m.SetColor("_MidSkyColor", new Color(0.18f, 0.16f, 0.34f));
            m.SetColor("_HorizonColor", new Color(0.42f, 0.28f, 0.50f));
            m.SetFloat("_HorizonY", 1.35f);
            m.SetFloat("_ZenithY", 6.60f);
            m.SetFloat("_MidPosition", 0.40f);
            m.SetFloat("_ZenithPower", 1.2f);
            m.SetFloat("_HorizonPower", 1.4f);
            m.SetColor("_HorizonBandColor", new Color(0.55f, 0.42f, 0.68f));
            m.SetFloat("_HorizonBandHeight", 1.55f);
            m.SetFloat("_HorizonBandWidth", 0.85f);
            m.SetFloat("_HorizonBandIntensity", 0.55f);
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Material GetOrCreateSunsetSunMaterial()
        {
            string path = $"{MaterialsPath}/M_StylizedSun_Sunset.mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;

            m = new Material(Shader.Find("HoaVaMau/StylizedSun"));
            m.name = "M_StylizedSun_Sunset";
            m.SetColor("_CoreColor", new Color(3.5f, 3.0f, 2.0f));
            m.SetColor("_GlowColor", new Color(2.0f, 0.8f, 0.2f));
            m.SetFloat("_CoreRadius", 0.20f);
            m.SetFloat("_GlowRadius", 0.85f);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        private static Material GetOrCreateTwilightMoonMaterial()
        {
            string path = $"{MaterialsPath}/M_StylizedSun_Twilight.mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;

            m = new Material(Shader.Find("HoaVaMau/StylizedSun"));
            m.name = "M_StylizedSun_Twilight";
            m.SetColor("_CoreColor", new Color(1.8f, 2.2f, 2.8f));
            m.SetColor("_GlowColor", new Color(0.6f, 0.8f, 1.5f));
            m.SetFloat("_CoreRadius", 0.15f);
            m.SetFloat("_GlowRadius", 0.65f);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        private static Material GetOrCreateSunsetWaterMaterial()
        {
            string path = $"{MaterialsPath}/M_StylizedWater_Sunset.mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;

            m = new Material(Shader.Find("HoaVaMau/StylizedWater2D"));
            m.name = "M_StylizedWater_Sunset";
            m.SetColor("_ShallowColor", new Color(0.14f, 0.38f, 0.42f, 0.95f));
            m.SetColor("_DeepColor", new Color(0.06f, 0.16f, 0.26f, 0.98f));
            m.SetColor("_HorizonWaterColor", new Color(0.68f, 0.38f, 0.24f, 1.0f));
            m.SetColor("_SunReflectionColor", new Color(1.0f, 0.84f, 0.52f, 1.0f));
            m.SetFloat("_SunWorldX", 2.5f);
            m.SetFloat("_ReflectionWidth", 3.5f);
            m.SetFloat("_ReflectionIntensity", 2.2f);
            m.SetFloat("_WaveSpeed", 1.2f);
            m.SetFloat("_WaveHeight", 0.02f);
            m.SetFloat("_RippleScale", 7.0f);
            m.SetFloat("_RippleSpeed", 1.0f);
            m.SetFloat("_NearZ", 0.5f);
            m.SetFloat("_FarZ", 28.0f);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        private static Material GetOrCreateTwilightWaterMaterial()
        {
            string path = $"{MaterialsPath}/M_StylizedWater_Twilight.mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;

            m = new Material(Shader.Find("HoaVaMau/StylizedWater2D"));
            m.name = "M_StylizedWater_Twilight";
            m.SetColor("_ShallowColor", new Color(0.08f, 0.16f, 0.28f, 0.95f));
            m.SetColor("_DeepColor", new Color(0.04f, 0.08f, 0.16f, 0.98f));
            m.SetColor("_HorizonWaterColor", new Color(0.24f, 0.18f, 0.32f, 1.0f));
            m.SetColor("_SunReflectionColor", new Color(0.72f, 0.85f, 1.0f, 1.0f));
            m.SetFloat("_SunWorldX", 3.0f);
            m.SetFloat("_ReflectionWidth", 2.8f);
            m.SetFloat("_ReflectionIntensity", 1.4f);
            m.SetFloat("_WaveSpeed", 0.9f);
            m.SetFloat("_WaveHeight", 0.015f);
            m.SetFloat("_RippleScale", 6.0f);
            m.SetFloat("_RippleSpeed", 0.7f);
            m.SetFloat("_NearZ", 0.5f);
            m.SetFloat("_FarZ", 28.0f);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        private static Material GetOrCreateStylizedGroundMaterial(Color baseCol, Color darkCol)
        {
            string path = $"{MaterialsPath}/M_Foreground_Ground.mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;

            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.name = "M_Foreground_Ground";
            m.SetColor("_BaseColor", baseCol);
            m.SetFloat("_Smoothness", 0.05f);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        private static Material GetOrCreateMountainMaterial(Color nearCol, Color farCol)
        {
            string path = $"{MaterialsPath}/M_DistantMountains.mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;

            Shader shader = Shader.Find("HoaVaMau/AtmosphericDepth") ?? Shader.Find("Universal Render Pipeline/Unlit");
            m = new Material(shader);
            m.name = "M_DistantMountains";
            if (shader.name == "HoaVaMau/AtmosphericDepth")
            {
                m.SetColor("_NearColor", nearCol);
                m.SetColor("_FarColor", farCol);
                m.SetFloat("_DepthNear", 20f);
                m.SetFloat("_DepthFar", 35f);
            }
            else
            {
                m.color = farCol;
            }
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        private static Material GetOrCreateRockMaterial()
        {
            string path = $"{MaterialsPath}/M_StylizedRock_Silhouette.mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;

            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.name = "M_StylizedRock_Silhouette";
            m.SetColor("_BaseColor", new Color(0.16f, 0.13f, 0.18f));
            m.SetFloat("_Smoothness", 0.15f);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        private static void ApplyMaterialToRenderers(GameObject go, Material mat)
        {
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                r.sharedMaterial = mat;
            }
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(LightingPath)) Directory.CreateDirectory(LightingPath);
            if (!Directory.Exists(MaterialsPath)) Directory.CreateDirectory(MaterialsPath);
            if (!Directory.Exists(PrefabsPath)) Directory.CreateDirectory(PrefabsPath);
            AssetDatabase.Refresh();
        }
    }
}
