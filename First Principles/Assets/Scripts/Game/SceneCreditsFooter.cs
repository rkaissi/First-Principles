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
    public const string ProprietaryLine = "© 2022-2026 · Proprietary · All rights reserved · First Principles";

    public const string AttributionLine =
        "<b><link=\"https://game-genesis.itch.io\"><color=#8b9dc9>GAME GENESIS</color></link></b> (<link=\"https://github.com/rkaissi/\"><color=#8b9dc9>Rayan Kaissi</color></link>) × " +
        "<b><link=\"https://orchaerospace.com\"><color=#8b9dc9>ORCH AEROSPACE</color></link></b> (<link=\"https://github.com/wonmor/\"><color=#8b9dc9>John Wonmo Seong</color></link>)";

    public const string SchoolPrideLine =
        "Proud graduates of <b>Garth Webb Secondary School</b>, Oakville.";

    /// <summary>Encourages support — keep tone grateful, not pushy (store / tip jars / wishlists).</summary>
    public const string SupportLine =
        "Four years of development — if you value this work, please support the project. Thank you.";

    public const string UnityLine = "Made with <b>Unity</b>. Unity is a trademark of Unity Technologies.";

    /// <summary>Compact block for small bottom strips (rich text).</summary>
    public static string BuildCompactRichText()
    {
        string p = LocalizationManager.Get("footer.proprietary", ProprietaryLine);
        string a = LocalizationManager.Get("footer.attribution", AttributionLine);
        string s = LocalizationManager.Get("footer.school", SchoolPrideLine);
        string u = LocalizationManager.Get("footer.support", SupportLine);
        string unity = LocalizationManager.Get("footer.unity", UnityLine);
        return
            "<align=center>" +
            $"<size=90%><color=#aaaaaa>{p}</color></size>\n" +
            $"<size=82%><color=#888899>{a}</color></size>\n" +
            $"<size=74%><color=#7a8498><i>{s}</i></color></size>\n" +
            $"<size=76%><color=#6f7d90><i>{u}</i></color></size>\n" +
            $"<size=78%><color=#6a6f80>{unity}</color></size>" +
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
