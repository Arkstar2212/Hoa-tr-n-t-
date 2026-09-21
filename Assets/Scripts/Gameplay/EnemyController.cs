using UnityEngine;

namespace HoaVaMau.Gameplay
{
    public enum EnemyState
    {
        Patrol,
        Notice,
        Suspicious,
        Detected,
        Chase,
        Search,
        Distracted
    }

    /// <summary>
    /// Finite State Machine for enemy patrol, stealth detection, and chase.
    /// Supports both standard 2.5D planar patrol and Planet of Lana style Depth-Watcher mode
    /// (standing in midground overlooking an open gap, periodically looking back to create timed stealth windows).
    /// Integrated with CheckpointManager for instant failure reset upon capture.
    /// </summary>
    public class EnemyController : MonoBehaviour
    {
        [Header("Patrol Waypoints")]
        [SerializeField] private float[] patrolXPositions = new float[] { 20f, 26f };
        [SerializeField] private float patrolSpeed = 1.8f;
        [SerializeField] private float chaseSpeed = 3.8f;
        [SerializeField] private float searchSpeed = 1.2f;

        [Header("Patrol Behavior")]
        [SerializeField] private float pauseAtWaypointDuration = 2.0f;
        [SerializeField] private bool lookAroundDuringPause = true;

        [Header("Detection Parameters")]
        [SerializeField] private float viewDistance = 8.5f;
        [SerializeField] private float viewAngle = 60.0f;
        [SerializeField] private float detectionBuildRate = 1.5f;
        [SerializeField] private float alertDecayRate = 0.6f;
        [SerializeField] private LayerMask occlusionMask = ~0;

        [Header("Physics & Cliff Detection")]
        [SerializeField] private float gravity = 22f;
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField] private float cliffCheckDistance = 0.55f;
        [SerializeField] private float groundCheckDistance = 1.6f;

        private float verticalVelocity = 0f;

        [Header("Depth Watcher Mode (Planet of Lana Style)")]
        [SerializeField] private bool isDepthWatcher = false;
        [SerializeField] private float watchScreenDuration = 4.2f;
        [SerializeField] private float lookBackDuration = 3.2f;

        [Header("References")]
        [SerializeField] private VisionConeMesh visionCone;
        [SerializeField] private Transform visualTransform;
        [SerializeField] private TextMesh alertIndicator; // 3D text above head for '?' or '!'

        // State machine
        public EnemyState CurrentState { get; private set; } = EnemyState.Patrol;
        public float AlertMeter { get; private set; } = 0f; // 0.0 to 1.0
        public float FacingDirection { get; private set; } = 1f; // +1 right, -1 left

        // Patrol & Cycle state
        private int currentWaypointIndex = 0;
        private float pauseTimer = 0f;
        private float lookAroundTimer = 0f;
        private bool isLookingBack = false;
        private float depthCycleTimer = 0f;

        // Search & Chase state
        private Vector3 lastKnownPlayerPos;
        private float searchTimer = 0f;
        private const float MaxSearchTime = 3.5f;

        // 3D Room Traversal & Perspective Tracking
        private Vector3 currentFacing3D = Vector3.right;
        private bool isInsideRoom3D = false;
        private const float RoomDoorwayX = 35.1f;
        private const float RoomEntranceThresholdX = 34.6f;

        // Distraction & Noise Investigation
        [Header("Distraction & Investigation")]
        [SerializeField] private float hearingRadius = 12.0f;
        private Vector3 distractionTargetPos;
        private float distractionInvestigateTimer = 0f;
        private bool hasReachedDistractionPoint = false;

        // Initial setup for respawn reset
        private Vector3 initialSpawnPosition;
        private float initialFacingDirection;

        // Target
        private PlayerController targetPlayer;

