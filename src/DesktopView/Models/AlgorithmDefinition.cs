using System.Collections.Generic;

namespace DesktopView.Models;

/// <summary>
/// Describes an algorithm exposed by a lab. The <see cref="TheoreticalComplexity"/> string is
/// interpreted by <see cref="DesktopView.Services.ComplexityParser"/> to obtain a concrete
/// function of <c>n</c>, so any lab can supply its own complexity without hardcoding.
/// </summary>
public sealed class AlgorithmDefinition
{
    public string Id { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    /// <summary>One of the symbolic identifiers supported by <see cref="Services.ComplexityParser"/>,
    /// e.g. <c>O(1)</c>, <c>O(log n)</c>, <c>O(n)</c>, <c>O(n log n)</c>, <c>O(n^2)</c>, <c>O(n^3)</c>.</summary>
    public string TheoreticalComplexity { get; init; } = "O(n)";

    /// <summary>Display-friendly short description of the algorithm.</summary>
    public string Description { get; init; } = string.Empty;
}
