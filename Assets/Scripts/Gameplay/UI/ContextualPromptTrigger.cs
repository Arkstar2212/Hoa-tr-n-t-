using UnityEngine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Contextual diegetic trigger for the Planet of Lana Prompt System.
    /// Places translucent, minimalist controller/keyboard glyphs in the world
    /// at key obstacle checkpoints (e.g. Rope, Ravine Jump, Fallen Trunk Crawl, Crate Grab).
    /// Immediately fades out when the player executes the matching action or exits the area.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ContextualPromptTrigger : MonoBehaviour
    {
        [Header("Prompt Configuration")]
        [SerializeField] private PlanetOfLanaPromptSystem.PromptType promptType = PlanetOfLanaPromptSystem.PromptType.Interact;
        [SerializeField] private Transform worldAnchor;
        [SerializeField] private bool showOnceOnly = true;
        [SerializeField] private bool hideOnMatchingInput = true;
        [SerializeField] private float overrideDuration = 0f; // 0 = stay active until input or exit

        [Header("State")]
        private bool hasTriggered = false;
        private bool isPlayerInside = false;

        public PlanetOfLanaPromptSystem.PromptType Type
        {
            get => promptType;
            set => promptType = value;
        }

        public Transform WorldAnchor
        {
            get => worldAnchor;
            set => worldAnchor = value;
        }

        public bool ShowOnceOnly
        {
            get => showOnceOnly;
            set => showOnceOnly = value;
        }

        private void Awake()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
            if (worldAnchor == null) worldAnchor = transform;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasTriggered && showOnceOnly) return;
            if (!IsPlayer(other)) return;

            isPlayerInside = true;
            if (PlanetOfLanaPromptSystem.Instance != null)
            {
                PlanetOfLanaPromptSystem.Instance.ShowPrompt(promptType, "", worldAnchor, overrideDuration);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsPlayer(other)) return;

            isPlayerInside = false;
            if (PlanetOfLanaPromptSystem.Instance != null)
            {
                PlanetOfLanaPromptSystem.Instance.HidePrompt();
            }
        }

        private void Update()
        {
            if (!isPlayerInside) return;
            if (!hideOnMatchingInput) return;

            if (HasMatchingInputPressed())
            {
                hasTriggered = true;
                if (PlanetOfLanaPromptSystem.Instance != null)
                {
                    PlanetOfLanaPromptSystem.Instance.HidePrompt();
                }
            }
        }

        private bool HasMatchingInputPressed()
        {
            switch (promptType)
            {
                case PlanetOfLanaPromptSystem.PromptType.Move:
                    return Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D) ||
                           Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow) ||
                           Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.35f;

                case PlanetOfLanaPromptSystem.PromptType.Jump:
                    return Input.GetKeyDown(KeyCode.Space) || Input.GetButtonDown("Jump");

                case PlanetOfLanaPromptSystem.PromptType.Crawl:
                    return Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.S) ||
                           Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.DownArrow);

                case PlanetOfLanaPromptSystem.PromptType.ClimbLedge:
                    return Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) ||
                           Input.GetKeyDown(KeyCode.Space);

                case PlanetOfLanaPromptSystem.PromptType.Interact:
                    return Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0);

                case PlanetOfLanaPromptSystem.PromptType.RopeGrab:
                    return Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Space) ||
                           Input.GetKeyDown(KeyCode.E);

                case PlanetOfLanaPromptSystem.PromptType.PushPull:
                    return Input.GetKeyDown(KeyCode.E) || Input.GetMouseButton(0);

                default:
                    return false;
            }
        }

        private bool IsPlayer(Collider col)
        {
            return col.CompareTag("Player") ||
                   col.GetComponent<PlayerController>() != null ||
                   col.name.Contains("Minh") ||
                   col.name.Contains("Player") ||
                   col.name.Contains("An");
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.35f);
            var col = GetComponent<BoxCollider>();
            if (col != null)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(col.center, col.size);
                Gizmos.DrawWireCube(col.center, col.size);
            }
        }
    }
}
