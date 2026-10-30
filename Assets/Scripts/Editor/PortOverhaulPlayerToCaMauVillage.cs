using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using HoaVaMau;
using HoaVaMau.Gameplay;
using HoaVaMau.Test3D;

namespace HoaVaMau.EditorScripts
{
    public static class PortOverhaulPlayerToCaMauVillage
    {
        private const string OverhaulScenePath = "Assets/Scenes/New Scene 1_Overhaul.unity";
        private const string CaMauVillageScenePath = "Assets/Scenes/CaMau_1954_Village.unity";
        private const string PlayerMinhPrefabPath = "Assets/Prefabs/Characters/PF_Player_Minh.prefab";
        private const string AnimatorControllerPath = "Assets/Animations/Controllers/AC_Player_An.controller";
        private const string FootstepAudioPath = "Assets/Audio/SFX/Player/SFX_An_Footsteps_Dirt.mp3";

        [MenuItem("Tools/Port Overhaul Player to CaMau Village")]
        public static void Execute()
        {
            Debug.Log("<color=#40b8a6>[PortPlayer]</color> Starting player port from Overhaul to CaMau_1954_Village...");

            // 1. Ensure any changes in active scene are saved cleanly without altering Overhaul
            var currentScene = EditorSceneManager.GetActiveScene();
            if (currentScene.isDirty)
            {
                EditorSceneManager.SaveScene(currentScene);
            }

            // 2. Open CaMau_1954_Village
            var villageScene = EditorSceneManager.OpenScene(CaMauVillageScenePath, OpenSceneMode.Single);
            if (!villageScene.IsValid())
            {
                Debug.LogError($"[PortPlayer] Failed to open scene at path: {CaMauVillageScenePath}");
                return;
            }

            // 3. Find old/bugged player under CaMau_1954_Village/Player
            Transform playerParent = null;
            Vector3 spawnPosition = new Vector3(0.50f, 0.45f, 13.00f);
            Quaternion spawnRotation = Quaternion.identity;

            var existingPlayerGo = GameObject.Find("CaMau_1954_Village/Player/An_Player");
            if (existingPlayerGo == null) existingPlayerGo = GameObject.Find("An_Player");
            if (existingPlayerGo == null) existingPlayerGo = GameObject.FindWithTag("Player");

            if (existingPlayerGo != null)
            {
                spawnPosition = existingPlayerGo.transform.position;
                spawnRotation = existingPlayerGo.transform.rotation;
                playerParent = existingPlayerGo.transform.parent;
                Debug.Log($"[PortPlayer] Found existing player: '{existingPlayerGo.name}', destroying old bugged instance...");
                Undo.DestroyObjectImmediate(existingPlayerGo);
            }

            if (playerParent == null)
            {
                var parentGo = GameObject.Find("CaMau_1954_Village/Player") ?? GameObject.Find("Player");
                if (parentGo != null) playerParent = parentGo.transform;
            }

            // 4. Load official Overhaul Player prefab (PF_Player_Minh)
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerMinhPrefabPath);
            if (playerPrefab == null)
            {
                Debug.LogError($"[PortPlayer] Could not find PF_Player_Minh prefab at: {PlayerMinhPrefabPath}");
                return;
            }

            // 5. Instantiate as a connected prefab instance
            var newPlayer = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, villageScene);
            Undo.RegisterCreatedObjectUndo(newPlayer, "Instantiate Overhaul Player in CaMau Village");
            newPlayer.name = "PF_Player_Minh";
            newPlayer.tag = "Player";
            newPlayer.layer = LayerMask.NameToLayer("Default");

            if (playerParent != null)
            {
                newPlayer.transform.SetParent(playerParent, true);
            }

            // Set spawn transform
            newPlayer.transform.position = spawnPosition;
            newPlayer.transform.rotation = spawnRotation;
            newPlayer.transform.localScale = new Vector3(1.15f, 1.15f, 1.15f);

            // 6. Configure CharacterController
            var cc = newPlayer.GetComponent<CharacterController>();
            if (cc == null) cc = newPlayer.AddComponent<CharacterController>();
            cc.center = new Vector3(0f, 0.75f, 0f);
            cc.radius = 0.30f;
            cc.height = 1.50f;
            cc.stepOffset = 0.35f;
            cc.skinWidth = 0.04f;
            cc.minMoveDistance = 0f;

