#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace HoaVaMau.Editor
{
    public static class DiagShadows
    {
        [MenuItem("Hoa va Mau/Diagnostics/Inspect Underground Renderers")]
        public static void Inspect()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/New Scene 1_Overhaul_Refined.unity", OpenSceneMode.Single);
            var rootObjs = scene.GetRootGameObjects();
            foreach (var go in rootObjs)
            {
                Debug.Log($"[Root GO] {go.name} (Active: {go.activeSelf})");
            }
        }
    }
}
#endif
