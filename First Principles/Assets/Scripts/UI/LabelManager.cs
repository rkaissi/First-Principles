using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Spawns TMP axis labels from <see cref="GridRendererUI"/> extents.
/// Y-axis numbers track <see cref="FunctionPlotter.AxisTickOffsetToMathY"/> when vertical auto-fit is on.
/// X-axis numbers track <see cref="FunctionPlotter.AxisTickOffsetToMathX"/> and refresh when the plot window
/// (zoom / pinch), <b>Trans</b> (<c>k</c>, <c>D</c>), or mode changes — calculator ticks show inner <c>u</c>.
/// </summary>
public class LabelManager : MonoBehaviour
{
    [SerializeField] private GameObject labelPrefab;

    //Label placement every how many grid increments
    [SerializeField] private int horizontalIncrement = 1;
    [SerializeField] private int verticalIncrement = 1;

    //Show the 0 label for the x axis
    [SerializeField] private bool xAxisOriginLabel = true;

    [SerializeField] private Vector2 xStartPos = new Vector2(0, 0);
    [SerializeField] private Vector2 yStartPos = new Vector2(0, 0);

    private List<TextMeshProUGUI> xLabels = new List<TextMeshProUGUI>();
    private List<TextMeshProUGUI> yLabels = new List<TextMeshProUGUI>();
    /// <summary>Signed plotter‑y tick offset from axis center (same units as pre–auto‑fit grid labels).</summary>
    private readonly List<float> _yTickOffsetsFromCenter = new List<float>();
    /// <summary>Signed plotter‑x tick offset from axis center (one column = one plotter‑<c>x</c> / polar <c>θ</c> unit).</summary>
    private readonly List<float> _xTickOffsetsFromCenter = new List<float>();

    private GridRendererUI gridRenderer;
    private float _lastAutoMid = float.NaN;
    private float _lastAutoScale = float.NaN;
    private float _lastXStart = float.NaN;
    private float _lastXEnd = float.NaN;
    private float _lastTransK = float.NaN;
    private float _lastTransD = float.NaN;
    private FunctionType _lastFunctionTypeForXAxis = (FunctionType)(-1);

    private void Awake()
    {
        gridRenderer = FindAnyObjectByType<GridRendererUI>();
    }

    private void Start()
    {
        GenerateLabels();
    }

    private void LateUpdate()
    {
        var plotter = FindAnyObjectByType<FunctionPlotter>();
        if (plotter == null)
            return;

        float mid = plotter.VerticalAxisLabelPivot;
        float sc = plotter.VerticalAxisLabelScale;

        if (!Mathf.Approximately(mid, _lastAutoMid) || !Mathf.Approximately(sc, _lastAutoScale))
        {
            _lastAutoMid = mid;
            _lastAutoScale = sc;
            RefreshYAxisLabelText();
        }

        if (HorizontalAxisStateChanged(plotter))
        {
            CacheHorizontalAxisState(plotter);
            RefreshXAxisLabelText();
        }
    }

    bool HorizontalAxisStateChanged(FunctionPlotter p)
    {
        // Plot window (zoom / pinch / scale).
        bool window = !Mathf.Approximately(p.xStart, _lastXStart)
                      || !Mathf.Approximately(p.xEnd, _lastXEnd);
        // CustomExpression axis readout is u = k·(x−D); must refresh when Trans or mode changes.
        bool trans = !Mathf.Approximately(p.transK, _lastTransK)
                     || !Mathf.Approximately(p.transD, _lastTransD);
        bool mode = p.functionType != _lastFunctionTypeForXAxis;
        return window || trans || mode;
    }

    void CacheHorizontalAxisState(FunctionPlotter p)
    {
        _lastXStart = p.xStart;
        _lastXEnd = p.xEnd;
        _lastTransK = p.transK;
        _lastTransD = p.transD;
        _lastFunctionTypeForXAxis = p.functionType;
    }

