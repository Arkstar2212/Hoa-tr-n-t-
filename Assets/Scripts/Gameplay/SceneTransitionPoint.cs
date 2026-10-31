using UnityEngine;
using UnityEngine.SceneManagement;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Điểm kết nối chuyển cảnh tại Cổng Làng Bắc (Village North Gate).
    /// Đặt tại cuối con đường thẳng xuyên qua cánh đồng lúa.
    /// Hoạt động an toàn, độc lập trong scene này, không can thiệp hay làm ảnh hưởng scene khác.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class SceneTransitionPoint : MonoBehaviour
    {
        [Header("Target Destination")]
        [Tooltip("Tên Scene tiếp theo muốn chuyển tới (để trống nếu chỉ làm điểm mốc/trigger hook)")]
        [SerializeField] private string targetSceneName = "";

        [Tooltip("Thông báo hiển thị khi nhân vật tiếp cận cổng làng")]
        [SerializeField] private string transitionPrompt = "Rời Làng Cà Mau (1954) - Tiến vào Cảnh tiếp theo...";

        [Header("Trigger Settings")]
        [SerializeField] private bool autoLoadOnEnter = false;
        [SerializeField] private float triggerCooldown = 2.0f;

        private float lastTriggerTime = -10f;

        public string TargetSceneName
        {
            get => targetSceneName;
            set => targetSceneName = value;
        }

        public string TransitionPrompt
        {
            get => transitionPrompt;
            set => transitionPrompt = value;
        }

        private void Reset()
        {
            var col = GetComponent<BoxCollider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (Time.time - lastTriggerTime < triggerCooldown) return;

            bool isPlayer = other.CompareTag("Player") ||
                            other.name.Contains("Player") ||
                            other.name.Contains("Minh") ||
                            other.name.Contains("An") ||
                            other.GetComponent<PlayerController>() != null;

            if (isPlayer)
            {
                lastTriggerTime = Time.time;
                Debug.Log($"[SceneTransitionPoint] Bé An chạm vào Cổng Làng: '{transitionPrompt}' -> Đích: '{(string.IsNullOrEmpty(targetSceneName) ? "Chưa chỉ định" : targetSceneName)}'");

                if (autoLoadOnEnter && !string.IsNullOrEmpty(targetSceneName))
                {
                    if (Application.CanStreamedLevelBeLoaded(targetSceneName))
                    {
                        SceneManager.LoadScene(targetSceneName);
                    }
                    else
                    {
                        Debug.LogWarning($"[SceneTransitionPoint] Scene '{targetSceneName}' chưa được thêm vào Build Settings.");
                    }
                }
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.85f, 0.35f, 0.45f);
            var col = GetComponent<BoxCollider>();
            if (col != null)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(col.center, col.size);
                Gizmos.color = new Color(0.2f, 0.85f, 0.35f, 0.95f);
                Gizmos.DrawWireCube(col.center, col.size);
            }
        }
    }
}
