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

    /// <summary>When true, keeps the avatar inside <see cref="playBounds"/> horizontally and uses them for fall death.</summary>
    public bool hasPlayBounds;
    public GameplayPlayBounds playBounds;
}

/// <summary>
/// Converts plotted curve/derivative points into a 2D "platformer world" (platforms + gaps).
/// Obstacles live in the same grid coordinate space as the graph (0..gridSize.x/y).
/// </summary>
public class GraphObstacleGenerator : MonoBehaviour
{
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
    /// <param name="playBoundsOptional">Inset AABB in grid units; null = full grid, no player hard clamp.</param>
    public GraphWorld GenerateWorld(
        LevelDefinition def,
        List<Vector2> curvePoints,
        List<Vector2> derivPoints,
        FunctionPlotter functionPlotter = null,
        GameplayPlayBounds? playBoundsOptional = null)
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

        var bounds = playBoundsOptional ?? GameplayPlayBounds.FullGrid(gridSize);
        world.hasPlayBounds = playBoundsOptional.HasValue;
        world.playBounds = bounds;

        float originY = gridSize.y / 2f;
        float width = gridSize.x;
        var gridOrigin = new Vector2Int(gridSize.x / 2, gridSize.y / 2);

        bool useRiemannStairs = def.useRiemannStairPlatforms
            && def.riemannRule != RiemannRule.None
            && def.riemannRectCount > 0
            && functionPlotter != null
            && (def.xEnd - def.xStart) > 1e-6f;

        // Finish zone at the far right inside the padded play area.
        float finishWidth = 1f;
        world.finish = new GridRect(Mathf.Max(bounds.XMax - finishWidth, bounds.XMin), bounds.XMax, bounds.YMin, bounds.YMax);

        int spawnCol = 0;
        float spawnYTop = float.PositiveInfinity;
        bool spawnChosen = false;

