using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using OmniDown.Models;
using OmniDown.Services.Downloads;
using OmniDown.Services.Engine;
using OmniDown.Services.Localization;
using OmniDown.Services.Notifications;
using OmniDown.Services.Rpc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using WinRT.Interop;

namespace OmniDown
{
    public sealed partial class MainWindow : Window
    {
        private readonly Aria2EngineHost _aria2EngineHost = new();
        private readonly Aria2RpcClient _aria2RpcClient = new();
        private readonly DownloadCoordinator _downloadCoordinator;
        private readonly SystemNotificationService _notifications;
        private readonly DispatcherTimer _refreshTimer = new();
        private readonly DispatcherTimer _statusMessageTimer = new();
        private readonly string _rpcSecret = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        private readonly ObservableCollection<DownloadTask> _visibleTasks = new();
        private readonly ObservableCollection<AppStatusMessage> _statusMessages = new();
        private string _currentTaskFilter = "Home";
        private TaskSortColumn _sortColumn = TaskSortColumn.CreatedAt;
        private bool _sortAscending = false;
        private bool _isRefreshing;
        private bool _isUpdatingSelectAllCheckBox;
        private bool _isUpdatingTaskSelection;
        private bool _hasStartedInitialLoad;
        private bool _isDownloadSpeedLimitEnabled;
        private bool _isUploadSpeedLimitEnabled;
        private long _downloadLimitBytesPerSecond;
        private long _uploadLimitBytesPerSecond;
        private readonly Dictionary<string, string> _observedTaskStatuses = new(StringComparer.OrdinalIgnoreCase);
        private readonly string _speedLimitSettingsPath = Path.Combine(GetAppDataDirectory(), "speed-limits.json");

        public ObservableCollection<DownloadTask> Tasks { get; } = new();

        public MainWindow()
        {
            InitializeComponent();
            TasksListView.ItemsSource = _visibleTasks;
            NotificationHistoryListView.ItemsSource = _statusMessages;
            SetWindowIcon();
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
            _downloadCoordinator = new DownloadCoordinator(_aria2RpcClient, Tasks);
            _notifications = ((App)Application.Current).Notifications;
            RecordObservedTaskStatuses();
            Closed += MainWindow_Closed;
            _refreshTimer.Interval = TimeSpan.FromSeconds(2);
            _refreshTimer.Tick += RefreshTimer_Tick;
            _statusMessageTimer.Interval = TimeSpan.FromSeconds(3);
            _statusMessageTimer.Tick += StatusMessageTimer_Tick;

            DownloadDirectoryTextBox.Text = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads");

            LoadSpeedLimitSettings();
            UpdateSearchPlaceholder();
            UpdateDownloadsHeader("Home");
            UpdateStatsVisibility("Home");
            ApplyTaskFilter("Home");
            ApplySettingsFilter();
            SetTaskListLoading(true);
            UpdateDashboard();
            UpdateGlobalSpeeds(0, 0);
            UpdateGlobalSpeedLimitText();
            UpdateAriaStatus();
            UpdateDebugStatus();
        }

        private async void NewDownloadButton_Click(object sender, RoutedEventArgs e)
        {
            TextBox uriTextBox = new()
            {
                Header = "Download URL",
                PlaceholderText = "https://example.com/file.zip"
            };
            uriTextBox.Header = Strings.Get("NewDownloadUrlHeader");

            TextBox fileNameTextBox = new()
            {
                PlaceholderText = "Leave empty to infer from the URL"
            };
            fileNameTextBox.Header = Strings.Get("NewDownloadTaskNameHeader");
            fileNameTextBox.PlaceholderText = Strings.Get("NewDownloadTaskNamePlaceholder");

            TextBox directoryTextBox = new()
            {
                Text = DownloadDirectoryTextBox.Text
            };
            directoryTextBox.Header = Strings.Get("NewDownloadDirectoryHeader");

            NumberBox splitCountNumberBox = new()
            {
                Header = Strings.Get("NewDownloadSplitCountHeader"),
                Value = 16,
                Minimum = 1,
                Maximum = 128,
                SmallChange = 1,
                LargeChange = 8,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact
            };

            StackPanel content = new()
            {
                Spacing = 12,
                Children =
                {
                    uriTextBox,
                    fileNameTextBox,
                    directoryTextBox,
                    splitCountNumberBox
                }
            };

            ContentDialog dialog = new()
            {
                XamlRoot = Content.XamlRoot,
                Title = Strings.Get("NewDownloadDialogTitle"),
                Content = content,
                PrimaryButtonText = Strings.Get("AddButtonText"),
                CloseButtonText = Strings.Get("CancelButtonText"),
                DefaultButton = ContentDialogButton.Primary
            };

            ContentDialogResult result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
            {
                return;
            }

            string sourceUri = uriTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(sourceUri))
            {
                ShowMessage(Strings.Get("DownloadUrlRequiredMessage"), InfoBarSeverity.Warning);
                return;
            }

