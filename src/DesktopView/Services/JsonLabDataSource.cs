using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DesktopView.Models;

namespace DesktopView.Services;

/// <summary>
/// Default <see cref="ILabDataSource"/> that parses the available labs/algorithms from a JSON
/// document at runtime. The JSON is located next to the application (or an override path) and is
/// not hardcoded in source — the structure is described by <see cref="LabCatalogDocument"/>.
/// </summary>
public sealed class JsonLabDataSource : ILabDataSource
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly string _sourcePath;

    public JsonLabDataSource(string? sourcePath = null)
    {
        _sourcePath = sourcePath ?? ResolveDefaultPath();
    }

    public async Task<IReadOnlyList<LabDefinition>> GetLabsAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_sourcePath))
        {
            return Array.Empty<LabDefinition>();
        }

        await using var stream = File.OpenRead(_sourcePath);
        var doc = await JsonSerializer.DeserializeAsync<LabCatalogDocument>(stream, JsonOptions, ct);
        if (doc is null || doc.Labs is null)
        {
            return Array.Empty<LabDefinition>();
        }

        var labs = new List<LabDefinition>(doc.Labs.Count);
        foreach (var lab in doc.Labs)
        {
            if (string.IsNullOrWhiteSpace(lab.Id))
            {
                continue;
            }

            labs.Add(new LabDefinition
            {
                Id = lab.Id!,
                DisplayName = lab.DisplayName ?? lab.Id!,
                Algorithms = (IReadOnlyList<AlgorithmDefinition>?)lab.Algorithms
                              ?? Array.Empty<AlgorithmDefinition>(),
            });
        }

        return labs;
    }

    private static string ResolveDefaultPath()
    {
        var dir = AppContext.BaseDirectory;
        return Path.Combine(dir, "Assets", "labs.json");
    }
}

// JSON DTOs — mirror of the on-disk catalog document.
internal sealed class LabCatalogDocument
{
    public List<LabDto>? Labs { get; set; }
}

internal sealed class LabDto
{
    public string? Id { get; set; }
    public string? DisplayName { get; set; }
    public List<AlgorithmDefinition>? Algorithms { get; set; }
}
