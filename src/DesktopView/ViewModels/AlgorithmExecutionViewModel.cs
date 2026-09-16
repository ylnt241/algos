using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopView.Models;
using DesktopView.Services;

namespace DesktopView.ViewModels;

/// <summary>
/// ViewModel backing the Algorithm Execution view. Drives a benchmark over a range of <c>n</c>
/// and exposes the measured average time and the theoretical complexity curve for plotting with
/// ScottPlot.Avalonia.
/// </summary>
public partial class AlgorithmExecutionViewModel : ViewModelBase
{
    private readonly ILabDataSource _labDataSource;
    private readonly IBenchmarkService _benchmarkService;
    private readonly ISettingsService _settingsService;
    private readonly Func<string?, Func<double, double>> _complexityResolver;

    public AlgorithmExecutionViewModel()
        : this(ViewModelServices.LabDataSource,
               ViewModelServices.BenchmarkService,
               ViewModelServices.SettingsService,
               ComplexityParser.Resolve)
    {
    }

    public AlgorithmExecutionViewModel(
        ILabDataSource labDataSource,
        IBenchmarkService benchmarkService,
        ISettingsService settingsService,
        Func<string?, Func<double, double>>? complexityResolver = null)
    {
        _labDataSource = labDataSource;
        _benchmarkService = benchmarkService;
        _settingsService = settingsService;
        _complexityResolver = complexityResolver ?? ComplexityParser.Resolve;
    }

    /// <summary>Selected lab id (set by the main view before navigation).</summary>
    [ObservableProperty]
    private string? _labId;

    /// <summary>Selected algorithm id (set by the main view before navigation).</summary>
    [ObservableProperty]
    private string? _algorithmId;

    /// <summary>Display title for the selected algorithm.</summary>
    [ObservableProperty]
    private string _algorithmTitle = string.Empty;

    /// <summary>Symbolic theoretical complexity of the selected algorithm.</summary>
    [ObservableProperty]
    private string _theoreticalComplexity = "O(n)";

    /// <summary>Status text shown while running or after completion.</summary>
    [ObservableProperty]
    private string _status = "Ready";

    /// <summary>Whether a benchmark is currently running.</summary>
    [ObservableProperty]
    private bool _isRunning;

    /// <summary>Re-evaluates whether Run can execute when running state changes.</summary>
    partial void OnIsRunningChanged(bool value)
    {
        RunCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Measured benchmark samples (avg ms per n).</summary>
    public ObservableCollection<BenchmarkSample> Samples { get; } = new();

    /// <summary>Theoretical complexity curve points (n, complexity(n)) paired with the
    /// measured samples so the view can overlay them on the same chart.</summary>
    public List<(double N, double Theoretical)> TheoreticalCurve { get; } = new();

    /// <summary>Configures the run for the selected lab/algorithm. Resolves the algorithm and
    /// its theoretical complexity from the data source asynchronously.</summary>
    [RelayCommand]
    private async Task ConfigureAsync(CancellationToken ct)
    {
        if (string.IsNullOrEmpty(LabId) || string.IsNullOrEmpty(AlgorithmId))
        {
            return;
        }

        var labs = await _labDataSource.GetLabsAsync(ct);
        var lab = labs.FirstOrDefault(l => l.Id == LabId);
        var algo = lab?.Algorithms.FirstOrDefault(a => a.Id == AlgorithmId);
        if (algo is null)
        {
            Status = "Algorithm not found.";
            return;
        }

        AlgorithmTitle = algo.DisplayName;
        TheoreticalComplexity = algo.TheoreticalComplexity;
        Status = $"Configured for {algo.DisplayName} ({algo.TheoreticalComplexity}).";
    }

    /// <summary>Runs the benchmark over a fixed range of <c>n</c> and refreshes the chart data.</summary>
    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task RunAsync(CancellationToken ct)
    {
        if (string.IsNullOrEmpty(LabId) || string.IsNullOrEmpty(AlgorithmId))
        {
            Status = "Select a lab and an algorithm first.";
            return;
        }

        var settings = await _settingsService.LoadAsync(ct);
        var labs = await _labDataSource.GetLabsAsync(ct);
        var lab = labs.FirstOrDefault(l => l.Id == LabId);
        var algo = lab?.Algorithms.FirstOrDefault(a => a.Id == AlgorithmId);
        if (algo is null)
        {
            Status = "Algorithm not found.";
            return;
        }

        AlgorithmTitle = algo.DisplayName;
        TheoreticalComplexity = algo.TheoreticalComplexity;

        IsRunning = true;
        Status = "Running benchmark…";
        try
        {
            // Range chosen so that the largest supported complexity (O(n^3)) stays within the
            // benchmark service's internal work cap, keeping the measured curve well-shaped.
            const int minN = 200;
            const int maxN = 2_000;
            const int step = 200;
            const int repeats = 5;

            var samples = await _benchmarkService.RunAsync(
                algo, minN, maxN, step, repeats, settings.EnableCaching, ct);

            Samples.Clear();
            TheoreticalCurve.Clear();
            foreach (var s in samples)
            {
                Samples.Add(s);
            }

            // Build the theoretical curve on the same n-grid and normalize it so it can be
            // overlaid with the measured curve on one chart.
            var resolver = _complexityResolver(algo.TheoreticalComplexity);
            double maxTheoretical = 0;
            double maxMeasured = 0;
            foreach (var s in samples)
            {
                var t = resolver(s.N);
                TheoreticalCurve.Add((s.N, t));
                if (t > maxTheoretical) maxTheoretical = t;
                if (s.AverageMilliseconds > maxMeasured) maxMeasured = s.AverageMilliseconds;
            }

            // Re-scale the theoretical curve to the measured range so both are visible together.
            if (maxTheoretical > 0 && maxMeasured > 0)
            {
                var scale = maxMeasured / maxTheoretical;
                for (int i = 0; i < TheoreticalCurve.Count; i++)
                {
                    var (n, t) = TheoreticalCurve[i];
                    TheoreticalCurve[i] = (n, t * scale);
                }
            }

            Status = $"Done — {samples.Count} samples.";
            RunCompleted?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException)
        {
            Status = "Cancelled.";
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
        }
        finally
        {
            IsRunning = false;
        }
    }

    private bool CanRun => !IsRunning;

    /// <summary>Raised when a benchmark run finishes so the view can redraw the chart.</summary>
    public event System.EventHandler? RunCompleted;

    /// <summary>Requests the host to return to the main selection view.</summary>
    public event System.EventHandler? Closed;

    /// <summary>Requests the host to return to the main selection view.</summary>
    [RelayCommand]
    private void Back()
    {
        Closed?.Invoke(this, EventArgs.Empty);
    }
}
