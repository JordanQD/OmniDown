using OmniDown.Models;
using OmniDown.Services.Rpc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OmniDown.Services.Downloads;

public sealed class DownloadCoordinator
{
    private readonly Aria2RpcClient _rpcClient;
    private readonly ObservableCollection<DownloadTask> _tasks;

    public DownloadCoordinator(Aria2RpcClient rpcClient, ObservableCollection<DownloadTask> tasks)
    {
        _rpcClient = rpcClient;
        _tasks = tasks;
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
            Progress = 0
        };

        _tasks.Insert(0, task);
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
    }

    private void UpsertTask(Aria2TaskStatus remoteTask)
    {
        if (NormalizeStatus(remoteTask.Status).Equals("Removed", StringComparison.OrdinalIgnoreCase))
        {
            DownloadTask? removedTask = _tasks.FirstOrDefault(item => item.Gid == remoteTask.Gid);
            if (removedTask is not null)
            {
                _tasks.Remove(removedTask);
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
        task.Status = NormalizeStatus(remoteTask.Status);
        task.TotalLength = totalLength;
        task.CompletedLength = completedLength;
        task.DownloadSpeed = ParseLong(remoteTask.DownloadSpeed);
        task.UploadSpeed = ParseLong(remoteTask.UploadSpeed);
        task.Progress = totalLength <= 0 ? 0 : Math.Clamp(completedLength * 100d / totalLength, 0, 100);

        if (string.IsNullOrWhiteSpace(task.Name))
        {
            task.Name = ResolveRemoteName(remoteTask);
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
        }
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
        return string.IsNullOrWhiteSpace(fileName) ? task.Gid : fileName;
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

    private static void DeleteFileIfExists(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return;
        }

        File.Delete(path);
    }
}

public sealed record DownloadSnapshot(long ActiveCount, long DownloadSpeed, long UploadSpeed);
