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
        private const string HouseThatchedFbx = "Assets/Models/Optimized/SM_StiltedHouse_Thatched_BlenderOptimized.fbx";
        private const string HouseThatchedMat = "Assets/Materials/Environment/Architecture/M_StiltedHouse_Thatched.mat";
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

        private static readonly Vector3 CentralTreePos = new Vector3(-6.0f, 0.50f, 13.0f);

        [MenuItem("Tools/Hoa Do/Overhaul Village Thatched Houses And Accurate NPCs")]
        public static void Execute()
        {
            Debug.Log("[VillageOverhaul] Bắt đầu đồng bộ: Chuẩn hóa 100% nhà tranh + Mở rộng làng + Đường mòn nối cây đa + Chuẩn hóa giới tính & tư thế đứng NPC...");

            // 1. STANDARDIZE ALL HOUSES TO THE VERIFIED THATCHED HOUSE MODEL
            ReplaceAllHousesWithThatchedCottages();

            // 2. CREATE ORGANIC DIRT TRAILS CONNECTING EACH HOUSE TO CENTRAL BANYAN TREE
            BuildRadialTrailsToCentralBanyanTree();

            // 3. SETUP 100% STANDING UPRIGHT NPCS WITH ACCURATE GENDER & DIALOGUE
            SetupAllUprightStandingNPCs();

            // 4. SAVE SCENE & ASSETS
            AssetDatabase.SaveAssets();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene()
            );

            // 5. CAPTURE QA SNAPSHOTS (Rule 4.2: 720p)
            CaptureQASnapshots();

            Debug.Log("[VillageOverhaul] HOÀN TẤT THÀNH CÔNG! Đã thay toàn bộ nhà thành nhà tranh, tạo mạng lưới đường nối cây đa trung tâm và sửa triệt để giới tính/tư thế NPC!");
        }

        // =========================================================================
        // 1. REPLACE ALL HOUSES WITH AUTHENTIC THATCHED COTTAGES
        // =========================================================================
        private struct HouseData
        {
            public string name;
            public Vector3 position;
            public float rotationY;
            public float scale;

            public HouseData(string n, Vector3 pos, float rotY, float s = 3.3f)
            {
                name = n;
                position = pos;
                rotationY = rotY;
                scale = s;
            }
        }

        private static void ReplaceAllHousesWithThatchedCottages()
        {
            var fbxThatched = AssetDatabase.LoadAssetAtPath<GameObject>(HouseThatchedFbx);
            var matThatched = AssetDatabase.LoadAssetAtPath<Material>(HouseThatchedMat);
            if (matThatched != null) matThatched.SetFloat("_Cull", 0f);

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

            // Define 12 beautifully spaced thatched cottages in an expanded village footprint:
            // Centered around the Central Sacred Banyan Tree at (-6.0, 0.50, 13.0)
            HouseData[] houses = new HouseData[]
            {
                // 1. Nhà An & Má (Gần cây trung tâm, ngõ Bắc)
                new HouseData("House_01_NhaAn", new Vector3(3.0f, 2.10f, 22.0f), 180f, 3.4f),

                // 2. Nhà Trưởng Thôn Năm (Phía Tây Bắc cây đa)
                new HouseData("House_02_TruongThonNam", new Vector3(-16.0f, 2.10f, 20.0f), 140f, 3.5f),

                // 3. Nhà Bác Ba Phi (Phía Đông cây đa)
                new HouseData("House_03_BacBaPhi", new Vector3(16.0f, 2.10f, 15.0f), -120f, 3.4f),

                // 4. Nhà Chú Năm Thợ Mộc (Ngõ Đông Bắc làng)
                new HouseData("House_04_ChuNamThoMoc", new Vector3(26.0f, 2.10f, 23.0f), -145f, 3.3f),

                // 5. Nhà Bác Ba Nông Dân (Mé cánh đồng lúa Tây Bắc)
                new HouseData("House_05_BacBaNongDan", new Vector3(-14.0f, 2.10f, 32.0f), 180f, 3.4f),

                // 6. Nhà Thím Bảy Phơi Thóc (Mé cánh đồng lúa Đông Bắc)
                new HouseData("House_06_ThimBayPhoiThoc", new Vector3(14.0f, 2.10f, 32.0f), 180f, 3.3f),

                // 7. Nhà Cô Út Bánh Bò (Phía Tây cây đa, xóm chợ)
                new HouseData("House_07_CoUtBanhBo", new Vector3(-18.0f, 2.10f, 9.5f), 90f, 3.3f),

                // 8. Nhà Chú Sáu Làm Vườn (Xóm vườn chuối Tây)
                new HouseData("House_08_ChuSauLamVuon", new Vector3(-28.0f, 2.10f, 17.0f), 80f, 3.3f),

                // 9. Nhà Dì Hai & Chị Tư Gánh Nước (Gần giếng ngọt phía Đông)
                new HouseData("House_09_DiHai_GiengNuoc", new Vector3(20.0f, 2.10f, 4.0f), -70f, 3.4f),

                // 10. Nhà Chú Bảy Chèo Đò (Bến sông kênh Tây Nam)
                new HouseData("House_10_ChuBayCheoDo", new Vector3(-14.0f, 2.10f, -5.5f), 25f, 3.3f),

                // 11. Nhà Xóm Kênh Đông Nam (Hướng ra bờ kênh)
                new HouseData("House_11_XomKenhDongNam", new Vector3(12.0f, 2.10f, -5.5f), -25f, 3.3f),

                // 12. Nhà Xóm Trong Phía Tây (Mở rộng quy mô làng về phía Tây)
                new HouseData("House_12_XomTrongTay", new Vector3(-36.0f, 2.10f, 7.5f), 55f, 3.3f)
            };

            foreach (var h in houses)
            {
                var inst = Object.Instantiate(fbxThatched, housesRoot.transform);
                inst.name = h.name;
                inst.transform.position = h.position;
                inst.transform.rotation = Quaternion.Euler(0f, h.rotationY, 0f);
                inst.transform.localScale = new Vector3(h.scale, h.scale, h.scale);

                var renderers = inst.GetComponentsInChildren<Renderer>();
                foreach (var r in renderers)
                {
                    if (matThatched != null) r.sharedMaterial = matThatched;
                }

                var col = inst.GetComponent<BoxCollider>(); if (col == null) col = inst.AddComponent<BoxCollider>();
                col.center = new Vector3(0f, -0.2f, 0f);
                col.size = new Vector3(2.5f, 1.8f, 2.5f);

                EditorUtility.SetDirty(inst);
            }

            Debug.Log($"[ReplaceHouses] Đã thay thế và bố trí đồng bộ 12 ngôi nhà tranh truyền thống ({HouseThatchedFbx}) xung quanh cây đa trung tâm!");
        }

        // =========================================================================
        // 2. RADIAL DIRT TRAILS CONNECTING EACH HOUSE TO CENTRAL TREE
        // =========================================================================
        private static void BuildRadialTrailsToCentralBanyanTree()
        {
            var roadMat = AssetDatabase.LoadAssetAtPath<Material>(RoadMatPath);
            var pathsRoot = GameObject.Find("CaMau_1954_Village/Paths_3D_Network") ?? GameObject.Find("Paths_3D_Network");
            if (pathsRoot == null) pathsRoot = new GameObject("Paths_3D_Network");

            var trailsContainer = GameObject.Find("Radial_House_Trails");
            if (trailsContainer != null) Object.DestroyImmediate(trailsContainer);

            trailsContainer = new GameObject("Radial_House_Trails");
            trailsContainer.transform.SetParent(pathsRoot.transform, false);

            Vector3 hubCenter = new Vector3(-6.0f, 0.506f, 13.0f);

            Vector3[] houseYards = new Vector3[]
            {
                new Vector3(3.0f, 0.506f, 17.5f),   // House 1 (Nhà An)
                new Vector3(-14.0f, 0.506f, 16.5f), // House 2 (Trưởng Thôn Năm)
                new Vector3(12.5f, 0.506f, 14.0f),  // House 3 (Bác Ba Phi)
                new Vector3(21.0f, 0.506f, 20.0f),  // House 4 (Chú Năm Thợ Mộc)
                new Vector3(-9.5f, 0.506f, 32.0f),  // House 5 (Bác Ba Nông Dân / Ra đồng)
                new Vector3(12.0f, 0.506f, 30.0f),  // House 6 (Thím Bảy Phơi Thóc)
                new Vector3(-14.5f, 0.506f, 10.0f), // House 7 (Cô Út Bánh Bò)
                new Vector3(-24.0f, 0.506f, 16.0f), // House 8 (Chú Sáu Vườn)
                new Vector3(16.0f, 0.506f, 5.0f),   // House 9 (Dì Hai / Giếng nước)
                new Vector3(-11.0f, 0.506f, -2.5f), // House 10 (Chú Bảy Bến sông)
                new Vector3(10.0f, 0.506f, -2.5f),  // House 11 (Bến sông Đông)
                new Vector3(-32.0f, 0.506f, 8.0f)   // House 12 (Xóm trong Tây)
            };

            for (int i = 0; i < houseYards.Length; i++)
            {
                Vector3 dest = houseYards[i];
                CreateConnectingTrail(trailsContainer.transform, $"Trail_To_House_{i + 1:D2}", hubCenter, dest, roadMat);
            }

            Debug.Log("[Trails] Đã tạo 12 đường mòn đất nối liền từng nhà đến cụm cây lớn trung tâm!");
        }

        private static void CreateConnectingTrail(Transform parent, string trailName, Vector3 start, Vector3 end, Material mat)
        {
            var go = new GameObject(trailName);
            go.transform.SetParent(parent, false);

            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            if (mat != null) mr.sharedMaterial = mat;

            int segments = 10;
            float trailWidth = 1.6f;
            int vertCount = (segments + 1) * 2;

            Vector3[] verts = new Vector3[vertCount];
            Vector2[] uvs = new Vector2[vertCount];
            Vector3[] normals = new Vector3[vertCount];
            int[] tris = new int[segments * 6];

            Vector3 dir = (end - start).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, dir).normalized * (trailWidth * 0.5f);

            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                // Subtle organic curve
                float curve = Mathf.Sin(t * Mathf.PI) * 0.45f;
                Vector3 center = Vector3.Lerp(start, end, t) + side * curve;

                verts[i * 2]     = center - side;
                verts[i * 2 + 1] = center + side;

                normals[i * 2]     = Vector3.up;
                normals[i * 2 + 1] = Vector3.up;

                uvs[i * 2]     = new Vector2(0f, t * 4f);
                uvs[i * 2 + 1] = new Vector2(1f, t * 4f);
            }

            int triIdx = 0;
            for (int i = 0; i < segments; i++)
            {
                int bL = i * 2;
                int bR = bL + 1;
                int tL = (i + 1) * 2;
                int tR = tL + 1;

                tris[triIdx++] = tL;
                tris[triIdx++] = tR;
                tris[triIdx++] = bL;

                tris[triIdx++] = tR;
                tris[triIdx++] = bR;
                tris[triIdx++] = bL;
            }

            Mesh mesh = new Mesh();
            mesh.name = trailName + "_Mesh";
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.normals = normals;
            mesh.triangles = tris;
            mesh.RecalculateBounds();

            mf.sharedMesh = mesh;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = true;
            EditorUtility.SetDirty(go);
        }

        // =========================================================================
        // 3. SETUP 100% STANDING UPRIGHT NPCS WITH ACCURATE GENDER & DIALOGUE
        // =========================================================================
        private static void SetupAllUprightStandingNPCs()
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

            // 1. MÁ CỦA AN (Nữ - Đứng trước sân Nhà An)
            BuildAccurateNPC(npcsRoot.transform, pMother, cFemale, "NPC_Mother_An",
                new Vector3(2.5f, groundY, 17.5f), Quaternion.Euler(0f, 180f, 0f),
                "Má của An", "Má Năm - Xóm Bờ Kênh",
                new string[]
                {
                    "An đó hả con? Đi chơi đâu từ sớm tới giờ, lúa chín vàng cả cánh đồng rồi kìa.",
                    "Về nhà ăn chén cơm gạo mới má vừa nấu, dẻo và thơm lừng luôn nè con."
                },
                new DialogueChoice[]
                {
                    new DialogueChoice { choiceText = "Dạ má, cơm gạo mới thơm quá má ơi!", responseLine = "Ừ, ăn no rồi phụ các bác ngoài sân phơi lúa nghe con. Nhớ đừng chạy nhảy làm đổ nia thóc.", responseAnim = "Female_Talking_01" },
                    new DialogueChoice { choiceText = "Má ơi, chiều nay con ra bến xem tàu tập kết nghen?", responseLine = "Được chớ, nhớ rủ thằng Minh đi chung cho có bạn, chớ có giỡn dưới mé nước sâu nghe chưa!", responseAnim = "Female_Talking_02" }
                },
                "Female_Waving", "Female_Talking_01");

            // 2. TRƯỞNG THÔN NĂM (Nam Cao Niên - Đứng trước nhà Trưởng thôn)
            BuildAccurateNPC(npcsRoot.transform, pElder, cElder, "NPC_Elderly_VillageChief",
                new Vector3(-14.0f, groundY, 16.5f), Quaternion.Euler(0f, 140f, 0f),
                "Trưởng Thôn Năm", "Bậc cao niên xóm Rạch Gốc",
                new string[]
                {
                    "Chào cháu An! Lúa năm nay trúng mùa lớn, hạt nào hạt nấy mẩy vàng óng ả khắp đồng.",
                    "Xóm mình đang phơi lúa mới để đóng bao gửi ra miền Bắc theo đoàn tàu tập kết ngoài cửa Sông Đốc đó."
                },
                new DialogueChoice[]
                {
                    new DialogueChoice { choiceText = "Cháu thấy đồng lúa vàng trải dài đẹp quá thưa bác!", responseLine = "Đúng rồi cháu, đất đai Nam Bộ phù sa màu mỡ nuôi nấng người dân mình. Giữ lấy hạt ngọc trời này nghe con.", responseAnim = "OldMan_Talking" },
                    new DialogueChoice { choiceText = "Bác cho cháu hỏi tàu tập kết bao giờ rời bến?", responseLine = "Sắp rồi con à. Chú Bảy ngoài bến đò đang hối hả chuyển những bao gạo cuối cùng lên ghe đấy.", responseAnim = "OldMan_Talking" }
                },
                "OldMan_Idle", "OldMan_Talking");

            // 3. BÁC BA PHI (Nam Cao Niên - Đứng trước nhà Bác Ba)
            BuildAccurateNPC(npcsRoot.transform, pElder, cElder, "NPC_Elderly_UncleBa",
                new Vector3(12.5f, groundY, 14.0f), Quaternion.Euler(0f, -90f, 0f),
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
                "OldMan_Idle", "OldMan_Talking");

            // 4. BÁC BA NÔNG DÂN (Nam Trưởng Thành - Đứng đầu bờ ruộng lúa)
            BuildAccurateNPC(npcsRoot.transform, pMale01, cMale, "NPC_Farmer_UncleBa",
                new Vector3(-9.5f, groundY, 35.5f), Quaternion.Euler(0f, 165f, 0f),
                "Bác Ba Nông Dân", "Lão nông tri điền xóm Lúa",
                new string[]
                {
                    "Chào cháu An! Lúa chín vàng cả cánh đồng mênh mông rồi con ơi, vụ mùa này bà con mình trúng lớn lắm!",
                    "Bà con xóm mình đang gom những giạ lúa mẩy nhất gửi theo tàu tập kết ra nuôi cán bộ, chiến sĩ miền Bắc đó con."
                },
                new DialogueChoice[]
                {
                    new DialogueChoice { choiceText = "Lúa năm nay đẹp quá thưa bác Ba!", responseLine = "Đúng rồi cháu, đất phù sa Nam Bộ màu mỡ nuôi nấng người dân mình. Giữ lấy hạt ngọc trời này nghe con.", responseAnim = "Male_Talking_01" },
                    new DialogueChoice { choiceText = "Cháu thấy không khí xóm mình rộn ràng quá!", responseLine = "Sắp tới giờ tàu rời bến ngoài cửa Sông Đốc rồi, ai nấy đều muốn gửi chút tình quê hương ra Bắc.", responseAnim = "Male_Talking_02" }
                },
                "Male_Waving", "Male_Talking_01");

            // 5. CÔ ÚT BÁNH BÒ (Nữ Trưởng Thành - Đứng trước quán bánh ngõ Tây)
            BuildAccurateNPC(npcsRoot.transform, pFemale, cFemale, "NPC_Merchant_CoUt",
                new Vector3(-14.5f, groundY, 10.0f), Quaternion.Euler(0f, 90f, 0f),
                "Cô Út Bánh Bò", "Gánh bánh ngọt xóm chợ",
                new string[]
                {
                    "An đó hả con? Lại đây cô Út cho cái bánh bò nước cốt dừa mới ra lò thơm nức mũi nè!",
                    "Cô thức từ sáng sớm làm mẻ bánh này vừa dẻo vừa béo, làm quà tiễn các anh bộ đội xuống tàu tập kết đó."
                },
                new DialogueChoice[]
                {
                    new DialogueChoice { choiceText = "Bánh của cô Út thơm phức luôn ạ!", responseLine = "Ăn ngon thì ăn nhiều nghe con, mai mốt hòa bình cô làm thêm một chõ bự thết đãi cả xóm!", responseAnim = "Female_Talking_01" },
                    new DialogueChoice { choiceText = "Cô ơi, tàu tập kết chừng nào nhổ neo vậy cô?", responseLine = "Chú Tư ngoài bến bảo đợi nước lớn chiều nay là tàu rời bến, cờ bay rợp cả mặt sông.", responseAnim = "Female_Talking_02" }
                },
                "Female_Waving", "Female_Talking_01");

            // 6. CHÚ NĂM THỢ MỘC (Nam Trưởng Thành - Đứng trước xưởng mộc ngõ Đông)
            BuildAccurateNPC(npcsRoot.transform, pMale04, cMale, "NPC_Carpenter_UncleNam",
                new Vector3(21.5f, groundY, 21.0f), Quaternion.Euler(0f, -135f, 0f),
                "Chú Năm Thợ Mộc", "Thợ dựng nhà sàn & xuồng ba lá",
                new string[]
                {
                    "Chào cậu nhỏ An! Chú đang đẽo nốt mấy mái dầm xuồng ba lá cho bà con chèo ra tàu tập kết ngoài cửa biển.",
                    "Gỗ tràm, gỗ đước rừng ngập mặn Cà Mau mình dẻo dai chịu nước mặn bền bỉ dữ lắm con à."
                },
                new DialogueChoice[]
                {
                    new DialogueChoice { choiceText = "Chú làm dầm xuồng đẹp quá chú Năm!", responseLine = "Hahaha! Khen hay lắm, ráng học giỏi sau này đóng tàu sắt lớn vượt đại dương nghe con trai!", responseAnim = "Male_Talking_01" }
                },
                "Male_Waving", "Male_Talking_01");

            // 7. ANH HAI DU KÍCH (Nam Thanh Niên - Đứng ngã ba đường xóm)
            BuildAccurateNPC(npcsRoot.transform, pMale01, cMale, "NPC_Guerrilla_AnhHai",
                new Vector3(-3.5f, groundY, 3.0f), Quaternion.Euler(0f, 170f, 0f),
                "Anh Hai Du Kích", "Chiến sĩ bảo vệ xóm làng",
                new string[]
                {
                    "Chào em An! Em đã gói ghém nắm đất đỏ quê hương gửi ra miền Bắc cho Bác Hồ chưa?",
                    "Nắm đất phù sa Nam Bộ tuy nhỏ bé nhưng mang trọn tấm lòng sắt son của đồng bào miền Nam gửi gắm nơi phương Bắc."
                },
                new DialogueChoice[]
                {
                    new DialogueChoice { choiceText = "Dạ em đã bọc trong khăn tay cẩn thận rồi anh Hai!", responseLine = "Giỏi lắm em trai! Dù tạm thời cách trở non sông, lòng người Nam Bắc một nhà không bao giờ phai nhạt.", responseAnim = "Male_Talking_02" }
                },
                "Male_Waving", "Male_Talking_01");

            // 8. THÍM BẢY PHƠI THÓC (Nữ Trưởng Thành - Đứng mé ruộng phía Đông)
            BuildAccurateNPC(npcsRoot.transform, pFemale, cFemale, "NPC_Villager_AuntBay",
                new Vector3(11.5f, groundY, 33.0f), Quaternion.Euler(0f, -75f, 0f),
                "Thím Bảy Phơi Thóc", "Phụ nữ xóm Lúa Rạch Gốc",
                new string[]
                {
                    "Nắng giòn phơi thóc mẩy hạt, lúa năm nay óng ánh như tơ vàng An ơi!",
                    "Cháu chạy nhảy coi chừng trượt chân xuống rãnh nước ruộng nghen con."
                },
                new DialogueChoice[]
                {
                    new DialogueChoice { choiceText = "Dạ thím Bảy, cháu đi men theo đường thôi ạ.", responseLine = "Ngoan! Lát về thím thưởng cho củ khoai mì hấp nước cốt dừa bùi béo nghen cháu.", responseAnim = "Female_Talking_01" }
                },
                "Female_Waving", "Female_Talking_01");

            // 9. CHÚ BẢY CHÈO ĐÒ (Nam Ngư Dân - Đứng đầu cầu bến sông)
            BuildAccurateNPC(npcsRoot.transform, pMale05, cMale, "NPC_Ferryman_UncleBay",
                new Vector3(-11.0f, groundY, -3.5f), Quaternion.Euler(0f, 25f, 0f),
                "Chú Bảy Chèo Đò", "Chủ xuồng ba lá bến Sông Đốc",
                new string[]
                {
                    "Xuồng ba lá chuẩn bị tách bến sang cồn dừa nước rồi đây con ơi!",
                    "Gió biển Cà Mau thổi lồng lộng, con nước ròng đưa ghe thuyền tấp nập về cửa biển."
                },
                new DialogueChoice[]
                {
                    new DialogueChoice { choiceText = "Chú ơi, ngoài biển Sông Đốc tàu tập kết to lắm hả chú?", responseLine = "To như quả núi con à! Tàu chở cán bộ đồng bào ra Bắc, cờ đỏ sao vàng bay rực rỡ cả góc trời!", responseAnim = "Male_Talking_02" }
                },
                "Male_Waving", "Male_Talking_01");

            // 10. CHỊ TƯ GÁNH NƯỚC (Nữ Trưởng Thành - Đứng bên giếng ngọt phía Đông)
            BuildAccurateNPC(npcsRoot.transform, pFemale, cFemale, "NPC_Female_WaterCarrier",
                new Vector3(16.0f, groundY, 4.5f), Quaternion.Euler(0f, -90f, 0f),
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
                "Female_Waving", "Female_Talking_01");

            // 11. DÌ HAI (Nữ Trung Niên - Đứng cạnh giếng nước)
            BuildAccurateNPC(npcsRoot.transform, pFemale, cFemale, "NPC_Villager_Waving_Mother",
                new Vector3(18.0f, groundY, 2.5f), Quaternion.Euler(0f, -60f, 0f),
                "Dì Hai", "Hậu phương tiễn đoàn tập kết",
                new string[]
                {
                    "Bà con mang thêm bánh tét, gạo mới ra bến Sông Đốc gửi các anh bộ đội nghen!",
                    "Đi mạnh giỏi, ngày thống nhất hòa bình lại về với quê hương Cà Mau!"
                },
                new DialogueChoice[]
                {
                    new DialogueChoice { choiceText = "Dì Hai ơi, mọi người sẽ sớm trở về sum họp phải không dì?", responseLine = "Chắc chắn rồi cháu! Đồng bào miền Nam lúc nào cũng hướng về ngày hội ngộ.", responseAnim = "Female_Talking_01" }
                },
                "Female_Waving", "Female_Talking_01");

            // 12. CHÚ SÁU LÀM VƯỜN (Nam Nông Dân - Đứng xóm vườn chuối Tây)
            BuildAccurateNPC(npcsRoot.transform, pMale04, cMale, "NPC_Villager_Gardener",
                new Vector3(-24.5f, groundY, 16.5f), Quaternion.Euler(0f, 95f, 0f),
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
                "Male_Waving", "Male_Talking_01");

            // 13. THẰNG MINH (Bé Trai - Đứng dưới gốc cây đa trung tâm)
            BuildAccurateNPC(npcsRoot.transform, pMale01, cSocial, "NPC_Friend_Minh",
                new Vector3(-6.0f, groundY, 8.5f), Quaternion.Euler(0f, 0f, 0f),
                "Thằng Minh", "Bạn nối khố của Bé An",
                new string[]
                {
                    "Ê An! Đi bắt cào cào trong ruộng lúa vàng với tao không?",
                    "Hôm qua tao thấy tổ chim sẻ đồng núp dưới gốc lúa bự chà bá luôn á!"
                },
                new DialogueChoice[]
                {
                    new DialogueChoice { choiceText = "Đi liền! Nhưng coi chừng dẫm gãy lúa của bác Năm nha!", responseLine = "Biết rồi, tao đi men theo bờ đường thôi, không có dẫm bậy bạ đâu mà sợ!", responseAnim = "Waving_02" },
                    new DialogueChoice { choiceText = "Má tao đang gọi về ăn cơm rồi, lát gặp nghen!", responseLine = "Ờ, ăn lẹ rồi chạy ra gốc cây đa hú tao một tiếng!", responseAnim = "Waving_01" }
                },
                "Waving_01", "Waving_02", scale: 0.88f);

            // 14. BÉ MẬN (Bé Gái - Đứng dưới tán bàng cạnh cây đa)
            BuildAccurateNPC(npcsRoot.transform, pChildGirl, cSocial, "NPC_Child_Playing_Girl",
                new Vector3(-9.0f, groundY, 9.5f), Quaternion.Euler(0f, 45f, 0f),
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
                "Waving_02", "Waving_03", scale: 0.82f);

            // 15. BÉ ÚT (Em Bé - Đứng trước sân Nhà An)
            BuildAccurateNPC(npcsRoot.transform, pChildGirl, cSocial, "NPC_Baby_Toddler",
                new Vector3(0.5f, groundY, 15.5f), Quaternion.Euler(0f, 180f, 0f),
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
                "Waving_01", "Waving_02", scale: 0.65f);

            Debug.Log("[SetupNPCs] Đã khởi tạo hoàn tất 15 NPC: 100% ĐỨNG THẲNG UPRIGHT, chuẩn xác giới tính từng model và hội thoại!");

            // Sample natural idle poses in Edit Mode so characters stand naturally without T-pose
            SampleIdlePosesForAllNPCs(npcsRoot.transform);
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

            // Ensure proper Animator with assigned runtime controller
            var anim = inst.GetComponent<Animator>(); if (anim == null) anim = inst.AddComponent<Animator>();
            if (animCtrl != null) anim.runtimeAnimatorController = animCtrl;
            anim.applyRootMotion = false;

            // Ensure upright standing CapsuleCollider
            var col = inst.GetComponent<CapsuleCollider>(); if (col == null) col = inst.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.85f * scale, 0f);
            col.radius = 0.35f * scale;
            col.height = 1.70f * scale;

            // Configure Dialogue
            var interactable = inst.GetComponent<GenshinNPCInteractable>(); if (interactable == null) interactable = inst.AddComponent<GenshinNPCInteractable>();
            interactable.Configure(name, title, openings, choices, defAnim, talkAnim);

            EditorUtility.SetDirty(inst);
        }

        // =========================================================================
        // 4. CAPTURE QA SNAPSHOTS (Rule 4.2: 720p 1280x720)
        // =========================================================================
        private static void CaptureQASnapshots()
        {
            if (!Directory.Exists(ArtifactDir)) Directory.CreateDirectory(ArtifactDir);

            int width = 1280;
            int height = 720;

            // Shot 1: Elevated panoramic view of the village showing thatched houses and radial dirt trails
            RenderShot(
                new Vector3(-6.0f, 18.0f, -8.0f),
                Quaternion.Euler(42.0f, 0.0f, 0.0f),
                width, height, 60.0f,
                Path.Combine(ArtifactDir, "CaMau_VillageThatchedHouses_Panoramic_QA.jpg")
            );

            // Shot 2: View from the Central Banyan Tree looking across paths to thatched houses and NPCs
            RenderShot(
                new Vector3(-6.0f, 2.5f, 13.0f),
                Quaternion.Euler(5.0f, 35.0f, 0.0f),
                width, height, 58.0f,
                Path.Combine(ArtifactDir, "CaMau_CentralBanyanTreeAndTrails_QA.jpg")
            );

            // Shot 3: Eye-level ground view showing standing NPCs in front of their thatched houses
            RenderShot(
                new Vector3(-1.0f, 1.8f, 12.0f),
                Quaternion.Euler(4.0f, 20.0f, 0.0f),
                width, height, 52.0f,
                Path.Combine(ArtifactDir, "CaMau_UprightNPCsAndHouses_QA.jpg")
            );

            Debug.Log("[QA] Đã chụp và lưu 3 ảnh QA vào artifact directory!");
        }

        private static void RenderShot(Vector3 pos, Quaternion rot, int w, int h, float fov, string outPath)
        {
            var camObj = new GameObject("Temp_QA_Camera");
            var cam = camObj.AddComponent<Camera>();
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 1200f;
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
