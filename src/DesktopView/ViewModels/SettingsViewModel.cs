using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopView.Models;
using DesktopView.Services;

namespace DesktopView.ViewModels;

/// <summary>ViewModel for the Settings view/dialog.</summary>
public partial class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;

    public SettingsViewModel() : this(ViewModelServices.SettingsService) { }

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        _working = new AppSettings();
        _persistTestData = _working.PersistTestData;
        _retentionPercent = _working.RetentionPercent;
        _enableCaching = _working.EnableCaching;
    }

    private AppSettings _working;

    /// <summary>Whether measured test data should persist between algorithm runs.</summary>
    [ObservableProperty]
    private bool _persistTestData;

    /// <summary>Retention period expressed as a data-ratio percentage (0–100).</summary>
    [ObservableProperty]
    private int _retentionPercent;

    /// <summary>Whether caching of benchmark results is enabled.</summary>
    [ObservableProperty]
    private bool _enableCaching;

    /// <summary>Loads persisted settings asynchronously without blocking the UI thread.</summary>
    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct)
    {
        var loaded = await _settingsService.LoadAsync(ct);
        _working = loaded;
        PersistTestData = loaded.PersistTestData;
        RetentionPercent = loaded.RetentionPercent;
        EnableCaching = loaded.EnableCaching;
    }

    /// <summary>Persists the current settings asynchronously.</summary>
    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync(CancellationToken ct)
    {
        _working.PersistTestData = PersistTestData;
        _working.RetentionPercent = RetentionPercent;
        _working.EnableCaching = EnableCaching;
        await _settingsService.SaveAsync(_working, ct);
        SaveCompleted?.Invoke(this, EventArgs.Empty);
    }

    private bool CanSave => true; // always saveable; validation can be added later

    /// <summary>Raised when settings have been successfully persisted.</summary>
    public event EventHandler? SaveCompleted;

    /// <summary>Raised when the user requests to close the settings dialog.</summary>
    public event EventHandler? Closed;

    /// <summary>Requests the host to close the embedded settings dialog.</summary>
    [RelayCommand]
    private void Close()
    {
        Closed?.Invoke(this, EventArgs.Empty);
    }
}
