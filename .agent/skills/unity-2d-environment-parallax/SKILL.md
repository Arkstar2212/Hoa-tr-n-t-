---
name: unity-2d-environment-parallax
description: Multi-layer 2D environment construction, cloud sprites, sorting layers, camera scrolling, atmospheric depth, and parallax for Hoa trên đất đỏ.
---

# Unity 2D Environment & Parallax Skill

This skill guides the construction, layer hierarchy, and parallax math for 2D and 2.5D scenic backdrops in **Hoa trên đất đỏ**.

## 1. Sorting Layers Architecture

To prevent visual sorting bugs and z-fighting, all 2D renderers (SpriteRenderer, ParticleSystem, UGUI Canvas) must strictly use the standardized Sorting Layers:

| Layer Order | Sorting Layer Name | Content Description |
| :---: | :--- | :--- |
| **0** | `Background_Sky` | Far sky gradient, distant sun/sunset, moon |
| **1** | `Background_FarClouds` | High altitude slow-drifting cumulus/cirrus clouds |
| **2** | `Background_DistantHills` | Distant mountain silhouettes with heavy atmospheric haze |
| **3** | `Background_Mid` | Mid-ground tree lines, river banks, distant boats |
| **4** | `Environment_Back` | Props and buildings behind the player walking line |
| **5** | `Gameplay_Entities` | Player character, interactive NPCs, pick-ups, enemies |
| **6** | `Environment_Front` | Foliage, water coconut palms (dừa nước) in front of player |
| **7** | `Foreground_Overlay` | Cinematic framing branches, vignettes, screen fog, dust particles |
| **8** | `Cinematic_Fade` | Full-screen fade quads or Canvas transitions |
| **9** | `UI` | Screen-space and world-space UI elements |

---

## 2. Parallax Scrolling Implementation

### Parallax Component Pattern
A clean, jitter-free parallax effect ties sprite position to camera movement:

```csharp
using UnityEngine;

namespace HoaTrenDatDo.Environment
{
    [ExecuteAlways]
    public class ParallaxLayer : MonoBehaviour
    {
        [Tooltip("0 = fixed to camera (infinite distance); 1 = moves 1:1 with camera (foreground)")]
        [Range(0f, 2f)]
        [SerializeField] private Vector2 parallaxFactor = new Vector2(0.5f, 0.2f);
        
        [SerializeField] private bool infiniteHorizontal = false;
        [SerializeField] private float textureUnitSizeX = 0f;

        private Transform cameraTransform;
        private Vector3 lastCameraPosition;

        private void Start()
        {
            if (Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
                lastCameraPosition = cameraTransform.position;
            }
        }

        private void LateUpdate()
        {
            if (cameraTransform == null)
            {
                if (Camera.main != null) cameraTransform = Camera.main.transform;
                else return;
            }

            Vector3 deltaMovement = cameraTransform.position - lastCameraPosition;
            transform.position += new Vector3(
                deltaMovement.x * (1f - parallaxFactor.x),
                deltaMovement.y * (1f - parallaxFactor.y),
                0f
            );
            
            lastCameraPosition = cameraTransform.position;

            // Optional seamless infinite wrapping
            if (infiniteHorizontal && textureUnitSizeX > 0f)
            {
                if (Mathf.Abs(cameraTransform.position.x - transform.position.x) >= textureUnitSizeX)
                {
                    float offsetPositionX = (cameraTransform.position.x - transform.position.x) % textureUnitSizeX;
                    transform.position = new Vector3(cameraTransform.position.x + offsetPositionX, transform.position.y);
                }
            }
        }
    }
}
```

---

## 3. Cloud Layering & Atmospheric Fog

- **Layer Depth via Color Tinting**: Far layers should blend towards the atmospheric haze color (soft warm golden-orange at sunset, pale misty blue-grey at dawn).
  - Far cloud sprite tint: lower alpha (40-60%) or tinted with background atmosphere.
  - Near cloud sprite tint: crisp, distinct watercolor brush edges.
- **Drift Motion**: Combine camera-driven parallax with continuous slow autonomous wind drift:
  ```csharp
  transform.position += Vector3.right * (windDriftSpeed * Time.deltaTime);
  ```

---

## 4. Asset Integrity & Sprite Setup Rules

- **Pixels Per Unit (PPU)**: Maintain consistent PPU across environment assets (standard: 100 PPU or match project texture settings). Inconsistent PPU causes pixel shimmer and mismatched scale.
- **Pivot Consistency**:
  - Grounded objects (trees, houses, props): Pivot at `Bottom Center`.
  - Clouds, skies, full-bleed backgrounds: Pivot at `Center` or `Top Center`.
- **Texture Compression**: Use `Point (no filter)` ONLY if pixel art; for hand-painted watercolor art, use `Bilinear` filtering with Crisp / High Quality compression to preserve painterly strokes without blur.
- **Preserve Concept Framing**: Never stretch or non-uniformly scale background art plates. Maintain original aspect ratio.
