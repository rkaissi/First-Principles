using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Menu-only scroll overlay explaining core gameplay, including the <b>f′ air jump</b>.
/// </summary>
public static class MenuTutorialOverlay
{
    private const string RootName = "MenuTutorialOverlayRoot";

    private static TextMeshProUGUI titleTmp;
    private static TextMeshProUGUI closeButtonTmp;
    private static TextMeshProUGUI bodyTmp;

    public static void Open(Transform canvasTransform)
    {
        if (canvasTransform == null)
            return;

        var existing = canvasTransform.Find(RootName);
        if (existing != null)
        {
            existing.gameObject.SetActive(true);
            existing.SetAsLastSibling();
            RefreshBodyAndLayout(existing);
            return;
        }

        var root = new GameObject(RootName);
        var rootRt = root.AddComponent<RectTransform>();
        rootRt.SetParent(canvasTransform, false);
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        var dim = root.AddComponent<Image>();
        dim.color = new Color(0.04f, 0.05f, 0.11f, 0.93f);
        dim.raycastTarget = true;

        void Close()
        {
            LocalizationManager.LanguageChanged -= RefreshAllLocalized;
            titleTmp = null;
            bodyTmp = null;
            closeButtonTmp = null;
            Object.Destroy(root);
        }

        var dimBtn = root.AddComponent<Button>();
        dimBtn.targetGraphic = dim;
        dimBtn.onClick.AddListener(Close);

        var panel = new GameObject("Panel");
        var panelRt = panel.AddComponent<RectTransform>();
        panelRt.SetParent(root.transform, false);
        bool tablet = DeviceLayout.IsTabletLike();
        ApplySafeArea(panelRt, tablet ? 14f : 10f);

        var panelBg = panel.AddComponent<Image>();
        RuntimeUiPolish.UseRoundedSliced(panelBg);
        panelBg.color = RuntimeUiPolish.PanelMid;
        RuntimeUiPolish.ApplyDropShadow(panelRt, new Vector2(3f, -5f), 0.34f);

        var titleGo = new GameObject("Title");
        var titleRt = titleGo.AddComponent<RectTransform>();
        titleRt.SetParent(panel.transform, false);
        titleRt.anchorMin = new Vector2(0.06f, 0.88f);
        titleRt.anchorMax = new Vector2(0.94f, 0.96f);
        titleRt.offsetMin = Vector2.zero;
        titleRt.offsetMax = Vector2.zero;
        titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
        titleTmp.text = LocalizationManager.Get("menu.tutorial_title", "How to play");
        titleTmp.fontSize = UiTypography.Scale(tablet ? 36 : 30);
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = RuntimeUiPolish.TitleIvory;
        titleTmp.richText = true;
        CopyFont(titleTmp);
        LocalizationManager.ApplyTextDirection(titleTmp);

        var closeBtnGo = new GameObject("CloseButton");
        var closeRt = closeBtnGo.AddComponent<RectTransform>();
        closeRt.SetParent(panel.transform, false);
        closeRt.anchorMin = new Vector2(0.92f, 0.92f);
        closeRt.anchorMax = new Vector2(0.98f, 0.98f);
        closeRt.pivot = new Vector2(1f, 1f);
        closeRt.sizeDelta = new Vector2(tablet ? 132f : 120f, tablet ? 50f : 44f);
        closeRt.anchoredPosition = Vector2.zero;

        var closeImg = closeBtnGo.AddComponent<Image>();
        RuntimeUiPolish.UseRoundedSliced(closeImg);
        closeImg.color = RuntimeUiPolish.ButtonNeutral;
        var closeBtn = closeBtnGo.AddComponent<Button>();
        closeBtn.targetGraphic = closeImg;
        RuntimeUiPolish.ApplyButtonTransitions(closeBtn, RuntimeUiPolish.ButtonNeutral,
            RuntimeUiPolish.ButtonNeutralHover, RuntimeUiPolish.PanelDeep);
        closeBtn.onClick.AddListener(Close);
        RuntimeUiPolish.ApplyDropShadow(closeRt, new Vector2(1.5f, -2f), 0.22f);

        var closeTxtGo = new GameObject("Text");
        var closeTxtRt = closeTxtGo.AddComponent<RectTransform>();
        closeTxtRt.SetParent(closeBtnGo.transform, false);
        closeTxtRt.anchorMin = Vector2.zero;
        closeTxtRt.anchorMax = Vector2.one;
        closeTxtRt.offsetMin = Vector2.zero;
        closeTxtRt.offsetMax = Vector2.zero;
        closeButtonTmp = closeTxtGo.AddComponent<TextMeshProUGUI>();
        closeButtonTmp.text = LocalizationManager.Get("ui.close", "Close");
        closeButtonTmp.fontSize = UiTypography.Scale(tablet ? 26 : 22);
        closeButtonTmp.fontStyle = FontStyles.Bold;
        closeButtonTmp.alignment = TextAlignmentOptions.Center;
        closeButtonTmp.color = Color.white;
        CopyFont(closeButtonTmp);
        closeButtonTmp.fontStyle = FontStyles.Bold;
        LocalizationManager.ApplyTextDirection(closeButtonTmp);

        var scrollGo = new GameObject("Scroll");
        var scrollRt = scrollGo.AddComponent<RectTransform>();
        scrollRt.SetParent(panel.transform, false);
        scrollRt.anchorMin = new Vector2(0.04f, 0.06f);
        scrollRt.anchorMax = new Vector2(0.96f, 0.84f);
        scrollRt.offsetMin = Vector2.zero;
        scrollRt.offsetMax = Vector2.zero;

        var scroll = scrollGo.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = tablet ? 40f : 25f;

        var viewportGo = new GameObject("Viewport");
        var viewportRt = viewportGo.AddComponent<RectTransform>();
        viewportRt.SetParent(scrollGo.transform, false);
        viewportRt.anchorMin = Vector2.zero;
        viewportRt.anchorMax = Vector2.one;
        viewportRt.offsetMin = Vector2.zero;
        viewportRt.offsetMax = Vector2.zero;
        var vImg = viewportGo.AddComponent<Image>();
        vImg.color = Color.clear;
        vImg.raycastTarget = true;
        viewportGo.AddComponent<RectMask2D>();
        scroll.viewport = viewportRt;

        var contentGo = new GameObject("Content");
        var contentRt = contentGo.AddComponent<RectTransform>();
        contentRt.SetParent(viewportGo.transform, false);
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0f, 0f);
        scroll.content = contentRt;
        var contentCsf = contentGo.AddComponent<ContentSizeFitter>();
        contentCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var tmpGo = new GameObject("TutorialText");
        var tmpRt = tmpGo.AddComponent<RectTransform>();
        tmpRt.SetParent(contentRt, false);
        tmpRt.anchorMin = new Vector2(0f, 1f);
        tmpRt.anchorMax = new Vector2(1f, 1f);
        tmpRt.pivot = new Vector2(0.5f, 1f);
        tmpRt.anchoredPosition = Vector2.zero;
        tmpRt.sizeDelta = new Vector2(-36f, 0f);

