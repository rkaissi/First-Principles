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

    /// <summary>
    /// Assigns <see cref="TMP_Settings.defaultFontAsset"/> (e.g. Quicksand after font setup) so runtime-built UI
    /// matches the project TMP default instead of whatever happens to be first in the scene.
    /// </summary>
    public static void ApplyDefaultFontAsset(TextMeshProUGUI target)
    {
        if (target == null)
            return;
        var font = TMP_Settings.defaultFontAsset;
        if (font == null)
            return;
        target.font = font;
        if (font.material != null)
            target.fontSharedMaterial = font.material;
    }
}
