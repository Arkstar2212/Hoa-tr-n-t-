using System.Collections.Generic;
using UnityEngine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Interactive Test Manager for Background Tree Clusters (Rừng cây hậu cảnh).
    /// - Allows switching focus between different Tree Cluster Options with keys [1] to [6] and [0]/[D] for Demo.
    /// - Supports smooth camera lerping, free horizontal pan ([A]/[D] or Arrows), and scroll zoom.
    /// - Displays an elegant on-screen Vietnamese HUD detailing each option's composition, ratio, and aesthetic intent.
    /// </summary>
    public class TreeClusterTestManager : MonoBehaviour
    {
        [System.Serializable]
        public struct ClusterOption
        {
            public string optionName;
            public string title;
            [TextArea(2, 4)]
            public string description;
            public float cameraX;
            public float cameraDistance;
            public float cameraHeight;
            public GameObject clusterRoot;
        }

        [Header("Camera Rig")]
        [SerializeField] private Camera testCamera;
        [SerializeField] private float moveSpeed = 16.0f;
        [SerializeField] private float smoothTime = 0.35f;

        [Header("Testing Options")]
        [SerializeField] private List<ClusterOption> options = new List<ClusterOption>();
        [SerializeField] private int currentOptionIndex = 0;

        public List<ClusterOption> Options => options;

        private Vector3 targetCamPos;
        private Vector3 camVelocity;
        private bool isFreePanning = false;

        private void Reset()
        {
            if (testCamera == null) testCamera = Camera.main;
        }

        private void Start()
        {
            if (testCamera == null) testCamera = Camera.main;
            if (options.Count > 0)
            {
                JumpToOption(currentOptionIndex, true);
            }
        }

        public void JumpToOption(int index, bool immediate = false)
        {
            if (index < 0 || index >= options.Count) return;
            currentOptionIndex = index;
            isFreePanning = false;

            var opt = options[index];
            targetCamPos = new Vector3(opt.cameraX, opt.cameraHeight, -opt.cameraDistance);

            if (immediate && testCamera != null)
            {
                testCamera.transform.position = targetCamPos;
                testCamera.transform.rotation = Quaternion.Euler(2.5f, 0f, 0f);
            }
        }

        private void Update()
        {
            if (testCamera == null) return;

            // 1. Hotkey switches
            for (int i = 0; i < Mathf.Min(options.Count, 9); i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    JumpToOption(i);
                }
            }

            // [0] for Option 10 (index 9) or Demo if fewer options
            if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                if (options.Count >= 10) JumpToOption(9);
                else if (options.Count > 0) JumpToOption(options.Count - 1);
            }

            // [D] for Demo Vista (the last option in list)
            if (Input.GetKeyDown(KeyCode.D))
            {
                if (options.Count > 0) JumpToOption(options.Count - 1);
            }

            // 2. Free horizontal panning
            float hInput = Input.GetAxisRaw("Horizontal");
            if (Mathf.Abs(hInput) > 0.05f)
            {
                isFreePanning = true;
                targetCamPos.x += hInput * moveSpeed * Time.deltaTime;
            }

            // 3. Zoom with scroll or vertical keys
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                targetCamPos.z += scroll * 12.0f;
                targetCamPos.z = Mathf.Clamp(targetCamPos.z, -28.0f, -8.0f);
            }

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            {
                targetCamPos.y += moveSpeed * 0.5f * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            {
                targetCamPos.y -= moveSpeed * 0.5f * Time.deltaTime;
            }

            // Smooth camera movement
            testCamera.transform.position = Vector3.SmoothDamp(
                testCamera.transform.position,
                targetCamPos,
                ref camVelocity,
                smoothTime
            );
        }

        private void OnGUI()
        {
            if (options == null || options.Count == 0) return;

            // Background HUD panel
            GUI.Box(new Rect(20, 20, 480, 230), "");

            GUILayout.BeginArea(new Rect(30, 25, 460, 220));

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.92f, 0.45f) } // Golden warm
            };

            GUIStyle subTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.35f, 0.95f, 0.75f) } // Mint green
            };

            GUIStyle bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true,
                normal = { textColor = Color.white }
            };

            GUIStyle hintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Italic,
                normal = { textColor = new Color(0.85f, 0.85f, 0.85f, 0.85f) }
            };

            var curOpt = options[currentOptionIndex];
            GUILayout.Label(curOpt.optionName.ToUpper(), titleStyle);
            GUILayout.Label(curOpt.title, subTitleStyle);
            GUILayout.Space(2);
            GUILayout.Label(curOpt.description, bodyStyle);
            GUILayout.Space(8);

            GUILayout.Label("⌨ HƯỚNG DẪN ĐIỀU KHIỂN:", hintStyle);
            GUILayout.Label("• Phím [1] - [9], [0]: Chuyển nhanh giữa các phương án cụm cây", hintStyle);
            GUILayout.Label("• Phím [D]: Xem toàn cảnh phối hợp lúa & rừng cây (Demo Vista)", hintStyle);
            GUILayout.Label("• Phím [A]/[D] hoặc Mũi tên: Lia camera tự do sang 2 bên", hintStyle);
            GUILayout.Label("• Cuộn chuột: Phóng to / Thu nhỏ khung hình", hintStyle);

            GUILayout.EndArea();

            // Bottom option switcher buttons
            float barWidth = Mathf.Min(options.Count * 95f, Screen.width - 40f);
            float startX = (Screen.width - barWidth) * 0.5f;

            GUILayout.BeginArea(new Rect(startX, Screen.height - 55, barWidth, 45));
            GUILayout.BeginHorizontal();
            for (int i = 0; i < options.Count; i++)
            {
                bool isCur = (i == currentOptionIndex);
                string btnLabel;
                if (i == options.Count - 1) btnLabel = "[D] Demo";
                else if (i == 9) btnLabel = "[0] PA 10";
                else btnLabel = $"[{i + 1}] PA {i + 1}";

                if (isCur) btnLabel = "▶ " + btnLabel;

                if (GUILayout.Button(btnLabel, GUILayout.Height(38)))
                {
                    JumpToOption(i);
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }
}
