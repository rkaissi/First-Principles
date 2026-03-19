using System.Collections.Generic;
using System.Text;

// -----------------------------------------------------------------------------
// TmpLatex — small LaTeX → TextMeshPro rich text (\(…\), \[…\], $$…$$)
// -----------------------------------------------------------------------------
// Fractions use <sup>/<sub> + ⁄; sub/sup; Greek; integrals; \text/\mathrm unwrap.
// -----------------------------------------------------------------------------

/// <summary>
/// Parses lightweight LaTeX delimiters in strings that may also contain TMP tags (&lt;b&gt;, &lt;color&gt;, etc.).
/// </summary>
public static class TmpLatex
{
    /// <summary>Process <c>\( … \)</c>, <c>\[ … \]</c>, and display <c>$$ … $$</c>.</summary>
    public static string Process(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var sb = new StringBuilder(input.Length + 64);
        int i = 0;
        while (i < input.Length)
        {
            if (TryMatch(input, i, "\\[", out int afterOpen) &&
                TryFindClosingDisplay(input, afterOpen, out int innerStart, out int innerEnd, out int afterClose))
            {
                sb.Append("\n<size=102%><align=center>");
                sb.Append(ConvertLatexFragment(input.Substring(innerStart, innerEnd - innerStart)));
                sb.Append("</align></size>\n");
                i = afterClose;
                continue;
            }

            if (TryMatch(input, i, "$$", out int afterDollar) &&
                TryFindDoubleDollarClose(input, afterDollar, out int ddA, out int ddB, out int afterDdClose))
            {
                sb.Append("\n<size=105%><align=center>");
                sb.Append(ConvertLatexFragment(input.Substring(ddA, ddB - ddA)));
                sb.Append("</align></size>\n");
                i = afterDdClose;
                continue;
            }

            if (TryMatch(input, i, "\\(", out int afterLp) &&
                TryFindInlineClose(input, afterLp, out int inA, out int inB, out int afterRp))
            {
                sb.Append(ConvertLatexFragment(input.Substring(inA, inB - inA)));
                i = afterRp;
                continue;
            }

            sb.Append(input[i]);
            i++;
        }

        return sb.ToString();
    }

    private static bool TryMatch(string s, int i, string token, out int after)
    {
        after = i;
        if (i + token.Length > s.Length)
            return false;
        for (int k = 0; k < token.Length; k++)
        {
            if (s[i + k] != token[k])
                return false;
        }

        after = i + token.Length;
        return true;
    }

    private static bool TryFindInlineClose(string s, int from, out int innerStart, out int innerEnd, out int afterClose)
    {
        innerStart = from;
        for (int j = from; j < s.Length - 1; j++)
        {
            if (s[j] == '\\' && s[j + 1] == ')')
            {
                innerEnd = j;
                afterClose = j + 2;
                return true;
            }
        }

        innerEnd = from;
        afterClose = from;
        return false;
    }

    private static bool TryFindClosingDisplay(string s, int from, out int innerStart, out int innerEnd, out int afterClose)
    {
        innerStart = from;
        for (int j = from; j < s.Length - 1; j++)
        {
            if (s[j] == '\\' && s[j + 1] == ']')
            {
                innerEnd = j;
                afterClose = j + 2;
                return true;
            }
        }

        innerEnd = from;
        afterClose = from;
        return false;
    }

    private static bool TryFindDoubleDollarClose(string s, int from, out int innerStart, out int innerEnd, out int afterClose)
    {
        innerStart = from;
        for (int j = from; j < s.Length - 1; j++)
        {
            if (s[j] == '$' && s[j + 1] == '$')
            {
                innerEnd = j;
                afterClose = j + 2;
                return true;
            }
        }

        innerEnd = from;
        afterClose = from;
        return false;
    }

