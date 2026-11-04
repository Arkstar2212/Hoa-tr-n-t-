using UnityEngine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Additive end-of-map transition trigger placed at the rightmost boundary of
    /// "New Scene 1_Overhaul.unity". When Bé An reaches the end of the Overhaul map:
    /// 1. Cleanly locks player input & stops velocity so An doesn't clip or fall.
    /// 2. Saves progression into HoaVaMauSaveSystem for the 3D Ca Mau 1954 Village chapter.
    /// 3. Fades to pure black screen and launches the post-Start-Menu BotW Loading Screen
    ///    to asynchronously load "CaMau_1954_Village".
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public class OverhaulEndMapTransitionTrigger : MonoBehaviour
    {
        [Header("Destination Scene")]
        [SerializeField] private string targetSceneName = "CaMau_1954_Village";
        [SerializeField] private string loadingCornerLabel = "ĐANG TẢI LÀNG CÀ MAU 1954...";

        [Header("End-of-Map Coordinate Fallback")]
        [Tooltip("Triggers automatically either via BoxCollider trigger or if An passes this X threshold.")]
        [SerializeField] private bool enablePositionXCheck = true;
        [SerializeField] private float triggerThresholdX = 234.0f;

        [Header("Save Metadata on Transition")]
        [SerializeField] private string savedChapterTitle = "Chương 2: Xóm Rạch Gốc 1954";
        [SerializeField] private string savedLocationName = "Làng Ven Sông Cà Mau 1954";
        [SerializeField] private Vector3 destinationSpawnPos = new Vector3(-7.0f, 0.55f, 182.0f);

        private PlayerController cachedPlayer;
        private bool hasTriggered = false;

        public void ConfigureTrigger(float thresholdX, string destScene = "CaMau_1954_Village")
        {
            triggerThresholdX = thresholdX;
            if (!string.IsNullOrEmpty(destScene)) targetSceneName = destScene;
        }

        private void Awake()
        {
            var box = GetComponent<BoxCollider>();
            if (box != null) box.isTrigger = true;
        }

        private void Start()
        {
            cachedPlayer = Object.FindAnyObjectByType<PlayerController>();
        }

        private void Update()
        {
            if (hasTriggered || !enablePositionXCheck) return;

            if (cachedPlayer == null)
            {
                cachedPlayer = Object.FindAnyObjectByType<PlayerController>();
                if (cachedPlayer == null) return;
            }

            if (cachedPlayer.enabled && cachedPlayer.transform.position.x >= triggerThresholdX)
            {
                ExecuteEndMapTransition(cachedPlayer.gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasTriggered) return;

            var player = other.GetComponentInParent<PlayerController>();
            if (player != null || other.CompareTag("Player"))
            {
                ExecuteEndMapTransition(player != null ? player.gameObject : other.gameObject);
            }
        }

        private void ExecuteEndMapTransition(GameObject playerObj)
        {
            if (hasTriggered) return;
            hasTriggered = true;

            Debug.Log($"<color=#34d399>[OverhaulEndMapTransitionTrigger]</color> Bé An reached end of Overhaul map (X >= {triggerThresholdX:F1}m). Fading to black -> BotW Loading Screen -> '{targetSceneName}'.");

            // 1. Cleanly lock 2.5D PlayerController & zero rigidbody motion
            if (playerObj != null)
            {
                var pc = playerObj.GetComponent<PlayerController>();
                if (pc != null)
                {
                    pc.enabled = false;
                }

                var rb = playerObj.GetComponent<Rigidbody>();
                if (rb != null && !rb.isKinematic)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                var anim = playerObj.GetComponentInChildren<Animator>();
                if (anim != null)
                {
                    anim.SetFloat("Speed", 0f);
                }
            }

            // 2. Save progress so Continue from Main Menu knows player reached Ca Mau 1954 3D Village
            int activeSlot = HoaVaMauSaveSystem.ActiveSlotIndex;
            HoaVaMauSaveSystem.SaveCurrentGameState(
                activeSlot,
                savedChapterTitle,
                savedLocationName,
                checkpoint: 10,
                pos: destinationSpawnPos,
                captureThumbnail: false,
                sceneName: targetSceneName
            );

            // 3. Fade black screen -> show post-Start-Menu BotW Loading Screen -> async load CaMau_1954_Village
            var loader = GetComponentInChildren<SceneTransitionBotWLoader>();
            if (loader != null)
            {
                loader.BeginTransition(targetSceneName, loadingCornerLabel);
            }
            else
            {
                SceneTransitionBotWLoader.LoadSceneWithBotWScreen(targetSceneName, loadingCornerLabel);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.98f, 0.78f, 0.25f, 0.35f);
            var box = GetComponent<BoxCollider>();
            if (box != null)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.DrawWireCube(box.center, box.size);
            }
        }
    }
}
