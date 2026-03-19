using UnityEngine;
using UnityEngine.UI;

// -----------------------------------------------------------------------------
// RuntimeUiPolish — procedurally generated rounded UI sprites + shared styling
// -----------------------------------------------------------------------------
// No Asset Store download required. For higher-end art, see docs/optional-unity-assets.md
// -----------------------------------------------------------------------------

/// <summary>Cached rounded-rect & soft-circle sprites (white RGBA) for uGUI <see cref="Image"/> tinting.</summary>
public static class RuntimeUiPolish
{
    private const int RoundTexSize = 64;
    private const int CornerRadius = 11;

    private static Sprite _rounded9Slice;
    private static Sprite _softCircle;

    public static Sprite Rounded9Slice
    {
        get
        {
            if (_rounded9Slice == null)
                _rounded9Slice = BuildRoundedRectSprite(RoundTexSize, RoundTexSize, CornerRadius);
            return _rounded9Slice;
        }
    }

    /// <summary>Soft-edged disk for the graph “character” (tint with <see cref="Image.color"/>).</summary>
    public static Sprite SoftCharacterBlob
    {
        get
        {
            if (_softCircle == null)
                _softCircle = BuildRadialFalloffSprite(48);
            return _softCircle;
        }
    }

    // --- Palette (cool graphite + coral accents) --------------------------------
    public static readonly Color PanelDeep = new Color(0.09f, 0.10f, 0.14f, 0.97f);
    public static readonly Color PanelMid = new Color(0.14f, 0.16f, 0.22f, 0.98f);
    public static readonly Color ButtonNeutral = new Color(0.20f, 0.23f, 0.30f, 0.95f);
    public static readonly Color ButtonNeutralHover = new Color(0.27f, 0.31f, 0.42f, 1f);
    public static readonly Color AccentTeal = new Color(0.18f, 0.42f, 0.48f, 0.98f);
    public static readonly Color AccentCoral = new Color(0.95f, 0.42f, 0.38f, 0.98f);
    public static readonly Color AccentJump = new Color(0.22f, 0.48f, 0.42f, 0.94f);
    public static readonly Color TitleIvory = new Color(0.94f, 0.95f, 0.98f, 1f);
    public static readonly Color PlayerBody = new Color(1f, 0.62f, 0.48f, 1f);

    public static void ApplyDropShadow(RectTransform target, Vector2? distance = null, float alpha = 0.28f)
    {
        if (target == null)
            return;

        var existing = target.gameObject.GetComponent<Shadow>();
        if (existing == null)
            existing = target.gameObject.AddComponent<Shadow>();

        var d = distance ?? new Vector2(2f, -3f);
        existing.effectDistance = d;
        existing.effectColor = new Color(0f, 0f, 0f, alpha);
        existing.useGraphicAlpha = true;
    }

    public static void ApplyButtonTransitions(Button button, Color baseColor, Color highlighted, Color pressed)
    {
        if (button == null)
            return;

        var c = button.colors;
        c.fadeDuration = 0.12f;
        c.normalColor = baseColor;
        c.highlightedColor = highlighted;
        c.pressedColor = Color.Lerp(pressed, Color.black, 0.15f);
        c.selectedColor = highlighted;
        c.disabledColor = new Color(0.35f, 0.35f, 0.38f, 0.55f);
        button.colors = c;
    }

    public static void UseRoundedSliced(Image img)
    {
        if (img == null)
            return;

        var s = Rounded9Slice;
        if (s == null)
            return;

        img.sprite = s;
        img.type = Image.Type.Sliced;
        img.preserveAspect = false;
    }

    public static bool ShouldUseSlicedForSize(float pxWidth, float pxHeight)
    {
        return pxWidth >= 28f && pxHeight >= 20f;
    }

    private static Sprite BuildRoundedRectSprite(int w, int h, int r)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float a = RoundedBoxAlphaSmooth(x, y, w, h, r);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }

        tex.Apply();
        var border = new Vector4(r, r, r, r);
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
    }

    private static float RoundedBoxAlphaSmooth(int x, int y, int w, int h, int r)
    {
        float feather = 1.15f;
        // Interior cross
        if (x >= r && x < w - r)
            return 1f;
        if (y >= r && y < h - r)
            return 1f;

        Vector2 c;
        if (x < r && y < r)
            c = new Vector2(r, r);
        else if (x >= w - r && y < r)
            c = new Vector2(w - 1 - r, r);
        else if (x < r && y >= h - r)
            c = new Vector2(r, h - 1 - r);
        else
            c = new Vector2(w - 1 - r, h - 1 - r);

        float d = Vector2.Distance(new Vector2(x, y), c) - r;
        return Mathf.Clamp01(0.5f - d / feather);
    }

    private static Sprite BuildRadialFalloffSprite(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float cx = (size - 1) * 0.5f;
        float cy = (size - 1) * 0.5f;
        float rad = size * 0.42f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                float a = Mathf.Clamp01(1f - (d - rad + 2f) / 4f);
                a = Mathf.Pow(a, 1.4f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
