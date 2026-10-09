using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using HoaVaMau.Gameplay;
using HoaVaMau.Test3D;

public static class Canh1SceneBuilder
{
    private const string ScenePath = "Assets/Scenes/canh1.unity";
    private const string SpritesDir = "Assets/Textures/Canh1_Sprites";
    private const string ShipSpritePath = "Assets/Textures/Canh1_Sprites/TransportShip_1954_Card.png";
    private const string BoyModelPath = "Assets/TripoModels/Boy_Character/Boy_Character.fbx";
    private const string BoyAnimCtrlPath = "Assets/TripoModels/Boy_Character/Boy_AnimatorController.controller";
    private const string WaterMatPath = "Assets/Materials/M_StylizedWater_Sunset.mat";
    private const string SkyMatPath = "Assets/Materials/M_StylizedSky_Sunset.mat";
    private const string SunMatPath = "Assets/Materials/M_StylizedSun_Sunset.mat";
    private const string WoodDeckMatPath = "Assets/Materials/Thung Go Vuong.mat";
    private const string FallbackTimberMat = "Assets/Materials/M_Boat_Wood.mat";
    private const string CrateModelPath = "Assets/Models/Thùng gỗ.fbx";

    [MenuItem("HoaVaMau/Build Canh1 Scene")]
    public static void BuildCanh1Scene()
    {
        Debug.Log("[Canh1SceneBuilder] Bắt đầu xây dựng scene Cà Mau 1954: canh1.unity...");

        ConfigureSpriteImporters();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject root = new GameObject("CAMAU_REGROUPMENT_1954");
        GameObject goCamera = CreateChild(root, "Camera");
        GameObject goLighting = CreateChild(root, "Lighting");
        GameObject goGameplay = CreateChild(root, "Gameplay");
        GameObject goDock = CreateChild(root, "Dock");
        GameObject goTransportShip = CreateChild(root, "TransportShip");
        GameObject goTransferBoats = CreateChild(root, "TransferBoats");
        GameObject goRiversideVillage = CreateChild(root, "RiversideVillage");
        GameObject goCivilianNPCs = CreateChild(root, "CivilianNPCs");
        GameObject goSoldierNPCs = CreateChild(root, "SoldierNPCs");
        GameObject goHistoricalProps = CreateChild(root, "HistoricalProps");
        GameObject goVegetation = CreateChild(root, "Vegetation");
        GameObject goVegForeground = CreateChild(goVegetation, "Foreground");
        GameObject goVegMidground = CreateChild(goVegetation, "Midground");
        GameObject goVegBackground = CreateChild(goVegetation, "Background");
        GameObject goWater = CreateChild(root, "Water");
        GameObject goSky = CreateChild(root, "Sky");
        GameObject goVFX = CreateChild(root, "VFX");

        // 1. Ánh sáng & Bầu trời hoàng hôn rực rỡ
        SetupLighting(goLighting);
        SetupSkyAndSun(goSky);
        SetupWater(goWater);

        // 2. Con tàu vận tải thép 1954 (Focal point đồ sộ ở hậu cảnh, thân tàu chìm nhẹ vào mặt sông)
        SetupTransportShip(goTransportShip);

        // 3. Cầu tàu gỗ chơi được (Midground Walkway chuẩn 2.5D, mặt ván vẽ tay nghệ thuật)
        SetupDockAndPlatforms(goDock, goGameplay);

        // 4. Nhân vật chính: Cậu bé Minh (Boy_Character từ TripoModels)
        GameObject playerGo = SetupPlayer(goGameplay);

        // 5. Camera Điện ảnh 2.5D (Planet of Lana Style)
        SetupCamera(goCamera, playerGo);

        // 6. Thuyền ba lá chuyển tiếp trên mặt sông (Sampans dập dềnh sóng nước)
        SetupTransferBoats(goTransferBoats);

        // 7. Làng nhà sàn ven sông phía xa
        SetupRiversideVillage(goRiversideVillage);

        // 8. Đồng bào & Bộ đội chào tiễn biệt
        SetupNPCs(goCivilianNPCs, goSoldierNPCs);

        // 9. Đạo cụ lịch sử & Chướng ngại vật platform
        SetupProps(goHistoricalProps);

        // 10. Thảm thực vật viền khung hình điện ảnh (Foreground Framing chuẩn bố cục)
        SetupVegetation(goVegForeground, goVegMidground, goVegBackground);

        // 11. Bụi vàng hoàng hôn lơ lửng trong không khí (Golden Atmospheric Dust)
        SetupAtmosphericVFX(goVFX);

        string dir = Path.GetDirectoryName(ScenePath);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log("[Canh1SceneBuilder] Đã hoàn thành và lưu scene: " + ScenePath);

        // Chụp ảnh render góc nhìn Camera chính để kiểm tra thị giác
        CaptureScreenshot("Screenshots/canh1_main_camera.png");
    }

