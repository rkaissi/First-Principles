using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

// -----------------------------------------------------------------------------
// LevelSelectController — all UI for the LevelSelect scene is code-generated
// -----------------------------------------------------------------------------
// Parents to MobileUiRoots safe rect. Scrolls if GameLevelCatalog.LevelCount grows.
// “Math tips” opens MathArticlesOverlay on the same Canvas.
// -----------------------------------------------------------------------------

/// <summary>
/// Builds Limbo-style level list UI at runtime and loads <c>Game</c> with <see cref="LevelSelection"/>.
/// </summary>
public class LevelSelectController : MonoBehaviour
{
    [Tooltip("Full-screen panel tint (defaults match RuntimeUiPolish theme).")]
    [SerializeField] private Color backgroundColor = new Color(0.09f, 0.10f, 0.14f, 0.97f);
    [SerializeField] private Color buttonColor = new Color(0.20f, 0.23f, 0.30f, 0.95f);

    private TextMeshProUGUI titleTmp;
    private TextMeshProUGUI howToPlayTmp;
    private TextMeshProUGUI mathTipsTmp;
    private TextMeshProUGUI backTmp;
    private TextMeshProUGUI levelSelectLangTmp;
    private readonly List<TextMeshProUGUI> categoryRowLabels = new List<TextMeshProUGUI>();
    private readonly List<TextMeshProUGUI> levelRowLabels = new List<TextMeshProUGUI>();
    private TextMeshProUGUI backToCategoriesTmp;

    private ScrollRect _levelScroll;
    private RectTransform _levelContentRt;
    private LayoutElement _levelContentLayoutElement;
    private VerticalLayoutGroup _levelContentVlg;
    private int _levelContentPadVertical;
    private float _levelRowHeight;
    private float _levelRowSpacing;

    private bool _levelPickMode;
    private int _levelViewStart;
    private int _levelViewEnd;
    private int _selectedCategoryIndex = -1;

    private void OnEnable()
    {
        LocalizationManager.LanguageChanged += RefreshLocalizedStrings;
    }

    private void OnDisable()
    {
        LocalizationManager.LanguageChanged -= RefreshLocalizedStrings;
    }

    private void RefreshLocalizedStrings()
    {
        if (titleTmp != null)
        {
            if (_levelPickMode
                && _selectedCategoryIndex >= 0
                && _selectedCategoryIndex < GameLevelCatalog.SelectCategories.Length)
            {
                var cat = GameLevelCatalog.SelectCategories[_selectedCategoryIndex];
                titleTmp.text = LocalizationManager.Get(cat.TitleLocalizationKey, cat.DefaultTitle);
            }
            else
            {
                titleTmp.text = LocalizationManager.Get("ui.choose_stage", "Select level");
            }

            LocalizationManager.ApplyTextDirection(titleTmp);
        }

        if (howToPlayTmp != null)
        {
            howToPlayTmp.text = LocalizationManager.Get("menu.tutorial_button", "How to play");
            LocalizationManager.ApplyTextDirection(howToPlayTmp);
        }

        if (mathTipsTmp != null)
        {
            mathTipsTmp.text = LocalizationManager.Get("ui.math_tips", "Math tips & snippets");
            LocalizationManager.ApplyTextDirection(mathTipsTmp);
        }
        if (backTmp != null)
        {
            backTmp.text = LocalizationManager.Get("ui.back_menu", "Back to Menu");
            LocalizationManager.ApplyTextDirection(backTmp);
        }
        if (backToCategoriesTmp != null)
        {
            backToCategoriesTmp.text = LocalizationManager.Get("ui.level_select.back_categories", "All categories");
            LocalizationManager.ApplyTextDirection(backToCategoriesTmp);
        }

        for (int i = 0; i < categoryRowLabels.Count; i++)
        {
            var tmp = categoryRowLabels[i];
            if (tmp == null || i >= GameLevelCatalog.SelectCategories.Length)
                continue;
            var cat = GameLevelCatalog.SelectCategories[i];
            tmp.text = LocalizationManager.Get(cat.TitleLocalizationKey, cat.DefaultTitle);
            LocalizationManager.ApplyTextDirection(tmp);
        }

        for (int i = 0; i < levelRowLabels.Count; i++)
        {
            var tmp = levelRowLabels[i];
            if (tmp == null)
                continue;
            int levelIdx = _levelViewStart + i;
            tmp.text = GameLevelCatalog.GetLocalizedDisplayName(levelIdx);
            LocalizationManager.ApplyTextDirection(tmp);
        }

        RefreshLevelSelectLanguageLabel();
    }

