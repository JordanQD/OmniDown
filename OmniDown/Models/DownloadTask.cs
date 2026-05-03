namespace OmniDown.Models;

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using OmniDown.Services.Localization;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

public sealed class DownloadTask : INotifyPropertyChanged
{
    private static readonly SolidColorBrush DefaultNameBrush = new(Colors.Black);
    private static readonly SolidColorBrush DefaultProgressBrush = new(Colors.DodgerBlue);
    private static readonly SolidColorBrush SuccessBrush = new(Colors.ForestGreen);
    private static readonly SolidColorBrush CautionBrush = new(Colors.Goldenrod);
    private static readonly SolidColorBrush CriticalBrush = new(Colors.Firebrick);

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
    private bool _isPeerTransfer;
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
                OnPropertyChanged(nameof(StatusBrushKey));
                OnPropertyChanged(nameof(NameBrush));
                OnPropertyChanged(nameof(StatusBrush));
                OnPropertyChanged(nameof(ProgressBrush));
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

    public string StatusBrushKey => Status.ToLowerInvariant() switch
    {
        "completed" => "Success",
        "paused" => "Caution",
        "error" => "Critical",
        _ => "Default"
    };

    public Brush NameBrush => StatusBrushKey switch
    {
        "Success" => GetResourceBrush("SystemFillColorSuccessBrush", SuccessBrush),
        "Caution" => GetResourceBrush("SystemFillColorCautionBrush", CautionBrush),
        "Critical" => GetResourceBrush("SystemFillColorCriticalBrush", CriticalBrush),
        _ => GetResourceBrush("TextFillColorPrimaryBrush", DefaultNameBrush)
    };

    public Brush StatusBrush => StatusBrushKey switch
    {
        "Success" => GetResourceBrush("SystemFillColorSuccessBrush", SuccessBrush),
        "Caution" => GetResourceBrush("SystemFillColorCautionBrush", CautionBrush),
        "Critical" => GetResourceBrush("SystemFillColorCriticalBrush", CriticalBrush),
        _ => GetResourceBrush("TextFillColorSecondaryBrush", DefaultNameBrush)
    };

    public Brush ProgressBrush => StatusBrushKey switch
    {
        "Success" => GetResourceBrush("SystemFillColorSuccessBrush", SuccessBrush),
        "Caution" => GetResourceBrush("SystemFillColorCautionBrush", CautionBrush),
        "Critical" => GetResourceBrush("SystemFillColorCriticalBrush", CriticalBrush),
        _ => GetResourceBrush("AccentFillColorDefaultBrush", DefaultProgressBrush)
    };

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
                OnPropertyChanged(nameof(DownloadSpeedText));
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
                OnPropertyChanged(nameof(UploadSpeedText));
            }
        }
    }

    public long CombinedSpeed => DownloadSpeed + UploadSpeed;

    public bool IsPeerTransfer
    {
        get => _isPeerTransfer;
        set
        {
            if (SetProperty(ref _isPeerTransfer, value))
            {
                OnPropertyChanged(nameof(SpeedText));
                OnPropertyChanged(nameof(NormalSpeedVisibility));
                OnPropertyChanged(nameof(PeerSpeedVisibility));
            }
        }
    }

    public Visibility NormalSpeedVisibility => IsPeerTransfer ? Visibility.Collapsed : Visibility.Visible;

    public Visibility PeerSpeedVisibility => IsPeerTransfer ? Visibility.Visible : Visibility.Collapsed;

    public string DownloadSpeedText => FormatSpeed(DownloadSpeed);

    public string UploadSpeedText => FormatSpeed(UploadSpeed);

    public string SpeedText => IsPeerTransfer
        ? $"{Strings.Get("DownloadSpeedPrefix")} {DownloadSpeedText}  {Strings.Get("UploadSpeedPrefix")} {UploadSpeedText}"
        : DownloadSpeedText;

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

    private static Brush GetResourceBrush(string key, Brush fallback)
    {
        return Application.Current.Resources.TryGetValue(key, out object value) && value is Brush brush
            ? brush
            : fallback;
    }
}
