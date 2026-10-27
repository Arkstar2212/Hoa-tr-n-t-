using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HoaVaMau.Gameplay
{
    public enum TutorialActionType
    {
        RunTo,
        JumpGap,
        CrouchUnder,
        ClimbLedge,
        PanoramaReveal
    }

    [System.Serializable]
    public class TutorialActionNode
    {
        [Tooltip("Descriptive name of this tutorial milestone (e.g., 'Jump Across Dock Gap')")]
        public string nodeName = "Action Node";
        [Tooltip("Target waypoint transform or world position where NPC moves to")]
        public Transform targetWaypoint;
        [Tooltip("The action the guide demonstrates to teach the player")]
        public TutorialActionType actionType = TutorialActionType.RunTo;
        [Tooltip("Speed multiplier while moving towards this node")]
        public float moveSpeedMultiplier = 1.0f;
        [Tooltip("Where the player needs to reach to trigger success")]
        public Transform playerSuccessTrigger;
        [Tooltip("Radius around playerSuccessTrigger to confirm player passed the challenge")]
        public float successRadius = 1.6f;
        [Tooltip("Minimal diegetic button hint to show only if player hesitates (e.g., 'Space', 'Ctrl', 'W')")]
        public string hintButtonGlyph = "Space";
        public Sprite hintSilhouetteIcon;
    }

    /// <summary>
    /// Follow-The-Leader Diegetic Tutorial Guide (Planet of Lana Style).
    /// Guides the player through environmental mechanics (Jumping, Crouching, Climbing)
    /// by demonstrating actions first in safe, readable framing, pacing dynamically with
    /// the player's movement, looking back/beckoning when the player hesitates, and
    /// seamlessly reinforcing the emotional relationship.
    /// </summary>
    public class DiegeticGuideLeader : MonoBehaviour
    {
        [Header("Tutorial Sequence")]
        [SerializeField] private List<TutorialActionNode> sequenceNodes = new List<TutorialActionNode>();
        [SerializeField] private int currentNodeIndex = 0;

        [Header("Locomotion & Pacing")]
        [SerializeField] private float baseRunSpeed = 4.2f;
        [SerializeField] private float jumpForwardForce = 5.2f;
        [SerializeField] private float jumpUpwardForce = 7.0f;
        [SerializeField] private float gravity = 22.0f;

        [Header("Follow-The-Leader Dynamic Triggers")]
        [Tooltip("Maximum lead distance before NPC slows down or waits")]
        [SerializeField] private float maxLeadDistance = 6.0f;
        [Tooltip("Distance at which NPC considers player has caught up")]
        [SerializeField] private float catchupDistance = 2.5f;
        [Tooltip("Time player must remain stationary before NPC looks back and beckons")]
        [SerializeField] private float stationaryHesitationTime = 2.0f;
        [Tooltip("Time player must hesitate at obstacle before minimal floating diegetic icon fades in")]
        [SerializeField] private float promptHesitationTime = 3.5f;

        [Header("Audio & Gestures (Non-Verbal Cues)")]
        [SerializeField] private AudioClip beckonWhistleSfx;
        [SerializeField] private AudioClip softCalloutSfx;
        [SerializeField] private AudioSource audioSource;

        [Header("Visual & Animation")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Animator animator;
        [SerializeField] private GameObject diegeticPromptPrefab;

        // Runtime State
        private PlayerController player;
        private Transform playerTransform;
        private CharacterController charController;
        private Vector3 velocity;
        private float facingDirection = 1f;
        private bool isGrounded = true;
        private bool isPerformingAction = false;
        private bool isWaitingAtNode = false;

        private float playerStillTimer = 0f;
        private float playerObstacleTimer = 0f;
        private Vector3 lastPlayerPos;
        private GameObject activePromptInstance;
        private CanvasGroup activePromptCanvasGroup;

        // Animator Hashes
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int JumpHash = Animator.StringToHash("Jump");
        private static readonly int CrouchHash = Animator.StringToHash("IsCrouching");
        private static readonly int ClimbHash = Animator.StringToHash("Climb");
        private static readonly int BeckonHash = Animator.StringToHash("Beckon");
        private static readonly int LookBackHash = Animator.StringToHash("LookBack");

        private void Awake()
        {
            charController = GetComponent<CharacterController>();
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            if (visualRoot == null) visualRoot = transform.Find("Visual") ?? transform;
        }

        private void Start()
        {
            FindPlayer();
            if (player != null)
            {
                lastPlayerPos = player.transform.position;
            }
        }

        private void FindPlayer()
        {
            if (player == null)
            {
                player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
                if (player != null) playerTransform = player.transform;
            }
        }

        private void Update()
        {
            FindPlayer();
            if (player == null || isPerformingAction) return;

            float dt = Time.deltaTime;
            MonitorPlayerProgress(dt);

            if (currentNodeIndex < sequenceNodes.Count)
            {
                var node = sequenceNodes[currentNodeIndex];
                if (!isWaitingAtNode)
                {
                    ExecuteLeaderPacing(node, dt);
                }
                else
                {
                    ExecuteWaitAndObserve(node, dt);
                }
            }
            else
            {
                // Completed tutorial sequence
                PlayIdle(dt);
            }

            ApplyGravityAndMove(dt);
        }

        // =====================================================================================
        // FOLLOW-THE-LEADER PACING LOGIC FLOW
        // =====================================================================================
        private void ExecuteLeaderPacing(TutorialActionNode node, float dt)
        {
            if (node.targetWaypoint == null) return;

            Vector3 toTarget = node.targetWaypoint.position - transform.position;
            toTarget.z = 0f;
            float distToWaypoint = toTarget.magnitude;

            float distToPlayer = transform.position.x - playerTransform.position.x;

            // 1. If NPC is getting too far ahead of the player, slow down or pause to maintain eye contact
            float speedMult = node.moveSpeedMultiplier;
            if (distToPlayer > maxLeadDistance)
            {
                speedMult *= 0.3f; // slow jog / waiting pace
            }

            // 2. Reached the action milestone waypoint
            if (distToWaypoint < 0.6f)
            {
                StartCoroutine(PerformNodeActionRoutine(node));
                return;
            }

            // 3. Move towards waypoint
            float moveDir = Mathf.Sign(toTarget.x);
            SetFacing(moveDir);

            velocity.x = moveDir * baseRunSpeed * speedMult;
            UpdateAnimatorLocomotion(Mathf.Abs(velocity.x));
        }

        private void ExecuteWaitAndObserve(TutorialActionNode node, float dt)
        {
            // Face back towards the player to maintain non-verbal connection
            float toPlayerX = playerTransform.position.x - transform.position.x;
            SetFacing(Mathf.Sign(toPlayerX));

            velocity.x = 0f;
            UpdateAnimatorLocomotion(0f);

            // Check if player has successfully overcome the obstacle
            bool playerPassed = false;
            if (node.playerSuccessTrigger != null)
            {
                float d = Vector2.Distance(
                    new Vector2(playerTransform.position.x, playerTransform.position.y),
                    new Vector2(node.playerSuccessTrigger.position.x, node.playerSuccessTrigger.position.y)
                );
                playerPassed = d <= node.successRadius;
            }
            else
            {
                // Default fallback: player has crossed NPC's X position
                playerPassed = (playerTransform.position.x >= transform.position.x - 0.5f);
            }

            // Also check corresponding player mechanics state
            if (node.actionType == TutorialActionType.JumpGap && player.CurrentState == PlayerState.Airborne)
            {
                DismissDiegeticPrompt();
            }
            else if (node.actionType == TutorialActionType.CrouchUnder && (player.IsCrouching || player.IsCrawling))
            {
                DismissDiegeticPrompt();
            }
            else if (node.actionType == TutorialActionType.ClimbLedge && (player.CurrentState == PlayerState.LedgeGrab || player.CurrentState == PlayerState.LedgeClimb))
            {
                DismissDiegeticPrompt();
            }

            if (playerPassed)
            {
                OnPlayerSucceededNode(node);
            }
        }

        // =====================================================================================
        // ENVIRONMENTAL ACTION DEMONSTRATIONS (MIMICRY)
        // =====================================================================================
        private IEnumerator PerformNodeActionRoutine(TutorialActionNode node)
        {
            isPerformingAction = true;
            velocity = Vector3.zero;

            switch (node.actionType)
            {
                case TutorialActionType.JumpGap:
                    yield return StartCoroutine(DemonstrateJumpAcrossGap());
                    break;

                case TutorialActionType.CrouchUnder:
                    yield return StartCoroutine(DemonstrateCrouchUnder());
                    break;

                case TutorialActionType.ClimbLedge:
                    yield return StartCoroutine(DemonstrateLedgeClimb());
                    break;

                case TutorialActionType.PanoramaReveal:
                    yield return StartCoroutine(DemonstratePanoramaGaze());
                    break;

                case TutorialActionType.RunTo:
                default:
                    yield return null;
                    break;
            }

            isPerformingAction = false;
            isWaitingAtNode = true;
            playerObstacleTimer = 0f;
        }

        private IEnumerator DemonstrateJumpAcrossGap()
        {
            SetFacing(1f);
            if (animator != null) animator.SetTrigger(JumpHash);

            // Running leap forward
            velocity.x = jumpForwardForce;
            velocity.y = jumpUpwardForce;
            isGrounded = false;

            float elapsed = 0f;
            while (elapsed < 0.65f || !isGrounded)
            {
                elapsed += Time.deltaTime;
                ApplyGravityAndMove(Time.deltaTime);
                if (elapsed > 0.2f && charController != null && charController.isGrounded)
                {
                    isGrounded = true;
                    break;
                }
                yield return null;
            }

            velocity = Vector3.zero;
            yield return new WaitForSeconds(0.2f);
        }

        private IEnumerator DemonstrateCrouchUnder()
        {
            SetFacing(1f);
            if (animator != null) animator.SetBool(CrouchHash, true);

            // Crouch walk forward smoothly under low obstacle
            float timer = 0f;
            while (timer < 1.4f)
            {
                timer += Time.deltaTime;
                velocity.x = baseRunSpeed * 0.45f;
                ApplyGravityAndMove(Time.deltaTime);
                yield return null;
            }

            if (animator != null) animator.SetBool(CrouchHash, false);
            velocity = Vector3.zero;
            yield return new WaitForSeconds(0.2f);
        }

        private IEnumerator DemonstrateLedgeClimb()
        {
            SetFacing(1f);
            // Jump and grab
            velocity.y = jumpUpwardForce * 0.8f;
            yield return new WaitForSeconds(0.3f);

            if (animator != null) animator.SetTrigger(ClimbHash);
            yield return new WaitForSeconds(0.8f);

            velocity = Vector3.zero;
        }

        private IEnumerator DemonstratePanoramaGaze()
        {
            SetFacing(1f);
            // Stand at vista edge, admiring the grand horizon
            yield return new WaitForSeconds(0.5f);
            TriggerBeckonGesture();
        }

        // =====================================================================================
        // PLAYER MONITORING & DIEGETIC CUES
        // =====================================================================================
        private void MonitorPlayerProgress(float dt)
        {
            float playerDelta = Vector3.Distance(playerTransform.position, lastPlayerPos);
            lastPlayerPos = playerTransform.position;

            if (playerDelta < 0.05f * dt)
            {
                playerStillTimer += dt;
                playerObstacleTimer += dt;
            }
            else
            {
                playerStillTimer = Mathf.Max(0f, playerStillTimer - dt * 2f);
            }

            // 1. If player is standing still for too long, NPC looks back and beckons
            if (playerStillTimer >= stationaryHesitationTime && isWaitingAtNode)
            {
                playerStillTimer = 0f;
                TriggerBeckonGesture();
            }

            // 2. If player is stuck at an obstacle, show minimal diegetic glyph directly above player
            if (playerObstacleTimer >= promptHesitationTime && isWaitingAtNode)
            {
                ShowDiegeticPrompt(sequenceNodes[currentNodeIndex]);
            }
        }

        private void TriggerBeckonGesture()
        {
            if (animator != null)
            {
                animator.SetTrigger(BeckonHash);
            }

            // Play non-verbal audio cue
            if (audioSource != null)
            {
                var clip = (UnityEngine.Random.value > 0.5f && beckonWhistleSfx != null) ? beckonWhistleSfx : softCalloutSfx;
                if (clip != null) audioSource.PlayOneShot(clip, 0.65f);
            }
        }

        private void OnPlayerSucceededNode(TutorialActionNode node)
        {
            DismissDiegeticPrompt();
            isWaitingAtNode = false;
            currentNodeIndex++;

            // Turn back forward with enthusiasm
            SetFacing(1f);
            if (animator != null) animator.SetTrigger(LookBackHash);
        }

        // =====================================================================================
        // MINIMAL DIEGETIC FLOATING PROMPT (Planet of Lana Image 5 Style)
        // =====================================================================================
        private void ShowDiegeticPrompt(TutorialActionNode node)
        {
            if (activePromptInstance != null) return;

            // Spawn or activate clean floating glyph above player's head
            if (diegeticPromptPrefab != null && playerTransform != null)
            {
                activePromptInstance = Instantiate(diegeticPromptPrefab, playerTransform.position + Vector3.up * 2.2f, Quaternion.identity);
                activePromptInstance.transform.SetParent(playerTransform);
                activePromptCanvasGroup = activePromptInstance.GetComponentInChildren<CanvasGroup>();
                if (activePromptCanvasGroup != null)
                {
                    StartCoroutine(FadePromptRoutine(activePromptCanvasGroup, 0f, 1f, 0.4f));
                }
            }
        }

        private void DismissDiegeticPrompt()
        {
            if (activePromptInstance != null)
            {
                if (activePromptCanvasGroup != null)
                {
                    StartCoroutine(FadePromptAndDestroyRoutine(activePromptCanvasGroup, activePromptInstance, 0.3f));
                }
                else
                {
                    Destroy(activePromptInstance);
                }
                activePromptInstance = null;
                activePromptCanvasGroup = null;
            }
            playerObstacleTimer = 0f;
        }

        private IEnumerator FadePromptRoutine(CanvasGroup group, float from, float to, float duration)
        {
            float t = 0f;
            group.alpha = from;
            while (t < duration)
            {
                t += Time.deltaTime;
                group.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            group.alpha = to;
        }

        private IEnumerator FadePromptAndDestroyRoutine(CanvasGroup group, GameObject go, float duration)
        {
            yield return StartCoroutine(FadePromptRoutine(group, group.alpha, 0f, duration));
            Destroy(go);
        }

        // =====================================================================================
        // UTILITIES & LOCOMOTION
        // =====================================================================================
        private void SetFacing(float dir)
        {
            if (Mathf.Abs(dir) > 0.01f)
            {
                facingDirection = Mathf.Sign(dir);
                if (visualRoot != null)
                {
                    visualRoot.localRotation = Quaternion.Euler(0f, facingDirection > 0f ? 0f : 180f, 0f);
                }
            }
        }

        private void UpdateAnimatorLocomotion(float speed)
        {
            if (animator != null)
            {
                animator.SetFloat(SpeedHash, speed);
            }
        }

        private void PlayIdle(float dt)
        {
            velocity.x = 0f;
            UpdateAnimatorLocomotion(0f);
        }

        private void ApplyGravityAndMove(float dt)
        {
            if (charController != null && charController.enabled)
            {
                if (charController.isGrounded && velocity.y < 0f)
                {
                    velocity.y = -2.0f;
                    isGrounded = true;
                }
                else
                {
                    velocity.y -= gravity * dt;
                }

                charController.Move(velocity * dt);

                // Lock Z to 0 for pure 2.5D planar navigation
                Vector3 p = transform.position;
                p.z = 0f;
                transform.position = p;
            }
            else
            {
                transform.position += velocity * dt;
            }
        }
    }
}
