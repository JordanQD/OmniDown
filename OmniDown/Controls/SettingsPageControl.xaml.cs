using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OmniDown.Models.Settings;
using System;
using System.Collections.Generic;

namespace OmniDown.Controls;

public sealed partial class SettingsPageControl : UserControl
{
    public SettingsPageControl()
    {
        InitializeComponent();

        // Forward events from section controls
        GeneralSettingsContent.GeneralSettingChanged += (_, args) => GeneralSettingChanged?.Invoke(this, args);
        GeneralSettingsContent.CloseBehaviorSettingChanged += (_, args) => CloseBehaviorSettingChanged?.Invoke(this, args);
        DownloadSettingsContent.BrowseDownloadDirectoryRequested += (_, args) => BrowseDownloadDirectoryRequested?.Invoke(this, args);
        DownloadSettingsContent.DownloadSettingChanged += (_, args) => DownloadSettingChanged?.Invoke(this, args);
        BitTorrentSettingsContent.BitTorrentSettingChanged += (_, args) => BitTorrentSettingChanged?.Invoke(this, args);
        BitTorrentSettingsContent.AddBtCustomTrackerRequested += (_, args) => AddBtCustomTrackerRequested?.Invoke(this, args);
        BitTorrentSettingsContent.SyncBtTrackerRequested += (_, args) => SyncBtTrackerRequested?.Invoke(this, args);
        NetworkSettingsContent.NetworkSettingChanged += (_, args) => NetworkSettingChanged?.Invoke(this, args);
        NetworkSettingsContent.DetectSystemProxyRequested += (_, args) => DetectSystemProxyRequested?.Invoke(this, args);
        NetworkSettingsContent.RandomBtPortRequested += (_, args) => RandomBtPortRequested?.Invoke(this, args);
        NetworkSettingsContent.RandomDhtPortRequested += (_, args) => RandomDhtPortRequested?.Invoke(this, args);
        NetworkSettingsContent.UserAgentPresetRequested += (_, args) => UserAgentPresetRequested?.Invoke(this, args);
        AdvancedSettingsContent.AdvancedSettingChanged += (_, args) => AdvancedSettingChanged?.Invoke(this, args);
        AdvancedSettingsContent.BrowseAriaPathRequested += (_, args) => BrowseAriaPathRequested?.Invoke(this, args);
        AdvancedSettingsContent.CopyRpcSecretRequested += (_, args) => CopyRpcSecretRequested?.Invoke(this, args);
        AdvancedSettingsContent.GenerateRpcSecretRequested += (_, args) => GenerateRpcSecretRequested?.Invoke(this, args);
        AdvancedSettingsContent.CopyExtensionApiSecretRequested += (_, args) => CopyExtensionApiSecretRequested?.Invoke(this, args);
        AdvancedSettingsContent.GenerateExtensionApiSecretRequested += (_, args) => GenerateExtensionApiSecretRequested?.Invoke(this, args);
        AdvancedSettingsContent.OpenConfigFolderRequested += (_, args) => OpenConfigFolderRequested?.Invoke(this, args);
        AdvancedSettingsContent.OpenLogFolderRequested += (_, args) => OpenLogFolderRequested?.Invoke(this, args);
        AdvancedSettingsContent.ClearSessionRequested += (_, args) => ClearSessionRequested?.Invoke(this, args);
        AdvancedSettingsContent.StartStopAriaRequested += (_, args) => StartStopAriaRequested?.Invoke(this, args);
        AdvancedSettingsContent.RestartAriaRequested += (_, args) => RestartAriaRequested?.Invoke(this, args);
        AboutSettingsContent.CopyCloneCommandRequested += (_, args) => CopyCloneCommandRequested?.Invoke(this, args);
        AboutSettingsContent.OpenAboutLinkRequested += (_, args) => OpenAboutLinkRequested?.Invoke(this, args);
    }

    // Layout controls
    internal ListView SettingsSectionListViewControl => SettingsSectionListView;
    internal ScrollViewer SettingsContentScrollViewerControl => SettingsContentScrollViewer;

