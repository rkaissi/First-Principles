using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// =============================================================================
// LevelManager — Game scene: curriculum levels, graph theme, platformer world
// =============================================================================
// Flow (single-scene Game):
//   1. Start() → SetupReferences() finds FunctionPlotter / renderers / plane, adds
//      GraphObstacleGenerator & DerivativePopAnimator if missing, creates runtime UI
//      (ObstaclesRoot, player, story TMP, HUD, Math concepts overlay button), Riemann helper, touch.
//   2. BuildSampleLevels() populates `levels`. CRITICAL: index order must match
//      GameLevelCatalog.DisplayNames (level select uses the same indices).
//   3. LoadLevel(i) → ApplyLevelTheme(def) pushes params into FunctionPlotter and HUD
//      state, then LoadLevelFullRoutine: world build → optional StageIntroOverlay roleplay
//      “page” → story banner fade (player can move during fade).
//   4. Update() advances nextStageIndex when PlayerCenterGrid.x crosses
//      stageTriggerXGrid[k], firing DerivativePopAnimator.
// Dependencies: Cartesian plane RectTransform, Canvas with CanvasSafeAreaBootstrap for mobile.
// =============================================================================

/// <summary>
/// Central coordinator for the playable “graph as level” mode on the Game scene.
/// </summary>
public class LevelManager : MonoBehaviour
{
    // --- Serialized tuning ---
    [Header("Stage Pops")]
    [SerializeField] private int defaultStageCount = 3;

    [Header("Player Death / Restart")]
    [SerializeField] private float deathMinYGrid = -2f;
    [SerializeField] private float restartDelaySeconds = 0.15f;

    private FunctionPlotter functionPlotter;
    private LineRendererUI curveRenderer;
    private DerivRendererUI derivRenderer;
    private GridRendererUI gridRenderer;
    private RectTransform cartesianPlaneRect;

    private GraphObstacleGenerator obstacleGenerator;
    private PlayerControllerUI2D playerController;
    private DerivativePopAnimator popAnimator;
    private RiemannStripRendererUI riemannRenderer;

    private RectTransform obstaclesRoot;

    private TextMeshProUGUI storyText;
    private TextMeshProUGUI stageHudText;
    private TextMeshProUGUI controlsHintText;
    private int lastStageHudKey = int.MinValue;
    private Sprite cachedHudPanelSprite;
    private float storyMiddlePauseSeconds = 1.65f;

    private bool gridThemeBaselineCaptured;
    private Color savedGridCenterLine;
    private Color savedGridOutsideLine;

    /// <summary>Runtime-built list of stages (also representable as LevelDefinition assets).</summary>
    private readonly List<LevelDefinition> levels = new List<LevelDefinition>();
    private int currentLevelIndex;

    /// <summary>How many stage boundary thresholds the player has already crossed (derivative pops).</summary>
    private int nextStageIndex;
    private List<float> stageTriggerXGrid;
    private List<Color> stagePopColors;

    private bool isRestarting;
    private Coroutine storyFadeRoutine;
    private Coroutine levelFlowRoutine;
    /// <summary>After death-restart, skip the full-screen roleplay card for the same stage.</summary>
    private bool skipNextStageIntro;

    private GameObject stageIntroRoot;
    private CanvasGroup stageIntroCanvasGroup;
    private TextMeshProUGUI stageIntroTitle;
    private TextMeshProUGUI stageIntroBody;
    private TextMeshProUGUI stageIntroHint;
    private bool stageIntroSkipRequested;

    /// <summary>Graphing calculator mode: transforms, scale zoom, pinch — no platformer.</summary>
    private bool graphCalculatorMode;

    private void Awake()
    {
        // Keep things single-scene: if another instance somehow appears, destroy it.
        if (FindObjectsByType<LevelManager>().Length > 1)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        LocalizationManager.LanguageChanged += OnLocalizationChanged;
    }

    private void OnDisable()
    {
        LocalizationManager.LanguageChanged -= OnLocalizationChanged;
    }

    private void OnLocalizationChanged()
    {
        RefreshControlsHintLocalized();
        RefreshMathConceptsLabelLocalized();
        RefreshSceneFooterLocalized();
        RefreshStageHudLocalizedForce();

        if (graphCalculatorMode)
            RefreshStoryBannerForCurrentMode(null);
        else if (levels.Count > 0 && currentLevelIndex >= 0 && currentLevelIndex < levels.Count)
            RefreshStoryBannerForCurrentMode(levels[currentLevelIndex]);
    }

    private void RefreshControlsHintLocalized()
    {
        if (controlsHintText == null)
            return;

        if (graphCalculatorMode)
        {
            controlsHintText.text = LocalizationManager.Get("controls.calculator",
                "<color=#7a8399>Graphing calculator</color>  <b>Type f(u)</b>  ·  <b>Trans</b>  ·  <b>Scale</b>  ·  <b>Pinch</b>  ·  <b>Back</b>");
        }
        else if (DeviceLayout.PreferOnScreenGameControls)
        {
            controlsHintText.text = LocalizationManager.Get("controls.mobile",
                "<color=#7a8399>Move</color>  <b><color=#ffd978>\u25C0 \u25B6</color></b>  <color=#5c6577>\u00b7</color>  <color=#7a8399>Jump</color>  <b><color=#ffd978>tap</color></b>  <size=90%><color=#5c6577>(keyboard: arrows / Space)</color></size>");
        }
        else
        {
            controlsHintText.text = LocalizationManager.Get("controls.desktop",
                "<color=#7a8399>Move</color>  <b><color=#ffd978>\u2190</color></b>  <b><color=#ffd978>\u2192</color></b>  <color=#5c6577>\u00b7</color>  <color=#7a8399>Jump</color>  <b><color=#ffd978>Space</color></b>");
        }

        LocalizationManager.ApplyTextDirection(controlsHintText);
    }

