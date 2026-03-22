using UnityEngine;
using UnityEngine.UI;

// -----------------------------------------------------------------------------
// WindTunnelBackdrop — procedural wind-tunnel / schlieren mood behind the graph
// -----------------------------------------------------------------------------
// Shown on all aerospace category stages via FunctionPlotter.showWindTunnelBackdrop.
// GPU-only; no CPU texture.
// -----------------------------------------------------------------------------

[DisallowMultipleComponent]
[RequireComponent(typeof(RawImage))]
public sealed class WindTunnelBackdrop : MonoBehaviour
{
    public const string ChildName = "_WindTunnelBackdrop";

    static readonly int IdAspect = Shader.PropertyToID("_Aspect");
    static readonly int IdFlowSpeed = Shader.PropertyToID("_FlowSpeed");
    static readonly int IdStreakScale = Shader.PropertyToID("_StreakScale");
    static readonly int IdColor = Shader.PropertyToID("_Color");

    static readonly Color WindTint = new Color(0.9f, 0.96f, 1f, 0.54f);

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

    public static void Sync(RectTransform gridPanel, FunctionPlotter fp)
    {
        if (gridPanel == null || fp == null)
            return;

        Transform existing = gridPanel.Find(ChildName);
        if (!fp.showWindTunnelBackdrop)
        {
            if (existing != null)
                existing.gameObject.SetActive(false);
            return;
        }

        GameObject go;
        if (existing == null)
        {
            go = new GameObject(ChildName, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(WindTunnelBackdrop));
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

        var backdrop = go.GetComponent<WindTunnelBackdrop>();
        if (backdrop == null)
            backdrop = go.AddComponent<WindTunnelBackdrop>();
        backdrop.Drive(fp);
    }

    void EnsureMaterial()
    {
        if (_mat != null)
            return;

        Shader sh = Resources.Load<Shader>("UI_WindTunnelBackdrop");
        if (sh == null)
            sh = Shader.Find("UI/WindTunnelBackdrop");
        if (sh == null)
        {
            Debug.LogWarning("WindTunnelBackdrop: shader UI_WindTunnelBackdrop not found (place under Resources).");
            return;
        }

        _mat = new Material(sh);
        _mat.SetColor(IdColor, WindTint);
        _raw.material = _mat;
    }

    public void Drive(FunctionPlotter fp)
    {
        EnsureMaterial();
        if (_mat == null)
            return;

        var rt = (RectTransform)transform;
        if (rt.anchorMin != Vector2.zero || rt.anchorMax != Vector2.one)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        float aspect = rt.rect.height > 1e-3f ? Mathf.Max(0.2f, rt.rect.width / rt.rect.height) : 1.7f;
        _mat.SetFloat(IdAspect, aspect);
        _mat.SetFloat(IdFlowSpeed, 0.42f + Mathf.Clamp01(fp.transK) * 0.35f);
        _mat.SetFloat(IdStreakScale, 7.5f + Mathf.Abs(fp.transA) * 1.2f);
        _mat.SetColor(IdColor, WindTint);
    }
}
