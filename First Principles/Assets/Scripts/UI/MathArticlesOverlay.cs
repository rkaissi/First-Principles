using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen scroll overlay for <see cref="LearningArticleLibrary"/> — opened from <b>Level select</b>
/// (<i>Math tips &amp; snippets</i>, full glossary) or from the <b>Game</b> scene (<i>Math concepts</i>, sections for the current level when the locale article uses <c>@@SECTION=</c> markers).
/// </summary>
public static class MathArticlesOverlay
{
    private const string OverlayName = "MathArticlesOverlayRoot";

    private static TextMeshProUGUI closeButtonTmp;
    private static TextMeshProUGUI articleBodyTmp;
    /// <summary>Preserves level filter when the article reloads after a language change.</summary>
    private static int? _articleLevelFilter;

    /// <param name="canvasTransform">Usually the scene Canvas; overlay becomes its last sibling.</param>
    /// <param name="forGameLevelIndex"><see langword="null"/> = full glossary (level select). Otherwise filtered sections for that level.</param>
    public static void Open(Transform canvasTransform, int? forGameLevelIndex = null)
    {
        if (canvasTransform == null)
            return;

        _articleLevelFilter = forGameLevelIndex;

        var existing = canvasTransform.Find(OverlayName);
        if (existing != null)
        {
            existing.gameObject.SetActive(true);
            existing.SetAsLastSibling();
            EnsureGoldenRatioBackdrop(existing);
            RefreshArticleBodyAndLayout(existing);
            return;
        }

        var root = new GameObject(OverlayName);
        var rootRt = root.AddComponent<RectTransform>();
        rootRt.SetParent(canvasTransform, false);
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        EnsureGoldenRatioBackdrop(root.transform);

        var dim = root.AddComponent<Image>();
        dim.color = new Color(0.04f, 0.05f, 0.11f, 0.93f);
        dim.raycastTarget = true;

        void Close()
        {
            _articleLevelFilter = null;
            LocalizationManager.LanguageChanged -= RefreshArticleBodyGlobal;
            articleBodyTmp = null;
            UnityEngine.Object.Destroy(root);
        }

        var dimBtn = root.AddComponent<Button>();
        dimBtn.targetGraphic = dim;
        dimBtn.onClick.AddListener(Close);

        var panel = new GameObject("Panel");
        var panelRt = panel.AddComponent<RectTransform>();
        panelRt.SetParent(root.transform, false);
        bool tablet = DeviceLayout.IsTabletLike();
        ApplySafeAreaToPanel(panelRt, outerMargin: tablet ? 14f : 10f);

        var panelBg = panel.AddComponent<Image>();
        RuntimeUiPolish.UseRoundedSliced(panelBg);
        panelBg.color = RuntimeUiPolish.PanelMid;
        RuntimeUiPolish.ApplyDropShadow(panelRt, new Vector2(3f, -5f), 0.34f);

        var closeBtnGo = new GameObject("CloseButton");
        var closeRt = closeBtnGo.AddComponent<RectTransform>();
        closeRt.SetParent(panel.transform, false);
        closeRt.anchorMin = new Vector2(0.92f, 0.92f);
        closeRt.anchorMax = new Vector2(0.98f, 0.98f);
        closeRt.pivot = new Vector2(1f, 1f);
        closeRt.sizeDelta = new Vector2(tablet ? 148f : 136f, tablet ? 56f : 50f);
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
        closeButtonTmp.fontSize = UiTypography.Scale(tablet ? 32 : 28);
        closeButtonTmp.fontStyle = FontStyles.Bold;
        closeButtonTmp.alignment = TextAlignmentOptions.Center;
        closeButtonTmp.color = Color.white;
        CopyFont(closeButtonTmp);
        closeButtonTmp.fontStyle = FontStyles.Bold;
        LocalizationManager.ApplyTextDirection(closeButtonTmp);
        LocalizationManager.LanguageChanged -= RefreshCloseLabel;
        LocalizationManager.LanguageChanged += RefreshCloseLabel;
        LocalizationManager.LanguageChanged -= RefreshArticleBodyGlobal;
        LocalizationManager.LanguageChanged += RefreshArticleBodyGlobal;

        var scrollGo = new GameObject("Scroll");
        var scrollRt = scrollGo.AddComponent<RectTransform>();
        scrollRt.SetParent(panel.transform, false);
        scrollRt.anchorMin = new Vector2(0.04f, 0.06f);
        scrollRt.anchorMax = new Vector2(0.96f, 0.88f);
        scrollRt.offsetMin = Vector2.zero;
        scrollRt.offsetMax = Vector2.zero;

        var scroll = scrollGo.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.elasticity = 0.12f;
        scroll.scrollSensitivity = DeviceLayout.LevelSelectScrollSensitivity;

        var viewportGo = new GameObject("Viewport");
        var viewportRt = viewportGo.AddComponent<RectTransform>();
        viewportRt.SetParent(scrollGo.transform, false);
        viewportRt.anchorMin = Vector2.zero;
        viewportRt.anchorMax = Vector2.one;
        viewportRt.offsetMin = Vector2.zero;
        viewportRt.offsetMax = Vector2.zero;
        var vImg = viewportGo.AddComponent<Image>();
        vImg.color = new Color(0f, 0f, 0f, 0f);
        // RectMask2D + raycastable Image avoids Mask+transparent-Image issues that clip all scroll content.
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

        var tmpGo = new GameObject("ArticleText");
        var tmpRt = tmpGo.AddComponent<RectTransform>();
        tmpRt.SetParent(contentRt, false);
        tmpRt.anchorMin = new Vector2(0f, 1f);
        tmpRt.anchorMax = new Vector2(1f, 1f);
        tmpRt.pivot = new Vector2(0.5f, 1f);
        tmpRt.anchoredPosition = Vector2.zero;
        tmpRt.sizeDelta = new Vector2(-40f, 0f);

        var body = tmpGo.AddComponent<TextMeshProUGUI>();
        articleBodyTmp = body;
        body.text = LearningArticleLibrary.GetLevelSelectArticleRichText();
        body.fontSize = UiTypography.Scale(tablet ? 27 : (Screen.height <= 950 ? 22 : 25));
        body.lineSpacing = 4f;
        body.alignment = TextAlignmentOptions.TopLeft;
        body.color = new Color(0.92f, 0.93f, 0.96f, 1f);
        body.textWrappingMode = TextWrappingModes.Normal;
        body.overflowMode = TextOverflowModes.Overflow;
        body.richText = true;
        body.margin = new Vector4(12f, 12f, 12f, 12f);
        CopyFont(body);
        ApplyArticleReadingLayout(body);

        var le = tmpGo.AddComponent<LayoutElement>();
        le.minWidth = 1f;

        Canvas.ForceUpdateCanvases();
        SyncArticleScrollLayout(body, scroll);

        root.transform.SetAsLastSibling();
    }

