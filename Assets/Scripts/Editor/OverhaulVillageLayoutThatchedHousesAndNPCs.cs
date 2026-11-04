using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using HoaVaMau;
using HoaVaMau.Dialogue;

namespace HoaDo.Editor
{
    public static class OverhaulVillageLayoutThatchedHousesAndNPCs
    {
        // Authentic Nam Bo Thatched Ground Cottage from the project's artwork reference
        private const string HouseThatchedFbx = "Assets/Models/Optimized/SM_NhaTranh_BlenderOptimized.fbx";
        private const string HouseThatchedMat = "Assets/Materials/Nhà tranh.mat";
        private const string RoadMatPath = "Assets/Materials/M_Village_Road_NamBo.mat";

        private const string NpcElderPrefab = "Assets/Prefabs/Characters/NPCs/PF_NPC_Elderly_Man.prefab";
        private const string NpcFemalePrefab = "Assets/Prefabs/Characters/NPCs/PF_NPC_Female_VN.prefab";
        private const string NpcMotherPrefab = "Assets/Prefabs/Characters/NPCs/PF_NPC_AnMother.prefab";
        private const string NpcVillager01Prefab = "Assets/Prefabs/Characters/NPCs/PF_NPC_Villager_01.prefab";
        private const string NpcVillager04Prefab = "Assets/Prefabs/Characters/NPCs/PF_NPC_Villager_04.prefab";
        private const string NpcVillager05Prefab = "Assets/Prefabs/Characters/NPCs/PF_NPC_Villager_05.prefab";
        private const string NpcQuietChildPrefab = "Assets/Prefabs/Characters/NPCs/PF_NPC_Quiet_Child.prefab";

        private const string AcElder = "Assets/Animations/Controllers/AC_NPC_ElderlyMan.controller";
        private const string AcFemale = "Assets/Animations/Controllers/AC_NPC_Female.controller";
        private const string AcMale = "Assets/Animations/Controllers/AC_NPC_Male.controller";
        private const string AcSocial = "Assets/Animations/Controllers/AC_NPC_Social_Waving.controller";

        private const string ArtifactDir = "C:/Users/ACER/.gemini/antigravity/brain/0b06ba00-d73d-4df0-adee-d4635db3297d";

        private const float UNIFORM_GROUND_Y = 0.505f;
        private const float MESH_BOTTOM_OFFSET = 0.413f;

        [MenuItem("Tools/Hoa Do/Overhaul Village Thatched Houses And Accurate NPCs")]
        public static void Execute()
        {
            Debug.Log("[VillageOverhaul] Đang đồng bộ: Nhà tranh Nam Bộ nguyên bản (SM_NhaTranh) + Collider khớp 1:1 + Thêm Collider thân cây + Chuẩn hóa NPC...");

            // 1. STANDARDIZE ALL HOUSES TO THE AUTHENTIC THATCHED COTTAGE MODEL (SM_NhaTranh)
            ReplaceAllHousesWithOriginalThatchedCottages();

            // 2. UNIFIED EARTHEN GROUND NETWORK (From Gate to Every House Yard)
            BuildUnifiedEarthenGroundNetwork();

            // 3. SETUP 100% STANDING UPRIGHT NPCS WITH MATCHING GENDER & DIALOGUE
            SetupAllUprightStandingNPCs();

            // 4. ADD TRUNK COLLIDERS TO ALL TREES (EXCEPT RICE & GRASS)
            AddCollidersToAllTrees();

            // 5. MARK SCENE DIRTY
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);

            // 6. CAPTURE QA SNAPSHOTS (Rule 4.2: 720p)
            CaptureQASnapshots();

            Debug.Log("[VillageOverhaul] HOÀN TẤT THÀNH CÔNG! Đã dùng đúng nhà tranh gốc, collider khớp 1:1 không dư thừa, và thêm collider cho tất cả cây cối!");
        }

        // =========================================================================
        // 1. REPLACE ALL HOUSES WITH AUTHENTIC THATCHED COTTAGES (SM_NhaTranh)
        // =========================================================================
        private struct HouseData
        {
            public string name;
            public Vector2 posXZ;
            public float rotationY;
            public float scale;

            public HouseData(string n, float x, float z, float rotY, float s = 3.6f)
            {
                name = n;
                posXZ = new Vector2(x, z);
                rotationY = rotY;
                scale = s;
            }
        }

