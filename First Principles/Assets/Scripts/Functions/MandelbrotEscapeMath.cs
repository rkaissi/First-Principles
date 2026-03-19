/// <summary>
/// Smooth (fractional) escape count for the quadratic Mandelbrot map z → z² + c, z₀ = 0.
/// Exterior uses the standard log–log smoothing so boundaries show filament detail; interior returns <paramref name="maxIter"/>.
/// </summary>
public static class MandelbrotEscapeMath
{
    const double Log2 = 0.6931471805599453;

    /// <param name="bailoutSq">Typical 256 — larger gives smoother exteriors (matches many explorer defaults).</param>
    public static float SmoothIterations(float cr, float ci, int maxIter, float bailoutSq = 256f)
    {
        float zr = 0f, zi = 0f;
        for (int n = 0; n < maxIter; n++)
        {
            float zr2 = zr * zr - zi * zi + cr;
            float zi2 = 2f * zr * zi + ci;
            zr = zr2;
            zi = zi2;
            float r2 = zr * zr + zi * zi;
            if (r2 > bailoutSq)
            {
                double logRn = 0.5 * System.Math.Log(r2);
                return n + (float)(1.0 - System.Math.Log(logRn) / Log2);
            }
        }

        return maxIter;
    }

    /// <summary>Integer escape step count (legacy / cheap checks).</summary>
    public static int IntegerIterations(float cr, float ci, int maxIter, float escapeRadiusSq = 4f)
    {
        float zr = 0f, zi = 0f;
        for (int n = 0; n < maxIter; n++)
        {
            float zr2 = zr * zr - zi * zi + cr;
            float zi2 = 2f * zr * zi + ci;
            zr = zr2;
            zi = zi2;
            if (zr * zr + zi * zi > escapeRadiusSq)
                return n;
        }

        return maxIter;
    }
}
