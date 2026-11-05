using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.Cinemachine;
using HoaVaMau.Cinematics;
using HoaVaMau.Test3D;

namespace HoaVaMau.EditorScripts
{
    /// <summary>
    /// Automated Builder for the One-Shot Continuous Cinematic Sequence in "CaMau_1954_Village.unity".
    /// Kịch bản chuẩn xác theo yêu cầu người dùng:
    /// - 1 Camera duy nhất di chuyển liên tục, không chuyển khung hình.
    /// - Cổng làng -> Dolly-in Zoom mặt -> Lượn 180° sau lưng -> An chạy về làng & Camera bay ra đúng góc đại cảnh của bạn.
    /// - Khi chạy gần tới làng -> Tối đen lại (Fade Out) và chuyển mượt về gameplay.
    /// </summary>
    public static class BuildVillageIntroCinematicTimeline
    {
        private const string ScenePath = "Assets/Scenes/CaMau_1954_Village.unity";
        private const string TimelineDirectory = "Assets/Timelines";
        private const string TimelinePath = "Assets/Timelines/TL_Village_Intro.playable";

        private const string WindAudioPath = "Assets/Audio/SFX/SFX_Ambience_RiceField_Wind.mp3";
        private const string DirtFootstepAudioPath = "Assets/Audio/SFX/SFX_An_Footsteps_Dirt.mp3";
        private const string BgmAudioPath = "Assets/Audio/Phat_Hien_Binh_Yen.mp3";

        [MenuItem("Tools/Hoa Tren Dat Do/Build Village Intro Cinematic")]
        public static string Execute()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            Debug.Log("[Cinematics Builder] Building One-Shot Continuous Intro Cutscene with User Vista & Fade-to-Gameplay...");

            // 1. Locate Bé An
            var playerGo = GameObject.Find("An_Player");
            if (playerGo == null)
            {
                Debug.LogError("[Cinematics Builder] An_Player GameObject not found in scene!");
                return "Failed: An_Player not found";
            }
            var playerController = playerGo.GetComponent<Player3DController>();
            var characterController = playerGo.GetComponent<CharacterController>();
            var playerAnimator = playerGo.GetComponent<Animator>();

            // Ensure An is stationed at the North Gate
            playerGo.transform.position = new Vector3(-7.0f, 0.505f, 184.0f);
            playerGo.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            var spawnGo = GameObject.Find("PlayerSpawn");
            if (spawnGo != null)
            {
                spawnGo.transform.position = new Vector3(-7.0f, 0.505f, 184.0f);
                spawnGo.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            }

            // 2. Setup Main Camera & CinemachineBrain
            var mainCam = Camera.main;
            if (mainCam == null)
            {
                Debug.LogError("[Cinematics Builder] Main Camera not found!");
                return "Failed: Main Camera not found";
            }
            var gameplayCamera = mainCam.GetComponent<ThirdPersonOrbitCamera>();

            var brain = mainCam.GetComponent<CinemachineBrain>();
            if (brain == null)
            {
                brain = mainCam.gameObject.AddComponent<CinemachineBrain>();
            }
            brain.ShowDebugText = false;
            brain.enabled = true;

            // 3. Setup Cinematic_Cutscene_Rig & SINGLE Continuous Virtual Camera
            var rigGo = GameObject.Find("Cinematic_Cutscene_Rig");
            if (rigGo == null)
            {
                rigGo = new GameObject("Cinematic_Cutscene_Rig");
                Undo.RegisterCreatedObjectUndo(rigGo, "Create Cutscene Rig");
            }

            // Clean up any old discrete shot cameras
            while (rigGo.transform.childCount > 0)
            {
                GameObject.DestroyImmediate(rigGo.transform.GetChild(0).gameObject);
            }

            // Single continuous moving camera (One-Shot)
            var camGo = new GameObject("CM_Continuous_CutsceneCam");
            camGo.transform.SetParent(rigGo.transform, false);
            camGo.transform.position = new Vector3(-5.8f, 1.8f, 173.0f);
            camGo.transform.LookAt(new Vector3(-7.0f, 1.25f, 184.0f));

            var continuousCam = camGo.AddComponent<CinemachineCamera>();
            continuousCam.Lens.FieldOfView = 48f;
            continuousCam.Lens.NearClipPlane = 0.1f;
            continuousCam.Lens.FarClipPlane = 1000f;
            continuousCam.Priority.Value = 25;

            // 4. Setup Fade UI Canvas
            var fadeCanvasGo = GameObject.Find("Cinematic_FadeCanvas");
            if (fadeCanvasGo == null)
            {
                fadeCanvasGo = new GameObject("Cinematic_FadeCanvas");
                Undo.RegisterCreatedObjectUndo(fadeCanvasGo, "Create Fade Canvas");
            }
            var canvas = fadeCanvasGo.GetComponent<Canvas>();
            if (canvas == null) canvas = fadeCanvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;

            var scaler = fadeCanvasGo.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = fadeCanvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var raycaster = fadeCanvasGo.GetComponent<GraphicRaycaster>();
            if (raycaster == null) fadeCanvasGo.AddComponent<GraphicRaycaster>();

