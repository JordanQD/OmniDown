using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OmniDown.Models;
using OmniDown.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace OmniDown
{
    public sealed partial class MainWindow : Window
    {
        private readonly Aria2ProcessService _aria2ProcessService = new();

        public ObservableCollection<DownloadTask> Tasks { get; } = new();

        public MainWindow()
        {
            InitializeComponent();
            Closed += MainWindow_Closed;

            DownloadDirectoryTextBox.Text = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads");

            SeedMvpTasks();
            ApplyTaskFilter("Tasks");
            UpdateDashboard();
            UpdateAriaStatus();
        }

        private async void NewDownloadButton_Click(object sender, RoutedEventArgs e)
        {
            TextBox uriTextBox = new()
            {
                Header = "Download URL",
                PlaceholderText = "https://example.com/file.zip"
            };

            TextBox fileNameTextBox = new()
            {
                Header = "Task name",
                PlaceholderText = "Leave empty to infer from the URL"
            };

            TextBox directoryTextBox = new()
            {
                Header = "Save directory",
                Text = DownloadDirectoryTextBox.Text
            };

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
                Title = "New download",
                Content = content,
                PrimaryButtonText = "Add",
                CloseButtonText = "Cancel",
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
                ShowMessage("A download URL is required.", InfoBarSeverity.Warning);
                return;
            }

            Tasks.Insert(0, new DownloadTask
            {
                Name = ResolveTaskName(sourceUri, fileNameTextBox.Text),
                SourceUri = sourceUri,
                SaveDirectory = string.IsNullOrWhiteSpace(directoryTextBox.Text)
                    ? DownloadDirectoryTextBox.Text
                    : directoryTextBox.Text.Trim(),
                Status = _aria2ProcessService.IsRunning ? "Awaiting RPC" : "Waiting for aria2",
                Progress = 0
            });

            UpdateDashboard();
            ShowMessage("Task added. Full aria2 RPC dispatch belongs to the phase 2 option map.", InfoBarSeverity.Success);
        }

        private async void StartAriaButton_Click(object sender, RoutedEventArgs e)
        {
            int rpcPort = double.IsNaN(RpcPortNumberBox.Value) ? 6800 : (int)RpcPortNumberBox.Value;
            Aria2StartResult result = await _aria2ProcessService.StartAsync(
                AriaPathTextBox.Text.Trim(),
                rpcPort,
                DownloadDirectoryTextBox.Text.Trim());

            UpdateAriaStatus();
            ShowMessage(result.Message, result.Started ? InfoBarSeverity.Success : InfoBarSeverity.Error);
        }

        private void StopAriaButton_Click(object sender, RoutedEventArgs e)
        {
            _aria2ProcessService.Stop();
            UpdateAriaStatus();
            ShowMessage("aria2 stopped.", InfoBarSeverity.Informational);
        }

        private void RootNavigation_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is not NavigationViewItem item)
            {
                return;
            }

            string tag = item.Tag?.ToString() ?? "Tasks";
            SettingsPage.Visibility = tag == "Settings" ? Visibility.Visible : Visibility.Collapsed;
            TasksPage.Visibility = tag == "Settings" ? Visibility.Collapsed : Visibility.Visible;

            ApplyTaskFilter(tag);
        }

        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            _aria2ProcessService.Dispose();
        }

        private void ApplyTaskFilter(string tag)
        {
            TasksListView.ItemsSource = tag switch
            {
                "Active" => Tasks.Where(task => task.Status.Contains("download", StringComparison.OrdinalIgnoreCase)),
                "Completed" => Tasks.Where(task => task.Status.Contains("complete", StringComparison.OrdinalIgnoreCase)),
                _ => Tasks
            };
        }

        private void SeedMvpTasks()
        {
            Tasks.Add(new DownloadTask
            {
                Name = "OmniDown MVP sample",
                SourceUri = "https://example.com/release.zip",
                SaveDirectory = DownloadDirectoryTextBox.Text,
                Status = "Waiting for aria2",
                Progress = 0
            });
        }

        private void UpdateDashboard()
        {
            TotalTasksText.Text = Tasks.Count.ToString();
            ActiveTasksText.Text = Tasks.Count(task => task.Status.Contains("download", StringComparison.OrdinalIgnoreCase)).ToString();
            DownloadSpeedText.Text = "0 KB/s";
        }

        private void UpdateAriaStatus()
        {
            string status = _aria2ProcessService.IsRunning
                ? $"Running #{_aria2ProcessService.ProcessId}"
                : "Stopped";

            AriaStatusText.Text = status;
            SettingsAriaStatusText.Text = status;
        }

        private void ShowMessage(string message, InfoBarSeverity severity)
        {
            StatusInfoBar.Message = message;
            StatusInfoBar.Severity = severity;
            StatusInfoBar.IsOpen = true;
        }

        private static string ResolveTaskName(string sourceUri, string requestedName)
        {
            if (!string.IsNullOrWhiteSpace(requestedName))
            {
                return requestedName.Trim();
            }

            if (Uri.TryCreate(sourceUri, UriKind.Absolute, out Uri? uri))
            {
                string fileName = System.IO.Path.GetFileName(uri.LocalPath);
                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    return fileName;
                }
            }

            return "New download";
        }
    }
}
