using UnityEngine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// SOLID - Single Responsibility Principle:
    /// Chuyên trách quản lý tương tác với hệ thống dây thừng (RopeController):
    /// bám dây, trèo lên/xuống, lấy đà đung đưa con lắc và bật nhảy phóng qua vực.
    /// </summary>
    public class PlayerRopeHandler
    {
        private readonly Transform transform;
        private readonly CharacterController charController;
        private RopeController currentRope;
        private float ropeAttachCooldown = 0f;

        public float HandOffsetHeight { get; set; } = 1.45f; // Khoảng cách từ gốc tọa độ chân lên đến bàn tay nắm dây
        public float CurrentRopeAngle { get; private set; } = 0f; // Góc nghiêng của dây thừng tại điểm bám

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

        public bool AttachToRope(PlayerController player, RopeController rope, Vector3 attachPos)
        {
            if (ropeAttachCooldown > 0f) return false;
            if (player.CurrentState == PlayerState.RopeGrab || player.CurrentState == PlayerState.RopeClimb) return false;
            if (player.CurrentState == PlayerState.LedgeClimb) return false;

            currentRope = rope;
            if (charController != null) charController.enabled = false;

            // Bàn tay bám đúng tại attachPos, thân người và chân nằm phía dưới
            transform.position = new Vector3(attachPos.x, attachPos.y - HandOffsetHeight, 0f);
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

            // Leo trèo lên / xuống dọc theo chiều dài dây thừng với tốc độ tự nhiên
            if (Mathf.Abs(verticalInput) > 0.1f)
            {
                float climbSpeed = (verticalInput > 0f) ? 2.2f : 2.8f;
                currentRope.Climb(player, verticalInput * climbSpeed * dt);
            }

            // Lấy đà đung đưa con lắc khi bấm A / D
            if (Mathf.Abs(horizontalInput) > 0.05f)
            {
                currentRope.ApplySwingInput(horizontalInput, dt);
            }

            // Cập nhật vị trí nhân vật theo đốt dây đang bám
            Vector3 ropeGrabPos = currentRope.GetPlayerRopePosition(player);
            transform.position = new Vector3(ropeGrabPos.x, ropeGrabPos.y - HandOffsetHeight, 0f);

            // Bật nhảy phóng khỏi dây thừng
            if (jumpPressed)
            {
                float jumpDir = horizontalInput != 0 ? Mathf.Sign(horizontalInput) : player.FacingDirection;
                Vector3 ropeReleaseVel = currentRope.GetReleaseVelocity();
                releaseVelocity = ropeReleaseVel + new Vector3(jumpDir * 4.2f, 8.6f * 0.95f, 0f);
                ropeAttachCooldown = 0.50f;
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

            CurrentRopeAngle = 0f;

            if (charController != null) charController.enabled = true;

            if (Mathf.Abs(releaseVelocity.x) > 0.1f)
            {
                transform.position += new Vector3(Mathf.Sign(releaseVelocity.x) * 0.40f, 0.12f, 0f);
            }
        }

        public void SetCooldown(float duration)
        {
            ropeAttachCooldown = duration;
        }
    }
}
