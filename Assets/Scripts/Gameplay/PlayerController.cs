using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HoaVaMau.Gameplay
{
    public enum PlayerState
    {
        Grounded,
        Airborne,
        LedgeGrab,
        LedgeClimb,
        RopeGrab,
        RopeClimb,
        PushPull,
        Carrying,
        AimingThrow
    }

    public enum PerspectiveMode
    {
        TwoD,
        ThreeD
    }

    /// <summary>
    /// Master 2.5D Cinematic Character Controller inspired by Planet of Lana, Inside, and Limbo.
    /// Features:
    /// - Walk & Sprint (analog sensitivity + Shift key, procedural body lean).
    /// - Jump (variable height, coyote time, jump buffering, landing squash & launch stretch).
    /// - Crouch & Crawl (low crawl through narrow rock tunnels with ceiling obstruction raycast).
    /// - Ledge Hang & Biomechanical Hoist.
    /// - Rope Climbing, Pendulum Pumping Swing, and Momentum Dismount.
    /// - Push & Pull heavy crates (Hold Left Mouse Button or E + move A/D).
    /// - Pick up (E) & Parabolic Throw (Hold Right Mouse Button, dynamic trajectory near/far).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Locomotion Speeds")]
        [SerializeField] private float walkSpeed = 3.6f;
        [SerializeField] private float sprintSpeed = 6.4f;
        [SerializeField] private float crouchSpeed = 2.0f;
        [SerializeField] private float crawlSpeed = 1.35f;
        [SerializeField] private float pushPullSpeed = 2.1f;
        [SerializeField] private float acceleration = 26f;
        [SerializeField] private float deceleration = 30f;

        [Header("Jumping & Gravity")]
        [SerializeField] private float jumpForce = 8.6f;
        [SerializeField] private float minJumpTime = 0.12f;
        [SerializeField] private float gravity = 23f;
        [SerializeField] private float fallGravityMultiplier = 1.4f;
        [SerializeField] private float terminalVelocity = 14.5f;
        [SerializeField] private float coyoteTimeDuration = 0.16f;
        [SerializeField] private float jumpBufferDuration = 0.15f;

        [Header("Heights & Colliders")]
        [SerializeField] private float standingHeight = 1.8f;
        [SerializeField] private float crouchHeight = 1.1f;
        [SerializeField] private float crawlHeight = 0.60f;
        [SerializeField] private LayerMask obstacleMask = ~0;

        [Header("Procedural Visual Dynamics")]
        [SerializeField] private Transform visualTransform;
        [SerializeField] private float runLeanAngle = 10.5f;
        [SerializeField] private float pushLeanAngle = 14.0f;
        [SerializeField] private float pullLeanAngle = -11.0f;

        [Header("Cinematic Ledge Climbing")]
        [SerializeField] private float climbDuration = 1.12f;
        [SerializeField] private float maxClimbForwardLean = 24f;

        [Header("Carry & Throw")]
        [SerializeField] private Transform carrySocket;
        [SerializeField] private TrajectoryPredictor trajectoryPredictor;
        [SerializeField] private float minThrowSpeed = 5.0f;
        [SerializeField] private float maxThrowSpeed = 14.5f;
        [SerializeField] private float throwChargeRate = 8.5f;

        // Public State
        public PlayerState CurrentState { get; private set; } = PlayerState.Grounded;
        public PerspectiveMode CurrentPerspective => PerspectiveMode.TwoD;
        public bool IsSprinting { get; private set; } = false;
        public bool IsCrouching { get; private set; } = false;
        public bool IsCrawling { get; private set; } = false;
        public float FacingDirection { get; private set; } = 1f; // +1 = right, -1 = left
        public float HorizontalInput => horizontalInput;
        public Vector3 Velocity => velocity;
        public bool CanAttachToRope => ropeAttachCooldown <= 0f;

        // Legacy compatibility properties for auxiliary systems
        public float Concealment { get; set; } = 0f;
        public bool IsHidingInBush { get; private set; } = false;
        public void SetHidingInBush(bool hiding) { IsHidingInBush = hiding; }
        public void SwitchPerspective(PerspectiveMode mode, float targetZ = 0f) { }

        // References
        private CharacterController charController;
        private LedgeGrabDetector ledgeDetector;
        private RopeController currentRope;
        private InteractableCrate currentCrate;
        private PortableItem carriedItem;

        // Timers & Physics
        private Vector3 velocity;
        private float horizontalInput;
        private float verticalInput;
        private bool jumpPressed;
        private bool jumpHeld;
        private bool crouchPressed;
        private bool sprintHeld;
        private bool interactPressed; // E key
        private bool grabCrateHeld;   // Left Mouse Button
        private bool aimThrowHeld;    // Right Mouse Button

        private float coyoteTimer = 0f;
        private float jumpBufferTimer = 0f;
        private float jumpTimeElapsed = 0f;
        private float ropeAttachCooldown = 0f;

        // Ledge climb variables
        private Vector3 climbStartPos;
        private Vector3 climbEndPos;
        private float climbProgress = 0f;

        // Planet of Lana carry/throw – drag with inertia
        private float currentThrowCharge = 5.0f;
        private Vector3 calculatedThrowVelocity;

        // Drag inertia: tracks carried item velocity history for momentum release
        private bool lmbDragActive = false;           // LMB held while carrying
        private Vector3 dragItemVelocity = Vector3.zero; // smoothed item world velocity
        private Vector3 lastCarrySocketPos = Vector3.zero; // prev frame socket world pos
        private float dragInertiaSmoothing = 12f;     // higher = snappier socket follow
        private float dragThrowScale = 1.6f;          // amplifies momentum on release

        // Base visual scale
        private float baseVisualScaleX = 0.64f;
        private float baseVisualScaleZ = 0.64f;
        private float squashStretchY = 1.0f;

        // Hand indicators for push/pull & carry
        private Transform handLeft;
        private Transform handRight;

        private void Awake()
        {
            charController = GetComponent<CharacterController>();
            if (charController != null)
            {
                charController.skinWidth = 0.04f;
                charController.stepOffset = 0.35f;
                charController.minMoveDistance = 0f;
            }
            ledgeDetector = GetComponent<LedgeGrabDetector>();

            if (visualTransform == null) visualTransform = transform.Find("Visual");
            if (visualTransform != null)
            {
                baseVisualScaleX = visualTransform.localScale.x;
                baseVisualScaleZ = visualTransform.localScale.z;
            }

            SetupCarrySocket();
            SetupHandVisuals();

            if (trajectoryPredictor == null)
            {
                trajectoryPredictor = GetComponentInChildren<TrajectoryPredictor>();
                if (trajectoryPredictor == null)
                {
                    GameObject trajGO = new GameObject("TrajectoryPredictor");
                    trajGO.transform.SetParent(transform);
                    trajGO.transform.localPosition = Vector3.zero;
                    trajectoryPredictor = trajGO.AddComponent<TrajectoryPredictor>();
                }
            }
        }

        private void SetupCarrySocket()
        {
            if (carrySocket == null)
            {
                Transform found = transform.Find("CarrySocket");
                if (found != null) carrySocket = found;
                else
                {
                    GameObject socketGO = new GameObject("CarrySocket");
                    socketGO.transform.SetParent(transform);
                    socketGO.transform.localPosition = new Vector3(0.42f, 1.02f, 0f);
                    carrySocket = socketGO.transform;
                }
            }
        }

        private void SetupHandVisuals()
        {
            if (visualTransform == null) return;

            Transform hl = visualTransform.Find("Hand_L");
            if (hl == null)
            {
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = "Hand_L";
                go.transform.SetParent(visualTransform);
                go.transform.localScale = new Vector3(0.14f, 0.14f, 0.14f);
                go.transform.localPosition = new Vector3(0.22f, 0.52f, 0.18f);
                Destroy(go.GetComponent<Collider>());
                handLeft = go.transform;
            }
            else handLeft = hl;

            Transform hr = visualTransform.Find("Hand_R");
            if (hr == null)
            {
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = "Hand_R";
                go.transform.SetParent(visualTransform);
                go.transform.localScale = new Vector3(0.14f, 0.14f, 0.14f);
                go.transform.localPosition = new Vector3(0.22f, 0.52f, -0.18f);
                Destroy(go.GetComponent<Collider>());
                handRight = go.transform;
            }
            else handRight = hr;
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            if (ropeAttachCooldown > 0f) ropeAttachCooldown -= dt;
            if (jumpBufferTimer > 0f) jumpBufferTimer -= dt;

            ReadInput();

            switch (CurrentState)
            {
                case PlayerState.Grounded:
                case PlayerState.Airborne:
                case PlayerState.Carrying:
                    HandleLocomotion(dt);
                    CheckObjectInteractions();
                    // While carrying and LMB drag active, update carry socket and track inertia
                    if (carriedItem != null && lmbDragActive)
                    {
                        UpdateDragCarryInertia(dt);
                    }
                    break;

                case PlayerState.PushPull:
                    HandlePushPull(dt);
                    break;

                case PlayerState.AimingThrow:
                    HandleAimingThrow(dt);
                    break;

                case PlayerState.LedgeGrab:
                    HandleLedgeGrab();
                    break;

                case PlayerState.LedgeClimb:
                    HandleLedgeClimb(dt);
                    break;

                case PlayerState.RopeGrab:
                case PlayerState.RopeClimb:
                    HandleRopeMovement(dt);
                    break;
            }

            // Always lock 2.5D plane
            Vector3 pos = transform.position;
            pos.z = 0f;
            transform.position = pos;

            UpdateVisualPostures(dt);
        }

        // ==========================================
        // INPUT PROCESSING
        // ==========================================
        private void ReadInput()
        {
            horizontalInput = 0f;
            verticalInput = 0f;
            jumpPressed = false;
            jumpHeld = false;
            crouchPressed = false;
            sprintHeld = false;
            interactPressed = false;
            grabCrateHeld = false;
            aimThrowHeld = false;

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) horizontalInput -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) horizontalInput += 1f;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) verticalInput += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed)
                {
                    verticalInput -= 1f;
                    crouchPressed = true;
                }

                if (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed) sprintHeld = true;
                if (kb.cKey.isPressed || kb.leftCtrlKey.isPressed) crouchPressed = true;

                if (kb.spaceKey.wasPressedThisFrame)
                {
                    jumpPressed = true;
                    jumpBufferTimer = jumpBufferDuration;
                }
                jumpHeld = kb.spaceKey.isPressed;

                if (kb.eKey.wasPressedThisFrame) interactPressed = true;
                if (kb.eKey.isPressed) grabCrateHeld = true;
            }

            var mouse = Mouse.current;
            if (mouse != null)
            {
                if (carriedItem != null)
                {
                    // Planet of Lana drag: hold LMB to drag item along movement with inertia
                    // Releasing LMB releases/throws with accumulated momentum
                    if (mouse.leftButton.isPressed)
                    {
                        lmbDragActive = true;
                    }
                    else if (lmbDragActive && mouse.leftButton.wasReleasedThisFrame)
                    {
                        // LMB just released – mark as throw using drag inertia
                        aimThrowHeld = true;
                        lmbDragActive = false;
                    }
                    else
                    {
                        lmbDragActive = false;
                    }

                    // Right click is still an instant drop
                    if (mouse.rightButton.wasPressedThisFrame) interactPressed = true;
                }
                else
                {
                    // Left click grabs and pushes/pulls crate
                    if (mouse.leftButton.isPressed) grabCrateHeld = true;
                    lmbDragActive = false;
                }
            }

            var gp = Gamepad.current;
            if (gp != null)
            {
                float stickX = gp.leftStick.x.ReadValue();
                if (Mathf.Abs(stickX) > 0.12f)
                {
                    horizontalInput = stickX;
                    if (Mathf.Abs(stickX) > 0.82f) sprintHeld = true;
                }

                float stickY = gp.leftStick.y.ReadValue();
                if (stickY > 0.45f) verticalInput = 1f;
                if (stickY < -0.45f)
                {
                    verticalInput = -1f;
                    crouchPressed = true;
                }

                if (gp.dpad.left.isPressed) horizontalInput -= 1f;
                if (gp.dpad.right.isPressed) horizontalInput += 1f;
                if (gp.dpad.down.isPressed || gp.buttonEast.isPressed) crouchPressed = true;

                if (gp.leftStickButton.isPressed || gp.rightTrigger.ReadValue() > 0.3f) sprintHeld = true;

                if (gp.buttonSouth.wasPressedThisFrame)
                {
                    jumpPressed = true;
                    jumpBufferTimer = jumpBufferDuration;
                }
                jumpHeld = gp.buttonSouth.isPressed;

                if (gp.buttonWest.wasPressedThisFrame) interactPressed = true;
                if (gp.buttonWest.isPressed || gp.rightTrigger.ReadValue() > 0.5f) grabCrateHeld = true;
                if (gp.leftTrigger.ReadValue() > 0.5f) aimThrowHeld = true;
            }
