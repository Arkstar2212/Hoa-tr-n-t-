using System.Collections.Generic;
using UnityEngine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Production-grade 2.5D Verlet & Resonant Harmonic Pendulum Rope Simulator (Planet of Lana standard).
    /// Features:
    /// - Physical Pendulum Kinematics when player attaches: 100% stable, zero vibration, natural resonant pumping (A/D).
    /// - Momentum Transfer: Leaping onto the rope transfers player airborne velocity into authentic pendulum swing.
    /// - Low Natural Air Damping: Preserves satisfying swing momentum and gives high athletic arcs (up to 48 deg).
    /// - Procedural Verlet Tail: Lower rope segments below the player trail with inertia, mass and air resistance.
    /// - Natural Ambient Breeze: Soft harmonic idle sway when unoccupied.
    /// - Silky Smooth LineRenderer with natural hemp fiber gradient.
    /// - Reliable trigger zones along the grab segment.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class RopeController : MonoBehaviour
    {
        [System.Serializable]
        public struct RopeNode
        {
            public Vector3 position;
            public Vector3 prevPosition;
            public Vector3 acceleration;

            public RopeNode(Vector3 pos)
            {
                position = pos;
                prevPosition = pos;
                acceleration = Vector3.zero;
            }
        }

        [Header("Rope Dimensions")]
        [SerializeField] private int nodeCount = 16;
        [SerializeField] private float totalLength = 4.6f;
        [SerializeField] private float ropeVisualWidth = 0.055f;

        [Header("Physics & Motion Smoothing")]
        [SerializeField] private float gravity = 22.0f;
        [Range(0.90f, 0.999f)]
        [SerializeField] private float airDamping = 0.988f;
        [Range(4, 20)]
        [SerializeField] private int constraintIterations = 14;
        [SerializeField] private float maxSwingSpeed = 6.5f;
        [SerializeField] private float maxSwingAngle = 48.0f; // Max deflection angle (degrees)

        [Header("Ambient Breeze (Gentle Wiggle)")]
        [SerializeField] private bool enableAmbientWiggle = true;
        [SerializeField] private float windAmplitude = 0.35f;
        [SerializeField] private float windFrequency = 1.25f;

        [Header("Player Interaction (Planet of Lana Standard)")]
        [SerializeField] private float playerSwingForce = 26.0f; // Responsive harmonic swing pumping
        [SerializeField] private float climbWiggleForce = 0.25f;
        [SerializeField] private float momentumTransferRatio = 0.85f; // Transfer jump momentum into swing

        [Header("Visuals")]
        [SerializeField] private Color ropeColor = new Color(0.60f, 0.45f, 0.28f);
        [SerializeField] private Material ropeMaterial;

        // Simulation State
        private RopeNode[] nodes;
        private float segmentLength;
        private Vector3 anchorPosition;
        private LineRenderer lineRenderer;

        // Attached Player & Pendulum State
        private PlayerController attachedPlayer;
        private float playerClimbFraction = 0.65f; // 0 = anchor at top, 1 = tip at bottom
        private float currentAngle = 0f; // Radians from vertical (0 = straight down)
        private float angularVelocity = 0f; // Radians / sec
        private float currentSwingInput = 0f;
        private float smoothedSwingInput = 0f;

        // Triggers
        private List<GameObject> triggerColliders = new List<GameObject>();

        // Public properties
        public float CurrentAngleDegrees => currentAngle * Mathf.Rad2Deg;
        public float CurrentAngleRadians => currentAngle;
        public float AngularVelocity => angularVelocity;
        public float PlayerClimbFraction => playerClimbFraction;
        public float TotalLength => totalLength;

        private void Awake()
        {
            InitializeRope();
        }

        [ContextMenu("Rebuild Rope")]
        public void BuildRope()
        {
            InitializeRope();
        }

        public void InitializeRope()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                if (Application.isPlaying) Destroy(transform.GetChild(i).gameObject);
                else DestroyImmediate(transform.GetChild(i).gameObject);
            }
            triggerColliders.Clear();

            anchorPosition = transform.position;
            anchorPosition.z = 0f;

            segmentLength = totalLength / Mathf.Max(1, nodeCount - 1);

            nodes = new RopeNode[nodeCount];
            for (int i = 0; i < nodeCount; i++)
            {
                Vector3 nodePos = anchorPosition + Vector3.down * (i * segmentLength);
                nodes[i] = new RopeNode(nodePos);
            }

            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null) lineRenderer = gameObject.AddComponent<LineRenderer>();

            Shader ropeShader = Shader.Find("Universal Render Pipeline/Unlit")
                             ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                             ?? Shader.Find("Sprites/Default");

            if (ropeMaterial == null)
            {
#if UNITY_EDITOR
                ropeMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Environment/CaMau/M_Coir_Rope_Braided.mat");
#endif
                if (ropeMaterial == null)
                {
                    ropeMaterial = new Material(ropeShader) { name = "Mat_CoirRope" };
                    if (ropeMaterial.HasProperty("_BaseColor")) ropeMaterial.SetColor("_BaseColor", ropeColor);
                    if (ropeMaterial.HasProperty("_Color")) ropeMaterial.SetColor("_Color", ropeColor);
                }
            }

            lineRenderer.material = ropeMaterial;
            lineRenderer.sharedMaterial = ropeMaterial;
            lineRenderer.textureMode = LineTextureMode.Tile;
            lineRenderer.startWidth = ropeVisualWidth;
            lineRenderer.endWidth = ropeVisualWidth * 0.88f;
            lineRenderer.startColor = Color.white;
            lineRenderer.endColor = Color.white;
            lineRenderer.useWorldSpace = true;
            lineRenderer.sortingOrder = 25;
            lineRenderer.numCapVertices = 6;
            lineRenderer.numCornerVertices = 4;
            lineRenderer.positionCount = nodeCount;

            // Setup Branch Knot Sprite at top anchor
            Transform knotTr = transform.Find("BranchKnot");
            if (knotTr == null)
            {
                GameObject knotGO = new GameObject("BranchKnot");
                knotGO.transform.SetParent(transform);
                knotTr = knotGO.transform;
            }
            knotTr.localPosition = new Vector3(0f, 0.03f, -0.03f);
            knotTr.localScale = new Vector3(0.55f, 0.55f, 1f);
            var sr = knotTr.GetComponent<SpriteRenderer>();
            if (sr == null) sr = knotTr.gameObject.AddComponent<SpriteRenderer>();