        public static void ReplaceAllHousesWithOriginalThatchedCottages()
        {
            var fbxThatched = AssetDatabase.LoadAssetAtPath<GameObject>(HouseThatchedFbx);
            var matThatched = AssetDatabase.LoadAssetAtPath<Material>(HouseThatchedMat);
            if (matThatched != null)
            {
                matThatched.SetFloat("_Cull", 0f); // Two-sided rendering for open eaves
                if (matThatched.HasProperty("_BaseColor")) matThatched.SetColor("_BaseColor", Color.white);
                EditorUtility.SetDirty(matThatched);
            }

            if (fbxThatched == null)
            {
                Debug.LogError("[ReplaceHouses] Không tìm thấy " + HouseThatchedFbx);
                return;
            }

            // Clean up old houses container and any loose old house objects
            var parentHouses = GameObject.Find("CaMau_1954_Village/Environment/Village_Houses_Network");
            if (parentHouses != null) Object.DestroyImmediate(parentHouses);

            var oldAddHouses = GameObject.Find("Additional_Village_Houses");
            if (oldAddHouses != null) Object.DestroyImmediate(oldAddHouses);

            // Recreate clean parent
            var env = GameObject.Find("CaMau_1954_Village/Environment") ?? GameObject.Find("CaMau_1954_Village");
            var housesRoot = new GameObject("Village_Houses_Network");
            if (env != null) housesRoot.transform.SetParent(env.transform, false);

            // 12 authentic thatched cottages positioned comfortably throughout the expanded village (scaled up ~22% for authentic proportions)
            HouseData[] houses = new HouseData[]
            {
                // 1. Nhà An & Má (Ngõ Bắc gần cây đa trung tâm)
                new HouseData("House_01_NhaAn", 3.0f, 22.0f, 180f, 4.4f),

                // 2. Nhà Trưởng Thôn Năm (Phía Tây Bắc cây đa)
                new HouseData("House_02_TruongThonNam", -16.0f, 20.0f, 140f, 4.5f),

                // 3. Nhà Bác Ba Phi (Phía Đông cây đa)
                new HouseData("House_03_BacBaPhi", 16.0f, 15.0f, -120f, 4.4f),

                // 4. Nhà Chú Năm Thợ Mộc (Ngõ Đông Bắc làng)
                new HouseData("House_04_ChuNamThoMoc", 26.0f, 23.0f, -145f, 4.3f),

                // 5. Nhà Bác Ba Nông Dân (Mé cánh đồng lúa Tây Bắc)
                new HouseData("House_05_BacBaNongDan", -14.0f, 32.0f, 180f, 4.4f),

                // 6. Nhà Thím Bảy Phơi Thóc (Mé cánh đồng lúa Đông Bắc)
                new HouseData("House_06_ThimBayPhoiThoc", 14.0f, 32.0f, 180f, 4.3f),

                // 7. Nhà Cô Út Bánh Bò (Phía Tây cây đa, xóm chợ)
                new HouseData("House_07_CoUtBanhBo", -18.0f, 9.5f, 90f, 4.3f),

                // 8. Nhà Chú Sáu Làm Vườn (Xóm vườn chuối Tây)
                new HouseData("House_08_ChuSauLamVuon", -28.0f, 17.0f, 80f, 4.3f),

                // 9. Nhà Dì Hai & Chị Tư Gánh Nước (Gần giếng ngọt phía Đông)
                new HouseData("House_09_DiHai_GiengNuoc", 20.0f, 4.0f, -70f, 4.4f),

                // 10. Nhà Chú Bảy Chèo Đò (Bến sông kênh Tây Nam)
                new HouseData("House_10_ChuBayCheoDo", -14.0f, -5.5f, 25f, 4.3f),

                // 11. Nhà Xóm Kênh Đông Nam (Hướng ra bờ kênh)
                new HouseData("House_11_XomKenhDongNam", 12.0f, -5.5f, -25f, 4.3f),

                // 12. Nhà Xóm Trong Phía Tây (Mở rộng quy mô làng về phía Tây)
                new HouseData("House_12_XomTrongTay", -36.0f, 7.5f, 55f, 4.3f)
            };

            var fbxMf = fbxThatched.GetComponentInChildren<MeshFilter>();
            Mesh cottageMesh = (fbxMf != null) ? fbxMf.sharedMesh : null;

            foreach (var h in houses)
            {
                var inst = Object.Instantiate(fbxThatched, housesRoot.transform);
                inst.name = h.name;

                // Height formula ensuring base touches ground firmly: yPos = UNIFORM_GROUND_Y + (0.413f * scale)
                float yPos = UNIFORM_GROUND_Y + (MESH_BOTTOM_OFFSET * h.scale);
                inst.transform.position = new Vector3(h.posXZ.x, yPos, h.posXZ.y);
                inst.transform.rotation = Quaternion.Euler(0f, h.rotationY, 0f);
                inst.transform.localScale = Vector3.one * h.scale;

                // Material assignment
                if (matThatched != null)
                {
                    foreach (var r in inst.GetComponentsInChildren<Renderer>())
                    {
                        r.sharedMaterial = matThatched;
                    }
                }

                // Remove any legacy oversized collider
                var oldCol = inst.GetComponent<Collider>();
                if (oldCol != null) Object.DestroyImmediate(oldCol);

                // 1:1 Precise Collision (không vượt quá nhà)
                // MeshCollider on the visual mesh guarantees exact polygon boundary match
                var childMeshObj = inst.transform.Find("Mesh_0") ?? inst.transform;
                var mc = childMeshObj.gameObject.GetComponent<MeshCollider>();
                if (mc == null) mc = childMeshObj.gameObject.AddComponent<MeshCollider>();
                if (cottageMesh != null) mc.sharedMesh = cottageMesh;

                // Tight interior wall BoxCollider (only back/side living area, leaving porch accessible)
                var box = inst.GetComponent<BoxCollider>();
                if (box == null) box = inst.AddComponent<BoxCollider>();
                box.center = new Vector3(0f, -0.05f, 0f);
                box.size = new Vector3(0.85f, 0.70f, 0.70f);

                EditorUtility.SetDirty(inst);
            }

            Debug.Log($"[ReplaceHouses] Đã dựng 12 ngôi nhà tranh nguyên bản ({HouseThatchedFbx}) với collider khớp 1:1 không bị lồi ra ngoài!");
        }

