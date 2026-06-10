using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OmniDown.Controls;
using OmniDown.Models.Settings;
using OmniDown.Services.Settings;
using System;
using System.Collections.Generic;

namespace OmniDown.Pages;

public sealed partial class GeneralSettingsPage : Page
{
    private GeneralSettingsSectionControl? _content;

    internal ISettingsHostActions? HostActions { get; set; }

    internal GeneralSettingsSectionControl? SectionContentControl => _content;

    internal void SetSectionContent(GeneralSettingsSectionControl content)
    {
        _content = content;
        Content = SettingsPageLayout.CreateSectionScrollViewer(content);
    }

    internal IEnumerable<SettingSearchEntry> SearchEntries =>
        _content?.SearchEntries ?? [];

    internal event EventHandler<GeneralSettingChangedEventArgs>? GeneralSettingChanged;
    internal event EventHandler<CloseBehaviorSettingChangedEventArgs>? CloseBehaviorSettingChanged;

    internal void ApplyGeneralSettings(GeneralSettings settings, bool isAutoStartEnabled) =>
        _content?.ApplyGeneralSettings(settings, isAutoStartEnabled);

    internal GeneralSettings GetGeneralSettings(GeneralSettings currentSettings) =>
        _content?.GetGeneralSettings(currentSettings) ?? currentSettings;

    internal void ApplyCloseBehaviorSettings(CloseBehaviorSettings settings) =>
        _content?.ApplyCloseBehaviorSettings(settings);

    internal void SetAutoStartEnabled(bool isEnabled) =>
        _content?.SetAutoStartEnabled(isEnabled);

    internal bool IsAutoStartEnabled => _content?.IsAutoStartEnabled == true;

    public GeneralSettingsPage()
    {
        InitializeComponent();
        SetSectionContent(new GeneralSettingsSectionControl());
        WireEvents();
    }

    // Forward events from the control
    internal void WireEvents()
    {
        if (_content is null) return;
        _content.GeneralSettingChanged += (s, e) => GeneralSettingChanged?.Invoke(this, e);
        _content.CloseBehaviorSettingChanged += (s, e) => CloseBehaviorSettingChanged?.Invoke(this, e);
    }
}
