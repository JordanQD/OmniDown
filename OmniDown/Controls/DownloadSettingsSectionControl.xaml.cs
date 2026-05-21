using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OmniDown.Services.Localization;
using System.Collections.Generic;

namespace OmniDown.Controls;

public sealed partial class DownloadSettingsSectionControl : UserControl
{
    public DownloadSettingsSectionControl()
    {
        InitializeComponent();
    }

    internal IEnumerable<SettingSearchEntry> SearchEntries =>
    [
        new(DefaultDirectorySettingCard, "default", "directory", "download", "folder", Strings.Get("DefaultDirectoryLabel.Text"), "目录", "保存"),
        new(MaxConcurrentDownloadsSettingCard, "concurrent", "download", "task", "同时", "下载", "任务"),
        new(SplitCountSettingCard, "split", "segment", "connection", "thread", "分片", "连接数"),
        new(MaxConnectionPerServerSettingCard, "connection", "server", "thread", "服务器", "连接数"),
        new(ContinueDownloadSettingCard, "continue", "resume", "download", "断点", "续传"),
        new(RemoteTimeSettingCard, "remote", "time", "timestamp", "server", "时间戳", "服务器"),
        new(MaxTriesSettingCard, "retry", "tries", "network", "重试"),
        new(RetryWaitSettingCard, "retry", "wait", "seconds", "等待", "重试"),
        new(DownloadCleanupSettingCard, "cleanup", "stale", "record", "清理", "记录"),
        new(TorrentCleanupSettingCard, "torrent", "cleanup", "delete", "种子", "清理", "删除")
    ];

    internal StackPanel DownloadSettingsContentControl => DownloadSettingsContent;
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

    internal event RoutedEventHandler? BrowseDownloadDirectoryRequested;
    internal event RoutedEventHandler? DownloadSettingChanged;

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
        UpdateToggleStateText(sender as ToggleSwitch);
        DownloadSettingChanged?.Invoke(sender, args);
    }

    private void DownloadSettingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        DownloadSettingChanged?.Invoke(sender, new RoutedEventArgs());
    }

    private void UpdateToggleStateText(ToggleSwitch? toggleSwitch)
    {
        if (toggleSwitch is null) return;

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
        return null;
    }
}
