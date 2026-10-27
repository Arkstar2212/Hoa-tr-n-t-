using System.Collections;
using UnityEngine;

namespace HoaVaMau.Environment
{
    /// <summary>
    /// Animates a distant seagull silhouette in the sky with natural flap-and-glide cycles,
    /// vertical bobbing on downstrokes, wind tilt, and horizon traversal.
    /// Based on authentic hand-drawn ink wing sketches.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    public class DistantSeagull : MonoBehaviour
    {
        [Header("Sprite Animation Frames")]
        [Tooltip("The 6 flight cycle frames (0: High V, 1: Down sweep, 2: Open V glide, 3: Mid down, 4: Downstroke peak, 5: Recovery)")]
        [SerializeField] private Sprite[] flightFrames;
        [Tooltip("Frame index to hold during gliding (typically Frame 2: Open V)")]
        [SerializeField] private int glideFrameIndex = 2;
        [Tooltip("Animation playback speed in frames per second")]
        [SerializeField] private float flapFps = 10f;

        [Header("Flight Rhythm (Flap vs. Glide)")]
        [Tooltip("Minimum number of consecutive flaps before gliding")]
        [SerializeField] private int minFlapCount = 3;
        [Tooltip("Maximum number of consecutive flaps before gliding")]
        [SerializeField] private int maxFlapCount = 6;
        [Tooltip("Minimum duration in seconds to glide with wings open")]
        [SerializeField] private float minGlideDuration = 1.8f;
        [Tooltip("Maximum duration in seconds to glide with wings open")]
        [SerializeField] private float maxGlideDuration = 3.5f;

        [Header("Movement & Horizon Drift")]
        [Tooltip("Horizontal forward flight speed in world units per second")]
        [SerializeField] private float flightSpeed = 1.5f;
        [Tooltip("Direction multiplier (1 = left-to-right, -1 = right-to-left)")]
        [SerializeField] private float flightDirection = 1f;
        [Tooltip("Vertical bobbing amplitude when flapping (lifts on downstroke)")]
        [SerializeField] private float flapBobAmplitude = 0.08f;
        [Tooltip("Gentle atmospheric wind drift wave amplitude")]
        [SerializeField] private float windWaveAmplitude = 0.15f;
        [Tooltip("Frequency of atmospheric wind wave")]
        [SerializeField] private float windWaveFrequency = 0.6f;

        [Header("Horizon Bounds (Wrapping)")]
        [Tooltip("Left-most boundary before wrapping")]
        [SerializeField] private float minBoundaryX = -25f;
        [Tooltip("Right-most boundary before wrapping")]
        [SerializeField] private float maxBoundaryX = 25f;
        [Tooltip("Random vertical variation when wrapping")]
        [SerializeField] private float verticalWrapVariation = 0.5f;

        [Header("Wind Tilt / Banking")]
        [Tooltip("Maximum tilt angle in degrees while soaring or turning")]
        [SerializeField] private float bankingAngle = 5f;

        private SpriteRenderer spriteRenderer;
        private float frameTimer;
        private int currentFrameIndex;
        private bool isGliding = false;
        private int flapsRemaining;
        private float glideTimer;
        private float baseHeightY;
        private float phaseOffset;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            baseHeightY = transform.localPosition.y;
            phaseOffset = Random.Range(0f, 100f);
            ResetToNewFlapBurst();
        }

