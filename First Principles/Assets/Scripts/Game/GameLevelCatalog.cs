using UnityEngine;

/// <summary>
/// Shared level titles (must match the order built in LevelManager sample levels).
/// </summary>
public static class GameLevelCatalog
{
    public static readonly string[] DisplayNames =
    {
        "Slope of Parabola",
        "Waves of Sine",
        "Shadows of Cosine",
        "Absolute Path"
    };

    public static int LevelCount => DisplayNames.Length;
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
