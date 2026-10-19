#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

namespace HoaVaMau.Editor
{
    /// <summary>
    /// Art Standardization Benchmark & Diagnostic Tool.
    /// Cho phép kiểm chứng an toàn 1-click giữa phong cách CG gốc và Painterly chuẩn hóa
    /// trên chính nhân vật Minh (Player_Minh) và cụm lúa so sánh 2D/3D.
    /// </summary>
    public static class ArtStandardizationBenchmark
    {
        private const string MinhMaterialPath = "Assets/TripoModels/Boy_Character/Materials/M_Boy_Character.mat";
        private const string MinhMetallicTexturePath = "Assets/TripoModels/Boy_Character/Boy_Character.fbm/Boy_Character_metallic_Smoothness.png";
        private const string RiceMaterialPath = "Assets/Materials/RicePlants/Mat_Rice_CrossQuad_Painterly_Test.mat";
        private const string Rice3DPrefabPath = "Assets/Prefabs/Environment/Rice/PF_RiceClump_Full.prefab";
        private const string DockTimberMaterialPath = "Assets/Materials/Environment/CaMau/M_CaMau_Dock_Timber_Painterly.mat";
        private const string PanoramaSkyMaterialPath = "Assets/Materials/Environment/Backdrop_Sky/M_Sunset_Sea_Horizon_Panorama.mat";
        private const string GradientSkyMaterialPath = "Assets/Materials/Environment/Backdrop_Sky/M_StylizedSky_Sunset.mat";

        // =========================================================================
        // 1. CHUYỂN NHÂN VẬT MINH SANG PHONG CÁCH PAINTERLY (KHỬ BÓNG NHỰA CG)
        // =========================================================================
        [MenuItem("Hoa va Mau/Art Standardization/1. Chuyen Minh sang Painterly (Khu Bong Nhua)", false, 10)]
        public static void ApplyPainterlyToMinh()
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(MinhMaterialPath);
            if (mat == null)
            {
                Debug.LogError($"[ArtStandardization] Khong tim thay Material tai: {MinhMaterialPath}");
                return;
            }

            Undo.RecordObject(mat, "Apply Painterly to Minh");

            // Tắt hoàn toàn tính kim loại và phản xạ gương
            mat.SetFloat("_Metallic", 0.0f);
            mat.SetFloat("_Smoothness", 0.08f);
            mat.SetFloat("_SpecularHighlights", 0.0f);
            mat.SetFloat("_EnvironmentReflections", 0.0f);

            // Làm dịu normal map gồ ghề của AI (từ 1.0 xuống 0.28 để bề mặt êm mịn như màu nước)
            if (mat.HasProperty("_BumpScale"))
            {
                mat.SetFloat("_BumpScale", 0.28f);
            }

            // Tháo bỏ MetallicGlossMap (AI bake tạo đốm phản quang loang lổ)
            mat.DisableKeyword("_METALLICSPECGLOSSMAP");
            mat.SetTexture("_MetallicGlossMap", null);

            // Sắc thái đón nắng hoàng hôn dịu nhẹ
            mat.SetColor("_BaseColor", new Color(1.0f, 0.96f, 0.92f, 1.0f));

            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();

            Debug.Log("<color=#40b8a6>[Art Standardization]</color> <b>ĐÃ CHUYỂN MINH SANG PAINTERLY:</b> Metallic=0, Smoothness=0.08, Specular=Off, BumpScale=0.28. Minh giờ đây có độ mờ mịn màng, không còn bóng nhựa CG!");
        }

        // =========================================================================
        // 2. KHÔI PHỤC MINH VỀ TRẠNG THÁI GỐC (REVERT TO ORIGINAL CG)
        // =========================================================================
        [MenuItem("Hoa va Mau/Art Standardization/2. Khoi phuc Minh ve CG Goc (Revert)", false, 11)]
        public static void RevertMinhToOriginal()
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(MinhMaterialPath);
            if (mat == null)
            {
                Debug.LogError($"[ArtStandardization] Khong tim thay Material tai: {MinhMaterialPath}");
                return;
            }

            Undo.RecordObject(mat, "Revert Minh to Original");

            Texture metallicTex = AssetDatabase.LoadAssetAtPath<Texture>(MinhMetallicTexturePath);

            mat.SetFloat("_Metallic", 1.0f);
            mat.SetFloat("_Smoothness", 0.50f);
            mat.SetFloat("_SpecularHighlights", 1.0f);
            mat.SetFloat("_EnvironmentReflections", 1.0f);

