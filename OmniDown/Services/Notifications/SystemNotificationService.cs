using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using OmniDown.Models;
using OmniDown.Services.Localization;
using System;
using System.Collections.Generic;
using System.IO;

namespace OmniDown.Services.Notifications;

public sealed class SystemNotificationService
{
    public const string ActionTaskAdded = "TaskAdded";
    public const string ActionDownloadCompleted = "DownloadCompleted";
    public const string ActionDownloadFailed = "DownloadFailed";

    private bool _isRegistered;

    public event EventHandler<TaskNotificationInvokedEventArgs>? NotificationInvoked;

    public void Register()
    {
        try
        {
            AppNotificationManager.Default.NotificationInvoked += OnNotificationInvoked;
            AppNotificationManager.Default.Register();
            _isRegistered = true;
        }
        catch
        {
            _isRegistered = false;
        }
    }

    public void Unregister()
    {
        if (!_isRegistered)
        {
            return;
        }

        try
        {
            AppNotificationManager.Default.Unregister();
            AppNotificationManager.Default.NotificationInvoked -= OnNotificationInvoked;
        }
        catch
        {
            // Notification teardown should never block app shutdown.
        }
    }

    public void ShowTaskAdded(DownloadTask task)
    {
        Show(
            Strings.Get("TaskAddedNotificationTitle"),
            Strings.Format("TaskAddedNotificationBody", GetTaskName(task)),
            ActionTaskAdded);
    }

    public void ShowDownloadCompleted(DownloadTask task)
    {
        Show(
            Strings.Get("TaskDownloadCompletedNotificationTitle"),
            Strings.Format("TaskDownloadCompletedNotificationBody", GetTaskName(task)),
            ActionDownloadCompleted,
            ResolveTaskFilePath(task),
            ResolveTaskFolderPath(task));
    }

    public void ShowDownloadFailed(DownloadTask task)
    {
        Show(
            Strings.Get("TaskDownloadFailedNotificationTitle"),
            Strings.Format("TaskDownloadFailedNotificationBody", GetTaskName(task)),
            ActionDownloadFailed);
    }

    private static void Show(string title, string body, string action, string? filePath = null, string? folderPath = null)
    {
        try
        {
            AppNotificationBuilder builder = new AppNotificationBuilder()
                .AddText(title)
                .AddText(body)
                .AddArgument("action", action);

            if (!string.IsNullOrWhiteSpace(filePath))
            {
                builder.AddArgument("filePath", filePath);
            }

            if (!string.IsNullOrWhiteSpace(folderPath))
            {
                builder.AddArgument("folderPath", folderPath);
            }

            AppNotification notification = builder.BuildNotification();

            AppNotificationManager.Default.Show(notification);
        }
        catch
        {
            // Notification availability depends on the current Windows app deployment context.
        }
    }

    private static string GetTaskName(DownloadTask task)
    {
        return string.IsNullOrWhiteSpace(task.Name)
            ? Strings.Get("UnknownTaskName")
            : task.Name;
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
        return !string.IsNullOrWhiteSpace(task.SaveDirectory)
            ? task.SaveDirectory
            : Path.GetDirectoryName(ResolveTaskFilePath(task)) ?? string.Empty;
    }

    private void OnNotificationInvoked(
        AppNotificationManager sender,
        AppNotificationActivatedEventArgs args)
    {
        args.Arguments.TryGetValue("action", out string? action);
        args.Arguments.TryGetValue("filePath", out string? filePath);
        args.Arguments.TryGetValue("folderPath", out string? folderPath);

        if (!string.IsNullOrWhiteSpace(action))
        {
            NotificationInvoked?.Invoke(this, new TaskNotificationInvokedEventArgs(action, filePath, folderPath));
        }
    }

}

public sealed record TaskNotificationInvokedEventArgs(
    string Action,
    string? FilePath,
    string? FolderPath);
