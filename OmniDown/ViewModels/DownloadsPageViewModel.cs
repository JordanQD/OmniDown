using Microsoft.UI.Xaml;
using OmniDown.Models;
using OmniDown.Services.Localization;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace OmniDown.ViewModels;

/// <summary>
/// DownloadsPage 的 ViewModel。管理任务列表的过滤、排序、选中状态和展示数据。
/// </summary>
public sealed class DownloadsPageViewModel : INotifyPropertyChanged
{
    private string _currentFilter = "Home";
    private TaskSortColumn _sortColumn = TaskSortColumn.CreatedAt;
    private bool _sortAscending;
    private string _searchQuery = string.Empty;
    private bool _isLoading = true;
    private bool _isTaskDetailsPaneOpen;
    private long _globalDownloadSpeed;
    private long _globalUploadSpeed;
    private long _downloadLimitBytesPerSec;
    private long _uploadLimitBytesPerSec;
    private bool _isDownloadSpeedLimitEnabled;
    private bool _isUploadSpeedLimitEnabled;
    private int _selectedTaskCount;
    private int _visibleTaskCount;

    // ── 指标计数 ──
    private int _totalCount;
    private int _activeCount;
    private int _pausedCount;
    private int _completedCount;
    private int _issueCount;

    // ── 指标文本 ──
    private string _totalTasksText = "0";
    private string _activeTasksText = "0";
    private string _pausedTasksText = "0";
    private string _completedTasksText = "0";
    private string _issueTasksText = "0";
    private string _globalDownloadSpeedText = "0 B/s";
    private string _globalUploadSpeedText = "0 B/s";
    private string _globalDownloadLimitText = string.Empty;
    private string _globalUploadLimitText = string.Empty;
    private string _downloadsTitleText = "Downloads";
    private string _statusBarItemCountText = "0 items";
    private string _statusBarSelectedCountText = "0 selected";
    private string _statusBarActiveTasksText = "0";
    private string _statusBarPausedTasksText = "0";
    private string _statusBarIssueTasksText = "0";
    private string _statusBarDownloadSpeedText = "0 B/s";
    private string _statusBarUploadSpeedText = "0 B/s";
    private string _statusBarDownloadLimitText = string.Empty;
    private string _statusBarUploadLimitText = string.Empty;

    private bool _isStatsPanelVisible;
    private bool _isCompletedMetricVisible;
    private bool _isIssueMetricVisible;
    private bool _isStatusBarSpeedPanelVisible;
    private bool _isStatusBarTaskCountsPanelVisible;
    private bool _isStatusBarIssueTasksPanelVisible;
    private bool _isStatusBarSelectedCountVisible;
    private bool _isStatusBarDownloadLimitVisible;
    private bool _isStatusBarUploadLimitVisible;
    private bool _isGlobalDownloadLimitIconVisible;
    private bool _isGlobalUploadLimitIconVisible;

    public event PropertyChangedEventHandler? PropertyChanged;

    private readonly ObservableCollection<DownloadTask> _tasks = new();
    public ObservableCollection<DownloadTask> VisibleTasks { get; } = new();

    public ObservableCollection<DownloadTask> AllTasks => _tasks;

    // ── 过滤器标签 ──
    public string CurrentFilter
    {
        get => _currentFilter;
        set { if (SetProperty(ref _currentFilter, value)) ApplyFilter(); }
    }

    public TaskSortColumn SortColumn
    {
        get => _sortColumn;
        set { if (SetProperty(ref _sortColumn, value)) ApplyFilter(); }
    }

    public bool SortAscending
    {
        get => _sortAscending;
        set { if (SetProperty(ref _sortAscending, value)) ApplyFilter(); }
    }

    public string SearchQuery
    {
        get => _searchQuery;
        set { if (SetProperty(ref _searchQuery, value)) ApplyFilter(); }
    }

    // ── 加载状态 ──
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    // ── 详情面板 ──
    public bool IsTaskDetailsPaneOpen
    {
        get => _isTaskDetailsPaneOpen;
        set => SetProperty(ref _isTaskDetailsPaneOpen, value);
    }

    // ── 全局速度 ──
    public long GlobalDownloadSpeed => _globalDownloadSpeed;
    public long GlobalUploadSpeed => _globalUploadSpeed;

    public string GlobalDownloadSpeedText
    {
        get => _globalDownloadSpeedText;
        set => SetProperty(ref _globalDownloadSpeedText, value);
    }

    public string GlobalUploadSpeedText
    {
        get => _globalUploadSpeedText;
        set => SetProperty(ref _globalUploadSpeedText, value);
    }

    public string GlobalDownloadLimitText
    {
        get => _globalDownloadLimitText;
        set => SetProperty(ref _globalDownloadLimitText, value);
    }

