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
        private const string MountainMatPath = "Assets/Materials/Environment/Rocks/M_Mountain_Crag_Rock.mat";
        private const string RockMeshPath = "Assets/Models/Optimized/SM_Rock_Large_BlenderOptimized.fbx";

        private const string MudMatPath = "Assets/Materials/M_CaMau_PaddyMud.mat";
        private const string RiceMatPath = "Assets/Materials/RicePlants/Mat_Rice_Meshy.mat";
        private const string RiceMeshPathLOD1 = "Assets/Models/RicePlants/SM_Rice_Plant_Meshy_LOD_1.fbx";
        private const string RiceMeshPathLOD0 = "Assets/Models/RicePlants/SM_Rice_Plant_Meshy_LOD_0.fbx";

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
        private const float ROAD_CENTER_X = -7.0f;

        [MenuItem("Tools/Hoa Do/Overhaul Village Thatched Houses And Accurate NPCs")]
        public static void Execute()
        {
            Debug.Log("[VillageOverhaul] Đang thực hiện gói nâng cấp toàn diện: 1. Lúa đồng đều đối xứng tuyệt đối; 2. Xóa sạch rãnh trắng núi đá & bịt kín khe trời; 3. Tái thiết bố cục làng quê hài hòa, nền đất nâu liền mạch!");

            // 0. CLEANUP LEGACY GHOST NPC DIALOGUE TRIGGERS
            CleanupLegacyNPCGameplayTriggers();

            // 1. TUNE MOUNTAIN ROCK MATERIAL & SEAL ALL HORIZON SKY GAPS ("Núi đá không còn rãnh trắng nữa")
            SealMountainWhiteGapsAndTuneMaterial();

            // 2. REGENERATE 100% SYMMETRICAL LUSH RICE FIELDS & CONTINUOUS BROWN MUD SOIL ("Lúa đồng đều hơn giữa 2 bên")
            BuildSymmetricalVastBrownPaddyAndRice();

            // 3. REORGANIZE VILLAGE COTTAGES: BALANCED, EVEN, AND BEAUTIFUL ("Bố cục ngôi làng đẹp hơn và đều hơn")
            ReplaceAllHousesWithOriginalThatchedCottages();

            // 4. UNIFIED CONTINUOUS VILLAGE EARTHEN GROUND (No jagged stepped cutouts or green holes in living yards)
            BuildUnifiedEarthenGroundNetwork();

            // 5. SETUP 100% STANDING UPRIGHT NPCS SYNCHRONIZED WITH THE NEW HOUSES & PLAZA
            SetupAllUprightStandingNPCs();

            // 6. OVERHAUL ALL TREES TO 8-DIRECTION 3D VOLUMETRIC, FIRMLY ROOTED, SYMMETRICAL FLANK BELTS
            OverhaulAllTrees8DirectionsAndDenseFlanks();

            // 7. MARK SCENE DIRTY & SAVE ASSETS
            AssetDatabase.SaveAssets();
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);

            // 8. CAPTURE QA SNAPSHOTS (Rule 4.2: 720p)
            CaptureQASnapshots();

            Debug.Log("[VillageOverhaul] HOÀN TẤT THÀNH CÔNG! Đã fix lúa đối xứng 100%, núi đá mịn màng không rãnh trắng, và bố cục làng quê đều đẹp liền mạch!");
        }

        // =========================================================================
        // 0. CLEANUP LEGACY GHOST NPC DIALOGUE TRIGGERS
        // =========================================================================
        public static void CleanupLegacyNPCGameplayTriggers()
        {
            var triggers = GameObject.Find("CaMau_1954_Village/Gameplay_Triggers");
            if (triggers == null) return;

            string[] legacyNpcTriggers = new string[]
            {
                "Trigger_01_Mother",
                "Trigger_06_ElderBa",
                "Trigger_07_RoadVillagerTu",
                "Trigger_08_VillageChief",
                "Trigger_09_VillageKids",
                "Trigger_11_BabyToddler",
                "Trigger_12_Gardener",
                "Trigger_14_FarmerRiceField",
                "Trigger_16_Ferryman"
            };

            int removedCount = 0;
            foreach (var name in legacyNpcTriggers)
            {
                var tr = triggers.transform.Find(name);
                if (tr != null)
                {
                    Object.DestroyImmediate(tr.gameObject);
                    removedCount++;
                }
            }

            Debug.Log($"[CleanupTriggers] Đã xóa {removedCount} legacy NPC triggers khỏi Gameplay_Triggers.");
        }

        // =========================================================================
        // 1. TUNE MOUNTAIN ROCK MATERIAL & SEAL HORIZON GAPS ("Núi đá không còn rãnh trắng")
        // =========================================================================
        public static void SealMountainWhiteGapsAndTuneMaterial()
        {
            // A. Material Tuning: Eliminate white rim highlights, chiseled facet borders, and sharp crack crevices
            var rockMat = AssetDatabase.LoadAssetAtPath<Material>(MountainMatPath);
            if (rockMat != null)
            {
                // Remove white rim highlighting completely
                rockMat.SetFloat("_RimStrength", 0.0f);
                rockMat.SetColor("_RimColor", new Color(0.55f, 0.58f, 0.50f, 1.0f));

                // Smooth out facet sharpness into organic painterly Ghibli stone
                rockMat.SetFloat("_FacetStrength", 0.15f);

                // Soft cel shading ramp (no sharp harsh white/dark polygon stepping)
                rockMat.SetFloat("_CelRampThreshold", 0.32f);
                rockMat.SetFloat("_CelRampSmoothness", 0.35f);

                // Disable artificial crack noise that creates white/contrasting slits
                rockMat.SetFloat("_CrackStrength", 0.0f);
                rockMat.SetFloat("_VerticalBandingStrength", 0.05f);

                // Restrict top lichen to only flat upper ledges
                rockMat.SetFloat("_UseTopOverlay", 1.0f);
                rockMat.SetFloat("_TopNormalThreshold", 0.82f);
                rockMat.SetFloat("_TopBlendSmoothness", 0.12f);
                rockMat.SetColor("_TopColor", new Color(0.50f, 0.56f, 0.42f, 1.0f));
                rockMat.SetColor("_TopShadowColor", new Color(0.24f, 0.30f, 0.24f, 1.0f));

                // Natural muted slate stone body
                rockMat.SetColor("_BaseColor", new Color(0.58f, 0.62f, 0.54f, 1.0f));
                rockMat.SetColor("_MidtoneColor", new Color(0.42f, 0.48f, 0.44f, 1.0f));
                rockMat.SetColor("_ShadowColor", new Color(0.20f, 0.28f, 0.28f, 1.0f));

                EditorUtility.SetDirty(rockMat);
            }

            var rockMesh = AssetDatabase.LoadAssetAtPath<Mesh>(RockMeshPath);
            if (rockMesh == null)
            {
                Debug.LogWarning("[SealMountain] Không tìm thấy rock mesh " + RockMeshPath);
                return;
            }

            // B. Add Center Spire Rock behind North Gate (Closes the 40m sky hole between Deep_01 and Deep_02)
            var northGateParent = GameObject.Find("CaMau_1954_Village/Environment/North_Gate_Passage");
            var centerMtn = GameObject.Find("NorthMtn_Rock_Center_Spire");
            if (centerMtn == null)
            {
                centerMtn = new GameObject("NorthMtn_Rock_Center_Spire");
                if (northGateParent != null) centerMtn.transform.SetParent(northGateParent.transform, true);
            }
            centerMtn.transform.position = new Vector3(ROAD_CENTER_X, 16.0f, 226.0f);
            centerMtn.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
            centerMtn.transform.localScale = new Vector3(46.0f, 42.0f, 38.0f);
            var mfCenter = centerMtn.GetComponent<MeshFilter>();
            if (mfCenter == null) mfCenter = centerMtn.AddComponent<MeshFilter>();
            mfCenter.sharedMesh = rockMesh;

            var mrCenter = centerMtn.GetComponent<MeshRenderer>();
            if (mrCenter == null) mrCenter = centerMtn.AddComponent<MeshRenderer>();
            mrCenter.sharedMaterial = rockMat;

            // B2. Add West & East Saddle Sealer Rocks (Closes any sky gaps in mountain valleys)
            var westSaddle = GameObject.Find("NorthMtn_Rock_West_SaddleSealer");
            if (westSaddle == null)
            {
                westSaddle = new GameObject("NorthMtn_Rock_West_SaddleSealer");
                if (northGateParent != null) westSaddle.transform.SetParent(northGateParent.transform, true);
            }
            westSaddle.transform.position = new Vector3(-24.0f, 18.0f, 230.0f);
            westSaddle.transform.rotation = Quaternion.Euler(0f, 30f, 0f);
            westSaddle.transform.localScale = new Vector3(38.0f, 40.0f, 34.0f);
            var mfWS = westSaddle.GetComponent<MeshFilter>();
            if (mfWS == null) mfWS = westSaddle.AddComponent<MeshFilter>();
            mfWS.sharedMesh = rockMesh;
            var mrWS = westSaddle.GetComponent<MeshRenderer>();
            if (mrWS == null) mrWS = westSaddle.AddComponent<MeshRenderer>();
            mrWS.sharedMaterial = rockMat;

            var eastSaddle = GameObject.Find("NorthMtn_Rock_East_SaddleSealer");
            if (eastSaddle == null)
            {
                eastSaddle = new GameObject("NorthMtn_Rock_East_SaddleSealer");
                if (northGateParent != null) eastSaddle.transform.SetParent(northGateParent.transform, true);
            }
            eastSaddle.transform.position = new Vector3(10.0f, 18.0f, 230.0f);
            eastSaddle.transform.rotation = Quaternion.Euler(0f, 60f, 0f);
            eastSaddle.transform.localScale = new Vector3(38.0f, 40.0f, 34.0f);
            var mfES = eastSaddle.GetComponent<MeshFilter>();
            if (mfES == null) mfES = eastSaddle.AddComponent<MeshFilter>();
            mfES.sharedMesh = rockMesh;
            var mrES = eastSaddle.GetComponent<MeshRenderer>();
            if (mrES == null) mrES = eastSaddle.AddComponent<MeshRenderer>();
            mrES.sharedMaterial = rockMat;

            // C. Add Close Backdrop Rock behind Gate (Completely blocks sky under wooden gateway)
            var backdropRoot = GameObject.Find("North_Backdrop_Elements");
            var closeRock = GameObject.Find("Rock_Backdrop_CenterPass");
            if (closeRock == null)
            {
                closeRock = new GameObject("Rock_Backdrop_CenterPass");
                if (backdropRoot != null) closeRock.transform.SetParent(backdropRoot.transform, true);
            }
            closeRock.transform.position = new Vector3(ROAD_CENTER_X, 3.8f, 198.5f);
            closeRock.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            closeRock.transform.localScale = new Vector3(6.5f, 7.5f, 4.8f);

            var mfClose = closeRock.GetComponent<MeshFilter>();
            if (mfClose == null) mfClose = closeRock.AddComponent<MeshFilter>();
            mfClose.sharedMesh = rockMesh;

            var mrClose = closeRock.GetComponent<MeshRenderer>();
            if (mrClose == null) mrClose = closeRock.AddComponent<MeshRenderer>();
            mrClose.sharedMaterial = rockMat;

            Debug.Log("[SealMountain] Đã hoàn tất phủ mịn vật liệu đá Ghibli và bít kín các khe hở rãnh trắng!");
        }

        // =========================================================================
        // 2. REGENERATE 100% SYMMETRICAL LUSH RICE FIELDS ("Lúa đồng đều hơn giữa 2 bên")
        // =========================================================================
        public static void BuildSymmetricalVastBrownPaddyAndRice()
        {
            var mudMat = AssetDatabase.LoadAssetAtPath<Material>(MudMatPath);
            var riceMat = AssetDatabase.LoadAssetAtPath<Material>(RiceMatPath);
            if (riceMat != null) riceMat.enableInstancing = true;

            var riceMesh = AssetDatabase.LoadAssetAtPath<Mesh>(RiceMeshPathLOD1) ??
                           AssetDatabase.LoadAssetAtPath<Mesh>(RiceMeshPathLOD0);

            if (mudMat == null || riceMat == null || riceMesh == null)
            {
                Debug.LogWarning("[SymmetricalPaddy] Không đủ tài nguyên mesh/material cho đồng lúa!");
                return;
            }

            // A. Single Vast Mud Base Mesh: Perfectly symmetrical around ROAD_CENTER_X (-7.0f)
            // Width = 82m (West: -48.0m to East: +34.0m, exact center -7.0m)
            // Length = Z from 44.0m to 182.0m (seals seamlessly into village earthen ground at Z = 44m)
            var vastPaddyObj = GameObject.Find("Vast_Single_Paddy_Field_BrownSoil");
            if (vastPaddyObj == null) vastPaddyObj = new GameObject("Vast_Single_Paddy_Field_BrownSoil");

            var terrainParent = GameObject.Find("CaMau_1954_Village/Environment/Terrain_3D");
            if (terrainParent != null) vastPaddyObj.transform.SetParent(terrainParent.transform, false);

            vastPaddyObj.transform.position = Vector3.zero;
            vastPaddyObj.transform.rotation = Quaternion.identity;
            vastPaddyObj.transform.localScale = Vector3.one;

            var mf = vastPaddyObj.GetComponent<MeshFilter>();
            if (mf == null) mf = vastPaddyObj.AddComponent<MeshFilter>();

            var mr = vastPaddyObj.GetComponent<MeshRenderer>();
            if (mr == null) mr = vastPaddyObj.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mudMat;

            int xSubdivs = 24;
            int zSubdivs = 72;
            float minX = -48.0f;
            float maxX = 34.0f; // Exactly symmetric: distance from -7.0 is 41.0m to both sides!
            float minZ = 32.0f; // Extended cleanly down to village entrance (Z = 32m) to eliminate all green gaps
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
                    uvs[idx] = new Vector2(tx * 16.0f, tz * 32.0f);
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
            mesh.name = "M_VastSymmetricalPaddy_BrownEarth";
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.normals = normals;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();

            mf.sharedMesh = mesh;
            mr.enabled = true;
            EditorUtility.SetDirty(vastPaddyObj);

            // B. Clean up old rice container
            var oldCrops = GameObject.Find("Vast_Paddy_Continuous_Crops_Root");
            if (oldCrops != null) Object.DestroyImmediate(oldCrops);

            var cropsRoot = new GameObject("Vast_Paddy_Continuous_Crops_Root");
            cropsRoot.transform.position = Vector3.zero;

            Transform pWest = new GameObject("West_Paddy_Flank").transform; pWest.SetParent(cropsRoot.transform, false);
            Transform pEast = new GameObject("East_Paddy_Flank").transform; pEast.SetParent(cropsRoot.transform, false);

            var rng = new System.Random(1954);

            // Both West & East have IDENTICAL parameters:
            // Road center: -7.0f, Road half-width: 3.5f (road spans [-10.5, -3.5])
            // Clearance to rice: 0.40m
            // West flank spans: X in [-46.9m, -10.9m] -> Width = 36.0m
            // East flank spans: X in [-3.1m, +32.9m]  -> Width = 36.0m
            // Both flanks start at Z = 34.0m and end at Z = 180.0m (seamlessly reaching village border)
            int rows = 76;
            float startZ = 180.0f;
            float endZ = 34.0f;
            float roadHalfWidth = 3.50f;
            float riceClearance = 0.40f;

            float westInnerX = ROAD_CENTER_X - roadHalfWidth - riceClearance; // -10.90f
            float westOuterX = westInnerX - 36.0f;                             // -46.90f

            float eastInnerX = ROAD_CENTER_X + roadHalfWidth + riceClearance; // -3.10f
            float eastOuterX = eastInnerX + 36.0f;                             // +32.90f

            int clustersPerRow = 18;
            int totalTufts = 0;

            for (int r = 0; r < rows; r++)
            {
                float tz = (float)r / (rows - 1);
                float z = Mathf.Lerp(startZ, endZ, tz);

                float hexOffset = (r % 2 == 1) ? 0.60f : 0.0f;

                // West Flank (18 lush clusters)
                for (int c = 0; c < clustersPerRow; c++)
                {
                    float frac = (float)c / (clustersPerRow - 1);
                    float x = Mathf.Lerp(westOuterX, westInnerX, frac) + ((c % 2 == 1) ? hexOffset : -hexOffset) * 0.35f;
                    float zJitter = z + ((float)rng.NextDouble() - 0.5f) * 0.15f;

                    float s = 3.10f + (float)rng.NextDouble() * 0.45f;
                    float sy = s * (1.0f + (float)rng.NextDouble() * 0.15f);

                    CreateRiceTuft(pWest, riceMesh, riceMat, new Vector3(x, UNIFORM_GROUND_Y, zJitter), s, sy, rng);
                    totalTufts++;
                }

                // East Flank (18 lush clusters, exact mirror)
                for (int c = 0; c < clustersPerRow; c++)
                {
                    float frac = (float)c / (clustersPerRow - 1);
                    float x = Mathf.Lerp(eastInnerX, eastOuterX, frac) + ((c % 2 == 1) ? hexOffset : -hexOffset) * 0.35f;
                    float zJitter = z + ((float)rng.NextDouble() - 0.5f) * 0.15f;

                    float s = 3.10f + (float)rng.NextDouble() * 0.45f;
                    float sy = s * (1.0f + (float)rng.NextDouble() * 0.15f);

                    CreateRiceTuft(pEast, riceMesh, riceMat, new Vector3(x, UNIFORM_GROUND_Y, zJitter), s, sy, rng);
                    totalTufts++;
                }
            }

            Debug.Log($"[SymmetricalPaddy] Đã tạo thành công {totalTufts} khóm lúa to đẹp ĐỐI XỨNG TUYỆT ĐỐI giữa 2 bên cánh đồng!");
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

        // =========================================================================
        // 3. REORGANIZE VILLAGE COTTAGES: BALANCED, EVEN, AND BEAUTIFUL
        // =========================================================================
        private struct HouseLayoutData
        {
            public string name;
            public Vector2 posXZ;
            public float rotationY;
            public float scale;

            public HouseLayoutData(string n, float x, float z, float rotY, float s = 5.0f)
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
                matThatched.SetFloat("_Cull", 0f);
                if (matThatched.HasProperty("_BaseColor")) matThatched.SetColor("_BaseColor", Color.white);
                EditorUtility.SetDirty(matThatched);
            }

            if (fbxThatched == null)
            {
                Debug.LogError("[ReplaceHouses] Không tìm thấy " + HouseThatchedFbx);
                return;
            }

            var parentHouses = GameObject.Find("CaMau_1954_Village/Environment/Village_Houses_Network");
            if (parentHouses != null) Object.DestroyImmediate(parentHouses);

            var oldAddHouses = GameObject.Find("Additional_Village_Houses");
            if (oldAddHouses != null) Object.DestroyImmediate(oldAddHouses);

            var env = GameObject.Find("CaMau_1954_Village/Environment") ?? GameObject.Find("CaMau_1954_Village");
            var housesRoot = new GameObject("Village_Houses_Network");
            if (env != null) housesRoot.transform.SetParent(env.transform, false);

            // BALANCED, HARMONIOUS 12-HOUSE ENSEMBLE:
            // Symmetrical North row facing South: House 01 & 02 (both 6.0m from road axis X = -7.0m)
            // Symmetrical South row facing South: House 10 & 11 (both 6.0m from road axis X = -7.0m)
            // West Hamlet: 4 houses arranged in a gentle crescent facing East toward central square
            // East Hamlet: 4 houses arranged in a matching crescent facing West toward central square
            HouseLayoutData[] houses = new HouseLayoutData[]
            {
                // North Row (Bờ ruộng lúa, quay mặt về Nam đón gió quảng trường)
                new HouseLayoutData("House_01_NhaAn", -1.0f, 26.0f, 180f, 5.0f),
                new HouseLayoutData("House_02_TruongThonNam", -13.0f, 26.0f, 180f, 5.0f),

                // West Hamlet (Xóm Tây - vườn cây & lò rèn)
                new HouseLayoutData("House_07_CoUtBanhBo", -20.0f, 14.0f, 90f, 5.0f),
                new HouseLayoutData("House_08_ChuSauVuon", -28.0f, 20.0f, 120f, 5.0f),
                new HouseLayoutData("House_06_BacTamRen", -28.0f, 8.0f, 60f, 5.0f),
                new HouseLayoutData("House_12_XomTay", -36.0f, 14.0f, 90f, 5.0f),

                // East Hamlet (Xóm Đông - giếng ngọt & vườn cây trái)
                new HouseLayoutData("House_03_BacBaPhi", 8.0f, 14.0f, -90f, 5.0f),
                new HouseLayoutData("House_04_ChuNamThoMoc", 16.0f, 20.0f, -120f, 5.0f),
                new HouseLayoutData("House_09_DiHaiGieng", 16.0f, 8.0f, -60f, 5.0f),
                new HouseLayoutData("House_05_AnhBayDonCa", 24.0f, 14.0f, -90f, 5.0f),

                // South Waterfront (Xóm Bến Sông & Bờ Kênh Cà Mau)
                new HouseLayoutData("House_10_ChuBayCheoDo", -13.0f, -1.0f, 0f, 5.0f),
                new HouseLayoutData("House_11_XomKenhDong", -1.0f, -1.0f, 0f, 5.0f)
            };

            var fbxMf = fbxThatched.GetComponentInChildren<MeshFilter>();
            Mesh cottageMesh = (fbxMf != null) ? fbxMf.sharedMesh : null;

            foreach (var h in houses)
            {
                var inst = Object.Instantiate(fbxThatched, housesRoot.transform);
                inst.name = h.name;

                float yPos = UNIFORM_GROUND_Y + (MESH_BOTTOM_OFFSET * h.scale);
                inst.transform.position = new Vector3(h.posXZ.x, yPos, h.posXZ.y);
                inst.transform.rotation = Quaternion.Euler(0f, h.rotationY, 0f);
                inst.transform.localScale = Vector3.one * h.scale;

                if (matThatched != null)
                {
                    foreach (var r in inst.GetComponentsInChildren<Renderer>())
                    {
                        r.sharedMaterial = matThatched;
                    }
                }

                // Clean up any old colliders
                var existingCols = inst.GetComponentsInChildren<Collider>();
                foreach (var c in existingCols) Object.DestroyImmediate(c);

                // Exact 1:1 BoxCollider matching house dimensions without exceeding house boundaries
                var box = inst.AddComponent<BoxCollider>();
                box.center = new Vector3(0f, 0f, 0f);
                box.size = new Vector3(1.80f, 0.80f, 1.25f);

                EditorUtility.SetDirty(inst);
            }

            Debug.Log($"[ReplaceHouses] Đã bố trí 12 ngôi nhà tranh đối xứng hài hòa quanh sân cây đa trung tâm!");
        }

        // =========================================================================
        // 4. UNIFIED CONTINUOUS VILLAGE EARTHEN GROUND (No jagged cuts, full cohesion)
        // =========================================================================
        public static void BuildUnifiedEarthenGroundNetwork()
        {
            var roadMat = AssetDatabase.LoadAssetAtPath<Material>(RoadMatPath);
            if (roadMat != null)
            {
                if (roadMat.HasProperty("_BaseColor")) roadMat.SetColor("_BaseColor", Color.white);
                roadMat.mainTextureScale = Vector2.one;
                roadMat.SetFloat("_Cull", 0f);
                EditorUtility.SetDirty(roadMat);
            }

            var pathsRoot = GameObject.Find("CaMau_1954_Village/Paths_3D_Network") ?? GameObject.Find("Paths_3D_Network");
            if (pathsRoot == null) pathsRoot = new GameObject("Paths_3D_Network");

            var oldUnified = GameObject.Find("Unified_Village_Earthen_Ground_Network");
            if (oldUnified != null) Object.DestroyImmediate(oldUnified);

            for (int i = pathsRoot.transform.childCount - 1; i >= 0; i--)
            {
                var c = pathsRoot.transform.GetChild(i);
                Object.DestroyImmediate(c.gameObject);
            }

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

            const float groundY = 0.508f;

            // 1. North Gate Arrival Apron (Sân đón Cổng Bắc)
            AddGrid(verts, uvs, normals, tris, -12.0f, -2.0f, 180.0f, 188.0f, 4, 3, groundY);

            // 2. Central Avenue through Rice Fields: Straight along ROAD_CENTER_X (-7.0f)
            // Width = 7.0m (from X = -10.5m to -3.5m)
            // Runs from Z = 184m down to Z = 32m
            int aveSegments = 40;
            float startZ = 184.0f;
            float endZ = 32.0f;
            for (int i = 0; i < aveSegments; i++)
            {
                float t0 = (float)i / aveSegments;
                float t1 = (float)(i + 1) / aveSegments;

                float z0 = Mathf.Lerp(startZ, endZ, t0);
                float z1 = Mathf.Lerp(startZ, endZ, t1);

                // Flaring slightly as it enters the village (from 3.5m half-width to 4.5m)
                float hw0 = Mathf.Lerp(3.5f, 4.5f, Mathf.Clamp01((t0 - 0.7f) / 0.3f));
                float hw1 = Mathf.Lerp(3.5f, 4.5f, Mathf.Clamp01((t1 - 0.7f) / 0.3f));

                Vector3 p0 = new Vector3(ROAD_CENTER_X - hw0, groundY, z0);
                Vector3 p1 = new Vector3(ROAD_CENTER_X + hw0, groundY, z0);
                Vector3 p2 = new Vector3(ROAD_CENTER_X + hw1, groundY, z1);
                Vector3 p3 = new Vector3(ROAD_CENTER_X - hw1, groundY, z1);

                AddQuad(verts, uvs, normals, tris, p0, p1, p2, p3);
            }

            // 3. North Village Entrance Flare & Connecting Crossway
            // Seamlessly connects the Avenue (Z=32) down to the North Row houses & square (Z=24)
            // Spans X in [-20.0m, +8.0m], Z in [24.0m, 34.0m]
            AddGrid(verts, uvs, normals, tris, -20.0f, 8.0f, 24.0f, 34.0f, 14, 4, groundY);

            // 4. THE GRAND CONTINUOUS VILLAGE LIVING FLOOR (Zero rectangular holes, completely unified)
            // Spans across the entire village living core:
            // X in [-40.0m, +28.0m], Z in [4.0m, 25.0m]
            // This covers the Central Banyan Plaza, West Hamlet, and East Hamlet as ONE continuous solid earth mesh!
            AddGrid(verts, uvs, normals, tris, -40.0f, 28.0f, 4.0f, 25.0f, 28, 12, groundY);

            // 5. South Central Thoroughfare (Đại lộ kết nối cây đa trung tâm xuống bến sông)
            // Spans X in [-16.0m, +2.0m], Z in [-3.0m, 5.0m]
            AddGrid(verts, uvs, normals, tris, -16.0f, 2.0f, -3.0f, 5.0f, 8, 4, groundY);

            // 6. South Waterfront Promenade (Bờ kênh & bến đò sông Cà Mau)
            // Spans X in [-26.0m, +14.0m], Z in [-8.0m, -1.0m]
            // Gently slopes toward river surface at Z = -8m
            int waterStepsZ = 4;
            int waterStepsX = 16;
            for (int z = 0; z < waterStepsZ; z++)
            {
                float tz0 = (float)z / waterStepsZ;
                float tz1 = (float)(z + 1) / waterStepsZ;
                float z0 = Mathf.Lerp(-1.0f, -8.0f, tz0);
                float z1 = Mathf.Lerp(-1.0f, -8.0f, tz1);
                float y0 = Mathf.Lerp(groundY, 0.20f, tz0);
                float y1 = Mathf.Lerp(groundY, 0.20f, tz1);

                for (int x = 0; x < waterStepsX; x++)
                {
                    float tx0 = (float)x / waterStepsX;
                    float tx1 = (float)(x + 1) / waterStepsX;
                    float x0 = Mathf.Lerp(-26.0f, 14.0f, tx0);
                    float x1 = Mathf.Lerp(-26.0f, 14.0f, tx1);

                    Vector3 p0 = new Vector3(x0, y0, z0);
                    Vector3 p1 = new Vector3(x1, y0, z0);
                    Vector3 p2 = new Vector3(x1, y1, z1);
                    Vector3 p3 = new Vector3(x0, y1, z1);

                    AddQuad(verts, uvs, normals, tris, p0, p1, p2, p3);
                }
            }

            Mesh unifiedMesh = new Mesh();
            unifiedMesh.name = "M_Unified_Village_Earthen_Ground_Seamless";
            unifiedMesh.SetVertices(verts);
            unifiedMesh.SetUVs(0, uvs);
            unifiedMesh.SetNormals(normals);
            unifiedMesh.SetTriangles(tris, 0);
            unifiedMesh.RecalculateBounds();

            mf.sharedMesh = unifiedMesh;
            mr.enabled = true;

            EditorUtility.SetDirty(unifiedObj);
            Debug.Log("[UnifiedGround] Đã tạo thành công nền đất nâu thôn quê LIỀN MẠCH 100% không còn vết cắt hay lỗ thủng cỏ!");
        }

        private static void AddGrid(List<Vector3> verts, List<Vector2> uvs, List<Vector3> normals, List<int> tris,
            float minX, float maxX, float minZ, float maxZ, int segsX, int segsZ, float y)
        {
            int baseIdx = verts.Count;
            for (int z = 0; z <= segsZ; z++)
            {
                float tz = (float)z / segsZ;
                float posZ = Mathf.Lerp(minZ, maxZ, tz);

                for (int x = 0; x <= segsX; x++)
                {
                    float tx = (float)x / segsX;
                    float posX = Mathf.Lerp(minX, maxX, tx);

                    verts.Add(new Vector3(posX, y, posZ));
                    normals.Add(Vector3.up);
                    uvs.Add(new Vector2(posX * 0.40f, posZ * 0.40f));
                }
            }

            for (int z = 0; z < segsZ; z++)
            {
                for (int x = 0; x < segsX; x++)
                {
                    int botLeft  = baseIdx + z * (segsX + 1) + x;
                    int botRight = botLeft + 1;
                    int topLeft  = baseIdx + (z + 1) * (segsX + 1) + x;
                    int topRight = topLeft + 1;

                    tris.Add(topLeft);
                    tris.Add(topRight);
                    tris.Add(botLeft);

                    tris.Add(topRight);
                    tris.Add(botRight);
                    tris.Add(botLeft);
                }
            }
        }

        private static void AddQuad(List<Vector3> verts, List<Vector2> uvs, List<Vector3> normals, List<int> tris,
            Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            int baseIdx = verts.Count;
            verts.Add(p0);
            verts.Add(p1);
            verts.Add(p2);
            verts.Add(p3);

            normals.Add(Vector3.up);
            normals.Add(Vector3.up);
            normals.Add(Vector3.up);
            normals.Add(Vector3.up);

            uvs.Add(new Vector2(p0.x * 0.40f, p0.z * 0.40f));
            uvs.Add(new Vector2(p1.x * 0.40f, p1.z * 0.40f));
            uvs.Add(new Vector2(p2.x * 0.40f, p2.z * 0.40f));
            uvs.Add(new Vector2(p3.x * 0.40f, p3.z * 0.40f));

            tris.Add(baseIdx);
            tris.Add(baseIdx + 1);
            tris.Add(baseIdx + 2);

            tris.Add(baseIdx);
            tris.Add(baseIdx + 2);
            tris.Add(baseIdx + 3);
        }

        // =========================================================================
        // 5. SETUP 100% STANDING UPRIGHT NPCS SYNCHRONIZED WITH THE NEW HOUSES & PLAZA
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

            // 1. TRƯỞNG THÔN NĂM (Đứng trước sân Nhà Trưởng Thôn 02)
            BuildAccurateNPC(npcsRoot.transform, pElder, cElder, "NPC_01_TruongThonNam",
                new Vector3(-13.0f, groundY, 21.0f), Quaternion.Euler(0f, 180f, 0f),
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

            // 2. MÁ CỦA AN (Đứng trước sân Nhà An 01)
            BuildAccurateNPC(npcsRoot.transform, pMother, cFemale, "NPC_02_MaAnMother",
                new Vector3(-1.0f, groundY, 21.0f), Quaternion.Euler(0f, 180f, 0f),
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

            // 3. BÁC BA PHI (Đứng trước sân Nhà Bác Ba Phi 03)
            BuildAccurateNPC(npcsRoot.transform, pElder, cElder, "NPC_03_BacBaPhi",
                new Vector3(6.5f, groundY, 14.0f), Quaternion.Euler(0f, -90f, 0f),
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

            // 4. CHÚ BẢY CHÈO ĐÒ (Đứng bến sông Nhà 10)
            BuildAccurateNPC(npcsRoot.transform, pMale05, cMale, "NPC_04_ChuBayCheoDo",
                new Vector3(-13.0f, groundY, -3.0f), Quaternion.Euler(0f, 0f, 0f),
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

            // 5. THẰNG MINH (Đứng giữa sân Cây Đa Trung Tâm)
            BuildAccurateNPC(npcsRoot.transform, pMale01, cSocial, "NPC_05_ThangMinh",
                new Vector3(-6.0f, groundY, 8.5f), Quaternion.Euler(0f, 0f, 0f),
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

            // 6. CHỊ TƯ GÁNH NƯỚC (Đứng bên giếng ngọt Nhà 09)
            BuildAccurateNPC(npcsRoot.transform, pFemale, cFemale, "NPC_06_ChiTuGanhNuoc",
                new Vector3(13.5f, groundY, 8.0f), Quaternion.Euler(0f, -60f, 0f),
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

            // 7. CHÚ SÁU LÀM VƯỜN (Đứng xóm vườn chuối Nhà 08)
            BuildAccurateNPC(npcsRoot.transform, pMale04, cMale, "NPC_07_ChuSauLamVuon",
                new Vector3(-24.5f, groundY, 20.0f), Quaternion.Euler(0f, 120f, 0f),
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

            // 8. BÉ TÈO (Đứng xóm trong phía Tây Nhà 12)
            BuildAccurateNPC(npcsRoot.transform, pMale04, cSocial, "NPC_08_BeTeo",
                new Vector3(-32.5f, groundY, 14.0f), Quaternion.Euler(0f, 90f, 0f),
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

            // 9. BÉ MẬN (Đứng gánh bánh bò Nhà 07)
            BuildAccurateNPC(npcsRoot.transform, pChildGirl, cSocial, "NPC_09_BeMan",
                new Vector3(-17.0f, groundY, 14.0f), Quaternion.Euler(0f, 90f, 0f),
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

            // 10. BÉ ÚT (Đứng gần mẹ Má An)
            BuildAccurateNPC(npcsRoot.transform, pChildGirl, cSocial, "NPC_10_BeUt",
                new Vector3(1.0f, groundY, 20.0f), Quaternion.Euler(0f, 180f, 0f),
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

            // 11. DÌ HAI (Đứng bờ kênh Nhà 11)
            BuildAccurateNPC(npcsRoot.transform, pFemale, cFemale, "NPC_11_DiHai",
                new Vector3(-1.0f, groundY, -3.0f), Quaternion.Euler(0f, 0f, 0f),
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

            // 12. CỤ BẢY GIÀ (Ngồi/đứng dưới bóng Cây Đa Trung Tâm)
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

            SampleIdlePosesForAllNPCs(npcsRoot.transform);

            Debug.Log("[SetupNPCs] Đã khởi tạo hoàn tất 12 NPC đứng vững chãi, khớp 100% với từng ngôi nhà và quảng trường!");
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

            var anim = inst.GetComponent<Animator>();
            if (anim == null) anim = inst.AddComponent<Animator>();
            if (animCtrl != null) anim.runtimeAnimatorController = animCtrl;
            anim.applyRootMotion = false;

            var col = inst.GetComponent<CapsuleCollider>();
            if (col == null) col = inst.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.85f * scale, 0f);
            col.radius = 0.35f * scale;
            col.height = 1.70f * scale;

            var interactable = inst.GetComponent<GenshinNPCInteractable>();
            if (interactable == null) interactable = inst.AddComponent<GenshinNPCInteractable>();
            interactable.Configure(name, title, openings, choices, defAnim, talkAnim);

            EditorUtility.SetDirty(inst);
        }

        // =========================================================================
        // 6. OVERHAUL ALL TREES: 8-DIRECTION 3D VOLUMETRIC, FIRMLY ROOTED, SYMMETRICAL FLANKS
        // =========================================================================
        public static void OverhaulAllTrees8DirectionsAndDenseFlanks()
        {
            var veg = GameObject.Find("CaMau_1954_Village/Environment/Vegetation_3D");
            if (veg == null) return;

            // Remove any legacy visible stakes
            var allChildren = veg.GetComponentsInChildren<Transform>(true);
            foreach (var tr in allChildren)
            {
                if (tr != null && (tr.name == "Trunk_3D_SolidCore" || tr.name.Contains("SolidCore")))
                {
                    Object.DestroyImmediate(tr.gameObject);
                }
            }

            var oldFlanks = veg.transform.Find("Dense_Paddy_Flanking_Orchards");
            if (oldFlanks != null) Object.DestroyImmediate(oldFlanks.gameObject);

            var denseFlanks = new GameObject("Dense_Paddy_Flanking_Orchards");
            denseFlanks.transform.SetParent(veg.transform, false);

            string[] treeSpritePaths = new string[]
            {
                "Assets/Textures/Environment/Lana_VN_Pack/Trees/T_LanaVN_Tree_01_LargeCanopy.png",
                "Assets/Textures/Environment/Lana_VN_Pack/Trees/T_LanaVN_Tree_02_Medium.png",
                "Assets/Textures/Environment/Lana_VN_Pack/Trees/T_LanaVN_Tree_03_SmallBushTree.png",
                "Assets/Textures/Environment/Lana_VN_Pack/Trees/T_LanaVN_Tree_04_SlenderTall.png",
                "Assets/Textures/Environment/Lana_VN_Pack/Trees/T_LanaVN_Tree_05_Banana.png",
                "Assets/Textures/Environment/Lana_VN_Pack/Trees/T_LanaVN_Tree_06_Coconut.png",
                "Assets/Textures/Environment/Lana_VN_Pack/Trees/T_LanaVN_Tree_07_BambooGrove.png"
            };

            var loadedSprites = new Sprite[treeSpritePaths.Length];
            for (int i = 0; i < treeSpritePaths.Length; i++)
            {
                loadedSprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(treeSpritePaths[i]);
            }

            int spawnedCount = 0;

            // West Flank Belt: Symmetrical distance from ROAD_CENTER_X (-7.0f)
            // Inner row: 42m away -> X = -49.0f
            // Mid row:   54m away -> X = -61.0f
            // Outer row: 66m away -> X = -73.0f
            for (float z = 36f; z <= 182f; z += 8.0f)
            {
                float xInner = -49f - Mathf.Sin(z * 0.22f) * 2.0f;
                int[] innerPool = new int[] { 1, 4, 5, 2 };
                int innerIdx = ((int)(z / 8.0f)) % innerPool.Length;
                SpawnFlankTree(denseFlanks.transform, loadedSprites[innerPool[innerIdx]],
                    new Vector3(xInner, 0.505f, z), Random.Range(1.10f, 1.25f), $"Tree_Flank_West_Inner_{z:F0}");
                spawnedCount++;

                float xMid = -61f + Mathf.Cos(z * 0.17f) * 2.5f;
                int[] midPool = new int[] { 3, 6, 1, 0 };
                int midIdx = ((int)(z / 8.0f) + 1) % midPool.Length;
                SpawnFlankTree(denseFlanks.transform, loadedSprites[midPool[midIdx]],
                    new Vector3(xMid, 0.505f, z + 4.0f), Random.Range(1.20f, 1.35f), $"Tree_Flank_West_Mid_{z:F0}");
                spawnedCount++;

                float xOuter = -73f - Mathf.Sin(z * 0.14f) * 2.5f;
                int[] outerPool = new int[] { 0, 6, 5, 3 };
                int outerIdx = ((int)(z / 8.0f) + 2) % outerPool.Length;
                SpawnFlankTree(denseFlanks.transform, loadedSprites[outerPool[outerIdx]],
                    new Vector3(xOuter, 0.505f, z), Random.Range(1.30f, 1.50f), $"Tree_Flank_West_Outer_{z:F0}");
                spawnedCount++;
            }

            // East Flank Belt: Exact Symmetrical distance from ROAD_CENTER_X (-7.0f)
            // Inner row: 42m away -> X = +35.0f
            // Mid row:   54m away -> X = +47.0f
            // Outer row: 66m away -> X = +59.0f
            for (float z = 36f; z <= 182f; z += 8.0f)
            {
                float xInner = 35f + Mathf.Sin(z * 0.22f) * 2.0f;
                int[] innerPool = new int[] { 1, 4, 5, 2 };
                int innerIdx = ((int)(z / 8.0f)) % innerPool.Length;
                SpawnFlankTree(denseFlanks.transform, loadedSprites[innerPool[innerIdx]],
                    new Vector3(xInner, 0.505f, z), Random.Range(1.10f, 1.25f), $"Tree_Flank_East_Inner_{z:F0}");
                spawnedCount++;

                float xMid = 47f + Mathf.Cos(z * 0.17f) * 2.5f;
                int[] midPool = new int[] { 3, 6, 1, 0 };
                int midIdx = ((int)(z / 8.0f) + 1) % midPool.Length;
                SpawnFlankTree(denseFlanks.transform, loadedSprites[midPool[midIdx]],
                    new Vector3(xMid, 0.505f, z + 4.0f), Random.Range(1.20f, 1.35f), $"Tree_Flank_East_Mid_{z:F0}");
                spawnedCount++;

                float xOuter = 59f + Mathf.Sin(z * 0.14f) * 2.5f;
                int[] outerPool = new int[] { 0, 6, 5, 3 };
                int outerIdx = ((int)(z / 8.0f) + 2) % outerPool.Length;
                SpawnFlankTree(denseFlanks.transform, loadedSprites[outerPool[outerIdx]],
                    new Vector3(xOuter, 0.505f, z), Random.Range(1.30f, 1.50f), $"Tree_Flank_East_Outer_{z:F0}");
                spawnedCount++;
            }

            // Process all trees in hierarchy: 8 directions and firm root planting
            int processedCount = 0;
            ProcessAllTreesInHierarchy(veg.transform, ref processedCount);

            Debug.Log($"[TreeOverhaul] Đã trồng {spawnedCount} cây dải rừng đối xứng và nâng cấp {processedCount} cây trên map 8 hướng 3D volumetric cắm rễ vào đất!");
        }

        private static void SpawnFlankTree(Transform parent, Sprite sprite, Vector3 pos, float scale, string name)
        {
            if (sprite == null) return;

            var treeObj = new GameObject(name);
            treeObj.transform.SetParent(parent, false);
            treeObj.transform.position = pos;
            treeObj.transform.localScale = Vector3.one * scale;

            var sr = treeObj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 0;
        }

        private static void ProcessAllTreesInHierarchy(Transform parent, ref int count)
        {
            foreach (Transform child in parent)
            {
                if (child.name == "Dense_Paddy_Flanking_Orchards")
                {
                    foreach (Transform flankTree in child)
                    {
                        ProcessSingleTreeTo8DirectionsAndPlantRoots(flankTree);
                        count++;
                    }
                }
                else if (child.name != "Tree_Central_StoneRing" && child.name != "Road_Organic_Flora_Root")
                {
                    ProcessSingleTreeTo8DirectionsAndPlantRoots(child);
                    count++;
                }
            }
        }

        private static void ProcessSingleTreeTo8DirectionsAndPlantRoots(Transform t)
        {
            if (t.name == "Tree_Central_StoneRing" || t.name == "Road_Organic_Flora_Root" || t.name == "Dense_Paddy_Flanking_Orchards")
                return;

            var sr = t.GetComponent<SpriteRenderer>();
            Sprite treeSprite = null;
            Material treeMat = null;

            if (sr != null && sr.sprite != null)
            {
                treeSprite = sr.sprite;
                treeMat = sr.sharedMaterial;
            }
            else
            {
                var childSRs = t.GetComponentsInChildren<SpriteRenderer>(true);
                foreach (var csr in childSRs)
                {
                    if (csr.sprite != null)
                    {
                        treeSprite = csr.sprite;
                        treeMat = csr.sharedMaterial;
                        break;
                    }
                }
            }

            if (treeSprite == null) return;

            for (int i = t.childCount - 1; i >= 0; i--)
            {
                var child = t.GetChild(i);
                if (child.name.Contains("Card") || child.name.Contains("Canopy") || child.name.Contains("Dome") ||
                    child.name.Contains("Fork") || child.name.Contains("Palm") || child.name.Contains("Bamboo") ||
                    child.name.Contains("Banana") || child.name.Contains("SolidCore"))
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }

            if (sr == null) sr = t.gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = treeSprite;
            if (treeMat != null) sr.sharedMaterial = treeMat;
            sr.color = Color.white;
            sr.sortingOrder = 0;

            float groundY = GetGroundSurfaceY(t.position.x, t.position.z);
            float scaleY = t.localScale.y;

            string lowName = t.name.ToLower();
            float targetPosY;

            if (lowName.Contains("banyan") || lowName.Contains("ghibli") || lowName.Contains("sacred"))
            {
                targetPosY = groundY - 0.20f;
            }
            else
            {
                float targetLowestRootWorldY = groundY - 0.32f;
                targetPosY = targetLowestRootWorldY - (treeSprite.bounds.min.y * scaleY);
            }

            t.position = new Vector3(t.position.x, targetPosY, t.position.z);

            // 7 Radial Cards (22.5, 45, 67.5, 90, 112.5, 135, 157.5 degrees)
            float[] angles = new float[] { 22.5f, 45.0f, 67.5f, 90.0f, 112.5f, 135.0f, 157.5f };
            Color[] shadings = new Color[]
            {
                new Color(0.97f, 0.97f, 0.97f, 1f),
                new Color(0.93f, 0.94f, 0.92f, 1f),
                new Color(0.90f, 0.91f, 0.89f, 1f),
                new Color(0.87f, 0.88f, 0.86f, 1f),
                new Color(0.91f, 0.92f, 0.90f, 1f),
                new Color(0.94f, 0.95f, 0.93f, 1f),
                new Color(0.98f, 0.98f, 0.97f, 1f)
            };

            for (int k = 0; k < angles.Length; k++)
            {
                var cardObj = new GameObject($"Card_Volumetric_{angles[k]:F1}Deg");
                cardObj.transform.SetParent(t, false);
                cardObj.transform.localPosition = Vector3.zero;
                cardObj.transform.localRotation = Quaternion.Euler(0f, angles[k], 0f);
                cardObj.transform.localScale = Vector3.one;

                var cardSR = cardObj.AddComponent<SpriteRenderer>();
                cardSR.sprite = treeSprite;
                if (treeMat != null) cardSR.sharedMaterial = treeMat;
                cardSR.color = shadings[k];
                cardSR.sortingOrder = 0;
            }

            var col = t.GetComponent<CapsuleCollider>();
            if (col == null) col = t.gameObject.AddComponent<CapsuleCollider>();

            if (lowName.Contains("central") || lowName.Contains("sacred") || lowName.Contains("banyan"))
            {
                col.center = new Vector3(0f, 2.5f, 0f);
                col.radius = 1.8f;
                col.height = 6.0f;
            }
            else if (lowName.Contains("coconut") || lowName.Contains("palm"))
            {
                col.center = new Vector3(0f, 3.2f, 0f);
                col.radius = 0.45f;
                col.height = 7.5f;
            }
            else if (lowName.Contains("banana"))
            {
                col.center = new Vector3(0f, 2.0f, 0f);
                col.radius = 0.70f;
                col.height = 4.5f;
            }
            else if (lowName.Contains("fruit") || lowName.Contains("canopy"))
            {
                col.center = new Vector3(0f, 2.5f, 0f);
                col.radius = 0.85f;
                col.height = 5.5f;
            }
            else if (lowName.Contains("bamboo"))
            {
                col.center = new Vector3(0f, 2.8f, 0f);
                col.radius = 0.75f;
                col.height = 6.5f;
            }
            else
            {
                col.center = new Vector3(0f, 2.2f, 0f);
                col.radius = 0.65f;
                col.height = 5.0f;
            }

            EditorUtility.SetDirty(t.gameObject);
        }

        private static float GetGroundSurfaceY(float x, float z)
        {
            if (z >= -3.0f) return UNIFORM_GROUND_Y;
            float t = Mathf.Clamp01((-3.0f - z) / 5.5f);
            return Mathf.Lerp(UNIFORM_GROUND_Y, 0.15f, t);
        }

        // =========================================================================
        // 7. CAPTURE QA SNAPSHOTS (Rule 4.2: 720p)
        // =========================================================================
        public static void CaptureQASnapshots()
        {
            if (!Directory.Exists(ArtifactDir)) Directory.CreateDirectory(ArtifactDir);

            int width = 1280;
            int height = 720;

            // Shot 1: Bird's-eye overview of the beautiful, even village layout with continuous earthen ground
            RenderShot(
                new Vector3(-6.0f, 32.0f, -6.0f),
                Quaternion.Euler(55.0f, 0.0f, 0.0f),
                width, height, 64.0f,
                Path.Combine(ArtifactDir, "CaMau_VillageLayout_Harmonious_QA.jpg")
            );

            // Shot 2: View looking down North Avenue showing 100% symmetrical rice fields on West and East
            RenderShot(
                new Vector3(-7.0f, 24.0f, 185.0f),
                Quaternion.Euler(28.0f, 180.0f, 0.0f),
                width, height, 58.0f,
                Path.Combine(ArtifactDir, "CaMau_VastPaddy_Symmetrical_QA.jpg")
            );

            // Shot 3: View looking North toward the Gate and Mountains: verifying zero white seams and no sky gaps
            RenderShot(
                new Vector3(-7.0f, 6.0f, 140.0f),
                Quaternion.Euler(10.0f, 0.0f, 0.0f),
                width, height, 54.0f,
                Path.Combine(ArtifactDir, "CaMau_NorthGate_Mountain_NoSeams_QA.jpg")
            );

            // Shot 4: Close perspective of the paddy southern boundary: verifying even rows on both sides
            RenderShot(
                new Vector3(-7.0f, 14.0f, 60.0f),
                Quaternion.Euler(42.0f, 180.0f, 0.0f),
                width, height, 50.0f,
                Path.Combine(ArtifactDir, "CaMau_PaddyBoundary_Even_QA.jpg")
            );

            // Shot 5: Central Banyan Plaza eye-level view: spacious continuous earthen square with Cottages and NPCs
            RenderShot(
                new Vector3(-6.0f, 2.2f, 9.0f),
                Quaternion.Euler(6.0f, 25.0f, 0.0f),
                width, height, 56.0f,
                Path.Combine(ArtifactDir, "CaMau_CentralBanyan_VillageSquare_QA.jpg")
            );

            Debug.Log("[QA] Đã chụp và lưu thành công 5 ảnh QA vào artifact directory!");
        }

        private static void RenderShot(Vector3 pos, Quaternion rot, int w, int h, float fov, string outPath)
        {
            var camObj = new GameObject("Temp_QA_Camera");
            var cam = camObj.AddComponent<Camera>();
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.2f;
            cam.farClipPlane = 400f;
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
