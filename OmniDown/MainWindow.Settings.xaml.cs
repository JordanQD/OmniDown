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
using System.Net;
using System.Net.Http;
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
        private Border BtAutoDownloadSettingCard => SettingsPage.BtAutoDownloadSettingCardControl;
        private Border BtForceEncryptionSettingCard => SettingsPage.BtForceEncryptionSettingCardControl;
        private Border BtKeepSeedingSettingCard => SettingsPage.BtKeepSeedingSettingCardControl;
        private Border BtMaxPeersSettingCard => SettingsPage.BtMaxPeersSettingCardControl;
        private Border BtTrackerSourceSettingCard => SettingsPage.BtTrackerSourceSettingCardControl;
        private Border BtTrackerCustomSourceSettingCard => SettingsPage.BtTrackerCustomSourceSettingCardControl;
        private Border BtTrackerListSettingCard => SettingsPage.BtTrackerListSettingCardControl;
        private Border BtAutoSyncTrackerSettingCard => SettingsPage.BtAutoSyncTrackerSettingCardControl;
        private Border UseSystemProxySettingCard => SettingsPage.UseSystemProxySettingCardControl;
        private Border CustomProxySettingCard => SettingsPage.CustomProxySettingCardControl;
        private Border UpnpSettingCard => SettingsPage.UpnpSettingCardControl;
        private Border BtPortSettingCard => SettingsPage.BtPortSettingCardControl;
        private Border DhtPortSettingCard => SettingsPage.DhtPortSettingCardControl;
        private Border UserAgentSettingCard => SettingsPage.UserAgentSettingCardControl;
        private Border ConnectTimeoutSettingCard => SettingsPage.ConnectTimeoutSettingCardControl;
        private Border TimeoutSettingCard => SettingsPage.TimeoutSettingCardControl;
        private Border FileAllocationSettingCard => SettingsPage.FileAllocationSettingCardControl;
        private Border AriaPathSettingCard => SettingsPage.AriaPathSettingCardControl;
        private Border RpcPortSettingCard => SettingsPage.RpcPortSettingCardControl;
        private Border ProcessStatusSettingCard => SettingsPage.ProcessStatusSettingCardControl;
        private Border TerminalSettingCard => SettingsPage.TerminalSettingCardControl;
        private Border AboutAppCard => SettingsPage.AboutAppCardControl;
        private Border AboutCloneCard => SettingsPage.AboutCloneCardControl;
        private Border AboutIssueCard => SettingsPage.AboutIssueCardControl;
        private Border AboutReferencesCard => SettingsPage.AboutReferencesCardControl;
        private Border AboutTrackerSourcesCard => SettingsPage.AboutTrackerSourcesCardControl;
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
        private ToggleSwitch BtAutoDownloadToggleSwitch => SettingsPage.BtAutoDownloadToggleSwitchControl;
        private TextBlock BtAutoDownloadStateText => SettingsPage.BtAutoDownloadStateTextControl;
        private ToggleSwitch BtForceEncryptionToggleSwitch => SettingsPage.BtForceEncryptionToggleSwitchControl;
        private TextBlock BtForceEncryptionStateText => SettingsPage.BtForceEncryptionStateTextControl;
        private ComboBox BtSeedingModeComboBox => SettingsPage.BtSeedingModeComboBoxControl;
        private NumberBox BtSeedRatioNumberBox => SettingsPage.BtSeedRatioNumberBoxControl;
        private NumberBox BtSeedTimeNumberBox => SettingsPage.BtSeedTimeNumberBoxControl;
        private NumberBox BtMaxPeersNumberBox => SettingsPage.BtMaxPeersNumberBoxControl;
        private Button BtTrackerSourceDropDownButton => SettingsPage.BtTrackerSourceDropDownButtonControl;
        private TextBlock BtTrackerSourceSummaryText => SettingsPage.BtTrackerSourceSummaryTextControl;
        private CheckBox BtTrackerNgosangBestCheckBox => SettingsPage.BtTrackerNgosangBestCheckBoxControl;
        private CheckBox BtTrackerNgosangBestIpCheckBox => SettingsPage.BtTrackerNgosangBestIpCheckBoxControl;
        private CheckBox BtTrackerNgosangAllCheckBox => SettingsPage.BtTrackerNgosangAllCheckBoxControl;
        private CheckBox BtTrackerNgosangAllIpCheckBox => SettingsPage.BtTrackerNgosangAllIpCheckBoxControl;
        private CheckBox BtTrackerNgosangCdnBestCheckBox => SettingsPage.BtTrackerNgosangCdnBestCheckBoxControl;
        private CheckBox BtTrackerNgosangCdnBestIpCheckBox => SettingsPage.BtTrackerNgosangCdnBestIpCheckBoxControl;
        private CheckBox BtTrackerNgosangCdnAllCheckBox => SettingsPage.BtTrackerNgosangCdnAllCheckBoxControl;
        private CheckBox BtTrackerNgosangCdnAllIpCheckBox => SettingsPage.BtTrackerNgosangCdnAllIpCheckBoxControl;
        private CheckBox BtTrackerXiu2BestCheckBox => SettingsPage.BtTrackerXiu2BestCheckBoxControl;
        private CheckBox BtTrackerXiu2AllCheckBox => SettingsPage.BtTrackerXiu2AllCheckBoxControl;
        private CheckBox BtTrackerXiu2HttpCheckBox => SettingsPage.BtTrackerXiu2HttpCheckBoxControl;
        private CheckBox BtTrackerXiu2CdnBestCheckBox => SettingsPage.BtTrackerXiu2CdnBestCheckBoxControl;
        private CheckBox BtTrackerXiu2CdnAllCheckBox => SettingsPage.BtTrackerXiu2CdnAllCheckBoxControl;
        private CheckBox BtTrackerXiu2CdnHttpCheckBox => SettingsPage.BtTrackerXiu2CdnHttpCheckBoxControl;
        private TextBox BtCustomTrackerSourceTextBox => SettingsPage.BtCustomTrackerSourceTextBoxControl;
        private ListView BtCustomTrackerSourceListView => SettingsPage.BtCustomTrackerSourceListViewControl;
        private TextBox BtTrackerSourceTextBox => SettingsPage.BtTrackerSourceTextBoxControl;
        private Button BtSyncTrackerButton => SettingsPage.BtSyncTrackerButtonControl;
        private TextBox BtTrackerListTextBox => SettingsPage.BtTrackerListTextBoxControl;
        private ToggleSwitch BtAutoSyncTrackerToggleSwitch => SettingsPage.BtAutoSyncTrackerToggleSwitchControl;
        private TextBlock BtAutoSyncTrackerStateText => SettingsPage.BtAutoSyncTrackerStateTextControl;
        private TextBlock BtLastTrackerSyncText => SettingsPage.BtLastTrackerSyncTextControl;
        private ToggleSwitch UseSystemProxyCheckBox => SettingsPage.UseSystemProxyCheckBoxControl;
        private TextBlock UseSystemProxyStateText => SettingsPage.UseSystemProxyStateTextControl;
        private ToggleSwitch CustomProxyToggleSwitch => SettingsPage.CustomProxyToggleSwitchControl;
        private TextBlock CustomProxyStateText => SettingsPage.CustomProxyStateTextControl;
        private TextBox ProxyServerTextBox => SettingsPage.ProxyServerTextBoxControl;
        private Button DetectSystemProxyButton => SettingsPage.DetectSystemProxyButtonControl;
        private TextBox ProxyBypassTextBox => SettingsPage.ProxyBypassTextBoxControl;
        private CheckBox ProxyDownloadsCheckBox => SettingsPage.ProxyDownloadsCheckBoxControl;
        private CheckBox ProxyTrackersCheckBox => SettingsPage.ProxyTrackersCheckBoxControl;
        private ToggleSwitch EnableUpnpToggleSwitch => SettingsPage.EnableUpnpToggleSwitchControl;
        private TextBlock EnableUpnpStateText => SettingsPage.EnableUpnpStateTextControl;
        private NumberBox BtListenPortNumberBox => SettingsPage.BtListenPortNumberBoxControl;
        private NumberBox DhtListenPortNumberBox => SettingsPage.DhtListenPortNumberBoxControl;
        private TextBox UserAgentTextBox => SettingsPage.UserAgentTextBoxControl;
        private NumberBox ConnectTimeoutNumberBox => SettingsPage.ConnectTimeoutNumberBoxControl;
        private NumberBox TimeoutNumberBox => SettingsPage.TimeoutNumberBoxControl;
        private ComboBox FileAllocationComboBox => SettingsPage.FileAllocationComboBoxControl;
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
            SettingsPage.BitTorrentSettingChanged += BitTorrentSetting_Changed;
            SettingsPage.NetworkSettingChanged += NetworkSetting_Changed;
            SettingsPage.DetectSystemProxyRequested += DetectSystemProxyButton_Click;
            SettingsPage.RandomBtPortRequested += RandomBtPortButton_Click;
            SettingsPage.RandomDhtPortRequested += RandomDhtPortButton_Click;
            SettingsPage.UserAgentPresetRequested += UserAgentPresetButton_Click;
            SettingsPage.AddBtCustomTrackerRequested += AddBtCustomTrackerButton_Click;
            SettingsPage.SyncBtTrackerRequested += SyncBtTrackerButton_Click;
            SettingsPage.StartAriaRequested += StartAriaButton_Click;
            SettingsPage.StopAriaRequested += StopAriaButton_Click;
            SettingsPage.CopyCloneCommandRequested += CopyCloneCommandButton_Click;
            SettingsPage.OpenAboutLinkRequested += OpenAboutLinkButton_Click;
        }

        private void UseSystemProxyCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isLoadingNetworkSettings)
            {
                SaveNetworkSettings();
            }

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

        private void BitTorrentSetting_Changed(object sender, RoutedEventArgs e)
        {
            if (_isLoadingBitTorrentSettings)
            {
                return;
            }

            UpdateBitTorrentDependentUi();
            UpdateTrackerSourceSummary();
            SaveBitTorrentSettings();
        }

        private void AddBtCustomTrackerButton_Click(object sender, RoutedEventArgs e)
        {
            AddCustomTrackerSource();
        }

        private async void SyncBtTrackerButton_Click(object sender, RoutedEventArgs e)
        {
            await SyncBitTorrentTrackersAsync();
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
            if (ReferenceEquals(toggleSwitch, BtAutoDownloadToggleSwitch)) return BtAutoDownloadStateText;
            if (ReferenceEquals(toggleSwitch, BtForceEncryptionToggleSwitch)) return BtForceEncryptionStateText;
            if (ReferenceEquals(toggleSwitch, BtAutoSyncTrackerToggleSwitch)) return BtAutoSyncTrackerStateText;
            if (ReferenceEquals(toggleSwitch, UseSystemProxyCheckBox)) return UseSystemProxyStateText;
            if (ReferenceEquals(toggleSwitch, CustomProxyToggleSwitch)) return CustomProxyStateText;
            if (ReferenceEquals(toggleSwitch, EnableUpnpToggleSwitch)) return EnableUpnpStateText;
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

        private void LoadBitTorrentSettings()
        {
            _settingsPageViewModel.LoadBitTorrentSettings();
            _isLoadingBitTorrentSettings = true;
            try
            {
                ApplyBitTorrentSettingsToUi();
            }
            finally
            {
                _isLoadingBitTorrentSettings = false;
            }
        }

        private void ApplyBitTorrentSettingsToUi()
        {
            BitTorrentSettings settings = NormalizeBitTorrentSettings(_settingsPageViewModel.BitTorrentSettings);
            if (settings != _settingsPageViewModel.BitTorrentSettings)
            {
                _settingsPageViewModel.SaveBitTorrentSettings(settings);
            }

            SetToggleSwitch(BtAutoDownloadToggleSwitch, settings.AutoDownloadContent);
            SetToggleSwitch(BtForceEncryptionToggleSwitch, settings.ForceEncryption);
            SetSeedingModeSelection(settings.KeepSeeding);
            SetToggleSwitch(BtAutoSyncTrackerToggleSwitch, settings.AutoSyncTracker);
            BtSeedRatioNumberBox.Value = settings.SeedRatio;
            BtSeedTimeNumberBox.Value = settings.SeedTimeMinutes;
            BtMaxPeersNumberBox.Value = settings.MaxPeers;
            ApplyTrackerSourceSelectionToUi(settings);
            BtTrackerListTextBox.Text = settings.TrackerList;
            UpdateBitTorrentDependentUi();
            UpdateTrackerSyncTimeText(settings.LastSyncTrackerTime);
        }

        private void LoadNetworkSettings()
        {
            _settingsPageViewModel.LoadNetworkSettings();
            _isLoadingNetworkSettings = true;
            try
            {
                ApplyNetworkSettingsToUi();
            }
            finally
            {
                _isLoadingNetworkSettings = false;
            }
        }

        private void ApplyNetworkSettingsToUi()
        {
            NetworkSettings settings = NormalizeNetworkSettings(_settingsPageViewModel.NetworkSettings);
            if (settings != _settingsPageViewModel.NetworkSettings)
            {
                _settingsPageViewModel.SaveNetworkSettings(settings);
            }

            SetToggleSwitch(UseSystemProxyCheckBox, settings.UseSystemProxy);
            SetToggleSwitch(CustomProxyToggleSwitch, settings.CustomProxyEnabled);
            ProxyServerTextBox.Text = settings.ProxyServer;
            ProxyBypassTextBox.Text = settings.ProxyBypass;
            ProxyDownloadsCheckBox.IsChecked = settings.ProxyDownloads;
            ProxyTrackersCheckBox.IsChecked = settings.ProxyTrackers;
            SetToggleSwitch(EnableUpnpToggleSwitch, settings.EnableUpnp);
            BtListenPortNumberBox.Value = settings.ListenPort;
            DhtListenPortNumberBox.Value = settings.DhtListenPort;
            UserAgentTextBox.Text = settings.UserAgent;
            ConnectTimeoutNumberBox.Value = settings.ConnectTimeoutSeconds;
            TimeoutNumberBox.Value = settings.TimeoutSeconds;
            SetFileAllocationSelection(settings.FileAllocation);
            UpdateNetworkDependentUi();
        }

        private void NetworkSetting_Changed(object sender, RoutedEventArgs e)
        {
            if (_isLoadingNetworkSettings)
            {
                return;
            }

            UpdateNetworkDependentUi();
            SaveNetworkSettings();
        }

        private void DetectSystemProxyButton_Click(object sender, RoutedEventArgs e)
        {
            SystemProxySettings proxySettings = SystemProxyResolver.Resolve();
            if (!proxySettings.HasProxy)
            {
                ShowMessage("没有检测到可用的 Windows 系统代理。", InfoBarSeverity.Informational);
                return;
            }

            ProxyServerTextBox.Text = proxySettings.AllProxy ?? string.Empty;
            ProxyBypassTextBox.Text = proxySettings.NoProxy ?? string.Empty;
            SetToggleSwitch(CustomProxyToggleSwitch, true);
            SaveNetworkSettings();
            ShowMessage("已填入系统代理。", InfoBarSeverity.Success);
        }

        private void RandomBtPortButton_Click(object sender, RoutedEventArgs e)
        {
            BtListenPortNumberBox.Value = Random.Shared.Next(20000, 25000);
            SaveNetworkSettings();
        }

        private void RandomDhtPortButton_Click(object sender, RoutedEventArgs e)
        {
            DhtListenPortNumberBox.Value = Random.Shared.Next(25000, 30000);
            SaveNetworkSettings();
        }

        private void UserAgentPresetButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
            {
                return;
            }

            UserAgentTextBox.Text = button.Tag?.ToString() switch
            {
                "Chrome" => "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36",
                "Edge" => "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36 Edg/124.0.0.0",
                "Safari" => "Mozilla/5.0 (Macintosh; Intel Mac OS X 14_4) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.4 Safari/605.1.15",
                "Firefox" => "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:125.0) Gecko/20100101 Firefox/125.0",
                "Transmission" => "Transmission/3.00",
                _ => string.Empty
            };
            SaveNetworkSettings();
        }

        private void SaveNetworkSettings()
        {
            if (_isLoadingNetworkSettings)
            {
                return;
            }

            NetworkSettings settings = NormalizeNetworkSettings(new NetworkSettings(
                UseSystemProxyCheckBox?.IsOn == true,
                CustomProxyToggleSwitch?.IsOn == true,
                ProxyServerTextBox.Text.Trim(),
                ProxyBypassTextBox.Text.Trim(),
                ProxyDownloadsCheckBox?.IsChecked == true,
                ProxyTrackersCheckBox?.IsChecked == true,
                EnableUpnpToggleSwitch?.IsOn == true,
                GetValidIntNumberBoxValue(BtListenPortNumberBox, 1024, 65535, NetworkSettings.Default.ListenPort),
                GetValidIntNumberBoxValue(DhtListenPortNumberBox, 1024, 65535, NetworkSettings.Default.DhtListenPort),
                SanitizeHeaderValue(UserAgentTextBox.Text),
                GetValidIntNumberBoxValue(ConnectTimeoutNumberBox, 1, 600, NetworkSettings.Default.ConnectTimeoutSeconds),
                GetValidIntNumberBoxValue(TimeoutNumberBox, 1, 600, NetworkSettings.Default.TimeoutSeconds),
                GetSelectedFileAllocation()));

            if (settings.CustomProxyEnabled &&
                !string.IsNullOrWhiteSpace(settings.ProxyServer) &&
                !IsValidProxyUrl(settings.ProxyServer))
            {
                ShowMessage("代理地址格式不正确，请使用 http:// 或 https://。", InfoBarSeverity.Warning);
                return;
            }

            if (UserAgentTextBox.Text != settings.UserAgent)
            {
                int selectionStart = UserAgentTextBox.SelectionStart;
                UserAgentTextBox.Text = settings.UserAgent;
                UserAgentTextBox.SelectionStart = Math.Min(selectionStart, settings.UserAgent.Length);
            }

            _settingsPageViewModel.SaveNetworkSettings(settings);
        }

        private void UpdateNetworkDependentUi()
        {
            bool proxyEnabled = CustomProxyToggleSwitch?.IsOn == true;
            ProxyServerTextBox.IsEnabled = proxyEnabled;
            ProxyBypassTextBox.IsEnabled = proxyEnabled;
            ProxyDownloadsCheckBox.IsEnabled = proxyEnabled;
            ProxyTrackersCheckBox.IsEnabled = proxyEnabled;
            DetectSystemProxyButton.IsEnabled = true;
        }

        private void SetFileAllocationSelection(string fileAllocation)
        {
            string normalized = NormalizeFileAllocation(fileAllocation);
            for (int index = 0; index < FileAllocationComboBox.Items.Count; index++)
            {
                if (FileAllocationComboBox.Items[index] is ComboBoxItem item &&
                    item.Tag?.ToString()?.Equals(normalized, StringComparison.OrdinalIgnoreCase) == true)
                {
                    FileAllocationComboBox.SelectedIndex = index;
                    return;
                }
            }

            FileAllocationComboBox.SelectedIndex = 0;
        }

        private string GetSelectedFileAllocation()
        {
            return FileAllocationComboBox?.SelectedItem is ComboBoxItem item &&
                item.Tag?.ToString() is string fileAllocation
                ? NormalizeFileAllocation(fileAllocation)
                : NetworkSettings.Default.FileAllocation;
        }

        private static NetworkSettings NormalizeNetworkSettings(NetworkSettings settings)
        {
            return settings with
            {
                ProxyServer = NormalizeProxyServer(settings.ProxyServer),
                ProxyBypass = NormalizeProxyBypass(settings.ProxyBypass),
                ListenPort = Math.Clamp(settings.ListenPort, 1024, 65535),
                DhtListenPort = Math.Clamp(settings.DhtListenPort, 1024, 65535),
                UserAgent = SanitizeHeaderValue(settings.UserAgent),
                ConnectTimeoutSeconds = Math.Clamp(settings.ConnectTimeoutSeconds, 1, 600),
                TimeoutSeconds = Math.Clamp(settings.TimeoutSeconds, 1, 600),
                FileAllocation = NormalizeFileAllocation(settings.FileAllocation)
            };
        }

        private static bool IsValidProxyUrl(string value)
        {
            return Uri.TryCreate(NormalizeProxyServer(value), UriKind.Absolute, out Uri? uri) &&
                uri.Host.Length > 0 &&
                (uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                    uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase));
        }

        private static string NormalizeProxyServer(string value)
        {
            string trimmed = value.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) ||
                trimmed.Contains("://", StringComparison.Ordinal))
            {
                return trimmed;
            }

            return $"http://{trimmed}";
        }

        private static string NormalizeProxyBypass(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return NetworkSettings.DefaultProxyBypass;
            }

            return string.Join(";", value
                .Replace(";", "\n", StringComparison.Ordinal)
                .Replace(",", "\n", StringComparison.Ordinal)
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase));
        }

        private static string NormalizeFileAllocation(string? value)
        {
            return value?.Trim().ToLowerInvariant() switch
            {
                "prealloc" => "prealloc",
                "trunc" => "trunc",
                "falloc" => "falloc",
                _ => "none"
            };
        }

        private static string SanitizeHeaderValue(string? value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : new string(value.Where(ch => ch is not ('\r' or '\n')).ToArray()).Trim();
        }

        private HttpClient CreateTrackerHttpClient()
        {
            NetworkSettings settings = _settingsPageViewModel.NetworkSettings;
            HttpMessageHandler handler = new HttpClientHandler();
            string? proxyServer = null;
            string bypass = string.Empty;

            if (settings.CustomProxyEnabled && settings.ProxyTrackers && !string.IsNullOrWhiteSpace(settings.ProxyServer))
            {
                proxyServer = settings.ProxyServer;
                bypass = settings.ProxyBypass;
            }
            else if (settings.UseSystemProxy && settings.ProxyTrackers)
            {
                SystemProxySettings systemProxy = SystemProxyResolver.Resolve();
                proxyServer = systemProxy.AllProxy;
                bypass = systemProxy.NoProxy ?? string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(proxyServer) &&
                Uri.TryCreate(proxyServer, UriKind.Absolute, out Uri? proxyUri) &&
                handler is HttpClientHandler httpHandler)
            {
                WebProxy proxy = new(proxyUri);
                string[] bypassList = bypass
                    .Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (bypassList.Length > 0)
                {
                    proxy.BypassList = bypassList;
                }

                httpHandler.Proxy = proxy;
                httpHandler.UseProxy = true;
            }

            return new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(20)
            };
        }

        private void SaveBitTorrentSettings()
        {
            string[] selectedSources = GetSelectedTrackerSourceUrls();
            BitTorrentSettings settings = NormalizeBitTorrentSettings(new BitTorrentSettings(
                true,
                BtAutoDownloadToggleSwitch?.IsOn == true,
                BtForceEncryptionToggleSwitch?.IsOn == true,
                IsKeepSeedingSelected(),
                GetValidDoubleNumberBoxValue(BtSeedRatioNumberBox, 0, 100, 1.0),
                GetValidIntNumberBoxValue(BtSeedTimeNumberBox, 0, 525600, 60),
                GetValidIntNumberBoxValue(BtMaxPeersNumberBox, 1, 1000, 128),
                BitTorrentSettings.Default.ListenPort,
                selectedSources.FirstOrDefault() ?? BitTorrentSettings.DefaultTrackerSourceUrl,
                selectedSources,
                GetCustomTrackerUrls(),
                NormalizeTrackerList(BtTrackerListTextBox.Text),
                BtAutoSyncTrackerToggleSwitch?.IsOn == true,
                _settingsPageViewModel.BitTorrentSettings.LastSyncTrackerTime));

            _settingsPageViewModel.SaveBitTorrentSettings(settings);
            UpdateTrackerSyncTimeText(settings.LastSyncTrackerTime);
        }

        private async Task SyncBitTorrentTrackersAsync()
        {
            string[] sourceUrls = GetSelectedTrackerSourceUrls();
            if (sourceUrls.Length == 0)
            {
                ShowMessage("请至少选择一个 Tracker 来源。", InfoBarSeverity.Warning);
                return;
            }

            BtSyncTrackerButton.IsEnabled = false;
            try
            {
                using HttpClient client = CreateTrackerHttpClient();
                List<string> trackerBlocks = [];
                List<string> failedSources = [];
                foreach (string sourceUrl in sourceUrls)
                {
                    try
                    {
                        trackerBlocks.Add(await client.GetStringAsync(sourceUrl));
                    }
                    catch
                    {
                        failedSources.Add(sourceUrl);
                    }
                }

                string trackers = NormalizeTrackerList(string.Join(Environment.NewLine, trackerBlocks));
                if (string.IsNullOrWhiteSpace(trackers))
                {
                    ShowMessage("Tracker 源没有返回可用地址。", InfoBarSeverity.Warning);
                    return;
                }

                BtTrackerListTextBox.Text = trackers;
                BitTorrentSettings settings = NormalizeBitTorrentSettings(new BitTorrentSettings(
                    true,
                    BtAutoDownloadToggleSwitch?.IsOn == true,
                    BtForceEncryptionToggleSwitch?.IsOn == true,
                    IsKeepSeedingSelected(),
                    GetValidDoubleNumberBoxValue(BtSeedRatioNumberBox, 0, 100, 1.0),
                    GetValidIntNumberBoxValue(BtSeedTimeNumberBox, 0, 525600, 60),
                    GetValidIntNumberBoxValue(BtMaxPeersNumberBox, 1, 1000, 128),
                    BitTorrentSettings.Default.ListenPort,
                    sourceUrls[0],
                    sourceUrls,
                    GetCustomTrackerUrls(),
                    trackers,
                    BtAutoSyncTrackerToggleSwitch?.IsOn == true,
                    DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
                _settingsPageViewModel.SaveBitTorrentSettings(settings);
                UpdateTrackerSyncTimeText(settings.LastSyncTrackerTime);
                ShowMessage(failedSources.Count == 0 ? "Tracker 已同步。" : $"Tracker 已部分同步，{failedSources.Count} 个来源失败。", failedSources.Count == 0 ? InfoBarSeverity.Success : InfoBarSeverity.Warning);
            }
            catch (Exception ex)
            {
                ShowMessage($"Tracker 同步失败：{ex.Message}", InfoBarSeverity.Error);
            }
            finally
            {
                BtSyncTrackerButton.IsEnabled = true;
            }
        }

        private async Task AutoSyncBitTorrentTrackersIfNeededAsync()
        {
            BitTorrentSettings current = _settingsPageViewModel.BitTorrentSettings;
            if (!current.AutoSyncTracker)
            {
                return;
            }

            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long oneDay = (long)TimeSpan.FromDays(1).TotalMilliseconds;
            string[] sourceUrls = NormalizeTrackerSourceUrls(current.SelectedTrackerSourceUrls, current.TrackerSourceUrl, current.CustomTrackerUrls);
            if (now - current.LastSyncTrackerTime < oneDay || sourceUrls.Length == 0)
            {
                return;
            }

            try
            {
                using HttpClient client = CreateTrackerHttpClient();
                List<string> trackerBlocks = [];
                foreach (string sourceUrl in sourceUrls)
                {
                    try
                    {
                        trackerBlocks.Add(await client.GetStringAsync(sourceUrl));
                    }
                    catch
                    {
                    }
                }

                string trackers = NormalizeTrackerList(string.Join(Environment.NewLine, trackerBlocks));
                if (string.IsNullOrWhiteSpace(trackers))
                {
                    return;
                }

                BitTorrentSettings updated = current with
                {
                    TrackerList = trackers,
                    LastSyncTrackerTime = now
                };
                _settingsPageViewModel.SaveBitTorrentSettings(updated);
                BtTrackerListTextBox.Text = trackers;
                UpdateTrackerSyncTimeText(now);
            }
            catch
            {
                // Auto-sync is best-effort; manual sync still reports failures.
            }
        }

        private void AddCustomTrackerSource()
        {
            string url = BtCustomTrackerSourceTextBox.Text.Trim();
            if (!IsValidHttpUrl(url))
            {
                ShowMessage("请输入有效的 HTTP/HTTPS Tracker 源地址。", InfoBarSeverity.Warning);
                return;
            }

            string[] customUrls = GetCustomTrackerUrls();
            if (customUrls.Contains(url, StringComparer.OrdinalIgnoreCase))
            {
                BtCustomTrackerSourceTextBox.Text = string.Empty;
                return;
            }

            BtCustomTrackerSourceListView.Items.Add(url);
            BtCustomTrackerSourceListView.SelectedItems.Add(url);
            BtCustomTrackerSourceTextBox.Text = string.Empty;
            UpdateTrackerSourceSummary();
            SaveBitTorrentSettings();
        }

        private void ApplyTrackerSourceSelectionToUi(BitTorrentSettings settings)
        {
            string[] selectedSources = NormalizeTrackerSourceUrls(settings.SelectedTrackerSourceUrls, settings.TrackerSourceUrl, settings.CustomTrackerUrls);
            foreach ((_, string url, CheckBox checkBox) in GetBuiltInTrackerSourceCheckBoxes())
            {
                SetCheckBox(checkBox, selectedSources.Contains(url, StringComparer.OrdinalIgnoreCase));
            }

            BtCustomTrackerSourceListView.Items.Clear();
            foreach (string url in GetNormalizedUrls(settings.CustomTrackerUrls))
            {
                BtCustomTrackerSourceListView.Items.Add(url);
                if (selectedSources.Contains(url, StringComparer.OrdinalIgnoreCase))
                {
                    BtCustomTrackerSourceListView.SelectedItems.Add(url);
                }
            }

            BtTrackerSourceTextBox.Text = selectedSources.FirstOrDefault() ?? BitTorrentSettings.DefaultTrackerSourceUrl;
            UpdateTrackerSourceSummary();
        }

        private string[] GetSelectedTrackerSourceUrls()
        {
            List<string> urls = [];
            foreach ((_, string url, CheckBox checkBox) in GetBuiltInTrackerSourceCheckBoxes())
            {
                if (checkBox?.IsChecked == true)
                {
                    urls.Add(url);
                }
            }

            foreach (object item in BtCustomTrackerSourceListView.SelectedItems)
            {
                if (item?.ToString() is string url && IsValidHttpUrl(url))
                {
                    urls.Add(url);
                }
            }

            return GetNormalizedUrls(urls);
        }

        private (string Label, string Url, CheckBox CheckBox)[] GetBuiltInTrackerSourceCheckBoxes()
        {
            return
            [
                ("trackers_best.txt", GetTrackerSourceUrl(BtTrackerNgosangBestCheckBox), BtTrackerNgosangBestCheckBox),
                ("trackers_best_ip.txt", GetTrackerSourceUrl(BtTrackerNgosangBestIpCheckBox), BtTrackerNgosangBestIpCheckBox),
                ("trackers_all.txt", GetTrackerSourceUrl(BtTrackerNgosangAllCheckBox), BtTrackerNgosangAllCheckBox),
                ("trackers_all_ip.txt", GetTrackerSourceUrl(BtTrackerNgosangAllIpCheckBox), BtTrackerNgosangAllIpCheckBox),
                ("trackers_best.txt CDN", GetTrackerSourceUrl(BtTrackerNgosangCdnBestCheckBox), BtTrackerNgosangCdnBestCheckBox),
                ("trackers_best_ip.txt CDN", GetTrackerSourceUrl(BtTrackerNgosangCdnBestIpCheckBox), BtTrackerNgosangCdnBestIpCheckBox),
                ("trackers_all.txt CDN", GetTrackerSourceUrl(BtTrackerNgosangCdnAllCheckBox), BtTrackerNgosangCdnAllCheckBox),
                ("trackers_all_ip.txt CDN", GetTrackerSourceUrl(BtTrackerNgosangCdnAllIpCheckBox), BtTrackerNgosangCdnAllIpCheckBox),
                ("best.txt", GetTrackerSourceUrl(BtTrackerXiu2BestCheckBox), BtTrackerXiu2BestCheckBox),
                ("all.txt", GetTrackerSourceUrl(BtTrackerXiu2AllCheckBox), BtTrackerXiu2AllCheckBox),
                ("http.txt", GetTrackerSourceUrl(BtTrackerXiu2HttpCheckBox), BtTrackerXiu2HttpCheckBox),
                ("best.txt CDN", GetTrackerSourceUrl(BtTrackerXiu2CdnBestCheckBox), BtTrackerXiu2CdnBestCheckBox),
                ("all.txt CDN", GetTrackerSourceUrl(BtTrackerXiu2CdnAllCheckBox), BtTrackerXiu2CdnAllCheckBox),
                ("http.txt CDN", GetTrackerSourceUrl(BtTrackerXiu2CdnHttpCheckBox), BtTrackerXiu2CdnHttpCheckBox)
            ];
        }

        private static string GetTrackerSourceUrl(CheckBox checkBox)
        {
            return checkBox?.Tag?.ToString() ?? string.Empty;
        }

        private void UpdateTrackerSourceSummary()
        {
            if (BtTrackerSourceSummaryText is null)
            {
                return;
            }

            List<string> selectedLabels = [];
            foreach ((string label, _, CheckBox checkBox) in GetBuiltInTrackerSourceCheckBoxes())
            {
                if (checkBox?.IsChecked == true)
                {
                    selectedLabels.Add(label);
                }
            }

            foreach (object item in BtCustomTrackerSourceListView.SelectedItems)
            {
                if (item?.ToString() is string url && IsValidHttpUrl(url))
                {
                    selectedLabels.Add(new Uri(url).Host);
                }
            }

            BtTrackerSourceSummaryText.Text = selectedLabels.Count switch
            {
                0 => "选择 Tracker 来源",
                1 => selectedLabels[0],
                <= 3 => string.Join(", ", selectedLabels),
                _ => $"已选择 {selectedLabels.Count} 个来源"
            };
        }

        private string[] GetCustomTrackerUrls()
        {
            List<string> urls = [];
            foreach (object item in BtCustomTrackerSourceListView.Items)
            {
                if (item?.ToString() is string url && IsValidHttpUrl(url))
                {
                    urls.Add(url);
                }
            }

            return GetNormalizedUrls(urls);
        }

        private static string[] NormalizeTrackerSourceUrls(string[]? selectedUrls, string? legacyUrl, string[]? customUrls)
        {
            string[] normalized = GetNormalizedUrls(selectedUrls);
            if (normalized.Length > 0)
            {
                return normalized;
            }

            return IsValidHttpUrl(legacyUrl ?? string.Empty)
                ? [legacyUrl!.Trim()]
                : [BitTorrentSettings.DefaultTrackerSourceUrl];
        }

        private static string[] GetNormalizedUrls(IEnumerable<string>? urls)
        {
            if (urls is null)
            {
                return [];
            }

            return urls
                .Select(url => url.Trim())
                .Where(IsValidHttpUrl)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static void SetCheckBox(CheckBox checkBox, bool isChecked)
        {
            if (checkBox is not null)
            {
                checkBox.IsChecked = isChecked;
            }
        }

        private bool IsKeepSeedingSelected()
        {
            return BtSeedingModeComboBox?.SelectedItem is ComboBoxItem item &&
                item.Tag?.ToString()?.Equals("Always", StringComparison.OrdinalIgnoreCase) == true;
        }

        private void SetSeedingModeSelection(bool keepSeeding)
        {
            if (BtSeedingModeComboBox is null)
            {
                return;
            }

            string mode = keepSeeding ? "Always" : "Limited";
            for (int index = 0; index < BtSeedingModeComboBox.Items.Count; index++)
            {
                if (BtSeedingModeComboBox.Items[index] is ComboBoxItem item &&
                    item.Tag?.ToString()?.Equals(mode, StringComparison.OrdinalIgnoreCase) == true)
                {
                    BtSeedingModeComboBox.SelectedIndex = index;
                    return;
                }
            }

            BtSeedingModeComboBox.SelectedIndex = 0;
        }

        private void UpdateBitTorrentDependentUi()
        {
            const bool isEnabled = true;
            bool keepSeeding = IsKeepSeedingSelected();

            BtAutoDownloadToggleSwitch.IsEnabled = isEnabled;
            BtForceEncryptionToggleSwitch.IsEnabled = isEnabled;
            BtSeedingModeComboBox.IsEnabled = isEnabled;
            BtSeedRatioNumberBox.IsEnabled = isEnabled && !keepSeeding;
            BtSeedTimeNumberBox.IsEnabled = isEnabled && !keepSeeding;
            BtMaxPeersNumberBox.IsEnabled = isEnabled;
            BtTrackerSourceDropDownButton.IsEnabled = isEnabled;
            foreach ((_, _, CheckBox checkBox) in GetBuiltInTrackerSourceCheckBoxes())
            {
                checkBox.IsEnabled = isEnabled;
            }

            BtCustomTrackerSourceTextBox.IsEnabled = isEnabled;
            BtCustomTrackerSourceListView.IsEnabled = isEnabled;
            BtTrackerSourceTextBox.IsEnabled = isEnabled;
            BtSyncTrackerButton.IsEnabled = isEnabled;
            BtTrackerListTextBox.IsEnabled = isEnabled;
            BtAutoSyncTrackerToggleSwitch.IsEnabled = isEnabled;
        }

        private void UpdateTrackerSyncTimeText(long lastSyncTime)
        {
            if (BtLastTrackerSyncText is null)
            {
                return;
            }

            BtLastTrackerSyncText.Text = lastSyncTime <= 0
                ? "尚未同步 Tracker。"
                : $"上次同步：{DateTimeOffset.FromUnixTimeMilliseconds(lastSyncTime).LocalDateTime:g}";
        }

        private static BitTorrentSettings NormalizeBitTorrentSettings(BitTorrentSettings settings)
        {
            string listenPort = string.IsNullOrWhiteSpace(settings.ListenPort)
                ? BitTorrentSettings.Default.ListenPort
                : settings.ListenPort.Trim();
            string sourceUrl = string.IsNullOrWhiteSpace(settings.TrackerSourceUrl)
                ? BitTorrentSettings.DefaultTrackerSourceUrl
                : settings.TrackerSourceUrl.Trim();

            return settings with
            {
                IsEnabled = true,
                SeedRatio = Math.Clamp(settings.SeedRatio, 0, 100),
                SeedTimeMinutes = Math.Clamp(settings.SeedTimeMinutes, 0, 525600),
                MaxPeers = Math.Clamp(settings.MaxPeers, 1, 1000),
                ListenPort = listenPort,
                TrackerSourceUrl = sourceUrl,
                SelectedTrackerSourceUrls = NormalizeTrackerSourceUrls(settings.SelectedTrackerSourceUrls, sourceUrl, settings.CustomTrackerUrls),
                CustomTrackerUrls = GetNormalizedUrls(settings.CustomTrackerUrls),
                TrackerList = NormalizeTrackerList(settings.TrackerList)
            };
        }

        private static string NormalizeTrackerList(string? trackerList)
        {
            if (string.IsNullOrWhiteSpace(trackerList))
            {
                return string.Empty;
            }

            return string.Join(Environment.NewLine, trackerList
                .Replace(",", "\n", StringComparison.Ordinal)
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(line => !line.StartsWith('#') && IsLikelyTrackerUrl(line))
                .Distinct(StringComparer.OrdinalIgnoreCase));
        }

        private static bool IsLikelyTrackerUrl(string value)
        {
            return value.StartsWith("udp://", StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith("ws://", StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith("wss://", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsValidHttpUrl(string value)
        {
            return Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) &&
                (uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                    uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase));
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

        private static double GetValidDoubleNumberBoxValue(NumberBox numberBox, double minimum, double maximum, double fallback)
        {
            if (numberBox is null || double.IsNaN(numberBox.Value))
            {
                return fallback;
            }

            return Math.Clamp(numberBox.Value, minimum, maximum);
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
