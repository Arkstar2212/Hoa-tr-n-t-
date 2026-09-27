using System.Collections.Generic;
using UnityEngine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Master Environmental Vegetation Interaction Hub for "Hoa và Máu".
    /// Coordinates player velocity, movement direction, trailing wake, damped spring recovery,
    /// and multi-layer depth response, feeding unified global shader vectors to URP grass shaders.
    /// </summary>
    [ExecuteAlways]
    public class GrassManager : MonoBehaviour
    {
        public static GrassManager Instance { get; private set; }

        [Header("Global Interaction Settings")]
        [Tooltip("Master horizontal interaction radius.")]
        [Range(0.5f, 3.0f)] public float interactionRadius = 1.45f;

        [Tooltip("Maximum bend displacement strength.")]
        [Range(0.2f, 2.0f)] public float bendStrength = 0.75f;

        [Tooltip("How strongly movement velocity directs grass bending (drag backwards).")]
        [Range(0.0f, 1.5f)] public float directionalDragWeight = 0.85f;

        [Tooltip("Clean Z-axis split so vegetation parts around the player.")]
        [Range(0.0f, 1.0f)] public float radialPartingWeight = 0.45f;

        [Tooltip("Downward flattening underfoot (stepped-on effect).")]
        [Range(0.0f, 1.0f)] public float steppedOnTrampleWeight = 0.85f;

        [Tooltip("Subtle resting displacement when player is standing still.")]
        [Range(0.0f, 0.5f)] public float idleDisplacement = 0.20f;

        [Header("Spring Damping & Recovery")]
        [Tooltip("Speed at which grass rebounds upright after player leaves.")]
        [Range(1.0f, 10.0f)] public float recoverySpeed = 4.8f;

        [Tooltip("Harmonic spring oscillation frequency during rebound.")]
        [Range(2.0f, 20.0f)] public float springFrequency = 10.5f;

        [Tooltip("Damping ratio for spring oscillation (higher = less oscillation, settles faster).")]
        [Range(1.0f, 12.0f)] public float springDamping = 5.2f;

        [Header("Multi-Layer Vegetation Depth Weights")]
        [Tooltip("Interaction weight for Layer 1: Back (silhouette layer).")]
        [Range(0.0f, 1.0f)] public float backLayerWeight = 0.12f;

        [Tooltip("Interaction weight for Layer 2: Middle (main gameplay layer).")]
        [Range(0.0f, 1.5f)] public float middleLayerWeight = 1.0f;

        [Tooltip("Interaction weight for Layer 3: Front (foreground framing layer).")]
        [Range(0.0f, 1.0f)] public float frontLayerWeight = 0.40f;

        [Header("Debug Mode")]
        public bool enableDebugMode = false;
        public bool showOnScreenStats = false;

        // Active interactor registry
        private static readonly List<GrassInteraction> ActiveInteractors = new List<GrassInteraction>(4);

        // Global Shader Property IDs
        private static readonly int PropPlayerPos = Shader.PropertyToID("_PlayerPosition");
        private static readonly int PropPlayerMovement = Shader.PropertyToID("_PlayerMovement");
        private static readonly int PropPlayerTrail0 = Shader.PropertyToID("_PlayerTrail0");
        private static readonly int PropPlayerTrail1 = Shader.PropertyToID("_PlayerTrail1");
        private static readonly int PropSpringParams = Shader.PropertyToID("_GrassSpringParams");
        private static readonly int PropInteractParams = Shader.PropertyToID("_GrassInteractionParams");
        private static readonly int PropLayerWeights = Shader.PropertyToID("_GrassLayerWeights");

        public static void RegisterInteractor(GrassInteraction interactor)
        {
            if (interactor != null && !ActiveInteractors.Contains(interactor))
            {
                ActiveInteractors.Add(interactor);
            }
        }

        public static void UnregisterInteractor(GrassInteraction interactor)
        {
            if (interactor != null)
            {
                ActiveInteractors.Remove(interactor);
            }
        }

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
            UpdateShaderGlobals();
        }

        private void UpdateShaderGlobals()
        {
            GrassInteraction primary = FindPrimaryInteractor();

            Vector4 playerPosVal = Vector4.zero;
            Vector4 movementVal = Vector4.zero;
            Vector4 trail0Val = new Vector4(0f, 0f, 0f, 1f);
            Vector4 trail1Val = new Vector4(0f, 0f, 0f, 1f);

            if (primary != null && primary.isActiveAndEnabled)
            {
                Vector3 wpos = primary.WorldPosition;
                float rad = primary.interactionRadius > 0.01f ? primary.interactionRadius : interactionRadius;
                playerPosVal = new Vector4(wpos.x, wpos.y, wpos.z, rad);

                Vector2 svel = primary.SmoothVelocity;
                float spd = primary.MovementSpeed;
                float dirFactor = primary.DirectionFactor;
                movementVal = new Vector4(svel.x, svel.y, spd, dirFactor);

                // Sample trail wake points
                var nodes = primary.TrailNodes;
                if (nodes.Count > 0)
                {
                    var n0 = nodes[nodes.Count - 1];
                    float ageNorm0 = Mathf.Clamp01(n0.age / primary.trailLifetime);
                    trail0Val = new Vector4(n0.position.x, n0.position.y, n0.position.z, ageNorm0);
                }
                if (nodes.Count > 1)
                {
                    var n1 = nodes[nodes.Count - 2];
                    float ageNorm1 = Mathf.Clamp01(n1.age / primary.trailLifetime);
                    trail1Val = new Vector4(n1.position.x, n1.position.y, n1.position.z, ageNorm1);
                }
            }
            // A zero radius intentionally disables character displacement until an active
            // interactor is available.  Falling back to Vector3.zero creates a visible,
            // phantom bend at the scene origin during startup and in edit mode.

            // Pack parameter vectors
            Vector4 springVal = new Vector4(bendStrength, recoverySpeed, springFrequency, springDamping);
            Vector4 interactVal = new Vector4(directionalDragWeight, radialPartingWeight, steppedOnTrampleWeight, idleDisplacement);
            Vector4 layerVal = new Vector4(backLayerWeight, middleLayerWeight, frontLayerWeight, 1.0f);

            // Send to global shader state
            Shader.SetGlobalVector(PropPlayerPos, playerPosVal);
            Shader.SetGlobalVector(PropPlayerMovement, movementVal);
            Shader.SetGlobalVector(PropPlayerTrail0, trail0Val);
            Shader.SetGlobalVector(PropPlayerTrail1, trail1Val);
            Shader.SetGlobalVector(PropSpringParams, springVal);
            Shader.SetGlobalVector(PropInteractParams, interactVal);
            Shader.SetGlobalVector(PropLayerWeights, layerVal);
        }

        private static GrassInteraction FindPrimaryInteractor()
        {
            GrassInteraction firstActive = null;

            for (int i = ActiveInteractors.Count - 1; i >= 0; i--)
            {
                GrassInteraction interactor = ActiveInteractors[i];
                if (interactor == null)
                {
                    ActiveInteractors.RemoveAt(i);
                    continue;
                }

                if (!interactor.isActiveAndEnabled) continue;
                if (firstActive == null) firstActive = interactor;

                // The protagonist is the authoritative interaction source.  Companions
                // remain valid fallbacks, but cannot steal the global grass shader state.
                if (interactor.GetComponent<PlayerController>() != null)
                {
                    return interactor;
                }
            }

            return firstActive;
        }

        private void OnGUI()
        {
            if (!enableDebugMode || !showOnScreenStats || !Application.isPlaying) return;

            GUI.color = Color.white;
            GUILayout.BeginArea(new Rect(15, 15, 290, 180), GUI.skin.box);
            GUILayout.Label("<color=#40b8a6><b>[GrassManager 3D Debug]</b></color>");

            GrassInteraction primary = (ActiveInteractors.Count > 0) ? ActiveInteractors[0] : null;
            if (primary != null)
            {
                string bendDir = "IDLE (Resting contact)";
                if (primary.SmoothVelocity.x > 0.2f) bendDir = "<color=#f5a623>DRAGGING LEFT</color> (Player -> Right)";
                else if (primary.SmoothVelocity.x < -0.2f) bendDir = "<color=#f5a623>DRAGGING RIGHT</color> (Player -> Left)";

                GUILayout.Label($"Player Pos: {primary.WorldPosition.x:F2}, {primary.WorldPosition.y:F2}");
                GUILayout.Label($"Velocity X: {primary.SmoothVelocity.x:F2} m/s");
                GUILayout.Label($"Speed: {primary.MovementSpeed:F2} m/s");
                GUILayout.Label($"Bend Direction: {bendDir}");
                GUILayout.Label($"Trail Nodes Active: {primary.TrailNodes.Count}");
            }
            else
            {
                GUILayout.Label("No active GrassInteraction component registered.");
            }

            GUILayout.Label($"Active Interactors: {ActiveInteractors.Count}");
            GUILayout.Label($"Radius: {interactionRadius:F1}m | Spring Freq: {springFrequency:F1}");
            GUILayout.EndArea();
        }

        private void OnDrawGizmos()
        {
            if (!enableDebugMode) return;

            if (ActiveInteractors.Count > 0)
            {
                foreach (var interactor in ActiveInteractors)
                {
                    if (interactor == null) continue;
                    Vector3 p = interactor.WorldPosition;
                    Gizmos.color = new Color(0.25f, 0.85f, 0.45f, 0.35f);
                    Gizmos.DrawWireCube(p + Vector3.up * 0.5f, new Vector3(interactor.interactionRadius * 2f, 1.2f, 0.8f));
                }
            }
        }
    }
}
