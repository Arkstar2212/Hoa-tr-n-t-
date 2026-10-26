---
name: hoa-do-art-direction
description: Visual art direction, watercolor hand-painted aesthetic, color palettes, atmospheric perspective, and strict asset integrity rules for Hoa trên đất đỏ.
---

# Hoa Trên Đất Đỏ — Art Direction & Visual Identity

This skill establishes and enforces the core artistic rules, visual language, and aesthetic standards for the project **Hoa trên đất đỏ**.

## 1. Core Visual Identity: Hand-Painted Historical Watercolor

- **Style**: Soft-brush, hand-painted storybook/picture book aesthetic (phong cách tranh vẽ tay màu nước, brush mềm, viền nét hữu cơ, ít chi tiết rườm rà).
- **Mood**: Nostalgic, poetic, evocative, melancholic yet gentle (trầm lắng, hoài niệm, đậm chất thơ và lịch sử).
- **Atmospheric Depth**: Strong atmospheric perspective (chiều sâu không khí). Distant layers blend into ambient haze; edges soften into mist.
- **Strictly Prohibited**:
  - No hyper-realistic CGI or glossy plastic 3D renders.
  - No neon, oversaturated anime/fantasy color palettes.
  - No generic AI-generated "plastic" surfaces or messy hallucinations.
  - No harsh, unblended vector cartoon outlines.

---

## 2. Color Palette & Lighting Harmony

| Environment State | Dominant Tones | Accent Colors | Atmosphere |
| :--- | :--- | :--- | :--- |
| **Sunset / Hoàng hôn** | Warm Ochre, Terra-cotta Red, Dusky Gold | Deep Mauve, Burnt Amber | Golden dust particles, soft glowing rim light |
| **Twilight / Chạng vạng** | Indigo, Muted Violet, Charcoal Slate | Warm Oil Lamp Yellow, Distant Embers | Soft blue-purple mist, cozy point lights |
| **Dawn / Bình minh** | Pale Sage, Morning Dew Silver, Soft Teal | Faint Rose, Early Sun Gold | Rising river vapor, high-key ambient light |
| **Noon / Trưa hè Nam Bộ**| Washed-out Alluvial Brown, Olive Green | Bleached Sunlight White | Heat haze, sharp vertical shadows under eaves |

---

## 3. ABSOLUTE Asset Integrity & Authenticity Rules

As mandated by project rules:
1. **NEVER Crop Out or Blindly Rescale Project Assets**:
   - Official concept arts and background plates located in `Assets/Art`, `CONCEPT ARTS`, and `Textures` have carefully composed aspect ratios and framing.
   - Never crop edges to force-fit a container unless explicitly specified by the human lead artist.
2. **Never Generate Mock/Placeholder Substitutes**:
   - If a scene requires an asset (e.g. background plate, character sprite, prop), search the existing project asset directories first:
     - `Assets/Art/`
     - `CONCEPT ARTS/`
     - `Assets/Textures/`
   - Never inject AI-generated placeholder images that conflict with the established art direction.
3. **Verify Resolution and PPU**:
   - Ensure sprite import settings preserve original fidelity without crunchy compression artifacts.

---

## 4. 2D/2.5D Lighting & URP Shader Pipeline

- Use **Universal Render Pipeline (URP)** CelShadeLit or 2D Lit shaders configured with:
  - Soft ramp falloffs rather than hard stark toon-shading lines.
  - Ambient light tuned to the scene's atmospheric tone (e.g., warm golden ambient in sunset scenes).
  - Subtle normal maps or height maps only when enhancing painterly brush strokes.
