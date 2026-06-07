using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OmniDown.Models;
using OmniDown.Models.Settings;
using OmniDown.Services.BrowserExtension;
using OmniDown.Services.Downloads;
using OmniDown.Services.Engine;
using OmniDown.Services.Logging;
using OmniDown.Services.Notifications;
using OmniDown.Services.Rpc;
using OmniDown.Services.Settings;
using OmniDown.Services.Shell;
using OmniDown.Services.Storage;
using OmniDown.Services.Widgets;
using OmniDown.Pages;
using OmniDown.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using WinRT.Interop;

namespace OmniDown
{
    public sealed partial class MainWindow : Window
    {
        private sealed record TorrentSelection(
            string Path,
            string DisplayName,
            byte[] Bytes,
            TorrentMetadata Metadata);

        private sealed record AriaRelatedSettingsSnapshot(
            DownloadSettings Download,
            BitTorrentSettings BitTorrent,
            NetworkSettings Network,
            AdvancedSettings Advanced);

        private enum SpeedLimitTargetMode
        {
            Global,
            Task
        }

        private const string CloneCommand = "git clone https://github.com/JordanQD/OmniDown.git";
        private readonly Aria2EngineHost _aria2EngineHost = new();
        private readonly Aria2RpcClient _aria2RpcClient = new();
        private readonly DownloadCoordinator _downloadCoordinator;
        private readonly SystemNotificationService _notifications;
        private readonly TaskbarProgressService _taskbarProgress;
        private TrayIconService? _trayIcon;
        private readonly DispatcherTimer _refreshTimer = new();
        private readonly DispatcherTimer _statusMessageTimer = new();
        private string _rpcSecret = AdvancedSettings.Default.RpcSecret;
        private readonly DownloadsPageViewModel _downloadsViewModel = new();
        private readonly ObservableCollection<DownloadTask> _visibleTasks = new();
        private readonly ObservableCollection<AppStatusMessage> _statusMessages = new();
        private readonly AutoStartService _autoStartService = new();
        private string _currentTaskFilter = "Home";
        private TaskSortColumn _sortColumn = TaskSortColumn.CreatedAt;
        private bool _sortAscending = false;
        private bool _isRefreshing;
        private bool _isUpdatingSelectAllCheckBox;
        private bool _isUpdatingTaskSelection;
        private bool _hasStartedInitialLoad;
        private bool _isDownloadSpeedLimitEnabled;
        private bool _isUploadSpeedLimitEnabled;
        private SpeedLimitTargetMode _speedLimitTargetMode = SpeedLimitTargetMode.Global;
        private string _speedLimitTaskGid = string.Empty;
        private int _taskDetailsSpeedLimitRequestId;
        private bool _isTaskDetailsPaneOpen;
        private long _currentGlobalDownloadSpeed;
        private long _currentGlobalUploadSpeed;
        private long _downloadLimitBytesPerSecond;
        private long _uploadLimitBytesPerSecond;
        private readonly Dictionary<string, string> _observedTaskStatuses = new(StringComparer.OrdinalIgnoreCase);
        private readonly AppSettingsStore _settingsStore = new();
        private readonly SettingsPageViewModel _settingsPageViewModel;
        private readonly nint _windowHandle;
        private bool _isExitRequested;
        private bool _isClosePromptOpen;
        private bool _isLoadingCloseBehaviorSettings;
        private bool _isLoadingGeneralSettings;
        private bool _isLoadingDownloadSettings;
        private bool _isLoadingBitTorrentSettings;
        private bool _isLoadingNetworkSettings;
        private bool _isLoadingAdvancedSettings;
        private bool _isNewDownloadDialogOpen;
        private bool _hasTriggeredAutoShutdown;
        private bool _hasSeenActiveDownloadsForAutoShutdown;
        private bool _isShutdownPrepared;
        private string _runningAriaSettingsSignature = string.Empty;
        private int _runningAriaRpcPort;
        private string _runningAriaRpcSecret = string.Empty;
        private AriaRelatedSettingsSnapshot? _pendingAriaSettingsRollback;
        private AriaRelatedSettingsSnapshot? _restartAriaSettingsRollback;
        private bool _isSavingAriaSettings;
        private bool _statusToastActionRestartsAria;
        private bool _isHiding;
        private string _lastStatusMessage = string.Empty;
        private string _lastClipboardDownloadText = string.Empty;
        private readonly Dictionary<string, bool> _observedTaskDownloadCompletions = new(StringComparer.OrdinalIgnoreCase);
        private readonly WidgetSnapshotStore _widgetSnapshotStore = new();
        private DownloadsPage? _downloadsPage;
        private AppSettingsPage? _appSettingsPage;

