// Written by Rayan Kaissi — uGUI Graphic: thick polyline for f(x) in grid coordinates.
// FunctionPlotter fills `points`; OnPopulateMesh extrudes segments. Keep gridSize aligned with GridRendererUI.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LineRendererUI : Graphic
{
    internal const string DragPolarOverlayNamePrefix = "DragPolar_";
    internal const string GraphCalcDerivOverlayPrefix = "GraphCalcDeriv_";

    /// <summary>
    /// Main function curve (excludes drag-polar / graphing-calculator overlay clones).
    /// </summary>
    public static LineRendererUI FindPrimaryCurve()
    {
        var all = UnityEngine.Object.FindObjectsByType<LineRendererUI>(FindObjectsInactive.Include);
        foreach (var lr in all)
        {
            if (lr == null || lr.gameObject == null)
                continue;
            string n = lr.gameObject.name;
            if (n.StartsWith(DragPolarOverlayNamePrefix, StringComparison.Ordinal))
                continue;
            if (n.StartsWith(GraphCalcDerivOverlayPrefix, StringComparison.Ordinal))
                continue;
            return lr;
        }

        return all != null && all.Length > 0 ? all[0] : null;
    }
    public Vector2Int gridSize;

    /// <summary>Polyline in grid space (x,y) matching FunctionPlotter’s sampled curve.</summary>
    public List<Vector2> points = new List<Vector2>();

    public float thickness = 10f;

    [Tooltip("When < 1, the polyline fades in left→right (set by FunctionPlotter).")]
    [Range(0f, 1f)]
    public float graphRevealProgress = 1f;

    public float graphRevealXMin;
    public float graphRevealXMax;

    [Tooltip("Disables horizontal reveal (e.g. for special overlay tests).")]
    public bool enableHorizontalGraphReveal = true;

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

    /// <summary>Left→right alpha sweep in grid x (see <see cref="DrawVerticesForPoint"/>).</summary>
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

    // Returns the greatest common divisor using the Euclidean Algorithm
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
        vertex.color = color * new Color(1f, 1f, 1f, segAlpha);

        vertex.position = Quaternion.Euler(0, 0, angle) * new Vector3(-thickness / 2, 0);
        vertex.position += new Vector3(unitWidth * point.x, unitHeight * point.y);
        vh.AddVert(vertex);

        vertex.color = color * new Color(1f, 1f, 1f, segAlpha);
        vertex.position = Quaternion.Euler(0, 0, angle) * new Vector3(thickness / 2, 0);
        vertex.position += new Vector3(unitWidth * point.x, unitHeight * point.y);
        vh.AddVert(vertex);

        vertex.color = color * new Color(1f, 1f, 1f, segAlpha);
        vertex.position = Quaternion.Euler(0, 0, angle) * new Vector3(-thickness / 2, 0);
        vertex.position += new Vector3(unitWidth * point2.x, unitHeight * point2.y);
        vh.AddVert(vertex);

        vertex.color = color * new Color(1f, 1f, 1f, segAlpha);
        vertex.position = Quaternion.Euler(0, 0, angle) * new Vector3(thickness / 2, 0);
        vertex.position += new Vector3(unitWidth * point2.x, unitHeight * point2.y);
        vh.AddVert(vertex);
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