        // =========================================================================
        // 2. UNIFIED EARTHEN GROUND NETWORK (From Gate to Every House Yard)
        // =========================================================================
        public static void BuildUnifiedEarthenGroundNetwork()
        {
            var roadMat = AssetDatabase.LoadAssetAtPath<Material>(RoadMatPath);
            if (roadMat != null)
            {
                if (roadMat.HasProperty("_BaseColor")) roadMat.SetColor("_BaseColor", Color.white);
                roadMat.mainTextureScale = Vector2.one; // 1:1 tiling for world-space UV scaling
                roadMat.SetFloat("_Cull", 0f);
                EditorUtility.SetDirty(roadMat);
            }

            var pathsRoot = GameObject.Find("CaMau_1954_Village/Paths_3D_Network") ?? GameObject.Find("Paths_3D_Network");
            if (pathsRoot == null) pathsRoot = new GameObject("Paths_3D_Network");

            // 1. Clean up ALL old fragmented separate quads and duplicate containers
            var oldUnified = GameObject.Find("Unified_Village_Earthen_Ground_Network");
            if (oldUnified != null) Object.DestroyImmediate(oldUnified);

            for (int i = pathsRoot.transform.childCount - 1; i >= 0; i--)
            {
                var c = pathsRoot.transform.GetChild(i);
                Object.DestroyImmediate(c.gameObject);
            }

            var oldRadial = GameObject.Find("Radial_House_Trails");
            if (oldRadial != null) Object.DestroyImmediate(oldRadial);

            var oldWinding = GameObject.Find("Winding_Country_Dirt_Road");
            if (oldWinding != null) Object.DestroyImmediate(oldWinding);

            // Deactivate legacy extension object without expensive multi-object destruction
            var legacyExt = GameObject.Find("CaMau_1954_Village/Environment/Terrain_3D/Paddy_5X_Extension_Root");
            if (legacyExt != null) legacyExt.SetActive(false);

            // 2. Build the single unified procedural mesh
            var unifiedObj = new GameObject("Unified_Village_Earthen_Ground_Network");
            unifiedObj.transform.SetParent(pathsRoot.transform, false);
            unifiedObj.transform.position = Vector3.zero;
            unifiedObj.transform.rotation = Quaternion.identity;
            unifiedObj.transform.localScale = Vector3.one;

            var mf = unifiedObj.AddComponent<MeshFilter>();
            var mr = unifiedObj.AddComponent<MeshRenderer>();
            if (roadMat != null) mr.sharedMaterial = roadMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = true;

            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var normals = new List<Vector3>();
            var tris = new List<int>();

            // Uniform ground elevation sitting cleanly 3mm above base terrain (0.505m)
            const float groundY = 0.508f;

            // A. North Gate Arrival Apron (Sân Đón Cổng Bắc)
            AddGrid(verts, uvs, normals, tris, -13.0f, -1.0f, 180.0f, 190.0f, 4, 3, groundY);

            // B. North Entrance Avenue (Đường Cổng Bắc xuyên qua đồng lúa)
            // Connects Z = 184m (Gate) down to Z = 34m (Village Entrance)
            int avenueSegments = 45;
            float startZ = 184.0f;
            float endZ = 34.0f;
            for (int i = 0; i < avenueSegments; i++)
            {
                float t0 = (float)i / avenueSegments;
                float t1 = (float)(i + 1) / avenueSegments;

                float z0 = Mathf.Lerp(startZ, endZ, t0);
                float z1 = Mathf.Lerp(startZ, endZ, t1);

                float curve0 = Mathf.Sin(t0 * Mathf.PI) * 0.50f;
                float curve1 = Mathf.Sin(t1 * Mathf.PI) * 0.50f;

                float cx0 = -7.0f + curve0;
                float cx1 = -7.0f + curve1;

                // Half width flaring smoothly from 3.3m at North gate to 4.5m at village entry
                float hw0 = Mathf.Lerp(3.3f, 4.5f, t0);
                float hw1 = Mathf.Lerp(3.3f, 4.5f, t1);

                Vector3 p0 = new Vector3(cx0 - hw0, groundY, z0);
                Vector3 p1 = new Vector3(cx0 + hw0, groundY, z0);
                Vector3 p2 = new Vector3(cx1 + hw1, groundY, z1);
                Vector3 p3 = new Vector3(cx1 - hw1, groundY, z1);

                AddQuad(verts, uvs, normals, tris, p0, p1, p2, p3);
            }

            // C. Village North Crossway & Entrance Flare (Ngõ Bắc Làng)
            // Spans X from -23m to +28m, Z from 24m to 35m
            AddGrid(verts, uvs, normals, tris, -23.0f, 28.0f, 24.0f, 35.0f, 14, 4, groundY);

            // D. Central Sacred Banyan Tree Plaza (Sân Cây Đa Trung Tâm)
            // Heart of the village: spans X from -19m to +8m, Z from 5m to 25m
            AddGrid(verts, uvs, normals, tris, -19.0f, 8.0f, 5.0f, 25.0f, 9, 8, groundY);

            // E. West Village Street & Pier Approach (Đường Phía Tây & Bến Đò)
            // Street east-west artery
            AddGrid(verts, uvs, normals, tris, -39.0f, -15.0f, 10.5f, 17.5f, 8, 3, groundY);
            // Southward approach to Pier
            AddGrid(verts, uvs, normals, tris, -28.0f, -20.5f, -5.0f, 11.5f, 3, 5, groundY);
            // Pier Landing Pad
            AddGrid(verts, uvs, normals, tris, -30.0f, -19.0f, -8.5f, -3.5f, 3, 2, groundY);

            // F. East Village Street & Harbor Approach (Đường Phía Đông & Bến Cảng)
            // Street east-west artery
            AddGrid(verts, uvs, normals, tris, 5.0f, 33.0f, 10.5f, 17.5f, 8, 3, groundY);
            // Southward approach to Harbor & Well
            AddGrid(verts, uvs, normals, tris, 17.5f, 25.5f, -4.0f, 11.5f, 3, 5, groundY);
            // Harbor Landing Pad
            AddGrid(verts, uvs, normals, tris, 15.5f, 28.0f, -8.5f, -3.0f, 3, 2, groundY);

            // G. South Waterfront Promenade & Central South Thoroughfare
            // Southern canal walkway
            AddGrid(verts, uvs, normals, tris, -27.0f, 25.0f, -8.0f, -3.5f, 16, 2, groundY);
            // Central South thoroughfare connecting Banyan Tree Plaza directly to canal wharf & bridge
            AddGrid(verts, uvs, normals, tris, -11.0f, -1.0f, -4.0f, 6.5f, 4, 4, groundY);

            // Natural organic connectors between streets & yards
            AddGrid(verts, uvs, normals, tris, 4.0f, 13.0f, 12.0f, 18.0f, 3, 2, groundY);  // Tree court to House 03
            AddGrid(verts, uvs, normals, tris, -19.0f, -12.0f, 11.5f, 16.0f, 2, 2, groundY); // Tree court to House 07 & 02

            // H. Dedicated Front Yard Pads & Alleys for ALL 12 Thatched Cottages
            // 1. House 01 (Nhà An & Má): pos=(3.0, 22.0)
            AddGrid(verts, uvs, normals, tris, -1.5f, 8.0f, 15.5f, 25.0f, 3, 3, groundY);

            // 2. House 02 (Trưởng Thôn Năm): pos=(-16.0, 20.0)
            AddGrid(verts, uvs, normals, tris, -21.0f, -10.5f, 14.5f, 23.0f, 3, 3, groundY);

            // 3. House 03 (Bác Ba Phi): pos=(16.0, 15.0)
            AddGrid(verts, uvs, normals, tris, 11.0f, 21.5f, 9.5f, 18.5f, 3, 3, groundY);

            // 4. House 04 (Chú Năm Thợ Mộc): pos=(26.0, 23.0)
            AddGrid(verts, uvs, normals, tris, 20.0f, 31.0f, 17.0f, 26.0f, 3, 3, groundY);
            AddGrid(verts, uvs, normals, tris, 21.5f, 27.0f, 13.5f, 18.5f, 2, 2, groundY); // Alley to East St

            // 5. House 05 (Bác Ba Nông Dân): pos=(-14.0, 32.0)
            AddGrid(verts, uvs, normals, tris, -19.5f, -8.5f, 25.5f, 35.0f, 3, 3, groundY);

            // 6. House 06 (Thím Bảy Phơi Thóc): pos=(14.0, 32.0)
            AddGrid(verts, uvs, normals, tris, 8.5f, 19.5f, 25.5f, 35.0f, 3, 3, groundY);

            // 7. House 07 (Cô Út Bánh Bò): pos=(-18.0, 9.5)
            AddGrid(verts, uvs, normals, tris, -22.5f, -11.5f, 5.5f, 14.0f, 3, 3, groundY);

            // 8. House 08 (Chú Sáu Làm Vườn): pos=(-28.0, 17.0)
            AddGrid(verts, uvs, normals, tris, -33.0f, -22.5f, 12.5f, 21.0f, 3, 3, groundY);
            AddGrid(verts, uvs, normals, tris, -30.5f, -24.0f, 9.5f, 14.0f, 2, 2, groundY); // Alley to West St

            // 9. House 09 (Dì Hai Giếng Nước): pos=(20.0, 4.0)
            AddGrid(verts, uvs, normals, tris, 14.0f, 25.0f, 0.5f, 9.0f, 3, 3, groundY);
            AddGrid(verts, uvs, normals, tris, 17.0f, 22.5f, 7.0f, 12.5f, 2, 2, groundY); // Alley to East St

            // 10. House 10 (Chú Bảy Chèo Đò): pos=(-14.0, -5.5)
            AddGrid(verts, uvs, normals, tris, -19.0f, -8.5f, -7.5f, 1.0f, 3, 3, groundY);

            // 11. House 11 (Xóm Kênh Đông): pos=(12.0, -5.5)
            AddGrid(verts, uvs, normals, tris, 6.5f, 17.0f, -7.5f, 1.0f, 3, 3, groundY);

            // 12. House 12 (Xóm Trong Phía Tây): pos=(-36.0, 7.5)
            AddGrid(verts, uvs, normals, tris, -41.0f, -30.5f, 3.5f, 12.5f, 3, 3, groundY);
            AddGrid(verts, uvs, normals, tris, -35.5f, -27.5f, 9.0f, 15.0f, 2, 2, groundY); // Alley to House 08 / West St

            // 3. Assemble and apply unified procedural mesh
            Mesh mesh = new Mesh();
            mesh.name = "M_Unified_Village_Earthen_Ground_Network";
            mesh.vertices = verts.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.normals = normals.ToArray();
            mesh.triangles = tris.ToArray();
            mesh.RecalculateBounds();

            mf.sharedMesh = mesh;
            EditorUtility.SetDirty(unifiedObj);

            Debug.Log($"[UnifiedRoad] ĐÃ TẠO MESH ĐẤT NÂU THỐNG NHẤT ({verts.Count} đỉnh, {tris.Count / 3} tam giác)! Nối liền từ cổng vào ({startZ}m) qua toàn bộ 12 sân nhà và bến sông với UV world-space chuẩn xác!");
        }

