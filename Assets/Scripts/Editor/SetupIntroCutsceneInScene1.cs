#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Video;
using Unity.Cinemachine;
using HoaVaMau.Gameplay;

namespace HoaVaMau.Editor
{
    public static class SetupIntroCutsceneInScene1
    {
        private const string ScenePath = "Assets/Scenes/New Scene 1_Overhaul.unity";
        private const string HistoricalVideoAssetPath = "Assets/Audio/Tài liệu lịch sử (1).mp4";

        [MenuItem("Tools/Hoa Tren Dat Do/Cutscene/Setup Historical Video Intro Cutscene")]
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

            // 1. Load official Historical Documentary Video ("Tài liệu lịch sử (1).mp4")
            VideoClip historicalVideo = AssetDatabase.LoadAssetAtPath<VideoClip>(HistoricalVideoAssetPath);
            if (historicalVideo == null)
            {
                string[] videoGuids = AssetDatabase.FindAssets("t:VideoClip", new[] { "Assets/Audio", "Assets" });
                foreach (string g in videoGuids)
                {
                    string p = AssetDatabase.GUIDToAssetPath(g);
                    var candidate = AssetDatabase.LoadAssetAtPath<VideoClip>(p);
                    if (candidate != null)
                    {
                        historicalVideo = candidate;
                        if (p.IndexOf("lịch sử", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            p.IndexOf("lich su", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            break;
                        }
                    }
                }
            }

            if (historicalVideo == null)
            {
                Debug.LogError($"[SetupIntroCutsceneInScene1] Không tìm thấy VideoClip tại '{HistoricalVideoAssetPath}'!");
                return;
            }

            // Load legacy fallback sprites so fallback fields remain valid
            Sprite s1 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Cinematics/HistorySlides/history_1.jpg");
            Sprite s2 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Cinematics/HistorySlides/history_2.jpg");
            Sprite s3 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Cinematics/HistorySlides/history_3.jpg");
            Sprite s4 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Cinematics/HistorySlides/history_4.jpg");
            Sprite vignette = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/T_Film_Vignette.png");
            Sprite scratches = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/T_Film_Scratches.png");

            // 2. Find or Create VCam_IntroSky (preserve existing position if already present)
            var vcamIntroGO = GameObject.Find("VCam_IntroSky");
            if (vcamIntroGO == null)
            {
                vcamIntroGO = new GameObject("VCam_IntroSky");
                vcamIntroGO.transform.position = new Vector3(-3.20f, 75.0f, -13.5f);
                vcamIntroGO.transform.rotation = Quaternion.identity;
            }
            var vcamSky = vcamIntroGO.GetComponent<CinemachineCamera>();
            if (vcamSky == null)
            {
                vcamSky = vcamIntroGO.AddComponent<CinemachineCamera>();
                vcamSky.OutputChannel = OutputChannels.Default;
                vcamSky.Lens.FieldOfView = 38f;
            }

            // Find Gameplay Camera
            var vcamGameplayGO = GameObject.Find("CM_VirtualCamera_2.5D");
            CinemachineCamera vcamGameplay = vcamGameplayGO != null ? vcamGameplayGO.GetComponent<CinemachineCamera>() : null;

            // Find Player (Canonical PF_Player_Minh)
            var playerGO = GameObject.Find("PF_Player_Minh") ?? GameObject.Find("Character_An");
            PlayerController playerCtrl = playerGO != null ? playerGO.GetComponent<PlayerController>() : null;

            // Find Ambient Wind AudioSource in scene
            AudioSource windSource = null;
            var windGO = GameObject.Find("Audio_RiceField_Wind");
            if (windGO != null)
            {
                windSource = windGO.GetComponent<AudioSource>();
            }

            // Find Cinemachine Brain
            var brain = UnityEngine.Object.FindAnyObjectByType<CinemachineBrain>();

            // 3. Find or Create IntroCutsceneController Root
            var cutsceneRoot = GameObject.Find("00_Cinematic_IntroCutsceneController");
            if (cutsceneRoot == null)
            {
                cutsceneRoot = new GameObject("00_Cinematic_IntroCutsceneController");
            }
            cutsceneRoot.SetActive(true);

            var controller = cutsceneRoot.GetComponent<IntroCutsceneController>();
            if (controller == null)
            {
                controller = cutsceneRoot.AddComponent<IntroCutsceneController>();
            }

            // Rebuild Cutscene Canvas with HistoricalVideoRawImage & assign video + fallback assets
            controller.EnsureCutsceneCanvasExists();
            controller.AssignSprites(s1, s2, s3, s4, vignette, scratches);
            controller.AssignHistoricalVideo(historicalVideo, true);
            controller.ResolveSceneReferences();

            // Setup serialized fields via SerializedObject for persistent storage
            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("useHistoricalVideo").boolValue = true;
            so.FindProperty("historicalVideoClip").objectReferenceValue = historicalVideo;
            so.FindProperty("videoVolume").floatValue = 1.0f;
            so.FindProperty("videoFadeInDuration").floatValue = 0.65f;
            so.FindProperty("videoFadeOutDuration").floatValue = 0.85f;
            so.FindProperty("playMonologueAfterVideo").boolValue = false;
            so.FindProperty("startWithSkyDescent").boolValue = false;
            so.FindProperty("forcePlayIntro").boolValue = true;
            so.FindProperty("vcamIntroSky").objectReferenceValue = vcamSky;
            so.FindProperty("cameraPanDuration").floatValue = 11.5f;

            var curveProp = so.FindProperty("descentProfileCurve");
            if (curveProp != null && (curveProp.animationCurveValue == null || curveProp.animationCurveValue.length == 0))
            {
                curveProp.animationCurveValue = IntroCutsceneController.CreateDefaultDescentCurve();
            }

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
            if (windSource != null)
            {
                so.FindProperty("ambientAudioSource").objectReferenceValue = windSource;
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(controller);

            // Keep 00_UI_CutsceneCanvas inactive in Edit Mode so Scene View / Game View remain unobstructed
            Transform canvasTr = cutsceneRoot.transform.Find("00_UI_CutsceneCanvas");
            if (canvasTr != null)
            {
                canvasTr.gameObject.SetActive(false);
                EditorUtility.SetDirty(canvasTr.gameObject);
            }

            // Reset session state so pressing Play immediately previews the full video intro cutscene
            IntroCutsceneState.ShouldPlayIntroCutscene = true;
            IntroCutsceneState.HasViewedInSession = false;

            // Mark Scene Dirty and Save
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log($"<color=#10b981>[SetupIntroCutsceneInScene1]</color> Đã cấu hình thành công Intro Cutscene sử dụng video tư liệu <b>'{historicalVideo.name}'</b> ({historicalVideo.width}x{historicalVideo.height}, {historicalVideo.length:F1}s) trong <b>New Scene 1_Overhaul.unity</b>!");
        }
    }
}
#endif
