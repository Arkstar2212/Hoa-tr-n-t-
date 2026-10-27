using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

public class ResetSceneViewCameraTool
{
    [MenuItem("Tools/Hoa Tren Dat Do/Camera/Khac Phuc Loi Zoom Mat Object (Reset Scene View Camera) %#f")]
    public static void ResetSceneViewCamera()
    {
        if (SceneView.lastActiveSceneView != null)
        {
            var sv = SceneView.lastActiveSceneView;

            // Tìm vị trí của Player hoặc tâm màn chơi
            GameObject target = GameObject.Find("PF_Player_Minh");
            Vector3 focusPos = target != null ? target.transform.position : new Vector3(35f, 1f, 0f);

            // Đặt lại tiêu cự SceneView vào trung tâm màn chơi với kích thước chuẩn
            sv.pivot = focusPos;
            sv.size = 6.0f;
            sv.rotation = Quaternion.identity;

            if (sv.camera != null)
            {
                sv.camera.nearClipPlane = 0.05f;
                sv.camera.farClipPlane = 1000f;
            }

            sv.Repaint();
            Debug.Log("<color=#10b981>[SceneView Camera]</color> Đã reset Near Clip Plane về 0.05m và lấy lại tiêu cự vào màn chơi thành công!");
        }
        else
        {
            Debug.LogWarning("Không tìm thấy cửa sổ Scene View đang hoạt động. Vui lòng mở hoặc nhấp vào Scene View trước!");
        }
    }
}
#endif
