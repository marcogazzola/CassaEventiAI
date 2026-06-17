using CplCassaEventi.Data;
using CplCassaEventi.Models;
using CplCassaEventi.Services;
using CplCassaEventi.ViewModels;
using CplCassaEventi.Views.BackOffice;
using CplCassaEventi.Views.FrontOffice;
using CplCassaEventi.Views.Shared;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace CplCassaEventi;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    public static AppSettings CurrentSettings { get; set; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        CurrentSettings = Services.GetRequiredService<ConfigService>().LoadAppSettings();
        ApplyTheme(CurrentSettings.DarkMode);

        Services.GetRequiredService<BackupService>().Start();

        var startup = Services.GetRequiredService<StartupWindow>();
        startup.Show();
    }

    private static void ConfigureServices(IServiceCollection s)
    {
        // Core services — singletons survive the whole session
        s.AddSingleton<ConfigService>();
        s.AddSingleton<EventService>();
        s.AddSingleton<BackupService>();
        s.AddSingleton<UsbService>();
        s.AddSingleton<AuthService>();
        s.AddSingleton<ProductService>();

        // Scoped/transient services
        s.AddTransient<SaleService>();
        s.AddTransient<ReceiptService>();
        s.AddTransient<PrintingService>();
        s.AddTransient<ReportService>();

        // ViewModels
        s.AddTransient<StartupViewModel>();
        s.AddTransient<LoginViewModel>();
        s.AddTransient<FrontOfficeViewModel>();
        s.AddTransient<BackOfficeViewModel>();
        s.AddTransient<DepartmentsViewModel>();
        s.AddTransient<ProductsViewModel>();
        s.AddTransient<OperatorsViewModel>();
        s.AddTransient<ReportViewModel>();

        // Windows
        s.AddTransient<StartupWindow>();
        s.AddTransient<LoginWindow>();
        s.AddTransient<MainWindow>();
        s.AddTransient<BackOfficeWindow>();
    }

    public static void ApplyTheme(bool dark)
    {
        var uri = new Uri(dark
            ? "pack://application:,,,/Resources/Themes/DarkTheme.xaml"
            : "pack://application:,,,/Resources/Themes/LightTheme.xaml");
        var dict = new ResourceDictionary { Source = uri };
        var existing = Current.Resources.MergedDictionaries
            .FirstOrDefault(d => d.Source?.OriginalString.Contains("Theme") == true);
        if (existing != null) Current.Resources.MergedDictionaries.Remove(existing);
        Current.Resources.MergedDictionaries.Add(dict);
        CurrentSettings.DarkMode = dark;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Services.GetRequiredService<BackupService>().Dispose();
        Services.GetRequiredService<EventService>().Dispose();
        base.OnExit(e);
    }
}
