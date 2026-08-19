using OmniDown.Models;
using OmniDown.Services.Logging;
using OmniDown.Services.Rpc;
using OmniDown.Services.Storage;
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

    public bool DeleteTorrentAfterComplete { get; set; }

    public DownloadCoordinator(Aria2RpcClient rpcClient, ObservableCollection<DownloadTask> tasks)
    {
        _rpcClient = rpcClient;
        _tasks = tasks;
        _taskCachePath = Path.Combine(AppPaths.LocalDataDirectory, "tasks.json");
        LoadTaskCache();
    }

    public async Task<DownloadTask> AddDownloadAsync(
        string sourceUri,
        string requestedName,
        string saveDirectory,
        int splitCount,
        string? referer = null,
        string? cookie = null,
        CancellationToken cancellationToken = default)
    {
        Ed2kFileLink? ed2kLink = null;
        if (Ed2kLinkParser.IsEd2kLink(sourceUri))
        {
            ed2kLink = Ed2kLinkParser.ParseFileLink(sourceUri);
            if (!await _rpcClient.SupportsEd2kAsync(cancellationToken))
            {
                throw new NotSupportedException("The active aria2 engine does not support ED2K.");
            }
        }

        string name = ResolveTaskName(sourceUri, requestedName);
        string? outputFileName = string.IsNullOrWhiteSpace(requestedName) ? null : name;
        string gid = await _rpcClient.AddUriAsync(sourceUri, outputFileName, saveDirectory, splitCount, referer, cookie, cancellationToken);

        DownloadTask task = new()
        {
            Gid = gid,
            Name = name,
            SourceUri = sourceUri,
            SaveDirectory = saveDirectory,
            LocalFilePath = string.IsNullOrWhiteSpace(name) ? string.Empty : Path.Combine(saveDirectory, name),
            Status = "Waiting",
            IsPeerTransfer = IsPeerTransfer(sourceUri),
            IsMetadataTransfer = IsMetadataTransfer(sourceUri),
            IsAria2SessionAttached = true,
            Progress = 0
        };

        if (ed2kLink is not null)
        {
            task.TotalLength = ed2kLink.FileSize;
        }

        _tasks.Insert(0, task);
        SaveTaskCache();
        return task;
    }

    public async Task<DownloadTask> AddTorrentAsync(
        byte[] torrentBytes,
        string torrentPath,
        TorrentMetadata metadata,
        string saveDirectory,
        int splitCount,
        IReadOnlyList<int> selectedFileIndexes,
        CancellationToken cancellationToken = default)
    {
        string gid = await _rpcClient.AddTorrentAsync(torrentBytes, saveDirectory, splitCount, selectedFileIndexes, cancellationToken);
        string name = string.IsNullOrWhiteSpace(metadata.Name)
            ? Path.GetFileNameWithoutExtension(torrentPath)
            : metadata.Name;

        DownloadTask task = new()
        {
            Gid = gid,
            Name = name,
            SourceUri = torrentPath,
            SaveDirectory = saveDirectory,
            LocalFilePath = string.IsNullOrWhiteSpace(name) ? string.Empty : Path.Combine(saveDirectory, name),
            Status = "Waiting",
            IsPeerTransfer = true,
            IsMetadataTransfer = false,
            IsAria2SessionAttached = true,
            TotalLength = metadata.Files
                .Where(file => selectedFileIndexes.Count == 0 || selectedFileIndexes.Contains(file.Index))
                .Sum(file => file.Length),
            Progress = 0
        };

        _tasks.Insert(0, task);
        SaveTaskCache();
        return task;
    }

    public void ClearTaskCache()
    {
        if (File.Exists(_taskCachePath))
        {
            File.Delete(_taskCachePath);
        }

        _tasks.Clear();
    }

    public async Task<DownloadSnapshot> RefreshAsync(CancellationToken cancellationToken = default)
    {
        RemoveRemovedTasks();

        List<Aria2TaskStatus> remoteTasks = [];
        remoteTasks.AddRange(await _rpcClient.TellActiveAsync(cancellationToken));
        remoteTasks.AddRange(await _rpcClient.TellWaitingAsync(cancellationToken));
        remoteTasks.AddRange(await _rpcClient.TellStoppedAsync(cancellationToken));
        HashSet<string> remoteGids = remoteTasks
            .Select(task => task.Gid)
            .Where(gid => !string.IsNullOrWhiteSpace(gid))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (Aria2TaskStatus remoteTask in remoteTasks)
        {
            UpsertTask(remoteTask);
            if (IsCompletedStatus(remoteTask.Status))
            {
                await RemoveCompletedDownloadResultAsync(remoteTask.Gid, cancellationToken);
            }
        }

        ReconcileDetachedTasks(remoteGids);

        Aria2GlobalStat stat = await _rpcClient.GetGlobalStatAsync(cancellationToken);
        return new DownloadSnapshot(
            ActiveCount: ParseLong(stat.NumActive),
            DownloadSpeed: _tasks.Sum(task => task.DownloadSpeed),
            UploadSpeed: _tasks.Sum(task => task.UploadSpeed));
    }

    public async Task PauseAsync(IEnumerable<DownloadTask> tasks, CancellationToken cancellationToken = default)
    {
        int detachedCount = 0;
        int operatedCount = 0;
        foreach (DownloadTask task in tasks.Where(task => !string.IsNullOrWhiteSpace(task.Gid)))
        {
            if (!task.IsAria2SessionAttached)
            {
                detachedCount++;
                continue;
            }

            try
            {
                await _rpcClient.PauseAsync(task.Gid, cancellationToken);
            }
            catch (Exception ex) when (IsGidNotFoundException(ex))
            {
                MarkTaskDetached(task);
                detachedCount++;
                continue;
            }

            operatedCount++;
            task.Status = "Pausing";
            task.DownloadSpeed = 0;
            task.UploadSpeed = 0;
        }

        ThrowIfOnlyDetachedTasks(operatedCount, detachedCount);
    }

    public async Task ResumeAsync(IEnumerable<DownloadTask> tasks, CancellationToken cancellationToken = default)
    {
        int detachedCount = 0;
        int operatedCount = 0;
        foreach (DownloadTask task in tasks.Where(task => !string.IsNullOrWhiteSpace(task.Gid)))
        {
            if (!task.IsAria2SessionAttached)
            {
                detachedCount++;
                continue;
            }

            try
            {
                await _rpcClient.UnpauseAsync(task.Gid, cancellationToken);
            }
            catch (Exception ex) when (IsGidNotFoundException(ex))
            {
                MarkTaskDetached(task);
                detachedCount++;
                continue;
            }

            operatedCount++;
            task.Status = "Resuming";
        }

        ThrowIfOnlyDetachedTasks(operatedCount, detachedCount);
    }

    public async Task RecoverAsync(IEnumerable<DownloadTask> tasks, int splitCount = 16, CancellationToken cancellationToken = default)
    {
        foreach (DownloadTask task in tasks.Where(IsErrorTask).ToArray())
        {
            if (string.IsNullOrWhiteSpace(task.SourceUri))
            {
                throw new InvalidOperationException($"Task {task.Name} does not have a source URI to recover.");
            }

            string oldGid = task.Gid;
            string saveDirectory = ResolveRecoveryDirectory(task);
            string? outputFileName = string.IsNullOrWhiteSpace(task.Name) || task.Name.Equals(task.Gid, StringComparison.OrdinalIgnoreCase)
                ? null
                : task.Name;

            string newGid;
            TorrentMetadata? torrentMetadata = null;
            if (IsLocalTorrentSource(task.SourceUri))
            {
                byte[] torrentBytes = await File.ReadAllBytesAsync(task.SourceUri, cancellationToken);
                torrentMetadata = TorrentMetadataReader.Read(torrentBytes);
                newGid = await _rpcClient.AddTorrentAsync(
                    torrentBytes,
                    saveDirectory,
                    splitCount,
                    [],
                    cancellationToken);
            }
            else
            {
                newGid = await _rpcClient.AddUriAsync(
                    task.SourceUri,
                    outputFileName,
                    saveDirectory,
                    splitCount,
                    null,
                    null,
                    cancellationToken);
            }

            await RemoveCompletedDownloadResultAsync(oldGid, cancellationToken);

            task.Gid = newGid;
            if (torrentMetadata is not null)
            {
                task.Name = string.IsNullOrWhiteSpace(torrentMetadata.Name)
                    ? Path.GetFileNameWithoutExtension(task.SourceUri)
                    : torrentMetadata.Name;
                task.IsPeerTransfer = true;
                task.IsMetadataTransfer = false;
                task.TotalLength = torrentMetadata.Files.Sum(file => file.Length);
            }

            task.SaveDirectory = saveDirectory;
            task.LocalFilePath = string.IsNullOrWhiteSpace(task.Name) ? string.Empty : Path.Combine(saveDirectory, task.Name);
            task.Status = "Waiting";
            task.IsAria2SessionAttached = true;
            task.Progress = 0;
            task.CompletedLength = 0;
            task.DownloadSpeed = 0;
            task.UploadSpeed = 0;
        }

        SaveTaskCache();
        await SaveAria2SessionAsync(cancellationToken);
    }

    private void RemoveRemovedTasks()
    {
        DownloadTask[] removedTasks = _tasks
            .Where(task => task.Status.Contains("removed", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (removedTasks.Length == 0)
        {
            return;
        }

        foreach (DownloadTask task in removedTasks)
        {
            _tasks.Remove(task);
        }

        SaveTaskCache();
    }

    public async Task DeleteAsync(IEnumerable<DownloadTask> tasks, bool deleteFiles = false, CancellationToken cancellationToken = default)
    {
        foreach (DownloadTask task in tasks.Where(task => !string.IsNullOrWhiteSpace(task.Gid)).ToArray())
        {
            await RemoveTaskFromAria2Async(task, cancellationToken);

            if (deleteFiles)
            {
                DeleteLocalFiles(task);
            }

            _tasks.Remove(task);
        }

        SaveTaskCache();
        await SaveAria2SessionAsync(cancellationToken);
    }

    private async Task RemoveTaskFromAria2Async(DownloadTask task, CancellationToken cancellationToken)
    {
        if (!task.IsAria2SessionAttached)
        {
            return;
        }

        if (IsResultOnlyStatus(task.Status))
        {
            try
            {
                await _rpcClient.RemoveDownloadResultAsync(task.Gid, cancellationToken);
                return;
            }
            catch
            {
                // Completed aria2 records may already be gone; the local list can still forget them.
                return;
            }
        }

        Exception? removeError = null;
        try
        {
            await _rpcClient.RemoveAsync(task.Gid, cancellationToken);
            return;
        }
        catch (Exception ex)
        {
            removeError = ex;
        }

        try
        {
            await _rpcClient.RemoveDownloadResultAsync(task.Gid, cancellationToken);
        }
        catch when (IsResultOnlyStatus(task.Status))
        {
        }
        catch
        {
            throw removeError;
        }
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
        await SaveAria2SessionAsync(cancellationToken);
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

    /// <summary>
    /// On startup, unconditionally purge all completed / error / removed download
    /// results from aria2's session, regardless of what the local task cache holds.
    /// This prevents stale results from resurrecting tasks that were cleared during
    /// a previous exit (where the RPC call may have failed silently).
    /// </summary>
    public async Task PurgeCompletedResultsFromAria2SessionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            IReadOnlyList<Aria2TaskStatus> stoppedTasks = await _rpcClient.TellStoppedAsync(cancellationToken);
            foreach (Aria2TaskStatus stoppedTask in stoppedTasks)
            {
                if (string.IsNullOrWhiteSpace(stoppedTask.Gid))
                {
                    continue;
                }

                string normalizedStatus = NormalizeStatus(stoppedTask.Status);
                if (IsResultOnlyStatus(normalizedStatus))
                {
                    await RemoveCompletedDownloadResultAsync(stoppedTask.Gid, cancellationToken);
                }
            }
        }
        catch
        {
            // Best-effort: if TellStopped fails (e.g. aria2 not yet ready), the next
            // RefreshAsync will still remove individual completed results via UpsertTask.
        }
    }

    public async Task SetGlobalSpeedLimitsAsync(
        long downloadLimitBytesPerSecond,
        long uploadLimitBytesPerSecond,
        CancellationToken cancellationToken = default)
    {
        Dictionary<string, string> options = new()
        {
            ["max-overall-download-limit"] = FormatAria2SpeedLimit(downloadLimitBytesPerSecond),
            ["max-overall-upload-limit"] = FormatAria2SpeedLimit(uploadLimitBytesPerSecond)
        };

        await _rpcClient.ChangeGlobalOptionAsync(options, cancellationToken);

        Dictionary<string, string> appliedOptions = await _rpcClient.GetGlobalOptionAsync(cancellationToken);
        VerifyAppliedSpeedLimit(
            appliedOptions,
            "max-overall-download-limit",
            downloadLimitBytesPerSecond);
        VerifyAppliedSpeedLimit(
            appliedOptions,
            "max-overall-upload-limit",
            uploadLimitBytesPerSecond);
    }

    public Task SetTaskSpeedLimitsAsync(
        string gid,
        long downloadLimitBytesPerSecond,
        long uploadLimitBytesPerSecond,
        CancellationToken cancellationToken = default)
    {
        Dictionary<string, string> options = new()
        {
            ["max-download-limit"] = FormatAria2SpeedLimit(downloadLimitBytesPerSecond),
            ["max-upload-limit"] = FormatAria2SpeedLimit(uploadLimitBytesPerSecond)
        };

        return _rpcClient.ChangeOptionAsync(gid, options, cancellationToken);
    }

    public Task SetTaskDownloadSpeedLimitAsync(
        string gid,
        long downloadLimitBytesPerSecond,
        CancellationToken cancellationToken = default)
    {
        Dictionary<string, string> options = new()
        {
            ["max-download-limit"] = FormatAria2SpeedLimit(downloadLimitBytesPerSecond)
        };

        return _rpcClient.ChangeOptionAsync(gid, options, cancellationToken);
    }

    public Task SetTaskUploadSpeedLimitAsync(
        string gid,
        long uploadLimitBytesPerSecond,
        CancellationToken cancellationToken = default)
    {
        Dictionary<string, string> options = new()
        {
            ["max-upload-limit"] = FormatAria2SpeedLimit(uploadLimitBytesPerSecond)
        };

        return _rpcClient.ChangeOptionAsync(gid, options, cancellationToken);
    }

    public Task<Dictionary<string, string>> GetTaskOptionsAsync(
        string gid,
        CancellationToken cancellationToken = default)
    {
        return _rpcClient.GetOptionAsync(gid, cancellationToken);
    }

    public Task SetGlobalDownloadSettingsAsync(
        int maxConcurrentDownloads,
        CancellationToken cancellationToken = default)
    {
        Dictionary<string, string> options = new()
        {
            ["max-concurrent-downloads"] = Math.Clamp(maxConcurrentDownloads, 1, 10).ToString(CultureInfo.InvariantCulture)
        };

        return _rpcClient.ChangeGlobalOptionAsync(options, cancellationToken);
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

        task.IsAria2SessionAttached = true;
        long totalLength = ParseLong(remoteTask.TotalLength);
        long completedLength = ParseLong(remoteTask.CompletedLength);
        string normalizedStatus = NormalizeStatus(remoteTask.Status);
        bool isDownloading = normalizedStatus.Contains("download", StringComparison.OrdinalIgnoreCase);
        if (ShouldPersistPendingStatus(task.Status, normalizedStatus))
        {
            task.Status = normalizedStatus;
        }
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
        task.ErrorCode = remoteTask.ErrorCode;
        task.ErrorMessage = remoteTask.ErrorMessage;
        task.IsPeerTransfer = IsPeerTransfer(remoteTask);
        task.IsMetadataTransfer = IsMetadataTransfer(remoteTask);
        task.Progress = task.TotalLength <= 0 ? task.Progress : Math.Clamp(task.CompletedLength * 100d / task.TotalLength, 0, 100);

        string resolvedRemoteName = ResolveRemoteName(remoteTask);
        if (!string.IsNullOrWhiteSpace(resolvedRemoteName) &&
            (string.IsNullOrWhiteSpace(task.Name) ||
                task.Name.Equals(task.Gid, StringComparison.OrdinalIgnoreCase) ||
                IsPlaceholderTaskName(task.Name)))
        {
            task.Name = resolvedRemoteName;
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
                (string.IsNullOrWhiteSpace(task.Name) ||
                    task.Name.Equals(task.Gid, StringComparison.OrdinalIgnoreCase) ||
                    IsPlaceholderTaskName(task.Name)))
            {
                task.Name = remoteName;
            }
        }

        if (normalizedStatus.Contains("complete", StringComparison.OrdinalIgnoreCase))
        {
            DeleteControlFiles(task, remoteTask);
            if (DeleteTorrentAfterComplete)
            {
                TryDeleteTorrentSourceFile(task.SourceUri);
            }
        }

        SaveTaskCache();
    }

    private void ReconcileDetachedTasks(HashSet<string> remoteGids)
    {
        bool changed = false;
        foreach (DownloadTask task in _tasks)
        {
            if (string.IsNullOrWhiteSpace(task.Gid))
            {
                continue;
            }

            bool isAttached = remoteGids.Contains(task.Gid);
            if (task.IsAria2SessionAttached != isAttached)
            {
                task.IsAria2SessionAttached = isAttached;
                changed = true;
            }

            if (!isAttached)
            {
                task.DownloadSpeed = 0;
                task.UploadSpeed = 0;
            }
        }

        if (changed)
        {
            SaveTaskCache();
        }
    }

    private static string NormalizeCachedStatus(string status)
    {
        if (status.Equals("Pausing", StringComparison.OrdinalIgnoreCase) ||
            status.Equals("Resuming", StringComparison.OrdinalIgnoreCase))
        {
            return "Waiting";
        }

        return status;
    }

    private static void MarkTaskDetached(DownloadTask task)
    {
        task.IsAria2SessionAttached = false;
        task.DownloadSpeed = 0;
        task.UploadSpeed = 0;
    }

    private static bool ShouldPersistPendingStatus(string currentStatus, string remoteStatus)
    {
        if (currentStatus.Equals("Pausing", StringComparison.OrdinalIgnoreCase))
        {
            // Keep "Pausing" only when aria2 still reports the pre-pause state,
            // meaning the pause hasn't taken effect yet.
            if (remoteStatus.Contains("download", StringComparison.OrdinalIgnoreCase) ||
                remoteStatus.Contains("waiting", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        if (currentStatus.Equals("Resuming", StringComparison.OrdinalIgnoreCase))
        {
            // Keep "Resuming" only when aria2 still reports "Paused",
            // meaning the resume hasn't taken effect yet.
            if (remoteStatus.Contains("paus", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        return true;
    }

    private static void ThrowIfOnlyDetachedTasks(int operatedCount, int detachedCount)
    {
        if (operatedCount == 0 && detachedCount > 0)
        {
            throw new InvalidOperationException("选中的任务已不在 aria2 会话中，任务记录已保留。");
        }
    }

    private static bool IsGidNotFoundException(Exception ex)
    {
        return ex.Message.Contains("GID", StringComparison.OrdinalIgnoreCase) &&
            ex.Message.Contains("is not found", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveTaskName(string sourceUri, string requestedName)
    {
        if (!string.IsNullOrWhiteSpace(requestedName))
        {
            return requestedName.Trim();
        }

        if (sourceUri.StartsWith("magnet:", StringComparison.OrdinalIgnoreCase))
        {
            string magnetName = ResolveMagnetDisplayName(sourceUri);
            if (!string.IsNullOrWhiteSpace(magnetName))
            {
                return magnetName;
            }

            string infoHash = ResolveMagnetInfoHash(sourceUri);
            if (!string.IsNullOrWhiteSpace(infoHash))
            {
                return $"Magnet {infoHash[..Math.Min(infoHash.Length, 12)]}";
            }
        }

        if (Ed2kLinkParser.TryParseFileLink(sourceUri, out Ed2kFileLink? ed2kLink))
        {
            return ed2kLink!.DisplayName;
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
        if (!string.IsNullOrWhiteSpace(task.Ed2k?.Name))
        {
            return task.Ed2k.Name.Trim();
        }

        string torrentName = ResolveBitTorrentName(task);
        if (!string.IsNullOrWhiteSpace(torrentName))
        {
            return torrentName;
        }

        string path = task.Files
            .FirstOrDefault(file => !string.IsNullOrWhiteSpace(file.Path) && !IsMetadataPath(file.Path))
            ?.Path ?? string.Empty;
        string fileName = Path.GetFileName(path);
        return string.IsNullOrWhiteSpace(fileName) ? string.Empty : fileName;
    }

    private static string ResolveBitTorrentName(Aria2TaskStatus task)
    {
        if (!task.BitTorrent.HasValue ||
            task.BitTorrent.Value.ValueKind is not JsonValueKind.Object ||
            !task.BitTorrent.Value.TryGetProperty("info", out JsonElement info) ||
            info.ValueKind is not JsonValueKind.Object ||
            !info.TryGetProperty("name", out JsonElement name) ||
            name.ValueKind is not JsonValueKind.String)
        {
            return string.Empty;
        }

        return name.GetString()?.Trim() ?? string.Empty;
    }

    private static string ResolveMagnetDisplayName(string sourceUri)
    {
        int queryStart = sourceUri.IndexOf('?');
        if (queryStart < 0 || queryStart == sourceUri.Length - 1)
        {
            return string.Empty;
        }

        foreach (string part in sourceUri[(queryStart + 1)..].Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            int separator = part.IndexOf('=');
            string key = separator >= 0 ? part[..separator] : part;
            if (!key.Equals("dn", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string value = separator >= 0 ? part[(separator + 1)..] : string.Empty;
            return DecodeQueryValue(value).Trim();
        }

        return string.Empty;
    }

    private static string ResolveMagnetInfoHash(string sourceUri)
    {
        int queryStart = sourceUri.IndexOf('?');
        if (queryStart < 0 || queryStart == sourceUri.Length - 1)
        {
            return string.Empty;
        }

        foreach (string part in sourceUri[(queryStart + 1)..].Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            int separator = part.IndexOf('=');
            string key = separator >= 0 ? part[..separator] : part;
            if (!key.Equals("xt", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string value = separator >= 0 ? DecodeQueryValue(part[(separator + 1)..]) : string.Empty;
            const string prefix = "urn:btih:";
            if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return value[prefix.Length..].Trim();
            }
        }

        return string.Empty;
    }

    private static string DecodeQueryValue(string value)
    {
        try
        {
            return Uri.UnescapeDataString(value.Replace('+', ' '));
        }
        catch
        {
            return value;
        }
    }

    private static string ResolveRemotePath(Aria2TaskStatus task)
    {
        return task.Files
            .FirstOrDefault(file => !string.IsNullOrWhiteSpace(file.Path) && !IsMetadataPath(file.Path))
            ?.Path ?? string.Empty;
    }

    private static string ResolveRemoteUri(Aria2TaskStatus task)
    {
        if (!string.IsNullOrWhiteSpace(task.Ed2k?.Ed2kLink))
        {
            return task.Ed2k.Ed2kLink.Trim();
        }

        return task.Files
            .SelectMany(file => file.Uris)
            .Select(uri => uri.Uri)
            .FirstOrDefault(uri => !string.IsNullOrWhiteSpace(uri)) ?? string.Empty;
    }

    private static bool IsPlaceholderTaskName(string name)
    {
        return name.Equals("New download", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("Magnet ", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsMetadataPath(string path)
    {
        string fileName = Path.GetFileName(path);
        return fileName.StartsWith("[METADATA]", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPeerTransfer(Aria2TaskStatus task)
    {
        if (task.BitTorrent.HasValue || task.Ed2k is not null)
        {
            return true;
        }

        string sourceUri = ResolveRemoteUri(task);
        return IsPeerTransfer(sourceUri);
    }

    private static bool IsPeerTransfer(string sourceUri)
    {
        return sourceUri.StartsWith("magnet:", StringComparison.OrdinalIgnoreCase) ||
            Ed2kLinkParser.IsEd2kLink(sourceUri);
    }

    private static bool IsMetadataTransfer(Aria2TaskStatus task)
    {
        return task.Files.Any(file => IsMetadataPath(file.Path));
    }

    private static bool IsMetadataTransfer(string sourceUri)
    {
        return sourceUri.StartsWith("magnet:", StringComparison.OrdinalIgnoreCase);
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

    private static string FormatAria2SpeedLimit(long bytesPerSecond)
    {
        return Math.Max(bytesPerSecond, 0).ToString(CultureInfo.InvariantCulture);
    }

    private static void VerifyAppliedSpeedLimit(
        IReadOnlyDictionary<string, string> options,
        string optionName,
        long expectedBytesPerSecond)
    {
        if (!options.TryGetValue(optionName, out string? value))
        {
            throw new InvalidOperationException($"aria2 did not report global option {optionName} after applying speed limits.");
        }

        long actualBytesPerSecond = ParseAria2SpeedLimit(value);
        long expected = Math.Max(expectedBytesPerSecond, 0);
        if (actualBytesPerSecond != expected)
        {
            throw new InvalidOperationException(
                $"aria2 reported {optionName}={value}, expected {FormatAria2SpeedLimit(expected)}.");
        }
    }

    private static long ParseAria2SpeedLimit(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        string trimmed = value.Trim();
        long multiplier = 1;
        char suffix = trimmed[^1];
        if (suffix is 'K' or 'k')
        {
            multiplier = 1024L;
            trimmed = trimmed[..^1];
        }
        else if (suffix is 'M' or 'm')
        {
            multiplier = 1024L * 1024L;
            trimmed = trimmed[..^1];
        }

        return double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed)
            ? Math.Max(0, (long)Math.Round(parsed * multiplier))
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

    private static void TryDeleteTorrentSourceFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path) ||
            !path.EndsWith(".torrent", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        try
        {
            DeleteFileIfExists(path);
        }
        catch
        {
            // A source torrent might be outside accessible storage or still open.
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

            foreach (CachedDownloadTask cachedTask in cachedTasks.Where(task =>
                !string.IsNullOrWhiteSpace(task.Gid) &&
                !task.Status.Contains("removed", StringComparison.OrdinalIgnoreCase)))
            {
                _tasks.Add(new DownloadTask
                {
                    Gid = cachedTask.Gid,
                    Name = cachedTask.Name,
                    SourceUri = cachedTask.SourceUri,
                    SaveDirectory = cachedTask.SaveDirectory,
                    LocalFilePath = cachedTask.LocalFilePath,
                    Status = NormalizeCachedStatus(cachedTask.Status),
                    Progress = cachedTask.Progress,
                    CompletedLength = cachedTask.CompletedLength,
                    TotalLength = cachedTask.TotalLength,
                    IsPeerTransfer = cachedTask.IsPeerTransfer,
                    IsMetadataTransfer = cachedTask.IsMetadataTransfer,
                    IsAria2SessionAttached = false,
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
                .Where(task =>
                    !string.IsNullOrWhiteSpace(task.Gid) &&
                    !task.Status.Contains("removed", StringComparison.OrdinalIgnoreCase))
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
                    task.IsMetadataTransfer,
                    task.CreatedAt))
                .ToList();

            File.WriteAllText(_taskCachePath, JsonSerializer.Serialize(cachedTasks, new JsonSerializerOptions
            {
                WriteIndented = true
            }));
        }
        catch (Exception ex)
        {
            AppLogger.Warning("TaskCache", $"SaveTaskCache failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Directly reads <c>tasks.json</c>, removes all completed entries, and writes it back.
    /// Used as a safety net on exit to guarantee the cache file is clean even if
    /// <see cref="SaveTaskCache"/> failed silently.
    /// </summary>
    public void PurgeCompletedTasksFromCacheFile()
    {
        if (!File.Exists(_taskCachePath))
        {
            return;
        }

        try
        {
            string json = File.ReadAllText(_taskCachePath);
            List<CachedDownloadTask>? cached = JsonSerializer.Deserialize<List<CachedDownloadTask>>(json);
            if (cached is null)
            {
                return;
            }

            List<CachedDownloadTask> filtered = cached
                .Where(entry => !IsCompletedStatus(entry.Status))
                .ToList();

            if (filtered.Count == cached.Count)
            {
                return; // Nothing to remove.
            }

            AppLogger.Info("TaskCache", $"PurgeCompleted: removing {cached.Count - filtered.Count} completed entries from tasks.json");
            File.WriteAllText(_taskCachePath, JsonSerializer.Serialize(filtered, new JsonSerializerOptions
            {
                WriteIndented = true
            }));
        }
        catch (Exception ex)
        {
            AppLogger.Warning("TaskCache", $"PurgeCompletedTasksFromCacheFile failed: {ex.Message}");
        }
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

    private async Task SaveAria2SessionAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _rpcClient.SaveSessionAsync(cancellationToken);
        }
        catch
        {
            // Session persistence is best-effort; startup cleanup will still reconcile stale entries.
        }
    }

    private static bool IsCompletedStatus(string status)
    {
        return NormalizeStatus(status).Contains("complete", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsResultOnlyStatus(string status)
    {
        string normalizedStatus = NormalizeStatus(status);
        return normalizedStatus.Contains("complete", StringComparison.OrdinalIgnoreCase)
            || normalizedStatus.Contains("error", StringComparison.OrdinalIgnoreCase)
            || normalizedStatus.Contains("removed", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsErrorTask(DownloadTask task)
    {
        return task.Status.Contains("error", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsLocalTorrentSource(string sourceUri)
    {
        return !string.IsNullOrWhiteSpace(sourceUri) &&
            sourceUri.EndsWith(".torrent", StringComparison.OrdinalIgnoreCase) &&
            File.Exists(sourceUri);
    }

    private static string ResolveRecoveryDirectory(DownloadTask task)
    {
        if (!string.IsNullOrWhiteSpace(task.SaveDirectory))
        {
            return task.SaveDirectory;
        }

        string? localDirectory = Path.GetDirectoryName(task.LocalFilePath);
        if (!string.IsNullOrWhiteSpace(localDirectory))
        {
            return localDirectory;
        }

        return AppPaths.DefaultDownloadDirectory;
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
    bool IsMetadataTransfer,
    DateTimeOffset CreatedAt);
