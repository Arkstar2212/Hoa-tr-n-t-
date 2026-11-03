#pragma warning disable 0219
#pragma warning disable 0414
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using Unity.Cinemachine;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Production-grade Seamless Intro Cutscene Controller for "Hoa trên Đất Đỏ".
    /// Orchestrates the cinematic pipeline into Scene 1 (New Scene 1_Overhaul):
    /// - Phase 1: Historical Archival Documentary Video ("Tài liệu lịch sử") rendered cleanly at native aspect ratio
    ///            with synchronized stereo audio (or legacy 4-slide Vintage Film Projector fallback if video is disabled).
    /// - Phase 2: Optional Narrative Monologue on deep quiet black screen with audio crossfade to ambient river/forest nature.
    /// - Phase 3: Seamless Camera Transition panning smoothly from cosmic sky/clouds down to Minh in the forest, followed by gameplay unlock.
    /// Features Hold to Skip (Space / Escape) and full Inspector customization.
    /// </summary>
    public class IntroCutsceneController : MonoBehaviour
    {
        [System.Serializable]
        public struct HistorySlideData
        {
            public Sprite image;
            [TextArea(2, 4)] public string subtitle;
            public float displayDuration;

            public HistorySlideData(Sprite img, string sub, float duration = 5.0f)
            {
                image = img;
                subtitle = sub;
                displayDuration = duration;
            }
        }

        [Header("Pipeline Control")]

        [Tooltip("If true, starts immediately with sky descent from current camera position without showing archival video/reel")]
        [SerializeField] private bool startWithSkyDescent = false;
        [SerializeField] private bool forcePlayIntro = true;
        [SerializeField] private bool autoFindReferencesOnStart = true;

        [Header("Video Tư Liệu Lịch Sử (Archival Video Intro)")]
        [Tooltip("Sử dụng video tư liệu lịch sử (Assets/Audio/Tài liệu lịch sử (1).mp4) làm phần mở đầu trước khi hạ camera từ bầu trời")]
        [SerializeField] private bool useHistoricalVideo = true;
        [SerializeField] private VideoClip historicalVideoClip;
        [SerializeField, Range(0f, 1f)] private float videoVolume = 1.0f;
        [SerializeField] private float videoFadeInDuration = 0.65f;
        [SerializeField] private float videoFadeOutDuration = 0.85f;
        [Tooltip("Nếu bật, sau khi phát hết video tư liệu sẽ chạy thêm chữ Lời Tựa (Giai Đoạn 2) trước khi hạ camera xuống rừng")]
        [SerializeField] private bool playMonologueAfterVideo = false;

        [Header("Giai Đoạn 1 (Dự Phòng): Máy Chiếu Phim Cổ (Vintage Film Projector)")]
        [SerializeField] private HistorySlideData[] historySlides;
        [SerializeField] private float slideKenBurnsEndScale = 1.05f;
        [SerializeField] private float slideCrossfadeDuration = 0.6f;
        [SerializeField] private float shutterFlashDuration = 0.08f;
        [SerializeField] private float filmJitterAmount = 2.0f;
        [SerializeField] private float filmGrainPulseSpeed = 16.0f;
        [SerializeField] private AudioClip projectorHumClip;
        [SerializeField] private AudioClip slideAdvanceSfxClip;

        [Header("Giai Đoạn 2: Lời Tựa Hành Trình (Narrative Monologue)")]
        [SerializeField]
        [TextArea(2, 4)]
        private string[] narrativeLines = new string[]
        {
            "Năm 1954 — Sau Hiệp định Genève, vùng đất Đỏ Cà Mau trở thành điểm hẹn của lịch sử.",
            "Có những cuộc chia ly là để hẹn ngày trở lại, có những bước chân đi là vì một ngày mai độc lập.",
            "Với cậu bé An, phía trước không chỉ là một chuyến hành trình...",
            "...mà là lời hứa, là sứ mệnh, và là niềm hy vọng về một quê hương trọn vẹn bình yên."
        };
        [SerializeField] private float monologueFadeInDuration = 0.6f;
        [SerializeField] private float monologueHoldDuration = 2.0f;
        [SerializeField] private float monologueFadeOutDuration = 0.6f;
        [SerializeField] private float monologueAudioCrossfadeDuration = 2.0f;

        [Header("Giai Đoạn 3: Chuyển Cảnh Camera Mượt Mà (Seamless Camera Transition)")]
        [SerializeField] private CinemachineCamera vcamIntroSky;
        [SerializeField] private CinemachineCamera vcamGameplay;
        [SerializeField] private CinemachineBrain cinemachineBrain;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private float blackCurtainFadeOutDuration = 0.45f;
        [SerializeField] private float cameraPanDuration = 11.5f;
        [SerializeField] private Vector3 skyZenithPosition = new Vector3(-3.20f, 75.0f, -13.5f);
        [SerializeField] private Vector3 forestGroundPosition = new Vector3(-3.20f, 0.10f, -13.5f);
        [SerializeField] private AnimationCurve descentProfileCurve;

        public static AnimationCurve CreateDefaultDescentCurve()
        {
            var curve = new AnimationCurve();
            // Calibrated for 11.5s descent:
            // 0.0s - 1.0s (t=0 to 0.087): Empty deep blue sky (Y=75m -> 67.5m)
            // 1.0s - 2.2s (t=0.087 to 0.191): Sparse clouds (Y=67.5m -> 56.5m)
            // 2.2s - 5.0s (t=0.191 to 0.435): Dense clouds (Y=56.5m -> 36.5m) [2.8s in dense clouds]
            // 5.0s - 8.7s (t=0.435 to 0.757): Open daytime blue sky (Y=36.5m -> 15.0m) [3.7s in open sky]
            // 8.7s - 11.5s (t=0.757 to 1.000): Forest canopy & ground settle (Y=15.0m -> 0.10m)
            curve.AddKey(new Keyframe(0.000f, 0.000f, 0.00f, 0.85f));
            curve.AddKey(new Keyframe(0.087f, 0.100f, 1.05f, 1.05f));
            curve.AddKey(new Keyframe(0.191f, 0.247f, 1.15f, 1.15f));
            curve.AddKey(new Keyframe(0.435f, 0.514f, 1.05f, 1.05f));
            curve.AddKey(new Keyframe(0.757f, 0.801f, 0.85f, 0.85f));
            curve.AddKey(new Keyframe(1.000f, 1.000f, 0.00f, 0.00f));
            for (int i = 0; i < curve.length; i++)
            {
                curve.SmoothTangents(i, 0.25f);
            }
            return curve;
        }

        [Header("Audio Sources & Crossfade")]
        [SerializeField] private AudioSource projectorAudioSource;
        [SerializeField] private AudioSource ambientAudioSource;
        [SerializeField] private AudioSource voiceoverAudioSource;
        [SerializeField] private AudioSource videoAudioSource;
        [SerializeField] private VideoPlayer historicalVideoPlayer;
        [SerializeField, Range(0f, 1f)] private float maxProjectorVolume = 0.55f;
        [SerializeField, Range(0f, 1f)] private float targetAmbientVolume = 0.70f;
        [SerializeField, Range(0f, 1f)] private float voiceoverVolume = 0.95f;

        [Header("Lồng Tiếng Thuyết Minh (Voiceover Audio Clips)")]
        [SerializeField] private AudioClip[] slideVoiceoverClips;
        [SerializeField] private AudioClip[] monologueVoiceoverClips;

        [Header("Hold to Skip Settings")]
        [SerializeField] private bool allowSkip = true;
        [SerializeField] private float holdToSkipDuration = 1.0f;

        // UI Canvas Components
        [Header("UI Canvas Hierarchy (Auto-generated if missing)")]
        [SerializeField] private Canvas cutsceneCanvas;
        [SerializeField] private CanvasGroup cutsceneCanvasGroup;
        [SerializeField] private Image blackCurtainImage;
        [SerializeField] private RectTransform videoScreenRoot;
        [SerializeField] private RawImage videoRawImage;
        [SerializeField] private CanvasGroup videoCanvasGroup;
        [SerializeField] private AspectRatioFitter videoAspectFitter;
        [SerializeField] private RectTransform projectorApertureRect;
        [SerializeField] private Image slideImage;
        [SerializeField] private CanvasGroup slideImageGroup;
        [SerializeField] private Image vignetteOverlayImage;
        [SerializeField] private Image filmScratchesOverlayImage;
        [SerializeField] private CanvasGroup shutterFlashGroup;
        [SerializeField] private TextMeshProUGUI subtitleText;
        [SerializeField] private CanvasGroup subtitleGroup;
        [SerializeField] private TextMeshProUGUI monologueText;
        [SerializeField] private CanvasGroup monologueGroup;
        [SerializeField] private TextMeshProUGUI archivalHeaderText;
        [SerializeField] private RectTransform skipPromptRoot;
        [SerializeField] private Image skipProgressBar;
        [SerializeField] private CanvasGroup skipPromptGroup;
        [SerializeField] private RectTransform skipFillMaskRect;
        [SerializeField] private TextMeshProUGUI skipKeycapBaseText;
        [SerializeField] private TextMeshProUGUI skipKeycapInvertedText;
        [SerializeField] private TMP_FontAsset cutsceneFontAsset;

        // Runtime State
        private Coroutine activeCutsceneRoutine;
        private RenderTexture activeVideoRenderTexture;
        private bool isCutsceneRunning = false;
        private bool isSkipTriggered = false;
        private bool isSkyDescending = false;
        private float currentSkipHoldTimer = 0f;
        private Vector2 baseAperturePos = Vector2.zero;
        private CinemachineBlendDefinition originalDefaultBlend;

        public bool IsCutsceneRunning => isCutsceneRunning;
        public VideoClip HistoricalVideoClip => historicalVideoClip;

        private void Awake()
        {
            if (!enabled)
            {
                enabled = true;
            }

            if (autoFindReferencesOnStart)
            {
                ResolveSceneReferences();
            }

            if (vcamIntroSky != null)
            {
                vcamIntroSky.gameObject.SetActive(true);
                vcamIntroSky.enabled = true;
                vcamIntroSky.transform.position = skyZenithPosition;
                vcamIntroSky.Priority = 35;
            }
            if (vcamGameplay != null)
            {
                vcamGameplay.Priority = 10;
            }
            if (cinemachineBrain != null && cinemachineBrain.UpdateMethod == CinemachineBrain.UpdateMethods.ManualUpdate)
            {
                cinemachineBrain.ManualUpdate();
            }

#if UNITY_EDITOR
            IntroCutsceneState.HasViewedInSession = false;
#endif
            bool willPlayPrologue = (forcePlayIntro || IntroCutsceneState.ShouldPlayIntroCutscene) && !startWithSkyDescent;
            if (willPlayPrologue)
            {
                // Mute wind ambience immediately on frame 0 so it never bleeds into the opening frame
                if (ambientAudioSource != null)
                {
                    ambientAudioSource.volume = 0f;
                }
                if (cutsceneCanvas != null)
                {
                    cutsceneCanvas.gameObject.SetActive(true);
                    if (cutsceneCanvasGroup != null) cutsceneCanvasGroup.alpha = 1f;
                    if (blackCurtainImage != null) blackCurtainImage.color = new Color(0.02f, 0.018f, 0.016f, 1f);
                    if (projectorApertureRect != null) projectorApertureRect.gameObject.SetActive(false);
                    if (videoCanvasGroup != null) videoCanvasGroup.alpha = 0f;
                }
            }
            else if (!isCutsceneRunning && cutsceneCanvas != null)
            {
                cutsceneCanvas.gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            // Evaluate whether cutscene should play
#if UNITY_EDITOR
            IntroCutsceneState.HasViewedInSession = false;
#endif
            bool shouldPlay = forcePlayIntro || IntroCutsceneState.ShouldPlayIntroCutscene;

            if (shouldPlay)
            {
                StartIntroCutscene();
            }
            else
            {
                BypassCutsceneAndPrepareGameplay();
            }
        }

        private void Update()
        {
            if (!isCutsceneRunning) return;

            UpdateFilmJitterAndGrain();
            UpdateHoldToSkip();
        }

        private void OnDisable()
        {
            CleanupVideoPlayback();
        }

        private void OnDestroy()
        {
            CleanupVideoPlayback();
        }

        // =====================================================================================
        // REFERENCE RESOLUTION & AUTO-SETUP
        // =====================================================================================
        public void ResolveSceneReferences()
        {
            if (playerController == null)
            {
                playerController = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            }
            if (playerTransform == null && playerController != null)
            {
                playerTransform = playerController.transform;
            }

            if (cinemachineBrain == null)
            {
                cinemachineBrain = UnityEngine.Object.FindAnyObjectByType<CinemachineBrain>();
            }

            var allCameras = UnityEngine.Object.FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Exclude);
            foreach (var cam in allCameras)
            {
                if ((vcamIntroSky == null || !vcamIntroSky.gameObject.activeInHierarchy) && (cam.name.Contains("IntroSky") || cam.name.Contains("Intro_Sky") || cam.name.Contains("VCam_IntroSky")))
                {
                    vcamIntroSky = cam;
                }
                else if (vcamGameplay == null && (cam.name.Contains("2.5D") || cam.name.Contains("Gameplay") || cam.name.Contains("VCam_Gameplay")))
                {
                    vcamGameplay = cam;
                }
            }

            if (vcamIntroSky != null && vcamIntroSky.transform.position.y > 50f)
            {
                skyZenithPosition = vcamIntroSky.transform.position;
            }

            // Create AudioSources if unassigned
            if (projectorAudioSource == null)
            {
                Transform existingProj = transform.Find("Audio_VintageProjectorHum");
                GameObject pGo = existingProj != null ? existingProj.gameObject : new GameObject("Audio_VintageProjectorHum");
                pGo.transform.SetParent(transform, false);
                projectorAudioSource = pGo.GetComponent<AudioSource>();
                if (projectorAudioSource == null) projectorAudioSource = pGo.AddComponent<AudioSource>();
                projectorAudioSource.playOnAwake = false;
                projectorAudioSource.loop = true;
                projectorAudioSource.volume = 0f;
            }

            if (ambientAudioSource == null)
            {
                var audioSources = UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Exclude);
                foreach (var src in audioSources)
                {
                    if (src != null && src.transform != transform && !src.transform.IsChildOf(transform))
                    {
                        if (src.gameObject.name.Contains("Wind") || (src.clip != null && src.clip.name.ToLowerInvariant().Contains("wind")))
                        {
                            ambientAudioSource = src;
                            break;
                        }
                    }
                }
            }

            if (voiceoverAudioSource == null)
            {
                Transform existingVo = transform.Find("Audio_VoiceoverNarration");
                GameObject vGo = existingVo != null ? existingVo.gameObject : new GameObject("Audio_VoiceoverNarration");
                vGo.transform.SetParent(transform, false);
                voiceoverAudioSource = vGo.GetComponent<AudioSource>();
                if (voiceoverAudioSource == null) voiceoverAudioSource = vGo.AddComponent<AudioSource>();
                voiceoverAudioSource.playOnAwake = false;
                voiceoverAudioSource.loop = false;
                voiceoverAudioSource.volume = voiceoverVolume;
            }

            EnsureVideoComponentsExist();
            AutoLoadHistoricalVideoIfEmpty();
            AutoLoadVoiceoverClipsIfEmpty();

            // Default Slides if empty
            if (historySlides == null || historySlides.Length == 0)
            {
                SetupDefaultSlideData();
            }

            if (cutsceneCanvas == null || videoScreenRoot == null || videoRawImage == null)
            {
                EnsureCutsceneCanvasExists();
            }
        }

        private void EnsureVideoComponentsExist()
        {
            Transform existingVidTr = transform.Find("Audio_HistoricalVideo");
            GameObject vidGo = existingVidTr != null ? existingVidTr.gameObject : new GameObject("Audio_HistoricalVideo");
            vidGo.transform.SetParent(transform, false);

            if (videoAudioSource == null)
            {
                videoAudioSource = vidGo.GetComponent<AudioSource>();
                if (videoAudioSource == null) videoAudioSource = vidGo.AddComponent<AudioSource>();
            }
            videoAudioSource.playOnAwake = false;
            videoAudioSource.loop = false;
            videoAudioSource.spatialBlend = 0f;
            videoAudioSource.volume = videoVolume;

            if (historicalVideoPlayer == null)
            {
                historicalVideoPlayer = vidGo.GetComponent<VideoPlayer>();
                if (historicalVideoPlayer == null) historicalVideoPlayer = vidGo.AddComponent<VideoPlayer>();
            }
            historicalVideoPlayer.playOnAwake = false;
            historicalVideoPlayer.isLooping = false;
            historicalVideoPlayer.waitForFirstFrame = true;
            historicalVideoPlayer.skipOnDrop = true;
            historicalVideoPlayer.renderMode = VideoRenderMode.RenderTexture;
            historicalVideoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        }

        public void AutoLoadHistoricalVideoIfEmpty()
        {
#if UNITY_EDITOR
            if (historicalVideoClip == null)
            {
                historicalVideoClip = UnityEditor.AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/Audio/Tài liệu lịch sử (1).mp4");
                if (historicalVideoClip == null)
                {
                    string[] guids = UnityEditor.AssetDatabase.FindAssets("t:VideoClip", new[] { "Assets/Audio", "Assets" });
                    foreach (string g in guids)
                    {
                        string p = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
                        var candidate = UnityEditor.AssetDatabase.LoadAssetAtPath<VideoClip>(p);
                        if (candidate != null)
                        {
                            historicalVideoClip = candidate;
                            if (p.IndexOf("lịch sử", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                p.IndexOf("lich su", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                break;
                            }
                        }
                    }
                }
            }
#endif
        }

        public void AssignHistoricalVideo(VideoClip clip, bool enableVideoMode = true)
        {
            historicalVideoClip = clip;
            useHistoricalVideo = enableVideoMode;
            EnsureVideoComponentsExist();
            if (historicalVideoPlayer != null)
            {
                historicalVideoPlayer.clip = clip;
            }
        }

        private void SetupDefaultSlideData()
        {
            historySlides = new HistorySlideData[4];
            historySlides[0] = new HistorySlideData(null, "Năm 1954 — Sau Hiệp định Genève, vùng đất Đỏ Cà Mau trở thành điểm tập kết 200 ngày lịch sử.", 2.8f);
            historySlides[1] = new HistorySlideData(null, "Hàng vạn đồng bào, chiến sĩ và những người con miền Nam tề tựu bên đôi bờ sông Đốc chuẩn bị cho chuyến đi xa.", 2.8f);
            historySlides[2] = new HistorySlideData(null, "Những ánh mắt lưu luyến, những cái ôm siết chặt và lời thề son sắt hẹn ngày non sông thống nhất.", 2.8f);
            historySlides[3] = new HistorySlideData(null, "Chuyến tàu chuyển quân rời bến trong nắng chiều, mang theo hành trang của cả một thế hệ kiên trung.", 3.0f);
        }

        public void AutoLoadVoiceoverClipsIfEmpty()
        {
#if UNITY_EDITOR
            if (slideVoiceoverClips == null || slideVoiceoverClips.Length < 4 || slideVoiceoverClips[0] == null)
            {
                slideVoiceoverClips = new AudioClip[4];
                for (int i = 0; i < 4; i++)
                {
                    string p = $"Assets/Audio/Voiceover/VO_Slide_0{i + 1}.mp3";
                    slideVoiceoverClips[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(p);
                }
            }

            if (monologueVoiceoverClips == null || monologueVoiceoverClips.Length < 4 || monologueVoiceoverClips[0] == null)
            {
                monologueVoiceoverClips = new AudioClip[4];
                for (int i = 0; i < 4; i++)
                {
                    string p = $"Assets/Audio/Voiceover/VO_Mono_0{i + 1}.mp3";
                    monologueVoiceoverClips[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(p);
                }
            }
#endif
        }

        // =====================================================================================
        // CUTSCENE LIFECYCLE
        // =====================================================================================
        public void StartIntroCutscene()
        {
            if (isCutsceneRunning) return;

            isCutsceneRunning = true;
            isSkipTriggered = false;
            currentSkipHoldTimer = 0f;

            // 1. Lock Player Controls
            if (playerController != null)
            {
                playerController.enabled = false;
                var rb = playerController.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }

            // 2. Setup Cameras
            if (cinemachineBrain != null)
            {
                originalDefaultBlend = cinemachineBrain.DefaultBlend;
            }

            if (vcamIntroSky != null)
            {
                vcamIntroSky.gameObject.SetActive(true);
                vcamIntroSky.enabled = true;
                vcamIntroSky.transform.position = skyZenithPosition;
                vcamIntroSky.Priority = 35; // Elevated priority to guarantee live camera
            }
            if (vcamGameplay != null)
            {
                vcamGameplay.Priority = 10;
            }

            if (startWithSkyDescent)
            {
                CleanupVideoPlayback();
                if (cutsceneCanvas != null)
                {
                    cutsceneCanvas.gameObject.SetActive(false);
                }
                activeCutsceneRoutine = StartCoroutine(DirectSkyDescentRoutine());
                return;
            }

            // 3. Prepare Canvas
            bool willPlayVideo = useHistoricalVideo && historicalVideoClip != null;
            if (cutsceneCanvas != null)
            {
                cutsceneCanvas.gameObject.SetActive(true);
            }
            if (cutsceneCanvasGroup != null)
            {
                cutsceneCanvasGroup.alpha = 1f;
                cutsceneCanvasGroup.interactable = true;
                cutsceneCanvasGroup.blocksRaycasts = true;
            }
            if (blackCurtainImage != null)
            {
                blackCurtainImage.color = willPlayVideo
                    ? new Color(0.02f, 0.018f, 0.016f, 1f)
                    : new Color(0.03f, 0.025f, 0.02f, 1f);
            }
            if (videoScreenRoot != null)
            {
                videoScreenRoot.gameObject.SetActive(willPlayVideo);
            }
            if (videoCanvasGroup != null)
            {
                videoCanvasGroup.alpha = 0f;
            }
            if (projectorApertureRect != null)
            {
                projectorApertureRect.gameObject.SetActive(!willPlayVideo);
            }
            if (subtitleGroup != null)
            {
                subtitleGroup.alpha = 0f;
            }
            if (monologueGroup != null)
            {
                monologueGroup.alpha = 0f;
            }
            if (vignetteOverlayImage != null)
            {
                vignetteOverlayImage.gameObject.SetActive(!willPlayVideo);
            }
            if (filmScratchesOverlayImage != null)
            {
                filmScratchesOverlayImage.gameObject.SetActive(!willPlayVideo);
            }
            if (skipPromptGroup != null)
            {
                skipPromptGroup.alpha = 0f;
            }

            // 4. Start Audio & Pipeline Coroutine
            activeCutsceneRoutine = StartCoroutine(CutscenePipelineRoutine());
        }

        private IEnumerator DirectSkyDescentRoutine()
        {
            // Ambient wind / nature sound
            if (ambientAudioSource != null)
            {
                ambientAudioSource.volume = targetAmbientVolume;
                if (!ambientAudioSource.isPlaying) ambientAudioSource.Play();
            }

            // Ensure starting view is the pristine high blue sky
            if (vcamIntroSky != null)
            {
                vcamIntroSky.enabled = true;
                vcamIntroSky.transform.position = skyZenithPosition;
                vcamIntroSky.Lens.FieldOfView = 38f;
            }

            // Brief serene hold (0.8s) to take in the vast blue zenith sky
            yield return new WaitForSeconds(0.8f);

            // Execute descent through clouds to game background and ground
            yield return StartCoroutine(ExecuteCameraPanAndGameplayHandoff());
        }

        private IEnumerator CutscenePipelineRoutine()
        {
            // Silence ambient wind initially during prologue
            if (ambientAudioSource != null)
            {
                ambientAudioSource.volume = 0f;
                if (!ambientAudioSource.isPlaying) ambientAudioSource.Play();
            }

            bool willPlayVideo = useHistoricalVideo && historicalVideoClip != null;

            if (willPlayVideo)
            {
                // =================================================================================
                // GIAI ĐOẠN 1 (MỚI): PHÁT VIDEO TƯ LIỆU LỊCH SỬ ("Tài liệu lịch sử (1).mp4")
                // =================================================================================
                if (projectorAudioSource != null && projectorAudioSource.isPlaying)
                {
                    projectorAudioSource.Stop();
                }

                yield return StartCoroutine(PlayHistoricalVideoRoutine());

                if (!isSkipTriggered && playMonologueAfterVideo)
                {
                    yield return new WaitForSeconds(0.35f);
                    yield return StartCoroutine(PlayMonologueSequenceRoutine());
                }
            }
            else
            {
                // =================================================================================
                // GIAI ĐOẠN 1 (DỰ PHÒNG): MÁY CHIẾU PHIM CỔ (VINTAGE REEL SLIDESHOW)
                // =================================================================================
                if (projectorAudioSource != null)
                {
                    if (projectorHumClip != null) projectorAudioSource.clip = projectorHumClip;
                    projectorAudioSource.volume = maxProjectorVolume;
                    projectorAudioSource.Play();
                }

                yield return new WaitForSeconds(0.6f);

                if (historySlides != null && historySlides.Length > 0)
                {
                    for (int i = 0; i < historySlides.Length; i++)
                    {
                        if (isSkipTriggered) break;

                        var slide = historySlides[i];

                        if (slideImage != null && slide.image != null)
                        {
                            slideImage.sprite = slide.image;
                            slideImage.preserveAspect = true;
                        }

                        if (subtitleText != null)
                        {
                            subtitleText.text = slide.subtitle;
                        }

                        yield return StartCoroutine(PlayShutterFlashRoutine());

                        AudioClip slideVo = (slideVoiceoverClips != null && i < slideVoiceoverClips.Length) ? slideVoiceoverClips[i] : null;
                        if (slideVo != null && voiceoverAudioSource != null)
                        {
                            voiceoverAudioSource.Stop();
                            voiceoverAudioSource.clip = slideVo;
                            voiceoverAudioSource.volume = voiceoverVolume;
                            voiceoverAudioSource.Play();
                        }

                        float elapsed = 0f;
                        float duration = (slideVo != null) ? (slideVo.length + 0.6f) : Mathf.Max(2.5f, slide.displayDuration);

                        if (slideImageGroup != null) slideImageGroup.alpha = 1f;
                        if (subtitleGroup != null) StartCoroutine(FadeCanvasGroupRoutine(subtitleGroup, 0f, 1f, slideCrossfadeDuration));

                        while (elapsed < duration)
                        {
                            if (isSkipTriggered) break;

                            elapsed += Time.deltaTime;
                            float progress = Mathf.Clamp01(elapsed / duration);

                            if (projectorApertureRect != null)
                            {
                                float scale = Mathf.Lerp(1.0f, slideKenBurnsEndScale, progress);
                                projectorApertureRect.localScale = new Vector3(scale, scale, 1f);
                            }

                            if (duration - elapsed <= slideCrossfadeDuration && subtitleGroup != null && subtitleGroup.alpha > 0.95f)
                            {
                                StartCoroutine(FadeCanvasGroupRoutine(subtitleGroup, 1f, 0f, slideCrossfadeDuration));
                            }

                            yield return null;
                        }

                        if (isSkipTriggered) break;

                        if (i < historySlides.Length - 1)
                        {
                            if (slideImageGroup != null) slideImageGroup.alpha = 0f;
                            yield return new WaitForSeconds(0.12f);
                        }
                    }
                }

                if (slideImageGroup != null) slideImageGroup.alpha = 0f;
                if (subtitleGroup != null) subtitleGroup.alpha = 0f;
                if (projectorApertureRect != null)
                {
                    projectorApertureRect.localScale = Vector3.one;
                    projectorApertureRect.gameObject.SetActive(false);
                }
                if (vignetteOverlayImage != null) vignetteOverlayImage.gameObject.SetActive(false);
                if (filmScratchesOverlayImage != null) filmScratchesOverlayImage.gameObject.SetActive(false);

                yield return new WaitForSeconds(0.4f);

                // GIAI ĐOẠN 2: LỜI TỰA HÀNH TRÌNH
                if (!isSkipTriggered)
                {
                    yield return StartCoroutine(PlayMonologueSequenceRoutine());
                }
            }

            if (monologueGroup != null) monologueGroup.alpha = 0f;

            // =================================================================================
            // GIAI ĐOẠN 3: CAMERA PAN TỪ TRỜI XANH ĐẬM QUA MÂY XUỐNG RỪNG & MINH
            // =================================================================================
            if (!isSkipTriggered)
            {
                yield return StartCoroutine(ExecuteCameraPanAndGameplayHandoff());
            }
        }

        private IEnumerator PlayHistoricalVideoRoutine()
        {
            if (historicalVideoClip == null) yield break;

            EnsureVideoComponentsExist();
            if (historicalVideoPlayer == null || videoRawImage == null) yield break;

            int vidW = Mathf.Max(64, (int)historicalVideoClip.width);
            int vidH = Mathf.Max(64, (int)historicalVideoClip.height);

            // Allocate clean RenderTexture matching exact native video resolution (e.g. 1440x1080)
            ReleaseVideoRenderTexture();
            activeVideoRenderTexture = new RenderTexture(vidW, vidH, 0, RenderTextureFormat.ARGB32)
            {
                name = "RT_IntroCutscene_HistoricalVideo",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                antiAliasing = 1
            };
            activeVideoRenderTexture.Create();

            // Clear RenderTexture to pure black so frame 0 never flashes stale GPU memory
            RenderTexture prevActive = RenderTexture.active;
            RenderTexture.active = activeVideoRenderTexture;
            GL.Clear(true, true, Color.black);
            RenderTexture.active = prevActive;

            if (videoAspectFitter != null)
            {
                videoAspectFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                videoAspectFitter.aspectRatio = (float)vidW / vidH;
            }

            videoRawImage.texture = activeVideoRenderTexture;
            videoRawImage.color = Color.white;

            if (videoScreenRoot != null) videoScreenRoot.gameObject.SetActive(true);
            if (videoCanvasGroup != null) videoCanvasGroup.alpha = 0f;

            // Configure VideoPlayer + AudioSource BEFORE calling Prepare()
            historicalVideoPlayer.Stop();
            historicalVideoPlayer.source = VideoSource.VideoClip;
            historicalVideoPlayer.clip = historicalVideoClip;
            historicalVideoPlayer.isLooping = false;
            historicalVideoPlayer.waitForFirstFrame = true;
            historicalVideoPlayer.skipOnDrop = true;
            historicalVideoPlayer.renderMode = VideoRenderMode.RenderTexture;
            historicalVideoPlayer.targetTexture = activeVideoRenderTexture;

            if (videoAudioSource != null && historicalVideoClip.audioTrackCount > 0)
            {
                videoAudioSource.Stop();
                videoAudioSource.clip = null;
                videoAudioSource.volume = videoVolume;
                historicalVideoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
                historicalVideoPlayer.EnableAudioTrack(0, true);
                historicalVideoPlayer.SetTargetAudioSource(0, videoAudioSource);
            }
            else
            {
                historicalVideoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
            }

            bool isFinished = false;
            bool hasError = false;
            VideoPlayer.EventHandler onLoopPoint = _ => isFinished = true;
            VideoPlayer.ErrorEventHandler onError = (_, msg) =>
            {
                Debug.LogWarning($"[IntroCutsceneController] VideoPlayer warning/error: {msg}");
                hasError = true;
            };

            historicalVideoPlayer.loopPointReached += onLoopPoint;
            historicalVideoPlayer.errorReceived += onError;

            try
            {
                historicalVideoPlayer.Prepare();

                float prepareTimeout = 6.0f;
                float prepTimer = 0f;
                while (!historicalVideoPlayer.isPrepared && !hasError && !isSkipTriggered && prepTimer < prepareTimeout)
                {
                    prepTimer += Time.unscaledDeltaTime;
                    yield return null;
                }

                if (hasError || isSkipTriggered || !historicalVideoPlayer.isPrepared)
                {
                    yield break;
                }

                historicalVideoPlayer.Play();

                // Wait until the very first frame is decoded onto the RenderTexture
                float firstFrameWait = 0f;
                while (!isSkipTriggered && !hasError && historicalVideoPlayer.frame < 0 && firstFrameWait < 1.0f)
                {
                    firstFrameWait += Time.unscaledDeltaTime;
                    yield return null;
                }

                double clipDuration = historicalVideoClip.length;
                float playbackElapsed = 0f;
                float fadeInDur = Mathf.Max(0.05f, videoFadeInDuration);
                float fadeOutDur = Mathf.Max(0.15f, videoFadeOutDuration);

                while (!isFinished && !hasError && !isSkipTriggered)
                {
                    if (Application.isFocused)
                    {
                        playbackElapsed += Time.deltaTime;
                    }

                    double currentVidTime = historicalVideoPlayer.time;
                    double remaining = clipDuration - currentVidTime;

                    // Compute smooth envelope alpha (fade-in at start, fade-out near end)
                    float inAlpha = Mathf.Clamp01((float)(currentVidTime / fadeInDur));
                    if (playbackElapsed > fadeInDur && inAlpha < 1f) inAlpha = 1f;

                    float outAlpha = remaining <= fadeOutDur
                        ? Mathf.Clamp01((float)(remaining / fadeOutDur))
                        : 1f;

                    float combinedAlpha = Mathf.SmoothStep(0f, 1f, Mathf.Min(inAlpha, outAlpha));
                    if (videoCanvasGroup != null)
                    {
                        videoCanvasGroup.alpha = combinedAlpha;
                    }

                    // Smooth audio fade-out & subtle wind cross-in during final second of the archival film
                    if (remaining <= fadeOutDur)
                    {
                        float endFactor = 1f - outAlpha;
                        if (videoAudioSource != null)
                        {
                            videoAudioSource.volume = Mathf.Lerp(videoVolume, 0f, endFactor);
                        }
                        if (ambientAudioSource != null && !playMonologueAfterVideo)
                        {
                            ambientAudioSource.volume = Mathf.Lerp(0f, targetAmbientVolume * 0.45f, endFactor);
                        }
                    }
                    else if (videoAudioSource != null)
                    {
                        videoAudioSource.volume = videoVolume;
                    }

                    // Safety fallback if clipDuration has clearly elapsed
                    if (clipDuration > 0.5 && playbackElapsed > (float)clipDuration + 1.5f)
                    {
                        break;
                    }

                    yield return null;
                }
            }
            finally
            {
                if (historicalVideoPlayer != null)
                {
                    historicalVideoPlayer.loopPointReached -= onLoopPoint;
                    historicalVideoPlayer.errorReceived -= onError;
                }
                CleanupVideoPlayback();
            }

            if (!isSkipTriggered)
            {
                yield return new WaitForSeconds(0.20f);
            }
        }

        private IEnumerator PlayMonologueSequenceRoutine()
        {
            if (narrativeLines == null || narrativeLines.Length == 0) yield break;

            for (int lineIdx = 0; lineIdx < narrativeLines.Length; lineIdx++)
            {
                if (isSkipTriggered) break;

                string line = narrativeLines[lineIdx];
                if (monologueText != null)
                {
                    monologueText.text = line;
                }

                AudioClip monoVo = (monologueVoiceoverClips != null && lineIdx < monologueVoiceoverClips.Length) ? monologueVoiceoverClips[lineIdx] : null;
                if (monoVo != null && voiceoverAudioSource != null)
                {
                    voiceoverAudioSource.Stop();
                    voiceoverAudioSource.clip = monoVo;
                    voiceoverAudioSource.volume = voiceoverVolume;
                    voiceoverAudioSource.Play();
                }

                if (lineIdx == narrativeLines.Length - 1)
                {
                    StartCoroutine(CrossfadeAudioRoutine(monologueAudioCrossfadeDuration));
                }

                if (monologueGroup != null)
                {
                    yield return StartCoroutine(FadeCanvasGroupRoutine(monologueGroup, 0f, 1f, monologueFadeInDuration));
                }

                float holdTimer = 0f;
                float targetHold = (monoVo != null) ? Mathf.Max(0.5f, monoVo.length - monologueFadeInDuration + 0.5f) : monologueHoldDuration;
                while (holdTimer < targetHold)
                {
                    if (isSkipTriggered) break;
                    holdTimer += Time.deltaTime;
                    yield return null;
                }

                if (isSkipTriggered) break;

                if (monologueGroup != null)
                {
                    yield return StartCoroutine(FadeCanvasGroupRoutine(monologueGroup, 1f, 0f, monologueFadeOutDuration));
                }

                yield return new WaitForSeconds(0.35f);
            }
        }

        public void CleanupVideoPlayback()
        {
            if (historicalVideoPlayer != null)
            {
                if (historicalVideoPlayer.isPlaying || historicalVideoPlayer.isPrepared)
                {
                    historicalVideoPlayer.Stop();
                }
                historicalVideoPlayer.targetTexture = null;
            }

            if (videoAudioSource != null && videoAudioSource.isPlaying)
            {
                videoAudioSource.Stop();
            }

            if (videoRawImage != null)
            {
                videoRawImage.texture = null;
            }

            if (videoCanvasGroup != null)
            {
                videoCanvasGroup.alpha = 0f;
            }

            if (videoScreenRoot != null)
            {
                videoScreenRoot.gameObject.SetActive(false);
            }

            ReleaseVideoRenderTexture();
        }

        private void ReleaseVideoRenderTexture()
        {
            if (activeVideoRenderTexture != null)
            {
                if (RenderTexture.active == activeVideoRenderTexture)
                {
                    RenderTexture.active = null;
                }
                activeVideoRenderTexture.Release();
                if (Application.isPlaying)
                {
                    Destroy(activeVideoRenderTexture);
                }
                else
                {
                    DestroyImmediate(activeVideoRenderTexture);
                }
                activeVideoRenderTexture = null;
            }
        }

        private IEnumerator ExecuteCameraPanAndGameplayHandoff()
        {
            // Ensure video screen and vintage film overlays are hidden so the sky-to-forest reveal is crystal clear
            CleanupVideoPlayback();
            if (projectorApertureRect != null) projectorApertureRect.gameObject.SetActive(false);
            if (vignetteOverlayImage != null) vignetteOverlayImage.gameObject.SetActive(false);
            if (filmScratchesOverlayImage != null) filmScratchesOverlayImage.gameObject.SetActive(false);
            if (slideImageGroup != null) slideImageGroup.alpha = 0f;
            if (subtitleGroup != null) subtitleGroup.alpha = 0f;
            if (monologueGroup != null) monologueGroup.alpha = 0f;

            // Ensure audio is fully transitioned to wind & forest nature
            if (projectorAudioSource != null && projectorAudioSource.isPlaying)
            {
                StartCoroutine(FadeAudioSourceVolumeRoutine(projectorAudioSource, projectorAudioSource.volume, 0f, 1.2f, true));
            }
            if (ambientAudioSource != null)
            {
                if (!ambientAudioSource.isPlaying) ambientAudioSource.Play();
                StartCoroutine(FadeAudioSourceVolumeRoutine(ambientAudioSource, ambientAudioSource.volume, targetAmbientVolume, 2.0f, false));
            }

            // Place Minh/An in forest sitting/playing posture while camera is up in the sky
            Animator anAnim = playerController != null ? playerController.GetComponentInChildren<Animator>() : null;
            if (anAnim != null)
            {
                int crouchHash = Animator.StringToHash("CrouchIdle");
                int idleHash = Animator.StringToHash("Idle");
                if (anAnim.HasState(0, crouchHash))
                {
                    anAnim.Play(crouchHash, 0, 0f);
                }
                else if (anAnim.HasState(0, idleHash))
                {
                    anAnim.Play(idleHash, 0, 0f);
                }
            }

            Vector3 skyZenithPos = skyZenithPosition;
            if (vcamIntroSky != null && vcamIntroSky.transform.position.y > 50f)
            {
                skyZenithPos = vcamIntroSky.transform.position;
            }
            Vector3 forestGroundPos = forestGroundPosition;
            float targetGameplayFov = 20f;
            if (vcamGameplay != null)
            {
                targetGameplayFov = vcamGameplay.Lens.FieldOfView;
                var pc = vcamGameplay.GetComponent<CinemachinePositionComposer>();
                if (pc != null && vcamGameplay.Follow != null)
                {
                    Vector3 targetPos = vcamGameplay.Follow.position + pc.TargetOffset;
                    forestGroundPos = targetPos - vcamGameplay.transform.forward * pc.CameraDistance;
                }
                else
                {
                    forestGroundPos = vcamGameplay.transform.position;
                }
            }
            float initialFov = vcamIntroSky != null ? vcamIntroSky.Lens.FieldOfView : 38f;

            // 1. Position VCam_IntroSky high in the deep blue cosmic zenith sky as the ACTIVE camera
            if (vcamIntroSky != null)
            {
                vcamIntroSky.transform.position = skyZenithPos;
                vcamIntroSky.transform.rotation = Quaternion.identity;
                vcamIntroSky.gameObject.SetActive(true);
                vcamIntroSky.enabled = true;
                vcamIntroSky.Priority = 35; // Guarantee live camera!
            }
            if (vcamGameplay != null)
            {
                vcamGameplay.Priority = 10;
            }

            // 2. Smoothly fade out the black curtain to reveal the deep blue cosmic sky
            float curtainDur = Mathf.Max(0.25f, blackCurtainFadeOutDuration);
            float elapsedCurtain = 0f;
            while (elapsedCurtain < curtainDur)
            {
                elapsedCurtain += Time.deltaTime;
                float alpha = Mathf.SmoothStep(1f, 0f, elapsedCurtain / curtainDur);
                if (cutsceneCanvasGroup != null) cutsceneCanvasGroup.alpha = alpha;
                if (blackCurtainImage != null) blackCurtainImage.color = new Color(0f, 0f, 0f, alpha);
                yield return null;
            }

            if (cutsceneCanvasGroup != null) cutsceneCanvasGroup.alpha = 0f;
            if (blackCurtainImage != null) blackCurtainImage.color = new Color(0f, 0f, 0f, 0f);

            // Brief pause at the cosmic zenith sky to admire the deep galaxy blue & high wisps
            yield return new WaitForSeconds(0.40f);

            // 3. Physical Camera Descent: Directly translate vcamIntroSky from Y=75m down to Y=0.10m
            isSkyDescending = true;
            isSkipTriggered = false; // Reset so player can hold Space during descent if they want to fast-forward
            currentSkipHoldTimer = 0f;

            float descentDuration = Mathf.Max(6.0f, cameraPanDuration);
            float panElapsed = 0f;
            bool stoodUp = false;
            GameObject cloudDescentGroup = GameObject.Find("IntroSky_CloudDescentGroup");

            while (panElapsed < descentDuration)
            {
                // Check if player is holding Space to fast-forward the descent
                bool isHoldingFF = false;
#if ENABLE_INPUT_SYSTEM
                if (Keyboard.current != null && (Keyboard.current.spaceKey.isPressed || Keyboard.current.escapeKey.isPressed)) isHoldingFF = true;
                if (!isHoldingFF && Gamepad.current != null && (Gamepad.current.buttonSouth.isPressed || Gamepad.current.startButton.isPressed)) isHoldingFF = true;
#else
                if (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.Escape)) isHoldingFF = true;
#endif

                float dt = Time.deltaTime * (isHoldingFF ? 3.0f : 1.0f);
                panElapsed += dt;
                float t = Mathf.Clamp01(panElapsed / descentDuration);
                float progress = descentProfileCurve != null && descentProfileCurve.length > 0
                    ? Mathf.Clamp01(descentProfileCurve.Evaluate(t))
                    : Mathf.SmoothStep(0f, 1f, t);

                // Directly drive VCam_IntroSky transform
                if (vcamIntroSky != null)
                {
                    vcamIntroSky.transform.position = Vector3.Lerp(skyZenithPos, forestGroundPos, progress);
                    vcamIntroSky.Lens.FieldOfView = Mathf.Lerp(initialFov, targetGameplayFov, progress);
                }

                // Drift the parallax cloud layers gently during descent
                if (cloudDescentGroup != null)
                {
                    int childCount = cloudDescentGroup.transform.childCount;
                    for (int c = 0; c < childCount; c++)
                    {
                        Transform tr = cloudDescentGroup.transform.GetChild(c);
                        float dir = (c % 2 == 0) ? 1f : -1f;
                        tr.localPosition += new Vector3(dir * 0.15f * Time.deltaTime, 0f, 0f);
                    }
                }

                // When camera is ~78% down into the forest clearing, Minh/An stands up from playing in the grass
                if (!stoodUp && t >= 0.78f)
                {
                    stoodUp = true;
                    if (anAnim != null)
                    {
                        anAnim.CrossFadeInFixedTime("Idle", 0.45f);
                    }
                }

                yield return null;
            }

            // Ensure camera is exactly at final ground position
            if (vcamIntroSky != null)
            {
                vcamIntroSky.transform.position = forestGroundPos;
                vcamIntroSky.Lens.FieldOfView = targetGameplayFov;
            }

            // 4. Hand off smoothly to Gameplay Camera
            if (vcamGameplay != null)
            {
                vcamGameplay.transform.position = forestGroundPos;
                vcamGameplay.Priority = 40;
            }
            if (vcamIntroSky != null)
            {
                vcamIntroSky.Priority = 0;
            }

            isSkyDescending = false;
            yield return new WaitForSeconds(0.15f);

            // 5. Complete transition and unlock Minh/An
            FinalizeGameplayUnlock();
        }

        private void FinalizeGameplayUnlock()
        {
            CleanupVideoPlayback();

            if (cutsceneCanvas != null)
            {
                cutsceneCanvas.gameObject.SetActive(false);
            }

            if (vcamGameplay != null)
            {
                vcamGameplay.Priority = 40;
            }
            if (vcamIntroSky != null)
            {
                vcamIntroSky.Priority = 0;
            }

            // Restore original blend definition
            if (cinemachineBrain != null)
            {
                cinemachineBrain.DefaultBlend = originalDefaultBlend;
            }

            // Unlock Minh/An
            if (playerController != null)
            {
                playerController.enabled = true;
            }

            isCutsceneRunning = false;
            IntroCutsceneState.CompleteIntro();

            Debug.Log("<color=#10b981>[IntroCutsceneController]</color> Seamless intro completed. Control handed off to player.");
        }

        // =====================================================================================
        // HOLD TO SKIP IMPLEMENTATION (PLANET OF LANA INVERTED SPACE KEYCAP)
        // =====================================================================================
        private void UpdateHoldToSkip()
        {
            if (!allowSkip) return;
            // Prevent skip during gameplay or after final unlock
            if (!isCutsceneRunning) return;

            bool isHolding = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                isHolding = Keyboard.current.spaceKey.isPressed || Keyboard.current.escapeKey.isPressed;
            }
            if (!isHolding && Gamepad.current != null)
            {
                isHolding = Gamepad.current.buttonSouth.isPressed || Gamepad.current.startButton.isPressed;
            }
#else
            isHolding = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.Escape);
#endif

            if (isHolding)
            {
                currentSkipHoldTimer += Time.deltaTime;
                float progress = Mathf.Clamp01(currentSkipHoldTimer / holdToSkipDuration);

                if (skipPromptGroup != null)
                {
                    skipPromptGroup.alpha = Mathf.MoveTowards(skipPromptGroup.alpha, 1.0f, Time.deltaTime * 5f);
                }
                if (skipFillMaskRect != null)
                {
                    skipFillMaskRect.sizeDelta = new Vector2(progress * 108f, 0f);
                }
                if (skipProgressBar != null)
                {
                    skipProgressBar.fillAmount = progress;
                }

                if (currentSkipHoldTimer >= holdToSkipDuration)
                {
                    TriggerSkip();
                }
            }
            else
            {
                currentSkipHoldTimer = Mathf.MoveTowards(currentSkipHoldTimer, 0f, Time.deltaTime * 3.0f);
                float progress = Mathf.Clamp01(currentSkipHoldTimer / holdToSkipDuration);

                if (skipFillMaskRect != null)
                {
                    skipFillMaskRect.sizeDelta = new Vector2(progress * 108f, 0f);
                }
                if (skipProgressBar != null)
                {
                    skipProgressBar.fillAmount = progress;
                }
                if (skipPromptGroup != null)
                {
                    // Rest at subtle affordance alpha (0.45f) so the player knows Space to Skip exists
                    skipPromptGroup.alpha = Mathf.MoveTowards(skipPromptGroup.alpha, 0.45f, Time.deltaTime * 2.5f);
                }
            }
        }

        public void TriggerSkip()
        {
            if (isSkipTriggered) return;

            isSkipTriggered = true;
            Debug.Log("<color=#fbbf24>[IntroCutsceneController]</color> Player skipped intro prologue. Beginning Sky Descent...");

            if (voiceoverAudioSource != null && voiceoverAudioSource.isPlaying)
            {
                voiceoverAudioSource.Stop();
            }

            CleanupVideoPlayback();

            if (activeCutsceneRoutine != null)
            {
                StopCoroutine(activeCutsceneRoutine);
                activeCutsceneRoutine = null;
            }

            if (isSkyDescending)
            {
                FinalizeGameplayUnlock();
            }
            else
            {
                activeCutsceneRoutine = StartCoroutine(SkipTransitionRoutine());
            }
        }

        private IEnumerator SkipTransitionRoutine()
        {
            if (skipPromptGroup != null) skipPromptGroup.alpha = 0f;
            if (slideImageGroup != null) slideImageGroup.alpha = 0f;
            if (subtitleGroup != null) subtitleGroup.alpha = 0f;
            if (monologueGroup != null) monologueGroup.alpha = 0f;

            // Flash warm shutter briefly (0.08s)
            yield return StartCoroutine(PlayShutterFlashRoutine());

            // Immediately start Phase 3 camera pan through the clouds to gameplay
            blackCurtainFadeOutDuration = 0.35f;
            cameraPanDuration = 11.5f;
            yield return StartCoroutine(ExecuteCameraPanAndGameplayHandoff());
        }

        // =====================================================================================
        // BYPASS CUTSCENE (CONTINUE GAMEPLAY DIRECTLY)
        // =====================================================================================
        public void BypassCutsceneAndPrepareGameplay()
        {
            isCutsceneRunning = false;
            CleanupVideoPlayback();

            if (cutsceneCanvas != null)
            {
                cutsceneCanvas.gameObject.SetActive(false);
            }

            if (vcamIntroSky != null)
            {
                vcamIntroSky.Priority = 0;
                vcamIntroSky.gameObject.SetActive(false);
            }

            if (vcamGameplay != null)
            {
                vcamGameplay.Priority = 20;
            }

            // Restore player position if save slot checkpoint is active
            int activeSlot = HoaVaMauSaveSystem.ActiveSlotIndex;
            var slotData = HoaVaMauSaveSystem.GetSlotData(activeSlot);
            if (slotData != null && slotData.isOccupied && slotData.checkpointIndex > 0)
            {
                Vector3 cpPos = new Vector3(slotData.playerPosX, slotData.playerPosY, slotData.playerPosZ);
                if (playerTransform != null)
                {
                    playerTransform.position = cpPos;
                }
                if (CheckpointManager.Instance != null)
                {
                    CheckpointManager.Instance.SetCheckpoint(cpPos, slotData.checkpointIndex);
                }
            }

            if (playerController != null)
            {
                playerController.enabled = true;
            }

            if (ambientAudioSource != null)
            {
                ambientAudioSource.volume = targetAmbientVolume;
                if (!ambientAudioSource.isPlaying) ambientAudioSource.Play();
            }

            IntroCutsceneState.CompleteIntro();
            Debug.Log("<color=#38bdf8>[IntroCutsceneController]</color> Cutscene bypassed. Standard gameplay resumed.");
        }

        // =====================================================================================
        // VISUAL EFFECTS: PERLIN JITTER, FILM GRAIN & SHUTTER FLASH
        // =====================================================================================
        private void UpdateFilmJitterAndGrain()
        {
            if (projectorApertureRect != null && projectorApertureRect.gameObject.activeInHierarchy)
            {
                // 24fps stepped projector cadence for authentic 1954 celluloid feel
                float steppedTime = Mathf.Floor(Time.time * 24f) / 24f;
                float jx = (Mathf.PerlinNoise(steppedTime * 18.0f, 0.3f) - 0.5f) * filmJitterAmount * 1.35f;
                float jy = (Mathf.PerlinNoise(0.7f, steppedTime * 21.0f) - 0.5f) * filmJitterAmount * 1.65f;
                projectorApertureRect.anchoredPosition = baseAperturePos + new Vector2(jx, jy);

                // Pure crisp original art presentation without muddy yellow tinting
                if (slideImage != null)
                {
                    slideImage.color = (slideImage.sprite != null) ? Color.white : new Color(0f, 0f, 0f, 0f);
                }
            }

            if (filmScratchesOverlayImage != null)
            {
                filmScratchesOverlayImage.color = new Color(1f, 1f, 1f, 0f);
            }
        }

        private IEnumerator PlayShutterFlashRoutine()
        {
            if (slideAdvanceSfxClip != null && projectorAudioSource != null)
            {
                projectorAudioSource.PlayOneShot(slideAdvanceSfxClip, 0.75f);
            }

            if (shutterFlashGroup != null)
            {
                shutterFlashGroup.alpha = 0.75f;
                float t = 0f;
                while (t < shutterFlashDuration)
                {
                    t += Time.deltaTime;
                    shutterFlashGroup.alpha = Mathf.Lerp(0.75f, 0f, t / shutterFlashDuration);
                    yield return null;
                }
                shutterFlashGroup.alpha = 0f;
            }
        }

        private IEnumerator FadeCanvasGroupRoutine(CanvasGroup group, float startAlpha, float endAlpha, float duration)
        {
            if (group == null) yield break;
            float elapsed = 0f;
            group.alpha = startAlpha;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                group.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
                yield return null;
            }
            group.alpha = endAlpha;
        }

        private IEnumerator CrossfadeAudioRoutine(float duration)
        {
            float elapsed = 0f;
            float startProjVol = projectorAudioSource != null ? projectorAudioSource.volume : 0f;
            float startAmbVol = ambientAudioSource != null ? ambientAudioSource.volume : 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float factor = Mathf.Clamp01(elapsed / duration);

                if (projectorAudioSource != null)
                {
                    projectorAudioSource.volume = Mathf.Lerp(startProjVol, 0f, factor);
                }
                if (ambientAudioSource != null)
                {
                    ambientAudioSource.volume = Mathf.Lerp(startAmbVol, targetAmbientVolume, factor);
                }
                yield return null;
            }

            if (projectorAudioSource != null) projectorAudioSource.volume = 0f;
            if (ambientAudioSource != null) ambientAudioSource.volume = targetAmbientVolume;
        }

        private IEnumerator FadeAudioSourceVolumeRoutine(AudioSource src, float from, float to, float duration, bool stopOnZero)
        {
            if (src == null) yield break;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                src.volume = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            src.volume = to;
            if (stopOnZero && to <= 0.001f)
            {
                src.Stop();
            }
        }

        // =====================================================================================
        // PROCEDURAL CANVAS GENERATION & ASSET BINDING
        // =====================================================================================
        public void EnsureCutsceneCanvasExists()
        {
            Transform existingChildCanvas = transform.Find("00_UI_CutsceneCanvas");
            GameObject existingCanvas = existingChildCanvas != null ? existingChildCanvas.gameObject : GameObject.Find("00_UI_CutsceneCanvas");
            if (existingCanvas != null)
            {
                if (Application.isPlaying) Destroy(existingCanvas);
                else DestroyImmediate(existingCanvas);
                cutsceneCanvas = null;
            }

            var canvasGO = new GameObject("00_UI_CutsceneCanvas");
            canvasGO.transform.SetParent(transform, false);
            cutsceneCanvas = canvasGO.AddComponent<Canvas>();
            cutsceneCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cutsceneCanvas.sortingOrder = 999; // Topmost sorting order

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();
            cutsceneCanvasGroup = canvasGO.AddComponent<CanvasGroup>();

            // Load crisp Dynamic SDF font with full Vietnamese diacritics support
#if UNITY_EDITOR
            if (cutsceneFontAsset == null)
            {
                cutsceneFontAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/SegoeUI_SDF_Dynamic.asset");
            }
#endif
            if (cutsceneFontAsset == null)
            {
                cutsceneFontAsset = TMP_Settings.defaultFontAsset;
            }

            // 1. Deep Warm Cinema Black Curtain
            var curtainGO = CreateUIElement("BlackCurtain", canvasGO.transform);
            StretchFull(curtainGO.GetComponent<RectTransform>());
            blackCurtainImage = curtainGO.AddComponent<Image>();
            blackCurtainImage.color = new Color(0.02f, 0.018f, 0.016f, 1.0f);

            // 1b. Historical Documentary Video Screen (Native 1440x1080 4:3 FitInParent, Zero Crop)
            var vidContainerGO = CreateUIElement("VideoScreenContainer", canvasGO.transform);
            videoScreenRoot = vidContainerGO.GetComponent<RectTransform>();
            StretchFull(videoScreenRoot);
            videoCanvasGroup = vidContainerGO.AddComponent<CanvasGroup>();
            videoCanvasGroup.alpha = 0f;
            videoCanvasGroup.interactable = false;
            videoCanvasGroup.blocksRaycasts = false;

            var vidRawGO = CreateUIElement("HistoricalVideoRawImage", vidContainerGO.transform);
            var vidRawRect = vidRawGO.GetComponent<RectTransform>();
            StretchFull(vidRawRect);
            videoRawImage = vidRawGO.AddComponent<RawImage>();
            videoRawImage.color = Color.white;
            videoRawImage.raycastTarget = false;
            videoAspectFitter = vidRawGO.AddComponent<AspectRatioFitter>();
            videoAspectFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            videoAspectFitter.aspectRatio = 1440f / 1080f;

            // 2. Projector Aperture Root (35mm Archival Gate - Legacy Fallback)
            var apertureGO = CreateUIElement("ProjectorAperture", canvasGO.transform);
            projectorApertureRect = apertureGO.GetComponent<RectTransform>();
            projectorApertureRect.anchorMin = new Vector2(0.14f, 0.23f);
            projectorApertureRect.anchorMax = new Vector2(0.86f, 0.90f);
            projectorApertureRect.pivot = new Vector2(0.5f, 0.5f);
            projectorApertureRect.offsetMin = Vector2.zero;
            projectorApertureRect.offsetMax = Vector2.zero;
            baseAperturePos = projectorApertureRect.anchoredPosition;

            // 2a. Warm Tungsten Projector Backing Matte
            var gateBgGO = CreateUIElement("ProjectorGateMatte", apertureGO.transform);
            var gateBgRect = gateBgGO.GetComponent<RectTransform>();
            gateBgRect.anchorMin = new Vector2(-0.045f, -0.04f);
            gateBgRect.anchorMax = new Vector2(1.045f, 1.06f);
            gateBgRect.offsetMin = Vector2.zero;
            gateBgRect.offsetMax = Vector2.zero;
            var gateBgImg = gateBgGO.AddComponent<Image>();
            gateBgImg.color = new Color(0.08f, 0.06f, 0.045f, 0.96f);
            gateBgImg.raycastTarget = false;

            // 2b. Inner Screen Backing
            var screenBgGO = CreateUIElement("CelluloidScreenBacking", apertureGO.transform);
            StretchFull(screenBgGO.GetComponent<RectTransform>());
            var screenBgImg = screenBgGO.AddComponent<Image>();
            screenBgImg.color = new Color(0f, 0f, 0f, 0.45f);
            screenBgImg.raycastTarget = false;

            // 3. Slide Image (Pure Original Full Color)
            var slideGO = CreateUIElement("SlideImage", apertureGO.transform);
            var slideRect = slideGO.GetComponent<RectTransform>();
            slideRect.anchorMin = new Vector2(0.015f, 0.02f);
            slideRect.anchorMax = new Vector2(0.985f, 0.95f);
            slideRect.offsetMin = Vector2.zero;
            slideRect.offsetMax = Vector2.zero;
            slideImage = slideGO.AddComponent<Image>();
            slideImage.preserveAspect = true;
            Sprite initialSprite = (historySlides != null && historySlides.Length > 0) ? historySlides[0].image : null;
            slideImage.sprite = initialSprite;
            slideImage.color = (initialSprite != null) ? Color.white : new Color(0f, 0f, 0f, 0f);
            slideImageGroup = slideGO.AddComponent<CanvasGroup>();

            // 3b. Archival Header Label on Top of 35mm Gate (Crisp SDF TextMeshPro)
            var headerGO = CreateUIElement("ArchivalHeaderText", apertureGO.transform);
            var headerRect = headerGO.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0.02f, 0.955f);
            headerRect.anchorMax = new Vector2(0.98f, 1.035f);
            headerRect.offsetMin = Vector2.zero;
            headerRect.offsetMax = Vector2.zero;
            archivalHeaderText = headerGO.AddComponent<TextMeshProUGUI>();
            if (cutsceneFontAsset != null) archivalHeaderText.font = cutsceneFontAsset;
            archivalHeaderText.fontSize = 15;
            archivalHeaderText.characterSpacing = 2.0f;
            archivalHeaderText.alignment = TextAlignmentOptions.Center;
            archivalHeaderText.color = new Color(0.90f, 0.82f, 0.65f, 0.85f);
            archivalHeaderText.text = "TƯ LIỆU LỊCH SỬ  •  SÔNG ĐỐC, CÀ MAU 1954  •  CUỘN PHIM 35MM";
            archivalHeaderText.raycastTarget = false;

            // 3c. Authentic 35mm Sprocket Hole Strips (Left & Right)
            void BuildSprocketStrip(string stripName, float anchorX)
            {
                var stripGO = CreateUIElement(stripName, apertureGO.transform);
                var stripRect = stripGO.GetComponent<RectTransform>();
                stripRect.anchorMin = new Vector2(anchorX - 0.014f, -0.02f);
                stripRect.anchorMax = new Vector2(anchorX + 0.014f, 1.02f);
                stripRect.offsetMin = Vector2.zero;
                stripRect.offsetMax = Vector2.zero;

                int holeCount = 10;
                for (int hIdx = 0; hIdx < holeCount; hIdx++)
                {
                    var holeGO = CreateUIElement($"Sprocket_{hIdx}", stripGO.transform);
                    var holeRect = holeGO.GetComponent<RectTransform>();
                    float yCenter = (hIdx + 0.5f) / holeCount;
                    holeRect.anchorMin = new Vector2(0.18f, yCenter - 0.024f);
                    holeRect.anchorMax = new Vector2(0.82f, yCenter + 0.024f);
                    holeRect.offsetMin = Vector2.zero;
                    holeRect.offsetMax = Vector2.zero;
                    var holeImg = holeGO.AddComponent<Image>();
                    holeImg.color = new Color(0.88f, 0.78f, 0.58f, 0.28f);
                    holeImg.raycastTarget = false;
                }
            }
            BuildSprocketStrip("SprocketStrip_Left", -0.022f);
            BuildSprocketStrip("SprocketStrip_Right", 1.022f);

            // 4. Vignette Overlay (Clear)
            var vigGO = CreateUIElement("VignetteOverlay", canvasGO.transform);
            StretchFull(vigGO.GetComponent<RectTransform>());
            vignetteOverlayImage = vigGO.AddComponent<Image>();
            vignetteOverlayImage.raycastTarget = false;
            vignetteOverlayImage.color = new Color(0f, 0f, 0f, 0f);

            // 5. Film Scratches Overlay (Disabled/Clear)
            var grainGO = CreateUIElement("FilmScratchesOverlay", canvasGO.transform);
            StretchFull(grainGO.GetComponent<RectTransform>());
            filmScratchesOverlayImage = grainGO.AddComponent<Image>();
            filmScratchesOverlayImage.raycastTarget = false;
            filmScratchesOverlayImage.color = new Color(0f, 0f, 0f, 0f);

            // 6. Subtitle Container (Bottom Archival Caption Bar)
            var subGO = CreateUIElement("SubtitleContainer", canvasGO.transform);
            var subRect = subGO.GetComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0.12f, 0.045f);
            subRect.anchorMax = new Vector2(0.88f, 0.205f);
            subRect.offsetMin = Vector2.zero;
            subRect.offsetMax = Vector2.zero;
            subtitleGroup = subGO.AddComponent<CanvasGroup>();
            subtitleGroup.alpha = 0f;

            var subBgImg = subGO.AddComponent<Image>();
            subBgImg.color = new Color(0.04f, 0.035f, 0.03f, 0.85f);
            subBgImg.raycastTarget = false;

            var subTextGO = CreateUIElement("SubtitleText", subGO.transform);
            var subTxtRect = subTextGO.GetComponent<RectTransform>();
            subTxtRect.anchorMin = new Vector2(0.04f, 0.06f);
            subTxtRect.anchorMax = new Vector2(0.96f, 0.94f);
            subTxtRect.offsetMin = Vector2.zero;
            subTxtRect.offsetMax = Vector2.zero;
            subtitleText = subTextGO.AddComponent<TextMeshProUGUI>();
            if (cutsceneFontAsset != null) subtitleText.font = cutsceneFontAsset;
            subtitleText.fontSize = 26;
            subtitleText.lineSpacing = 12f;
            subtitleText.alignment = TextAlignmentOptions.Center;
            subtitleText.color = new Color(1.00f, 0.96f, 0.88f, 0.98f);
            subtitleText.textWrappingMode = TextWrappingModes.Normal;
            subtitleText.overflowMode = TextOverflowModes.Truncate;

            // 7. Monologue Container (Center - Narrative Prologue)
            var monoGO = CreateUIElement("MonologueContainer", canvasGO.transform);
            var monoRect = monoGO.GetComponent<RectTransform>();
            monoRect.anchorMin = new Vector2(0.12f, 0.32f);
            monoRect.anchorMax = new Vector2(0.88f, 0.68f);
            monoRect.offsetMin = Vector2.zero;
            monoRect.offsetMax = Vector2.zero;
            monologueGroup = monoGO.AddComponent<CanvasGroup>();
            monologueGroup.alpha = 0f;

            var monoTextGO = CreateUIElement("MonologueText", monoGO.transform);
            StretchFull(monoTextGO.GetComponent<RectTransform>());
            monologueText = monoTextGO.AddComponent<TextMeshProUGUI>();
            if (cutsceneFontAsset != null) monologueText.font = cutsceneFontAsset;
            monologueText.fontSize = 34;
            monologueText.fontStyle = FontStyles.Italic;
            monologueText.lineSpacing = 16f;
            monologueText.alignment = TextAlignmentOptions.Center;
            monologueText.color = new Color(1.00f, 0.98f, 0.94f, 0.98f);
            monologueText.textWrappingMode = TextWrappingModes.Normal;

            // 8. Warm Amber Shutter Flash
            var flashGO = CreateUIElement("ShutterFlash", canvasGO.transform);
            StretchFull(flashGO.GetComponent<RectTransform>());
            var flashImg = flashGO.AddComponent<Image>();
            flashImg.color = new Color(1.0f, 0.92f, 0.72f, 0.82f);
            flashImg.raycastTarget = false;
            shutterFlashGroup = flashGO.AddComponent<CanvasGroup>();
            shutterFlashGroup.alpha = 0f;

            // 9. Authentic Planet of Lana Pure Diegetic Keycap Skip Button (Bottom Right)
            // Displays: [ SPACE ] only (clean keycap box with animated white fill & letter-by-letter black text inversion)
            var skipGO = CreateUIElement("SkipPromptRoot", canvasGO.transform);
            skipPromptRoot = skipGO.GetComponent<RectTransform>();
            skipPromptRoot.anchorMin = new Vector2(1f, 0f);
            skipPromptRoot.anchorMax = new Vector2(1f, 0f);
            skipPromptRoot.pivot = new Vector2(1f, 0f);
            skipPromptRoot.anchoredPosition = new Vector2(-48f, 36f);
            skipPromptRoot.sizeDelta = new Vector2(108f, 34f);
            skipPromptGroup = skipGO.AddComponent<CanvasGroup>();
            skipPromptGroup.alpha = 0.45f; // Subtle affordance hint when cutscene starts

            // Distinct Keycap Container [ SPACE ] with Dual-Layer Inversion Mask
            float keycapW = 108f;
            var keycapGO = CreateUIElement("Keycap_SPACE_Box", skipGO.transform);
            var keycapRect = keycapGO.GetComponent<RectTransform>();
            StretchFull(keycapRect); // Exactly matches skipPromptRoot (108 x 34)

            // Keycap Dark Translucent Backing
            var keycapBg = keycapGO.AddComponent<Image>();
            keycapBg.color = new Color(0.04f, 0.07f, 0.12f, 0.90f);
            keycapBg.raycastTarget = false;

            // Layer 1: Base White Text (shows through the dark backing when unmasked)
            var baseLabelGO = CreateUIElement("BaseText_SPACE", keycapGO.transform);
            StretchFull(baseLabelGO.GetComponent<RectTransform>());
            skipKeycapBaseText = baseLabelGO.AddComponent<TextMeshProUGUI>();
            if (cutsceneFontAsset != null) skipKeycapBaseText.font = cutsceneFontAsset;
            skipKeycapBaseText.fontSize = 14;
            skipKeycapBaseText.fontStyle = FontStyles.Bold;
            skipKeycapBaseText.characterSpacing = 2.5f;
            skipKeycapBaseText.alignment = TextAlignmentOptions.Center;
            skipKeycapBaseText.color = new Color(1.0f, 1.0f, 1.0f, 0.98f);
            skipKeycapBaseText.text = "SPACE";
            skipKeycapBaseText.raycastTarget = false;

            // Layer 2: Fill Mask Container (RectMask2D driven by hold progress)
            var fillMaskGO = CreateUIElement("FillMask", keycapGO.transform);
            skipFillMaskRect = fillMaskGO.GetComponent<RectTransform>();
            skipFillMaskRect.anchorMin = new Vector2(0f, 0f);
            skipFillMaskRect.anchorMax = new Vector2(0f, 1f);
            skipFillMaskRect.pivot = new Vector2(0f, 0.5f);
            skipFillMaskRect.anchoredPosition = Vector2.zero;
            skipFillMaskRect.sizeDelta = new Vector2(0f, 0f);
            fillMaskGO.AddComponent<RectMask2D>();

            // Layer 2a: Solid White Progress Fill
            var fillImgGO = CreateUIElement("FillWhiteBar", fillMaskGO.transform);
            var fillImgRect = fillImgGO.GetComponent<RectTransform>();
            fillImgRect.anchorMin = new Vector2(0f, 0f);
            fillImgRect.anchorMax = new Vector2(0f, 1f);
            fillImgRect.pivot = new Vector2(0f, 0.5f);
            fillImgRect.anchoredPosition = Vector2.zero;
            fillImgRect.sizeDelta = new Vector2(keycapW, 0f);
            var fillImg = fillImgGO.AddComponent<Image>();
            fillImg.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            fillImg.raycastTarget = false;

            // Layer 2b: Inverted Pitch Black Text (unmasked over the white fill at exact identical coordinates!)
            var invLabelGO = CreateUIElement("InvertedText_SPACE", fillMaskGO.transform);
            var invRect = invLabelGO.GetComponent<RectTransform>();
            invRect.anchorMin = new Vector2(0f, 0f);
            invRect.anchorMax = new Vector2(0f, 1f);
            invRect.pivot = new Vector2(0f, 0.5f);
            invRect.anchoredPosition = Vector2.zero;
            invRect.sizeDelta = new Vector2(keycapW, 0f);
            skipKeycapInvertedText = invLabelGO.AddComponent<TextMeshProUGUI>();
            if (cutsceneFontAsset != null) skipKeycapInvertedText.font = cutsceneFontAsset;
            skipKeycapInvertedText.fontSize = 14;
            skipKeycapInvertedText.fontStyle = FontStyles.Bold;
            skipKeycapInvertedText.characterSpacing = 2.5f;
            skipKeycapInvertedText.alignment = TextAlignmentOptions.Center;
            skipKeycapInvertedText.color = new Color(0.04f, 0.07f, 0.12f, 1.0f);
            skipKeycapInvertedText.text = "SPACE";
            skipKeycapInvertedText.raycastTarget = false;

            // Layer 3: Crisp Diegetic Border Edges (Created on top of fillMask so border outlines stay crisp!)
            void CreateKeyEdge(string eName, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
            {
                var edge = CreateUIElement(eName, keycapGO.transform);
                var ert = edge.GetComponent<RectTransform>();
                ert.anchorMin = aMin;
                ert.anchorMax = aMax;
                ert.offsetMin = oMin;
                ert.offsetMax = oMax;
                var eimg = edge.AddComponent<Image>();
                eimg.color = new Color(0.88f, 0.92f, 0.98f, 0.70f);
                eimg.raycastTarget = false;
            }
            CreateKeyEdge("EdgeT", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -1.5f), Vector2.zero);
            CreateKeyEdge("EdgeB", new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 1.5f));
            CreateKeyEdge("EdgeL", new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(1.5f, 0f));
            CreateKeyEdge("EdgeR", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-1.5f, 0f), Vector2.zero);
        }

        private GameObject CreateUIElement(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        private void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        public void AssignSprites(Sprite s1, Sprite s2, Sprite s3, Sprite s4, Sprite vignette, Sprite scratches)
        {
            if (historySlides == null || historySlides.Length < 4)
            {
                SetupDefaultSlideData();
            }
            if (historySlides.Length >= 4)
            {
                historySlides[0].image = s1;
                historySlides[1].image = s2;
                historySlides[2].image = s3;
                historySlides[3].image = s4;
            }

            if (vignetteOverlayImage != null && vignette != null)
            {
                vignetteOverlayImage.sprite = vignette;
            }
            if (filmScratchesOverlayImage != null && scratches != null)
            {
                filmScratchesOverlayImage.sprite = scratches;
            }
        }
    }
}
