/*
 * FunctionPlotter.cs — John Seong / First Principles
 *
 * Maintenance overview:
 *   • Each Update() calls InitPlotFunction → samples f and numeric f' over [xStart,xEnd].
 *   • Points are in “grid space”: (xPlot + gridOrigin.x, yPlot + gridOrigin.y).
 *   • To add a new curve: extend FunctionType, EvaluateFunctionY, and UpdateEquationText.
 *   • LevelManager sets public fields to match LevelDefinition; differentiate=true feeds DerivRendererUI.
 *   • SampleCurvePlotterY / SetEquationExtraSuffix support Riemann overlay & TMP sub-lines.
 */

using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FunctionPlotter : MonoBehaviour
{
    // The starting & ending x coordinates
    public float xStart = 0f;
    public float xEnd = 10f;

    // Increase in xValue for each for loop iteration
    public float step = 0.5f;

    public float transA = 0, transK = 0, transC = 0, transD = 0;

    // Power variable only applicable to 'x^n' functions — represented as 'n' here
    // Base N variable only applicable to 'n^x' functions — represented as 'n' here

    public int power = 2;

    // A infinitesimally small number chosen in order to perform numerical differentiation
    private float hValue = (float)(Mathf.Pow(10, -4));

    // The type of function to be plotted
    public FunctionType functionType;

    public int baseN = 2;

    /// <summary>When <see cref="functionType"/> is <see cref="FunctionType.CustomExpression"/>, evaluates this as <c>f(u)</c> with <c>u = transK·(x−transD)</c>; plotted <c>y = transA·f(u)+transC</c>.</summary>
    [TextArea(1, 3)]
    public string customExpression = "x^2";

    public bool differentiate = false;

    // Local points list
    private List<Vector2> points = new List<Vector2>();
    private List<Vector2> dPoints = new List<Vector2>();

    [SerializeField] TextMeshProUGUI equationText;

    /// <summary>Optional second line under the main equation (e.g. Riemann / integral note).</summary>
    private string equationExtraSuffix = "";

    private LineRendererUI lineRenderer;
    private DerivRendererUI derivRenderer;

    private void Reset()
    {
        InitPlotFunction();
        RefreshGrid();
    }

    private void OnValidate()
    {
        InitPlotFunction();
        RefreshGrid();
    }

    private void Update()
    {
        InitPlotFunction();
        RefreshGrid();
    }

    public void InitPlotFunction()
    {
        PlotFunction(this.functionType);
    }

    // Refresh ONLY the original function graph, not the derivative one
    public void RefreshLine()
    {
        lineRenderer.enabled = false;
        lineRenderer.enabled = true;
    }

    public void RefreshDeriv()
    {
        derivRenderer.enabled = false;
        derivRenderer.enabled = true;
    }

    public void RefreshGrid()
    {
        var grid = FindAnyObjectByType<GridRendererUI>();
        if (grid == null)
            return;
        grid.enabled = false;
        grid.enabled = true;
    }

    /// <summary>
    /// f(x) in plotter coordinates (before adding grid origin), using current transforms.
    /// </summary>
    public float SampleCurvePlotterY(float xPlotter)
    {
        return EvaluateFunctionY(functionType, transA, transK, transC, transD, power, baseN, xPlotter);
    }

    public void SetEquationExtraSuffix(string suffix)
    {
        equationExtraSuffix = suffix ?? "";
    }

    /// <summary>Switches to typed expression mode (Faxas graphing).</summary>
    public void SetCustomExpression(string expression)
    {
        customExpression = string.IsNullOrWhiteSpace(expression) ? "0" : expression.Trim();
        functionType = FunctionType.CustomExpression;
    }

    private void PlotFunction(FunctionType type)
    {
        lineRenderer = FindAnyObjectByType<LineRendererUI>();
        derivRenderer = FindAnyObjectByType<DerivRendererUI>();

        if (lineRenderer != null)
        {
            points.Clear();
            RefreshLine();

            ComputeGraph(type, transA, transK, transC, transD, power, baseN);
            lineRenderer.points = points;
        }

        if (differentiate == true && lineRenderer != null)
        {
            // Refresh ONLY the derivative graph & show on the UI
            RefreshDeriv();

            if (derivRenderer != null)
            {
                dPoints.Clear();
                points.Clear();
                RefreshLine();

                ComputeGraph(type, transA, transK, transC, transD, power, baseN);
                derivRenderer.points = dPoints;
            }
        }

        else if (differentiate == false)
        {
            dPoints.Clear();

            // Refresh ONLY the derivative graph & hide on the UI
            RefreshDeriv();
        }
    }

    public void ComputeGraph(FunctionType functionType, float transA, float transK, float transC, float transD, int power, int baseN)
    {
        if (equationText != null)
            UpdateEquationText(functionType, transA, transK, transC, transD, power, baseN);

        Vector2Int gridOrigin = lineRenderer.gridSize / 2;

        for (float i = xStart; i <= xEnd; i += step)
        {
            float xValue = i;
            float yValue = EvaluateFunctionY(functionType, transA, transK, transC, transD, power, baseN, xValue);
            float dyValue = (EvaluateFunctionY(functionType, transA, transK, transC, transD, power, baseN, xValue + hValue) - EvaluateFunctionY(functionType, transA, transK, transC, transD, power, baseN, xValue - hValue)) / (hValue * 2);

            if (IsFinite(yValue))
            {
                // Add the coordinates to the array
                this.points.Add(new Vector2(xValue + gridOrigin.x, yValue + gridOrigin.y));
            }

            if (IsFinite(dyValue))
            {
                // Get the differentiated coordinates to another array
                this.dPoints.Add(new Vector2(xValue + gridOrigin.x, dyValue + gridOrigin.y));
            }
        }
    }

    private float EvaluateFunctionY(FunctionType type, float transA, float transK, float transC, float transD, int power, int baseN, float xValue)
    {
        float u = transK * (xValue - transD);

        return type switch
        {
            FunctionType.CustomExpression =>
                EvaluateCustomExpression(transA, transC, u),

            FunctionType.Power => transA * (Mathf.Pow(u, power) + transC),
            FunctionType.Absolute => transA * (Mathf.Abs(u) + transC),
            FunctionType.Exponential => transA * (Mathf.Pow(baseN, u) + transC),
            FunctionType.NaturalExp => transA * (Mathf.Exp(u) + transC),
            FunctionType.Log => transA * (Mathf.Log10(u) + transC),
            FunctionType.NaturalLog => transA * (Mathf.Log(u) + transC),
            FunctionType.SquareRoot => transA * (Mathf.Sqrt(u) + transC),
            FunctionType.Sine => transA * (Mathf.Sin(u) + transC),
            FunctionType.Cosine => transA * (Mathf.Cos(u) + transC),
            FunctionType.Tangent => transA * (Mathf.Tan(u) + transC),

            // Maclaurin (Taylor at 0) partial sums — `power` = number of nonzero terms beyond constant where applicable.
            FunctionType.MaclaurinExpSeries => transA * (MaclaurinExpPartialSum(u, power) + transC),
            FunctionType.MaclaurinSinSeries => transA * (MaclaurinSinPartialSum(u, power) + transC),
            FunctionType.MaclaurinCosSeries => transA * (MaclaurinCosPartialSum(u, power) + transC),

            // Geometric series partial sum Σ u^k, k=0..power — avoid |u|≥1 for stability in view.
            FunctionType.GeometricSeriesPartial => transA * (GeometricPartialSum(u, power) + transC),

            // Multivariable "slices": fix y0 = transC, plot z along x — u = transK*(x - transD). (transD is x-phase only.)
            FunctionType.MultivarParaboloidSlice => transA * (u * u + transC * transC),
            FunctionType.MultivarSaddleSlice => transA * (u * u - transC * transC),

            // Engineering / applied: u = transK*(x - transD); `power` ↔ oscillation index; `baseN` ↔ decay strength.
            FunctionType.DampedOscillator => DampedOscillatorY(u, transA, transC, power, baseN),
            FunctionType.HyperbolicCosine => transA * ((float)System.Math.Cosh(Mathf.Clamp(u, -8f, 8f)) + transC),
            FunctionType.FullWaveRectifiedSine => transA * (Mathf.Abs(Mathf.Sin(u)) + transC),

            // AP Calculus BC & polar: u = transK*(x - transD) plays the role of θ in polar captions.
            FunctionType.Arctangent => transA * Mathf.Atan(u) + transC,
            FunctionType.Logistic => LogisticY(u, transA, transC, baseN),
            FunctionType.HyperbolicSine => transA * (float)System.Math.Sinh(Mathf.Clamp(u, -4f, 4f)) + transC,
            FunctionType.ExponentialDecay => transA * Mathf.Exp(-Mathf.Max(0.02f, transK) * Mathf.Abs(u)) + transC,
            FunctionType.PolarCardioid => transA * (1f + Mathf.Cos(u)) + transC,
            FunctionType.PolarRose => transA * Mathf.Cos(Mathf.Max(1, power) * u) + transC,

            // Upper half of (u)² + (y−k)² = R² with u = transK·(x−h), R = |transA|, k = transC, h = transD.
            FunctionType.CircleUpper => CircleUpperY(u, transA, transC),

            // Aerospace / aerodynamics teaching curves (u = transK·(x−transD)).
            FunctionType.AeroLiftVsAlpha => AeroLiftVsAlphaY(u, transA, transC),
            FunctionType.AeroIsothermalDensity => AeroIsothermalDensityY(u, transA, transC, baseN),
            FunctionType.AeroNewtonianSinSquared => AeroNewtonianSinSquaredY(u, transA, transC),

            // Mandelbrot: escape-time vs Im(c) with Re(c)=transA; use |Im| inside iteration (same count as c̄) — cheap symmetry about the real axis.
            FunctionType.MandelbrotEscapeImSlice => MandelbrotEscapeImSliceY(u, transA, transC, power, baseN),

            _ => 0f
        };
    }

    private float EvaluateCustomExpression(float transA, float transC, float u)
    {
        if (MathExpressionEvaluator.TryEvaluate(customExpression, u, out float fy, out _))
            return transA * fy + transC;
        return float.NaN;
    }

    /// <summary>
    /// 1D slice through the Mandelbrot diag: c = cr + i·u, y ≈ normalized escape iterations (cheap preview, not deep zoom).
    /// Uses <paramref name="ciAbs"/> = |u| when computing z²+c since n(c) = n(c̄). Keep <paramref name="maxIter"/> modest (e.g. 24–32) for CPU.
    /// </summary>
    private static float MandelbrotEscapeImSliceY(float u, float cr, float yOffset, int maxIter, int heightScaleFromBaseN)
    {
        maxIter = Mathf.Clamp(maxIter, 10, 34);
        float amp = 0.14f * Mathf.Max(1, heightScaleFromBaseN);
        int it = MandelbrotEscapeIterations(cr, Mathf.Abs(u), maxIter);
        float norm = it / (float)maxIter;
        return yOffset + amp * norm;
    }

    private static int MandelbrotEscapeIterations(float cr, float ci, int maxIter)
    {
        float zr = 0f, zi = 0f;
        for (int n = 0; n < maxIter; n++)
        {
            float zr2 = zr * zr - zi * zi + cr;
            float zi2 = 2f * zr * zi + ci;
            zr = zr2;
            zi = zi2;
            if (zr * zr + zi * zi > 4f)
                return n;
        }
        return maxIter;
    }

    /// <summary>Crude CL(α): linear to ±α_stall then exponential decay (stall).</summary>
    private static float AeroLiftVsAlphaY(float u, float liftSlope, float c)
    {
        float stall = 0.58f;
        if (Mathf.Abs(u) <= stall)
            return liftSlope * u + c;
        float sign = Mathf.Sign(u);
        float peak = liftSlope * stall * sign;
        return peak * Mathf.Exp(-(Mathf.Abs(u) - stall) * 1.12f) + c;
    }

    /// <summary>ρ/ρ₀ ~ e^{−h/H} for h≥0; u as scaled altitude. baseN scales 1/H.</summary>
    private static float AeroIsothermalDensityY(float u, float rhoScale, float c, int scaleHeightInv)
    {
        float h = Mathf.Max(0f, u);
        float invH = 0.055f * Mathf.Max(1, scaleHeightInv > 0 ? scaleHeightInv : 1);
        return rhoScale * Mathf.Exp(-invH * h) + c;
    }

    /// <summary>Newtonian impact theory mood: Cp ~ sin²α for α∈[0,π/2].</summary>
    private static float AeroNewtonianSinSquaredY(float u, float a, float c)
    {
        float rad = Mathf.Clamp(u, 0f, 1.48f);
        float s = Mathf.Sin(rad);
        return a * s * s + c;
    }

    /// <summary>y = k + √(R² − u²) for |u|≤R; outside domain uses k so samples stay finite (flat shoulder).</summary>
    private static float CircleUpperY(float u, float radiusSigned, float k)
    {
        float r = Mathf.Max(0.02f, Mathf.Abs(radiusSigned));
        float s = r * r - u * u;
        if (s < 0f)
            return float.NaN;
        return k + Mathf.Sqrt(s);
    }

    /// <summary>S-curve L/(1+e^{-s u}) + C; <paramref name="baseN"/> scales steepness (larger → sharper transition).</summary>
    private static float LogisticY(float u, float carryingCapacity, float c, int steepnessFromBaseN)
    {
        float s = 0.05f * Mathf.Max(1, steepnessFromBaseN > 0 ? steepnessFromBaseN : 1);
        float z = Mathf.Clamp(-s * u, -30f, 30f);
        return carryingCapacity / (1f + Mathf.Exp(z)) + c;
    }

    /// <summary>Underdamped-style envelope A·e^(-α|u|)·sin(ωu) + C (u = scaled time/position).</summary>
    private static float DampedOscillatorY(float u, float a, float c, int power, int baseN)
    {
        float omega = 0.28f * Mathf.Max(1, power);
        float decay = 0.042f * Mathf.Max(1, baseN);
        return a * Mathf.Exp(-decay * Mathf.Abs(u)) * Mathf.Sin(omega * u) + c;
    }

    /// <summary>e^u ≈ Σ_{k=0}^{N} u^k/k!</summary>
    private static float MaclaurinExpPartialSum(float u, int maxDegree)
    {
        maxDegree = Mathf.Clamp(maxDegree, 0, 18);
        float sum = 0f;
        float term = 1f;
        sum += term;
        for (int k = 1; k <= maxDegree; k++)
        {
            term *= u / k;
            sum += term;
            if (!IsFinite(sum)) break;
        }
        return sum;
    }

    /// <summary>sin u ≈ Σ (-1)^n u^{2n+1}/(2n+1)! up to n = maxN.</summary>
    private static float MaclaurinSinPartialSum(float u, int maxN)
    {
        maxN = Mathf.Clamp(maxN, 0, 12);
        float sum = 0f;
        float term = u;
        sum += term;
        for (int n = 1; n <= maxN; n++)
        {
            term *= -u * u / ((2f * n) * (2f * n + 1f));
            sum += term;
            if (!IsFinite(sum)) break;
        }
        return sum;
    }

    /// <summary>cos u ≈ Σ (-1)^n u^{2n}/(2n)! up to n = maxN.</summary>
    private static float MaclaurinCosPartialSum(float u, int maxN)
    {
        maxN = Mathf.Clamp(maxN, 0, 12);
        float sum = 0f;
        float term = 1f;
        sum += term;
        for (int n = 1; n <= maxN; n++)
        {
            term *= -u * u / ((2f * n - 1f) * (2f * n));
            sum += term;
            if (!IsFinite(sum)) break;
        }
        return sum;
    }

    /// <summary>Σ_{k=0}^{N} u^k for N = maxPower (geometric partial sum).</summary>
    private static float GeometricPartialSum(float u, int maxPower)
    {
        maxPower = Mathf.Clamp(maxPower, 0, 24);
        float sum = 0f;
        float uPow = 1f;
        sum += uPow;
        for (int k = 1; k <= maxPower; k++)
        {
            uPow *= u;
            sum += uPow;
            if (!IsFinite(sum)) break;
        }
        return sum;
    }

    private static bool IsFinite(float f) => !(float.IsNaN(f) || float.IsInfinity(f));

    private static string EscapeTmpRichText(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return "";
        return raw.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    }

    private void UpdateEquationText(FunctionType type, float transA, float transK, float transC, float transD, int power, int baseN)
    {
        // Keep equation text simple and consistent; domain errors (e.g. log/sqrt of non-positive) are handled by skipping non-finite points.
        string a = transA.ToString();
        string k = transK.ToString();
        string c = transC.ToString();
        string d = transD.ToString();

        switch (type)
        {
            case FunctionType.Power:
                equationText.text = $@"\(f(x) = {a}\cdot\left({k}(x-{d})\right)^{{{power}}} + {c}\)";
                break;
            case FunctionType.Absolute:
                equationText.text = $@"\(f(x) = {a}\left(\left|{k}(x-{d})\right| + {c}\right)\)";
                break;
            case FunctionType.Exponential:
                // transA* (baseN^(k(x-d)) + c)
                equationText.text = $@"\(f(x) = {a}\left({baseN}^{{{k}(x-{d})}} + {c}\right)\)";
                break;
            case FunctionType.NaturalExp:
                equationText.text = $@"\(f(x) = {a}\left(e^{{{k}(x-{d})}} + {c}\right)\)";
                break;
            case FunctionType.Log:
                equationText.text = $@"\(f(x) = {a}\left(\log_{10}\!\left({k}(x-{d})\right) + {c}\right)\)";
                break;
            case FunctionType.NaturalLog:
                equationText.text = $@"\(f(x) = {a}\left(\ln\left({k}(x-{d})\right) + {c}\right)\)";
                break;
            case FunctionType.SquareRoot:
                equationText.text = $@"\(f(x) = {a}\left(\sqrt{{{k}(x-{d})}} + {c}\right)\)";
                break;
            case FunctionType.Sine:
                equationText.text = $@"\(f(x) = {a}\left(\sin\left({k}(x-{d})\right) + {c}\right)\)";
                break;
            case FunctionType.Cosine:
                equationText.text = $@"\(f(x) = {a}\left(\cos\left({k}(x-{d})\right) + {c}\right)\)";
                break;
            case FunctionType.Tangent:
                equationText.text = $@"\(f(x) = {a}\left(\tan\left({k}(x-{d})\right) + {c}\right)\)";
                break;
            case FunctionType.MaclaurinExpSeries:
                equationText.text = $@"\(P_{{{power}}}[e^{{u}}],\; u={k}(x-{d})\)"; 
                break;
            case FunctionType.MaclaurinSinSeries:
                equationText.text = $@"\(P_{{{power}}}[\sin u],\; u={k}(x-{d})\)";
                break;
            case FunctionType.MaclaurinCosSeries:
                equationText.text = $@"\(P_{{{power}}}[\cos u],\; u={k}(x-{d})\)";
                break;
            case FunctionType.GeometricSeriesPartial:
                equationText.text = $@"\(\sum_{{j=0}}^{{{power}}} u^{{j}},\; u={k}(x-{d})\)";
                break;
            case FunctionType.MultivarParaboloidSlice:
                equationText.text = $@"\(z={a}\left(u^{2}+y_{{0}}^{2}\right),\; u={k}(x-{d}),\; y_{{0}}={c}\)";
                break;
            case FunctionType.MultivarSaddleSlice:
                equationText.text = $@"\(z={a}\left(u^{2}-y_{{0}}^{2}\right),\; u={k}(x-{d}),\; y_{{0}}={c}\)";
                break;
            case FunctionType.DampedOscillator:
                equationText.text = $@"\(f={a}\, e^{{-\alpha|u|}}\sin(\omega u)+{c},\; u={k}(x-{d})\)";
                break;
            case FunctionType.HyperbolicCosine:
                equationText.text = $@"\(f={a}\left(\cosh(u)+{c}\right),\; u={k}(x-{d})\)";
                break;
            case FunctionType.FullWaveRectifiedSine:
                equationText.text = $@"\(f={a}\left(\left|\sin u\right|+{c}\right),\; u={k}(x-{d})\)";
                break;
            case FunctionType.Arctangent:
                equationText.text = $@"\(f={a}\arctan(u)+{c},\; u={k}(x-{d})\)";
                break;
            case FunctionType.Logistic:
                equationText.text = $@"\(\text{{Logistic}},\; L\approx{a},\; u={k}(x-{d})\)";
                break;
            case FunctionType.HyperbolicSine:
                equationText.text = $@"\(f={a}\sinh(u)+{c},\; u={k}(x-{d})\)";
                break;
            case FunctionType.ExponentialDecay:
                equationText.text = $@"\(f={a}\, e^{{-{k}|u|}}+{c},\; u={k}(x-{d})\)";
                break;
            case FunctionType.PolarCardioid:
                equationText.text = $@"\(r \propto 1+\cos\theta,\; \theta\leftrightarrow u={k}(x-{d})\)";
                break;
            case FunctionType.PolarRose:
                equationText.text = $@"\(r \propto \cos({power}\theta),\; \theta\leftrightarrow u={k}(x-{d})\)";
                break;
            case FunctionType.CircleUpper:
                equationText.text = $@"\(u^{2}+(y-{c})^{2}={a}^{2}\ \text{{(upper)}},\; u={k}(x-{d})\)";
                break;
            case FunctionType.AeroLiftVsAlpha:
                equationText.text = $@"\(C_L(\alpha)\ \text{{stall model}},\; \text{{slope}}\approx {a},\; u={k}(x-{d})\)";
                break;
            case FunctionType.AeroIsothermalDensity:
                equationText.text = $@"\(\rho/\rho_0 \propto e^{{-h/H}},\; u={k}(x-{d}),\; H^{{-1}}\propto {baseN}\)";
                break;
            case FunctionType.AeroNewtonianSinSquared:
                equationText.text = $@"\(C_p \propto \sin^{{2}}\alpha,\; u={k}(x-{d})\)";
                break;
            case FunctionType.MandelbrotEscapeImSlice:
                equationText.text =
                    $@"<b>\(\text{{Mandelbrot slice}}\)</b> \(h\propto\text{{escape-time}},\; c=({a})+\mathrm{{i}}u,\; u={k}(x-{d}),\; N={power}\)";
                break;
            case FunctionType.CustomExpression:
            {
                string esc = EscapeTmpRichText(customExpression);
                equationText.text =
                    $"<b>Typed f(u)</b>\n" +
                    $"<size=94%><color=#f2f4ff>{esc}</color></size>\n" +
                    $"<size=78%><color=#94a3b8>Plotted: <b>y = A·f(u)+C</b>, u = k·(x−D). Edit below. Use sin, cos, tan, sqrt, ln, log, exp, ^, pi, e …</color></size>";
                if (!string.IsNullOrEmpty(equationExtraSuffix))
                    equationText.text += $"\n<size=85%><color=#a8b2d1>{equationExtraSuffix}</color></size>";
                return;
            }
            default:
                equationText.text = @"\(f(x)\)";
                break;
        }

        if (!string.IsNullOrEmpty(equationExtraSuffix))
            equationText.text += $"\n<size=85%><color=#a8b2d1>{equationExtraSuffix}</color></size>";

        if (equationText != null)
            equationText.text = TmpLatex.Process(equationText.text);
    }
}

