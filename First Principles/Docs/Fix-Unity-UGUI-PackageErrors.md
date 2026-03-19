# Fix: UGUI / Test Framework compile errors & orphan package `.meta` files

## Symptoms

Any of these usually mean **a bad local package tree**, not a bug in your game scripts:

- `CS0246: UIToolkitInteroperabilityBridge could not be found` (from `EventSystem.cs` in **com.unity.ugui**)
- `CS0234: UnityEngine.TestTools.Logging does not exist` or `NUnitExtensions` missing (from **com.unity.test-framework**)
- Console spam: **meta file exists but asset can't be found** under `Packages/com.unity.ugui/` or `Packages/com.unity.test-framework/`
- **Immutable packages unexpectedly altered** under `Packages/com.unity.*`

In this repo, **`First Principles/Packages/` should only contain** `manifest.json` and `packages-lock.json`.  
If you see **`Packages/com.unity.ugui/`** or **`Packages/com.unity.test-framework/`** as real folders with (partial) files, they are almost certainly **corrupt duplicates** that override the registry versions and break compilation.

## Fix (recommended)

1. **Quit Unity** completely.
2. From the **repository root**, run:
   ```bash
   ./clean-unity-library.sh
   ```
   This removes:
   - `First Principles/Library` (regenerates `PackageCache`)
   - `First Principles/Packages/com.unity.ugui` and `.../com.unity.test-framework` **if present**
3. Reopen the project in Unity and wait for **package restore** and reimport.

`Library` is in `.gitignore` and is safe to delete.

## Manual fix (same idea)

- Delete **`First Principles/Library`**
- If they exist, delete entire folders:
  - **`First Principles/Packages/com.unity.ugui`**
  - **`First Principles/Packages/com.unity.test-framework`**
- Reopen Unity

## Do not

- Hand-edit or “fix” files under **`Library/PackageCache/`** — they are **immutable**; deleting `Library` restores them.
- Edit **`Packages/com.unity.*`** when those are meant to come from the Package Manager — use the cleanup above instead.

## Version note

Match **Unity** to `ProjectSettings/ProjectVersion.txt`.  
`com.unity.ugui` **2.x** in `Packages/manifest.json` is intended for **Unity 6** in this project.
