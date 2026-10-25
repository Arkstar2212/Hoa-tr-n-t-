using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HoaVaMau.Gameplay
{
    public enum AnAnimationMode
    {
        Auto = 0,
        ForceWalk = 1,
        ForceRun = 2,
        ForceClimb = 3,
        ForceCrouch = 4,
        ForceCrawl = 5,
        ForceCrouchWalk = 6,
        ForceLedgeHang = 7
    }

    /// <summary>
    /// Component điều khiển Animation mượt mà và trực quan cho nhân vật An (Nhân vật chính):
    /// - Sử dụng Avatar và Animation chính thức từ thư viện dự án (Assets/Animations/Hero).
    /// - Phân tách rõ:
    ///     * Ngồi tại chỗ (Crouch -> Hero_Stealth_Crouch_Idle_Loop)
    ///     * Ngồi tiến về trước (CrouchWalk -> Hero_Stealth_Crouch_Walk_Fwd_IP)
    ///     * Bám trên vách đá (LedgeHang -> Hero_Traverse_Hanging_Idle_Loop)
    ///     * Đồng bộ Bám vách đá (LedgeHang) -> Leo lên (Climb) khớp vị trí bàn tay và quỹ đạo cơ học.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Animator))]
    public class AnAnimationController : MonoBehaviour
    {
        [Header("Chế độ Animation")]
        [Tooltip("Auto: tự động theo di chuyển / phím bấm gameplay. Force*: ép xem động tác")]
        [SerializeField] private AnAnimationMode mode = AnAnimationMode.Auto;

        [Header("Tùy chọn phím tắt test nhanh (Play Mode)")]
        [Tooltip("Bấm 1 (Walk), 2 (Run), 3 (Climb), 4 (Crouch), 5 (Crawl), 6 (LedgeHang), 7 (CrouchWalk), 0 (Auto)")]
        [SerializeField] private bool enableKeyShortcuts = true;

        [Header("Hiển thị giao diện điều khiển trên màn hình (OnScreen GUI)")]
        [SerializeField] private bool showOnScreenGui = true;

        [Header("Xoay mặt mượt mà")]
        [SerializeField] private bool smoothFacing = true;
        [SerializeField] private float turnSpeed = 720f;

        private enum ThrowAnimPhase
        {
            None = 0,
            Pickup = 1,
            AimWindup = 2,
            Release = 3
        }

        private const float ThrowPickupStartNorm = 0.00f;
        private const float ThrowPickupGrabNorm = 0.21f;
        private const float ThrowPickupEndNorm = 0.37f;
        private const float ThrowAimHoldNorm = 0.45f;
        private const float ThrowReleaseEndNorm = 0.88f;

        private const float ClimbHangStartNorm = 0.0f;
        private const float ClimbStandEndNorm = 0.985f;

        private Animator animator;
        private CharacterController characterController;
        private PlayerController playerController;
        private Transform hipsBone;
        private Transform spineBone;
        private Transform chestBone;
        private Transform upperChestBone;
        private Transform neckBone;
        private Transform headBone;
        private Transform leftUpperLegBone;
        private Transform leftLowerLegBone;
        private Transform leftFootBone;
        private Transform rightUpperLegBone;
        private Transform rightLowerLegBone;
        private Transform rightFootBone;
        private Transform leftUpperArmBone;
        private Transform leftLowerArmBone;
        private Transform leftHandBone;
        private Transform rightUpperArmBone;
        private Transform rightLowerArmBone;
        private Transform rightHandBone;

        private Transform activeCarrySocket;
        private Vector3 pickupGroundWorldPos;
        private ThrowAnimPhase throwPhase = ThrowAnimPhase.None;
        private float throwNormTime = 0f;
        private float standingHipOffsetY = 0.63f;
        private float lastHangHipWorldY = 0f;
        private bool hasValidHangHipY = false;
        private float forceClimbPreviewTimer = 0f;
        private float currentSmoothSpeed = 0f;
        private float speedVelocity = 0f;
        private float targetFacingAngle = 90f;
        private string currentPlayingState = "Idle";

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsClimbingHash = Animator.StringToHash("IsClimbing");
        private static readonly int IsLedgeHangingHash = Animator.StringToHash("IsLedgeHanging");
        private static readonly int IsCrouchingHash = Animator.StringToHash("IsCrouching");
        private static readonly int IsCrawlingHash = Animator.StringToHash("IsCrawling");

        public AnAnimationMode CurrentMode => mode;
        public AnAnimationMode Mode { get => mode; set => mode = value; }
        public void SetMode(AnAnimationMode newMode) { mode = newMode; }
        public string ActiveStateName => currentPlayingState;

        private void Awake()
        {
            SetupComponents();
        }

        private void OnEnable()
        {
            SetupComponents();
            if (animator != null) animator.speed = 1f;
        }

        private void Start()
        {
            SetupComponents();
            if (animator != null) animator.speed = 1f;
            targetFacingAngle = transform.eulerAngles.y;
            standingHipOffsetY = 0.63f * Mathf.Max(0.1f, transform.lossyScale.y);
        }

        private void Update()
        {
            if (animator == null)
            {
                SetupComponents();
                if (animator == null) return;
            }

            if (animator.applyRootMotion) animator.applyRootMotion = false;

            if (!Application.isPlaying)
            {
                if (mode != AnAnimationMode.Auto)
                {
                    UpdateAnimationLogic();
                }
                return;
            }

            if (enableKeyShortcuts)
            {
                HandleInputShortcuts();
            }

            UpdateFacingRotation();
            UpdateAnimationLogic();
        }

        private void OnAnimatorMove()
        {
        }

        public void ManualLateUpdate() => LateUpdate();

        private void LateUpdate()
        {
            if (animator == null) return;

            CacheHumanBones();

            // Mecanim hoàn toàn làm chủ tư thế nhân vật (Crouch, Walk, Run, Idle, Jump).
            // Tuyệt đối không can thiệp thủ công vào xương hông hay góc xoay xương khi Crouch.

            if (hipsBone != null)
            {
                // Khi Bám trên vách đá (LedgeHang): Khóa bàn tay bám chuẩn xác vào mép gờ vách
                if (currentPlayingState == "LedgeHang")
                {
                    if (playerController != null && playerController.LedgeClimber != null &&
                        playerController.CurrentState == PlayerState.LedgeGrab)
                    {
                        Vector3 hipPos = hipsBone.position;
                        float facing = playerController.FacingDirection;
                        float wallX = playerController.LedgeClimber.LedgeWallX;
                        float topY = playerController.LedgeClimber.LedgeTopY;

                        if (leftHandBone != null && rightHandBone != null)
                        {
                            float avgHandY = 0.5f * (leftHandBone.position.y + rightHandBone.position.y);
                            float hipToHandY = Mathf.Max(0.35f, avgHandY - hipPos.y);
                            hipPos.y = topY - 0.03f - hipToHandY;
                        }
                        else
                        {
                            hipPos.y = topY - 0.62f;
                        }

                        hipPos.x = wallX - facing * 0.24f;
                        lastHangHipWorldY = hipPos.y;
                        hasValidHangHipY = true;
                        hipsBone.position = hipPos;
                    }
                }
                // Khi Leo lên vách đá (Climb): Đồng bộ vị trí Hips chuyển tiếp từ vị trí LedgeHang lên sàn đứng
                else if (currentPlayingState == "Climb")
                {
                    if (playerController != null && playerController.LedgeClimber != null &&
                        playerController.CurrentState == PlayerState.LedgeClimb)
                    {
                        Vector3 hipPos = hipsBone.position;
                        float climbT = Mathf.Clamp01(playerController.LedgeClimber.ClimbProgress);
                        float facing = playerController.FacingDirection;
                        float wallX = playerController.LedgeClimber.LedgeWallX;
                        float topY = playerController.LedgeClimber.LedgeTopY;
                        Vector3 endPos = playerController.LedgeClimber.ClimbEndPos;

                        float startHipY = hasValidHangHipY ? lastHangHipWorldY : (topY - 0.58f);
                        float edgeMantleHipY = topY + standingHipOffsetY * 0.55f;
                        float finalStandHipY = topY + standingHipOffsetY;

                        if (climbT <= 0.45f)
                        {
                            float subT = climbT / 0.45f;
                            float easeY = Mathf.SmoothStep(0f, 1f, subT);
                            hipPos.y = Mathf.Lerp(startHipY, edgeMantleHipY, easeY);
                            hipPos.x = Mathf.Lerp(wallX - facing * 0.24f, wallX - facing * 0.08f, subT * subT);
                        }
                        else
                        {
                            float subT = (climbT - 0.45f) / 0.55f;
                            float ease = Mathf.SmoothStep(0f, 1f, subT);
                            hipPos.y = Mathf.Lerp(edgeMantleHipY, finalStandHipY, ease);
                            hipPos.x = Mathf.Lerp(wallX - facing * 0.08f, endPos.x, ease);
                        }
                        hipsBone.position = hipPos;
                    }
                }
            }

            // Đồng bộ vị trí vật cầm trên tay theo từng pha Nhặt (Pickup) và Chuẩn bị ném (AimWindup)
            if (activeCarrySocket != null)
            {
                float facing = (playerController != null) ? playerController.FacingDirection : 1f;
                Vector3 defaultCarryWorldPos = transform.position + new Vector3(facing * 0.44f, 1.05f, 0f);

                if (throwPhase == ThrowAnimPhase.Pickup)
                {
                    if (throwNormTime < ThrowPickupGrabNorm)
                    {
                        activeCarrySocket.position = new Vector3(pickupGroundWorldPos.x, pickupGroundWorldPos.y, 0f);
                    }
                    else
                    {
                        float liftT = Mathf.Clamp01((throwNormTime - ThrowPickupGrabNorm) / (ThrowPickupEndNorm - ThrowPickupGrabNorm));
                        Vector3 handPos = (rightHandBone != null)
                            ? new Vector3(rightHandBone.position.x, rightHandBone.position.y, 0f)
                            : Vector3.Lerp(pickupGroundWorldPos, defaultCarryWorldPos, liftT);
                        activeCarrySocket.position = Vector3.Lerp(handPos, defaultCarryWorldPos, liftT * liftT);
                    }
                }
                else if (throwPhase == ThrowAnimPhase.AimWindup && rightHandBone != null)
                {
                    activeCarrySocket.position = new Vector3(rightHandBone.position.x, rightHandBone.position.y + 0.05f, 0f);
                }
            }
        }

        private void CacheHumanBones()
        {
            if (animator == null || !animator.isHuman) return;
            if (hipsBone == null) hipsBone = animator.GetBoneTransform(HumanBodyBones.Hips);
            if (spineBone == null) spineBone = animator.GetBoneTransform(HumanBodyBones.Spine);
            if (chestBone == null) chestBone = animator.GetBoneTransform(HumanBodyBones.Chest);
            if (upperChestBone == null) upperChestBone = animator.GetBoneTransform(HumanBodyBones.UpperChest);
            if (neckBone == null) neckBone = animator.GetBoneTransform(HumanBodyBones.Neck);
            if (headBone == null) headBone = animator.GetBoneTransform(HumanBodyBones.Head);

            if (leftUpperLegBone == null) leftUpperLegBone = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            if (leftLowerLegBone == null) leftLowerLegBone = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            if (leftFootBone == null) leftFootBone = animator.GetBoneTransform(HumanBodyBones.LeftFoot);

            if (rightUpperLegBone == null) rightUpperLegBone = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            if (rightLowerLegBone == null) rightLowerLegBone = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
            if (rightFootBone == null) rightFootBone = animator.GetBoneTransform(HumanBodyBones.RightFoot);

            if (leftUpperArmBone == null) leftUpperArmBone = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            if (leftLowerArmBone == null) leftLowerArmBone = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            if (leftHandBone == null) leftHandBone = animator.GetBoneTransform(HumanBodyBones.LeftHand);

            if (rightUpperArmBone == null) rightUpperArmBone = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            if (rightLowerArmBone == null) rightLowerArmBone = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            if (rightHandBone == null) rightHandBone = animator.GetBoneTransform(HumanBodyBones.RightHand);
        }

        public void SetupComponents()
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (characterController == null) characterController = GetComponent<CharacterController>();
            if (playerController == null) playerController = GetComponent<PlayerController>();

            if (animator != null)
            {
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.applyRootMotion = false;
                CacheHumanBones();
            }

            var smrList = GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var smr in smrList)
            {
                smr.updateWhenOffscreen = true;
            }

#if UNITY_EDITOR
            if (animator != null)
            {
                string controllerPath = "Assets/Animations/Controllers/AC_Player_An.controller";
                var ctrl = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
                if (ctrl != null && animator.runtimeAnimatorController != ctrl)
                {
                    animator.runtimeAnimatorController = ctrl;
                    UnityEditor.EditorUtility.SetDirty(animator);
                }

                if (animator.avatar == null)
                {
                    string fbxPath = "Assets/Models/Characters/Character_An.fbx";
                    var subAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(fbxPath);
                    foreach (var a in subAssets)
                    {
                        if (a is Avatar av && av.isValid)
                        {
                            animator.avatar = av;
                            UnityEditor.EditorUtility.SetDirty(animator);
                            break;
                        }
                    }
                }
            }
#endif
        }

        private void HandleInputShortcuts()
        {
            bool key1 = false;
            bool key2 = false;
            bool key3 = false;
            bool key4 = false;
            bool key5 = false;
            bool key6 = false;
            bool key7 = false;
            bool key0 = false;

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) key1 = true;
                if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) key2 = true;
                if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) key3 = true;
                if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame) key4 = true;
                if (kb.digit5Key.wasPressedThisFrame || kb.numpad5Key.wasPressedThisFrame) key5 = true;
                if (kb.digit6Key.wasPressedThisFrame || kb.numpad6Key.wasPressedThisFrame) key6 = true;
                if (kb.digit7Key.wasPressedThisFrame || kb.numpad7Key.wasPressedThisFrame) key7 = true;
                if (kb.digit0Key.wasPressedThisFrame || kb.numpad0Key.wasPressedThisFrame || kb.backspaceKey.wasPressedThisFrame) key0 = true;
            }
