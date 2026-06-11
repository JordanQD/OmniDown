using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using OmniDown.Controls;
using OmniDown.ViewModels;
using System;
using Windows.Foundation;

namespace OmniDown.Pages;

/// <summary>
/// 下载任务列表页面。包含工具栏、统计面板、任务列表、详情面板、状态栏。
/// </summary>
public sealed partial class DownloadsPage : Page
{
    private readonly DownloadsPageViewModel _viewModel;

    public DownloadsPage(DownloadsPageViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        HookSpeedChevronAnimation();
    }

    private void HookSpeedChevronAnimation()
    {
        // 动态创建 RotateTransform（不在 XAML 属性元素里放，避免字段生成问题）
        var chevronRotate = new RotateTransform { Angle = 0 };
        _statusBarSpeedChevron.RenderTransform = chevronRotate;

        // 初始：箭头朝上（ChevronDown 旋转 180° = 上箭头）
        chevronRotate.Angle = 180;

        if (_speedLimitButton?.Flyout is Flyout flyout)
        {
            flyout.Opened += (_, _) => chevronRotate.Angle = 0;    // 打开 → 下箭头
            flyout.Closed += (_, _) => chevronRotate.Angle = 180;  // 关闭 → 上箭头
        }
    }

    public DownloadsPageViewModel ViewModel => _viewModel;

    // ── x:Bind 辅助函数 ──

    public static Visibility BoolToVisibility(bool value) =>
        value ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility InvertBoolToVisibility(bool value) =>
        value ? Visibility.Collapsed : Visibility.Visible;

    public static double BoolToOpacity(bool value) =>
        value ? 1.0 : 0.0;

    // ── 控件公开属性（供 MainWindow 重定向使用）──