    private void RefreshLevelSelectLanguageLabel()
    {
        if (levelSelectLangTmp == null)
            return;
        levelSelectLangTmp.text = LocalizationManager.GetLanguageChipDisplayText();
        LocalizationManager.ApplyLanguagePickerTextDirection(levelSelectLangTmp);
    }

    private void Start()
    {
        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("LevelSelectController: No Canvas in scene.");
            return;
        }

        var safeParent = MobileUiRoots.GetSafeContentParent(canvas.transform);
        var panel = new GameObject("LevelSelectPanel");
        var prt = panel.AddComponent<RectTransform>();
        prt.SetParent(safeParent != null ? safeParent : canvas.transform, false);
        prt.anchorMin = Vector2.zero;
        prt.anchorMax = Vector2.one;
        prt.offsetMin = Vector2.zero;
        prt.offsetMax = Vector2.zero;

        var pimg = panel.AddComponent<Image>();
        pimg.color = backgroundColor;
        pimg.raycastTarget = true;

        var backdropGo = new GameObject("FallingSymbolsBackdrop");
        var backdropRt = backdropGo.AddComponent<RectTransform>();
        backdropRt.SetParent(prt, false);
        backdropRt.anchorMin = Vector2.zero;
        backdropRt.anchorMax = Vector2.one;
        backdropRt.offsetMin = Vector2.zero;
        backdropRt.offsetMax = Vector2.zero;
        backdropGo.AddComponent<LevelSelectFallingSymbolsBackdrop>();
        backdropGo.transform.SetAsFirstSibling();

        var titleGo = new GameObject("Title");
        var titleRt = titleGo.AddComponent<RectTransform>();
        titleRt.SetParent(panel.transform, false);
        bool tablet = DeviceLayout.IsTabletLike();
        titleRt.anchorMin = new Vector2(0.5f, tablet ? 0.91f : 0.9f);
        titleRt.anchorMax = new Vector2(0.5f, tablet ? 0.91f : 0.9f);
        titleRt.pivot = new Vector2(0.5f, 0.5f);
        titleRt.sizeDelta = new Vector2(tablet ? 1080f : 1000f, tablet ? 108f : 96f);
        titleRt.anchoredPosition = Vector2.zero;

        titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
        titleTmp.text = LocalizationManager.Get("ui.choose_stage", "Select level");
        titleTmp.fontSize = UiTypography.Scale(tablet ? 52 : 46);
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = RuntimeUiPolish.TitleIvory;
        CopyFontFromAny(titleTmp);
        LocalizationManager.ApplyTextDirection(titleTmp);

        CreateLevelSelectLanguagePicker(panel.transform);

        // Scrollable list (categories first, then per-category stages).
        var scrollGo = new GameObject("LevelScroll");
        var scrollRt = scrollGo.AddComponent<RectTransform>();
        scrollRt.SetParent(panel.transform, false);
        scrollRt.anchorMin = DeviceLayout.LevelSelectScrollAnchorMin;
        scrollRt.anchorMax = DeviceLayout.LevelSelectScrollAnchorMax;
        scrollRt.offsetMin = Vector2.zero;
        scrollRt.offsetMax = Vector2.zero;

        _levelScroll = scrollGo.AddComponent<ScrollRect>();
        _levelScroll.horizontal = false;
        _levelScroll.vertical = true;
        _levelScroll.movementType = ScrollRect.MovementType.Clamped;
        _levelScroll.scrollSensitivity = DeviceLayout.LevelSelectScrollSensitivity;

        var viewportGo = new GameObject("Viewport");
        var viewportRt = viewportGo.AddComponent<RectTransform>();
        viewportRt.SetParent(scrollGo.transform, false);
        viewportRt.anchorMin = Vector2.zero;
        viewportRt.anchorMax = Vector2.one;
        viewportRt.offsetMin = Vector2.zero;
        viewportRt.offsetMax = Vector2.zero;
        viewportGo.AddComponent<RectMask2D>();
        var vImg = viewportGo.AddComponent<Image>();
        vImg.color = new Color(0f, 0f, 0f, 0f);
        vImg.raycastTarget = true;
        _levelScroll.viewport = viewportRt;

