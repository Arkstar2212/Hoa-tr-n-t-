#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using HoaVaMau.Gameplay;

namespace HoaVaMau.Editor
{
    public static class TestCameraFollow
    {
        [MenuItem("Hoa va Mau/Test Camera Tracking Diagnostic")]
        public static void RunDiagnostic()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            Camera cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("[TestCamera] Camera.main is NULL!");
                return;
            }

            Debug.Log($"[TestCamera] Camera found: {cam.name}, pos={cam.transform.position}, rot={cam.transform.rotation.eulerAngles}");

            Cinematic2DCamera camCtrl = cam.GetComponent<Cinematic2DCamera>();
            if (camCtrl == null)
            {
                Debug.LogError("[TestCamera] Cinematic2DCamera component is NULL on Camera.main!");
                return;
            }

            Debug.Log($"[TestCamera] Cinematic2DCamera: enabled={camCtrl.enabled}, mode={camCtrl.CurrentMode}, target={camCtrl.Target}");

            GameObject player = GameObject.Find("Player_Minh");
            if (player == null)
            {
                Debug.LogError("[TestCamera] Player_Minh not found in scene!");
                return;
            }

            Vector3 origPlayerPos = player.transform.position;
            Debug.Log($"[TestCamera] Player_Minh pos={origPlayerPos}");
            PlayerController pCtrl = player.GetComponent<PlayerController>();
            Debug.Log($"[TestCamera] PlayerController: enabled={pCtrl != null && pCtrl.enabled}");

            // Test snap to target
            camCtrl.Target = player.transform;
            camCtrl.SnapToTarget();
            Debug.Log($"[TestCamera] After SnapToTarget: cam pos={cam.transform.position}");

            System.Reflection.MethodInfo lateUpdate = typeof(Cinematic2DCamera).GetMethod("LateUpdate", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Test movement and smooth convergence across frames
            for (int i = 1; i <= 15; i++)
            {
                if (i <= 5)
                {
                    player.transform.position += new Vector3(0.5f, 0f, 0f);
                }
                lateUpdate.Invoke(camCtrl, null);
                if (i % 3 == 0 || i == 15)
                {
                    Debug.Log($"[TestCamera] Frame {i}: Player pos.x={player.transform.position.x:F2}, Cam pos.x={cam.transform.position.x:F2}, pos.y={cam.transform.position.y:F2}");
                }
            }

            // Restore player and camera to original clean positions
            player.transform.position = origPlayerPos;
            camCtrl.SnapToTarget();
            Debug.Log($"[TestCamera] Diagnostic finished. Restored player to {origPlayerPos}, cam to {cam.transform.position}");
        }
    }
}
#endif
