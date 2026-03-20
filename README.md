<img width="100" alt="Screenshot 2022-08-30 at 1 53 22 PM" src="https://user-images.githubusercontent.com/35755386/187352165-4def6be5-9d9a-423d-9863-ea2c45b4e02d.png">

# First Principles: An Interactive Module

A **graphing calculator** and **derivative-driven platformer** built in **Unity 6**. The **name** nods to **Elon Musk**’s comments on a **first-principles approach** to **business** and to **problem-solving in life and work**—paired here with **calculus** on the graph as the literal teaching layer ([`docs/first-principles-business.md`](docs/first-principles-business.md)).  
Credits: **GAME GENESIS** ([itch.io](https://game-genesis.itch.io) · [Rayan Kaissi](https://github.com/rkaissi/)) × **ORCH AEROSPACE** ([orchaerospace.com](https://orchaerospace.com) · [John Wonmo Seong](https://github.com/wonmor)). **Proprietary** — see [`LICENSE`](LICENSE) and [`CREDITS.md`](CREDITS.md) for terms, App Store–style attribution, and third-party notices. **Orch Avionic 1 EFB** (promo): [`docs/orch-avionic-efb.md`](docs/orch-avionic-efb.md).

---

## Beta build 1.0

The project pairs a **Cartesian graph UI** (functions + numeric derivatives) with **Limbo-style** gameplay: **platforms** follow the curve and **gaps / hazards** follow derivative rules, with **staged progression** (HUD), **level select**, primer plus **Taylor / Maclaurin / series / multivar** levels, **area-under-the-curve / Riemann sum** stages (left, right, midpoint rectangles + optional stair platforms), an **AP Calculus BC** extension (polar rose/cardioid, logistic, inverse trig, transcendental-tooling levels), **AP Physics C** hooks (decay, projectile, angular momentum / rotation stories), and **aerospace / aerodynamics** stages (lift vs α & stall, drag polar, atmosphere ρ(h), phugoid mood, Newtonian sin²α, Strouhal shedding, re-entry decay envelope), with typography matched to the main equation label. **Mobile / touch:** portrait-oriented scaler, **safe-area** UI, **scrollable** level list, and **on-screen move/jump** controls on handheld / touch builds.

**Player build version** (Unity **Project Settings → Player**): **1.0** (`bundleVersion`).

**Localization:** UI strings and level titles in **English, Hindi, Urdu, Arabic, French, Chinese (Simplified), Korean, Japanese, German, Spanish, Italian, Czech, Polish, and Russian** (`Assets/Resources/Localization/*.txt`). Menu & level select include a language chip; choice is saved in `PlayerPrefs`. Long level stories default to English unless you add optional `story.N` keys per locale. **TMP multilingual glyphs:** `TmpGlobalFallbackBootstrap` loads **Noto Sans**, **Noto Sans Arabic**, **Noto Sans Devanagari** (Hindi), **Noto Sans Bengali** (Bangla), **Noto Nastaliq Urdu**, and **Noto Sans SC / KR / JP** from `Assets/Resources/Fonts/` (OFL; dynamic SDF atlases at runtime). See `Assets/Resources/Fonts/LICENSE-Noto-OFL.txt`.

**Input:** The **[Input System](https://docs.unity3d.com/Packages/com.unity.inputsystem@latest)** package is enabled (`com.unity.inputsystem` in `Packages/manifest.json`; **Player → Active Input Handling → Input System Package**). At runtime, `UiInputModuleBootstrap` replaces legacy `StandaloneInputModule` with `InputSystemUIInputModule` on each `EventSystem` so menus work without `UnityEngine.Input`. See [`docs/troubleshooting.md`](docs/troubleshooting.md) (Input System + uGUI section).

**Links:** [Demo (YouTube)](https://www.youtube.com/watch?v=yo540yl4Xhs) · [Legacy wiki](https://github.com/GameGenesis/First-Principles/wiki/First-Principles-Official-Documentation) · [Promo video](https://www.youtube.com/watch?v=k0soEFAK-CQ)

---

## Documentation (GitHub Pages)

Comprehensive docs for setup, gameplay, architecture, and troubleshooting live in **`/docs`** (Jekyll, GitHub Pages–compatible).

| | |
|--|--|
| **Browse in repo** | [`docs/index.md`](docs/index.md) |
| **Published site** | After you enable **Settings → Pages → `/docs`**: `https://<user>.github.io/First-Principles/` (set `url` / `baseurl` in [`docs/_config.yml`](docs/_config.yml)) |

Topics covered: **Unity 6000.4.0f1** project path (`First Principles/`), **Menu → Level select → Game**, **Math tips & snippets** overlay on level select, controls (**arrows / WASD**, **Space** jump), **stages HUD**, package restore script, **TextMesh Pro** + **Quicksand** UI font + Noto/Nanum fallbacks ([`docs/setup.md`](docs/setup.md)), **LaTeX-style math** (MathJax on GitHub Pages docs; `TmpLatex` → TMP in-game), and Pages **404** fixes. Runtime UI uses procedural **rounded sprites** (`RuntimeUiPolish`); optional Asset Store art: [`docs/optional-unity-assets.md`](docs/optional-unity-assets.md). Learning writeups: [`docs/math-concepts.md`](docs/math-concepts.md), [`docs/derivative-rules.md`](docs/derivative-rules.md) (power / product / quotient / chain), [`docs/definite-indefinite-integrals.md`](docs/definite-indefinite-integrals.md) (definite vs indefinite + FTC), [`docs/first-principles-business.md`](docs/first-principles-business.md) (first principles thinking ↔ game metaphors), [`docs/competition-math.md`](docs/competition-math.md), [`docs/amc-10-12.md`](docs/amc-10-12.md), [`docs/engineering-math.md`](docs/engineering-math.md), and **separate** unofficial prep: [`docs/tmua-calculus.md`](docs/tmua-calculus.md), [`docs/mat-calculus.md`](docs/mat-calculus.md), [`docs/ap-calculus-bc.md`](docs/ap-calculus-bc.md), [`docs/ap-physics-c.md`](docs/ap-physics-c.md).

---

## Repository layout

| Path | Purpose |
|------|---------|
| **`First Principles/`** | **Unity project root** — open this folder in Unity Hub (`Assets`, `ProjectSettings`, `Packages`). |
| **`docs/`** | GitHub Pages documentation (Jekyll). |
| **`clean-unity-library.sh`** | Deletes `First Principles/Library` and stray `Packages/com.unity.*` embeds; use when packages are corrupt. |
| **`.github/workflows/`** | CI: Jekyll **docs** build; **Unity** Edit Mode tests via [GameCI](https://game.ci/) (needs `UNITY_LICENSE` secret — see [`docs/ci.md`](docs/ci.md)). |
| **`README.md`** | This file. |
| **`CREDITS.md`** | Attribution & third-party summary (App Store / support pages). |

---

## Quick start

1. Install **Unity 6000.4.0f1** (see `First Principles/ProjectSettings/ProjectVersion.txt`).
2. **Add** the folder **`…/First-Principles/First Principles`** in Unity Hub.
3. Open **Menu** scene, press **Play**.
4. **Play** flow: **Menu** → **Level select** → **Game** (level index is carried by `LevelSelection`).

---

## Gameplay highlights

- **Level select** — Runtime UI from `LevelSelectController` + `GameLevelCatalog` names.
- **Stages** — Derivative “pop” thresholds; **Stage *k* / *n*** HUD (same font stack as **Equation**).
- **controls** — ←/→ or A/D, **Space** / W / ↑ to jump; legacy Trans/Scale graph buttons are **off** in favour of level definitions.
- **Spawn** — Start column chooses the **lowest** safe platform among opening columns so you don’t begin at the top of the curve.

---

## Planned editions

1. **Pre-Calculus** — The nature of functions  
2. **Fundamentals of Calculus** — Limits and differentiation  
3. **Integral calculus**  
4. **Infinite series**

---

## Screenshots

<img width="1728" alt="Screen Shot 2026-03-15 at 2 57 39 PM" src="https://user-images.githubusercontent.com/35755386/158451505-71e056ee-cca4-42ee-a621-38c092c806f2.png">

<img width="1728" alt="Screen Shot 2026-03-15 at 2 57 31 PM" src="https://user-images.githubusercontent.com/35755386/158451522-61e0c14c-57c3-4819-b023-b7d7c13075aa.png">

---

## Dependencies

- [**Unity**](https://unity.com) **6** (6000.4.0f1)
- **Unity UI (uGUI)** + **TextMesh Pro** (via editor / packages; import TMP Essentials if prompted)

---

## Troubleshooting (short)

| Issue | Action |
|--------|--------|
| **Wrong project / “Untitled”** | Open **`First Principles/`** in Hub; load `Assets/Scenes/…`. |
| **UGUI / package chaos** | Quit Unity → `./clean-unity-library.sh` → reopen. Details: [`docs/troubleshooting.md`](docs/troubleshooting.md) and `First Principles/Docs/Fix-Unity-UGUI-PackageErrors.md`. |
| **GitHub Pages 404** | Set **`url`** and **`baseurl`** in `docs/_config.yml` to match your user + repo name. |

---

**© 2022-2026** · **Proprietary** · **First Principles**
