using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;
using OmniDown.Controls;
using OmniDown.Models;
using OmniDown.Models.Settings;
using OmniDown.Services.Engine;
using OmniDown.Services.Ed2k;
using OmniDown.Services.Localization;
using OmniDown.Services.Logging;
using OmniDown.Services.Rpc;
using OmniDown.Services.Settings;
using OmniDown.Services.Storage;
using OmniDown.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using WinRT.Interop;

namespace OmniDown
{
    public sealed partial class MainWindow
    {
        private FrameworkElement GeneralSettingsContent => SettingsPage.GeneralSettingsContentControl;
        private FrameworkElement DownloadSettingsContent => SettingsPage.DownloadSettingsContentControl;
        private FrameworkElement BitTorrentSettingsContent => SettingsPage.BitTorrentSettingsContentControl;
        private FrameworkElement Ed2kSettingsContent => SettingsPage.Ed2kSettingsContentControl;
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
        private Button BtSyncTrackerButton => SettingsPage.BtSyncTrackerButtonControl;
        private ToggleSwitch BtAutoSyncTrackerToggleSwitch => SettingsPage.BtAutoSyncTrackerToggleSwitchControl;
        private TextBlock BtAutoSyncTrackerStateText => SettingsPage.BtAutoSyncTrackerStateTextControl;
        private TextBlock BtLastTrackerSyncText => SettingsPage.BtLastTrackerSyncTextControl;
        private NumberBox Ed2kListenPortNumberBox => SettingsPage.Ed2kSettingsContentControl.Ed2kListenPortNumberBoxControl;
        private NumberBox Ed2kUdpListenPortNumberBox => SettingsPage.Ed2kSettingsContentControl.Ed2kUdpListenPortNumberBoxControl;
        private NumberBox Ed2kUploadSlotsNumberBox => SettingsPage.Ed2kSettingsContentControl.Ed2kUploadSlotsNumberBoxControl;
        private TextBox Ed2kServerListUrlTextBox => SettingsPage.Ed2kSettingsContentControl.Ed2kServerListUrlTextBoxControl;
        private TextBox Ed2kKadBootstrapUrlTextBox => SettingsPage.Ed2kSettingsContentControl.Ed2kKadBootstrapUrlTextBoxControl;
        private ToggleSwitch Ed2kAutoSyncToggleSwitch => SettingsPage.Ed2kSettingsContentControl.Ed2kAutoSyncToggleSwitchControl;
        private TextBlock Ed2kAutoSyncStateText => SettingsPage.Ed2kSettingsContentControl.Ed2kAutoSyncStateTextControl;
        private ComboBox Ed2kSyncIntervalComboBox => SettingsPage.Ed2kSettingsContentControl.Ed2kSyncIntervalComboBoxControl;
        private Button Ed2kSyncNowButton => SettingsPage.Ed2kSettingsContentControl.Ed2kSyncNowButtonControl;
        private TextBlock Ed2kLastSyncText => SettingsPage.Ed2kSettingsContentControl.Ed2kLastSyncTextControl;
        private TextBox Ed2kSearchKeywordTextBox => SettingsPage.Ed2kSettingsContentControl.Ed2kSearchKeywordTextBoxControl;
        private ComboBox Ed2kFileTypeComboBox => SettingsPage.Ed2kSettingsContentControl.Ed2kFileTypeComboBoxControl;
        private NumberBox Ed2kMinSourcesNumberBox => SettingsPage.Ed2kSettingsContentControl.Ed2kMinSourcesNumberBoxControl;
        private NumberBox Ed2kSearchTimeoutNumberBox => SettingsPage.Ed2kSettingsContentControl.Ed2kSearchTimeoutNumberBoxControl;
        private ToggleSwitch UseSystemProxyCheckBox => SettingsPage.UseSystemProxyCheckBoxControl;
        private TextBlock UseSystemProxyStateText => SettingsPage.UseSystemProxyStateTextControl;
        private ToggleSwitch CustomProxyToggleSwitch => SettingsPage.CustomProxyToggleSwitchControl;
        private TextBlock CustomProxyStateText => SettingsPage.CustomProxyStateTextControl;
        private TextBox ProxyServerTextBox => SettingsPage.ProxyServerTextBoxControl;
        private TextBox ProxyUsernameTextBox => SettingsPage.ProxyUsernameTextBoxControl;
        private PasswordBox ProxyPasswordBox => SettingsPage.ProxyPasswordBoxControl;
        private Button DetectSystemProxyButton => SettingsPage.DetectSystemProxyButtonControl;
        private TextBox ProxyBypassTextBox => SettingsPage.ProxyBypassTextBoxControl;
        private Button ProxyScopeDropDownButton => SettingsPage.ProxyScopeDropDownButtonControl;
        private CheckBox ProxyDownloadsCheckBox => SettingsPage.ProxyDownloadsCheckBoxControl;
        private CheckBox ProxyTrackersCheckBox => SettingsPage.ProxyTrackersCheckBoxControl;
        private ToggleSwitch EnableUpnpToggleSwitch => SettingsPage.EnableUpnpToggleSwitchControl;
        private TextBlock EnableUpnpStateText => SettingsPage.EnableUpnpStateTextControl;
        private NumberBox BtListenPortNumberBox => SettingsPage.BtListenPortNumberBoxControl;
        private NumberBox DhtListenPortNumberBox => SettingsPage.DhtListenPortNumberBoxControl;
        private ComboBox UserAgentComboBox => SettingsPage.UserAgentComboBoxControl;
        private CommunityToolkit.WinUI.Controls.SettingsCard UserAgentCustomSettingCard => SettingsPage.UserAgentCustomSettingCardControl;
        private TextBox UserAgentTextBox => SettingsPage.UserAgentTextBoxControl;
        private NumberBox ConnectTimeoutNumberBox => SettingsPage.ConnectTimeoutNumberBoxControl;
        private NumberBox TimeoutNumberBox => SettingsPage.TimeoutNumberBoxControl;
        private ComboBox FileAllocationComboBox => SettingsPage.FileAllocationComboBoxControl;
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
            SettingsPage.GeneralSettingChanged += SettingsPage_GeneralSettingChanged;
            SettingsPage.CloseBehaviorSettingChanged += SettingsPage_CloseBehaviorSettingChanged;
            SettingsPage.BrowseDownloadDirectoryRequested += BrowseDownloadDirectoryButton_Click;
            SettingsPage.DownloadSettingChanged += DownloadSetting_Changed;
            SettingsPage.BitTorrentSettingChanged += BitTorrentSetting_Changed;
            SettingsPage.Ed2kSettingChanged += Ed2kSetting_Changed;
            SettingsPage.RandomEd2kPortRequested += RandomEd2kPortButton_Click;
            SettingsPage.RandomEd2kUdpPortRequested += RandomEd2kUdpPortButton_Click;
            SettingsPage.SyncEd2kRequested += SyncEd2kButton_Click;
            SettingsPage.SearchEd2kRequested += SearchEd2kButton_Click;
            SettingsPage.DownloadEd2kSearchResultRequested += DownloadEd2kSearchResult_Click;
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
            ShowSettingsSaveTeachingTip();
        }

