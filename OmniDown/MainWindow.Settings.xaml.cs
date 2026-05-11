using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using OmniDown.Models;
using OmniDown.Models.Settings;
using OmniDown.Services.Downloads;
using OmniDown.Services.Engine;
using OmniDown.Services.Localization;
using OmniDown.Services.Notifications;
using OmniDown.Services.Rpc;
using OmniDown.Services.Settings;
using OmniDown.Services.Shell;
using OmniDown.Services.Storage;
using OmniDown.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI;
using WinRT.Interop;

namespace OmniDown
{
    public sealed partial class MainWindow
    {
        private ListView SettingsSectionListView => SettingsPage.SettingsSectionListViewControl;
        private StackPanel GeneralSettingsContent => SettingsPage.GeneralSettingsContentControl;
        private StackPanel DownloadSettingsContent => SettingsPage.DownloadSettingsContentControl;
        private StackPanel BitTorrentSettingsContent => SettingsPage.BitTorrentSettingsContentControl;
        private StackPanel NetworkSettingsContent => SettingsPage.NetworkSettingsContentControl;
        private StackPanel AdvancedSettingsContent => SettingsPage.AdvancedSettingsContentControl;
        private StackPanel AboutSettingsContent => SettingsPage.AboutSettingsContentControl;
        private Border StartupSettingCard => SettingsPage.StartupSettingCardControl;
        private Border RestoreWindowSettingCard => SettingsPage.RestoreWindowSettingCardControl;
        private Border ResumeOnLaunchSettingCard => SettingsPage.ResumeOnLaunchSettingCardControl;
        private Border ExitCleanupSettingCard => SettingsPage.ExitCleanupSettingCardControl;
        private Border PauseActiveOnExitSettingCard => SettingsPage.PauseActiveOnExitSettingCardControl;
        private Border CloseBehaviorSettingCard => SettingsPage.CloseBehaviorSettingCardControl;
        private Border TaskbarProgressSettingCard => SettingsPage.TaskbarProgressSettingCardControl;
        private Border ThemeSettingCard => SettingsPage.ThemeSettingCardControl;
        private Border NotificationsSettingCard => SettingsPage.NotificationsSettingCardControl;
        private Border DownloadStartNotificationSettingCard => SettingsPage.DownloadStartNotificationSettingCardControl;
        private Border DownloadCompleteNotificationSettingCard => SettingsPage.DownloadCompleteNotificationSettingCardControl;
        private Border AutoShutdownSettingCard => SettingsPage.AutoShutdownSettingCardControl;
        private Border PreventSleepSettingCard => SettingsPage.PreventSleepSettingCardControl;
        private Border DefaultDirectorySettingCard => SettingsPage.DefaultDirectorySettingCardControl;
        private Border SplitCountSettingCard => SettingsPage.SplitCountSettingCardControl;
        private Border SpeedLimitSettingCard => SettingsPage.SpeedLimitSettingCardControl;
        private Border BtEnableSettingCard => SettingsPage.BtEnableSettingCardControl;
        private Border BtPortSettingCard => SettingsPage.BtPortSettingCardControl;
        private Border BtSeedRatioSettingCard => SettingsPage.BtSeedRatioSettingCardControl;
        private Border UseSystemProxySettingCard => SettingsPage.UseSystemProxySettingCardControl;
        private Border CustomProxySettingCard => SettingsPage.CustomProxySettingCardControl;
        private Border RetrySettingCard => SettingsPage.RetrySettingCardControl;
        private Border AriaPathSettingCard => SettingsPage.AriaPathSettingCardControl;
        private Border RpcPortSettingCard => SettingsPage.RpcPortSettingCardControl;
        private Border ProcessStatusSettingCard => SettingsPage.ProcessStatusSettingCardControl;
        private Border TerminalSettingCard => SettingsPage.TerminalSettingCardControl;
        private Border AboutAppCard => SettingsPage.AboutAppCardControl;
        private Border AboutCloneCard => SettingsPage.AboutCloneCardControl;
        private Border AboutIssueCard => SettingsPage.AboutIssueCardControl;
        private Border AboutReferencesCard => SettingsPage.AboutReferencesCardControl;
        private Border AboutLicenseCard => SettingsPage.AboutLicenseCardControl;
        private ToggleSwitch AutoStartToggleSwitch => SettingsPage.AutoStartToggleSwitchControl;
        private ToggleSwitch RestoreWindowPlacementToggleSwitch => SettingsPage.RestoreWindowPlacementToggleSwitchControl;
        private ToggleSwitch ResumeDownloadsOnLaunchToggleSwitch => SettingsPage.ResumeDownloadsOnLaunchToggleSwitchControl;
        private ToggleSwitch AutoClearCompletedOnExitToggleSwitch => SettingsPage.AutoClearCompletedOnExitToggleSwitchControl;
        private ToggleSwitch PauseActiveOnExitToggleSwitch => SettingsPage.PauseActiveOnExitToggleSwitchControl;
        private ToggleSwitch CloseToTrayToggleSwitch => SettingsPage.CloseToTrayToggleSwitchControl;
        private ToggleSwitch ShowTaskbarProgressToggleSwitch => SettingsPage.ShowTaskbarProgressToggleSwitchControl;
        private ComboBox ThemeComboBox => SettingsPage.ThemeComboBoxControl;
        private ToggleSwitch SystemNotificationsToggleSwitch => SettingsPage.SystemNotificationsToggleSwitchControl;
        private ToggleSwitch DownloadStartNotificationsToggleSwitch => SettingsPage.DownloadStartNotificationsToggleSwitchControl;
        private ToggleSwitch DownloadCompleteNotificationsToggleSwitch => SettingsPage.DownloadCompleteNotificationsToggleSwitchControl;
        private ToggleSwitch AutoShutdownWhenCompleteToggleSwitch => SettingsPage.AutoShutdownWhenCompleteToggleSwitchControl;
        private ToggleSwitch PreventSleepWhileDownloadingToggleSwitch => SettingsPage.PreventSleepWhileDownloadingToggleSwitchControl;
        private TextBox DownloadDirectoryTextBox => SettingsPage.DownloadDirectoryTextBoxControl;
        private ToggleSwitch UseSystemProxyCheckBox => SettingsPage.UseSystemProxyCheckBoxControl;
        private TextBox AriaPathTextBox => SettingsPage.AriaPathTextBoxControl;
        private NumberBox RpcPortNumberBox => SettingsPage.RpcPortNumberBoxControl;
        private TextBlock SettingsAriaStatusText => SettingsPage.SettingsAriaStatusTextControl;
        private StackPanel ProcessStatusSettingControl => SettingsPage.ProcessStatusSettingControlControl;
        private TextBlock AboutVersionText => SettingsPage.AboutVersionTextControl;
        private TextBlock AboutCloneCommandText => SettingsPage.AboutCloneCommandTextControl;

