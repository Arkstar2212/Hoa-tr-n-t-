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
        ForceCrawl = 5
    }

    /// <summary>
    /// Component điều khiển Animation mượt mà và trực quan cho nhân vật An (Nhân vật chính):
    /// - Sử dụng Avatar và Animation chính thức từ thư viện dự án (Assets/Animations/Hero).
    /// - Không sử dụng bất kỳ mô hình AI gen hay phụ kiện giả lập nào.
    /// - Xoay mặt mượt mà (Smooth RotateTowards 720 deg/s).
    /// - Kết nối trực tiếp với PlayerController để cập nhật Speed, Ledge Grab, Leo dây, Bò trườn.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Animator))]
    public class AnAnimationController : MonoBehaviour
    {
        [Header("Chế độ Animation")]
        [Tooltip("Auto: tự động theo di chuyển / phím bấm gameplay. Force*: ép xem động tác")]
        [SerializeField] private AnAnimationMode mode = AnAnimationMode.Auto;

        [Header("Tùy chọn phím tắt test nhanh (Play Mode)")]
        [Tooltip("Bấm 1 (Walk), 2 (Run), 3 (Climb), 4 (Crouch), 5 (Crawl), 0 (Auto Gameplay)")]
        [SerializeField] private bool enableKeyShortcuts = true;

        [Header("Hiển thị giao diện điều khiển trên màn hình (OnScreen GUI)")]
        [SerializeField] private bool showOnScreenGui = false;
        public bool ShowOnScreenGui => showOnScreenGui;

        [Header("Xoay mặt mượt mà")]
        [SerializeField] private bool smoothFacing = true;
        [SerializeField] private float turnSpeed = 720f;

        [Header("Trạng thái Cutscene")]
        [SerializeField] private bool isCutsceneActive = false;
        public bool IsCutsceneActive
        {
            get => isCutsceneActive;
            set
            {
                isCutsceneActive = value;
                currentSmoothSpeed = 0f;
                speedVelocity = 0f;
                if (!isCutsceneActive && animator != null)
                {
                    animator.speed = 1f;
                }
            }
        }

        public void PlayCutsceneAnimation(string stateName, float duration = 0.25f)
        {
            if (animator == null) return;
            currentSmoothSpeed = 0f;
            speedVelocity = 0f;
            CrossFadeToState(stateName, duration);
        }

        public void SetCutsceneSpeed(float speed)
        {
            currentSmoothSpeed = speed;
            if (animator != null)
            {
                animator.SetFloat(SpeedHash, speed);
            }
        }

        private enum ThrowAnimPhase
        {
            None = 0,
            Pickup = 1,       // Khúc đầu (0.00 -> 0.37): Cúi xuống nhặt vật từ mặt đất lên tay
            AimWindup = 2,    // Khúc chuẩn bị ném (0.37 -> 0.45): Đưa tay ra sau ngắm ném và giữ nguyên tư thế
            Release = 3       // Khúc cuối (0.45 -> 0.88): Vung tay ném mạnh về phía trước và thu tay về
        }

        private const float ThrowPickupStartNorm = 0.00f;
        private const float ThrowPickupGrabNorm = 0.21f;  // Frame 31: tay chạm vật dưới đất
        private const float ThrowPickupEndNorm = 0.37f;   // Frame 54: đứng thẳng dậy cầm vật trước ngực
        private const float ThrowAimHoldNorm = 0.45f;     // Frame 66: tay phải kéo ra sau đầu chuẩn bị ném
        private const float ThrowReleaseEndNorm = 0.88f;  // Frame 128: hoàn tất vung tay ném và thu tay về

        private Animator animator;
        private CharacterController characterController;
        private PlayerController playerController;
        private Transform hipsBone;
        private Transform rightHandBone;
        private Transform activeCarrySocket;
        private Vector3 pickupGroundWorldPos;
        private ThrowAnimPhase throwPhase = ThrowAnimPhase.None;
        private float throwNormTime = 0f;
        private float standingHipOffsetY = 0.515f;
        private float climbClipLength = 2.167f;
        private float lastClimbExcessY = 0f;
        private float lastClimbWallHugX = 0f;
        private float currentSmoothSpeed = 0f;
        private float speedVelocity = 0f;
        private float targetFacingAngle = 90f; // 90 = Right, -90 = Left
        private string currentPlayingState = "Idle";
        [Header("Tầm nhìn đầu khi ngồi (LookAt IK)")]
        [Range(0f, 1f)]
        [SerializeField] private float crouchHeadLookWeight = 0.85f;
        private float currentHeadIKWeight = 0f;
        private Transform headBone;
        private bool isLanding = false;
        private float landingTimer = 0f;
        private const float DefaultLandingDuration = 0.35f;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsClimbingHash = Animator.StringToHash("IsClimbing");
        private static readonly int IsCrouchingHash = Animator.StringToHash("IsCrouching");
        private static readonly int IsCrawlingHash = Animator.StringToHash("IsCrawling");

        public AnAnimationMode CurrentMode => mode;
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
            standingHipOffsetY = 0.515f * Mathf.Max(0.1f, transform.lossyScale.y);
        }

        private void Update()
        {
            if (isCutsceneActive) return;

            if (animator == null)
            {
                SetupComponents();
                if (animator == null) return;
            }

            if (animator.applyRootMotion) animator.applyRootMotion = false;

            if (!Application.isPlaying) return;

            if (throwPhase == ThrowAnimPhase.None && (playerController == null || playerController.CurrentState != PlayerState.PushPull) && animator.speed != 1f)
            {
                animator.speed = 1f;
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
            if (isCutsceneActive)
            {
                if (animator != null && animator.applyRootMotion)
                {
                    transform.position += animator.deltaPosition;
                    transform.rotation *= animator.deltaRotation;
                }
                return;
            }
            // Chặn hoàn toàn Root Motion tự động của Animator để CharacterController và PlayerLedgeClimber
            // làm chủ 100% vị trí vật lý, không bao giờ xảy ra xung đột giật lùi.
        }

        
        private void OnAnimatorIK(int layerIndex)
        {
            if (isCutsceneActive) return;
            if (animator == null || !animator.isHuman) return;

            // Rope IK: Khóa bàn tay bám chắc chắn vào dây thừng theo chuẩn Planet of Lana
            if (playerController != null &&
                (playerController.CurrentState == PlayerState.RopeGrab || playerController.CurrentState == PlayerState.RopeClimb) &&
                playerController.RopeHandler != null && playerController.RopeHandler.CurrentRope != null)
            {
                var rope = playerController.RopeHandler.CurrentRope;
                Vector3 ropeGrabPos = rope.GetPlayerRopePosition(playerController);
                float radAngle = rope.CurrentAngleRadians;
                Vector3 ropeTangent = new Vector3(Mathf.Sin(radAngle), -Mathf.Cos(radAngle), 0f);
                Quaternion ropeHandRot = Quaternion.LookRotation(Vector3.forward, -ropeTangent);

                if (currentPlayingState == "RopeHang" || currentPlayingState == "RopeSwing")
                {
                    // Staggered grip: Tay phải nắm trên (+10cm), tay trái nắm dưới (-10cm)
                    Vector3 rHandTarget = ropeGrabPos + ropeTangent * 0.10f;
                    Vector3 lHandTarget = ropeGrabPos - ropeTangent * 0.10f;

                    animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0.95f);
                    animator.SetIKPosition(AvatarIKGoal.RightHand, rHandTarget);
                    animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0.85f);
                    animator.SetIKRotation(AvatarIKGoal.RightHand, ropeHandRot);

                    animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0.95f);
                    animator.SetIKPosition(AvatarIKGoal.LeftHand, lHandTarget);
                    animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0.85f);
                    animator.SetIKRotation(AvatarIKGoal.LeftHand, ropeHandRot);
                }
                else
                {
                    // Khi đang trèo: Hand-over-hand bám dọc theo trục dây thừng
                    Vector3 rHandAnim = animator.GetIKPosition(AvatarIKGoal.RightHand);
                    Vector3 lHandAnim = animator.GetIKPosition(AvatarIKGoal.LeftHand);

                    Vector3 rProjected = ropeGrabPos + Vector3.Project(rHandAnim - ropeGrabPos, ropeTangent);
                    Vector3 lProjected = ropeGrabPos + Vector3.Project(lHandAnim - ropeGrabPos, ropeTangent);

                    animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0.90f);
                    animator.SetIKPosition(AvatarIKGoal.RightHand, rProjected);
                    animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0.80f);
                    animator.SetIKRotation(AvatarIKGoal.RightHand, ropeHandRot);

                    animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0.90f);
                    animator.SetIKPosition(AvatarIKGoal.LeftHand, lProjected);
                    animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0.80f);
                    animator.SetIKRotation(AvatarIKGoal.LeftHand, ropeHandRot);
                }

                // LookAt IK: Nhìn theo trục dây lên trên khi leo hoặc nhìn xuống khi tụt
                float lookOffset = (currentPlayingState == "RopeClimbDown") ? -1.0f : 1.5f;
                Vector3 ropeLookTarget = ropeGrabPos + ropeTangent * lookOffset;
                animator.SetLookAtWeight(0.70f, 0.1f, 0.80f, 0.0f, 0.4f);
                animator.SetLookAtPosition(ropeLookTarget);
                return;
            }
            else if (playerController != null && playerController.CurrentState == PlayerState.PushPull && playerController.InteractionHandler != null)
            {
                var crate = playerController.InteractionHandler.CurrentCrate;
                if (crate != null)
                {
                    int side = crate.PlayerSide; // -1 = player is on the left, +1 = player is on the right
                    float halfWidth = crate.BoxSize.x * 0.5f;
                    float contactX = crate.transform.position.x + side * halfWidth;
                    float handY = crate.transform.position.y + 0.66f;

                    float facing = (playerController != null) ? playerController.FacingDirection : -side;
                    // Khi nhìn sang phải (+90° around Y): tay trái ở -Z, tay phải ở +Z. Khi nhìn sang trái (-90°): ngược lại.
                    Vector3 lHandTarget = new Vector3(contactX, handY + 0.02f, -facing * 0.11f);
                    Vector3 rHandTarget = new Vector3(contactX, handY - 0.02f, facing * 0.11f);

                    animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0.92f);
                    animator.SetIKPosition(AvatarIKGoal.LeftHand, lHandTarget);

                    animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0.92f);
                    animator.SetIKPosition(AvatarIKGoal.RightHand, rHandTarget);
                    return;
                }
                else
                {
                    animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
                    animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
                    animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
                    animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
                }
            }
            else
            {
                animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
                animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
                animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
            }

            bool isCrouchedState = (currentPlayingState == "Crouch" || currentPlayingState == "CrouchWalk" || currentPlayingState == "CrouchIdle" ||
                                   mode == AnAnimationMode.ForceCrouch ||
                                   (playerController != null && playerController.IsCrouching));

            float targetWeight = isCrouchedState ? crouchHeadLookWeight : 0f;
            float dt = Application.isPlaying ? Time.deltaTime : 0.02f;
            currentHeadIKWeight = Mathf.MoveTowards(currentHeadIKWeight, targetWeight, 5f * dt);

            if (currentHeadIKWeight > 0.001f)
            {
                Transform head = (headBone != null) ? headBone : animator.GetBoneTransform(HumanBodyBones.Head);
                Vector3 headPos = (head != null) ? head.position : (transform.position + Vector3.up * 0.9f);

                // Điểm nhìn hướng thẳng ngang tầm mắt theo hướng nhìn của nhân vật
                Vector3 lookTarget = headPos + transform.forward * 4.0f;

                // Humanoid LookAt IK: bodyWeight=0 (giữ nguyên dáng thân), headWeight=0.85 (nâng đầu tự nhiên theo giải phẫu xương, không gục đầu/vẹo cổ)
                animator.SetLookAtWeight(currentHeadIKWeight, 0.0f, 0.85f, 0.0f, 0.5f);
                animator.SetLookAtPosition(lookTarget);
            }
            else
            {
                animator.SetLookAtWeight(0f);
            }
        }

        private void LateUpdate()
        {
            if (isCutsceneActive) return;
            if (!Application.isPlaying || animator == null) return;

            if (hipsBone == null && animator.isHuman)
            {
                hipsBone = animator.GetBoneTransform(HumanBodyBones.Hips);
            }
            if (rightHandBone == null && animator.isHuman)
            {
                rightHandBone = animator.GetBoneTransform(HumanBodyBones.RightHand);
            }

            if (hipsBone != null)
            {
                Vector3 rootPos = transform.position;
                Vector3 hipPos = hipsBone.position;
                float rawOffsetY = hipPos.y - rootPos.y;

                AnimatorStateInfo currInfo = animator.GetCurrentAnimatorStateInfo(0);
                bool inTrans = animator.IsInTransition(0);
                AnimatorStateInfo nextInfo = inTrans ? animator.GetNextAnimatorStateInfo(0) : default;
                AnimatorTransitionInfo transInfo = inTrans ? animator.GetAnimatorTransitionInfo(0) : default;

                bool currIsClimb = currInfo.IsName("Climb");
                bool nextIsClimb = inTrans && nextInfo.IsName("Climb");

                if (!inTrans && !currIsClimb && (currInfo.IsName("Idle") || currInfo.IsName("Walk")))
                {
                    if (rawOffsetY > 0.25f && rawOffsetY < 1.5f)
                    {
                        standingHipOffsetY = Mathf.Lerp(standingHipOffsetY, rawOffsetY, Time.deltaTime * 10f);
                    }
                }

                // Khóa hoàn toàn trục X và Z của xương Hips theo vị trí thực của nhân vật (100% In-Place)
                // Ngăn chặn triệt để hiện tượng mesh chạy tiến ra trước Collider rồi bị giật lùi lại khi lặp vòng
                if (currentPlayingState != "RopeHang" && currentPlayingState != "RopeSwing" && currentPlayingState != "RopeClimbUp" && currentPlayingState != "RopeClimbDown" && currentPlayingState != "RopeClimb")
                {
                    hipPos.x = rootPos.x;
                    hipPos.z = rootPos.z;
                }

                // Chuẩn hóa mượt mà độ cao Hips cho Climb ở cả 3 giai đoạn:
                // 1) Bám gờ / Đang leo (currIsClimb && !inTrans)
                // 2) CrossFade từ Climb sang Walk / Run / Idle ngay khúc chạy lên (currIsClimb && inTrans) -> triệt tiêu 100% lỗi vọt Hips +1.2m
                // 3) CrossFade từ Jump / Fall vào Climb (nextIsClimb && !currIsClimb)
                if (currIsClimb && !inTrans)
                {
                    float climbT;
                    if (playerController != null)
                    {
                        if (playerController.CurrentState == PlayerState.LedgeGrab)
                            climbT = 0f;
                        else if (playerController.CurrentState == PlayerState.LedgeClimb && playerController.LedgeClimber != null)
                            climbT = Mathf.Clamp01(playerController.LedgeClimber.ClimbProgress);
                        else
                            climbT = Mathf.Clamp01(currInfo.normalizedTime);
                    }
                    else
                    {
                        climbT = Mathf.Clamp01(currInfo.normalizedTime);
                    }

                    float facing = (playerController != null) ? playerController.FacingDirection : 1f;
                    float scaleRatio = Mathf.Max(0.7f, transform.lossyScale.y / 1.15f);

                    // Hàm chuông sin^2(pi * t): giá trị = 0 và đạo hàm vận tốc = 0 tại cả t=0 lẫn t=1 (không gãy giật đầu/cuối)
                    float sinVal = Mathf.Sin(climbT * Mathf.PI);
                    float smoothBell = sinVal * sinVal;

                    float targetOffsetY = standingHipOffsetY - (0.15f * scaleRatio) * smoothBell;
                    lastClimbExcessY = rawOffsetY - targetOffsetY;
                    lastClimbWallHugX = facing * (0.06f * scaleRatio) * smoothBell;

                    hipPos.x = rootPos.x + lastClimbWallHugX;
                    hipPos.y = rootPos.y + targetOffsetY;
                }
                else if (currIsClimb && inTrans)
                {
                    float blendOut = 1f - Mathf.Clamp01(transInfo.normalizedTime);
                    hipPos.x = rootPos.x + blendOut * lastClimbWallHugX;
                    hipPos.y = rootPos.y + rawOffsetY - blendOut * lastClimbExcessY;
                }
                else if (nextIsClimb && !currIsClimb)
                {
                    float blendIn = Mathf.Clamp01(transInfo.normalizedTime);
                    float startClimbExcess = (0.723f * transform.lossyScale.y) - standingHipOffsetY;
                    hipPos.y = rootPos.y + rawOffsetY - blendIn * startClimbExcess;
                }

                hipsBone.position = hipPos;
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
                        // Khi tay đang cúi xuống: giữ vật nằm yên trên mặt đất cho tới lúc tay chạm vào vật
                        activeCarrySocket.position = new Vector3(pickupGroundWorldPos.x, pickupGroundWorldPos.y, 0f);
                    }
                    else
                    {
                        // Khi tay nhấc vật từ đất lên ngực: đi theo bàn tay phải và hòa vào vị trí cầm trước ngực
                        float liftT = Mathf.Clamp01((throwNormTime - ThrowPickupGrabNorm) / (ThrowPickupEndNorm - ThrowPickupGrabNorm));
                        Vector3 handPos = (rightHandBone != null)
                            ? new Vector3(rightHandBone.position.x, rightHandBone.position.y, 0f)
                            : Vector3.Lerp(pickupGroundWorldPos, defaultCarryWorldPos, liftT);
                        activeCarrySocket.position = Vector3.Lerp(handPos, defaultCarryWorldPos, liftT * liftT);
                    }
                }
                else if (throwPhase == ThrowAnimPhase.AimWindup && rightHandBone != null)
                {
                    // Khi giơ tay ra sau chuẩn bị ném: đặt vật nằm trên bàn tay phải đang vung ra sau
                    activeCarrySocket.position = new Vector3(rightHandBone.position.x, rightHandBone.position.y + 0.05f, 0f);
                }
            }
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
                if (animator.isHuman)
                {
                    if (hipsBone == null) hipsBone = animator.GetBoneTransform(HumanBodyBones.Hips);
                    if (rightHandBone == null) rightHandBone = animator.GetBoneTransform(HumanBodyBones.RightHand);
                }

                if (animator.runtimeAnimatorController != null)
                {
                    foreach (var clip in animator.runtimeAnimatorController.animationClips)
                    {
                        if (clip != null && string.Equals(clip.name, "climb", System.StringComparison.OrdinalIgnoreCase) && clip.length > 0.2f)
                        {
                            climbClipLength = clip.length;
                            break;
                        }
                    }
                }
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
                if (kb.digit0Key.wasPressedThisFrame || kb.numpad0Key.wasPressedThisFrame || kb.backspaceKey.wasPressedThisFrame) key0 = true;
            }
