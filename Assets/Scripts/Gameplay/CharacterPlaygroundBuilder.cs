using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
#endif

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Master Level Builder for the Planet of Lana-style 2.5D Character Playground.
    /// Builds a focused, cinematic test gym dedicated 100% to character mechanics:
    /// - Station 1: Walk, Sprint (Shift / Analog) & Variable Jump Runway
    /// - Station 2: Multi-Tier Ledge Grab & Biomechanical Climb
    /// - Station 3: Low Cave Tunnel (Crouch & Crawl with Ceiling Clearance Check)
    /// - Station 4: Push / Pull Heavy Crate (Hold Left Click + A/D, climb on crate to reach high cliff)
    /// - Station 5: Verlet Rope Chasm (Climb Up/Down, Swing Momentum Pumping, Launch Dismount)
    /// - Station 6: Pick Up & Parabolic Throw Sanctuary (E to pick up, Right Click aim trajectory near/far)
    /// </summary>
    public class CharacterPlaygroundBuilder : MonoBehaviour
    {
#if UNITY_EDITOR
        private const string SceneAssetPath = "Assets/Scenes/Playground_Mechanics.unity";

        [MenuItem("Hoa va Mau/Build Character Playground Level")]
        public static void BuildMenuCommand()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(scene.path) || scene.path != SceneAssetPath)
            {
                scene = EditorSceneManager.OpenScene(SceneAssetPath, OpenSceneMode.Single);
            }
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);

            // Clean previous objects
            PurgeSceneClutter();

            // Build fresh playground
            GameObject builderGO = new GameObject("Temp_PlaygroundBuilder");
            var builder = builderGO.AddComponent<CharacterPlaygroundBuilder>();
            try
            {
                builder.BuildPlayground();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, SceneAssetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                Debug.Log("<color=#40b8a6>[Character Playground]</color> Built successfully and saved to " + SceneAssetPath);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("<color=#ef4444>[Character Playground] Error building playground: " + ex + "</color>");
            }
            finally
            {
                DestroyImmediate(builderGO);
            }
        }

        private static void PurgeSceneClutter()
        {
            var scene = EditorSceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                if (root == null) continue;
                // Preserve camera, lights, volume, Player_An, and existing project assets
                if (root.name == "Main Camera" || root.name == "Directional Light" || root.name == "Global Volume") continue;
                if (root.name.Contains("Player_An") || root.name.Contains("Environment") || root.name.Contains("Art") || root.name.Contains("Terrain")) continue;

                // Only clean previous temporary playground root
                if (root.name.StartsWith("--- Character Playground"))
                {
                    DestroyImmediate(root);
                }
            }
        }

        public void BuildPlayground()
        {
            GameObject root = new GameObject("--- Character Playground (Planet of Lana Style) ---");

            // Palette Materials (Saved as persistent Unity Assets)
            Material groundMat = GetOrCreateMaterial("M_Playground_Ground", new Color(0.24f, 0.48f, 0.22f), 0.12f); // Lush Green Meadow
            Material cliffMat = GetOrCreateMaterial("M_Playground_Cliff", new Color(0.42f, 0.45f, 0.49f), 0.08f); // Cool Slate Grey Rock
            Material woodMat = GetOrCreateMaterial("M_Playground_Timber", new Color(0.50f, 0.32f, 0.18f), 0.20f); // Warm Timber
            Material crateMat = GetOrCreateMaterial("M_Playground_Crate", new Color(0.72f, 0.48f, 0.22f), 0.25f); // Honey Wood Box
            Material metalMat = GetOrCreateMaterial("M_Playground_Iron", new Color(0.18f, 0.20f, 0.24f), 0.65f); // Cast Iron Bands
            Material playerMat = GetOrCreateMaterial("M_Playground_PlayerTorso", new Color(0.12f, 0.42f, 0.78f), 0.20f); // Indigo Protagonist Tunic
            Material skinMat = GetOrCreateMaterial("M_Playground_Skin", new Color(0.92f, 0.75f, 0.60f), 0.10f); // Warm Skin
            Material headbandMat = GetOrCreateMaterial("M_Playground_Headband", new Color(0.88f, 0.28f, 0.18f), 0.10f); // Red Headband
            Material orbMat = GetOrCreateGlowingMaterial("M_Playground_Orb", new Color(0.15f, 0.90f, 1.0f)); // Radiant Cyan
            Material altarMat = GetOrCreateMaterial("M_Playground_Altar", new Color(0.28f, 0.32f, 0.38f), 0.30f);

            // Lighting, Atmosphere & Simulation Managers
            SetupLightingAndAtmosphere(root.transform);
            SetupManagers(root.transform);

            // STATION 1: SPRINT & JUMP RUNWAY (X = -23 to -6)
            // Extra thick (4.0m deep) platform base so falling through is physically impossible
            BuildPlatform(root.transform, "Runway_Main", new Vector3(-13f, -2.0f, 0f), new Vector3(20f, 4.0f, 4f), groundMat);
            BuildPlatform(root.transform, "Runway_StepHurdle", new Vector3(-8f, 0.45f, 0f), new Vector3(1.6f, 0.9f, 3.5f), cliffMat);
            BuildSign(root.transform, "Sign_Station1", new Vector3(-21f, 2.8f, 0f), "TRẠM 1: CHẠY & NHẢY\n[A]/[D] Đi | [Shift] Chạy | [Space] Nhảy\nChạy lấy đà + [Space] phóng xa vượt trội!");
            BuildGrassPatch(root.transform, "Runway_L", -21f, -9.5f, 0.0f);
            BuildGrassPatch(root.transform, "Runway_R", -6.5f, -4.0f, 0.0f);
            BuildCheckpoint(root.transform, "CP_Station1", new Vector3(-17.5f, 0.05f, 0f), 0);

            // KHÚC DÂY TEST NGAY RUNWAY (Test Rope Segment)
            BuildPlatform(root.transform, "TestRope_OverheadBeam", new Vector3(-11.5f, 6.2f, 0f), new Vector3(4.2f, 0.35f, 1.2f), woodMat);
            BuildPlatform(root.transform, "TestRope_Pillar_L", new Vector3(-13.4f, 3.1f, 0.4f), new Vector3(0.35f, 6.0f, 0.4f), woodMat);
            BuildPlatform(root.transform, "TestRope_Pillar_R", new Vector3(-9.6f, 3.1f, 0.4f), new Vector3(0.35f, 6.0f, 0.4f), woodMat);
            BuildVerletRope(root.transform, new Vector3(-11.5f, 6.0f, 0f), 4.2f);
            BuildSign(root.transform, "Sign_Rope_Test", new Vector3(-13.0f, 2.6f, 0f), "KHÚC DÂY TEST\nNhảy vào bám | [W]/[S] leo\n[A]/[D] đu lấy đà | [Space] phóng");

            // BÓNG TEST & CANH NÉM PARABOL (Test Throwable Balls)
            BuildThrowableItem(root.transform, "TestBall_Runway_1", new Vector3(-15.6f, 0.35f, 0f), orbMat);
            BuildThrowableItem(root.transform, "TestBall_Runway_2", new Vector3(-14.8f, 0.35f, 0f), orbMat);
            BuildSign(root.transform, "Sign_Ball_Test", new Vector3(-16.8f, 2.0f, 0f), "BÓNG TEST & CANH NÉM\n[E] Nhặt | Giữ [Chuột Trái] canh góc & lực (Parabol)\nThả để Ném | [Chuột Phải] đặt xuống");

            // THÙNG TEST (KÉO / ĐẨY & PHÁ VỠ PHYSX)
            BuildPushPullCrate(root.transform, new Vector3(-6.2f, 0.0f, 0f), crateMat, metalMat);
            BuildSign(root.transform, "Sign_Crate_Test", new Vector3(-7.2f, 2.4f, 0f), "THÙNG TEST\nGiữ [Chuột Trái] + [A]/[D] kéo/đẩy\nNém bóng trúng 3 lần -> Nứt & nổ vỡ mảnh PhysX!");

            // STATION 2: MULTI-TIER LEDGE CLIMB (X = -5 to 8)
            BuildLedgeCliff(root.transform, "Ledge_Tier1", new Vector3(-1f, -1.1f, 0f), new Vector3(5f, 5.8f, 3.5f), cliffMat, groundMat);
            BuildLedgeCliff(root.transform, "Ledge_Tier2", new Vector3(5.5f, -0.1f, 0f), new Vector3(6f, 7.8f, 3.5f), cliffMat, groundMat);
            BuildSign(root.transform, "Sign_Station2", new Vector3(-4.5f, 2.6f, 0f), "TRẠM 2: BÁM MÉP ĐỊA HÌNH\nNhảy bắt mép vách -> [W] trèo lên, [S] buông tay");
            BuildGrassPatch(root.transform, "Ledge_T1", -2.8f, 0.8f, 1.8f);
            BuildGrassPatch(root.transform, "Ledge_T2", 3.2f, 7.8f, 3.8f);
            BuildCheckpoint(root.transform, "CP_Station2", new Vector3(3.5f, 3.85f, 0f), 1);

            // STATION 3: LOW CAVE TUNNEL (Crouch & Crawl) (X = 8.5 to 21)
            BuildPlatform(root.transform, "Tunnel_Floor", new Vector3(14.5f, -0.1f, 0f), new Vector3(12f, 7.8f, 3.5f), groundMat);
            // Low Rock Ceiling (clearance = 0.85m)
            BuildPlatform(root.transform, "Tunnel_Ceiling", new Vector3(14.5f, 5.85f, 0f), new Vector3(8.0f, 2.4f, 3.5f), cliffMat);
            BuildPlatform(root.transform, "Tunnel_Arch_L", new Vector3(10.5f, 5.0f, 0.5f), new Vector3(1.0f, 3.5f, 2.2f), cliffMat);
            BuildPlatform(root.transform, "Tunnel_Arch_R", new Vector3(18.5f, 5.0f, 0.5f), new Vector3(1.0f, 3.5f, 2.2f), cliffMat);
            BuildSign(root.transform, "Sign_Station3", new Vector3(9.2f, 5.2f, 0f), "TRẠM 3: HẦM BÒ TRƯỜN\n[C]/[Ctrl] Ngồi | Tự động bò trườn qua khe thấp");
            BuildCheckpoint(root.transform, "CP_Station3", new Vector3(19.5f, 3.85f, 0f), 2);

            // STATION 4: PUSH & PULL CRATE PUZZLE (X = 21 to 37)
            BuildPlatform(root.transform, "CrateZone_Floor", new Vector3(28f, -2.0f, 0f), new Vector3(14f, 4.0f, 4f), groundMat);
            BuildLedgeCliff(root.transform, "CrateZone_HighCliff", new Vector3(36.5f, -0.2f, 0f), new Vector3(5f, 7.6f, 4f), cliffMat, groundMat);
            BuildPushPullCrate(root.transform, new Vector3(25.5f, 0.0f, 0f), crateMat, metalMat);
            BuildSign(root.transform, "Sign_Station4", new Vector3(22.5f, 2.6f, 0f), "TRẠM 4: KÉO & ĐẨY THÙNG (PLANET OF LANA)\nGiữ [Chuột Trái] + [A]/[D] để kéo/đẩy thùng sát vách rồi leo lên!");
            BuildGrassPatch(root.transform, "Crate_L", 21.5f, 23.8f, 0.0f);
            BuildGrassPatch(root.transform, "Crate_R", 30.5f, 33.2f, 0.0f);
            BuildCheckpoint(root.transform, "CP_Station4", new Vector3(36.5f, 3.65f, 0f), 3);

            // STATION 5: VERLET ROPE CHASM (X = 38 to 54)
            BuildPlatform(root.transform, "Rope_OverheadBeam", new Vector3(43.5f, 9.0f, 0f), new Vector3(10f, 0.45f, 1.5f), woodMat);
            BuildPlatform(root.transform, "Rope_SupportPillar_L", new Vector3(39.0f, 6.3f, 0.8f), new Vector3(0.5f, 5.5f, 0.5f), woodMat);
            BuildPlatform(root.transform, "Rope_SupportPillar_R", new Vector3(48.0f, 6.3f, 0.8f), new Vector3(0.5f, 5.5f, 0.5f), woodMat);
            BuildLedgeCliff(root.transform, "Rope_LandingCliff", new Vector3(52f, -0.2f, 0f), new Vector3(8f, 7.6f, 4f), cliffMat, groundMat);
            BuildVerletRope(root.transform, new Vector3(43.5f, 8.8f, 0f), 5.5f);
            BuildPitHazard(root.transform, "Chasm_PitHazard", new Vector3(43.5f, -2.5f, 0f), new Vector3(14f, 2.0f, 6f));
            BuildSign(root.transform, "Sign_Station5", new Vector3(37.5f, 5.2f, 0f), "TRẠM 5: LEO & ĐU DÂY THỪNG (QUÁN TÍNH)\nNhảy bám dây | [W]/[S] leo | [A]/[D] đu nhấp lấy đà | [Space] phóng xa");
            BuildGrassPatch(root.transform, "RopeLanding", 49.0f, 55.0f, 3.6f);
            BuildCheckpoint(root.transform, "CP_Station5", new Vector3(50.0f, 3.65f, 0f), 4);

            // STATION 6: PICK UP & THROW SANCTUARY (X = 55 to 70)
            BuildPlatform(root.transform, "Throw_Floor", new Vector3(63f, -0.2f, 0f), new Vector3(16f, 7.6f, 4f), groundMat);
            BuildPlatform(root.transform, "Throw_Pedestal", new Vector3(57f, 3.9f, 0f), new Vector3(1.4f, 0.6f, 1.0f), altarMat);
            BuildThrowableItem(root.transform, "PowerOrb_1", new Vector3(56.7f, 4.45f, 0f), orbMat);
            BuildThrowableItem(root.transform, "PowerOrb_2", new Vector3(57.3f, 4.45f, 0f), orbMat);
            BuildThrowableItem(root.transform, "PowerOrb_3", new Vector3(58.5f, 3.85f, 0f), orbMat);
            BuildPlatform(root.transform, "Throw_TargetPillar", new Vector3(67f, 3.6f, 0f), new Vector3(1.8f, 3.6f, 2.5f), cliffMat);
            BuildThrowTargetBasket(root.transform, new Vector3(67f, 5.45f, 0f), altarMat, metalMat);
            BuildSign(root.transform, "Sign_Station6", new Vector3(54.5f, 5.2f, 0f), "TRẠM 6: CẦM & NÉM ĐỒ VẬT\n[E] Nhặt / Thả đồ | Giữ [Chuột Trái] ngắm đường Parabol -> Nhả để Ném vào giỏ");
            BuildGrassPatch(root.transform, "ThrowFloor", 55.0f, 65.0f, 3.6f);
            BuildCheckpoint(root.transform, "CP_Station6", new Vector3(55.0f, 3.65f, 0f), 5);

            // SPAWN PROTAGONIST AN (Authentic Model & Animations)
            BuildPlayer(root.transform, new Vector3(-17.5f, 0.05f, 0f), orbMat);
        }

        private static void SetupManagers(Transform parent)
        {
            GameObject mgrs = new GameObject("Gameplay_Managers");
            mgrs.transform.SetParent(parent);

            var cpMgr = mgrs.AddComponent<CheckpointManager>();
            cpMgr.SetCheckpoint(new Vector3(-17.5f, 0.05f, 0f), 0);

            mgrs.AddComponent<WindManager>();
            mgrs.AddComponent<GrassManager>();
        }

        private static void BuildCheckpoint(Transform parent, string name, Vector3 pos, int index)
        {
            GameObject cp = new GameObject(name);
            cp.transform.SetParent(parent);
            cp.transform.position = pos;

            var box = cp.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(2.5f, 4.0f, 4.0f);
            box.center = new Vector3(0f, 2.0f, 0f);

            var zone = cp.AddComponent<CheckpointZone>();
            zone.checkpointIndex = index;
        }

        private static void BuildPitHazard(Transform parent, string name, Vector3 pos, Vector3 size)
        {
            GameObject pit = new GameObject(name);
            pit.transform.SetParent(parent);
            pit.transform.position = pos;

            var box = pit.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = size;

            pit.AddComponent<PitHazardZone>();
        }

        private static void BuildGrassPatch(Transform parent, string prefix, float startX, float endX, float y, float zMin = -0.35f, float zMax = 0.35f, float step = 0.55f)
        {
            GameObject group = new GameObject(prefix + "_GrassPatch");
            group.transform.SetParent(parent);
            group.transform.position = Vector3.zero;

            int index = 0;
            for (float x = startX; x <= endX; x += step)
            {
                float localX = x + Random.Range(-0.14f, 0.14f);
                float localZ = Random.Range(zMin, zMax);
                float localY = y + Random.Range(-0.03f, 0.04f); // subtle ground unevenness
                Vector3 pos = new Vector3(localX, localY, localZ);

                GameObject tuftGO = new GameObject($"{prefix}_Tuft_{index:D2}");
                tuftGO.transform.SetParent(group.transform);
                tuftGO.transform.position = pos;
                tuftGO.transform.localRotation = Quaternion.Euler(0f, Random.Range(-45f, 45f), 0f);

                // More mesh variety: every 4th tuft is a small clustered patch for lush clumps
                TuftMeshStyle style;
                int mod4 = index % 4;
                if (mod4 == 0) style = TuftMeshStyle.StarCards;
                else if (mod4 == 1) style = TuftMeshStyle.CurvedFan;
                else if (mod4 == 2) style = TuftMeshStyle.CrossCards;
                else style = TuftMeshStyle.ClusteredPatch; // small lush cluster every 4th

                TuftTextureVariant variant = (index % 3 == 0) ? TuftTextureVariant.Leaning : TuftTextureVariant.Upright;

                // Smaller but not uniform — organic size variation
                float w = Random.Range(0.52f, 0.68f);
                float h = Random.Range(0.38f, 0.50f);

                var tuft = tuftGO.AddComponent<Stylized3DGrassTuft>();
                tuft.Setup(style, variant, w, h);

                index++;
            }
        }

        private static void BuildPlatform(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private static void BuildLedgeCliff(Transform parent, string name, Vector3 pos, Vector3 size, Material stoneMat, Material turfMat)
        {
            GameObject cliffGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cliffGO.name = name;
            cliffGO.transform.SetParent(parent);
            cliffGO.transform.position = pos;
            cliffGO.transform.localScale = size;
            cliffGO.GetComponent<MeshRenderer>().sharedMaterial = stoneMat;

            // Green grass cap on top of cliff (Planet of Lana aesthetic)
            float capHeight = 0.14f;
            GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cap.name = "Turf_Top";
            cap.transform.SetParent(cliffGO.transform);
            cap.transform.localPosition = new Vector3(0f, 0.5f - (capHeight / size.y * 0.5f), 0f);
            cap.transform.localScale = new Vector3(1.02f, capHeight / size.y, 1.02f);
            cap.GetComponent<MeshRenderer>().sharedMaterial = turfMat;
            DestroyImmediate(cap.GetComponent<Collider>());
        }

        private static void BuildPushPullCrate(Transform parent, Vector3 pos, Material woodMat, Material metalMat)
        {
            GameObject crateGO = new GameObject("Interactable_Crate_PlanetOfLana");
            crateGO.transform.SetParent(parent);
            crateGO.transform.position = pos;

            crateGO.AddComponent<BoxCollider>();
            crateGO.AddComponent<Rigidbody>();
            crateGO.AddComponent<InteractableCrate>();
            crateGO.AddComponent<DestructibleCrate>();

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Visual_Body";
            body.transform.SetParent(crateGO.transform);
            body.transform.localPosition = new Vector3(0f, 0.70f, 0f);
            body.transform.localScale = new Vector3(1.4f, 1.4f, 1.0f);
            body.GetComponent<MeshRenderer>().sharedMaterial = woodMat;
            DestroyImmediate(body.GetComponent<Collider>());

            GameObject bandHoriz = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bandHoriz.name = "Band_Horizontal";
            bandHoriz.transform.SetParent(crateGO.transform);
            bandHoriz.transform.localPosition = new Vector3(0f, 0.70f, 0f);
            bandHoriz.transform.localScale = new Vector3(1.42f, 0.18f, 1.02f);
            bandHoriz.GetComponent<MeshRenderer>().sharedMaterial = metalMat;
            DestroyImmediate(bandHoriz.GetComponent<Collider>());

            GameObject bandVert = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bandVert.name = "Band_Vertical";
            bandVert.transform.SetParent(crateGO.transform);
            bandVert.transform.localPosition = new Vector3(0f, 0.70f, 0f);
            bandVert.transform.localScale = new Vector3(0.18f, 1.42f, 1.02f);
            bandVert.GetComponent<MeshRenderer>().sharedMaterial = metalMat;
            DestroyImmediate(bandVert.GetComponent<Collider>());
        }

        private static void BuildThrowableItem(Transform parent, string name, Vector3 pos, Material orbMat)
        {
            GameObject itemGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            itemGO.name = name;
            itemGO.transform.SetParent(parent);
            itemGO.transform.position = pos;
            itemGO.transform.localScale = new Vector3(0.38f, 0.38f, 0.38f);
            itemGO.GetComponent<MeshRenderer>().sharedMaterial = orbMat;

            itemGO.AddComponent<PortableItem>();
        }

        private static void BuildThrowTargetBasket(Transform parent, Vector3 pos, Material altarMat, Material rimMat)
        {
            GameObject basket = new GameObject("ThrowTarget_AltarBasket");
            basket.transform.SetParent(parent);
            basket.transform.position = pos;

            GameObject bowl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bowl.name = "Bowl";
            bowl.transform.SetParent(basket.transform);
            bowl.transform.localPosition = Vector3.zero;
            bowl.transform.localScale = new Vector3(1.4f, 0.25f, 1.4f);
            bowl.GetComponent<MeshRenderer>().sharedMaterial = altarMat;

            GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.name = "Rim";
            rim.transform.SetParent(basket.transform);
            rim.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            rim.transform.localScale = new Vector3(1.5f, 0.08f, 1.5f);
            rim.GetComponent<MeshRenderer>().sharedMaterial = rimMat;
        }

        private static void BuildVerletRope(Transform parent, Vector3 anchorPos, float length)
        {
            GameObject ropeGO = new GameObject("VerletRope_Chasm");
            ropeGO.transform.SetParent(parent);
            ropeGO.transform.position = anchorPos;

            var rope = ropeGO.AddComponent<RopeController>();
            rope.BuildRope();
        }

        private static void BuildPlayer(Transform parent, Vector3 pos, Material orbMat)
        {
            // Use verified authentic character An model with official animations
            GameObject playerGO = GameObject.Find("Player_An") ?? GameObject.FindWithTag("Player");

            if (playerGO == null)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/PF_Player_An.prefab");
                if (prefab != null)
                {
                    playerGO = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    playerGO.name = "Player_An";
                    playerGO.tag = "Player";
                    playerGO.transform.position = pos;
                    playerGO.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                }
            }

            if (playerGO == null)
            {
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Characters/Character_An.fbx");
                if (model != null)
                {
                    playerGO = (GameObject)PrefabUtility.InstantiatePrefab(model);
                    playerGO.name = "Player_An";
                    playerGO.tag = "Player";
                    playerGO.transform.position = pos;
                    playerGO.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

                    var cc = playerGO.AddComponent<CharacterController>();
                    cc.height = 1.8f;
                    cc.radius = 0.35f;
                    cc.center = new Vector3(0f, 0.9f, 0f);
                    cc.stepOffset = 0.35f;

                    playerGO.AddComponent<AnAnimationController>();
                    playerGO.AddComponent<PlayerController>();
                    playerGO.AddComponent<LedgeGrabDetector>();
                    playerGO.AddComponent<GrassInteraction>();
                }
            }

            if (playerGO != null)
            {
                // Ensure camera tracks Player_An
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    var camCtrl = mainCam.GetComponent<Cinematic2DCamera>() ?? mainCam.gameObject.AddComponent<Cinematic2DCamera>();
                    camCtrl.Target = playerGO.transform;
                    camCtrl.ConfigureLevelBounds(-35f, 120f, 1.0f, false);
                    camCtrl.SnapToTarget();
                }
            }
        }

        private static void BuildSign(Transform parent, string name, Vector3 pos, string text)
        {
            GameObject signGO = new GameObject(name);
            signGO.transform.SetParent(parent);
            signGO.transform.position = pos;

            TextMesh tm = signGO.AddComponent<TextMesh>();
            tm.text = text;
            tm.fontSize = 24;
            tm.characterSize = 0.055f;
            tm.color = new Color(0.95f, 0.95f, 0.95f, 0.95f);
            tm.anchor = TextAnchor.LowerLeft;
            tm.alignment = TextAlignment.Left;
        }

        private static void SetupLightingAndAtmosphere(Transform parent)
        {
            var lightObj = GameObject.Find("Directional Light");
            Light sun;
            if (lightObj == null)
            {
                lightObj = new GameObject("Directional Light");
                sun = lightObj.AddComponent<Light>();
                sun.type = LightType.Directional;
            }
            else
            {
                sun = lightObj.GetComponent<Light>();
            }

            sun.color = new Color(1.0f, 0.96f, 0.90f);
            sun.intensity = 1.25f;
            lightObj.transform.rotation = Quaternion.Euler(34f, -32f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.68f, 0.82f, 0.95f);
            RenderSettings.ambientEquatorColor = new Color(0.48f, 0.58f, 0.62f);
            RenderSettings.ambientGroundColor = new Color(0.24f, 0.28f, 0.26f);

            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.56f, 0.76f, 0.92f);
            }

            // Distant rolling hills starting behind Station 4 (X = 22+) so Station 1 & 2 have clean open sky
            GameObject bgGroup = new GameObject("Backdrop_DistantHills");
            bgGroup.transform.SetParent(parent);
            bgGroup.transform.position = new Vector3(36f, -6f, 32f);

            Material hillMatFar = GetOrCreateMaterial("M_BG_Far", new Color(0.50f, 0.64f, 0.76f), 0f);
            Material hillMatMid = GetOrCreateMaterial("M_BG_Mid", new Color(0.40f, 0.52f, 0.64f), 0f);

            for (int i = 0; i <= 3; i++)
            {
                GameObject hill = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                hill.name = $"Hill_{i}";
                hill.transform.SetParent(bgGroup.transform);
                hill.transform.localPosition = new Vector3(i * 18f, -1f + Mathf.Sin(i * 0.9f) * 2.0f, (i % 2 == 0) ? 6f : 0f);
                hill.transform.localScale = new Vector3(28f, 14f, 4f);
                hill.GetComponent<MeshRenderer>().sharedMaterial = (i % 2 == 0) ? hillMatFar : hillMatMid;
                DestroyImmediate(hill.GetComponent<Collider>());
            }
        }

        private static Material GetOrCreateMaterial(string name, Color baseColor, float smoothness)
        {
            string dir = "Assets/Materials";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            string path = $"{dir}/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                string[] guids = AssetDatabase.FindAssets($"{name} t:Material", new[] { dir });
                if (guids.Length > 0)
                {
                    path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                }
            }
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            if (mat == null)
            {
                mat = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.color = baseColor;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", baseColor);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material GetOrCreateGlowingMaterial(string name, Color glowColor)
        {
            string dir = "Assets/Materials";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            string path = $"{dir}/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                string[] guids = AssetDatabase.FindAssets($"{name} t:Material", new[] { dir });
                if (guids.Length > 0)
                {
                    path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                }
            }
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");

            if (mat == null)
            {
                mat = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.color = glowColor;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", glowColor);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", glowColor);
            EditorUtility.SetDirty(mat);
            return mat;
        }
#endif
    }
}
