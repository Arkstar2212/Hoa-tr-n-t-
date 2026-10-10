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
            // Intentionally disabled to preserve Player_MeshyBoy model, animations, and verified project assets.
            // Playground building must only be invoked manually if ever needed.
        }
    }
}