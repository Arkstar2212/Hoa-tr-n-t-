---
name: unity-ui-ux-responsive
description: Responsive UI/UX layout, Canvas Scaler settings, anchor alignment, TextMeshPro Vietnamese diacritics, and multi-resolution scaling in Unity 6.
---

# Unity UI/UX & Resolution Scaling Skill

This skill governs the construction, alignment, and testing of user interfaces (Start Menu, Settings, Pause Menu, Dialogues, HUD) across diverse aspect ratios (16:9, 16:10, 21:9 ultrawide, mobile) for **Hoa trên đất đỏ**.

## 1. Canvas Scaler Standard Configuration

Every screen-space Canvas in the project must follow these standard properties:

| Property | Standard Value | Rationale |
| :--- | :--- | :--- |
| **UI Scale Mode** | `Scale With Screen Size` | Adapts dynamically to different screen resolutions. |
| **Reference Resolution** | `1920 x 1080` (X: 1920, Y: 1080) | Standard full HD baseline for game art plates. |
| **Screen Match Mode** | `Match Width Or Height` | Predictable scaling without uneven aspect stretching. |
| **Match** | `0.5` (Landscape balance) | Balances between vertical and horizontal expansion. |
| **Reference Pixels Per Unit** | `100` | Matches sprite texture PPU. |

---

## 2. RectTransform Anchor Rules

- **Never leave RectTransforms anchored to the center while using fixed pixel offsets for screen-edge elements**.
- **Anchor Guidelines**:
  - **Screen Headers / Title**: Anchor `Top-Center` (`Min: (0.5, 1)`, `Max: (0.5, 1)`).
  - **Start Menu Button Column**: Anchor `Middle-Left` or `Center` (`Min: (0.5, 0.5)`, `Max: (0.5, 0.5)`).
  - **Version Info / Copyright**: Anchor `Bottom-Right` (`Min: (1, 0)`, `Max: (1, 0)`).
  - **Full-Screen Background Plates**: Anchor `Stretch All` (`Min: (0, 0)`, `Max: (1, 1)` with `Left: 0, Top: 0, Right: 0, Bottom: 0`).
  - **Safe Area**: For mobile or notch screens, use a `SafeArea` script that offsets RectTransform based on `Screen.safeArea`.

---

## 3. TextMeshPro (TMP) & Vietnamese Font Rendering

- **Vietnamese Character Support**:
  - Fonts MUST support Unicode Latin Extended characters (`ă, â, đ, ê, ô, ơ, ư` and tone marks: sắc, huyền, hỏi, ngã, nặng).
  - Missing glyphs render as square boxes (tofu) or fallback font mismatches.
- **Crisp Text Settings**:
  - Enable `Auto Size` with conservative bounds (e.g., `Min: 18`, `Max: 36`) to avoid overflow on narrow screens.
  - Set `Extra Padding: Checked` to prevent edge clipping on italic or custom glyphs.
  - Set `Sampling Point Size` adequately high in the TMP Font Asset generation to prevent blur.

---

## 4. Menu State Machine Architecture

Manage UI panels cleanly using a central UI Manager:

```csharp
using UnityEngine;

namespace HoaTrenDatDo.UI
{
    public class UIMenuManager : MonoBehaviour
    {
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject pausePanel;

        public void OpenPanel(GameObject targetPanel)
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(mainMenuPanel == targetPanel);
            if (settingsPanel != null) settingsPanel.SetActive(settingsPanel == targetPanel);
            if (pausePanel != null) pausePanel.SetActive(pausePanel == targetPanel);
        }

        public void TogglePause()
        {
            if (pausePanel == null) return;
            bool isPaused = !pausePanel.activeSelf;
            pausePanel.SetActive(isPaused);
            Time.timeScale = isPaused ? 0f : 1f;
        }
    }
}
```

---

## 5. UI Resolution Testing Checklist

Using Unity MCP, test UI behavior before finishing:
1. Capture Game View at 1920x1080 (16:9 standard).
2. Capture Game View at 2560x1080 or 3440x1440 (21:9 ultrawide) — verify background plates do not expose grey void borders.
3. Capture Game View at 1920x1200 (16:10) — verify buttons don't get clipped off the bottom edge.
4. Verify Vietnamese dialogue text displays without missing character glyphs.
