namespace OmniDown.Models;

using OmniDown.Services.Localization;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

public sealed class DownloadTask : INotifyPropertyChanged
{
    private string _gid = string.Empty;
    private string _name = string.Empty;
    private string _sourceUri = string.Empty;
    private string _saveDirectory = string.Empty;
    private string _localFilePath = string.Empty;
    private string _status = "Waiting";
    private double _progress;
    private long _completedLength;
    private long _totalLength;
    private long _downloadSpeed;
    private long _uploadSpeed;
    private DateTimeOffset _createdAt = DateTimeOffset.Now;
    private bool _isSelected;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Gid
    {
        get => _gid;
        set => SetProperty(ref _gid, value);
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string SourceUri
    {
        get => _sourceUri;
        set => SetProperty(ref _sourceUri, value);
    }

    public string SaveDirectory
    {
        get => _saveDirectory;
        set => SetProperty(ref _saveDirectory, value);
    }

    public string LocalFilePath
    {
        get => _localFilePath;
        set => SetProperty(ref _localFilePath, value);
    }

    public string Status
    {
        get => _status;
        set
        {
            if (SetProperty(ref _status, value))
            {
                OnPropertyChanged(nameof(StatusText));
            }
        }
    }

    public string StatusText => Status.ToLowerInvariant() switch
    {
        "downloading" => Strings.Get("TaskStatusDownloading"),
        "waiting" => Strings.Get("TaskStatusWaiting"),
        "paused" => Strings.Get("TaskStatusPaused"),
        "completed" => Strings.Get("TaskStatusCompleted"),
        "error" => Strings.Get("TaskStatusError"),
        "removed" => Strings.Get("TaskStatusRemoved"),
        _ => Status
    };

    public double Progress
    {
        get => _progress;
        set
        {
            if (SetProperty(ref _progress, value))
            {
                OnPropertyChanged(nameof(ProgressText));
            }
        }
    }

    public string ProgressText => $"{Progress:0}%";

    public long CompletedLength
    {
        get => _completedLength;
        set => SetProperty(ref _completedLength, value);
    }

    public long TotalLength
    {
        get => _totalLength;
        set
        {
            if (SetProperty(ref _totalLength, value))
            {
                OnPropertyChanged(nameof(SizeText));
            }
        }
    }

    public string SizeText => TotalLength <= 0 ? "-" : FormatBytes(TotalLength);

    public DateTimeOffset CreatedAt
    {
        get => _createdAt;
        set => SetProperty(ref _createdAt, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public long DownloadSpeed
    {
        get => _downloadSpeed;
        set
        {
            if (SetProperty(ref _downloadSpeed, value))
            {
                OnPropertyChanged(nameof(SpeedText));
                OnPropertyChanged(nameof(CombinedSpeed));
            }
        }
    }

    public long UploadSpeed
    {
        get => _uploadSpeed;
        set
        {
            if (SetProperty(ref _uploadSpeed, value))
            {
                OnPropertyChanged(nameof(SpeedText));
                OnPropertyChanged(nameof(CombinedSpeed));
            }
        }
    }

    public long CombinedSpeed => DownloadSpeed + UploadSpeed;

    public string SpeedText => $"{Strings.Get("DownloadSpeedPrefix")} {FormatSpeed(DownloadSpeed)}  {Strings.Get("UploadSpeedPrefix")} {FormatSpeed(UploadSpeed)}";

    private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(storage, value))
        {
            return false;
        }

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private static string FormatSpeed(long bytesPerSecond)
    {
        string[] units = ["B/s", "KB/s", "MB/s", "GB/s"];
        double speed = bytesPerSecond;
        int unitIndex = 0;
        while (speed >= 1024 && unitIndex < units.Length - 1)
        {
            speed /= 1024;
            unitIndex++;
        }

        return $"{speed:0.#} {units[unitIndex]}";
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double size = bytes;
        int unitIndex = 0;
        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        return $"{size:0.#} {units[unitIndex]}";
    }
}
