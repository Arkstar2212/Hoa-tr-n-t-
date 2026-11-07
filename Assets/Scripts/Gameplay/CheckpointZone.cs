using UnityEngine;
using HoaVaMau.Test3D;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Trigger zone that registers a new checkpoint with the CheckpointManager.
    /// Auto-saves slot progression and triggers live portrait screenshot capture (Planet of Lana standard).
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
                else
                {
                    int activeSlot = HoaVaMauSaveSystem.ActiveSlotIndex;
                    string activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                    string chapter = (activeScene == "CaMau_1954_Village") ? "Chương 2: Xóm Rạch Gốc 1954" : "Chương 1: Đất Đỏ Cà Mau";
                    string location = (activeScene == "CaMau_1954_Village") ? $"Làng Ven Sông (Trạm {checkpointIndex})" : $"Rừng Sớm Cà Mau (Trạm {checkpointIndex})";
                    HoaVaMauSaveSystem.SaveCurrentGameState(activeSlot, chapter, location, checkpointIndex, spawnPos, true, activeScene);
                    ThumbnailCaptureRuntime.CaptureSlot(activeSlot);
                    PlanetOfLanaStartMenuSystem.TriggerAutosave(location);
                }
            }
        }
    }
}
