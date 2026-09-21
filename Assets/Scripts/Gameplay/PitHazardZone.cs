using UnityEngine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Guarantees instant respawn upon touching pit floor or bottomless void.
    /// </summary>
    public class PitHazardZone : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponentInParent<PlayerController>();
            if (player != null && CheckpointManager.Instance != null)
            {
                CheckpointManager.Instance.OnPlayerCaught();
            }
        }
    }
}