#if UNITY_EDITOR
            if (sr.sprite == null)
            {
                sr.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Environment/Rope/T_Rope_Branch_Knot_Coil.png");
            }
#endif
            sr.sortingOrder = lineRenderer.sortingOrder + 1;

            // Trigger colliders starting from 20% down to 90% of length (playable zone)
            int triggerCount = 8;
            int startNode = Mathf.Max(1, Mathf.RoundToInt(nodeCount * 0.18f));
            int endNode = Mathf.Min(nodeCount - 1, Mathf.RoundToInt(nodeCount * 0.92f));
            float step = (float)(endNode - startNode) / Mathf.Max(1, triggerCount - 1);

            for (int t = 0; t < triggerCount; t++)
            {
                int nodeIdx = Mathf.Clamp(Mathf.RoundToInt(startNode + t * step), startNode, endNode);
                GameObject trigGO = new GameObject($"RopeTrigger_{t}");
                trigGO.transform.SetParent(transform);
                trigGO.transform.position = nodes[nodeIdx].position;

                Rigidbody rb = trigGO.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;

                SphereCollider sc = trigGO.AddComponent<SphereCollider>();
                sc.radius = 0.45f;
                sc.isTrigger = true;

                RopeSegmentTrigger rst = trigGO.AddComponent<RopeSegmentTrigger>();
                rst.Init(this, nodeIdx);

                triggerColliders.Add(trigGO);
            }

            UpdateVisuals();
        }

        private void FixedUpdate()
        {
            if (nodes == null || nodes.Length < 2) return;

            float dt = Time.fixedDeltaTime;
            anchorPosition = transform.position;
            anchorPosition.z = 0f;

            if (attachedPlayer != null)
            {
                // =========================================================================
                // 1. ACTIVE PLAYER: PHYSICAL RESONANT PENDULUM KINEMATICS (PLANET OF LANA)
                // =========================================================================
                SimulatePlayerPendulum(dt);
            }
            else
            {
                // =========================================================================
                // 2. UNOCCUPIED: VERLET POSITION-BASED SIMULATION WITH AMBIENT SWAY
                // =========================================================================
                SimulateFreeVerletRope(dt);
            }

            UpdateTriggerPositions();
        }

        private void SimulatePlayerPendulum(float dt)
        {
            float effLength = Mathf.Max(0.8f, totalLength * playerClimbFraction);
            float maxAngleRad = maxSwingAngle * Mathf.Deg2Rad;

            // Restoring torque from gravity: alpha_g = -(g / L) * sin(theta)
            float gravityTorque = -(gravity / effLength) * Mathf.Sin(currentAngle);

            // Natural settling damping: low damping when swinging freely (0.22f) for authentic inertia,
            // active counter-braking only when player deliberately holds opposite to swing direction.
            bool isBraking = Mathf.Abs(smoothedSwingInput) > 0.1f && (Mathf.Sign(smoothedSwingInput) != Mathf.Sign(angularVelocity)) && Mathf.Abs(angularVelocity) > 0.25f;
            float bodyDamping = isBraking ? 1.5f : (Mathf.Abs(smoothedSwingInput) > 0.05f ? 0.14f : 0.22f);

            // Smooth user swing input
            smoothedSwingInput = Mathf.MoveTowards(smoothedSwingInput, currentSwingInput, dt * 5.0f);

            // Resonant pumping (holding with swing direction pumps energy, holding against acts as brake)
            float pumpTorque = 0f;
            if (Mathf.Abs(smoothedSwingInput) > 0.05f)
            {
                bool isPumpingWithMotion = (Mathf.Sign(smoothedSwingInput) == Mathf.Sign(angularVelocity)) || Mathf.Abs(angularVelocity) < 0.25f;
                if (isPumpingWithMotion)
                {
                    // Maximum pumping efficiency at low angles
                    float efficiency = 1.0f + 0.55f * Mathf.Max(0f, Mathf.Cos(currentAngle));
                    pumpTorque = smoothedSwingInput * (playerSwingForce / effLength) * efficiency;
                }
                else
                {
                    // Active counter-force brake when player pushes opposite to swing
                    pumpTorque = smoothedSwingInput * (playerSwingForce * 1.20f / effLength);
                }
            }

            // Angular acceleration
            float angularAcc = gravityTorque - (bodyDamping * angularVelocity) + pumpTorque;

            // Soft spring barrier at maximum swing angle clamp
            if (currentAngle > maxAngleRad)
            {
                angularAcc -= (currentAngle - maxAngleRad) * 60f;
                angularVelocity *= 0.95f;
            }
            else if (currentAngle < -maxAngleRad)
            {
                angularAcc -= (currentAngle - (-maxAngleRad)) * 60f;
                angularVelocity *= 0.95f;
            }

            // Semi-implicit Euler integration
            float maxAngularVel = maxSwingSpeed / effLength;
            angularVelocity += angularAcc * dt;
            angularVelocity = Mathf.Clamp(angularVelocity, -maxAngularVel, maxAngularVel);
            currentAngle += angularVelocity * dt;
            currentAngle = Mathf.Clamp(currentAngle, -maxAngleRad - 0.05f, maxAngleRad + 0.05f);

            // Decay input gradually if unheld
            currentSwingInput = Mathf.MoveTowards(currentSwingInput, 0f, dt * 3.5f);

            // Update rope nodes along the pendulum line & procedural tail
            int grabIdx = Mathf.Clamp(Mathf.RoundToInt(playerClimbFraction * (nodes.Length - 1)), 1, nodes.Length - 1);
            Vector3 grabPos = anchorPosition + new Vector3(Mathf.Sin(currentAngle), -Mathf.Cos(currentAngle), 0f) * effLength;

            // Nodes from anchor to player grab point form a taut pendulum cord with subtle catenary bow
            nodes[0].position = anchorPosition;
            nodes[0].prevPosition = anchorPosition;
            nodes[0].acceleration = Vector3.zero;

            for (int i = 1; i <= grabIdx; i++)
            {
                float t = (float)i / grabIdx;
                Vector3 straightPos = Vector3.Lerp(anchorPosition, grabPos, t);
                // Subtle dynamic sag flex along swing normal
                float sag = Mathf.Sin(t * Mathf.PI) * 0.04f * Mathf.Abs(angularVelocity);
                Vector3 normal = new Vector3(Mathf.Cos(currentAngle), Mathf.Sin(currentAngle), 0f);
                Vector3 finalPos = straightPos - normal * sag;
                finalPos.z = 0f;

                nodes[i].prevPosition = nodes[i].position;
                nodes[i].position = finalPos;
                nodes[i].acceleration = Vector3.zero;
            }

            // Nodes below the player (tail) dangle with secondary Verlet physics
            Vector3 gravityForce = Vector3.down * (gravity * 0.85f);
            for (int i = grabIdx + 1; i < nodes.Length; i++)
            {
                Vector3 currentPos = nodes[i].position;
                Vector3 prevPos = nodes[i].prevPosition;
                Vector3 vel = (currentPos - prevPos) * 0.96f;

                Vector3 nextPos = currentPos + vel + gravityForce * (dt * dt);
                nextPos.z = 0f;

                nodes[i].prevPosition = currentPos;
                nodes[i].position = nextPos;
            }

            // Distance relaxation on tail segments
            for (int iter = 0; iter < 6; iter++)
            {
                nodes[grabIdx].position = grabPos;
                for (int i = grabIdx; i < nodes.Length - 1; i++)
                {
                    Vector3 delta = nodes[i + 1].position - nodes[i].position;
                    float dist = delta.magnitude;
                    if (dist > 0.0001f)
                    {
                        float error = (dist - segmentLength) / dist;
                        Vector3 correction = delta * (error * 0.6f);
                        if (i == grabIdx)
                        {
                            nodes[i + 1].position -= correction;
                        }
                        else
                        {
                            nodes[i].position += correction * 0.5f;
                            nodes[i + 1].position -= correction * 0.5f;
                        }
                    }
                }
            }
        }

        private void SimulateFreeVerletRope(float dt)
        {
            nodes[0].position = anchorPosition;
            nodes[0].prevPosition = anchorPosition;
            nodes[0].acceleration = Vector3.zero;

            Vector3 gravityForce = Vector3.down * gravity;
            float windTime = Time.time * windFrequency;

            for (int i = 1; i < nodes.Length; i++)
            {
                nodes[i].acceleration += gravityForce;

                if (enableAmbientWiggle)
                {
                    float phase = i * 0.4f;
                    float wave = Mathf.Sin(windTime + phase);
                    float heightFactor = (float)i / (nodes.Length - 1);
                    float sway = wave * windAmplitude * heightFactor;
                    nodes[i].acceleration += new Vector3(sway, 0f, 0f);
                }
            }

            // Verlet integration
            for (int i = 1; i < nodes.Length; i++)
            {
                Vector3 currentPos = nodes[i].position;
                Vector3 prevPos = nodes[i].prevPosition;
                Vector3 velocity = (currentPos - prevPos) * airDamping;
                velocity = Vector3.ClampMagnitude(velocity, maxSwingSpeed * dt);

                Vector3 nextPos = currentPos + velocity + nodes[i].acceleration * (dt * dt);
                nextPos.z = 0f;

                nodes[i].prevPosition = currentPos;
                nodes[i].position = nextPos;
                nodes[i].acceleration = Vector3.zero;
            }

            // Distance constraint relaxation
            for (int iter = 0; iter < constraintIterations; iter++)
            {
                nodes[0].position = anchorPosition;

                for (int i = 0; i < nodes.Length - 1; i++)
                {
                    Vector3 delta = nodes[i + 1].position - nodes[i].position;
                    float dist = delta.magnitude;
                    if (dist < 0.0001f) continue;

                    float error = (dist - segmentLength) / dist;
                    Vector3 correction = delta * (error * 0.5f);

                    if (i == 0)
                    {
                        nodes[1].position -= delta * error;
                    }
                    else
                    {
                        nodes[i].position += correction;
                        nodes[i + 1].position -= correction;
                    }
                }

                // Keep anchor fixed
                nodes[0].position = anchorPosition;
            }

            // Compute current approximate angle from center node
            int midIdx = Mathf.Clamp(nodes.Length / 2, 1, nodes.Length - 1);
            Vector2 fromAnchor = new Vector2(nodes[midIdx].position.x - anchorPosition.x, nodes[midIdx].position.y - anchorPosition.y);
            currentAngle = Mathf.Atan2(fromAnchor.x, -fromAnchor.y);
            angularVelocity = 0f;
            smoothedSwingInput = 0f;
        }

        private void UpdateTriggerPositions()
        {
            if (triggerColliders.Count == 0 || nodes == null) return;
            int startNode = Mathf.Max(1, Mathf.RoundToInt(nodes.Length * 0.18f));
            int endNode = Mathf.Min(nodes.Length - 1, Mathf.RoundToInt(nodes.Length * 0.92f));
            float step = (float)(endNode - startNode) / Mathf.Max(1, triggerColliders.Count - 1);

            for (int t = 0; t < triggerColliders.Count; t++)
            {
                int nodeIdx = Mathf.Clamp(Mathf.RoundToInt(startNode + t * step), startNode, endNode);
                if (triggerColliders[t] != null)
                {
                    triggerColliders[t].transform.position = nodes[nodeIdx].position;
                }
            }
        }

        private void LateUpdate()
        {
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (nodes == null || nodes.Length < 2 || lineRenderer == null) return;

            Vector3[] positions = new Vector3[nodes.Length];
            for (int i = 0; i < nodes.Length; i++)
            {
                positions[i] = nodes[i].position;
            }
            lineRenderer.positionCount = nodes.Length;
            lineRenderer.SetPositions(positions);
        }

        // ==========================================
        // PLAYER INTERACTION INTERFACE
        // ==========================================
        public void OnPlayerTouchSegment(PlayerController player, int nodeIndex, Vector3 incomingVelocity = default)
        {
            if (attachedPlayer != null) return;
            if (!player.CanAttachToRope) return;
            if (player.CurrentState == PlayerState.LedgeGrab || player.CurrentState == PlayerState.LedgeClimb) return;
            if (player.CurrentState == PlayerState.RopeGrab || player.CurrentState == PlayerState.RopeClimb) return;

            attachedPlayer = player;
            nodeIndex = Mathf.Clamp(nodeIndex, 1, nodes.Length - 2);
            playerClimbFraction = (float)nodeIndex / Mathf.Max(1, nodes.Length - 1);
            playerClimbFraction = Mathf.Clamp(playerClimbFraction, 0.10f, 0.90f);

            float effLength = Mathf.Max(0.8f, totalLength * playerClimbFraction);
            Vector3 attachPos = nodes[nodeIndex].position;

            // Compute current pendulum angle from attach position
            Vector2 fromAnchor = new Vector2(attachPos.x - anchorPosition.x, attachPos.y - anchorPosition.y);
            currentAngle = Mathf.Atan2(fromAnchor.x, -fromAnchor.y);

            // Transfer incoming jump momentum into initial pendulum angular velocity (Planet of Lana standard)
            if (incomingVelocity.sqrMagnitude > 0.05f)
            {
                float tangentialSpeed = incomingVelocity.x * Mathf.Cos(currentAngle) + incomingVelocity.y * Mathf.Sin(currentAngle);
                angularVelocity = (tangentialSpeed / effLength) * momentumTransferRatio;
                float maxAngularVel = maxSwingSpeed / effLength;
                angularVelocity = Mathf.Clamp(angularVelocity, -maxAngularVel, maxAngularVel);
            }
            else
            {
                angularVelocity = 0f;
            }

            smoothedSwingInput = 0f;
            currentSwingInput = 0f;

            player.AttachToRope(this, attachPos, incomingVelocity);
        }

        public void ApplySwingInput(float horizontalInput, float dt)
        {
            currentSwingInput = Mathf.Clamp(horizontalInput, -1f, 1f);
        }

        public int GetNearestNodeIndex(Vector3 pos, out float distance)
        {
            int bestIdx = 0;
            float minDistSq = float.MaxValue;
            if (nodes != null)
            {
                for (int i = 0; i < nodes.Length; i++)
                {
                    float dSq = (nodes[i].position - pos).sqrMagnitude;
                    if (dSq < minDistSq)
                    {
                        minDistSq = dSq;
                        bestIdx = i;
                    }
                }
            }
            distance = Mathf.Sqrt(minDistSq);
            return bestIdx;
        }

        public void Climb(PlayerController player, float deltaMeters)
        {
            float oldLength = Mathf.Max(0.8f, totalLength * playerClimbFraction);
            float deltaFraction = -deltaMeters / totalLength;
            playerClimbFraction = Mathf.Clamp(playerClimbFraction + deltaFraction, 0.10f, 0.90f);
            float newLength = Mathf.Max(0.8f, totalLength * playerClimbFraction);

            // Conservation of angular momentum during climb (L1 * w1 = L2 * w2)
            if (newLength > 0.1f && oldLength > 0.1f)
            {
                angularVelocity = (angularVelocity * oldLength) / newLength;
            }

            // Subtle hand wiggle perturbation
            int grabIdx = Mathf.Clamp(Mathf.RoundToInt(playerClimbFraction * (nodes.Length - 1)), 1, nodes.Length - 1);
            float wiggle = Mathf.Sin(Time.time * 20f) * climbWiggleForce * Mathf.Sign(deltaMeters);
            nodes[grabIdx].position += new Vector3(wiggle * 0.012f, 0f, 0f);
        }

        public Vector3 GetPlayerRopePosition(PlayerController player)
        {
            float effLength = Mathf.Max(0.8f, totalLength * playerClimbFraction);
            return anchorPosition + new Vector3(Mathf.Sin(currentAngle), -Mathf.Cos(currentAngle), 0f) * effLength;
        }

        public Vector3 GetPointAtFraction(float fraction)
        {
            if (nodes == null || nodes.Length == 0) return transform.position;

            float exactIndex = Mathf.Clamp01(fraction) * (nodes.Length - 1);
            int lowerIndex = Mathf.FloorToInt(exactIndex);
            int upperIndex = Mathf.CeilToInt(exactIndex);
            float t = exactIndex - lowerIndex;

            if (lowerIndex >= nodes.Length - 1) return nodes[nodes.Length - 1].position;
            return Vector3.Lerp(nodes[lowerIndex].position, nodes[upperIndex].position, t);
        }

        public Vector3 GetRopeTangent(float fraction)
        {
            if (nodes == null || nodes.Length < 2) return Vector3.down;

            float exactIndex = Mathf.Clamp01(fraction) * (nodes.Length - 1);
            int lowerIndex = Mathf.Clamp(Mathf.FloorToInt(exactIndex), 0, nodes.Length - 2);
            int upperIndex = lowerIndex + 1;

            Vector3 delta = nodes[upperIndex].position - nodes[lowerIndex].position;
            return delta.sqrMagnitude > 0.0001f ? delta.normalized : Vector3.down;
        }

        public Vector3 GetReleaseVelocity()
        {
            // Physical tangential pendulum velocity: v = omega x r
            float effLength = Mathf.Max(0.8f, totalLength * playerClimbFraction);
            float vx = angularVelocity * effLength * Mathf.Cos(currentAngle);
            float vy = angularVelocity * effLength * Mathf.Sin(currentAngle);
            Vector3 vel = new Vector3(vx, vy, 0f);
            return vel; // Transfer physical pendulum release velocity
        }

        public void ReleasePlayer(PlayerController player)
        {
            if (attachedPlayer == player)
            {
                attachedPlayer = null;
                // Seed Verlet velocities so the rope continues swinging smoothly after release
                float effLength = Mathf.Max(0.8f, totalLength * playerClimbFraction);
                for (int i = 1; i < nodes.Length; i++)
                {
                    float factor = (float)i / (nodes.Length - 1);
                    float vx = angularVelocity * effLength * factor * Mathf.Cos(currentAngle);
                    float vy = angularVelocity * effLength * factor * Mathf.Sin(currentAngle);
                    nodes[i].prevPosition = nodes[i].position - new Vector3(vx, vy, 0f) * Time.fixedDeltaTime;
                }
            }
        }
    }
}
