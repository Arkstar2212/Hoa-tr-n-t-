using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Collections.Generic;

namespace HoaVaMau.Editor
{
    /// <summary>
    /// Công cụ Editor tự động:
    /// 1. Cấu hình Rig Humanoid & Loop Time cho toàn bộ Animation Mixamo và Model NPC 3D (bảo toàn cài đặt Based Upon Feet đã tinh chỉnh).
    /// 2. Tạo bộ Animator Controller cho NPC (AC_NPC_Female, AC_NPC_Male, AC_NPC_ElderlyMan, AC_NPC_Social_Waving).
    /// 3. Tự động gắn Animator + Avatar + Controller vào các Prefab NPC hiện có và tạo Prefab NPC sẵn sàng diễn hoạt.
    /// </summary>
    public static class ConfigureImportedMixamoAnimations
    {
        private const string AutoSyncSessionKey = "HoaVaMau_NPC_Animations_Synced_v4";

        [InitializeOnLoadMethod]
        private static void OnEditorLoadedAutoSync()
        {
            if (SessionState.GetBool(AutoSyncSessionKey, false)) return;
            SessionState.SetBool(AutoSyncSessionKey, true);

            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
                ConfigureAnimationAndModelRigs();
                BuildNPCControllersAndBindPrefabs();
                AssetDatabase.SaveAssets();
            };
        }

        [MenuItem("Tools/Hoa Va Mau/Cấu hình Animation Humanoid & Gắn Vào Toàn Bộ NPC")]
        public static void ConfigureAllAndBindToNPCs()
        {
            int animCount = ConfigureAnimationAndModelRigs();
            int npcCount = BuildNPCControllersAndBindPrefabs();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=#4ade80>[HoaVaMau]</color> Đã cấu hình {animCount} FBX Humanoid và gắn Animation Controller cho {npcCount} Prefab NPC!");
            EditorUtility.DisplayDialog(
                "Hoàn tất Gắn Animation NPC",
                $"Đã cấu hình Humanoid cho {animCount} file FBX và tự động gắn Animator Controller + Avatar vào {npcCount} NPC Prefab!",
                "OK");
        }