        private void Ed2kSetting_Changed(object sender, RoutedEventArgs e)
        {
            if (_isLoadingEd2kSettings)
            {
                return;
            }

            ShowSettingsSaveTeachingTip();
        }

        private void RandomEd2kPortButton_Click(object sender, RoutedEventArgs e)
        {
            Ed2kListenPortNumberBox.Value = Random.Shared.Next(29000, 30000);
        }

        private void RandomEd2kUdpPortButton_Click(object sender, RoutedEventArgs e)
        {
            Ed2kUdpListenPortNumberBox.Value = Random.Shared.Next(30000, 31000);
        }

        private async void SyncEd2kButton_Click(object sender, RoutedEventArgs e)
        {
            Ed2kSettings? candidate = TryGetEd2kSettingsFromUi(showValidationError: true);
            if (candidate is null)
            {
                return;
            }

            Ed2kSyncNowButton.IsEnabled = false;
            try
            {
                Ed2kBootstrapStatus status = await _ed2kBootstrapService.SyncAsync(
                    candidate.ServerListUrl,
                    candidate.KadBootstrapUrl);
                long syncTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                Ed2kSettings persisted = _settingsPageViewModel.Ed2kSettings with { LastSyncTime = syncTime };
                _settingsPageViewModel.SaveEd2kSettings(persisted);
                UpdateEd2kLastSyncText(syncTime, status);
                ShowMessage(Strings.Get("Ed2kBootstrapSyncSucceededMessage"), InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                AppLogger.Error("ED2K.BootstrapSync", ex);
                ShowMessage(Strings.Get("Ed2kBootstrapSyncFailedMessage"), InfoBarSeverity.Error, ex.Message);
            }
            finally
            {
                Ed2kSyncNowButton.IsEnabled = true;
            }
        }

        private async void SearchEd2kButton_Click(object sender, RoutedEventArgs e)
        {
            Ed2kSettingsSectionControl section = SettingsPage.Ed2kSettingsContentControl;
            if (section.IsEd2kSearchActive)
            {
                _ed2kSearchCancellation?.Cancel();
                return;
            }

            string keyword = Ed2kSearchKeywordTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(keyword))
            {
                ShowMessage(Strings.Get("Ed2kSearchKeywordRequiredMessage"), InfoBarSeverity.Warning);
                _ = Ed2kSearchKeywordTextBox.Focus(FocusState.Programmatic);
                return;
            }

            Aria2EngineStartResult startResult = await EnsureAria2StartedAsync();
            if (!startResult.Started)
            {
                ShowEngineStartFailure(startResult);
                return;
            }

            try
            {
                if (!await _aria2RpcClient.SupportsEd2kAsync())
                {
                    throw new NotSupportedException("The active aria2 engine does not support ED2K search.");
                }
            }
            catch (Exception ex)
            {
                ShowUserError(UserErrorContext.Ed2kSearch, ex);
                return;
            }

            int timeoutSeconds = GetValidIntNumberBoxValue(
                Ed2kSearchTimeoutNumberBox,
                10,
                600,
                Ed2kSettings.DefaultSearchTimeout);
            TimeSpan duration = TimeSpan.FromSeconds(timeoutSeconds);
            string fileType = GetEd2kSearchFileType(GetSelectedTag(
                Ed2kFileTypeComboBox,
                Ed2kSettings.DefaultFileType));
            int minimumSources = GetValidIntNumberBoxValue(
                Ed2kMinSourcesNumberBox,
                1,
                9999,
                Ed2kSettings.DefaultMinSources);

            _ed2kSearchCancellation?.Dispose();
            _ed2kSearchCancellation = new CancellationTokenSource();
            CancellationToken cancellationToken = _ed2kSearchCancellation.Token;
            section.SetEd2kSearchResults([]);
            section.SetEd2kSearchState(
                true,
                TimeSpan.Zero,
                duration,
                Strings.Format("Ed2kSearchProgressText", 0, timeoutSeconds, 0));
            ShowMessage(Strings.Get("Ed2kSearchStartedMessage"), InfoBarSeverity.Informational);

            TimeSpan finalElapsed = TimeSpan.Zero;
            string finalStatus = string.Empty;
            try
            {
                Progress<Ed2kSearchProgress> progress = new(update =>
                {
                    finalElapsed = update.Elapsed;
                    section.SetEd2kSearchResults(update.Results);
                    section.SetEd2kSearchState(
                        true,
                        update.Elapsed,
                        update.Duration,
                        Strings.Format(
                            "Ed2kSearchProgressText",
                            Math.Min((int)update.Elapsed.TotalSeconds, timeoutSeconds),
                            timeoutSeconds,
                            update.Results.Count));
                });
                IReadOnlyList<Aria2Ed2kSearchResult> results = await _ed2kSearchService.SearchAsync(
                    keyword,
                    fileType,
                    minimumSources,
                    duration,
                    progress,
                    cancellationToken);
                section.SetEd2kSearchResults(results);
                finalElapsed = duration;
                finalStatus = results.Count == 0
                    ? Strings.Get("Ed2kSearchEmptyMessage")
                    : Strings.Format("Ed2kSearchCompletedMessage", results.Count);
                ShowMessage(
                    finalStatus,
                    results.Count == 0 ? InfoBarSeverity.Warning : InfoBarSeverity.Success);
            }
            catch (OperationCanceledException)
            {
                finalStatus = section.SearchResults.Count == 0
                    ? Strings.Get("Ed2kSearchCancelledMessage")
                    : Strings.Format("Ed2kSearchCancelledWithResultsMessage", section.SearchResults.Count);
                ShowMessage(finalStatus, InfoBarSeverity.Informational);
            }
            catch (Exception ex)
            {
                finalStatus = Strings.Get("Ed2kSearchFailedStatusText");
                AppLogger.Error("ED2K.Search", ex);
                ShowUserError(UserErrorContext.Ed2kSearch, ex);
            }
            finally
            {
                section.SetEd2kSearchState(false, finalElapsed, duration, finalStatus);
                _ed2kSearchCancellation?.Dispose();
                _ed2kSearchCancellation = null;
            }
        }

