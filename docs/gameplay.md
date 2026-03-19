---
layout: page
title: Gameplay
permalink: /gameplay/
---

# Gameplay

## Modes

1. **Graphing calculator** — Functions are plotted on the Cartesian plane; numeric derivatives can be shown as a second curve (`FunctionPlotter`, `LineRendererUI`, `DerivRendererUI`).
2. **Derivative platformer** — On **Game** scene, a small character runs on **platforms** generated from the curve; **gaps / hazards** follow rules based on the **derivative** (`GraphObstacleGenerator`, `PlayerControllerUI2D`).

Level parameters (colors, function type, transformation coefficients, story text) are defined in code via **`LevelManager`** sample levels / **`LevelDefinition`**.

### Sample level lineup (`GameLevelCatalog`)

Titles are built at runtime from **`GameLevelCatalog.DisplayNames`** (order matches **`LevelManager`** sample levels). Expect **30+** named stages spanning primer → integrals → engineering → **AP Calculus BC** motifs → **polar** plots → **Physics C** hooks, including:

| Block | Examples |
|--------|-----------|
| Primer + classics | Primer, parabola, sine, cosine, absolute |
| Series / multivar | Maclaurin e^x & sin, geometric tail, saddle & paraboloid slices |
| Integrals | Area under the curve, Riemann left / right / midpoint |
| Engineering | Damped oscillation, catenary (cosh), full-wave rectified sine `|sin(x)|` |
| AP BC + polar | Arctan, logistic, Maclaurin cos, ln & √x, tan window, e^{kx}, cubic sketching, b^x; polar cardioid & rose; **circle** (upper semicircle / implicit form) |
| Physics C | Exponential decay (τ / RC), projectile parabola, angular momentum / L = Iω (SHM framing) |

Open **Math tips & snippets** on the level-select screen for **short article text** plus **separate** blocks for **TMUA**, **MAT**, **AP Calculus BC**, and **AP Physics C**. Longer notes: **`docs/math-concepts.md`**, **`docs/engineering-math.md`**, **`docs/tmua-calculus.md`**, **`docs/mat-calculus.md`**, **`docs/ap-calculus-bc.md`**, **`docs/ap-physics-c.md`**.

Special levels can **tint the grid** and adjust how long story text stays on screen.

## Controls (platformer)

| Input | Action |
|--------|--------|
| **← / →** or **A / D** | Move |
| **Space**, **W**, or **↑** | Jump |
| **Jump** axis / gamepad | Still supported via Unity **Input Manager** |
| **Touch** (phones / tablets) | **On-screen ◀ ▶ and Jump** when the build is mobile/touch-capable; safe-area layout keeps them above the home indicator. Keyboard still works on devices that have one. |

### Mobile-friendly UI

- **CanvasScaler** uses reference **1080×1920** with a **dynamic** `matchWidthOrHeight` (via `CanvasSafeAreaBootstrap`): phones stay near **0.42–0.45**; **~4:3 iPad / tablet** sizes lean toward **0.5–0.52** so landscape and portrait both stay balanced.
- **Tablet detection** uses shortest side in **dp** (`DeviceLayout.IsTabletLike`, threshold ~592dp) so high-res phones are not mistaken for iPads.
- **`CanvasSafeAreaBootstrap`** wraps existing Canvas children in a **`_SafeContent`** root inset with **`Screen.safeArea`** (notches / rounded corners).
- Runtime HUD (**story**, **stage**, **controls hint**, level-select list, math overlay) parents to that safe root where applicable.

Legacy **Trans** / **Scale** tuning buttons on the Game UI are **disabled**; tuning is driven by **level definitions**, not free sliders.

## Stages

- The run is split into **stages** (derivative **“pop”** moments at X thresholds).
- A **stage HUD** (top-left) shows **Stage *k* / *n*** using the same typography family as the equation label.
- Crossing a stage line triggers **`DerivativePopAnimator`** (visual emphasis on the derivative).

## Level flow

1. **Menu** — Entry and scene fade; footer shows **Credits** (GAME GENESIS × ORCH AEROSPACE), version, proprietary / initiative line, and **Unity** trademark notice ([`CREDITS.md`](../CREDITS.md) for full attribution).
2. **LevelSelect** — `LevelSelectController` builds a **scrollable** list from **`GameLevelCatalog.DisplayNames`**, plus a **Math tips & snippets** button that opens **`MathArticlesOverlay`** (`LearningArticleLibrary`). Choosing a level calls **`LevelSelection.SetSelectedLevel`** and loads **Game**.
3. **Game** — `LevelManager` reads **`LevelSelection.ConsumeSelectedLevel`**, applies the level theme to **`FunctionPlotter`**, regenerates obstacles, and spawns / resets the player.

## Spawn position

The generator picks a **spawn column** among the “safe start” columns where the **platform top is lowest**, so the player does not always appear at the visually highest part of the early curve.

## Win / death

- **Hazards** or falling below a Y threshold causes **respawn** on the same level.
- Reaching the **finish zone** (far right) **advances** to the next sample level (wraps when last is complete).

## UI polish

- **Story** text appears at the top with the **level name** and fades (TMP).
- **Controls** hint bar at the bottom shows move / jump hints.
- **Stage** panel uses a dark “glass” backing and accent strip consistent with the Limbo-like aesthetic.