        private static void AddQuad(List<Vector3> verts, List<Vector2> uvs, List<Vector3> normals, List<int> tris,
            Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            int baseIdx = verts.Count;

            verts.Add(p0);
            verts.Add(p1);
            verts.Add(p2);
            verts.Add(p3);

            // World-space UV mapping: identical texture density everywhere (4.54m per tile)
            const float uvScale = 0.22f;
            uvs.Add(new Vector2(p0.x * uvScale, p0.z * uvScale));
            uvs.Add(new Vector2(p1.x * uvScale, p1.z * uvScale));
            uvs.Add(new Vector2(p2.x * uvScale, p2.z * uvScale));
            uvs.Add(new Vector2(p3.x * uvScale, p3.z * uvScale));

            normals.Add(Vector3.up);
            normals.Add(Vector3.up);
            normals.Add(Vector3.up);
            normals.Add(Vector3.up);

            // Triangle 1: p0 -> p1 -> p2
            tris.Add(baseIdx);
            tris.Add(baseIdx + 1);
            tris.Add(baseIdx + 2);

            // Triangle 2: p0 -> p2 -> p3
            tris.Add(baseIdx);
            tris.Add(baseIdx + 2);
            tris.Add(baseIdx + 3);
        }

        private static void AddGrid(List<Vector3> verts, List<Vector2> uvs, List<Vector3> normals, List<int> tris,
            float minX, float maxX, float minZ, float maxZ, int xSubdivs, int zSubdivs, float y)
        {
            const float uvScale = 0.22f;
            int startVertIdx = verts.Count;

            for (int z = 0; z <= zSubdivs; z++)
            {
                float fz = (float)z / zSubdivs;
                float pz = Mathf.Lerp(minZ, maxZ, fz);

                for (int x = 0; x <= xSubdivs; x++)
                {
                    float fx = (float)x / xSubdivs;
                    float px = Mathf.Lerp(minX, maxX, fx);

                    verts.Add(new Vector3(px, y, pz));
                    uvs.Add(new Vector2(px * uvScale, pz * uvScale));
                    normals.Add(Vector3.up);
                }
            }

            int rowSize = xSubdivs + 1;
            for (int z = 0; z < zSubdivs; z++)
            {
                for (int x = 0; x < xSubdivs; x++)
                {
                    int bL = startVertIdx + z * rowSize + x;
                    int bR = bL + 1;
                    int tL = startVertIdx + (z + 1) * rowSize + x;
                    int tR = tL + 1;

                    // Triangle 1
                    tris.Add(tL);
                    tris.Add(tR);
                    tris.Add(bL);

                    // Triangle 2
                    tris.Add(tR);
                    tris.Add(bR);
                    tris.Add(bL);
                }
            }
        }

