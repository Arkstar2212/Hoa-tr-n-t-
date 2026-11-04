using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using HoaVaMau;
using HoaVaMau.Dialogue;

namespace HoaDo.Editor
{
    public static class OverhaulPaddyHousesNPCsAndCamera
    {
        private const string MudMatPath = "Assets/Materials/M_CaMau_PaddyMud.mat";
        private const string RiceMatPath = "Assets/Materials/RicePlants/Mat_Rice_Meshy.mat";
        private const string RiceMeshPathLOD1 = "Assets/Models/RicePlants/SM_Rice_Plant_Meshy_LOD_1.fbx";
        private const string RiceMeshPathLOD0 = "Assets/Models/RicePlants/SM_Rice_Plant_Meshy_LOD_0.fbx";

        private const string HouseThatchedFbx = "Assets/Models/Optimized/SM_StiltedHouse_Thatched_BlenderOptimized.fbx";
        private const string HouseWoodFbx = "Assets/Models/Optimized/SM_StiltedHouse_Wood_BlenderOptimized.fbx";
        private const string HouseThatchedMat = "Assets/Materials/Environment/Architecture/M_StiltedHouse_Thatched.mat";
        private const string HouseWoodMat = "Assets/Materials/Environment/Architecture/M_StiltedHouse_Wood.mat";

        private const string NpcElderPrefab = "Assets/Prefabs/Characters/NPCs/PF_NPC_Elderly_Man.prefab";
        private const string NpcFemalePrefab = "Assets/Prefabs/Characters/NPCs/PF_NPC_Female_VN.prefab";
        private const string NpcVillager01Prefab = "Assets/Prefabs/Characters/NPCs/PF_NPC_Villager_01.prefab";
        private const string NpcVillager02Prefab = "Assets/Prefabs/Characters/NPCs/PF_NPC_Villager_02.prefab";
        private const string NpcVillager03Prefab = "Assets/Prefabs/Characters/NPCs/PF_NPC_Villager_03.prefab";

        private const string ArtifactDir = "C:/Users/ACER/.gemini/antigravity/brain/0b06ba00-d73d-4df0-adee-d4635db3297d";

        [MenuItem("Tools/Hoa Do/Overhaul Paddy Houses NPCs And Camera")]
        public static void Execute()
        {
            Debug.Log("[Overhaul] Bắt đầu đồng bộ: Lấp đầy lúa & Xóa màu xanh + Thêm nhà + Thêm NPC hội thoại + Zoom Camera...");

            // 1. FILL THE MISSING RICE & REMOVE ALL GREEN SOIL / DIKES
            FixPaddyAndFillDenseRice();

            // 2. ADD MORE AUTHENTIC HOUSES TO THE VILLAGE
            AddMoreVillageHouses();

            // 3. ADD HISTORICAL 1954 NAM BO NPCS WITH FULL DIALOGUE
            AddMoreVillageNPCs();

            // 4. CONFIGURE CAMERA DISTANCE & ZOOM
            ConfigureCameraDistanceAndZoom();

            // 5. SAVE SCENE & ASSETS
            AssetDatabase.SaveAssets();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene()
            );

            // 6. CAPTURE QA SNAPSHOTS
            CaptureQASnapshots();

            Debug.Log("[Overhaul] HOÀN TẤT THÀNH CÔNG! Đã lấp kín lúa vàng, dọn sạch đất xanh, thêm nhà, thêm NPC và căn chỉnh camera zoom!");
        }

