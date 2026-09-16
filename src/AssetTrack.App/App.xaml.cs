using System.IO;
using System.Windows;
using System.Windows.Threading;
using AssetTrack.App.Navigation;
using AssetTrack.App.ViewModels;
using AssetTrack.App.Views;
using AssetTrack.Core.Abstractions;
using AssetTrack.Core.Services;
using AssetTrack.Data;
using AssetTrack.Data.Seeding;
using AssetTrack.Data.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AssetTrack.App;

public partial class App : Application
{
    private IServiceProvider _services = null!;
    private bool _isNavigatingAway;

    /// <summary>Pragmatic escape hatch for leaf controls (e.g. SearchableDropdown) that need a
    /// service but aren't worth threading a dedicated ViewModel/command through every call site.</summary>
    public static IServiceProvider Services { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        _services = BuildServiceProvider();
        Services = _services;

        try
        {
            var dbFactory = _services.GetRequiredService<IDbContextFactory<AssetTrackDbContext>>();
            await using var db = await dbFactory.CreateDbContextAsync();
            await db.Database.MigrateAsync();
            await DbSeeder.SeedAsync(db);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"AssetTrack could not prepare its local database and cannot continue.\n\n{ex.Message}",
                "AssetTrack — Startup error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(-1);
            return;
        }

        ShowLoginWindow();
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            $"Something went wrong and the last action could not be completed.\n\n{e.Exception.Message}",
            "AssetTrack — Unexpected error", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        // A background load that failed without anyone awaiting it. We can't show it in the
        // UI it belonged to, but we must observe it so the process doesn't tear down.
        e.SetObserved();
    }

    private static IServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        var dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AssetTrack");
        Directory.CreateDirectory(dataDir);
        var dbPath = Path.Combine(dataDir, "assettrack.db");

        // A factory, not a shared instance: WPF ViewModels live for a whole sign-in session,
        // so a single shared DbContext would be used concurrently (search-as-you-type, a
        // dialog loading while a page is still loading) and EF Core does not allow that.
        // Each service call below creates and disposes its own short-lived context.
        services.AddDbContextFactory<AssetTrackDbContext>(options => options.UseSqlite($"Data Source={dbPath}"));

        services.AddSingleton<ISessionContext, SessionContext>();
        services.AddScoped<IAuthService, EfAuthService>();
        services.AddScoped<IAssetService, EfAssetService>();
        services.AddScoped<IUserService, EfUserService>();
        services.AddScoped<IActivityLogService, EfActivityLogService>();
        services.AddScoped<ILookupService, EfLookupService>();

        // Scoped (not singleton): each sign-in creates a new DI scope for the shell session,
        // so navigation/dialog state is tied to that session.
        services.AddScoped<INavigationService, NavigationService>();
        services.AddScoped<IDialogService, DialogService>();

        services.AddTransient<LoginViewModel>();
        services.AddTransient<ShellViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<AssetsListViewModel>();
        services.AddTransient<AssetDetailViewModel>();
        services.AddTransient<UsersListViewModel>();
        services.AddTransient<UserDetailViewModel>();
        services.AddTransient<ActivityLogViewModel>();
        services.AddTransient<AllocateAssetDialogViewModel>();
        services.AddTransient<ReturnAssetDialogViewModel>();
        services.AddTransient<AddAssetDialogViewModel>();
        services.AddTransient<AddUserDialogViewModel>();
        services.AddTransient<EditAssetDialogViewModel>();
        services.AddTransient<EditUserDialogViewModel>();
        services.AddTransient<MarkScrapDialogViewModel>();

        return services.BuildServiceProvider();
    }

    private void ShowLoginWindow()
    {
        var scope = _services.CreateScope();
        var loginViewModel = scope.ServiceProvider.GetRequiredService<LoginViewModel>();
        var loginWindow = new LoginWindow(loginViewModel);

        loginViewModel.SignedIn += _ =>
        {
            _isNavigatingAway = true;
            ShowShellWindow();
            loginWindow.Close();
        };

        loginWindow.Closed += (_, _) =>
        {
            scope.Dispose();
            if (!_isNavigatingAway)
                Shutdown();
            _isNavigatingAway = false;
        };

        MainWindow = loginWindow;
        loginWindow.Show();
    }

    private void ShowShellWindow()
    {
        var scope = _services.CreateScope();
        var shellViewModel = scope.ServiceProvider.GetRequiredService<ShellViewModel>();
        var shellWindow = new MainWindow(shellViewModel);

        shellWindow.Closed += (_, _) =>
        {
            scope.Dispose();
            // Signing out returns to the login screen; closing the window (titlebar X)
            // exits the app — MainWindow sets WasSignedOut only for the former.
            if (shellWindow.WasSignedOut)
                ShowLoginWindow();
            else
                Shutdown();
        };

        MainWindow = shellWindow;
        shellWindow.Show();
    }
}
