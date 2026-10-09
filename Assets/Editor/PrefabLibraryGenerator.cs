using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using HoaVaMau.Gameplay;
using HoaVaMau.Test3D;

public static class PrefabLibraryGenerator
{
    private const string BasePrefabDir = "Assets/Prefabs";

    [MenuItem("HoaVaMau/Generate Prefab Library")]
    public static void GeneratePrefabs()
    {
        Debug.Log("[PrefabLibraryGenerator] Bắt đầu quét các scene và trích xuất Prefabs chuẩn...");

        // 1. Tạo cấu trúc thư mục phân loại rõ ràng
        EnsureFolder(BasePrefabDir);
        EnsureFolder(BasePrefabDir + "/Characters");
        EnsureFolder(BasePrefabDir + "/NPCs_Silhouettes");
        EnsureFolder(BasePrefabDir + "/Boats");
        EnsureFolder(BasePrefabDir + "/Environment_Dock");
        EnsureFolder(BasePrefabDir + "/Props");
        EnsureFolder(BasePrefabDir + "/Vegetation");
        EnsureFolder(BasePrefabDir + "/VFX");

        // 2. Trích xuất từ scene chính: canh1.unity
        ExtractFromCanh1();

        // 3. Trích xuất từ scene: CaMau_Dock_1954.unity
        ExtractFromCaMauDock1954();

        // 4. Trích xuất từ scene: canh 2d.unity
        ExtractFromCanh2D();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PrefabLibraryGenerator] Hoàn thành tạo thư viện Prefabs!");
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

