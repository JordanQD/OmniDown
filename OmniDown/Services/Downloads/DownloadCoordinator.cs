using OmniDown.Models;
using OmniDown.Services.Rpc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OmniDown.Services.Downloads;

public sealed class DownloadCoordinator
{
    private readonly Aria2RpcClient _rpcClient;
    private readonly ObservableCollection<DownloadTask> _tasks;
    private readonly string _taskCachePath;

    public DownloadCoordinator(Aria2RpcClient rpcClient, ObservableCollection<DownloadTask> tasks)
    {
        _rpcClient = rpcClient;
        _tasks = tasks;
        _taskCachePath = Path.Combine(GetAppDataDirectory(), "tasks.json");
        LoadTaskCache();
    }

    public async Task<DownloadTask> AddDownloadAsync(
        string sourceUri,
        string requestedName,
        string saveDirectory,
        int splitCount,
        CancellationToken cancellationToken = default)
    {
        string name = ResolveTaskName(sourceUri, requestedName);
        string? outputFileName = string.IsNullOrWhiteSpace(requestedName) ? null : name;
        string gid = await _rpcClient.AddUriAsync(sourceUri, outputFileName, saveDirectory, splitCount, cancellationToken);

        DownloadTask task = new()
        {
            Gid = gid,
            Name = name,
            SourceUri = sourceUri,
            SaveDirectory = saveDirectory,
            LocalFilePath = string.IsNullOrWhiteSpace(name) ? string.Empty : Path.Combine(saveDirectory, name),
            Status = "Waiting",
            IsPeerTransfer = IsPeerTransfer(sourceUri),
            Progress = 0
        };

        _tasks.Insert(0, task);
        SaveTaskCache();
        return task;
    }

    public async Task<DownloadSnapshot> RefreshAsync(CancellationToken cancellationToken = default)
    {
        List<Aria2TaskStatus> remoteTasks = [];
        remoteTasks.AddRange(await _rpcClient.TellActiveAsync(cancellationToken));
        remoteTasks.AddRange(await _rpcClient.TellWaitingAsync(cancellationToken));
        remoteTasks.AddRange(await _rpcClient.TellStoppedAsync(cancellationToken));

        foreach (Aria2TaskStatus remoteTask in remoteTasks)
        {
            UpsertTask(remoteTask);
            if (IsCompletedStatus(remoteTask.Status))
            {
                await RemoveCompletedDownloadResultAsync(remoteTask.Gid, cancellationToken);
            }
        }

        Aria2GlobalStat stat = await _rpcClient.GetGlobalStatAsync(cancellationToken);
        return new DownloadSnapshot(
            ActiveCount: ParseLong(stat.NumActive),
            DownloadSpeed: ParseLong(stat.DownloadSpeed),
            UploadSpeed: ParseLong(stat.UploadSpeed));
    }

    public async Task PauseAsync(IEnumerable<DownloadTask> tasks, CancellationToken cancellationToken = default)
    {
        foreach (DownloadTask task in tasks.Where(task => !string.IsNullOrWhiteSpace(task.Gid)))
        {
            await _rpcClient.PauseAsync(task.Gid, cancellationToken);
            task.Status = "Paused";
        }
    }

    public async Task ResumeAsync(IEnumerable<DownloadTask> tasks, CancellationToken cancellationToken = default)
    {
        foreach (DownloadTask task in tasks.Where(task => !string.IsNullOrWhiteSpace(task.Gid)))
        {
            await _rpcClient.UnpauseAsync(task.Gid, cancellationToken);
            task.Status = "Waiting";
        }
    }

    public async Task DeleteAsync(IEnumerable<DownloadTask> tasks, bool deleteFiles = false, CancellationToken cancellationToken = default)
    {
        foreach (DownloadTask task in tasks.Where(task => !string.IsNullOrWhiteSpace(task.Gid)).ToArray())
        {
            try
            {
                await _rpcClient.RemoveAsync(task.Gid, cancellationToken);
            }
            catch
            {
                await _rpcClient.RemoveDownloadResultAsync(task.Gid, cancellationToken);
            }

            if (deleteFiles)
            {
                DeleteLocalFiles(task);
            }

            _tasks.Remove(task);
        }

        SaveTaskCache();
    }

    public async Task<int> ClearCompletedAsync(bool deleteFiles = false, CancellationToken cancellationToken = default)
    {
        DownloadTask[] completedTasks = _tasks
            .Where(task => IsCompletedStatus(task.Status))
            .ToArray();

        foreach (DownloadTask task in completedTasks)
        {
            if (deleteFiles)
            {
                DeleteLocalFiles(task);
            }

            await RemoveCompletedDownloadResultAsync(task.Gid, cancellationToken);
            _tasks.Remove(task);
        }

        SaveTaskCache();
        return completedTasks.Length;
    }

    public async Task RemoveCompletedDownloadResultsAsync(CancellationToken cancellationToken = default)
    {
        foreach (DownloadTask task in _tasks
            .Where(task => !string.IsNullOrWhiteSpace(task.Gid) && IsCompletedStatus(task.Status))
            .ToArray())
        {
            await RemoveCompletedDownloadResultAsync(task.Gid, cancellationToken);
        }
    }

