using System.Collections.Generic;

/// <summary>
/// Which <c>@@SECTION=id</c> blocks from <c>Localization/MathArticle/*.txt</c> to show when opening
/// <b>Math concepts</b> during a level. Level select passes <see langword="null"/> for the full article.
/// </summary>
public static class MathArticleLevelSectionMap
{
    /// <summary>Ordered section ids to concatenate for <paramref name="levelIndex"/>.</summary>
    public static IReadOnlyList<string> GetSectionKeysForLevel(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= GameLevelCatalog.LevelCount)
            return new[] { "intro", "how_graph" };

        if (GameLevelCatalog.IsComingSoonLevel(levelIndex))
            return new[] { "intro", "how_graph" };

        // 0 — First Principles Primer
        if (levelIndex == 0)
            return new[] { "intro", "how_graph", "s1", "rules", "biz" };

        if (levelIndex == 1)
            return new[] { "intro", "how_graph", "s1", "s2" };

        if (levelIndex is >= 2 and <= 3)
            return new[] { "intro", "how_graph", "s1", "s3" };

        if (levelIndex == 4)
            return new[] { "intro", "how_graph", "s1", "s4" };

        if (levelIndex == 5)
            return new[] { "intro", "how_graph", "s1", "s5", "rules" };

        if (levelIndex == 6)
            return new[] { "intro", "how_graph", "s1", "s5" };

        if (levelIndex == 7)
            return new[] { "intro", "how_graph", "s1", "s6" };

        if (levelIndex is >= 8 and <= 9)
            return new[] { "intro", "how_graph", "s1", "s7" };

        if (levelIndex == 10)
            return new[] { "intro", "how_graph", "s1", "s8", "int_block" };

        if (levelIndex is >= 11 and <= 13)
            return new[] { "intro", "how_graph", "s8", "int_block" };

        if (levelIndex == 14)
            return new[] { "intro", "how_graph", "s10", "s9" };

        if (levelIndex == 15)
            return new[] { "intro", "how_graph", "s11" };

        if (levelIndex == 16)
            return new[] { "intro", "how_graph", "s12" };

        if (levelIndex == 17)
            return new[] { "intro", "how_graph", "s1", "rules" };

        if (levelIndex == 18)
            return new[] { "intro", "how_graph", "rules", "int_block" };

        if (levelIndex is >= 19 and <= 20)
            return new[] { "intro", "how_graph", "rules", "exam" };

        if (levelIndex == 21)
            return new[] { "intro", "how_graph", "s5", "rules" };

        if (levelIndex == 22)
            return new[] { "intro", "how_graph", "s10", "rules" };

        if (levelIndex == 23)
            return new[] { "intro", "how_graph", "rules", "exam" };

        if (levelIndex == 24)
            return new[] { "intro", "how_graph", "s2", "exam" };

        if (levelIndex == 25)
            return new[] { "intro", "how_graph", "s5", "rules" };

        if (levelIndex == 26)
            return new[] { "intro", "how_graph", "rules", "int_block" };

        if (levelIndex == 27)
            return new[] { "intro", "how_graph", "s4", "rules" };

        if (levelIndex == 28)
            return new[] { "intro", "how_graph", "s1", "rules", "s3" };

        if (levelIndex == 29)
            return new[] { "intro", "how_graph", "s1", "rules" };

        if (levelIndex == 30)
            return new[] { "intro", "how_graph", "s3", "s10" };

        if (levelIndex == 31)
            return new[] { "intro", "how_graph", "s1", "rules" };

        if (levelIndex == 32)
            return new[] { "intro", "how_graph", "rules" };

        if (levelIndex == 33)
            return new[] { "intro", "how_graph", "s13" };

        if (levelIndex == 34)
            return new[] { "intro", "how_graph", "s9", "exam" };

        if (levelIndex is >= 35 and <= 41)
            return new[] { "intro", "how_graph", "aero" };

        if (levelIndex is >= 42 and <= 43)
            return new[] { "intro", "how_graph", "s2", "s3" };

        if (levelIndex is >= 44 and <= 45)
            return new[] { "intro", "how_graph", "s9", "rules", "int_block" };

        if (levelIndex is >= 46 and <= 49)
            return new[] { "intro", "how_graph", "rules", "exam" };

        if (levelIndex == 50)
            return new[] { "intro", "how_graph", "s10", "s3", "exam" };

        if (levelIndex is >= 51 and <= 58)
            return new[] { "intro", "how_graph", "s1", "rules" };

        return new[] { "intro", "how_graph" };
    }
}
