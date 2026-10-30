using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using HoaVaMau.Gameplay;
using HoaVaMau.Test3D;

namespace HoaVaMau.Editor
{
    /// <summary>
    /// Công cụ chuẩn hóa DUY NHẤT cho Nhân Vật Chính (PF_Player_Minh / AC_Player_An.controller):
    /// 1. Dọn sạch toàn bộ các file Player Controller bị thừa sau mỗi đợt git pull (tự động remap GUID sang AC_Player_An.controller trước khi xóa).
    /// 2. Đồng bộ chuẩn ModelImporter (Based Upon Feet) cho CrouchIdle, Crouch, Crawl & sub-clip 'climb' của Boy_Character.fbx.
    /// 3. Bật iKPass thật sự trên AC_Player_An.controller và đồng bộ PF_Player_Minh.prefab mà KHÔNG tác động vào New Scene 1_Overhaul.unity.
    /// </summary>
    public static class FixCrouchLocomotionSync
    {
        public const string CanonicalControllerPath = "Assets/Animations/Controllers/AC_Player_An.controller";
        public const string CanonicalPrefabMinhPath = "Assets/Prefabs/Characters/PF_Player_Minh.prefab";
        private const string LegacyPrefabAnPath = "Assets/Prefabs/Characters/PF_Player_An.prefab";

        private const string CrouchIdleFbx = "Assets/Animations/Hero/Stealth/Hero_Stealth_Crouch_Idle_Loop.fbx";
        private const string CrouchWalkFbx = "Assets/Animations/Hero/Stealth/Hero_Stealth_Crouch_Walk_Fwd_IP.fbx";
        private const string CrawlFwdFbx = "Assets/Animations/Hero/Stealth/Hero_Stealth_Crawl_Fwd_IP.fbx";
        private const string LedgeHangFbx = "Assets/Animations/Hero/Traverse/Hero_Traverse_Hanging_Idle_Loop.fbx";
        private const string BoyCharacterFbx = "Assets/TripoModels/Boy_Character/Boy_Character.fbx";

        /// <summary>
        /// Danh sách các AnimatorController (.controller) bị thừa của Player cần tự động remap sang AC_Player_An rồi xóa bỏ.
        /// </summary>
        private static readonly string[] RedundantPlayerControllerPaths = new[]
        {
            "Assets/TripoModels/Boy_Character/Boy_AnimatorController.controller",
            "Assets/Animations/Controllers/AC_Player3D_An.controller",
            "Assets/Animations/Controllers/AC_Player3D_Interaction.controller",
            "Assets/Animations/Controllers/AC_Hero_GapBung.controller",
            "Assets/Animations/Controllers/AC_Hero_GapBungPlayer.controller",
            "Assets/Animations/Hero/An_Hero_AnimatorController.controller",
            "Assets/Models/gap bung.controller",
            "Assets/Models/GapbungPlayerController.controller",
            "Assets/Models/Player3D_InteractionController.controller"
        };

        /// <summary>
        /// Danh sách các Script / Prefab điều khiển Player bị trùng lặp (legacy) cần loại bỏ nếu xuất hiện lại sau khi git pull.
        /// </summary>
        private static readonly string[] RedundantPlayerScriptsAndPrefabs = new[]
        {
            "Assets/Scripts/Gameplay/MeshyBoyAnimationController.cs",
            "Assets/Scripts/Gameplay/GapbungPlayerSetup.cs",
            "Assets/Scripts/Editor/SetupChaRiggedPlayerMenu.cs",
            "Assets/Prefabs/Characters/PF_Player_MeshyBoy.prefab",
            "Assets/Prefabs/Player_MeshyBoy.prefab"
        };

        /// <summary>
        /// Danh sách các Animation Clip (.anim / .fbx) bị thừa, lỗi hoặc trùng lặp cần tự động loại bỏ.
        /// </summary>
        private static readonly string[] RedundantAnimationClipPaths = new[]
        {
            // Clip .anim sinh thủ công cũ bị lỗi lún chân khi ngồi
            "Assets/Animations/Hero/Custom/An_Crouch_Idle.anim",
            "Assets/Animations/Hero/Custom/An_Crouch_Walk.anim",

            // Clip .anim trích xuất trùng lặp dung lượng lớn (~50.4 MB) trong Boy_Character
            "Assets/TripoModels/Boy_Character/Hero_Loco_Idle_Loop_IP.anim",
            "Assets/TripoModels/Boy_Character/Hero_Loco_Walk_Fwd_IP.anim",
            "Assets/TripoModels/Boy_Character/Hero_Loco_Run_Fwd_IP.anim",
            "Assets/TripoModels/Boy_Character/Hero_Traverse_Ledge_ClimbUp.anim",

            // Bản tải thô / trùng lặp (1) trên Mixamo của Crouch & CrouchWalk
            "Assets/Animations/Hero/Stealth/Crouch Walk.fbx",
            "Assets/Animations/Hero/Stealth/Crouched Walking.fbx",
            "Assets/Animations/Hero/Stealth/Crouched Walking (1).fbx",
            "Assets/Animations/Hero/Stealth/Crouching Idle (1).fbx",

            // Clip leo tường Mixamo cũ gây lỗi gập cổ chui vào áo (đã thay bằng Boy_Character.fbx :: climb)
            "Assets/Animations/Hero/Traverse/Hero_Traverse_Braced_Hang_To_Crouch.fbx",

            // Dummy Mixamo T-Pose và clip AI test bắn cung không thuộc bối cảnh dự án
            "Assets/Animations/Hero/Y Bot.fbx",
            "Assets/MeshyImports/Boy Character Rig_20260926_073036/Meshy_AI_Boy_Character_Rig_biped_Animation_Archery_Shot_2_withSkin.fbx"
        };

