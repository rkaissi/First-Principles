---
layout: page
title: Competition math — in-game stage & study lens
permalink: /competition-math/
---

# Competition math — in-game stage & study lens

**First Principles** includes a dedicated **Competition math** stage (near the end of level select) that uses **`y = a\ln(k(x-d)) + c`** on a shifted domain — the same **natural logarithm** family you use in **AP Calculus**, **TMUA/MAT**, and contest prep, but framed for **inequalities, bounding, and “is the function bending the right way?”**

This page is **unofficial** study context only — not affiliated with the **MAA** (AMC/AIME), **UKMT**, **Oxford MAT**, or any olympiad organiser. For real contests, use **official** syllabi, past papers, and coaches.

---

## What “competition math” usually stresses

| Theme | Calculus link | Game hook |
|--------|----------------|-----------|
| **Bounding & estimates** | Compare to tangents/secants; **mean value** mood | \(\ln\) is **concave**: chord/tangent tricks are standard toolkit |
| **AM–GM (arithmetic–geometric mean)** | Often proved via \(\ln\) or convexity | Same “turn products into sums” spirit as \(\sum \ln\) |
| **Smoothness & case splits** | Domain of \(\ln\), removable vs essential mood | Stage stays on a safe branch of \(\ln\) so the graph is clean |
| **Clever substitution** | Chain rule / change of variables | Parallel to “replace \(u\) to simplify” in integrals |

---

## Why \(\ln\) for this stage

- **Concavity**: \(f''(x)<0\) on \((0,\infty)\) for \(f(x)=\ln x\) — **Jensen’s inequality** for concave functions (and the **tangent line upper bound**) shows up constantly in olympiad-style inequalities.
- **Derivative**: \(\frac{d}{dx}\ln x = \frac{1}{x}\) — reciprocals and **harmonic-flavored** estimates appear in many discrete bounds.
- **Domain hygiene**: \(\ln\) forces you to think **where** an expression is defined — a contest habit of checking **positivity** before taking logs.

The **derivative-driven platform rule** in the game is unchanged: where the sampled derivative is large enough, you tend to get **platforms**; where it is not, you may see **gaps / hazards** — you are still “walking on slopes.”

---

## How this fits next to other docs

| Resource | Role |
|----------|------|
| **[Math concepts]({% link math-concepts.md %})** | Broad in-game curriculum index |
| **[AP Calculus BC — prep]({% link ap-calculus-bc.md %})** | Syllabus-shaped BC map (includes \(\ln\), concavity, series) |
| **[TMUA — calculus]({% link tmua-calculus.md %})** / **[MAT — calculus]({% link mat-calculus.md %})** | UK admissions multiple-choice / reasoning pace |
| **This page** | Contest **style**: bounds, concavity, \(\ln\) as a hub |

---

## In-game title

Look in level select for **“Competition math: ln, concavity & bound tricks”** (before the **Mandelbrot** boss stage).

---

*Unofficial · **First Principles** (proprietary) — for learning mood only.*
