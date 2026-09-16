using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;

namespace DesktopView;

sealed class Program
{
    private static StreamWriter? _log;

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // Surface otherwise-silent runtime exceptions (e.g. ScottPlot GL/render errors) and
        // route them to a log file next to the executable so blank-screen issues are diagnosable.
        _log = new StreamWriter(Path.Combine(AppContext.BaseDirectory, "runtime.log"), append: false)
        {
            AutoFlush = true,
        };

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            _log.WriteLine($"[AppDomain.UnhandledException] {e.ExceptionObject}");
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            _log.WriteLine($"[UnobservedTaskException] {e.Exception}");
            e.SetObserved();
        };

        try
        {
            BuildAvaloniaApp()
                .AfterSetup(_ => _log.WriteLine("Avalonia setup completed."))
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            _log.WriteLine($"[Fatal] {ex}");
            throw;
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}