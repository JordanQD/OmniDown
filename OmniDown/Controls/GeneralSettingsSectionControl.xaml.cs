using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OmniDown.Models.Settings;
using OmniDown.Services.Localization;
using System;
using System.Collections.Generic;

namespace OmniDown.Controls;

public sealed partial class GeneralSettingsSectionControl : UserControl
{
    private bool _isApplyingSettings;

    public GeneralSettingsSectionControl()
    {
        InitializeComponent();
    }

    internal IEnumerable<SettingSearchEntry> SearchEntries =>
    [
        new(StartupSettingCard, "startup", "launch", "engine", "aria2", "启动", "引擎"),
        new(RestoreWindowSettingCard, "restore", "window", "position", "size", "启动", "窗口", "位置", "大小"),
        new(ResumeOnLaunchSettingCard, "resume", "restore", "download", "launch", "恢复", "下载", "启动"),
        new(ExitCleanupSettingCard, "clear", "completed", "exit", "cleanup", "清理", "完成", "退出"),
        new(PauseActiveOnExitSettingCard, "pause", "active", "downloading", "exit", "暂停", "下载", "退出"),
        new(CloseBehaviorSettingCard, "close", "tray", "background", "exit", "关闭", "托盘", "后台", "退出"),
        new(TaskbarProgressSettingCard, "taskbar", "progress", "download", "任务栏", "进度"),
        new(ThemeSettingCard, "theme", "appearance", "system", "dark", "light", "主题", "外观"),
        new(NotificationsSettingCard, "notification", "complete", "failed", "通知"),
        new(DownloadStartNotificationSettingCard, "notification", "start", "download", "开始", "通知"),
        new(DownloadCompleteNotificationSettingCard, "notification", "complete", "failed", "download", "完成", "失败", "通知"),
        new(AutoShutdownSettingCard, "shutdown", "complete", "download", "关机", "完成"),
        new(PreventSleepSettingCard, "sleep", "power", "download", "休眠", "睡眠", "下载")
    ];

    internal event EventHandler<GeneralSettingChangedEventArgs>? GeneralSettingChanged;
    internal event EventHandler<CloseBehaviorSettingChangedEventArgs>? CloseBehaviorSettingChanged;

    internal void ApplyGeneralSettings(GeneralSettings settings, bool isAutoStartEnabled)
    {
        _isApplyingSettings = true;
        try
        {
            SetToggleSwitch(AutoStartToggleSwitch, isAutoStartEnabled);
            SetToggleSwitch(RestoreWindowPlacementToggleSwitch, settings.RestoreWindowPlacement);
            SetToggleSwitch(ResumeDownloadsOnLaunchToggleSwitch, settings.ResumeDownloadsOnLaunch);
            SetToggleSwitch(AutoClearCompletedOnExitToggleSwitch, settings.AutoClearCompletedOnExit);
            SetToggleSwitch(PauseActiveOnExitToggleSwitch, settings.PauseActiveOnExit);
            SetToggleSwitch(ShowTaskbarProgressToggleSwitch, settings.ShowTaskbarProgress);
            SetToggleSwitch(SystemNotificationsToggleSwitch, settings.SystemNotificationsEnabled);
            SetToggleSwitch(DownloadStartNotificationsToggleSwitch, settings.DownloadStartNotificationsEnabled);
            SetToggleSwitch(DownloadCompleteNotificationsToggleSwitch, settings.DownloadCompleteNotificationsEnabled);
            SetToggleSwitch(AutoShutdownWhenCompleteToggleSwitch, settings.AutoShutdownWhenComplete);
            SetToggleSwitch(PreventSleepWhileDownloadingToggleSwitch, settings.PreventSleepWhileDownloading);
            SetThemeComboBoxSelection(settings.Theme);
        }
        finally
        {
            _isApplyingSettings = false;
        }
    }

