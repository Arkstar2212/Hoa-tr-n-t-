using UnityEngine;
using System.Collections;
using HoaVaMau.Test3D;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Manages player checkpoints and instantaneous failure respawn (Planet of Lana style).
    /// Supports both 2.5D PlayerController (Overhaul) and 3D Player3DController (CaMau_1954_Village).
    /// </summary>
    public class CheckpointManager : MonoBehaviour
    {
        public static CheckpointManager Instance { get; private set; }

        [Header("Respawn Settings")]
        [SerializeField] private Vector3 currentCheckpointPos = new Vector3(0.0f, -1.77f, 0f);
        [SerializeField] private float fallDeathY = -1.5f;

        private Vector3 initialSpawnPosition = new Vector3(0.0f, -1.77f, 0f);
        private int currentCheckpointIndex = 0;
        private PlayerController player;
        private Player3DController player3D;
        private EnemyController[] enemies;
        private bool isRespawning = false;

        public Vector3 CurrentCheckpointPos => currentCheckpointPos;
        public int CurrentCheckpointIndex => currentCheckpointIndex;

        public void ConfigureDefaults(Vector3 initialPos, float deathY)
        {
            currentCheckpointPos = initialPos;
            initialSpawnPosition = initialPos;
            fallDeathY = deathY;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            initialSpawnPosition = currentCheckpointPos;
        }

        private void Start()
        {
            FindReferences();
        }

        public void FindReferences()
        {
            if (player == null)
            {
                player = Object.FindAnyObjectByType<PlayerController>();
                if (player != null && currentCheckpointPos == Vector3.zero)
                {
                    currentCheckpointPos = player.transform.position;
                    initialSpawnPosition = currentCheckpointPos;
                }
            }
            if (player == null && player3D == null)
            {
                player3D = Object.FindAnyObjectByType<Player3DController>();
                if (player3D != null && (currentCheckpointPos == Vector3.zero || currentCheckpointPos == new Vector3(0f, -1.77f, 0f)))
                {
                    currentCheckpointPos = player3D.transform.position;
                    initialSpawnPosition = currentCheckpointPos;
                }
            }
            enemies = Object.FindObjectsByType<EnemyController>();
        }

        private void Update()
        {
            if (isRespawning) return;
            if (PauseMenuController.Instance != null && PauseMenuController.Instance.IsPaused) return;

            // [R] Quick Respawn key
            bool rPressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) rPressed = true;
#else
            if (Input.GetKeyDown(KeyCode.R)) rPressed = true;
#endif
            if (rPressed)
            {
                OnPlayerCaught();
                return;
            }

            if (player != null && player.transform.position.y < fallDeathY)
            {
                OnPlayerCaught();
            }
            else if (player == null && player3D != null && player3D.transform.position.y < fallDeathY)
            {
                OnPlayerCaught();
            }
        }

        public void SetCheckpoint(Vector3 pos, int index = 0)
        {
            bool is3D = (player == null && (player3D != null || Object.FindAnyObjectByType<Player3DController>() != null));
            currentCheckpointPos = is3D ? pos : new Vector3(pos.x, pos.y, 0f);
            if (index > currentCheckpointIndex)
            {
                currentCheckpointIndex = index;
            }

            if (DynamicDifficultyManager.Instance != null)
            {
                DynamicDifficultyManager.Instance.AdvanceCheckpoint(currentCheckpointIndex);
            }

            Debug.Log($"<color=#40b8a6>[CheckpointManager]</color> Checkpoint {currentCheckpointIndex} updated to: {currentCheckpointPos}");
        }

        public void RestartFromBeginning()
        {
            currentCheckpointPos = initialSpawnPosition;
            currentCheckpointIndex = 0;
            if (DynamicDifficultyManager.Instance != null)
            {
                DynamicDifficultyManager.Instance.currentCheckpointIndex = 0;
                DynamicDifficultyManager.Instance.failuresAtCurrentCheckpoint = 0;
                DynamicDifficultyManager.Instance.frustrationIndex = 0f;
            }
            OnPlayerCaught();
        }

        public void OnPlayerCaught()
        {
            if (isRespawning) return;
            StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            isRespawning = true;
            Debug.Log("<color=#e05a47>[CheckpointManager]</color> Player caught / fallen! Respawning at checkpoint...");

            if (DynamicDifficultyManager.Instance != null)
            {
                DynamicDifficultyManager.Instance.RecordPlayerFailure(currentCheckpointPos);
            }

            yield return new WaitForSeconds(0.2f);

            FindReferences();

            if (player != null)
            {
                player.RespawnAt(currentCheckpointPos);
            }
            else if (player3D != null)
            {
                player3D.RespawnAt(currentCheckpointPos);
                if (CaMau3DUIGuideController.Instance != null)
                {
                    CaMau3DUIGuideController.ShowNarrativeToast("ĐIỂM LƯU AN TOÀN", "An đã quay trở lại bờ đất an toàn.", 2.4f);
                }
            }

            if (Camera.main != null)
            {
                var camCtrl = Camera.main.GetComponent<Cinematic2DCamera>();
                if (camCtrl != null)
                {
                    camCtrl.SnapToTarget();
                }
            }

            if (enemies == null || enemies.Length == 0)
            {
                enemies = Object.FindObjectsByType<EnemyController>();
            }

            foreach (var enemy in enemies)
            {
                if (enemy != null)
                {
                    enemy.ResetToDefaultState();
                }
            }

            yield return new WaitForSeconds(0.3f);
            isRespawning = false;
        }
    }
}
