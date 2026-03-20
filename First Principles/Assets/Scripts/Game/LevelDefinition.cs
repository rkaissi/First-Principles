using System.Collections.Generic;
using UnityEngine;

// -----------------------------------------------------------------------------
// LevelDefinition — Data container for one playable graph stage
// -----------------------------------------------------------------------------
// Used in two ways: (1) CreateAssetMenu .asset files in the editor, (2) runtime
// ScriptableObject.CreateInstance in LevelManager.MakeLevel. Keep field semantics in
// sync with LevelManager.ApplyLevelTheme and GraphObstacleGenerator.GenerateWorld.
// -----------------------------------------------------------------------------

/// <summary>
/// Defines a single stage: which curve to plot, what colors to use, and how the derivative
/// should influence gameplay (safe vs hazard/gaps), plus story text and optional Riemann UX.
/// </summary>
[CreateAssetMenu(menuName = "FirstPrinciples/Level Definition", fileName = "LevelDefinition")]
public class LevelDefinition : ScriptableObject
{
    [Header("Identity")]
    public string levelName = "Stage";
    [TextArea(4, 12)]
    public string storyText = "Follow the curve. Watch the derivative.";
    [Tooltip("Extra seconds the story stays readable after fading in (0 = use LevelManager default).")]
    public float storyPauseSeconds = 0f;

    [Header("Graph Parameters (FunctionPlotter)")]
    public FunctionType functionType = FunctionType.Power;

    [Tooltip("Fit f(x) and f′(x) vertically so the curve uses most of the grid (exaggerates flat graphs). Off for graphing calculator mode.")]
    public bool autoFitGraphVertical = true;

    [Tooltip("Target fraction of half the grid height (from center line) used by the curve band (after padding).")]
    [Range(0.38f, 0.92f)]
    public float graphVerticalFillFraction = 0.74f;

    public float xStart = -20f;
    public float xEnd = 20f;
    public float step = 0.1f;

    public float transA = 1f;
    public float transK = 0.4f;
    public float transC = -2f;
    public float transD = 2f;

    public int power = 2;
    public int baseN = 2;

    [Header("Derivative-Driven Gameplay")]
    [Tooltip("When derivative dyValue is greater than this threshold, the column is considered safe.")]
    public float derivativeSafeThreshold = 0f;

    [Tooltip("How many columns at the start should always have platforms (to ensure you can start the level).")]
    public int forcePlatformsAtStartColumns = 3;

    [Tooltip("How many columns at the end should always have platforms (to ensure finish is reachable).")]
    public int forcePlatformsAtEndColumns = 1;

    [Tooltip("Platforms are created with a fixed height in grid units.")]
    public float platformThicknessGrid = 0.6f;

    [Tooltip("Visual hazard/spikes height in grid units.")]
    public float hazardHeightGrid = 0.5f;

    [Header("Visual Theme")]
    public Color curveColor = new Color(1f, 0.759119f, 0f, 1f);
    public Color derivativeColor = new Color(1f, 0.1782313f, 0f, 1f);

    [Tooltip("Optional per-stage pop colors (if empty, derivativeColor will be used).")]
    public List<Color> stageDerivativePopColors = new List<Color>();

    [Tooltip("For AeroDragPolarTriple: overlay colors [parasitic C_D,par line, induced C_D,ind curve]. Total uses curveColor.")]
    public List<Color> dragPolarOverlayColors = new List<Color>();

    [Tooltip("X trigger positions (grid units, relative to left edge of the graph) where we pop the derivative.")]
    public List<float> stageTriggerX = new List<float>();

    [Header("Level flow (optional)")]
    [Tooltip("How many derivative-pop boundaries to use (0 = LevelManager default).")]
    public int derivativePopTriggerCount = 0;

    [Tooltip("Tint the background grid to match this level’s mood.")]
    public bool applyGridTheming = false;

    public Color gridCenterLineTheming = new Color(1f, 1f, 1f, 0.39f);
    public Color gridOutsideLineTheming = new Color(1f, 1f, 1f, 0.14f);

    [Header("Riemann sums & area under the curve")]
    [Tooltip("Left / right / midpoint sample for rectangles and optional stair platforms.")]
    public RiemannRule riemannRule = RiemannRule.None;

    [Tooltip("Number of subintervals n (rectangles). Larger n → closer to ∫ f dx.")]
    [Min(1)]
    public int riemannRectCount = 16;

    [Tooltip("Fill rectangles from the x-axis to f(x*) in the graph plane.")]
    public bool showRiemannVisualization = false;

    [Tooltip("Platforms are flat per subinterval at the Riemann sample height (step terrain under the curve).")]
    public bool useRiemannStairPlatforms = false;

    [Tooltip("When stair platforms are on: each tread covers this fraction of its subinterval width (rest is air). Lower = wider gaps, more jumps.")]
    [Range(0.22f, 1f)]
    public float riemannPlatformCoverage = 0.62f;

    public Color riemannFillColor = new Color(0.25f, 0.55f, 0.95f, 0.32f);

    public void EnsureDefaultStagePopData(int stageCount)
    {
        if (stageCount < 1)
            return;

        if (stageDerivativePopColors == null || stageDerivativePopColors.Count == 0)
        {
            stageDerivativePopColors = new List<Color>(stageCount);
            for (int i = 0; i < stageCount; i++)
            {
                // Simple variation across stages while staying within the derivative theme.
                float t = stageCount == 1 ? 0f : (float)i / (stageCount - 1);
                stageDerivativePopColors.Add(Color.Lerp(derivativeColor, Color.white, 0.25f * t));
            }
        }

        if (stageTriggerX == null || stageTriggerX.Count == 0)
        {
            stageTriggerX = new List<float>(stageCount);
            for (int i = 1; i <= stageCount; i++)
            {
                // Triggers at 1/(stageCount+1) increments across the grid width.
                stageTriggerX.Add(0f); // LevelManager will convert/override based on actual grid size.
            }
        }
    }
}