        var contentGo = new GameObject("Content");
        var contentRt = contentGo.AddComponent<RectTransform>();
        contentRt.SetParent(viewportGo.transform, false);
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0f, 0f);

        _levelScroll.content = contentRt;
        _levelContentRt = contentRt;

        _levelContentVlg = contentGo.AddComponent<VerticalLayoutGroup>();
        _levelContentVlg.childAlignment = TextAnchor.UpperCenter;
        _levelRowSpacing = tablet ? 20f : 18f;
        _levelContentVlg.spacing = _levelRowSpacing;
        _levelContentVlg.padding = new RectOffset(tablet ? 16 : 12, tablet ? 16 : 12, tablet ? 14 : 12, tablet ? 14 : 12);
        _levelContentVlg.childControlHeight = true;
        _levelContentVlg.childControlWidth = true;
        _levelContentVlg.childForceExpandHeight = false;
        _levelContentVlg.childForceExpandWidth = true;

        var fitter = contentGo.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _levelRowHeight = tablet ? 100f : 92f;
        _levelContentPadVertical = _levelContentVlg.padding.top + _levelContentVlg.padding.bottom;
        _levelContentLayoutElement = contentGo.AddComponent<LayoutElement>();
        _levelContentLayoutElement.minHeight = 400f;

        BuildCategoryView(false);

        RebuildScrollContent(_levelScroll, contentRt);
        StartCoroutine(FinalizeScrollAfterTmpLayout(_levelScroll, contentRt));

        // Chips anchored to bottom (Math tips above How to play); create order puts How to play on top in sibling stack.
        CreateMathArticlesButton(panel.transform, canvas.transform);
        CreateHowToPlayButton(panel.transform, canvas.transform);

        CreateBackButton(panel.transform);
    }

    private static void RebuildScrollContent(ScrollRect scroll, RectTransform contentRt)
    {
        if (contentRt == null)
            return;

        for (int i = 0; i < contentRt.childCount; i++)
        {
            var tmp = contentRt.GetChild(i).GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
                tmp.ForceMeshUpdate(true);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRt);
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRt);

        if (scroll != null)
            scroll.verticalNormalizedPosition = 1f;
    }

    private static IEnumerator FinalizeScrollAfterTmpLayout(ScrollRect scroll, RectTransform contentRt)
    {
        yield return null;
        yield return new WaitForEndOfFrame();
        RebuildScrollContent(scroll, contentRt);
    }

    private void CreateHowToPlayButton(Transform panelRoot, Transform canvasTransform)
    {
        if (GameObject.Find("LevelSelectHowToPlayButton") != null)
            return;

        var go = new GameObject("LevelSelectHowToPlayButton");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(panelRoot, false);
        bool tablet = DeviceLayout.IsTabletLike();
        // Leave room for bottom-left Back button (see CreateBackButton).
        float bottomInset = tablet ? 88f : 92f;
        float howH = tablet ? 76f : 70f;
        // Bottom-centered — lowest chip (Math tips sits above; see CreateMathArticlesButton).
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(tablet ? 700f : 632f, howH);
        rt.anchoredPosition = new Vector2(0f, bottomInset);

        var img = go.AddComponent<Image>();
        RuntimeUiPolish.UseRoundedSliced(img);
        img.color = new Color(0.24f, 0.30f, 0.55f, 0.96f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        RuntimeUiPolish.ApplyButtonTransitions(btn, img.color,
            Color.Lerp(img.color, Color.white, 0.2f),
            Color.Lerp(img.color, Color.black, 0.18f));
        RuntimeUiPolish.ApplyDropShadow(rt, new Vector2(2f, -3f), 0.26f);

        var textGo = new GameObject("Text");
        var trt = textGo.AddComponent<RectTransform>();
        trt.SetParent(go.transform, false);
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(16f, 8f);
        trt.offsetMax = new Vector2(-16f, -8f);

        howToPlayTmp = textGo.AddComponent<TextMeshProUGUI>();
        howToPlayTmp.text = LocalizationManager.Get("menu.tutorial_button", "How to play");
        howToPlayTmp.fontSize = UiTypography.Scale(tablet ? 36 : 32);
        howToPlayTmp.fontStyle = FontStyles.Bold;
        howToPlayTmp.alignment = TextAlignmentOptions.Center;
        howToPlayTmp.color = new Color(0.92f, 0.98f, 1f, 1f);
        howToPlayTmp.textWrappingMode = TextWrappingModes.Normal;
        howToPlayTmp.overflowMode = TextOverflowModes.Overflow;
        howToPlayTmp.richText = true;
        CopyFontFromAny(howToPlayTmp);
        howToPlayTmp.fontStyle = FontStyles.Bold;
        LocalizationManager.ApplyTextDirection(howToPlayTmp);

        btn.onClick.AddListener(() =>
        {
            if (canvasTransform == null)
                return;
            MenuTutorialOverlay.Open(canvasTransform);
        });
    }

    private void CreateMathArticlesButton(Transform panelRoot, Transform canvasTransform)
    {
        var go = new GameObject("MathArticlesButton");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(panelRoot, false);
        bool tablet = DeviceLayout.IsTabletLike();
        float bottomInset = tablet ? 88f : 92f;
        float howH = tablet ? 76f : 70f;
        float mathH = tablet ? 82f : 76f;
        float chipGap = 12f;
        // Bottom band — directly above "How to play".
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(tablet ? 700f : 632f, mathH);
        rt.anchoredPosition = new Vector2(0f, bottomInset + howH + chipGap);

        var img = go.AddComponent<Image>();
        RuntimeUiPolish.UseRoundedSliced(img);
        img.color = RuntimeUiPolish.AccentTeal;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        RuntimeUiPolish.ApplyButtonTransitions(btn, RuntimeUiPolish.AccentTeal,
            Color.Lerp(RuntimeUiPolish.AccentTeal, Color.white, 0.2f),
            Color.Lerp(RuntimeUiPolish.AccentTeal, Color.black, 0.22f));
        RuntimeUiPolish.ApplyDropShadow(rt, new Vector2(2f, -3f), 0.28f);

        var textGo = new GameObject("Text");
        var trt = textGo.AddComponent<RectTransform>();
        trt.SetParent(go.transform, false);
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(16f, 8f);
        trt.offsetMax = new Vector2(-16f, -8f);

        mathTipsTmp = textGo.AddComponent<TextMeshProUGUI>();
        mathTipsTmp.text = LocalizationManager.Get("ui.math_tips", "Math tips & snippets");
        mathTipsTmp.fontSize = UiTypography.Scale(tablet ? 38 : 34);
        mathTipsTmp.fontStyle = FontStyles.Bold;
        mathTipsTmp.alignment = TextAlignmentOptions.Center;
        mathTipsTmp.color = new Color(0.9f, 0.96f, 1f, 1f);
        mathTipsTmp.textWrappingMode = TextWrappingModes.Normal;
        mathTipsTmp.overflowMode = TextOverflowModes.Overflow;
        CopyFontFromAny(mathTipsTmp);
        mathTipsTmp.fontStyle = FontStyles.Bold;
        LocalizationManager.ApplyTextDirection(mathTipsTmp);

        btn.onClick.AddListener(() => MathArticlesOverlay.Open(canvasTransform));
    }

    private void CreateLevelSelectLanguagePicker(Transform panelRoot)
    {
        if (GameObject.Find("LevelSelectLanguagePicker") != null)
            return;

        bool tablet = DeviceLayout.IsTabletLike();
        var go = new GameObject("LevelSelectLanguagePicker");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(panelRoot, false);
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(tablet ? 14f : 10f, tablet ? -10f : -8f);
        rt.sizeDelta = new Vector2(tablet ? 380f : 340f, tablet ? 64f : 56f);

        var img = go.AddComponent<Image>();
        RuntimeUiPolish.UseRoundedSliced(img);
        img.color = new Color(0.16f, 0.2f, 0.28f, 0.92f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        RuntimeUiPolish.ApplyButtonTransitions(btn, img.color,
            Color.Lerp(img.color, Color.white, 0.15f),
            Color.Lerp(img.color, Color.black, 0.2f));
        btn.onClick.AddListener(LocalizationManager.CycleNext);

        var textGo = new GameObject("Text");
        var trt = textGo.AddComponent<RectTransform>();
        trt.SetParent(go.transform, false);
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(12f, 6f);
        trt.offsetMax = new Vector2(-12f, -6f);

        levelSelectLangTmp = textGo.AddComponent<TextMeshProUGUI>();
        levelSelectLangTmp.enableAutoSizing = true;
        levelSelectLangTmp.fontSizeMin = Mathf.Max(12, UiTypography.Scale(14));
        levelSelectLangTmp.fontSizeMax = UiTypography.Scale(tablet ? 26 : 22);
        levelSelectLangTmp.fontSize = levelSelectLangTmp.fontSizeMax;
        levelSelectLangTmp.alignment = TextAlignmentOptions.Center;
        levelSelectLangTmp.color = new Color(0.93f, 0.96f, 1f, 1f);
        levelSelectLangTmp.textWrappingMode = TextWrappingModes.Normal;
        levelSelectLangTmp.overflowMode = TextOverflowModes.Overflow;
        CopyFontFromAny(levelSelectLangTmp);
        RefreshLevelSelectLanguageLabel();
    }

    private static void CopyFontFromAny(TextMeshProUGUI target)
    {
        UiTypography.ApplyDefaultFontAsset(target);
        if (target.font == null)
        {
            var any = FindAnyObjectByType<TextMeshProUGUI>();
            if (any != null && any != target && any.font != null)
            {
                target.font = any.font;
                if (any.fontSharedMaterial != null)
                    target.fontSharedMaterial = any.fontSharedMaterial;
            }
        }

        if (target != null)
            target.fontStyle = FontStyles.Bold;
    }

    /// <returns>Label <see cref="TextMeshProUGUI"/> for live language updates.</returns>
    private TextMeshProUGUI CreateLevelButton(Transform parent, string label, UnityAction onClick, Color? backgroundOverride = null)
    {
        var go = new GameObject("LevelButton");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);

        bool tablet = DeviceLayout.IsTabletLike();
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = tablet ? 100f : 92f;
        le.minHeight = tablet ? 100f : 92f;

        Color bg = backgroundOverride ?? buttonColor;
        var img = go.AddComponent<Image>();
        RuntimeUiPolish.UseRoundedSliced(img);
        img.color = bg;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        RuntimeUiPolish.ApplyButtonTransitions(btn, bg,
            RuntimeUiPolish.ButtonNeutralHover,
            new Color(0.12f, 0.13f, 0.16f, 1f));
        RuntimeUiPolish.ApplyDropShadow(rt, new Vector2(1.5f, -2.5f), 0.2f);

        var textGo = new GameObject("Text");
        var trt = textGo.AddComponent<RectTransform>();
        trt.SetParent(go.transform, false);
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(20f, 8f);
        trt.offsetMax = new Vector2(-20f, -8f);

        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = Mathf.Max(14, UiTypography.Scale(17));
        tmp.fontSizeMax = UiTypography.Scale(tablet ? 32 : 30);
        tmp.fontSize = tmp.fontSizeMax;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = RuntimeUiPolish.TitleIvory;
        CopyFontFromAny(tmp);
        LocalizationManager.ApplyTextDirection(tmp);

        btn.onClick.AddListener(onClick);
        return tmp;
    }

    private void CreateBackButton(Transform parent)
    {
        bool tablet = DeviceLayout.IsTabletLike();
        var go = new GameObject("BackButton");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        float margin = tablet ? 20f : 16f;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = Vector2.zero;
        rt.sizeDelta = new Vector2(tablet ? 300f : 272f, tablet ? 64f : 58f);
        rt.anchoredPosition = new Vector2(margin, margin + 4f);

        var img = go.AddComponent<Image>();
        RuntimeUiPolish.UseRoundedSliced(img);
        img.color = buttonColor;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        RuntimeUiPolish.ApplyButtonTransitions(btn, buttonColor,
            RuntimeUiPolish.ButtonNeutralHover,
            new Color(0.12f, 0.13f, 0.16f, 1f));
        RuntimeUiPolish.ApplyDropShadow(rt, new Vector2(1.5f, -2.5f), 0.22f);

        var textGo = new GameObject("Text");
        var trt = textGo.AddComponent<RectTransform>();
        trt.SetParent(go.transform, false);
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(10f, 6f);
        trt.offsetMax = new Vector2(-10f, -6f);

        backTmp = textGo.AddComponent<TextMeshProUGUI>();
        backTmp.text = LocalizationManager.Get("ui.back_menu", "Back to Menu");
        backTmp.fontSize = UiTypography.Scale(tablet ? 32 : 28);
        backTmp.fontStyle = FontStyles.Bold;
        backTmp.alignment = TextAlignmentOptions.Center;
        backTmp.color = RuntimeUiPolish.TitleIvory;
        CopyFontFromAny(backTmp);
        backTmp.fontStyle = FontStyles.Bold;
        LocalizationManager.ApplyTextDirection(backTmp);

        btn.onClick.AddListener(() => SceneTransitionHost.LoadSingleScene("Menu"));
    }

    private void ClearLevelScrollContent()
    {
        if (_levelContentRt == null)
            return;
        for (int i = _levelContentRt.childCount - 1; i >= 0; i--)
            Destroy(_levelContentRt.GetChild(i).gameObject);
        categoryRowLabels.Clear();
        levelRowLabels.Clear();
        backToCategoriesTmp = null;
    }

    private void UpdateLevelScrollMinHeight(int rowCount)
    {
        if (_levelContentLayoutElement == null)
            return;
        _levelContentLayoutElement.minHeight =
            rowCount * _levelRowHeight + Mathf.Max(0, rowCount - 1) * _levelRowSpacing + _levelContentPadVertical;
    }

    /// <param name="scrollToTop">When true, snap scroll after rebuild (e.g. returning from a submenu).</param>
    private void BuildCategoryView(bool scrollToTop)
    {
        _levelPickMode = false;
        _selectedCategoryIndex = -1;
        ClearLevelScrollContent();

        for (int c = 0; c < GameLevelCatalog.SelectCategories.Length; c++)
        {
            int catIdx = c;
            var cat = GameLevelCatalog.SelectCategories[c];
            var tmp = CreateLevelButton(
                _levelContentRt,
                LocalizationManager.Get(cat.TitleLocalizationKey, cat.DefaultTitle),
                () => OpenCategory(catIdx));
            categoryRowLabels.Add(tmp);
        }

        UpdateLevelScrollMinHeight(GameLevelCatalog.SelectCategories.Length);
        RefreshLocalizedStrings();
        RebuildScrollContent(_levelScroll, _levelContentRt);
        if (scrollToTop && _levelScroll != null)
            _levelScroll.verticalNormalizedPosition = 1f;
        if (isActiveAndEnabled && _levelScroll != null && _levelContentRt != null)
            StartCoroutine(FinalizeScrollAfterTmpLayout(_levelScroll, _levelContentRt));
    }

    private void OpenCategory(int categoryIndex)
    {
        if (categoryIndex < 0 || categoryIndex >= GameLevelCatalog.SelectCategories.Length)
            return;

        var cat = GameLevelCatalog.SelectCategories[categoryIndex];
        BuildLevelViewForCategory(categoryIndex, cat.FirstLevelIndex, cat.LastLevelIndexInclusive);
    }

    private void BuildLevelViewForCategory(int categoryIndex, int firstLevel, int lastLevel)
    {
        _levelPickMode = true;
        _selectedCategoryIndex = categoryIndex;
        _levelViewStart = firstLevel;
        _levelViewEnd = lastLevel;
        ClearLevelScrollContent();

        Color backTint = new Color(0.15f, 0.17f, 0.24f, 0.96f);
        backToCategoriesTmp = CreateLevelButton(
            _levelContentRt,
            LocalizationManager.Get("ui.level_select.back_categories", "All categories"),
            () => BuildCategoryView(true),
            backTint);

        int n = lastLevel - firstLevel + 1;
        for (int i = 0; i < n; i++)
        {
            int idx = firstLevel + i;
            int capture = idx;
            var rowTmp = CreateLevelButton(
                _levelContentRt,
                GameLevelCatalog.GetLocalizedDisplayName(capture),
                () => StartGameAt(capture));
            levelRowLabels.Add(rowTmp);
        }

        UpdateLevelScrollMinHeight(n + 1);
        RefreshLocalizedStrings();
        RebuildScrollContent(_levelScroll, _levelContentRt);
        if (_levelScroll != null)
            _levelScroll.verticalNormalizedPosition = 1f;
        if (isActiveAndEnabled && _levelScroll != null && _levelContentRt != null)
            StartCoroutine(FinalizeScrollAfterTmpLayout(_levelScroll, _levelContentRt));
    }

    private void StartGameAt(int index)
    {
        if (GameLevelCatalog.IsComingSoonLevel(index))
        {
            ShowComingSoonOverlay();
            return;
        }

        LevelSelection.SetSelectedLevel(index);
        SceneTransitionHost.LoadSingleScene("Game");
    }

    private const string ComingSoonOverlayName = "ComingSoonOverlayRoot";

    /// <summary>Full-screen message for catalog levels not yet playable (e.g. Hohmann transfer).</summary>
    private void ShowComingSoonOverlay()
    {
        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
            return;

        var existing = canvas.transform.Find(ComingSoonOverlayName);
        if (existing != null)
            Destroy(existing.gameObject);

        var root = new GameObject(ComingSoonOverlayName);
        var rootRt = root.AddComponent<RectTransform>();
        rootRt.SetParent(canvas.transform, false);
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;
        rootRt.SetAsLastSibling();

        var dim = root.AddComponent<Image>();
        dim.color = new Color(0.04f, 0.05f, 0.1f, 0.88f);
        dim.raycastTarget = true;

        var dimBtn = root.AddComponent<Button>();
        dimBtn.targetGraphic = dim;
        dimBtn.transition = Selectable.Transition.None;
        dimBtn.onClick.AddListener(() => Destroy(root));

        bool tablet = DeviceLayout.IsTabletLike();
        var panelGo = new GameObject("Panel");
        var panelRt = panelGo.AddComponent<RectTransform>();
        panelRt.SetParent(root.transform, false);
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(tablet ? 560f : 480f, tablet ? 200f : 176f);

        var panelImg = panelGo.AddComponent<Image>();
        RuntimeUiPolish.UseRoundedSliced(panelImg);
        panelImg.color = new Color(0.12f, 0.14f, 0.2f, 0.98f);
        panelImg.raycastTarget = true;

        var panelBtn = panelGo.AddComponent<Button>();
        panelBtn.targetGraphic = panelImg;
        panelBtn.transition = Selectable.Transition.None;
        panelBtn.onClick.AddListener(() => Destroy(root));

        var msgGo = new GameObject("Message");
        var msgRt = msgGo.AddComponent<RectTransform>();
        msgRt.SetParent(panelGo.transform, false);
        msgRt.anchorMin = new Vector2(0.06f, 0.28f);
        msgRt.anchorMax = new Vector2(0.94f, 0.92f);
        msgRt.offsetMin = Vector2.zero;
        msgRt.offsetMax = Vector2.zero;

        var msgTmp = msgGo.AddComponent<TextMeshProUGUI>();
        msgTmp.text = LocalizationManager.Get("ui.coming_soon", "Coming soon!");
        msgTmp.fontSize = UiTypography.Scale(tablet ? 36 : 30);
        msgTmp.alignment = TextAlignmentOptions.Center;
        msgTmp.color = RuntimeUiPolish.TitleIvory;
        msgTmp.fontStyle = FontStyles.Bold;
        msgTmp.raycastTarget = false;
        CopyFontFromAny(msgTmp);
        LocalizationManager.ApplyTextDirection(msgTmp);

        var okGo = new GameObject("DismissHint");
        var okRt = okGo.AddComponent<RectTransform>();
        okRt.SetParent(panelGo.transform, false);
        okRt.anchorMin = new Vector2(0.06f, 0.08f);
        okRt.anchorMax = new Vector2(0.94f, 0.26f);
        okRt.offsetMin = Vector2.zero;
        okRt.offsetMax = Vector2.zero;

        var okTmp = okGo.AddComponent<TextMeshProUGUI>();
        okTmp.text = LocalizationManager.Get("ui.coming_soon_tap", "Tap anywhere to close");
        okTmp.fontSize = UiTypography.Scale(tablet ? 22 : 19);
        okTmp.alignment = TextAlignmentOptions.Center;
        okTmp.color = new Color(0.75f, 0.8f, 0.92f, 0.85f);
        okTmp.raycastTarget = false;
        CopyFontFromAny(okTmp);
        okTmp.fontStyle = FontStyles.Italic;
        LocalizationManager.ApplyTextDirection(okTmp);
    }
}
