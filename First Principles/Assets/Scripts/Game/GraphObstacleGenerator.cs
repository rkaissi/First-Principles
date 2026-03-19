using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// =============================================================================
// GraphObstacleGenerator — curve & derivative → platform / hazard columns
// =============================================================================
// For each integer column of the graph grid, samples f and f' (or Rieman stair rule)
// to decide SAFE (solid platform) vs hazard gap. Visuals are child UI Images under
// obstaclesRoot; logical rects live in GraphWorld for PlayerControllerUI2D.
// Coordinate space: same as LineRendererUI points (grid cells, origin at grid center).
// =============================================================================

/// <summary>Axis-aligned rectangle in graph grid units (column slices, finish band, etc.).</summary>
public struct GridRect
{
    public float xMin;
    public float xMax;
    public float yMin;
    public float yMax;

    public GridRect(float xMin, float xMax, float yMin, float yMax)
    {
        this.xMin = xMin;
        this.xMax = xMax;
        this.yMin = yMin;
        this.yMax = yMax;
    }

    public bool ContainsY(float y) => y >= yMin && y <= yMax;
}

public class GraphWorld
{
    public Vector2Int gridSize;
    public List<GridRect> platforms = new List<GridRect>();
    public List<GridRect> hazards = new List<GridRect>();
    public GridRect finish;

    public float spawnXGrid;
    public float spawnYTopGrid;
}

/// <summary>
/// Converts plotted curve/derivative points into a 2D "platformer world" (platforms + gaps).
/// Obstacles live in the same grid coordinate space as the graph (0..gridSize.x/y).
/// </summary>
public class GraphObstacleGenerator : MonoBehaviour
{
    [Header("Fair gaps")]
    [Tooltip("Derivative-gated hazard columns cannot exceed this many in a row — extra columns become walkable bridges so jumps stay feasible.")]
    [SerializeField] private int maxConsecutiveDerivativeHazards = 4;

    [Tooltip("When sampling the curve mesh, allow the nearest vertex this far in grid X (wider = fewer missing platforms).")]
    [SerializeField] private float curveSampleMaxGridDx = 2.35f;

    private RectTransform obstaclesRoot;
    private Vector2Int gridSize;
    private float unitWidth;
    private float unitHeight;
    private Sprite obstacleSprite;

    public void SetLayout(RectTransform obstaclesRoot, Vector2Int gridSize, float unitWidth, float unitHeight)
    {
        this.obstaclesRoot = obstaclesRoot;
        this.gridSize = gridSize;
        this.unitWidth = unitWidth;
        this.unitHeight = unitHeight;

        obstacleSprite = RuntimeUiPolish.Rounded9Slice != null ? RuntimeUiPolish.Rounded9Slice : TryGetSquareSprite();
    }

