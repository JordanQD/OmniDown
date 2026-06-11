using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using OmniDown.Models;
using OmniDown.Services.Engine;
using OmniDown.Services.Localization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                await Task.Delay(150);
                await RefreshDownloadsAsync();
                await ConfirmOperationAsync([task]);
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
            string filePath = ResolveTaskFilePath(task);
            if (!string.IsNullOrWhiteSpace(task.SaveDirectory) &&
                !string.IsNullOrWhiteSpace(task.Name))
            {
                string contentDirectory = Path.Combine(task.SaveDirectory, task.Name);
                if (Directory.Exists(contentDirectory))
                {
                    return contentDirectory;
                }
            }

            if (!string.IsNullOrWhiteSpace(filePath))
            {
                if (Directory.Exists(filePath))
                {
                    return filePath;
                }

                string? fileDirectory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrWhiteSpace(fileDirectory))
                {
                    return fileDirectory;
                }
            }

            return task.SaveDirectory;
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
            UpdateStatusBar();
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

        private static bool IsRunningDownloadTask(DownloadTask task)
        {
            return task.Status.Contains("download", StringComparison.OrdinalIgnoreCase)
                || task.Status.Contains("resum", StringComparison.OrdinalIgnoreCase)
                || task.Status.Contains("paus", StringComparison.OrdinalIgnoreCase) && !IsPausedTask(task);
        }

        private static bool IsWaitingTask(DownloadTask task)
        {
            return task.Status.Contains("waiting", StringComparison.OrdinalIgnoreCase);
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

        private static bool IsTaskDownloadContentCompleted(DownloadTask task)
        {
            if (IsCompletedTask(task))
            {
                return true;
            }

            return task.IsPeerTransfer &&
                task.TotalLength > 0 &&
                task.CompletedLength >= task.TotalLength;
        }

        private static bool IsIssueTask(DownloadTask task)
        {
            return IsErrorTaskStatus(task.Status);
        }

        private static bool IsTaskCategoryMatch(DownloadTask task, string categoryTag)
        {
            return categoryTag switch
            {
                "Images" => HasExtension(task, ImageExtensions),
                "Videos" => HasExtension(task, VideoExtensions),
                "Audio" => HasExtension(task, AudioExtensions),
                "Documents" => HasExtension(task, DocumentExtensions),
                "Archives" => HasExtension(task, ArchiveExtensions),
                "Apps" => HasExtension(task, AppExtensions),
                "Folders" => IsFolderLikeTask(task),
                "Other" => !IsKnownCategoryTask(task),
                _ => true
            };
        }

        private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".tif", ".tiff", ".heic", ".avif", ".svg", ".ico"
        };

        private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".ts", ".m2ts", ".3gp"
        };

        private static readonly HashSet<string> AudioExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp3", ".flac", ".wav", ".aac", ".m4a", ".ogg", ".opus", ".wma", ".ape", ".alac"
        };

        private static readonly HashSet<string> DocumentExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".md", ".rtf", ".epub", ".mobi", ".azw3", ".csv", ".json", ".xml"
        };

        private static readonly HashSet<string> ArchiveExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2", ".xz", ".zst", ".tgz", ".tbz2", ".txz", ".iso"
        };

        private static readonly HashSet<string> AppExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".exe", ".msi", ".msix", ".msixbundle", ".appx", ".appxbundle", ".apk", ".dmg", ".pkg", ".deb", ".rpm"
        };

        private static bool IsKnownCategoryTask(DownloadTask task)
        {
            return IsFolderLikeTask(task)
                || HasExtension(task, ImageExtensions)
                || HasExtension(task, VideoExtensions)
                || HasExtension(task, AudioExtensions)
                || HasExtension(task, DocumentExtensions)
                || HasExtension(task, ArchiveExtensions)
                || HasExtension(task, AppExtensions);
        }

        private static bool IsFolderLikeTask(DownloadTask task)
        {
            if (task.IsPeerTransfer)
            {
                return true;
            }

            string filePath = ResolveTaskFilePath(task);
            return !string.IsNullOrWhiteSpace(filePath) && Directory.Exists(filePath);
        }

        private static bool HasExtension(DownloadTask task, HashSet<string> extensions)
        {
            string extension = GetTaskExtension(task);
            return !string.IsNullOrWhiteSpace(extension) && extensions.Contains(extension);
        }

        private static string GetTaskExtension(DownloadTask task)
        {
            string[] candidates =
            [
                task.LocalFilePath,
                task.Name,
                task.SourceUri
            ];

            foreach (string candidate in candidates)
            {
                if (string.IsNullOrWhiteSpace(candidate))
                {
                    continue;
                }

                string value = candidate;
                if (Uri.TryCreate(candidate, UriKind.Absolute, out Uri? uri))
                {
                    value = uri.LocalPath;
                }

                string extension = Path.GetExtension(value);
                if (!string.IsNullOrWhiteSpace(extension))
                {
                    return extension;
                }
            }

            return string.Empty;
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

            SettingsPage.ApplySearchFilter(GetSearchQuery());
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

            DownloadsTitleText.Text = tag switch
            {
                "Waiting" => "等待中",
                "Paused" => "暂停中",
                _ => Strings.Get($"{resourceKey}Title")
            };
        }

        private void UpdateStatsVisibility(string tag)
        {
            if (StatsPanel is null)
            {
                return;
            }

            StatsPanel.Visibility = Visibility.Collapsed;
            CompletedMetricPanel.Visibility = tag == "Home" ? Visibility.Visible : Visibility.Collapsed;
            IssueMetricPanel.Visibility = tag == "Home" ? Visibility.Visible : Visibility.Collapsed;
            TasksListHeaderPanel.Visibility = Visibility.Visible;
            TasksListHeaderPanel.Margin = new Thickness(0, 8, 0, 4);
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

            // 同步 ViewModel 数据
            _downloadsViewModel.AllTasks.Clear();
            foreach (var task in Tasks) _downloadsViewModel.AllTasks.Add(task);
            _downloadsViewModel.CurrentFilter = _currentTaskFilter;
            _downloadsViewModel.SortColumn = _sortColumn;
            _downloadsViewModel.SortAscending = _sortAscending;
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