    public Grid TasksContentHost => _tasksContentHost;
    public Grid TasksPage => _tasksPage;
    public Grid StatsPanel => _statsPanel;
    public ListView TasksListView => _tasksListView;
    public Grid TasksLoadingPanel => _tasksLoadingPanel;
    public ProgressRing TasksLoadingRing => _tasksLoadingRing;
    public Border TasksHeaderPanel => _tasksHeaderPanel;
    public Border TasksPageSurface => _tasksPageSurface;
    public Border TasksListHeaderPanel => _tasksListHeaderPanel;
    public ColumnDefinition TaskDetailsHostColumn => _taskDetailsHostColumn;
    public CheckBox SelectAllTasksCheckBox => _selectAllTasksCheckBox;
    public TextBlock TotalTasksText => _totalTasksText;
    public TextBlock ActiveTasksText => _activeTasksText;
    public TextBlock PausedTasksText => _pausedTasksText;
    public TextBlock CompletedTasksText => _completedTasksText;
    public TextBlock IssueTasksText => _issueTasksText;
    public TextBlock DownloadsTitleText => _downloadsTitleText;
    public StackPanel TotalMetricPanel => _totalMetricPanel;
    public StackPanel ActiveMetricPanel => _activeMetricPanel;
    public StackPanel PausedMetricPanel => _pausedMetricPanel;
    public StackPanel CompletedMetricPanel => _completedMetricPanel;
    public StackPanel IssueMetricPanel => _issueMetricPanel;
    public TextBlock TotalMetricLabelText => _totalMetricLabelText;
    public TextBlock ActiveMetricLabelText => _activeMetricLabelText;
    public TextBlock PausedMetricLabelText => _pausedMetricLabelText;
    public TextBlock CompletedMetricLabelText => _completedMetricLabelText;
    public TextBlock IssueMetricLabelText => _issueMetricLabelText;
    public TextBlock GlobalUploadSpeedText => _globalUploadSpeedText;
    public TextBlock GlobalDownloadSpeedText => _globalDownloadSpeedText;
    public TextBlock GlobalUploadLimitText => _globalUploadLimitText;
    public TextBlock GlobalDownloadLimitText => _globalDownloadLimitText;
    public Grid GlobalUploadLimitIconPanel => _globalUploadLimitIconPanel;
    public Grid GlobalDownloadLimitIconPanel => _globalDownloadLimitIconPanel;
    public DropDownButton SpeedLimitButton => _speedLimitButton;
    public ToggleSwitch UploadLimitToggleSwitch => _uploadLimitToggleSwitch;
    public ToggleSwitch DownloadLimitToggleSwitch => _downloadLimitToggleSwitch;
    public NumberBox UploadLimitNumberBox => _uploadLimitNumberBox;
    public NumberBox DownloadLimitNumberBox => _downloadLimitNumberBox;
    public ComboBox UploadLimitUnitComboBox => _uploadLimitUnitComboBox;
    public ComboBox DownloadLimitUnitComboBox => _downloadLimitUnitComboBox;
    public AppBarButton NewDownloadButton => _newDownloadButton;
    public AppBarButton ResumeTasksButton => _resumeTasksButton;
    public AppBarButton PauseTasksButton => _pauseTasksButton;
    public AppBarButton RecoverTasksButton => _recoverTasksButton;
    public AppBarButton DeleteTasksButton => _deleteTasksButton;
    public AppBarButton SortTasksButton => _sortTasksButton;
    public AppBarToggleButton TaskDetailsButton => _taskDetailsButton;
    public AppBarButton OpenSelectedTaskFileButton => _openSelectedTaskFileButton;
    public AppBarButton OpenSelectedTaskFolderButton => _openSelectedTaskFolderButton;
    public AppBarButton CopySelectedTaskLinksButton => _copySelectedTaskLinksButton;
    public AppBarButton ClearCompletedTasksButton => _clearCompletedTasksButton;
    public ComboBox TaskFilterComboBox => _taskFilterComboBox;
    public ToggleMenuFlyoutItem SortByCreatedAtMenuItem => _sortByCreatedAtMenuItem;
    public ToggleMenuFlyoutItem SortByNameMenuItem => _sortByNameMenuItem;
    public ToggleMenuFlyoutItem SortBySizeMenuItem => _sortBySizeMenuItem;
    public ToggleMenuFlyoutItem SortAscendingMenuItem => _sortAscendingMenuItem;
    public ToggleMenuFlyoutItem SortDescendingMenuItem => _sortDescendingMenuItem;
    public Border StatusBarPanel => _statusBarPanel;
    public TextBlock StatusBarItemCountText => _statusBarItemCountText;
    public TextBlock StatusBarSelectedCountText => _statusBarSelectedCountText;
    public Rectangle StatusBarSelectedCountDivider => _statusBarSelectedCountDivider;
    public Rectangle StatusBarTaskCountsDivider => _statusBarTaskCountsDivider;
    public StackPanel StatusBarTaskCountsPanel => _statusBarTaskCountsPanel;
    public TextBlock StatusBarActiveTasksText => _statusBarActiveTasksText;
    public TextBlock StatusBarPausedTasksText => _statusBarPausedTasksText;
    public TextBlock StatusBarIssueTasksText => _statusBarIssueTasksText;
    public Grid StatusBarIssueTasksPanel => _statusBarIssueTasksPanel;
    public StackPanel StatusBarSpeedPanel => _statusBarSpeedPanel;
    public Button StatusBarSpeedButton => _statusBarSpeedButton;
    public TextBlock StatusBarUploadSpeedText => _statusBarUploadSpeedText;
    public TextBlock StatusBarDownloadSpeedText => _statusBarDownloadSpeedText;
    public StackPanel StatusBarUploadLimitPanel => _statusBarUploadLimitPanel;
    public StackPanel StatusBarDownloadLimitPanel => _statusBarDownloadLimitPanel;
    public TextBlock StatusBarUploadLimitText => _statusBarUploadLimitText;
    public TextBlock StatusBarDownloadLimitText => _statusBarDownloadLimitText;
    public Button NotificationHistoryButton => _notificationHistoryButton;
    public ListView NotificationHistoryListView => _notificationHistoryListView;
    public TextBlock DebugEngineText => _debugEngineText;
    public TaskDetailsPaneControl TaskDetailsPane => _taskDetailsPane;
    public InfoBar StatusToastInfoBar => _statusToastInfoBar;
    public Button StatusToastActionButton => _statusToastActionButton;
    public FontIcon StatusToastActionIcon => _statusToastActionIcon;
    public TextBlock StatusToastActionText => _statusToastActionText;
    public TeachingTip SettingsSaveTeachingTip => _settingsSaveTeachingTip;
    public TeachingTip AriaRestartTeachingTip => _ariaRestartTeachingTip;

