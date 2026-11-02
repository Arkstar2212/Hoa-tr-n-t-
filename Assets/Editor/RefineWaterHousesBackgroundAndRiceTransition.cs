using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

namespace HoaVaMau.EditorTools
{
    public static class RefineWaterHousesBackgroundAndRiceTransition
    {
        [MenuItem("Tools/Hoa Tren Dat Do/Execute Refine Water Houses Background And Rice Transition")]
        public static void Execute()
        {
            Debug.Log("[RefineScene] Bắt đầu đồng bộ hóa nước, lấy lại model nhà chuẩn, làm đẹp background tự nhiên và tạo đường nối scene trên cánh đồng lúa...");

            // 1. Đồng bộ toàn bộ Material Nước trong Scene: Không còn chỗ có chỗ không
            UnifyAllWaterSurfaces();

            // 2. Lấy lại đúng các Model/Prefab Nhà chuẩn trong Project để thay lại toàn bộ nhà trong Scene
            ReinstantiateProjectHousePrefabs();

            // 3. Tinh chỉnh Background tự nhiên với nhiều lớp núi mờ, rừng tràm và mây hoàng hôn Nam Bộ
            SetupNaturalPainterlyBackground();

            // 4. Tạo đường đi chính trên cánh đồng lúa dẫn tới cổng chuyển Scene (không sửa bất kỳ scene nào khác)
            CreateRiceFieldSceneTransitionPath();

            // 5. Cập nhật vật lý và lưu Scene
            Physics.SyncTransforms();
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("[RefineScene] HOÀN TẤT THÀNH CÔNG: Đã đồng bộ nước, lấy lại model nhà project, background tự nhiên sâu thẳm, và hoàn thiện đường đi nối scene trên cánh đồng!");
        }

