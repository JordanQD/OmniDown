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
    private bool _isMetadataTransfer;
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
                OnPropertyChanged(nameof(RemainingTimeText));
                OnPropertyChanged(nameof(IsPaused));
                OnPropertyChanged(nameof(IsError));
                OnPropertyChanged(nameof(ToggleActionGlyph));
                OnPropertyChanged(nameof(ToggleActionText));
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
                OnPropertyChanged(nameof(RemainingTimeText));
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
        set
        {
            if (SetProperty(ref _completedLength, value))
            {
                OnPropertyChanged(nameof(TransferredSizeText));
                OnPropertyChanged(nameof(RemainingTimeText));
            }
        }
    }

    public long TotalLength
    {
        get => _totalLength;
        set
        {
            if (SetProperty(ref _totalLength, value))
            {
                OnPropertyChanged(nameof(SizeText));
                OnPropertyChanged(nameof(TransferredSizeText));
                OnPropertyChanged(nameof(RemainingTimeText));
            }
        }
    }

    public string SizeText => TotalLength <= 0 ? "-" : FormatBytes(TotalLength);

    public string TransferredSizeText => TotalLength <= 0
        ? $"{FormatBytes(CompletedLength)} / -"
        : $"{FormatBytes(CompletedLength)} / {FormatBytes(TotalLength)}";

    public DateTimeOffset CreatedAt
    {
        get => _createdAt;
        set => SetProperty(ref _createdAt, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
            {
                OnPropertyChanged(nameof(TaskItemBackground));
                OnPropertyChanged(nameof(TaskItemBorderBrush));
                OnPropertyChanged(nameof(SelectionIndicatorOpacity));
                OnPropertyChanged(nameof(SelectionCheckboxOpacity));
                OnPropertyChanged(nameof(TaskIconOpacity));
            }
        }
    }

    public Brush TaskItemBackground => IsSelected
        ? GetResourceBrush("ControlFillColorDefaultBrush", DefaultProgressBrush)
        : new SolidColorBrush(Colors.Transparent);

    public Brush TaskItemBorderBrush => IsSelected
        ? GetResourceBrush("CardStrokeColorDefaultBrush", DefaultProgressBrush)
        : new SolidColorBrush(Colors.Transparent);

    public double SelectionIndicatorOpacity => IsSelected ? 1 : 0;

    public double SelectionCheckboxOpacity => IsSelected ? 1 : 0;

    public double TaskIconOpacity => IsSelected ? 0 : 1;

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
                OnPropertyChanged(nameof(RemainingTimeText));
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

    public bool IsMetadataTransfer
    {
        get => _isMetadataTransfer;
        set => SetProperty(ref _isMetadataTransfer, value);
    }

    public Visibility NormalSpeedVisibility => IsPeerTransfer ? Visibility.Collapsed : Visibility.Visible;

    public Visibility PeerSpeedVisibility => IsPeerTransfer ? Visibility.Visible : Visibility.Collapsed;

    public bool IsPaused => Status.Contains("paused", StringComparison.OrdinalIgnoreCase);

    public bool IsError => Status.Contains("error", StringComparison.OrdinalIgnoreCase);

    public string ToggleActionGlyph => IsError
        ? "\uE72C"
        : IsPaused ? "\uE768" : "\uE769";

    public string ToggleActionText => IsError
        ? Strings.Get("RecoverTasksButton.Label")
        : IsPaused
        ? Strings.Get("ResumeTasksButton.Label")
        : Strings.Get("PauseTasksButton.Label");

    public string OpenFileActionText => Strings.Get("TaskOpenFileActionText");

    public string OpenFolderActionText => Strings.Get("TaskOpenFolderActionText");

    public string CopyLinkActionText => Strings.Get("TaskCopyLinkActionText");

    public string DeleteEntryActionText => Strings.Get("TaskDeleteEntryActionText");

    public string DownloadSpeedText => FormatSpeed(DownloadSpeed);

    public string UploadSpeedText => FormatSpeed(UploadSpeed);

    public string RemainingTimeText => FormatRemainingTime();

    public string SpeedText => IsPeerTransfer
        ? $"{Strings.Get("UploadSpeedPrefix")} {UploadSpeedText}  {Strings.Get("DownloadSpeedPrefix")} {DownloadSpeedText}"
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

    private string FormatRemainingTime()
    {
        if (Status.Contains("complete", StringComparison.OrdinalIgnoreCase))
        {
            return $"0 {Strings.Get("RemainingTimeSecondsUnit")}";
        }

        if (!Status.Contains("download", StringComparison.OrdinalIgnoreCase) ||
            TotalLength <= 0 ||
            CompletedLength < 0 ||
            CompletedLength >= TotalLength ||
            DownloadSpeed <= 0)
        {
            return "-";
        }

        long remainingSeconds = (long)Math.Ceiling((TotalLength - CompletedLength) / (double)DownloadSpeed);
        TimeSpan remaining = TimeSpan.FromSeconds(Math.Max(remainingSeconds, 0));
        if (remaining.TotalDays >= 1)
        {
            return $"{(int)remaining.TotalDays} {Strings.Get("RemainingTimeDaysUnit")} {remaining.Hours} {Strings.Get("RemainingTimeHoursUnit")}";
        }

        if (remaining.TotalHours >= 1)
        {
            return $"{(int)remaining.TotalHours} {Strings.Get("RemainingTimeHoursUnit")} {remaining.Minutes} {Strings.Get("RemainingTimeMinutesUnit")}";
        }

        if (remaining.TotalMinutes >= 1)
        {
            return $"{(int)remaining.TotalMinutes} {Strings.Get("RemainingTimeMinutesUnit")} {remaining.Seconds} {Strings.Get("RemainingTimeSecondsUnit")}";
        }

        return $"{remaining.Seconds} {Strings.Get("RemainingTimeSecondsUnit")}";
    }

    private static Brush GetResourceBrush(string key, Brush fallback)
    {
        return Application.Current.Resources.TryGetValue(key, out object value) && value is Brush brush
            ? brush
            : fallback;
    }
}