        // =========================================================================
        // 1. FILL RICE & ELIMINATE GREEN SOIL / DIKES
        // =========================================================================
        private static void FixPaddyAndFillDenseRice()
        {
            var mudMat = AssetDatabase.LoadAssetAtPath<Material>(MudMatPath);
            if (mudMat != null)
            {
                mudMat.enableInstancing = true;
                EditorUtility.SetDirty(mudMat);
            }

            // Disable all legacy green dikes, berms, and old plots around the paddy
            var allT = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int hiddenGreenCount = 0;
            foreach (var t in allT)
            {
                if (t == null) continue;
                string n = t.name.ToLower();

                bool isOldDikeOrPlot = n.Contains("dike") || n.Contains("berm") || 
                                       n.StartsWith("paddy_plot") || n.Contains("plot_boundary") ||
                                       n.Contains("plot_dike");

                if (isOldDikeOrPlot)
                {
                    // Check if it's in the paddy area (Z >= 34m)
                    if (t.position.z >= 34.0f)
                    {
                        var mr = t.GetComponent<MeshRenderer>();
                        if (mr != null) mr.enabled = false;
                        t.gameObject.SetActive(false);
                        EditorUtility.SetDirty(t.gameObject);
                        hiddenGreenCount++;
                    }
                }
            }
            Debug.Log($"[FixPaddy] Đã ẩn {hiddenGreenCount} bờ mẫu/dike xanh cũ gây loang lổ đất xanh.");

            // Rebuild single vast brown paddy ground mesh down to Z = 35.0f (completely covers south gap)
            var vastPaddyObj = GameObject.Find("Vast_Single_Paddy_Field_BrownSoil");
            if (vastPaddyObj == null)
            {
                vastPaddyObj = new GameObject("Vast_Single_Paddy_Field_BrownSoil");
                vastPaddyObj.transform.position = Vector3.zero;
            }

            var mf = vastPaddyObj.GetComponent<MeshFilter>() ?? vastPaddyObj.AddComponent<MeshFilter>();
            var mrPaddy = vastPaddyObj.GetComponent<MeshRenderer>() ?? vastPaddyObj.AddComponent<MeshRenderer>();
            if (mudMat != null) mrPaddy.sharedMaterial = mudMat;

            int xSubdivs = 24;
            int zSubdivs = 72;
            float minX = -48.0f;
            float maxX = 40.0f;
            float minZ = 35.0f; // Extended down to 35m (covers the entire southern gap!)
            float maxZ = 182.0f;
            float groundY = 0.504f;

            int vertCount = (xSubdivs + 1) * (zSubdivs + 1);
            Vector3[] vertices = new Vector3[vertCount];
            Vector2[] uvs = new Vector2[vertCount];
            Vector3[] normals = new Vector3[vertCount];
            int[] triangles = new int[xSubdivs * zSubdivs * 6];

            for (int z = 0; z <= zSubdivs; z++)
            {
                float tz = (float)z / zSubdivs;
                float posZ = Mathf.Lerp(minZ, maxZ, tz);

                for (int x = 0; x <= xSubdivs; x++)
                {
                    float tx = (float)x / xSubdivs;
                    float posX = Mathf.Lerp(minX, maxX, tx);

                    int idx = z * (xSubdivs + 1) + x;
                    vertices[idx] = new Vector3(posX, groundY, posZ);
                    normals[idx] = Vector3.up;
                    uvs[idx] = new Vector2(tx * 16.0f, tz * 34.0f);
                }
            }

            int triIdx = 0;
            for (int z = 0; z < zSubdivs; z++)
            {
                for (int x = 0; x < xSubdivs; x++)
                {
                    int botLeft  = z * (xSubdivs + 1) + x;
                    int botRight = botLeft + 1;
                    int topLeft  = (z + 1) * (xSubdivs + 1) + x;
                    int topRight = topLeft + 1;

                    triangles[triIdx++] = topLeft;
                    triangles[triIdx++] = topRight;
                    triangles[triIdx++] = botLeft;

                    triangles[triIdx++] = topRight;
                    triangles[triIdx++] = botRight;
                    triangles[triIdx++] = botLeft;
                }
            }

            Mesh mesh = new Mesh();
            mesh.name = "M_VastSinglePaddy_BrownEarth_Extended";
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.normals = normals;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();

            mf.sharedMesh = mesh;
            mrPaddy.enabled = true;
            EditorUtility.SetDirty(vastPaddyObj);

            // Populate dense rice crops covering ALL the way down to Z = 36.5f
            var riceMesh = AssetDatabase.LoadAssetAtPath<Mesh>(RiceMeshPathLOD1) ??
                           AssetDatabase.LoadAssetAtPath<Mesh>(RiceMeshPathLOD0);
            var riceMat = AssetDatabase.LoadAssetAtPath<Material>(RiceMatPath);
            if (riceMat != null) riceMat.enableInstancing = true;

            var oldCrops = GameObject.Find("Vast_Paddy_Continuous_Crops_Root");
            if (oldCrops != null) Object.DestroyImmediate(oldCrops);

            var container = new GameObject("Vast_Paddy_Continuous_Crops_Root");
            container.transform.position = Vector3.zero;

            Transform pNW = new GameObject("West_North").transform; pNW.SetParent(container.transform, false);
            Transform pNM = new GameObject("West_Mid").transform;   pNM.SetParent(container.transform, false);
            Transform pNS = new GameObject("West_South").transform; pNS.SetParent(container.transform, false);

            Transform pEW = new GameObject("East_North").transform; pEW.SetParent(container.transform, false);
            Transform pEM = new GameObject("East_Mid").transform;   pEM.SetParent(container.transform, false);
            Transform pES = new GameObject("East_South").transform; pES.SetParent(container.transform, false);

            var rng = new System.Random(1954);
            int rows = 135; // 135 rows across 144m length (dense ~1.06m spacing)
            float startZ = 180.0f;
            float endZ = 36.5f; // EXTENDED RIGHT DOWN TO 36.5m!
            int totalTufts = 0;

            for (int r = 0; r < rows; r++)
            {
                float t = (float)r / (rows - 1);
                float z = Mathf.Lerp(startZ, endZ, t);

                float curveOffset = Mathf.Sin(t * Mathf.PI * 2.0f) * 0.95f;
                float roadCenterX = -7.0f + curveOffset;

                float roadHalfWidth = 3.6f;
                if (t < 0.08f) roadHalfWidth = Mathf.Lerp(4.0f, 3.6f, t / 0.08f);
                else if (t > 0.88f)
                {
                    float plazaBlend = (t - 0.88f) / 0.12f;
                    roadHalfWidth = Mathf.Lerp(3.6f, 6.2f, plazaBlend * plazaBlend);
                }

                float stepX = 1.05f;
                float hexOffset = (r % 2 == 1) ? stepX * 0.5f : 0f;

                float westMinX = -46.0f;
                float westMaxX = roadCenterX - roadHalfWidth - 0.35f;

                float eastMinX = roadCenterX + roadHalfWidth + 0.35f;
                float eastMaxX = 38.0f;

                Transform westParent = (z > 130f) ? pNW : ((z > 80f) ? pNM : pNS);
                Transform eastParent = (z > 130f) ? pEW : ((z > 80f) ? pEM : pES);

                // Populate West
                float westWidth = westMaxX - westMinX;
                if (westWidth > 1.2f)
                {
                    int clusters = Mathf.Max(2, Mathf.RoundToInt(westWidth / stepX));
                    for (int c = 0; c < clusters; c++)
                    {
                        float frac = (float)c / (clusters - 1);
                        float posX = Mathf.Lerp(westMinX, westMaxX, frac) + hexOffset + ((float)rng.NextDouble() - 0.5f) * 0.20f;
                        if (posX < westMinX || posX > westMaxX) continue;

                        float posZ = z + ((float)rng.NextDouble() - 0.5f) * 0.15f;
                        float s = 2.15f + (float)rng.NextDouble() * 0.40f;
                        float sy = s * (1.0f + (float)rng.NextDouble() * 0.18f);

                        CreateRiceTuft(westParent, riceMesh, riceMat, new Vector3(posX, groundY, posZ), s, sy, rng);
                        totalTufts++;
                    }
                }

                // Populate East
                float eastWidth = eastMaxX - eastMinX;
                if (eastWidth > 1.2f)
                {
                    int clusters = Mathf.Max(2, Mathf.RoundToInt(eastWidth / stepX));
                    for (int c = 0; c < clusters; c++)
                    {
                        float frac = (float)c / (clusters - 1);
                        float posX = Mathf.Lerp(eastMinX, eastMaxX, frac) + hexOffset + ((float)rng.NextDouble() - 0.5f) * 0.20f;
                        if (posX < eastMinX || posX > eastMaxX) continue;

                        float posZ = z + ((float)rng.NextDouble() - 0.5f) * 0.15f;
                        float s = 2.15f + (float)rng.NextDouble() * 0.40f;
                        float sy = s * (1.0f + (float)rng.NextDouble() * 0.18f);

                        CreateRiceTuft(eastParent, riceMesh, riceMat, new Vector3(posX, groundY, posZ), s, sy, rng);
                        totalTufts++;
                    }
                }
            }

            Debug.Log($"[FixPaddy] Đã trồng {totalTufts} khóm lúa lớn bao phủ toàn bộ cánh đồng tới Z = 36.5m (lấp kín hoàn toàn khoảng trống bị thiếu)!");
        }

