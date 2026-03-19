---
layout: page
title: Optional Unity assets (visual upgrade)
permalink: /optional-unity-assets/
---

# Optional Unity assets (visual upgrade)

The project ships with **procedural rounded UI** (`RuntimeUiPolish`) so it looks decent **without** downloading anything. To go further, import packs **yourself** in the Unity Editor (Asset Store or `.unitypackage`).

## Free / popular starting points

| Pack / style | Notes |
|--------------|--------|
| **Kenney.nl UI packs** | CC0-style interface tiles; swap `Image.sprite` on buttons/panels. |
| **TextMesh Pro** | Already bundled; try different **font assets** (OFL fonts) for title/body. |
| **2D Simple Nature / backgrounds** | Parallax layers behind the graph (separate Canvas or world space). |
| **Particle systems** | Subtle dust or glow on jump (Gameplay feel, not required). |

## How to integrate after import

1. Drop sprites into `Assets/` (e.g. `Assets/Art/UI/`).
2. Set **Texture Type: Sprite (2D and UI)**, **Mesh Type: Full Rect**, enable **9-slice** borders if needed.
3. Replace `RuntimeUiPolish.Rounded9Slice` usage with your sprite in:
   - `LevelSelectController` (buttons + panel),
   - `MobileTouchControls`,
   - `GraphObstacleGenerator` (platform `Image.sprite`),
   - or keep procedural rounds for consistency.

## Licensing

Verify **each pack’s license** before shipping (commercial use, redistribution, attribution). Proprietary game code does not automatically cover third-party art.

---

*Unofficial guide for **First Principles** maintainers.*
