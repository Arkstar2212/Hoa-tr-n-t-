using UnityEngine;
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

        private InteractableObject3D closestInteractable;
        private static string currentHUDMessage = "";
        private static float hudMessageTimer = 0f;

        private void Start()
        {
            if (playerTransform == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null) playerTransform = player.transform;
            }
        }

        private void Update()
        {
            if (playerTransform == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null) playerTransform = player.transform;
                if (playerTransform == null) return;
            }

            // Find closest interactable
            InteractableObject3D[] allInteractables = Object.FindObjectsByType<InteractableObject3D>();
            closestInteractable = null;
            float minDistance = detectionRadius;

            foreach (var item in allInteractables)
            {
                float dist = Vector3.Distance(playerTransform.position, item.transform.position);
                if (dist <= item.interactionRadius && dist < minDistance)
                {
                    minDistance = dist;
                    closestInteractable = item;
                }
            }

            // Read E key
            bool ePressed = false;
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.eKey.wasPressedThisFrame) ePressed = true;
            var gp = Gamepad.current;
            if (gp != null && gp.buttonSouth.wasPressedThisFrame) ePressed = true;
#else
            if (Input.GetKeyDown(KeyCode.E)) ePressed = true;
#endif

            if (closestInteractable != null && ePressed)
            {
                closestInteractable.Interact();
                ShowHUDMessage(closestInteractable.actionMessage, 3.0f);
            }

            // Update HUD timer
            if (hudMessageTimer > 0f)
            {
                hudMessageTimer -= Time.deltaTime;
                if (hudMessageTimer <= 0f)
                {
                    currentHUDMessage = "";
                }
            }
        }

        public static void ShowHUDMessage(string msg, float duration = 3.0f)
        {
            currentHUDMessage = msg;
            hudMessageTimer = duration;
        }

        private void OnGUI()
        {
            // Prompt display
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

            // Notification / HUD Message
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

            // Helpful Instructions corner
            GUIStyle guideStyle = new GUIStyle(GUI.skin.label);
            guideStyle.fontSize = 14;
            guideStyle.normal.textColor = new Color(1f, 1f, 1f, 0.85f);
            string guide = "W/A/S/D: Di chuyển  |  Shift: Chạy nhanh  |  E: Tương tác";
            GUI.Label(new Rect(20, 20, 500, 30), guide, guideStyle);
        }
    }
}
