using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using OmniDown.Controls;
using OmniDown.Models;
using OmniDown.Models.Settings;
using OmniDown.Services.Downloads;
using OmniDown.Services.Engine;
using OmniDown.Services.Localization;
using OmniDown.Services.Logging;
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
        private ScrollViewer SettingsContentScrollViewer => SettingsPage.SettingsContentScrollViewerControl;
        private FrameworkElement GeneralSettingsContent => SettingsPage.GeneralSettingsContentControl;
        private FrameworkElement DownloadSettingsContent => SettingsPage.DownloadSettingsContentControl;
        private FrameworkElement BitTorrentSettingsContent => SettingsPage.BitTorrentSettingsContentControl;
        private FrameworkElement NetworkSettingsContent => SettingsPage.NetworkSettingsContentControl;
        private FrameworkElement AdvancedSettingsContent => SettingsPage.AdvancedSettingsContentControl;
        private FrameworkElement AboutSettingsContent => SettingsPage.AboutSettingsContentControl;
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
        private TextBox AriaPathTextBox => SettingsPage.AriaPathTextBoxControl;
        private ComboBox EngineTypeComboBox => SettingsPage.EngineTypeComboBoxControl;
        private TextBlock EngineVersionText => SettingsPage.EngineVersionTextControl;
        private NumberBox RpcPortNumberBox => SettingsPage.RpcPortNumberBoxControl;
        private PasswordBox RpcSecretPasswordBox => SettingsPage.RpcSecretPasswordBoxControl;
        private ToggleSwitch ExtensionAutoSubmitToggleSwitch => SettingsPage.ExtensionAutoSubmitToggleSwitchControl;
        private TextBlock ExtensionAutoSubmitStateText => SettingsPage.ExtensionAutoSubmitStateTextControl;
        private NumberBox ExtensionApiPortNumberBox => SettingsPage.ExtensionApiPortNumberBoxControl;
        private PasswordBox ExtensionApiSecretPasswordBox => SettingsPage.ExtensionApiSecretPasswordBoxControl;
        private ComboBox LogLevelComboBox => SettingsPage.LogLevelComboBoxControl;
        private TextBlock AdvancedPathsSummaryText => SettingsPage.AdvancedPathsSummaryTextControl;
        private TextBlock LogPathsSummaryText => SettingsPage.LogPathsSummaryTextControl;
        private ToggleSwitch ClipboardDetectionToggleSwitch => SettingsPage.ClipboardDetectionToggleSwitchControl;
        private TextBlock ClipboardDetectionStateText => SettingsPage.ClipboardDetectionStateTextControl;
        private ToggleSwitch ClipboardHttpToggleSwitch => SettingsPage.ClipboardHttpToggleSwitchControl;
        private ToggleSwitch ClipboardFtpToggleSwitch => SettingsPage.ClipboardFtpToggleSwitchControl;
        private ToggleSwitch ClipboardMagnetToggleSwitch => SettingsPage.ClipboardMagnetToggleSwitchControl;
        private ToggleSwitch ClipboardThunderToggleSwitch => SettingsPage.ClipboardThunderToggleSwitchControl;
        private ToggleSwitch ClipboardBtHashToggleSwitch => SettingsPage.ClipboardBtHashToggleSwitchControl;
        private ToggleSwitch ProtocolMagnetToggleSwitch => SettingsPage.ProtocolMagnetToggleSwitchControl;
        private TextBlock ProtocolMagnetStateText => SettingsPage.ProtocolMagnetStateTextControl;
        private ToggleSwitch ProtocolThunderToggleSwitch => SettingsPage.ProtocolThunderToggleSwitchControl;
        private TextBlock ProtocolThunderStateText => SettingsPage.ProtocolThunderStateTextControl;
        private ToggleSwitch ProtocolOmniDownToggleSwitch => SettingsPage.ProtocolOmniDownToggleSwitchControl;
        private TextBlock ProtocolOmniDownStateText => SettingsPage.ProtocolOmniDownStateTextControl;
        private TextBlock SettingsAriaStatusText => SettingsPage.SettingsAriaStatusTextControl;
        private StackPanel ProcessStatusSettingControl => SettingsPage.ProcessStatusSettingControlControl;
        private FontIcon AriaStartStopIcon => SettingsPage.AriaStartStopIconControl;
        private Button AriaStartStopButton => SettingsPage.AriaStartStopButtonControl;
        private Button AriaRestartButton => SettingsPage.AriaRestartButtonControl;
        private TextBlock AboutVersionText => SettingsPage.AboutVersionTextControl;
        private TextBlock AboutCloneCommandText => SettingsPage.AboutCloneCommandTextControl;

        private void HookSettingsPageEvents()
        {
            SettingsPage.SectionNavigationRequested += SettingsPage_SectionNavigationRequested;
            SettingsPage.GeneralSettingChanged += SettingsPage_GeneralSettingChanged;
            SettingsPage.CloseBehaviorSettingChanged += SettingsPage_CloseBehaviorSettingChanged;
            SettingsPage.BrowseDownloadDirectoryRequested += BrowseDownloadDirectoryButton_Click;
            SettingsPage.DownloadSettingChanged += DownloadSetting_Changed;
            SettingsPage.BitTorrentSettingChanged += BitTorrentSetting_Changed;
            SettingsPage.NetworkSettingChanged += NetworkSetting_Changed;
            SettingsPage.DetectSystemProxyRequested += DetectSystemProxyButton_Click;
            SettingsPage.RandomBtPortRequested += RandomBtPortButton_Click;
            SettingsPage.RandomDhtPortRequested += RandomDhtPortButton_Click;
            SettingsPage.UserAgentPresetRequested += UserAgentPresetButton_Click;
            SettingsPage.AdvancedSettingChanged += AdvancedSetting_Changed;
            SettingsPage.BrowseAriaPathRequested += BrowseAriaPathButton_Click;
            SettingsPage.CopyRpcSecretRequested += CopyRpcSecretButton_Click;
            SettingsPage.GenerateRpcSecretRequested += GenerateRpcSecretButton_Click;
            SettingsPage.CopyExtensionApiSecretRequested += CopyExtensionApiSecretButton_Click;
            SettingsPage.GenerateExtensionApiSecretRequested += GenerateExtensionApiSecretButton_Click;
            SettingsPage.OpenConfigFolderRequested += OpenConfigFolderButton_Click;
            SettingsPage.OpenLogFolderRequested += OpenLogFolderButton_Click;
            SettingsPage.ClearSessionRequested += ClearSessionButton_Click;
            SettingsPage.AddBtCustomTrackerRequested += AddBtCustomTrackerButton_Click;
            SettingsPage.SyncBtTrackerRequested += SyncBtTrackerButton_Click;
            SettingsPage.StartStopAriaRequested += StartStopAriaButton_Click;
            SettingsPage.RestartAriaRequested += RestartAriaButton_Click;
            SettingsPage.ManualUpdateRequested += ManualEngineUpdateButton_Click;
            SettingsPage.CopyCloneCommandRequested += CopyCloneCommandButton_Click;
            SettingsPage.OpenAboutLinkRequested += OpenAboutLinkButton_Click;
        }

        private void UseSystemProxyCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isLoadingNetworkSettings)
            {
                ShowSettingsSaveTeachingTip();
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
            ShowSettingsSaveTeachingTip();
        }

        private void DownloadSetting_Changed(object sender, RoutedEventArgs e)
        {
            if (_isLoadingDownloadSettings)
            {
                return;
            }

            ShowSettingsSaveTeachingTip();
        }

        private void BitTorrentSetting_Changed(object sender, RoutedEventArgs e)
        {
            if (_isLoadingBitTorrentSettings)
            {
                return;
            }

            UpdateBitTorrentDependentUi();
            UpdateTrackerSourceSummary();
            ShowSettingsSaveTeachingTip();
        }

        private void AddBtCustomTrackerButton_Click(object sender, RoutedEventArgs e)
        {
            AddCustomTrackerSource();
        }

        private async void SyncBtTrackerButton_Click(object sender, RoutedEventArgs e)
        {
            await SyncBitTorrentTrackersAsync();
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

        private async Task ApplyRuntimeDownloadSettingsAsync(DownloadSettings settings)
        {
            if (!_aria2EngineHost.IsRunning)
            {
                return;
            }

            try
            {
                await _downloadCoordinator.SetGlobalDownloadSettingsAsync(settings.MaxConcurrentDownloads);
            }
            catch (Exception ex)
            {
                ShowMessage($"同步下载设置到 aria2 失败：{ex.Message}", InfoBarSeverity.Warning);
            }
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
                SettingsPage.ApplyCloseBehaviorSettings(_settingsPageViewModel.CloseBehaviorSettings);
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
                SettingsPage.ApplyGeneralSettings(_settingsPageViewModel.GeneralSettings, _autoStartService.IsEnabled());
                ApplyThemeSetting(_settingsPageViewModel.GeneralSettings.Theme);
            }
            finally
            {
                _isLoadingGeneralSettings = false;
            }
        }

        private void UpdateGeneralSettingsFromUi()
        {
            _settingsPageViewModel.UpdateGeneralSettings(SettingsPage.GetGeneralSettings(_settingsPageViewModel.GeneralSettings));
        }

        private void ApplyGeneralSettingsSideEffects(GeneralSettingChangeKind changeKind)
        {
            if (changeKind == GeneralSettingChangeKind.AutoStart)
            {
                _ = SetAutoStartEnabledAsync(SettingsPage.IsAutoStartEnabled);
            }

            if (changeKind == GeneralSettingChangeKind.ShowTaskbarProgress)
            {
                UpdateTaskbarProgressFromTasks();
            }

            if (changeKind == GeneralSettingChangeKind.PreventSleepWhileDownloading)
            {
                UpdateSystemSleepOverride();
            }
        }

        private void SettingsPage_GeneralSettingChanged(object? sender, GeneralSettingChangedEventArgs args)
        {
            if (_isLoadingGeneralSettings)
            {
                return;
            }

            UpdateGeneralSettingsFromUi();
            if (args.Kind == GeneralSettingChangeKind.Theme)
            {
                ApplyThemeSetting(_settingsPageViewModel.GeneralSettings.Theme);
            }

            SaveGeneralSettings();
            ApplyGeneralSettingsSideEffects(args.Kind);
        }

        private void SetToggleSwitch(ToggleSwitch? toggleSwitch, bool isOn)
        {
            if (toggleSwitch is null)
            {
                return;
            }

            toggleSwitch.IsOn = isOn;
        }

        private void SettingsPage_CloseBehaviorSettingChanged(object? sender, CloseBehaviorSettingChangedEventArgs args)
        {
            if (_isLoadingCloseBehaviorSettings)
            {
                return;
            }

            _settingsPageViewModel.UpdateCloseBehavior(args.MinimizeToTrayOnClose);
            SaveCloseBehaviorSettings();
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
                SettingsPage.SetAutoStartEnabled(_autoStartService.IsEnabled());
            }
            catch
            {
                SettingsPage.SetAutoStartEnabled(false);
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
            ShowSettingsSaveTeachingTip();

            if (ReferenceEquals(sender, UseSystemProxyCheckBox))
            {
                UpdateDebugStatus();
            }
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
            ShowSettingsSaveTeachingTip();
            ShowMessage("已填入系统代理。", InfoBarSeverity.Success);
        }

        private void RandomBtPortButton_Click(object sender, RoutedEventArgs e)
        {
            BtListenPortNumberBox.Value = Random.Shared.Next(20000, 25000);
            ShowSettingsSaveTeachingTip();
        }

        private void RandomDhtPortButton_Click(object sender, RoutedEventArgs e)
        {
            DhtListenPortNumberBox.Value = Random.Shared.Next(25000, 30000);
            ShowSettingsSaveTeachingTip();
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
            ShowSettingsSaveTeachingTip();
        }

        private void LoadAdvancedSettings()
        {
            _settingsPageViewModel.LoadAdvancedSettings();
            AppLogger.Configure(_settingsPageViewModel.AdvancedSettings.LogLevel);
            _isLoadingAdvancedSettings = true;
            try
            {
                ApplyAdvancedSettingsToUi();
            }
            finally
            {
                _isLoadingAdvancedSettings = false;
            }
        }

        private void ApplyAdvancedSettingsToUi()
        {
            AdvancedSettings settings = _settingsPageViewModel.AdvancedSettings;
            AriaPathTextBox.Text = settings.Aria2Path;
            SetEngineTypeSelection(settings.EngineType);
            RpcPortNumberBox.Value = settings.RpcPort;
            RpcSecretPasswordBox.Password = settings.RpcSecret;
            SetToggleSwitch(ExtensionAutoSubmitToggleSwitch, settings.AutoSubmitFromExtension);
            ExtensionApiPortNumberBox.Value = settings.ExtensionApiPort;
            ExtensionApiSecretPasswordBox.Password = settings.ExtensionApiSecret;
            SetLogLevelSelection(settings.LogLevel);
            SetToggleSwitch(ClipboardDetectionToggleSwitch, settings.ClipboardDetectionEnabled);
            SetToggleSwitch(ClipboardHttpToggleSwitch, settings.ClipboardHttpEnabled);
            SetToggleSwitch(ClipboardFtpToggleSwitch, settings.ClipboardFtpEnabled);
            SetToggleSwitch(ClipboardMagnetToggleSwitch, settings.ClipboardMagnetEnabled);
            SetToggleSwitch(ClipboardThunderToggleSwitch, settings.ClipboardThunderEnabled);
            SetToggleSwitch(ClipboardBtHashToggleSwitch, settings.ClipboardBtHashEnabled);
            ProtocolAssociationService.Synchronize(
                settings.ProtocolMagnetEnabled,
                settings.ProtocolThunderEnabled,
                settings.ProtocolOmniDownEnabled);
            RefreshProtocolDefaultToggles();
            AdvancedPathsSummaryText.Text = $"保存设置、任务缓存和 {Path.GetFileName(GetAriaSessionPath())}。";
            LogPathsSummaryText.Text = $"保存 {Path.GetFileName(AppPaths.AppLogPath)} 和 {Path.GetFileName(AppPaths.Aria2LogPath)}。";
            _rpcSecret = settings.RpcSecret;
            UpdateClipboardTypeControls();
        }

        private async void AdvancedSetting_Changed(object sender, RoutedEventArgs e)
        {
            if (_isLoadingAdvancedSettings)
            {
                return;
            }

            ShowSettingsSaveTeachingTip();
            UpdateClipboardTypeControls();

            if (ReferenceEquals(sender, EngineTypeComboBox) ||
                ReferenceEquals(sender, AriaPathTextBox))
            {
                await RefreshEngineVersionAsync();
            }

            if (sender is ToggleSwitch toggleSwitch &&
                toggleSwitch.IsOn &&
                (ReferenceEquals(toggleSwitch, ProtocolMagnetToggleSwitch) ||
                    ReferenceEquals(toggleSwitch, ProtocolThunderToggleSwitch) ||
                    ReferenceEquals(toggleSwitch, ProtocolOmniDownToggleSwitch)))
            {
                _ = Launcher.LaunchUriAsync(new Uri("ms-settings:defaultapps"));
                ShowMessage("已注册协议入口。若浏览器仍未打开 OmniDown，请在 Windows 默认应用中把该协议设为 OmniDown。", InfoBarSeverity.Informational);
            }
        }

        private async Task RefreshEngineVersionAsync()
        {
            try
            {
                await _aria2EngineHost.DetectVersionAsync(
                    string.IsNullOrWhiteSpace(AriaPathTextBox.Text) ? null : AriaPathTextBox.Text.Trim(),
                    GetSelectedEngineType());
            }
            catch
            {
                // Best-effort version detection.
            }

            EngineVersionText.Text = string.IsNullOrEmpty(_aria2EngineHost.EngineVariant)
                ? "未检测"
                : _aria2EngineHost.EngineVariant;
        }

        private async void BrowseAriaPathButton_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker picker = new()
            {
                SuggestedStartLocation = PickerLocationId.ComputerFolder
            };
            picker.FileTypeFilter.Add(".exe");
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));

            StorageFile? file = await picker.PickSingleFileAsync();
            if (file is null)
            {
                return;
            }

            AriaPathTextBox.Text = file.Path;
            ShowSettingsSaveTeachingTip();
        }

        private void CopyRpcSecretButton_Click(object sender, RoutedEventArgs e)
        {
            CopyTextToClipboard(RpcSecretPasswordBox.Password);
            ShowMessage("RPC 密钥已复制。", InfoBarSeverity.Success);
        }

        private void GenerateRpcSecretButton_Click(object sender, RoutedEventArgs e)
        {
            RpcSecretPasswordBox.Password = AdvancedSettings.GenerateSecret();
            ShowSettingsSaveTeachingTip();
        }

        private void CopyExtensionApiSecretButton_Click(object sender, RoutedEventArgs e)
        {
            CopyTextToClipboard(ExtensionApiSecretPasswordBox.Password);
            ShowMessage("扩展 API 密钥已复制。", InfoBarSeverity.Success);
        }

        private void GenerateExtensionApiSecretButton_Click(object sender, RoutedEventArgs e)
        {
            ExtensionApiSecretPasswordBox.Password = AdvancedSettings.GenerateSecret();
            ShowSettingsSaveTeachingTip();
        }

        private async void OpenConfigFolderButton_Click(object sender, RoutedEventArgs e)
        {
            Directory.CreateDirectory(AppPaths.LocalDataDirectory);
            await Launcher.LaunchFolderPathAsync(AppPaths.LocalDataDirectory);
        }

        private async void OpenLogFolderButton_Click(object sender, RoutedEventArgs e)
        {
            Directory.CreateDirectory(AppPaths.LogDirectory);
            await Launcher.LaunchFolderPathAsync(AppPaths.LogDirectory);
        }

        private async void ClearSessionButton_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog dialog = new()
            {
                XamlRoot = Content.XamlRoot,
                Title = "清空 aria2 会话？",
                Content = "这会删除本地 download.session 和 tasks.json。正在运行的下载不会被删除，但建议先停止 aria2 后再清空。",
                PrimaryButtonText = "清空",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Close
            };

            if (await dialog.ShowAsync() != ContentDialogResult.Primary)
            {
                return;
            }

            try
            {
                string sessionPath = GetAriaSessionPath();
                if (File.Exists(sessionPath))
                {
                    File.Delete(sessionPath);
                }

                _downloadCoordinator.ClearTaskCache();
                ApplyTaskFilter(_currentTaskFilter);
                UpdateDashboard();
                UpdateTaskDetailsPane();
                UpdateTaskbarProgressFromTasks();
                UpdateSystemSleepOverride();

                ShowMessage("aria2 会话和任务缓存已清空。", InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                ShowMessage($"清空会话失败：{ex.Message}", InfoBarSeverity.Error);
            }
        }

        private void SaveAdvancedSettings()
        {
            if (_isLoadingAdvancedSettings)
            {
                return;
            }

            AdvancedSettings settings = GetAdvancedSettingsFromUi();

            _settingsPageViewModel.SaveAdvancedSettings(settings);
            AppLogger.Configure(_settingsPageViewModel.AdvancedSettings.LogLevel);
            ProtocolAssociationService.Synchronize(
                settings.ProtocolMagnetEnabled,
                settings.ProtocolThunderEnabled,
                settings.ProtocolOmniDownEnabled);
            _rpcSecret = _settingsPageViewModel.AdvancedSettings.RpcSecret;
            _aria2RpcClient.Configure(_settingsPageViewModel.AdvancedSettings.RpcPort, _rpcSecret);
            RestartBrowserExtensionApiServer();
            UpdateClipboardTypeControls();
            UpdateAriaRestartNotification();
        }

        private AdvancedSettings GetAdvancedSettingsFromUi()
        {
            return new AdvancedSettings(
                AriaPathTextBox.Text.Trim(),
                GetSelectedEngineType(),
                GetValidIntNumberBoxValue(RpcPortNumberBox, 1024, 65535, AdvancedSettings.Default.RpcPort),
                string.IsNullOrWhiteSpace(RpcSecretPasswordBox.Password)
                    ? AdvancedSettings.GenerateSecret()
                    : RpcSecretPasswordBox.Password.Trim(),
                ExtensionAutoSubmitToggleSwitch?.IsOn == true,
                GetValidIntNumberBoxValue(ExtensionApiPortNumberBox, 1024, 65535, AdvancedSettings.Default.ExtensionApiPort),
                string.IsNullOrWhiteSpace(ExtensionApiSecretPasswordBox.Password)
                    ? AdvancedSettings.GenerateSecret()
                    : ExtensionApiSecretPasswordBox.Password.Trim(),
                GetSelectedLogLevel(),
                ClipboardDetectionToggleSwitch?.IsOn == true,
                ClipboardHttpToggleSwitch?.IsOn == true,
                ClipboardFtpToggleSwitch?.IsOn == true,
                ClipboardMagnetToggleSwitch?.IsOn == true,
                ClipboardThunderToggleSwitch?.IsOn == true,
                ClipboardBtHashToggleSwitch?.IsOn == true,
                ProtocolMagnetToggleSwitch?.IsOn == true,
                ProtocolThunderToggleSwitch?.IsOn == true,
                ProtocolOmniDownToggleSwitch?.IsOn == true);
        }

        private void ShowSettingsSaveTeachingTip()
        {
            if (_pendingAriaSettingsRollback is null)
            {
                _pendingAriaSettingsRollback = CaptureCurrentAriaSettings();
            }

            if (!HasPendingAriaSettingsChanges())
            {
                _pendingAriaSettingsRollback = null;
                SettingsSaveTeachingTip.IsOpen = false;
                return;
            }

            AriaRestartTeachingTip.IsOpen = false;
            SettingsSaveTeachingTip.Title = Strings.Get("SettingsSaveTeachingTipTitle");
            bool requiresRestart = PendingAriaSettingsRequireRestart();
            SettingsSaveTeachingTip.Subtitle = requiresRestart
                ? Strings.Get("SettingsSaveRestartTeachingTipSubtitle")
                : Strings.Get("SettingsSaveTeachingTipSubtitle");
            SettingsSaveTeachingTip.ActionButtonContent = requiresRestart
                ? Strings.Get("SettingsSaveRestartTeachingTipActionButtonContent")
                : Strings.Get("SettingsSaveTeachingTipActionButtonContent");
            SettingsSaveTeachingTip.CloseButtonContent = Strings.Get("TeachingTipCancelButtonContent");
            SettingsSaveTeachingTip.IsOpen = true;
        }

        private async void SettingsSaveTeachingTip_ActionButtonClick(TeachingTip sender, object args)
        {
            AriaRelatedSettingsSnapshot rollback = _pendingAriaSettingsRollback ?? CaptureCurrentAriaSettings();
            bool requiresRestart = PendingAriaSettingsRequireRestart();
            _isSavingAriaSettings = true;
            try
            {
                if (!SaveNetworkSettings())
                {
                    SettingsSaveTeachingTip.IsOpen = true;
                    return;
                }

                SaveDownloadSettings();
                SaveBitTorrentSettings();
                SaveAdvancedSettings();
            }
            finally
            {
                _isSavingAriaSettings = false;
            }

            _restartAriaSettingsRollback = rollback;
            _pendingAriaSettingsRollback = null;
            SettingsSaveTeachingTip.IsOpen = false;
            if (requiresRestart && _aria2EngineHost.IsRunning)
            {
                _restartAriaSettingsRollback = null;
                await StopAriaAsync(showMessage: false);
                await StartAriaAsync();
            }
            else
            {
                UpdateAriaRestartNotification();
            }
        }

        private void SettingsSaveTeachingTip_CloseButtonClick(TeachingTip sender, object args)
        {
            CancelPendingAriaSettingsChanges();
        }

        private async void AriaRestartTeachingTip_ActionButtonClick(TeachingTip sender, object args)
        {
            AriaRestartTeachingTip.IsOpen = false;
            _restartAriaSettingsRollback = null;
            await StopAriaAsync(showMessage: false);
            await StartAriaAsync();
        }

        private void AriaRestartTeachingTip_CloseButtonClick(TeachingTip sender, object args)
        {
            if (_restartAriaSettingsRollback is not null)
            {
                RestoreAriaSettingsSnapshot(_restartAriaSettingsRollback, save: true);
            }

            _restartAriaSettingsRollback = null;
            AriaRestartTeachingTip.IsOpen = false;
        }

        private void CancelPendingAriaSettingsChanges()
        {
            if (_pendingAriaSettingsRollback is not null)
            {
                RestoreAriaSettingsSnapshot(_pendingAriaSettingsRollback, save: false);
            }

            _pendingAriaSettingsRollback = null;
            SettingsSaveTeachingTip.IsOpen = false;
        }

        private void DismissSettingsTeachingTips()
        {
            CancelPendingAriaSettingsChanges();
            AriaRestartTeachingTip.IsOpen = false;
        }

        private AriaRelatedSettingsSnapshot CaptureCurrentAriaSettings()
        {
            return new AriaRelatedSettingsSnapshot(
                _settingsPageViewModel.DownloadSettings,
                _settingsPageViewModel.BitTorrentSettings,
                _settingsPageViewModel.NetworkSettings,
                _settingsPageViewModel.AdvancedSettings);
        }

        private bool PendingAriaSettingsRequireRestart()
        {
            if (!_aria2EngineHost.IsRunning)
            {
                return false;
            }

            string pendingSignature = CreateAriaRestartSettingsSignature(
                GetDownloadSettingsFromUi(),
                GetBitTorrentSettingsFromUi(),
                GetNetworkSettingsFromUi(),
                GetAdvancedSettingsFromUi());

            return !string.Equals(pendingSignature, _runningAriaSettingsSignature, StringComparison.Ordinal);
        }

        private bool HasPendingAriaSettingsChanges()
        {
            if (_pendingAriaSettingsRollback is null)
            {
                return false;
            }

            return GetDownloadSettingsFromUi() != _pendingAriaSettingsRollback.Download ||
                !BitTorrentSettingsEqual(GetBitTorrentSettingsFromUi(), _pendingAriaSettingsRollback.BitTorrent) ||
                GetNetworkSettingsFromUi() != _pendingAriaSettingsRollback.Network ||
                GetAdvancedSettingsFromUi() != _pendingAriaSettingsRollback.Advanced;
        }

        private static bool BitTorrentSettingsEqual(BitTorrentSettings left, BitTorrentSettings right)
        {
            return left.IsEnabled == right.IsEnabled &&
                left.AutoDownloadContent == right.AutoDownloadContent &&
                left.ForceEncryption == right.ForceEncryption &&
                left.KeepSeeding == right.KeepSeeding &&
                Math.Abs(left.SeedRatio - right.SeedRatio) < 0.0001 &&
                left.SeedTimeMinutes == right.SeedTimeMinutes &&
                left.MaxPeers == right.MaxPeers &&
                string.Equals(left.ListenPort, right.ListenPort, StringComparison.Ordinal) &&
                string.Equals(left.TrackerSourceUrl, right.TrackerSourceUrl, StringComparison.Ordinal) &&
                left.SelectedTrackerSourceUrls.SequenceEqual(right.SelectedTrackerSourceUrls, StringComparer.OrdinalIgnoreCase) &&
                left.CustomTrackerUrls.SequenceEqual(right.CustomTrackerUrls, StringComparer.OrdinalIgnoreCase) &&
                string.Equals(NormalizeTrackerList(left.TrackerList), NormalizeTrackerList(right.TrackerList), StringComparison.Ordinal) &&
                left.AutoSyncTracker == right.AutoSyncTracker &&
                left.LastSyncTrackerTime == right.LastSyncTrackerTime;
        }

        private void RestoreAriaSettingsSnapshot(AriaRelatedSettingsSnapshot snapshot, bool save)
        {
            if (save)
            {
                _settingsPageViewModel.SaveDownloadSettings(snapshot.Download);
                _settingsPageViewModel.SaveBitTorrentSettings(snapshot.BitTorrent);
                _settingsPageViewModel.SaveNetworkSettings(snapshot.Network);
                _settingsPageViewModel.SaveAdvancedSettings(snapshot.Advanced);
            }

            _isLoadingDownloadSettings = true;
            _isLoadingBitTorrentSettings = true;
            _isLoadingNetworkSettings = true;
            _isLoadingAdvancedSettings = true;
            try
            {
                if (!save)
                {
                    _settingsPageViewModel.SaveDownloadSettings(snapshot.Download);
                    _settingsPageViewModel.SaveBitTorrentSettings(snapshot.BitTorrent);
                    _settingsPageViewModel.SaveNetworkSettings(snapshot.Network);
                    _settingsPageViewModel.SaveAdvancedSettings(snapshot.Advanced);
                }

                ApplyDownloadSettingsToUi();
                ApplyBitTorrentSettingsToUi();
                ApplyNetworkSettingsToUi();
                ApplyAdvancedSettingsToUi();
            }
            finally
            {
                _isLoadingDownloadSettings = false;
                _isLoadingBitTorrentSettings = false;
                _isLoadingNetworkSettings = false;
                _isLoadingAdvancedSettings = false;
            }

            _aria2RpcClient.Configure(_settingsPageViewModel.AdvancedSettings.RpcPort, _settingsPageViewModel.AdvancedSettings.RpcSecret);
            UpdateAriaRestartNotification();
        }

        private void UpdateAriaRestartNotification()
        {
            if (_isSavingAriaSettings ||
                !_aria2EngineHost.IsRunning ||
                string.IsNullOrWhiteSpace(_runningAriaSettingsSignature))
            {
                return;
            }

            string currentSignature = CreateAriaRestartSettingsSignature();
            if (string.Equals(currentSignature, _runningAriaSettingsSignature, StringComparison.Ordinal))
            {
                if (_statusToastActionRestartsAria)
                {
                    _statusToastActionRestartsAria = false;
                    AnimateInfoBarHide();
                }

                AriaRestartTeachingTip.IsOpen = false;
                _restartAriaSettingsRollback = null;
                return;
            }

            ShowAriaRestartNotification();
        }

        private void ShowAriaRestartNotification()
        {
            string message = Strings.Get("AriaRestartRequiredMessage");
            if (AriaRestartTeachingTip.IsOpen)
            {
                return;
            }

            _statusMessages.Insert(0, new AppStatusMessage(
                message,
                FormatStatusMessageDetail(DateTimeOffset.Now),
                GetSeverityText(InfoBarSeverity.Warning),
                GetSeverityGlyph(InfoBarSeverity.Warning),
                GetSeverityBrush(InfoBarSeverity.Warning)));
            AriaRestartTeachingTip.Title = Strings.Get("AriaRestartRequiredTitle");
            AriaRestartTeachingTip.Subtitle = message;
            AriaRestartTeachingTip.ActionButtonContent = Strings.Get("AriaRestartTeachingTipActionButtonContent");
            AriaRestartTeachingTip.CloseButtonContent = Strings.Get("TeachingTipCancelButtonContent");
            SettingsSaveTeachingTip.IsOpen = false;
            AriaRestartTeachingTip.IsOpen = true;
            AppLogger.Warning("UI", message);
        }

        private string CreateAriaRestartSettingsSignature()
        {
            return CreateAriaRestartSettingsSignature(
                _settingsPageViewModel.DownloadSettings,
                _settingsPageViewModel.BitTorrentSettings,
                _settingsPageViewModel.NetworkSettings,
                _settingsPageViewModel.AdvancedSettings);
        }

        private string CreateAriaRestartSettingsSignature(
            DownloadSettings download,
            BitTorrentSettings bitTorrent,
            NetworkSettings network,
            AdvancedSettings advanced)
        {

            string[] values =
            [
                download.DownloadDirectory,
                download.SplitCount.ToString(CultureInfo.InvariantCulture),
                download.MaxConnectionPerServer.ToString(CultureInfo.InvariantCulture),
                download.ContinueDownloads.ToString(CultureInfo.InvariantCulture),
                download.RemoteTime.ToString(CultureInfo.InvariantCulture),
                download.MaxTries.ToString(CultureInfo.InvariantCulture),
                download.RetryWaitSeconds.ToString(CultureInfo.InvariantCulture),
                network.UseSystemProxy.ToString(CultureInfo.InvariantCulture),
                network.CustomProxyEnabled.ToString(CultureInfo.InvariantCulture),
                network.ProxyServer,
                network.ProxyBypass,
                network.ProxyDownloads.ToString(CultureInfo.InvariantCulture),
                network.ListenPort.ToString(CultureInfo.InvariantCulture),
                network.DhtListenPort.ToString(CultureInfo.InvariantCulture),
                network.UserAgent,
                network.ConnectTimeoutSeconds.ToString(CultureInfo.InvariantCulture),
                network.TimeoutSeconds.ToString(CultureInfo.InvariantCulture),
                network.FileAllocation,
                bitTorrent.AutoDownloadContent.ToString(CultureInfo.InvariantCulture),
                bitTorrent.ForceEncryption.ToString(CultureInfo.InvariantCulture),
                bitTorrent.KeepSeeding.ToString(CultureInfo.InvariantCulture),
                bitTorrent.SeedRatio.ToString("0.###", CultureInfo.InvariantCulture),
                bitTorrent.SeedTimeMinutes.ToString(CultureInfo.InvariantCulture),
                bitTorrent.MaxPeers.ToString(CultureInfo.InvariantCulture),
                NormalizeTrackerList(bitTorrent.TrackerList),
                advanced.Aria2Path,
                advanced.EngineType.ToString(),
                advanced.RpcPort.ToString(CultureInfo.InvariantCulture),
                advanced.RpcSecret,
                advanced.LogLevel
            ];

            return string.Join('\u001F', values.Select(value => value.Replace('\u001F', ' ')));
        }

        private void SetLogLevelSelection(string logLevel)
        {
            string normalized = string.IsNullOrWhiteSpace(logLevel) ? AdvancedSettings.Default.LogLevel : logLevel.Trim();
            for (int index = 0; index < LogLevelComboBox.Items.Count; index++)
            {
                if (LogLevelComboBox.Items[index] is ComboBoxItem item &&
                    item.Tag?.ToString()?.Equals(normalized, StringComparison.OrdinalIgnoreCase) == true)
                {
                    LogLevelComboBox.SelectedIndex = index;
                    return;
                }
            }

            LogLevelComboBox.SelectedIndex = 2;
        }

        private void SetEngineTypeSelection(Aria2EngineType engineType)
        {
            string tag = engineType.ToString();
            for (int index = 0; index < EngineTypeComboBox.Items.Count; index++)
            {
                if (EngineTypeComboBox.Items[index] is ComboBoxItem item &&
                    item.Tag?.ToString()?.Equals(tag, StringComparison.OrdinalIgnoreCase) == true)
                {
                    EngineTypeComboBox.SelectedIndex = index;
                    SettingsPage.UpdateAriaPathVisibility();
                    return;
                }
            }

            EngineTypeComboBox.SelectedIndex = 1;
            SettingsPage.UpdateAriaPathVisibility();
        }

        private Aria2EngineType GetSelectedEngineType()
        {
            return EngineTypeComboBox?.SelectedItem is ComboBoxItem item &&
                item.Tag?.ToString() is string tag &&
                Enum.TryParse(tag, ignoreCase: true, out Aria2EngineType engineType)
                ? engineType
                : Aria2EngineType.Aria2Next;
        }

        private string GetSelectedLogLevel()
        {
            return LogLevelComboBox?.SelectedItem is ComboBoxItem item
                ? item.Tag?.ToString() ?? AdvancedSettings.Default.LogLevel
                : AdvancedSettings.Default.LogLevel;
        }

        private void UpdateClipboardTypeControls()
        {
            bool enabled = ClipboardDetectionToggleSwitch?.IsOn == true;
            ClipboardHttpToggleSwitch.IsEnabled = enabled;
            ClipboardFtpToggleSwitch.IsEnabled = enabled;
            ClipboardMagnetToggleSwitch.IsEnabled = enabled;
            ClipboardThunderToggleSwitch.IsEnabled = enabled;
            ClipboardBtHashToggleSwitch.IsEnabled = enabled;
        }

        private void RefreshProtocolDefaultToggles()
        {
            AdvancedSettings settings = _settingsPageViewModel.AdvancedSettings;
            SetToggleSwitch(ProtocolMagnetToggleSwitch,
                settings.ProtocolMagnetEnabled ||
                ProtocolAssociationService.IsRegistered("magnet") ||
                IsOmniDownDefaultProtocol("magnet"));
            SetToggleSwitch(ProtocolThunderToggleSwitch,
                settings.ProtocolThunderEnabled ||
                ProtocolAssociationService.IsRegistered("thunder") ||
                IsOmniDownDefaultProtocol("thunder"));
            SetToggleSwitch(ProtocolOmniDownToggleSwitch,
                settings.ProtocolOmniDownEnabled ||
                ProtocolAssociationService.IsRegistered("omnidown") ||
                IsOmniDownDefaultProtocol("omnidown"));
        }

        private static bool IsOmniDownDefaultProtocol(string protocol)
        {
            try
            {
                using RegistryKey? userChoice = Registry.CurrentUser.OpenSubKey(
                    $@"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\{protocol}\UserChoice");
                string? progId = userChoice?.GetValue("ProgId")?.ToString();
                if (string.IsNullOrWhiteSpace(progId))
                {
                    return false;
                }

                return IsOmniDownProgId(progId);
            }
            catch
            {
                return false;
            }
        }

        private static bool IsOmniDownProgId(string progId)
        {
            static bool Matches(RegistryKey? key)
            {
                if (key is null)
                {
                    return false;
                }

                string appName = key.GetValue("ApplicationName")?.ToString() ?? string.Empty;
                string appUserModelId = key.GetValue("AppUserModelID")?.ToString() ?? string.Empty;
                return appName.Contains("OmniDown", StringComparison.OrdinalIgnoreCase) ||
                    appUserModelId.Contains("4a0e4208-8be1-4e00-90d0-404bf41c73d8", StringComparison.OrdinalIgnoreCase) ||
                    appUserModelId.Contains("OmniDown", StringComparison.OrdinalIgnoreCase);
            }

            using RegistryKey? currentUserApp = Registry.CurrentUser.OpenSubKey($@"Software\Classes\{progId}\Application");
            if (Matches(currentUserApp))
            {
                return true;
            }

            using RegistryKey? classesRootApp = Registry.ClassesRoot.OpenSubKey($@"{progId}\Application");
            return Matches(classesRootApp);
        }

        private static string GetAriaSessionPath()
        {
            return Path.Combine(AppPaths.LocalDataDirectory, "download.session");
        }

        private static void CopyTextToClipboard(string text)
        {
            DataPackage package = new();
            package.SetText(text);
            Clipboard.SetContent(package);
        }

        private bool SaveNetworkSettings()
        {
            if (_isLoadingNetworkSettings)
            {
                return false;
            }

            NetworkSettings settings = GetNetworkSettingsFromUi();

            if (settings.CustomProxyEnabled &&
                !string.IsNullOrWhiteSpace(settings.ProxyServer) &&
                !IsValidProxyUrl(settings.ProxyServer))
            {
                ShowMessage("代理地址格式不正确，请使用 http:// 或 https://。", InfoBarSeverity.Warning);
                return false;
            }

            if (UserAgentTextBox.Text != settings.UserAgent)
            {
                int selectionStart = UserAgentTextBox.SelectionStart;
                UserAgentTextBox.Text = settings.UserAgent;
                UserAgentTextBox.SelectionStart = Math.Min(selectionStart, settings.UserAgent.Length);
            }

            _settingsPageViewModel.SaveNetworkSettings(settings);
            UpdateAriaRestartNotification();
            return true;
        }

        private NetworkSettings GetNetworkSettingsFromUi()
        {
            return NormalizeNetworkSettings(new NetworkSettings(
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
            BitTorrentSettings settings = GetBitTorrentSettingsFromUi();

            _settingsPageViewModel.SaveBitTorrentSettings(settings);
            UpdateTrackerSyncTimeText(settings.LastSyncTrackerTime);
            UpdateAriaRestartNotification();
        }

        private BitTorrentSettings GetBitTorrentSettingsFromUi()
        {
            string[] selectedSources = GetSelectedTrackerSourceUrls();
            return NormalizeBitTorrentSettings(new BitTorrentSettings(
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
            ShowSettingsSaveTeachingTip();
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
            SettingsPage.SetAutoStartEnabled(result.IsEnabled);
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
            DownloadSettings settings = GetDownloadSettingsFromUi();

            _settingsPageViewModel.SaveDownloadSettings(settings);
            if (_downloadCoordinator is not null)
            {
                _downloadCoordinator.DeleteTorrentAfterComplete = settings.DeleteTorrentAfterComplete;
                _ = ApplyRuntimeDownloadSettingsAsync(settings);
            }

            UpdateAriaRestartNotification();
        }

        private DownloadSettings GetDownloadSettingsFromUi()
        {
            return NormalizeDownloadSettings(new DownloadSettings(
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
