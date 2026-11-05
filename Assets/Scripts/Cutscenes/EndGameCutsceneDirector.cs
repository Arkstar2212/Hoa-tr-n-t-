using System;
using UnityEngine;
using TMPro;

namespace HoaVaMau.Cutscenes
{
    /// <summary>
    /// Director script for the End Game Cutscene ("To Be Continued").
    /// Controls:
    /// 1. 3D Ship motion & realistic hydrodynamic water bobbing.
    /// 2. Seamless camera transition: Stern close-up -> Side-scroller 2D view.
    /// 3. Smokestack particle system synchronization.
    /// 4. Cinematic letterboxing and "To Be Continued..." UI reveal.
    /// 5. Full in-editor scrubbing support via [ExecuteAlways] and scrubProgress slider.
    /// </summary>
    [ExecuteAlways]
    public class EndGameCutsceneDirector : MonoBehaviour
    {
        [Header("Cinematic Flow Settings")]
        [Tooltip("Total duration of the cutscene in seconds")]
        [SerializeField] private float totalDuration = 22f;

        [Tooltip("Normalized time (0 to 1) when camera finishes transition from stern to 2D side view")]
        [Range(0.1f, 0.6f)]
        [SerializeField] private float transitionEndNormalized = 0.30f;

        [Tooltip("Normalized time when camera locks its horizontal position so the boat sails past and off-screen")]
        [Range(0.2f, 0.7f)]
        [SerializeField] private float cameraHoldNormalized = 0.48f;

        [Tooltip("Normalized time when 'To Be Continued' UI begins fading in (after ship has fully exited)")]
        [Range(0.6f, 0.95f)]
        [SerializeField] private float uiRevealNormalized = 0.80f;

        [Header("Editor Preview")]
        [Range(0f, 1f)]
        [SerializeField] private float scrubProgress = 0f;
        [SerializeField] private bool autoPlayInEditMode = false;

        [Header("Ship References & Trajectory")]
        [SerializeField] private Transform shipTransform;
        [SerializeField] private ParticleSystem smokeParticleSystem;
        [SerializeField] private ParticleSystem bowWakeParticles;
        [SerializeField] private ParticleSystem sternWakeParticles;
        [SerializeField] private Vector3 shipStartPosition = new Vector3(-25f, 0f, 0f);
        [SerializeField] private Vector3 shipEndPosition = new Vector3(48f, 0f, 0f);

        [Header("Hydrodynamic Bobbing")]
        [SerializeField] private float bobbingSpeed = 1.0f;
        [SerializeField] private float bobbingAmplitude = 0.05f;
        [SerializeField] private float rollAngle = 0.8f;
        [SerializeField] private float pitchAngle = 0.5f;

        [Header("Camera Choreography")]
        [SerializeField] private Camera cutsceneCamera;

        [Tooltip("Stern View: Offset relative to ship (Close behind stern looking along the ship forward)")]
        [SerializeField] private Vector3 sternCameraOffset = new Vector3(-6.2f, 2.2f, 1.1f);
        [SerializeField] private Vector3 sternCameraEuler = new Vector3(6.5f, 75f, 0f);
        [SerializeField] private float sternFov = 45f;

        [Tooltip("Side 2D View: Offset relative to ship (Perpendicular side profile viewing Left-to-Right motion)")]
        [SerializeField] private Vector3 sideCameraOffset = new Vector3(0f, 1.8f, -17.0f);
        [SerializeField] private Vector3 sideCameraEuler = new Vector3(1.0f, 0f, 0f);
        [SerializeField] private float sideFov = 38f;

        [Tooltip("Fixed X position where camera rests while watching the boat sail off-screen")]
        [SerializeField] private float cameraRestX = 10f;

        [Header("Audio")]
        [SerializeField] private AudioSource shipHornSource;
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private float hornTriggerNormalized = 0.05f;
        private bool hornPlayed = false;

        [Header("UI References")]
        [SerializeField] private CanvasGroup uiCanvasGroup;
        [SerializeField] private RectTransform letterboxTop;
        [SerializeField] private RectTransform letterboxBottom;
        [SerializeField] private TextMeshProUGUI toBeContinuedText;
        [SerializeField] private TextMeshProUGUI subtitleText;
        [SerializeField] private TextMeshProUGUI restartHintText;

        // Runtime state
        private float currentTime = 0f;
        private bool isPlaying = false;

        public float Progress => scrubProgress;
        public bool IsPlaying => isPlaying;

        private void Start()
        {
            if (Application.isPlaying)
            {
                PlayCutscene();
            }
            else
            {
                EvaluateCutscene(scrubProgress);
            }
        }

