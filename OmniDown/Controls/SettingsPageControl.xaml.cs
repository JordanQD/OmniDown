using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using OmniDown.Models.Settings;
using OmniDown.Services.Localization;
using System;
using System.Collections.Generic;

namespace OmniDown.Controls;

public sealed partial class SettingsPageControl : UserControl
{
    public SettingsPageControl()
    {
        InitializeComponent();
        GeneralSettingsContent.GeneralSettingChanged += (_, args) => GeneralSettingChanged?.Invoke(this, args);
        GeneralSettingsContent.CloseBehaviorSettingChanged += (_, args) => CloseBehaviorSettingChanged?.Invoke(this, args);
    }

    internal ListView SettingsSectionListViewControl => SettingsSectionListView;
    internal ScrollViewer SettingsContentScrollViewerControl => SettingsContentScrollViewer;
    internal GeneralSettingsSectionControl GeneralSettingsContentControl => GeneralSettingsContent;
    internal StackPanel DownloadSettingsContentControl => DownloadSettingsContent;
    internal StackPanel BitTorrentSettingsContentControl => BitTorrentSettingsContent;
    internal StackPanel NetworkSettingsContentControl => NetworkSettingsContent;
    internal StackPanel AdvancedSettingsContentControl => AdvancedSettingsContent;
    internal StackPanel AboutSettingsContentControl => AboutSettingsContent;
    internal TextBox DownloadDirectoryTextBoxControl => DownloadDirectoryTextBox;
    internal NumberBox MaxConcurrentDownloadsNumberBoxControl => MaxConcurrentDownloadsNumberBox;
    internal NumberBox SplitCountNumberBoxControl => SplitCountNumberBox;
    internal NumberBox MaxConnectionPerServerNumberBoxControl => MaxConnectionPerServerNumberBox;
    internal ToggleSwitch ContinueDownloadToggleSwitchControl => ContinueDownloadToggleSwitch;
    internal TextBlock ContinueDownloadStateTextControl => ContinueDownloadStateText;
    internal ComboBox RemoteTimeComboBoxControl => RemoteTimeComboBox;
    internal NumberBox MaxTriesNumberBoxControl => MaxTriesNumberBox;
    internal NumberBox RetryWaitNumberBoxControl => RetryWaitNumberBox;
    internal ToggleSwitch AutoDeleteStaleRecordsToggleSwitchControl => AutoDeleteStaleRecordsToggleSwitch;
    internal TextBlock AutoDeleteStaleRecordsStateTextControl => AutoDeleteStaleRecordsStateText;
    internal ToggleSwitch DeleteTorrentAfterCompleteToggleSwitchControl => DeleteTorrentAfterCompleteToggleSwitch;
    internal TextBlock DeleteTorrentAfterCompleteStateTextControl => DeleteTorrentAfterCompleteStateText;
    internal ToggleSwitch BtAutoDownloadToggleSwitchControl => BtAutoDownloadToggleSwitch;
    internal TextBlock BtAutoDownloadStateTextControl => BtAutoDownloadStateText;
    internal ToggleSwitch BtForceEncryptionToggleSwitchControl => BtForceEncryptionToggleSwitch;
    internal TextBlock BtForceEncryptionStateTextControl => BtForceEncryptionStateText;
    internal ComboBox BtSeedingModeComboBoxControl => BtSeedingModeComboBox;
    internal NumberBox BtSeedRatioNumberBoxControl => BtSeedRatioNumberBox;
    internal NumberBox BtSeedTimeNumberBoxControl => BtSeedTimeNumberBox;
    internal NumberBox BtMaxPeersNumberBoxControl => BtMaxPeersNumberBox;
    internal Button BtTrackerSourceDropDownButtonControl => BtTrackerSourceDropDownButton;
    internal TextBlock BtTrackerSourceSummaryTextControl => BtTrackerSourceSummaryText;
    internal CheckBox BtTrackerNgosangBestCheckBoxControl => BtTrackerNgosangBestCheckBox;
    internal CheckBox BtTrackerNgosangBestIpCheckBoxControl => BtTrackerNgosangBestIpCheckBox;
    internal CheckBox BtTrackerNgosangAllCheckBoxControl => BtTrackerNgosangAllCheckBox;
    internal CheckBox BtTrackerNgosangAllIpCheckBoxControl => BtTrackerNgosangAllIpCheckBox;
    internal CheckBox BtTrackerNgosangCdnBestCheckBoxControl => BtTrackerNgosangCdnBestCheckBox;
    internal CheckBox BtTrackerNgosangCdnBestIpCheckBoxControl => BtTrackerNgosangCdnBestIpCheckBox;
    internal CheckBox BtTrackerNgosangCdnAllCheckBoxControl => BtTrackerNgosangCdnAllCheckBox;
    internal CheckBox BtTrackerNgosangCdnAllIpCheckBoxControl => BtTrackerNgosangCdnAllIpCheckBox;
    internal CheckBox BtTrackerXiu2BestCheckBoxControl => BtTrackerXiu2BestCheckBox;
    internal CheckBox BtTrackerXiu2AllCheckBoxControl => BtTrackerXiu2AllCheckBox;
    internal CheckBox BtTrackerXiu2HttpCheckBoxControl => BtTrackerXiu2HttpCheckBox;
    internal CheckBox BtTrackerXiu2CdnBestCheckBoxControl => BtTrackerXiu2CdnBestCheckBox;
    internal CheckBox BtTrackerXiu2CdnAllCheckBoxControl => BtTrackerXiu2CdnAllCheckBox;
    internal CheckBox BtTrackerXiu2CdnHttpCheckBoxControl => BtTrackerXiu2CdnHttpCheckBox;
    internal TextBox BtCustomTrackerSourceTextBoxControl => BtCustomTrackerSourceTextBox;
    internal ListView BtCustomTrackerSourceListViewControl => BtCustomTrackerSourceListView;
    internal TextBox BtTrackerSourceTextBoxControl => BtTrackerSourceTextBox;
    internal Button BtSyncTrackerButtonControl => BtSyncTrackerButton;
    internal TextBox BtTrackerListTextBoxControl => BtTrackerListTextBox;
    internal ToggleSwitch BtAutoSyncTrackerToggleSwitchControl => BtAutoSyncTrackerToggleSwitch;
    internal TextBlock BtAutoSyncTrackerStateTextControl => BtAutoSyncTrackerStateText;
    internal TextBlock BtLastTrackerSyncTextControl => BtLastTrackerSyncText;
    internal ToggleSwitch UseSystemProxyCheckBoxControl => UseSystemProxyCheckBox;
    internal TextBlock UseSystemProxyStateTextControl => UseSystemProxyStateText;
    internal ToggleSwitch CustomProxyToggleSwitchControl => CustomProxyToggleSwitch;
    internal TextBlock CustomProxyStateTextControl => CustomProxyStateText;
    internal TextBox ProxyServerTextBoxControl => ProxyServerTextBox;
    internal Button DetectSystemProxyButtonControl => DetectSystemProxyButton;
    internal TextBox ProxyBypassTextBoxControl => ProxyBypassTextBox;
    internal CheckBox ProxyDownloadsCheckBoxControl => ProxyDownloadsCheckBox;
    internal CheckBox ProxyTrackersCheckBoxControl => ProxyTrackersCheckBox;
    internal ToggleSwitch EnableUpnpToggleSwitchControl => EnableUpnpToggleSwitch;
    internal TextBlock EnableUpnpStateTextControl => EnableUpnpStateText;
    internal NumberBox BtListenPortNumberBoxControl => BtListenPortNumberBox;
    internal NumberBox DhtListenPortNumberBoxControl => DhtListenPortNumberBox;
    internal TextBox UserAgentTextBoxControl => UserAgentTextBox;
    internal NumberBox ConnectTimeoutNumberBoxControl => ConnectTimeoutNumberBox;
    internal NumberBox TimeoutNumberBoxControl => TimeoutNumberBox;
    internal ComboBox FileAllocationComboBoxControl => FileAllocationComboBox;
    internal ToggleSwitch TerminalOutputToggleSwitchControl => TerminalOutputToggleSwitch;
    internal TextBlock TerminalOutputStateTextControl => TerminalOutputStateText;
    internal TextBox AriaPathTextBoxControl => AriaPathTextBox;
    internal NumberBox RpcPortNumberBoxControl => RpcPortNumberBox;
    internal PasswordBox RpcSecretPasswordBoxControl => RpcSecretPasswordBox;
    internal ToggleSwitch ExtensionAutoSubmitToggleSwitchControl => ExtensionAutoSubmitToggleSwitch;
    internal TextBlock ExtensionAutoSubmitStateTextControl => ExtensionAutoSubmitStateText;
    internal NumberBox ExtensionApiPortNumberBoxControl => ExtensionApiPortNumberBox;
    internal PasswordBox ExtensionApiSecretPasswordBoxControl => ExtensionApiSecretPasswordBox;
    internal ComboBox LogLevelComboBoxControl => LogLevelComboBox;
    internal TextBlock AdvancedPathsSummaryTextControl => AdvancedPathsSummaryText;
    internal ToggleSwitch ClipboardDetectionToggleSwitchControl => ClipboardDetectionToggleSwitch;
    internal TextBlock ClipboardDetectionStateTextControl => ClipboardDetectionStateText;
    internal ToggleSwitch ClipboardHttpToggleSwitchControl => ClipboardHttpToggleSwitch;
    internal ToggleSwitch ClipboardFtpToggleSwitchControl => ClipboardFtpToggleSwitch;
    internal ToggleSwitch ClipboardMagnetToggleSwitchControl => ClipboardMagnetToggleSwitch;
    internal ToggleSwitch ClipboardThunderToggleSwitchControl => ClipboardThunderToggleSwitch;
    internal ToggleSwitch ClipboardBtHashToggleSwitchControl => ClipboardBtHashToggleSwitch;
    internal ToggleSwitch ProtocolMagnetToggleSwitchControl => ProtocolMagnetToggleSwitch;
    internal TextBlock ProtocolMagnetStateTextControl => ProtocolMagnetStateText;
    internal ToggleSwitch ProtocolThunderToggleSwitchControl => ProtocolThunderToggleSwitch;
    internal TextBlock ProtocolThunderStateTextControl => ProtocolThunderStateText;
    internal ToggleSwitch ProtocolOmniDownToggleSwitchControl => ProtocolOmniDownToggleSwitch;
    internal TextBlock ProtocolOmniDownStateTextControl => ProtocolOmniDownStateText;
    internal TextBlock SettingsAriaStatusTextControl => SettingsAriaStatusText;
    internal StackPanel ProcessStatusSettingControlControl => ProcessStatusSettingControl;
    internal FontIcon AriaStartStopIconControl => AriaStartStopIcon;
    internal Button AriaStartStopButtonControl => AriaStartStopButton;
    internal Button AriaRestartButtonControl => AriaRestartButton;
    internal TextBlock AboutVersionTextControl => AboutVersionText;
    internal TextBlock AboutCloneCommandTextControl => AboutCloneCommandText;