    private static GameObject CreateChild(GameObject parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent.transform, false);
        return child;
    }

    private static void ConfigureSpriteImporters()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { SpritesDir });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                bool dirty = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    dirty = true;
                }
                if (!importer.alphaIsTransparency)
                {
                    importer.alphaIsTransparency = true;
                    dirty = true;
                }
                if (importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = false;
                    dirty = true;
                }
                if (importer.spritePixelsPerUnit != 100f)
                {
                    importer.spritePixelsPerUnit = 100f;
                    dirty = true;
                }
                if (dirty) importer.SaveAndReimport();
            }
        }
        AssetDatabase.Refresh();
    }

    private static void SetupLighting(GameObject parent)
    {
        GameObject sunGo = new GameObject("Sun_DirectionalLight");
        sunGo.transform.SetParent(parent.transform, false);
        sunGo.transform.rotation = Quaternion.Euler(14f, 195f, 0f);

        Light sun = sunGo.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1.0f, 0.74f, 0.46f);
        sun.intensity = 1.35f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.72f;

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.96f, 0.70f, 0.48f);
        RenderSettings.ambientEquatorColor = new Color(0.50f, 0.44f, 0.52f);
        RenderSettings.ambientGroundColor = new Color(0.20f, 0.22f, 0.26f);

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.90f, 0.66f, 0.46f, 1.0f);
        RenderSettings.fogDensity = 0.0022f;
    }

    private static void SetupSkyAndSun(GameObject parent)
    {
        Material skyMat = AssetDatabase.LoadAssetAtPath<Material>(SkyMatPath);
        if (skyMat != null) RenderSettings.skybox = skyMat;

        GameObject skyQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        skyQuad.name = "Sky_Backdrop";
        skyQuad.transform.SetParent(parent.transform, false);
        skyQuad.transform.position = new Vector3(2f, 20f, 120f);
        skyQuad.transform.localScale = new Vector3(280f, 80f, 1f);
        if (skyMat != null) skyQuad.GetComponent<MeshRenderer>().sharedMaterial = skyMat;
        Object.DestroyImmediate(skyQuad.GetComponent<Collider>());

        Material sunMat = AssetDatabase.LoadAssetAtPath<Material>(SunMatPath);
        GameObject sunDisc = GameObject.CreatePrimitive(PrimitiveType.Quad);
        sunDisc.name = "Sun_Disc";
        sunDisc.transform.SetParent(parent.transform, false);
        sunDisc.transform.position = new Vector3(5.5f, 8.8f, 115f);
        sunDisc.transform.localScale = new Vector3(16f, 16f, 1f);
        if (sunMat != null) sunDisc.GetComponent<MeshRenderer>().sharedMaterial = sunMat;
        Object.DestroyImmediate(sunDisc.GetComponent<Collider>());
    }

    private static void SetupWater(GameObject parent)
    {
        Material waterMat = AssetDatabase.LoadAssetAtPath<Material>(WaterMatPath);

        GameObject waterPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        waterPlane.name = "Stylized_RiverWater";
        waterPlane.transform.SetParent(parent.transform, false);
        waterPlane.transform.position = new Vector3(0f, -0.45f, 35f);
        waterPlane.transform.localScale = new Vector3(36f, 1f, 18f);

        if (waterMat != null) waterPlane.GetComponent<MeshRenderer>().sharedMaterial = waterMat;
        Object.DestroyImmediate(waterPlane.GetComponent<Collider>());
    }

    private static void SetupTransportShip(GameObject parent)
    {
        Sprite shipSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ShipSpritePath);
        if (shipSprite != null)
        {
            GameObject ship = new GameObject("Large_Transport_Ship_1954");
            ship.transform.SetParent(parent.transform, false);

            // Vị trí hậu cảnh: Z = 54.0m, X = 5.5m (giữa-phải khung hình), Y = 5.4m
            // Chiều cao sprite 6.0m -> scale 2.5x = 15.0m (bán kính 7.5m)
            // Đáy lườn tàu ở Y = 5.4 - 7.5 = -2.1m (chìm vững chắc vào mặt sông Y = -0.45m)
            // Mạn tàu và buồng lái hòa quyện cùng ánh nắng vàng hoàng hôn
            ship.transform.position = new Vector3(5.5f, 5.4f, 54.0f);
            ship.transform.localScale = new Vector3(2.5f, 2.5f, 1.0f);

            SpriteRenderer sr = ship.AddComponent<SpriteRenderer>();
            sr.sprite = shipSprite;
            sr.color = new Color(0.96f, 0.92f, 0.86f, 1.0f);
        }
    }

    private static void SetupDockAndPlatforms(GameObject goDock, GameObject goGameplay)
    {
        GameObject goPlatforms = CreateChild(goGameplay, "Platforms");
        Material deckMat = AssetDatabase.LoadAssetAtPath<Material>(WoodDeckMatPath);
        if (deckMat == null) deckMat = AssetDatabase.LoadAssetAtPath<Material>(FallbackTimberMat);
        Sprite pierSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesDir + "/BG_Prop_02.png");

        // 1. Walkable 3D Physics Colliders
        // Đoạn 1: Lối vào và khu tập trung gia đình (X = -17 đến +5, Y = 0.8)
        CreatePlatformBox(goPlatforms, new Vector3(-6.0f, 0.6f, 0f), new Vector3(22f, 0.4f, 2.8f));

        // Đoạn 2: Gờ nâng cao vượt kiện hàng (X = +5.5 đến +10.5, Y = 1.3)
        CreatePlatformBox(goPlatforms, new Vector3(8.0f, 1.1f, 0f), new Vector3(5.0f, 0.4f, 2.8f));

        // Đoạn 3: Đảo kiện hàng vượt sông (X = 12.0 đến 14.5, Y = 1.0, gap 1.5m)
        CreatePlatformBox(goPlatforms, new Vector3(13.25f, 0.8f, 0f), new Vector3(2.5f, 0.4f, 2.8f));

        // Đoạn 4: Mũi bến ngắm tàu xa (X = 16 đến 24, Y = 0.8)
        CreatePlatformBox(goPlatforms, new Vector3(20.0f, 0.6f, 0f), new Vector3(8.0f, 0.4f, 2.8f));

        // 2. Visual Wooden Deck Art Cards (Mặt ván vẽ tay nghệ thuật)
        if (pierSprite != null)
        {
            CreatePierArtCard(goDock, "Pier_Art_Entrance", new Vector3(-14.0f, 0.35f, -0.1f), new Vector3(1.6f, 1.6f, 1f), pierSprite);
            CreatePierArtCard(goDock, "Pier_Art_Mid1", new Vector3(-6.5f, 0.35f, -0.1f), new Vector3(1.6f, 1.6f, 1f), pierSprite);
            CreatePierArtCard(goDock, "Pier_Art_Mid2", new Vector3(1.0f, 0.35f, -0.1f), new Vector3(1.6f, 1.6f, 1f), pierSprite);
            CreatePierArtCard(goDock, "Pier_Art_Elevated", new Vector3(8.0f, 0.85f, -0.1f), new Vector3(1.6f, 1.6f, 1f), pierSprite);
            CreatePierArtCard(goDock, "Pier_Art_StepIsland", new Vector3(13.25f, 0.55f, -0.1f), new Vector3(1.0f, 1.6f, 1f), pierSprite);
            CreatePierArtCard(goDock, "Pier_Art_ViewPier", new Vector3(19.5f, 0.35f, -0.1f), new Vector3(1.6f, 1.6f, 1f), pierSprite);
        }

        // 3. Khối dầm gỗ 3D lùi về phía sau (Z = 0.8f, tạo độ dày sàn và bóng đổ thực tế)
        CreateDockBeam3D(goDock, new Vector3(-6.0f, 0.6f, 0.8f), new Vector3(22f, 0.38f, 1.6f), deckMat);
        CreateDockBeam3D(goDock, new Vector3(8.0f, 1.1f, 0.8f), new Vector3(5.0f, 0.38f, 1.6f), deckMat);
        CreateDockBeam3D(goDock, new Vector3(13.25f, 0.8f, 0.8f), new Vector3(2.5f, 0.38f, 1.6f), deckMat);
        CreateDockBeam3D(goDock, new Vector3(20.0f, 0.6f, 0.8f), new Vector3(8.0f, 0.38f, 1.6f), deckMat);

        // 4. Nhà chòi bến đợi (Open Pavilion Shelter) ở đầu bến tàu bên trái
        Sprite shelterSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesDir + "/BG_Prop_05.png");
        CreateCard(goDock, "Dock_Waiting_Shelter", new Vector3(-15.5f, 2.05f, 0.45f), new Vector3(1.4f, 1.4f, 1f), shelterSprite);
    }

    private static void CreatePlatformBox(GameObject parent, Vector3 pos, Vector3 size)
    {
        GameObject platform = new GameObject("Walkable_Platform");
        platform.transform.SetParent(parent.transform, false);
        platform.transform.position = pos;
        BoxCollider box = platform.AddComponent<BoxCollider>();
        box.size = size;
    }

    private static void CreateDockBeam3D(GameObject parent, Vector3 pos, Vector3 size, Material mat)
    {
        GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
        beam.name = "Dock_Timber_Beam";
        beam.transform.SetParent(parent.transform, false);
        beam.transform.position = pos;
        beam.transform.localScale = size;
        if (mat != null) beam.GetComponent<MeshRenderer>().sharedMaterial = mat;
        Object.DestroyImmediate(beam.GetComponent<Collider>());
    }

    private static void CreatePierArtCard(GameObject parent, string name, Vector3 pos, Vector3 scale, Sprite sprite)
    {
        GameObject card = new GameObject(name);
        card.transform.SetParent(parent.transform, false);
        card.transform.position = pos;
        card.transform.localScale = scale;
        SpriteRenderer sr = card.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = new Color(0.98f, 0.94f, 0.88f, 1.0f);
    }

    private static GameObject SetupPlayer(GameObject goGameplay)
    {
        GameObject goSpawn = CreateChild(goGameplay, "PlayerSpawn");
        goSpawn.transform.position = new Vector3(-12.8f, 1.0f, 0f);

        GameObject boyModelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BoyModelPath);
        GameObject playerInstance = null;

        if (boyModelPrefab != null)
        {
            playerInstance = (GameObject)PrefabUtility.InstantiatePrefab(boyModelPrefab, goGameplay.transform);
            playerInstance.name = "Player_Minh";
            playerInstance.tag = "Player";
            playerInstance.transform.position = new Vector3(-12.8f, 0.85f, 0f);
            playerInstance.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            playerInstance.transform.localScale = Vector3.one * 1.0f;

            CharacterController cc = playerInstance.GetComponent<CharacterController>();
            if (cc == null) cc = playerInstance.AddComponent<CharacterController>();
            cc.height = 1.5f;
            cc.radius = 0.30f;
            cc.center = new Vector3(0f, 0.75f, 0f);
            cc.stepOffset = 0.35f;

            Animator anim = playerInstance.GetComponent<Animator>();
            if (anim != null)
            {
                var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(BoyAnimCtrlPath);
                if (ctrl != null) anim.runtimeAnimatorController = ctrl;
            }

            PlayerController pc = playerInstance.GetComponent<PlayerController>();
            if (pc == null) pc = playerInstance.AddComponent<PlayerController>();
        }

        return playerInstance;
    }

    private static void SetupCamera(GameObject parent, GameObject targetPlayer)
    {
        GameObject camGo = new GameObject("Main Camera");
        camGo.transform.SetParent(parent.transform, false);
        camGo.transform.position = new Vector3(-10.0f, 2.2f, -13.0f);
        camGo.transform.rotation = Quaternion.identity;
        camGo.tag = "MainCamera";

        Camera cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = 36f;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 300f;
        cam.clearFlags = CameraClearFlags.Skybox;

        camGo.AddComponent<AudioListener>();

        Cinematic2DCamera cineCam = camGo.AddComponent<Cinematic2DCamera>();
        if (targetPlayer != null)
        {
            cineCam.Target = targetPlayer.transform;
            cineCam.ConfigureLevelBounds(-10.0f, 18.0f, 1.2f, true);
        }
    }

    private static void SetupTransferBoats(GameObject parent)
    {
        Sprite sampanCovered = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesDir + "/BG_Prop_01.png");
        Sprite sampanOpenHat = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesDir + "/BG_Prop_03.png");
        Sprite waterLily = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesDir + "/FG_Mangrove_23.png");

        // 1. Xuồng ba lá neo cạnh chân cầu tàu (Tiền cảnh bến sông)
        CreateBoat(parent, "Sampan_Moored_Dock", new Vector3(-6.5f, -0.32f, 2.8f), new Vector3(0.9f, 0.9f, 1f), sampanCovered, true);

        // 2. Xuồng mui trần với nón lá dập dềnh giữa dòng
        CreateBoat(parent, "Sampan_MidRiver_Hat", new Vector3(-1.0f, -0.28f, 14.0f), new Vector3(0.8f, 0.8f, 1f), sampanOpenHat, true);

        // 3. Xuồng trung chuyển hướng về phía tàu lớn
        CreateBoat(parent, "Sampan_Transfer_Transit", new Vector3(5.0f, -0.25f, 24.0f), new Vector3(0.65f, 0.65f, 1f), sampanCovered, true);

        // 4. Xuồng nhỏ cập mạn tàu thép xa
        CreateBoat(parent, "Sampan_At_Ship_Ladder", new Vector3(10.5f, -0.22f, 40.0f), new Vector3(0.48f, 0.48f, 1f), sampanOpenHat, true);

        // 5. Bèo lục bình hoa tím trôi dạt ven bến
        if (waterLily != null)
        {
            CreateCard(parent, "WaterLily_Cluster_1", new Vector3(-8.5f, -0.42f, 1.8f), new Vector3(0.6f, 0.6f, 1f), waterLily);
            CreateCard(parent, "WaterLily_Cluster_2", new Vector3(-3.0f, -0.42f, 2.2f), new Vector3(0.5f, 0.5f, 1f), waterLily);
            CreateCard(parent, "WaterLily_Cluster_3", new Vector3(6.5f, -0.42f, 2.5f), new Vector3(0.7f, 0.7f, 1f), waterLily);
        }
    }

    private static void CreateBoat(GameObject parent, string name, Vector3 pos, Vector3 scale, Sprite sprite, bool addFloat)
    {
        if (sprite == null) return;
        GameObject boat = new GameObject(name);
        boat.transform.SetParent(parent.transform, false);
        boat.transform.position = pos;
        boat.transform.localScale = scale;

        SpriteRenderer sr = boat.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;

        if (addFloat)
        {
            boat.AddComponent<FloatingBoat3D>();
        }
    }

    private static void SetupRiversideVillage(GameObject parent)
    {
        Sprite hutStilt = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesDir + "/BG_Prop_04.png");
        Sprite hutThatched = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesDir + "/BG_Prop_07.png");

        // Làng nhà sàn bờ sông bên trái
        CreateCard(parent, "Village_Hut_Left", new Vector3(-22.0f, 1.6f, 38.0f), new Vector3(1.8f, 1.8f, 1f), hutStilt);

        // Nhà đôi lợp lá bờ sông bên phải
        CreateCard(parent, "Village_Hut_Right", new Vector3(28.0f, 1.6f, 42.0f), new Vector3(1.8f, 1.8f, 1f), hutThatched);
    }

    private static void SetupNPCs(GameObject goCivilians, GameObject goSoldiers)
    {
        Sprite familyWithChild = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesDir + "/BG_Prop_23.png");
        Sprite motherChild = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesDir + "/BG_Prop_27.png");
        Sprite elderlyPerson = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesDir + "/BG_Prop_26.png");
        Sprite soldierDuffel = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesDir + "/BG_Prop_24.png");
        Sprite soldierSalute = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesDir + "/BG_Prop_21.png");
        Sprite familyWaving = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesDir + "/BG_Prop_25.png");

        // Nhóm 1: Gia đình bịn rịn chia tay ở khu chờ lối vào (đứng trên sàn Y = 0.8)
        CreateCard(goCivilians, "Civilian_Family_Group", new Vector3(-10.5f, 1.48f, 0.35f), new Vector3(1.15f, 1.15f, 1f), familyWithChild);
        CreateCard(goCivilians, "Mother_Holding_Child", new Vector3(-8.8f, 1.44f, 0.35f), new Vector3(1.15f, 1.15f, 1f), motherChild);
        CreateCard(goCivilians, "Elderly_Near_Luggage", new Vector3(-6.2f, 1.45f, 0.35f), new Vector3(1.15f, 1.15f, 1f), elderlyPerson);

        // Nhóm 2: Bộ đội mang ba lô cóc & Quân dân vẫy chào
        CreateCard(goSoldiers, "Soldier_With_Duffel", new Vector3(-2.0f, 1.48f, 0.35f), new Vector3(1.18f, 1.18f, 1f), soldierDuffel);
        CreateCard(goSoldiers, "Soldier_Saluting_Pier", new Vector3(2.2f, 1.52f, 0.35f), new Vector3(1.18f, 1.18f, 1f), soldierSalute);
        CreateCard(goCivilians, "Family_Waving_Farewell", new Vector3(4.0f, 1.48f, 0.35f), new Vector3(1.15f, 1.15f, 1f), familyWaving);
    }

    private static void SetupProps(GameObject parent)
    {
        GameObject cratePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CrateModelPath);
        Sprite crates2D = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesDir + "/BG_Prop_10.png");
        Sprite sacks2D = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesDir + "/BG_Prop_15.png");
        Sprite luggageBundle = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesDir + "/BG_Prop_12.png");

        // Đạo cụ 2D trên mặt cầu tàu (Xếp gọn gàng không che lối đi)
        CreateCard(parent, "Rice_Sacks_Shelter", new Vector3(-14.5f, 1.35f, 0.25f), new Vector3(1.3f, 1.3f, 1f), sacks2D);
        CreateCard(parent, "Luggage_Bundles_Pier", new Vector3(-13.8f, 1.30f, 0.25f), new Vector3(1.1f, 1.1f, 1f), luggageBundle);
        CreateCard(parent, "Crates_Stack_Pier", new Vector3(-7.5f, 1.38f, 0.25f), new Vector3(1.3f, 1.3f, 1f), crates2D);

        // Thùng gỗ 3D tạo chướng ngại vật nhảy leo trèo
        if (cratePrefab != null)
        {
            Create3DProp(parent, cratePrefab, "Cargo_Crate_StepObstacle", new Vector3(5.8f, 1.35f, 0f), Vector3.one * 0.85f);
            Create3DProp(parent, cratePrefab, "Cargo_Crate_IslandGap", new Vector3(13.25f, 1.4f, 0f), Vector3.one * 0.85f);
        }
    }

    private static void Create3DProp(GameObject parent, GameObject prefab, string name, Vector3 pos, Vector3 scale)
    {
        GameObject prop = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform);
        prop.name = name;
        prop.transform.position = pos;
        prop.transform.localScale = scale;
    }

    private static void SetupVegetation(GameObject goFG, GameObject goMG, GameObject goBG)
    {
        Sprite rootStilt = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesDir + "/FG_Mangrove_01.png");
        Sprite vinesHanging = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesDir + "/FG_Mangrove_10.png");
        Sprite nipaPalm = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesDir + "/FG_Mangrove_14.png");
        Sprite bushClean = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesDir + "/FG_Mangrove_07.png");
        Sprite mangroveTree = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesDir + "/FG_Mangrove_06.png");

        // Tiền cảnh viền khung điện ảnh (Foreground Framing):
        // Mép trái khung hình (Ẩn phần ngọn phẳng, chỉ đưa chùm rễ cong tự nhiên vào góc)
        CreateCard(goFG, "FG_Mangrove_Root_Left", new Vector3(-14.5f, 0.4f, -6.5f), new Vector3(1.8f, 1.8f, 1f), rootStilt);

        // Góc trên bên trái: Dây leo rủ xuống
        CreateCard(goFG, "FG_Vines_Top_Left", new Vector3(-12.2f, 4.2f, -6.5f), new Vector3(1.3f, 1.3f, 1f), vinesHanging);

        // Góc trên bên phải: Tán dừa nước
        CreateCard(goFG, "FG_NipaPalm_Top_Right", new Vector3(-6.8f, 4.3f, -6.5f), new Vector3(1.4f, 1.4f, 1f), nipaPalm);

        // Góc dưới mép trái: Bụi rễ cây đước
        CreateCard(goFG, "FG_Mangrove_Bush_BottomLeft", new Vector3(-13.2f, -0.6f, -6.5f), new Vector3(1.1f, 1.1f, 1f), bushClean);

        // Góc dưới mép phải: Bụi cây đước
        CreateCard(goFG, "FG_Mangrove_Bush_BottomRight", new Vector3(-6.8f, -0.7f, -6.5f), new Vector3(1.1f, 1.1f, 1f), bushClean);

        // Hậu cảnh: Rừng đước hai bên bờ sông xa
        if (mangroveTree != null)
        {
            CreateCard(goBG, "BG_Mangrove_Forest_Left", new Vector3(-28.0f, 3.5f, 45.0f), new Vector3(3.0f, 3.0f, 1f), mangroveTree);
            CreateCard(goBG, "BG_Mangrove_Forest_Right", new Vector3(34.0f, 3.5f, 48.0f), new Vector3(3.2f, 3.2f, 1f), mangroveTree);
        }
    }

    private static void SetupAtmosphericVFX(GameObject parent)
    {
        GameObject dustGo = new GameObject("Atmospheric_Golden_Dust");
        dustGo.transform.SetParent(parent.transform, false);
        dustGo.transform.position = new Vector3(-2.0f, 2.5f, 0f);

        ParticleSystem ps = dustGo.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.startLifetime = 7.0f;
        main.startSpeed = 0.25f;
        main.startSize = 0.06f;
        main.startColor = new Color(1.0f, 0.85f, 0.50f, 0.45f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 80;

        var emission = ps.emission;
        emission.rateOverTime = 10f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(25.0f, 5.0f, 12.0f);

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(0.1f, 0.25f);
        vel.y = new ParticleSystem.MinMaxCurve(-0.05f, 0.08f);
        vel.z = new ParticleSystem.MinMaxCurve(0.0f, 0.0f);

        ParticleSystemRenderer renderer = dustGo.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");
    }

    private static void CreateCard(GameObject parent, string name, Vector3 pos, Vector3 scale, Sprite sprite)
    {
        if (sprite == null) return;
        GameObject card = new GameObject(name);
        card.transform.SetParent(parent.transform, false);
        card.transform.position = pos;
        card.transform.localScale = scale;

        SpriteRenderer sr = card.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
    }

    public static void CaptureScreenshot(string path = "Screenshots/canh1_main_camera.png")
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[Canh1SceneBuilder] Không tìm thấy Camera.main để chụp ảnh.");
            return;
        }

        int w = 1920;
        int h = 1080;
        RenderTexture rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
        RenderTexture prevRt = cam.targetTexture;
        RenderTexture prevActive = RenderTexture.active;

        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;

        Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();

        cam.targetTexture = prevRt;
        RenderTexture.active = prevActive;
        Object.DestroyImmediate(rt);

        string fullPath = Path.Combine(Directory.GetCurrentDirectory(), path);
        string dir = Path.GetDirectoryName(fullPath);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        File.WriteAllBytes(fullPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        Debug.Log("[Canh1SceneBuilder] Đã chụp ảnh render camera chính vào: " + path);
    }
}
