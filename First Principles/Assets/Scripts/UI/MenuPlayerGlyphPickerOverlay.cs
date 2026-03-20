using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Menu-only overlay: colored square grid of math symbols for the player avatar.
/// </summary>
public static class MenuPlayerGlyphPickerOverlay
{
    private const string RootName = "MenuPlayerGlyphPickerRoot";

    public static void Open(Transform canvasTransform)
    {
        if (canvasTransform == null)
            return;

        var existing = canvasTransform.Find(RootName);
        if (existing != null)
        {
            existing.gameObject.SetActive(true);
            existing.SetAsLastSibling();
            RefreshLocalized(existing);
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
        dim.color = new Color(0.02f, 0.03f, 0.08f, 0.94f);
        dim.raycastTarget = true;

        void Close()
        {
            LocalizationManager.LanguageChanged -= RefreshLocalizedStatic;
            Object.Destroy(root);
        }

        var dimBtn = root.AddComponent<Button>();
        dimBtn.targetGraphic = dim;
        dimBtn.onClick.AddListener(Close);

        var panel = new GameObject("Panel");
        var panelRt = panel.AddComponent<RectTransform>();
        panelRt.SetParent(root.transform, false);
        bool tablet = DeviceLayout.IsTabletLike();
        ApplySafeArea(panelRt, tablet ? 12f : 8f);

        var panelBg = panel.AddComponent<Image>();
        RuntimeUiPolish.UseRoundedSliced(panelBg);
        panelBg.color = RuntimeUiPolish.PanelMid;
        RuntimeUiPolish.ApplyDropShadow(panelRt, new Vector2(3f, -5f), 0.34f);

        var titleGo = new GameObject("Title");
        var titleRt = titleGo.AddComponent<RectTransform>();
        titleRt.SetParent(panel.transform, false);
        titleRt.anchorMin = new Vector2(0.06f, 0.86f);
        titleRt.anchorMax = new Vector2(0.94f, 0.95f);
        titleRt.offsetMin = Vector2.zero;
        titleRt.offsetMax = Vector2.zero;
        var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
        titleTmp.fontSize = UiTypography.Scale(tablet ? 34 : 28);
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = RuntimeUiPolish.TitleIvory;
        titleTmp.richText = true;
        CopyFont(titleTmp);
        titleTmp.text = LocalizationManager.Get("menu.player_icon_title", "Player icon");

        var hintGo = new GameObject("Hint");
        var hintRt = hintGo.AddComponent<RectTransform>();
        hintRt.SetParent(panel.transform, false);
        hintRt.anchorMin = new Vector2(0.08f, 0.78f);
        hintRt.anchorMax = new Vector2(0.92f, 0.845f);
        hintRt.offsetMin = Vector2.zero;
        hintRt.offsetMax = Vector2.zero;
        var hintTmp = hintGo.AddComponent<TextMeshProUGUI>();
        hintTmp.fontSize = UiTypography.Scale(tablet ? 22 : 18);
        hintTmp.alignment = TextAlignmentOptions.Center;
        hintTmp.color = new Color(0.75f, 0.78f, 0.86f, 0.95f);
        hintTmp.richText = true;
        CopyFont(hintTmp);
        hintTmp.text = LocalizationManager.Get("menu.player_icon_hint", "Tap a symbol — it’s your avatar in levels.");

        var gridGo = new GameObject("GridHost");
        var gridHostRt = gridGo.AddComponent<RectTransform>();
        gridHostRt.SetParent(panel.transform, false);
        gridHostRt.anchorMin = new Vector2(0.06f, 0.12f);
        gridHostRt.anchorMax = new Vector2(0.94f, 0.74f);
        gridHostRt.offsetMin = Vector2.zero;
        gridHostRt.offsetMax = Vector2.zero;

        var scrollGo = new GameObject("Scroll");
        var scrollRt = scrollGo.AddComponent<RectTransform>();
        scrollRt.SetParent(gridHostRt, false);
        scrollRt.anchorMin = Vector2.zero;
        scrollRt.anchorMax = Vector2.one;
        scrollRt.offsetMin = Vector2.zero;
        scrollRt.offsetMax = Vector2.zero;

        var scroll = scrollGo.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 36f;

        var viewport = new GameObject("Viewport");
        var viewportRt = viewport.AddComponent<RectTransform>();
        viewportRt.SetParent(scrollRt, false);
        viewportRt.anchorMin = Vector2.zero;
        viewportRt.anchorMax = Vector2.one;
        viewportRt.offsetMin = Vector2.zero;
        viewportRt.offsetMax = Vector2.zero;
        var mask = viewport.AddComponent<RectMask2D>();
        mask.padding = Vector4.zero;
        scroll.viewport = viewportRt;

        var content = new GameObject("Content");
        var contentRt = content.AddComponent<RectTransform>();
        contentRt.SetParent(viewportRt, false);
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0f, 400f);
        scroll.content = contentRt;

        var grid = content.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(tablet ? 108f : 92f, tablet ? 108f : 92f);
        grid.spacing = new Vector2(tablet ? 16f : 12f, tablet ? 16f : 12f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = tablet ? 4 : 3;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.padding = new RectOffset(
            tablet ? 8 : 6,
            tablet ? 8 : 6,
            tablet ? 10 : 8,
            tablet ? 18 : 14);

        var contentFitter = content.AddComponent<ContentSizeFitter>();
        contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        int selected = PlayerGlyphSettings.GetSelectedIndex();
        for (int i = 0; i < PlayerGlyphSettings.Glyphs.Length; i++)
        {
            int idx = i;
            string g = PlayerGlyphSettings.Glyphs[i];
            Color cell = PlayerGlyphSettings.GetAccentColorForIndex(i);
            if (i == selected)
                cell = Color.Lerp(cell, Color.white, 0.2f);

            var cellGo = new GameObject($"GlyphChoice_{i}");
            var cellRt = cellGo.AddComponent<RectTransform>();
            cellRt.SetParent(contentRt, false);

            var cellImg = cellGo.AddComponent<Image>();
            RuntimeUiPolish.UseRoundedSliced(cellImg);
            cellImg.color = cell;

            var btn = cellGo.AddComponent<Button>();
            btn.targetGraphic = cellImg;
            RuntimeUiPolish.ApplyButtonTransitions(btn, cell,
                Color.Lerp(cell, Color.white, 0.22f),
                Color.Lerp(cell, Color.black, 0.2f));
            btn.onClick.AddListener(() =>
            {
                PlayerGlyphSettings.SetSelectedIndex(idx);
                Close();
            });
            RuntimeUiPolish.ApplyDropShadow(cellRt, new Vector2(2f, -3f), 0.26f);

            var txtGo = new GameObject("Text");
            var txtRt = txtGo.AddComponent<RectTransform>();
            txtRt.SetParent(cellGo.transform, false);
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(6f, 6f);
            txtRt.offsetMax = new Vector2(-6f, -6f);
            var tmp = txtGo.AddComponent<TextMeshProUGUI>();
            tmp.text = g;
            tmp.fontSize = UiTypography.Scale(tablet ? 52 : 44);
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(1f, 0.99f, 0.97f, 1f);
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 22;
            tmp.fontSizeMax = 128;
            tmp.richText = false;
            tmp.outlineWidth = i == selected ? 0.38f : 0.28f;
            tmp.outlineColor = i == selected
                ? new Color(1f, 1f, 1f, 0.82f)
                : new Color(0f, 0f, 0f, 0.68f);
            CopyFont(tmp);
        }

        var closerGo = new GameObject("DoneButton");
        var closerRt = closerGo.AddComponent<RectTransform>();
        closerRt.SetParent(panel.transform, false);
        closerRt.anchorMin = new Vector2(0.28f, 0.03f);
        closerRt.anchorMax = new Vector2(0.72f, 0.095f);
        closerRt.offsetMin = Vector2.zero;
        closerRt.offsetMax = Vector2.zero;

        var closerImg = closerGo.AddComponent<Image>();
        RuntimeUiPolish.UseRoundedSliced(closerImg);
        closerImg.color = RuntimeUiPolish.ButtonNeutral;
        var closerBtn = closerGo.AddComponent<Button>();
        closerBtn.targetGraphic = closerImg;
        RuntimeUiPolish.ApplyButtonTransitions(closerBtn, RuntimeUiPolish.ButtonNeutral,
            RuntimeUiPolish.ButtonNeutralHover,
            RuntimeUiPolish.PanelDeep);
        closerBtn.onClick.AddListener(Close);

        var closerTxtGo = new GameObject("Text");
        var closerTxtRt = closerTxtGo.AddComponent<RectTransform>();
        closerTxtRt.SetParent(closerGo.transform, false);
        closerTxtRt.anchorMin = Vector2.zero;
        closerTxtRt.anchorMax = Vector2.one;
        closerTxtRt.offsetMin = Vector2.zero;
        closerTxtRt.offsetMax = Vector2.zero;
        var closerTmp = closerTxtGo.AddComponent<TextMeshProUGUI>();
        closerTmp.fontSize = UiTypography.Scale(tablet ? 24 : 20);
        closerTmp.alignment = TextAlignmentOptions.Center;
        closerTmp.color = Color.white;
        CopyFont(closerTmp);
        closerTmp.text = LocalizationManager.Get("menu.player_icon_done", "Done");

        LocalizationManager.LanguageChanged -= RefreshLocalizedStatic;
        LocalizationManager.LanguageChanged += RefreshLocalizedStatic;

        void RefreshLocalizedStatic() => RefreshLocalized(root.transform);

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRt);
        float prefH = LayoutUtility.GetPreferredHeight(contentRt);
        contentRt.sizeDelta = new Vector2(0f, Mathf.Max(prefH + 8f, 200f));
        scroll.verticalNormalizedPosition = 1f;

        root.transform.SetAsLastSibling();
    }

