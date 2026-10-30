using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Contextual diegetic trigger for the Planet of Lana Prompt System.
    /// Supports both 2.5D Overhaul and 3D Ca Mau 1954 Village under both Input System & Legacy Input.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ContextualPromptTrigger : MonoBehaviour
    {
        [Header("Prompt Configuration")]
        [SerializeField] private PlanetOfLanaPromptSystem.PromptType promptType = PlanetOfLanaPromptSystem.PromptType.Interact;
        [SerializeField] private Transform worldAnchor;
        [SerializeField] private bool showOnceOnly = true;
        [SerializeField] private bool hideOnMatchingInput = true;
        [SerializeField] private float overrideDuration = 0f;

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
                float dur = overrideDuration > 0.05f ? overrideDuration : 6.0f;
                PlanetOfLanaPromptSystem.Instance.ShowPrompt(promptType, "", worldAnchor, dur);
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
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                switch (promptType)
                {
                    case PlanetOfLanaPromptSystem.PromptType.Move:
                        return kb.wKey.isPressed || kb.aKey.isPressed || kb.sKey.isPressed || kb.dKey.isPressed ||
                               kb.leftArrowKey.isPressed || kb.rightArrowKey.isPressed || kb.upArrowKey.isPressed || kb.downArrowKey.isPressed;
                    case PlanetOfLanaPromptSystem.PromptType.Jump:
                        return kb.spaceKey.wasPressedThisFrame || KeybindingManager.IsActionPressed(GameAction.Jump);
                    case PlanetOfLanaPromptSystem.PromptType.Crawl:
                        return kb.cKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame || kb.leftCtrlKey.wasPressedThisFrame ||
                               KeybindingManager.IsActionPressed(GameAction.Crouch);
                    case PlanetOfLanaPromptSystem.PromptType.ClimbLedge:
                        return kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame;
                    case PlanetOfLanaPromptSystem.PromptType.Interact:
                        return kb.eKey.wasPressedThisFrame || KeybindingManager.IsActionPressed(GameAction.Interact);
                    case PlanetOfLanaPromptSystem.PromptType.RopeGrab:
                        return kb.fKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame;
                    case PlanetOfLanaPromptSystem.PromptType.PushPull:
                        return kb.eKey.wasPressedThisFrame || KeybindingManager.IsActionPressed(GameAction.Interact);
                }
            }
            return false;
#else
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
                    return Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.Space);
                case PlanetOfLanaPromptSystem.PromptType.Interact:
                    return Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0);
                case PlanetOfLanaPromptSystem.PromptType.RopeGrab:
                    return Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E);
                case PlanetOfLanaPromptSystem.PromptType.PushPull:
                    return Input.GetKeyDown(KeyCode.E) || Input.GetMouseButton(0);
                default:
                    return false;
            }
#endif
        }

        private static bool IsPlayer(Collider col)
        {
            return col.CompareTag("Player") ||
                   col.GetComponentInParent<PlayerController>() != null ||
                   col.GetComponentInParent<HoaVaMau.Test3D.Player3DController>() != null ||
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
