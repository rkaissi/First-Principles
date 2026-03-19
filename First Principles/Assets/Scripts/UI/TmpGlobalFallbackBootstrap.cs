using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

// -----------------------------------------------------------------------------
// TmpGlobalFallbackBootstrap — multilingual glyphs for TextMesh Pro
// -----------------------------------------------------------------------------
// Default LiberationSans SDF only covers a small Unicode range. Localized UI in
// Arabic, Chinese, Japanese, Korean, etc. needs wider coverage. At subsystem init
// we build dynamic SDF font assets from bundled Noto TTFs (OFL) under
// Resources/Fonts and append them to the default TMP font + global TMP fallbacks.
// -----------------------------------------------------------------------------

/// <summary>
/// Loads Noto font resources and registers them as TMP fallbacks before scenes run.
/// </summary>
public static class TmpGlobalFallbackBootstrap
{
    static bool _installed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnDomainReload() => _installed = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        if (_installed)
            return;
        _installed = true;

        // Order: Latin+extensions, **Arabic** (critical for ar locale — Noto Sans TTF alone often lacks full Arabic coverage in TMP dynamic atlases),
        // Devanagari (Hindi), Nastaliq (Urdu), then CJK.
        var extras = new List<TMP_FontAsset>();
        TryAddFont("Fonts/NotoSans-Regular", extras);
        TryAddFont("Fonts/NotoSansArabic-Regular", extras);
        TryAddFont("Fonts/NotoSansDevanagari-Regular", extras);
        TryAddFont("Fonts/NotoNastaliqUrdu-Regular", extras);
        TryAddFont("Fonts/NotoSansSC-Regular", extras);
        TryAddFont("Fonts/NotoSansKR-Regular", extras);
        TryAddFont("Fonts/NotoSansJP-Regular", extras);

        if (extras.Count == 0)
            return;

        AppendFallbacks(TMP_Settings.defaultFontAsset, extras);
        // Many scene objects reference the stock Liberation SDF assets directly (not the TMP default).
        AppendFallbacks(Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF"), extras);
        AppendFallbacks(Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF - Fallback"), extras);
    }

    static void AppendFallbacks(TMP_FontAsset asset, List<TMP_FontAsset> extras)
    {
        if (asset == null || extras == null || extras.Count == 0)
            return;
        if (asset.fallbackFontAssetTable == null)
            asset.fallbackFontAssetTable = new List<TMP_FontAsset>();
        foreach (var f in extras)
        {
            if (f != null && !asset.fallbackFontAssetTable.Contains(f))
                asset.fallbackFontAssetTable.Add(f);
        }
    }

    static void TryAddFont(string resourcePathNoExt, List<TMP_FontAsset> sink)
    {
        var unityFont = Resources.Load<Font>(resourcePathNoExt);
        if (unityFont == null)
            return;

        try
        {
            var tmp = TMP_FontAsset.CreateFontAsset(
                unityFont,
                90,
                9,
                GlyphRenderMode.SDFAA,
                2048,
                2048,
                AtlasPopulationMode.Dynamic,
                enableMultiAtlasSupport: true);

            if (tmp == null)
                return;

            tmp.name = $"[Runtime] {unityFont.name}";
            sink.Add(tmp);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[TMP] Could not create fallback font from '{resourcePathNoExt}': {e.Message}");
        }
    }
}
