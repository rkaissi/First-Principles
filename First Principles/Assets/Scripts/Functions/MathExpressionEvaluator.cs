using System;
using UnityEngine;

/// <summary>
/// Small math parser for typed <c>f(x)</c> in graphing calculator mode (no <c>eval()</c>, no code execution).
/// Supports <c>+ − * / ^</c>, parentheses, unary <c>−</c>, <c>sin cos tan asin acos atan sqrt abs</c>,
/// <c>log</c> (base 10), <c>ln</c>, <c>exp</c>, <c>min max</c> (two args), constants <c>pi</c> <c>e</c>, implicit multiply (e.g. <c>2x</c>, <c>2(</c>, <c>)x</c>).
/// </summary>
public static class MathExpressionEvaluator
{
    public static bool TryEvaluate(string expression, float x, out float y, out string error)
    {
        y = float.NaN;
        error = null;
        if (string.IsNullOrWhiteSpace(expression))
        {
            error = "Empty expression";
            return false;
        }

        try
        {
            var p = new Parser(expression.Trim(), x);
            y = p.ParseExpression();
            p.SkipWs();
            if (!p.Eof())
            {
                error = $"Unexpected text: \"{p.PeekRest()}\"";
                return false;
            }

            return true;
        }
        catch (ParseException ex)
        {
            error = ex.Message;
            return false;
        }
    }

    /// <summary>Quick sanity check at a few sample points (may still be undefined elsewhere).</summary>
    public static bool TryValidateRough(string expression, out string error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(expression))
        {
            error = "Empty expression";
            return false;
        }

        foreach (float sx in new[] { -1f, 0f, 0.5f, 1f, 2f })
        {
            if (!TryEvaluate(expression, sx, out float y, out string err))
            {
                error = err;
                return false;
            }

            // NaN at a sample point is OK (domain hole); reject only non-finite overflows.
            if (float.IsInfinity(y))
            {
                error = "Value overflow";
                return false;
            }
        }

