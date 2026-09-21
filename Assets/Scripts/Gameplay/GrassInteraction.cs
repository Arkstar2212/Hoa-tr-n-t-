using System.Collections.Generic;
using UnityEngine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Attached to the player (and any moving characters/companions/creatures).
    /// Tracks world position, smoothed velocity, directional movement drag, and a trail wake.
    /// Feeds dynamic interaction data to GrassManager for GPU vertex displacement.
    /// </summary>
    [DisallowMultipleComponent]
    public class GrassInteraction : MonoBehaviour
    {
        [Header("Interaction Settings")]
        [Tooltip("Horizontal radius within which grass blades detect this character.")]
        [Range(0.5f, 3.0f)] public float interactionRadius = 1.45f;

        [Tooltip("Direct underfoot contact radius where grass is trampled/flattened downward.")]
        [Range(0.2f, 1.2f)] public float footContactRadius = 0.55f;

        [Tooltip("Multiplier for how aggressively this character bends grass.")]
        [Range(0.1f, 2.5f)] public float pushStrength = 1.0f;

        [Tooltip("Smoothing time for horizontal velocity to prevent jitter.")]
        [Range(0.01f, 0.3f)] public float velocitySmoothTime = 0.08f;

        [Tooltip("Smoothing time when reversing movement direction (smooth wave rather than instant flip).")]
        [Range(0.05f, 0.4f)] public float directionTurnSmoothTime = 0.14f;

        [Header("Trail Wake Settings")]
        [Tooltip("How long the trailing wake behind the character lingers in seconds.")]
        [Range(0.2f, 2.0f)] public float trailLifetime = 0.85f;

        [Header("Debug Visualization")]
        [Tooltip("Toggle visual gizmos in the Scene view for interaction radius, direction, and trail.")]
        public bool showDebugGizmos = true;

        [System.Serializable]
        public struct TrailNode
        {
            public Vector3 position;
            public Vector2 velocity;
            public float age;
        }

        // Public state accessed by GrassManager
        public Vector3 WorldPosition => transform.position;
        public Vector2 SmoothVelocity => currentSmoothVel;
        public float MovementSpeed => currentSpeed;
        public float DirectionFactor => currentDirFactor; // -1 (left) to +1 (right)
        public IReadOnlyList<TrailNode> TrailNodes => trailNodes;

        // Internal velocity tracking
        private Vector3 lastPosition;
        private Vector2 rawVelocity;
        private Vector2 currentSmoothVel;
        private Vector2 velSmoothDeriv;
        private float currentSpeed;
        private float currentDirFactor = 0f;
        private float dirFactorDeriv = 0f;

        // Trail wake buffer (up to 4 historical points)
        private const int MaxTrailNodes = 4;
        private readonly List<TrailNode> trailNodes = new List<TrailNode>(MaxTrailNodes);
        private float trailSampleTimer = 0f;
        private const float TrailSampleInterval = 0.07f;

        private CharacterController charController;
        private PlayerController playerCtrl;

        private void Awake()
        {
            charController = GetComponent<CharacterController>();
            playerCtrl = GetComponent<PlayerController>();
            lastPosition = transform.position;
        }

        private void OnEnable()
        {
            GrassManager.RegisterInteractor(this);
            lastPosition = transform.position;
        }

        private void OnDisable()
        {
            GrassManager.UnregisterInteractor(this);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0.0001f) return;

            Vector3 currentPos = transform.position;

            // 1. Track velocity on the ground plane.  Vertical jump/fall speed must not
            // drive a horizontal grass push or leave a false trail behind the character.
            if (charController != null && charController.enabled)
            {
                rawVelocity = new Vector2(charController.velocity.x, charController.velocity.z);
            }
            else
            {
                rawVelocity = new Vector2((currentPos.x - lastPosition.x) / dt, (currentPos.z - lastPosition.z) / dt);
            }
            lastPosition = currentPos;

            // 2. Smooth velocity to prevent single-frame spikes
            currentSmoothVel = Vector2.SmoothDamp(currentSmoothVel, rawVelocity, ref velSmoothDeriv, velocitySmoothTime);
            currentSpeed = currentSmoothVel.magnitude;

            // 3. Smooth directional factor (-1 moving left, 0 idle, +1 moving right)
            float targetDir = 0f;
            if (Mathf.Abs(currentSmoothVel.x) > 0.15f)
            {
                targetDir = Mathf.Sign(currentSmoothVel.x);
            }
            else if (playerCtrl != null)
            {
                // In micro-movements, use facing direction with lower weight
                targetDir = playerCtrl.FacingDirection * 0.25f;
            }
            currentDirFactor = Mathf.SmoothDamp(currentDirFactor, targetDir, ref dirFactorDeriv, directionTurnSmoothTime);

            // 4. Update and age existing trail nodes
            for (int i = trailNodes.Count - 1; i >= 0; i--)
            {
                var node = trailNodes[i];
                node.age += dt;
                if (node.age > trailLifetime)
                {
                    trailNodes.RemoveAt(i);
                }
                else
                {
                    trailNodes[i] = node;
                }
            }

            // 5. Sample new trail node when moving
            trailSampleTimer += dt;
            if (trailSampleTimer >= TrailSampleInterval)
            {
                trailSampleTimer = 0f;
                if (currentSpeed > 0.35f)
                {
                    if (trailNodes.Count >= MaxTrailNodes)
                    {
                        trailNodes.RemoveAt(0);
                    }
                    trailNodes.Add(new TrailNode
                    {
                        position = currentPos,
                        velocity = currentSmoothVel,
                        age = 0f
                    });
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!showDebugGizmos) return;

            Vector3 pos = transform.position;

            // Interaction radius (Outer wave)
            Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.4f);
            Gizmos.DrawWireSphere(pos + Vector3.up * 0.5f, interactionRadius);

            // Foot contact radius (Trample zone)
            Gizmos.color = new Color(0.95f, 0.75f, 0.15f, 0.7f);
            Gizmos.DrawWireSphere(pos + Vector3.up * 0.15f, footContactRadius);

            // Velocity vector & drag direction
            if (Application.isPlaying && currentSpeed > 0.1f)
            {
                // Velocity arrow (direction player is moving)
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(pos + Vector3.up * 0.6f, new Vector3(currentSmoothVel.x, 0f, currentSmoothVel.y) * 0.4f);

                // Grass bend drag arrow (opposite direction!)
                Gizmos.color = Color.magenta;
                float dragDirX = -Mathf.Sign(currentSmoothVel.x);
                Gizmos.DrawRay(pos + Vector3.up * 0.3f, new Vector3(dragDirX * 0.8f, -0.2f, 0f));
            }

            // Trail wake nodes
            if (Application.isPlaying)
            {
                Gizmos.color = new Color(0.8f, 0.2f, 0.8f, 0.5f);
                foreach (var node in trailNodes)
                {
                    float alpha = 1f - (node.age / trailLifetime);
                    Gizmos.color = new Color(0.8f, 0.3f, 1f, alpha);
                    Gizmos.DrawWireSphere(node.position + Vector3.up * 0.2f, 0.25f * alpha);
                }
            }
        }
    }
}