    internal GeneralSettings GetGeneralSettings(GeneralSettings currentSettings)
    {
        return currentSettings with
        {
            RestoreWindowPlacement = RestoreWindowPlacementToggleSwitch?.IsOn == true,
            ResumeDownloadsOnLaunch = ResumeDownloadsOnLaunchToggleSwitch?.IsOn == true,
            AutoClearCompletedOnExit = AutoClearCompletedOnExitToggleSwitch?.IsOn == true,
            PauseActiveOnExit = PauseActiveOnExitToggleSwitch?.IsOn == true,
            ShowTaskbarProgress = ShowTaskbarProgressToggleSwitch?.IsOn == true,
            SystemNotificationsEnabled = SystemNotificationsToggleSwitch?.IsOn == true,
            DownloadStartNotificationsEnabled = DownloadStartNotificationsToggleSwitch?.IsOn == true,
            DownloadCompleteNotificationsEnabled = DownloadCompleteNotificationsToggleSwitch?.IsOn == true,
            AutoShutdownWhenComplete = AutoShutdownWhenCompleteToggleSwitch?.IsOn == true,
            PreventSleepWhileDownloading = PreventSleepWhileDownloadingToggleSwitch?.IsOn == true,
            Theme = GetSelectedTheme()
        };
    }

    internal void ApplyCloseBehaviorSettings(CloseBehaviorSettings settings)
    {
        _isApplyingSettings = true;
        try
        {
            SetToggleSwitch(CloseToTrayToggleSwitch, settings.MinimizeToTrayOnClose == true);
        }
        finally
        {
            _isApplyingSettings = false;
        }
    }

    internal void SetAutoStartEnabled(bool isEnabled)
    {
        _isApplyingSettings = true;
        try
        {
            SetToggleSwitch(AutoStartToggleSwitch, isEnabled);
        }
        finally
        {
            _isApplyingSettings = false;
        }
    }

    internal bool IsAutoStartEnabled => AutoStartToggleSwitch?.IsOn == true;

    private void SettingToggleSwitch_Toggled(object sender, RoutedEventArgs args)
    {
        if (sender is not ToggleSwitch toggleSwitch)
        {
            return;
        }

        UpdateToggleStateText(toggleSwitch);
        if (_isApplyingSettings)
        {
            return;
        }

        if (TryGetGeneralSettingChangeKind(toggleSwitch, out GeneralSettingChangeKind kind))
        {
            GeneralSettingChanged?.Invoke(this, new GeneralSettingChangedEventArgs(kind));
            return;
        }

        if (ReferenceEquals(toggleSwitch, CloseToTrayToggleSwitch))
        {
            CloseBehaviorSettingChanged?.Invoke(this, new CloseBehaviorSettingChangedEventArgs(toggleSwitch.IsOn));
        }
    }

    private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        if (_isApplyingSettings)
        {
            return;
        }

