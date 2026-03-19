# Documentation (GitHub Pages)

This folder is the **Jekyll** source for **GitHub Pages**.

**Developed by [John Wonmo Seong](https://github.com/wonmor) (ORCH AEROSPACE) and [Rayan Kaissi](https://github.com/rkaissi/) (GAME GENESIS).** See repo root [`CREDITS.md`](../CREDITS.md) and [`LICENSE`](../LICENSE).

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
| `setup.md` | Unity setup & clean restore |
| `gameplay.md` | Controls, stages, flow |
| `math-concepts.md` | Game math notes + index to exam prep |
| `tmua-calculus.md` / `mat-calculus.md` | Unofficial UK admissions calculus prep |
| `ap-calculus-bc.md` / `ap-physics-c.md` | Unofficial US AP prep maps |
| `engineering-math.md` | Applied / engineering angle |
| `architecture.md` | Scenes & scripts |
| `troubleshooting.md` | Packages, TMP, Pages |
