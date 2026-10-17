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
        [SerializeField] private LayerMask obstacleMask = ~0; // Default to all layers

        [Header("Position Offsets")]
        [SerializeField] private float wallHangOffset = 0.28f;
        [SerializeField] private float platformLandForward = 0.42f;

        /// <summary>
        /// Attempts to detect a valid ledge in the facing direction.
        /// </summary>
        public bool TryDetectLedge(float facingDir, out Vector3 hangPos, out Vector3 platformLandPos)
        {
            hangPos = Vector3.zero;
            platformLandPos = Vector3.zero;

            Vector3 origin = transform.position;
            Vector3 forward = new Vector3(facingDir, 0f, 0f);

            // 1. Quét tìm bề mặt tường thẳng đứng ở tầm ngực / vai
            Vector3 chestOrigin = origin + Vector3.up * chestHeight;
            Vector3 waistOrigin = origin + Vector3.up * (chestHeight * 0.65f);

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
            if (!Physics.Raycast(downOrigin, Vector3.down, out RaycastHit ledgeHit, 2.2f, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            // Mặt trên của gờ phải là mặt phẳng đi được (normal hướng lên)
            if (ledgeHit.normal.y < 0.7f) return false;

            float topY = ledgeHit.point.y;

            // Chỉ bám gờ khi mép tường nằm đúng tầm tay với (cao hơn bàn chân từ 0.45m đến 1.55m)
            float ledgeHeightAboveFeet = topY - origin.y;
            if (ledgeHeightAboveFeet < 0.45f || ledgeHeightAboveFeet > 1.55f)
            {
                return false;
            }

            // Vị trí bắt đầu bám (hangPos): Áp sát ngay mặt trước của vách tường (wallX - facingDir * wallHangOffset),
            // giữ nguyên độ cao tiếp xúc tự nhiên dưới mép gờ để tay bám đúng vào góc gờ (wallX, topY) mà không bị dịch chuyển tức thời.
            float hangX = wallX - facingDir * wallHangOffset;
            float hangY = Mathf.Clamp(origin.y, topY - 1.05f, topY - 0.50f);
            hangPos = new Vector3(hangX, hangY, 0f);

            // Vị trí kết thúc trèo (platformLandPos): Đứng vững trên mặt phẳng đỉnh platform, cách mép gờ 0.42m
            float landX = wallX + facingDir * platformLandForward;
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
