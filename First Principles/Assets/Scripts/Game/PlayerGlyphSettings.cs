using System;
using UnityEngine;

/// <summary>
/// Persisted platformer avatar symbol (shown on main menu and on the in-game player).
/// Default is <b>∑</b> (sigma); stored in <see cref="PlayerPrefs"/>.
/// </summary>
public static class PlayerGlyphSettings
{
    private const string PrefsKey = "player_glyph_index";

    /// <summary>Index in <see cref="Glyphs"/> when no preference is saved yet (sigma ∑).</summary>
    public const int DefaultGlyphIndex = 7;

    /// <summary>Ordered list of symbols the player can choose (Geometry Dash–style math icons).</summary>
    public static readonly string[] Glyphs = { "+", "×", "x", "=", "−", "÷", "π", "∑" };

    private static readonly Color[] AccentColors =
    {
        new Color(0.95f, 0.42f, 0.38f, 0.98f),
        new Color(0.18f, 0.55f, 0.62f, 0.98f),
        new Color(0.55f, 0.42f, 0.95f, 0.98f),
        new Color(0.95f, 0.75f, 0.28f, 0.98f),
        new Color(0.35f, 0.72f, 0.45f, 0.98f),
        new Color(0.95f, 0.55f, 0.25f, 0.98f),
        new Color(0.45f, 0.65f, 0.95f, 0.98f),
        new Color(0.75f, 0.40f, 0.65f, 0.98f),
    };

    public static event Action GlyphChanged;

    public static int GetSelectedIndex()
    {
        return Mathf.Clamp(PlayerPrefs.GetInt(PrefsKey, DefaultGlyphIndex), 0, Glyphs.Length - 1);
    }

    public static string GetSelectedGlyph()
    {
        return Glyphs[GetSelectedIndex()];
    }

    public static Color GetAccentColorForIndex(int index)
    {
        if (AccentColors.Length == 0)
            return RuntimeUiPolish.AccentCoral;
        index = Mathf.Clamp(index, 0, AccentColors.Length - 1);
        return AccentColors[index % AccentColors.Length];
    }

    public static Color GetAccentForCurrentSelection()
    {
        return GetAccentColorForIndex(GetSelectedIndex());
    }

    public static void SetSelectedIndex(int index)
    {
        index = Mathf.Clamp(index, 0, Glyphs.Length - 1);
        int prev = PlayerPrefs.GetInt(PrefsKey, DefaultGlyphIndex);
        PlayerPrefs.SetInt(PrefsKey, index);
        PlayerPrefs.Save();
        if (prev != index)
            GlyphChanged?.Invoke();
    }
}
