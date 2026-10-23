using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Production-grade Seamless Intro Cutscene Controller for "Hoa trên Đất Đỏ".
    /// Orchestrates the 3-phase cinematic pipeline into Scene 1 (New Scene 1_Overhaul):
    /// - Phase 1: Vintage Film Projector Reel presenting 4 authentic historical documentations with Ken Burns zoom,
    ///            Perlin frame jitter, Vignette, film grain/scratches, shutter flash, and contextual subtitles.
    /// - Phase 2: Narrative Monologue on deep quiet black screen with audio crossfade from projector to ambient river nature.
    /// - Phase 3: Seamless Camera Transition panning smoothly from sky/treetops down to An, followed by gameplay unlock.
    /// Features Hold to Skip (Space / Escape for 1.5s) and full Inspector customization.
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
        [Tooltip("Force play intro cutscene regardless of save/session state (useful for Editor testing)")]
        [SerializeField] private bool forcePlayIntro = false;
        [SerializeField] private bool autoFindReferencesOnStart = true;

        [Header("Giai Đoạn 1: Máy Chiếu Phim Cổ (Vintage Film Projector)")]
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
        [SerializeField] private float monologueFadeInDuration = 1.0f;
        [SerializeField] private float monologueHoldDuration = 3.0f;
        [SerializeField] private float monologueFadeOutDuration = 0.8f;
        [SerializeField] private float monologueAudioCrossfadeDuration = 3.0f;

        [Header("Giai Đoạn 3: Chuyển Cảnh Camera Mượt Mà (Seamless Camera Transition)")]
        [SerializeField] private CinemachineCamera vcamIntroSky;
        [SerializeField] private CinemachineCamera vcamGameplay;
        [SerializeField] private CinemachineBrain cinemachineBrain;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private float blackCurtainFadeOutDuration = 2.0f;
        [SerializeField] private float cameraPanDuration = 4.5f;

        [Header("Audio Sources & Crossfade")]
        [SerializeField] private AudioSource projectorAudioSource;
        [SerializeField] private AudioSource ambientAudioSource;
        [SerializeField, Range(0f, 1f)] private float maxProjectorVolume = 0.55f;
        [SerializeField, Range(0f, 1f)] private float targetAmbientVolume = 0.70f;

        [Header("Hold to Skip Settings")]
        [SerializeField] private bool allowSkip = true;
        [SerializeField] private float holdToSkipDuration = 1.5f;

        // UI Canvas Components
        [Header("UI Canvas Hierarchy (Auto-generated if missing)")]
        [SerializeField] private Canvas cutsceneCanvas;
        [SerializeField] private CanvasGroup cutsceneCanvasGroup;
        [SerializeField] private Image blackCurtainImage;
        [SerializeField] private RectTransform projectorApertureRect;
        [SerializeField] private Image slideImage;
        [SerializeField] private CanvasGroup slideImageGroup;
        [SerializeField] private Image vignetteOverlayImage;
        [SerializeField] private Image filmScratchesOverlayImage;
        [SerializeField] private CanvasGroup shutterFlashGroup;
        [SerializeField] private Text subtitleText;
        [SerializeField] private CanvasGroup subtitleGroup;
        [SerializeField] private Text monologueText;
        [SerializeField] private CanvasGroup monologueGroup;
        [SerializeField] private RectTransform skipPromptRoot;
        [SerializeField] private Image skipProgressBar;
        [SerializeField] private CanvasGroup skipPromptGroup;

        // Runtime State
        private Coroutine activeCutsceneRoutine;
        private bool isCutsceneRunning = false;
        private bool isSkipTriggered = false;
        private float currentSkipHoldTimer = 0f;
        private Vector2 baseAperturePos = Vector2.zero;
        private CinemachineBlendDefinition originalDefaultBlend;

        public bool IsCutsceneRunning => isCutsceneRunning;

        private void Awake()
        {
            if (autoFindReferencesOnStart)
            {
                ResolveSceneReferences();
            }
        }

        private void Start()
        {
            // Evaluate whether cutscene should play
            bool shouldPlay = forcePlayIntro || (IntroCutsceneState.ShouldPlayIntroCutscene && !IntroCutsceneState.HasViewedInSession);

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

            var allCameras = UnityEngine.Object.FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
            foreach (var cam in allCameras)
            {
                if (cam.name.Contains("IntroSky") || cam.name.Contains("Intro_Sky") || cam.name.Contains("VCam_IntroSky"))
                {
                    vcamIntroSky = cam;
                }
                else if (cam.name.Contains("2.5D") || cam.name.Contains("Gameplay") || cam.name.Contains("VCam_Gameplay"))
                {
                    vcamGameplay = cam;
                }
            }

            // Create AudioSources if unassigned
            if (projectorAudioSource == null)
            {
                var pGo = new GameObject("Audio_VintageProjectorHum");
                pGo.transform.SetParent(transform);
                projectorAudioSource = pGo.AddComponent<AudioSource>();
                projectorAudioSource.playOnAwake = false;
                projectorAudioSource.loop = true;
                projectorAudioSource.volume = 0f;
            }

            if (ambientAudioSource == null)
            {
                // Try finding ambient source in scene
                var audioSources = UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
                foreach (var src in audioSources)
                {
                    if (src.clip != null && src.clip.name.ToLower().Contains("wind"))
                    {
                        ambientAudioSource = src;
                        break;
                    }
                }
            }

            // Default Slides if empty
            if (historySlides == null || historySlides.Length == 0)
            {
                SetupDefaultSlideData();
            }

            EnsureCutsceneCanvasExists();
        }

        private void SetupDefaultSlideData()
        {
            historySlides = new HistorySlideData[4];
            historySlides[0] = new HistorySlideData(null, "Năm 1954 — Sau Hiệp định Genève, vùng đất Đỏ Cà Mau trở thành điểm tập kết 200 ngày lịch sử.", 5.0f);
            historySlides[1] = new HistorySlideData(null, "Hàng vạn đồng bào, chiến sĩ và những người con miền Nam tề tựu bên đôi bờ sông Đốc chuẩn bị cho chuyến đi xa.", 5.0f);
            historySlides[2] = new HistorySlideData(null, "Những ánh mắt lưu luyến, những cái ôm siết chặt và lời thề son sắt hẹn ngày non sông thống nhất.", 5.0f);
            historySlides[3] = new HistorySlideData(null, "Chuyến tàu chuyển quân rời bến trong nắng chiều, mang theo hành trang của cả một thế hệ kiên trung.", 5.5f);
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
                vcamIntroSky.Priority = 30; // Elevated priority to guarantee live camera
            }
            if (vcamGameplay != null)
            {
                vcamGameplay.Priority = 10;
            }

            // 3. Prepare Canvas
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
                blackCurtainImage.color = Color.black;
            }
            if (skipPromptGroup != null)
            {
                skipPromptGroup.alpha = 0f;
            }

            // 4. Start Audio & Pipeline Coroutine
            activeCutsceneRoutine = StartCoroutine(CutscenePipelineRoutine());
        }

        private IEnumerator CutscenePipelineRoutine()
        {
            // Start vintage projector hum
            if (projectorAudioSource != null)
            {
                if (projectorHumClip != null) projectorAudioSource.clip = projectorHumClip;
                projectorAudioSource.volume = maxProjectorVolume;
                projectorAudioSource.Play();
            }

            // Silence ambient initially
            if (ambientAudioSource != null)
            {
                ambientAudioSource.volume = 0f;
                if (!ambientAudioSource.isPlaying) ambientAudioSource.Play();
            }

            yield return new WaitForSeconds(0.6f);

            // =================================================================================
            // GIAI ĐOẠN 1: MÁY CHIẾU PHIM CỔ (VINTAGE REEL SLIDESHOW)
            // =================================================================================
            if (historySlides != null && historySlides.Length > 0)
            {
                for (int i = 0; i < historySlides.Length; i++)
                {
                    if (isSkipTriggered) break;

                    var slide = historySlides[i];

                    // Setup slide sprite
                    if (slideImage != null && slide.image != null)
                    {
                        slideImage.sprite = slide.image;
                        slideImage.preserveAspect = true;
                    }

                    // Setup subtitle text
                    if (subtitleText != null)
                    {
                        subtitleText.text = slide.subtitle;
                    }

                    // Shutter flash & advance sound
                    yield return StartCoroutine(PlayShutterFlashRoutine());

                    // Ken Burns Zoom + Subtitle display
                    float elapsed = 0f;
                    float duration = Mathf.Max(2.5f, slide.displayDuration);

                    // Fade in slide & subtitle smoothly
                    if (slideImageGroup != null) slideImageGroup.alpha = 1f;
                    if (subtitleGroup != null) StartCoroutine(FadeCanvasGroupRoutine(subtitleGroup, 0f, 1f, slideCrossfadeDuration));

                    while (elapsed < duration)
                    {
                        if (isSkipTriggered) break;

                        elapsed += Time.deltaTime;
                        float progress = Mathf.Clamp01(elapsed / duration);

                        // Ken Burns Zoom (1.0 -> slideKenBurnsEndScale)
                        if (projectorApertureRect != null)
                        {
                            float scale = Mathf.Lerp(1.0f, slideKenBurnsEndScale, progress);
                            projectorApertureRect.localScale = new Vector3(scale, scale, 1f);
                        }

                        // Fade subtitle out during last 0.5s of the slide
                        if (duration - elapsed <= slideCrossfadeDuration && subtitleGroup != null && subtitleGroup.alpha > 0.95f)
                        {
                            StartCoroutine(FadeCanvasGroupRoutine(subtitleGroup, 1f, 0f, slideCrossfadeDuration));
                        }

                        yield return null;
                    }

                    if (isSkipTriggered) break;

                    // Quick shutter blackout between slides
                    if (i < historySlides.Length - 1)
                    {
                        if (slideImageGroup != null) slideImageGroup.alpha = 0f;
                        yield return new WaitForSeconds(0.12f);
                    }
                }
            }

            // Turn off slide reel display
            if (slideImageGroup != null) slideImageGroup.alpha = 0f;
            if (subtitleGroup != null) subtitleGroup.alpha = 0f;
            if (projectorApertureRect != null) projectorApertureRect.localScale = Vector3.one;

            yield return new WaitForSeconds(0.4f);

            // =================================================================================
            // GIAI ĐOẠN 2: LỜI TỰA HÀNH TRÌNH, SỨ MỆNH & HY VỌNG (NARRATIVE MONOLOGUE)
            // =================================================================================
            if (!isSkipTriggered && narrativeLines != null && narrativeLines.Length > 0)
            {
                for (int lineIdx = 0; lineIdx < narrativeLines.Length; lineIdx++)
                {
                    if (isSkipTriggered) break;

                    string line = narrativeLines[lineIdx];
                    if (monologueText != null)
                    {
                        monologueText.text = line;
                    }

                    // If this is the last line, trigger audio crossfade simultaneously
                    if (lineIdx == narrativeLines.Length - 1)
                    {
                        StartCoroutine(CrossfadeAudioRoutine(monologueAudioCrossfadeDuration));
                    }

                    // Fade In Monologue line
                    if (monologueGroup != null)
                    {
                        yield return StartCoroutine(FadeCanvasGroupRoutine(monologueGroup, 0f, 1f, monologueFadeInDuration));
                    }

                    // Hold
                    float holdTimer = 0f;
                    while (holdTimer < monologueHoldDuration)
                    {
                        if (isSkipTriggered) break;
                        holdTimer += Time.deltaTime;
                        yield return null;
                    }

                    if (isSkipTriggered) break;

                    // Fade Out Monologue line
                    if (monologueGroup != null)
                    {
                        yield return StartCoroutine(FadeCanvasGroupRoutine(monologueGroup, 1f, 0f, monologueFadeOutDuration));
                    }

                    yield return new WaitForSeconds(0.35f);
                }
            }

            if (monologueGroup != null) monologueGroup.alpha = 0f;

            // =================================================================================
            // GIAI ĐOẠN 3: CAMERA PAN XUỐNG AN & BẮT ĐẦU GAMEPLAY (SEAMLESS TRANSITION)
            // =================================================================================
            yield return StartCoroutine(ExecuteCameraPanAndGameplayHandoff());
        }

        private IEnumerator ExecuteCameraPanAndGameplayHandoff()
        {
            // Ensure audio is fully transitioned
            if (projectorAudioSource != null && projectorAudioSource.isPlaying)
            {
                StartCoroutine(FadeAudioSourceVolumeRoutine(projectorAudioSource, projectorAudioSource.volume, 0f, 1.5f, true));
            }
            if (ambientAudioSource != null)
            {
                StartCoroutine(FadeAudioSourceVolumeRoutine(ambientAudioSource, ambientAudioSource.volume, targetAmbientVolume, 2.0f, false));
            }

            // 1. Configure Cinemachine Brain blend to smooth EaseInOut
            if (cinemachineBrain != null)
            {
                cinemachineBrain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, cameraPanDuration);
            }

            // 2. Drop Sky Camera priority to initiate Cinemachine pan to Gameplay camera
            if (vcamIntroSky != null)
            {
                vcamIntroSky.Priority = 0;
            }
            if (vcamGameplay != null)
            {
                vcamGameplay.Priority = 20;
            }

            // 3. Smoothly fade out black curtain over 2 seconds to reveal sky & treetops
            float elapsedCurtain = 0f;
            while (elapsedCurtain < blackCurtainFadeOutDuration)
            {
                elapsedCurtain += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsedCurtain / blackCurtainFadeOutDuration);
                if (cutsceneCanvasGroup != null) cutsceneCanvasGroup.alpha = alpha;
                if (blackCurtainImage != null) blackCurtainImage.color = new Color(0f, 0f, 0f, alpha);
                yield return null;
            }

            if (cutsceneCanvasGroup != null) cutsceneCanvasGroup.alpha = 0f;

            // 4. Wait for Cinemachine to finish landing on An (remaining camera pan duration)
            float remainingPanTime = Mathf.Max(0.2f, cameraPanDuration - blackCurtainFadeOutDuration);
            yield return new WaitForSeconds(remainingPanTime);

            // 5. Complete transition
            FinalizeGameplayUnlock();
        }

        private void FinalizeGameplayUnlock()
        {
            if (cutsceneCanvas != null)
            {
                cutsceneCanvas.gameObject.SetActive(false);
            }

            // Restore original blend definition
            if (cinemachineBrain != null)
            {
                cinemachineBrain.DefaultBlend = originalDefaultBlend;
            }

            // Unlock An
            if (playerController != null)
            {
                playerController.enabled = true;
            }

            isCutsceneRunning = false;
            IntroCutsceneState.CompleteIntro();

            Debug.Log("<color=#10b981>[IntroCutsceneController]</color> Seamless intro completed. Control handed off to An.");
        }

        // =====================================================================================
        // HOLD TO SKIP IMPLEMENTATION
        // =====================================================================================
        private void UpdateHoldToSkip()
        {
            if (!allowSkip || isSkipTriggered) return;

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
                if (skipPromptGroup != null)
                {
                    skipPromptGroup.alpha = Mathf.MoveTowards(skipPromptGroup.alpha, 1f, Time.deltaTime * 4f);
                }
                if (skipProgressBar != null)
                {
                    skipProgressBar.fillAmount = Mathf.Clamp01(currentSkipHoldTimer / holdToSkipDuration);
                }

                if (currentSkipHoldTimer >= holdToSkipDuration)
                {
                    TriggerSkip();
                }
            }
            else
            {
                currentSkipHoldTimer = Mathf.MoveTowards(currentSkipHoldTimer, 0f, Time.deltaTime * 2.5f);
                if (skipProgressBar != null)
                {
                    skipProgressBar.fillAmount = Mathf.Clamp01(currentSkipHoldTimer / holdToSkipDuration);
                }
                if (skipPromptGroup != null && currentSkipHoldTimer <= 0.05f)
                {
                    skipPromptGroup.alpha = Mathf.MoveTowards(skipPromptGroup.alpha, 0f, Time.deltaTime * 3f);
                }
            }
        }

        public void TriggerSkip()
        {
            if (isSkipTriggered) return;

            isSkipTriggered = true;
            Debug.Log("<color=#fbbf24>[IntroCutsceneController]</color> Player skipped intro cutscene.");

            if (activeCutsceneRoutine != null)
            {
                StopCoroutine(activeCutsceneRoutine);
            }

            StartCoroutine(SkipTransitionRoutine());
        }

        private IEnumerator SkipTransitionRoutine()
        {
            if (skipPromptGroup != null) skipPromptGroup.alpha = 0f;
            if (slideImageGroup != null) slideImageGroup.alpha = 0f;
            if (subtitleGroup != null) subtitleGroup.alpha = 0f;
            if (monologueGroup != null) monologueGroup.alpha = 0f;

            // Flash white shutter
            yield return StartCoroutine(PlayShutterFlashRoutine());

            // Jump immediately into phase 3 camera pan (with accelerated pan for responsive feel)
            cameraPanDuration = 1.5f;
            yield return StartCoroutine(ExecuteCameraPanAndGameplayHandoff());
        }

        // =====================================================================================
        // BYPASS CUTSCENE (CONTINUE GAMEPLAY DIRECTLY)
        // =====================================================================================
        public void BypassCutsceneAndPrepareGameplay()
        {
            isCutsceneRunning = false;

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

            // Restore An position if save slot checkpoint is active
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
            if (projectorApertureRect != null && filmJitterAmount > 0.01f)
            {
                float timeX = Time.time * 22.0f;
                float timeY = Time.time * 19.5f;
                float jx = (Mathf.PerlinNoise(timeX, 0f) - 0.5f) * filmJitterAmount;
                float jy = (Mathf.PerlinNoise(0f, timeY) - 0.5f) * filmJitterAmount;
                projectorApertureRect.anchoredPosition = baseAperturePos + new Vector2(jx, jy);
            }

            if (filmScratchesOverlayImage != null)
            {
                // Jitter scratch UV coordinates / position
                float grainAlpha = 0.15f + Mathf.Sin(Time.time * filmGrainPulseSpeed) * 0.08f;
                filmScratchesOverlayImage.color = new Color(1f, 1f, 1f, grainAlpha);
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
                shutterFlashGroup.alpha = 1f;
                float t = 0f;
                while (t < shutterFlashDuration)
                {
                    t += Time.deltaTime;
                    shutterFlashGroup.alpha = Mathf.Lerp(1f, 0f, t / shutterFlashDuration);
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
            if (cutsceneCanvas != null) return;

            var existingCanvas = GameObject.Find("00_UI_CutsceneCanvas");
            if (existingCanvas != null)
            {
                cutsceneCanvas = existingCanvas.GetComponent<Canvas>();
                cutsceneCanvasGroup = existingCanvas.GetComponent<CanvasGroup>();
                return;
            }

            var canvasGO = new GameObject("00_UI_CutsceneCanvas");
            canvasGO.transform.SetParent(transform);
            cutsceneCanvas = canvasGO.AddComponent<Canvas>();
            cutsceneCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cutsceneCanvas.sortingOrder = 999; // Topmost sorting order

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();
            cutsceneCanvasGroup = canvasGO.AddComponent<CanvasGroup>();

            // 1. Black Curtain
            var curtainGO = CreateUIElement("BlackCurtain", canvasGO.transform);
            StretchFull(curtainGO.GetComponent<RectTransform>());
            blackCurtainImage = curtainGO.AddComponent<Image>();
            blackCurtainImage.color = Color.black;

            // 2. Projector Aperture Root
            var apertureGO = CreateUIElement("ProjectorAperture", canvasGO.transform);
            projectorApertureRect = apertureGO.GetComponent<RectTransform>();
            projectorApertureRect.anchorMin = new Vector2(0.1f, 0.22f);
            projectorApertureRect.anchorMax = new Vector2(0.9f, 0.90f);
            projectorApertureRect.pivot = new Vector2(0.5f, 0.5f);
            projectorApertureRect.offsetMin = Vector2.zero;
            projectorApertureRect.offsetMax = Vector2.zero;
            baseAperturePos = projectorApertureRect.anchoredPosition;

            // 3. Slide Image
            var slideGO = CreateUIElement("SlideImage", apertureGO.transform);
            StretchFull(slideGO.GetComponent<RectTransform>());
            slideImage = slideGO.AddComponent<Image>();
            slideImage.preserveAspect = true;
            slideImageGroup = slideGO.AddComponent<CanvasGroup>();

            // 4. Vignette Overlay
            var vigGO = CreateUIElement("VignetteOverlay", canvasGO.transform);
            StretchFull(vigGO.GetComponent<RectTransform>());
            vignetteOverlayImage = vigGO.AddComponent<Image>();
            vignetteOverlayImage.raycastTarget = false;
            vignetteOverlayImage.color = new Color(1f, 1f, 1f, 0.85f);

            // 5. Film Scratches Overlay
            var grainGO = CreateUIElement("FilmScratchesOverlay", canvasGO.transform);
            StretchFull(grainGO.GetComponent<RectTransform>());
            filmScratchesOverlayImage = grainGO.AddComponent<Image>();
            filmScratchesOverlayImage.raycastTarget = false;
            filmScratchesOverlayImage.color = new Color(1f, 1f, 1f, 0.18f);

            // 6. Subtitle Container (Bottom)
            var subGO = CreateUIElement("SubtitleContainer", canvasGO.transform);
            var subRect = subGO.GetComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0.12f, 0.05f);
            subRect.anchorMax = new Vector2(0.88f, 0.20f);
            subRect.offsetMin = Vector2.zero;
            subRect.offsetMax = Vector2.zero;
            subtitleGroup = subGO.AddComponent<CanvasGroup>();

            var subTextGO = CreateUIElement("SubtitleText", subGO.transform);
            StretchFull(subTextGO.GetComponent<RectTransform>());
            subtitleText = subTextGO.AddComponent<Text>();
            subtitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            subtitleText.fontSize = 25;
            subtitleText.fontStyle = FontStyle.Normal;
            subtitleText.alignment = TextAnchor.MiddleCenter;
            subtitleText.color = new Color(0.96f, 0.94f, 0.88f, 0.95f);
            subtitleText.horizontalOverflow = HorizontalWrapMode.Wrap;
            subtitleText.verticalOverflow = VerticalWrapMode.Truncate;

            var subShadow = subTextGO.AddComponent<Shadow>();
            subShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            subShadow.effectDistance = new Vector2(2f, -2f);

            // 7. Monologue Container (Center)
            var monoGO = CreateUIElement("MonologueContainer", canvasGO.transform);
            var monoRect = monoGO.GetComponent<RectTransform>();
            monoRect.anchorMin = new Vector2(0.15f, 0.35f);
            monoRect.anchorMax = new Vector2(0.85f, 0.65f);
            monoRect.offsetMin = Vector2.zero;
            monoRect.offsetMax = Vector2.zero;
            monologueGroup = monoGO.AddComponent<CanvasGroup>();
            monologueGroup.alpha = 0f;

            var monoTextGO = CreateUIElement("MonologueText", monoGO.transform);
            StretchFull(monoTextGO.GetComponent<RectTransform>());
            monologueText = monoTextGO.AddComponent<Text>();
            monologueText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            monologueText.fontSize = 32;
            monologueText.fontStyle = FontStyle.Italic;
            monologueText.alignment = TextAnchor.MiddleCenter;
            monologueText.color = new Color(0.98f, 0.96f, 0.92f, 0.98f);
            monologueText.horizontalOverflow = HorizontalWrapMode.Wrap;

            var monoShadow = monoTextGO.AddComponent<Shadow>();
            monoShadow.effectColor = new Color(0f, 0f, 0f, 0.95f);
            monoShadow.effectDistance = new Vector2(2.5f, -2.5f);

            // 8. Shutter Flash
            var flashGO = CreateUIElement("ShutterFlash", canvasGO.transform);
            StretchFull(flashGO.GetComponent<RectTransform>());
            var flashImg = flashGO.AddComponent<Image>();
            flashImg.color = new Color(1f, 1f, 1f, 1f);
            flashImg.raycastTarget = false;
            shutterFlashGroup = flashGO.AddComponent<CanvasGroup>();
            shutterFlashGroup.alpha = 0f;

            // 9. Skip Prompt (Bottom Right)
            var skipGO = CreateUIElement("SkipPromptRoot", canvasGO.transform);
            skipPromptRoot = skipGO.GetComponent<RectTransform>();
            skipPromptRoot.anchorMin = new Vector2(1f, 0f);
            skipPromptRoot.anchorMax = new Vector2(1f, 0f);
            skipPromptRoot.pivot = new Vector2(1f, 0f);
            skipPromptRoot.anchoredPosition = new Vector2(-40f, 30f);
            skipPromptRoot.sizeDelta = new Vector2(280f, 36f);
            skipPromptGroup = skipGO.AddComponent<CanvasGroup>();
            skipPromptGroup.alpha = 0f;

            var skipTextGO = CreateUIElement("SkipLabel", skipGO.transform);
            var skipTextRect = skipTextGO.GetComponent<RectTransform>();
            skipTextRect.anchorMin = new Vector2(0f, 0f);
            skipTextRect.anchorMax = new Vector2(0.85f, 1f);
            skipTextRect.offsetMin = Vector2.zero;
            skipTextRect.offsetMax = Vector2.zero;
            var skipText = skipTextGO.AddComponent<Text>();
            skipText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            skipText.fontSize = 15;
            skipText.alignment = TextAnchor.MiddleRight;
            skipText.color = new Color(0.85f, 0.85f, 0.85f, 0.75f);
            skipText.text = "Giữ [Space] để bỏ qua";

            var skipBarBg = CreateUIElement("SkipBarBg", skipGO.transform);
            var barBgRect = skipBarBg.GetComponent<RectTransform>();
            barBgRect.anchorMin = new Vector2(0.88f, 0.25f);
            barBgRect.anchorMax = new Vector2(1.0f, 0.75f);
            barBgRect.offsetMin = Vector2.zero;
            barBgRect.offsetMax = Vector2.zero;
            var bgImg = skipBarBg.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.6f);

            var skipBarFill = CreateUIElement("SkipBarFill", skipBarBg.transform);
            StretchFull(skipBarFill.GetComponent<RectTransform>());
            skipProgressBar = skipBarFill.AddComponent<Image>();
            skipProgressBar.type = Image.Type.Filled;
            skipProgressBar.fillMethod = Image.FillMethod.Horizontal;
            skipProgressBar.fillAmount = 0f;
            skipProgressBar.color = new Color(0.98f, 0.85f, 0.55f, 0.95f);
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
