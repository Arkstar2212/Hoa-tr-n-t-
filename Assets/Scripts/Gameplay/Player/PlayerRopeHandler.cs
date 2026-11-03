using UnityEngine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// SOLID - Single Responsibility Principle:
    /// Chuyên trách quản lý tương tác với hệ thống dây thừng (RopeController):
    /// bám dây, trèo lên/xuống, lấy đà đung đưa con lắc và bật nhảy phóng qua vực theo chuẩn Planet of Lana.
    /// </summary>
    public class PlayerRopeHandler
    {
        private readonly Transform transform;
        private readonly CharacterController charController;
        private RopeController currentRope;
        private float ropeAttachCooldown = 0f;

        public float HandOffsetHeight { get; set; } = 1.20f; // Khoảng cách từ gốc tọa độ chân lên đến bàn tay nắm dây (chuẩn 1.20m theo Boy_Character)
        public float CurrentRopeAngle => currentRope != null ? currentRope.CurrentAngleDegrees : 0f;

        public bool CanAttachToRope => ropeAttachCooldown <= 0f;
        public RopeController CurrentRope => currentRope;

        public PlayerRopeHandler(Transform transform, CharacterController charController)
        {
            this.transform = transform;
            this.charController = charController;
        }

        public void Tick(float dt)
        {
            if (ropeAttachCooldown > 0f) ropeAttachCooldown -= dt;
        }

        public bool AttachToRope(PlayerController player, RopeController rope, Vector3 attachPos, Vector3 incomingVelocity = default)
        {
            if (ropeAttachCooldown > 0f) return false;
            if (player.CurrentState == PlayerState.RopeGrab || player.CurrentState == PlayerState.RopeClimb) return false;
            if (player.CurrentState == PlayerState.LedgeClimb) return false;

            currentRope = rope;
            if (charController != null) charController.enabled = false;

            // Bàn tay bám đúng tại attachPos, thân người và chân nằm phía dưới
            transform.position = new Vector3(attachPos.x, attachPos.y - HandOffsetHeight, 0f);

            // Xoay mặt theo hướng nhảy tới nếu có vận tốc ngang
            if (Mathf.Abs(incomingVelocity.x) > 0.2f)
            {
                player.SetFacingDirection(Mathf.Sign(incomingVelocity.x));
            }

            return true;
        }

        public bool UpdateRopeMovement(
            PlayerController player,
            float horizontalInput,
            float verticalInput,
            bool jumpPressed,
            float dt,
            out Vector3 releaseVelocity)
        {
            releaseVelocity = Vector3.zero;

            if (currentRope == null)
            {
                DetachFromRope(player, Vector3.zero);
                return false;
            }

            // 1. Leo trèo lên / xuống dọc theo chiều dài dây thừng với tốc độ mượt mà tự nhiên
            if (Mathf.Abs(verticalInput) > 0.12f)
            {
                float climbSpeed = (verticalInput > 0f) ? 0.50f : 0.55f;
                currentRope.Climb(player, verticalInput * climbSpeed * dt);
            }

            // 2. Chuyển hướng mặt theo phím A / D (Chuẩn Planet of Lana: gọn lẹ, không momentum swing tích lũy phóng người)
            if (currentRope != null) currentRope.ApplySwingInput(horizontalInput, dt);
            if (Mathf.Abs(horizontalInput) > 0.05f)
            {
                // No momentum swing pumping
                player.SetFacingDirection(Mathf.Sign(horizontalInput));
            }
            else if (Mathf.Abs(currentRope.AngularVelocity) > 0.45f)
            {
                // Tự động hướng mặt theo hướng đang vung dây (Planet of Lana standard)
                player.SetFacingDirection(Mathf.Sign(currentRope.AngularVelocity));
            }

            // 3. Cập nhật vị trí và góc xoay nhân vật theo đốt dây đang bám (Planet of Lana standard)
            Vector3 ropeGrabPos = currentRope.GetPlayerRopePosition(player);
            float ropeAngle = currentRope.CurrentAngleDegrees;
            float angVel = currentRope.AngularVelocity;
            float lagAngle = -Mathf.Clamp(angVel * 3.5f, -14f, 14f);
            float totalTiltAngle = ropeAngle + lagAngle;

            Quaternion ropeTilt = Quaternion.AngleAxis(totalTiltAngle, Vector3.forward);
            Vector3 pivotOffset = ropeTilt * new Vector3(0f, HandOffsetHeight, 0f);

            float facingYaw = (player.FacingDirection > 0) ? 90f : -90f;
            transform.rotation = ropeTilt * Quaternion.Euler(0f, facingYaw, 0f);
            transform.position = ropeGrabPos - pivotOffset;

            // 4. Bật nhảy phóng khỏi dây thừng (Planet of Lana Athletic Leap Arc)
            if (jumpPressed)
            {
                Vector3 ropeTanVel = (currentRope != null) ? currentRope.GetReleaseVelocity() : Vector3.zero;

                // Thả tay rơi tự do nếu đang nhấn giữ phím xuống [S]
                if (verticalInput < -0.4f)
                {
                    releaseVelocity = Vector3.zero;
                }
                else
                {
                    // Phóng người theo đà đu dây: kế thừa vận tốc tiếp tuyến + xung lực bật nhảy
                    float launchDir = horizontalInput != 0f
                        ? Mathf.Sign(horizontalInput)
                        : (Mathf.Abs(ropeTanVel.x) > 0.4f ? Mathf.Sign(ropeTanVel.x) : player.FacingDirection);

                    float launchSpeedX = Mathf.Clamp(4.0f + Mathf.Abs(ropeTanVel.x) * 1.15f, 4.5f, 8.5f);
                    float launchSpeedY = (player != null && player.Motor != null) ? Mathf.Max(player.Motor.JumpForce, 6.2f) : 6.2f;

                    releaseVelocity = new Vector3(launchDir * launchSpeedX, launchSpeedY, 0f);
                    player.SetFacingDirection(launchDir);
                }

                ropeAttachCooldown = 0.35f;
                DetachFromRope(player, releaseVelocity);
                return false; // Released
            }

            return true; // Still on rope
        }

        public void DetachFromRope(PlayerController player, Vector3 releaseVelocity)
        {
            if (currentRope != null)
            {
                currentRope.ReleasePlayer(player);
                currentRope = null;
            }

            if (charController != null) charController.enabled = true;

            // Đặt lại góc nghiêng nhân vật thẳng đứng theo hướng mặt
            float facingYaw = (player.FacingDirection > 0) ? 90f : -90f;
            transform.rotation = Quaternion.Euler(0f, facingYaw, 0f);

            if (Mathf.Abs(releaseVelocity.x) > 0.1f)
            {
                transform.position += new Vector3(Mathf.Sign(releaseVelocity.x) * 0.35f, 0.10f, 0f);
            }
        }

        public void SetCooldown(float duration)
        {
            ropeAttachCooldown = duration;
        }
    }
}
