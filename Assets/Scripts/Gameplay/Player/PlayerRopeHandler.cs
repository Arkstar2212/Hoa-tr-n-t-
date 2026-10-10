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
            transform.position = new Vector3(attachPos.x, attachPos.y, 0f);
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

            if (Mathf.Abs(verticalInput) > 0.1f)
            {
                float climbSpeed = 5.2f; // Leo dây nhanh và nhạy hơn
                currentRope.Climb(player, verticalInput * climbSpeed * dt);
            }

            if (Mathf.Abs(horizontalInput) > 0.1f)
            {
                currentRope.ApplySwingInput(horizontalInput, dt);
            }

            transform.position = currentRope.GetPlayerRopePosition(player);

            if (jumpPressed)
            {
                float jumpDir = horizontalInput != 0 ? Mathf.Sign(horizontalInput) : player.FacingDirection;
                releaseVelocity = currentRope.GetReleaseVelocity() + new Vector3(jumpDir * 3.6f * 1.4f, 8.6f * 1.05f, 0f);
                ropeAttachCooldown = 0.55f;
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

            if (Mathf.Abs(releaseVelocity.x) > 0.1f)
            {
                transform.position += new Vector3(Mathf.Sign(releaseVelocity.x) * 0.45f, 0.15f, 0f);
            }
        }

        public void SetCooldown(float duration)
        {
            ropeAttachCooldown = duration;
        }
    }
}
