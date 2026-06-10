using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OmniDown.Controls;
using OmniDown.Services.Settings;
using System.Collections.Generic;

namespace OmniDown.Pages;

public sealed partial class AboutSettingsPage : Page
{
    private AboutSettingsSectionControl? _content;

    internal ISettingsHostActions? HostActions { get; set; }

    internal AboutSettingsSectionControl? SectionContentControl => _content;

    internal void SetSectionContent(AboutSettingsSectionControl content)
    {
        _content = content;
        Content = SettingsPageLayout.CreateSectionScrollViewer(content);
    }

    internal IEnumerable<SettingSearchEntry> SearchEntries =>
        _content?.SearchEntries ?? [];

    internal TextBlock? AboutVersionTextControl => _content?.AboutVersionTextControl;
    internal TextBlock? AboutCloneCommandTextControl => _content?.AboutCloneCommandTextControl;

    internal event RoutedEventHandler? CopyCloneCommandRequested;
    internal event RoutedEventHandler? OpenAboutLinkRequested;

    public AboutSettingsPage()
    {
        InitializeComponent();
        SetSectionContent(new AboutSettingsSectionControl());
        WireEvents();
    }

    internal void WireEvents()
    {
        if (_content is null) return;
        _content.CopyCloneCommandRequested += (s, e) => CopyCloneCommandRequested?.Invoke(this, e);
        _content.OpenAboutLinkRequested += (s, e) => OpenAboutLinkRequested?.Invoke(this, e);
    }
}
