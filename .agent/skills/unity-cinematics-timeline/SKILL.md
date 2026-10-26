---
name: unity-cinematics-timeline
description: Opening cinematics, Unity Timeline, Cinemachine 3 camera transitions, cloud descent, fade transitions, audio synchronization, and player control handover.
---

# Unity Cinematics & Timeline Skill

This skill governs the creation and sequencing of cutscenes, camera transitions, and player control handovers in **Hoa trên đất đỏ** using Unity 6, Timeline (`com.unity.timeline 6.6.0`), and Cinemachine 3 (`com.unity.cinemachine 6.6.0`).

## 1. Cinematic Scene Architecture

A standard cinematic sequence consists of:

```
[Play Timeline] 
      │
      ▼
[Input State: Locked (Cutscene Mode)]
      │
      ▼
[Phase 1: Establishing Shot] (High altitude cloud overview)
      │
      ▼
[Phase 2: Camera Descent] (Passing through cloud layers, parallax reveal)
      │
      ▼
[Phase 3: Scene Focus] (Dock / Rice fields / Protagonist)
      │
      ▼
[Phase 4: Timeline Complete Signal]
      │
      ▼
[Input State: Unlocked (Gameplay Handover)] (Seamless blend, zero jitter)
```

---

## 2. Cinemachine 3 (Unity 6) Standards

In Cinemachine 3, virtual cameras are components named `CinemachineCamera`:

- **Camera Blending**:
  - Use `CinemachineBrain` on the Main Camera with standard blend curve `EaseInOut` (duration: 1.5s - 2.5s for narrative pacing).
  - Use Priority switching or Timeline Activation Tracks.
- **Cinematic Framing**:
  - Keep camera damping smooth (`Damping` between 0.8 and 1.5) to avoid robotic jerkiness.
  - Set dead zones appropriately so minor character fidgeting does not cause camera jitter.
- **Orthographic vs Perspective**:
  - If using 2.5D with 3D depth, match FOV and clipping planes (`Near: 0.1`, `Far: 1000`).
  - If pure 2D Orthographic, animate `OrthographicSize` on `CinemachineCamera` to zoom in/out smoothly during descent.

---

## 3. Timeline Composition Guidelines

1. **Director Setup**:
   - `PlayableDirector` on the scene's sequence manager GameObject.
   - `PlayOnAwake`: Set to true for opening scenes, or trigger via code during transitions.
   - `WrapMode`: `None` or `Hold` (hold last frame while gameplay takes over).

2. **Track Types & Separation**:
   - **Activation Track**: Enable/disable specific cinematic props or lighting adjustments.
   - **Cinemachine Track**: Control which virtual camera is active and blend between them.
   - **Animation Track**: Character movements or camera dolly paths.
   - **Audio Track**: Ambience (wind, river, cicadas), music, and synchronized dialogue.
   - **Signal Track**: Trigger game logic events at precise timestamps (e.g., enable HUD, trigger player movement unlock).

---

## 4. Seamless Gameplay Handover Pattern

**Problem**: When a cutscene ends, the player character often snaps, jerks, or teleports if the cutscene animation does not match the player controller's initial state.

**Solution**: State-driven handover controller:

```csharp
using UnityEngine;
using UnityEngine.Playables;

namespace HoaTrenDatDo.Cinematics
{
    public class CinematicController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayableDirector director;
        [SerializeField] private GameObject playerObject;
        [SerializeField] private MonoBehaviour playerInputHandler; // or PlayerInput
        [SerializeField] private Animator playerAnimator;

        [Header("Camera Handover")]
        [SerializeField] private GameObject cutsceneCamera;
        [SerializeField] private GameObject gameplayCamera;

        private void OnEnable()
        {
            if (director != null)
                director.stopped += OnCutsceneFinished;
        }

        private void OnDisable()
        {
            if (director != null)
                director.stopped -= OnCutsceneFinished;
        }

        public void PlayIntroSequence()
        {
            // 1. Lock player input
            if (playerInputHandler != null) playerInputHandler.enabled = false;

            // 2. Play director
            director.Play();
        }

        private void OnCutsceneFinished(PlayableDirector pd)
        {
            // 3. Ensure player animator is in Idle state
            if (playerAnimator != null)
            {
                playerAnimator.Play("Idle", 0, 0f);
            }

            // 4. Activate gameplay camera with Cinemachine blending
            if (cutsceneCamera != null) cutsceneCamera.SetActive(false);
            if (gameplayCamera != null) gameplayCamera.SetActive(true);

            // 5. Unlock player input
            if (playerInputHandler != null) playerInputHandler.enabled = true;

            Debug.Log("[Cinematics] Cutscene completed. Gameplay handed over cleanly.");
        }
    }
}
```

---

## 5. Screen Transitions & Fade Management

- Use a Canvas with a dedicated Sorting Layer (`Cinematic_Fade` or highest order).
- Use `CanvasGroup` alpha fade:
  ```csharp
  public IEnumerator FadeRoutine(float targetAlpha, float duration)
  {
      float startAlpha = canvasGroup.alpha;
      float elapsed = 0f;
      while (elapsed < duration)
      {
          elapsed += Time.deltaTime;
          canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
          yield return null;
      }
      canvasGroup.alpha = targetAlpha;
  }
  ```
- Trigger fades via Timeline Signal Emitters for frame-accurate transitions.
