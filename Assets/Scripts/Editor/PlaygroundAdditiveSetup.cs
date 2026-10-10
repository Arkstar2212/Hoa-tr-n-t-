using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace HoaVaMau.Gameplay
{
    public static class PlaygroundAdditiveSetup
    {
        private const string SceneAssetPath = "Assets/Scenes/Playground_Mechanics.unity";

        [MenuItem("Hoa va Mau/Setup Additive Test Elements")]
        public static void ExecuteSetup()
        {
            var scene = EditorSceneManager.OpenScene(SceneAssetPath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError("[PlaygroundAdditiveSetup] Failed to open " + SceneAssetPath);
                return;
            }

            // 1. Verify Player_An is present
            GameObject player = GameObject.Find("Player_An");
            if (player == null)
            {
                var allPlayers = GameObject.FindGameObjectsWithTag("Player");
                foreach (var p in allPlayers)
                {
                    if (p.name.Contains("Player_An") || p.name.Contains("nhan vat chinh"))
                    {
                        player = p;
                        break;
                    }
                }
            }

            if (player == null)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/PF_Player_An.prefab");
                if (prefab != null)
                {
                    player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    player.name = "Player_An";
                    player.tag = "Player";
                    player.transform.position = new Vector3(-17.5f, 0.1f, 0f);
                    player.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                    Debug.Log("[PlaygroundAdditiveSetup] Restored Player_An from prefab.");
                }
            }

            // Ensure PlayerController has the updated smooth parameters
            if (player != null)
            {
                var pc = player.GetComponent<PlayerController>();
                if (pc != null)
                {
                    SerializedObject so = new SerializedObject(pc);
                    var walkSpeedProp = so.FindProperty("walkSpeed");
                    if (walkSpeedProp != null) walkSpeedProp.floatValue = 2.4f;
                    var sprintSpeedProp = so.FindProperty("sprintSpeed");
                    if (sprintSpeedProp != null) sprintSpeedProp.floatValue = 4.2f;
                    var climbDurationProp = so.FindProperty("climbDuration");
                    if (climbDurationProp != null) climbDurationProp.floatValue = 1.05f;
                    so.ApplyModifiedProperties();
                }

                // Ensure camera targets player
                Camera cam = Camera.main;
                if (cam != null)
                {
                    var camCtrl = cam.GetComponent<Cinematic2DCamera>();
                    if (camCtrl == null) camCtrl = cam.gameObject.AddComponent<Cinematic2DCamera>();
                    camCtrl.Target = player.transform;
                    camCtrl.ConfigureLevelBounds(-35f, 120f, 1.0f, false);
                }
            }

            // 2. Find or create Test Elements Parent
            GameObject testRoot = GameObject.Find("--- Test_Mechanics_Additive_Zone ---");
            if (testRoot == null)
            {
                testRoot = new GameObject("--- Test_Mechanics_Additive_Zone ---");
            }

            // Materials
            Material timberMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Foliage/M_Playground_Timber.mat")
                              ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Foliage/M_Playground_Timber.mat");
            Material cliffMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Rocks/M_Playground_Cliff.mat")
                             ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Rocks/M_Playground_Cliff.mat");
            Material crateMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Props/M_WoodenCrate_01.mat")
                             ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Props/M_Playground_Crate.mat")
                             ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Props/M_Playground_Crate.mat");
            Material orbMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Props/M_Playground_Orb.mat")
                           ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Props/M_Playground_Orb.mat");

            // ========================================================
            // A. TEST ROPE SEGMENT (Khúc dây test ngay gần vạch xuất phát)
            // ========================================================
            // Overhead mount beam at x = -11.5, y = 5.25
            Transform ropeAnchor = testRoot.transform.Find("Test_Rope_Mount");
            if (ropeAnchor == null)
            {
                GameObject mountGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
                mountGO.name = "Test_Rope_Mount";
                mountGO.transform.SetParent(testRoot.transform);
                mountGO.transform.position = new Vector3(-11.5f, 5.25f, 0f);
                mountGO.transform.localScale = new Vector3(1.6f, 0.35f, 0.8f);
                if (timberMat != null) mountGO.GetComponent<MeshRenderer>().sharedMaterial = timberMat;
                ropeAnchor = mountGO.transform;
            }

            Transform testRopeT = testRoot.transform.Find("Test_VerletRope");
            GameObject testRopeGO;
            if (testRopeT == null)
            {
                testRopeGO = new GameObject("Test_VerletRope");
                testRopeGO.transform.SetParent(testRoot.transform);
                testRopeGO.transform.position = new Vector3(-11.5f, 5.1f, 0f);
            }
            else
            {
                testRopeGO = testRopeT.gameObject;
                testRopeGO.transform.position = new Vector3(-11.5f, 5.1f, 0f);
            }

            var testRope = testRopeGO.GetComponent<RopeController>();
            if (testRope == null) testRope = testRopeGO.AddComponent<RopeController>();
            {
                SerializedObject soRope = new SerializedObject(testRope);
                soRope.FindProperty("nodeCount").intValue = 14;
                soRope.FindProperty("totalLength").floatValue = 3.8f;
                soRope.FindProperty("ropeVisualWidth").floatValue = 0.10f;
                soRope.FindProperty("gravity").floatValue = 22.0f;
                soRope.FindProperty("airDamping").floatValue = 0.985f;
                soRope.FindProperty("maxSwingSpeed").floatValue = 7.5f;
                soRope.FindProperty("maxSwingAngle").floatValue = 52.0f;
                soRope.FindProperty("playerSwingForce").floatValue = 16.0f;
                soRope.FindProperty("climbWiggleForce").floatValue = 0.25f;
                soRope.ApplyModifiedProperties();
            }
            testRope.BuildRope();

            // ========================================================
            // B. TEST THROWABLE BALLS (Lụm bóng & Canh ném Parabol)
            // ========================================================
            Vector3[] ballPositions = new Vector3[]
            {
                new Vector3(-15.3f, 0.35f, 0f),
                new Vector3(-14.1f, 0.35f, 0f)
            };

            for (int b = 0; b < ballPositions.Length; b++)
            {
                string ballName = $"Test_ThrowableOrb_{b + 1}";
                Transform ballT = testRoot.transform.Find(ballName);
                GameObject ballGO;
                if (ballT == null)
                {
                    ballGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    ballGO.name = ballName;
                    ballGO.transform.SetParent(testRoot.transform);
                }
                else
                {
                    ballGO = ballT.gameObject;
                }

                ballGO.transform.position = ballPositions[b];
                ballGO.transform.localScale = new Vector3(0.40f, 0.40f, 0.40f);
                if (orbMat != null) ballGO.GetComponent<MeshRenderer>().sharedMaterial = orbMat;

                var rb = ballGO.GetComponent<Rigidbody>();
                if (rb == null) rb = ballGO.AddComponent<Rigidbody>();
                rb.mass = 2.5f;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

                var col = ballGO.GetComponent<SphereCollider>();
                if (col != null) col.radius = 0.5f;

                var portable = ballGO.GetComponent<PortableItem>();
                if (portable == null) portable = ballGO.AddComponent<PortableItem>();
                {
                    SerializedObject soBall = new SerializedObject(portable);
                    soBall.FindProperty("itemName").stringValue = $"Ancient Power Core {b + 1}";
                    soBall.FindProperty("pickupRadius").floatValue = 1.35f;
                    soBall.FindProperty("itemMass").floatValue = 2.5f;
                    soBall.ApplyModifiedProperties();
                }
            }

            // ========================================================
            // C. TEST CRATE & ELEVATED PLATFORM (Kéo thùng & Vỡ thùng PhysX)
            // ========================================================
            // Elevated platform at x = -5.0, top at y = 1.70m
            Transform platT = testRoot.transform.Find("Test_Platform_Step");
            GameObject platGO;
            if (platT == null)
            {
                platGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
                platGO.name = "Test_Platform_Step";
                platGO.transform.SetParent(testRoot.transform);
            }
            else
            {
                platGO = platT.gameObject;
            }
            platGO.transform.position = new Vector3(-5.0f, 0.85f, 0f);
            platGO.transform.localScale = new Vector3(3.2f, 1.7f, 3.0f);
            if (cliffMat != null) platGO.GetComponent<MeshRenderer>().sharedMaterial = cliffMat;

            // Test Crate at x = -8.0, y = 0.0
            Transform crateT = testRoot.transform.Find("Test_Interactable_Destructible_Crate");
            GameObject crateGO;
            if (crateT == null)
            {
                crateGO = new GameObject("Test_Interactable_Destructible_Crate");
                crateGO.transform.SetParent(testRoot.transform);

                // Visual Child
                GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visual.name = "Visual_Body";
                visual.transform.SetParent(crateGO.transform);
                visual.transform.localPosition = new Vector3(0f, 0.70f, 0f);
                visual.transform.localScale = new Vector3(1.4f, 1.4f, 1.0f);
                if (crateMat != null) visual.GetComponent<MeshRenderer>().sharedMaterial = crateMat;
                Object.DestroyImmediate(visual.GetComponent<Collider>());
            }
            else
            {
                crateGO = crateT.gameObject;
            }
            crateGO.transform.position = new Vector3(-8.0f, 0.0f, 0f);

            var crateCol = crateGO.GetComponent<BoxCollider>();
            if (crateCol == null) crateCol = crateGO.AddComponent<BoxCollider>();
            crateCol.size = new Vector3(1.4f, 1.4f, 1.0f);
            crateCol.center = new Vector3(0f, 0.70f, 0f);

            var crateRb = crateGO.GetComponent<Rigidbody>();
            if (crateRb == null) crateRb = crateGO.AddComponent<Rigidbody>();
            crateRb.mass = 60f;
            crateRb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;

            var ic = crateGO.GetComponent<InteractableCrate>();
            if (ic == null) ic = crateGO.AddComponent<InteractableCrate>();

            var dc = crateGO.GetComponent<DestructibleCrate>();
            if (dc == null) dc = crateGO.AddComponent<DestructibleCrate>();

            // ========================================================
            // D. UPDATE EXISTING SCENE CRATES & ROPE
            // ========================================================
            // Existing VerletRope_Chasm
            GameObject chasmRopeGO = GameObject.Find("VerletRope_Chasm");
            if (chasmRopeGO != null)
            {
                var chasmRope = chasmRopeGO.GetComponent<RopeController>();
                if (chasmRope != null)
                {
                    SerializedObject so = new SerializedObject(chasmRope);
                    so.FindProperty("gravity").floatValue = 22.0f;
                    so.FindProperty("airDamping").floatValue = 0.985f;
                    so.FindProperty("maxSwingSpeed").floatValue = 7.5f;
                    so.FindProperty("maxSwingAngle").floatValue = 52.0f;
                    so.FindProperty("playerSwingForce").floatValue = 16.0f;
                    so.FindProperty("climbWiggleForce").floatValue = 0.25f;
                    so.ApplyModifiedProperties();
                    chasmRope.BuildRope();
                }
            }

            // Existing Interactable_Crate_PlanetOfLana
            GameObject chasmCrateGO = GameObject.Find("Interactable_Crate_PlanetOfLana");
            if (chasmCrateGO != null)
            {
                if (chasmCrateGO.GetComponent<DestructibleCrate>() == null)
                {
                    chasmCrateGO.AddComponent<DestructibleCrate>();
                }
            }

            // Save Scene
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, SceneAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Debug.Log("<color=#40b8a6>[PlaygroundAdditiveSetup]</color> Additive test elements configured successfully! Player_An active.");
        }
    }
}