    // Section content containers
    internal GeneralSettingsSectionControl GeneralSettingsContentControl => GeneralSettingsContent;
    internal DownloadSettingsSectionControl DownloadSettingsContentControl => DownloadSettingsContent;
    internal BitTorrentSettingsSectionControl BitTorrentSettingsContentControl => BitTorrentSettingsContent;
    internal NetworkSettingsSectionControl NetworkSettingsContentControl => NetworkSettingsContent;
    internal AdvancedSettingsSectionControl AdvancedSettingsContentControl => AdvancedSettingsContent;
    internal AboutSettingsSectionControl AboutSettingsContentControl => AboutSettingsContent;

    // Download section controls
    internal TextBox DownloadDirectoryTextBoxControl => DownloadSettingsContent.DownloadDirectoryTextBoxControl;
    internal NumberBox MaxConcurrentDownloadsNumberBoxControl => DownloadSettingsContent.MaxConcurrentDownloadsNumberBoxControl;
    internal NumberBox SplitCountNumberBoxControl => DownloadSettingsContent.SplitCountNumberBoxControl;
    internal NumberBox MaxConnectionPerServerNumberBoxControl => DownloadSettingsContent.MaxConnectionPerServerNumberBoxControl;
    internal ToggleSwitch ContinueDownloadToggleSwitchControl => DownloadSettingsContent.ContinueDownloadToggleSwitchControl;
    internal TextBlock ContinueDownloadStateTextControl => DownloadSettingsContent.ContinueDownloadStateTextControl;
    internal ComboBox RemoteTimeComboBoxControl => DownloadSettingsContent.RemoteTimeComboBoxControl;
    internal NumberBox MaxTriesNumberBoxControl => DownloadSettingsContent.MaxTriesNumberBoxControl;
    internal NumberBox RetryWaitNumberBoxControl => DownloadSettingsContent.RetryWaitNumberBoxControl;
    internal ToggleSwitch AutoDeleteStaleRecordsToggleSwitchControl => DownloadSettingsContent.AutoDeleteStaleRecordsToggleSwitchControl;
    internal TextBlock AutoDeleteStaleRecordsStateTextControl => DownloadSettingsContent.AutoDeleteStaleRecordsStateTextControl;
    internal ToggleSwitch DeleteTorrentAfterCompleteToggleSwitchControl => DownloadSettingsContent.DeleteTorrentAfterCompleteToggleSwitchControl;
    internal TextBlock DeleteTorrentAfterCompleteStateTextControl => DownloadSettingsContent.DeleteTorrentAfterCompleteStateTextControl;

