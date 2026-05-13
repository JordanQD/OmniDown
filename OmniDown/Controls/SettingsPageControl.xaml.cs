using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System;

namespace OmniDown.Controls;

public sealed partial class SettingsPageControl : UserControl
{
    public SettingsPageControl()
    {
        InitializeComponent();
    }

    internal ListView SettingsSectionListViewControl => SettingsSectionListView;
    internal ScrollViewer SettingsContentScrollViewerControl => SettingsContentScrollViewer;
    internal StackPanel GeneralSettingsContentControl => GeneralSettingsContent;
    internal StackPanel DownloadSettingsContentControl => DownloadSettingsContent;
    internal StackPanel BitTorrentSettingsContentControl => BitTorrentSettingsContent;
    internal StackPanel NetworkSettingsContentControl => NetworkSettingsContent;
    internal StackPanel AdvancedSettingsContentControl => AdvancedSettingsContent;
    internal StackPanel AboutSettingsContentControl => AboutSettingsContent;
    internal Border StartupSettingCardControl => StartupSettingCard;
    internal Border RestoreWindowSettingCardControl => RestoreWindowSettingCard;
    internal Border ResumeOnLaunchSettingCardControl => ResumeOnLaunchSettingCard;
    internal Border ExitCleanupSettingCardControl => ExitCleanupSettingCard;
    internal Border PauseActiveOnExitSettingCardControl => PauseActiveOnExitSettingCard;
    internal Border CloseBehaviorSettingCardControl => CloseBehaviorSettingCard;
    internal Border TaskbarProgressSettingCardControl => TaskbarProgressSettingCard;
    internal Border ThemeSettingCardControl => ThemeSettingCard;
    internal Border NotificationsSettingCardControl => NotificationsSettingCard;
    internal Border DownloadStartNotificationSettingCardControl => DownloadStartNotificationSettingCard;
    internal Border DownloadCompleteNotificationSettingCardControl => DownloadCompleteNotificationSettingCard;
    internal Border AutoShutdownSettingCardControl => AutoShutdownSettingCard;
    internal Border PreventSleepSettingCardControl => PreventSleepSettingCard;
    internal Border DefaultDirectorySettingCardControl => DefaultDirectorySettingCard;
    internal Border MaxConcurrentDownloadsSettingCardControl => MaxConcurrentDownloadsSettingCard;
    internal Border SplitCountSettingCardControl => SplitCountSettingCard;
    internal Border MaxConnectionPerServerSettingCardControl => MaxConnectionPerServerSettingCard;
    internal Border ContinueDownloadSettingCardControl => ContinueDownloadSettingCard;
    internal Border RemoteTimeSettingCardControl => RemoteTimeSettingCard;
    internal Border MaxTriesSettingCardControl => MaxTriesSettingCard;
    internal Border RetryWaitSettingCardControl => RetryWaitSettingCard;
    internal Border DownloadCleanupSettingCardControl => DownloadCleanupSettingCard;
    internal Border TorrentCleanupSettingCardControl => TorrentCleanupSettingCard;
    internal Border BtAutoDownloadSettingCardControl => BtAutoDownloadSettingCard;
    internal Border BtForceEncryptionSettingCardControl => BtForceEncryptionSettingCard;
    internal Border BtKeepSeedingSettingCardControl => BtKeepSeedingSettingCard;
    internal Border BtMaxPeersSettingCardControl => BtMaxPeersSettingCard;
    internal Border BtTrackerSourceSettingCardControl => BtTrackerSourceSettingCard;
    internal Border BtTrackerCustomSourceSettingCardControl => BtTrackerCustomSourceSettingCard;
    internal Border BtTrackerListSettingCardControl => BtTrackerListSettingCard;
    internal Border BtAutoSyncTrackerSettingCardControl => BtAutoSyncTrackerSettingCard;
    internal Border UseSystemProxySettingCardControl => UseSystemProxySettingCard;
    internal Border CustomProxySettingCardControl => CustomProxySettingCard;
    internal Border UpnpSettingCardControl => UpnpSettingCard;
    internal Border BtPortSettingCardControl => BtPortSettingCard;
    internal Border DhtPortSettingCardControl => DhtPortSettingCard;
    internal Border UserAgentSettingCardControl => UserAgentSettingCard;
    internal Border ConnectTimeoutSettingCardControl => ConnectTimeoutSettingCard;
    internal Border TimeoutSettingCardControl => TimeoutSettingCard;
    internal Border FileAllocationSettingCardControl => FileAllocationSettingCard;
    internal Border AriaPathSettingCardControl => AriaPathSettingCard;
    internal Border RpcPortSettingCardControl => RpcPortSettingCard;
    internal Border RpcSecretSettingCardControl => RpcSecretSettingCard;
    internal Border ProcessStatusSettingCardControl => ProcessStatusSettingCard;
    internal Border ExtensionAutoSubmitSettingCardControl => ExtensionAutoSubmitSettingCard;
    internal Border ExtensionApiPortSettingCardControl => ExtensionApiPortSettingCard;
    internal Border ExtensionApiSecretSettingCardControl => ExtensionApiSecretSettingCard;
    internal Border LogLevelSettingCardControl => LogLevelSettingCard;
    internal Border AdvancedPathsSettingCardControl => AdvancedPathsSettingCard;
    internal Border SessionResetSettingCardControl => SessionResetSettingCard;
    internal Border ClipboardDetectionSettingCardControl => ClipboardDetectionSettingCard;
    internal Border ClipboardTypesSettingCardControl => ClipboardTypesSettingCard;
    internal Border ProtocolMagnetSettingCardControl => ProtocolMagnetSettingCard;
    internal Border ProtocolThunderSettingCardControl => ProtocolThunderSettingCard;
    internal Border ProtocolOmniDownSettingCardControl => ProtocolOmniDownSettingCard;
    internal Border TerminalSettingCardControl => TerminalSettingCard;
    internal Border AboutAppCardControl => AboutAppCard;
    internal Border AboutCloneCardControl => AboutCloneCard;
    internal Border AboutIssueCardControl => AboutIssueCard;
    internal Border AboutReferencesCardControl => AboutReferencesCard;
    internal Border AboutTrackerSourcesCardControl => AboutTrackerSourcesCard;
    internal Border AboutLicenseCardControl => AboutLicenseCard;
    internal ToggleSwitch AutoStartToggleSwitchControl => AutoStartToggleSwitch;
    internal TextBlock AutoStartStateTextControl => AutoStartStateText;
    internal ToggleSwitch RestoreWindowPlacementToggleSwitchControl => RestoreWindowPlacementToggleSwitch;
    internal TextBlock RestoreWindowPlacementStateTextControl => RestoreWindowPlacementStateText;
    internal ToggleSwitch ResumeDownloadsOnLaunchToggleSwitchControl => ResumeDownloadsOnLaunchToggleSwitch;
    internal TextBlock ResumeDownloadsOnLaunchStateTextControl => ResumeDownloadsOnLaunchStateText;
    internal ToggleSwitch AutoClearCompletedOnExitToggleSwitchControl => AutoClearCompletedOnExitToggleSwitch;
    internal TextBlock AutoClearCompletedOnExitStateTextControl => AutoClearCompletedOnExitStateText;
    internal ToggleSwitch PauseActiveOnExitToggleSwitchControl => PauseActiveOnExitToggleSwitch;
    internal TextBlock PauseActiveOnExitStateTextControl => PauseActiveOnExitStateText;
    internal ToggleSwitch CloseToTrayToggleSwitchControl => CloseToTrayToggleSwitch;
    internal TextBlock CloseToTrayStateTextControl => CloseToTrayStateText;
    internal ToggleSwitch ShowTaskbarProgressToggleSwitchControl => ShowTaskbarProgressToggleSwitch;
    internal TextBlock ShowTaskbarProgressStateTextControl => ShowTaskbarProgressStateText;
    internal ComboBox ThemeComboBoxControl => ThemeComboBox;
    internal ToggleSwitch SystemNotificationsToggleSwitchControl => SystemNotificationsToggleSwitch;
    internal TextBlock SystemNotificationsStateTextControl => SystemNotificationsStateText;
    internal ToggleSwitch DownloadStartNotificationsToggleSwitchControl => DownloadStartNotificationsToggleSwitch;
    internal TextBlock DownloadStartNotificationsStateTextControl => DownloadStartNotificationsStateText;
    internal ToggleSwitch DownloadCompleteNotificationsToggleSwitchControl => DownloadCompleteNotificationsToggleSwitch;
    internal TextBlock DownloadCompleteNotificationsStateTextControl => DownloadCompleteNotificationsStateText;
    internal ToggleSwitch AutoShutdownWhenCompleteToggleSwitchControl => AutoShutdownWhenCompleteToggleSwitch;
    internal TextBlock AutoShutdownWhenCompleteStateTextControl => AutoShutdownWhenCompleteStateText;
    internal ToggleSwitch PreventSleepWhileDownloadingToggleSwitchControl => PreventSleepWhileDownloadingToggleSwitch;
    internal TextBlock PreventSleepWhileDownloadingStateTextControl => PreventSleepWhileDownloadingStateText;
    internal TextBox DownloadDirectoryTextBoxControl => DownloadDirectoryTextBox;
    internal NumberBox MaxConcurrentDownloadsNumberBoxControl => MaxConcurrentDownloadsNumberBox;
    internal NumberBox SplitCountNumberBoxControl => SplitCountNumberBox;
    internal NumberBox MaxConnectionPerServerNumberBoxControl => MaxConnectionPerServerNumberBox;
    internal ToggleSwitch ContinueDownloadToggleSwitchControl => ContinueDownloadToggleSwitch;
    internal TextBlock ContinueDownloadStateTextControl => ContinueDownloadStateText;
    internal ComboBox RemoteTimeComboBoxControl => RemoteTimeComboBox;
    internal NumberBox MaxTriesNumberBoxControl => MaxTriesNumberBox;
    internal NumberBox RetryWaitNumberBoxControl => RetryWaitNumberBox;
    internal ToggleSwitch AutoDeleteStaleRecordsToggleSwitchControl => AutoDeleteStaleRecordsToggleSwitch;
    internal TextBlock AutoDeleteStaleRecordsStateTextControl => AutoDeleteStaleRecordsStateText;
    internal ToggleSwitch DeleteTorrentAfterCompleteToggleSwitchControl => DeleteTorrentAfterCompleteToggleSwitch;
    internal TextBlock DeleteTorrentAfterCompleteStateTextControl => DeleteTorrentAfterCompleteStateText;
    internal ToggleSwitch BtAutoDownloadToggleSwitchControl => BtAutoDownloadToggleSwitch;
    internal TextBlock BtAutoDownloadStateTextControl => BtAutoDownloadStateText;
    internal ToggleSwitch BtForceEncryptionToggleSwitchControl => BtForceEncryptionToggleSwitch;
    internal TextBlock BtForceEncryptionStateTextControl => BtForceEncryptionStateText;
    internal ComboBox BtSeedingModeComboBoxControl => BtSeedingModeComboBox;
    internal NumberBox BtSeedRatioNumberBoxControl => BtSeedRatioNumberBox;
    internal NumberBox BtSeedTimeNumberBoxControl => BtSeedTimeNumberBox;
    internal NumberBox BtMaxPeersNumberBoxControl => BtMaxPeersNumberBox;
    internal Button BtTrackerSourceDropDownButtonControl => BtTrackerSourceDropDownButton;
    internal TextBlock BtTrackerSourceSummaryTextControl => BtTrackerSourceSummaryText;
    internal CheckBox BtTrackerNgosangBestCheckBoxControl => BtTrackerNgosangBestCheckBox;
    internal CheckBox BtTrackerNgosangBestIpCheckBoxControl => BtTrackerNgosangBestIpCheckBox;
    internal CheckBox BtTrackerNgosangAllCheckBoxControl => BtTrackerNgosangAllCheckBox;
    internal CheckBox BtTrackerNgosangAllIpCheckBoxControl => BtTrackerNgosangAllIpCheckBox;
    internal CheckBox BtTrackerNgosangCdnBestCheckBoxControl => BtTrackerNgosangCdnBestCheckBox;
    internal CheckBox BtTrackerNgosangCdnBestIpCheckBoxControl => BtTrackerNgosangCdnBestIpCheckBox;
    internal CheckBox BtTrackerNgosangCdnAllCheckBoxControl => BtTrackerNgosangCdnAllCheckBox;
    internal CheckBox BtTrackerNgosangCdnAllIpCheckBoxControl => BtTrackerNgosangCdnAllIpCheckBox;
    internal CheckBox BtTrackerXiu2BestCheckBoxControl => BtTrackerXiu2BestCheckBox;
    internal CheckBox BtTrackerXiu2AllCheckBoxControl => BtTrackerXiu2AllCheckBox;
    internal CheckBox BtTrackerXiu2HttpCheckBoxControl => BtTrackerXiu2HttpCheckBox;
    internal CheckBox BtTrackerXiu2CdnBestCheckBoxControl => BtTrackerXiu2CdnBestCheckBox;
    internal CheckBox BtTrackerXiu2CdnAllCheckBoxControl => BtTrackerXiu2CdnAllCheckBox;
    internal CheckBox BtTrackerXiu2CdnHttpCheckBoxControl => BtTrackerXiu2CdnHttpCheckBox;
    internal TextBox BtCustomTrackerSourceTextBoxControl => BtCustomTrackerSourceTextBox;
    internal ListView BtCustomTrackerSourceListViewControl => BtCustomTrackerSourceListView;
    internal TextBox BtTrackerSourceTextBoxControl => BtTrackerSourceTextBox;
    internal Button BtSyncTrackerButtonControl => BtSyncTrackerButton;
    internal TextBox BtTrackerListTextBoxControl => BtTrackerListTextBox;
    internal ToggleSwitch BtAutoSyncTrackerToggleSwitchControl => BtAutoSyncTrackerToggleSwitch;
    internal TextBlock BtAutoSyncTrackerStateTextControl => BtAutoSyncTrackerStateText;
    internal TextBlock BtLastTrackerSyncTextControl => BtLastTrackerSyncText;
    internal ToggleSwitch UseSystemProxyCheckBoxControl => UseSystemProxyCheckBox;
    internal TextBlock UseSystemProxyStateTextControl => UseSystemProxyStateText;
    internal ToggleSwitch CustomProxyToggleSwitchControl => CustomProxyToggleSwitch;
    internal TextBlock CustomProxyStateTextControl => CustomProxyStateText;
    internal TextBox ProxyServerTextBoxControl => ProxyServerTextBox;
    internal Button DetectSystemProxyButtonControl => DetectSystemProxyButton;
    internal TextBox ProxyBypassTextBoxControl => ProxyBypassTextBox;
    internal CheckBox ProxyDownloadsCheckBoxControl => ProxyDownloadsCheckBox;
    internal CheckBox ProxyTrackersCheckBoxControl => ProxyTrackersCheckBox;
    internal ToggleSwitch EnableUpnpToggleSwitchControl => EnableUpnpToggleSwitch;
    internal TextBlock EnableUpnpStateTextControl => EnableUpnpStateText;
    internal NumberBox BtListenPortNumberBoxControl => BtListenPortNumberBox;
    internal NumberBox DhtListenPortNumberBoxControl => DhtListenPortNumberBox;
    internal TextBox UserAgentTextBoxControl => UserAgentTextBox;
    internal NumberBox ConnectTimeoutNumberBoxControl => ConnectTimeoutNumberBox;
    internal NumberBox TimeoutNumberBoxControl => TimeoutNumberBox;
    internal ComboBox FileAllocationComboBoxControl => FileAllocationComboBox;
    internal ToggleSwitch TerminalOutputToggleSwitchControl => TerminalOutputToggleSwitch;
    internal TextBlock TerminalOutputStateTextControl => TerminalOutputStateText;
    internal TextBox AriaPathTextBoxControl => AriaPathTextBox;
    internal NumberBox RpcPortNumberBoxControl => RpcPortNumberBox;
    internal PasswordBox RpcSecretPasswordBoxControl => RpcSecretPasswordBox;
    internal ToggleSwitch ExtensionAutoSubmitToggleSwitchControl => ExtensionAutoSubmitToggleSwitch;
    internal TextBlock ExtensionAutoSubmitStateTextControl => ExtensionAutoSubmitStateText;
    internal NumberBox ExtensionApiPortNumberBoxControl => ExtensionApiPortNumberBox;
    internal PasswordBox ExtensionApiSecretPasswordBoxControl => ExtensionApiSecretPasswordBox;
    internal ComboBox LogLevelComboBoxControl => LogLevelComboBox;
    internal TextBlock AdvancedPathsSummaryTextControl => AdvancedPathsSummaryText;
    internal ToggleSwitch ClipboardDetectionToggleSwitchControl => ClipboardDetectionToggleSwitch;
    internal TextBlock ClipboardDetectionStateTextControl => ClipboardDetectionStateText;
    internal ToggleSwitch ClipboardHttpToggleSwitchControl => ClipboardHttpToggleSwitch;
    internal ToggleSwitch ClipboardFtpToggleSwitchControl => ClipboardFtpToggleSwitch;
    internal ToggleSwitch ClipboardMagnetToggleSwitchControl => ClipboardMagnetToggleSwitch;
    internal ToggleSwitch ClipboardThunderToggleSwitchControl => ClipboardThunderToggleSwitch;
    internal ToggleSwitch ClipboardBtHashToggleSwitchControl => ClipboardBtHashToggleSwitch;
    internal ToggleSwitch ProtocolMagnetToggleSwitchControl => ProtocolMagnetToggleSwitch;
    internal TextBlock ProtocolMagnetStateTextControl => ProtocolMagnetStateText;
    internal ToggleSwitch ProtocolThunderToggleSwitchControl => ProtocolThunderToggleSwitch;
    internal TextBlock ProtocolThunderStateTextControl => ProtocolThunderStateText;
    internal ToggleSwitch ProtocolOmniDownToggleSwitchControl => ProtocolOmniDownToggleSwitch;
    internal TextBlock ProtocolOmniDownStateTextControl => ProtocolOmniDownStateText;
    internal TextBlock SettingsAriaStatusTextControl => SettingsAriaStatusText;
    internal StackPanel ProcessStatusSettingControlControl => ProcessStatusSettingControl;
    internal FontIcon AriaStartStopIconControl => AriaStartStopIcon;
    internal Button AriaStartStopButtonControl => AriaStartStopButton;
    internal Button AriaRestartButtonControl => AriaRestartButton;
    internal TextBlock AboutVersionTextControl => AboutVersionText;
    internal TextBlock AboutCloneCommandTextControl => AboutCloneCommandText;

