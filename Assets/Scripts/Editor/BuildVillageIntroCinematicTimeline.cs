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
    /// Automated Builder for the Village Gate Cinematic Sequence in "CaMau_1954_Village.unity".
    /// Creates:
    /// - Timeline Asset: Assets/Timelines/TL_Village_Intro.playable with Cinemachine Track & 5 Shot Clips
    /// - Cinemachine Brain on Main Camera with EaseInOut blends (1.8s)
    /// - 5 CinemachineCameras under "Cinematic_Cutscene_Rig":
    ///     1. CM_Shot1_GateEstablishing: Chiếu camera từ cổng làng
    ///     2. CM_Shot2_FaceCloseUp: Zoom vào mặt nhân vật
    ///     3. CM_Shot3_BehindBack: Từ mặt quay qua sau lưng nhân vật
    ///     4. CM_Shot4_CranePullBackHigh: Zoom camera ra xa từ sau lưng và cao lên từ từ
    ///     5. CM_Shot5_GameplayBlend: Chuyển mượt về camera gameplay tại cổng làng
    /// - Fullscreen UI Fade Canvas "Cinematic_FadeCanvas"
    /// - Cutscene Manager "Cinematic_Cutscene_Manager" with CinematicVillageIntroDirector
    /// - Full reference binding and scene persistence
    /// </summary>
    public static class BuildVillageIntroCinematicTimeline
    {
        private const string ScenePath = "Assets/Scenes/CaMau_1954_Village.unity";
        private const string TimelineDirectory = "Assets/Timelines";
        private const string TimelinePath = "Assets/Timelines/TL_Village_Intro.playable";

        private const string WindAudioPath = "Assets/Audio/SFX/SFX_Ambience_RiceField_Wind.mp3";
        private const string BgmAudioPath = "Assets/Audio/Phat_Hien_Binh_Yen.mp3";

        [MenuItem("Tools/Hoa Tren Dat Do/Build Village Intro Cinematic")]
        public static string Execute()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            Debug.Log("[Cinematics Builder] Starting automated setup for Village Intro Cutscene...");

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
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 1.8f);
            brain.ShowDebugText = false;
            brain.enabled = true;

            // 3. Setup Cinematic_Cutscene_Rig & Virtual Cameras
            var rigGo = GameObject.Find("Cinematic_Cutscene_Rig");
            if (rigGo == null)
            {
                rigGo = new GameObject("Cinematic_Cutscene_Rig");
                Undo.RegisterCreatedObjectUndo(rigGo, "Create Cutscene Rig");
            }

            // Shot 1: Chiếu camera từ cổng làng
            var shot1 = SetupCinemachineCamera(rigGo, "CM_Shot1_GateEstablishing",
                new Vector3(-5.8f, 1.8f, 173.0f),
                Quaternion.Euler(2.85f, 353.77f, 0.0f),
                fov: 48f, priority: 25);

            // Shot 2: Zoom vào mặt nhân vật
            var shot2 = SetupCinemachineCamera(rigGo, "CM_Shot2_FaceCloseUp",
                new Vector3(-6.75f, 1.34f, 182.4f),
                Quaternion.Euler(0.0f, 351.12f, 0.0f),
                fov: 34f, priority: 10);

            // Shot 3: Từ mặt quay qua sau lưng nhân vật
            var shot3 = SetupCinemachineCamera(rigGo, "CM_Shot3_BehindBack",
                new Vector3(-6.7f, 1.45f, 185.6f),
                Quaternion.Euler(1.10f, 181.10f, 0.0f),
                fov: 42f, priority: 10);

            // Shot 4: Zoom camera ra xa từ sau lưng và cao lên từ từ
            var shot4 = SetupCinemachineCamera(rigGo, "CM_Shot4_CranePullBackHigh",
                new Vector3(-7.0f, 4.5f, 191.5f),
                Quaternion.Euler(5.98f, 180.0f, 0.0f),
                fov: 50f, priority: 10);

            // Shot 5: Chuyển mượt về camera gameplay tại cổng làng
            var shot5 = SetupCinemachineCamera(rigGo, "CM_Shot5_GameplayBlend",
                new Vector3(-7.0f, 2.05f, 186.6f),
                Quaternion.Euler(18.0f, 180.0f, 0.0f),
                fov: 56.0f, priority: 10);

            // Clean up any old unused shot cameras
            var oldShot = rigGo.transform.Find("CM_Shot1_NorthGateWide");
            if (oldShot != null) GameObject.DestroyImmediate(oldShot.gameObject);
            oldShot = rigGo.transform.Find("CM_Shot2_FollowBehind");
            if (oldShot != null) GameObject.DestroyImmediate(oldShot.gameObject);
            oldShot = rigGo.transform.Find("CM_Shot3_RiceFieldTrack");
            if (oldShot != null) GameObject.DestroyImmediate(oldShot.gameObject);
            oldShot = rigGo.transform.Find("CM_Shot4_HighVillageVista");
            if (oldShot != null) GameObject.DestroyImmediate(oldShot.gameObject);

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
            canvasGroup.alpha = 0f; // Hidden by default in Editor; activated during cutscene
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            // Fullscreen black panel
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
                // Clear existing tracks
                var existingTracks = timeline.GetOutputTracks();
                foreach (var track in existingTracks)
                {
                    timeline.DeleteTrack(track);
                }
            }

            // Create Cinemachine Track
            var cmTrack = timeline.CreateTrack<CinemachineTrack>(null, "Cinemachine Track");

            // Create clips
            var clip1 = cmTrack.CreateClip<CinemachineShot>();
            clip1.start = 0.0;
            clip1.duration = 3.0;
            clip1.displayName = "Shot 1: Gate Establishing";
            var shotAsset1 = clip1.asset as CinemachineShot;
            string guid1 = Guid.NewGuid().ToString();
            shotAsset1.VirtualCamera.exposedName = guid1;

            var clip2 = cmTrack.CreateClip<CinemachineShot>();
            clip2.start = 3.0;
            clip2.duration = 3.5;
            clip2.displayName = "Shot 2: Zoom Into Face";
            var shotAsset2 = clip2.asset as CinemachineShot;
            string guid2 = Guid.NewGuid().ToString();
            shotAsset2.VirtualCamera.exposedName = guid2;

            var clip3 = cmTrack.CreateClip<CinemachineShot>();
            clip3.start = 6.5;
            clip3.duration = 3.0;
            clip3.displayName = "Shot 3: Orbit Behind Back";
            var shotAsset3 = clip3.asset as CinemachineShot;
            string guid3 = Guid.NewGuid().ToString();
            shotAsset3.VirtualCamera.exposedName = guid3;

            var clip4 = cmTrack.CreateClip<CinemachineShot>();
            clip4.start = 9.5;
            clip4.duration = 3.5;
            clip4.displayName = "Shot 4: Crane Pull Back High";
            var shotAsset4 = clip4.asset as CinemachineShot;
            string guid4 = Guid.NewGuid().ToString();
            shotAsset4.VirtualCamera.exposedName = guid4;

            var clip5 = cmTrack.CreateClip<CinemachineShot>();
            clip5.start = 13.0;
            clip5.duration = 1.5;
            clip5.displayName = "Shot 5: Gameplay Perspective Blend";
            var shotAsset5 = clip5.asset as CinemachineShot;
            string guid5 = Guid.NewGuid().ToString();
            shotAsset5.VirtualCamera.exposedName = guid5;

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

            // Bind CinemachineBrain and Virtual Cameras to PlayableDirector
            director.SetGenericBinding(cmTrack, brain);
            director.SetReferenceValue(guid1, shot1);
            director.SetReferenceValue(guid2, shot2);
            director.SetReferenceValue(guid3, shot3);
            director.SetReferenceValue(guid4, shot4);
            director.SetReferenceValue(guid5, shot5);

            // Setup Audio Sources
            var audioSources = mgrGo.GetComponents<AudioSource>();
            AudioSource ambienceSrc = audioSources.Length > 0 ? audioSources[0] : mgrGo.AddComponent<AudioSource>();

            ambienceSrc.playOnAwake = false;
            ambienceSrc.spatialBlend = 0f;
            ambienceSrc.loop = true;

            var windClip = AssetDatabase.LoadAssetAtPath<AudioClip>(WindAudioPath);
            var bgmClip = AssetDatabase.LoadAssetAtPath<AudioClip>(BgmAudioPath);

            // Setup CinematicVillageIntroDirector component
            var introDirector = mgrGo.GetComponent<CinematicVillageIntroDirector>();
            if (introDirector == null) introDirector = mgrGo.AddComponent<CinematicVillageIntroDirector>();

            // Serialize all references using SerializedObject
            var so = new SerializedObject(introDirector);
            so.Update();

            so.FindProperty("cutsceneDuration").floatValue = 14.0f;
            so.FindProperty("autoPlayOnStart").boolValue = true;
            so.FindProperty("allowSkip").boolValue = true;
            so.FindProperty("fadeInDuration").floatValue = 1.6f;

            so.FindProperty("playerController").objectReferenceValue = playerController;
            so.FindProperty("characterController").objectReferenceValue = characterController;
            so.FindProperty("playerAnimator").objectReferenceValue = playerAnimator;
            so.FindProperty("gameplayCamera").objectReferenceValue = gameplayCamera;

            so.FindProperty("playableDirector").objectReferenceValue = director;
            so.FindProperty("cinemachineBrain").objectReferenceValue = brain;

            so.FindProperty("shot1GateWide").objectReferenceValue = shot1;
            so.FindProperty("shot2FaceCloseUp").objectReferenceValue = shot2;
            so.FindProperty("shot3BehindBack").objectReferenceValue = shot3;
            so.FindProperty("shot4CranePullBackHigh").objectReferenceValue = shot4;
            so.FindProperty("shot5GameplayBlend").objectReferenceValue = shot5;

            so.FindProperty("fadeCanvasGroup").objectReferenceValue = canvasGroup;

            so.FindProperty("ambienceAudioSource").objectReferenceValue = ambienceSrc;
            so.FindProperty("windAmbienceClip").objectReferenceValue = windClip;
            so.FindProperty("villageBgmClip").objectReferenceValue = bgmClip;

            so.ApplyModifiedProperties();

            // Mark Scene Dirty
            EditorSceneManager.MarkSceneDirty(activeScene);

            Debug.Log("[Cinematics Builder] Village Intro Cutscene setup successfully updated!");
            return "SUCCESS: Village Intro Cutscene updated with Gate Establishing -> Face Zoom -> Turn Behind -> Crane Pull Back High -> Gameplay Handover.";
        }

        private static CinemachineCamera SetupCinemachineCamera(GameObject parent, string name, Vector3 pos, Quaternion rot, float fov, int priority)
        {
            var childTransform = parent.transform.Find(name);
            GameObject camGo = childTransform != null ? childTransform.gameObject : null;
            if (camGo == null)
            {
                camGo = new GameObject(name);
                camGo.transform.SetParent(parent.transform, false);
                Undo.RegisterCreatedObjectUndo(camGo, $"Create {name}");
            }

            camGo.transform.position = pos;
            camGo.transform.rotation = rot;

            var cmCam = camGo.GetComponent<CinemachineCamera>();
            if (cmCam == null) cmCam = camGo.AddComponent<CinemachineCamera>();

            cmCam.Lens.FieldOfView = fov;
            cmCam.Lens.NearClipPlane = 0.1f;
            cmCam.Lens.FarClipPlane = 1000f;
            cmCam.Priority.Value = priority;

            return cmCam;
        }
    }
}
