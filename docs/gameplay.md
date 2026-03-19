---
layout: page
title: Gameplay
permalink: /gameplay/
---

# Gameplay

The product **First Principles** is named in part after **Elon Musk**’s comments on a **first-principles approach** to **business** and **problem-solving in life and work**; see **[First principles thinking & business]({% link first-principles-business.md %})** for how that lines up with the graph and derivative gameplay.

## Modes

1. **Graphing calculator** — Functions are plotted on the Cartesian plane; numeric derivatives can be shown as a second curve (`FunctionPlotter`, `LineRendererUI`, `DerivRendererUI`).
2. **Derivative platformer** — On **Game** scene, a small character runs on **platforms** generated from the curve; **gaps / hazards** follow rules based on the **derivative** (`GraphObstacleGenerator`, `PlayerControllerUI2D`).
3. **Graphic calculator mode** — From the **main menu** (green **Graphic calculator mode** button) or the **first row** on **Level select**, loads **Game** with **no platformer**: a **typed equation** field graphs **f(u)** with variable **`x`** in the box (inner \(u = k(x-D)\); on-screen \(y = A\cdot f(u)+C\)). **Trans** (**transformations**: cycle \(A\), \(k\), \(C\), \(D\); double-tap nudges **+**, hold **−**) and **Scale** (**zoom** in on tap, zoom out on hold). **Two-finger pinch** on the graph changes the visible \(x\)-window. Supported syntax includes `+ − * / ^`, parentheses, `sin cos tan`, `asin acos atan`, `sinh cosh tanh`, `sqrt abs`, `log` (base 10), `ln`, `exp`, `min(,) max(,)`, `pi`, `e`, and implicit multiply (e.g. `2x`, `2sin(x)`).

Level parameters (colors, function type, transformation coefficients, story text) are defined in code via **`LevelManager`** sample levels / **`LevelDefinition`** (except this free-graph mode, which uses a fixed starter parabola).

### Sample level lineup (`GameLevelCatalog`)

Titles are built at runtime from **`GameLevelCatalog.DisplayNames`** (order matches **`LevelManager`** sample levels). Expect **40+** named stages spanning primer → integrals → engineering → **AP Calculus BC** motifs → **polar** plots → **Physics C** hooks → **aerospace** teaching curves → **competition math** (\(\ln\), concavity mood) → a **final Mandelbrot escape-slice boss**, including:

| Block | Examples |
|--------|-----------|
| Primer + classics | Primer, parabola, sine, cosine, absolute |
| Series / multivar | Maclaurin e^x & sin, geometric tail, saddle & paraboloid slices |
| Integrals | Area under the curve, Riemann left / right / midpoint |
| Engineering | Damped oscillation, catenary (cosh), full-wave rectified sine `|sin(x)|` |
| AP BC + polar | Arctan, logistic, Maclaurin cos, ln & √x, tan window, e^{kx}, cubic sketching, b^x; polar cardioid & rose; **circle** (upper semicircle / implicit form) |
| Physics C | Exponential decay (τ / RC), projectile parabola, angular momentum / L = Iω (SHM framing) |
| **Aerospace** | Lift \(C_L(\alpha)\) + stall, drag polar, isothermal \(\rho(h)\), phugoid/damped mode, Newtonian \(\sin^{2}\alpha\), Strouhal sine, re-entry decay envelope |

Open **Math tips & snippets** on the level-select screen for **short article text** plus blocks for **first principles thinking (business)** — see **[First principles thinking & business]({% link first-principles-business.md %})** — and **AMC 10/12**, **competition math**, **TMUA**, **MAT**, **AP Calculus BC**, and **AP Physics C**. Longer notes: **`docs/math-concepts.md`**, **`docs/engineering-math.md`**, **`docs/amc-10-12.md`**, **`docs/competition-math.md`**, **`docs/tmua-calculus.md`**, **`docs/mat-calculus.md`**, **`docs/ap-calculus-bc.md`**, **`docs/ap-physics-c.md`**.

Special levels can **tint the grid** and adjust how long story text stays on screen.

## Controls (platformer)

| Input | Action |
|--------|--------|
| **← / →** or **A / D** | Move |
| **Space**, **W**, or **↑** | Jump |
| **Gamepad** (left stick + south face button) | Supported via **[Input System](https://docs.unity3d.com/Packages/com.unity.inputsystem@latest)** |
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
2. **LevelSelect** — `LevelSelectController` builds a **scrollable** list from **`GameLevelCatalog.DisplayNames`**, plus **Math tips & snippets** (`MathArticlesOverlay` / `LearningArticleLibrary`). Choosing a level calls **`LevelSelection.SetSelectedLevel`** and loads **Game**. A bottom **SceneCreditsFooter** matches Menu copy (`SceneCreditsFooter.cs`).
3. **Game** — `LevelManager` reads **`LevelSelection.ConsumeSelectedLevel`**, applies the level theme to **`FunctionPlotter`**, regenerates obstacles, and spawns / resets the player. The same **SceneCreditsFooter** strip appears above the controls hint bar.

## Spawn position

The generator picks a **spawn column** among the “safe start” columns where the **platform top is lowest**, so the player does not always appear at the visually highest part of the early curve.

## Win / death

- **Hazards** or falling below a Y threshold causes **respawn** on the same level.
- Reaching the **finish zone** (far right) **advances** to the next sample level (wraps when last is complete).

## UI polish

- **Story** text appears at the top with the **level name** and fades (TMP).
- **Math concepts** (top-right during play) opens the same scrollable **LearningArticleLibrary** reader as Level select’s *Math tips & snippets* — including an on-device explanation of how the main graph, derivative, platforms, Riemann strips, and stages map to calculus.
- **Controls** hint bar at the bottom shows move / jump hints.
- **Stage** panel uses a dark “glass” backing and accent strip consistent with the Limbo-like aesthetic.
