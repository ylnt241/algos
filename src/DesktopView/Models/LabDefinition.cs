using System.Collections.Generic;

namespace DesktopView.Models;

/// <summary>
/// Describes a laboratory work: a named collection of algorithms.
/// Populated dynamically by <see cref="DesktopView.Services.ILabDataSource"/>.
/// </summary>
public sealed class LabDefinition
{
    public string Id { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public IReadOnlyList<AlgorithmDefinition> Algorithms { get; init; } =
        System.Array.Empty<AlgorithmDefinition>();
}
