using Avalonia.Controls;
using DesktopView.ViewModels;

namespace DesktopView.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }

        // When a new execution VM is created, subscribe to its Closed event so we can return
        // to the main selection view without coupling the VMs to each other.
        vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(MainViewModel.Execution) && vm.Execution is { } exec)
            {
                exec.Closed += (_, _) =>
                {
                    vm.BackCommand.Execute(null);
                };
            }
        };
    }
}
