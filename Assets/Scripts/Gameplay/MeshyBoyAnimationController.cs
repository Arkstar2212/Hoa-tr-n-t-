using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HoaVaMau.Gameplay
{
    public enum MeshyAnimationMode
    {
        Auto = 0,
        ForceWalk = 1,
        ForceRun = 2,
        ForceClimb = 3,
        ForceCrouch = 4,
        ForceCrawl = 5
    }

    /// <summary>
    /// Component điều khiển Animation mượt mà (smooth) cho model Meshy Boy:
    /// - Không bị giật frame (loại bỏ animator.Play(0f) và animator.speed = 0).
    /// - Sử dụng Animator Blend Tree / CrossFade với thời gian chuyển tiếp 0.15s - 0.2s.
    /// - Xoay mặt mượt mà (Smooth RotateTowards 720 deg/s) thay vì giật lật 180 độ tức thì.
    /// - Hỗ trợ cả chế độ Gameplay tự động (Đi bộ / Chạy / Leo / Ngồi / Bò) lẫn các phím tắt ép xem.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Animator))]
    public class MeshyBoyAnimationController : MonoBehaviour
    {
        [Header("Chế độ Animation")]
        [Tooltip("Auto: tự động theo di chuyển / phím bấm gameplay. Force*: ép xem động tác cố định")]
        [SerializeField] private MeshyAnimationMode mode = MeshyAnimationMode.Auto;

        [Header("Tùy chọn tương tác bàn phím (Play Mode)")]
        [Tooltip("Bấm 1 (Walk), 2 (Run), 3 (Climb), 4 (Crouch), 5 (Crawl), 0 (Auto Gameplay)")]
        [SerializeField] private bool enableKeyShortcuts = true;

        [Header("Hiển thị giao diện điều khiển trên màn hình (OnScreen GUI - Bấm F1/H để bật/tắt)")]
        [SerializeField] private bool showOnScreenGui = false;

        [Header("Xoay mặt mượt mà")]
        [SerializeField] private bool smoothFacing = true;
        [SerializeField] private float turnSpeed = 720f;

        private Animator animator;
        private CharacterController characterController;
        private PlayerController playerController;
        private float currentSmoothSpeed = 0f;
        private float speedVelocity = 0f;
        private float targetFacingAngle = 90f; // 90 = Right, -90 = Left
        private string currentPlayingState = "Idle";

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsClimbingHash = Animator.StringToHash("IsClimbing");
        private static readonly int IsCrouchingHash = Animator.StringToHash("IsCrouching");
        private static readonly int IsCrawlingHash = Animator.StringToHash("IsCrawling");
        private static readonly int AnimStateHash = Animator.StringToHash("AnimState");

        public MeshyAnimationMode CurrentMode => mode;
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
        }

        private void Update()
        {
            if (animator == null)
            {
                SetupComponents();
                if (animator == null) return;
            }

            if (!Application.isPlaying) return;

            // Đảm bảo tốc độ Animator luôn mượt mà (1.0f)
            if (animator.speed != 1f) animator.speed = 1f;

            // Xử lý phím tắt test nhanh
            if (enableKeyShortcuts)
            {
                HandleInputShortcuts();
            }

            // Xử lý xoay hướng mượt mà
            UpdateFacingRotation();

            // Cập nhật trạng thái animation
            UpdateAnimationLogic();
        }

        public void SetupComponents()
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (characterController == null) characterController = GetComponent<CharacterController>();
            if (playerController == null) playerController = GetComponent<PlayerController>();

            if (animator != null)
            {
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }

            var smr = GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr != null)
            {
                smr.updateWhenOffscreen = true;
            }

#if UNITY_EDITOR
            if (animator != null)
            {
                string controllerPath = "Assets/TripoModels/Boy_Character/Boy_AnimatorController.controller";
                var ctrl = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
                if (ctrl != null && animator.runtimeAnimatorController != ctrl)
                {
                    animator.runtimeAnimatorController = ctrl;
                    UnityEditor.EditorUtility.SetDirty(animator);
                }

                if (animator.avatar == null)
                {
                    string fbxPath = "Assets/TripoModels/Boy_Character/Boy_Character.fbx";
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
            bool toggleGui = false;

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
                if (kb.f1Key.wasPressedThisFrame || kb.hKey.wasPressedThisFrame) toggleGui = true;
            }
#else
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) key1 = true;
            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) key2 = true;
            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) key3 = true;
            if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) key4 = true;
            if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5)) key5 = true;
            if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0) || Input.GetKeyDown(KeyCode.Backspace)) key0 = true;
            if (Input.GetKeyDown(KeyCode.F1) || Input.GetKeyDown(KeyCode.H)) toggleGui = true;