#else
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) key1 = true;
            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) key2 = true;
            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) key3 = true;
            if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) key4 = true;
            if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5)) key5 = true;
            if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0) || Input.GetKeyDown(KeyCode.Backspace)) key0 = true;
#endif

            if (key1) ForceWalk();
            else if (key2) ForceRun();
            else if (key3) ForceClimb();
            else if (key4) ForceCrouch();
            else if (key5) ForceCrawl();
            else if (key0) SetAutoMode();
        }

        private void UpdateFacingRotation()
        {
            if (playerController != null && (playerController.CurrentState == PlayerState.RopeGrab || playerController.CurrentState == PlayerState.RopeClimb))
            {
                return;
            }
            if (playerController != null)
            {
                if (playerController.CurrentState == PlayerState.PushPull)
                {
                    // CỰC KỲ QUAN TRỌNG: Khi đẩy hoặc kéo thùng, nhân vật BẮT BUỘC luôn quay mặt hướng vào thùng gỗ!
                    // Tuyệt đối không xoay theo phím bấm h (khi bấm A kéo lùi thì nhân vật vẫn nhìn thùng gỗ và bước lùi)!
                    targetFacingAngle = (playerController.FacingDirection > 0) ? 90f : -90f;
                }
                else if (playerController.CurrentState == PlayerState.LedgeClimb || playerController.CurrentState == PlayerState.LedgeGrab)
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
                animator.SetBool(IsClimbingHash, false);
                animator.SetBool(IsCrouchingHash, false);
                animator.SetBool(IsCrawlingHash, false);
                animator.SetFloat(SpeedHash, 2.0f);
                CrossFadeToState("Walk", 0.15f);
                return;
            }
            if (mode == AnAnimationMode.ForceRun)
            {
                animator.SetBool(IsClimbingHash, false);
                animator.SetBool(IsCrouchingHash, false);
                animator.SetBool(IsCrawlingHash, false);
                animator.SetFloat(SpeedHash, 5.5f);
                CrossFadeToState("Run", 0.15f);
                return;
            }
            if (mode == AnAnimationMode.ForceClimb)
            {
                animator.SetBool(IsClimbingHash, true);
                animator.SetBool(IsCrouchingHash, false);
                animator.SetBool(IsCrawlingHash, false);
                CrossFadeToState("Climb", 0.08f);
                return;
            }
            if (mode == AnAnimationMode.ForceCrouch)
            {
                animator.SetBool(IsClimbingHash, false);
                animator.SetBool(IsCrouchingHash, true);
                animator.SetBool(IsCrawlingHash, false);
                CrossFadeToState("Crouch", 0.15f);
                return;
            }
            if (mode == AnAnimationMode.ForceCrawl)
            {
                animator.SetBool(IsClimbingHash, false);
                animator.SetBool(IsCrouchingHash, true);
                animator.SetBool(IsCrawlingHash, true);
                CrossFadeToState("Crawl", 0.12f);
                return;
            }

            // 0. Xử lý trạng thái Tiếp Đất (Landing) sau khi rơi
            if (isLanding)
            {
                landingTimer -= dt;
                // Nếu người chơi bắt đầu chạy hoặc hết thời gian tiếp đất, chuyển mượt sang locomotion
                if (currentSmoothSpeed > 0.25f && landingTimer < (DefaultLandingDuration - 0.14f))
                {
                    isLanding = false;
                }
                else if (landingTimer <= 0f)
                {
                    isLanding = false;
                }
                else
                {
                    CrossFadeToState("Land", 0.08f);
                    return;
                }
            }

            // Chế độ Tự Động (Auto Gameplay)
            bool isClimbing = false;
            bool isCrouching = false;
            bool isCrawling = false;
            float targetSpeed = 0f;

            if (playerController != null)
            {
                if (playerController.CurrentState == PlayerState.LedgeGrab)
                {
                    // Đang bám treo tay vào gờ vách đá (chờ bấm W/Up để trèo lên)
                    throwPhase = ThrowAnimPhase.None;
                    currentSmoothSpeed = Mathf.MoveTowards(currentSmoothSpeed, 0f, 15f * dt);
                    speedVelocity = 0f;
                    animator.SetFloat(SpeedHash, 0f);
                    animator.SetBool(IsClimbingHash, true);
                    if (currentPlayingState != "LedgeHang")
                    {
                        currentPlayingState = "LedgeHang";
                        animator.Play("Climb", 0, 0f);
                    }
                    animator.speed = 0f;
                    return;
                }
                else if (playerController.CurrentState == PlayerState.LedgeClimb)
                {
                    // Đang thực hiện động tác kéo người trèo lên đỉnh vách
                    throwPhase = ThrowAnimPhase.None;
                    float effDur = (playerController.LedgeClimber != null) ? playerController.LedgeClimber.EffectiveClimbDuration : 1.15f;
                    float climbProg = (playerController.LedgeClimber != null) ? playerController.LedgeClimber.ClimbProgress : 0f;

                    float hInput = playerController.HorizontalInput;
                    bool movingFwd = Mathf.Abs(hInput) > 0.08f && Mathf.Sign(hInput) == playerController.FacingDirection;
                    bool sprintFwd = movingFwd && (playerController.IsSprinting || (playerController.InputReader != null && playerController.InputReader.SprintHeld));

                    // Đồng bộ tốc độ phát clip 'climb' (2.167s) khớp 1:1 với thời gian vượt gờ thực tế
                    // để nhân vật hoàn tất trọn vẹn pha đứng thẳng bước chân lên sàn (t = 0.96) trước khi nối vào Walk/Run
                    float standUpBoost = (climbProg > 0.50f && movingFwd) ? 1.20f : 1.0f;
                    animator.speed = ((climbClipLength * 0.96f) / Mathf.Max(0.5f, effDur)) * standUpBoost;
                    animator.SetBool(IsClimbingHash, true);

                    float desiredSpeed = movingFwd ? (sprintFwd ? 5.5f : 2.2f) : 0f;
                    if (climbProg > 0.45f)
                    {
                        currentSmoothSpeed = Mathf.SmoothDamp(currentSmoothSpeed, desiredSpeed, ref speedVelocity, 0.09f, Mathf.Infinity, dt);
                    }
                    else
                    {
                        currentSmoothSpeed = 0f;
                        speedVelocity = 0f;
                    }
                    animator.SetFloat(SpeedHash, currentSmoothSpeed);

                    if (currentPlayingState != "Climb")
                    {
                        currentPlayingState = "Climb";
                        if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Climb"))
                        {
                            animator.Play("Climb", 0, 0f);
                        }
                    }
                    return;
                }
                else if (playerController.CurrentState == PlayerState.RopeGrab || playerController.CurrentState == PlayerState.RopeClimb)
                {
                    // Đang bám hoặc leo dây thừng (Rope mechanics)
                    throwPhase = ThrowAnimPhase.None;
                    float vertInput = (playerController.InputReader != null) ? playerController.InputReader.Vertical : 0f;

                    if (vertInput > 0.12f)
                    {
                        CrossFadeToState("RopeClimbUp", 0.10f);
                        animator.speed = 1.15f;
                    }
                    else if (vertInput < -0.12f)
                    {
                        CrossFadeToState("RopeClimbDown", 0.10f);
                        animator.speed = 1.15f;
                    }
                    else
                    {
                        // Bám dây tự nhiên (RopeHang): hai tay nắm dây, thân người buông theo phương thẳng đứng
                        // Chuyển động đung đưa con lắc được điều khiển vật lý 1:1 qua PlayerRopeHandler (không dùng animation quẫy chân)
                        CrossFadeToState("RopeHang", 0.15f);
                        animator.speed = 1.0f;
                    }
                    return;
                }
                else
                {
                    if (throwPhase == ThrowAnimPhase.None && (playerController == null || playerController.CurrentState != PlayerState.PushPull) && animator.speed != 1f) animator.speed = 1f;
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

            animator.SetBool(IsClimbingHash, isClimbing);
            animator.SetBool(IsCrouchingHash, isCrouching);
            animator.SetBool(IsCrawlingHash, isCrawling);
            animator.SetFloat(SpeedHash, currentSmoothSpeed);

            if (throwPhase == ThrowAnimPhase.Pickup)
            {
                // Pha 1 (Khi bấm E nhặt vật): Chạy khúc đầu của animation Throw (0.00 -> 0.37: cúi xuống nhặt vật lên ngực)
                currentPlayingState = "Throw";
                animator.speed = 0f;
                throwNormTime += (ThrowPickupEndNorm / 0.72f) * dt;

                // Cho phép ngắt sớm pha đứng dậy nếu người chơi bắt đầu di chuyển sau khi đã nhặt vật lên tay
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
            }
            else if (throwPhase == ThrowAnimPhase.AimWindup || (playerController != null && playerController.CurrentState == PlayerState.AimingThrow))
            {
                // Pha 2 (Chuẩn bị ném / Giữ Chuột Trái): Bỏ qua đoạn nhặt đồ, lấy khúc chuẩn bị ném (0.37 -> 0.45) và giữ nguyên tư thế giơ tay ngắm
                if (throwPhase != ThrowAnimPhase.AimWindup)
                {
                    throwPhase = ThrowAnimPhase.AimWindup;
                    throwNormTime = ThrowPickupEndNorm;
                }
                currentPlayingState = "Throw";
                animator.speed = 0f;
                throwNormTime = Mathf.MoveTowards(throwNormTime, ThrowAimHoldNorm, (0.08f / 0.18f) * dt);
                animator.Play("Throw", 0, throwNormTime);
            }
            else if (throwPhase == ThrowAnimPhase.Release)
            {
                // Pha 3 (Thả Chuột Trái ném): Chạy khúc cuối của animation Throw (0.45 -> 0.88: vung tay ném mạnh và thu tay về)
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
            }
            else if (playerController != null && playerController.CurrentState == PlayerState.PushPull)
            {
                float moveInput = playerController.HorizontalInput;
                float facing = playerController.FacingDirection;
                bool isMoving = Mathf.Abs(moveInput) > 0.05f;

                if (!isMoving)
                {
                    if (currentPlayingState != "PushPull" && currentPlayingState != "Pull")
                    {
                        CrossFadeToState("PushPull", 0.10f);
                    }

                    // Nếu animator đang trong quá trình crossfade, duy trì speed = 1f để transition hoàn tất vào đúng tư thế clip!
                    if (animator.IsInTransition(0))
                    {
                        animator.speed = 1.0f;
                    }
                    else
                    {
                        // Khi đã hoàn tất vào clip và đứng yên: tạm dừng tại frame tiếp xúc
                        animator.speed = 0f;
                    }
                }
                else
                {
                    bool isPushingForward = Mathf.Sign(moveInput) == facing;
                    if (isPushingForward)
                    {
                        // Đẩy thùng tiến tới: chạy clip đẩy tiến với tốc độ tự nhiên
                        CrossFadeToState("PushPull", 0.12f);
                        animator.speed = 1.0f;
                    }
                    else
                    {
                        // Kéo thùng lùi lại: chạy clip kéo lùi chuyên dụng
                        CrossFadeToState("Pull", 0.12f);
                        animator.speed = 1.0f;
                    }
                }
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
                isLanding = false;
                if (playerController.Velocity.y > 0.4f)
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
                CrossFadeToState("Crawl", 0.12f);
            }
            else if (playerController != null && playerController.CurrentState == PlayerState.LedgeGrab)
            {
                CrossFadeToState("LedgeHang", 0.08f);
            }
            else if (playerController != null && playerController.CurrentState == PlayerState.LedgeClimb)
            {
                CrossFadeToState("Climb", 0.08f);
            }
            else if (isCrouching)
            {
                if (currentSmoothSpeed > 0.12f)
                {
                    CrossFadeToState("Crouch", 0.15f);
                }
                else
                {
                    CrossFadeToState("CrouchIdle", 0.15f);
                }
            }
            else
            {
                if (currentPlayingState == "Climb" || currentPlayingState == "Jump" || currentPlayingState == "Fall" || currentPlayingState == "Throw" || currentPlayingState == "Land" || currentPlayingState == "LedgeHang" || currentPlayingState == "RopeClimb" || currentPlayingState == "RopeHang" || currentPlayingState == "RopeSwing" || currentPlayingState == "RopeClimbUp" || currentPlayingState == "RopeClimbDown")
                {
                    string next = "Idle";
                    if (currentSmoothSpeed > 3.0f) next = "Run";
                    else if (currentSmoothSpeed > 0.15f) next = "Walk";
                    CrossFadeToState(next, 0.15f);
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
            throwNormTime = ThrowPickupEndNorm; // Bắt đầu ngay tại mốc chuẩn bị ném (0.37), bỏ qua hoàn toàn đoạn nhặt đồ
            currentPlayingState = "Throw";
            animator.speed = 0f;
            animator.Play("Throw", 0, throwNormTime);
        }

        public void OnThrowReleased()
        {
            if (animator == null) return;
            ResetCarrySocketOffset();
            throwPhase = ThrowAnimPhase.Release;
            throwNormTime = ThrowAimHoldNorm; // Chạy từ tư thế giơ tay ngắm (0.45) hết khúc cuối vung tay ném (0.88)
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

        public Animator AnimatorComponent => animator;

        public void ResetAnimatorSpeed()
        {
            if (animator != null) animator.speed = 1f;
        }

        public void OnLanded(float impactVelocity)
        {
            if (animator == null) return;
            throwPhase = ThrowAnimPhase.None;
            animator.speed = 1f;
            isLanding = true;
            landingTimer = DefaultLandingDuration;
            CrossFadeToState("Land", 0.08f);
        }

        public void OnLedgeHangStarted()
        {
            if (animator == null) return;
            throwPhase = ThrowAnimPhase.None;
            currentSmoothSpeed = 0f;
            speedVelocity = 0f;
            currentPlayingState = "LedgeHang";
            animator.SetBool(IsClimbingHash, true);
            animator.Play("Climb", 0, 0f);
            animator.speed = 0f;
        }

        public void OnClimbCancelled()
        {
            if (animator == null) return;
            animator.speed = 1f;
            currentPlayingState = "Fall";
            animator.SetBool(IsClimbingHash, false);
            CrossFadeToState("Fall", 0.12f);
        }

        public void OnClimbStarted()
        {
            if (animator == null) return;
            throwPhase = ThrowAnimPhase.None;
            float effDur = (playerController != null && playerController.LedgeClimber != null)
                ? playerController.LedgeClimber.EffectiveClimbDuration
                : 1.15f;
            animator.speed = (climbClipLength * 0.96f) / Mathf.Max(0.5f, effDur);
            currentPlayingState = "Climb";
            animator.SetBool(IsClimbingHash, true);
            if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Climb"))
            {
                animator.CrossFadeInFixedTime("Climb", 0.08f, 0, 0f);
            }
        }

        public void OnClimbFinished()
        {
            if (animator == null) return;
            animator.speed = 1f;
            animator.SetBool(IsClimbingHash, false);

            string next = "Idle";
            if (playerController != null)
            {
                float h = playerController.HorizontalInput;
                bool movingFwd = Mathf.Abs(h) > 0.08f && Mathf.Sign(h) == playerController.FacingDirection;
                if (playerController.IsCrawling)
                {
                    next = "Crawl";
                    currentSmoothSpeed = 1.2f;
                }
                else if (playerController.IsCrouching)
                {
                    next = movingFwd ? "Crouch" : "CrouchIdle";
                    currentSmoothSpeed = movingFwd ? 1.8f : 0f;
                }
                else if (movingFwd)
                {
                    bool sprinting = playerController.IsSprinting || (playerController.InputReader != null && playerController.InputReader.SprintHeld);
                    next = sprinting ? "Run" : "Walk";
                    currentSmoothSpeed = Mathf.Max(currentSmoothSpeed, sprinting ? 4.2f : 2.0f);
                }
                else
                {
                    currentSmoothSpeed = 0f;
                }
            }
            else if (currentSmoothSpeed > 0.15f)
            {
                next = (currentSmoothSpeed > 3.0f) ? "Run" : "Walk";
            }

            animator.SetFloat(SpeedHash, currentSmoothSpeed);
            currentPlayingState = next;
            animator.CrossFadeInFixedTime(next, 0.18f);
        }

        private void CrossFadeToState(string stateName, float duration)
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;

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
            animator.CrossFadeInFixedTime(stateName, duration);
        }

        public void ForceWalk()
        {
            mode = AnAnimationMode.ForceWalk;
            if (animator != null)
            {
                animator.SetBool(IsClimbingHash, false);
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
                animator.SetBool(IsClimbingHash, false);
                animator.SetBool(IsCrouchingHash, false);
                animator.SetBool(IsCrawlingHash, false);
                animator.SetFloat(SpeedHash, 5.5f);
            }
            CrossFadeToState("Run", 0.12f);
        }

        public void ForceClimb()
        {
            mode = AnAnimationMode.ForceClimb;
            CrossFadeToState("Climb", 0.15f);
        }

        public void ForceCrouch()
        {
            mode = AnAnimationMode.ForceCrouch;
            if (animator != null)
            {
                animator.SetBool(IsCrouchingHash, true);
                animator.SetBool(IsCrawlingHash, false);
            }
            CrossFadeToState("Crouch", 0.15f);
        }

        public void ForceCrawl()
        {
            mode = AnAnimationMode.ForceCrawl;
            if (animator != null)
            {
                animator.SetBool(IsCrouchingHash, true);
                animator.SetBool(IsCrawlingHash, true);
            }
            CrossFadeToState("Crawl", 0.12f);
        }

        public void SetAutoMode()
        {
            mode = AnAnimationMode.Auto;
        }

#if AN_DEBUG_GUI
        private void OnGUI()
        {
            if (!showOnScreenGui) return;

            GUILayout.BeginArea(new Rect(20, 20, 360, 280), GUI.skin.box);
            GUILayout.Label("<color=#38bdf8><b>🎮 ĐIỀU KHIỂN NHÂN VẬT AN (HOA VÀ MÁU)</b></color>", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 14 });
            GUILayout.Space(2);

            string modeText = (mode == AnAnimationMode.Auto) 
                ? $"<color=#4ade80>TỰ ĐỘNG [{currentPlayingState}]</color>" 
                : $"<color=#facc15>ÉP BUỘC [{mode}]</color>";
            GUILayout.Label($"Trạng thái hiện tại: <b>{modeText}</b>", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 13 });
            GUILayout.Space(4);

            GUILayout.Label("• <b>A / D</b>: Đi bộ (Walk)\n• <b>Shift + A/D</b>: Chạy nhanh (Run)\n• <b>C / Ctrl / S</b>: Ngồi xổm (Crouch)\n• <b>Chui hang</b>: Tự động Bò trườn (Crawl)\n• <b>W khi bám gờ</b>: Leo trèo (Climb)", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 11 });
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

            GUI.backgroundColor = (mode == AnAnimationMode.ForceCrawl) ? Color.yellow : Color.white;
            if (GUILayout.Button("5: Crawl (Bò)")) ForceCrawl();
            GUILayout.EndHorizontal();

            GUI.backgroundColor = (mode == AnAnimationMode.Auto) ? Color.cyan : Color.white;
            if (GUILayout.Button("0: Chế độ Tự Động (Auto Gameplay)")) SetAutoMode();
            GUI.backgroundColor = Color.white;

            GUILayout.EndArea();
        }
#endif

        [ContextMenu("Ép chạy: Walk (Đi bộ)")]
        public void ContextMenu_PlayWalk() => ForceWalk();

        [ContextMenu("Ép chạy: Run (Chạy nhanh)")]
        public void ContextMenu_PlayRun() => ForceRun();

        [ContextMenu("Ép chạy: Climb (Leo trèo)")]
        public void ContextMenu_PlayClimb() => ForceClimb();

        [ContextMenu("Ép chạy: Crouch (Ngồi)")]
        public void ContextMenu_PlayCrouch() => ForceCrouch();

        [ContextMenu("Ép chạy: Crawl (Bò trườn)")]
        public void ContextMenu_PlayCrawl() => ForceCrawl();

        [ContextMenu("Về chế độ Tự Động")]
        public void ContextMenu_Auto() => SetAutoMode();
    }
}
