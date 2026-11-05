using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace HoaVaMau.EditorScripts
{
    public static class PurgePaddyTreesAndDensifyRice
    {
        private const string RiceMeshPath = "Assets/Models/RicePlants/SM_Rice_Plant_Meshy_LOD_1.fbx";
        private const string RiceMatPath = "Assets/Materials/RicePlants/Mat_Rice_Meshy.mat";
        private const string ArtifactDir = "C:/Users/ACER/.gemini/antigravity/brain/0b06ba00-d73d-4df0-adee-d4635db3297d";

        private const float UNIFORM_GROUND_Y = 0.505f;
        private const float ROAD_CENTER_X = -7.0f;
        private const float ROAD_HALF_WIDTH = 3.50f;
        private const float RICE_CLEARANCE = 0.40f;

        // Rice Field Exact Boundaries (VÙNG BẢO VỆ TUYỆT ĐỐI)
        private const float PADDY_START_Z = 180.0f;
        private const float PADDY_END_Z = 34.0f;
        private const float FLANK_WIDTH = 36.0f;

        private const float WEST_INNER_X = ROAD_CENTER_X - ROAD_HALF_WIDTH - RICE_CLEARANCE; // -10.90f
        private const float WEST_OUTER_X = WEST_INNER_X - FLANK_WIDTH;                        // -46.90f

        private const float EAST_INNER_X = ROAD_CENTER_X + ROAD_HALF_WIDTH + RICE_CLEARANCE; // -3.10f
        private const float EAST_OUTER_X = EAST_INNER_X + FLANK_WIDTH;                        // +32.90f

        [MenuItem("Tools/Hoa Do/Purge Paddy Trees And Densify Rice")]
        public static void Execute()
        {
            Debug.Log("<color=#40b8a6>[PurgePaddyTreesAndDensifyRice]</color> Khởi động thanh lọc cây trong ruộng lúa & tăng mật độ lúa dày đặc...");

            var activeScene = SceneManager.GetActiveScene();

            // 1. XOÁ HẾT TẤT CẢ CÂY CỐI ĐANG Ở TRONG RUỘNG LÚA
            int purgedCount = PurgeTreesInsidePaddy();
            Debug.Log($"<color=#40b8a6>[PurgePaddyTreesAndDensifyRice]</color> Đã xoá sạch {purgedCount} cây cối/thực vật xâm lấn ruộng lúa!");

            // 2. CHỈNH SỬA LẠI CÂY CỐI KHU VỰC CỬA ẢI BẮC (Chỉ đặt trên sườn núi Z >= 184m, tuyệt đối không chạm ruộng lúa)
            ReorganizeNorthGateFoliage();

            // 3. CHỈNH SỬA LÚA DÀY ĐẶC HƠN NỮA NHƯNG VẪN CÂN BẰNG TUYỆT ĐỐI 2 BÊN
            int totalTufts = DensifySymmetricalRicePaddy();
            Debug.Log($"<color=#40b8a6>[PurgePaddyTreesAndDensifyRice]</color> Đã tạo xong {totalTufts} khóm lúa dày đặc, đối xứng hoàn hảo 2 bên!");

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            // 4. CHỤP ẢNH KIỂM CHỨNG TRỰC QUAN
            CaptureQA();
        }

        private static int PurgeTreesInsidePaddy()
        {
            var cropsRoot = GameObject.Find("Vast_Paddy_Continuous_Crops_Root");
            var allGos = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            var toDelete = new List<GameObject>();

            foreach (var go in allGos)
            {
                if (go == null) continue;

                // Bỏ qua bản thân ruộng lúa và các đối tượng địa hình, đường xá
                if (cropsRoot != null && (go == cropsRoot || go.transform.IsChildOf(cropsRoot.transform))) continue;
                if (go.name.Contains("Road") || go.name.Contains("Ground") || go.name.Contains("Terrain") ||
                    go.name.Contains("Water") || go.name.Contains("Canal") || go.name.Contains("Scarecrow")) continue;

                // Kiểm tra xem có phải là cây cối / thực vật không
                string n = go.name.ToLower();
                bool isPlant = n.Contains("tree") || n.Contains("bamboo") || n.Contains("coconut") ||
                               n.Contains("palm") || n.Contains("banana") || n.Contains("bush") || n.Contains("card_volumetric");

                if (!isPlant) continue;

                // Nếu là child của một cây khác, bỏ qua để xoá từ root cây
                if (go.transform.parent != null)
                {
                    string pn = go.transform.parent.name.ToLower();
                    if (pn.Contains("tree") || pn.Contains("bamboo") || pn.Contains("coconut") || pn.Contains("palm") || pn.Contains("banana"))
                        continue;
                }

                Vector3 p = go.transform.position;
                // Kiểm tra toạ độ xem có nằm trong phạm vi ruộng lúa Tây hoặc Đông
                bool inWest = (p.x >= -48.0f && p.x <= -9.5f && p.z >= 33.0f && p.z <= 182.0f);
                bool inEast = (p.x >= -4.5f && p.x <= 34.5f && p.z >= 33.0f && p.z <= 182.0f);

                if (inWest || inEast)
                {
                    toDelete.Add(go);
                }
            }

            int count = toDelete.Count;
            foreach (var go in toDelete)
            {
                if (go != null)
                {
                    Undo.DestroyObjectImmediate(go);
                }
            }

            return count;
        }

        private static void ReorganizeNorthGateFoliage()
        {
            var transGroup = GameObject.Find("Transition_Valley_Foliage_Image1");
            if (transGroup != null)
            {
                // Xoá toàn bộ các cây cũ trong group này vì trước đó bị đặt lấn vào ruộng lúa (Z <= 181m)
                while (transGroup.transform.childCount > 0)
                {
                    var child = transGroup.transform.GetChild(0).gameObject;
                    Undo.DestroyObjectImmediate(child);
                }

                // Tái thiết cây cối CHỈ Ở TRÊN SƯỜN NÚI VÀ ĐÈO NÚI (Z >= 184.5m, hoàn toàn bên ngoài ruộng lúa!)
                var pBamboo = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_07_BambooGrove.prefab");
                var pCoconut = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_06_Coconut.prefab");
                var pHeroTree = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Trees/PF_Lana_Hero_Tree_Layered.prefab");

                // Rừng tre trên sườn núi Tây (X <= -13.5m, Z >= 185m)
                if (pBamboo != null)
                {
                    SpawnTree(pBamboo, transGroup.transform, "Mountain_Bamboo_West_01", new Vector3(-14.5f, 1.2f, 186.0f), Quaternion.Euler(0f, 35f, 0f), 1.35f);
                    SpawnTree(pBamboo, transGroup.transform, "Mountain_Bamboo_West_02", new Vector3(-18.5f, 2.5f, 188.0f), Quaternion.Euler(0f, 75f, 0f), 1.4f);
                    SpawnTree(pBamboo, transGroup.transform, "Mountain_Bamboo_West_03", new Vector3(-24.0f, 3.8f, 190.0f), Quaternion.Euler(0f, 20f, 0f), 1.45f);

                    // Rừng tre trên sườn núi Đông (X >= 2.5m, Z >= 185m)
                    SpawnTree(pBamboo, transGroup.transform, "Mountain_Bamboo_East_01", new Vector3(2.8f, 1.2f, 186.0f), Quaternion.Euler(0f, -35f, 0f), 1.35f);
                    SpawnTree(pBamboo, transGroup.transform, "Mountain_Bamboo_East_02", new Vector3(6.5f, 2.5f, 188.0f), Quaternion.Euler(0f, -75f, 0f), 1.4f);
                    SpawnTree(pBamboo, transGroup.transform, "Mountain_Bamboo_East_03", new Vector3(11.0f, 3.8f, 190.0f), Quaternion.Euler(0f, -20f, 0f), 1.45f);
                }

                // Dừa nước sườn núi xa
                if (pCoconut != null)
                {
                    SpawnTree(pCoconut, transGroup.transform, "Mountain_Coconut_West_01", new Vector3(-20.0f, 3.0f, 187.0f), Quaternion.Euler(0f, 45f, -4f), 1.2f);
                    SpawnTree(pCoconut, transGroup.transform, "Mountain_Coconut_East_01", new Vector3(8.5f, 3.0f, 187.0f), Quaternion.Euler(0f, -45f, 4f), 1.2f);
                }

                // Cây cổ thụ tự nhiên tựa lưng vào vách đá
                if (pHeroTree != null)
                {
                    SpawnTree(pHeroTree, transGroup.transform, "HeroTree_West_Cliff", new Vector3(-28.0f, 4.0f, 192.0f), Quaternion.Euler(0f, 60f, 0f), 1.5f);
                    SpawnTree(pHeroTree, transGroup.transform, "HeroTree_East_Cliff", new Vector3(15.0f, 4.0f, 192.0f), Quaternion.Euler(0f, -60f, 0f), 1.5f);
                }
            }
        }

        private static int DensifySymmetricalRicePaddy()
        {
            var cropsRoot = GameObject.Find("Vast_Paddy_Continuous_Crops_Root");
            if (cropsRoot == null)
            {
                cropsRoot = new GameObject("Vast_Paddy_Continuous_Crops_Root");
                cropsRoot.transform.position = Vector3.zero;
            }

            // Tìm hoặc tạo West & East Flank
            Transform pWest = cropsRoot.transform.Find("West_Paddy_Flank");
            if (pWest == null)
            {
                pWest = new GameObject("West_Paddy_Flank").transform;
                pWest.SetParent(cropsRoot.transform, false);
            }
            else
            {
                // Xoá sạch toàn bộ lúa cũ của cánh Tây
                while (pWest.childCount > 0)
                {
                    Undo.DestroyObjectImmediate(pWest.GetChild(0).gameObject);
                }
            }

            Transform pEast = cropsRoot.transform.Find("East_Paddy_Flank");
            if (pEast == null)
            {
                pEast = new GameObject("East_Paddy_Flank").transform;
                pEast.SetParent(cropsRoot.transform, false);
            }
            else
            {
                // Xoá sạch toàn bộ lúa cũ của cánh Đông
                while (pEast.childCount > 0)
                {
                    Undo.DestroyObjectImmediate(pEast.GetChild(0).gameObject);
                }
            }

            var riceMesh = LoadMeshFromFbx(RiceMeshPath);
            var riceMat = AssetDatabase.LoadAssetAtPath<Material>(RiceMatPath);
            if (riceMesh == null || riceMat == null)
            {
                Debug.LogError("[PurgePaddyTreesAndDensifyRice] Không tải được Mesh hoặc Material của Lúa!");
                return 0;
            }

            var rng = new System.Random(1954);

            // THÔNG SỐ MẬT ĐỘ LÚA MỚI (DÀY ĐẶC GẤP ĐÔI, ĐỐI XỨNG CÂN BẰNG 100%)
            // Tăng từ 76 hàng lên 96 hàng (khoảng cách Z ~1.53m)
            // Tăng từ 18 khóm lên 24 khóm mỗi hàng (khoảng cách X ~1.56m)
            // Tăng kích thước khóm từ 3.10-3.55m lên 3.45-3.90m để các khóm ken dày đặc đan cài vào nhau
            int rows = 96;
            int clustersPerRow = 24;
            int totalCreated = 0;

            for (int r = 0; r < rows; r++)
            {
                float tz = (float)r / (rows - 1);
                float z = Mathf.Lerp(PADDY_START_Z, PADDY_END_Z, tz);

                float hexOffset = (r % 2 == 1) ? 0.60f : 0.0f;

                // --- CÁNH ĐỒNG TÂY (West Flank - 24 khóm) ---
                for (int c = 0; c < clustersPerRow; c++)
                {
                    float frac = (float)c / (clustersPerRow - 1);
                    float x = Mathf.Lerp(WEST_OUTER_X, WEST_INNER_X, frac) + ((c % 2 == 1) ? hexOffset : -hexOffset) * 0.35f;
                    float zJitter = z + ((float)rng.NextDouble() - 0.5f) * 0.15f;

                    float s = 3.45f + (float)rng.NextDouble() * 0.45f;
                    float sy = s * (1.0f + (float)rng.NextDouble() * 0.15f);

                    CreateRiceTuft(pWest, riceMesh, riceMat, new Vector3(x, UNIFORM_GROUND_Y, zJitter), s, sy, rng);
                    totalCreated++;
                }

                // --- CÁNH ĐỒNG ĐÔNG (East Flank - 24 khóm, đối xứng toán học tuyệt đối) ---
                for (int c = 0; c < clustersPerRow; c++)
                {
                    float frac = (float)c / (clustersPerRow - 1);
                    float x = Mathf.Lerp(EAST_INNER_X, EAST_OUTER_X, frac) + ((c % 2 == 1) ? hexOffset : -hexOffset) * 0.35f;
                    float zJitter = z + ((float)rng.NextDouble() - 0.5f) * 0.15f;

                    float s = 3.45f + (float)rng.NextDouble() * 0.45f;
                    float sy = s * (1.0f + (float)rng.NextDouble() * 0.15f);

                    CreateRiceTuft(pEast, riceMesh, riceMat, new Vector3(x, UNIFORM_GROUND_Y, zJitter), s, sy, rng);
                    totalCreated++;
                }
            }

            return totalCreated;
        }

        private static void CreateRiceTuft(Transform parent, Mesh mesh, Material mat, Vector3 pos, float s, float sy, System.Random rng)
        {
            var tuft = new GameObject("VastRice_Tuft");
            tuft.transform.SetParent(parent, false);
            tuft.transform.position = pos;

            float rotY = (float)rng.NextDouble() * 360f;
            tuft.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
            tuft.transform.localScale = new Vector3(s, sy, s);

            var mf = tuft.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            var mr = tuft.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
        }

        private static void SpawnTree(GameObject prefab, Transform parent, string name, Vector3 pos, Quaternion rot, float scale)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            if (go != null)
            {
                go.name = name;
                go.transform.position = pos;
                go.transform.rotation = rot;
                go.transform.localScale = Vector3.one * scale;
            }
        }

        private static Mesh LoadMeshFromFbx(string fbxPath)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (var asset in assets)
            {
                if (asset is Mesh m && !m.name.StartsWith("__preview__"))
                {
                    return m;
                }
            }
            return null;
        }

        [MenuItem("Tools/Hoa Do/Capture Paddy QA")]
        public static void CaptureQA()
        {
            if (!Directory.Exists(ArtifactDir)) Directory.CreateDirectory(ArtifactDir);

            // 1. Góc đại cảnh trên cao nhìn toàn bộ ruộng lúa từ Nam ra Bắc (Hình 1 chuẩn)
            Vector3 camPos1 = new Vector3(-7.0f, 38.0f, 115.0f);
            Quaternion camRot1 = Quaternion.Euler(36.0f, 0.0f, 0.0f);
            RenderView(camPos1, camRot1, 58.0f, Path.Combine(ArtifactDir, "Test_RuongLua_DayDac_Aerial_QA.jpg"));

            // 2. Góc nhìn từ đường đất nhìn thẳng về cổng làng Bắc (xác nhận sạch bóng cây trong ruộng)
            Vector3 camPos2 = new Vector3(-7.0f, 2.0f, 100.0f);
            Quaternion camRot2 = Quaternion.Euler(4.0f, 0.0f, 0.0f);
            RenderView(camPos2, camRot2, 60.0f, Path.Combine(ArtifactDir, "Test_RuongLua_DayDac_EyeLevel_QA.jpg"));

            // 3. Góc nhìn từ Cổng làng Bắc nhìn vào ruộng lúa (Hình 2 view)
            Vector3 camPos3 = new Vector3(-7.0f, 2.15f, 175.2f);
            Quaternion camRot3 = Quaternion.Euler(6.0f, 0.0f, 0.0f);
            RenderView(camPos3, camRot3, 50.0f, Path.Combine(ArtifactDir, "Test_Hinh2_Gate_Gorge_QA.jpg"));

            Debug.Log("[CaptureQA] Đã chụp xong cả 3 ảnh kiểm chứng ruộng lúa!");
        }

        private static void RenderView(Vector3 pos, Quaternion rot, float fov, string outputPath)
        {
            var camGo = new GameObject("Temp_QA_Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.transform.position = pos;
            cam.transform.rotation = rot;
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 1000f;

            int width = 1280;
            int height = 720;
            var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            byte[] bytes = tex.EncodeToJPG(80);
            File.WriteAllBytes(outputPath, bytes);

            cam.targetTexture = null;
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);
            UnityEngine.Object.DestroyImmediate(camGo);
            UnityEngine.Object.DestroyImmediate(tex);

            Debug.Log($"[RenderView] Đã lưu ảnh QA: {outputPath}");
        }
    }
}
