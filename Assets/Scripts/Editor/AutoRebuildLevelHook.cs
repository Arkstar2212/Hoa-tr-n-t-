using UnityEditor;
using UnityEngine;

namespace HoaVaMau.Gameplay
{
    [InitializeOnLoad]
    public static class AutoRebuildLevelHook
    {
        private const string PrefKey = "HoaVaMau_SceneRebuild_CharacterPlayground_V4_DragThrowGrassTuft";

        static AutoRebuildLevelHook()
        {
            EditorApplication.delayCall += RebuildOnce;
        }

        private static void RebuildOnce()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorPrefs.GetBool(PrefKey, false)) return;

            EditorPrefs.SetBool(PrefKey, true);
            Debug.Log("<color=#40b8a6>[Hoa và Máu]</color> Auto-rebuilding scene with Planet of Lana 2D Character Playground...");
            CharacterPlaygroundBuilder.BuildMenuCommand();
        }
    }
}