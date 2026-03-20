---
layout: page
title: Engineering math
permalink: /engineering-math/
---

# Engineering math (plain-language map)

This page is the **engineering** corner of the project: **why** certain functions show up when you **model** the world, and how that connects to the **general math snippets** in [**Math concepts**]({% link math-concepts.md %}).

---

## 1. Modeling mindset

Engineers rarely stop at “find the derivative.” They:

1. **Pick variables** that describe the system (time, position, voltage, temperature…).
2. **Write equations** that encode laws (Newton, Kirchhoff, conservation, empirical fits).
3. **Linearize or approximate** near an operating point when things get messy.
4. **Check** against measurement—models are **tools**, not perfect truth.

The game’s **graphs** are **toy models**: same shapes, fewer knobs.

---

## 2. Oscillation, decay, and damping

**Vibration + friction** (or resistance) often produces **oscillation inside a shrinking envelope**:

- Think “**ring-down**” after you pluck a string or step on a springy platform.
- Cartoon form: something like \(e^{-\alpha t}\sin(\omega t)\) — **exponential decay** × **sine**.

**Where you see it:** mechanical vibrations, RLC circuits, mass–spring–damper, control “transient” response.

**Game link:** *Engineering: damped oscillation*.

---

## 3. Catenary and hyperbolic cosine

A **hanging cable** under its own weight (idealized) traces a **catenary**, often modeled with **cosh** — the **hyperbolic cosine**. It is **not** the same curve as a **parabola** (even if both look arch-shaped in photos).

**Where you see it:** **Suspension bridges**, cables, some structural shapes; **hyperbolic functions** also appear in advanced PDEs and physics.

**Game link:** *Engineering: catenary (cosh)*.

---

## 4. AC, rectification, and \(|sin|\)

**Sine waves** describe **alternating** current/voltage. A **full-wave rectifier** flips the **negative** half-cycles **up**, turning the wave into humps that stay **nonnegative**—a **\(|\sin|\)**-style shape in a first cartoon model.

**Fine print:** real converters have diodes, harmonics, filters, and efficiency math—but the **graph idea** is the right starting picture.

**Game link:** *Engineering: rectified AC (|sin|)*.

---

## 5. Complex numbers and phasors (stub for later you)

For **steady AC** at a **single frequency**, engineers often replace \(\sin/\cos\) with **complex exponentials**: the “spinning arrow” picture (**phasor**). It turns differential equations into **algebra** problems in one frequency.

You don’t need complex numbers to enjoy this repo—but if you keep studying **circuits** or **vibrations**, they become **the** shortcut.

---

## 6. Aerospace engineering & aerodynamics (game map)

**First Principles** adds **Aerospace:** stages — calculus-shaped **teaching curves**, not a wind-tunnel or Navier–Stokes solver.

| Idea | Typical model / graph | Game stage |
|------|----------------------|------------|
| **Lift vs α** | \(C_L\) ~ linear before **stall**, then loss of lift | *Aerospace: lift C_L(α) linear + stall* |
| **Drag polar** | \(C_D = C_{D0} + K C_L^2\) | *Aerospace: drag polar (parasitic, induced, total — three traces)* |

**Parasitic vs induced vs total (parabolic polar cartoon):**

- **Parasitic (profile / zero-lift) drag** — lumped in **\(C_{D0}\)** in this model: skin friction, pressure form drag, interference — the part that does **not** grow with \(C_L\) in \(C_D = C_{D0} + K C_L^2\).
- **Induced drag** — trailing-vortex / lift-carrying cost, **\(\propto C_L^2\)** (here **\(K C_L^2\)**); high \(C_L\) (slow flight, tight turns) pays extra.
- **Overall drag curve** — plot **\(C_D\)** vs **\(C_L\)** (the **drag polar**): an upward-opening parabola; its **minimum** locates a best-compromise **\(C_L\)** for min drag at a given configuration (before adding propulsion and constraint soup).

In-game, **every Aerospace stage** prepends a short **drag polar refresher** on the story banner above that stage’s specific topic.
| **Atmosphere** | \(\rho(h) \propto e^{-h/H}\) (isothermal cartoon) | *Aerospace: isothermal atmosphere ρ(h)* |
| **Longitudinal modes** | Damped oscillation (phugoid / short-period mood) | *Aerospace: phugoid / damped pitch–heave mood* |
| **Hypersonic teaching** | \(C_p \propto \sin^{2}\alpha\) (Newtonian impact) | *Aerospace: Newtonian \(C_p \propto \sin^{2}\alpha\)* |
| **Unsteady shedding** | Strouhal \(f \sim \mathrm{St}\, U/D\) → periodic trace | *Aerospace: Strouhal / vortex shedding tone* |
| **Entry / heating mood** | Exponential decay envelope for simplified threat histories | *Aerospace: re-entry decay envelope* |

**Fine print:** Real vehicles couple **Mach**, **Re**, **elasticity**, **controls**, **propulsion**, and mission constraints. Use these graphs to **practice reading slopes, areas, and nonlinear breaks** — then carry the habits to J. D. Anderson, Etkin & Reid, or your department’s aero courses.

---

## 7. Transforms and signals (concept only)

**Fourier** ideas: build signals from **sines and cosines** of many frequencies. **Laplace** ideas: turn **time** problems into **algebra** to study **transients and stability**. Both are “engineering math heavy hitters,” both show up **after** you’re comfortable with derivatives, integrals, and exponentials.

---

## 8. Linear algebra (why it matters)

**Vectors**, **matrices**, and **linear systems** describe:

- **3D forces** and **structures** (finite-element intuition starts here).
- **Networks** and **state-space** control.
- **Least squares** fits to messy data.

The **multivariable slice** levels in First Principles are a **tiny step** toward “many variables at once.”

---

## Curriculum crosswalk

| Topic | Game / graph stages |
|--------|---------------------|
| Slopes / derivative gameplay | Primer, classics |
| Series / Maclaurin | e^x, sin(x), geometric |
| Multivar slices | Saddle, paraboloid |
| Integrals / Riemann | Area, left/right/mid |
| Damped motion, cosh, rectified AC | Engineering levels |
| Aerodynamics & flight textbook curves | **Aerospace:** … stages (see §6) |

For the **full bite-sized list** (including non-engineering topics), see [**Math concepts & snippets →**]({% link math-concepts.md %}).

---

*Part of First Principles (proprietary).*
