using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines a single stage: which curve to plot, what colors to use, and how the derivative
/// should influence gameplay (safe vs hazard/gaps), plus the story text.
/// </summary>
[CreateAssetMenu(menuName = "FirstPrinciples/Level Definition", fileName = "LevelDefinition")]
public class LevelDefinition : ScriptableObject
{
    [Header("Identity")]
    public string levelName = "Stage";
    [TextArea(2, 6)]
    public string storyText = "Follow the curve. Watch the derivative.";

    [Header("Graph Parameters (FunctionPlotter)")]
    public FunctionType functionType = FunctionType.Power;

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

    [Tooltip("X trigger positions (grid units, relative to left edge of the graph) where we pop the derivative.")]
    public List<float> stageTriggerX = new List<float>();

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