        private void HookSettingsPageEvents()
        {
            SettingsPage.SectionSelectionChanged += SettingsSectionListView_SelectionChanged;
            SettingsPage.SettingToggleSwitchToggled += SettingToggleSwitch_Toggled;
            SettingsPage.ThemeSelectionChanged += ThemeComboBox_SelectionChanged;
            SettingsPage.StartAriaRequested += StartAriaButton_Click;
            SettingsPage.StopAriaRequested += StopAriaButton_Click;
            SettingsPage.CopyCloneCommandRequested += CopyCloneCommandButton_Click;
            SettingsPage.OpenAboutLinkRequested += OpenAboutLinkButton_Click;
        }

        private void UseSystemProxyCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            UpdateDebugStatus();
        }

        private void SettingToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (sender is not ToggleSwitch toggleSwitch ||
                toggleSwitch.Parent is not StackPanel panel)
            {
                return;
            }

            TextBlock? stateText = panel.Children.OfType<TextBlock>().FirstOrDefault();
            if (stateText is not null)
            {
                SetToggleStateText(stateText, toggleSwitch.IsOn);
            }

            if (ReferenceEquals(toggleSwitch, UseSystemProxyCheckBox))
            {
                UseSystemProxyCheckBox_Changed(sender, e);
            }

            if (ReferenceEquals(toggleSwitch, CloseToTrayToggleSwitch))
            {
                if (_isLoadingCloseBehaviorSettings)
                {
                    return;
                }

                _settingsPageViewModel.UpdateCloseBehavior(toggleSwitch.IsOn);
                SaveCloseBehaviorSettings();
            }

