using CommunityToolkit.WinUI.Animations;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OmniDown.Models;
using OmniDown.ViewModels;
using System;
using System.ComponentModel;
using System.IO;

namespace OmniDown.Controls;

public sealed partial class TaskDetailsPaneControl : UserControl
{
    public static readonly DependencyProperty SelectedTaskProperty =
        DependencyProperty.Register(
            nameof(SelectedTask),
            typeof(DownloadTask),
            typeof(TaskDetailsPaneControl),
            new PropertyMetadata(null, OnSelectedTaskChanged));

    public static readonly DependencyProperty SelectedTaskCountProperty =
        DependencyProperty.Register(
            nameof(SelectedTaskCount),
            typeof(int),
            typeof(TaskDetailsPaneControl),
            new PropertyMetadata(0, OnSelectedTaskCountChanged));

    private DownloadTask? _subscribedTask;
    private int _overviewItemCount;
    private int _overviewActiveCount;
    private int _overviewPausedCount;
    private int _overviewIssueCount;
    private long _overviewDownloadSpeed;
    private long _overviewUploadSpeed;
    private bool _overviewDownloadLimitEnabled;
    private long _overviewDownloadLimit;
    private bool _overviewUploadLimitEnabled;
    private long _overviewUploadLimit;
    private bool _ariaIsRunning;
    private string _ariaEngineVariant = string.Empty;
    private int? _ariaProcessId;
    private string _ariaEndpoint = string.Empty;
    private string _ariaDiagnosticText = string.Empty;
    private FrameworkElement? _currentContent;
    private int _previousVisibleSelectedIndex;

    public TaskDetailsPaneViewModel ViewModel { get; } = new();

    public event EventHandler<SpeedLimitApplyRequestedEventArgs>? SpeedLimitApplyRequested;

    public TaskDetailsPaneControl()
    {
        InitializeComponent();
        DataContext = ViewModel;
        ViewModel.SpeedLimitApplyRequested += ViewModel_SpeedLimitApplyRequested;
        TaskDetailsSelectorBar.SelectedItem = TaskDetailsSummaryItem;
        Refresh();
    }

    public DownloadTask? SelectedTask
    {
        get => (DownloadTask?)GetValue(SelectedTaskProperty);
        set => SetValue(SelectedTaskProperty, value);
    }

    public int SelectedTaskCount
    {
        get => (int)GetValue(SelectedTaskCountProperty);
        set => SetValue(SelectedTaskCountProperty, value);
    }

    public void UpdateOverviewState(
        int itemCount,
        int activeCount,
        int pausedCount,
        int issueCount,
        long downloadSpeed,
        long uploadSpeed,
        bool downloadLimitEnabled,
        long downloadLimit,
        bool uploadLimitEnabled,
        long uploadLimit,
        bool ariaIsRunning,
        string ariaEngineVariant,
        int? ariaProcessId,
        string ariaEndpoint,
        string ariaDiagnosticText)
    {
        _overviewItemCount = Math.Max(itemCount, 0);
        _overviewActiveCount = Math.Max(activeCount, 0);
        _overviewPausedCount = Math.Max(pausedCount, 0);
        _overviewIssueCount = Math.Max(issueCount, 0);
        _overviewDownloadSpeed = Math.Max(downloadSpeed, 0);
        _overviewUploadSpeed = Math.Max(uploadSpeed, 0);
        _overviewDownloadLimitEnabled = downloadLimitEnabled;
        _overviewDownloadLimit = Math.Max(downloadLimit, 0);
        _overviewUploadLimitEnabled = uploadLimitEnabled;
        _overviewUploadLimit = Math.Max(uploadLimit, 0);
        _ariaIsRunning = ariaIsRunning;
        _ariaEngineVariant = ariaEngineVariant;
        _ariaProcessId = ariaProcessId;
        _ariaEndpoint = ariaEndpoint;
        _ariaDiagnosticText = ariaDiagnosticText;
        RefreshOverviewDetails();
        RefreshAriaStatusDetails();
    }

