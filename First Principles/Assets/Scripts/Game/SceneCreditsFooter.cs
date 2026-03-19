using TMPro;
using UnityEngine;

// -----------------------------------------------------------------------------
// SceneCreditsFooter — same legal/credits lines as Menu.unity (keep in sync)
// -----------------------------------------------------------------------------
// Level select + Game HUD build UI at runtime; this centralizes copy so all scenes agree.
// -----------------------------------------------------------------------------

/// <summary>Footer rich text shown on level select and in-game (matches <c>Menu</c> scene).</summary>
public static class SceneCreditsFooter
{
    public const string ProprietaryLine = "Proprietary · All rights reserved · College Math For Toddlers";

    public const string AttributionLine =
        "<b>GAME GENESIS</b> (Rayan Kaissi) × <b>ORCH AEROSPACE</b> (John Wonmo Seong)";

    public const string UnityLine = "Made with <b>Unity</b>. Unity is a trademark of Unity Technologies.";

    /// <summary>Compact block for small bottom strips (rich text).</summary>
    public static string BuildCompactRichText()
    {
        return
            "<align=center>" +
            $"<size=90%><color=#aaaaaa>{ProprietaryLine}</color></size>\n" +
            $"<size=82%><color=#888899>{AttributionLine}</color></size>\n" +
            $"<size=78%><color=#6a6f80>{UnityLine}</color></size>" +
            "</align>";
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