        bodyTmp = tmpGo.AddComponent<TextMeshProUGUI>();
        bodyTmp.text = BuildBodyText();
        bodyTmp.fontSize = UiTypography.Scale(tablet ? 26 : (Screen.height <= 950 ? 21 : 24));
        bodyTmp.lineSpacing = 2f;
        bodyTmp.alignment = TextAlignmentOptions.TopLeft;
        bodyTmp.color = new Color(0.92f, 0.93f, 0.96f, 1f);
        bodyTmp.textWrappingMode = TextWrappingModes.Normal;
        bodyTmp.richText = true;
        bodyTmp.margin = new Vector4(10f, 10f, 10f, 10f);
        CopyFont(bodyTmp);
        ApplyReadingLayout(bodyTmp);

        var fitter = tmpGo.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        tmpGo.AddComponent<LayoutElement>().minWidth = 1f;

        bodyTmp.ForceMeshUpdate(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRt);
        Canvas.ForceUpdateCanvases();
        scroll.verticalNormalizedPosition = 1f;

        LocalizationManager.LanguageChanged -= RefreshAllLocalized;
        LocalizationManager.LanguageChanged += RefreshAllLocalized;

        root.transform.SetAsLastSibling();
    }

    private static string BuildBodyText()
    {
        string raw = LocalizationManager.Get("menu.tutorial_body", "");
        if (string.IsNullOrWhiteSpace(raw))
            raw = DefaultBodyEnglish();
        return TmpLatex.Process(raw);
    }

    private static string DefaultBodyEnglish()
    {
        return
            "<b>Goal</b>\n" +
            "Move along the graph, use platforms (green), avoid red hazards, and reach the bright exit on the right.\n\n" +
            "<b>Controls</b>\n" +
            "• <b>Move</b> — arrows / WASD, or on-screen buttons.\n" +
            "• <b>Jump</b> — Space (or mobile jump). You jump from solid platforms.\n\n" +
            "<b>The two curves</b>\n" +
            "• <color=#c4b5fd>Main curve</color> — the function you walk on.\n" +
            "• <color=#fdba74>Derivative \\(f'\\)</color> — slope information; it’s drawn as a second line.\n\n" +
            "<b>Air jump on the derivative</b>\n" +
            "While you are <b>in the air</b>, if your character overlaps the <b>derivative curve</b>, you can press <b>Jump again</b> for an extra hop.\n\n" +
            "<size=94%><color=#a8b2d1>The derivative line brightens when you touch it. You get <b>one</b> bonus jump each time you brush it until you leave the curve or land. On phones you may feel a short vibration when you touch \\(f'\\); on PC a sound plays instead when vibration isn’t available.</color></size>\n\n" +
            "<b>Select level</b>\n" +
            "<size=94%>On this screen, stages are grouped (Core, Engineering, AP Calculus, Aerospace …). Tap <b>How to play</b> at the bottom to reopen this help. Pick a group, then a level.</size>";
    }

    private static void RefreshAllLocalized()
    {
        if (titleTmp != null)
        {
            titleTmp.text = LocalizationManager.Get("menu.tutorial_title", "How to play");
            LocalizationManager.ApplyTextDirection(titleTmp);
        }

        if (bodyTmp != null)
        {
            bodyTmp.text = BuildBodyText();
            ApplyReadingLayout(bodyTmp);
            bodyTmp.ForceMeshUpdate(true);
            var contentRt = bodyTmp.rectTransform.parent as RectTransform;
            if (contentRt != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRt);
        }

        if (closeButtonTmp != null)
        {
            closeButtonTmp.text = LocalizationManager.Get("ui.close", "Close");
            closeButtonTmp.fontStyle = FontStyles.Bold;
            LocalizationManager.ApplyTextDirection(closeButtonTmp);
        }
    }

    private static void RefreshBodyAndLayout(Transform overlayRoot)
    {
        var titleTr = overlayRoot.Find("Panel/Title");
        titleTmp = titleTr != null ? titleTr.GetComponent<TextMeshProUGUI>() : null;
        var bodyTr = overlayRoot.Find("Panel/Scroll/Viewport/Content/TutorialText");
        bodyTmp = bodyTr != null ? bodyTr.GetComponent<TextMeshProUGUI>() : null;
        var closeTr = overlayRoot.Find("Panel/CloseButton/Text");
        closeButtonTmp = closeTr != null ? closeTr.GetComponent<TextMeshProUGUI>() : null;
        RefreshAllLocalized();
        var scrollTr = overlayRoot.Find("Panel/Scroll");
        var scroll = scrollTr != null ? scrollTr.GetComponent<ScrollRect>() : null;
        if (scroll != null)
            scroll.verticalNormalizedPosition = 1f;
    }

    private static void ApplySafeArea(RectTransform panelRt, float outerMargin)
    {
        Rect sa = Screen.safeArea;
        float w = Mathf.Max(1f, Screen.width);
        float h = Mathf.Max(1f, Screen.height);
        float m = outerMargin;
        float pxMin = Mathf.Clamp(sa.xMin + m, 0f, w);
        float pxMax = Mathf.Clamp(sa.xMax - m, 0f, w);
        float pyMin = Mathf.Clamp(sa.yMin + m, 0f, h);
        float pyMax = Mathf.Clamp(sa.yMax - m, 0f, h);
        panelRt.anchorMin = new Vector2(pxMin / w, pyMin / h);
        panelRt.anchorMax = new Vector2(pxMax / w, pyMax / h);
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;
    }

    private static void ApplyReadingLayout(TextMeshProUGUI tmp)
    {
        if (tmp == null)
            return;
        LocalizationManager.ApplyTextDirection(tmp);
        tmp.alignment = LocalizationManager.IsRightToLeft
            ? TextAlignmentOptions.TopRight
            : TextAlignmentOptions.TopLeft;
    }

    private static void CopyFont(TextMeshProUGUI target)
    {
        var any = Object.FindAnyObjectByType<TextMeshProUGUI>();
        if (any != null && any != target && any.font != null)
            target.font = any.font;
        if (target.font == null && TMP_Settings.defaultFontAsset != null)
            target.font = TMP_Settings.defaultFontAsset;
    }
}
