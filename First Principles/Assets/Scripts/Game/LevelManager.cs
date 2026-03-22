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
    /// <summary>Scene-authored Cartesian plane size (fallback 1820×980); used to scale down on narrow portrait screens.</summary>
    private Vector2 _cartesianGameplayDesignSize = new Vector2(1820f, 980f);
    private Vector2 _lastSyncedCartesianRectSize = new Vector2(-1f, -1f);

    private GraphObstacleGenerator obstacleGenerator;
    private PlayerControllerUI2D playerController;
    private DerivativePopAnimator popAnimator;
    private RiemannStripRendererUI riemannRenderer;

    private RectTransform obstaclesRoot;

    private TextMeshProUGUI storyText;
    private TextMeshProUGUI stageHudText;
    private TextMeshProUGUI scoreHudText;
    private int lastStageHudKey = int.MinValue;
    private Sprite cachedHudPanelSprite;
    private float storyMiddlePauseSeconds = 1.65f;

    private bool gridThemeBaselineCaptured;
    private Color savedGridCenterLine;
    private Color savedGridOutsideLine;

    /// <summary>Runtime-built list of stages (also representable as LevelDefinition assets).</summary>
    private readonly List<LevelDefinition> levels = new List<LevelDefinition>();
    private int currentLevelIndex;

    private const int GameplayStartingScore = 100;
    private const int DerivativeTouchPenaltyPoints = 5;

    /// <summary>Touch f′ penalty score (starts at <see cref="GameplayStartingScore"/> each level).</summary>
    private int gameplayScore = GameplayStartingScore;

    /// <summary>How many stage boundary thresholds the player has already crossed (derivative pops).</summary>
    private int nextStageIndex;
    private List<float> stageTriggerXGrid;
    private List<Color> stagePopColors;

    private bool isRestarting;
    private Coroutine storyFadeRoutine;
    private Coroutine levelFlowRoutine;
    /// <summary>After death-restart, skip the full-screen roleplay card for the same stage.</summary>
    private bool skipNextStageIntro;

    /// <summary>After death-restart, skip the spawn spotlight so restarts stay snappy.</summary>
    private bool skipSpawnSpotlight;

    [Header("Spawn spotlight")]
    [Tooltip("Total time from level ready until controls unlock from spotlight (fade in + hold + fade out).")]
    [SerializeField] private float spawnSpotlightTotalSeconds = 1.5f;

    [Tooltip("Optional; if null, loads Resources/UI_SpotlightDim then Shader.Find.")]
    [SerializeField] private Shader spawnSpotlightShader;

    private GameObject spawnSpotlightRoot;
    private Material spawnSpotlightMaterial;
    private RectTransform spawnSpotlightRect;

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
        RefreshMathConceptsLabelLocalized();
        RefreshStageHudLocalizedForce();

        if (graphCalculatorMode)
        {
            RefreshStoryBannerForCurrentMode(null);
            TightenGraphCalculatorStoryBanner();
            var canvas = FindAnyObjectByType<Canvas>();
            var safe = canvas != null ? MobileUiRoots.GetSafeContentParent(canvas.transform) as RectTransform : null;
            var calcParent = safe != null ? safe : canvas?.transform as RectTransform;
            float bridge = DeviceLayout.PreferOnScreenGameControls ? DeviceLayout.TouchHintVerticalOffset : 22f;
            float transRow = bridge + 74f;
            float eqBottom = GraphCalculatorEquationPanelBottomY(transRow);
            if (functionPlotter != null && calcParent != null)
                GraphCalculatorEquationPanel.Ensure(calcParent, functionPlotter, FindPrimaryEquationTmp(), eqBottom, 108f);
            var paramTmp = GameObject.Find("GraphicCalculatorParamHint")?.GetComponent<TextMeshProUGUI>();
            LayoutGraphCalculatorParamHintBelowStory(paramTmp, storyText != null ? storyText.rectTransform : null,
                DeviceLayout.IsTabletLike() ? 144f : 132f, 10f);
        }
        else if (levels.Count > 0 && currentLevelIndex >= 0 && currentLevelIndex < levels.Count)
            RefreshStoryBannerForCurrentMode(levels[currentLevelIndex]);

        RefreshScoreHud();
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

    private void RefreshStageHudLocalizedForce()
    {
        lastStageHudKey = int.MinValue;
        RefreshStageHud();
        RefreshScoreHud();
    }

    private void RefreshStoryBannerForCurrentMode(LevelDefinition def)
    {
        if (storyText == null)
            return;

        if (graphCalculatorMode || def == null)
        {
            storyText.text = TmpLatex.Process(LocalizationManager.Get("graph.calculator_intro",
                "<b>Graphing calculator mode</b>\n" +
                "<size=88%>Type almost any <b>f(u)</b> in the field (variable <b>x</b> in your formula); <b>Trans</b> adjusts A, k, C, D; <b>Scale</b> · <b>pinch</b> zoom the window.</size>"));
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
        curveRenderer = LineRendererUI.FindPrimaryCurve();
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

        if (cartesianPlaneRect != null)
        {
            Vector2 sd = cartesianPlaneRect.sizeDelta;
            if (sd.sqrMagnitude > 10_000f)
                _cartesianGameplayDesignSize = sd;
        }

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
        // Always create the layer on phones/tablets; disable in graphing calculator so a later level load
        // (or any code that re-runs Ensure) does not depend on "first scene entry was platformer".
        if (mainCanvas != null && DeviceLayout.PreferOnScreenGameControls)
        {
            GameplayScreenTouchZones.EnsureForGameCanvas(mainCanvas.transform);
            GameplayScreenTouchZones.SetActiveForGameplayMode(!graphCalculatorMode);
        }

        // Wire callbacks.
        playerController.SetDeathCallback(RestartCurrentLevel);
        playerController.SetFinishCallback(AdvanceLevel);
        playerController.SetDerivativeLineGrazeScoringCallback(OnDerivativeLineGrazePenalty);
        if (derivRenderer != null)
            playerController.BindDerivativeRenderer(derivRenderer);
        ConfigureGameBackButtonDestination();

        if (!graphCalculatorMode)
        {
            FitCartesianPlaneForGameplay();
            SyncGameplayLayoutToCartesianPlane();
            EnsurePlatformerHudPresentation();
        }
    }

    /// <summary>
    /// Scales the fixed-design Cartesian plane to fit the grid background (portrait phones, notches, touch HUD).
    /// </summary>
    private void FitCartesianPlaneForGameplay()
    {
        if (graphCalculatorMode || cartesianPlaneRect == null)
            return;

        var parent = cartesianPlaneRect.parent as RectTransform;
        if (parent == null)
            return;

        float pw = parent.rect.width;
        float ph = parent.rect.height;
        if (pw < 2f || ph < 2f)
            return;

        bool mobile = DeviceLayout.PreferOnScreenGameControls;
        float hPad = mobile ? 8f : 14f;
        float aspectTall = ph / Mathf.Max(1f, pw);
        float topReserve;
        float bottomReserve;
        if (mobile)
        {
            // Portrait height fraction so graph + axes scale together on tall phones (not only TMP labels).
            float tallBlend = Mathf.Clamp01((aspectTall - 1.35f) / 0.82f);
            topReserve = Mathf.Clamp(ph * Mathf.Lerp(0.102f, 0.086f, tallBlend), 120f, 205f);
            float hintFloor = DeviceLayout.TouchHintVerticalOffset + 26f;
            bottomReserve = Mathf.Clamp(ph * Mathf.Lerp(0.128f, 0.112f, tallBlend), hintFloor, 255f);
        }
        else
        {
            topReserve = 138f;
            bottomReserve = 70f;
        }

        float maxW = Mathf.Max(64f, pw - hPad * 2f);
        float maxH = Mathf.Max(64f, ph - topReserve - bottomReserve);

        float baseW = Mathf.Max(1f, _cartesianGameplayDesignSize.x);
        float baseH = Mathf.Max(1f, _cartesianGameplayDesignSize.y);
        float s = Mathf.Min(1f, maxW / baseW, maxH / baseH);

        Vector2 newSize = new Vector2(baseW * s, baseH * s);
        float yNudge = mobile ? (bottomReserve - topReserve) * 0.18f : 0f;

        cartesianPlaneRect.anchorMin = new Vector2(0.5f, 0.5f);
        cartesianPlaneRect.anchorMax = new Vector2(0.5f, 0.5f);
        cartesianPlaneRect.pivot = new Vector2(0.5f, 0.5f);

        if ((cartesianPlaneRect.sizeDelta - newSize).sqrMagnitude < 0.25f &&
            Mathf.Abs(cartesianPlaneRect.anchoredPosition.y - yNudge) < 0.5f)
            return;

        cartesianPlaneRect.sizeDelta = newSize;
        cartesianPlaneRect.anchoredPosition = new Vector2(0f, yNudge);
    }

    /// <summary>
    /// Authoring used a fixed 1820×980 grid rect; after <see cref="FitCartesianPlaneForGameplay"/> the Cartesian plane
    /// shrinks on phones — stretch grid + curve renderers to fill it so axes, graphs, and obstacle math line up.
    /// </summary>
    private void StretchGameplayGridStackToCartesianPlane()
    {
        if (gridRenderer == null || cartesianPlaneRect == null)
            return;

        var gridRt = gridRenderer.rectTransform;
        gridRt.anchorMin = Vector2.zero;
        gridRt.anchorMax = Vector2.one;
        gridRt.pivot = new Vector2(0.5f, 0.5f);
        gridRt.offsetMin = Vector2.zero;
        gridRt.offsetMax = Vector2.zero;
        gridRt.anchoredPosition = Vector2.zero;

        for (int i = 0; i < gridRt.childCount; i++)
        {
            var lineRt = gridRt.GetChild(i) as RectTransform;
            if (lineRt == null)
                continue;
            lineRt.anchorMin = Vector2.zero;
            lineRt.anchorMax = Vector2.one;
            lineRt.pivot = new Vector2(0f, 0f);
            lineRt.offsetMin = Vector2.zero;
            lineRt.offsetMax = Vector2.zero;
            lineRt.anchoredPosition = Vector2.zero;
        }

        gridRenderer.SetVerticesDirty();
    }

    private void SyncGameplayLayoutToCartesianPlane()
    {
        if (graphCalculatorMode || cartesianPlaneRect == null || gridRenderer == null ||
            obstacleGenerator == null || playerController == null)
            return;

        StretchGameplayGridStackToCartesianPlane();

        Vector2 sz = cartesianPlaneRect.rect.size;
        var gridSize = gridRenderer.gridSize;

        if (obstaclesRoot != null)
        {
            obstaclesRoot.sizeDelta = sz;
            float uw = sz.x / gridSize.x;
            float uh = sz.y / gridSize.y;
            obstacleGenerator.SetLayout(obstaclesRoot, gridSize, uw, uh);
            obstacleGenerator.RefreshObstaclePixelLayout();
        }

        playerController.ApplyResponsivePlaneLayout(cartesianPlaneRect, gridSize);
    }

    private void LateUpdate()
    {
        if (graphCalculatorMode)
            return;
        if (cartesianPlaneRect == null || gridRenderer == null || obstacleGenerator == null || playerController == null)
            return;

        FitCartesianPlaneForGameplay();

        Vector2 sz = cartesianPlaneRect.rect.size;
        if ((sz - _lastSyncedCartesianRectSize).sqrMagnitude < 0.01f)
            return;

        _lastSyncedCartesianRectSize = sz;
        SyncGameplayLayoutToCartesianPlane();
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
            storyText.fontStyle = FontStyles.Bold;
            TightenGraphCalculatorStoryBanner();
        }

        // No STAGE / PTS HUD in calculator — hide the whole panel (not platformer scoring).
        if (stageHudText != null && stageHudText.transform.parent != null)
            stageHudText.transform.parent.gameObject.SetActive(false);
        else
        {
            if (stageHudText != null)
                stageHudText.gameObject.SetActive(false);
            if (scoreHudText != null)
                scoreHudText.gameObject.SetActive(false);
        }

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
        functionPlotter.autoScaleVertical = false;
        // Stretch [xStart,xEnd] across the grid so pinch / Scale update _autoMidX/_autoScaleX; axis ticks track (LabelManager).
        functionPlotter.autoScaleHorizontal = true;
        // Pinch / Scale: same plotter units per grid row as per column (square math window).
        functionPlotter.lockVerticalPlotScaleToHorizontalWindow = true;
        functionPlotter.showWindTunnelBackdrop = false;

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

        EnsureRiemannRenderer();
        if (riemannRenderer != null)
            riemannRenderer.ClearStrips();

        var canvas = FindAnyObjectByType<Canvas>();
        var safe = canvas != null ? MobileUiRoots.GetSafeContentParent(canvas.transform) as RectTransform : null;
        var hintParent = safe != null ? safe : canvas?.transform as RectTransform;
        float bridgeControls = DeviceLayout.PreferOnScreenGameControls ? DeviceLayout.TouchHintVerticalOffset : 22f;
        float transRowBottom = bridgeControls + 74f;
        float equationPanelBottomY = GraphCalculatorEquationPanelBottomY(transRowBottom);

        var equationStyleRef = FindPrimaryEquationTmp();
        if (equationStyleRef != null)
            equationStyleRef.fontStyle = FontStyles.Bold;

        GraphCalculatorEquationPanel.Ensure(hintParent, functionPlotter, equationStyleRef, equationPanelBottomY, 108f);

        var transGo = GameObject.Find("TransButton");
        var scaleGo = GameObject.Find("ScaleButton");
        LayoutCalculatorToolButtons(transGo, scaleGo, transRowBottom);

        TextMeshProUGUI paramHint = null;
        var legacyHint = GameObject.Find("FaxasGraphParamHint");
        if (legacyHint != null)
            legacyHint.name = "GraphicCalculatorParamHint";

        bool tabletHint = DeviceLayout.IsTabletLike();
        if (hintParent != null && GameObject.Find("GraphicCalculatorParamHint") == null)
        {
            var hintGo = new GameObject("GraphicCalculatorParamHint");
            var hrt = hintGo.AddComponent<RectTransform>();
            hrt.SetParent(hintParent, false);

            paramHint = hintGo.AddComponent<TextMeshProUGUI>();
            paramHint.richText = true;
            paramHint.textWrappingMode = TextWrappingModes.Normal;
            paramHint.overflowMode = TextOverflowModes.Overflow;
            paramHint.fontSize = UiTypography.Scale(tabletHint ? 31 : 27);
            paramHint.alignment = TextAlignmentOptions.Top;
            paramHint.color = new Color(0.9f, 0.93f, 0.98f, 0.96f);
            ApplyPrimaryUiTypography(paramHint, FindPrimaryEquationTmp(), outlineWidth: 0.12f, outlineAlpha: 0.45f);
            paramHint.fontStyle = FontStyles.Bold;
        }
        else if (GameObject.Find("GraphicCalculatorParamHint") != null)
        {
            paramHint = GameObject.Find("GraphicCalculatorParamHint").GetComponent<TextMeshProUGUI>();
            if (paramHint != null)
            {
                var eqRef = FindPrimaryEquationTmp();
                if (eqRef != null && eqRef.font != null)
                {
                    paramHint.font = eqRef.font;
                    if (eqRef.fontSharedMaterial != null)
                        paramHint.fontSharedMaterial = eqRef.fontSharedMaterial;
                }
                else
                    UiTypography.ApplyDefaultFontAsset(paramHint);
                paramHint.fontStyle = FontStyles.Bold;
            }
        }

        LayoutGraphCalculatorParamHintBelowStory(paramHint, storyText != null ? storyText.rectTransform : null,
            tabletHint ? 144f : 132f, 10f);

        ApplyGraphCalculatorControlButtonTypography(transGo);
        ApplyGraphCalculatorControlButtonTypography(scaleGo);

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

        var calcAnalysis = GetComponent<GraphCalculatorAnalysisControls>();
        if (calcAnalysis == null)
            calcAnalysis = gameObject.AddComponent<GraphCalculatorAnalysisControls>();
        calcAnalysis.Configure(functionPlotter, riemannRenderer, curveRenderer, equationStyleRef, transRowBottom);
    }

    /// <summary>Trans / Scale labels use the project TMP default font and bold weight in calculator mode.</summary>
    private static void ApplyGraphCalculatorControlButtonTypography(GameObject buttonRoot)
    {
        if (buttonRoot == null)
            return;
        var eqRef = FindPrimaryEquationTmp();
        foreach (var tmp in buttonRoot.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (eqRef != null && eqRef.font != null)
            {
                tmp.font = eqRef.font;
                if (eqRef.fontSharedMaterial != null)
                    tmp.fontSharedMaterial = eqRef.fontSharedMaterial;
            }
            else
                UiTypography.ApplyDefaultFontAsset(tmp);
            tmp.fontStyle = FontStyles.Bold;
        }
    }

    /// <summary>Shrink the tall story placeholder so the intro sits at the top; height follows TMP preferred size.</summary>
    void TightenGraphCalculatorStoryBanner()
    {
        if (storyText == null)
            return;

        storyText.alignment = TextAlignmentOptions.Top;
        Canvas.ForceUpdateCanvases();
        storyText.ForceMeshUpdate();
        float w = storyText.rectTransform.rect.width;
        if (w < 48f)
            w = Mathf.Max(320f, Screen.safeArea.width * 0.88f);
        Vector2 pv = storyText.GetPreferredValues(storyText.text, w, 0f);
        float h = Mathf.Clamp(pv.y + 14f, 52f, 280f);
        storyText.rectTransform.sizeDelta = new Vector2(0f, h);
    }

    /// <summary>Bottom Y for the f(u) panel so it sits just above the Deriv / ∫ row (matches <see cref="GraphCalculatorAnalysisControls.BuildToolButtons"/>).</summary>
    float GraphCalculatorEquationPanelBottomY(float transRowBottomY)
    {
        bool tablet = DeviceLayout.IsTabletLike();
        return GraphCalculatorAnalysisControls.DerivativeIntegralRowTopFromBottom(transRowBottomY, tablet) + 12f;
    }

    /// <summary>Places the Trans / Scale hint under the intro banner (calculator mode).</summary>
    static void LayoutGraphCalculatorParamHintBelowStory(TextMeshProUGUI hint, RectTransform storyRt, float blockHeight, float gap)
    {
        if (hint == null || storyRt == null)
            return;

        RectTransform hrt = hint.rectTransform;
        hrt.SetParent(storyRt.parent, false);
        hrt.anchorMin = storyRt.anchorMin;
        hrt.anchorMax = storyRt.anchorMax;
        hrt.pivot = new Vector2(0.5f, 1f);
        hrt.sizeDelta = new Vector2(0f, blockHeight);
        hrt.anchoredPosition = new Vector2(storyRt.anchoredPosition.x,
            storyRt.anchoredPosition.y - storyRt.sizeDelta.y - gap);
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
        img.sprite = TryGetSquareSprite();
        img.color = Color.clear;
        img.type = Image.Type.Simple;
        img.raycastTarget = false;

        var glyphGo = new GameObject("Glyph");
        var glyphRt = glyphGo.AddComponent<RectTransform>();
        glyphRt.SetParent(rect, false);
        glyphRt.anchorMin = Vector2.zero;
        glyphRt.anchorMax = Vector2.one;
        glyphRt.offsetMin = Vector2.zero;
        glyphRt.offsetMax = Vector2.zero;
        var glyphTmp = glyphGo.AddComponent<TextMeshProUGUI>();
        glyphTmp.text = PlayerGlyphSettings.GetSelectedGlyph();
        glyphTmp.alignment = TextAlignmentOptions.Center;
        glyphTmp.enableAutoSizing = true;
        glyphTmp.fontSizeMin = 22;
        glyphTmp.fontSizeMax = 512;
        glyphTmp.color = RuntimeUiPolish.PlayerBody;
        glyphTmp.richText = false;
        glyphTmp.raycastTarget = false;
        ApplyPrimaryUiTypography(glyphTmp, FindPrimaryEquationTmp(), outlineWidth: 0.18f, outlineAlpha: 0.42f);
        // ApplyPrimaryUiTypography copies reference fontStyle; keep the player glyph visibly bold/thick.
        glyphTmp.fontStyle = FontStyles.Bold;
        // Slight rim so the bright fill still separates from lime/purple graph lines.
        glyphTmp.outlineWidth = 0.22f;
        glyphTmp.outlineColor = new Color(0.04f, 0.06f, 0.12f, 0.40f);

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
            bool wideHud = DeviceLayout.IsTabletLike();
            panelRt.sizeDelta = new Vector2(wideHud ? 620f : 560f, wideHud ? 92f : 88f);
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
            textRt.anchorMin = new Vector2(0f, 0f);
            textRt.anchorMax = new Vector2(0.54f, 1f);
            textRt.offsetMin = new Vector2(18f, 12f);
            textRt.offsetMax = new Vector2(-6f, -14f);

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.richText = true;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.fontSize = UiTypography.Scale(28);
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = UiTypography.Scale(16);
            tmp.fontSizeMax = UiTypography.Scale(30);
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = new Color(0.94f, 0.95f, 0.98f, 1f);
            tmp.characterSpacing = 0.35f;
            tmp.lineSpacing = -4f;
            ApplyPrimaryUiTypography(tmp, equationStyle, outlineWidth: 0.16f, outlineAlpha: 0.55f);
            tmp.text = FormatStageHudLine(1, 1);

            stageHudText = tmp;

            var scoreGo = new GameObject("ScoreHud");
            var scoreRt = scoreGo.AddComponent<RectTransform>();
            scoreRt.SetParent(panelGo.transform, false);
            scoreRt.anchorMin = new Vector2(0.52f, 0f);
            scoreRt.anchorMax = new Vector2(1f, 1f);
            scoreRt.offsetMin = new Vector2(4f, 12f);
            scoreRt.offsetMax = new Vector2(-18f, -14f);
            var scoreTmp = scoreGo.AddComponent<TextMeshProUGUI>();
            scoreTmp.richText = true;
            scoreTmp.textWrappingMode = TextWrappingModes.Normal;
            scoreTmp.overflowMode = TextOverflowModes.Overflow;
            scoreTmp.fontSize = UiTypography.Scale(28);
            scoreTmp.enableAutoSizing = true;
            scoreTmp.fontSizeMin = UiTypography.Scale(16);
            scoreTmp.fontSizeMax = UiTypography.Scale(30);
            scoreTmp.alignment = TextAlignmentOptions.MidlineRight;
            scoreTmp.color = new Color(0.94f, 0.95f, 0.98f, 1f);
            scoreTmp.lineSpacing = -4f;
            ApplyPrimaryUiTypography(scoreTmp, equationStyle, outlineWidth: 0.16f, outlineAlpha: 0.55f);
            scoreHudText = scoreTmp;
            RefreshScoreHud();
        }

        CreateMathConceptsButtonIfNeeded(canvas, equationStyle);
    }

    /// <summary>Top-right control: opens <see cref="MathArticlesOverlay"/> with math sections scoped to the current level (full glossary in graphing-calculator mode).</summary>
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
        rt.sizeDelta = new Vector2(tablet ? 292f : 262f, tablet ? 60f : 56f);

        var img = go.AddComponent<Image>();
        RuntimeUiPolish.UseRoundedSliced(img);
        img.color = RuntimeUiPolish.AccentTeal;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        RuntimeUiPolish.ApplyButtonTransitions(btn, RuntimeUiPolish.AccentTeal,
            Color.Lerp(RuntimeUiPolish.AccentTeal, Color.white, 0.18f),
            Color.Lerp(RuntimeUiPolish.AccentTeal, Color.black, 0.25f));
        btn.onClick.AddListener(() => MathArticlesOverlay.Open(canvas.transform,
            graphCalculatorMode ? (int?)null : currentLevelIndex));
        RuntimeUiPolish.ApplyDropShadow(rt, new Vector2(2f, -3f), 0.3f);

        var textGo = new GameObject("Text");
        var trt = textGo.AddComponent<RectTransform>();
        trt.SetParent(go.transform, false);
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(12f, 6f);
        trt.offsetMax = new Vector2(-12f, -6f);

        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = LocalizationManager.Get("ui.math_concepts", "Math concepts");
        tmp.fontSize = UiTypography.Scale(tablet ? 30 : 27);
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = UiTypography.Scale(17);
        tmp.fontSizeMax = UiTypography.Scale(tablet ? 30 : 27);
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.overflowMode = TextOverflowModes.Truncate;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.92f, 0.97f, 1f, 1f);
        tmp.raycastTarget = false;
        ApplyPrimaryUiTypography(tmp, equationStyle, outlineWidth: 0.14f, outlineAlpha: 0.48f);
        tmp.fontStyle = FontStyles.Bold;
        LocalizationManager.ApplyTextDirection(tmp);
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
            $"<color=#9aa3b8><size=78%><b>{stageWord}</b></size></color>\n" +
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

    void ResetGameplayScoreForNewLevel()
    {
        gameplayScore = GameplayStartingScore;
        RefreshScoreHud();
    }

    void OnDerivativeLineGrazePenalty()
    {
        if (graphCalculatorMode)
            return;
        gameplayScore = Mathf.Max(0, gameplayScore - DerivativeTouchPenaltyPoints);
        RefreshScoreHud();
    }

    void RefreshScoreHud()
    {
        if (scoreHudText == null)
            return;
        string label = LocalizationManager.Get("hud.points", "PTS");
        scoreHudText.text =
            $"<color=#9aa3b8><size=78%><b>{label}</b></size></color>\n<b><color=#fde68a>{gameplayScore}</color></b>";
        LocalizationManager.ApplyTextDirection(scoreHudText);
    }

    /// <summary>
    /// Stage + score share <c>StageHudPanel</c>. Graphing calculator hides the whole panel; platformer needs
    /// both lines visible. Other UI can reparent later — re-activate panel + children and bring HUD forward.
    /// </summary>
    private void EnsurePlatformerHudPresentation()
    {
        if (graphCalculatorMode)
            return;

        if (stageHudText != null && stageHudText.transform.parent != null)
        {
            var stagePanel = stageHudText.transform.parent.gameObject;
            stagePanel.SetActive(true);
            stageHudText.gameObject.SetActive(true);
            if (scoreHudText != null)
            {
                scoreHudText.gameObject.SetActive(true);
                RefreshScoreHud();
            }

            stagePanel.transform.SetAsLastSibling();
        }
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
    /// Finds or creates <see cref="RiemannStripRendererUI"/> under the same parent as the main curve
    /// (matches <see cref="LineRendererUI"/> rect + draw order so strips align with the graph).
    /// </summary>
    private void EnsureRiemannRenderer()
    {
        if (riemannRenderer == null)
            riemannRenderer = FindAnyObjectByType<RiemannStripRendererUI>();

        if (riemannRenderer != null && curveRenderer != null)
        {
            var curveLineRt = curveRenderer.GetComponent<RectTransform>();
            Transform lineParent = curveLineRt != null ? curveLineRt.parent : null;
            if (lineParent != null)
            {
                if (riemannRenderer.transform.parent != lineParent)
                    riemannRenderer.transform.SetParent(lineParent, false);
                var riemannRt = riemannRenderer.GetComponent<RectTransform>();
                if (curveLineRt != null && riemannRt != null)
                {
                    riemannRt.anchorMin = curveLineRt.anchorMin;
                    riemannRt.anchorMax = curveLineRt.anchorMax;
                    riemannRt.pivot = curveLineRt.pivot;
                    riemannRt.anchoredPosition = curveLineRt.anchoredPosition;
                    riemannRt.sizeDelta = curveLineRt.sizeDelta;
                    riemannRt.localScale = curveLineRt.localScale;
                }

                riemannRenderer.gameObject.SetActive(true);
                riemannRenderer.enabled = true;
                riemannRenderer.transform.SetSiblingIndex(0);
            }
        }

        if (riemannRenderer != null || curveRenderer == null || cartesianPlaneRect == null)
            return;

        var go = new GameObject("RiemannStrips");
        RectTransform curveRt = curveRenderer.GetComponent<RectTransform>();
        Transform parentT = curveRt != null && curveRt.parent != null ? curveRt.parent : cartesianPlaneRect;
        go.transform.SetParent(parentT, false);
        var stripsRt = go.AddComponent<RectTransform>();
        if (curveRt != null)
        {
            stripsRt.anchorMin = curveRt.anchorMin;
            stripsRt.anchorMax = curveRt.anchorMax;
            stripsRt.pivot = curveRt.pivot;
            stripsRt.anchoredPosition = curveRt.anchoredPosition;
            stripsRt.sizeDelta = curveRt.sizeDelta;
            stripsRt.localScale = curveRt.localScale;
        }
        else
        {
            stripsRt.anchorMin = Vector2.zero;
            stripsRt.anchorMax = Vector2.one;
            stripsRt.offsetMin = Vector2.zero;
            stripsRt.offsetMax = Vector2.zero;
        }

        riemannRenderer = go.AddComponent<RiemannStripRendererUI>();
        riemannRenderer.raycastTarget = false;

        // Draw before the Function / derivative lines (same parent as curve).
        go.transform.SetSiblingIndex(0);
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
            riemannFillColor: new Color(0.95f, 0.35f, 0.4f, 0.32f),
            riemannPlatformCoverage: 0.58f
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
            riemannFillColor: new Color(0.25f, 0.55f, 0.95f, 0.3f),
            riemannPlatformCoverage: 0.58f
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
            riemannFillColor: new Color(0.45f, 0.85f, 0.55f, 0.28f),
            riemannPlatformCoverage: 0.58f
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
                "The big equation shows <b>r(θ)</b> with a <b>θ</b> window — an <b>r-vs-θ</b> plot (AP-style), not <b>y</b> versus Cartesian <b>x</b> on the labels. Later you convert to the plane with <b>x = r cos θ</b>, <b>y = r sin θ</b>.\n\n" +
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

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[34],
            FunctionType.ThermoAdiabaticPV,
            curveColor: new Color(0.98f, 0.52f, 0.28f, 1f),
            derivativeColor: new Color(0.45f, 0.82f, 0.98f, 1f),
            transA: 9.5f,
            transK: 0.38f,
            transC: -2.35f,
            transD: -16f,
            power: 2,
            baseN: 140,
            story:
                "<b>Thermodynamics — reversible adiabatic path</b> — for an ideal gas, <color=#fdba74>quasi-static adiabats</color> obey <b>PV<sup>γ</sup> = constant</b> (no heat exchange, work comes only from internal energy bookkeeping).\n\n" +
                "Here the horizontal walk is a scaled <color=#7dd3fc>“volume-like”</color> coordinate; height tracks <b>pressure mood</b> as P ∝ V<sup>−γ</sup>. We set <b>N = 140</b> so γ ≈ 1.40 — a familiar air/diatomic story problem vibe, not a lab instrument.\n\n" +
                "<size=92%><color=#a8b2d1>Teaching sketch only: real pistons have losses, gradients, and non-equilibrium corners — but the slope story you feel underfoot is still γ.</color></size>",
            derivativePopTriggerCountOverride: 4,
            applyGridTheming: true,
            gridCenter: new Color(0.38f, 0.18f, 0.1f, 0.36f),
            gridOutside: new Color(0.22f, 0.1f, 0.06f, 0.1f),
            storyPauseSecondsOverride: 2.85f,
            graphStep: 0.11f,
            levelXStart: -14f,
            levelXEnd: 14f
        ));

        // ---- Aerospace engineering & aerodynamics (indices 35–41) -----------------------------

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[35],
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
            storyPauseSecondsOverride: 2.65f,
            showWindTunnelBackdrop: true
        ));

        var dragPolarOverlays = new[]
        {
            new Color(0.55f, 0.68f, 0.9f, 0.95f),
            new Color(0.98f, 0.5f, 0.55f, 0.9f)
        };

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[36],
            FunctionType.AeroDragPolarTriple,
            curveColor: new Color(0.75f, 0.55f, 1f, 1f),
            derivativeColor: new Color(1f, 0.72f, 0.35f, 1f),
            transA: 0.072f,
            transK: 0.42f,
            transC: -2.02f,
            transD: 0f,
            power: 2,
            baseN: 2,
            story:
                "<b>Drag polar — three traces at once</b> — the graph shows <color=#93c5fd><b>parasitic (zero‑lift / profile) drag</b></color> as a flat baseline, <color=#fb7185><b>induced drag</b></color> as the bowl that grows with |C_L|, and <color=#c4b5fd><b>total C_D</b></color> as their sum (walk the thick total — same parabola as before).\n\n" +
                "Classic identity: <color=#c4b5fd>C_D = C_{D0} + K C_L²</color>; min‑drag C_L is where the marginal induced penalty balances mission speed/α choices.\n\n" +
                "<size=92%><color=#a8b2d1>Horizontal axis: u ~ C_L. Purple = C_D,tot (platforms); blue = C_D,par; coral = C_D,ind alone (from zero lift).</color></size>",
            derivativePopTriggerCountOverride: 3,
            applyGridTheming: true,
            gridCenter: new Color(0.35f, 0.25f, 0.5f, 0.36f),
            gridOutside: new Color(0.25f, 0.18f, 0.36f, 0.1f),
            storyPauseSecondsOverride: 2.65f,
            dragPolarOverlayColors: dragPolarOverlays,
            showWindTunnelBackdrop: true
        ));

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[37],
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
            storyPauseSecondsOverride: 2.6f,
            showWindTunnelBackdrop: true
        ));

        var aeroPhugColors = new[]
        {
            new Color(0.5f, 0.78f, 1f, 1f),
            new Color(1f, 0.55f, 0.4f, 1f),
            new Color(0.75f, 0.55f, 1f, 1f)
        };

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[38],
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
            storyPauseSecondsOverride: 2.75f,
            showWindTunnelBackdrop: true
        ));

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[39],
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
            storyPauseSecondsOverride: 2.55f,
            showWindTunnelBackdrop: true
        ));

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[40],
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
            storyPauseSecondsOverride: 2.45f,
            showWindTunnelBackdrop: true
        ));

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[41],
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
            storyPauseSecondsOverride: 2.65f,
            showWindTunnelBackdrop: true
        ));

        // Economics (42–43); Transforms (44–45); boss block (46–49); spring (50); Big O (51–58).
        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[42],
            FunctionType.EconomyDotcomBubbleStylized,
            curveColor: new Color(0.14f, 0.58f, 0.34f, 1f),
            derivativeColor: new Color(0.98f, 0.72f, 0.28f, 1f),
            transA: 2.35f,
            transK: 0.118f,
            transC: -2.38f,
            transD: 0f,
            power: 2,
            baseN: 2,
            story:
                "<b>Dot‑com bubble — stylized chart walk</b> — equity indices like the broad <color=#86efac>S&amp;P 500</color> (or the racier <color=#7dd3fc>Nasdaq Composite</color>) climbed through the late 1990s, then <color=#fca5a5>gapped down</color> as the 2000–02 tech hangover unwound years of euphoria.\n\n" +
                "This path is a <b>smooth teaching silhouette</b> — not downloaded tick data — but it catches the storytelling shape: **grind, parabolic enthusiasm, air pocket, slow rebuild**. Slopes and concavity still read like real market moods.\n\n" +
                "<size=92%><color=#a8b2d1>Educational allegory only; not investment advice or a replica of any index.</color></size>",
            derivativePopTriggerCountOverride: 4,
            applyGridTheming: true,
            gridCenter: new Color(0.18f, 0.32f, 0.22f, 0.38f),
            gridOutside: new Color(0.12f, 0.22f, 0.15f, 0.11f),
            storyPauseSecondsOverride: 2.95f,
            graphStep: 0.09f
        ));

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[43],
            FunctionType.EconomySubprime2008Stylized,
            curveColor: new Color(0.72f, 0.22f, 0.2f, 1f),
            derivativeColor: new Color(0.52f, 0.78f, 0.95f, 1f),
            transA: 2.5f,
            transK: 0.115f,
            transC: -2.42f,
            transD: 0f,
            power: 2,
            baseN: 2,
            story:
                "<b>Global financial crisis — stylized stress curve</b> — US <color=#fde047>housing & mortgage</color> risk, structured credit losses, and institutional fragility fed a <color=#fca5a5>violent repricing</color> in 2007–09 that spilled across banks, money markets, and real economies (familiar names in history books: Lehman’s collapse in Sept 2008 as a flashpoint).\n\n" +
                "Again: <b>no real GSPC series here</b> — just a qualitative spline with a **crest near complacency**, a **cliff**, and a **long crawl** that matches how people <i>remember</i> the V‑shock conversation.\n\n" +
                "<size=92%><color=#a8b2d1>Simplified drama for calculus class; markets are vastly richer than one line.</color></size>",
            derivativePopTriggerCountOverride: 4,
            applyGridTheming: true,
            gridCenter: new Color(0.36f, 0.14f, 0.12f, 0.36f),
            gridOutside: new Color(0.26f, 0.1f, 0.09f, 0.1f),
            storyPauseSecondsOverride: 3.05f,
            graphStep: 0.09f
        ));

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[44],
            FunctionType.TransformFourierSinc,
            curveColor: new Color(0.42f, 0.78f, 1f, 1f),
            derivativeColor: new Color(1f, 0.55f, 0.85f, 1f),
            transA: 2.8f,
            transK: 0.95f,
            transC: -1.35f,
            transD: 0f,
            power: 1,
            baseN: 2,
            story:
                "<b>Fourier transform — sinc glimpse</b> — a sharp pulse in time smears into a tall <color=#7dd3fc>sinc</color> in frequency: the side lobes are the price of band-limiting dreams.\n\n" +
                "Here the horizontal axis is an angular-frequency style <i>u</i>; height follows <b>sin(u)/u</b> (normalized to 1 at the origin). It is the magnitude mood of a rectangular window — not a full FFT engine, but the canonical homework picture.\n\n" +
                "<size=92%><color=#a8b2d1>Real transforms add phase; this stage is the clean symmetric <b>even sinc</b> silhouette.</color></size>",
            derivativePopTriggerCountOverride: 4,
            applyGridTheming: true,
            gridCenter: new Color(0.12f, 0.22f, 0.38f, 0.38f),
            gridOutside: new Color(0.08f, 0.14f, 0.22f, 0.11f),
            storyPauseSecondsOverride: 2.85f,
            graphStep: 0.085f,
            levelXStart: -18f,
            levelXEnd: 18f
        ));

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[45],
            FunctionType.TransformLaplaceCausalDecay,
            curveColor: new Color(0.55f, 0.95f, 0.65f, 1f),
            derivativeColor: new Color(0.98f, 0.72f, 0.38f, 1f),
            transA: 3.4f,
            transK: 0.72f,
            transC: -2.55f,
            transD: -6f,
            power: 1,
            baseN: 125,
            story:
                "<b>Laplace transform — causal exponential</b> — for <b>t ≥ 0</b>, the table favorite <color=#86efac>f(t) = e<sup>−at</sup></color> is the polite ancestor of every damped pole in your s-domain algebra.\n\n" +
                "We plot that decay with <b>N = 125</b> so the rate is about <b>a ≈ 1.25</b>: horizontal axis as time after the jump, vertical as amplitude.\n\n" +
                "<size=92%><color=#a8b2d1>Heaviside <b>H(t)</b> hides the past: nothing before <b>t = 0</b>. The transform pairs it with <b>1/(s+a)</b> in your cheat sheet dreams.</color></size>",
            derivativePopTriggerCountOverride: 4,
            applyGridTheming: true,
            gridCenter: new Color(0.14f, 0.28f, 0.18f, 0.36f),
            gridOutside: new Color(0.08f, 0.18f, 0.1f, 0.1f),
            storyPauseSecondsOverride: 2.8f,
            graphStep: 0.08f,
            levelXStart: -6f,
            levelXEnd: 14f
        ));

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[46],
            FunctionType.PolarGoldenLogSpiral,
            curveColor: new Color(0.99f, 0.86f, 0.38f, 1f),
            derivativeColor: new Color(0.62f, 0.42f, 0.98f, 1f),
            transA: 2.15f,
            transK: 1f,
            transC: -3.08f,
            transD: 0f,
            power: 1,
            baseN: 105,
            story:
                "<b>Secret boss — golden spiral</b> — nature’s favorite growth curve is a <color=#fbbf24>logarithmic spiral</color>: each time angle θ advances steadily, radius scales by a fixed factor tied to the <color=#fde68a>golden ratio φ = (1+√5)/2</color>.\n\n" +
                "Fibonacci rectangles, nautilus moods, and phyllotaxis in sunflowers all whisper the same proportion — here you walk the graph as <b>r(θ) ∝ φ^{kθ}</b> on a polar-style readout (horizontal = θ, vertical = r).\n\n" +
                "<size=92%><color=#a8b2d1>Exact classical spirals use arc-length subtleties; this stage is the clean calculus headline: exponential growth in φ as you turn.</color></size>",
            derivativePopTriggerCountOverride: 4,
            applyGridTheming: true,
            gridCenter: new Color(0.42f, 0.34f, 0.14f, 0.38f),
            gridOutside: new Color(0.22f, 0.17f, 0.08f, 0.12f),
            storyPauseSecondsOverride: 2.95f,
            graphStep: 0.075f,
            levelXStart: -11.5f,
            levelXEnd: 13.5f
        ));

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[47],
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
                "<b>True finale — Mandelbrot encore</b> — the <color=#a8b2d1>backdrop</color> is again the classic <b>c-plane</b> (Re horizontal, Im vertical) colored by <color=#a7f3d0>smooth escape time</color>; your path follows the same slice: height vs <color=#86efac>Im(c)</color> at fixed <color=#86efac>Re(c)</color>.\n\n" +
                "This is the <b>final boss gate</b>: every step still reads Julia–Mandelbrot folklore — bulbs, filaments, and the cardioid where behavior flips.\n\n" +
                "<size=92%><color=#a8b2d1>Fractional escape counts + <b>|Im|</b> symmetry on this slice — the Mandelbrot coastline in one graph.</color></size>",
            derivativePopTriggerCountOverride: 4,
            applyGridTheming: true,
            gridCenter: new Color(0.12f, 0.32f, 0.48f, 0.4f),
            gridOutside: new Color(0.08f, 0.18f, 0.28f, 0.12f),
            storyPauseSecondsOverride: 2.95f,
            graphStep: 0.14f,
            levelXStart: -16f,
            levelXEnd: 16f
        ));

        float lorenzBossX0 = -14f;
        float lorenzBossX1 = 14f;
        float lorenzBossK = LorenzAttractorSamples.TimeMax / Mathf.Max(lorenzBossX1 - lorenzBossX0, 0.01f);

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[48],
            FunctionType.ChaosLorenzButterflyX,
            curveColor: new Color(0.42f, 0.86f, 1f, 1f),
            derivativeColor: new Color(1f, 0.52f, 0.88f, 1f),
            transA: 6.85f,
            transK: lorenzBossK,
            transC: 0f,
            transD: lorenzBossX0,
            power: 1,
            baseN: 2,
            story:
                "<b>True finale — Chaos Theory</b> — the <color=#a8b2d1>horizontal axis</color> is a long stretch of <color=#86efac>simulation time</color>; height tracks the classic Lorenz <color=#7dd3fc>x(t)</color> with σ=10, ρ=28, β=8/3.\n\n" +
                "Watch how the attractor <b>keeps retuning</b>: the same law, sensitive dependence, a path that never quite repeats — <color=#fda4af>chaos theory</color> made legible as motion.\n\n" +
                "<size=92%><color=#a8b2d1>Normalized after burn-in so the wings fill your grid; the curve scrolls through phase so the stage feels alive.</color></size>",
            derivativePopTriggerCountOverride: 4,
            applyGridTheming: true,
            gridCenter: new Color(0.1f, 0.18f, 0.34f, 0.42f),
            gridOutside: new Color(0.07f, 0.12f, 0.22f, 0.12f),
            storyPauseSecondsOverride: 3.05f,
            graphStep: 0.11f,
            levelXStart: lorenzBossX0,
            levelXEnd: lorenzBossX1
        ));

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[49],
            FunctionType.PhysicsGravityWellInverseSqrt,
            curveColor: new Color(0.55f, 0.72f, 1f, 1f),
            derivativeColor: new Color(1f, 0.45f, 0.35f, 1f),
            transA: 4.2f,
            transK: 0.82f,
            transC: 1.35f,
            transD: 0.38f,
            power: 1,
            baseN: 2,
            story:
                "<b>BOSS: Black hole — gravity well</b> — the curve is a <color=#7dd3fc>softened 1/r</color> slice: depth spikes toward the center, then relaxes into a gentle far-field shoulder.\n\n" +
                "It is a <b>teaching stand-in</b> for how gravitational potential tightens near mass — not a full GR embedding, but the right <color=#fda4af>“falling in”</color> intuition on a 1D graph.\n\n" +
                "<size=92%><color=#a8b2d1>ε from <b>D</b> keeps the well finite so the derivative game stays fair; ride the rim, respect the steep core.</color></size>",
            derivativePopTriggerCountOverride: 4,
            applyGridTheming: true,
            gridCenter: new Color(0.06f, 0.08f, 0.22f, 0.48f),
            gridOutside: new Color(0.04f, 0.06f, 0.14f, 0.14f),
            storyPauseSecondsOverride: 3f,
            graphStep: 0.1f,
            levelXStart: -12f,
            levelXEnd: 12f
        ));

        var springMassColors = new[]
        {
            new Color(0.42f, 0.95f, 0.68f, 1f),
            new Color(1f, 0.52f, 0.42f, 1f),
            new Color(0.58f, 0.78f, 1f, 1f)
        };

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[50],
            FunctionType.SpringMassUndamped,
            curveColor: new Color(0.38f, 0.92f, 0.74f, 1f),
            derivativeColor: new Color(1f, 0.48f, 0.52f, 1f),
            transA: 1.08f,
            transK: 0.9f,
            transC: -2.08f,
            transD: 0f,
            power: 14,
            baseN: 2,
            story:
                "<b>Spring–mass without damping</b> — Hooke’s law says the restoring force points toward equilibrium: <color=#86efac><b>F = −k x</b></color> for displacement from rest.\n\n" +
                "For a block on a frictionless horizontal surface, Newton gives <color=#7dd3fc>m ẍ = −k x</color>, so <color=#fde047>x(t) = A cos(ωt) + x₀</color> with <color=#a5b4fc>ω = √(k/m)</color> — simple harmonic motion.\n\n" +
                "<size=92%><color=#a8b2d1>Horizontal axis here is a time-like <i>t</i> (via <b>u = k(x−D)</b>); height is position. Compare with <b>Engineering: damped oscillation</b> — same spring intuition with friction draining energy.</color></size>",
            derivativePopTriggerCountOverride: 3,
            applyGridTheming: true,
            gridCenter: new Color(0.14f, 0.36f, 0.32f, 0.38f),
            gridOutside: new Color(0.1f, 0.26f, 0.24f, 0.11f),
            levelStageColors: springMassColors,
            storyPauseSecondsOverride: 2.75f,
            graphStep: 0.085f,
            levelXStart: -14f,
            levelXEnd: 14f
        ));

        // --- Big O notation (algorithms): u = k(x−D) plays the role of input size n on the graph ---
        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[51],
            FunctionType.Power,
            curveColor: new Color(0.65f, 0.85f, 1f, 1f),
            derivativeColor: new Color(1f, 0.78f, 0.45f, 1f),
            transA: 1.85f,
            transK: 0.12f,
            transC: -0.35f,
            transD: 0f,
            power: 0,
            baseN: 2,
            story:
                "<b>Big O: O(1)</b> — constant time: the work doesn’t grow with input size. Here <color=#7dd3fc>u⁰ = 1</color>, so height stays flat as <i>u</i> (our stand-in for <i>n</i>) marches.\n\n" +
                "<size=92%><color=#a8b2d1>Hash lookups and array indexing are classic O(1) <i>when the model fits</i> — the graph is the calm horizontal proof.</color></size>",
            derivativePopTriggerCountOverride: 3,
            graphStep: 0.11f,
            levelXStart: -12f,
            levelXEnd: 12f
        ));

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[52],
            FunctionType.NaturalLog,
            curveColor: new Color(0.5f, 0.92f, 0.78f, 1f),
            derivativeColor: new Color(0.98f, 0.55f, 0.82f, 1f),
            transA: 1.05f,
            transK: 0.38f,
            transC: -2.35f,
            transD: 0f,
            power: 1,
            baseN: 2,
            story:
                "<b>Big O: O(log n)</b> — each step shrinks the problem by a constant factor (binary search, balanced trees). <color=#86efac>ln u</color> climbs gently forever.\n\n" +
                "<size=92%><color=#a8b2d1>Domain needs <i>u</i> &gt; 0 — we sample away from zero so the law stays finite on the grid.</color></size>",
            derivativePopTriggerCountOverride: 3,
            graphStep: 0.1f,
            levelXStart: 0.45f,
            levelXEnd: 16f
        ));

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[53],
            FunctionType.SquareRoot,
            curveColor: new Color(0.72f, 0.78f, 1f, 1f),
            derivativeColor: new Color(1f, 0.62f, 0.48f, 1f),
            transA: 1.15f,
            transK: 0.36f,
            transC: -2.05f,
            transD: 0f,
            power: 1,
            baseN: 2,
            story:
                "<b>Big O: O(√n)</b> — sublinear growth: think nested loops that only walk up to √n, or some number-theory sieves. <color=#7dd3fc>√u</color> bends upward but bows to log in the long run.\n\n" +
                "<size=92%><color=#a8b2d1>We keep <i>u</i> ≥ 0 so the square root stays real.</color></size>",
            derivativePopTriggerCountOverride: 3,
            graphStep: 0.095f,
            levelXStart: 0f,
            levelXEnd: 15f
        ));

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[54],
            FunctionType.Power,
            curveColor: new Color(0.55f, 0.9f, 1f, 1f),
            derivativeColor: new Color(1f, 0.5f, 0.65f, 1f),
            transA: 0.34f,
            transK: 0.5f,
            transC: -2.12f,
            transD: 0f,
            power: 1,
            baseN: 2,
            story:
                "<b>Big O: O(n)</b> — scan the input once: summing a list, finding a max, a single loop. <color=#86efac>u¹</color> is the straight-ahead ramp.\n\n" +
                "<size=92%><color=#a8b2d1>Slope (derivative) stays in the same growth class — still linear in this stylized picture.</color></size>",
            derivativePopTriggerCountOverride: 3,
            graphStep: 0.1f,
            levelXStart: -9f,
            levelXEnd: 9f
        ));

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[55],
            FunctionType.BigONLogN,
            curveColor: new Color(0.48f, 0.82f, 1f, 1f),
            derivativeColor: new Color(1f, 0.58f, 0.78f, 1f),
            transA: 0.22f,
            transK: 0.48f,
            transC: -2.45f,
            transD: 0f,
            power: 1,
            baseN: 2,
            story:
                "<b>Big O: O(n log n)</b> — the sweet spot for sorting lower bounds (comparison sorts) and many divide steps. <color=#7dd3fc>u ln u</color> pulls away from linear but loses to n².\n\n" +
                "<size=92%><color=#a8b2d1>We clamp the small‑u end so ln doesn’t sing on zero — same idea as “n big enough” in proofs.</color></size>",
            derivativePopTriggerCountOverride: 3,
            graphStep: 0.095f,
            levelXStart: 0.5f,
            levelXEnd: 14f
        ));

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[56],
            FunctionType.Power,
            curveColor: new Color(0.6f, 0.72f, 1f, 1f),
            derivativeColor: new Color(1f, 0.52f, 0.42f, 1f),
            transA: 0.042f,
            transK: 0.46f,
            transC: -2f,
            transD: 0f,
            power: 2,
            baseN: 2,
            story:
                "<b>Big O: O(n²)</b> — nested loops, many pairwise checks, naive matrix setups. <color=#fda4af>u²</color> accelerates — the derivative warns you early.\n\n" +
                "<size=92%><color=#a8b2d1>When you see the parabola in complexity class, look for a double walk over data.</color></size>",
            derivativePopTriggerCountOverride: 3,
            graphStep: 0.09f,
            levelXStart: -7.5f,
            levelXEnd: 7.5f
        ));

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[57],
            FunctionType.Power,
            curveColor: new Color(0.52f, 0.68f, 0.98f, 1f),
            derivativeColor: new Color(1f, 0.48f, 0.55f, 1f),
            transA: 0.0055f,
            transK: 0.34f,
            transC: -1.55f,
            transD: 0f,
            power: 3,
            baseN: 2,
            story:
                "<b>Big O: O(n³)</b> — triple loops, naive Floyd–Warshall mood, dense cubic work. <color=#fbbf24>u³</color> rockets faster than n² — small constants don’t save you forever.\n\n" +
                "<size=92%><color=#a8b2d1>Good algorithms often <b>avoid</b> this exponent; the graph shows why.</color></size>",
            derivativePopTriggerCountOverride: 3,
            graphStep: 0.085f,
            levelXStart: -6f,
            levelXEnd: 6f
        ));

        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[58],
            FunctionType.Exponential,
            curveColor: new Color(0.58f, 0.95f, 0.72f, 1f),
            derivativeColor: new Color(1f, 0.45f, 0.88f, 1f),
            transA: 0.075f,
            transK: 0.42f,
            transC: -1.15f,
            transD: 0f,
            power: 1,
            baseN: 2,
            story:
                "<b>Big O: O(2ⁿ)</b> — exhaustive subsets, brute-force SAT-ish nightmares. <color=#86efac>2ᵘ</color> explodes; we keep the window modest so the grid survives.\n\n" +
                "<size=92%><color=#a8b2d1>Memoization, pruning, and smarter structure exist precisely because this curve leaves polite bounds.</color></size>",
            derivativePopTriggerCountOverride: 3,
            graphStep: 0.11f,
            levelXStart: -5.5f,
            levelXEnd: 3.5f
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
        float? levelXEnd = null,
        Color[] dragPolarOverlayColors = null,
        float? riemannPlatformCoverage = null,
        bool showWindTunnelBackdrop = false)
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
        if (riemannPlatformCoverage.HasValue)
            def.riemannPlatformCoverage = Mathf.Clamp(riemannPlatformCoverage.Value, 0.22f, 1f);

        if (dragPolarOverlayColors != null && dragPolarOverlayColors.Length >= 2)
        {
            def.dragPolarOverlayColors = new List<Color>
            {
                dragPolarOverlayColors[0],
                dragPolarOverlayColors[1]
            };
        }

        def.showWindTunnelBackdrop = showWindTunnelBackdrop;

        return def;
    }

    /// <summary>Loads a level by index, applies theme, fades story, rebuilds collision world.</summary>
    private void LoadLevel(int index)
    {
        if (levels.Count == 0 || functionPlotter == null)
            return;

        if (!graphCalculatorMode)
            ResetGameplayScoreForNewLevel();

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
        functionPlotter.autoScaleVertical = def.autoFitGraphVertical;
        functionPlotter.verticalFillFraction = def.graphVerticalFillFraction;
        functionPlotter.autoScaleHorizontal = def.autoFitGraphHorizontal;
        functionPlotter.horizontalFillFraction = def.graphHorizontalFillFraction;
        functionPlotter.lockVerticalPlotScaleToHorizontalWindow = false;

        functionPlotter.transA = def.transA;
        functionPlotter.transK = def.transK;
        functionPlotter.transC = def.transC;
        functionPlotter.transD = def.transD;

        functionPlotter.power = def.power;
        functionPlotter.baseN = def.baseN;
        functionPlotter.differentiate = true;
        functionPlotter.showWindTunnelBackdrop = def.showWindTunnelBackdrop;

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
        derivRenderer.thickness = DerivRendererUI.DefaultThicknessPixels;
        if (popAnimator != null)
            popAnimator.SyncRestThicknessFromTarget();

        if (def.functionType == FunctionType.AeroDragPolarTriple)
        {
            Color oc0 = new Color(0.55f, 0.68f, 0.9f, 0.95f);
            Color oc1 = new Color(0.98f, 0.5f, 0.55f, 0.9f);
            if (def.dragPolarOverlayColors != null && def.dragPolarOverlayColors.Count >= 2)
            {
                oc0 = def.dragPolarOverlayColors[0];
                oc1 = def.dragPolarOverlayColors[1];
            }

            functionPlotter.ConfigureDragPolarOverlayColors(oc0, oc1);
        }

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
    /// Stops full-screen overlays that can survive <see cref="StopCoroutine"/> (same scene) and block touch raycasts.
    /// </summary>
    private void TearDownInterruptedLevelFlowUi()
    {
        if (stageIntroRoot != null)
        {
            if (stageIntroCanvasGroup != null)
            {
                stageIntroCanvasGroup.blocksRaycasts = false;
                stageIntroCanvasGroup.interactable = false;
            }

            stageIntroRoot.SetActive(false);
        }

        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
            return;

        var safe = MobileUiRoots.GetSafeContentParent(canvas.transform);
        Transform parent = safe != null ? safe : canvas.transform;
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            var c = parent.GetChild(i);
            if (c.name == "MobileControlGuideOverlay")
                Destroy(c.gameObject);
        }
    }

    /// <summary>
    /// Re-creates / re-parents the full-screen touch catcher after layout so it stays above the graph stack.
    /// </summary>
    private void EnsureGameplayTouchZonesAfterLevelReady()
    {
        if (graphCalculatorMode || !DeviceLayout.PreferOnScreenGameControls)
            return;

        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
            return;

        GameplayScreenTouchZones.EnsureForGameCanvas(canvas.transform);
        GameplayScreenTouchZones.SetActiveForGameplayMode(true);
    }

    /// <summary>
    /// Builds platforms after plot refresh; then optional roleplay “page”, then the ordinary story banner fade.
    /// </summary>
    private IEnumerator LoadLevelFullRoutine(LevelDefinition def)
    {
        TearDownInterruptedLevelFlowUi();

        if (playerController != null)
            playerController.SetInputLocked(true);

        bool grantStrongFirstJump = !graphCalculatorMode && !skipSpawnSpotlight;
        yield return LoadWorldAfterThemeChange(def, grantStrongFirstJump);

        bool showIntro = !skipNextStageIntro
                         && !graphCalculatorMode
                         && currentLevelIndex >= 0
                         && currentLevelIndex < StageRoleplayLibrary.Count;

        skipNextStageIntro = false;

        bool runSpawnSpotlight = !graphCalculatorMode && !skipSpawnSpotlight;
        skipSpawnSpotlight = false;

        if (showIntro)
            yield return RunEnumerated(RunStageIntroCoroutine(def));

        if (runSpawnSpotlight && playerController != null)
            yield return RunEnumerated(RunSpawnSpotlightRoutine());

        // Match original behaviour: player can move while the top story banner fades.
        if (playerController != null)
        {
            MobileInputBridge.ClearTouchRouting();
            playerController.SetInputLocked(false);
        }

        EnsureGameplayTouchZonesAfterLevelReady();
        EnsurePlatformerHudPresentation();

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

    /// <summary>
    /// Fullscreen dim with a soft hole on the player — runs while input is still locked (after optional roleplay card).
    /// </summary>
    private IEnumerator RunSpawnSpotlightRoutine()
    {
        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null || playerController == null)
            yield break;

        EnsureSpawnSpotlightOverlay(canvas);
        if (spawnSpotlightRoot == null || spawnSpotlightMaterial == null || spawnSpotlightRect == null)
            yield break;

        var playerRt = playerController.PlayerVisualRect;
        if (playerRt == null)
            yield break;

        float total = Mathf.Max(0.35f, spawnSpotlightTotalSeconds);
        float fadeIn = Mathf.Min(0.22f, total * 0.12f);
        float fadeOut = Mathf.Min(0.22f, total * 0.12f);
        float hold = Mathf.Max(0.1f, total - fadeIn - fadeOut);
        const float maxAlpha = 0.84f;

        spawnSpotlightMaterial.SetFloat("_SpotRadius", 0.19f);
        spawnSpotlightMaterial.SetFloat("_SpotSoft", 0.11f);

        spawnSpotlightRoot.SetActive(true);
        spawnSpotlightRoot.transform.SetAsLastSibling();

        float t = 0f;
        while (t < fadeIn)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / fadeIn) * maxAlpha;
            spawnSpotlightMaterial.SetColor("_DimColor", new Color(0f, 0f, 0f, a));
            UpdateSpawnSpotlightHole(spawnSpotlightRect, playerRt, spawnSpotlightMaterial);
            yield return null;
        }

        spawnSpotlightMaterial.SetColor("_DimColor", new Color(0f, 0f, 0f, maxAlpha));

        t = 0f;
        while (t < hold)
        {
            t += Time.unscaledDeltaTime;
            UpdateSpawnSpotlightHole(spawnSpotlightRect, playerRt, spawnSpotlightMaterial);
            yield return null;
        }

        t = 0f;
        while (t < fadeOut)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(maxAlpha, 0f, Mathf.Clamp01(t / fadeOut));
            spawnSpotlightMaterial.SetColor("_DimColor", new Color(0f, 0f, 0f, a));
            UpdateSpawnSpotlightHole(spawnSpotlightRect, playerRt, spawnSpotlightMaterial);
            yield return null;
        }

        spawnSpotlightMaterial.SetColor("_DimColor", new Color(0f, 0f, 0f, 0f));
        spawnSpotlightRoot.SetActive(false);
    }

    private void EnsureSpawnSpotlightOverlay(Canvas canvas)
    {
        if (spawnSpotlightRoot != null)
            return;

        Shader sh = spawnSpotlightShader;
        if (sh == null)
            sh = Resources.Load<Shader>("UI_SpotlightDim");
        if (sh == null)
            sh = Shader.Find("UI/SpotlightDim");
        if (sh == null)
        {
            Debug.LogWarning("LevelManager: Shader UI/SpotlightDim not found — ensure Assets/Resources/UI_SpotlightDim.shader exists or assign spawnSpotlightShader.");
            return;
        }

        var safe = MobileUiRoots.GetSafeContentParent(canvas.transform);
        Transform parent = safe != null ? safe : canvas.transform;

        spawnSpotlightRoot = new GameObject("SpawnSpotlightOverlay", typeof(RectTransform));
        spawnSpotlightRect = spawnSpotlightRoot.GetComponent<RectTransform>();
        spawnSpotlightRect.SetParent(parent, false);
        spawnSpotlightRect.anchorMin = Vector2.zero;
        spawnSpotlightRect.anchorMax = Vector2.one;
        spawnSpotlightRect.offsetMin = Vector2.zero;
        spawnSpotlightRect.offsetMax = Vector2.zero;

        var img = spawnSpotlightRoot.AddComponent<Image>();
        img.raycastTarget = false;
        img.color = Color.white;
        img.sprite = TryGetSquareSprite();

        spawnSpotlightMaterial = new Material(sh);
        img.material = spawnSpotlightMaterial;
        spawnSpotlightMaterial.SetColor("_DimColor", new Color(0f, 0f, 0f, 0f));

        spawnSpotlightRoot.SetActive(false);
    }

    private static void UpdateSpawnSpotlightHole(RectTransform overlayRt, RectTransform playerRt, Material mat)
    {
        if (overlayRt == null || playerRt == null || mat == null)
            return;

        float aspect = overlayRt.rect.width / Mathf.Max(overlayRt.rect.height, 1e-4f);
        mat.SetFloat("_Aspect", aspect);

        // Same Canvas tree: world → overlay-local avoids Overlay vs Camera camera-null pitfalls.
        Vector3 world = playerRt.TransformPoint(playerRt.rect.center);
        Vector3 local = overlayRt.InverseTransformPoint(world);

        Rect r = overlayRt.rect;
        float u = Mathf.InverseLerp(r.xMin, r.xMax, local.x);
        float v = Mathf.InverseLerp(r.yMin, r.yMax, local.y);
        mat.SetVector("_SpotCenter", new Vector4(u, v, 0f, 0f));
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
        stageIntroHint.text = LocalizationManager.Get("ui.stage_intro_hint", "Tap anywhere to continue");
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
        bool stageIntroTablet = DeviceLayout.IsTabletLike();

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
        hintRt.offsetMin = new Vector2(16f, 12f);
        hintRt.offsetMax = new Vector2(-16f, 88f);
        stageIntroHint = hintGo.AddComponent<TextMeshProUGUI>();
        stageIntroHint.fontSize = UiTypography.Scale(stageIntroTablet ? 26 : 23);
        stageIntroHint.alignment = TextAlignmentOptions.Bottom;
        stageIntroHint.color = new Color(0.75f, 0.8f, 0.95f, 0.85f);
        stageIntroHint.fontStyle = FontStyles.Italic;
        stageIntroHint.richText = true;
        stageIntroHint.raycastTarget = false;
        ApplyPrimaryUiTypography(stageIntroHint, FindPrimaryEquationTmp(), outlineWidth: 0.06f, outlineAlpha: 0.3f);
        stageIntroHint.fontStyle = FontStyles.Italic;

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
    private IEnumerator LoadWorldAfterThemeChange(LevelDefinition def, bool grantStrongFirstGroundJump)
    {
        // Wait for the plot to regenerate points with the new parameters.
        yield return null;
        yield return new WaitForEndOfFrame();

        if (obstacleGenerator == null || playerController == null)
            yield break;

        FitCartesianPlaneForGameplay();

        var gridSize = gridRenderer.gridSize;

        var unitWidth = cartesianPlaneRect.rect.width / (float)gridSize.x;
        var unitHeight = cartesianPlaneRect.rect.height / (float)gridSize.y;
        obstacleGenerator.SetLayout(obstaclesRoot, gridSize, unitWidth, unitHeight);

        EnsureRiemannRenderer();
        if (riemannRenderer != null)
        {
            bool riemannBackdrop = def.riemannRectCount > 0
                && (def.showRiemannVisualization
                    || (def.useRiemannStairPlatforms && def.riemannRule != RiemannRule.None));
            if (riemannBackdrop)
                riemannRenderer.transform.SetSiblingIndex(0);
            riemannRenderer.Rebuild(def, functionPlotter);
        }

        var curvePoints = curveRenderer.points;
        var derivPoints = derivRenderer.points;

        var playBounds = GameplayPlayBounds.Compute(cartesianPlaneRect, gridSize);
        var world = obstacleGenerator.GenerateWorld(def, curvePoints, derivPoints, functionPlotter, playBounds);
        if (world.hasPlayBounds)
            playerController.SetDeathMinYGrid(world.playBounds.YMin - 0.4f);
        else
            playerController.SetDeathMinYGrid(deathMinYGrid);

        playerController.SetGridToPixelUnits(unitWidth, unitHeight);
        playerController.SetWorld(world);
        playerController.ResetToSpawn(world, grantStrongFirstGroundJump);

        _lastSyncedCartesianRectSize = cartesianPlaneRect.rect.size;
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
        skipSpawnSpotlight = true;
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

