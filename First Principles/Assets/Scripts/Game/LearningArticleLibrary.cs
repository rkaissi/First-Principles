using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Plain-language math snippets for the in-game reader (TextMesh Pro rich text).
/// Mathematical parts use LaTeX delimiters <c>\( … \)</c> / <c>\[ … \]</c>; <see cref="TmpLatex.Process"/> converts them at load time.
/// Body text loads from <c>Resources/Localization/MathArticle/{language}.txt</c> (fallback: <c>en.txt</c>) so every locale can ship a translation.
/// Optional lines <c>@@SECTION=id</c> split the article; in-game <see cref="MathArticlesOverlay"/> can request a subset by level.
/// </summary>
public static class LearningArticleLibrary
{
    private const string MathArticleResourcePrefix = "Localization/MathArticle/";
    private const string SectionDirectivePrefix = "@@SECTION=";

    /// <summary>Full glossary (level select, or locales without section markers).</summary>
    public static string GetLevelSelectArticleRichText() => GetArticleRichTextForOverlay(forLevelIndex: null);

    /// <param name="forLevelIndex"><see langword="null"/> = entire article (section directive lines stripped). Otherwise sections from <see cref="MathArticleLevelSectionMap"/>.</param>
    public static string GetArticleRichTextForOverlay(int? forLevelIndex)
    {
        string raw = LoadRawArticleForCurrentLanguage();
        string selected = SelectRawForOverlay(raw, forLevelIndex);
        return ProcessArticleBody(selected);
    }

    private static string ProcessArticleBody(string raw)
    {
        string processed = TmpLatex.Process(raw);
        return processed.Replace("&amp;", "&");
    }

    private static string SelectRawForOverlay(string raw, int? forLevelIndex)
    {
        if (string.IsNullOrEmpty(raw) || raw.IndexOf(SectionDirectivePrefix, StringComparison.Ordinal) < 0)
            return raw;

        if (forLevelIndex == null)
            return StripSectionDirectiveLines(raw);

        Dictionary<string, string> sections = ParseSectionDictionary(raw);
        if (sections.Count == 0)
            return StripSectionDirectiveLines(raw);

        IReadOnlyList<string> keys = MathArticleLevelSectionMap.GetSectionKeysForLevel(forLevelIndex.Value);
        var sb = new StringBuilder();
        foreach (string key in keys)
        {
            if (!sections.TryGetValue(key, out string chunk) || string.IsNullOrWhiteSpace(chunk))
                continue;
            if (sb.Length > 0)
                sb.Append("\n\n");
            sb.Append(chunk.Trim());
        }

        if (sb.Length == 0)
            return StripSectionDirectiveLines(raw);

        return sb.ToString();
    }

    private static string StripSectionDirectiveLines(string raw)
    {
        string[] lines = raw.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        var sb = new StringBuilder();
        foreach (string line in lines)
        {
            if (line.TrimStart().StartsWith(SectionDirectivePrefix, StringComparison.Ordinal))
                continue;
            if (sb.Length > 0)
                sb.Append('\n');
            sb.Append(line);
        }

        return sb.ToString();
    }

    private static Dictionary<string, string> ParseSectionDictionary(string raw)
    {
        var d = new Dictionary<string, string>(StringComparer.Ordinal);
        string[] lines = raw.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        string currentKey = null;
        var chunk = new StringBuilder();

        void Flush()
        {
            if (currentKey != null)
                d[currentKey] = chunk.ToString().TrimEnd();
            chunk.Clear();
        }

        foreach (string line in lines)
        {
            string t = line.Trim();
            if (t.StartsWith(SectionDirectivePrefix, StringComparison.Ordinal) && t.Length > SectionDirectivePrefix.Length)
            {
                Flush();
                currentKey = t.Substring(SectionDirectivePrefix.Length).Trim();
                continue;
            }

            if (chunk.Length > 0)
                chunk.Append('\n');
            chunk.Append(line);
        }

        Flush();
        return d;
    }

    private static string LoadRawArticleForCurrentLanguage()
    {
        string code = LocalizationManager.CurrentLanguage;
        TextAsset ta = Resources.Load<TextAsset>($"{MathArticleResourcePrefix}{code}");
        if (ta == null || string.IsNullOrWhiteSpace(ta.text))
            ta = Resources.Load<TextAsset>($"{MathArticleResourcePrefix}en");

        if (ta != null && !string.IsNullOrEmpty(ta.text))
            return ta.text;

        return "<b>Math article missing.</b>\n\nPlace <b>Localization/MathArticle/en.txt</b> under Resources (see repo).";
    }
}
