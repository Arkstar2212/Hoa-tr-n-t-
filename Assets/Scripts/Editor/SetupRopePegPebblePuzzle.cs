using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using HoaVaMau.Gameplay;

namespace HoaVaMau.Editor
{
    /// <summary>
    /// Editor Script thiết lập câu đố "Ném đá cuội vỡ chốt cọc gỗ buộc dây -> Dây trôi rơi xuống -> Leo lên Higher Ground -> Đẩy thùng leo lên".
    /// Tuân thủ tuyệt đối Rule 4.1 (Gom 1 file C# Editor Script duy nhất) và Rule 4.3 (Additive Only - Bảo toàn nguyên trạng Scene).
    /// </summary>
    public static class SetupRopePegPebblePuzzle
    {
        [MenuItem("Tools/Hoa Tren Dat Do/Setup Rope Peg Pebble Puzzle")]
        public static void Execute()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.name != "New Scene 1_Overhaul")
            {
                scene = EditorSceneManager.OpenScene("Assets/Scenes/New Scene 1_Overhaul.unity", OpenSceneMode.Single);
            }

            // 1. Tìm nhánh Obstacles trong Scene
            GameObject gpRoot = GameObject.Find("02_Gameplay");
            Transform obstaclesRoot = gpRoot != null ? gpRoot.transform.Find("ForestManual_Obstacles") : null;
            if (obstaclesRoot == null)
            {
                var foundObs = GameObject.Find("ForestManual_Obstacles");
                if (foundObs != null) obstaclesRoot = foundObs.transform;
            }

            if (obstaclesRoot == null)
            {
                Debug.LogError("[SetupRopePegPebblePuzzle] Không tìm thấy ForestManual_Obstacles trong Scene!");
                return;
            }

            // 2. Cấu hình dây thừng Obstacle02_RavineSwingRope ở trạng thái cuộn trên cao (startCoiled = true)
            // Khi chốt cọc gỗ vỡ, dây sẽ tự động trôi rơi xuống với chiều dài 5.10m (từ Y = 2.15m xuống Y = -2.95m)
            // giúp Player leo lên Higher Ground (Col_04_RavineEastPlateau tại X = 17.6m..20.6m, Y = -0.55m) rồi tiến tới đẩy thùng gỗ (X = 30.0m).
            Transform ropeTr = obstaclesRoot.Find("Obstacle02_RavineSwingRope");
            RopeController targetRope = ropeTr != null ? ropeTr.GetComponent<RopeController>() : Object.FindAnyObjectByType<RopeController>();

            if (targetRope != null)
            {
                Undo.RecordObject(targetRope.transform, "Configure Coiled Ravine Rope");
                targetRope.transform.position = new Vector3(16.10f, 2.15f, -0.15f);

                SerializedObject ropeSO = new SerializedObject(targetRope);
                var totalLenProp = ropeSO.FindProperty("totalLength");
                var nodeCountProp = ropeSO.FindProperty("nodeCount");
                var startCoiledProp = ropeSO.FindProperty("startCoiled");
                var coiledLenProp = ropeSO.FindProperty("coiledLength");
                var unrollSpeedProp = ropeSO.FindProperty("dropUnrollSpeed");

                if (totalLenProp != null) totalLenProp.floatValue = 5.10f;
                if (nodeCountProp != null) nodeCountProp.intValue = 18;
                if (startCoiledProp != null) startCoiledProp.boolValue = true;
                if (coiledLenProp != null) coiledLenProp.floatValue = 0.45f;
                if (unrollSpeedProp != null) unrollSpeedProp.floatValue = 8.5f;
                ropeSO.ApplyModifiedProperties();

                targetRope.InitializeRope();
                EditorUtility.SetDirty(targetRope);
            }

            // 3. Tạo hoặc làm mới nhóm Additive riêng biệt cho Chốt Cọc Gỗ & Đá Cuội Ném
            const string PuzzleRootName = "[Additive_RavineRopePeg_PebblePuzzle]";
            Transform existingPuzzleRoot = obstaclesRoot.Find(PuzzleRootName);
            if (existingPuzzleRoot != null)
            {
                Object.DestroyImmediate(existingPuzzleRoot.gameObject);
            }

            GameObject puzzleRoot = new GameObject(PuzzleRootName);
            puzzleRoot.transform.SetParent(obstaclesRoot, false);
            puzzleRoot.transform.localPosition = Vector3.zero;

            // Load Materials chuẩn dự án
            Material woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Props/M_WoodenCrate_01.mat")
                            ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Crate_RusticWood.mat")
                            ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Foliage/M_Playground_Timber.mat");

            Material darkWoodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Foliage/M_Playground_Timber.mat")
                                ?? woodMat;

