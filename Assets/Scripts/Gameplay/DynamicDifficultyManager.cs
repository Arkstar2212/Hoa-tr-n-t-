using UnityEngine;
using System;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Core IT Innovation Module: Dynamic Difficulty Adjustment (DDA) Engine for "Hoa và Máu".
    /// Fulfills the IT & Innovation specifications outlined in the EXE101 Project Document.
    /// Tracks player failure telemetry, respawn frequency, and time-stuck per checkpoint.
    /// Automatically tunes enemy vision cones, detection rates, and patrol awareness in real-time
    /// to maintain the player's psychological Flow State and mitigate drop-off frustration.
    /// Press [F1] to toggle the live Telemetry & DDA HUD overlay for presentations and grading.
    /// </summary>
    [ExecuteAlways]
    public class DynamicDifficultyManager : MonoBehaviour
    {
        public static DynamicDifficultyManager Instance { get; private set; }

        [Header("DDA Tuning Thresholds")]
        [Tooltip("Number of failures at a single checkpoint before DDA begins soft intervention.")]
        [Range(1, 5)] [SerializeField] private int failureInterventionThreshold = 2;

        [Tooltip("Frustration threshold (0.0 to 1.0) above which enemy vision cone is dynamically reduced.")]
        [Range(0.2f, 0.8f)] [SerializeField] private float frustrationTrigger = 0.50f;

        [Header("Adaptive Output Multipliers")]
        [Tooltip("Minimum view distance multiplier under maximum frustration.")]
        [Range(0.60f, 0.95f)] [SerializeField] private float minViewDistanceMult = 0.80f; // -20% distance

        [Tooltip("Minimum view angle multiplier under maximum frustration.")]
        [Range(0.65f, 0.95f)] [SerializeField] private float minViewAngleMult = 0.82f;    // -18% cone angle

        [Header("Telemetry & Analytics State")]
        public int currentCheckpointIndex = 0;
        public int failuresAtCurrentCheckpoint = 0;
        public int totalGameFailures = 0;
        public float timeAtCurrentCheckpoint = 0f;
        public float frustrationIndex = 0f; // 0.0 (calm mastery) to 1.0 (severe frustration)

        [Header("Live Applied Modifiers")]
        public float ViewDistanceMultiplier { get; private set; } = 1.0f;
        public float ViewAngleMultiplier { get; private set; } = 1.0f;
        public float DetectionRateMultiplier { get; private set; } = 1.0f;
        public bool IsDDAActive => frustrationIndex >= frustrationTrigger;

        [Header("Telemetry HUD")]
        public bool showTelemetryHUD = true;

        public event Action<float, float> OnModifiersChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                if (Application.isPlaying) Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            Instance = this;
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            timeAtCurrentCheckpoint += Time.deltaTime;

            // F1 toggles Telemetry HUD
            bool f1Pressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame) f1Pressed = true;
#else
            if (Input.GetKeyDown(KeyCode.F1)) f1Pressed = true;
