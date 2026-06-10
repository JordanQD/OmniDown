using OmniDown.Models.Settings;
using OmniDown.Services.Settings;
using System;

namespace OmniDown.ViewModels;

internal sealed class SettingsPageViewModel
{
    private readonly AppSettingsStore _settingsStore;

    public SettingsPageViewModel(AppSettingsStore settingsStore)
    {
        _settingsStore = settingsStore;
    }

    // ── Settings state ──

    public GeneralSettings GeneralSettings { get; private set; } = GeneralSettings.Default;

    public DownloadSettings DownloadSettings { get; private set; } = DownloadSettings.Default;

    public BitTorrentSettings BitTorrentSettings { get; private set; } = BitTorrentSettings.Default;

    public NetworkSettings NetworkSettings { get; private set; } = NetworkSettings.Default;

    public AdvancedSettings AdvancedSettings { get; private set; } = AdvancedSettings.Default;

    public CloseBehaviorSettings CloseBehaviorSettings { get; private set; } = CloseBehaviorSettings.Default;

    public SpeedLimitSettings SpeedLimitSettings { get; private set; } = SpeedLimitSettings.Default;

    // ── Dirty tracking ──

    /// <summary>
    /// True when any settings section has unsaved changes.
    /// Each settings Page sets this to true on user edits, and the host
    /// resets it to false after saving.
    /// </summary>
    public bool HasPendingChanges { get; set; }

    /// <summary>
    /// Reset dirty flag after a successful save.
    /// </summary>
    public void ClearPendingChanges()
    {
        HasPendingChanges = false;
    }

    /// <summary>
    /// Mark that the user has made an edit that needs saving.
    /// </summary>
    public void MarkPendingChanges()
    {
        HasPendingChanges = true;
    }

    // ── Load ──

    public void LoadAll()
    {
        LoadGeneralSettings();
        LoadDownloadSettings();
        LoadBitTorrentSettings();
        LoadNetworkSettings();
        LoadAdvancedSettings();
        LoadCloseBehaviorSettings();
        LoadSpeedLimitSettings();
    }

    public void LoadGeneralSettings()
    {
        GeneralSettings = NormalizeGeneralSettings(_settingsStore.ReadGeneralSettings());
    }

    public void LoadDownloadSettings()
    {
        DownloadSettings = _settingsStore.ReadDownloadSettings();
    }

    public void LoadBitTorrentSettings()
    {
        BitTorrentSettings = _settingsStore.ReadBitTorrentSettings();
    }

    public void LoadNetworkSettings()
    {
        NetworkSettings = _settingsStore.ReadNetworkSettings();
    }

    public void LoadAdvancedSettings()
    {
        AdvancedSettings = NormalizeAdvancedSettings(_settingsStore.ReadAdvancedSettings());
        _settingsStore.SaveAdvancedSettings(AdvancedSettings);
    }

    public void LoadCloseBehaviorSettings()
    {
        CloseBehaviorSettings = _settingsStore.ReadCloseBehaviorSettings();
    }

    public void LoadSpeedLimitSettings()
    {
        SpeedLimitSettings = _settingsStore.ReadSpeedLimitSettings();
    }

    // ── Save ──

    public void SaveGeneralSettings()
    {
        GeneralSettings = NormalizeGeneralSettings(GeneralSettings);
        _settingsStore.SaveGeneralSettings(GeneralSettings);
    }

    public void SaveDownloadSettings()
    {
        _settingsStore.SaveDownloadSettings(DownloadSettings);
    }

    public void SaveDownloadSettings(DownloadSettings settings)
    {
        DownloadSettings = settings;
        _settingsStore.SaveDownloadSettings(settings);
    }

    public void SaveBitTorrentSettings()
    {
        _settingsStore.SaveBitTorrentSettings(BitTorrentSettings);
    }

    public void SaveBitTorrentSettings(BitTorrentSettings settings)
    {
        BitTorrentSettings = settings;
        _settingsStore.SaveBitTorrentSettings(settings);
    }

    public void SaveNetworkSettings()
    {
        _settingsStore.SaveNetworkSettings(NetworkSettings);
    }

    public void SaveNetworkSettings(NetworkSettings settings)
    {
        NetworkSettings = settings;
        _settingsStore.SaveNetworkSettings(settings);
    }

    public void SaveAdvancedSettings()
    {
        AdvancedSettings = NormalizeAdvancedSettings(AdvancedSettings);
        _settingsStore.SaveAdvancedSettings(AdvancedSettings);
    }

    public void SaveAdvancedSettings(AdvancedSettings settings)
    {
        AdvancedSettings = NormalizeAdvancedSettings(settings);
        _settingsStore.SaveAdvancedSettings(AdvancedSettings);
    }

    public void SaveCloseBehaviorSettings()
    {
        _settingsStore.SaveCloseBehaviorSettings(CloseBehaviorSettings);
    }

    public void SaveSpeedLimitSettings()
    {
        _settingsStore.SaveSpeedLimitSettings(SpeedLimitSettings);
    }

    public void SaveSpeedLimitSettings(SpeedLimitSettings settings)
    {
        SpeedLimitSettings = settings;
        _settingsStore.SaveSpeedLimitSettings(settings);
    }

    // ── Save all pending sections ──

    public void SaveAll()
    {
        SaveGeneralSettings();
        SaveDownloadSettings();
        SaveBitTorrentSettings();
        SaveNetworkSettings();
        SaveAdvancedSettings();
        SaveCloseBehaviorSettings();
        SaveSpeedLimitSettings();
        ClearPendingChanges();
    }

    // ── Per-section updates (called by settings Pages) ──

    public void UpdateGeneralSettings(GeneralSettings settings)
    {
        GeneralSettings = NormalizeGeneralSettings(settings);
        MarkPendingChanges();
    }

    public void UpdateDownloadSettings(DownloadSettings settings)
    {
        DownloadSettings = settings;
        MarkPendingChanges();
    }

    public void UpdateBitTorrentSettings(BitTorrentSettings settings)
    {
        BitTorrentSettings = settings;
        MarkPendingChanges();
    }

    public void UpdateNetworkSettings(NetworkSettings settings)
    {
        NetworkSettings = settings;
        MarkPendingChanges();
    }

    public void UpdateAdvancedSettings(AdvancedSettings settings)
    {
        AdvancedSettings = NormalizeAdvancedSettings(settings);
        MarkPendingChanges();
    }

    public void UpdateSpeedLimitSettings(SpeedLimitSettings settings)
    {
        SpeedLimitSettings = settings;
        MarkPendingChanges();
    }

    // ── Fine-grained general settings update (kept for MainWindow compatibility) ──

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
        MarkPendingChanges();
    }

    // ── Close behavior ──

    public void UpdateCloseBehavior(bool minimizeToTrayOnClose)
    {
        CloseBehaviorSettings = CloseBehaviorSettings with
        {
            MinimizeToTrayOnClose = minimizeToTrayOnClose
        };
        MarkPendingChanges();
    }

    // ── Window placement (saved separately, does not trigger pending-changes UI) ──

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

    // ── Normalization helpers ──

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

        Aria2EngineType engineType = settings.EngineType is Aria2EngineType.Aria2c or Aria2EngineType.Aria2Next or Aria2EngineType.Custom
            ? settings.EngineType
            : AdvancedSettings.Default.EngineType;

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
            Aria2Path = settings.Aria2Path?.Trim() ?? string.Empty,
            EngineType = engineType
        };
    }
}
