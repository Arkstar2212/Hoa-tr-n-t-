using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace HoaDatDo.EditorTools
{
    public static class OptimizePlayerScaleAndCameraFraming
    {
        private const string TargetScenePath = "Assets/Scenes/New Scene 1_Overhaul.unity";
        private const string PlayerObjectName = "PF_Player_Minh";
        private const string VCamObjectName = "CM_VirtualCamera_2.5D";

        // Tỉ lệ vàng tối ưu
        private static readonly Vector3 OptimizedPlayerScale = new Vector3(1.25f, 1.25f, 1.25f);
        private const float OptimizedCameraDistance = 10.5f;
        private static readonly Vector3 OptimizedTargetOffset = new Vector3(1.8f, 1.35f, 0f);

        // Thông số gốc dự phòng
        private static readonly Vector3 OriginalPlayerScale = new Vector3(1.15f, 1.15f, 1.15f);
        private const float OriginalCameraDistance = 13.5f;
        private static readonly Vector3 OriginalTargetOffset = new Vector3(2.0f, 2.17f, 0f);

        [MenuItem("Tools/Hoa Đất Đỏ/Optimize Player Scale & Camera Framing (Tỉ lệ Vàng)")]
        public static void ApplyOptimization()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (!activeScene.path.Contains("New Scene 1_Overhaul"))
            {
                activeScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            }

            // 1. Tối ưu scale người chơi (+8.7%)
            var player = GameObject.Find(PlayerObjectName);
            if (player != null)
            {
                Undo.RecordObject(player.transform, "Optimize Player Scale");
                player.transform.localScale = OptimizedPlayerScale;
                EditorUtility.SetDirty(player);
                Debug.Log($"<color=#38ef7d>[Framing] Updated '{PlayerObjectName}' scale to {OptimizedPlayerScale}</color>");
            }
            else
            {
                Debug.LogWarning($"[Framing] '{PlayerObjectName}' not found in active scene.");
            }

            // 2. Kéo gần Camera Cinemachine & hạ trọng tâm tầm nhìn
            var vcamObj = GameObject.Find(VCamObjectName);
            if (vcamObj != null)
            {
                var comp = vcamObj.GetComponent("CinemachinePositionComposer");
                if (comp != null)
                {
                    SerializedObject so = new SerializedObject(comp);
                    var pDist = so.FindProperty("CameraDistance");
                    var pOffset = so.FindProperty("TargetOffset");

                    if (pDist != null) pDist.floatValue = OptimizedCameraDistance;
                    if (pOffset != null) pOffset.vector3Value = OptimizedTargetOffset;

                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(vcamObj);
                    Debug.Log($"<color=#38ef7d>[Framing] Updated '{VCamObjectName}': Distance={OptimizedCameraDistance}m, Offset={OptimizedTargetOffset}</color>");
                }
            }
            else
            {
                Debug.LogWarning($"[Framing] '{VCamObjectName}' not found in active scene.");
            }

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log("<color=#38ef7d><b>[Framing] Successfully applied Golden Ratio Framing! Bé An is now ~35% larger on screen.</b></color>");
        }

        [MenuItem("Tools/Hoa Đất Đỏ/Revert Player Scale & Camera Framing (Khôi phục gốc)")]
        public static void RevertOptimization()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (!activeScene.path.Contains("New Scene 1_Overhaul"))
            {
                activeScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            }

            var player = GameObject.Find(PlayerObjectName);
            if (player != null)
            {
                Undo.RecordObject(player.transform, "Revert Player Scale");
                player.transform.localScale = OriginalPlayerScale;
                EditorUtility.SetDirty(player);
                Debug.Log($"<color=#ffd200>[Framing] Reverted '{PlayerObjectName}' scale to {OriginalPlayerScale}</color>");
            }

            var vcamObj = GameObject.Find(VCamObjectName);
            if (vcamObj != null)
            {
                var comp = vcamObj.GetComponent("CinemachinePositionComposer");
                if (comp != null)
                {
                    SerializedObject so = new SerializedObject(comp);
                    var pDist = so.FindProperty("CameraDistance");
                    var pOffset = so.FindProperty("TargetOffset");

                    if (pDist != null) pDist.floatValue = OriginalCameraDistance;
                    if (pOffset != null) pOffset.vector3Value = OriginalTargetOffset;

                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(vcamObj);
                    Debug.Log($"<color=#ffd200>[Framing] Reverted '{VCamObjectName}': Distance={OriginalCameraDistance}m, Offset={OriginalTargetOffset}</color>");
                }
            }

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log("<color=#ffd200><b>[Framing] Reverted to original framing settings.</b></color>");
        }
    }
}
