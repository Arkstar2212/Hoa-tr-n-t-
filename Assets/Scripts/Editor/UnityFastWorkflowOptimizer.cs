using UnityEngine;
using UnityEditor;

namespace HoaVaMau.EditorTools
{
    /// <summary>
    /// Tự động tối ưu hóa hiệu năng phản hồi giữa Unity Editor và Unity MCP:
    /// - Luôn giữ Application.runInBackground = true để Editor không bị sleep/lag khi unfocused.
    /// - Bật enterPlayModeOptionsEnabled để vào Play mode tức thì không tốn 10-15s reload domain.
    /// - Giữ Editor loop phản hồi nhanh chóng cho các socket MCP request.
    /// </summary>
    [InitializeOnLoad]
    public static class UnityFastWorkflowOptimizer
    {
        static UnityFastWorkflowOptimizer()
        {
            ApplySettings();
            EditorApplication.update -= MaintainActiveLoop;
            EditorApplication.update += MaintainActiveLoop;
        }

        [MenuItem("Tools/Hoa Va Mau/High-Speed MCP Optimization")]
        public static void ApplySettings()
        {
            if (!Application.runInBackground)
            {
                Application.runInBackground = true;
            }

            if (!EditorSettings.enterPlayModeOptionsEnabled)
            {
                EditorSettings.enterPlayModeOptionsEnabled = true;
                EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;
            }
        }

        private static void MaintainActiveLoop()
        {
            if (!Application.runInBackground)
            {
                Application.runInBackground = true;
            }
        }
    }
}