public enum FunctionType
{
    Power,
    Absolute,
    Exponential,
    NaturalExp,
    Log,
    NaturalLog,
    SquareRoot,
    Sine,
    Cosine,
    Tangent,

    // Infinite series / Taylor–Maclaurin (partial sums)
    MaclaurinExpSeries,
    MaclaurinSinSeries,
    MaclaurinCosSeries,
    GeometricSeriesPartial,

    // Multivariable surfaces as 1D slices (fixed y₀ = transC, δ = transD shift)
    MultivarParaboloidSlice,
    MultivarSaddleSlice,

    // Engineering / applied classical shapes
    DampedOscillator,
    HyperbolicCosine,
    FullWaveRectifiedSine,

    // AP Calculus BC extras, polar (r vs θ plotted with θ on the horizontal axis), Physics C hooks
    Arctangent,
    Logistic,
    HyperbolicSine,
    ExponentialDecay,
    PolarCardioid,
    PolarRose,

    // Upper semicircle: y = k + √(R²−u²), u = transK·(x−transD), R = |transA|, center (transD, transC) when transK = 1.
    CircleUpper,

    // Aerospace / aerodynamics (toy models for instruction — not a CFD solver)
    AeroLiftVsAlpha,
    AeroIsothermalDensity,
    AeroNewtonianSinSquared,

    /// <summary>Escape iteration count vs Im(c) with fixed Re(c) = transA (boss slice); uses |Im| in iteration for conjugate symmetry.</summary>
    MandelbrotEscapeImSlice,

    /// <summary>User-typed <c>f(u)</c> via <see cref="FunctionPlotter.customExpression"/> (Faxas graphing).</summary>
    CustomExpression
}

/* 

:: Numerical Differentiation Explanation ::

You can't calculate the exact derivative of a function using a computer program (unless you're doing symbolic math... but that's another, way more complicated, topic).

There are several approaches to computing a numerical derivative of a function. The simplest is the centered three-point method:

Take a small number h
Evaluate  [f(x+h) - f(x-h)] / 2h 
Voilà, an approximation of f'(x), with only two function evaluations
Another approach is the centered five-point method:

Take a small number h
Evaluate [f(x-2h) - 8f(x-h) + 8f(x+h) - f(x+2h)] / 12h
Voilà, a better approximation of f'(x), but it requires more function evaluations

In this program, we use the relatively simpler approach 'the three-point method.'

*/