using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
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

    private readonly List<LevelDefinition> levels = new List<LevelDefinition>();
    private int currentLevelIndex;

    private int nextStageIndex;
    private List<float> stageTriggerXGrid;
    private List<Color> stagePopColors;

    private bool isRestarting;
    private Coroutine storyFadeRoutine;

    private void Awake()
    {
        // Keep things single-scene: if another instance somehow appears, destroy it.
        if (FindObjectsByType<LevelManager>().Length > 1)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        SetupReferences();
        BuildSampleLevels();
        int startIndex = LevelSelection.ConsumeSelectedLevel(levels.Count);
        LoadLevel(startIndex);
    }

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
        HideLegacyGraphTuningButtons();

        // Wire callbacks.
        playerController.SetDeathCallback(RestartCurrentLevel);
        playerController.SetFinishCallback(AdvanceLevel);
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
        img.color = new Color(1f, 1f, 1f, 1f);

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
        storyGo.transform.SetParent(canvas.transform, false);

        var tmp = storyGo.AddComponent<TextMeshProUGUI>();
        tmp.text = "";
        tmp.fontSize = 32;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.richText = true;
        ApplyPrimaryUiTypography(tmp, FindPrimaryEquationTmp(), outlineWidth: 0.06f, outlineAlpha: 0.35f);

        var rt = tmp.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0, -90f);
        rt.sizeDelta = new Vector2(900f, 120f);

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
            panelRt.SetParent(canvas.transform, false);
            panelRt.anchorMin = new Vector2(0f, 1f);
            panelRt.anchorMax = new Vector2(0f, 1f);
            panelRt.pivot = new Vector2(0f, 1f);
            panelRt.anchoredPosition = new Vector2(22f, -20f);
            panelRt.sizeDelta = new Vector2(340f, 76f);

            var panelBg = panelGo.AddComponent<Image>();
            panelBg.sprite = panelSprite;
            panelBg.color = new Color(0.06f, 0.07f, 0.11f, 0.88f);
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
            accentImg.color = new Color(0.95f, 0.72f, 0.25f, 0.95f);
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
            tmp.enableWordWrapping = false;
            tmp.fontSize = 30;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = new Color(0.94f, 0.95f, 0.98f, 1f);
            tmp.characterSpacing = 0.35f;
            tmp.lineSpacing = -4f;
            ApplyPrimaryUiTypography(tmp, equationStyle, outlineWidth: 0.16f, outlineAlpha: 0.55f);
            tmp.text = FormatStageHudLine(1, 1);

            stageHudText = tmp;
        }

        if (controlsHintText == null)
        {
            var barGo = new GameObject("ControlsHintPanel");
            var barRt = barGo.AddComponent<RectTransform>();
            barRt.SetParent(canvas.transform, false);
            barRt.anchorMin = new Vector2(0.5f, 0f);
            barRt.anchorMax = new Vector2(0.5f, 0f);
            barRt.pivot = new Vector2(0.5f, 0f);
            barRt.anchoredPosition = new Vector2(0f, 18f);
            barRt.sizeDelta = new Vector2(620f, 54f);

            var barBg = barGo.AddComponent<Image>();
            barBg.sprite = panelSprite;
            barBg.color = new Color(0.06f, 0.07f, 0.11f, 0.82f);
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
            tmp.enableWordWrapping = false;
            tmp.fontSize = 22;
            tmp.alignment = TextAlignmentOptions.Midline;
            tmp.color = new Color(0.82f, 0.85f, 0.92f, 0.92f);
            tmp.characterSpacing = 0.25f;
            ApplyPrimaryUiTypography(tmp, equationStyle, outlineWidth: 0.14f, outlineAlpha: 0.5f);
            tmp.text =
                "<color=#7a8399>Move</color>  " +
                "<b><color=#ffd978>\u2190</color></b>  <b><color=#ffd978>\u2192</color></b>  " +
                "<color=#5c6577>·</color>  " +
                "<color=#7a8399>Jump</color>  " +
                "<b><color=#ffd978>Space</color></b>";

            controlsHintText = tmp;
        }
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
        return
            "<color=#9aa3b8><size=78%>STAGE</size></color>\n" +
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
    }

    /// <summary>Square / UI sprite for flat panels (falls back to a tiny white sprite so Image always draws).</summary>
    private Sprite GetHudPanelSprite()
    {
        if (cachedHudPanelSprite != null)
            return cachedHudPanelSprite;

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

    private void BuildSampleLevels()
    {
        levels.Clear();

        // Stage parameters are tuned to fit within the current UI grid range.
        levels.Add(MakeLevel(
            GameLevelCatalog.DisplayNames[0],
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
            GameLevelCatalog.DisplayNames[1],
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
            GameLevelCatalog.DisplayNames[2],
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
            GameLevelCatalog.DisplayNames[3],
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
    }

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
        string story)
    {
        var def = ScriptableObject.CreateInstance<LevelDefinition>();
        def.levelName = name;
        def.functionType = functionType;
        def.xStart = functionPlotter != null ? functionPlotter.xStart : -20f;
        def.xEnd = functionPlotter != null ? functionPlotter.xEnd : 20f;
        def.step = functionPlotter != null ? functionPlotter.step : 0.1f;

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

        def.stageDerivativePopColors = new List<Color>(defaultStageCount);
        for (int i = 0; i < defaultStageCount; i++)
        {
            float t = defaultStageCount == 1 ? 0f : (float)i / (defaultStageCount - 1);
            def.stageDerivativePopColors.Add(Color.Lerp(derivativeColor, Color.white, 0.35f * t));
        }

        def.storyText = story;
        return def;
    }

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
        StartCoroutine(LoadWorldAfterThemeChange(def));
    }

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

        curveRenderer.color = def.curveColor;
        derivRenderer.color = def.derivativeColor;

        // Stage pop setup.
        stagePopColors = def.stageDerivativePopColors ?? new List<Color>();
        if (stagePopColors.Count == 0)
        {
            stagePopColors = new List<Color>(defaultStageCount);
            for (int i = 0; i < defaultStageCount; i++)
                stagePopColors.Add(def.derivativeColor);
        }

        stageTriggerXGrid = new List<float>(defaultStageCount);
        float width = gridRenderer.gridSize.x;
        for (int i = 1; i <= defaultStageCount; i++)
        {
            stageTriggerXGrid.Add((i / (float)(defaultStageCount + 1)) * width);
        }

        // Story.
        if (storyText != null)
        {
            storyText.text = $"<b>{def.levelName}</b>\n{def.storyText}";
            if (storyFadeRoutine != null)
                StopCoroutine(storyFadeRoutine);
            storyFadeRoutine = StartCoroutine(FadeStoryTextRoutine());
        }
    }

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

        var curvePoints = curveRenderer.points;
        var derivPoints = derivRenderer.points;

        var world = obstacleGenerator.GenerateWorld(def, curvePoints, derivPoints);
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

        yield return new WaitForSeconds(1.5f);

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

    private void Update()
    {
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

