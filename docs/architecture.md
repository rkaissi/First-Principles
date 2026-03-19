---
layout: page
title: Architecture
permalink: /architecture/
---

# Architecture

## Scenes (build order)

Configured in `First Principles/ProjectSettings/EditorBuildSettings.asset`:

- **Menu** — `SceneFader`, entry to level select or other scenes.
- **LevelSelect** — Runtime UI + `LevelSelectController`.
- **Game** — Graph UI, `GameManager` (function plotter + faders + `LevelManager`).

## High-level diagram

```
Menu                    LevelSelect              Game
────                    ───────────              ────
SceneFader              LevelSelectController    Canvas + graph renderers
        LoadLevelSelect   │  (scroll list +       FunctionPlotter
                          │   MathArticlesOverlay / LearningArticleLibrary)
                                 └──► LevelSelection (static)
                                            │
                                            ▼
                                      LevelManager
                                            ├── Applies LevelDefinition → FunctionPlotter / colors
                                            ├── GraphObstacleGenerator → platforms / hazards
                                            ├── PlayerControllerUI2D
                                            └── DerivativePopAnimator (deriv line)
```

## Core scripts (`Assets/Scripts`)

### Game (`Game/`)

| Script | Role |
|--------|------|
| `LevelDefinition` | Scriptable-style level config: function params, derivative rules, colors, story (also built at runtime in samples). |
| `LevelManager` | Orchestrates levels, HUD (stage + controls + story), theme, obstacle regen, restart / advance. |
| `GameLevelCatalog` | Display names + level count. |
| `LevelSelection` | Static bridge: selected index from LevelSelect → Game. |
| `LevelSelectController` | Builds **scrollable** list UI, **Math tips** button; loads **Game**. |
| `LearningArticleLibrary` | Rich-text article snippets (curriculum + engineering) for the overlay. |
| `MathArticlesOverlay` | Full-screen scroll reader on level select. |
| `DeviceLayout`, `MobileUiRoots`, `CanvasSafeAreaBootstrap` | Safe-area insets & mobile layout. |
| `MobileTouchControls`, `MobileInputBridge` | Touch game controls + bridge to `PlayerControllerUI2D`. |
| `GraphObstacleGenerator` | Samples curve/derivative columns → `GraphWorld` (platforms, hazards, finish, spawn). |
| `PlayerControllerUI2D` | Grid-space movement, jump, collisions vs `GridRect` list. |
| `DerivativePopAnimator` | Short “pop” on derivative renderer at stage crossings. |

### Functions (`Functions/`)

| Script | Role |
|--------|------|
| `FunctionPlotter` | Samples `FunctionType`, pushes points to line/derivative renderers, equation TMP. |

### UI (`UI/`)

| Script | Role |
|--------|------|
| `LineRendererUI` / `DerivRendererUI` | UI-space polylines. |
| `GridRendererUI` | Grid mesh. |
| `LabelManager` / axis labels | Axis numbers. |
| `SceneFader` | Fade + `LoadGame` / `LoadLevelSelect` / `LoadMenu`. |

## Coordinate spaces

- **Graph:** points use the plotter’s logical X and grid Y (see grid size on `GridRendererUI`).
- **Platforms:** `GraphObstacleGenerator` maps column index → rectangles in the same grid space; `PlayerControllerUI2D` converts grid ↔ `RectTransform` pixels under **Cartesian Plane**.

## Persistence

- **Level index:** in-memory only (`LevelSelection`), cleared after **Consume** on Game load.

## APIs deprecated in Unity 6

Project code prefers **`FindAnyObjectByType`** and parameterless **`FindObjectsByType`** over deprecated `FindObjectOfType` / sort-mode overloads where applicable.