    public string GlobalUploadLimitText
    {
        get => _globalUploadLimitText;
        set => SetProperty(ref _globalUploadLimitText, value);
    }

    // ── 速度限制 ──
    public long DownloadLimitBytesPerSec
    {
        get => _downloadLimitBytesPerSec;
        set => SetProperty(ref _downloadLimitBytesPerSec, value);
    }

    public long UploadLimitBytesPerSec
    {
        get => _uploadLimitBytesPerSec;
        set => SetProperty(ref _uploadLimitBytesPerSec, value);
    }

    public bool IsDownloadSpeedLimitEnabled
    {
        get => _isDownloadSpeedLimitEnabled;
        set => SetProperty(ref _isDownloadSpeedLimitEnabled, value);
    }

    public bool IsUploadSpeedLimitEnabled
    {
        get => _isUploadSpeedLimitEnabled;
        set => SetProperty(ref _isUploadSpeedLimitEnabled, value);
    }

    // ── 状态栏文本 ──
    public string StatusBarItemCountText { get => _statusBarItemCountText; set => SetProperty(ref _statusBarItemCountText, value); }
    public string StatusBarSelectedCountText { get => _statusBarSelectedCountText; set => SetProperty(ref _statusBarSelectedCountText, value); }
    public string StatusBarActiveTasksText { get => _statusBarActiveTasksText; set => SetProperty(ref _statusBarActiveTasksText, value); }
    public string StatusBarPausedTasksText { get => _statusBarPausedTasksText; set => SetProperty(ref _statusBarPausedTasksText, value); }
    public string StatusBarIssueTasksText { get => _statusBarIssueTasksText; set => SetProperty(ref _statusBarIssueTasksText, value); }
    public string StatusBarDownloadSpeedText { get => _statusBarDownloadSpeedText; set => SetProperty(ref _statusBarDownloadSpeedText, value); }
    public string StatusBarUploadSpeedText { get => _statusBarUploadSpeedText; set => SetProperty(ref _statusBarUploadSpeedText, value); }
    public string StatusBarDownloadLimitText { get => _statusBarDownloadLimitText; set => SetProperty(ref _statusBarDownloadLimitText, value); }
    public string StatusBarUploadLimitText { get => _statusBarUploadLimitText; set => SetProperty(ref _statusBarUploadLimitText, value); }

    // ── 仪表盘文本 ──
    public string DownloadsTitleText { get => _downloadsTitleText; set => SetProperty(ref _downloadsTitleText, value); }
    public string TotalTasksText { get => _totalTasksText; set => SetProperty(ref _totalTasksText, value); }
    public string ActiveTasksText { get => _activeTasksText; set => SetProperty(ref _activeTasksText, value); }
    public string PausedTasksText { get => _pausedTasksText; set => SetProperty(ref _pausedTasksText, value); }
    public string CompletedTasksText { get => _completedTasksText; set => SetProperty(ref _completedTasksText, value); }
    public string IssueTasksText { get => _issueTasksText; set => SetProperty(ref _issueTasksText, value); }

    // ── 可见性 ──
    public bool IsStatsPanelVisible { get => _isStatsPanelVisible; set => SetProperty(ref _isStatsPanelVisible, value); }
    public bool IsCompletedMetricVisible { get => _isCompletedMetricVisible; set => SetProperty(ref _isCompletedMetricVisible, value); }
    public bool IsIssueMetricVisible { get => _isIssueMetricVisible; set => SetProperty(ref _isIssueMetricVisible, value); }
    public bool IsStatusBarSpeedPanelVisible { get => _isStatusBarSpeedPanelVisible; set => SetProperty(ref _isStatusBarSpeedPanelVisible, value); }
    public bool IsStatusBarTaskCountsPanelVisible { get => _isStatusBarTaskCountsPanelVisible; set => SetProperty(ref _isStatusBarTaskCountsPanelVisible, value); }
    public bool IsStatusBarSelectedCountVisible { get => _isStatusBarSelectedCountVisible; set => SetProperty(ref _isStatusBarSelectedCountVisible, value); }
    public bool IsStatusBarIssueTasksPanelVisible { get => _isStatusBarIssueTasksPanelVisible; set => SetProperty(ref _isStatusBarIssueTasksPanelVisible, value); }
    public bool IsStatusBarDownloadLimitVisible { get => _isStatusBarDownloadLimitVisible; set => SetProperty(ref _isStatusBarDownloadLimitVisible, value); }
    public bool IsStatusBarUploadLimitVisible { get => _isStatusBarUploadLimitVisible; set => SetProperty(ref _isStatusBarUploadLimitVisible, value); }
    public bool IsGlobalDownloadLimitIconVisible { get => _isGlobalDownloadLimitIconVisible; set => SetProperty(ref _isGlobalDownloadLimitIconVisible, value); }
    public bool IsGlobalUploadLimitIconVisible { get => _isGlobalUploadLimitIconVisible; set => SetProperty(ref _isGlobalUploadLimitIconVisible, value); }

