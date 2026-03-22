using UnityEngine;
using UnityEngine.UI;

// -----------------------------------------------------------------------------
// GoldenSpiralBackdrop — logarithmic φ-spiral field behind the graph (boss stage)
// -----------------------------------------------------------------------------
// Shown when FunctionType is PolarGoldenLogSpiral; GPU-only, matches stage theme.
// -----------------------------------------------------------------------------

[DisallowMultipleComponent]
[RequireComponent(typeof(RawImage))]
public sealed class GoldenSpiralBackdrop : MonoBehaviour
{
    public const string ChildName = "_GoldenSpiralBackdrop";

    static readonly int IdTimeLive = Shader.PropertyToID("_TimeLive");
    static readonly int IdAspect = Shader.PropertyToID("_Aspect");
    static readonly int IdColor = Shader.PropertyToID("_Color");

    static readonly Color GoldenTint = new Color(1f, 0.88f, 0.52f, 0.5f);

    RawImage _raw;
    Material _mat;

    void Awake()
    {
        _raw = GetComponent<RawImage>();
        _raw.raycastTarget = false;
    }

    void OnDestroy()
    {
        if (_mat != null)
            Destroy(_mat);
    }

    void Update()
    {
        if (_mat == null || !_raw.gameObject.activeInHierarchy)
            return;
        _mat.SetFloat(IdTimeLive, Time.time);
    }

    public static void Sync(RectTransform gridPanel, FunctionPlotter fp)
    {
        if (gridPanel == null || fp == null)
            return;

        Transform existing = gridPanel.Find(ChildName);
        if (fp.functionType != FunctionType.PolarGoldenLogSpiral)
        {
            if (existing != null)
                existing.gameObject.SetActive(false);
            return;
        }

        GameObject go;
        if (existing == null)
        {
            go = new GameObject(ChildName, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(GoldenSpiralBackdrop));
            go.transform.SetParent(gridPanel, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var raw = go.GetComponent<RawImage>();
            raw.raycastTarget = false;
            raw.texture = Texture2D.whiteTexture;
            raw.color = Color.white;
        }
        else
            go = existing.gameObject;

        go.SetActive(true);
        go.transform.SetAsFirstSibling();

        var backdrop = go.GetComponent<GoldenSpiralBackdrop>();
        if (backdrop == null)
            backdrop = go.AddComponent<GoldenSpiralBackdrop>();
        backdrop.Drive(fp);
    }

    void EnsureMaterial()
    {
        if (_mat != null)
            return;

        Shader sh = Resources.Load<Shader>("UI_GoldenSpiralBackdrop");
        if (sh == null)
            sh = Shader.Find("UI/GoldenSpiralBackdrop");
        if (sh == null)
        {
            Debug.LogWarning("GoldenSpiralBackdrop: shader UI_GoldenSpiralBackdrop not found (place under Resources).");
            return;
        }

        _mat = new Material(sh);
        _raw.material = _mat;
    }

    public void Drive(FunctionPlotter fp)
    {
        EnsureMaterial();
        if (_mat == null)
            return;

        var rtDrive = (RectTransform)transform;
        if (rtDrive.anchorMin != Vector2.zero || rtDrive.anchorMax != Vector2.one)
        {
            rtDrive.anchorMin = Vector2.zero;
            rtDrive.anchorMax = Vector2.one;
            rtDrive.offsetMin = Vector2.zero;
            rtDrive.offsetMax = Vector2.zero;
        }

        float aspect = rtDrive.rect.height > 1e-3f ? Mathf.Max(0.2f, rtDrive.rect.width / rtDrive.rect.height) : 1.7f;
        _mat.SetFloat(IdAspect, aspect);
        _mat.SetFloat(IdTimeLive, Time.time);
        _mat.SetColor(IdColor, GoldenTint);
    }
}
