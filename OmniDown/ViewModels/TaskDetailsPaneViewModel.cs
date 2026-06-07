using Microsoft.UI.Xaml;
using OmniDown.Services.Localization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace OmniDown.ViewModels;

public enum SpeedLimitScope
{
    Global,
    Task
}

public enum SpeedLimitDirection
{
    Upload,
    Download
}

public sealed class SpeedLimitApplyRequestedEventArgs(
    SpeedLimitScope scope,
    SpeedLimitDirection direction,
    bool isEnabled,
    long bytesPerSecond) : EventArgs
{
    public SpeedLimitScope Scope { get; } = scope;
    public SpeedLimitDirection Direction { get; } = direction;
    public bool IsEnabled { get; } = isEnabled;
    public long BytesPerSecond { get; } = bytesPerSecond;
}

public sealed class TaskDetailsPaneViewModel : INotifyPropertyChanged
{
    private const string UnitKilobytes = "KB/s";
    private const string UnitMegabytes = "MB/s";

    private string _overviewItemCountText = string.Empty;
    private string _overviewActiveCountText = string.Empty;
    private string _overviewPausedCountText = string.Empty;
    private string _overviewIssueCountText = string.Empty;
    private string _overviewDownloadSpeedText = "0 B/s";
    private string _overviewUploadSpeedText = "0 B/s";
    private string _overviewDownloadLimitText = string.Empty;
    private string _overviewUploadLimitText = string.Empty;
    private Visibility _overviewDownloadLimitVisibility = Visibility.Collapsed;
    private Visibility _overviewUploadLimitVisibility = Visibility.Collapsed;
    private bool _overviewDownloadLimitEnabled;
    private bool _overviewUploadLimitEnabled;
    private double _overviewDownloadLimitValue = 1;
    private double _overviewUploadLimitValue = 1;
    private string _overviewDownloadLimitUnit = UnitKilobytes;
    private string _overviewUploadLimitUnit = UnitKilobytes;
    private string _taskDownloadSpeedText = "0 B/s";
    private string _taskUploadSpeedText = "0 B/s";
    private string _taskDownloadLimitText = string.Empty;
    private string _taskUploadLimitText = string.Empty;
    private Visibility _taskDownloadLimitVisibility = Visibility.Collapsed;
    private Visibility _taskUploadLimitVisibility = Visibility.Collapsed;
    private Visibility _taskUploadSectionVisibility = Visibility.Visible;
    private bool _taskDownloadLimitEnabled;
    private bool _taskUploadLimitEnabled;
    private double _taskDownloadLimitValue = 1;
    private double _taskUploadLimitValue = 1;
    private string _taskDownloadLimitUnit = UnitKilobytes;
    private string _taskUploadLimitUnit = UnitKilobytes;
    private bool _lastOverviewDownloadLimitEnabled;
    private bool _lastOverviewUploadLimitEnabled;
    private long _lastOverviewDownloadLimit = -1;
    private long _lastOverviewUploadLimit = -1;
    private bool _lastTaskDownloadLimitEnabled;
    private bool _lastTaskUploadLimitEnabled;
    private long _lastTaskDownloadLimit = -1;
    private long _lastTaskUploadLimit = -1;
    private bool _isOverviewUploadLimitEditorExpanded;
    private bool _isOverviewDownloadLimitEditorExpanded;
    private bool _isTaskUploadLimitEditorExpanded;
    private bool _isTaskDownloadLimitEditorExpanded;