        private async void DownloadEd2kSearchResult_Click(
            object? sender,
            Ed2kSearchDownloadRequestedEventArgs e)
        {
            Aria2EngineStartResult startResult = await EnsureAria2StartedAsync();
            if (!startResult.Started)
            {
                ShowEngineStartFailure(startResult);
                return;
            }

            try
            {
                DownloadSettings downloadSettings = _settingsPageViewModel.DownloadSettings;
                DownloadTask task = await _downloadCoordinator.AddDownloadAsync(
                    e.Result.Ed2kLink,
                    e.Result.Name,
                    downloadSettings.DownloadDirectory,
                    downloadSettings.SplitCount);
                _observedTaskStatuses[task.Gid] = task.Status;
                ShowTaskAddedNotification(task);
                ShowMessage(Strings.Get("Ed2kSearchDownloadStartedMessage"), InfoBarSeverity.Success);
                await RefreshDownloadsAsync();
                UpdateDashboard();
            }
            catch (Exception ex)
            {
                ShowUserError(UserErrorContext.Ed2kSearchDownload, ex);
            }
        }

        private static string GetEd2kSearchFileType(string value) => value.ToLowerInvariant() switch
        {
            "audio" => "audio",
            "video" => "video",
            "document" => "doc",
            "archive" => "archive",
            _ => string.Empty
        };

        private void LoadEd2kSettings()
        {
            _settingsPageViewModel.LoadEd2kSettings();
            _isLoadingEd2kSettings = true;
            try
            {
                ApplyEd2kSettingsToUi();
            }
            finally
            {
                _isLoadingEd2kSettings = false;
            }
        }

