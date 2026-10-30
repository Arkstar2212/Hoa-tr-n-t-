using UnityEngine;
using HoaVaMau.Gameplay;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HoaVaMau.Test3D
{
    public class InteractionSystem3D : MonoBehaviour
    {
        [Header("Detection")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private float detectionRadius = 3.0f;

        private Player3DController player3D;
        private InteractableObject3D closestInteractable;
        private InteractableCrate3D closestCrate;
        private PortableItem3D closestPortableItem;

        private static string currentHUDMessage = "";
        private static float hudMessageTimer = 0f;

        public InteractableObject3D ClosestInteractable => closestInteractable;

        private void Start()
        {
            ResolvePlayer();
        }

        private void ResolvePlayer()
        {
            if (playerTransform == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null) playerTransform = player.transform;
            }
            if (playerTransform != null && player3D == null)
            {
                player3D = playerTransform.GetComponent<Player3DController>();
            }
        }

        private void Update()
        {
            if (PauseMenuController.Instance != null && PauseMenuController.Instance.IsPaused) return;

            ResolvePlayer();
            if (playerTransform == null) return;

            // Read Interact [E] & Toss [Q] keys
            bool ePressed = KeybindingManager.IsActionPressed(GameAction.Interact);
            bool qPressed = false;
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.eKey.wasPressedThisFrame) ePressed = true;
                if (kb.qKey.wasPressedThisFrame) qPressed = true;
            }
            var gp = Gamepad.current;
            if (gp != null)
            {
                if (gp.buttonSouth.wasPressedThisFrame || gp.buttonWest.wasPressedThisFrame) ePressed = true;
                if (gp.rightShoulder.wasPressedThisFrame) qPressed = true;
            }
#else
            if (Input.GetKeyDown(KeyCode.E)) ePressed = true;
            if (Input.GetKeyDown(KeyCode.Q)) qPressed = true;
#endif

            // 1. Handle active carried 3D item (Place [E] / Toss [Q])
            if (player3D != null && player3D.CarriedItem != null)
            {
                if (ePressed)
                {
                    string title = player3D.CarriedItem.ItemTitle;
                    player3D.CarriedItem.PlaceDown();
                    ShowHUDMessage($"Đã đặt {title} xuống đất nhẹ nhàng.", 2.4f, title);
                    return;
                }
                if (qPressed)
                {
                    string title = player3D.CarriedItem.ItemTitle;
                    player3D.CarriedItem.TossForward();
                    ShowHUDMessage($"Đã ném nhẹ {title} về phía trước.", 2.2f, title);
                    return;
                }
            }

            // 2. Handle active grabbed 3D crate release on [E]
            if (player3D != null && player3D.GrabbedCrate != null && ePressed)
            {
                player3D.GrabbedCrate.ReleaseGrab();
                return;
            }

            // Do not compete with VillageNPCInteractable if an NPC owns [E] right now
            if (VillageNPCInteractable.HasActivePromptOwner)
            {
                closestInteractable = null;
                closestCrate = null;
                closestPortableItem = null;
                TickHUDTimer();
                return;
            }

            // 3. Find closest PortableItem3D
            closestPortableItem = null;
            if (player3D != null && player3D.CarriedItem == null)
            {
                var items = Object.FindObjectsByType<PortableItem3D>();
                float bestItemDist = detectionRadius;
                foreach (var it in items)
                {
                    if (it == null || !it.CanPickUp(playerTransform)) continue;
                    float d = Vector3.Distance(playerTransform.position, it.transform.position);
                    if (d < bestItemDist) { bestItemDist = d; closestPortableItem = it; }
                }
            }

            // 4. Find closest InteractableCrate3D
            closestCrate = null;
            if (player3D != null && player3D.GrabbedCrate == null && closestPortableItem == null)
            {
                var crates = Object.FindObjectsByType<InteractableCrate3D>();
                float bestCrateDist = detectionRadius;
                foreach (var cr in crates)
                {
                    if (cr == null || !cr.CanInteract(playerTransform)) continue;
                    float d = Vector3.Distance(playerTransform.position, cr.transform.position);
                    if (d < bestCrateDist) { bestCrateDist = d; closestCrate = cr; }
                }
            }

            // 5. Find closest InteractableObject3D
            InteractableObject3D[] allInteractables = Object.FindObjectsByType<InteractableObject3D>();
            closestInteractable = null;
            float minDistance = detectionRadius;

            foreach (var item in allInteractables)
            {
                if (item == null || !item.gameObject.activeInHierarchy || item.isTriggerZone) continue;
                float dist = Vector3.Distance(playerTransform.position, item.transform.position);
                if (dist <= item.interactionRadius && dist < minDistance)
                {
                    minDistance = dist;
                    closestInteractable = item;
                }
            }

            if (ePressed)
            {
                if (closestPortableItem != null && player3D != null)
                {
                    closestPortableItem.PickUp(player3D);
                    ShowHUDMessage(closestPortableItem.PickupMessage, 3.2f, closestPortableItem.ItemTitle);
                }
                else if (closestCrate != null && player3D != null)
                {
                    closestCrate.ToggleGrab(player3D);
                    ShowHUDMessage("Dùng [W][A][S][D] để đẩy/kéo thùng gỗ, nhấn [E] để buông tay.", 3.0f, closestCrate.DisplayName);
                }
                else if (closestInteractable != null)
                {
                    closestInteractable.Interact();
                    ShowHUDMessage(closestInteractable.actionMessage, 3.6f, closestInteractable.objectTitle);
                }
            }

            TickHUDTimer();
        }

        private static void TickHUDTimer()
        {
            if (hudMessageTimer > 0f)
            {
                hudMessageTimer -= Time.deltaTime;
                if (hudMessageTimer <= 0f)
                {
                    currentHUDMessage = "";
                }
            }
        }

        public static void ShowHUDMessage(string msg, float duration = 3.0f, string title = "KHÁM PHÁ LÀNG QUÊ")
        {
            currentHUDMessage = msg;
            hudMessageTimer = duration;

            if (CaMau3DUIGuideController.Instance != null)
            {
                CaMau3DUIGuideController.ShowNarrativeToast(title, msg, duration);
            }
        }

        private void OnGUI()
        {
            // When CaMau3DUIGuideController is active, render clean 1920x1080 Planet-of-Lana Canvas UI instead of legacy OnGUI boxes
            if (CaMau3DUIGuideController.Instance != null) return;

            if (closestInteractable != null)
            {
                GUIStyle promptStyle = new GUIStyle(GUI.skin.box);
                promptStyle.fontSize = 18;
                promptStyle.fontStyle = FontStyle.Bold;
                promptStyle.alignment = TextAnchor.MiddleCenter;
                promptStyle.normal.textColor = Color.white;

                string text = $"[ {closestInteractable.objectTitle} ]\n{closestInteractable.promptMessage}";
                float width = 400f;
                float height = 65f;
                float x = (Screen.width - width) * 0.5f;
                float y = Screen.height - 150f;

                GUI.backgroundColor = new Color(0.1f, 0.15f, 0.25f, 0.85f);
                GUI.Box(new Rect(x, y, width, height), text, promptStyle);
            }

            if (!string.IsNullOrEmpty(currentHUDMessage))
            {
                GUIStyle msgStyle = new GUIStyle(GUI.skin.box);
                msgStyle.fontSize = 20;
                msgStyle.fontStyle = FontStyle.Bold;
                msgStyle.alignment = TextAnchor.MiddleCenter;
                msgStyle.normal.textColor = new Color(0.3f, 1.0f, 0.6f, 1.0f);

                float width = 420f;
                float height = 50f;
                float x = (Screen.width - width) * 0.5f;
                float y = Screen.height - 80f;

                GUI.backgroundColor = new Color(0.05f, 0.2f, 0.1f, 0.9f);
                GUI.Box(new Rect(x, y, width, height), currentHUDMessage, msgStyle);
            }
        }
    }
}
