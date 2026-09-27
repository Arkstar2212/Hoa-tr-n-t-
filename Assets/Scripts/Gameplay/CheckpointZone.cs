using UnityEngine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Trigger zone that registers a new checkpoint with the CheckpointManager.
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

            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                isTriggered = true;
                Vector3 spawnPos = transform.position + respawnOffset;
                spawnPos.z = 0f;
                if (CheckpointManager.Instance != null)
                {
                    CheckpointManager.Instance.SetCheckpoint(spawnPos, checkpointIndex);
                }
            }
        }
    }
}
