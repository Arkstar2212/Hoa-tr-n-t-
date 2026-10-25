using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using System.IO;

namespace HoaVaMau.Gameplay
{
    public static class SetupAnPlaygroundScene
    {
        private const string SceneAssetPath = "Assets/Scenes/Playground_Mechanics.unity";
        private const string ControllerPath = "Assets/Animations/Controllers/AC_Player_An.controller";
        private const string PrefabPath = "Assets/Prefabs/Characters/PF_Player_An.prefab";
        private const string ModelPath = "Assets/Models/Characters/Character_An.fbx";

        [MenuItem("Hoa va Mau/Setup An Character & Playground Scene")]
        public static void Execute()
        {
            Debug.Log("<color=#40b8a6>[SetupAnPlaygroundScene]</color> Starting setup for An character and Playground scene...");

            // 1. Create or update An_Hero_AnimatorController
            var controller = CreateOrUpdateAnimatorController();

            // 2. Create Player_An Prefab
            GameObject playerPrefab = CreatePlayerAnPrefab(controller);

            // 3. Setup Playground Scene
            SetupScene(playerPrefab);

            Debug.Log("<color=#40b8a6>[SetupAnPlaygroundScene]</color> Successfully configured An character and Playground_Mechanics scene!");
        }

        public static AnimatorController CreateOrUpdateAnimatorController()
        {
            string dir = Path.GetDirectoryName(ControllerPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }

            EnsureParameter(controller, "Speed", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "IsClimbing", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "IsCrouching", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "IsCrawling", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "IsGrounded", AnimatorControllerParameterType.Bool);

            var rootStateMachine = controller.layers[0].stateMachine;

            AnimationClip idleClip = LoadClipFromFBX("Assets/Animations/Hero/Loco/Hero_Loco_Idle_Loop_IP.fbx");
            AnimationClip walkClip = LoadClipFromFBX("Assets/TripoModels/Boy_Character/Boy_Character.fbx", "walk");
            if (walkClip == null) walkClip = LoadClipFromFBX("Assets/Animations/Hero/Loco/Hero_Loco_Walk_Fwd_IP.fbx");
            AnimationClip runClip = LoadClipFromFBX("Assets/TripoModels/Boy_Character/Boy_Character.fbx", "run");
            if (runClip == null) runClip = LoadClipFromFBX("Assets/Animations/Hero/Loco/Hero_Loco_Run_Fwd_IP.fbx");
            AnimationClip climbClip = LoadClipFromFBX("Assets/TripoModels/Boy_Character/Boy_Character.fbx", "climb");
            if (climbClip == null) climbClip = LoadClipFromFBX("Assets/Animations/Hero/Traverse/Hero_Traverse_Climb_Down_IP.fbx");
            AnimationClip ledgeHangClip = LoadClipFromFBX("Assets/Animations/Hero/Traverse/Hero_Traverse_Hanging_Idle_Loop.fbx");
            AnimationClip crouchClip = LoadClipFromFBX("Assets/Animations/Hero/Stealth/Hero_Stealth_Crouch_Idle_Loop.fbx");
            AnimationClip crouchWalkClip = LoadClipFromFBX("Assets/Animations/Hero/Stealth/Hero_Stealth_Crouch_Walk_Fwd_IP.fbx");
            AnimationClip crawlClip = LoadClipFromFBX("Assets/Animations/Hero/Stealth/Hero_Stealth_Crawl_Fwd_IP.fbx");

            SetOrCreateState(rootStateMachine, "Idle", idleClip, new Vector3(250, 100, 0));
            SetOrCreateState(rootStateMachine, "Walk", walkClip, new Vector3(250, 200, 0));
            SetOrCreateState(rootStateMachine, "Run", runClip, new Vector3(250, 300, 0));
            var climbState = SetOrCreateState(rootStateMachine, "Climb", climbClip, new Vector3(500, 100, 0));
            if (climbState != null) climbState.speed = 1.85f;
            SetOrCreateState(rootStateMachine, "LedgeHang", ledgeHangClip, new Vector3(750, 100, 0));
            SetOrCreateState(rootStateMachine, "Crouch", crouchClip, new Vector3(500, 200, 0));
            SetOrCreateState(rootStateMachine, "CrouchWalk", crouchWalkClip, new Vector3(750, 200, 0));
            SetOrCreateState(rootStateMachine, "Crawl", crawlClip, new Vector3(500, 300, 0));

            // Default state: Idle
            foreach (var state in rootStateMachine.states)
            {
                if (state.state.name == "Idle")
                {
                    rootStateMachine.defaultState = state.state;
                    break;
                }
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void EnsureParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
        {
            foreach (var p in controller.parameters)
            {
                if (p.name == name) return;
            }
            controller.AddParameter(name, type);
        }

        private static AnimatorState SetOrCreateState(AnimatorStateMachine sm, string name, AnimationClip clip, Vector3 pos)
        {
            foreach (var childState in sm.states)
            {
                if (childState.state.name == name)
                {
                    if (clip != null) childState.state.motion = clip;
                    return childState.state;
                }
            }

            var newState = sm.AddState(name, pos);
            if (clip != null) newState.motion = clip;
            return newState;
        }

        public static GameObject CreatePlayerAnPrefab(AnimatorController controller)
        {
            GameObject anModel = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (anModel == null)
            {
                Debug.LogError("[SetupAnPlaygroundScene] Cannot find Model at " + ModelPath);
                return null;
            }

            // Create temporary instance
            GameObject playerGO = (GameObject)PrefabUtility.InstantiatePrefab(anModel);
            playerGO.name = "Player_An";
            playerGO.tag = "Player";
            playerGO.transform.position = Vector3.zero;
            playerGO.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            playerGO.transform.localScale = Vector3.one;

            // CharacterController
            var cc = playerGO.GetComponent<CharacterController>();
            if (cc == null) cc = playerGO.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.stepOffset = 0.35f;

            // Animator
            var anim = playerGO.GetComponent<Animator>();
            if (anim == null) anim = playerGO.AddComponent<Animator>();
            anim.runtimeAnimatorController = controller;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            anim.applyRootMotion = false;

            // Find Avatar from nhan vat chinh 3d.fbx
            var subAssets = AssetDatabase.LoadAllAssetsAtPath(ModelPath);
            foreach (var a in subAssets)
            {
                if (a is Avatar av && av.isValid)
                {
                    anim.avatar = av;
                    break;
                }
            }

            // AnAnimationController
            var aac = playerGO.GetComponent<AnAnimationController>();
            if (aac == null) aac = playerGO.AddComponent<AnAnimationController>();

            // PlayerController
            var pc = playerGO.GetComponent<PlayerController>();
            if (pc == null) pc = playerGO.AddComponent<PlayerController>();

            // LedgeGrabDetector
            var ledge = playerGO.GetComponent<LedgeGrabDetector>();
            if (ledge == null) ledge = playerGO.AddComponent<LedgeGrabDetector>();

            // GrassInteraction
            var grass = playerGO.GetComponent<GrassInteraction>();
            if (grass == null) grass = playerGO.AddComponent<GrassInteraction>();

            // Child CarrySocket
            Transform socket = playerGO.transform.Find("CarrySocket");
            if (socket == null)
            {
                GameObject socketGO = new GameObject("CarrySocket");
                socketGO.transform.SetParent(playerGO.transform, false);
                socketGO.transform.localPosition = new Vector3(0.35f, 1.05f, 0f);
                socket = socketGO.transform;
            }

            // Child TrajectoryPredictor
            Transform traj = playerGO.transform.Find("TrajectoryPredictor");
            if (traj == null)
            {
                GameObject trajGO = new GameObject("TrajectoryPredictor");
                trajGO.transform.SetParent(playerGO.transform, false);
                trajGO.transform.localPosition = Vector3.zero;
                trajGO.AddComponent<TrajectoryPredictor>();
                var lr = trajGO.GetComponent<LineRenderer>();
                if (lr != null)
                {
                    Material orbMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Props/M_Playground_Orb.mat");
                    if (orbMat != null) lr.sharedMaterial = orbMat;
                    lr.enabled = false;
                }
                traj = trajGO.transform;
            }

            // Configure PlayerController references
            SerializedObject so = new SerializedObject(pc);
            var carryProp = so.FindProperty("carrySocket");
            if (carryProp != null) carryProp.objectReferenceValue = socket;
            var trajProp = so.FindProperty("trajectoryPredictor");
            if (trajProp != null) trajProp.objectReferenceValue = traj.GetComponent<TrajectoryPredictor>();
            var walkSpeedProp = so.FindProperty("walkSpeed");
            if (walkSpeedProp != null) walkSpeedProp.floatValue = 2.4f;
            var sprintSpeedProp = so.FindProperty("sprintSpeed");
            if (sprintSpeedProp != null) sprintSpeedProp.floatValue = 4.2f;
            var climbDurProp = so.FindProperty("climbDuration");
            if (climbDurProp != null) climbDurProp.floatValue = 1.05f;
            so.ApplyModifiedProperties();

            // Save Prefab
            string prefDir = Path.GetDirectoryName(PrefabPath);
            if (!Directory.Exists(prefDir)) Directory.CreateDirectory(prefDir);

            GameObject prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(playerGO, PrefabPath, InteractionMode.AutomatedAction);
            Object.DestroyImmediate(playerGO);

            AssetDatabase.SaveAssets();
            return prefab;
        }

        public static void SetupScene(GameObject playerPrefab)
        {
            var scene = EditorSceneManager.OpenScene(SceneAssetPath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError("[SetupAnPlaygroundScene] Failed to open " + SceneAssetPath);
                return;
            }

            // 1. Remove all old Meshy / Tripo / Dummy AI players
            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                if (root == null) continue;
                if (root.name.Contains("Player_MeshyBoy") || root.name.Contains("Meshy") || root.name.Contains("Boy_Character"))
                {
                    Object.DestroyImmediate(root);
                }
            }

            // Also search recursively for any Player tagged objects that are not Player_An
            var allTaggedPlayers = GameObject.FindGameObjectsWithTag("Player");
            foreach (var p in allTaggedPlayers)
            {
                if (p.name != "Player_An")
                {
                    Object.DestroyImmediate(p);
                }
            }

            // 2. Instantiate Player_An if not present
            GameObject existingPlayer = GameObject.Find("Player_An");
            if (existingPlayer == null && playerPrefab != null)
            {
                existingPlayer = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
                existingPlayer.name = "Player_An";
                existingPlayer.tag = "Player";
                existingPlayer.transform.position = new Vector3(-17.5f, 0.05f, 0f);
                existingPlayer.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            }

            // 3. Connect Camera to Player_An
            Camera cam = Camera.main;
            if (cam != null && existingPlayer != null)
            {
                var camCtrl = cam.GetComponent<Cinematic2DCamera>();
                if (camCtrl == null) camCtrl = cam.gameObject.AddComponent<Cinematic2DCamera>();
                camCtrl.Target = existingPlayer.transform;
                camCtrl.ConfigureLevelBounds(-35f, 120f, 1.0f, false);
                camCtrl.SnapToTarget();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, SceneAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static AnimationClip LoadClipFromFBX(string fbxPath, string clipName = null)
        {
            var objects = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            if (objects == null) return null;
            foreach (var obj in objects)
            {
                if (obj is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    if (string.IsNullOrEmpty(clipName) || clip.name.Equals(clipName, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return clip;
                    }
                }
            }
            return null;
        }
    }
}
