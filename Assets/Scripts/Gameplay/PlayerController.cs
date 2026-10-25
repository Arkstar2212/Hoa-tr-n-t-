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
        [SerializeField] private float crouchSpeed = 1.8f;
        [SerializeField] private float crawlSpeed = 1.2f;
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
        [SerializeField] private float climbDuration = 1.15f;
        [SerializeField] private float maxClimbForwardLean = 24f;
        [SerializeField] private bool autoClimbOnLedgeGrab = false;

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
        private float ledgeGrabTimer = 0f;
        private float ledgeGrabCooldown = 0f;

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
        public PlayerLedgeClimber LedgeClimber { get { EnsureInitialized(); return ledgeClimber; } }
        public PlayerRopeHandler RopeHandler { get { EnsureInitialized(); return ropeHandler; } }
        public PlayerVisualEffects VisualEffects { get { EnsureInitialized(); return visualEffects; } }

        /// <summary>
        /// Cung cấp vị trí camera tập trung chính xác (Theo dõi hông nhân vật khi leo vách)
        /// </summary>
        public Vector3 CameraTargetPosition
        {
            get
            {
                if (CurrentState == PlayerState.LedgeClimb || CurrentState == PlayerState.LedgeGrab)
                {
                    if (hipBone == null) hipBone = transform.Find("Armature/BoneRoot/Hip");
                    if (hipBone != null)
                    {
                        return new Vector3(hipBone.position.x, hipBone.position.y - 0.9f, 0f);
                    }
                    else if (ledgeClimber != null)
                    {
                        float t = Mathf.Clamp01(ledgeClimber.ClimbProgress);
                        return Vector3.Lerp(ledgeClimber.ClimbStartPos, ledgeClimber.ClimbEndPos, t);
                    }
                }
                return transform.position;
            }
        }

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
                InitializeHandlers();
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
            InitializeHandlers();
        }

        private void OnEnable()
        {
            if (charController != null && CurrentState != PlayerState.LedgeClimb && CurrentState != PlayerState.LedgeGrab)
            {
                charController.enabled = true;
            }
            if (hipBone == null) hipBone = transform.Find("Armature/BoneRoot/Hip");
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
                if (trajectoryPredictor == null)
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
            inputReader.ReadInput();

            // 2. Cập nhật Timers
            ropeHandler.Tick(dt);
            motor.TickTimers(dt);
            if (ledgeGrabCooldown > 0f) ledgeGrabCooldown -= dt;
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
                        targetRope.OnPlayerTouchSegment(this, targetNode);
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
                    HandleLedgeGrab(dt);
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
                }
            }
            else
            {
                CurrentState = PlayerState.Airborne;

                // Kiểm tra bám mép vách khi nhảy hoặc rơi gần mép đá
                if (ledgeClimber != null && ledgeGrabCooldown <= 0f && motor.Velocity.y < 3.5f && !interactionHandler.IsCarrying)
                {
                    if (ledgeClimber.TryDetectLedge(FacingDirection, motor.Velocity.y, out Vector3 grabPos, out Vector3 landPos))
                    {
                        StartLedgeGrab(grabPos, landPos);
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
            ledgeGrabTimer = 0f;
            motor.Velocity = Vector3.zero;
            ledgeClimber.StartLedgeGrab(grabPos, landPos, FacingDirection);
        }

        private void HandleLedgeGrab(float dt)
        {
            ledgeGrabTimer += dt;
            bool pushingAway = (inputReader.Horizontal != 0 && Mathf.Sign(inputReader.Horizontal) != FacingDirection);

            // Bấm Nhảy + Hướng ngược lại: Bật nhảy rời khỏi vách đá
            if (inputReader.JumpPressed && pushingAway)
            {
                ledgeGrabCooldown = 0.35f;
                if (charController != null) charController.enabled = true;
                CurrentState = PlayerState.Airborne;
                motor.Velocity = new Vector3(-FacingDirection * walkSpeed * 0.85f, jumpForce * 0.85f, 0f);
                SetFacingDirection(-FacingDirection);
                return;
            }

            // Chờ ít nhất một nhịp ngắn (0.18s) ở tư thế bám vách (LedgeHang) để thấy rõ động tác tay bám gờ,
            // sau đó bấm W / Up / Space (hoặc giữ hướng tiến vào vách sau 0.32s, hoặc khi bật autoClimbOnLedgeGrab) sẽ đồng bộ leo lên (Climb)
            bool explicitClimbInput = (ledgeGrabTimer >= 0.12f) && (inputReader.Vertical > 0.2f || inputReader.JumpPressed);
            bool holdForwardClimb = (ledgeGrabTimer >= 0.32f) && (inputReader.Horizontal != 0 && Mathf.Sign(inputReader.Horizontal) == FacingDirection);
            bool autoClimbReady = autoClimbOnLedgeGrab && (ledgeGrabTimer >= 0.28f);

            if (explicitClimbInput || holdForwardClimb || autoClimbReady)
            {
                CurrentState = PlayerState.LedgeClimb;
                ledgeClimber.BeginClimbFromGrab();
                return;
            }

            // Bấm S / Down hoặc thả tay hướng ra ngoài (sau 0.20s): Buông tay rơi xuống
            if (ledgeGrabTimer >= 0.20f && (inputReader.Vertical < -0.3f || pushingAway))
            {
                ledgeGrabCooldown = 0.40f;
                if (charController != null) charController.enabled = true;
                CurrentState = PlayerState.Airborne;
                motor.Velocity = new Vector3(-FacingDirection * 1.2f, -2.0f, 0f);
                return;
            }
        }

        private void HandleLedgeClimb(float dt)
        {
            bool finished = ledgeClimber.UpdateClimb(
                dt,
                FacingDirection,
                visualTransform,
                visualEffects.BaseVisualScaleX,
                visualEffects.BaseVisualScaleY,
                visualEffects.BaseVisualScaleZ);

            if (finished)
            {
                motor.Velocity = Vector3.zero;
                CurrentState = PlayerState.Grounded;
                visualEffects.ResetScale();
                var anAnim = GetComponent<AnAnimationController>();
                if (anAnim != null) anAnim.OnClimbFinished();
            }
        }

        public void AttachToRope(RopeController rope, Vector3 attachPos)
        {
            if (ropeHandler.AttachToRope(this, rope, attachPos))
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

            if (Input.GetKeyDown(KeyCode.F1))
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
            else if (IsCrouching) stateLabel = (Mathf.Abs(Velocity.x) > 0.15f) ? "NGỒI ĐI TIẾP [CROUCH WALK]" : "NGỒI XỔM [CROUCH IDLE]";
            else if (CurrentState == PlayerState.PushPull) { stateLabel = "KÉO / ĐẨY THÙNG [PUSH-PULL]"; stateColor = "#f59e0b"; }
            else if (CurrentState == PlayerState.AimingThrow) { stateLabel = "NGẮM NÉM [AIM & THROW]"; stateColor = "#38bdf8"; }
            else if (CurrentState == PlayerState.Carrying) { stateLabel = "ĐANG BƯNG ĐỒ [CARRY]"; stateColor = "#a78bfa"; }
            else if (CurrentState == PlayerState.RopeGrab || CurrentState == PlayerState.RopeClimb) { stateLabel = "LEO DÂY [ROPE]"; stateColor = "#fb923c"; }
            else if (CurrentState == PlayerState.LedgeGrab) { stateLabel = "BÁM VÁCH ĐÁ [LEDGE HANG]"; stateColor = "#f472b6"; }
            else if (CurrentState == PlayerState.LedgeClimb) { stateLabel = "TRÈO LÊN VÁCH [LEDGE CLIMB]"; stateColor = "#ec4899"; }
            else if (CurrentState == PlayerState.Airborne) { stateLabel = "NHẢY / TRÊN KHÔNG [AIR]"; stateColor = "#60a5fa"; }
            else { stateLabel = "ĐI BỘ [WALK]"; }

            GUI.Label(new Rect(26f, 18f, bannerW - 20f, 24f), $"<color={stateColor}>● {stateLabel}</color>", stateStyle);

            GUIStyle subStyle = new GUIStyle(GUI.skin.label) { fontSize = 10, normal = { textColor = new Color(0.8f, 0.85f, 0.9f) } };
            string ropeHint = (nearbyRope != null && CurrentState != PlayerState.RopeGrab && CurrentState != PlayerState.RopeClimb) ? " | <color=#fb923c><b>[F] Leo dây</b></color>" : "";
            GUI.Label(new Rect(26f, 36f, bannerW - 20f, 18f), $"Tốc độ: {Velocity.x:F1} m/s | Hướng: {(FacingDirection > 0 ? "Phải" : "Trái")}{ropeHint}", subStyle);

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
                "• <b>[A] / [D]</b>: Đi bộ trái / phải (nhích nhẹ = đi chậm)\n" +
                "• <b>[S] / [C] / [Ctrl]</b>: Ngồi xổm (Crouch) | <b>+ [A]/[D]</b>: Ngồi tiến về trước (Crouch Walk)\n" +
                "• <b>Nhảy sát mép vách đá</b>: Tự động <b>Bám trên vách đá (Ledge Hang)</b> | <b>[W]/[Space]</b>: <b>Leo lên</b>\n" +
                "• <b>[Left Shift]</b>: Chạy nước rút (Sprint) tốc độ tự nhiên\n" +
                "• <b>[Space]</b>: Nhảy | <b>Đặc biệt: Chạy lấy đà + [Space]</b> phóng xa vượt trội theo chiều ngang!\n" +
                "• <b>[Giữ Chuột Trái] cạnh thùng + [A]/[D]</b>: <b>Kéo / Đẩy thùng gỗ</b> áp sát vách leo lên\n" +
                "• <b>[E]</b>: <b>Nhặt đồ / Nhặt bóng</b> | Bấm [E] lần nữa hoặc [Chuột Phải] để đặt nhẹ xuống\n" +
                "• <b>Cầm bóng → Giữ [Chuột Trái]</b>: <b>Canh góc & Lực ném</b> (rê chuột chỉnh tầm xa/gần, vệt Parabol)\n" +
                "• <b>Thả [Chuột Trái]</b>: <b>Ném bóng</b> theo đúng quỹ đạo đã canh!\n" +
                "• <b>Dây thừng</b>: Đến gần/nhảy đến bấm <b>[F]</b> để bám | <b>[W]/[S]</b> trèo | <b>[A]/[D]</b> đu đưa | <b>[Space]</b> nhảy phóng";

            GUI.Label(new Rect(px + 12f, py + 26f, panelW - 24f, panelH - 30f), controls, guideText);
        }
    }
}