    private static void OnSelectedTaskChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        TaskDetailsPaneControl control = (TaskDetailsPaneControl)dependencyObject;
        control.SubscribeToTask(args.OldValue as DownloadTask, args.NewValue as DownloadTask);
        control.Refresh();
    }

    private static void OnSelectedTaskCountChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        ((TaskDetailsPaneControl)dependencyObject).Refresh();
    }

    private void SubscribeToTask(DownloadTask? oldTask, DownloadTask? newTask)
    {
        if (oldTask is not null)
        {
            oldTask.PropertyChanged -= SelectedTask_PropertyChanged;
        }

        _subscribedTask = newTask;
        if (_subscribedTask is not null)
        {
            _subscribedTask.PropertyChanged += SelectedTask_PropertyChanged;
        }
    }

    private void SelectedTask_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (SelectedTask is null)
        {
            ShowOverview();
            return;
        }

        ShowTask(SelectedTask);
    }

    private void ShowOverview()
    {
        ConfigureSelectorBar(DetailPaneMode.Overview);
        RefreshOverviewDetails();
        RefreshAriaStatusDetails();
        ShowTaskDetailsSection(SelectedSectionTag);
    }

    private void HideAllContent()
    {
        TaskDetailsOverviewContent.Visibility = Visibility.Collapsed;
        TaskDetailsAriaStatusContent.Visibility = Visibility.Collapsed;
        TaskDetailsSummaryContent.Visibility = Visibility.Collapsed;
        TaskDetailsFilesContent.Visibility = Visibility.Collapsed;
        TaskDetailsOptionsContent.Visibility = Visibility.Collapsed;
        TaskDetailsSourceContent.Visibility = Visibility.Collapsed;
        TaskDetailsStatusContent.Visibility = Visibility.Collapsed;
        TaskDetailsPeersContent.Visibility = Visibility.Collapsed;
        TaskDetailsTrackersContent.Visibility = Visibility.Collapsed;
    }

    private void ShowTask(DownloadTask task)
    {
        ConfigureSelectorBar(ResolveDetailPaneMode(task));

        TaskDetailsNameText.Text = string.IsNullOrWhiteSpace(task.Name) ? "未命名任务" : task.Name;
        string resolvedFilePath = ResolveTaskFilePath(task);
        TaskDetailsDownloadPathText.Text = string.IsNullOrWhiteSpace(resolvedFilePath)
            ? task.SaveDirectory
            : resolvedFilePath;
        bool isDownloadTargetDirectory = Directory.Exists(resolvedFilePath);
        TaskDetailsOpenDownloadFileButton.Visibility = isDownloadTargetDirectory
            ? Visibility.Collapsed
            : Visibility.Visible;
        bool isDownloadComplete = string.Equals(task.Status, "completed", StringComparison.OrdinalIgnoreCase);
        TaskDetailsOpenDownloadFileButton.IsEnabled = isDownloadComplete && File.Exists(resolvedFilePath);
        TaskDetailsGidText.Text = string.IsNullOrWhiteSpace(task.Gid) ? "-" : task.Gid;
        TaskDetailsHeroIcon.Glyph = task.IsPeerTransfer ? "\uE968" : "\uE7C3";
        TaskDetailsHeroIcon.Foreground = task.StatusBrush;
        TaskDetailsSizeText.Text = task.SizeText;
        TaskDetailsProgressPercentText.Text = task.ProgressText;
        TaskDetailsStatusDetailText.Text = task.StatusDetailText;
        TaskDetailsCreatedAtText.Text = task.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
        TaskDetailsSourceText.Text = string.IsNullOrWhiteSpace(task.SourceUri) ? "-" : task.SourceUri;

        TaskDetailsProgressBar.Value = Math.Clamp(task.Progress, 0, 100);
        TaskDetailsProgressBar.Foreground = task.ProgressBrush;
        TaskDetailsCompletedText.Text = task.TotalLength > 0
            ? $"{FormatBytesForDetails(task.CompletedLength)} / {FormatBytesForDetails(task.TotalLength)}"
            : FormatBytesForDetails(task.CompletedLength);
        TaskDetailsRemainingText.Text = task.RemainingTimeText;
        ViewModel.UpdateTaskSpeeds(task.DownloadSpeed, task.UploadSpeed, ShouldShowUploadDetails(task));
        RefreshFileDetails(task, resolvedFilePath);
        RefreshOptionDetails(task);
        RefreshPeerDetails(task);
        RefreshTrackerDetails(task);

        ShowTaskDetailsSection(SelectedSectionTag);
    }

    private void RefreshFileDetails(DownloadTask task, string resolvedFilePath)
    {
        TaskDetailsFileNameText.Text = string.IsNullOrWhiteSpace(task.Name) ? "-" : task.Name;
        TaskDetailsFilePathText.Text = string.IsNullOrWhiteSpace(resolvedFilePath) ? "-" : resolvedFilePath;
        TaskDetailsSaveDirectoryText.Text = string.IsNullOrWhiteSpace(task.SaveDirectory) ? "-" : task.SaveDirectory;
        TaskDetailsLocalStateText.Text = ResolveLocalStateText(task, resolvedFilePath);
    }

    private void RefreshOptionDetails(DownloadTask task)
    {
        TaskDetailsTaskTypeText.Text = ResolveTaskTypeText(task);
        TaskDetailsSessionText.Text = task.IsAria2SessionAttached
            ? "已连接到当前 aria2 会话"
            : "仅保留本地记录，当前 aria2 会话中未找到该 GID";
        TaskDetailsProtocolText.Text = ResolveProtocolText(task.SourceUri);
        TaskDetailsTaskOptionsText.Text = "当前侧栏显示已缓存的任务信息；Referer、Cookie、代理等任务级 aria2 选项尚未接入 getOption。";
    }

    private void RefreshPeerDetails(DownloadTask task)
    {
        TaskDetailsPeerModeText.Text = task.IsPeerTransfer
            ? task.IsMetadataTransfer ? "BitTorrent 元数据获取" : "BitTorrent / 磁力传输"
            : "普通 HTTP/FTP 下载";
        TaskDetailsPeerUploadText.Text = task.UploadSpeedText;
        TaskDetailsPeerDownloadText.Text = task.DownloadSpeedText;
        TaskDetailsPeerDetailText.Text = task.IsPeerTransfer
            ? "节点列表尚未接入 aria2.getPeers；当前可查看该任务的上下行速度和传输模式。"
            : "该任务不是 BT 或磁力传输，通常没有 Peer 节点。";
    }

    private void RefreshTrackerDetails(DownloadTask task)
    {
        TaskDetailsTrackerApplicabilityText.Text = task.IsPeerTransfer
            ? "适用于该 BT / 磁力任务"
            : "普通下载通常不使用 Tracker";
        TaskDetailsTrackerSourceText.Text = string.IsNullOrWhiteSpace(task.SourceUri) ? "-" : task.SourceUri;
        TaskDetailsTrackerDetailText.Text = task.IsPeerTransfer
            ? "Tracker tier、URL 和状态尚未接入 aria2.getServers；当前显示任务来源作为追踪入口。"
            : "该任务没有可显示的 Tracker 信息。";
    }

    private void RefreshOverviewDetails()
    {
        ViewModel.UpdateOverview(
            _overviewItemCount,
            _overviewActiveCount,
            _overviewPausedCount,
            _overviewIssueCount,
            _overviewDownloadSpeed,
            _overviewUploadSpeed,
            _overviewDownloadLimitEnabled,
            _overviewDownloadLimit,
            _overviewUploadLimitEnabled,
            _overviewUploadLimit);
    }

    public void UpdateSelectedTaskSpeedLimitState(
        bool downloadLimitEnabled,
        long downloadLimit,
        bool uploadLimitEnabled,
        long uploadLimit)
    {
        ViewModel.UpdateTaskLimits(downloadLimitEnabled, downloadLimit, uploadLimitEnabled, uploadLimit);
    }

    private void RefreshAriaStatusDetails()
    {
        TaskDetailsAriaVersionText.Text = string.IsNullOrWhiteSpace(_ariaEngineVariant)
            ? "未检测"
            : _ariaEngineVariant;
        TaskDetailsAriaRunningStateText.Text = _ariaIsRunning ? "运行中" : "未运行";
        TaskDetailsAriaProcessText.Text = _ariaIsRunning && _ariaProcessId is int processId
            ? $"PID {processId}"
            : "-";
        TaskDetailsAriaEndpointText.Text = string.IsNullOrWhiteSpace(_ariaEndpoint) ? "-" : _ariaEndpoint;
        TaskDetailsAriaDiagnosticText.Text = string.IsNullOrWhiteSpace(_ariaDiagnosticText) ? "-" : _ariaDiagnosticText;
    }

    private string SelectedSectionTag => TaskDetailsSelectorBar.SelectedItem?.Tag?.ToString() ?? "Summary";

    private void TaskDetailsSelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        ShowTaskDetailsSection(SelectedSectionTag);
    }

    private void ViewModel_SpeedLimitApplyRequested(object? sender, SpeedLimitApplyRequestedEventArgs e)
    {
        SpeedLimitApplyRequested?.Invoke(this, e);
    }

    private void ShowTaskDetailsSection(string tag)
    {
        FrameworkElement targetContent = ResolveContentForTag(tag);
        if (_currentContent == targetContent &&
            targetContent.Visibility == Visibility.Visible)
        {
            return;
        }

        int currentVisibleSelectedIndex = GetVisibleSelectedIndex();
        int direction = currentVisibleSelectedIndex >= _previousVisibleSelectedIndex ? 1 : -1;
        if (SelectedTask is null)
        {
            HideAllContent();
            ShowContent(targetContent, direction, animate: _currentContent is not null);
            _currentContent = targetContent;
            _previousVisibleSelectedIndex = currentVisibleSelectedIndex;
            return;
        }

        HideAllContent();
        ShowContent(targetContent, direction, animate: _currentContent is not null);
        _currentContent = targetContent;
        _previousVisibleSelectedIndex = currentVisibleSelectedIndex;
    }

    private FrameworkElement ResolveContentForTag(string tag)
    {
        if (SelectedTask is null)
        {
            return tag == "AriaStatus" ? TaskDetailsAriaStatusContent : TaskDetailsOverviewContent;
        }

        return tag switch
        {
            "Files" => TaskDetailsFilesContent,
            "Options" => TaskDetailsOptionsContent,
            "Source" => TaskDetailsSourceContent,
            "Status" => TaskDetailsStatusContent,
            "Peers" => TaskDetailsPeersContent,
            "Trackers" => TaskDetailsTrackersContent,
            _ => TaskDetailsSummaryContent
        };
    }

    private static void ShowContent(FrameworkElement content, int direction, bool animate)
    {
        content.Visibility = Visibility.Visible;
        if (!animate)
        {
            content.Opacity = 1;
            content.Translation = new System.Numerics.Vector3(0, 0, 0);
            return;
        }

        double offset = direction >= 0 ? 24 : -24;
        AnimationBuilder.Create()
            .Opacity(to: 1, from: 0, duration: TimeSpan.FromMilliseconds(140))
            .Translation(Axis.X, to: 0, from: offset, duration: TimeSpan.FromMilliseconds(220))
            .Start(content);
    }

    private int GetVisibleSelectedIndex()
    {
        int visibleIndex = 0;
        foreach (object item in TaskDetailsSelectorBar.Items)
        {
            if (item is not SelectorBarItem selectorItem ||
                selectorItem.Visibility != Visibility.Visible)
            {
                continue;
            }

            if (selectorItem == TaskDetailsSelectorBar.SelectedItem)
            {
                return visibleIndex;
            }

            visibleIndex++;
        }

        return 0;
    }

    private void ConfigureSelectorBar(DetailPaneMode mode)
    {
        SetSelectorItemVisible(TaskDetailsSummaryItem, true);
        SetSelectorItemVisible(TaskDetailsAriaStatusItem, mode == DetailPaneMode.Overview);
        SetSelectorItemVisible(TaskDetailsFilesItem, mode != DetailPaneMode.Overview);
        SetSelectorItemVisible(TaskDetailsOptionsItem, mode != DetailPaneMode.Overview);
        SetSelectorItemVisible(TaskDetailsSourceItem, mode is DetailPaneMode.Normal or DetailPaneMode.Ed2k);
        SetSelectorItemVisible(TaskDetailsStatusItem, mode is DetailPaneMode.Magnet or DetailPaneMode.Ed2k);
        SetSelectorItemVisible(TaskDetailsPeersItem, mode == DetailPaneMode.Magnet);
        SetSelectorItemVisible(TaskDetailsTrackersItem, mode == DetailPaneMode.Magnet);

        if (TaskDetailsSelectorBar.SelectedItem is not SelectorBarItem selectedItem ||
            selectedItem.Visibility != Visibility.Visible)
        {
            TaskDetailsSelectorBar.SelectedItem = TaskDetailsSummaryItem;
            _previousVisibleSelectedIndex = 0;
        }
    }

    private static void SetSelectorItemVisible(SelectorBarItem item, bool isVisible)
    {
        item.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    private static string FormatBytesForDetails(long bytes)
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

    private static string FormatSpeedForDetails(long bytesPerSecond)
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

    private static string ResolveLocalStateText(DownloadTask task, string resolvedFilePath)
    {
        if (string.IsNullOrWhiteSpace(resolvedFilePath))
        {
            return "尚未解析到本地文件路径";
        }

        if (File.Exists(resolvedFilePath))
        {
            return "文件已存在";
        }

        if (Directory.Exists(resolvedFilePath))
        {
            return "目录已存在";
        }

        if (!string.IsNullOrWhiteSpace(task.SaveDirectory) && Directory.Exists(task.SaveDirectory))
        {
            return "保存目录存在，目标文件尚未生成或已被移动";
        }

        return "路径暂不可用";
    }

    private static string ResolveTaskTypeText(DownloadTask task)
    {
        if (task.IsPeerTransfer)
        {
            return task.IsMetadataTransfer ? "磁力链接元数据任务" : "BT / 磁力任务";
        }

        if (Uri.TryCreate(task.SourceUri, UriKind.Absolute, out Uri? uri) &&
            !string.IsNullOrWhiteSpace(uri.Scheme))
        {
            return $"{uri.Scheme.ToUpperInvariant()} 下载任务";
        }

        return "下载任务";
    }

    private static DetailPaneMode ResolveDetailPaneMode(DownloadTask task)
    {
        string source = task.SourceUri.Trim();
        if (source.StartsWith("ed2k://", StringComparison.OrdinalIgnoreCase))
        {
            return DetailPaneMode.Ed2k;
        }

        if (source.StartsWith("magnet:", StringComparison.OrdinalIgnoreCase) ||
            task.IsPeerTransfer)
        {
            return DetailPaneMode.Magnet;
        }

        return DetailPaneMode.Normal;
    }

    private static bool ShouldShowUploadDetails(DownloadTask task)
    {
        return task.IsPeerTransfer ||
            task.SourceUri.StartsWith("ed2k://", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveProtocolText(string sourceUri)
    {
        if (string.IsNullOrWhiteSpace(sourceUri))
        {
            return "-";
        }

        if (Uri.TryCreate(sourceUri, UriKind.Absolute, out Uri? uri) &&
            !string.IsNullOrWhiteSpace(uri.Scheme))
        {
            return uri.Scheme;
        }

        return "本地路径或自定义来源";
    }

    private static string ResolveTaskFilePath(DownloadTask task)
    {
        if (!string.IsNullOrWhiteSpace(task.LocalFilePath))
        {
            return task.LocalFilePath;
        }

        return string.IsNullOrWhiteSpace(task.SaveDirectory) || string.IsNullOrWhiteSpace(task.Name)
            ? string.Empty
            : Path.Combine(task.SaveDirectory, task.Name);
    }

    private enum DetailPaneMode
    {
        Overview,
        Normal,
        Magnet,
        Ed2k
    }

    private void TaskDetailsCopyGidButton_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedTask is not { } task || string.IsNullOrWhiteSpace(task.Gid))
        {
            return;
        }

        Windows.ApplicationModel.DataTransfer.DataPackage package = new();
        package.SetText(task.Gid);
        Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(package);
    }

    private void TaskDetailsOpenDownloadFileButton_Click(object sender, RoutedEventArgs e)
    {
        string resolvedFilePath = SelectedTask is not null ? ResolveTaskFilePath(SelectedTask) : string.Empty;
        if (!string.IsNullOrWhiteSpace(resolvedFilePath) && File.Exists(resolvedFilePath))
        {
            OpenShellTarget(resolvedFilePath);
        }
    }

    private void TaskDetailsOpenDownloadFolderButton_Click(object sender, RoutedEventArgs e)
    {
        string resolvedFilePath = SelectedTask is not null ? ResolveTaskFilePath(SelectedTask) : string.Empty;
        string? folderPath;

        if (!string.IsNullOrWhiteSpace(resolvedFilePath))
        {
            if (Directory.Exists(resolvedFilePath))
            {
                folderPath = resolvedFilePath;
            }
            else
            {
                folderPath = File.Exists(resolvedFilePath)
                    ? Path.GetDirectoryName(resolvedFilePath)
                    : null;
            }
        }
        else
        {
            folderPath = SelectedTask?.SaveDirectory;
        }

        if (!string.IsNullOrWhiteSpace(folderPath) && Directory.Exists(folderPath))
        {
            OpenShellTarget(folderPath);
        }
    }

    private void TaskDetailsOpenSourceFileButton_Click(object sender, RoutedEventArgs e)
    {
        string? sourceUri = SelectedTask?.SourceUri;
        if (string.IsNullOrWhiteSpace(sourceUri))
        {
            return;
        }

        if (Uri.TryCreate(sourceUri, UriKind.Absolute, out Uri? uri) && uri.IsFile)
        {
            OpenShellTarget(uri.LocalPath);
        }
    }

    private void TaskDetailsOpenSourceFolderButton_Click(object sender, RoutedEventArgs e)
    {
        string? sourceUri = SelectedTask?.SourceUri;
        if (string.IsNullOrWhiteSpace(sourceUri))
        {
            return;
        }

        if (Uri.TryCreate(sourceUri, UriKind.Absolute, out Uri? uri) && uri.IsFile)
        {
            string? folderPath = Path.GetDirectoryName(uri.LocalPath);
            if (!string.IsNullOrWhiteSpace(folderPath) && Directory.Exists(folderPath))
            {
                OpenShellTarget(folderPath);
            }
        }
    }

    private static void OpenShellTarget(string path)
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }
}