    public int SelectedTaskCount { get => _selectedTaskCount; set => SetProperty(ref _selectedTaskCount, value); }
    public int VisibleTaskCount { get => _visibleTaskCount; set => SetProperty(ref _visibleTaskCount, value); }
    public int TotalCount => _totalCount;
    public int ActiveCount => _activeCount;

    // ── 过滤逻辑 ──

    public void SetGlobalSpeeds(long downloadBps, long uploadBps)
    {
        _globalDownloadSpeed = downloadBps;
        _globalUploadSpeed = uploadBps;
        GlobalDownloadSpeedText = FormatSpeed(downloadBps);
        GlobalUploadSpeedText = FormatSpeed(uploadBps);
        StatusBarDownloadSpeedText = GlobalDownloadSpeedText;
        StatusBarUploadSpeedText = GlobalUploadSpeedText;
    }

    public void UpdateSpeedLimits(bool downloadEnabled, bool uploadEnabled, long downloadBps, long uploadBps)
    {
        IsDownloadSpeedLimitEnabled = downloadEnabled;
        IsUploadSpeedLimitEnabled = uploadEnabled;
        DownloadLimitBytesPerSec = downloadBps;
        UploadLimitBytesPerSec = uploadBps;

        IsGlobalDownloadLimitIconVisible = downloadEnabled && downloadBps > 0;
        IsGlobalUploadLimitIconVisible = uploadEnabled && uploadBps > 0;
        GlobalDownloadLimitText = downloadEnabled && downloadBps > 0 ? FormatSpeed(downloadBps) : string.Empty;
        GlobalUploadLimitText = uploadEnabled && uploadBps > 0 ? FormatSpeed(uploadBps) : string.Empty;
        IsStatusBarDownloadLimitVisible = IsGlobalDownloadLimitIconVisible;
        IsStatusBarUploadLimitVisible = IsGlobalUploadLimitIconVisible;
        StatusBarDownloadLimitText = GlobalDownloadLimitText;
        StatusBarUploadLimitText = GlobalUploadLimitText;
    }

    public void UpdateDashboard(string tag)
    {
        // 标题
        DownloadsTitleText = tag switch
        {
            "Downloading" => Strings.Get("DownloadingPageTitle"),
            "Waiting" => "等待中",
            "Paused" => "暂停中",
            "Completed" => Strings.Get("CompletedPageTitle"),
            "Issues" => Strings.Get("IssuesPageTitle"),
            _ => Strings.Get("HomePageTitle")
        };

        // 统计面板可见性
        IsStatsPanelVisible = false;
        IsCompletedMetricVisible = tag == "Home";
        IsIssueMetricVisible = tag == "Home";

        // 从全部任务计算计数
        var allTasks = AllTasks.ToList();
        bool isTransferPage = tag == "Downloading";
        _totalCount = allTasks.Count;
        _activeCount = allTasks.Count(IsActiveTransferTask);
        _pausedCount = allTasks.Count(IsPausedTask);
        _completedCount = allTasks.Count(IsCompletedTask);
        _issueCount = allTasks.Count(IsIssueTask);

        TotalTasksText = (isTransferPage ? allTasks.Count(IsDownloadingTask) : _totalCount).ToString();
        ActiveTasksText = _activeCount.ToString();
        PausedTasksText = _pausedCount.ToString();
        CompletedTasksText = _completedCount.ToString();
        IssueTasksText = _issueCount.ToString();
    }

    public void UpdateStatusBar(int visibleCount, int selectedCount, string filterTag)
    {
        _visibleTaskCount = visibleCount;
        _selectedTaskCount = selectedCount;

        StatusBarItemCountText = Strings.Format("StatusBarItemCountText", visibleCount);
        IsStatusBarSelectedCountVisible = selectedCount > 0;
        StatusBarSelectedCountText = selectedCount > 0
            ? Strings.Format("StatusBarSelectedItemCountText", selectedCount)
            : string.Empty;

        bool showTransferSummary = filterTag is "Home" or "Downloading";
        IsStatusBarSpeedPanelVisible = showTransferSummary;
        IsStatusBarTaskCountsPanelVisible = showTransferSummary;

        if (showTransferSummary)
        {
            // 用全部任务的计数而非可见任务
            var all = AllTasks.ToList();
            _activeCount = all.Count(IsActiveTransferTask);
            _pausedCount = all.Count(IsPausedTask);
            _issueCount = all.Count(IsIssueTask);
            StatusBarActiveTasksText = _activeCount.ToString();
            StatusBarPausedTasksText = _pausedCount.ToString();
            StatusBarIssueTasksText = _issueCount.ToString();
        }

        IsStatusBarIssueTasksPanelVisible = showTransferSummary;
    }

