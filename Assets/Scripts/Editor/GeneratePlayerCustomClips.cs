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

            SaveOrUpdateAsset(clip, path);
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
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

            // 1. Root suspension height matching Hero Humanoid standard (1.124m)
            AddCurve(clip, "RootT.x", duration, samples, t => -0.004f);
            AddCurve(clip, "RootT.y", duration, samples, t => 1.124f + Mathf.Sin(t * Mathf.PI * 2f / duration) * 0.015f);
            AddCurve(clip, "RootT.z", duration, samples, t => -0.012f);

            // 2. Spine & Chest natural suspension under gravity
            AddCurve(clip, "Spine Front-Back", duration, samples, t => -0.02f + Mathf.Sin(t * Mathf.PI * 2f / duration) * 0.015f);
            AddCurve(clip, "Chest Front-Back", duration, samples, t => 0.06f + Mathf.Sin(t * Mathf.PI * 2f / duration) * 0.025f);
            AddCurve(clip, "Head Nod Down-Up", duration, samples, t => 0.22f + Mathf.Sin(t * Mathf.PI * 2f / duration) * 0.015f); // Looking up at grip

            // 3. Elevated Clavicles supporting full body hanging weight
            AddCurve(clip, "Right Shoulder Down-Up", duration, samples, t => 1.27f);
            AddCurve(clip, "Right Shoulder Front-Back", duration, samples, t => 0.72f);
            AddCurve(clip, "Left Shoulder Down-Up", duration, samples, t => 1.16f);
            AddCurve(clip, "Left Shoulder Front-Back", duration, samples, t => 0.52f);

            // 4. Staggered overhead grip (Planet of Lana style: Right hand high, Left hand ~15cm lower)
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

            // 5. Solid cylinder grip on rope (tight fists around rope)
            AddFingerGripCurves(clip, duration, samples, "Right", -0.80f);
            AddFingerGripCurves(clip, duration, samples, "Left", -0.80f);

            // 6. Relaxed legs dangling along rope
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

            // 1. Root vertical impulse upward (2 pulls per loop at suspended height ~1.124m)
            AddCurve(clip, "RootT.y", duration, samples, t =>
            {
                float phase = (t / duration) * Mathf.PI * 4f;
                return 1.124f + Mathf.Sin(phase) * 0.035f;
            });
            AddCurve(clip, "RootT.x", duration, samples, t => 0f);
            AddCurve(clip, "RootT.z", duration, samples, t => -0.012f);

            // 2. Spine & Chest flex + rotation towards reaching hand
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
            AddCurve(clip, "Head Nod Down-Up", duration, samples, t => 0.28f); // Looking focused upward at rope grip

            // 3. Hand-over-hand climbing upward
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

            // 4. Tight grip on rope
            AddFingerGripCurves(clip, duration, samples, "Right", -0.80f);
            AddFingerGripCurves(clip, duration, samples, "Left", -0.80f);

            // 5. Athletic leg tucking & bicycling (Planet of Lana standard)
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

            // 1. Root lowering impulse at suspended height ~1.124m
            AddCurve(clip, "RootT.y", duration, samples, t =>
            {
                float phase = (t / duration) * Mathf.PI * 4f;
                return 1.124f - Mathf.Sin(phase) * 0.025f;
            });
            AddCurve(clip, "RootT.x", duration, samples, t => 0f);
            AddCurve(clip, "RootT.z", duration, samples, t => -0.012f);

            // 2. Spine & Head: looking downward along descent route
            AddCurve(clip, "Spine Front-Back", duration, samples, t => -0.04f);
            AddCurve(clip, "Chest Front-Back", duration, samples, t => 0.04f);
            AddCurve(clip, "Head Nod Down-Up", duration, samples, t => -0.22f);

            // 3. Hand-under-hand descending downward
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

            // 4. Tight grip on rope
            AddFingerGripCurves(clip, duration, samples, "Right", -0.80f);
            AddFingerGripCurves(clip, duration, samples, "Left", -0.80f);

            // 5. Relaxed legs extending downward along rope line
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