using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using HoaVaMau.Gameplay;

namespace HoaVaMau.Editor
{
    public static class FixLanaRopeAndCrate
    {
        [MenuItem("Tools/Hoa va Mau/Apply Planet of Lana Rope Crate and Jump Fixes")]
        public static void ApplyFixes()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            Debug.Log($"[FixLanaRopeAndCrate] Applying fixes to active scene: {activeScene.path}");

            // 1. Fix Player Locomotion (Planet of Lana jump height & reach)
            var player = Object.FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                var playerSO = new SerializedObject(player);
                var jfProp = playerSO.FindProperty("jumpForce");
                var gravProp = playerSO.FindProperty("gravity");
                if (jfProp != null) jfProp.floatValue = 6.2f;
                if (gravProp != null) gravProp.floatValue = 22.5f;
                playerSO.ApplyModifiedProperties();

                var ledgeDetector = player.GetComponent<LedgeGrabDetector>();
                if (ledgeDetector != null)
                {
                    var ldSO = new SerializedObject(ledgeDetector);
                    var minReachProp = ldSO.FindProperty("minLedgeReach");
                    var maxReachProp = ldSO.FindProperty("maxLedgeReach");
                    if (minReachProp != null) minReachProp.floatValue = 0.60f;
                    if (maxReachProp != null) maxReachProp.floatValue = 2.15f;
                    ldSO.ApplyModifiedProperties();
                }

                Debug.Log("<color=#4ade80>[FixLanaRopeAndCrate]</color> Player JumpForce set to 6.2f, Gravity to 22.5f, MaxLedgeReach to 2.15f.");
            }

            // 2. Fix Crate (Prevent sinking into ground, size 0.92m, exact bottom alignment)
            var crate = Object.FindAnyObjectByType<InteractableCrate>();
            if (crate != null)
            {
                Undo.RecordObject(crate.transform, "Fix Crate Transform");
                // Floor collider top is at Y = -1.10m. Align crate pivot exactly to Y = -1.10m
                crate.transform.position = new Vector3(30.0f, -1.10f, 0f);
                crate.transform.rotation = Quaternion.identity;

                var crateSO = new SerializedObject(crate);
                var boxSizeProp = crateSO.FindProperty("boxSize");
                var massProp = crateSO.FindProperty("mass");
                if (boxSizeProp != null) boxSizeProp.vector3Value = new Vector3(0.92f, 0.92f, 1.0f);
                if (massProp != null) massProp.floatValue = 28f;
                crateSO.ApplyModifiedProperties();

                var bc = crate.GetComponent<BoxCollider>();
                if (bc != null)
                {
                    Undo.RecordObject(bc, "Fix Crate Collider");
                    bc.size = new Vector3(0.92f, 0.92f, 1.0f);
                    bc.center = new Vector3(0f, 0.46f, 0f);
                }

                var rb = crate.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Undo.RecordObject(rb, "Fix Crate Rigidbody");
                    rb.mass = 28f;
                    rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
                }

                // Align visual mesh child precisely so its bottom is at local Y = 0
                crate.AlignChildMeshToBase();

                Debug.Log("<color=#4ade80>[FixLanaRopeAndCrate]</color> Crate positioned at (30.0, -1.10, 0), size=0.92m, bottom aligned perfectly at ground surface.");
            }

            // 3. Fix Rope (Planet of Lana clean ropeTraversal: no resonant momentum launch)
            var rope = Object.FindAnyObjectByType<RopeController>();
            if (rope != null)
            {
                var ropeSO = new SerializedObject(rope);
                var maxSpeedProp = ropeSO.FindProperty("maxSwingSpeed");
                var maxAngleProp = ropeSO.FindProperty("maxSwingAngle");
                var swingForceProp = ropeSO.FindProperty("playerSwingForce");
                var momRatioProp = ropeSO.FindProperty("momentumTransferRatio");

                if (maxSpeedProp != null) maxSpeedProp.floatValue = 2.0f;
                if (maxAngleProp != null) maxAngleProp.floatValue = 6.0f;
                if (swingForceProp != null) swingForceProp.floatValue = 0.0f;
                if (momRatioProp != null) momRatioProp.floatValue = 0.0f;
                ropeSO.ApplyModifiedProperties();

                rope.InitializeRope();
                Debug.Log("<color=#4ade80>[FixLanaRopeAndCrate]</color> Rope updated: zero momentum launch, maxSwingAngle=6 deg, stable hemp line.");
            }

            // 4. Save Scene & Assets
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            AssetDatabase.SaveAssets();

            Debug.Log("<color=#38bdf8>[FixLanaRopeAndCrate]</color> <b>ALL PLANET OF LANA FIXES APPLIED AND SAVED SUCCESSFULLY!</b>");
        }
    }
}
