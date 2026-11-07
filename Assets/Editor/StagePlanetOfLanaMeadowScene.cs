#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using HoaVaMau.Gameplay;

namespace HoaDo.EditorTools
{
    public static class StagePlanetOfLanaMeadowScene
    {
        private const string ScenePath = "Assets/Scenes/Scene_PlanetOfLana_GrassMeadow.unity";
        private const string GrassMatPath = "Assets/Materials/M_PlanetOfLana_MeadowGrass.mat";
        private const string GroundMatPath = "Assets/Materials/M_PlanetOfLana_GroundTurf.mat";

        [MenuItem("Tools/Planet of Lana/Stage Grass Meadow Scene (New Dedicated Scene)", priority = 1)]
        public static void BuildScene()
        {
            Debug.Log("[StagePlanetOfLanaMeadowScene] === Bắt đầu khởi tạo Scene Đồi Cỏ Planet of Lana ===");

            // 1. Chuẩn bị thư mục
            EnsureFolder("Assets/Scenes");
            EnsureFolder("Assets/Materials");

            // 2. Tạo hoặc cấu hình Material Cỏ Vẽ Tay Đậm Chất Thơ (Painterly Shader)
            Material grassMat = SetupMeadowGrassMaterial();

            // 3. Tạo Material Đất Nền
            Material groundMat = SetupGroundMaterial();

            // 4. Khởi tạo Scene mới hoàn toàn độc lập
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 5. Thiết lập Ánh sáng & Môi trường (Warm Sunny Day)
            SetupLightingAndEnvironment();

            // 6. Camera điện ảnh góc 16:9
            SetupCinematicCamera();

            // 7. Tầng Trời Sâu & Vành Đai Hành Tinh Vũ Trụ (Deep Celestial Sky)
            SetupDeepSkyBackground();

            // 8. Tầng Đồi Núi Xa & Mây Ghibli (Atmospheric Hills & Cumulus Clouds)
            SetupCloudsAndDistantHills();

            // 9. Nền Đất Lượn Sóng (Rolling Hill Slope Mesh)
            SetupRollingGroundSlope(groundMat);

            // 10. THẢM CỎ PAINTERLY ĐA TẦNG VỚI HÀNG TRĂM KHÓM CỎ CHÍNH THỨC
            SetupLushMeadowGrassField(grassMat);

            // 11. Các yếu tố Đóng Khung (Framing Trees & Rocks)
            SetupFramingTreesAndRocks();

            // 12. Tầng Tiền Cảnh Đen (Foreground Silhouettes)
            SetupForegroundSilhouettes();

            // 13. Giao Diện Tối Giản Phong Cách Menu Planet of Lana (UI Canvas)
            SetupMinimalistMenuUI();

            // 14. Lưu và hoàn tất Scene
            EditorSceneManager.SaveScene(newScene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[StagePlanetOfLanaMeadowScene] === HOÀN TẤT THÀNH CÔNG! Scene đã lưu tại: {ScenePath} ===");
        }

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path).Replace("\\", "/");
                string folderName = Path.GetFileName(path);
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }

        private static Material SetupMeadowGrassMaterial()
        {
            Shader grassShader = Shader.Find("HoaVaMau/PlanetOfLana_PainterlyGrass")
                              ?? Shader.Find("HoaVaMau/Stylized3DGrassTuft")
                              ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(GrassMatPath);
            if (mat == null)
            {
                mat = new Material(grassShader) { name = "M_PlanetOfLana_MeadowGrass" };
                AssetDatabase.CreateAsset(mat, GrassMatPath);
            }
            else
            {
                mat.shader = grassShader;
            }

            // Dải màu 3 tầng chuẩn watercolor nghệ thuật Planet of Lana
            mat.SetColor("_BaseShadowColor", new Color(0.10f, 0.22f, 0.11f, 1.0f)); // Chân cỏ bóng râm sẫm
            mat.SetColor("_MidBladeColor",   new Color(0.42f, 0.63f, 0.24f, 1.0f)); // Thân cỏ xanh lục tự nhiên
            mat.SetColor("_TipSunColor",     new Color(0.89f, 0.96f, 0.58f, 1.0f)); // Ngọn cỏ ngả vàng nắng
            mat.SetColor("_SunbeamGlintColor", new Color(1.0f, 0.99f, 0.78f, 1.0f)); // Vệt nắng gắt lấp lánh

            mat.SetFloat("_ColorGradientStrength", 0.75f);
            mat.SetFloat("_DappleScale", 0.14f);
            mat.SetFloat("_DappleSpeed", 0.65f);
            mat.SetFloat("_DappleIntensity", 0.50f);

            mat.SetFloat("_WindSpeed", 1.55f);
            mat.SetFloat("_WindFrequency", 0.45f);
            mat.SetFloat("_WindSwayStrength", 0.18f);
            mat.SetFloat("_TipFlutterSpeed", 8.0f);
            mat.SetFloat("_TipFlutterStrength", 0.04f);

            mat.SetFloat("_NormalUpFactor", 0.85f);
            mat.SetFloat("_AmbientLightMin", 0.65f);

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material SetupGroundMaterial()
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") 
                            ?? Shader.Find("Universal Render Pipeline/Unlit");

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(GroundMatPath);
            if (mat == null)
            {
                mat = new Material(litShader) { name = "M_PlanetOfLana_GroundTurf" };
                AssetDatabase.CreateAsset(mat, GroundMatPath);
            }

            Texture2D groundTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/LanaStyle_Ground_Seamless.png")
                               ?? AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/PainterlyGround_Seamless.png");

            if (groundTex != null)
            {
                mat.mainTexture = groundTex;
                mat.mainTextureScale = new Vector2(4f, 2f);
            }
            mat.color = new Color(0.48f, 0.58f, 0.32f, 1.0f);

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void SetupLightingAndEnvironment()
        {
            GameObject lightRoot = new GameObject("00_Environment_Lighting");

            // Direct Sunlight
            GameObject dirLightGo = new GameObject("Sun_Directional");
            dirLightGo.transform.SetParent(lightRoot.transform);
            dirLightGo.transform.rotation = Quaternion.Euler(42f, -35f, 0f);

            Light light = dirLightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1.0f, 0.96f, 0.88f);
            light.intensity = 1.25f;
            light.shadows = LightShadows.Soft;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.75f, 0.88f);
            RenderSettings.skybox = null;
        }

        private static void SetupCinematicCamera()
        {
            GameObject camGo = new GameObject("01_Camera_Cinematic");
            camGo.transform.position = new Vector3(0f, 1.25f, -8.6f);
            camGo.transform.rotation = Quaternion.identity;

            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.32f, 0.66f, 0.78f); // Bầu trời lam trong vắt
            cam.fieldOfView = 40f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 120f;
            cam.tag = "MainCamera";

            camGo.AddComponent<AudioListener>();
        }

        private static void SetupDeepSkyBackground()
        {
            GameObject skyRoot = new GameObject("02_Background_DeepSky");

            // 1. Sky Gradient / Background vista
            Sprite skySpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Backgrounds_Lana/BG_Lana_MainSkyHills_Official.png")
                         ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Sky/game_background_sky.png");

            if (skySpr != null)
            {
                GameObject skyPlane = new GameObject("Sky_Panorama_Plate");
                skyPlane.transform.SetParent(skyRoot.transform);
                skyPlane.transform.position = new Vector3(0f, 3.2f, 35f);
                skyPlane.transform.localScale = new Vector3(4.5f, 4.5f, 1f);

                var sr = skyPlane.AddComponent<SpriteRenderer>();
                sr.sprite = skySpr;
                sr.sortingOrder = -100;
            }

            // 2. Vành Đai Hành Tinh Vũ Trụ Khổng Lồ (Giant Celestial Moon / Ring)
            Sprite ringSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Sky/celestial_ring.png")
                          ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Sky/giant_celestial_moon.png");

            if (ringSpr != null)
            {
                GameObject ringGo = new GameObject("Celestial_Ring_Moon");
                ringGo.transform.SetParent(skyRoot.transform);
                ringGo.transform.position = new Vector3(-0.6f, 5.2f, 28f);
                ringGo.transform.localScale = new Vector3(5.2f, 5.2f, 1f);

                var sr = ringGo.AddComponent<SpriteRenderer>();
                sr.sprite = ringSpr;
                sr.color = new Color(0.92f, 0.98f, 1.0f, 0.70f);
                sr.sortingOrder = -90;
            }
        }

        private static void SetupCloudsAndDistantHills()
        {
            GameObject cloudRoot = new GameObject("03_Distant_Clouds_And_Hills");

            // 1. Mây trắng bồng bềnh Ghibli bên phải (Cumulus cloud)
            Sprite cloud1 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Clouds/cumulus_cloud_02.png")
                         ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Clouds/lana_cloud_01.png");

            if (cloud1 != null)
            {
                GameObject c1 = new GameObject("Cumulus_Cloud_Tower_Right");
                c1.transform.SetParent(cloudRoot.transform);
                c1.transform.position = new Vector3(4.8f, 1.8f, 20f);
                c1.transform.localScale = new Vector3(3.8f, 3.8f, 1f);

                var sr = c1.AddComponent<SpriteRenderer>();
                sr.sprite = cloud1;
                sr.color = new Color(1.0f, 0.98f, 0.95f, 0.95f);
                sr.sortingOrder = -80;
            }

            // 2. Mây thấp bên trái
            Sprite cloud2 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Clouds/lana_cloud_02.png");
            if (cloud2 != null)
            {
                GameObject c2 = new GameObject("Cumulus_Cloud_Left");
                c2.transform.SetParent(cloudRoot.transform);
                c2.transform.position = new Vector3(-4.2f, 0.6f, 22f);
                c2.transform.localScale = new Vector3(3.2f, 3.2f, 1f);

                var sr = c2.AddComponent<SpriteRenderer>();
                sr.sprite = cloud2;
                sr.color = new Color(0.95f, 0.95f, 0.98f, 0.85f);
                sr.sortingOrder = -81;
            }

            // 3. Đồi núi xanh ở hậu cảnh xa
            Sprite hillsSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Backgrounds_Lana/BG_Lana_PlainsHills_Day.png");
            if (hillsSpr != null)
            {
                GameObject hillsGo = new GameObject("Distant_Green_Hills");
                hillsGo.transform.SetParent(cloudRoot.transform);
                hillsGo.transform.position = new Vector3(0f, -0.6f, 14f);
                hillsGo.transform.localScale = new Vector3(2.6f, 2.2f, 1f);

                var sr = hillsGo.AddComponent<SpriteRenderer>();
                sr.sprite = hillsSpr;
                sr.color = new Color(0.78f, 0.88f, 0.92f, 1.0f); // Sương mờ khí quyển
                sr.sortingOrder = -50;
            }
        }

        private static float GetGroundY(float x, float z)
        {
            // Đường dốc thoải: cao ở mỏm đá bên trái (-6m), võng nhẹ ở thung lũng giữa, lài về bên phải
            float hillCurve = Mathf.Sin(x * 0.18f + 0.35f) * 0.55f;
            float slopeX = -x * 0.05f;
            float depthDrop = -z * 0.12f;
            return -0.45f + hillCurve + slopeX + depthDrop;
        }

        private static void SetupRollingGroundSlope(Material groundMat)
        {
            GameObject groundGo = new GameObject("04_Rolling_Ground_Slope");
            groundGo.transform.position = Vector3.zero;

            MeshFilter mf = groundGo.AddComponent<MeshFilter>();
            MeshRenderer mr = groundGo.AddComponent<MeshRenderer>();
            mr.sharedMaterial = groundMat;

            // Xây dựng mặt đất cong tự nhiên (Grid mesh)
            int segmentsX = 40;
            int segmentsZ = 16;
            float minX = -18f, maxX = 18f;
            float minZ = -1.2f, maxZ = 7.0f;

            int vertCount = (segmentsX + 1) * (segmentsZ + 1);
            Vector3[] vertices = new Vector3[vertCount];
            Vector2[] uvs = new Vector2[vertCount];
            Vector3[] normals = new Vector3[vertCount];

            int idx = 0;
            for (int iz = 0; iz <= segmentsZ; iz++)
            {
                float tz = (float)iz / segmentsZ;
                float z = Mathf.Lerp(minZ, maxZ, tz);

                for (int ix = 0; ix <= segmentsX; ix++)
                {
                    float tx = (float)ix / segmentsX;
                    float x = Mathf.Lerp(minX, maxX, tx);
                    float y = GetGroundY(x, z);

                    vertices[idx] = new Vector3(x, y, z);
                    uvs[idx] = new Vector2(tx * 8f, tz * 3f);
                    normals[idx] = Vector3.up;
                    idx++;
                }
            }

            int triCount = segmentsX * segmentsZ * 6;
            int[] triangles = new int[triCount];
            int triIdx = 0;

            for (int iz = 0; iz < segmentsZ; iz++)
            {
                for (int ix = 0; ix < segmentsX; ix++)
                {
                    int row1 = iz * (segmentsX + 1);
                    int row2 = (iz + 1) * (segmentsX + 1);

                    triangles[triIdx++] = row1 + ix;
                    triangles[triIdx++] = row2 + ix;
                    triangles[triIdx++] = row1 + ix + 1;

                    triangles[triIdx++] = row1 + ix + 1;
                    triangles[triIdx++] = row2 + ix;
                    triangles[triIdx++] = row2 + ix + 1;
                }
            }

            Mesh mesh = new Mesh();
            mesh.name = "RollingMeadowGroundMesh";
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.normals = normals;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            mf.sharedMesh = mesh;
        }

        private static void SetupLushMeadowGrassField(Material grassMat)
        {
            GameObject meadowRoot = new GameObject("05_Lush_Meadow_Grass_Field");

            // 1. Tải danh sách sprite cỏ chính thức
            List<Sprite> grassSprites = LoadOfficialGrassSprites();
            List<Sprite> flowerSprites = LoadOfficialFlowerSprites();

            if (grassSprites.Count == 0)
            {
                Debug.LogWarning("[StagePlanetOfLanaMeadowScene] Không tìm thấy sprite cỏ trong thư mục Grass!");
                return;
            }

            // 2. Rải cỏ theo 5 tầng sâu để tạo độ dày đặc và lớp lang
            int totalGrassCount = 0;
            float[] depthTiers = new float[] { -0.5f, 0.6f, 1.8f, 3.0f, 4.4f };
            int[] countsPerTier = new int[] { 36, 48, 56, 52, 42 };

            for (int t = 0; t < depthTiers.Length; t++)
            {
                float baseZ = depthTiers[t];
                int count = countsPerTier[t];

                GameObject tierRoot = new GameObject($"Grass_Tier_{t:00}_Z{baseZ:0.0}m");
                tierRoot.transform.SetParent(meadowRoot.transform);

                float stepX = 23.0f / (count - 1);
                for (int i = 0; i < count; i++)
                {
                    float x = -11.5f + (i * stepX) + Random.Range(-0.25f, 0.25f);
                    float z = baseZ + Random.Range(-0.2f, 0.2f);
                    float y = GetGroundY(x, z);

                    Sprite pickedSpr = grassSprites[Random.Range(0, grassSprites.Count)];

                    GameObject tuft = new GameObject($"Tuft_{t}_{i:00}");
                    tuft.transform.SetParent(tierRoot.transform);
                    tuft.transform.position = new Vector3(x, y, z);

                    float scale = Random.Range(0.85f, 1.25f);
                    tuft.transform.localScale = new Vector3(scale, scale, 1f);

                    var sr = tuft.AddComponent<SpriteRenderer>();
                    sr.sprite = pickedSpr;
                    sr.sharedMaterial = grassMat;
                    sr.flipX = (Random.value > 0.5f);

                    // Thêm chuyển động đung đưa theo gió hữu cơ quanh gốc (Pivot Sway)
                    var sway = tuft.AddComponent<PlanetOfLanaFoliageSway>();
                    ConfigureFoliageSway(sway, 3.2f, 1.55f);

                    totalGrassCount++;

                    // Thỉnh thoảng cắm một bông hoa dại nhỏ giữa các khóm cỏ
                    if (flowerSprites.Count > 0 && Random.value < 0.16f)
                    {
                        Sprite flowerSpr = flowerSprites[Random.Range(0, flowerSprites.Count)];
                        GameObject flowerGo = new GameObject($"Wildflower_{t}_{i:00}");
                        flowerGo.transform.SetParent(tierRoot.transform);
                        flowerGo.transform.position = new Vector3(x + Random.Range(-0.15f, 0.15f), y + 0.1f, z - 0.05f);
                        flowerGo.transform.localScale = Vector3.one * Random.Range(0.7f, 1.05f);

                        var flowerSr = flowerGo.AddComponent<SpriteRenderer>();
                        flowerSr.sprite = flowerSpr;
                        flowerSr.sharedMaterial = grassMat; // Sử dụng cùng material để hưởng hiệu ứng gió và nắng

                        var flowerSway = flowerGo.AddComponent<PlanetOfLanaFoliageSway>();
                        ConfigureFoliageSway(flowerSway, 4.0f, 1.8f);
                    }
                }
            }

            Debug.Log($"[StagePlanetOfLanaMeadowScene] Đã rải thành công {totalGrassCount} khóm cỏ trên đồi.");
        }

        private static List<Sprite> LoadOfficialGrassSprites()
        {
            var list = new List<Sprite>();
            string[] guids = AssetDatabase.FindAssets("t:Sprite", new string[] { "Assets/Art/PlanetOfLana_Official/Grass" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Sprite spr = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (spr != null) list.Add(spr);
            }

            // Bổ sung các sprite cỏ painterly nếu có
            string[] fallbackGuids = AssetDatabase.FindAssets("t:Sprite", new string[] { "Assets/Textures/Grass" });
            foreach (string guid in fallbackGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Sprite spr = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (spr != null && !list.Contains(spr)) list.Add(spr);
            }

            return list;
        }

        private static List<Sprite> LoadOfficialFlowerSprites()
        {
            var list = new List<Sprite>();
            string[] guids = AssetDatabase.FindAssets("t:Sprite", new string[] { "Assets/Art/PlanetOfLana_Official/Flora" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string filename = Path.GetFileNameWithoutExtension(path).ToLower();
                if (filename.Contains("white") || filename.Contains("yellow") || filename.Contains("flower"))
                {
                    Sprite spr = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (spr != null) list.Add(spr);
                }
            }
            return list;
        }

        private static void ConfigureFoliageSway(PlanetOfLanaFoliageSway sway, float angle, float speed)
        {
            var so = new SerializedObject(sway);
            var angleProp = so.FindProperty("maxRotationAngle");
            if (angleProp != null) angleProp.floatValue = angle;

            var speedProp = so.FindProperty("swaySpeed");
            if (speedProp != null) speedProp.floatValue = speed;

            var offsetProp = so.FindProperty("useWorldPositionOffset");
            if (offsetProp != null) offsetProp.boolValue = true;

            var noiseProp = so.FindProperty("harmonicNoise");
            if (noiseProp != null) noiseProp.floatValue = 0.35f;

            var anchorProp = so.FindProperty("anchorBottomPivot");
            if (anchorProp != null) anchorProp.boolValue = true;

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetupFramingTreesAndRocks()
        {
            GameObject frameRoot = new GameObject("06_Framing_Trees_And_Rocks");

            // 1. Cây Cổ Thụ Uốn Lượn Bên Phải (Hero Curved Tree)
            Sprite treeRightSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Lana_Unified_Art/Trees/T_Lana_Tree_02_WindblownCurved.png")
                               ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Lana_Unified_Art/Trees/T_Lana_Tree_01_HeroBanyan.png");

            if (treeRightSpr != null)
            {
                GameObject treeRight = new GameObject("Tree_Right_HeroCurved");
                treeRight.transform.SetParent(frameRoot.transform);
                treeRight.transform.position = new Vector3(6.8f, 2.1f, -1.0f);
                treeRight.transform.localScale = new Vector3(1.85f, 1.85f, 1f);

                var sr = treeRight.AddComponent<SpriteRenderer>();
                sr.sprite = treeRightSpr;
                sr.sortingOrder = 30;
            }

            // 2. Hàng Cây Rừng Thân Mảnh Bên Trái (Slender Forest Trees)
            Sprite treeLeftSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Lana_Unified_Art/Trees/T_Lana_Tree_03_SlenderForest.png");
            if (treeLeftSpr != null)
            {
                GameObject treeLeft = new GameObject("Tree_Left_SlenderForest");
                treeLeft.transform.SetParent(frameRoot.transform);
                treeLeft.transform.position = new Vector3(-7.6f, 2.7f, -1.2f);
                treeLeft.transform.localScale = new Vector3(1.65f, 1.65f, 1f);

                var sr = treeLeft.AddComponent<SpriteRenderer>();
                sr.sprite = treeLeftSpr;
                sr.sortingOrder = 25;
            }

            // 3. Mỏm Đá Nằm Bên Trái Đồi (Left Rock Ledge)
            Sprite rockSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/PlanetOfLana_Official/Ground_Rocks/Std_Pg_HDL_Rock_001.png");
            if (rockSpr != null)
            {
                GameObject rockGo = new GameObject("Rock_Ledge_Left");
                rockGo.transform.SetParent(frameRoot.transform);
                rockGo.transform.position = new Vector3(-6.2f, -0.45f, -1.4f);
                rockGo.transform.localScale = new Vector3(1.6f, 1.6f, 1f);

                var sr = rockGo.AddComponent<SpriteRenderer>();
                sr.sprite = rockSpr;
                sr.sortingOrder = 20;
            }
        }

        private static void SetupForegroundSilhouettes()
        {
            GameObject fgRoot = new GameObject("07_Foreground_DarkSilhouettes");

            Color silhouetteColor = new Color(0.06f, 0.10f, 0.08f, 1.0f);

            // Bố trí cụm lá đen tiền cảnh góc trái
            Sprite fg1 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/PlanetOfLana_Official/Foreground/FGPlant01.png");
            if (fg1 != null)
            {
                GameObject fgLeft = new GameObject("FG_Silhouette_Left");
                fgLeft.transform.SetParent(fgRoot.transform);
                fgLeft.transform.position = new Vector3(-7.2f, -1.8f, -4.5f);
                fgLeft.transform.localScale = new Vector3(1.8f, 1.8f, 1f);

                var sr = fgLeft.AddComponent<SpriteRenderer>();
                sr.sprite = fg1;
                sr.color = silhouetteColor;
                sr.sortingOrder = 100;
            }

            // Bố trí cụm lá đen tiền cảnh góc phải
            Sprite fg2 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/PlanetOfLana_Official/Foreground/FGPlant02.png");
            if (fg2 != null)
            {
                GameObject fgRight = new GameObject("FG_Silhouette_Right");
                fgRight.transform.SetParent(fgRoot.transform);
                fgRight.transform.position = new Vector3(6.5f, -1.9f, -4.5f);
                fgRight.transform.localScale = new Vector3(2.0f, 2.0f, 1f);

                var sr = fgRight.AddComponent<SpriteRenderer>();
                sr.sprite = fg2;
                sr.color = silhouetteColor;
                sr.sortingOrder = 100;
            }
        }

        private static void SetupMinimalistMenuUI()
        {
            GameObject canvasGo = new GameObject("08_UI_PlanetOfLana_Title");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            GameObject menuContainer = new GameObject("Menu_Buttons_Cluster");
            menuContainer.transform.SetParent(canvasGo.transform, false);
            RectTransform menuRect = menuContainer.AddComponent<RectTransform>();
            menuRect.anchorMin = new Vector2(0.12f, 0.40f);
            menuRect.anchorMax = new Vector2(0.12f, 0.40f);
            menuRect.pivot = new Vector2(0f, 0.5f);
            menuRect.anchoredPosition = Vector2.zero;

            VerticalLayoutGroup layout = menuContainer.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 22f;
            layout.childControlHeight = false;
            layout.childControlWidth = false;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;

            CreateMenuLabel(menuContainer.transform, "PLAY", false);
            CreateMenuLabel(menuContainer.transform, "SETTINGS", false);
            CreateMenuLabel(menuContainer.transform, "CREDITS", false);
            CreateMenuLabel(menuContainer.transform, "QUIT", true);
        }

        private static void CreateMenuLabel(Transform parent, string textStr, bool isSelectedBox)
        {
            GameObject itemGo = new GameObject($"Btn_{textStr}");
            itemGo.transform.SetParent(parent, false);
            RectTransform rt = itemGo.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(150, 40);

            if (isSelectedBox)
            {
                // Hộp viền trắng phong cách Planet of Lana
                Image bg = itemGo.AddComponent<Image>();
                bg.color = Color.white;

                GameObject textGo = new GameObject("Text");
                textGo.transform.SetParent(itemGo.transform, false);
                RectTransform textRt = textGo.AddComponent<RectTransform>();
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.offsetMin = Vector2.zero;
                textRt.offsetMax = Vector2.zero;

                TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
                tmp.text = textStr;
                tmp.fontSize = 24;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = new Color(0.12f, 0.25f, 0.35f, 1.0f); // Chữ sẫm màu trên nền hộp trắng
            }
            else
            {
                TextMeshProUGUI tmp = itemGo.AddComponent<TextMeshProUGUI>();
                tmp.text = textStr;
                tmp.fontSize = 24;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.Left;
                tmp.color = Color.white;
            }
        }
    }
}
#endif