#endif

            if (toggleGui) showOnScreenGui = !showOnScreenGui;

            // Khi người chơi di chuyển (bấm A/D), tự động trở về Auto Gameplay để phản hồi tức thì
            if (mode != MeshyAnimationMode.Auto && playerController != null && Mathf.Abs(playerController.HorizontalInput) > 0.08f)
            {
                SetAutoMode();
            }

            if (key1) ForceWalk();
            else if (key2) ForceRun();
            else if (key3) ForceClimb();
            else if (key4) ForceCrouch();
            else if (key5) ForceCrawl();
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

            // 1. Chế độ Ép Buộc (Manual Override)
            if (mode == MeshyAnimationMode.ForceWalk)
            {
                animator.SetBool(IsClimbingHash, false);
                animator.SetBool(IsCrouchingHash, false);
                animator.SetBool(IsCrawlingHash, false);
                animator.SetFloat(SpeedHash, 2.0f);
                CrossFadeToState("Walk", 0.15f);
                return;
            }
            if (mode == MeshyAnimationMode.ForceRun)
            {
                animator.SetBool(IsClimbingHash, false);
                animator.SetBool(IsCrouchingHash, false);
                animator.SetBool(IsCrawlingHash, false);
                animator.SetFloat(SpeedHash, 5.5f);
                CrossFadeToState("Run", 0.15f);
                return;
            }
            if (mode == MeshyAnimationMode.ForceClimb)
            {
                animator.SetBool(IsClimbingHash, true);
                animator.SetBool(IsCrouchingHash, false);
                animator.SetBool(IsCrawlingHash, false);
                CrossFadeToState("Climb", 0.08f);
                return;
            }
            if (mode == MeshyAnimationMode.ForceCrouch)
            {
                animator.SetBool(IsClimbingHash, false);
                animator.SetBool(IsCrouchingHash, true);
                animator.SetBool(IsCrawlingHash, false);
                CrossFadeToState("Crouch", 0.15f);
                return;
            }
            if (mode == MeshyAnimationMode.ForceCrawl)
            {
                animator.SetBool(IsClimbingHash, false);
                animator.SetBool(IsCrouchingHash, true);
                animator.SetBool(IsCrawlingHash, true);
                CrossFadeToState("Crawl", 0.12f);
                return;
            }

            // 2. Chế độ Tự Động (Auto Gameplay)
            bool isClimbing = false;
            bool isCrouching = false;
            bool isCrawling = false;
            float targetSpeed = 0f;

            if (playerController != null)
            {
                // Kiểm tra trạng thái leo trèo
                if (playerController.CurrentState == PlayerState.LedgeClimb || 
                    playerController.CurrentState == PlayerState.RopeClimb ||
                    playerController.CurrentState == PlayerState.LedgeGrab ||
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
                        else if (playerController.IsSprinting) targetSpeed = 4.2f;
                        else targetSpeed = 2.0f;
                    }
                    else
                    {
                        targetSpeed = 0f;
                    }
                }
            }

            // Làm mượt tốc độ biến thiên (SmoothDamp) để chuyển động cực kỳ êm ái
            currentSmoothSpeed = Mathf.SmoothDamp(currentSmoothSpeed, targetSpeed, ref speedVelocity, 0.08f, Mathf.Infinity, dt);

            animator.SetBool(IsClimbingHash, isClimbing);
            animator.SetBool(IsCrouchingHash, isCrouching);
            animator.SetBool(IsCrawlingHash, isCrawling);
            animator.SetFloat(SpeedHash, currentSmoothSpeed);

            if (isClimbing)
            {
                if (currentPlayingState != "Climb")
                {
                    currentPlayingState = "Climb";
                    animator.Play("Climb", 0, 0f);
                }
            }
            else if (isCrawling)
            {
                CrossFadeToState("Crawl", 0.12f);
            }
            else if (isCrouching)
            {
                CrossFadeToState("Crouch", 0.15f);
            }
            else
            {
                if (currentPlayingState == "Climb")
                {
                    // Vừa hoàn tất leo trèo, chuyển mượt mà sang Idle/Walk (các pose đã khớp 100%)
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

        public void OnClimbStarted()
        {
            if (animator == null) return;
            currentPlayingState = "Climb";
            animator.SetBool(IsClimbingHash, true);
            animator.CrossFadeInFixedTime("Climb", 0.08f);
        }

        public void OnClimbFinished()
        {
            if (animator == null) return;
            string next = (currentSmoothSpeed > 0.15f) ? "Walk" : "Idle";
            currentPlayingState = next;
            animator.SetBool(IsClimbingHash, false);
            animator.CrossFadeInFixedTime(next, 0.12f);
        }

        private void CrossFadeToState(string stateName, float duration)
        {
            if (animator == null) return;

            // Nếu đã ở trong state này hoặc đang trong quá trình chuyển sang state này -> không gọi đè lại để tránh bị reset frame liên tục
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
            mode = MeshyAnimationMode.ForceWalk;
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
            mode = MeshyAnimationMode.ForceRun;
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
            mode = MeshyAnimationMode.ForceClimb;
            CrossFadeToState("Climb", 0.15f);
        }

        public void ForceCrouch()
        {
            mode = MeshyAnimationMode.ForceCrouch;
            if (animator != null)
            {
                animator.SetBool(IsCrouchingHash, true);
                animator.SetBool(IsCrawlingHash, false);
            }
            CrossFadeToState("Crouch", 0.15f);
        }

        public void ForceCrawl()
        {
            mode = MeshyAnimationMode.ForceCrawl;
            if (animator != null)
            {
                animator.SetBool(IsCrouchingHash, true);
                animator.SetBool(IsCrawlingHash, true);
            }
            CrossFadeToState("Crawl", 0.12f);
        }

        public void SetAutoMode()
        {
            mode = MeshyAnimationMode.Auto;
        }

        private void OnGUI()
        {
            if (!showOnScreenGui || !Application.isPlaying) return;

            GUILayout.BeginArea(new Rect(20, 20, 360, 280), GUI.skin.box);
            GUILayout.Label("<color=#38bdf8><b>🎮 MESHY BOY TEST CONTROLLER</b></color>", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 14 });
            GUILayout.Space(2);

            string modeText = (mode == MeshyAnimationMode.Auto) 
                ? $"<color=#4ade80>TỰ ĐỘNG [{currentPlayingState}]</color>" 
                : $"<color=#facc15>ÉP BUỘC [{mode}]</color>";
            GUILayout.Label($"Trạng thái hiện tại: <b>{modeText}</b>", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 13 });
            GUILayout.Space(4);

            GUILayout.Label("• <b>A / D</b>: Đi bộ mượt mà (Walk)\n• <b>Shift + A/D</b>: Tăng tốc chạy nhanh (Run)\n• <b>C / Ctrl / S</b>: Ngồi xổm (Crouch)\n• <b>Chui qua hang nhỏ</b>: Tự động Bò trườn (Crawl)\n• <b>W khi bám vách đá</b>: Leo trèo (Climb)", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 11 });
            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            GUI.backgroundColor = (mode == MeshyAnimationMode.ForceWalk) ? Color.yellow : Color.white;
            if (GUILayout.Button("1: Walk")) ForceWalk();

            GUI.backgroundColor = (mode == MeshyAnimationMode.ForceRun) ? Color.yellow : Color.white;
            if (GUILayout.Button("2: Run")) ForceRun();

            GUI.backgroundColor = (mode == MeshyAnimationMode.ForceClimb) ? Color.yellow : Color.white;
            if (GUILayout.Button("3: Climb")) ForceClimb();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUI.backgroundColor = (mode == MeshyAnimationMode.ForceCrouch) ? Color.yellow : Color.white;
            if (GUILayout.Button("4: Crouch (Ngồi)")) ForceCrouch();

            GUI.backgroundColor = (mode == MeshyAnimationMode.ForceCrawl) ? Color.yellow : Color.white;
            if (GUILayout.Button("5: Crawl (Bò)")) ForceCrawl();
            GUILayout.EndHorizontal();

            GUI.backgroundColor = (mode == MeshyAnimationMode.Auto) ? Color.cyan : Color.white;
            if (GUILayout.Button("0: Chế độ Tự Động (Auto Gameplay)")) SetAutoMode();
            GUI.backgroundColor = Color.white;

            GUILayout.EndArea();
        }

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
