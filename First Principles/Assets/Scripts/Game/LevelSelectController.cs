using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
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
    private TextMeshProUGUI mathTipsTmp;
    private TextMeshProUGUI backTmp;
    private TextMeshProUGUI graphicCalculatorEntryTmp;
    private TextMeshProUGUI levelSelectLangTmp;
    private readonly System.Collections.Generic.List<TextMeshProUGUI> levelRowLabels = new System.Collections.Generic.List<TextMeshProUGUI>();

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
            titleTmp.text = LocalizationManager.Get("ui.choose_stage", "Choose a graph stage");
            LocalizationManager.ApplyTextDirection(titleTmp);
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
        if (graphicCalculatorEntryTmp != null)
        {
            graphicCalculatorEntryTmp.text = LocalizationManager.Get("ui.graphic_calculator_mode", "Graphic calculator mode");
            LocalizationManager.ApplyTextDirection(graphicCalculatorEntryTmp);
        }

        for (int i = 0; i < levelRowLabels.Count; i++)
        {
            var tmp = levelRowLabels[i];
            if (tmp == null)
                continue;
            tmp.text = GameLevelCatalog.GetLocalizedDisplayName(i);
            LocalizationManager.ApplyTextDirection(tmp);
        }

        RefreshLevelSelectLanguageLabel();
    }

    private void RefreshLevelSelectLanguageLabel()
    {
        if (levelSelectLangTmp == null)
            return;
        levelSelectLangTmp.text =
            $"{LocalizationManager.Get("ui.language", "Language")}: {LocalizationManager.GetLanguagePickerLabel(LocalizationManager.CurrentLanguage)}";
        LocalizationManager.ApplyTextDirection(levelSelectLangTmp);
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
        titleTmp.text = LocalizationManager.Get("ui.choose_stage", "Choose a graph stage");
        titleTmp.fontSize = tablet ? 52 : 46;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = RuntimeUiPolish.TitleIvory;
        CopyFontFromAny(titleTmp);
        LocalizationManager.ApplyTextDirection(titleTmp);

        CreateMathArticlesButton(panel.transform, canvas.transform);
        CreateLevelSelectLanguagePicker(panel.transform);

        // Scrollable list (many levels + readable on small screens).
        var scrollGo = new GameObject("LevelScroll");
        var scrollRt = scrollGo.AddComponent<RectTransform>();
        scrollRt.SetParent(panel.transform, false);
        scrollRt.anchorMin = DeviceLayout.LevelSelectScrollAnchorMin;
        scrollRt.anchorMax = DeviceLayout.LevelSelectScrollAnchorMax;
        scrollRt.offsetMin = Vector2.zero;
        scrollRt.offsetMax = Vector2.zero;

        var scroll = scrollGo.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
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

        var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.spacing = tablet ? 20f : 18f;
        vlg.padding = new RectOffset(tablet ? 16 : 12, tablet ? 16 : 12, tablet ? 14 : 12, tablet ? 14 : 12);
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;

        var fitter = contentGo.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        graphicCalculatorEntryTmp = CreateLevelButton(contentRt, LocalizationManager.Get("ui.graphic_calculator_mode", "Graphic calculator mode"), LaunchGraphCalculator);

        for (int i = 0; i < GameLevelCatalog.LevelCount; i++)
        {
            int idx = i;
            var rowTmp = CreateLevelButton(contentRt, GameLevelCatalog.GetLocalizedDisplayName(i), () => StartGameAt(idx));
            levelRowLabels.Add(rowTmp);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRt);
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRt);
        scroll.verticalNormalizedPosition = 1f;

        CreateBackButton(panel.transform);
    }

    private void CreateMathArticlesButton(Transform panelRoot, Transform canvasTransform)
    {
        var go = new GameObject("MathArticlesButton");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(panelRoot, false);
        bool tablet = DeviceLayout.IsTabletLike();
        rt.anchorMin = new Vector2(0.5f, tablet ? 0.81f : 0.8f);
        rt.anchorMax = new Vector2(0.5f, tablet ? 0.81f : 0.8f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(tablet ? 620f : 560f, tablet ? 68f : 62f);
        rt.anchoredPosition = Vector2.zero;

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
        trt.offsetMin = new Vector2(12f, 6f);
        trt.offsetMax = new Vector2(-12f, -6f);

        mathTipsTmp = textGo.AddComponent<TextMeshProUGUI>();
        mathTipsTmp.text = LocalizationManager.Get("ui.math_tips", "Math tips & snippets");
        mathTipsTmp.fontSize = tablet ? 32 : 28;
        mathTipsTmp.alignment = TextAlignmentOptions.Center;
        mathTipsTmp.color = new Color(0.9f, 0.96f, 1f, 1f);
        CopyFontFromAny(mathTipsTmp);
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
        rt.anchoredPosition = new Vector2(tablet ? 14f : 10f, tablet ? -8f : -6f);
        rt.sizeDelta = new Vector2(tablet ? 300f : 260f, tablet ? 44f : 40f);

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
        trt.offsetMin = new Vector2(8f, 4f);
        trt.offsetMax = new Vector2(-8f, -4f);

        levelSelectLangTmp = textGo.AddComponent<TextMeshProUGUI>();
        levelSelectLangTmp.fontSize = tablet ? 20 : 18;
        levelSelectLangTmp.alignment = TextAlignmentOptions.Center;
        levelSelectLangTmp.color = new Color(0.93f, 0.96f, 1f, 1f);
        levelSelectLangTmp.textWrappingMode = TextWrappingModes.Normal;
        CopyFontFromAny(levelSelectLangTmp);
        RefreshLevelSelectLanguageLabel();
    }

    private static void CopyFontFromAny(TextMeshProUGUI target)
    {
        var any = FindAnyObjectByType<TextMeshProUGUI>();
        if (any != null && any != target && any.font != null)
            target.font = any.font;
        if (target.font == null && TMP_Settings.defaultFontAsset != null)
            target.font = TMP_Settings.defaultFontAsset;
    }

    /// <returns>Label <see cref="TextMeshProUGUI"/> for live language updates.</returns>
    private TextMeshProUGUI CreateLevelButton(Transform parent, string label, UnityAction onClick)
    {
        var go = new GameObject("LevelButton");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);

        bool tablet = DeviceLayout.IsTabletLike();
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = tablet ? 92f : 84f;
        le.minHeight = tablet ? 92f : 84f;

        var img = go.AddComponent<Image>();
        RuntimeUiPolish.UseRoundedSliced(img);
        img.color = buttonColor;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        RuntimeUiPolish.ApplyButtonTransitions(btn, buttonColor,
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
        tmp.fontSize = tablet ? 32 : 30;
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
        rt.anchorMin = new Vector2(0.5f, tablet ? 0.048f : 0.055f);
        rt.anchorMax = new Vector2(0.5f, tablet ? 0.048f : 0.055f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(tablet ? 300f : 260f, tablet ? 64f : 58f);
        rt.anchoredPosition = Vector2.zero;

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
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        backTmp = textGo.AddComponent<TextMeshProUGUI>();
        backTmp.text = LocalizationManager.Get("ui.back_menu", "Back to Menu");
        backTmp.fontSize = tablet ? 30 : 26;
        backTmp.alignment = TextAlignmentOptions.Center;
        backTmp.color = RuntimeUiPolish.TitleIvory;
        CopyFontFromAny(backTmp);
        LocalizationManager.ApplyTextDirection(backTmp);

        btn.onClick.AddListener(() => StartCoroutine(LoadSceneRoutine("Menu")));
    }

    private void StartGameAt(int index)
    {
        LevelSelection.SetSelectedLevel(index);
        StartCoroutine(LoadSceneRoutine("Game"));
    }

    private static void LaunchGraphCalculator()
    {
        GraphCalculatorSession.RequestEnterFromMenu();
        var ctrl = FindAnyObjectByType<LevelSelectController>();
        if (ctrl != null)
            ctrl.StartCoroutine(ctrl.LoadSceneRoutine("Game"));
        else
            SceneManager.LoadScene("Game");
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        var canvas = FindAnyObjectByType<Canvas>();
        var parentRt = canvas != null
            ? MobileUiRoots.GetSafeContentParent(canvas.transform) as RectTransform ?? canvas.transform as RectTransform
            : null;

        GameObject veil = null;
        if (parentRt != null)
        {
            veil = new GameObject("SceneLoadingVeil");
            var vrt = veil.AddComponent<RectTransform>();
            vrt.SetParent(parentRt, false);
            vrt.SetAsLastSibling();
            vrt.anchorMin = Vector2.zero;
            vrt.anchorMax = Vector2.one;
            vrt.offsetMin = Vector2.zero;
            vrt.offsetMax = Vector2.zero;

            var dim = veil.AddComponent<Image>();
            dim.color = new Color(0.05f, 0.06f, 0.09f, 0.88f);
            dim.raycastTarget = true;

            var labelGo = new GameObject("LoadingText");
            var lrt = labelGo.AddComponent<RectTransform>();
            lrt.SetParent(veil.transform, false);
            lrt.anchorMin = new Vector2(0.5f, 0.5f);
            lrt.anchorMax = new Vector2(0.5f, 0.5f);
            lrt.sizeDelta = new Vector2(720f, 120f);
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = LocalizationManager.Get("ui.loading", "Loading…");
            tmp.fontSize = 40;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.94f, 0.96f, 1f, 0.96f);
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.raycastTarget = false;
            CopyFontFromAny(tmp);
            LocalizationManager.ApplyTextDirection(tmp);
        }

        yield return AsyncSceneLoader.LoadCoroutine(sceneName);
    }
}