    /// <param name="functionPlotter">Required for Riemann stair mode; used to evaluate exact f at sample x.</param>
    public GraphWorld GenerateWorld(LevelDefinition def, List<Vector2> curvePoints, List<Vector2> derivPoints, FunctionPlotter functionPlotter = null)
    {
        if (obstaclesRoot == null)
        {
            Debug.LogError("GraphObstacleGenerator: obstaclesRoot is null. Call SetLayout first.");
            return new GraphWorld();
        }

        // Clear previous obstacles (but keep other scene children under the parent).
        for (int i = obstaclesRoot.childCount - 1; i >= 0; i--)
        {
            var child = obstaclesRoot.GetChild(i);
            if (child == null)
                continue;
            if (child.name.StartsWith("Platform", System.StringComparison.OrdinalIgnoreCase) ||
                child.name.StartsWith("Hazard", System.StringComparison.OrdinalIgnoreCase) ||
                child.name.StartsWith("FinishExitGradient", System.StringComparison.OrdinalIgnoreCase))
                Destroy(child.gameObject);
        }

        var world = new GraphWorld();
        world.gridSize = gridSize;

        float originY = gridSize.y / 2f;
        float width = gridSize.x;
        var gridOrigin = new Vector2Int(gridSize.x / 2, gridSize.y / 2);

        bool useRiemannStairs = def.useRiemannStairPlatforms
            && def.riemannRule != RiemannRule.None
            && def.riemannRectCount > 0
            && functionPlotter != null
            && (def.xEnd - def.xStart) > 1e-6f;

        // Finish zone at the far right.
        float finishWidth = 1f;
        world.finish = new GridRect(width - finishWidth, width, 0f, gridSize.y);

        int spawnCol = 0;
        float spawnYTop = float.PositiveInfinity;
        bool spawnChosen = false;

        int hazardRun = 0;
        int maxHazardRun = Mathf.Max(1, maxConsecutiveDerivativeHazards);

        // Generate columns.
        for (int col = 0; col < gridSize.x; col++)
        {
            float xSample = col + 0.5f;
            float xPlotCol = xSample - gridOrigin.x;
            float xPlotForF = xPlotCol;
            float xDerivSample = xSample;

            if (useRiemannStairs)
            {
                int n = Mathf.Max(1, def.riemannRectCount);
                float dx = (def.xEnd - def.xStart) / n;
                float t = (xPlotCol - def.xStart) / dx;
                int idx = Mathf.Clamp(Mathf.FloorToInt(t), 0, n - 1);
                float xL = def.xStart + idx * dx;
                float xR = def.xStart + (idx + 1) * dx;
                xPlotForF = def.riemannRule switch
                {
                    RiemannRule.Left => xL,
                    RiemannRule.Right => xR,
                    RiemannRule.Midpoint => 0.5f * (xL + xR),
                    _ => xPlotCol
                };
                xDerivSample = xPlotForF + gridOrigin.x;
            }

            bool hasCurve;
            float yCurve;
            if (useRiemannStairs)
            {
                float yPlot = functionPlotter.SampleCurvePlotterY(xPlotForF);
                hasCurve = IsFiniteFloat(yPlot);
                yCurve = yPlot + gridOrigin.y;
            }
            else
            {
                hasCurve = TrySampleNearestY(curvePoints, xSample, out yCurve);
            }

            bool hasDeriv = TrySampleNearestY(derivPoints, xDerivSample, out float yDeriv);

            float dyValue = hasDeriv ? (yDeriv - originY) : float.NegativeInfinity;
            bool safeByDerivative = hasDeriv && dyValue > def.derivativeSafeThreshold;

            bool forcedSafeStart = col < def.forcePlatformsAtStartColumns;
            bool forcedSafeEnd = col >= gridSize.x - def.forcePlatformsAtEndColumns;
            bool safe = safeByDerivative || forcedSafeStart || forcedSafeEnd;

            // Long runs of derivative hazards create horizontal chasms the jump arc cannot clear — bridge with a platform.
            if (!safe && hazardRun >= maxHazardRun)
                safe = true;

            // Skip columns that are completely out of range to avoid off-screen platforms.
            if (!safe)
            {
                hazardRun++;
                var hazard = new GridRect(col, col + 1f, 0f, def.hazardHeightGrid);
                world.hazards.Add(hazard);
                // Make hazards follow the derivative theme color for stronger readability.
                CreateRectVisual($"Hazard_{col}", hazard, def.derivativeColor);
                continue;
            }

            hazardRun = 0;

            // Safe platform height based on the curve.
            float platformTop = hasCurve ? yCurve : originY;
            platformTop = Mathf.Clamp(platformTop, def.platformThicknessGrid, gridSize.y - 0.01f);
            float platformBottom = Mathf.Clamp(platformTop - def.platformThicknessGrid, 0f, platformTop);

            var platform = new GridRect(col, col + 1f, platformBottom, platformTop);
            world.platforms.Add(platform);
            CreateRectVisual($"Platform_{col}", platform, def.curveColor);

            // Pick the lowest platform among the starting columns so the player doesn't spawn at the top of the graph.
            if (forcedSafeStart && safe && (!spawnChosen || platformTop < spawnYTop))
            {
                spawnCol = col;
                spawnYTop = platformTop;
                spawnChosen = true;
            }
        }

        if (!spawnChosen && world.platforms.Count > 0)
        {
            // Fallback: spawn on the first platform.
            world.spawnXGrid = world.platforms[0].xMin + 0.5f;
            spawnYTop = world.platforms[0].yMax;
        }
        else
        {
            world.spawnXGrid = spawnCol + 0.5f;
        }

        if (float.IsPositiveInfinity(spawnYTop) && world.platforms.Count > 0)
            spawnYTop = world.platforms[0].yMax;
        world.spawnYTopGrid = spawnYTop;

        CreateFinishExitGradient(world.finish);
        return world;
    }

