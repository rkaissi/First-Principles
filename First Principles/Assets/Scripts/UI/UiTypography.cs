using System;
using TMPro;
using UnityEngine;

/// <summary>
/// Single knob for **global UI text size**. All runtime TMP sizes should go through
/// <see cref="Scale"/> so menus, HUD, overlays stay consistent when this changes.
/// </summary>
public static class UiTypography
{
    /// <summary>Multiply script-driven font sizes by this (1.15 ≈ +15%).</summary>
    public const float GlobalScale = 1.15f;

    public static int Scale(int px) => Mathf.Max(1, Mathf.RoundToInt(px * GlobalScale));

    public static float Scale(float px) => Mathf.Max(1f, Mathf.Round(px * GlobalScale));

    /// <summary>Same asset referenced by Menu/Game scenes; must live under a <c>Resources</c> folder for <see cref="Resources.Load"/>.</summary>
    const string ProjectPrimaryTmpFontResourcePath = "Fonts/Nunito-VariableFont_wght SDF";

    /// <summary>
    /// Picks a TMP font for runtime-built UI (level select, overlays, etc.): uses <see cref="TMP_Settings.defaultFontAsset"/>
    /// when you have replaced the stock Liberation default (e.g. Quicksand menu command); otherwise loads
    /// <b>Resources/<see cref="ProjectPrimaryTmpFontResourcePath"/></b> so level select matches Menu/Game (Nunito in this repo).
    /// </summary>
    public static void ApplyDefaultFontAsset(TextMeshProUGUI target)
    {
        if (target == null)
            return;
        TMP_FontAsset settingsFont = TMP_Settings.defaultFontAsset;
        TMP_FontAsset fromResources = Resources.Load<TMP_FontAsset>(ProjectPrimaryTmpFontResourcePath);

        TMP_FontAsset font = fromResources;
        if (settingsFont != null && !IsLikelyStockLiberationSans(settingsFont))
            font = settingsFont;
        if (font == null)
            font = settingsFont;
        if (font == null)
            return;

        target.font = font;
        if (font.material != null)
            target.fontSharedMaterial = font.material;
    }

    static bool IsLikelyStockLiberationSans(TMP_FontAsset f) =>
        f != null && f.name != null &&
        f.name.IndexOf("Liberation", StringComparison.OrdinalIgnoreCase) >= 0;
}
