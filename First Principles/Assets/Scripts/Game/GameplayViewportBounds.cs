using UnityEngine;

/// <summary>
/// Playable AABB in graph grid units — keeps platforms, hazards, spawn, finish, and the player
/// inside screen/safe-area padding (including space for the mobile touch bar).
/// </summary>
public readonly struct GameplayPlayBounds
{
    public float XMin { get; }
    public float XMax { get; }
    public float YMin { get; }
    public float YMax { get; }

    public GameplayPlayBounds(float xMin, float xMax, float yMin, float yMax)
    {
        XMin = xMin;
        XMax = xMax;
        YMin = yMin;
        YMax = yMax;
    }

    /// <summary>Full graph grid (no inset).</summary>
    public static GameplayPlayBounds FullGrid(Vector2Int gridSize)
    {
        return new GameplayPlayBounds(0f, gridSize.x, 0f, gridSize.y);
    }

    /// <summary>
    /// Converts padded screen/local rect margins into grid units using the Cartesian plane size.
    /// </summary>
    public static GameplayPlayBounds Compute(RectTransform cartesianPlane, Vector2Int gridSize)
    {
        if (cartesianPlane == null || gridSize.x < 1 || gridSize.y < 1)
            return FullGrid(gridSize);

        float w = Mathf.Max(1f, cartesianPlane.rect.width);
        float h = Mathf.Max(1f, cartesianPlane.rect.height);
        float unitX = w / gridSize.x;
        float unitY = h / gridSize.y;

        float hPad = DeviceLayout.IsTabletLike()
            ? Mathf.Max(16f, w * 0.022f)
            : Mathf.Max(10f, w * 0.018f);

        float topPad = Mathf.Max(44f, h * 0.065f);

        float bottomPad = Mathf.Max(12f, h * 0.022f);
        if (DeviceLayout.PreferOnScreenGameControls)
            bottomPad = Mathf.Max(bottomPad, DeviceLayout.TouchControlBarHeight + 36f);

        float xMin = hPad / unitX;
        float xMax = gridSize.x - hPad / unitX;
        float yMin = bottomPad / unitY;
        float yMax = gridSize.y - topPad / unitY;

        const float minSpan = 8f;
        if (xMax - xMin < minSpan)
        {
            float cx = (xMin + xMax) * 0.5f;
            xMin = Mathf.Max(0f, cx - minSpan * 0.5f);
            xMax = Mathf.Min(gridSize.x, cx + minSpan * 0.5f);
        }

        if (yMax - yMin < minSpan)
        {
            float cy = (yMin + yMax) * 0.5f;
            yMin = Mathf.Max(0f, cy - minSpan * 0.5f);
            yMax = Mathf.Min(gridSize.y, cy + minSpan * 0.5f);
        }

        xMin = Mathf.Clamp(xMin, 0f, gridSize.x);
        xMax = Mathf.Clamp(xMax, 0f, gridSize.x);
        yMin = Mathf.Clamp(yMin, 0f, gridSize.y);
        yMax = Mathf.Clamp(yMax, 0f, gridSize.y);
        if (xMax < xMin)
            (xMin, xMax) = (xMax, xMin);
        if (yMax < yMin)
            (yMin, yMax) = (yMax, yMin);

        return new GameplayPlayBounds(xMin, xMax, yMin, yMax);
    }
}
