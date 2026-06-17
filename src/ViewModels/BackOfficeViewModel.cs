using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CplCassaEventi.Models;
using CplCassaEventi.Services;
using System.Collections.ObjectModel;

namespace CplCassaEventi.ViewModels;

public partial class BackOfficeViewModel : BaseViewModel
{
    private readonly EventService _events;
    private readonly ConfigService _config;
    private readonly BackupService _backup;
    private readonly UsbService _usb;
    private readonly PrintingService _printing;

    public DepartmentsViewModel Departments { get; }
    public ProductsViewModel Products { get; }
    public OperatorsViewModel Operators { get; }
    public ReportViewModel Report { get; }

    public BackOfficeViewModel(
        EventService events, ConfigService config, BackupService backup,
        UsbService usb, PrintingService printing,
        DepartmentsViewModel departments, ProductsViewModel products,
        OperatorsViewModel operators, ReportViewModel report)
    {
        _events = events; _config = config; _backup = backup;
        _usb = usb; _printing = printing;
        Departments = departments; Products = products;
        Operators = operators; Report = report;

        LoadSettings();
        LoadPaymentMethods();
        LoadReceiptConfig();
        LoadEventInfo();
        Departments.Load();
        Products.Load();
        Operators.Load();
    }

    // ── Event info ────────────────────────────────────────────────────────

    [ObservableProperty] private string _activeEventName = string.Empty;
    [ObservableProperty] private string _activeDbPath = string.Empty;
    [ObservableProperty] private ObservableCollection<(string Name, string Path, DateTime Date)> _archivedEvents = [];
    [ObservableProperty] private string _newEventName = string.Empty;

    private void LoadEventInfo()
    {
        ActiveEventName = App.CurrentSettings.ActiveEventName;
        ActiveDbPath = App.CurrentSettings.ActiveDbPath;
        ArchivedEvents = new(_events.GetArchivedEvents());
    }

    [RelayCommand]
    private void CreateNewEvent()
    {
        if (string.IsNullOrWhiteSpace(NewEventName)) { ShowError("Inserisci il nome dell'evento."); return; }
        if (!Confirm($"Creare il nuovo evento \"{NewEventName}\"?")) return;
        _events.CreateNewEvent(NewEventName);
        NewEventName = string.Empty;
        LoadEventInfo();
        StatusMessage = $"Evento creato.";
    }

    [RelayCommand]
    private void CloseAndArchiveEvent()
    {
        if (!Confirm("Chiudere e archiviare l'evento corrente?\nIl database verrà rinominato e spostato nell'archivio.")) return;
        var path = _events.CloseAndArchive();
        LoadEventInfo();
        StatusMessage = $"Evento archiviato: {System.IO.Path.GetFileName(path)}";
    }

    [RelayCommand]
    private void OpenArchivedEvent((string Name, string Path, DateTime Date) ev)
    {
        if (!Confirm($"Riaprire l'evento \"{ev.Name}\"?\nL'evento corrente verrà chiuso.")) return;
        _events.OpenEvent(ev.Name, ev.Path);
        LoadEventInfo();
        StatusMessage = $"Evento \"{ev.Name}\" riaperto.";
    }

    // ── Settings ──────────────────────────────────────────────────────────

    [ObservableProperty] private bool _printerEnabled;
    [ObservableProperty] private string _printerPort = "COM3";
    [ObservableProperty] private bool _showTotalInFooter;
    [ObservableProperty] private bool _kioskMode;
    [ObservableProperty] private bool _darkMode;
    [ObservableProperty] private bool _autoBackupEnabled;
    [ObservableProperty] private int _autoBackupIntervalMinutes = 30;

    private void LoadSettings()
    {
        var s = App.CurrentSettings;
        PrinterEnabled = s.PrinterEnabled; PrinterPort = s.PrinterPort;
        ShowTotalInFooter = s.ShowTotalInFooter; KioskMode = s.KioskMode;
        DarkMode = s.DarkMode; AutoBackupEnabled = s.AutoBackupEnabled;
        AutoBackupIntervalMinutes = s.AutoBackupIntervalMinutes;
    }