    private void UpsertTask(Aria2TaskStatus remoteTask)
    {
        if (NormalizeStatus(remoteTask.Status).Equals("Removed", StringComparison.OrdinalIgnoreCase))
        {
            DownloadTask? removedTask = _tasks.FirstOrDefault(item => item.Gid == remoteTask.Gid);
            if (removedTask is not null)
            {
                _tasks.Remove(removedTask);
                SaveTaskCache();
            }

            return;
        }

        DownloadTask? task = _tasks.FirstOrDefault(item => item.Gid == remoteTask.Gid);
        if (task is null)
        {
            task = new DownloadTask
            {
                Gid = remoteTask.Gid,
                Name = ResolveRemoteName(remoteTask),
                SourceUri = ResolveRemoteUri(remoteTask),
                SaveDirectory = remoteTask.Directory,
                LocalFilePath = ResolveRemotePath(remoteTask)
            };
            _tasks.Add(task);
        }

        long totalLength = ParseLong(remoteTask.TotalLength);
        long completedLength = ParseLong(remoteTask.CompletedLength);
        string normalizedStatus = NormalizeStatus(remoteTask.Status);
        bool isDownloading = normalizedStatus.Contains("download", StringComparison.OrdinalIgnoreCase);
        task.Status = normalizedStatus;
        if (totalLength > 0)
        {
            task.TotalLength = totalLength;
        }

        if (completedLength > 0 || totalLength > 0 || isDownloading)
        {
            task.CompletedLength = completedLength;
        }

        task.DownloadSpeed = isDownloading ? ParseLong(remoteTask.DownloadSpeed) : 0;
        task.UploadSpeed = isDownloading ? ParseLong(remoteTask.UploadSpeed) : 0;
        task.IsPeerTransfer = IsPeerTransfer(remoteTask);
        task.Progress = task.TotalLength <= 0 ? task.Progress : Math.Clamp(task.CompletedLength * 100d / task.TotalLength, 0, 100);

        if (string.IsNullOrWhiteSpace(task.Name))
        {
            string remoteName = ResolveRemoteName(remoteTask);
            if (!string.IsNullOrWhiteSpace(remoteName))
            {
                task.Name = remoteName;
            }
        }

        if (string.IsNullOrWhiteSpace(task.SourceUri))
        {
            task.SourceUri = ResolveRemoteUri(remoteTask);
        }

        if (string.IsNullOrWhiteSpace(task.SaveDirectory))
        {
            task.SaveDirectory = remoteTask.Directory;
        }

        string remotePath = ResolveRemotePath(remoteTask);
        if (!string.IsNullOrWhiteSpace(remotePath))
        {
            task.LocalFilePath = remotePath;
            string remoteName = Path.GetFileName(remotePath);
            if (!string.IsNullOrWhiteSpace(remoteName) &&
                (string.IsNullOrWhiteSpace(task.Name) || task.Name.Equals(task.Gid, StringComparison.OrdinalIgnoreCase)))
            {
                task.Name = remoteName;
            }
        }

        if (normalizedStatus.Contains("complete", StringComparison.OrdinalIgnoreCase))
        {
            DeleteControlFiles(task, remoteTask);
        }

        SaveTaskCache();
    }

    private static string ResolveTaskName(string sourceUri, string requestedName)
    {
        if (!string.IsNullOrWhiteSpace(requestedName))
        {
            return requestedName.Trim();
        }

        if (Uri.TryCreate(sourceUri, UriKind.Absolute, out Uri? uri))
        {
            string fileName = Path.GetFileName(uri.LocalPath);
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                return fileName;
            }
        }