        private static void CreateRiceTuft(Transform parent, Mesh mesh, Material mat, Vector3 pos, float sxz, float sy, System.Random rng)
        {
            var go = new GameObject("Rice_Tuft");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
            go.transform.localScale = new Vector3(sxz, sy, sxz);

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = true;
        }

        // =========================================================================
        // 2. ADD MORE VILLAGE HOUSES
        // =========================================================================
        private static void AddMoreVillageHouses()
        {
            var fbxThatched = AssetDatabase.LoadAssetAtPath<GameObject>(HouseThatchedFbx);
            var fbxWood = AssetDatabase.LoadAssetAtPath<GameObject>(HouseWoodFbx);
            var matThatched = AssetDatabase.LoadAssetAtPath<Material>(HouseThatchedMat);
            var matWood = AssetDatabase.LoadAssetAtPath<Material>(HouseWoodMat);

            if (matThatched != null) matThatched.SetFloat("_Cull", 0f);
            if (matWood != null) matWood.SetFloat("_Cull", 0f);

            var parentRoot = GameObject.Find("CaMau_1954_Village/Environment/Village_Houses_Network");
            if (parentRoot == null)
            {
                parentRoot = GameObject.Find("Village_Houses_Network") ?? new GameObject("Village_Houses_Network");
            }

            var extraHousesContainer = GameObject.Find("Additional_Village_Houses");
            if (extraHousesContainer != null) Object.DestroyImmediate(extraHousesContainer);

            extraHousesContainer = new GameObject("Additional_Village_Houses");
            extraHousesContainer.transform.SetParent(parentRoot.transform, false);

            // House 1: East Alley Thatched Stilted House (X = 18m, Z = 22m)
            if (fbxThatched != null)
            {
                CreateHouseInstance(fbxThatched, matThatched, "House_Add_EastAlley_Thatched", extraHousesContainer.transform,
                    new Vector3(18.0f, 3.20f, 22.0f), Quaternion.Euler(0f, -30f, 0f), new Vector3(3.3f, 3.3f, 3.3f));
            }

            // House 2: West Alley Wood Stilted House (X = -18.5m, Z = 20.5m)
            if (fbxWood != null)
            {
                CreateHouseInstance(fbxWood, matWood, "House_Add_WestAlley_Wood", extraHousesContainer.transform,
                    new Vector3(-18.5f, 2.15f, 20.5f), Quaternion.Euler(0f, 45f, 0f), new Vector3(3.2f, 3.2f, 3.2f));
            }

            // House 3: Paddy Edge Thatched Cottage (X = -28.0m, Z = 32.0m)
            if (fbxThatched != null)
            {
                CreateHouseInstance(fbxThatched, matThatched, "House_Add_PaddyEdge_Thatched", extraHousesContainer.transform,
                    new Vector3(-28.0f, 3.20f, 32.0f), Quaternion.Euler(0f, 15f, 0f), new Vector3(3.4f, 3.4f, 3.4f));
            }

            // House 4: East Plaza Wood Stilted House (X = 26.0m, Z = 8.0m)
            if (fbxWood != null)
            {
                CreateHouseInstance(fbxWood, matWood, "House_Add_EastPlaza_Wood", extraHousesContainer.transform,
                    new Vector3(26.0f, 2.15f, 8.0f), Quaternion.Euler(0f, -65f, 0f), new Vector3(3.2f, 3.2f, 3.2f));
            }

            // House 5: North Village Thatched House near Paddy Transition (X = -12.0m, Z = 34.0f)
            if (fbxThatched != null)
            {
                CreateHouseInstance(fbxThatched, matThatched, "House_Add_NorthVillage_Thatched", extraHousesContainer.transform,
                    new Vector3(-12.0f, 3.20f, 34.0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(3.3f, 3.3f, 3.3f));
            }

            Debug.Log("[AddHouses] Đã dựng thêm 5 ngôi nhà sàn truyền thống Nam Bộ vào các ngõ xóm và cạnh đồng lúa!");
        }

        private static GameObject CreateHouseInstance(GameObject fbxSource, Material mat, string houseName, Transform parent,
            Vector3 worldPos, Quaternion rotation, Vector3 scale)
        {
            var inst = Object.Instantiate(fbxSource, parent);
            inst.name = houseName;
            inst.transform.position = worldPos;
            inst.transform.rotation = rotation;
            inst.transform.localScale = scale;

            var renderers = inst.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                if (mat != null) r.sharedMaterial = mat;
            }

            var col = inst.GetComponent<BoxCollider>();
            if (col == null)
            {
                col = inst.AddComponent<BoxCollider>();
                col.center = new Vector3(0f, -0.2f, 0f);
                col.size = new Vector3(2.5f, 1.8f, 2.5f);
            }

            EditorUtility.SetDirty(inst);
            return inst;
        }

