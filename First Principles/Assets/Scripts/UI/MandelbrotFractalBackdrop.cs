using System;
using UnityEngine;
using UnityEngine.UI;

// -----------------------------------------------------------------------------
// MandelbrotFractalBackdrop — classic c-plane image behind the graph (boss level)
// -----------------------------------------------------------------------------
// The main curve is still a 1D slice (Im(c) vs smooth escape time). Players expect
// the familiar cardioid / bulbs; this RawImage draws a standard Re×Im view with a
// gold vertical line at the slice Re(c) = transA so the relationship is obvious.
// -----------------------------------------------------------------------------

/// <summary>
/// Runtime object under the graph <see cref="Grid"/>; toggled from <see cref="FunctionPlotter"/>.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RawImage))]
public sealed class MandelbrotFractalBackdrop : MonoBehaviour
{
    public const string ChildName = "_MandelbrotSetBackdrop";

    const int TexW = 400;
    const int TexH = 220;

    /// <summary>Full-set framing (matches classic textbook screenshots).</summary>
    public const float ReMin = -2.15f;
    public const float ReMax = 1.05f;
    public const float ImMin = -1.38f;
    public const float ImMax = 1.38f;

    RawImage _raw;
    Texture2D _tex;
    int _hash = int.MinValue;

    void Awake() => _raw = GetComponent<RawImage>();

    /// <summary>Creates / hides the child on <paramref name="gridPanel"/> and refreshes the texture when inputs change.</summary>
    public static void Sync(RectTransform gridPanel, FunctionPlotter fp)
    {
        if (gridPanel == null || fp == null)
            return;

        Transform existing = gridPanel.Find(ChildName);
        if (fp.functionType != FunctionType.MandelbrotEscapeImSlice)
        {
            if (existing != null)
                existing.gameObject.SetActive(false);
            return;
        }

        GameObject go;
        if (existing == null)
        {
            go = new GameObject(ChildName, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(MandelbrotFractalBackdrop));
            go.transform.SetParent(gridPanel, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var raw = go.GetComponent<RawImage>();
            raw.raycastTarget = false;
            raw.color = new Color(1f, 1f, 1f, 0.76f);
        }
        else
            go = existing.gameObject;

        go.SetActive(true);
        go.transform.SetAsFirstSibling();

        var backdrop = go.GetComponent<MandelbrotFractalBackdrop>();
        if (backdrop == null)
            backdrop = go.AddComponent<MandelbrotFractalBackdrop>();
        backdrop.Drive(fp);
    }

    public void Drive(FunctionPlotter fp)
    {
        int maxIter = Mathf.Clamp(fp.power, 24, 160);
        int h = HashCode.Combine(Mathf.RoundToInt(fp.transA * 1_000_000f), maxIter, 0x4d414e44);

        if (h == _hash && _tex != null)
            return;

        _hash = h;
        BuildTexture(fp.transA, maxIter);
    }

    void BuildTexture(float sliceCr, int maxIter)
    {
        if (_tex == null)
        {
            _tex = new Texture2D(TexW, TexH, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            _raw.texture = _tex;
        }

        var pixels = new Color32[TexW * TexH];
        int slicePx = Mathf.Clamp(Mathf.RoundToInt((sliceCr - ReMin) / (ReMax - ReMin) * (TexW - 1)), 0, TexW - 1);

        for (int j = 0; j < TexH; j++)
        {
            float ci = ImMin + j / (float)(TexH - 1) * (ImMax - ImMin);
            int row = j * TexW;
            for (int i = 0; i < TexW; i++)
            {
                float cr = ReMin + i / (float)(TexW - 1) * (ReMax - ReMin);
                float sm = MandelbrotEscapeMath.SmoothIterations(cr, ci, maxIter);
                Color col = ColorFromSmooth(sm, maxIter);
                if (Mathf.Abs(i - slicePx) <= 1)
                    col = Color.Lerp(col, new Color(1f, 0.92f, 0.25f, 0.95f), 0.52f);
                pixels[row + i] = (Color32)col;
            }
        }

        _tex.SetPixels32(pixels);
        _tex.Apply(false);
    }

    static Color ColorFromSmooth(float smoothIter, int maxIter)
    {
        if (smoothIter >= maxIter - 0.001f)
            return new Color(0.035f, 0.045f, 0.14f, 0.93f);

        float t = Mathf.Clamp01(smoothIter / maxIter);
        float hue = (t * 5.85f + 0.52f) % 1f;
        Color c = Color.HSVToRGB(hue, 0.84f, Mathf.Lerp(0.28f, 1f, t));
        c.a = 0.9f;
        return c;
    }

    void OnDestroy()
    {
        if (_tex != null)
        {
            Destroy(_tex);
            _tex = null;
        }
    }
}