        private void OnEnable()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            baseHeightY = transform.localPosition.y;
        }

        private void ResetToNewFlapBurst()
        {
            isGliding = false;
            flapsRemaining = Random.Range(minFlapCount, maxFlapCount + 1);
            currentFrameIndex = 0;
            frameTimer = 0f;
        }

        private void StartGlide()
        {
            isGliding = true;
            currentFrameIndex = Mathf.Clamp(glideFrameIndex, 0, flightFrames != null && flightFrames.Length > 0 ? flightFrames.Length - 1 : 0);
            glideTimer = Random.Range(minGlideDuration, maxGlideDuration);
            ApplyCurrentFrame();
        }

        private void Update()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (flightFrames == null || flightFrames.Length == 0) return;

            float dt = Application.isPlaying ? Time.deltaTime : 0.0166f;
            float time = (Application.isPlaying ? Time.time : (float)Time.realtimeSinceStartup) + phaseOffset;

            // 1. Advance Flight Rhythm & Animation
            UpdateAnimation(dt);

            // 2. Advance Spatial Movement
            UpdatePositionAndTilt(dt, time);
        }

        private void UpdateAnimation(float dt)
        {
            if (isGliding)
            {
                glideTimer -= dt;
                if (glideTimer <= 0f)
                {
                    ResetToNewFlapBurst();
                }
                else
                {
                    currentFrameIndex = Mathf.Clamp(glideFrameIndex, 0, flightFrames.Length - 1);
                    ApplyCurrentFrame();
                }
            }
            else
            {
                frameTimer += dt;
                float frameDuration = 1f / Mathf.Max(1f, flapFps);
                if (frameTimer >= frameDuration)
                {
                    frameTimer -= frameDuration;
                    currentFrameIndex++;

                    // Completed one full flap cycle
                    if (currentFrameIndex >= flightFrames.Length)
                    {
                        currentFrameIndex = 0;
                        flapsRemaining--;
                        if (flapsRemaining <= 0)
                        {
                            StartGlide();
                            return;
                        }
                    }
                    ApplyCurrentFrame();
                }
            }
        }

        private void ApplyCurrentFrame()
        {
            if (spriteRenderer != null && flightFrames != null && currentFrameIndex >= 0 && currentFrameIndex < flightFrames.Length)
            {
                spriteRenderer.sprite = flightFrames[currentFrameIndex];
            }
        }

        private void UpdatePositionAndTilt(float dt, float time)
        {
            Vector3 pos = transform.localPosition;

            // Horizontal flight
            pos.x += flightDirection * flightSpeed * dt;

            // Wrap across horizon bounds
            if (flightDirection > 0f && pos.x > maxBoundaryX)
            {
                pos.x = minBoundaryX;
                baseHeightY += Random.Range(-verticalWrapVariation, verticalWrapVariation);
            }
            else if (flightDirection < 0f && pos.x < minBoundaryX)
            {
                pos.x = maxBoundaryX;
                baseHeightY += Random.Range(-verticalWrapVariation, verticalWrapVariation);
            }

            // Vertical bobbing: lifts during downstroke (frame 3-4) and drops during upstroke
            float flapLift = 0f;
            if (!isGliding)
            {
                // Frame 4 is downstroke peak (max upward impulse)
                float normalizedCycle = (float)currentFrameIndex / Mathf.Max(1, flightFrames.Length);
                flapLift = Mathf.Sin(normalizedCycle * Mathf.PI * 2f) * flapBobAmplitude;
            }

            // Wind wave
            float windDrift = Mathf.Sin(time * windWaveFrequency) * windWaveAmplitude;

            pos.y = baseHeightY + flapLift + windDrift;
            transform.localPosition = pos;

            // Banking / tilt into the wind
            float bankZ = Mathf.Sin(time * (windWaveFrequency * 0.7f)) * bankingAngle;
            if (flightDirection < 0f) bankZ = -bankZ;
            transform.localRotation = Quaternion.Euler(0f, flightDirection < 0f ? 180f : 0f, bankZ);
        }

        public void SetFlightFrames(Sprite[] frames, int glideIdx = 2)
        {
            flightFrames = frames;
            glideFrameIndex = glideIdx;
            ApplyCurrentFrame();
        }

        public void ConfigureDepth(float scale, Color tint, float speedMult)
        {
            transform.localScale = Vector3.one * scale;
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null) spriteRenderer.color = tint;
            flightSpeed *= speedMult;
        }
    }
}
