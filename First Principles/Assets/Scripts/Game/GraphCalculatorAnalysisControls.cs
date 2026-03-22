using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Graphing calculator: one numeric derivative overlay (f′, one tap), one-shot Riemann strips + primitive hint.
/// Resets when the equation changes (<see cref="NotifyExpressionChanged"/>).
/// </summary>
public class GraphCalculatorAnalysisControls : MonoBehaviour
{
    static readonly Color[] DerivativePalette =
    {
        new Color(0.98f, 0.45f, 0.55f, 1f),
        new Color(0.55f, 0.88f, 1f, 1f),
        new Color(0.78f, 0.55f, 0.98f, 1f),
        new Color(0.55f, 0.94f, 0.68f, 1f),
    };

    /// <summary>How many derivative taps allowed per equation (only f′).</summary>
    const int MaxDerivOrder = 1;
    const int RiemannRectCount = 40;

    FunctionPlotter plotter;
    RiemannStripRendererUI riemann;
    LineRendererUI primaryCurve;
    TextMeshProUGUI fontRef;

    LineRendererUI[] derivLines = new LineRendererUI[MaxDerivOrder];
    int derivativeDepth;
    bool integralUsed;
    bool integralBarsActive;
    float integralCachedXStart;
    float integralCachedXEnd;

    static readonly Color CalculatorRiemannFill = new Color(0.28f, 0.62f, 1f, 0.52f);

    Button derivButton;
    Button integralButton;
    GameObject derivButtonRoot;
    GameObject integralButtonRoot;

