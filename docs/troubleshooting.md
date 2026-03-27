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

## Input System (new) + uGUI

The project uses the **[Input System](https://docs.unity3d.com/Packages/com.unity.inputsystem@latest)** package with **Active Input Handling** set to **Input System Package** (`Project Settings → Player → Other Settings`).

Scenes still serialize Unity’s default **`StandaloneInputModule`** on each **`EventSystem`**, but that module reads **`UnityEngine.Input`**, which throws *InvalidOperationException* when the player is **Input System only**. **`UiInputModuleBootstrap`** ( `Assets/Scripts/Input/UiInputModuleBootstrap.cs` ) listens to **`SceneManager.sceneLoaded`**, removes **`StandaloneInputModule`** with **`Object.DestroyImmediate`**, and adds **`InputSystemUIInputModule`** (default UI actions are assigned automatically). That runs after the scene’s **`Awake` / `OnEnable`** and before the first **`EventSystem.Update`**, so the legacy module never ticks.

- **uGUI / menus**: **`InputSystemUIInputModule`** after bootstrap (pointer + navigation from the default UI action map).
- **Gameplay** (`PlayerControllerUI2D`): `Keyboard` + `Gamepad` via `UnityEngine.InputSystem`.
- **Graphing calculator pinch zoom** (`GraphPinchZoom`): **Enhanced Touch** (`Touch.activeTouches`).

If you delete the bootstrap or duplicate `EventSystem` setups in new scenes, ensure each active **`EventSystem`** uses **`InputSystemUIInputModule`** (not **`StandaloneInputModule`**) while **Active Input Handling** is **Input System Package**. Alternatively, set handling to **Both** and you can leave **`StandaloneInputModule`** in scenes — at the cost of enabling the old **`UnityEngine.Input`** backend.

If **Package Manager** fails to resolve `com.unity.inputsystem`, open **Window → Package Manager**, select **Input System**, and use the **version Unity recommends** for your editor; then align `Packages/manifest.json` with that version.

If you see **`TypeLoadException` … `InputActionAsset` from assembly `Unity.InputSystem`**, the Input System DLLs in `Library/` are often out of sync: close Unity, delete the project’s **`Library`** folder (and let the editor reimport), or **Reimport** the Input System package. The repo pins a version verified for **Unity 6000.4** (`manifest.json`); adjust if your patch release differs.

## macOS / Mac standalone build fails in a few seconds

**Symptoms:** Console shows **Build completed with a result of 'Failed'** and a stack trace mentioning **`EditorApplication:Internal_CallDelayFunctions`**. That line is only where Unity reported the failure — the **real errors are a few lines above** (red **`error CS…`**, **`BuildFailedException`**, **`IOException`**, missing module, etc.).

**What to do first**

1. Open **Console** (clear it), run **Build** again, and scroll to the **first red error** — copy that full message (and the one after it if it says “2 errors”).
2. In **Unity Hub** → your **6000.4.x** editor → gear → **Add modules**: enable **Mac Build Support (Mono)** and, if you use IL2CPP for Mac, **Mac Build Support (IL2CPP)**. Without the right module, the build can fail almost immediately.
3. **Player Settings → Other Settings → Company Name** must not use characters that break **bundle IDs / Info.plist** (e.g. avoid **`;`** in the name). This project uses a plain **`Orch Aerospace and Game Genesis`** style string for that field; full legal attribution stays in **`CREDITS.md`** / **`LICENSE`**.
4. **Close other Unity instances** using the same project before running a **command-line** build; only one editor may own the `Library` folder at a time.

**CLI build (optional, captures a log)** — quit the Unity Editor for this project, then (adjust the editor path if yours differs):

```bash
mkdir -p _MacBuildOut
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -nographics -quit \
  -projectPath "/path/to/First Principles" \
  -buildTarget StandaloneOSX \
  -buildPath "/path/to/_MacBuildOut/First Principles.app" \
  -logFile "/path/to/_MacBuildOut/build.log"
tail -100 "/path/to/_MacBuildOut/build.log"
```

If the log mentions **code signing** or **hardened runtime**, configure **Player Settings → macOS** (signing team, entitlements) for distribution builds; unsigned local builds usually still work for testing on your own Mac.

If the Console shows **`error CS0103: The name 'Handheld' does not exist`**, some script is calling **`UnityEngine.Handheld`** (e.g. **`Handheld.Vibrate()`**), which only exists for **iOS/Android** player builds — not for **Mac / Windows / Linux** standalone. Wrap those calls in **`#if UNITY_ANDROID || UNITY_IOS`** (or remove them on desktop). This project does that for derivative-line haptics in **`PlayerControllerUI2D`**.

## Mac / desktop build feels slow or crashes

**Cause (fixed in repo):** **`FunctionPlotter.Update`** used to run a **full graph resample** (`ComputeGraph` over `[xStart,xEnd]` with step `h`) **every frame** and toggled **`GridRendererUI`** off/on every frame to “refresh” it. On **Retina Macs** that is a huge amount of UI mesh work and can **stutter**, **heat-throttle**, or **OOM / crash**.

**Fix:** Updates now **replot only when** curve parameters change or the **Lorenz** stage is animating; during the **left→right reveal**, only **vertex / fade** refresh runs. The grid is **not** toggled every frame.

If problems remain: lower **Quality** in **Project Settings → Quality** for **Standalone**, close other heavy apps, and capture **`~/Library/Logs/Unity/Player.log`** after a crash for the native stack.
