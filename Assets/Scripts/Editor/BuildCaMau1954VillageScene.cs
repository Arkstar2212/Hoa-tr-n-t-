using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using HoaVaMau.Test3D;

namespace HoaVaMau.Editor
{
    /// <summary>
    /// Builder Script dựng Scene 3D hoàn chỉnh duy nhất: CaMau_1954_Village.unity
    /// Tái hiện chuẩn xác 100% bố cục Bản đồ Làng Ven Sông Cà Mau 1954 (Chương I - GÓI ĐẤT)
    /// Góc nhìn 3D Tự Do / Isometric Cinematic Adventure theo phong cách Planet of Lana & Mekong Delta
    /// </summary>
    public static class BuildCaMau1954VillageScene
    {
        public const string ScenePath = "Assets/Scenes/CaMau_1954_Village.unity";
        private const string MaterialsPath = "Assets/Materials";
        private const string PrefabsPath = "Assets/Prefabs";

        [MenuItem("Máu và Hoa/Build 3D Ca Mau 1954 Village Scene", priority = 1)]
        public static void BuildScene()
        {
            Debug.Log("<color=#38bdf8>[BuildCaMau1954VillageScene]</color> Bắt đầu dựng Scene 3D: CaMau_1954_Village...");

            // 1. Sao lưu an toàn scene hiện tại nếu có
            if (File.Exists(ScenePath))
            {
                string backupDir = "Assets/Scenes/Backups";
                if (!Directory.Exists(backupDir)) Directory.CreateDirectory(backupDir);
                string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupPath = $"{backupDir}/CaMau_1954_Village_Backup_{timeStamp}.unity";
                File.Copy(ScenePath, backupPath, true);
                AssetDatabase.Refresh();
                Debug.Log($"<color=#f59e0b>[Backup]</color> Đã tạo bản sao lưu scene gốc tại: {backupPath}");
            }

            // 2. Tạo scene mới tinh
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 3. Chuẩn bị Materials chất lượng cao của dự án (Textured PBR & Stylized Shaders)
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null) urpLit = Shader.Find("Standard");

            // Cỏ phong cảnh có Texture hoa văn thảm cỏ sắc nét
            Material grassMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/M_CaMau_Landscape_Grass.mat");
            if (grassMat == null)
            {
                grassMat = new Material(urpLit);
                grassMat.name = "M_CaMau_Landscape_Grass";
                AssetDatabase.CreateAsset(grassMat, $"{MaterialsPath}/M_CaMau_Landscape_Grass.mat");
            }
            Texture2D grassTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Grass/meadow_turf.png");
            if (grassTex != null)
            {
                grassMat.SetTexture("_BaseMap", grassTex);
                grassMat.SetTextureScale("_BaseMap", new Vector2(1f, 1f));
            }
            grassMat.SetColor("_BaseColor", new Color(0.48f, 0.65f, 0.32f));
            grassMat.SetFloat("_Smoothness", 0.05f);
            EditorUtility.SetDirty(grassMat);

            // Đường đất nện ấm áp có họa tiết đất sỏi
            Material dirtMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/M_Earthen_Trail.mat");
            if (dirtMat == null)
            {
                dirtMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/M_CaMau_Landscape_Dirt.mat");
                if (dirtMat == null)
                {
                    dirtMat = new Material(urpLit);
                    dirtMat.name = "M_CaMau_Landscape_Dirt";
                    Texture2D dirtTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Grass/meadow_turf.png");
                    if (dirtTex != null)
                    {
                        dirtMat.SetTexture("_BaseMap", dirtTex);
                        dirtMat.SetTextureScale("_BaseMap", new Vector2(8f, 2f));
                    }
                    dirtMat.SetColor("_BaseColor", new Color(0.68f, 0.48f, 0.30f));
                    dirtMat.SetFloat("_Smoothness", 0.08f);
                    AssetDatabase.CreateAsset(dirtMat, $"{MaterialsPath}/M_CaMau_Landscape_Dirt.mat");
                }
            }

            // Mặt nước 3D sóng sánh ánh hoàng hôn (Uber Stylized Water Shader)
            Material waterMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Shaders/Uber Stylized Water/Template Materials/UWa-Template-Tropical.mat");
            if (waterMat == null) waterMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Shaders/Uber Stylized Water/Template Materials/UWa-Template-Genshin.mat");
            if (waterMat == null) waterMat = GetOrCreateMaterial("M_StylizedWater_Sunset", new Color(0.14f, 0.38f, 0.42f, 0.90f), 0.95f);

            // Gỗ mộc thời tiết
            Material timberLightMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/M_Wood_Weathered.mat");
            if (timberLightMat == null) timberLightMat = GetOrCreateMaterial("M_CaMau_TimberLight", new Color(0.55f, 0.42f, 0.28f), 0.10f);

            Material timberDarkMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/M_CaMau_TimberDark.mat");
            if (timberDarkMat == null) timberDarkMat = GetOrCreateMaterial("M_CaMau_TimberDark", new Color(0.28f, 0.20f, 0.13f), 0.12f);

            Material wetPilingMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/M_CaMau_WetPiling.mat");
            if (wetPilingMat == null) wetPilingMat = GetOrCreateMaterial("M_CaMau_WetPiling", new Color(0.18f, 0.14f, 0.10f), 0.30f);

            Material bambooMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/M_CaMau_Bamboo.mat");
            if (bambooMat == null) bambooMat = GetOrCreateMaterial("M_CaMau_Bamboo", new Color(0.68f, 0.62f, 0.35f), 0.20f);

            // Rơm vàng có texture sợi rơm óng ả
            Material strawMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/M_CaMau_Textured_Straw.mat");
            if (strawMat == null)
            {
                strawMat = new Material(urpLit);
                strawMat.name = "M_CaMau_Textured_Straw";
                Texture2D hayTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Environment/vietnamese_straw_haystack_clean.png");
                if (hayTex != null)
                {
                    strawMat.SetTexture("_BaseMap", hayTex);
                    strawMat.SetTextureScale("_BaseMap", new Vector2(2f, 2f));
                }
                strawMat.SetColor("_BaseColor", new Color(0.95f, 0.80f, 0.35f));
                strawMat.SetFloat("_Smoothness", 0.08f);
                AssetDatabase.CreateAsset(strawMat, $"{MaterialsPath}/M_CaMau_Textured_Straw.mat");
            }

            Material thatchMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/M_CaMau_ThatchRoof.mat");
            if (thatchMat == null) thatchMat = GetOrCreateMaterial("M_CaMau_ThatchRoof", new Color(0.58f, 0.48f, 0.32f), 0.05f);

            Material pavedStoneMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/M_CaMau_PavedStone.mat");
            if (pavedStoneMat == null) pavedStoneMat = GetOrCreateMaterial("M_CaMau_PavedStone", new Color(0.48f, 0.50f, 0.48f), 0.15f);

            Material clayUrnMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/M_CaMau_ClayUrn.mat");
            if (clayUrnMat == null) clayUrnMat = GetOrCreateMaterial("M_CaMau_ClayUrn", new Color(0.46f, 0.34f, 0.26f), 0.18f);

            Material riverMudMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/M_CaMau_RiverMud.mat");
            if (riverMudMat == null) riverMudMat = GetOrCreateMaterial("M_CaMau_RiverMud", new Color(0.26f, 0.22f, 0.18f), 0.05f);

            Material clothBrownMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/M_CaMau_ClothBrown.mat");
            if (clothBrownMat == null) clothBrownMat = GetOrCreateMaterial("M_CaMau_ClothBrown", new Color(0.45f, 0.35f, 0.25f), 0.05f);

            Material khanRanMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/M_CaMau_KhanRan.mat");
            if (khanRanMat == null) khanRanMat = GetOrCreateMaterial("M_CaMau_KhanRan", new Color(0.85f, 0.25f, 0.20f), 0.05f);

            Material anSilhouetteMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/Characters/M_Character_Silhouette.mat");
            if (anSilhouetteMat == null) anSilhouetteMat = GetOrCreateMaterial("M_Character_Silhouette", new Color(0.12f, 0.10f, 0.14f), 0.05f);

            Material skyMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/Environment/Backdrop_Sky/M_StylizedSky_Sunset.mat");
            Material sunMat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/Environment/Backdrop_Sky/M_StylizedSun_Sunset.mat");

            // 4. Khởi tạo Cấu trúc Hierarchy Chuẩn
            GameObject root = new GameObject("CaMau_1954_Village");
            GameObject envGroup = CreateChild(root, "Environment");
            GameObject terrainGroup = CreateChild(envGroup, "Terrain_3D");
            GameObject pathsGroup = CreateChild(envGroup, "Paths_3D_Network");
            GameObject waterGroup = CreateChild(envGroup, "WaterSystem_3D");
            GameObject vegGroup = CreateChild(envGroup, "Vegetation_3D");
            GameObject bgGroup = CreateChild(envGroup, "Background_Atmosphere");

            GameObject villageAreasGroup = CreateChild(root, "Village_Areas");
            GameObject area01 = CreateChild(villageAreasGroup, "01_Nha_An");
            GameObject area02 = CreateChild(villageAreasGroup, "02_Khu_Lay_Nuoc");
            GameObject area03 = CreateChild(villageAreasGroup, "03_Duong_Lang");
            GameObject area04 = CreateChild(villageAreasGroup, "04_San_Lang");
            GameObject area05 = CreateChild(villageAreasGroup, "05_Vuon_Cay");
            GameObject area06 = CreateChild(villageAreasGroup, "06_Canh_Dong");
            GameObject area07 = CreateChild(villageAreasGroup, "07_Bo_Song");
            GameObject area08 = CreateChild(villageAreasGroup, "08_Ben_Xuong");

            GameObject propsGroup = CreateChild(root, "Props");
            GameObject npcsGroup = CreateChild(root, "NPCs");
            GameObject playerGroup = CreateChild(root, "Player");
            GameObject triggersGroup = CreateChild(root, "Gameplay_Triggers");
            GameObject lightingGroup = CreateChild(root, "Lighting");
            GameObject cameraGroup = CreateChild(root, "Camera_Rig");
            GameObject managersGroup = CreateChild(root, "Managers");
            GameObject audioGroup = CreateChild(root, "Audio");

            // ==========================================
            // 5. LIGHTING & POST PROCESSING (HOÀNG HÔN ẤM ÁP)
            // ==========================================
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.96f, 0.82f, 0.65f);
            RenderSettings.ambientEquatorColor = new Color(0.72f, 0.62f, 0.45f);
            RenderSettings.ambientGroundColor = new Color(0.35f, 0.32f, 0.25f);
            Material skyboxMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Shaders/Uber Stylized Water/Demo/Sky/SKy 22.mat");
            if (skyboxMat != null)
            {
                RenderSettings.skybox = skyboxMat;
            }
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0035f;
            RenderSettings.fogColor = new Color(0.92f, 0.78f, 0.62f);

