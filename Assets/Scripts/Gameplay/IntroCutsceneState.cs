using UnityEngine;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// Cross-scene state handshake for the Intro Cutscene pipeline.
    /// Dictates whether New Scene 1_Overhaul should trigger the Vintage Reel Intro Cutscene
    /// or proceed straight to standard gameplay.
    /// </summary>
    public static class IntroCutsceneState
    {
        private const string PrefsKey_ShouldPlay = "HoaVaMau_IntroCutscene_ShouldPlay";
        private const string PrefsKey_HasViewed = "HoaVaMau_IntroCutscene_HasViewedSession";

        private static bool? runtimeShouldPlay = null;
        private static bool runtimeHasViewed = false;

        /// <summary>
        /// True if New Game was selected OR Continue was selected with a fresh save (checkpoint 0).
        /// False if Continue was chosen with existing checkpoint progression.
        /// </summary>
        public static bool ShouldPlayIntroCutscene
        {
            get
            {
                if (runtimeShouldPlay.HasValue) return runtimeShouldPlay.Value;
                return PlayerPrefs.GetInt(PrefsKey_ShouldPlay, 0) == 1;
            }
            set
            {
                runtimeShouldPlay = value;
                PlayerPrefs.SetInt(PrefsKey_ShouldPlay, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// Session guard: tracks if the player already watched the cutscene in this runtime session.
        /// Prevents cutscene from looping on in-session reloads unless explicitly starting a New Game.
        /// </summary>
        public static bool HasViewedInSession
        {
            get => runtimeHasViewed;
            set
            {
                runtimeHasViewed = value;
                PlayerPrefs.SetInt(PrefsKey_HasViewed, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static void MarkForNewGame()
        {
            ShouldPlayIntroCutscene = true;
            HasViewedInSession = false;
        }

        public static void MarkForContinue(int checkpointIndex)
        {
            // If checkpointIndex is 0 (start of game, no checkpoint passed), play cutscene if not yet viewed
            if (checkpointIndex == 0 && !HasViewedInSession)
            {
                ShouldPlayIntroCutscene = true;
            }
            else
            {
                ShouldPlayIntroCutscene = false;
            }
        }

        public static void CompleteIntro()
        {
            ShouldPlayIntroCutscene = false;
            HasViewedInSession = true;
        }
    }
}
