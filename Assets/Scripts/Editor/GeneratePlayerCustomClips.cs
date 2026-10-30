using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace HoaVaMau.Editor
{
    public static class GeneratePlayerCustomClips
    {
        private const string CustomClipsDir = "Assets/Animations/Hero/Custom";
        private const string CrouchIdleFbx = "Assets/Animations/Hero/Stealth/Hero_Stealth_Crouch_Idle_Loop.fbx";
        private const string CrouchWalkFbx = "Assets/Animations/Hero/Stealth/Hero_Stealth_Crouch_Walk_Fwd_IP.fbx";
        private const string BoyCharacterFbx = "Assets/TripoModels/Boy_Character/Boy_Character.fbx";

        [MenuItem("Hoa va Mau/Animation/Generate Custom Player Humanoid Clips")]
        public static void GenerateClipsAndConfigureControllers()
        {
            if (!Directory.Exists(CustomClipsDir))
            {
                Directory.CreateDirectory(CustomClipsDir);
                AssetDatabase.Refresh();
            }

            // Sử dụng trực tiếp FBX chuẩn của dự án cho CrouchIdle, Crouch và Climb (không bao giờ ghi đè bằng clip tự sinh)
            var crouchIdleClip = LoadFbxClip(CrouchIdleFbx);
            var crouchWalkClip = LoadFbxClip(CrouchWalkFbx);
            var climbClip = LoadFbxClip(BoyCharacterFbx, "climb");

            var ropeHangClip      = CreateRopeHangClip();
            var ropeClimbUpClip   = CreateRopeClimbUpClip();
            var ropeClimbDownClip = CreateRopeClimbDownClip();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string[] controllers = new[]
            {
                "Assets/Animations/Controllers/AC_Player_An.controller"
            };

            foreach (var ctrlPath in controllers)
            {
                var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);
                if (ctrl != null)
                {
                    ConfigureController(ctrl, crouchIdleClip, crouchWalkClip, climbClip, ropeHangClip, ropeClimbUpClip, ropeClimbDownClip);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=#4ade80>[CustomClips]</color> <b>Đã tạo 3 clip Rope Humanoid và đồng bộ chuẩn Crouch / Climb cho Animator Controllers!</b>");
        }

        private static AnimationClip LoadFbxClip(string fbxPath, string exactClipName = null)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            if (assets == null) return null;

            foreach (var a in assets)
            {
                if (a is AnimationClip c && !c.name.StartsWith("__preview__"))
                {
                    if (string.IsNullOrEmpty(exactClipName) || string.Equals(c.name, exactClipName, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return c;
                    }
                }
            }
            return null;
        }

        private static void AddFingerGripCurves(AnimationClip clip, float duration, int samples, string prefix, float curl)
        {
            string[] fingers = new[]
            {
                "Thumb 1", "Thumb 2", "Thumb 3",
                "Index 1", "Index 2", "Index 3",
                "Middle 1", "Middle 2", "Middle 3",
                "Ring 1", "Ring 2", "Ring 3",
                "Little 1", "Little 2", "Little 3"
            };

            foreach (var f in fingers)
            {
                AddCurve(clip, $"{prefix} {f} Stretched", duration, samples, t => curl);
            }
            AddCurve(clip, $"{prefix} Thumb Spread", duration, samples, t => -0.25f);
        }

        private static AnimationClip CreateRopeHangClip()
        {
            string path = $"{CustomClipsDir}/An_Rope_Hang_Idle.anim";
            var clip = new AnimationClip { name = "An_Rope_Hang_Idle" };
            float duration = 1.8f;
            int samples = 45;

            AddCurve(clip, "RootT.x", duration, samples, t => -0.004f);
            AddCurve(clip, "RootT.y", duration, samples, t => 1.124f + Mathf.Sin(t * Mathf.PI * 2f / duration) * 0.015f);
            AddCurve(clip, "RootT.z", duration, samples, t => -0.012f);

            AddCurve(clip, "Spine Front-Back", duration, samples, t => -0.02f + Mathf.Sin(t * Mathf.PI * 2f / duration) * 0.015f);
            AddCurve(clip, "Chest Front-Back", duration, samples, t => 0.06f + Mathf.Sin(t * Mathf.PI * 2f / duration) * 0.025f);
            AddCurve(clip, "Head Nod Down-Up", duration, samples, t => 0.22f + Mathf.Sin(t * Mathf.PI * 2f / duration) * 0.015f);

            AddCurve(clip, "Right Shoulder Down-Up", duration, samples, t => 1.27f);
            AddCurve(clip, "Right Shoulder Front-Back", duration, samples, t => 0.72f);
            AddCurve(clip, "Left Shoulder Down-Up", duration, samples, t => 1.16f);
            AddCurve(clip, "Left Shoulder Front-Back", duration, samples, t => 0.52f);

            AddCurve(clip, "Right Arm Down-Up", duration, samples, t => 0.78f);
            AddCurve(clip, "Right Arm Front-Back", duration, samples, t => 0.28f);
            AddCurve(clip, "Right Arm Twist In-Out", duration, samples, t => 0.50f);
            AddCurve(clip, "Right Forearm Stretch", duration, samples, t => 0.73f);
            AddCurve(clip, "Right Forearm Twist In-Out", duration, samples, t => -0.67f);
            AddCurve(clip, "Right Hand Down-Up", duration, samples, t => -0.12f);

            AddCurve(clip, "Left Arm Down-Up", duration, samples, t => 0.76f);
            AddCurve(clip, "Left Arm Front-Back", duration, samples, t => 0.32f);
            AddCurve(clip, "Left Arm Twist In-Out", duration, samples, t => 0.48f);
            AddCurve(clip, "Left Forearm Stretch", duration, samples, t => 0.69f);
            AddCurve(clip, "Left Forearm Twist In-Out", duration, samples, t => -0.62f);
            AddCurve(clip, "Left Hand Down-Up", duration, samples, t => -0.12f);

            AddFingerGripCurves(clip, duration, samples, "Right", -0.80f);
            AddFingerGripCurves(clip, duration, samples, "Left", -0.80f);

            AddCurve(clip, "Left Upper Leg Front-Back", duration, samples, t => -0.10f + Mathf.Sin(t * Mathf.PI * 2f / duration) * 0.02f);
            AddCurve(clip, "Right Upper Leg Front-Back", duration, samples, t => -0.06f + Mathf.Sin(t * Mathf.PI * 2f / duration) * 0.02f);
            AddCurve(clip, "Left Upper Leg In-Out", duration, samples, t => 0.08f);
            AddCurve(clip, "Right Upper Leg In-Out", duration, samples, t => -0.05f);
            AddCurve(clip, "Left Lower Leg Stretch", duration, samples, t => -0.24f);
            AddCurve(clip, "Right Lower Leg Stretch", duration, samples, t => -0.18f);
            AddCurve(clip, "Left Foot Up-Down", duration, samples, t => -0.15f);
            AddCurve(clip, "Right Foot Up-Down", duration, samples, t => -0.15f);

            SaveOrUpdateAsset(clip, path);
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        }

        private static AnimationClip CreateRopeClimbUpClip()
        {
            string path = $"{CustomClipsDir}/An_Rope_Climb_Up.anim";
            var clip = new AnimationClip { name = "An_Rope_Climb_Up" };
            float duration = 0.90f;
            int samples = 45;

            AddCurve(clip, "RootT.y", duration, samples, t =>
            {
                float phase = (t / duration) * Mathf.PI * 4f;
                return 1.124f + Mathf.Sin(phase) * 0.035f;
            });
            AddCurve(clip, "RootT.x", duration, samples, t => 0f);
            AddCurve(clip, "RootT.z", duration, samples, t => -0.012f);

            AddCurve(clip, "Spine Front-Back", duration, samples, t =>
            {
                float phase = (t / duration) * Mathf.PI * 4f;
                return 0.08f + 0.04f * Mathf.Sin(phase);
            });
            AddCurve(clip, "Chest Front-Back", duration, samples, t => 0.10f);
            AddCurve(clip, "Spine Twist Left-Right", duration, samples, t =>
            {
                float phase = (t / duration) * Mathf.PI * 2f;
                return 0.12f * Mathf.Sin(phase);
            });
            AddCurve(clip, "Chest Twist Left-Right", duration, samples, t =>
            {
                float phase = (t / duration) * Mathf.PI * 2f;
                return 0.10f * Mathf.Sin(phase);
            });
            AddCurve(clip, "Head Nod Down-Up", duration, samples, t => 0.28f);

            AddCurve(clip, "Right Shoulder Down-Up", duration, samples, t =>
            {
                float phase = (t / duration) * Mathf.PI * 2f;
                return 1.20f + 0.25f * Mathf.Sin(phase);
            });
            AddCurve(clip, "Right Shoulder Front-Back", duration, samples, t => 0.65f);
            AddCurve(clip, "Right Arm Down-Up", duration, samples, t =>
            {
                float phase = (t / duration) * Mathf.PI * 2f;
                return 0.75f + 0.22f * Mathf.Sin(phase);
            });
            AddCurve(clip, "Right Arm Front-Back", duration, samples, t => 0.30f);
            AddCurve(clip, "Right Arm Twist In-Out", duration, samples, t => 0.50f);
            AddCurve(clip, "Right Forearm Stretch", duration, samples, t =>
            {
                float phase = (t / duration) * Mathf.PI * 2f;
                return 0.60f + 0.30f * Mathf.Sin(phase);
            });
            AddCurve(clip, "Right Forearm Twist In-Out", duration, samples, t => -0.65f);
            AddCurve(clip, "Right Hand Down-Up", duration, samples, t => -0.12f);

            AddCurve(clip, "Left Shoulder Down-Up", duration, samples, t =>
            {
                float phase = (t / duration) * Mathf.PI * 2f + Mathf.PI;
                return 1.20f + 0.25f * Mathf.Sin(phase);
            });
            AddCurve(clip, "Left Shoulder Front-Back", duration, samples, t => 0.65f);
            AddCurve(clip, "Left Arm Down-Up", duration, samples, t =>
            {
                float phase = (t / duration) * Mathf.PI * 2f + Mathf.PI;
                return 0.75f + 0.22f * Mathf.Sin(phase);
            });
            AddCurve(clip, "Left Arm Front-Back", duration, samples, t => 0.30f);
            AddCurve(clip, "Left Arm Twist In-Out", duration, samples, t => 0.48f);
            AddCurve(clip, "Left Forearm Stretch", duration, samples, t =>
            {
                float phase = (t / duration) * Mathf.PI * 2f + Mathf.PI;
                return 0.60f + 0.30f * Mathf.Sin(phase);
            });
            AddCurve(clip, "Left Forearm Twist In-Out", duration, samples, t => -0.62f);
            AddCurve(clip, "Left Hand Down-Up", duration, samples, t => -0.12f);

            AddFingerGripCurves(clip, duration, samples, "Right", -0.80f);
            AddFingerGripCurves(clip, duration, samples, "Left", -0.80f);

            AddCurve(clip, "Left Upper Leg Front-Back", duration, samples, t =>
            {
                float phase = (t / duration) * Mathf.PI * 2f;
                return -0.10f + 0.38f * Mathf.Max(0f, -Mathf.Sin(phase));
            });
            AddCurve(clip, "Left Lower Leg Stretch", duration, samples, t =>
            {
                float phase = (t / duration) * Mathf.PI * 2f;
                return -0.20f - 0.45f * Mathf.Max(0f, -Mathf.Sin(phase));
            });
            AddCurve(clip, "Left Foot Up-Down", duration, samples, t =>
            {
                float phase = (t / duration) * Mathf.PI * 2f;
                return 0.18f * Mathf.Max(0f, -Mathf.Sin(phase)) - 0.12f;
            });

            AddCurve(clip, "Right Upper Leg Front-Back", duration, samples, t =>
            {
                float phase = (t / duration) * Mathf.PI * 2f;
                return -0.10f + 0.38f * Mathf.Max(0f, Mathf.Sin(phase));
            });
            AddCurve(clip, "Right Lower Leg Stretch", duration, samples, t =>
            {
                float phase = (t / duration) * Mathf.PI * 2f;
                return -0.20f - 0.45f * Mathf.Max(0f, Mathf.Sin(phase));
            });
            AddCurve(clip, "Right Foot Up-Down", duration, samples, t =>
            {
                float phase = (t / duration) * Mathf.PI * 2f;
                return 0.18f * Mathf.Max(0f, Mathf.Sin(phase)) - 0.12f;
            });

            AddCurve(clip, "Left Upper Leg In-Out", duration, samples, t => 0.06f);
            AddCurve(clip, "Right Upper Leg In-Out", duration, samples, t => -0.05f);

            SaveOrUpdateAsset(clip, path);
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        }

        private static AnimationClip CreateRopeClimbDownClip()
        {
            string path = $"{CustomClipsDir}/An_Rope_Climb_Down.anim";
            var clip = new AnimationClip { name = "An_Rope_Climb_Down" };
            float duration = 0.90f;
            int samples = 45;

            AddCurve(clip, "RootT.y", duration, samples, t =>
            {
                float phase = (t / duration) * Mathf.PI * 4f;
                return 1.124f - Mathf.Sin(phase) * 0.025f;
            });
            AddCurve(clip, "RootT.x", duration, samples, t => 0f);
            AddCurve(clip, "RootT.z", duration, samples, t => -0.012f);

            AddCurve(clip, "Spine Front-Back", duration, samples, t => -0.04f);
            AddCurve(clip, "Chest Front-Back", duration, samples, t => 0.04f);
            AddCurve(clip, "Head Nod Down-Up", duration, samples, t => -0.22f);

            AddCurve(clip, "Right Shoulder Down-Up", duration, samples, t =>
            {
                float phase = (t / duration) * Mathf.PI * 2f;
                return 1.05f - 0.20f * Mathf.Sin(phase);
            });
            AddCurve(clip, "Right Shoulder Front-Back", duration, samples, t => 0.60f);
            AddCurve(clip, "Right Arm Down-Up", duration, samples, t =>
            {
                float phase = (t / duration) * Mathf.PI * 2f;
                return 0.65f - 0.20f * Mathf.Sin(phase);
            });
            AddCurve(clip, "Right Arm Front-Back", duration, samples, t => 0.28f);
            AddCurve(clip, "Right Arm Twist In-Out", duration, samples, t => 0.48f);
            AddCurve(clip, "Right Forearm Stretch", duration, samples, t =>
            {
                float phase = (t / duration) * Mathf.PI * 2f;
                return 0.50f - 0.25f * Mathf.Sin(phase);
            });
            AddCurve(clip, "Right Forearm Twist In-Out", duration, samples, t => -0.65f);
            AddCurve(clip, "Right Hand Down-Up", duration, samples, t => -0.12f);

            AddCurve(clip, "Left Shoulder Down-Up", duration, samples, t =>
            {
                float phase = (t / duration) * Mathf.PI * 2f + Mathf.PI;
                return 1.05f - 0.20f * Mathf.Sin(phase);
            });
            AddCurve(clip, "Left Shoulder Front-Back", duration, samples, t => 0.60f);
            AddCurve(clip, "Left Arm Down-Up", duration, samples, t =>
            {
                float phase = (t / duration) * Mathf.PI * 2f + Mathf.PI;
                return 0.65f - 0.20f * Mathf.Sin(phase);
            });
            AddCurve(clip, "Left Arm Front-Back", duration, samples, t => 0.28f);
            AddCurve(clip, "Left Arm Twist In-Out", duration, samples, t => 0.48f);
            AddCurve(clip, "Left Forearm Stretch", duration, samples, t =>
            {
                float phase = (t / duration) * Mathf.PI * 2f + Mathf.PI;
                return 0.50f - 0.25f * Mathf.Sin(phase);
            });
            AddCurve(clip, "Left Forearm Twist In-Out", duration, samples, t => -0.62f);
            AddCurve(clip, "Left Hand Down-Up", duration, samples, t => -0.12f);

            AddFingerGripCurves(clip, duration, samples, "Right", -0.80f);
            AddFingerGripCurves(clip, duration, samples, "Left", -0.80f);

            AddCurve(clip, "Left Upper Leg Front-Back", duration, samples, t =>
            {
                float phase = (t / duration) * Mathf.PI * 2f;
                return -0.14f - 0.08f * Mathf.Sin(phase);
            });
            AddCurve(clip, "Right Upper Leg Front-Back", duration, samples, t =>
            {
                float phase = (t / duration) * Mathf.PI * 2f + Mathf.PI;
                return -0.14f - 0.08f * Mathf.Sin(phase);
            });
            AddCurve(clip, "Left Lower Leg Stretch", duration, samples, t => -0.22f);
            AddCurve(clip, "Right Lower Leg Stretch", duration, samples, t => -0.22f);
            AddCurve(clip, "Left Foot Up-Down", duration, samples, t => -0.20f);
            AddCurve(clip, "Right Foot Up-Down", duration, samples, t => -0.20f);

            SaveOrUpdateAsset(clip, path);
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        }

        private static void AddCurve(AnimationClip clip, string propName, float duration, int samples, System.Func<float, float> evaluator)
        {
            var binding = new EditorCurveBinding
            {
                path = "",
                type = typeof(Animator),
                propertyName = propName
            };

            var keys = new Keyframe[samples + 1];
            for (int i = 0; i <= samples; i++)
            {
                float t = (float)i / samples * duration;
                float val = evaluator(t);
                keys[i] = new Keyframe(t, val);
            }

            var curve = new AnimationCurve(keys);
            for (int i = 0; i < curve.keys.Length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Auto);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Auto);
            }

            AnimationUtility.SetEditorCurve(clip, binding, curve);
        }

        private static void SetLoop(AnimationClip clip)
        {
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = true;
            settings.loopBlendOrientation = true;
            settings.loopBlendPositionY = true;
            settings.loopBlendPositionXZ = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
        }

        private static void SaveOrUpdateAsset(AnimationClip clip, string path)
        {
            SetLoop(clip);
            var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (existing != null)
            {
                existing.ClearCurves();
                var bindings = AnimationUtility.GetCurveBindings(clip);
                foreach (var b in bindings)
                {
                    AnimationUtility.SetEditorCurve(existing, b, AnimationUtility.GetEditorCurve(clip, b));
                }
                SetLoop(existing);
                EditorUtility.SetDirty(existing);
            }
            else
            {
                AssetDatabase.CreateAsset(clip, path);
            }
        }

        private static void ConfigureController(
            AnimatorController ctrl,
            AnimationClip crouchIdle,
            AnimationClip crouchWalk,
            AnimationClip climbClip,
            AnimationClip ropeHang,
            AnimationClip ropeClimbUp,
            AnimationClip ropeClimbDown)
        {
            // Bật iKPass cho toàn bộ Layer (phải gán lại mảng ctrl.layers để lưu vào asset)
            var layers = ctrl.layers;
            for (int i = 0; i < layers.Length; i++)
            {
                layers[i].iKPass = true;
            }
            ctrl.layers = layers;

            var sm = ctrl.layers[0].stateMachine;

            // 1. Gán CrouchIdle, Crouch và Climb chuẩn của dự án
            if (crouchIdle != null) SetStateMotion(sm, "CrouchIdle", crouchIdle, true, ikOnFeet: true);
            if (crouchWalk != null) SetStateMotion(sm, "Crouch", crouchWalk, true, ikOnFeet: true);
            if (climbClip != null) SetStateMotion(sm, "Climb", climbClip, true, ikOnFeet: false);

            // 2. Gán các state Rope
            SetStateMotion(sm, "RopeClimb", ropeClimbUp, false);
            SetStateMotion(sm, "RopeHang", ropeHang, true);
            SetStateMotion(sm, "RopeClimbUp", ropeClimbUp, true);
            SetStateMotion(sm, "RopeClimbDown", ropeClimbDown, true);

            EditorUtility.SetDirty(ctrl);
        }

        private static void SetStateMotion(AnimatorStateMachine sm, string stateName, Motion motion, bool createIfMissing = false, bool ikOnFeet = false)
        {
            AnimatorState targetState = null;
            foreach (var child in sm.states)
            {
                if (child.state.name == stateName)
                {
                    targetState = child.state;
                    break;
                }
            }

            if (targetState == null && createIfMissing)
            {
                targetState = sm.AddState(stateName);
            }

            if (targetState != null)
            {
                targetState.motion = motion;
                targetState.iKOnFeet = ikOnFeet;
            }
        }
    }
}
