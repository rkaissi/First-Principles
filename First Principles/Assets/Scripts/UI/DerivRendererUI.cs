using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Same rendering approach as <see cref="LineRendererUI"/> but used for numeric derivative samples.
/// Tint/thickness animated by <see cref="DerivativePopAnimator"/> during stage transitions.
/// </summary>
public class DerivRendererUI : Graphic
{
    public Vector2Int gridSize;

    public List<Vector2> points = new List<Vector2>();

    public float thickness = 10f;

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

    private void Update()
    {
        UpdateGridSize();
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
        UIVertex vertex = UIVertex.simpleVert;
        Color drawCol = Color.Lerp(color, Color.white, Mathf.Clamp01(playerProximityHighlight));
        vertex.color = drawCol;

        vertex.position = Quaternion.Euler(0, 0, angle) * new Vector3(-thickness / 2, 0);
        vertex.position += new Vector3(unitWidth * point.x, unitHeight * point.y);
        vh.AddVert(vertex);

        vertex.color = drawCol;
        vertex.position = Quaternion.Euler(0, 0, angle) * new Vector3(thickness / 2, 0);
        vertex.position += new Vector3(unitWidth * point.x, unitHeight * point.y);
        vh.AddVert(vertex);

        vertex.position = Quaternion.Euler(0, 0, angle) * new Vector3(-thickness / 2, 0);
        vertex.position += new Vector3(unitWidth * point2.x, unitHeight * point2.y);
        vh.AddVert(vertex);

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