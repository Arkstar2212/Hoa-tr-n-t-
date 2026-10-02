using UnityEngine;
using UnityEditor;
using HoaVaMau.Gameplay;

namespace HoaVaMau.Editor
{
    [CustomEditor(typeof(MeshyBoyAnimationController))]
    public class MeshyBoyAnimationControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "ĐIỀU KHIỂN ANIMATION CHO MESHY BOY:\n" +
                "• A / D: Đi bộ mượt mà (Walk)\n" +
                "• Shift + A/D: Tăng tốc chạy (Run)\n" +
                "• Space: Nhảy (Jump)\n" +
                "• W khi bám vách gờ / dây: Leo trèo (Climb)\n" +
                "• Phím tắt test ép động tác: 1 (Walk), 2 (Run), 3 (Climb), 0 (Auto Gameplay)", 
                MessageType.Info);

            EditorGUILayout.Space(6);

            // Draw default fields
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Kích Hoạt Nhanh Animation (Test)", EditorStyles.boldLabel);

            var controller = (MeshyBoyAnimationController)target;

            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = controller.CurrentMode == MeshyAnimationMode.ForceWalk ? new Color(0.4f, 0.9f, 0.4f) : Color.white;
            if (GUILayout.Button("▶ Walk (Đi bộ)", GUILayout.Height(32)))
            {
                controller.ForceWalk();
                SceneView.RepaintAll();
            }

            GUI.backgroundColor = controller.CurrentMode == MeshyAnimationMode.ForceRun ? new Color(0.4f, 0.8f, 1.0f) : Color.white;
            if (GUILayout.Button("▶ Run (Chạy nhanh)", GUILayout.Height(32)))
            {
                controller.ForceRun();
                SceneView.RepaintAll();
            }

            GUI.backgroundColor = controller.CurrentMode == MeshyAnimationMode.ForceClimb ? new Color(1.0f, 0.7f, 0.3f) : Color.white;
            if (GUILayout.Button("▶ Climb (Leo trèo)", GUILayout.Height(32)))
            {
                controller.ForceClimb();
                SceneView.RepaintAll();
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            GUI.backgroundColor = controller.CurrentMode == MeshyAnimationMode.Auto ? new Color(0.4f, 0.9f, 1.0f) : Color.white;
            if (GUILayout.Button("🔄 Về Chế Độ Tự Động (Auto Gameplay)", GUILayout.Height(28)))
            {
                controller.SetAutoMode();
                SceneView.RepaintAll();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(6);

            if (GUILayout.Button("Kiểm tra và Tự Động Cấu Hình Lại Animator + Avatar", GUILayout.Height(26)))
            {
                controller.SetupComponents();
                EditorUtility.SetDirty(controller.gameObject);
                Debug.Log("<color=#4ade80>[Meshy Boy]</color> Đã kiểm tra và đồng bộ lại Animator & Avatar thành công!");
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
