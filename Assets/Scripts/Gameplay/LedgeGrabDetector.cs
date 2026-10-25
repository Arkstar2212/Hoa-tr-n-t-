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

        public float LastDetectedWallX { get; private set; }
        public float LastDetectedTopY { get; private set; }
        public float WallHangOffset => wallHangOffset;

        /// <summary>
        /// Attempts to detect a valid ledge in the facing direction.
        /// </summary>
        public bool TryDetectLedge(float facingDir, out Vector3 hangPos, out Vector3 platformLandPos)
        {
            hangPos = Vector3.zero;
            platformLandPos = Vector3.zero;

            Vector3 origin = transform.position;
            Vector3 forward = new Vector3(facingDir, 0f, 0f);

            // 1. Quét tìm bề mặt tường thẳng đứng ở tầm vai / ngực / bụng
            Vector3 shoulderOrigin = origin + Vector3.up * (chestHeight * 1.25f);
            Vector3 chestOrigin = origin + Vector3.up * chestHeight;
            Vector3 waistOrigin = origin + Vector3.up * (chestHeight * 0.65f);

            RaycastHit wallHit;
            bool hitWall = Physics.Raycast(shoulderOrigin, forward, out wallHit, forwardCheckDist, obstacleMask, QueryTriggerInteraction.Ignore)
                        || Physics.Raycast(chestOrigin, forward, out wallHit, forwardCheckDist, obstacleMask, QueryTriggerInteraction.Ignore)
                        || Physics.Raycast(waistOrigin, forward, out wallHit, forwardCheckDist, obstacleMask, QueryTriggerInteraction.Ignore);

            if (!hitWall)
            {
                return false;
            }

            // Tường phải gần như thẳng đứng
            if (Mathf.Abs(wallHit.normal.y) > 0.38f) return false;

            // 2. Kiểm tra khoảng trống phía trên đầu — phải thông thoáng để nhân vật trèo lên
            Vector3 headOrigin = origin + Vector3.up * (headHeight + 0.35f);
            if (Physics.Raycast(headOrigin, forward, wallHit.distance + 0.35f, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                return false; // Vướng trần phía trên
            }

            // 3. Bắn tia từ trên xuống để tìm chính xác mặt phẳng đỉnh gờ (topY)
            float wallX = wallHit.point.x;
            Vector3 downOrigin = new Vector3(wallX + facingDir * 0.22f, origin.y + headHeight + 0.55f, 0f);
            if (!Physics.Raycast(downOrigin, Vector3.down, out RaycastHit ledgeHit, 2.4f, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            // Mặt trên của gờ phải là mặt phẳng đi được (normal hướng lên)
            if (ledgeHit.normal.y < 0.65f) return false;

            float topY = ledgeHit.point.y;

            // Chỉ bám gờ khi mép tường nằm trong tầm tay với (cao hơn bàn chân từ 0.35m đến 1.70m)
            float ledgeHeightAboveFeet = topY - origin.y;
            if (ledgeHeightAboveFeet < 0.35f || ledgeHeightAboveFeet > 1.70f)
            {
                return false;
            }

            LastDetectedWallX = wallX;
            LastDetectedTopY = topY;

            // Vị trí bắt đầu bám (hangPos): Áp sát mặt trước của vách tường (wallX - facingDir * wallHangOffset),
            // cố định độ cao chuẩn (topY - 0.96m) để đôi bàn tay của animation LedgeHang và frame bắt đầu của Climb
            // khớp 100% vào đúng góc gờ vách đá (wallX, topY).
            float hangX = wallX - facingDir * wallHangOffset;
            float hangY = topY - 0.96f;
            hangPos = new Vector3(hangX, hangY, 0f);

            // Vị trí kết thúc trèo (platformLandPos): Đứng vững trên mặt phẳng đỉnh platform
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
