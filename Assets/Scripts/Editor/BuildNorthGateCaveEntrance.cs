using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace HoaVaMau.EditorScripts
{
    public static class BuildNorthGateCaveEntrance
    {
        private const string CaveRootName = "Gate_North_Cave_Entrance";
        private const string RockLargeFbx = "Assets/Models/Optimized/SM_Rock_Large_BlenderOptimized.fbx";
        private const string RockMediumFbx = "Assets/Models/Optimized/SM_Rock_Medium_02_BlenderOptimized.fbx";
        private const string RockMatPath = "Assets/Materials/Environment/Rocks/M_Mountain_Crag_Rock.mat";
        private const string RoadMatPath = "Assets/Materials/M_Village_Road_NamBo.mat";
        private const string MudMatPath = "Assets/Materials/M_CaMau_PaddyMud.mat";

        private const string ArtifactDir = "C:/Users/ACER/.gemini/antigravity/brain/0b06ba00-d73d-4df0-adee-d4635db3297d";

        [MenuItem("Tools/Hoa Do/Build North Gate Cave Entrance")]
        public static void BuildCaveEntrance()
        {
            Debug.Log("<color=#40b8a6>[BuildNorthGateCaveEntrance]</color> Bắt đầu kiến tạo hang núi đá tự nhiên tại Cổng Làng Bắc...");

            var activeScene = SceneManager.GetActiveScene();

            // 1. Load Meshes & Materials
            Mesh rockLargeMesh = LoadMeshFromFbx(RockLargeFbx);
            Mesh rockMediumMesh = LoadMeshFromFbx(RockMediumFbx);
            Material rockMat = AssetDatabase.LoadAssetAtPath<Material>(RockMatPath);
            Material roadMat = AssetDatabase.LoadAssetAtPath<Material>(RoadMatPath) ??
                               AssetDatabase.LoadAssetAtPath<Material>(MudMatPath);

            if (rockLargeMesh == null || rockMat == null)
            {
                Debug.LogError("[BuildNorthGateCaveEntrance] Không load được rock mesh hoặc material!");
                return;
            }

            // 2. Clean up old floating rocks and previous cave test
            DestroyIfExists("Rock_Backdrop_CenterPass");
            DestroyIfExists("Rock_Backdrop_WestMonolith");
            DestroyIfExists("Rock_Backdrop_EastMonolith");
            DestroyIfExists(CaveRootName);

            // 3. Create Root
            var caveRoot = new GameObject(CaveRootName);
            var gate = GameObject.Find("Gate_Village_North");
            if (gate != null)
            {
                caveRoot.transform.SetParent(gate.transform.parent, true);
            }
            caveRoot.transform.position = new Vector3(-7.0f, 0.505f, 184.0f);

            var groupWestWall = CreateGroup(caveRoot.transform, "Cave_West_Wall");
            var groupEastWall = CreateGroup(caveRoot.transform, "Cave_East_Wall");
            var groupArch = CreateGroup(caveRoot.transform, "Cave_Overhead_Arch");
            var groupInterior = CreateGroup(caveRoot.transform, "Cave_Interior_Backdrop");
            var groupFloor = CreateGroup(caveRoot.transform, "Cave_Tunnel_Floor");
            var groupDetails = CreateGroup(caveRoot.transform, "Cave_Mouth_Details");

            // =========================================================================
            // 4. CAVE FLOOR (Đường đất nền hang kéo dài từ cổng Z=184m vào Z=208m)
            // Corridor width = 4.5m (X: -9.25m đến -4.75m, tâm -7.0m)
            // =========================================================================
            CreateCaveFloorPlane(groupFloor.transform, "Cave_Soil_Path",
                new Vector3(-7.0f, 0.508f, 196.0f), new Vector3(5.5f, 1.0f, 26.0f), roadMat);

            // =========================================================================
            // 5. WEST CAVE WALL (Vách đá phía Tây, X <= -9.0m, ôm sát cột trái cổng làng)
            // Cột trái cổng làng ở X = -8.5m. Vách đá ôm từ X = -9.2m trở ra ngoài
            // =========================================================================
            // A. Chân vách đá ôm sát cột trái cổng làng (Z = 184.0m)
            SpawnRockPiece(groupWestWall.transform, "WestWall_GateFlank_01", rockMediumMesh ?? rockLargeMesh, rockMat,
                new Vector3(-10.8f, 1.8f, 184.2f),
                Quaternion.Euler(0f, 35f, -5f),
                new Vector3(2.2f, 3.2f, 2.4f));

            // B. Khối vách đá lớn nâng cao sườn Tây (Z = 185.5m)
            SpawnRockPiece(groupWestWall.transform, "WestWall_GateFlank_High", rockLargeMesh, rockMat,
                new Vector3(-12.8f, 4.2f, 185.5f),
                Quaternion.Euler(10f, 45f, -8f),
                new Vector3(3.8f, 6.5f, 4.0f));

            // C. Vách đá tầng giữa trong hang Tây (Z = 191.0m)
            SpawnRockPiece(groupWestWall.transform, "WestWall_MidTunnel", rockLargeMesh, rockMat,
                new Vector3(-12.4f, 3.5f, 191.0f),
                Quaternion.Euler(5f, 50f, 0f),
                new Vector3(3.8f, 6.8f, 4.2f));

            // C2. Khối vách đá mở rộng phía Tây kết nối sườn núi (Z = 187.0m, X = -16.5m)
            SpawnRockPiece(groupWestWall.transform, "WestWall_FlankConnector", rockLargeMesh, rockMat,
                new Vector3(-16.5f, 3.8f, 187.0f),
                Quaternion.Euler(0f, 30f, -6f),
                new Vector3(5.5f, 8.2f, 4.8f));

            // D. Vách đá sâu trong hang Tây (Z = 198.0m)
            SpawnRockPiece(groupWestWall.transform, "WestWall_DeepTunnel", rockLargeMesh, rockMat,
                new Vector3(-12.2f, 3.8f, 198.5f),
                Quaternion.Euler(-5f, 30f, 5f),
                new Vector3(3.8f, 7.2f, 4.4f));

            // E. Đá chân vách che khe nối mặt đất Tây (X = -9.6m)
            SpawnRockPiece(groupWestWall.transform, "WestWall_Ledge_01", rockMediumMesh ?? rockLargeMesh, rockMat,
                new Vector3(-9.6f, 0.7f, 188.0f),
                Quaternion.Euler(0f, 80f, 0f),
                new Vector3(1.6f, 1.4f, 2.2f));

            SpawnRockPiece(groupWestWall.transform, "WestWall_Ledge_02", rockMediumMesh ?? rockLargeMesh, rockMat,
                new Vector3(-9.5f, 0.7f, 195.0f),
                Quaternion.Euler(0f, 65f, 0f),
                new Vector3(1.6f, 1.4f, 2.2f));

            // =========================================================================
            // 6. EAST CAVE WALL (Vách đá phía Đông, X >= -5.0m, ôm sát cột phải cổng làng)
            // Cột phải cổng làng ở X = -5.5m. Vách đá ôm từ X = -4.8m trở ra ngoài
            // =========================================================================
            // A. Chân vách đá ôm sát cột phải cổng làng (Z = 184.0m)
            SpawnRockPiece(groupEastWall.transform, "EastWall_GateFlank_01", rockMediumMesh ?? rockLargeMesh, rockMat,
                new Vector3(-3.2f, 1.8f, 184.2f),
                Quaternion.Euler(0f, -35f, 5f),
                new Vector3(2.2f, 3.2f, 2.4f));

            // B. Khối vách đá lớn nâng cao sườn Đông (Z = 185.5m)
            SpawnRockPiece(groupEastWall.transform, "EastWall_GateFlank_High", rockLargeMesh, rockMat,
                new Vector3(-1.2f, 4.2f, 185.5f),
                Quaternion.Euler(10f, -45f, 8f),
                new Vector3(3.8f, 6.5f, 4.0f));

            // C. Vách đá tầng giữa trong hang Đông (Z = 191.0m)
            SpawnRockPiece(groupEastWall.transform, "EastWall_MidTunnel", rockLargeMesh, rockMat,
                new Vector3(-1.6f, 3.5f, 191.0f),
                Quaternion.Euler(5f, -50f, 0f),
                new Vector3(3.8f, 6.8f, 4.2f));

            // C2. Khối vách đá mở rộng phía Đông kết nối sườn núi (Z = 187.0m, X = +3.5m)
            SpawnRockPiece(groupEastWall.transform, "EastWall_FlankConnector", rockLargeMesh, rockMat,
                new Vector3(3.5f, 3.8f, 187.0f),
                Quaternion.Euler(0f, -30f, 6f),
                new Vector3(5.5f, 8.2f, 4.8f));

            // D. Vách đá sâu trong hang Đông (Z = 198.0m)
            SpawnRockPiece(groupEastWall.transform, "EastWall_DeepTunnel", rockLargeMesh, rockMat,
                new Vector3(-1.8f, 3.8f, 198.5f),
                Quaternion.Euler(-5f, -30f, -5f),
                new Vector3(3.8f, 7.2f, 4.4f));

            // E. Đá chân vách che khe nối mặt đất Đông (X = -4.4m)
            SpawnRockPiece(groupEastWall.transform, "EastWall_Ledge_01", rockMediumMesh ?? rockLargeMesh, rockMat,
                new Vector3(-4.4f, 0.7f, 188.0f),
                Quaternion.Euler(0f, -80f, 0f),
                new Vector3(1.6f, 1.4f, 2.2f));

            SpawnRockPiece(groupEastWall.transform, "EastWall_Ledge_02", rockMediumMesh ?? rockLargeMesh, rockMat,
                new Vector3(-4.5f, 0.7f, 195.0f),
                Quaternion.Euler(0f, -65f, 0f),
                new Vector3(1.6f, 1.4f, 2.2f));

            // =========================================================================
            // 7. CAVE ARCH / OVERHEAD ROOF (Vòm trần hang đá bao trùm trên mái cổng làng)
            // Độ cao: Y >= 4.8m để không che lấp cổng gỗ hay nhân vật
            // =========================================================================
            // A. Vòm đá mặt trước phía trên mái cổng (Z = 185.5m, Y = 6.2m)
            SpawnRockPiece(groupArch.transform, "Arch_FrontMouth", rockLargeMesh, rockMat,
                new Vector3(-7.0f, 6.2f, 185.5f),
                Quaternion.Euler(180f, 90f, 0f),
                new Vector3(6.5f, 3.8f, 4.2f));

            // B. Vòm đá giữa hang (Z = 192.5m, Y = 6.8f)
            SpawnRockPiece(groupArch.transform, "Arch_MidRoof", rockLargeMesh, rockMat,
                new Vector3(-7.0f, 6.8f, 192.5f),
                Quaternion.Euler(185f, 45f, 0f),
                new Vector3(7.2f, 4.2f, 4.8f));

            // C. Vòm đá sâu trong hang (Z = 199.5m, Y = 7.2f)
            SpawnRockPiece(groupArch.transform, "Arch_DeepRoof", rockLargeMesh, rockMat,
                new Vector3(-7.0f, 7.2f, 199.5f),
                Quaternion.Euler(175f, -45f, 0f),
                new Vector3(7.5f, 4.5f, 5.0f));

            // =========================================================================
            // 8. CAVE INTERIOR BACKDROP (Vách đá sâu hun hút bên trong hang, Z >= 204m)
            // Bịt kín hoàn toàn khoảng trời và cỏ xanh bên dưới, tạo cảm giác hang sâu thẳm
            // =========================================================================
            // A. Vách đá đáy hang chính diện (Z = 205.0m, Y = 3.2m)
            // Hạ thấp xuống Y = 3.2m với chiều cao 8.5m -> Chân vách cắm sâu xuống Y = 0m, đỉnh vách cao 7.5m
            SpawnRockPiece(groupInterior.transform, "Interior_BackWall_Center", rockLargeMesh, rockMat,
                new Vector3(-7.0f, 3.2f, 205.0f),
                Quaternion.Euler(0f, 85f, 0f),
                new Vector3(6.5f, 8.5f, 4.5f));

            // B. Vách đá uốn cong góc Tây sâu trong hang (Z = 208.0m)
            SpawnRockPiece(groupInterior.transform, "Interior_BackWall_West", rockLargeMesh, rockMat,
                new Vector3(-11.0f, 3.8f, 208.0f),
                Quaternion.Euler(0f, 40f, 0f),
                new Vector3(6.0f, 9.2f, 4.5f));

            // C. Vách đá uốn cong góc Đông sâu trong hang (Z = 208.0m)
            SpawnRockPiece(groupInterior.transform, "Interior_BackWall_East", rockLargeMesh, rockMat,
                new Vector3(-3.0f, 3.8f, 208.0f),
                Quaternion.Euler(0f, -40f, 0f),
                new Vector3(6.0f, 9.2f, 4.5f));

            // D. Núi cao trên đỉnh hang che bầu trời phía trên vòm đá (Z = 212.0m, Y = 12.0m)
            SpawnRockPiece(groupInterior.transform, "Interior_MountainPeak_Top", rockLargeMesh, rockMat,
                new Vector3(-7.0f, 12.0f, 212.0f),
                Quaternion.Euler(0f, 20f, 0f),
                new Vector3(18.0f, 14.0f, 12.0f));

            // =========================================================================
            // 9. CAVE MOUTH FOLIAGE DETAILS (Tre trúc, dương xỉ viền vách đá cửa hang)
            // =========================================================================
            var prefabBamboo = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_VN_Pack/Trees/PF_LanaVN_Tree_07_BambooGrove.prefab");
            var prefabBush1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Lana_Concept_Assets/Bushes/PF_T_HD_Bush_Broadleaf_01.prefab");

            if (prefabBamboo != null)
            {
                SpawnDetailPrefab(prefabBamboo, groupDetails.transform, "CaveMouth_Bamboo_West",
                    new Vector3(-11.2f, 0.505f, 183.2f), Quaternion.Euler(0f, 30f, 0f), new Vector3(1.1f, 1.2f, 1.1f));
                SpawnDetailPrefab(prefabBamboo, groupDetails.transform, "CaveMouth_Bamboo_East",
                    new Vector3(-2.8f, 0.505f, 183.2f), Quaternion.Euler(0f, -30f, 0f), new Vector3(1.1f, 1.2f, 1.1f));
            }

            if (prefabBush1 != null)
            {
                SpawnDetailPrefab(prefabBush1, groupDetails.transform, "CaveMouth_Fern_West",
                    new Vector3(-9.2f, 0.52f, 184.6f), Quaternion.Euler(0f, 45f, 0f), new Vector3(0.85f, 0.85f, 0.85f));
                SpawnDetailPrefab(prefabBush1, groupDetails.transform, "CaveMouth_Fern_East",
                    new Vector3(-4.8f, 0.52f, 184.6f), Quaternion.Euler(0f, -45f, 0f), new Vector3(0.85f, 0.85f, 0.85f));

                // Bụi dương xỉ vắt ngang trên mái cổng gỗ
                SpawnDetailPrefab(prefabBush1, groupDetails.transform, "CaveMouth_RoofFern_01",
                    new Vector3(-8.0f, 4.2f, 184.3f), Quaternion.Euler(15f, 20f, 0f), new Vector3(0.75f, 0.75f, 0.75f));
                SpawnDetailPrefab(prefabBush1, groupDetails.transform, "CaveMouth_RoofFern_02",
                    new Vector3(-5.8f, 4.2f, 184.3f), Quaternion.Euler(15f, -20f, 0f), new Vector3(0.75f, 0.75f, 0.75f));
            }

            // =========================================================================
            // 10. UPDATE PLAYER POSITION TO START INSIDE CAVE MOUTH
            // =========================================================================
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                player.transform.position = new Vector3(-7.0f, 0.505f, 186.5f);
                player.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                EditorUtility.SetDirty(player);
            }

            EditorSceneManager.MarkSceneDirty(activeScene);
            Debug.Log("<color=#40b8a6>[BuildNorthGateCaveEntrance]</color> Hoàn tất kiến tạo Hang Núi Đá Tự Nhiên tại Cổng Làng Bắc!");

            // Tự động chụp ảnh QA ngay lập tức
            CaptureGateCaveQA();
        }

        private static Mesh LoadMeshFromFbx(string path)
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (fbx != null)
            {
                var mf = fbx.GetComponentInChildren<MeshFilter>();
                if (mf != null) return mf.sharedMesh;
            }
            return AssetDatabase.LoadAssetAtPath<Mesh>(path);
        }

        private static void DestroyIfExists(string name)
        {
            var go = GameObject.Find(name);
            if (go != null)
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static GameObject CreateGroup(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static GameObject SpawnRockPiece(Transform parent, string name, Mesh mesh, Material mat, Vector3 pos, Quaternion rot, Vector3 scale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = rot;
            go.transform.localScale = scale;

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;

            return go;
        }

        private static GameObject SpawnDetailPrefab(GameObject prefab, Transform parent, string name, Vector3 pos, Quaternion rot, Vector3 scale)
        {
            if (prefab == null) return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            if (go != null)
            {
                go.name = name;
                go.transform.position = pos;
                go.transform.rotation = rot;
                go.transform.localScale = scale;
            }
            return go;
        }

        private static void CreateCaveFloorPlane(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Plane);
            quad.name = name;
            quad.transform.SetParent(parent, false);
            quad.transform.position = pos;
            quad.transform.rotation = Quaternion.identity;
            quad.transform.localScale = new Vector3(scale.x * 0.1f, 1.0f, scale.z * 0.1f);

            var col = quad.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.DestroyImmediate(col);

            var mr = quad.GetComponent<MeshRenderer>();
            if (mr != null && mat != null) mr.sharedMaterial = mat;
        }

        [MenuItem("Tools/Hoa Do/Capture Gate Cave QA Screenshot")]
        public static void CaptureGateCaveQA()
        {
            if (!Directory.Exists(ArtifactDir)) Directory.CreateDirectory(ArtifactDir);

            // Góc nhìn Hình 1: Toàn cảnh cổng làng nhìn hướng Bắc vào hang đá
            Vector3 camPos1 = new Vector3(-7.0f, 2.35f, 175.0f);
            Quaternion camRot1 = Quaternion.Euler(6.5f, 0.0f, 0.0f);
            RenderView(camPos1, camRot1, 50.0f, Path.Combine(ArtifactDir, "Test_Gate_Cave_QA.jpg"));

            // Góc nhìn Cận cảnh mặt Bé An bước ra khỏi hang
            Vector3 camPos2 = new Vector3(-7.0f, 1.32f, 182.65f);
            Quaternion camRot2 = Quaternion.Euler(1.0f, 0.0f, 0.0f);
            RenderView(camPos2, camRot2, 28.0f, Path.Combine(ArtifactDir, "Test_Gate_Cave_Closeup_QA.jpg"));

            Debug.Log("[CaptureGateCaveQA] Đã xuất ảnh QA vào artifacts directory!");
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
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
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
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(tex);
            UnityEngine.Object.DestroyImmediate(camGo);
        }
    }
}