        public ObservableCollection<DownloadTask> Tasks { get; } = new();

        public MainWindow()
        {
            AppLogger.PrepareLogFile(AppPaths.AppLogPath);
            _browserExtensionApiServer = new BrowserExtensionApiServer(
                HandleBrowserExtensionAddAsync,
                HandleBrowserExtensionStatAsync,
                PauseAllBrowserExtensionTasksAsync,
                ResumeAllBrowserExtensionTasksAsync,
                GetBrowserExtensionVersion);
            _settingsPageViewModel = new SettingsPageViewModel(_settingsStore);
            InitializeComponent();
            _appSettingsPage = new AppSettingsPage();
            _downloadsPage = new DownloadsPage(_downloadsViewModel);
            WireMainPageEvents();
            _downloadsPage.TasksListView.ItemsSource = _visibleTasks;
            _downloadsPage.NotificationHistoryListView.ItemsSource = _statusMessages;
            ContentFrame.Content = _downloadsPage;
            WinUIGallery.App.MainWindow.NavigationView = RootNavigation;
            HookSettingsPageEvents();
            _downloadsViewModel.AllTasks.Clear();
            SetTaskListLoading(true);
            _windowHandle = WindowNative.GetWindowHandle(this);
            SetWindowIcon();
            LoadGeneralSettings();
            SyncAutoStartToggle();
            ApplyWindowPlacementOrDefault();
            _taskbarProgress = new TaskbarProgressService(_windowHandle);
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
            AppWindow.Closing += MainWindow_Closing;

            // Clean up completed tasks from the on-disk cache at startup, so that even
            // if the exit-time cleanup failed (RPC timeout, file lock, process killed, etc.),
            // the next launch starts with a clean task list.
            if (_settingsPageViewModel.GeneralSettings.AutoClearCompletedOnExit)
            {
                PurgeCompletedTasksFromCacheFile();
            }

            _downloadCoordinator = new DownloadCoordinator(_aria2RpcClient, Tasks);
            _downloadCoordinator.DeleteTorrentAfterComplete = _settingsPageViewModel.DownloadSettings.DeleteTorrentAfterComplete;
            _notifications = ((App)Application.Current).Notifications;
            _notifications.NotificationInvoked += Notifications_NotificationInvoked;
            RecordObservedTaskStatuses();
            Closed += MainWindow_Closed;
            _refreshTimer.Interval = TimeSpan.FromSeconds(1);
            _refreshTimer.Tick += RefreshTimer_Tick;
            _statusMessageTimer.Interval = TimeSpan.FromSeconds(3);
            _statusMessageTimer.Tick += StatusMessageTimer_Tick;

            SettingsPage.NavigateTo("Home");
            InitializeAboutSection();

            LoadDownloadSettings();
            LoadBitTorrentSettings();
            LoadNetworkSettings();
            LoadAdvancedSettings();
            StartBrowserExtensionApiServer();
            Clipboard.ContentChanged += Clipboard_ContentChanged;
            _ = AutoSyncBitTorrentTrackersIfNeededAsync();
            LoadSpeedLimitSettings();
            LoadCloseBehaviorSettings();
            UpdateSearchPlaceholder();
            UpdateDownloadsHeader("Home");
            UpdateStatsVisibility("Home");
            ApplySettingsFilter();
            UpdateDashboard();
            UpdateGlobalSpeeds(0, 0);
            UpdateGlobalSpeedLimitText();
            UpdateAriaStatus();
            UpdateDebugStatus();
            UpdateTaskDetailsPane();
            ApplyToolbarTooltips();
            AppLogger.Info("App", "MainWindow initialized");
            _ = CheckEngineUpdateAsync();
        }

