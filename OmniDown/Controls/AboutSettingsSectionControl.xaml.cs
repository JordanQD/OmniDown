using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;

namespace OmniDown.Controls;

public sealed partial class AboutSettingsSectionControl : UserControl
{
    public AboutSettingsSectionControl()
    {
        InitializeComponent();
    }

    internal IEnumerable<SettingSearchEntry> SearchEntries =>
    [
        new(AboutAppCard, "about", "version", "omnidown", "关于", "版本"),
        new(AboutCloneCard, "clone", "repository", "github", "克隆", "仓库"),
        new(AboutIssueCard, "bug", "issue", "feature", "github", "问题", "建议"),
        new(AboutReferencesCard, "dependencies", "references", "license", "files", "motrix", "aria2", "unigetui", "winui", "依赖", "参考", "许可证"),
        new(AboutTrackerSourcesCard, "tracker", "trackers", "trackerslist", "TrackersListCollection", "ngosang", "xiu2", "bittorrent", "追踪器", "服务器"),
        new(AboutLicenseCard, "license", "third-party", "notice", "warranty", "mit", "gpl", "许可证", "第三方", "声明")
    ];

    internal StackPanel AboutSettingsContentControl => AboutSettingsContent;
    internal TextBlock AboutVersionTextControl => AboutVersionText;
    internal TextBlock AboutCloneCommandTextControl => AboutCloneCommandText;

    internal event RoutedEventHandler? CopyCloneCommandRequested;
    internal event RoutedEventHandler? OpenAboutLinkRequested;

    private void CopyCloneCommandButton_Click(object sender, RoutedEventArgs args)
    {
        CopyCloneCommandRequested?.Invoke(sender, args);
    }

    private void OpenAboutLinkButton_Click(object sender, RoutedEventArgs args)
    {
        OpenAboutLinkRequested?.Invoke(sender, args);
    }
}
