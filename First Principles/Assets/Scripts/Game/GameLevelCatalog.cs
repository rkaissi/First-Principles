using UnityEngine;

// -----------------------------------------------------------------------------
// GameLevelCatalog + LevelSelection — strings & handoff from LevelSelect → Game
// -----------------------------------------------------------------------------
// DisplayNames[i] MUST correspond to levels[i] from LevelManager.BuildSampleLevels.
// LevelSelection is static session state cleared after ConsumeSelectedLevel.
// -----------------------------------------------------------------------------

/// <summary>
/// Shared level titles (must match the order built in <see cref="LevelManager"/> sample levels).
/// </summary>
public static class GameLevelCatalog
{
    /// <summary>Human-readable titles; indices drive LevelSelectController button order.</summary>
    public static readonly string[] DisplayNames =
    {
        "First Principles Primer",
        "Slope of Parabola",
        "Waves of Sine",
        "Shadows of Cosine",
        "Absolute Path",
        "Maclaurin: e^x",
        "Maclaurin: sin(x)",
        "Series: geometric tail",
        "Saddle slice (multivar)",
        "Paraboloid slice (multivar)",
        "Area under the curve",
        "Riemann: left endpoints",
        "Riemann: right endpoints",
        "Riemann: midpoint rule",
        "Engineering: damped oscillation",
        "Engineering: catenary (cosh)",
        "Engineering: rectified AC (|sin|)",
        // --- AP Calculus BC + Physics C extension (order must match LevelManager.BuildSampleLevels) ---
        "BC: arctan & inverse trig",
        "BC: logistic growth (dP/dt = kP(1−P/L))",
        "Polar: cardioid r ~ 1+cos θ",
        "Polar: rose r ~ cos(nθ)",
        "BC: sinh x & hyperbolic functions",
        "Physics C: exponential decay (τ, RC)",
        "Physics C: angular momentum & L = Iω",
        "Physics C: projectile height y(t)",
        "BC: Maclaurin cos(x)",
        "BC: ln x & ∫ dx/x",
        "BC: √x & domain / cusp craft",
        "BC: tan x between asymptotes",
        "BC: e^{kx} & y′ = ky",
        "BC: phase & SHM (energy swaps)",
        "BC: cubic & inflection (sketching)",
        "BC: b^x & d/dx b^x",
        "Circle: (x−h)² + (y−k)² = R²",
        // --- Aerospace engineering & aerodynamics (order must match LevelManager.BuildSampleLevels) ---
        "Aerospace: lift C_L(α) linear + stall",
        "Aerospace: parabolic drag polar C_D(C_L)",
        "Aerospace: isothermal atmosphere ρ(h)",
        "Aerospace: phugoid / damped pitch–heave mood",
        "Aerospace: Newtonian Cp ~ sin²α",
        "Aerospace: Strouhal / vortex shedding tone",
        "Aerospace: re-entry decay envelope (ρV heating mood)",
        "Competition math: ln, concavity & bound tricks",
        "BOSS: Mandelbrot escape slice (fractal boundary mood)"
    };

    public static int LevelCount => DisplayNames.Length;

    /// <summary>Localized title for level <paramref name="index"/>; falls back to <see cref="DisplayNames"/>.</summary>
    public static string GetLocalizedDisplayName(int index)
    {
        if (index < 0 || index >= DisplayNames.Length)
            return "";
        string key = $"level.{index}";
        return LocalizationManager.Get(key, DisplayNames[index]);
    }
}

/// <summary>
/// Carries the chosen level index from the level-select scene into <c>Game</c>.
/// </summary>
public static class LevelSelection
{
    public static int SelectedLevelIndex { get; private set; }
    public static bool HasSelection { get; private set; }

    public static void SetSelectedLevel(int index)
    {
        SelectedLevelIndex = index;
        HasSelection = true;
    }

    /// <summary>Returns 0 if no level was chosen (e.g. opened Game scene directly).</summary>
    public static int ConsumeSelectedLevel(int levelCount)
    {
        if (levelCount <= 0)
            return 0;

        if (!HasSelection)
            return 0;

        HasSelection = false;
        return Mathf.Clamp(SelectedLevelIndex, 0, levelCount - 1);
    }
}
