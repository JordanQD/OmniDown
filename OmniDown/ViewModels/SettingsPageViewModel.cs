namespace OmniDown.ViewModels;

using OmniDown.Models.Settings;
using OmniDown.Services.Settings;
using System;

internal sealed class SettingsPageViewModel
{
    private readonly AppSettingsStore _settingsStore;

    public SettingsPageViewModel(AppSettingsStore settingsStore)
    {
        _settingsStore = settingsStore;
    }

    public GeneralSettings GeneralSettings { get; private set; } = GeneralSettings.Default;

    public DownloadSettings DownloadSettings { get; private set; } = DownloadSettings.Default;

    public BitTorrentSettings BitTorrentSettings { get; private set; } = BitTorrentSettings.Default;

    public NetworkSettings NetworkSettings { get; private set; } = NetworkSettings.Default;

    public AdvancedSettings AdvancedSettings { get; private set; } = AdvancedSettings.Default;

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

    public void LoadBitTorrentSettings()
    {
        BitTorrentSettings = _settingsStore.ReadBitTorrentSettings();
    }

    public void SaveBitTorrentSettings(BitTorrentSettings settings)
    {
        BitTorrentSettings = settings;
        _settingsStore.SaveBitTorrentSettings(settings);
    }

    public void LoadNetworkSettings()
    {
        NetworkSettings = _settingsStore.ReadNetworkSettings();
    }

    public void SaveNetworkSettings(NetworkSettings settings)
    {
        NetworkSettings = settings;
        _settingsStore.SaveNetworkSettings(settings);
    }

    public void LoadAdvancedSettings()
    {
        AdvancedSettings = NormalizeAdvancedSettings(_settingsStore.ReadAdvancedSettings());
        _settingsStore.SaveAdvancedSettings(AdvancedSettings);
    }

    public void SaveAdvancedSettings(AdvancedSettings settings)
    {
        AdvancedSettings = NormalizeAdvancedSettings(settings);
        _settingsStore.SaveAdvancedSettings(AdvancedSettings);
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
            AutoShutdownWhenComplete = autoShutdownWhenComplete,
            PreventSleepWhileDownloading = preventSleepWhileDownloading,
            Theme = theme
        };
    }

    private static GeneralSettings NormalizeGeneralSettings(GeneralSettings settings)
    {
        return settings with
        {
            Theme = string.IsNullOrWhiteSpace(settings.Theme)
                ? GeneralSettings.Default.Theme
                : settings.Theme
        };
    }

    private static AdvancedSettings NormalizeAdvancedSettings(AdvancedSettings settings)
    {
        string logLevel = settings.LogLevel?.Trim().ToLowerInvariant() ?? string.Empty;
        if (logLevel is not ("debug" or "info" or "notice" or "warn" or "error"))
        {
            logLevel = AdvancedSettings.Default.LogLevel;
        }

        return settings with
        {
            RpcPort = Math.Clamp(settings.RpcPort, 1024, 65535),
            RpcSecret = string.IsNullOrWhiteSpace(settings.RpcSecret)
                ? AdvancedSettings.GenerateSecret()
                : settings.RpcSecret.Trim(),
            ExtensionApiPort = Math.Clamp(settings.ExtensionApiPort, 1024, 65535),
            ExtensionApiSecret = string.IsNullOrWhiteSpace(settings.ExtensionApiSecret)
                ? AdvancedSettings.GenerateSecret()
                : settings.ExtensionApiSecret.Trim(),
            LogLevel = logLevel,
            Aria2Path = settings.Aria2Path?.Trim() ?? string.Empty
        };
    }
}