    // BitTorrent section controls
    internal ToggleSwitch BtAutoDownloadToggleSwitchControl => BitTorrentSettingsContent.BtAutoDownloadToggleSwitchControl;
    internal TextBlock BtAutoDownloadStateTextControl => BitTorrentSettingsContent.BtAutoDownloadStateTextControl;
    internal ToggleSwitch BtForceEncryptionToggleSwitchControl => BitTorrentSettingsContent.BtForceEncryptionToggleSwitchControl;
    internal TextBlock BtForceEncryptionStateTextControl => BitTorrentSettingsContent.BtForceEncryptionStateTextControl;
    internal ComboBox BtSeedingModeComboBoxControl => BitTorrentSettingsContent.BtSeedingModeComboBoxControl;
    internal NumberBox BtSeedRatioNumberBoxControl => BitTorrentSettingsContent.BtSeedRatioNumberBoxControl;
    internal NumberBox BtSeedTimeNumberBoxControl => BitTorrentSettingsContent.BtSeedTimeNumberBoxControl;
    internal NumberBox BtMaxPeersNumberBoxControl => BitTorrentSettingsContent.BtMaxPeersNumberBoxControl;
    internal Button BtTrackerSourceDropDownButtonControl => BitTorrentSettingsContent.BtTrackerSourceDropDownButtonControl;
    internal TextBlock BtTrackerSourceSummaryTextControl => BitTorrentSettingsContent.BtTrackerSourceSummaryTextControl;
    internal CheckBox BtTrackerNgosangBestCheckBoxControl => BitTorrentSettingsContent.BtTrackerNgosangBestCheckBoxControl;
    internal CheckBox BtTrackerNgosangBestIpCheckBoxControl => BitTorrentSettingsContent.BtTrackerNgosangBestIpCheckBoxControl;
    internal CheckBox BtTrackerNgosangAllCheckBoxControl => BitTorrentSettingsContent.BtTrackerNgosangAllCheckBoxControl;
    internal CheckBox BtTrackerNgosangAllIpCheckBoxControl => BitTorrentSettingsContent.BtTrackerNgosangAllIpCheckBoxControl;
    internal CheckBox BtTrackerNgosangCdnBestCheckBoxControl => BitTorrentSettingsContent.BtTrackerNgosangCdnBestCheckBoxControl;
    internal CheckBox BtTrackerNgosangCdnBestIpCheckBoxControl => BitTorrentSettingsContent.BtTrackerNgosangCdnBestIpCheckBoxControl;
    internal CheckBox BtTrackerNgosangCdnAllCheckBoxControl => BitTorrentSettingsContent.BtTrackerNgosangCdnAllCheckBoxControl;
    internal CheckBox BtTrackerNgosangCdnAllIpCheckBoxControl => BitTorrentSettingsContent.BtTrackerNgosangCdnAllIpCheckBoxControl;
    internal CheckBox BtTrackerXiu2BestCheckBoxControl => BitTorrentSettingsContent.BtTrackerXiu2BestCheckBoxControl;
    internal CheckBox BtTrackerXiu2AllCheckBoxControl => BitTorrentSettingsContent.BtTrackerXiu2AllCheckBoxControl;
    internal CheckBox BtTrackerXiu2HttpCheckBoxControl => BitTorrentSettingsContent.BtTrackerXiu2HttpCheckBoxControl;
    internal CheckBox BtTrackerXiu2CdnBestCheckBoxControl => BitTorrentSettingsContent.BtTrackerXiu2CdnBestCheckBoxControl;
    internal CheckBox BtTrackerXiu2CdnAllCheckBoxControl => BitTorrentSettingsContent.BtTrackerXiu2CdnAllCheckBoxControl;
    internal CheckBox BtTrackerXiu2CdnHttpCheckBoxControl => BitTorrentSettingsContent.BtTrackerXiu2CdnHttpCheckBoxControl;
    internal TextBox BtCustomTrackerSourceTextBoxControl => BitTorrentSettingsContent.BtCustomTrackerSourceTextBoxControl;
    internal ListView BtCustomTrackerSourceListViewControl => BitTorrentSettingsContent.BtCustomTrackerSourceListViewControl;
    internal TextBox BtTrackerSourceTextBoxControl => BitTorrentSettingsContent.BtTrackerSourceTextBoxControl;
    internal Button BtSyncTrackerButtonControl => BitTorrentSettingsContent.BtSyncTrackerButtonControl;
    internal TextBox BtTrackerListTextBoxControl => BitTorrentSettingsContent.BtTrackerListTextBoxControl;
    internal ToggleSwitch BtAutoSyncTrackerToggleSwitchControl => BitTorrentSettingsContent.BtAutoSyncTrackerToggleSwitchControl;
    internal TextBlock BtAutoSyncTrackerStateTextControl => BitTorrentSettingsContent.BtAutoSyncTrackerStateTextControl;
    internal TextBlock BtLastTrackerSyncTextControl => BitTorrentSettingsContent.BtLastTrackerSyncTextControl;

    // Network section controls
    internal ToggleSwitch UseSystemProxyCheckBoxControl => NetworkSettingsContent.UseSystemProxyCheckBoxControl;
    internal TextBlock UseSystemProxyStateTextControl => NetworkSettingsContent.UseSystemProxyStateTextControl;
    internal ToggleSwitch CustomProxyToggleSwitchControl => NetworkSettingsContent.CustomProxyToggleSwitchControl;
    internal TextBlock CustomProxyStateTextControl => NetworkSettingsContent.CustomProxyStateTextControl;
    internal TextBox ProxyServerTextBoxControl => NetworkSettingsContent.ProxyServerTextBoxControl;
    internal Button DetectSystemProxyButtonControl => NetworkSettingsContent.DetectSystemProxyButtonControl;
    internal TextBox ProxyBypassTextBoxControl => NetworkSettingsContent.ProxyBypassTextBoxControl;
    internal CheckBox ProxyDownloadsCheckBoxControl => NetworkSettingsContent.ProxyDownloadsCheckBoxControl;
    internal CheckBox ProxyTrackersCheckBoxControl => NetworkSettingsContent.ProxyTrackersCheckBoxControl;
    internal ToggleSwitch EnableUpnpToggleSwitchControl => NetworkSettingsContent.EnableUpnpToggleSwitchControl;
    internal TextBlock EnableUpnpStateTextControl => NetworkSettingsContent.EnableUpnpStateTextControl;
    internal NumberBox BtListenPortNumberBoxControl => NetworkSettingsContent.BtListenPortNumberBoxControl;
    internal NumberBox DhtListenPortNumberBoxControl => NetworkSettingsContent.DhtListenPortNumberBoxControl;
    internal TextBox UserAgentTextBoxControl => NetworkSettingsContent.UserAgentTextBoxControl;
    internal NumberBox ConnectTimeoutNumberBoxControl => NetworkSettingsContent.ConnectTimeoutNumberBoxControl;
    internal NumberBox TimeoutNumberBoxControl => NetworkSettingsContent.TimeoutNumberBoxControl;
    internal ComboBox FileAllocationComboBoxControl => NetworkSettingsContent.FileAllocationComboBoxControl;

