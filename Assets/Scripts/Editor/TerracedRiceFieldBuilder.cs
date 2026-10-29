#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;

namespace HoaVaMau.EditorTools
{
    /// <summary>
    /// Công cụ sinh Ruộng Bậc Thang 2D/2.5D tự động cho dự án "Hoa trên đất đỏ".
    /// 
    /// ĐẶC TÍNH THIẾT KẾ:
    /// 1. Tự động sinh 3 tầng bậc thang uốn lượn mềm mại (bờ ruộng gợn sóng tự nhiên).
    /// 2. Tự tính toán độ sâu trục Z và hòa sắc sương mù (Atmospheric Fog Tint) theo quy chuẩn Nam Bộ.
    /// 3. Tạo gờ bờ ruộng đắp đất ngăn bờ (Earthen Ridge Berm) tạo rõ hình khối bậc thang.
    /// 4. Additive 100%: Gom toàn bộ vào GameObject "[TERRACED_RICE_FIELDS]", không chạm vào cây cối hay nhân vật hiện có.
    /// </summary>
    public class TerracedRiceFieldBuilder : EditorWindow
    {
        public const string RootContainerName = "[TERRACED_RICE_FIELDS]";
        private const string GenMeshDir = "Assets/Materials/LanaOverhaul/GeneratedMeshes";
        private const string RiceTexDir = "Assets/Textures/Lana_Unified_Art/Rice";

        [Header("Tọa độ & Phạm vi")]
        public float startX = -35.0f;
        public float endX = 35.0f;
        public float baseY = -1.85f;

        [Header("Độ cong & Uốn lượn")]
        [Range(0.05f, 0.8f)] public float waveAmplitude = 0.22f;
        [Range(0.05f, 0.5f)] public float waveFrequency = 0.16f;
        [Range(-0.05f, 0.05f)] public float hillSlope = 0.012f;

        [Header("Cấu hình 3 Tầng Bậc Thang")]
        // Tầng 1: Ruộng gần (Vàng lúa chín ấm áp)
        public float tier1_Z = 1.6f;
        public float tier1_YOffset = 0.15f;
        public Color tier1_Color = new Color(0.88f, 0.74f, 0.31f, 1.0f); // Vàng chín #E0BD4E
        public Color tier1_RidgeColor = new Color(0.35f, 0.30f, 0.16f, 1.0f); // Bờ đất nâu cỏ

        // Tầng 2: Ruộng giữa (Xanh mạ non tươi tắn)
        public float tier2_Z = 4.2f;
        public float tier2_YOffset = 0.85f;
        public Color tier2_Color = new Color(0.58f, 0.71f, 0.29f, 1.0f); // Xanh mạ #94B54B
        public Color tier2_RidgeColor = new Color(0.26f, 0.32f, 0.15f, 1.0f);

        // Tầng 3: Ruộng xa (Pha sương lam chân núi)
        public float tier3_Z = 7.5f;
        public float tier3_YOffset = 1.65f;
        public Color tier3_Color = new Color(0.39f, 0.56f, 0.50f, 1.0f); // Xanh sương mù #628E80
        public Color tier3_RidgeColor = new Color(0.20f, 0.28f, 0.25f, 1.0f);

        [Header("Trang trí")]
        public bool spawnRiceTuftsAlongRidge = true;

        private Vector2 scrollPos;

        [MenuItem("Hoa va Mau/Level Design/Tạo Ruộng Bậc Thang (Terraced Rice Fields Tool)", false, 20)]
        public static void ShowWindow()
        {
            var win = GetWindow<TerracedRiceFieldBuilder>("Ruộng Bậc Thang");
            win.minSize = new Vector2(380, 560);
            win.AutoDetectFromScene();
        }

        [MenuItem("Hoa va Mau/Level Design/1-Click Sinh Nhanh Ruộng Bậc Thang (Quick Generate)", false, 21)]
        public static void QuickGenerate()
        {
            var builder = CreateInstance<TerracedRiceFieldBuilder>();
            builder.AutoDetectFromScene();
            builder.GenerateTerraces();
            DestroyImmediate(builder);
        }

