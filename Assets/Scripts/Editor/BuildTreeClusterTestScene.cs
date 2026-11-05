#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using HoaVaMau.Gameplay;

namespace HoaVaMau.EditorTools
{
    /// <summary>
    /// Master Scene Builder for Testing Background Tree Clusters (Rừng cây hậu cảnh).
    /// Generates Assets/Scenes/Tree_Cluster_Test.unity with:
    /// - 6 distinct architectural & ecological tree cluster options for 1954 Southern Vietnam / Ca Mau.
    /// - 1 full in-context Demo Vista combining background trees + golden rice paddies + character + watercolor sky.
    /// - Human scale references (Bé An 3D model) at each stand.
    /// - Interactive Camera & HUD test manager (Keys [1]-[6], [0]/[D], Arrows/AD pan, Zoom).
    /// </summary>
    public static class BuildTreeClusterTestScene
    {
        public const string ScenePath = "Assets/Scenes/Tree_Cluster_Test.unity";

        // Asset Prefab Paths
        private const string TreeLargeCanopy = "Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_01_LargeCanopy.prefab";
        private const string TreeMedium = "Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_02_Medium.prefab";
        private const string TreeSmallBush = "Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_03_SmallBushTree.prefab";
        private const string TreeSlenderTall = "Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_04_SlenderTall.prefab";
        private const string TreeBanana = "Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_05_Banana.prefab";
        private const string TreeCoconut = "Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_06_Coconut.prefab";
        private const string TreeBamboo = "Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_07_BambooGrove.prefab";
        private const string TreeHeroLayered = "Assets/Prefabs/Environment/Trees/PF_Hero_Layered_Tree.prefab";
        private const string TreeCurvedV2 = "Assets/Prefabs/Environment/Trees/PF_Curved_Hero_Tree_V2.prefab";
        private const string PlayerAnPrefab = "Assets/Prefabs/Characters/PF_Player_An.prefab";

        // Rice Prefabs for Demo
        private const string RiceClusterDense = "Assets/Prefabs/RiceField/Clusters/PF_Cluster_02_ThamLuaDayDan.prefab";
        private const string RiceClusterCanal = "Assets/Prefabs/RiceField/Clusters/PF_Cluster_04_BoKenhXanhNon.prefab";
        private const string RiceClusterSparse = "Assets/Prefabs/RiceField/Clusters/PF_Cluster_05_KhomThuaThanhThoat.prefab";
        private const string RiceSpriteGoldenBig = "Assets/Prefabs/RiceField/PF_Sprite_01_CoLua_BuiVangLon.prefab";
        private const string RiceSpriteWindTuft = "Assets/Prefabs/RiceField/PF_Sprite_07_CumCo_BuiLuaGio_VangTuoi.prefab";

        [MenuItem("Tools/Hoa trên đất đỏ/Tạo Scene Test Rừng Cây Hậu Cảnh (Tree Cluster Test Scene)", false, 10)]
        public static void GenerateScene()
        {
            Debug.Log("<color=#3b82f6>[TreeClusterTest]</color> Bắt đầu dựng Scene thử nghiệm các cụm rừng cây hậu cảnh...");

            // 1. Tạo mới Scene hoặc mở sạch
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 2. Nhóm Root
            GameObject envRoot = new GameObject("=== ENVIRONMENT & LIGHTING ===");
            GameObject standsRoot = new GameObject("=== TESTING STANDS (OPTIONS 1 - 6) ===");
            GameObject demoRoot = new GameObject("=== IN_CONTEXT_DEMO_VISTA ===");
            GameObject rigRoot = new GameObject("=== CAMERA & TEST RIG ===");

            // 3. Ánh sáng & Bầu trời
            SetupLightingAndBackdrop(envRoot);

            // 4. Bờ đất chung xuyên suốt
            SetupGroundEmbankment(envRoot);

            // 5. Dựng 10 Phương Án Cụm Cây
            var opt1 = BuildOption1_AiryBambooMelaleuca(standsRoot);
            var opt2 = BuildOption2_LushCoconutBanyan(standsRoot);
            var opt3 = BuildOption3_DeepLayeredUMinh(standsRoot);
            var opt4 = BuildOption4_WindsweptBambooTrees(standsRoot);
            var opt5 = BuildOption5_CanalBankOrchard(standsRoot);
            var opt6 = BuildOption6_HeroLandmark(standsRoot);
            var opt7 = BuildOption7_EndlessMelaleuca(standsRoot);
            var opt8 = BuildOption8_RiverbankCoconutFronds(standsRoot);
            var opt9 = BuildOption9_BambooGatewayArch(standsRoot);
            var opt10 = BuildOption10_VastHorizonSparse(standsRoot);

            // 6. Dựng Demo Toàn Cảnh Phối Hợp (Cây hậu cảnh + Lúa tiền cảnh + Nhân vật)
            var optDemo = BuildDemoVista(demoRoot);

            // 7. Thiết lập Camera & Test Manager
            SetupCameraAndManager(rigRoot, new List<TreeClusterTestManager.ClusterOption> {
                opt1, opt2, opt3, opt4, opt5, opt6, opt7, opt8, opt9, opt10, optDemo
            });

            // 8. Lưu Scene
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"<color=#10b981>[TreeClusterTest]</color> Tạo Scene thành công tại: {ScenePath}!");

            // 9. Tự động xuất toàn bộ Prefab
            ExportAllClustersToPrefabs();
        }

        private static void SetupLightingAndBackdrop(GameObject parent)
        {
            // Sun Light - Warm Nam Bo golden sunlight
            GameObject lightGo = new GameObject("Directional Light - Warm Golden Sun");
            lightGo.transform.SetParent(parent.transform);
            Light l = lightGo.AddComponent<Light>();
            l.type = LightType.Directional;
            l.color = new Color(1.0f, 0.96f, 0.90f);
            l.intensity = 1.25f;
            lightGo.transform.rotation = Quaternion.Euler(22f, -30f, 0f);

            // Ambient
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.72f, 0.82f, 0.88f);

            // Far Sky Backdrop Quad with authentic watercolor texture
            GameObject sky = GameObject.CreatePrimitive(PrimitiveType.Quad);
            sky.name = "Far_Watercolor_Sky_Backdrop";
            sky.transform.SetParent(parent.transform);
            sky.transform.position = new Vector3(100f, 15f, 48f);
            sky.transform.localScale = new Vector3(480f, 45f, 1f);
            Object.DestroyImmediate(sky.GetComponent<Collider>());

