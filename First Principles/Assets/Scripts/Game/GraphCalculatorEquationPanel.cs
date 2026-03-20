using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime <see cref="TMP_InputField"/> for graphing calculator mode: user types <c>f(u)</c> (variable <c>x</c> in the formula = inner <c>u</c> after Trans).
/// </summary>
public static class GraphCalculatorEquationPanel
{
    private const string RootName = "GraphicCalculatorEquationRoot";
    private const string LegacyRootName = "FaxasEquationInputRoot";

    private static TextMeshProUGUI labelTmp;
    private static TextMeshProUGUI placeholderTmp;
    private static TextMeshProUGUI statusTmp;

    public static void Ensure(RectTransform parent, FunctionPlotter plotter, TextMeshProUGUI typographyReference, float bottomY, float panelHeight)
    {
        if (parent == null || plotter == null)
            return;

        var existing = GameObject.Find(RootName);
        if (existing != null)
        {
            ApplyEquationPanelFontAndWeight(existing, typographyReference, plotter);
            return;
        }

        var legacyRoot = GameObject.Find(LegacyRootName);
        if (legacyRoot != null)
        {
            legacyRoot.name = RootName;
            ApplyEquationPanelFontAndWeight(legacyRoot, typographyReference, plotter);
            return;
        }

        bool tablet = DeviceLayout.IsTabletLike();
        float w = tablet ? 980f : 900f;

        var root = new GameObject(RootName);
        var rt = root.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, bottomY);
        rt.sizeDelta = new Vector2(w, panelHeight);

        var bg = root.AddComponent<Image>();
        RuntimeUiPolish.UseRoundedSliced(bg);
        bg.color = new Color(0.1f, 0.11f, 0.15f, 0.92f);
        bg.raycastTarget = true;

        var labelGo = new GameObject("Label");
        var lrt = labelGo.AddComponent<RectTransform>();
        lrt.SetParent(rt, false);
        lrt.anchorMin = new Vector2(0f, 1f);
        lrt.anchorMax = new Vector2(0f, 1f);
        lrt.pivot = new Vector2(0f, 1f);
        lrt.anchoredPosition = new Vector2(tablet ? 16f : 12f, -6f);
        lrt.sizeDelta = new Vector2(220f, 28f);
        var label = labelGo.AddComponent<TextMeshProUGUI>();
        label.text = LocalizationManager.Get("graph.label_fu", "f(u) =");
        label.fontSize = UiTypography.Scale(tablet ? 26 : 24);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.color = new Color(0.85f, 0.88f, 0.95f, 0.9f);
        CopyFont(label, typographyReference);
        label.fontStyle = FontStyles.Bold;

        var inputShell = new GameObject("TMP_InputField");
        var irt = inputShell.AddComponent<RectTransform>();
        irt.SetParent(rt, false);
        irt.anchorMin = new Vector2(0f, 0f);
        irt.anchorMax = new Vector2(1f, 1f);
        irt.offsetMin = new Vector2(tablet ? 96f : 88f, 10f);
        irt.offsetMax = new Vector2(-14f, -36f);

        var inputBg = inputShell.AddComponent<Image>();
        RuntimeUiPolish.UseRoundedSliced(inputBg);
        inputBg.color = new Color(0.15f, 0.16f, 0.21f, 0.95f);

        var textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        textArea.transform.SetParent(inputShell.transform, false);
        var tArt = textArea.GetComponent<RectTransform>();
        tArt.anchorMin = Vector2.zero;
        tArt.anchorMax = Vector2.one;
        tArt.offsetMin = new Vector2(10f, 8f);
        tArt.offsetMax = new Vector2(-10f, -8f);

        var textGo = new GameObject("Text");
        var textRt = textGo.AddComponent<RectTransform>();
        textRt.SetParent(textArea.transform, false);
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        var textMesh = textGo.AddComponent<TextMeshProUGUI>();
        textMesh.text = plotter.customExpression;
        textMesh.fontSize = UiTypography.Scale(tablet ? 24 : 21);
        textMesh.color = Color.white;
        textMesh.alignment = TextAlignmentOptions.MidlineLeft;
        CopyFont(textMesh, typographyReference);
        textMesh.fontStyle = FontStyles.Bold;

        var phGo = new GameObject("Placeholder");
        var phRt = phGo.AddComponent<RectTransform>();
        phRt.SetParent(textArea.transform, false);
        phRt.anchorMin = Vector2.zero;
        phRt.anchorMax = Vector2.one;
        phRt.offsetMin = Vector2.zero;
        phRt.offsetMax = Vector2.zero;
        var ph = phGo.AddComponent<TextMeshProUGUI>();
        ph.text = LocalizationManager.Get("graph.placeholder", "x^2 + sin(x) · ln(x) for x>0 · min(x,3)...");
        ph.fontSize = UiTypography.Scale(tablet ? 24 : 21);
        ph.color = new Color(1f, 1f, 1f, 0.32f);
        ph.fontStyle = FontStyles.Bold | FontStyles.Italic;
        ph.alignment = TextAlignmentOptions.MidlineLeft;
        CopyFont(ph, typographyReference);

        var input = inputShell.AddComponent<TMP_InputField>();
        input.textComponent = textMesh;
        input.placeholder = ph;
        input.textViewport = tArt;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.characterValidation = TMP_InputField.CharacterValidation.None;
        input.text = plotter.customExpression;