            Aria2EngineStartResult startResult = await EnsureAria2StartedAsync();
            if (!startResult.Started)
            {
                ShowMessage(startResult.Message, InfoBarSeverity.Error);
                return;
            }

            string saveDirectory = string.IsNullOrWhiteSpace(directoryTextBox.Text)
                ? DownloadDirectoryTextBox.Text
                : directoryTextBox.Text.Trim();
            int splitCount = GetDownloadSplitCount(splitCountNumberBox);

            try
            {
                DownloadTask task = await _downloadCoordinator.AddDownloadAsync(sourceUri, fileNameTextBox.Text, saveDirectory, splitCount);
                _observedTaskStatuses[task.Gid] = task.Status;
                ShowMessage(Strings.Get("TaskAddedMessage"), InfoBarSeverity.Success);
                _notifications.ShowTaskAdded(task);
                await RefreshDownloadsAsync();
            }
            catch (Exception ex)
            {
                ShowMessage(Strings.Format("AddTaskFailedMessage", ex.Message), InfoBarSeverity.Error);
            }

            UpdateDashboard();
        }

        private static int GetDownloadSplitCount(NumberBox numberBox)
        {
            if (double.IsNaN(numberBox.Value))
            {
                return 1;
            }

            return Math.Clamp((int)Math.Round(numberBox.Value), 1, 128);
        }

        private async void StartAriaButton_Click(object sender, RoutedEventArgs e)
        {
            Aria2EngineStartResult result = await EnsureAria2StartedAsync();
            UpdateAriaStatus();
            ShowMessage(result.Message, result.Started ? InfoBarSeverity.Success : InfoBarSeverity.Error);
        }

        private async void StopAriaButton_Click(object sender, RoutedEventArgs e)
        {
            _refreshTimer.Stop();
            await SaveAriaSessionIfRunningAsync();
            _aria2EngineHost.Stop();
            UpdateGlobalSpeeds(0, 0);
            UpdateGlobalSpeedLimitText();
            UpdateAriaStatus();
            ShowMessage(Strings.Get("AriaStoppedMessage"), InfoBarSeverity.Informational);
        }