        private static int ConfigureAnimationAndModelRigs()
        {
            string[] animFolders = new string[]
            {
                "Assets/Animations/Hero",
                "Assets/Animations/NPCs",
                "Assets/Animations/Enemy"
            };

            string[] npcModelFolders = new string[]
            {
                "Assets/Models/Characters/NPCs",
                "Assets/TripoModels"
            };

            int count = 0;

            AssetDatabase.StartAssetEditing();
            try
            {
                // 1. Configure Animation FBX files (preserving existing calibrated clip settings)
                string[] animGuids = AssetDatabase.FindAssets("t:Model", animFolders);
                foreach (string guid in animGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase)) continue;
                    if (path.Contains("Character_An.fbx") || path.Contains("Y Bot.fbx")) continue;

                    var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                    if (importer == null) continue;

                    bool modified = false;

                    if (importer.animationType != ModelImporterAnimationType.Human)
                    {
                        importer.animationType = ModelImporterAnimationType.Human;
                        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                        modified = true;
                    }

                    bool usedDefaults = false;
                    var clips = importer.clipAnimations;
                    if (clips == null || clips.Length == 0)
                    {
                        clips = importer.defaultClipAnimations;
                        usedDefaults = true;
                    }

                    if (clips != null && clips.Length > 0)
                    {
                        string fileName = Path.GetFileNameWithoutExtension(path);
                        bool isTransition = fileName.Contains("_To_", System.StringComparison.OrdinalIgnoreCase);

                        bool shouldLoop = !isTransition && (
                                          fileName.Contains("Loop", System.StringComparison.OrdinalIgnoreCase) ||
                                          fileName.Contains("Idle", System.StringComparison.OrdinalIgnoreCase) ||
                                          fileName.Contains("Walk", System.StringComparison.OrdinalIgnoreCase) ||
                                          fileName.Contains("Crawl", System.StringComparison.OrdinalIgnoreCase) ||
                                          fileName.Contains("Crouch", System.StringComparison.OrdinalIgnoreCase) ||
                                          fileName.Contains("Sneak", System.StringComparison.OrdinalIgnoreCase) ||
                                          fileName.Contains("Run", System.StringComparison.OrdinalIgnoreCase) ||
                                          fileName.Contains("Waving", System.StringComparison.OrdinalIgnoreCase) ||
                                          fileName.Contains("Crying", System.StringComparison.OrdinalIgnoreCase) ||
                                          fileName.Contains("Talking", System.StringComparison.OrdinalIgnoreCase));

                        bool isAerialOrHang = fileName.Contains("Hanging", System.StringComparison.OrdinalIgnoreCase) ||
                                              fileName.Contains("Jump", System.StringComparison.OrdinalIgnoreCase) ||
                                              fileName.Contains("Fall", System.StringComparison.OrdinalIgnoreCase) ||
                                              fileName.Contains("Traverse", System.StringComparison.OrdinalIgnoreCase);

                        bool isGroundedStealthOrLoco = !isAerialOrHang && (
                                                       fileName.Contains("Crouch", System.StringComparison.OrdinalIgnoreCase) ||
                                                       fileName.Contains("Crawl", System.StringComparison.OrdinalIgnoreCase) ||
                                                       fileName.Contains("Sneak", System.StringComparison.OrdinalIgnoreCase) ||
                                                       fileName.Contains("Walk", System.StringComparison.OrdinalIgnoreCase) ||
                                                       fileName.Contains("Run", System.StringComparison.OrdinalIgnoreCase) ||
                                                       fileName.Contains("Idle", System.StringComparison.OrdinalIgnoreCase));

                        bool clipChanged = usedDefaults;
                        foreach (var clip in clips)
                        {
                            if (clip.name != fileName) { clip.name = fileName; clipChanged = true; }
                            if (shouldLoop && (!clip.loopTime || !clip.loopPose))
                            {
                                clip.loopTime = true;
                                clip.loopPose = true;
                                clipChanged = true;
                            }
                            if (!clip.lockRootRotation) { clip.lockRootRotation = true; clipChanged = true; }
                            if (!clip.lockRootHeightY) { clip.lockRootHeightY = true; clipChanged = true; }
                            if (!clip.lockRootPositionXZ) { clip.lockRootPositionXZ = true; clipChanged = true; }

                            if (isGroundedStealthOrLoco && (clip.keepOriginalPositionY || !clip.heightFromFeet))
                            {
                                clip.keepOriginalPositionY = false; // Based Upon Feet
                                clip.heightFromFeet = true;
                                clip.heightOffset = 0f;
                                clipChanged = true;
                            }
                        }

                        if (clipChanged)
                        {
                            importer.clipAnimations = clips;
                            modified = true;
                        }
                    }

                    if (modified)
                    {
                        importer.SaveAndReimport();
                        count++;
                    }
                }

