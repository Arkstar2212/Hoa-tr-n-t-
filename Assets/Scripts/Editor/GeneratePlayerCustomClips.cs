using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace HoaVaMau.Editor
{
    public static class GeneratePlayerCustomClips
    {
        private const string CustomClipsDir = "Assets/Animations/Hero/Custom";

        [MenuItem("Hoa va Mau/Animation/Generate Custom Player Humanoid Clips")]
        public static void GenerateClipsAndConfigureControllers()
        {
            if (!Directory.Exists(CustomClipsDir))
            {
                Directory.CreateDirectory(CustomClipsDir);
                AssetDatabase.Refresh();
            }

            var crouchIdleClip = CreateCrouchIdleClip();
            var crouchWalkClip = CreateCrouchWalkClip();
            var ropeHangClip   = CreateRopeHangClip();
            var ropeClimbUpClip   = CreateRopeClimbUpClip();
            var ropeClimbDownClip = CreateRopeClimbDownClip();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Configure Animator Controllers
            string[] controllers = new[]
            {
                "Assets/TripoModels/Boy_Character/Boy_AnimatorController.controller",
                "Assets/Animations/Controllers/AC_Player_An.controller"
            };

            foreach (var ctrlPath in controllers)
            {
                var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);
                if (ctrl != null)
                {
                    ConfigureController(ctrl, crouchIdleClip, crouchWalkClip, ropeHangClip, ropeClimbUpClip, ropeClimbDownClip);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=#4ade80>[CustomClips]</color> <b>Successfully generated 5 custom Humanoid clips and configured Animator Controllers!</b>");
        }

        private static AnimationClip CreateCrouchIdleClip()
        {
            string path = $"{CustomClipsDir}/An_Crouch_Idle.anim";
            var clip = new AnimationClip { name = "An_Crouch_Idle" };
            float duration = 1.6f;
            int samples = 30;

            // 1. Lowered Root Center of Gravity
            AddCurve(clip, "RootT.y", duration, samples, t => -0.32f + Mathf.Sin(t * Mathf.PI * 2f / duration) * 0.012f);
            AddCurve(clip, "RootT.x", duration, samples, t => 0f);
            AddCurve(clip, "RootT.z", duration, samples, t => 0f);

            // 2. Spine angled forward ~20 deg with gentle breathing
            AddCurve(clip, "Spine Front-Back", duration, samples, t => 0.35f + Mathf.Sin(t * Mathf.PI * 2f / duration) * 0.025f);
            AddCurve(clip, "Chest Front-Back", duration, samples, t => 0.22f + Mathf.Sin(t * Mathf.PI * 2f / duration) * 0.035f);
            AddCurve(clip, "Head Nod Down-Up", duration, samples, t => -0.18f + Mathf.Sin(t * Mathf.PI * 2f / duration) * 0.015f);

            // 3. Deep squat knee bend & foot contact
            AddCurve(clip, "Left Upper Leg Front-Back", duration, samples, t => -0.65f);
            AddCurve(clip, "Right Upper Leg Front-Back", duration, samples, t => -0.62f);
            AddCurve(clip, "Left Lower Leg Stretch", duration, samples, t => -0.72f);
            AddCurve(clip, "Right Lower Leg Stretch", duration, samples, t => -0.68f);
            AddCurve(clip, "Left Foot Up-Down", duration, samples, t => 0.22f);
            AddCurve(clip, "Right Foot Up-Down", duration, samples, t => 0.22f);

            // 4. Arms bent, hands resting gently near knees
            AddCurve(clip, "Left Arm Down-Up", duration, samples, t => -0.35f);
            AddCurve(clip, "Right Arm Down-Up", duration, samples, t => -0.35f);
            AddCurve(clip, "Left Arm Front-Back", duration, samples, t => 0.25f);
            AddCurve(clip, "Right Arm Front-Back", duration, samples, t => 0.25f);
            AddCurve(clip, "Left Forearm Stretch", duration, samples, t => -0.55f);
            AddCurve(clip, "Right Forearm Stretch", duration, samples, t => -0.55f);
            AddCurve(clip, "Left Hand Down-Up", duration, samples, t => -0.15f);
            AddCurve(clip, "Right Hand Down-Up", duration, samples, t => -0.15f);

            SetLoop(clip);
            SaveOrUpdateAsset(clip, path);
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        }

        private static AnimationClip CreateCrouchWalkClip()
        {
            string path = $"{CustomClipsDir}/An_Crouch_Walk.anim";
            var clip = new AnimationClip { name = "An_Crouch_Walk" };
            float duration = 1.0f;
            int samples = 30;

            // 1. Lowered Root Center of Gravity with rhythmic step bob
            AddCurve(clip, "RootT.y", duration, samples, t => -0.30f + Mathf.Sin(t * Mathf.PI * 4f / duration) * 0.022f);
            AddCurve(clip, "RootT.x", duration, samples, t => 0f);
            AddCurve(clip, "RootT.z", duration, samples, t => 0f);

            // 2. Spine leaning forward in stealth locomotion
            AddCurve(clip, "Spine Front-Back", duration, samples, t => 0.32f + Mathf.Sin(t * Mathf.PI * 4f / duration) * 0.02f);
            AddCurve(clip, "Chest Front-Back", duration, samples, t => 0.20f + Mathf.Sin(t * Mathf.PI * 4f / duration) * 0.02f);
            AddCurve(clip, "Head Nod Down-Up", duration, samples, t => -0.18f);

            // 3. Stride cycle (Left Leg and Right Leg alternating)
            AddCurve(clip, "Left Upper Leg Front-Back", duration, samples, t =>
            {
                float phase = (t / duration) * Mathf.PI * 2f;
                return -0.65f + 0.35f * Mathf.Sin(phase);
            });
            AddCurve(clip, "Right Upper Leg Front-Back", duration, samples, t =>
            {
                float phase = (t / duration) * Mathf.PI * 2f + Mathf.PI;
                return -0.65f + 0.35f * Mathf.Sin(phase);
            });
            AddCurve(clip, "Left Lower Leg Stretch", duration, samples, t =>
            {
                float phase = (t / duration) * Mathf.PI * 2f;
                return -0.70f + 0.30f * Mathf.Max(0f, -Mathf.Sin(phase));
            });
            AddCurve(clip, "Right Lower Leg Stretch", duration, samples, t =>
            {
                float phase = (t / duration) * Mathf.PI * 2f + Mathf.PI;
                return -0.70f + 0.30f * Mathf.Max(0f, -Mathf.Sin(phase));
            });
            AddCurve(clip, "Left Foot Up-Down", duration, samples, t =>
            {
                float phase = (t / duration) * Mathf.PI * 2f;
                return 0.20f + 0.15f * Mathf.Cos(phase);
            });
            AddCurve(clip, "Right Foot Up-Down", duration, samples, t =>
            {
                float phase = (t / duration) * Mathf.PI * 2f + Mathf.PI;
                return 0.20f + 0.15f * Mathf.Cos(phase);
            });

            // 4. Arms counterbalance forward swing
            AddCurve(clip, "Left Arm Front-Back", duration, samples, t =>
            {
                float phase = (t / duration) * Mathf.PI * 2f;
                return 0.20f - 0.22f * Mathf.Sin(phase);
            });
            AddCurve(clip, "Right Arm Front-Back", duration, samples, t =>
            {
                float phase = (t / duration) * Mathf.PI * 2f;
                return 0.20f + 0.22f * Mathf.Sin(phase);
            });
            AddCurve(clip, "Left Arm Down-Up", duration, samples, t => -0.32f);
            AddCurve(clip, "Right Arm Down-Up", duration, samples, t => -0.32f);
            AddCurve(clip, "Left Forearm Stretch", duration, samples, t => -0.55f);
            AddCurve(clip, "Right Forearm Stretch", duration, samples, t => -0.55f);

            SetLoop(clip);
            SaveOrUpdateAsset(clip, path);
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        }

        private static AnimationClip CreateRopeHangClip()
        {
            string path = $"{CustomClipsDir}/An_Rope_Hang_Idle.anim";
            var clip = new AnimationClip { name = "An_Rope_Hang_Idle" };
            float duration = 1.6f;
            int samples = 30;

            // 1. Root suspended
            AddCurve(clip, "RootT.x", duration, samples, t => 0f);
            AddCurve(clip, "RootT.y", duration, samples, t => Mathf.Sin(t * Mathf.PI * 2f / duration) * 0.015f);
            AddCurve(clip, "RootT.z", duration, samples, t => 0f);

            // 2. Hanging spine
            AddCurve(clip, "Spine Front-Back", duration, samples, t => -0.06f + Mathf.Sin(t * Mathf.PI * 2f / duration) * 0.02f);
            AddCurve(clip, "Chest Front-Back", duration, samples, t => 0.04f);
            AddCurve(clip, "Head Nod Down-Up", duration, samples, t => 0.15f); // Looking slightly up towards rope anchor

            // 3. Hands raised up overhead gripping the vertical rope
            AddCurve(clip, "Right Arm Down-Up", duration, samples, t => 0.85f);
            AddCurve(clip, "Right Arm Front-Back", duration, samples, t => 0.12f);
            AddCurve(clip, "Right Forearm Stretch", duration, samples, t => 0.45f);
            AddCurve(clip, "Right Hand Down-Up", duration, samples, t => -0.10f);

            AddCurve(clip, "Left Arm Down-Up", duration, samples, t => 0.72f);
            AddCurve(clip, "Left Arm Front-Back", duration, samples, t => 0.10f);
            AddCurve(clip, "Left Forearm Stretch", duration, samples, t => 0.35f);
            AddCurve(clip, "Left Hand Down-Up", duration, samples, t => -0.10f);

            // 4. Legs hanging loosely with gentle rope embrace
            AddCurve(clip, "Left Upper Leg Front-Back", duration, samples, t => -0.12f + Mathf.Sin(t * Mathf.PI * 2f / duration) * 0.03f);
            AddCurve(clip, "Right Upper Leg Front-Back", duration, samples, t => -0.08f + Mathf.Sin(t * Mathf.PI * 2f / duration) * 0.03f);
            AddCurve(clip, "Left Lower Leg Stretch", duration, samples, t => -0.28f);
            AddCurve(clip, "Right Lower Leg Stretch", duration, samples, t => -0.22f);
            AddCurve(clip, "Left Foot Up-Down", duration, samples, t => -0.15f);
            AddCurve(clip, "Right Foot Up-Down", duration, samples, t => -0.15f);

            SetLoop(clip);
            SaveOrUpdateAsset(clip, path);
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        }

        private static AnimationClip CreateRopeClimbUpClip()
        {
            string path = $"{CustomClipsDir}/An_Rope_Climb_Up.anim";
            var clip = new AnimationClip { name = "An_Rope_Climb_Up" };
            float duration = 0.85f;
            int samples = 30;

            // 1. Root vertical impulse upward
            AddCurve(clip, "RootT.y", duration, samples, t =>
            {
                float phase = (t / duration) * Mathf.PI * 2f;
                return Mathf.Sin(phase) * 0.035f;
            });
            AddCurve(clip, "RootT.x", duration, samples, t => 0f);
            AddCurve(clip, "RootT.z", duration, samples, t => 0f);

            // 2. Spine flex
            AddCurve(clip, "Spine Front-Back", duration, samples, t => 0.10f);
            AddCurve(clip, "Head Nod Down-Up", duration, samples, t => 0.22f); // Eyes on climb path

            // 3. Hand-over-hand climbing upward
            AddCurve(clip, "Right Arm Down-Up", duration, samples, t =>
            {
                float phase = (t / duration) * Mathf.PI * 2f;
                return 0.70f + 0.25f * Mathf.Sin(phase);
            });
            AddCurve(clip, "Left Arm Down-Up", duration, samples, t =>
            {
                float phase = (t / duration) * Mathf.PI * 2f + Mathf.PI;
                return 0.70f + 0.25f * Mathf.Sin(phase);
            });
            AddCurve(clip, "Right Forearm Stretch", duration, samples, t =>
            {
                float phase = (t / duration) * Mathf.PI * 2f;
                return 0.25f + 0.35f * Mathf.Max(0f, Mathf.Sin(phase));
            });
            AddCurve(clip, "Left Forearm Stretch", duration, samples, t =>
            {
                float phase = (t / duration) * Mathf.PI * 2f + Mathf.PI;
                return 0.25f + 0.35f * Mathf.Max(0f, Mathf.Sin(phase));
            });

            // 4. Legs stepping / tucking upward
            AddCurve(clip, "Left Upper Leg Front-Back", duration, samples, t =>
            {
                float phase = (t / duration) * Mathf.PI * 2f;
                return -0.25f + 0.28f * Mathf.Sin(phase);
            });
            AddCurve(clip, "Right Upper Leg Front-Back", duration, samples, t =>
            {
                float phase = (t / duration) * Mathf.PI * 2f + Mathf.PI;
                return -0.25f + 0.28f * Mathf.Sin(phase);
            });
            AddCurve(clip, "Left Lower Leg Stretch", duration, samples, t => -0.40f);
            AddCurve(clip, "Right Lower Leg Stretch", duration, samples, t => -0.40f);

            SetLoop(clip);
            SaveOrUpdateAsset(clip, path);
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        }

        private static AnimationClip CreateRopeClimbDownClip()
        {
            string path = $"{CustomClipsDir}/An_Rope_Climb_Down.anim";
            var clip = new AnimationClip { name = "An_Rope_Climb_Down" };
            float duration = 0.85f;
            int samples = 30;

            // 1. Root lowering impulse
            AddCurve(clip, "RootT.y", duration, samples, t =>
            {
                float phase = (t / duration) * Mathf.PI * 2f;
                return -Mathf.Sin(phase) * 0.035f;
            });
            AddCurve(clip, "RootT.x", duration, samples, t => 0f);
            AddCurve(clip, "RootT.z", duration, samples, t => 0f);

            // 2. Spine straight
            AddCurve(clip, "Spine Front-Back", duration, samples, t => -0.05f);
            AddCurve(clip, "Head Nod Down-Up", duration, samples, t => -0.15f); // Looking down

            // 3. Hand-over-hand descending downward
            AddCurve(clip, "Right Arm Down-Up", duration, samples, t =>
            {
                float phase = (t / duration) * Mathf.PI * 2f;
                return 0.65f - 0.22f * Mathf.Sin(phase);
            });
            AddCurve(clip, "Left Arm Down-Up", duration, samples, t =>
            {
                float phase = (t / duration) * Mathf.PI * 2f + Mathf.PI;
                return 0.65f - 0.22f * Mathf.Sin(phase);
            });
            AddCurve(clip, "Right Forearm Stretch", duration, samples, t => 0.35f);
            AddCurve(clip, "Left Forearm Stretch", duration, samples, t => 0.35f);

            // 4. Legs extending down along rope
            AddCurve(clip, "Left Upper Leg Front-Back", duration, samples, t =>
            {
                float phase = (t / duration) * Mathf.PI * 2f;
                return -0.15f - 0.20f * Mathf.Sin(phase);
            });
            AddCurve(clip, "Right Upper Leg Front-Back", duration, samples, t =>
            {
                float phase = (t / duration) * Mathf.PI * 2f + Mathf.PI;
                return -0.15f - 0.20f * Mathf.Sin(phase);
            });
            AddCurve(clip, "Left Lower Leg Stretch", duration, samples, t => -0.30f);
            AddCurve(clip, "Right Lower Leg Stretch", duration, samples, t => -0.30f);

            SetLoop(clip);
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
            var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(clip, existing);
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
            AnimationClip ropeHang,
            AnimationClip ropeClimbUp,
            AnimationClip ropeClimbDown)
        {
            var sm = ctrl.layers[0].stateMachine;

            // 1. Update Crouch & CrouchIdle states
            SetStateMotion(sm, "CrouchIdle", crouchIdle);
            SetStateMotion(sm, "Crouch", crouchWalk);

            // 2. Update Rope states
            SetStateMotion(sm, "RopeClimb", ropeClimbUp); // Fallback / legacy state name
            SetStateMotion(sm, "RopeHang", ropeHang, true);
            SetStateMotion(sm, "RopeClimbUp", ropeClimbUp, true);
            SetStateMotion(sm, "RopeClimbDown", ropeClimbDown, true);

            EditorUtility.SetDirty(ctrl);
        }

        private static void SetStateMotion(AnimatorStateMachine sm, string stateName, Motion motion, bool createIfMissing = false)
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
            }
        }
    }
}