            // 7. Configure Animator
            var anim = newPlayer.GetComponent<Animator>();
            if (anim != null)
            {
                var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimatorControllerPath);
                if (ctrl != null) anim.runtimeAnimatorController = ctrl;
                anim.applyRootMotion = false;
                anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }

            // 8. Configure 3D Locomotion Controller (Player3DController)
            var p3d = newPlayer.GetComponent<Player3DController>();
            if (p3d == null) p3d = newPlayer.AddComponent<Player3DController>();

            var footstepClip = AssetDatabase.LoadAssetAtPath<AudioClip>(FootstepAudioPath);
            var orbitCam = Object.FindAnyObjectByType<ThirdPersonOrbitCamera>();
            Transform camTransform = orbitCam != null ? orbitCam.transform : (Camera.main != null ? Camera.main.transform : null);

            var soP3D = new SerializedObject(p3d);
            var spMoveSpeed = soP3D.FindProperty("moveSpeed");
            if (spMoveSpeed != null) spMoveSpeed.floatValue = 3.2f;
            var spSprintSpeed = soP3D.FindProperty("sprintSpeed");
            if (spSprintSpeed != null) spSprintSpeed.floatValue = 5.6f;
            var spCrouchSpeed = soP3D.FindProperty("crouchSpeed");
            if (spCrouchSpeed != null) spCrouchSpeed.floatValue = 1.8f;
            var spJumpHeight = soP3D.FindProperty("jumpHeight");
            if (spJumpHeight != null) spJumpHeight.floatValue = 1.25f;
            var spGravity = soP3D.FindProperty("gravity");
            if (spGravity != null) spGravity.floatValue = 22.5f;
            var spRotSpeed = soP3D.FindProperty("rotationSpeed");
            if (spRotSpeed != null) spRotSpeed.floatValue = 12.0f;
            var spAnim = soP3D.FindProperty("animator");
            if (spAnim != null) spAnim.objectReferenceValue = anim;
            var spCam = soP3D.FindProperty("cameraTransform");
            if (spCam != null) spCam.objectReferenceValue = camTransform;
            var spClip = soP3D.FindProperty("footstepDirtClip");
            if (spClip != null) spClip.objectReferenceValue = footstepClip;
            soP3D.ApplyModifiedProperties();

            // 9. Disable 2D controllers on the 3D player to prevent 2.5D Z-lock & facing lock
            var p2d = newPlayer.GetComponent<PlayerController>();
            if (p2d != null)
            {
                p2d.enabled = false;
            }
            var anAnim = newPlayer.GetComponent<AnAnimationController>();
            if (anAnim != null)
            {
                anAnim.enabled = false;
            }

            // 10. Reconnect Scene References
            if (orbitCam != null)
            {
                var soCam = new SerializedObject(orbitCam);
                var spTarget = soCam.FindProperty("target");
                if (spTarget != null)
                {
                    spTarget.objectReferenceValue = newPlayer.transform;
                    soCam.ApplyModifiedProperties();
                }
                orbitCam.Target = newPlayer.transform;
                Debug.Log("[PortPlayer] ThirdPersonOrbitCamera target linked to new player.");
            }

            var interSystem = Object.FindAnyObjectByType<InteractionSystem3D>();
            if (interSystem != null)
            {
                var soInter = new SerializedObject(interSystem);
                var spPlayer = soInter.FindProperty("playerTransform");
                if (spPlayer != null)
                {
                    spPlayer.objectReferenceValue = newPlayer.transform;
                    soInter.ApplyModifiedProperties();
                }
                Debug.Log("[PortPlayer] InteractionSystem3D playerTransform linked to new player.");
            }

            // 11. Mark scene dirty & save
            EditorSceneManager.MarkSceneDirty(villageScene);
            EditorSceneManager.SaveScene(villageScene);

            Debug.Log("<color=#40b8a6>[PortPlayer]</color> SUCCESSFULLY ported Overhaul character (PF_Player_Minh) to CaMau_1954_Village!");
        }
    }
}
