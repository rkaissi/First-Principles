using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

// -----------------------------------------------------------------------------
// LocalizationManager — key=value tables under Resources/Localization/{code}.txt
// -----------------------------------------------------------------------------
// PlayerPrefs key: fp_language. Codes: en, ar, fr, zh, ko, ja, de, es
// -----------------------------------------------------------------------------

/// <summary>Loads string tables and notifies listeners when the active language changes.</summary>
public static class LocalizationManager
{
    public const string PlayerPrefsKey = "fp_language";

    /// <summary>Ordered list used by the menu language control and <see cref="CycleNext"/>.</summary>
    public static readonly string[] LanguageCodes = { "en", "ar", "fr", "zh", "ko", "ja", "de", "es" };

    private static readonly Dictionary<string, string> Table = new Dictionary<string, string>(StringComparer.Ordinal);
    private static string _current = "en";

    public static event Action LanguageChanged;

    public static string CurrentLanguage => _current;

    public static bool IsRightToLeft => string.Equals(_current, "ar", StringComparison.Ordinal);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Boot()
    {
        string saved = PlayerPrefs.GetString(PlayerPrefsKey, "en");
        if (Array.IndexOf(LanguageCodes, saved) < 0)
            saved = "en";
        LoadLanguageInternal(saved, raiseEvent: false);
    }

    /// <summary>Returns native UI name for a language code (for the picker label).</summary>
    public static string GetLanguagePickerLabel(string code)
    {
        switch (code)
        {
            case "en": return "English";
            case "ar": return "العربية";
            case "fr": return "Français";
            case "zh": return "简体中文";
            case "ko": return "한국어";
            case "ja": return "日本語";
            case "de": return "Deutsch";
            case "es": return "Español";
            default: return code;
        }
    }

    public static void SetLanguage(string code)
    {
        if (string.IsNullOrEmpty(code))
            return;
        if (Array.IndexOf(LanguageCodes, code) < 0)
            code = "en";

        if (string.Equals(_current, code, StringComparison.Ordinal))
            return;

        LoadLanguageInternal(code, raiseEvent: true);
        PlayerPrefs.SetString(PlayerPrefsKey, code);
        PlayerPrefs.Save();
    }

    /// <summary>Cycles through <see cref="LanguageCodes"/> (for a compact menu control).</summary>
    public static void CycleNext()
    {
        int i = Array.IndexOf(LanguageCodes, _current);
        if (i < 0) i = 0;
        int next = (i + 1) % LanguageCodes.Length;
        SetLanguage(LanguageCodes[next]);
    }

    private static void LoadLanguageInternal(string code, bool raiseEvent)
    {
        Table.Clear();
        _current = code;

        ParseInto(Table, LoadTextAsset("en"));
        if (!string.Equals(code, "en", StringComparison.Ordinal))
            ParseInto(Table, LoadTextAsset(code));

        if (raiseEvent)
            LanguageChanged?.Invoke();
    }

    private static string LoadTextAsset(string code)
    {
        var ta = Resources.Load<TextAsset>($"{LocalizationResourceFolder}/{code}");
        return ta != null ? ta.text : "";
    }

    private const string LocalizationResourceFolder = "Localization";

    private static void ParseInto(Dictionary<string, string> sink, string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        using var reader = new StringReader(text);
        string line;
        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();
            if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                continue;

            int eq = line.IndexOf('=');
            if (eq <= 0)
                continue;

            string key = line.Substring(0, eq).Trim();
            string val = line.Substring(eq + 1).Trim();
            val = val.Replace("\\n", "\n");
            if (key.Length > 0)
                sink[key] = val;
        }
    }

    /// <summary>Localized string or <paramref name="fallback"/> or <paramref name="key"/>.</summary>
    public static string Get(string key, string fallback = null)
    {
        if (string.IsNullOrEmpty(key))
            return fallback ?? "";

        // Legacy key (renamed for English terminology: “graphing” vs “graphic”).
        if (string.Equals(key, "ui.graphic_calculator_mode", StringComparison.Ordinal))
            key = "ui.graphing_calculator_mode";

        if (Table.TryGetValue(key, out string v) && !string.IsNullOrEmpty(v))
            return v;

        return fallback ?? key;
    }

    /// <summary>Uses translation only if present and non-empty; otherwise <paramref name="fallbackEnglish"/>.</summary>
    public static string GetWithFallback(string key, string fallbackEnglish)
    {
        if (string.IsNullOrEmpty(key))
            return fallbackEnglish ?? "";

        if (string.Equals(key, "ui.graphic_calculator_mode", StringComparison.Ordinal))
            key = "ui.graphing_calculator_mode";

        if (Table.TryGetValue(key, out string v) && !string.IsNullOrEmpty(v))
            return v;

        return fallbackEnglish ?? "";
    }

    public static void ApplyTextDirection(TextMeshProUGUI tmp)
    {
        if (tmp == null)
            return;
        tmp.isRightToLeftText = IsRightToLeft;
    }
}
