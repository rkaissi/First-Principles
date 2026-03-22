using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Same rendering approach as <see cref="LineRendererUI"/> but used for numeric derivative samples.
/// Tint/thickness animated by <see cref="DerivativePopAnimator"/> during stage transitions.
/// </summary>
public class DerivRendererUI : Graphic
{
    /// <summary>Scene / level default stroke width (reset on theme load and after derivative pop animation).</summary>
    public const float DefaultThicknessPixels = 10f;

    public Vector2Int gridSize;

    public List<Vector2> points = new List<Vector2>();

    public float thickness = DefaultThicknessPixels;

    [Tooltip("When < 1, the polyline fades in left→right (set by FunctionPlotter).")]
    [Range(0f, 1f)]
    public float graphRevealProgress = 1f;

    public float graphRevealXMin;
    public float graphRevealXMax;

    public bool enableHorizontalGraphReveal = true;

    [Tooltip("0 = line uses normal color; 1 = strong highlight when player grazes f′ (driven by PlayerControllerUI2D).")]
    [Range(0f, 1f)]
    public float playerProximityHighlight;

    private float width;
    private float height;
    private float unitWidth;
    private float unitHeight;

    private GridRendererUI grid;

#if UNITY_EDITOR
    protected override void Reset()
    {
        base.Reset();

        if (grid == null)
            grid = GetComponentInParent<GridRendererUI>();

        UpdateGridSize();
    }
#endif

    protected override void Awake()
    {
        base.Awake();

        if (grid == null)
            grid = GetComponentInParent<GridRendererUI>();

        UpdateGridSize();
    }

    public void SetGraphRevealFade(float progress01, float xMinGrid, float xMaxGrid)
    {
        graphRevealProgress = Mathf.Clamp01(progress01);
        graphRevealXMin = xMinGrid;
        graphRevealXMax = xMaxGrid;
        SetVerticesDirty();
    }

    private void Update()
    {
        UpdateGridSize();
    }

    static float HorizontalRevealAlpha(float gridX, float revealX, float feather)
    {
        float t = Mathf.Clamp01((gridX - (revealX - feather)) / Mathf.Max(feather, 1e-4f));
        return 1f - Mathf.SmoothStep(0f, 1f, t);
    }

    // When a UI generates a mesh...
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        width = rectTransform.rect.width;
        height = rectTransform.rect.height;

        unitWidth = width / (float)gridSize.x;
        unitHeight = height / (float)gridSize.y;

        if (points.Count < 2)
            return;

        float angle = 0;

        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector2 point = points[i];
            Vector2 point2 = points[i + 1];

            if (i < points.Count - 1)
                angle = GetAngle(point, point2) + 90f;

            DrawVerticesForPoint(point, point2, vh, angle);
        }

        for (int i = 0; i < points.Count - 1; i++)
        {
            int index = i * 4;
            vh.AddTriangle(index + 0, index + 1, index + 2);
            vh.AddTriangle(index + 1, index + 2, index + 3);
        }
    }

    // Converts the dimensions of the graph into an aspect ratio
    public Vector2 GetAspectRatio(float width, float height)
    {
        Vector2 aspectRatio = new Vector2(width, height);

        while (!floatIsInt(aspectRatio.x) && !floatIsInt(aspectRatio.y))
        {
            aspectRatio *= 10;
        }

        aspectRatio /= GetGreatestCommonDivisor((int)aspectRatio.x, (int)aspectRatio.y);

        return aspectRatio;
    }

    // Checks if a float is an integer
    public bool floatIsInt(float f)
    {
        return Mathf.Approximately(f, Mathf.RoundToInt(f));
    }

    // Returns the greatest common divisor
    public int GetGreatestCommonDivisor(int first, int second)
    {
        while (first != 0 && second != 0)
        {
            if (first > second) first %= second;
            else second %= first;
        }

        return first == 0 ? second : first;
    }

    //Gets the angle from one point to the next
    public float GetAngle(Vector2 current, Vector2 target)
    {
        Vector2 aspectRatio = GetAspectRatio(width, height);
        return (float)(Mathf.Atan2(aspectRatio.y * (target.y - current.y), aspectRatio.x * (target.x - current.x)) * (180 / Mathf.PI));
    }

    // Draws vertices for each point on the graph
    private void DrawVerticesForPoint(Vector2 point, Vector2 point2, VertexHelper vh, float angle)
    {
        float segAlpha = 1f;
        if (enableHorizontalGraphReveal && graphRevealProgress < 0.999f)
        {
            float left = Mathf.Min(graphRevealXMin, graphRevealXMax);
            float right = Mathf.Max(graphRevealXMin, graphRevealXMax);
            float revealX = Mathf.Lerp(left, right, graphRevealProgress);
            float feather = Mathf.Max((right - left) * 0.055f, 0.35f);
            float a0 = HorizontalRevealAlpha(point.x, revealX, feather);
            float a1 = HorizontalRevealAlpha(point2.x, revealX, feather);
            segAlpha = Mathf.Min(a0, a1);
        }

        UIVertex vertex = UIVertex.simpleVert;
        Color drawCol = Color.Lerp(color, Color.white, Mathf.Clamp01(playerProximityHighlight));
        drawCol.a *= segAlpha;
        vertex.color = drawCol;

        vertex.position = Quaternion.Euler(0, 0, angle) * new Vector3(-thickness / 2, 0);
        vertex.position += new Vector3(unitWidth * point.x, unitHeight * point.y);
        vh.AddVert(vertex);

        vertex.color = drawCol;
        vertex.position = Quaternion.Euler(0, 0, angle) * new Vector3(thickness / 2, 0);
        vertex.position += new Vector3(unitWidth * point.x, unitHeight * point.y);
        vh.AddVert(vertex);

        vertex.color = drawCol;
        vertex.position = Quaternion.Euler(0, 0, angle) * new Vector3(-thickness / 2, 0);
        vertex.position += new Vector3(unitWidth * point2.x, unitHeight * point2.y);
        vh.AddVert(vertex);

        vertex.color = drawCol;
        vertex.position = Quaternion.Euler(0, 0, angle) * new Vector3(thickness / 2, 0);
        vertex.position += new Vector3(unitWidth * point2.x, unitHeight * point2.y);
        vh.AddVert(vertex);
    }

    /// <summary>Call after updating <see cref="playerProximityHighlight"/> so the mesh repaints.</summary>
    public void RefreshHighlightGeometry()
    {
        SetVerticesDirty();
    }

    // Updates the line grid based on the parent graph
    private void UpdateGridSize()
    {
        if (grid != null && gridSize != grid.gridSize)
        {
            gridSize = grid.gridSize;
            SetVerticesDirty();
        }
    }
}