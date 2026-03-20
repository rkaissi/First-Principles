---
layout: page
title: Definite vs indefinite integrals — game glossary
permalink: /definite-indefinite-integrals/
---

# Definite vs indefinite integrals — game glossary

Short guide matching the **in-app** block **“Integrals: definite vs indefinite — score vs loadout”** in **Level select → Math tips & snippets** / **Game → Math concepts**.

---

## Side-by-side

| | **Indefinite** \(\int f(x)\,dx\) | **Definite** \(\int_a^b f(x)\,dx\) |
|--|-----------------------------------|-------------------------------------|
| **What you get** | A **family** of functions \(F(x)+C\) with \(F'=f\) | A **number** (signed scalar) |
| **“+ C”** | **Yes** — antiderivatives differ by a constant | **No** — limits fix the value |
| **Bounds** | None written on the integral sign | **\(a\)** and **\(b\)** on the symbol |
| **Game metaphor** | **Loadout class** — many builds; \(C\) is like a harmless vertical shift in antiderivative space | **Stage score** — one tally for the interval \([a,b]\) |
| **Typical use** | General solution of DEs, symbolic work, pattern recognition | Area, accumulation, probability mass, work, totals |

---

## Fundamental Theorem of Calculus (bridge)

If \(F\) is an antiderivative of \(f\) on \([a,b]\) (standard hypotheses):

\[
\int_a^b f(x)\,dx = F(b) - F(a).
\]

**Intuition:** compute the **indefinite** recipe \(F\) once, then the **definite** answer is **endpoint difference** — “final gauge minus starting gauge.” Often written \(\bigl[F(x)\bigr]_a^b\).

---

## Signed area

For a **definite** integral, area **above** the \(x\)-axis counts **positive**, below counts **negative** — net signed area. Disjoint or ugly regions: split at zeros or breakpoints, integrate piecewise, then add.

---

## Riemann sums → definite integral

Approximate \(\displaystyle \int_a^b f(x)\,dx\) by:

1. Partition \([a,b]\) into strips of width \(\Delta x\).
2. Pick a **sample** per strip (left, right, **midpoint**).
3. Sum \(f(x^\ast)\,\Delta x\).

Refining the partition (more, thinner strips) converges to the definite integral for nice functions — this is the mood of the game’s **Riemann** stages.

---

## In First Principles

- **Area under the curve** / **Riemann** levels emphasize the **definite** idea and rectangle refinements.
- The **graphing** layer plots functions \(y=f(x)\); shading/stairs visualize accumulation.
- See also **[Math concepts — §8]({% link math-concepts.md %}#8-integrals-and-riemann-sums)** and **[Differentiation rules]({% link derivative-rules.md %})** (inverse mindset: derivative vs antiderivative).

---

## See also

- [Math concepts & snippets]({% link math-concepts.md %})  
- [AP Calculus BC — prep]({% link ap-calculus-bc.md %}) — FTC, accumulation functions, improper integrals in syllabus context  

---

*Informal notes for learners — not a substitute for your course text or examiner specifications.*
