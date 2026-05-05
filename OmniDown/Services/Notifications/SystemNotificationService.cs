using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using OmniDown.Models;
using OmniDown.Services.Localization;
using System;

namespace OmniDown.Services.Notifications;

public sealed class SystemNotificationService
{
    private bool _isRegistered;

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
            Strings.Format("TaskAddedNotificationBody", GetTaskName(task)));
    }

    public void ShowDownloadCompleted(DownloadTask task)
    {
        Show(
            Strings.Get("TaskDownloadCompletedNotificationTitle"),
            Strings.Format("TaskDownloadCompletedNotificationBody", GetTaskName(task)));
    }

    public void ShowDownloadFailed(DownloadTask task)
    {
        Show(
            Strings.Get("TaskDownloadFailedNotificationTitle"),
            Strings.Format("TaskDownloadFailedNotificationBody", GetTaskName(task)));
    }

    private static void Show(string title, string body)
    {
        try
        {
            AppNotification notification = new AppNotificationBuilder()
                .AddText(title)
                .AddText(body)
                .BuildNotification();

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

    private static void OnNotificationInvoked(
        AppNotificationManager sender,
        AppNotificationActivatedEventArgs args)
    {
    }
}
