using DesktopView.Services;

namespace DesktopView.ViewModels;

/// <summary>
/// Tiny composition root for view models. Resolves the application services once and shares them
/// across view models so the views stay free of service construction logic.
/// </summary>
public static class ViewModelServices
{
    public static ILabDataSource LabDataSource { get; } = new JsonLabDataSource();
    public static ISettingsService SettingsService { get; } = new SettingsService();
    public static IBenchmarkService BenchmarkService { get; } = new BenchmarkService();
}