    private static string ConvertLatexFragment(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return raw;

        string s = raw.Trim();
        s = s.Replace(@"\,", " ").Replace(@"\;", " ").Replace(@"\:", "\u2009").Replace(@"\!", "");
        s = s.Replace(@"\tfrac", @"\frac").Replace(@"\displaystyle", "").Replace(@"\textstyle", "");
        s = s.Replace(@"\ast", "*");

        int guard = 0;
        while (guard++ < 64)
        {
            int f = s.IndexOf(@"\frac", System.StringComparison.Ordinal);
            if (f < 0)
                break;
            if (!TryParseBraced(s, f + 5, out int aStart, out int aEnd, out int afterA))
                break;
            if (afterA >= s.Length || s[afterA] != '{')
                break;
            if (!TryParseBraced(s, afterA, out int bStart, out int bEnd, out int afterB))
                break;

            string num = ConvertLatexFragment(s.Substring(aStart, aEnd - aStart));
            string den = ConvertLatexFragment(s.Substring(bStart, bEnd - bStart));
            string frac = $"<sup>{num}</sup><size=78%>⁄</size><sub>{den}</sub>";
            s = s.Substring(0, f) + frac + s.Substring(afterB);
        }

        guard = 0;
        while (guard++ < 32)
        {
            int r = s.IndexOf(@"\sqrt", System.StringComparison.Ordinal);
            if (r < 0)
                break;
            if (r + 5 >= s.Length || s[r + 5] != '{')
                break;
            if (!TryParseBraced(s, r + 5, out int inS, out int inE, out int afterBrace))
                break;

            string inner = ConvertLatexFragment(s.Substring(inS, inE - inS));
            string rep = $"\u221A({inner})";
            s = s.Substring(0, r) + rep + s.Substring(afterBrace);
        }

        s = StripWrappers(s, @"\text");
        s = StripWrappers(s, @"\mathrm");
        s = StripWrappers(s, @"\operatorname");
        s = RemoveLeftRight(s);
        s = ReplaceCommandsLongestFirst(s, GreekAndSymbols);
        s = ReplaceSimpleCommands(s, @"\cdot", "·");
        s = ReplaceSimpleCommands(s, @"\times", "\u00D7");
        s = ReplaceSimpleCommands(s, @"\pm", "\u00B1");
        s = ReplaceSimpleCommands(s, @"\mp", "\u2213");
        s = ReplaceSimpleCommands(s, @"\leq", "\u2264");
        s = ReplaceSimpleCommands(s, @"\geq", "\u2265");
        s = ReplaceSimpleCommands(s, @"\neq", "\u2260");
        s = ReplaceSimpleCommands(s, @"\approx", "\u2248");
        s = ReplaceSimpleCommands(s, @"\propto", "\u221D");
        s = ReplaceSimpleCommands(s, @"\infty", "\u221E");
        s = ReplaceSimpleCommands(s, @"\partial", "\u2202");
        s = ReplaceSimpleCommands(s, @"\nabla", "\u2207");
        s = ReplaceSimpleCommands(s, @"\int", "\u222B");
        s = ReplaceSimpleCommands(s, @"\oint", "\u222E");
        s = ReplaceSimpleCommands(s, @"\sum", "\u2211");
        s = ReplaceSimpleCommands(s, @"\prod", "\u220F");
        s = ReplaceSimpleCommands(s, @"\ldots", "\u2026");
        s = ReplaceSimpleCommands(s, @"\cdots", "\u22EF");
        s = ReplaceSimpleCommands(s, @"\mapsto", "\u21A6");
        s = ReplaceSimpleCommands(s, @"\Rightarrow", "\u21D2");
        s = ReplaceSimpleCommands(s, @"\rightarrow", "\u2192");
        s = ReplaceSimpleCommands(s, @"\to", "\u2192");
        s = ReplaceSimpleCommands(s, @"\in", "\u2208");
        s = ReplaceSimpleCommands(s, @"\forall", "\u2200");
        s = ReplaceSimpleCommands(s, @"\exists", "\u2203");
        s = ReplaceSimpleCommands(s, @"\mathbb{R}", "\u211D");
        s = ReplaceSimpleCommands(s, @"\mathbb{C}", "\u2102");
        s = ReplaceSimpleCommands(s, @"\varepsilon", "\u03B5");
        s = ReplaceSimpleCommands(s, @"\Delta", "\u0394");

        s = ReplaceTrig(s, @"\sinh", "sinh");
        s = ReplaceTrig(s, @"\cosh", "cosh");
        s = ReplaceTrig(s, @"\tanh", "tanh");
        s = ReplaceTrig(s, @"\sin", "sin");
        s = ReplaceTrig(s, @"\cos", "cos");
        s = ReplaceTrig(s, @"\tan", "tan");
        s = ReplaceTrig(s, @"\cot", "cot");
        s = ReplaceTrig(s, @"\sec", "sec");
        s = ReplaceTrig(s, @"\csc", "csc");
        s = ReplaceTrig(s, @"\ln", "ln");
        s = ReplaceTrig(s, @"\log", "log");
        s = ReplaceTrig(s, @"\exp", "exp");
        s = ReplaceTrig(s, @"\max", "max");
        s = ReplaceTrig(s, @"\min", "min");

        s = ApplySubSup(s);
        s = CollapseGroupingBraces(s);
        return s;
    }

