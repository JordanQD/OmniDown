namespace OmniDown.Models.Settings;

internal sealed record GeneralSettings(
    bool RestoreWindowPlacement,
    bool ResumeDownloadsOnLaunch,
    bool AutoClearCompletedOnExit,
    bool PauseActiveOnExit,
    bool ShowTaskbarProgress,
    bool SystemNotificationsEnabled,
    bool DownloadStartNotificationsEnabled,
    bool DownloadCompleteNotificationsEnabled,
    bool AutoShutdownWhenComplete,
    bool PreventSleepWhileDownloading,
    string Theme,
    int WindowX,
    int WindowY,
    int WindowWidth,
    int WindowHeight)
{
    public static GeneralSettings Default { get; } = new(
        RestoreWindowPlacement: false,
        ResumeDownloadsOnLaunch: false,
        AutoClearCompletedOnExit: false,
        PauseActiveOnExit: true,
        ShowTaskbarProgress: true,
        SystemNotificationsEnabled: true,
        DownloadStartNotificationsEnabled: false,
        DownloadCompleteNotificationsEnabled: true,
        AutoShutdownWhenComplete: false,
        PreventSleepWhileDownloading: false,
        Theme: "Default",
        WindowX: 0,
        WindowY: 0,
        WindowWidth: 0,
        WindowHeight: 0);
}
