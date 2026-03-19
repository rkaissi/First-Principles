using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

        obstacleSprite = TryGetSquareSprite();
    }

    public GraphWorld GenerateWorld(LevelDefinition def, List<Vector2> curvePoints, List<Vector2> derivPoints)
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
            if (child != null && child.name.StartsWith("Platform", System.StringComparison.OrdinalIgnoreCase))
                Destroy(child.gameObject);
            else if (child != null && child.name.StartsWith("Hazard", System.StringComparison.OrdinalIgnoreCase))
                Destroy(child.gameObject);
            else
            {
                // For safety, do not remove arbitrary children.
            }
        }

        var world = new GraphWorld();
        world.gridSize = gridSize;

        float originY = gridSize.y / 2f;
        float width = gridSize.x;

        // Finish zone at the far right.
        float finishWidth = 1f;
        world.finish = new GridRect(width - finishWidth, width, 0f, gridSize.y);

        int spawnCol = Mathf.Clamp(def.forcePlatformsAtStartColumns, 1, gridSize.x) - 1;
        float spawnYTop = 0f;
        bool spawnChosen = false;

        // Generate columns.
        for (int col = 0; col < gridSize.x; col++)
        {
            float xSample = col + 0.5f;

            bool hasCurve = TrySampleNearestY(curvePoints, xSample, out float yCurve);
            bool hasDeriv = TrySampleNearestY(derivPoints, xSample, out float yDeriv);

            float dyValue = hasDeriv ? (yDeriv - originY) : float.NegativeInfinity;
            bool safeByDerivative = hasDeriv && dyValue > def.derivativeSafeThreshold;

            bool forcedSafeStart = col < def.forcePlatformsAtStartColumns;
            bool forcedSafeEnd = col >= gridSize.x - def.forcePlatformsAtEndColumns;
            bool safe = safeByDerivative || forcedSafeStart || forcedSafeEnd;

            // Skip columns that are completely out of range to avoid off-screen platforms.
            if (!safe)
            {
                var hazard = new GridRect(col, col + 1f, 0f, def.hazardHeightGrid);
                world.hazards.Add(hazard);
                // Make hazards follow the derivative theme color for stronger readability.
                CreateRectVisual($"Hazard_{col}", hazard, def.derivativeColor);
                continue;
            }

            // Safe platform height based on the curve.
            float platformTop = hasCurve ? yCurve : originY;
            platformTop = Mathf.Clamp(platformTop, def.platformThicknessGrid, gridSize.y - 0.01f);
            float platformBottom = Mathf.Clamp(platformTop - def.platformThicknessGrid, 0f, platformTop);

            var platform = new GridRect(col, col + 1f, platformBottom, platformTop);
            world.platforms.Add(platform);
            CreateRectVisual($"Platform_{col}", platform, def.curveColor);

            if (!spawnChosen && forcedSafeStart)
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

        world.spawnYTopGrid = spawnYTop;
        return world;
    }

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
        img.color = new Color(color.r, color.g, color.b, 0.9f);
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

