---
name: unity-cinematic-workflow
description: Standardized cutscene and cinematic pipeline for Hoa trên đất đỏ — cloud descent, Timeline directors, Cinemachine blending, fade transitions, and control handover.
---

# Hoa Trên Đất Đỏ — Cinematic Scene Workflow

This skill standardizes the end-to-end production pipeline for opening sequences, story vignettes, and camera cutscenes in **Hoa trên đất đỏ**.

## 1. The Signature Opening: Cloud Descent Pipeline

The game's narrative hallmark is the opening descent from celestial/cloud layers down into the historical Nam Bộ landscape:

```
[Start Scene]
      │
      ▼
[Phase 1: High Cloud Sea]
  - Main Camera at high vertical Y offset.
  - Slow horizontal drift of cirrus clouds (`Background_FarClouds`).
  - Distant sunset/dawn glow behind mountain horizon silhouette.
  - Sound: High wind whistle, gentle introductory chord.
      │
      ▼
[Phase 2: Vertical Descent (Cinemachine Dolly/Path)]
  - Camera descends smoothly along Y axis with EaseInOut curve.
  - Passes through foreground cloud wisps (`Foreground_Overlay`) with alpha fade.
  - Depth parallax reveals mid-ground river and tree canopy.
  - Sound: Wind subsides, ambient river sounds, distant cicadas emerge.
      │
      ▼
[Phase 3: Dock / Rice Field Reveal]
  - Camera levels off horizontally to match gameplay eye level.
  - Framing settles on the protagonist standing at the dock or path.
  - Title card or location stamp fades in: "Cà Mau, 1954".
      │
      ▼
[Phase 4: Player Control Handover]
  - Timeline reaches final frame marker (Signal Emitter).
  - UI HUD fades in.
  - Character input unlocks with zero positional snap or animation pop.
```

---

## 2. Setting Up Timeline & Cinemachine in Unity MCP

When configuring or inspecting a cinematic scene via Unity MCP:

1. **Check Director Bindings**:
   ```json
   call_mcp_tool: {
     "ServerName": "unity",
     "ToolName": "eval",
     "Arguments": {
       "code": "var director = UnityEngine.Object.FindFirstObjectByType<UnityEngine.Playables.PlayableDirector>(); if (director == null) return \"No PlayableDirector in scene\"; return $\"Director playing {director.playableAsset?.name}, time: {director.time}/{director.duration}\";"
     }
   }
   ```

2. **Verify Camera Tracking**:
   Inspect active Cinemachine cameras and their priorities using MCP `eval`.

3. **Check Signal Receivers**:
   Ensure all `SignalReceiver` components have valid method bindings for:
   - `OnFadeOutStart()`
   - `OnTitleCardShow()`
   - `OnGameplayControlEnable()`

---

## 3. Audio & Ambiance Synchronization

- **Cross-fading Audio**:
  - Sky ambiance (high-altitude wind track) fades out over 3.0s as camera descends below $Y = 15$.
  - Wetland ambiance (water lap, crickets, rustling reeds) fades in proportionally.
- Use Timeline Audio Tracks or dedicated `AudioSource` triggers attached to Signal Emitters.

---

## 4. Cinematic QA Checklist

Before finalizing any cutscene:
- [ ] Is camera movement free of micro-stutters?
- [ ] Do foreground clouds fade cleanly without clipping into the camera's near clipping plane?
- [ ] Does the player character remain rooted and grounded throughout the sequence?
- [ ] When input unlocks, does pressing movement keys instantly transition the character from idle to walking smoothly?
- [ ] Is the audio mix balanced (dialogue/chords audible above atmospheric wind)?
