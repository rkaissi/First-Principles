---
layout: page
title: Continuous integration (GitHub Actions)
permalink: /ci/
---

# Continuous integration (GitHub Actions)

Workflows live in **`.github/workflows/`** at the git repository root (alongside the `First Principles/` Unity project folder).

| Workflow | When it runs | Secrets |
|----------|----------------|---------|
| **Documentation** (`docs.yml`) | Push/PR touching `docs/**` | None — runs `bundle exec jekyll build`. |
| **Unity** (`unity.yml`) | Push/PR touching `First Principles/**` | **`UNITY_LICENSE`** (see below). |

## Unity license for CI

The **Unity** job uses [GameCI](https://game.ci/)’s [`unity-test-runner`](https://github.com/game-ci/unity-test-runner). You must add a repository secret:

1. Follow the official activation guide: **[GameCI — Unity Activation](https://game.ci/docs/github/activation)**.
2. In GitHub: **Settings → Secrets and variables → Actions → New repository secret**  
   - Name: `UNITY_LICENSE`  
   - Value: the license content as documented by GameCI (often the **Base64**-encoded license file for your Unity seat).

Until `UNITY_LICENSE` is set, the Unity workflow will fail on the test step — the documentation workflow will still pass.

## Local checks

- **Docs:** `cd docs && bundle install && bundle exec jekyll serve`
- **Unity:** open the **`First Principles`** folder in Unity **6000.4.0f1** (see `ProjectSettings/ProjectVersion.txt`).

## Optional: Play Mode tests

If you add Play Mode tests later, extend `.github/workflows/unity.yml` with a second job (or matrix entry) using `testMode: playmode`.

---

*Maintainers: keep `unityVersion` in `unity.yml` aligned with `First Principles/ProjectSettings/ProjectVersion.txt`.*
