using System.IO;
using UnityEngine;
using UnityEngine.Playables;
using UnityEditor;
using UnityEditor.Animations;
using HoaVaMau.Gameplay;

namespace HoaVaMau.Editor
{
    public static class FixCrouchLocomotionSync
    {
        private const string CrouchIdleFbx = "Assets/Animations/Hero/Stealth/Hero_Stealth_Crouch_Idle_Loop.fbx";
        private const string CrouchWalkFbx = "Assets/Animations/Hero/Stealth/Hero_Stealth_Crouch_Walk_Fwd_IP.fbx";
        private const string OldCrouchedWalkFbx = "Assets/Animations/Hero/Stealth/Crouched Walking.fbx";

        private const string ControllerAnPath = "Assets/Animations/Controllers/AC_Player_An.controller";
        private const string ControllerBoyPath = "Assets/TripoModels/Boy_Character/Boy_AnimatorController.controller";

        private const string PrefabMinhPath = "Assets/Prefabs/Characters/PF_Player_Minh.prefab";
        private const string PrefabAnPath = "Assets/Prefabs/Characters/PF_Player_An.prefab";

        private const string BrainArtifactDir = @"C:\Users\Thanh Tài\.gemini\antigravity\brain\1c9fd682-f664-4aac-9c55-19df1ae698aa";

        [MenuItem("Tools/Sync Crouch Locomotion")]
        public static void SyncAll()
        {
            Debug.Log("<color=#38bdf8>[FixCrouchLocomotionSync]</color> Bắt đầu đồng bộ hóa toàn diện Crouch Locomotion...");

            // 1. Cấu hình ModelImporter FBX Clip Animation
            ConfigureModelImporter(CrouchIdleFbx);
            ConfigureModelImporter(CrouchWalkFbx);
            ConfigureModelImporter(OldCrouchedWalkFbx);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 2. Lấy AnimationClip Hero_Stealth_Crouch_Walk_Fwd_IP
            AnimationClip walkClip = LoadAnimationClip(CrouchWalkFbx);
            AnimationClip idleClip = LoadAnimationClip(CrouchIdleFbx);

            if (walkClip == null)
            {
                Debug.LogError("[FixCrouchLocomotionSync] Không tìm thấy AnimationClip trong: " + CrouchWalkFbx);
            }

            // 3. Cập nhật AnimatorControllers (AC_Player_An & Boy_AnimatorController)
            UpdateAnimatorController(ControllerAnPath, walkClip);
            UpdateAnimatorController(ControllerBoyPath, walkClip);

            // 4. Cập nhật Prefabs (PF_Player_Minh & PF_Player_An)
            UpdatePlayerPrefab(PrefabMinhPath);
            UpdatePlayerPrefab(PrefabAnPath);

            AssetDatabase.SaveAssets();

            // 5. Chụp 2 ảnh Preview góc ngang (Crouch & CrouchWalk)
            CapturePreviews(idleClip, walkClip);

            Debug.Log("<color=#4ade80>[FixCrouchLocomotionSync]</color> Hoàn tất đồng bộ hóa thành công!");
        }

        private static void ConfigureModelImporter(string fbxPath)
        {
            ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogWarning("[FixCrouchLocomotionSync] Không tìm thấy file FBX: " + fbxPath);
                return;
            }

            ModelImporterClipAnimation[] clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
            {
                clips = importer.defaultClipAnimations;
            }

            if (clips == null || clips.Length == 0) return;

            foreach (var clip in clips)
            {
                clip.lockRootHeightY = true;
                clip.keepOriginalPositionY = false; // Based upon feet
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
            Debug.Log($"[FixCrouchLocomotionSync] Đã chuẩn hóa Root Motion Bake Y (Based upon Feet, offset=0) cho {fbxPath}");
        }

        private static AnimationClip LoadAnimationClip(string fbxPath)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (var a in assets)
            {
                if (a is AnimationClip c && !c.name.StartsWith("__preview__"))
                {
                    return c;
                }
            }
            return null;
        }

        private static void UpdateAnimatorController(string controllerPath, AnimationClip newWalkClip)
        {
            AnimatorController ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (ctrl == null) return;

            // Bật iKPass cho các Layer
            for (int i = 0; i < ctrl.layers.Length; i++)
            {
                ctrl.layers[i].iKPass = true;
            }

            // Duyệt các state
            foreach (var layer in ctrl.layers)
            {
                UpdateStateMachine(layer.stateMachine, newWalkClip);
            }

            EditorUtility.SetDirty(ctrl);
            Debug.Log($"[FixCrouchLocomotionSync] Đã gán clip và bật iKOnFeet trong Controller: {controllerPath}");
        }