    internal event SelectionChangedEventHandler? SectionSelectionChanged;
    internal event RoutedEventHandler? SettingToggleSwitchToggled;
    internal event EventHandler<GeneralSettingChangedEventArgs>? GeneralSettingChanged;
    internal event EventHandler<CloseBehaviorSettingChangedEventArgs>? CloseBehaviorSettingChanged;
    internal event RoutedEventHandler? BrowseDownloadDirectoryRequested;
    internal event RoutedEventHandler? DownloadSettingChanged;
    internal event RoutedEventHandler? BitTorrentSettingChanged;
    internal event RoutedEventHandler? NetworkSettingChanged;
    internal event RoutedEventHandler? DetectSystemProxyRequested;
    internal event RoutedEventHandler? RandomBtPortRequested;
    internal event RoutedEventHandler? RandomDhtPortRequested;
    internal event RoutedEventHandler? UserAgentPresetRequested;
    internal event RoutedEventHandler? AdvancedSettingChanged;
    internal event RoutedEventHandler? BrowseAriaPathRequested;
    internal event RoutedEventHandler? CopyRpcSecretRequested;
    internal event RoutedEventHandler? GenerateRpcSecretRequested;
    internal event RoutedEventHandler? CopyExtensionApiSecretRequested;
    internal event RoutedEventHandler? GenerateExtensionApiSecretRequested;
    internal event RoutedEventHandler? OpenConfigFolderRequested;
    internal event RoutedEventHandler? CopySessionPathRequested;
    internal event RoutedEventHandler? ClearSessionRequested;
    internal event RoutedEventHandler? AddBtCustomTrackerRequested;
    internal event RoutedEventHandler? SyncBtTrackerRequested;
    internal event RoutedEventHandler? StartStopAriaRequested;
    internal event RoutedEventHandler? RestartAriaRequested;
    internal event RoutedEventHandler? CopyCloneCommandRequested;
    internal event RoutedEventHandler? OpenAboutLinkRequested;

