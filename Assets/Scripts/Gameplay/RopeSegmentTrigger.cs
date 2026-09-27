using UnityEngine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Trigger component attached to each rope grab zone.
    /// </summary>
    public class RopeSegmentTrigger : MonoBehaviour
    {
        private RopeController controller;
        private int index;

        public void Init(RopeController ctrl, int idx)
        {
            controller = ctrl;
            index = idx;
        }

        private void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                controller.OnPlayerTouchSegment(player, index);
            }
        }
    }
}
