using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

// -----------------------------------------------------------------------------
// LocalizationManager — key=value tables under Resources/Localization/{code}.txt
// -----------------------------------------------------------------------------
// PlayerPrefs key: fp_language. Codes: en, hi, ur, ar, fr, zh, ko, ja, de, es
// -----------------------------------------------------------------------------

/// <summary>Loads string tables and notifies listeners when the active language changes.</summary>
public static class LocalizationManager
{
    public const string PlayerPrefsKey = "fp_language";

    /// <summary>Ordered list used by the menu language control and <see cref="CycleNext"/>.</summary>
    public static readonly string[] LanguageCodes = { "en", "hi", "ur", "ar", "fr", "zh", "ko", "ja", "de", "es" };

    private static readonly Dictionary<string, string> Table = new Dictionary<string, string>(StringComparer.Ordinal);
    private static string _current = "en";

    public static event Action LanguageChanged;

    public static string CurrentLanguage => _current;

    public static bool IsRightToLeft =>
        string.Equals(_current, "ar", StringComparison.Ordinal) ||
        string.Equals(_current, "ur", StringComparison.Ordinal);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Boot()
    {
        if (!PlayerPrefs.HasKey(PlayerPrefsKey))
        {
            string initial = MapDeviceLanguageToCode();
            PlayerPrefs.SetString(PlayerPrefsKey, initial);
            PlayerPrefs.Save();
        }

        string saved = PlayerPrefs.GetString(PlayerPrefsKey, "en");
        if (Array.IndexOf(LanguageCodes, saved) < 0)
            saved = "en";
        LoadLanguageInternal(saved, raiseEvent: false);
    }

    /// <summary>
    /// Maps <see cref="Application.systemLanguage"/> (and a few platform hints) to a <see cref="LanguageCodes"/> entry.
    /// Used only on <b>first launch</b> before <see cref="PlayerPrefsKey"/> exists. Users can change language anytime in menus.
    /// </summary>
    /// <remarks>Unity’s enum does not list every ISO language; unsupported locales default to <c>en</c>. iOS/Android/macOS/Windows/Linux all supply <see cref="Application.systemLanguage"/>.</remarks>
    public static string MapDeviceLanguageToCode()
    {
        var lang = Application.systemLanguage;

        switch (lang)
        {
            case SystemLanguage.English:
                return "en";
            case SystemLanguage.French:
                return "fr";
            case SystemLanguage.German:
                return "de";
            case SystemLanguage.Japanese:
                return "ja";
            case SystemLanguage.Korean:
                return "ko";
            case SystemLanguage.Chinese:
            case SystemLanguage.ChineseSimplified:
                return "zh";
            case SystemLanguage.ChineseTraditional:
                return "zh";
            case SystemLanguage.Spanish:
                return "es";
            case SystemLanguage.Arabic:
                return "ar";
            case SystemLanguage.Hindi:
                return "hi";
            default:
                break;
        }

        // Some Unity versions expose Urdu only via enum name; string compare is version-safe.
        if (string.Equals(lang.ToString(), "Urdu", StringComparison.OrdinalIgnoreCase))
            return "ur";

        return "en";
    }

    /// <summary>Returns native UI name for a language code (for the picker label).</summary>
    public static string GetLanguagePickerLabel(string code)
    {
        switch (code)
        {
            case "en": return "English";
            case "hi": return "हिन्दी";
            case "ur": return "اردو";
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

    /// <summary>
    /// Text for the compact language chip (<c>Language: …</c>). Uses a left-to-right embed so the first tap after
    /// English (Arabic, Hindi, etc.) lays out reliably in TextMesh Pro; full UI still uses <see cref="ApplyTextDirection"/>.
    /// </summary>
    public static string GetLanguageChipDisplayText()
    {
        string langWord = Get("ui.language", "Language");
        string name = GetLanguagePickerLabel(_current);
        // U+200E LEFT-TO-RIGHT MARK — keeps “label: native name” readable when the active locale is RTL or mixed scripts.
        return "\u200e" + langWord + ": " + name;
    }

    /// <summary>Forces LTR on the language-cycle control so mixed Latin + Arabic / Devanagari renders correctly.</summary>
    public static void ApplyLanguagePickerTextDirection(TextMeshProUGUI tmp)
    {
        if (tmp == null)
            return;
        tmp.isRightToLeftText = false;
    }
}
