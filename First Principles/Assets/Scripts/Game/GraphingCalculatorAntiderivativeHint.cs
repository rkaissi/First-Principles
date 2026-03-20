using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Best-effort symbolic primitive string for common <c>f(u)</c> patterns (typed calculator). Falls back to generic +C text.
/// </summary>
public static class GraphingCalculatorAntiderivativeHint
{
    public static string TryFormatPrimitiveLine(string rawExpression)
    {
        if (string.IsNullOrWhiteSpace(rawExpression))
            return "";

        string s = rawExpression.Trim().ToLowerInvariant();
        s = Regex.Replace(s, @"\s+", "");

        // Constant
        if (Regex.IsMatch(s, @"^-?(\d+\.?\d*|\.\d+)$"))
        {
            if (float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float c))
                return $"{FormatNum(c)}·u + C";
        }

        // u, x as variable
        if (s == "x" || s == "u")
            return "u²/2 + C";

        // Power x^n or u^n
        var pow = Regex.Match(s, @"^(?:x|u)\^(\d+)$");
        if (pow.Success && int.TryParse(pow.Groups[1].Value, out int n))
        {
            int d = n + 1;
            return $"u^{d}/{d} + C";
        }

        if (s == "x^2" || s == "u^2")
            return "u³/3 + C";
        if (s == "x^3" || s == "u^3")
            return "u⁴/4 + C";

        if (s.Contains("sin(x)") || s.Contains("sin(u)"))
            return "-cos(u) + C";
        if (s.Contains("cos(x)") || s.Contains("cos(u)"))
            return "sin(u) + C";

        if (s.Contains("exp(") || s == "e^x" || s == "e^ x")
            return "exp(u) + C";

        if (s.Contains("1/x") || s.Contains("1/u") || s == "x^-1" || s == "u^-1")
            return "ln|u| + C";

        return ""; // caller may use generic fallback
    }

    static string FormatNum(float c)
        => Mathf.Approximately(c, Mathf.Round(c)) ? Mathf.RoundToInt(c).ToString() : c.ToString("0.###");
}
