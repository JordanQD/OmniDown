using System;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using OmniDown.Services.Logging;
using OmniDown.Services.Storage;

namespace OmniDown.Services.Engine;

public sealed class EngineUpdateService
{
    private const string GitHubReleaseUrl = "https://api.github.com/repos/AnInsomniacy/aria2-next/releases/latest";
    private readonly HttpClient _httpClient;

    public EngineUpdateService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "OmniDown-EngineUpdater");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<EngineUpdateInfo?> CheckForUpdateAsync(string currentVersion)
    {
        if (string.IsNullOrWhiteSpace(currentVersion))
        {
            AppLogger.Info("EngineUpdater", "current version unknown, skipping update check");
            return null;
        }

        try
        {
            AppLogger.Info("EngineUpdater", $"checking for updates, current={currentVersion}");
            string json = await _httpClient.GetStringAsync(GitHubReleaseUrl);
            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;

            string tagName = root.GetProperty("tag_name").GetString() ?? string.Empty;
            string latestVersion = tagName.StartsWith("v", StringComparison.OrdinalIgnoreCase)
                ? tagName[1..]
                : tagName;

            if (!IsNewer(latestVersion, currentVersion))
            {
                AppLogger.Info("EngineUpdater", $"current {currentVersion} is up to date (latest={latestVersion})");
                return null;
            }

            string platformSuffix = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => "windows-x86_64",
                Architecture.Arm64 => "windows-arm64",
                _ => ""
            };

            if (string.IsNullOrEmpty(platformSuffix))
            {
                AppLogger.Warning("EngineUpdater", $"unsupported platform: {RuntimeInformation.ProcessArchitecture}");
                return null;
            }

            foreach (JsonElement asset in root.GetProperty("assets").EnumerateArray())
            {
                string name = asset.GetProperty("name").GetString() ?? string.Empty;
                if (name.Contains(platformSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    string downloadUrl = asset.GetProperty("browser_download_url").GetString() ?? string.Empty;
                    AppLogger.Info("EngineUpdater", $"found update: {name} version={latestVersion}");
                    return new EngineUpdateInfo(latestVersion, downloadUrl, name);
                }
            }

            AppLogger.Warning("EngineUpdater", $"no matching asset for platform {platformSuffix}");
            return null;
        }
        catch (Exception ex)
        {
            AppLogger.Warning("EngineUpdater", $"update check failed: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> DownloadAndInstallAsync(EngineUpdateInfo update, string targetPath)
    {
        string tempPath = targetPath + ".download";
        try
        {
            AppLogger.Info("EngineUpdater", $"downloading {update.FileName} from {update.DownloadUrl}");
            using HttpResponseMessage response = await _httpClient.GetAsync(update.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            string? directory = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await using Stream sourceStream = await response.Content.ReadAsStreamAsync();
            await using FileStream targetStream = File.Create(tempPath, 4096, FileOptions.Asynchronous);
            await sourceStream.CopyToAsync(targetStream);

            // Atomic replacement: delete old, rename new
            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }

            File.Move(tempPath, targetPath);
            AppLogger.Info("EngineUpdater", $"installed {update.Version} to {targetPath}");
            return true;
        }
        catch (Exception ex)
        {
            AppLogger.Error("EngineUpdater", $"download/install failed: {ex.Message}");
            TryDelete(tempPath);
            return false;
        }
    }

    public string GetBundledEnginePath()
    {
        string architecture = RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
        return Path.Combine(
            Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
            ?? AppContext.BaseDirectory,
            "Engines", "aria2", $"win-{architecture}", "aria2c.exe");
    }

    private static bool IsNewer(string latest, string current)
    {
        if (Version.TryParse(latest, out Version? latestVersion) &&
            Version.TryParse(current, out Version? currentVersion))
        {
            return latestVersion > currentVersion;
        }

        return string.Compare(latest, current, StringComparison.OrdinalIgnoreCase) != 0;
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch { /* best effort */ }
    }
}

public sealed record EngineUpdateInfo(string Version, string DownloadUrl, string FileName);