#else
            horizontalInput = Input.GetAxisRaw("Horizontal");
            verticalInput = Input.GetAxisRaw("Vertical");
            jumpPressed = Input.GetKeyDown(KeyCode.Space);
            jumpHeld = Input.GetKey(KeyCode.Space);
            if (jumpPressed) jumpBufferTimer = jumpBufferDuration;

            sprintHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            crouchPressed = Input.GetKey(KeyCode.C) || Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
            interactPressed = Input.GetKeyDown(KeyCode.E);

            if (carriedItem != null)
            {
                // LMB drag: hold to carry with inertia, release to throw
                if (Input.GetMouseButton(0))
                {
                    lmbDragActive = true;
                }
                else if (lmbDragActive && Input.GetMouseButtonUp(0))
                {
                    aimThrowHeld = true;
                    lmbDragActive = false;
                }
                else
                {
                    lmbDragActive = false;
                }

                // Right click = drop
                if (Input.GetMouseButtonDown(1)) interactPressed = true;
            }
            else
            {
                grabCrateHeld = Input.GetMouseButton(0) || Input.GetKey(KeyCode.E);
                lmbDragActive = false;
            }
#endif

            // Horizontal facing direction
            if (CurrentState != PlayerState.PushPull && CurrentState != PlayerState.AimingThrow)
            {
                if (Mathf.Abs(horizontalInput) > 0.1f)
                {
                    FacingDirection = Mathf.Sign(horizontalInput);
                    UpdateVisualFacing();
                }
            }
        }

        // ==========================================
        // LOCOMOTION & JUMPING
        // ==========================================
        private void HandleLocomotion(float dt)
        {
            // Dual ground check: CharacterController.isGrounded + SphereCast downward cushion
            Vector3 sphereOrigin = transform.position + Vector3.up * 0.45f;
            float sphereRadius = (charController != null) ? charController.radius * 0.85f : 0.28f;
            bool sphereHit = Physics.SphereCast(sphereOrigin, sphereRadius, Vector3.down, out RaycastHit groundHit, 0.48f, obstacleMask, QueryTriggerInteraction.Ignore);
            bool grounded = (charController != null && charController.isGrounded) || (velocity.y <= 0.1f && sphereHit && groundHit.normal.y > 0.65f);

            if (grounded)
            {
                coyoteTimer = coyoteTimeDuration;
                if (CurrentState == PlayerState.Airborne)
                {
                    // Landing squash
                    squashStretchY = 0.76f;
                    CurrentState = (carriedItem != null) ? PlayerState.Carrying : PlayerState.Grounded;
                }
                velocity.y = -2.0f; // Firm ground stick to prevent floating/falling through
            }
            else
            {
                coyoteTimer -= dt;
                CurrentState = PlayerState.Airborne;

                // Variable jump height & fall gravity
                float activeGravity = gravity;
                if (velocity.y > 0f)
                {
                    jumpTimeElapsed += dt;
                    if (!jumpHeld && jumpTimeElapsed > minJumpTime)
                    {
                        activeGravity *= 1.8f;
                    }
                }
                else
                {
                    activeGravity *= fallGravityMultiplier;
                }

                velocity.y -= activeGravity * dt;
                if (velocity.y < -terminalVelocity) velocity.y = -terminalVelocity;

                // Ledge grab detection when descending or at apex
                if (ledgeDetector != null && velocity.y < 3.5f && carriedItem == null)
                {
                    if (ledgeDetector.TryDetectLedge(FacingDirection, out Vector3 grabPos, out Vector3 landPos))
                    {
                        StartLedgeGrab(grabPos, landPos);
                        return;
                    }
                }
            }

            // Crouch & Crawl evaluation
            UpdateCrouchAndCrawl(grounded);

            // Jump execution (Coyote Time + Jump Buffer)
            if (jumpBufferTimer > 0f && coyoteTimer > 0f && !IsCrawling)
            {
                jumpBufferTimer = 0f;
                coyoteTimer = 0f;
                jumpTimeElapsed = 0f;
                velocity.y = jumpForce;
                squashStretchY = 1.25f; // Jump launch stretch
                CurrentState = PlayerState.Airborne;
            }

            // Speed calculation
            float currentSpeed = walkSpeed;
            if (IsCrawling)
            {
                currentSpeed = crawlSpeed;
                IsSprinting = false;
            }
            else if (IsCrouching)
            {
                currentSpeed = crouchSpeed;
                IsSprinting = false;
            }
            else if (sprintHeld && Mathf.Abs(horizontalInput) > 0.1f && carriedItem == null)
            {
                currentSpeed = sprintSpeed;
                IsSprinting = true;
            }
            else
            {
                currentSpeed = walkSpeed;
                IsSprinting = false;
            }

            float targetVelX = horizontalInput * currentSpeed;
            float accelRate = (Mathf.Abs(horizontalInput) > 0.05f) ? acceleration : deceleration;
            velocity.x = Mathf.MoveTowards(velocity.x, targetVelX, accelRate * dt);
            velocity.z = 0f;

            if (charController != null && charController.enabled)
            {
                charController.Move(velocity * dt);
            }
        }

        private void UpdateCrouchAndCrawl(bool grounded)
        {
            bool ceilingBlocked = CheckCeilingObstruction();

            if (grounded)
            {
                if (ceilingBlocked)
                {
                    IsCrawling = true;
                    IsCrouching = true;
                }
                else if (crouchPressed)
                {
                    IsCrouching = true;
                    IsCrawling = false;
                }
                else
                {
                    IsCrouching = false;
                    IsCrawling = false;
                }
            }
            else
            {
                if (!ceilingBlocked)
                {
                    IsCrouching = false;
                    IsCrawling = false;
                }
            }

            ApplyColliderDimensions();
        }

        private bool CheckCeilingObstruction()
        {
            Vector3 origin = transform.position + Vector3.up * 0.45f;
            float checkDist = standingHeight - 0.45f;
            return Physics.SphereCast(origin, 0.28f, Vector3.up, out _, checkDist, obstacleMask, QueryTriggerInteraction.Ignore);
        }

        private void ApplyColliderDimensions()
        {
            float targetH = standingHeight;
            if (IsCrawling) targetH = crawlHeight;
            else if (IsCrouching) targetH = crouchHeight;

            charController.height = targetH;
            charController.center = new Vector3(0f, targetH * 0.5f, 0f);
        }

        // ==========================================
        // OBJECT INTERACTIONS (PUSH/PULL & CARRY/THROW)
        // ==========================================
        private void CheckObjectInteractions()
        {
            // 1. Check for Push/Pull Crate
            if (grabCrateHeld && carriedItem == null && CurrentState == PlayerState.Grounded)
            {
                InteractableCrate crate = FindNearbyCrate(out int side);
                if (crate != null)
                {
                    StartPushPull(crate, side);
                    return;
                }
            }

            // 2. E key: Pick up if nothing held, or gently drop if holding
            if (interactPressed)
            {
                if (carriedItem != null)
                {
                    DropCarriedItem();
                }
                else
                {
                    PortableItem item = FindNearbyItem();
                    if (item != null)
                    {
                        PickupItem(item);
                    }
                }
            }

            // 3. Planet of Lana drag throw: LMB released after dragging → fling with inertia
            if (carriedItem != null && aimThrowHeld)
            {
                ExecuteDragThrow();
            }
        }

        private InteractableCrate FindNearbyCrate(out int side)
        {
            side = 0;
            var crates = Object.FindObjectsByType<InteractableCrate>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var crate in crates)
            {
                if (crate != null && crate.CanPlayerGrab(transform.position, out side))
                {
                    return crate;
                }
            }
            return null;
        }

        private PortableItem FindNearbyItem()
        {
            var items = Object.FindObjectsByType<PortableItem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            PortableItem closest = null;
            float minDist = float.MaxValue;

            foreach (var item in items)
            {
                if (item != null && item.CanPickup(transform.position))
                {
                    float d = Vector3.Distance(transform.position, item.transform.position);
                    if (d < minDist)
                    {
                        minDist = d;
                        closest = item;
                    }
                }
            }
            return closest;
        }

        // ==========================================
        // PUSH & PULL MECHANICS (Planet of Lana Style)
        // ==========================================
        private void StartPushPull(InteractableCrate crate, int side)
        {
            currentCrate = crate;
            CurrentState = PlayerState.PushPull;
            velocity = Vector3.zero;

            crate.StartGrab(this, side);

            // Anchor player facing towards the crate
            FacingDirection = -side;
            UpdateVisualFacing();

            Vector3 anchorPos = crate.GetPlayerAnchorPosition(side);
            transform.position = new Vector3(anchorPos.x, transform.position.y, 0f);
        }

        private void HandlePushPull(float dt)
        {
            if (currentCrate == null || !grabCrateHeld)
            {
                StopPushPull();
                return;
            }

            if (jumpPressed)
            {
                StopPushPull();
                return;
            }

            // Clamp input to pushPullSpeed
            float clampedInput = Mathf.Clamp(horizontalInput, -1f, 1f);
            currentCrate.MoveWithPlayer(clampedInput, dt);

            Vector3 anchorPos = currentCrate.GetPlayerAnchorPosition(currentCrate.PlayerSide);
            transform.position = new Vector3(anchorPos.x, transform.position.y, 0f);

            FacingDirection = -currentCrate.PlayerSide;
            UpdateVisualFacing();
        }

        private void StopPushPull()
        {
            if (currentCrate != null)
            {
                currentCrate.ReleaseGrab();
                currentCrate = null;
            }
            CurrentState = PlayerState.Grounded;
        }

        // ==========================================
        // CARRY & THROW MECHANICS (Planet of Lana Style)
        // ==========================================
        private void PickupItem(PortableItem item)
        {
            carriedItem = item;
            carriedItem.OnPickedUp(this, carrySocket);
            CurrentState = PlayerState.Carrying;
            UpdateCarrySocketTransform();

            // Initialize inertia tracking
            lmbDragActive = false;
            dragItemVelocity = Vector3.zero;
            lastCarrySocketPos = carrySocket != null ? carrySocket.position : transform.position;
        }

        private void DropCarriedItem()
        {
            if (carriedItem == null) return;

            Vector3 dropPos = transform.position + new Vector3(FacingDirection * 0.72f, 0.45f, 0f);
            Vector3 gentleNudge = new Vector3(FacingDirection * 1.5f, 0.5f, 0f);

            carriedItem.OnDropped(dropPos, gentleNudge);
            carriedItem = null;
            lmbDragActive = false;
            dragItemVelocity = Vector3.zero;
            CurrentState = PlayerState.Grounded;
        }

        /// <summary>
        /// Called every frame while LMB is held with a carried item.
        /// Tracks the carry socket velocity (which moves with the player) to build inertia.
        /// Also offsets the carry socket toward movement direction for a natural drag feel.
        /// </summary>
        private void UpdateDragCarryInertia(float dt)
        {
            if (carrySocket == null || dt <= 0f) return;

            Vector3 currentSocketPos = carrySocket.position;

            // Smooth the item velocity based on socket travel this frame
            Vector3 rawVel = (currentSocketPos - lastCarrySocketPos) / dt;
            dragItemVelocity = Vector3.Lerp(dragItemVelocity, rawVel, dragInertiaSmoothing * dt);

            lastCarrySocketPos = currentSocketPos;

            // Lag: shift carry socket slightly opposite to movement for drag feel
            // (item "resists" initial movement, then swings in movement direction)
            float dragOffsetX = -velocity.x * 0.035f;
            float dragOffsetY = -velocity.y * 0.025f;
            if (carrySocket != null)
            {
                Vector3 baseLocal = new Vector3(FacingDirection * 0.44f, 1.05f, 0f);
                carrySocket.localPosition = new Vector3(
                    baseLocal.x + dragOffsetX,
                    baseLocal.y + dragOffsetY,
                    baseLocal.z
                );
            }

            // Show trajectory prediction while dragging (like Planet of Lana's visible arc)
            Vector3 predictVelocity = dragItemVelocity * dragThrowScale;
            predictVelocity = Vector3.ClampMagnitude(predictVelocity, maxThrowSpeed);
            if (trajectoryPredictor != null && predictVelocity.sqrMagnitude > 0.5f)
            {
                Vector3 origin = currentSocketPos;
                origin.z = 0f;
                trajectoryPredictor.RenderTrajectory(origin, predictVelocity, Physics.gravity.y);
            }
            else if (trajectoryPredictor != null)
            {
                trajectoryPredictor.Hide();
            }
        }

        /// <summary>
        /// Called when LMB is released while carrying (aimThrowHeld = true from input).
        /// Releases item with the inertia velocity accumulated during drag.
        /// If player was stationary, falls back to directional throw.
        /// </summary>
        private void ExecuteDragThrow()
        {
            if (trajectoryPredictor != null) trajectoryPredictor.Hide();
            if (carriedItem == null) return;

            Vector3 launchPos = (carrySocket != null) ? carrySocket.position : transform.position + Vector3.up * 1.1f;
            launchPos.z = 0f;

            Vector3 throwVelocity = dragItemVelocity * dragThrowScale;
            throwVelocity = Vector3.ClampMagnitude(throwVelocity, maxThrowSpeed);

            // Minimum throw speed so it always goes somewhere interesting
            if (throwVelocity.sqrMagnitude < minThrowSpeed * minThrowSpeed)
            {
                // Fallback: throw in facing direction with small arc
                Vector3 forward = new Vector3(FacingDirection, 0.55f, 0f).normalized;
                throwVelocity = forward * minThrowSpeed;
            }

            carriedItem.OnThrown(launchPos, throwVelocity);
            carriedItem = null;
            lmbDragActive = false;
            dragItemVelocity = Vector3.zero;
            CurrentState = PlayerState.Grounded;
        }

        // Legacy aim-throw (kept for compatibility, not used with new drag system)
        private void StartAimingThrow()
        {
            CurrentState = PlayerState.AimingThrow;
            currentThrowCharge = minThrowSpeed;
            velocity = Vector3.zero;
        }

        private void HandleAimingThrow(float dt)
        {
            if (carriedItem == null || !aimThrowHeld)
            {
                ExecuteThrow();
                return;
            }

            currentThrowCharge = Mathf.MoveTowards(currentThrowCharge, maxThrowSpeed, throwChargeRate * dt);

            Vector3 origin = (carrySocket != null) ? carrySocket.position : transform.position + Vector3.up * 1.1f;
            origin.z = 0f;

            Camera cam = Camera.main;
            bool targetedByMouse = false;
            Vector3 targetPoint = Vector3.zero;

            if (cam != null)
            {
                Vector2 mouseScreenPos = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
                if (Mouse.current != null) mouseScreenPos = Mouse.current.position.ReadValue();
#else
                mouseScreenPos = Input.mousePosition;
#endif
                if (mouseScreenPos.sqrMagnitude > 1f)
                {
                    Ray ray = cam.ScreenPointToRay(mouseScreenPos);
                    Plane plane2D = new Plane(Vector3.forward, Vector3.zero);
                    if (plane2D.Raycast(ray, out float enter))
                    {
                        targetPoint = ray.GetPoint(enter);
                        targetPoint.z = 0f;
                        targetedByMouse = true;
                    }
                }
            }

            if (targetedByMouse)
            {
                Vector3 diff = targetPoint - origin;
                if (Mathf.Abs(diff.x) > 0.15f)
                {
                    FacingDirection = Mathf.Sign(diff.x);
                    UpdateVisualFacing();
                }

                float dist = diff.magnitude;
                float speed = Mathf.Clamp(dist * 1.35f, minThrowSpeed, maxThrowSpeed);
                Vector3 dir = diff.normalized;
                dir.y = Mathf.Clamp(dir.y + 0.25f, 0.2f, 0.9f);
                dir.Normalize();
                calculatedThrowVelocity = dir * speed;
            }
            else
            {
                Vector3 forward = new Vector3(FacingDirection, 0.65f, 0f).normalized;
                calculatedThrowVelocity = forward * currentThrowCharge;
            }

            if (trajectoryPredictor != null)
            {
                trajectoryPredictor.RenderTrajectory(origin, calculatedThrowVelocity, Physics.gravity.y);
            }

            UpdateCarrySocketTransform();
        }

        private void ExecuteThrow()
        {
            if (trajectoryPredictor != null) trajectoryPredictor.Hide();

            if (carriedItem != null)
            {
                Vector3 launchPos = (carrySocket != null) ? carrySocket.position : transform.position + Vector3.up * 1.1f;
                launchPos.z = 0f;
                carriedItem.OnThrown(launchPos, calculatedThrowVelocity);
                carriedItem = null;
            }

            CurrentState = PlayerState.Grounded;
        }

        private void UpdateCarrySocketTransform()
        {
            if (carrySocket != null && !lmbDragActive)
            {
                // Reset to neutral position when not dragging
                carrySocket.localPosition = new Vector3(FacingDirection * 0.44f, 1.05f, 0f);
            }
        }

        // ==========================================
        // LEDGE HANG & CLIMB
        // ==========================================
        public void StartLedgeGrab(Vector3 grabPos, Vector3 platformPos)
        {
            CurrentState = PlayerState.LedgeGrab;
            velocity = Vector3.zero;

            charController.enabled = false;
            transform.position = grabPos;

            climbStartPos = grabPos;
            climbEndPos = platformPos;

            if (visualTransform != null)
            {
                float yRot = FacingDirection > 0 ? 0f : 180f;
                visualTransform.localEulerAngles = new Vector3(0f, yRot, 0f);
            }
        }

        private void HandleLedgeGrab()
        {
            bool pushingAway = (horizontalInput != 0 && Mathf.Sign(horizontalInput) != FacingDirection);

            if (jumpPressed && pushingAway)
            {
                charController.enabled = true;
                CurrentState = PlayerState.Airborne;
                velocity = new Vector3(-FacingDirection * walkSpeed * 0.85f, jumpForce * 0.85f, 0f);
                FacingDirection = -FacingDirection;
                UpdateVisualFacing();
                return;
            }

            bool wantsClimb = verticalInput > 0.2f || jumpPressed || (horizontalInput != 0 && Mathf.Sign(horizontalInput) == FacingDirection);
            if (wantsClimb)
            {
                CurrentState = PlayerState.LedgeClimb;
                climbProgress = 0f;
                return;
            }

            if (verticalInput < -0.3f || pushingAway)
            {
                charController.enabled = true;
                CurrentState = PlayerState.Airborne;
                velocity = new Vector3(-FacingDirection * 1.2f, -2.0f, 0f);
                return;
            }
        }

        private void HandleLedgeClimb(float dt)
        {
            climbProgress += dt / Mathf.Max(0.1f, climbDuration);
            float t = Mathf.Clamp01(climbProgress);

            transform.position = EvaluateClimbTrajectory(t);
            ApplyClimbVisualDynamics(t);

            if (climbProgress >= 1.0f)
            {
                transform.position = climbEndPos;
                charController.enabled = true;
                velocity = Vector3.zero;
                CurrentState = PlayerState.Grounded;
                ResetVisualScale();
            }
        }

        private Vector3 EvaluateClimbTrajectory(float t)
        {
            float facing = FacingDirection;
            Vector3 start = climbStartPos;
            Vector3 end = climbEndPos;

            float wallX = start.x + facing * 0.34f;
            float ledgeY = end.y - 0.05f;

            Vector3 p0 = start;
            Vector3 p1 = new Vector3(wallX - facing * 0.16f, ledgeY - 0.30f, 0f);
            Vector3 p2 = new Vector3(wallX + facing * 0.38f, ledgeY + 0.06f, 0f);
            Vector3 p3 = end;

            if (t <= 0.14f)
            {
                float subT = t / 0.14f;
                float dip = Mathf.Sin(subT * Mathf.PI) * 0.025f;
                return p0 - new Vector3(0f, dip, 0f);
            }
            else if (t <= 0.52f)
            {
                float subT = (t - 0.14f) / 0.38f;
                float ease = 1f - Mathf.Pow(1f - subT, 2.2f);
                return Vector3.Lerp(p0, p1, ease);
            }
            else if (t <= 0.80f)
            {
                float subT = (t - 0.52f) / 0.28f;
                float ease = Mathf.SmoothStep(0f, 1f, subT);
                return Vector3.Lerp(p1, p2, ease);
            }
            else
            {
                float subT = (t - 0.80f) / 0.20f;
                float ease = Mathf.SmoothStep(0f, 1f, subT);
                return Vector3.Lerp(p2, p3, ease);
            }
        }

        private void ApplyClimbVisualDynamics(float t)
        {
            if (visualTransform == null) return;

            float facing = FacingDirection;
            float leanAngle = 0f;
            float heightFactor = 1f;
            float widthFactor = 1f;

            if (t <= 0.52f)
            {
                float subT = t / 0.52f;
                leanAngle = Mathf.Lerp(0f, 14f, subT);
                heightFactor = Mathf.Lerp(1.04f, 0.90f, subT);
                widthFactor = Mathf.Lerp(0.96f, 1.08f, subT);
            }
            else if (t <= 0.80f)
            {
                float subT = (t - 0.52f) / 0.28f;
                leanAngle = Mathf.Lerp(14f, maxClimbForwardLean, Mathf.Sin(subT * Mathf.PI));
                heightFactor = Mathf.Lerp(0.90f, 0.68f, subT);
                widthFactor = Mathf.Lerp(1.08f, 1.20f, subT);
            }
            else
            {
                float subT = (t - 0.80f) / 0.20f;
                leanAngle = Mathf.Lerp(maxClimbForwardLean * 0.4f, 0f, subT);
                heightFactor = Mathf.Lerp(0.68f, 1.0f, subT);
                widthFactor = Mathf.Lerp(1.20f, 1.0f, subT);
            }

            float targetZRot = -facing * leanAngle;
            float yRot = facing > 0 ? 0f : 180f;
            visualTransform.localEulerAngles = new Vector3(0f, yRot, targetZRot);

            visualTransform.localScale = new Vector3(baseVisualScaleX * widthFactor, (standingHeight * 0.5f) * heightFactor, baseVisualScaleZ * widthFactor);
            visualTransform.localPosition = new Vector3(0f, (standingHeight * 0.5f) * heightFactor, 0f);
        }

        // ==========================================
        // ROPE INTERACTION
        // ==========================================
        public void AttachToRope(RopeController rope, Vector3 attachPos)
        {
            if (ropeAttachCooldown > 0f) return;
            if (CurrentState == PlayerState.RopeGrab || CurrentState == PlayerState.RopeClimb) return;
            if (CurrentState == PlayerState.LedgeClimb) return;

            currentRope = rope;
            CurrentState = PlayerState.RopeGrab;
            velocity = Vector3.zero;

            charController.enabled = false;
            transform.position = new Vector3(attachPos.x, attachPos.y, 0f);
        }

        private void HandleRopeMovement(float dt)
        {
            if (currentRope == null)
            {
                DetachFromRope(Vector3.zero);
                return;
            }

            if (Mathf.Abs(verticalInput) > 0.1f)
            {
                CurrentState = PlayerState.RopeClimb;
                float climbSpeed = 3.2f;
                currentRope.Climb(this, verticalInput * climbSpeed * dt);
            }
            else
            {
                CurrentState = PlayerState.RopeGrab;
            }

            if (Mathf.Abs(horizontalInput) > 0.1f)
            {
                currentRope.ApplySwingInput(horizontalInput, dt);
                FacingDirection = Mathf.Sign(horizontalInput);
                UpdateVisualFacing();
            }

            transform.position = currentRope.GetPlayerRopePosition(this);

            if (jumpPressed)
            {
                float jumpDir = horizontalInput != 0 ? Mathf.Sign(horizontalInput) : FacingDirection;
                Vector3 releaseMomentum = currentRope.GetReleaseVelocity() + new Vector3(jumpDir * walkSpeed * 1.4f, jumpForce * 1.05f, 0f);
                ropeAttachCooldown = 0.55f;
                DetachFromRope(releaseMomentum);
            }
        }

        public void DetachFromRope(Vector3 releaseVelocity)
        {
            if (currentRope != null)
            {
                currentRope.ReleasePlayer(this);
                currentRope = null;
            }

            charController.enabled = true;
            CurrentState = PlayerState.Airborne;
            velocity = releaseVelocity;

            if (Mathf.Abs(releaseVelocity.x) > 0.1f)
            {
                transform.position += new Vector3(Mathf.Sign(releaseVelocity.x) * 0.45f, 0.15f, 0f);
            }
        }

        // ==========================================
        // PROCEDURAL VISUAL DYNAMICS & POSTURES
        // ==========================================
        private void UpdateVisualPostures(float dt)
        {
            if (visualTransform == null) return;
            if (CurrentState == PlayerState.LedgeClimb) return;

            float yRot = (FacingDirection > 0) ? 0f : 180f;
            float zRot = 0f;
            float targetHeight = standingHeight;
            float widthScale = 1.0f;

            squashStretchY = Mathf.MoveTowards(squashStretchY, 1.0f, 3.2f * dt);

            if (CurrentState == PlayerState.PushPull)
            {
                bool pushing = (horizontalInput != 0 && Mathf.Sign(horizontalInput) == FacingDirection);
                bool pulling = (horizontalInput != 0 && Mathf.Sign(horizontalInput) != FacingDirection);

                if (pushing) zRot = -FacingDirection * pushLeanAngle;
                else if (pulling) zRot = -FacingDirection * pullLeanAngle;
                else zRot = -FacingDirection * (pushLeanAngle * 0.5f);

                targetHeight = standingHeight * 0.94f;
                widthScale = 1.05f;

                if (handLeft != null) handLeft.localPosition = new Vector3(0.55f, 0.54f, 0.18f);
                if (handRight != null) handRight.localPosition = new Vector3(0.55f, 0.54f, -0.18f);
            }
            else if (IsCrawling)
            {
                targetHeight = crawlHeight;
                widthScale = 1.35f;
                zRot = -FacingDirection * 8f;

                if (handLeft != null) handLeft.localPosition = new Vector3(0.35f, 0.20f, 0.18f);
                if (handRight != null) handRight.localPosition = new Vector3(0.35f, 0.20f, -0.18f);
            }
            else if (IsCrouching)
            {
                targetHeight = crouchHeight;
                widthScale = 1.15f;
            }
            else if (IsSprinting && CurrentState == PlayerState.Grounded)
            {
                zRot = -FacingDirection * runLeanAngle;
            }

            visualTransform.localEulerAngles = new Vector3(0f, yRot, zRot);

            float scaleY = (targetHeight * 0.5f) * squashStretchY;
            visualTransform.localScale = new Vector3(baseVisualScaleX * widthScale, scaleY, baseVisualScaleZ * widthScale);
            visualTransform.localPosition = new Vector3(0f, scaleY, 0f);

            if (CurrentState != PlayerState.PushPull && CurrentState != PlayerState.Carrying && !IsCrawling)
            {
                if (handLeft != null) handLeft.localPosition = new Vector3(0.20f, 0.52f, 0.18f);
                if (handRight != null) handRight.localPosition = new Vector3(0.20f, 0.52f, -0.18f);
            }
        }

        private void UpdateVisualFacing()
        {
            if (visualTransform != null)
            {
                Vector3 rot = visualTransform.localEulerAngles;
                rot.y = (FacingDirection > 0) ? 0f : 180f;
                visualTransform.localEulerAngles = rot;
            }
            UpdateCarrySocketTransform();
        }

        private void ResetVisualScale()
        {
            if (visualTransform != null)
            {
                visualTransform.localScale = new Vector3(baseVisualScaleX, standingHeight * 0.5f, baseVisualScaleZ);
                visualTransform.localPosition = new Vector3(0f, standingHeight * 0.5f, 0f);
            }
        }

        public void Teleport(Vector3 newPos)
        {
            bool wasEnabled = charController.enabled;
            charController.enabled = false;
            transform.position = new Vector3(newPos.x, newPos.y, 0f);
            velocity = Vector3.zero;
            charController.enabled = wasEnabled;
        }

        public void RespawnAt(Vector3 checkpointPos)
        {
            if (currentRope != null)
            {
                currentRope.ReleasePlayer(this);
                currentRope = null;
            }
            Teleport(checkpointPos);
            CurrentState = PlayerState.Grounded;
            ropeAttachCooldown = 0.5f;
            ResetVisualScale();
        }

        // ==========================================
        // ON-SCREEN HUD GUIDE
        // ==========================================
        private void OnGUI()
        {
            if (!Application.isPlaying) return;

            // 1. Current State Banner (Top Left)
            float bannerW = 280f;
            float bannerH = 42f;
            GUI.Box(new Rect(16f, 16f, bannerW, bannerH), GUIContent.none);

            GUIStyle stateStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };

            string stateColor = "#4ade80";
            string stateLabel = CurrentState.ToString().ToUpper();

            if (IsSprinting) stateLabel = "CHẠY NHANH [SPRINT]";
            else if (IsCrawling) stateLabel = "BÒ TRƯỜN [CRAWL]";
            else if (IsCrouching) stateLabel = "NGỒI XỔM [CROUCH]";
            else if (CurrentState == PlayerState.PushPull) { stateLabel = "KÉO / ĐẨY THÙNG [PUSH-PULL]"; stateColor = "#f59e0b"; }
            else if (CurrentState == PlayerState.AimingThrow) { stateLabel = "NGẮM NÉM [AIM & THROW]"; stateColor = "#38bdf8"; }
            else if (CurrentState == PlayerState.Carrying) { stateLabel = "ĐANG BƯNG ĐỒ [CARRY]"; stateColor = "#a78bfa"; }
            else if (CurrentState == PlayerState.RopeGrab || CurrentState == PlayerState.RopeClimb) { stateLabel = "LEO DÂY [ROPE]"; stateColor = "#fb923c"; }
            else if (CurrentState == PlayerState.LedgeGrab || CurrentState == PlayerState.LedgeClimb) { stateLabel = "BÁM VÁCH [LEDGE]"; stateColor = "#f472b6"; }
            else if (CurrentState == PlayerState.Airborne) { stateLabel = "NHẢY / TRÊN KHÔNG [AIR]"; stateColor = "#60a5fa"; }
            else { stateLabel = "ĐI BỘ [WALK]"; }

            GUI.Label(new Rect(26f, 18f, bannerW - 20f, 24f), $"<color={stateColor}>● {stateLabel}</color>", stateStyle);

            GUIStyle subStyle = new GUIStyle(GUI.skin.label) { fontSize = 10, normal = { textColor = new Color(0.8f, 0.85f, 0.9f) } };
            GUI.Label(new Rect(26f, 36f, bannerW - 20f, 18f), $"Tốc độ: {velocity.x:F1} m/s | Hướng: {(FacingDirection > 0 ? "Phải" : "Trái")}", subStyle);

            // 2. Comprehensive Controls Guide (Top Right)
            float panelW = 430f;
            float panelH = 220f;
            float px = Screen.width - panelW - 16f;
            float py = 16f;

            GUI.Box(new Rect(px, py, panelW, panelH), GUIContent.none);

            GUIStyle guideTitle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.35f, 0.85f, 1.0f) }
            };
            GUI.Label(new Rect(px + 12f, py + 8f, panelW - 24f, 20f), "BẢNG ĐIỀU KHIỂN NHÂN VẬT (PLANET OF LANA 2D)", guideTitle);

            GUIStyle guideText = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal = { textColor = Color.white }
            };

            string controls = 
                "• <b>[A] / [D]</b> hoặc <b>Cần Analog</b>: Đi bộ (nhích nhẹ = đi chậm)\n" +
                "• <b>[Left Shift]</b> hoặc <b>Analog đẩy mạnh</b>: Chạy nước rút (Sprint)\n" +
                "• <b>[Space]</b>: Nhảy (nhấp nhẹ = nhảy thấp, giữ = nhảy cao, mép vực Coyote)\n" +
                "• <b>[C] / [Ctrl] / [S]</b>: Ngồi xổm | Tự động <b>Bò trườn (Crawl)</b> khi vào hầm thấp\n" +
                "• <b>[Chuột Trái] (Giữ gần thùng) + [A]/[D]</b>: Bám & <b>Kéo / Đẩy thùng gỗ</b>\n" +
                "• <b>[E]</b>: <b>Nhặt đồ</b> | Bấm [E] lần nữa để đặt nhẹ xuống\n" +
                "• <b>Đang cầm đồ → Giữ [Chuột Trái]</b>: <b>Kéo lê (Drag)</b> – vật theo quán tính\n" +
                "• <b>Thả [Chuột Trái]</b>: <b>Ném</b> theo hướng và tốc độ kéo (Parabol)\n" +
                "• <b>[Chuột Phải] khi đang cầm</b>: Đặt xuống ngay\n" +
                "• <b>Dây thừng</b>: Nhảy vào bám | <b>[A]/[D]</b> lấy đà đung đưa quán tính | <b>[Space]</b> phóng qua vực";

            GUI.Label(new Rect(px + 12f, py + 28f, panelW - 24f, panelH - 32f), controls, guideText);
        }
    }
}
