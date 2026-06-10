using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OmniDown.Controls;
using OmniDown.Services.Settings;
using System.Collections.Generic;

namespace OmniDown.Pages;

public sealed partial class DownloadSettingsPage : Page
{
    private DownloadSettingsSectionControl? _content;

    internal ISettingsHostActions? HostActions { get; set; }

    internal DownloadSettingsSectionControl? SectionContentControl => _content;

    internal void SetSectionContent(DownloadSettingsSectionControl content)
    {
        _content = content;
        Content = SettingsPageLayout.CreateSectionScrollViewer(content);
    }

    internal IEnumerable<SettingSearchEntry> SearchEntries =>
        _content?.SearchEntries ?? [];

    internal event RoutedEventHandler? BrowseDownloadDirectoryRequested;
    internal event RoutedEventHandler? DownloadSettingChanged;

    public DownloadSettingsPage()
    {
        InitializeComponent();
        SetSectionContent(new DownloadSettingsSectionControl());
        WireEvents();
    }

    internal void WireEvents()
    {
        if (_content is null) return;
        _content.BrowseDownloadDirectoryRequested += (s, e) => BrowseDownloadDirectoryRequested?.Invoke(this, e);
        _content.DownloadSettingChanged += (s, e) => DownloadSettingChanged?.Invoke(this, e);
    }
}