    private void RefreshMathConceptsLabelLocalized()
    {
        var go = GameObject.Find("MathConceptsButton");
        if (go == null)
            return;
        var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp == null)
            return;
        tmp.text = LocalizationManager.Get("ui.math_concepts", "Math concepts");
        LocalizationManager.ApplyTextDirection(tmp);
    }

    private void RefreshSceneFooterLocalized()
    {
        var go = GameObject.Find("SceneCreditsFooter");
        if (go == null)
            return;
        var tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp == null)
            return;
        tmp.text = SceneCreditsFooter.BuildCompactRichText();
        LocalizationManager.ApplyTextDirection(tmp);
    }

    private void RefreshStageHudLocalizedForce()
    {
        lastStageHudKey = int.MinValue;
        RefreshStageHud();
    }

    private void RefreshStoryBannerForCurrentMode(LevelDefinition def)
    {
        if (storyText == null)
            return;

        if (graphCalculatorMode || def == null)
        {
            storyText.text = TmpLatex.Process(LocalizationManager.Get("graph.calculator_intro",
                "<b>Graphing calculator mode</b>\n" +
                "<size=88%>Type almost any <b>f(u)</b> in the field (variable <b>x</b> in your formula); <b>Trans</b> adjusts A, k, C, D; <b>Scale</b> &amp; <b>pinch</b> zoom the window.</size>"));
            LocalizationManager.ApplyTextDirection(storyText);
            return;
        }

        string title = LocalizationManager.GetWithFallback($"level.{currentLevelIndex}", def.levelName);
        string story = LocalizationManager.GetWithFallback($"story.{currentLevelIndex}", def.storyText);
        if (GameLevelCatalog.IsAerospaceLevel(currentLevelIndex))
        {
            string dragPreamble = LocalizationManager.Get("aerospace.story_drag_polar_preamble",
                "<b><color=#c4b5fd>Drag polar refresher</color></b> (every <b>Aerospace</b> stage)\n" +
                "<size=92%><b>Parasitic (zero-lift) drag</b> — skin friction, form drag, interference lumped as <b>C<sub>D0</sub></b> in the parabolic model (roughly <i>not</i> the part that grows with lift).\n" +
                "<b>Induced drag</b> — the cost of making lift: trailing vortices add ~ <b>K C<sub>L</sub>²</b> (higher C<sub>L</sub> / tighter turns → more induced).\n" +
                "<b>Overall drag polar</b> — <b>C<sub>D</sub> = C<sub>D0</sub> + K C<sub>L</sub>²</b>: an upward-opening parabola in C<sub>L</sub>; min-drag <b>C<sub>L</sub></b> sits between “too slow / high α” and “too fast / low α” for real missions.</size>");
            story = $"{dragPreamble}\n\n{story}";
        }

        storyText.text = TmpLatex.Process($"<b>{title}</b>\n{story}");
        // Use RTL for Arabic / Urdu; Latin-heavy mixed math still renders with TMP bidi when possible.
        LocalizationManager.ApplyTextDirection(storyText);
    }

    private void Start()
    {
        graphCalculatorMode = GraphCalculatorSession.ConsumeEnterRequest();
        SetupReferences();
        if (graphCalculatorMode)
        {
            EnterGraphCalculatorMode();
            return;
        }

        BuildSampleLevels();
        // LevelSelect sets LevelSelection; opening Game directly falls back to index 0.
        int startIndex = LevelSelection.ConsumeSelectedLevel(levels.Count);
        LoadLevel(startIndex);
    }

    /// <summary>
    /// One-time wiring: graph components, obstacle generator, player, HUD, Riemann UI hook, touch bar.
    /// </summary>
    private void SetupReferences()
    {
        functionPlotter = FindAnyObjectByType<FunctionPlotter>();
        curveRenderer = FindAnyObjectByType<LineRendererUI>();
        derivRenderer = FindAnyObjectByType<DerivRendererUI>();
        gridRenderer = FindAnyObjectByType<GridRendererUI>();

        if (functionPlotter == null || curveRenderer == null || derivRenderer == null || gridRenderer == null)
        {
            Debug.LogError("LevelManager: Missing required graph components in the scene.");
            return;
        }

        var planeGo = GameObject.Find("Cartesian Plane");
        cartesianPlaneRect = planeGo != null ? planeGo.GetComponent<RectTransform>() : curveRenderer.GetComponent<RectTransform>();
        if (cartesianPlaneRect == null)
            cartesianPlaneRect = curveRenderer.GetComponent<RectTransform>();

        obstacleGenerator = GetComponent<GraphObstacleGenerator>();
        if (obstacleGenerator == null)
            obstacleGenerator = gameObject.AddComponent<GraphObstacleGenerator>();

        popAnimator = GetComponent<DerivativePopAnimator>();
        if (popAnimator == null)
            popAnimator = gameObject.AddComponent<DerivativePopAnimator>();
        popAnimator.SetTarget(derivRenderer);

        CreateObstaclesRootIfNeeded();
        CreatePlayerIfNeeded();
        CreateStoryTextIfNeeded();
        CreateGameplayHudIfNeeded();
        if (!graphCalculatorMode)
            HideLegacyGraphTuningButtons();
        EnsureRiemannRenderer();
        var mainCanvas = FindAnyObjectByType<Canvas>();
        if (mainCanvas != null && !graphCalculatorMode)
            MobileTouchControls.EnsureForGameCanvas(mainCanvas.transform);

        // Wire callbacks.
        playerController.SetDeathCallback(RestartCurrentLevel);
        playerController.SetFinishCallback(AdvanceLevel);
        ConfigureGameBackButtonDestination();
    }

    /// <summary>
    /// Graphing calculator is entered from the main menu only — back returns to <b>Menu</b>.
    /// Levels use back to <b>LevelSelect</b> (scene default is replaced here for clarity).
    /// </summary>
    private void ConfigureGameBackButtonDestination()
    {
        var backGo = GameObject.Find("BackButton");
        if (backGo == null)
            return;
        var btn = backGo.GetComponent<Button>();
        if (btn == null)
            return;

        btn.onClick.RemoveAllListeners();
        var fader = FindAnyObjectByType<SceneFader>();
        if (graphCalculatorMode)
        {
            if (fader != null)
                btn.onClick.AddListener(fader.LoadMenu);
            else
                btn.onClick.AddListener(() => SceneManager.LoadScene("Menu"));
        }
        else
        {
            if (fader != null)
                btn.onClick.AddListener(fader.LoadLevelSelect);
            else
                btn.onClick.AddListener(() => SceneManager.LoadScene("LevelSelect"));
        }
    }

    /// <summary>
    /// Free graphing workspace (graphing calculator mode).
    /// Legacy <c>TransButton</c> / <c>ScaleButton</c> are shown; pinch zoom applies on the graph window.
    /// </summary>
    private void EnterGraphCalculatorMode()
    {
        levels.Clear();
        currentLevelIndex = 0;
        stageTriggerXGrid = new List<float>();
        stagePopColors = new List<Color>();
        nextStageIndex = 0;

        if (obstaclesRoot != null)
            obstaclesRoot.gameObject.SetActive(false);

        if (playerController != null)
            playerController.gameObject.SetActive(false);

        if (gridRenderer != null && !gridThemeBaselineCaptured)
        {
            savedGridCenterLine = gridRenderer.centerLine;
            savedGridOutsideLine = gridRenderer.outsideLine;
            gridThemeBaselineCaptured = true;
        }

        if (gridRenderer != null)
        {
            gridRenderer.centerLine = savedGridCenterLine;
            gridRenderer.outsideLine = savedGridOutsideLine;
            gridRenderer.enabled = false;
            gridRenderer.enabled = true;
        }

        if (storyFadeRoutine != null)
        {
            StopCoroutine(storyFadeRoutine);
            storyFadeRoutine = null;
        }

        if (levelFlowRoutine != null)
        {
            StopCoroutine(levelFlowRoutine);
            levelFlowRoutine = null;
        }

        if (storyText != null)
        {
            storyText.gameObject.SetActive(true);
            RefreshStoryBannerForCurrentMode(null);
            storyText.color = new Color(1f, 1f, 1f, 0.94f);
        }

        if (stageHudText != null && stageHudText.transform.parent != null)
            stageHudText.transform.parent.gameObject.SetActive(false);

        functionPlotter.transA = 1f;
        functionPlotter.transK = 1f;
        functionPlotter.transC = 0f;
        functionPlotter.transD = 0f;
        functionPlotter.power = 2;
        functionPlotter.baseN = 2;
        functionPlotter.differentiate = false;
        functionPlotter.xStart = -12f;
        functionPlotter.xEnd = 12f;
        functionPlotter.step = 0.06f;
        functionPlotter.SetEquationExtraSuffix("");
        functionPlotter.SetCustomExpression("x^2");

        if (curveRenderer != null)
        {
            curveRenderer.color = new Color(0.95f, 0.8f, 0.38f, 1f);
            curveRenderer.enabled = false;
            curveRenderer.enabled = true;
        }

        if (derivRenderer != null)
        {
            derivRenderer.color = new Color(0.55f, 0.78f, 1f, 1f);
            derivRenderer.enabled = false;
        }

        if (riemannRenderer != null)
            riemannRenderer.ClearStrips();

        var canvas = FindAnyObjectByType<Canvas>();
        var safe = canvas != null ? MobileUiRoots.GetSafeContentParent(canvas.transform) as RectTransform : null;
        var hintParent = safe != null ? safe : canvas?.transform as RectTransform;
        float bridgeControls = DeviceLayout.PreferOnScreenGameControls ? DeviceLayout.TouchHintVerticalOffset : 22f;
        float transRowBottom = bridgeControls + 74f;

        GraphCalculatorEquationPanel.Ensure(hintParent, functionPlotter, FindPrimaryEquationTmp(), transRowBottom + 110f, 108f);

        var transGo = GameObject.Find("TransButton");
        var scaleGo = GameObject.Find("ScaleButton");
        LayoutCalculatorToolButtons(transGo, scaleGo, transRowBottom);

        TextMeshProUGUI paramHint = null;
        var legacyHint = GameObject.Find("FaxasGraphParamHint");
        if (legacyHint != null)
            legacyHint.name = "GraphicCalculatorParamHint";

        if (hintParent != null && GameObject.Find("GraphicCalculatorParamHint") == null)
        {
            var hintGo = new GameObject("GraphicCalculatorParamHint");
            var hrt = hintGo.AddComponent<RectTransform>();
            hrt.SetParent(hintParent, false);
            hrt.anchorMin = new Vector2(0.5f, 0f);
            hrt.anchorMax = new Vector2(0.5f, 0f);
            hrt.pivot = new Vector2(0.5f, 0f);
            bool tablet = DeviceLayout.IsTabletLike();
            float up = DeviceLayout.PreferOnScreenGameControls ? DeviceLayout.TouchHintVerticalOffset + 312f : 318f;
            hrt.anchoredPosition = new Vector2(0f, up);
            hrt.sizeDelta = new Vector2(tablet ? 1000f : 920f, tablet ? 128f : 118f);

            paramHint = hintGo.AddComponent<TextMeshProUGUI>();
            paramHint.richText = true;
            paramHint.textWrappingMode = TextWrappingModes.Normal;
            paramHint.fontSize = UiTypography.Scale(tablet ? 26 : 23);
            paramHint.alignment = TextAlignmentOptions.Top;
            paramHint.color = new Color(0.9f, 0.93f, 0.98f, 0.96f);
            ApplyPrimaryUiTypography(paramHint, FindPrimaryEquationTmp(), outlineWidth: 0.12f, outlineAlpha: 0.45f);
        }
        else if (GameObject.Find("GraphicCalculatorParamHint") != null)
            paramHint = GameObject.Find("GraphicCalculatorParamHint").GetComponent<TextMeshProUGUI>();

        foreach (var oldT in GetComponents<GraphCalculatorToolbar>())
            Destroy(oldT);
        foreach (var oldP in GetComponents<GraphPinchZoom>())
            Destroy(oldP);

        var toolbar = gameObject.AddComponent<GraphCalculatorToolbar>();
        toolbar.Configure(functionPlotter,
            transGo != null ? transGo.GetComponent<Button>() : null,
            scaleGo != null ? scaleGo.GetComponent<Button>() : null,
            paramHint);

        var pinch = gameObject.AddComponent<GraphPinchZoom>();
        pinch.Setup(functionPlotter);

        RefreshControlsHintLocalized();
    }

    private static void LayoutCalculatorToolButtons(GameObject transGo, GameObject scaleGo, float anchoredBottomY)
    {
        var canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
            return;

        var safe = MobileUiRoots.GetSafeContentParent(canvas.transform) as RectTransform;
        var parent = safe != null ? safe : canvas.transform as RectTransform;
        if (parent == null)
            return;

        bool tablet = DeviceLayout.IsTabletLike();
        float bottom = anchoredBottomY;
        float w = tablet ? 234f : 218f;
        float h = tablet ? 108f : 100f;

        if (transGo != null)
        {
            transGo.SetActive(true);
            var rt = transGo.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(tablet ? 20f : 14f, bottom);
        }

        if (scaleGo != null)
        {
            scaleGo.SetActive(true);
            var rt = scaleGo.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(tablet ? 20f + w + 14f : 14f + w + 10f, bottom);
        }
    }

    private void CreateObstaclesRootIfNeeded()
    {
        if (obstaclesRoot != null)
            return;

        obstaclesRoot = new GameObject("ObstaclesRoot", typeof(RectTransform)).GetComponent<RectTransform>();
        obstaclesRoot.SetParent(cartesianPlaneRect, false);
        obstaclesRoot.anchorMin = new Vector2(0, 0);
        obstaclesRoot.anchorMax = new Vector2(0, 0);
        obstaclesRoot.pivot = new Vector2(0, 0);
        obstaclesRoot.anchoredPosition = Vector2.zero;
        obstaclesRoot.sizeDelta = cartesianPlaneRect.rect.size;
    }

    private void CreatePlayerIfNeeded()
    {
        if (playerController != null)
            return;

        var playerGo = new GameObject("GraphPlayer");
        // Player must not be destroyed when we rebuild obstacles.
        playerGo.transform.SetParent(cartesianPlaneRect, false);
        playerGo.transform.localPosition = Vector3.zero;

        var rect = playerGo.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(0, 0);
        rect.pivot = new Vector2(0, 0);

        var img = playerGo.AddComponent<Image>();
        img.sprite = RuntimeUiPolish.SoftCharacterBlob != null ? RuntimeUiPolish.SoftCharacterBlob : TryGetSquareSprite();
        img.color = RuntimeUiPolish.PlayerBody;
        img.type = Image.Type.Simple;
        RuntimeUiPolish.ApplyDropShadow(rect, new Vector2(1f, -2f), 0.35f);

        playerController = playerGo.AddComponent<PlayerControllerUI2D>();
        playerController.BindVisual(rect, img);
        playerController.SetDeathMinYGrid(deathMinYGrid);

        var unitWidth = cartesianPlaneRect.rect.width / (float)gridRenderer.gridSize.x;
        var unitHeight = cartesianPlaneRect.rect.height / (float)gridRenderer.gridSize.y;
        playerController.SetGridToPixelUnits(unitWidth, unitHeight);
    }

    private void CreateStoryTextIfNeeded()
    {
        if (storyText != null)
            return;

        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("LevelManager: Could not find Canvas. Story text will not be created.");
            return;
        }

        var storyGo = new GameObject("StoryText");
        var safe = MobileUiRoots.GetSafeContentParent(canvas.transform);
        storyGo.transform.SetParent(safe != null ? safe : canvas.transform, false);

        var tmp = storyGo.AddComponent<TextMeshProUGUI>();
        tmp.text = "";
        tmp.fontSize = UiTypography.Scale(38);
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = UiTypography.Scale(22);
        tmp.fontSizeMax = UiTypography.Scale(42);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.richText = true;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.overflowMode = TextOverflowModes.Overflow;
        ApplyPrimaryUiTypography(tmp, FindPrimaryEquationTmp(), outlineWidth: 0.06f, outlineAlpha: 0.35f);

        var rt = tmp.rectTransform;
        rt.anchorMin = new Vector2(0.04f, 1f);
        rt.anchorMax = new Vector2(0.96f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0, -64f);
        rt.sizeDelta = new Vector2(0f, 280f);

        tmp.color = new Color(1f, 1f, 1f, 0f);

        storyText = tmp;
    }

    /// <summary>Hides old graph "Trans" / "Scale" tuning buttons so levels control parameters; gameplay uses arrows + jump.</summary>
    private static void HideLegacyGraphTuningButtons()
    {
        foreach (var name in new[] { "TransButton", "ScaleButton" })
        {
            var go = GameObject.Find(name);
            if (go != null)
                go.SetActive(false);
        }
    }

    private void CreateGameplayHudIfNeeded()
    {
        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
            return;

        var equationStyle = FindPrimaryEquationTmp();
        var panelSprite = GetHudPanelSprite();

        if (stageHudText == null)
        {
            var panelGo = new GameObject("StageHudPanel");
            var panelRt = panelGo.AddComponent<RectTransform>();
            var safe = MobileUiRoots.GetSafeContentParent(canvas.transform);
            panelRt.SetParent(safe != null ? safe : canvas.transform, false);
            panelRt.anchorMin = new Vector2(0f, 1f);
            panelRt.anchorMax = new Vector2(0f, 1f);
            panelRt.pivot = new Vector2(0f, 1f);
            float topPad = DeviceLayout.PreferOnScreenGameControls ? 12f : 20f;
            panelRt.anchoredPosition = new Vector2(18f, -topPad);
            panelRt.sizeDelta = new Vector2(380f, 80f);
            RuntimeUiPolish.ApplyDropShadow(panelRt, new Vector2(2f, -3f), 0.26f);

            var panelBg = panelGo.AddComponent<Image>();
            panelBg.sprite = panelSprite;
            panelBg.color = new Color(0.08f, 0.09f, 0.13f, 0.91f);
            panelBg.raycastTarget = false;
            panelBg.type = Image.Type.Sliced;
            // If sprite isn't 9-slice, Simple still works for a soft tile look.
            if (panelSprite != null && panelSprite.border.sqrMagnitude < 0.001f)
                panelBg.type = Image.Type.Simple;

            var accentGo = new GameObject("StageHudAccent");
            var accentRt = accentGo.AddComponent<RectTransform>();
            accentRt.SetParent(panelGo.transform, false);
            accentRt.anchorMin = new Vector2(0f, 1f);
            accentRt.anchorMax = new Vector2(1f, 1f);
            accentRt.pivot = new Vector2(0.5f, 1f);
            accentRt.anchoredPosition = Vector2.zero;
            accentRt.sizeDelta = new Vector2(0f, 3f);
            var accentImg = accentGo.AddComponent<Image>();
            accentImg.sprite = panelSprite;
            accentImg.color = new Color(0.92f, 0.55f, 0.42f, 0.95f);
            accentImg.raycastTarget = false;

            var textGo = new GameObject("StageHud");
            var textRt = textGo.AddComponent<RectTransform>();
            textRt.SetParent(panelGo.transform, false);
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(18f, 12f);
            textRt.offsetMax = new Vector2(-16f, -14f);

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.richText = true;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.fontSize = UiTypography.Scale(28);
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = UiTypography.Scale(18);
            tmp.fontSizeMax = UiTypography.Scale(30);
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = new Color(0.94f, 0.95f, 0.98f, 1f);
            tmp.characterSpacing = 0.35f;
            tmp.lineSpacing = -4f;
            ApplyPrimaryUiTypography(tmp, equationStyle, outlineWidth: 0.16f, outlineAlpha: 0.55f);
            tmp.text = FormatStageHudLine(1, 1);

            stageHudText = tmp;
        }

        CreateMathConceptsButtonIfNeeded(canvas, equationStyle);

        if (!graphCalculatorMode)
        {
            if (controlsHintText == null)
            {
                bool tabletUi = DeviceLayout.IsTabletLike();
                var barGo = new GameObject("ControlsHintPanel");
                var barRt = barGo.AddComponent<RectTransform>();
                var safe = MobileUiRoots.GetSafeContentParent(canvas.transform);
                barRt.SetParent(safe != null ? safe : canvas.transform, false);
                barRt.anchorMin = new Vector2(0.5f, 0f);
                barRt.anchorMax = new Vector2(0.5f, 0f);
                barRt.pivot = new Vector2(0.5f, 0f);
                float up = DeviceLayout.PreferOnScreenGameControls ? DeviceLayout.TouchHintVerticalOffset : 22f;
                barRt.anchoredPosition = new Vector2(0f, up);
                barRt.sizeDelta = new Vector2(tabletUi ? 900f : 760f, tabletUi ? 60f : 56f);

                var barBg = barGo.AddComponent<Image>();
                barBg.sprite = panelSprite;
                barBg.color = new Color(0.08f, 0.09f, 0.13f, 0.85f);
                barBg.raycastTarget = false;
                barBg.type = panelSprite != null && panelSprite.border.sqrMagnitude > 0.001f ? Image.Type.Sliced : Image.Type.Simple;

                var textGo = new GameObject("ControlsHint");
                var textRt = textGo.AddComponent<RectTransform>();
                textRt.SetParent(barGo.transform, false);
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.offsetMin = new Vector2(20f, 8f);
                textRt.offsetMax = new Vector2(-20f, -8f);

                var tmp = textGo.AddComponent<TextMeshProUGUI>();
                tmp.richText = true;
                tmp.textWrappingMode = TextWrappingModes.Normal;
                tmp.fontSize = UiTypography.Scale(24);
                tmp.alignment = TextAlignmentOptions.Midline;
                tmp.color = new Color(0.82f, 0.85f, 0.92f, 0.92f);
                tmp.characterSpacing = 0.25f;
                ApplyPrimaryUiTypography(tmp, equationStyle, outlineWidth: 0.14f, outlineAlpha: 0.5f);
                controlsHintText = tmp;
            }

            RefreshControlsHintLocalized();

            CreateSceneCreditsFooterStrip(canvas, equationStyle);
        }
    }

    /// <summary>Top-right control: opens <see cref="MathArticlesOverlay"/> (same body as level-select math tips).</summary>
    private void CreateMathConceptsButtonIfNeeded(Canvas canvas, TextMeshProUGUI equationStyle)
    {
        if (canvas == null || GameObject.Find("MathConceptsButton") != null)
            return;

        var safe = MobileUiRoots.GetSafeContentParent(canvas.transform);
        var parent = safe != null ? safe : canvas.transform;
        bool tablet = DeviceLayout.IsTabletLike();
        float topPad = DeviceLayout.PreferOnScreenGameControls ? 12f : 20f;

        var go = new GameObject("MathConceptsButton");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-18f, -topPad);
        rt.sizeDelta = new Vector2(tablet ? 248f : 220f, tablet ? 52f : 48f);

        var img = go.AddComponent<Image>();
        RuntimeUiPolish.UseRoundedSliced(img);
        img.color = RuntimeUiPolish.AccentTeal;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        RuntimeUiPolish.ApplyButtonTransitions(btn, RuntimeUiPolish.AccentTeal,
            Color.Lerp(RuntimeUiPolish.AccentTeal, Color.white, 0.18f),
            Color.Lerp(RuntimeUiPolish.AccentTeal, Color.black, 0.25f));
        btn.onClick.AddListener(() => MathArticlesOverlay.Open(canvas.transform));
        RuntimeUiPolish.ApplyDropShadow(rt, new Vector2(2f, -3f), 0.3f);

        var textGo = new GameObject("Text");
        var trt = textGo.AddComponent<RectTransform>();
        trt.SetParent(go.transform, false);
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(8f, 4f);
        trt.offsetMax = new Vector2(-8f, -4f);

        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = LocalizationManager.Get("ui.math_concepts", "Math concepts");
        tmp.fontSize = UiTypography.Scale(tablet ? 24 : 21);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.92f, 0.97f, 1f, 1f);
        tmp.raycastTarget = false;
        ApplyPrimaryUiTypography(tmp, equationStyle, outlineWidth: 0.14f, outlineAlpha: 0.48f);
        LocalizationManager.ApplyTextDirection(tmp);
    }

    private void CreateSceneCreditsFooterStrip(Canvas canvas, TextMeshProUGUI equationStyle)
    {
        if (canvas == null || GameObject.Find("SceneCreditsFooter") != null)
            return;

        bool tabletUi = DeviceLayout.IsTabletLike();
        var footerGo = new GameObject("SceneCreditsFooter");
        var footerRt = footerGo.AddComponent<RectTransform>();
        var safe = MobileUiRoots.GetSafeContentParent(canvas.transform);
        footerRt.SetParent(safe != null ? safe : canvas.transform, false);
        footerRt.anchorMin = new Vector2(0.5f, 0f);
        footerRt.anchorMax = new Vector2(0.5f, 0f);
        footerRt.pivot = new Vector2(0.5f, 0f);
        float controlsUp = DeviceLayout.PreferOnScreenGameControls ? DeviceLayout.TouchHintVerticalOffset : 22f;
        float barH = tabletUi ? 60f : 56f;
        footerRt.anchoredPosition = new Vector2(0f, controlsUp + barH + 12f);
        footerRt.sizeDelta = new Vector2(tabletUi ? 960f : 880f, 74f);

        var ftmp = footerGo.AddComponent<TextMeshProUGUI>();
        ftmp.text = SceneCreditsFooter.BuildCompactRichText();
        ftmp.richText = true;
        ftmp.textWrappingMode = TextWrappingModes.Normal;
        ftmp.fontSize = UiTypography.Scale(tabletUi ? 16 : 14);
        ftmp.alignment = TextAlignmentOptions.Bottom;
        ftmp.color = new Color(0.88f, 0.89f, 0.92f, 0.9f);
        ftmp.raycastTarget = false;
        ApplyPrimaryUiTypography(ftmp, equationStyle, outlineWidth: 0.1f, outlineAlpha: 0.45f);
        LocalizationManager.ApplyTextDirection(ftmp);
    }

    /// <summary>The big equation label in <c>Game</c> — used as the typography reference for all gameplay HUD copy.</summary>
    private static TextMeshProUGUI FindPrimaryEquationTmp()
    {
        var go = GameObject.Find("Equation");
        if (go != null)
        {
            var t = go.GetComponent<TextMeshProUGUI>();
            if (t != null && t.font != null)
                return t;
        }

        foreach (var t in FindObjectsByType<TextMeshProUGUI>())
        {
            if (t != null && t.font != null && t.gameObject.CompareTag("EquationText"))
                return t;
        }

        return null;
    }

    private static void ApplyPrimaryUiTypography(TextMeshProUGUI target, TextMeshProUGUI reference, float outlineWidth = 0.14f, float outlineAlpha = 0.5f)
    {
        if (reference != null)
        {
            target.font = reference.font;
            if (reference.fontSharedMaterial != null)
                target.fontSharedMaterial = reference.fontSharedMaterial;
            target.fontStyle = reference.fontStyle;
        }
        else if (TMP_Settings.defaultFontAsset != null)
            target.font = TMP_Settings.defaultFontAsset;

        target.outlineWidth = outlineWidth;
        target.outlineColor = new Color(0f, 0f, 0f, outlineAlpha);
    }

    private static string FormatStageHudLine(int stage, int total)
    {
        string stageWord = LocalizationManager.Get("hud.stage", "STAGE");
        return
            $"<color=#9aa3b8><size=78%>{stageWord}</size></color>\n" +
            $"<b><color=#f2f4ff>{stage}</color></b><color=#5c6578> / </color><b><color=#e8ebf7>{total}</color></b>";
    }

    private void RefreshStageHud()
    {
        if (stageHudText == null || stageTriggerXGrid == null)
            return;

        int total = Mathf.Max(1, stageTriggerXGrid.Count + 1);
        int stage = Mathf.Clamp(nextStageIndex + 1, 1, total);
        int key = stage | (total << 16);
        if (key == lastStageHudKey)
            return;
        lastStageHudKey = key;
        stageHudText.text = FormatStageHudLine(stage, total);
        LocalizationManager.ApplyTextDirection(stageHudText);
    }

    /// <summary>Square / UI sprite for flat panels (falls back to a tiny white sprite so Image always draws).</summary>
    private Sprite GetHudPanelSprite()
    {
        if (cachedHudPanelSprite != null)
            return cachedHudPanelSprite;

        if (RuntimeUiPolish.Rounded9Slice != null)
        {
            cachedHudPanelSprite = RuntimeUiPolish.Rounded9Slice;
            return cachedHudPanelSprite;
        }

        var fromScene = TryGetSquareSprite();
        if (fromScene != null)
        {
            cachedHudPanelSprite = fromScene;
            return cachedHudPanelSprite;
        }

        var tex = Texture2D.whiteTexture;
        cachedHudPanelSprite = Sprite.Create(
            tex,
            new Rect(0f, 0f, tex.width, tex.height),
            new Vector2(0.5f, 0.5f),
            100f);
        return cachedHudPanelSprite;
    }

    private Sprite TryGetSquareSprite()
    {
        // Prefer the existing scene sprite.
        var outsideSquare1 = GameObject.Find("OutsideSquare1");
        if (outsideSquare1 != null)
        {
            var img = outsideSquare1.GetComponent<Image>();
            if (img != null && img.sprite != null)
                return img.sprite;
        }

        // Fallback: first Image with a sprite.
        var allImages = FindObjectsByType<Image>();
        foreach (var img in allImages)
        {
            if (img != null && img.sprite != null)
                return img.sprite;
        }

        return null;
    }

    /// <summary>
    /// Finds or creates <see cref="RiemannStripRendererUI"/> under the cartesian plane, sibling-ordered under the main curve line.
    /// </summary>
    private void EnsureRiemannRenderer()
    {
        if (riemannRenderer == null)
            riemannRenderer = FindAnyObjectByType<RiemannStripRendererUI>();

        if (riemannRenderer != null || curveRenderer == null || cartesianPlaneRect == null)
            return;

        var go = new GameObject("RiemannStrips");
        go.transform.SetParent(cartesianPlaneRect, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        riemannRenderer = go.AddComponent<RiemannStripRendererUI>();
        riemannRenderer.raycastTarget = false;

        int lineIdx = curveRenderer.transform.GetSiblingIndex();
        go.transform.SetSiblingIndex(lineIdx);
    }

    /// <summary>
    /// Constructs all built-in <see cref="LevelDefinition"/> instances. When adding a level:
    /// append to <see cref="GameLevelCatalog.DisplayNames"/> with the SAME index, then add a
    /// matching <c>levels.Add(MakeLevel(...))</c> block here using that display name index.
    /// </summary>
    private void BuildSampleLevels()
    {
        levels.Clear();

        var primerStageColors = new[]
        {
            new Color(0.78f, 0.62f, 1f, 1f),
            new Color(1f, 0.84f, 0.4f, 1f),
            new Color(0.5f, 0.86f, 1f, 1f),
            new Color(1f, 0.55f, 0.72f, 1f)
        };

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[0],
            FunctionType.Power,
            curveColor: new Color(0.96f, 0.82f, 0.45f, 1f),
            derivativeColor: new Color(0.55f, 0.78f, 1f, 1f),
            transA: 0.38f,
            transK: 0.42f,
            transC: -1.85f,
            transD: 0f,
            power: 2,
            baseN: 2,
            story:
                "<color=#c4b5fd><b>Derivative</b></color> = slope of the tangent — how fast <b>f(x)</b> rises or falls at each step.\n\n" +
                "<color=#fde047>Gold light</color> traces your path; <color=#7dd3fc>ice-blue</color> is f'(x) sculpting <i>where the floor exists</i>.\n\n" +
                "<size=92%><color=#a8b2d1>Where f'(x) clears the rule, platforms hold; where it falls short, the void opens. Each bright <b>pop</b> is another act in the analysis you're walking through.</color></size>\n\n" +
                "<size=88%><color=#94a3b8><b>First principles (business habit):</b> like builders at Tesla / SpaceX-style talks — peel analogy until you hit bedrock facts, then reason upward. Here, <b>f(x)</b> is your outcome model; <b>f'(x)</b> is where small input changes move the outcome fastest. Open <b>Math tips & snippets → First principles thinking</b> or <b>docs/first-principles-business.md</b> for the map.</color></size>",
            derivativePopTriggerCountOverride: 4,
            applyGridTheming: true,
            gridCenter: new Color(0.55f, 0.45f, 0.92f, 0.4f),
            gridOutside: new Color(0.4f, 0.36f, 0.62f, 0.11f),
            levelStageColors: primerStageColors,
            storyPauseSecondsOverride: 2.95f
        ));

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[1],
            FunctionType.Power,
            curveColor: new Color(0.9f, 0.3f, 1f, 1f),
            derivativeColor: new Color(1f, 0.76f, 0.1f, 1f),
            transA: 1f,
            transK: 0.35f,
            transC: -2f,
            transD: 0f,
            power: 2,
            baseN: 2,
            story:
                "A traveler walks where the slope is kind. When the derivative turns negative, the ground thins into a gap."
        ));

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[2],
            FunctionType.Sine,
            curveColor: new Color(0.2f, 1f, 0.7f, 1f),
            derivativeColor: new Color(0.2f, 0.8f, 1f, 1f),
            transA: 1f,
            transK: 0.65f,
            transC: -2f,
            transD: 0f,
            power: 2,
            baseN: 2,
            story:
                "The curve rises and falls like breath. The derivative points where to land: positive means a safe step, negative means leap."
        ));

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[3],
            FunctionType.Cosine,
            curveColor: new Color(1f, 0.6f, 0.2f, 1f),
            derivativeColor: new Color(0.9f, 0.2f, 0.6f, 1f),
            transA: 1f,
            transK: 0.65f,
            transC: -2f,
            transD: 0f,
            power: 2,
            baseN: 2,
            story:
                "Cosine hides its meaning in the sign of its derivative. Watch the pop: it marks where danger will appear."
        ));

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[4],
            FunctionType.Absolute,
            curveColor: new Color(0.4f, 0.7f, 1f, 1f),
            derivativeColor: new Color(1f, 0.15f, 0.15f, 1f),
            transA: 1f,
            transK: 0.55f,
            transC: -2f,
            transD: 0f,
            power: 2,
            baseN: 2,
            story:
                "The absolute curve folds into a single path. Where the traveler crosses the turning point, the derivative flips—and so does the ground."
        ));

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[5],
            FunctionType.MaclaurinExpSeries,
            curveColor: new Color(0.3f, 0.95f, 0.65f, 1f),
            derivativeColor: new Color(0.95f, 0.45f, 0.25f, 1f),
            transA: 0.48f,
            transK: 0.14f,
            transC: -2.05f,
            transD: 0f,
            power: 10,
            baseN: 2,
            story:
                "<color=#86efac>Taylor polynomials</color> hug a smooth function near a point. <b>Maclaurin</b> is Taylor centered at <b>0</b>.\n\n" +
                "Here the trail is a high-degree partial sum of <b>e^u</b> — polynomials stacking toward the infinite series that rebuilds the exponential.",
            derivativePopTriggerCountOverride: 3,
            applyGridTheming: true,
            gridCenter: new Color(0.25f, 0.55f, 0.42f, 0.38f),
            gridOutside: new Color(0.2f, 0.35f, 0.32f, 0.12f),
            storyPauseSecondsOverride: 2.35f
        ));

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[6],
            FunctionType.MaclaurinSinSeries,
            curveColor: new Color(0.45f, 0.78f, 1f, 1f),
            derivativeColor: new Color(1f, 0.5f, 0.85f, 1f),
            transA: 0.52f,
            transK: 0.52f,
            transC: -2f,
            transD: 0f,
            power: 8,
            baseN: 2,
            story:
                "<color=#7dd3fc>Odd powers of u</color> alternate signs — that is the Maclaurin DNA of <b>sin(u)</b>.\n\n" +
                "This graph is a truncated Taylor stack climbing toward the endless sine wave; every extra term is another promise the series keeps near 0.",
            derivativePopTriggerCountOverride: 3,
            applyGridTheming: true,
            gridCenter: new Color(0.28f, 0.42f, 0.65f, 0.38f),
            gridOutside: new Color(0.2f, 0.3f, 0.48f, 0.11f),
            storyPauseSecondsOverride: 2.3f
        ));

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[7],
            FunctionType.GeometricSeriesPartial,
            curveColor: new Color(0.85f, 0.7f, 1f, 1f),
            derivativeColor: new Color(1f, 0.35f, 0.55f, 1f),
            transA: 0.42f,
            transK: 0.038f,
            transC: -2.1f,
            transD: 0.5f,
            power: 16,
            baseN: 2,
            story:
                "A <color=#f5d0fe>geometric series</color> stacks powers of <b>u</b>. Inside its radius of convergence the tail shrinks — partial sums <i>stabilize</i> toward a limit.\n\n" +
                "Feel how the derivative of that finite stack reshapes the terrain as you move along <b>x</b>.",
            derivativePopTriggerCountOverride: 3,
            applyGridTheming: true,
            gridCenter: new Color(0.62f, 0.35f, 0.72f, 0.36f),
            gridOutside: new Color(0.42f, 0.28f, 0.5f, 0.11f),
            storyPauseSecondsOverride: 2.25f
        ));

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[8],
            FunctionType.MultivarSaddleSlice,
            curveColor: new Color(0.5f, 0.85f, 1f, 1f),
            derivativeColor: new Color(1f, 0.65f, 0.2f, 1f),
            transA: 0.11f,
            transK: 0.42f,
            transC: 2.15f,
            transD: 0f,
            power: 2,
            baseN: 2,
            story:
                "Think <b>z = x² − y₀²</b> with <b>y₀</b> fixed — a <color=#38bdf8>slice</color> through a <i>saddle surface</i>.\n\n" +
                "The x-derivative still reads the landscape: multivariable ideas, one-variable motion.",
            derivativePopTriggerCountOverride: 4,
            applyGridTheming: true,
            gridCenter: new Color(0.22f, 0.42f, 0.62f, 0.4f),
            gridOutside: new Color(0.18f, 0.3f, 0.45f, 0.12f),
            storyPauseSecondsOverride: 2.5f
        ));

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[9],
            FunctionType.MultivarParaboloidSlice,
            curveColor: new Color(1f, 0.92f, 0.55f, 1f),
            derivativeColor: new Color(0.55f, 0.35f, 1f, 1f),
            transA: 0.095f,
            transK: 0.4f,
            transC: 1.75f,
            transD: 0f,
            power: 2,
            baseN: 2,
            story:
                "Now <b>z = x² + y₀²</b>: an <color=#fde047>elliptic paraboloid</color>. Freezing <b>y₀</b> traces a <i>bowl</i> in your plane.\n\n" +
                "Gradients in multivar calculus point uphill; here the slice still shows how steeply the bowl climbs as you sprint.",
            derivativePopTriggerCountOverride: 3,
            applyGridTheming: true,
            gridCenter: new Color(0.55f, 0.48f, 0.28f, 0.38f),
            gridOutside: new Color(0.4f, 0.35f, 0.22f, 0.11f),
            storyPauseSecondsOverride: 2.35f
        ));

        var integralStageColors = new[]
        {
            new Color(0.45f, 0.82f, 1f, 1f),
            new Color(0.95f, 0.72f, 0.35f, 1f),
            new Color(0.75f, 0.55f, 1f, 1f)
        };

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[10],
            FunctionType.NaturalExp,
            curveColor: new Color(0.98f, 0.88f, 0.48f, 1f),
            derivativeColor: new Color(0.3f, 0.78f, 1f, 1f),
            transA: 0.34f,
            transK: 0.2f,
            transC: -1.88f,
            transD: 0f,
            power: 2,
            baseN: 2,
            story:
                "The <color=#fde047>definite integral</color> of a nonnegative rate is the <b>accumulated amount</b> — here, the <i>area under the curve</i> between two x-values.\n\n" +
                "The blue glass columns are a <b>Riemann sum</b>: chop the interval into equal widths Δx, pick a sample height f(x*) in each slice, and add up <b>f(x*)·Δx</b>. With more rectangles, the sum hugs the true area — <b>∫ f(x) dx</b>.\n\n" +
                "<size=92%><color=#a8b2d1>Your trail still follows the smooth graph; the shading only <i>approximates</i> what integration measures.</color></size>",
            derivativePopTriggerCountOverride: 3,
            applyGridTheming: true,
            gridCenter: new Color(0.22f, 0.4f, 0.58f, 0.38f),
            gridOutside: new Color(0.16f, 0.28f, 0.42f, 0.11f),
            levelStageColors: integralStageColors,
            storyPauseSecondsOverride: 2.75f,
            riemannRule: RiemannRule.None,
            riemannRectCount: 22,
            showRiemannVisualization: true,
            useRiemannStairPlatforms: false,
            riemannFillColor: new Color(0.25f, 0.52f, 0.92f, 0.3f)
        ));

        var riemannLeftColors = new[]
        {
            new Color(0.98f, 0.45f, 0.42f, 1f),
            new Color(1f, 0.78f, 0.35f, 1f),
            new Color(0.55f, 0.95f, 0.62f, 1f)
        };

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[11],
            FunctionType.Power,
            curveColor: new Color(1f, 0.82f, 0.35f, 1f),
            derivativeColor: new Color(0.95f, 0.35f, 0.42f, 1f),
            transA: 0.44f,
            transK: 0.36f,
            transC: -1.82f,
            transD: 0f,
            power: 2,
            baseN: 2,
            story:
                "<b>Left-handed Riemann sum:</b> in each subinterval [xᵢ, xᵢ₊₁], take the rectangle height <b>f(xᵢ)</b> — the <color=#f87171>left endpoint</color>.\n\n" +
                "If f is increasing, left samples are always the <i>shortest</i> side of the strip, so the sum <b>underestimates</b> the area.\n\n" +
                "<size=92%><color=#a8b2d1>Platforms are flat steps at those left heights — feel the conservative staircase under the parabola.</color></size>",
            derivativePopTriggerCountOverride: 3,
            applyGridTheming: true,
            gridCenter: new Color(0.52f, 0.28f, 0.22f, 0.36f),
            gridOutside: new Color(0.38f, 0.2f, 0.18f, 0.1f),
            levelStageColors: riemannLeftColors,
            storyPauseSecondsOverride: 2.55f,
            riemannRule: RiemannRule.Left,
            riemannRectCount: 14,
            showRiemannVisualization: true,
            useRiemannStairPlatforms: true,
            riemannFillColor: new Color(0.95f, 0.35f, 0.4f, 0.32f)
        ));

        var riemannRightColors = new[]
        {
            new Color(0.35f, 0.72f, 1f, 1f),
            new Color(0.55f, 0.95f, 0.85f, 1f),
            new Color(0.85f, 0.55f, 1f, 1f)
        };

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[12],
            FunctionType.Power,
            curveColor: new Color(1f, 0.82f, 0.35f, 1f),
            derivativeColor: new Color(0.35f, 0.75f, 1f, 1f),
            transA: 0.44f,
            transK: 0.36f,
            transC: -1.82f,
            transD: 0f,
            power: 2,
            baseN: 2,
            story:
                "<b>Right-handed Riemann sum:</b> height <b>f(xᵢ₊₁)</b> — the <color=#38bdf8>right endpoint</color> of each slice.\n\n" +
                "For an increasing function, right endpoints are <i>taller</i>, so this rule <b>overestimates</b> the area — the mirror story of the left sum.\n\n" +
                "<size=92%><color=#a8b2d1>Each step jumps at the right edge; compare mentally with the left-endpoint stage.</color></size>",
            derivativePopTriggerCountOverride: 3,
            applyGridTheming: true,
            gridCenter: new Color(0.2f, 0.38f, 0.55f, 0.37f),
            gridOutside: new Color(0.15f, 0.28f, 0.4f, 0.1f),
            levelStageColors: riemannRightColors,
            storyPauseSecondsOverride: 2.55f,
            riemannRule: RiemannRule.Right,
            riemannRectCount: 14,
            showRiemannVisualization: true,
            useRiemannStairPlatforms: true,
            riemannFillColor: new Color(0.25f, 0.55f, 0.95f, 0.3f)
        ));

        var riemannMidColors = new[]
        {
            new Color(0.65f, 1f, 0.55f, 1f),
            new Color(0.98f, 0.72f, 0.95f, 1f),
            new Color(0.72f, 0.78f, 1f, 1f)
        };

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[13],
            FunctionType.Power,
            curveColor: new Color(1f, 0.82f, 0.35f, 1f),
            derivativeColor: new Color(0.55f, 0.95f, 0.5f, 1f),
            transA: 0.44f,
            transK: 0.36f,
            transC: -1.82f,
            transD: 0f,
            power: 2,
            baseN: 2,
            story:
                "<b>Midpoint rule:</b> sample at the <color=#86efac>center</color> (xᵢ + xᵢ₊₁)/2. The rectangle straddles the strip symmetrically.\n\n" +
                "On curved graphs, midpoints often cancel over/under-shoot from one side to the other — a practical choice when you want a <i>tight</i> approximation without taking many rectangles.\n\n" +
                "<size=92%><color=#a8b2d1>Steps sit halfway along each slice; notice how the walk hugs the bowl more evenly than pure left or right rules.</color></size>",
            derivativePopTriggerCountOverride: 3,
            applyGridTheming: true,
            gridCenter: new Color(0.25f, 0.48f, 0.28f, 0.36f),
            gridOutside: new Color(0.18f, 0.34f, 0.22f, 0.1f),
            levelStageColors: riemannMidColors,
            storyPauseSecondsOverride: 2.55f,
            riemannRule: RiemannRule.Midpoint,
            riemannRectCount: 14,
            showRiemannVisualization: true,
            useRiemannStairPlatforms: true,
            riemannFillColor: new Color(0.45f, 0.85f, 0.55f, 0.28f)
        ));

        var engDampColors = new[]
        {
            new Color(0.4f, 0.85f, 1f, 1f),
            new Color(1f, 0.55f, 0.25f, 1f),
            new Color(0.85f, 0.45f, 1f, 1f)
        };

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[14],
            FunctionType.DampedOscillator,
            curveColor: new Color(0.35f, 0.9f, 1f, 1f),
            derivativeColor: new Color(1f, 0.5f, 0.2f, 1f),
            transA: 0.52f,
            transK: 0.48f,
            transC: -1.95f,
            transD: 0f,
            power: 5,
            baseN: 2,
            story:
                "<b>Damped oscillation</b> shows up everywhere in engineering: springs, circuits, structures.\n\n" +
                "Imagine a weight bobbing on a spring with friction: it still wiggles, but the wiggle <color=#38bdf9>shrinks over time</color> — that decay is the exponential envelope; the sine part is the vibration.\n\n" +
                "<size=92%><color=#a8b2d1>Your path is the graph; the derivative still decides safe columns — watch how the slope behaves near the peaks of each ring-down.</color></size>",
            derivativePopTriggerCountOverride: 3,
            applyGridTheming: true,
            gridCenter: new Color(0.15f, 0.38f, 0.5f, 0.38f),
            gridOutside: new Color(0.12f, 0.26f, 0.36f, 0.1f),
            levelStageColors: engDampColors,
            storyPauseSecondsOverride: 2.6f
        ));

        var engCatColors = new[]
        {
            new Color(0.95f, 0.75f, 0.35f, 1f),
            new Color(0.45f, 0.55f, 1f, 1f),
            new Color(0.5f, 0.95f, 0.65f, 1f)
        };

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[15],
            FunctionType.HyperbolicCosine,
            curveColor: new Color(1f, 0.82f, 0.4f, 1f),
            derivativeColor: new Color(0.35f, 0.45f, 1f, 1f),
            transA: 0.16f,
            transK: 0.38f,
            transC: -2.15f,
            transD: 0f,
            power: 2,
            baseN: 2,
            story:
                "A hanging chain or cable suspended at two points forms a <b>catenary</b>. In many idealized setups it is modeled with <color=#fde047>cosh</color> — the hyperbolic cosine.\n\n" +
                "Unlike a parabola (projectile motion in uniform gravity), the catenary comes from balancing tension along a flexible rope under its own weight — a classic intro to <i>hyperbolic functions</i> in statics.\n\n" +
                "<size=92%><color=#a8b2d1>The graph climbs gently at first then steepens; engineers use these curves for arches and cables.</color></size>",
            derivativePopTriggerCountOverride: 3,
            applyGridTheming: true,
            gridCenter: new Color(0.48f, 0.36f, 0.2f, 0.36f),
            gridOutside: new Color(0.35f, 0.26f, 0.15f, 0.1f),
            levelStageColors: engCatColors,
            storyPauseSecondsOverride: 2.55f
        ));

        var engAcColors = new[]
        {
            new Color(0.95f, 0.4f, 0.9f, 1f),
            new Color(0.45f, 0.9f, 1f, 1f),
            new Color(1f, 0.85f, 0.4f, 1f)
        };

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[16],
            FunctionType.FullWaveRectifiedSine,
            curveColor: new Color(1f, 0.5f, 0.85f, 1f),
            derivativeColor: new Color(0.4f, 0.85f, 1f, 1f),
            transA: 0.42f,
            transK: 0.58f,
            transC: -1.95f,
            transD: 0f,
            power: 2,
            baseN: 2,
            story:
                "<b>|sin(x)|</b> is a <color=#f0abfc>full-wave rectified</color> AC sine: flip everything below the axis upward — like a simple model after a rectifier in power electronics.\n\n" +
                "The smooth humps touch zero; the corners where sin crosses zero become sharp points, so the derivative jumps (engineering tasks often use averages / RMS values for power calculations).\n\n" +
                "<size=92%><color=#a8b2d1>Use this stage as a bridge from pure trig to how waveforms look after circuits reshape them.</color></size>",
            derivativePopTriggerCountOverride: 3,
            applyGridTheming: true,
            gridCenter: new Color(0.45f, 0.22f, 0.42f, 0.36f),
            gridOutside: new Color(0.32f, 0.16f, 0.3f, 0.1f),
            levelStageColors: engAcColors,
            storyPauseSecondsOverride: 2.5f
        ));

        // ---- AP Calculus BC + polar + Physics C (indices 17–32) --------------------------------

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[17],
            FunctionType.Arctangent,
            curveColor: new Color(0.45f, 0.82f, 1f, 1f),
            derivativeColor: new Color(1f, 0.55f, 0.35f, 1f),
            transA: 1.05f,
            transK: 0.32f,
            transC: -2.05f,
            transD: 0f,
            power: 2,
            baseN: 2,
            story:
                "<b>AP Calculus BC — inverse trig.</b> <color=#38bdf8>Arctan</color> is the hero of bounded slopes: d/dx arctan(x) = 1/(1+x²).\n\n" +
                "It shows up in integrals that produce arctangent, in related‑rate geometry problems, and whenever an angle is defined from a ratio that grows slowly.\n\n" +
                "<size=92%><color=#a8b2d1>The graph levels toward horizontal asymptotes — a visual for limits at ±∞.</color></size>",
            derivativePopTriggerCountOverride: 3,
            applyGridTheming: true,
            gridCenter: new Color(0.22f, 0.4f, 0.55f, 0.36f),
            gridOutside: new Color(0.16f, 0.28f, 0.4f, 0.1f),
            storyPauseSecondsOverride: 2.45f
        ));

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[18],
            FunctionType.Logistic,
            curveColor: new Color(0.4f, 0.95f, 0.65f, 1f),
            derivativeColor: new Color(0.95f, 0.35f, 0.55f, 1f),
            transA: 1.65f,
            transK: 0.28f,
            transC: -3.15f,
            transD: 0f,
            power: 2,
            baseN: 4,
            story:
                "<b>Logistic differential equation</b> (BC staple): dP/dt = kP(1 − P/L). Growth is nearly exponential when P is small, then <color=#86efac>curves</color> as it nears carrying capacity <b>L</b>.\n\n" +
                "Population models, rumor spread, and saturated chemical reactions share this S‑shape. Separation of variables leads here; the inflection point is where growth is fastest.\n\n" +
                "<size=92%><color=#a8b2d1>Read the rise as “early exponential,” the bend as competition, the top as equilibrium.</color></size>",
            derivativePopTriggerCountOverride: 3,
            applyGridTheming: true,
            gridCenter: new Color(0.2f, 0.45f, 0.32f, 0.35f),
            gridOutside: new Color(0.14f, 0.32f, 0.24f, 0.1f),
            storyPauseSecondsOverride: 2.75f
        ));

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[19],
            FunctionType.PolarCardioid,
            curveColor: new Color(1f, 0.72f, 0.35f, 1f),
            derivativeColor: new Color(0.45f, 0.55f, 1f, 1f),
            transA: 0.52f,
            transK: 0.34f,
            transC: -2.35f,
            transD: 0f,
            power: 2,
            baseN: 2,
            story:
                "<b>Polar coordinates</b>: describe points by <color=#fde047>(r, θ)</color> instead of (x, y). A <b>cardioid</b> has the family flavor r ~ 1 + cos θ — a heartbeat‑shaped loop.\n\n" +
                "Here the horizontal axis stands in for θ and the vertical for r(θ) (same trick AP uses when you first graph polar equations before converting to x = r cos θ, y = r sin θ).\n\n" +
                "<size=92%><color=#a8b2d1>Area in polar uses ½∫ r² dθ; tangent slope needs dr/dθ.</color></size>",
            derivativePopTriggerCountOverride: 3,
            applyGridTheming: true,
            gridCenter: new Color(0.5f, 0.34f, 0.18f, 0.35f),
            gridOutside: new Color(0.36f, 0.24f, 0.14f, 0.1f),
            storyPauseSecondsOverride: 2.7f
        ));

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[20],
            FunctionType.PolarRose,
            curveColor: new Color(0.95f, 0.45f, 0.9f, 1f),
            derivativeColor: new Color(0.45f, 0.9f, 1f, 1f),
            transA: 0.78f,
            transK: 0.3f,
            transC: -2f,
            transD: 0f,
            power: 5,
            baseN: 2,
            story:
                "<b>Polar rose</b>: r ~ cos(nθ) traces petals meeting at the origin. Odd <b>n</b> here gives <b>n</b> petals for this cosine form (a classic exam plot).\n\n" +
                "Symmetry and period tell you how many times the radius returns to zero — great practice for converting polar area and arc length integrals.\n\n" +
                "<size=92%><color=#a8b2d1>Watch derivative pops where r changes fastest — those are steep walls on the petal edges.</color></size>",
            derivativePopTriggerCountOverride: 4,
            applyGridTheming: true,
            gridCenter: new Color(0.42f, 0.22f, 0.48f, 0.36f),
            gridOutside: new Color(0.3f, 0.16f, 0.34f, 0.1f),
            storyPauseSecondsOverride: 2.65f
        ));

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[21],
            FunctionType.HyperbolicSine,
            curveColor: new Color(0.55f, 0.95f, 0.55f, 1f),
            derivativeColor: new Color(0.95f, 0.5f, 0.35f, 1f),
            transA: 0.072f,
            transK: 0.38f,
            transC: -2.05f,
            transD: 0f,
            power: 2,
            baseN: 2,
            story:
                "<b>Hyperbolic sine & cosine</b> (BC): sinh x = (e^x − e^{−x})/2, cosh x = (e^x + e^{−x})/2, and cosh² − sinh² = 1 (a hyperbola identity).\n\n" +
                "They solve linear ODEs, describe hanging cables alongside cosh, and mirror trig identities with occasional sign flips.\n\n" +
                "<size=92%><color=#a8b2d1>You already met the catenary’s cosh; sinh is its odd, rise‑from‑zero partner.</color></size>",
            derivativePopTriggerCountOverride: 3,
            applyGridTheming: true,
            gridCenter: new Color(0.22f, 0.48f, 0.28f, 0.35f),
            gridOutside: new Color(0.16f, 0.34f, 0.2f, 0.1f),
            storyPauseSecondsOverride: 2.5f
        ));

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[22],
            FunctionType.ExponentialDecay,
            curveColor: new Color(0.5f, 0.78f, 1f, 1f),
            derivativeColor: new Color(1f, 0.65f, 0.25f, 1f),
            transA: 1.15f,
            transK: 0.095f,
            transC: -2.25f,
            transD: 0f,
            power: 2,
            baseN: 2,
            story:
                "<b>AP Physics C (calculus‑based)</b> — exponential decay: charge on a discharging capacitor, current in an RL loop, or any quantity Q(t) with dQ/dt ~ −Q.\n\n" +
                "Solution: <color=#38bdf9>Q = Q₀ e^{−t/τ}</color>; τ (time constant) sets how fast the tail relaxes — the same picture as “half‑life” thinking.\n\n" +
                "<size=92%><color=#a8b2d1>The graph is a one‑sided bump; the derivative carries the sign of “still leaking toward zero.”</color></size>",
            derivativePopTriggerCountOverride: 3,
            applyGridTheming: true,
            gridCenter: new Color(0.15f, 0.35f, 0.5f, 0.36f),
            gridOutside: new Color(0.12f, 0.25f, 0.36f, 0.1f),
            storyPauseSecondsOverride: 2.55f
        ));

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[23],
            FunctionType.Cosine,
            curveColor: new Color(0.65f, 0.85f, 1f, 1f),
            derivativeColor: new Color(1f, 0.55f, 0.85f, 1f),
            transA: 0.92f,
            transK: 0.52f,
            transC: -2f,
            transD: 0f,
            power: 2,
            baseN: 2,
            story:
                "<b>Physics C — rotation & angular momentum.</b> For rigid spin about a fixed axis, <color=#7dd3fc>L = I ω</color> (angular momentum = moment of inertia × angular speed).\n\n" +
                "Net torque τ = dL/dt (like F = dp for linear motion). Small oscillations of many rotational systems look <i>sinusoidal</i> — the same graph as linear SHM, now dressed as θ(t) or ω(t).\n\n" +
                "<size=92%><color=#a8b2d1>Energy sloshes between kinetic ½Iω² and restoring “spring” terms in ϕ — walk the cosine as a rotation story.</color></size>",
            derivativePopTriggerCountOverride: 3,
            applyGridTheming: true,
            gridCenter: new Color(0.24f, 0.36f, 0.55f, 0.35f),
            gridOutside: new Color(0.18f, 0.26f, 0.4f, 0.1f),
            storyPauseSecondsOverride: 2.7f
        ));

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[24],
            FunctionType.Power,
            curveColor: new Color(0.95f, 0.82f, 0.35f, 1f),
            derivativeColor: new Color(0.5f, 0.75f, 1f, 1f),
            transA: -0.26f,
            transK: 0.36f,
            transC: 7.7f,
            transD: 0f,
            power: 2,
            baseN: 2,
            story:
                "<b>Projectile height vs time</b> (constant g): y(t) = y₀ + v₀ t − ½ g t² — a <color=#fde047>downward parabola</color> in t for vertical motion.\n\n" +
                "Derivatives give vertical velocity, then acceleration −g: the Physics C calculus trilogy x, v, a shows up in every kinematics sprint.\n\n" +
                "<size=92%><color=#a8b2d1>Peak is where velocity (derivative) crosses zero — a free optimization problem.</color></size>",
            derivativePopTriggerCountOverride: 3,
            applyGridTheming: true,
            gridCenter: new Color(0.48f, 0.38f, 0.2f, 0.34f),
            gridOutside: new Color(0.34f, 0.26f, 0.14f, 0.1f),
            storyPauseSecondsOverride: 2.45f
        ));

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[25],
            FunctionType.MaclaurinCosSeries,
            curveColor: new Color(0.55f, 0.92f, 1f, 1f),
            derivativeColor: new Color(1f, 0.45f, 0.65f, 1f),
            transA: 0.5f,
            transK: 0.48f,
            transC: -2f,
            transD: 0f,
            power: 8,
            baseN: 2,
            story:
                "<b>Maclaurin for cos(x)</b> uses even powers alternating signs: 1 − x²/2! + x⁴/4! − … — the partner series to sine’s odd powers.\n\n" +
                "On the AP exam you estimate errors with Taylor remainders and reason about radius of convergence — here you get to <i>see</i> the polynomial hug the true cosine near 0.\n\n" +
                "<size=92%><color=#a8b2d1>More terms ⇢ wider trustworthy fit; the derivative polynomials track −sin x in spirit.</color></size>",
            derivativePopTriggerCountOverride: 3,
            applyGridTheming: true,
            gridCenter: new Color(0.2f, 0.42f, 0.55f, 0.35f),
            gridOutside: new Color(0.14f, 0.3f, 0.42f, 0.1f),
            storyPauseSecondsOverride: 2.45f
        ));

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[26],
            FunctionType.NaturalLog,
            curveColor: new Color(0.65f, 0.95f, 0.55f, 1f),
            derivativeColor: new Color(0.85f, 0.45f, 1f, 1f),
            transA: 0.48f,
            transK: 0.15f,
            transC: -2.1f,
            transD: -25f,
            power: 2,
            baseN: 2,
            story:
                "<b>Natural logarithm</b> is the star of ∫ (1/x) dx = ln|x| + C and shows up in p‑growth comparisons, half‑lives, and ε–δ arguments about slow divergence.\n\n" +
                "Domain x > 0 (here ensured by shifting the graph so u stays positive): slopes are always positive but shrink as x grows — classic “diminishing returns.”\n\n" +
                "<size=92%><color=#a8b2d1>BC links ln x to harmonic series / integral test intuitions.</color></size>",
            derivativePopTriggerCountOverride: 3,
            applyGridTheming: true,
            gridCenter: new Color(0.28f, 0.45f, 0.24f, 0.35f),
            gridOutside: new Color(0.2f, 0.32f, 0.18f, 0.1f),
            storyPauseSecondsOverride: 2.35f
        ));

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[27],
            FunctionType.SquareRoot,
            curveColor: new Color(0.95f, 0.6f, 0.4f, 1f),
            derivativeColor: new Color(0.45f, 0.65f, 1f, 1f),
            transA: 0.55f,
            transK: 0.14f,
            transC: -2.15f,
            transD: -20f,
            power: 2,
            baseN: 2,
            story:
                "<b>√x</b> — domain restriction hero. d/dx √x = 1/(2√x) blows up approaching 0 from the right: infinite slope at the vertical tangent place (a classic BC “improper behavior” discussion).\n\n" +
                "Substitution integrals and arc length formulas love √(1 + (dy/dx)²); the cusp language matches “watch the derivative.”\n\n" +
                "<size=92%><color=#a8b2d1>Gameplay still samples smooth pieces; the story is the analytic caution at the boundary.</color></size>",
            derivativePopTriggerCountOverride: 3,
            applyGridTheming: true,
            gridCenter: new Color(0.48f, 0.3f, 0.18f, 0.35f),
            gridOutside: new Color(0.34f, 0.22f, 0.12f, 0.1f),
            storyPauseSecondsOverride: 2.35f
        ));

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[28],
            FunctionType.Tangent,
            curveColor: new Color(0.6f, 0.85f, 1f, 1f),
            derivativeColor: new Color(1f, 0.4f, 0.35f, 1f),
            transA: 0.42f,
            transK: 0.048f,
            transC: -2f,
            transD: 0f,
            power: 2,
            baseN: 2,
            story:
                "<b>Tangent</b> packs vertical asymptotes where cos → 0 — limits sprint material on every AP sheet.\n\n" +
                "Here the window is chosen so you explore a single smooth branch between asymptotes: sec²x is the derivative, always ≥ 1 when defined.\n\n" +
                "<size=92%><color=#a8b2d1>BC parametric/polar work often reduces to chasing trig identities; tan is a spine in those algebra stories.</color></size>",
            derivativePopTriggerCountOverride: 3,
            applyGridTheming: true,
            gridCenter: new Color(0.2f, 0.38f, 0.52f, 0.35f),
            gridOutside: new Color(0.14f, 0.28f, 0.38f, 0.1f),
            storyPauseSecondsOverride: 2.3f
        ));

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[29],
            FunctionType.NaturalExp,
            curveColor: new Color(0.4f, 1f, 0.75f, 1f),
            derivativeColor: new Color(1f, 0.55f, 0.45f, 1f),
            transA: 0.2f,
            transK: 0.065f,
            transC: -2.05f,
            transD: 0f,
            power: 2,
            baseN: 2,
            story:
                "<b>Exponential growth ODE</b>: if y′ = k y then y = Ce^{kx} — the reason e^x is “its own derivative” up to scaling.\n\n" +
                "Separable equations, slope fields, and half‑life problems all orbit this curve before you meet logistic saturation next door.\n\n" +
                "<size=92%><color=#a8b2d1>Contrast with the ∫ e^x dx level earlier: there we shaded area; here we emphasize <i>rate proportional to amount</i>.</color></size>",
            derivativePopTriggerCountOverride: 3,
            applyGridTheming: true,
            gridCenter: new Color(0.18f, 0.42f, 0.3f, 0.35f),
            gridOutside: new Color(0.13f, 0.3f, 0.22f, 0.1f),
            storyPauseSecondsOverride: 2.35f
        ));

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[30],
            FunctionType.Sine,
            curveColor: new Color(0.82f, 0.55f, 1f, 1f),
            derivativeColor: new Color(0.45f, 0.95f, 0.75f, 1f),
            transA: 0.95f,
            transK: 0.48f,
            transC: -2f,
            transD: 0.35f,
            power: 2,
            baseN: 2,
            story:
                "<b>Phase & SHM.</b> sin(ωt + ϕ) is the same motion as cosine, just time‑shifted — <color=#e9d5ff>energy swaps</color> between kinetic and potential in an ideal spring.\n\n" +
                "Parametric circles (x = R cos t, y = R sin t) project to these components; BC’s vector‑valued motion unit leans on the same trig backbone.\n\n" +
                "<size=92%><color=#a8b2d1>Derivative cos tracks velocity up to constants: the platform logic is “who leads, who lags?”</color></size>",
            derivativePopTriggerCountOverride: 3,
            applyGridTheming: true,
            gridCenter: new Color(0.38f, 0.22f, 0.5f, 0.35f),
            gridOutside: new Color(0.28f, 0.16f, 0.36f, 0.1f),
            storyPauseSecondsOverride: 2.4f
        ));

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[31],
            FunctionType.Power,
            curveColor: new Color(0.95f, 0.5f, 0.45f, 1f),
            derivativeColor: new Color(0.55f, 0.55f, 1f, 1f),
            transA: 0.055f,
            transK: 0.38f,
            transC: -1.88f,
            transD: 0f,
            power: 3,
            baseN: 2,
            story:
                "<b>Cubic graph sketching</b> — a BC classroom ritual: find critical points, inflection where y″ flips sign, end behavior ±∞.\n\n" +
                "<color=#fca5a5>Inflection</color> is where curvature changes; the derivative has a local max/min there for smooth cubics.\n\n" +
                "<size=92%><color=#a8b2d1>Your feet feel one hump + one valley pattern typical of monotone‑derivative pieces between flexes.</color></size>",
            derivativePopTriggerCountOverride: 4,
            applyGridTheming: true,
            gridCenter: new Color(0.5f, 0.24f, 0.2f, 0.34f),
            gridOutside: new Color(0.36f, 0.17f, 0.15f, 0.1f),
            storyPauseSecondsOverride: 2.35f
        ));

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[32],
            FunctionType.Exponential,
            curveColor: new Color(0.85f, 0.75f, 1f, 1f),
            derivativeColor: new Color(1f, 0.6f, 0.35f, 1f),
            transA: 0.32f,
            transK: 0.088f,
            transC: -2.05f,
            transD: 0f,
            power: 2,
            baseN: 2,
            story:
                "<b>General exponential</b> b^x has derivative proportional to itself: d/dx b^x = (ln b) b^x.\n\n" +
                "That constant <color=#c4b5fd>ln b</color> is the bridge from base‑10 or base‑2 growth to the natural base e where the constant becomes 1.\n\n" +
                "<size=92%><color=#a8b2d1>Pair mentally with the Maclaurin and logistic levels — three lenses on “growth language.”</color></size>",
            derivativePopTriggerCountOverride: 3,
            applyGridTheming: true,
            gridCenter: new Color(0.4f, 0.32f, 0.55f, 0.35f),
            gridOutside: new Color(0.28f, 0.22f, 0.4f, 0.1f),
            storyPauseSecondsOverride: 2.3f
        ));

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[33],
            FunctionType.CircleUpper,
            curveColor: new Color(0.55f, 0.82f, 1f, 1f),
            derivativeColor: new Color(1f, 0.65f, 0.45f, 1f),
            transA: 2.65f,
            transK: 1f,
            transC: -2.15f,
            transD: 0f,
            power: 2,
            baseN: 2,
            story:
                "<b>Circle equation</b> in standard form: <color=#7dd3fc>(x − h)² + (y − k)² = R²</color>. A full circle is not a single y = f(x) graph — it fails the vertical line test — so we walk the <b>upper semicircle</b>:\n\n" +
                "y = k + √(R² − (x − h)²) on |x − h| ≤ R. <b>Implicit differentiation</b> on the circle gives dy/dx = −(x − h)/(y − k) (away from y = k on the full curve).\n\n" +
                "<size=92%><color=#a8b2d1>Parametric form x = h + R cos t, y = k + R sin t is another AP favorite; this stage keeps you on the top arc.</color></size>",
            derivativePopTriggerCountOverride: 3,
            applyGridTheming: true,
            gridCenter: new Color(0.22f, 0.36f, 0.52f, 0.36f),
            gridOutside: new Color(0.16f, 0.26f, 0.38f, 0.1f),
            storyPauseSecondsOverride: 2.65f
        ));

        // ---- Aerospace engineering & aerodynamics (indices 34–40) -----------------------------

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[34],
            FunctionType.AeroLiftVsAlpha,
            curveColor: new Color(0.35f, 0.72f, 1f, 1f),
            derivativeColor: new Color(1f, 0.58f, 0.3f, 1f),
            transA: 0.82f,
            transK: 0.38f,
            transC: -2.05f,
            transD: 0f,
            power: 2,
            baseN: 2,
            story:
                "<b>Lift vs angle of attack</b> — on a wing, <color=#38bdf8>coefficient C_L</color> grows roughly linearly with α in the attached‑flow regime (thin‑airfoil / small‑angle mood).\n\n" +
                "Past the <b>stall</b> angle the boundary layer separates; lift drops sharply — a nonlinear break your feet feel as the graph stops climbing.\n\n" +
                "<size=92%><color=#a8b2d1>Real design couples Mach, Reynolds, sweep, twist; this graph is a calculus “shape class” for slope & saturation.</color></size>",
            derivativePopTriggerCountOverride: 3,
            applyGridTheming: true,
            gridCenter: new Color(0.16f, 0.32f, 0.48f, 0.37f),
            gridOutside: new Color(0.12f, 0.22f, 0.34f, 0.11f),
            storyPauseSecondsOverride: 2.65f
        ));

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[35],
            FunctionType.Power,
            curveColor: new Color(0.75f, 0.55f, 1f, 1f),
            derivativeColor: new Color(1f, 0.72f, 0.35f, 1f),
            transA: 0.072f,
            transK: 0.42f,
            transC: -2.02f,
            transD: 0f,
            power: 2,
            baseN: 2,
            story:
                "<b>Drag polar</b> — aircraft guys write <color=#c4b5fd>C_D = C_{D0} + K C_L²</color> (parabolic drag polar) to mash profile + induced drag into one quadratic in lift coefficient.\n\n" +
                "Min drag at a certain C_L sets best glide / L/D intuition; integrating under such curves feeds range & endurance estimates (Breguet‑style reasoning in the big books).\n\n" +
                "<size=92%><color=#a8b2d1>Here u plays the role of C_L on the horizontal axis — walk the bowl of extra drag as you leave the sweet spot.</color></size>",
            derivativePopTriggerCountOverride: 3,
            applyGridTheming: true,
            gridCenter: new Color(0.35f, 0.25f, 0.5f, 0.36f),
            gridOutside: new Color(0.25f, 0.18f, 0.36f, 0.1f),
            storyPauseSecondsOverride: 2.55f
        ));

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[36],
            FunctionType.AeroIsothermalDensity,
            curveColor: new Color(0.45f, 0.88f, 0.72f, 1f),
            derivativeColor: new Color(0.95f, 0.45f, 0.5f, 1f),
            transA: 1.05f,
            transK: 0.32f,
            transC: -2.28f,
            transD: -6f,
            power: 2,
            baseN: 4,
            story:
                "<b>Atmosphere (isothermal cartoon)</b> — pressure and density drop roughly <color=#86efac>exponentially</color> with altitude: p, ρ ~ e^{−h/H} with <b>scale height</b> H (temperature & mean molar mass set the mood in the real ISA).\n\n" +
                "Aero engineers live in these curves: thrust, Reynolds, Mach, dynamic pressure q = ½ρV² all track ρ(h).\n\n" +
                "<size=92%><color=#a8b2d1>Plot uses h ≥ 0 on the transformed axis; negative side clips—like launching from sea level only.</color></size>",
            derivativePopTriggerCountOverride: 3,
            applyGridTheming: true,
            gridCenter: new Color(0.18f, 0.42f, 0.34f, 0.36f),
            gridOutside: new Color(0.13f, 0.3f, 0.24f, 0.1f),
            storyPauseSecondsOverride: 2.6f
        ));

        var aeroPhugColors = new[]
        {
            new Color(0.5f, 0.78f, 1f, 1f),
            new Color(1f, 0.55f, 0.4f, 1f),
            new Color(0.75f, 0.55f, 1f, 1f)
        };

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[37],
            FunctionType.DampedOscillator,
            curveColor: new Color(0.4f, 0.85f, 0.95f, 1f),
            derivativeColor: new Color(1f, 0.52f, 0.28f, 1f),
            transA: 0.48f,
            transK: 0.44f,
            transC: -1.98f,
            transD: 0f,
            power: 4,
            baseN: 2,
            story:
                "<b>Longitudinal dynamics</b> — a rigid aircraft has <color=#7dd3fc>short‑period</color> (quick pitch / heave) and <b>phugoid</b> (slow exchange of altitude & speed) modes.\n\n" +
                "Linearized state‑space models are eigenvalues & eigenvectors; cartooned here as a <b>damped oscillation</b> — exponential envelope × sine — the same mathematics as mass–spring–damper labs.\n\n" +
                "<size=92%><color=#a8b2d1>Flight control & autopilot designers tame these modes with feedback; feel the derivative change at crests and troughs.</color></size>",
            derivativePopTriggerCountOverride: 3,
            applyGridTheming: true,
            gridCenter: new Color(0.14f, 0.35f, 0.48f, 0.36f),
            gridOutside: new Color(0.1f, 0.25f, 0.34f, 0.1f),
            levelStageColors: aeroPhugColors,
            storyPauseSecondsOverride: 2.75f
        ));

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[38],
            FunctionType.AeroNewtonianSinSquared,
            curveColor: new Color(1f, 0.45f, 0.42f, 1f),
            derivativeColor: new Color(0.5f, 0.82f, 1f, 1f),
            transA: 0.92f,
            transK: 0.5f,
            transC: -2.05f,
            transD: 0f,
            power: 2,
            baseN: 2,
            story:
                "<b>Newtonian / impact theory mood</b> — in hypersonic teaching models, surface <color=#fca5a5>pressure coefficient</color> scales like sin²α for windward facets (turning momentum of molecules).\n\n" +
                "Not a replacement for CFD or shock‑expansion — but the right <i>calculus vocabulary</i>: nonlinear trig powering heat‑shield and entry corridor conversations.\n\n" +
                "<size=92%><color=#a8b2d1>Horizontal axis plays local slope angle; only α ≥ 0 is meaningful on this branch.</color></size>",
            derivativePopTriggerCountOverride: 3,
            applyGridTheming: true,
            gridCenter: new Color(0.48f, 0.22f, 0.2f, 0.35f),
            gridOutside: new Color(0.34f, 0.15f, 0.14f, 0.1f),
            storyPauseSecondsOverride: 2.55f
        ));

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[39],
            FunctionType.Sine,
            curveColor: new Color(0.55f, 0.65f, 1f, 1f),
            derivativeColor: new Color(1f, 0.48f, 0.72f, 1f),
            transA: 0.42f,
            transK: 0.55f,
            transC: -1.95f,
            transD: 0f,
            power: 2,
            baseN: 2,
            story:
                "<b>Strouhal number</b> — bluff bodies shed vortices at a characteristic frequency <color=#93c5fd>f ≈ St · U / D</color> (St ≈ 0.2 for cylinders in the textbook band).\n\n" +
                "That periodicity drives vibrations, noise, and fatigue loading on antennas, cables, and control surfaces in wake turbulence.\n\n" +
                "<size=92%><color=#a8b2d1>Sine waves are the fingerprint of linearized unsteady aero & flutter thinking — walk the cycle as if reading a hot‑wire trace.</color></size>",
            derivativePopTriggerCountOverride: 3,
            applyGridTheming: true,
            gridCenter: new Color(0.22f, 0.28f, 0.52f, 0.36f),
            gridOutside: new Color(0.16f, 0.2f, 0.38f, 0.1f),
            storyPauseSecondsOverride: 2.45f
        ));

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[40],
            FunctionType.ExponentialDecay,
            curveColor: new Color(1f, 0.72f, 0.38f, 1f),
            derivativeColor: new Color(0.45f, 0.78f, 1f, 1f),
            transA: 1.05f,
            transK: 0.072f,
            transC: -2.22f,
            transD: 0f,
            power: 2,
            baseN: 2,
            story:
                "<b>Re‑entry & hypersonic heating mood</b> — heat flux scales with <color=#fde047>dynamic pressure × velocity</color> roughly like ρ V³ in many order‑of‑magnitude chats (models vary!), so as altitude climbs (ρ↓) and speed bleeds off, the <i>threat curve</i> relaxes exponentially in time in simplified histories.\n\n" +
                "Thermal protection, trajectory shaping, and bank angle modulation all serve to keep material beneath limits — calculus is the language of those trade curves.\n\n" +
                "<size=92%><color=#a8b2d1>Use this stage as a qualitative decay envelope, not a quantitative SpaceX memo.</color></size>",
            derivativePopTriggerCountOverride: 3,
            applyGridTheming: true,
            gridCenter: new Color(0.42f, 0.3f, 0.18f, 0.35f),
            gridOutside: new Color(0.3f, 0.22f, 0.12f, 0.1f),
            storyPauseSecondsOverride: 2.65f
        ));

        // Competition math (41) + Mandelbrot boss (42) — keep order aligned with GameLevelCatalog.
        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[41],
            FunctionType.NaturalLog,
            curveColor: new Color(0.42f, 0.88f, 0.96f, 1f),
            derivativeColor: new Color(0.98f, 0.62f, 0.32f, 1f),
            transA: 0.58f,
            transK: 0.26f,
            transC: -1.92f,
            transD: -1.85f,
            power: 2,
            baseN: 2,
            story:
                "<b>Competition mathematics</b> — contests like <color=#7dd3fc>AMC / AIME</color> and proof‑style olympiads reward <i>structure</i>: symmetry, bounding, and knowing when a function is <b>concave or convex</b>.\n\n" +
                "The natural log is <color=#fdba74>concave on (0,∞)</color> — **chords lie below the graph**, tangents lie above on one side — a factory of **linear bounds** (tangents & secants) used in inequalities and “prove bound” problems.\n\n" +
                "<size=92%><color=#a8b2d1>Your feet follow \\(\\ln\\) on a shifted domain; the derivative plot mirrors \\(1/x\\) behavior — the same reciprocal intuition behind many integral estimates. Not affiliated with MAA or any contest body.</color></size>",
            derivativePopTriggerCountOverride: 4,
            applyGridTheming: true,
            gridCenter: new Color(0.14f, 0.38f, 0.46f, 0.38f),
            gridOutside: new Color(0.1f, 0.26f, 0.32f, 0.11f),
            storyPauseSecondsOverride: 2.85f
        ));

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[42],
            FunctionType.MandelbrotEscapeImSlice,
            curveColor: new Color(0.25f, 0.98f, 0.62f, 1f),
            derivativeColor: new Color(0.98f, 0.38f, 0.82f, 1f),
            transA: -0.743643887f,
            transK: 0.088f,
            transC: -2.18f,
            transD: 0f,
            power: 80,
            baseN: 26,
            story:
                "<b>Final stage — Mandelbrot set</b> — the <color=#a8b2d1>backdrop</color> is the classic <b>c-plane</b> (Re horizontal, Im vertical) colored by <color=#a7f3d0>smooth escape time</color>; the bright line marks your fixed <color=#86efac>Re(c)</color>. The green curve is the same slice as before: height vs <color=#86efac>Im(c)</color>.\n\n" +
                "The cardioid and bulbs are the boundary where Julia sets disconnect; zooming that coastline reveals endless filaments (true deep zoom needs a different engine, but the map is real Mandelbrot math).\n\n" +
                "<size=92%><color=#a8b2d1>Slice iteration uses <b>|Im(c)|</b> (conjugate symmetry). Curve uses fractional escape counts so steps look less “flat” than raw integers.</color></size>",
            derivativePopTriggerCountOverride: 4,
            applyGridTheming: true,
            gridCenter: new Color(0.12f, 0.32f, 0.48f, 0.4f),
            gridOutside: new Color(0.08f, 0.18f, 0.28f, 0.12f),
            storyPauseSecondsOverride: 2.9f,
            graphStep: 0.14f,
            levelXStart: -16f,
            levelXEnd: 16f
        ));
    }

    /// <summary>
    /// Factory for a runtime <see cref="LevelDefinition"/> ScriptableObject instance (not saved as an asset).
    /// Optional Riemann args apply integral / stair visualization stages.
    /// </summary>
    private LevelDefinition MakeLevel(
        string name,
        FunctionType functionType,
        Color curveColor,
        Color derivativeColor,
        float transA,
        float transK,
        float transC,
        float transD,
        int power,
        int baseN,
        string story,
        int derivativePopTriggerCountOverride = 0,
        bool applyGridTheming = false,
        Color gridCenter = default,
        Color gridOutside = default,
        Color[] levelStageColors = null,
        float storyPauseSecondsOverride = 0f,
        RiemannRule riemannRule = RiemannRule.None,
        int riemannRectCount = 18,
        bool showRiemannVisualization = false,
        bool useRiemannStairPlatforms = false,
        Color? riemannFillColor = null,
        float graphStep = 0f,
        float? levelXStart = null,
        float? levelXEnd = null)
    {
        var def = ScriptableObject.CreateInstance<LevelDefinition>();
        def.levelName = name;
        def.functionType = functionType;
        def.xStart = levelXStart ?? (functionPlotter != null ? functionPlotter.xStart : -20f);
        def.xEnd = levelXEnd ?? (functionPlotter != null ? functionPlotter.xEnd : 20f);
        def.step = graphStep > 1e-5f ? graphStep : (functionPlotter != null ? functionPlotter.step : 0.1f);

        def.transA = transA;
        def.transK = transK;
        def.transC = transC;
        def.transD = transD;
        def.power = power;
        def.baseN = baseN;

        def.curveColor = curveColor;
        def.derivativeColor = derivativeColor;

        def.derivativeSafeThreshold = 0f;
        def.forcePlatformsAtStartColumns = 3;
        def.forcePlatformsAtEndColumns = 1;
        def.platformThicknessGrid = 0.6f;
        def.hazardHeightGrid = 0.5f;

        int popN = derivativePopTriggerCountOverride > 0 ? derivativePopTriggerCountOverride : defaultStageCount;
        def.derivativePopTriggerCount = derivativePopTriggerCountOverride;
        def.applyGridTheming = applyGridTheming;
        if (applyGridTheming)
        {
            def.gridCenterLineTheming = gridCenter;
            def.gridOutsideLineTheming = gridOutside;
        }

        if (storyPauseSecondsOverride > 0.01f)
            def.storyPauseSeconds = storyPauseSecondsOverride;

        def.stageDerivativePopColors = new List<Color>(popN);
        if (levelStageColors != null && levelStageColors.Length >= popN)
        {
            for (int i = 0; i < popN; i++)
                def.stageDerivativePopColors.Add(levelStageColors[i]);
        }
        else
        {
            for (int i = 0; i < popN; i++)
            {
                float t = popN == 1 ? 0f : (float)i / (popN - 1);
                def.stageDerivativePopColors.Add(Color.Lerp(derivativeColor, Color.white, 0.35f * t));
            }
        }

        def.storyText = story;

        def.riemannRule = riemannRule;
        def.riemannRectCount = riemannRectCount;
        def.showRiemannVisualization = showRiemannVisualization;
        def.useRiemannStairPlatforms = useRiemannStairPlatforms;
        if (riemannFillColor.HasValue)
            def.riemannFillColor = riemannFillColor.Value;

        return def;
    }

    /// <summary>Loads a level by index, applies theme, fades story, rebuilds collision world.</summary>
    private void LoadLevel(int index)
    {
        if (levels.Count == 0 || functionPlotter == null)
            return;

        currentLevelIndex = Mathf.Clamp(index, 0, levels.Count - 1);
        nextStageIndex = 0;
        lastStageHudKey = int.MinValue;
        isRestarting = false;

        var def = levels[currentLevelIndex];
        ApplyLevelTheme(def);
        RefreshStageHud();

        if (levelFlowRoutine != null)
            StopCoroutine(levelFlowRoutine);
        levelFlowRoutine = StartCoroutine(LoadLevelFullRoutine(def));
    }

    /// <summary>
    /// Copies <paramref name="def"/> into FunctionPlotter + line colors + grid theme + stage triggers + story text.
    /// Does not rebuild physics platforms (see <see cref="LoadWorldAfterThemeChange"/>).
    /// </summary>
    private void ApplyLevelTheme(LevelDefinition def)
    {
        functionPlotter.functionType = def.functionType;
        functionPlotter.xStart = def.xStart;
        functionPlotter.xEnd = def.xEnd;
        functionPlotter.step = def.step;

        functionPlotter.transA = def.transA;
        functionPlotter.transK = def.transK;
        functionPlotter.transC = def.transC;
        functionPlotter.transD = def.transD;

        functionPlotter.power = def.power;
        functionPlotter.baseN = def.baseN;
        functionPlotter.differentiate = true;

        if (def.showRiemannVisualization)
        {
            if (def.riemannRule == RiemannRule.None)
                functionPlotter.SetEquationExtraSuffix($"Area ≈ Σ f(x*) Δx, n={def.riemannRectCount} (Δx=(b−a)/n)");
            else
                functionPlotter.SetEquationExtraSuffix($"Riemann {def.riemannRule}: n={def.riemannRectCount}");
        }
        else if (def.useRiemannStairPlatforms && def.riemannRule != RiemannRule.None)
            functionPlotter.SetEquationExtraSuffix($"Stairs: {def.riemannRule} rule, n={def.riemannRectCount}");
        else
            functionPlotter.SetEquationExtraSuffix("");

        curveRenderer.color = def.curveColor;
        derivRenderer.color = def.derivativeColor;

        int popN = def.derivativePopTriggerCount > 0 ? def.derivativePopTriggerCount : defaultStageCount;
        var fromDef = def.stageDerivativePopColors;
        if (fromDef != null && fromDef.Count >= popN && fromDef.Count == popN)
            stagePopColors = new List<Color>(fromDef);
        else if (fromDef != null && fromDef.Count >= popN)
            stagePopColors = fromDef.GetRange(0, popN);
        else
        {
            stagePopColors = new List<Color>(popN);
            for (int i = 0; i < popN; i++)
            {
                float t = popN == 1 ? 0f : (float)i / (popN - 1);
                stagePopColors.Add(Color.Lerp(def.derivativeColor, Color.white, 0.35f * t));
            }
        }

        stageTriggerXGrid = new List<float>(popN);
        float width = gridRenderer.gridSize.x;
        for (int i = 1; i <= popN; i++)
            stageTriggerXGrid.Add((i / (float)(popN + 1)) * width);

        storyMiddlePauseSeconds = def.storyPauseSeconds > 0.01f ? def.storyPauseSeconds : 1.65f;

        if (gridRenderer != null)
        {
            if (!gridThemeBaselineCaptured)
            {
                savedGridCenterLine = gridRenderer.centerLine;
                savedGridOutsideLine = gridRenderer.outsideLine;
                gridThemeBaselineCaptured = true;
            }

            if (def.applyGridTheming)
            {
                gridRenderer.centerLine = def.gridCenterLineTheming;
                gridRenderer.outsideLine = def.gridOutsideLineTheming;
            }
            else
            {
                gridRenderer.centerLine = savedGridCenterLine;
                gridRenderer.outsideLine = savedGridOutsideLine;
            }

            gridRenderer.enabled = false;
            gridRenderer.enabled = true;
        }

        // Story + roleplay intro run from <see cref="LoadLevelFullRoutine"/> after the world is built.
    }

    /// <summary>
    /// Builds platforms after plot refresh; then optional roleplay “page”, then the ordinary story banner fade.
    /// </summary>
    private IEnumerator LoadLevelFullRoutine(LevelDefinition def)
    {
        if (playerController != null)
            playerController.SetInputLocked(true);

        yield return LoadWorldAfterThemeChange(def);

        bool showIntro = !skipNextStageIntro
                         && !graphCalculatorMode
                         && currentLevelIndex >= 0
                         && currentLevelIndex < StageRoleplayLibrary.Count;

        skipNextStageIntro = false;

        if (showIntro)
            yield return RunEnumerated(RunStageIntroCoroutine(def));

        // Match original behaviour: player can move while the top story banner fades.
        if (playerController != null)
            playerController.SetInputLocked(false);

        if (storyText != null)
        {
            RefreshStoryBannerForCurrentMode(def);
            yield return RunEnumerated(FadeStoryTextRoutine());
        }

        levelFlowRoutine = null;
    }

    /// <summary>Runs a child iterator inside this MonoBehaviour coroutine without starting a nested Unity coroutine.</summary>
    private static IEnumerator RunEnumerated(IEnumerator inner)
    {
        if (inner == null)
            yield break;

        while (inner.MoveNext())
            yield return inner.Current;
    }

    private IEnumerator RunStageIntroCoroutine(LevelDefinition def)
    {
        EnsureStageIntroOverlay();
        if (stageIntroRoot == null || stageIntroCanvasGroup == null)
            yield break;

        stageIntroSkipRequested = false;

        string title = LocalizationManager.GetWithFallback($"level.{currentLevelIndex}", def.levelName);
        stageIntroTitle.text = title;
        stageIntroBody.text = TmpLatex.Process(StageRoleplayLibrary.GetRoleplayText(currentLevelIndex));
        LocalizationManager.ApplyTextDirection(stageIntroTitle);
        LocalizationManager.ApplyTextDirection(stageIntroBody);
        stageIntroHint.text = LocalizationManager.Get("ui.stage_intro_hint", "Tap to continue");
        LocalizationManager.ApplyTextDirection(stageIntroHint);

        stageIntroRoot.SetActive(true);
        stageIntroCanvasGroup.alpha = 0f;
        stageIntroCanvasGroup.interactable = false;
        stageIntroCanvasGroup.blocksRaycasts = false;

        float fadeIn = 0.38f;
        float t = 0f;
        while (t < fadeIn)
        {
            t += Time.unscaledDeltaTime;
            stageIntroCanvasGroup.alpha = Mathf.SmoothStep(0f, 1f, t / fadeIn);
            yield return null;
        }

        stageIntroCanvasGroup.alpha = 1f;
        stageIntroCanvasGroup.interactable = true;
        stageIntroCanvasGroup.blocksRaycasts = true;

        const float maxWait = 6f;
        const float minBeforeSkip = 0.42f;
        float waited = 0f;
        while (waited < maxWait)
        {
            waited += Time.unscaledDeltaTime;
            if (stageIntroSkipRequested && waited >= minBeforeSkip)
                break;
            yield return null;
        }

        stageIntroCanvasGroup.interactable = false;
        stageIntroCanvasGroup.blocksRaycasts = false;

        float fadeOut = 0.32f;
        t = 0f;
        while (t < fadeOut)
        {
            t += Time.unscaledDeltaTime;
            stageIntroCanvasGroup.alpha = Mathf.SmoothStep(1f, 0f, t / fadeOut);
            yield return null;
        }

        stageIntroCanvasGroup.alpha = 0f;
        stageIntroRoot.SetActive(false);
    }

    private void EnsureStageIntroOverlay()
    {
        if (stageIntroRoot != null)
            return;

        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
            return;

        var safe = MobileUiRoots.GetSafeContentParent(canvas.transform);
        var parent = safe != null ? safe : canvas.transform;

        stageIntroRoot = new GameObject("StageIntroOverlay");
        var rootRt = stageIntroRoot.AddComponent<RectTransform>();
        rootRt.SetParent(parent, false);
        rootRt.SetAsLastSibling();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        stageIntroCanvasGroup = stageIntroRoot.AddComponent<CanvasGroup>();
        stageIntroCanvasGroup.alpha = 0f;
        stageIntroCanvasGroup.blocksRaycasts = false;

        var dimGo = new GameObject("Dim");
        var dimRt = dimGo.AddComponent<RectTransform>();
        dimRt.SetParent(rootRt, false);
        dimRt.anchorMin = Vector2.zero;
        dimRt.anchorMax = Vector2.one;
        dimRt.offsetMin = Vector2.zero;
        dimRt.offsetMax = Vector2.zero;
        var dimImg = dimGo.AddComponent<Image>();
        dimImg.color = new Color(0.04f, 0.05f, 0.08f, 0.88f);
        dimImg.raycastTarget = false;

        var panelGo = new GameObject("Panel");
        var panelRt = panelGo.AddComponent<RectTransform>();
        panelRt.SetParent(rootRt, false);
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(Mathf.Min(760f, Screen.width * 0.9f), Mathf.Min(520f, Screen.height * 0.72f));
        var panelImg = panelGo.AddComponent<Image>();
        RuntimeUiPolish.UseRoundedSliced(panelImg);
        panelImg.color = new Color(0.1f, 0.11f, 0.15f, 0.97f);
        panelImg.raycastTarget = false;
        RuntimeUiPolish.ApplyDropShadow(panelRt, new Vector2(2f, -4f), 0.35f);

        var titleGo = new GameObject("Title");
        var titleRt = titleGo.AddComponent<RectTransform>();
        titleRt.SetParent(panelRt, false);
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.offsetMin = new Vector2(22f, -120f);
        titleRt.offsetMax = new Vector2(-22f, -16f);
        stageIntroTitle = titleGo.AddComponent<TextMeshProUGUI>();
        stageIntroTitle.fontSize = UiTypography.Scale(30);
        stageIntroTitle.fontStyle = FontStyles.Bold;
        stageIntroTitle.alignment = TextAlignmentOptions.Top;
        stageIntroTitle.color = RuntimeUiPolish.TitleIvory;
        stageIntroTitle.richText = true;
        stageIntroTitle.textWrappingMode = TextWrappingModes.Normal;
        stageIntroTitle.raycastTarget = false;
        ApplyPrimaryUiTypography(stageIntroTitle, FindPrimaryEquationTmp(), outlineWidth: 0.1f, outlineAlpha: 0.4f);

        var bodyGo = new GameObject("Body");
        var bodyRt = bodyGo.AddComponent<RectTransform>();
        bodyRt.SetParent(panelRt, false);
        bodyRt.anchorMin = new Vector2(0f, 0.22f);
        bodyRt.anchorMax = new Vector2(1f, 1f);
        bodyRt.offsetMin = new Vector2(22f, 8f);
        bodyRt.offsetMax = new Vector2(-22f, -132f);
        stageIntroBody = bodyGo.AddComponent<TextMeshProUGUI>();
        stageIntroBody.fontSize = UiTypography.Scale(23);
        stageIntroBody.alignment = TextAlignmentOptions.Top;
        stageIntroBody.color = new Color(0.92f, 0.93f, 0.96f, 1f);
        stageIntroBody.richText = true;
        stageIntroBody.textWrappingMode = TextWrappingModes.Normal;
        stageIntroBody.lineSpacing = 2f;
        stageIntroBody.raycastTarget = false;
        ApplyPrimaryUiTypography(stageIntroBody, FindPrimaryEquationTmp(), outlineWidth: 0.08f, outlineAlpha: 0.35f);

        var hintGo = new GameObject("Hint");
        var hintRt = hintGo.AddComponent<RectTransform>();
        hintRt.SetParent(panelRt, false);
        hintRt.anchorMin = new Vector2(0f, 0f);
        hintRt.anchorMax = new Vector2(1f, 0f);
        hintRt.pivot = new Vector2(0.5f, 0f);
        hintRt.offsetMin = new Vector2(16f, 14f);
        hintRt.offsetMax = new Vector2(-16f, 76f);
        stageIntroHint = hintGo.AddComponent<TextMeshProUGUI>();
        stageIntroHint.fontSize = UiTypography.Scale(19);
        stageIntroHint.alignment = TextAlignmentOptions.Bottom;
        stageIntroHint.color = new Color(0.75f, 0.8f, 0.95f, 0.85f);
        stageIntroHint.fontStyle = FontStyles.Italic;
        stageIntroHint.richText = true;
        stageIntroHint.raycastTarget = false;
        ApplyPrimaryUiTypography(stageIntroHint, FindPrimaryEquationTmp(), outlineWidth: 0.06f, outlineAlpha: 0.3f);

        var tapGo = new GameObject("TapToContinue");
        var tapRt = tapGo.AddComponent<RectTransform>();
        tapRt.SetParent(rootRt, false);
        tapRt.SetAsLastSibling();
        tapRt.anchorMin = Vector2.zero;
        tapRt.anchorMax = Vector2.one;
        tapRt.offsetMin = Vector2.zero;
        tapRt.offsetMax = Vector2.zero;
        var tapImg = tapGo.AddComponent<Image>();
        tapImg.color = new Color(0f, 0f, 0f, 0.001f);
        tapImg.raycastTarget = true;
        var tapBtn = tapGo.AddComponent<Button>();
        tapBtn.targetGraphic = tapImg;
        tapBtn.transition = Selectable.Transition.None;
        tapBtn.onClick.AddListener(() => stageIntroSkipRequested = true);

        stageIntroRoot.SetActive(false);
    }

    /// <summary>
    /// After theme swap, FunctionPlotter.Update repopulates points; we defer one frame so
    /// <see cref="LineRendererUI.points"/> / derivative lists are current before sampling columns.
    /// </summary>
    private IEnumerator LoadWorldAfterThemeChange(LevelDefinition def)
    {
        // Wait for the plot to regenerate points with the new parameters.
        yield return null;
        yield return new WaitForEndOfFrame();

        if (obstacleGenerator == null || playerController == null)
            yield break;

        var gridSize = gridRenderer.gridSize;

        var unitWidth = cartesianPlaneRect.rect.width / (float)gridSize.x;
        var unitHeight = cartesianPlaneRect.rect.height / (float)gridSize.y;
        obstacleGenerator.SetLayout(obstaclesRoot, gridSize, unitWidth, unitHeight);

        EnsureRiemannRenderer();
        if (riemannRenderer != null)
            riemannRenderer.Rebuild(def, functionPlotter);

        var curvePoints = curveRenderer.points;
        var derivPoints = derivRenderer.points;

        var world = obstacleGenerator.GenerateWorld(def, curvePoints, derivPoints, functionPlotter);
        playerController.SetWorld(world);
        playerController.ResetToSpawn(world);
    }

    private IEnumerator FadeStoryTextRoutine()
    {
        if (storyText == null)
            yield break;

        storyText.color = new Color(storyText.color.r, storyText.color.g, storyText.color.b, 0f);

        float t = 0f;
        float fadeIn = 0.25f;
        while (t < fadeIn)
        {
            t += Time.deltaTime;
            float a = Mathf.SmoothStep(0f, 1f, t / fadeIn);
            storyText.color = new Color(storyText.color.r, storyText.color.g, storyText.color.b, a);
            yield return null;
        }

        yield return new WaitForSeconds(storyMiddlePauseSeconds);

        t = 0f;
        float fadeOut = 0.35f;
        while (t < fadeOut)
        {
            t += Time.deltaTime;
            float a = Mathf.SmoothStep(1f, 0f, t / fadeOut);
            storyText.color = new Color(storyText.color.r, storyText.color.g, storyText.color.b, a);
            yield return null;
        }
    }

    /// <summary>HUD refresh + derivative “pop” triggers based on player X in grid space.</summary>
    private void Update()
    {
        if (graphCalculatorMode)
            return;

        if (playerController == null || stageTriggerXGrid == null)
            return;

        RefreshStageHud();

        // Trigger derivative "pops" at stage boundaries.
        while (nextStageIndex < stageTriggerXGrid.Count)
        {
            if (playerController.PlayerCenterGrid.x >= stageTriggerXGrid[nextStageIndex])
            {
                var popColor = nextStageIndex < stagePopColors.Count ? stagePopColors[nextStageIndex] : derivRenderer.color;
                popAnimator.Pop(popColor);
                derivRenderer.color = popColor;
                nextStageIndex++;
            }
            else
            {
                break;
            }
        }
    }

    private void RestartCurrentLevel()
    {
        if (isRestarting)
            return;

        StartCoroutine(RestartRoutine());
    }

    private IEnumerator RestartRoutine()
    {
        isRestarting = true;
        yield return new WaitForSeconds(restartDelaySeconds);
        skipNextStageIntro = true;
        LoadLevel(currentLevelIndex);
    }

    private void AdvanceLevel()
    {
        if (levels.Count == 0)
            return;

        int next = currentLevelIndex + 1;
        if (next >= levels.Count)
            next = 0; // loop for now

        LoadLevel(next);
    }
}