    private static string ReplaceCommandsLongestFirst(string s, Dictionary<string, string> map)
    {
        var keys = new List<string>(map.Keys);
        keys.Sort((a, b) => b.Length.CompareTo(a.Length));
        foreach (var k in keys)
            s = s.Replace(k, map[k]);
        return s;
    }

    private static string ReplaceSimpleCommands(string s, string cmd, string repl) => s.Replace(cmd, repl);

    private static string ReplaceTrig(string s, string cmd, string name) => s.Replace(cmd, $"<i>{name}</i>");

    private static readonly Dictionary<string, string> GreekAndSymbols = BuildGreek();

    private static Dictionary<string, string> BuildGreek()
    {
        return new Dictionary<string, string>
        {
            ["\\alpha"] = "\u03B1", ["\\beta"] = "\u03B2", ["\\gamma"] = "\u03B3", ["\\delta"] = "\u03B4",
            ["\\epsilon"] = "\u03B5", ["\\zeta"] = "\u03B6", ["\\eta"] = "\u03B7", ["\\theta"] = "\u03B8",
            ["\\iota"] = "\u03B9", ["\\kappa"] = "\u03BA", ["\\lambda"] = "\u03BB", ["\\mu"] = "\u03BC",
            ["\\nu"] = "\u03BD", ["\\xi"] = "\u03BE", ["\\pi"] = "\u03C0", ["\\rho"] = "\u03C1",
            ["\\sigma"] = "\u03C3", ["\\tau"] = "\u03C4", ["\\upsilon"] = "\u03C5", ["\\phi"] = "\u03C6",
            ["\\chi"] = "\u03C7", ["\\psi"] = "\u03C8", ["\\omega"] = "\u03C9",
            ["\\Gamma"] = "\u0393", ["\\Theta"] = "\u0398", ["\\Lambda"] = "\u039B", ["\\Xi"] = "\u039E",
            ["\\Pi"] = "\u03A0", ["\\Sigma"] = "\u03A3", ["\\Phi"] = "\u03A6", ["\\Psi"] = "\u03A8", ["\\Omega"] = "\u03A9"
        };
    }

    private static bool TryParseBraced(string s, int braceIdx, out int innerStart, out int innerEnd, out int afterClose)
    {
        innerStart = innerEnd = afterClose = braceIdx;
        if (braceIdx >= s.Length || s[braceIdx] != '{')
            return false;

        int depth = 1;
        int i = braceIdx + 1;
        innerStart = i;
        while (i < s.Length && depth > 0)
        {
            if (s[i] == '{')
                depth++;
            else if (s[i] == '}')
                depth--;
            i++;
        }

        if (depth != 0)
            return false;

        innerEnd = i - 1;
        afterClose = i;
        return true;
    }