        [MenuItem("Tools/Hoa Tren Dat Do/Enforce Canonical Player Controller (Post-Pull Cleanup)")]
        public static void EnforceCanonicalPlayerAndCleanRedundant()
        {
            int removedCount = CleanRedundantPlayerControllers();
            SyncAllAssets(captureDefaultPreviews: false);
            Debug.Log($"<color=#4ade80>[PlayerStandard]</color> Hoàn tất chuẩn hóa Player! Đã dọn {removedCount} file thừa (controller/clip) và đồng bộ AC_Player_An.controller + PF_Player_Minh.prefab.");
        }

        [MenuItem("Tools/Sync Crouch Locomotion")]
        public static void SyncAll()
        {
            SyncAllAssets(captureDefaultPreviews: true);
        }

        [MenuItem("Tools/Sync Crouch & Ledge Climb Animations")]
        public static void SyncCrouchAndLedgeClimbMenu()
        {
            SyncAllAssets(captureDefaultPreviews: false);
        }

        public static int CleanRedundantPlayerControllers()
        {
            string canonicalGuid = AssetDatabase.AssetPathToGUID(CanonicalControllerPath);
            if (string.IsNullOrEmpty(canonicalGuid))
            {
                Debug.LogError("[PlayerStandard] Không tìm thấy Controller chuẩn tại: " + CanonicalControllerPath);
                return 0;
            }

            // 1. Thu thập GUID của các .controller thừa hiện đang tồn tại
            var redundantGuids = new HashSet<string>();
            foreach (string path in RedundantPlayerControllerPaths)
            {
                string guid = AssetDatabase.AssetPathToGUID(path);
                if (!string.IsNullOrEmpty(guid) && guid != canonicalGuid && File.Exists(path))
                {
                    redundantGuids.Add(guid);
                }
            }

            // 2. Remap GUID trong các Prefab và Scene phụ (TUYỆT ĐỐI KHÔNG ĐỤNG TỚI New Scene 1_Overhaul.unity - Rule 4.3)
            if (redundantGuids.Count > 0)
            {
                string[] yamlAssetTypes = new[] { "t:Prefab", "t:Scene" };
                foreach (string filter in yamlAssetTypes)
                {
                    foreach (string g in AssetDatabase.FindAssets(filter, new[] { "Assets" }))
                    {
                        string assetPath = AssetDatabase.GUIDToAssetPath(g);
                        if (string.IsNullOrEmpty(assetPath) || !File.Exists(assetPath)) continue;
                        if (assetPath.EndsWith("New Scene 1_Overhaul.unity", System.StringComparison.OrdinalIgnoreCase)) continue;
                        if (assetPath.Contains("/Backups/")) continue;

                        string text = File.ReadAllText(assetPath);
                        bool replaced = false;
                        foreach (string oldGuid in redundantGuids)
                        {
                            if (text.Contains(oldGuid))
                            {
                                text = text.Replace(oldGuid, canonicalGuid);
                                replaced = true;
                            }
                        }

                        if (replaced)
                        {
                            File.WriteAllText(assetPath, text);
                            Debug.Log($"[PlayerStandard] Đã remap tham chiếu AnimatorController thừa -> AC_Player_An trong: {assetPath}");
                        }
                    }
                }
                AssetDatabase.Refresh();
            }

            // 3. Làm sạch Boy_Character_Model.prefab nếu có (gỡ script thừa/missing và gán AC_Player_An)
            const string boyModelPrefabPath = "Assets/TripoModels/Boy_Character/Boy_Character_Model.prefab";
            if (File.Exists(boyModelPrefabPath))
            {
                GameObject root = PrefabUtility.LoadPrefabContents(boyModelPrefabPath);
                if (root != null)
                {
                    try
                    {
                        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);
                        var oldMeshy = root.GetComponent("MeshyBoyAnimationController") as MonoBehaviour;
                        if (oldMeshy != null) Object.DestroyImmediate(oldMeshy);

                        var anim = root.GetComponent<Animator>();
                        var canonicalCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(CanonicalControllerPath);
                        if (anim != null && canonicalCtrl != null)
                        {
                            anim.runtimeAnimatorController = canonicalCtrl;
                            anim.applyRootMotion = false;
                        }
                        PrefabUtility.SaveAsPrefabAsset(root, boyModelPrefabPath);
                    }
                    finally
                    {
                        PrefabUtility.UnloadPrefabContents(root);
                    }
                }
            }

            // 4. Xóa toàn bộ các file .controller thừa và script/prefab Player bị trùng lặp
            int deleted = 0;
            foreach (string path in RedundantPlayerControllerPaths)
            {
                if (File.Exists(path) && AssetDatabase.DeleteAsset(path))
                {
                    Debug.Log($"<color=#f59e0b>[PlayerStandard]</color> Đã xóa AnimatorController thừa: {path}");
                    deleted++;
                }
            }

            foreach (string path in RedundantPlayerScriptsAndPrefabs)
            {
                if (File.Exists(path) && AssetDatabase.DeleteAsset(path))
                {
                    Debug.Log($"<color=#f59e0b>[PlayerStandard]</color> Đã xóa file Player Controller thừa: {path}");
                    deleted++;
                }
            }

            foreach (string path in RedundantAnimationClipPaths)
            {
                if (File.Exists(path) && AssetDatabase.DeleteAsset(path))
                {
                    Debug.Log($"<color=#f59e0b>[PlayerStandard]</color> Đã xóa Animation Clip thừa: {path}");
                    deleted++;
                }
            }

