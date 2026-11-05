using UnityEngine;
using UnityEngine.SceneManagement;
using HoaVaMau.Gameplay;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HoaVaMau.Test3D
{
    /// <summary>
    /// Full 3D Character Locomotion & Mechanics Controller for Bé An in "CaMau_1954_Village.unity".
    /// Upgraded with full feature parity from Overhaul's PlayerController & PlayerAudioController:
    /// - Camera-relative 3D movement (WASD / Arrows / KeybindingManager / Gamepad)
    /// - Sprint ([Shift]), Jump with Coyote Time & Jump Buffer ([Space]), Stealth Crouch ([C] / [Ctrl])
    /// - Barefoot Dirt Footstep & Jump/Land SFX (SFX_An_Footsteps_Dirt.mp3)
    /// - 3D Crate Push/Pull (InteractableCrate3D), 3D Item Carry/Toss (PortableItem3D),
    ///   3D Stealth Bush Concealment (BushHideZone), and Checkpoint Respawn (CheckpointManager).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class Player3DController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5.0f;
        [SerializeField] private float sprintSpeed = 8.0f;
        [SerializeField] private float crouchSpeed = 2.4f;
        [SerializeField] private float cratePushSpeed = 2.35f;
        [SerializeField] private float jumpHeight = 1.25f;
        [SerializeField] private float rotationSpeed = 12.0f;
        [SerializeField] private float gravity = 20.0f;
        [SerializeField] private float pushPower = 3.5f;

        [Header("References & Audio")]
        [SerializeField] private Animator animator;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private AudioClip footstepDirtClip;

        [Header("Footstep Audio Volume (To & Rõ)")]
        [SerializeField, Range(0.1f, 1f)] private float walkFootstepVolume = 0.95f;
        [SerializeField, Range(0.1f, 1f)] private float sprintFootstepVolume = 1.0f;
        [SerializeField, Range(0.1f, 1f)] private float crouchFootstepVolume = 0.60f;
        [Tooltip("Hệ số tăng cường âm lượng tiếng bước chân")]
        [SerializeField, Range(1.0f, 1.8f)] private float footstepVolumeBoost = 1.30f;

        private CharacterController controller;
        private AudioSource footstepAudioSource;
        private AudioSource footstepBoosterSource;
        private Transform hipsBone;
        private Vector3 velocity;
        private float verticalVelocity = 0f;

        // Standing vs Crouching Capsule Geometry
        private float standingHeight = 1.15f;
        private Vector3 standingCenter = new Vector3(0f, 0.575f, 0f);
        private bool isCrouching = false;
        private bool isSprinting = false;

        // Coyote Time & Jump Buffer (ported from Overhaul PlayerController)
        private float coyoteTimer = 0f;
        private float jumpBufferTimer = 0f;
        private bool wasGroundedLastFrame = true;
        private const float CoyoteDuration = 0.12f;
        private const float JumpBufferDuration = 0.12f;

        // Footstep cadence
        private float stepDistanceAccumulator = 0f;

        // Ported Overhaul Gameplay States
        private bool isHidingInBush = false;
        private float concealment = 0f;
        private InteractableCrate3D grabbedCrate;
        private PortableItem3D carriedItem;
        public bool IsInputLocked { get; set; } = false;

        private static readonly int SpeedParam = Animator.StringToHash("Speed");
        private static readonly int IsMovingParam = Animator.StringToHash("IsMoving");
        private static readonly int IsCrouchingParam = Animator.StringToHash("IsCrouching");
        private static readonly int IsGroundedParam = Animator.StringToHash("IsGrounded");
        private bool hasSpeedParam = false;
        private bool hasIsMovingParam = false;
        private bool hasIsCrouchingParam = false;
        private bool hasIsGroundedParam = false;
        private string currentAnimState = "";

        public bool IsGrounded => controller != null && controller.isGrounded;
        public bool IsCrouching => isCrouching;
        public bool IsSprinting => isSprinting;
        public bool IsHidingInBush => isHidingInBush;
        public float Concealment { get => concealment; set => concealment = Mathf.Clamp01(value); }
        public Vector3 PlanarVelocity => new Vector3(velocity.x, 0f, velocity.z);
        public float PlanarSpeed => new Vector2(velocity.x, velocity.z).magnitude;
        public InteractableCrate3D GrabbedCrate => grabbedCrate;
        public PortableItem3D CarriedItem => carriedItem;

        public void ConfigureFootstepAudio(AudioClip dirtStepClip)
        {
            footstepDirtClip = dirtStepClip;
        }

        public void SetHidingInBush(bool hiding)
        {
            isHidingInBush = hiding;
            if (!hiding) concealment = 0f;
        }

        public void SetGrabbedCrate(InteractableCrate3D crate) => grabbedCrate = crate;
        public void ClearGrabbedCrate(InteractableCrate3D crate) { if (grabbedCrate == crate) grabbedCrate = null; }

        public void SetCarriedItem(PortableItem3D item) => carriedItem = item;
        public void ClearCarriedItem(PortableItem3D item) { if (carriedItem == item) carriedItem = null; }

        private void Awake()
        {
            // Disable any leftover 2D controllers on the 3D player so they never override rotation or Animator
            // Canonical 2D controllers disabled on 3D player

            var player2D = GetComponent<PlayerController>();
            if (player2D != null) player2D.enabled = false;
            var anAnim2D = GetComponent<AnAnimationController>();
            if (anAnim2D != null) anAnim2D.enabled = false;

            // Ensure mesh visibility and proper scale
            if (transform.localScale.y < 0.8f)
            {
                transform.localScale = Vector3.one * 1.25f;
            }
            foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                smr.gameObject.SetActive(true);
                smr.enabled = true;
                smr.updateWhenOffscreen = true;
            }
            foreach (var mr in GetComponentsInChildren<MeshRenderer>(true))
            {
                mr.enabled = true;
            }

            controller = GetComponent<CharacterController>();
            if (controller != null)
            {
                standingHeight = controller.height > 0.2f ? controller.height : 1.15f;
                standingCenter = controller.center.sqrMagnitude > 0.001f ? controller.center : new Vector3(0f, standingHeight * 0.5f, 0f);
            }

            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.enabled = true;
                animator.speed = 1f;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                if (animator.isHuman) hipsBone = animator.GetBoneTransform(HumanBodyBones.Hips);
                RefreshAnimatorParamFlags();
            }

            ResolveMainCameraTransform();

            footstepAudioSource = GetComponent<AudioSource>();
            if (footstepAudioSource == null) footstepAudioSource = gameObject.AddComponent<AudioSource>();
            footstepAudioSource.playOnAwake = false;
            footstepAudioSource.loop = true;
            footstepAudioSource.spatialBlend = 0.0f; // 2D direct stereo sound for player's own footsteps: loud, punchy, zero camera distance dropoff

            if (footstepDirtClip == null)
            {
#if UNITY_EDITOR
                footstepDirtClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_An_Footsteps_Dirt.wav");
                if (footstepDirtClip == null) footstepDirtClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/SFX_An_Footsteps_Dirt.mp3");
#endif
            }
            if (footstepDirtClip != null) footstepAudioSource.clip = footstepDirtClip;

            if (footstepBoosterSource == null)
            {
                var boostGo = new GameObject("Audio_Footsteps_Booster");
                boostGo.transform.SetParent(transform, false);
                footstepBoosterSource = boostGo.AddComponent<AudioSource>();
                footstepBoosterSource.playOnAwake = false;
                footstepBoosterSource.loop = true;
                footstepBoosterSource.spatialBlend = 0.0f;
                if (footstepDirtClip != null) footstepBoosterSource.clip = footstepDirtClip;
            }
        }

        private void Start()
        {
            ResolveMainCameraTransform();

            Transform spawnPoint = null;
            var spawnObj = GameObject.Find("PlayerSpawn");
            if (spawnObj != null) spawnPoint = spawnObj.transform;

#if UNITY_EDITOR
            // Trong Unity Editor: luôn ưu tiên vị trí authored trong Scene hoặc GameObject PlayerSpawn
            var introDirector = Object.FindAnyObjectByType<HoaVaMau.Cinematics.CinematicVillageIntroDirector>();
            if (introDirector != null && introDirector.isActiveAndEnabled)
            {
                // Cutscene Director quản lý vị trí bắt đầu tại cổng và kết thúc tại làng
            }
            else if (spawnPoint != null)
            {
                RespawnAt(spawnPoint.position, spawnPoint.rotation);
                return;
            }
            else
            {
                if (transform.position.z > 100f) transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                var orbitCam = Object.FindAnyObjectByType<ThirdPersonOrbitCamera>();
                if (orbitCam != null)
                {
                    orbitCam.Target = transform;
                    orbitCam.SnapToTarget(transform.eulerAngles.y);
                }
                return;
            }
#else
            // Khi xuất bản build độc lập: nạp toạ độ lưu nếu người chơi ấn TIẾP TỤC từ Menu
            var slot = HoaVaMauSaveSystem.GetSlotData(HoaVaMauSaveSystem.ActiveSlotIndex);
            if (slot != null && slot.isOccupied &&
                slot.savedSceneName == SceneManager.GetActiveScene().name &&
                slot.checkpointIndex > 0 &&
                (Mathf.Abs(slot.playerPosX) > 0.01f || Mathf.Abs(slot.playerPosZ) > 0.01f))
            {
                if (slot.playerPosX >= -80f && slot.playerPosX <= 70f &&
                    slot.playerPosZ >= -30f && slot.playerPosZ <= 195f)
                {
                    Vector3 savedPos = new Vector3(slot.playerPosX, Mathf.Clamp(slot.playerPosY, 0.08f, 3.5f), slot.playerPosZ);
                    Quaternion? savedRot = (savedPos.z > 100f) ? Quaternion.Euler(0f, 180f, 0f) : (Quaternion?)null;
                    RespawnAt(savedPos, savedRot);
                    return;
                }
            }

            if (spawnPoint != null)
            {
                RespawnAt(spawnPoint.position, spawnPoint.rotation);
                return;
            }

            var orbitCamBuild = Object.FindAnyObjectByType<ThirdPersonOrbitCamera>();
            if (orbitCamBuild != null)
            {
                orbitCamBuild.Target = transform;
                orbitCamBuild.SnapToTarget(transform.eulerAngles.y);
            }
#endif
        }

        private void ResolveMainCameraTransform()
        {
            var orbitCam = Object.FindAnyObjectByType<ThirdPersonOrbitCamera>();
            if (orbitCam != null)
            {
                cameraTransform = orbitCam.transform;
                orbitCam.Target = transform;
                return;
            }
            if (Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
        }

        private void RefreshAnimatorParamFlags()
        {
            hasSpeedParam = false;
            hasIsMovingParam = false;
            hasIsCrouchingParam = false;
            hasIsGroundedParam = false;
            if (animator == null || animator.runtimeAnimatorController == null) return;
            foreach (var p in animator.parameters)
            {
                if (p.nameHash == SpeedParam && p.type == AnimatorControllerParameterType.Float) hasSpeedParam = true;
                else if (p.nameHash == IsMovingParam && p.type == AnimatorControllerParameterType.Bool) hasIsMovingParam = true;
                else if (p.nameHash == IsCrouchingParam && p.type == AnimatorControllerParameterType.Bool) hasIsCrouchingParam = true;
                else if (p.nameHash == IsGroundedParam && p.type == AnimatorControllerParameterType.Bool) hasIsGroundedParam = true;
            }
        }

        private void TryCrossFadeState(string stateName, float duration = 0.14f)
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;
            if (currentAnimState == stateName) return;

            int hash = Animator.StringToHash(stateName);
            if (!animator.HasState(0, hash)) return;

            currentAnimState = stateName;
            animator.CrossFadeInFixedTime(hash, duration, 0);
        }

        private void OnDisable()
        {
            velocity = Vector3.zero;
            if (footstepAudioSource != null && footstepAudioSource.isPlaying)
            {
                footstepAudioSource.Stop();
            }
            if (animator != null)
            {
                if (hasSpeedParam) animator.SetFloat(SpeedParam, 0f);
                if (hasIsMovingParam) animator.SetBool(IsMovingParam, false);
            }
        }

        private void OnAnimatorMove()
        {
            // Chặn hoàn toàn Root Motion tự động để tránh nhân vật bị giật lùi
        }

        private void LateUpdate()
        {
            if (animator == null) return;
            if (hipsBone == null && animator.isHuman)
            {
                hipsBone = animator.GetBoneTransform(HumanBodyBones.Hips);
            }
            if (hipsBone != null)
            {
                Vector3 pos = hipsBone.position;
                pos.x = transform.position.x;
                pos.z = transform.position.z;
                hipsBone.position = pos;
            }
        }

        public void RespawnAt(Vector3 worldPos, Quaternion? rot = null)
        {
            if (grabbedCrate != null) grabbedCrate.ReleaseGrab();
            isCrouching = false;
            verticalVelocity = 0f;
            velocity = Vector3.zero;
            if (rot.HasValue) transform.rotation = rot.Value;

            if (controller != null)
            {
                controller.enabled = false;
                controller.height = standingHeight;
                controller.center = standingCenter;
                transform.position = worldPos;
                controller.enabled = true;
            }
            else
            {
                transform.position = worldPos;
            }

            var orbitCam = Object.FindAnyObjectByType<ThirdPersonOrbitCamera>();
            if (orbitCam != null)
            {
                orbitCam.Target = transform;
                orbitCam.SnapToTarget(transform.eulerAngles.y);
            }
        }

        private void Update()
        {
            if (PauseMenuController.Instance != null && PauseMenuController.Instance.IsPaused) return;

            if (animator != null && animator.applyRootMotion)
            {
                animator.applyRootMotion = false;
            }
            if (cameraTransform == null)
            {
                ResolveMainCameraTransform();
            }

            float dt = Time.deltaTime;
            ReadPlayerInputs(out float horizontal, out float vertical, out bool sprintHeld, out bool jumpPressed, out bool crouchPressed);
            if (IsInputLocked)
            {
                horizontal = 0f;
                vertical = 0f;
                sprintHeld = false;
                jumpPressed = false;
                crouchPressed = false;
            }

            if (IsInputLocked)
            {
                velocity = Vector3.zero;
                return;
            }

            // Toggle Stealth Crouch on [C] / [LeftCtrl]
            if (crouchPressed && grabbedCrate == null)
            {
                isCrouching = !isCrouching;
            }

            // Smooth CharacterController capsule height & visual subtle crouch posture
            if (controller != null)
            {
                float targetH = isCrouching ? standingHeight * 0.62f : standingHeight;
                controller.height = Mathf.MoveTowards(controller.height, targetH, dt * 4.5f);
                controller.center = new Vector3(standingCenter.x, controller.height * 0.5f, standingCenter.z);
            }

            Vector3 rawInput = new Vector3(horizontal, 0f, vertical);
            float inputMag = Mathf.Clamp01(rawInput.magnitude);
            Vector3 inputDir = inputMag > 0.01f ? rawInput.normalized : Vector3.zero;

            isSprinting = sprintHeld && !isCrouching && grabbedCrate == null && carriedItem == null && inputMag > 0.1f;

            float targetMoveSpeed = moveSpeed;
            if (grabbedCrate != null) targetMoveSpeed = cratePushSpeed;
            else if (isCrouching) targetMoveSpeed = crouchSpeed;
            else if (isSprinting) targetMoveSpeed = sprintSpeed;
            else if (carriedItem != null) targetMoveSpeed = moveSpeed * 0.88f;

            Vector3 moveDirection = Vector3.zero;
            if (inputMag >= 0.1f)
            {
                if (cameraTransform != null)
                {
                    Vector3 camForward = Vector3.Scale(cameraTransform.forward, new Vector3(1f, 0f, 1f)).normalized;
                    Vector3 camRight = Vector3.Scale(cameraTransform.right, new Vector3(1f, 0f, 1f)).normalized;
                    moveDirection = (camForward * inputDir.z + camRight * inputDir.x).normalized;
                }
                else
                {
                    moveDirection = inputDir;
                }

                if (moveDirection.sqrMagnitude > 0.001f && grabbedCrate == null)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * dt);
                }
            }

            // Grounded, Coyote Time & Jump Buffer
            bool grounded = controller.isGrounded;
            if (grounded)
            {
                coyoteTimer = CoyoteDuration;
                if (verticalVelocity < 0f) verticalVelocity = -1.5f;
            }
            else
            {
                coyoteTimer -= dt;
                verticalVelocity -= gravity * dt;
            }

            if (jumpPressed) jumpBufferTimer = JumpBufferDuration;
            else jumpBufferTimer -= dt;

            if (jumpBufferTimer > 0f && coyoteTimer > 0f && grabbedCrate == null)
            {
                if (isCrouching) isCrouching = false;
                verticalVelocity = Mathf.Sqrt(2f * gravity * Mathf.Max(0.4f, jumpHeight));
                jumpBufferTimer = 0f;
                coyoteTimer = 0f;
                if (footstepAudioSource != null && footstepAudioSource.isPlaying) footstepAudioSource.Stop();
                if (footstepBoosterSource != null && footstepBoosterSource.isPlaying) footstepBoosterSource.Stop();
            }

            wasGroundedLastFrame = grounded;

            velocity = moveDirection * (inputMag * targetMoveSpeed);
            velocity.y = verticalVelocity;

            controller.Move(velocity * dt);

            float planarSpd = new Vector2(velocity.x, velocity.z).magnitude;
            if (animator != null)
            {
                if (animator.speed != 1f) animator.speed = 1f;
                if (hasSpeedParam) animator.SetFloat(SpeedParam, planarSpd);
                if (hasIsMovingParam) animator.SetBool(IsMovingParam, planarSpd > 0.15f);
                if (hasIsCrouchingParam) animator.SetBool(IsCrouchingParam, isCrouching);
                if (hasIsGroundedParam) animator.SetBool(IsGroundedParam, grounded);

                // Drive direct state transitions for controllers like AC_Player_An
                if (!grounded && verticalVelocity > 0.5f)
                {
                    TryCrossFadeState("Jump", 0.10f);
                }
                else if (!grounded && verticalVelocity <= 0.5f)
                {
                    TryCrossFadeState("Fall", 0.12f);
                }
                else if (grabbedCrate != null)
                {
                    TryCrossFadeState("PushPull", 0.15f);
                }
                else if (isCrouching)
                {
                    TryCrossFadeState(planarSpd > 0.15f ? "Crouch" : "CrouchIdle", 0.15f);
                }
                else if ((isSprinting || planarSpd > 4.2f) && planarSpd > 0.2f)
                {
                    TryCrossFadeState("Run", 0.14f);
                }
                else if (planarSpd > 0.15f)
                {
                    TryCrossFadeState("Walk", 0.14f);
                }
                else
                {
                    TryCrossFadeState("Idle", 0.15f);
                }
            }

            UpdateFootstepAudio(planarSpd, grounded, inputMag, dt);
        }

        private void UpdateFootstepAudio(float planarSpd, bool grounded, float inputMag, float dt)
        {
            if (footstepAudioSource == null) return;

            if (footstepAudioSource.clip != footstepDirtClip && footstepDirtClip != null)
            {
                footstepAudioSource.clip = footstepDirtClip;
            }
            if (footstepBoosterSource != null && footstepBoosterSource.clip != footstepDirtClip && footstepDirtClip != null)
            {
                footstepBoosterSource.clip = footstepDirtClip;
            }

            // Chỉ phát tiếng bước chân khi: Đang chạm đất VÀ đang di chuyển (có vận tốc >= 0.20m/s và có input điều khiển >= 0.05)
            bool isMovingOnGround = grounded && planarSpd >= 0.20f && inputMag >= 0.05f &&
                                    (PauseMenuController.Instance == null || !PauseMenuController.Instance.IsPaused);

            // Khi dừng lại, đứng im hoặc ở trên không -> NGẮT NGAY LẬP TỨC
            if (!isMovingOnGround)
            {
                if (footstepAudioSource.isPlaying) footstepAudioSource.Stop();
                if (footstepBoosterSource != null && footstepBoosterSource.isPlaying) footstepBoosterSource.Stop();
                stepDistanceAccumulator = 0f;
                return;
            }

            // Đang di chuyển: loop âm thanh tiếng bước chân với âm lượng & cao độ phù hợp
            float sfxSetting = PlayerPrefs.GetFloat("Settings_SFXVol", 1.0f);
            float baseVol = isCrouching ? crouchFootstepVolume : (isSprinting ? sprintFootstepVolume : walkFootstepVolume);
            float targetPitch = isCrouching ? 0.85f : (isSprinting ? 1.15f : 1.0f);

            float totalTargetVol = baseVol * footstepVolumeBoost * Mathf.Max(0.5f, sfxSetting);
            float mainTargetVol = Mathf.Clamp01(totalTargetVol);
            float boostTargetVol = Mathf.Clamp01(totalTargetVol - 1.0f);

            footstepAudioSource.loop = true;
            if (footstepBoosterSource != null) footstepBoosterSource.loop = true;

            if (!footstepAudioSource.isPlaying)
            {
                footstepAudioSource.volume = mainTargetVol;
                footstepAudioSource.pitch = targetPitch;
                if (footstepAudioSource.clip != null)
                {
                    footstepAudioSource.Play();
                }

                if (footstepBoosterSource != null && boostTargetVol > 0.01f && footstepBoosterSource.clip != null)
                {
                    footstepBoosterSource.volume = boostTargetVol;
                    footstepBoosterSource.pitch = targetPitch;
                    footstepBoosterSource.timeSamples = footstepAudioSource.timeSamples;
                    footstepBoosterSource.Play();
                }
            }
            else
            {
                footstepAudioSource.volume = Mathf.MoveTowards(footstepAudioSource.volume, mainTargetVol, dt * 4.0f);
                footstepAudioSource.pitch = Mathf.MoveTowards(footstepAudioSource.pitch, targetPitch, dt * 3.0f);

                if (footstepBoosterSource != null)
                {
                    if (boostTargetVol > 0.01f)
                    {
                        if (!footstepBoosterSource.isPlaying)
                        {
                            footstepBoosterSource.timeSamples = footstepAudioSource.timeSamples;
                            footstepBoosterSource.Play();
                        }
                        footstepBoosterSource.volume = Mathf.MoveTowards(footstepBoosterSource.volume, boostTargetVol, dt * 4.0f);
                        footstepBoosterSource.pitch = Mathf.MoveTowards(footstepBoosterSource.pitch, targetPitch, dt * 3.0f);
                    }
                    else if (footstepBoosterSource.isPlaying)
                    {
                        footstepBoosterSource.Stop();
                    }
                }
            }
        }

        private static void ReadPlayerInputs(out float horizontal, out float vertical, out bool isSprint, out bool jumpPressed, out bool crouchPressed)
        {
            horizontal = 0f;
            vertical = 0f;
            isSprint = false;
            jumpPressed = KeybindingManager.IsActionPressed(GameAction.Jump);
            crouchPressed = KeybindingManager.IsActionPressed(GameAction.Crouch);

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed || KeybindingManager.IsActionHeld(GameAction.MoveUp)) vertical += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) vertical -= 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed || KeybindingManager.IsActionHeld(GameAction.MoveLeft)) horizontal -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed || KeybindingManager.IsActionHeld(GameAction.MoveRight)) horizontal += 1f;
                if (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed) isSprint = true;
                if (kb.spaceKey.wasPressedThisFrame) jumpPressed = true;
                if (kb.cKey.wasPressedThisFrame || kb.leftCtrlKey.wasPressedThisFrame) crouchPressed = true;
            }
            var gp = Gamepad.current;
            if (gp != null)
            {
                Vector2 stick = gp.leftStick.ReadValue();
                if (stick.sqrMagnitude > 0.05f)
                {
                    horizontal += stick.x;
                    vertical += stick.y;
                }
                if (gp.leftTrigger.isPressed || gp.rightTrigger.isPressed) isSprint = true;
                if (gp.buttonSouth.wasPressedThisFrame) jumpPressed = true;
                if (gp.buttonEast.wasPressedThisFrame) crouchPressed = true;
            }
#else
            horizontal = Input.GetAxisRaw("Horizontal");
            vertical = Input.GetAxisRaw("Vertical");
            isSprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (Input.GetKeyDown(KeyCode.Space)) jumpPressed = true;
            if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.LeftControl)) crouchPressed = true;
#endif
            horizontal = Mathf.Clamp(horizontal, -1f, 1f);
            vertical = Mathf.Clamp(vertical, -1f, 1f);
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            Rigidbody body = hit.collider.attachedRigidbody;
            if (body == null || body.isKinematic) return;
            if (hit.moveDirection.y < -0.3f) return;

            Vector3 pushDir = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z);
            if (pushDir.sqrMagnitude < 0.001f) return;

            body.AddForce(pushDir * pushPower, ForceMode.Impulse);
        }
    }
}