    private static string StripWrappers(string s, string cmd)
    {
        int guard = 0;
        while (guard++ < 48)
        {
            int k = s.IndexOf(cmd, System.StringComparison.Ordinal);
            if (k < 0)
                break;
            int open = k + cmd.Length;
            if (open >= s.Length || s[open] != '{')
                break;
            if (!TryParseBraced(s, open, out int inS, out int inE, out int after))
                break;

            string inner = s.Substring(inS, inE - inS);
            s = s.Substring(0, k) + inner + s.Substring(after);
        }

        return s;
    }

    /// <summary>Remove \left( \right) style — keep inner parentheses only.</summary>
    private static string RemoveLeftRight(string s)
    {
        s = s.Replace(@"\left(", "(");
        s = s.Replace(@"\right)", ")");
        s = s.Replace(@"\left[", "[");
        s = s.Replace(@"\right]", "]");
        s = s.Replace(@"\left\{", "{");
        s = s.Replace(@"\right\}", "}");
        s = s.Replace(@"\middle|", "|");
        s = s.Replace(@"\left|", "|");
        s = s.Replace(@"\right|", "|");
        return s;
    }

    private static string ApplySubSup(string s)
    {
        int g = 0;
        while (g++ < 128)
        {
            int k = s.IndexOf("^{", System.StringComparison.Ordinal);
            if (k < 0)
                break;
            if (!TryParseBraced(s, k + 1, out int inS, out int inE, out int after))
                break;

            string inner = ConvertLatexFragment(s.Substring(inS, inE - inS));
            s = s.Substring(0, k) + "<sup>" + inner + "</sup>" + s.Substring(after);
        }

        g = 0;
        while (g++ < 128)
        {
            int k = s.IndexOf("_{", System.StringComparison.Ordinal);
            if (k < 0)
                break;
            if (!TryParseBraced(s, k + 1, out int inS, out int inE, out int after))
                break;

            string inner = ConvertLatexFragment(s.Substring(inS, inE - inS));
            s = s.Substring(0, k) + "<sub>" + inner + "</sub>" + s.Substring(after);
        }

        g = 0;
        while (g++ < 128)
        {
            int k = -1;
            for (int i = 0; i < s.Length - 1; i++)
            {
                if (s[i] == '^' && s[i + 1] != '{' && s[i + 1] != '(' && s[i + 1] != '\\')
                {
                    k = i;
                    break;
                }
            }

            if (k < 0)
                break;
            char ch = s[k + 1];
            s = s.Substring(0, k) + "<sup>" + ch + "</sup>" + s.Substring(k + 2);
        }

        g = 0;
        while (g++ < 128)
        {
            int k = -1;
            for (int i = 0; i < s.Length - 1; i++)
            {
                if (s[i] == '_' && s[i + 1] != '{' && s[i + 1] != '(' && s[i + 1] != '\\')
                {
                    k = i;
                    break;
                }
            }

            if (k < 0)
                break;
            char ch = s[k + 1];
            s = s.Substring(0, k) + "<sub>" + ch + "</sub>" + s.Substring(k + 2);
        }

        return s;
    }

    private static string CollapseGroupingBraces(string s)
    {
        int g = 0;
        while (g++ < 64)
        {
            int k = s.IndexOf('{', System.StringComparison.Ordinal);
            if (k < 0)
                break;
            if (!TryParseBraced(s, k, out int inS, out int inE, out int after))
                break;

            string inner = s.Substring(inS, inE - inS);
            if (inner.IndexOf('{', System.StringComparison.Ordinal) < 0)
                s = s.Substring(0, k) + inner + s.Substring(after);
            else
                break;
        }

        return s;
    }
}
