using UnityEngine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// SOLID - Single Responsibility Principle:
    /// Quản lý toàn diện tương tác với hệ thống dây thừng (RopeController):
    /// bám dây êm ái, trèo lên/xuống linh hoạt mượt mà, đung đưa con lắc cộng hưởng tự nhiên (Planet of Lana standard),
    /// không bị giật lật mặt 180 độ bất thường, và bật nhảy kế thừa trọn vẹn động lượng tiếp tuyến.
    /// </summary>
    public class PlayerRopeHandler
    {
        private readonly Transform transform;
        private readonly CharacterController charController;
        private RopeController currentRope;
        private float ropeAttachCooldown = 0f;

        public float HandOffsetHeight { get; set; } = 1.20f; // Khoảng cách từ gốc chân lên tay nắm dây (chuẩn Boy_Character)
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

            // Bàn tay bám đúng tại attachPos, thân người nằm phía dưới
            transform.position = new Vector3(attachPos.x, attachPos.y - HandOffsetHeight, 0f);

            // Xoay mặt theo hướng nhảy tới nếu có vận tốc ngang rõ rệt
            if (Mathf.Abs(incomingVelocity.x) > 0.25f)
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

            // 1. Leo trèo lên / xuống dọc theo chiều dài dây thừng với tốc độ mượt mà tự nhiên (1.25m/s - 1.35m/s)
            if (Mathf.Abs(verticalInput) > 0.12f)
            {
                float climbSpeed = (verticalInput > 0f) ? 1.25f : 1.35f;
                currentRope.Climb(player, verticalInput * climbSpeed * dt);
            }

            // 2. Tiếp nhận input đung đưa con lắc & đổi hướng mặt tự nhiên (Chuẩn Planet of Lana)
            if (currentRope != null) currentRope.ApplySwingInput(horizontalInput, dt);

            // Chỉ đổi hướng mặt khi người chơi cố ý nhấn giữ rẽ hướng (ngưỡng 0.35f), KHÔNG tự động đảo mặt theo chu kỳ vung dây
            if (Mathf.Abs(horizontalInput) > 0.35f)
            {
                player.SetFacingDirection(Mathf.Sign(horizontalInput));
            }

            // 3. Cập nhật vị trí và góc xoay nhân vật theo đốt dây đang bám (Planet of Lana standard)
            Vector3 ropeGrabPos = currentRope.GetPlayerRopePosition(player);
            float ropeAngle = currentRope.CurrentAngleDegrees;
            float angVel = currentRope.AngularVelocity;

            // Độ trễ quán tính thân người mềm mại theo luồng gió và chuyển động con lắc
            float lagAngle = -Mathf.Clamp(angVel * 2.2f, -10f, 10f);
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
                    // Hướng phóng: ưu tiên phím A/D người chơi đang giữ, nếu buông phím thì theo đà vung dây hoặc hướng mặt
                    float launchDir = Mathf.Abs(horizontalInput) > 0.15f
                        ? Mathf.Sign(horizontalInput)
                        : (Mathf.Abs(ropeTanVel.x) > 0.3f ? Mathf.Sign(ropeTanVel.x) : player.FacingDirection);

                    // Kế thừa trọn vẹn động lượng tiếp tuyến của con lắc kết hợp sức bật chân
                    float baseSpeedX = 4.0f;
                    float swingBonusX = Mathf.Abs(ropeTanVel.x) * 1.35f;
                    float launchSpeedX = Mathf.Clamp(baseSpeedX + swingBonusX, 4.2f, 9.5f);
                    float launchSpeedY = (player != null && player.Motor != null) ? Mathf.Max(player.Motor.JumpForce, 6.6f) : 6.6f;

                    releaseVelocity = new Vector3(launchDir * launchSpeedX, launchSpeedY, 0f);
                    player.SetFacingDirection(launchDir);
                }

                ropeAttachCooldown = 0.32f;
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
