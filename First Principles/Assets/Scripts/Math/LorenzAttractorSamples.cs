using UnityEngine;

/// <summary>
/// Pre-integrated Lorenz attractor (σ=10, ρ=28, β=8/3) for the butterfly-effect boss stage.
/// Horizontal axis in-game maps to time; height samples the <b>x</b> coordinate (chaotic, sensitive ICs).
/// </summary>
public static class LorenzAttractorSamples
{
    private static float[] _xSeries;
    private static float _tMax = 1f;
    private static bool _built;

    private const float Sigma = 10f;
    private const float Rho = 28f;
    private const float Beta = 8f / 3f;

    /// <summary>Normalized x in roughly [-1,1] after burn-in, for time in [0, <see cref="TimeMax"/>].</summary>
    public static float SampleNormalizedX(float time)
    {
        EnsureBuilt();
        if (_xSeries == null || _xSeries.Length < 4)
            return 0f;
        time = Mathf.Clamp(time, 0f, _tMax);
        float u = time / _tMax * (_xSeries.Length - 1);
        int i = Mathf.FloorToInt(u);
        float f = u - i;
        i = Mathf.Clamp(i, 0, _xSeries.Length - 2);
        return Mathf.Lerp(_xSeries[i], _xSeries[i + 1], f);
    }

    public static float TimeMax
    {
        get
        {
            EnsureBuilt();
            return _tMax;
        }
    }

    private static void EnsureBuilt()
    {
        if (_built)
            return;
        _built = true;

        const int burnInSteps = 900;
        const int n = 3600;
        const float dt = 0.028f;

        float x = 0.2f, y = 0.15f, z = 0.1f;

        for (int s = 0; s < burnInSteps; s++)
            StepEuler(ref x, ref y, ref z, dt);

        _xSeries = new float[n];
        float xMin = x, xMax = x;
        float bx = x, by = y, bz = z;

        for (int i = 0; i < n; i++)
        {
            StepEuler(ref bx, ref by, ref bz, dt);
            _xSeries[i] = bx;
            if (bx < xMin) xMin = bx;
            if (bx > xMax) xMax = bx;
        }

        float span = Mathf.Max(xMax - xMin, 1e-3f);
        float mid = 0.5f * (xMax + xMin);
        for (int i = 0; i < n; i++)
            _xSeries[i] = (_xSeries[i] - mid) / span * 2f;

        _tMax = (n - 1) * dt;
    }

    private static void StepEuler(ref float x, ref float y, ref float z, float dt)
    {
        float dx = Sigma * (y - x);
        float dy = x * (Rho - z) - y;
        float dz = x * y - Beta * z;
        x += dx * dt;
        y += dy * dt;
        z += dz * dt;
    }
}
