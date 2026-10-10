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
    public static class BuildPlanetOfLanaPierScene
    {
        private const string ScenePath = "Assets/Scenes/Scene_Platform_Test.unity";
        private const string MaterialsPath = "Assets/Materials";
        private const string LightingPath = "Assets/Lighting";
        private const string PrefabsPath = "Assets/Prefabs";

        [MenuItem("Hoa va Mau/Rebuild Planet of Lana Pier Scene (Full 3D Environment)")]
        public static void BuildPierScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            
            // Clean previous objects completely
            var roots = scene.GetRootGameObjects();
            foreach (var r in roots)
            {
                Object.DestroyImmediate(r);
            }

            // 1. Setup Camera (Orthographic Side-View for Planet of Lana 2.5D look)
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            Camera cam = camObj.AddComponent<Camera>();
            camObj.transform.position = new Vector3(0f, 1.8f, -14.5f);
            camObj.transform.rotation = Quaternion.identity;
            cam.orthographic = true;
            cam.orthographicSize = 5.0f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 120f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.18f, 0.14f, 0.35f);

            var urpCam = camObj.AddComponent<UniversalAdditionalCameraData>();
            urpCam.renderPostProcessing = true;
            urpCam.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            camObj.AddComponent<AudioListener>();

            var sideScrollCam = camObj.AddComponent<SideScroll2DCamera>();
            sideScrollCam.OrthographicSize = 5.0f;

            // 2. Setup Directional Light (Warm Golden Sunset Sun)
            GameObject lightObj = new GameObject("Directional Light");
            Light dirLight = lightObj.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.color = new Color(1.0f, 0.76f, 0.48f);
            dirLight.intensity = 1.45f;
            dirLight.shadows = LightShadows.Soft;
            dirLight.shadowStrength = 0.65f;
            lightObj.transform.position = new Vector3(0f, 10f, -5f);
            lightObj.transform.rotation = Quaternion.Euler(16f, 22f, 0f);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.28f, 0.22f, 0.42f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.34f, 0.36f);
            RenderSettings.ambientGroundColor = new Color(0.12f, 0.10f, 0.14f);
            RenderSettings.fog = false;

            // 3. Post-Processing Volume
            GameObject volumeObj = new GameObject("[Global_Volume_PostProcessing]");
            var volume = volumeObj.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;
            VolumeProfile sunsetProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>($"{LightingPath}/VolumeProfile_Sunset.asset");
            volume.profile = sunsetProfile;

            // 4. Background: Sky Backdrop & Sun
            Material skyMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/M_StylizedSky_Sunset.mat");
            GameObject skyObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            skyObj.name = "Background_SkyGradient";
            skyObj.transform.position = new Vector3(0f, 6.5f, 45f);
            skyObj.transform.localScale = new Vector3(90f, 24f, 1f);
            skyObj.GetComponent<Renderer>().sharedMaterial = skyMat;
            Object.DestroyImmediate(skyObj.GetComponent<Collider>());

            Material sunMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/M_StylizedSun_Sunset.mat");
            GameObject sunObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            sunObj.name = "Background_SunsetSun";
            sunObj.transform.position = new Vector3(4.2f, 3.2f, 38f);
            sunObj.transform.localScale = new Vector3(6.5f, 6.5f, 1f);
            sunObj.GetComponent<Renderer>().sharedMaterial = sunMat;
            Object.DestroyImmediate(sunObj.GetComponent<Collider>());
            var sunCtrl = sunObj.AddComponent<SunController>();

            // 5. Distant Clouds
            CreateDistantClouds(scene, 34f, new Color(1f, 0.82f, 0.68f, 0.55f));

            // 6. Distant Mountains
            Material mountainMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/M_DistantMountains.mat");
            CreateDistantMountains(mountainMat);

            // 7. Procedural God Rays (Smooth soft beams)
            CreateStylizedGodRays();

            // 8. Atmospheric Golden Dust Motes
            CreateGoldenMotes();

            // 9. Midground: Subdivided Stylized Water Plane (Fully covers bottom of screen)
            Material waterMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/M_StylizedWater_Sunset.mat");
            GameObject waterObj = CreateSubdividedWaterPlane("Midground_StylizedWater", 85f, 48f, -16.0f, -4.0f, 1.35f);
            waterObj.GetComponent<Renderer>().sharedMaterial = waterMat;
            var waterCtrl = waterObj.AddComponent<WaterController>();
            waterCtrl.SetSunTransform(sunObj.transform);

            // 10. Playable Wooden Pier & Boardwalk (Full 3D Modular Structure)
            GameObject pierGroup = new GameObject("Gameplay_Wooden_Pier");
            BuildWoodenPierStructure(pierGroup.transform);

            // 11. Left Shore & Natural Rock Outcrop
            GameObject shoreGroup = new GameObject("Left_Shore_Ecosystem");
            BuildLeftShore(shoreGroup.transform);

            // 12. Coastal Stilt Village (Right Shore Architecture)
            GameObject villageGroup = new GameObject("Coastal_Stilt_Village");
            BuildCoastalStiltVillage(villageGroup.transform);

            // 13. Floating Boats with Bobbing Motion
            GameObject boatsGroup = new GameObject("Floating_Boats_Group");
            BuildFloatingBoats(boatsGroup.transform);

            // 14. Natural Trees & Foliage (Planet of Lana Hand-Crafted Trees)
            GameObject treesGroup = new GameObject("Nature_Trees_And_Foliage");
            BuildNaturalTrees(treesGroup.transform);

            // 15. Player Character (Rigged Boy Runner with BaseColor Texture)
            GameObject playerObj = SpawnPlayerCharacter(pierGroup.transform);
            if (playerObj != null)
            {
                sideScrollCam.Target = playerObj.transform;
            }

            // 16. Gameplay & Environment Managers
            GameObject mgrs = new GameObject("Gameplay_Managers");
            mgrs.AddComponent<WindManager>();
            mgrs.AddComponent<GrassManager>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("<color=#40b8a6>[Planet of Lana]</color> Rebuilt platform test scene completely with genuine 3D assets!");
        }

        private static void CreateDistantMountains(Material mountainMat)
        {
            if (mountainMat == null) return;
            GameObject ridge1 = CreateMountainMesh("Background_MountainRidge_Far", 85f, 40, 29f, 1.45f, 1.8f, 0.13f, 1.2f);
            ridge1.GetComponent<Renderer>().sharedMaterial = mountainMat;

            GameObject ridge2 = CreateMountainMesh("Background_MountainRidge_Mid", 75f, 35, 25f, 1.35f, 1.2f, 0.18f, 3.5f);
            ridge2.GetComponent<Renderer>().sharedMaterial = mountainMat;
        }

        private static GameObject CreateMountainMesh(string name, float width, int segments, float depthZ, float baseY, float peakHeight, float freq, float phase)
        {
            GameObject obj = new GameObject(name);
            obj.transform.position = new Vector3(0f, 0f, depthZ);

            MeshFilter mf = obj.AddComponent<MeshFilter>();
            MeshRenderer mr = obj.AddComponent<MeshRenderer>();

            Mesh mesh = new Mesh { name = name + "_Mesh" };

            Vector3[] verts = new Vector3[(segments + 1) * 2];
            Vector2[] uvs = new Vector2[verts.Length];
            int[] tris = new int[segments * 6];

            float dx = width / segments;
            float halfW = width * 0.5f;

            for (int i = 0; i <= segments; i++)
            {
                float x = i * dx - halfW;
                float u = (float)i / segments;

                float peak = Mathf.Sin(x * freq + phase) * 0.5f
                           + Mathf.Cos(x * freq * 2.1f + phase * 1.5f) * 0.3f
                           + Mathf.Abs(Mathf.Sin(x * freq * 0.8f + 0.4f)) * 0.4f;
                float yTop = baseY + Mathf.Max(0.1f, peak * peakHeight);
                float yBottom = baseY - 2.5f;

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
            Material cloudMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/M_DistantClouds.mat");
            if (cloudMat == null) return;

            Vector3[] cloudPositions = {
                new Vector3(-10f, 5.2f, depthZ),
                new Vector3(4f, 5.8f, depthZ - 1f),
                new Vector3(14f, 4.8f, depthZ + 1f)
            };
            Vector3[] cloudScales = {
                new Vector3(10f, 3.2f, 1f),
                new Vector3(12f, 3.6f, 1f),
                new Vector3(9f, 2.8f, 1f)
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

        private static void CreateStylizedGodRays()
        {
            Material raysMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/M_StylizedGodRays.mat");
            if (raysMat == null) return;

            GameObject rays = GameObject.CreatePrimitive(PrimitiveType.Quad);
            rays.name = "Atmosphere_GodRays";
            rays.transform.position = new Vector3(3.8f, 2.8f, 8.0f);
            rays.transform.rotation = Quaternion.Euler(0f, 0f, -24f);
            rays.transform.localScale = new Vector3(14f, 11f, 1f);
            rays.GetComponent<Renderer>().sharedMaterial = raysMat;
            Object.DestroyImmediate(rays.GetComponent<Collider>());
        }

        private static void CreateGoldenMotes()
        {
            GameObject motesObj = new GameObject("Atmosphere_GoldenMotes");
            motesObj.transform.position = new Vector3(0f, 1.5f, 0f);

            var ps = motesObj.AddComponent<ParticleSystem>();
            var psRenderer = motesObj.GetComponent<ParticleSystemRenderer>();

            var main = ps.main;
            main.startLifetime = 7f;
            main.startSpeed = 0.25f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.16f);
            main.startColor = new Color(1.0f, 0.88f, 0.55f, 0.75f);
            main.maxParticles = 80;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 12f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(25f, 6f, 8f);

            var velOverLifetime = ps.velocityOverLifetime;
            velOverLifetime.enabled = true;
            velOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.15f, 0.25f);
            velOverLifetime.y = new ParticleSystem.MinMaxCurve(0.08f, 0.20f);
            velOverLifetime.z = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);

            var colOverLifetime = ps.colorOverLifetime;
            colOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.85f, 0.5f), 0f), new GradientColorKey(new Color(1f, 0.95f, 0.7f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.85f, 0.25f), new GradientAlphaKey(0.85f, 0.75f), new GradientAlphaKey(0f, 1f) }
            );
            colOverLifetime.color = grad;

            Material motesMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/M_GoldenMotes.mat");
            if (motesMat != null)
            {
                psRenderer.sharedMaterial = motesMat;
            }
        }

        private static GameObject CreateSubdividedWaterPlane(string name, float width, float length, float startZ, float startY, float endY)
        {
            GameObject planeObj = new GameObject(name);
            planeObj.transform.position = Vector3.zero;

            MeshFilter mf = planeObj.AddComponent<MeshFilter>();
            MeshRenderer mr = planeObj.AddComponent<MeshRenderer>();

            int segX = 75;
            int segZ = 45;
            Mesh mesh = new Mesh { name = "SubdividedWaterMesh" };

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

        private static void BuildWoodenPierStructure(Transform parent)
        {
            Material timberMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/M_Playground_Timber.mat");
            Material lanternGlowMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/M_Boat_Lantern.mat");

            // Continuous Gameplay Collider
            GameObject colObj = new GameObject("Gameplay_Pier_Collider");
            colObj.transform.SetParent(parent, false);
            colObj.transform.position = new Vector3(-2.0f, -0.6f, 0f);
            var boxCol = colObj.AddComponent<BoxCollider>();
            boxCol.size = new Vector3(22.0f, 0.4f, 2.8f);

            // Longitudinal Timber Support Beams
            GameObject beamFront = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beamFront.name = "Support_Beam_Front";
            beamFront.transform.SetParent(parent, false);
            beamFront.transform.position = new Vector3(-2.0f, -0.72f, -0.65f);
            beamFront.transform.localScale = new Vector3(21.0f, 0.22f, 0.25f);
            beamFront.GetComponent<Renderer>().sharedMaterial = timberMat;
            Object.DestroyImmediate(beamFront.GetComponent<Collider>());

            GameObject beamBack = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beamBack.name = "Support_Beam_Back";
            beamBack.transform.SetParent(parent, false);
            beamBack.transform.position = new Vector3(-2.0f, -0.72f, 0.65f);
            beamBack.transform.localScale = new Vector3(21.0f, 0.22f, 0.25f);
            beamBack.GetComponent<Renderer>().sharedMaterial = timberMat;
            Object.DestroyImmediate(beamBack.GetComponent<Collider>());

            // Deck Planks (Modular Wooden Slats with Organic Jitter)
            float startX = -12.0f;
            float endX = 7.0f;
            float plankStep = 0.42f;
            int plankIndex = 0;

            for (float x = startX; x <= endX; x += plankStep)
            {
                GameObject plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plank.name = $"Deck_Plank_{plankIndex:D2}";
                plank.transform.SetParent(parent, false);

                float yJitter = Random.Range(-0.012f, 0.012f);
                float rotJitter = Random.Range(-1.2f, 1.2f);
                float lengthJitter = Random.Range(1.8f, 2.1f);

                plank.transform.position = new Vector3(x, -0.52f + yJitter, 0f);
                plank.transform.rotation = Quaternion.Euler(0f, rotJitter, 0f);
                plank.transform.localScale = new Vector3(0.36f, 0.12f, lengthJitter);
                plank.GetComponent<Renderer>().sharedMaterial = timberMat;
                Object.DestroyImmediate(plank.GetComponent<Collider>());

                plankIndex++;
            }

            // Stilt Pilings (Wooden Posts extending deep into water)
            float[] pilingX = { -11.5f, -9.5f, -7.5f, -5.5f, -3.5f, -1.5f, 0.5f, 2.5f, 4.5f, 6.5f };
            for (int i = 0; i < pilingX.Length; i++)
            {
                float x = pilingX[i];

                // Front Stilt
                GameObject stiltF = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stiltF.name = $"Stilt_Front_{i}";
                stiltF.transform.SetParent(parent, false);
                stiltF.transform.position = new Vector3(x, -2.2f, -0.65f);
                stiltF.transform.localScale = new Vector3(0.20f, 1.8f, 0.20f);
                stiltF.GetComponent<Renderer>().sharedMaterial = timberMat;
                Object.DestroyImmediate(stiltF.GetComponent<Collider>());

                // Back Stilt
                GameObject stiltB = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stiltB.name = $"Stilt_Back_{i}";
                stiltB.transform.SetParent(parent, false);
                stiltB.transform.position = new Vector3(x, -2.2f, 0.65f);
                stiltB.transform.localScale = new Vector3(0.20f, 1.8f, 0.20f);
                stiltB.GetComponent<Renderer>().sharedMaterial = timberMat;
                Object.DestroyImmediate(stiltB.GetComponent<Collider>());

                // Railing Post (Back side)
                GameObject railPost = GameObject.CreatePrimitive(PrimitiveType.Cube);
                railPost.name = $"Railing_Post_{i}";
                railPost.transform.SetParent(parent, false);
                railPost.transform.position = new Vector3(x, -0.05f, 0.85f);
                railPost.transform.localScale = new Vector3(0.12f, 0.95f, 0.12f);
                railPost.GetComponent<Renderer>().sharedMaterial = timberMat;
                Object.DestroyImmediate(railPost.GetComponent<Collider>());
            }

            // Horizontal Safety Railing Wire/Timber along back
            GameObject railingBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            railingBar.name = "Railing_TopBar";
            railingBar.transform.SetParent(parent, false);
            railingBar.transform.position = new Vector3(-2.5f, 0.35f, 0.85f);
            railingBar.transform.localScale = new Vector3(20.0f, 0.08f, 0.08f);
            railingBar.GetComponent<Renderer>().sharedMaterial = timberMat;
            Object.DestroyImmediate(railingBar.GetComponent<Collider>());

            // Lantern Posts at Key Intervals (Left, Center)
            CreatePierLanternPost(parent, new Vector3(-6.5f, -0.5f, 0.85f), timberMat, lanternGlowMat);
            CreatePierLanternPost(parent, new Vector3(0.5f, -0.5f, 0.85f), timberMat, lanternGlowMat);

            // Deck Props (Wooden crates, boxes)
            GameObject cratePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Thùng gỗ.prefab");
            GameObject slatCubePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Meshy_AI_Wooden_Slat_Cube_0927180014_texture.prefab");

            if (cratePrefab != null)
            {
                var c1 = Object.Instantiate(cratePrefab, parent);
                c1.name = "Pier_Crate_Stack_1";
                c1.transform.position = new Vector3(-8.8f, -0.45f, 0.45f);
                c1.transform.rotation = Quaternion.Euler(0f, 15f, 0f);
                c1.transform.localScale = Vector3.one * 0.7f;
            }

            if (slatCubePrefab != null)
            {
                var c2 = (GameObject)PrefabUtility.InstantiatePrefab(slatCubePrefab, parent);
                c2.name = "Pier_Slat_Cube";
                c2.transform.position = new Vector3(2.8f, -0.45f, 0.45f);
                c2.transform.rotation = Quaternion.Euler(0f, -20f, 0f);
                c2.transform.localScale = Vector3.one * 0.70f;
            }
        }

        private static void CreatePierLanternPost(Transform parent, Vector3 basePos, Material timberMat, Material glowMat)
        {
            GameObject postRoot = new GameObject("Pier_Lantern_Post");
            postRoot.transform.SetParent(parent, false);
            postRoot.transform.position = basePos;

            // Vertical pole
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Pole";
            pole.transform.SetParent(postRoot.transform, false);
            pole.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            pole.transform.localScale = new Vector3(0.09f, 1.1f, 0.09f);
            pole.GetComponent<Renderer>().sharedMaterial = timberMat;
            Object.DestroyImmediate(pole.GetComponent<Collider>());

            // Horizontal cross arm
            GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arm.name = "Arm";
            arm.transform.SetParent(postRoot.transform, false);
            arm.transform.localPosition = new Vector3(-0.25f, 2.15f, 0f);
            arm.transform.localScale = new Vector3(0.55f, 0.07f, 0.07f);
            arm.GetComponent<Renderer>().sharedMaterial = timberMat;
            Object.DestroyImmediate(arm.GetComponent<Collider>());

            // Hanging Lantern
            GameObject lantern = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lantern.name = "Lantern_Bulb";
            lantern.transform.SetParent(postRoot.transform, false);
            lantern.transform.localPosition = new Vector3(-0.48f, 1.95f, 0f);
            lantern.transform.localScale = Vector3.one * 0.22f;
            lantern.GetComponent<Renderer>().sharedMaterial = glowMat;
            Object.DestroyImmediate(lantern.GetComponent<Collider>());

            // Point Light
            Light light = lantern.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1.0f, 0.78f, 0.42f);
            light.intensity = 2.2f;
            light.range = 5.5f;
            light.shadows = LightShadows.Soft;
        }

        private static void BuildLeftShore(Transform parent)
        {
            Material groundMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/M_Foreground_Ground.mat");
            Material rockMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/M_StylizedRock_Silhouette.mat");

            // Shore ground bank connecting into pier
            GameObject bank = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bank.name = "LeftShore_Ground";
            bank.transform.SetParent(parent, false);
            bank.transform.position = new Vector3(-13.5f, -1.0f, 0.5f);
            bank.transform.localScale = new Vector3(6.5f, 2.2f, 4.0f);
            bank.GetComponent<Renderer>().sharedMaterial = groundMat;

            // 3D Rocks
            GameObject rock01 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Environment/SM_Rock_Small_01.fbx");
            GameObject rock02 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Environment/SM_Rock_Small_02.fbx");

            if (rock01 != null)
            {
                var r1 = Object.Instantiate(rock01, parent);
                r1.name = "Shore_Rock_1";
                r1.transform.position = new Vector3(-12.8f, -0.45f, 0.8f);
                r1.transform.rotation = Quaternion.Euler(-10f, 45f, 15f);
                r1.transform.localScale = Vector3.one * 1.35f;
                if (rockMat != null) ApplyMaterialToRenderers(r1, rockMat);
            }

            if (rock02 != null)
            {
                var r2 = Object.Instantiate(rock02, parent);
                r2.name = "Shore_Rock_2";
                r2.transform.position = new Vector3(-10.5f, -0.8f, 2.2f);
                r2.transform.rotation = Quaternion.Euler(0f, 85f, -10f);
                r2.transform.localScale = Vector3.one * 1.1f;
                if (rockMat != null) ApplyMaterialToRenderers(r2, rockMat);
            }

            // 3D Stylized Grass Tufts along shore
            GameObject grassPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/PF_StylizedGrassTuft_3D.prefab");
            if (grassPrefab != null)
            {
                float[] gx = { -13.2f, -12.4f, -11.6f, -10.9f };
                for (int i = 0; i < gx.Length; i++)
                {
                    var g = (GameObject)PrefabUtility.InstantiatePrefab(grassPrefab, parent);
                    g.name = $"Shore_Grass_{i}";
                    g.transform.position = new Vector3(gx[i], -0.38f, 0.4f + (i % 2) * 0.3f);
                    g.transform.rotation = Quaternion.Euler(0f, (i * 65f) % 360f, 0f);
                    g.transform.localScale = Vector3.one * 0.85f;
                }
            }
        }

        private static void BuildCoastalStiltVillage(Transform parent)
        {
            Material timberMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/M_Playground_Timber.mat");
            Material roofMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/M_Playground_Crate.mat");
            Material glowMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/M_Boat_Lantern.mat");

            // Village Main Platform Deck
            GameObject villageDeck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            villageDeck.name = "Village_Platform_Main";
            villageDeck.transform.SetParent(parent, false);
            villageDeck.transform.position = new Vector3(8.5f, -0.45f, 2.5f);
            villageDeck.transform.localScale = new Vector3(8.0f, 0.25f, 6.0f);
            villageDeck.GetComponent<Renderer>().sharedMaterial = timberMat;

            // Stilts under the village deck
            for (int x = 0; x <= 2; x++)
            {
                for (int z = 0; z <= 2; z++)
                {
                    GameObject stilt = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    stilt.name = $"Village_Stilt_{x}_{z}";
                    stilt.transform.SetParent(parent, false);
                    stilt.transform.position = new Vector3(5.5f + x * 3.0f, -2.2f, 0.5f + z * 2.2f);
                    stilt.transform.localScale = new Vector3(0.24f, 1.8f, 0.24f);
                    stilt.GetComponent<Renderer>().sharedMaterial = timberMat;
                    Object.DestroyImmediate(stilt.GetComponent<Collider>());
                }
            }

            // Stilt House 1 (Main Village Hut, clearly visible at X = 7.8)
            CreateStiltHut(parent, "Village_Hut_Main", new Vector3(7.8f, 0.75f, 2.8f), new Vector3(3.2f, 2.1f, 3.0f), timberMat, roofMat, glowMat);

            // Stilt House 2 (Adjacent higher hut behind it at X = 11.2)
            CreateStiltHut(parent, "Village_Hut_Back", new Vector3(11.2f, 1.15f, 5.0f), new Vector3(3.6f, 2.4f, 3.2f), timberMat, roofMat, glowMat);
        }

        private static void CreateStiltHut(Transform parent, string name, Vector3 pos, Vector3 size, Material wallMat, Material roofMat, Material glowMat)
        {
            GameObject hut = new GameObject(name);
            hut.transform.SetParent(parent, false);
            hut.transform.position = pos;

            // Walls (Body)
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Walls";
            body.transform.SetParent(hut.transform, false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = size;
            body.GetComponent<Renderer>().sharedMaterial = wallMat;

            // Pitched Roof
            float roofOverhang = 0.45f;
            float halfW = size.x * 0.5f;

            GameObject roofLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roofLeft.name = "Roof_Left";
            roofLeft.transform.SetParent(hut.transform, false);
            roofLeft.transform.localPosition = new Vector3(-halfW * 0.5f, size.y * 0.5f + 0.45f, 0f);
            roofLeft.transform.localRotation = Quaternion.Euler(0f, 0f, 30f);
            roofLeft.transform.localScale = new Vector3(halfW + roofOverhang, 0.14f, size.z + roofOverhang);
            roofLeft.GetComponent<Renderer>().sharedMaterial = roofMat;
            Object.DestroyImmediate(roofLeft.GetComponent<Collider>());

            GameObject roofRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roofRight.name = "Roof_Right";
            roofRight.transform.SetParent(hut.transform, false);
            roofRight.transform.localPosition = new Vector3(halfW * 0.5f, size.y * 0.5f + 0.45f, 0f);
            roofRight.transform.localRotation = Quaternion.Euler(0f, 0f, -30f);
            roofRight.transform.localScale = new Vector3(halfW + roofOverhang, 0.14f, size.z + roofOverhang);
            roofRight.GetComponent<Renderer>().sharedMaterial = roofMat;
            Object.DestroyImmediate(roofRight.GetComponent<Collider>());

            // Warm Lantern under the eave
            GameObject lantern = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lantern.name = "Eave_Lantern";
            lantern.transform.SetParent(hut.transform, false);
            lantern.transform.localPosition = new Vector3(-halfW - 0.2f, size.y * 0.25f, -size.z * 0.35f);
            lantern.transform.localScale = Vector3.one * 0.24f;
            lantern.GetComponent<Renderer>().sharedMaterial = glowMat;
            Object.DestroyImmediate(lantern.GetComponent<Collider>());

            Light hutLight = lantern.AddComponent<Light>();
            hutLight.type = LightType.Point;
            hutLight.color = new Color(1.0f, 0.76f, 0.38f);
            hutLight.intensity = 2.4f;
            hutLight.range = 6.0f;
            hutLight.shadows = LightShadows.Soft;
        }

        private static void BuildFloatingBoats(Transform parent)
        {
            GameObject boatPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/PF_StylizedBoat.prefab");
            if (boatPrefab == null) return;

            // Boat 1: Moored beside the pier in midground (X = -5.2, Z = 3.2)
            var b1 = (GameObject)PrefabUtility.InstantiatePrefab(boatPrefab, parent);
            b1.name = "Floating_Boat_NearPier";
            b1.transform.position = new Vector3(-5.2f, -0.68f, 3.2f);
            b1.transform.rotation = Quaternion.Euler(0f, 25f, 0f);
            b1.transform.localScale = Vector3.one * 0.75f;

            var floatScript1 = b1.GetComponent<FloatingBoatSimple>();
            if (floatScript1 == null) floatScript1 = b1.AddComponent<FloatingBoatSimple>();
            floatScript1.SetParameters(0.05f, 1.2f, 2.2f, 0.4f);

            // Boat 2: Village boat near stilt dock (X = 4.8, Z = 4.5)
            var b2 = (GameObject)PrefabUtility.InstantiatePrefab(boatPrefab, parent);
            b2.name = "Floating_Boat_Village";
            b2.transform.position = new Vector3(4.8f, -0.2f, 4.5f);
            b2.transform.rotation = Quaternion.Euler(0f, -25f, 0f);
            b2.transform.localScale = Vector3.one * 0.60f;

            var floatScript2 = b2.GetComponent<FloatingBoatSimple>();
            if (floatScript2 == null) floatScript2 = b2.AddComponent<FloatingBoatSimple>();
            floatScript2.SetParameters(0.04f, 1.0f, 1.8f, 1.6f);

            // Boat 3: Distant Junk Boat with tall sails (X = -8.0, Z = 15.0)
            var b3 = (GameObject)PrefabUtility.InstantiatePrefab(boatPrefab, parent);
            b3.name = "Distant_Junk_Ship";
            b3.transform.position = new Vector3(-8.0f, 0.5f, 15.0f);
            b3.transform.rotation = Quaternion.Euler(0f, -12f, 0f);
            b3.transform.localScale = Vector3.one * 1.35f;

            var floatScript3 = b3.GetComponent<FloatingBoatSimple>();
            if (floatScript3 == null) floatScript3 = b3.AddComponent<FloatingBoatSimple>();
            floatScript3.SetParameters(0.035f, 0.75f, 1.2f, 2.5f);
        }

        private static void BuildNaturalTrees(Transform parent)
        {
            // Use authentic Planet of Lana tree prefabs from the project
            GameObject heroTreePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Lana_Artistic_Hero_Tree.prefab");
            GameObject layeredTreePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Hero_Layered_Tree.prefab");

            // Hero Tree arching over the Left Shore
            if (heroTreePrefab != null)
            {
                var treeL = (GameObject)PrefabUtility.InstantiatePrefab(heroTreePrefab, parent);
                treeL.name = "Left_Hero_Tree";
                treeL.transform.position = new Vector3(-10.8f, -0.4f, 1.5f);
                treeL.transform.rotation = Quaternion.Euler(0f, 15f, 0f);
                treeL.transform.localScale = Vector3.one * 0.85f;
            }

            // Layered Tree behind the Village on the Right
            if (layeredTreePrefab != null)
            {
                var treeR = (GameObject)PrefabUtility.InstantiatePrefab(layeredTreePrefab, parent);
                treeR.name = "Village_Backdrop_Tree";
                treeR.transform.position = new Vector3(12.5f, -0.3f, 4.0f);
                treeR.transform.rotation = Quaternion.Euler(0f, -20f, 0f);
                treeR.transform.localScale = Vector3.one * 0.95f;
            }
        }

        private static GameObject SpawnPlayerCharacter(Transform parent)
        {
            GameObject charPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Characters/Character_Rigged_Animations.fbx");
            if (charPrefab == null) charPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Characters/Character_An.fbx");
            if (charPrefab == null) return null;

            GameObject player = Object.Instantiate(charPrefab, parent);
            player.name = "Hero_Boy_Runner";
            player.tag = "Player";
            player.transform.position = new Vector3(-2.0f, -0.42f, 0f);
            player.transform.rotation = Quaternion.Euler(0f, 90f, 0f); // Facing right along the pier
            player.transform.localScale = Vector3.one * 0.95f;

            // Apply character texture
            Texture2D charTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/MeshyImports/Boy Character Rig_20260926_073036/meshy_basecolor.png");
            if (charTex == null) charTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/MeshyImports/Boy Character Rig_20260926_073041/meshy_basecolor.png");
            if (charTex != null)
            {
                var r = player.GetComponentInChildren<Renderer>();
                if (r != null && r.sharedMaterial != null)
                {
                    r.sharedMaterial.mainTexture = charTex;
                    r.sharedMaterial.SetColor("_BaseColor", Color.white);
                }
            }

            var cc = player.GetComponent<CharacterController>();
            if (cc == null) cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            var playerCtrl = player.GetComponent<PlayerController>();
            if (playerCtrl == null) playerCtrl = player.AddComponent<PlayerController>();

            var animator = player.GetComponent<Animator>();
            if (animator != null)
            {
                var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/Controllers/AC_Hero_GapBung.controller");
                if (controller != null) animator.runtimeAnimatorController = controller;
            }

            return player;
        }

        private static void ApplyMaterialToRenderers(GameObject go, Material mat)
        {
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                r.sharedMaterial = mat;
            }
        }
    }
}
