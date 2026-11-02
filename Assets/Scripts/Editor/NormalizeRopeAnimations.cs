using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using HoaVaMau.Gameplay;

namespace HoaVaMau.Editor
{
    public static class NormalizeRopeAnimations
    {
        private const string HeroAnimDir = "Assets/Animations/Hero";
        private const string TraverseDir = "Assets/Animations/Hero/Traverse";
        private const string ControllerPath = "Assets/Animations/Controllers/AC_Player_An.controller";

        private const string ClimbFbx = "Assets/Animations/Hero/Traverse/Hero_Traverse_Rope_Climb.fbx";
        private const string SwingFbx = "Assets/Animations/Hero/Traverse/Hero_Traverse_Rope_Swinging.fbx";
        private const string ClimbDownFbx = "Assets/Animations/Hero/Traverse/Hero_Traverse_Climb_Down_IP.fbx";
        private const string HangIdleFbx = "Assets/Animations/Hero/Traverse/Hero_Traverse_Hanging_Idle_Loop.fbx";

        [MenuItem("Tools/Hoa Tren Dat Do/Normalize Rope Animations")]
        public static void Execute()
        {
            Debug.Log("<color=#38bdf8>[NormalizeRopeAnimations]</color> Bắt đầu nâng cấp toàn diện cơ chế & animation leo đu dây...");

            // 1. Cấu hình ModelImporter cho toàn bộ 4 animation
            ConfigureImporter(ClimbFbx, "Hero_Traverse_Rope_Climb", true);
            ConfigureImporter(SwingFbx, "Hero_Traverse_Rope_Swinging", true);
            ConfigureImporter(ClimbDownFbx, "Hero_Traverse_Climb_Down_IP", true);
            ConfigureImporter(HangIdleFbx, "Hero_Traverse_Hanging_Idle_Loop", true);

            AssetDatabase.Refresh();

            // 2. Tải các AnimationClip chuẩn Humanoid
            AnimationClip climbClip = LoadHumanoidClip(ClimbFbx);
            AnimationClip swingClip = LoadHumanoidClip(SwingFbx);
            AnimationClip climbDownClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/Hero/Custom/An_Rope_Climb_Down_Reversed.anim");
            if (climbDownClip == null) climbDownClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/Hero/Custom/An_Rope_Climb_Down.anim");
            if (climbDownClip == null) climbDownClip = LoadHumanoidClip(ClimbDownFbx);
            AnimationClip hangIdleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/Hero/Custom/An_Rope_Hang_Idle.anim");
            if (hangIdleClip == null) hangIdleClip = LoadHumanoidClip(HangIdleFbx);

            // 3. Cập nhật vào Animator Controller (AC_Player_An)
            UpdateAnimatorController(climbClip, swingClip, climbDownClip, hangIdleClip);

            // 4. Cập nhật RopeController trong scene hiện tại
            UpdateSceneRopeControllers();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=#4ade80>[NormalizeRopeAnimations]</color> <b>HOÀN THÀNH: Đã phân tách RopeHang (tĩnh) vs RopeSwing (đu đà), sửa leo lên/xuống linh hoạt!</b>");
        }

        private static void ConfigureImporter(string assetPath, string clipName, bool isLooping)
        {
            if (!File.Exists(assetPath)) return;

            var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null) return;

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importAnimation = true;

            var defaultClips = importer.defaultClipAnimations;
            ModelImporterClipAnimation clipAnim = (defaultClips != null && defaultClips.Length > 0)
                ? defaultClips[0]
                : new ModelImporterClipAnimation();

            clipAnim.name = clipName;
            clipAnim.takeName = "mixamo.com";
            clipAnim.loopTime = isLooping;
            clipAnim.loopPose = isLooping;

            clipAnim.lockRootRotation = true;
            clipAnim.keepOriginalOrientation = true;

            clipAnim.lockRootHeightY = true;
            clipAnim.keepOriginalPositionY = true;

            clipAnim.lockRootPositionXZ = true;
            clipAnim.keepOriginalPositionXZ = true;

            importer.clipAnimations = new[] { clipAnim };
            importer.SaveAndReimport();
            Debug.Log($"[NormalizeRopeAnimations] Configured Humanoid & Bake Into Pose for '{assetPath}'");
        }