        private void ApplyEd2kSettingsToUi()
        {
            Ed2kSettings settings = _settingsPageViewModel.Ed2kSettings;
            Ed2kListenPortNumberBox.Value = settings.ListenPort;
            Ed2kUdpListenPortNumberBox.Value = settings.UdpListenPort;
            Ed2kUploadSlotsNumberBox.Value = settings.UploadSlots;
            Ed2kServerListUrlTextBox.Text = settings.ServerListUrl;
            Ed2kKadBootstrapUrlTextBox.Text = settings.KadBootstrapUrl;
            SettingsPage.Ed2kSettingsContentControl.SetEd2kServerAddresses(
                (settings.ServerList ?? string.Empty)
                    .Split(new[] { '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(server => server.Trim()),
                (settings.DisabledServerList ?? string.Empty)
                    .Split(new[] { '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(server => server.Trim()));
            SetToggleSwitch(Ed2kAutoSyncToggleSwitch, settings.AutoSyncEnabled);
            Ed2kAutoSyncStateText.Text = settings.AutoSyncEnabled
                ? Strings.Get("ToggleOnState.Text")
                : Strings.Get("ToggleOffState.Text");
            SetComboBoxSelectionByTag(Ed2kSyncIntervalComboBox, settings.SyncInterval, Ed2kSettings.DefaultSyncInterval);
            Ed2kSearchKeywordTextBox.Text = settings.SearchKeyword;
            SetComboBoxSelectionByTag(Ed2kFileTypeComboBox, settings.FileType, Ed2kSettings.DefaultFileType);
            Ed2kMinSourcesNumberBox.Value = settings.MinSources;
            Ed2kSearchTimeoutNumberBox.Value = settings.SearchTimeout;
            UpdateEd2kLastSyncText(settings.LastSyncTime, _ed2kBootstrapService.GetStatus());
        }

        private Ed2kSettings? TryGetEd2kSettingsFromUi(bool showValidationError)
        {
            int listenPort = GetValidIntNumberBoxValue(Ed2kListenPortNumberBox, 0, 65535, Ed2kSettings.DefaultListenPort);
            int udpPort = GetValidIntNumberBoxValue(Ed2kUdpListenPortNumberBox, 0, 65535, Ed2kSettings.DefaultUdpListenPort);
            int uploadSlots = GetValidIntNumberBoxValue(Ed2kUploadSlotsNumberBox, 1, 100, Ed2kSettings.DefaultUploadSlots);
            string serverUrl = Ed2kServerListUrlTextBox.Text.Trim();
            string nodesUrl = Ed2kKadBootstrapUrlTextBox.Text.Trim();
            string servers = string.Join(",", SettingsPage.Ed2kSettingsContentControl.Ed2kServerAddresses);
            string disabledServers = string.Join(",", SettingsPage.Ed2kSettingsContentControl.DisabledEd2kServerAddresses);

            bool valid = IsHttpUrl(serverUrl) && IsHttpUrl(nodesUrl) && ValidateEd2kServers(servers);
            if (!valid)
            {
                if (showValidationError)
                {
                    ShowMessage(Strings.Get("Ed2kSettingsInvalidMessage"), InfoBarSeverity.Error);
                }

                return null;
            }

            Ed2kSettings current = _settingsPageViewModel.Ed2kSettings;
            return new Ed2kSettings(
                listenPort,
                udpPort,
                uploadSlots,
                serverUrl,
                nodesUrl,
                true,
                servers,
                disabledServers,
                Ed2kAutoSyncToggleSwitch.IsOn,
                GetSelectedTag(Ed2kSyncIntervalComboBox, Ed2kSettings.DefaultSyncInterval),
                current.LastSyncTime,
                Ed2kSearchKeywordTextBox.Text.Trim(),
                GetSelectedTag(Ed2kFileTypeComboBox, Ed2kSettings.DefaultFileType),
                GetValidIntNumberBoxValue(Ed2kMinSourcesNumberBox, 1, 9999, Ed2kSettings.DefaultMinSources),
                GetValidIntNumberBoxValue(Ed2kSearchTimeoutNumberBox, 10, 600, Ed2kSettings.DefaultSearchTimeout));
        }

        private bool SaveEd2kSettings()
        {
            if (_isLoadingEd2kSettings)
            {
                return true;
            }

            Ed2kSettings? settings = TryGetEd2kSettingsFromUi(showValidationError: true);
            if (settings is null)
            {
                return false;
            }

            _settingsPageViewModel.SaveEd2kSettings(settings);
            return true;
        }

        private async Task AutoSyncEd2kBootstrapIfNeededAsync()
        {
            Ed2kSettings settings = _settingsPageViewModel.Ed2kSettings;
            if (_settingsPageViewModel.AdvancedSettings.EngineType == Aria2EngineType.Aria2c ||
                !Ed2kBootstrapService.IsSyncDue(settings, DateTimeOffset.UtcNow))
            {
                return;
            }

            try
            {
                Ed2kBootstrapStatus status = await _ed2kBootstrapService.SyncAsync(
                    settings.ServerListUrl,
                    settings.KadBootstrapUrl);
                long syncTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                _settingsPageViewModel.SaveEd2kSettings(settings with { LastSyncTime = syncTime });
                UpdateEd2kLastSyncText(syncTime, status);
                AppLogger.Info("ED2K", "bootstrap files auto-synced");
            }
            catch (Exception ex)
            {
                AppLogger.Warning("ED2K", $"automatic bootstrap sync failed: {ex.Message}");
            }
        }

        private void UpdateEd2kLastSyncText(long lastSyncTime, Ed2kBootstrapStatus status)
        {
            if (lastSyncTime <= 0)
            {
                Ed2kLastSyncText.Text = Strings.Get("Ed2kNeverSyncedText");
                return;
            }

            string time = DateTimeOffset.FromUnixTimeMilliseconds(lastSyncTime).ToLocalTime().ToString("g");
            Ed2kLastSyncText.Text = status.IsReady
                ? Strings.Format("Ed2kLastSyncStatusText", time, status.ServerMetSize ?? 0, status.NodesDatSize ?? 0)
                : Strings.Format("Ed2kLastSyncTimeText", time);
        }

        private static bool IsHttpUrl(string value) =>
            Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

        private static bool ValidateEd2kServers(string value)
        {
            foreach (string server in value.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                int separator = server.LastIndexOf(':');
                if (separator <= 0 || separator == server.Length - 1 ||
                    !int.TryParse(server[(separator + 1)..], out int port) || port is <= 0 or > 65535)
                {
                    return false;
                }
            }

            return true;
        }

        private static string GetSelectedTag(ComboBox comboBox, string fallback) =>
            comboBox.SelectedItem is ComboBoxItem item ? item.Tag?.ToString() ?? fallback : fallback;

        private static void SetComboBoxSelectionByTag(ComboBox comboBox, string value, string fallback)
        {
            string expected = string.IsNullOrWhiteSpace(value) ? fallback : value;
            for (int index = 0; index < comboBox.Items.Count; index++)
            {
                if (comboBox.Items[index] is ComboBoxItem item &&
                    item.Tag?.ToString()?.Equals(expected, StringComparison.OrdinalIgnoreCase) == true)
                {
                    comboBox.SelectedIndex = index;
                    return;
                }
            }

            comboBox.SelectedIndex = 0;
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
            if (_speedLimitTargetMode == SpeedLimitTargetMode.Task)
            {
                await ApplyTaskSpeedLimitAsync(hideFlyout);
                return;
            }

            UpdateGlobalSpeedLimitStateFromToolbar();

            Aria2EngineStartResult startResult = await EnsureAria2StartedAsync();
            if (!startResult.Started)
            {
                ShowEngineStartFailure(startResult);
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
                ShowUserError(UserErrorContext.SpeedLimit, ex);
            }
        }

        private async Task ApplyTaskSpeedLimitAsync(bool hideFlyout)
        {
            if (string.IsNullOrWhiteSpace(_speedLimitTaskGid))
            {
                ShowMessage(Strings.Get("TaskSpeedLimitNoTaskMessage"), InfoBarSeverity.Warning);
                return;
            }

            bool downloadEnabled = DownloadLimitToggleSwitch?.IsOn == true;
            bool uploadEnabled = UploadLimitToggleSwitch?.IsOn == true;
            long downloadLimit = downloadEnabled
                ? GetSpeedLimitBytesPerSecond(DownloadLimitNumberBox, GetSelectedSpeedLimitUnit(DownloadLimitUnitComboBox))
                : 0;
            long uploadLimit = uploadEnabled
                ? GetSpeedLimitBytesPerSecond(UploadLimitNumberBox, GetSelectedSpeedLimitUnit(UploadLimitUnitComboBox))
                : 0;

            Aria2EngineStartResult startResult = await EnsureAria2StartedAsync();
            if (!startResult.Started)
            {
                ShowEngineStartFailure(startResult);
                return;
            }

            try
            {
                await _downloadCoordinator.SetTaskSpeedLimitsAsync(_speedLimitTaskGid, downloadLimit, uploadLimit);
                TaskDetailsPane?.UpdateSelectedTaskSpeedLimitState(
                    downloadLimit > 0,
                    downloadLimit,
                    uploadLimit > 0,
                    uploadLimit);
                if (hideFlyout)
                {
                    SpeedLimitButton.Flyout?.Hide();
                }

                ShowMessage(Strings.Get("TaskSpeedLimitAppliedMessage"), InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                ShowUserError(UserErrorContext.TaskSpeedLimit, ex);
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
                ShowUserError(UserErrorContext.DownloadSettingsSync, ex, InfoBarSeverity.Warning);
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

        private void PrepareGlobalSpeedLimitFlyout()
        {
            _speedLimitTargetMode = SpeedLimitTargetMode.Global;
            _speedLimitTaskGid = string.Empty;
            LoadSpeedLimitSettings();
        }

        private async Task LoadTaskSpeedLimitStateIntoToolbarAsync(string gid)
        {
            long downloadLimit = 0;
            long uploadLimit = 0;

            if (_aria2EngineHost.IsRunning)
            {
                try
                {
                    Dictionary<string, string> options = await _downloadCoordinator.GetTaskOptionsAsync(gid);
                    if (options.TryGetValue("max-download-limit", out string? downloadValue))
                    {
                        downloadLimit = ParseAria2SpeedLimit(downloadValue);
                    }

                    if (options.TryGetValue("max-upload-limit", out string? uploadValue))
                    {
                        uploadLimit = ParseAria2SpeedLimit(uploadValue);
                    }
                }
                catch
                {
                    downloadLimit = 0;
                    uploadLimit = 0;
                }
            }

            SetSpeedLimitControlsFromBytes(downloadLimit > 0, downloadLimit, uploadLimit > 0, uploadLimit);
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
            string[] selectedSources = NormalizeTrackerSourceUrls(
                settings.SelectedTrackerSourceUrls,
                settings.TrackerSourceUrl,
                settings.CustomTrackerUrls);
            SettingsPage.BitTorrentSettingsContentControl.SetTrackerSources(selectedSources, settings.CustomTrackerUrls);
            SettingsPage.BitTorrentSettingsContentControl.SetTrackerAddresses(
                settings.TrackerList.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                settings.DisabledTrackerList.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
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
            NetworkSettings settings = NormalizeNetworkSettingsWithSystemProxyDefault(_settingsPageViewModel.NetworkSettings);
            if (settings != _settingsPageViewModel.NetworkSettings)
            {
                _settingsPageViewModel.SaveNetworkSettings(settings);
            }

            SetToggleSwitch(CustomProxyToggleSwitch, settings.CustomProxyEnabled);
            ProxyServerTextBox.Text = settings.ProxyServer;
            ProxyUsernameTextBox.Text = settings.ProxyUsername;
            ProxyPasswordBox.Password = settings.ProxyPassword;
            ProxyBypassTextBox.Text = FormatProxyBypassForDisplay(settings.ProxyBypass);
            ProxyDownloadsCheckBox.IsChecked = settings.ProxyDownloads;
            ProxyTrackersCheckBox.IsChecked = settings.ProxyTrackers;
            SetToggleSwitch(EnableUpnpToggleSwitch, settings.EnableUpnp);
            BtListenPortNumberBox.Value = settings.ListenPort;
            DhtListenPortNumberBox.Value = settings.DhtListenPort;
            UserAgentTextBox.Text = settings.UserAgent;
            ApplyUserAgentSelectionToUi(settings.UserAgent);
            ConnectTimeoutNumberBox.Value = settings.ConnectTimeoutSeconds;
            TimeoutNumberBox.Value = settings.TimeoutSeconds;
            SetFileAllocationSelection(settings.FileAllocation);
            UpdateNetworkDependentUi();
            SettingsPage.NetworkSettingsContentControl.UpdateProxyScopeSummary();
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
                ShowMessage(Strings.Get("SystemProxyNotFoundMessage"), InfoBarSeverity.Informational);
                return;
            }

            ProxyServerTextBox.Text = proxySettings.AllProxy ?? string.Empty;
            ProxyBypassTextBox.Text = proxySettings.NoProxy ?? string.Empty;
            SetToggleSwitch(CustomProxyToggleSwitch, true);
            ShowSettingsSaveTeachingTip();
            ShowMessage(Strings.Get("SystemProxyAppliedMessage"), InfoBarSeverity.Success);
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
            if (sender is not FrameworkElement element)
            {
                return;
            }

            string tag = element is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem selectedItem
                ? selectedItem.Tag?.ToString() ?? string.Empty
                : element.Tag?.ToString() ?? "Default";
            if (_isLoadingNetworkSettings)
            {
                return;
            }

            if (tag.Equals("Custom", StringComparison.OrdinalIgnoreCase))
            {
                SetUserAgentCustomInputVisible(true);
                return;
            }

            UserAgentTextBox.Text = GetUserAgentPresetValue(tag);
            ApplyUserAgentSelectionToUi(UserAgentTextBox.Text);
            ShowSettingsSaveTeachingTip();
        }

        private void ApplyUserAgentSelectionToUi(string userAgent)
        {
            string preset = GetUserAgentPresetName(userAgent);
            SetUserAgentComboBoxSelection(preset);
            SetUserAgentCustomInputVisible(preset == "Custom");
        }

        private void SetUserAgentComboBoxSelection(string preset)
        {
            if (UserAgentComboBox is null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(preset))
            {
                UserAgentComboBox.SelectedIndex = -1;
                return;
            }

            foreach (object item in UserAgentComboBox.Items)
            {
                if (item is ComboBoxItem comboBoxItem &&
                    comboBoxItem.Tag?.ToString()?.Equals(preset, StringComparison.OrdinalIgnoreCase) == true)
                {
                    UserAgentComboBox.SelectedItem = comboBoxItem;
                    return;
                }
            }
        }

        private void SetUserAgentCustomInputVisible(bool isVisible)
        {
            UserAgentCustomSettingCard.Visibility = isVisible
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private static string GetUserAgentPresetName(string? userAgent)
        {
            string normalized = userAgent?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return "Default";
            }

            foreach (string preset in new[] { "Chrome", "Edge", "Safari", "Firefox", "Transmission" })
            {
                if (string.Equals(normalized, GetUserAgentPresetValue(preset), StringComparison.Ordinal))
                {
                    return preset;
                }
            }

            return "Custom";
        }

        private static string GetUserAgentPresetValue(string preset)
        {
            return preset switch
            {
                "Chrome" => "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36",
                "Edge" => "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36 Edg/124.0.0.0",
                "Safari" => "Mozilla/5.0 (Macintosh; Intel Mac OS X 14_4) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.4 Safari/605.1.15",
                "Firefox" => "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:125.0) Gecko/20100101 Firefox/125.0",
                "Transmission" => "Transmission/3.00",
                _ => string.Empty
            };
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
            _ariaPath = settings.Aria2Path;
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

            if (ReferenceEquals(sender, EngineTypeComboBox))
            {
                string importedPath = Aria2EngineStore.GetImportedEnginePath(GetSelectedEngineType());
                _ariaPath = File.Exists(importedPath) ? importedPath : string.Empty;
                await RefreshEngineVersionAsync();
            }

            if (sender is ToggleSwitch toggleSwitch &&
                toggleSwitch.IsOn &&
                (ReferenceEquals(toggleSwitch, ProtocolMagnetToggleSwitch) ||
                    ReferenceEquals(toggleSwitch, ProtocolThunderToggleSwitch) ||
                    ReferenceEquals(toggleSwitch, ProtocolOmniDownToggleSwitch)))
            {
                _ = Launcher.LaunchUriAsync(new Uri("ms-settings:defaultapps"));
                ShowMessage(Strings.Get("ProtocolRegisteredMessage"), InfoBarSeverity.Informational);
            }
        }

        private async Task RefreshEngineVersionAsync()
        {
            try
            {
                await _aria2EngineHost.DetectVersionAsync(
                    string.IsNullOrWhiteSpace(_ariaPath) ? null : _ariaPath,
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

            try
            {
                string importedPath = Aria2EngineStore.Import(file.Path, GetSelectedEngineType());
                _ariaPath = importedPath;
                await RefreshEngineVersionAsync();
                ShowSettingsSaveTeachingTip();
                ShowMessage(Strings.Get("EngineImportedMessage"), InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                ShowUserError(UserErrorContext.EngineImport, ex);
            }
        }

        private void CopyRpcSecretButton_Click(object sender, RoutedEventArgs e)
        {
            CopyTextToClipboard(RpcSecretPasswordBox.Password);
            ShowMessage(Strings.Get("RpcSecretCopiedMessage"), InfoBarSeverity.Success);
        }

        private void GenerateRpcSecretButton_Click(object sender, RoutedEventArgs e)
        {
            RpcSecretPasswordBox.Password = AdvancedSettings.GenerateSecret();
            ShowSettingsSaveTeachingTip();
        }

        private void CopyExtensionApiSecretButton_Click(object sender, RoutedEventArgs e)
        {
            CopyTextToClipboard(ExtensionApiSecretPasswordBox.Password);
            ShowMessage(Strings.Get("ExtensionApiSecretCopiedMessage"), InfoBarSeverity.Success);
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
                Title = Strings.Get("ClearSessionDialogTitle"),
                Content = Strings.Get("ClearSessionDialogContent"),
                PrimaryButtonText = Strings.Get("ClearButtonText"),
                CloseButtonText = Strings.Get("CancelButtonText"),
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

                ShowMessage(Strings.Get("SessionClearedMessage"), InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                ShowUserError(UserErrorContext.SessionClear, ex);
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
                _ariaPath,
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
                if (!SaveEd2kSettings())
                {
                    SettingsSaveTeachingTip.IsOpen = true;
                    return;
                }
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
                _settingsPageViewModel.Ed2kSettings,
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
                TryGetEd2kSettingsFromUi(showValidationError: false) ?? _settingsPageViewModel.Ed2kSettings,
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
                (TryGetEd2kSettingsFromUi(showValidationError: false) ?? _settingsPageViewModel.Ed2kSettings) != _pendingAriaSettingsRollback.Ed2k ||
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
                string.Equals(NormalizeTrackerList(left.DisabledTrackerList), NormalizeTrackerList(right.DisabledTrackerList), StringComparison.Ordinal) &&
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
                _settingsPageViewModel.SaveEd2kSettings(snapshot.Ed2k);
                _settingsPageViewModel.SaveAdvancedSettings(snapshot.Advanced);
            }

            _isLoadingDownloadSettings = true;
            _isLoadingBitTorrentSettings = true;
            _isLoadingEd2kSettings = true;
            _isLoadingNetworkSettings = true;
            _isLoadingAdvancedSettings = true;
            try
            {
                if (!save)
                {
                    _settingsPageViewModel.SaveDownloadSettings(snapshot.Download);
                    _settingsPageViewModel.SaveBitTorrentSettings(snapshot.BitTorrent);
                    _settingsPageViewModel.SaveNetworkSettings(snapshot.Network);
                    _settingsPageViewModel.SaveEd2kSettings(snapshot.Ed2k);
                    _settingsPageViewModel.SaveAdvancedSettings(snapshot.Advanced);
                }

                ApplyDownloadSettingsToUi();
                ApplyBitTorrentSettingsToUi();
                ApplyEd2kSettingsToUi();
                ApplyNetworkSettingsToUi();
                ApplyAdvancedSettingsToUi();
            }
            finally
            {
                _isLoadingDownloadSettings = false;
                _isLoadingBitTorrentSettings = false;
                _isLoadingEd2kSettings = false;
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
                _settingsPageViewModel.Ed2kSettings,
                _settingsPageViewModel.AdvancedSettings);
        }

        private string CreateAriaRestartSettingsSignature(
            DownloadSettings download,
            BitTorrentSettings bitTorrent,
            NetworkSettings network,
            Ed2kSettings ed2k,
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
                network.ProxyUsername,
                network.ProxyPassword,
                network.ProxyBypass,
                network.ProxyDownloads.ToString(CultureInfo.InvariantCulture),
                network.ProxyTrackers.ToString(CultureInfo.InvariantCulture),
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
                NormalizeTrackerList(bitTorrent.DisabledTrackerList),
                ed2k.ListenPort.ToString(CultureInfo.InvariantCulture),
                ed2k.UdpListenPort.ToString(CultureInfo.InvariantCulture),
                ed2k.UploadSlots.ToString(CultureInfo.InvariantCulture),
                ed2k.ServerList,
                ed2k.DisabledServerList,
                ed2k.KadBootstrapEnabled.ToString(CultureInfo.InvariantCulture),
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
                    return;
                }
            }

            EngineTypeComboBox.SelectedIndex = 1;
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
                ShowMessage(Strings.Get("InvalidProxyUrlMessage"), InfoBarSeverity.Warning);
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
                false,
                CustomProxyToggleSwitch?.IsOn == true,
                ProxyServerTextBox.Text.Trim(),
                ProxyUsernameTextBox.Text.Trim(),
                ProxyPasswordBox.Password,
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
            ProxyUsernameTextBox.IsEnabled = proxyEnabled;
            ProxyPasswordBox.IsEnabled = proxyEnabled;
            ProxyBypassTextBox.IsEnabled = proxyEnabled;
            ProxyScopeDropDownButton.IsEnabled = proxyEnabled;
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
                ProxyUsername = SanitizeHeaderValue(settings.ProxyUsername),
                ProxyPassword = SanitizeHeaderValue(settings.ProxyPassword),
                ProxyBypass = NormalizeProxyBypass(settings.ProxyBypass),
                ListenPort = Math.Clamp(settings.ListenPort, 1024, 65535),
                DhtListenPort = Math.Clamp(settings.DhtListenPort, 1024, 65535),
                UserAgent = SanitizeHeaderValue(settings.UserAgent),
                ConnectTimeoutSeconds = Math.Clamp(settings.ConnectTimeoutSeconds, 1, 600),
                TimeoutSeconds = Math.Clamp(settings.TimeoutSeconds, 1, 600),
                FileAllocation = NormalizeFileAllocation(settings.FileAllocation)
            };
        }

        private static NetworkSettings NormalizeNetworkSettingsWithSystemProxyDefault(NetworkSettings settings)
        {
            NetworkSettings normalized = NormalizeNetworkSettings(settings);
            if (!string.IsNullOrWhiteSpace(normalized.ProxyServer))
            {
                return normalized;
            }

            SystemProxySettings proxySettings = SystemProxyResolver.Resolve();
            if (!proxySettings.HasProxy || string.IsNullOrWhiteSpace(proxySettings.AllProxy))
            {
                return normalized;
            }

            return NormalizeNetworkSettings(normalized with
            {
                ProxyServer = proxySettings.AllProxy,
                ProxyBypass = string.IsNullOrWhiteSpace(proxySettings.NoProxy)
                    ? normalized.ProxyBypass
                    : proxySettings.NoProxy
            });
        }

        private static bool IsValidProxyUrl(string value)
        {
            return Uri.TryCreate(NormalizeProxyServer(value), UriKind.Absolute, out Uri? uri) &&
                uri.Host.Length > 0 &&
                (uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                    uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase));
        }

        private static string NormalizeProxyServer(string? value)
        {
            string trimmed = value?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(trimmed) ||
                trimmed.Contains("://", StringComparison.Ordinal))
            {
                return trimmed;
            }

            return $"http://{trimmed}";
        }

        private static string NormalizeProxyBypass(string? value)
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

        private static string FormatProxyBypassForDisplay(string? value)
        {
            return string.Join(Environment.NewLine, NormalizeProxyBypass(value)
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
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
                if (!string.IsNullOrWhiteSpace(settings.ProxyUsername))
                {
                    proxy.Credentials = new NetworkCredential(settings.ProxyUsername, settings.ProxyPassword);
                }

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
            string[] selectedSources = SettingsPage.BitTorrentSettingsContentControl.SelectedTrackerSourceUrls.ToArray();
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
                SettingsPage.BitTorrentSettingsContentControl.CustomTrackerSourceUrls.ToArray(),
                NormalizeTrackerList(string.Join(Environment.NewLine, SettingsPage.BitTorrentSettingsContentControl.TrackerAddresses)),
                NormalizeTrackerList(string.Join(Environment.NewLine, SettingsPage.BitTorrentSettingsContentControl.DisabledTrackerAddresses)),
                BtAutoSyncTrackerToggleSwitch?.IsOn == true,
                _settingsPageViewModel.BitTorrentSettings.LastSyncTrackerTime));
        }

        private async Task SyncBitTorrentTrackersAsync()
        {
            string[] sourceUrls = SettingsPage.BitTorrentSettingsContentControl.SelectedTrackerSourceUrls.ToArray();
            if (sourceUrls.Length == 0)
            {
                ShowMessage(Strings.Get("TrackerSourceRequiredMessage"), InfoBarSeverity.Warning);
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
                    ShowMessage(Strings.Get("TrackerNoUsableAddressMessage"), InfoBarSeverity.Warning);
                    return;
                }

                SettingsPage.BitTorrentSettingsContentControl.SetTrackerAddresses(
                    trackers.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
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
                    SettingsPage.BitTorrentSettingsContentControl.CustomTrackerSourceUrls.ToArray(),
                    trackers,
                    string.Empty,
                    BtAutoSyncTrackerToggleSwitch?.IsOn == true,
                    DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
                _settingsPageViewModel.SaveBitTorrentSettings(settings);
                UpdateTrackerSyncTimeText(settings.LastSyncTrackerTime);
                ShowMessage(
                    failedSources.Count == 0
                        ? Strings.Get("TrackerSyncSuccessMessage")
                        : Strings.Format("TrackerSyncPartialMessage", failedSources.Count),
                    failedSources.Count == 0 ? InfoBarSeverity.Success : InfoBarSeverity.Warning);
            }
            catch (Exception ex)
            {
                ShowUserError(UserErrorContext.TrackerSync, ex);
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
                    DisabledTrackerList = string.Empty,
                    LastSyncTrackerTime = now
                };
                _settingsPageViewModel.SaveBitTorrentSettings(updated);
                SettingsPage.BitTorrentSettingsContentControl.SetTrackerAddresses(
                    trackers.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                UpdateTrackerSyncTimeText(now);
            }
            catch
            {
                // Auto-sync is best-effort; manual sync still reports failures.
            }
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
            SettingsPage.BitTorrentSettingsContentControl.SetTrackerControlsEnabled(isEnabled);
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
                TrackerList = NormalizeTrackerList(settings.TrackerList),
                DisabledTrackerList = NormalizeTrackerList(settings.DisabledTrackerList)
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
                ShowMessage(Strings.Get("AutoStartPermissionRequiredMessage"), InfoBarSeverity.Warning);
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

        private void UpdateGlobalSpeedLimitStateFromToolbar()
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

        private void SetSpeedLimitControlsFromBytes(
            bool downloadEnabled,
            long downloadBytesPerSecond,
            bool uploadEnabled,
            long uploadBytesPerSecond)
        {
            SetToggleSwitch(DownloadLimitToggleSwitch, downloadEnabled);
            SetToggleSwitch(UploadLimitToggleSwitch, uploadEnabled);
            SetSpeedLimitValue(DownloadLimitNumberBox, DownloadLimitUnitComboBox, downloadBytesPerSecond);
            SetSpeedLimitValue(UploadLimitNumberBox, UploadLimitUnitComboBox, uploadBytesPerSecond);
            SetDownloadSpeedLimitInputsEnabled(downloadEnabled);
            SetUploadSpeedLimitInputsEnabled(uploadEnabled);
        }

        private static void SetSpeedLimitValue(NumberBox numberBox, ComboBox comboBox, long bytesPerSecond)
        {
            long normalized = Math.Max(bytesPerSecond, 1024);
            const long mb = 1024L * 1024L;
            bool useMegabytes = normalized >= mb && normalized % mb == 0;
            comboBox.SelectedIndex = useMegabytes ? 1 : 0;
            long divisor = useMegabytes ? mb : 1024L;
            numberBox.Value = Math.Max(1, (double)normalized / divisor);
        }

        private static long ParseAria2SpeedLimit(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return 0;
            }

            string trimmed = value.Trim();
            long multiplier = 1;
            char suffix = trimmed[^1];
            if (suffix is 'K' or 'k')
            {
                multiplier = 1024L;
                trimmed = trimmed[..^1];
            }
            else if (suffix is 'M' or 'm')
            {
                multiplier = 1024L * 1024L;
                trimmed = trimmed[..^1];
            }

            return double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed)
                ? Math.Max(0, (long)Math.Round(parsed * multiplier))
                : 0;
        }
    }
}
