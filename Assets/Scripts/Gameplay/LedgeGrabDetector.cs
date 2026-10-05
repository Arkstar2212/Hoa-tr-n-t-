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
        [SerializeField] private float platformLandForward = 0.72f;

        /// <summary>
        /// Attempts to detect a valid ledge in the facing direction.
        /// </summary>
        public bool TryDetectLedge(float facingDir, out Vector3 hangPos, out Vector3 platformLandPos)
        {
            hangPos = Vector3.zero;
            platformLandPos = Vector3.zero;

            Vector3 origin = transform.position;
            Vector3 forward = new Vector3(facingDir, 0f, 0f);

            // 1. Check chest level — MUST hit a solid wall
            Vector3 chestOrigin = origin + Vector3.up * chestHeight;
            if (!Physics.Raycast(chestOrigin, forward, out RaycastHit wallHit, forwardCheckDist, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            // Wall must be roughly vertical
            if (Mathf.Abs(wallHit.normal.y) > 0.35f) return false;

            // 2. Check head/above level — MUST be clear space to grab
            Vector3 headOrigin = origin + Vector3.up * (headHeight + 0.3f);
            if (Physics.Raycast(headOrigin, forward, forwardCheckDist + 0.2f, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                return false; // Obstructed overhead
            }

            // 3. Cast downward onto the top edge of the platform
            Vector3 downOrigin = chestOrigin + forward * (wallHit.distance + 0.25f) + Vector3.up * 1.2f;
            if (!Physics.Raycast(downOrigin, Vector3.down, out RaycastHit ledgeHit, 1.8f, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            // Platform top must be walkable (upward normal)
            if (ledgeHit.normal.y < 0.7f) return false;

            // Compute exact edge point
            float wallX = wallHit.point.x;
            float topY = ledgeHit.point.y;

            // With normalized climb animation, root is placed directly on platform surface.
            // Hands reach to wall edge at t=0, and character finishes standing at this exact spot.
            hangPos = new Vector3(wallX + facingDir * platformLandForward, topY, 0f);
            platformLandPos = hangPos;

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
