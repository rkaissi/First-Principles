using TMPro;
using UnityEngine;

// -----------------------------------------------------------------------------
// SceneCreditsFooter — keep in sync with Menu.unity <c>MenuCreditsBlock</c> (two lines).
// -----------------------------------------------------------------------------
// Level select + Game HUD use <see cref="BuildCompactRichText"/>; Menu uses the same
// strings via <c>menu.home_footer</c> in <see cref="LocalizationManager"/>.
// -----------------------------------------------------------------------------

/// <summary>Short home / footer copy (two lines). Overridden by <c>menu.home_footer</c>.</summary>
public static class SceneCreditsFooter
{
    /// <summary>Rich text: title + version, then collaboration line (matches main menu intent).</summary>
    public const string HomeFooterDefault =
        "<b>First Principles</b> <color=#555555>(version 1.0)</color>\n\n" +
        "<size=34>John Seong (Orch Aerospace) × GameGenesis (Rayan Kaissi)</size>";

    /// <summary>Compact block for bottom strips (level select, in-game).</summary>
    public static string BuildCompactRichText()
    {
        string body = LocalizationManager.Get("menu.home_footer", HomeFooterDefault);
        return "<align=center>" + body + "</align>";
    }

    public static void CopyFontIfPossible(TextMeshProUGUI target)
    {
        if (target == null)
            return;

        var any = Object.FindAnyObjectByType<TextMeshProUGUI>();
        if (any != null && any != target && any.font != null)
            target.font = any.font;
        else if (TMP_Settings.defaultFontAsset != null)
            target.font = TMP_Settings.defaultFontAsset;
    }
}