#else
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) key1 = true;
            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) key2 = true;
            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) key3 = true;
            if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) key4 = true;
            if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5)) key5 = true;
            if (Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6)) key6 = true;
            if (Input.GetKeyDown(KeyCode.Alpha7) || Input.GetKeyDown(KeyCode.Keypad7)) key7 = true;
            if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0) || Input.GetKeyDown(KeyCode.Backspace)) key0 = true;
#endif

            if (key1) ForceWalk();
            else if (key2) ForceRun();
            else if (key3) ForceClimb();
            else if (key4) ForceCrouch();
            else if (key5) ForceCrawl();
            else if (key6) ForceLedgeHang();
            else if (key7) ForceCrouchWalk();
            else if (key0) SetAutoMode();
        }

        private void UpdateFacingRotation()
        {
            if (playerController != null)
            {
                if (playerController.CurrentState == PlayerState.LedgeClimb || playerController.CurrentState == PlayerState.LedgeGrab)
                {
                    targetFacingAngle = (playerController.FacingDirection > 0) ? 90f : -90f;
                }
                else
                {
                    float h = playerController.HorizontalInput;
                    if (Mathf.Abs(h) > 0.08f)
                    {
                        targetFacingAngle = (h > 0) ? 90f : -90f;
                    }
                }
            }

            if (smoothFacing)
            {
                Quaternion targetRot = Quaternion.Euler(0f, targetFacingAngle, 0f);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
            }
        }

        private void UpdateAnimationLogic()
        {
            if (animator == null) return;

            float dt = Time.deltaTime;

            if (mode == AnAnimationMode.ForceWalk)
            {
                animator.speed = 1f;
                animator.SetBool(IsClimbingHash, false);
                animator.SetBool(IsLedgeHangingHash, false);
                animator.SetBool(IsCrouchingHash, false);
                animator.SetBool(IsCrawlingHash, false);
                animator.SetFloat(SpeedHash, 2.0f);
                CrossFadeToState("Walk", 0.15f);
                return;
            }
            if (mode == AnAnimationMode.ForceRun)
            {
                animator.speed = 1f;
                animator.SetBool(IsClimbingHash, false);
                animator.SetBool(IsLedgeHangingHash, false);
                animator.SetBool(IsCrouchingHash, false);
                animator.SetBool(IsCrawlingHash, false);
                animator.SetFloat(SpeedHash, 5.5f);
                CrossFadeToState("Run", 0.15f);
                return;
            }
            if (mode == AnAnimationMode.ForceLedgeHang)
            {
                animator.speed = 1f;
                animator.SetBool(IsClimbingHash, false);
                animator.SetBool(IsLedgeHangingHash, true);
                animator.SetBool(IsCrouchingHash, false);
                animator.SetBool(IsCrawlingHash, false);
                CrossFadeToState("LedgeHang", 0.12f);
                return;
            }
            if (mode == AnAnimationMode.ForceClimb)
            {
                animator.SetBool(IsClimbingHash, true);
                animator.SetBool(IsLedgeHangingHash, false);
                animator.SetBool(IsCrouchingHash, false);
                animator.SetBool(IsCrawlingHash, false);
                forceClimbPreviewTimer = (forceClimbPreviewTimer + dt / 1.15f) % 1.0f;
                float syncedNorm = Mathf.Lerp(ClimbHangStartNorm, ClimbStandEndNorm, forceClimbPreviewTimer);
                currentPlayingState = "Climb";
                animator.speed = 0f;
                animator.Play("Climb", 0, syncedNorm);
                return;
            }
            if (mode == AnAnimationMode.ForceCrouch)
            {
                animator.speed = 1f;
                animator.SetBool(IsClimbingHash, false);
                animator.SetBool(IsLedgeHangingHash, false);
                animator.SetBool(IsCrouchingHash, true);
                animator.SetBool(IsCrawlingHash, false);
                CrossFadeToState("Crouch", 0.15f);
                return;
            }
            if (mode == AnAnimationMode.ForceCrouchWalk)
            {
                animator.speed = 1f;
                animator.SetBool(IsClimbingHash, false);
                animator.SetBool(IsLedgeHangingHash, false);
                animator.SetBool(IsCrouchingHash, true);
                animator.SetBool(IsCrawlingHash, false);
                CrossFadeToState("CrouchWalk", 0.15f);
                return;
            }
            if (mode == AnAnimationMode.ForceCrawl)
            {
                animator.speed = 1f;
                animator.SetBool(IsClimbingHash, false);
                animator.SetBool(IsLedgeHangingHash, false);
                animator.SetBool(IsCrouchingHash, true);
                animator.SetBool(IsCrawlingHash, true);
                CrossFadeToState("Crawl", 0.12f);
                return;
            }

            // Chế độ Tự Động (Auto Gameplay)
            bool isLedgeHanging = false;
            bool isClimbing = false;
            bool isCrouching = false;
            bool isCrawling = false;
            float targetSpeed = 0f;

            if (playerController != null)
            {
                if (playerController.CurrentState == PlayerState.LedgeGrab)
                {
                    isLedgeHanging = true;
                }
                else if (playerController.CurrentState == PlayerState.LedgeClimb || 
                         playerController.CurrentState == PlayerState.RopeClimb ||
                         playerController.CurrentState == PlayerState.RopeGrab)
                {
                    isClimbing = true;
                }
                else
                {
                    isCrouching = playerController.IsCrouching;
                    isCrawling = playerController.IsCrawling;

                    float h = Mathf.Abs(playerController.HorizontalInput);
                    if (h > 0.08f)
                    {
                        if (isCrawling) targetSpeed = 1.2f;
                        else if (isCrouching) targetSpeed = 1.8f;
                        else if (playerController.IsSprinting) targetSpeed = 5.5f;
                        else targetSpeed = 2.2f;
                    }
                    else
                    {
                        targetSpeed = 0f;
                    }
                }
            }

            currentSmoothSpeed = Mathf.SmoothDamp(currentSmoothSpeed, targetSpeed, ref speedVelocity, 0.08f, Mathf.Infinity, dt);

            animator.SetBool(IsLedgeHangingHash, isLedgeHanging);
            animator.SetBool(IsClimbingHash, isClimbing);
            animator.SetBool(IsCrouchingHash, isCrouching);
            animator.SetBool(IsCrawlingHash, isCrawling);
            animator.SetFloat(SpeedHash, currentSmoothSpeed);

            if (isLedgeHanging)
            {
                throwPhase = ThrowAnimPhase.None;
                animator.speed = 1f;
                CrossFadeToState("LedgeHang", 0.10f);
                return;
            }

            if (isClimbing)
            {
                throwPhase = ThrowAnimPhase.None;

                if (playerController != null && playerController.CurrentState == PlayerState.LedgeClimb && playerController.LedgeClimber != null)
                {
                    float climbT = Mathf.Clamp01(playerController.LedgeClimber.ClimbProgress);
                    float syncedNorm = Mathf.Lerp(ClimbHangStartNorm, ClimbStandEndNorm, climbT);

                    if (climbT < 0.12f && animator.IsInTransition(0))
                    {
                        float clipDuration = 2.8f;
                        float climbDur = Mathf.Max(0.2f, playerController.LedgeClimber.ClimbDuration);
                        animator.speed = ((ClimbStandEndNorm - ClimbHangStartNorm) * clipDuration) / climbDur;
                        currentPlayingState = "Climb";
                    }
                    else
                    {
                        currentPlayingState = "Climb";
                        animator.speed = 0f;
                        animator.Play("Climb", 0, syncedNorm);
                    }
                }
                else
                {
                    bool movingOnRope = (playerController != null && playerController.CurrentState == PlayerState.RopeClimb);
                    animator.speed = movingOnRope ? 1f : 0f;
                    CrossFadeToState("Climb", 0.12f);
                }
                return;
            }

            if (throwPhase == ThrowAnimPhase.Pickup)
            {
                currentPlayingState = "Throw";
                animator.speed = 0f;
                throwNormTime += (ThrowPickupEndNorm / 0.72f) * dt;

                if (throwNormTime >= ThrowPickupEndNorm || (throwNormTime >= 0.30f && currentSmoothSpeed > 0.35f))
                {
                    ResetCarrySocketOffset();
                    throwPhase = ThrowAnimPhase.None;
                    animator.speed = 1f;
                    string next = (currentSmoothSpeed > 0.15f) ? "Walk" : "Idle";
                    CrossFadeToState(next, 0.12f);
                }
                else
                {
                    animator.Play("Throw", 0, Mathf.Clamp(throwNormTime, ThrowPickupStartNorm, ThrowPickupEndNorm));
                }
                return;
            }
            if (throwPhase == ThrowAnimPhase.AimWindup || (playerController != null && playerController.CurrentState == PlayerState.AimingThrow))
            {
                if (throwPhase != ThrowAnimPhase.AimWindup)
                {
                    throwPhase = ThrowAnimPhase.AimWindup;
                    throwNormTime = ThrowPickupEndNorm;
                }
                currentPlayingState = "Throw";
                animator.speed = 0f;
                throwNormTime = Mathf.MoveTowards(throwNormTime, ThrowAimHoldNorm, (0.08f / 0.18f) * dt);
                animator.Play("Throw", 0, throwNormTime);
                return;
            }
            if (throwPhase == ThrowAnimPhase.Release)
            {
                currentPlayingState = "Throw";
                animator.speed = 0f;
                throwNormTime += ((ThrowReleaseEndNorm - ThrowAimHoldNorm) / 0.50f) * dt;

                if (throwNormTime >= ThrowReleaseEndNorm || (throwNormTime >= 0.65f && currentSmoothSpeed > 0.35f))
                {
                    throwPhase = ThrowAnimPhase.None;
                    animator.speed = 1f;
                    string next = (currentSmoothSpeed > 0.15f) ? "Walk" : "Idle";
                    CrossFadeToState(next, 0.12f);
                }
                else
                {
                    animator.Play("Throw", 0, Mathf.Clamp(throwNormTime, ThrowAimHoldNorm, ThrowReleaseEndNorm));
                }
                return;
            }

            animator.speed = 1f;

            if (playerController != null && playerController.CurrentState == PlayerState.PushPull)
            {
                CrossFadeToState("PushPull", 0.15f);
            }
            else if (playerController != null && playerController.CurrentState == PlayerState.Carrying)
            {
                if (currentSmoothSpeed > 0.15f)
                {
                    CrossFadeToState("Carry", 0.15f);
                }
                else
                {
                    CrossFadeToState("Idle", 0.15f);
                }
            }
            else if (playerController != null && playerController.CurrentState == PlayerState.Airborne)
            {
                if (playerController.Velocity.y > 0.2f)
                {
                    CrossFadeToState("Jump", 0.12f);
                }
                else
                {
                    CrossFadeToState("Fall", 0.15f);
                }
            }
            else if (isCrawling)
            {
                bool isMovingCrawl = (playerController != null && Mathf.Abs(playerController.HorizontalInput) > 0.08f) || currentSmoothSpeed > 0.12f;
                animator.speed = isMovingCrawl ? Mathf.Clamp(currentSmoothSpeed / 1.2f, 0.70f, 1.25f) : 0f;
                CrossFadeToState("Crawl", 0.12f);
            }
            else if (isCrouching)
            {
                bool isMovingWhileCrouched = (playerController != null && Mathf.Abs(playerController.HorizontalInput) > 0.08f) || currentSmoothSpeed > 0.14f;
                if (isMovingWhileCrouched)
                {
                    animator.speed = Mathf.Clamp(currentSmoothSpeed / 1.75f, 0.75f, 1.25f);
                    CrossFadeToState("CrouchWalk", 0.14f);
                }
                else
                {
                    animator.speed = 1f;
                    CrossFadeToState("Crouch", 0.14f);
                }
            }
            else
            {
                if (currentPlayingState == "Climb" || currentPlayingState == "LedgeHang" || currentPlayingState == "Jump" || currentPlayingState == "Fall" || currentPlayingState == "Throw")
                {
                    string next = (currentSmoothSpeed > 0.15f) ? "Walk" : "Idle";
                    CrossFadeToState(next, 0.12f);
                }
                else
                {
                    string targetState = "Idle";
                    if (currentSmoothSpeed > 3.0f) targetState = "Run";
                    else if (currentSmoothSpeed > 0.15f) targetState = "Walk";

                    CrossFadeToState(targetState, 0.15f);
                }
            }
        }

        public void OnPickupStarted(Vector3 itemGroundWorldPos, Transform carrySocketTransform)
        {
            if (animator == null) return;
            pickupGroundWorldPos = itemGroundWorldPos;
            activeCarrySocket = carrySocketTransform;
            throwPhase = ThrowAnimPhase.Pickup;
            throwNormTime = ThrowPickupStartNorm;
            currentPlayingState = "Throw";
            animator.speed = 0f;
            animator.Play("Throw", 0, ThrowPickupStartNorm);
        }

        public void OnAimThrowStarted(Transform carrySocketTransform)
        {
            if (animator == null) return;
            activeCarrySocket = carrySocketTransform;
            throwPhase = ThrowAnimPhase.AimWindup;
            throwNormTime = ThrowPickupEndNorm;
            currentPlayingState = "Throw";
            animator.speed = 0f;
            animator.Play("Throw", 0, throwNormTime);
        }

        public void OnThrowReleased()
        {
            if (animator == null) return;
            ResetCarrySocketOffset();
            throwPhase = ThrowAnimPhase.Release;
            throwNormTime = ThrowAimHoldNorm;
            currentPlayingState = "Throw";
            animator.speed = 0f;
            animator.Play("Throw", 0, throwNormTime);
        }

        public void OnAimThrowCancelled()
        {
            ResetCarrySocketOffset();
            throwPhase = ThrowAnimPhase.None;
            if (animator != null)
            {
                animator.speed = 1f;
                string next = (currentSmoothSpeed > 0.15f) ? "Walk" : "Idle";
                CrossFadeToState(next, 0.12f);
            }
        }

        private void ResetCarrySocketOffset()
        {
            if (activeCarrySocket != null)
            {
                float facing = (playerController != null) ? playerController.FacingDirection : 1f;
                activeCarrySocket.localPosition = new Vector3(facing * 0.44f, 1.05f, 0f);
            }
        }

        public void OnLedgeHangStarted()
        {
            if (animator == null) return;
            throwPhase = ThrowAnimPhase.None;
            hasValidHangHipY = false;
            animator.speed = 1f;
            currentPlayingState = "LedgeHang";
            animator.SetBool(IsLedgeHangingHash, true);
            animator.SetBool(IsClimbingHash, false);
            animator.CrossFadeInFixedTime("LedgeHang", 0.08f);
        }

        public void OnClimbStarted()
        {
            if (animator == null) return;
            throwPhase = ThrowAnimPhase.None;
            currentPlayingState = "Climb";
            animator.SetBool(IsLedgeHangingHash, false);
            animator.SetBool(IsClimbingHash, true);
            animator.speed = 1f;
            animator.CrossFadeInFixedTime("Climb", 0.10f, 0, ClimbHangStartNorm);
        }

        public void OnClimbFinished()
        {
            if (animator == null) return;
            hasValidHangHipY = false;
            animator.speed = 1f;
            string next = (currentSmoothSpeed > 0.15f) ? "Walk" : "Idle";
            currentPlayingState = next;
            animator.SetBool(IsLedgeHangingHash, false);
            animator.SetBool(IsClimbingHash, false);
            animator.CrossFadeInFixedTime(next, 0.12f);
        }

        private void CrossFadeToState(string stateName, float duration)
        {
            if (animator == null) return;

            if (animator.GetCurrentAnimatorStateInfo(0).IsName(stateName))
            {
                currentPlayingState = stateName;
                return;
            }

            if (animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).IsName(stateName))
            {
                currentPlayingState = stateName;
                return;
            }

            currentPlayingState = stateName;
            if (Application.isPlaying)
            {
                animator.CrossFadeInFixedTime(stateName, duration);
            }
            else
            {
                animator.Play(stateName, 0, 0f);
            }
        }

        public void ForceWalk()
        {
            mode = AnAnimationMode.ForceWalk;
            if (animator != null)
            {
                animator.speed = 1f;
                animator.SetBool(IsClimbingHash, false);
                animator.SetBool(IsLedgeHangingHash, false);
                animator.SetBool(IsCrouchingHash, false);
                animator.SetBool(IsCrawlingHash, false);
                animator.SetFloat(SpeedHash, 2.0f);
            }
            CrossFadeToState("Walk", 0.12f);
        }

        public void ForceRun()
        {
            mode = AnAnimationMode.ForceRun;
            if (animator != null)
            {
                animator.speed = 1f;
                animator.SetBool(IsClimbingHash, false);
                animator.SetBool(IsLedgeHangingHash, false);
                animator.SetBool(IsCrouchingHash, false);
                animator.SetBool(IsCrawlingHash, false);
                animator.SetFloat(SpeedHash, 5.5f);
            }
            CrossFadeToState("Run", 0.12f);
        }

        public void ForceLedgeHang()
        {
            mode = AnAnimationMode.ForceLedgeHang;
            if (animator != null)
            {
                animator.speed = 1f;
                animator.SetBool(IsClimbingHash, false);
                animator.SetBool(IsLedgeHangingHash, true);
                animator.SetBool(IsCrouchingHash, false);
                animator.SetBool(IsCrawlingHash, false);
            }
            CrossFadeToState("LedgeHang", 0.12f);
        }

        public void ForceClimb()
        {
            mode = AnAnimationMode.ForceClimb;
            forceClimbPreviewTimer = 0f;
            if (animator != null)
            {
                animator.SetBool(IsClimbingHash, true);
                animator.SetBool(IsLedgeHangingHash, false);
            }
            CrossFadeToState("Climb", 0.10f);
        }

        public void ForceCrouch()
        {
            mode = AnAnimationMode.ForceCrouch;
            if (animator != null)
            {
                animator.speed = 1f;
                animator.SetBool(IsCrouchingHash, true);
                animator.SetBool(IsCrawlingHash, false);
            }
            CrossFadeToState("Crouch", 0.15f);
        }

        public void ForceCrouchWalk()
        {
            mode = AnAnimationMode.ForceCrouchWalk;
            if (animator != null)
            {
                animator.speed = 1f;
                animator.SetBool(IsCrouchingHash, true);
                animator.SetBool(IsCrawlingHash, false);
            }
            CrossFadeToState("CrouchWalk", 0.15f);
        }

        public void ForceCrawl()
        {
            mode = AnAnimationMode.ForceCrawl;
            if (animator != null)
            {
                animator.speed = 1f;
                animator.SetBool(IsCrouchingHash, true);
                animator.SetBool(IsCrawlingHash, true);
            }
            CrossFadeToState("Crawl", 0.12f);
        }

        public void SetAutoMode()
        {
            mode = AnAnimationMode.Auto;
            if (animator != null) animator.speed = 1f;
        }

        private void OnGUI()
        {
            if (!showOnScreenGui || !Application.isPlaying) return;

            GUILayout.BeginArea(new Rect(20, 20, 380, 320), GUI.skin.box);
            GUILayout.Label("<color=#38bdf8><b>🎮 ĐIỀU KHIỂN NHÂN VẬT AN (HOA VÀ MÁU)</b></color>", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 14 });
            GUILayout.Space(2);

            string modeText = (mode == AnAnimationMode.Auto) 
                ? $"<color=#4ade80>TỰ ĐỘNG [{currentPlayingState}]</color>" 
                : $"<color=#facc15>ÉP BUỘC [{mode}]</color>";
            GUILayout.Label($"Trạng thái hiện tại: <b>{modeText}</b>", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 13 });
            GUILayout.Space(4);

            GUILayout.Label("• <b>A / D</b>: Đi bộ (Walk) | <b>Shift + A/D</b>: Chạy (Run)\n• <b>C / Ctrl / S</b>: Ngồi tại chỗ (Crouch)\n• <b>Ngồi + A/D</b>: Ngồi tiến về trước (CrouchWalk)\n• <b>Nhảy vào mép đá</b>: Bám vách đá (LedgeHang)\n• <b>W / Space khi bám vách</b>: Đồng bộ Leo lên (Climb)", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 11 });
            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            GUI.backgroundColor = (mode == AnAnimationMode.ForceWalk) ? Color.yellow : Color.white;
            if (GUILayout.Button("1: Walk")) ForceWalk();

            GUI.backgroundColor = (mode == AnAnimationMode.ForceRun) ? Color.yellow : Color.white;
            if (GUILayout.Button("2: Run")) ForceRun();

            GUI.backgroundColor = (mode == AnAnimationMode.ForceClimb) ? Color.yellow : Color.white;
            if (GUILayout.Button("3: Climb")) ForceClimb();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUI.backgroundColor = (mode == AnAnimationMode.ForceCrouch) ? Color.yellow : Color.white;
            if (GUILayout.Button("4: Crouch (Ngồi)")) ForceCrouch();

            GUI.backgroundColor = (mode == AnAnimationMode.ForceCrouchWalk) ? Color.yellow : Color.white;
            if (GUILayout.Button("7: Ngồi đi tới")) ForceCrouchWalk();

            GUI.backgroundColor = (mode == AnAnimationMode.ForceCrawl) ? Color.yellow : Color.white;
            if (GUILayout.Button("5: Crawl (Bò)")) ForceCrawl();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUI.backgroundColor = (mode == AnAnimationMode.ForceLedgeHang) ? Color.yellow : Color.white;
            if (GUILayout.Button("6: LedgeHang (Bám vách đá)")) ForceLedgeHang();
            GUILayout.EndHorizontal();

            GUI.backgroundColor = (mode == AnAnimationMode.Auto) ? Color.cyan : Color.white;
            if (GUILayout.Button("0: Chế độ Tự Động (Auto Gameplay)")) SetAutoMode();
            GUI.backgroundColor = Color.white;

            GUILayout.EndArea();
        }

        [ContextMenu("Ép chạy: Walk (Đi bộ)")]
        public void ContextMenu_PlayWalk() => ForceWalk();

        [ContextMenu("Ép chạy: Run (Chạy nhanh)")]
        public void ContextMenu_PlayRun() => ForceRun();

        [ContextMenu("Ép chạy: LedgeHang (Bám vách đá)")]
        public void ContextMenu_PlayLedgeHang() => ForceLedgeHang();

        [ContextMenu("Ép chạy: Climb (Leo trèo)")]
        public void ContextMenu_PlayClimb() => ForceClimb();

        [ContextMenu("Ép chạy: Crouch (Ngồi tại chỗ)")]
        public void ContextMenu_PlayCrouch() => ForceCrouch();

        [ContextMenu("Ép chạy: CrouchWalk (Ngồi tiến về trước)")]
        public void ContextMenu_PlayCrouchWalk() => ForceCrouchWalk();

        [ContextMenu("Ép chạy: Crawl (Bò trườn)")]
        public void ContextMenu_PlayCrawl() => ForceCrawl();

        [ContextMenu("Về chế độ Tự Động")]
        public void ContextMenu_Auto() => SetAutoMode();
    }
}
