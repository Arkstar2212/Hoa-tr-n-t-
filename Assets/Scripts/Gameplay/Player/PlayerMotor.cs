using UnityEngine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// SOLID - Single Responsibility Principle:
    /// Chuyên trách động học chuyển động vật lý (Physics & Locomotion Motor):
    /// - Quản lý gia tốc / giảm tốc và tốc độ mục tiêu (Đi bộ, Chạy nhanh, Bò, Đẩy kéo).
    /// - Quản lý trọng lực biến thiên, Coyote Time, Jump Buffering, và chiều cao nhảy biến thiên.
    /// - Di chuyển CharacterController trên mặt phẳng 2D (trục X và Y, Z cố định = 0).
    /// </summary>
    public class PlayerMotor
    {
        private readonly CharacterController charController;

        // Configuration
        public float WalkSpeed { get; set; } = 2.4f;
        public float SprintSpeed { get; set; } = 4.2f; // Tốc độ chạy vừa phải, không quá nhanh
        public float RunningJumpBoostMultiplier { get; set; } = 1.45f; // Tăng tầm bay ngang khi chạy lấy đà
        public float CrouchSpeed { get; set; } = 1.8f;
        public float CrawlSpeed { get; set; } = 1.2f;
        public float PushPullSpeed { get; set; } = 2.1f;
        public float Acceleration { get; set; } = 26f;
        public float Deceleration { get; set; } = 30f;

        public float JumpForce { get; set; } = 6.2f; // Planet of Lana grounded vertical jump height (~0.85m)
        public float MinJumpTime { get; set; } = 0.12f;
        public float Gravity { get; set; } = 22.5f;
        public float FallGravityMultiplier { get; set; } = 1.4f;
        public float TerminalVelocity { get; set; } = 14.5f;
        public float CoyoteTimeDuration { get; set; } = 0.16f;
        public float JumpBufferDuration { get; set; } = 0.15f;

        // Runtime Physics State
        public Vector3 Velocity { get; set; }
        public float CoyoteTimer { get; set; }
        public float JumpBufferTimer { get; set; }
        public float JumpTimeElapsed { get; set; }
        public bool IsMomentumJump { get; private set; }
        public bool IsGrounded => charController != null && charController.isGrounded;

        public PlayerMotor(CharacterController charController)
        {
            this.charController = charController;
            if (this.charController != null)
            {
                this.charController.skinWidth = 0.04f;
                this.charController.stepOffset = 0.35f;
                this.charController.minMoveDistance = 0f;
            }
        }

        public void TickTimers(float dt)
        {
            if (JumpBufferTimer > 0f) JumpBufferTimer -= dt;
            if (CoyoteTimer > 0f) CoyoteTimer -= dt;
        }

        public void QueueJump()
        {
            JumpBufferTimer = JumpBufferDuration;
        }

        public bool TryExecuteJump(bool isCrawling, bool isSprinting, float horizontalInput, out bool jumped)
        {
            jumped = false;
            if (JumpBufferTimer > 0f && CoyoteTimer > 0f && !isCrawling)
            {
                JumpBufferTimer = 0f;
                CoyoteTimer = 0f;
                JumpTimeElapsed = 0f;
                Vector3 vel = Velocity;
                vel.y = JumpForce;

                // Nếu chạy nhảy lấy đà (sprint + di chuyển ngang): Tăng mạnh xung lượng ngang để nhảy xa hơn hẳn
                if (isSprinting && Mathf.Abs(horizontalInput) > 0.1f)
                {
                    float dir = Mathf.Sign(horizontalInput);
                    vel.x = dir * (SprintSpeed * RunningJumpBoostMultiplier);
                    IsMomentumJump = true;
                }
                else
                {
                    IsMomentumJump = false;
                }

                Velocity = vel;
                jumped = true;
                return true;
            }
            return false;
        }

        public bool TryExecuteJump(bool isCrawling, out bool jumped)
        {
            return TryExecuteJump(isCrawling, false, 0f, out jumped);
        }

        public void ApplyGravity(bool grounded, bool jumpHeld, float dt)
        {
            Vector3 vel = Velocity;

            if (grounded)
            {
                CoyoteTimer = CoyoteTimeDuration;
                vel.y = -4.5f; // Lực ghì bám mặt đất dốc (ramp) để khi chạy xuống dốc không bị hẫng airborne
                IsMomentumJump = false;
            }
            else
            {
                float activeGravity = Gravity;
                if (vel.y > 0f)
                {
                    JumpTimeElapsed += dt;
                    if (!jumpHeld && JumpTimeElapsed > MinJumpTime)
                    {
                        activeGravity *= 1.8f;
                    }
                }
                else
                {
                    activeGravity *= FallGravityMultiplier;
                }

                vel.y -= activeGravity * dt;
                if (vel.y < -TerminalVelocity) vel.y = -TerminalVelocity;
            }

            Velocity = vel;
        }

        public void ApplyHorizontalMovement(float horizontalInput, float currentSpeed, float dt)
        {
            Vector3 vel = Velocity;
            float targetVelX = horizontalInput * currentSpeed;

            // Nếu đang trong cú nhảy lấy đà trên không: bảo toàn đà bay ngang
            if (IsMomentumJump && !IsGrounded && Mathf.Abs(vel.x) > currentSpeed)
            {
                float airDecel = 3.2f;
                vel.x = Mathf.MoveTowards(vel.x, targetVelX, airDecel * dt);
            }
            else
            {
                float accelRate = (Mathf.Abs(horizontalInput) > 0.05f) ? Acceleration : Deceleration;
                vel.x = Mathf.MoveTowards(vel.x, targetVelX, accelRate * dt);
            }

            vel.z = 0f;
            Velocity = vel;

            if (charController != null && charController.enabled)
            {
                charController.Move(Velocity * dt);

                // Khóa cứng trục Z = 0 cho gameplay 2.5D
                Vector3 pos = charController.transform.position;
                if (Mathf.Abs(pos.z) > 0.0001f)
                {
                    pos.z = 0f;
                    charController.transform.position = pos;
                }
            }
        }

        public void MoveDirect(Vector3 displacement)
        {
            if (charController != null && charController.enabled)
            {
                charController.Move(displacement);
            }
        }

        public void Teleport(Vector3 position)
        {
            if (charController != null)
            {
                bool wasEnabled = charController.enabled;
                charController.enabled = false;
                charController.transform.position = new Vector3(position.x, position.y, 0f);
                Velocity = Vector3.zero;
                charController.enabled = wasEnabled;
            }
        }
    }
}
