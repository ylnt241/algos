using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using DesktopView.ViewModels;
using ScottPlot;
using ScottPlot.Avalonia;

namespace DesktopView.Views;

/// <summary>
/// Algorithm Execution view. Hosts the ScottPlot.Avalonia chart and redraws it whenever the
/// view model signals that a benchmark run has completed.
/// </summary>
public partial class AlgorithmExecutionView : UserControl
{
    private AvaPlot? _plot;

    public AlgorithmExecutionView()
    {
        InitializeComponent();
        _plot = this.FindControl<AvaPlot>("PlotControl");

        // Render an empty plot once the control is attached to the visual tree and has a
        // real size — calling Refresh() before this point renders into a 0x0 surface.
        Loaded += OnLoaded;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnLoaded(object? sender, System.EventArgs e)
    {
        if (_plot is null)
        {
            return;
        }

        // Ensure the plot surface has a title even before the first run, and force a render
        // now that the control is laid out. ScottPlot.Avalonia needs Refresh() after the
        // control has a non-zero size, so we post the refresh past layout.
        Dispatcher.UIThread.Post(() =>
        {
            _plot.Plot.Clear();
            _plot.Plot.Title("Press 'Run benchmark' to plot");
            _plot.Plot.XLabel("n (input size)");
            _plot.Plot.YLabel("Average time (ms)");
            _plot.Refresh();
        }, DispatcherPriority.Loaded);
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is AlgorithmExecutionViewModel vm)
        {
            vm.RunCompleted -= OnRunCompleted;
            vm.RunCompleted += OnRunCompleted;
        }
    }

    private void OnRunCompleted(object? sender, System.EventArgs e)
    {
        // The benchmark runs asynchronously; marshal the redraw back onto the UI thread so
        // ScottPlot renders into the live control surface.
        Dispatcher.UIThread.Post(RenderChart, DispatcherPriority.Normal);
    }

    private void RenderChart()
    {
        if (_plot is null || DataContext is not AlgorithmExecutionViewModel vm)
        {
            return;
        }

        var plt = _plot.Plot;
        plt.Clear();

        if (vm.Samples.Count == 0)
        {
            plt.Title("No data");
            _plot.Refresh();
            return;
        }

        var ns = vm.Samples.Select(s => (double)s.N).ToArray();
        var measured = vm.Samples.Select(s => s.AverageMilliseconds).ToArray();

        // Measured average execution time vs n.
        var measuredPlot = plt.Add.Scatter(ns, measured);
        measuredPlot.LegendText = "Measured avg time (ms)";
        measuredPlot.LineWidth = 2;
        measuredPlot.MarkerSize = 6;
        measuredPlot.Color = ScottPlot.Colors.Blue;

        // Theoretical complexity curve (scaled to the measured range) vs n.
        if (vm.TheoreticalCurve.Count > 0)
        {
            var tNs = vm.TheoreticalCurve.Select(p => p.N).ToArray();
            var tVals = vm.TheoreticalCurve.Select(p => p.Theoretical).ToArray();
            var theoreticalPlot = plt.Add.Scatter(tNs, tVals);
            theoreticalPlot.LegendText = $"Theoretical {vm.TheoreticalComplexity} (scaled)";
            theoreticalPlot.LineWidth = 2;
            theoreticalPlot.MarkerSize = 4;
            theoreticalPlot.Color = ScottPlot.Colors.Red;
            theoreticalPlot.LinePattern = ScottPlot.LinePattern.Dashed;
        }

        plt.Title($"Algorithm execution analysis — {vm.AlgorithmTitle}");
        plt.XLabel("n (input size)");
        plt.YLabel("Average time (ms)");
        plt.ShowLegend();
        _plot.Refresh();
    }
}
