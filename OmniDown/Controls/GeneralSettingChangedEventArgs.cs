using System;

namespace OmniDown.Controls;

internal enum GeneralSettingChangeKind
{
    AutoStart,
    RestoreWindowPlacement,
    ResumeDownloadsOnLaunch,
    AutoClearCompletedOnExit,
    PauseActiveOnExit,
    ShowTaskbarProgress,
    SystemNotifications,
    DownloadStartNotifications,
    DownloadCompleteNotifications,
    AutoShutdownWhenComplete,
    PreventSleepWhileDownloading,
    Theme
}

internal sealed class GeneralSettingChangedEventArgs(GeneralSettingChangeKind kind) : EventArgs
{
    public GeneralSettingChangeKind Kind { get; } = kind;
}

internal sealed class CloseBehaviorSettingChangedEventArgs(bool minimizeToTrayOnClose) : EventArgs
{
    public bool MinimizeToTrayOnClose { get; } = minimizeToTrayOnClose;
}
