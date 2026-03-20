using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Level select only: infinite gentle “rain” of math symbols (+, ×, ÷, =, …) behind the list UI.
/// </summary>
[DisallowMultipleComponent]
public class LevelSelectFallingSymbolsBackdrop : MonoBehaviour
{
    private sealed class GlyphEntry
    {
        public RectTransform Rt;
        public TextMeshProUGUI Tmp;
        public int Id;
        public float Speed;
        public float Drift;
        public float SpinDegPerSec;
        public float ZRot;
    }

    [SerializeField] private int glyphCount = 60;

    private static readonly string[] SymbolPool =
    {
        "+", "−", "×", "÷", "=", "x", "y", "π", "∑", "∫", "√", "±", "%", "∞", "Δ", "θ", "·", "/", "(", ")", "[", "]",
    };

    private readonly List<GlyphEntry> _glyphs = new List<GlyphEntry>();
    private RectTransform _rt;
    private bool _built;

    private void Awake()
    {
        _rt = transform as RectTransform;
    }

    private void Start()
    {
        StartCoroutine(BuildWhenLayoutReady());
    }

    private IEnumerator BuildWhenLayoutReady()
    {
        yield return null;
        yield return new WaitForEndOfFrame();
        for (int i = 0; i < 6 && (_rt.rect.width < 8f || _rt.rect.height < 8f); i++)
            yield return null;

        if (_built)
            yield break;
        BuildGlyphs();
        _built = true;
    }

    private void Update()
    {
        if (!_built || _glyphs.Count == 0 || _rt == null)
            return;

        float w = Mathf.Max(16f, _rt.rect.width);
        float h = Mathf.Max(16f, _rt.rect.height);
        float dt = Time.deltaTime;

        foreach (var g in _glyphs)
        {
            var p = g.Rt.anchoredPosition;
            p.x += g.Drift * dt;
            p.y -= g.Speed * dt;
            g.ZRot += g.SpinDegPerSec * dt;
            g.Rt.localRotation = Quaternion.Euler(0f, 0f, g.ZRot);

            if (p.y < -h * 0.58f)
                p = RandomRespawnTop(g, w, h);

            g.Rt.anchoredPosition = p;
        }
    }

    private void BuildGlyphs()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);
        _glyphs.Clear();

        float w = Mathf.Max(16f, _rt.rect.width);
        float h = Mathf.Max(16f, _rt.rect.height);
        int n = Mathf.Clamp(glyphCount, 24, 120);

        for (int i = 0; i < n; i++)
        {
            string ch = SymbolPool[i % SymbolPool.Length];
            var go = new GameObject($"FallingGlyph_{i}");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(_rt, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(96f, 96f);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = ch;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = UiTypography.Scale(18);
            tmp.fontSizeMax = UiTypography.Scale(46);
            tmp.fontSize = UiTypography.Scale(32);
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.raycastTarget = false;
            tmp.richText = false;

            float lum = 0.7f + Mathf.PerlinNoise(i * 0.11f, 0.4f) * 0.3f;
            tmp.color = new Color(lum * 0.55f, lum * 0.62f, 0.92f, 0.08f + Mathf.PerlinNoise(0.3f, i * 0.09f) * 0.17f);

            CopyFont(tmp);

            Random.InitState(91011 + i * 997);
            var entry = new GlyphEntry
            {
                Rt = rt,
                Tmp = tmp,
                Speed = Random.Range(62f, 155f),
                Drift = Random.Range(-18f, 18f),
                SpinDegPerSec = Random.Range(-22f, 22f),
                ZRot = Random.Range(0f, 360f),
            };
            rt.anchoredPosition = RandomStart(i, w, h);
            _glyphs.Add(entry);
        }
    }

    private static Vector2 RandomStart(int index, float w, float h)
    {
        Random.InitState(77077 + index * 337);
        float x = (Random.value - 0.5f) * w * 0.94f;
        float y = Random.Range(-h * 0.05f, h * 0.55f);
        return new Vector2(x, y);
    }

    private static Vector2 RandomRespawnTop(GlyphEntry g, float w, float h)
    {
        Random.InitState(1299827 + g.Id * 1103 + (Mathf.FloorToInt(Time.unscaledTime * 60f) << 3));
        float x = (Random.value - 0.5f) * w * 0.94f;
        float y = Random.Range(h * 0.52f, h * 0.62f);
        g.Speed = Random.Range(62f, 155f);
        g.Drift = Random.Range(-18f, 18f);
        g.SpinDegPerSec = Random.Range(-22f, 22f);
        return new Vector2(x, y);
    }

    private static void CopyFont(TextMeshProUGUI target)
    {
        UiTypography.ApplyDefaultFontAsset(target);
        if (target.font != null)
            return;
        var any = FindAnyObjectByType<TextMeshProUGUI>();
        if (any != null && any != target && any.font != null)
        {
            target.font = any.font;
            if (any.fontSharedMaterial != null)
                target.fontSharedMaterial = any.fontSharedMaterial;
        }
    }
}
