using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using HoaTrenDatDo.Environment;

namespace HoaTrenDatDo.EditorTools
{
    public static class BirdSpriteAndAnimationSetup
    {
        private const string SpriteDir = "Assets/Art/Environment/Sky/Bird_Sparrow";
        private const string AnimDir = "Assets/Animations/Environment";
        private const string ControllerDir = "Assets/Animations/Controllers";
        private const string PrefabDir = "Assets/Prefabs/Environment";

        private const string AnimPath = AnimDir + "/Bird_Sparrow_Fly.anim";
        private const string ControllerPath = ControllerDir + "/AC_Bird_Sparrow.controller";
        private const string PrefabPath = PrefabDir + "/Bird_Sparrow_Entity.prefab";

        [MenuItem("Tools/Hoa Tren Dat Do/Build Complete Bird Animation & Spawner")]
        public static void BuildCompleteBirdSystem()
        {
            Debug.Log("[BirdSetup] Step 1: Configuring Bird Sprites...");
            ConfigureBirdSprites();

            Debug.Log("[BirdSetup] Step 2: Creating Animation Clip & Controller...");
            var controller = CreateAnimationAndController();

            Debug.Log("[BirdSetup] Step 3: Creating Bird Entity Prefab...");
            var prefab = CreateBirdPrefab(controller);

            Debug.Log("[BirdSetup] Step 4: Adding Ambient Sky Birds Spawner to Active Scene...");
            AddAmbientBirdsToActiveScene(prefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=green>[BirdSetup] Successfully built complete bird animation, prefab, and sky manager!</color>");
        }

        public static void ConfigureBirdSprites()
        {
            for (int i = 0; i < 12; i++)
            {
                string path = $"{SpriteDir}/bird_frame_{i:D2}.png";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spritePixelsPerUnit = 100;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;

                    var settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                    settings.spriteAlignment = (int)SpriteAlignment.Center;
                    importer.SetTextureSettings(settings);

                    EditorUtility.SetDirty(importer);
                    importer.SaveAndReimport();
                }
            }
        }

        public static AnimatorController CreateAnimationAndController()
        {
            if (!Directory.Exists(AnimDir)) Directory.CreateDirectory(AnimDir);
            if (!Directory.Exists(ControllerDir)) Directory.CreateDirectory(ControllerDir);

            // Load all 12 sprites
            Sprite[] sprites = new Sprite[12];
            for (int i = 0; i < 12; i++)
            {
                string path = $"{SpriteDir}/bird_frame_{i:D2}.png";
                sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprites[i] == null)
                {
                    Debug.LogWarning($"[BirdSetup] Could not load sprite at {path}");
                }
            }

            // Create or overwrite AnimationClip
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimPath);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, AnimPath);
            }
            else
            {
                clip.ClearCurves();
            }

            clip.frameRate = 12f;

            // Make it loop
            var animSettings = AnimationUtility.GetAnimationClipSettings(clip);
            animSettings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, animSettings);

            // Set ObjectReference curve for SpriteRenderer
            var binding = new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = "",
                propertyName = "m_Sprite"
            };

            var keyframes = new ObjectReferenceKeyframe[13];
            for (int i = 0; i < 12; i++)
            {
                keyframes[i] = new ObjectReferenceKeyframe
                {
                    time = i * (1f / 12f),
                    value = sprites[i]
                };
            }
            // Loop frame back to frame 0
            keyframes[12] = new ObjectReferenceKeyframe
            {
                time = 1.0f,
                value = sprites[0]
            };

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
            EditorUtility.SetDirty(clip);

            // Create or overwrite Animator Controller
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }

            var rootStateMachine = controller.layers[0].stateMachine;
            // Clear existing states if any
            foreach (var childState in rootStateMachine.states)
            {
                rootStateMachine.RemoveState(childState.state);
            }

            var flyState = rootStateMachine.AddState("Fly");
            flyState.motion = clip;
            rootStateMachine.defaultState = flyState;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            return controller;
        }

        public static GameObject CreateBirdPrefab(AnimatorController controller)
        {
            if (!Directory.Exists(PrefabDir)) Directory.CreateDirectory(PrefabDir);

            string initialSpritePath = $"{SpriteDir}/bird_frame_00.png";
            Sprite initialSprite = AssetDatabase.LoadAssetAtPath<Sprite>(initialSpritePath);

            GameObject tempGo = new GameObject("Bird_Sparrow_Entity");

            var sr = tempGo.AddComponent<SpriteRenderer>();
            sr.sprite = initialSprite;
            // Check sorting layers, fallback to Default with order 10
            sr.sortingOrder = 10;

            var anim = tempGo.AddComponent<Animator>();
            anim.runtimeAnimatorController = controller;

            tempGo.AddComponent<BirdFlightEntity>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(tempGo, PrefabPath);
            Object.DestroyImmediate(tempGo);

            Debug.Log($"[BirdSetup] Saved Bird Entity Prefab to {PrefabPath}");
            return prefab;
        }

        [MenuItem("Tools/Hoa Tren Dat Do/Add Ambient Birds To Current Scene")]
        public static void AddAmbientBirdsToActiveSceneMenuItem()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogError("[BirdSetup] Prefab not found at " + PrefabPath + ". Run 'Build Complete Bird Animation & Spawner' first.");
                return;
            }
            AddAmbientBirdsToActiveScene(prefab);
        }

        public static void AddAmbientBirdsToActiveScene(GameObject prefab)
        {
            const string ManagerName = "[Additive_Sky_Birds]";
            GameObject existing = GameObject.Find(ManagerName);
            if (existing != null)
            {
                Debug.Log($"[BirdSetup] Found existing '{ManagerName}'. Updating prefab reference.");
                var mgr = existing.GetComponent<AmbientSkyBirdsManager>();
                if (mgr != null)
                {
                    var serializedMgr = new SerializedObject(mgr);
                    serializedMgr.FindProperty("birdPrefab").objectReferenceValue = prefab;
                    serializedMgr.ApplyModifiedProperties();
                }
                return;
            }

            GameObject skyBirdsManagerGo = new GameObject(ManagerName);
            skyBirdsManagerGo.transform.position = Vector3.zero;

            var manager = skyBirdsManagerGo.AddComponent<AmbientSkyBirdsManager>();
            var so = new SerializedObject(manager);
            so.FindProperty("birdPrefab").objectReferenceValue = prefab;
            so.FindProperty("minInterval").floatValue = 22f;
            so.FindProperty("maxInterval").floatValue = 50f;
            so.FindProperty("spawnChance").floatValue = 0.85f;
            so.FindProperty("minFlockSize").intValue = 1;
            so.FindProperty("maxFlockSize").intValue = 3;
            so.FindProperty("minBoundaryX").floatValue = -30f;
            so.FindProperty("maxBoundaryX").floatValue = 150f;
            so.FindProperty("minAltitudeY").floatValue = 10f;
            so.FindProperty("maxAltitudeY").floatValue = 16f;
            so.FindProperty("minDepthZ").floatValue = 16f;
            so.FindProperty("maxDepthZ").floatValue = 21f;
            so.ApplyModifiedProperties();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(skyBirdsManagerGo.scene);
            Debug.Log($"[BirdSetup] Added '{ManagerName}' to scene {skyBirdsManagerGo.scene.name} (Additive, Non-destructive).");
        }
    }
}
