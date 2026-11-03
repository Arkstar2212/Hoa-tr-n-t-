using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using HoaTrenDatDo.Environment;

namespace HoaTrenDatDo.EditorTools
{
    public static class SetupAmbientFloatingLeavesAndPetals
    {
        private const string CoconutLeafMatPath = "Assets/Materials/Environment/M_VFX_DriftingCoconutLeaf.mat";
        private const string FlowerPetalMatPath = "Assets/Materials/Environment/M_VFX_FloatingFlowerPetal.mat";

        private const string CoconutLeafTexPath = "Assets/Art/PlanetOfLana_Official/Bushes/Seed-Leaf.png";
        private const string FlowerPetalTexPath = "Assets/Art/PlanetOfLana_Official/Flora/RockFlowerPink01.png";

        [MenuItem("Tools/VFX/Setup Ambient Floating Leaves And Petals")]
        public static void SetupVFX()
        {
            // 1. Ensure Materials Exist with URP Particle Unlit Shader
            var leafMat = GetOrCreateParticleMaterial(CoconutLeafMatPath, CoconutLeafTexPath, new Color(0.92f, 0.94f, 0.72f, 0.85f));
            var petalMat = GetOrCreateParticleMaterial(FlowerPetalMatPath, FlowerPetalTexPath, new Color(0.98f, 0.82f, 0.88f, 0.88f));

            // 2. Locate or Create Root Parent in 09_VFX
            var vfxRoot = GameObject.Find("09_VFX");
            var existing = GameObject.Find("[VFX_Ambient_Floating_Leaves_And_Petals]");
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }

            var rootGO = new GameObject("[VFX_Ambient_Floating_Leaves_And_Petals]");
            if (vfxRoot != null)
            {
                rootGO.transform.SetParent(vfxRoot.transform, false);
            }
            rootGO.transform.position = Vector3.zero;

            // Add camera follower
            var follower = rootGO.AddComponent<AmbientDriftVFXController>();

            // 3. Create Floating Coconut Leaves Particle System
            var leafGO = new GameObject("FloatingCoconutLeaves");
            leafGO.transform.SetParent(rootGO.transform, false);
            ConfigureCoconutLeafParticles(leafGO, leafMat);

            // 4. Create Floating Flower Petals Particle System
            var petalGO = new GameObject("FloatingFlowerPetals");
            petalGO.transform.SetParent(rootGO.transform, false);
            ConfigureFlowerPetalParticles(petalGO, petalMat);

            // 5. Mark Scene Dirty and Save
            EditorUtility.SetDirty(rootGO);
            EditorSceneManager.MarkSceneDirty(rootGO.scene);
            EditorSceneManager.SaveScene(rootGO.scene);

            Debug.Log("<color=#10b981>[AmbientVFX]</color> Successfully configured Ambient Floating Leaves and Flower Petals in New Scene 1_Overhaul!");
        }

        private static Material GetOrCreateParticleMaterial(string matPath, string texPath, Color tint)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (shader == null) shader = Shader.Find("Particles/Standard Unlit");

                mat = new Material(shader);
                mat.SetFloat("_Surface", 1); // Transparent
                mat.SetFloat("_Blend", 0);   // Alpha
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                AssetDatabase.CreateAsset(mat, matPath);
            }

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex != null)
            {
                mat.mainTexture = tex;
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            }
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
            mat.color = tint;

            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            return mat;
        }

        private static void ConfigureCoconutLeafParticles(GameObject go, Material mat)
        {
            var ps = go.AddComponent<ParticleSystem>();
            var psr = go.GetComponent<ParticleSystemRenderer>();
            psr.sharedMaterial = mat;
            psr.renderMode = ParticleSystemRenderMode.Billboard;
            psr.sortingOrder = 4;

            // Main Module
            var main = ps.main;
            main.duration = 5.0f;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(7.0f, 11.0f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.30f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.95f, 0.96f, 0.80f, 0.85f), new Color(0.78f, 0.88f, 0.65f, 0.75f));
            main.gravityModifier = 0.02f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 35;
            main.playOnAwake = true;

            // Emission Module
            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 3.2f;

            // Shape Module (Wide box framed across the 2.5D camera view)
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(26f, 11f, 2.2f);

            // Velocity Over Lifetime Module (Nam Bo river breeze drifting left-to-right)
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(0.7f, 1.6f);
            vel.y = new ParticleSystem.MinMaxCurve(-0.10f, -0.35f);
            vel.z = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);

            // Color Over Lifetime Module (Smooth fade in and fade out)
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(Color.white, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.0f, 0.0f), new GradientAlphaKey(0.85f, 0.15f), new GradientAlphaKey(0.85f, 0.80f), new GradientAlphaKey(0.0f, 1.0f) }
            );
            col.color = new ParticleSystem.MinMaxGradient(grad);

            // Rotation Over Lifetime Module (Gentle leaf tumbling)
            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-45f * Mathf.Deg2Rad, 45f * Mathf.Deg2Rad);

            // Noise Module (Fluttering organic sway in the wind)
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.30f;
            noise.frequency = 0.35f;
            noise.scrollSpeed = 0.20f;
            noise.damping = true;
        }

        private static void ConfigureFlowerPetalParticles(GameObject go, Material mat)
        {
            var ps = go.AddComponent<ParticleSystem>();
            var psr = go.GetComponent<ParticleSystemRenderer>();
            psr.sharedMaterial = mat;
            psr.renderMode = ParticleSystemRenderMode.Billboard;
            psr.sortingOrder = 5;

            // Main Module
            var main = ps.main;
            main.duration = 5.0f;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(6.0f, 10.0f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.0f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.20f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.98f, 0.82f, 0.88f, 0.85f), new Color(0.99f, 0.90f, 0.94f, 0.75f));
            main.gravityModifier = 0.012f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 28;
            main.playOnAwake = true;

            // Emission Module
            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 2.8f;

            // Shape Module
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(26f, 11f, 2.0f);

            // Velocity Over Lifetime Module
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
            vel.y = new ParticleSystem.MinMaxCurve(-0.06f, -0.28f);
            vel.z = new ParticleSystem.MinMaxCurve(-0.10f, 0.10f);

            // Color Over Lifetime Module
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(Color.white, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.0f, 0.0f), new GradientAlphaKey(0.85f, 0.15f), new GradientAlphaKey(0.85f, 0.80f), new GradientAlphaKey(0.0f, 1.0f) }
            );
            col.color = new ParticleSystem.MinMaxGradient(grad);

            // Rotation Over Lifetime Module
            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-55f * Mathf.Deg2Rad, 55f * Mathf.Deg2Rad);

            // Noise Module (Delicate petal floating flutter)
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.38f;
            noise.frequency = 0.45f;
            noise.scrollSpeed = 0.25f;
            noise.damping = true;
        }
    }
}