            var canvasGroup = fadeCanvasGo.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = fadeCanvasGo.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            var panelTransform = fadeCanvasGo.transform.Find("BlackOverlay");
            GameObject panelGo = panelTransform != null ? panelTransform.gameObject : null;
            if (panelGo == null)
            {
                panelGo = new GameObject("BlackOverlay");
                panelGo.transform.SetParent(fadeCanvasGo.transform, false);
                var img = panelGo.AddComponent<Image>();
                img.color = Color.black;
                var rect = panelGo.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = Vector2.zero;
                rect.anchoredPosition = Vector2.zero;
            }

            // 5. Build Timeline Asset
            if (!Directory.Exists(TimelineDirectory))
            {
                Directory.CreateDirectory(TimelineDirectory);
            }

            TimelineAsset timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(TimelinePath);
            if (timeline == null)
            {
                timeline = ScriptableObject.CreateInstance<TimelineAsset>();
                timeline.name = "TL_Village_Intro";
                AssetDatabase.CreateAsset(timeline, TimelinePath);
            }
            else
            {
                var existingTracks = timeline.GetOutputTracks();
                foreach (var track in existingTracks)
                {
                    timeline.DeleteTrack(track);
                }
            }

            // Create Cinemachine Track with ONE single continuous clip
            var cmTrack = timeline.CreateTrack<CinemachineTrack>(null, "Cinemachine Track");
            var clip = cmTrack.CreateClip<CinemachineShot>();
            clip.start = 0.0;
            clip.duration = 16.5;
            clip.displayName = "One-Shot Continuous Village Intro";
            var shotAsset = clip.asset as CinemachineShot;
            string guid = Guid.NewGuid().ToString();
            shotAsset.VirtualCamera.exposedName = guid;

            EditorUtility.SetDirty(timeline);
            AssetDatabase.SaveAssets();

            // 6. Setup Cutscene Manager & Director
            var mgrGo = GameObject.Find("Cinematic_Cutscene_Manager");
            if (mgrGo == null)
            {
                mgrGo = new GameObject("Cinematic_Cutscene_Manager");
                Undo.RegisterCreatedObjectUndo(mgrGo, "Create Cutscene Manager");
            }

            var director = mgrGo.GetComponent<PlayableDirector>();
            if (director == null) director = mgrGo.AddComponent<PlayableDirector>();
            director.playableAsset = timeline;
            director.playOnAwake = true;
            director.extrapolationMode = DirectorWrapMode.None;

            director.SetGenericBinding(cmTrack, brain);
            director.SetReferenceValue(guid, continuousCam);

            // Audio Sources
            var audioSources = mgrGo.GetComponents<AudioSource>();
            AudioSource ambienceSrc = audioSources.Length > 0 ? audioSources[0] : mgrGo.AddComponent<AudioSource>();
            AudioSource footstepSrc = audioSources.Length > 1 ? audioSources[1] : mgrGo.AddComponent<AudioSource>();

            ambienceSrc.playOnAwake = false;
            ambienceSrc.spatialBlend = 0f;
            ambienceSrc.loop = true;

            footstepSrc.playOnAwake = false;
            footstepSrc.spatialBlend = 0f;
            footstepSrc.loop = false;

            var windClip = AssetDatabase.LoadAssetAtPath<AudioClip>(WindAudioPath);
            var dirtClip = AssetDatabase.LoadAssetAtPath<AudioClip>(DirtFootstepAudioPath);
            var bgmClip = AssetDatabase.LoadAssetAtPath<AudioClip>(BgmAudioPath);

            // Setup CinematicVillageIntroDirector component
            var introDirector = mgrGo.GetComponent<CinematicVillageIntroDirector>();
            if (introDirector == null) introDirector = mgrGo.AddComponent<CinematicVillageIntroDirector>();

            var so = new SerializedObject(introDirector);
            so.Update();

            so.FindProperty("cutsceneDuration").floatValue = 16.5f;
            so.FindProperty("autoPlayOnStart").boolValue = true;
            so.FindProperty("allowSkip").boolValue = true;
            so.FindProperty("fadeInDuration").floatValue = 1.4f;
            so.FindProperty("fadeOutDuration").floatValue = 1.3f;

            so.FindProperty("playerController").objectReferenceValue = playerController;
            so.FindProperty("characterController").objectReferenceValue = characterController;
            so.FindProperty("playerAnimator").objectReferenceValue = playerAnimator;
            so.FindProperty("gameplayCamera").objectReferenceValue = gameplayCamera;

            so.FindProperty("playableDirector").objectReferenceValue = director;
            so.FindProperty("cinemachineBrain").objectReferenceValue = brain;
            so.FindProperty("continuousCam").objectReferenceValue = continuousCam;

            so.FindProperty("fadeCanvasGroup").objectReferenceValue = canvasGroup;

            so.FindProperty("ambienceAudioSource").objectReferenceValue = ambienceSrc;
            so.FindProperty("footstepAudioSource").objectReferenceValue = footstepSrc;

            so.FindProperty("dirtFootstepClip").objectReferenceValue = dirtClip;
            so.FindProperty("windAmbienceClip").objectReferenceValue = windClip;
            so.FindProperty("villageBgmClip").objectReferenceValue = bgmClip;

            so.ApplyModifiedProperties();

            // Mark Scene Dirty
            EditorSceneManager.MarkSceneDirty(activeScene);

            Debug.Log("[Cinematics Builder] One-Shot Continuous Cutscene with User Vista & Fade Out successfully updated!");
            return "SUCCESS: Cutscene updated with User Vista Angle & Fade Out at village approach.";
        }
    }
}