    /// <summary>Call after a new <c>f(u)</c> is applied from the equation panel.</summary>
    public static void NotifyExpressionChanged(FunctionPlotter forPlotter)
    {
        if (forPlotter == null)
            return;
        var all = FindObjectsByType<GraphCalculatorAnalysisControls>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var c in all)
        {
            if (c != null && c.plotter == forPlotter)
                c.ResetAnalysisState();
        }
    }

    public void Configure(
        FunctionPlotter functionPlotter,
        RiemannStripRendererUI riemannRenderer,
        LineRendererUI primaryLine,
        TextMeshProUGUI typographyReference,
        float transRowBottomY)
    {
        plotter = functionPlotter;
        riemann = riemannRenderer;
        primaryCurve = primaryLine;
        fontRef = typographyReference;

        derivativeDepth = 0;
        integralUsed = false;
        integralBarsActive = false;
        CleanupDerivativeLineComponents();
        DestroyUiButtons();

        BuildToolButtons(transRowBottomY);
        RefreshButtonInteractable();

        if (plotter != null)
            plotter.SetEquationExtraSuffix("");
        if (riemann != null)
            riemann.ClearStrips();
    }

    void OnDestroy()
    {
        CleanupDerivativeLineComponents();
        DestroyUiButtons();
    }

    void ResetAnalysisState()
    {
        derivativeDepth = 0;
        integralUsed = false;
        integralBarsActive = false;
        if (plotter != null)
            plotter.SetEquationExtraSuffix("");
        if (riemann != null)
            riemann.ClearStrips();
        HideAllDerivativeLines();
        RefreshButtonInteractable();
    }

    void LateUpdate()
    {
        if (plotter == null || primaryCurve == null)
            return;

        if (integralBarsActive && riemann != null && plotter.functionType == FunctionType.CustomExpression
            && (!Mathf.Approximately(integralCachedXStart, plotter.xStart)
                || !Mathf.Approximately(integralCachedXEnd, plotter.xEnd)))
        {
            riemann.RebuildForGraphingCalculator(plotter, primaryCurve, RiemannRectCount, RiemannRule.Midpoint, CalculatorRiemannFill);
            integralCachedXStart = plotter.xStart;
            integralCachedXEnd = plotter.xEnd;
            if (primaryCurve.transform.parent != null)
                riemann.transform.SetSiblingIndex(0);
        }

        if (plotter.functionType != FunctionType.CustomExpression)
        {
            HideAllDerivativeLines();
            return;
        }

        EnsureDerivativeLinesAllocated();
        Vector2Int gridOrigin = primaryCurve.gridSize / 2;

        for (int k = 0; k < MaxDerivOrder; k++)
        {
            bool on = k < derivativeDepth;
            var lr = derivLines[k];
            if (lr == null)
                continue;
            lr.gameObject.SetActive(on);
            if (!on)
                continue;

            lr.points.Clear();
                for (float x = plotter.xStart; x <= plotter.xEnd; x += plotter.step)
                {
                    float gy = plotter.SampleNthDerivativeGridY(x, k + 1);
                    if (float.IsFinite(gy))
                        lr.points.Add(new Vector2(plotter.MapPlotterXToGridX(x, gridOrigin.x), gy));
                }

            lr.enabled = false;
            lr.enabled = true;
            plotter.ApplyGraphRevealToLineRenderer(lr);
        }
    }

    void OnDerivativePressed()
    {
        if (plotter == null || plotter.functionType != FunctionType.CustomExpression)
            return;
        if (derivativeDepth >= MaxDerivOrder)
            return;
        derivativeDepth++;
        RefreshButtonInteractable();
    }

    void OnIntegralPressed()
    {
        if (integralUsed || plotter == null || riemann == null)
            return;
        if (plotter.functionType != FunctionType.CustomExpression)
            return;

        integralUsed = true;
        integralBarsActive = true;
        RefreshButtonInteractable();

        riemann.RebuildForGraphingCalculator(plotter, primaryCurve, RiemannRectCount, RiemannRule.Midpoint, CalculatorRiemannFill);
        integralCachedXStart = plotter.xStart;
        integralCachedXEnd = plotter.xEnd;
        if (primaryCurve != null && primaryCurve.transform.parent != null)
            riemann.transform.SetSiblingIndex(0);

        string expr = plotter.customExpression ?? "";
        string hint = GraphingCalculatorAntiderivativeHint.TryFormatPrimitiveLine(expr);
        if (string.IsNullOrEmpty(hint))
            hint = LocalizationManager.Get("graph.calc_integral_fallback", "∫ f(u) du + C  (no symbolic guess)");

        string prefix = LocalizationManager.Get("graph.calc_primitive_prefix", "Primitive (guess):");
        plotter.SetEquationExtraSuffix($"{prefix} {hint}");
    }

    void RefreshButtonInteractable()
    {
        if (derivButton != null)
            derivButton.interactable = derivativeDepth < MaxDerivOrder;
        if (integralButton != null)
            integralButton.interactable = !integralUsed;
    }

    void HideAllDerivativeLines()
    {
        for (int k = 0; k < MaxDerivOrder; k++)
        {
            if (derivLines[k] != null)
                derivLines[k].gameObject.SetActive(false);
        }
    }

    void EnsureDerivativeLinesAllocated()
    {
        if (primaryCurve == null)
            return;

        for (int k = 0; k < MaxDerivOrder; k++)
        {
            if (derivLines[k] != null)
                continue;

            var parent = primaryCurve.transform.parent;
            int idx = primaryCurve.transform.GetSiblingIndex();
            var clone = Instantiate(primaryCurve.gameObject, parent);
            clone.name = LineRendererUI.GraphCalcDerivOverlayPrefix + (k + 1);
            clone.transform.SetSiblingIndex(idx + 1 + k);

            var lr = clone.GetComponent<LineRendererUI>();
            lr.raycastTarget = false;
            lr.points = new System.Collections.Generic.List<Vector2>();
            lr.color = DerivativePalette[k];
            lr.thickness = primaryCurve.thickness * 0.78f;
            derivLines[k] = lr;
        }
    }

    void CleanupDerivativeLineComponents()
    {
        HideAllDerivativeLines();
        for (int k = 0; k < MaxDerivOrder; k++)
        {
            if (derivLines[k] != null)
            {
                Destroy(derivLines[k].gameObject);
                derivLines[k] = null;
            }
        }
    }

    void BuildToolButtons(float transRowBottomY)
    {
        var canvas = FindAnyObjectByType<Canvas>();
        var safe = canvas != null ? MobileUiRoots.GetSafeContentParent(canvas.transform) as RectTransform : null;
        var parent = safe != null ? safe : canvas?.transform as RectTransform;
        if (parent == null)
            return;

        bool tablet = DeviceLayout.IsTabletLike();
        float w = tablet ? 234f : 218f;
        float h = tablet ? 108f : 100f;
        float row2Bottom = transRowBottomY + h + 10f;
        float gap = tablet ? 14f : 10f;
        float x0 = tablet ? 20f : 14f;
        float halfW = (w * 2f + gap) * 0.5f - gap * 0.25f;

        derivButtonRoot = CreateCalcButton(
            "GraphCalcDerivButton",
            parent,
            new Vector2(x0, row2Bottom),
            new Vector2(halfW, h * 0.92f),
            LocalizationManager.Get("graph.calc_deriv", "Deriv"),
            OnDerivativePressed);

        integralButtonRoot = CreateCalcButton(
            "GraphCalcIntegralButton",
            parent,
            new Vector2(x0 + halfW + gap, row2Bottom),
            new Vector2(halfW, h * 0.92f),
            LocalizationManager.Get("graph.calc_integral", "∫ Area"),
            OnIntegralPressed);

        derivButton = derivButtonRoot.GetComponent<Button>();
        integralButton = integralButtonRoot.GetComponent<Button>();
    }

    GameObject CreateCalcButton(string objectName, RectTransform parent, Vector2 anchoredPos, Vector2 size, string label, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(objectName);
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;

        var img = go.AddComponent<Image>();
        RuntimeUiPolish.UseRoundedSliced(img);
        img.color = new Color(0.16f, 0.18f, 0.24f, 0.96f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.95f, 0.95f, 1f, 1f);
        colors.pressedColor = new Color(0.85f, 0.87f, 0.95f, 1f);
        colors.disabledColor = new Color(0.45f, 0.47f, 0.52f, 0.65f);
        btn.colors = colors;
        btn.onClick.AddListener(onClick);

        var textGo = new GameObject("Text");
        var trt = textGo.AddComponent<RectTransform>();
        trt.SetParent(rt, false);
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(8f, 6f);
        trt.offsetMax = new Vector2(-8f, -6f);

        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = UiTypography.Scale(DeviceLayout.IsTabletLike() ? 30 : 26);
        tmp.color = new Color(0.94f, 0.95f, 0.98f, 1f);
        tmp.fontStyle = FontStyles.Bold;
        tmp.raycastTarget = false;
        if (fontRef != null && fontRef.font != null)
        {
            tmp.font = fontRef.font;
            if (fontRef.fontSharedMaterial != null)
                tmp.fontSharedMaterial = fontRef.fontSharedMaterial;
        }
        else
            UiTypography.ApplyDefaultFontAsset(tmp);

        return go;
    }

    void DestroyUiButtons()
    {
        if (derivButtonRoot != null)
        {
            Destroy(derivButtonRoot);
            derivButtonRoot = null;
        }
        if (integralButtonRoot != null)
        {
            Destroy(integralButtonRoot);
            integralButtonRoot = null;
        }
        derivButton = null;
        integralButton = null;
    }

    /// <summary>
    /// Top edge of the Deriv / ∫ row in the same bottom‑anchored space as <see cref="BuildToolButtons"/>
    /// (<c>anchoredPosition.y</c> + button height).
    /// </summary>
    public static float DerivativeIntegralRowTopFromBottom(float transRowBottomY, bool tablet)
    {
        float h = tablet ? 108f : 100f;
        float row2Bottom = transRowBottomY + h + 10f;
        return row2Bottom + h * 0.92f;
    }
}
