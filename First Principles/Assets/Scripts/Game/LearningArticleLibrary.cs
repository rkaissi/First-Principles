/// <summary>
/// Plain-language math snippets for the in-game reader (TextMesh Pro rich text).
/// Mathematical parts use LaTeX delimiters <c>\( … \)</c> / <c>\[ … \]</c>; <see cref="TmpLatex.Process"/> converts them at load time.
/// Mirror ideas in docs (MathJax on GitHub Pages).
/// </summary>
public static class LearningArticleLibrary
{
    /// <summary>Large verbatim TMP block; embedded newlines are meaningful for paragraph breaks.</summary>
    public static string GetLevelSelectArticleRichText()
    {
        return TmpLatex.Process(@"<b><size=120%>Math concepts — in-game reader</size></b>
<size=90%>The game <b>First Principles</b> takes its name from public remarks by <b>Elon Musk</b> on a <b>first-principles approach</b> to <b>business</b> and to <b>solving problems in life and work</b>—then teaches <b>calculus-first</b> ideas on the graph as a matching metaphor.</size>
<size=90%>Use <b>Math concepts</b> while playing, or <b>Math tips & snippets</b> on Level select — same scrollable notes. Tap the dark backdrop or <b>Close</b> to dismiss.</size>

<b><size=115%>How this game teaches calculus (read the graph while you play)</size></b>
<size=92%>
• <b>Main curve</b> — the thick path on the Cartesian grid is \(y = f(x)\) for this stage. Your character tries to stand on platforms that follow that shape. The <b>Equation</b> label (when visible) names the rule being plotted.

• <b>Derivative curve \(f'(x)\)</b> — drawn as a second graph; values come from a <i>numerical</i> derivative (sampled slopes). On many levels, <b>where \(f'\) is large enough you get solid ground</b>; where it falls short you can fall through — the platformer is tied to slope logic.

• <b>Stages & “pops”</b> — the <b>Stage</b> HUD counts story beats. As you move right, the derivative visualization can <i>pop</i> or recolor at set \(x\)-values (like curtain lifts between ideas in a lesson).

• <b>Riemann sums / area</b> — some levels shade rectangles or build <i>stair</i> platforms from left-, right-, or midpoint-rule samples. That pictures \(\displaystyle \int_a^b f(x)\,dx\) as the limit of \(\sum f(x^\ast)\,\Delta x\).

• <b>Story text</b> — the banner at the top ties each level to the math (series, polar, physics metaphors, etc.). Pause and read; it fades so the run stays readable. The <b>First Principles Primer</b> also nudges <b>first-principles business</b> thinking (reason from fundamentals vs analogy).

Everything below is a <b>topic glossary</b> you can skim between deaths or after finishing a graph — plus a <b>First principles thinking (business)</b> section before exam prep.
</size>

<b><size=110%>1. Derivatives = slope & rate</size></b>
The derivative \(f'(x)\) measures how steeply \(f(x)\) rises or falls. In physics it is often a <i>rate</i>: how fast position changes (velocity), how fast temperature changes along a bar, etc. In this game, the derivative helps decide where the ground exists.

<b><size=110%>2. Parabola (power / quadratic)</size></b>
A quadratic \(y = a(x-h)^2+k\) is the shape of projectile motion in ideal textbook setups and many optimization problems (min/max). One smooth hump; the slope switches sign at the vertex.

<b><size=110%>3. Sine & cosine = waves & rotation</size></b>
Sines and cosines describe vibrations, AC signals, sound, and anything that repeats. Cosine is sine shifted: same wave, different starting phase. Complex numbers (below) make these waves easier to solve in circuits and vibrations.

<b><size=110%>4. Absolute value & kinks</size></b>
\(|x|\) bends the graph so there is a corner on the axis. The derivative jumps there in ideal math — in real programs we plot smooth samples, but the idea matters: nonsmooth points need special care in analysis and simulation.

<b><size=110%>5. Taylor & Maclaurin series</size></b>
Smooth functions can be approximated near a point by polynomials with matching derivatives. Maclaurin means “expand around \(0\).” More terms usually improve the fit nearby; the full infinite sum is the series (where it converges).

<b><size=110%>6. Geometric series</size></b>
Sums \(u^0+u^1+u^2+\cdots\) appear in probability, signal processing, and digital math. When \(|u|<1\) the tail shrinks and the infinite sum has a clean closed form; that is the same mood as stability and “things settle.”

<b><size=110%>7. Multivariable slices</size></b>
Surfaces \(z = f(x,y)\) can be cut by fixing \(y=y_0\) — you get a 1D curve in \(x\). That is how higher-dimensional calculus is often reasoned about in engineering: fix all but one variable, take partial derivatives, gradients, directional slopes.

<b><size=110%>8. Integrals & area (Riemann sums)</size></b>
The definite integral \(\displaystyle \int_a^b f(x)\,dx\) is the signed area under the curve. Riemann sums chop \([a,b]\) into thin rectangles: pick sample heights (left, right, midpoint), add \(f(x^\ast)\,\Delta x\). More rectangles → closer to the true integral.

<b><size=110%>9. Engineering math — modeling mindset</size></b>
Engineering math picks tools that match the world: linear algebra for structures and networks, complex numbers / phasors for steady AC, differential equations for motion and heat, transforms (Laplace/Fourier) for signals and control. The goal is a <i>usable model</i>, then check it against reality.

<b><size=110%>10. Damped oscillation</size></b>
Many systems lose energy while oscillating: \(e^{-\alpha t}\sin(\omega t)\) style decay is the cartoon of that idea — envelope shrinks, oscillation persists briefly. Mechanical damping, resistor–capacitor–inductor circuits, and control systems all share this language.

<b><size=110%>11. Catenary & cosh</size></b>
A hanging cable under its own weight forms a catenary; \(\cosh\) is the hyperbolic cosine that models that ideal shape (and shows up in hyperbolic PDEs and relativity too). Different from a parabola even if both look “like arches.”

<b><size=110%>12. Rectified sine \(|\sin|\)</size></b>
Full-wave rectification flips negative lobes upward — a first step in turning AC into something closer to DC for power supplies. Corners at zeros mean derivatives jump — a reminder that idealized circuits still start from calculus intuitions.

<b><size=110%>13. Circle \((x-h)^2 + (y-k)^2 = R^2\)</size></b>
A circle is usually written implicitly. Solving for \(y\) gives two branches (\(\pm\sqrt{\cdot}\)). The game’s circle stage uses the <i>upper</i> semicircle so the path stays a function \(y(x)\) over one sweep. Implicit differentiation yields \(\frac{dy}{dx} = -\frac{x-h}{y-k}\); at the ends of the diameter the tangent is vertical (slope blows up).

<b><size=120%>──────── Aerospace engineering & aerodynamics ────────</size></b>
<size=92%>Levels prefixed <b>Aerospace:</b> turn textbook flight‑vehicle math into paths you run. They are <i>toy models</i> for pedagogy — not CFD, flight test, or ITAR‑grade simulations.</size>

<b><size=118%>Lift, drag, atmosphere</size></b>
\(C_L(\alpha)\) grows ~linearly before <b>stall</b> (flow separation), then drops — slopes & breakpoints drive your platforms. Drag polar \(C_D = C_{D0} + K C_L^2\) is a parabola in \(C_L\): min‑drag sweet spots matter for glide & endurance. Isothermal atmosphere uses \(\rho(h) \propto e^{-h/H}\) (scale height \(H\)): density drives \(q = \tfrac{1}{2}\rho V^2\), Reynolds, thrust lapse.

<b><size=118%>Stability, unsteady aero, entry</size></b>
<b>Phugoid / short‑period</b> moods use damped sinusoids from linearized dynamics (eigenvalues in state space). <b>Strouhal</b> shedding ties frequency to \(U/D\) with sine‑like traces. <b>Newtonian \(\sin^2\alpha\)</b> sketches hypersonic windward pressure teaching curves. <b>Re‑entry decay</b> uses an exponential envelope mood for “how fast heating threat relaxes” in simplified stories.

<b><size=90%>See docs/engineering-math.md § Aerospace for a longer map.</size></b>

<b><size=120%>──────── First principles thinking (business) ────────</size></b>
<size=92%>Startup culture often cites <b>Elon Musk</b> for reviving <b>first principles</b> at places like <b>Tesla / SpaceX</b>: stop trusting “the market always prices it this way,” and instead <b>unpack assumptions</b> until you hit bedrock facts (materials, energy, physics, true unit costs), then <b>reason upward</b> into a new design. The idea is older than any one founder — but the <i>habit</i> matches this game’s visuals.</size>

<b><size=118%>Map the metaphor</size></b>
• <b>Your mental “business curve”</b> is like <b>f(x)</b> — the story you tell about how output moves with a lever (price, latency, quality, throughput).
• <b>Where it breaks or soars</b> is like <b>f'(x)</b> — <i>sensitivity</i>. Same as here: derivative magnitude decides whether the “floor” (your plan) actually holds.
• <b>Stages & pops</b> — slice the bet into acts; each reveal is a new hypothesis. <b>Riemann / area levels</b> — many small choices sum; refine the grid before you scale spend.
• <b>Multivariable slices</b> — fix hidden variables on purpose; one clear 1D walk beats a fuzzy 4D slide deck.

<b><size=92%>This is <i>not</i> legal, tax, or investing advice — a thinking drill you can pair with real advisors and data. Full write-up:</size></b> <b>docs/first-principles-business.md</b> on GitHub Pages.

<b><size=120%>──────── Exam prep (separate tracks) ────────</size></b>

<b><size=118%>Competition mathematics (contest lens)</size></b>
Problems from contests like <b>AMC / AIME</b> often reward <b>bounding</b>, <b>symmetry</b>, and knowing when a function is <b>concave or convex</b>. Natural log is a classic hub: \(\ln\) is <i>concave</i> on its domain — tangents/secants give linear estimates used in inequalities. In-game stage: <b>Competition math: ln, concavity & bound tricks</b> (before the Mandelbrot boss). <size=90%>Not affiliated with MAA or any contest body.</size> → <b>docs/competition-math.md</b>

<b><size=118%>AMC 10 &amp; AMC 12 (MAA, US)</size></b>
Both are middle/early high-school <b>multiple-choice</b> sprints; <b>calculus is not required</b>. Load-bearing ideas: smart algebra, functions (including logs), coordinate geometry, counting &amp; probability, modular arithmetic. Graph fluency from this game helps with <b>shape intuition</b>, <b>domains</b> (e.g. \(\ln\), \(\sqrt{\cdot}\)), and reading options even when you solve by algebra. <size=90%>Not affiliated with MAA — use official AMC materials for real problems.</size> → <b>docs/amc-10-12.md</b>

<b><size=118%>TMUA — Test of Mathematics for University Admission (UK)</size></b>
Two-paper <b>multiple choice</b>. Calculus shows up as fast <b>chain/product/quotient</b> fluency, sketch reasoning from \(f'\) and \(f''\), \(\int\) as signed area / FTC mood, <b>domains</b> of \(\ln\) & \(\sqrt{\cdot}\), exp/log inequalities, limits & asymptotes. Typical traps: \(|x|\) kinks, endpoint maxima, “exactly one option is true” elimination. <size=90%>No reproduced questions—use official TMUA materials.</size> → <b>docs/tmua-calculus.md</b>

<b><size=118%>MAT — Mathematics Admissions Test (Oxford / UK)</size></b>
Emphasis on <b>multi-step reasoning</b> and exact algebra—not the same pacing as TMUA. Calculus supports <b>graph sense</b>, implicit curves, inequalities, and “how many solutions?” via monotonicity. Format evolves; check Oxford’s official MAT page. <size=90%>No reproduced questions—use official MAT papers.</size> → <b>docs/mat-calculus.md</b>

<b><size=118%>AP Calculus BC (College Board, US)</size></b>
BC stacks <b>series & Taylor</b>, <b>parametric / polar / vector-valued</b> motion, <b>DEs</b> (logistic, separable), and richer integration (incl. improper) beside AB. Polar recap: \(\displaystyle A = \tfrac{1}{2}\int r^2\,d\theta\); arc length \(\displaystyle \int \sqrt{r^2+(dr/d\theta)^2}\,d\theta\). In-game: stages <b>BC:</b>, <b>Polar:</b>, <b>Circle</b>, plus Maclaurin & Riemann levels. <size=90%>Pair with CB Course Description & FRQs.</size> → <b>docs/ap-calculus-bc.md</b>

<b><size=118%>AP Physics C (College Board, US)</size></b>
<b>Calculus-first</b> mechanics & E&amp;M: \(v = dr/dt\), \(a = dv/dt\), work integrals, \(\tau = dL/dt\), \(L = I\omega\) on a fixed axis, angular momentum conservation when \(\tau_{\mathrm{ext}} = 0\); E&amp;M uses flux / line-integral setups. Game motifs: <i>exponential decay \((\tau)\)</i>, <i>projectile parabola</i>, <i>rotation / SHM</i>—visual hooks only. → <b>docs/ap-physics-c.md</b>

<b><size=110%>Where to read more</size></b>
<b>docs/math-concepts.md</b> (index), <b>docs/first-principles-business.md</b> (Musk-popularized builder lens ↔ game), <b>docs/competition-math.md</b> &amp; <b>docs/amc-10-12.md</b> (contest / MAA map), <b>docs/engineering-math.md</b> (applied circuits/oscillations), and the exam-prep files above on GitHub Pages.

— © 2022-2026 · GAME GENESIS × ORCH AEROSPACE · First Principles
");
    }
}