        private void Start()
        {
            initialSpawnPosition = transform.position;
            initialFacingDirection = FacingDirection;
            currentFacing3D = new Vector3(FacingDirection, 0f, 0f);

            targetPlayer = Object.FindAnyObjectByType<PlayerController>();

            if (visionCone != null)
            {
                visionCone.viewDistance = viewDistance;
                visionCone.viewAngle = viewAngle;
                visionCone.obstacleLayer = occlusionMask;
            }

            if (!isDepthWatcher && (patrolXPositions == null || patrolXPositions.Length < 2))
            {
                float x = transform.position.x;
                patrolXPositions = new float[] { x - 3f, x + 3f };
            }
        }

        private void Update()
        {
            // If level completed, pacify enemy completely
            if (LevelGoalTrigger.Instance != null && LevelGoalTrigger.Instance.IsGoalReached)
            {
                AlertMeter = 0f;
                CurrentState = EnemyState.Patrol;
                UpdateConeColor();
                UpdateAlertIndicator();
                return;
            }

            float dt = Time.deltaTime;
            isInsideRoom3D = transform.position.x >= RoomEntranceThresholdX;

            if (isDepthWatcher)
            {
                UpdateDepthWatcherCycle(dt);
            }

            bool canSeePlayer = EvaluateLineOfSight(out float sightClarity);

            UpdateAlertMeter(canSeePlayer, sightClarity, dt);

            UpdateStateMachine(dt);

            if (visionCone != null)
            {
                float ddaDistMult = DynamicDifficultyManager.Instance != null ? DynamicDifficultyManager.Instance.ViewDistanceMultiplier : 1.0f;
                float ddaAngleMult = DynamicDifficultyManager.Instance != null ? DynamicDifficultyManager.Instance.ViewAngleMultiplier : 1.0f;
                visionCone.viewDistance = viewDistance * ddaDistMult;
                visionCone.viewAngle = viewAngle * ddaAngleMult;

                if (isDepthWatcher)
                {
                    if (CurrentState == EnemyState.Distracted)
                    {
                        Vector3 dirToTarget = (distractionTargetPos - (transform.position + Vector3.up * 1.5f)).normalized;
                        visionCone.UpdateConeDirection(dirToTarget, Vector3.right);
                    }
                    else if (isLookingBack)
                    {
                        // Looking back into the deep background
                        visionCone.UpdateConeDirection(new Vector3(0f, 0.15f, 1f), Vector3.right);
                    }
                    else
                    {
                        // Looking forward down across the player's gap
                        visionCone.UpdateConeDirection(new Vector3(0f, -0.32f, -1f), Vector3.right);
                    }
                }
                else if (isInsideRoom3D || CurrentState == EnemyState.Chase || CurrentState == EnemyState.Search || CurrentState == EnemyState.Suspicious || CurrentState == EnemyState.Distracted)
                {
                    // 3D Horizontal Vision Cone across the floor
                    Vector3 forward = currentFacing3D.normalized;
                    if (forward.sqrMagnitude < 0.001f) forward = Vector3.right;
                    Vector3 lateral = Vector3.Cross(Vector3.up, forward);
                    if (lateral.sqrMagnitude < 0.001f) lateral = Vector3.forward;
                    visionCone.UpdateConeDirection(forward, lateral);
                }
                else
                {
                    visionCone.UpdateCone(FacingDirection);
                }
                UpdateConeColor();
            }

            UpdateVisualFacing();
            UpdateAlertIndicator();

            // Lock 2.5D position and apply ground gravity for planar enemies outside the 3D room
            if (!isDepthWatcher)
            {
                ApplyGravityAndGround(dt);
                if (!isInsideRoom3D && CurrentState == EnemyState.Patrol)
                {
                    Vector3 p = transform.position;
                    p.z = Mathf.MoveTowards(p.z, 0f, dt * 3.0f);
                    transform.position = p;
                }
            }
        }

        private void UpdateDepthWatcherCycle(float dt)
        {
            depthCycleTimer += dt;
            if (!isLookingBack)
            {
                if (depthCycleTimer >= watchScreenDuration)
                {
                    isLookingBack = true;
                    depthCycleTimer = 0f;
                }
            }
            else
            {
                if (depthCycleTimer >= lookBackDuration)
                {
                    isLookingBack = false;
                    depthCycleTimer = 0f;
                }
            }
        }

