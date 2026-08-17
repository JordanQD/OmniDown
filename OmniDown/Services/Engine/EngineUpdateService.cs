using OmniDown.Services.Logging;
using OmniDown.Services.Storage;
using System;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;

namespace OmniDown.Services.Engine;

public sealed class EngineUpdateService
{
    private const string GitHubReleaseUrl = "https://api.github.com/repos/AnInsomniacy/aria2-next/releases/latest";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(6);
    private readonly HttpClient _httpClient;

    public EngineUpdateService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "OmniDown-EngineUpdater");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<EngineUpdateCheckResult> CheckForUpdateAsync(string currentVersion, bool forceRefresh = false)
    {
        if (string.IsNullOrWhiteSpace(currentVersion))
        {
            AppLogger.Info("EngineUpdater", "current version unknown, skipping update check");
            return new EngineUpdateCheckResult(false, false, null, null);
        }

        try
        {
            // Skip API call if recently checked (unless forced)
            if (!forceRefresh)
            {
                UpdateCache cache = LoadCache();
                if (cache.IsFresh)
                {
                    AppLogger.Info("EngineUpdater", $"using cached result: latest={cache.LatestVersion} (checked {cache.LastCheck:O})");
                    bool newer = IsNewer(cache.LatestVersion, currentVersion);
                    return new EngineUpdateCheckResult(true, newer, null, cache.LatestVersion);
                }
            }

            AppLogger.Info("EngineUpdater", $"checking for updates, current={currentVersion}");
            string json = await _httpClient.GetStringAsync(GitHubReleaseUrl);
            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;

            string tagName = root.GetProperty("tag_name").GetString() ?? string.Empty;
            string latestVersion = tagName.StartsWith("v", StringComparison.OrdinalIgnoreCase)
                ? tagName[1..]
                : tagName;

            SaveCache(latestVersion);

            if (!IsNewer(latestVersion, currentVersion))
            {
                AppLogger.Info("EngineUpdater", $"current {currentVersion} is up to date (latest={latestVersion})");
                return new EngineUpdateCheckResult(true, false, null, latestVersion);
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
                return new EngineUpdateCheckResult(false, false, null, null);
            }

            foreach (JsonElement asset in root.GetProperty("assets").EnumerateArray())
            {
                string name = asset.GetProperty("name").GetString() ?? string.Empty;
                if (name.Contains(platformSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    string downloadUrl = asset.GetProperty("browser_download_url").GetString() ?? string.Empty;
                    AppLogger.Info("EngineUpdater", $"found update: {name} version={latestVersion}");
                    return new EngineUpdateCheckResult(true, true, new EngineUpdateInfo(latestVersion, downloadUrl, name), latestVersion);
                }
            }

            AppLogger.Warning("EngineUpdater", $"no matching asset for platform {platformSuffix}");
            return new EngineUpdateCheckResult(false, false, null, null);
        }
        catch (HttpRequestException ex)
        {
            string error = ex.StatusCode.HasValue
                ? $"GitHub API returned {(int)ex.StatusCode} ({ex.StatusCode})"
                : $"network error: {ex.Message}";
            AppLogger.Warning("EngineUpdater", $"update check failed: {error}");
            return new EngineUpdateCheckResult(false, false, null, null, error);
        }
        catch (Exception ex)
        {
            AppLogger.Warning("EngineUpdater", $"update check failed: {ex.Message}");
            return new EngineUpdateCheckResult(false, false, null, null, ex.Message);
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

            TryDelete(tempPath);

            await using Stream sourceStream = await response.Content.ReadAsStreamAsync();
            await using FileStream targetStream = File.Create(tempPath, 4096, FileOptions.Asynchronous);
            await sourceStream.CopyToAsync(targetStream);

            // Retry swapping the file — antivirus may briefly lock the target
            for (int attempt = 0; attempt < 5; attempt++)
            {
                try
                {
                    if (File.Exists(targetPath))
                    {
                        File.Delete(targetPath);
                    }

                    File.Move(tempPath, targetPath);
                    AppLogger.Info("EngineUpdater", $"installed {update.Version} to {targetPath}");
                    return true;
                }
                catch (IOException) when (attempt < 4)
                {
                    AppLogger.Info("EngineUpdater", $"file locked, retrying ({attempt + 1}/5)...");
                    await Task.Delay(TimeSpan.FromSeconds(2));
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            AppLogger.Error("EngineUpdater", $"download/install failed: {ex.Message}");
            TryDelete(tempPath);
            return false;
        }
    }

    public string GetImportedEnginePath()
    {
        return Aria2EngineStore.GetImportedEnginePath(OmniDown.Models.Settings.Aria2EngineType.Aria2Next);
    }

    public bool IsImportedEngineAvailable()
    {
        string localPath = GetImportedEnginePath();
        bool exists = File.Exists(localPath);
        if (!exists)
        {
            AppLogger.Warning("EngineUpdater", "no imported aria2-next engine found");
        }

        return exists;
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

    private static string CachePath => Path.Combine(AppPaths.LocalDataDirectory, "engine_update_cache.json");

    private static UpdateCache LoadCache()
    {
        try
        {
            if (File.Exists(CachePath))
            {
                string json = File.ReadAllText(CachePath);
                return JsonSerializer.Deserialize<UpdateCache>(json) ?? new UpdateCache();
            }
        }
        catch { }
        return new UpdateCache();
    }

    private static void SaveCache(string latestVersion)
    {
        try
        {
            var cache = new UpdateCache
            {
                LastCheck = DateTimeOffset.UtcNow,
                LatestVersion = latestVersion
            };
            string? directory = Path.GetDirectoryName(CachePath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(CachePath, JsonSerializer.Serialize(cache));
        }
        catch { }
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch { /* best effort */ }
    }

    private sealed class UpdateCache
    {
        public DateTimeOffset LastCheck { get; set; }
        public string LatestVersion { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonIgnore]
        public bool IsFresh => (DateTimeOffset.UtcNow - LastCheck) < CacheDuration && !string.IsNullOrEmpty(LatestVersion);
    }
}

public sealed record EngineUpdateCheckResult(
    bool Succeeded,
    bool UpdateAvailable,
    EngineUpdateInfo? Update,
    string? LatestVersion,
    string? ErrorMessage = null);

public sealed record EngineUpdateInfo(string Version, string DownloadUrl, string FileName);