    // Advanced section controls
    internal TextBox AriaPathTextBoxControl => AdvancedSettingsContent.AriaPathTextBoxControl;
    internal NumberBox RpcPortNumberBoxControl => AdvancedSettingsContent.RpcPortNumberBoxControl;
    internal PasswordBox RpcSecretPasswordBoxControl => AdvancedSettingsContent.RpcSecretPasswordBoxControl;
    internal ToggleSwitch ExtensionAutoSubmitToggleSwitchControl => AdvancedSettingsContent.ExtensionAutoSubmitToggleSwitchControl;
    internal TextBlock ExtensionAutoSubmitStateTextControl => AdvancedSettingsContent.ExtensionAutoSubmitStateTextControl;
    internal NumberBox ExtensionApiPortNumberBoxControl => AdvancedSettingsContent.ExtensionApiPortNumberBoxControl;
    internal PasswordBox ExtensionApiSecretPasswordBoxControl => AdvancedSettingsContent.ExtensionApiSecretPasswordBoxControl;
    internal ComboBox LogLevelComboBoxControl => AdvancedSettingsContent.LogLevelComboBoxControl;
    internal TextBlock AdvancedPathsSummaryTextControl => AdvancedSettingsContent.AdvancedPathsSummaryTextControl;
    internal TextBlock LogPathsSummaryTextControl => AdvancedSettingsContent.LogPathsSummaryTextControl;
    internal ToggleSwitch ClipboardDetectionToggleSwitchControl => AdvancedSettingsContent.ClipboardDetectionToggleSwitchControl;
    internal TextBlock ClipboardDetectionStateTextControl => AdvancedSettingsContent.ClipboardDetectionStateTextControl;
    internal ToggleSwitch ClipboardHttpToggleSwitchControl => AdvancedSettingsContent.ClipboardHttpToggleSwitchControl;
    internal ToggleSwitch ClipboardFtpToggleSwitchControl => AdvancedSettingsContent.ClipboardFtpToggleSwitchControl;
    internal ToggleSwitch ClipboardMagnetToggleSwitchControl => AdvancedSettingsContent.ClipboardMagnetToggleSwitchControl;
    internal ToggleSwitch ClipboardThunderToggleSwitchControl => AdvancedSettingsContent.ClipboardThunderToggleSwitchControl;
    internal ToggleSwitch ClipboardBtHashToggleSwitchControl => AdvancedSettingsContent.ClipboardBtHashToggleSwitchControl;
    internal ToggleSwitch ProtocolMagnetToggleSwitchControl => AdvancedSettingsContent.ProtocolMagnetToggleSwitchControl;
    internal TextBlock ProtocolMagnetStateTextControl => AdvancedSettingsContent.ProtocolMagnetStateTextControl;
    internal ToggleSwitch ProtocolThunderToggleSwitchControl => AdvancedSettingsContent.ProtocolThunderToggleSwitchControl;
    internal TextBlock ProtocolThunderStateTextControl => AdvancedSettingsContent.ProtocolThunderStateTextControl;
    internal ToggleSwitch ProtocolOmniDownToggleSwitchControl => AdvancedSettingsContent.ProtocolOmniDownToggleSwitchControl;
    internal TextBlock ProtocolOmniDownStateTextControl => AdvancedSettingsContent.ProtocolOmniDownStateTextControl;
    internal TextBlock SettingsAriaStatusTextControl => AdvancedSettingsContent.SettingsAriaStatusTextControl;
    internal StackPanel ProcessStatusSettingControlControl => AdvancedSettingsContent.ProcessStatusSettingControlControl;
    internal FontIcon AriaStartStopIconControl => AdvancedSettingsContent.AriaStartStopIconControl;
    internal Button AriaStartStopButtonControl => AdvancedSettingsContent.AriaStartStopButtonControl;
    internal Button AriaRestartButtonControl => AdvancedSettingsContent.AriaRestartButtonControl;

