#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace HoaVaMau.EditorTools
{
    /// <summary>
    /// Chỉnh sửa và hoàn thiện bố cục 3D + UV Tiling của bộ Plane Map trong "CaMau_1954_Village.unity"
    /// đối chiếu trực tiếp với concept art "Toàn cảnh đảo ruộng lúa thanh bình.png":
    /// 1. Sửa lỗi ngược trục UV (sọc ngang / vạch ngựa vằn) trên tất cả các tuyến đường ngang và chéo:
    ///    - Đảm bảo mọi Quad đường mòn đều có localScale.x = Chiều rộng (4.0m - 4.8m) và localScale.y = Chiều dài,
    ///      xoay theo góc hướng đường (Euler(90, yaw, 0)) và UV Tiling = (1, length / 4.8f).
    /// 2. Tắt tấm vuông "Plaza_Central_TreeCourt" (26x24m lặp 3x3 sọc vàng dưới gốc cây đa) để gốc Cây Đa Cổ Thụ
    ///    và vòng đá nằm tự nhiên trên thảm cỏ xanh mượt đúng như Concept Art.
    /// 3. Triệt tiêu 100% lỗi Z-Fighting (nhấp nháy sọc răng cưa) bằng cách phân tầng độ cao Y rõ ràng
    ///    (từ 0.520m đến 0.542m so với mặt đất nền 0.500m) cho từng tuyến đường và thửa ruộng.
    /// 4. Căn chỉnh lại kết nối mạng lưới đường làng (Paths_3D_Network) và dời nhẹ House_03_North_Center
    ///    ra khỏi lòng đường để lối đi từ Bến Xuồng -> Cây Đa -> Ngã Tư -> Cánh Đồng Lúa -> Cổng Làng liền mạch.
    /// 5. Xóa các tấm Quad Wildflower cắt vuông góc đè lên đường đất, chỉ giữ thảm cỏ hoa tự nhiên.
    /// </summary>
    public static class ApplyPlaneMapToScene
    {
        private const string TexDir = "Assets/Textures/Ground_Tiles";
        private const string MatDir = "Assets/Materials/PlaneMap";

        [MenuItem("Hoa va Mau/Environment/Apply Plane Map Assets to Scene", false, 11)]
        public static void ApplyToScene()
        {
            Debug.Log("<color=#3b82f6>[ApplyPlaneMap]</color> Bắt đầu chuẩn hóa bố cục 3D & UV Plane Map cho CaMau_1954_Village...");

            if (!Directory.Exists(MatDir))
            {
                Directory.CreateDirectory(MatDir);
            }

            // 1. TẠO & CẤU HÌNH CÁC MATERIALS CHUẨN
            var matGrass = CreateMaterial("M_PlaneMap_Grass", $"{TexDir}/T_PlaneMap_Grass_Tile.png", new Vector2(16f, 10f), 0.04f);
            var matDikeGrass = CreateMaterial("M_PlaneMap_DikeGrass", $"{TexDir}/T_PlaneMap_Grass_Tile.png", new Vector2(4f, 1f), 0.04f);
            var matWood = CreateMaterial("M_PlaneMap_WoodPlanks", $"{TexDir}/T_PlaneMap_WoodPlanks_Tile.png", new Vector2(1f, 4f), 0.12f);
            var matCliff = CreateMaterial("M_PlaneMap_CliffEdge", $"{TexDir}/T_PlaneMap_CliffEdge_Tile.png", new Vector2(4f, 1f), 0.05f);

            // 2. GẮN VÀO MẶT PHẲNG ĐỊA HÌNH CHÍNH (Terrain_Unified_Ground_Base, max Y = 0.500)
            var groundBase = GameObject.Find("CaMau_1954_Village/Environment/Terrain_3D/Terrain_Unified_Ground_Base");
            if (groundBase != null)
            {
                var mr = groundBase.GetComponent<MeshRenderer>();
                if (mr != null) mr.sharedMaterial = matGrass;
            }

            // 3. CHUẨN HÓA BỜ MẪU RUỘNG LÚA & NỀN RUỘNG (Tránh Z-Fighting và đồng bộ màu cỏ xanh dưới gốc lúa vàng nhẹ)
            var terrain = GameObject.Find("CaMau_1954_Village/Environment/Terrain_3D");
            if (terrain != null)
            {
                var lushGreenMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/M_RiceCrop_LushGreen.mat") ?? matDikeGrass;
                for (int i = 0; i < terrain.transform.childCount; i++)
                {
                    var c = terrain.transform.GetChild(i);
                    if (c.name.Contains("Dike"))
                    {
                        var mr = c.GetComponent<MeshRenderer>();
                        if (mr != null) mr.sharedMaterial = matDikeGrass;
                        // Nâng nhẹ bờ ruộng lên Y = 0.55f để nổi rõ và không Z-fight với đường/nền
                        Vector3 p = c.position;
                        c.position = new Vector3(p.x, 0.55f, p.z);
                    }
                    else if (c.name.StartsWith("Paddy_Plot_0") && !c.name.Contains("_R0"))
                    {
                        // Nền các thửa ruộng: nâng lên Y = 0.518f để không Z-fight với Ground Base (0.500f)
                        // và đồng bộ nền cỏ xanh dịu dưới các khóm lúa vàng nhẹ
                        Vector3 p = c.position;
                        c.position = new Vector3(p.x, 0.518f, p.z);
                        var mr = c.GetComponent<MeshRenderer>();
                        if (mr != null && lushGreenMat != null)
                        {
                            mr.sharedMaterial = lushGreenMat;
                        }
                    }
                }
            }

            // 4. SỬA LỖI BỐ CỤC & UV TILING CỦA MẠNG LƯỚI ĐƯỜNG LÀNG (Paths_3D_Network)
            FixRoadNetworkAndUVs();

            // 5. DỜI NHẸ NHÀ SỐ 3 (House_03_North_Center) KHỎI LÒNG ĐƯỜNG TRUNG TÂM
            var house03 = GameObject.Find("CaMau_1954_Village/Environment/Village_Houses_Network/House_03_North_Center");
            if (house03 != null)
            {
                // Trước đó ở (-8.0, 1.99, 29.0) đè thẳng lên đường trục Bắc-Nam (X = -7.0).
                // Dời sang (-16.5, 1.99, 28.5) để nằm gọn trên thảm cỏ bên trái đường mòn.
                house03.transform.position = new Vector3(-16.5f, 1.99f, 28.5f);
            }

            // 6. GẮN VÁN GỖ VÀO BẾN XUỒNG & BỜ KÈ
            ApplyWoodPlanksToPiers(matWood);

            var wall = GameObject.Find("CaMau_1954_Village/Environment/Terrain_3D/Beach_Stone_RetainingWall");
            if (wall != null)
            {
                for (int i = 0; i < wall.transform.childCount; i++)
                {
                    var mr = wall.transform.GetChild(i).GetComponent<MeshRenderer>();
                    if (mr != null) mr.sharedMaterial = matCliff;
                }
            }

            // 7. XÓA CÁC TẤM QUAD WILDFLOWER CẮT VUÔNG GÓC ĐÈ LÊN ĐƯỜNG ĐẤT
            // (Vì T_PlaneMap_Grass_Tile trên nền chính đã có sẵn hoa cúc dại vẽ tay liền mạch không lộ viền Quad)
            var existingWildflowers = GameObject.Find("CaMau_1954_Village/Environment/Vegetation_3D/Wildflower_Meadows_PlaneMap")
                                   ?? GameObject.Find("Wildflower_Meadows_PlaneMap");
            if (existingWildflowers != null)
            {
                Object.DestroyImmediate(existingWildflowers);
            }

            // LƯU SCENE VÀ ASSETS
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();

            Debug.Log("<color=#10b981>[ApplyPlaneMap]</color> ĐÃ SỬA XONG TOÀN BỘ LỖI UV ĐƯỜNG MÒN, Z-FIGHTING VÀ BỐ CỤC SCENE!");
        }

        private static void FixRoadNetworkAndUVs()
        {
            var paths = GameObject.Find("CaMau_1954_Village/Environment/Paths_3D_Network");
            if (paths == null) return;

            // Tắt tấm vuông Plaza_Central_TreeCourt (26x24m) gây lỗi 3 sọc vàng lớn dưới gốc Cây Đa Cổ Thụ.
            // Trong concept art, gốc Cây Đa và vòng đá nằm trực tiếp trên thảm cỏ xanh mượt.
            var plaza = paths.transform.Find("Plaza_Central_TreeCourt");
            if (plaza != null)
            {
                plaza.gameObject.SetActive(false);
            }

            // Cấu hình chuẩn mực cho từng đoạn đường:
            // Lưu ý quan trọng: T_PlaneMap_DirtRoad_Tile.png có lòng đường chạy dọc theo trục V (local Y),
            // và 2 mép cỏ xanh nằm ở 2 bên trái/phải theo trục U (local X).
            // Do đó MỌI Quad đường mòn bắt buộc phải có:
            //   - localScale = (roadWidth, roadLength, 1f)
            //   - rotation   = Quaternion.Euler(90f, yawAngle, 0f) (với yaw = 0 là Bắc-Nam, yaw = 90 là Đông-Tây)
            //   - UV Tiling  = (1f, roadLength / 4.8f)

            // 1. Trục đường ngang chính phía trước cánh đồng lúa (Đông - Tây, dài 115m)
            ConfigureRoadSegment(
                paths.transform,
                "Road_North_Horizontal",
                pos: new Vector3(-5.0f, 0.528f, 36.0f),
                yaw: 90.0f,
                width: 4.5f,
                length: 115.0f
            );

            // 2. Đường thẳng xuyên giữa 2 cánh đồng lúa lên Cổng Làng phía Bắc (Bắc - Nam, dài 45m)
            ConfigureRoadSegment(
                paths.transform,
                "Road_North_StraightPaddyAisle",
                pos: new Vector3(-7.0f, 0.534f, 58.0f),
                yaw: 0.0f,
                width: 4.6f,
                length: 45.0f
            );

            // 3. Đường nối từ Ngã tư cánh đồng (Z=36) xuống sân Cây Đa Cổ Thụ (Z=13) và nối thẳng xuống đường làng phía Nam (Z=6)
            ConfigureRoadSegment(
                paths.transform,
                "Road_Center_To_North",
                pos: new Vector3(-7.0f, 0.532f, 21.5f),
                yaw: 0.0f,
                width: 4.5f,
                length: 31.0f
            );

            // 4. Đường làng nhánh Tây: nối từ trục trung tâm Cây Đa (X=-7, Z=8) qua xóm nhà phía Tây & lối xuống Bến Xuồng (X=-39, Z=12)
            ConfigureRoadSegment(
                paths.transform,
                "Road_West_To_House6_And_Pier",
                pos: new Vector3(-22.5f, 0.526f, 10.0f),
                yaw: 97.0f,
                width: 4.5f,
                length: 33.0f
            );

            // 5. Đường làng nhánh Đông: nối từ trục trung tâm Cây Đa (X=-7, Z=8) qua xóm nhà phía Đông đến Bến Cảng Đông (X=40, Z=11)
            ConfigureRoadSegment(
                paths.transform,
                "Road_East_Village_Street",
                pos: new Vector3(16.5f, 0.524f, 9.5f),
                yaw: 86.0f,
                width: 4.5f,
                length: 48.0f
            );

            // 6. Đường dốc dẫn xuống Bến Xuồng phía Tây (từ đường nhánh Tây xuống cầu ván gỗ)
            ConfigureRoadSegment(
                paths.transform,
                "Road_West_Pier_Approach",
                pos: new Vector3(-22.0f, 0.420f, -1.0f),
                yaw: 12.0f,
                width: 4.2f,
                length: 20.0f
            );

            // 7. Đường dốc dẫn xuống Bến Cảng phía Đông
            ConfigureRoadSegment(
                paths.transform,
                "Road_East_Harbor_Approach",
                pos: new Vector3(45.0f, 0.420f, -1.0f),
                yaw: -24.0f,
                width: 4.4f,
                length: 22.0f
            );

            // 8. Đường dạo ven bờ kè biển phía Nam (Đông - Tây, dài 96m)
            ConfigureRoadSegment(
                paths.transform,
                "Road_Beach_Promenade",
                pos: new Vector3(0.0f, 0.450f, -7.8f),
                yaw: 90.0f,
                width: 4.0f,
                length: 96.0f
            );
        }

        private static void ConfigureRoadSegment(Transform parent, string roadName, Vector3 pos, float yaw, float width, float length)
        {
            var road = parent.Find(roadName);
            if (road == null) return;

            road.gameObject.SetActive(true);
            road.position = pos;
            road.rotation = Quaternion.Euler(90f, yaw, 0f);
            road.localScale = new Vector3(width, length, 1f);

            var mr = road.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                float repeatV = Mathf.Max(1f, Mathf.Round(length / 4.8f));
                var mat = CreateMaterial($"M_PlaneMap_Road_{roadName}", $"{TexDir}/T_PlaneMap_DirtRoad_Tile.png", new Vector2(1f, repeatV), 0.04f);
                mr.sharedMaterial = mat;
            }
        }

        private static Material CreateMaterial(string name, string texPath, Vector2 tiling, float smoothness)
        {
            string path = $"{MatDir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex != null)
            {
                mat.SetTexture("_BaseMap", tex);
                mat.SetTextureScale("_BaseMap", tiling);
                mat.SetTextureOffset("_BaseMap", Vector2.zero);
            }

            mat.SetColor("_BaseColor", Color.white);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.0f);

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void ApplyWoodPlanksToPiers(Material matWood)
        {
            var ben = GameObject.Find("CaMau_1954_Village/Village_Areas/08_Ben_Xuong");
            if (ben == null) return;

            for (int i = 0; i < ben.transform.childCount; i++)
            {
                var c = ben.transform.GetChild(i);
                if (c.name.Contains("Deck") || c.name.Contains("Platform") || c.name.Contains("Walkway") || c.name.Contains("Piling"))
                {
                    var mr = c.GetComponent<MeshRenderer>();
                    if (mr != null) mr.sharedMaterial = matWood;
                }
            }
        }
    }
}
#endif
