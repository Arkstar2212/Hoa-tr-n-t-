---
name: unity-animation-character-controller
description: 2D/2.5D character locomotion, Animator Controller states, New Input System integration, cutscene input locking, and smooth gameplay blending in Unity 6.
---

# Unity Animation & Character Controller Skill

This skill governs the player controller, 2D physics movement, and Animator state machine in **Hoa trên đất đỏ**.

## 1. Controller Architecture & Separation of Concerns

Separate physics movement from visual animation:

1. **`PlayerMotor2D`**: Handles Rigidbody2D velocity, grounded detection, slopes, and gravity.
2. **`PlayerInputReader`**: Wraps the Unity Input System (`UnityEngine.InputSystem`), dispatching movement vectors and button presses as events.
3. **`PlayerAnimatorView`**: Listens to motor state (speed, grounded, vertical velocity) and updates the `Animator` parameters using pre-hashed IDs.

---

## 2. Rigidbody2D Locomotion (Unity 6)

In Unity 6, use `rb.linearVelocity` instead of the deprecated `rb.velocity`:

```csharp
using UnityEngine;

namespace HoaTrenDatDo.Character
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMotor2D : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 4.5f;
        [SerializeField] private float runSpeed = 7.0f;
        [SerializeField] private float groundCheckRadius = 0.2f;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private Transform groundCheckPoint;

        private Rigidbody2D rb;
        private Vector2 moveInput;
        private bool isGrounded;
        private bool isFacingRight = true;

        public float CurrentSpeed => Mathf.Abs(rb.linearVelocity.x);
        public bool IsGrounded => isGrounded;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        public void SetMoveInput(Vector2 input)
        {
            moveInput = input;
        }

        private void FixedUpdate()
        {
            CheckGrounded();
            ApplyHorizontalMovement();
            HandleFacingDirection();
        }

        private void CheckGrounded()
        {
            if (groundCheckPoint != null)
            {
                isGrounded = Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, groundLayer);
            }
        }

        private void ApplyHorizontalMovement()
        {
            float targetSpeed = moveInput.x * walkSpeed;
            // Unity 6: rb.linearVelocity replaces rb.velocity
            rb.linearVelocity = new Vector2(targetSpeed, rb.linearVelocity.y);
        }

        private void HandleFacingDirection()
        {
            if (moveInput.x > 0.01f && !isFacingRight) Flip();
            else if (moveInput.x < -0.01f && isFacingRight) Flip();
        }

        private void Flip()
        {
            isFacingRight = !isFacingRight;
            Vector3 scale = transform.localScale;
            scale.x *= -1f;
            transform.localScale = scale;
        }
    }
}
```

---

## 3. Animator State Machine & Transition Rules

- **Use Hashed Parameter IDs**: Never pass string literals like `animator.SetFloat("Speed", ...)` inside `Update()`.
  ```csharp
  private static readonly int SpeedHash = Animator.StringToHash("Speed");
  private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
  ```
- **Has Exit Time**:
  - `Idle` -> `Walk`: `Has Exit Time: Unchecked`, `Transition Duration: 0.1s`. Immediate response to player input.
  - `Attack` / `Interact` -> `Idle`: `Has Exit Time: Checked`, allowing the motion to complete before returning to neutral.
- **Damping & Blending**: Use Blend Trees for smooth walk-to-run transitions based on a normalized `Speed` float parameter.

---

## 4. Input Locking & Cutscene Transitions

To prevent character sliding or teleporting when transitioning between cutscene and gameplay:

1. **Locking Input**:
   - Zero out `moveInput` immediately before disabling control:
     ```csharp
     motor.SetMoveInput(Vector2.zero);
     rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
     animator.SetFloat(SpeedHash, 0f);
     ```
2. **Preventing Teleportation**:
   - When Timeline finishes, never force a sudden `transform.position = ...` unless intentional.
   - Let the Cinemachine brain blend the camera smoothly while the player remains grounded in their cutscene arrival position.
