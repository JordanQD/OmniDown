using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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
    internal Border BtEnableSettingCardControl => BtEnableSettingCard;
    internal Border BtPortSettingCardControl => BtPortSettingCard;
    internal Border BtSeedRatioSettingCardControl => BtSeedRatioSettingCard;
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
    internal ToggleSwitch RestoreWindowPlacementToggleSwitchControl => RestoreWindowPlacementToggleSwitch;
    internal ToggleSwitch ResumeDownloadsOnLaunchToggleSwitchControl => ResumeDownloadsOnLaunchToggleSwitch;
    internal ToggleSwitch AutoClearCompletedOnExitToggleSwitchControl => AutoClearCompletedOnExitToggleSwitch;
    internal ToggleSwitch PauseActiveOnExitToggleSwitchControl => PauseActiveOnExitToggleSwitch;
    internal ToggleSwitch CloseToTrayToggleSwitchControl => CloseToTrayToggleSwitch;
    internal ToggleSwitch ShowTaskbarProgressToggleSwitchControl => ShowTaskbarProgressToggleSwitch;
    internal ComboBox ThemeComboBoxControl => ThemeComboBox;
    internal ToggleSwitch SystemNotificationsToggleSwitchControl => SystemNotificationsToggleSwitch;
    internal ToggleSwitch DownloadStartNotificationsToggleSwitchControl => DownloadStartNotificationsToggleSwitch;
    internal ToggleSwitch DownloadCompleteNotificationsToggleSwitchControl => DownloadCompleteNotificationsToggleSwitch;
    internal ToggleSwitch AutoShutdownWhenCompleteToggleSwitchControl => AutoShutdownWhenCompleteToggleSwitch;
    internal ToggleSwitch PreventSleepWhileDownloadingToggleSwitchControl => PreventSleepWhileDownloadingToggleSwitch;
    internal TextBox DownloadDirectoryTextBoxControl => DownloadDirectoryTextBox;
    internal NumberBox MaxConcurrentDownloadsNumberBoxControl => MaxConcurrentDownloadsNumberBox;
    internal NumberBox SplitCountNumberBoxControl => SplitCountNumberBox;
    internal NumberBox MaxConnectionPerServerNumberBoxControl => MaxConnectionPerServerNumberBox;
    internal ToggleSwitch ContinueDownloadToggleSwitchControl => ContinueDownloadToggleSwitch;
    internal ComboBox RemoteTimeComboBoxControl => RemoteTimeComboBox;
    internal NumberBox MaxTriesNumberBoxControl => MaxTriesNumberBox;
    internal NumberBox RetryWaitNumberBoxControl => RetryWaitNumberBox;
    internal ToggleSwitch AutoDeleteStaleRecordsToggleSwitchControl => AutoDeleteStaleRecordsToggleSwitch;
    internal ToggleSwitch DeleteTorrentAfterCompleteToggleSwitchControl => DeleteTorrentAfterCompleteToggleSwitch;
    internal ToggleSwitch UseSystemProxyCheckBoxControl => UseSystemProxyCheckBox;
    internal TextBox AriaPathTextBoxControl => AriaPathTextBox;
    internal NumberBox RpcPortNumberBoxControl => RpcPortNumberBox;
    internal TextBlock SettingsAriaStatusTextControl => SettingsAriaStatusText;
    internal StackPanel ProcessStatusSettingControlControl => ProcessStatusSettingControl;
    internal TextBlock AboutVersionTextControl => AboutVersionText;
    internal TextBlock AboutCloneCommandTextControl => AboutCloneCommandText;

    internal event SelectionChangedEventHandler? SectionSelectionChanged;
    internal event RoutedEventHandler? SettingToggleSwitchToggled;
    internal event SelectionChangedEventHandler? ThemeSelectionChanged;
    internal event RoutedEventHandler? BrowseDownloadDirectoryRequested;
    internal event RoutedEventHandler? DownloadSettingChanged;
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
