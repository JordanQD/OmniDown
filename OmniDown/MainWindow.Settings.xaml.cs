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
        private Border DownloadCompleteNotificationActionSettingCard => SettingsPage.DownloadCompleteNotificationActionSettingCardControl;
        private Border AutoShutdownSettingCard => SettingsPage.AutoShutdownSettingCardControl;
        private Border PreventSleepSettingCard => SettingsPage.PreventSleepSettingCardControl;
        private Border DefaultDirectorySettingCard => SettingsPage.DefaultDirectorySettingCardControl;
        private Border MaxConcurrentDownloadsSettingCard => SettingsPage.MaxConcurrentDownloadsSettingCardControl;
        private Border SplitCountSettingCard => SettingsPage.SplitCountSettingCardControl;
        private Border MaxConnectionPerServerSettingCard => SettingsPage.MaxConnectionPerServerSettingCardControl;
        private Border ContinueDownloadSettingCard => SettingsPage.ContinueDownloadSettingCardControl;
        private Border RemoteTimeSettingCard => SettingsPage.RemoteTimeSettingCardControl;
        private Border MaxTriesSettingCard => SettingsPage.MaxTriesSettingCardControl;
        private Border RetryWaitSettingCard => SettingsPage.RetryWaitSettingCardControl;
        private Border DownloadCleanupSettingCard => SettingsPage.DownloadCleanupSettingCardControl;
        private Border TorrentCleanupSettingCard => SettingsPage.TorrentCleanupSettingCardControl;
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
        private TextBlock AutoStartStateText => SettingsPage.AutoStartStateTextControl;
        private ToggleSwitch RestoreWindowPlacementToggleSwitch => SettingsPage.RestoreWindowPlacementToggleSwitchControl;
        private TextBlock RestoreWindowPlacementStateText => SettingsPage.RestoreWindowPlacementStateTextControl;
        private ToggleSwitch ResumeDownloadsOnLaunchToggleSwitch => SettingsPage.ResumeDownloadsOnLaunchToggleSwitchControl;
        private TextBlock ResumeDownloadsOnLaunchStateText => SettingsPage.ResumeDownloadsOnLaunchStateTextControl;
        private ToggleSwitch AutoClearCompletedOnExitToggleSwitch => SettingsPage.AutoClearCompletedOnExitToggleSwitchControl;
        private TextBlock AutoClearCompletedOnExitStateText => SettingsPage.AutoClearCompletedOnExitStateTextControl;
        private ToggleSwitch PauseActiveOnExitToggleSwitch => SettingsPage.PauseActiveOnExitToggleSwitchControl;
        private TextBlock PauseActiveOnExitStateText => SettingsPage.PauseActiveOnExitStateTextControl;
        private ToggleSwitch CloseToTrayToggleSwitch => SettingsPage.CloseToTrayToggleSwitchControl;
        private TextBlock CloseToTrayStateText => SettingsPage.CloseToTrayStateTextControl;
        private ToggleSwitch ShowTaskbarProgressToggleSwitch => SettingsPage.ShowTaskbarProgressToggleSwitchControl;
        private TextBlock ShowTaskbarProgressStateText => SettingsPage.ShowTaskbarProgressStateTextControl;
        private ComboBox ThemeComboBox => SettingsPage.ThemeComboBoxControl;
        private ToggleSwitch SystemNotificationsToggleSwitch => SettingsPage.SystemNotificationsToggleSwitchControl;
        private TextBlock SystemNotificationsStateText => SettingsPage.SystemNotificationsStateTextControl;
        private ToggleSwitch DownloadStartNotificationsToggleSwitch => SettingsPage.DownloadStartNotificationsToggleSwitchControl;
        private TextBlock DownloadStartNotificationsStateText => SettingsPage.DownloadStartNotificationsStateTextControl;
        private ToggleSwitch DownloadCompleteNotificationsToggleSwitch => SettingsPage.DownloadCompleteNotificationsToggleSwitchControl;
        private TextBlock DownloadCompleteNotificationsStateText => SettingsPage.DownloadCompleteNotificationsStateTextControl;
        private ComboBox DownloadCompleteNotificationActionComboBox => SettingsPage.DownloadCompleteNotificationActionComboBoxControl;
        private ToggleSwitch AutoShutdownWhenCompleteToggleSwitch => SettingsPage.AutoShutdownWhenCompleteToggleSwitchControl;
        private TextBlock AutoShutdownWhenCompleteStateText => SettingsPage.AutoShutdownWhenCompleteStateTextControl;
        private ToggleSwitch PreventSleepWhileDownloadingToggleSwitch => SettingsPage.PreventSleepWhileDownloadingToggleSwitchControl;
        private TextBlock PreventSleepWhileDownloadingStateText => SettingsPage.PreventSleepWhileDownloadingStateTextControl;
        private TextBox DownloadDirectoryTextBox => SettingsPage.DownloadDirectoryTextBoxControl;
        private NumberBox MaxConcurrentDownloadsNumberBox => SettingsPage.MaxConcurrentDownloadsNumberBoxControl;
        private NumberBox SplitCountNumberBox => SettingsPage.SplitCountNumberBoxControl;
        private NumberBox MaxConnectionPerServerNumberBox => SettingsPage.MaxConnectionPerServerNumberBoxControl;
        private ToggleSwitch ContinueDownloadToggleSwitch => SettingsPage.ContinueDownloadToggleSwitchControl;
        private TextBlock ContinueDownloadStateText => SettingsPage.ContinueDownloadStateTextControl;
        private ComboBox RemoteTimeComboBox => SettingsPage.RemoteTimeComboBoxControl;
        private NumberBox MaxTriesNumberBox => SettingsPage.MaxTriesNumberBoxControl;
        private NumberBox RetryWaitNumberBox => SettingsPage.RetryWaitNumberBoxControl;
        private ToggleSwitch AutoDeleteStaleRecordsToggleSwitch => SettingsPage.AutoDeleteStaleRecordsToggleSwitchControl;
        private TextBlock AutoDeleteStaleRecordsStateText => SettingsPage.AutoDeleteStaleRecordsStateTextControl;
        private ToggleSwitch DeleteTorrentAfterCompleteToggleSwitch => SettingsPage.DeleteTorrentAfterCompleteToggleSwitchControl;
        private TextBlock DeleteTorrentAfterCompleteStateText => SettingsPage.DeleteTorrentAfterCompleteStateTextControl;
        private ToggleSwitch BtEnableToggleSwitch => SettingsPage.BtEnableToggleSwitchControl;
        private TextBlock BtEnableStateText => SettingsPage.BtEnableStateTextControl;
        private ToggleSwitch UseSystemProxyCheckBox => SettingsPage.UseSystemProxyCheckBoxControl;
        private TextBlock UseSystemProxyStateText => SettingsPage.UseSystemProxyStateTextControl;
        private ToggleSwitch CustomProxyToggleSwitch => SettingsPage.CustomProxyToggleSwitchControl;
        private TextBlock CustomProxyStateText => SettingsPage.CustomProxyStateTextControl;
        private ToggleSwitch TerminalOutputToggleSwitch => SettingsPage.TerminalOutputToggleSwitchControl;
        private TextBlock TerminalOutputStateText => SettingsPage.TerminalOutputStateTextControl;
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
            SettingsPage.NotificationActionSelectionChanged += NotificationActionComboBox_SelectionChanged;
            SettingsPage.BrowseDownloadDirectoryRequested += BrowseDownloadDirectoryButton_Click;
            SettingsPage.DownloadSettingChanged += DownloadSetting_Changed;
            SettingsPage.StartAriaRequested += StartAriaButton_Click;
            SettingsPage.StopAriaRequested += StopAriaButton_Click;
            SettingsPage.CopyCloneCommandRequested += CopyCloneCommandButton_Click;
            SettingsPage.OpenAboutLinkRequested += OpenAboutLinkButton_Click;
        }

        private void UseSystemProxyCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            UpdateDebugStatus();
        }

        private async void BrowseDownloadDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            FolderPicker picker = new()
            {
                SuggestedStartLocation = PickerLocationId.Downloads
            };
            picker.FileTypeFilter.Add("*");
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));

            StorageFolder? folder = await picker.PickSingleFolderAsync();
            if (folder is null)
            {
                return;
            }

            DownloadDirectoryTextBox.Text = folder.Path;
            SaveDownloadSettings();
        }

        private void DownloadSetting_Changed(object sender, RoutedEventArgs e)
        {
            if (_isLoadingDownloadSettings)
            {
                return;
            }

            SaveDownloadSettings();
        }

        private void SettingToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (sender is not ToggleSwitch toggleSwitch)
            {
                return;
            }

            UpdateToggleStateText(toggleSwitch);

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
            await ApplySpeedLimitAsync(true);
        }

        private async Task ApplySpeedLimitAsync(bool hideFlyout)
        {
            UpdateSpeedLimitStateFromToolbar();

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
                if (hideFlyout)
                {
                    SpeedLimitButton.Flyout?.Hide();
                }

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
            SetDownloadCompleteNotificationActionSelection(settings.DownloadCompleteNotificationAction);
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
                GetSelectedDownloadCompleteNotificationAction(),
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

        private void SetToggleSwitch(ToggleSwitch? toggleSwitch, bool isOn)
        {
            if (toggleSwitch is null)
            {
                return;
            }

            toggleSwitch.IsOn = isOn;
            UpdateToggleStateText(toggleSwitch);
        }

        private void ApplyCloseBehaviorSettingsToUi()
        {
            if (CloseToTrayToggleSwitch is null)
            {
                return;
            }

            CloseToTrayToggleSwitch.IsOn = _settingsPageViewModel.CloseBehaviorSettings.MinimizeToTrayOnClose == true;
            UpdateToggleStateText(CloseToTrayToggleSwitch);
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
                SetToggleStateText(stateText, toggleSwitch.IsOn);
            }
        }

        private static void SetToggleStateText(TextBlock stateText, bool isOn)
        {
            stateText.Text = isOn ? "开" : "关";
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
            if (ReferenceEquals(toggleSwitch, ContinueDownloadToggleSwitch)) return ContinueDownloadStateText;
            if (ReferenceEquals(toggleSwitch, AutoDeleteStaleRecordsToggleSwitch)) return AutoDeleteStaleRecordsStateText;
            if (ReferenceEquals(toggleSwitch, DeleteTorrentAfterCompleteToggleSwitch)) return DeleteTorrentAfterCompleteStateText;
            if (ReferenceEquals(toggleSwitch, BtEnableToggleSwitch)) return BtEnableStateText;
            if (ReferenceEquals(toggleSwitch, UseSystemProxyCheckBox)) return UseSystemProxyStateText;
            if (ReferenceEquals(toggleSwitch, CustomProxyToggleSwitch)) return CustomProxyStateText;
            if (ReferenceEquals(toggleSwitch, TerminalOutputToggleSwitch)) return TerminalOutputStateText;

            return null;
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

        private void NotificationActionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoadingGeneralSettings)
            {
                return;
            }

            UpdateGeneralSettingsFromUi();
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

        private string GetSelectedDownloadCompleteNotificationAction()
        {
            return DownloadCompleteNotificationActionComboBox?.SelectedItem is ComboBoxItem item &&
                item.Tag?.ToString() is string action &&
                !string.IsNullOrWhiteSpace(action)
                ? action
                : "Home";
        }

        private void SetDownloadCompleteNotificationActionSelection(string action)
        {
            if (DownloadCompleteNotificationActionComboBox is null)
            {
                return;
            }

            for (int index = 0; index < DownloadCompleteNotificationActionComboBox.Items.Count; index++)
            {
                if (DownloadCompleteNotificationActionComboBox.Items[index] is ComboBoxItem item &&
                    item.Tag?.ToString()?.Equals(action, StringComparison.OrdinalIgnoreCase) == true)
                {
                    DownloadCompleteNotificationActionComboBox.SelectedIndex = index;
                    return;
                }
            }

            DownloadCompleteNotificationActionComboBox.SelectedIndex = 0;
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

        private void LoadDownloadSettings()
        {
            _settingsPageViewModel.LoadDownloadSettings();
            _isLoadingDownloadSettings = true;
            try
            {
                ApplyDownloadSettingsToUi();
            }
            finally
            {
                _isLoadingDownloadSettings = false;
            }
        }

        private void ApplyDownloadSettingsToUi()
        {
            DownloadSettings settings = NormalizeDownloadSettings(_settingsPageViewModel.DownloadSettings);
            if (settings != _settingsPageViewModel.DownloadSettings)
            {
                _settingsPageViewModel.SaveDownloadSettings(settings);
            }

            DownloadDirectoryTextBox.Text = settings.DownloadDirectory;
            MaxConcurrentDownloadsNumberBox.Value = settings.MaxConcurrentDownloads;
            SplitCountNumberBox.Value = settings.SplitCount;
            MaxConnectionPerServerNumberBox.Value = settings.MaxConnectionPerServer;
            MaxTriesNumberBox.Value = settings.MaxTries;
            RetryWaitNumberBox.Value = settings.RetryWaitSeconds;
            SetToggleSwitch(ContinueDownloadToggleSwitch, settings.ContinueDownloads);
            SetToggleSwitch(AutoDeleteStaleRecordsToggleSwitch, settings.AutoDeleteStaleRecords);
            SetToggleSwitch(DeleteTorrentAfterCompleteToggleSwitch, settings.DeleteTorrentAfterComplete);
            RemoteTimeComboBox.SelectedIndex = settings.RemoteTime ? 1 : 0;
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

        private void UpdateSpeedLimitStateFromToolbar()
        {
            _isDownloadSpeedLimitEnabled = DownloadLimitToggleSwitch?.IsOn == true;
            _isUploadSpeedLimitEnabled = UploadLimitToggleSwitch?.IsOn == true;
            _downloadLimitBytesPerSecond = _isDownloadSpeedLimitEnabled
                ? GetSpeedLimitBytesPerSecond(DownloadLimitNumberBox, GetSelectedSpeedLimitUnit(DownloadLimitUnitComboBox))
                : 0;
            _uploadLimitBytesPerSecond = _isUploadSpeedLimitEnabled
                ? GetSpeedLimitBytesPerSecond(UploadLimitNumberBox, GetSelectedSpeedLimitUnit(UploadLimitUnitComboBox))
                : 0;
        }

        private void SaveDownloadSettings()
        {
            DownloadSettings settings = NormalizeDownloadSettings(new DownloadSettings(
                string.IsNullOrWhiteSpace(DownloadDirectoryTextBox.Text) ? AppPaths.DefaultDownloadDirectory : DownloadDirectoryTextBox.Text.Trim(),
                GetValidIntNumberBoxValue(MaxConcurrentDownloadsNumberBox, 1, 10, 5),
                GetValidIntNumberBoxValue(SplitCountNumberBox, 1, 256, 64),
                GetValidIntNumberBoxValue(MaxConnectionPerServerNumberBox, 1, 16, 16),
                ContinueDownloadToggleSwitch?.IsOn == true,
                RemoteTimeComboBox?.SelectedItem is ComboBoxItem item &&
                    item.Tag?.ToString()?.Equals("Server", StringComparison.OrdinalIgnoreCase) == true,
                GetValidIntNumberBoxValue(MaxTriesNumberBox, 0, 60, 0),
                GetValidIntNumberBoxValue(RetryWaitNumberBox, 0, 600, 10),
                AutoDeleteStaleRecordsToggleSwitch?.IsOn == true,
                DeleteTorrentAfterCompleteToggleSwitch?.IsOn == true));

            _settingsPageViewModel.SaveDownloadSettings(settings);
            if (_downloadCoordinator is not null)
            {
                _downloadCoordinator.DeleteTorrentAfterComplete = settings.DeleteTorrentAfterComplete;
            }
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

        private static int GetValidIntNumberBoxValue(NumberBox numberBox, int minimum, int maximum, int fallback)
        {
            if (numberBox is null || double.IsNaN(numberBox.Value))
            {
                return fallback;
            }

            return Math.Clamp((int)Math.Round(numberBox.Value), minimum, maximum);
        }

        private static DownloadSettings NormalizeDownloadSettings(DownloadSettings settings)
        {
            string directory = string.IsNullOrWhiteSpace(settings.DownloadDirectory)
                ? AppPaths.DefaultDownloadDirectory
                : settings.DownloadDirectory.Trim();

            return settings with
            {
                DownloadDirectory = directory,
                MaxConcurrentDownloads = Math.Clamp(settings.MaxConcurrentDownloads, 1, 10),
                SplitCount = Math.Clamp(settings.SplitCount, 1, 256),
                MaxConnectionPerServer = Math.Clamp(settings.MaxConnectionPerServer, 1, 16),
                MaxTries = Math.Clamp(settings.MaxTries, 0, 60),
                RetryWaitSeconds = Math.Clamp(settings.RetryWaitSeconds, 0, 600)
            };
        }

        private static void SetSpeedLimitUnit(ComboBox comboBox, string unit)
        {
            comboBox.SelectedIndex = unit.Equals("MB/s", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        }
    }
}