        private void AutoDetectFromScene()
        {
            // Tự động tìm nhân vật hoặc camera để định vị tọa độ X, Y phù hợp nhất
            var player = GameObject.FindWithTag("Player");
            if (player == null) player = GameObject.Find("An");
            if (player == null) player = GameObject.Find("Player");

            if (player != null)
            {
                float px = player.transform.position.x;
                float py = player.transform.position.y;
                startX = px - 35.0f;
                endX = px + 35.0f;
                baseY = py;
            }
            else if (Camera.main != null)
            {
                float cx = Camera.main.transform.position.x;
                float cy = Camera.main.transform.position.y;
                startX = cx - 35.0f;
                endX = cx + 35.0f;
                baseY = cy - 2.0f;
            }
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("🌾 BỘ SINH RUỘNG BẬC THANG (TERRACED RICE FIELDS)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Tự động tạo 3 tầng ruộng bậc thang uốn lượn có chiều sâu 2.5D, lấp kín khoảng trống giữa lối đi và dãy núi xa.", MessageType.Info);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("1. Phạm vi & Tọa độ", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🎯 Căn tự động theo Player / Camera"))
            {
                AutoDetectFromScene();
            }
            EditorGUILayout.EndHorizontal();

            startX = EditorGUILayout.FloatField("Start X (Bắt đầu):", startX);
            endX = EditorGUILayout.FloatField("End X (Kết thúc):", endX);
            baseY = EditorGUILayout.FloatField("Base Y (Cao độ nền):", baseY);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("2. Độ cong & Dáng ruộng", EditorStyles.boldLabel);
            waveAmplitude = EditorGUILayout.Slider("Độ dợn sóng (Amplitude):", waveAmplitude, 0.05f, 0.6f);
            waveFrequency = EditorGUILayout.Slider("Tần số sóng (Frequency):", waveFrequency, 0.05f, 0.4f);
            hillSlope = EditorGUILayout.Slider("Độ dốc thoai thoải (Slope):", hillSlope, -0.05f, 0.05f);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("3. Cấu hình 3 Tầng Ruộng", EditorStyles.boldLabel);

            // Tầng 1
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Tầng 1 (Gần - Lúa chín vàng):", EditorStyles.boldLabel);
            tier1_Z = EditorGUILayout.FloatField("Độ sâu Z:", tier1_Z);
            tier1_YOffset = EditorGUILayout.FloatField("Độ cao Y:", tier1_YOffset);
            tier1_Color = EditorGUILayout.ColorField("Màu thảm lúa:", tier1_Color);
            tier1_RidgeColor = EditorGUILayout.ColorField("Màu gờ bờ ruộng:", tier1_RidgeColor);
            EditorGUILayout.EndVertical();

            // Tầng 2
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Tầng 2 (Trung cảnh - Mạ non xanh):", EditorStyles.boldLabel);
            tier2_Z = EditorGUILayout.FloatField("Độ sâu Z:", tier2_Z);
            tier2_YOffset = EditorGUILayout.FloatField("Độ cao Y:", tier2_YOffset);
            tier2_Color = EditorGUILayout.ColorField("Màu thảm lúa:", tier2_Color);
            tier2_RidgeColor = EditorGUILayout.ColorField("Màu gờ bờ ruộng:", tier2_RidgeColor);
            EditorGUILayout.EndVertical();

            // Tầng 3
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Tầng 3 (Viễn cảnh - Xanh sương mù):", EditorStyles.boldLabel);
            tier3_Z = EditorGUILayout.FloatField("Độ sâu Z:", tier3_Z);
            tier3_YOffset = EditorGUILayout.FloatField("Độ cao Y:", tier3_YOffset);
            tier3_Color = EditorGUILayout.ColorField("Màu thảm lúa:", tier3_Color);
            tier3_RidgeColor = EditorGUILayout.ColorField("Màu gờ bờ ruộng:", tier3_RidgeColor);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("4. Chi tiết trang trí", EditorStyles.boldLabel);
            spawnRiceTuftsAlongRidge = EditorGUILayout.Toggle("Cắm khóm lúa dọc bờ gờ", spawnRiceTuftsAlongRidge);

            EditorGUILayout.Space(12);
            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.4f, 1f);
            if (GUILayout.Button("✨ TẠO / CẬP NHẬT RUỘNG BẬC THANG", GUILayout.Height(38)))
            {
                GenerateTerraces();
            }

            GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f, 1f);
            if (GUILayout.Button("🗑️ XÓA RUỘNG BẬC THANG", GUILayout.Height(26)))
            {
                ClearTerraces();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndScrollView();
        }

