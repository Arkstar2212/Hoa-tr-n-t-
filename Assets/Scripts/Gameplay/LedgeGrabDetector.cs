using UnityEngine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Detects climbable ledges in front of the player when airborne.
    /// Emits raycasts to calculate exact hanging position and post-climb landing position.
    /// </summary>
    public class LedgeGrabDetector : MonoBehaviour
    {
        [Header("Detection Settings")]
        [SerializeField] private float forwardCheckDist = 0.85f;
        [SerializeField] private float headHeight = 1.6f;
        [SerializeField] private float chestHeight = 1.0f;
        [SerializeField] private float minLedgeReach = 0.60f;
        [SerializeField] private float maxLedgeReach = 2.15f;
        [SerializeField] private LayerMask obstacleMask = ~0; // Default to all layers

        [Header("Position Offsets")]
        [SerializeField] private float wallHangOffset = 0.24f;
        [SerializeField] private float platformLandForward = 0.46f;

        private Animator cachedAnimator;

        public Vector3 LastLedgeCorner { get; private set; }
        public bool HasValidLedgeCorner { get; private set; }

        private float GetCharacterScaleFactor()
        {
            if (cachedAnimator == null) cachedAnimator = GetComponent<Animator>();
            if (cachedAnimator == null) cachedAnimator = GetComponentInChildren<Animator>();

            if (cachedAnimator != null && cachedAnimator.isHuman)
            {
                // Minh (Boy_Character @ scale 1.15) has humanScale ~0.5408 => worldHumanScale ~0.622 => factor = 1.0
                float worldHumanScale = cachedAnimator.humanScale * Mathf.Max(0.1f, transform.lossyScale.y);
                if (worldHumanScale > 0.1f)
                {
                    return Mathf.Clamp(worldHumanScale / 0.622f, 0.70f, 1.85f);
                }
            }

            return Mathf.Clamp(transform.lossyScale.y / 1.15f, 0.75f, 1.75f);
        }

        /// <summary>
        /// Attempts to detect a valid ledge in the facing direction.
        /// </summary>
        public bool TryDetectLedge(float facingDir, out Vector3 hangPos, out Vector3 platformLandPos)
        {
            hangPos = Vector3.zero;
            platformLandPos = Vector3.zero;
            HasValidLedgeCorner = false;

            float scaleFactor = GetCharacterScaleFactor();
            Vector3 origin = transform.position;
            Vector3 forward = new Vector3(facingDir, 0f, 0f);

            // 1. Quét tìm bề mặt tường thẳng đứng ở tầm ngực / vai
            Vector3 chestOrigin = origin + Vector3.up * (chestHeight * Mathf.Lerp(1f, scaleFactor, 0.5f));
            Vector3 waistOrigin = origin + Vector3.up * (chestHeight * 0.55f * Mathf.Lerp(1f, scaleFactor, 0.5f));

            RaycastHit wallHit;
            bool hitWall = Physics.Raycast(chestOrigin, forward, out wallHit, forwardCheckDist, obstacleMask, QueryTriggerInteraction.Ignore)
                        || Physics.Raycast(waistOrigin, forward, out wallHit, forwardCheckDist, obstacleMask, QueryTriggerInteraction.Ignore);

            if (!hitWall)
            {
                return false;
            }

            // Tường phải gần như thẳng đứng
            if (Mathf.Abs(wallHit.normal.y) > 0.35f) return false;

            // 2. Kiểm tra khoảng trống phía trên đầu — phải thông thoáng để nhân vật trèo lên
            Vector3 headOrigin = origin + Vector3.up * (headHeight + 0.3f);
            if (Physics.Raycast(headOrigin, forward, wallHit.distance + 0.35f, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                return false; // Vướng trần phía trên
            }

            // 3. Bắn tia từ trên xuống để tìm chính xác mặt phẳng đỉnh gờ (topY)
            float wallX = wallHit.point.x;
            Vector3 downOrigin = new Vector3(wallX + facingDir * 0.22f, origin.y + headHeight + 0.45f, 0f);
            if (!Physics.Raycast(downOrigin, Vector3.down, out RaycastHit ledgeHit, 2.4f, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            // Mặt trên của gờ phải là mặt phẳng đi được (normal hướng lên)
            if (ledgeHit.normal.y < 0.7f) return false;

            float topY = ledgeHit.point.y;

            // Chỉ bám gờ khi mép tường nằm đúng tầm tay với khi nhảy / rơi (cao hơn bàn chân từ 0.6m đến 2.15m)
            float ledgeHeightAboveFeet = topY - origin.y;
            if (ledgeHeightAboveFeet < minLedgeReach || ledgeHeightAboveFeet > maxLedgeReach)
            {
                return false;
            }

            LastLedgeCorner = new Vector3(wallX, topY, 0f);
            HasValidLedgeCorner = true;

            // Vị trí bắt đầu bám (hangPos) chuẩn theo tỉ lệ xương thực tế của nhân vật:
            // Với Minh (scaleFactor=1.0), Hips cách mặt tường 0.24m và thấp hơn gờ 0.38m (root thấp hơn gờ 0.97m),
            // giúp hai bàn tay bám khít đúng góc gờ (wallX, topY) và chân phải đạp sát mặt tường thẳng đứng.
            float calibratedWallOffset = Mathf.Clamp(wallHangOffset, 0.20f, 0.25f) * scaleFactor;
            float calibratedHangDrop = 0.86f * scaleFactor;

            float hangX = wallX - facingDir * calibratedWallOffset;
            float hangY = topY - calibratedHangDrop;
            if (Physics.Raycast(new Vector3(hangX, topY - 0.05f, 0f), Vector3.down, out RaycastHit floorHit, calibratedHangDrop + 0.5f, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                hangY = Mathf.Max(hangY, floorHit.point.y);
            }
            hangPos = new Vector3(hangX, hangY, 0f);

            // Vị trí kết thúc trèo (platformLandPos): Đứng vững trên mặt phẳng đỉnh platform, không trượt xa quá mức
            float calibratedLandForward = Mathf.Clamp(platformLandForward, 0.38f, 0.48f) * scaleFactor;
            float landX = wallX + facingDir * calibratedLandForward;
            platformLandPos = new Vector3(landX, topY, 0f);

            return true;
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = transform.position;
            Gizmos.color = Color.green;
            Gizmos.DrawLine(origin + Vector3.up * chestHeight, origin + Vector3.up * chestHeight + Vector3.right * forwardCheckDist);
            Gizmos.color = Color.red;
            Gizmos.DrawLine(origin + Vector3.up * headHeight, origin + Vector3.up * headHeight + Vector3.right * forwardCheckDist);
        }
    }
}
