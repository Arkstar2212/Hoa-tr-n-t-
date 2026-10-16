#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using HoaVaMau.Environment;

namespace HoaVaMau.Editor
{
    [CustomEditor(typeof(PerspectiveLayerScaler))]
    [CanEditMultipleObjects]
    public class PerspectiveLayerScalerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var scaler = (PerspectiveLayerScaler)target;

            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                "Perspective Layer Scaler: Tự động điều chỉnh Scale tỷ lệ thuận với khoảng cách Camera khi dịch chuyển Z.\n" +
                "Giúp thay đổi độ sâu Parallax mà không làm vỡ bố cục hay sai lệch kích thước concept ban đầu.",
                MessageType.Info
            );
            EditorGUILayout.Space(4);

            DrawDefaultInspector();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Chức Năng Nhanh (Layer Presets & Calibration)", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Khóa Tham Chiếu Hiện Tại", GUILayout.Height(26)))
            {
                foreach (var obj in targets)
                {
                    if (obj is PerspectiveLayerScaler s)
                    {
                        Undo.RecordObject(s, "Capture Reference Scale");
                        s.CaptureCurrentAsReference();
                    }
                }
            }
            if (GUILayout.Button("Về Ground (Z = 0)", GUILayout.Height(26)))
            {
                foreach (var obj in targets)
                {
                    if (obj is PerspectiveLayerScaler s)
                    {
                        Undo.RecordObject(s.transform, "Snap to Z=0");
                        s.SnapToPlayableGround();
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Đẩy nhanh vào tầng Z chuẩn:", EditorStyles.miniBoldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Tiền cảnh (Z = -3)"))
            {
                ApplyPresetZ(-3.0f);
            }
            if (GUILayout.Button("Trung cảnh gần (Z = +8)"))
            {
                ApplyPresetZ(8.0f);
            }
            if (GUILayout.Button("Trung cảnh xa (Z = +20)"))
            {
                ApplyPresetZ(20.0f);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Hậu cảnh núi (Z = +60)"))
            {
                ApplyPresetZ(60.0f);
            }
            if (GUILayout.Button("Bầu trời / Mây (Z = +120)"))
            {
                ApplyPresetZ(120.0f);
            }
            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }

        private void ApplyPresetZ(float z)
        {
            foreach (var obj in targets)
            {
                if (obj is PerspectiveLayerScaler s)
                {
                    Undo.RecordObject(s.transform, "Push Layer Z");
                    s.PushToLayerPreset(z);
                }
            }
        }
    }
}
#endif
