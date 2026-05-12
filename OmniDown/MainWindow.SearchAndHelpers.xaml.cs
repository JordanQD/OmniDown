using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using OmniDown.Models;
using OmniDown.Models.Settings;
using OmniDown.Services.Downloads;
using OmniDown.Services.Engine;
using OmniDown.Services.Localization;
using OmniDown.Services.Notifications;
using OmniDown.Services.Rpc;
using OmniDown.Services.Settings;
using OmniDown.Services.Shell;
using OmniDown.Services.Storage;
using OmniDown.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI;
using WinRT.Interop;

namespace OmniDown
{
    public sealed partial class MainWindow
    {
        private void TitleSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            ApplySearchFilters();
        }

        private void TitleSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            ApplySearchFilters();
        }

        private void TitleSearchBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            ApplySearchFilters();
        }

        private void ApplySearchFilters()
        {
            ApplyTaskFilter(_currentTaskFilter);
            ApplySettingsFilter();
        }

        private void TerminalToggleButton_Changed(object sender, RoutedEventArgs e)
        {
            if (TerminalPanel is null || TerminalToggleButton is null)
            {
                return;
            }

            TerminalPanel.Visibility = TerminalToggleButton.IsChecked == true
                ? Visibility.Visible
                : Visibility.Collapsed;
            UpdateDebugStatus();
        }

        private void ClearTerminalButton_Click(object sender, RoutedEventArgs e)
        {
            _aria2EngineHost.ClearTerminal();
            UpdateDebugStatus();
        }

        private async System.Threading.Tasks.Task RunSelectedTaskOperationAsync(
            Func<IReadOnlyList<DownloadTask>, System.Threading.Tasks.Task> operation,
            string successMessage)
        {
            List<DownloadTask> selectedTasks = GetSelectedTasks();
            if (selectedTasks.Count == 0)
            {
                return;
            }

            Aria2EngineStartResult startResult = await EnsureAria2StartedAsync();
            if (!startResult.Started)
            {
                ShowMessage(startResult.Message, InfoBarSeverity.Error);
                return;
            }

            try
            {
                await operation(selectedTasks);
                ApplyTaskFilter(_currentTaskFilter);
                UpdateDashboard();
                UpdateGlobalSpeedsFromTasks();
                await RefreshDownloadsAsync();
                ApplyTaskFilter(_currentTaskFilter);
                UpdateSelectionCommands();
                ShowMessage(successMessage, InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                ShowMessage(Strings.Format("TaskOperationFailedMessage", ex.Message), InfoBarSeverity.Error);
            }
        }

        private async System.Threading.Tasks.Task<bool?> ConfirmDeleteAsync()
        {
            CheckBox deleteFilesCheckBox = new()
            {
                Content = Strings.Get("DeleteFilesCheckBoxContent")
            };

            StackPanel dialogContent = new()
            {
                Spacing = 12,
                Children =
                {
                    new TextBlock
                    {
                        Text = Strings.Get("DeleteDialogContent"),
                        TextWrapping = TextWrapping.Wrap
                    },
                    deleteFilesCheckBox
                }
            };

            ContentDialog dialog = new()
            {
                XamlRoot = Content.XamlRoot,
                Title = Strings.Get("DeleteDialogTitle"),
                Content = dialogContent,
                PrimaryButtonText = Strings.Get("DeleteButtonText"),
                CloseButtonText = Strings.Get("CancelButtonText"),
                DefaultButton = ContentDialogButton.Close
            };

            ContentDialogResult result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary
                ? deleteFilesCheckBox.IsChecked == true
                : null;
        }

        private async System.Threading.Tasks.Task<bool?> ConfirmClearRecordsAsync()
        {
            CheckBox deleteFilesCheckBox = new()
            {
                Content = Strings.Get("ClearRecordsDeleteFilesCheckBoxContent")
            };

            StackPanel dialogContent = new()
            {
                Spacing = 16,
                Children =
                {
                    new TextBlock
                    {
                        Text = Strings.Get("ClearRecordsDialogContent"),
                        TextWrapping = TextWrapping.Wrap
                    },
                    deleteFilesCheckBox
                }
            };

            StackPanel title = new()
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10,
                Children =
                {
                    new FontIcon
                    {
                        Glyph = "\uE946",
                        Foreground = Application.Current.Resources.TryGetValue("SystemFillColorCautionBrush", out object brush) && brush is Brush cautionBrush
                            ? cautionBrush
                            : null
                    },
                    new TextBlock
                    {
                        Text = Strings.Get("ClearRecordsDialogTitle"),
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                    }
                }
            };

            ContentDialog dialog = new()
            {
                XamlRoot = Content.XamlRoot,
                Title = title,
                Content = dialogContent,
                PrimaryButtonText = Strings.Get("ClearRecordsYesButtonText"),
                CloseButtonText = Strings.Get("ClearRecordsNoButtonText"),
                DefaultButton = ContentDialogButton.Close
            };

            ContentDialogResult result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary
                ? deleteFilesCheckBox.IsChecked == true
                : null;
        }

        private async System.Threading.Tasks.Task RunTaskOperationAsync(
            DownloadTask task,
            Func<IReadOnlyList<DownloadTask>, System.Threading.Tasks.Task> operation,
            string successMessage)
        {
            if (string.IsNullOrWhiteSpace(task.Gid))
            {
                return;
            }

            Aria2EngineStartResult startResult = await EnsureAria2StartedAsync();
            if (!startResult.Started)
            {
                ShowMessage(startResult.Message, InfoBarSeverity.Error);
                return;
            }

            try
            {
                await operation([task]);
                ApplyTaskFilter(_currentTaskFilter);
                UpdateDashboard();
                UpdateGlobalSpeedsFromTasks();
                UpdateTaskDetailsPane();
                await RefreshDownloadsAsync();
                ApplyTaskFilter(_currentTaskFilter);
                UpdateSelectionCommands();
                ShowMessage(successMessage, InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                ShowMessage(Strings.Format("TaskOperationFailedMessage", ex.Message), InfoBarSeverity.Error);
            }
        }

        private static DownloadTask? GetTaskFromSender(object sender)
        {
            return sender is FrameworkElement { DataContext: DownloadTask task }
                ? task
                : null;
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

        private static string ResolveTaskFolderPath(DownloadTask task)
        {
            return !string.IsNullOrWhiteSpace(task.SaveDirectory) && Directory.Exists(task.SaveDirectory)
                ? task.SaveDirectory
                : Path.GetDirectoryName(ResolveTaskFilePath(task)) ?? string.Empty;
        }

        private static void OpenShellTarget(string path)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }

        private List<DownloadTask> GetSelectedTasks()
        {
            return TasksListView.SelectedItems
                .OfType<DownloadTask>()
                .Where(task => !string.IsNullOrWhiteSpace(task.Gid))
                .ToList();
        }

        private DownloadTask? GetSingleSelectedTask()
        {
            List<DownloadTask> selectedTasks = GetSelectedTasks();
            return selectedTasks.Count == 1 ? selectedTasks[0] : null;
        }

        private void UpdateSelectionCommands()
        {
            List<DownloadTask> selectedTasks = GetSelectedTasks();
            bool hasSelection = selectedTasks.Count > 0;
            DownloadTask? singleSelectedTask = selectedTasks.Count == 1 ? selectedTasks[0] : null;

            ResumeTasksButton.IsEnabled = selectedTasks.Any(IsPausedTask);
            PauseTasksButton.IsEnabled = selectedTasks.Any(IsActiveTransferTask);
            RecoverTasksButton.IsEnabled = selectedTasks.Any(IsRecoverableTask);
            DeleteTasksButton.IsEnabled = hasSelection;
            OpenSelectedTaskFileButton.IsEnabled = singleSelectedTask is not null && CanOpenTaskFile(singleSelectedTask);
            OpenSelectedTaskFolderButton.IsEnabled = singleSelectedTask is not null && CanOpenTaskFolder(singleSelectedTask);
            CopySelectedTaskLinksButton.IsEnabled = selectedTasks.Any(task => !string.IsNullOrWhiteSpace(task.SourceUri));
            UpdateSelectAllCheckBox();
        }

        private static bool CanOpenTaskFile(DownloadTask task)
        {
            return IsCompletedTask(task) && File.Exists(ResolveTaskFilePath(task));
        }

        private static bool CanOpenTaskFolder(DownloadTask task)
        {
            return Directory.Exists(ResolveTaskFolderPath(task));
        }

        private static bool IsDownloadingTask(DownloadTask task)
        {
            return task.Status.Contains("download", StringComparison.OrdinalIgnoreCase)
                || task.Status.Contains("waiting", StringComparison.OrdinalIgnoreCase)
                || task.Status.Contains("paused", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsActiveTransferTask(DownloadTask task)
        {
            return IsDownloadingTask(task) && !IsPausedTask(task);
        }

        private static bool IsPausedTask(DownloadTask task)
        {
            return task.Status.Contains("paused", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsCompletedTask(DownloadTask task)
        {
            return IsCompletedTaskStatus(task.Status);
        }

        private static bool IsIssueTask(DownloadTask task)
        {
            return IsErrorTaskStatus(task.Status)
                || task.Status.Contains("removed", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsRecoverableTask(DownloadTask task)
        {
            return IsErrorTaskStatus(task.Status);
        }

        private static bool IsCompletedTaskStatus(string status)
        {
            return status.Contains("complete", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsErrorTaskStatus(string status)
        {
            return status.Contains("error", StringComparison.OrdinalIgnoreCase);
        }

        private static int GetDefaultRecoverySplitCount()
        {
            return 16;
        }

        private string GetSearchQuery()
        {
            return TitleSearchBox?.Text.Trim() ?? string.Empty;
        }

        private static bool IsTaskSearchMatch(DownloadTask task, string query)
        {
            string normalizedQuery = NormalizeSearchText(query);
            if (string.IsNullOrWhiteSpace(normalizedQuery))
            {
                return true;
            }

            string[] terms = query
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (terms.Length == 0)
            {
                terms = [query];
            }

            string searchableText = string.Join(' ', new[]
            {
                task.Name,
                task.StatusText,
                task.ProgressText,
                task.SizeText,
                task.RemainingTimeText,
                task.DownloadSpeedText,
                task.UploadSpeedText
            });

            string normalizedSearchableText = NormalizeSearchText(searchableText);
            return terms
                .Select(NormalizeSearchText)
                .Where(term => !string.IsNullOrWhiteSpace(term))
                .All(normalizedSearchableText.Contains);
        }

        private void ApplySettingsFilter()
        {
            if (SettingsPage is null)
            {
                return;
            }

            string query = GetSearchQuery();
            SetSettingVisibility(StartupSettingCard, query, "startup", "launch", "engine", "aria2", "启动", "引擎");
            SetSettingVisibility(RestoreWindowSettingCard, query, "restore", "window", "position", "size", "启动", "窗口", "位置", "大小");
            SetSettingVisibility(ResumeOnLaunchSettingCard, query, "resume", "restore", "download", "launch", "恢复", "下载", "启动");
            SetSettingVisibility(ExitCleanupSettingCard, query, "clear", "completed", "exit", "cleanup", "清理", "完成", "退出");
            SetSettingVisibility(PauseActiveOnExitSettingCard, query, "pause", "active", "downloading", "exit", "暂停", "下载", "退出");
            SetSettingVisibility(CloseBehaviorSettingCard, query, "close", "tray", "background", "exit", "关闭", "托盘", "后台", "退出");
            SetSettingVisibility(TaskbarProgressSettingCard, query, "taskbar", "progress", "download", "任务栏", "进度");
            SetSettingVisibility(ThemeSettingCard, query, "theme", "appearance", "system", "dark", "light", "主题", "外观");
            SetSettingVisibility(NotificationsSettingCard, query, "notification", "complete", "failed", "通知");
            SetSettingVisibility(DownloadStartNotificationSettingCard, query, "notification", "start", "download", "开始", "通知");
            SetSettingVisibility(DownloadCompleteNotificationSettingCard, query, "notification", "complete", "failed", "download", "完成", "失败", "通知");
            SetSettingVisibility(DownloadCompleteNotificationActionSettingCard, query, "notification", "complete", "click", "action", "file", "folder", "通知", "点击", "动作", "文件", "文件夹");
            SetSettingVisibility(AutoShutdownSettingCard, query, "shutdown", "complete", "download", "关机", "完成");
            SetSettingVisibility(PreventSleepSettingCard, query, "sleep", "power", "download", "休眠", "睡眠", "下载");

            SetSettingVisibility(DefaultDirectorySettingCard, query, "default", "directory", "download", "folder", Strings.Get("DefaultDirectoryLabel.Text"), "目录", "保存");
            SetSettingVisibility(MaxConcurrentDownloadsSettingCard, query, "concurrent", "download", "task", "同时", "下载", "任务");
            SetSettingVisibility(SplitCountSettingCard, query, "split", "segment", "connection", "thread", "分片", "连接数");
            SetSettingVisibility(MaxConnectionPerServerSettingCard, query, "connection", "server", "thread", "服务器", "连接数");
            SetSettingVisibility(ContinueDownloadSettingCard, query, "continue", "resume", "download", "断点", "续传");
            SetSettingVisibility(RemoteTimeSettingCard, query, "remote", "time", "timestamp", "server", "时间戳", "服务器");
            SetSettingVisibility(MaxTriesSettingCard, query, "retry", "tries", "network", "重试");
            SetSettingVisibility(RetryWaitSettingCard, query, "retry", "wait", "seconds", "等待", "重试");
            SetSettingVisibility(DownloadCleanupSettingCard, query, "cleanup", "stale", "record", "清理", "记录");
            SetSettingVisibility(TorrentCleanupSettingCard, query, "torrent", "cleanup", "delete", "种子", "清理", "删除");

            SetSettingVisibility(BtAutoDownloadSettingCard, query, "bittorrent", "torrent", "metalink", "magnet", "auto", "内容", "自动");
            SetSettingVisibility(BtForceEncryptionSettingCard, query, "bittorrent", "encryption", "crypto", "加密");
            SetSettingVisibility(BtKeepSeedingSettingCard, query, "bittorrent", "seed", "keep", "ratio", "time", "bt", "做种", "分享率", "时间");
            SetSettingVisibility(BtMaxPeersSettingCard, query, "bittorrent", "peer", "max", "bt", "连接");
            SetSettingVisibility(BtTrackerSourceSettingCard, query, "bittorrent", "tracker", "source", "sync", "bt", "同步");
            SetSettingVisibility(BtTrackerCustomSourceSettingCard, query, "bittorrent", "tracker", "custom", "url", "bt", "自定义");
            SetSettingVisibility(BtTrackerListSettingCard, query, "bittorrent", "tracker", "list", "bt");
            SetSettingVisibility(BtAutoSyncTrackerSettingCard, query, "bittorrent", "tracker", "auto sync", "bt", "自动同步");

            SetSettingVisibility(UseSystemProxySettingCard, query, "proxy", "system proxy", Strings.Get("ProxyLabel.Text"), "Use Windows system proxy when aria2 starts", "代理");
            SetSettingVisibility(CustomProxySettingCard, query, "proxy", "http", "https", "socks", "custom", "代理");
            SetSettingVisibility(RetrySettingCard, query, "retry", "network", "connection", "重试", "网络");

            SetSettingVisibility(AriaPathSettingCard, query, "aria2c", "path", Strings.Get("AriaPathLabel.Text"), Strings.Get("AriaPathTextBox.PlaceholderText"), "路径");
            SetSettingVisibility(RpcPortSettingCard, query, "rpc", "port", Strings.Get("RpcPortLabel.Text"), "端口");
            SetSettingVisibility(ProcessStatusSettingCard, query, "process", "status", "aria2", Strings.Get("ProcessStatusLabel.Text"), "状态");
            SetSettingVisibility(TerminalSettingCard, query, "terminal", "log", "debug", "aria2", "终端", "日志");

            SetSettingVisibility(AboutAppCard, query, "about", "version", "omnidown", "关于", "版本");
            SetSettingVisibility(AboutCloneCard, query, "clone", "repository", "github", "克隆", "仓库");
            SetSettingVisibility(AboutIssueCard, query, "bug", "issue", "feature", "github", "问题", "建议");
            SetSettingVisibility(AboutReferencesCard, query, "dependencies", "references", "license", "files", "motrix", "aria2", "unigetui", "winui", "依赖", "参考", "许可证");
            SetSettingVisibility(AboutLicenseCard, query, "license", "third-party", "notice", "warranty", "mit", "gpl", "许可证", "第三方", "声明");
        }

        private static void SetSettingVisibility(FrameworkElement? element, string query, params string[] searchableText)
        {
            if (element is null)
            {
                return;
            }

            bool isVisible = string.IsNullOrWhiteSpace(query) || searchableText.Any(text => SearchContains(text, query));
            element.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateSearchPlaceholder()
        {
            if (TitleSearchBox is null)
            {
                return;
            }

            TitleSearchBox.PlaceholderText = _currentTaskFilter == "Settings"
                ? Strings.Get("SearchSettingsPlaceholder")
                : Strings.Get("SearchDownloadsPlaceholder");
        }

        private void UpdateDownloadsHeader(string tag)
        {
            if (DownloadsTitleText is null || tag == "Settings")
            {
                return;
            }

            string resourceKey = tag switch
            {
                "Downloading" => "DownloadingPage",
                "Completed" => "CompletedPage",
                "Issues" => "IssuesPage",
                _ => "HomePage"
            };

            DownloadsTitleText.Text = Strings.Get($"{resourceKey}Title");
        }

        private void UpdateStatsVisibility(string tag)
        {
            if (StatsPanel is null)
            {
                return;
            }

            StatsPanel.Visibility = tag is "Home" or "Downloading" ? Visibility.Visible : Visibility.Collapsed;
            CompletedMetricPanel.Visibility = tag == "Home" ? Visibility.Visible : Visibility.Collapsed;
            IssueMetricPanel.Visibility = tag == "Home" ? Visibility.Visible : Visibility.Collapsed;
            TasksListHeaderPanel.Margin = tag is "Home" or "Downloading"
                ? new Thickness(0, 0, 0, 4)
                : new Thickness(0, 20, 0, 4);
        }

        private IEnumerable<DownloadTask> SortTasks(IEnumerable<DownloadTask> tasks)
        {
            IOrderedEnumerable<DownloadTask> orderedTasks = _sortColumn switch
            {
                TaskSortColumn.CreatedAt => tasks.OrderBy(task => task.CreatedAt),
                TaskSortColumn.Size => tasks.OrderBy(task => task.TotalLength),
                _ => tasks.OrderBy(task => task.Name, StringComparer.CurrentCultureIgnoreCase)
            };

            return _sortAscending ? orderedTasks : orderedTasks.Reverse();
        }

        private void RestoreSelection(HashSet<string> selectedGids)
        {
            _isUpdatingTaskSelection = true;
            try
            {
                TasksListView.SelectedItems.Clear();
                foreach (DownloadTask task in _visibleTasks)
                {
                    bool isSelected = selectedGids.Contains(task.Gid);
                    task.IsSelected = isSelected;
                    if (isSelected)
                    {
                        TasksListView.SelectedItems.Add(task);
                    }
                    UpdateTaskSelectionGlyphVisibility(task);
                }
            }
            finally
            {
                _isUpdatingTaskSelection = false;
            }
        }

        private void SyncVisibleTasks(IReadOnlyList<DownloadTask> desiredTasks)
        {
            for (int index = _visibleTasks.Count - 1; index >= 0; index--)
            {
                if (!desiredTasks.Contains(_visibleTasks[index]))
                {
                    _visibleTasks.RemoveAt(index);
                }
            }

            for (int desiredIndex = 0; desiredIndex < desiredTasks.Count; desiredIndex++)
            {
                DownloadTask desiredTask = desiredTasks[desiredIndex];
                int currentIndex = _visibleTasks.IndexOf(desiredTask);
                if (currentIndex == desiredIndex)
                {
                    continue;
                }

                if (currentIndex >= 0)
                {
                    _visibleTasks.Move(currentIndex, desiredIndex);
                }
                else
                {
                    _visibleTasks.Insert(desiredIndex, desiredTask);
                }
            }
        }

        private void UpdateSelectAllCheckBox()
        {
            if (SelectAllTasksCheckBox is null)
            {
                return;
            }

            _isUpdatingSelectAllCheckBox = true;
            try
            {
                int itemCount = _visibleTasks.Count;
                int selectedCount = TasksListView.SelectedItems.Count;
                SelectAllTasksCheckBox.IsEnabled = itemCount > 0;
                SelectAllTasksCheckBox.IsChecked = selectedCount switch
                {
                    0 => false,
                    _ when selectedCount == itemCount => true,
                    _ => null
                };
            }
            finally
            {
                _isUpdatingSelectAllCheckBox = false;
            }
        }

        private void UpdateTaskSelectionGlyphVisibility(DownloadTask task)
        {
            if (TasksListView.ContainerFromItem(task) is ListViewItem itemContainer &&
                FindDescendant<UserControl>(itemContainer) is UserControl userControl)
            {
                VisualStateManager.GoToState(userControl, task.IsSelected ? "ShowCheckbox" : "HideCheckbox", true);
            }
        }

        private static void UpdateTaskSelectionGlyphVisibility(FrameworkElement element, bool isPointerOver)
        {
            UserControl? userControl = FindAncestor<UserControl>(element);
            if (userControl?.DataContext is DownloadTask task)
            {
                VisualStateManager.GoToState(userControl, task.IsSelected || isPointerOver ? "ShowCheckbox" : "HideCheckbox", true);
            }
        }

        private static T? FindAncestor<T>(DependencyObject? start) where T : DependencyObject
        {
            DependencyObject? current = start;
            while (current is not null)
            {
                if (current is T match)
                {
                    return match;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }

        private static T? FindDescendant<T>(DependencyObject start) where T : DependencyObject
        {
            int count = VisualTreeHelper.GetChildrenCount(start);
            for (int index = 0; index < count; index++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(start, index);
                if (child is T match)
                {
                    return match;
                }

                T? descendant = FindDescendant<T>(child);
                if (descendant is not null)
                {
                    return descendant;
                }
            }

            return null;
        }

        private static bool SearchContains(string? value, string query)
        {
            string normalizedValue = NormalizeSearchText(value);
            string normalizedQuery = NormalizeSearchText(query);
            return !string.IsNullOrWhiteSpace(normalizedValue)
                && !string.IsNullOrWhiteSpace(normalizedQuery)
                && normalizedValue.Contains(normalizedQuery, StringComparison.Ordinal);
        }

        private static string NormalizeSearchText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            StringBuilder builder = new(value.Length);
            foreach (char character in value.Normalize(NormalizationForm.FormKC))
            {
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(character);
                if (category is UnicodeCategory.LowercaseLetter
                    or UnicodeCategory.UppercaseLetter
                    or UnicodeCategory.TitlecaseLetter
                    or UnicodeCategory.ModifierLetter
                    or UnicodeCategory.OtherLetter
                    or UnicodeCategory.DecimalDigitNumber
                    or UnicodeCategory.LetterNumber
                    or UnicodeCategory.OtherNumber)
                {
                    builder.Append(char.ToLowerInvariant(character));
                }
            }

            return builder.ToString();
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
    }
}