        // =========================================================================
        // 3. ADD HISTORICAL 1954 NAM BO NPCS WITH FULL DIALOGUE
        // =========================================================================
        private static void AddMoreVillageNPCs()
        {
            var npcsParent = GameObject.Find("CaMau_1954_Village/NPCs") ?? GameObject.Find("NPCs");
            if (npcsParent == null)
            {
                npcsParent = new GameObject("NPCs");
            }

            var extraNPCsContainer = GameObject.Find("Additional_Village_NPCs");
            if (extraNPCsContainer != null) Object.DestroyImmediate(extraNPCsContainer);

            extraNPCsContainer = new GameObject("Additional_Village_NPCs");
            extraNPCsContainer.transform.SetParent(npcsParent.transform, false);

            var pElder = AssetDatabase.LoadAssetAtPath<GameObject>(NpcElderPrefab) ??
                         AssetDatabase.LoadAssetAtPath<GameObject>(NpcVillager01Prefab);
            var pFemale = AssetDatabase.LoadAssetAtPath<GameObject>(NpcFemalePrefab);
            var pVillager02 = AssetDatabase.LoadAssetAtPath<GameObject>(NpcVillager02Prefab) ?? pElder;
            var pVillager03 = AssetDatabase.LoadAssetAtPath<GameObject>(NpcVillager03Prefab) ?? pElder;

            // NPC 1: Bác Ba Nông Dân (Đầu bờ ruộng lúa)
            if (pElder != null)
            {
                CreateNPCWithDialogue(extraNPCsContainer.transform, pElder, "NPC_Farmer_UncleBa",
                    new Vector3(-9.5f, 0.52f, 36.5f), Quaternion.Euler(0f, 160f, 0f),
                    "Bác Ba Nông Dân", "Lão nông tri điền xóm Lúa",
                    new string[]
                    {
                        "Chào cháu An! Lúa chín vàng cả cánh đồng mênh mông rồi con ơi, vụ này bà con mình trúng mùa lớn lắm!",
                        "Bà con trong xóm đang gom những giạ lúa mẩy nhất gửi theo đoàn tàu tập kết ra nuôi cán bộ, bộ đội miền Bắc đó con."
                    },
                    new DialogueChoice[]
                    {
                        new DialogueChoice
                        {
                            choiceText = "Lúa năm nay đẹp quá thưa bác Ba!",
                            responseLine = "Đúng rồi cháu, đất phù sa Nam Bộ màu mỡ nuôi nấng người dân mình. Giữ lấy hạt ngọc trời này nghe con.",
                            responseAnim = "OldMan_Talking"
                        },
                        new DialogueChoice
                        {
                            choiceText = "Cháu thấy không khí xóm mình rộn ràng quá!",
                            responseLine = "Sắp tới giờ tàu rời bến ngoài cửa Sông Đốc rồi con à, ai nấy đều muốn gửi chút tình quê hương ra Bắc.",
                            responseAnim = "OldMan_Talking"
                        }
                    },
                    "OldMan_Idle", "OldMan_Talking");
            }

            // NPC 2: Cô Út Bánh Bò (Sân giếng xóm chợ)
            if (pFemale != null)
            {
                CreateNPCWithDialogue(extraNPCsContainer.transform, pFemale, "NPC_Merchant_CoUt",
                    new Vector3(6.5f, 0.52f, 16.0f), Quaternion.Euler(0f, -110f, 0f),
                    "Cô Út Bánh Bò", "Gánh bánh ngọt xóm chợ",
                    new string[]
                    {
                        "An đó hả con? Lại đây cô Út cho cái bánh bò nước cốt dừa mới ra lò thơm nức mũi nè!",
                        "Cô thức từ sáng sớm làm mẻ bánh này vừa dẻo vừa béo, làm quà tiễn các anh bộ đội xuống tàu tập kết đó."
                    },
                    new DialogueChoice[]
                    {
                        new DialogueChoice
                        {
                            choiceText = "Bánh của cô Út thơm phức luôn ạ!",
                            responseLine = "Ăn ngon thì ăn nhiều nghe con, mai mốt hòa bình cô làm thêm một chõ bự thết đãi cả xóm Rạch Gốc!",
                            responseAnim = "Female_Talking_01"
                        },
                        new DialogueChoice
                        {
                            choiceText = "Cô ơi, tàu tập kết chừng nào nhổ neo vậy cô?",
                            responseLine = "Chú Tư ngoài bến bảo đợi nước lớn chiều nay là tàu rời bến, cờ đỏ sao vàng bay rợp cả mặt sông.",
                            responseAnim = "Female_Talking_02"
                        }
                    },
                    "Female_Waving", "Female_Talking_01");
            }

            // NPC 3: Chú Năm Thợ Mộc (Bên ngôi nhà mới ngõ Đông)
            if (pVillager03 != null)
            {
                CreateNPCWithDialogue(extraNPCsContainer.transform, pVillager03, "NPC_Carpenter_UncleNam",
                    new Vector3(15.0f, 0.52f, 21.0f), Quaternion.Euler(0f, -45f, 0f),
                    "Chú Năm Thợ Mộc", "Thợ dựng nhà sàn & xuồng ba lá",
                    new string[]
                    {
                        "Chào cậu nhỏ An! Chú đang đẽo nốt mấy mái dầm xuồng ba lá cho bà con chèo ra tàu tập kết ngoài cửa biển.",
                        "Gỗ tràm, gỗ đước rừng ngập mặn Cà Mau mình dẻo dai chịu nước mặn bền bỉ dữ lắm con à."
                    },
                    new DialogueChoice[]
                    {
                        new DialogueChoice
                        {
                            choiceText = "Chú làm dầm xuồng đẹp quá chú Năm!",
                            responseLine = "Hahaha! Khen hay lắm, ráng học giỏi sau này đóng tàu sắt lớn vượt đại dương nghe con trai!",
                            responseAnim = "Male_Talking_01"
                        }
                    },
                    "Male_Waving", "Male_Talking_01");
            }

            // NPC 4: Anh Hai Du Kích (Ngã ba đường xóm)
            if (pVillager02 != null)
            {
                CreateNPCWithDialogue(extraNPCsContainer.transform, pVillager02, "NPC_Guerrilla_AnhHai",
                    new Vector3(-3.5f, 0.52f, 3.0f), Quaternion.Euler(0f, 170f, 0f),
                    "Anh Hai Du Kích", "Chiến sĩ bảo vệ xóm làng",
                    new string[]
                    {
                        "Chào em An! Em đã gói ghém nắm đất đỏ quê hương gửi ra miền Bắc cho Bác Hồ chưa?",
                        "Nắm đất phù sa Nam Bộ tuy nhỏ bé nhưng mang trọn tấm lòng sắt son của đồng bào miền Nam gửi gắm nơi phương Bắc."
                    },
                    new DialogueChoice[]
                    {
                        new DialogueChoice
                        {
                            choiceText = "Dạ em đã bọc trong khăn tay cẩn thận rồi anh Hai!",
                            responseLine = "Giỏi lắm em trai! Dù tạm thời cách trở non sông, lòng người Nam Bắc một nhà không bao giờ phai nhạt.",
                            responseAnim = "Waving_01"
                        }
                    },
                    "Waving_01", "Waving_02");
            }

            // NPC 5: Thím Bảy Phơi Thóc (Đầu ngõ ruộng phía Đông)
            if (pFemale != null)
            {
                CreateNPCWithDialogue(extraNPCsContainer.transform, pFemale, "NPC_Villager_AuntBay",
                    new Vector3(11.5f, 0.52f, 34.0f), Quaternion.Euler(0f, -75f, 0f),
                    "Thím Bảy Phơi Thóc", "Phụ nữ xóm Lúa Rạch Gốc",
                    new string[]
                    {
                        "Nắng giòn phơi thóc mẩy hạt, lúa năm nay óng ánh như tơ vàng An ơi!",
                        "Cháu chạy nhảy coi chừng trượt chân xuống rãnh nước ruộng nghen con."
                    },
                    new DialogueChoice[]
                    {
                        new DialogueChoice
                        {
                            choiceText = "Dạ thím Bảy, cháu đi men theo đường thôi ạ.",
                            responseLine = "Ngoan! Lát về thím thưởng cho củ khoai mì hấp nước cốt dừa bùi béo nghen cháu.",
                            responseAnim = "Female_Talking_01"
                        }
                    },
                    "Female_Waving", "Female_Talking_01");
            }

            Debug.Log("[AddNPCs] Đã thêm 5 NPC lịch sử Nam Bộ 1954 với đầy đủ tên nhân vật, bối cảnh và hội thoại tương tác!");
        }

