using Avalonia.Controls;
using DesktopView.ViewModels;

namespace DesktopView.Views;

/// <summary>
/// Modal dialog window that hosts the embedded <c>SettingsView</c> panel. It wires the shared
/// <see cref="SettingsViewModel"/> events (close requested / settings saved) to the window's
/// own lifecycle so the dialog actually appears and can be dismissed by the user.
/// </summary>
public partial class SettingsDialog : Window
{
    public SettingsDialog()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is not SettingsViewModel vm)
        {
            return;
        }

        // Close the dialog when the view model signals a close request (Close button).
        vm.Closed -= OnViewModelClosed;
        vm.Closed += OnViewModelClosed;

        // Close the dialog after a successful save.
        vm.SaveCompleted -= OnSaveCompleted;
        vm.SaveCompleted += OnSaveCompleted;
    }

    private void OnViewModelClosed(object? sender, System.EventArgs e)
    {
        Close();
    }

    private void OnSaveCompleted(object? sender, System.EventArgs e)
    {
        Close();
    }
}