using UnityEngine;
using UnityEngine.UI;

// -----------------------------------------------------------------------------
// BlackHoleGargantuaBackdrop — Schwarzschild photon paths on the GPU (boss stage)
// -----------------------------------------------------------------------------
// Fragment shader integrates u'' + u = 3 M u² (equatorial null geodesics) with
// conserved impact parameter b; procedural accretion disk + photon ring for a
// Gargantua-like mood. Sync hides the layer off-stage to avoid cost.
// -----------------------------------------------------------------------------

/// <summary>
/// Runtime child under the graph <see cref="Grid"/>; toggled from <see cref="FunctionPlotter"/>.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RawImage))]
public sealed class BlackHoleGargantuaBackdrop : MonoBehaviour
{
    public const string ChildName = "_BlackHoleGargantuaBackdrop";

    static readonly int IdTimeLive = Shader.PropertyToID("_TimeLive");
    static readonly int IdAspect = Shader.PropertyToID("_Aspect");

    RawImage _raw;
    Material _mat;
    int _paramHash = int.MinValue;

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

    /// <summary>Creates / hides the child on <paramref name="gridPanel"/>; drives shader params when visible.</summary>
    public static void Sync(RectTransform gridPanel, FunctionPlotter fp)
    {
        if (gridPanel == null || fp == null)
            return;

        Transform existing = gridPanel.Find(ChildName);
        if (fp.functionType != FunctionType.PhysicsGravityWellInverseSqrt)
        {
            if (existing != null)
                existing.gameObject.SetActive(false);
            return;
        }

        GameObject go;
        if (existing == null)
        {
            go = new GameObject(ChildName, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(BlackHoleGargantuaBackdrop));
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

        var backdrop = go.GetComponent<BlackHoleGargantuaBackdrop>();
        if (backdrop == null)
            backdrop = go.AddComponent<BlackHoleGargantuaBackdrop>();
        backdrop.Drive(fp);
    }

    void EnsureMaterial()
    {
        if (_mat != null)
            return;

        Shader sh = Resources.Load<Shader>("UI_BlackHoleGargantuaBackdrop");
        if (sh == null)
            sh = Shader.Find("UI/BlackHoleGargantuaBackdrop");
        if (sh == null)
        {
            Debug.LogWarning("BlackHoleGargantuaBackdrop: shader UI_BlackHoleGargantuaBackdrop not found (place under Resources).");
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

        Rect r = rtDrive.rect;
        float aspect = r.height > 1e-3f ? Mathf.Max(0.2f, r.width / r.height) : 1.7f;

        // Map level softening |D| → shader mass scale (gameplay uses softened 1/r well).
        float mass = Mathf.Lerp(0.75f, 1.35f, Mathf.Clamp01(Mathf.Abs(fp.transD) / 0.5f));
        float bMin = 3.1f * mass;
        float bMax = 13.5f * mass + Mathf.Abs(fp.transA) * 0.08f;

        int h;
        unchecked
        {
            h = 17;
            h = h * 31 + fp.transA.GetHashCode();
            h = h * 31 + fp.transD.GetHashCode();
            h = h * 31 + aspect.GetHashCode();
            h = h * 31 + mass.GetHashCode();
        }

        _mat.SetFloat(IdAspect, aspect);
        _mat.SetFloat("_Mass", mass);
        _mat.SetFloat("_BMin", bMin);
        _mat.SetFloat("_BMax", bMax);
        _mat.SetFloat("_CamDist", 14f + Mathf.Clamp(fp.transA, 0f, 8f) * 0.12f);

        if (h != _paramHash)
        {
            _paramHash = h;
            _mat.SetFloat("_DiskBright", 2.2f + Mathf.Clamp01(fp.transK - 0.5f) * 0.6f);
        }
    }
}
