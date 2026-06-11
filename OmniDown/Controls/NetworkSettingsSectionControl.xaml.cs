using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CommunityToolkit.WinUI.Controls;
using OmniDown.Services.Localization;
using System.Collections.Generic;

namespace OmniDown.Controls;

public sealed partial class NetworkSettingsSectionControl : UserControl
{
    public NetworkSettingsSectionControl()
    {
        InitializeComponent();
    }

    internal IEnumerable<SettingSearchEntry> SearchEntries =>
    [
        new(DownloadProxySettingCard, "proxy", "http", "https", "download", "tracker", "update", "代理"),
        new(UpnpSettingCard, "upnp", "nat", "pmp", "port", "端口", "映射"),
        new(BtPortSettingCard, "bt", "bittorrent", "listen", "port", "监听", "端口"),
        new(DhtPortSettingCard, "dht", "listen", "port", "监听", "端口"),
        new(UserAgentSettingCard, "user-agent", "ua", "browser", "transmission", "浏览器"),
        new(ConnectTimeoutSettingCard, "connect", "timeout", "seconds", "连接", "超时"),
        new(TimeoutSettingCard, "timeout", "seconds", "transfer", "传输", "超时"),
        new(FileAllocationSettingCard, "file", "allocation", "disk", "prealloc", "文件", "预分配")
    ];

    internal StackPanel NetworkSettingsContentControl => NetworkSettingsContent;
    internal ToggleSwitch UseSystemProxyCheckBoxControl => DownloadProxyToggleSwitch;
    internal TextBlock UseSystemProxyStateTextControl => DownloadProxyStateText;
    internal ToggleSwitch CustomProxyToggleSwitchControl => DownloadProxyToggleSwitch;
    internal TextBlock CustomProxyStateTextControl => DownloadProxyStateText;
    internal ToggleSwitch DownloadProxyToggleSwitchControl => DownloadProxyToggleSwitch;
    internal TextBlock DownloadProxyStateTextControl => DownloadProxyStateText;
    internal TextBox ProxyServerTextBoxControl => ProxyServerTextBox;
    internal TextBox ProxyUsernameTextBoxControl => ProxyUsernameTextBox;
    internal PasswordBox ProxyPasswordBoxControl => ProxyPasswordBox;
    internal Button DetectSystemProxyButtonControl => DetectSystemProxyButton;
    internal TextBox ProxyBypassTextBoxControl => ProxyBypassTextBox;
    internal Button ProxyScopeDropDownButtonControl => ProxyScopeDropDownButton;
    internal CheckBox ProxyDownloadsCheckBoxControl => ProxyDownloadsCheckBox;
    internal CheckBox ProxyTrackersCheckBoxControl => ProxyTrackersCheckBox;
    internal ToggleSwitch EnableUpnpToggleSwitchControl => EnableUpnpToggleSwitch;
    internal TextBlock EnableUpnpStateTextControl => EnableUpnpStateText;
    internal NumberBox BtListenPortNumberBoxControl => BtListenPortNumberBox;
    internal NumberBox DhtListenPortNumberBoxControl => DhtListenPortNumberBox;
    internal ComboBox UserAgentComboBoxControl => UserAgentComboBox;
    internal SettingsCard UserAgentCustomSettingCardControl => UserAgentCustomSettingCard;
    internal TextBox UserAgentTextBoxControl => UserAgentTextBox;
    internal NumberBox ConnectTimeoutNumberBoxControl => ConnectTimeoutNumberBox;
    internal NumberBox TimeoutNumberBoxControl => TimeoutNumberBox;
    internal ComboBox FileAllocationComboBoxControl => FileAllocationComboBox;

    internal event RoutedEventHandler? NetworkSettingChanged;
    internal event RoutedEventHandler? DetectSystemProxyRequested;
    internal event RoutedEventHandler? RandomBtPortRequested;
    internal event RoutedEventHandler? RandomDhtPortRequested;
    internal event RoutedEventHandler? UserAgentPresetRequested;

    private void NetworkSettingToggleSwitch_Toggled(object sender, RoutedEventArgs args)
    {
        UpdateToggleStateText(sender as ToggleSwitch);
        NetworkSettingChanged?.Invoke(sender, args);
    }

    private void NetworkSettingTextBox_TextChanged(object sender, TextChangedEventArgs args)
    {
        NetworkSettingChanged?.Invoke(sender, new RoutedEventArgs());
    }

    private void NetworkSettingPasswordBox_PasswordChanged(object sender, RoutedEventArgs args)
    {
        NetworkSettingChanged?.Invoke(sender, args);
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
        UpdateProxyScopeSummary();
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

    private void UserAgentComboBox_SelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        UserAgentPresetRequested?.Invoke(sender, new RoutedEventArgs());
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
        if (ReferenceEquals(toggleSwitch, DownloadProxyToggleSwitch)) return DownloadProxyStateText;
        if (ReferenceEquals(toggleSwitch, EnableUpnpToggleSwitch)) return EnableUpnpStateText;
        return null;
    }

    internal void UpdateProxyScopeSummary()
    {
        bool downloadsSelected = ProxyDownloadsCheckBox?.IsChecked == true;
        bool trackersSelected = ProxyTrackersCheckBox?.IsChecked == true;

        ProxyDownloadsToken.Visibility = downloadsSelected ? Visibility.Visible : Visibility.Collapsed;
        ProxyTrackersToken.Visibility = trackersSelected ? Visibility.Visible : Visibility.Collapsed;
        ProxyScopePlaceholderText.Visibility = downloadsSelected || trackersSelected
            ? Visibility.Collapsed
            : Visibility.Visible;
    }
}
