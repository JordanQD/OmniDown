using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using OmniDown.Models;
using OmniDown.Pages;
using OmniDown.Services.Engine;
using OmniDown.Services.Localization;
using OmniDown.Services.Logging;
using OmniDown.Services.Notifications;
using OmniDown.Services.Shell;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;

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

        private async void ManualEngineUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            await CheckEngineUpdateAsync(isManual: true);
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
            await _aria2EngineHost.ShutdownAsync(_aria2RpcClient);
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

            if (isSettings)
            {
                if (_appSettingsPage == null)
                {
                    _appSettingsPage = new AppSettingsPage();
                    _appSettingsPage.InitializeNavigation(_settingsPageViewModel, this);
                    _appSettingsPage.NavigationStateChanged += (_, _) => UpdateTitleBarBackButton();
                    HookSettingsPageEvents();
                }

                ContentFrame.Content = _appSettingsPage;
                _appSettingsPage.NavigateTo("Home");
                RootNavigation.SelectedItem = SettingsNavItem;
                ClearTitleSearchBox();
                UpdateSearchPlaceholder();
                UpdateStatusBar();
                UpdateTitleBarBackButton();
                return;
            }

            if (_downloadsPage == null)
            {
                _downloadsPage = new DownloadsPage(_downloadsViewModel);
                WireMainPageEvents();
                _downloadsPage.TasksListView.ItemsSource = _visibleTasks;
                _downloadsPage.NotificationHistoryListView.ItemsSource = _statusMessages;
            }

            ContentFrame.Content = _downloadsPage;

            UpdateTitleBarBackButton();
            UpdateSearchPlaceholder();
            UpdateDownloadFilterTokens(tag);
            UpdateDownloadsHeader(tag);
            UpdateStatsVisibility(tag);
            ApplyTaskFilter(tag);
            UpdateDashboard();
            UpdateFilterAppliedIndicator();
            ApplySettingsFilter();
        }

        private void TaskFilterButton_Click(object sender, RoutedEventArgs e)
        {
            _isTaskFilterPanelOpen = !_isTaskFilterPanelOpen;
            UpdateFilterPanelVisibility();
            UpdateFilterAppliedIndicator();
        }

        private void TaskStatusFilterTokenView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingDownloadFilterTokens || sender is not CommunityToolkit.Labs.WinUI.TokenView tokenView)
            {
                return;
            }

            if (tokenView.SelectedItem is CommunityToolkit.Labs.WinUI.TokenItem item &&
                item.Tag?.ToString() is string tag &&
                !string.IsNullOrWhiteSpace(tag))
            {
                RootNavigation.SelectedItem = TasksNavItem;
                NavigateTo(tag);
                UpdateFilterAppliedIndicator();
            }
        }

        private void TaskCategoryFilterTokenView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingDownloadFilterTokens || sender is not CommunityToolkit.Labs.WinUI.TokenView tokenView)
            {
                return;
            }

            if (tokenView.SelectedItem is CommunityToolkit.Labs.WinUI.TokenItem item &&
                item.Tag?.ToString() is string tag &&
                !string.IsNullOrWhiteSpace(tag))
            {
                _currentTaskCategoryFilter = tag;
                ApplyTaskFilter(_currentTaskFilter);
                UpdateDashboard();
                UpdateFilterAppliedIndicator();
            }
        }

        private void UpdateDownloadFilterTokens(string tag)
        {
            if (_downloadsPage is null)
            {
                return;
            }

            _isUpdatingDownloadFilterTokens = true;
            try
            {
                SelectTokenByTag(TaskStatusFilterTokenView, tag);
                SelectTokenByTag(TaskCategoryFilterTokenView, _currentTaskCategoryFilter);
                UpdateFilterPanelVisibility();
                UpdateFilterAppliedIndicator();
            }
            finally
            {
                _isUpdatingDownloadFilterTokens = false;
            }
        }

        private void UpdateFilterPanelVisibility()
        {
            Visibility visibility = _isTaskFilterPanelOpen ? Visibility.Visible : Visibility.Collapsed;
            TaskFilterPanel.Visibility = visibility;
            TaskCategoryFilterPanel.Visibility = visibility;
        }

        private void UpdateFilterAppliedIndicator()
        {
            bool isFilterApplied =
                _currentTaskFilter is not "Home" and not "Settings" ||
                !string.Equals(_currentTaskCategoryFilter, "All", StringComparison.OrdinalIgnoreCase);

            TaskFilterButton.IsChecked = isFilterApplied;
            ToolTipService.SetToolTip(TaskFilterButton, isFilterApplied ? "筛选（已应用）" : "筛选");
            AutomationProperties.SetName(TaskFilterButton, isFilterApplied ? "筛选，已应用" : "筛选");
        }

        private static void SelectTokenByTag(CommunityToolkit.Labs.WinUI.TokenView tokenView, string tag)
        {
            for (int index = 0; index < tokenView.Items.Count; index++)
            {
                if (tokenView.Items[index] is CommunityToolkit.Labs.WinUI.TokenItem tokenItem &&
                    string.Equals(tokenItem.Tag?.ToString(), tag, StringComparison.OrdinalIgnoreCase))
                {
                    tokenView.SelectedIndex = index;
                    return;
                }
            }

            tokenView.SelectedIndex = tokenView.Items.Count > 0 ? 0 : -1;
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

        private void AppTitleBar_BackRequested(TitleBar sender, object args)
        {
            if (_currentTaskFilter == "Settings" &&
                _appSettingsPage is not null &&
                _appSettingsPage.CanGoBack)
            {
                _appSettingsPage.GoBack();
                UpdateTitleBarBackButton();
            }
        }

        private void UpdateTitleBarBackButton()
        {
            AppTitleBar.IsBackButtonVisible =
                _currentTaskFilter == "Settings" &&
                _appSettingsPage is not null &&
                _appSettingsPage.CanGoBack;
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
            SetToolbarText(TaskFilterButton, "筛选");
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

            _ = ConfirmAutoShutdownOnStartupAsync();
        }

        private async Task ConfirmAutoShutdownOnStartupAsync()
        {
            if (!_settingsPageViewModel.GeneralSettings.AutoShutdownWhenComplete)
            {
                return;
            }

            // Small delay so the main window is visible before the dialog appears.
            await Task.Delay(500);

            ContentDialog dialog = new()
            {
                XamlRoot = Content.XamlRoot,
                Title = "下载完成自动关机已开启",
                Content = "上次使用时开启了「下载完成自动关机」，是否保持此设置？\n如果忘记关闭，下次下载完成时仍会自动关机。",
                PrimaryButtonText = "关闭自动关机",
                SecondaryButtonText = "保持开启",
                DefaultButton = ContentDialogButton.Primary
            };

            ContentDialogResult dialogResult = await dialog.ShowAsync();
            if (dialogResult == ContentDialogResult.Primary)
            {
                _isLoadingGeneralSettings = true;
                try
                {
                    _settingsPageViewModel.UpdateGeneralSettings(
                        _settingsPageViewModel.GeneralSettings with { AutoShutdownWhenComplete = false });
                    SaveGeneralSettings();
                    SettingsPage.ApplyGeneralSettings(_settingsPageViewModel.GeneralSettings, _autoStartService.IsEnabled());
                }
                finally
                {
                    _isLoadingGeneralSettings = false;
                }
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
            RemoveMinimumWindowSizeHook();
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
            await _aria2EngineHost.ShutdownAsync(_aria2RpcClient);
            _aria2RpcClient.Dispose();
            _aria2EngineHost.Dispose();
            AppLogger.Info("App", "MainWindow closed");
        }

        private async void MainWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            if (_isExitRequested || _hasTriggeredAutoShutdown)
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