        // =========================================================================
        // 1. ĐỒNG BỘ NƯỚC (KHÔNG CÒN CHỖ CÓ CHỖ KHÔNG)
        // =========================================================================
        private static void UnifyAllWaterSurfaces()
        {
            // Sử dụng Material nước 3D cao cấp UWa-Template-Tropical từ hệ thống Uber Stylized Water
            var matWater = AssetDatabase.LoadAssetAtPath<Material>("Assets/Shaders/Uber Stylized Water/Template Materials/UWa-Template-Tropical.mat");
            if (matWater == null)
            {
                matWater = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Water/M_StylizedWater_Sunset.mat");
            }

            var waterRoot = GameObject.Find("WaterSystem_3D");
            if (waterRoot == null)
            {
                waterRoot = new GameObject("WaterSystem_3D");
                var env = GameObject.Find("CaMau_1954_Village/Environment");
                if (env != null) waterRoot.transform.SetParent(env.transform, false);
            }

            // 1. Biển lớn phía Nam: Trải dài bao la từ bờ sông (Z = -8m) ra xa khơi (Z = -168m), rộng 320m
            var wSea = GameObject.Find("Water_South_Sea_Expansive");
            if (wSea == null)
            {
                wSea = GameObject.CreatePrimitive(PrimitiveType.Plane);
                wSea.name = "Water_South_Sea_Expansive";
                wSea.transform.SetParent(waterRoot.transform, false);
            }
            wSea.transform.position = new Vector3(0f, -0.65f, -88.00f);
            wSea.transform.localScale = new Vector3(32.00f, 1.00f, 16.00f);
            var mrSea = wSea.GetComponent<MeshRenderer>();
            if (mrSea != null && matWater != null) mrSea.sharedMaterial = matWater;
            var colSea = wSea.GetComponent<Collider>();
            if (colSea != null) colSea.enabled = false;
            EditorUtility.SetDirty(wSea);

            // 2. Kênh nước phía Đông (chảy dọc làng qua cầu ván gỗ):
            // Kéo dài liên tục từ cửa sông (Z = -12m) chảy xuyên suốt qua cầu ván gỗ đến đầu ruộng lúa (Z = 36m)
            var wCanal = GameObject.Find("Water_East_Canal");
            if (wCanal == null)
            {
                wCanal = GameObject.CreatePrimitive(PrimitiveType.Plane);
                wCanal.name = "Water_East_Canal";
                wCanal.transform.SetParent(waterRoot.transform, false);
            }
            wCanal.transform.position = new Vector3(17.50f, -0.65f, 12.00f);
            wCanal.transform.localScale = new Vector3(0.65f, 1.00f, 4.80f); // Chiều dài 48m bao trùm toàn tuyến kênh
            var mrCanal = wCanal.GetComponent<MeshRenderer>();
            if (mrCanal != null && matWater != null) mrCanal.sharedMaterial = matWater;
            var colCanal = wCanal.GetComponent<Collider>();
            if (colCanal != null) colCanal.enabled = false;
            EditorUtility.SetDirty(wCanal);

            // 3. Lạch nước phía Tây (chảy quanh vườn cây và chòi nước):
            var wCreek = GameObject.Find("Water_West_Creek");
            if (wCreek == null)
            {
                wCreek = GameObject.CreatePrimitive(PrimitiveType.Plane);
                wCreek.name = "Water_West_Creek";
                wCreek.transform.SetParent(waterRoot.transform, false);
            }
            wCreek.transform.position = new Vector3(-25.00f, -0.65f, -6.00f);
            wCreek.transform.localScale = new Vector3(1.50f, 1.00f, 2.80f);
            var mrCreek = wCreek.GetComponent<MeshRenderer>();
            if (mrCreek != null && matWater != null) mrCreek.sharedMaterial = matWater;
            var colCreek = wCreek.GetComponent<Collider>();
            if (colCreek != null) colCreek.enabled = false;
            EditorUtility.SetDirty(wCreek);

            // 4. Vùng nước bến xuồng / cầu cảng bờ sông:
            // Đảm bảo dưới sàn ván bến xuồng (-10m đến -8m) có nước lăn tăn đầy đủ, không bị hở đất bùn khô
            var wPier = GameObject.Find("Water_BoSong_Wharf");
            if (wPier == null)
            {
                wPier = GameObject.CreatePrimitive(PrimitiveType.Plane);
                wPier.name = "Water_BoSong_Wharf";
                wPier.transform.SetParent(waterRoot.transform, false);
            }
            wPier.transform.position = new Vector3(-6.00f, -0.65f, -9.50f);
            wPier.transform.localScale = new Vector3(2.50f, 1.00f, 0.80f);
            var mrPier = wPier.GetComponent<MeshRenderer>();
            if (mrPier != null && matWater != null) mrPier.sharedMaterial = matWater;
            var colPier = wPier.GetComponent<Collider>();
            if (colPier != null) colPier.enabled = false;
            EditorUtility.SetDirty(wPier);

            Debug.Log("[RefineScene] Đã đồng bộ 100% các mặt nước với UWa-Template-Tropical: Biển Nam, Kênh Đông, Lạch Tây, Bến Xuồng đều đầy ắp nước!");
        }

