using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using HoaVaMau.Gameplay;

namespace HoaVaMau.Editor
{
    public static class CaptureValidationScreenshot
    {
        [MenuItem("Hoa va Mau/Rebuild And Capture Playground")]
        public static void RebuildAndCapture()
        {
            CharacterPlaygroundBuilder.BuildMenuCommand();
            CaptureAllPlaygroundViews();
        }

        [MenuItem("Hoa va Mau/Capture Playground Screenshots")]
        public static void CaptureAllPlaygroundViews()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            Camera cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("Camera.main not found!");
                return;
            }

            Vector3 origPos = cam.transform.position;
            Quaternion origRot = cam.transform.rotation;

            // View 1: Protagonist on Runway facing Ledges (Station 1 & 2)
            CaptureCameraView(cam, new Vector3(-6.5f, 2.7f, -21.0f), Quaternion.identity, "Capture_Playground_Runway_Ledges.png");

            // View 2: Tunnel & Push-Pull Crate Puzzle (Station 3 & 4)
            CaptureCameraView(cam, new Vector3(23.5f, 3.6f, -22.5f), Quaternion.identity, "Capture_Playground_Tunnel_Crate.png");

            // View 3: Verlet Rope Chasm (Station 5)
            CaptureCameraView(cam, new Vector3(45.0f, 5.0f, -17.0f), Quaternion.identity, "Capture_Playground_Rope_Throw.png");

            // View 4: Pick Up & Throw Sanctuary (Station 6)
            CaptureCameraView(cam, new Vector3(61.5f, 4.8f, -14.5f), Quaternion.identity, "Capture_Playground_Throw_Sanctuary.png");

            // View 5: Protagonist Character Close-up
            CaptureCameraView(cam, new Vector3(-17.0f, 1.15f, -4.5f), Quaternion.identity, "Capture_Playground_Protagonist_Detail.png");

            // Restore camera
            cam.transform.position = origPos;
            cam.transform.rotation = origRot;
            var camCtrl = cam.GetComponent<Cinematic2DCamera>();
            if (camCtrl != null)
            {
                camCtrl.FindPlayerReference();
                camCtrl.SnapToTarget();
            }

            Debug.Log("<color=#40b8a6>[CaptureValidation]</color> All 5 Playground screenshots captured successfully!");
        }

        private static void CaptureCameraView(Camera cam, Vector3 position, Quaternion rotation, string filename)
        {
            cam.transform.position = position;
            cam.transform.rotation = rotation;

            int width = 1920;
            int height = 1080;
            RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture prevRt = cam.targetTexture;
            cam.targetTexture = rt;

            cam.Render();

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            cam.targetTexture = prevRt;
            RenderTexture.active = null;
            Object.DestroyImmediate(rt);

            byte[] bytes = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            string outDir = Path.Combine(Application.dataPath, "../Screenshots");
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

            string outPath = Path.Combine(outDir, filename);
            File.WriteAllBytes(outPath, bytes);
            Debug.Log("<color=#40b8a6>[CaptureValidation]</color> Captured view to: " + outPath);
        }
    }
}