    public void GenerateLabels()
    {
        DeleteCurrentLabels();

        Vector2 xPos = xStartPos;
        Vector2 yPos = yStartPos;

        float xPosOffset = gridRenderer.rectTransform.rect.width / gridRenderer.gridSize.x;
        float yPosOffset = gridRenderer.rectTransform.rect.height / gridRenderer.gridSize.y;

        int xSize = gridRenderer.gridSize.x;
        int ySize = gridRenderer.gridSize.y;

        int xPositive = xSize / (2 * horizontalIncrement) + 1;
        int xNegative = (xPositive * 2) - 1;

        int yPositive = ySize / (2 * verticalIncrement) + 1;
        int yNegative = (yPositive * 2) - 1;

        var plotter = FindAnyObjectByType<FunctionPlotter>();

        //Horizontal Labels

        for (int i = 0; i < xPositive; i++)
        {
            float xTickOffset = i * horizontalIncrement;
            if (i == 0 && !xAxisOriginLabel)
            {
                xLabels.Add(null);
                _xTickOffsetsFromCenter.Add(xTickOffset);
                xPos.x += xPosOffset * horizontalIncrement;
                continue;
            }

            xLabels.Add(Instantiate(labelPrefab, transform.TransformPoint(xPos), Quaternion.identity, transform).GetComponent<TextMeshProUGUI>());
            _xTickOffsetsFromCenter.Add(xTickOffset);
            xLabels[xLabels.Count - 1].text = FormatXTick(plotter, xTickOffset);
            xPos.x += xPosOffset * horizontalIncrement;
        }

        xPos = xStartPos;
        xPos.x -= xPosOffset * horizontalIncrement;

        for (int i = xPositive; i < xNegative; i++)
        {
            float xTickOffset = -(i - (xPositive - 1)) * horizontalIncrement;
            xLabels.Add(Instantiate(labelPrefab, transform.TransformPoint(xPos), Quaternion.identity, transform).GetComponent<TextMeshProUGUI>());
            _xTickOffsetsFromCenter.Add(xTickOffset);
            xLabels[xLabels.Count - 1].text = FormatXTick(plotter, xTickOffset);
            xPos.x -= xPosOffset * horizontalIncrement;
        }

        //Vertical Labels
        for (int i = 0; i < yPositive; i++)
        {
            float tickOffset = i * verticalIncrement;
            yLabels.Add(Instantiate(labelPrefab, transform.TransformPoint(yPos), Quaternion.identity, transform).GetComponent<TextMeshProUGUI>());
            _yTickOffsetsFromCenter.Add(tickOffset);
            yLabels[yLabels.Count - 1].text = FormatYTick(plotter, tickOffset);
            yPos.y += yPosOffset * verticalIncrement;
        }

        yPos = yStartPos;
        yPos.y -= yPosOffset * verticalIncrement;

        for (int i = yPositive; i < yNegative; i++)
        {
            float tickOffset = -(i - (yPositive - 1)) * verticalIncrement;
            yLabels.Add(Instantiate(labelPrefab, transform.TransformPoint(yPos), Quaternion.identity, transform).GetComponent<TextMeshProUGUI>());
            _yTickOffsetsFromCenter.Add(tickOffset);
            yLabels[yLabels.Count - 1].text = FormatYTick(plotter, tickOffset);
            yPos.y -= yPosOffset * verticalIncrement;
        }

        var pAfter = FindAnyObjectByType<FunctionPlotter>();
        _lastAutoMid = pAfter != null ? pAfter.VerticalAxisLabelPivot : 0f;
        _lastAutoScale = pAfter != null ? pAfter.VerticalAxisLabelScale : 1f;
        RefreshYAxisLabelText();

        if (pAfter != null)
            CacheHorizontalAxisState(pAfter);
        else
        {
            _lastXStart = float.NaN;
            _lastXEnd = float.NaN;
            _lastTransK = float.NaN;
            _lastTransD = float.NaN;
            _lastFunctionTypeForXAxis = (FunctionType)(-1); // force first LateUpdate to call RefreshXAxisLabelText
        }

        RefreshXAxisLabelText();
    }

    void RefreshXAxisLabelText()
    {
        var plotter = FindAnyObjectByType<FunctionPlotter>();
        if (xLabels.Count != _xTickOffsetsFromCenter.Count)
            return;
        for (int i = 0; i < xLabels.Count; i++)
        {
            if (xLabels[i] == null)
                continue;
            xLabels[i].text = FormatXTick(plotter, _xTickOffsetsFromCenter[i]);
        }
    }

    static string FormatXTick(FunctionPlotter plotter, float tickOffsetFromCenter)
    {
        float v = plotter != null ? plotter.AxisTickOffsetToMathX(tickOffsetFromCenter) : tickOffsetFromCenter;
        return FormatAxisNumber(v);
    }

    void RefreshYAxisLabelText()
    {
        var plotter = FindAnyObjectByType<FunctionPlotter>();
        if (yLabels.Count != _yTickOffsetsFromCenter.Count)
            return;
        for (int i = 0; i < yLabels.Count; i++)
        {
            if (yLabels[i] == null)
                continue;
            yLabels[i].text = FormatYTick(plotter, _yTickOffsetsFromCenter[i]);
        }
    }

    static string FormatYTick(FunctionPlotter plotter, float tickOffsetFromCenter)
    {
        float v = plotter != null ? plotter.AxisTickOffsetToMathY(tickOffsetFromCenter) : tickOffsetFromCenter;
        return FormatAxisNumber(v);
    }

    static string FormatAxisNumber(float v)
    {
        if (float.IsNaN(v) || float.IsInfinity(v))
            return "—";
        if (Mathf.Abs(v) < 1e-5f)
            return "0";
        if (Mathf.Approximately(v, Mathf.Round(v)))
            return Mathf.RoundToInt(v).ToString();
        float av = Mathf.Abs(v);
        if (av >= 100f)
            return v.ToString("0");
        if (av >= 10f)
            return v.ToString("0.#");
        return v.ToString("0.##");
    }

    private void DeleteCurrentLabels()
    {
        xLabels.Clear();
        yLabels.Clear();
        _xTickOffsetsFromCenter.Clear();
        _yTickOffsetsFromCenter.Clear();

        if (transform.childCount > 0)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
        }
    }
}
