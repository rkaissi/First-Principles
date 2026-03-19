# Documentation (GitHub Pages)

This folder is the **Jekyll** source for **GitHub Pages**.

**Developed by [John Wonmo Seong](https://github.com/wonmor) ([ORCH AEROSPACE — orchaerospace.com](https://orchaerospace.com)) and [Rayan Kaissi](https://github.com/rkaissi/) ([GAME GENESIS — itch.io](https://game-genesis.itch.io)).** See [`orch-avionic-efb.md`](orch-avionic-efb.md) for the Orch Avionic 1 EFB promo page. Repo root [`CREDITS.md`](../CREDITS.md) and [`LICENSE`](../LICENSE).

## Enable Pages

1. GitHub → **Settings** → **Pages**
2. **Source:** Deploy from branch  
3. **Branch:** `main` (or default), folder **`/docs`**
4. Save

Update **`_config.yml`** with your real **`url`** and **`baseurl`** (repo name).

## Local preview

```bash
cd docs
bundle install
bundle exec jekyll serve
```

Open `http://127.0.0.1:4000/First-Principles/` (adjust for your `baseurl`).

**Math:** Markdown pages may use LaTeX in `\(...\)` / `\[...\]`; see [`setup.md#latex-math-on-the-doc-site`](setup.md#latex-math-on-the-doc-site).

## Contents

| File | Purpose |
|------|---------|
| `index.md` | Documentation home |
| `first-principles-business.md` | First principles (Musk-popularized builder lens) ↔ game metaphors; not business advice |
| `orch-avionic-efb.md` | [ORCH Aerospace](https://orchaerospace.com) — Orch Avionic 1 EFB (promo / disclosures) |
| `setup.md` | Unity setup & clean restore |
| `gameplay.md` | Controls, stages, flow |
| `math-concepts.md` | Game math notes + index to exam prep |
| `competition-math.md` | Contest-style lens (AMC/AIME mood, \(\ln\) / concavity); in-game stage |
| `amc-10-12.md` | Unofficial AMC 10 & 12 topic map + tie-in to graph practice |
| `tmua-calculus.md` / `mat-calculus.md` | Unofficial UK admissions calculus prep |
| `ap-calculus-bc.md` / `ap-physics-c.md` | Unofficial US AP prep maps |
| `engineering-math.md` | Applied / engineering angle |
| `architecture.md` | Scenes & scripts |
| `troubleshooting.md` | Packages, TMP, Pages |