    public void HideStatusBar()
    {
        IsStatusBarSpeedPanelVisible = false;
        IsStatusBarTaskCountsPanelVisible = false;
        IsStatusBarIssueTasksPanelVisible = false;
        IsStatusBarSelectedCountVisible = false;
    }

    public void ApplyFilter()
    {
        string tag = _currentFilter;
        string query = _searchQuery;

        var filtered = tag switch
        {
            "Downloading" => AllTasks.Where(t => IsRunningDownloadTask(t) && IsSearchMatch(t, query)),
            "Waiting" => AllTasks.Where(t => IsWaitingTask(t) && IsSearchMatch(t, query)),
            "Paused" => AllTasks.Where(t => IsPausedTask(t) && IsSearchMatch(t, query)),
            "Completed" => AllTasks.Where(t => IsCompletedTask(t) && IsSearchMatch(t, query)),
            "Issues" => AllTasks.Where(t => IsIssueTask(t) && IsSearchMatch(t, query)),
            _ => AllTasks.Where(t => IsSearchMatch(t, query))
        };

        var sorted = SortTasks(filtered).ToList();

        VisibleTasks.Clear();
        foreach (var task in sorted)
        {
            VisibleTasks.Add(task);
        }

        UpdateDashboard(tag);
        UpdateStatusBar(VisibleTasks.Count, _selectedTaskCount, tag);
        VisibleTaskCount = VisibleTasks.Count;
    }

    private IOrderedEnumerable<DownloadTask> SortTasks(IEnumerable<DownloadTask> tasks)
    {
        Func<DownloadTask, IComparable> keySelector = _sortColumn switch
        {
            TaskSortColumn.Name => t => t.Name,
            TaskSortColumn.Size => t => t.TotalLength,
            _ => t => t.Gid
        };

        return _sortAscending
            ? tasks.OrderBy(keySelector)
            : tasks.OrderByDescending(keySelector);
    }

    // ── 静态判断方法（与原 MainWindow.SearchAndHelpers.xaml.cs 逻辑一致）──
    //
    // task.Status 是原始状态码（"downloading"/"paused"/"complete"/"error" 等）
    // task.StatusText 是本地化后的显示文本（"下载中"/"已暂停" 等），不能用于逻辑判断！

    public static bool IsDownloadingTask(DownloadTask task)
    {
        return task.Status.Contains("download", StringComparison.OrdinalIgnoreCase)
            || task.Status.Contains("waiting", StringComparison.OrdinalIgnoreCase)
            || task.Status.Contains("paused", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsActiveTransferTask(DownloadTask task) =>
        IsDownloadingTask(task) && !IsPausedTask(task);

    public static bool IsRunningDownloadTask(DownloadTask task) =>
        task.Status.Contains("download", StringComparison.OrdinalIgnoreCase)
        || task.Status.Contains("resum", StringComparison.OrdinalIgnoreCase)
        || task.Status.Contains("paus", StringComparison.OrdinalIgnoreCase) && !IsPausedTask(task);

    public static bool IsWaitingTask(DownloadTask task) =>
        task.Status.Contains("waiting", StringComparison.OrdinalIgnoreCase);

    public static bool IsPausedTask(DownloadTask task) =>
        task.Status.Contains("paused", StringComparison.OrdinalIgnoreCase);

    public static bool IsCompletedTask(DownloadTask task) =>
        task.Status.Contains("complete", StringComparison.OrdinalIgnoreCase);

    public static bool IsIssueTask(DownloadTask task) =>
        task.Status.Contains("error", StringComparison.OrdinalIgnoreCase);

    private static bool IsSearchMatch(DownloadTask task, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return true;

        if (task.Gid.Equals(query, StringComparison.OrdinalIgnoreCase))
            return true;

        if (task.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private static string FormatSpeed(long bytesPerSecond)
    {
        if (bytesPerSecond <= 0) return "0 B/s";
        if (bytesPerSecond < 1024) return $"{bytesPerSecond} B/s";
        if (bytesPerSecond < 1024 * 1024) return $"{bytesPerSecond / 1024.0:F1} KB/s";
        if (bytesPerSecond < 1024L * 1024 * 1024) return $"{bytesPerSecond / (1024.0 * 1024):F1} MB/s";
        return $"{bytesPerSecond / (1024.0 * 1024 * 1024):F2} GB/s";
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
