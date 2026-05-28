using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using CommunityToolkit.WinUI.Animations;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.Win32;
using OmniDown.Models;
using OmniDown.Models.Settings;
using OmniDown.Services.Downloads;
using OmniDown.Services.Engine;
using OmniDown.Services.Localization;
using OmniDown.Services.Logging;
using OmniDown.Services.Notifications;
using OmniDown.Services.Rpc;
using OmniDown.Services.Settings;
using OmniDown.Services.Shell;
using OmniDown.Services.Storage;
using OmniDown.Services.Widgets;
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
            UpdateStatusBar();
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
            AppLogger.Info("Aria2Startup", $"ensure-start rpcPort={rpcPort} downloadDir={downloadSettings.DownloadDirectory}");
            _aria2RpcClient.Configure(rpcPort, _rpcSecret);

            Aria2EngineStartResult result = await _aria2EngineHost.StartAsync(new Aria2EngineOptions(
                string.IsNullOrWhiteSpace(advancedSettings.Aria2Path) ? null : advancedSettings.Aria2Path,
                advancedSettings.EngineType,
                rpcPort,
                downloadSettings.DownloadDirectory,
                _rpcSecret,
                _settingsPageViewModel.NetworkSettings.UseSystemProxy,
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
                AppLogger.Info("Aria2Startup", "RPC ping succeeded");
                await ApplyConfiguredSpeedLimitsAsync();
                _refreshTimer.Start();
                await _downloadCoordinator.PurgeCompletedResultsFromAria2SessionAsync();
                await RefreshDownloadsAsync();
            }
            catch (Exception ex)
            {
                AppLogger.Error("Aria2Startup", ex);
                _aria2EngineHost.Stop();
                UpdateDebugStatus();
                return Aria2EngineStartResult.Failure($"aria2 started but RPC is not reachable: {ex.Message}");
            }

            _runningAriaSettingsSignature = CreateAriaRestartSettingsSignature();
            _runningAriaRpcPort = rpcPort;
            _runningAriaRpcSecret = _rpcSecret;
            UpdateAriaRestartNotification();
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
                AppLogger.Info("Aria2Shutdown", "saving aria2 session");
                if (_runningAriaRpcPort > 0 && !string.IsNullOrWhiteSpace(_runningAriaRpcSecret))
                {
                    _aria2RpcClient.Configure(_runningAriaRpcPort, _runningAriaRpcSecret);
                }

                await _downloadCoordinator.RemoveCompletedDownloadResultsAsync();
                await _aria2RpcClient.SaveSessionAsync();
                await System.Threading.Tasks.Task.Delay(500);
            }
            catch (Exception ex)
            {
                AppLogger.Warning("Aria2Shutdown", $"save session failed: {ex.Message}");
                // Best-effort: Stop/close must continue even if aria2 is no longer reachable.
            }
            finally
            {
                AdvancedSettings advancedSettings = _settingsPageViewModel.AdvancedSettings;
                _rpcSecret = advancedSettings.RpcSecret;
                _aria2RpcClient.Configure(advancedSettings.RpcPort, _rpcSecret);
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
                UpdateWidgets(snapshot);
                UpdateTaskbarProgressFromTasks();
                UpdateSystemSleepOverride();
                TryAutoShutdownWhenDownloadsComplete();
                UpdateAriaStatus();
            }
            catch (Exception ex)
            {
                AppLogger.Warning("Aria2Refresh", ex.Message);
                UpdateDebugStatus();
                ShowMessageOnce(Strings.Format("RpcRefreshFailedMessage", ex.Message), InfoBarSeverity.Warning);
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        private void UpdateWidgets(DownloadSnapshot snapshot)
        {
            try
            {
                var widgetSnapshot = WidgetSnapshot.FromTasks(
                    Tasks, snapshot.DownloadSpeed, snapshot.UploadSpeed, _aria2EngineHost.IsRunning);
                _ = _widgetSnapshotStore.SaveAsync(widgetSnapshot);
                new WidgetUpdateService().UpdateAll(widgetSnapshot);
            }
            catch
            {
                // Widget updates are best-effort; failures here should not surface as RPC errors.
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

            if (StatusBarDownloadSpeedText is not null)
            {
                StatusBarDownloadSpeedText.Text = FormatSpeed(downloadSpeed);
            }

            if (StatusBarUploadSpeedText is not null)
            {
                StatusBarUploadSpeedText.Text = FormatSpeed(uploadSpeed);
            }
        }

        private void UpdateGlobalSpeedsFromTasks()
        {
            UpdateGlobalSpeeds(
                Tasks.Sum(task => task.DownloadSpeed),
                Tasks.Sum(task => task.UploadSpeed));
        }

        private void UpdateStatusBar()
        {
            if (StatusBarItemCountText is null)
            {
                return;
            }

            bool isSettings = _currentTaskFilter == "Settings";
            StatusBarPanel.Visibility = isSettings ? Visibility.Collapsed : Visibility.Visible;
            if (isSettings)
            {
                return;
            }

            bool showTransferSummary = _currentTaskFilter is "Home" or "Downloading";
            bool showIssues = _currentTaskFilter == "Home";
            int selectedItemCount = GetSelectedTasks().Count;

            StatusBarItemCountText.Text = FormatStatusBarItemCount(_visibleTasks.Count);
            StatusBarSelectedCountText.Text = FormatStatusBarSelectedItemCount(selectedItemCount);
            StatusBarSelectedCountText.Visibility = selectedItemCount > 0 ? Visibility.Visible : Visibility.Collapsed;
            StatusBarSelectedCountDivider.Visibility = selectedItemCount > 0 ? Visibility.Visible : Visibility.Collapsed;
            StatusBarTaskCountsDivider.Visibility = showTransferSummary ? Visibility.Visible : Visibility.Collapsed;
            StatusBarTaskCountsPanel.Visibility = showTransferSummary ? Visibility.Visible : Visibility.Collapsed;
            StatusBarSpeedPanel.Visibility = showTransferSummary ? Visibility.Visible : Visibility.Collapsed;
            StatusBarIssueTasksPanel.Visibility = showIssues ? Visibility.Visible : Visibility.Collapsed;

            if (!showTransferSummary)
            {
                return;
            }

            StatusBarActiveTasksText.Text = _visibleTasks.Count(IsActiveTransferTask).ToString(CultureInfo.CurrentCulture);
            StatusBarPausedTasksText.Text = _visibleTasks.Count(IsPausedTask).ToString(CultureInfo.CurrentCulture);
            StatusBarIssueTasksText.Text = _visibleTasks.Count(IsIssueTask).ToString(CultureInfo.CurrentCulture);
        }

        private static string FormatStatusBarItemCount(int itemCount)
        {
            return Strings.Format("StatusBarItemCountText", itemCount);
        }

        private static string FormatStatusBarSelectedItemCount(int itemCount)
        {
            return Strings.Format("StatusBarSelectedItemCountText", itemCount);
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

        private void PurgeCompletedTasksFromCacheFile()
        {
            string cachePath = Path.Combine(AppPaths.LocalDataDirectory, "tasks.json");
            if (!File.Exists(cachePath))
            {
                return;
            }

            try
            {
                string json = File.ReadAllText(cachePath);
                List<CachedDownloadTask>? cached = JsonSerializer.Deserialize<List<CachedDownloadTask>>(json);
                if (cached is null)
                {
                    return;
                }

                List<CachedDownloadTask> filtered = cached
                    .Where(entry => !entry.Status.Contains("complete", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (filtered.Count == cached.Count)
                {
                    return;
                }

                AppLogger.Info("Startup", $"PurgeCompletedTasks: removing {cached.Count - filtered.Count} completed tasks from cache");
                File.WriteAllText(cachePath, JsonSerializer.Serialize(filtered, new JsonSerializerOptions
                {
                    WriteIndented = true
                }));
            }
            catch (Exception ex)
            {
                AppLogger.Warning("Startup", $"PurgeCompletedTasksFromCacheFile failed: {ex.Message}");
            }
        }

        private async Task PrepareDownloadsForShutdownAsync()
        {
            if (_isShutdownPrepared)
            {
                return;
            }

            _isShutdownPrepared = true;

            if (_aria2EngineHost.IsRunning)
            {
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
                }
                catch
                {
                    // Pause-on-exit is best-effort; shutdown should not be blocked by a stale RPC connection.
                }
            }

            if (_settingsPageViewModel.GeneralSettings.AutoClearCompletedOnExit)
            {
                AppLogger.Info("Shutdown", "AutoClearCompletedOnExit enabled, clearing completed tasks");
                try
                {
                    int cleared = await _downloadCoordinator.ClearCompletedAsync(deleteFiles: false);
                    AppLogger.Info("Shutdown", $"ClearCompletedAsync removed {cleared} tasks from memory");
                }
                catch (Exception ex)
                {
                    AppLogger.Warning("Shutdown", $"ClearCompletedAsync failed: {ex.Message}");
                }

                // Safety net: directly purge completed entries from tasks.json on disk,
                // in case SaveTaskCache inside ClearCompletedAsync failed silently.
                _downloadCoordinator.PurgeCompletedTasksFromCacheFile();
            }

            ApplyTaskFilter(_currentTaskFilter);
            UpdateDashboard();
            UpdateSelectionCommands();
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

        private async void TryAutoShutdownWhenDownloadsComplete()
        {
            if (!_settingsPageViewModel.GeneralSettings.AutoShutdownWhenComplete || _hasTriggeredAutoShutdown)
            {
                return;
            }

            bool hasCompletedTask = Tasks.Any(IsCompletedTask);
            bool hasRunningTask = Tasks.Any(IsActiveTransferTask);
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
            _isShutdownPrepared = true;
            try
            {
                if (_settingsPageViewModel.GeneralSettings.AutoClearCompletedOnExit)
                {
                    await _downloadCoordinator.ClearCompletedAsync(deleteFiles: false);
                }

                await SaveAriaSessionIfRunningAsync();
            }
            catch
            {
                // Best-effort cleanup before shutdown.
            }

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

            if (StatusBarUploadLimitText is not null)
            {
                StatusBarUploadLimitText.Text = showUploadLimit ? FormatSpeed(_uploadLimitBytesPerSecond) : string.Empty;
            }

            if (StatusBarUploadLimitPanel is not null)
            {
                StatusBarUploadLimitPanel.Visibility = showUploadLimit ? Visibility.Visible : Visibility.Collapsed;
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

            if (StatusBarDownloadLimitText is not null)
            {
                StatusBarDownloadLimitText.Text = showDownloadLimit ? FormatSpeed(_downloadLimitBytesPerSecond) : string.Empty;
            }

            if (StatusBarDownloadLimitPanel is not null)
            {
                StatusBarDownloadLimitPanel.Visibility = showDownloadLimit ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void StatusBarSpeedButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement placementTarget)
            {
                SpeedLimitButton.Flyout?.ShowAt(placementTarget);
            }
        }

        private void UpdateAriaStatus()
        {
            bool isRunning = _aria2EngineHost.IsRunning;
            string status = isRunning
                ? Strings.Format("AriaRunningStatus", _aria2EngineHost.ProcessId ?? 0)
                : Strings.Get("AriaStoppedStatus");

            SettingsAriaStatusText.Text = status;
            EngineVersionText.Text = string.IsNullOrEmpty(_aria2EngineHost.EngineVariant)
                ? "未检测"
                : _aria2EngineHost.EngineVariant;
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
            _lastStatusMessage = message;
            _statusMessages.Insert(0, new AppStatusMessage(
                message,
                FormatStatusMessageDetail(DateTimeOffset.Now),
                GetSeverityText(severity),
                GetSeverityGlyph(severity),
                GetSeverityBrush(severity)));
            StatusToastInfoBar.Title = GetSeverityText(severity);
            StatusToastInfoBar.Message = message;
            StatusToastInfoBar.Severity = severity;
            _statusToastActionRestartsAria = false;
            StatusToastActionIcon.Glyph = "\uE8C8";
            StatusToastActionText.Text = Strings.Get("StatusToastCopyButtonLabel.Text");
            StatusToastActionButton.Visibility = IsCopyableStatusMessage(severity) ? Visibility.Visible : Visibility.Collapsed;
            AppLogger.Write(ToLogLevel(severity), "UI", message);
            _statusMessageTimer.Stop();
            _statusMessageTimer.Start();
            AnimateInfoBarShow();
        }

        private void ShowMessageOnce(string message, InfoBarSeverity severity)
        {
            if (message.Equals(_lastStatusMessage, StringComparison.Ordinal))
            {
                return;
            }

            ShowMessage(message, severity);
        }

        private void AnimateInfoBarShow()
        {
            _isHiding = false;
            StatusToastInfoBar.Visibility = Visibility.Visible;
            StatusToastInfoBar.IsOpen = true;

            AnimationBuilder.Create()
                .Opacity(to: 1, from: 0,
                    duration: TimeSpan.FromMilliseconds(250),
                    easingType: EasingType.Cubic, easingMode: EasingMode.EaseOut)
                .Translation(Axis.Y, to: 0, from: 30,
                    duration: TimeSpan.FromMilliseconds(300),
                    easingType: EasingType.Cubic, easingMode: EasingMode.EaseOut)
                .Start(StatusToastInfoBar);
        }

        private void AnimateInfoBarHide()
        {
            if (_isHiding || !StatusToastInfoBar.IsOpen)
                return;
            _isHiding = true;

            AnimationBuilder.Create()
                .Opacity(to: 0, from: 1,
                    duration: TimeSpan.FromMilliseconds(150),
                    easingType: EasingType.Cubic, easingMode: EasingMode.EaseIn)
                .Translation(Axis.Y, to: 20, from: 0,
                    duration: TimeSpan.FromMilliseconds(150),
                    easingType: EasingType.Cubic, easingMode: EasingMode.EaseIn)
                .Start(StatusToastInfoBar, () =>
                {
                    if (!_isHiding) return;
                    StatusToastInfoBar.IsOpen = false;
                    StatusToastInfoBar.Visibility = Visibility.Collapsed;
                    StatusToastActionButton.Visibility = Visibility.Collapsed;
                    _statusToastActionRestartsAria = false;
                    _isHiding = false;
                });
        }

        private void StatusMessageTimer_Tick(object? sender, object e)
        {
            _statusMessageTimer.Stop();
            AnimateInfoBarHide();
        }

        private void StatusToastInfoBar_Closed(InfoBar sender, InfoBarClosedEventArgs args)
        {
            _isHiding = false;
            StatusToastInfoBar.Visibility = Visibility.Collapsed;
            StatusToastActionButton.Visibility = Visibility.Collapsed;
            _statusToastActionRestartsAria = false;
        }

        private async void StatusToastActionButton_Click(object sender, RoutedEventArgs e)
        {
            if (_statusToastActionRestartsAria)
            {
                _statusToastActionRestartsAria = false;
                AnimateInfoBarHide();
                await StopAriaAsync(showMessage: false);
                await StartAriaAsync();
                return;
            }

            DataPackage package = new()
            {
                RequestedOperation = DataPackageOperation.Copy
            };
            package.SetText(StatusToastInfoBar.Message);
            Clipboard.SetContent(package);
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

        private static bool IsCopyableStatusMessage(InfoBarSeverity severity)
        {
            return severity is InfoBarSeverity.Warning or InfoBarSeverity.Error;
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
                _observedTaskDownloadCompletions[task.Gid] = IsTaskDownloadContentCompleted(task);
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
                    bool wasDownloadContentCompleted =
                        _observedTaskDownloadCompletions.TryGetValue(task.Gid, out bool observedDownloadContentCompleted) &&
                        observedDownloadContentCompleted;
                    bool isDownloadContentCompleted = IsTaskDownloadContentCompleted(task);
                    if (!wasDownloadContentCompleted && isDownloadContentCompleted)
                    {
                        if (IsAutoDownloadedMetadataTask(task))
                        {
                            ShowTaskAddedNotification(task);
                        }
                        else
                        {
                            ShowDownloadCompletedNotification(task);
                        }
                    }

                    if (!IsCompletedTaskStatus(previousStatus) &&
                        IsCompletedTask(task) &&
                        ShouldShowTaskCompletedAfterSeedingNotification(task))
                    {
                        ShowTaskCompletedNotification(task);
                    }
                    else if (!IsErrorTaskStatus(previousStatus) && IsErrorTaskStatus(task.Status))
                    {
                        ShowDownloadFailedNotification(task);
                    }
                }

                _observedTaskStatuses[task.Gid] = task.Status;
                _observedTaskDownloadCompletions[task.Gid] = IsTaskDownloadContentCompleted(task);
            }

            foreach (string staleGid in _observedTaskStatuses.Keys.Except(currentGids, StringComparer.OrdinalIgnoreCase).ToArray())
            {
                _observedTaskStatuses.Remove(staleGid);
                _observedTaskDownloadCompletions.Remove(staleGid);
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

        private void ShowTaskCompletedNotification(DownloadTask task)
        {
            if (_settingsPageViewModel.GeneralSettings.SystemNotificationsEnabled && _settingsPageViewModel.GeneralSettings.DownloadCompleteNotificationsEnabled)
            {
                _notifications.ShowTaskCompleted(task);
            }
        }

        private bool IsAutoDownloadedMetadataTask(DownloadTask task)
        {
            return task.IsMetadataTransfer &&
                _settingsPageViewModel.BitTorrentSettings.IsEnabled &&
                _settingsPageViewModel.BitTorrentSettings.AutoDownloadContent;
        }

        private bool ShouldShowTaskCompletedAfterSeedingNotification(DownloadTask task)
        {
            if (!task.IsPeerTransfer || task.IsMetadataTransfer)
            {
                return false;
            }

            BitTorrentSettings settings = _settingsPageViewModel.BitTorrentSettings;
            return settings.IsEnabled &&
                (settings.KeepSeeding || settings.SeedRatio > 0 || settings.SeedTimeMinutes > 0);
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
                UseSystemProxyCheckBox is null)
            {
                return;
            }

            string engineStatus = _aria2EngineHost.IsRunning
                ? Strings.Format("DebugAriaRunningStatus", _aria2EngineHost.ProcessId ?? 0)
                : Strings.Get("DebugAriaStoppedStatus");

            DebugEngineText.Text = engineStatus;
        }

        private static AppLogLevel ToLogLevel(InfoBarSeverity severity)
        {
            return severity switch
            {
                InfoBarSeverity.Error => AppLogLevel.Error,
                InfoBarSeverity.Warning => AppLogLevel.Warning,
                InfoBarSeverity.Success => AppLogLevel.Info,
                _ => AppLogLevel.Info
            };
        }
    }
}