        return "New download";
    }

    private static string ResolveRemoteName(Aria2TaskStatus task)
    {
        string path = task.Files.FirstOrDefault(file => !string.IsNullOrWhiteSpace(file.Path))?.Path ?? string.Empty;
        string fileName = Path.GetFileName(path);
        return string.IsNullOrWhiteSpace(fileName) ? string.Empty : fileName;
    }

    private static string ResolveRemotePath(Aria2TaskStatus task)
    {
        return task.Files.FirstOrDefault(file => !string.IsNullOrWhiteSpace(file.Path))?.Path ?? string.Empty;
    }

    private static string ResolveRemoteUri(Aria2TaskStatus task)
    {
        return task.Files
            .SelectMany(file => file.Uris)
            .Select(uri => uri.Uri)
            .FirstOrDefault(uri => !string.IsNullOrWhiteSpace(uri)) ?? string.Empty;
    }

    private static bool IsPeerTransfer(Aria2TaskStatus task)
    {
        if (task.BitTorrent.HasValue)
        {
            return true;
        }

        string sourceUri = ResolveRemoteUri(task);
        return IsPeerTransfer(sourceUri);
    }

    private static bool IsPeerTransfer(string sourceUri)
    {
        return sourceUri.StartsWith("magnet:", StringComparison.OrdinalIgnoreCase)
            || sourceUri.EndsWith(".torrent", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeStatus(string status)
    {
        return status.ToLowerInvariant() switch
        {
            "active" => "Downloading",
            "waiting" => "Waiting",
            "paused" => "Paused",
            "complete" => "Completed",
            "error" => "Error",
            "removed" => "Removed",
            _ => status
        };
    }

    private static long ParseLong(string value)
    {
        return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long result)
            ? result
            : 0;
    }

    private static void DeleteLocalFiles(DownloadTask task)
    {
        string path = task.LocalFilePath;
        if (string.IsNullOrWhiteSpace(path) && !string.IsNullOrWhiteSpace(task.SaveDirectory) && !string.IsNullOrWhiteSpace(task.Name))
        {
            path = Path.Combine(task.SaveDirectory, task.Name);
        }

        DeleteFileIfExists(path);
        DeleteFileIfExists($"{path}.aria2");
    }

    private static void DeleteControlFiles(DownloadTask task, Aria2TaskStatus remoteTask)
    {
        HashSet<string> candidatePaths = new(StringComparer.OrdinalIgnoreCase);
        AddControlFileCandidate(candidatePaths, task.LocalFilePath);

        if (!string.IsNullOrWhiteSpace(task.SaveDirectory) &&
            !string.IsNullOrWhiteSpace(task.Name))
        {
            AddControlFileCandidate(candidatePaths, Path.Combine(task.SaveDirectory, task.Name));
        }

        foreach (Aria2FileStatus file in remoteTask.Files)
        {
            AddControlFileCandidate(candidatePaths, file.Path);
        }

        foreach (string candidatePath in candidatePaths)
        {
            TryDeleteFileIfExists(candidatePath);
        }
    }

    private static void AddControlFileCandidate(HashSet<string> candidatePaths, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        candidatePaths.Add($"{path}.aria2");
    }

    private static void DeleteFileIfExists(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return;
        }

        File.Delete(path);
    }

    private static void TryDeleteFileIfExists(string path)
    {
        try
        {
            DeleteFileIfExists(path);
        }
        catch
        {
            // aria2 may still briefly hold the control file; the next refresh will retry.
        }
    }

    private void LoadTaskCache()
    {
        if (!File.Exists(_taskCachePath))
        {
            return;
        }

        try
        {
            string json = File.ReadAllText(_taskCachePath);
            List<CachedDownloadTask>? cachedTasks = JsonSerializer.Deserialize<List<CachedDownloadTask>>(json);
            if (cachedTasks is null)
            {
                return;
            }

            foreach (CachedDownloadTask cachedTask in cachedTasks.Where(task => !string.IsNullOrWhiteSpace(task.Gid)))
            {
                _tasks.Add(new DownloadTask
                {
                    Gid = cachedTask.Gid,
                    Name = cachedTask.Name,
                    SourceUri = cachedTask.SourceUri,
                    SaveDirectory = cachedTask.SaveDirectory,
                    LocalFilePath = cachedTask.LocalFilePath,
                    Status = cachedTask.Status,
                    Progress = cachedTask.Progress,
                    CompletedLength = cachedTask.CompletedLength,
                    TotalLength = cachedTask.TotalLength,
                    IsPeerTransfer = cachedTask.IsPeerTransfer,
                    CreatedAt = cachedTask.CreatedAt
                });
            }
        }
        catch
        {
            // A corrupt cache should not stop aria2 from being the source of truth.
        }
    }

    private void SaveTaskCache()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_taskCachePath)!);
            List<CachedDownloadTask> cachedTasks = _tasks
                .Where(task => !string.IsNullOrWhiteSpace(task.Gid))
                .Select(task => new CachedDownloadTask(
                    task.Gid,
                    task.Name,
                    task.SourceUri,
                    task.SaveDirectory,
                    task.LocalFilePath,
                    task.Status,
                    task.Progress,
                    task.CompletedLength,
                    task.TotalLength,
                    task.IsPeerTransfer,
                    task.CreatedAt))
                .ToList();

            File.WriteAllText(_taskCachePath, JsonSerializer.Serialize(cachedTasks, new JsonSerializerOptions
            {
                WriteIndented = true
            }));
        }
        catch
        {
            // Cache persistence is best-effort; aria2 session data still controls downloads.
        }
    }

    private static string GetAppDataDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OmniDown");
    }

    private async Task RemoveCompletedDownloadResultAsync(string gid, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(gid))
        {
            return;
        }

        try
        {
            await _rpcClient.RemoveDownloadResultAsync(gid, cancellationToken);
        }
        catch
        {
            // The completed result may already be gone; cached task metadata remains the UI source of truth.
        }
    }

    private static bool IsCompletedStatus(string status)
    {
        return NormalizeStatus(status).Contains("complete", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed record DownloadSnapshot(long ActiveCount, long DownloadSpeed, long UploadSpeed);

internal sealed record CachedDownloadTask(
    string Gid,
    string Name,
    string SourceUri,
    string SaveDirectory,
    string LocalFilePath,
    string Status,
    double Progress,
    long CompletedLength,
    long TotalLength,
    bool IsPeerTransfer,
    DateTimeOffset CreatedAt);
