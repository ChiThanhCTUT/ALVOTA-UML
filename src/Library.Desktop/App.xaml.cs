using System.Windows;
using Library.Application;
using Library.Desktop.Services;
using Library.Desktop.ViewModels;
using Library.Infrastructure;
using Library.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Library.Desktop;

public partial class App : System.Windows.Application
{
    private static readonly string LogPath = FindSolutionOrBaseFile("app_log.txt");
    private readonly IHost _host;

    private static string FindSolutionOrBaseFile(string fileName)
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var dir = new System.IO.DirectoryInfo(baseDir);
        for (int i = 0; i < 6 && dir != null; i++)
        {
            var slnCandidate = System.IO.Path.Combine(dir.FullName, "Library.sln");
            if (System.IO.File.Exists(slnCandidate))
            {
                return System.IO.Path.Combine(dir.FullName, fileName);
            }
            dir = dir.Parent;
        }
        return System.IO.Path.Combine(baseDir, fileName);
    }

    private static string FindDatabasePath()
    {
        var solutionDb = FindSolutionOrBaseFile("library.db");
        if (System.IO.File.Exists(solutionDb))
        {
            return solutionDb;
        }

        var localDb = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "library.db");
        if (System.IO.File.Exists(localDb))
        {
            return localDb;
        }

        return solutionDb;
    }

    public App()
    {
        try
        {
            System.IO.File.WriteAllText(LogPath, "0. App constructor started\n");
            DispatcherUnhandledException += (s, ev) =>
            {
                System.IO.File.AppendAllText(LogPath, "DISPATCHER UNHANDLED: " + ev.Exception.ToString() + "\n");
            };
            AppDomain.CurrentDomain.UnhandledException += (s, ev) =>
            {
                System.IO.File.AppendAllText(LogPath, "UNHANDLED: " + ev.ExceptionObject.ToString() + "\n");
            };

            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Core Clean Architecture Layers
                    services.AddApplication();
                    var dbPath = FindDatabasePath();
                    services.AddInfrastructure($"Data Source={dbPath}");

                    // Scoped Mediator per request to isolate LibraryDbContext lifetime
                    services.AddSingleton<IMediator, ScopedMediator>();
                    services.AddSingleton<ISender>(sp => sp.GetRequiredService<IMediator>());
                    services.AddSingleton<IPublisher>(sp => sp.GetRequiredService<IMediator>());

                    // Desktop Services
                services.AddSingleton<IDialogService, DialogService>();
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<IAudioService, AudioService>();

                // ViewModels
                services.AddSingleton<MainViewModel>();
                services.AddTransient<LoginViewModel>();
                services.AddTransient<DashboardViewModel>();
                services.AddTransient<CirculationViewModel>();
                services.AddTransient<CatalogViewModel>();
                services.AddTransient<MemberViewModel>();
                services.AddTransient<FineViewModel>();
                services.AddTransient<InventoryViewModel>();
                services.AddTransient<ReportViewModel>();
                services.AddTransient<SystemViewModel>();

                // Windows
                services.AddSingleton<MainWindow>();
            })
            .Build();
            System.IO.File.AppendAllText(LogPath, "0.1 Host built successfully\n");
        }
        catch (Exception ex)
        {
            System.IO.File.AppendAllText(LogPath, "CONSTRUCTOR EXCEPTION: " + ex.ToString() + "\n");
            throw;
        }
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        try
        {
            System.IO.File.AppendAllText(LogPath, "1. OnStartup started\n");
            base.OnStartup(e);

            ShutdownMode = ShutdownMode.OnMainWindowClose;

            System.IO.File.AppendAllText(LogPath, "2. Starting host\n");
            await _host.StartAsync();

            System.IO.File.AppendAllText(LogPath, "3. Initializing DB\n");
            await using (var scope = _host.Services.CreateAsyncScope())
            {
                var dbInitializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
                await dbInitializer.InitializeAsync();
            }

            System.IO.File.AppendAllText(LogPath, "4. Resolving MainWindow\n");
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();

            mainWindow.Loaded += (s, ev) => System.IO.File.AppendAllText(LogPath, "MainWindow Loaded\n");
            mainWindow.ContentRendered += (s, ev) => System.IO.File.AppendAllText(LogPath, "MainWindow ContentRendered\n");
            mainWindow.IsVisibleChanged += (s, ev) => System.IO.File.AppendAllText(LogPath, $"MainWindow IsVisibleChanged: {mainWindow.IsVisible}\n");
            mainWindow.Closing += (s, ev) => System.IO.File.AppendAllText(LogPath, $"MainWindow Closing: Cancel={ev.Cancel}\n");
            mainWindow.Closed += (s, ev) => System.IO.File.AppendAllText(LogPath, "MainWindow Closed!\n");

            System.IO.File.AppendAllText(LogPath, "5. Setting MainWindow and Showing\n");
            MainWindow = mainWindow;
            mainWindow.WindowState = WindowState.Normal;
            mainWindow.Show();
            mainWindow.Activate();
            mainWindow.Topmost = true;
            mainWindow.Topmost = false;
            mainWindow.Focus();
            var handle = new System.Windows.Interop.WindowInteropHelper(mainWindow).Handle;
            System.IO.File.AppendAllText(LogPath, $"6. MainWindow.Show completed, HWND={handle}\n");
        }
        catch (Exception ex)
        {
            System.IO.File.AppendAllText(LogPath, "EXCEPTION: " + ex.ToString() + "\n");
            throw;
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        System.IO.File.AppendAllText(LogPath, $"OnExit called. ExitCode={e.ApplicationExitCode}\n");
        await _host.StopAsync();
        _host.Dispose();
        base.OnExit(e);
    }
}
