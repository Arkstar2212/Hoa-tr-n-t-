using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HoaVaMau.Test3D
{
    [RequireComponent(typeof(CharacterController))]
    public class Player3DController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5.0f;
        [SerializeField] private float sprintSpeed = 8.0f;
        [SerializeField] private float rotationSpeed = 12.0f;
        [SerializeField] private float gravity = 20.0f;
        [SerializeField] private float pushPower = 3.5f;

        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private Transform cameraTransform;

        private CharacterController controller;
        private Vector3 velocity;
        private float verticalVelocity = 0f;

        private static readonly int SpeedParam = Animator.StringToHash("Speed");
        private static readonly int IsMovingParam = Animator.StringToHash("IsMoving");

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        }

        private void Update()
        {
            float horizontal = 0f;
            float vertical = 0f;
            bool isSprint = false;

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) vertical += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) vertical -= 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) horizontal -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) horizontal += 1f;
                if (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed) isSprint = true;
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
            }
#else
            horizontal = Input.GetAxisRaw("Horizontal");
            vertical = Input.GetAxisRaw("Vertical");
            isSprint = Input.GetKey(KeyCode.LeftShift);
#endif

            Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;
            float currentSpeed = isSprint ? sprintSpeed : moveSpeed;
            Vector3 moveDirection = Vector3.zero;

            if (inputDir.magnitude >= 0.1f)
            {
                if (cameraTransform != null)
                {
                    Vector3 camForward = Vector3.Scale(cameraTransform.forward, new Vector3(1, 0, 1)).normalized;
                    Vector3 camRight = Vector3.Scale(cameraTransform.right, new Vector3(1, 0, 1)).normalized;
                    moveDirection = (camForward * inputDir.z + camRight * inputDir.x).normalized;
                }
                else
                {
                    moveDirection = inputDir;
                }

                if (moveDirection.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
            }

            if (controller.isGrounded)
            {
                verticalVelocity = -1.0f;
            }
            else
            {
                verticalVelocity -= gravity * Time.deltaTime;
            }

            velocity = moveDirection * (inputDir.magnitude * currentSpeed);
            velocity.y = verticalVelocity;

            controller.Move(velocity * Time.deltaTime);

            if (animator != null)
            {
                float planarSpeed = new Vector3(velocity.x, 0, velocity.z).magnitude;
                animator.SetFloat(SpeedParam, planarSpeed);
                animator.SetBool(IsMovingParam, planarSpeed > 0.15f);
            }
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            Rigidbody body = hit.collider.attachedRigidbody;
            if (body == null || body.isKinematic) return;
            if (hit.moveDirection.y < -0.3f) return;

            Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);
            if (pushDir.sqrMagnitude < 0.001f) return;

            body.AddForce(pushDir * pushPower, ForceMode.Impulse);
        }
    }
}
