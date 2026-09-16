using System;
using System.Collections.Generic;

namespace DesktopView.Services;

/// <summary>
/// Parses symbolic big-O complexity strings into concrete functions of <c>n</c>.
/// This keeps the lab data source free of any hardcoding: each lab simply declares a
/// complexity symbol and the parser turns it into a curve.
/// </summary>
public static class ComplexityParser
{
    /// <summary>Maps a normalized symbol (without whitespace, lowercased) to a function of n.</summary>
    private static readonly Dictionary<string, Func<double, double>> _table = new(StringComparer.OrdinalIgnoreCase)
    {
        ["o(1)"]       = _ => 1,
        ["o(logn)"]    = n => Math.Log2(Math.Max(1, n)),
        ["o(log(n))"]  = n => Math.Log2(Math.Max(1, n)),
        ["o(n)"]       = n => n,
        ["o(nlogn)"]   = n => n * Math.Log2(Math.Max(1, n)),
        ["o(nlog(n))"] = n => n * Math.Log2(Math.Max(1, n)),
        ["o(n^2)"]     = n => n * n,
        ["o(n^3)"]     = n => n * n * n,
        ["o(2^n)"]     = n => Math.Pow(2, Math.Min(n, 30)), // capped to avoid overflow
        ["o(n!)"]      = n => Factorial((int)Math.Min(n, 20)),
    };

    /// <summary>Resolves a symbolic complexity string to a function of <c>n</c>.
    /// Falls back to <c>O(n)</c> when the symbol is unknown.</summary>
    public static Func<double, double> Resolve(string? symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            return n => n;
        }

        var normalized = symbol.Trim();
        if (_table.TryGetValue(normalized, out var f))
        {
            return f;
        }

        // Collapse any inner whitespace before a second lookup.
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s+", "");
        return _table.TryGetValue(normalized, out f) ? f : (n => n);
    }

    private static double Factorial(int n)
    {
        double r = 1;
        for (int i = 2; i <= n; i++)
        {
            r *= i;
        }
        return r;
    }
}