    public TaskDetailsPaneViewModel()
    {
        ApplySpeedLimitCommand = new RelayCommand(ApplySpeedLimit);
        ToggleSpeedLimitEditorCommand = new RelayCommand(ToggleSpeedLimitEditor);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<SpeedLimitApplyRequestedEventArgs>? SpeedLimitApplyRequested;

    public ICommand ApplySpeedLimitCommand { get; }
    public ICommand ToggleSpeedLimitEditorCommand { get; }

    public string ItemCountLabel => Strings.Get("TaskDetailsItemCountLabel");
    public string TransferCountLabel => Strings.Get("TaskDetailsTransferCountLabel");
    public string PausedCountLabel => Strings.Get("TaskDetailsPausedCountLabel");
    public string IssueCountLabel => Strings.Get("TaskDetailsIssueCountLabel");
    public string DownloadSpeedLabel => Strings.Get("TaskDetailsDownloadSpeedLabel");
    public string UploadSpeedLabel => Strings.Get("TaskDetailsUploadSpeedLabel");
    public string UploadLimitHeader => Strings.Get("TaskDetailsUploadLimitHeader");
    public string DownloadLimitHeader => Strings.Get("TaskDetailsDownloadLimitHeader");
    public string SpeedLimitEnabledLabel => Strings.Get("TaskDetailsSpeedLimitEnabledLabel");
    public string SpeedLimitValueLabel => Strings.Get("TaskDetailsSpeedLimitValueLabel");
    public string SpeedLimitUnitLabel => Strings.Get("TaskDetailsSpeedLimitUnitLabel");
    public string SpeedLimitApplyText => Strings.Get("TaskDetailsSpeedLimitApplyText");
    public string LimitOffText => Strings.Get("TaskDetailsSpeedLimitOffText");

    public string OverviewItemCountText { get => _overviewItemCountText; private set => SetProperty(ref _overviewItemCountText, value); }
    public string OverviewActiveCountText { get => _overviewActiveCountText; private set => SetProperty(ref _overviewActiveCountText, value); }
    public string OverviewPausedCountText { get => _overviewPausedCountText; private set => SetProperty(ref _overviewPausedCountText, value); }
    public string OverviewIssueCountText { get => _overviewIssueCountText; private set => SetProperty(ref _overviewIssueCountText, value); }
    public string OverviewDownloadSpeedText { get => _overviewDownloadSpeedText; private set => SetProperty(ref _overviewDownloadSpeedText, value); }
    public string OverviewUploadSpeedText { get => _overviewUploadSpeedText; private set => SetProperty(ref _overviewUploadSpeedText, value); }
    public string OverviewDownloadLimitText { get => _overviewDownloadLimitText; private set => SetProperty(ref _overviewDownloadLimitText, value); }
    public string OverviewUploadLimitText { get => _overviewUploadLimitText; private set => SetProperty(ref _overviewUploadLimitText, value); }
    public Visibility OverviewDownloadLimitVisibility { get => _overviewDownloadLimitVisibility; private set => SetProperty(ref _overviewDownloadLimitVisibility, value); }
    public Visibility OverviewUploadLimitVisibility { get => _overviewUploadLimitVisibility; private set => SetProperty(ref _overviewUploadLimitVisibility, value); }
    public string OverviewDownloadLimitSummary => CreateLimitSummary(OverviewDownloadLimitEnabled, OverviewDownloadLimitValue, OverviewDownloadLimitUnit);
    public string OverviewUploadLimitSummary => CreateLimitSummary(OverviewUploadLimitEnabled, OverviewUploadLimitValue, OverviewUploadLimitUnit);
    public Visibility OverviewDownloadLimitEditorVisibility => IsOverviewDownloadLimitEditorExpanded ? Visibility.Visible : Visibility.Collapsed;
    public Visibility OverviewUploadLimitEditorVisibility => IsOverviewUploadLimitEditorExpanded ? Visibility.Visible : Visibility.Collapsed;
    public string OverviewDownloadLimitChevron => IsOverviewDownloadLimitEditorExpanded ? "\uE70E" : "\uE70D";
    public string OverviewUploadLimitChevron => IsOverviewUploadLimitEditorExpanded ? "\uE70E" : "\uE70D";
    public bool IsOverviewDownloadLimitEditorExpanded { get => _isOverviewDownloadLimitEditorExpanded; private set { if (SetProperty(ref _isOverviewDownloadLimitEditorExpanded, value)) NotifyEditorExpandedChanged(nameof(OverviewDownloadLimitEditorVisibility), nameof(OverviewDownloadLimitChevron)); } }
    public bool IsOverviewUploadLimitEditorExpanded { get => _isOverviewUploadLimitEditorExpanded; private set { if (SetProperty(ref _isOverviewUploadLimitEditorExpanded, value)) NotifyEditorExpandedChanged(nameof(OverviewUploadLimitEditorVisibility), nameof(OverviewUploadLimitChevron)); } }
    public bool OverviewDownloadLimitEnabled { get => _overviewDownloadLimitEnabled; set { if (SetProperty(ref _overviewDownloadLimitEnabled, value)) NotifyLimitInputsChanged(nameof(OverviewDownloadLimitSummary), nameof(IsOverviewDownloadLimitInputEnabled)); } }
    public bool OverviewUploadLimitEnabled { get => _overviewUploadLimitEnabled; set { if (SetProperty(ref _overviewUploadLimitEnabled, value)) NotifyLimitInputsChanged(nameof(OverviewUploadLimitSummary), nameof(IsOverviewUploadLimitInputEnabled)); } }
    public double OverviewDownloadLimitValue { get => _overviewDownloadLimitValue; set { if (SetProperty(ref _overviewDownloadLimitValue, NormalizeLimitValue(value))) OnPropertyChanged(nameof(OverviewDownloadLimitSummary)); } }
    public double OverviewUploadLimitValue { get => _overviewUploadLimitValue; set { if (SetProperty(ref _overviewUploadLimitValue, NormalizeLimitValue(value))) OnPropertyChanged(nameof(OverviewUploadLimitSummary)); } }
    public string OverviewDownloadLimitUnit { get => _overviewDownloadLimitUnit; set { if (SetProperty(ref _overviewDownloadLimitUnit, NormalizeUnit(value))) OnPropertyChanged(nameof(OverviewDownloadLimitSummary)); } }
    public string OverviewUploadLimitUnit { get => _overviewUploadLimitUnit; set { if (SetProperty(ref _overviewUploadLimitUnit, NormalizeUnit(value))) OnPropertyChanged(nameof(OverviewUploadLimitSummary)); } }
    public bool IsOverviewDownloadLimitInputEnabled => OverviewDownloadLimitEnabled;
    public bool IsOverviewUploadLimitInputEnabled => OverviewUploadLimitEnabled;

    public string TaskDownloadSpeedText { get => _taskDownloadSpeedText; private set => SetProperty(ref _taskDownloadSpeedText, value); }
    public string TaskUploadSpeedText { get => _taskUploadSpeedText; private set => SetProperty(ref _taskUploadSpeedText, value); }
    public string TaskDownloadLimitText { get => _taskDownloadLimitText; private set => SetProperty(ref _taskDownloadLimitText, value); }
    public string TaskUploadLimitText { get => _taskUploadLimitText; private set => SetProperty(ref _taskUploadLimitText, value); }
    public Visibility TaskDownloadLimitVisibility { get => _taskDownloadLimitVisibility; private set => SetProperty(ref _taskDownloadLimitVisibility, value); }
    public Visibility TaskUploadLimitVisibility { get => _taskUploadLimitVisibility; private set => SetProperty(ref _taskUploadLimitVisibility, value); }
    public Visibility TaskUploadSectionVisibility { get => _taskUploadSectionVisibility; private set => SetProperty(ref _taskUploadSectionVisibility, value); }
    public string TaskDownloadLimitSummary => CreateLimitSummary(TaskDownloadLimitEnabled, TaskDownloadLimitValue, TaskDownloadLimitUnit);
    public string TaskUploadLimitSummary => CreateLimitSummary(TaskUploadLimitEnabled, TaskUploadLimitValue, TaskUploadLimitUnit);
    public Visibility TaskDownloadLimitEditorVisibility => IsTaskDownloadLimitEditorExpanded ? Visibility.Visible : Visibility.Collapsed;
    public Visibility TaskUploadLimitEditorVisibility => IsTaskUploadLimitEditorExpanded ? Visibility.Visible : Visibility.Collapsed;
    public string TaskDownloadLimitChevron => IsTaskDownloadLimitEditorExpanded ? "\uE70E" : "\uE70D";
    public string TaskUploadLimitChevron => IsTaskUploadLimitEditorExpanded ? "\uE70E" : "\uE70D";
    public bool IsTaskDownloadLimitEditorExpanded { get => _isTaskDownloadLimitEditorExpanded; private set { if (SetProperty(ref _isTaskDownloadLimitEditorExpanded, value)) NotifyEditorExpandedChanged(nameof(TaskDownloadLimitEditorVisibility), nameof(TaskDownloadLimitChevron)); } }
    public bool IsTaskUploadLimitEditorExpanded { get => _isTaskUploadLimitEditorExpanded; private set { if (SetProperty(ref _isTaskUploadLimitEditorExpanded, value)) NotifyEditorExpandedChanged(nameof(TaskUploadLimitEditorVisibility), nameof(TaskUploadLimitChevron)); } }
    public bool TaskDownloadLimitEnabled { get => _taskDownloadLimitEnabled; set { if (SetProperty(ref _taskDownloadLimitEnabled, value)) NotifyLimitInputsChanged(nameof(TaskDownloadLimitSummary), nameof(IsTaskDownloadLimitInputEnabled)); } }
    public bool TaskUploadLimitEnabled { get => _taskUploadLimitEnabled; set { if (SetProperty(ref _taskUploadLimitEnabled, value)) NotifyLimitInputsChanged(nameof(TaskUploadLimitSummary), nameof(IsTaskUploadLimitInputEnabled)); } }
    public double TaskDownloadLimitValue { get => _taskDownloadLimitValue; set { if (SetProperty(ref _taskDownloadLimitValue, NormalizeLimitValue(value))) OnPropertyChanged(nameof(TaskDownloadLimitSummary)); } }
    public double TaskUploadLimitValue { get => _taskUploadLimitValue; set { if (SetProperty(ref _taskUploadLimitValue, NormalizeLimitValue(value))) OnPropertyChanged(nameof(TaskUploadLimitSummary)); } }
    public string TaskDownloadLimitUnit { get => _taskDownloadLimitUnit; set { if (SetProperty(ref _taskDownloadLimitUnit, NormalizeUnit(value))) OnPropertyChanged(nameof(TaskDownloadLimitSummary)); } }
    public string TaskUploadLimitUnit { get => _taskUploadLimitUnit; set { if (SetProperty(ref _taskUploadLimitUnit, NormalizeUnit(value))) OnPropertyChanged(nameof(TaskUploadLimitSummary)); } }
    public bool IsTaskDownloadLimitInputEnabled => TaskDownloadLimitEnabled;
    public bool IsTaskUploadLimitInputEnabled => TaskUploadLimitEnabled;

    public void UpdateOverview(
        int itemCount,
        int activeCount,
        int pausedCount,
        int issueCount,
        long downloadSpeed,
        long uploadSpeed,
        bool downloadLimitEnabled,
        long downloadLimit,
        bool uploadLimitEnabled,
        long uploadLimit)
    {
        OverviewItemCountText = FormatItemCount(itemCount);
        OverviewActiveCountText = Math.Max(activeCount, 0).ToString();
        OverviewPausedCountText = Math.Max(pausedCount, 0).ToString();
        OverviewIssueCountText = Math.Max(issueCount, 0).ToString();
        OverviewDownloadSpeedText = FormatSpeed(downloadSpeed);
        OverviewUploadSpeedText = FormatSpeed(uploadSpeed);
        ApplyDisplayLimit(downloadLimitEnabled, downloadLimit, value => OverviewDownloadLimitText = value, value => OverviewDownloadLimitVisibility = value);
        ApplyDisplayLimit(uploadLimitEnabled, uploadLimit, value => OverviewUploadLimitText = value, value => OverviewUploadLimitVisibility = value);

        if (downloadLimitEnabled != _lastOverviewDownloadLimitEnabled || downloadLimit != _lastOverviewDownloadLimit)
        {
            SetLimitEditor(downloadLimitEnabled, downloadLimit, value => OverviewDownloadLimitEnabled = value, value => OverviewDownloadLimitValue = value, value => OverviewDownloadLimitUnit = value);
            _lastOverviewDownloadLimitEnabled = downloadLimitEnabled;
            _lastOverviewDownloadLimit = downloadLimit;
        }

        if (uploadLimitEnabled != _lastOverviewUploadLimitEnabled || uploadLimit != _lastOverviewUploadLimit)
        {
            SetLimitEditor(uploadLimitEnabled, uploadLimit, value => OverviewUploadLimitEnabled = value, value => OverviewUploadLimitValue = value, value => OverviewUploadLimitUnit = value);
            _lastOverviewUploadLimitEnabled = uploadLimitEnabled;
            _lastOverviewUploadLimit = uploadLimit;
        }
    }

    public void UpdateTaskSpeeds(long downloadSpeed, long uploadSpeed, bool showsUpload)
    {
        TaskDownloadSpeedText = FormatSpeed(downloadSpeed);
        TaskUploadSpeedText = FormatSpeed(uploadSpeed);
        TaskUploadSectionVisibility = showsUpload ? Visibility.Visible : Visibility.Collapsed;
    }

    public void UpdateTaskLimits(
        bool downloadLimitEnabled,
        long downloadLimit,
        bool uploadLimitEnabled,
        long uploadLimit)
    {
        ApplyDisplayLimit(downloadLimitEnabled, downloadLimit, value => TaskDownloadLimitText = value, value => TaskDownloadLimitVisibility = value);
        ApplyDisplayLimit(uploadLimitEnabled, uploadLimit, value => TaskUploadLimitText = value, value => TaskUploadLimitVisibility = value);

        if (downloadLimitEnabled != _lastTaskDownloadLimitEnabled || downloadLimit != _lastTaskDownloadLimit)
        {
            SetLimitEditor(downloadLimitEnabled, downloadLimit, value => TaskDownloadLimitEnabled = value, value => TaskDownloadLimitValue = value, value => TaskDownloadLimitUnit = value);
            _lastTaskDownloadLimitEnabled = downloadLimitEnabled;
            _lastTaskDownloadLimit = downloadLimit;
        }

        if (uploadLimitEnabled != _lastTaskUploadLimitEnabled || uploadLimit != _lastTaskUploadLimit)
        {
            SetLimitEditor(uploadLimitEnabled, uploadLimit, value => TaskUploadLimitEnabled = value, value => TaskUploadLimitValue = value, value => TaskUploadLimitUnit = value);
            _lastTaskUploadLimitEnabled = uploadLimitEnabled;
            _lastTaskUploadLimit = uploadLimit;
        }
    }

    private void ApplySpeedLimit(object? parameter)
    {
        if (parameter?.ToString() is not string target)
        {
            return;
        }

        SpeedLimitApplyRequestedEventArgs? args = target switch
        {
            "GlobalUpload" => CreateApplyArgs(SpeedLimitScope.Global, SpeedLimitDirection.Upload, OverviewUploadLimitEnabled, OverviewUploadLimitValue, OverviewUploadLimitUnit),
            "GlobalDownload" => CreateApplyArgs(SpeedLimitScope.Global, SpeedLimitDirection.Download, OverviewDownloadLimitEnabled, OverviewDownloadLimitValue, OverviewDownloadLimitUnit),
            "TaskUpload" => CreateApplyArgs(SpeedLimitScope.Task, SpeedLimitDirection.Upload, TaskUploadLimitEnabled, TaskUploadLimitValue, TaskUploadLimitUnit),
            "TaskDownload" => CreateApplyArgs(SpeedLimitScope.Task, SpeedLimitDirection.Download, TaskDownloadLimitEnabled, TaskDownloadLimitValue, TaskDownloadLimitUnit),
            _ => null
        };

        if (args is not null)
        {
            SpeedLimitApplyRequested?.Invoke(this, args);
        }
    }

    private void ToggleSpeedLimitEditor(object? parameter)
    {
        if (parameter?.ToString() is not string target)
        {
            return;
        }

        switch (target)
        {
            case "GlobalUpload":
                IsOverviewUploadLimitEditorExpanded = !IsOverviewUploadLimitEditorExpanded;
                break;
            case "GlobalDownload":
                IsOverviewDownloadLimitEditorExpanded = !IsOverviewDownloadLimitEditorExpanded;
                break;
            case "TaskUpload":
                IsTaskUploadLimitEditorExpanded = !IsTaskUploadLimitEditorExpanded;
                break;
            case "TaskDownload":
                IsTaskDownloadLimitEditorExpanded = !IsTaskDownloadLimitEditorExpanded;
                break;
        }
    }

    private static SpeedLimitApplyRequestedEventArgs CreateApplyArgs(
        SpeedLimitScope scope,
        SpeedLimitDirection direction,
        bool isEnabled,
        double value,
        string unit)
    {
        return new SpeedLimitApplyRequestedEventArgs(scope, direction, isEnabled, isEnabled ? GetBytesPerSecond(value, unit) : 0);
    }

    private static void ApplyDisplayLimit(
        bool isEnabled,
        long bytesPerSecond,
        Action<string> setText,
        Action<Visibility> setVisibility)
    {
        bool hasLimit = isEnabled && bytesPerSecond > 0;
        setText(hasLimit ? Strings.Format("TaskDetailsSpeedLimitInlineText", FormatSpeed(bytesPerSecond)) : string.Empty);
        setVisibility(hasLimit ? Visibility.Visible : Visibility.Collapsed);
    }

    private static void SetLimitEditor(
        bool isEnabled,
        long bytesPerSecond,
        Action<bool> setEnabled,
        Action<double> setValue,
        Action<string> setUnit)
    {
        setEnabled(isEnabled && bytesPerSecond > 0);
        (double value, string unit) = GetValueAndUnit(bytesPerSecond);
        setUnit(unit);
        setValue(value);
    }

    private static (double Value, string Unit) GetValueAndUnit(long bytesPerSecond)
    {
        long normalized = Math.Max(bytesPerSecond, 1024);
        const long mb = 1024L * 1024L;
        bool useMegabytes = normalized >= mb && normalized % mb == 0;
        double divisor = useMegabytes ? mb : 1024d;
        return (Math.Max(1, normalized / divisor), useMegabytes ? UnitMegabytes : UnitKilobytes);
    }

    private static long GetBytesPerSecond(double value, string unit)
    {
        long multiplier = NormalizeUnit(unit).Equals(UnitMegabytes, StringComparison.OrdinalIgnoreCase)
            ? 1024L * 1024L
            : 1024L;

        return Math.Max(1, (long)Math.Round(NormalizeLimitValue(value))) * multiplier;
    }

    private static string CreateLimitSummary(bool isEnabled, double value, string unit)
    {
        return isEnabled
            ? Strings.Format("TaskDetailsSpeedLimitInlineText", $"{NormalizeLimitValue(value):0.#} {NormalizeUnit(unit)}")
            : Strings.Get("TaskDetailsSpeedLimitOffText");
    }

    private static string FormatItemCount(int count)
    {
        return Strings.Format("TaskDetailsItemCountFormat", Math.Max(count, 0));
    }

    private static string FormatSpeed(long bytesPerSecond)
    {
        string[] units = ["B/s", "KB/s", "MB/s", "GB/s"];
        double speed = Math.Max(bytesPerSecond, 0);
        int unitIndex = 0;
        while (speed >= 1024 && unitIndex < units.Length - 1)
        {
            speed /= 1024;
            unitIndex++;
        }

        return $"{speed:0.#} {units[unitIndex]}";
    }

    private static double NormalizeLimitValue(double value)
    {
        return double.IsNaN(value) || value < 1 ? 1 : value;
    }

    private static string NormalizeUnit(string? unit)
    {
        return string.Equals(unit, UnitMegabytes, StringComparison.OrdinalIgnoreCase) ? UnitMegabytes : UnitKilobytes;
    }

    private void NotifyLimitInputsChanged(string summaryPropertyName, string isInputEnabledPropertyName)
    {
        OnPropertyChanged(summaryPropertyName);
        OnPropertyChanged(isInputEnabledPropertyName);
    }

    private void NotifyEditorExpandedChanged(string visibilityPropertyName, string chevronPropertyName)
    {
        OnPropertyChanged(visibilityPropertyName);
        OnPropertyChanged(chevronPropertyName);
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private sealed class RelayCommand(Action<object?> execute) : ICommand
    {
        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => execute(parameter);

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