    private static void ExtractFromCanh1()
    {
        var root = GameObject.Find("CAMAU_REGROUPMENT_1954");
        if (root == null)
        {
            Debug.LogWarning("[PrefabLibraryGenerator] Không tìm thấy CAMAU_REGROUPMENT_1954 trong scene hiện tại.");
            return;
        }

        // --- CHARACTERS ---
        Transform playerMinh = root.transform.Find("Gameplay/Player_Minh");
        if (playerMinh != null)
        {
            SaveAsNormalizedPrefab(playerMinh.gameObject, BasePrefabDir + "/Characters/PF_Player_Minh.prefab", true);
        }

        // --- BOATS ---
        Transform ship = root.transform.Find("TransportShip/Large_Transport_Ship_1954");
        if (ship != null)
        {
            SaveAsNormalizedPrefab(ship.gameObject, BasePrefabDir + "/Boats/PF_TransportShip_1954.prefab", false);
        }

        Transform sampanCovered = root.transform.Find("TransferBoats/Sampan_Moored_Dock");
        if (sampanCovered != null)
        {
            SaveAsNormalizedPrefab(sampanCovered.gameObject, BasePrefabDir + "/Boats/PF_Sampan_Covered_Floating.prefab", false);
        }

        Transform sampanHat = root.transform.Find("TransferBoats/Sampan_MidRiver_Hat");
        if (sampanHat != null)
        {
            SaveAsNormalizedPrefab(sampanHat.gameObject, BasePrefabDir + "/Boats/PF_Sampan_Open_Hat_Floating.prefab", false);
        }

        Transform waterLily = root.transform.Find("TransferBoats/WaterLily_Cluster_1");
        if (waterLily != null)
        {
            SaveAsNormalizedPrefab(waterLily.gameObject, BasePrefabDir + "/Boats/PF_WaterLily_Cluster.prefab", false);
        }

        // --- ENVIRONMENT & DOCK ---
        // Ghép 1 nhịp cầu tàu hoàn chỉnh thành Modular Prefab
        CreateModularDockSegmentPrefab();

        Transform shelter = root.transform.Find("Dock/Dock_Waiting_Shelter");
        if (shelter != null)
        {
            SaveAsNormalizedPrefab(shelter.gameObject, BasePrefabDir + "/Environment_Dock/PF_Dock_Waiting_Shelter.prefab", false);
        }

        Transform villageHut = root.transform.Find("RiversideVillage/Village_Hut_Right");
        if (villageHut != null)
        {
            SaveAsNormalizedPrefab(villageHut.gameObject, BasePrefabDir + "/Environment_Dock/PF_Village_ThatchedHut.prefab", false);
        }

        // --- NPCS SILHOUETTES ---
        Transform npcCivFamily = root.transform.Find("CivilianNPCs/Civilian_Family_Group");
        if (npcCivFamily != null) SaveAsNormalizedPrefab(npcCivFamily.gameObject, BasePrefabDir + "/NPCs_Silhouettes/PF_NPC_Civilian_Family.prefab", false);

        Transform npcMotherChild = root.transform.Find("CivilianNPCs/Mother_Holding_Child");
        if (npcMotherChild != null) SaveAsNormalizedPrefab(npcMotherChild.gameObject, BasePrefabDir + "/NPCs_Silhouettes/PF_NPC_Mother_Child.prefab", false);

        Transform npcElderly = root.transform.Find("CivilianNPCs/Elderly_Near_Luggage");
        if (npcElderly != null) SaveAsNormalizedPrefab(npcElderly.gameObject, BasePrefabDir + "/NPCs_Silhouettes/PF_NPC_Elderly_Person.prefab", false);

        Transform npcFamilyWaving = root.transform.Find("CivilianNPCs/Family_Waving_Farewell");
        if (npcFamilyWaving != null) SaveAsNormalizedPrefab(npcFamilyWaving.gameObject, BasePrefabDir + "/NPCs_Silhouettes/PF_NPC_Family_Waving.prefab", false);

        Transform npcSoldierDuffel = root.transform.Find("SoldierNPCs/Soldier_With_Duffel");
        if (npcSoldierDuffel != null) SaveAsNormalizedPrefab(npcSoldierDuffel.gameObject, BasePrefabDir + "/NPCs_Silhouettes/PF_NPC_Soldier_Duffel.prefab", false);

        Transform npcSoldierSalute = root.transform.Find("SoldierNPCs/Soldier_Saluting_Pier");
        if (npcSoldierSalute != null) SaveAsNormalizedPrefab(npcSoldierSalute.gameObject, BasePrefabDir + "/NPCs_Silhouettes/PF_NPC_Soldier_Saluting.prefab", false);

        // --- PROPS ---
        Transform sacks = root.transform.Find("HistoricalProps/Rice_Sacks_Shelter");
        if (sacks != null) SaveAsNormalizedPrefab(sacks.gameObject, BasePrefabDir + "/Props/PF_Prop_RiceSacks.prefab", false);

        Transform luggage = root.transform.Find("HistoricalProps/Luggage_Bundles_Pier");
        if (luggage != null) SaveAsNormalizedPrefab(luggage.gameObject, BasePrefabDir + "/Props/PF_Prop_LuggageBundles.prefab", false);

        Transform crates2D = root.transform.Find("HistoricalProps/Crates_Stack_Pier");
        if (crates2D != null) SaveAsNormalizedPrefab(crates2D.gameObject, BasePrefabDir + "/Props/PF_Prop_CratesStack_2D.prefab", false);

        Transform crate3D = root.transform.Find("HistoricalProps/Cargo_Crate_StepObstacle");
        if (crate3D != null) SaveAsNormalizedPrefab(crate3D.gameObject, BasePrefabDir + "/Props/PF_Prop_CargoCrate_3D.prefab", false);

        // --- VEGETATION ---
        Transform rootLeft = root.transform.Find("Vegetation/Foreground/FG_Mangrove_Root_Left");
        if (rootLeft != null) SaveAsNormalizedPrefab(rootLeft.gameObject, BasePrefabDir + "/Vegetation/PF_FG_Mangrove_Root.prefab", false);

        Transform vines = root.transform.Find("Vegetation/Foreground/FG_Vines_Top_Left");
        if (vines != null) SaveAsNormalizedPrefab(vines.gameObject, BasePrefabDir + "/Vegetation/PF_FG_Vines_Hanging.prefab", false);

        Transform nipa = root.transform.Find("Vegetation/Foreground/FG_NipaPalm_Top_Right");
        if (nipa != null) SaveAsNormalizedPrefab(nipa.gameObject, BasePrefabDir + "/Vegetation/PF_FG_NipaPalm_Fronds.prefab", false);

        Transform bush = root.transform.Find("Vegetation/Foreground/FG_Mangrove_Bush_BottomLeft");
        if (bush != null) SaveAsNormalizedPrefab(bush.gameObject, BasePrefabDir + "/Vegetation/PF_FG_Mangrove_Bush.prefab", false);

        Transform forest = root.transform.Find("Vegetation/Background/BG_Mangrove_Forest_Left");
        if (forest != null) SaveAsNormalizedPrefab(forest.gameObject, BasePrefabDir + "/Vegetation/PF_BG_Mangrove_Forest.prefab", false);

        // --- VFX ---
        Transform vfxDust = root.transform.Find("VFX/Atmospheric_Golden_Dust");
        if (vfxDust != null) SaveAsNormalizedPrefab(vfxDust.gameObject, BasePrefabDir + "/VFX/PF_Atmospheric_GoldenDust.prefab", false);
    }