        private bool EvaluateLineOfSight(out float clarity)
        {
            clarity = 0f;
            if (targetPlayer == null) return false;

            // Full concealment in bush
            if (targetPlayer.Concealment >= 0.95f) return false;

            float ddaDistMult = DynamicDifficultyManager.Instance != null ? DynamicDifficultyManager.Instance.ViewDistanceMultiplier : 1.0f;
            float ddaAngleMult = DynamicDifficultyManager.Instance != null ? DynamicDifficultyManager.Instance.ViewAngleMultiplier : 1.0f;
            float effectiveViewDistance = viewDistance * ddaDistMult;
            float effectiveViewAngle = viewAngle * ddaAngleMult;

            Vector3 eyePos = transform.position + Vector3.up * 1.5f;
            Vector3 playerEye = targetPlayer.transform.position + Vector3.up * 1.2f;
            Vector3 dirToPlayer = playerEye - eyePos;
            float dist = dirToPlayer.magnitude;

            if (dist > effectiveViewDistance) return false;

            if (isDepthWatcher)
            {
                // Looking away -> gap is 100% safe
                if (isLookingBack) return false;

                Vector3 forwardVec = new Vector3(0f, -0.32f, -1f).normalized;
                float angle = Vector3.Angle(forwardVec, dirToPlayer.normalized);
                if (angle > effectiveViewAngle * 0.5f) return false;
            }
            else
            {
                Vector3 facingVec = currentFacing3D.normalized;
                if (facingVec.sqrMagnitude < 0.001f) facingVec = new Vector3(FacingDirection, 0f, 0f);
                float angle = Vector3.Angle(facingVec, dirToPlayer.normalized);
                if (angle > effectiveViewAngle * 0.5f) return false;
            }

            // Raycast line of sight check against obstacles (crates, rocks, terrain, room walls)
            if (Physics.Raycast(eyePos, dirToPlayer.normalized, out RaycastHit hit, dist, occlusionMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.gameObject != targetPlayer.gameObject && hit.collider.transform.root != targetPlayer.transform.root)
                {
                    return false; // Occluded by cover!
                }
            }

            // Clarity affected by distance and bush cover
            float distFactor = 1f - (dist / effectiveViewDistance);
            float coverFactor = 1f - targetPlayer.Concealment;
            clarity = Mathf.Clamp01(distFactor * coverFactor);

            return true;
        }

        private void UpdateAlertMeter(bool seesPlayer, float clarity, float dt)
        {
            if (seesPlayer)
            {
                AlertMeter += dt * (detectionBuildRate * clarity + 0.35f);
                if (targetPlayer != null) lastKnownPlayerPos = targetPlayer.transform.position;
            }
            else
            {
                AlertMeter -= dt * alertDecayRate;
            }

            AlertMeter = Mathf.Clamp01(AlertMeter);
        }

        private void UpdateStateMachine(float dt)
        {
            switch (CurrentState)
            {
                case EnemyState.Patrol:
                    if (AlertMeter >= 1.0f)
                    {
                        CurrentState = EnemyState.Chase;
                    }
                    else if (AlertMeter >= 0.35f)
                    {
                        CurrentState = EnemyState.Suspicious;
                    }
                    else
                    {
                        if (!isDepthWatcher) HandlePatrol(dt);
                    }
                    break;

                case EnemyState.Suspicious:
                    if (AlertMeter >= 1.0f)
                    {
                        CurrentState = EnemyState.Chase;
                    }
                    else if (AlertMeter <= 0.05f)
                    {
                        CurrentState = EnemyState.Patrol;
                    }
                    else
                    {
                        if (!isDepthWatcher && targetPlayer != null)
                        {
                            Vector3 dir = lastKnownPlayerPos - transform.position;
                            dir.y = 0f;
                            if (dir.sqrMagnitude > 0.01f)
                            {
                                currentFacing3D = dir.normalized;
                                FacingDirection = Mathf.Sign(dir.x != 0 ? dir.x : FacingDirection);
                            }
                        }
                    }
                    break;

                case EnemyState.Chase:
                    if (AlertMeter <= 0.3f)
                    {
                        CurrentState = EnemyState.Search;
                        searchTimer = 0f;
                    }
                    else
                    {
                        HandleChase(dt);
                    }
                    break;

                case EnemyState.Search:
                    searchTimer += dt;
                    if (AlertMeter >= 0.8f)
                    {
                        CurrentState = EnemyState.Chase;
                    }
                    else if (searchTimer >= MaxSearchTime)
                    {
                        CurrentState = EnemyState.Patrol;
                    }
                    else
                    {
                        if (!isDepthWatcher) HandleSearch(dt);
                    }
                    break;

                case EnemyState.Distracted:
                    if (AlertMeter >= 1.0f)
                    {
                        CurrentState = EnemyState.Chase;
                    }
                    else
                    {
                        HandleDistracted(dt);
                    }
                    break;
            }
        }

