using UnityEngine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// NieR: Automata Style Seamless Perspective Transition Zone.
    /// Manages the threshold between the 2.5D outdoor puzzle platformer
    /// and the full 3D interior refugee shelter/camp room.
    /// Ensures 100% stable, jitter-free perspective switching without edge stuttering.
    /// </summary>
    public class PerspectiveTransitionZone : MonoBehaviour
    {
        [Header("Zone Configuration")]
        [Tooltip("World X threshold separating 2D (outside) from 3D (inside)")]
        [SerializeField] private float thresholdX = 35.5f;

        [Tooltip("Target Z plane when returning to 2D platformer")]
        [SerializeField] private float targetZPlane = 0f;

        [Header("Room Camera Settings")]
        [SerializeField] private Vector3 roomCameraOffset = new Vector3(0f, 5.8f, -7.2f);
        [SerializeField] private Vector3 roomCameraRotation = new Vector3(30f, 0f, 0f);
        [SerializeField] private float roomCameraFov = 44f;
        [SerializeField] private Vector3 roomMinCamPos = new Vector3(34.0f, 4.5f, -8.0f);
        [SerializeField] private Vector3 roomMaxCamPos = new Vector3(56.0f, 10.0f, 8.0f);

        [Header("Location Banner")]
        [SerializeField] private string locationTitle = "TRẠM TIẾP ĐÓN ĐỒNG BÀO - CÀ MAU 1954";
        [SerializeField] private string locationSubtitle = "Khu Vực Tị Nạn và Điểm Dừng Chân An Toàn (3D Mode)";
        [SerializeField] private float bannerDuration = 4.0f;

        private float bannerTimer = 0f;
        private bool isInside3D = false;
        private PlayerController player;
        private Cinematic2DCamera cam;

        private void Start()
        {
            FindReferences();
        }

        public void FindReferences()
        {
            if (player == null) player = Object.FindAnyObjectByType<PlayerController>();
            if (cam == null && Camera.main != null) cam = Camera.main.GetComponent<Cinematic2DCamera>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player") && other.GetComponent<PlayerController>() == null) return;
            EvaluateTransition(other.transform.position);
        }

        private void OnTriggerStay(Collider other)
        {
            if (!other.CompareTag("Player") && other.GetComponent<PlayerController>() == null) return;
            EvaluateTransition(other.transform.position);
        }

        private void EvaluateTransition(Vector3 playerPos)
        {
            FindReferences();

            // Moving into the room (X >= thresholdX)
            if (playerPos.x >= thresholdX && !isInside3D)
            {
                Enter3DRoom();
            }
            // Moving back out to 2D platformer (X < thresholdX)
            else if (playerPos.x < thresholdX && isInside3D)
            {
                ExitTo2DPlatformer();
            }
        }

        public void Enter3DRoom()
        {
            isInside3D = true;
            bannerTimer = bannerDuration;

            if (player != null)
            {
                player.SwitchPerspective(PerspectiveMode.ThreeD);
            }

            if (cam != null)
            {
                cam.ConfigureRoomCamera(roomCameraOffset, roomCameraRotation, roomCameraFov, roomMinCamPos, roomMaxCamPos);
                cam.SetCameraMode(CameraPerspectiveMode.FreeRoom3D);
            }

            Debug.Log("<color=#40b8a6>[Hoa và Máu]</color> Transitioned into 3D Refugee Shelter Room.");
        }

        public void ExitTo2DPlatformer()
        {
            isInside3D = false;

            if (player != null)
            {
                player.SwitchPerspective(PerspectiveMode.TwoD, targetZPlane);
            }

            if (cam != null)
            {
                cam.SetCameraMode(CameraPerspectiveMode.SideScroll2D);
            }

            Debug.Log("<color=#40b8a6>[Hoa và Máu]</color> Transitioned back to 2.5D Platformer.");
        }

        private void Update()
        {
            if (bannerTimer > 0f)
            {
                bannerTimer -= Time.deltaTime;
            }
        }

        private void OnGUI()
        {
            if (bannerTimer <= 0f) return;

            float alpha = Mathf.Clamp01(bannerTimer / 1.0f);
            Color prevColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);

            float width = 520f;
            float height = 75f;
            float x = (Screen.width - width) * 0.5f;
            float y = 45f;

            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background = Texture2D.whiteTexture;
            Color oldBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.08f, 0.12f, 0.16f, 0.88f * alpha);

            GUI.Box(new Rect(x, y, width, height), GUIContent.none, boxStyle);
            GUI.backgroundColor = oldBg;

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.fontSize = 16;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.normal.textColor = new Color(0.98f, 0.88f, 0.45f, alpha);
            GUI.Label(new Rect(x, y + 8, width, 26), locationTitle, titleStyle);

            GUIStyle subStyle = new GUIStyle(GUI.skin.label);
            subStyle.alignment = TextAnchor.MiddleCenter;
            subStyle.fontSize = 12;
            subStyle.fontStyle = FontStyle.Italic;
            subStyle.normal.textColor = new Color(0.85f, 0.90f, 0.95f, alpha * 0.9f);
            GUI.Label(new Rect(x, y + 36, width, 22), locationSubtitle, subStyle);

            GUI.color = prevColor;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.85f, 0.95f, 0.35f);
            Gizmos.DrawWireCube(transform.position, GetComponent<Collider>() != null ? GetComponent<Collider>().bounds.size : Vector3.one * 2f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(new Vector3(thresholdX, transform.position.y - 3f, -5f), new Vector3(thresholdX, transform.position.y + 5f, 5f));
        }
    }
}