        private static void CreateNPCWithDialogue(Transform parent, GameObject prefab, string objName,
            Vector3 pos, Quaternion rot, string name, string title, string[] openings, DialogueChoice[] choices,
            string defAnim, string talkAnim)
        {
            var inst = Object.Instantiate(prefab, parent);
            inst.name = objName;
            inst.transform.position = pos;
            inst.transform.rotation = rot;

            // Collider
            var col = inst.GetComponent<CapsuleCollider>();
            if (col == null)
            {
                col = inst.AddComponent<CapsuleCollider>();
                col.center = new Vector3(0f, 0.85f, 0f);
                col.radius = 0.35f;
                col.height = 1.70f;
            }

            // Dialogue Component
            var interactable = inst.GetComponent<GenshinNPCInteractable>() ?? inst.AddComponent<GenshinNPCInteractable>();
            interactable.Configure(name, title, openings, choices, defAnim, talkAnim);

            EditorUtility.SetDirty(inst);
        }

        // =========================================================================
        // 4. CONFIGURE CAMERA DISTANCE & ZOOM
        // =========================================================================
        private static void ConfigureCameraDistanceAndZoom()
        {
            var mainCam = Camera.main;
            if (mainCam == null)
            {
                var camObj = GameObject.Find("Main Camera");
                if (camObj != null) mainCam = camObj.GetComponent<Camera>();
            }

            if (mainCam != null)
            {
                var orbitCam = mainCam.GetComponent<ThirdPersonOrbitCamera>();
                if (orbitCam != null)
                {
                    var so = new SerializedObject(orbitCam);
                    so.Update();

                    var spDefault = so.FindProperty("defaultDistance");
                    var spMin = so.FindProperty("minDistance");
                    var spMax = so.FindProperty("maxDistance");
                    var spZoomSpeed = so.FindProperty("zoomSpeed");

                    // Set closer default distance (2.6m) and responsive zoom limits (0.8m - 7.5m)
                    if (spDefault != null) spDefault.floatValue = 2.6f;
                    if (spMin != null) spMin.floatValue = 0.8f;
                    if (spMax != null) spMax.floatValue = 7.5f;
                    if (spZoomSpeed != null) spZoomSpeed.floatValue = 2.0f;

                    so.ApplyModifiedProperties();

                    // Snap immediately to target with new distance
                    orbitCam.SnapToTarget();
                    EditorUtility.SetDirty(orbitCam);
                    Debug.Log("<color=#10b981>[Camera]</color> Đã cập nhật ThirdPersonOrbitCamera: defaultDistance = 2.6m, minDistance = 0.8m (cho phép lăn chuột zoom sát lại gần An)!");
                }
            }
        }