#endif
            if (f1Pressed)
            {
                showTelemetryHUD = !showTelemetryHUD;
            }

            EvaluateFrustration();
        }

        /// <summary>
        /// Real-time evaluation of player frustration using failure count and idle/stuck time.
        /// </summary>
        private void EvaluateFrustration()
        {
            // Base frustration rises with failures at the same checkpoint
            float failureScore = Mathf.Clamp01((float)failuresAtCurrentCheckpoint / Mathf.Max(1, failureInterventionThreshold * 2));
            
            // Stuck time score (if stuck at a checkpoint over 45s, frustration rises)
            float timeScore = Mathf.Clamp01((timeAtCurrentCheckpoint - 25f) / 60f);

            // Combined fuzzy index
            float rawFrustration = Mathf.Clamp01(failureScore * 0.75f + timeScore * 0.25f);
            frustrationIndex = Mathf.MoveTowards(frustrationIndex, rawFrustration, Time.deltaTime * 0.5f);

            // Calculate adaptive multipliers
            if (frustrationIndex >= frustrationTrigger)
            {
                float t = Mathf.InverseLerp(frustrationTrigger, 1.0f, frustrationIndex);
                ViewDistanceMultiplier = Mathf.Lerp(1.0f, minViewDistanceMult, t);
                ViewAngleMultiplier = Mathf.Lerp(1.0f, minViewAngleMult, t);
                DetectionRateMultiplier = Mathf.Lerp(1.0f, 0.85f, t);
            }
            else
            {
                ViewDistanceMultiplier = 1.0f;
                ViewAngleMultiplier = 1.0f;
                DetectionRateMultiplier = 1.0f;
            }

            OnModifiersChanged?.Invoke(ViewDistanceMultiplier, ViewAngleMultiplier);
        }

        /// <summary>
        /// Called by CheckpointManager whenever the player fails and respawns.
        /// </summary>
        public void RecordPlayerFailure(Vector3 checkpointPos)
        {
            failuresAtCurrentCheckpoint++;
            totalGameFailures++;

            Debug.Log($"<color=#40b8a6>[AI DDA Telemetry]</color> Failure recorded at Checkpoint {currentCheckpointIndex}. " +
                      $"Consecutive fails: {failuresAtCurrentCheckpoint}, Frustration: {frustrationIndex:F2}");

            EvaluateFrustration();
        }

        /// <summary>
        /// Called when the player successfully registers a new forward checkpoint.
        /// </summary>
        public void AdvanceCheckpoint(int newIndex)
        {
            if (newIndex > currentCheckpointIndex)
            {
                Debug.Log($"<color=#40b8a6>[AI DDA Telemetry]</color> Checkpoint advance {currentCheckpointIndex} -> {newIndex}. " +
                          $"Zone cleared in {timeAtCurrentCheckpoint:F1}s with {failuresAtCurrentCheckpoint} fails. Flow State restored!");

                currentCheckpointIndex = newIndex;
                failuresAtCurrentCheckpoint = 0;
                timeAtCurrentCheckpoint = 0f;
                frustrationIndex = Mathf.Max(0f, frustrationIndex - 0.45f);
                EvaluateFrustration();
            }
        }

        private void OnGUI()
        {
            if (!showTelemetryHUD || !Application.isPlaying) return;

            GUI.color = Color.white;
            GUI.backgroundColor = new Color(0.1f, 0.12f, 0.15f, 0.88f);

            Rect hudRect = new Rect(Screen.width - 320, 15, 305, 175);
            GUILayout.BeginArea(hudRect, GUI.skin.box);

            GUILayout.Label("<color=#40b8a6><b>[AI DDA Telemetry Hub]</b></color> <size=10>(EXE101)</size>");
            GUILayout.Label($"Checkpoint ID: <b>{currentCheckpointIndex}</b> | Fails here: <b>{failuresAtCurrentCheckpoint}</b> (Total: {totalGameFailures})");
            GUILayout.Label($"Zone Playtime: <b>{timeAtCurrentCheckpoint:F1}s</b>");

            string statusColor = IsDDAActive ? "#f5a623" : "#50e3c2";
            string statusText = IsDDAActive ? "ADAPTING (Softening AI)" : "OPTIMAL FLOW";
            GUILayout.Label($"Frustration Index: <b>{frustrationIndex * 100f:F0}%</b> (<color={statusColor}>{statusText}</color>)");

            GUILayout.Label($"Enemy View Range: <b>{ViewDistanceMultiplier * 100f:F0}%</b>");
            GUILayout.Label($"Enemy Vision Cone: <b>{ViewAngleMultiplier * 100f:F0}%</b>");

            GUILayout.Label("<size=10><i>Press [F1] to toggle Telemetry HUD | [R] Quick Reset</i></size>");
            GUILayout.EndArea();
        }
    }
}