        public void ClearTerraces()
        {
            var existing = GameObject.Find(RootContainerName);
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing);
                Debug.Log("<color=#10b981>[TerracedRiceFields]</color> Đã xóa toàn bộ cụm ruộng bậc thang thành công.");
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
        }

        public void GenerateTerraces()
        {
            if (!Directory.Exists(GenMeshDir))
            {
                Directory.CreateDirectory(GenMeshDir);
            }

            // Xóa cụm cũ nếu có
            ClearTerraces();

            var root = new GameObject(RootContainerName);
            Undo.RegisterCreatedObjectUndo(root, "Generate Terraced Rice Fields");

            Material grassCapMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/LanaOverhaul/M_Lana_GrassCap.mat");
            if (grassCapMat == null)
            {
                grassCapMat = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default"));
                if (grassCapMat.shader == null || grassCapMat.shader.name == "Hidden/InternalErrorShader")
                {
                    grassCapMat = new Material(Shader.Find("Sprites/Default"));
                }
            }

            Material spriteMat = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default"));
            if (spriteMat.shader == null || spriteMat.shader.name == "Hidden/InternalErrorShader")
            {
                spriteMat = new Material(Shader.Find("Sprites/Default"));
            }

            Sprite sprClump = AssetDatabase.LoadAssetAtPath<Sprite>($"{RiceTexDir}/T_Lana_Rice_01_GoldenClump.png");
            Sprite sprCluster = AssetDatabase.LoadAssetAtPath<Sprite>($"{RiceTexDir}/T_Lana_Rice_Panicle_05_Cluster.png");

            // Tầng 3 (Dựng trước để render phía sau)
            BuildSingleTerraceTier(root.transform, "Tier_03_FarMistRice", 3,
                tier3_Z, baseY + tier3_YOffset, tier3_Color, tier3_RidgeColor,
                waveAmplitude * 1.3f, waveFrequency * 0.85f, 2.5f, grassCapMat, spriteMat, sprCluster);

            // Tầng 2
            BuildSingleTerraceTier(root.transform, "Tier_02_MidGreenRice", 2,
                tier2_Z, baseY + tier2_YOffset, tier2_Color, tier2_RidgeColor,
                waveAmplitude * 1.1f, waveFrequency * 1.0f, 1.2f, grassCapMat, spriteMat, sprClump);

            // Tầng 1 (Gần nhất)
            BuildSingleTerraceTier(root.transform, "Tier_01_NearGoldenRice", 1,
                tier1_Z, baseY + tier1_YOffset, tier1_Color, tier1_RidgeColor,
                waveAmplitude * 0.9f, waveFrequency * 1.15f, 0.0f, grassCapMat, spriteMat, sprClump);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            SceneView.RepaintAll();
            Debug.Log("<color=#10b981>[TerracedRiceFields]</color> ĐÃ DỰNG THÀNH CÔNG 3 TẦNG RUỘNG BẬC THANG!");
        }