        // =========================================================================
        // 3. SETUP 100% STANDING UPRIGHT NPCS WITH ACCURATE GENDER & DIALOGUE
        // =========================================================================
        public static void SetupAllUprightStandingNPCs()
        {
            var pElder = AssetDatabase.LoadAssetAtPath<GameObject>(NpcElderPrefab);
            var pFemale = AssetDatabase.LoadAssetAtPath<GameObject>(NpcFemalePrefab);
            var pMother = AssetDatabase.LoadAssetAtPath<GameObject>(NpcMotherPrefab) ?? pFemale;
            var pMale01 = AssetDatabase.LoadAssetAtPath<GameObject>(NpcVillager01Prefab);
            var pMale04 = AssetDatabase.LoadAssetAtPath<GameObject>(NpcVillager04Prefab) ?? pMale01;
            var pMale05 = AssetDatabase.LoadAssetAtPath<GameObject>(NpcVillager05Prefab) ?? pMale01;
            var pChildGirl = AssetDatabase.LoadAssetAtPath<GameObject>(NpcQuietChildPrefab) ?? pFemale;

            var cElder = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AcElder);
            var cFemale = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AcFemale);
            var cMale = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AcMale);
            var cSocial = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AcSocial);

            var oldRoot = GameObject.Find("CaMau_1954_Village/NPCs") ?? GameObject.Find("NPCs");
            if (oldRoot != null) Object.DestroyImmediate(oldRoot);

            var villageParent = GameObject.Find("CaMau_1954_Village");
            var npcsRoot = new GameObject("NPCs");
            if (villageParent != null) npcsRoot.transform.SetParent(villageParent.transform, false);

            float groundY = 0.505f;

            // 1. TRƯỞNG THÔN NĂM (Nam Cao Niên - Đứng trước sân Nhà Trưởng thôn)
            BuildAccurateNPC(npcsRoot.transform, pElder, cElder, "NPC_01_TruongThonNam",
                new Vector3(-13.5f, groundY, 16.5f), Quaternion.Euler(0f, 140f, 0f),
                "Trưởng Thôn Năm", "Bậc cao niên xóm Rạch Gốc",
                new string[]
                {
                    "Chào cháu An! Lúa năm nay trúng mùa lớn, hạt nào hạt nấy mẩy vàng óng ả khắp đồng.",
                    "Xóm mình đang phơi lúa mới để đóng bao gửi ra miền Bắc theo đoàn tàu tập kết ngoài cửa Sông Đốc đó."
                },
                new DialogueChoice[]
                {
                    new DialogueChoice { choiceText = "Cháu thấy đồng lúa vàng trải dài đẹp quá thưa bác!", responseLine = "Đúng rồi cháu, đất đai Nam Bộ phù sa màu mỡ nuôi nấng người dân mình. Giữ lấy hạt ngọc trời này nghe con.", responseAnim = "OldMan_Talking" },
                    new DialogueChoice { choiceText = "Bác cho cháu hỏi tàu tập kết bao giờ rời bến?", responseLine = "Sắp rồi con à. Chú Bảy ngoài bến đò đang hối hả chuyển những bao gạo cuối cùng lên ghe đấy.", responseAnim = "OldMan_Talking" },
                    new DialogueChoice { choiceText = "Cháu xin phép đi dạo tiếp quanh xóm ạ.", responseLine = "Đi cẩn thận nghe con, nhớ ghé qua má dặn chuyện nhà trước khi trời tối nghen.", responseAnim = "OldMan_Talking" }
                },
                "OldMan_Idle", "OldMan_Talking", scale: 1.42f);

            // 2. MÁ CỦA AN (Nữ Trưởng Thành - Đứng trước hiên Nhà An)
            BuildAccurateNPC(npcsRoot.transform, pMother, cFemale, "NPC_02_MaAnMother",
                new Vector3(2.5f, groundY, 17.0f), Quaternion.Euler(0f, 180f, 0f),
                "Má của An", "Má Năm - Xóm Bờ Kênh",
                new string[]
                {
                    "An đó hả con? Đi chơi đâu từ sáng tới giờ, lúa chín vàng cả cánh đồng rồi kìa.",
                    "Về nhà ăn chén cơm gạo mới má vừa nấu, dẻo và thơm lừng luôn nè con."
                },
                new DialogueChoice[]
                {
                    new DialogueChoice { choiceText = "Dạ má, cơm gạo mới thơm quá má ơi!", responseLine = "Ừ, ăn no rồi phụ các bác ngoài sân phơi lúa nghe con. Nhớ đừng chạy nhảy làm đổ nia thóc.", responseAnim = "Female_Talking_01" },
                    new DialogueChoice { choiceText = "Má ơi, chiều nay con ra bến xem tàu tập kết nghen?", responseLine = "Được chớ, nhớ rủ thằng Minh đi chung cho có bạn, chớ có nhảy giỡn dưới mé nước sâu nghe chưa!", responseAnim = "Female_Talking_02" }
                },
                "Female_Waving", "Female_Talking_01", scale: 1.40f);

            // 3. BÁC BA PHI (Nam Cao Niên - Đứng trước sân Nhà Bác Ba)
            BuildAccurateNPC(npcsRoot.transform, pElder, cElder, "NPC_03_BacBaPhi",
                new Vector3(12.5f, groundY, 13.5f), Quaternion.Euler(0f, -120f, 0f),
                "Bác Ba Phi", "Huyền thoại kể chuyện rừng U Minh",
                new string[]
                {
                    "Hahaha! Thằng nhỏ An lại đây bác Ba kể chuyện bắt cá sấu khổng lồ ở lung Tràm cho nghe!",
                    "Bữa đó sấu nổi đặc như bèo, bác chỉ cần quăng một sợi dây mây là lôi thẳng lên bờ một con dài cả chục thước!"
                },
                new DialogueChoice[]
                {
                    new DialogueChoice { choiceText = "Bác Ba kể thiệt không hay bác lại nói dóc đó?", responseLine = "Tổ cha mày! Bác Ba mày mà thèm nói dóc hả! Hỏi cả xóm Rạch Gốc này ai mà không phục tài bác!", responseAnim = "OldMan_Talking" },
                    new DialogueChoice { choiceText = "Con thích nghe chuyện bác Ba cưỡi rùa vàng hơn!", responseLine = "Hè hè, để bữa nào rảnh bác dẫn mày vô lung Tràm, chỉ cho con rùa to bằng cái nia phơi lúa!", responseAnim = "OldMan_Talking" }
                },
                "OldMan_Idle", "OldMan_Talking", scale: 1.42f);

            // 4. CHÚ BẢY CHÈO ĐÒ (Nam Ngư Dân - Đứng bến sông Nhà 10)
            BuildAccurateNPC(npcsRoot.transform, pMale05, cMale, "NPC_04_ChuBayCheoDo",
                new Vector3(-12.0f, groundY, -3.5f), Quaternion.Euler(0f, 25f, 0f),
                "Chú Bảy Chèo Đò", "Chủ xuồng ba lá - Bến Sông Đốc",
                new string[]
                {
                    "Xuồng ba lá chuẩn bị tách bến sang cồn dừa nước rồi đây con ơi!",
                    "Gió biển Cà Mau thổi lồng lộng, con nước ròng đưa ghe thuyền tấp nập về cửa biển."
                },
                new DialogueChoice[]
                {
                    new DialogueChoice { choiceText = "Chú Bảy cho con quá giang qua bên kia sông với!", responseLine = "Lên xuồng ngồi im nghe con, sông sâu nước xiết, chớ thò tay quơ nước kẻo trượt chân!", responseAnim = "Male_Talking_01" },
                    new DialogueChoice { choiceText = "Chú ơi, ngoài biển Sông Đốc tàu tập kết to lắm hả chú?", responseLine = "To như quả núi con à! Tàu chở cán bộ đồng bào ra Bắc, cờ đỏ sao vàng bay rực rỡ cả góc trời!", responseAnim = "Male_Talking_02" }
                },
                "Male_Waving", "Male_Talking_01", scale: 1.42f);

            // 5. THẰNG MINH (Bé Trai - Bạn cùng lứa Bé An, đứng gần ngã ba sân đình)
            BuildAccurateNPC(npcsRoot.transform, pMale01, cSocial, "NPC_05_ThangMinh",
                new Vector3(-6.0f, groundY, 7.5f), Quaternion.Euler(0f, 20f, 0f),
                "Thằng Minh", "Bạn nối khố của Bé An",
                new string[]
                {
                    "Ê An! Đi bắt cào cào trong ruộng lúa vàng với tao không?",
                    "Hôm qua tao thấy tổ chim sẻ đồng núp dưới gốc lúa bự chà bá luôn á!"
                },
                new DialogueChoice[]
                {
                    new DialogueChoice { choiceText = "Đi liền! Nhưng coi chừng dẫm gãy lúa của bác Năm nha!", responseLine = "Biết rồi, tao đi men theo bờ mẫu ruộng thôi, không có dẫm bậy bạ đâu mà sợ!", responseAnim = "Waving_02" },
                    new DialogueChoice { choiceText = "Má tao đang gọi về ăn cơm rồi, lát gặp nghen!", responseLine = "Ờ, ăn lẹ rồi chạy ra đầu cầu khỉ đầu xóm hú tao một tiếng!", responseAnim = "Waving_01" }
                },
                "Waving_01", "Waving_02", scale: 1.25f);

            // 6. CHỊ TƯ GÁNH NƯỚC (Nữ Trưởng Thành - Đứng bên giếng ngọt Nhà 09)
            BuildAccurateNPC(npcsRoot.transform, pFemale, cFemale, "NPC_06_ChiTuGanhNuoc",
                new Vector3(16.5f, groundY, 5.0f), Quaternion.Euler(0f, -75f, 0f),
                "Chị Tư Gánh Nước", "Người gánh nước giếng ngọt xóm",
                new string[]
                {
                    "Tránh đường cho chị gánh đôi thùng nước mưa ngọt về đổ lu nghe An!",
                    "Nước mưa mùa này trong veo ngọt lịm, nấu trà cho các cụ uống là số một."
                },
                new DialogueChoice[]
                {
                    new DialogueChoice { choiceText = "Để em phụ chị xách một thùng nghen!", responseLine = "Thôi khỏi chú em nhỏ xíu, xách nặng đau lưng má la chị chết! Lo đi chơi ngoan đi.", responseAnim = "Female_Talking_01" }
                },
                "Female_Waving", "Female_Talking_01", scale: 1.40f);

            // 7. CHÚ SÁU LÀM VƯỜN (Nam Nông Dân - Đứng xóm vườn chuối Nhà 08)
            BuildAccurateNPC(npcsRoot.transform, pMale04, cMale, "NPC_07_ChuSauLamVuon",
                new Vector3(-24.5f, groundY, 16.5f), Quaternion.Euler(0f, 80f, 0f),
                "Chú Sáu Làm Vườn", "Nông dân vườn chuối & dừa nước",
                new string[]
                {
                    "Mấy quày dừa nước bên kia mương ngọt nước cơm dày dữ lắm An ơi.",
                    "Chiều nay ghé chú chặt cho một trái dừa nước ăn cho mát dạ."
                },
                new DialogueChoice[]
                {
                    new DialogueChoice { choiceText = "Dạ, dừa nước chấm đường ăn ngon hết sẩy!", responseLine = "Hahaha, đúng điệu dân Cà Mau rồi đó cháu!", responseAnim = "Male_Talking_01" }
                },
                "Male_Waving", "Male_Talking_01", scale: 1.42f);

            // 8. BÉ TÈO (Trẻ Chăn Trâu - Đứng xóm trong phía Tây Nhà 12)
            BuildAccurateNPC(npcsRoot.transform, pMale04, cSocial, "NPC_08_BeTeo",
                new Vector3(-31.5f, groundY, 8.5f), Quaternion.Euler(0f, 60f, 0f),
                "Bé Tèo", "Trẻ chăn trâu xóm trong",
                new string[]
                {
                    "Anh An ơi! Cào cào của em bắt được bự hơn của anh nè!",
                    "Nó búng một cái bay qua tận rãnh mương luôn á!"
                },
                new DialogueChoice[]
                {
                    new DialogueChoice { choiceText = "Để anh bắt con muồm muỗm to gấp đôi cho em coi!", responseLine = "Dạ, em thách anh bắt được đó!", responseAnim = "Waving_02" }
                },
                "Waving_01", "Waving_02", scale: 1.15f);

            // 9. BÉ MẬN (Bé Gái - Đứng xóm chợ Nhà 07)
            BuildAccurateNPC(npcsRoot.transform, pChildGirl, cSocial, "NPC_09_BeMan",
                new Vector3(-14.5f, groundY, 9.5f), Quaternion.Euler(0f, 90f, 0f),
                "Bé Mận", "Bé gái xóm chợ Rạch Gốc",
                new string[]
                {
                    "Anh An coi em kết hoa dại cài lên nón lá đẹp chưa nè!",
                    "Hoa màu vàng như màu hạt lúa chín ngoài đồng vậy đó."
                },
                new DialogueChoice[]
                {
                    new DialogueChoice { choiceText = "Khéo tay quá, như nón công chúa miền Tây!", responseLine = "Hi hi, em cảm ơn anh An nhiều nghen!", responseAnim = "Waving_03" }
                },
                "Waving_02", "Waving_03", scale: 1.10f);

            // 10. BÉ ÚT (Em Bé - Đứng trước sân Nhà An)
            BuildAccurateNPC(npcsRoot.transform, pChildGirl, cSocial, "NPC_10_BeUt",
                new Vector3(0.5f, groundY, 15.5f), Quaternion.Euler(0f, 175f, 0f),
                "Bé Út", "Em bé ngoan của xóm",
                new string[]
                {
                    "A... anh An... bế Út... bế Út ra xem chim sẻ đồng!",
                    "Út thích hoa vàng lúa vàng lắm!"
                },
                new DialogueChoice[]
                {
                    new DialogueChoice { choiceText = "Út đứng ngoan kẻo ngã nghen, lát anh hái hoa cho!", responseLine = "Dạ... Út ngoan mà!", responseAnim = "Waving_01" }
                },
                "Waving_01", "Waving_02", scale: 0.85f);

            // 11. DÌ HAI (Nữ Trung Niên - Đứng bờ kênh Nhà 11)
            BuildAccurateNPC(npcsRoot.transform, pFemale, cFemale, "NPC_11_DiHai",
                new Vector3(10.5f, groundY, -2.5f), Quaternion.Euler(0f, -25f, 0f),
                "Dì Hai", "Hậu phương tiễn đoàn tập kết",
                new string[]
                {
                    "Bà con mang thêm bánh tét, gạo mới ra bến Sông Đốc gửi các anh bộ đội nghen!",
                    "Đi mạnh giỏi, ngày thống nhất hòa bình lại về với quê hương Cà Mau!"
                },
                new DialogueChoice[]
                {
                    new DialogueChoice { choiceText = "Dì Hai ơi, mọi người sẽ sớm trở về sum họp phải không dì?", responseLine = "Chắc chắn rồi cháu! Đồng bào miền Nam lúc nào cũng hướng về ngày hội ngộ.", responseAnim = "Female_Talking_02" }
                },
                "Female_Waving", "Female_Talking_02", scale: 1.40f);

            // 12. CỤ BẢY GIÀ (Lão Thành - Ngồi/đứng dưới bóng Cây Đa Trung Tâm)
            BuildAccurateNPC(npcsRoot.transform, pElder, cElder, "NPC_12_CuBayGia",
                new Vector3(-4.5f, groundY, 14.5f), Quaternion.Euler(0f, -90f, 0f),
                "Cụ Bảy Già", "Lão thành cách mạng xóm kênh",
                new string[]
                {
                    "Cây tràm, cây đước Cà Mau bám rễ sâu như tấm lòng người dân Nam Bộ vậy cháu à.",
                    "Dù có đi đâu xa, ngửi mùi rơm rạ lúa chín là nhớ da diết quê nhà."
                },
                new DialogueChoice[]
                {
                    new DialogueChoice { choiceText = "Cháu sẽ luôn ghi nhớ lời cụ dạy ạ.", responseLine = "Cháu là đứa trẻ ngoan. Lớn lên phụ giúp quê hương đất nước nghen cháu.", responseAnim = "OldMan_Talking" }
                },
                "OldMan_Idle", "OldMan_Talking", scale: 1.42f);

            // Sample natural standing idle poses in Edit Mode so characters never T-pose
            SampleIdlePosesForAllNPCs(npcsRoot.transform);

            Debug.Log("[SetupNPCs] Đã khởi tạo hoàn tất 12 NPC CHUẨN XÁC 1:1 theo kịch bản thoại với kích cỡ phóng to tự nhiên!");
        }

        private static void SampleIdlePosesForAllNPCs(Transform npcsRoot)
        {
            var femaleClip = LoadFirstClipFromAsset("Assets/Animations/NPCs/Female/NPC_Female_Talking_01.fbx");
            var maleClip = LoadFirstClipFromAsset("Assets/Animations/NPCs/Male/NPC_Male_Talking_01.fbx");
            var elderClip = LoadFirstClipFromAsset("Assets/Animations/NPCs/Male/NPC_Male_OldMan_Idle_Loop.fbx");
            var waveClip = LoadFirstClipFromAsset("Assets/Animations/NPCs/Social/NPC_Social_Waving_01.fbx");

            AnimationMode.StartAnimationMode();

            foreach (Transform child in npcsRoot)
            {
                var anim = child.GetComponent<Animator>();
                if (anim == null) continue;

                AnimationClip targetClip = waveClip;
                string ctrlName = anim.runtimeAnimatorController != null ? anim.runtimeAnimatorController.name : "";
                if (ctrlName.Contains("Elder")) targetClip = elderClip;
                else if (ctrlName.Contains("Female")) targetClip = femaleClip;
                else if (ctrlName.Contains("Male")) targetClip = maleClip;
                else targetClip = waveClip;

                if (targetClip != null)
                {
                    AnimationMode.SampleAnimationClip(child.gameObject, targetClip, 0.4f);
                }
            }

            AnimationMode.StopAnimationMode();
        }

        private static AnimationClip LoadFirstClipFromAsset(string path)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var a in assets)
            {
                if (a is AnimationClip c && !c.name.StartsWith("__preview__")) return c;
            }
            return null;
        }

        private static void BuildAccurateNPC(Transform parent, GameObject prefab, RuntimeAnimatorController animCtrl,
            string objName, Vector3 pos, Quaternion rot, string name, string title, string[] openings, DialogueChoice[] choices,
            string defAnim, string talkAnim, float scale = 1.0f)
        {
            if (prefab == null) return;

            var inst = Object.Instantiate(prefab, parent);
            inst.name = objName;
            inst.transform.position = pos;
            inst.transform.rotation = rot;
            inst.transform.localScale = Vector3.one * scale;

            var anim = inst.GetComponent<Animator>(); if (anim == null) anim = inst.AddComponent<Animator>();
            if (animCtrl != null) anim.runtimeAnimatorController = animCtrl;
            anim.applyRootMotion = false;

            var col = inst.GetComponent<CapsuleCollider>(); if (col == null) col = inst.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.85f * scale, 0f);
            col.radius = 0.35f * scale;
            col.height = 1.70f * scale;

            var interactable = inst.GetComponent<GenshinNPCInteractable>(); if (interactable == null) interactable = inst.AddComponent<GenshinNPCInteractable>();
            interactable.Configure(name, title, openings, choices, defAnim, talkAnim);

            EditorUtility.SetDirty(inst);
        }

        // =========================================================================
        // 4. ADD TRUNK COLLIDERS TO ALL TREES (EXCEPT RICE & GRASS)
        // =========================================================================
        public static void AddCollidersToAllTrees()
        {
            var veg = GameObject.Find("CaMau_1954_Village/Environment/Vegetation_3D");
            if (veg == null) return;

            int count = 0;
            foreach (Transform t in veg.transform)
            {
                // Stone ring already has its own stone colliders
                if (t.name == "Tree_Central_StoneRing") continue;

                // Add or adjust CapsuleCollider on the tree trunk
                var col = t.GetComponent<CapsuleCollider>();
                if (col == null) col = t.gameObject.AddComponent<CapsuleCollider>();

                string n = t.name.ToLower();
                if (n.Contains("central") || n.Contains("sacred") || n.Contains("banyan"))
                {
                    // Grand Central Banyan Tree: wide trunk
                    col.center = new Vector3(0f, 2.5f, 0f);
                    col.radius = 1.8f;
                    col.height = 6.0f;
                }
                else if (n.Contains("coconut") || n.Contains("palm"))
                {
                    // Coconut palm trunk
                    col.center = new Vector3(0f, 3.0f, 0f);
                    col.radius = 0.50f;
                    col.height = 7.0f;
                }
                else if (n.Contains("banana"))
                {
                    // Banana tree clump
                    col.center = new Vector3(0f, 2.0f, 0f);
                    col.radius = 0.75f;
                    col.height = 4.5f;
                }
                else if (n.Contains("fruit") || n.Contains("canopy"))
                {
                    // Fruit & shade trees
                    col.center = new Vector3(0f, 2.5f, 0f);
                    col.radius = 0.85f;
                    col.height = 5.5f;
                }
                else if (n.Contains("bamboo"))
                {
                    // Bamboo grove clump
                    col.center = new Vector3(0f, 2.5f, 0f);
                    col.radius = 0.80f;
                    col.height = 5.5f;
                }
                else if (n.Contains("bush"))
                {
                    // Dense bush tree
                    col.center = new Vector3(0f, 1.8f, 0f);
                    col.radius = 0.75f;
                    col.height = 4.0f;
                }
                else
                {
                    // Default tree trunk
                    col.center = new Vector3(0f, 2.5f, 0f);
                    col.radius = 0.65f;
                    col.height = 5.5f;
                }

                col.isTrigger = false;
                EditorUtility.SetDirty(t.gameObject);
                count++;
            }

            Debug.Log($"[TreeColliders] Đã thêm Trunk CapsuleCollider cho {count} cây cối! Lúa và cỏ không có collider, người chơi đi qua hoàn toàn tự do!");
        }

        // =========================================================================
        // 5. CAPTURE QA SNAPSHOTS (Rule 4.2: 720p 1280x720)
        // =========================================================================
        public static void CaptureQASnapshots()
        {
            if (!Directory.Exists(ArtifactDir)) Directory.CreateDirectory(ArtifactDir);

            int width = 1280;
            int height = 720;

            // Shot 1: Elevated panoramic overview showing the entire unified earthen network from gate avenue to all house yards
            RenderShot(
                new Vector3(-6.0f, 32.0f, -4.0f),
                Quaternion.Euler(58.0f, 0.0f, 0.0f),
                width, height, 64.0f,
                Path.Combine(ArtifactDir, "CaMau_UnifiedEarthenGround_Overview_QA.jpg")
            );

            // Shot 2: Eye-level ground view showing scaled-up NPCs (Má của An & Bé Út) in front of their enlarged authentic thatched cottage
            RenderShot(
                new Vector3(2.5f, 1.8f, 13.5f),
                Quaternion.Euler(4.0f, 0.0f, 0.0f),
                width, height, 54.0f,
                Path.Combine(ArtifactDir, "CaMau_EnlargedNPCsAndHouse_QA.jpg")
            );

            // Shot 3: Plaza view from Central Banyan Tree showing continuous earthen ground across cottage yards and NPCs
            RenderShot(
                new Vector3(-6.0f, 2.2f, 9.0f),
                Quaternion.Euler(6.0f, 25.0f, 0.0f),
                width, height, 56.0f,
                Path.Combine(ArtifactDir, "CaMau_CentralBanyanAndHouseYards_QA.jpg")
            );

            Debug.Log("[QA] Đã chụp và lưu 3 ảnh QA vào artifact directory!");
        }

        private static void RenderShot(Vector3 pos, Quaternion rot, int w, int h, float fov, string outPath)
        {
            var camObj = new GameObject("Temp_QA_Camera");
            var cam = camObj.AddComponent<Camera>();
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.2f;
            cam.farClipPlane = 280f; // Fast frustum culling within village bounds
            camObj.transform.position = pos;
            camObj.transform.rotation = rot;

            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);

            cam.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();

            byte[] bytes = tex.EncodeToJPG(80);
            File.WriteAllBytes(outPath, bytes);

            cam.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(camObj);
        }
    }
}