        private void HandlePatrol(float dt)
        {
            if (patrolXPositions == null || patrolXPositions.Length == 0) return;

            // If enemy was inside 3D shelter room, return through doorway to bridge first!
            if (isInsideRoom3D)
            {
                Vector3 exitWaypoint = new Vector3(34.6f, transform.position.y, 0f);
                Vector3 toExit = exitWaypoint - transform.position;
                toExit.y = 0f;
                if (toExit.magnitude > 0.3f)
                {
                    Vector3 moveDir = toExit.normalized;
                    currentFacing3D = Vector3.Lerp(currentFacing3D, moveDir, dt * 8f);
                    FacingDirection = Mathf.Sign(moveDir.x != 0 ? moveDir.x : FacingDirection);
                    transform.position += moveDir * patrolSpeed * dt;
                    return;
                }
            }

            float targetX = patrolXPositions[currentWaypointIndex];
            float distToTarget = Mathf.Abs(transform.position.x - targetX);
            currentFacing3D = new Vector3(FacingDirection, 0f, 0f);

            if (distToTarget < 0.2f)
            {
                pauseTimer += dt;
                if (lookAroundDuringPause)
                {
                    lookAroundTimer += dt;
                    if (lookAroundTimer >= 1.0f)
                    {
                        FacingDirection = -FacingDirection;
                        currentFacing3D = new Vector3(FacingDirection, 0f, 0f);
                        lookAroundTimer = 0f;
                    }
                }

                if (pauseTimer >= pauseAtWaypointDuration)
                {
                    pauseTimer = 0f;
                    lookAroundTimer = 0f;
                    currentWaypointIndex = (currentWaypointIndex + 1) % patrolXPositions.Length;
                    FacingDirection = Mathf.Sign(patrolXPositions[currentWaypointIndex] - transform.position.x);
                    currentFacing3D = new Vector3(FacingDirection, 0f, 0f);
                }
            }
            else
            {
                FacingDirection = Mathf.Sign(targetX - transform.position.x);
                currentFacing3D = new Vector3(FacingDirection, 0f, 0f);

                // Cliff edge protection: do not walk off platform edges!
                if (!HasGroundAhead3D(new Vector3(FacingDirection, 0f, 0f)))
                {
                    FacingDirection = -FacingDirection;
                    currentFacing3D = new Vector3(FacingDirection, 0f, 0f);
                    currentWaypointIndex = (currentWaypointIndex + 1) % patrolXPositions.Length;
                    pauseTimer = 0f;
                    return;
                }

                float step = FacingDirection * patrolSpeed * dt;
                transform.position += new Vector3(step, 0f, 0f);
            }
        }