            var skyTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Backgrounds_Lana/BG_Forest_CleanPlate.png");
            var skyMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            skyMat.name = "M_Test_Sky_Watercolor";
            if (skyTex != null)
            {
                skyMat.mainTexture = skyTex;
                skyMat.color = new Color(1.0f, 1.0f, 1.0f, 0.95f);
            }
            else
            {
                skyMat.color = new Color(0.55f, 0.75f, 0.92f);
            }
            sky.GetComponent<MeshRenderer>().sharedMaterial = skyMat;
        }

        private static void SetupGroundEmbankment(GameObject parent)
        {
            // 1. Horizontal ground surface (tạo mặt đất tự nhiên trải rộng từ Z = -2.5m đến Z = 32m)
            // Đảm bảo tất cả gốc cây từ gần đến xa đều cắm vững chãi trên mặt đất, không hở nền trời bên dưới.
            GameObject groundPlane = GameObject.CreatePrimitive(PrimitiveType.Quad);
            groundPlane.name = "Continuous_Ground_Surface_Horizontal";
            groundPlane.transform.SetParent(parent.transform);
            groundPlane.transform.position = new Vector3(100f, -1.5f, 14.5f);
            groundPlane.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // Face UP
            groundPlane.transform.localScale = new Vector3(480f, 35f, 1f);
            Object.DestroyImmediate(groundPlane.GetComponent<Collider>());

            var groundMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/LanaOverhaul/GroundVariants/M_LanaGround_01_LushForest_Roots.mat");
            if (groundMat == null)
            {
                groundMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                groundMat.color = new Color(0.24f, 0.33f, 0.16f);
            }
            groundPlane.GetComponent<MeshRenderer>().sharedMaterial = groundMat;

            // 2. Front vertical mud bank (chân bờ đất tiền cảnh ở Z = -2.5m)
            GameObject mudBank = GameObject.CreatePrimitive(PrimitiveType.Quad);
            mudBank.name = "Continuous_Ground_Front_MudBank";
            mudBank.transform.SetParent(parent.transform);
            mudBank.transform.position = new Vector3(100f, -4.5f, -2.5f);
            mudBank.transform.rotation = Quaternion.identity; // Face Camera
            mudBank.transform.localScale = new Vector3(480f, 6.0f, 1f);
            Object.DestroyImmediate(mudBank.GetComponent<Collider>());

            var mudMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/LanaOverhaul/GroundVariants/M_LanaGround_04_Alluvial_MudBank.mat");
            if (mudMat == null)
            {
                mudMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                mudMat.color = new Color(0.18f, 0.22f, 0.12f);
            }
            mudBank.GetComponent<MeshRenderer>().sharedMaterial = mudMat;
        }

        // ==========================================
        // OPTION 1: RẶNG TRE & RỪNG TRÀM THƯA THOÁNG
        // ==========================================
        private static TreeClusterTestManager.ClusterOption BuildOption1_AiryBambooMelaleuca(GameObject parent)
        {
            float posX = -60.0f;
            GameObject root = new GameObject("Cluster_01_RangTre_Va_TramThua");
            root.transform.SetParent(parent.transform);
            root.transform.position = new Vector3(posX, 0f, 0f);

            // Trees
            SpawnPrefab(TreeBamboo, root.transform, new Vector3(-2.8f, -1.5f, 5.0f), Vector3.zero, new Vector3(1.25f, 1.25f, 1.25f));
            SpawnPrefab(TreeSlenderTall, root.transform, new Vector3(2.5f, -1.5f, 6.5f), Vector3.zero, new Vector3(1.3f, 1.3f, 1.3f));
            SpawnPrefab(TreeSlenderTall, root.transform, new Vector3(5.2f, -1.5f, 8.5f), new Vector3(0, 0, 4f), new Vector3(1.15f, 1.15f, 1.15f));
            SpawnPrefab(TreeBanana, root.transform, new Vector3(-0.8f, -1.5f, 3.5f), Vector3.zero, new Vector3(1.1f, 1.1f, 1.1f));
            SpawnPrefab(TreeSmallBush, root.transform, new Vector3(3.8f, -1.5f, 4.0f), Vector3.zero, new Vector3(1.0f, 1.0f, 1.0f));

            // Character scale ref
            SpawnCharacterRef(root.transform, new Vector3(0f, -1.5f, 0f));
            CreateBanner(root.transform, new Vector3(0f, 6.2f, 0f), "PHƯƠNG ÁN 1: RẶNG TRE & TRÀM THƯA THOÁNG", "Tán tre mềm mại + tràm thanh mảnh vươn cao, khoảng hở đón nắng, nhìn thấu chân trời.");

            return new TreeClusterTestManager.ClusterOption
            {
                optionName = "Phương Án 1",
                title = "Rặng Tre & Rừng Tràm Thưa Thoáng (Airy Bamboo & Slender Melaleuca)",
                description = "• Tỉ lệ: 40% tre nghiêng, 35% tràm mảnh, 25% chuối và bụi thấp che gốc.\n• Đặc tính: Nhẹ nhàng, sáng sủa, giữ khoảng hở ~45% bầu trời, không gây cảm giác ngột ngạt.",
                cameraX = posX,
                cameraDistance = 17.5f,
                cameraHeight = 2.4f,
                clusterRoot = root
            };
        }

        // ==========================================
        // OPTION 2: RỪNG DỪA NƯỚC & CỔ THỤ XUM HUÊ
        // ==========================================
        private static TreeClusterTestManager.ClusterOption BuildOption2_LushCoconutBanyan(GameObject parent)
        {
            float posX = -30.0f;
            GameObject root = new GameObject("Cluster_02_RungDua_CoThuXumHue");
            root.transform.SetParent(parent.transform);
            root.transform.position = new Vector3(posX, 0f, 0f);

            // Large Canopy in center
            SpawnPrefab(TreeLargeCanopy, root.transform, new Vector3(0f, -1.5f, 7.5f), Vector3.zero, new Vector3(1.35f, 1.35f, 1.35f));
            // Coconuts flaking both sides
            SpawnPrefab(TreeCoconut, root.transform, new Vector3(-4.8f, -1.5f, 5.0f), new Vector3(0, 0, -4f), new Vector3(1.25f, 1.25f, 1.25f));
            SpawnPrefab(TreeCoconut, root.transform, new Vector3(4.2f, -1.5f, 5.5f), new Vector3(0, 180f, 5f), new Vector3(1.15f, 1.15f, 1.15f));
            // Mid tree & Banana
            SpawnPrefab(TreeMedium, root.transform, new Vector3(-2.5f, -1.5f, 4.2f), Vector3.zero, new Vector3(1.05f, 1.05f, 1.05f));
            SpawnPrefab(TreeBanana, root.transform, new Vector3(2.0f, -1.5f, 3.2f), Vector3.zero, new Vector3(1.2f, 1.2f, 1.2f));

            SpawnCharacterRef(root.transform, new Vector3(0f, -1.5f, 0f));
            CreateBanner(root.transform, new Vector3(0f, 6.5f, 0f), "PHƯƠNG ÁN 2: RỪNG DỪA NƯỚC & CỔ THỤ XUM HUÊ", "Tán bồ đề đại thụ che bóng mát đan xen tán dừa nước xòe rộng, đậm chất miệt vườn Cà Mau.");

            return new TreeClusterTestManager.ClusterOption
            {
                optionName = "Phương Án 2",
                title = "Rừng Dừa Nước & Cổ ThỤ Xum Huê (Lush Coconut & Banyan Canopy)",
                description = "• Tỉ lệ: Cổ thụ đại ngàn (trung tâm) + Dừa Nam Bộ 2 cánh + Bụi chuối bờ kênh.\n• Đặc tính: Độ phủ bóng cao (~80%), tán lá tầng tầng lớp lớp, ấm no trù phú ven sông rạch.",
                cameraX = posX,
                cameraDistance = 18.0f,
                cameraHeight = 2.5f,
                clusterRoot = root
            };
        }

        // ==========================================
        // OPTION 3: RỪNG U MINH SÂU 3 TẦNG LỚP
        // ==========================================
        private static TreeClusterTestManager.ClusterOption BuildOption3_DeepLayeredUMinh(GameObject parent)
        {
            float posX = 0.0f;
            GameObject root = new GameObject("Cluster_03_RungUMinh_3TangChieuSau");
            root.transform.SetParent(parent.transform);
            root.transform.position = new Vector3(posX, 0f, 0f);

            // Layer 1: Near-Back (Z ~ 4.5m)
            SpawnPrefab(TreeBanana, root.transform, new Vector3(-3.5f, -1.5f, 4.0f), Vector3.zero, new Vector3(1.15f, 1.15f, 1.15f));
            SpawnPrefab(TreeSmallBush, root.transform, new Vector3(3.0f, -1.5f, 4.5f), Vector3.zero, new Vector3(1.1f, 1.1f, 1.1f));

            // Layer 2: Mid-Back (Z ~ 12m)
            SpawnPrefab(TreeBamboo, root.transform, new Vector3(-1.5f, -1.5f, 11.0f), Vector3.zero, new Vector3(1.4f, 1.4f, 1.4f));
            SpawnPrefab(TreeSlenderTall, root.transform, new Vector3(4.0f, -1.5f, 13.0f), Vector3.zero, new Vector3(1.35f, 1.35f, 1.35f));
            SpawnPrefab(TreeMedium, root.transform, new Vector3(-4.8f, -1.5f, 14.0f), Vector3.zero, new Vector3(1.3f, 1.3f, 1.3f));

            // Layer 3: Far-Back (Z ~ 24m)
            var farTree = SpawnPrefab(TreeLargeCanopy, root.transform, new Vector3(1.0f, -1.5f, 24.0f), Vector3.zero, new Vector3(2.4f, 2.4f, 2.4f));
            if (farTree != null)
            {
                // Tint lighter with atmospheric haze
                var sr = farTree.GetComponentInChildren<SpriteRenderer>();
                if (sr != null) sr.color = new Color(0.75f, 0.85f, 0.90f, 0.88f);
            }

            SpawnCharacterRef(root.transform, new Vector3(0f, -1.5f, 0f));
            CreateBanner(root.transform, new Vector3(0f, 6.6f, 0f), "PHƯƠNG ÁN 3: RỪNG U MINH SÂU 3 TẦNG LỚP (PARALLAX BANDS)", "Phân 3 dải cự ly Z (Gần 4.5m - Giữa 12m - Xa 24m), tạo ảo giác chiều sâu thị sai vô tận.");

            return new TreeClusterTestManager.ClusterOption
            {
                optionName = "Phương Án 3",
                title = "Rừng U Minh Sâu 3 Tầng Lớp (Layered Parallax Bands)",
                description = "• Tỉ lệ: Cận hậu (4m) - Trung hậu (12m) - Viễn hậu mờ sương (24m).\n• Đặc tính: Khai thác tối đa hiệu ứng thị sai (Parallax depth) khi camera cuộn 2.5D.",
                cameraX = posX,
                cameraDistance = 19.0f,
                cameraHeight = 2.6f,
                clusterRoot = root
            };
        }

        // ==========================================
        // OPTION 4: LUỸ TRE & CÂY NGHIÊNG GIÓ ĐỒNG
        // ==========================================
        private static TreeClusterTestManager.ClusterOption BuildOption4_WindsweptBambooTrees(GameObject parent)
        {
            float posX = 30.0f;
            GameObject root = new GameObject("Cluster_04_LuyTre_NghiengGioDong");
            root.transform.SetParent(parent.transform);
            root.transform.position = new Vector3(posX, 0f, 0f);

            // Curved hero tree leaning right
            SpawnPrefab(TreeCurvedV2, root.transform, new Vector3(-1.0f, -1.5f, 6.0f), Vector3.zero, new Vector3(1.2f, 1.2f, 1.2f));
            // Bamboo leaning with wind
            SpawnPrefab(TreeBamboo, root.transform, new Vector3(4.0f, -1.5f, 5.0f), new Vector3(0, 0, -8f), new Vector3(1.25f, 1.25f, 1.25f));
            // Slender tall tilted
            SpawnPrefab(TreeSlenderTall, root.transform, new Vector3(-4.5f, -1.5f, 7.5f), new Vector3(0, 0, -5f), new Vector3(1.2f, 1.2f, 1.2f));
            SpawnPrefab(TreeSmallBush, root.transform, new Vector3(1.5f, -1.5f, 3.8f), Vector3.zero, new Vector3(1.05f, 1.05f, 1.05f));

            SpawnCharacterRef(root.transform, new Vector3(0f, -1.5f, 0f));
            CreateBanner(root.transform, new Vector3(0f, 6.2f, 0f), "PHƯƠNG ÁN 4: LUỸ TRE & CÂY NGHIÊNG GIÓ ĐỒNG", "Thân cây uốn lượn nhịp nhàng theo chiều gió biển, tạo đường dẫn mắt người chơi tiến bước.");

            return new TreeClusterTestManager.ClusterOption
            {
                optionName = "Phương Án 4",
                title = "Luỹ Tre & Cây Nghiêng Gió Đồng (Windswept Bamboo & Trees)",
                description = "• Tỉ lệ: Cây cong vươn mình + Luỹ tre nghiêng 8 độ + Tràm mảnh khảnh đung đưa.\n• Đặc tính: Động lực học thị giác (Visual Flow) dẫn dắt hướng di chuyển từ trái sang phải.",
                cameraX = posX,
                cameraDistance = 17.5f,
                cameraHeight = 2.4f,
                clusterRoot = root
            };
        }

        // ==========================================
        // OPTION 5: VƯỜN CÂY BỜ MƯƠNG ĐA DẠNG
        // ==========================================
        private static TreeClusterTestManager.ClusterOption BuildOption5_CanalBankOrchard(GameObject parent)
        {
            float posX = 60.0f;
            GameObject root = new GameObject("Cluster_05_VuonCayBoKenh_DaDang");
            root.transform.SetParent(parent.transform);
            root.transform.position = new Vector3(posX, 0f, 0f);

            // Tall coconut
            SpawnPrefab(TreeCoconut, root.transform, new Vector3(-3.5f, -1.5f, 5.5f), Vector3.zero, new Vector3(1.3f, 1.3f, 1.3f));
            // Medium fruit tree
            SpawnPrefab(TreeMedium, root.transform, new Vector3(2.5f, -1.5f, 6.5f), Vector3.zero, new Vector3(1.2f, 1.2f, 1.2f));
            // Banana clusters at base
            SpawnPrefab(TreeBanana, root.transform, new Vector3(-0.8f, -1.5f, 3.5f), Vector3.zero, new Vector3(1.25f, 1.25f, 1.25f));
            SpawnPrefab(TreeBanana, root.transform, new Vector3(4.5f, -1.5f, 3.8f), new Vector3(0, 180f, 0), new Vector3(1.1f, 1.1f, 1.1f));
            // Small bush
            SpawnPrefab(TreeSmallBush, root.transform, new Vector3(-5.5f, -1.5f, 4.0f), Vector3.zero, new Vector3(1.15f, 1.15f, 1.15f));

            SpawnCharacterRef(root.transform, new Vector3(0f, -1.5f, 0f));
            CreateBanner(root.transform, new Vector3(0f, 6.4f, 0f), "PHƯƠNG ÁN 5: VƯỜN CÂY BỜ KÊNH ĐA DẠNG", "Dừa cao, cây ăn trái và rặng chuối so le tự nhiên, phân tầng sinh thái bờ rạch Nam Bộ.");

            return new TreeClusterTestManager.ClusterOption
            {
                optionName = "Phương Án 5",
                title = "Vườn Cây Bờ Kênh Đa Dạng (Canal Embankment Mixed Orchard)",
                description = "• Tỉ lệ: Dừa cao tầng 1 + Cây ăn trái tầng 2 + Rặng chuối & bụi thấp tầng 3.\n• Đặc tính: Bố cục tự nhiên so le không đối xứng, rất phù hợp hậu cảnh ven nhà lá / bờ rạch.",
                cameraX = posX,
                cameraDistance = 18.0f,
                cameraHeight = 2.5f,
                clusterRoot = root
            };
        }

        // ==========================================
        // OPTION 6: CÂY ĐẠI THỤ LANDMARK ĐIỂM NHẤN
        // ==========================================
        private static TreeClusterTestManager.ClusterOption BuildOption6_HeroLandmark(GameObject parent)
        {
            float posX = 90.0f;
            GameObject root = new GameObject("Cluster_06_HeroTree_Landmark");
            root.transform.SetParent(parent.transform);
            root.transform.position = new Vector3(posX, 0f, 0f);

            // Majestic Layered Banyan Tree
            SpawnPrefab(TreeHeroLayered, root.transform, new Vector3(0f, -1.5f, 8.0f), Vector3.zero, new Vector3(1.4f, 1.4f, 1.4f));
            // Satellite small foliage
            SpawnPrefab(TreeSmallBush, root.transform, new Vector3(-5.5f, -1.5f, 4.5f), Vector3.zero, new Vector3(1.1f, 1.1f, 1.1f));
            SpawnPrefab(TreeSmallBush, root.transform, new Vector3(5.5f, -1.5f, 4.8f), new Vector3(0, 180f, 0), new Vector3(1.05f, 1.05f, 1.05f));

            SpawnCharacterRef(root.transform, new Vector3(0f, -1.5f, 0f));
            CreateBanner(root.transform, new Vector3(0f, 7.2f, 0f), "PHƯƠNG ÁN 6: CÂY ĐẠI THỤ LANDMARK ĐIỂM NHẤN", "Cây bồ đề cổ thụ vĩ đại uy nghi, hai bên thoáng đãng tôn vẻ cổ kính thiêng liêng.");

            return new TreeClusterTestManager.ClusterOption
            {
                optionName = "Phương Án 6",
                title = "Cây Đại Thụ Landmark Điểm Nhấn (Hero Banyan & Satellite Foliage)",
                description = "• Tỉ lệ: 1 Cổ thụ đại ngàn phân tầng (14m tán) + 2 bụi nhỏ vệ tinh hai bên.\n• Đặc tính: Điểm nhấn thị giác mạnh mẽ (Focal Point), dành riêng cho các phân đoạn tự sự / hoàng hôn.",
                cameraX = posX,
                cameraDistance = 21.0f,
                cameraHeight = 3.2f,
                clusterRoot = root
            };
        }

        // ==========================================
        // OPTION 7: RỪNG TRÀM BẠT NGÀN MÙA NƯỚC NỔI
        // ==========================================
        private static TreeClusterTestManager.ClusterOption BuildOption7_EndlessMelaleuca(GameObject parent)
        {
            float posX = 120.0f;
            GameObject root = new GameObject("Cluster_07_RungTram_BatNgan");
            root.transform.SetParent(parent.transform);
            root.transform.position = new Vector3(posX, 0f, 0f);

            // Layer 1 - Tiền/Trung tràm (Z = 6m .. 8m)
            SpawnPrefab(TreeSlenderTall, root.transform, new Vector3(-5.2f, -1.5f, 6.0f), Vector3.zero, new Vector3(1.25f, 1.25f, 1.25f));
            SpawnPrefab(TreeSlenderTall, root.transform, new Vector3(-2.0f, -1.5f, 7.5f), new Vector3(0, 0, 3f), new Vector3(1.30f, 1.30f, 1.30f));
            SpawnPrefab(TreeSlenderTall, root.transform, new Vector3(1.5f, -1.5f, 6.2f), new Vector3(0, 0, -2f), new Vector3(1.20f, 1.20f, 1.20f));
            SpawnPrefab(TreeSlenderTall, root.transform, new Vector3(4.8f, -1.5f, 8.0f), new Vector3(0, 0, 4f), new Vector3(1.35f, 1.35f, 1.35f));

            // Layer 2 - Hậu cảnh tràm sâu (Z = 13m .. 18m)
            SpawnPrefab(TreeSlenderTall, root.transform, new Vector3(-3.5f, -1.5f, 14.0f), Vector3.zero, new Vector3(1.45f, 1.45f, 1.45f));
            SpawnPrefab(TreeSlenderTall, root.transform, new Vector3(3.0f, -1.5f, 16.0f), Vector3.zero, new Vector3(1.50f, 1.50f, 1.50f));
            SpawnPrefab(TreeSlenderTall, root.transform, new Vector3(0.0f, -1.5f, 18.0f), Vector3.zero, new Vector3(1.50f, 1.50f, 1.50f));

            // Low wetland bushes at roots
            SpawnPrefab(TreeSmallBush, root.transform, new Vector3(-3.8f, -1.5f, 4.2f), Vector3.zero, new Vector3(1.10f, 1.10f, 1.10f));
            SpawnPrefab(TreeSmallBush, root.transform, new Vector3(2.5f, -1.5f, 4.0f), Vector3.zero, new Vector3(1.05f, 1.05f, 1.05f));

            SpawnCharacterRef(root.transform, new Vector3(0f, -1.5f, 0f));
            CreateBanner(root.transform, new Vector3(0f, 6.5f, 0f), "PHƯƠNG ÁN 7: RỪNG TRÀM BẠT NGÀN MÙA NƯỚC NỔI", "Thân tràm thẳng tắp vươn cao đa tầng, nhịp điệu sọc dọc uy nghi bạt ngàn U Minh Hạ.");

            return new TreeClusterTestManager.ClusterOption
            {
                optionName = "Phương Án 7",
                title = "Rừng Tràm Bạt Ngàn Mùa Nước Nổi (Endless Melaleuca Vertical Rhythm)",
                description = "• Tỉ lệ: 70% Tràm thân thẳng đa tầng (Z=6..18m) + 30% Bụi thấp che chân đất phèn.\n• Đặc tính: Nhịp điệu sọc dọc (Vertical Rhythm) mạnh mẽ, tái hiện rừng tràm U Minh mùa nước nổi bạt ngàn.",
                cameraX = posX,
                cameraDistance = 18.5f,
                cameraHeight = 2.6f,
                clusterRoot = root
            };
        }

        // ==========================================
        // OPTION 8: CỒN DỪA NƯỚC BỜ SÔNG ĐỐC
        // ==========================================
        private static TreeClusterTestManager.ClusterOption BuildOption8_RiverbankCoconutFronds(GameObject parent)
        {
            float posX = 150.0f;
            GameObject root = new GameObject("Cluster_08_ConDuaNuoc_SongDoc");
            root.transform.SetParent(parent.transform);
            root.transform.position = new Vector3(posX, 0f, 0f);

            // Coconut palms arching toward river
            SpawnPrefab(TreeCoconut, root.transform, new Vector3(-4.2f, -1.5f, 6.0f), new Vector3(0, 0, -6f), new Vector3(1.30f, 1.30f, 1.30f));
            SpawnPrefab(TreeCoconut, root.transform, new Vector3(0.5f, -1.5f, 7.5f), new Vector3(0, 0, 4f), new Vector3(1.35f, 1.35f, 1.35f));
            SpawnPrefab(TreeCoconut, root.transform, new Vector3(5.0f, -1.5f, 5.5f), new Vector3(0, 180f, 8f), new Vector3(1.25f, 1.25f, 1.25f));

            // Lush water banana clumps
            SpawnPrefab(TreeBanana, root.transform, new Vector3(-2.0f, -1.5f, 4.0f), Vector3.zero, new Vector3(1.20f, 1.20f, 1.20f));
            SpawnPrefab(TreeBanana, root.transform, new Vector3(3.2f, -1.5f, 4.2f), new Vector3(0, 180f, 0), new Vector3(1.15f, 1.15f, 1.15f));

            // Orchard fruit tree & bush
            SpawnPrefab(TreeMedium, root.transform, new Vector3(-6.0f, -1.5f, 5.0f), Vector3.zero, new Vector3(1.05f, 1.05f, 1.05f));
            SpawnPrefab(TreeSmallBush, root.transform, new Vector3(2.2f, -1.5f, 4.5f), Vector3.zero, new Vector3(1.10f, 1.10f, 1.10f));

            SpawnCharacterRef(root.transform, new Vector3(0f, -1.5f, 0f));
            CreateBanner(root.transform, new Vector3(0f, 6.5f, 0f), "PHƯƠNG ÁN 8: CỒN DỪA NƯỚC BỜ SÔNG ĐỐC", "Rặng dừa nghiêng đón gió biển, khóm chuối xanh mướt ôm trọn mé sông rạch trù phú.");

            return new TreeClusterTestManager.ClusterOption
            {
                optionName = "Phương Án 8",
                title = "Cồn Dừa Nước Bờ Sông Đốc (Sông Đốc Riverbank Coconut & Banana Fronds)",
                description = "• Tỉ lệ: 50% Dừa Nam Bộ xòe tán đón gió + 35% Bụi chuối mọc ken dày + 15% Cây ăn trái bờ kênh.\n• Đặc tính: Đậm chất cồn bãi sông nước miền Tây, tán dừa cong duyên dáng tạo nét sinh động cho chân trời.",
                cameraX = posX,
                cameraDistance = 18.0f,
                cameraHeight = 2.5f,
                clusterRoot = root
            };
        }

        // ==========================================
        // OPTION 9: CỔNG LÀNG VÒM TRE RÂM MÁT
        // ==========================================
        private static TreeClusterTestManager.ClusterOption BuildOption9_BambooGatewayArch(GameObject parent)
        {
            float posX = 180.0f;
            GameObject root = new GameObject("Cluster_09_CongLang_VomTre");
            root.transform.SetParent(parent.transform);
            root.transform.position = new Vector3(posX, 0f, 0f);

            // Left arching bamboo leaning right
            SpawnPrefab(TreeBamboo, root.transform, new Vector3(-4.5f, -1.5f, 5.5f), new Vector3(0, 0, -11f), new Vector3(1.45f, 1.45f, 1.45f));
            // Right arching bamboo leaning left
            SpawnPrefab(TreeBamboo, root.transform, new Vector3(4.5f, -1.5f, 5.5f), new Vector3(0, 180f, -11f), new Vector3(1.45f, 1.45f, 1.45f));

            // Supporting bamboo groves
            SpawnPrefab(TreeBamboo, root.transform, new Vector3(-2.0f, -1.5f, 8.0f), new Vector3(0, 0, -6f), new Vector3(1.25f, 1.25f, 1.25f));
            SpawnPrefab(TreeBamboo, root.transform, new Vector3(2.2f, -1.5f, 8.2f), new Vector3(0, 0, 5f), new Vector3(1.20f, 1.20f, 1.20f));

            // Far depth fruit tree in middle
            SpawnPrefab(TreeMedium, root.transform, new Vector3(0.0f, -1.5f, 13.0f), Vector3.zero, new Vector3(1.30f, 1.30f, 1.30f));

            // Low bushes along border
            SpawnPrefab(TreeSmallBush, root.transform, new Vector3(-5.2f, -1.5f, 3.8f), Vector3.zero, new Vector3(1.10f, 1.10f, 1.10f));
            SpawnPrefab(TreeSmallBush, root.transform, new Vector3(5.2f, -1.5f, 3.8f), Vector3.zero, new Vector3(1.10f, 1.10f, 1.10f));

            SpawnCharacterRef(root.transform, new Vector3(0f, -1.5f, 0f));
            CreateBanner(root.transform, new Vector3(0f, 6.6f, 0f), "PHƯƠNG ÁN 9: CỔNG LÀNG VÒM TRE RÂM MÁT", "Hai hàng tre đan ngọn tạo vòm cổng thiên nhiên che bóng râm, đón chào người lữ khách.");

            return new TreeClusterTestManager.ClusterOption
            {
                optionName = "Phương Án 9",
                title = "Cổng Làng Vòm Tre Râm Mát (Natural Bamboo Tunnel Arch)",
                description = "• Tỉ lệ: 65% Tre làng uốn cong giao nhau + 20% Cây ăn trái xa + 15% Bụi cỏ ven đường.\n• Đặc tính: Tạo cảm giác đường hầm thiên nhiên (Green Tunnel), đóng khung nhân vật cực kỳ ấn tượng.",
                cameraX = posX,
                cameraDistance = 18.0f,
                cameraHeight = 2.5f,
                clusterRoot = root
            };
        }

        // ==========================================
        // OPTION 10: RỪNG THƯA HOÀNG HÔN CHÂN TRỜI
        // ==========================================
        private static TreeClusterTestManager.ClusterOption BuildOption10_VastHorizonSparse(GameObject parent)
        {
            float posX = 210.0f;
            GameObject root = new GameObject("Cluster_10_RungThua_HoangHon");
            root.transform.SetParent(parent.transform);
            root.transform.position = new Vector3(posX, 0f, 0f);

            // Curved hero tree on distant ridge
            SpawnPrefab(TreeCurvedV2, root.transform, new Vector3(-4.0f, -1.5f, 14.0f), new Vector3(0, 0, 4f), new Vector3(1.20f, 1.20f, 1.20f));
            // Distant slender tree
            SpawnPrefab(TreeSlenderTall, root.transform, new Vector3(3.5f, -1.5f, 18.0f), Vector3.zero, new Vector3(1.30f, 1.30f, 1.30f));
            // Small bush far away
            SpawnPrefab(TreeSmallBush, root.transform, new Vector3(8.0f, -1.5f, 12.0f), Vector3.zero, new Vector3(1.05f, 1.05f, 1.05f));
            // Low banana near
            SpawnPrefab(TreeBanana, root.transform, new Vector3(-1.5f, -1.5f, 8.5f), Vector3.zero, new Vector3(0.95f, 0.95f, 0.95f));

            SpawnCharacterRef(root.transform, new Vector3(0f, -1.5f, 0f));
            CreateBanner(root.transform, new Vector3(0f, 6.4f, 0f), "PHƯƠNG ÁN 10: RỪNG THƯA HOÀNG HÔN CHÂN TRỜI", "Bố cục bưng biền tối giản, mở rộng 65% bầu trời, giàu chất thơ và không gian tự sự.");

            return new TreeClusterTestManager.ClusterOption
            {
                optionName = "Phương Án 10",
                title = "Rừng Thưa Hoàng Hôn Chân Trời (Minimalist Vast Horizon & Story Beats)",
                description = "• Tỉ lệ: Cây thưa thớt ở xa (Z=12..18m), chừa 65% diện tích cho nền trời watercolor khoáng đạt.\n• Đặc tính: Tuyệt vời cho các trường đoạn hoàng hôn, hoài niệm, đối thoại nội tâm hoặc điểm kết cutscene.",
                cameraX = posX,
                cameraDistance = 19.5f,
                cameraHeight = 2.8f,
                clusterRoot = root
            };
        }

        // ==========================================
        // DEMO VISTA: TOÀN CẢNH PHỐI HỢP LÚA & RỪNG CÂY
        // ==========================================
        private static TreeClusterTestManager.ClusterOption BuildDemoVista(GameObject parent)
        {
            float posX = 255.0f;
            GameObject root = new GameObject("[DEMO_InContext_Vista]");
            root.transform.SetParent(parent.transform);
            root.transform.position = new Vector3(posX, 0f, 0f);

            // 1. Dải Ruộng Lúa Vàng (Midground Z = 2.0m .. 3.5m)
            SpawnPrefab(RiceClusterDense, root.transform, new Vector3(-12.0f, -1.5f, 2.5f), Vector3.zero, Vector3.one);
            SpawnPrefab(RiceClusterCanal, root.transform, new Vector3(0f, -1.5f, 2.8f), Vector3.zero, Vector3.one);
            SpawnPrefab(RiceClusterSparse, root.transform, new Vector3(12.0f, -1.5f, 2.2f), Vector3.zero, Vector3.one);
            SpawnPrefab(RiceSpriteGoldenBig, root.transform, new Vector3(-6.0f, -1.5f, 1.8f), Vector3.zero, Vector3.one);
            SpawnPrefab(RiceSpriteWindTuft, root.transform, new Vector3(6.0f, -1.5f, 2.0f), Vector3.zero, Vector3.one);

            // 2. Dải Rừng Cây Hậu Cảnh (Background Z = 6m .. 16m)
            SpawnPrefab(TreeBamboo, root.transform, new Vector3(-14.0f, -1.5f, 6.0f), Vector3.zero, new Vector3(1.3f, 1.3f, 1.3f));
            SpawnPrefab(TreeLargeCanopy, root.transform, new Vector3(-4.0f, -1.5f, 9.5f), Vector3.zero, new Vector3(1.35f, 1.35f, 1.35f));
            SpawnPrefab(TreeCoconut, root.transform, new Vector3(5.0f, -1.5f, 6.5f), Vector3.zero, new Vector3(1.25f, 1.25f, 1.25f));
            SpawnPrefab(TreeSlenderTall, root.transform, new Vector3(11.0f, -1.5f, 8.0f), Vector3.zero, new Vector3(1.2f, 1.2f, 1.2f));
            SpawnPrefab(TreeBanana, root.transform, new Vector3(1.5f, -1.5f, 4.5f), Vector3.zero, new Vector3(1.2f, 1.2f, 1.2f));
            SpawnPrefab(TreeSmallBush, root.transform, new Vector3(-8.5f, -1.5f, 5.0f), Vector3.zero, new Vector3(1.1f, 1.1f, 1.1f));

            // 3. Nhân vật Bé An ở vị trí đi bộ
            SpawnCharacterRef(root.transform, new Vector3(0f, -1.5f, 0f));
            CreateBanner(root.transform, new Vector3(0f, 7.2f, 0f), "DEMO VISTA: TOÀN CẢNH PHỐI HỢP RỪNG CÂY & ĐỒNG LÚA", "Phối hợp hoàn chỉnh: Bầu trời mây + Rừng cây hậu cảnh + Cánh đồng lúa vàng + Bé An.");

            return new TreeClusterTestManager.ClusterOption
            {
                optionName = "Demo Vista",
                title = "Toàn Cảnh Phối Hợp Rừng Cây & Đồng Lúa (Full In-Context Vista Demo)",
                description = "• Sự kết hợp mẫu giữa rừng cây hậu cảnh (Z=6..16m) với dải lúa vàng tiền cảnh (Z=2..3.5m).\n• Đánh giá trực tiếp cảm giác hòa quyện, phân tầng thị sai và chiều sâu không gian khi chơi game.",
                cameraX = posX,
                cameraDistance = 21.0f,
                cameraHeight = 3.0f,
                clusterRoot = root
            };
        }

        // ==========================================
        // HELPERS: PREFAB SPAWN & LABELS
        // ==========================================
        private static GameObject SpawnPrefab(string path, Transform parent, Vector3 localPos, Vector3 localRot, Vector3 localScale)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"[TreeClusterTest] Prefab not found: {path}");
                return null;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(parent);
            instance.transform.localPosition = localPos;
            instance.transform.localRotation = Quaternion.Euler(localRot);
            instance.transform.localScale = localScale;
            return instance;
        }

        private static void SpawnCharacterRef(Transform parent, Vector3 localPos)
        {
            GameObject anPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerAnPrefab);
            if (anPrefab != null)
            {
                GameObject anInst = (GameObject)PrefabUtility.InstantiatePrefab(anPrefab);
                anInst.name = "CharRef_BeAn";
                anInst.transform.SetParent(parent);
                anInst.transform.localPosition = localPos;
                anInst.transform.localRotation = Quaternion.Euler(0f, 90f, 0f); // Facing right
                anInst.transform.localScale = Vector3.one;

                // Disable player controller component so it acts as pure visual reference
                var pc = anInst.GetComponent<PlayerController>();
                if (pc != null) Object.DestroyImmediate(pc);
                var rb = anInst.GetComponent<Rigidbody2D>();
                if (rb != null) Object.DestroyImmediate(rb);
            }
        }

        private static void CreateBanner(Transform parent, Vector3 localPos, string title, string subTitle)
        {
            GameObject bannerGo = new GameObject("Stand_Title_Banner");
            bannerGo.transform.SetParent(parent);
            bannerGo.transform.localPosition = localPos;

            // Semi-translucent dark slate backing plane for high readability
            var bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bg.name = "Banner_Backing";
            bg.transform.SetParent(bannerGo.transform);
            bg.transform.localPosition = new Vector3(0f, 0f, 0.05f);
            bg.transform.localScale = new Vector3(18.5f, 1.85f, 1f);
            Object.DestroyImmediate(bg.GetComponent<Collider>());
            var bgMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            bgMat.color = new Color(0.08f, 0.12f, 0.16f, 0.82f);
            bg.GetComponent<MeshRenderer>().sharedMaterial = bgMat;

            // Simple 3D text mesh
            var tm = bannerGo.AddComponent<TextMesh>();
            tm.text = $"{title}\n{subTitle}";
            tm.fontSize = 28;
            tm.characterSize = 0.12f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = new Color(1f, 0.94f, 0.45f); // Golden yellow
        }

        [MenuItem("Tools/Hoa trên đất đỏ/Chụp ảnh các Phương Án Rừng Cây Test (10 PA + Demo Vista)", false, 11)]
        public static void CaptureAllSnapshots()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("No MainCamera found in scene!");
                return;
            }

            var mgr = Object.FindAnyObjectByType<TreeClusterTestManager>();
            if (mgr == null)
            {
                Debug.LogError("No TreeClusterTestManager found!");
                return;
            }

            string outDir = @"C:\Users\Thanh Tài\.gemini\antigravity\brain\1c44fbae-764e-472a-aec6-c9c375d88a28";
            int width = 1280;
            int height = 720;
            RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;

            string[] fileNames = new string[]
            {
                "tree_cluster_option_1.jpg",
                "tree_cluster_option_2.jpg",
                "tree_cluster_option_3.jpg",
                "tree_cluster_option_4.jpg",
                "tree_cluster_option_5.jpg",
                "tree_cluster_option_6.jpg",
                "tree_cluster_option_7.jpg",
                "tree_cluster_option_8.jpg",
                "tree_cluster_option_9.jpg",
                "tree_cluster_option_10.jpg",
                "tree_cluster_option_demo_vista.jpg"
            };

            for (int i = 0; i < mgr.Options.Count && i < fileNames.Length; i++)
            {
                var opt = mgr.Options[i];
                cam.transform.position = new Vector3(opt.cameraX, opt.cameraHeight, -opt.cameraDistance);
                cam.transform.rotation = Quaternion.Euler(2.5f, 0f, 0f);
                cam.Render();

                RenderTexture.active = rt;
                Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
                RenderTexture.active = null;

                byte[] jpg = tex.EncodeToJPG(80);
                string fullPath = Path.Combine(outDir, fileNames[i]);
                File.WriteAllBytes(fullPath, jpg);
                Object.DestroyImmediate(tex);
                Debug.Log($"[TreeClusterTest] Captured snapshot {i + 1}: {fullPath}");
            }

            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Debug.Log("<color=#10b981>[TreeClusterTest]</color> Đã chụp toàn bộ ảnh thành công!");
        }

        [MenuItem("Tools/Hoa trên đất đỏ/Xuất Toàn Bộ Cụm Cây Thành Prefabs (Export Tree Cluster Prefabs)", false, 12)]
        public static void ExportAllClustersToPrefabs()
        {
            // Open Scene if needed
            var currentScene = EditorSceneManager.GetActiveScene();
            if (currentScene.path != ScenePath)
            {
                currentScene = EditorSceneManager.OpenScene(ScenePath);
            }

            var mgr = Object.FindFirstObjectByType<TreeClusterTestManager>();
            if (mgr == null)
            {
                Debug.LogError("[TreeClusterTest] Không tìm thấy TreeClusterTestManager trong Scene!");
                return;
            }

            string targetFolder = "Assets/Prefabs/Environment/TreeClusters";
            if (!System.IO.Directory.Exists(targetFolder))
            {
                System.IO.Directory.CreateDirectory(targetFolder);
                AssetDatabase.Refresh();
            }

            string[] prefabNames = new string[]
            {
                "PF_TreeCluster_01_AiryBambooMelaleuca",
                "PF_TreeCluster_02_LushCoconutBanyan",
                "PF_TreeCluster_03_DeepLayeredUMinh",
                "PF_TreeCluster_04_WindsweptBambooTrees",
                "PF_TreeCluster_05_CanalBankOrchard",
                "PF_TreeCluster_06_HeroLandmark",
                "PF_TreeCluster_07_EndlessMelaleuca",
                "PF_TreeCluster_08_RiverbankCoconutFronds",
                "PF_TreeCluster_09_BambooGatewayArch",
                "PF_TreeCluster_10_VastHorizonSparse",
                "PF_TreeCluster_DemoVista_FullForestAndRice"
            };

            for (int i = 0; i < mgr.Options.Count && i < prefabNames.Length; i++)
            {
                var opt = mgr.Options[i];
                if (opt.clusterRoot == null) continue;

                // Create a clean detached clone for prefab creation
                GameObject clone = Object.Instantiate(opt.clusterRoot);
                clone.name = prefabNames[i];
                clone.transform.position = Vector3.zero;
                clone.transform.rotation = Quaternion.identity;
                clone.transform.localScale = Vector3.one;

                // Clean up scene-test-only objects (chỉ giữ lại CÂY CỐI & MÔI TRƯỜNG, xóa sạch text và nhân vật)
                List<GameObject> toRemove = new List<GameObject>();
                foreach (Transform child in clone.transform)
                {
                    string n = child.name;
                    if (n.StartsWith("Stand") || n.Contains("Banner") || n.StartsWith("CharRef") || 
                        n.Contains("Player") || n.Contains("BeAn") || child.GetComponentInChildren<TextMesh>() != null)
                    {
                        toRemove.Add(child.gameObject);
                    }
                }
                foreach (var obj in toRemove)
                {
                    Object.DestroyImmediate(obj);
                }

                string assetPath = $"{targetFolder}/{prefabNames[i]}.prefab";
                PrefabUtility.SaveAsPrefabAsset(clone, assetPath);
                Object.DestroyImmediate(clone);
                Debug.Log($"<color=#10b981>[TreeClusterTest]</color> Đã xuất Prefab thành công: {assetPath}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=#10b981>[TreeClusterTest]</color> Hoàn tất xuất toàn bộ 11 cụm cây thành Prefabs!");
        }

        private static void SetupCameraAndManager(GameObject parent, List<TreeClusterTestManager.ClusterOption> options)
        {
            GameObject camGo = new GameObject("PreviewCamera");
            camGo.transform.SetParent(parent.transform);
            camGo.tag = "MainCamera";

            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Color;
            cam.backgroundColor = new Color(0.55f, 0.75f, 0.92f);
            cam.fieldOfView = 36f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 120f;

            // Add Test Manager
            var mgr = camGo.AddComponent<TreeClusterTestManager>();
            var so = new SerializedObject(mgr);

            so.FindProperty("testCamera").objectReferenceValue = cam;
            var optProp = so.FindProperty("options");
            optProp.ClearArray();

            for (int i = 0; i < options.Count; i++)
            {
                optProp.InsertArrayElementAtIndex(i);
                var el = optProp.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("optionName").stringValue = options[i].optionName;
                el.FindPropertyRelative("title").stringValue = options[i].title;
                el.FindPropertyRelative("description").stringValue = options[i].description;
                el.FindPropertyRelative("cameraX").floatValue = options[i].cameraX;
                el.FindPropertyRelative("cameraDistance").floatValue = options[i].cameraDistance;
                el.FindPropertyRelative("cameraHeight").floatValue = options[i].cameraHeight;
                el.FindPropertyRelative("clusterRoot").objectReferenceValue = options[i].clusterRoot;
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            // Initial pos
            camGo.transform.position = new Vector3(-60f, 2.5f, -18f);
            camGo.transform.rotation = Quaternion.Euler(2.5f, 0f, 0f);
        }
    }
}
#endif
