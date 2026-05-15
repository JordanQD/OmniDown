using Microsoft.Windows.AppNotifications;
using OmniDown.Models;
using OmniDown.Services.Localization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security;

namespace OmniDown.Services.Notifications;

public sealed class SystemNotificationService
{
    public const string ActionTaskAdded = "TaskAdded";
    public const string ActionDownloadCompleted = "DownloadCompleted";
    public const string ActionTaskCompleted = "TaskCompleted";
    public const string ActionDownloadFailed = "DownloadFailed";
    public const string ActionOpenDownloadedFile = "OpenDownloadedFile";
    public const string ActionOpenDownloadedFolder = "OpenDownloadedFolder";

    private bool _isRegistered;

    public event EventHandler<TaskNotificationInvokedEventArgs>? NotificationInvoked;

    public static bool IsOpenDownloadedAction(string action)
    {
        return action is ActionOpenDownloadedFile or ActionOpenDownloadedFolder;
    }

    public static bool TryParseActivationArguments(string? activationArguments, out TaskNotificationInvokedEventArgs notificationArgs)
    {
        notificationArgs = new TaskNotificationInvokedEventArgs(string.Empty, null, null);
        if (string.IsNullOrWhiteSpace(activationArguments))
        {
            return false;
        }

        activationArguments = activationArguments.Trim().Trim('"');

        if (Uri.TryCreate(activationArguments, UriKind.Absolute, out Uri? uri) &&
            uri.Scheme.Equals("omnidown", StringComparison.OrdinalIgnoreCase) &&
            uri.Host.Equals("notification", StringComparison.OrdinalIgnoreCase))
        {
            activationArguments = uri.Query.TrimStart('?');
        }

        Dictionary<string, string> arguments = new(StringComparer.OrdinalIgnoreCase);
        foreach (string part in activationArguments.Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            int separatorIndex = part.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            string key = WebUtility.UrlDecode(part[..separatorIndex]);
            string value = WebUtility.UrlDecode(part[(separatorIndex + 1)..]);
            if (!string.IsNullOrWhiteSpace(key))
            {
                arguments[key] = value;
            }
        }

        if (!arguments.TryGetValue("action", out string? action) || string.IsNullOrWhiteSpace(action))
        {
            return false;
        }

        arguments.TryGetValue("filePath", out string? filePath);
        arguments.TryGetValue("folderPath", out string? folderPath);
        arguments.TryGetValue("gid", out string? gid);
        notificationArgs = new TaskNotificationInvokedEventArgs(action, filePath, folderPath, gid);
        return true;
    }

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
            task.Gid,
            ResolveTaskFilePath(task),
            ResolveTaskFolderPath(task));
    }

    public void ShowTaskCompleted(DownloadTask task)
    {
        Show(
            Strings.Get("TaskCompletedNotificationTitle"),
            Strings.Format("TaskCompletedNotificationBody", GetTaskName(task)),
            ActionTaskCompleted,
            task.Gid,
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

    private static void Show(string title, string body, string action, string? gid = null, string? filePath = null, string? folderPath = null)
    {
        try
        {
            AppNotification notification = new(CreateProtocolNotificationPayload(title, body, action, gid, filePath, folderPath));
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

    private static string CreateProtocolNotificationPayload(
        string title,
        string body,
        string action,
        string? gid,
        string? filePath,
        string? folderPath)
    {
        string launchUri = CreateNotificationProtocolUri(action, gid, filePath, folderPath);
        string actions = action is ActionDownloadCompleted or ActionTaskCompleted
            ? $"""
                  <actions>
                    <action content="{XmlEncode(Strings.Get("TaskOpenFileActionText"))}" activationType="protocol" arguments="{XmlEncode(CreateNotificationProtocolUri(ActionOpenDownloadedFile, gid, filePath, folderPath))}" />
                    <action content="{XmlEncode(Strings.Get("TaskOpenFolderActionText"))}" activationType="protocol" arguments="{XmlEncode(CreateNotificationProtocolUri(ActionOpenDownloadedFolder, gid, filePath, folderPath))}" />
                  </actions>
              """
            : string.Empty;

        return $"""
            <toast activationType="protocol" launch="{XmlEncode(launchUri)}">
              <visual>
                <binding template="ToastGeneric">
                  <text>{XmlEncode(title)}</text>
                  <text>{XmlEncode(body)}</text>
                </binding>
              </visual>
              {actions}
            </toast>
            """;
    }

    public static string CreateActivationArguments(string action, string? gid = null, string? filePath = null, string? folderPath = null)
    {
        List<string> arguments =
        [
            $"action={Uri.EscapeDataString(action)}"
        ];

        if (!string.IsNullOrWhiteSpace(filePath))
        {
            arguments.Add($"filePath={Uri.EscapeDataString(filePath)}");
        }

        if (!string.IsNullOrWhiteSpace(folderPath))
        {
            arguments.Add($"folderPath={Uri.EscapeDataString(folderPath)}");
        }

        if (!string.IsNullOrWhiteSpace(gid))
        {
            arguments.Add($"gid={Uri.EscapeDataString(gid)}");
        }

        return string.Join('&', arguments);
    }

    private static string CreateNotificationProtocolUri(string action, string? gid, string? filePath, string? folderPath)
    {
        return $"omnidown://notification?{CreateActivationArguments(action, gid, filePath, folderPath)}";
    }

    private static string XmlEncode(string value)
    {
        return SecurityElement.Escape(value) ?? string.Empty;
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

    private void OnNotificationInvoked(
        AppNotificationManager sender,
        AppNotificationActivatedEventArgs args)
    {
        if (TryParseActivationArguments(args.Argument, out TaskNotificationInvokedEventArgs notificationArgs))
        {
            NotificationInvoked?.Invoke(this, notificationArgs);
            return;
        }

        args.Arguments.TryGetValue("action", out string? action);
        if (string.IsNullOrWhiteSpace(action))
        {
            return;
        }

        args.Arguments.TryGetValue("filePath", out string? filePath);
        args.Arguments.TryGetValue("folderPath", out string? folderPath);
        args.Arguments.TryGetValue("gid", out string? gid);
        NotificationInvoked?.Invoke(this, new TaskNotificationInvokedEventArgs(action, filePath, folderPath, gid));
    }

}

public sealed record TaskNotificationInvokedEventArgs(
    string Action,
    string? FilePath,
    string? FolderPath,
    string? Gid = null);
