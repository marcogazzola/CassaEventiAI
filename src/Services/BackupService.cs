using System.IO;
using System.Timers;

namespace CplCassaEventi.Services;

public class BackupService : IDisposable
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

        var backupFolder = Path.Combine(
            Path.GetDirectoryName(settings.ActiveDbPath)!, "Backups");
        Directory.CreateDirectory(backupFolder);

        var fileName = $"backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";
        var dest = Path.Combine(backupFolder, fileName);

        File.Copy(settings.ActiveDbPath, dest, overwrite: true);

        // Keep only last 10 backups
        var backups = Directory.GetFiles(backupFolder, "backup_*.db")
            .OrderByDescending(f => f).ToList();
        foreach (var old in backups.Skip(10))
            File.Delete(old);

        return dest;
    }

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
