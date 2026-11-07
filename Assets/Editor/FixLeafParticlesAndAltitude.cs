#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using HoaTrenDatDo.Environment;

namespace HoaTrenDatDo.EditorTools
{
    public static class FixLeafParticlesAndAltitude
    {
        private const string SceneOverhaulPath = "Assets/Scenes/New Scene 1_Overhaul.unity";
        private const string CoconutLeafMatPath = "Assets/Materials/Environment/M_VFX_DriftingCoconutLeaf.mat";
        private const string RealLeafTexPath = "Assets/Art/PlanetOfLana_Official/Bushes/PlantLeaf01.png";

        [MenuItem("Tools/VFX/Fix Leaf Particles And Sky Cutoff", priority = 20)]
        public static void ExecuteFix()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != SceneOverhaulPath)
            {
                if (System.IO.File.Exists(SceneOverhaulPath))
                {
                    activeScene = EditorSceneManager.OpenScene(SceneOverhaulPath, OpenSceneMode.Single);
                }
            }

            Debug.Log("<color=#10b981>[FixLeafParticles]</color> Updating leaf particles & configuring ground altitude clamp...");

            // 1. Ensure Material uses PlantLeaf01.png
            var leafMat = AssetDatabase.LoadAssetAtPath<Material>(CoconutLeafMatPath);
            var leafTex = AssetDatabase.LoadAssetAtPath<Texture2D>(RealLeafTexPath);
            if (leafMat != null && leafTex != null)
            {
                leafMat.mainTexture = leafTex;
                if (leafMat.HasProperty("_BaseMap")) leafMat.SetTexture("_BaseMap", leafTex);
                if (leafMat.HasProperty("_BaseColor")) leafMat.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.95f));
                leafMat.color = new Color(1f, 1f, 1f, 0.95f);
                EditorUtility.SetDirty(leafMat);
                Debug.Log("<color=#10b981>[FixLeafParticles]</color> Material M_VFX_DriftingCoconutLeaf set to authentic hand-painted PlantLeaf01.png");
            }

            // 2. Locate [VFX_Ambient_Floating_Leaves_And_Petals]
            var vfxRoot = GameObject.Find("[VFX_Ambient_Floating_Leaves_And_Petals]");
            if (vfxRoot == null)
            {
                Debug.LogWarning("[FixLeafParticles] [VFX_Ambient_Floating_Leaves_And_Petals] not found! Invoking SetupVFX...");
                SetupAmbientFloatingLeavesAndPetals.SetupVFX();
                vfxRoot = GameObject.Find("[VFX_Ambient_Floating_Leaves_And_Petals]");
            }

            if (vfxRoot != null)
            {
                Undo.RecordObject(vfxRoot.transform, "Adjust VFX Root Altitude");
                // Reset local position down to ground level (Y = 1.8m), NOT high up in sky (Y = 76m)
                vfxRoot.transform.localPosition = new Vector3(vfxRoot.transform.localPosition.x, 1.8f, 0f);

                var controller = vfxRoot.GetComponent<AmbientDriftVFXController>();
                if (controller == null)
                {
                    controller = vfxRoot.AddComponent<AmbientDriftVFXController>();
                }

                // Configure Controller Serialized Properties
                var so = new SerializedObject(controller);
                so.Update();
                var propOffsetX = so.FindProperty("offsetX");
                if (propOffsetX != null) propOffsetX.floatValue = -2.0f;
                var propOffsetY = so.FindProperty("offsetY");
                if (propOffsetY != null) propOffsetY.floatValue = 1.0f;
                var propMinY = so.FindProperty("minEmitterY");
                if (propMinY != null) propMinY.floatValue = 0.5f;
                var propMaxY = so.FindProperty("maxEmitterY");
                if (propMaxY != null) propMaxY.floatValue = 3.5f;
                var propSkyCutoff = so.FindProperty("skyCutoffAltitude");
                if (propSkyCutoff != null) propSkyCutoff.floatValue = 6.0f;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(controller);

                // Configure Child Particle Systems (World simulation space + start colors + clear)
                var particleSystems = vfxRoot.GetComponentsInChildren<ParticleSystem>(true);
                foreach (var ps in particleSystems)
                {
                    Undo.RecordObject(ps, "Configure Particle Simulation Space");
                    var main = ps.main;
                    main.simulationSpace = ParticleSystemSimulationSpace.World;
                    main.prewarm = false;

                    if (ps.gameObject.name.Contains("CoconutLeaves") || ps.gameObject.name.Contains("Leaf"))
                    {
                        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 1f, 0.95f), new Color(0.92f, 0.98f, 0.88f, 0.85f));
                        main.startSize = new ParticleSystem.MinMaxCurve(0.20f, 0.35f);
                    }

                    ps.Clear();
                    EditorUtility.SetDirty(ps);
                }

                Debug.Log("<color=#10b981>[FixLeafParticles]</color> Clamped emitter between Y=0.5m and Y=3.5m, sky cutoff at Y=6.0m, simulationSpace set to World.");
            }

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            AssetDatabase.SaveAssets();

            Debug.Log("<color=#10b981>[FixLeafParticles]</color> Fix successfully applied and saved!");
        }
    }
}
#endif
