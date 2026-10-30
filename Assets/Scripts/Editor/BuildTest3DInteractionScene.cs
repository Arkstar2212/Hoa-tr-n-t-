using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;
using HoaVaMau.Test3D;

namespace HoaVaMau.Editor
{
    public class BuildTest3DInteractionScene
    {
        [MenuItem("Hoa va Mau/Build 3D Interaction Test Scene")]
        public static void BuildScene()
        {
            Debug.Log("<color=#38bdf8>[Build 3D Scene]</color> Starting construction of Test_3D_Interaction.unity...");

            // 1. Create a brand new independent empty scene
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            string scenePath = "Assets/Scenes/Test_3D_Interaction.unity";

            // 2. Load / Create Required Materials
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");

            // Material: Ground Grass
            Material matGrass = GetOrCreateMaterial("Assets/Materials/Foliage/M_Test3D_Grass.mat", litShader, new Color(0.31f, 0.45f, 0.22f), 0.1f);
            // Material: Ground Dirt Path
            Material matDirt = GetOrCreateMaterial("Assets/Materials/Environment/Ground/M_Test3D_Dirt.mat", litShader, new Color(0.48f, 0.38f, 0.27f), 0.15f);
            // Material: Water 3D
            Material matWater = GetOrCreateMaterial("Assets/Materials/Environment/Water/M_Test3D_Water.mat", litShader, new Color(0.12f, 0.52f, 0.62f, 0.85f), 0.9f, true);
            // Material: Rock
            Material matRock = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/Rocks/M_StylizedRock_Silhouette.mat");
            if (matRock == null) matRock = GetOrCreateMaterial("Assets/Materials/M_Test3D_Rock.mat", litShader, new Color(0.38f, 0.36f, 0.40f), 0.2f);
            // Material: Wood
            Material matWood = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Vehicles/M_Boat_Wood.mat");
            if (matWood == null) matWood = GetOrCreateMaterial("Assets/Materials/M_Test3D_Wood.mat", litShader, new Color(0.38f, 0.24f, 0.14f), 0.2f);
            // Material: Foliage
            Material matFoliage = GetOrCreateMaterial("Assets/Materials/Foliage/M_Test3D_Foliage.mat", litShader, new Color(0.24f, 0.42f, 0.18f), 0.1f);
            // Material: Relic
            Material matRelic = GetOrCreateMaterial("Assets/Materials/Props/M_Test3D_Relic.mat", litShader, new Color(0.95f, 0.80f, 0.30f), 0.8f);

            // 3. Setup Directional Light
            GameObject lightGo = new GameObject("Directional Light");
            Light dirLight = lightGo.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.color = new Color(1.0f, 0.96f, 0.90f);
            dirLight.intensity = 1.25f;
            dirLight.shadows = LightShadows.Soft;
            dirLight.shadowStrength = 0.85f;
            lightGo.transform.rotation = Quaternion.Euler(45f, -35f, 0f);

            // 4. Setup Camera (3D_Test_Camera)
            GameObject camGo = new GameObject("3D_Test_Camera");
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 1000f;
            camGo.AddComponent<AudioListener>();
            Test3DCameraFollow camFollow = camGo.AddComponent<Test3DCameraFollow>();

            // 5. Environment Root
            GameObject envRoot = new GameObject("Environment");

            // 5.1 Ground
            GameObject groundGroup = new GameObject("Ground");
            groundGroup.transform.SetParent(envRoot.transform);

            // Main Grass Ground
            GameObject mainGround = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mainGround.name = "MainGround_Grass";
            mainGround.transform.SetParent(groundGroup.transform);
            mainGround.transform.position = new Vector3(0f, -0.5f, 6f);
            mainGround.transform.localScale = new Vector3(36f, 1f, 24f);
            mainGround.GetComponent<MeshRenderer>().sharedMaterial = matGrass;

            // Dirt Walkway Path
            GameObject dirtPath = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dirtPath.name = "DirtWalkwayPath";
            dirtPath.transform.SetParent(groundGroup.transform);
            dirtPath.transform.position = new Vector3(0f, -0.48f, 6f);
            dirtPath.transform.localScale = new Vector3(4.5f, 1f, 24f);
            dirtPath.GetComponent<MeshRenderer>().sharedMaterial = matDirt;

            // Shore Bank Slope
            GameObject shoreBank = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shoreBank.name = "ShoreBankSlope";
            shoreBank.transform.SetParent(groundGroup.transform);
            shoreBank.transform.position = new Vector3(0f, -0.8f, -6.5f);
            shoreBank.transform.rotation = Quaternion.Euler(15f, 0f, 0f);
            shoreBank.transform.localScale = new Vector3(36f, 1f, 6f);
            shoreBank.GetComponent<MeshRenderer>().sharedMaterial = matDirt;

            // 5.2 Rocks_Bao (BẮT BUỘC sử dụng asset đá của Bảo: Rock001.fbx & Rock0022.fbx)
            GameObject rocksGroup = new GameObject("Rocks_Bao");
            rocksGroup.transform.SetParent(envRoot.transform);

            GameObject rock001Prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Environment/SM_Rock_Small_01.fbx");
            GameObject rock0022Prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Environment/SM_Rock_Small_02.fbx");

            // Sắp xếp bố cục rõ ràng theo đúng yêu cầu:
            //       ROCK    ROCK
            //    ROCK   ROCK
            //         ROCK
            //   ROCK        ROCK
            Vector3[] rockPositions = new Vector3[]
            {
                // Hàng 4 (Xa nhất)
                new Vector3(-4.0f, 0f, 12.0f),
                new Vector3( 4.0f, 0f, 12.0f),
                // Hàng 3 (Trung xa)
                new Vector3(-2.2f, 0f,  9.5f),
                new Vector3( 2.2f, 0f,  9.5f),
                // Hàng 2 (Chính giữa)
                new Vector3( 0.0f, 0f,  7.0f),
                // Hàng 1 (Gần nhất, hai bên mở rộng)
                new Vector3(-5.5f, 0f,  5.0f),
                new Vector3( 5.5f, 0f,  5.0f)
            };

            for (int i = 0; i < rockPositions.Length; i++)
            {
                GameObject sourcePrefab = (i % 2 == 0 && rock001Prefab != null) ? rock001Prefab : (rock0022Prefab != null ? rock0022Prefab : rock001Prefab);
                if (sourcePrefab != null)
                {
                    GameObject rockInstance = (GameObject)PrefabUtility.InstantiatePrefab(sourcePrefab, rocksGroup.transform);
                    rockInstance.name = $"Rock_Bao_{i + 1}";
                    rockInstance.transform.position = rockPositions[i];
                    // Normalize rock scale: Rock001 has child 'Cube' at localScale 100, Rock0022 has root at scale 1
                    float targetWorldScale = 0.7f + (i * 0.05f); // ~2.2m to 2.8m boulder size
                    if (sourcePrefab == rock001Prefab)
                    {
                        rockInstance.transform.localScale = Vector3.one * targetWorldScale;
                    }
                    else
                    {
                        rockInstance.transform.localScale = Vector3.one * (targetWorldScale * 100f);
                    }
                    rockInstance.transform.rotation = Quaternion.Euler(0f, (i * 57f) % 360f, 0f);

                    // Ensure all sub-renderers have material, shadow, and MeshCollider
                    MeshRenderer[] renderers = rockInstance.GetComponentsInChildren<MeshRenderer>(true);
                    foreach (var r in renderers)
                    {
                        r.sharedMaterial = matRock;
                        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                        r.receiveShadows = true;

                        MeshFilter mf = r.GetComponent<MeshFilter>();
                        if (mf != null && mf.sharedMesh != null)
                        {
                            MeshCollider mc = r.gameObject.GetComponent<MeshCollider>();
                            if (mc == null) mc = r.gameObject.AddComponent<MeshCollider>();
                            mc.convex = true;
                        }
                    }

                    // Strip any leftover Blender default Camera or Light from imported fbx
                    Transform camChild = rockInstance.transform.Find("Camera");
                    if (camChild != null) Object.DestroyImmediate(camChild.gameObject);
                    Transform lightChild = rockInstance.transform.Find("Light");
                    if (lightChild != null) Object.DestroyImmediate(lightChild.gameObject);

                    // Align base of rock to Y = 0 (Ground level)
                    Renderer[] rends = rockInstance.GetComponentsInChildren<Renderer>();
                    if (rends.Length > 0)
                    {
                        Bounds b = rends[0].bounds;
                        for (int k = 1; k < rends.Length; k++) b.Encapsulate(rends[k].bounds);
                        rockInstance.transform.position += new Vector3(0, -b.min.y, 0);
                    }
                }
            }

            // 5.3 Trees
            GameObject treesGroup = new GameObject("Trees");
            treesGroup.transform.SetParent(envRoot.transform);

            Vector3[] treePositions = new Vector3[]
            {
                new Vector3(-11f, 0f,  2f),
                new Vector3(-13f, 0f,  8f),
                new Vector3(-10f, 0f, 15f),
                new Vector3( 10f, 0f, 15f),
                new Vector3( 13f, 0f,  8f),
                new Vector3( 11f, 0f,  2f),
                new Vector3(  0f, 0f, 17f)
            };

            for (int i = 0; i < treePositions.Length; i++)
            {
                GameObject treeGo = new GameObject($"Tree_3D_{i + 1}");
                treeGo.transform.SetParent(treesGroup.transform);
                treeGo.transform.position = treePositions[i];

                // Trunk
                GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                trunk.name = "Trunk";
                trunk.transform.SetParent(treeGo.transform);
                trunk.transform.localPosition = new Vector3(0f, 1.75f, 0f);
                trunk.transform.localScale = new Vector3(0.5f, 1.75f, 0.5f);
                trunk.GetComponent<MeshRenderer>().sharedMaterial = matWood;

                // Foliage layers (multi-sphere stylized canopy)
                GameObject canopy1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                canopy1.name = "Canopy_Lower";
                canopy1.transform.SetParent(treeGo.transform);
                canopy1.transform.localPosition = new Vector3(0f, 3.8f, 0f);
                canopy1.transform.localScale = new Vector3(3.2f, 2.2f, 3.2f);
                canopy1.GetComponent<MeshRenderer>().sharedMaterial = matFoliage;

                GameObject canopy2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                canopy2.name = "Canopy_Upper";
                canopy2.transform.SetParent(treeGo.transform);
                canopy2.transform.localPosition = new Vector3(0f, 5.0f, 0f);
                canopy2.transform.localScale = new Vector3(2.2f, 1.8f, 2.2f);
                canopy2.GetComponent<MeshRenderer>().sharedMaterial = matFoliage;
            }

            // 5.4 Grass
            GameObject grassGroup = new GameObject("Grass");
            grassGroup.transform.SetParent(envRoot.transform);

            GameObject grassTuftPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Foliage/PF_StylizedGrassTuft_3D.prefab");
            if (grassTuftPrefab != null)
            {
                Vector3[] grassPositions = new Vector3[]
                {
                    new Vector3(-4f, 0f, 1f),
                    new Vector3(-3f, 0f, 3f),
                    new Vector3( 4f, 0f, 1f),
                    new Vector3( 3f, 0f, 3.5f),
                    new Vector3(-7f, 0f, 6f),
                    new Vector3( 7f, 0f, 6f),
                    new Vector3(-3f, 0f, 11f),
                    new Vector3( 3f, 0f, 11f),
                    new Vector3(-8f, 0f, 12f),
                    new Vector3( 8f, 0f, 12f)
                };

                for (int i = 0; i < grassPositions.Length; i++)
                {
                    GameObject gInstance = (GameObject)PrefabUtility.InstantiatePrefab(grassTuftPrefab, grassGroup.transform);
                    gInstance.name = $"GrassTuft_{i + 1}";
                    gInstance.transform.position = grassPositions[i];
                    gInstance.transform.localScale = Vector3.one * Random.Range(1.2f, 1.6f);
                    gInstance.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                }
            }

            // 5.5 EnvironmentObjects (Wooden Pier & Crates)
            GameObject envObjects = new GameObject("EnvironmentObjects");
            envObjects.transform.SetParent(envRoot.transform);

            // Pier Deck
            GameObject pierDeck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pierDeck.name = "WoodenPier_Deck";
            pierDeck.transform.SetParent(envObjects.transform);
            pierDeck.transform.position = new Vector3(-3.5f, 0.05f, -6.0f);
            pierDeck.transform.localScale = new Vector3(2.5f, 0.2f, 6.0f);
            pierDeck.GetComponent<MeshRenderer>().sharedMaterial = matWood;

            // Pier Pilings
            Vector3[] postPositions = new Vector3[]
            {
                new Vector3(-4.5f, -0.6f, -4.0f),
                new Vector3(-2.5f, -0.6f, -4.0f),
                new Vector3(-4.5f, -0.6f, -7.0f),
                new Vector3(-2.5f, -0.6f, -7.0f),
                new Vector3(-4.5f, -0.6f, -8.8f),
                new Vector3(-2.5f, -0.6f, -8.8f)
            };
            foreach (var postPos in postPositions)
            {
                GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                post.name = "PierPost";
                post.transform.SetParent(envObjects.transform);
                post.transform.position = postPos;
                post.transform.localScale = new Vector3(0.2f, 0.8f, 0.2f);
                post.GetComponent<MeshRenderer>().sharedMaterial = matWood;
            }

            // 6. Water 3D
            GameObject waterGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            waterGo.name = "Water";
            waterGo.transform.position = new Vector3(0f, -0.2f, -12.0f);
            waterGo.transform.localScale = new Vector3(4.0f, 1.0f, 2.0f); // 40m x 20m
            waterGo.GetComponent<MeshRenderer>().sharedMaterial = matWater;
            // Remove plane collider so boat and objects can settle properly
            Object.DestroyImmediate(waterGo.GetComponent<Collider>());

            // 7. Boat 3D
            GameObject boatPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Vehicles/PF_StylizedBoat.prefab");
            GameObject boatGo = null;
            if (boatPrefab != null)
            {
                boatGo = (GameObject)PrefabUtility.InstantiatePrefab(boatPrefab);
                boatGo.name = "Boat";
                boatGo.transform.position = new Vector3(-3.5f, -0.2f, -8.5f);
                boatGo.transform.rotation = Quaternion.Euler(0f, 15f, 0f);
                boatGo.transform.localScale = Vector3.one * 1.35f;

                // Add BoxCollider for physical stepping / test
                BoxCollider boatCol = boatGo.GetComponent<BoxCollider>();
                if (boatCol == null) boatCol = boatGo.AddComponent<BoxCollider>();
                boatCol.center = new Vector3(0f, 0.35f, 0f);
                boatCol.size = new Vector3(2.6f, 0.7f, 1.2f);

                // Add floating script
                boatGo.AddComponent<FloatingBoat3D>();
            }

            // 8. Player 3D
            GameObject charPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Characters/Character_Rigged_Animations.fbx");
            GameObject playerGo = null;
            if (charPrefab != null)
            {
                playerGo = (GameObject)PrefabUtility.InstantiatePrefab(charPrefab);
                playerGo.name = "Player";
                playerGo.tag = "Player";
                playerGo.transform.position = new Vector3(0f, 0.05f, 0f);
                playerGo.transform.rotation = Quaternion.identity;
                playerGo.transform.localScale = Vector3.one;

                // CharacterController
                CharacterController cc = playerGo.GetComponent<CharacterController>();
                if (cc == null) cc = playerGo.AddComponent<CharacterController>();
                cc.center = new Vector3(0f, 0.85f, 0f);
                cc.radius = 0.35f;
                cc.height = 1.7f;
                cc.stepOffset = 0.3f;
                cc.slopeLimit = 45f;

                // Player3DController
                Player3DController pController = playerGo.AddComponent<Player3DController>();

                // Animator Controller
                Animator anim = playerGo.GetComponent<Animator>();
                if (anim == null) anim = playerGo.AddComponent<Animator>();
                anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/Controllers/AC_Player_An.controller");

                // Assign player to camera follow
                camFollow.SetTarget(playerGo.transform);
            }

            // 9. Interactive Objects
            GameObject interactRoot = new GameObject("InteractiveObjects");

            // 9.1 Inspect Object
            GameObject inspectGo = new GameObject("InspectObject");
            inspectGo.transform.SetParent(interactRoot.transform);
            inspectGo.transform.position = new Vector3(-3.0f, 0f, 2.5f);

            // Pedestal base
            GameObject pedBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pedBase.name = "PedestalBase";
            pedBase.transform.SetParent(inspectGo.transform);
            pedBase.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            pedBase.transform.localScale = new Vector3(0.8f, 0.35f, 0.8f);
            pedBase.GetComponent<MeshRenderer>().sharedMaterial = matWood;

            // Relic crystal
            GameObject relicMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            relicMesh.name = "RelicCrystal";
            relicMesh.transform.SetParent(inspectGo.transform);
            relicMesh.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            relicMesh.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            relicMesh.transform.localRotation = Quaternion.Euler(45f, 45f, 45f);
            relicMesh.GetComponent<MeshRenderer>().sharedMaterial = matRelic;

            // Inspect Interactable Component
            InteractableObject3D inspectComp = inspectGo.AddComponent<InteractableObject3D>();
            inspectComp.interactionType = InteractionType.Inspect;
            inspectComp.objectTitle = "Cổ Vật Phát Sáng (Ancient Relic)";
            inspectComp.promptMessage = "Bấm [E] để Quan Sát";
            inspectComp.actionMessage = "You found an object.";
            inspectComp.interactionRadius = 2.5f;

            // 9.2 Pushable Object (Rigidbody + Collider)
            GameObject pushGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pushGo.name = "PushableObject";
            pushGo.transform.SetParent(interactRoot.transform);
            pushGo.transform.position = new Vector3(0f, 0.55f, 3.5f);
            pushGo.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
            pushGo.GetComponent<MeshRenderer>().sharedMaterial = matWood;

            // Rigidbody
            Rigidbody pushRb = pushGo.AddComponent<Rigidbody>();
            pushRb.mass = 12f;
            pushRb.linearDamping = 2.5f;
            pushRb.angularDamping = 3.0f;
            pushRb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            InteractableObject3D pushComp = pushGo.AddComponent<InteractableObject3D>();
            pushComp.interactionType = InteractionType.Push;
            pushComp.objectTitle = "Thùng Gỗ Vật Lý (Pushable Crate)";
            pushComp.promptMessage = "Đi thẳng vào thùng để đẩy (Push)";
            pushComp.actionMessage = "Đang đẩy thùng gỗ vật lý...";
            pushComp.interactionRadius = 2.5f;

            // 9.3 Trigger Object (Lantern / Mystic Beacon)
            GameObject triggerGo = new GameObject("TriggerObject");
            triggerGo.transform.SetParent(interactRoot.transform);
            triggerGo.transform.position = new Vector3(3.0f, 0f, 2.5f);

            // Pillar
            GameObject trigPillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trigPillar.name = "Pillar";
            trigPillar.transform.SetParent(triggerGo.transform);
            trigPillar.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            trigPillar.transform.localScale = new Vector3(0.5f, 0.75f, 0.5f);
            trigPillar.GetComponent<MeshRenderer>().sharedMaterial = matRock;

            // Beacon Orb
            GameObject beaconOrb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            beaconOrb.name = "BeaconOrb";
            beaconOrb.transform.SetParent(triggerGo.transform);
            beaconOrb.transform.localPosition = new Vector3(0f, 1.7f, 0f);
            beaconOrb.transform.localScale = Vector3.one * 0.45f;
            beaconOrb.GetComponent<MeshRenderer>().sharedMaterial = matRelic;

            // Light attached to trigger
            GameObject orbLightGo = new GameObject("BeaconLight");
            orbLightGo.transform.SetParent(triggerGo.transform);
            orbLightGo.transform.localPosition = new Vector3(0f, 1.7f, 0f);
            Light orbLight = orbLightGo.AddComponent<Light>();
            orbLight.type = LightType.Point;
            orbLight.range = 7f;
            orbLight.intensity = 2.5f;
            orbLight.color = Color.cyan;
            orbLight.enabled = false; // Initially off

            // Trigger Zone (SphereCollider as trigger)
            SphereCollider trigCol = triggerGo.AddComponent<SphereCollider>();
            trigCol.isTrigger = true;
            trigCol.radius = 2.2f;
            trigCol.center = new Vector3(0f, 1f, 0f);

            // Trigger Interactable Component
            InteractableObject3D trigComp = triggerGo.AddComponent<InteractableObject3D>();
            trigComp.interactionType = InteractionType.Trigger;
            trigComp.objectTitle = "Trụ Đèn Thần Bí (Mystic Beacon)";
            trigComp.promptMessage = "Bước vào vùng sáng hoặc bấm [E] để Kích Hoạt";
            trigComp.actionMessage = "Object activated.";
            trigComp.isTriggerZone = true;
            trigComp.interactionRadius = 2.5f;

            // Reflection field assignment
            var lightField = typeof(InteractableObject3D).GetField("targetLight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (lightField != null) lightField.SetValue(trigComp, orbLight);

            // 10. Systems Root
            GameObject sysRoot = new GameObject("Systems");
            GameObject interSysGo = new GameObject("InteractionSystem");
            interSysGo.transform.SetParent(sysRoot.transform);
            interSysGo.AddComponent<InteractionSystem3D>();

            // 11. Save Scene
            EditorSceneManager.MarkSceneDirty(newScene);
            bool saved = EditorSceneManager.SaveScene(newScene, scenePath);
            AssetDatabase.SaveAssets();

            Debug.Log($"<color=#4ade80>[SUCCESS]</color> Đã tạo xong Scene 3D: {scenePath}! (Saved: {saved})");
        }

        private static Material GetOrCreateMaterial(string path, Shader shader, Color color, float smoothness, bool transparent = false)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                else if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);

                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

                if (transparent)
                {
                    mat.SetFloat("_Surface", 1); // Transparent
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                }

                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }
    }
}
