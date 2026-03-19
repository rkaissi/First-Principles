using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Graphing calculator mode: <b>Trans</b> edits transformation parameters; <b>Scale</b> zooms the window in/out.
/// Trans: short tap cycles A → k → C → D; double-tap nudges +; hold (~½s) nudges −.
/// Scale: short tap zoom in; hold zoom out. Pair with <see cref="GraphPinchZoom"/> for pinch.
/// </summary>
public class GraphCalculatorToolbar : MonoBehaviour
{
    [SerializeField] private float longPressSeconds = 0.48f;
    [SerializeField] private float doubleTapWindow = 0.38f;
    [SerializeField] private float paramStep = 0.1f;
    [SerializeField] private float kStep = 0.06f;
    [SerializeField] private float zoomTapFactor = 0.86f;

    private FunctionPlotter plot;
    private TextMeshProUGUI hint;
    private int paramIndex;
    private float transLastShortTime = -1f;

    private void OnEnable()
    {
        LocalizationManager.LanguageChanged += OnLocChanged;
    }

    private void OnDisable()
    {
        LocalizationManager.LanguageChanged -= OnLocChanged;
    }

    private void OnLocChanged() => RefreshHint();

    public void Configure(FunctionPlotter functionPlotter, Button transBtn, Button scaleBtn, TextMeshProUGUI parameterHint)
    {
        plot = functionPlotter;
        hint = parameterHint;

        if (transBtn != null)
        {
            transBtn.onClick.RemoveAllListeners();
            AttachPressSplit(transBtn.gameObject, OnTransShort, OnTransLong);
        }

        if (scaleBtn != null)
        {
            scaleBtn.onClick.RemoveAllListeners();
            AttachPressSplit(scaleBtn.gameObject, OnScaleShort, OnScaleLong);
        }

        RefreshHint();
    }

    private void AttachPressSplit(GameObject go, System.Action shortRel, System.Action longRel)
    {
        var h = go.GetComponent<UiShortLongPress>();
        if (h == null)
            h = go.AddComponent<UiShortLongPress>();
        h.longPressThreshold = longPressSeconds;
        h.onShortRelease = shortRel;
        h.onLongRelease = longRel;
    }

    private void OnTransShort()
    {
        if (plot == null)
            return;

        if (transLastShortTime > 0f && Time.time - transLastShortTime < doubleTapWindow)
        {
            NudgeParam(+1);
            transLastShortTime = -1f;
            return;
        }

        transLastShortTime = Time.time;
        paramIndex = (paramIndex + 1) % 4;
        RefreshHint();
    }

    private void OnTransLong()
    {
        if (plot == null)
            return;
        NudgeParam(-1);
    }

    private void NudgeParam(int sign)
    {
        float s = sign > 0 ? 1f : -1f;
        switch (paramIndex)
        {
            case 0:
                plot.transA = ClampGeneric(plot.transA + s * paramStep);
                break;
            case 1:
                plot.transK = Mathf.Clamp(plot.transK + s * kStep, 0.02f, 6f);
                break;
            case 2:
                plot.transC = ClampGeneric(plot.transC + s * paramStep);
                break;
            case 3:
                plot.transD = ClampGeneric(plot.transD + s * paramStep);
                break;
        }

        RefreshHint();
    }

    private static float ClampGeneric(float v)
    {
        return Mathf.Clamp(v, -12f, 12f);
    }

    private void OnScaleShort() => ApplyZoomWindow(zoomTapFactor);

    private void OnScaleLong() => ApplyZoomWindow(1f / zoomTapFactor);

    private void ApplyZoomWindow(float halfWidthMultiplier)
    {
        if (plot == null)
            return;
        float mid = (plot.xStart + plot.xEnd) * 0.5f;
        float half = (plot.xEnd - plot.xStart) * 0.5f * halfWidthMultiplier;
        half = Mathf.Clamp(half, 0.35f, 160f);
        plot.xStart = mid - half;
        plot.xEnd = mid + half;
        plot.step = Mathf.Clamp((plot.xEnd - plot.xStart) / 480f, 0.004f, 0.42f);
    }

    private void RefreshHint()
    {
        if (hint == null || plot == null)
            return;

        string[] names =
        {
            LocalizationManager.Get("graph.param_a", "A (vertical scale)"),
            LocalizationManager.Get("graph.param_k", "k (horizontal scale)"),
            LocalizationManager.Get("graph.param_c", "C (vertical shift)"),
            LocalizationManager.Get("graph.param_d", "D (horizontal shift)")
        };
        string line1 = LocalizationManager.Get("graph.line1", "<b>Graphing calculator mode</b>");
        string line2 = LocalizationManager.Get("graph.line2",
            "<size=88%><color=#c4d0e8>Type <b>f(u)</b> below (variable <b>x</b> in the box = inner u). Then:</color></size>");
        string line3Fmt = LocalizationManager.Get("graph.line3",
            "<size=92%><b>Trans</b> → {0} · double-tap <b>+</b> · hold <b>−</b> · <b>Scale</b> zoom in / hold zoom out · two-finger <b>pinch</b></size>");
        string line3 = string.Format(line3Fmt, names[paramIndex]);
        string line4Fmt = LocalizationManager.Get("graph.line4",
            "<size=88%><color=#a8b2d1>A={0} &nbsp;k={1} &nbsp;C={2} &nbsp;D={3} &nbsp;&nbsp;x∈[{4},{5}]</color></size>");
        string line4 = string.Format(line4Fmt,
            plot.transA.ToString("0.##"),
            plot.transK.ToString("0.##"),
            plot.transC.ToString("0.##"),
            plot.transD.ToString("0.##"),
            plot.xStart.ToString("0.##"),
            plot.xEnd.ToString("0.##"));

        hint.richText = true;
        hint.alignment = TextAlignmentOptions.Center;
        hint.text = line1 + "\n" + line2 + "\n" + line3 + "\n" + line4;
        LocalizationManager.ApplyTextDirection(hint);
    }
}

/// <summary>Short release vs long release on the same <see cref="Button"/> without using onClick.</summary>
public class UiShortLongPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public float longPressThreshold = 0.48f;
    public System.Action onShortRelease;
    public System.Action onLongRelease;

    private float downTime;
    private bool down;

    public void OnPointerDown(PointerEventData eventData)
    {
        downTime = Time.time;
        down = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!down)
            return;
        down = false;
        float dt = Time.time - downTime;
        if (dt >= longPressThreshold)
            onLongRelease?.Invoke();
        else
            onShortRelease?.Invoke();
    }
}