            // Sun Directional Light
            GameObject sunLightObj = new GameObject("Directional_SunLight_Golden");
            sunLightObj.transform.SetParent(lightingGroup.transform, false);
            sunLightObj.transform.rotation = Quaternion.Euler(34f, 32f, 0f);
            Light sun = sunLightObj.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1.0f, 0.88f, 0.72f);
            sun.intensity = 1.35f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.65f;

            // Fill Light
            GameObject fillLightObj = new GameObject("FillLight_Sky");
            fillLightObj.transform.SetParent(lightingGroup.transform, false);
            fillLightObj.transform.rotation = Quaternion.Euler(60f, -145f, 0f);
            Light fill = fillLightObj.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.65f, 0.78f, 0.90f);
            fill.intensity = 0.40f;
            fill.shadows = LightShadows.None;

            // Post-processing Volume
            GameObject volumeObj = new GameObject("PostProcessing_Sunset");
            volumeObj.transform.SetParent(lightingGroup.transform, false);
            var vol = volumeObj.AddComponent<Volume>();
            vol.isGlobal = true;
            VolumeProfile vp = ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom = vp.Add<Bloom>(true);
            bloom.intensity.value = 0.28f;
            bloom.threshold.value = 0.90f;
            var tonemap = vp.Add<Tonemapping>(true);
            tonemap.mode.value = TonemappingMode.Neutral;
            var colorGrad = vp.Add<ColorAdjustments>(true);
            colorGrad.postExposure.value = 0.15f;
            colorGrad.colorFilter.value = new Color(1.0f, 0.96f, 0.90f);
            colorGrad.saturation.value = 8.0f;
            vol.sharedProfile = vp;

            // Sky Backdrop Dome
            if (skyMat != null)
            {
                GameObject skyBackdrop = GameObject.CreatePrimitive(PrimitiveType.Quad);
                skyBackdrop.name = "Backdrop_SkySunset";
                skyBackdrop.transform.SetParent(bgGroup.transform, false);
                skyBackdrop.transform.position = new Vector3(0f, 18f, 74f);
                skyBackdrop.transform.rotation = Quaternion.Euler(0, 180f, 0);
                skyBackdrop.transform.localScale = new Vector3(180f, 55f, 1f);
                skyBackdrop.GetComponent<Renderer>().sharedMaterial = skyMat;
                UnityEngine.Object.DestroyImmediate(skyBackdrop.GetComponent<Collider>());
            }

            // Distant Mangroves on Horizon (Bọc trọn 3 phía Bắc, Tây, Đông)
            var bgMangrovePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Vegetation/PF_BG_Mangrove_Forest.prefab");
            if (bgMangrovePrefab != null)
            {
                // Rừng ngập mặn phía Bắc chân trời
                for (int i = 0; i < 9; i++)
                {
                    float x = -42f + i * 11f;
                    var b = (GameObject)PrefabUtility.InstantiatePrefab(bgMangrovePrefab, bgGroup.transform);
                    b.transform.position = new Vector3(x, 0.45f, 32f + (i % 2) * 2f);
                    b.transform.localScale = Vector3.one * 1.35f;
                }

                // Rừng ngập mặn viền phía Tây (sau vườn cây)
                for (int j = 0; j < 5; j++)
                {
                    float z = -8f + j * 9f;
                    var b = (GameObject)PrefabUtility.InstantiatePrefab(bgMangrovePrefab, bgGroup.transform);
                    b.transform.position = new Vector3(-36f, -0.65f, z);
                    b.transform.rotation = Quaternion.Euler(0, 90f, 0);
                    b.transform.localScale = Vector3.one * 1.30f;
                }

                // Rừng ngập mặn viền phía Đông (sau cánh đồng)
                for (int k = 0; k < 5; k++)
                {
                    float z = -2f + k * 9f;
                    var b = (GameObject)PrefabUtility.InstantiatePrefab(bgMangrovePrefab, bgGroup.transform);
                    b.transform.position = new Vector3(45f, -0.65f, z);
                    b.transform.rotation = Quaternion.Euler(0, -90f, 0);
                    b.transform.localScale = Vector3.one * 1.30f;
                }
            }

            // Distant Sailing Ship
            var shipPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Boats/PF_TransportShip_1954.prefab");
            if (shipPrefab != null)
            {
                var s1 = (GameObject)PrefabUtility.InstantiatePrefab(shipPrefab, bgGroup.transform);
                s1.transform.position = new Vector3(-18f, -0.65f, -32f);
                s1.transform.rotation = Quaternion.Euler(0, 110f, 0);
                s1.transform.localScale = Vector3.one * 0.85f;

                var s2 = (GameObject)PrefabUtility.InstantiatePrefab(shipPrefab, bgGroup.transform);
                s2.transform.position = new Vector3(28f, -0.65f, -28f);
                s2.transform.rotation = Quaternion.Euler(0, 260f, 0);
                s2.transform.localScale = Vector3.one * 0.70f;
            }

            // ==========================================
            // 6. THIẾT KẾ ĐỊA HÌNH 3D (3D TERRAIN & ISLAND LAYOUT)
            // ==========================================
            // Làng Cà Mau là bán đảo xanh bao quanh bởi Sông Nam và Kênh Đông
            // NỀN ĐỊA HÌNH ĐẠI LỤC LIỀN MẠCH (Seamless Continent) trải rộng 150m x 150m
            CreateGroundBlock(terrainGroup.transform, "Terrain_Base_Expansive", new Vector3(0f, -2.2f, 0f), new Vector3(150.0f, 2.2f, 150.0f), grassMat);

            // Sân Làng (Trung tâm): Y = 0.0
            CreateGroundBlock(terrainGroup.transform, "Terrain_SanLang_Main", new Vector3(0f, -1.0f, 2.0f), new Vector3(26.0f, 2.0f, 20.0f), grassMat);

            // Gò Nhà An (Phía Bắc, hơi nâng cao nhẹ): Y = 0.40m
            CreateGroundBlock(terrainGroup.transform, "Terrain_NhaAn_Knoll", new Vector3(1.0f, -0.75f, 17.5f), new Vector3(22.0f, 2.3f, 16.0f), grassMat);
            // Dốc thoai thoải nối Sân Làng lên Nhà An (Smooth Ramp)
            CreateSlopedRamp(terrainGroup.transform, "Ramp_SanLang_To_NhaAn", new Vector3(0.5f, 0.18f, 9.5f), new Vector3(18.0f, 0.45f, 4.2f), -5.5f, grassMat);

            // Vườn Cây Ăn Trái (Phía Tây): Y = 0.20m
            CreateGroundBlock(terrainGroup.transform, "Terrain_VuonCay_Knoll", new Vector3(-18.0f, -0.85f, 7.5f), new Vector3(20.0f, 2.1f, 24.0f), grassMat);
            // Dốc thoai thoải nối Sân Làng sang Vườn Cây
            CreateSlopedRamp(terrainGroup.transform, "Ramp_SanLang_To_VuonCay", new Vector3(-8.2f, 0.10f, 5.0f), new Vector3(4.2f, 0.30f, 18.0f), 0f, grassMat, 3.2f);

            // Gò Khu Lấy Nước (Phía Đông Sân Làng): Y = 0.10m
            CreateGroundBlock(terrainGroup.transform, "Terrain_KhuLayNuoc_Knoll", new Vector3(12.5f, -0.90f, 6.0f), new Vector3(12.0f, 2.0f, 16.0f), grassMat);

            // Cánh Đồng Lúa (Phía Đông & Đông Bắc bên kia Kênh): Y = -0.05m
            CreateGroundBlock(terrainGroup.transform, "Terrain_CanhDong_Paddies", new Vector3(28.0f, -1.05f, 14.0f), new Vector3(26.0f, 2.0f, 34.0f), grassMat);

            // Bờ Đê & Đường Bờ Ruộng Walkable (Dikes): Y = 0.15m
            CreateWalkableDike(terrainGroup.transform, "Dike_Main_EastWest", new Vector3(28.0f, 0.08f, 10.0f), new Vector3(24.0f, 0.25f, 2.0f), dirtMat);
            CreateWalkableDike(terrainGroup.transform, "Dike_Cross_NorthSouth", new Vector3(26.0f, 0.08f, 18.0f), new Vector3(2.0f, 0.25f, 20.0f), dirtMat);

            // Bờ Kênh Thoai Thoải & Lòng Kênh
            CreateGroundBlock(terrainGroup.transform, "CanalBed_Mud", new Vector3(17.5f, -1.8f, 8.0f), new Vector3(8.0f, 1.5f, 44.0f), riverMudMat);
            CreateGroundBlock(terrainGroup.transform, "RiverBed_South_Mud", new Vector3(0f, -2.0f, -24.0f), new Vector3(140.0f, 1.8f, 32.0f), riverMudMat);

            // Tường biên vô hình ngăn nhân vật rơi ra ngoài bản đồ
            CreateWorldBarrier(terrainGroup.transform, "Barrier_North", new Vector3(0f, 3f, 32f), new Vector3(100f, 8f, 2f));
            CreateWorldBarrier(terrainGroup.transform, "Barrier_South", new Vector3(0f, 3f, -28f), new Vector3(100f, 8f, 2f));
            CreateWorldBarrier(terrainGroup.transform, "Barrier_West", new Vector3(-32f, 3f, 0f), new Vector3(2f, 8f, 70f));
            CreateWorldBarrier(terrainGroup.transform, "Barrier_East", new Vector3(44f, 3f, 0f), new Vector3(2f, 8f, 70f));

            // ==========================================
            // 7. MẠNG LƯỚI ĐƯỜNG ĐẤT 3D (WINDING DIRT PATHS)
            // ==========================================
            // Sân làng chính: Khoảng đất nện rộng rãi màu ấm
            CreateDirtCourtyard(pathsGroup.transform, "Path_SanLang_Courtyard", new Vector3(0f, 0.008f, 1.0f), new Vector3(13.5f, 0.015f, 10.5f), dirtMat);

            // Tuyến 1: Sân Làng lên Nhà An (Trục Bắc)
            CreateDirtPathSegment(pathsGroup.transform, "Path_SanLang_To_NhaAn", new Vector3(0f, 0.22f, 8.5f), new Vector3(2.6f, 0.02f, 7.5f), dirtMat);
            // Thềm đất trước nhà An mở rộng đón lối lên
            CreateDirtCourtyard(pathsGroup.transform, "Path_NhaAn_Apron", new Vector3(0.5f, 0.42f, 13.8f), new Vector3(5.5f, 0.015f, 3.8f), dirtMat);

            // Tuyến 2: Sân Làng sang Vườn Cây (Trục Tây)
            CreateDirtPathSegment(pathsGroup.transform, "Path_SanLang_To_VuonCay", new Vector3(-9.5f, 0.10f, 4.0f), new Vector3(8.5f, 0.02f, 2.4f), dirtMat);
            // Thềm đất trước giàn bầu vườn cây
            CreateDirtCourtyard(pathsGroup.transform, "Path_VuonCay_Apron", new Vector3(-15.5f, 0.20f, 3.5f), new Vector3(5.0f, 0.015f, 4.0f), dirtMat);

            // Tuyến 3: Sân Làng xuống Bờ Sông & Bến Xuồng (Trục Nam)
            CreateDirtPathSegment(pathsGroup.transform, "Path_SanLang_To_BoSong", new Vector3(0.5f, 0.008f, -5.5f), new Vector3(2.8f, 0.02f, 6.5f), dirtMat);

            // Tuyến 4: Sân Làng sang Khu Lấy Nước (Trục Đông)
            CreateDirtPathSegment(pathsGroup.transform, "Path_SanLang_To_KhuLayNuoc", new Vector3(7.5f, 0.05f, 3.5f), new Vector3(5.5f, 0.02f, 2.4f), dirtMat);

            // Tuyến 5: Khu Lấy Nước qua Cầu Gỗ vào Cánh Đồng
            CreateDirtPathSegment(pathsGroup.transform, "Path_KhuLayNuoc_To_Bridge", new Vector3(13.2f, 0.08f, -0.5f), new Vector3(2.4f, 0.02f, 5.0f), dirtMat);

            // Tuyến 6: Nhà An rẽ lối đi tắt sang Khu Lấy Nước
            CreateDirtPathSegment(pathsGroup.transform, "Path_NhaAn_To_KhuLayNuoc", new Vector3(7.0f, 0.26f, 12.0f), new Vector3(5.5f, 0.02f, 2.0f), dirtMat);

            // Tuyến 7: Vườn Cây có lối mòn xuống Bờ Sông (Lối tắt ven rạch)
            CreateDirtPathSegment(pathsGroup.transform, "Path_VuonCay_To_BoSong", new Vector3(-17.0f, 0.10f, -3.0f), new Vector3(2.2f, 0.02f, 9.0f), dirtMat);

            // Các khúc nối đường bo góc mềm mại (Curved Junction Arcs)
            CreateDirtPathSegment(pathsGroup.transform, "Path_Corner_NW", new Vector3(-5.2f, 0.12f, 5.8f), new Vector3(4.0f, 0.02f, 2.8f), dirtMat, 40f);
            CreateDirtPathSegment(pathsGroup.transform, "Path_Corner_NE", new Vector3(5.0f, 0.15f, 6.2f), new Vector3(4.0f, 0.02f, 2.8f), dirtMat, -35f);
            CreateDirtPathSegment(pathsGroup.transform, "Path_Corner_SW", new Vector3(-5.5f, 0.008f, -3.2f), new Vector3(4.2f, 0.02f, 2.6f), dirtMat, -40f);
            CreateDirtPathSegment(pathsGroup.transform, "Path_Corner_SE", new Vector3(4.8f, 0.008f, -3.0f), new Vector3(4.0f, 0.02f, 2.6f), dirtMat, 35f);

            // ==========================================
            // 8. HỆ THỐNG MẶT NƯỚC 3D (RIVER & CANALS)
            // ==========================================
            // Sông Cà Mau lớn phía Nam (Mặt nước mênh mông, tràn ngập dưới sàn bến tàu và bờ sông)
            GameObject riverSouth = CreateWaterSurface("Water_South_River", new Vector3(0f, -0.65f, -20.5f), new Vector3(90f, 1f, 26f), waterMat);
            riverSouth.transform.SetParent(waterGroup.transform, false);

            // Kênh Rạch phía Đông chảy dọc ranh giới làng và cánh đồng (X = 14.5 đến 20.5)
            GameObject canalEast = CreateWaterSurface("Water_East_Canal", new Vector3(17.5f, -0.65f, 9.0f), new Vector3(6.5f, 1f, 40f), waterMat);
            canalEast.transform.SetParent(waterGroup.transform, false);

            // Rạch nước nhánh nhỏ phía Tây vườn cây
            GameObject creekWest = CreateWaterSurface("Water_West_Creek", new Vector3(-27.0f, -0.65f, -5.0f), new Vector3(12f, 1f, 24f), waterMat);
            creekWest.transform.SetParent(waterGroup.transform, false);

            // Rễ cây đước/bần ngập mặn ven mép nước tạo độ bám tự nhiên
            var rootPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Environment/ModularKit/PF_Mod_MangroveRoot_Cluster.prefab");
            if (rootPrefab != null)
            {
                PlacePrefab(rootPrefab, waterGroup.transform, new Vector3(15.5f, -0.65f, -4.0f), Vector3.one * 0.9f, 20f);
                PlacePrefab(rootPrefab, waterGroup.transform, new Vector3(19.5f, -0.65f, 5.0f), Vector3.one * 1.0f, -60f);
                PlacePrefab(rootPrefab, waterGroup.transform, new Vector3(15.5f, -0.65f, 16.0f), Vector3.one * 0.95f, 45f);
                PlacePrefab(rootPrefab, waterGroup.transform, new Vector3(-19.0f, -0.65f, -9.8f), Vector3.one * 1.1f, -15f);
            }

            // Cụm bèo hoa súng nở ven mép nước
            var waterLilyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Boats/PF_WaterLily_Cluster.prefab");
            if (waterLilyPrefab != null)
            {
                PlacePrefab(waterLilyPrefab, waterGroup.transform, new Vector3(-14f, -0.62f, -11.5f), Vector3.one * 0.9f);
                PlacePrefab(waterLilyPrefab, waterGroup.transform, new Vector3(-4f, -0.62f, -12.0f), Vector3.one * 1.1f);
                PlacePrefab(waterLilyPrefab, waterGroup.transform, new Vector3(17.5f, -0.62f, 4.0f), Vector3.one * 0.85f);
                PlacePrefab(waterLilyPrefab, waterGroup.transform, new Vector3(17.5f, -0.62f, 14.0f), Vector3.one * 0.95f);
                PlacePrefab(waterLilyPrefab, waterGroup.transform, new Vector3(12.0f, -0.62f, -14.0f), Vector3.one * 1.0f);
            }

            // ==========================================
            // 9. KHU VỰC 1: NHÀ AN (01_Nha_An)
            // ==========================================
            // Nhà sàn mái lá truyền thống Nam Bộ 3D chuẩn xác
            var thatchedHouse3DPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Environment/PF_StiltedHouse_Thatched.prefab");
            var woodHouse3DPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Environment/PF_StiltedHouse_Wood.prefab");

            if (thatchedHouse3DPrefab != null)
            {
                var hut = PlaceStiltedHouse3D(thatchedHouse3DPrefab, area01.transform, new Vector3(0.5f, 0.45f, 20.2f), Vector3.one * 4.2f, 180f, false);
                hut.name = "House_An_StiltedHouse_3D";
            }

            // Cầu thang gỗ và hiên nhà sàn nối xuống mặt đất
            CreateWoodenSteps3D(area01.transform, new Vector3(0.5f, 0.42f, 16.5f), timberLightMat, 4);

            // Dây phơi đồ áo bà ba & khăn rằn bên hông nhà
            Create3DClothesline(area01.transform, new Vector3(4.8f, 0.45f, 16.5f), new Vector3(4.8f, 0.45f, 20.0f), timberLightMat, clothBrownMat, khanRanMat);

            // Lu nước mưa sành & chổi dừa trước bậc thềm
            CreateCeramicWaterUrn(area01.transform, "LuNuoc_NhaAn_01", new Vector3(-1.8f, 0.45f, 16.0f), 0.50f, clayUrnMat, pavedStoneMat);
            CreateCeramicWaterUrn(area01.transform, "LuNuoc_NhaAn_02", new Vector3(-2.6f, 0.45f, 16.2f), 0.42f, clayUrnMat, pavedStoneMat);
            CreateCoconutBroom3D(area01.transform, new Vector3(-1.2f, 0.45f, 16.2f), bambooMat);

            // Chuồng gà & hàng rào quây góc sân
            CreateChickenPen3D(area01.transform, new Vector3(7.2f, 0.45f, 18.0f), bambooMat, thatchMat);

            // Chậu kiểng dân dã bên thềm nhà
            CreatePottedPlant3D(area01.transform, new Vector3(-0.6f, 0.95f, 16.5f), clayUrnMat, grassMat);
            CreatePottedPlant3D(area01.transform, new Vector3(2.4f, 0.95f, 16.5f), clayUrnMat, grassMat);

            // NPC: Mẹ An đứng đón con trước hiên nhà
            var motherPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Characters/NPCs/PF_NPC_AnMother.prefab");
            if (motherPrefab != null)
            {
                var mother = (GameObject)PrefabUtility.InstantiatePrefab(motherPrefab, area01.transform);
                mother.name = "NPC_Mother_An";
                mother.transform.position = new Vector3(0.5f, 0.45f, 15.2f);
                mother.transform.rotation = Quaternion.Euler(0, 180f, 0);
                mother.transform.localScale = Vector3.one * 1.40f;
            }

            // Trigger Tương tác: Nói chuyện với Mẹ
            Create3DInteractionTrigger(triggersGroup.transform, "INT_TalkToMother", new Vector3(0.5f, 0.45f, 14.5f), "Nói chuyện với Mẹ (Bắt đầu ngày mới)", 2.4f);

            // ==========================================
            // 10. KHU VỰC 2: KHU LẤY NƯỚC (02_Khu_Lay_Nuoc)
            // ==========================================
            // Giếng nước đá tròn & Chòi che mưa mái lá 3D
            Create3DWellStation(area02.transform, new Vector3(12.0f, 0.10f, 6.0f), pavedStoneMat, timberDarkMat, thatchMat, clayUrnMat, waterMat);

            // Rào tre bao quanh khu giếng nước với cổng mở ra đường làng
            CreateBambooFencePerimeter(area02.transform, new Vector3(12.0f, 0.10f, 6.0f), 3.8f, 3.8f, bambooMat, false, true, false, false);

            // NPC: Cô Ba gánh nước
            var femaleVnPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Characters/NPCs/PF_NPC_Female_VN.prefab");
            if (femaleVnPrefab != null)
            {
                var female = (GameObject)PrefabUtility.InstantiatePrefab(femaleVnPrefab, area02.transform);
                female.name = "NPC_Female_WaterCarrier";
                female.transform.position = new Vector3(10.5f, 0.10f, 7.2f);
                female.transform.rotation = Quaternion.Euler(0, 140f, 0);
                female.transform.localScale = Vector3.one * 1.40f;
            }

            // Trigger Tương tác: Lấy nước giúp mẹ
            Create3DInteractionTrigger(triggersGroup.transform, "INT_FetchWater", new Vector3(12.0f, 0.10f, 5.0f), "Múc gáo nước ngọt mát lành giúp Mẹ", 2.4f);

            // ==========================================
            // 11. KHU VỰC 3: ĐƯỜNG LÀNG & CẦU GỖ VÒM (03_Duong_Lang)
            // ==========================================
            // Cầu gỗ vòm mộc mạc bắc qua Kênh Đông nối sang Cánh Đồng
            Create3DArchedBridge(area03.transform, new Vector3(14.5f, 0.08f, -2.0f), new Vector3(20.5f, 0.08f, -2.0f), 2.4f, 0.90f, timberLightMat, wetPilingMat);

            // Xuồng cui neo dưới chân cầu
            var sampanOpenPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Boats/PF_Sampan_Open_Hat_Floating.prefab");
            if (sampanOpenPrefab != null)
            {
                var boatCreek = (GameObject)PrefabUtility.InstantiatePrefab(sampanOpenPrefab, area03.transform);
                boatCreek.name = "Sampan_Under_Bridge";
                boatCreek.transform.position = new Vector3(17.5f, -0.65f, 1.2f);
                boatCreek.transform.rotation = Quaternion.Euler(0, 15f, 0);
                boatCreek.transform.localScale = Vector3.one * 0.90f;
                boatCreek.AddComponent<FloatingBoat3D>();
            }

            // Hàng rào tre chạy dọc hai bên đường làng
            CreateBambooFenceSegment(area03.transform, new Vector3(-8.0f, 0.08f, 5.5f), new Vector3(-2.0f, 0.08f, 5.5f), bambooMat);
            CreateBambooFenceSegment(area03.transform, new Vector3(2.5f, 0.18f, 7.5f), new Vector3(8.5f, 0.18f, 7.5f), bambooMat);
            CreateBambooFenceSegment(area03.transform, new Vector3(-2.5f, 0.0f, -4.0f), new Vector3(-2.5f, 0.0f, -8.0f), bambooMat);
            CreateBambooFenceSegment(area03.transform, new Vector3(3.5f, 0.0f, -4.0f), new Vector3(3.5f, 0.0f, -8.0f), bambooMat);

            // ==========================================
            // 12. KHU VỰC 4: SÂN LÀNG (04_San_Lang)
            // ==========================================
            // Cây đại thụ trung tâm tỏa bóng mát cả sân
            var heroTreePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Environment/Trees/PF_Lana_Artistic_Hero_Tree.prefab");
            if (heroTreePrefab == null) heroTreePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Environment/Trees/PF_Curved_Hero_Tree_Knoll.prefab");
            if (heroTreePrefab != null)
            {
                var tree = (GameObject)PrefabUtility.InstantiatePrefab(heroTreePrefab, area04.transform);
                tree.name = "HeroTree_SanLang";
                tree.transform.position = new Vector3(-4.0f, 0.0f, 3.5f);
                tree.transform.rotation = Quaternion.Euler(0, 35f, 0);
                tree.transform.localScale = Vector3.one * 1.55f;
            }

            // Ghế gỗ dài dưới bóng cây mát & Cột cờ Tổ quốc
            Create3DWoodenBench(area04.transform, new Vector3(-4.0f, 0.0f, 1.8f), 2.2f, timberLightMat);
            Create3DFlagpole(area04.transform, new Vector3(-6.2f, 0.0f, 2.5f), bambooMat);

            // Đống Rơm Vàng 3D quy mô lớn & Xe ba gác mộc
            Create3DHaystack(area04.transform, new Vector3(6.5f, 0.0f, 2.5f), strawMat, timberDarkMat);
            Create3DFarmCart(area04.transform, new Vector3(8.5f, 0.0f, 1.2f), timberLightMat, strawMat);

            // Bao lúa thu hoạch tựa bên đống rơm
            var riceSacksPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Props/PF_Prop_RiceSacks.prefab");
            if (riceSacksPrefab != null)
            {
                PlacePrefab(riceSacksPrefab, area04.transform, new Vector3(5.2f, 0.0f, 1.2f), Vector3.one * 0.90f);
            }

            // Đàn gà con nhặt thóc giữa sân
            CreateChickenFlock3D(area04.transform, new Vector3(0.5f, 0.0f, 1.5f));

            // NPCs: Trẻ con chơi bắn bi & rượt bắt
            var boySingleBunPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Characters/PF_3D_Boy_SingleBun.prefab");
            if (boySingleBunPrefab != null)
            {
                var boy = (GameObject)PrefabUtility.InstantiatePrefab(boySingleBunPrefab, area04.transform);
                boy.name = "NPC_Child_Playing_Boy";
                boy.transform.position = new Vector3(1.5f, 0.0f, 0.5f);
                boy.transform.rotation = Quaternion.Euler(0, 30f, 0);
                boy.transform.localScale = Vector3.one * 0.85f;
            }

            var quietChildPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Characters/NPCs/PF_NPC_Quiet_Child.prefab");
            if (quietChildPrefab != null)
            {
                var girl = (GameObject)PrefabUtility.InstantiatePrefab(quietChildPrefab, area04.transform);
                girl.name = "NPC_Child_Playing_Girl";
                girl.transform.position = new Vector3(2.8f, 0.0f, 1.4f);
                girl.transform.rotation = Quaternion.Euler(0, -120f, 0);
                girl.transform.localScale = Vector3.one * 0.82f;
            }

            // Bác Hai Trưởng Xóm ngồi đàm đạo bên ghế gỗ
            var elderlyManPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Characters/NPCs/PF_NPC_Elderly_Man.prefab");
            if (elderlyManPrefab != null)
            {
                var elder = (GameObject)PrefabUtility.InstantiatePrefab(elderlyManPrefab, area04.transform);
                elder.name = "NPC_Elderly_VillageChief";
                elder.transform.position = new Vector3(-3.5f, 0.0f, 2.2f);
                elder.transform.rotation = Quaternion.Euler(0, 160f, 0);
                elder.transform.localScale = Vector3.one * 1.38f;
            }

            // Triggers Sân Làng
            Create3DInteractionTrigger(triggersGroup.transform, "INT_TalkToVillager_01", new Vector3(-3.5f, 0.0f, 1.8f), "Ngồi trò chuyện cùng Bác Hai Trưởng Xóm", 2.2f);
            Create3DInteractionTrigger(triggersGroup.transform, "INT_PlayWithKids", new Vector3(2.0f, 0.0f, 1.0f), "Chơi đuổi bắt và bắn bi cùng đám bạn nhỏ", 2.5f);

            // ==========================================
            // 13. KHU VỰC 5: VƯỜN CÂY ĂN TRÁI (05_Vuon_Cay)
            // ==========================================
            // Nhà vườn gỗ nhỏ nép dưới tán cây góc Tây Bắc (3D Wood Stilted House)
            if (woodHouse3DPrefab != null)
            {
                var orchardHut = PlaceStiltedHouse3D(woodHouse3DPrefab, area05.transform, new Vector3(-21.0f, 0.20f, 17.5f), Vector3.one * 3.6f, 125f, true);
                orchardHut.name = "House_Orchard_WoodCottage_3D";
            }

            // Giàn Bầu Bí 3D che mát lối đi với quả bầu trĩu giàn
            Create3DGourdTrellis(area05.transform, new Vector3(-18.0f, 0.20f, 2.0f), bambooMat, grassMat);

            // Vườn chuối trĩu quả & Cây ăn trái trĩu quả đỏ (chôm chôm / xoài)
            var bananaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Environment/Vegetation/PF_BananaPlant_Lush.prefab");
            if (bananaPrefab != null)
            {
                PlacePrefab(bananaPrefab, area05.transform, new Vector3(-14.0f, 0.20f, 8.5f), Vector3.one * 1.15f);
                PlacePrefab(bananaPrefab, area05.transform, new Vector3(-16.5f, 0.20f, 12.0f), Vector3.one * 1.30f);
                PlacePrefab(bananaPrefab, area05.transform, new Vector3(-24.0f, 0.20f, 7.0f), Vector3.one * 1.20f);
            }

            // Cây ăn trái đỏ rực (Orchard Fruit Trees)
            Create3DFruitTree(area05.transform, new Vector3(-19.5f, 0.20f, 11.5f), timberDarkMat, grassMat, new Color(0.88f, 0.22f, 0.15f));
            Create3DFruitTree(area05.transform, new Vector3(-23.0f, 0.20f, 13.0f), timberDarkMat, grassMat, new Color(0.95f, 0.40f, 0.10f));

            // Rổ sọt mây tre đựng trái cây vừa hái
            var basketPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Props/PF_Prop_Bamboo_Basket.prefab");
            if (basketPrefab != null)
            {
                PlacePrefab(basketPrefab, area05.transform, new Vector3(-17.0f, 0.20f, 3.2f), Vector3.one * 0.95f);
            }

            // NPC: Bác làm vườn
            var villager02Prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Characters/NPCs/PF_NPC_Villager_02.prefab");
            if (villager02Prefab != null)
            {
                var gardener = (GameObject)PrefabUtility.InstantiatePrefab(villager02Prefab, area05.transform);
                gardener.name = "NPC_Villager_Gardener";
                gardener.transform.position = new Vector3(-16.0f, 0.20f, 9.5f);
                gardener.transform.rotation = Quaternion.Euler(-90f, 110f, 0f);
                gardener.transform.localScale = Vector3.one * 1.38f;
            }

            // Trigger Tương tác: Hái quả vườn cây
            Create3DInteractionTrigger(triggersGroup.transform, "INT_PickFruit", new Vector3(-18.0f, 0.20f, 2.5f), "Hái trái bầu chín ngọt mát mang về cho Mẹ", 2.2f);

            // ==========================================
            // 14. KHU VỰC 6: CÁNH ĐỒNG LÚA (06_Canh_Dong)
            // ==========================================
            // Các luống lúa chín vàng óng ả nhiều tầng lớp
            var riceRowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Environment/Rice/PF_Rice_RowStrip_4m.prefab");
            if (riceRowPrefab != null)
            {
                for (int r = 0; r < 5; r++)
                {
                    float z = 2.0f + r * 5.0f;
                    for (int c = 0; c < 3; c++)
                    {
                        float x = 22.0f + c * 5.5f;
                        var rice = (GameObject)PrefabUtility.InstantiatePrefab(riceRowPrefab, area06.transform);
                        rice.transform.position = new Vector3(x, -0.05f, z);
                        rice.transform.rotation = Quaternion.Euler(0, 0f, 0);
                        rice.transform.localScale = new Vector3(1.35f, 1.25f, 1.20f);
                    }
                }
            }

            // Con Trâu Nước 3D đang gặm cỏ thanh bình giữa ruộng
            Create3DWaterBuffalo(area06.transform, new Vector3(27.0f, -0.05f, 18.5f), 135f, wetPilingMat, timberLightMat);

            // Bù Nhìn Rơm 3D đội nón lá trên bờ đê
            Create3DScarecrow(area06.transform, new Vector3(22.5f, 0.15f, 8.5f), timberLightMat, strawMat, bambooMat);

            // NPC: Chú Ba nông dân gặt lúa
            var villager04Prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Characters/NPCs/PF_NPC_Villager_04.prefab");
            if (villager04Prefab != null)
            {
                var farmer = (GameObject)PrefabUtility.InstantiatePrefab(villager04Prefab, area06.transform);
                farmer.name = "NPC_Farmer_RiceField";
                farmer.transform.position = new Vector3(24.5f, 0.15f, 11.5f);
                farmer.transform.rotation = Quaternion.Euler(0, -90f, 0);
                farmer.transform.localScale = Vector3.one * 1.38f;
            }

            // Trigger Tương tác: Mang cơm cho Chú Ba
            Create3DInteractionTrigger(triggersGroup.transform, "INT_DeliverMeal", new Vector3(23.5f, 0.15f, 10.5f), "Đưa cặp lồng cơm trưa cho Chú Ba nông dân", 2.4f);

            // ==========================================
            // 15. KHU VỰC 7: BỜ SÔNG (07_Bo_Song)
            // ==========================================
            // Cầu ván gỗ ven sông (River Boardwalk)
            Create3DBoardwalk(area07.transform, "Boardwalk_Riverfront", new Vector3(-8.0f, 0.0f, -10.5f), new Vector3(22.0f, 0.25f, 3.2f), timberLightMat, wetPilingMat, true);

            // Chòi nhà sàn ven nước góc Tây Nam (3D Thatched Stilted House)
            if (thatchedHouse3DPrefab != null)
            {
                var waterHut = PlaceStiltedHouse3D(thatchedHouse3DPrefab, area07.transform, new Vector3(-21.0f, -0.65f, -11.5f), Vector3.one * 3.4f, 75f, false);
                waterHut.name = "WaterHut_RiverEdge_3D";
            }

            // Giàn phơi lưới đánh cá & Bẫy cá lờ/dớn
            var fishingNetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Props/PF_Prop_Fishing_Net.prefab");
            if (fishingNetPrefab != null)
            {
                PlacePrefab(fishingNetPrefab, area07.transform, new Vector3(-12.0f, 0.10f, -9.5f), new Vector3(1.2f, 0.8f, 0.3f));
                PlacePrefab(fishingNetPrefab, area07.transform, new Vector3(-2.5f, 0.10f, -9.5f), new Vector3(1.2f, 0.8f, 0.3f));
            }

            // Ghế gỗ ngắm hoàng hôn bên bờ sông
            Create3DWoodenBench(area07.transform, new Vector3(-6.0f, 0.12f, -9.8f), 2.0f, timberLightMat);

            // Xuồng mộc có người chèo xuồng ven bờ
            if (sampanOpenPrefab != null)
            {
                var rowBoat = (GameObject)PrefabUtility.InstantiatePrefab(sampanOpenPrefab, area07.transform);
                rowBoat.name = "Sampan_River_Rower";
                rowBoat.transform.position = new Vector3(-12.0f, -0.65f, -15.5f);
                rowBoat.transform.rotation = Quaternion.Euler(0, 95f, 0);
                rowBoat.transform.localScale = Vector3.one * 1.05f;
                rowBoat.AddComponent<FloatingBoat3D>();
            }

            // Trigger Tương tác: Ngắm hoàng hôn bên bờ sông
            Create3DInteractionTrigger(triggersGroup.transform, "INT_SitByRiver", new Vector3(-6.0f, 0.12f, -9.8f), "Ngồi bờ sông ngắm hoàng hôn chiều tà Cà Mau", 2.2f);

            // ==========================================
            // 16. KHU VỰC 8: BẾN XUỒNG & CẦU TÀU (08_Ben_Xuong)
            // ==========================================
            // Cầu tàu gỗ vươn dài ra giữa lòng sông (Walkable Pier)
            Create3DBoardwalk(area08.transform, "WalkablePier_Wooden", new Vector3(6.0f, -0.10f, -15.5f), new Vector3(3.2f, 0.25f, 11.0f), timberLightMat, wetPilingMat, false);

            // Chòi bến nghỉ chân mái lá 3D ở đầu cầu tàu
            Create3DDockShelter(area08.transform, new Vector3(2.8f, 0.05f, -11.5f), timberLightMat, thatchMat, timberDarkMat);

            // 2 Chiếc Xuồng Ba Lá bập bềnh theo sóng nước
            var sampanCoveredPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Boats/PF_Sampan_Covered_Floating.prefab");
            if (sampanCoveredPrefab != null)
            {
                var coveredBoat = (GameObject)PrefabUtility.InstantiatePrefab(sampanCoveredPrefab, area08.transform);
                coveredBoat.name = "Sampan_Covered_Pier";
                coveredBoat.transform.position = new Vector3(10.5f, -0.65f, -16.5f);
                coveredBoat.transform.rotation = Quaternion.Euler(0, 120f, 0);
                coveredBoat.transform.localScale = Vector3.one * 1.10f;
                coveredBoat.AddComponent<FloatingBoat3D>();
            }

            if (sampanOpenPrefab != null)
            {
                var openBoat = (GameObject)PrefabUtility.InstantiatePrefab(sampanOpenPrefab, area08.transform);
                openBoat.name = "Sampan_Open_Pier";
                openBoat.transform.position = new Vector3(2.5f, -0.65f, -17.5f);
                openBoat.transform.rotation = Quaternion.Euler(0, 60f, 0);
                openBoat.transform.localScale = Vector3.one * 1.05f;
                openBoat.AddComponent<FloatingBoat3D>();
            }

            // Thùng cá muối & Thùng gỗ vận chuyển trên bến
            var barrelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Props/PF_Prop_SaltFish_Barrel.prefab");
            if (barrelPrefab != null)
            {
                PlacePrefab(barrelPrefab, area08.transform, new Vector3(4.8f, 0.05f, -13.0f), Vector3.one * 0.90f);
                PlacePrefab(barrelPrefab, area08.transform, new Vector3(7.2f, 0.05f, -14.5f), Vector3.one * 0.85f);
            }

            // NPC: Bác Lái Đò
            var villager05Prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Characters/NPCs/PF_NPC_Villager_05.prefab");
            if (villager05Prefab != null)
            {
                var ferryman = (GameObject)PrefabUtility.InstantiatePrefab(villager05Prefab, area08.transform);
                ferryman.name = "NPC_Ferryman_UncleBay";
                ferryman.transform.position = new Vector3(6.0f, 0.05f, -16.5f);
                ferryman.transform.rotation = Quaternion.Euler(0, 0f, 0);
                ferryman.transform.localScale = Vector3.one * 1.40f;
            }

            // Trigger Tương tác: Kiểm tra bến xuồng
            Create3DInteractionTrigger(triggersGroup.transform, "INT_InspectPier", new Vector3(6.0f, 0.05f, -16.5f), "Trò chuyện cùng Bác Lái Đò & xem chuyến đò chiều", 2.4f);

            // ==========================================
            // 17. VEGETATION: HỆ CÂY CỐI 3D & BỤI CÂY NHIỆT ĐỚI
            // ==========================================
            var heroKnollTreePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Environment/Trees/PF_Curved_Hero_Tree_Knoll.prefab");
            var heroLayeredTreePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Environment/Trees/PF_Hero_Layered_Tree.prefab");
            var branchClusterTreePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Environment/Trees/PF_Branch_Cluster_Tree.prefab");
            var bushBroadleafPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Environment/Lana_Concept_Assets/Bushes/PF_T_HD_Bush_Broadleaf_01.prefab");
            var bushFlowerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Environment/Lana_Concept_Assets/Bushes/PF_T_HD_Bush_Low_02_Flower.prefab");

            // Rặng cây đại thụ 3D xanh mát ôm trọn sau lưng Nhà An
            if (heroLayeredTreePrefab != null)
            {
                PlacePrefab(heroLayeredTreePrefab, vegGroup.transform, new Vector3(-6.5f, 0.45f, 25.5f), Vector3.one * 1.55f, 15f);
                PlacePrefab(heroLayeredTreePrefab, vegGroup.transform, new Vector3(7.5f, 0.45f, 25.0f), Vector3.one * 1.45f, -30f);
                PlacePrefab(heroLayeredTreePrefab, vegGroup.transform, new Vector3(-14.0f, 0.0f, -8.0f), Vector3.one * 1.35f, 40f);
            }

            if (heroKnollTreePrefab != null)
            {
                PlacePrefab(heroKnollTreePrefab, vegGroup.transform, new Vector3(2.5f, 0.45f, 26.5f), Vector3.one * 1.50f, 5f);
                PlacePrefab(heroKnollTreePrefab, vegGroup.transform, new Vector3(-22.0f, 0.20f, 23.5f), Vector3.one * 1.40f, 55f);
                PlacePrefab(heroKnollTreePrefab, vegGroup.transform, new Vector3(2.0f, 0.0f, -8.5f), Vector3.one * 1.25f, -20f);
            }

            if (branchClusterTreePrefab != null)
            {
                PlacePrefab(branchClusterTreePrefab, vegGroup.transform, new Vector3(-1.5f, 0.45f, 27.5f), Vector3.one * 1.70f, 180f);
                PlacePrefab(branchClusterTreePrefab, vegGroup.transform, new Vector3(-25.0f, 0.20f, 18.0f), Vector3.one * 1.45f, 75f);
                PlacePrefab(branchClusterTreePrefab, vegGroup.transform, new Vector3(14.0f, 0.10f, 13.5f), Vector3.one * 1.30f, -75f);
                PlacePrefab(branchClusterTreePrefab, vegGroup.transform, new Vector3(20.5f, 0.0f, -7.5f), Vector3.one * 1.35f, 50f);
            }

            // Bụi cây nhiệt đới 3D & bụi hoa quanh sân nhà An và lối đi
            if (bushBroadleafPrefab != null)
            {
                PlacePrefab(bushBroadleafPrefab, vegGroup.transform, new Vector3(-3.2f, 0.45f, 17.5f), Vector3.one * 0.90f, 20f);
                PlacePrefab(bushBroadleafPrefab, vegGroup.transform, new Vector3(9.2f, 0.45f, 18.5f), Vector3.one * 0.95f, 40f);
                PlacePrefab(bushBroadleafPrefab, vegGroup.transform, new Vector3(-11.5f, 0.20f, 8.5f), Vector3.one * 0.85f, -25f);
            }

            if (bushFlowerPrefab != null)
            {
                PlacePrefab(bushFlowerPrefab, vegGroup.transform, new Vector3(4.2f, 0.45f, 17.0f), Vector3.one * 0.90f, -15f);
                PlacePrefab(bushFlowerPrefab, vegGroup.transform, new Vector3(-7.5f, 0.20f, 4.5f), Vector3.one * 0.85f, 55f);
                PlacePrefab(bushFlowerPrefab, vegGroup.transform, new Vector3(14.5f, 0.08f, 1.5f), Vector3.one * 0.85f, -30f);
            }

            // Dừa nước ven mép kênh và bờ sông
            var nipaPalmPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Vegetation/PF_FG_NipaPalm_Fronds.prefab");
            if (nipaPalmPrefab != null)
            {
                PlacePrefab(nipaPalmPrefab, vegGroup.transform, new Vector3(-18.0f, -0.65f, -10.5f), Vector3.one * 1.25f, 35f);
                PlacePrefab(nipaPalmPrefab, vegGroup.transform, new Vector3(-11.0f, -0.65f, -10.5f), Vector3.one * 1.15f, -20f);
                PlacePrefab(nipaPalmPrefab, vegGroup.transform, new Vector3(-1.0f, -0.65f, -10.5f), Vector3.one * 1.20f, 15f);
                PlacePrefab(nipaPalmPrefab, vegGroup.transform, new Vector3(14.0f, -0.65f, -5.5f), Vector3.one * 1.15f, -40f);
                PlacePrefab(nipaPalmPrefab, vegGroup.transform, new Vector3(21.0f, -0.65f, 2.0f), Vector3.one * 1.25f, 25f);
                PlacePrefab(nipaPalmPrefab, vegGroup.transform, new Vector3(21.0f, -0.65f, 12.0f), Vector3.one * 1.30f, -25f);
                PlacePrefab(nipaPalmPrefab, vegGroup.transform, new Vector3(21.0f, -0.65f, 22.0f), Vector3.one * 1.25f, 40f);
            }

            // Bụi cỏ nghệ thuật 3D rải rác tạo chiều sâu
            var grassTuftPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Foliage/PF_StylizedGrassTuft_MeadowCluster.prefab");
            if (grassTuftPrefab != null)
            {
                Vector3[] tuftSpots = new Vector3[]
                {
                    new Vector3(-6f, 0f, 0.5f), new Vector3(4f, 0f, -1.5f), new Vector3(-2f, 0f, -3.5f),
                    new Vector3(-10f, 0.2f, 7f), new Vector3(10f, 0.1f, 2f), new Vector3(24f, 0.1f, 6f),
                    new Vector3(1.5f, 0.45f, 13.5f), new Vector3(-3f, 0.45f, 16.5f)
                };
                foreach (var spot in tuftSpots)
                {
                    PlacePrefab(grassTuftPrefab, vegGroup.transform, spot, Vector3.one * 0.9f);
                }
            }

            // Hoa dại đồng quê Việt Nam (Hoa trắng, hoa cúc vàng & bụi sậy ven bờ rạch)
            var whiteFlowerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Environment/Lana_VN_Pack/Wildflowers/PF_LanaVN_Wildflower_03_WhiteBlossom.prefab");
            if (whiteFlowerPrefab != null)
            {
                PlacePrefab(whiteFlowerPrefab, vegGroup.transform, new Vector3(-3.5f, 0.05f, 4.5f), Vector3.one * 1.1f, 25f);
                PlacePrefab(whiteFlowerPrefab, vegGroup.transform, new Vector3(2.5f, 0.45f, 14.5f), Vector3.one * 1.0f, -40f);
                PlacePrefab(whiteFlowerPrefab, vegGroup.transform, new Vector3(-12.5f, 0.20f, 6.0f), Vector3.one * 1.15f, 60f);
                PlacePrefab(whiteFlowerPrefab, vegGroup.transform, new Vector3(14.0f, 0.08f, 2.5f), Vector3.one * 0.95f, -15f);
            }

            var yellowFlowerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Environment/Lana_VN_Pack/Wildflowers/PF_LanaVN_Wildflower_05_YellowBlossom.prefab");
            if (yellowFlowerPrefab != null)
            {
                PlacePrefab(yellowFlowerPrefab, vegGroup.transform, new Vector3(-4.8f, 0.008f, -1.8f), Vector3.one * 1.0f, 45f);
                PlacePrefab(yellowFlowerPrefab, vegGroup.transform, new Vector3(6.5f, 0.008f, -2.5f), Vector3.one * 1.1f, -30f);
                PlacePrefab(yellowFlowerPrefab, vegGroup.transform, new Vector3(13.5f, 0.08f, -3.8f), Vector3.one * 1.05f, 15f);
            }

            var reedsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Environment/Lana_VN_Pack/Wildflowers/PF_LanaVN_Wildflower_01_Reeds.prefab");
            if (reedsPrefab != null)
            {
                PlacePrefab(reedsPrefab, vegGroup.transform, new Vector3(15.2f, -0.65f, -1.5f), Vector3.one * 1.25f, 10f);
                PlacePrefab(reedsPrefab, vegGroup.transform, new Vector3(19.8f, -0.65f, -3.0f), Vector3.one * 1.30f, -20f);
                PlacePrefab(reedsPrefab, vegGroup.transform, new Vector3(-7.5f, -0.65f, -9.8f), Vector3.one * 1.20f, 35f);
            }

            // Bao lúa chất bên đống rơm và chòi bến sông
            if (riceSacksPrefab != null)
            {
                PlacePrefab(riceSacksPrefab, propsGroup.transform, new Vector3(7.8f, 0.008f, 2.0f), Vector3.one * 1.10f, -35f);
                PlacePrefab(riceSacksPrefab, propsGroup.transform, new Vector3(3.2f, 0.05f, -12.5f), Vector3.one * 1.05f, 15f);
            }

            // Bụi bụi vàng hoàng hôn lơ lửng trong không gian làng quê (Atmospheric Golden Dust VFX)
            var dustPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/VFX/PF_Atmospheric_GoldenDust.prefab");
            if (dustPrefab != null)
            {
                var dust = (GameObject)PrefabUtility.InstantiatePrefab(dustPrefab, bgGroup.transform);
                dust.name = "VFX_Atmospheric_GoldenDust";
                dust.transform.position = new Vector3(0f, 2.5f, 0f);
            }

            // ==========================================
            // NATURAL SHORELINES, MUD BANKS & 3D BOULDERS
            // ==========================================
            var mossyBoulderPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Environment/PF_MossyBoulder.prefab");
            var largeRockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Environment/PF_Rock_Large_01.prefab");
            var mudBankPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Environment/ModularKit/PF_Mod_MudBank_Chunk.prefab");

            if (mudBankPrefab != null)
            {
                // Bờ bùn tự nhiên che lấp viền vuông vức mép nước Kênh Đông
                PlacePrefab(mudBankPrefab, waterGroup.transform, new Vector3(15.2f, -0.65f, 2.0f), Vector3.one * 0.95f, 90f);
                PlacePrefab(mudBankPrefab, waterGroup.transform, new Vector3(15.2f, -0.65f, 12.0f), Vector3.one * 1.0f, 90f);
                PlacePrefab(mudBankPrefab, waterGroup.transform, new Vector3(15.2f, -0.65f, 22.0f), Vector3.one * 0.95f, 90f);

                // Bờ bùn tự nhiên dọc mép nước Bờ Sông Nam
                PlacePrefab(mudBankPrefab, waterGroup.transform, new Vector3(-16.0f, -0.65f, -10.5f), Vector3.one * 1.05f, 0f);
                PlacePrefab(mudBankPrefab, waterGroup.transform, new Vector3(-5.0f, -0.65f, -10.5f), Vector3.one * 1.0f, 0f);
                PlacePrefab(mudBankPrefab, waterGroup.transform, new Vector3(12.0f, -0.65f, -10.5f), Vector3.one * 1.10f, 0f);
            }

            if (mossyBoulderPrefab != null)
            {
                PlacePrefab(mossyBoulderPrefab, waterGroup.transform, new Vector3(14.8f, -0.4f, -2.5f), Vector3.one * 1.2f, 30f);
                PlacePrefab(mossyBoulderPrefab, waterGroup.transform, new Vector3(15.5f, -0.4f, 7.5f), Vector3.one * 1.0f, -45f);
                PlacePrefab(mossyBoulderPrefab, waterGroup.transform, new Vector3(-4.0f, -0.4f, -10.2f), Vector3.one * 1.3f, 15f);
                PlacePrefab(mossyBoulderPrefab, area04.transform, new Vector3(-6.5f, 0.0f, 4.5f), Vector3.one * 1.15f, 45f);
            }

            if (largeRockPrefab != null)
            {
                PlacePrefab(largeRockPrefab, waterGroup.transform, new Vector3(-18.5f, -0.5f, -10.0f), Vector3.one * 1.1f, 60f);
                PlacePrefab(largeRockPrefab, waterGroup.transform, new Vector3(9.0f, -0.5f, -10.8f), Vector3.one * 1.2f, -25f);
                PlacePrefab(largeRockPrefab, waterGroup.transform, new Vector3(15.8f, -0.5f, 16.5f), Vector3.one * 1.05f, 40f);
            }

            // ==========================================
            // 18. PLAYER SETUP: BÉ AN (FULL 3D MOVEMENT)
            // ==========================================
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Characters/PF_Player_An.prefab");
            GameObject playerObj = null;
            if (playerPrefab != null)
            {
                playerObj = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, playerGroup.transform);
                playerObj.name = "An_Player";
                playerObj.tag = "Player";
                // Spawn ngay trên đường đất trước bậc thềm Nhà An, hướng mặt về phía Mẹ và Nhà sàn 3D
                playerObj.transform.position = new Vector3(0.5f, 0.45f, 13.0f);
                playerObj.transform.rotation = Quaternion.Euler(0, 0f, 0);
                playerObj.transform.localScale = Vector3.one * 0.58f;

                // Tháo gỡ / Vô hiệu hóa controller 2.5D cũ
                var old2DController = playerObj.GetComponent("PlayerController") as MonoBehaviour;
                if (old2DController != null) UnityEngine.Object.DestroyImmediate(old2DController);

                var oldMotor = playerObj.GetComponent("PlayerMotor") as MonoBehaviour;
                if (oldMotor != null) UnityEngine.Object.DestroyImmediate(oldMotor);

                // Cấu hình CharacterController 3D mượt mà
                var cc = playerObj.GetComponent<CharacterController>();
                if (cc == null) cc = playerObj.AddComponent<CharacterController>();
                cc.center = new Vector3(0, 0.95f, 0);
                cc.height = 1.9f;
                cc.radius = 0.42f;
                cc.stepOffset = 0.45f;
                cc.slopeLimit = 50f;

                // Gắn Player3DController điều khiển tự do 3D (WASD/Arrows + Jump + Camera Relative)
                var p3d = playerObj.GetComponent<Player3DController>();
                if (p3d == null) p3d = playerObj.AddComponent<Player3DController>();

                // Gán Silhouette Shader điện ảnh Planet of Lana
                var smr = playerObj.GetComponentInChildren<SkinnedMeshRenderer>();
                if (smr != null && anSilhouetteMat != null)
                {
                    smr.sharedMaterial = anSilhouetteMat;
                }

                // Sample Idle pose ngay trong Scene để không bị T-pose
                var idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/TripoModels/Boy_Character/Hero_Loco_Idle_Loop_IP.anim");
                if (idleClip != null)
                {
                    idleClip.SampleAnimation(playerObj, 0f);
                }
            }

            // ==========================================
            // 19. CAMERA RIG: THIRD-PERSON 3D ORBIT CAMERA
            // ==========================================
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            camObj.transform.SetParent(cameraGroup.transform, false);

            Camera cam = camObj.AddComponent<Camera>();
            cam.orthographic = false;
            cam.fieldOfView = 60.0f; // Góc nhìn 3D chuẩn game góc nhìn thứ 3
            cam.nearClipPlane = 0.2f;
            cam.farClipPlane = 250f;
            cam.clearFlags = CameraClearFlags.Skybox;

            var urpCam = camObj.AddComponent<UniversalAdditionalCameraData>();
            urpCam.renderPostProcessing = true;
            urpCam.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            camObj.AddComponent<AudioListener>();

            // Gắn ThirdPersonOrbitCamera theo dõi nhân vật 3D góc nhìn thứ 3 chuẩn mực
            var tpCam = camObj.AddComponent<ThirdPersonOrbitCamera>();
            if (playerObj != null)
            {
                tpCam.Target = playerObj.transform;
                camObj.transform.position = playerObj.transform.position + new Vector3(0.25f, 1.8f, -3.6f);
                camObj.transform.rotation = Quaternion.Euler(16.0f, 0f, 0f);
            }

            // ==========================================
            // 20. MANAGERS & INTERACTION SYSTEM 3D
            // ==========================================
            GameObject interSysObj = new GameObject("InteractionSystem3D");
            interSysObj.transform.SetParent(managersGroup.transform, false);
            var interSys = interSysObj.AddComponent<InteractionSystem3D>();
            if (playerObj != null)
            {
                var pField = typeof(InteractionSystem3D).GetField("playerTransform", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (pField != null) pField.SetValue(interSys, playerObj.transform);
            }

            // ==========================================
            // 21. AUDIO (TIẾNG GIÓ & NHẠC QUÊ HƯƠNG THANH BÌNH)
            // ==========================================
            var windClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/soundreality-wind-blowing-457954.mp3");
            var musicClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Breath of the World.mp3");

            GameObject audioCtrlObj = new GameObject("IntroAudioController");
            audioCtrlObj.transform.SetParent(audioGroup.transform, false);
            var audioCtrl = audioCtrlObj.AddComponent<IntroAudioController>();

            AudioSource windSource = audioCtrlObj.AddComponent<AudioSource>();
            windSource.clip = windClip;
            windSource.loop = true;
            windSource.playOnAwake = false;
            windSource.volume = 0.32f;

            AudioSource musicSource = audioCtrlObj.AddComponent<AudioSource>();
            musicSource.clip = musicClip;
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.volume = 0.45f;

            audioCtrl.windAudioSource = windSource;
            audioCtrl.lofiAudioSource = musicSource;
            audioCtrl.windMaxVolume = 0.32f;
            audioCtrl.lofiMaxVolume = 0.45f;

            // ==========================================
            // 22. LƯU SCENE VÀO Assets/Scenes/CaMau_1954_Village.unity
            // ==========================================
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"<color=#34d399>[THÀNH CÔNG]</color> Đã dựng và lưu Scene 3D hoàn chỉnh tại: <b>{ScenePath}</b>");
        }

        // ==========================================
        // CÁC HÀM TIỆN ÍCH DỰNG GEOMETRY 3D
        // ==========================================
        private static GameObject CreateChild(GameObject parent, string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent.transform, false);
            return child;
        }

        private static Material GetOrCreateMaterial(string name, Color color, float smoothness = 0.1f)
        {
            string path = $"{MaterialsPath}/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
                if (urpLit == null) urpLit = Shader.Find("Standard");
                mat = new Material(urpLit);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                else mat.color = color;
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }

        private static void CreateGroundBlock(Transform parent, string name, Vector3 pos, Vector3 size, Material grassMat)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.position = pos;
            block.transform.localScale = size;
            var rend = block.GetComponent<Renderer>();
            rend.sharedMaterial = grassMat;

            // World-space UV tiling: 1 tile every 7.5 meters
            var mpb = new MaterialPropertyBlock();
            rend.GetPropertyBlock(mpb);
            mpb.SetVector("_BaseMap_ST", new Vector4(size.x / 7.5f, size.z / 7.5f, 0f, 0f));
            rend.SetPropertyBlock(mpb);
        }

        private static void CreateSlopedRamp(Transform parent, string name, Vector3 pos, Vector3 size, float pitchAngle, Material mat, float rollAngle = 0f)
        {
            GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ramp.name = name;
            ramp.transform.SetParent(parent, false);
            ramp.transform.position = pos;
            ramp.transform.rotation = Quaternion.Euler(pitchAngle, 0f, rollAngle);
            ramp.transform.localScale = size;
            var rend = ramp.GetComponent<Renderer>();
            rend.sharedMaterial = mat;

            var mpb = new MaterialPropertyBlock();
            rend.GetPropertyBlock(mpb);
            mpb.SetVector("_BaseMap_ST", new Vector4(size.x / 7.5f, size.z / 7.5f, 0f, 0f));
            rend.SetPropertyBlock(mpb);
        }

        private static void CreateWalkableDike(Transform parent, string name, Vector3 pos, Vector3 size, Material dirtMat)
        {
            GameObject dike = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dike.name = name;
            dike.transform.SetParent(parent, false);
            dike.transform.position = pos;
            dike.transform.localScale = size;
            dike.GetComponent<Renderer>().sharedMaterial = dirtMat;
        }

        private static void CreateWorldBarrier(Transform parent, string name, Vector3 pos, Vector3 size)
        {
            GameObject barrier = new GameObject(name);
            barrier.transform.SetParent(parent, false);
            barrier.transform.position = pos;
            var box = barrier.AddComponent<BoxCollider>();
            box.size = size;
        }

        private static void CreateDirtCourtyard(Transform parent, string name, Vector3 pos, Vector3 size, Material dirtMat)
        {
            GameObject patch = GameObject.CreatePrimitive(PrimitiveType.Cube);
            patch.name = name;
            patch.transform.SetParent(parent, false);
            patch.transform.position = pos;
            patch.transform.localScale = size;
            patch.GetComponent<Renderer>().sharedMaterial = dirtMat;
            UnityEngine.Object.DestroyImmediate(patch.GetComponent<Collider>());
        }

        private static void CreateDirtPathSegment(Transform parent, string name, Vector3 pos, Vector3 size, Material dirtMat, float yaw = 0f)
        {
            GameObject path = GameObject.CreatePrimitive(PrimitiveType.Cube);
            path.name = name;
            path.transform.SetParent(parent, false);
            path.transform.position = pos;
            path.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            path.transform.localScale = size;
            path.GetComponent<Renderer>().sharedMaterial = dirtMat;
            UnityEngine.Object.DestroyImmediate(path.GetComponent<Collider>());
        }

        private static GameObject CreateWaterSurface(string name, Vector3 pos, Vector3 size, Material waterMat)
        {
            GameObject water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            water.name = name;
            water.transform.position = pos;
            water.transform.localScale = new Vector3(size.x / 10f, 1f, size.z / 10f);
            water.GetComponent<Renderer>().sharedMaterial = waterMat;
            UnityEngine.Object.DestroyImmediate(water.GetComponent<Collider>());
            return water;
        }

        private static void PlacePrefab(GameObject prefab, Transform parent, Vector3 pos, Vector3 scale, float yaw = 0f)
        {
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            inst.transform.position = pos;
            inst.transform.rotation = Quaternion.Euler(0, yaw, 0);
            inst.transform.localScale = scale;
        }

        // ==========================================
        // CÁC MÔ HÌNH 3D CHI TIẾT (PROCEDURAL 3D CRAFTING)
        // ==========================================
        private static void CreateWoodenSteps3D(Transform parent, Vector3 pos, Material timberMat, int steps)
        {
            GameObject stepsRoot = new GameObject("WoodenStairs_Steps");
            stepsRoot.transform.SetParent(parent, false);
            stepsRoot.transform.position = pos;

            for (int i = 0; i < steps; i++)
            {
                GameObject step = GameObject.CreatePrimitive(PrimitiveType.Cube);
                step.name = $"Step_{i}";
                step.transform.SetParent(stepsRoot.transform, false);
                step.transform.localPosition = new Vector3(0f, i * 0.14f, i * 0.28f);
                step.transform.localScale = new Vector3(1.8f, 0.12f, 0.35f);
                step.GetComponent<Renderer>().sharedMaterial = timberMat;
            }

            // Dốc collider mượt mà cho cầu thang giúp nhân vật bước lên êm ái
            GameObject ramp = new GameObject("Stairs_SmoothRamp");
            ramp.transform.SetParent(stepsRoot.transform, false);
            ramp.transform.localPosition = new Vector3(0f, (steps * 0.14f) * 0.5f, (steps * 0.28f) * 0.5f);
            ramp.transform.localRotation = Quaternion.Euler(26f, 0f, 0f);
            var rBox = ramp.AddComponent<BoxCollider>();
            rBox.size = new Vector3(1.9f, 0.15f, steps * 0.35f);
        }

        private static void Create3DClothesline(Transform parent, Vector3 start, Vector3 end, Material woodMat, Material cloth1, Material cloth2)
        {
            GameObject cl = new GameObject("Clothesline_3D");
            cl.transform.SetParent(parent, false);

            // 2 cọc tre
            GameObject p1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            p1.name = "Post_A";
            p1.transform.SetParent(cl.transform, false);
            p1.transform.position = start + Vector3.up * 1.0f;
            p1.transform.localScale = new Vector3(0.08f, 1.0f, 0.08f);
            p1.GetComponent<Renderer>().sharedMaterial = woodMat;

            GameObject p2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            p2.name = "Post_B";
            p2.transform.SetParent(cl.transform, false);
            p2.transform.position = end + Vector3.up * 1.0f;
            p2.transform.localScale = new Vector3(0.08f, 1.0f, 0.08f);
            p2.GetComponent<Renderer>().sharedMaterial = woodMat;

            // Dây phơi
            Vector3 mid = (start + end) * 0.5f + Vector3.up * 1.85f;
            float dist = Vector3.Distance(start, end);
            GameObject rope = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rope.name = "Rope";
            rope.transform.SetParent(cl.transform, false);
            rope.transform.position = mid;
            rope.transform.rotation = Quaternion.LookRotation(end - start);
            rope.transform.localScale = new Vector3(0.02f, 0.02f, dist);
            rope.GetComponent<Renderer>().sharedMaterial = woodMat;
            UnityEngine.Object.DestroyImmediate(rope.GetComponent<Collider>());

            // Áo bà ba nâu
            GameObject shirt = GameObject.CreatePrimitive(PrimitiveType.Quad);
            shirt.name = "Cloth_AoBaBa";
            shirt.transform.SetParent(cl.transform, false);
            shirt.transform.position = start + (end - start) * 0.35f + Vector3.up * 1.45f;
            shirt.transform.rotation = Quaternion.LookRotation(end - start) * Quaternion.Euler(0, 90f, 0);
            shirt.transform.localScale = new Vector3(0.65f, 0.75f, 1f);
            shirt.GetComponent<Renderer>().sharedMaterial = cloth1;
            UnityEngine.Object.DestroyImmediate(shirt.GetComponent<Collider>());

            // Khăn rằn đỏ
            GameObject scarf = GameObject.CreatePrimitive(PrimitiveType.Quad);
            scarf.name = "Cloth_KhanRan";
            scarf.transform.SetParent(cl.transform, false);
            scarf.transform.position = start + (end - start) * 0.70f + Vector3.up * 1.50f;
            scarf.transform.rotation = Quaternion.LookRotation(end - start) * Quaternion.Euler(0, 90f, 0);
            scarf.transform.localScale = new Vector3(0.35f, 0.65f, 1f);
            scarf.GetComponent<Renderer>().sharedMaterial = cloth2;
            UnityEngine.Object.DestroyImmediate(scarf.GetComponent<Collider>());
        }

        private static void CreateCeramicWaterUrn(Transform parent, string name, Vector3 pos, float radius, Material urnMat, Material lidMat)
        {
            GameObject urn = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            urn.name = name;
            urn.transform.SetParent(parent, false);
            urn.transform.localPosition = pos + Vector3.up * (radius * 0.9f);
            urn.transform.localScale = new Vector3(radius * 2f, radius * 0.9f, radius * 2f);
            urn.GetComponent<Renderer>().sharedMaterial = urnMat;

            // Nắp vại
            GameObject lid = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lid.name = "Lid";
            lid.transform.SetParent(urn.transform, false);
            lid.transform.localPosition = new Vector3(0f, 1.02f, 0f);
            lid.transform.localScale = new Vector3(1.05f, 0.08f, 1.05f);
            lid.GetComponent<Renderer>().sharedMaterial = lidMat;
            UnityEngine.Object.DestroyImmediate(lid.GetComponent<Collider>());
        }

        private static void CreateCoconutBroom3D(Transform parent, Vector3 pos, Material bambooMat)
        {
            GameObject broom = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            broom.name = "CoconutBroom_3D";
            broom.transform.SetParent(parent, false);
            broom.transform.position = pos + new Vector3(0f, 0.45f, 0f);
            broom.transform.rotation = Quaternion.Euler(15f, 25f, 0f);
            broom.transform.localScale = new Vector3(0.06f, 0.45f, 0.06f);
            broom.GetComponent<Renderer>().sharedMaterial = bambooMat;
        }

        private static void CreateChickenPen3D(Transform parent, Vector3 pos, Material bambooMat, Material thatchMat)
        {
            GameObject coop = new GameObject("ChickenCoop_3D");
            coop.transform.SetParent(parent, false);
            coop.transform.position = pos;

            // Chòi gà nhỏ
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "Coop_Body";
            box.transform.SetParent(coop.transform, false);
            box.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            box.transform.localScale = new Vector3(1.6f, 1.0f, 1.4f);
            box.GetComponent<Renderer>().sharedMaterial = bambooMat;

            // Mái lá
            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "Coop_Roof";
            roof.transform.SetParent(coop.transform, false);
            roof.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            roof.transform.localScale = new Vector3(1.9f, 0.15f, 1.7f);
            roof.GetComponent<Renderer>().sharedMaterial = thatchMat;
        }

        private static void CreatePottedPlant3D(Transform parent, Vector3 pos, Material potMat, Material leafMat)
        {
            GameObject pot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pot.name = "PottedPlant_3D";
            pot.transform.SetParent(parent, false);
            pot.transform.position = pos + Vector3.up * 0.15f;
            pot.transform.localScale = new Vector3(0.35f, 0.15f, 0.35f);
            pot.GetComponent<Renderer>().sharedMaterial = potMat;

            GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leaf.name = "PlantFoliage";
            leaf.transform.SetParent(pot.transform, false);
            leaf.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            leaf.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
            leaf.GetComponent<Renderer>().sharedMaterial = leafMat;
            UnityEngine.Object.DestroyImmediate(leaf.GetComponent<Collider>());
        }

        private static void Create3DWellStation(Transform parent, Vector3 pos, Material stoneMat, Material timberMat, Material thatchMat, Material urnMat, Material waterMat)
        {
            GameObject station = new GameObject("WellStation_3D");
            station.transform.SetParent(parent, false);
            station.transform.position = pos;

            // Nền đá xẻ lát giếng
            GameObject stoneDeck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stoneDeck.name = "StonePlatform";
            stoneDeck.transform.SetParent(station.transform, false);
            stoneDeck.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            stoneDeck.transform.localScale = new Vector3(4.2f, 0.08f, 4.2f);
            stoneDeck.GetComponent<Renderer>().sharedMaterial = stoneMat;

            // Thành giếng đá tròn
            GameObject wellRim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wellRim.name = "Well_Rim";
            wellRim.transform.SetParent(station.transform, false);
            wellRim.transform.localPosition = new Vector3(-0.4f, 0.45f, 0f);
            wellRim.transform.localScale = new Vector3(1.8f, 0.45f, 1.8f);
            wellRim.GetComponent<Renderer>().sharedMaterial = stoneMat;

            // Mặt nước trong lòng giếng
            GameObject wellWater = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wellWater.name = "Well_WaterSurface";
            wellWater.transform.SetParent(wellRim.transform, false);
            wellWater.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            wellWater.transform.localScale = new Vector3(0.85f, 0.02f, 0.85f);
            wellWater.GetComponent<Renderer>().sharedMaterial = waterMat;
            UnityEngine.Object.DestroyImmediate(wellWater.GetComponent<Collider>());

            // 4 Cột chòi che giếng
            for (int x = -1; x <= 1; x += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    post.name = $"Post_{x}_{z}";
                    post.transform.SetParent(station.transform, false);
                    post.transform.localPosition = new Vector3(x * 1.8f, 1.15f, z * 1.8f);
                    post.transform.localScale = new Vector3(0.12f, 1.15f, 0.12f);
                    post.GetComponent<Renderer>().sharedMaterial = timberMat;
                }
            }

            // Mái lá chòi che giếng
            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "Well_Roof_Thatch";
            roof.transform.SetParent(station.transform, false);
            roof.transform.localPosition = new Vector3(0f, 2.45f, 0f);
            roof.transform.localScale = new Vector3(4.2f, 0.20f, 4.2f);
            roof.GetComponent<Renderer>().sharedMaterial = thatchMat;

            // Các chum vại sành trữ nước
            CreateCeramicWaterUrn(station.transform, "ChumNuoc_01", new Vector3(1.2f, 0.08f, -1.0f), 0.45f, urnMat, stoneMat);
            CreateCeramicWaterUrn(station.transform, "ChumNuoc_02", new Vector3(1.4f, 0.08f, 0.5f), 0.52f, urnMat, stoneMat);
            CreateCeramicWaterUrn(station.transform, "ChumNuoc_03", new Vector3(-1.3f, 0.08f, 1.3f), 0.38f, urnMat, stoneMat);
        }

        private static void Create3DHaystack(Transform parent, Vector3 pos, Material strawMat, Material timberMat)
        {
            GameObject haystack = new GameObject("Haystack_3D");
            haystack.transform.SetParent(parent, false);
            haystack.transform.position = pos;

            // Tầng 1: Đáy rộng
            GameObject t1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            t1.name = "Tier_Bottom";
            t1.transform.SetParent(haystack.transform, false);
            t1.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            t1.transform.localScale = new Vector3(3.4f, 0.45f, 3.4f);
            t1.GetComponent<Renderer>().sharedMaterial = strawMat;

            // Tầng 2: Thân vòm
            GameObject t2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            t2.name = "Tier_Mid";
            t2.transform.SetParent(haystack.transform, false);
            t2.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            t2.transform.localScale = new Vector3(2.6f, 0.45f, 2.6f);
            t2.GetComponent<Renderer>().sharedMaterial = strawMat;

            // Tầng 3: Chóp nhọn
            GameObject t3 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            t3.name = "Tier_TopDome";
            t3.transform.SetParent(haystack.transform, false);
            t3.transform.localPosition = new Vector3(0f, 1.85f, 0f);
            t3.transform.localScale = new Vector3(1.8f, 1.2f, 1.8f);
            t3.GetComponent<Renderer>().sharedMaterial = strawMat;

            // Cọc tre nhô lên ở đỉnh giữ đống rơm
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "CenterPole";
            pole.transform.SetParent(haystack.transform, false);
            pole.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            pole.transform.localScale = new Vector3(0.08f, 1.6f, 0.08f);
            pole.GetComponent<Renderer>().sharedMaterial = timberMat;
        }

        private static void Create3DFarmCart(Transform parent, Vector3 pos, Material timberMat, Material strawMat)
        {
            GameObject cart = new GameObject("FarmCart_3D");
            cart.transform.SetParent(parent, false);
            cart.transform.position = pos;

            // Thùng xe
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Cart_Body";
            body.transform.SetParent(cart.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            body.transform.localScale = new Vector3(1.4f, 0.35f, 2.0f);
            body.GetComponent<Renderer>().sharedMaterial = timberMat;

            // Hai bánh xe gỗ
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wheel.name = $"Wheel_{(side < 0 ? "L" : "R")}";
                wheel.transform.SetParent(cart.transform, false);
                wheel.transform.localPosition = new Vector3(side * 0.8f, 0.35f, 0f);
                wheel.transform.localRotation = Quaternion.Euler(0, 0, 90f);
                wheel.transform.localScale = new Vector3(0.70f, 0.08f, 0.70f);
                wheel.GetComponent<Renderer>().sharedMaterial = timberMat;
            }

            // Đống rơm nhỏ trên xe
            GameObject load = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            load.name = "Straw_Load";
            load.transform.SetParent(cart.transform, false);
            load.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            load.transform.localScale = new Vector3(1.2f, 0.5f, 1.6f);
            load.GetComponent<Renderer>().sharedMaterial = strawMat;
            UnityEngine.Object.DestroyImmediate(load.GetComponent<Collider>());
        }

        private static void Create3DGourdTrellis(Transform parent, Vector3 pos, Material bambooMat, Material leafMat)
        {
            GameObject trellis = new GameObject("GourdTrellis_3D");
            trellis.transform.SetParent(parent, false);
            trellis.transform.position = pos;

            // 6 Cột tre dựng giàn
            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    post.name = $"Post_{x}_{z}";
                    post.transform.SetParent(trellis.transform, false);
                    post.transform.localPosition = new Vector3(x * 2.0f, 1.15f, z * 1.5f);
                    post.transform.localScale = new Vector3(0.08f, 1.15f, 0.08f);
                    post.GetComponent<Renderer>().sharedMaterial = bambooMat;
                }
            }

            // Mái giàn nan tre
            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "Trellis_Roof";
            roof.transform.SetParent(trellis.transform, false);
            roof.transform.localPosition = new Vector3(0f, 2.30f, 0f);
            roof.transform.localScale = new Vector3(4.8f, 0.06f, 3.4f);
            roof.GetComponent<Renderer>().sharedMaterial = bambooMat;

            // Thảm lá dây leo xanh mướt trên giàn
            GameObject vineMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vineMesh.name = "Vine_Foliage";
            vineMesh.transform.SetParent(trellis.transform, false);
            vineMesh.transform.localPosition = new Vector3(0f, 2.36f, 0f);
            vineMesh.transform.localScale = new Vector3(4.6f, 0.12f, 3.2f);
            vineMesh.GetComponent<Renderer>().sharedMaterial = leafMat;
            UnityEngine.Object.DestroyImmediate(vineMesh.GetComponent<Collider>());

            // 6 Quả Bầu Dài treo lủng lẳng dưới giàn
            Material gourdMat = GetOrCreateMaterial("M_CaMau_Gourd", new Color(0.68f, 0.82f, 0.45f), 0.25f);
            Vector3[] gourdOffsets = new Vector3[]
            {
                new Vector3(-1.4f, 1.70f, -0.6f), new Vector3(-0.6f, 1.65f, 0.8f), new Vector3(0.4f, 1.80f, -0.4f),
                new Vector3(1.2f, 1.60f, 0.5f), new Vector3(-1.0f, 1.75f, 0.3f), new Vector3(1.6f, 1.70f, -0.7f)
            };

            for (int i = 0; i < gourdOffsets.Length; i++)
            {
                GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                g.name = $"Gourd_{i}";
                g.transform.SetParent(trellis.transform, false);
                g.transform.localPosition = gourdOffsets[i];
                g.transform.localScale = new Vector3(0.12f, 0.28f, 0.12f);
                g.GetComponent<Renderer>().sharedMaterial = gourdMat;
                UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
            }
        }

        private static void Create3DFruitTree(Transform parent, Vector3 pos, Material woodMat, Material leafMat, Color fruitColor)
        {
            GameObject tree = new GameObject("FruitTree_3D");
            tree.transform.SetParent(parent, false);
            tree.transform.position = pos;

            // Thân cây
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(tree.transform, false);
            trunk.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            trunk.transform.localScale = new Vector3(0.25f, 1.25f, 0.25f);
            trunk.GetComponent<Renderer>().sharedMaterial = woodMat;

            // Tán lá
            GameObject leaves = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leaves.name = "Canopy";
            leaves.transform.SetParent(tree.transform, false);
            leaves.transform.localPosition = new Vector3(0f, 2.8f, 0f);
            leaves.transform.localScale = new Vector3(3.2f, 2.2f, 3.2f);
            leaves.GetComponent<Renderer>().sharedMaterial = leafMat;

            // Chùm quả chín
            Material fruitMat = GetOrCreateMaterial("M_Fruit_Red", fruitColor, 0.35f);
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f * Mathf.Deg2Rad;
                GameObject fruit = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                fruit.name = $"Fruit_{i}";
                fruit.transform.SetParent(leaves.transform, false);
                fruit.transform.localPosition = new Vector3(Mathf.Cos(angle) * 0.45f, -0.25f, Mathf.Sin(angle) * 0.45f);
                fruit.transform.localScale = Vector3.one * 0.18f;
                fruit.GetComponent<Renderer>().sharedMaterial = fruitMat;
                UnityEngine.Object.DestroyImmediate(fruit.GetComponent<Collider>());
            }
        }

        private static void Create3DArchedBridge(Transform parent, Vector3 start, Vector3 end, float width, float archHeight, Material timberMat, Material pilingMat)
        {
            GameObject bridge = new GameObject("ArchedWoodenBridge_3D");
            bridge.transform.SetParent(parent, false);

            Vector3 center = (start + end) * 0.5f;
            float span = Vector3.Distance(start, end);
            int plankCount = 16;

            for (int i = 0; i <= plankCount; i++)
            {
                float t = (float)i / plankCount;
                Vector3 p = Vector3.Lerp(start, end, t);
                // Vòm parabol: 4 * h * t * (1 - t)
                p.y += 4f * archHeight * t * (1f - t);

                GameObject plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plank.name = $"BridgePlank_{i}";
                plank.transform.SetParent(bridge.transform, false);
                plank.transform.position = p;
                plank.transform.rotation = Quaternion.LookRotation(end - start) * Quaternion.Euler(0, 90f, 0);
                plank.transform.localScale = new Vector3(width, 0.08f, 0.35f);
                plank.GetComponent<Renderer>().sharedMaterial = timberMat;
            }

            // 4 Cặp Cọc gỗ đóng sâu dưới lòng kênh
            for (int pIdx = 1; pIdx <= 3; pIdx++)
            {
                float t = pIdx * 0.25f;
                Vector3 p = Vector3.Lerp(start, end, t);
                p.y += 4f * archHeight * t * (1f - t);

                for (int side = -1; side <= 1; side += 2)
                {
                    GameObject pile = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    pile.name = $"BridgePile_{pIdx}_{side}";
                    pile.transform.SetParent(bridge.transform, false);
                    pile.transform.position = new Vector3(p.x, -0.2f, p.z + side * (width * 0.45f));
                    pile.transform.localScale = new Vector3(0.18f, 1.2f, 0.18f);
                    pile.GetComponent<Renderer>().sharedMaterial = pilingMat;
                }
            }

            // Lan can cầu hai bên
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rail.name = $"Handrail_{(side < 0 ? "S" : "N")}";
                rail.transform.SetParent(bridge.transform, false);
                rail.transform.position = center + Vector3.up * (archHeight * 0.7f + 0.5f) + new Vector3(0, 0, side * (width * 0.48f));
                rail.transform.rotation = Quaternion.LookRotation(end - start);
                rail.transform.localScale = new Vector3(0.08f, 0.08f, span);
                rail.GetComponent<Renderer>().sharedMaterial = timberMat;
                UnityEngine.Object.DestroyImmediate(rail.GetComponent<Collider>());
            }

            // Dốc collider mượt mà cho cầu vòm
            GameObject walkCol = new GameObject("Bridge_Walkable_Ramps");
            walkCol.transform.SetParent(bridge.transform, false);

            GameObject rampUp = new GameObject("Ramp_Up");
            rampUp.transform.SetParent(walkCol.transform, false);
            rampUp.transform.position = Vector3.Lerp(start, center, 0.5f) + Vector3.up * (archHeight * 0.45f);
            rampUp.transform.rotation = Quaternion.LookRotation(end - start) * Quaternion.Euler(12f, 0, 0);
            var b1 = rampUp.AddComponent<BoxCollider>();
            b1.size = new Vector3(width, 0.15f, span * 0.55f);

            GameObject rampDown = new GameObject("Ramp_Down");
            rampDown.transform.SetParent(walkCol.transform, false);
            rampDown.transform.position = Vector3.Lerp(center, end, 0.5f) + Vector3.up * (archHeight * 0.45f);
            rampDown.transform.rotation = Quaternion.LookRotation(end - start) * Quaternion.Euler(-12f, 0, 0);
            var b2 = rampDown.AddComponent<BoxCollider>();
            b2.size = new Vector3(width, 0.15f, span * 0.55f);
        }

        private static void Create3DBoardwalk(Transform parent, string name, Vector3 pos, Vector3 size, Material timberMat, Material pilingMat, bool addRailing)
        {
            GameObject bw = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bw.name = name;
            bw.transform.SetParent(parent, false);
            bw.transform.position = pos;
            bw.transform.localScale = size;
            bw.GetComponent<Renderer>().sharedMaterial = timberMat;

            // Cọc gỗ đóng ven mép nước
            int pileCount = Mathf.Max(2, Mathf.RoundToInt(size.x / 4.0f));
            for (int i = 0; i <= pileCount; i++)
            {
                float x = pos.x - size.x * 0.5f + (size.x / pileCount) * i;
                GameObject pile = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pile.name = $"Pile_{i}";
                pile.transform.SetParent(bw.transform, false);
                pile.transform.position = new Vector3(x, pos.y - 0.6f, pos.z - size.z * 0.45f);
                pile.transform.localScale = new Vector3(0.20f, 0.8f, 0.20f);
                pile.GetComponent<Renderer>().sharedMaterial = pilingMat;
            }

            if (addRailing)
            {
                GameObject rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rail.name = "WaterSide_Rail";
                rail.transform.SetParent(bw.transform, false);
                rail.transform.position = new Vector3(pos.x, pos.y + 0.45f, pos.z - size.z * 0.48f);
                rail.transform.localScale = new Vector3(size.x, 0.08f, 0.08f);
                rail.GetComponent<Renderer>().sharedMaterial = timberMat;
                UnityEngine.Object.DestroyImmediate(rail.GetComponent<Collider>());
            }
        }

        private static void Create3DWaterBuffalo(Transform parent, Vector3 pos, float yaw, Material skinMat, Material hornMat)
        {
            GameObject buffalo = new GameObject("WaterBuffalo_3D");
            buffalo.transform.SetParent(parent, false);
            buffalo.transform.position = pos;
            buffalo.transform.rotation = Quaternion.Euler(0, yaw, 0);

            // Thân trâu mộc mạc
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.name = "Torso";
            body.transform.SetParent(buffalo.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            body.transform.localScale = new Vector3(1.2f, 1.0f, 2.0f);
            body.GetComponent<Renderer>().sharedMaterial = skinMat;

            // Đầu trâu
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            head.name = "Head";
            head.transform.SetParent(buffalo.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.15f, 1.15f);
            head.transform.localScale = new Vector3(0.55f, 0.50f, 0.75f);
            head.GetComponent<Renderer>().sharedMaterial = skinMat;

            // Hai sừng cong hình bán nguyệt đặc trưng trâu nước Việt Nam
            for (int s = -1; s <= 1; s += 2)
            {
                GameObject horn = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                horn.name = $"Horn_{(s < 0 ? "L" : "R")}";
                horn.transform.SetParent(head.transform, false);
                horn.transform.localPosition = new Vector3(s * 0.45f, 0.35f, -0.1f);
                horn.transform.localRotation = Quaternion.Euler(25f, s * 45f, s * 60f);
                horn.transform.localScale = new Vector3(0.08f, 0.45f, 0.08f);
                horn.GetComponent<Renderer>().sharedMaterial = hornMat;
                UnityEngine.Object.DestroyImmediate(horn.GetComponent<Collider>());
            }

            // 4 Chân trâu
            for (int x = -1; x <= 1; x += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    leg.name = $"Leg_{x}_{z}";
                    leg.transform.SetParent(buffalo.transform, false);
                    leg.transform.localPosition = new Vector3(x * 0.42f, 0.45f, z * 0.65f);
                    leg.transform.localScale = new Vector3(0.18f, 0.45f, 0.18f);
                    leg.GetComponent<Renderer>().sharedMaterial = skinMat;
                }
            }
        }

        private static void Create3DScarecrow(Transform parent, Vector3 pos, Material timberMat, Material strawMat, Material hatMat)
        {
            GameObject sc = new GameObject("Scarecrow_3D");
            sc.transform.SetParent(parent, false);
            sc.transform.position = pos;

            // Cột đứng
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "MainPole";
            pole.transform.SetParent(sc.transform, false);
            pole.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            pole.transform.localScale = new Vector3(0.08f, 1.1f, 0.08f);
            pole.GetComponent<Renderer>().sharedMaterial = timberMat;

            // Xà ngang dang tay
            GameObject cross = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cross.name = "CrossArm";
            cross.transform.SetParent(sc.transform, false);
            cross.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            cross.transform.localRotation = Quaternion.Euler(0, 0, 90f);
            cross.transform.localScale = new Vector3(0.06f, 0.9f, 0.06f);
            cross.GetComponent<Renderer>().sharedMaterial = timberMat;

            // Thân rơm cuộn
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "StrawTorso";
            body.transform.SetParent(sc.transform, false);
            body.transform.localPosition = new Vector3(0f, 1.35f, 0f);
            body.transform.localScale = new Vector3(0.35f, 0.45f, 0.35f);
            body.GetComponent<Renderer>().sharedMaterial = strawMat;

            // Nón lá chóp nhọn trên đầu
            GameObject hat = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hat.name = "NonLa_Hat";
            hat.transform.SetParent(sc.transform, false);
            hat.transform.localPosition = new Vector3(0f, 2.15f, 0f);
            hat.transform.localRotation = Quaternion.Euler(8f, 15f, 0f);
            hat.transform.localScale = new Vector3(0.65f, 0.08f, 0.65f);
            hat.GetComponent<Renderer>().sharedMaterial = hatMat;
        }

        private static void Create3DWoodenBench(Transform parent, Vector3 pos, float length, Material timberMat)
        {
            GameObject bench = new GameObject("WoodenBench_3D");
            bench.transform.SetParent(parent, false);
            bench.transform.position = pos;

            // Mặt ghế
            GameObject seat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seat.name = "Seat";
            seat.transform.SetParent(bench.transform, false);
            seat.transform.localPosition = new Vector3(0f, 0.42f, 0f);
            seat.transform.localScale = new Vector3(length, 0.08f, 0.55f);
            seat.GetComponent<Renderer>().sharedMaterial = timberMat;

            // Hai chân ghế
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leg.name = $"Leg_{(side < 0 ? "L" : "R")}";
                leg.transform.SetParent(bench.transform, false);
                leg.transform.localPosition = new Vector3(side * (length * 0.4f), 0.20f, 0f);
                leg.transform.localScale = new Vector3(0.12f, 0.40f, 0.45f);
                leg.GetComponent<Renderer>().sharedMaterial = timberMat;
            }
        }

        private static void Create3DFlagpole(Transform parent, Vector3 pos, Material bambooMat)
        {
            GameObject flagRoot = new GameObject("Flagpole_3D");
            flagRoot.transform.SetParent(parent, false);
            flagRoot.transform.position = pos;

            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Bamboo_Pole";
            pole.transform.SetParent(flagRoot.transform, false);
            pole.transform.localPosition = new Vector3(0f, 2.0f, 0f);
            pole.transform.localScale = new Vector3(0.06f, 2.0f, 0.06f);
            pole.GetComponent<Renderer>().sharedMaterial = bambooMat;

            // Lá cờ đỏ sao vàng nhỏ
            Material flagMat = GetOrCreateMaterial("M_Flag_Red", new Color(0.90f, 0.15f, 0.15f), 0.1f);
            GameObject flagQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            flagQuad.name = "Flag_Banner";
            flagQuad.transform.SetParent(flagRoot.transform, false);
            flagQuad.transform.localPosition = new Vector3(0.35f, 3.6f, 0f);
            flagQuad.transform.localScale = new Vector3(0.70f, 0.45f, 1f);
            flagQuad.GetComponent<Renderer>().sharedMaterial = flagMat;
            UnityEngine.Object.DestroyImmediate(flagQuad.GetComponent<Collider>());
        }

        private static void CreateChickenFlock3D(Transform parent, Vector3 center)
        {
            Material chickenMat = GetOrCreateMaterial("M_Chicken_Yellow", new Color(0.95f, 0.85f, 0.35f), 0.1f);
            Vector3[] spots = new Vector3[]
            {
                center + new Vector3(-0.8f, 0.08f, 0.5f),
                center + new Vector3(0.4f, 0.08f, -0.6f),
                center + new Vector3(1.1f, 0.08f, 0.8f)
            };

            for (int i = 0; i < spots.Length; i++)
            {
                GameObject ch = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                ch.name = $"Chicken_{i}";
                ch.transform.SetParent(parent, false);
                ch.transform.position = spots[i];
                ch.transform.localScale = new Vector3(0.22f, 0.18f, 0.28f);
                ch.GetComponent<Renderer>().sharedMaterial = chickenMat;
                UnityEngine.Object.DestroyImmediate(ch.GetComponent<Collider>());
            }
        }

        private static void CreateBambooFenceSegment(Transform parent, Vector3 start, Vector3 end, Material bambooMat)
        {
            GameObject fence = new GameObject("BambooFence_Seg");
            fence.transform.SetParent(parent, false);

            float len = Vector3.Distance(start, end);
            int pickets = Mathf.Max(2, Mathf.RoundToInt(len / 0.8f));

            for (int i = 0; i <= pickets; i++)
            {
                float t = (float)i / pickets;
                Vector3 p = Vector3.Lerp(start, end, t);

                GameObject picket = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                picket.name = $"Picket_{i}";
                picket.transform.SetParent(fence.transform, false);
                picket.transform.position = p + Vector3.up * 0.45f;
                picket.transform.localScale = new Vector3(0.06f, 0.45f, 0.06f);
                picket.GetComponent<Renderer>().sharedMaterial = bambooMat;
            }

            // Thanh ngang
            GameObject rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rail.name = "CrossRail";
            rail.transform.SetParent(fence.transform, false);
            rail.transform.position = (start + end) * 0.5f + Vector3.up * 0.55f;
            rail.transform.rotation = Quaternion.LookRotation(end - start);
            rail.transform.localScale = new Vector3(0.04f, 0.04f, len);
            rail.GetComponent<Renderer>().sharedMaterial = bambooMat;
            UnityEngine.Object.DestroyImmediate(rail.GetComponent<Collider>());
        }

        private static void CreateBambooFencePerimeter(Transform parent, Vector3 center, float sizeX, float sizeZ, Material bambooMat, bool openNorth, bool openSouth, bool openEast, bool openWest)
        {
            float hx = sizeX * 0.5f;
            float hz = sizeZ * 0.5f;

            if (!openNorth) CreateBambooFenceSegment(parent, center + new Vector3(-hx, 0, hz), center + new Vector3(hx, 0, hz), bambooMat);
            if (!openSouth) CreateBambooFenceSegment(parent, center + new Vector3(-hx, 0, -hz), center + new Vector3(hx, 0, -hz), bambooMat);
            if (!openEast) CreateBambooFenceSegment(parent, center + new Vector3(hx, 0, -hz), center + new Vector3(hx, 0, hz), bambooMat);
            if (!openWest) CreateBambooFenceSegment(parent, center + new Vector3(-hx, 0, -hz), center + new Vector3(-hx, 0, hz), bambooMat);
        }

        private static void Create3DInteractionTrigger(Transform parent, string name, Vector3 pos, string prompt, float radius)
        {
            GameObject triggerObj = new GameObject(name);
            triggerObj.transform.SetParent(parent, false);
            triggerObj.transform.position = pos;

            var col = triggerObj.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = radius;

            var interactable = triggerObj.AddComponent<InteractableObject3D>();
            interactable.objectTitle = name;
            interactable.promptMessage = prompt;
            interactable.actionMessage = prompt;
            interactable.interactionRadius = radius;
        }

        private static GameObject PlaceStiltedHouse3D(GameObject prefab, Transform parent, Vector3 basePos, Vector3 scale, float yaw = 0f, bool isWood = false)
        {
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            float yOffset = isWood ? (0.67f * scale.y) : (0.95f * scale.y);
            Vector3 finalPos = new Vector3(basePos.x, basePos.y + yOffset, basePos.z);
            inst.transform.position = finalPos;
            inst.transform.rotation = Quaternion.Euler(0, yaw, 0);
            inst.transform.localScale = scale;
            if (inst.transform.childCount > 0)
            {
                for (int i = 0; i < inst.transform.childCount; i++)
                {
                    var child = inst.transform.GetChild(i);
                    if (Mathf.Abs(child.localPosition.x) > 5f || Mathf.Abs(child.localPosition.z) > 5f)
                    {
                        child.localPosition = new Vector3(0, child.localPosition.y, 0);
                    }
                }
            }
            var boxCol = inst.GetComponent<BoxCollider>();
            if (boxCol == null)
            {
                boxCol = inst.AddComponent<BoxCollider>();
                if (isWood)
                {
                    boxCol.center = new Vector3(0f, 0f, 0f);
                    boxCol.size = new Vector3(1.1f, 1.4f, 1.9f);
                }
                else
                {
                    boxCol.center = new Vector3(0f, 0f, 0f);
                    boxCol.size = new Vector3(1.4f, 1.9f, 1.7f);
                }
            }
            return inst;
        }

        private static void Create3DDockShelter(Transform parent, Vector3 pos, Material timberMat, Material thatchMat, Material benchMat)
        {
            GameObject shelter = new GameObject("Dock_WaitingShelter_3D");
            shelter.transform.SetParent(parent, false);
            shelter.transform.position = pos;

            // Sàn gỗ nâng cao trên cọc
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Shelter_Floor";
            floor.transform.SetParent(shelter.transform, false);
            floor.transform.localPosition = new Vector3(0f, 0.12f, 0f);
            floor.transform.localScale = new Vector3(3.6f, 0.22f, 3.2f);
            floor.GetComponent<Renderer>().sharedMaterial = timberMat;

            // 4 Cọc gỗ nâng sàn & đỡ mái (Stilt Posts)
            for (int x = -1; x <= 1; x += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    post.name = $"Post_{x}_{z}";
                    post.transform.SetParent(shelter.transform, false);
                    post.transform.localPosition = new Vector3(x * 1.6f, 1.30f, z * 1.4f);
                    post.transform.localScale = new Vector3(0.14f, 1.30f, 0.14f);
                    post.GetComponent<Renderer>().sharedMaterial = timberMat;
                }
            }

            // Mái lá dốc đôi (Gable Roof)
            GameObject roofLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roofLeft.name = "Roof_Slope_North";
            roofLeft.transform.SetParent(shelter.transform, false);
            roofLeft.transform.localPosition = new Vector3(0f, 2.75f, 0.75f);
            roofLeft.transform.localRotation = Quaternion.Euler(24f, 0f, 0f);
            roofLeft.transform.localScale = new Vector3(4.0f, 0.16f, 2.0f);
            roofLeft.GetComponent<Renderer>().sharedMaterial = thatchMat;

            GameObject roofRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roofRight.name = "Roof_Slope_South";
            roofRight.transform.SetParent(shelter.transform, false);
            roofRight.transform.localPosition = new Vector3(0f, 2.75f, -0.75f);
            roofRight.transform.localRotation = Quaternion.Euler(-24f, 0f, 0f);
            roofRight.transform.localScale = new Vector3(4.0f, 0.16f, 2.0f);
            roofRight.GetComponent<Renderer>().sharedMaterial = thatchMat;

            // Đòn nóc (Ridge beam)
            GameObject ridge = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ridge.name = "Roof_Ridge";
            ridge.transform.SetParent(shelter.transform, false);
            ridge.transform.localPosition = new Vector3(0f, 3.12f, 0f);
            ridge.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            ridge.transform.localScale = new Vector3(0.12f, 2.1f, 0.12f);
            ridge.GetComponent<Renderer>().sharedMaterial = timberMat;

            // Ghế băng gỗ dài bên trong cho khách đợi xuồng
            GameObject bench = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bench.name = "Waiting_Bench";
            bench.transform.SetParent(shelter.transform, false);
            bench.transform.localPosition = new Vector3(-1.1f, 0.45f, 0f);
            bench.transform.localScale = new Vector3(0.55f, 0.35f, 2.2f);
            bench.GetComponent<Renderer>().sharedMaterial = benchMat;
        }

        public static void SetShot(string shotName)
        {
            var p = GameObject.FindWithTag("Player");
            var cam = Camera.main;
            if (p == null || cam == null) return;

            var idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/TripoModels/Boy_Character/Hero_Loco_Idle_Loop_IP.anim");
            if (idleClip != null) idleClip.SampleAnimation(p, 0f);

            switch (shotName)
            {
                case "01_NhaAn":
                    p.transform.position = new Vector3(0.5f, 0.45f, 13.0f);
                    p.transform.rotation = Quaternion.Euler(0, 0f, 0);
                    cam.transform.position = new Vector3(0.75f, 1.85f, 9.4f);
                    cam.transform.rotation = Quaternion.Euler(14f, 0f, 0f);
                    break;
                case "02_KhuLayNuoc":
                    p.transform.position = new Vector3(9.2f, 0.10f, 4.2f);
                    p.transform.rotation = Quaternion.Euler(0, 45f, 0);
                    cam.transform.position = new Vector3(6.8f, 1.6f, 1.8f);
                    cam.transform.rotation = Quaternion.Euler(14f, 45f, 0f);
                    break;
                case "03_CauGo":
                    p.transform.position = new Vector3(13.5f, 0.08f, -2.0f);
                    p.transform.rotation = Quaternion.Euler(0, 90f, 0);
                    cam.transform.position = new Vector3(10.2f, 1.6f, -2.2f);
                    cam.transform.rotation = Quaternion.Euler(12f, 90f, 0f);
                    break;
                case "04_SanLang":
                    p.transform.position = new Vector3(0.5f, 0.02f, 1.0f);
                    p.transform.rotation = Quaternion.Euler(0, -75f, 0);
                    cam.transform.position = new Vector3(3.6f, 1.6f, 1.8f);
                    cam.transform.rotation = Quaternion.Euler(14f, -75f, 0f);
                    break;
                case "05_VuonCay":
                    p.transform.position = new Vector3(-15.5f, 0.20f, 3.5f);
                    p.transform.rotation = Quaternion.Euler(0, 130f, 0);
                    cam.transform.position = new Vector3(-13.2f, 1.6f, 5.8f);
                    cam.transform.rotation = Quaternion.Euler(12f, 130f, 0f);
                    break;
                case "06_CanhDong":
                    p.transform.position = new Vector3(23.5f, 0.12f, 8.5f);
                    p.transform.rotation = Quaternion.Euler(0, 30f, 0);
                    cam.transform.position = new Vector3(21.8f, 1.6f, 5.5f);
                    cam.transform.rotation = Quaternion.Euler(12f, 30f, 0f);
                    break;
                case "07_BoSong":
                    p.transform.position = new Vector3(-6.0f, 0.12f, -8.0f);
                    p.transform.rotation = Quaternion.Euler(0, 180f, 0);
                    cam.transform.position = new Vector3(-6.0f, 1.6f, -4.5f);
                    cam.transform.rotation = Quaternion.Euler(14f, 180f, 0f);
                    break;
                case "08_BenXuong":
                    p.transform.position = new Vector3(6.0f, 0.05f, -12.0f);
                    p.transform.rotation = Quaternion.Euler(0, 180f, 0);
                    cam.transform.position = new Vector3(6.0f, 1.6f, -8.5f);
                    cam.transform.rotation = Quaternion.Euler(14f, 180f, 0f);
                    break;
            }
            if (idleClip != null) idleClip.SampleAnimation(p, 0f);
        }
    }
}
