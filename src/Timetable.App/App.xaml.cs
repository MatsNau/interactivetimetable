using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Timetable.Application.Workspaces;
using Timetable.App.Services;
using Timetable.App.ViewModels;
using Timetable.Infrastructure.Workspaces;

namespace Timetable.App;

/// <summary>
/// Einstiegspunkt: baut den Generic Host mit DI-Container auf und
/// zeigt das Hauptfenster. Alle Services und ViewModels werden hier registriert.
/// </summary>
public partial class App : System.Windows.Application
{
    private readonly IHost _host;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(ConfigureServices)
            .Build();
    }

    private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        services.AddSingleton<SettingsStore>();
        services.AddSingleton<IPlanWorkspaceFactory, PlanWorkspaceFactory>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.StartAsync();
        _host.Services.GetRequiredService<MainWindow>().Show();
        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        // Anwesenheit sauber zurückziehen, bevor der Container herunterfährt.
        if (_host.Services.GetService<MainViewModel>() is { } viewModel)
            await viewModel.DisposeAsync();

        await _host.StopAsync();
        _host.Dispose();
        base.OnExit(e);
    }
}
