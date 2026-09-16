using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DesktopView.Models;

namespace DesktopView.Services;

/// <summary>Provides the set of available labs and their algorithms at runtime.</summary>
public interface ILabDataSource
{
    /// <summary>Asynchronously loads the available labs. Implementations are expected to parse
    /// an external data source rather than hardcoding specific implementations.</summary>
    Task<IReadOnlyList<LabDefinition>> GetLabsAsync(CancellationToken ct = default);
}
