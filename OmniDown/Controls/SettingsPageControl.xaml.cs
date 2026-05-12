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
    internal Border DownloadCompleteNotificationActionSettingCardControl => DownloadCompleteNotificationActionSettingCard;
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
    internal Border RetrySettingCardControl => RetrySettingCard;
    internal Border AriaPathSettingCardControl => AriaPathSettingCard;
    internal Border RpcPortSettingCardControl => RpcPortSettingCard;
    internal Border ProcessStatusSettingCardControl => ProcessStatusSettingCard;
    internal Border TerminalSettingCardControl => TerminalSettingCard;
    internal Border AboutAppCardControl => AboutAppCard;
    internal Border AboutCloneCardControl => AboutCloneCard;
    internal Border AboutIssueCardControl => AboutIssueCard;
    internal Border AboutReferencesCardControl => AboutReferencesCard;
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
    internal ComboBox DownloadCompleteNotificationActionComboBoxControl => DownloadCompleteNotificationActionComboBox;
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
    internal ToggleSwitch TerminalOutputToggleSwitchControl => TerminalOutputToggleSwitch;
    internal TextBlock TerminalOutputStateTextControl => TerminalOutputStateText;
    internal TextBox AriaPathTextBoxControl => AriaPathTextBox;
    internal NumberBox RpcPortNumberBoxControl => RpcPortNumberBox;
    internal TextBlock SettingsAriaStatusTextControl => SettingsAriaStatusText;
    internal StackPanel ProcessStatusSettingControlControl => ProcessStatusSettingControl;
    internal TextBlock AboutVersionTextControl => AboutVersionText;
    internal TextBlock AboutCloneCommandTextControl => AboutCloneCommandText;

    internal event SelectionChangedEventHandler? SectionSelectionChanged;
    internal event RoutedEventHandler? SettingToggleSwitchToggled;
    internal event SelectionChangedEventHandler? ThemeSelectionChanged;
    internal event SelectionChangedEventHandler? NotificationActionSelectionChanged;
    internal event RoutedEventHandler? BrowseDownloadDirectoryRequested;
    internal event RoutedEventHandler? DownloadSettingChanged;
    internal event RoutedEventHandler? BitTorrentSettingChanged;
    internal event RoutedEventHandler? AddBtCustomTrackerRequested;
    internal event RoutedEventHandler? SyncBtTrackerRequested;
    internal event RoutedEventHandler? StartAriaRequested;
    internal event RoutedEventHandler? StopAriaRequested;
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

    private void NotificationActionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        NotificationActionSelectionChanged?.Invoke(sender, args);
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

    private void StartAriaButton_Click(object sender, RoutedEventArgs args)
    {
        StartAriaRequested?.Invoke(sender, args);
    }

    private void StopAriaButton_Click(object sender, RoutedEventArgs args)
    {
        StopAriaRequested?.Invoke(sender, args);
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