    /// <summary>Full-screen gold φ / Fibonacci drift behind the dim (first sibling so panel stays on top).</summary>
    private static void EnsureGoldenRatioBackdrop(Transform overlayRoot)
    {
        if (overlayRoot == null || overlayRoot.Find("GoldenRatioBackdrop") != null)
            return;

        var goldenGo = new GameObject("GoldenRatioBackdrop");
        var gRt = goldenGo.AddComponent<RectTransform>();
        gRt.SetParent(overlayRoot, false);
        gRt.anchorMin = Vector2.zero;
        gRt.anchorMax = Vector2.one;
        gRt.offsetMin = Vector2.zero;
        gRt.offsetMax = Vector2.zero;
        goldenGo.AddComponent<MathTipsGoldenRatioBackdrop>();
        goldenGo.transform.SetAsFirstSibling();
    }

    private static void RefreshArticleBodyGlobal() => RefreshArticleBodyText();

    private static void RefreshArticleBodyText()
    {
        if (articleBodyTmp == null)
            return;
        articleBodyTmp.text = LearningArticleLibrary.GetArticleRichTextForOverlay(_articleLevelFilter);
        ApplyArticleReadingLayout(articleBodyTmp);
        LocalizationManager.ApplyTextDirection(articleBodyTmp);
        var scroll = articleBodyTmp.GetComponentInParent<ScrollRect>();
        SyncArticleScrollLayout(articleBodyTmp, scroll);
    }