        // =========================================================================
        // 2. LẤY LẠI MODEL NHÀ CHUẨN TỪ PROJECT
        // =========================================================================
        private static void ReinstantiateProjectHousePrefabs()
        {
            var pThatched = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/PF_StiltedHouse_Thatched.prefab");
            var pWood = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/PF_StiltedHouse_Wood.prefab");
            var pNhaSanGo = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/NhaSanGo.prefab");

            // Đảm bảo Material Two-Sided (Cull Off) để các mặt nhà đều hiển thị trọn vẹn
            var matThatched = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Architecture/M_StiltedHouse_Thatched.mat");
            if (matThatched != null && matThatched.HasProperty("_Cull")) matThatched.SetFloat("_Cull", 0f);

            var matWood = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Architecture/M_StiltedHouse_Wood.mat");
            if (matWood != null && matWood.HasProperty("_Cull")) matWood.SetFloat("_Cull", 0f);

            var matNhaSanGo = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/NhaSanGo.mat");
            if (matNhaSanGo != null && matNhaSanGo.HasProperty("_Cull")) matNhaSanGo.SetFloat("_Cull", 0f);

            // Thay thế 6 nhà bằng đúng Prefab gốc:
            // 1. House_An_StiltedHouse_3D: Nhà sàn mái lá của An
            ReplaceWithProjectPrefab("House_An_StiltedHouse_3D", pThatched,
                new Vector3(0.50f, 4.44f, 19.50f),
                Quaternion.Euler(0f, 180f, 0f),
                new Vector3(4.20f, 4.20f, 4.20f),
                "01_Nha_An"
            );

            // 2. Nha_HangXom_NhaAn: Nhà sàn mái lá hàng xóm
            ReplaceWithProjectPrefab("Nha_HangXom_NhaAn", pThatched,
                new Vector3(-7.50f, 4.25f, 19.50f),
                Quaternion.Euler(0f, 180f, 0f),
                new Vector3(4.00f, 4.00f, 4.00f),
                "01_Nha_An"
            );

            // 3. Nha_BacBa_KhuLayNuoc: Nhà sàn gỗ Bác Ba ở khu giếng nước
            ReplaceWithProjectPrefab("Nha_BacBa_KhuLayNuoc", pWood,
                new Vector3(12.50f, 1.50f, 14.50f),
                Quaternion.Euler(0f, 180f, 0f),
                new Vector3(2.20f, 2.20f, 2.20f),
                "02_Khu_Lay_Nuoc"
            );

            // 4. Nha_XomVuon_BacHai: Nhà sàn mái lá Bác Hai xóm vườn
            ReplaceWithProjectPrefab("Nha_XomVuon_BacHai", pThatched,
                new Vector3(-19.50f, 3.85f, 23.50f),
                Quaternion.Euler(0f, 170f, 0f),
                new Vector3(4.00f, 4.00f, 4.00f),
                "05_Vuon_Cay"
            );

            // 5. Nha_XomVuon_Go: Nhà sàn gỗ xóm vườn
            ReplaceWithProjectPrefab("Nha_XomVuon_Go", pNhaSanGo != null ? pNhaSanGo : pWood,
                new Vector3(-25.50f, 1.50f, 23.50f),
                Quaternion.Euler(0f, 150f, 0f),
                new Vector3(2.20f, 2.20f, 2.20f),
                "05_Vuon_Cay"
            );

            // 6. Nha_NongDan_CanhDong: Nhà nông dân trên bờ ruộng phía Bắc
            ReplaceWithProjectPrefab("Nha_NongDan_CanhDong", pThatched,
                new Vector3(26.00f, 3.50f, 34.00f),
                Quaternion.Euler(0f, 180f, 0f),
                new Vector3(3.60f, 3.60f, 3.60f),
                "06_Canh_Dong"
            );

            Debug.Log("[RefineScene] Đã thay thế thành công toàn bộ 6 ngôi nhà bằng Prefab gốc chuẩn của dự án!");
        }

