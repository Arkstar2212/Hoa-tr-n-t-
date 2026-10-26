# Project Rules - Hoa trên đất đỏ

- **Project Root**: `C:\Users\Arkstarz\Documents\GitHub\Hoa-tr-n-t-` (Dự án chính thức: **Hoa trên đất đỏ**)
- **Unity Version**: Unity 6 (6000.x) with URP (Universal Render Pipeline) and New Input System.

## 1. Asset Integrity & Authenticity (CRITICAL)
- NEVER crop out, alter, or use images/assets that are not the correct, official, and intended project assets.
- Always verify asset paths, aspect ratios, and resolutions directly from the designated asset folders (`Assets/Art`, `CONCEPT ARTS`, `Assets/Textures`).
- Never substitute unverified, incorrectly cropped, or mock assets when building scenes or assigning materials.
- Adhere strictly to the soft-brush watercolor aesthetic, muted historical palette, and 1950s Nam Bộ / Cà Mau setting.

## 2. Active Project Skills (`.agents/skills/`)
When performing development tasks, refer to and adhere to the project's specialized skills:
- **`unity-csharp-standards`**: C# 9+ architecture, lifecycle caching, event decoupling, and New Input System standards.
- **`unity-code-audit`**: Pre/post-change audit, NullReference prevention, and missing Inspector reference detection.
- **`unity-2d-environment-parallax`**: Sorting layers (`Background_Sky` to `UI`), parallax factor math, cloud drifting.
- **`unity-cinematics-timeline`**: Timeline directors, Cinemachine 3 virtual cameras, smooth control handover.
- **`unity-ui-ux-responsive`**: Canvas Scaler (1920x1080 Match 0.5), anchor alignment, TextMeshPro Vietnamese diacritics.
- **`unity-animation-character-controller`**: Rigidbody2D locomotion (`rb.linearVelocity`), Animator parameters, input freeze.
- **`hoa-do-art-direction`**: Watercolor hand-painted visual identity, historical lighting palettes, atmospheric depth.
- **`vietnamese-environment-art`**: Authentic 1950s Nam Bộ foliage (dừa nước, bần, tràm), wooden docks, xuồng ba lá.
- **`unity-cinematic-workflow`**: Scene opening pipeline (cloud descent -> dock reveal -> gameplay handover).
- **`unity-ui-visual-qa`**: Multi-resolution visual verification and typography audit using Unity MCP.

## 3. Unity MCP Live Verification Workflow
- Before finishing any script change or scene modification, use the connected `unity` MCP server tools:
  - `editor_status` & `get_console_logs`: Confirm zero compilation errors and zero runtime exceptions.
  - `capture_game_view`: Visually verify camera framing, parallax layers, and UI alignment.
  - `eval`: Inspect live hierarchy or component states when needed.