        return true;
    }

    private class ParseException : Exception
    {
        public ParseException(string message) : base(message) { }
    }

    private class Parser
    {
        private readonly string s;
        private readonly float xVal;
        private int i;

        public Parser(string src, float x)
        {
            s = src;
            xVal = x;
            i = 0;
        }

        public bool Eof() => i >= s.Length;

        public string PeekRest()
        {
            SkipWs();
            return i < s.Length ? s.Substring(i, Mathf.Min(24, s.Length - i)) : "";
        }

        public void SkipWs()
        {
            while (i < s.Length && char.IsWhiteSpace(s[i]))
                i++;
        }

        private bool Match(char c)
        {
            SkipWs();
            if (i < s.Length && s[i] == c)
            {
                i++;
                return true;
            }

            return false;
        }

        private bool MatchInsensitive(string word)
        {
            SkipWs();
            if (i + word.Length > s.Length)
                return false;
            for (int k = 0; k < word.Length; k++)
            {
                if (char.ToLowerInvariant(s[i + k]) != word[k])
                    return false;
            }

            // avoid matching "sin" inside "xsine"
            if (i + word.Length < s.Length && char.IsLetterOrDigit(s[i + word.Length]))
                return false;

            i += word.Length;
            return true;
        }

        public float ParseExpression() => ParseAddSub();

        private float ParseAddSub()
        {
            float v = ParseMulDiv();
            while (true)
            {
                SkipWs();
                if (Match('+'))
                    v += ParseMulDiv();
                else if (Match('-'))
                    v -= ParseMulDiv();
                else
                    break;
            }

            return v;
        }

        private float ParseMulDiv()
        {
            float v = ParseUnary();
            while (true)
            {
                SkipWs();
                if (Match('*'))
                {
                    v *= ParseUnary();
                }
                else if (Match('/'))
                {
                    float d = ParseUnary();
                    if (Mathf.Abs(d) < 1e-12f)
                        return float.NaN;
                    v /= d;
                }
                else if (ImplicitMultiplyFollows())
                {
                    v *= ParseUnary();
                }
                else
                    break;
            }

            return v;
        }

        private bool ImplicitMultiplyFollows()
        {
            if (Eof())
                return false;
            SkipWs();
            if (i >= s.Length)
                return false;
            char c = s[i];
            // After a value, "2x", "2(", ")(" , "2 sin"
            if (char.IsDigit(c) || c == '.' || c == '(')
                return true;
            if (char.IsLetter(c))
            {
                // unary minus after * is handled elsewhere; here "3x" "3sin"
                return true;
            }

            return false;
        }

        private float ParseUnary()
        {
            SkipWs();
            if (Match('+'))
                return ParseUnary();
            if (Match('-'))
                return -ParseUnary();
            return ParsePow();
        }

        private float ParsePow()
        {
            float b = ParsePrimary();
            SkipWs();
            if (Match('^') || MatchTwo('*'))
            {
                float e = ParsePow(); // right-associative
                try
                {
                    // Avoid Mathf.Pow domain noise for simple integers
                    if (Mathf.Abs(e - Mathf.Round(e)) < 1e-5f && e > -30f && e < 30f)
                        return IntPowSafe(b, Mathf.RoundToInt(e));
                    return Mathf.Pow(b, e);
                }
                catch
                {
                    return float.NaN;
                }
            }

            return b;
        }

        private bool MatchTwo(char c)
        {
            SkipWs();
            if (i + 1 < s.Length && s[i] == c && s[i + 1] == c)
            {
                i += 2;
                return true;
            }

            return false;
        }

        private static float IntPowSafe(float b, int exp)
        {
            if (exp == 0)
                return 1f;
            float r = 1f;
            int ae = Mathf.Abs(exp);
            for (int k = 0; k < ae; k++)
                r *= b;
            return exp < 0 ? 1f / r : r;
        }

        private float ParsePrimary()
        {
            SkipWs();
            if (i >= s.Length)
                throw new ParseException("Unexpected end of expression");

            if (Match('('))
            {
                float v = ParseExpression();
                if (!Match(')'))
                    throw new ParseException("Missing ')'");
                return v;
            }

            if (char.IsDigit(s[i]) || (s[i] == '.' && i + 1 < s.Length && char.IsDigit(s[i + 1])))
                return ReadNumber();

            return ParseIdentOrCall();
        }

        private float ReadNumber()
        {
            int start = i;
            bool dot = false;
            while (i < s.Length)
            {
                char c = s[i];
                if (char.IsDigit(c))
                    i++;
                else if (c == '.' && !dot)
                {
                    dot = true;
                    i++;
                }
                else
                    break;
            }

            if (start == i)
                throw new ParseException("Expected number");
            string slice = s.Substring(start, i - start);
            if (!float.TryParse(slice, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float v))
                throw new ParseException("Bad number");
            return v;
        }

        private float ParseIdentOrCall()
        {
            // identifiers: x, pi, e, functions
            if (MatchInsensitive("sinh"))
                return (float)System.Math.Sinh(ClampArg(ExpectFn1Arg(), 4f));
            if (MatchInsensitive("cosh"))
                return (float)System.Math.Cosh(ClampArg(ExpectFn1Arg(), 8f));
            if (MatchInsensitive("tanh"))
                return (float)System.Math.Tanh(ClampArg(ExpectFn1Arg(), 8f));
            if (MatchInsensitive("asin"))
                return Mathf.Asin(ExpectFn1Arg());
            if (MatchInsensitive("acos"))
                return Mathf.Acos(ExpectFn1Arg());
            if (MatchInsensitive("atan"))
                return Mathf.Atan(ExpectFn1Arg());
            if (MatchInsensitive("sin"))
                return Mathf.Sin(ExpectFn1Arg());
            if (MatchInsensitive("cos"))
                return Mathf.Cos(ExpectFn1Arg());
            if (MatchInsensitive("tan"))
                return Mathf.Tan(ExpectFn1Arg());
            if (MatchInsensitive("sqrt"))
                return Mathf.Sqrt(ExpectFn1Arg());
            if (MatchInsensitive("abs"))
                return Mathf.Abs(ExpectFn1Arg());
            if (MatchInsensitive("log"))
            {
                // log10
                float a = ExpectFn1Arg();
                if (a <= 0f)
                    return float.NaN;
                return Mathf.Log10(a);
            }

            if (MatchInsensitive("ln"))
            {
                float a = ExpectFn1Arg();
                if (a <= 0f)
                    return float.NaN;
                return Mathf.Log(a);
            }

            if (MatchInsensitive("exp"))
                return Mathf.Exp(ClampArg(ExpectFn1Arg(), 20f));
            if (MatchInsensitive("min"))
                return ExpectFn2Args(min: true);
            if (MatchInsensitive("max"))
                return ExpectFn2Args(min: false);

            if (MatchInsensitive("pi"))
                return Mathf.PI;
            if (MatchInsensitive("e"))
                return Mathf.Exp(1f);
            if (MatchInsensitive("x"))
                return xVal;

            throw new ParseException($"Unknown symbol near \"{PeekRest()}\"");
        }

        private static float ClampArg(float v, float lim)
        {
            return Mathf.Clamp(v, -lim, lim);
        }

        private float ExpectFn1Arg()
        {
            if (!Match('('))
                throw new ParseException("Expected '(' after function");
            float v = ParseExpression();
            if (!Match(')'))
                throw new ParseException("Missing ')'");
            return v;
        }

        private float ExpectFn2Args(bool min)
        {
            if (!Match('('))
                throw new ParseException("Expected '(' after function");
            float a = ParseExpression();
            SkipWs();
            if (!Match(','))
                throw new ParseException("Expected ','");
            float b = ParseExpression();
            if (!Match(')'))
                throw new ParseException("Missing ')'");
            return min ? Mathf.Min(a, b) : Mathf.Max(a, b);
        }
    }
}
