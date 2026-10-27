using UnityEngine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// SOLID - Single Responsibility Principle:
    /// Chuyên trách quản lý tư thế nhân vật (Đứng, Ngồi khom, Bò trườn)
    /// cùng với việc quét tia phát hiện trần thấp (Ceiling/Tunnel detection)
    /// và thay đổi kích thước Collider CharacterController tương ứng.
    /// </summary>
    public class PlayerPostureHandler
    {
        private readonly CharacterController charController;
        private readonly Transform transform;

        private readonly float standingHeight;
        private readonly float crouchHeight;
        private readonly float crawlHeight;
        private readonly LayerMask obstacleMask;

        public bool IsCrouching { get; private set; }
        public bool IsCrawling { get; private set; }

        public float StandingHeight => standingHeight;
        public float CrouchHeight => crouchHeight;
        public float CrawlHeight => crawlHeight;

        public PlayerPostureHandler(
            CharacterController charController,
            Transform transform,
            float standingHeight = 1.8f,
            float crouchHeight = 1.1f,
            float crawlHeight = 0.60f,
            LayerMask? obstacleMask = null)
        {
            this.charController = charController;
            this.transform = transform;
            this.standingHeight = standingHeight;
            this.crouchHeight = crouchHeight;
            this.crawlHeight = crawlHeight;
            this.obstacleMask = obstacleMask ?? ~0;
        }

        public void UpdatePostures(bool grounded, bool crouchPressed, float moveInputX, float facingDirection)
        {
            bool ceilingBlocked = CheckCeilingObstruction(out bool blockedFromCrouch);

            if (grounded)
            {
                if (crouchPressed)
                {
                    // Người chơi chủ động bấm phím ngồi (S)
                    IsCrouching = true;
                    // Bò nếu trần ép thấp (< 1.1m) hoặc đang di chuyển qua khe hẹp
                    IsCrawling = blockedFromCrouch || (Mathf.Abs(moveInputX) > 0.05f && ceilingBlocked);
                }
                else
                {
                    // Người chơi không bấm S:
                    // KHÔNG BAO GIỜ TỰ ĐỘNG BÒ KHI ĐI BÌNH THƯỜNG.
                    // Chỉ giữ nguyên tư thế ngồi/bò nếu trần nhà / hốc đá phía trên đang chặn không cho đứng dậy!
                    if (ceilingBlocked)
                    {
                        IsCrouching = true;
                        if (blockedFromCrouch)
                        {
                            IsCrawling = true;
                        }
                        else
                        {
                            IsCrawling = false;
                        }
                    }
                    else
                    {
                        // Đứng thẳng hoàn toàn
                        IsCrouching = false;
                        IsCrawling = false;
                    }
                }
            }
            else
            {
                IsCrouching = false;
                IsCrawling = false;
            }

            ApplyColliderDimensions();
        }

        public bool CheckCeilingObstruction(out bool blockedFromCrouch)
        {
            Vector3 origin = transform.position + Vector3.up * 0.35f;
            float radius = 0.22f;
            float checkDist = standingHeight - 0.35f;

            if (Physics.SphereCast(origin, radius, Vector3.up, out RaycastHit hit, checkDist, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                float clearance = hit.distance + 0.35f;
                // Nếu khoảng hở từ sàn đến trần nhỏ hơn chiều cao ngồi xổm (crouchHeight = 1.1m) thì buộc phải bò
                blockedFromCrouch = (clearance < crouchHeight + 0.05f);
                return true;
            }

            blockedFromCrouch = false;
            return false;
        }

        public bool CheckLowOverhangAhead(float moveDirX, float facingDirection)
        {
            float dirX = (Mathf.Abs(moveDirX) > 0.05f) ? Mathf.Sign(moveDirX) : facingDirection;
            Vector3 forward = new Vector3(dirX, 0f, 0f);

            float radius = (charController != null) ? charController.radius : 0.36f;
            float checkDist = radius + 0.55f;

            // Kiểm tra phía trước ở tầm đầu/ngực (chiều cao ngồi xổm/đứng)
            Vector3 highOrigin = transform.position + Vector3.up * 0.85f;
            bool highBlocked = Physics.SphereCast(highOrigin, 0.16f, forward, out _, checkDist, obstacleMask, QueryTriggerInteraction.Ignore);

            if (!highBlocked) return false;

            // Kiểm tra phía trước ở tầm sát đất (chiều cao bò trườn)
            Vector3 lowOrigin = transform.position + Vector3.up * (crawlHeight * 0.5f);
            bool lowBlocked = Physics.SphereCast(lowOrigin, 0.14f, forward, out _, checkDist, obstacleMask, QueryTriggerInteraction.Ignore);

            // Nếu phía trên vướng nhưng phía dưới thông thoáng -> Có hầm/khe thấp phía trước!
            return !lowBlocked;
        }

        public void ApplyColliderDimensions()
        {
            if (charController == null) return;

            float targetH = standingHeight;
            if (IsCrawling) targetH = crawlHeight;
            else if (IsCrouching) targetH = crouchHeight;

            charController.height = targetH;
            charController.center = new Vector3(0f, targetH * 0.5f, 0f);
        }
    }
}