        private void RootNavigation_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is not NavigationViewItem item)
            {
                return;
            }

            string tag = item.Tag?.ToString() ?? "Home";
            _currentTaskFilter = tag;
            SettingsPage.Visibility = tag == "Settings" ? Visibility.Visible : Visibility.Collapsed;
            TasksPage.Visibility = tag == "Settings" ? Visibility.Collapsed : Visibility.Visible;

            UpdateSearchPlaceholder();
            UpdateDownloadsHeader(tag);
            UpdateStatsVisibility(tag);
            ApplyTaskFilter(tag);
            ApplySettingsFilter();
        }

        private void AppTitleBar_PaneToggleRequested(TitleBar sender, object args)
        {
            RootNavigation.IsPaneOpen = !RootNavigation.IsPaneOpen;
        }

        private async void RootGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (_hasStartedInitialLoad)
            {
                return;
            }

            _hasStartedInitialLoad = true;
            SetTaskListLoading(true);
            await WaitForNextRenderAsync();
            await StartAriaOnLaunchAsync();
        }

        private async System.Threading.Tasks.Task StartAriaOnLaunchAsync()
        {
            try
            {
                Aria2EngineStartResult result = await EnsureAria2StartedAsync();
                UpdateAriaStatus();
                if (!result.Started)
                {
                    ShowMessage(result.Message, InfoBarSeverity.Warning);
                }
            }
            finally
            {
                SetTaskListLoading(false);
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
            _refreshTimer.Stop();
            SaveSpeedLimitSettings();
            await SaveAriaSessionIfRunningAsync();
            _notifications.Unregister();
            _aria2RpcClient.Dispose();
            _aria2EngineHost.Dispose();
        }

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
        }

        private async void RefreshTimer_Tick(object? sender, object e)
        {
            await RefreshDownloadsAsync();
        }

        private async System.Threading.Tasks.Task<Aria2EngineStartResult> EnsureAria2StartedAsync()
        {
            int rpcPort = double.IsNaN(RpcPortNumberBox.Value) ? 6800 : (int)RpcPortNumberBox.Value;
            _aria2RpcClient.Configure(rpcPort, _rpcSecret);

            Aria2EngineStartResult result = await _aria2EngineHost.StartAsync(new Aria2EngineOptions(
                string.IsNullOrWhiteSpace(AriaPathTextBox.Text) ? null : AriaPathTextBox.Text.Trim(),
                rpcPort,
                DownloadDirectoryTextBox.Text.Trim(),
                _rpcSecret,
                UseSystemProxyCheckBox.IsChecked == true));

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

        private void UpdateDashboard()
        {
            TotalTasksText.Text = Tasks.Count.ToString();
            ActiveTasksText.Text = Tasks.Count(IsDownloadingTask).ToString();
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
            string status = _aria2EngineHost.IsRunning
                ? Strings.Format("AriaRunningStatus", _aria2EngineHost.ProcessId ?? 0)
                : Strings.Get("AriaStoppedStatus");

            SettingsAriaStatusText.Text = status;
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
                        _notifications.ShowDownloadCompleted(task);
                    }
                    else if (!IsErrorTaskStatus(previousStatus) && IsErrorTaskStatus(task.Status))
                    {
                        _notifications.ShowDownloadFailed(task);
                    }
                }

                _observedTaskStatuses[task.Gid] = task.Status;
            }

            foreach (string staleGid in _observedTaskStatuses.Keys.Except(currentGids, StringComparer.OrdinalIgnoreCase).ToArray())
            {
                _observedTaskStatuses.Remove(staleGid);
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

        private async void ResumeTasksButton_Click(object sender, RoutedEventArgs e)
        {
            await RunSelectedTaskOperationAsync(
                tasks => _downloadCoordinator.ResumeAsync(tasks),
                Strings.Get("TasksResumedMessage"));
        }

        private async void PauseTasksButton_Click(object sender, RoutedEventArgs e)
        {
            await RunSelectedTaskOperationAsync(
                tasks => _downloadCoordinator.PauseAsync(tasks),
                Strings.Get("TasksPausedMessage"));
        }

        private async void RecoverTasksButton_Click(object sender, RoutedEventArgs e)
        {
            await RunSelectedTaskOperationAsync(
                tasks => _downloadCoordinator.RecoverAsync(tasks, GetDefaultRecoverySplitCount()),
                Strings.Get("TasksRecoveredMessage"));
        }

        private async void DeleteTasksButton_Click(object sender, RoutedEventArgs e)
        {
            List<DownloadTask> selectedTasks = GetSelectedTasks();
            if (selectedTasks.Count == 0)
            {
                return;
            }

            bool? deleteFiles = await ConfirmDeleteAsync();
            if (deleteFiles is null)
            {
                return;
            }

            await RunSelectedTaskOperationAsync(
                tasks => _downloadCoordinator.DeleteAsync(tasks, deleteFiles.Value),
                Strings.Get("TasksDeletedMessage"));
        }

        private async void ClearCompletedTasksButton_Click(object sender, RoutedEventArgs e)
        {
            bool? deleteFiles = await ConfirmClearRecordsAsync();
            if (deleteFiles is null)
            {
                return;
            }

            int clearedCount = await _downloadCoordinator.ClearCompletedAsync(deleteFiles.Value);
            ApplyTaskFilter(_currentTaskFilter);
            UpdateDashboard();
            UpdateSelectionCommands();

            string message = clearedCount == 0
                ? Strings.Get("ClearCompletedTasksEmptyMessage")
                : Strings.Get("ClearCompletedTasksMessage");
            ShowMessage(message, InfoBarSeverity.Success);
        }

        private async void TaskTogglePauseResumeButton_Click(object sender, RoutedEventArgs e)
        {
            if (GetTaskFromSender(sender) is not DownloadTask task)
            {
                return;
            }

            if (IsErrorTaskStatus(task.Status))
            {
                await RunTaskOperationAsync(
                    task,
                    tasks => _downloadCoordinator.RecoverAsync(tasks, GetDefaultRecoverySplitCount()),
                    Strings.Get("TasksRecoveredMessage"));
            }
            else if (task.IsPaused)
            {
                await RunTaskOperationAsync(
                    task,
                    tasks => _downloadCoordinator.ResumeAsync(tasks),
                    Strings.Get("TasksResumedMessage"));
            }
            else
            {
                await RunTaskOperationAsync(
                    task,
                    tasks => _downloadCoordinator.PauseAsync(tasks),
                    Strings.Get("TasksPausedMessage"));
            }
        }

        private void TaskOpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (GetTaskFromSender(sender) is not DownloadTask task)
            {
                return;
            }

            string filePath = ResolveTaskFilePath(task);
            if (!File.Exists(filePath))
            {
                ShowMessage(Strings.Get("TaskFileNotFoundMessage"), InfoBarSeverity.Warning);
                return;
            }

            OpenShellTarget(filePath);
        }

        private void TaskOpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (GetTaskFromSender(sender) is not DownloadTask task)
            {
                return;
            }

            string folderPath = !string.IsNullOrWhiteSpace(task.SaveDirectory) && Directory.Exists(task.SaveDirectory)
                ? task.SaveDirectory
                : Path.GetDirectoryName(ResolveTaskFilePath(task)) ?? string.Empty;

            if (!Directory.Exists(folderPath))
            {
                ShowMessage(Strings.Get("TaskFolderNotFoundMessage"), InfoBarSeverity.Warning);
                return;
            }

            OpenShellTarget(folderPath);
        }

        private void TaskCopyLinkButton_Click(object sender, RoutedEventArgs e)
        {
            if (GetTaskFromSender(sender) is not DownloadTask task ||
                string.IsNullOrWhiteSpace(task.SourceUri))
            {
                ShowMessage(Strings.Get("TaskLinkNotFoundMessage"), InfoBarSeverity.Warning);
                return;
            }

            DataPackage package = new();
            package.SetText(task.SourceUri);
            Clipboard.SetContent(package);
            ShowMessage(Strings.Get("TaskLinkCopiedMessage"), InfoBarSeverity.Success);
        }

        private async void TaskDeleteEntryButton_Click(object sender, RoutedEventArgs e)
        {
            if (GetTaskFromSender(sender) is not DownloadTask task)
            {
                return;
            }

            bool? deleteFiles = await ConfirmDeleteAsync();
            if (deleteFiles is null)
            {
                return;
            }

            await RunTaskOperationAsync(
                task,
                tasks => _downloadCoordinator.DeleteAsync(tasks, deleteFiles.Value),
                Strings.Get("TaskEntryDeletedMessage"));
        }

        private void TaskCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingTaskSelection ||
                sender is not CheckBox checkBox ||
                checkBox.DataContext is not DownloadTask task)
            {
                return;
            }

            _isUpdatingTaskSelection = true;
            try
            {
                if (checkBox.IsChecked == true)
                {
                    if (!TasksListView.SelectedItems.Contains(task))
                    {
                        TasksListView.SelectedItems.Add(task);
                    }
                }
                else
                {
                    TasksListView.SelectedItems.Remove(task);
                }
            }
            finally
            {
                _isUpdatingTaskSelection = false;
            }

            UpdateTaskSelectionGlyphVisibility(task);
            UpdateSelectionCommands();
        }

        private void TasksListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingTaskSelection)
            {
                return;
            }

            _isUpdatingTaskSelection = true;
            try
            {
                foreach (DownloadTask task in e.AddedItems.OfType<DownloadTask>())
                {
                    task.IsSelected = true;
                    UpdateTaskSelectionGlyphVisibility(task);
                }

                foreach (DownloadTask task in e.RemovedItems.OfType<DownloadTask>())
                {
                    task.IsSelected = false;
                    UpdateTaskSelectionGlyphVisibility(task);
                }
            }
            finally
            {
                _isUpdatingTaskSelection = false;
            }

            UpdateSelectionCommands();
        }

        private void SelectAllTasksCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingSelectAllCheckBox)
            {
                return;
            }

            SetVisibleTasksSelected(true);
        }

        private void SelectAllTasksCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingSelectAllCheckBox)
            {
                return;
            }

            SetVisibleTasksSelected(false);
        }

        private void SelectAllTasksCheckBox_Indeterminate(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingSelectAllCheckBox)
            {
                return;
            }

            SetVisibleTasksSelected(false);
        }

        private void SetVisibleTasksSelected(bool isSelected)
        {
            _isUpdatingTaskSelection = true;
            try
            {
                TasksListView.SelectedItems.Clear();
                foreach (DownloadTask task in _visibleTasks)
                {
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

            UpdateSelectionCommands();
        }

        private void TaskIconSelectionBox_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                UpdateTaskSelectionGlyphVisibility(element, true);
            }
        }

        private void TaskIconSelectionBox_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                UpdateTaskSelectionGlyphVisibility(element, false);
            }
        }

        private void TaskItem_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is UserControl userControl &&
                userControl.DataContext is DownloadTask task)
            {
                VisualStateManager.GoToState(userControl, task.IsSelected ? "ShowCheckbox" : "HideCheckbox", false);
            }
        }

        private void SortColumnMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not ToggleMenuFlyoutItem menuItem ||
                !Enum.TryParse(menuItem.Tag?.ToString(), out TaskSortColumn selectedColumn))
            {
                return;
            }

            _sortColumn = selectedColumn;
            ApplyTaskFilter(_currentTaskFilter);
        }

        private void SortDirectionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not ToggleMenuFlyoutItem menuItem)
            {
                return;
            }

            _sortAscending = string.Equals(menuItem.Tag?.ToString(), "Ascending", StringComparison.Ordinal);
            ApplyTaskFilter(_currentTaskFilter);
        }

        private void SortMenuFlyout_Opening(object sender, object e)
        {
            SortByCreatedAtMenuItem.IsChecked = _sortColumn == TaskSortColumn.CreatedAt;
            SortByNameMenuItem.IsChecked = _sortColumn == TaskSortColumn.Name;
            SortBySizeMenuItem.IsChecked = _sortColumn == TaskSortColumn.Size;
            SortAscendingMenuItem.IsChecked = _sortAscending;
            SortDescendingMenuItem.IsChecked = !_sortAscending;
        }

        private void UseSystemProxyCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            UpdateDebugStatus();
        }

        private async void ApplySpeedLimitButton_Click(object sender, RoutedEventArgs e)
        {
            _isDownloadSpeedLimitEnabled = DownloadLimitToggleSwitch.IsOn;
            _isUploadSpeedLimitEnabled = UploadLimitToggleSwitch.IsOn;
            _downloadLimitBytesPerSecond = _isDownloadSpeedLimitEnabled
                ? GetSpeedLimitBytesPerSecond(DownloadLimitNumberBox, GetSelectedSpeedLimitUnit(DownloadLimitUnitComboBox))
                : 0;
            _uploadLimitBytesPerSecond = _isUploadSpeedLimitEnabled
                ? GetSpeedLimitBytesPerSecond(UploadLimitNumberBox, GetSelectedSpeedLimitUnit(UploadLimitUnitComboBox))
                : 0;

            Aria2EngineStartResult startResult = await EnsureAria2StartedAsync();
            if (!startResult.Started)
            {
                ShowMessage(startResult.Message, InfoBarSeverity.Error);
                return;
            }

            try
            {
                await ApplyConfiguredSpeedLimitsAsync();
                SaveSpeedLimitSettings();
                UpdateGlobalSpeedLimitText();
                SpeedLimitButton.Flyout?.Hide();
                ShowMessage(Strings.Get("SpeedLimitAppliedMessage"), InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                ShowMessage(Strings.Format("SpeedLimitApplyFailedMessage", ex.Message), InfoBarSeverity.Error);
            }
        }

        private Task ApplyConfiguredSpeedLimitsAsync()
        {
            return _downloadCoordinator.SetGlobalSpeedLimitsAsync(
                _isDownloadSpeedLimitEnabled ? _downloadLimitBytesPerSecond : 0,
                _isUploadSpeedLimitEnabled ? _uploadLimitBytesPerSecond : 0);
        }

        private static long GetSpeedLimitBytesPerSecond(NumberBox numberBox, string unit)
        {
            if (numberBox is null || double.IsNaN(numberBox.Value))
            {
                return 0;
            }

            long multiplier = unit.Equals("MB/s", StringComparison.OrdinalIgnoreCase)
                ? 1024L * 1024L
                : 1024L;

            return Math.Max(1, (long)Math.Round(numberBox.Value)) * multiplier;
        }

        private void DownloadLimitToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            SetDownloadSpeedLimitInputsEnabled(DownloadLimitToggleSwitch.IsOn);
        }

        private void UploadLimitToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            SetUploadSpeedLimitInputsEnabled(UploadLimitToggleSwitch.IsOn);
        }

        private void SetDownloadSpeedLimitInputsEnabled(bool isEnabled)
        {
            if (DownloadLimitNumberBox is not null)
            {
                DownloadLimitNumberBox.IsEnabled = isEnabled;
            }

            if (DownloadLimitUnitComboBox is not null)
            {
                DownloadLimitUnitComboBox.IsEnabled = isEnabled;
            }
        }

        private void SetUploadSpeedLimitInputsEnabled(bool isEnabled)
        {
            if (UploadLimitNumberBox is not null)
            {
                UploadLimitNumberBox.IsEnabled = isEnabled;
            }

            if (UploadLimitUnitComboBox is not null)
            {
                UploadLimitUnitComboBox.IsEnabled = isEnabled;
            }
        }

        private static string GetSelectedSpeedLimitUnit(ComboBox comboBox)
        {
            return comboBox?.SelectedItem is ComboBoxItem item &&
                item.Content?.ToString() is string unit &&
                !string.IsNullOrWhiteSpace(unit)
                ? unit
                : "KB/s";
        }

        private void LoadSpeedLimitSettings()
        {
            SpeedLimitSettings settings = ReadSpeedLimitSettings();

            SetSpeedLimitUnit(UploadLimitUnitComboBox, settings.UploadUnit);
            SetSpeedLimitUnit(DownloadLimitUnitComboBox, settings.DownloadUnit);
            UploadLimitNumberBox.Value = Math.Max(settings.UploadValue, 1);
            DownloadLimitNumberBox.Value = Math.Max(settings.DownloadValue, 1);

            _isUploadSpeedLimitEnabled = settings.UploadEnabled;
            _isDownloadSpeedLimitEnabled = settings.DownloadEnabled;
            _uploadLimitBytesPerSecond = settings.UploadEnabled
                ? GetSpeedLimitBytesPerSecond(UploadLimitNumberBox, GetSelectedSpeedLimitUnit(UploadLimitUnitComboBox))
                : 0;
            _downloadLimitBytesPerSecond = settings.DownloadEnabled
                ? GetSpeedLimitBytesPerSecond(DownloadLimitNumberBox, GetSelectedSpeedLimitUnit(DownloadLimitUnitComboBox))
                : 0;

            UploadLimitToggleSwitch.IsOn = settings.UploadEnabled;
            DownloadLimitToggleSwitch.IsOn = settings.DownloadEnabled;
            SetUploadSpeedLimitInputsEnabled(settings.UploadEnabled);
            SetDownloadSpeedLimitInputsEnabled(settings.DownloadEnabled);
        }

        private SpeedLimitSettings ReadSpeedLimitSettings()
        {
            if (!File.Exists(_speedLimitSettingsPath))
            {
                return SpeedLimitSettings.Default;
            }

            try
            {
                string json = File.ReadAllText(_speedLimitSettingsPath);
                return JsonSerializer.Deserialize<SpeedLimitSettings>(json) ?? SpeedLimitSettings.Default;
            }
            catch
            {
                return SpeedLimitSettings.Default;
            }
        }

        private void SaveSpeedLimitSettings()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_speedLimitSettingsPath)!);
                SpeedLimitSettings settings = new(
                    DownloadLimitToggleSwitch?.IsOn == true,
                    GetValidNumberBoxValue(DownloadLimitNumberBox),
                    GetSelectedSpeedLimitUnit(DownloadLimitUnitComboBox),
                    UploadLimitToggleSwitch?.IsOn == true,
                    GetValidNumberBoxValue(UploadLimitNumberBox),
                    GetSelectedSpeedLimitUnit(UploadLimitUnitComboBox));

                File.WriteAllText(_speedLimitSettingsPath, JsonSerializer.Serialize(settings, new JsonSerializerOptions
                {
                    WriteIndented = true
                }));
            }
            catch
            {
                // Settings persistence is best-effort; aria2 can still run with default limits.
            }
        }

        private static double GetValidNumberBoxValue(NumberBox numberBox)
        {
            return numberBox is null || double.IsNaN(numberBox.Value) || numberBox.Value < 1
                ? 1
                : numberBox.Value;
        }

        private static void SetSpeedLimitUnit(ComboBox comboBox, string unit)
        {
            comboBox.SelectedIndex = unit.Equals("MB/s", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        }

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

        private void UpdateSelectionCommands()
        {
            List<DownloadTask> selectedTasks = GetSelectedTasks();
            bool hasSelection = selectedTasks.Count > 0;
            ResumeTasksButton.IsEnabled = hasSelection;
            PauseTasksButton.IsEnabled = hasSelection;
            RecoverTasksButton.IsEnabled = selectedTasks.Any(IsRecoverableTask);
            DeleteTasksButton.IsEnabled = hasSelection;
            UpdateSelectAllCheckBox();
        }

        private static bool IsDownloadingTask(DownloadTask task)
        {
            return task.Status.Contains("download", StringComparison.OrdinalIgnoreCase)
                || task.Status.Contains("waiting", StringComparison.OrdinalIgnoreCase)
                || task.Status.Contains("paused", StringComparison.OrdinalIgnoreCase);
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
            SetSettingVisibility(AriaPathSettingLabel, AriaPathTextBox, query, "aria2c", "path", Strings.Get("AriaPathLabel.Text"), Strings.Get("AriaPathTextBox.PlaceholderText"));
            SetSettingVisibility(RpcPortSettingLabel, RpcPortNumberBox, query, "rpc", "port", Strings.Get("RpcPortLabel.Text"));
            SetSettingVisibility(DefaultDirectorySettingLabel, DownloadDirectoryTextBox, query, "default", "directory", "download", Strings.Get("DefaultDirectoryLabel.Text"));
            SetSettingVisibility(ProxySettingLabel, UseSystemProxyCheckBox, query, "proxy", "system proxy", Strings.Get("ProxyLabel.Text"), Strings.Get("UseSystemProxyCheckBox.Content"));
            SetSettingVisibility(ProcessStatusSettingLabel, ProcessStatusSettingControl, query, "process", "status", "aria2", Strings.Get("ProcessStatusLabel.Text"));
        }

        private static void SetSettingVisibility(FrameworkElement label, FrameworkElement control, string query, params string[] searchableText)
        {
            bool isVisible = string.IsNullOrWhiteSpace(query) || searchableText.Any(text => SearchContains(text, query));
            Visibility visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            label.Visibility = visibility;
            control.Visibility = visibility;
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
            if (DownloadsTitleText is null || DownloadsSubtitleText is null || tag == "Settings")
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
            DownloadsSubtitleText.Text = Strings.Get($"{resourceKey}Subtitle");
        }

        private void UpdateStatsVisibility(string tag)
        {
            if (StatsPanel is null)
            {
                return;
            }

            StatsPanel.Visibility = tag == "Home" ? Visibility.Visible : Visibility.Collapsed;
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

        private void SetWindowIcon()
        {
            nint windowHandle = WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(windowHandle);
            AppWindow appWindow = AppWindow.GetFromWindowId(windowId);
            string? iconPath = ResolveAssetPath("Assets", "OmniDown.ico");
            if (!string.IsNullOrWhiteSpace(iconPath))
            {
                appWindow.SetIcon(iconPath);
            }
        }

        private static string? ResolveAssetPath(params string[] pathSegments)
        {
            string basePath = AppContext.BaseDirectory;
            string candidate = Path.Combine([basePath, .. pathSegments]);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            string? parentPath = Directory.GetParent(basePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))?.FullName;
            if (parentPath is null)
            {
                return null;
            }

            candidate = Path.Combine([parentPath, .. pathSegments]);
            return File.Exists(candidate) ? candidate : null;
        }

        private static string GetAppDataDirectory()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "OmniDown");
        }
    }

    internal sealed record SpeedLimitSettings(
        bool DownloadEnabled,
        double DownloadValue,
        string DownloadUnit,
        bool UploadEnabled,
        double UploadValue,
        string UploadUnit)
    {
        public static SpeedLimitSettings Default { get; } = new(
            DownloadEnabled: false,
            DownloadValue: 1024,
            DownloadUnit: "KB/s",
            UploadEnabled: false,
            UploadValue: 1024,
            UploadUnit: "KB/s");
    }

    internal sealed record AppStatusMessage(
        string Message,
        string DetailText,
        string SeverityText,
        string SeverityGlyph,
        Brush SeverityBrush);

    internal enum TaskSortColumn
    {
        CreatedAt,
        Name,
        Size
    }
}