            if (_isLoadingGeneralSettings)
            {
                return;
            }

            if (IsGeneralSettingsToggle(toggleSwitch))
            {
                UpdateGeneralSettingsFromUi();
                SaveGeneralSettings();
                ApplyGeneralSettingsSideEffects(toggleSwitch);
            }
        }

        private async void ApplySpeedLimitButton_Click(object sender, RoutedEventArgs e)
        {
            _isDownloadSpeedLimitEnabled = DownloadLimitToggleSwitch.IsOn;
            _isUploadSpeedLimitEnabled = UploadLimitToggleSwitch.IsOn;
            _downloadLimitBytesPerSecond = _isDownloadSpeedLimitEnabled
                ? GetSpeedLimitBytesPerSecond(DownloadLimitNumberBox, GetSelectedSpeedLimitUnit(DownloadLimitUnitComboBox))
                : 0;
            _uploadLimitBytesPerSecond = _isUploadSpeedLimitEnabled
                ? GetSpeedLimitBytesPerSecond(UploadLimitNumberBox, GetSelectedSpeedLimitUnit(UploadLimitUnitComboBox))
                : 0;

            Aria2EngineStartResult startResult = await EnsureAria2StartedAsync();
            if (!startResult.Started)
            {
                ShowMessage(startResult.Message, InfoBarSeverity.Error);
                return;
            }

            try
            {
                await ApplyConfiguredSpeedLimitsAsync();
                SaveSpeedLimitSettings();
                UpdateGlobalSpeedLimitText();
                SpeedLimitButton.Flyout?.Hide();
                ShowMessage(Strings.Get("SpeedLimitAppliedMessage"), InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                ShowMessage(Strings.Format("SpeedLimitApplyFailedMessage", ex.Message), InfoBarSeverity.Error);
            }
        }

        private Task ApplyConfiguredSpeedLimitsAsync()
        {
            return _downloadCoordinator.SetGlobalSpeedLimitsAsync(
                _isDownloadSpeedLimitEnabled ? _downloadLimitBytesPerSecond : 0,
                _isUploadSpeedLimitEnabled ? _uploadLimitBytesPerSecond : 0);
        }

        private static long GetSpeedLimitBytesPerSecond(NumberBox numberBox, string unit)
        {
            if (numberBox is null || double.IsNaN(numberBox.Value))
            {
                return 0;
            }

            long multiplier = unit.Equals("MB/s", StringComparison.OrdinalIgnoreCase)
                ? 1024L * 1024L
                : 1024L;

            return Math.Max(1, (long)Math.Round(numberBox.Value)) * multiplier;
        }

        private void DownloadLimitToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            SetDownloadSpeedLimitInputsEnabled(DownloadLimitToggleSwitch.IsOn);
        }

        private void UploadLimitToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            SetUploadSpeedLimitInputsEnabled(UploadLimitToggleSwitch.IsOn);
        }

        private void SetDownloadSpeedLimitInputsEnabled(bool isEnabled)
        {
            if (DownloadLimitNumberBox is not null)
            {
                DownloadLimitNumberBox.IsEnabled = isEnabled;
            }

            if (DownloadLimitUnitComboBox is not null)
            {
                DownloadLimitUnitComboBox.IsEnabled = isEnabled;
            }
        }

        private void SetUploadSpeedLimitInputsEnabled(bool isEnabled)
        {
            if (UploadLimitNumberBox is not null)
            {
                UploadLimitNumberBox.IsEnabled = isEnabled;
            }

            if (UploadLimitUnitComboBox is not null)
            {
                UploadLimitUnitComboBox.IsEnabled = isEnabled;
            }
        }

        private static string GetSelectedSpeedLimitUnit(ComboBox comboBox)
        {
            return comboBox?.SelectedItem is ComboBoxItem item &&
                item.Content?.ToString() is string unit &&
                !string.IsNullOrWhiteSpace(unit)
                ? unit
                : "KB/s";
        }

        private void LoadSpeedLimitSettings()
        {
            _settingsPageViewModel.LoadSpeedLimitSettings();
            SpeedLimitSettings settings = _settingsPageViewModel.SpeedLimitSettings;

            SetSpeedLimitUnit(UploadLimitUnitComboBox, settings.UploadUnit);
            SetSpeedLimitUnit(DownloadLimitUnitComboBox, settings.DownloadUnit);
            UploadLimitNumberBox.Value = Math.Max(settings.UploadValue, 1);
            DownloadLimitNumberBox.Value = Math.Max(settings.DownloadValue, 1);

            _isUploadSpeedLimitEnabled = settings.UploadEnabled;
            _isDownloadSpeedLimitEnabled = settings.DownloadEnabled;
            _uploadLimitBytesPerSecond = settings.UploadEnabled
                ? GetSpeedLimitBytesPerSecond(UploadLimitNumberBox, GetSelectedSpeedLimitUnit(UploadLimitUnitComboBox))
                : 0;
            _downloadLimitBytesPerSecond = settings.DownloadEnabled
                ? GetSpeedLimitBytesPerSecond(DownloadLimitNumberBox, GetSelectedSpeedLimitUnit(DownloadLimitUnitComboBox))
                : 0;

            UploadLimitToggleSwitch.IsOn = settings.UploadEnabled;
            DownloadLimitToggleSwitch.IsOn = settings.DownloadEnabled;
            SetUploadSpeedLimitInputsEnabled(settings.UploadEnabled);
            SetDownloadSpeedLimitInputsEnabled(settings.DownloadEnabled);
        }

        private void LoadCloseBehaviorSettings()
        {
            _settingsPageViewModel.LoadCloseBehaviorSettings();
            _isLoadingCloseBehaviorSettings = true;
            try
            {
                ApplyCloseBehaviorSettingsToUi();
            }
            finally
            {
                _isLoadingCloseBehaviorSettings = false;
            }
        }

        private void LoadGeneralSettings()
        {
            _settingsPageViewModel.LoadGeneralSettings();
            _isLoadingGeneralSettings = true;
            try
            {
                ApplyGeneralSettingsToUi();
                ApplyThemeSetting(_settingsPageViewModel.GeneralSettings.Theme);
            }
            finally
            {
                _isLoadingGeneralSettings = false;
            }
        }

        private void ApplyGeneralSettingsToUi()
        {
            GeneralSettings settings = _settingsPageViewModel.GeneralSettings;
            SetToggleSwitch(AutoStartToggleSwitch, _autoStartService.IsEnabled());
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

        private void UpdateGeneralSettingsFromUi()
        {
            _settingsPageViewModel.UpdateGeneralSettings(
                RestoreWindowPlacementToggleSwitch?.IsOn == true,
                ResumeDownloadsOnLaunchToggleSwitch?.IsOn == true,
                AutoClearCompletedOnExitToggleSwitch?.IsOn == true,
                PauseActiveOnExitToggleSwitch?.IsOn == true,
                ShowTaskbarProgressToggleSwitch?.IsOn == true,
                SystemNotificationsToggleSwitch?.IsOn == true,
                DownloadStartNotificationsToggleSwitch?.IsOn == true,
                DownloadCompleteNotificationsToggleSwitch?.IsOn == true,
                AutoShutdownWhenCompleteToggleSwitch?.IsOn == true,
                PreventSleepWhileDownloadingToggleSwitch?.IsOn == true,
                GetSelectedTheme());
        }

        private void ApplyGeneralSettingsSideEffects(ToggleSwitch changedToggleSwitch)
        {
            if (ReferenceEquals(changedToggleSwitch, AutoStartToggleSwitch))
            {
                _ = SetAutoStartEnabledAsync(AutoStartToggleSwitch.IsOn);
            }

            if (ReferenceEquals(changedToggleSwitch, ShowTaskbarProgressToggleSwitch))
            {
                UpdateTaskbarProgressFromTasks();
            }

            if (ReferenceEquals(changedToggleSwitch, PreventSleepWhileDownloadingToggleSwitch))
            {
                UpdateSystemSleepOverride();
            }
        }

        private static bool IsGeneralSettingsToggle(ToggleSwitch toggleSwitch)
        {
            return toggleSwitch.Name is
                "AutoStartToggleSwitch" or
                "RestoreWindowPlacementToggleSwitch" or
                "ResumeDownloadsOnLaunchToggleSwitch" or
                "AutoClearCompletedOnExitToggleSwitch" or
                "PauseActiveOnExitToggleSwitch" or
                "ShowTaskbarProgressToggleSwitch" or
                "SystemNotificationsToggleSwitch" or
                "DownloadStartNotificationsToggleSwitch" or
                "DownloadCompleteNotificationsToggleSwitch" or
                "AutoShutdownWhenCompleteToggleSwitch" or
                "PreventSleepWhileDownloadingToggleSwitch";
        }

        private static void SetToggleSwitch(ToggleSwitch? toggleSwitch, bool isOn)
        {
            if (toggleSwitch is null)
            {
                return;
            }

            toggleSwitch.IsOn = isOn;
            if (toggleSwitch.Parent is StackPanel panel &&
                panel.Children.OfType<TextBlock>().FirstOrDefault() is TextBlock stateText)
            {
                SetToggleStateText(stateText, isOn);
            }
        }

        private void ApplyCloseBehaviorSettingsToUi()
        {
            if (CloseToTrayToggleSwitch is null)
            {
                return;
            }

            CloseToTrayToggleSwitch.IsOn = _settingsPageViewModel.CloseBehaviorSettings.MinimizeToTrayOnClose == true;
            if (CloseToTrayToggleSwitch.Parent is StackPanel panel &&
                panel.Children.OfType<TextBlock>().FirstOrDefault() is TextBlock stateText)
            {
                SetToggleStateText(stateText, CloseToTrayToggleSwitch.IsOn);
            }
        }

        private static void SetToggleStateText(TextBlock stateText, bool isOn)
        {
            stateText.Text = isOn ? "开" : "关";
        }

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoadingGeneralSettings)
            {
                return;
            }

            UpdateGeneralSettingsFromUi();
            ApplyThemeSetting(_settingsPageViewModel.GeneralSettings.Theme);
            SaveGeneralSettings();
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

        private void ApplyThemeSetting(string theme)
        {
            if (Content is not FrameworkElement root)
            {
                return;
            }

            root.RequestedTheme = theme.ToLowerInvariant() switch
            {
                "light" => ElementTheme.Light,
                "dark" => ElementTheme.Dark,
                _ => ElementTheme.Default
            };
        }

        private void SyncAutoStartToggle()
        {
            try
            {
                SetToggleSwitch(AutoStartToggleSwitch, _autoStartService.IsEnabled());
            }
            catch
            {
                SetToggleSwitch(AutoStartToggleSwitch, false);
            }
        }

        private async Task SetAutoStartEnabledAsync(bool isEnabled)
        {
            AutoStartUpdateResult result = await _autoStartService.SetEnabledAsync(isEnabled);
            SetToggleSwitch(AutoStartToggleSwitch, result.IsEnabled);
            if (result.RequiresUserPermission)
            {
                ShowMessage("自动启动未启用，请在 Windows 启动应用设置中允许 OmniDown。", InfoBarSeverity.Warning);
            }
        }

        private void SaveGeneralSettings()
        {
            _settingsPageViewModel.SaveGeneralSettings();
        }

        private void SaveSpeedLimitSettings()
        {
            SpeedLimitSettings settings = new(
                DownloadLimitToggleSwitch?.IsOn == true,
                GetValidNumberBoxValue(DownloadLimitNumberBox),
                GetSelectedSpeedLimitUnit(DownloadLimitUnitComboBox),
                UploadLimitToggleSwitch?.IsOn == true,
                GetValidNumberBoxValue(UploadLimitNumberBox),
                GetSelectedSpeedLimitUnit(UploadLimitUnitComboBox));

            _settingsPageViewModel.SaveSpeedLimitSettings(settings);
        }

        private void SaveCloseBehaviorSettings()
        {
            _settingsPageViewModel.SaveCloseBehaviorSettings();
        }

        private static double GetValidNumberBoxValue(NumberBox numberBox)
        {
            return numberBox is null || double.IsNaN(numberBox.Value) || numberBox.Value < 1
                ? 1
                : numberBox.Value;
        }

        private static void SetSpeedLimitUnit(ComboBox comboBox, string unit)
        {
            comboBox.SelectedIndex = unit.Equals("MB/s", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        }
    }
}
