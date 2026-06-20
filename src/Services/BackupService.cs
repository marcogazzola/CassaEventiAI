using CassaEventiAI.Models;
using System.IO;
using System.Timers;

namespace CassaEventiAI.Services;

public class BackupService(ConfigService config) : IDisposable
{
    private System.Timers.Timer? _timer;

    public void Start()
    {
        var settings = App.CurrentSettings;
        if (!settings.AutoBackupEnabled) return;

        _timer = new System.Timers.Timer(settings.AutoBackupIntervalMinutes * 60_000);
        _timer.Elapsed += OnTimerElapsed;
        _timer.AutoReset = true;
        _timer.Start();
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e) => RunBackup();

    public string RunBackup()
    {
        var settings = App.CurrentSettings;
        if (string.IsNullOrEmpty(settings.ActiveDbPath)) return string.Empty;

        var safeName = MakeSafeFileName(settings.ActiveEventName ?? "evento");
        var backupFolder = config.GetBackupsFolder();
        Directory.CreateDirectory(backupFolder);

        var fileName = $"{safeName}_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";
        var dest = Path.Combine(backupFolder, fileName);

        File.Copy(settings.ActiveDbPath, dest, overwrite: true);

        var backups = Directory.GetFiles(backupFolder, $"{safeName}_backup_*.db")
            .OrderByDescending(f => f).ToList();
        foreach (var old in backups.Skip(10))
            File.Delete(old);

        return dest;
    }

    public List<BackupInfo> GetBackupsForCurrentEvent()
    {
        var settings = App.CurrentSettings;
        if (string.IsNullOrEmpty(settings.ActiveEventName)) return [];
        var safeName = MakeSafeFileName(settings.ActiveEventName);
        var backupFolder = config.GetBackupsFolder();
        if (!Directory.Exists(backupFolder)) return [];
        return Directory.GetFiles(backupFolder, $"{safeName}_backup_*.db")
            .Select(f => new BackupInfo { Path = f, Date = File.GetLastWriteTime(f) })
            .OrderByDescending(x => x.Date)
            .ToList();
    }

    private static string MakeSafeFileName(string name)
        => string.Concat(name.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_").ToLower();

    public void BackupToUsb(string usbPath)
    {
        var settings = App.CurrentSettings;
        if (string.IsNullOrEmpty(settings.ActiveDbPath))
            throw new InvalidOperationException("Nessun evento attivo.");

        var dest = Path.Combine(usbPath, "CplCassaBackup");
        Directory.CreateDirectory(dest);

        var fileName = $"{Path.GetFileNameWithoutExtension(settings.ActiveDbPath)}_{DateTime.Now:yyyyMMdd_HHmm}.db";
        File.Copy(settings.ActiveDbPath, Path.Combine(dest, fileName), overwrite: true);
    }

    public void Stop() => _timer?.Stop();

    public void Dispose()
    {
        _timer?.Stop();
        _timer?.Dispose();
    }
}