        private void HandleChase(float dt)
        {
            if (isDepthWatcher)
            {
                if (CheckpointManager.Instance != null)
                {
                    CheckpointManager.Instance.OnPlayerCaught();
                }
                return;
            }

            Vector3 playerPos = (targetPlayer != null) ? targetPlayer.transform.position : lastKnownPlayerPos;
            Vector3 currentPos = transform.position;

            // Intelligent doorway navigation between 2.5D bridge (X < 34.8) and 3D room (X >= 35.0)
            Vector3 moveTarget;
            bool enemyIsOutside = currentPos.x < 34.8f;
            bool playerIsInside = playerPos.x >= 35.0f;
            bool enemyIsInside = currentPos.x >= 35.3f;
            bool playerIsOutside = playerPos.x < 34.8f;

            if (enemyIsOutside && playerIsInside)
            {
                // Navigate into the doorway threshold (Z clamped to opening corridor)
                moveTarget = new Vector3(35.2f, currentPos.y, Mathf.Clamp(playerPos.z, -0.9f, 0.9f));
            }
            else if (enemyIsInside && playerIsOutside)
            {
                // Navigate back out through doorway into bridge
                moveTarget = new Vector3(34.6f, currentPos.y, 0f);
            }
            else
            {
                // Both inside 3D room or both outside -> Direct 3D chase!
                moveTarget = playerPos;
                if (!isInsideRoom3D && !playerIsInside)
                {
                    moveTarget.z = 0f;
                }
            }

            Vector3 toTarget = moveTarget - currentPos;
            toTarget.y = 0f;
            float dist = toTarget.magnitude;

            if (dist > 0.15f)
            {
                Vector3 moveDir = toTarget.normalized;
                currentFacing3D = Vector3.Lerp(currentFacing3D, moveDir, dt * 10f);
                FacingDirection = Mathf.Sign(moveDir.x != 0 ? moveDir.x : FacingDirection);

                // Cliff edge protection ahead in direction of movement
                if (HasGroundAhead3D(moveDir))
                {
                    float step = chaseSpeed * dt;
                    if (step > dist) step = dist;
                    transform.position += moveDir * step;
                }
            }

            // Caught player check (Full 3D Euclidean distance on horizontal plane)
            if (targetPlayer != null)
            {
                // If player is concealed in a bush, enemy brushing past does NOT trigger capture!
                if (targetPlayer.IsHidingInBush && targetPlayer.Concealment >= 0.85f)
                {
                    return;
                }

                Vector3 diff = targetPlayer.transform.position - transform.position;
                float horizontalDist = new Vector2(diff.x, diff.z).magnitude;
                if (horizontalDist < 1.35f && Mathf.Abs(diff.y) < 1.8f)
                {
                    if (CheckpointManager.Instance != null)
                    {
                        CheckpointManager.Instance.OnPlayerCaught();
                    }
                }
            }
        }

        private void HandleSearch(float dt)
        {
            Vector3 currentPos = transform.position;
            Vector3 targetPos = lastKnownPlayerPos;
            if (!isInsideRoom3D && targetPlayer != null && targetPlayer.CurrentPerspective == PerspectiveMode.TwoD)
            {
                targetPos.z = 0f;
            }

            Vector3 toLastKnown = targetPos - currentPos;
            toLastKnown.y = 0f;
            float dist = toLastKnown.magnitude;

            if (dist > 0.45f)
            {
                Vector3 moveDir = toLastKnown.normalized;
                currentFacing3D = Vector3.Lerp(currentFacing3D, moveDir, dt * 8f);
                FacingDirection = Mathf.Sign(moveDir.x != 0 ? moveDir.x : FacingDirection);

                if (HasGroundAhead3D(moveDir))
                {
                    transform.position += moveDir * searchSpeed * dt;
                }
            }
            else
            {
                // At last known location: search around in 3D
                lookAroundTimer += dt;
                if (lookAroundTimer >= 1.0f)
                {
                    lookAroundTimer = 0f;
                    if (isInsideRoom3D)
                    {
                        // Periodically scan around different 3D angles in the room
                        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                        currentFacing3D = new Vector3(Mathf.Cos(randomAngle), 0f, Mathf.Sin(randomAngle));
                    }
                    else
                    {
                        FacingDirection = -FacingDirection;
                        currentFacing3D = new Vector3(FacingDirection, 0f, 0f);
                    }
                }
            }
        }

