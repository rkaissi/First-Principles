---
layout: page
title: Troubleshooting
permalink: /troubleshooting/
---

# Troubleshooting

## Unity opens the wrong folder / “Untitled” scene

- Add **`…/First-Principles/First Principles`** (with **`Assets`**) in Unity Hub — not only the git parent folder.
- If the scene tab says **Untitled**, use **File → Open Scene** and open `Assets/Scenes/Game.unity` (or save your scene under `Assets/Scenes/`).

## Package / UGUI / test framework compile errors

**Symptoms:** `UIToolkitInteroperabilityBridge` missing, `UnityEngine.TestTools.Logging` missing, orphan `.meta` under `Packages/com.unity.*`, or errors pointing at **`Library/PackageCache`** and **immutable** packages.

**Cause:** Corrupt **`Library`** and/or **local embedded** folders under `First Principles/Packages/com.unity.ugui` or `com.unity.test-framework` that override registry packages.

**Fix:**

1. Quit Unity.
2. From repo root: `./clean-unity-library.sh`
3. Reopen the project.

Do **not** hand-edit `Library/PackageCache`. The repo’s `Packages/` in git should normally only contain `manifest.json` and `packages-lock.json`.

More detail in the Unity project doc:  
`First Principles/Docs/Fix-Unity-UGUI-PackageErrors.md` (in the repository).

## TextMesh Pro — “No Font Asset” / import does nothing

- Ensure **TMP Essentials** imported (try **GameObject → UI → Text - TextMeshPro** to trigger the wizard).
- **`Window → TextMeshPro → Import TMP Essential Resources`**
- Gameplay HUD copies typography from the **Equation** label; if that object has no font, assign font assets under **TextMesh Pro** resources in the inspector.

## GitHub Pages — site 404 or wrong paths

1. **Repository** → **Settings** → **Pages** → Build from **`main`** / **`master`**, folder **`/docs`**.
2. Edit **`docs/_config.yml`**:
   - `url`: your GitHub Pages host (e.g. `https://<user>.github.io`).
   - `baseurl`: `/<repository-name>` (leading slash, no trailing slash), e.g. `/First-Principles`.

After changing `_config.yml`, wait for the **Pages build** to finish (Actions tab).

## `.gitignore` and `Library/`

The root `.gitignore` ignores **`**/Library/`**, **`**/UserSettings/`**, etc., so the Unity project under **`First Principles/`** is covered. Do not commit `Library/` — teammates regenerate it locally.

## Input System (new)

The project uses the **[Input System](https://docs.unity3d.com/Packages/com.unity.inputsystem@latest)** package with **Active Input Handling** set to **Input System Package** (`Project Settings → Player → Other Settings`). Keyboard, gamepad, touch pinch, and UI go through the new stack.

- **Gameplay** (`PlayerControllerUI2D`): `Keyboard` + `Gamepad` via `UnityEngine.InputSystem`.
- **Faxas pinch zoom** (`GraphPinchZoom`): **Enhanced Touch** (`Touch.activeTouches`).
- **uGUI**: `EventSystemInputModuleBootstrap` replaces any scene **`StandaloneInputModule`** with **`InputSystemUIInputModule`** on load (defaults are assigned automatically).

If **Package Manager** fails to resolve `com.unity.inputsystem`, open **Window → Package Manager**, select **Input System**, and use the **version Unity recommends** for your editor; then align `Packages/manifest.json` with that version.

If you see **`TypeLoadException` … `InputActionAsset` from assembly `Unity.InputSystem`**, the Input System DLLs in `Library/` are often out of sync: close Unity, delete the project’s **`Library`** folder (and let the editor reimport), or **Reimport** the Input System package. The repo pins a version verified for **Unity 6000.4** (`manifest.json`); adjust if your patch release differs.
