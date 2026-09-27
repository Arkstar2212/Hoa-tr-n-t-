using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Interactive narrative prop in the 3D Refugee Shelter Room.
    /// Allows An to walk up in 3D, press [E], and examine objects
    /// (e.g. Maritime maps, Campfire, Medical chests, Keepsakes) with rich historical context.
    /// </summary>
    public class ShelterInteractable : MonoBehaviour
    {
        [Header("Interaction Settings")]
        [SerializeField] private string promptTitle = "Xem vật phẩm";
        [SerializeField] private string promptKey = "[E] Kiểm tra";
        [SerializeField] [TextArea(3, 6)] private string narrativeDescription = "Mô tả chi tiết...";
        [SerializeField] private float interactRadius = 2.2f;

        private Transform playerTransform;
        private bool isPlayerInRange = false;
        private bool isShowingDialog = false;
        private float dialogTimer = 0f;

        private void Start()
        {
            FindPlayerReference();
        }

        private void FindPlayerReference()
        {
            var p = Object.FindAnyObjectByType<PlayerController>();
            if (p != null) playerTransform = p.transform;
        }

        public void Configure(string title, string keyPrompt, string description, float radius = 2.2f)
        {
            promptTitle = title;
            promptKey = keyPrompt;
            narrativeDescription = description;
            interactRadius = radius;
        }

        private void Update()
        {
            if (playerTransform == null)
            {
                FindPlayerReference();
                if (playerTransform == null) return;
            }

            float distSq = (transform.position - playerTransform.position).sqrMagnitude;
            isPlayerInRange = distSq <= (interactRadius * interactRadius);

            if (!isPlayerInRange && isShowingDialog)
            {
                isShowingDialog = false;
            }

            if (isPlayerInRange)
            {
                bool interactPressed = false;
#if ENABLE_INPUT_SYSTEM
                if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) interactPressed = true;
                if (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame) interactPressed = true;
#else
                if (Input.GetKeyDown(KeyCode.E)) interactPressed = true;
#endif
                if (interactPressed)
                {
                    isShowingDialog = !isShowingDialog;
                    if (isShowingDialog) dialogTimer = 8.0f;
                }
            }

            if (isShowingDialog && dialogTimer > 0f)
            {
                dialogTimer -= Time.deltaTime;
                if (dialogTimer <= 0f) isShowingDialog = false;
            }
        }

        private void OnGUI()
        {
            if (!isPlayerInRange) return;

            // Prompt when in range
            if (!isShowingDialog)
            {
                Vector3 screenPos = Camera.main != null ? Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1.2f) : Vector3.zero;
                if (screenPos.z > 0)
                {
                    float width = 250f;
                    float height = 50f;
                    float x = screenPos.x - width * 0.5f;
                    float y = Screen.height - screenPos.y - height * 0.5f;

                    GUIStyle bgStyle = new GUIStyle(GUI.skin.box);
                    bgStyle.normal.background = Texture2D.whiteTexture;
                    Color oldBg = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(0.1f, 0.14f, 0.18f, 0.92f);
                    GUI.Box(new Rect(x, y, width, height), GUIContent.none, bgStyle);
                    GUI.backgroundColor = oldBg;

                    GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
                    titleStyle.alignment = TextAnchor.MiddleCenter;
                    titleStyle.fontSize = 12;
                    titleStyle.fontStyle = FontStyle.Bold;
                    titleStyle.normal.textColor = new Color(0.95f, 0.85f, 0.35f);
                    GUI.Label(new Rect(x, y + 4, width, 18), promptTitle, titleStyle);

                    GUIStyle keyStyle = new GUIStyle(GUI.skin.label);
                    keyStyle.alignment = TextAnchor.MiddleCenter;
                    keyStyle.fontSize = 11;
                    keyStyle.normal.textColor = Color.white;
                    GUI.Label(new Rect(x, y + 24, width, 18), promptKey, keyStyle);
                }
            }
            // Dialog box when inspected
            else
            {
                float dWidth = 620f;
                float dHeight = 120f;
                float dx = (Screen.width - dWidth) * 0.5f;
                float dy = Screen.height - dHeight - 45f;

                GUIStyle bgStyle = new GUIStyle(GUI.skin.box);
                bgStyle.normal.background = Texture2D.whiteTexture;
                Color oldBg = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.06f, 0.08f, 0.12f, 0.95f);
                GUI.Box(new Rect(dx, dy, dWidth, dHeight), GUIContent.none, bgStyle);
                GUI.backgroundColor = oldBg;

                GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
                headerStyle.fontSize = 14;
                headerStyle.fontStyle = FontStyle.Bold;
                headerStyle.normal.textColor = new Color(0.95f, 0.82f, 0.30f);
                GUI.Label(new Rect(dx + 16, dy + 10, dWidth - 32, 22), promptTitle + " — [Nhấn E để đóng]", headerStyle);

                GUIStyle descStyle = new GUIStyle(GUI.skin.label);
                descStyle.fontSize = 12;
                descStyle.wordWrap = true;
                descStyle.normal.textColor = new Color(0.92f, 0.94f, 0.96f);
                GUI.Label(new Rect(dx + 16, dy + 36, dWidth - 32, dHeight - 44), narrativeDescription, descStyle);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, interactRadius);
        }
    }
}