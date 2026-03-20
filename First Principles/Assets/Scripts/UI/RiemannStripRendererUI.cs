using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Semi-transparent quads under the main curve. Rebuild when level loads (LevelManager → Rebuild).
// Strips use plotter math space + gridOrigin like FunctionPlotter; RiemannRule.None draws midpoint samples.
/// <summary>
/// Draws semi-transparent vertical strips for Riemann rectangles (from x-axis to sample height).
/// Grid coordinates match <see cref="LineRendererUI"/> (same parent <see cref="GridRendererUI"/>).
/// </summary>
public class RiemannStripRendererUI : Graphic
{
    public Vector2Int gridSize = new Vector2Int(40, 40);

    private readonly List<Vector4> strips = new List<Vector4>(); // xL, xR, yMin, yMax (grid space)
    private GridRendererUI grid;

#if UNITY_EDITOR
    protected override void Reset()
    {
        base.Reset();
        raycastTarget = false;
        if (grid == null)
            grid = GetComponentInParent<GridRendererUI>();
        UpdateGridSizeFromParent();
    }
#endif

    protected override void Awake()
    {
        base.Awake();
        raycastTarget = false;
        if (grid == null)
            grid = GetComponentInParent<GridRendererUI>();
        UpdateGridSizeFromParent();
    }

    private void Update()
    {
        UpdateGridSizeFromParent();
    }

    private void UpdateGridSizeFromParent()
    {
        if (grid != null && grid.gridSize != gridSize)
        {
            gridSize = grid.gridSize;
            SetVerticesDirty();
        }
    }

    /// <summary>Clears rectangles (e.g. switching to free graphing mode).</summary>
    public void ClearStrips()
    {
        strips.Clear();
        SetVerticesDirty();
    }

    /// <summary>Rebuild strip geometry from level + plotter.</summary>
    public void Rebuild(LevelDefinition def, FunctionPlotter plotter)
    {
        strips.Clear();
        color = def.riemannFillColor;

        bool backdrop =
            def.riemannRectCount >= 1
            && plotter != null
            && (def.showRiemannVisualization
                || (def.useRiemannStairPlatforms && def.riemannRule != RiemannRule.None));

        if (!backdrop)
        {
            SetVerticesDirty();
            return;
        }

        if (grid == null)
            grid = GetComponentInParent<GridRendererUI>();
        if (grid != null)
            gridSize = grid.gridSize;

        Vector2Int origin = gridSize / 2;
        int n = Mathf.Max(1, def.riemannRectCount);
        float xStart = def.xStart;
        float xEnd = def.xEnd;
        float span = xEnd - xStart;
        if (span <= 1e-6f)
        {
            SetVerticesDirty();
            return;
        }

        float dx = span / n;

        for (int i = 0; i < n; i++)
        {
            float xL = xStart + i * dx;
            float xR = xStart + (i + 1) * dx;
            float xS = SampleX(def.riemannRule, xL, xR);
            float gyTop = plotter.SampleCurveGridY(xS);
            if (!IsFinite(gyTop))
                continue;

            float gxL = xL + origin.x;
            float gxR = xR + origin.x;
            float gyAxis = origin.y;
            float ymin = Mathf.Min(gyAxis, gyTop);
            float ymax = Mathf.Max(gyAxis, gyTop);

            strips.Add(new Vector4(gxL, gxR, ymin, ymax));
        }

        SetVerticesDirty();
    }

    private static float SampleX(RiemannRule rule, float xL, float xR)
    {
        return rule switch
        {
            RiemannRule.Left => xL,
            RiemannRule.Right => xR,
            RiemannRule.Midpoint => 0.5f * (xL + xR),
            _ => 0.5f * (xL + xR)
        };
    }

    private static bool IsFinite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (strips.Count == 0)
            return;

        float width = rectTransform.rect.width;
        float height = rectTransform.rect.height;
        if (gridSize.x < 1 || gridSize.y < 1)
            return;

        float unitWidth = width / gridSize.x;
        float unitHeight = height / gridSize.y;
        Color c = color;

        int baseIndex = 0;
        for (int i = 0; i < strips.Count; i++)
        {
            var s = strips[i];
            float xL = s.x;
            float xR = s.y;
            float ymin = s.z;
            float ymax = s.w;

            UIVertex v = UIVertex.simpleVert;
            v.color = c;

            v.position = new Vector3(unitWidth * xL, unitHeight * ymin, 0f);
            vh.AddVert(v);

            v.position = new Vector3(unitWidth * xR, unitHeight * ymin, 0f);
            vh.AddVert(v);

            v.position = new Vector3(unitWidth * xR, unitHeight * ymax, 0f);
            vh.AddVert(v);

            v.position = new Vector3(unitWidth * xL, unitHeight * ymax, 0f);
            vh.AddVert(v);

            vh.AddTriangle(baseIndex, baseIndex + 1, baseIndex + 2);
            vh.AddTriangle(baseIndex, baseIndex + 2, baseIndex + 3);
            baseIndex += 4;
        }
    }
}
