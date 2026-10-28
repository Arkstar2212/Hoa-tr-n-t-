#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.Cinemachine;
using HoaVaMau.Gameplay;

namespace HoaVaMau.Editor
{
    public static class SetupIntroCutsceneInScene1
    {
        private const string ScenePath = "Assets/Scenes/New Scene 1_Overhaul.unity";

        [MenuItem("Hoa va Mau/Cutscene/Setup Intro Cutscene in Scene 1")]
        public static void SetupCutsceneInScene1()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                if (File.Exists(ScenePath))
                {
                    scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                }
                else
                {
                    Debug.LogError($"[SetupIntroCutsceneInScene1] Cannot find scene at {ScenePath}");
                    return;
                }
            }

            // 1. Load authentic sprites
            Sprite s1 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Cinematics/HistorySlides/history_1.jpg");
            Sprite s2 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Cinematics/HistorySlides/history_2.jpg");
            Sprite s3 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Cinematics/HistorySlides/history_3.jpg");
            Sprite s4 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Cinematics/HistorySlides/history_4.jpg");
            Sprite vignette = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/T_Film_Vignette.png");
            Sprite scratches = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/T_Film_Scratches.png");

            AudioClip windClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/soundreality-wind-blowing-457954.mp3");

            // 2. Find or Create VCam_IntroSky
            var vcamIntroGO = GameObject.Find("VCam_IntroSky");
            if (vcamIntroGO == null)
            {
                vcamIntroGO = new GameObject("VCam_IntroSky");
            }
            vcamIntroGO.transform.position = new Vector3(-3.20f, 75.0f, -13.5f);
            vcamIntroGO.transform.rotation = Quaternion.identity;
            var vcamSky = vcamIntroGO.GetComponent<CinemachineCamera>();
            if (vcamSky == null)
            {
                vcamSky = vcamIntroGO.AddComponent<CinemachineCamera>();
            }
            vcamSky.OutputChannel = OutputChannels.Default;
            vcamSky.Priority = 0; // default 0 until cutscene activates (which elevates to 30)
            vcamSky.Lens.FieldOfView = 38f;

            // Find Gameplay Camera
            var vcamGameplayGO = GameObject.Find("CM_VirtualCamera_2.5D");
            CinemachineCamera vcamGameplay = null;
            if (vcamGameplayGO != null)
            {
                vcamGameplay = vcamGameplayGO.GetComponent<CinemachineCamera>();
                if (vcamGameplay != null)
                {
                    vcamGameplay.Priority = 10;
                }
            }

            // Find Player
            var playerGO = GameObject.Find("PF_Player_Minh") ?? GameObject.Find("Character_An");
            PlayerController playerCtrl = null;
            if (playerGO != null)
            {
                playerCtrl = playerGO.GetComponent<PlayerController>();
            }

            // Find Cinemachine Brain
            var brain = Object.FindAnyObjectByType<CinemachineBrain>();

            // 3. Find or Create IntroCutsceneController Root
            var cutsceneRoot = GameObject.Find("00_Cinematic_IntroCutsceneController");
            if (cutsceneRoot == null)
            {
                cutsceneRoot = new GameObject("00_Cinematic_IntroCutsceneController");
            }

            var controller = cutsceneRoot.GetComponent<IntroCutsceneController>();
            if (controller == null)
            {
                controller = cutsceneRoot.AddComponent<IntroCutsceneController>();
            }

            // Ensure Canvas is generated
            controller.EnsureCutsceneCanvasExists();
            controller.AssignSprites(s1, s2, s3, s4, vignette, scratches);
            controller.ResolveSceneReferences();

            // Setup serialized fields via SerializedObject for persistent undo-safe storage
            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("vcamIntroSky").objectReferenceValue = vcamSky;
            so.FindProperty("cameraPanDuration").floatValue = 11.5f;
            so.FindProperty("forcePlayIntro").boolValue = true;
            if (vcamGameplay != null)
            {
                so.FindProperty("vcamGameplay").objectReferenceValue = vcamGameplay;
            }
            if (brain != null)
            {
                so.FindProperty("cinemachineBrain").objectReferenceValue = brain;
            }
            if (playerCtrl != null)
            {
                so.FindProperty("playerController").objectReferenceValue = playerCtrl;
                so.FindProperty("playerTransform").objectReferenceValue = playerCtrl.transform;
            }

            so.ApplyModifiedProperties();

            // Mark Scene Dirty and Save
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("<color=#10b981>[SetupIntroCutsceneInScene1]</color> Successfully configured IntroCutsceneController and VCam_IntroSky in New Scene 1_Overhaul.unity!");
        }
    }
}
#endif
