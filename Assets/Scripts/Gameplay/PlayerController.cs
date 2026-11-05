using UnityEngine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Player Controller tuân thủ chuẩn kiến trúc SOLID:
    /// - S (Single Responsibility): Đóng vai trò Facade / Context Orchestrator, điều phối các Handler độc lập:
    ///     * IPlayerInput / PlayerInputReader: Xử lý nhập liệu bàn phím/chuột.
    ///     * PlayerMotor: Quản lý động học di chuyển, gia tốc, trọng lực và nhảy.
    ///     * PlayerPostureHandler: Quản lý quét trần thấp, bò trườn, ngồi khom và kích thước Collider.
    ///     * PlayerInteractionHandler: Quản lý kéo/đẩy thùng gỗ, bưng bê ném đồ vật.
    ///     * PlayerLedgeClimber: Quản lý bám gờ mép và trèo vách đá.
    ///     * PlayerRopeHandler: Quản lý bám đu dây thừng.
    ///     * PlayerVisualEffects: Quản lý độ nghiêng thân người, co giãn (squash & stretch) và xoay mặt.
    /// - O (Open/Closed): Dễ dàng mở rộng hoặc thêm trạng thái mới mà không sửa nát code cũ.
    /// - L (Liskov Substitution): Các thành phần phụ thuộc vào Interface và contract rõ ràng.
    /// - I (Interface Segregation): Các interface chuyên biệt, không ép buộc phụ thuộc thừa.
    /// - D (Dependency Inversion): Tách biệt logic game khỏi thiết bị phần cứng cụ thể (IPlayerInput).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Locomotion Speeds")]
        [SerializeField] private float walkSpeed = 2.4f;
        [SerializeField] private float sprintSpeed = 4.2f; // Tốc độ chạy tự nhiên, không quá nhanh
        [SerializeField] private float crouchSpeed = 1.15f;
        [SerializeField] private float crawlSpeed = 1.2f;
        [SerializeField] private float pushPullSpeed = 2.1f;
        [SerializeField] private float acceleration = 26f;
        [SerializeField] private float deceleration = 30f;

        [Header("Jumping & Gravity")]
        [SerializeField] private float jumpForce = 6.2f;
        [SerializeField] private float minJumpTime = 0.12f;
        [SerializeField] private float gravity = 22.5f;
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
        [SerializeField] private float climbDuration = 1.22f;
        [SerializeField] private float maxClimbForwardLean = 24f;
        [SerializeField] private bool autoClimbOnLedgeGrab = false; // Mặc định bám gờ chờ người chơi bấm W/Up để leo lên

        [Header("Carry & Throw")]
        [SerializeField] private Transform carrySocket;
        [SerializeField] private TrajectoryPredictor trajectoryPredictor;
        [SerializeField] private float minThrowSpeed = 5.0f;
        [SerializeField] private float maxThrowSpeed = 14.5f;
        [SerializeField] private float throwChargeRate = 8.5f;

        // SOLID Handlers (Separated Concerns)
        private IPlayerInput inputReader;
        private PlayerMotor motor;
        private PlayerPostureHandler postureHandler;
        private PlayerInteractionHandler interactionHandler;
        private PlayerLedgeClimber ledgeClimber;
        private PlayerRopeHandler ropeHandler;
        private PlayerVisualEffects visualEffects;

        // References
        private CharacterController charController;
        private LedgeGrabDetector ledgeDetector;
        private Transform hipBone;
        private float airTime = 0f;
        private float lastFallVelocityY = 0f;

        // Public State (Backwards Compatibility API)
        public PlayerState CurrentState { get; private set; } = PlayerState.Grounded;
        public PerspectiveMode CurrentPerspective => PerspectiveMode.TwoD;
        public bool IsSprinting { get; private set; } = false;
        public bool IsCrouching => Posture != null && Posture.IsCrouching;
        public bool IsCrawling => Posture != null && Posture.IsCrawling;
        public float FacingDirection { get; private set; } = 1f; // +1 = right, -1 = left
        public float HorizontalInput => InputReader != null ? InputReader.Horizontal : 0f;
        public Vector3 Velocity => Motor != null ? Motor.Velocity : Vector3.zero;
        public bool CanAttachToRope => RopeHandler != null && RopeHandler.CanAttachToRope;

        // Public Handlers Accessors (Clean SOLID composition with lazy init)
        public IPlayerInput InputReader { get { EnsureInitialized(); return inputReader; } }
        public PlayerMotor Motor { get { EnsureInitialized(); return motor; } }
        public PlayerPostureHandler Posture { get { EnsureInitialized(); return postureHandler; } }
        public PlayerInteractionHandler Interactions { get { EnsureInitialized(); return interactionHandler; } }
        public PlayerInteractionHandler InteractionHandler => Interactions;
        public PlayerLedgeClimber LedgeClimber { get { EnsureInitialized(); return ledgeClimber; } }
        public PlayerRopeHandler RopeHandler { get { EnsureInitialized(); return ropeHandler; } }
        public PlayerVisualEffects VisualEffects { get { EnsureInitialized(); return visualEffects; } }

        /// <summary>
        /// Cung cấp vị trí camera tập trung chính xác (Theo dõi mượt mà khi leo vách không giật khung hình)
        /// </summary>
        public Vector3 CameraTargetPosition => transform.position;

        // Legacy compatibility properties
        public float Concealment { get; set; } = 0f;
        public bool IsHidingInBush { get; private set; } = false;
        public void SetHidingInBush(bool hiding) { IsHidingInBush = hiding; }
        public void SwitchPerspective(PerspectiveMode mode, float targetZ = 0f) { }

        public void SetState(PlayerState newState) => CurrentState = newState;
        public void SetFacingDirection(float dir)
        {
            FacingDirection = dir;
            if (visualEffects != null) visualEffects.UpdateFacing(FacingDirection);
            if (interactionHandler != null) interactionHandler.UpdateCarrySocketTransform(FacingDirection);
        }

        public void EnsureInitialized()
        {
            if (motor == null || postureHandler == null || inputReader == null)
            {
                InitializeHandlers(); if (postureHandler != null) postureHandler.UpdatePostures(true, false, 0f, FacingDirection);
            }
        }

        public void InitializeHandlers()
        {
            if (charController == null) charController = GetComponent<CharacterController>();
            if (ledgeDetector == null) ledgeDetector = GetComponent<LedgeGrabDetector>();
            if (ledgeDetector == null) ledgeDetector = gameObject.AddComponent<LedgeGrabDetector>();
            if (hipBone == null) hipBone = transform.Find("Armature/BoneRoot/Hip");
            if (visualTransform == null) visualTransform = transform.Find("Visual");

            if (GetComponent<AnAnimationController>() == null)
            {
                gameObject.AddComponent<AnAnimationController>();
            }

            SetupCarrySocket();
            SetupTrajectoryPredictor();

            // Khởi tạo các Handler chuyên biệt theo chuẩn SOLID
            if (inputReader == null) inputReader = new PlayerInputReader();
            if (motor == null)
            {
                motor = new PlayerMotor(charController)
                {
                    WalkSpeed = walkSpeed,
                    SprintSpeed = sprintSpeed,
                    CrouchSpeed = crouchSpeed,
                    CrawlSpeed = crawlSpeed,
                    PushPullSpeed = pushPullSpeed,
                    Acceleration = acceleration,
                    Deceleration = deceleration,
                    JumpForce = jumpForce,
                    MinJumpTime = minJumpTime,
                    Gravity = gravity,
                    FallGravityMultiplier = fallGravityMultiplier,
                    TerminalVelocity = terminalVelocity,
                    CoyoteTimeDuration = coyoteTimeDuration,
                    JumpBufferDuration = jumpBufferDuration
                };
            }

            if (postureHandler == null)
                postureHandler = new PlayerPostureHandler(charController, transform, standingHeight, crouchHeight, crawlHeight, obstacleMask);
            if (interactionHandler == null)
                interactionHandler = new PlayerInteractionHandler(transform, carrySocket, trajectoryPredictor, minThrowSpeed, maxThrowSpeed, throwChargeRate);
            if (ledgeClimber == null)
                ledgeClimber = new PlayerLedgeClimber(transform, charController, ledgeDetector, climbDuration, maxClimbForwardLean, autoClimbOnLedgeGrab);
            if (ropeHandler == null)
                ropeHandler = new PlayerRopeHandler(transform, charController);
            if (visualEffects == null)
            {
                visualEffects = new PlayerVisualEffects(transform, visualTransform)
                {
                    RunLeanAngle = runLeanAngle,
                    PushLeanAngle = pushLeanAngle,
                    PullLeanAngle = pullLeanAngle
                };
            }
        }

        private void Awake()
        {
            InitializeHandlers(); if (postureHandler != null) postureHandler.UpdatePostures(true, false, 0f, FacingDirection);
        }

        private void OnEnable()
        {
            if (charController != null && CurrentState != PlayerState.LedgeClimb && CurrentState != PlayerState.LedgeGrab)
            {
                charController.enabled = true;
            }
            if (hipBone == null) hipBone = transform.Find("Armature/BoneRoot/Hip");
        }

        private void OnDisable()
        {
            if (motor != null)
            {
                motor.Velocity = Vector3.zero;
            }
            var audioCtrl = GetComponent<PlayerAudioController>();
            if (audioCtrl != null)
            {
                audioCtrl.StopFootsteps(immediate: true);
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

        private void SetupTrajectoryPredictor()
        {
            if (trajectoryPredictor == null)
            {
                trajectoryPredictor = GetComponentInChildren<TrajectoryPredictor>();
                if (trajectoryPredictor == null && Application.isPlaying)
                {
                    GameObject trajGO = new GameObject("TrajectoryPredictor");
                    trajGO.transform.SetParent(transform);
                    trajGO.transform.localPosition = Vector3.zero;
                    trajectoryPredictor = trajGO.AddComponent<TrajectoryPredictor>();
                }
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // 1. Đọc Input
            EnsureInitialized();
            if (inputReader == null) return;
            inputReader.ReadInput();

            // 2. Cập nhật Timers
            ropeHandler.Tick(dt);
            motor.TickTimers(dt);
            if (inputReader.JumpPressed) motor.QueueJump();

            // 2.5 Bấm phím [F] để bám leo dây hoặc thả buông dây thừng
            if (inputReader.RopeGrabPressed)
            {
                if (CurrentState == PlayerState.RopeGrab || CurrentState == PlayerState.RopeClimb)
                {
                    // Đang leo dây mà bấm F: Thả tay rơi xuống tự do
                    DetachFromRope(Vector3.zero);
                }
                else if (CanAttachToRope)
                {
                    // Muốn bám dây: kiểm tra trigger đang chạm hoặc tìm dây gần nhất trong 2.0m
                    RopeController targetRope = nearbyRope;
                    int targetNode = nearbyRopeNodeIndex;

                    if (targetRope == null)
                    {
                        targetRope = FindNearbyRope(out targetNode);
                    }

                    if (targetRope != null && targetNode >= 0)
                    {
                        targetRope.OnPlayerTouchSegment(this, targetNode, motor.Velocity);
                    }
                }
            }

            // 3. Phân luồng máy trạng thái (State Dispatcher)
            switch (CurrentState)
            {
                case PlayerState.Grounded:
                case PlayerState.Airborne:
                case PlayerState.Carrying:
                    HandleLocomotion(dt);
                    interactionHandler.CheckInteractions(
                        this,
                        inputReader.InteractPressed,
                        inputReader.GrabCrateHeld,
                        inputReader.MouseLeftPressed,
                        inputReader.MouseRightPressed);
                    break;

                case PlayerState.AimingThrow:
                    // Dừng chân tập trung canh góc & lực ném
                    interactionHandler.UpdateAimingThrow(
                        this,
                        inputReader.MousePosition,
                        inputReader.MouseLeftReleasedThisFrame,
                        inputReader.MouseRightPressed || inputReader.InteractPressed,
                        dt);
                    break;

                case PlayerState.PushPull:
                    interactionHandler.UpdatePushPull(this, inputReader.Horizontal, inputReader.GrabCrateHeld, inputReader.JumpPressed, dt);
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

            // 4. Cập nhật hiệu ứng thị giác cơ học
            if (ropeHandler != null && (CurrentState == PlayerState.RopeGrab || CurrentState == PlayerState.RopeClimb) && ropeHandler.CurrentRope != null)
            {
                visualEffects.RopeAngle = ropeHandler.CurrentRope.CurrentAngleDegrees;
                visualEffects.HandOffsetHeight = ropeHandler.HandOffsetHeight;
                // Planet of Lana dynamic swing lag: thân người có độ trễ quán tính theo vận tốc góc con lắc
                float angVel = ropeHandler.CurrentRope.AngularVelocity;
                visualEffects.SwingLagAngle = -Mathf.Clamp(angVel * 3.5f, -14f, 14f);
            }
            else
            {
                visualEffects.RopeAngle = 0f;
                visualEffects.SwingLagAngle = 0f;
            }

            visualEffects.UpdatePostures(
                CurrentState,
                FacingDirection,
                inputReader.Horizontal,
                IsSprinting,
                IsCrouching,
                IsCrawling,
                postureHandler.StandingHeight,
                postureHandler.CrouchHeight,
                postureHandler.CrawlHeight,
                dt);
        }

        private void HandleLocomotion(float dt)
        {
            bool grounded = motor.IsGrounded;

            // Cập nhật hướng mặt
            if (Mathf.Abs(inputReader.Horizontal) > 0.08f)
            {
                FacingDirection = Mathf.Sign(inputReader.Horizontal);
                visualEffects.UpdateFacing(FacingDirection);
                interactionHandler.UpdateCarrySocketTransform(FacingDirection);
            }

            // Trọng lực & Nhảy
            motor.ApplyGravity(grounded, inputReader.JumpHeld, dt);

            if (grounded)
            {
                if (CurrentState == PlayerState.Airborne)
                {
                    visualEffects.SetSquashStretch(0.76f); // Landing squash
                    CurrentState = interactionHandler.IsCarrying ? PlayerState.Carrying : PlayerState.Grounded;

                    // Kích hoạt animation tiếp đất mượt mà sau khi rơi
                    if (lastFallVelocityY < -2.5f || airTime > 0.16f)
                    {
                        var anAnim = GetComponent<AnAnimationController>();
                        if (anAnim != null) anAnim.OnLanded(lastFallVelocityY);
                    }
                    airTime = 0f;
                    lastFallVelocityY = 0f;
                }
            }
            else
            {
                CurrentState = PlayerState.Airborne;
                airTime += dt;
                lastFallVelocityY = motor.Velocity.y;

                // Kiểm tra bám mép vách khi nhảy hoặc rơi
                if (ledgeClimber != null && motor.Velocity.y < 3.5f && !interactionHandler.IsCarrying)
                {
                    if (ledgeClimber.TryDetectLedge(FacingDirection, motor.Velocity.y, out Vector3 grabPos, out Vector3 landPos))
                    {
                        StartLedgeGrab(grabPos, landPos);
                        return;
                    }
                }

                // Tự động bám dây thừng khi nhảy qua hoặc chạm dây trong không trung (chuẩn Planet of Lana)
                if (CanAttachToRope && !interactionHandler.IsCarrying)
                {
                    var rope = FindNearbyRope(out int targetNode);
                    if (rope != null && targetNode >= 0)
                    {
                        rope.OnPlayerTouchSegment(this, targetNode, motor.Velocity);
                        return;
                    }
                }
            }

            // Đánh giá tư thế Ngồi & Bò
            postureHandler.UpdatePostures(grounded, inputReader.CrouchPressed, inputReader.Horizontal, FacingDirection);

            // Bật nhảy (Coyote Time + Jump Buffer + Running Momentum Leap)
            if (motor.TryExecuteJump(IsCrawling, IsSprinting, inputReader.Horizontal, out bool jumped))
            {
                visualEffects.SetSquashStretch(1.25f); // Launch stretch
                CurrentState = PlayerState.Airborne;
                airTime = 0.05f;
            }

            // Tính toán tốc độ di chuyển
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
            else if (inputReader.SprintHeld && Mathf.Abs(inputReader.Horizontal) > 0.1f && !interactionHandler.IsCarrying)
            {
                currentSpeed = sprintSpeed;
                IsSprinting = true;
            }
            else
            {
                currentSpeed = walkSpeed;
                IsSprinting = false;
            }

            motor.ApplyHorizontalMovement(inputReader.Horizontal, currentSpeed, dt);
        }

        private void StartLedgeGrab(Vector3 grabPos, Vector3 landPos)
        {
            CurrentState = PlayerState.LedgeGrab;
            motor.Velocity = Vector3.zero;
            airTime = 0f;
            lastFallVelocityY = 0f;
            ledgeClimber.PrepareLedgeGrab(grabPos, landPos, FacingDirection);

            var anAnim = GetComponent<AnAnimationController>();
            if (anAnim != null) anAnim.OnLedgeHangStarted();

            if (autoClimbOnLedgeGrab)
            {
                StartClimbUp();
            }
        }

        private void StartClimbUp()
        {
            CurrentState = PlayerState.LedgeClimb;
            ledgeClimber.StartClimbing();
        }

        private void HandleLedgeGrab()
        {
            bool pushingAway = (inputReader.Horizontal != 0 && Mathf.Sign(inputReader.Horizontal) != FacingDirection);

            // Nhảy đạp tường phóng ngược ra sau
            if (inputReader.JumpPressed && pushingAway)
            {
                if (charController != null) charController.enabled = true;
                CurrentState = PlayerState.Airborne;
                motor.Velocity = new Vector3(-FacingDirection * walkSpeed * 1.15f, jumpForce * 0.90f, 0f);
                SetFacingDirection(-FacingDirection);
                var anAnim = GetComponent<AnAnimationController>();
                if (anAnim != null) anAnim.OnClimbCancelled();
                return;
            }

            // Người chơi chủ động bấm phím [W] hoặc [Mũi tên lên] (hoặc phím nhảy) để leo lên đỉnh vách đá
            bool wantsClimb = inputReader.Vertical > 0.2f || (inputReader.JumpPressed && !pushingAway);
            if (wantsClimb)
            {
                StartClimbUp();
                return;
            }

            // Bấm [S] hoặc [Mũi tên xuống] (hoặc đẩy ra xa) để buông tay rơi xuống
            if (inputReader.Vertical < -0.3f || pushingAway)
            {
                if (charController != null) charController.enabled = true;
                CurrentState = PlayerState.Airborne;
                motor.Velocity = new Vector3(-FacingDirection * 1.0f, -2.0f, 0f);
                var anAnim = GetComponent<AnAnimationController>();
                if (anAnim != null) anAnim.OnClimbCancelled();
                return;
            }
        }

        private void HandleLedgeClimb(float dt)
        {
            bool movingForward = Mathf.Abs(inputReader.Horizontal) > 0.1f &&
                                 Mathf.Sign(inputReader.Horizontal) == FacingDirection;
            IsSprinting = movingForward && inputReader.SprintHeld && !interactionHandler.IsCarrying;

            bool finished = ledgeClimber.UpdateClimb(
                dt,
                FacingDirection,
                visualTransform,
                visualEffects.BaseVisualScaleX,
                visualEffects.BaseVisualScaleY,
                visualEffects.BaseVisualScaleZ,
                movingForward);

            if (finished)
            {
                postureHandler.UpdatePostures(true, inputReader.CrouchPressed, inputReader.Horizontal, FacingDirection);

                float carrySpeed = 0f;
                if (movingForward && !IsCrawling)
                {
                    float targetLocoSpeed = IsCrouching ? crouchSpeed : (IsSprinting ? sprintSpeed : walkSpeed);
                    carrySpeed = Mathf.Clamp(
                        Mathf.Max(ledgeClimber.ExitHorizontalSpeed, targetLocoSpeed * 0.72f),
                        0f,
                        targetLocoSpeed);
                }

                // Giữ lực ghì mặt sàn (-4.5f) và nối tiếp quán tính ngang mượt mà thay vì phanh khựng về 0
                motor.Velocity = new Vector3(FacingDirection * carrySpeed, -4.5f, 0f);
                motor.CoyoteTimer = coyoteTimeDuration;
                airTime = 0f;
                lastFallVelocityY = 0f;
                CurrentState = PlayerState.Grounded;
                visualEffects.ResetScale();
            }
        }

        public void AttachToRope(RopeController rope, Vector3 attachPos, Vector3 incomingVelocity = default)
        {
            if (ropeHandler.AttachToRope(this, rope, attachPos, incomingVelocity))
            {
                CurrentState = PlayerState.RopeGrab;
                motor.Velocity = Vector3.zero;
            }
        }

        private void HandleRopeMovement(float dt)
        {
            bool stillOnRope = ropeHandler.UpdateRopeMovement(
                this,
                inputReader.Horizontal,
                inputReader.Vertical,
                inputReader.JumpPressed,
                dt,
                out Vector3 releaseVelocity);

            if (stillOnRope)
            {
                CurrentState = (Mathf.Abs(inputReader.Vertical) > 0.1f) ? PlayerState.RopeClimb : PlayerState.RopeGrab;
                if (Mathf.Abs(inputReader.Horizontal) > 0.1f)
                {
                    SetFacingDirection(Mathf.Sign(inputReader.Horizontal));
                }
            }
            else
            {
                CurrentState = PlayerState.Airborne;
                motor.Velocity = releaseVelocity;
            }
        }

        private RopeController nearbyRope;
        private int nearbyRopeNodeIndex = -1;

        public void RegisterNearbyRope(RopeController rope, int nodeIndex)
        {
            nearbyRope = rope;
            nearbyRopeNodeIndex = nodeIndex;
        }

        public void UnregisterNearbyRope(RopeController rope)
        {
            if (nearbyRope == rope)
            {
                nearbyRope = null;
                nearbyRopeNodeIndex = -1;
            }
        }

        public RopeController FindNearbyRope(out int nearestNodeIndex)
        {
            nearestNodeIndex = -1;
            var ropes = Object.FindObjectsByType<RopeController>(FindObjectsInactive.Exclude);
            RopeController bestRope = null;
            float bestDistSq = 2.0f * 2.0f; // Bán kính tìm kiếm 2.0 mét

            Vector3 playerCenter = transform.position + Vector3.up * 1.0f;

            foreach (var rope in ropes)
            {
                if (rope == null) continue;
                int nodeIdx = rope.GetNearestNodeIndex(playerCenter, out float dist);
                if (dist <= 2.0f && (dist * dist) < bestDistSq)
                {
                    bestDistSq = dist * dist;
                    bestRope = rope;
                    nearestNodeIndex = nodeIdx;
                }
            }

            return bestRope;
        }

        public void DetachFromRope(Vector3 releaseVelocity)
        {
            ropeHandler.DetachFromRope(this, releaseVelocity);
            CurrentState = PlayerState.Airborne;
            motor.Velocity = releaseVelocity;
        }

        public void Teleport(Vector3 newPos)
        {
            motor.Teleport(newPos);
        }

        public void RespawnAt(Vector3 checkpointPos)
        {
            if (ropeHandler.CurrentRope != null)
            {
                ropeHandler.DetachFromRope(this, Vector3.zero);
            }
            Teleport(checkpointPos);
            CurrentState = PlayerState.Grounded;
            ropeHandler.SetCooldown(0.5f);
            visualEffects.ResetScale();
        }

        [Header("Debug & HUD")]
        [SerializeField] private bool showDebugHUD = false;

        private void OnGUI()
        {
            if (!Application.isPlaying) return;

            bool f1Pressed = false;
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.f1Key.wasPressedThisFrame) f1Pressed = true;
#else
            if (Input.GetKeyDown(KeyCode.F1)) f1Pressed = true;
#endif
            if (f1Pressed)
            {
                showDebugHUD = !showDebugHUD;
            }

            if (!showDebugHUD) return;

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
            string ropeHint = (nearbyRope != null && CurrentState != PlayerState.RopeGrab && CurrentState != PlayerState.RopeClimb) ? " | <color=#fb923c><b>[F] Leo dây</b></color>" : "";
            GUI.Label(new Rect(26f, 36f, bannerW - 20f, 18f), $"Tốc độ: {Velocity.x:F1} m/s | Hướng: {(FacingDirection > 0 ? "Phải" : "Trái")}{ropeHint}", subStyle);
        }
    }
}