            // Xóa thư mục Materials rỗng đi kèm Y Bot.fbx cũ (nếu còn tồn tại)
            const string yBotMatFolder = "Assets/Animations/Hero/Materials";
            if (AssetDatabase.IsValidFolder(yBotMatFolder) && !File.Exists("Assets/Animations/Hero/Y Bot.fbx"))
            {
                AssetDatabase.DeleteAsset(yBotMatFolder);
            }

            // 5. Dọn sạch Missing Script trên các Prefab nhân vật (nếu vừa gỡ script cũ)
            string[] charPrefabs = new[] { CanonicalPrefabMinhPath, LegacyPrefabAnPath, boyModelPrefabPath };
            foreach (string pfPath in charPrefabs)
            {
                if (!File.Exists(pfPath)) continue;
                GameObject root = PrefabUtility.LoadPrefabContents(pfPath);
                if (root == null) continue;
                try
                {
                    int stripped = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);
                    if (stripped > 0)
                    {
                        PrefabUtility.SaveAsPrefabAsset(root, pfPath);
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 6. Khôi phục tự động nhân vật ở các Scene phụ (CaMau_1954_Village, Prologue...) nếu dính Missing Prefab cũ
            RepairSecondaryScenesMissingPlayer();

            return deleted;
        }

        public static void RepairSecondaryScenesMissingPlayer()
        {
            if (EditorApplication.isPlaying) return;

            // 6A. Sửa Scene 3D CaMau_1954_Village.unity (tuyệt đối không đụng New Scene 1_Overhaul.unity)
            const string villageScenePath = "Assets/Scenes/CaMau_1954_Village.unity";
            if (File.Exists(villageScenePath))
            {
                bool alreadyOpen = false;
                Scene villageScene = SceneManager.GetSceneByPath(villageScenePath);
                if (villageScene.isLoaded)
                {
                    alreadyOpen = true;
                }
                else
                {
                    villageScene = EditorSceneManager.OpenScene(villageScenePath, OpenSceneMode.Additive);
                }

                if (villageScene.IsValid() && villageScene.isLoaded)
                {
                    bool modified = false;
                    Transform playerGroup = null;
                    GameObject anPlayer = null;
                    GameObject villageMainCam = null;
                    ThirdPersonOrbitCamera villageOrbitCam = null;
                    InteractionSystem3D villageInterSys = null;
                    var emptyMissingShells = new List<GameObject>();

                    foreach (GameObject root in villageScene.GetRootGameObjects())
                    {
                        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                        {
                            if (t == null) continue;
                            if (t.name == "Player" && t.parent != null && t.parent.name == "CaMau_1954_Village")
                                playerGroup = t;
                            if (t.name.StartsWith("An_Player"))
                                anPlayer = t.gameObject;
                            if (t.name == "Main Camera" && t.GetComponent<Camera>() != null)
                                villageMainCam = t.gameObject;

                            var orbit = t.GetComponent<ThirdPersonOrbitCamera>();
                            if (orbit != null) villageOrbitCam = orbit;

                            var inter = t.GetComponent<InteractionSystem3D>();
                            if (inter != null) villageInterSys = inter;

                            if (t.gameObject != anPlayer && !t.name.StartsWith("An_Player") &&
                                PrefabUtility.IsPrefabAssetMissing(t.gameObject) &&
                                PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject))
                            {
                                var comps = t.gameObject.GetComponents<Component>();
                                if (comps.Length <= 1 && t.childCount == 0)
                                {
                                    emptyMissingShells.Add(t.gameObject);
                                }
                            }
                        }
                    }

                    foreach (GameObject shell in emptyMissingShells)
                    {
                        if (shell != null)
                        {
                            Object.DestroyImmediate(shell);
                            modified = true;
                        }
                    }

                    bool needsPlayerRebuild = anPlayer == null ||
                                              PrefabUtility.IsPrefabAssetMissing(anPlayer) ||
                                              anPlayer.GetComponentInChildren<SkinnedMeshRenderer>(true) == null;

                    if (needsPlayerRebuild)
                    {
                        Vector3 spawnPos = new Vector3(0.5f, 0.45f, 13.0f);
                        Quaternion spawnRot = Quaternion.Euler(0f, 0f, 0f);
                        if (anPlayer != null)
                        {
                            if (playerGroup == null) playerGroup = anPlayer.transform.parent;
                            Object.DestroyImmediate(anPlayer);
                            anPlayer = null;
                        }

                        GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TripoModels/Boy_Character/Boy_Character_Model.prefab")
                                              ?? AssetDatabase.LoadAssetAtPath<GameObject>(BoyCharacterFbx);
                        if (modelPrefab != null)
                        {
                            anPlayer = playerGroup != null
                                ? (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, playerGroup)
                                : (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, villageScene);

                            anPlayer.name = "An_Player";
                            anPlayer.tag = "Player";
                            anPlayer.SetActive(true);
                            anPlayer.transform.position = spawnPos;
                            anPlayer.transform.rotation = spawnRot;
                            anPlayer.transform.localScale = Vector3.one * 1.25f;
                            modified = true;
                        }
                    }

                    if (anPlayer != null)
                    {
                        anPlayer.tag = "Player";
                        Material boyMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/TripoModels/Boy_Character/Materials/M_Boy_Character.mat");
                        foreach (var smr in anPlayer.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                        {
                            smr.gameObject.SetActive(true);
                            smr.enabled = true;
                            smr.updateWhenOffscreen = true;
                            if (boyMat != null && smr.sharedMaterial != boyMat)
                            {
                                smr.sharedMaterial = boyMat;
                                modified = true;
                            }
                        }

                        Animator anim = anPlayer.GetComponentInChildren<Animator>(true);
                        if (anim != null)
                        {
                            var canonicalCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(CanonicalControllerPath);
                            if (canonicalCtrl != null && anim.runtimeAnimatorController != canonicalCtrl)
                            {
                                anim.runtimeAnimatorController = canonicalCtrl;
                                modified = true;
                            }
                            if (anim.avatar == null || !anim.avatar.isValid)
                            {
                                foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(BoyCharacterFbx))
                                {
                                    if (sub is Avatar av && av.isValid)
                                    {
                                        anim.avatar = av;
                                        modified = true;
                                        break;
                                    }
                                }
                            }
                            anim.applyRootMotion = false;
                            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                        }

                        CharacterController cc = anPlayer.GetComponent<CharacterController>();
                        if (cc == null) { cc = anPlayer.AddComponent<CharacterController>(); modified = true; }
                        cc.height = 1.15f;
                        cc.radius = 0.25f;
                        cc.center = new Vector3(0f, 0.575f, 0f);
                        cc.stepOffset = 0.32f;
                        cc.slopeLimit = 50f;
                        cc.skinWidth = 0.02f;

                        Player3DController p3d = anPlayer.GetComponent<Player3DController>();
                        if (p3d == null) { p3d = anPlayer.AddComponent<Player3DController>(); modified = true; }

                        AudioClip footstepDirt = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_An_Footsteps_Dirt.mp3");
                        p3d.ConfigureFootstepAudio(footstepDirt);

                        var p3dSO = new SerializedObject(p3d);
                        var animProp = p3dSO.FindProperty("animator");
                        if (animProp != null && animProp.objectReferenceValue != anim) { animProp.objectReferenceValue = anim; modified = true; }
                        if (villageMainCam != null)
                        {
                            var camProp = p3dSO.FindProperty("cameraTransform");
                            if (camProp != null && camProp.objectReferenceValue != villageMainCam.transform) { camProp.objectReferenceValue = villageMainCam.transform; modified = true; }
                        }
                        p3dSO.ApplyModifiedPropertiesWithoutUndo();

                        if (villageOrbitCam != null)
                        {
                            villageOrbitCam.Target = anPlayer.transform;
                            var camSO = new SerializedObject(villageOrbitCam);
                            var targetProp = camSO.FindProperty("target");
                            if (targetProp != null && targetProp.objectReferenceValue != anPlayer.transform)
                            {
                                targetProp.objectReferenceValue = anPlayer.transform;
                                modified = true;
                            }
                            camSO.ApplyModifiedPropertiesWithoutUndo();
                            villageOrbitCam.SnapToTarget(0f);
                        }

                        if (villageInterSys != null)
                        {
                            var interSO = new SerializedObject(villageInterSys);
                            var pTransProp = interSO.FindProperty("playerTransform");
                            if (pTransProp != null && pTransProp.objectReferenceValue != anPlayer.transform)
                            {
                                pTransProp.objectReferenceValue = anPlayer.transform;
                                modified = true;
                            }
                            interSO.ApplyModifiedPropertiesWithoutUndo();
                        }
                    }

                    if (modified)
                    {
                        EditorSceneManager.MarkSceneDirty(villageScene);
                        EditorSceneManager.SaveScene(villageScene, villageScenePath);
                        Debug.Log("<color=#4ade80>[PlayerStandard]</color> Đã khôi phục nhân vật 3D An_Player hoàn chỉnh trong CaMau_1954_Village.unity!");
                    }

                    if (!alreadyOpen)
                    {
                        EditorSceneManager.CloseScene(villageScene, true);
                    }
                }
            }

            // 6B. Sửa Scene phụ Scene_Prologue_RiceField_CaMau_Polished.unity nếu có Missing Prefab Player
            const string riceScenePath = "Assets/Scenes/Scene_Prologue_RiceField_CaMau_Polished.unity";
            if (File.Exists(riceScenePath))
            {
                bool alreadyOpen = false;
                Scene riceScene = SceneManager.GetSceneByPath(riceScenePath);
                if (riceScene.isLoaded) alreadyOpen = true;
                else riceScene = EditorSceneManager.OpenScene(riceScenePath, OpenSceneMode.Additive);

                if (riceScene.IsValid() && riceScene.isLoaded)
                {
                    bool modified = false;
                    GameObject minhPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CanonicalPrefabMinhPath);
                    if (minhPrefab != null)
                    {
                        var brokenPlayers = new List<GameObject>();
                        foreach (GameObject root in riceScene.GetRootGameObjects())
                        {
                            if (PrefabUtility.IsPrefabAssetMissing(root) && root.name.Contains("PLAYER"))
                            {
                                brokenPlayers.Add(root);
                            }
                        }

                        foreach (GameObject broken in brokenPlayers)
                        {
                            Vector3 pos = broken.transform.position;
                            Quaternion rot = broken.transform.rotation;
                            string oldName = broken.name.Split(' ')[0];
                            Object.DestroyImmediate(broken);

                            GameObject replacement = (GameObject)PrefabUtility.InstantiatePrefab(minhPrefab, riceScene);
                            replacement.name = oldName;
                            replacement.tag = "Player";
                            replacement.transform.position = pos;
                            replacement.transform.rotation = rot;
                            modified = true;
                        }
                    }

                    if (modified)
                    {
                        EditorSceneManager.MarkSceneDirty(riceScene);
                        EditorSceneManager.SaveScene(riceScene, riceScenePath);
                        Debug.Log("<color=#4ade80>[PlayerStandard]</color> Đã khôi phục nhân vật PF_Player_Minh trong Scene_Prologue_RiceField_CaMau_Polished.unity!");
                    }

                    if (!alreadyOpen)
                    {
                        EditorSceneManager.CloseScene(riceScene, true);
                    }
                }
            }
        }

