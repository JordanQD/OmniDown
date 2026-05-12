namespace OmniDown.ViewModels;

using OmniDown.Models.Settings;
using OmniDown.Services.Settings;

internal sealed class SettingsPageViewModel
{
    private readonly AppSettingsStore _settingsStore;

    public SettingsPageViewModel(AppSettingsStore settingsStore)
    {
        _settingsStore = settingsStore;
    }

    public GeneralSettings GeneralSettings { get; private set; } = GeneralSettings.Default;

    public DownloadSettings DownloadSettings { get; private set; } = DownloadSettings.Default;

    public CloseBehaviorSettings CloseBehaviorSettings { get; private set; } = CloseBehaviorSettings.Default;

    public SpeedLimitSettings SpeedLimitSettings { get; private set; } = SpeedLimitSettings.Default;

    public void LoadGeneralSettings()
    {
        GeneralSettings = NormalizeGeneralSettings(_settingsStore.ReadGeneralSettings());
    }

    public void SaveGeneralSettings()
    {
        GeneralSettings = NormalizeGeneralSettings(GeneralSettings);
        _settingsStore.SaveGeneralSettings(GeneralSettings);
    }

    public void LoadDownloadSettings()
    {
        DownloadSettings = _settingsStore.ReadDownloadSettings();
    }

    public void SaveDownloadSettings(DownloadSettings settings)
    {
        DownloadSettings = settings;
        _settingsStore.SaveDownloadSettings(settings);
    }

    public void LoadCloseBehaviorSettings()
    {
        CloseBehaviorSettings = _settingsStore.ReadCloseBehaviorSettings();
    }

    public void SaveCloseBehaviorSettings()
    {
        _settingsStore.SaveCloseBehaviorSettings(CloseBehaviorSettings);
    }

    public void LoadSpeedLimitSettings()
    {
        SpeedLimitSettings = _settingsStore.ReadSpeedLimitSettings();
    }

    public void SaveSpeedLimitSettings(SpeedLimitSettings settings)
    {
        SpeedLimitSettings = settings;
        _settingsStore.SaveSpeedLimitSettings(settings);
    }

    public void UpdateCloseBehavior(bool minimizeToTrayOnClose)
    {
        CloseBehaviorSettings = CloseBehaviorSettings with
        {
            MinimizeToTrayOnClose = minimizeToTrayOnClose
        };
    }

    public void UpdateWindowPlacement(int x, int y, int width, int height)
    {
        GeneralSettings = GeneralSettings with
        {
            WindowX = x,
            WindowY = y,
            WindowWidth = width,
            WindowHeight = height
        };
    }

    public void UpdateGeneralSettings(
        bool restoreWindowPlacement,
        bool resumeDownloadsOnLaunch,
        bool autoClearCompletedOnExit,
        bool pauseActiveOnExit,
        bool showTaskbarProgress,
        bool systemNotificationsEnabled,
        bool downloadStartNotificationsEnabled,
        bool downloadCompleteNotificationsEnabled,
        string downloadCompleteNotificationAction,
        bool autoShutdownWhenComplete,
        bool preventSleepWhileDownloading,
        string theme)
    {
        GeneralSettings = GeneralSettings with
        {
            RestoreWindowPlacement = restoreWindowPlacement,
            ResumeDownloadsOnLaunch = resumeDownloadsOnLaunch,
            AutoClearCompletedOnExit = autoClearCompletedOnExit,
            PauseActiveOnExit = pauseActiveOnExit,
            ShowTaskbarProgress = showTaskbarProgress,
            SystemNotificationsEnabled = systemNotificationsEnabled,
            DownloadStartNotificationsEnabled = downloadStartNotificationsEnabled,
            DownloadCompleteNotificationsEnabled = downloadCompleteNotificationsEnabled,
            DownloadCompleteNotificationAction = downloadCompleteNotificationAction,
            AutoShutdownWhenComplete = autoShutdownWhenComplete,
            PreventSleepWhileDownloading = preventSleepWhileDownloading,
            Theme = theme
        };
    }

    private static GeneralSettings NormalizeGeneralSettings(GeneralSettings settings)
    {
        string notificationAction = settings.DownloadCompleteNotificationAction;
        if (notificationAction is not ("Home" or "OpenFile" or "OpenFolder"))
        {
            notificationAction = GeneralSettings.Default.DownloadCompleteNotificationAction;
        }

        return settings with
        {
            DownloadCompleteNotificationAction = notificationAction,
            Theme = string.IsNullOrWhiteSpace(settings.Theme)
                ? GeneralSettings.Default.Theme
                : settings.Theme
        };
    }
}
