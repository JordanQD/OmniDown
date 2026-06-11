using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using OmniDown.Controls;
using OmniDown.Services.Settings;
using OmniDown.ViewModels;
using System;
using System.Collections.Generic;

namespace OmniDown.Pages;

public sealed partial class AppSettingsPage : Page
{
    private string _currentSection = string.Empty;
    private ISettingsHostActions? _hostActions;
    private Page? _displayedPage;

    // ── Settings pages (created once; each page owns its own section control) ──

    private readonly SettingsHomePage _homePage = new();
    private readonly GeneralSettingsPage _generalPage = new();
    private readonly DownloadSettingsPage _downloadPage = new();
    private readonly BitTorrentSettingsPage _bitTorrentPage = new();
    private readonly NetworkSettingsPage _networkPage = new();
    private readonly AdvancedSettingsPage _advancedPage = new();
    private readonly AboutSettingsPage _aboutPage = new();

    private readonly Dictionary<string, Page> _sectionPages;

    public event EventHandler? NavigationStateChanged;

    public AppSettingsPage()
    {
        InitializeComponent();
        SettingsFrame.Navigated += SettingsFrame_Navigated;
        _sectionPages = new Dictionary<string, Page>
        {
            ["General"] = _generalPage,
            ["Download"] = _downloadPage,
            ["BitTorrent"] = _bitTorrentPage,
            ["Network"] = _networkPage,
            ["Advanced"] = _advancedPage,
            ["About"] = _aboutPage,
        };
        _homePage.SectionRequested += OnSectionRequested;
        NavigateToHome(new EntranceNavigationTransitionInfo());
    }

    internal void InitializeNavigation(SettingsPageViewModel viewModel, ISettingsHostActions hostActions)
    {
        _hostActions = hostActions;
        _generalPage.HostActions = hostActions;
        _downloadPage.HostActions = hostActions;
        _bitTorrentPage.HostActions = hostActions;
        _networkPage.HostActions = hostActions;
        _advancedPage.HostActions = hostActions;
        _aboutPage.HostActions = hostActions;
    }

    // ── Navigation ──

