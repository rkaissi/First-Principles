/*
 * FunctionPlotter.cs Written by John Seong
 * An Open-Source Project
 * Main Features:
 * 1. Plot Functions
 * 2. Plot Their Corresponding First Derivatives
 */

using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FunctionPlotter : MonoBehaviour
{
    // The starting & ending x coordinates
    public float xStart = 0f;
    public float xEnd = 10f;

    // Increase in xValue for each for loop iteration
    public float step = 0.5f;

    public float transA = 0, transK = 0, transC = 0, transD = 0;

    // Power variable only applicable to 'x^n' functions — represented as 'n' here
    // Base N variable only applicable to 'n^x' functions — represented as 'n' here

    public int power = 2;

    // A infinitesimally small number chosen in order to perform numerical differentiation
    private float hValue = (float)(Mathf.Pow(10, -4));

    // The type of function to be plotted
    public FunctionType functionType;

    public int baseN = 2;

    public bool differentiate = false;

    // Local points list
    private List<Vector2> points = new List<Vector2>();
    private List<Vector2> dPoints = new List<Vector2>();

    [SerializeField] TextMeshProUGUI equationText;

    private LineRendererUI lineRenderer;
    private DerivRendererUI derivRenderer;

    private void Reset()
    {
        InitPlotFunction();
        RefreshGrid();
    }

    private void OnValidate()
    {
        InitPlotFunction();
        RefreshGrid();
    }

    private void Update()
    {
        InitPlotFunction();
        RefreshGrid();
    }

    public void InitPlotFunction()
    {
        PlotFunction(this.functionType);
    }

    // Refresh ONLY the original function graph, not the derivative one
    public void RefreshLine()
    {
        lineRenderer.enabled = false;
        lineRenderer.enabled = true;
    }

    public void RefreshDeriv()
    {
        derivRenderer.enabled = false;
        derivRenderer.enabled = true;
    }

    public void RefreshGrid()
    {
        var grid = FindFirstObjectByType<GridRendererUI>();
        if (grid == null)
            return;
        grid.enabled = false;
        grid.enabled = true;
    }

    private void PlotFunction(FunctionType type)
    {
        lineRenderer = FindFirstObjectByType<LineRendererUI>();
        derivRenderer = FindFirstObjectByType<DerivRendererUI>();

        if (lineRenderer != null)
        {
            points.Clear();
            RefreshLine();

            ComputeGraph(type, transA, transK, transC, transD, power, baseN);
            lineRenderer.points = points;
        }

        if (differentiate == true && lineRenderer != null)
        {
            // Refresh ONLY the derivative graph & show on the UI
            RefreshDeriv();

            if (derivRenderer != null)
            {
                dPoints.Clear();
                points.Clear();
                RefreshLine();

                ComputeGraph(type, transA, transK, transC, transD, power, baseN);
                derivRenderer.points = dPoints;
            }
        }

        else if (differentiate == false)
        {
            dPoints.Clear();

            // Refresh ONLY the derivative graph & hide on the UI
            RefreshDeriv();
        }
    }

    public void ComputeGraph(FunctionType functionType, float transA, float transK, float transC, float transD, int power, int baseN)
    {
        if (equationText != null)
            UpdateEquationText(functionType, transA, transK, transC, transD, power, baseN);

        Vector2Int gridOrigin = lineRenderer.gridSize / 2;

        for (float i = xStart; i <= xEnd; i += step)
        {
            float xValue = i;
            float yValue = EvaluateFunctionY(functionType, transA, transK, transC, transD, power, baseN, xValue);
            float dyValue = (EvaluateFunctionY(functionType, transA, transK, transC, transD, power, baseN, xValue + hValue) - EvaluateFunctionY(functionType, transA, transK, transC, transD, power, baseN, xValue - hValue)) / (hValue * 2);

            if (IsFinite(yValue))
            {
                // Add the coordinates to the array
                this.points.Add(new Vector2(xValue + gridOrigin.x, yValue + gridOrigin.y));
            }

            if (IsFinite(dyValue))
            {
                // Get the differentiated coordinates to another array
                this.dPoints.Add(new Vector2(xValue + gridOrigin.x, dyValue + gridOrigin.y));
            }
        }
    }

    private float EvaluateFunctionY(FunctionType type, float transA, float transK, float transC, float transD, int power, int baseN, float xValue)
    {
        float u = transK * (xValue - transD);

        return type switch
        {
            FunctionType.Power => transA * (Mathf.Pow(u, power) + transC),
            FunctionType.Absolute => transA * (Mathf.Abs(u) + transC),
            FunctionType.Exponential => transA * (Mathf.Pow(baseN, u) + transC),
            FunctionType.NaturalExp => transA * (Mathf.Exp(u) + transC),
            FunctionType.Log => transA * (Mathf.Log10(u) + transC),
            FunctionType.NaturalLog => transA * (Mathf.Log(u) + transC),
            FunctionType.SquareRoot => transA * (Mathf.Sqrt(u) + transC),
            FunctionType.Sine => transA * (Mathf.Sin(u) + transC),
            FunctionType.Cosine => transA * (Mathf.Cos(u) + transC),
            FunctionType.Tangent => transA * (Mathf.Tan(u) + transC),
            _ => 0f
        };
    }

    private static bool IsFinite(float f) => !(float.IsNaN(f) || float.IsInfinity(f));

    private void UpdateEquationText(FunctionType type, float transA, float transK, float transC, float transD, int power, int baseN)
    {
        // Keep equation text simple and consistent; domain errors (e.g. log/sqrt of non-positive) are handled by skipping non-finite points.
        string a = transA.ToString();
        string k = transK.ToString();
        string c = transC.ToString();
        string d = transD.ToString();

        switch (type)
        {
            case FunctionType.Power:
                equationText.text = $"f(x) = {a}*(({k}*(x - {d}))^{power} + ({c}))";
                break;
            case FunctionType.Absolute:
                equationText.text = $"f(x) = {a}*(|{k}*(x - {d})| + ({c}))";
                break;
            case FunctionType.Exponential:
                equationText.text = $"f(x) = {a}*({baseN}^({k}*(x - {d})) + ({c}))";
                break;
            case FunctionType.NaturalExp:
                equationText.text = $"f(x) = {a}*(e^({k}*(x - {d})) + ({c}))";
                break;
            case FunctionType.Log:
                equationText.text = $"f(x) = {a}*(log10({k}*(x - {d})) + ({c}))";
                break;
            case FunctionType.NaturalLog:
                equationText.text = $"f(x) = {a}*(ln({k}*(x - {d})) + ({c}))";
                break;
            case FunctionType.SquareRoot:
                equationText.text = $"f(x) = {a}*(sqrt({k}*(x - {d})) + ({c}))";
                break;
            case FunctionType.Sine:
                equationText.text = $"f(x) = {a}*(sin({k}*(x - {d})) + ({c}))";
                break;
            case FunctionType.Cosine:
                equationText.text = $"f(x) = {a}*(cos({k}*(x - {d})) + ({c}))";
                break;
            case FunctionType.Tangent:
                equationText.text = $"f(x) = {a}*(tan({k}*(x - {d})) + ({c}))";
                break;
            default:
                equationText.text = "f(x)";
                break;
        }
    }
}

public enum FunctionType
{
    Power, Absolute, Exponential, NaturalExp, Log, NaturalLog, SquareRoot, Sine, Cosine, Tangent
}

/* 

:: Numerical Differentiation Explanation ::

You can't calculate the exact derivative of a function using a computer program (unless you're doing symbolic math... but that's another, way more complicated, topic).

There are several approaches to computing a numerical derivative of a function. The simplest is the centered three-point method:

Take a small number h
Evaluate  [f(x+h) - f(x-h)] / 2h 
Voilà, an approximation of f'(x), with only two function evaluations
Another approach is the centered five-point method:

Take a small number h
Evaluate [f(x-2h) - 8f(x-h) + 8f(x+h) - f(x+2h)] / 12h
Voilà, a better approximation of f'(x), but it requires more function evaluations

In this program, we use the relatively simpler approach 'the three-point method.'

*/