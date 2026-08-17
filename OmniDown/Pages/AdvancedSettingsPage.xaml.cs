using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OmniDown.Controls;
using OmniDown.Services.Settings;
using System.Collections.Generic;

namespace OmniDown.Pages;

public sealed partial class AdvancedSettingsPage : Page
{
    private AdvancedSettingsSectionControl? _content;

    internal ISettingsHostActions? HostActions { get; set; }

    internal AdvancedSettingsSectionControl? SectionContentControl => _content;

    internal void SetSectionContent(AdvancedSettingsSectionControl content)
    {
        _content = content;
        Content = SettingsPageLayout.CreateSectionScrollViewer(content);
    }

    internal IEnumerable<SettingSearchEntry> SearchEntries =>
        _content?.SearchEntries ?? [];

    internal event RoutedEventHandler? AdvancedSettingChanged;
    internal event RoutedEventHandler? BrowseAriaPathRequested;
    internal event RoutedEventHandler? CopyRpcSecretRequested;
    internal event RoutedEventHandler? GenerateRpcSecretRequested;
    internal event RoutedEventHandler? CopyExtensionApiSecretRequested;
    internal event RoutedEventHandler? GenerateExtensionApiSecretRequested;
    internal event RoutedEventHandler? OpenConfigFolderRequested;
    internal event RoutedEventHandler? OpenLogFolderRequested;
    internal event RoutedEventHandler? ClearSessionRequested;
    internal event RoutedEventHandler? StartStopAriaRequested;
    internal event RoutedEventHandler? RestartAriaRequested;
    internal event RoutedEventHandler? ManualUpdateRequested;

    public AdvancedSettingsPage()
    {
        InitializeComponent();
        SetSectionContent(new AdvancedSettingsSectionControl());
        WireEvents();
    }

    internal void WireEvents()
    {
        if (_content is null) return;
        _content.AdvancedSettingChanged += (s, e) => AdvancedSettingChanged?.Invoke(this, e);
        _content.BrowseAriaPathRequested += (s, e) => BrowseAriaPathRequested?.Invoke(this, e);
        _content.CopyRpcSecretRequested += (s, e) => CopyRpcSecretRequested?.Invoke(this, e);
        _content.GenerateRpcSecretRequested += (s, e) => GenerateRpcSecretRequested?.Invoke(this, e);
        _content.CopyExtensionApiSecretRequested += (s, e) => CopyExtensionApiSecretRequested?.Invoke(this, e);
        _content.GenerateExtensionApiSecretRequested += (s, e) => GenerateExtensionApiSecretRequested?.Invoke(this, e);
        _content.OpenConfigFolderRequested += (s, e) => OpenConfigFolderRequested?.Invoke(this, e);
        _content.OpenLogFolderRequested += (s, e) => OpenLogFolderRequested?.Invoke(this, e);
        _content.ClearSessionRequested += (s, e) => ClearSessionRequested?.Invoke(this, e);
        _content.StartStopAriaRequested += (s, e) => StartStopAriaRequested?.Invoke(this, e);
        _content.RestartAriaRequested += (s, e) => RestartAriaRequested?.Invoke(this, e);
        _content.ManualUpdateRequested += (s, e) => ManualUpdateRequested?.Invoke(this, e);
    }
}