        public void OnDistractionNoise(Vector3 noisePos, float radius)
        {
            if (CurrentState == EnemyState.Chase) return; // Ignore if actively chasing player

            float dist = Vector3.Distance(transform.position, noisePos);
            float maxHearDist = Mathf.Min(radius, hearingRadius);
            if (dist > maxHearDist) return;

            if (isDepthWatcher)
            {
                // Turn gaze directly towards distraction noise
                distractionTargetPos = noisePos;
                distractionInvestigateTimer = 3.5f;
                AlertMeter = 0.45f;
                CurrentState = EnemyState.Distracted;
                UpdateConeColor();
                UpdateAlertIndicator();
            }
            else
            {
                CurrentState = EnemyState.Distracted;
                distractionTargetPos = noisePos;
                distractionInvestigateTimer = 0f;
                hasReachedDistractionPoint = false;
                AlertMeter = 0.45f;
                UpdateConeColor();
                UpdateAlertIndicator();
            }

            Debug.Log($"<color=#f59e0b>[EnemyController]</color> {gameObject.name} distracted by noise at {noisePos:F1}");
        }

        private void HandleDistracted(float dt)
        {
            if (isDepthWatcher)
            {
                distractionInvestigateTimer -= dt;
                if (distractionInvestigateTimer <= 0f)
                {
                    AlertMeter = 0f;
                    CurrentState = EnemyState.Patrol;
                    depthCycleTimer = 0f;
                    isLookingBack = false;
                }
                return;
            }

            if (!hasReachedDistractionPoint)
            {
                Vector3 currentPos = transform.position;
                Vector3 targetPos = distractionTargetPos;

                // If outside 3D shelter and target is outside, keep Z = 0
                if (!isInsideRoom3D && targetPos.x < 34.8f)
                {
                    targetPos.z = 0f;
                }

                Vector3 toTarget = targetPos - currentPos;
                toTarget.y = 0f;
                float dist = toTarget.magnitude;

                if (dist > 0.45f)
                {
                    Vector3 moveDir = toTarget.normalized;
                    currentFacing3D = Vector3.Lerp(currentFacing3D, moveDir, dt * 8f);
                    FacingDirection = Mathf.Sign(moveDir.x != 0 ? moveDir.x : FacingDirection);

                    if (HasGroundAhead3D(moveDir))
                    {
                        float step = searchSpeed * 1.25f * dt;
                        if (step > dist) step = dist;
                        transform.position += moveDir * step;
                    }
                }
                else
                {
                    hasReachedDistractionPoint = true;
                    distractionInvestigateTimer = 0f;
                    lookAroundTimer = 0f;
                }
            }
            else
            {
                // Arrived at noise spot: look around and inspect for 3 seconds
                distractionInvestigateTimer += dt;
                lookAroundTimer += dt;
                if (lookAroundTimer >= 1.0f)
                {
                    FacingDirection = -FacingDirection;
                    currentFacing3D = new Vector3(FacingDirection, 0f, 0f);
                    lookAroundTimer = 0f;
                }

                if (distractionInvestigateTimer >= 3.0f)
                {
                    // Investigation finished without finding player -> resume normal patrol
                    AlertMeter = 0f;
                    CurrentState = EnemyState.Patrol;
                    pauseTimer = 0f;
                    lookAroundTimer = 0f;
                }
            }
        }