        if (useRiemannStairs)
        {
            BuildRiemannSpacedStairWorld(
                def,
                derivPoints,
                functionPlotter,
                world,
                gridOrigin,
                originY,
                bounds,
                ref spawnCol,
                ref spawnYTop,
                ref spawnChosen);
        }
        else
        {
            // Classic column scan: derivative / forced edges decide safe vs hazard; curve sets platform height.
            for (int col = 0; col < gridSize.x; col++)
            {
                float xSample = col + 0.5f;
                float xPlotter = Mathf.Clamp(xSample - gridOrigin.x, def.xStart, def.xEnd);

                bool hasCurve = functionPlotter != null && IsFiniteFloat(functionPlotter.SampleCurveGridY(xPlotter));
                float yCurve = hasCurve ? functionPlotter.SampleCurveGridY(xPlotter) : float.NaN;

                float dyRaw = functionPlotter != null ? functionPlotter.EvaluateNumericalDerivativeY(xPlotter) : float.NaN;
                bool hasDeriv = IsFiniteFloat(dyRaw);
                bool safeByDerivative = hasDeriv && dyRaw > def.derivativeSafeThreshold;

                bool forcedSafeStart = col < def.forcePlatformsAtStartColumns;
                bool forcedSafeEnd = col >= gridSize.x - def.forcePlatformsAtEndColumns;
                bool safe = safeByDerivative || forcedSafeStart || forcedSafeEnd;

                if (!safe)
                {
                    var hazard = new GridRect(col, col + 1f, 0f, def.hazardHeightGrid);
                    AddHazardClamped(world, def, $"Hazard_{col}", hazard, bounds);
                    continue;
                }

                float platformTop = hasCurve ? yCurve : originY;
                platformTop = Mathf.Clamp(platformTop, def.platformThicknessGrid, gridSize.y - 0.01f);
                float platformBottom = Mathf.Clamp(platformTop - def.platformThicknessGrid, 0f, platformTop);

                var platform = new GridRect(col, col + 1f, platformBottom, platformTop);
                AddPlatformClamped(world, def, $"Platform_{col}", platform, bounds);

                if (forcedSafeStart && safe && (!spawnChosen || platformTop < spawnYTop))
                {
                    spawnCol = col;
                    spawnYTop = platformTop;
                    spawnChosen = true;
                }
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

        if (world.hasPlayBounds)
        {
            float hx = 0.5f;
            world.spawnXGrid = Mathf.Clamp(world.spawnXGrid, bounds.XMin + hx, bounds.XMax - hx);
            world.spawnYTopGrid = Mathf.Clamp(world.spawnYTopGrid, bounds.YMin + def.platformThicknessGrid * 0.5f, bounds.YMax);
        }

        CreateFinishExitGradient(world.finish);
        return world;
    }

    private static bool TryClampRectToBounds(GridRect r, GameplayPlayBounds b, out GridRect clipped)
    {
        float x0 = Mathf.Max(r.xMin, b.XMin);
        float x1 = Mathf.Min(r.xMax, b.XMax);
        float y0 = Mathf.Max(r.yMin, b.YMin);
        float y1 = Mathf.Min(r.yMax, b.YMax);
        if (x1 - x0 < 0.03f || y1 - y0 < 0.03f)
        {
            clipped = default;
            return false;
        }

        clipped = new GridRect(x0, x1, y0, y1);
        return true;
    }

    private void AddPlatformClamped(GraphWorld world, LevelDefinition def, string name, GridRect rect, GameplayPlayBounds b)
    {
        if (!TryClampRectToBounds(rect, b, out var c))
            return;
        world.platforms.Add(c);
        CreateRectVisual(name, c, def.curveColor);
    }

    private void AddHazardClamped(GraphWorld world, LevelDefinition def, string name, GridRect rect, GameplayPlayBounds b)
    {
        if (!TryClampRectToBounds(rect, b, out var c))
            return;
        world.hazards.Add(c);
        CreateRectVisual(name, c, def.derivativeColor);
    }

    /// <summary>
    /// Riemann stair levels: one tread per subinterval, narrowed so gaps stay air; full rectangles
    /// stay visible via <see cref="RiemannStripRendererUI"/> backdrop.
    /// </summary>
    private void BuildRiemannSpacedStairWorld(
        LevelDefinition def,
        List<Vector2> derivPoints,
        FunctionPlotter functionPlotter,
        GraphWorld world,
        Vector2Int gridOrigin,
        float originY,
        GameplayPlayBounds bounds,
        ref int spawnCol,
        ref float spawnYTop,
        ref bool spawnChosen)
    {
        int n = Mathf.Max(1, def.riemannRectCount);
        float dxPlot = (def.xEnd - def.xStart) / n;
        float cov = Mathf.Clamp(def.riemannPlatformCoverage, 0.22f, 1f);

        for (int i = 0; i < n; i++)
        {
            float xL = def.xStart + i * dxPlot;
            float xR = def.xStart + (i + 1) * dxPlot;
            float xS = SampleRiemannSampleX(def.riemannRule, xL, xR);
            float yGrid = functionPlotter.SampleCurveGridY(xS);
            if (!IsFiniteFloat(yGrid))
                continue;

            float colL = xL + gridOrigin.x;
            float colR = xR + gridOrigin.x;
            float center = 0.5f * (colL + colR);
            float halfSpan = 0.5f * (colR - colL) * cov;
            float pMin = Mathf.Clamp(center - halfSpan, 0f, gridSize.x);
            float pMax = Mathf.Clamp(center + halfSpan, 0f, gridSize.x);
            if (pMax - pMin < 0.18f)
                continue;

            float platformTop = yGrid;
            platformTop = Mathf.Clamp(platformTop, def.platformThicknessGrid, gridSize.y - 0.01f);
            float platformBottom = Mathf.Clamp(platformTop - def.platformThicknessGrid, 0f, platformTop);

            var plat = new GridRect(pMin, pMax, platformBottom, platformTop);
            AddPlatformClamped(world, def, $"Platform_Riemann_{i}", plat, bounds);
        }

        float starterRefTop = float.NaN;
        foreach (var p in world.platforms)
        {
            if (p.xMax <= 0f)
                continue;
            if (p.xMin >= def.forcePlatformsAtStartColumns + 4)
                continue;
            if (float.IsNaN(starterRefTop) || p.yMax < starterRefTop)
                starterRefTop = p.yMax;
        }

        if (float.IsNaN(starterRefTop))
            starterRefTop = Mathf.Clamp(gridSize.y * 0.24f, def.platformThicknessGrid * 2.5f, gridSize.y * 0.42f);

        for (int col = 0; col < def.forcePlatformsAtStartColumns; col++)
        {
            if (ColumnOverlapsAnyPlatform(col, world.platforms))
                continue;

            float platformTop = Mathf.Clamp(starterRefTop, def.platformThicknessGrid, gridSize.y - 0.01f);
            float platformBottom = Mathf.Clamp(platformTop - def.platformThicknessGrid, 0f, platformTop);
            var pad = new GridRect(col, col + 1f, platformBottom, platformTop);
            AddPlatformClamped(world, def, $"Platform_StartPad_{col}", pad, bounds);
        }

        float endRefTop = starterRefTop;
        foreach (var p in world.platforms)
        {
            if (p.xMin < gridSize.x - def.forcePlatformsAtEndColumns - 4)
                continue;
            endRefTop = Mathf.Max(endRefTop, p.yMax);
        }

        for (int col = gridSize.x - def.forcePlatformsAtEndColumns; col < gridSize.x; col++)
        {
            if (col < 0)
                continue;
            if (ColumnOverlapsAnyPlatform(col, world.platforms))
                continue;

            float platformTop = Mathf.Clamp(endRefTop, def.platformThicknessGrid, gridSize.y - 0.01f);
            float platformBottom = Mathf.Clamp(platformTop - def.platformThicknessGrid, 0f, platformTop);
            var pad = new GridRect(col, col + 1f, platformBottom, platformTop);
            AddPlatformClamped(world, def, $"Platform_EndPad_{col}", pad, bounds);
        }

        for (int col = 0; col < gridSize.x; col++)
        {
            if (col + 1f <= bounds.XMin || col >= bounds.XMax)
                continue;

            if (ColumnOverlapsAnyPlatform(col, world.platforms))
                continue;

            float xSample = col + 0.5f;
            float xPlotter = Mathf.Clamp(xSample - gridOrigin.x, def.xStart, def.xEnd);
            float dyRaw = functionPlotter != null ? functionPlotter.EvaluateNumericalDerivativeY(xPlotter) : float.NaN;
            bool hasDeriv = IsFiniteFloat(dyRaw);
            bool safeByDerivative = hasDeriv && dyRaw > def.derivativeSafeThreshold;
            if (!safeByDerivative)
            {
                var hazard = new GridRect(col, col + 1f, 0f, def.hazardHeightGrid);
                AddHazardClamped(world, def, $"Hazard_{col}", hazard, bounds);
            }
        }

        spawnChosen = false;
        spawnYTop = float.PositiveInfinity;
        for (int col = 0; col < def.forcePlatformsAtStartColumns; col++)
        {
            foreach (var p in world.platforms)
            {
                if (!IntervalsOverlap(col, col + 1f, p.xMin, p.xMax))
                    continue;
                if (!spawnChosen || p.yMax < spawnYTop)
                {
                    spawnChosen = true;
                    spawnYTop = p.yMax;
                    spawnCol = col;
                }
            }
        }
    }

    private static float SampleRiemannSampleX(RiemannRule rule, float xL, float xR)
    {
        return rule switch
        {
            RiemannRule.Left => xL,
            RiemannRule.Right => xR,
            RiemannRule.Midpoint => 0.5f * (xL + xR),
            _ => 0.5f * (xL + xR)
        };
    }

    private static bool IntervalsOverlap(float a0, float a1, float b0, float b1)
    {
        return a1 > b0 && a0 < b1;
    }

    private static bool ColumnOverlapsAnyPlatform(int col, List<GridRect> platforms)
    {
        if (platforms == null || platforms.Count == 0)
            return false;
        float c0 = col;
        float c1 = col + 1f;
        for (int i = 0; i < platforms.Count; i++)
        {
            var p = platforms[i];
            if (c1 > p.xMin && c0 < p.xMax)
                return true;
        }

        return false;
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
        var rect = new GridRect(xMin, xMax, finishBand.yMin, finishBand.yMax);

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
        if (bestDist > 1.25f)
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

