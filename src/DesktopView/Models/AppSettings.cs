namespace DesktopView.Models;

/// <summary>
/// User-facing application settings. Persisted by <see cref="DesktopView.Services.ISettingsService"/>.
/// </summary>
public sealed class AppSettings
{
    /// <summary>Whether measured benchmark/test data should be kept between algorithm runs.</summary>
    public bool PersistTestData { get; set; } = false;

    /// <summary>
    /// Retention period expressed as a data-ratio percentage (0–100). When
    /// <see cref="PersistTestData"/> is enabled, only this fraction of the accumulated samples
    /// are retained between runs.
    /// </summary>
    public int RetentionPercent { get; set; } = 50;

    /// <summary>Whether caching of benchmark results is enabled.</summary>
    public bool EnableCaching { get; set; } = true;
}
