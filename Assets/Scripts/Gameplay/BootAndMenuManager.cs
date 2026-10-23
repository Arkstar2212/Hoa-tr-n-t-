using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HoaVaMau.Gameplay
{
    /// <summary>
    /// PHẦN 1: Boot Sequence & Start Menu Manager for "Hoa trên Đất Đỏ".
    /// Coordinates:
    /// 1. Splash Sequence:
    ///    - Step 1: Team Logo on pure black (#000000) - Fade in (1s) -> Hold (1.5s) -> Fade out (1s).
    ///    - Step 2: Headphone advisory screen - Fade in (1s) -> Hold (2s) -> Fade out (1s).
    ///    - Step 3: Main Menu with scenic living vista & BGM.
    /// 2. Save / Checkpoint Navigation Logic:
    ///    - "New Game" OR "Continue" with unstarted/empty save (checkpoint 0):
    ///      Loads Scene 1 (New Scene 1_Overhaul) with shouldPlayIntroCutscene = true.
    ///    - "Continue" with valid progression (checkpoint > 0):
    ///      Loads Scene 1 with shouldPlayIntroCutscene = false, restoring An at saved checkpoint.
    /// </summary>
    [DisallowMultipleComponent]
    public class BootAndMenuManager : MonoBehaviour
    {
        public static BootAndMenuManager Instance { get; private set; }

        [Header("Target Gameplay Scene")]
        [SerializeField] private string targetGameplaySceneName = "New Scene 1_Overhaul";

        [Header("Menu System Reference")]
        [SerializeField] private PlanetOfLanaStartMenuSystem startMenuSystem;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (startMenuSystem == null)
            {
                startMenuSystem = GetComponent<PlanetOfLanaStartMenuSystem>();
                if (startMenuSystem == null)
                {
                    startMenuSystem = UnityEngine.Object.FindAnyObjectByType<PlanetOfLanaStartMenuSystem>();
                }
            }
        }

        /// <summary>
        /// Handles 'New Game' request for a specific slot.
        /// Commits fresh save (checkpoint 0) and flags cutscene to play.
        /// </summary>
        public void HandleNewGame(int slotIndex = 1)
        {
            Debug.Log($"<color=#10b981>[BootAndMenuManager]</color> Starting New Game in Slot {slotIndex}. Intro cutscene enabled.");
            IntroCutsceneState.MarkForNewGame();
            HoaVaMauSaveSystem.StartNewGame(slotIndex, targetGameplaySceneName);
        }

        /// <summary>
        /// Handles 'Continue' request for a specific slot.
        /// Inspects checkpoint progression:
        /// - If checkpoint == 0: triggers intro cutscene.
        /// - If checkpoint > 0: bypasses cutscene and sets checkpoint position.
        /// </summary>
        public void HandleContinue(int slotIndex = 1)
        {
            var data = HoaVaMauSaveSystem.GetSlotData(slotIndex);
            int cpIndex = (data != null && data.isOccupied) ? data.checkpointIndex : 0;

            if (cpIndex == 0)
            {
                Debug.Log($"<color=#fbbf24>[BootAndMenuManager]</color> Continuing Slot {slotIndex} at initial start. Intro cutscene enabled.");
                IntroCutsceneState.MarkForNewGame();
            }
            else
            {
                Debug.Log($"<color=#38bdf8>[BootAndMenuManager]</color> Continuing Slot {slotIndex} at checkpoint {cpIndex}. Intro cutscene bypassed.");
                IntroCutsceneState.MarkForContinue(cpIndex);
            }

            HoaVaMauSaveSystem.ContinueGame(slotIndex, targetGameplaySceneName);
        }

        /// <summary>
        /// Checks if current save data allows continuing.
        /// </summary>
        public bool CanContinue()
        {
            return HoaVaMauSaveSystem.HasAnySaveData();
        }
    }
}
