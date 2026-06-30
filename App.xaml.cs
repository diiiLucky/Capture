using System.Windows;
using CodexCapture.Services;

namespace CodexCapture;

public partial class App : System.Windows.Application
{
    private AppController? _controller;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        _controller = new AppController();
        await _controller.StartAsync(e.Args.Contains("--minimized", StringComparer.OrdinalIgnoreCase));
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_controller is not null)
        {
            await _controller.DisposeAsync();
        }

        base.OnExit(e);
    }
}
