using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using OmniDown.Models;
using OmniDown.Models.Settings;
using OmniDown.Services.BrowserExtension;
using OmniDown.Services.Downloads;
using OmniDown.Services.Engine;
using OmniDown.Services.Localization;
using OmniDown.Services.Logging;
using OmniDown.Services.Notifications;
using OmniDown.Services.Rpc;
using OmniDown.Services.Settings;
using OmniDown.Services.Shell;
using OmniDown.Services.Storage;
using OmniDown.Services.Widgets;
using OmniDown.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI;
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
        private bool _isTaskDetailsPaneOpen;
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

        public ObservableCollection<DownloadTask> Tasks { get; } = new();

        public MainWindow()
        {
            _browserExtensionApiServer = new BrowserExtensionApiServer(
                HandleBrowserExtensionAddAsync,
                HandleBrowserExtensionStatAsync,
                PauseAllBrowserExtensionTasksAsync,
                ResumeAllBrowserExtensionTasksAsync,
                GetBrowserExtensionVersion);
            _settingsPageViewModel = new SettingsPageViewModel(_settingsStore);
            InitializeComponent();
            WinUIGallery.App.MainWindow.NavigationView = RootNavigation;
            HookSettingsPageEvents();
            TasksListView.ItemsSource = _visibleTasks;
            NotificationHistoryListView.ItemsSource = _statusMessages;
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
            _refreshTimer.Interval = TimeSpan.FromSeconds(2);
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
                bool installed = await updater.DownloadAndInstallAsync(update, bundledPath);
                if (installed)
                {
                    // Bust cache so next check reflects the new version
                    TryBustUpdateCache();
                    ShowMessage($"aria2-next 已更新到 {update.Version}，重启 aria2 后生效。", InfoBarSeverity.Success);
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
    }
}
