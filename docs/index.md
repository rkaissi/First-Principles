---
layout: default
title: Home
---

# First Principles — documentation

**Release: Beta 1.0**

**First Principles** is a **Unity 6** project (proprietary; see [`LICENSE`](../LICENSE)) that combines a **graphing calculator** (functions and numeric derivatives on a grid) with a **Limbo-inspired 2D platformer**: platforms and hazards are driven by the curve and its derivative, with **staged progression** and per-level themes (including primer, series, multivariable slices, **integral / Riemann-sum** stages, and **engineering math** graphs). **Readable article snippets** live in **`docs/math-concepts.md`**, **`docs/engineering-math.md`**, and **four separated exam-prep notes** — **`docs/tmua-calculus.md`**, **`docs/mat-calculus.md`**, **`docs/ap-calculus-bc.md`**, **`docs/ap-physics-c.md`** (all unofficial; not past papers)—plus in-app **Level select → Math tips & snippets** with matching **TMUA / MAT / AP BC / AP Physics C** blocks.

Credits: **GAME GENESIS** ([Rayan Kaissi](https://github.com/GameGenesis)) × **ORCH AEROSPACE** ([John Wonmo Seong](https://github.com/wonmor)), as part of *College Math For Toddlers*. **Proprietary** — [`LICENSE`](../LICENSE). Full attribution: [`CREDITS.md`](../CREDITS.md).

## Quick links

| Guide | Description |
|------|----------------|
| [Setup]({% link setup.md %}) | Unity version, clone, open the correct project folder |
| [Gameplay]({% link gameplay.md %}) | Controls, stages, level select, how the graph affects the world |
| [Math concepts & snippets]({% link math-concepts.md %}) | Plain-language notes for every curriculum theme in the game |
| [Engineering math]({% link engineering-math.md %}) | Damped motion, catenary, AC rectification, phasors/transforms (intro) |
| [TMUA — calculus]({% link tmua-calculus.md %}) | UK TMUA — MCQ-style calculus topic map (unofficial) |
| [MAT — calculus]({% link mat-calculus.md %}) | UK MAT (Oxford-style) — reasoning & calculus lens (unofficial) |
| [AP Calculus BC — prep]({% link ap-calculus-bc.md %}) | US AP BC — syllabus topics & in-game map (unofficial) |
| [AP Physics C — prep]({% link ap-physics-c.md %}) | US AP Physics C — calculus-first mechanics/E&M hooks (unofficial) |
| [Architecture]({% link architecture.md %}) | Scenes, scripts, data flow |
| [Troubleshooting]({% link troubleshooting.md %}) | Package cache, TextMeshPro, GitHub Pages / `baseurl` |

## Repository layout

```
First-Principles/                 ← git repository root (this site: /docs)
├── docs/                         ← GitHub Pages source (you are here)
├── README.md
├── clean-unity-library.sh
└── First Principles/             ← Unity project (note the space)
    ├── Assets/
    ├── Packages/
    ├── ProjectSettings/
    └── ...
```

Always open the **`First Principles`** folder (the one that contains `Assets` and `ProjectSettings`) in Unity Hub — not the parent git folder alone.

## External links

- [YouTube — demo](https://www.youtube.com/watch?v=yo540yl4Xhs)
- [Wiki — official documentation (legacy)](https://github.com/GameGenesis/First-Principles/wiki/First-Principles-Official-Documentation)
- [Repository](https://github.com/GameGenesis/First-Principles)

---

*Documentation version aligned with Unity **6000.4.0f1** and the graph + platformer flow described in this site.*
