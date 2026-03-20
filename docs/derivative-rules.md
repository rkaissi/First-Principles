---
layout: page
title: Differentiation rules — game-style playbook
permalink: /derivative-rules/
---

# Differentiation rules — game-style playbook

Plain-language map of the **algebra of derivatives**: power, sum, product, quotient, and chain. It mirrors the **in-game** block in **Level select → Math tips & snippets** / **Game → Math concepts** (“**Differentiation rules — your skill tree**”).

---

## How this connects to First Principles

| Rule | On the graph | Game metaphor |
|------|----------------|---------------|
| **Power** | \(x^n \Rightarrow n x^{n-1}\) | “Stage exponent” controls how fast tilt ramps. |
| **Constant ×** | \((kf)' = kf'\) | One buff scales every local slope. |
| **Sum / diff** | \((f\pm g)' = f' \pm g'\) | Stacked modifiers add cleanly. |
| **Product** | \((uv)' = u'v + uv'\) | Two meters both contribute. |
| **Quotient** | \(\bigl(\frac{u}{v}\bigr)' = \frac{u'v-uv'}{v^2}\) | Top vs bottom track; watch \(v=0\). |
| **Chain** | \((f\circ g)' = (f'\circ g)\,g'\) | Nested stage: outer × inner slope. |

The **derivative curve** in-app is still **numeric** for arbitrary \(f\); these rules explain **closed forms** you learn in class and **why** many stages look related before sampling.

---

## Power rule

For \(y = x^n\) (where \(x^n\) is defined on the interval you care about),

\[
\frac{d}{dx} x^n = n\, x^{\,n-1}.
\]

**Intuition:** the exponent becomes a **multiplier** out front; the variable’s power **drops by one** — the slope field “inherits” the degree structure.

---

## Constant multiple & sum / difference

\[
\frac{d}{dx}\bigl[k\,f(x)\bigr] = k\,f'(x), \qquad
\frac{d}{dx}\bigl[f(x) \pm g(x)\bigr] = f'(x) \pm g'(x).
\]

**Intuition:** scaling or adding functions **scales or adds** slopes. Linearity is why superposition shows up everywhere in physics and engineering.

---

## Product rule

\[
\frac{d}{dx}\bigl[u(x)\,v(x)\bigr] = u'(x)\,v(x) + u(x)\,v'(x).
\]

**Mnemonic:** “first times derivative of second **plus** second times derivative of first.” Neither factor can be ignored: both cross-terms matter.

---

## Quotient rule

Where \(v(x) \neq 0\),

\[
\frac{d}{dx}\frac{u(x)}{v(x)} = \frac{u'(x)\,v(x) - u(x)\,v'(x)}{[v(x)]^2}.
\]

**Mnemonic:** “**low** \(\times\) **d-high** minus **high** \(\times\) **d-low**, over **low squared**” (with \(u\) = high, \(v\) = low). **Domain:** wherever \(v = 0\) or you cross a **vertical asymptote** / cutout, stop — the formula doesn’t apply there.

---

## Chain rule

If \(y = f(g(x))\), then

\[
\frac{dy}{dx} = f'(g(x))\cdot g'(x).
\]

**Intuition:** a small nudge in \(x\) moves \(g\) by about \(g'(x)\,\Delta x\); then \(f\) responds to that inner change at rate \(f'(g(x))\). Total sensitivity = **outer** × **inner**. Matches the game’s inner variable \(u = k(x-D)\): change \(x\) → change \(u\) → change \(f(u)\).

---

## What we didn’t squeeze into the HUD

- **Exponential / log** rules (\(e^{kx}\), \(\ln x\)) appear on named stages — see [Math concepts]({% link math-concepts.md %}) and topic pages.
- **Implicit differentiation** (e.g. circle stage) handles relations that aren’t solved as \(y=f(x)\) everywhere.
- **Linear approx / differentials** tie slopes to “nearby value” predictions — useful for bounds and TMUA-style elimination.

---

## See also

- [Math concepts & snippets]({% link math-concepts.md %}) — full glossary and exam crosswalk  
- [AP Calculus BC — prep]({% link ap-calculus-bc.md %}) — where these rules sit in the BC toolbox  
- [TMUA — calculus]({% link tmua-calculus.md %}) — MCQ fluency with product / quotient / chain  

---

*Unofficial study notes — not affiliated with College Board, Cambridge Assessment, or Oxford.*