                // 2. Configure NPC Character Model FBX files to Humanoid
                string[] modelGuids = AssetDatabase.FindAssets("t:Model", npcModelFolders);
                foreach (string guid in modelGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase)) continue;
                    if (path.Contains("rock_outcrop") || path.Contains("Character_NPC_02")) continue;

                    var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                    if (importer == null) continue;

                    if (importer.animationType != ModelImporterAnimationType.Human ||
                        importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
                    {
                        importer.animationType = ModelImporterAnimationType.Human;
                        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                        importer.SaveAndReimport();
                        count++;
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            return count;
        }

        private static int BuildNPCControllersAndBindPrefabs()
        {
            const string controllerDir = "Assets/Animations/Controllers";
            if (!AssetDatabase.IsValidFolder(controllerDir))
            {
                Directory.CreateDirectory(controllerDir);
            }

            // Load NPC Animation Clips
            AnimationClip femCry = LoadClipFromFbx("Assets/Animations/NPCs/Female/NPC_Female_Crying_Loop.fbx");
            AnimationClip femTalk1 = LoadClipFromFbx("Assets/Animations/NPCs/Female/NPC_Female_Talking_01.fbx");
            AnimationClip femTalk2 = LoadClipFromFbx("Assets/Animations/NPCs/Female/NPC_Female_Talking_02.fbx");

            AnimationClip oldIdle = LoadClipFromFbx("Assets/Animations/NPCs/Male/NPC_Male_OldMan_Idle_Loop.fbx");
            AnimationClip oldWalk = LoadClipFromFbx("Assets/Animations/NPCs/Male/NPC_Male_OldMan_Walk_Loop.fbx");
            AnimationClip maleTalk1 = LoadClipFromFbx("Assets/Animations/NPCs/Male/NPC_Male_Talking_01.fbx");
            AnimationClip maleTalk2 = LoadClipFromFbx("Assets/Animations/NPCs/Male/NPC_Male_Talking_02.fbx");

            AnimationClip wave1 = LoadClipFromFbx("Assets/Animations/NPCs/Social/NPC_Social_Waving_01.fbx");
            AnimationClip wave2 = LoadClipFromFbx("Assets/Animations/NPCs/Social/NPC_Social_Waving_02.fbx");
            AnimationClip wave3 = LoadClipFromFbx("Assets/Animations/NPCs/Social/NPC_Social_Waving_03.fbx");

            // 1. Build Controllers
            AnimatorController acElderly = CreateLoopingController(
                $"{controllerDir}/AC_NPC_ElderlyMan.controller",
                new (string, AnimationClip)[] {
                    ("OldMan_Idle", oldIdle),
                    ("OldMan_Talking", maleTalk1),
                    ("OldMan_Walk", oldWalk)
                },
                chainDefaultStates: false);

            AnimatorController acFemale = CreateLoopingController(
                $"{controllerDir}/AC_NPC_Female.controller",
                new (string, AnimationClip)[] {
                    ("Female_Talking_01", femTalk1),
                    ("Female_Talking_02", femTalk2),
                    ("Female_Waving", wave1),
                    ("Female_Crying", femCry)
                },
                chainDefaultStates: true);

            AnimatorController acMale = CreateLoopingController(
                $"{controllerDir}/AC_NPC_Male.controller",
                new (string, AnimationClip)[] {
                    ("Male_Talking_01", maleTalk1),
                    ("Male_Talking_02", maleTalk2),
                    ("Male_Waving", wave2)
                },
                chainDefaultStates: true);

            AnimatorController acSocialWave = CreateLoopingController(
                $"{controllerDir}/AC_NPC_Social_Waving.controller",
                new (string, AnimationClip)[] {
                    ("Waving_01", wave1),
                    ("Waving_02", wave2),
                    ("Waving_03", wave3)
                },
                chainDefaultStates: true);

            int boundCount = 0;

            // 2. Bind to existing 3D Character Prefabs in Assets/Prefabs/Characters
            boundCount += BindAnimatorToExistingPrefab(
                "Assets/Prefabs/Characters/PF_3D_Elderly_Man.prefab",
                "Assets/TripoModels/Elderly_Man/Elderly_Man.fbx",
                "Assets/TripoModels/Elderly_Man/Materials/M_Elderly_Man.mat",
                acElderly);

            boundCount += BindAnimatorToExistingPrefab(
                "Assets/Prefabs/Characters/PF_3D_Boy_SingleBun.prefab",
                "Assets/TripoModels/Boy_SingleBun/Boy_SingleBun.fbx",
                "Assets/TripoModels/Boy_SingleBun/Materials/M_Boy_SingleBun.mat",
                acSocialWave);

            boundCount += BindAnimatorToExistingPrefab(
                "Assets/Prefabs/Characters/PF_3D_Baby_Character.prefab",
                "Assets/TripoModels/Baby_Character/Baby_Character.fbx",
                "Assets/TripoModels/Baby_Character/Materials/M_Baby_Character.mat",
                acSocialWave);

            // 3. Create / Update ready-to-use Animated NPC Prefabs in Assets/Prefabs/Characters/NPCs
            const string npcPrefabDir = "Assets/Prefabs/Characters/NPCs";
            if (!AssetDatabase.IsValidFolder(npcPrefabDir))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs/Characters", "NPCs");
            }

            var npcDefinitions = new (string prefabName, string fbxPath, string matPath, AnimatorController controller)[]
            {
                ("PF_NPC_AnMother", "Assets/Models/Characters/NPCs/Character_AnMother_35.fbx", "Assets/Models/Characters/NPCs/Materials/M_Character_AnMother_35.mat", acFemale),
                ("PF_NPC_Female_VN", "Assets/TripoModels/female_Vietnamese/female_Vietnamese.fbx", "Assets/TripoModels/female_Vietnamese/Materials/female_Vietnamese.mat", acFemale),
                ("PF_NPC_Female_War", "Assets/TripoModels/character_3d_model_femela_in_war/character_3d_model_femela_in_war.fbx", "Assets/TripoModels/character_3d_model_femela_in_war/Materials/character_3d_model_femela_in_war.mat", acFemale),
                ("PF_NPC_Elderly_Man", "Assets/TripoModels/Elderly_Man/Elderly_Man.fbx", "Assets/TripoModels/Elderly_Man/Materials/M_Elderly_Man.mat", acElderly),
                ("PF_NPC_Quiet_Child", "Assets/TripoModels/Meshy_AI_Quiet_Village_Child_1008013430_texture/Meshy_AI_Quiet_Village_Child_1008013430_texture.fbx", "Assets/TripoModels/Meshy_AI_Quiet_Village_Child_1008013430_texture/Materials/Meshy_AI_Quiet_Village_Child_1008013430_texture.mat", acSocialWave),
                ("PF_NPC_Villager_01", "Assets/Models/Characters/NPCs/Character_NPC_01.fbx", "Assets/Models/Characters/NPCs/Materials/M_Character_NPC_01.mat", acMale),
                ("PF_NPC_Villager_02", "Assets/Models/Characters/NPCs/Character_NPC_02.fbx", "Assets/Models/Characters/NPCs/Materials/M_Character_NPC_02.mat", null),
                ("PF_NPC_Villager_03", "Assets/Models/Characters/NPCs/Character_NPC_03.fbx", "Assets/Models/Characters/NPCs/Materials/M_Character_NPC_03.mat", acFemale),
                ("PF_NPC_Villager_04", "Assets/Models/Characters/NPCs/Character_NPC_04.fbx", "Assets/Models/Characters/NPCs/Materials/M_Character_NPC_04.mat", acMale),
                ("PF_NPC_Villager_05", "Assets/Models/Characters/NPCs/Character_NPC_05.fbx", "Assets/Models/Characters/NPCs/Materials/M_Character_NPC_05.mat", acSocialWave),
                ("PF_NPC_Villager_06", "Assets/Models/Characters/NPCs/Character_NPC_06.fbx", "Assets/Models/Characters/NPCs/Materials/M_Character_NPC_06.mat", acFemale),
                ("PF_NPC_Villager_07", "Assets/Models/Characters/NPCs/Character_NPC_07.fbx", "Assets/Models/Characters/NPCs/Materials/M_Character_NPC_07.mat", acMale),
                ("PF_NPC_Villager_08", "Assets/Models/Characters/NPCs/Character_NPC_08.fbx", "Assets/Models/Characters/NPCs/Materials/M_Character_NPC_08.mat", acSocialWave),
                ("PF_NPC_Villager_09", "Assets/Models/Characters/NPCs/Character_NPC_09.fbx", "Assets/Models/Characters/NPCs/Materials/M_Character_NPC_09.mat", acMale),
            };

            foreach (var def in npcDefinitions)
            {
                if (CreateOrUpdateAnimatedNPCPrefab($"{npcPrefabDir}/{def.prefabName}.prefab", def.fbxPath, def.matPath, def.controller))
                {
                    boundCount++;
                }
            }

            return boundCount;
        }

        private static AnimationClip LoadClipFromFbx(string fbxPath)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            if (assets == null) return null;
            foreach (Object asset in assets)
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    return clip;
                }
            }
            return null;
        }

        private static Avatar LoadAvatarFromFbx(string fbxPath)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            if (assets == null) return null;
            foreach (Object asset in assets)
            {
                if (asset is Avatar avatar)
                {
                    return avatar;
                }
            }
            return null;
        }

        private static AnimatorController CreateLoopingController(
            string path,
            (string stateName, AnimationClip clip)[] states,
            bool chainDefaultStates)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            }

            var sm = controller.layers[0].stateMachine;
            var existingStates = sm.states;
            for (int i = existingStates.Length - 1; i >= 0; i--)
            {
                sm.RemoveState(existingStates[i].state);
            }

            List<AnimatorState> createdStates = new List<AnimatorState>();
            for (int i = 0; i < states.Length; i++)
            {
                if (states[i].clip == null) continue;
                AnimatorState st = sm.AddState(states[i].stateName, new Vector3(280, i * 70, 0));
                st.motion = states[i].clip;
                createdStates.Add(st);
            }

            if (createdStates.Count > 0)
            {
                sm.defaultState = createdStates[0];
                if (chainDefaultStates && createdStates.Count >= 2)
                {
                    var t1 = createdStates[0].AddTransition(createdStates[1]);
                    t1.hasExitTime = true;
                    t1.exitTime = 2.0f;
                    t1.duration = 0.4f;

                    var t2 = createdStates[1].AddTransition(createdStates[0]);
                    t2.hasExitTime = true;
                    t2.exitTime = 2.0f;
                    t2.duration = 0.4f;
                }
            }

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static int BindAnimatorToExistingPrefab(string prefabPath, string sourceFbxPath, string materialPath, AnimatorController controller)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null || controller == null) return 0;

            Avatar avatar = LoadAvatarFromFbx(sourceFbxPath);
            Material mat = !string.IsNullOrEmpty(materialPath) ? AssetDatabase.LoadAssetAtPath<Material>(materialPath) : null;
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null) return 0;

            Animator anim = root.GetComponent<Animator>();
            if (anim == null) anim = root.AddComponent<Animator>();

            anim.runtimeAnimatorController = controller;
            if (avatar != null) anim.avatar = avatar;
            anim.applyRootMotion = false;

            if (mat != null)
            {
                foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
                {
                    r.sharedMaterial = mat;
                }
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            return 1;
        }

        private static bool CreateOrUpdateAnimatedNPCPrefab(string prefabPath, string sourceFbxPath, string materialPath, AnimatorController controller)
        {
            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(sourceFbxPath);
            if (modelPrefab == null) return false;

            Avatar avatar = LoadAvatarFromFbx(sourceFbxPath);
            Material mat = !string.IsNullOrEmpty(materialPath) ? AssetDatabase.LoadAssetAtPath<Material>(materialPath) : null;
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab);
            if (instance == null) return false;

            instance.name = Path.GetFileNameWithoutExtension(prefabPath);

            if (controller != null)
            {
                Animator anim = instance.GetComponent<Animator>();
                if (anim == null) anim = instance.AddComponent<Animator>();

                anim.runtimeAnimatorController = controller;
                if (avatar != null) anim.avatar = avatar;
                anim.applyRootMotion = false;
            }

            if (mat != null)
            {
                foreach (Renderer r in instance.GetComponentsInChildren<Renderer>(true))
                {
                    r.sharedMaterial = mat;
                }
            }

            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Object.DestroyImmediate(instance);
            return true;
        }
    }
}
