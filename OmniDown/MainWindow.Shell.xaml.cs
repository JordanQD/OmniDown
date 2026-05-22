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
using OmniDown.Services.Logging;
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
        private async void StartStopAriaButton_Click(object sender, RoutedEventArgs e)
        {
            if (_aria2EngineHost.IsRunning)
            {
                await StopAriaAsync();
                return;
            }

            await StartAriaAsync();
        }

        private async void RestartAriaButton_Click(object sender, RoutedEventArgs e)
        {
            await StopAriaAsync(showMessage: false);
            await StartAriaAsync();
        }

        private async Task StartAriaAsync()
        {
            AppLogger.Info("Aria2Command", "start requested");
            Aria2EngineStartResult result = await EnsureAria2StartedAsync();
            UpdateAriaStatus();
            ShowMessage(result.Message, result.Started ? InfoBarSeverity.Success : InfoBarSeverity.Error);
        }

        private async Task StopAriaAsync(bool showMessage = true)
        {
            AppLogger.Info("Aria2Command", "stop requested");
            _refreshTimer.Stop();
            await SaveAriaSessionIfRunningAsync();
            _aria2EngineHost.Stop();
            _runningAriaSettingsSignature = string.Empty;
            _runningAriaRpcPort = 0;
            _runningAriaRpcSecret = string.Empty;
            UpdateGlobalSpeeds(0, 0);
            _taskbarProgress.Clear();
            UpdateGlobalSpeedLimitText();
            UpdateAriaStatus();
            if (showMessage)
            {
                ShowMessage(Strings.Get("AriaStoppedMessage"), InfoBarSeverity.Informational);
            }
        }

        private void TrayIcon_ShowRequested(object? sender, EventArgs e)
        {
            DispatcherQueue.TryEnqueue(ShowFromTray);
        }

        public void ShowAndActivate()
        {
            WindowVisibilityService.ShowAndActivate(_windowHandle);
        }

        private void Notifications_NotificationInvoked(object? sender, TaskNotificationInvokedEventArgs args)
        {
            DispatcherQueue.TryEnqueue(() => HandleNotificationInvoked(args));
        }

        public void HandleNotificationActivation(TaskNotificationInvokedEventArgs args)
        {
            DispatcherQueue.TryEnqueue(() => HandleNotificationInvoked(args));
        }

        private void HandleNotificationInvoked(TaskNotificationInvokedEventArgs args)
        {
            switch (args.Action)
            {
                case SystemNotificationService.ActionTaskAdded:
                case SystemNotificationService.ActionDownloadFailed:
                    ShowAndActivate();
                    NavigateToHome();
                    break;
                case SystemNotificationService.ActionDownloadCompleted:
                case SystemNotificationService.ActionTaskCompleted:
                    ShowAndActivate();
                    HandleDownloadCompletedNotificationInvoked(args);
                    break;
                case SystemNotificationService.ActionOpenDownloadedFile:
                case SystemNotificationService.ActionOpenDownloadedFolder:
                    HandleDownloadCompletedNotificationInvoked(args);
                    break;
            }
        }

        private void HandleDownloadCompletedNotificationInvoked(TaskNotificationInvokedEventArgs args)
        {
            switch (args.Action)
            {
                case SystemNotificationService.ActionOpenDownloadedFile:
                    OpenNotificationFile(args);
                    break;
                case SystemNotificationService.ActionOpenDownloadedFolder:
                    OpenNotificationFolder(args);
                    break;
                default:
                    NavigateToHome();
                    break;
            }
        }

        private void NavigateToHome()
        {
            RootNavigation.SelectedItem = TasksNavItem;
            NavigateTo("Home");
        }

        private void OpenNotificationFile(TaskNotificationInvokedEventArgs args)
        {
            string? filePath = ResolveNotificationFilePath(args);
            if (!string.IsNullOrWhiteSpace(filePath) && (File.Exists(filePath) || Directory.Exists(filePath)))
            {
                OpenShellTarget(filePath);
                return;
            }

            NavigateToHome();
            ShowAndActivate();
            ShowMessage(Strings.Get("TaskFileNotFoundMessage"), InfoBarSeverity.Warning);
        }

        private void OpenNotificationFolder(TaskNotificationInvokedEventArgs args)
        {
            string? filePath = ResolveNotificationFilePath(args);
            string? folderPath = ResolveNotificationFolderPath(args, filePath);
            string? resolvedFolderPath = !string.IsNullOrWhiteSpace(folderPath)
                ? folderPath
                : string.IsNullOrWhiteSpace(filePath)
                    ? null
                    : Path.GetDirectoryName(filePath);

            if (!string.IsNullOrWhiteSpace(resolvedFolderPath) && Directory.Exists(resolvedFolderPath))
            {
                OpenShellTarget(resolvedFolderPath);
                return;
            }

            NavigateToHome();
            ShowAndActivate();
            ShowMessage(Strings.Get("TaskFolderNotFoundMessage"), InfoBarSeverity.Warning);
        }

        private string? ResolveNotificationFilePath(TaskNotificationInvokedEventArgs args)
        {
            if (!string.IsNullOrWhiteSpace(args.Gid) &&
                Tasks.FirstOrDefault(task => task.Gid.Equals(args.Gid, StringComparison.OrdinalIgnoreCase)) is DownloadTask task)
            {
                string taskFilePath = ResolveTaskFilePath(task);
                if (!string.IsNullOrWhiteSpace(taskFilePath))
                {
                    return taskFilePath;
                }
            }

            return args.FilePath;
        }

        private string? ResolveNotificationFolderPath(TaskNotificationInvokedEventArgs args, string? resolvedFilePath)
        {
            if (!string.IsNullOrWhiteSpace(args.Gid) &&
                Tasks.FirstOrDefault(task => task.Gid.Equals(args.Gid, StringComparison.OrdinalIgnoreCase)) is DownloadTask task)
            {
                string taskFolderPath = ResolveTaskFolderPath(task);
                if (!string.IsNullOrWhiteSpace(taskFolderPath))
                {
                    return taskFolderPath;
                }
            }

            if (!string.IsNullOrWhiteSpace(resolvedFilePath))
            {
                if (Directory.Exists(resolvedFilePath))
                {
                    return resolvedFilePath;
                }

                string? fileDirectory = Path.GetDirectoryName(resolvedFilePath);
                if (!string.IsNullOrWhiteSpace(fileDirectory))
                {
                    return fileDirectory;
                }
            }

            return args.FolderPath;
        }

        private void TrayIcon_ExitRequested(object? sender, EventArgs e)
        {
            DispatcherQueue.TryEnqueue(RequestExit);
        }

        private void UpdateTrayIconLabels()
        {
            _trayIcon?.UpdateLabels(
                Strings.Get("TrayTooltipText"),
                Strings.Get("TrayShowMenuItemText"),
                Strings.Get("TrayExitMenuItemText"));
        }

        private void RootNavigation_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            string tag = GetNavigationTag(args);
            NavigateTo(tag);
        }

        private void RootNavigation_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            string tag = args.InvokedItemContainer?.Tag?.ToString() ?? string.Empty;
            NavigateTo(tag);
        }

        private void SettingsNavItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            SettingsPage.NavigateTo("Home");
            NavigateTo("Settings");
            e.Handled = true;
        }

        private void NavigateTo(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                return;
            }

            bool wasSettings = _currentTaskFilter == "Settings";
            bool isSettings = tag == "Settings";
            if (wasSettings && !isSettings)
            {
                DismissSettingsTeachingTips();
            }

            _currentTaskFilter = tag;
            TasksHeaderPanel.Visibility = isSettings ? Visibility.Collapsed : Visibility.Visible;
            SettingsPage.Visibility = isSettings ? Visibility.Visible : Visibility.Collapsed;
            TasksPage.Visibility = isSettings ? Visibility.Collapsed : Visibility.Visible;
            if (isSettings)
            {
                RootNavigation.SelectedItem = SettingsNavItem;
                TaskDetailsPane.Visibility = Visibility.Collapsed;
                ClearTitleSearchBox();
                UpdateSearchPlaceholder();
                UpdateStatusBar();
                ShowSettingsPage();
                UpdateTitleBarBackButton();
                return;
            }

            UpdateTitleBarBackButton();
            UpdateTaskDetailsPaneVisibility();
            UpdateSearchPlaceholder();
            UpdateDownloadsHeader(tag);
            UpdateStatsVisibility(tag);
            ApplyTaskFilter(tag);
            UpdateDashboard();
            ApplySettingsFilter();
        }

        private static string GetNavigationTag(NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItemContainer?.Tag?.ToString() is string containerTag &&
                !string.IsNullOrWhiteSpace(containerTag))
            {
                return containerTag;
            }

            if (args.SelectedItem is NavigationViewItem item &&
                item.Tag?.ToString() is string itemTag &&
                !string.IsNullOrWhiteSpace(itemTag))
            {
                return itemTag;
            }

            return string.Empty;
        }

        private void SettingsPage_SectionNavigationRequested(object? sender, string tag)
        {
            if (SettingsPage.Visibility == Visibility.Visible)
            {
                DismissSettingsTeachingTips();
            }

            if (tag == "Home")
            {
                ApplySettingsFilter();
                ResetSettingsSectionFocus();
                UpdateTitleBarBackButton();
                return;
            }

            SettingsPage.NavigateTo(tag);
            ApplySettingsFilter();
            ResetSettingsSectionViewport();

            if (tag == "Advanced")
            {
                _isLoadingAdvancedSettings = true;
                try
                {
                    RefreshProtocolDefaultToggles();
                }
                finally
                {
                    _isLoadingAdvancedSettings = false;
                }
            }

            UpdateTitleBarBackButton();
        }

        private void ShowSettingsPage()
        {
            try
            {
                string tag = GetSelectedSettingsSectionTag();
                if (string.IsNullOrWhiteSpace(tag) || tag == "Home")
                {
                    SettingsPage.NavigateTo("Home");
                }
                else
                {
                    SettingsPage.NavigateTo(tag);
                }
                ApplySettingsFilter();
                ResetSettingsSectionFocus();
                UpdateTitleBarBackButton();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Show settings page failed: {ex}");
                SettingsPage.NavigateTo("Home");
                UpdateTitleBarBackButton();
            }
        }

        private void ShowSettingsSection(string tag)
        {
            SettingsPage.NavigateTo(tag);
            UpdateTitleBarBackButton();
        }

        private void ResetSettingsSectionViewport()
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                SettingsContentScrollViewer?.ChangeView(null, 0, null, disableAnimation: true);
                ResetSettingsSectionFocus();
            });
        }

        private void ResetSettingsSectionFocus()
        {
            SettingsContentScrollViewer?.Focus(FocusState.Programmatic);
        }

        private void InitializeAboutSection()
        {
            AboutVersionText.Text = GetAppVersionText();
            AboutCloneCommandText.Text = CloneCommand;
        }

        private static string GetAppVersionText()
        {
            try
            {
                Windows.ApplicationModel.PackageVersion version = Windows.ApplicationModel.Package.Current.Id.Version;
                return $"{version.Major}.{version.Minor}.{version.Build}";
            }
            catch
            {
                Version? assemblyVersion = typeof(MainWindow).Assembly.GetName().Version;
                return assemblyVersion is null
                    ? "1.0.0"
                    : $"{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}";
            }
        }

        private void CopyCloneCommandButton_Click(object sender, RoutedEventArgs e)
        {
            DataPackage package = new()
            {
                RequestedOperation = DataPackageOperation.Copy
            };
            package.SetText(CloneCommand);
            Clipboard.SetContent(package);
            ShowMessage(Strings.Get("CloneCommandCopiedMessage"), InfoBarSeverity.Success);
        }

        private async void OpenAboutLinkButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element
                || element.Tag?.ToString() is not string url
                || !Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
            {
                return;
            }

            await Launcher.LaunchUriAsync(uri);
        }

        private void AppTitleBar_PaneToggleRequested(TitleBar sender, object args)
        {
            RootNavigation.IsPaneOpen = !RootNavigation.IsPaneOpen;
        }

        private void AppTitleBar_BackRequested(TitleBar sender, object args)
        {
            if (_currentTaskFilter == "Settings" &&
                !string.IsNullOrWhiteSpace(SettingsPage.CurrentSection))
            {
                SettingsPage.NavigateBackToHome();
                UpdateTitleBarBackButton();
            }
        }

        private void UpdateTitleBarBackButton()
        {
            AppTitleBar.IsBackButtonVisible =
                _currentTaskFilter == "Settings" &&
                !string.IsNullOrWhiteSpace(SettingsPage.CurrentSection);
        }

        private void RootNavigation_DisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args)
        {
            AppTitleBar.IsPaneToggleButtonVisible = sender.PaneDisplayMode != NavigationViewPaneDisplayMode.Top;
        }

        private void ApplyToolbarTooltips()
        {
            SetToolbarText(NewDownloadButton, Strings.Get("NewDownloadButton.Label"));
            SetToolbarText(ResumeTasksButton, Strings.Get("ResumeTasksButton.Label"));
            SetToolbarText(PauseTasksButton, Strings.Get("PauseTasksButton.Label"));
            SetToolbarText(RecoverTasksButton, Strings.Get("RecoverTasksButton.Label"));
            SetToolbarText(OpenSelectedTaskFileButton, Strings.Get("OpenSelectedTaskFileButton.Label"));
            SetToolbarText(OpenSelectedTaskFolderButton, Strings.Get("OpenSelectedTaskFolderButton.Label"));
            SetToolbarText(CopySelectedTaskLinksButton, Strings.Get("CopySelectedTaskLinksButton.Label"));
            SetToolbarText(DeleteTasksButton, Strings.Get("DeleteTasksButton.Label"));
            SetToolbarText(ClearCompletedTasksButton, Strings.Get("ClearCompletedTasksButton.Label"));
            SetToolbarText(SortTasksButton, Strings.Get("SortTasksButton.Label"));
            SetToolbarText(TaskDetailsButton, Strings.Get("TaskDetailsButton.Label"));
        }

        private static void SetToolbarText(FrameworkElement element, string text, string? shortcut = null)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            string tooltip = string.IsNullOrWhiteSpace(shortcut)
                ? text
                : Strings.Format("ToolbarShortcutTooltipFormat", text, shortcut);
            ToolTipService.SetToolTip(element, tooltip);
            AutomationProperties.SetName(element, text);
        }

        private async void RootGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (_hasStartedInitialLoad)
            {
                return;
            }

            _hasStartedInitialLoad = true;
            EnsureTrayIconInitialized();
            SetTaskListLoading(true);
            await WaitForNextRenderAsync();
            await StartAriaOnLaunchAsync();
        }

        private void EnsureTrayIconInitialized()
        {
            if (_trayIcon is not null)
            {
                return;
            }

            _trayIcon = new TrayIconService(_windowHandle, ResolveAssetPath("Assets", "OmniDown.ico"));
            UpdateTrayIconLabels();
            _trayIcon.ShowRequested += TrayIcon_ShowRequested;
            _trayIcon.ExitRequested += TrayIcon_ExitRequested;
        }

        private string GetSelectedSettingsSectionTag()
        {
            return SettingsPage?.CurrentSection ?? string.Empty;
        }

        private void ClearTitleSearchBox()
        {
            if (TitleSearchBox is not null && !string.IsNullOrEmpty(TitleSearchBox.Text))
            {
                TitleSearchBox.Text = string.Empty;
            }
        }

        private async System.Threading.Tasks.Task StartAriaOnLaunchAsync()
        {
            try
            {
                Aria2EngineStartResult result = await EnsureAria2StartedAsync();
                UpdateAriaStatus();
                if (!result.Started)
                {
                    ApplyTaskFilter(_currentTaskFilter);
                    UpdateDashboard();
                    ShowMessage(result.Message, InfoBarSeverity.Warning);
                }
                else
                {
                    await ResumeDownloadsOnLaunchAsync();
                    ApplyTaskFilter(_currentTaskFilter);
                    UpdateDashboard();
                }
            }
            finally
            {
                SetTaskListLoading(false);
            }
        }

        private async Task ResumeDownloadsOnLaunchAsync()
        {
            if (!_settingsPageViewModel.GeneralSettings.ResumeDownloadsOnLaunch || !_aria2EngineHost.IsRunning)
            {
                return;
            }

            DownloadTask[] pausedTasks = Tasks
                .Where(task => task.IsAria2SessionAttached && IsPausedTask(task) && IsDownloadingTask(task))
                .ToArray();
            if (pausedTasks.Length == 0)
            {
                return;
            }

            HashSet<string> tasksAwaitingProgress = pausedTasks
                .Where(task => task.CompletedLength > 0)
                .Select(task => task.Gid)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            try
            {
                await _downloadCoordinator.ResumeAsync(pausedTasks);
                await RefreshDownloadsAsync();
                await WaitForResumedTaskProgressAsync(tasksAwaitingProgress);
            }
            catch (Exception ex)
            {
                ShowMessage($"恢复下载任务失败：{ex.Message}", InfoBarSeverity.Warning);
            }
        }

        private async Task WaitForResumedTaskProgressAsync(HashSet<string> taskGids)
        {
            if (taskGids.Count == 0)
            {
                return;
            }

            DateTimeOffset deadline = DateTimeOffset.Now.AddSeconds(8);
            while (DateTimeOffset.Now < deadline)
            {
                if (taskGids.All(gid =>
                    Tasks.FirstOrDefault(task => string.Equals(task.Gid, gid, StringComparison.OrdinalIgnoreCase)) is not { } task ||
                    task.CompletedLength > 0 ||
                    IsCompletedTask(task) ||
                    IsIssueTask(task)))
                {
                    return;
                }

                await Task.Delay(250);
                await RefreshDownloadsAsync();
            }
        }

        private static Task WaitForNextRenderAsync()
        {
            TaskCompletionSource completionSource = new();
            EventHandler<object>? renderingHandler = null;
            renderingHandler = (sender, args) =>
            {
                CompositionTarget.Rendering -= renderingHandler;
                completionSource.TrySetResult();
            };

            CompositionTarget.Rendering += renderingHandler;
            return completionSource.Task;
        }

        private async void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            AppLogger.Info("App", "MainWindow closing");
            _refreshTimer.Stop();
            Clipboard.ContentChanged -= Clipboard_ContentChanged;
            ReleaseSystemSleepOverride();
            SaveWindowPlacementSettings();
            _taskbarProgress.Clear();
            await PrepareDownloadsForShutdownAsync();
            SaveCloseBehaviorSettings();
            SaveGeneralSettings();
            await SaveAriaSessionIfRunningAsync();
            _notifications.Unregister();
            _notifications.NotificationInvoked -= Notifications_NotificationInvoked;
            _trayIcon?.Dispose();
            _browserExtensionApiServer.Dispose();
            _aria2RpcClient.Dispose();
            _aria2EngineHost.Dispose();
            AppLogger.Info("App", "MainWindow closed");
        }

        private async void MainWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            if (_isExitRequested)
            {
                return;
            }

            args.Cancel = true;

            bool? minimizeToTray = _settingsPageViewModel.CloseBehaviorSettings.MinimizeToTrayOnClose;
            if (minimizeToTray is null)
            {
                minimizeToTray = await AskCloseBehaviorAsync();
                if (minimizeToTray is null)
                {
                    return;
                }

                _settingsPageViewModel.UpdateCloseBehavior(minimizeToTray.Value);
                SettingsPage.ApplyCloseBehaviorSettings(_settingsPageViewModel.CloseBehaviorSettings);
                SaveCloseBehaviorSettings();
            }

            if (minimizeToTray.Value)
            {
                HideToTray();
            }
            else
            {
                RequestExit();
            }
        }
    }
}
