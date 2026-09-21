using UnityEngine;
using UnityEditor;
using HoaVaMau.Gameplay;

namespace HoaVaMau.Editor
{
    [InitializeOnLoad]
    public static class CinematicEnvironmentEditor
    {
        static CinematicEnvironmentEditor()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }
        }

        [MenuItem("Hoa va Mau/Rebuild Character Playground (Planet of Lana)")]
        public static void RebuildPlaygroundMenuCommand()
        {
            CharacterPlaygroundBuilder.BuildMenuCommand();
        }

        [MenuItem("Hoa va Mau/Start Action (Play Game) %#p")]
        public static void StartActionMenuCommand()
        {
            EditorApplication.isPlaying = true;
        }
    }
}
