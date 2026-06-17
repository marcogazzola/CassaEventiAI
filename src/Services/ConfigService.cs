using CplCassaEventi.Models;
using System.IO;
using System.Text.Json;

namespace CplCassaEventi.Services;

public class ConfigService
{
    private static readonly JsonSerializerOptions _json = new() { WriteIndented = true };

    private static string AppDataRoot =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CplCassaEventi");

    public string DataFolder => Path.Combine(AppDataRoot, "Config");
    private string ArchiveFolder => Path.Combine(AppDataRoot, "Archive");
    private string EventsFolder => Path.Combine(AppDataRoot, "Events");
    private string AppSettingsFile => Path.Combine(AppDataRoot, "app_settings.json");

    private string DepartmentsFile => Path.Combine(DataFolder, "departments.json");
    private string ProductsFile    => Path.Combine(DataFolder, "products.json");
    private string PaymentFile     => Path.Combine(DataFolder, "payment_methods.json");
    private string OperatorsFile   => Path.Combine(DataFolder, "operators.json");
    private string ReceiptFile     => Path.Combine(DataFolder, "receipt_config.json");

    public ConfigService() => EnsureFolders();

    public void EnsureFolders()
    {
        Directory.CreateDirectory(DataFolder);
        Directory.CreateDirectory(ArchiveFolder);
        Directory.CreateDirectory(EventsFolder);
    }

    public string GetArchiveFolder() => ArchiveFolder;
    public string GetEventsFolder() => EventsFolder;

    // ── AppSettings ───────────────────────────────────────────────────────

    public AppSettings LoadAppSettings()
    {
        if (!File.Exists(AppSettingsFile)) return new AppSettings();
        try { return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(AppSettingsFile)) ?? new(); }
        catch { return new(); }
    }

    public void SaveAppSettings(AppSettings s)
        => File.WriteAllText(AppSettingsFile, JsonSerializer.Serialize(s, _json));

    // ── Departments ───────────────────────────────────────────────────────

    public List<Department> LoadDepartments()
    {
        if (!File.Exists(DepartmentsFile)) return [];
        return JsonSerializer.Deserialize<List<Department>>(File.ReadAllText(DepartmentsFile)) ?? [];
    }

    public void SaveDepartments(List<Department> list)
        => File.WriteAllText(DepartmentsFile, JsonSerializer.Serialize(list, _json));

    // ── Products ──────────────────────────────────────────────────────────

    public List<Product> LoadProducts()
    {
        if (!File.Exists(ProductsFile)) return [];
        return JsonSerializer.Deserialize<List<Product>>(File.ReadAllText(ProductsFile)) ?? [];
    }

    public void SaveProducts(List<Product> list)
        => File.WriteAllText(ProductsFile, JsonSerializer.Serialize(list, _json));

    // ── Payment Methods ───────────────────────────────────────────────────

    public List<PaymentMethod> LoadPaymentMethods()
    {
        if (!File.Exists(PaymentFile))
            return [
                new() { Key="cash",   Label="Contanti", IsActive=true, RequiresCashInput=true,  SortOrder=0 },
                new() { Key="card",   Label="Carta",    IsActive=true, RequiresCashInput=false, SortOrder=1 },
                new() { Key="ticket", Label="Buono",    IsActive=false,RequiresCashInput=false, SortOrder=2 }
            ];
        return JsonSerializer.Deserialize<List<PaymentMethod>>(File.ReadAllText(PaymentFile)) ?? [];
    }

    public void SavePaymentMethods(List<PaymentMethod> list)
        => File.WriteAllText(PaymentFile, JsonSerializer.Serialize(list, _json));

    // ── Operators ─────────────────────────────────────────────────────────

    public List<Operator> LoadOperators()
    {
        if (!File.Exists(OperatorsFile)) return [];
        return JsonSerializer.Deserialize<List<Operator>>(File.ReadAllText(OperatorsFile)) ?? [];
    }

    public void SaveOperators(List<Operator> list)
        => File.WriteAllText(OperatorsFile, JsonSerializer.Serialize(list, _json));

    public int NextOperatorId()
    {
        var ops = LoadOperators();
        return ops.Count > 0 ? ops.Max(o => o.Id) + 1 : 1;
    }

    // ── Receipt Config ────────────────────────────────────────────────────

    public ReceiptConfig LoadReceiptConfig()
    {
        if (!File.Exists(ReceiptFile)) return new ReceiptConfig();
        return JsonSerializer.Deserialize<ReceiptConfig>(File.ReadAllText(ReceiptFile)) ?? new();
    }

    public void SaveReceiptConfig(ReceiptConfig cfg)
        => File.WriteAllText(ReceiptFile, JsonSerializer.Serialize(cfg, _json));

    // ── USB Export/Import ─────────────────────────────────────────────────

    public void ExportToUsb(string usbPath)
    {
        var dest = Path.Combine(usbPath, "CplCassaConfig");
        Directory.CreateDirectory(dest);
        foreach (var f in Directory.GetFiles(DataFolder, "*.json"))
            File.Copy(f, Path.Combine(dest, Path.GetFileName(f)), overwrite: true);
    }

    public void ImportFromUsb(string usbPath)
    {
        var src = Path.Combine(usbPath, "CplCassaConfig");
        if (!Directory.Exists(src))
            throw new DirectoryNotFoundException("Cartella CplCassaConfig non trovata nella chiavetta.");
        foreach (var f in Directory.GetFiles(src, "*.json"))
            File.Copy(f, Path.Combine(DataFolder, Path.GetFileName(f)), overwrite: true);
    }
}
