using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime scroll overlay for <see cref="LearningArticleLibrary"/> (level select etc.).
/// </summary>
public static class MathArticlesOverlay
{
    private const string OverlayName = "MathArticlesOverlayRoot";

    /// <param name="canvasTransform">Usually the scene Canvas; overlay becomes its last sibling.</param>
    public static void Open(Transform canvasTransform)
    {
        if (canvasTransform == null)
            return;

        var existing = canvasTransform.Find(OverlayName);
        if (existing != null)
        {
            existing.gameObject.SetActive(true);
            existing.SetAsLastSibling();
            return;
        }

        var root = new GameObject(OverlayName);
        var rootRt = root.AddComponent<RectTransform>();
        rootRt.SetParent(canvasTransform, false);
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        var dim = root.AddComponent<Image>();
        dim.color = new Color(0.02f, 0.02f, 0.05f, 0.92f);
        dim.raycastTarget = true;

        void Close() => UnityEngine.Object.Destroy(root);

        var dimBtn = root.AddComponent<Button>();
        dimBtn.targetGraphic = dim;
        dimBtn.onClick.AddListener(Close);

        var panel = new GameObject("Panel");
        var panelRt = panel.AddComponent<RectTransform>();
        panelRt.SetParent(root.transform, false);
        bool tablet = DeviceLayout.IsTabletLike();
        ApplySafeAreaToPanel(panelRt, outerMargin: tablet ? 14f : 10f);

        var panelBg = panel.AddComponent<Image>();
        panelBg.color = new Color(0.14f, 0.15f, 0.18f, 1f);

        var closeBtnGo = new GameObject("CloseButton");
        var closeRt = closeBtnGo.AddComponent<RectTransform>();
        closeRt.SetParent(panel.transform, false);
        closeRt.anchorMin = new Vector2(0.92f, 0.92f);
        closeRt.anchorMax = new Vector2(0.98f, 0.98f);
        closeRt.pivot = new Vector2(1f, 1f);
        closeRt.sizeDelta = new Vector2(tablet ? 132f : 120f, tablet ? 50f : 44f);
        closeRt.anchoredPosition = Vector2.zero;

        var closeImg = closeBtnGo.AddComponent<Image>();
        closeImg.color = new Color(0.28f, 0.28f, 0.32f, 1f);
        var closeBtn = closeBtnGo.AddComponent<Button>();
        closeBtn.targetGraphic = closeImg;
        closeBtn.onClick.AddListener(Close);

        var closeTxtGo = new GameObject("Text");
        var closeTxtRt = closeTxtGo.AddComponent<RectTransform>();
        closeTxtRt.SetParent(closeBtnGo.transform, false);
        closeTxtRt.anchorMin = Vector2.zero;
        closeTxtRt.anchorMax = Vector2.one;
        closeTxtRt.offsetMin = Vector2.zero;
        closeTxtRt.offsetMax = Vector2.zero;
        var closeTmp = closeTxtGo.AddComponent<TextMeshProUGUI>();
        closeTmp.text = "Close";
        closeTmp.fontSize = tablet ? 26 : 22;
        closeTmp.alignment = TextAlignmentOptions.Center;
        closeTmp.color = Color.white;
        CopyFont(closeTmp);

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
        vImg.color = new Color(0f, 0f, 0f, 0f);
        var mask = viewportGo.AddComponent<Mask>();
        mask.showMaskGraphic = false;
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
        body.text = LearningArticleLibrary.GetLevelSelectArticleRichText();
        body.fontSize = tablet ? 27 : (Screen.height <= 950 ? 22 : 25);
        body.lineSpacing = 4f;
        body.alignment = TextAlignmentOptions.TopLeft;
        body.color = new Color(0.92f, 0.93f, 0.96f, 1f);
        body.enableWordWrapping = true;
        body.richText = true;
        body.margin = new Vector4(12f, 12f, 12f, 12f);
        CopyFont(body);

        var fitter = tmpGo.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var le = tmpGo.AddComponent<LayoutElement>();
        le.minWidth = 1f;

        root.SetAsLastSibling();
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

    private static void CopyFont(TextMeshProUGUI target)
    {
        var any = UnityEngine.Object.FindAnyObjectByType<TextMeshProUGUI>();
        if (any != null && any != target && any.font != null)
            target.font = any.font;
        if (target.font == null && TMP_Settings.defaultFontAsset != null)
            target.font = TMP_Settings.defaultFontAsset;
    }
}
