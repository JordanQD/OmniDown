using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using OmniDown.Models;
using OmniDown.Services.Engine;
using OmniDown.Services.Localization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace OmniDown
{
    public sealed partial class MainWindow
    {
        private async void ResumeTasksButton_Click(object sender, RoutedEventArgs e)
        {
            await RunTaskOperationSetAsync(
                GetSelectedTasks().Where(IsPausedTask).ToArray(),
                tasks => _downloadCoordinator.ResumeAsync(tasks),
                Strings.Get("TasksResumedMessage"));
        }

        private async void PauseTasksButton_Click(object sender, RoutedEventArgs e)
        {
            await RunTaskOperationSetAsync(
                GetSelectedTasks().Where(IsActiveTransferTask).ToArray(),
                tasks => _downloadCoordinator.PauseAsync(tasks),
                Strings.Get("TasksPausedMessage"));
        }

        private async void RecoverTasksButton_Click(object sender, RoutedEventArgs e)
        {
            await RunTaskOperationSetAsync(
                GetSelectedTasks().Where(IsRecoverableTask).ToArray(),
                tasks => _downloadCoordinator.RecoverAsync(tasks, GetDefaultRecoverySplitCount()),
                Strings.Get("TasksRecoveredMessage"));
        }

        private void OpenSelectedTaskFileButton_Click(object sender, RoutedEventArgs e)
        {
            DownloadTask? task = GetSingleSelectedTask();
            if (task is not null)
            {
                OpenTaskFile(task);
            }
        }

        private void OpenSelectedTaskFolderButton_Click(object sender, RoutedEventArgs e)
        {
            DownloadTask? task = GetSingleSelectedTask();
            if (task is not null)
            {
                OpenTaskFolder(task);
            }
        }

        private void CopySelectedTaskLinksButton_Click(object sender, RoutedEventArgs e)
        {
            CopyTaskLinks(GetSelectedTasks());
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
            else if (task.IsSharing)
            {
                await RunTaskOperationAsync(
                    task,
                    tasks => _downloadCoordinator.StopEd2kSharingAsync(tasks),
                    Strings.Get("Ed2kSharingStoppedMessage"));
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

            OpenTaskFile(task);
        }

        private void OpenTaskFile(DownloadTask task)
        {
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

            OpenTaskFolder(task);
        }

        private void OpenTaskFolder(DownloadTask task)
        {
            string folderPath = ResolveTaskFolderPath(task);

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

            CopyTaskLinks([task]);
        }

        private void CopyTaskLinks(IReadOnlyList<DownloadTask> tasks)
        {
            string[] links = tasks
                .Select(task => task.SourceUri)
                .Where(link => !string.IsNullOrWhiteSpace(link))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (links.Length == 0)
            {
                ShowMessage(Strings.Get("TaskLinkNotFoundMessage"), InfoBarSeverity.Warning);
                return;
            }

            DataPackage package = new();
            package.SetText(string.Join(Environment.NewLine, links));
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
            UpdateTaskDetailsPane();
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
            UpdateTaskDetailsPane();
        }

        private void TasksListView_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (sender is not ListView listView ||
                FindAncestor<ListViewItem>(e.OriginalSource as DependencyObject) is not null ||
                !e.GetCurrentPoint(listView).Properties.IsLeftButtonPressed)
            {
                return;
            }

            TasksListView.SelectedItems.Clear();
            foreach (DownloadTask task in _visibleTasks)
            {
                task.IsSelected = false;
                UpdateTaskSelectionGlyphVisibility(task);
            }

            UpdateSelectionCommands();
            UpdateTaskDetailsPane();
        }

        private void TasksListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (sender is not ListView listView)
            {
                return;
            }

            DownloadTask? rightTappedTask = null;
            if (FindAncestor<ListViewItem>(e.OriginalSource as DependencyObject) is ListViewItem itemContainer)
            {
                rightTappedTask = itemContainer.Content as DownloadTask ?? itemContainer.DataContext as DownloadTask;
            }

            if (rightTappedTask is null)
            {
                TasksListView.SelectedItems.Clear();
                UpdateSelectionCommands();
                UpdateTaskDetailsPane();
                return;
            }

            if (!TasksListView.SelectedItems.Contains(rightTappedTask))
            {
                _isUpdatingTaskSelection = true;
                try
                {
                    TasksListView.SelectedItems.Clear();
                    foreach (DownloadTask task in _visibleTasks)
                    {
                        task.IsSelected = ReferenceEquals(task, rightTappedTask);
                        if (task.IsSelected)
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
                UpdateTaskDetailsPane();
            }

            MenuFlyout flyout = CreateTaskContextMenu(GetSelectedTasks());
            if (flyout.Items.Count > 0)
            {
                flyout.ShowAt(listView, e.GetPosition(listView));
                e.Handled = true;
            }
        }

        private MenuFlyout CreateTaskContextMenu(IReadOnlyList<DownloadTask> selectedTasks)
        {
            MenuFlyout flyout = new();
            if (selectedTasks.Count == 0)
            {
                return flyout;
            }

            DownloadTask[] activeTasks = selectedTasks.Where(IsActiveTransferTask).ToArray();
            DownloadTask[] pausedTasks = selectedTasks.Where(IsPausedTask).ToArray();
            DownloadTask[] recoverableTasks = selectedTasks.Where(IsRecoverableTask).ToArray();
            DownloadTask[] sharingTasks = selectedTasks.Where(task => task.IsSharing && task.IsEd2kTransfer).ToArray();

            if (sharingTasks.Length > 0)
            {
                flyout.Items.Add(CreateTaskContextMenuItem(
                    Strings.Get("StopSharingActionText"),
                    "\uE71A",
                    async () => await RunTaskOperationSetAsync(
                        sharingTasks,
                        tasks => _downloadCoordinator.StopEd2kSharingAsync(tasks),
                        Strings.Get("Ed2kSharingStoppedMessage"))));
            }

            if (activeTasks.Length > 0)
            {
                flyout.Items.Add(CreateTaskContextMenuItem(
                    Strings.Get("PauseTasksButton.Label"),
                    "\uE769",
                    async () => await RunTaskOperationSetAsync(
                        activeTasks,
                        tasks => _downloadCoordinator.PauseAsync(tasks),
                        Strings.Get("TasksPausedMessage"))));
            }

            if (pausedTasks.Length > 0)
            {
                flyout.Items.Add(CreateTaskContextMenuItem(
                    Strings.Get("ResumeTasksButton.Label"),
                    "\uE768",
                    async () => await RunTaskOperationSetAsync(
                        pausedTasks,
                        tasks => _downloadCoordinator.ResumeAsync(tasks),
                        Strings.Get("TasksResumedMessage"))));
            }

            if (recoverableTasks.Length > 0)
            {
                flyout.Items.Add(CreateTaskContextMenuItem(
                    Strings.Get("RecoverTasksButton.Label"),
                    "\uE72C",
                    async () => await RunTaskOperationSetAsync(
                        recoverableTasks,
                        tasks => _downloadCoordinator.RecoverAsync(tasks, GetDefaultRecoverySplitCount()),
                        Strings.Get("TasksRecoveredMessage"))));
            }

            bool hasTransferAction = flyout.Items.Count > 0;
            if (selectedTasks.Count == 1)
            {
                DownloadTask task = selectedTasks[0];
                if (CanOpenTaskFile(task))
                {
                    AddTaskContextSeparatorIfNeeded(flyout, hasTransferAction);
                    flyout.Items.Add(CreateTaskContextMenuItem(
                        Strings.Get("TaskOpenFileActionText"),
                        "\uE8A7",
                        () => OpenTaskFile(task)));
                }

                if (CanOpenTaskFolder(task))
                {
                    AddTaskContextSeparatorIfNeeded(flyout, hasTransferAction || CanOpenTaskFile(task));
                    flyout.Items.Add(CreateTaskContextMenuItem(
                        Strings.Get("TaskOpenFolderActionText"),
                        "\uE8B7",
                        () => OpenTaskFolder(task)));
                }
            }

            if (selectedTasks.Any(task => !string.IsNullOrWhiteSpace(task.SourceUri)))
            {
                AddTaskContextSeparatorIfNeeded(flyout, flyout.Items.Count > 0);
                flyout.Items.Add(CreateTaskContextMenuItem(
                    Strings.Get("TaskCopyLinkActionText"),
                    "\uE71B",
                    () => CopyTaskLinks(selectedTasks)));
            }

            AddTaskContextSeparatorIfNeeded(flyout, flyout.Items.Count > 0);
            flyout.Items.Add(CreateTaskContextMenuItem(
                Strings.Get("TaskDeleteEntryActionText"),
                "\uE711",
                async () =>
                {
                    bool? deleteFiles = await ConfirmDeleteAsync();
                    if (deleteFiles is null)
                    {
                        return;
                    }

                    await RunTaskOperationSetAsync(
                        selectedTasks,
                        tasks => _downloadCoordinator.DeleteAsync(tasks, deleteFiles.Value),
                        Strings.Get("TasksDeletedMessage"));
                }));

            return flyout;
        }

        private static MenuFlyoutItem CreateTaskContextMenuItem(string text, string glyph, Action action)
        {
            MenuFlyoutItem item = new()
            {
                Text = text,
                Icon = new FontIcon { Glyph = glyph }
            };
            item.Click += (_, _) => action();
            return item;
        }

        private static void AddTaskContextSeparatorIfNeeded(MenuFlyout flyout, bool shouldAdd)
        {
            if (shouldAdd && flyout.Items.LastOrDefault() is not MenuFlyoutSeparator)
            {
                flyout.Items.Add(new MenuFlyoutSeparator());
            }
        }

        private async System.Threading.Tasks.Task RunTaskOperationSetAsync(
            IReadOnlyList<DownloadTask> tasks,
            Func<IReadOnlyList<DownloadTask>, System.Threading.Tasks.Task> operation,
            string successMessage)
        {
            DownloadTask[] taskSet = tasks
                .Where(task => !string.IsNullOrWhiteSpace(task.Gid))
                .ToArray();
            if (taskSet.Length == 0)
            {
                return;
            }

            Aria2EngineStartResult startResult = await EnsureAria2StartedAsync();
            if (!startResult.Started)
            {
                ShowEngineStartFailure(startResult);
                return;
            }

            try
            {
                await operation(taskSet);
                await Task.Delay(150);
                await RefreshDownloadsAsync();
                await ConfirmOperationAsync(taskSet);
                UpdateSelectionCommands();
                ShowMessage(successMessage, InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                ApplyTaskFilter(_currentTaskFilter);
                UpdateDashboard();
                UpdateGlobalSpeedsFromTasks();
                UpdateSelectionCommands();
                ShowUserError(UserErrorContext.TaskOperation, ex);
            }
        }

        private async System.Threading.Tasks.Task ConfirmOperationAsync(IReadOnlyList<DownloadTask> tasks)
        {
            for (int i = 0; i < 6 && tasks.Any(t => t.IsOperationPending); i++)
            {
                await Task.Delay(500);
                await RefreshDownloadsAsync();
            }
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
    }
}
