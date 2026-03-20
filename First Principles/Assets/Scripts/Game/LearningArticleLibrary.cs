using UnityEngine;

/// <summary>
/// Plain-language math snippets for the in-game reader (TextMesh Pro rich text).
/// Mathematical parts use LaTeX delimiters <c>\( … \)</c> / <c>\[ … \]</c>; <see cref="TmpLatex.Process"/> converts them at load time.
/// Body text loads from <c>Resources/Localization/MathArticle/{language}.txt</c> (fallback: <c>en.txt</c>) so every locale can ship a translation.
/// Mirror ideas in docs (MathJax on GitHub Pages).
/// </summary>
public static class LearningArticleLibrary
{
    private const string MathArticleResourcePrefix = "Localization/MathArticle/";

    /// <summary>Large TMP block; per-locale file is UTF-8 with meaningful newlines.</summary>
    public static string GetLevelSelectArticleRichText()
    {
        string raw = LoadRawArticleForCurrentLanguage();
        return TmpLatex.Process(raw);
    }

    private static string LoadRawArticleForCurrentLanguage()
    {
        string code = LocalizationManager.CurrentLanguage;
        var ta = Resources.Load<TextAsset>($"{MathArticleResourcePrefix}{code}");
        if (ta == null || string.IsNullOrWhiteSpace(ta.text))
            ta = Resources.Load<TextAsset>($"{MathArticleResourcePrefix}en");

        if (ta != null && !string.IsNullOrEmpty(ta.text))
            return ta.text;

        return "<b>Math article missing.</b>\n\nPlace <b>Localization/MathArticle/en.txt</b> under Resources (see repo).";
    }
}
