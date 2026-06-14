using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OmniDown.Services.Localization;
using System.Collections.Generic;

namespace OmniDown.Controls;

public sealed partial class Ed2kSettingsSectionControl : UserControl
{
    public Ed2kSettingsSectionControl()
    {
        InitializeComponent();
    }

    internal IEnumerable<SettingSearchEntry> SearchEntries =>
    [
        new(Ed2kListenPortSettingCard, "ed2k", "port", "端口", "监听"),
        new(Ed2kUdpListenPortSettingCard, "ed2k", "udp", "port", "端口"),
        new(Ed2kUploadSlotsSettingCard, "ed2k", "upload", "slots", "上传", "槽位"),
        new(Ed2kServerListUrlSettingCard, "ed2k", "server.met", "url", "服务器", "来源"),
        new(Ed2kKadBootstrapUrlSettingCard, "ed2k", "nodes.dat", "kad", "url", "节点", "来源"),
        new(Ed2kServerListSettingCard, "ed2k", "server", "服务器", "列表"),
        new(Ed2kAutoSyncSettingCard, "ed2k", "sync", "auto", "同步", "自动"),
        new(Ed2kSearchKeywordSettingCard, "ed2k", "search", "keyword", "搜索", "关键词"),
        new(Ed2kFileTypeSettingCard, "ed2k", "search", "file", "type", "文件", "类型"),
        new(Ed2kMinSourcesSettingCard, "ed2k", "search", "source", "来源", "最少"),
        new(Ed2kSearchTimeoutSettingCard, "ed2k", "search", "timeout", "time", "时长", "搜索")
    ];

    internal StackPanel Ed2kSettingsContentControl => Ed2kSettingsContent;
    internal NumberBox Ed2kListenPortNumberBoxControl => Ed2kListenPortNumberBox;
    internal NumberBox Ed2kUdpListenPortNumberBoxControl => Ed2kUdpListenPortNumberBox;
    internal NumberBox Ed2kUploadSlotsNumberBoxControl => Ed2kUploadSlotsNumberBox;
    internal TextBox Ed2kServerListUrlTextBoxControl => Ed2kServerListUrlTextBox;
    internal TextBox Ed2kKadBootstrapUrlTextBoxControl => Ed2kKadBootstrapUrlTextBox;
    internal TextBox Ed2kServerListTextBoxControl => Ed2kServerListTextBox;
    internal ToggleSwitch Ed2kAutoSyncToggleSwitchControl => Ed2kAutoSyncToggleSwitch;
    internal TextBlock Ed2kAutoSyncStateTextControl => Ed2kAutoSyncStateText;
    internal ComboBox Ed2kSyncIntervalComboBoxControl => Ed2kSyncIntervalComboBox;
    internal Button Ed2kSyncNowButtonControl => Ed2kSyncNowButton;
    internal TextBlock Ed2kLastSyncTextControl => Ed2kLastSyncText;
    internal TextBox Ed2kSearchKeywordTextBoxControl => Ed2kSearchKeywordTextBox;
    internal Button Ed2kSearchKeywordButtonControl => Ed2kSearchKeywordButton;
    internal ComboBox Ed2kFileTypeComboBoxControl => Ed2kFileTypeComboBox;
    internal NumberBox Ed2kMinSourcesNumberBoxControl => Ed2kMinSourcesNumberBox;
    internal NumberBox Ed2kSearchTimeoutNumberBoxControl => Ed2kSearchTimeoutNumberBox;

    internal event RoutedEventHandler? Ed2kSettingChanged;
    internal event RoutedEventHandler? RandomEd2kPortRequested;
    internal event RoutedEventHandler? RandomEd2kUdpPortRequested;
    internal event RoutedEventHandler? SyncEd2kRequested;
    internal event RoutedEventHandler? SearchEd2kRequested;

    private void Ed2kSettingToggleSwitch_Toggled(object sender, RoutedEventArgs args)
    {
        UpdateToggleStateText(sender as ToggleSwitch);
        Ed2kSettingChanged?.Invoke(sender, args);
    }

    private void Ed2kSettingTextBox_TextChanged(object sender, TextChangedEventArgs args)
    {
        Ed2kSettingChanged?.Invoke(sender, new RoutedEventArgs());
    }

    private void Ed2kSettingNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        Ed2kSettingChanged?.Invoke(sender, new RoutedEventArgs());
    }

    private void Ed2kSettingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        Ed2kSettingChanged?.Invoke(sender, new RoutedEventArgs());
    }

    private void RandomEd2kPortButton_Click(object sender, RoutedEventArgs args)
    {
        RandomEd2kPortRequested?.Invoke(sender, args);
    }

    private void RandomEd2kUdpPortButton_Click(object sender, RoutedEventArgs args)
    {
        RandomEd2kUdpPortRequested?.Invoke(sender, args);
    }

    private void SyncEd2kButton_Click(object sender, RoutedEventArgs args)
    {
        SyncEd2kRequested?.Invoke(sender, args);
    }

    private void SearchEd2kButton_Click(object sender, RoutedEventArgs args)
    {
        SearchEd2kRequested?.Invoke(sender, args);
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
        if (ReferenceEquals(toggleSwitch, Ed2kAutoSyncToggleSwitch)) return Ed2kAutoSyncStateText;
        return null;
    }
}
