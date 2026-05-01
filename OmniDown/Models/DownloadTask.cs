namespace OmniDown.Models;

using OmniDown.Services.Localization;
using System.ComponentModel;
using System.Runtime.CompilerServices;

public sealed class DownloadTask : INotifyPropertyChanged
{
    private string _gid = string.Empty;
    private string _name = string.Empty;
    private string _sourceUri = string.Empty;
    private string _saveDirectory = string.Empty;
    private string _status = "Waiting";
    private double _progress;
    private long _completedLength;
    private long _totalLength;
    private long _downloadSpeed;

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
        set => SetProperty(ref _progress, value);
    }

    public long CompletedLength
    {
        get => _completedLength;
        set => SetProperty(ref _completedLength, value);
    }

    public long TotalLength
    {
        get => _totalLength;
        set => SetProperty(ref _totalLength, value);
    }

    public long DownloadSpeed
    {
        get => _downloadSpeed;
        set => SetProperty(ref _downloadSpeed, value);
    }

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
}
