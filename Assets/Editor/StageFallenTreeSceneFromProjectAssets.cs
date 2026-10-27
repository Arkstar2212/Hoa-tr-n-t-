using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class StageFallenTreeSceneFromProjectAssets : Editor
{
    private const string ParentName = "[Additive_FallenTree_Scene]";

    [MenuItem("Tools/Hoa Tren Dat Do/Stage Canh 2 Fallen Tree (Tu Asset Project)")]
    public static void BuildFallenTreeScene()
    {
        // 1. Kiểm tra Scene hiện tại
        var activeScene = EditorSceneManager.GetActiveScene();
        if (!activeScene.name.Contains("New Scene 1_Overhaul"))
        {
            Debug.LogWarning("Vui lòng mở Scene 'New Scene 1_Overhaul' để dàn dựng!");
        }

        // 2. Tìm hoặc tạo Group cha Additive
        GameObject rootGroup = GameObject.Find(ParentName);
        if (rootGroup != null)
        {
            Undo.DestroyObjectImmediate(rootGroup);
        }

        rootGroup = new GameObject(ParentName);
        Undo.RegisterCreatedObjectUndo(rootGroup, "Stage Fallen Tree Scene");
        rootGroup.transform.position = Vector3.zero;

        // Load các Asset chính thức có sẵn trong Project
        GameObject prefabGocCay = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Trees/PF_Tree_GocCay2_Artistic.prefab");
        GameObject prefabBanana = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_05_Banana.prefab");
        GameObject prefabBushFlower = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_Concept_Assets/Bushes/PF_T_HD_Bush_Low_02_Flower.prefab");
        GameObject prefabBushBroadleaf = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_Concept_Assets/Bushes/PF_T_HD_Bush_Broadleaf_01.prefab");
        GameObject prefabRockMossy = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_Concept_Assets/Rocks/PF_T_HD_Rock_02_MediumMossyDome.prefab");
        GameObject prefabGrassMeadow = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_Concept_Assets/Grass/PF_T_HD_GrassCarpet_02_TallMeadow.prefab");
        GameObject prefabRootCluster = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/ModularKit/PF_Mod_MangroveRoot_Cluster.prefab");

        // Load Textures/Sprites
        Sprite sprFallenTrunk = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Lana_Unified_Art/Branches/T_Lana_FallenTrunk_Mossy.png");
        Sprite sprCurvedTrunk = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Tree/Curved_Tree_V2_Trunk.png");
        Sprite sprCurvedCanopy = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Tree/Curved_Tree_V2_Canopy.png");
        Sprite sprCurvedMound = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Tree/Curved_Tree_V2_Mound.png");
        Sprite sprForestBG = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Backgrounds_Lana/BG_Forest_CleanPlate.png");
        if (sprForestBG == null)
            sprForestBG = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Backgrounds_Lana/BG_Forest_CleanPlate_OpenMeadow.png");

        Material sprLitMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/LanaOverhaul/SpriteMats/M_Spr_T_Lana_FallenTrunk_Mossy.mat");

        // -------------------------------------------------------------
        // KHU VỰC A (X ≈ 35.5 - 38.5): Gốc cây cổ thụ mục rỗng trên vách cao
        // -------------------------------------------------------------
        GameObject areaA = new GameObject("AreaA_TreeStump_HighCliff");
        areaA.transform.SetParent(rootGroup.transform, false);
        areaA.transform.position = new Vector3(36.8f, 2.15f, 0.2f);

        // Gò đất chân gốc cây
        if (sprCurvedMound != null)
        {
            GameObject mound = new GameObject("Mound_EarthBase");
            mound.transform.SetParent(areaA.transform, false);
            mound.transform.localPosition = new Vector3(-0.4f, 0.2f, 0.1f);
            mound.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
            var sr = mound.AddComponent<SpriteRenderer>();
            sr.sprite = sprCurvedMound;
            if (sprLitMat != null) sr.material = sprLitMat;
            sr.sortingOrder = 5;
        }

        // Gốc cây gãy nghiêng
        if (prefabGocCay != null)
        {
            GameObject stump = (GameObject)PrefabUtility.InstantiatePrefab(prefabGocCay, areaA.transform);
            stump.name = "Hero_Ancient_Stump";
            stump.transform.localPosition = new Vector3(0f, 0.8f, -0.05f);
            stump.transform.localRotation = Quaternion.Euler(0f, 0f, -28f); // nghiêng chếch xuống hướng vực
            stump.transform.localScale = new Vector3(1.15f, 1.15f, 1f);
        }

        // Cụm rễ phụ bám mép vách đất
        if (prefabRootCluster != null)
        {
            GameObject roots = (GameObject)PrefabUtility.InstantiatePrefab(prefabRootCluster, areaA.transform);
            roots.name = "Hanging_MangroveRoots";
            roots.transform.localPosition = new Vector3(0.6f, -0.4f, -0.1f);
            roots.transform.localRotation = Quaternion.Euler(0f, 0f, 15f);
            roots.transform.localScale = new Vector3(0.85f, 0.85f, 1f);
        }

        // -------------------------------------------------------------
        // KHU VỰC B (X ≈ 38.5 - 46.5): Thân cây vắt ngang làm cầu gỗ tự nhiên
        // -------------------------------------------------------------
        GameObject areaB = new GameObject("AreaB_FallenTrunk_Bridge");
        areaB.transform.SetParent(rootGroup.transform, false);
        areaB.transform.position = new Vector3(42.5f, 1.25f, 0.05f);

        // Thân cây gỗ lớn dáng cong phủ rêu
        Sprite trunkSpriteToUse = sprCurvedTrunk != null ? sprCurvedTrunk : sprFallenTrunk;
        if (trunkSpriteToUse != null)
        {
            GameObject trunk = new GameObject("Fallen_Bridge_Trunk");
            trunk.transform.SetParent(areaB.transform, false);
            trunk.transform.localPosition = Vector3.zero;
            trunk.transform.localRotation = Quaternion.Euler(0f, 0f, -14.5f); // Dốc mượt mà nối từ Y=2.2 xuống Y=0.3
            trunk.transform.localScale = new Vector3(1.35f, 1.2f, 1f);

            var sr = trunk.AddComponent<SpriteRenderer>();
            sr.sprite = trunkSpriteToUse;
            if (sprLitMat != null) sr.material = sprLitMat;
            sr.sortingOrder = 10;
        }

        // Lớp thân gỗ phụ phủ rêu dày ở trung cảnh
        if (sprFallenTrunk != null && trunkSpriteToUse != sprFallenTrunk)
        {
            GameObject mossyTrunk = new GameObject("Mossy_Bark_Layer");
            mossyTrunk.transform.SetParent(areaB.transform, false);
            mossyTrunk.transform.localPosition = new Vector3(-0.8f, 0.4f, 0.12f);
            mossyTrunk.transform.localRotation = Quaternion.Euler(0f, 0f, -12f);
            mossyTrunk.transform.localScale = new Vector3(1.1f, 0.9f, 1f);

            var sr = mossyTrunk.AddComponent<SpriteRenderer>();
            sr.sprite = sprFallenTrunk;
            if (sprLitMat != null) sr.material = sprLitMat;
            sr.sortingOrder = 8;
        }

        // BoxCollider cho thân cây nghiêng (Nhân vật chạy nhảy trên thân cây)
        GameObject trunkColliderObj = new GameObject("TrunkBridge_PhysicsCollider");
        trunkColliderObj.transform.SetParent(areaB.transform, false);
        trunkColliderObj.transform.localPosition = new Vector3(0.1f, 0.25f, 0f);
        trunkColliderObj.transform.localRotation = Quaternion.Euler(0f, 0f, -14.5f);
        var boxCol = trunkColliderObj.AddComponent<BoxCollider>();
        boxCol.size = new Vector3(8.8f, 0.55f, 3f);

        // Bụi hoa dại vàng/trắng rải trên lưng thân cây
        if (prefabBushFlower != null)
        {
            GameObject flower1 = (GameObject)PrefabUtility.InstantiatePrefab(prefabBushFlower, areaB.transform);
            flower1.name = "Trunk_Wildflower_01";
            flower1.transform.localPosition = new Vector3(-2.2f, 0.95f, -0.08f);
            flower1.transform.localScale = new Vector3(0.6f, 0.6f, 1f);

            GameObject flower2 = (GameObject)PrefabUtility.InstantiatePrefab(prefabBushFlower, areaB.transform);
            flower2.name = "Trunk_Wildflower_02";
            flower2.transform.localPosition = new Vector3(1.2f, 0.15f, -0.08f);
            flower2.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
        }

        // -------------------------------------------------------------
        // KHU VỰC C (X ≈ 47.0 - 52.0): Đầu thân cây cắm xuống đất & Tán lá
        // -------------------------------------------------------------
        GameObject areaC = new GameObject("AreaC_TreeCrown_LandingMeadow");
        areaC.transform.SetParent(rootGroup.transform, false);
        areaC.transform.position = new Vector3(48.5f, -0.3f, 0.1f);

        // Tán lá xanh xum xuê cắm phập xuống đất
        if (sprCurvedCanopy != null)
        {
            GameObject canopy = new GameObject("Fallen_Leafy_Canopy");
            canopy.transform.SetParent(areaC.transform, false);
            canopy.transform.localPosition = new Vector3(0.5f, 0.6f, 0.15f);
            canopy.transform.localRotation = Quaternion.Euler(0f, 0f, 25f);
            canopy.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

            var sr = canopy.AddComponent<SpriteRenderer>();
            sr.sprite = sprCurvedCanopy;
            if (sprLitMat != null) sr.material = sprLitMat;
            sr.sortingOrder = 9;
        }

        // Đá tảng rêu tựa ngọn cây
        if (prefabRockMossy != null)
        {
            GameObject rock = (GameObject)PrefabUtility.InstantiatePrefab(prefabRockMossy, areaC.transform);
            rock.name = "Mossy_SupportRock";
            rock.transform.localPosition = new Vector3(-0.8f, -0.4f, -0.05f);
            rock.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
        }

        // Thảm cỏ và hoa dại đón đầu
        if (prefabGrassMeadow != null)
        {
            GameObject grass = (GameObject)PrefabUtility.InstantiatePrefab(prefabGrassMeadow, areaC.transform);
            grass.name = "Landing_MeadowGrass";
            grass.transform.localPosition = new Vector3(1.5f, -0.6f, -0.05f);
            grass.transform.localScale = new Vector3(1f, 0.8f, 1f);
        }

        if (prefabBushBroadleaf != null)
        {
            GameObject bush = (GameObject)PrefabUtility.InstantiatePrefab(prefabBushBroadleaf, areaC.transform);
            bush.name = "Landing_Bush_Broadleaf";
            bush.transform.localPosition = new Vector3(2.6f, -0.4f, 0.1f);
            bush.transform.localScale = new Vector3(0.85f, 0.85f, 1f);
        }

        // -------------------------------------------------------------
        // HẬU CẢNH: Phông nền rừng sương mù khóa kín trời xanh phía sau
        // -------------------------------------------------------------
        if (sprForestBG != null)
        {
            GameObject bgObj = new GameObject("BG_Forest_MistyBackdrop_Extension");
            bgObj.transform.SetParent(rootGroup.transform, false);
            bgObj.transform.position = new Vector3(45.0f, 4.8f, 18.5f);
            bgObj.transform.localScale = new Vector3(1.4f, 1.4f, 1f);

            var sr = bgObj.AddComponent<SpriteRenderer>();
            sr.sprite = sprForestBG;
            sr.sortingLayerName = "Default";
            sr.sortingOrder = -50;
        }

        // -------------------------------------------------------------
        // TIỀN CẢNH (Z < 0): Lá chuối Nam Bộ & viền mờ tạo chiều sâu
        // -------------------------------------------------------------
        if (prefabBanana != null)
        {
            GameObject fgBanana = (GameObject)PrefabUtility.InstantiatePrefab(prefabBanana, rootGroup.transform);
            fgBanana.name = "FG_Banana_CinematicDepth";
            fgBanana.transform.position = new Vector3(33.5f, -1.8f, -1.4f);
            fgBanana.transform.localScale = new Vector3(0.95f, 0.95f, 1f);
        }

        // Đánh dấu dirty và lưu scene
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);

        Debug.Log("===> Đã dàn dựng hoàn chỉnh Phân Cảnh Cây Đổ (The Fallen Tree Scene) bằng chính Asset của dự án vào Scene!");
    }
}
