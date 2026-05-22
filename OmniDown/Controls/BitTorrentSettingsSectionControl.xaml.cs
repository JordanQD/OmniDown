using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OmniDown.Services.Localization;
using System;
using System.Collections.Generic;

namespace OmniDown.Controls;

public sealed partial class BitTorrentSettingsSectionControl : UserControl
{
    public BitTorrentSettingsSectionControl()
    {
        InitializeComponent();
    }

    internal IEnumerable<SettingSearchEntry> SearchEntries =>
    [
        new(BtAutoDownloadSettingCard, "bittorrent", "torrent", "metalink", "magnet", "auto", "内容", "自动"),
        new(BtForceEncryptionSettingCard, "bittorrent", "encryption", "crypto", "加密"),
        new(BtKeepSeedingSettingCard, "bittorrent", "seed", "keep", "ratio", "time", "bt", "做种", "分享率", "时间"),
        new(BtMaxPeersSettingCard, "bittorrent", "peer", "max", "bt", "连接"),
        new(BtTrackerSourceSettingCard, "bittorrent", "tracker", "source", "sync", "bt", "同步"),
        new(BtTrackerCustomSourceSettingCard, "bittorrent", "tracker", "custom", "url", "bt", "自定义"),
        new(BtTrackerListSettingCard, "bittorrent", "tracker", "list", "bt"),
        new(BtAutoSyncTrackerSettingCard, "bittorrent", "tracker", "auto sync", "bt", "自动同步")
    ];

    internal StackPanel BitTorrentSettingsContentControl => BitTorrentSettingsContent;
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

    internal event RoutedEventHandler? BitTorrentSettingChanged;
    internal event RoutedEventHandler? AddBtCustomTrackerRequested;
    internal event RoutedEventHandler? SyncBtTrackerRequested;

    private void AddBtCustomTrackerButton_Click(object sender, RoutedEventArgs args)
    {
        AddBtCustomTrackerRequested?.Invoke(sender, args);
    }

    private void SyncBtTrackerButton_Click(object sender, RoutedEventArgs args)
    {
        SyncBtTrackerRequested?.Invoke(sender, args);
    }

    private void BitTorrentSettingToggleSwitch_Toggled(object sender, RoutedEventArgs args)
    {
        UpdateToggleStateText(sender as ToggleSwitch);
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

    private void UpdateToggleStateText(ToggleSwitch? toggleSwitch)
    {
        if (toggleSwitch is null) return;

        TextBlock? stateText = GetToggleStateText(toggleSwitch);
        if (stateText is not null)
        {
            stateText.Text = toggleSwitch.IsOn ? Strings.Get("ToggleOnState.Text") : Strings.Get("ToggleOffState.Text");
        }
    }

    private TextBlock? GetToggleStateText(ToggleSwitch toggleSwitch)
    {
        if (ReferenceEquals(toggleSwitch, BtAutoDownloadToggleSwitch)) return BtAutoDownloadStateText;
        if (ReferenceEquals(toggleSwitch, BtForceEncryptionToggleSwitch)) return BtForceEncryptionStateText;
        if (ReferenceEquals(toggleSwitch, BtAutoSyncTrackerToggleSwitch)) return BtAutoSyncTrackerStateText;
        return null;
    }
}