        private static void ReplaceWithProjectPrefab(string houseName, GameObject prefabAsset, Vector3 pos, Quaternion rot, Vector3 scale, string targetParentName)
        {
            if (prefabAsset == null)
            {
                Debug.LogWarning($"[RefineScene] Không tìm thấy Prefab Asset cho {houseName}!");
                return;
            }

            var existing = GameObject.Find(houseName);
            Transform parent = null;
            if (existing != null)
            {
                parent = existing.transform.parent;
                Object.DestroyImmediate(existing);
            }
            else if (!string.IsNullOrEmpty(targetParentName))
            {
                var pGo = GameObject.Find(targetParentName);
                if (pGo != null) parent = pGo.transform;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset);
            instance.name = houseName;
            if (parent != null) instance.transform.SetParent(parent, false);

            instance.transform.position = pos;
            instance.transform.rotation = rot;
            instance.transform.localScale = scale;

            // Đảm bảo child Mesh_0 đứng thẳng và không bị nhân đôi scale
            var meshChild = instance.transform.Find("Mesh_0");
            if (meshChild != null)
            {
                meshChild.localPosition = Vector3.zero;
                // Nếu prefab là thatched blender optimized thì rotation = identity, scale = 1
                if (prefabAsset.name.Contains("Thatched"))
                {
                    meshChild.localRotation = Quaternion.identity;
                    meshChild.localScale = Vector3.one;
                }
            }

            // Gắn BoxCollider để ngăn nhân vật đi xuyên tường
            var col = instance.GetComponent<BoxCollider>();
            if (col == null) col = instance.AddComponent<BoxCollider>();
            col.center = Vector3.zero;
            col.size = new Vector3(1.4f, 1.9f, 1.7f);

            EditorUtility.SetDirty(instance);
        }

        // =========================================================================
        // 3. BACKGROUND TỰ NHIÊN (NHIỀU LỚP NÚI MỜ, RỪNG TRÀM & MÂY HOÀNG HÔN)
        // =========================================================================
        private static void SetupNaturalPainterlyBackground()
        {
            var bgRoot = GameObject.Find("09_Natural_Painterly_Background");
            if (bgRoot == null)
            {
                bgRoot = new GameObject("09_Natural_Painterly_Background");
                var env = GameObject.Find("CaMau_1954_Village/Environment");
                if (env != null) bgRoot.transform.SetParent(env.transform, false);
            }

            var texMistyMountains = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/RiceFieldLayers/Layer4_MistyMountains_Alpha.png");
            var texKarstPeaks = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Environment/karst_mountains_lana_clean.png");
            var texGoldenClouds = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Lana_Unified_Art/Clouds/T_Lana_Cloud_04_GoldenHorizon.png");
            var texDistantClouds = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Environment/Sky/Clouds/Distant/T_Cloud_Distant_Horizon03.png");

            // 1. Lớp núi mờ hùng vĩ phía chân trời Bắc (Misty Mountains Panorama):
            // Đặt tại Z = 84m ngay sau cánh đồng lúa (Z=76m), mở rộng 240m che kín toàn bộ chân trời
            CreateBackgroundLayerQuad(bgRoot.transform,
                "BG_Layer_01_MistyMountains_Panorama",
                texMistyMountains,
                new Vector3(0f, 13.50f, 84.00f),
                new Vector3(240f, 28f, 1f),
                new Color(1f, 0.94f, 0.82f, 0.90f) // Ánh hoàng hôn ấm áp hòa quyện với bầu trời
            );

            // 2. Lớp rặng núi đá vôi / ngọn đồi trùng điệp tầng giữa (Karst Peaks Mid):
            // Đặt xen kẽ tại Z = 86m tạo độ sâu thị giác (Atmospheric Depth)
            CreateBackgroundLayerQuad(bgRoot.transform,
                "BG_Layer_02_KarstPeaks_West",
                texKarstPeaks,
                new Vector3(-45f, 11.00f, 86.00f),
                new Vector3(110f, 22f, 1f),
                new Color(0.96f, 0.88f, 0.78f, 0.75f)
            );

            CreateBackgroundLayerQuad(bgRoot.transform,
                "BG_Layer_02_KarstPeaks_East",
                texKarstPeaks,
                new Vector3(45f, 11.00f, 86.00f),
                new Vector3(110f, 22f, 1f),
                new Color(0.96f, 0.88f, 0.80f, 0.75f)
            );

            // 3. Lớp mây hoàng hôn trôi ngang chân trời (Golden Horizon Clouds):
            // Lượn lờ phía trên rặng núi ở độ cao Y = 22m đến 38m
            CreateBackgroundLayerQuad(bgRoot.transform,
                "BG_Layer_03_GoldenClouds_Center",
                texGoldenClouds,
                new Vector3(0f, 26.00f, 87.00f),
                new Vector3(160f, 24f, 1f),
                new Color(1f, 0.96f, 0.88f, 0.85f)
            );

            CreateBackgroundLayerQuad(bgRoot.transform,
                "BG_Layer_03_DistantClouds_Top",
                texDistantClouds,
                new Vector3(0f, 38.00f, 89.00f),
                new Vector3(180f, 22f, 1f),
                new Color(1f, 0.92f, 0.84f, 0.70f)
            );

            Debug.Log("[RefineScene] Đã thiết lập Background tự nhiên đa tầng: Dãy núi sương mờ, đồi đá vôi và dải mây hoàng hôn Nam Bộ!");
        }