    // ── 事件转发 ──

    public event RoutedEventHandler? NewDownloadButtonClick;
    public event RoutedEventHandler? ResumeTasksButtonClick;
    public event RoutedEventHandler? PauseTasksButtonClick;
    public event RoutedEventHandler? RecoverTasksButtonClick;
    public event RoutedEventHandler? DeleteTasksButtonClick;
    public event RoutedEventHandler? OpenSelectedTaskFileButtonClick;
    public event RoutedEventHandler? OpenSelectedTaskFolderButtonClick;
    public event RoutedEventHandler? CopySelectedTaskLinksButtonClick;
    public event RoutedEventHandler? ClearCompletedTasksButtonClick;
    public event RoutedEventHandler? TaskDetailsButtonClick;
    public event RoutedEventHandler? StatusBarSpeedButtonClick;
    public event RoutedEventHandler? SpeedLimitButtonClick;
    public event RoutedEventHandler? ApplySpeedLimitButtonClick;
    public event RoutedEventHandler? SortColumnMenuItemClick;
    public event RoutedEventHandler? SortDirectionMenuItemClick;
    public event RoutedEventHandler? StatusToastActionButtonClick;
    public event SelectionChangedEventHandler? TaskFilterSelectionChanged;
    public event RoutedEventHandler? UploadLimitToggleSwitchToggled;
    public event RoutedEventHandler? DownloadLimitToggleSwitchToggled;
    public event RoutedEventHandler? SelectAllTasksCheckBoxChecked;
    public event RoutedEventHandler? SelectAllTasksCheckBoxUnchecked;
    public event RoutedEventHandler? SelectAllTasksCheckBoxIndeterminate;
    public event RoutedEventHandler? TaskCheckBoxChanged;
    public event RoutedEventHandler? TaskItemLoaded;
    public event PointerEventHandler? TasksListViewPointerPressed;
    public event PointerEventHandler? TaskIconSelectionBoxPointerEntered;
    public event PointerEventHandler? TaskIconSelectionBoxPointerExited;
    public event RightTappedEventHandler? TasksListViewRightTapped;
    public event SelectionChangedEventHandler? TasksListViewSelectionChanged;
    public event EventHandler<object>? SortMenuFlyoutOpening;
    public event TypedEventHandler<InfoBar, InfoBarClosedEventArgs>? StatusToastInfoBarClosed;
    public event TypedEventHandler<TeachingTip, object>? SettingsSaveTeachingTipActionButtonClick;
    public event TypedEventHandler<TeachingTip, object>? SettingsSaveTeachingTipCloseButtonClick;
    public event TypedEventHandler<TeachingTip, object>? AriaRestartTeachingTipActionButtonClick;
    public event TypedEventHandler<TeachingTip, object>? AriaRestartTeachingTipCloseButtonClick;

