using System;
using UnityEngine;

/// <summary>Central-difference approximations for 1st–4th derivative (graphing calculator overlays).</summary>
public static class MathNthDerivative
{
    /// <summary>Numeric dⁿf/dxⁿ at <paramref name="x"/> (n in 1..4).</summary>
    public static float Evaluate(Func<float, float> f, float x, int order, float h)
    {
        h = Mathf.Clamp(Mathf.Abs(h), 1e-6f, 0.05f);
        if (f == null)
            return float.NaN;
        order = Mathf.Clamp(order, 1, 4);
        try
        {
            return order switch
            {
                1 => (f(x + h) - f(x - h)) / (2f * h),
                2 => (f(x + h) - 2f * f(x) + f(x - h)) / (h * h),
                3 => (-f(x - 2f * h) + 2f * f(x - h) - 2f * f(x + h) + f(x + 2f * h)) / (2f * h * h * h),
                4 => (f(x - 2f * h) - 4f * f(x - h) + 6f * f(x) - 4f * f(x + h) + f(x + 2f * h)) / (h * h * h * h),
                _ => float.NaN
            };
        }
        catch
        {
            return float.NaN;
        }
    }
}