        private void BuildSingleTerraceTier(
            Transform parent, string tierName, int tierIndex,
            float z, float targetY, Color mainColor, Color ridgeColor,
            float amp, float freq, float phase, Material floorMat, Material spriteMat, Sprite decorSprite)
        {
            var tierGO = new GameObject(tierName);
            tierGO.transform.SetParent(parent, false);

            int cols = 140;
            float stepX = (endX - startX) / cols;

            // 1. DỰNG MESH THẢM LÚA (RICE CARPET MESH)
            var verts = new List<Vector3>();
            var colors = new List<Color>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            // 3 hàng vertex: Hàng 0 (Đáy sâu), Hàng 1 (Gần mép trên), Hàng 2 (Đỉnh bờ gờ)
            float bottomY = baseY - 4.5f;

            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c <= cols; c++)
                {
                    float x = startX + c * stepX;
                    float u = (float)c / cols;

                    // Đường uốn lượn tự nhiên của bờ ruộng
                    float wave = Mathf.Sin(x * freq + phase) * amp + Mathf.Cos(x * freq * 0.65f + phase * 1.4f) * (amp * 0.45f);
                    float slope = (x - ((startX + endX) * 0.5f)) * hillSlope;
                    float topY = targetY + wave + slope;

                    float y;
                    Color vCol;

                    if (r == 0)
                    {
                        // Đáy mesh (kéo sâu xuống dưới để không bị hở đáy)
                        y = bottomY;
                        vCol = ApplyFogTint(z, mainColor * 0.65f); // Đáy đậm màu hơn tạo cảm giác bóng đổ
                    }
                    else if (r == 1)
                    {
                        // Thảm lúa ngay dưới gờ bờ
                        y = topY - 0.09f;
                        vCol = ApplyFogTint(z, mainColor);
                    }
                    else
                    {
                        // Đỉnh gờ bờ đê (Berm Ridge)
                        y = topY;
                        vCol = ApplyFogTint(z, ridgeColor);
                    }

                    verts.Add(new Vector3(x, y, z));
                    colors.Add(vCol);
                    uvs.Add(new Vector2(u * 20.0f, (float)r / 2.0f));
                }
            }

            int colStride = cols + 1;
            for (int r = 0; r < 2; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    int i0 = r * colStride + c;
                    int i1 = i0 + 1;
                    int i2 = (r + 1) * colStride + c;
                    int i3 = i2 + 1;

                    tris.Add(i0); tris.Add(i2); tris.Add(i1);
                    tris.Add(i1); tris.Add(i2); tris.Add(i3);
                }
            }

            Mesh mesh = new Mesh { name = $"{tierName}_Mesh" };
            mesh.SetVertices(verts);
            mesh.SetColors(colors);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            string assetPath = $"{GenMeshDir}/{tierName}_Mesh.asset";
            AssetDatabase.CreateAsset(mesh, assetPath);

            var mf = tierGO.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            var mr = tierGO.AddComponent<MeshRenderer>();
            mr.sharedMaterial = floorMat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;

            // 2. CẮM CÁC KHÓM LÚA TRANG TRÍ DỌC BỜ GỜ (TỰ NHIÊN HÓA ĐƯỜNG CẮT)
            if (spawnRiceTuftsAlongRidge && decorSprite != null)
            {
                var clutterRoot = new GameObject($"{tierName}_RiceTufts");
                clutterRoot.transform.SetParent(tierGO.transform, false);

                float tuftInterval = Mathf.Lerp(1.4f, 2.8f, (z - 1.5f) / 7.0f);
                float curX = startX + Random.Range(0.5f, 1.5f);

                int tuftIndex = 0;
                while (curX < endX - 0.5f)
                {
                    float wave = Mathf.Sin(curX * freq + phase) * amp + Mathf.Cos(curX * freq * 0.65f + phase * 1.4f) * (amp * 0.45f);
                    float slope = (curX - ((startX + endX) * 0.5f)) * hillSlope;
                    float topY = targetY + wave + slope;

                    var tuftGO = new GameObject($"Tuft_{tuftIndex:D2}");
                    tuftGO.transform.SetParent(clutterRoot.transform, false);

                    // Offset Z nhẹ để các khóm lúa đứng trước đỉnh bờ gờ
                    tuftGO.transform.position = new Vector3(curX, topY - 0.02f, z - 0.05f);

                    var sr = tuftGO.AddComponent<SpriteRenderer>();
                    sr.sprite = decorSprite;
                    sr.sharedMaterial = spriteMat;

                    // Kích thước lùi dần theo xa gần (Perspective Scaling)
                    float scaleFactor = Mathf.Clamp(1.0f - (z - 1.5f) * 0.065f, 0.45f, 1.0f);
                    float randomVariance = Random.Range(0.85f, 1.15f);
                    tuftGO.transform.localScale = new Vector3(
                        scaleFactor * randomVariance * (Random.value > 0.5f ? 1f : -1f),
                        scaleFactor * randomVariance,
                        1f);

                    // Màu sắc hòa vào sương mù
                    sr.color = ApplyFogTint(z, mainColor * Random.Range(0.92f, 1.08f));
                    sr.sortingOrder = 10 - tierIndex;

                    curX += tuftInterval * Random.Range(0.85f, 1.25f);
                    tuftIndex++;
                }
            }
        }

        private static Color ApplyFogTint(float z, Color baseCol)
        {
            // Sắc sương lam sông nước Nam Bộ
            Color riverFogTint = new Color(0.58f, 0.70f, 0.82f, 1.0f);
            float t = Mathf.Clamp01((z - 1.0f) / 12.0f) * 0.42f;
            return Color.Lerp(baseCol, riverFogTint, t);
        }
    }
}
#endif