    internal void ApplySearchFilter(string query)
    {
        foreach (SettingSearchEntry entry in GetSearchEntries())
        {
            entry.ApplyFilter(query);
        }
    }

    private IEnumerable<SettingSearchEntry> GetSearchEntries()
    {
        foreach (SettingSearchEntry entry in GeneralSettingsContent.SearchEntries)
        {
            yield return entry;
        }

        yield return new(DefaultDirectorySettingCard, "default", "directory", "download", "folder", Strings.Get("DefaultDirectoryLabel.Text"), "目录", "保存");
        yield return new(MaxConcurrentDownloadsSettingCard, "concurrent", "download", "task", "同时", "下载", "任务");
        yield return new(SplitCountSettingCard, "split", "segment", "connection", "thread", "分片", "连接数");
        yield return new(MaxConnectionPerServerSettingCard, "connection", "server", "thread", "服务器", "连接数");
        yield return new(ContinueDownloadSettingCard, "continue", "resume", "download", "断点", "续传");
        yield return new(RemoteTimeSettingCard, "remote", "time", "timestamp", "server", "时间戳", "服务器");
        yield return new(MaxTriesSettingCard, "retry", "tries", "network", "重试");
        yield return new(RetryWaitSettingCard, "retry", "wait", "seconds", "等待", "重试");
        yield return new(DownloadCleanupSettingCard, "cleanup", "stale", "record", "清理", "记录");
        yield return new(TorrentCleanupSettingCard, "torrent", "cleanup", "delete", "种子", "清理", "删除");

        yield return new(BtAutoDownloadSettingCard, "bittorrent", "torrent", "metalink", "magnet", "auto", "内容", "自动");
        yield return new(BtForceEncryptionSettingCard, "bittorrent", "encryption", "crypto", "加密");
        yield return new(BtKeepSeedingSettingCard, "bittorrent", "seed", "keep", "ratio", "time", "bt", "做种", "分享率", "时间");
        yield return new(BtMaxPeersSettingCard, "bittorrent", "peer", "max", "bt", "连接");
        yield return new(BtTrackerSourceSettingCard, "bittorrent", "tracker", "source", "sync", "bt", "同步");
        yield return new(BtTrackerCustomSourceSettingCard, "bittorrent", "tracker", "custom", "url", "bt", "自定义");
        yield return new(BtTrackerListSettingCard, "bittorrent", "tracker", "list", "bt");
        yield return new(BtAutoSyncTrackerSettingCard, "bittorrent", "tracker", "auto sync", "bt", "自动同步");

        yield return new(UseSystemProxySettingCard, "proxy", "system proxy", Strings.Get("ProxyLabel.Text"), "Use Windows system proxy when aria2 starts", "代理");
        yield return new(CustomProxySettingCard, "proxy", "http", "https", "socks", "custom", "代理");
        yield return new(UpnpSettingCard, "upnp", "nat", "pmp", "port", "端口", "映射");
        yield return new(BtPortSettingCard, "bt", "bittorrent", "listen", "port", "监听", "端口");
        yield return new(DhtPortSettingCard, "dht", "listen", "port", "监听", "端口");
        yield return new(UserAgentSettingCard, "user-agent", "ua", "browser", "transmission", "浏览器");
        yield return new(ConnectTimeoutSettingCard, "connect", "timeout", "seconds", "连接", "超时");
        yield return new(TimeoutSettingCard, "timeout", "seconds", "transfer", "传输", "超时");
        yield return new(FileAllocationSettingCard, "file", "allocation", "disk", "prealloc", "文件", "预分配");

        yield return new(AriaPathSettingCard, "aria2c", "path", Strings.Get("AriaPathLabel.Text"), Strings.Get("AriaPathTextBox.PlaceholderText"), "路径");
        yield return new(RpcPortSettingCard, "rpc", "port", Strings.Get("RpcPortLabel.Text"), "端口");
        yield return new(RpcSecretSettingCard, "rpc", "secret", "token", "密钥", "令牌");
        yield return new(ProcessStatusSettingCard, "process", "status", "aria2", Strings.Get("ProcessStatusLabel.Text"), "状态");
        yield return new(ExtensionAutoSubmitSettingCard, "extension", "browser", "auto submit", "扩展", "浏览器", "自动提交");
        yield return new(ExtensionApiPortSettingCard, "extension", "api", "port", "browser", "扩展", "端口");
        yield return new(ExtensionApiSecretSettingCard, "extension", "api", "secret", "browser", "扩展", "密钥");
        yield return new(LogLevelSettingCard, "log", "level", "debug", "日志", "级别");
        yield return new(AdvancedPathsSettingCard, "config", "session", "folder", "path", "配置", "会话", "目录");
        yield return new(SessionResetSettingCard, "session", "reset", "clear", "aria2", "会话", "清空");
        yield return new(ClipboardDetectionSettingCard, "clipboard", "detect", "paste", "剪贴板", "检测", "粘贴");
        yield return new(ClipboardTypesSettingCard, "clipboard", "http", "ftp", "magnet", "thunder", "hash", "剪贴板", "磁力", "迅雷");
        yield return new(ProtocolMagnetSettingCard, "default", "program", "protocol", "magnet", "默认程序", "协议", "磁力");
        yield return new(ProtocolThunderSettingCard, "default", "program", "protocol", "thunder", "默认程序", "协议", "迅雷");
        yield return new(ProtocolOmniDownSettingCard, "default", "program", "protocol", "omnidown", "extension", "默认程序", "协议", "扩展");
        yield return new(TerminalSettingCard, "terminal", "log", "debug", "aria2", "终端", "日志");

        yield return new(AboutAppCard, "about", "version", "omnidown", "关于", "版本");
        yield return new(AboutCloneCard, "clone", "repository", "github", "克隆", "仓库");
        yield return new(AboutIssueCard, "bug", "issue", "feature", "github", "问题", "建议");
        yield return new(AboutReferencesCard, "dependencies", "references", "license", "files", "motrix", "aria2", "unigetui", "winui", "依赖", "参考", "许可证");
        yield return new(AboutTrackerSourcesCard, "tracker", "trackers", "trackerslist", "TrackersListCollection", "ngosang", "xiu2", "bittorrent", "追踪器", "服务器");
        yield return new(AboutLicenseCard, "license", "third-party", "notice", "warranty", "mit", "gpl", "许可证", "第三方", "声明");
    }

