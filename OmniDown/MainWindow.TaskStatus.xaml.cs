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
        private void ApplyTaskFilter(string tag)
        {
            string query = GetSearchQuery();
            HashSet<string> selectedGids = GetSelectedTasks()
                .Select(task => task.Gid)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            IEnumerable<DownloadTask> filteredTasks = tag switch
            {
                "Downloading" => Tasks.Where(task => IsDownloadingTask(task) && IsTaskSearchMatch(task, query)),
                "Completed" => Tasks.Where(task => IsCompletedTask(task) && IsTaskSearchMatch(task, query)),
                "Issues" => Tasks.Where(task => IsIssueTask(task) && IsTaskSearchMatch(task, query)),
                _ => Tasks.Where(task => IsTaskSearchMatch(task, query))
            };

            SyncVisibleTasks(SortTasks(filteredTasks).ToList());
            RestoreSelection(selectedGids);
            UpdateSelectionCommands();
            UpdateTaskDetailsPane();
        }

        private async void RefreshTimer_Tick(object? sender, object e)
        {
            await RefreshDownloadsAsync();
        }

        private async System.Threading.Tasks.Task<Aria2EngineStartResult> EnsureAria2StartedAsync()
        {
            AdvancedSettings advancedSettings = _settingsPageViewModel.AdvancedSettings;
            int rpcPort = advancedSettings.RpcPort;
            _rpcSecret = advancedSettings.RpcSecret;
            DownloadSettings downloadSettings = _settingsPageViewModel.DownloadSettings;
            _aria2RpcClient.Configure(rpcPort, _rpcSecret);

            Aria2EngineStartResult result = await _aria2EngineHost.StartAsync(new Aria2EngineOptions(
                string.IsNullOrWhiteSpace(advancedSettings.Aria2Path) ? null : advancedSettings.Aria2Path,
                rpcPort,
                downloadSettings.DownloadDirectory,
                _rpcSecret,
                UseSystemProxyCheckBox.IsOn,
                downloadSettings.MaxConcurrentDownloads,
                downloadSettings.SplitCount,
                downloadSettings.MaxConnectionPerServer,
                downloadSettings.ContinueDownloads,
                downloadSettings.RemoteTime,
                downloadSettings.MaxTries,
                downloadSettings.RetryWaitSeconds,
                _settingsPageViewModel.NetworkSettings,
                _settingsPageViewModel.BitTorrentSettings,
                advancedSettings));

            if (!result.Started)
            {
                return result;
            }

            try
            {
                await _aria2RpcClient.PingAsync();
                await ApplyConfiguredSpeedLimitsAsync();
                _refreshTimer.Start();
                await _downloadCoordinator.RemoveCompletedDownloadResultsAsync();
                await RefreshDownloadsAsync();
            }
            catch (Exception ex)
            {
                _aria2EngineHost.Stop();
                UpdateDebugStatus();
                return Aria2EngineStartResult.Failure($"aria2 started but RPC is not reachable: {ex.Message}");
            }

            return result;
        }

        private async System.Threading.Tasks.Task SaveAriaSessionIfRunningAsync()
        {
            if (!_aria2EngineHost.IsRunning)
            {
                return;
            }

            try
            {
                await _downloadCoordinator.RemoveCompletedDownloadResultsAsync();
                await _aria2RpcClient.SaveSessionAsync();
            }
            catch
            {
                // Best-effort: Stop/close must continue even if aria2 is no longer reachable.
            }
        }

        private async System.Threading.Tasks.Task RefreshDownloadsAsync()
        {
            if (_isRefreshing || !_aria2EngineHost.IsRunning)
            {
                if (!_aria2EngineHost.IsRunning)
                {
                    UpdateGlobalSpeeds(0, 0);
                    _taskbarProgress.Clear();
                }

                return;
            }

            _isRefreshing = true;
            try
            {
                DownloadSnapshot snapshot = await _downloadCoordinator.RefreshAsync();
                ShowTaskStatusNotifications();
                ApplyTaskFilter(_currentTaskFilter);
                UpdateDashboard();
                UpdateGlobalSpeeds(snapshot.DownloadSpeed, snapshot.UploadSpeed);
                UpdateTaskbarProgressFromTasks();
                UpdateSystemSleepOverride();
                TryAutoShutdownWhenDownloadsComplete();
                UpdateAriaStatus();
            }
            catch (Exception ex)
            {
                UpdateDebugStatus();
                ShowMessage(Strings.Format("RpcRefreshFailedMessage", ex.Message), InfoBarSeverity.Warning);
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        private void TaskDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            _isTaskDetailsPaneOpen = !_isTaskDetailsPaneOpen;
            UpdateTaskDetailsPaneVisibility();
            UpdateTaskDetailsPane();
        }

        private void TaskDetailsCloseButton_Click(object sender, RoutedEventArgs e)
        {
            _isTaskDetailsPaneOpen = false;
            UpdateTaskDetailsPaneVisibility();
        }

        private void UpdateTaskDetailsPaneVisibility()
        {
            if (TaskDetailsPane is null)
            {
                return;
            }

            bool canShow = _isTaskDetailsPaneOpen && _currentTaskFilter != "Settings";
            TaskDetailsHostColumn.Width = canShow ? new GridLength(360) : new GridLength(0);
            TaskDetailsPane.Visibility = canShow ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateTaskDetailsPane()
        {
            if (TaskDetailsPane is null)
            {
                return;
            }

            List<DownloadTask> selectedTasks = GetSelectedTasks();
            if (selectedTasks.Count != 1)
            {
                TaskDetailsPane.SelectedTaskCount = selectedTasks.Count;
                TaskDetailsPane.SelectedTask = null;
                return;
            }

            TaskDetailsPane.SelectedTaskCount = selectedTasks.Count;
            TaskDetailsPane.SelectedTask = selectedTasks[0];
        }

        private void UpdateDashboard()
        {
            bool isTransferPage = _currentTaskFilter == "Downloading";
            TotalMetricLabelText.Text = isTransferPage ? Strings.Get("DownloadingPageTitle") : Strings.Get("TotalMetricLabel.Text");
            ActiveMetricLabelText.Text = Strings.Get("ActiveMetricLabel.Text");
            PausedMetricLabelText.Text = Strings.Get("PausedMetricLabel");
            CompletedMetricLabelText.Text = Strings.Get("DownloadSpeedMetricLabel.Text");
            IssueMetricLabelText.Text = Strings.Get("Aria2MetricLabel.Text");

            TotalTasksText.Text = (isTransferPage ? Tasks.Count(IsDownloadingTask) : Tasks.Count).ToString();
            ActiveTasksText.Text = Tasks.Count(IsActiveTransferTask).ToString();
            PausedTasksText.Text = Tasks.Count(IsPausedTask).ToString();
            CompletedTasksText.Text = Tasks.Count(IsCompletedTask).ToString();
            IssueTasksText.Text = Tasks.Count(IsIssueTask).ToString();
        }

        private void UpdateGlobalSpeeds(long downloadSpeed, long uploadSpeed)
        {
            if (GlobalDownloadSpeedText is not null)
            {
                GlobalDownloadSpeedText.Text = FormatSpeed(downloadSpeed);
            }

            if (GlobalUploadSpeedText is not null)
            {
                GlobalUploadSpeedText.Text = FormatSpeed(uploadSpeed);
            }
        }

        private void UpdateGlobalSpeedsFromTasks()
        {
            UpdateGlobalSpeeds(
                Tasks.Sum(task => task.DownloadSpeed),
                Tasks.Sum(task => task.UploadSpeed));
        }

        private void HideToTray()
        {
            _taskbarProgress.Clear();
            WindowVisibilityService.Hide(_windowHandle);
        }

        private void ShowFromTray()
        {
            WindowVisibilityService.ShowAndActivate(_windowHandle);
        }

        private void RequestExit()
        {
            _isExitRequested = true;
            Close();
        }

        private async Task PrepareDownloadsForShutdownAsync()
        {
            if (_isShutdownPrepared || !_aria2EngineHost.IsRunning)
            {
                return;
            }

            _isShutdownPrepared = true;
            try
            {
                await RefreshDownloadsAsync();

                if (_settingsPageViewModel.GeneralSettings.PauseActiveOnExit)
                {
                    DownloadTask[] activeTasks = Tasks.Where(IsActiveTransferTask).ToArray();
                    if (activeTasks.Length > 0)
                    {
                        await _downloadCoordinator.PauseAsync(activeTasks);
                    }
                }

                if (_settingsPageViewModel.GeneralSettings.AutoClearCompletedOnExit)
                {
                    await _downloadCoordinator.ClearCompletedAsync(deleteFiles: false);
                }

                ApplyTaskFilter(_currentTaskFilter);
                UpdateDashboard();
                UpdateSelectionCommands();
            }
            catch
            {
                // Exit rules are best-effort; shutdown should not be blocked by a stale RPC connection.
            }
        }

        private async Task<bool?> AskCloseBehaviorAsync()
        {
            if (_isClosePromptOpen)
            {
                return null;
            }

            _isClosePromptOpen = true;
            try
            {
                ContentDialog dialog = new()
                {
                    XamlRoot = Content.XamlRoot,
                    Title = Strings.Get("CloseBehaviorDialogTitle"),
                    Content = Strings.Get("CloseBehaviorDialogContent"),
                    PrimaryButtonText = Strings.Get("CloseBehaviorMinimizeButtonText"),
                    SecondaryButtonText = Strings.Get("CloseBehaviorExitButtonText"),
                    CloseButtonText = Strings.Get("CancelButtonText"),
                    DefaultButton = ContentDialogButton.Primary
                };

                ContentDialogResult result = await dialog.ShowAsync();
                return result switch
                {
                    ContentDialogResult.Primary => true,
                    ContentDialogResult.Secondary => false,
                    _ => null
                };
            }
            finally
            {
                _isClosePromptOpen = false;
            }
        }

        private void UpdateTaskbarProgressFromTasks()
        {
            if (!_settingsPageViewModel.GeneralSettings.ShowTaskbarProgress)
            {
                _taskbarProgress.Clear();
                return;
            }

            DownloadTask[] activeTasks = Tasks
                .Where(task => task.Status.Contains("download", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (activeTasks.Length == 0)
            {
                _taskbarProgress.Clear();
                return;
            }

            long totalLength = activeTasks
                .Where(task => task.TotalLength > 0)
                .Sum(task => task.TotalLength);
            long completedLength = activeTasks
                .Where(task => task.CompletedLength > 0)
                .Sum(task => task.CompletedLength);

            double progress = totalLength > 0
                ? Math.Clamp(completedLength / (double)totalLength, 0, 1)
                : 0;

            _taskbarProgress.SetProgress(progress);
        }

        private void TryAutoShutdownWhenDownloadsComplete()
        {
            if (!_settingsPageViewModel.GeneralSettings.AutoShutdownWhenComplete || _hasTriggeredAutoShutdown)
            {
                return;
            }

            bool hasCompletedTask = Tasks.Any(IsCompletedTask);
            bool hasRunningTask = Tasks.Any(IsActiveTransferTask) || Tasks.Any(task => task.Status.Contains("waiting", StringComparison.OrdinalIgnoreCase));
            if (hasRunningTask)
            {
                _hasSeenActiveDownloadsForAutoShutdown = true;
                return;
            }

            if (!hasCompletedTask || !_hasSeenActiveDownloadsForAutoShutdown)
            {
                return;
            }

            _hasTriggeredAutoShutdown = true;
            try
            {
                SystemShutdownService.ShutdownNow();
            }
            catch (Exception ex)
            {
                ShowMessage($"自动关机失败：{ex.Message}", InfoBarSeverity.Warning);
            }
        }

        private void UpdateSystemSleepOverride()
        {
            bool shouldStayAwake = _settingsPageViewModel.GeneralSettings.PreventSleepWhileDownloading &&
                Tasks.Any(task => task.Status.Contains("download", StringComparison.OrdinalIgnoreCase));

            if (shouldStayAwake)
            {
                SystemSleepOverrideService.KeepSystemAwake();
            }
            else
            {
                ReleaseSystemSleepOverride();
            }
        }

        private static void ReleaseSystemSleepOverride()
        {
            SystemSleepOverrideService.Release();
        }

        private void UpdateGlobalSpeedLimitText()
        {
            bool showUploadLimit = _isUploadSpeedLimitEnabled && _uploadLimitBytesPerSecond > 0;
            if (GlobalUploadLimitIconPanel is not null)
            {
                GlobalUploadLimitIconPanel.Opacity = showUploadLimit ? 1 : 0;
            }

            if (GlobalUploadLimitText is not null)
            {
                GlobalUploadLimitText.Opacity = showUploadLimit ? 1 : 0;
                GlobalUploadLimitText.Text = showUploadLimit ? FormatSpeed(_uploadLimitBytesPerSecond) : string.Empty;
            }

            bool showDownloadLimit = _isDownloadSpeedLimitEnabled && _downloadLimitBytesPerSecond > 0;
            if (GlobalDownloadLimitIconPanel is not null)
            {
                GlobalDownloadLimitIconPanel.Opacity = showDownloadLimit ? 1 : 0;
            }

            if (GlobalDownloadLimitText is not null)
            {
                GlobalDownloadLimitText.Opacity = showDownloadLimit ? 1 : 0;
                GlobalDownloadLimitText.Text = showDownloadLimit ? FormatSpeed(_downloadLimitBytesPerSecond) : string.Empty;
            }
        }

        private void UpdateAriaStatus()
        {
            bool isRunning = _aria2EngineHost.IsRunning;
            string status = isRunning
                ? Strings.Format("AriaRunningStatus", _aria2EngineHost.ProcessId ?? 0)
                : Strings.Get("AriaStoppedStatus");

            SettingsAriaStatusText.Text = status;
            if (AriaStartStopIcon is not null)
            {
                AriaStartStopIcon.Glyph = isRunning ? "\uE769" : "\uE768";
            }

            if (AriaStartStopButton is not null)
            {
                ToolTipService.SetToolTip(AriaStartStopButton, isRunning ? "停止" : "启动");
            }

            if (AriaRestartButton is not null)
            {
                AriaRestartButton.IsEnabled = isRunning;
            }

            UpdateDebugStatus();
        }

        private void ShowMessage(string message, InfoBarSeverity severity)
        {
            _statusMessages.Insert(0, new AppStatusMessage(
                message,
                FormatStatusMessageDetail(DateTimeOffset.Now),
                GetSeverityText(severity),
                GetSeverityGlyph(severity),
                GetSeverityBrush(severity)));
            StatusToastMessageText.Text = message;
            StatusToastGlyph.Glyph = GetSeverityGlyph(severity);
            StatusToastGlyph.Foreground = GetSeverityBrush(severity);
            StatusToastPanel.BorderBrush = GetSeverityBrush(severity);
            StatusToastPanel.Visibility = Visibility.Visible;
            _statusMessageTimer.Stop();
            _statusMessageTimer.Start();
        }

        private void StatusMessageTimer_Tick(object? sender, object e)
        {
            _statusMessageTimer.Stop();
            StatusToastPanel.Visibility = Visibility.Collapsed;
        }

        private static string FormatStatusMessageDetail(DateTimeOffset timestamp)
        {
            return timestamp.ToString("HH:mm:ss", CultureInfo.CurrentCulture);
        }

        private static string GetSeverityText(InfoBarSeverity severity)
        {
            return severity switch
            {
                InfoBarSeverity.Success => Strings.Get("NotificationSeveritySuccess"),
                InfoBarSeverity.Warning => Strings.Get("NotificationSeverityWarning"),
                InfoBarSeverity.Error => Strings.Get("NotificationSeverityError"),
                _ => Strings.Get("NotificationSeverityInfo")
            };
        }

        private static string GetSeverityGlyph(InfoBarSeverity severity)
        {
            return severity switch
            {
                InfoBarSeverity.Success => "\uE73E",
                InfoBarSeverity.Warning => "\uE7BA",
                InfoBarSeverity.Error => "\uEA39",
                _ => "\uE946"
            };
        }

        private static Brush GetSeverityBrush(InfoBarSeverity severity)
        {
            return severity switch
            {
                InfoBarSeverity.Success => GetResourceBrush("SystemFillColorSuccessBrush", new SolidColorBrush(Colors.ForestGreen)),
                InfoBarSeverity.Warning => GetResourceBrush("SystemFillColorCautionBrush", new SolidColorBrush(Colors.Goldenrod)),
                InfoBarSeverity.Error => GetResourceBrush("SystemFillColorCriticalBrush", new SolidColorBrush(Colors.Firebrick)),
                _ => GetResourceBrush("AccentFillColorDefaultBrush", new SolidColorBrush(Colors.DodgerBlue))
            };
        }

        private static Brush GetResourceBrush(string key, Brush fallback)
        {
            return Application.Current.Resources.TryGetValue(key, out object value) && value is Brush brush
                ? brush
                : fallback;
        }

        private void RecordObservedTaskStatuses()
        {
            foreach (DownloadTask task in Tasks.Where(task => !string.IsNullOrWhiteSpace(task.Gid)))
            {
                _observedTaskStatuses[task.Gid] = task.Status;
            }
        }

        private void ShowTaskStatusNotifications()
        {
            HashSet<string> currentGids = new(StringComparer.OrdinalIgnoreCase);

            foreach (DownloadTask task in Tasks.Where(task => !string.IsNullOrWhiteSpace(task.Gid)))
            {
                currentGids.Add(task.Gid);
                if (_observedTaskStatuses.TryGetValue(task.Gid, out string? previousStatus))
                {
                    if (!IsCompletedTaskStatus(previousStatus) && IsCompletedTask(task))
                    {
                        ShowDownloadCompletedNotification(task);
                    }
                    else if (!IsErrorTaskStatus(previousStatus) && IsErrorTaskStatus(task.Status))
                    {
                        ShowDownloadFailedNotification(task);
                    }
                }

                _observedTaskStatuses[task.Gid] = task.Status;
            }

            foreach (string staleGid in _observedTaskStatuses.Keys.Except(currentGids, StringComparer.OrdinalIgnoreCase).ToArray())
            {
                _observedTaskStatuses.Remove(staleGid);
            }
        }

        private void ShowTaskAddedNotification(DownloadTask task)
        {
            if (_settingsPageViewModel.GeneralSettings.SystemNotificationsEnabled && _settingsPageViewModel.GeneralSettings.DownloadStartNotificationsEnabled)
            {
                _notifications.ShowTaskAdded(task);
            }
        }

        private void ShowDownloadCompletedNotification(DownloadTask task)
        {
            if (_settingsPageViewModel.GeneralSettings.SystemNotificationsEnabled && _settingsPageViewModel.GeneralSettings.DownloadCompleteNotificationsEnabled)
            {
                _notifications.ShowDownloadCompleted(task);
            }
        }

        private void ShowDownloadFailedNotification(DownloadTask task)
        {
            if (_settingsPageViewModel.GeneralSettings.SystemNotificationsEnabled && _settingsPageViewModel.GeneralSettings.DownloadCompleteNotificationsEnabled)
            {
                _notifications.ShowDownloadFailed(task);
            }
        }

        private void SetTaskListLoading(bool isLoading)
        {
            if (TasksLoadingPanel is null || TasksLoadingRing is null)
            {
                return;
            }

            TasksLoadingRing.IsActive = isLoading;
            TasksLoadingPanel.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            TasksListView.Visibility = isLoading ? Visibility.Collapsed : Visibility.Visible;
        }

        private void UpdateDebugStatus()
        {
            if (DebugEngineText is null ||
                TerminalTextBlock is null ||
                UseSystemProxyCheckBox is null)
            {
                return;
            }

            string engineStatus = _aria2EngineHost.IsRunning
                ? Strings.Format("DebugAriaRunningStatus", _aria2EngineHost.ProcessId ?? 0)
                : Strings.Get("DebugAriaStoppedStatus");

            DebugEngineText.Text = engineStatus;
            TerminalTextBlock.Text = _aria2EngineHost.TerminalText;
            TerminalScrollViewer?.ChangeView(null, double.MaxValue, null);
        }
    }
}