    private void NewDownloadButton_Click(object sender, RoutedEventArgs e) => NewDownloadButtonClick?.Invoke(sender, e);
    private void ResumeTasksButton_Click(object sender, RoutedEventArgs e) => ResumeTasksButtonClick?.Invoke(sender, e);
    private void PauseTasksButton_Click(object sender, RoutedEventArgs e) => PauseTasksButtonClick?.Invoke(sender, e);
    private void RecoverTasksButton_Click(object sender, RoutedEventArgs e) => RecoverTasksButtonClick?.Invoke(sender, e);
    private void DeleteTasksButton_Click(object sender, RoutedEventArgs e) => DeleteTasksButtonClick?.Invoke(sender, e);
    private void OpenSelectedTaskFileButton_Click(object sender, RoutedEventArgs e) => OpenSelectedTaskFileButtonClick?.Invoke(sender, e);
    private void OpenSelectedTaskFolderButton_Click(object sender, RoutedEventArgs e) => OpenSelectedTaskFolderButtonClick?.Invoke(sender, e);
    private void CopySelectedTaskLinksButton_Click(object sender, RoutedEventArgs e) => CopySelectedTaskLinksButtonClick?.Invoke(sender, e);
    private void ClearCompletedTasksButton_Click(object sender, RoutedEventArgs e) => ClearCompletedTasksButtonClick?.Invoke(sender, e);
    private void TaskDetailsButton_Click(object sender, RoutedEventArgs e) => TaskDetailsButtonClick?.Invoke(sender, e);
    private void StatusBarSpeedButton_Click(object sender, RoutedEventArgs e) => StatusBarSpeedButtonClick?.Invoke(sender, e);
    private void SpeedLimitButton_Click(object sender, RoutedEventArgs e) => SpeedLimitButtonClick?.Invoke(sender, e);
    private void ApplySpeedLimitButton_Click(object sender, RoutedEventArgs e) => ApplySpeedLimitButtonClick?.Invoke(sender, e);
    private void SortColumnMenuItem_Click(object sender, RoutedEventArgs e) => SortColumnMenuItemClick?.Invoke(sender, e);
    private void SortDirectionMenuItem_Click(object sender, RoutedEventArgs e) => SortDirectionMenuItemClick?.Invoke(sender, e);
    private void StatusToastActionButton_Click(object sender, RoutedEventArgs e) => StatusToastActionButtonClick?.Invoke(sender, e);
    private void TaskFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => TaskFilterSelectionChanged?.Invoke(sender, e);
    private void UploadLimitToggleSwitch_Toggled(object sender, RoutedEventArgs e) => UploadLimitToggleSwitchToggled?.Invoke(sender, e);
    private void DownloadLimitToggleSwitch_Toggled(object sender, RoutedEventArgs e) => DownloadLimitToggleSwitchToggled?.Invoke(sender, e);
    private void SelectAllTasksCheckBox_Checked(object sender, RoutedEventArgs e) => SelectAllTasksCheckBoxChecked?.Invoke(sender, e);
    private void SelectAllTasksCheckBox_Unchecked(object sender, RoutedEventArgs e) => SelectAllTasksCheckBoxUnchecked?.Invoke(sender, e);
    private void SelectAllTasksCheckBox_Indeterminate(object sender, RoutedEventArgs e) => SelectAllTasksCheckBoxIndeterminate?.Invoke(sender, e);
    private void TaskCheckBox_Changed(object sender, RoutedEventArgs e) => TaskCheckBoxChanged?.Invoke(sender, e);
    private void TaskItem_Loaded(object sender, RoutedEventArgs e) => TaskItemLoaded?.Invoke(sender, e);
    private void TasksListView_PointerPressed(object sender, PointerRoutedEventArgs e) => TasksListViewPointerPressed?.Invoke(sender, e);
    private void TaskIconSelectionBox_PointerEntered(object sender, PointerRoutedEventArgs e) => TaskIconSelectionBoxPointerEntered?.Invoke(sender, e);
    private void TaskIconSelectionBox_PointerExited(object sender, PointerRoutedEventArgs e) => TaskIconSelectionBoxPointerExited?.Invoke(sender, e);
    private void TasksListView_RightTapped(object sender, RightTappedRoutedEventArgs e) => TasksListViewRightTapped?.Invoke(sender, e);
    private void TasksListView_SelectionChanged(object sender, SelectionChangedEventArgs e) => TasksListViewSelectionChanged?.Invoke(sender, e);
    private void SortMenuFlyout_Opening(object? sender, object e) => SortMenuFlyoutOpening?.Invoke(sender, e);
    private void StatusToastInfoBar_Closed(InfoBar sender, InfoBarClosedEventArgs args) => StatusToastInfoBarClosed?.Invoke(sender, args);
    private void SettingsSaveTeachingTip_ActionButtonClick(TeachingTip sender, object args) => SettingsSaveTeachingTipActionButtonClick?.Invoke(sender, args);
    private void SettingsSaveTeachingTip_CloseButtonClick(TeachingTip sender, object args) => SettingsSaveTeachingTipCloseButtonClick?.Invoke(sender, args);
    private void AriaRestartTeachingTip_ActionButtonClick(TeachingTip sender, object args) => AriaRestartTeachingTipActionButtonClick?.Invoke(sender, args);
    private void AriaRestartTeachingTip_CloseButtonClick(TeachingTip sender, object args) => AriaRestartTeachingTipCloseButtonClick?.Invoke(sender, args);
}
