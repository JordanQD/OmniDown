using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OmniDown.Models;
using OmniDown.Services.Downloads;
using OmniDown.Services.Engine;
using OmniDown.Services.Localization;
using OmniDown.Services.Rpc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;

namespace OmniDown
{
    public sealed partial class MainWindow : Window
    {
        private readonly Aria2EngineHost _aria2EngineHost = new();
        private readonly Aria2RpcClient _aria2RpcClient = new();
        private readonly DownloadCoordinator _downloadCoordinator;
        private readonly DispatcherTimer _refreshTimer = new();
        private readonly string _rpcSecret = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        private string _currentTaskFilter = "Tasks";
        private bool _isRefreshing;

        public ObservableCollection<DownloadTask> Tasks { get; } = new();

        public MainWindow()
        {
            InitializeComponent();
            _downloadCoordinator = new DownloadCoordinator(_aria2RpcClient, Tasks);
            Closed += MainWindow_Closed;
            _refreshTimer.Interval = TimeSpan.FromSeconds(2);
            _refreshTimer.Tick += RefreshTimer_Tick;

            DownloadDirectoryTextBox.Text = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads");

            ApplyTaskFilter("Tasks");
            UpdateDashboard();
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

            StackPanel content = new()
            {
                Spacing = 12,
                Children =
                {
                    uriTextBox,
                    fileNameTextBox,
                    directoryTextBox
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

            try
            {
                await _downloadCoordinator.AddDownloadAsync(sourceUri, fileNameTextBox.Text, saveDirectory);
                await RefreshDownloadsAsync();
                ShowMessage(Strings.Get("TaskAddedMessage"), InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                ShowMessage(Strings.Format("AddTaskFailedMessage", ex.Message), InfoBarSeverity.Error);
            }

            UpdateDashboard();
        }

        private async void StartAriaButton_Click(object sender, RoutedEventArgs e)
        {
            Aria2EngineStartResult result = await EnsureAria2StartedAsync();
            UpdateAriaStatus();
            ShowMessage(result.Message, result.Started ? InfoBarSeverity.Success : InfoBarSeverity.Error);
        }

        private void StopAriaButton_Click(object sender, RoutedEventArgs e)
        {
            _refreshTimer.Stop();
            _aria2EngineHost.Stop();
            UpdateAriaStatus();
            ShowMessage(Strings.Get("AriaStoppedMessage"), InfoBarSeverity.Informational);
        }

        private void RootNavigation_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is not NavigationViewItem item)
            {
                return;
            }

            string tag = item.Tag?.ToString() ?? "Tasks";
            _currentTaskFilter = tag;
            SettingsPage.Visibility = tag == "Settings" ? Visibility.Visible : Visibility.Collapsed;
            TasksPage.Visibility = tag == "Settings" ? Visibility.Collapsed : Visibility.Visible;

            ApplyTaskFilter(tag);
        }

        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            _refreshTimer.Stop();
            _aria2RpcClient.Dispose();
            _aria2EngineHost.Dispose();
        }

        private void ApplyTaskFilter(string tag)
        {
            TasksListView.ItemsSource = tag switch
            {
                "Active" => Tasks.Where(IsActiveTask),
                "Completed" => Tasks.Where(task => task.Status.Contains("complete", StringComparison.OrdinalIgnoreCase)),
                _ => Tasks
            };
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
                _refreshTimer.Start();
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

        private async System.Threading.Tasks.Task RefreshDownloadsAsync()
        {
            if (_isRefreshing || !_aria2EngineHost.IsRunning)
            {
                return;
            }

            _isRefreshing = true;
            try
            {
                DownloadSnapshot snapshot = await _downloadCoordinator.RefreshAsync();
                ApplyTaskFilter(_currentTaskFilter);
                UpdateDashboard(updateSpeed: false);
                ActiveTasksText.Text = snapshot.ActiveCount.ToString();
                DownloadSpeedText.Text = FormatSpeed(snapshot.DownloadSpeed);
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

        private void UpdateDashboard(bool updateSpeed = true)
        {
            TotalTasksText.Text = Tasks.Count.ToString();
            ActiveTasksText.Text = Tasks.Count(IsActiveTask).ToString();
            if (updateSpeed)
            {
                DownloadSpeedText.Text = "0 KB/s";
            }
        }

        private void UpdateAriaStatus()
        {
            string status = _aria2EngineHost.IsRunning
                ? Strings.Format("AriaRunningStatus", _aria2EngineHost.ProcessId ?? 0)
                : Strings.Get("AriaStoppedStatus");

            AriaStatusText.Text = status;
            SettingsAriaStatusText.Text = status;
            UpdateDebugStatus();
        }

        private void ShowMessage(string message, InfoBarSeverity severity)
        {
            StatusInfoBar.Message = message;
            StatusInfoBar.Severity = severity;
            StatusInfoBar.IsOpen = true;
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

        private async void DeleteTasksButton_Click(object sender, RoutedEventArgs e)
        {
            List<DownloadTask> selectedTasks = GetSelectedTasks();
            if (selectedTasks.Count == 0)
            {
                return;
            }

            ContentDialog dialog = new()
            {
                XamlRoot = Content.XamlRoot,
                Title = Strings.Get("DeleteDialogTitle"),
                Content = Strings.Get("DeleteDialogContent"),
                PrimaryButtonText = Strings.Get("DeleteButtonText"),
                CloseButtonText = Strings.Get("CancelButtonText"),
                DefaultButton = ContentDialogButton.Close
            };

            ContentDialogResult result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
            {
                return;
            }

            await RunSelectedTaskOperationAsync(
                tasks => _downloadCoordinator.DeleteAsync(tasks),
                Strings.Get("TasksDeletedMessage"));
        }

        private void TasksListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSelectionCommands();
        }

        private void UseSystemProxyCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            UpdateDebugStatus();
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

        private List<DownloadTask> GetSelectedTasks()
        {
            return TasksListView.SelectedItems
                .OfType<DownloadTask>()
                .Where(task => !string.IsNullOrWhiteSpace(task.Gid))
                .ToList();
        }

        private void UpdateSelectionCommands()
        {
            bool hasSelection = GetSelectedTasks().Count > 0;
            ResumeTasksButton.IsEnabled = hasSelection;
            PauseTasksButton.IsEnabled = hasSelection;
            DeleteTasksButton.IsEnabled = hasSelection;
        }

        private static bool IsActiveTask(DownloadTask task)
        {
            return task.Status.Contains("download", StringComparison.OrdinalIgnoreCase)
                || task.Status.Contains("waiting", StringComparison.OrdinalIgnoreCase);
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