        // =========================================================================
        // 5. CAPTURE QA SNAPSHOTS (Rule 4.2: 720p 1280x720)
        // =========================================================================
        private static void CaptureQASnapshots()
        {
            if (!Directory.Exists(ArtifactDir)) Directory.CreateDirectory(ArtifactDir);

            int width = 1280;
            int height = 720;

            // Shot 1: The South Paddy Gap that was previously empty with green dikes
            // Now filled with lush rice and brown earth!
            RenderShot(
                new Vector3(-6.5f, 5.0f, 28.0f),
                Quaternion.Euler(14.0f, 0.0f, 0.0f),
                width, height, 56.0f,
                Path.Combine(ArtifactDir, "CaMau_FixedSouthPaddyGap_QA.jpg")
            );

            // Shot 2: Village street view showing new houses and NPCs
            RenderShot(
                new Vector3(-5.0f, 3.5f, 40.0f),
                Quaternion.Euler(12.0f, 180.0f, 0.0f),
                width, height, 58.0f,
                Path.Combine(ArtifactDir, "CaMau_VillageHousesAndNPCs_QA.jpg")
            );

            // Shot 3: Close-up zoom view on Player An
            var player = GameObject.Find("Player_An_3D") ?? GameObject.Find("Player_An");
            Vector3 playerPos = (player != null) ? player.transform.position : new Vector3(-7.0f, 0.55f, 182.0f);
            RenderShot(
                playerPos + new Vector3(0.0f, 1.4f, 2.6f),
                Quaternion.Euler(10.0f, 180.0f, 0.0f),
                width, height, 52.0f,
                Path.Combine(ArtifactDir, "CaMau_CameraZoomView_QA.jpg")
            );

            Debug.Log("[QA] Đã chụp và lưu 3 ảnh QA 720p vào artifact directory!");
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