        GeneralSettingChanged?.Invoke(this, new GeneralSettingChangedEventArgs(GeneralSettingChangeKind.Theme));
    }

    private void SetToggleSwitch(ToggleSwitch? toggleSwitch, bool isOn)
    {
        if (toggleSwitch is null)
        {
            return;
        }

        toggleSwitch.IsOn = isOn;
        UpdateToggleStateText(toggleSwitch);
    }

    private void UpdateToggleStateText(ToggleSwitch? toggleSwitch)
    {
        if (toggleSwitch is null)
        {
            return;
        }

        TextBlock? stateText = GetToggleStateText(toggleSwitch);
        if (stateText is not null)
        {
            stateText.Text = toggleSwitch.IsOn ? Strings.Get("ToggleOnState.Text") : Strings.Get("ToggleOffState.Text");
        }
    }

    private TextBlock? GetToggleStateText(ToggleSwitch toggleSwitch)
    {
        if (ReferenceEquals(toggleSwitch, AutoStartToggleSwitch)) return AutoStartStateText;
        if (ReferenceEquals(toggleSwitch, RestoreWindowPlacementToggleSwitch)) return RestoreWindowPlacementStateText;
        if (ReferenceEquals(toggleSwitch, ResumeDownloadsOnLaunchToggleSwitch)) return ResumeDownloadsOnLaunchStateText;
        if (ReferenceEquals(toggleSwitch, AutoClearCompletedOnExitToggleSwitch)) return AutoClearCompletedOnExitStateText;
        if (ReferenceEquals(toggleSwitch, PauseActiveOnExitToggleSwitch)) return PauseActiveOnExitStateText;
        if (ReferenceEquals(toggleSwitch, CloseToTrayToggleSwitch)) return CloseToTrayStateText;
        if (ReferenceEquals(toggleSwitch, ShowTaskbarProgressToggleSwitch)) return ShowTaskbarProgressStateText;
        if (ReferenceEquals(toggleSwitch, SystemNotificationsToggleSwitch)) return SystemNotificationsStateText;
        if (ReferenceEquals(toggleSwitch, DownloadStartNotificationsToggleSwitch)) return DownloadStartNotificationsStateText;
        if (ReferenceEquals(toggleSwitch, DownloadCompleteNotificationsToggleSwitch)) return DownloadCompleteNotificationsStateText;
        if (ReferenceEquals(toggleSwitch, AutoShutdownWhenCompleteToggleSwitch)) return AutoShutdownWhenCompleteStateText;
        if (ReferenceEquals(toggleSwitch, PreventSleepWhileDownloadingToggleSwitch)) return PreventSleepWhileDownloadingStateText;

        return null;
    }

    private bool TryGetGeneralSettingChangeKind(ToggleSwitch toggleSwitch, out GeneralSettingChangeKind kind)
    {
        if (ReferenceEquals(toggleSwitch, AutoStartToggleSwitch)) kind = GeneralSettingChangeKind.AutoStart;
        else if (ReferenceEquals(toggleSwitch, RestoreWindowPlacementToggleSwitch)) kind = GeneralSettingChangeKind.RestoreWindowPlacement;
        else if (ReferenceEquals(toggleSwitch, ResumeDownloadsOnLaunchToggleSwitch)) kind = GeneralSettingChangeKind.ResumeDownloadsOnLaunch;
        else if (ReferenceEquals(toggleSwitch, AutoClearCompletedOnExitToggleSwitch)) kind = GeneralSettingChangeKind.AutoClearCompletedOnExit;
        else if (ReferenceEquals(toggleSwitch, PauseActiveOnExitToggleSwitch)) kind = GeneralSettingChangeKind.PauseActiveOnExit;
        else if (ReferenceEquals(toggleSwitch, ShowTaskbarProgressToggleSwitch)) kind = GeneralSettingChangeKind.ShowTaskbarProgress;
        else if (ReferenceEquals(toggleSwitch, SystemNotificationsToggleSwitch)) kind = GeneralSettingChangeKind.SystemNotifications;
        else if (ReferenceEquals(toggleSwitch, DownloadStartNotificationsToggleSwitch)) kind = GeneralSettingChangeKind.DownloadStartNotifications;
        else if (ReferenceEquals(toggleSwitch, DownloadCompleteNotificationsToggleSwitch)) kind = GeneralSettingChangeKind.DownloadCompleteNotifications;
        else if (ReferenceEquals(toggleSwitch, AutoShutdownWhenCompleteToggleSwitch)) kind = GeneralSettingChangeKind.AutoShutdownWhenComplete;
        else if (ReferenceEquals(toggleSwitch, PreventSleepWhileDownloadingToggleSwitch)) kind = GeneralSettingChangeKind.PreventSleepWhileDownloading;
        else
        {
            kind = default;
            return false;
        }

        return true;
    }

    private string GetSelectedTheme()
    {
        return ThemeComboBox?.SelectedItem is ComboBoxItem item &&
            item.Tag?.ToString() is string theme &&
            !string.IsNullOrWhiteSpace(theme)
            ? theme
            : "Default";
    }

    private void SetThemeComboBoxSelection(string theme)
    {
        if (ThemeComboBox is null)
        {
            return;
        }

        for (int index = 0; index < ThemeComboBox.Items.Count; index++)
        {
            if (ThemeComboBox.Items[index] is ComboBoxItem item &&
                item.Tag?.ToString()?.Equals(theme, StringComparison.OrdinalIgnoreCase) == true)
            {
                ThemeComboBox.SelectedIndex = index;
                return;
            }
        }

        ThemeComboBox.SelectedIndex = 0;
    }
}