            if (mat.HasProperty("_BumpScale"))
            {
                mat.SetFloat("_BumpScale", 1.0f);
            }

            if (metallicTex != null)
            {
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");
                mat.SetTexture("_MetallicGlossMap", metallicTex);
            }

            mat.SetColor("_BaseColor", Color.white);

            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();

            Debug.Log("<color=#e67e22>[Art Standardization]</color> <b>ĐÃ KHÔI PHỤC MINH VỀ GỐC:</b> Metallic=1.0, Smoothness=0.50, MetallicMap=On.");
        }

        // =========================================================================
        // 3. TẠO CỤM SO SÁNH LÚA 2D CROSS-QUAD VS 3D TRONG SCENE HIỆN TẠI
        // =========================================================================
        [MenuItem("Hoa va Mau/Art Standardization/3. Tao Cum So Sanh Lua 2D vs 3D (Side-by-Side)", false, 20)]
        public static void SpawnRiceComparisonBenchmark()
        {
            const string BenchmarkRootName = "[ART_BENCHMARK_RICE_COMPARISON]";

            // Xóa cụm cũ nếu đã tồn tại để tránh trùng lặp
            GameObject existingRoot = GameObject.Find(BenchmarkRootName);
            if (existingRoot != null)
            {
                Undo.DestroyObjectImmediate(existingRoot);
            }

            // Tìm vị trí của Player_Minh để đặt cụm thử nghiệm ngay cạnh tầm nhìn
            Vector3 spawnBase = new Vector3(-6.0f, -0.48f, 0.4f);
            GameObject player = GameObject.Find("Player_Minh");
            if (player != null)
            {
                spawnBase = player.transform.position + new Vector3(2.5f, 0f, 0.35f);
            }

            GameObject rootObj = new GameObject(BenchmarkRootName);
            Undo.RegisterCreatedObjectUndo(rootObj, "Create Rice Benchmark Root");
            rootObj.transform.position = spawnBase;

            // --- BÊN TRÁI: CỤM LÚA 3D HIỆN TẠI (EXISTING 3D RICE) ---
            GameObject prefab3D = AssetDatabase.LoadAssetAtPath<GameObject>(Rice3DPrefabPath);
            if (prefab3D != null)
            {
                GameObject rice3D = (GameObject)PrefabUtility.InstantiatePrefab(prefab3D, rootObj.transform);
                rice3D.name = "A_Existing_3D_Rice_Mesh";
                rice3D.transform.localPosition = new Vector3(-1.2f, 0f, 0f);
                rice3D.transform.localScale = Vector3.one * 1.0f;
            }

            // --- BÊN PHẢI: CỤM LÚA 2.5D CROSS-QUAD MỚI (PAINTERLY HAND-PAINTED) ---
            Material riceMat = AssetDatabase.LoadAssetAtPath<Material>(RiceMaterialPath);
            if (riceMat != null)
            {
                GameObject rice2D = CreateTriQuadRiceCluster("B_New_Painterly_Rice_TriQuad", riceMat);
                rice2D.transform.SetParent(rootObj.transform, false);
                rice2D.transform.localPosition = new Vector3(1.2f, 0f, 0f);
                rice2D.transform.localScale = Vector3.one * 1.35f;
            }

            Selection.activeGameObject = rootObj;
            Debug.Log("<color=#40b8a6>[Art Standardization]</color> <b>ĐÃ TẠO CỤM SO SÁNH LÚA:</b>\n" +
                      $"• Bên trái: Lúa 3D hiện tại (Mesh)\n" +
                      $"• Bên phải: Lúa 2.5D Tri-Quad mới (Nét cọ Concept + Stylized Grass Shader)\n" +
                      $"Vị trí đặt: {spawnBase}. Mở Game View để so sánh trực quan!");
        }

        // =========================================================================
        // 4. DỌN DẸP CỤM SO SÁNH LÚA
        // =========================================================================
        [MenuItem("Hoa va Mau/Art Standardization/4. Xoa Cum So Sanh Lua (Clean)", false, 21)]
        public static void CleanRiceComparisonBenchmark()
        {
            GameObject existingRoot = GameObject.Find("[ART_BENCHMARK_RICE_COMPARISON]");
            if (existingRoot != null)
            {
                Undo.DestroyObjectImmediate(existingRoot);
                Debug.Log("<color=#40b8a6>[Art Standardization]</color> Đã xóa cụm so sánh lúa an toàn!");
            }
            else
            {
                Debug.Log("[Art Standardization] Không tìm thấy cụm so sánh nào cần xóa.");
            }
        }

        // =========================================================================
        // HELPER: TẠO MESH TRI-QUAD GIAO NHAU 3 HƯỚNG (GÓC 60 ĐỘ) CHO LÚA 2.5D
        // =========================================================================
        private static GameObject CreateTriQuadRiceCluster(string name, Material mat)
        {
            GameObject obj = new GameObject(name);
            MeshFilter mf = obj.AddComponent<MeshFilter>();
            MeshRenderer mr = obj.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;

            Mesh mesh = new Mesh { name = "Rice_TriQuad_Mesh" };

            // 3 mặt phẳng quay quanh trục Y (0°, 60°, 120°) để không bao giờ bị dẹp cạnh ở góc nhìn 2.5D
            float width = 0.85f;
            float height = 1.25f;
            float halfW = width * 0.5f;

            float[] angles = { 0f, 60f, 120f };
            Vector3[] verts = new Vector3[angles.Length * 4];
            Vector2[] uvs = new Vector2[angles.Length * 4];
            Vector3[] normals = new Vector3[angles.Length * 4];
            int[] tris = new int[angles.Length * 6];

            for (int i = 0; i < angles.Length; i++)
            {
                float rad = angles[i] * Mathf.Deg2Rad;
                Vector3 right = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * halfW;
                Vector3 normal = new Vector3(-Mathf.Sin(rad), 0.35f, Mathf.Cos(rad)).normalized; // hướng nhẹ lên trời để đón ánh hoàng hôn

                int vIdx = i * 4;
                verts[vIdx + 0] = -right;                     // đáy trái (Y = 0)
                verts[vIdx + 1] = right;                      // đáy phải (Y = 0)
                verts[vIdx + 2] = -right + Vector3.up * height; // đỉnh trái
                verts[vIdx + 3] = right + Vector3.up * height;  // đỉnh phải

                uvs[vIdx + 0] = new Vector2(0f, 0f);
                uvs[vIdx + 1] = new Vector2(1f, 0f);
                uvs[vIdx + 2] = new Vector2(0f, 1f);
                uvs[vIdx + 3] = new Vector2(1f, 1f);

                normals[vIdx + 0] = normal;
                normals[vIdx + 1] = normal;
                normals[vIdx + 2] = normal;
                normals[vIdx + 3] = normal;

                int tIdx = i * 6;
                // Mặt trước
                tris[tIdx + 0] = vIdx + 0;
                tris[tIdx + 1] = vIdx + 2;
                tris[tIdx + 2] = vIdx + 1;
                tris[tIdx + 3] = vIdx + 1;
                tris[tIdx + 4] = vIdx + 2;
                tris[tIdx + 5] = vIdx + 3;
            }

            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.normals = normals;
            mesh.triangles = tris;
            mesh.RecalculateBounds();

            mf.sharedMesh = mesh;
            return obj;
        }

        // =========================================================================
        // 5. CHUẨN HÓA GỖ CẦU CẢNG (WEATHERED TIMBER)
        // =========================================================================
        [MenuItem("Hoa va Mau/Art Standardization/5. Chuan Hoa Go Cau Cang (Weathered Timber)", false, 30)]
        public static void ApplyStandardizedTimberToDock()
        {
            Material timberMat = AssetDatabase.LoadAssetAtPath<Material>(DockTimberMaterialPath);
            if (timberMat == null)
            {
                Debug.LogError($"[ArtStandardization] Không tìm thấy Timber Material tại: {DockTimberMaterialPath}");
                return;
            }

            GameObject[] allObjects = Object.FindObjectsByType<GameObject>();
            int count = 0;
            foreach (var go in allObjects)
            {
                if (go.name.Contains("Dock_Timber_Beam") || go.name.Contains("Pier_Timber") || go.name == "Walkable_Platform")
                {
                    MeshRenderer mr = go.GetComponent<MeshRenderer>();
                    if (mr != null)
                    {
                        Undo.RecordObject(mr, "Standardize Timber Material");
                        mr.sharedMaterial = timberMat;
                        count++;
                    }
                }
            }

            Debug.Log($"<color=#40b8a6>[Art Standardization]</color> <b>ĐÃ CHUẨN HÓA GỖ CẦU CẢNG:</b> Gán {timberMat.name} (Smoothness=0.09, Metallic=0, tone nâu sẫm ấm) cho {count} dầm gỗ cầu cảng!");
        }

        // =========================================================================
        // 6. BẬT NỀN TRỜI BIỂN HOÀNG HÔN SIÊU RỘNG (PANORAMA SKY - HÌNH 4)
        // =========================================================================
        [MenuItem("Hoa va Mau/Art Standardization/6. Bat Nen Troi Panorama Bien Hoang Hon (Hinh 4)", false, 40)]
        public static void ApplySunsetPanoramaSky()
        {
            Material panoMat = AssetDatabase.LoadAssetAtPath<Material>(PanoramaSkyMaterialPath);
            if (panoMat == null)
            {
                Debug.LogError($"[ArtStandardization] Không tìm thấy Panorama Material tại: {PanoramaSkyMaterialPath}");
                return;
            }

            GameObject skyObj = GameObject.Find("Sky_Backdrop");
            if (skyObj == null)
            {
                skyObj = GameObject.Find("Background_SkyBackdrop");
            }

            if (skyObj != null)
            {
                MeshRenderer mr = skyObj.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    Undo.RecordObject(mr, "Apply Panorama Sky Material");
                    mr.sharedMaterial = panoMat;
                    Debug.Log("<color=#40b8a6>[Art Standardization]</color> <b>ĐÃ BẬT NỀN TRỜI PANORAMA BIỂN HOÀNG HÔN (HÌNH 4):</b> Chân trời biển rộng, rặng cây xa xăm và màu hoàng hôn rực rỡ!");
                    return;
                }
            }

            Debug.LogWarning("[Art Standardization] Không tìm thấy Sky_Backdrop trong scene. Đang tạo mới plane Parallax...");
            GameObject newSky = GameObject.CreatePrimitive(PrimitiveType.Quad);
            newSky.name = "Sky_Backdrop_Panorama_Painterly";
            newSky.transform.position = new Vector3(2f, 20f, 110f);
            newSky.transform.localScale = new Vector3(280f, 80f, 1f);
            newSky.GetComponent<MeshRenderer>().sharedMaterial = panoMat;
            Object.DestroyImmediate(newSky.GetComponent<Collider>());
            Undo.RegisterCreatedObjectUndo(newSky, "Create Panorama Sky Plane");
            Debug.Log("<color=#40b8a6>[Art Standardization]</color> <b>ĐÃ TẠO MỚI NỀN TRỜI PANORAMA BIỂN HOÀNG HÔN!</b>");
        }

        // =========================================================================
        // 7. KHÔI PHỤC NỀN TRỜI GRADIENT CŨ
        // =========================================================================
        [MenuItem("Hoa va Mau/Art Standardization/7. Khoi Phuc Nen Troi Gradient Cu", false, 41)]
        public static void RevertGradientSky()
        {
            Material gradMat = AssetDatabase.LoadAssetAtPath<Material>(GradientSkyMaterialPath);
            if (gradMat == null) return;

            GameObject skyObj = GameObject.Find("Sky_Backdrop");
            if (skyObj == null) skyObj = GameObject.Find("Background_SkyBackdrop");
            if (skyObj != null)
            {
                MeshRenderer mr = skyObj.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    Undo.RecordObject(mr, "Revert Gradient Sky");
                    mr.sharedMaterial = gradMat;
                    Debug.Log("<color=#e67e22>[Art Standardization]</color> Đã khôi phục nền trời Gradient cũ.");
                }
            }
        }

        // =========================================================================
        // 8. CĂN CHỈNH CAMERA CINEMATIC 2.5D (FOCUS MINH & TOÀN CẢNH HOÀNG HÔN)
        // =========================================================================
        [MenuItem("Hoa va Mau/Art Standardization/8. Can Chinh Camera Cinematic 2.5D (Focus Minh)", false, 50)]
        public static void AlignCinematicCamera()
        {
            // 1. Reset Camera Parent position
            var camParent = GameObject.Find("CAMAU_REGROUPMENT_1954/Camera");
            if (camParent != null)
            {
                Undo.RecordObject(camParent.transform, "Reset Camera Parent Pos");
                camParent.transform.localPosition = Vector3.zero;
            }

            // 2. Align Main Camera
            var cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("[ArtStandardization] Không tìm thấy Camera.main!");
                return;
            }

            Undo.RecordObject(cam.transform, "Align Main Camera Transform");
            Undo.RecordObject(cam, "Align Main Camera Component");

            cam.transform.localPosition = new Vector3(-11.6f, 2.2f, -13.5f);
            cam.transform.localRotation = Quaternion.identity;
            cam.fieldOfView = 32f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 300f;

            // 3. Configure Cinematic2DCamera
            var cine = cam.GetComponent<HoaVaMau.Gameplay.Cinematic2DCamera>();
            if (cine != null)
            {
                Undo.RecordObject(cine, "Configure Cinematic2DCamera");
                var so = new SerializedObject(cine);
                so.FindProperty("cameraOffset2D").vector2Value = new Vector2(0f, 1.35f);
                so.FindProperty("lookAheadDistance").floatValue = 1.2f;
                so.FindProperty("smoothTimeX").floatValue = 0.15f;
                so.FindProperty("smoothTimeY").floatValue = 0.20f;
                so.FindProperty("lookAheadSmoothTime").floatValue = 0.25f;
                so.FindProperty("fixedZ").floatValue = -13.5f;
                so.FindProperty("clampCameraBounds").boolValue = true;
                so.FindProperty("minCameraX").floatValue = -12.5f;
                so.FindProperty("maxCameraX").floatValue = 18.5f;
                so.FindProperty("minCameraY").floatValue = 5.0f;
                so.FindProperty("defaultFov").floatValue = 32f;
                so.ApplyModifiedProperties();
            }

            // 4. Clean up extraneous crate cameras
            Camera[] allCams = Object.FindObjectsByType<Camera>();
            foreach (var c in allCams)
            {
                if (c.tag != "MainCamera" && c.name == "Camera")
                {
                    c.enabled = false;
                }
            }

            // 5. Framing foreground mangrove tree
            var fgTree = GameObject.Find("FG_Mangrove_Root_Left");
            if (fgTree != null)
            {
                Undo.RecordObject(fgTree.transform, "Adjust FG Mangrove Root Framing");
                fgTree.transform.position = new Vector3(-15.8f, 5.2f, -6.5f);
                fgTree.transform.localScale = new Vector3(2.5f, 2.5f, 1.0f);
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            Debug.Log("<color=#40b8a6>[Art Standardization]</color> <b>ĐÃ CĂN CHỈNH CAMERA CINEMATIC 2.5D:</b>\n" +
                      "• Parent Camera reset về (0,0,0) (khắc phục trôi Y=59.46).\n" +
                      "• Focus chính xác vào Minh tại spawn (-12.8, 4.85) với Look-Ahead 1.2m.\n" +
                      "• Khung hình tỷ lệ vàng 1/3 (Cầu cảng ở đáy, Nhân vật & Tàu Kilinski ở giữa, Nắng hoàng hôn ở trên).\n" +
                      "• Distance Z = -13.5, FOV = 32, Clamp X = [-12.5, 18.5].");
        }

        // =========================================================================
        // 9. KHÔI PHỤC CAMERA VỀ MẶC ĐỊNH
        // =========================================================================
        [MenuItem("Hoa va Mau/Art Standardization/9. Khoi Phuc Camera Ve Mac Dinh", false, 51)]
        public static void RevertCameraToOriginal()
        {
            var cam = Camera.main;
            if (cam == null) return;

            Undo.RecordObject(cam.transform, "Revert Camera");
            cam.transform.localPosition = new Vector3(-10f, 2.2f, -11.5f);
            cam.fieldOfView = 36f;

            var cine = cam.GetComponent<HoaVaMau.Gameplay.Cinematic2DCamera>();
            if (cine != null)
            {
                Undo.RecordObject(cine, "Revert Cinematic2DCamera");
                var so = new SerializedObject(cine);
                so.FindProperty("cameraOffset2D").vector2Value = new Vector2(0f, 1.5f);
                so.FindProperty("fixedZ").floatValue = -11.5f;
                so.FindProperty("minCameraX").floatValue = -10.0f;
                so.FindProperty("maxCameraX").floatValue = 18.0f;
                so.FindProperty("defaultFov").floatValue = 36f;
                so.ApplyModifiedProperties();
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            Debug.Log("<color=#e67e22>[Art Standardization]</color> Đã khôi phục thông số Camera về mặc định cũ.");
        }
    }
}
#endif
