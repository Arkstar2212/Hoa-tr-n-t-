#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace HoaVaMau.EditorTools
{
    /// <summary>
    /// Công cụ trích xuất dải ruộng bậc thang màu nước từ tranh Concept gốc (Image 1)
    /// và đưa trực tiếp vào tầm nhìn hiện tại trong Scene của "Hoa trên đất đỏ".
    /// </summary>
    public class ExtractRiceFieldBackdrop : EditorWindow
    {
        public const string ConceptImagePath = @"C:\Users\Thanh Tài\.gemini\antigravity\brain\2d2bb4a1-16ed-41b7-ba92-9bb355dfc687\.user_uploaded\media_1793103937537_b72eb122.jpg";
        public const string TargetDir = "Assets/Textures/Lana_Unified_Art/Rice";
        public const string TargetPngPath = TargetDir + "/T_Concept_RiceTerraces_Watercolor.png";
        public const string BackdropGOName = "[VISTA_TERRACED_RICE_FIELD_BACKDROP]";

        [Header("Tùy chỉnh Cắt Dải Ruộng")]
        [Range(0.15f, 0.45f)] public float cropBottom = 0.28f;
        [Range(0.50f, 0.85f)] public float cropTop = 0.68f;
        [Range(0.01f, 0.15f)] public float featherTop = 0.08f;
        [Range(0.01f, 0.15f)] public float featherBottom = 0.06f;

        [Header("Tọa độ & Kích thước trong Scene")]
        public float posX = 73.0f;
        public float posY = 0.85f;
        public float posZ = 3.5f;
        public float scaleX = 42.0f;
        public float scaleY = 9.5f;
        public int sortingOrder = 2; // Hiển thị phía trước phông nền núi, nhưng sau cây cối tiền cảnh

        [MenuItem("Hoa va Mau/Level Design/Trích xuất Ruộng Bậc Thang từ Tranh Gốc (Extract Concept Rice Field)", false, 10)]
        public static void OpenWindow()
        {
            var win = GetWindow<ExtractRiceFieldBackdrop>("Trích Xuất Ruộng Concept");
            win.minSize = new Vector2(420, 560);
            win.AutoFitScenePosition();
        }

        [MenuItem("Hoa va Mau/Level Design/1-Click Trích Xuất & Đặt Ngay Ruộng Concept (Ngay Tầm Mắt)", false, 11)]
        public static void QuickExtractAndSpawn()
        {
            var extractor = CreateInstance<ExtractRiceFieldBackdrop>();
            extractor.AutoFitScenePosition();
            extractor.ExecuteExtractAndSpawn();
            DestroyImmediate(extractor);
        }

        public void AutoFitScenePosition()
        {
            // 1. Ưu tiên lấy theo vị trí đang nhìn trong Scene View
            if (SceneView.lastActiveSceneView != null)
            {
                posX = SceneView.lastActiveSceneView.pivot.x;
                posY = SceneView.lastActiveSceneView.pivot.y - 0.2f;
                return;
            }

            // 2. Tìm vật thể đang được chọn trong Hierarchy (nếu có)
            if (Selection.activeTransform != null)
            {
                posX = Selection.activeTransform.position.x;
                posY = Selection.activeTransform.position.y + 0.8f;
                return;
            }

            // 3. Tìm nhân vật An hoặc Player
            var player = GameObject.FindWithTag("Player");
            if (player == null) player = GameObject.Find("An");
            if (player == null) player = GameObject.Find("Player");
            if (player == null) player = GameObject.Find("PF_Player_An");

            if (player != null)
            {
                posX = player.transform.position.x;
                posY = player.transform.position.y + 1.2f;
                return;
            }

            // 4. Tìm các cây đặc trưng ở khu vực đồng lúa (Bamboo / Coconut / Rice)
            var bamboo = GameObject.Find("RiceField_Horizon_Bamboo1");
            if (bamboo == null) bamboo = GameObject.Find("PF_LanaVN_Tree_07_BambooGrove");
            if (bamboo != null)
            {
                posX = bamboo.transform.position.x - 3.0f;
                posY = bamboo.transform.position.y + 1.5f;
                return;
            }

            // Mặc định khu vực Cánh đồng lúa Zone 3 của game
            posX = 73.0f;
            posY = -0.5f;
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("🌾 TRÍCH XUẤT RUỘNG BẬC THANG TỪ TRANH CONCEPT", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("File ảnh PNG đã được tạo sẵn tại:\nAssets/Textures/Lana_Unified_Art/Rice/T_Concept_RiceTerraces_Watercolor.png\nBạn có thể bấm nút bên dưới để đặt tự động, hoặc kéo thả file này từ Project vào Scene!", MessageType.Info);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("1. Căn Tọa Độ Vào Tầm Mắt", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🎯 Căn Theo Góc Nhìn Scene View Hiện Tại"))
            {
                if (SceneView.lastActiveSceneView != null)
                {
                    posX = SceneView.lastActiveSceneView.pivot.x;
                    posY = SceneView.lastActiveSceneView.pivot.y - 0.5f;
                }
            }
            if (GUILayout.Button("📍 Căn Theo Vật Thể Đang Chọn"))
            {
                if (Selection.activeTransform != null)
                {
                    posX = Selection.activeTransform.position.x;
                    posY = Selection.activeTransform.position.y;
                }
            }
            EditorGUILayout.EndHorizontal();

            posX = EditorGUILayout.FloatField("Vị trí X (Trục ngang):", posX);
            posY = EditorGUILayout.FloatField("Vị trí Y (Độ cao):", posY);
            posZ = EditorGUILayout.FloatField("Độ sâu Z (Sau lối đi):", posZ);
            scaleX = EditorGUILayout.FloatField("Chiều rộng (Scale X):", scaleX);
            scaleY = EditorGUILayout.FloatField("Chiều cao (Scale Y):", scaleY);
            sortingOrder = EditorGUILayout.IntField("Sorting Order (Mặc định = 2):", sortingOrder);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("2. Tinh Chỉnh Cắt Ảnh (Nâng Cao)", EditorStyles.boldLabel);
            cropBottom = EditorGUILayout.Slider("Mép dưới:", cropBottom, 0.15f, 0.45f);
            cropTop = EditorGUILayout.Slider("Mép trên:", cropTop, 0.50f, 0.85f);
            featherTop = EditorGUILayout.Slider("Độ nhòe mép trên:", featherTop, 0.01f, 0.15f);
            featherBottom = EditorGUILayout.Slider("Độ nhòe mép dưới:", featherBottom, 0.01f, 0.15f);

            EditorGUILayout.Space(12);
            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.4f, 1f);
            if (GUILayout.Button("🎨 ĐẶT DẢI RUỘNG VÀO SCENE NGAY TẦM MẮT", GUILayout.Height(38)))
            {
                ExecuteExtractAndSpawn();
            }

            GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f, 1f);
            if (GUILayout.Button("🗑️ XÓA DẢI RUỘNG KHỎI SCENE", GUILayout.Height(26)))
            {
                ClearBackdrop();
            }
            GUI.backgroundColor = Color.white;
        }

        public void ClearBackdrop()
        {
            var go = GameObject.Find(BackdropGOName);
            if (go != null)
            {
                Undo.DestroyObjectImmediate(go);
                Debug.Log("<color=#10b981>[ExtractRiceField]</color> Đã xóa dải ruộng khỏi Scene.");
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
        }

        public void ExecuteExtractAndSpawn()
        {
            if (!File.Exists(ConceptImagePath))
            {
                Debug.LogError($"[ExtractRiceField] Không tìm thấy file ảnh concept tại: {ConceptImagePath}");
                return;
            }

            // Đọc và cắt ảnh nếu chưa có hoặc cập nhật
            byte[] rawBytes = File.ReadAllBytes(ConceptImagePath);
            Texture2D srcTex = new Texture2D(2, 2);
            srcTex.LoadImage(rawBytes);

            int w = srcTex.width;
            int h = srcTex.height;

            int startY = Mathf.FloorToInt(h * cropBottom);
            int endY = Mathf.CeilToInt(h * cropTop);
            int cropHeight = endY - startY;

            Texture2D outTex = new Texture2D(w, cropHeight, TextureFormat.RGBA32, false);
            Color[] srcPixels = srcTex.GetPixels(0, startY, w, cropHeight);
            Color[] outPixels = new Color[srcPixels.Length];

            float topFeatherPx = cropHeight * (featherTop / (cropTop - cropBottom));
            float botFeatherPx = cropHeight * (featherBottom / (cropTop - cropBottom));

            for (int y = 0; y < cropHeight; y++)
            {
                float alpha = 1.0f;
                if (y < botFeatherPx)
                {
                    alpha = Mathf.SmoothStep(0f, 1f, y / botFeatherPx);
                }
                else if (y > cropHeight - topFeatherPx)
                {
                    alpha = Mathf.SmoothStep(1f, 0f, (y - (cropHeight - topFeatherPx)) / topFeatherPx);
                }

                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;
                    Color c = srcPixels[idx];
                    c.a *= alpha;
                    outPixels[idx] = c;
                }
            }

            outTex.SetPixels(outPixels);
            outTex.Apply();

            if (!Directory.Exists(TargetDir)) Directory.CreateDirectory(TargetDir);
            byte[] pngBytes = outTex.EncodeToPNG();
            File.WriteAllBytes(TargetPngPath, pngBytes);
            AssetDatabase.ImportAsset(TargetPngPath, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(TargetPngPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.maxTextureSize = 2048;
                importer.SaveAndReimport();
            }

            ClearBackdrop();

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(TargetPngPath);
            if (sprite == null)
            {
                Debug.LogError($"[ExtractRiceField] Không thể tải Sprite sau khi import tại: {TargetPngPath}");
                return;
            }

            var backdropGO = new GameObject(BackdropGOName);
            Undo.RegisterCreatedObjectUndo(backdropGO, "Spawn Concept Rice Terraces Backdrop");

            backdropGO.transform.position = new Vector3(posX, posY, posZ);
            backdropGO.transform.localScale = new Vector3(scaleX, scaleY, 1.0f);

            var sr = backdropGO.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;

            Material mat = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
            if (mat.shader == null || mat.shader.name == "Hidden/InternalErrorShader")
            {
                mat = new Material(Shader.Find("Sprites/Default"));
            }
            sr.sharedMaterial = mat;

            Selection.activeGameObject = backdropGO;
            EditorGUIUtility.PingObject(backdropGO);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            SceneView.RepaintAll();
            Debug.Log($"<color=#10b981>[ExtractRiceField]</color> ĐÃ ĐẶT DẢI RUỘNG BẬC THANG TẠI TỌA ĐỘ: X={posX:F1}, Y={posY:F1}, Z={posZ:F1}, SortingOrder={sortingOrder}");
        }
    }
}
#endif
