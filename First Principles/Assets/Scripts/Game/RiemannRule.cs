// Used by LevelDefinition, RiemannStripRendererUI, and GraphObstacleGenerator (stair platforms).
// None: integration intro may still draw midpoint strips in the renderer; gameplay treats as “no stair rule”.
/// <summary>Which sample height is used inside each subinterval for Riemann rectangles / stair platforms.</summary>
public enum RiemannRule
{
    /// <summary>Visualization may default to midpoint; no dedicated stair sampling in obstacle gen.</summary>
    None = 0,
    /// <summary>Sample at left endpoint x_i of each strip.</summary>
    Left = 1,
    /// <summary>Sample at right endpoint x_{i+1} of each strip.</summary>
    Right = 2,
    /// <summary>Sample at strip midpoint.</summary>
    Midpoint = 3
}