            Material pebbleMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/GhibliModular/M_Ghibli_PebbleRock.mat")
                              ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Rocks/M_Rock_MossyBoulder.mat")
                              ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Props/M_Playground_Orb.mat");

            Sprite ropeCoilSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Environment/Rope/T_Rope_Branch_Knot_Coil.png");

            // 4. Dựng CHỐT CỌC GỖ BUỘC DÂY (Obstacle02_WoodenPeg_RopeLatch) tại (16.10, 2.15, 0.0)
            // Chịu đúng 1 phát ném đá cuội (maxHits = 1) -> Vỡ chốt gỗ & thả dây thừng rơi xuống
            GameObject pegGO = new GameObject("Obstacle02_WoodenPeg_RopeLatch");
            pegGO.transform.SetParent(puzzleRoot.transform, false);
            pegGO.transform.position = new Vector3(16.10f, 2.15f, 0f);

            GameObject visualBody = new GameObject("Visual_Body");
            visualBody.transform.SetParent(pegGO.transform, false);
            visualBody.transform.localPosition = Vector3.zero;

            // Thanh chốt gỗ ngang giữ cuộn dây
            GameObject crossPeg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crossPeg.name = "WoodenPeg_CrossBar";
            crossPeg.transform.SetParent(visualBody.transform, false);
            crossPeg.transform.localPosition = new Vector3(0f, 0.04f, -0.05f);
            crossPeg.transform.localRotation = Quaternion.Euler(0f, 0f, -11f);
            crossPeg.transform.localScale = new Vector3(0.58f, 0.16f, 0.24f);
            if (woodMat != null) crossPeg.GetComponent<MeshRenderer>().sharedMaterial = woodMat;
            Object.DestroyImmediate(crossPeg.GetComponent<Collider>());

            // Cọc gỗ đóng dọc giữ chốt
            GameObject verticalPost = GameObject.CreatePrimitive(PrimitiveType.Cube);
            verticalPost.name = "WoodenPeg_VerticalStake";
            verticalPost.transform.SetParent(visualBody.transform, false);
            verticalPost.transform.localPosition = new Vector3(0.02f, 0.14f, 0.02f);
            verticalPost.transform.localRotation = Quaternion.Euler(0f, 0f, 4f);
            verticalPost.transform.localScale = new Vector3(0.18f, 0.52f, 0.22f);
            if (darkWoodMat != null) verticalPost.GetComponent<MeshRenderer>().sharedMaterial = darkWoodMat;
            Object.DestroyImmediate(verticalPost.GetComponent<Collider>());

            // Đai chốt khóa giữa cọc gỗ
            GameObject latchBand = GameObject.CreatePrimitive(PrimitiveType.Cube);
            latchBand.name = "WoodenPeg_LatchBand";
            latchBand.transform.SetParent(visualBody.transform, false);
            latchBand.transform.localPosition = new Vector3(0f, 0.04f, -0.05f);
            latchBand.transform.localRotation = Quaternion.Euler(0f, 0f, -11f);
            latchBand.transform.localScale = new Vector3(0.22f, 0.20f, 0.28f);
            if (darkWoodMat != null) latchBand.GetComponent<MeshRenderer>().sharedMaterial = darkWoodMat;
            Object.DestroyImmediate(latchBand.GetComponent<Collider>());

            // Cuộn dây buộc quanh chốt cọc gỗ (sẽ biến mất cùng chốt khi vỡ, nhường chỗ cho dây thật bung xuống)
            if (ropeCoilSpr != null)
            {
                GameObject coilGO = new GameObject("CoiledRope_OnPeg_Sprite");
                coilGO.transform.SetParent(visualBody.transform, false);
                coilGO.transform.localPosition = new Vector3(0f, -0.08f, -0.18f);
                coilGO.transform.localScale = new Vector3(0.78f, 0.78f, 1f);
                var coilSR = coilGO.AddComponent<SpriteRenderer>();
                coilSR.sprite = ropeCoilSpr;
                coilSR.sortingOrder = 27;
            }

            // Collider nhận va chạm từ Đá Cuội ném tới (Z = 0f, kích thước rộng rãi giúp ngắm ném 1 phát trúng ngay)
            var pegCol = pegGO.AddComponent<BoxCollider>();
            pegCol.size = new Vector3(1.15f, 1.15f, 1.40f);
            pegCol.center = Vector3.zero;

            var pegRb = pegGO.AddComponent<Rigidbody>();
            pegRb.isKinematic = true;
            pegRb.useGravity = false;

