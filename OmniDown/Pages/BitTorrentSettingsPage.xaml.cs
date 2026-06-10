using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OmniDown.Controls;
using OmniDown.Services.Settings;
using System.Collections.Generic;

namespace OmniDown.Pages;

public sealed partial class BitTorrentSettingsPage : Page
{
    private BitTorrentSettingsSectionControl? _content;

    internal ISettingsHostActions? HostActions { get; set; }

    internal BitTorrentSettingsSectionControl? SectionContentControl => _content;

    internal void SetSectionContent(BitTorrentSettingsSectionControl content)
    {
        _content = content;
        Content = SettingsPageLayout.CreateSectionScrollViewer(content);
    }

    internal IEnumerable<SettingSearchEntry> SearchEntries =>
        _content?.SearchEntries ?? [];

    internal event RoutedEventHandler? BitTorrentSettingChanged;
    internal event RoutedEventHandler? AddBtCustomTrackerRequested;
    internal event RoutedEventHandler? SyncBtTrackerRequested;

    public BitTorrentSettingsPage()
    {
        InitializeComponent();
        SetSectionContent(new BitTorrentSettingsSectionControl());
        WireEvents();
    }

    internal void WireEvents()
    {
        if (_content is null) return;
        _content.BitTorrentSettingChanged += (s, e) => BitTorrentSettingChanged?.Invoke(this, e);
        _content.AddBtCustomTrackerRequested += (s, e) => AddBtCustomTrackerRequested?.Invoke(this, e);
        _content.SyncBtTrackerRequested += (s, e) => SyncBtTrackerRequested?.Invoke(this, e);
    }
}
