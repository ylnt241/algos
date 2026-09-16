using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopView.Models;
using DesktopView.Services;

namespace DesktopView.ViewModels;

/// <summary>
/// Main window view model. Hosts the lab/algorithm selection (two ComboBoxes), the Start command
/// that navigates to the Algorithm Execution view, and the Open Settings command that surfaces an
/// embedded settings view/dialog.
/// </summary>
public partial class MainViewModel : ViewModelBase
{
    private readonly ILabDataSource _labDataSource;

    public MainViewModel() : this(ViewModelServices.LabDataSource) { }

    public MainViewModel(ILabDataSource labDataSource)
    {
        _labDataSource = labDataSource;
        _ = LoadLabsAsync(default);
    }

    /// <summary>Available labs (first ComboBox).</summary>
    public ObservableCollection<LabDefinition> Labs { get; } = new();

    /// <summary>Algorithms available for the currently selected lab (second ComboBox).</summary>
    public ObservableCollection<AlgorithmDefinition> Algorithms { get; } = new();

    /// <summary>Currently selected lab.</summary>
    [ObservableProperty]
    private LabDefinition? _selectedLab;

    /// <summary>Currently selected algorithm.</summary>
    [ObservableProperty]
    private AlgorithmDefinition? _selectedAlgorithm;

    /// <summary>Whether the lab catalog is still loading.</summary>
    [ObservableProperty]
    private bool _isLoadingLabs;

    /// <summary>ViewModel for the embedded settings view/dialog.</summary>
    [ObservableProperty]
    private SettingsViewModel? _settings;

    /// <summary>ViewModel for the algorithm execution view, created on Start.</summary>
    [ObservableProperty]
    private AlgorithmExecutionViewModel? _execution;

    /// <summary>True while the execution view is being shown (drives content visibility).</summary>
    [ObservableProperty]
    private bool _isExecutionVisible;

    /// <summary>Refreshes the algorithms list when the selected lab changes.</summary>
    partial void OnSelectedLabChanged(LabDefinition? value)
    {
        Algorithms.Clear();
        SelectedAlgorithm = null;
        if (value is not null)
        {
            foreach (var a in value.Algorithms)
            {
                Algorithms.Add(a);
            }
        }

        StartCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Re-evaluates whether Start can execute when the algorithm changes.</summary>
    partial void OnSelectedAlgorithmChanged(AlgorithmDefinition? value)
    {
        StartCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Loads the lab catalog from the data source asynchronously.</summary>
    [RelayCommand]
    private async Task LoadLabsAsync(CancellationToken ct)
    {
        IsLoadingLabs = true;
        try
        {
            var labs = await _labDataSource.GetLabsAsync(ct);
            Labs.Clear();
            foreach (var lab in labs)
            {
                Labs.Add(lab);
            }

            if (Labs.Count > 0)
            {
                SelectedLab = Labs[0];
            }
        }
        finally
        {
            IsLoadingLabs = false;
        }
    }

    /// <summary>Opens the embedded settings view (creates the settings VM lazily).</summary>
    [RelayCommand]
    private async Task OpenSettingsAsync(CancellationToken ct)
    {
        if (Settings is null)
        {
            Settings = new SettingsViewModel();
            Settings.Closed += (_, _) => Settings = null;
        }

        await Settings.LoadCommand.ExecuteAsync(ct);
    }

    /// <summary>Closes the embedded settings view.</summary>
    [RelayCommand]
    private void CloseSettings()
    {
        Settings = null;
    }

    /// <summary>Navigates to the Algorithm Execution view for the selected lab/algorithm.</summary>
    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task StartAsync(CancellationToken ct)
    {
        if (SelectedLab is null || SelectedAlgorithm is null)
        {
            return;
        }

        Execution = new AlgorithmExecutionViewModel
        {
            LabId = SelectedLab.Id,
            AlgorithmId = SelectedAlgorithm.Id,
        };
        await Execution.ConfigureCommand.ExecuteAsync(ct);
        IsExecutionVisible = true;
    }

    /// <summary>Returns from the Algorithm Execution view to the main selection.</summary>
    [RelayCommand]
    private void Back()
    {
        IsExecutionVisible = false;
        Execution = null;
    }

    private bool CanStart => SelectedLab is not null && SelectedAlgorithm is not null;
}
