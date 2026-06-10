using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Shapes;
using OmniDown.Controls;
using OmniDown.Pages;

namespace OmniDown;

/// <summary>
/// 重定向属性，将旧 x:Name 字段映射到拆分后的 DownloadsPage / AppSettingsPage。
/// </summary>
public sealed partial class MainWindow
{
    // ── DownloadsPage 控件（所有任务列表、状态栏、工具栏、通知）──

    private Grid TasksContentHost => _downloadsPage!.TasksContentHost;
    private Grid TasksPage => _downloadsPage!.TasksPage;
    private Grid StatsPanel => _downloadsPage!.StatsPanel;
    private Border TasksHeaderPanel => _downloadsPage!.TasksHeaderPanel;
    private Border TasksPageSurface => _downloadsPage!.TasksPageSurface;
    private Border TasksListHeaderPanel => _downloadsPage!.TasksListHeaderPanel;
    private ColumnDefinition TaskDetailsHostColumn => _downloadsPage!.TaskDetailsHostColumn;

    private ListView TasksListView => _downloadsPage!.TasksListView;
    private Grid TasksLoadingPanel => _downloadsPage!.TasksLoadingPanel;
    private ProgressRing TasksLoadingRing => _downloadsPage!.TasksLoadingRing;
    private CheckBox SelectAllTasksCheckBox => _downloadsPage!.SelectAllTasksCheckBox;

    private TaskDetailsPaneControl TaskDetailsPane => _downloadsPage!.TaskDetailsPane;

    private TextBlock TotalTasksText => _downloadsPage!.TotalTasksText;
    private TextBlock ActiveTasksText => _downloadsPage!.ActiveTasksText;
    private TextBlock PausedTasksText => _downloadsPage!.PausedTasksText;
    private TextBlock CompletedTasksText => _downloadsPage!.CompletedTasksText;
    private TextBlock IssueTasksText => _downloadsPage!.IssueTasksText;
    private TextBlock DownloadsTitleText => _downloadsPage!.DownloadsTitleText;
    private StackPanel TotalMetricPanel => _downloadsPage!.TotalMetricPanel;
    private StackPanel ActiveMetricPanel => _downloadsPage!.ActiveMetricPanel;
    private StackPanel PausedMetricPanel => _downloadsPage!.PausedMetricPanel;
    private StackPanel CompletedMetricPanel => _downloadsPage!.CompletedMetricPanel;
    private StackPanel IssueMetricPanel => _downloadsPage!.IssueMetricPanel;
    private TextBlock TotalMetricLabelText => _downloadsPage!.TotalMetricLabelText;
    private TextBlock ActiveMetricLabelText => _downloadsPage!.ActiveMetricLabelText;
    private TextBlock PausedMetricLabelText => _downloadsPage!.PausedMetricLabelText;
    private TextBlock CompletedMetricLabelText => _downloadsPage!.CompletedMetricLabelText;
    private TextBlock IssueMetricLabelText => _downloadsPage!.IssueMetricLabelText;

    private TextBlock GlobalUploadSpeedText => _downloadsPage!.GlobalUploadSpeedText;
    private TextBlock GlobalDownloadSpeedText => _downloadsPage!.GlobalDownloadSpeedText;
    private TextBlock GlobalUploadLimitText => _downloadsPage!.GlobalUploadLimitText;
    private TextBlock GlobalDownloadLimitText => _downloadsPage!.GlobalDownloadLimitText;
    private Grid GlobalUploadLimitIconPanel => _downloadsPage!.GlobalUploadLimitIconPanel;
    private Grid GlobalDownloadLimitIconPanel => _downloadsPage!.GlobalDownloadLimitIconPanel;
    private DropDownButton SpeedLimitButton => _downloadsPage!.SpeedLimitButton;
    private ToggleSwitch UploadLimitToggleSwitch => _downloadsPage!.UploadLimitToggleSwitch;
    private ToggleSwitch DownloadLimitToggleSwitch => _downloadsPage!.DownloadLimitToggleSwitch;
    private NumberBox UploadLimitNumberBox => _downloadsPage!.UploadLimitNumberBox;
    private NumberBox DownloadLimitNumberBox => _downloadsPage!.DownloadLimitNumberBox;
    private ComboBox UploadLimitUnitComboBox => _downloadsPage!.UploadLimitUnitComboBox;
    private ComboBox DownloadLimitUnitComboBox => _downloadsPage!.DownloadLimitUnitComboBox;

