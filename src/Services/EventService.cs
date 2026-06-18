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
        var archiveName = $"{MakeSafeFileName(s.ActiveEventName ?? "evento")}_{DateTime.Now:ddMMyyyy_HHmm}.db";
        var dest = Path.Combine(archiveFolder, archiveName);
        _db.Dispose(); _db = null;
        File.Copy(s.ActiveDbPath!, dest);
        s.ActiveDbPath = string.Empty;
        s.ActiveEventName = string.Empty;
        config.SaveAppSettings(s);
        return dest;
    }

    public List<(string Name, string Path, DateTime Date)> GetArchivedEvents()
    {
        var folder = config.GetArchiveFolder();
        if (!Directory.Exists(folder)) return [];
        return Directory.GetFiles(folder, "*.db")
            .Select(f => (
                Name: Path.GetFileNameWithoutExtension(f).Replace("_", " "),
                Path: f,
                Date: File.GetLastWriteTime(f)))
            .OrderByDescending(x => x.Date)
            .ToList();
    }

    private static string MakeSafeFileName(string name)
        => string.Concat(name.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_").ToLower();

    public void Dispose() => _db?.Dispose();
}
