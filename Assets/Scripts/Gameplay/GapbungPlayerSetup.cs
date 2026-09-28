using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Script gắn vào model cha_rigged_animations để biến model thành Nhân vật chính (Player Controller)
    /// đồng thời kích hoạt Animation Gập Bụng khi nhấn phím G (tương thích cả New Input System và Legacy Input).
    /// </summary>
    public class GapbungPlayerSetup : MonoBehaviour
    {
        [Header("Cấu hình kích hoạt Animation")]
        [Tooltip("Phím bấm để kích hoạt động tác gập bụng (dành cho Legacy Input)")]
        public KeyCode triggerKey = KeyCode.G;

        [Tooltip("Tên State hoặc Clip animation trong Animator Controller")]
        public string animationStateName = "Gapbung_rig";

        private Animator animator;
        private PlayerController playerController;

        private void Awake()
        {
            // 1. Tự động tìm Animator
            animator = GetComponent<Animator>();
            if (animator == null) animator = GetComponentInChildren<Animator>();

            // 2. Tự động kiểm tra và thêm CharacterController nếu chưa có
            CharacterController cc = GetComponent<CharacterController>();
            if (cc == null)
            {
                cc = gameObject.AddComponent<CharacterController>();
                cc.height = 1.8f;
                cc.radius = 0.36f;
                cc.center = new Vector3(0f, 0.9f, 0f);
                cc.stepOffset = 0.35f;
            }

            // 3. Tự động gắn PlayerController điều khiển di chuyển
            playerController = GetComponent<PlayerController>();
            if (playerController == null)
            {
                playerController = gameObject.AddComponent<PlayerController>();
            }

            // 4. Gán tag Player
            gameObject.tag = "Player";
        }

        private void Start()
        {
            // 5. Tự động chuyển Camera chính đi theo nhân vật mới này
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                var cam2D = mainCam.GetComponent<Cinematic2DCamera>();
                if (cam2D != null)
                {
                    cam2D.Target = transform;
                    cam2D.SnapToTarget();
                }
            }
        }

        private void Update()
        {
            bool keyPressed = false;

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.gKey.wasPressedThisFrame) keyPressed = true;
            }
#else
            if (Input.GetKeyDown(triggerKey)) keyPressed = true;
#endif

            if (keyPressed)
            {
                TriggerGapbung();
            }
        }

        public void TriggerGapbung()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
                if (animator == null) animator = GetComponentInChildren<Animator>();
            }

            if (animator != null)
            {
                // 1. Kiểm tra xem Animator Controller có parameter Trigger "Gapbung" hay không
                bool hasTriggerParam = false;
                foreach (var param in animator.parameters)
                {
                    if (param.name == "Gapbung" && param.type == AnimatorControllerParameterType.Trigger)
                    {
                        hasTriggerParam = true;
                        break;
                    }
                }

                if (hasTriggerParam)
                {
                    animator.SetTrigger("Gapbung");
                    Debug.Log("<color=#4ade80>[Gapbung] Đã kích hoạt Trigger 'Gapbung'!</color>");
                    return;
                }

                // 2. Thử phát các tên State phổ biến (kể cả tên full từ FBX)
                string[] candidateNames = new string[]
                {
                    animationStateName,
                    "Gapbung_rig",
                    "RIG-Meshy_stylized-boy-character_Armature|Gapbung_rig",
                    "RIG-Meshy_stylized-boy-character_Armature|Gapbung_baked",
                    "Gapbung_baked",
                    "Gapbung",
                    "gap bung"
                };

                bool stateFound = false;
                foreach (string stateName in candidateNames)
                {
                    int hash = Animator.StringToHash(stateName);
                    if (animator.HasState(0, hash))
                    {
                        animator.Play(hash, 0, 0f);
                        Debug.Log($"<color=#4ade80>[Gapbung] Đã phát State: '{stateName}'!</color>");
                        stateFound = true;
                        break;
                    }
                }

                if (!stateFound)
                {
                    // Thử phát lại state hiện tại ở layer 0 từ đầu (Frame 0)
                    AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
                    animator.Play(info.fullPathHash, 0, 0f);
                    Debug.Log("<color=#4ade80>[Gapbung] Đã phát lại State hiện tại từ Frame 0!</color>");
                }
            }
            else
            {
                Debug.LogWarning("[Gapbung] Không tìm thấy Animator trên nhân vật!");
            }
        }
    }
}