        private static void UpdateStateMachine(AnimatorStateMachine sm, AnimationClip newWalkClip)
        {
            if (sm == null) return;

            foreach (var childState in sm.states)
            {
                var state = childState.state;
                if (state.name == "Crouch")
                {
                    state.iKOnFeet = true;
                }
                else if (state.name == "CrouchWalk")
                {
                    state.iKOnFeet = true;
                    if (newWalkClip != null)
                    {
                        state.motion = newWalkClip;
                    }
                }
            }

            foreach (var childSm in sm.stateMachines)
            {
                UpdateStateMachine(childSm.stateMachine, newWalkClip);
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
                    if (crouchProp != null)
                    {
                        crouchProp.floatValue = 1.15f;
                    }
                    so.ApplyModifiedProperties();
                }

                var animCtrl = root.GetComponent<AnAnimationController>();
                if (animCtrl == null)
                {
                    animCtrl = root.AddComponent<AnAnimationController>();
                }

                var anim = root.GetComponent<Animator>();
                if (anim != null)
                {
                    anim.applyRootMotion = false;
                    anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Debug.Log($"[FixCrouchLocomotionSync] Đã cập nhật crouchSpeed = 1.15f & AnAnimationController trên Prefab: {prefabPath}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void CapturePreviews(AnimationClip idleClip, AnimationClip walkClip)
        {
            GameObject minhPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabMinhPath);
            if (minhPrefab == null)
            {
                minhPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabAnPath);
            }
            if (minhPrefab == null) return;

            Vector3 studioPos = new Vector3(300f, 50f, 300f);

            GameObject tempChar = (GameObject)PrefabUtility.InstantiatePrefab(minhPrefab);
            tempChar.name = "__TempMinh_Preview__";
            tempChar.transform.position = studioPos;
            tempChar.transform.rotation = Quaternion.Euler(0f, 90f, 0f); // Nhìn ngang sang phải (+X)

            var animCtrl = tempChar.GetComponent<AnAnimationController>();
            if (animCtrl != null) animCtrl.enabled = false;

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.transform.position = studioPos;
            floor.transform.localScale = new Vector3(0.5f, 1f, 0.5f);
            var floorRenderer = floor.GetComponent<Renderer>();
            if (floorRenderer != null)
            {
                var floorMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                floorMat.color = new Color(0.22f, 0.25f, 0.28f);
                floorRenderer.material = floorMat;
            }

            GameObject camGo = new GameObject("__TempPreviewCam__");
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Color;
            cam.backgroundColor = new Color(0.12f, 0.15f, 0.18f);
            cam.transform.position = studioPos + new Vector3(0.0f, 0.65f, -2.4f);
            cam.transform.rotation = Quaternion.identity;
            cam.fieldOfView = 36f;

            GameObject lightGo = new GameObject("__TempPreviewLight__");
            Light dirLight = lightGo.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            lightGo.transform.rotation = Quaternion.Euler(35f, -30f, 0f);
            dirLight.intensity = 1.3f;
            dirLight.color = Color.white;

            RenderTexture rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;

            string screenshotDir = "Assets/Screenshots";
            if (!Directory.Exists(screenshotDir)) Directory.CreateDirectory(screenshotDir);
            if (!Directory.Exists(BrainArtifactDir)) Directory.CreateDirectory(BrainArtifactDir);

            try
            {
                var anim = tempChar.GetComponent<Animator>();

                // 1. Capture Crouch (đứng yên)
                if (anim != null && idleClip != null)
                {
                    var playable = UnityEngine.Playables.AnimationPlayableUtilities.PlayClip(anim, idleClip, out var graph);
                    playable.SetTime(idleClip.length * 0.35f);
                    graph.Evaluate();

                    cam.Render();
                    SaveRenderTextureToPng(rt, $"{screenshotDir}/Minh_Crouch_Preview.png", $"{BrainArtifactDir}/Minh_Crouch_Preview.png");
                    graph.Destroy();
                }

                // 2. Capture CrouchWalk (bước đi - sải bước chân)
                if (anim != null && walkClip != null)
                {
                    var playable = UnityEngine.Playables.AnimationPlayableUtilities.PlayClip(anim, walkClip, out var graph);
                    playable.SetTime(walkClip.length * 0.35f);
                    graph.Evaluate();

                    cam.Render();
                    SaveRenderTextureToPng(rt, $"{screenshotDir}/Minh_CrouchWalk_Preview.png", $"{BrainArtifactDir}/Minh_CrouchWalk_Preview.png");
                    graph.Destroy();
                }
            }
            finally
            {
                cam.targetTexture = null;
                RenderTexture.active = null;
                rt.Release();
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(tempChar);
                Object.DestroyImmediate(floor);
                Object.DestroyImmediate(camGo);
                Object.DestroyImmediate(lightGo);
            }
        }

        private static void SaveRenderTextureToPng(RenderTexture rt, string assetPath, string brainPath)
        {
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;

            Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();

            byte[] bytes = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);
            RenderTexture.active = prev;

            File.WriteAllBytes(assetPath, bytes);
            File.WriteAllBytes(brainPath, bytes);
            AssetDatabase.ImportAsset(assetPath);
            Debug.Log($"[FixCrouchLocomotionSync] Đã lưu ảnh preview: {assetPath}");
        }
    }
}
