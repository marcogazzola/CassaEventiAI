using CassaEventiAI.Data;
using CassaEventiAI.Models;
using CassaEventiAI.Services;
using CassaEventiAI.ViewModels;
using CassaEventiAI.Views.BackOffice;
using CassaEventiAI.Views.FrontOffice;
using CassaEventiAI.Views.Reports;
using CassaEventiAI.Views.Shared;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace CassaEventiAI;

public partial class App : System.Windows.Application
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
        ApplyTheme();

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
        s.AddSingleton<UpdateService>();

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
        s.AddTransient<ReportWindow>();
    }

    public static void ApplyTheme()
    {
        var uri = new Uri("pack://application:,,,/Resources/Themes/LightTheme.xaml");
        var dict = new ResourceDictionary { Source = uri };
        var existing = Current.Resources.MergedDictionaries
            .FirstOrDefault(d => d.Source?.OriginalString.Contains("Theme") == true);
        if (existing != null) Current.Resources.MergedDictionaries.Remove(existing);
        Current.Resources.MergedDictionaries.Add(dict);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Services.GetRequiredService<BackupService>().Dispose();
        Services.GetRequiredService<EventService>().Dispose();
        base.OnExit(e);
    }
}
