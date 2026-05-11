using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OmniDown.Models;
using System;
using System.ComponentModel;
using System.IO;

namespace OmniDown.Controls;

public sealed partial class TaskDetailsPaneControl : UserControl
{
    public static readonly DependencyProperty SelectedTaskProperty =
        DependencyProperty.Register(
            nameof(SelectedTask),
            typeof(DownloadTask),
            typeof(TaskDetailsPaneControl),
            new PropertyMetadata(null, OnSelectedTaskChanged));

    public static readonly DependencyProperty SelectedTaskCountProperty =
        DependencyProperty.Register(
            nameof(SelectedTaskCount),
            typeof(int),
            typeof(TaskDetailsPaneControl),
            new PropertyMetadata(0, OnSelectedTaskCountChanged));

    private DownloadTask? _subscribedTask;

    public TaskDetailsPaneControl()
    {
        InitializeComponent();
        TaskDetailsSelectorBar.SelectedItem = TaskDetailsSummaryItem;
        Refresh();
    }

    public DownloadTask? SelectedTask
    {
        get => (DownloadTask?)GetValue(SelectedTaskProperty);
        set => SetValue(SelectedTaskProperty, value);
    }

    public int SelectedTaskCount
    {
        get => (int)GetValue(SelectedTaskCountProperty);
        set => SetValue(SelectedTaskCountProperty, value);
    }

    private static void OnSelectedTaskChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        TaskDetailsPaneControl control = (TaskDetailsPaneControl)dependencyObject;
        control.SubscribeToTask(args.OldValue as DownloadTask, args.NewValue as DownloadTask);
        control.Refresh();
    }

    private static void OnSelectedTaskCountChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        ((TaskDetailsPaneControl)dependencyObject).Refresh();
    }

    private void SubscribeToTask(DownloadTask? oldTask, DownloadTask? newTask)
    {
        if (oldTask is not null)
        {
            oldTask.PropertyChanged -= SelectedTask_PropertyChanged;
        }

        _subscribedTask = newTask;
        if (_subscribedTask is not null)
        {
            _subscribedTask.PropertyChanged += SelectedTask_PropertyChanged;
        }
    }

    private void SelectedTask_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (SelectedTask is null)
        {
            ShowEmpty();
            return;
        }

        ShowTask(SelectedTask);
    }

    private void ShowEmpty()
    {
        TaskDetailsEmptyContent.Visibility = Visibility.Visible;
        TaskDetailsSummaryContent.Visibility = Visibility.Collapsed;
        TaskDetailsActivityContent.Visibility = Visibility.Collapsed;
        TaskDetailsFilesContent.Visibility = Visibility.Collapsed;
        TaskDetailsOptionsContent.Visibility = Visibility.Collapsed;
        TaskDetailsPeersContent.Visibility = Visibility.Collapsed;
        TaskDetailsTrackersContent.Visibility = Visibility.Collapsed;

        TaskDetailsEmptyTitleText.Text = SelectedTaskCount == 0 ? "未选择任务" : $"已选择 {SelectedTaskCount} 个任务";
        TaskDetailsEmptyMessageText.Text = SelectedTaskCount == 0
            ? "选择一个任务以查看概要、活动、文件、选项、节点和追踪器。"
            : "请选择单个任务以查看详细信息并继续编辑任务级选项。";
    }

    private void ShowTask(DownloadTask task)
    {
        TaskDetailsEmptyContent.Visibility = Visibility.Collapsed;

        TaskDetailsNameText.Text = string.IsNullOrWhiteSpace(task.Name) ? "未命名任务" : task.Name;
        string resolvedFilePath = ResolveTaskFilePath(task);
        TaskDetailsPathText.Text = string.IsNullOrWhiteSpace(resolvedFilePath)
            ? task.SaveDirectory
            : resolvedFilePath;
        TaskDetailsGidText.Text = string.IsNullOrWhiteSpace(task.Gid) ? "-" : task.Gid;
        TaskDetailsStatusText.Text = task.StatusText;
        TaskDetailsStatusText.Foreground = task.StatusBrush;
        TaskDetailsHeroIcon.Glyph = task.IsPeerTransfer ? "\uE968" : "\uE7C3";
        TaskDetailsHeroIcon.Foreground = task.StatusBrush;
        TaskDetailsSizeText.Text = task.SizeText;
        TaskDetailsProgressText.Text = task.ProgressText;
        TaskDetailsCreatedAtText.Text = task.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
        TaskDetailsSourceText.Text = string.IsNullOrWhiteSpace(task.SourceUri) ? "-" : task.SourceUri;

        TaskDetailsProgressBar.Value = Math.Clamp(task.Progress, 0, 100);
        TaskDetailsProgressBar.Foreground = task.ProgressBrush;
        TaskDetailsCompletedText.Text = task.TotalLength > 0
            ? $"{FormatBytesForDetails(task.CompletedLength)} / {FormatBytesForDetails(task.TotalLength)}"
            : FormatBytesForDetails(task.CompletedLength);
        TaskDetailsRemainingText.Text = task.RemainingTimeText;
        TaskDetailsDownloadSpeedText.Text = task.DownloadSpeedText;
        TaskDetailsUploadSpeedText.Text = task.UploadSpeedText;

        ShowTaskDetailsSection(SelectedSectionTag);
    }

    private string SelectedSectionTag => TaskDetailsSelectorBar.SelectedItem?.Tag?.ToString() ?? "Summary";

    private void TaskDetailsSelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        ShowTaskDetailsSection(SelectedSectionTag);
    }

    private void ShowTaskDetailsSection(string tag)
    {
        bool hasSingleSelection = TaskDetailsEmptyContent.Visibility != Visibility.Visible;
        TaskDetailsSummaryContent.Visibility = hasSingleSelection && tag == "Summary" ? Visibility.Visible : Visibility.Collapsed;
        TaskDetailsActivityContent.Visibility = hasSingleSelection && tag == "Activity" ? Visibility.Visible : Visibility.Collapsed;
        TaskDetailsFilesContent.Visibility = hasSingleSelection && tag == "Files" ? Visibility.Visible : Visibility.Collapsed;
        TaskDetailsOptionsContent.Visibility = hasSingleSelection && tag == "Options" ? Visibility.Visible : Visibility.Collapsed;
        TaskDetailsPeersContent.Visibility = hasSingleSelection && tag == "Peers" ? Visibility.Visible : Visibility.Collapsed;
        TaskDetailsTrackersContent.Visibility = hasSingleSelection && tag == "Trackers" ? Visibility.Visible : Visibility.Collapsed;
    }

    private static string FormatBytesForDetails(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double size = bytes;
        int unitIndex = 0;
        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        return $"{size:0.#} {units[unitIndex]}";
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
}