    private static void RefreshLocalized(Transform overlayRoot)
    {
        var titleTmp = overlayRoot.Find("Panel/Title") != null
            ? overlayRoot.Find("Panel/Title").GetComponent<TextMeshProUGUI>()
            : null;
        if (titleTmp != null)
        {
            titleTmp.text = LocalizationManager.Get("menu.player_icon_title", "Player icon");
            LocalizationManager.ApplyTextDirection(titleTmp);
        }

        var hintTmp = overlayRoot.Find("Panel/Hint") != null
            ? overlayRoot.Find("Panel/Hint").GetComponent<TextMeshProUGUI>()
            : null;
        if (hintTmp != null)
        {
            hintTmp.text = LocalizationManager.Get("menu.player_icon_hint", "Tap a symbol — it’s your avatar in levels.");
            LocalizationManager.ApplyTextDirection(hintTmp);
        }

        var doneTmp = overlayRoot.Find("Panel/DoneButton/Text") != null
            ? overlayRoot.Find("Panel/DoneButton/Text").GetComponent<TextMeshProUGUI>()
            : null;
        if (doneTmp != null)
        {
            doneTmp.text = LocalizationManager.Get("menu.player_icon_done", "Done");
            LocalizationManager.ApplyTextDirection(doneTmp);
        }
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

    private static void CopyFont(TextMeshProUGUI target)
    {
        var any = Object.FindAnyObjectByType<TextMeshProUGUI>();
        if (any != null && any != target && any.font != null)
            target.font = any.font;
        if (target.font == null && TMP_Settings.defaultFontAsset != null)
            target.font = TMP_Settings.defaultFontAsset;
    }
}