    private AppBarButton NewDownloadButton => _downloadsPage!.NewDownloadButton;
    private AppBarButton ResumeTasksButton => _downloadsPage!.ResumeTasksButton;
    private AppBarButton PauseTasksButton => _downloadsPage!.PauseTasksButton;
    private AppBarButton RecoverTasksButton => _downloadsPage!.RecoverTasksButton;
    private AppBarButton DeleteTasksButton => _downloadsPage!.DeleteTasksButton;
    private AppBarButton SortTasksButton => _downloadsPage!.SortTasksButton;
    private AppBarToggleButton TaskDetailsButton => _downloadsPage!.TaskDetailsButton;
    private AppBarButton OpenSelectedTaskFileButton => _downloadsPage!.OpenSelectedTaskFileButton;
    private AppBarButton OpenSelectedTaskFolderButton => _downloadsPage!.OpenSelectedTaskFolderButton;
    private AppBarButton CopySelectedTaskLinksButton => _downloadsPage!.CopySelectedTaskLinksButton;
    private AppBarButton ClearCompletedTasksButton => _downloadsPage!.ClearCompletedTasksButton;
    private ToggleMenuFlyoutItem SortByCreatedAtMenuItem => _downloadsPage!.SortByCreatedAtMenuItem;
    private ToggleMenuFlyoutItem SortByNameMenuItem => _downloadsPage!.SortByNameMenuItem;
    private ToggleMenuFlyoutItem SortBySizeMenuItem => _downloadsPage!.SortBySizeMenuItem;
    private ToggleMenuFlyoutItem SortAscendingMenuItem => _downloadsPage!.SortAscendingMenuItem;
    private ToggleMenuFlyoutItem SortDescendingMenuItem => _downloadsPage!.SortDescendingMenuItem;

    private Border StatusBarPanel => _downloadsPage!.StatusBarPanel;
    private TextBlock StatusBarItemCountText => _downloadsPage!.StatusBarItemCountText;
    private TextBlock StatusBarSelectedCountText => _downloadsPage!.StatusBarSelectedCountText;
    private Rectangle StatusBarSelectedCountDivider => _downloadsPage!.StatusBarSelectedCountDivider;
    private Rectangle StatusBarTaskCountsDivider => _downloadsPage!.StatusBarTaskCountsDivider;
    private StackPanel StatusBarTaskCountsPanel => _downloadsPage!.StatusBarTaskCountsPanel;
    private TextBlock StatusBarActiveTasksText => _downloadsPage!.StatusBarActiveTasksText;
    private TextBlock StatusBarPausedTasksText => _downloadsPage!.StatusBarPausedTasksText;
    private TextBlock StatusBarIssueTasksText => _downloadsPage!.StatusBarIssueTasksText;
    private Grid StatusBarIssueTasksPanel => _downloadsPage!.StatusBarIssueTasksPanel;
    private StackPanel StatusBarSpeedPanel => _downloadsPage!.StatusBarSpeedPanel;
    private Button StatusBarSpeedButton => _downloadsPage!.StatusBarSpeedButton;
    private TextBlock StatusBarUploadSpeedText => _downloadsPage!.StatusBarUploadSpeedText;
    private TextBlock StatusBarDownloadSpeedText => _downloadsPage!.StatusBarDownloadSpeedText;
    private StackPanel StatusBarUploadLimitPanel => _downloadsPage!.StatusBarUploadLimitPanel;
    private StackPanel StatusBarDownloadLimitPanel => _downloadsPage!.StatusBarDownloadLimitPanel;
    private TextBlock StatusBarUploadLimitText => _downloadsPage!.StatusBarUploadLimitText;
    private TextBlock StatusBarDownloadLimitText => _downloadsPage!.StatusBarDownloadLimitText;

    private Button NotificationHistoryButton => _downloadsPage!.NotificationHistoryButton;
    private ListView NotificationHistoryListView => _downloadsPage!.NotificationHistoryListView;
    private TextBlock DebugEngineText => _downloadsPage!.DebugEngineText;

    private InfoBar StatusToastInfoBar => _downloadsPage!.StatusToastInfoBar;
    private Button StatusToastActionButton => _downloadsPage!.StatusToastActionButton;
    private FontIcon StatusToastActionIcon => _downloadsPage!.StatusToastActionIcon;
    private TextBlock StatusToastActionText => _downloadsPage!.StatusToastActionText;
    private TeachingTip SettingsSaveTeachingTip => _downloadsPage!.SettingsSaveTeachingTip;
    private TeachingTip AriaRestartTeachingTip => _downloadsPage!.AriaRestartTeachingTip;

    // ── AppSettingsPage 控件 ──

    private AppSettingsPage SettingsPage => _appSettingsPage!;
    internal ScrollViewer SettingsContentScrollViewer => _appSettingsPage!.SettingsContentScrollViewerControl!;
}
