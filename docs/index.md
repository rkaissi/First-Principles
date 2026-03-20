---
layout: default
title: Home
---

# First Principles — documentation

**Release: Beta 1.0**

**Languages / اللغات / Langues:** **English** (this page) · [**العربية**]({% link ar/index.md %}) · [**Français**]({% link fr/index.md %})

**First Principles** is a **Unity 6** project (proprietary; see [`LICENSE`](../LICENSE)) that combines a **graphing calculator** (functions and numeric derivatives on a grid) with a **Limbo-inspired 2D platformer**: platforms and hazards are driven by the curve and its derivative, with **staged progression** and per-level themes (including primer, series, multivariable slices, **integral / Riemann-sum** stages, and **engineering math** graphs). The **game name** was inspired by **Elon Musk**’s comments on the **first-principles approach** to **running a business** and to **solving problems in life and work**—stripping analogy to reason from bedrock—while the **gameplay** leans into calculus “first principles” (definitions, slopes, accumulation) as a **literal** metaphor. More on that bridge: **[First principles thinking & business]({% link first-principles-business.md %})** and **Level select → Math tips & snippets** in-app. **Readable math snippets** live in **`docs/math-concepts.md`**, **[`docs/competition-math.md`]({% link competition-math.md %})**, **[`docs/amc-10-12.md`]({% link amc-10-12.md %})**, **`docs/engineering-math.md`**, and **four separated exam-prep notes** — **`docs/tmua-calculus.md`**, **`docs/mat-calculus.md`**, **`docs/ap-calculus-bc.md`**, **`docs/ap-physics-c.md`** (all unofficial; not past papers)—plus in-app **Level select → Math tips & snippets** with matching **AMC / competition / TMUA / MAT / AP BC / AP Physics C** blocks.

## Developers

**Developed by [John Wonmo Seong](https://github.com/wonmor) ([**ORCH AEROSPACE**](https://orchaerospace.com) · [GitHub](https://github.com/wonmor)) and [Rayan Kaissi](https://github.com/rkaissi/) ([**GAME GENESIS**](https://game-genesis.itch.io)).**

**Proprietary** — [`LICENSE`](../LICENSE). Store-style attribution and third-party notes: [`CREDITS.md`](../CREDITS.md).

## ORCH Aerospace — Orch Avionic 1 EFB

**[orchaerospace.com](https://orchaerospace.com)** — **Orch Avionic 1 EFB** *(NEW)*: your predictive\* copilot for **GA flying** / *copilote prédictif\* en aviation générale* — **ADS-B**, **GPS**, handheld radio, fuel calculation, **Jeppesen\*** charts in one form factor; **on-device AI** for predictive synthetic vision around busy traffic **without requiring an internet connection\*.** Full blurb and disclosures → **[Orch Avionic 1 EFB (promo)]({% link orch-avionic-efb.md %})**.

## Quick links

| Guide | Description |
|------|----------------|
| [ORCH Aerospace — Orch Avionic 1 EFB]({% link orch-avionic-efb.md %}) | [orchaerospace.com](https://orchaerospace.com) — on-device AI avionics / EFB announcement (promo) |
| [Setup]({% link setup.md %}) | Unity version, clone, open the correct project folder |
| [Gameplay]({% link gameplay.md %}) | Controls, stages, level select, how the graph affects the world |
| [First principles thinking & business]({% link first-principles-business.md %}) | Elon Musk–style “reason from bedrock”; maps game metaphors (f, f′, stages) to running a business |
| [Math concepts & snippets]({% link math-concepts.md %}) | Plain-language notes for every curriculum theme in the game |
| [Differentiation rules — playbook]({% link derivative-rules.md %}) | Power / product / quotient / chain — game-style + mnemonics (matches in-app “skill tree”) |
| [Definite vs indefinite integrals]({% link definite-indefinite-integrals.md %}) | Antiderivative + \(C\) vs bounded score; FTC bridge; matches in-app “score vs loadout” |
| [Competition math]({% link competition-math.md %}) | Contest-style bounds, concavity & \(\ln\) — maps to the in-game competition stage |
| [AMC 10 & 12 — prep]({% link amc-10-12.md %}) | Unofficial MAA contest map; algebra/geo/NT + how graph skills tie in |
| [Engineering math]({% link engineering-math.md %}) | Damped motion, catenary, AC rectification, phasors/transforms (intro) |
| [TMUA — calculus]({% link tmua-calculus.md %}) | UK TMUA — MCQ-style calculus topic map (unofficial) |
| [MAT — calculus]({% link mat-calculus.md %}) | UK MAT (Oxford-style) — reasoning & calculus lens (unofficial) |
| [AP Calculus BC — prep]({% link ap-calculus-bc.md %}) | US AP BC — syllabus topics & in-game map (unofficial) |
| [AP Physics C — prep]({% link ap-physics-c.md %}) | US AP Physics C — calculus-first mechanics/E&M hooks (unofficial) |
| [Architecture]({% link architecture.md %}) | Scenes, scripts, data flow |
| [CI — GitHub Actions]({% link ci.md %}) | Docs build + Unity (GameCI) workflow; `UNITY_LICENSE` setup |
| [Optional Unity assets]({% link optional-unity-assets.md %}) | Free UI / art packs you can import (project also uses procedural `RuntimeUiPolish`) |
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

- [ORCH Aerospace — orchaerospace.com](https://orchaerospace.com)
- [Game Genesis — itch.io](https://game-genesis.itch.io)
- [YouTube — demo](https://www.youtube.com/watch?v=yo540yl4Xhs)
- [Wiki — official documentation (legacy)](https://github.com/GameGenesis/First-Principles/wiki/First-Principles-Official-Documentation)
- [Repository](https://github.com/GameGenesis/First-Principles)

---

*Documentation version aligned with Unity **6000.4.0f1** and the graph + platformer flow described in this site.*