    internal void NavigateTo(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag) || tag == "Home")
        {
            NavigateToHome(new EntranceNavigationTransitionInfo());
            return;
        }
        NavigateToSection(tag);
    }

    private void NavigateToHome(NavigationTransitionInfo transitionInfo)
    {
        if (string.IsNullOrEmpty(_currentSection) &&
            ReferenceEquals(_displayedPage, _homePage))
        {
            ResetCurrentScrollViewerToTop();
            NavigationStateChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        _currentSection = string.Empty;
        ShowPage(_homePage, transitionInfo);

        NavigationStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnSectionRequested(object? sender, string tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return;
        if (tag == _currentSection) return;
        NavigateToSection(tag);
    }

    private void NavigateToSection(string tag)
    {
        if (!_sectionPages.TryGetValue(tag, out Page? page)) return;
        if (tag == _currentSection)
        {
            ResetCurrentScrollViewerToTop();
            NavigationStateChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        _currentSection = tag;

        ShowPage(page, new SlideNavigationTransitionInfo
        {
            Effect = SlideNavigationTransitionEffect.FromRight
        });
        NavigationStateChanged?.Invoke(this, EventArgs.Empty);
    }

    internal void GoBack()
    {
        if (string.IsNullOrWhiteSpace(_currentSection)) return;

        NavigateToHome(new SlideNavigationTransitionInfo
        {
            Effect = SlideNavigationTransitionEffect.FromLeft
        });
    }

    internal bool CanGoBack => !string.IsNullOrWhiteSpace(_currentSection);
    internal string CurrentSection => _currentSection;

    private void ShowPage(Page page, NavigationTransitionInfo transitionInfo)
    {
        _displayedPage = page;
        SettingsFrame.Navigate(typeof(SettingsNavigationHostPage), page, transitionInfo);
    }

    private void SettingsFrame_Navigated(object sender, NavigationEventArgs e)
    {
        SettingsContentScrollViewerControl = SettingsFrame.Content is DependencyObject content
            ? FindFirstDescendant<ScrollViewer>(content)
            : null;
        ResetCurrentScrollViewerToTop();
    }

    private void ResetCurrentScrollViewerToTop()
    {
        ScrollViewer? scrollViewer = SettingsContentScrollViewerControl;
        if (scrollViewer is null) return;

        scrollViewer.ChangeView(null, 0.0, null, disableAnimation: true);
        _ = scrollViewer.DispatcherQueue.TryEnqueue(() =>
        {
            scrollViewer.UpdateLayout();
            scrollViewer.ChangeView(null, 0.0, null, disableAnimation: true);
        });
    }

    // ── Section control accessors (same API as old SettingsPageControl) ──

    internal GeneralSettingsSectionControl GeneralSettingsContentControl => _generalPage.SectionContentControl!;
    internal DownloadSettingsSectionControl DownloadSettingsContentControl => _downloadPage.SectionContentControl!;
    internal BitTorrentSettingsSectionControl BitTorrentSettingsContentControl => _bitTorrentPage.SectionContentControl!;
    internal NetworkSettingsSectionControl NetworkSettingsContentControl => _networkPage.SectionContentControl!;
    internal AdvancedSettingsSectionControl AdvancedSettingsContentControl => _advancedPage.SectionContentControl!;
    internal AboutSettingsSectionControl AboutSettingsContentControl => _aboutPage.SectionContentControl!;

    // ── Section events (forwarded from the eagerly-created controls) ──

    internal event EventHandler<GeneralSettingChangedEventArgs>? GeneralSettingChanged
    {
        add => _generalPage.GeneralSettingChanged += value;
        remove => _generalPage.GeneralSettingChanged -= value;
    }
    internal event EventHandler<CloseBehaviorSettingChangedEventArgs>? CloseBehaviorSettingChanged
    {
        add => _generalPage.CloseBehaviorSettingChanged += value;
        remove => _generalPage.CloseBehaviorSettingChanged -= value;
    }
    internal event RoutedEventHandler? BrowseDownloadDirectoryRequested
    {
        add => _downloadPage.BrowseDownloadDirectoryRequested += value;
        remove => _downloadPage.BrowseDownloadDirectoryRequested -= value;
    }
    internal event RoutedEventHandler? DownloadSettingChanged
    {
        add => _downloadPage.DownloadSettingChanged += value;
        remove => _downloadPage.DownloadSettingChanged -= value;
    }
    internal event RoutedEventHandler? BitTorrentSettingChanged
    {
        add => _bitTorrentPage.BitTorrentSettingChanged += value;
        remove => _bitTorrentPage.BitTorrentSettingChanged -= value;
    }
    internal event RoutedEventHandler? AddBtCustomTrackerRequested
    {
        add => _bitTorrentPage.AddBtCustomTrackerRequested += value;
        remove => _bitTorrentPage.AddBtCustomTrackerRequested -= value;
    }
    internal event RoutedEventHandler? SyncBtTrackerRequested
    {
        add => _bitTorrentPage.SyncBtTrackerRequested += value;
        remove => _bitTorrentPage.SyncBtTrackerRequested -= value;
    }
    internal event RoutedEventHandler? NetworkSettingChanged
    {
        add => _networkPage.NetworkSettingChanged += value;
        remove => _networkPage.NetworkSettingChanged -= value;
    }
    internal event RoutedEventHandler? DetectSystemProxyRequested
    {
        add => _networkPage.DetectSystemProxyRequested += value;
        remove => _networkPage.DetectSystemProxyRequested -= value;
    }
    internal event RoutedEventHandler? RandomBtPortRequested
    {
        add => _networkPage.RandomBtPortRequested += value;
        remove => _networkPage.RandomBtPortRequested -= value;
    }
    internal event RoutedEventHandler? RandomDhtPortRequested
    {
        add => _networkPage.RandomDhtPortRequested += value;
        remove => _networkPage.RandomDhtPortRequested -= value;
    }
    internal event RoutedEventHandler? UserAgentPresetRequested
    {
        add => _networkPage.UserAgentPresetRequested += value;
        remove => _networkPage.UserAgentPresetRequested -= value;
    }
    internal event RoutedEventHandler? AdvancedSettingChanged
    {
        add => _advancedPage.AdvancedSettingChanged += value;
        remove => _advancedPage.AdvancedSettingChanged -= value;
    }
    internal event RoutedEventHandler? BrowseAriaPathRequested
    {
        add => _advancedPage.BrowseAriaPathRequested += value;
        remove => _advancedPage.BrowseAriaPathRequested -= value;
    }
    internal event RoutedEventHandler? CopyRpcSecretRequested
    {
        add => _advancedPage.CopyRpcSecretRequested += value;
        remove => _advancedPage.CopyRpcSecretRequested -= value;
    }
    internal event RoutedEventHandler? GenerateRpcSecretRequested
    {
        add => _advancedPage.GenerateRpcSecretRequested += value;
        remove => _advancedPage.GenerateRpcSecretRequested -= value;
    }
    internal event RoutedEventHandler? CopyExtensionApiSecretRequested
    {
        add => _advancedPage.CopyExtensionApiSecretRequested += value;
        remove => _advancedPage.CopyExtensionApiSecretRequested -= value;
    }
    internal event RoutedEventHandler? GenerateExtensionApiSecretRequested
    {
        add => _advancedPage.GenerateExtensionApiSecretRequested += value;
        remove => _advancedPage.GenerateExtensionApiSecretRequested -= value;
    }
    internal event RoutedEventHandler? OpenConfigFolderRequested
    {
        add => _advancedPage.OpenConfigFolderRequested += value;
        remove => _advancedPage.OpenConfigFolderRequested -= value;
    }
    internal event RoutedEventHandler? OpenLogFolderRequested
    {
        add => _advancedPage.OpenLogFolderRequested += value;
        remove => _advancedPage.OpenLogFolderRequested -= value;
    }
    internal event RoutedEventHandler? ClearSessionRequested
    {
        add => _advancedPage.ClearSessionRequested += value;
        remove => _advancedPage.ClearSessionRequested -= value;
    }
    internal event RoutedEventHandler? StartStopAriaRequested
    {
        add => _advancedPage.StartStopAriaRequested += value;
        remove => _advancedPage.StartStopAriaRequested -= value;
    }
    internal event RoutedEventHandler? RestartAriaRequested
    {
        add => _advancedPage.RestartAriaRequested += value;
        remove => _advancedPage.RestartAriaRequested -= value;
    }
    internal event RoutedEventHandler? ManualUpdateRequested
    {
        add => _advancedPage.ManualUpdateRequested += value;
        remove => _advancedPage.ManualUpdateRequested -= value;
    }

    // About events (forwarded from _aboutContent)
    internal event RoutedEventHandler? CopyCloneCommandRequested
    {
        add => _aboutPage.CopyCloneCommandRequested += value;
        remove => _aboutPage.CopyCloneCommandRequested -= value;
    }
    internal event RoutedEventHandler? OpenAboutLinkRequested
    {
        add => _aboutPage.OpenAboutLinkRequested += value;
        remove => _aboutPage.OpenAboutLinkRequested -= value;
    }

    // ── Download section individual controls (needed by MainWindow) ──

    internal TextBox DownloadDirectoryTextBoxControl => DownloadSettingsContentControl.DownloadDirectoryTextBoxControl;
    internal NumberBox MaxConcurrentDownloadsNumberBoxControl => DownloadSettingsContentControl.MaxConcurrentDownloadsNumberBoxControl;
    internal NumberBox SplitCountNumberBoxControl => DownloadSettingsContentControl.SplitCountNumberBoxControl;
    internal NumberBox MaxConnectionPerServerNumberBoxControl => DownloadSettingsContentControl.MaxConnectionPerServerNumberBoxControl;
    internal ToggleSwitch ContinueDownloadToggleSwitchControl => DownloadSettingsContentControl.ContinueDownloadToggleSwitchControl;
    internal TextBlock ContinueDownloadStateTextControl => DownloadSettingsContentControl.ContinueDownloadStateTextControl;
    internal ComboBox RemoteTimeComboBoxControl => DownloadSettingsContentControl.RemoteTimeComboBoxControl;
    internal NumberBox MaxTriesNumberBoxControl => DownloadSettingsContentControl.MaxTriesNumberBoxControl;
    internal NumberBox RetryWaitNumberBoxControl => DownloadSettingsContentControl.RetryWaitNumberBoxControl;
    internal ToggleSwitch AutoDeleteStaleRecordsToggleSwitchControl => DownloadSettingsContentControl.AutoDeleteStaleRecordsToggleSwitchControl;
    internal TextBlock AutoDeleteStaleRecordsStateTextControl => DownloadSettingsContentControl.AutoDeleteStaleRecordsStateTextControl;
    internal ToggleSwitch DeleteTorrentAfterCompleteToggleSwitchControl => DownloadSettingsContentControl.DeleteTorrentAfterCompleteToggleSwitchControl;
    internal TextBlock DeleteTorrentAfterCompleteStateTextControl => DownloadSettingsContentControl.DeleteTorrentAfterCompleteStateTextControl;

    // ── BitTorrent individual controls ──

    internal ToggleSwitch BtAutoDownloadToggleSwitchControl => BitTorrentSettingsContentControl.BtAutoDownloadToggleSwitchControl;
    internal TextBlock BtAutoDownloadStateTextControl => BitTorrentSettingsContentControl.BtAutoDownloadStateTextControl;
    internal ToggleSwitch BtForceEncryptionToggleSwitchControl => BitTorrentSettingsContentControl.BtForceEncryptionToggleSwitchControl;
    internal TextBlock BtForceEncryptionStateTextControl => BitTorrentSettingsContentControl.BtForceEncryptionStateTextControl;
    internal ComboBox BtSeedingModeComboBoxControl => BitTorrentSettingsContentControl.BtSeedingModeComboBoxControl;
    internal NumberBox BtSeedRatioNumberBoxControl => BitTorrentSettingsContentControl.BtSeedRatioNumberBoxControl;
    internal NumberBox BtSeedTimeNumberBoxControl => BitTorrentSettingsContentControl.BtSeedTimeNumberBoxControl;
    internal NumberBox BtMaxPeersNumberBoxControl => BitTorrentSettingsContentControl.BtMaxPeersNumberBoxControl;
    internal Button BtTrackerSourceDropDownButtonControl => BitTorrentSettingsContentControl.BtTrackerSourceDropDownButtonControl;
    internal CheckBox BtTrackerNgosangBestCheckBoxControl => BitTorrentSettingsContentControl.BtTrackerNgosangBestCheckBoxControl;
    internal CheckBox BtTrackerNgosangBestIpCheckBoxControl => BitTorrentSettingsContentControl.BtTrackerNgosangBestIpCheckBoxControl;
    internal CheckBox BtTrackerNgosangAllCheckBoxControl => BitTorrentSettingsContentControl.BtTrackerNgosangAllCheckBoxControl;
    internal CheckBox BtTrackerNgosangAllIpCheckBoxControl => BitTorrentSettingsContentControl.BtTrackerNgosangAllIpCheckBoxControl;
    internal CheckBox BtTrackerNgosangCdnBestCheckBoxControl => BitTorrentSettingsContentControl.BtTrackerNgosangCdnBestCheckBoxControl;
    internal CheckBox BtTrackerNgosangCdnBestIpCheckBoxControl => BitTorrentSettingsContentControl.BtTrackerNgosangCdnBestIpCheckBoxControl;
    internal CheckBox BtTrackerNgosangCdnAllCheckBoxControl => BitTorrentSettingsContentControl.BtTrackerNgosangCdnAllCheckBoxControl;
    internal CheckBox BtTrackerNgosangCdnAllIpCheckBoxControl => BitTorrentSettingsContentControl.BtTrackerNgosangCdnAllIpCheckBoxControl;
    internal CheckBox BtTrackerXiu2BestCheckBoxControl => BitTorrentSettingsContentControl.BtTrackerXiu2BestCheckBoxControl;
    internal CheckBox BtTrackerXiu2AllCheckBoxControl => BitTorrentSettingsContentControl.BtTrackerXiu2AllCheckBoxControl;
    internal CheckBox BtTrackerXiu2HttpCheckBoxControl => BitTorrentSettingsContentControl.BtTrackerXiu2HttpCheckBoxControl;
    internal CheckBox BtTrackerXiu2CdnBestCheckBoxControl => BitTorrentSettingsContentControl.BtTrackerXiu2CdnBestCheckBoxControl;
    internal CheckBox BtTrackerXiu2CdnAllCheckBoxControl => BitTorrentSettingsContentControl.BtTrackerXiu2CdnAllCheckBoxControl;
    internal CheckBox BtTrackerXiu2CdnHttpCheckBoxControl => BitTorrentSettingsContentControl.BtTrackerXiu2CdnHttpCheckBoxControl;
    internal TextBox BtCustomTrackerSourceTextBoxControl => BitTorrentSettingsContentControl.BtCustomTrackerSourceTextBoxControl;
    internal ListView BtCustomTrackerSourceListViewControl => BitTorrentSettingsContentControl.BtCustomTrackerSourceListViewControl;
    internal TextBox BtTrackerSourceTextBoxControl => BitTorrentSettingsContentControl.BtTrackerSourceTextBoxControl;
    internal Button BtSyncTrackerButtonControl => BitTorrentSettingsContentControl.BtSyncTrackerButtonControl;
    internal TextBox BtTrackerListTextBoxControl => BitTorrentSettingsContentControl.BtTrackerListTextBoxControl;
    internal ToggleSwitch BtAutoSyncTrackerToggleSwitchControl => BitTorrentSettingsContentControl.BtAutoSyncTrackerToggleSwitchControl;
    internal TextBlock BtAutoSyncTrackerStateTextControl => BitTorrentSettingsContentControl.BtAutoSyncTrackerStateTextControl;
    internal TextBlock BtLastTrackerSyncTextControl => BitTorrentSettingsContentControl.BtLastTrackerSyncTextControl;

    // ── Network individual controls ──

    internal ToggleSwitch UseSystemProxyCheckBoxControl => NetworkSettingsContentControl.UseSystemProxyCheckBoxControl;
    internal TextBlock UseSystemProxyStateTextControl => NetworkSettingsContentControl.UseSystemProxyStateTextControl;
    internal ToggleSwitch CustomProxyToggleSwitchControl => NetworkSettingsContentControl.CustomProxyToggleSwitchControl;
    internal TextBlock CustomProxyStateTextControl => NetworkSettingsContentControl.CustomProxyStateTextControl;
    internal TextBox ProxyServerTextBoxControl => NetworkSettingsContentControl.ProxyServerTextBoxControl;
    internal TextBox ProxyUsernameTextBoxControl => NetworkSettingsContentControl.ProxyUsernameTextBoxControl;
    internal PasswordBox ProxyPasswordBoxControl => NetworkSettingsContentControl.ProxyPasswordBoxControl;
    internal Button DetectSystemProxyButtonControl => NetworkSettingsContentControl.DetectSystemProxyButtonControl;
    internal TextBox ProxyBypassTextBoxControl => NetworkSettingsContentControl.ProxyBypassTextBoxControl;
    internal Button ProxyScopeDropDownButtonControl => NetworkSettingsContentControl.ProxyScopeDropDownButtonControl;
    internal CheckBox ProxyDownloadsCheckBoxControl => NetworkSettingsContentControl.ProxyDownloadsCheckBoxControl;
    internal CheckBox ProxyTrackersCheckBoxControl => NetworkSettingsContentControl.ProxyTrackersCheckBoxControl;
    internal ToggleSwitch EnableUpnpToggleSwitchControl => NetworkSettingsContentControl.EnableUpnpToggleSwitchControl;
    internal TextBlock EnableUpnpStateTextControl => NetworkSettingsContentControl.EnableUpnpStateTextControl;
    internal NumberBox BtListenPortNumberBoxControl => NetworkSettingsContentControl.BtListenPortNumberBoxControl;
    internal NumberBox DhtListenPortNumberBoxControl => NetworkSettingsContentControl.DhtListenPortNumberBoxControl;
    internal ComboBox UserAgentComboBoxControl => NetworkSettingsContentControl.UserAgentComboBoxControl;
    internal CommunityToolkit.WinUI.Controls.SettingsCard UserAgentCustomSettingCardControl => NetworkSettingsContentControl.UserAgentCustomSettingCardControl;
    internal TextBox UserAgentTextBoxControl => NetworkSettingsContentControl.UserAgentTextBoxControl;
    internal NumberBox ConnectTimeoutNumberBoxControl => NetworkSettingsContentControl.ConnectTimeoutNumberBoxControl;
    internal NumberBox TimeoutNumberBoxControl => NetworkSettingsContentControl.TimeoutNumberBoxControl;
    internal ComboBox FileAllocationComboBoxControl => NetworkSettingsContentControl.FileAllocationComboBoxControl;

    // ── Advanced individual controls ──

    internal ComboBox EngineTypeComboBoxControl => AdvancedSettingsContentControl.EngineTypeComboBoxControl;
    internal TextBox AriaPathTextBoxControl => AdvancedSettingsContentControl.AriaPathTextBoxControl;
    internal TextBlock EngineVersionTextControl => AdvancedSettingsContentControl.EngineVersionTextControl;
    internal ToggleSwitch EngineAutoUpdateToggleControl => AdvancedSettingsContentControl.EngineAutoUpdateToggleControl;
    internal NumberBox RpcPortNumberBoxControl => AdvancedSettingsContentControl.RpcPortNumberBoxControl;
    internal PasswordBox RpcSecretPasswordBoxControl => AdvancedSettingsContentControl.RpcSecretPasswordBoxControl;
    internal ToggleSwitch ExtensionAutoSubmitToggleSwitchControl => AdvancedSettingsContentControl.ExtensionAutoSubmitToggleSwitchControl;
    internal TextBlock ExtensionAutoSubmitStateTextControl => AdvancedSettingsContentControl.ExtensionAutoSubmitStateTextControl;
    internal NumberBox ExtensionApiPortNumberBoxControl => AdvancedSettingsContentControl.ExtensionApiPortNumberBoxControl;
    internal PasswordBox ExtensionApiSecretPasswordBoxControl => AdvancedSettingsContentControl.ExtensionApiSecretPasswordBoxControl;
    internal ComboBox LogLevelComboBoxControl => AdvancedSettingsContentControl.LogLevelComboBoxControl;
    internal TextBlock AdvancedPathsSummaryTextControl => AdvancedSettingsContentControl.AdvancedPathsSummaryTextControl;
    internal TextBlock LogPathsSummaryTextControl => AdvancedSettingsContentControl.LogPathsSummaryTextControl;
    internal ToggleSwitch ClipboardDetectionToggleSwitchControl => AdvancedSettingsContentControl.ClipboardDetectionToggleSwitchControl;
    internal TextBlock ClipboardDetectionStateTextControl => AdvancedSettingsContentControl.ClipboardDetectionStateTextControl;
    internal ToggleSwitch ClipboardHttpToggleSwitchControl => AdvancedSettingsContentControl.ClipboardHttpToggleSwitchControl;
    internal ToggleSwitch ClipboardFtpToggleSwitchControl => AdvancedSettingsContentControl.ClipboardFtpToggleSwitchControl;
    internal ToggleSwitch ClipboardMagnetToggleSwitchControl => AdvancedSettingsContentControl.ClipboardMagnetToggleSwitchControl;
    internal ToggleSwitch ClipboardThunderToggleSwitchControl => AdvancedSettingsContentControl.ClipboardThunderToggleSwitchControl;
    internal ToggleSwitch ClipboardBtHashToggleSwitchControl => AdvancedSettingsContentControl.ClipboardBtHashToggleSwitchControl;
    internal ToggleSwitch ProtocolMagnetToggleSwitchControl => AdvancedSettingsContentControl.ProtocolMagnetToggleSwitchControl;
    internal TextBlock ProtocolMagnetStateTextControl => AdvancedSettingsContentControl.ProtocolMagnetStateTextControl;
    internal ToggleSwitch ProtocolThunderToggleSwitchControl => AdvancedSettingsContentControl.ProtocolThunderToggleSwitchControl;
    internal TextBlock ProtocolThunderStateTextControl => AdvancedSettingsContentControl.ProtocolThunderStateTextControl;
    internal ToggleSwitch ProtocolOmniDownToggleSwitchControl => AdvancedSettingsContentControl.ProtocolOmniDownToggleSwitchControl;
    internal TextBlock ProtocolOmniDownStateTextControl => AdvancedSettingsContentControl.ProtocolOmniDownStateTextControl;
    internal TextBlock SettingsAriaStatusTextControl => AdvancedSettingsContentControl.SettingsAriaStatusTextControl;
    internal StackPanel ProcessStatusSettingControlControl => AdvancedSettingsContentControl.ProcessStatusSettingControlControl;
    internal FontIcon AriaStartStopIconControl => AdvancedSettingsContentControl.AriaStartStopIconControl;
    internal Button AriaStartStopButtonControl => AdvancedSettingsContentControl.AriaStartStopButtonControl;
    internal Button AriaRestartButtonControl => AdvancedSettingsContentControl.AriaRestartButtonControl;

    // ── About controls (directly from shared _aboutContent) ──

    internal TextBlock AboutVersionTextControl => AboutSettingsContentControl.AboutVersionTextControl;
    internal TextBlock AboutCloneCommandTextControl => AboutSettingsContentControl.AboutCloneCommandTextControl;

    // ── General / CloseBehavior forwarding ──

    internal void ApplyGeneralSettings(Models.Settings.GeneralSettings settings, bool isAutoStartEnabled) =>
        GeneralSettingsContentControl.ApplyGeneralSettings(settings, isAutoStartEnabled);

    internal Models.Settings.GeneralSettings GetGeneralSettings(Models.Settings.GeneralSettings currentSettings) =>
        GeneralSettingsContentControl.GetGeneralSettings(currentSettings);

    internal void ApplyCloseBehaviorSettings(Models.Settings.CloseBehaviorSettings settings) =>
        GeneralSettingsContentControl.ApplyCloseBehaviorSettings(settings);

    internal void SetAutoStartEnabled(bool isEnabled) =>
        GeneralSettingsContentControl.SetAutoStartEnabled(isEnabled);

    internal bool IsAutoStartEnabled => GeneralSettingsContentControl.IsAutoStartEnabled;

    internal void UpdateAriaPathVisibility() =>
        AdvancedSettingsContentControl.UpdateAriaPathVisibility();

    internal ScrollViewer? SettingsContentScrollViewerControl { get; set; }

    private static T? FindFirstDescendant<T>(DependencyObject parent)
        where T : DependencyObject
    {
        int count = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match)
            {
                return match;
            }

            T? descendant = FindFirstDescendant<T>(child);
            if (descendant is not null)
            {
                return descendant;
            }
        }

        return null;
    }

    // ── Search (delegates to all section controls, same as old SettingsPageControl) ──

    internal void ApplySearchFilter(string query)
    {
        foreach (SettingSearchEntry entry in GetSearchEntries())
        {
            entry.ApplyFilter(query);
        }
    }

    private IEnumerable<SettingSearchEntry> GetSearchEntries()
    {
        foreach (SettingSearchEntry entry in GeneralSettingsContentControl.SearchEntries) yield return entry;
        foreach (SettingSearchEntry entry in DownloadSettingsContentControl.SearchEntries) yield return entry;
        foreach (SettingSearchEntry entry in BitTorrentSettingsContentControl.SearchEntries) yield return entry;
        foreach (SettingSearchEntry entry in NetworkSettingsContentControl.SearchEntries) yield return entry;
        foreach (SettingSearchEntry entry in AdvancedSettingsContentControl.SearchEntries) yield return entry;
        foreach (SettingSearchEntry entry in AboutSettingsContentControl.SearchEntries) yield return entry;
    }
}