    // About section controls
    internal TextBlock AboutVersionTextControl => AboutSettingsContent.AboutVersionTextControl;
    internal TextBlock AboutCloneCommandTextControl => AboutSettingsContent.AboutCloneCommandTextControl;

    // Events
    internal event SelectionChangedEventHandler? SectionSelectionChanged;
    internal event EventHandler<GeneralSettingChangedEventArgs>? GeneralSettingChanged;
    internal event EventHandler<CloseBehaviorSettingChangedEventArgs>? CloseBehaviorSettingChanged;
    internal event RoutedEventHandler? BrowseDownloadDirectoryRequested;
    internal event RoutedEventHandler? DownloadSettingChanged;
    internal event RoutedEventHandler? BitTorrentSettingChanged;
    internal event RoutedEventHandler? AddBtCustomTrackerRequested;
    internal event RoutedEventHandler? SyncBtTrackerRequested;
    internal event RoutedEventHandler? NetworkSettingChanged;
    internal event RoutedEventHandler? DetectSystemProxyRequested;
    internal event RoutedEventHandler? RandomBtPortRequested;
    internal event RoutedEventHandler? RandomDhtPortRequested;
    internal event RoutedEventHandler? UserAgentPresetRequested;
    internal event RoutedEventHandler? AdvancedSettingChanged;
    internal event RoutedEventHandler? BrowseAriaPathRequested;
    internal event RoutedEventHandler? CopyRpcSecretRequested;
    internal event RoutedEventHandler? GenerateRpcSecretRequested;
    internal event RoutedEventHandler? CopyExtensionApiSecretRequested;
    internal event RoutedEventHandler? GenerateExtensionApiSecretRequested;
    internal event RoutedEventHandler? OpenConfigFolderRequested;
    internal event RoutedEventHandler? OpenLogFolderRequested;
    internal event RoutedEventHandler? ClearSessionRequested;
    internal event RoutedEventHandler? StartStopAriaRequested;
    internal event RoutedEventHandler? RestartAriaRequested;
    internal event RoutedEventHandler? CopyCloneCommandRequested;
    internal event RoutedEventHandler? OpenAboutLinkRequested;

    internal void ApplySearchFilter(string query)
    {
        foreach (SettingSearchEntry entry in GetSearchEntries())
        {
            entry.ApplyFilter(query);
        }
    }

    private IEnumerable<SettingSearchEntry> GetSearchEntries()
    {
        foreach (SettingSearchEntry entry in GeneralSettingsContent.SearchEntries)
        {
            yield return entry;
        }

        foreach (SettingSearchEntry entry in DownloadSettingsContent.SearchEntries)
        {
            yield return entry;
        }

        foreach (SettingSearchEntry entry in BitTorrentSettingsContent.SearchEntries)
        {
            yield return entry;
        }

        foreach (SettingSearchEntry entry in NetworkSettingsContent.SearchEntries)
        {
            yield return entry;
        }

        foreach (SettingSearchEntry entry in AdvancedSettingsContent.SearchEntries)
        {
            yield return entry;
        }

        foreach (SettingSearchEntry entry in AboutSettingsContent.SearchEntries)
        {
            yield return entry;
        }
    }

    internal void ApplyGeneralSettings(GeneralSettings settings, bool isAutoStartEnabled)
    {
        GeneralSettingsContent.ApplyGeneralSettings(settings, isAutoStartEnabled);
    }

    internal GeneralSettings GetGeneralSettings(GeneralSettings currentSettings)
    {
        return GeneralSettingsContent.GetGeneralSettings(currentSettings);
    }

    internal void ApplyCloseBehaviorSettings(CloseBehaviorSettings settings)
    {
        GeneralSettingsContent.ApplyCloseBehaviorSettings(settings);
    }

    internal void SetAutoStartEnabled(bool isEnabled)
    {
        GeneralSettingsContent.SetAutoStartEnabled(isEnabled);
    }

    internal bool IsAutoStartEnabled => GeneralSettingsContent.IsAutoStartEnabled;

    private void SettingsSectionListView_SelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        SectionSelectionChanged?.Invoke(sender, args);
    }
}
