using System.Windows;
using Library.Application;
using Library.Desktop.Services;
using Library.Desktop.ViewModels;
using Library.Infrastructure;
using Library.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Library.Desktop;

public partial class App : System.Windows.Application
{
    private const string LogPath = @"C:\Users\Vinh\Downloads\QUAN_LY_THU_VIEN\app_log.txt";
    private readonly IHost _host;

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
                    var workspaceDb = @"C:\Users\Vinh\Downloads\QUAN_LY_THU_VIEN\library.db";
                    var dbPath = System.IO.File.Exists(workspaceDb)
                        ? workspaceDb
                        : System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "library.db");
                    services.AddInfrastructure($"Data Source={dbPath}");

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

    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            System.IO.File.AppendAllText(LogPath, "1. OnStartup started\n");
            base.OnStartup(e);

            ShutdownMode = ShutdownMode.OnMainWindowClose;

            System.IO.File.AppendAllText(LogPath, "2. Starting host\n");
            _host.Start();

            System.IO.File.AppendAllText(LogPath, "3. Initializing DB\n");
            using (var scope = _host.Services.CreateScope())
            {
                var dbInitializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
                dbInitializer.InitializeAsync().GetAwaiter().GetResult();
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

    protected override void OnExit(ExitEventArgs e)
    {
        System.IO.File.AppendAllText(LogPath, $"OnExit called. ExitCode={e.ApplicationExitCode}\n");
        _host.StopAsync().GetAwaiter().GetResult();
        _host.Dispose();
        base.OnExit(e);
    }
}