        private void ApplyGravityAndGround(float dt)
        {
            if (isDepthWatcher) return;

            Vector3 origin = transform.position + Vector3.up * 0.6f;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore))
            {
                Vector3 p = transform.position;
                p.y = hit.point.y;
                transform.position = p;
                verticalVelocity = -1f; // firm ground stick
            }
            else
            {
                verticalVelocity -= gravity * dt;
                if (verticalVelocity < -18f) verticalVelocity = -18f;
                transform.position += new Vector3(0f, verticalVelocity * dt, 0f);
            }
        }

        private bool HasGroundAhead(float dirX)
        {
            return HasGroundAhead3D(new Vector3(dirX, 0f, 0f));
        }

        private bool HasGroundAhead3D(Vector3 moveDir)
        {
            Vector3 flatDir = new Vector3(moveDir.x, 0f, moveDir.z).normalized;
            Vector3 rayStart = transform.position + flatDir * cliffCheckDistance + Vector3.up * 0.5f;
            return Physics.Raycast(rayStart, Vector3.down, out _, groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore);
        }

        private void OnCollisionEnter(Collision col)
        {
            var player = col.collider.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                // When player is concealed in a bush, enemy brushing past in the dark does NOT trigger catch!
                if (player.IsHidingInBush || player.Concealment >= 0.85f)
                {
                    return;
                }

                if (CheckpointManager.Instance != null)
                {
                    CheckpointManager.Instance.OnPlayerCaught();
                }
            }
        }

        public void ResetToDefaultState()
        {
            transform.position = initialSpawnPosition;
            FacingDirection = initialFacingDirection;
            currentFacing3D = new Vector3(initialFacingDirection, 0f, 0f);
            isInsideRoom3D = false;
            hasReachedDistractionPoint = false;
            distractionInvestigateTimer = 0f;
            verticalVelocity = 0f;
            CurrentState = EnemyState.Patrol;
            AlertMeter = 0f;
            pauseTimer = 0f;
            lookAroundTimer = 0f;
            searchTimer = 0f;
            depthCycleTimer = 0f;
            isLookingBack = false;
            currentWaypointIndex = 0;
            UpdateConeColor();
            UpdateAlertIndicator();
            UpdateVisualFacing();
        }

        private void UpdateConeColor()
        {
            if (visionCone == null) return;

            switch (CurrentState)
            {
                case EnemyState.Chase:
                    visionCone.SetColor(new Color(0.95f, 0.15f, 0.20f, 0.60f)); // Red alert
                    break;
                case EnemyState.Suspicious:
                case EnemyState.Search:
                    visionCone.SetColor(new Color(1.00f, 0.72f, 0.10f, 0.45f)); // Amber warning
                    break;
                default:
                    visionCone.SetColor(new Color(0.28f, 0.80f, 0.95f, 0.25f)); // Calm translucent cyan
                    break;
            }
        }

        private void UpdateAlertIndicator()
        {
            if (alertIndicator == null) return;

            switch (CurrentState)
            {
                case EnemyState.Chase:
                    alertIndicator.text = "!";
                    alertIndicator.color = Color.red;
                    break;
                case EnemyState.Suspicious:
                    alertIndicator.text = "?";
                    alertIndicator.color = new Color(1f, 0.7f, 0f);
                    break;
                case EnemyState.Search:
                    alertIndicator.text = "??";
                    alertIndicator.color = new Color(1f, 0.8f, 0.2f);
                    break;
                default:
                    alertIndicator.text = "";
                    break;
            }
        }

        private void UpdateVisualFacing()
        {
            if (visualTransform != null)
            {
                if (isDepthWatcher)
                {
                    Vector3 rot = visualTransform.localEulerAngles;
                    rot.y = isLookingBack ? 0f : 180f;
                    visualTransform.localEulerAngles = rot;
                }
                else if (isInsideRoom3D || (CurrentState == EnemyState.Chase && Mathf.Abs(currentFacing3D.z) > 0.1f))
                {
                    // Full 3D yaw facing smoothly aligned with movement
                    Vector3 flatDir = new Vector3(currentFacing3D.x, 0f, currentFacing3D.z);
                    if (flatDir.sqrMagnitude > 0.01f)
                    {
                        Quaternion targetRot = Quaternion.LookRotation(flatDir.normalized, Vector3.up);
                        visualTransform.rotation = Quaternion.Slerp(visualTransform.rotation, targetRot, Time.deltaTime * 10f);
                    }
                }
                else
                {
                    Vector3 rot = visualTransform.localEulerAngles;
                    rot.y = FacingDirection > 0 ? 0f : 180f;
                    visualTransform.localEulerAngles = rot;
                }
            }
        }
    }
}
