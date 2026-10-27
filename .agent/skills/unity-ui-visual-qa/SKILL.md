---
name: unity-ui-visual-qa
description: Visual quality assurance, multi-resolution verification, Vietnamese typography audit, and game view capture using Unity MCP.
---

# Unity UI Visual QA & Verification Skill

This skill defines the testing protocol for UI, menu screens, dialogue boxes, and visual presentation in **Hoa trên đất đỏ**, utilizing live Unity MCP tools.

## 1. Automated Visual QA Loop

Instead of guessing whether a UI layout looks right, execute this visual QA loop:

```
[Open Target Scene (MCP)]
        │
        ▼
[Enter Play Mode (MCP: editor_play)]
        │
        ▼
[Capture Game View (MCP: capture_game_view)]
        │
        ▼
[Inspect Output Artifact / Screenshot]
  - Check alignment against screen edges
  - Check text clarity & diacritic rendering
  - Check contrast over hand-painted backdrop
        │
        ▼
[Test Different Aspect Ratios / Resolutions]
        │
        ▼
[Exit Play Mode (MCP: editor_stop)]
```

---

## 2. Key Visual QA Checks

### 1. Typography & Diacritics (Chữ tiếng Việt)
- Verify that Vietnamese characters with complex diacritics (e.g., `ở, ử, ễ, ậ, ắ, đ`) render using the intended font rather than falling back to system Arial or producing blank boxes (tofu `□`).
- Check that text padding does not truncate accents at the top of the RectTransform bounding box.

### 2. Contrast & Readability over Watercolor Backdrops
- Hand-painted watercolor backgrounds often have dynamic warm/cool areas.
- UI text must have subtle drop shadows or backing panels (soft dark parchment or translucent rice paper effect) to remain readable over light sky and dark water.

### 3. Resolution & Safe Areas
- **16:9 (1920x1080)**: Baseline standard. Verify all buttons fit comfortably without crowding.
- **21:9 Ultrawide**: Verify that background art does not cut off with visible scene boundaries or void edges. Canvas elements should stay anchored to their intended corners or center column.
- **16:10 (1920x1200)**: Verify that bottom HUD or navigation bars are not pushed off-screen.

---

## 3. Unity MCP Commands for QA

1. **Capture Current Game View**:
   ```json
   call_mcp_tool: {
     "ServerName": "unity",
     "ToolName": "capture_game_view",
     "Arguments": {}
   }
   ```

2. **Verify Canvas Scaler via Code Evaluation**:
   ```json
   call_mcp_tool: {
     "ServerName": "unity",
     "ToolName": "eval",
     "Arguments": {
       "code": "var scaler = UnityEngine.Object.FindFirstObjectByType<UnityEngine.UI.CanvasScaler>(); if(scaler == null) return \"No CanvasScaler\"; return $\"Mode: {scaler.uiScaleMode}, RefRes: {scaler.referenceResolution}, Match: {scaler.matchWidthOrHeight}\";"
     }
   }
   ```

3. **Check for Inactive or Overlapping Canvases**:
   Inspect active canvases in hierarchy to ensure multiple full-screen overlay canvases aren't blocking raycasts or fighting for draw calls.