        public static void SyncAllAssets(bool captureDefaultPreviews = false)
        {
            Debug.Log("<color=#38bdf8>[FixCrouchLocomotionSync]</color> Bắt đầu đồng bộ hóa chuẩn Crouch, CrouchWalk & Ledge Climb...");

            // 1. Chuẩn hóa ModelImporter FBX cho toàn bộ động tác Ngồi / Ngồi đi / Bò trườn (Based Upon Feet, offset = 0)
            ConfigureGroundedLoopImporter(CrouchIdleFbx);
            ConfigureGroundedLoopImporter(CrouchWalkFbx);
            ConfigureGroundedLoopImporter(CrawlFwdFbx);
            ConfigureBoyClimbSubClip(BoyCharacterFbx);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 2. Nạp các AnimationClip chính thức của dự án
            AnimationClip idleClip = LoadAnimationClip(CrouchIdleFbx);
            AnimationClip walkClip = LoadAnimationClip(CrouchWalkFbx);
            AnimationClip crawlClip = LoadAnimationClip(CrawlFwdFbx);
            AnimationClip ledgeHangClip = LoadAnimationClip(LedgeHangFbx);
            AnimationClip climbClip = LoadAnimationClip(BoyCharacterFbx, "climb");

            if (idleClip == null) Debug.LogError("[FixCrouchLocomotionSync] Không tìm thấy CrouchIdle clip tại: " + CrouchIdleFbx);
            if (walkClip == null) Debug.LogError("[FixCrouchLocomotionSync] Không tìm thấy CrouchWalk clip tại: " + CrouchWalkFbx);
            if (climbClip == null) Debug.LogError("[FixCrouchLocomotionSync] Không tìm thấy sub-clip 'climb' tại: " + BoyCharacterFbx);

            // 3. Cập nhật DUY NHẤT AnimatorController chuẩn: AC_Player_An.controller + bật iKPass thật sự trên mảng layers
            UpdateAnimatorController(CanonicalControllerPath, idleClip, walkClip, crawlClip, climbClip, ledgeHangClip);

            // 4. Cập nhật Prefab chuẩn PF_Player_Minh (và PF_Player_An dự phòng) - bảo toàn tuyệt đối Scene hiện hành
            UpdatePlayerPrefab(CanonicalPrefabMinhPath);
            if (File.Exists(LegacyPrefabAnPath))
            {
                UpdatePlayerPrefab(LegacyPrefabAnPath);
            }

            AssetDatabase.SaveAssets();

            if (captureDefaultPreviews)
            {
                CapturePreviews(idleClip, walkClip);
            }

            Debug.Log("<color=#4ade80>[FixCrouchLocomotionSync]</color> Đã hoàn tất đồng bộ hóa CrouchIdle, Crouch & Ledge Climb trên AC_Player_An.controller!");
        }

