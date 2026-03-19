---
layout: page
title: Math concepts & snippets
permalink: /math-concepts/
---

# Math concepts & snippets

Friendly **article-style** notes that match what you see in the **First Principles** game and graph. For a dedicated **engineering** lens, see [**Engineering math**]({% link engineering-math.md %}).

---

## How this ties to the game

- **Gold / curve** → the function \(f(x)\).
- **Derivative curve** → \(f'(x)\) (slope). In many levels, **where the derivative is “high enough,”** you get **platforms**; otherwise, **gaps / hazards**.
- **Stages** → story beats and **visual “pops”** on the derivative as you move right.
- **Math tips overlay** → from **Level select**, open **“Math tips & snippets”** for the same ideas in short form (also on GitHub here).

---

## 1. Derivative = slope and rate

**In one sentence:** the derivative measures how fast \(f(x)\) changes when \(x\) nudges forward.

- On a graph, it is the **slope of the tangent line**.
- In applications, it is often a **rate**: velocity (position vs time), growth rate, heat flow per unit length, etc.

**Why the game cares:** the engine uses the **sign and size** of the derivative to shape **safe ground**—so you literally **walk on the calculus**.

---

## 2. Parabolas (power / quadratic)

A quadratic like \(y = a(x-h)^2 + k\) is a **single smooth bump or bowl**. Classic uses:

- **Projectile motion** (ideal intro model).
- **Optimization** (max/min story problems).

The **vertex** is where the slope **flattens to zero**—a key calculus idea (critical point).

---

## 3. Sine, cosine, and waves

**Sine and cosine** describe **repeating** behavior: rotations, vibrations, AC signals.

- They are the **same wave** with a **phase shift**.
- **Amplitude** = how tall; **frequency** = how packed the waves are.

**Game link:** *Waves of Sine* and *Shadows of Cosine*.

---

## 4. Absolute value and corners

\(|x|\) and \(|f(x)|\) can create **kinks** where the graph **bends sharply**. Derivatives **aren’t defined** at those ideal corners in the pure math sense—but numerically we still sample nearby slopes, which is why games and labs need **care** at nonsmooth points.

---

## 5. Taylor / Maclaurin series

**Taylor:** approximate a **nice function** near a point using a **polynomial** whose derivatives match. **Maclaurin** means “expand around **0**.”

**Intuition:** low-degree terms capture **local** behavior; adding terms usually improves the fit **near** the expansion point (where the series converges).

**Game link:** *Maclaurin: e^x*, *Maclaurin: sin(x)*.

---

## 6. Geometric series

Sums like \(1 + u + u^2 + \cdots\) appear everywhere: **probability**, **DSP**, **economics** models. When \(|u| < 1\), the infinite sum **converges** to a neat closed form—the same “tail shrinks” mood as **stability**.

**Game link:** *Series: geometric tail*.

---

## 7. Multivariable “slices”

A surface \(z = f(x,y)\) can be **cut** by fixing \(y = y_0\). You then see a **1D curve** in \(x\)—exactly how many engineers **reduce** dimension to reason about a bigger model.

**Game link:** *Saddle slice*, *Paraboloid slice*.

---

## 8. Integrals and Riemann sums

The **definite integral** \(\int_a^b f(x)\,dx\) is (for nice functions) the **signed area** under the curve from \(a\) to \(b\).

**Riemann sum recipe:**

1. Split \([a,b]\) into many small intervals of width \(\Delta x\).
2. In each interval, pick a **sample** \(x^*\) (left end, right end, or **midpoint**).
3. Add up \(f(x^*)\,\Delta x\).

More rectangles → usually **closer** to the true integral.

**Game link:** *Area under the curve*, *Riemann: left / right / midpoint*.

---

## 9. Exam preparation — separate guides

These are **four standalone** pages (plus **engineering** below). Each uses **official exam materials** for real questions; the repo only provides **unofficial topic maps** and **game cross-links**.

| Guide | Audience |
|--------|-----------|
| **[TMUA — calculus]({% link tmua-calculus.md %})** | UK **TMUA** — two-paper **multiple choice**; calculus fluency & elimination |
| **[MAT — calculus & reasoning]({% link mat-calculus.md %})** | UK **MAT** (Oxford, etc.) — careful reasoning & multi-step work |
| **[AP Calculus BC — prep]({% link ap-calculus-bc.md %})** | US **AP BC** — series, polar/parametric/vector, DEs, FRQ/MC pairing |
| **[AP Physics C — prep]({% link ap-physics-c.md %})** | US **Physics C** — calculus-first mechanics & E&M hooks |

---

## 10. Polar coordinates (game link)

Used heavily in **AP BC** and in-game **Polar:** stages. Quick recap: \(x=r\cos\theta\), \(y=r\sin\theta\); area \(\frac12\int r^2\,d\theta\); arc length \(\int \sqrt{r^2+(dr/d\theta)^2}\,d\theta\). **Full BC context:** see **[AP Calculus BC — prep]({% link ap-calculus-bc.md %})**.

---

## 11. Where engineering math fits

Engineering math is still “calculus + algebra + models,” but the **goal** is **systems you can build**: circuits, structures, controls, signals. See the companion page:

→ **[Engineering math →]({% link engineering-math.md %})**

---

## In-game vs docs

| Where | What |
|--------|------|
| **Level select → Math tips & snippets** | Short TMP article + **four separated prep blocks** (TMUA, MAT, AP BC, AP Physics C). |
| **This site (`math-concepts.md`)** | Game concepts + index to exam prep pages. |
| **`tmua-calculus.md`**, **`mat-calculus.md`**, **`ap-calculus-bc.md`**, **`ap-physics-c.md`** | Unofficial **standalone** prep notes (not past papers). |
| **`engineering-math.md`** | Damped motion, phasors, transforms, linear algebra hooks. |

---

*Aligned with the **First Principles** curriculum: primer through integrals, engineering stages, and **separate** unofficial prep tracks for **TMUA**, **MAT**, **AP Calculus BC**, and **AP Physics C**.*
