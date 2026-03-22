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
/// <summary>Level-select submenu: contiguous level index range (inclusive).</summary>
public readonly struct LevelSelectCategory
{
    public readonly string TitleLocalizationKey;
    public readonly string DefaultTitle;
    public readonly int FirstLevelIndex;
    public readonly int LastLevelIndexInclusive;

    public LevelSelectCategory(string titleLocalizationKey, string defaultTitle, int firstLevelIndex, int lastLevelIndexInclusive)
    {
        TitleLocalizationKey = titleLocalizationKey;
        DefaultTitle = defaultTitle;
        FirstLevelIndex = firstLevelIndex;
        LastLevelIndexInclusive = lastLevelIndexInclusive;
    }

    public int LevelCount => LastLevelIndexInclusive - FirstLevelIndex + 1;
}

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
        "Physics C: thermodynamics (adiabatic P–V, γ from N)",
        // --- Aerospace engineering & aerodynamics (order must match LevelManager.BuildSampleLevels) ---
        "Aerospace: lift C_L(α) linear + stall",
        "Aerospace: drag polar (parasitic, induced, total)",
        "Aerospace: isothermal atmosphere ρ(h)",
        "Aerospace: phugoid / damped pitch–heave mood",
        "Aerospace: Newtonian Cp ~ sin²α",
        "Aerospace: Strouhal / vortex shedding tone",
        "Aerospace: re-entry decay envelope (ρV heating mood)",
        "Economics: dot-com bubble & crash (stylized index)",
        "Economics: 2008 crisis & recovery (stylized index)",
        "Transforms: Fourier — sinc spectrum (rect ↔ sinc)",
        "Transforms: Laplace — causal exponential decay",
        "BOSS: golden ratio spiral (logarithmic polar)",
        "BOSS: Mandelbrot — finale (fractal escape slice)",
        "BOSS: Lorenz attractor — Chaos Theory (strange attractor)",
        "BOSS: Black hole — gravity well (1/r slice)",
        "Physics C: spring–mass SHM (Hooke's law, undamped)",
        "Big O: O(1) — constant time",
        "Big O: O(log n) — logarithmic growth",
        "Big O: O(√n) — sublinear root growth",
        "Big O: O(n) — linear growth",
        "Big O: O(n log n) — n log n growth",
        "Big O: O(n²) — quadratic growth",
        "Big O: O(n³) — cubic growth",
        "Big O: O(2ⁿ) — exponential (base 2)",
        // Placeholder: listed in Level Select but not built in LevelManager yet (see IsComingSoonLevel).
        "Astrophysics: Hohmann transfer"
    };

    public static int LevelCount => DisplayNames.Length;

    /// <summary>Level index reserved for a future stage; <see cref="LevelSelectController"/> shows “coming soon” instead of loading Game.</summary>
    public const int HohmannTransferComingSoonLevelIndex = 59;

    public static bool IsComingSoonLevel(int index) =>
        index == HohmannTransferComingSoonLevelIndex;

    /// <summary>
    /// Grouped picker on the Level Select scene (indices must match <see cref="DisplayNames"/> / <see cref="LevelManager"/>).
    /// </summary>
    public static readonly LevelSelectCategory[] SelectCategories =
    {
        new LevelSelectCategory("level_select.cat.core", "Core & series", 0, 8),
        new LevelSelectCategory("level_select.cat.integration", "Multivar & integration", 9, 13),
        new LevelSelectCategory("level_select.cat.engineering", "Engineering", 14, 16),
        new LevelSelectCategory("level_select.cat.ap_bc", "AP Calculus BC & Physics C", 17, 34),
        new LevelSelectCategory("level_select.cat.aerospace", "Aerospace", 35, 41),
        new LevelSelectCategory("level_select.cat.astrophysics", "Astrophysics", 59, 59),
        new LevelSelectCategory("level_select.cat.economics", "Economics", 42, 43),
        new LevelSelectCategory("level_select.cat.transforms", "Transforms", 44, 45),
        new LevelSelectCategory("level_select.cat.spring_physics", "Spring & SHM", 50, 50),
        new LevelSelectCategory("level_select.cat.big_o", "Big O notation", 51, 58),
        new LevelSelectCategory("level_select.cat.final_boss", "Final boss", 46, 49),
    };

    /// <summary>First index of the contiguous <b>Aerospace:</b> block (must match <see cref="LevelManager"/> sample levels).</summary>
    public const int AerospaceLevelsBeginIndex = 35;

    /// <summary>Inclusive last index of the Aerospace block (re-entry stage).</summary>
    public const int AerospaceLevelsEndIndex = 41;

    /// <summary>True for levels whose titles start with <c>Aerospace:</c> in the catalog.</summary>
    public static bool IsAerospaceLevel(int index) =>
        index >= AerospaceLevelsBeginIndex && index <= AerospaceLevelsEndIndex;

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