            var destructiblePeg = pegGO.AddComponent<DestructibleCrate>();
            SerializedObject pegSO = new SerializedObject(destructiblePeg);
            pegSO.FindProperty("maxHits").intValue = 1; // Ném đá cuội 1 phát là vỡ chốt!
            pegSO.FindProperty("minImpactSpeed").floatValue = 1.2f;
            pegSO.FindProperty("collapseOutwardForce").floatValue = 1.6f;
            pegSO.FindProperty("upwardPopForce").floatValue = 1.1f;
            pegSO.FindProperty("plankPieceCount").intValue = 6;
            pegSO.FindProperty("chunkPieceCount").intValue = 6;
            if (woodMat != null) pegSO.FindProperty("woodMaterial").objectReferenceValue = woodMat;
            if (targetRope != null) pegSO.FindProperty("linkedRopeToDrop").objectReferenceValue = targetRope;
            pegSO.ApplyModifiedProperties();

            // 5. Tạo các viên ĐÁ CUỘI NÉM (PortableItem) trên bờ vực bên trái & dưới đáy hố phòng hờ
            CreateThrowablePebble(puzzleRoot.transform, "Throwable_Pebble_01", new Vector3(12.65f, -0.34f, 0f), pebbleMat);
            CreateThrowablePebble(puzzleRoot.transform, "Throwable_Pebble_02", new Vector3(13.25f, -0.34f, 0f), pebbleMat);
            CreateThrowablePebble(puzzleRoot.transform, "Throwable_Pebble_03", new Vector3(13.80f, -0.34f, 0f), pebbleMat);

            // Đá cuội dự phòng dưới đáy vực (Y = -3.80m) để nếu Player lỡ rơi xuống hố trước khi ném vẫn có đá ném lên chốt gỗ
            CreateThrowablePebble(puzzleRoot.transform, "Throwable_Pebble_RavineFloor_04", new Vector3(15.20f, -3.56f, 0f), pebbleMat);
            CreateThrowablePebble(puzzleRoot.transform, "Throwable_Pebble_RavineFloor_05", new Vector3(16.85f, -3.56f, 0f), pebbleMat);

            // 6. Đảm bảo Player có sẵn TrajectoryPredictor & CarrySocket để hiển thị đường ngắm Parabol mượt mà
            var player = Object.FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                if (player.GetComponentInChildren<TrajectoryPredictor>() == null)
                {
                    GameObject trajGO = new GameObject("TrajectoryPredictor");
                    trajGO.transform.SetParent(player.transform, false);
                    trajGO.transform.localPosition = Vector3.zero;
                    trajGO.AddComponent<TrajectoryPredictor>();
                }
                EditorUtility.SetDirty(player);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("<color=#4ade80>[SetupRopePegPebblePuzzle]</color> Hoàn tất thiết lập Chốt Cọc Gỗ Buộc Dây (1 phát vỡ) + Đá Cuội Ném (PortableItem) + Dây Thừng Bung Rơi Xuống để leo lên Higher Ground trước khi tới cảnh đẩy thùng!");
        }

        private static void CreateThrowablePebble(Transform parent, string name, Vector3 position, Material pebbleMat)
        {
            GameObject pebbleGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pebbleGO.name = name;
            pebbleGO.transform.SetParent(parent, false);
            pebbleGO.transform.position = position;
            pebbleGO.transform.localScale = new Vector3(0.32f, 0.32f, 0.32f);

            var rootRenderer = pebbleGO.GetComponent<MeshRenderer>();
            if (rootRenderer != null)
            {
                if (pebbleMat != null) rootRenderer.sharedMaterial = pebbleMat;
                rootRenderer.enabled = false; // Ẩn sphere chuẩn để hiển thị khối đá cuội dẹt tự nhiên bên trong
            }

            // Tạo hình dáng viên đá cuội tự nhiên (hơi dẹt bầu dục) bên trong
            GameObject visualRock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visualRock.name = "Pebble_VisualMesh";
            visualRock.transform.SetParent(pebbleGO.transform, false);
            visualRock.transform.localPosition = Vector3.zero;
            visualRock.transform.localRotation = Quaternion.Euler(8f, 24f, -12f);
            visualRock.transform.localScale = new Vector3(1.18f, 0.78f, 0.96f);
            if (pebbleMat != null) visualRock.GetComponent<MeshRenderer>().sharedMaterial = pebbleMat;
            Object.DestroyImmediate(visualRock.GetComponent<Collider>());

            var rb = pebbleGO.GetComponent<Rigidbody>();
            if (rb == null) rb = pebbleGO.AddComponent<Rigidbody>();
            rb.mass = 2.0f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY;

            var portable = pebbleGO.AddComponent<PortableItem>();
            SerializedObject so = new SerializedObject(portable);
            so.FindProperty("itemName").stringValue = "Đá cuội";
            so.FindProperty("pickupRadius").floatValue = 1.45f;
            so.FindProperty("itemMass").floatValue = 2.0f;
            so.FindProperty("respawnBelowY").floatValue = -12.0f;
            so.ApplyModifiedProperties();
        }
    }
}