    private static void CreateModularDockSegmentPrefab()
    {
        string path = BasePrefabDir + "/Environment_Dock/PF_Dock_Segment_Walkable.prefab";
        GameObject dockSegment = new GameObject("PF_Dock_Segment_Walkable");

        // 1. Visual Card mặt ván gỗ vẽ tay
        Sprite pierSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Canh1_Sprites/BG_Prop_02.png");
        if (pierSprite != null)
        {
            GameObject visual = new GameObject("Visual_Pier_Planks");
            visual.transform.SetParent(dockSegment.transform, false);
            visual.transform.localPosition = new Vector3(0f, -0.25f, -0.1f);
            visual.transform.localScale = new Vector3(1.6f, 1.6f, 1f);
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = pierSprite;
            sr.color = new Color(0.98f, 0.94f, 0.88f, 1.0f);
        }

        // 2. Dầm gỗ 3D phía sau tạo bề dày
        Material timberMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Thung Go Vuong.mat");
        if (timberMat == null) timberMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Boat_Wood.mat");
        GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
        beam.name = "Timber_Beam_3D";
        beam.transform.SetParent(dockSegment.transform, false);
        beam.transform.localPosition = new Vector3(0f, 0f, 0.8f);
        beam.transform.localScale = new Vector3(7.66f, 0.38f, 1.6f);
        if (timberMat != null) beam.GetComponent<MeshRenderer>().sharedMaterial = timberMat;
        Object.DestroyImmediate(beam.GetComponent<Collider>());

        // 3. BoxCollider va chạm chơi được cho nhân vật đứng lên
        BoxCollider col = dockSegment.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, 0f, 0f);
        col.size = new Vector3(7.66f, 0.4f, 2.8f);

        PrefabUtility.SaveAsPrefabAsset(dockSegment, path);
        Object.DestroyImmediate(dockSegment);
        Debug.Log("[PrefabLibraryGenerator] Đã tạo Modular Dock Prefab: " + path);
    }

    private static void ExtractFromCaMauDock1954()
    {
        string scenePath = "Assets/Scenes/CaMau_Dock_1954.unity";
        var sc = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        var root = GameObject.Find("CAMAU_DOCK_1954");
        if (root != null)
        {
            // Thùng ủ cá muối
            Transform barrel = root.transform.Find("Props/Barrels/Salt_Fish_Barrel_0");
            if (barrel != null) SaveAsNormalizedPrefab(barrel.gameObject, BasePrefabDir + "/Props/PF_Prop_SaltFish_Barrel.prefab", false);

            // Giỏ nan cần xé tre
            Transform basket = root.transform.Find("Props/Baskets/Bamboo_Basket_0");
            if (basket != null) SaveAsNormalizedPrefab(basket.gameObject, BasePrefabDir + "/Props/PF_Prop_Bamboo_Basket.prefab", false);

            // Lưới đánh cá phơi ven bến
            Transform net = root.transform.Find("Props/FishingNets/Draped_Fish_Net_0");
            if (net != null) SaveAsNormalizedPrefab(net.gameObject, BasePrefabDir + "/Props/PF_Prop_Fishing_Net.prefab", false);

            // Nhà sàn Nam Bộ
            Transform hut = root.transform.Find("Village/Houses/House_Stilt_1");
            if (hut != null) SaveAsNormalizedPrefab(hut.gameObject, BasePrefabDir + "/Environment_Dock/PF_Village_StiltHut_3D.prefab", false);
        }
        EditorSceneManager.CloseScene(sc, true);
    }

    private static void ExtractFromCanh2D()
    {
        string scenePath = "Assets/Scenes/canh 2d.unity";
        var sc = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

        // 3D Characters đã gắn sẵn Material hoàn chỉnh
        GameObject elderlyMan = GameObject.Find("Meshy_AI_Elderly_Man_0929030907_texture");
        if (elderlyMan != null) SaveAsNormalizedPrefab(elderlyMan, BasePrefabDir + "/Characters/PF_3D_Elderly_Man.prefab", false);

        GameObject boyBun = GameObject.Find("Meshy_AI_boy_single_bun_0929030110_image-to-3d-texture");
        if (boyBun != null) SaveAsNormalizedPrefab(boyBun, BasePrefabDir + "/Characters/PF_3D_Boy_SingleBun.prefab", false);

        GameObject babyChar = GameObject.Find("baby_character_3d_model");
        if (babyChar != null) SaveAsNormalizedPrefab(babyChar, BasePrefabDir + "/Characters/PF_3D_Baby_Character.prefab", false);

        EditorSceneManager.CloseScene(sc, true);
    }

    private static void SaveAsNormalizedPrefab(GameObject source, string targetPath, bool keepScale)
    {
        GameObject clone = Object.Instantiate(source);
        clone.name = Path.GetFileNameWithoutExtension(targetPath);

        // Reset vị trí gốc về (0, 0, 0) để khi kéo vào scene khác luôn xuất hiện ngay tâm con trỏ
        clone.transform.position = Vector3.zero;
        clone.transform.rotation = Quaternion.identity;
        if (!keepScale)
        {
            // Giữ nguyên scale đã tinh chỉnh của source
            clone.transform.localScale = source.transform.localScale;
        }

        PrefabUtility.SaveAsPrefabAsset(clone, targetPath);
        Object.DestroyImmediate(clone);
        Debug.Log("[PrefabLibraryGenerator] Đã lưu Prefab: " + targetPath);
    }
}
