using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using DesktopView.ViewModels;

namespace DesktopView;

/// <summary>
/// Given a view model, returns the corresponding view if possible.
/// </summary>
[RequiresUnreferencedCode(
    "Default implementation of ViewLocator involves reflection which may be trimmed away.",
    Url = "https://docs.avaloniaui.net/docs/concepts/view-locator")]
public class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is null)
            return null;

        var type = param.GetType();
        var full = type.FullName!;
        // View models live in DesktopView.ViewModels; their views live in DesktopView.Views.
        // Map the namespace and strip the "Model" suffix from the type name, then resolve the
        // view type using its assembly-qualified name so Type.GetType always succeeds.
        var viewName = full
            .Replace("DesktopView.ViewModels", "DesktopView.Views", StringComparison.Ordinal)
            .Replace("ViewModel", "View", StringComparison.Ordinal);
        var qualified = $"{viewName}, {type.Assembly.GetName().Name}";
        var viewType = Type.GetType(qualified);

        if (viewType != null)
        {
            return (Control)Activator.CreateInstance(viewType)!;
        }

        return new TextBlock { Text = "Not Found: " + viewName };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}