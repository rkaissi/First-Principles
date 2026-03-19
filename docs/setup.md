---
layout: page
title: Setup
permalink: /setup/
---

# Setup

## Requirements

| Requirement | Notes |
|-------------|--------|
| **Unity Editor** | **6000.4.0f1** (Unity 6) — see `First Principles/ProjectSettings/ProjectVersion.txt` |
| **Input** | **[Input System](https://docs.unity3d.com/Packages/com.unity.inputsystem@latest)** package (`com.unity.inputsystem` in `Packages/manifest.json`); **Player → Active Input Handling** is **Input System Package**. `Assets/Scripts/Input/UiInputModuleBootstrap.cs` swaps **`StandaloneInputModule`** → **`InputSystemUIInputModule`** after each scene load so uGUI matches that setting (gameplay already uses **`UnityEngine.InputSystem`**). |
| **Disk** | Large `Library/` folder; safe to delete and regenerate |
| **OS** | Windows / macOS / Linux (editor-supported) |

Optional: **Git LFS** if you add large assets later (not required for the scripts-focused workflow).

## Clone the repository

```bash
git clone https://github.com/GameGenesis/First-Principles.git
cd First-Principles
```

If you fork the repo, use your fork URL instead.

## Open the correct Unity project

The Unity project lives in a subfolder **with a space** in the name:

```
First-Principles/First Principles/
```

In **Unity Hub** → **Add** → choose:

`.../First-Principles/First Principles`

You should see `Assets`, `Packages`, and `ProjectSettings` at the root of the added project.

## Typography (Outfit)

UI copy uses the **Outfit** font (Google Fonts, SIL OFL 1.1 — `Assets/Fonts/Outfit-OFL.txt`) through **TextMesh Pro**.

1. Open the project in the Unity **Editor** (other Unity instances must **not** have the same project open).
2. Menu: **First Principles → Fonts → Apply Outfit for all TextMesh Pro**.

This generates `Assets/Fonts/Outfit SDF.asset` if needed, sets it as **TMP Settings → Default Font Asset**, adds **Liberation Sans** as a fallback for rare glyphs, and assigns Outfit on all **TextMeshPro** / **TextMeshProUGUI** under `Assets/Scenes` and `Assets` prefabs.

CLI (optional, project closed in the Editor):

```bash
Unity -batchmode -quit -projectPath "/path/to/First Principles" -executeMethod OutfitFontProjectSetup.GenerateAndApplyAllBatch
```

**Common mistake:** adding only `First-Principles` (parent) will not load the Unity project correctly.

## First open / packages

1. Open the project in Unity and wait for **import** and **package restore**.
2. If you use **TextMesh Pro** for the first time, import **TMP Essentials** when prompted (*GameObject → UI → Text - TextMeshPro* often triggers the importer).

## Clean restore (corrupted Library / packages)

If you see compile errors about immutable packages, missing UGUI types, or broken test framework assemblies:

1. Quit Unity.
2. From the repo root run:

   ```bash
   ./clean-unity-library.sh
   ```

3. Reopen the project.

See [Troubleshooting]({% link troubleshooting.md %}) for details.

## Run the game

1. Open scene **`Assets/Scenes/Menu.unity`** (or build settings entry scene).
2. **Play** from the Editor.

Flow: **Menu** → **Level select** → **Game** (chosen level index is passed via `LevelSelection`).

## Optional: local documentation site

From the `docs/` folder (Ruby + Bundler required):

```bash
cd docs
bundle install
bundle exec jekyll serve
```

Browse to `http://localhost:4000/First-Principles/` (include `baseurl` path if configured).

## LaTeX math on the doc site

Pages load **MathJax 3** via [`_includes/custom-head.html`](_includes/custom-head.html). In Markdown, use standard delimiters, for example:

- Inline: `\( f'(x) \)` → \( f'(x) \)
- Display: `\[ \int_a^b f(x)\,dx \]` or `$$ \sum_{k=0}^{n} a_k $$`

The **Unity game** does not run MathJax; it uses `TmpLatex` to turn the same `\(\cdots\)` delimiters into **TextMesh Pro** rich text (subset: fractions, sub/sup, Greek, common operators). Keep in-game strings in that format when you want pretty equations.
