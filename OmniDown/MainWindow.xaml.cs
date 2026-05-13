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
using OmniDown.Services.Downloads;
using OmniDown.Services.Engine;
using OmniDown.Services.Localization;
using OmniDown.Services.Notifications;
using OmniDown.Services.Rpc;
using OmniDown.Services.Settings;
using OmniDown.Services.Shell;
using OmniDown.Services.Storage;
using OmniDown.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
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

        private const string CloneCommand = "git clone https://github.com/JordanQD/OmniDown.git";
        private readonly Aria2EngineHost _aria2EngineHost = new();
        private readonly Aria2RpcClient _aria2RpcClient = new();
        private readonly DownloadCoordinator _downloadCoordinator;
        private readonly SystemNotificationService _notifications;
        private readonly TaskbarProgressService _taskbarProgress;
        private TrayIconService? _trayIcon;
        private readonly DispatcherTimer _refreshTimer = new();
        private readonly DispatcherTimer _statusMessageTimer = new();
        private readonly string _rpcSecret = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
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
        private bool _isNewDownloadDialogOpen;
        private bool _hasTriggeredAutoShutdown;
        private bool _hasSeenActiveDownloadsForAutoShutdown;
        private bool _isShutdownPrepared;

        public ObservableCollection<DownloadTask> Tasks { get; } = new();

        public MainWindow()
        {
            _settingsPageViewModel = new SettingsPageViewModel(_settingsStore);
            InitializeComponent();
            HookSettingsPageEvents();
            TasksListView.ItemsSource = _visibleTasks;
            NotificationHistoryListView.ItemsSource = _statusMessages;
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

            SettingsSectionListView.SelectedIndex = 0;
            ShowSettingsSection("General");
            InitializeAboutSection();

            LoadDownloadSettings();
            LoadBitTorrentSettings();
            LoadNetworkSettings();
            _ = AutoSyncBitTorrentTrackersIfNeededAsync();
            LoadSpeedLimitSettings();
            LoadCloseBehaviorSettings();
            UpdateSearchPlaceholder();
            UpdateDownloadsHeader("Home");
            UpdateStatsVisibility("Home");
            ApplyTaskFilter("Home");
            ApplySettingsFilter();
            SetTaskListLoading(true);
            UpdateDashboard();
            UpdateGlobalSpeeds(0, 0);
            UpdateGlobalSpeedLimitText();
            UpdateAriaStatus();
            UpdateDebugStatus();
            UpdateTaskDetailsPane();
            ApplyToolbarTooltips();
        }
    }
}
