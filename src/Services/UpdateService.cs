using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;

namespace CassaEventiAI.Services;

public class UpdateService
{
    private const string GitHubOwner = "marcogazzola";
    private const string GitHubRepo = "CassaEventiAI";

    private static readonly HttpClient _http = new();

    static UpdateService()
    {
        _http.DefaultRequestHeaders.UserAgent.ParseAdd($"CassaEventiAI/{CurrentVersion}");
        _http.Timeout = TimeSpan.FromSeconds(10);
    }

    public static Version CurrentVersion =>
        Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0, 0);

    public static string CurrentVersionString =>
        $"v{CurrentVersion.Major}.{CurrentVersion.Minor}.{CurrentVersion.Build}";

    public record ReleaseInfo(string TagName, string VersionString, string DownloadUrl);

    public async Task<ReleaseInfo?> CheckForUpdateAsync()
    {
        try
        {
            var url = $"https://api.github.com/repos/{GitHubOwner}/{GitHubRepo}/releases/latest";
            var json = await _http.GetStringAsync(url);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var tagName = root.GetProperty("tag_name").GetString() ?? "";
            var versionStr = tagName.TrimStart('v');
            if (!Version.TryParse(versionStr, out var latest)) return null;
            if (latest <= CurrentVersion) return null;

            string? downloadUrl = null;
            foreach (var asset in root.GetProperty("assets").EnumerateArray())
            {
                var name = asset.GetProperty("name").GetString() ?? "";
                if (name.StartsWith("CassaEventiAI_Setup") && name.EndsWith(".exe"))
                {
                    downloadUrl = asset.GetProperty("browser_download_url").GetString();
                    break;
                }
            }

            return downloadUrl is null ? null : new ReleaseInfo(tagName, versionStr, downloadUrl);
        }
        catch
        {
            return null;
        }
    }

    public async Task DownloadAndInstallAsync(ReleaseInfo release, Action<int>? onProgress = null)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"CassaEventiAI_Setup_{release.VersionString}.exe");

        using var response = await _http.GetAsync(release.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        var total = response.Content.Headers.ContentLength ?? -1L;

        await using var stream = await response.Content.ReadAsStreamAsync();
        await using var file = File.Create(tempPath);

        var buffer = new byte[65536];
        long downloaded = 0;
        int read;
        while ((read = await stream.ReadAsync(buffer)) > 0)
        {
            await file.WriteAsync(buffer.AsMemory(0, read));
            downloaded += read;
            if (total > 0) onProgress?.Invoke((int)(downloaded * 100 / total));
        }
        await file.FlushAsync();

        Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });
    }
}