        private async Task CheckEngineUpdateAsync(bool isManual = false)
        {
            if (!isManual)
            {
                try
                {
                    object? value = Windows.Storage.ApplicationData.Current.LocalSettings.Values["EngineAutoUpdateEnabled"];
                    if (value is not true) return;
                }
                catch { return; }
            }

            try
            {
                EngineUpdateService updater = new();

                // Ensure engine is in writable local data (copy from AppX on first run)
                bool engineAvailable = await updater.EnsureEngineAvailableAsync();
                if (!engineAvailable)
                {
                    if (isManual) ShowMessage("未找到可更新的内置引擎。", InfoBarSeverity.Error);
                    return;
                }

                string bundledPath = updater.GetBundledEnginePath();

                if (!File.Exists(bundledPath))
                {
                    AppLogger.Info("EngineUpdater", "no bundled engine found, skipping update check");
                    if (isManual) ShowMessage("未找到内置引擎。", InfoBarSeverity.Error);
                    return;
                }

                string currentVersion = await DetectEngineVersionAsync(bundledPath);
                if (string.IsNullOrWhiteSpace(currentVersion))
                {
                    AppLogger.Info("EngineUpdater", "could not detect current engine version");
                    if (isManual) ShowMessage("无法检测当前引擎版本。", InfoBarSeverity.Error);
                    return;
                }

                EngineUpdateCheckResult result = await updater.CheckForUpdateAsync(currentVersion, forceRefresh: isManual);

                if (!result.Succeeded)
                {
                    if (isManual) ShowMessage($"检查更新失败：{result.ErrorMessage ?? "未知错误"}", InfoBarSeverity.Error);
                    return;
                }

                if (!result.UpdateAvailable)
                {
                    if (isManual) ShowMessage($"aria2-next {currentVersion} 已是最新版本。", InfoBarSeverity.Success);
                    return;
                }

                EngineUpdateInfo update = result.Update!;
                ShowMessage($"正在下载 aria2-next {update.Version}…", InfoBarSeverity.Success);

                bool wasRunning = _aria2EngineHost.IsRunning;
                if (wasRunning)
                {
                    await StopAriaAsync(showMessage: false);
                }

                bool installed = await updater.DownloadAndInstallAsync(update, bundledPath);

                if (wasRunning)
                {
                    await StartAriaAsync();
                }

                if (installed)
                {
                    // Bust cache so next check reflects the new version
                    TryBustUpdateCache();
                    ShowMessage($"aria2-next 已更新到 {update.Version}，已自动重启。", InfoBarSeverity.Success);
                }
                else
                {
                    ShowMessage($"aria2-next {update.Version} 安装失败，请查看日志。", InfoBarSeverity.Error);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Warning("EngineUpdater", $"update check failed: {ex.Message}");
                if (isManual) ShowMessage($"内核更新检查失败：{ex.Message}", InfoBarSeverity.Error);
            }
        }

        private static void TryBustUpdateCache()
        {
            try
            {
                string path = Path.Combine(AppPaths.LocalDataDirectory, "engine_update_cache.json");
                if (File.Exists(path)) File.Delete(path);
            }
            catch { }
        }

        private static async Task<string> DetectEngineVersionAsync(string executablePath)
        {
            try
            {
                System.Diagnostics.Process process = new()
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = executablePath,
                        ArgumentList = { "--version" },
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };
                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();
                string firstLine = output.Split('\n', StringSplitOptions.RemoveEmptyEntries)[0].Trim();
                int idx = firstLine.LastIndexOf("version ", StringComparison.OrdinalIgnoreCase);
                return idx >= 0 ? firstLine[(idx + 8)..].Trim() : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private void WireMainPageEvents()
        {
            if (_downloadsPage == null) return;

            _downloadsPage.NewDownloadButtonClick += NewDownloadButton_Click;
            _downloadsPage.ResumeTasksButtonClick += ResumeTasksButton_Click;
            _downloadsPage.PauseTasksButtonClick += PauseTasksButton_Click;
            _downloadsPage.RecoverTasksButtonClick += RecoverTasksButton_Click;
            _downloadsPage.DeleteTasksButtonClick += DeleteTasksButton_Click;
            _downloadsPage.OpenSelectedTaskFileButtonClick += OpenSelectedTaskFileButton_Click;
            _downloadsPage.OpenSelectedTaskFolderButtonClick += OpenSelectedTaskFolderButton_Click;
            _downloadsPage.CopySelectedTaskLinksButtonClick += CopySelectedTaskLinksButton_Click;
            _downloadsPage.ClearCompletedTasksButtonClick += ClearCompletedTasksButton_Click;
            _downloadsPage.TaskDetailsButtonClick += TaskDetailsButton_Click;
            _downloadsPage.StatusBarSpeedButtonClick += StatusBarSpeedButton_Click;
            _downloadsPage.SpeedLimitButtonClick += SpeedLimitButton_Click;
            _downloadsPage.ApplySpeedLimitButtonClick += ApplySpeedLimitButton_Click;
            _downloadsPage.SortColumnMenuItemClick += SortColumnMenuItem_Click;
            _downloadsPage.SortDirectionMenuItemClick += SortDirectionMenuItem_Click;
            _downloadsPage.StatusToastActionButtonClick += StatusToastActionButton_Click;
            _downloadsPage.UploadLimitToggleSwitchToggled += UploadLimitToggleSwitch_Toggled;
            _downloadsPage.DownloadLimitToggleSwitchToggled += DownloadLimitToggleSwitch_Toggled;
            _downloadsPage.SelectAllTasksCheckBoxChecked += SelectAllTasksCheckBox_Checked;
            _downloadsPage.SelectAllTasksCheckBoxUnchecked += SelectAllTasksCheckBox_Unchecked;
            _downloadsPage.SelectAllTasksCheckBoxIndeterminate += SelectAllTasksCheckBox_Indeterminate;
            _downloadsPage.TaskCheckBoxChanged += TaskCheckBox_Changed;
            _downloadsPage.TaskItemLoaded += TaskItem_Loaded;
            _downloadsPage.TasksListViewPointerPressed += TasksListView_PointerPressed;
            _downloadsPage.TaskIconSelectionBoxPointerEntered += TaskIconSelectionBox_PointerEntered;
            _downloadsPage.TaskIconSelectionBoxPointerExited += TaskIconSelectionBox_PointerExited;
            _downloadsPage.TasksListViewRightTapped += TasksListView_RightTapped;
            _downloadsPage.TasksListViewSelectionChanged += TasksListView_SelectionChanged;
            _downloadsPage.SortMenuFlyoutOpening += SortMenuFlyout_Opening;
            _downloadsPage.StatusToastInfoBarClosed += StatusToastInfoBar_Closed;
            _downloadsPage.SettingsSaveTeachingTipActionButtonClick += SettingsSaveTeachingTip_ActionButtonClick;
            _downloadsPage.SettingsSaveTeachingTipCloseButtonClick += SettingsSaveTeachingTip_CloseButtonClick;
            _downloadsPage.AriaRestartTeachingTipActionButtonClick += AriaRestartTeachingTip_ActionButtonClick;
            _downloadsPage.AriaRestartTeachingTipCloseButtonClick += AriaRestartTeachingTip_CloseButtonClick;
            _downloadsPage.TaskDetailsPane.SpeedLimitApplyRequested += TaskDetailsPane_SpeedLimitApplyRequested;
        }
    }
}