    internal event SelectionChangedEventHandler? SectionSelectionChanged;
    internal event RoutedEventHandler? SettingToggleSwitchToggled;
    internal event SelectionChangedEventHandler? ThemeSelectionChanged;
    internal event RoutedEventHandler? BrowseDownloadDirectoryRequested;
    internal event RoutedEventHandler? DownloadSettingChanged;
    internal event RoutedEventHandler? BitTorrentSettingChanged;
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
    internal event RoutedEventHandler? CopySessionPathRequested;
    internal event RoutedEventHandler? ClearSessionRequested;
    internal event RoutedEventHandler? AddBtCustomTrackerRequested;
    internal event RoutedEventHandler? SyncBtTrackerRequested;
    internal event RoutedEventHandler? StartStopAriaRequested;
    internal event RoutedEventHandler? RestartAriaRequested;
    internal event RoutedEventHandler? CopyCloneCommandRequested;
    internal event RoutedEventHandler? OpenAboutLinkRequested;

    private void SettingsSectionListView_SelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        SectionSelectionChanged?.Invoke(sender, args);
    }

    private void SettingToggleSwitch_Toggled(object sender, RoutedEventArgs args)
    {
        SettingToggleSwitchToggled?.Invoke(sender, args);
    }

    private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        ThemeSelectionChanged?.Invoke(sender, args);
    }

    private void AdvancedSettingTextBox_TextChanged(object sender, TextChangedEventArgs args)
    {
        AdvancedSettingChanged?.Invoke(sender, new RoutedEventArgs());
    }

    private void AdvancedSettingPasswordBox_PasswordChanged(object sender, RoutedEventArgs args)
    {
        AdvancedSettingChanged?.Invoke(sender, args);
    }

    private void AdvancedSettingNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        AdvancedSettingChanged?.Invoke(sender, new RoutedEventArgs());
    }

    private void AdvancedSettingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        AdvancedSettingChanged?.Invoke(sender, new RoutedEventArgs());
    }

    private void AdvancedSettingToggleSwitch_Toggled(object sender, RoutedEventArgs args)
    {
        SettingToggleSwitch_Toggled(sender, args);
        AdvancedSettingChanged?.Invoke(sender, args);
    }

    private void BrowseAriaPathButton_Click(object sender, RoutedEventArgs args)
    {
        BrowseAriaPathRequested?.Invoke(sender, args);
    }

    private void CopyRpcSecretButton_Click(object sender, RoutedEventArgs args)
    {
        CopyRpcSecretRequested?.Invoke(sender, args);
    }

    private void GenerateRpcSecretButton_Click(object sender, RoutedEventArgs args)
    {
        GenerateRpcSecretRequested?.Invoke(sender, args);
    }

    private void CopyExtensionApiSecretButton_Click(object sender, RoutedEventArgs args)
    {
        CopyExtensionApiSecretRequested?.Invoke(sender, args);
    }

    private void GenerateExtensionApiSecretButton_Click(object sender, RoutedEventArgs args)
    {
        GenerateExtensionApiSecretRequested?.Invoke(sender, args);
    }

    private void OpenConfigFolderButton_Click(object sender, RoutedEventArgs args)
    {
        OpenConfigFolderRequested?.Invoke(sender, args);
    }

    private void CopySessionPathButton_Click(object sender, RoutedEventArgs args)
    {
        CopySessionPathRequested?.Invoke(sender, args);
    }

    private void ClearSessionButton_Click(object sender, RoutedEventArgs args)
    {
        ClearSessionRequested?.Invoke(sender, args);
    }

    private void BrowseDownloadDirectoryButton_Click(object sender, RoutedEventArgs args)
    {
        BrowseDownloadDirectoryRequested?.Invoke(sender, args);
    }

    private void DownloadSettingTextBox_TextChanged(object sender, TextChangedEventArgs args)
    {
        DownloadSettingChanged?.Invoke(sender, new RoutedEventArgs());
    }

    private void DownloadSettingNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        DownloadSettingChanged?.Invoke(sender, new RoutedEventArgs());
    }

    private void DownloadSettingToggleSwitch_Toggled(object sender, RoutedEventArgs args)
    {
        SettingToggleSwitch_Toggled(sender, args);
        DownloadSettingChanged?.Invoke(sender, args);
    }

    private void DownloadSettingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        DownloadSettingChanged?.Invoke(sender, new RoutedEventArgs());
    }

    private void BitTorrentSettingToggleSwitch_Toggled(object sender, RoutedEventArgs args)
    {
        SettingToggleSwitch_Toggled(sender, args);
        BitTorrentSettingChanged?.Invoke(sender, args);
    }

    private void BitTorrentSettingTextBox_TextChanged(object sender, TextChangedEventArgs args)
    {
        BitTorrentSettingChanged?.Invoke(sender, new RoutedEventArgs());
    }

    private void BitTorrentSettingNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        BitTorrentSettingChanged?.Invoke(sender, new RoutedEventArgs());
    }

    private void BitTorrentSettingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        BitTorrentSettingChanged?.Invoke(sender, new RoutedEventArgs());
    }

    private void BitTorrentSettingCheckBox_Changed(object sender, RoutedEventArgs args)
    {
        BitTorrentSettingChanged?.Invoke(sender, args);
    }

    private void BitTorrentSettingListView_SelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        BitTorrentSettingChanged?.Invoke(sender, new RoutedEventArgs());
    }

    private void NetworkSettingToggleSwitch_Toggled(object sender, RoutedEventArgs args)
    {
        SettingToggleSwitch_Toggled(sender, args);
        NetworkSettingChanged?.Invoke(sender, args);
    }

    private void NetworkSettingTextBox_TextChanged(object sender, TextChangedEventArgs args)
    {
        NetworkSettingChanged?.Invoke(sender, new RoutedEventArgs());
    }

    private void NetworkSettingNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        NetworkSettingChanged?.Invoke(sender, new RoutedEventArgs());
    }

    private void NetworkSettingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        NetworkSettingChanged?.Invoke(sender, new RoutedEventArgs());
    }

    private void NetworkSettingCheckBox_Changed(object sender, RoutedEventArgs args)
    {
        NetworkSettingChanged?.Invoke(sender, args);
    }

    private void DetectSystemProxyButton_Click(object sender, RoutedEventArgs args)
    {
        DetectSystemProxyRequested?.Invoke(sender, args);
    }

    private void RandomBtPortButton_Click(object sender, RoutedEventArgs args)
    {
        RandomBtPortRequested?.Invoke(sender, args);
    }

    private void RandomDhtPortButton_Click(object sender, RoutedEventArgs args)
    {
        RandomDhtPortRequested?.Invoke(sender, args);
    }

    private void UserAgentPresetButton_Click(object sender, RoutedEventArgs args)
    {
        UserAgentPresetRequested?.Invoke(sender, args);
    }

    private void AddBtCustomTrackerButton_Click(object sender, RoutedEventArgs args)
    {
        AddBtCustomTrackerRequested?.Invoke(sender, args);
    }

    private void ToggleBtSeedSettingsButton_Click(object sender, RoutedEventArgs args)
    {
        bool shouldExpand = BtSeedLimitsBox.Visibility != Visibility.Visible;
        AnimateSettingsPanel(BtSeedLimitsBox, shouldExpand);
        BtSeedChevronIcon.Glyph = shouldExpand ? "\uE70E" : "\uE70D";
    }

    private void ToggleBtTrackerListButton_Click(object sender, RoutedEventArgs args)
    {
        bool shouldExpand = BtTrackerListBox.Visibility != Visibility.Visible;
        AnimateSettingsPanel(BtTrackerListBox, shouldExpand);
        BtTrackerListChevronIcon.Glyph = shouldExpand ? "\uE70E" : "\uE70D";
    }

    private void ToggleClipboardTypesButton_Click(object sender, RoutedEventArgs args)
    {
        bool shouldExpand = ClipboardTypesBox.Visibility != Visibility.Visible;
        AnimateSettingsPanel(ClipboardTypesBox, shouldExpand);
        ClipboardTypesChevronIcon.Glyph = shouldExpand ? "\uE70E" : "\uE70D";
    }

    private static void AnimateSettingsPanel(UIElement panel, bool expand)
    {
        if (expand)
        {
            panel.Visibility = Visibility.Visible;
        }

        DoubleAnimation opacityAnimation = new()
        {
            From = expand ? 0 : 1,
            To = expand ? 1 : 0,
            Duration = new Duration(TimeSpan.FromMilliseconds(140)),
            EnableDependentAnimation = true
        };

        Storyboard.SetTarget(opacityAnimation, panel);
        Storyboard.SetTargetProperty(opacityAnimation, "Opacity");

        Storyboard storyboard = new();
        storyboard.Children.Add(opacityAnimation);
        if (!expand)
        {
            storyboard.Completed += (_, _) => panel.Visibility = Visibility.Collapsed;
        }

        storyboard.Begin();
    }

    private void SyncBtTrackerButton_Click(object sender, RoutedEventArgs args)
    {
        SyncBtTrackerRequested?.Invoke(sender, args);
    }

    private void StartStopAriaButton_Click(object sender, RoutedEventArgs args)
    {
        StartStopAriaRequested?.Invoke(sender, args);
    }

    private void RestartAriaButton_Click(object sender, RoutedEventArgs args)
    {
        RestartAriaRequested?.Invoke(sender, args);
    }

    private void CopyCloneCommandButton_Click(object sender, RoutedEventArgs args)
    {
        CopyCloneCommandRequested?.Invoke(sender, args);
    }

    private void OpenAboutLinkButton_Click(object sender, RoutedEventArgs args)
    {
        OpenAboutLinkRequested?.Invoke(sender, args);
    }
}
