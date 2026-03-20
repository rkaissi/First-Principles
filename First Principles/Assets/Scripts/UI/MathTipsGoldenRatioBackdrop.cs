using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// <see cref="MathArticlesOverlay"/> only: soft drifting φ / Fibonacci hints in <b>gold</b> tones (teaching mood, not clutter).
/// </summary>
[DisallowMultipleComponent]
public class MathTipsGoldenRatioBackdrop : MonoBehaviour
{
    private sealed class GlyphEntry
    {
        public RectTransform Rt;
        public int Id;
        public float Speed;
        public float Drift;
        public float SpinDegPerSec;
        public float ZRot;
    }

    [SerializeField] private int glyphCount = 52;

    private static readonly string[] SymbolPool =
    {
        "φ", "Φ", "≈", "·", "+",
        "1", "2", "3", "5", "8", "13", "21", "34", "55",
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

            if (p.y < -h * 0.55f)
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
        int n = Mathf.Clamp(glyphCount, 20, 96);

        for (int i = 0; i < n; i++)
        {
            string ch = SymbolPool[i % SymbolPool.Length];
            var go = new GameObject($"GoldenGlyph_{i}");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(_rt, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(120f, 96f);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = ch;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = UiTypography.Scale(14);
            tmp.fontSizeMax = UiTypography.Scale(40);
            tmp.fontSize = UiTypography.Scale(26);
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.raycastTarget = false;
            tmp.richText = false;

            float gold = 0.55f + Mathf.PerlinNoise(i * 0.13f, 0.37f) * 0.45f;
            tmp.color = new Color(
                Mathf.Lerp(0.88f, 1f, gold),
                Mathf.Lerp(0.62f, 0.88f, gold),
                Mathf.Lerp(0.28f, 0.52f, gold),
                0.06f + Mathf.PerlinNoise(0.21f, i * 0.11f) * 0.12f);

            CopyFont(tmp);

            Random.InitState(41414 + i * 911);
            var entry = new GlyphEntry
            {
                Rt = rt,
                Id = i,
                Speed = Random.Range(38f, 108f),
                Drift = Random.Range(-14f, 14f),
                SpinDegPerSec = Random.Range(-14f, 14f),
                ZRot = Random.Range(0f, 360f),
            };
            rt.anchoredPosition = RandomStart(i, w, h);
            _glyphs.Add(entry);
        }
    }

    private static Vector2 RandomStart(int index, float w, float h)
    {
        Random.InitState(88022 + index * 419);
        float x = (Random.value - 0.5f) * w * 0.96f;
        float y = Random.Range(-h * 0.08f, h * 0.58f);
        return new Vector2(x, y);
    }

    private static Vector2 RandomRespawnTop(GlyphEntry g, float w, float h)
    {
        Random.InitState(220011 + g.Id * 1319 + (Mathf.FloorToInt(Time.unscaledTime * 60f) << 2));
        float x = (Random.value - 0.5f) * w * 0.96f;
        float y = Random.Range(h * 0.5f, h * 0.62f);
        g.Speed = Random.Range(38f, 108f);
        g.Drift = Random.Range(-14f, 14f);
        g.SpinDegPerSec = Random.Range(-14f, 14f);
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