        var statusGo = new GameObject("Status");
        var srt = statusGo.AddComponent<RectTransform>();
        srt.SetParent(rt, false);
        srt.anchorMin = new Vector2(0f, 0f);
        srt.anchorMax = new Vector2(1f, 0f);
        srt.pivot = new Vector2(0.5f, 0f);
        var status = statusGo.AddComponent<TextMeshProUGUI>();
        status.alignment = TextAlignmentOptions.Midline;
        status.color = new Color(0.75f, 0.92f, 0.8f, 0.95f);
        status.richText = true;
        status.text = LocalizationManager.Get("graph.status_enter", "Enter / tap away to graph");
        CopyFont(status, typographyReference);
        status.fontStyle = FontStyles.Bold;
        ApplyStatusHintLayoutAndSize(status, srt, tablet);

        labelTmp = label;
        placeholderTmp = ph;
        statusTmp = status;
        LocalizationManager.ApplyTextDirection(labelTmp);
        LocalizationManager.ApplyTextDirection(placeholderTmp);
        LocalizationManager.ApplyTextDirection(statusTmp);
        LocalizationManager.LanguageChanged -= RefreshEquationPanelStaticCopy;
        LocalizationManager.LanguageChanged += RefreshEquationPanelStaticCopy;

        WireEquationApplyHandlers(root, plotter);
    }

    static void WireEquationApplyHandlers(GameObject root, FunctionPlotter plotter)
    {
        if (root == null || plotter == null)
            return;

        var input = root.GetComponentInChildren<TMP_InputField>(true);
        var status = root.transform.Find("Status")?.GetComponent<TextMeshProUGUI>();
        if (input == null)
            return;

        input.onSubmit.RemoveAllListeners();
        input.onEndEdit.RemoveAllListeners();
        input.onSubmit.AddListener(s => TryApplyEquation(plotter, status, s));
        input.onEndEdit.AddListener(s => TryApplyEquation(plotter, status, s));
    }

    static void TryApplyEquation(FunctionPlotter plotter, TextMeshProUGUI status, string s)
    {
        string t = string.IsNullOrWhiteSpace(s) ? "" : s.Trim();
        if (string.IsNullOrEmpty(t))
            return;

        if (!MathExpressionEvaluator.TryValidateRough(t, out string err))
        {
            if (status != null)
                status.text = $"<color=#ff9a9a>{TmpEscape(err)}</color>";
            return;
        }

        if (status != null)
            status.text = LocalizationManager.Get("graph.status_graphed", "<color=#8fd9b3>Graphed</color>");
        plotter.SetCustomExpression(t);
        GraphCalculatorAnalysisControls.NotifyExpressionChanged(plotter);
    }

    private static void RefreshEquationPanelStaticCopy()
    {
        if (labelTmp != null)
        {
            labelTmp.text = LocalizationManager.Get("graph.label_fu", "f(u) =");
            LocalizationManager.ApplyTextDirection(labelTmp);
        }
        if (placeholderTmp != null)
        {
            placeholderTmp.text = LocalizationManager.Get("graph.placeholder", "x^2 + sin(x) · ln(x) for x>0 · min(x,3)...");
            LocalizationManager.ApplyTextDirection(placeholderTmp);
        }
        if (statusTmp != null && statusTmp.text.IndexOf("#ff9a9a", StringComparison.Ordinal) < 0)
        {
            statusTmp.text = LocalizationManager.Get("graph.status_enter", "Enter / tap away to graph");
            LocalizationManager.ApplyTextDirection(statusTmp);
        }
    }

    private static string TmpEscape(string s)
    {
        if (string.IsNullOrEmpty(s))
            return "";
        return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    }

    private static void CopyFont(TextMeshProUGUI target, TextMeshProUGUI reference)
    {
        if (reference != null && reference.font != null)
        {
            target.font = reference.font;
            if (reference.fontSharedMaterial != null)
                target.fontSharedMaterial = reference.fontSharedMaterial;
        }
        else
            UiTypography.ApplyDefaultFontAsset(target);
    }

    /// <summary>
    /// Re-applies project font + bold weights when the equation panel already exists (scene or prior session).
    /// </summary>
    static void ApplyEquationPanelFontAndWeight(GameObject root, TextMeshProUGUI typographyReference, FunctionPlotter plotter)
    {
        if (root == null)
            return;

        labelTmp = root.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
        placeholderTmp = root.transform.Find("TMP_InputField/Text Area/Placeholder")?.GetComponent<TextMeshProUGUI>();
        statusTmp = root.transform.Find("Status")?.GetComponent<TextMeshProUGUI>();

        foreach (var t in root.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            bool placeholder = t == placeholderTmp
                || t.gameObject.name.IndexOf("Placeholder", StringComparison.OrdinalIgnoreCase) >= 0;
            CopyFont(t, typographyReference);
            t.fontStyle = placeholder ? (FontStyles.Bold | FontStyles.Italic) : FontStyles.Bold;
        }

        bool tablet = DeviceLayout.IsTabletLike();
        ApplyStatusHintLayoutAndSize(statusTmp, statusTmp != null ? statusTmp.rectTransform : null, tablet);

        LocalizationManager.LanguageChanged -= RefreshEquationPanelStaticCopy;
        LocalizationManager.LanguageChanged += RefreshEquationPanelStaticCopy;

        WireEquationApplyHandlers(root, plotter);
    }

    static void ApplyStatusHintLayoutAndSize(TextMeshProUGUI status, RectTransform srt, bool tablet)
    {
        if (status == null)
            return;
        status.fontSize = UiTypography.Scale(tablet ? 19 : 17);
        if (srt != null)
        {
            srt.anchoredPosition = new Vector2(0f, 0f);
            srt.sizeDelta = new Vector2(-24f, 28f);
        }
    }
}