    private static void RefreshArticleBodyAndLayout(Transform overlayRoot)
    {
        var closeTr = overlayRoot.Find("Panel/CloseButton/Text");
        closeButtonTmp = closeTr != null ? closeTr.GetComponent<TextMeshProUGUI>() : null;
        RefreshCloseLabel();

        var bodyTr = overlayRoot.Find("Panel/Scroll/Viewport/Content/ArticleText");
        articleBodyTmp = bodyTr != null ? bodyTr.GetComponent<TextMeshProUGUI>() : null;
        RefreshArticleBodyText();
        var scrollTr = overlayRoot.Find("Panel/Scroll");
        var scroll = scrollTr != null ? scrollTr.GetComponent<ScrollRect>() : null;
        if (scroll != null)
            scroll.verticalNormalizedPosition = 1f;
    }

    /// <summary>
    /// TMP + layout groups often under-report height; drive content height from <see cref="TMP_Text.GetPreferredValues"/> so the <see cref="ScrollRect"/> can scroll long articles.
    /// </summary>
    private static void SyncArticleScrollLayout(TextMeshProUGUI body, ScrollRect scroll)
    {
        if (body == null)
            return;

        body.overflowMode = TextOverflowModes.Overflow;
        body.enableWordWrapping = true;

        var tmpRt = body.rectTransform;
        var contentRt = tmpRt.parent as RectTransform;
        var viewportRt = contentRt != null ? contentRt.parent as RectTransform : null;
        if (contentRt == null || viewportRt == null)
            return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(viewportRt);

        // Content must be at least viewport-tall so width is valid on first layout pass.
        float viewportW = Mathf.Max(1f, viewportRt.rect.width);
        float viewportH = Mathf.Max(1f, viewportRt.rect.height);
        if (contentRt.sizeDelta.y < viewportH)
            contentRt.sizeDelta = new Vector2(0f, viewportH);

        Canvas.ForceUpdateCanvases();
        float innerW = Mathf.Max(48f, contentRt.rect.width - 48f);
        body.ForceMeshUpdate(true);
        Vector2 pref = body.GetPreferredValues(innerW, 0f);
        float textH = Mathf.Max(pref.y, 1f);
        const float verticalPad = 20f;
        tmpRt.sizeDelta = new Vector2(-40f, textH);
        contentRt.sizeDelta = new Vector2(0f, Mathf.Max(viewportH, textH + verticalPad));

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRt);
        LayoutRebuilder.ForceRebuildLayoutImmediate(viewportRt);
        if (scroll != null)
        {
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 1f;
        }
    }

    private static void RefreshCloseLabel()
    {
        if (closeButtonTmp == null)
            return;
        closeButtonTmp.text = LocalizationManager.Get("ui.close", "Close");
        closeButtonTmp.fontStyle = FontStyles.Bold;
        LocalizationManager.ApplyTextDirection(closeButtonTmp);
    }

    private static void ApplySafeAreaToPanel(RectTransform panelRt, float outerMargin)
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

    /// <summary>
    /// Must run whenever the article body text changes. First open previously skipped RTL, so Arabic was LTR/invisible-looking.
    /// </summary>
    private static void ApplyArticleReadingLayout(TextMeshProUGUI tmp)
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
        var any = UnityEngine.Object.FindAnyObjectByType<TextMeshProUGUI>();
        if (any != null && any != target && any.font != null)
            target.font = any.font;
        if (target.font == null && TMP_Settings.defaultFontAsset != null)
            target.font = TMP_Settings.defaultFontAsset;
    }
}