    [RelayCommand]
    private void SaveSettings()
    {
        var s = App.CurrentSettings;
        s.PrinterEnabled = PrinterEnabled; s.PrinterPort = PrinterPort;
        s.ShowTotalInFooter = ShowTotalInFooter; s.KioskMode = KioskMode;
        s.AutoBackupEnabled = AutoBackupEnabled; s.AutoBackupIntervalMinutes = AutoBackupIntervalMinutes;
        _config.SaveAppSettings(s);
        if (DarkMode != s.DarkMode) App.ApplyTheme(DarkMode);
        StatusMessage = "Impostazioni salvate.";
    }

    // ── Payment methods ───────────────────────────────────────────────────

    [ObservableProperty] private ObservableCollection<PaymentMethod> _paymentMethods = [];

    private void LoadPaymentMethods() => PaymentMethods = new(_config.LoadPaymentMethods());

    [RelayCommand] private void AddPaymentMethod()
        => PaymentMethods.Add(new PaymentMethod { Key = $"method{PaymentMethods.Count + 1}", Label = "Nuovo", IsActive = true, SortOrder = PaymentMethods.Count });

    [RelayCommand] private void RemovePaymentMethod(PaymentMethod pm) => PaymentMethods.Remove(pm);

    [RelayCommand] private void SavePaymentMethods()
    {
        _config.SavePaymentMethods([.. PaymentMethods]);
        StatusMessage = "Metodi di pagamento salvati.";
    }

    // ── Receipt config ────────────────────────────────────────────────────

    [ObservableProperty] private string _receiptHeader = string.Empty;
    [ObservableProperty] private string _receiptFooter = string.Empty;
    [ObservableProperty] private bool _printPrices = true;
    [ObservableProperty] private bool _printDeptSubtotals;

    private void LoadReceiptConfig()
    {
        var c = _config.LoadReceiptConfig();
        ReceiptHeader = c.HeaderText; ReceiptFooter = c.FooterText;
        PrintPrices = c.PrintPrices; PrintDeptSubtotals = c.PrintDepartmentSubtotals;
    }

    [RelayCommand] private void SaveReceiptConfig()
    {
        _config.SaveReceiptConfig(new ReceiptConfig
        {
            HeaderText = ReceiptHeader, FooterText = ReceiptFooter,
            PrintPrices = PrintPrices, PrintDepartmentSubtotals = PrintDeptSubtotals
        });
        StatusMessage = "Configurazione scontrino salvata.";
    }

    // ── Printer test ──────────────────────────────────────────────────────

    [RelayCommand] private void TestPrint() { _printing.PrintTestPage(); StatusMessage = "Pagina di test inviata."; }

    // ── Backup ────────────────────────────────────────────────────────────

    [RelayCommand] private void RunManualBackup()
    {
        var p = _backup.RunBackup();
        StatusMessage = string.IsNullOrEmpty(p) ? "Nessun evento attivo." : $"Backup: {System.IO.Path.GetFileName(p)}";
    }

    [RelayCommand] private void BackupToUsb()
    {
        var d = _usb.GetFirstUsb();
        if (d == null) { ShowError("Nessuna chiavetta USB trovata."); return; }
        _backup.BackupToUsb(d.RootDirectory.FullName);
        StatusMessage = $"Backup copiato su {d.Name}.";
    }

    // ── USB config ────────────────────────────────────────────────────────

    [RelayCommand] private void ExportConfigToUsb()
    {
        var d = _usb.GetFirstUsb();
        if (d == null) { ShowError("Nessuna chiavetta USB trovata."); return; }
        _config.ExportToUsb(d.RootDirectory.FullName);
        StatusMessage = $"Configurazione esportata su {d.Name}.";
    }

    [RelayCommand] private void ImportConfigFromUsb()
    {
        var d = _usb.GetFirstUsb();
        if (d == null) { ShowError("Nessuna chiavetta USB trovata."); return; }
        if (!Confirm("Sovrascrivere la configurazione corrente con quella della chiavetta?")) return;
        _config.ImportFromUsb(d.RootDirectory.FullName);
        Departments.Load(); Products.Load();
        StatusMessage = "Configurazione importata.";
    }
}