        private static AnimationClip LoadHumanoidClip(string fbxPath)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (var a in assets)
            {
                if (a is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    return clip;
                }
            }
            return null;
        }

        private static void UpdateAnimatorController(
            AnimationClip climbClip,
            AnimationClip swingClip,
            AnimationClip climbDownClip,
            AnimationClip hangIdleClip)
        {
            var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (ctrl == null)
            {
                Debug.LogError($"[NormalizeRopeAnimations] Không tìm thấy controller tại {ControllerPath}");
                return;
            }

            var rootSm = ctrl.layers[0].stateMachine;

            // Tìm hoặc tạo state RopeSwing
            AnimatorState ropeSwingState = null;
            foreach (var child in rootSm.states)
            {
                if (child.state.name == "RopeSwing")
                {
                    ropeSwingState = child.state;
                    break;
                }
            }
            if (ropeSwingState == null)
            {
                ropeSwingState = rootSm.AddState("RopeSwing", new Vector3(500, 200, 0));
                Debug.Log("[NormalizeRopeAnimations] Đã tạo mới State 'RopeSwing' trong Animator.");
            }

            // Gán clip cho RopeSwing (Dùng chung HangIdle tĩnh để loại bỏ chuyển động quẫy chân lệch nhịp của Mixamo)
            ropeSwingState.motion = hangIdleClip;
            ropeSwingState.speed = 1.0f;

            // Cập nhật các states còn lại
            foreach (var childState in rootSm.states)
            {
                var state = childState.state;

                // 1. RopeHang: Bám tĩnh lơ lửng buông chân thở nhẹ (KHÔNG vung chân)
                if (state.name == "RopeHang")
                {
                    state.motion = hangIdleClip != null ? hangIdleClip : swingClip;
                    state.speed = 1.0f;
                    Debug.Log("[NormalizeRopeAnimations] RopeHang: gán clip tĩnh Hanging Idle (không bị vung lắc bừa bãi).");
                }
                // 2. RopeClimbUp: Trèo lên dứt khoát
                else if (state.name == "RopeClimbUp" || state.name == "RopeClimb")
                {
                    state.motion = climbClip;
                    state.speed = 1.15f;
                }
                // 3. RopeClimbDown: Trèo tụt xuống mượt mà (dùng clip ClimbDown chuyên trách)
                else if (state.name == "RopeClimbDown")
                {
                    state.motion = climbDownClip != null ? climbDownClip : climbClip;
                    state.speed = 1.15f; // Chạy thuận chiều tốc độ 1.15x
                    Debug.Log("[NormalizeRopeAnimations] RopeClimbDown: gán clip Hero_Traverse_Climb_Down_IP thuận chiều.");
                }
            }

            EditorUtility.SetDirty(ctrl);
        }

        private static void UpdateSceneRopeControllers()
        {
            var ropes = Object.FindObjectsByType<RopeController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var r in ropes)
            {
                var so = new SerializedObject(r);
                var speedProp = so.FindProperty("maxSwingSpeed");
                var angleProp = so.FindProperty("maxSwingAngle");
                var forceProp = so.FindProperty("playerSwingForce");
                var momProp = so.FindProperty("momentumTransferRatio");
                var widthProp = so.FindProperty("ropeVisualWidth");
                var matProp = so.FindProperty("ropeMaterial");

                if (speedProp != null) speedProp.floatValue = 5.0f;
                if (angleProp != null) angleProp.floatValue = 32.0f;
                if (forceProp != null) forceProp.floatValue = 15.0f;
                if (momProp != null) momProp.floatValue = 0.70f;
                if (widthProp != null) widthProp.floatValue = 0.055f;

                var braidedMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/CaMau/M_Coir_Rope_Braided.mat");
                if (matProp != null && braidedMat != null) matProp.objectReferenceValue = braidedMat;

                so.ApplyModifiedProperties();

                r.InitializeRope();
                EditorUtility.SetDirty(r);
            }
            Debug.Log($"[NormalizeRopeAnimations] Đã đồng bộ thông số cho {ropes.Length} dây thừng trong Scene!");
        }
    }
}