        private static void CreateBackgroundLayerQuad(Transform parent, string layerName, Texture2D tex, Vector3 pos, Vector3 scale, Color tintColor)
        {
            if (tex == null) return;

            Transform existing = parent.Find(layerName);
            GameObject quadGo;
            if (existing != null)
            {
                quadGo = existing.gameObject;
            }
            else
            {
                quadGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quadGo.name = layerName;
                quadGo.transform.SetParent(parent, false);
            }

            quadGo.transform.position = pos;
            quadGo.transform.rotation = Quaternion.identity; // Hướng vuông góc với trục Z, nhìn thẳng về phía Nam
            quadGo.transform.localScale = scale;

            var col = quadGo.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            var mr = quadGo.GetComponent<MeshRenderer>();
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");

            var mat = new Material(shader);
            mat.name = $"Mat_{layerName}";
            mat.mainTexture = tex;

            // Thiết lập chế độ Transparent cho URP Unlit
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f); // 1 = Transparent
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f); // 0 = Alpha
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tintColor);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", tintColor);
            if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f); // Two-sided

            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 10;
            mr.sharedMaterial = mat;

            EditorUtility.SetDirty(quadGo);
        }

        // =========================================================================
        // 4. TẠO ĐƯỜNG ĐI TRÊN CÁNH ĐỒNG LÚA ĐỂ NỐI SCENE
        // =========================================================================
        private static void CreateRiceFieldSceneTransitionPath()
        {
            var canhDongRoot = GameObject.Find("06_Canh_Dong");
            if (canhDongRoot == null)
            {
                var villageAreas = GameObject.Find("Village_Areas");
                if (villageAreas != null) canhDongRoot = villageAreas.transform.Find("06_Canh_Dong")?.gameObject;
            }

            Transform parent = canhDongRoot != null ? canhDongRoot.transform : null;

            var matTrail = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Paths/M_Earthen_Trail.mat");
            var matWood = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Wood_Weathered.mat");

            // 1. Đường mòn bờ ruộng chính (Grand Rice Field Highway):
            // Chạy xuyên suốt từ sau Nhà An (Z = 20m) đến tận cuối cánh đồng lúa (Z = 76m), rộng 3.2m
            var mainPath = GameObject.Find("Path_RiceField_Transition_GrandSpine");
            if (mainPath == null)
            {
                mainPath = GameObject.CreatePrimitive(PrimitiveType.Cube);
                mainPath.name = "Path_RiceField_Transition_GrandSpine";
                if (parent != null) mainPath.transform.SetParent(parent, false);
            }
            mainPath.transform.position = new Vector3(0.50f, 0.08f, 48.00f);
            mainPath.transform.localScale = new Vector3(3.20f, 0.16f, 56.00f); // Phủ từ Z = 20m đến Z = 76m
            mainPath.transform.rotation = Quaternion.identity;

            var mrPath = mainPath.GetComponent<MeshRenderer>();
            if (mrPath != null && matTrail != null) mrPath.sharedMaterial = matTrail;

            var colPath = mainPath.GetComponent<BoxCollider>();
            if (colPath != null)
            {
                colPath.center = Vector3.zero;
                colPath.size = Vector3.one;
            }
            EditorUtility.SetDirty(mainPath);

            // 2. Cổng làng / Cột mốc bờ ruộng đánh dấu điểm chuyển cảnh (Transition Landmark Gate):
            // Đặt tại điểm cuối của con đường (Z = 75.0m, X = 0.5m)
            var gateFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Optimized/SM_Cong_lang_BlenderOptimized.fbx");
            var gateGo = GameObject.Find("Gate_RiceField_SceneExit_Landmark");
            if (gateGo == null)
            {
                if (gateFbx != null)
                {
                    gateGo = (GameObject)PrefabUtility.InstantiatePrefab(gateFbx);
                }
                else
                {
                    gateGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                }
                gateGo.name = "Gate_RiceField_SceneExit_Landmark";
                if (parent != null) gateGo.transform.SetParent(parent, false);
            }
            gateGo.transform.position = new Vector3(0.50f, 0.08f, 75.00f);
            gateGo.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            gateGo.transform.localScale = new Vector3(3.80f, 3.80f, 3.80f);
            EditorUtility.SetDirty(gateGo);

            // 3. Vùng kích hoạt chuyển cảnh (Scene Transition Trigger Box):
            // Đặt ngay tại cổng làng (Z = 75.0m), kích thước rộng 6m để người chơi đi qua là kích hoạt mượt mà
            var triggerGo = GameObject.Find("Trigger_SceneTransition_RiceField_Exit");
            if (triggerGo == null)
            {
                triggerGo = new GameObject("Trigger_SceneTransition_RiceField_Exit");
                if (parent != null) triggerGo.transform.SetParent(parent, false);
            }
            triggerGo.transform.position = new Vector3(0.50f, 1.50f, 75.20f);
            triggerGo.transform.rotation = Quaternion.identity;

            var triggerCol = triggerGo.GetComponent<BoxCollider>();
            if (triggerCol == null) triggerCol = triggerGo.AddComponent<BoxCollider>();
            triggerCol.isTrigger = true;
            triggerCol.center = Vector3.zero;
            triggerCol.size = new Vector3(6.00f, 4.00f, 2.50f);

            // Gắn component OverhaulEndMapTransitionTrigger sẵn có của project để kết nối hệ thống BotW Loading Screen
            var transitionComp = triggerGo.GetComponent<HoaVaMau.Gameplay.OverhaulEndMapTransitionTrigger>();
            if (transitionComp == null) transitionComp = triggerGo.AddComponent<HoaVaMau.Gameplay.OverhaulEndMapTransitionTrigger>();

            EditorUtility.SetDirty(triggerGo);

            // 4. Đảm bảo Barrier_North nằm sau Trigger (Z = 78.5m) để An có thể chạm vào Trigger chuyển cảnh
            var t3d = GameObject.Find("CaMau_1954_Village/Environment/Terrain_3D");
            if (t3d == null) t3d = GameObject.Find("Terrain_3D");
            if (t3d != null)
            {
                var bn = t3d.transform.Find("Barrier_North");
                if (bn != null)
                {
                    bn.position = new Vector3(0f, 3f, 78.50f);
                    var colBn = bn.GetComponent<BoxCollider>();
                    if (colBn != null)
                    {
                        colBn.size = new Vector3(120f, 8f, 2f);
                        colBn.center = Vector3.zero;
                    }
                    EditorUtility.SetDirty(bn);
                }
            }

            Debug.Log("[RefineScene] Đã tạo đường nối Scene xuyên suốt cánh đồng lúa từ Z=20m đến Z=76m, kèm Cổng làng và Trigger chuyển Scene!");
        }
    }
}
