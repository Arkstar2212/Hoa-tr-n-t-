using UnityEngine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Companion Creature Controller for "An" / "Tiểu Linh" (inspired by Mui in Planet of Lana).
    /// Faithfully follows Minh across platforms, leaps over gaps, huddles down in stealth reeds/bushes,
    /// and interacts organically with the procedural grass system.
    /// </summary>
    public class CompanionController : MonoBehaviour
    {
        [Header("Target Tracking")]
        [SerializeField] private Transform followTarget;
        [SerializeField] private float followDistance = 1.25f;
        [SerializeField] private float moveSpeed = 4.0f;
        [SerializeField] private float acceleration = 18f;
        [SerializeField] private float jumpForce = 7.5f;
        [SerializeField] private float gravity = 22f;

        [Header("Animation & Styling")]
        [SerializeField] private Transform visualTransform;
        [SerializeField] private float hopFrequency = 12f;
        [SerializeField] private float hopAmplitude = 0.08f;
        [SerializeField] private float breathSpeed = 2.4f;

        // References
        private CharacterController charController;
        private PlayerController playerCtrl;
        private GrassInteraction grassInteraction;

        // Locomotion state
        private Vector3 velocity;
        private float facingDirection = 1f;
        private bool isGrounded = true;
        private float verticalVelocity = 0f;
        private Vector3 baseVisualScale = Vector3.one;

        private void Awake()
        {
            charController = GetComponent<CharacterController>();
            grassInteraction = GetComponent<GrassInteraction>();
            if (visualTransform == null)
            {
                visualTransform = transform.Find("Visual");
            }
            if (visualTransform != null)
            {
                baseVisualScale = visualTransform.localScale;
            }
        }

        private void Start()
        {
            FindPlayerTarget();
        }

        public void SetTarget(Transform target)
        {
            followTarget = target;
            if (followTarget != null)
            {
                playerCtrl = followTarget.GetComponent<PlayerController>();
            }
        }

        private void FindPlayerTarget()
        {
            if (followTarget == null)
            {
                var player = Object.FindAnyObjectByType<PlayerController>();
                if (player != null)
                {
                    SetTarget(player.transform);
                }
            }
        }

        private void Update()
        {
            if (followTarget == null)
            {
                FindPlayerTarget();
                if (followTarget == null) return;
            }

            float dt = Time.deltaTime;
            HandleLocomotion(dt);
            UpdateStealthAndVisuals(dt);
        }

        private void HandleLocomotion(float dt)
        {
            Vector3 targetPos = followTarget.position;
            float targetFacing = 1f;
            if (playerCtrl != null)
            {
                targetFacing = playerCtrl.FacingDirection;
            }

            // Ideal trailing offset behind the player
            Vector3 desiredPos = targetPos - new Vector3(targetFacing * followDistance, 0f, 0f);

            // In 2D mode, stay locked to Z = 0
            if (playerCtrl == null || playerCtrl.CurrentPerspective == PerspectiveMode.TwoD)
            {
                desiredPos.z = 0f;
            }

            Vector3 toDesired = desiredPos - transform.position;
            if (playerCtrl == null || playerCtrl.CurrentPerspective == PerspectiveMode.TwoD)
            {
                toDesired.z = 0f;
            }

            float dist = toDesired.magnitude;
            float targetVelX = 0f;
            float targetVelZ = 0f;

            if (dist > 0.25f)
            {
                float speedMultiplier = Mathf.Clamp(dist / followDistance, 0.5f, 1.8f);
                targetVelX = Mathf.Sign(toDesired.x) * moveSpeed * speedMultiplier;
                if (Mathf.Abs(toDesired.x) > 0.1f)
                {
                    facingDirection = Mathf.Sign(toDesired.x);
                }

                if (playerCtrl != null && playerCtrl.CurrentPerspective == PerspectiveMode.ThreeD)
                {
                    targetVelZ = Mathf.Sign(toDesired.z) * moveSpeed * speedMultiplier;
                }
            }

            velocity.x = Mathf.MoveTowards(velocity.x, targetVelX, acceleration * dt);
            velocity.z = Mathf.MoveTowards(velocity.z, targetVelZ, acceleration * dt);

            // Gravity & Vertical movement
            if (charController != null && charController.isGrounded)
            {
                isGrounded = true;
                if (verticalVelocity < 0f) verticalVelocity = -1.5f;

                // Jump if player is jumping or target is higher up
                if (playerCtrl != null && (playerCtrl.CurrentState == PlayerState.Airborne || targetPos.y > transform.position.y + 0.6f) && dist > 0.8f)
                {
                    verticalVelocity = jumpForce;
                    isGrounded = false;
                }
            }
            else
            {
                isGrounded = false;
                verticalVelocity -= gravity * dt;
            }

            velocity.y = verticalVelocity;

            if (charController != null && charController.enabled)
            {
                charController.Move(velocity * dt);
            }
            else
            {
                transform.position += velocity * dt;
            }

            // Lock 2D plane
            if (playerCtrl == null || playerCtrl.CurrentPerspective == PerspectiveMode.TwoD)
            {
                Vector3 p = transform.position;
                p.z = 0f;
                transform.position = p;
            }
        }

        private void UpdateStealthAndVisuals(float dt)
        {
            if (visualTransform == null) return;

            // Turn visual towards facing direction
            visualTransform.localRotation = Quaternion.Euler(0f, facingDirection > 0 ? 0f : 180f, 0f);

            bool isHiding = playerCtrl != null && (playerCtrl.IsCrouching || playerCtrl.IsHidingInBush);
            float speed = new Vector2(velocity.x, velocity.z).magnitude;

            if (isHiding)
            {
                // Huddle down / squash low in stealth
                Vector3 crouchScale = new Vector3(baseVisualScale.x * 1.15f, baseVisualScale.y * 0.62f, baseVisualScale.z * 1.15f);
                visualTransform.localScale = Vector3.Lerp(visualTransform.localScale, crouchScale, dt * 8f);
            }
            else if (speed > 0.5f && isGrounded)
            {
                // Running hop squash & stretch
                float hop = Mathf.Sin(Time.time * hopFrequency) * hopAmplitude;
                float stretch = 1f + (hop * 1.5f);
                float squash = 1f - (hop * 0.8f);
                Vector3 runningScale = new Vector3(baseVisualScale.x * squash, baseVisualScale.y * stretch, baseVisualScale.z * squash);
                visualTransform.localScale = Vector3.Lerp(visualTransform.localScale, runningScale, dt * 14f);
            }
            else
            {
                // Gentle idle breathing
                float breath = Mathf.Sin(Time.time * breathSpeed) * 0.04f;
                Vector3 idleScale = new Vector3(baseVisualScale.x * (1f - breath), baseVisualScale.y * (1f + breath), baseVisualScale.z * (1f - breath));
                visualTransform.localScale = Vector3.Lerp(visualTransform.localScale, idleScale, dt * 4f);
            }
        }

        /// <summary>
        /// Instantly teleports companion next to Minh upon checkpoint respawn.
        /// </summary>
        public void TeleportToPlayer()
        {
            if (followTarget == null) return;
            if (charController != null) charController.enabled = false;
            transform.position = followTarget.position - new Vector3(facingDirection * 0.9f, 0f, 0f);
            velocity = Vector3.zero;
            verticalVelocity = 0f;
            if (charController != null) charController.enabled = true;
        }
    }
}
