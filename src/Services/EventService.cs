using CassaEventiAI.Data;
using CassaEventiAI.Models;
using System.IO;

namespace CassaEventiAI.Services;

public class EventService(ConfigService config)
{
    private CassaDbContext? _db;

    public CassaDbContext Db => _db ?? throw new InvalidOperationException("Nessun evento attivo.");
    public bool HasActiveEvent => _db != null && !string.IsNullOrEmpty(App.CurrentSettings.ActiveDbPath);

    public void OpenEvent(string eventName, string dbPath)
    {
        _db?.Dispose();
        _db = CassaDbContext.Create(dbPath);
        App.CurrentSettings.ActiveEventName = eventName;
        App.CurrentSettings.ActiveDbPath = dbPath;
        config.SaveAppSettings(App.CurrentSettings);
    }

    public string CreateNewEvent(string eventName)
    {
        var safe = MakeSafeFileName(eventName);
        var folder = config.GetEventsFolder();
        var dbPath = Path.Combine(folder, $"{safe}.db");
        Directory.CreateDirectory(folder);
        OpenEvent(eventName, dbPath);
        return dbPath;
    }

    public string CloseAndArchive()
    {
        if (_db == null) throw new InvalidOperationException("Nessun evento da chiudere.");
        var s = App.CurrentSettings;
        var archiveFolder = config.GetArchiveFolder();
        Directory.CreateDirectory(archiveFolder);
        var safeName = MakeSafeFileName(s.ActiveEventName ?? "evento");
        var archiveName = $"{safeName}_Archived_{DateTime.Now:yyyyMMdd_HHmm}.db";
        var dest = Path.Combine(archiveFolder, archiveName);
        _db.Dispose(); _db = null;
        File.Move(s.ActiveDbPath!, dest);
        s.ActiveDbPath = string.Empty;
        s.ActiveEventName = string.Empty;
        config.SaveAppSettings(s);
        return dest;
    }

    public List<ArchivedEventInfo> GetArchivedEvents()
    {
        var folder = config.GetArchiveFolder();
        if (!Directory.Exists(folder)) return [];
        return Directory.GetFiles(folder, "*_Archived_*.db")
            .Select(f =>
            {
                var stem = Path.GetFileNameWithoutExtension(f);
                var idx = stem.LastIndexOf("_Archived_", StringComparison.Ordinal);
                var originalSafe = idx >= 0 ? stem[..idx] : stem;
                return new ArchivedEventInfo
                {
                    Name = originalSafe.Replace("_", " "),
                    Path = f,
                    Date = File.GetLastWriteTime(f)
                };
            })
            .OrderByDescending(x => x.Date)
            .ToList();
    }

    public void RestoreFromBackup(string backupPath)
    {
        var s = App.CurrentSettings;
        if (string.IsNullOrEmpty(s.ActiveDbPath))
            throw new InvalidOperationException("Nessun evento attivo.");
        _db?.Dispose(); _db = null;
        File.Copy(backupPath, s.ActiveDbPath!, overwrite: true);
        _db = CassaDbContext.Create(s.ActiveDbPath!);
    }

    public void ReopenArchivedEvent(ArchivedEventInfo ev)
    {
        var stem = Path.GetFileNameWithoutExtension(ev.Path);
        var idx = stem.LastIndexOf("_Archived_", StringComparison.Ordinal);
        var originalSafe = idx >= 0 ? stem[..idx] : stem;
        var eventsFolder = config.GetEventsFolder();
        Directory.CreateDirectory(eventsFolder);
        var dest = Path.Combine(eventsFolder, $"{originalSafe}.db");
        File.Move(ev.Path, dest);
        OpenEvent(ev.Name.Trim(), dest);
    }

    private static string MakeSafeFileName(string name)
        => string.Concat(name.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_").ToLower();

    public void Dispose() => _db?.Dispose();
}
