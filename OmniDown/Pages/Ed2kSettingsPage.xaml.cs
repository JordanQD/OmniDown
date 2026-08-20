using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OmniDown.Controls;
using OmniDown.Services.Settings;
using System;
using System.Collections.Generic;

namespace OmniDown.Pages;

public sealed partial class Ed2kSettingsPage : Page
{
    private Ed2kSettingsSectionControl? _content;

    internal ISettingsHostActions? HostActions { get; set; }

    internal Ed2kSettingsSectionControl? SectionContentControl => _content;

    internal void SetSectionContent(Ed2kSettingsSectionControl content)
    {
        _content = content;
        Content = SettingsPageLayout.CreateSectionScrollViewer(content);
    }

    internal IEnumerable<SettingSearchEntry> SearchEntries =>
        _content?.SearchEntries ?? [];

    internal event RoutedEventHandler? Ed2kSettingChanged;
    internal event RoutedEventHandler? RandomEd2kPortRequested;
    internal event RoutedEventHandler? RandomEd2kUdpPortRequested;
    internal event RoutedEventHandler? SyncEd2kRequested;
    internal event RoutedEventHandler? SearchEd2kRequested;
    internal event EventHandler<Ed2kSearchDownloadRequestedEventArgs>? DownloadEd2kSearchResultRequested;

    public Ed2kSettingsPage()
    {
        InitializeComponent();
        SetSectionContent(new Ed2kSettingsSectionControl());
        WireEvents();
    }

    internal void WireEvents()
    {
        if (_content is null) return;
        _content.Ed2kSettingChanged += (s, e) => Ed2kSettingChanged?.Invoke(this, e);
        _content.RandomEd2kPortRequested += (s, e) => RandomEd2kPortRequested?.Invoke(this, e);
        _content.RandomEd2kUdpPortRequested += (s, e) => RandomEd2kUdpPortRequested?.Invoke(this, e);
        _content.SyncEd2kRequested += (s, e) => SyncEd2kRequested?.Invoke(this, e);
        _content.SearchEd2kRequested += (s, e) => SearchEd2kRequested?.Invoke(this, e);
        _content.DownloadEd2kSearchResultRequested += (s, e) => DownloadEd2kSearchResultRequested?.Invoke(this, e);
    }
}