        private static void ConfigureGroundedLoopImporter(string fbxPath)
        {
            ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer == null) return;

            ModelImporterClipAnimation[] clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
            {
                clips = importer.defaultClipAnimations;
            }
            if (clips == null || clips.Length == 0) return;

            foreach (var clip in clips)
            {
                clip.lockRootHeightY = true;
                clip.keepOriginalPositionY = false; // Based Upon Feet
                clip.heightFromFeet = true;
                clip.heightOffset = 0.0f;
                clip.loopTime = true;
                clip.loopPose = true;
                clip.lockRootRotation = true;
                clip.lockRootPositionXZ = true;
            }

            importer.clipAnimations = clips;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }

        private static void ConfigureBoyClimbSubClip(string fbxPath)
        {
            ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer == null) return;

            ModelImporterClipAnimation[] clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
            {
                clips = importer.defaultClipAnimations;
            }
            if (clips == null || clips.Length == 0) return;

            bool changed = false;
            foreach (var clip in clips)
            {
                if (string.Equals(clip.name, "climb", System.StringComparison.OrdinalIgnoreCase))
                {
                    if (clip.loopTime || !clip.lockRootRotation || !clip.lockRootHeightY || !clip.lockRootPositionXZ)
                    {
                        clip.loopTime = false;
                        clip.loopPose = false;
                        clip.lockRootRotation = true;
                        clip.lockRootHeightY = true;
                        clip.lockRootPositionXZ = true;
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                importer.clipAnimations = clips;
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }
        }

        public static AnimationClip LoadAnimationClip(string fbxPath, string exactName = null)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            if (assets == null) return null;

            foreach (var a in assets)
            {
                if (a is AnimationClip c && !c.name.StartsWith("__preview__"))
                {
                    if (string.IsNullOrEmpty(exactName) || string.Equals(c.name, exactName, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return c;
                    }
                }
            }
            return null;
        }

        private static void UpdateAnimatorController(
            string controllerPath,
            AnimationClip crouchIdleClip,
            AnimationClip crouchWalkClip,
            AnimationClip crawlClip,
            AnimationClip climbClip,
            AnimationClip ledgeHangClip)
        {
            AnimatorController ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (ctrl == null) return;

            // QUAN TRỌNG: ctrl.layers trả về bản sao mảng, bắt buộc phải gán ngược lại ctrl.layers = layers
            var layers = ctrl.layers;
            for (int i = 0; i < layers.Length; i++)
            {
                layers[i].iKPass = true;
            }
            ctrl.layers = layers;

            foreach (var layer in ctrl.layers)
            {
                UpdateStateMachine(layer.stateMachine, crouchIdleClip, crouchWalkClip, crawlClip, climbClip, ledgeHangClip, isRootSm: true);
            }

            EditorUtility.SetDirty(ctrl);
        }

        private static void UpdateStateMachine(
            AnimatorStateMachine sm,
            AnimationClip crouchIdleClip,
            AnimationClip crouchWalkClip,
            AnimationClip crawlClip,
            AnimationClip climbClip,
            AnimationClip ledgeHangClip,
            bool isRootSm)
        {
            if (sm == null) return;

            bool hasCrouchIdle = false;
            bool hasCrouch = false;
            bool hasClimb = false;

            foreach (var childState in sm.states)
            {
                var state = childState.state;
                if (state.name == "CrouchIdle")
                {
                    hasCrouchIdle = true;
                    state.iKOnFeet = true;
                    state.speed = 1f;
                    if (crouchIdleClip != null) state.motion = crouchIdleClip;
                }
                else if (state.name == "Crouch" || state.name == "CrouchWalk")
                {
                    if (state.name == "Crouch") hasCrouch = true;
                    state.iKOnFeet = true;
                    state.speed = 1f;
                    if (crouchWalkClip != null) state.motion = crouchWalkClip;
                }
                else if (state.name == "Crawl")
                {
                    state.iKOnFeet = true;
                    state.speed = 1f;
                    if (crawlClip != null) state.motion = crawlClip;
                }
                else if (state.name == "Climb")
                {
                    hasClimb = true;
                    state.iKOnFeet = false;
                    state.speed = 1f;
                    if (climbClip != null) state.motion = climbClip;
                }
                else if (state.name == "LedgeHang")
                {
                    state.iKOnFeet = false;
                    state.speed = 1f;
                    if (ledgeHangClip != null) state.motion = ledgeHangClip;
                }
            }

            if (isRootSm)
            {
                if (!hasCrouchIdle && crouchIdleClip != null)
                {
                    var st = sm.AddState("CrouchIdle");
                    st.motion = crouchIdleClip;
                    st.iKOnFeet = true;
                    st.speed = 1f;
                }
                if (!hasCrouch && crouchWalkClip != null)
                {
                    var st = sm.AddState("Crouch");
                    st.motion = crouchWalkClip;
                    st.iKOnFeet = true;
                    st.speed = 1f;
                }
                if (!hasClimb && climbClip != null)
                {
                    var st = sm.AddState("Climb");
                    st.motion = climbClip;
                    st.iKOnFeet = false;
                    st.speed = 1f;
                }
            }

            foreach (var childSm in sm.stateMachines)
            {
                UpdateStateMachine(childSm.stateMachine, crouchIdleClip, crouchWalkClip, crawlClip, climbClip, ledgeHangClip, isRootSm: false);
            }
        }

        private static void UpdatePlayerPrefab(string prefabPath)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null) return;

            try
            {
                var pc = root.GetComponent<PlayerController>();
                if (pc != null)
                {
                    var so = new SerializedObject(pc);
                    var crouchProp = so.FindProperty("crouchSpeed");
                    if (crouchProp != null) crouchProp.floatValue = 1.15f;

                    var climbDurProp = so.FindProperty("climbDuration");
                    if (climbDurProp != null) climbDurProp.floatValue = 1.22f;

                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                var animCtrl = root.GetComponent<AnAnimationController>();
                if (animCtrl == null)
                {
                    animCtrl = root.AddComponent<AnAnimationController>();
                }

                var anim = root.GetComponent<Animator>();
                if (anim != null)
                {
                    var canonicalCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(CanonicalControllerPath);
                    if (canonicalCtrl != null) anim.runtimeAnimatorController = canonicalCtrl;
                    anim.applyRootMotion = false;
                    anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void CapturePreviews(AnimationClip idleClip, AnimationClip walkClip)
        {
            GameObject minhPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CanonicalPrefabMinhPath);
            if (minhPrefab == null) return;

            Vector3 studioPos = new Vector3(400f, 120f, 400f);
            var tempObjects = new List<Object>();

            GameObject tempChar = Object.Instantiate(minhPrefab, studioPos, Quaternion.Euler(0f, 90f, 0f));
            tempChar.hideFlags = HideFlags.HideAndDontSave;
            tempObjects.Add(tempChar);

            var animCtrl = tempChar.GetComponent<AnAnimationController>();
            if (animCtrl != null) animCtrl.enabled = false;

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.hideFlags = HideFlags.HideAndDontSave;
            floor.transform.position = studioPos;
            floor.transform.localScale = new Vector3(0.5f, 1f, 0.5f);
            tempObjects.Add(floor);

            var floorRenderer = floor.GetComponent<Renderer>();
            if (floorRenderer != null)
            {
                var floorMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                floorMat.hideFlags = HideFlags.HideAndDontSave;
                floorMat.color = new Color(0.22f, 0.25f, 0.28f);
                floorRenderer.sharedMaterial = floorMat;
                tempObjects.Add(floorMat);
            }

            GameObject camGo = new GameObject("__TempPreviewCam__") { hideFlags = HideFlags.HideAndDontSave };
            tempObjects.Add(camGo);
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.15f, 0.18f);
            cam.transform.position = studioPos + new Vector3(0.0f, 0.65f, -2.4f);
            cam.transform.rotation = Quaternion.identity;
            cam.fieldOfView = 36f;

            GameObject lightGo = new GameObject("__TempPreviewLight__") { hideFlags = HideFlags.HideAndDontSave };
            tempObjects.Add(lightGo);
            Light dirLight = lightGo.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            lightGo.transform.rotation = Quaternion.Euler(35f, -30f, 0f);
            dirLight.intensity = 1.3f;

            RenderTexture rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;
            tempObjects.Add(rt);

            string screenshotDir = "Assets/Screenshots";
            if (!Directory.Exists(screenshotDir)) Directory.CreateDirectory(screenshotDir);

            try
            {
                var anim = tempChar.GetComponent<Animator>();
                if (anim != null && idleClip != null)
                {
                    var playable = AnimationPlayableUtilities.PlayClip(anim, idleClip, out var graph);
                    playable.SetTime(idleClip.length * 0.35f);
                    graph.Evaluate();
                    cam.Render();
                    SaveRenderTextureToJpg(rt, $"{screenshotDir}/Minh_Crouch_Preview.jpg");
                    graph.Destroy();
                }

                if (anim != null && walkClip != null)
                {
                    var playable = AnimationPlayableUtilities.PlayClip(anim, walkClip, out var graph);
                    playable.SetTime(walkClip.length * 0.35f);
                    graph.Evaluate();
                    cam.Render();
                    SaveRenderTextureToJpg(rt, $"{screenshotDir}/Minh_CrouchWalk_Preview.jpg");
                    graph.Destroy();
                }
            }
            finally
            {
                cam.targetTexture = null;
                RenderTexture.active = null;
                rt.Release();
                for (int i = tempObjects.Count - 1; i >= 0; i--)
                {
                    if (tempObjects[i] != null) Object.DestroyImmediate(tempObjects[i]);
                }
            }
        }

        public static string RenderVerificationContactSheet(string outputJpgPath)
        {
            GameObject minhPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CanonicalPrefabMinhPath);
            if (minhPrefab == null) return "Missing PF_Player_Minh.prefab";

            AnimationClip idleStandingClip = LoadAnimationClip(BoyCharacterFbx, "idle") ?? LoadAnimationClip("Assets/Animations/Hero/Locomotion/Hero_Loco_Idle_Loop.fbx");
            AnimationClip climbClip = LoadAnimationClip(BoyCharacterFbx, "climb");
            AnimationClip crouchIdleClip = LoadAnimationClip(CrouchIdleFbx);
            AnimationClip crouchWalkClip = LoadAnimationClip(CrouchWalkFbx);

            Vector3 studioOrigin = new Vector3(450f, 160f, 450f);
            var tempObjects = new List<Object>();

            int totalW = 1280;
            int totalH = 720;
            int cols = 5;
            int rows = 2;
            int cellW = totalW / cols; // 256
            int cellH = totalH / rows; // 360

            Texture2D sheet = new Texture2D(totalW, totalH, TextureFormat.RGB24, false);
            tempObjects.Add(sheet);

            RenderTexture cellRt = new RenderTexture(cellW, cellH, 24);
            tempObjects.Add(cellRt);

            Material cliffMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            cliffMat.hideFlags = HideFlags.HideAndDontSave;
            cliffMat.color = new Color(0.46f, 0.64f, 0.25f);
            tempObjects.Add(cliffMat);

            Material edgeMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            edgeMat.hideFlags = HideFlags.HideAndDontSave;
            edgeMat.color = new Color(0.85f, 0.72f, 0.32f);
            tempObjects.Add(edgeMat);

            GameObject cliffBlock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cliffBlock.hideFlags = HideFlags.HideAndDontSave;
            cliffBlock.GetComponent<Renderer>().sharedMaterial = cliffMat;
            tempObjects.Add(cliffBlock);

            GameObject cornerMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cornerMarker.hideFlags = HideFlags.HideAndDontSave;
            cornerMarker.GetComponent<Renderer>().sharedMaterial = edgeMat;
            tempObjects.Add(cornerMarker);

            GameObject tempChar = Object.Instantiate(minhPrefab, studioOrigin, Quaternion.Euler(0f, 90f, 0f));
            tempChar.hideFlags = HideFlags.HideAndDontSave;
            tempObjects.Add(tempChar);

            foreach (var mb in tempChar.GetComponents<MonoBehaviour>())
            {
                if (mb != null) mb.enabled = false;
            }

            GameObject camGo = new GameObject("__VerifyCam__") { hideFlags = HideFlags.HideAndDontSave };
            tempObjects.Add(camGo);
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.24f, 0.52f, 0.74f);
            cam.orthographic = true;
            cam.targetTexture = cellRt;

            var summary = new System.Text.StringBuilder();

            try
            {
                Animator anim = tempChar.GetComponent<Animator>();
                Transform hips = anim.GetBoneTransform(HumanBodyBones.Hips);
                Transform lFoot = anim.GetBoneTransform(HumanBodyBones.LeftFoot);
                Transform rFoot = anim.GetBoneTransform(HumanBodyBones.RightFoot);
                Transform lHand = anim.GetBoneTransform(HumanBodyBones.LeftHand);
                Transform rHand = anim.GetBoneTransform(HumanBodyBones.RightHand);

                float standingHipOffsetY = anim.humanScale * tempChar.transform.lossyScale.y * 0.955f;

                // --- ROW 1 (Top: y = cellH): Ledge Grab & Climb Trajectory ---
                Vector3 corner = studioOrigin;
                cliffBlock.transform.position = corner + new Vector3(1.0f, -1.2f, 0f);
                cliffBlock.transform.localScale = new Vector3(2.0f, 2.4f, 1.2f);
                cornerMarker.transform.position = corner + new Vector3(0.02f, -0.01f, 0f);
                cornerMarker.transform.localScale = new Vector3(0.05f, 0.03f, 1.25f);

                var climber = new PlayerLedgeClimber(tempChar.transform, null, null, 1.22f, 24f, false);
                Vector3 grabPos = new Vector3(corner.x - 0.24f, corner.y - 0.86f, corner.z);
                Vector3 landPos = new Vector3(corner.x + 0.46f, corner.y, corner.z);
                climber.PrepareLedgeGrab(grabPos, landPos, 1f);

                float[] climbTimes = new[] { 0.00f, 0.25f, 0.50f, 0.72f, 1.00f };
                for (int i = 0; i < cols; i++)
                {
                    float t = climbTimes[i];
                    Vector3 rootPos = (t <= 0.001f) ? grabPos : climber.EvaluateClimbTrajectory(t, 1f);
                    tempChar.transform.position = rootPos;
                    tempChar.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

                    AnimationClip sampleClip = (t >= 0.99f && idleStandingClip != null) ? idleStandingClip : climbClip;
                    float sampleNorm = (t >= 0.99f && idleStandingClip != null) ? 0f : Mathf.Min(t, 0.90f);

                    sampleClip.SampleAnimation(tempChar, sampleNorm * sampleClip.length);
                    tempChar.transform.position = rootPos;
                    tempChar.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

                    Vector3 hipPos = hips.position;
                    hipPos.x = rootPos.x;
                    hipPos.z = rootPos.z;
                    float baseHipY = rootPos.y + standingHipOffsetY;
                    if (t <= 0.001f)
                    {
                        hipPos.y = baseHipY;
                    }
                    else
                    {
                        float minFootRelHipY = Mathf.Min(lFoot.position.y, rFoot.position.y) - hips.position.y;
                        float soleAnkleH = standingHipOffsetY * 0.062f;
                        float footGroundedHipY = rootPos.y + soleAnkleH - minFootRelHipY;
                        float groundLockW = Mathf.SmoothStep(0.48f, 0.64f, t);
                        hipPos.y = Mathf.Lerp(baseHipY, footGroundedHipY, groundLockW);
                    }
                    hips.position = hipPos;

                    float minFootAboveTop = Mathf.Min(lFoot.position.y, rFoot.position.y) - corner.y;
                    float avgHandRelCornerY = 0.5f * (lHand.position.y + rHand.position.y) - corner.y;
                    summary.AppendLine($"Climb t={t:F2}: root=({rootPos.x - corner.x:F2}, {rootPos.y - corner.y:F2}), handRelTopY={avgHandRelCornerY:F2}, minFootRelTopY={minFootAboveTop:F2}");

                    cam.orthographicSize = 1.15f;
                    camGo.transform.position = corner + new Vector3(0.15f, 0.18f, -4.0f);
                    camGo.transform.rotation = Quaternion.identity;
                    cam.Render();
                    BlitCellToSheet(cellRt, sheet, i * cellW, cellH);
                }

                // --- ROW 2 (Bottom: y = 0): StandIdle + CrouchIdle + CrouchWalk (3 phases) ---
                Vector3 floorOrigin = studioOrigin;
                cliffBlock.transform.position = floorOrigin + new Vector3(0f, -0.5f, 0f);
                cliffBlock.transform.localScale = new Vector3(4.0f, 1.0f, 2.0f);
                cornerMarker.transform.position = floorOrigin + new Vector3(0f, 0.005f, -0.55f);
                cornerMarker.transform.localScale = new Vector3(2.5f, 0.01f, 0.02f);

                (string label, AnimationClip clip, float norm)[] crouchSamples = new[]
                {
                    ("StandIdle", idleStandingClip, 0.0f),
                    ("CrouchIdle", crouchIdleClip, 0.35f),
                    ("CrouchWalk_00", crouchWalkClip, 0.00f),
                    ("CrouchWalk_25", crouchWalkClip, 0.25f),
                    ("CrouchWalk_50", crouchWalkClip, 0.50f)
                };

                for (int i = 0; i < cols; i++)
                {
                    var item = crouchSamples[i];
                    tempChar.transform.position = floorOrigin;
                    tempChar.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

                    if (item.clip != null)
                    {
                        item.clip.SampleAnimation(tempChar, item.norm * item.clip.length);
                        tempChar.transform.position = floorOrigin;
                        tempChar.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

                        Vector3 hipPos = hips.position;
                        hipPos.x = floorOrigin.x;
                        hipPos.z = floorOrigin.z;
                        hips.position = hipPos;

                        if (i >= 1)
                        {
                            Transform head = anim.GetBoneTransform(HumanBodyBones.Head);
                            if (head != null)
                            {
                                Quaternion targetLook = Quaternion.LookRotation(Vector3.right, Vector3.up);
                                head.rotation = Quaternion.Slerp(head.rotation, targetLook, 0.65f);
                            }
                        }

                        float lFootY = lFoot.position.y - floorOrigin.y;
                        float rFootY = rFoot.position.y - floorOrigin.y;
                        float hipY = hips.position.y - floorOrigin.y;
                        summary.AppendLine($"{item.label}: hipY={hipY:F3}, LFootY={lFootY:F3}, RFootY={rFootY:F3}");

                        cam.orthographicSize = 0.85f;
                        camGo.transform.position = floorOrigin + new Vector3(0.05f, 0.62f, -4.0f);
                        camGo.transform.rotation = Quaternion.identity;
                        cam.Render();
                        BlitCellToSheet(cellRt, sheet, i * cellW, 0);
                    }
                }

                sheet.Apply();
                string dir = Path.GetDirectoryName(outputJpgPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllBytes(outputJpgPath, sheet.EncodeToJPG(80));
            }
            finally
            {
                cam.targetTexture = null;
                RenderTexture.active = null;
                cellRt.Release();
                for (int i = tempObjects.Count - 1; i >= 0; i--)
                {
                    if (tempObjects[i] != null) Object.DestroyImmediate(tempObjects[i]);
                }
            }

            return summary.ToString();
        }

        private static void BlitCellToSheet(RenderTexture rt, Texture2D sheet, int destX, int destY)
        {
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            sheet.ReadPixels(new Rect(0, 0, rt.width, rt.height), destX, destY);
            RenderTexture.active = prev;
        }

        private static void SaveRenderTextureToJpg(RenderTexture rt, string assetPath)
        {
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;

            Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();

            byte[] bytes = tex.EncodeToJPG(80);
            Object.DestroyImmediate(tex);
            RenderTexture.active = prev;

            File.WriteAllBytes(assetPath, bytes);
            AssetDatabase.ImportAsset(assetPath);
        }
    }
}
