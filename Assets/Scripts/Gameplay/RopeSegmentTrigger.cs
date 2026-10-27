using UnityEngine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Trigger component attached to each rope grab zone.
    /// Giúp Player nhận biết đang ở trong tầm với của dây thừng để bấm [F] leo dây.
    /// </summary>
    public class RopeSegmentTrigger : MonoBehaviour
    {
        private RopeController controller;
        private int index;

        public RopeController Controller => controller;
        public int Index => index;

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
                player.RegisterNearbyRope(controller, index);

                // Tự động bám dây khi đang nhảy / trên không mà chạm vào dây
                if (player.CurrentState == PlayerState.Airborne && player.CanAttachToRope)
                {
                    controller.OnPlayerTouchSegment(player, index);
                }
            }
        }

        private void OnTriggerStay(Collider other)
        {
            var player = other.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                player.RegisterNearbyRope(controller, index);

                // Tự động bám dây khi đang rơi / trên không
                if (player.CurrentState == PlayerState.Airborne && player.CanAttachToRope)
                {
                    controller.OnPlayerTouchSegment(player, index);
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var player = other.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                player.UnregisterNearbyRope(controller);
            }
        }
    }
}