        private void Update()
        {
            if (Application.isPlaying)
            {
                if (isPlaying)
                {
                    currentTime += Time.deltaTime;
                    scrubProgress = Mathf.Clamp01(currentTime / Mathf.Max(1f, totalDuration));
                    EvaluateCutscene(scrubProgress);

                    // Check ship horn trigger
                    if (!hornPlayed && scrubProgress >= hornTriggerNormalized && shipHornSource != null)
                    {
                        shipHornSource.Play();
                        hornPlayed = true;
                    }

                    if (currentTime >= totalDuration)
                    {
                        isPlaying = false;
                    }
                }

                // Press Space or R to replay cutscene in play mode
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.R))
                {
                    PlayCutscene();
                }
            }
            else
            {
                if (autoPlayInEditMode)
                {
                    scrubProgress = (scrubProgress + Time.deltaTime / totalDuration) % 1.0f;
                }
                EvaluateCutscene(scrubProgress);
            }
        }

        /// <summary>
        /// Starts playing the cutscene from the beginning.
        /// </summary>
        public void PlayCutscene()
        {
            currentTime = 0f;
            scrubProgress = 0f;
            isPlaying = true;
            hornPlayed = false;

            if (bgmSource != null && !bgmSource.isPlaying)
            {
                bgmSource.Play();
            }

            if (smokeParticleSystem != null && !smokeParticleSystem.isPlaying)
            {
                smokeParticleSystem.Play();
            }

            EvaluateCutscene(0f);
        }

        /// <summary>
        /// Evaluates all cutscene objects and camera transformations at normalized progress t in [0, 1].
        /// </summary>
        public void EvaluateCutscene(float t)
        {
            t = Mathf.Clamp01(t);

            // 1. SHIP POSITION & HYDRODYNAMIC BOBBING
            float simulatedTime = Application.isPlaying ? Time.time : (t * totalDuration);
            float bobY = Mathf.Sin(simulatedTime * bobbingSpeed) * bobbingAmplitude;
            float pitchX = Mathf.Sin(simulatedTime * bobbingSpeed * 0.8f) * pitchAngle;
            float rollZ = Mathf.Cos(simulatedTime * bobbingSpeed * 0.6f) * rollAngle;

            Vector3 baseShipPos = Vector3.Lerp(shipStartPosition, shipEndPosition, t);
            Vector3 finalShipPos = new Vector3(baseShipPos.x, baseShipPos.y + bobY, baseShipPos.z);

            if (shipTransform != null)
            {
                shipTransform.position = finalShipPos;
                // Base forward rotation is (0, 180, 0) so ship points towards +X (bow forward)
                shipTransform.rotation = Quaternion.Euler(pitchX, 180f, rollZ);
            }

            // 2. CAMERA CHOREOGRAPHY
            if (cutsceneCamera != null)
            {
                // Ease factor for camera transition (SmoothStep)
                float rawCamT = Mathf.Clamp01(t / Mathf.Max(0.01f, transitionEndNormalized));
                float camT = Mathf.SmoothStep(0f, 1f, rawCamT);

                // Spherical / arc offset interpolation from stern to side
                Vector3 currentOffset = Vector3.Lerp(sternCameraOffset, sideCameraOffset, camT);
                Quaternion sternRot = Quaternion.Euler(sternCameraEuler);
                Quaternion sideRot = Quaternion.Euler(sideCameraEuler);
                Quaternion currentRot = Quaternion.Slerp(sternRot, sideRot, camT);
                float currentFov = Mathf.Lerp(sternFov, sideFov, camT);

                // Horizontal tracking:
                // Camera initially tracks with the ship, then smoothly settles into cameraRestX,
                // so the ship sails past across the golden sun and exits off-screen to the right!
                float dynamicCamX = finalShipPos.x + currentOffset.x;
                float finalCamX = dynamicCamX;

                if (t > transitionEndNormalized)
                {
                    float holdT = Mathf.Clamp01((t - transitionEndNormalized) / Mathf.Max(0.01f, cameraHoldNormalized - transitionEndNormalized));
                    float easeHold = Mathf.SmoothStep(0f, 1f, holdT);
                    finalCamX = Mathf.Lerp(dynamicCamX, cameraRestX, easeHold);
                }

                cutsceneCamera.transform.position = new Vector3(finalCamX, finalShipPos.y + currentOffset.y, finalShipPos.z + currentOffset.z);
                cutsceneCamera.transform.rotation = currentRot;
                cutsceneCamera.fieldOfView = currentFov;
            }

            // 3. SMOKE & WAKE PARTICLE EMISSION
            if (smokeParticleSystem != null)
            {
                var emission = smokeParticleSystem.emission;
                emission.enabled = true;
            }
            if (sternWakeParticles != null)
            {
                var emission = sternWakeParticles.emission;
                emission.enabled = true;
            }

            // 4. "TO BE CONTINUED" UI FADE & LETTERBOXING
            if (uiCanvasGroup != null)
            {
                float uiT = Mathf.Clamp01((t - uiRevealNormalized) / Mathf.Max(0.01f, 1f - uiRevealNormalized));
                float alpha = Mathf.SmoothStep(0f, 1f, uiT);
                uiCanvasGroup.alpha = alpha;

                // Subtle slow vertical breathing for "TO BE CONTINUED" text
                if (toBeContinuedText != null)
                {
                    float textScale = Mathf.Lerp(0.92f, 1.0f, alpha);
                    toBeContinuedText.transform.localScale = Vector3.one * textScale;
                }

                // Letterbox height expansion
                if (Application.isPlaying)
                {
                    float letterboxHeight = Mathf.Lerp(45f, 75f, alpha);
                    if (letterboxTop != null)
                    {
                        letterboxTop.sizeDelta = new Vector2(letterboxTop.sizeDelta.x, letterboxHeight);
                    }
                    if (letterboxBottom != null)
                    {
                        letterboxBottom.sizeDelta = new Vector2(letterboxBottom.sizeDelta.x, letterboxHeight);
                    }
                }
            }
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this != null)
                    {
                        EvaluateCutscene(scrubProgress);
                    }
                };
            }
            else
            {
                EvaluateCutscene(scrubProgress);
            }
#else
            EvaluateCutscene(scrubProgress);
#endif
        }
    }
}