    /// <summary>
    /// Sky-blue horizontal gradient on the far right so the exit / finish band reads clearly.
    /// Drawn above platforms (last sibling); player is parented to the plane, not this root.
    /// </summary>
    private void CreateFinishExitGradient(GridRect finishBand)
    {
        if (obstaclesRoot == null)
            return;

        const float fadeWidthGrid = 2.85f;
        float xMax = finishBand.xMax;
        float xMin = Mathf.Max(0f, xMax - fadeWidthGrid);
        var rect = new GridRect(xMin, xMax, 0f, gridSize.y);

        float pxX = rect.xMin * unitWidth;
        float pxY = rect.yMin * unitHeight;
        float pxW = (rect.xMax - rect.xMin) * unitWidth;
        float pxH = (rect.yMax - rect.yMin) * unitHeight;

        var go = new GameObject("FinishExitGradient");
        go.transform.SetParent(obstaclesRoot, false);
        go.transform.SetAsLastSibling();

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = Vector2.zero;
        rt.anchoredPosition = new Vector2(pxX, pxY);
        rt.sizeDelta = new Vector2(pxW, pxH);

        var grad = go.AddComponent<UiHorizontalGradientGraphic>();
        grad.raycastTarget = false;
        grad.SetGradientColors(
            new Color(0.58f, 0.88f, 1f, 0f),
            new Color(0.32f, 0.74f, 0.98f, 0.52f));
    }

    private static bool IsFiniteFloat(float v) => !float.IsNaN(v) && !float.IsInfinity(v);

    private void CreateRectVisual(string name, GridRect rect, Color color)
    {
        if (obstaclesRoot == null)
            return;

        // Convert grid rect to anchored pixel rect.
        float pxX = rect.xMin * unitWidth;
        float pxY = rect.yMin * unitHeight;
        float pxW = (rect.xMax - rect.xMin) * unitWidth;
        float pxH = (rect.yMax - rect.yMin) * unitHeight;

        var go = new GameObject(name);
        go.transform.SetParent(obstaclesRoot, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = Vector2.zero;
        rt.anchoredPosition = new Vector2(pxX, pxY);
        rt.sizeDelta = new Vector2(pxW, pxH);

        var img = go.AddComponent<Image>();
        img.sprite = obstacleSprite;
        bool isHazard = name.StartsWith("Hazard", System.StringComparison.OrdinalIgnoreCase);
        var c = isHazard ? color : Color.Lerp(color, Color.white, 0.12f);
        img.color = new Color(c.r, c.g, c.b, isHazard ? 0.88f : 0.93f);
        if (obstacleSprite != null && obstacleSprite.border.sqrMagnitude > 0.001f &&
            RuntimeUiPolish.ShouldUseSlicedForSize(pxW, pxH))
            img.type = Image.Type.Sliced;
        else
            img.type = Image.Type.Simple;
        img.raycastTarget = false;
    }

    private bool TrySampleNearestY(List<Vector2> points, float xSample, out float y)
    {
        y = 0f;
        if (points == null || points.Count == 0)
            return false;

        float bestDist = float.PositiveInfinity;
        float bestY = 0f;

        for (int i = 0; i < points.Count; i++)
        {
            float d = Mathf.Abs(points[i].x - xSample);
            if (d < bestDist)
            {
                bestDist = d;
                bestY = points[i].y;
            }
        }

        // Reject if the nearest sample is too far away (avoids nonsense at extreme edges).
        if (bestDist > curveSampleMaxGridDx)
            return false;

        if (float.IsNaN(bestY) || float.IsInfinity(bestY))
            return false;

        y = bestY;
        return true;
    }

    private Sprite TryGetSquareSprite()
    {
        var outsideSquare1 = GameObject.Find("OutsideSquare1");
        if (outsideSquare1 != null)
        {
            var img = outsideSquare1.GetComponent<Image>();
            if (img != null && img.sprite != null)
                return img.sprite;
        }

        // Fallback: any sprite-backed Image.
        var allImages = FindObjectsByType<Image>();
        foreach (var img in allImages)
        {
            if (img != null && img.sprite != null)
                return img.sprite;
        }

        return null;
    }
}

