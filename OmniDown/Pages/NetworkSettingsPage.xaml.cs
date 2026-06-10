using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OmniDown.Controls;
using OmniDown.Services.Settings;
using System.Collections.Generic;

namespace OmniDown.Pages;

public sealed partial class NetworkSettingsPage : Page
{
    private NetworkSettingsSectionControl? _content;

    internal ISettingsHostActions? HostActions { get; set; }

    internal NetworkSettingsSectionControl? SectionContentControl => _content;

    internal void SetSectionContent(NetworkSettingsSectionControl content)
    {
        _content = content;
        Content = SettingsPageLayout.CreateSectionScrollViewer(content);
    }

    internal IEnumerable<SettingSearchEntry> SearchEntries =>
        _content?.SearchEntries ?? [];

    internal event RoutedEventHandler? NetworkSettingChanged;
    internal event RoutedEventHandler? DetectSystemProxyRequested;
    internal event RoutedEventHandler? RandomBtPortRequested;
    internal event RoutedEventHandler? RandomDhtPortRequested;
    internal event RoutedEventHandler? UserAgentPresetRequested;

    public NetworkSettingsPage()
    {
        InitializeComponent();
        SetSectionContent(new NetworkSettingsSectionControl());
        WireEvents();
    }

    internal void WireEvents()
    {
        if (_content is null) return;
        _content.NetworkSettingChanged += (s, e) => NetworkSettingChanged?.Invoke(this, e);
        _content.DetectSystemProxyRequested += (s, e) => DetectSystemProxyRequested?.Invoke(this, e);
        _content.RandomBtPortRequested += (s, e) => RandomBtPortRequested?.Invoke(this, e);
        _content.RandomDhtPortRequested += (s, e) => RandomDhtPortRequested?.Invoke(this, e);
        _content.UserAgentPresetRequested += (s, e) => UserAgentPresetRequested?.Invoke(this, e);
    }
}
