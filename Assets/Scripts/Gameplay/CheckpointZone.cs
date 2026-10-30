using UnityEngine;
using HoaVaMau.Test3D;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Trigger zone that registers a new checkpoint with the CheckpointManager.
    /// Supports both 2.5D PlayerController (Overhaul) and 3D Player3DController (CaMau_1954_Village).
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class CheckpointZone : MonoBehaviour
    {
        [SerializeField] private Vector3 respawnOffset = Vector3.zero;
        [SerializeField] public int checkpointIndex = 0;
        private bool isTriggered = false;

        private void Reset()
        {
            BoxCollider col = GetComponent<BoxCollider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isTriggered) return;

            PlayerController player2D = other.GetComponentInParent<PlayerController>();
            Player3DController player3D = other.GetComponentInParent<Player3DController>();

            if (player2D != null || player3D != null)
            {
                isTriggered = true;
                Vector3 spawnPos = transform.position + respawnOffset;
                if (player2D != null && player3D == null)
                {
                    spawnPos.z = 0f;
                }
                if (CheckpointManager.Instance != null)
                {
                    CheckpointManager.Instance.SetCheckpoint(spawnPos, checkpointIndex);
                }
            }
        }
    }
}