    internal void ApplyGeneralSettings(GeneralSettings settings, bool isAutoStartEnabled)
    {
        GeneralSettingsContent.ApplyGeneralSettings(settings, isAutoStartEnabled);
    }

    internal GeneralSettings GetGeneralSettings(GeneralSettings currentSettings)
    {
        return GeneralSettingsContent.GetGeneralSettings(currentSettings);
    }

    internal void ApplyCloseBehaviorSettings(CloseBehaviorSettings settings)
    {
        GeneralSettingsContent.ApplyCloseBehaviorSettings(settings);
    }

    internal void SetAutoStartEnabled(bool isEnabled)
    {
        GeneralSettingsContent.SetAutoStartEnabled(isEnabled);
    }

    internal bool IsAutoStartEnabled => GeneralSettingsContent.IsAutoStartEnabled;

    private void SettingsSectionListView_SelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        SectionSelectionChanged?.Invoke(sender, args);
    }

    private void SettingToggleSwitch_Toggled(object sender, RoutedEventArgs args)
    {
        if (sender is ToggleSwitch toggleSwitch)
        {
            UpdateToggleStateText(toggleSwitch);
        }

        SettingToggleSwitchToggled?.Invoke(sender, args);
    }

    private void SetToggleSwitch(ToggleSwitch? toggleSwitch, bool isOn)
    {
        if (toggleSwitch is null)
        {
            return;
        }

        toggleSwitch.IsOn = isOn;
        UpdateToggleStateText(toggleSwitch);
    }

    private void UpdateToggleStateText(ToggleSwitch? toggleSwitch)
    {
        if (toggleSwitch is null)
        {
            return;
        }

        TextBlock? stateText = GetToggleStateText(toggleSwitch);
        if (stateText is not null)
        {
            stateText.Text = toggleSwitch.IsOn ? "开" : "关";
        }
    }

    private TextBlock? GetToggleStateText(ToggleSwitch toggleSwitch)
    {
        if (ReferenceEquals(toggleSwitch, ContinueDownloadToggleSwitch)) return ContinueDownloadStateText;
        if (ReferenceEquals(toggleSwitch, AutoDeleteStaleRecordsToggleSwitch)) return AutoDeleteStaleRecordsStateText;
        if (ReferenceEquals(toggleSwitch, DeleteTorrentAfterCompleteToggleSwitch)) return DeleteTorrentAfterCompleteStateText;
        if (ReferenceEquals(toggleSwitch, BtAutoDownloadToggleSwitch)) return BtAutoDownloadStateText;
        if (ReferenceEquals(toggleSwitch, BtForceEncryptionToggleSwitch)) return BtForceEncryptionStateText;
        if (ReferenceEquals(toggleSwitch, BtAutoSyncTrackerToggleSwitch)) return BtAutoSyncTrackerStateText;
        if (ReferenceEquals(toggleSwitch, UseSystemProxyCheckBox)) return UseSystemProxyStateText;
        if (ReferenceEquals(toggleSwitch, CustomProxyToggleSwitch)) return CustomProxyStateText;
        if (ReferenceEquals(toggleSwitch, EnableUpnpToggleSwitch)) return EnableUpnpStateText;
        if (ReferenceEquals(toggleSwitch, ExtensionAutoSubmitToggleSwitch)) return ExtensionAutoSubmitStateText;
        if (ReferenceEquals(toggleSwitch, ClipboardDetectionToggleSwitch)) return ClipboardDetectionStateText;
        if (ReferenceEquals(toggleSwitch, ProtocolMagnetToggleSwitch)) return ProtocolMagnetStateText;
        if (ReferenceEquals(toggleSwitch, ProtocolThunderToggleSwitch)) return ProtocolThunderStateText;
        if (ReferenceEquals(toggleSwitch, ProtocolOmniDownToggleSwitch)) return ProtocolOmniDownStateText;
        if (ReferenceEquals(toggleSwitch, TerminalOutputToggleSwitch)) return TerminalOutputStateText;

        return null;
    }

    private void AdvancedSettingTextBox_TextChanged(object sender, TextChangedEventArgs args)
    {
        AdvancedSettingChanged?.Invoke(sender, new RoutedEventArgs());
    }

    private void AdvancedSettingPasswordBox_PasswordChanged(object sender, RoutedEventArgs args)
    {
        AdvancedSettingChanged?.Invoke(sender, args);
    }

    private void AdvancedSettingNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        AdvancedSettingChanged?.Invoke(sender, new RoutedEventArgs());
    }

    private void AdvancedSettingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        AdvancedSettingChanged?.Invoke(sender, new RoutedEventArgs());
    }

    private void AdvancedSettingToggleSwitch_Toggled(object sender, RoutedEventArgs args)
    {
        SettingToggleSwitch_Toggled(sender, args);
        AdvancedSettingChanged?.Invoke(sender, args);
    }

    private void BrowseAriaPathButton_Click(object sender, RoutedEventArgs args)
    {
        BrowseAriaPathRequested?.Invoke(sender, args);
    }

    private void CopyRpcSecretButton_Click(object sender, RoutedEventArgs args)
    {
        CopyRpcSecretRequested?.Invoke(sender, args);
    }

    private void GenerateRpcSecretButton_Click(object sender, RoutedEventArgs args)
    {
        GenerateRpcSecretRequested?.Invoke(sender, args);
    }

    private void CopyExtensionApiSecretButton_Click(object sender, RoutedEventArgs args)
    {
        CopyExtensionApiSecretRequested?.Invoke(sender, args);
    }

    private void GenerateExtensionApiSecretButton_Click(object sender, RoutedEventArgs args)
    {
        GenerateExtensionApiSecretRequested?.Invoke(sender, args);
    }

    private void OpenConfigFolderButton_Click(object sender, RoutedEventArgs args)
    {
        OpenConfigFolderRequested?.Invoke(sender, args);
    }

    private void CopySessionPathButton_Click(object sender, RoutedEventArgs args)
    {
        CopySessionPathRequested?.Invoke(sender, args);
    }

    private void ClearSessionButton_Click(object sender, RoutedEventArgs args)
    {
        ClearSessionRequested?.Invoke(sender, args);
    }

    private void BrowseDownloadDirectoryButton_Click(object sender, RoutedEventArgs args)
    {
        BrowseDownloadDirectoryRequested?.Invoke(sender, args);
    }

    private void DownloadSettingTextBox_TextChanged(object sender, TextChangedEventArgs args)
    {
        DownloadSettingChanged?.Invoke(sender, new RoutedEventArgs());
    }

    private void DownloadSettingNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        DownloadSettingChanged?.Invoke(sender, new RoutedEventArgs());
    }

    private void DownloadSettingToggleSwitch_Toggled(object sender, RoutedEventArgs args)
    {
        SettingToggleSwitch_Toggled(sender, args);
        DownloadSettingChanged?.Invoke(sender, args);
    }

    private void DownloadSettingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        DownloadSettingChanged?.Invoke(sender, new RoutedEventArgs());
    }

    private void BitTorrentSettingToggleSwitch_Toggled(object sender, RoutedEventArgs args)
    {
        SettingToggleSwitch_Toggled(sender, args);
        BitTorrentSettingChanged?.Invoke(sender, args);
    }

    private void BitTorrentSettingTextBox_TextChanged(object sender, TextChangedEventArgs args)
    {
        BitTorrentSettingChanged?.Invoke(sender, new RoutedEventArgs());
    }

    private void BitTorrentSettingNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        BitTorrentSettingChanged?.Invoke(sender, new RoutedEventArgs());
    }

    private void BitTorrentSettingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        BitTorrentSettingChanged?.Invoke(sender, new RoutedEventArgs());
    }

    private void BitTorrentSettingCheckBox_Changed(object sender, RoutedEventArgs args)
    {
        BitTorrentSettingChanged?.Invoke(sender, args);
    }

    private void BitTorrentSettingListView_SelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        BitTorrentSettingChanged?.Invoke(sender, new RoutedEventArgs());
    }

    private void NetworkSettingToggleSwitch_Toggled(object sender, RoutedEventArgs args)
    {
        SettingToggleSwitch_Toggled(sender, args);
        NetworkSettingChanged?.Invoke(sender, args);
    }

    private void NetworkSettingTextBox_TextChanged(object sender, TextChangedEventArgs args)
    {
        NetworkSettingChanged?.Invoke(sender, new RoutedEventArgs());
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

    private void UserAgentPresetButton_Click(object sender, RoutedEventArgs args)
    {
        UserAgentPresetRequested?.Invoke(sender, args);
    }

    private void AddBtCustomTrackerButton_Click(object sender, RoutedEventArgs args)
    {
        AddBtCustomTrackerRequested?.Invoke(sender, args);
    }

    private void ToggleBtSeedSettingsButton_Click(object sender, RoutedEventArgs args)
    {
        bool shouldExpand = BtSeedLimitsBox.Visibility != Visibility.Visible;
        AnimateSettingsPanel(BtSeedLimitsBox, shouldExpand);
        BtSeedChevronIcon.Glyph = shouldExpand ? "\uE70E" : "\uE70D";
    }

    private void ToggleBtTrackerListButton_Click(object sender, RoutedEventArgs args)
    {
        bool shouldExpand = BtTrackerListBox.Visibility != Visibility.Visible;
        AnimateSettingsPanel(BtTrackerListBox, shouldExpand);
        BtTrackerListChevronIcon.Glyph = shouldExpand ? "\uE70E" : "\uE70D";
    }

    private void ToggleClipboardTypesButton_Click(object sender, RoutedEventArgs args)
    {
        bool shouldExpand = ClipboardTypesBox.Visibility != Visibility.Visible;
        AnimateSettingsPanel(ClipboardTypesBox, shouldExpand);
        ClipboardTypesChevronIcon.Glyph = shouldExpand ? "\uE70E" : "\uE70D";
    }

    private static void AnimateSettingsPanel(UIElement panel, bool expand)
    {
        if (expand)
        {
            panel.Visibility = Visibility.Visible;
        }

        DoubleAnimation opacityAnimation = new()
        {
            From = expand ? 0 : 1,
            To = expand ? 1 : 0,
            Duration = new Duration(TimeSpan.FromMilliseconds(140)),
            EnableDependentAnimation = true
        };

        Storyboard.SetTarget(opacityAnimation, panel);
        Storyboard.SetTargetProperty(opacityAnimation, "Opacity");

        Storyboard storyboard = new();
        storyboard.Children.Add(opacityAnimation);
        if (!expand)
        {
            storyboard.Completed += (_, _) => panel.Visibility = Visibility.Collapsed;
        }

        storyboard.Begin();
    }

    private void SyncBtTrackerButton_Click(object sender, RoutedEventArgs args)
    {
        SyncBtTrackerRequested?.Invoke(sender, args);
    }

    private void StartStopAriaButton_Click(object sender, RoutedEventArgs args)
    {
        StartStopAriaRequested?.Invoke(sender, args);
    }

    private void RestartAriaButton_Click(object sender, RoutedEventArgs args)
    {
        RestartAriaRequested?.Invoke(sender, args);
    }

    private void CopyCloneCommandButton_Click(object sender, RoutedEventArgs args)
    {
        CopyCloneCommandRequested?.Invoke(sender, args);
    }

    private void OpenAboutLinkButton_Click(object sender, RoutedEventArgs args)
    {
        OpenAboutLinkRequested?.Invoke(sender, args);
    }
}
