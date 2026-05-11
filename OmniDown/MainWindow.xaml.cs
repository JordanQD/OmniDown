using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using OmniDown.Models;
using OmniDown.Services.Downloads;
using OmniDown.Services.Engine;
using OmniDown.Services.Localization;
using OmniDown.Services.Notifications;
using OmniDown.Services.Rpc;
using OmniDown.Services.Shell;
using OmniDown.Services.Storage;
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
        private SelectorBar? _taskDetailsSelectorBar;
        private SelectorBarItem? _taskDetailsSummaryItem;
        private long _downloadLimitBytesPerSecond;
        private long _uploadLimitBytesPerSecond;
        private readonly Dictionary<string, string> _observedTaskStatuses = new(StringComparer.OrdinalIgnoreCase);
        private readonly string _speedLimitSettingsPath = Path.Combine(AppPaths.LocalDataDirectory, "speed-limits.json");
        private readonly string _closeBehaviorSettingsPath = Path.Combine(AppPaths.LocalDataDirectory, "close-behavior.json");
        private readonly nint _windowHandle;
        private CloseBehaviorSettings _closeBehaviorSettings = CloseBehaviorSettings.Default;
        private bool _isExitRequested;
        private bool _isClosePromptOpen;
        private bool _isLoadingCloseBehaviorSettings;
        private bool _isNewDownloadDialogOpen;

        public ObservableCollection<DownloadTask> Tasks { get; } = new();

        public MainWindow()
        {
            InitializeComponent();
            TasksListView.ItemsSource = _visibleTasks;
            NotificationHistoryListView.ItemsSource = _statusMessages;
            InitializeTaskDetailsSelectorBar();
            _windowHandle = WindowNative.GetWindowHandle(this);
            SetWindowIcon();
            ConfigureDefaultWindowSize();
            _taskbarProgress = new TaskbarProgressService(_windowHandle);
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
            AppWindow.Closing += MainWindow_Closing;
            _downloadCoordinator = new DownloadCoordinator(_aria2RpcClient, Tasks);
            _notifications = ((App)Application.Current).Notifications;
            RecordObservedTaskStatuses();
            Closed += MainWindow_Closed;
            _refreshTimer.Interval = TimeSpan.FromSeconds(2);
            _refreshTimer.Tick += RefreshTimer_Tick;
            _statusMessageTimer.Interval = TimeSpan.FromSeconds(3);
            _statusMessageTimer.Tick += StatusMessageTimer_Tick;

            SettingsSectionListView.SelectedIndex = 0;
            ShowSettingsSection("General");
            InitializeAboutSection();
            DownloadDirectoryTextBox.Text = AppPaths.DefaultDownloadDirectory;

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
            ShowTaskDetailsSection("Summary");
            UpdateTaskDetailsPane();
            ApplyToolbarTooltips();
        }

        private async void NewDownloadButton_Click(object sender, RoutedEventArgs e)
        {
            await ShowNewDownloadDialogAsync();
        }

        private void NewDownloadKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            _ = ShowNewDownloadDialogAsync();
        }

        private async Task ShowNewDownloadDialogAsync()
        {
            if (_isNewDownloadDialogOpen)
            {
                return;
            }

            _isNewDownloadDialogOpen = true;
            try
            {
            TextBox uriTextBox = new()
            {
                Header = "Download URL",
                PlaceholderText = "https://example.com/file.zip",
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                Height = 64,
                MaxHeight = 220
            };
            uriTextBox.Header = Strings.Get("NewDownloadUrlHeader");
            uriTextBox.PlaceholderText = Strings.Get("NewDownloadUrlPlaceholder");
            ScrollViewer.SetVerticalScrollBarVisibility(uriTextBox, ScrollBarVisibility.Auto);
            uriTextBox.TextChanged += (_, _) => UpdateUriTextBoxHeight(uriTextBox);
            uriTextBox.SizeChanged += (_, _) => UpdateUriTextBoxHeight(uriTextBox);
            KeyboardAccelerator pasteUriAccelerator = new()
            {
                Key = VirtualKey.V,
                Modifiers = VirtualKeyModifiers.Control
            };
            pasteUriAccelerator.Invoked += async (_, args) =>
            {
                args.Handled = true;
                await PasteClipboardTextAsync(uriTextBox);
            };
            uriTextBox.KeyboardAccelerators.Add(pasteUriAccelerator);

            Button pasteUriButton = new()
            {
                Content = new FontIcon
                {
                    Glyph = "\uE77F",
                    FontSize = 16,
                    Width = 16,
                    Height = 16
                },
                Width = 40,
                Height = 40,
                MinWidth = 40,
                MinHeight = 40,
                Padding = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(8, 0, 0, 0)
            };
            ToolTipService.SetToolTip(pasteUriButton, Strings.Get("PasteButtonText"));
            pasteUriButton.Click += async (_, _) => await PasteClipboardTextAsync(uriTextBox);

            Grid uriInputRow = new()
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                Children =
                {
                    uriTextBox,
                    pasteUriButton
                }
            };
            Grid.SetColumn(pasteUriButton, 1);

            TorrentSelection? torrentSelection = null;
            ObservableCollection<TorrentFileEntry> torrentFiles = [];
            StackPanel torrentRowsPanel = new()
            {
                Spacing = 0
            };
            TextBlock torrentFileNameText = new()
            {
                Text = Strings.Get("TorrentNoFileSelectedText"),
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = GetThemeBrush("TextFillColorSecondaryBrush", Colors.Gray)
            };
            Border torrentTag = CreateTorrentTag();
            Button clearTorrentButton = CreateIconButton("\uE711", Strings.Get("ClearTorrentFileButtonText"));
            Button openTorrentButton = CreateIconButton("\uE8E5", Strings.Get("OpenTorrentFileButtonText"));
            Grid torrentInputRow = CreateTorrentInputRow(torrentFileNameText, torrentTag, openTorrentButton, clearTorrentButton);
            torrentInputRow.Visibility = Visibility.Collapsed;
            torrentTag.Visibility = Visibility.Collapsed;
            clearTorrentButton.Visibility = Visibility.Collapsed;

            TextBlock torrentSummaryText = new()
            {
                Text = Strings.Get("TorrentNoFileSelectedText"),
                Foreground = GetThemeBrush("TextFillColorSecondaryBrush", Colors.Gray),
                Visibility = Visibility.Collapsed
            };
            CheckBox selectAllTorrentFilesCheckBox = new()
            {
                IsChecked = true,
                VerticalAlignment = VerticalAlignment.Center
            };
            Action updateTorrentPanelVisibility = () => { };
            Action<TorrentSelection> setTorrentSelection = selected =>
            {
                torrentSelection = selected;
                torrentFiles.Clear();
                foreach (TorrentFileEntry file in selected.Metadata.Files)
                {
                    torrentFiles.Add(file);
                }

                torrentFileNameText.Text = selected.DisplayName;
                torrentFileNameText.Foreground = GetThemeBrush("TextFillColorPrimaryBrush", Colors.Black);
                RenderTorrentRows(torrentRowsPanel, torrentFiles, selectAllTorrentFilesCheckBox);
                updateTorrentPanelVisibility();
            };

            openTorrentButton.Click += async (_, _) =>
            {
                TorrentSelection? selected = await PickTorrentFileAsync();
                if (selected is null)
                {
                    return;
                }

                setTorrentSelection(selected);
            };
            clearTorrentButton.Click += (_, _) =>
            {
                torrentSelection = null;
                torrentFiles.Clear();
                torrentFileNameText.Text = Strings.Get("TorrentNoFileSelectedText");
                torrentFileNameText.Foreground = GetThemeBrush("TextFillColorSecondaryBrush", Colors.Gray);
                RenderTorrentRows(torrentRowsPanel, torrentFiles, selectAllTorrentFilesCheckBox);
                updateTorrentPanelVisibility();
            };

            selectAllTorrentFilesCheckBox.Click += (_, _) =>
            {
                bool isSelected = selectAllTorrentFilesCheckBox.IsChecked == true;
                foreach (TorrentFileEntry file in torrentFiles)
                {
                    file.IsSelected = isSelected;
                }

                RenderTorrentRows(torrentRowsPanel, torrentFiles, selectAllTorrentFilesCheckBox);
            };

            Grid torrentHeader = new()
            {
                Height = 36,
                Padding = new Thickness(8, 0, 8, 0),
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(40) },
                    new ColumnDefinition { Width = new GridLength(54) },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(96) }
                },
                Children =
                {
                    selectAllTorrentFilesCheckBox,
                    CreateHeaderText(Strings.Get("TorrentFileIndexHeader")),
                    CreateHeaderText(Strings.Get("TorrentFileNameHeader")),
                    CreateHeaderText(Strings.Get("TorrentFileSizeHeader"))
                }
            };
            Grid.SetColumn(torrentHeader.Children[1] as FrameworkElement, 1);
            Grid.SetColumn(torrentHeader.Children[2] as FrameworkElement, 2);
            Grid.SetColumn(torrentHeader.Children[3] as FrameworkElement, 3);

            Border torrentFilesBorder = new()
            {
                BorderBrush = GetThemeBrush("ControlStrokeColorDefaultBrush", Colors.Gray),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Visibility = Visibility.Collapsed,
                Child = new StackPanel
                {
                    Children =
                    {
                        torrentHeader,
                        new ScrollViewer
                        {
                            MaxHeight = 220,
                            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                            Content = torrentRowsPanel
                        }
                    }
                }
            };

            bool isTorrentMode = false;
            Button linkTaskTypeButton = CreateTaskTypeButton(Strings.Get("LinkTaskTabHeader"));
            Button torrentTaskTypeButton = CreateTaskTypeButton(Strings.Get("TorrentTaskTabHeader"));
            Border linkTaskTypeIndicator = CreateTaskTypeIndicator();
            Border torrentTaskTypeIndicator = CreateTaskTypeIndicator();
            Action<bool> setTaskMode = _ => { };
            linkTaskTypeButton.Click += (_, _) => setTaskMode(false);
            torrentTaskTypeButton.Click += (_, _) => setTaskMode(true);

            Grid taskTypeSelector = new()
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = new GridLength(3) }
                },
                Children =
                {
                    linkTaskTypeButton,
                    torrentTaskTypeButton,
                    linkTaskTypeIndicator,
                    torrentTaskTypeIndicator
                }
            };
            Grid.SetColumn(torrentTaskTypeButton, 1);
            Grid.SetRow(linkTaskTypeIndicator, 1);
            Grid.SetRow(torrentTaskTypeIndicator, 1);
            Grid.SetColumn(torrentTaskTypeIndicator, 1);

            StackPanel selectorHeader = new()
            {
                Spacing = 12,
                Children =
                {
                    new TextBlock
                    {
                        Text = Strings.Get("NewDownloadDialogTitle"),
                        FontSize = 24,
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                    },
                    taskTypeSelector
                }
            };

            TextBox fileNameTextBox = new()
            {
                PlaceholderText = "Leave empty to infer from the URL"
            };
            fileNameTextBox.Header = Strings.Get("NewDownloadTaskNameHeader");
            fileNameTextBox.PlaceholderText = Strings.Get("NewDownloadTaskNamePlaceholder");

            TextBox directoryTextBox = new()
            {
                Text = DownloadDirectoryTextBox.Text
            };
            directoryTextBox.Header = Strings.Get("NewDownloadDirectoryHeader");

            NumberBox splitCountNumberBox = new()
            {
                Header = Strings.Get("NewDownloadSplitCountHeader"),
                Value = 16,
                Minimum = 1,
                Maximum = 128,
                SmallChange = 1,
                LargeChange = 8,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact
            };

            StackPanel content = new()
            {
                Width = 680,
                MaxWidth = 760,
                Spacing = 12,
                Children =
                {
                    selectorHeader,
                    uriInputRow,
                    torrentInputRow,
                    torrentSummaryText,
                    torrentFilesBorder,
                    fileNameTextBox,
                    directoryTextBox,
                    splitCountNumberBox
                }
            };

            updateTorrentPanelVisibility = () =>
            {
                bool hasTorrent = torrentSelection is not null;
                bool isTorrentSelected = isTorrentMode;
                torrentFilesBorder.Visibility = isTorrentSelected && hasTorrent ? Visibility.Visible : Visibility.Collapsed;
                torrentTag.Visibility = hasTorrent ? Visibility.Visible : Visibility.Collapsed;
                openTorrentButton.Visibility = hasTorrent ? Visibility.Collapsed : Visibility.Visible;
                clearTorrentButton.Visibility = hasTorrent ? Visibility.Visible : Visibility.Collapsed;
                torrentSummaryText.Text = hasTorrent
                    ? Strings.Format("TorrentFileCountText", torrentFiles.Count)
                    : Strings.Get("TorrentNoFileSelectedText");
                torrentSummaryText.Visibility = isTorrentSelected && hasTorrent ? Visibility.Visible : Visibility.Collapsed;
            };
            setTaskMode = isTorrentSelected =>
            {
                isTorrentMode = isTorrentSelected;
                uriInputRow.Visibility = isTorrentSelected ? Visibility.Collapsed : Visibility.Visible;
                torrentInputRow.Visibility = isTorrentSelected ? Visibility.Visible : Visibility.Collapsed;
                linkTaskTypeIndicator.Visibility = isTorrentSelected ? Visibility.Collapsed : Visibility.Visible;
                torrentTaskTypeIndicator.Visibility = isTorrentSelected ? Visibility.Visible : Visibility.Collapsed;
                linkTaskTypeButton.Foreground = GetThemeBrush(
                    isTorrentSelected ? "TextFillColorSecondaryBrush" : "TextFillColorPrimaryBrush",
                    isTorrentSelected ? Colors.Gray : Colors.Black);
                torrentTaskTypeButton.Foreground = GetThemeBrush(
                    isTorrentSelected ? "TextFillColorPrimaryBrush" : "TextFillColorSecondaryBrush",
                    isTorrentSelected ? Colors.Black : Colors.Gray);
                updateTorrentPanelVisibility();
            };
            setTaskMode(false);
            string? clipboardDownloadText = await GetClipboardDownloadTextAsync();
            if (clipboardDownloadText is not null)
            {
                uriTextBox.Text = clipboardDownloadText;
                uriTextBox.SelectionStart = uriTextBox.Text.Length;
                uriTextBox.SelectionLength = 0;
            }

            Border dropOverlay = new()
            {
                Visibility = Visibility.Collapsed,
                Background = new SolidColorBrush(Colors.Black)
                {
                    Opacity = 0.82
                },
                CornerRadius = new CornerRadius(8),
                Child = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Spacing = 10,
                    Children =
                    {
                        new FontIcon
                        {
                            Glyph = "\uE8E5",
                            FontSize = 28
                        },
                        new TextBlock
                        {
                            Text = Strings.Get("TorrentDropOverlayText"),
                            FontSize = 16,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Foreground = new SolidColorBrush(Colors.White)
                        }
                    }
                }
            };

            bool submitRequestedByEnter = false;
            ContentDialog? dialog = null;
            Grid dialogContent = new()
            {
                Width = 680,
                MaxWidth = 760,
                AllowDrop = true,
                IsTabStop = true,
                Children =
                {
                    content,
                    dropOverlay
                }
            };
            dialogContent.AddHandler(
                UIElement.KeyDownEvent,
                new KeyEventHandler((_, args) =>
                {
                    if (args.Key != VirtualKey.Enter)
                    {
                        return;
                    }

                    submitRequestedByEnter = true;
                    args.Handled = true;
                    dialog?.Hide();
                }),
                true);
            dialogContent.Tapped += (_, args) =>
            {
                if (args.OriginalSource is DependencyObject source &&
                    FindAncestor<Control>(source) is null)
                {
                    _ = dialogContent.Focus(FocusState.Programmatic);
                }
            };
            dialogContent.DragOver += (_, args) =>
            {
                if (args.DataView.Contains(StandardDataFormats.StorageItems))
                {
                    args.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
                    dropOverlay.Visibility = Visibility.Visible;
                }
            };
            dialogContent.DragLeave += (_, _) =>
            {
                dropOverlay.Visibility = Visibility.Collapsed;
            };
            dialogContent.Drop += async (_, args) =>
            {
                dropOverlay.Visibility = Visibility.Collapsed;
                if (!args.DataView.Contains(StandardDataFormats.StorageItems))
                {
                    return;
                }

                IReadOnlyList<IStorageItem> items = await args.DataView.GetStorageItemsAsync();
                StorageFile? file = items
                    .OfType<StorageFile>()
                    .FirstOrDefault(item => item.FileType.Equals(".torrent", StringComparison.OrdinalIgnoreCase));
                if (file is null)
                {
                    return;
                }

                setTaskMode(true);
                setTorrentSelection(await LoadTorrentFileAsync(file));
            };

            dialog = new ContentDialog
            {
                XamlRoot = Content.XamlRoot,
                Content = dialogContent,
                PrimaryButtonText = Strings.Get("AddButtonText"),
                CloseButtonText = Strings.Get("CancelButtonText"),
                DefaultButton = ContentDialogButton.Primary
            };
            dialog.Opened += (_, _) =>
            {
                _ = uriTextBox.Focus(FocusState.Programmatic);
            };
            dialog.Resources["ContentDialogMaxWidth"] = 820d;

            ContentDialogResult result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary && !submitRequestedByEnter)
            {
                return;
            }

            bool isTorrentTask = isTorrentMode;
            List<string> sourceUris = isTorrentTask ? [] : GetDownloadSourceUris(uriTextBox.Text);
            if (!isTorrentTask && sourceUris.Count == 0)
            {
                ShowMessage(Strings.Get("DownloadUrlRequiredMessage"), InfoBarSeverity.Warning);
                return;
            }

            if (isTorrentTask && torrentSelection is null)
            {
                ShowMessage(Strings.Get("TorrentFileRequiredMessage"), InfoBarSeverity.Warning);
                return;
            }

            List<int> selectedTorrentFileIndexes = isTorrentTask
                ? torrentFiles.Where(file => file.IsSelected).Select(file => file.Index).ToList()
                : [];
            if (isTorrentTask && selectedTorrentFileIndexes.Count == 0)
            {
                ShowMessage(Strings.Get("TorrentFileSelectionRequiredMessage"), InfoBarSeverity.Warning);
                return;
            }

            Aria2EngineStartResult startResult = await EnsureAria2StartedAsync();
            if (!startResult.Started)
            {
                ShowMessage(startResult.Message, InfoBarSeverity.Error);
                return;
            }

            string saveDirectory = string.IsNullOrWhiteSpace(directoryTextBox.Text)
                ? DownloadDirectoryTextBox.Text
                : directoryTextBox.Text.Trim();
            int splitCount = GetDownloadSplitCount(splitCountNumberBox);

            try
            {
                List<DownloadTask> addedTasks = [];
                if (isTorrentTask && torrentSelection is not null)
                {
                    IReadOnlyList<int> aria2Selection = selectedTorrentFileIndexes.Count == torrentFiles.Count
                        ? []
                        : selectedTorrentFileIndexes;
                    DownloadTask task = await _downloadCoordinator.AddTorrentAsync(
                        torrentSelection.Bytes,
                        torrentSelection.Path,
                        torrentSelection.Metadata,
                        saveDirectory,
                        splitCount,
                        aria2Selection);
                    _observedTaskStatuses[task.Gid] = task.Status;
                    addedTasks.Add(task);
                    _notifications.ShowTaskAdded(task);
                }
                else
                {
                    string requestedName = sourceUris.Count == 1 ? fileNameTextBox.Text : string.Empty;
                    foreach (string sourceUri in sourceUris)
                    {
                        DownloadTask task = await _downloadCoordinator.AddDownloadAsync(sourceUri, requestedName, saveDirectory, splitCount);
                        _observedTaskStatuses[task.Gid] = task.Status;
                        addedTasks.Add(task);
                        _notifications.ShowTaskAdded(task);
                    }
                }

                ShowMessage(
                    addedTasks.Count == 1
                        ? Strings.Get("TaskAddedMessage")
                        : Strings.Format("TasksAddedMessage", addedTasks.Count),
                    InfoBarSeverity.Success);
                await RefreshDownloadsAsync();
            }
            catch (Exception ex)
            {
                ShowMessage(Strings.Format("AddTaskFailedMessage", ex.Message), InfoBarSeverity.Error);
            }

            UpdateDashboard();
            }
            finally
            {
                _isNewDownloadDialogOpen = false;
            }
        }

        private async Task<TorrentSelection?> PickTorrentFileAsync()
        {
            FileOpenPicker picker = new()
            {
                SuggestedStartLocation = PickerLocationId.Downloads
            };
            picker.FileTypeFilter.Add(".torrent");
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));

            StorageFile? file = await picker.PickSingleFileAsync();
            if (file is null)
            {
                return null;
            }

            return await LoadTorrentFileAsync(file);
        }

        private static async Task<TorrentSelection> LoadTorrentFileAsync(StorageFile file)
        {
            byte[] bytes = await File.ReadAllBytesAsync(file.Path);
            TorrentMetadata metadata = TorrentMetadataReader.Read(bytes);
            string displayName = string.IsNullOrWhiteSpace(metadata.Name)
                ? file.Name
                : metadata.Name;
            return new TorrentSelection(file.Path, displayName, bytes, metadata);
        }

        private static Border CreateSelectedTorrentPanel(TextBlock nameText, Func<Task> removeAction)
        {
            Button removeButton = new()
            {
                Content = new FontIcon
                {
                    Glyph = "\uE711",
                    FontSize = 12
                },
                Width = 32,
                Height = 32,
                MinWidth = 32,
                Padding = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Center
            };
            removeButton.Click += async (_, _) => await removeAction();

            Grid row = new()
            {
                Padding = new Thickness(12, 8, 8, 8),
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                Children =
                {
                    nameText,
                    new Border
                    {
                        Background = GetThemeBrush("AccentFillColorDefaultBrush", Colors.DodgerBlue),
                        CornerRadius = new CornerRadius(3),
                        Padding = new Thickness(8, 3, 8, 3),
                        Margin = new Thickness(8, 0, 8, 0),
                        VerticalAlignment = VerticalAlignment.Center,
                        Child = new TextBlock
                        {
                            Text = "Torrent",
                            FontSize = 12,
                            Foreground = GetThemeBrush("TextOnAccentFillColorPrimaryBrush", Colors.White)
                        }
                    },
                    removeButton
                }
            };
            Grid.SetColumn((FrameworkElement)row.Children[1], 1);
            Grid.SetColumn((FrameworkElement)row.Children[2], 2);

            return new Border
            {
                Background = GetThemeBrush("CardBackgroundFillColorSecondaryBrush", Colors.Transparent),
                CornerRadius = new CornerRadius(6),
                Child = row
            };
        }

        private static StackPanel CreateIconText(string glyph, string text)
        {
            return new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                Children =
                {
                    new FontIcon
                    {
                        Glyph = glyph,
                        FontSize = 16,
                        Width = 16,
                        Height = 16
                    },
                    new TextBlock
                    {
                        Text = text,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                }
            };
        }

        private static TextBlock CreateHeaderText(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
        }

        private static Button CreateTaskTypeButton(string text)
        {
            return new Button
            {
                Content = text,
                Background = new SolidColorBrush(Colors.Transparent),
                BorderBrush = new SolidColorBrush(Colors.Transparent),
                Padding = new Thickness(16, 8, 16, 8),
                MinWidth = 96,
                HorizontalAlignment = HorizontalAlignment.Left
            };
        }

        private static Grid CreateTorrentInputRow(
            TextBlock fileNameText,
            Border torrentTag,
            Button openButton,
            Button clearButton)
        {
            Grid row = new()
            {
                Height = 48,
                Padding = new Thickness(12, 0, 6, 0),
                Background = GetThemeBrush("CardBackgroundFillColorSecondaryBrush", Colors.Transparent),
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                Children =
                {
                    fileNameText,
                    torrentTag,
                    openButton,
                    clearButton
                }
            };
            Grid.SetColumn(torrentTag, 1);
            Grid.SetColumn(openButton, 2);
            Grid.SetColumn(clearButton, 3);
            return row;
        }

        private static Border CreateTorrentTag()
        {
            return new Border
            {
                Background = GetThemeBrush("AccentFillColorDefaultBrush", Colors.DodgerBlue),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(8, 3, 8, 3),
                Margin = new Thickness(8, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = "Torrent",
                    FontSize = 12,
                    Foreground = GetThemeBrush("TextOnAccentFillColorPrimaryBrush", Colors.White)
                }
            };
        }

        private static Button CreateIconButton(string glyph, string tooltip)
        {
            Button button = new()
            {
                Content = new FontIcon
                {
                    Glyph = glyph,
                    FontSize = 16,
                    Width = 16,
                    Height = 16
                },
                Width = 40,
                Height = 40,
                MinWidth = 40,
                MinHeight = 40,
                Padding = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Center
            };
            ToolTipService.SetToolTip(button, tooltip);
            return button;
        }

        private static Border CreateTaskTypeIndicator()
        {
            return new Border
            {
                Height = 3,
                Margin = new Thickness(12, 0, 12, 0),
                Background = GetThemeBrush("AccentFillColorDefaultBrush", Colors.DodgerBlue),
                CornerRadius = new CornerRadius(2),
                VerticalAlignment = VerticalAlignment.Bottom
            };
        }

        private static Brush GetThemeBrush(string key, Color fallback)
        {
            return Application.Current.Resources.TryGetValue(key, out object value) && value is Brush brush
                ? brush
                : new SolidColorBrush(fallback);
        }

        private static void RenderTorrentRows(
            StackPanel rowsPanel,
            IEnumerable<TorrentFileEntry> files,
            CheckBox selectAllCheckBox)
        {
            rowsPanel.Children.Clear();
            List<TorrentFileEntry> fileList = files.ToList();
            selectAllCheckBox.IsChecked = fileList.Count == 0
                ? false
                : fileList.All(file => file.IsSelected)
                    ? true
                    : fileList.Any(file => file.IsSelected)
                        ? null
                        : false;

            foreach (TorrentFileEntry file in fileList)
            {
                CheckBox checkBox = new()
                {
                    IsChecked = file.IsSelected,
                    VerticalAlignment = VerticalAlignment.Center
                };
                checkBox.Click += (_, _) =>
                {
                    file.IsSelected = checkBox.IsChecked == true;
                    RenderTorrentRows(rowsPanel, fileList, selectAllCheckBox);
                };

                Grid row = new()
                {
                    MinHeight = 38,
                    Padding = new Thickness(8, 0, 8, 0),
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = new GridLength(40) },
                        new ColumnDefinition { Width = new GridLength(54) },
                        new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                        new ColumnDefinition { Width = new GridLength(96) }
                    },
                    Children =
                    {
                        checkBox,
                        new TextBlock
                        {
                            Text = file.Index.ToString(CultureInfo.InvariantCulture),
                            VerticalAlignment = VerticalAlignment.Center
                        },
                        new TextBlock
                        {
                            Text = file.Path,
                            TextTrimming = TextTrimming.CharacterEllipsis,
                            VerticalAlignment = VerticalAlignment.Center
                        },
                        new TextBlock
                        {
                            Text = file.SizeText,
                            HorizontalAlignment = HorizontalAlignment.Right,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    }
                };
                Grid.SetColumn((FrameworkElement)row.Children[1], 1);
                Grid.SetColumn((FrameworkElement)row.Children[2], 2);
                Grid.SetColumn((FrameworkElement)row.Children[3], 3);
                rowsPanel.Children.Add(new Border
                {
                    BorderBrush = GetThemeBrush("ControlStrokeColorDefaultBrush", Colors.Gray),
                    BorderThickness = new Thickness(0, 1, 0, 0),
                    Child = row
                });
            }
        }

        private static void UpdateUriTextBoxHeight(TextBox textBox)
        {
            const double singleLineHeight = 64;
            const double lineHeight = 22;
            const double maxHeight = 220;
            const double horizontalPadding = 28;
            const double averageCharacterWidth = 7.4;

            double textWidth = textBox.ActualWidth > 0
                ? Math.Max(160, textBox.ActualWidth - horizontalPadding)
                : 560;
            int charactersPerLine = Math.Max(20, (int)Math.Floor(textWidth / averageCharacterWidth));
            int visualLineCount = 0;

            string normalizedText = (textBox.Text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
            foreach (string line in normalizedText.Split('\n'))
            {
                visualLineCount += Math.Max(1, (int)Math.Ceiling(Math.Max(1, line.Length) / (double)charactersPerLine));
            }

            double desiredHeight = singleLineHeight + (Math.Max(1, visualLineCount) - 1) * lineHeight;
            textBox.Height = Math.Clamp(desiredHeight, singleLineHeight, maxHeight);
        }

        private static List<string> GetDownloadSourceUris(string text)
        {
            return text
                .Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static async Task<string?> GetClipboardDownloadTextAsync()
        {
            try
            {
                DataPackageView content = Clipboard.GetContent();
                if (!content.Contains(StandardDataFormats.Text))
                {
                    return null;
                }

                string clipboardText = await content.GetTextAsync();
                List<string> sourceUris = GetDownloadSourceUris(clipboardText)
                    .Where(IsLikelyDownloadSourceUri)
                    .ToList();
                return sourceUris.Count == 0
                    ? null
                    : EnsureTrailingLineBreak(string.Join(Environment.NewLine, sourceUris));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Read clipboard download URL failed: {ex}");
                return null;
            }
        }

        private static async Task PasteClipboardTextAsync(TextBox textBox)
        {
            try
            {
                DataPackageView content = Clipboard.GetContent();
                if (!content.Contains(StandardDataFormats.Text))
                {
                    return;
                }

                string clipboardText = EnsureTrailingLineBreak((await content.GetTextAsync()).Trim());
                if (string.IsNullOrWhiteSpace(clipboardText))
                {
                    return;
                }

                string currentText = textBox.Text ?? string.Empty;
                int selectionStart = Math.Clamp(textBox.SelectionStart, 0, currentText.Length);
                int selectionLength = Math.Clamp(textBox.SelectionLength, 0, currentText.Length - selectionStart);
                string prefix = currentText[..selectionStart];
                string suffix = currentText[(selectionStart + selectionLength)..];

                if (!string.IsNullOrWhiteSpace(prefix) && !EndsWithLineBreak(prefix))
                {
                    clipboardText = Environment.NewLine + clipboardText;
                }

                textBox.Text = prefix + clipboardText + suffix;
                textBox.SelectionStart = (prefix + clipboardText).Length;
                textBox.SelectionLength = 0;
                textBox.Focus(FocusState.Programmatic);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Paste download URL failed: {ex}");
            }
        }

        private static bool IsLikelyDownloadSourceUri(string text)
        {
            if (text.StartsWith("magnet:", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!Uri.TryCreate(text, UriKind.Absolute, out Uri? uri))
            {
                return false;
            }

            return uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
                uri.Scheme.Equals(Uri.UriSchemeFtp, StringComparison.OrdinalIgnoreCase) ||
                uri.Scheme.Equals("sftp", StringComparison.OrdinalIgnoreCase);
        }

        private static string EnsureTrailingLineBreak(string text)
        {
            return string.IsNullOrEmpty(text) || EndsWithLineBreak(text)
                ? text
                : text + Environment.NewLine;
        }

        private static bool EndsWithLineBreak(string text)
        {
            return text.EndsWith('\r') || text.EndsWith('\n');
        }

        private static int GetDownloadSplitCount(NumberBox numberBox)
        {
            if (double.IsNaN(numberBox.Value))
            {
                return 1;
            }

            return Math.Clamp((int)Math.Round(numberBox.Value), 1, 128);
        }

        private async void StartAriaButton_Click(object sender, RoutedEventArgs e)
        {
            Aria2EngineStartResult result = await EnsureAria2StartedAsync();
            UpdateAriaStatus();
            ShowMessage(result.Message, result.Started ? InfoBarSeverity.Success : InfoBarSeverity.Error);
        }

        private async void StopAriaButton_Click(object sender, RoutedEventArgs e)
        {
            _refreshTimer.Stop();
            await SaveAriaSessionIfRunningAsync();
            _aria2EngineHost.Stop();
            UpdateGlobalSpeeds(0, 0);
            _taskbarProgress.Clear();
            UpdateGlobalSpeedLimitText();
            UpdateAriaStatus();
            ShowMessage(Strings.Get("AriaStoppedMessage"), InfoBarSeverity.Informational);
        }

        private void TrayIcon_ShowRequested(object? sender, EventArgs e)
        {
            DispatcherQueue.TryEnqueue(ShowFromTray);
        }

        private void TrayIcon_ExitRequested(object? sender, EventArgs e)
        {
            DispatcherQueue.TryEnqueue(RequestExit);
        }

        private void UpdateTrayIconLabels()
        {
            _trayIcon?.UpdateLabels(
                Strings.Get("TrayTooltipText"),
                Strings.Get("TrayShowMenuItemText"),
                Strings.Get("TrayExitMenuItemText"));
        }

        private void RootNavigation_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            string tag = GetNavigationTag(args);
            NavigateTo(tag);
        }

        private void RootNavigation_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            string tag = args.InvokedItemContainer?.Tag?.ToString() ?? string.Empty;
            NavigateTo(tag);
        }

        private void SettingsNavItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            NavigateTo("Settings");
            e.Handled = true;
        }

        private void NavigateTo(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                return;
            }

            _currentTaskFilter = tag;
            bool isSettings = tag == "Settings";
            TasksHeaderPanel.Visibility = isSettings ? Visibility.Collapsed : Visibility.Visible;
            SettingsPage.Visibility = isSettings ? Visibility.Visible : Visibility.Collapsed;
            TasksPage.Visibility = isSettings ? Visibility.Collapsed : Visibility.Visible;
            if (isSettings)
            {
                RootNavigation.SelectedItem = SettingsNavItem;
                TaskDetailsPane.Visibility = Visibility.Collapsed;
                ClearTitleSearchBox();
                UpdateSearchPlaceholder();
                ShowSettingsPage();
                return;
            }

            UpdateTaskDetailsPaneVisibility();
            UpdateSearchPlaceholder();
            UpdateDownloadsHeader(tag);
            UpdateStatsVisibility(tag);
            ApplyTaskFilter(tag);
            UpdateDashboard();
            ApplySettingsFilter();
        }

        private static string GetNavigationTag(NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItemContainer?.Tag?.ToString() is string containerTag &&
                !string.IsNullOrWhiteSpace(containerTag))
            {
                return containerTag;
            }

            if (args.SelectedItem is NavigationViewItem item &&
                item.Tag?.ToString() is string itemTag &&
                !string.IsNullOrWhiteSpace(itemTag))
            {
                return itemTag;
            }

            return string.Empty;
        }

        private void SettingsSectionListView_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            if (SettingsSectionListView.SelectedItem is not ListViewItem item)
            {
                return;
            }

            string tag = item.Tag?.ToString() ?? "General";
            ShowSettingsSection(tag);
            ApplySettingsFilter();
        }

        private void ShowSettingsPage()
        {
            try
            {
                ShowSettingsSection(GetSelectedSettingsSectionTag());
                ApplySettingsFilter();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Show settings page failed: {ex}");
                ShowSettingsSection("General");
            }
        }

        private void ShowSettingsSection(string tag)
        {
            if (GeneralSettingsContent is null
                || DownloadSettingsContent is null
                || BitTorrentSettingsContent is null
                || NetworkSettingsContent is null
                || AdvancedSettingsContent is null
                || AboutSettingsContent is null)
            {
                return;
            }

            GeneralSettingsContent.Visibility = tag == "General" ? Visibility.Visible : Visibility.Collapsed;
            DownloadSettingsContent.Visibility = tag == "Download" ? Visibility.Visible : Visibility.Collapsed;
            BitTorrentSettingsContent.Visibility = tag == "BitTorrent" ? Visibility.Visible : Visibility.Collapsed;
            NetworkSettingsContent.Visibility = tag == "Network" ? Visibility.Visible : Visibility.Collapsed;
            AdvancedSettingsContent.Visibility = tag == "Advanced" ? Visibility.Visible : Visibility.Collapsed;
            AboutSettingsContent.Visibility = tag == "About" ? Visibility.Visible : Visibility.Collapsed;
        }

        private void InitializeAboutSection()
        {
            AboutVersionText.Text = GetAppVersionText();
            AboutCloneCommandText.Text = CloneCommand;
        }

        private static string GetAppVersionText()
        {
            try
            {
                Windows.ApplicationModel.PackageVersion version = Windows.ApplicationModel.Package.Current.Id.Version;
                return $"{version.Major}.{version.Minor}.{version.Build}";
            }
            catch
            {
                Version? assemblyVersion = typeof(MainWindow).Assembly.GetName().Version;
                return assemblyVersion is null
                    ? "1.0.0"
                    : $"{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}";
            }
        }

        private void CopyCloneCommandButton_Click(object sender, RoutedEventArgs e)
        {
            DataPackage package = new()
            {
                RequestedOperation = DataPackageOperation.Copy
            };
            package.SetText(CloneCommand);
            Clipboard.SetContent(package);
            ShowMessage(Strings.Get("CloneCommandCopiedMessage"), InfoBarSeverity.Success);
        }

        private async void OpenAboutLinkButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element
                || element.Tag?.ToString() is not string url
                || !Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
            {
                return;
            }

            await Launcher.LaunchUriAsync(uri);
        }

        private void AppTitleBar_PaneToggleRequested(TitleBar sender, object args)
        {
            RootNavigation.IsPaneOpen = !RootNavigation.IsPaneOpen;
        }

        private void ApplyToolbarTooltips()
        {
            SetToolbarText(NewDownloadButton, Strings.Get("NewDownloadButton.Label"), "Ctrl+N");
            SetToolbarText(ResumeTasksButton, Strings.Get("ResumeTasksButton.Label"));
            SetToolbarText(PauseTasksButton, Strings.Get("PauseTasksButton.Label"));
            SetToolbarText(RecoverTasksButton, Strings.Get("RecoverTasksButton.Label"));
            SetToolbarText(OpenSelectedTaskFileButton, Strings.Get("OpenSelectedTaskFileButton.Label"));
            SetToolbarText(OpenSelectedTaskFolderButton, Strings.Get("OpenSelectedTaskFolderButton.Label"));
            SetToolbarText(CopySelectedTaskLinksButton, Strings.Get("CopySelectedTaskLinksButton.Label"));
            SetToolbarText(DeleteTasksButton, Strings.Get("DeleteTasksButton.Label"));
            SetToolbarText(ClearCompletedTasksButton, Strings.Get("ClearCompletedTasksButton.Label"));
            SetToolbarText(SortTasksButton, Strings.Get("SortTasksButton.Label"));
            SetToolbarText(TaskDetailsButton, Strings.Get("TaskDetailsButton.Label"));
        }

        private static void SetToolbarText(FrameworkElement element, string text, string? shortcut = null)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            string tooltip = string.IsNullOrWhiteSpace(shortcut)
                ? text
                : Strings.Format("ToolbarShortcutTooltipFormat", text, shortcut);
            ToolTipService.SetToolTip(element, tooltip);
            AutomationProperties.SetName(element, text);
        }

        private async void RootGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (_hasStartedInitialLoad)
            {
                return;
            }

            _hasStartedInitialLoad = true;
            EnsureTrayIconInitialized();
            SetTaskListLoading(true);
            await WaitForNextRenderAsync();
            await StartAriaOnLaunchAsync();
        }

        private void EnsureTrayIconInitialized()
        {
            if (_trayIcon is not null)
            {
                return;
            }

            _trayIcon = new TrayIconService(_windowHandle, ResolveAssetPath("Assets", "OmniDown.ico"));
            UpdateTrayIconLabels();
            _trayIcon.ShowRequested += TrayIcon_ShowRequested;
            _trayIcon.ExitRequested += TrayIcon_ExitRequested;
        }

        private string GetSelectedSettingsSectionTag()
        {
            return SettingsSectionListView?.SelectedItem is ListViewItem item
                ? item.Tag?.ToString() ?? "General"
                : "General";
        }

        private void ClearTitleSearchBox()
        {
            if (TitleSearchBox is not null && !string.IsNullOrEmpty(TitleSearchBox.Text))
            {
                TitleSearchBox.Text = string.Empty;
            }
        }

        private async System.Threading.Tasks.Task StartAriaOnLaunchAsync()
        {
            try
            {
                Aria2EngineStartResult result = await EnsureAria2StartedAsync();
                UpdateAriaStatus();
                if (!result.Started)
                {
                    ShowMessage(result.Message, InfoBarSeverity.Warning);
                }
            }
            finally
            {
                SetTaskListLoading(false);
            }
        }

        private static Task WaitForNextRenderAsync()
        {
            TaskCompletionSource completionSource = new();
            EventHandler<object>? renderingHandler = null;
            renderingHandler = (sender, args) =>
            {
                CompositionTarget.Rendering -= renderingHandler;
                completionSource.TrySetResult();
            };

            CompositionTarget.Rendering += renderingHandler;
            return completionSource.Task;
        }

        private async void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            _refreshTimer.Stop();
            _taskbarProgress.Clear();
            SaveSpeedLimitSettings();
            SaveCloseBehaviorSettings();
            await SaveAriaSessionIfRunningAsync();
            _notifications.Unregister();
            _trayIcon?.Dispose();
            _aria2RpcClient.Dispose();
            _aria2EngineHost.Dispose();
        }

        private async void MainWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            if (_isExitRequested)
            {
                return;
            }

            args.Cancel = true;

            bool? minimizeToTray = _closeBehaviorSettings.MinimizeToTrayOnClose;
            if (minimizeToTray is null)
            {
                minimizeToTray = await AskCloseBehaviorAsync();
                if (minimizeToTray is null)
                {
                    return;
                }

                _closeBehaviorSettings = _closeBehaviorSettings with
                {
                    MinimizeToTrayOnClose = minimizeToTray
                };
                ApplyCloseBehaviorSettingsToUi();
                SaveCloseBehaviorSettings();
            }

            if (minimizeToTray.Value)
            {
                HideToTray();
            }
            else
            {
                RequestExit();
            }
        }

        private void ApplyTaskFilter(string tag)
        {
            string query = GetSearchQuery();
            HashSet<string> selectedGids = GetSelectedTasks()
                .Select(task => task.Gid)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            IEnumerable<DownloadTask> filteredTasks = tag switch
            {
                "Downloading" => Tasks.Where(task => IsDownloadingTask(task) && IsTaskSearchMatch(task, query)),
                "Completed" => Tasks.Where(task => IsCompletedTask(task) && IsTaskSearchMatch(task, query)),
                "Issues" => Tasks.Where(task => IsIssueTask(task) && IsTaskSearchMatch(task, query)),
                _ => Tasks.Where(task => IsTaskSearchMatch(task, query))
            };

            SyncVisibleTasks(SortTasks(filteredTasks).ToList());
            RestoreSelection(selectedGids);
            UpdateSelectionCommands();
            UpdateTaskDetailsPane();
        }

        private async void RefreshTimer_Tick(object? sender, object e)
        {
            await RefreshDownloadsAsync();
        }

        private async System.Threading.Tasks.Task<Aria2EngineStartResult> EnsureAria2StartedAsync()
        {
            int rpcPort = double.IsNaN(RpcPortNumberBox.Value) ? 6800 : (int)RpcPortNumberBox.Value;
            _aria2RpcClient.Configure(rpcPort, _rpcSecret);

            Aria2EngineStartResult result = await _aria2EngineHost.StartAsync(new Aria2EngineOptions(
                string.IsNullOrWhiteSpace(AriaPathTextBox.Text) ? null : AriaPathTextBox.Text.Trim(),
                rpcPort,
                DownloadDirectoryTextBox.Text.Trim(),
                _rpcSecret,
                UseSystemProxyCheckBox.IsOn));

            if (!result.Started)
            {
                return result;
            }

            try
            {
                await _aria2RpcClient.PingAsync();
                await ApplyConfiguredSpeedLimitsAsync();
                _refreshTimer.Start();
                await _downloadCoordinator.RemoveCompletedDownloadResultsAsync();
                await RefreshDownloadsAsync();
            }
            catch (Exception ex)
            {
                _aria2EngineHost.Stop();
                UpdateDebugStatus();
                return Aria2EngineStartResult.Failure($"aria2 started but RPC is not reachable: {ex.Message}");
            }

            return result;
        }

        private async System.Threading.Tasks.Task SaveAriaSessionIfRunningAsync()
        {
            if (!_aria2EngineHost.IsRunning)
            {
                return;
            }

            try
            {
                await _downloadCoordinator.RemoveCompletedDownloadResultsAsync();
                await _aria2RpcClient.SaveSessionAsync();
            }
            catch
            {
                // Best-effort: Stop/close must continue even if aria2 is no longer reachable.
            }
        }

        private async System.Threading.Tasks.Task RefreshDownloadsAsync()
        {
            if (_isRefreshing || !_aria2EngineHost.IsRunning)
            {
                if (!_aria2EngineHost.IsRunning)
                {
                    UpdateGlobalSpeeds(0, 0);
                    _taskbarProgress.Clear();
                }

                return;
            }

            _isRefreshing = true;
            try
            {
                DownloadSnapshot snapshot = await _downloadCoordinator.RefreshAsync();
                ShowTaskStatusNotifications();
                ApplyTaskFilter(_currentTaskFilter);
                UpdateDashboard();
                UpdateGlobalSpeeds(snapshot.DownloadSpeed, snapshot.UploadSpeed);
                UpdateTaskbarProgressFromTasks();
                UpdateAriaStatus();
            }
            catch (Exception ex)
            {
                UpdateDebugStatus();
                ShowMessage(Strings.Format("RpcRefreshFailedMessage", ex.Message), InfoBarSeverity.Warning);
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        private void TaskDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            _isTaskDetailsPaneOpen = !_isTaskDetailsPaneOpen;
            UpdateTaskDetailsPaneVisibility();
            UpdateTaskDetailsPane();
        }

        private void TaskDetailsCloseButton_Click(object sender, RoutedEventArgs e)
        {
            _isTaskDetailsPaneOpen = false;
            UpdateTaskDetailsPaneVisibility();
        }

        private void InitializeTaskDetailsSelectorBar()
        {
            _taskDetailsSelectorBar = new SelectorBar();
            _taskDetailsSummaryItem = CreateTaskDetailsSelectorItem("概要", "Summary");
            _taskDetailsSelectorBar.Items.Add(_taskDetailsSummaryItem);
            _taskDetailsSelectorBar.Items.Add(CreateTaskDetailsSelectorItem("活动", "Activity"));
            _taskDetailsSelectorBar.Items.Add(CreateTaskDetailsSelectorItem("文件", "Files"));
            _taskDetailsSelectorBar.Items.Add(CreateTaskDetailsSelectorItem("选项", "Options"));
            _taskDetailsSelectorBar.Items.Add(CreateTaskDetailsSelectorItem("节点", "Peers"));
            _taskDetailsSelectorBar.Items.Add(CreateTaskDetailsSelectorItem("追踪器", "Trackers"));
            _taskDetailsSelectorBar.SelectedItem = _taskDetailsSummaryItem;
            _taskDetailsSelectorBar.SelectionChanged += TaskDetailsSelectorBar_SelectionChanged;
            TaskDetailsSelectorHost.Children.Add(_taskDetailsSelectorBar);
        }

        private static SelectorBarItem CreateTaskDetailsSelectorItem(string text, string tag)
        {
            return new SelectorBarItem
            {
                Text = text,
                Tag = tag
            };
        }

        private void TaskDetailsSelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
        {
            string tag = sender.SelectedItem?.Tag?.ToString() ?? "Summary";
            ShowTaskDetailsSection(tag);
        }

        private void UpdateTaskDetailsPaneVisibility()
        {
            if (TaskDetailsPane is null)
            {
                return;
            }

            bool canShow = _isTaskDetailsPaneOpen && _currentTaskFilter != "Settings";
            TaskDetailsHostColumn.Width = canShow ? new GridLength(360) : new GridLength(0);
            TaskDetailsPane.Visibility = canShow ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateTaskDetailsPane()
        {
            if (TaskDetailsPane is null)
            {
                return;
            }

            List<DownloadTask> selectedTasks = GetSelectedTasks();
            if (selectedTasks.Count != 1)
            {
                TaskDetailsEmptyContent.Visibility = Visibility.Visible;
                TaskDetailsSummaryContent.Visibility = Visibility.Collapsed;
                TaskDetailsActivityContent.Visibility = Visibility.Collapsed;
                TaskDetailsFilesContent.Visibility = Visibility.Collapsed;
                TaskDetailsOptionsContent.Visibility = Visibility.Collapsed;
                TaskDetailsPeersContent.Visibility = Visibility.Collapsed;
                TaskDetailsTrackersContent.Visibility = Visibility.Collapsed;

                TaskDetailsEmptyTitleText.Text = selectedTasks.Count == 0 ? "未选择任务" : $"已选择 {selectedTasks.Count} 个任务";
                TaskDetailsEmptyMessageText.Text = selectedTasks.Count == 0
                    ? "选择一个任务以查看概要、活动、文件、选项、节点和追踪器。"
                    : "请选择单个任务以查看详细信息并继续编辑任务级选项。";
                return;
            }

            DownloadTask task = selectedTasks[0];
            TaskDetailsEmptyContent.Visibility = Visibility.Collapsed;

            TaskDetailsNameText.Text = string.IsNullOrWhiteSpace(task.Name) ? "未命名任务" : task.Name;
            TaskDetailsPathText.Text = string.IsNullOrWhiteSpace(ResolveTaskFilePath(task))
                ? task.SaveDirectory
                : ResolveTaskFilePath(task);
            TaskDetailsGidText.Text = string.IsNullOrWhiteSpace(task.Gid) ? "-" : task.Gid;
            TaskDetailsStatusText.Text = task.StatusText;
            TaskDetailsStatusText.Foreground = task.StatusBrush;
            TaskDetailsHeroIcon.Glyph = task.IsPeerTransfer ? "\uE968" : "\uE7C3";
            TaskDetailsHeroIcon.Foreground = task.StatusBrush;
            TaskDetailsSizeText.Text = task.SizeText;
            TaskDetailsProgressText.Text = task.ProgressText;
            TaskDetailsCreatedAtText.Text = task.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
            TaskDetailsSourceText.Text = string.IsNullOrWhiteSpace(task.SourceUri) ? "-" : task.SourceUri;

            TaskDetailsProgressBar.Value = Math.Clamp(task.Progress, 0, 100);
            TaskDetailsProgressBar.Foreground = task.ProgressBrush;
            TaskDetailsCompletedText.Text = task.TotalLength > 0
                ? $"{FormatBytesForDetails(task.CompletedLength)} / {FormatBytesForDetails(task.TotalLength)}"
                : FormatBytesForDetails(task.CompletedLength);
            TaskDetailsRemainingText.Text = task.RemainingTimeText;
            TaskDetailsDownloadSpeedText.Text = task.DownloadSpeedText;
            TaskDetailsUploadSpeedText.Text = task.UploadSpeedText;

            string selectedTag = _taskDetailsSelectorBar?.SelectedItem?.Tag?.ToString() ?? "Summary";
            ShowTaskDetailsSection(selectedTag);
        }

        private void ShowTaskDetailsSection(string tag)
        {
            if (TaskDetailsSummaryContent is null)
            {
                return;
            }

            bool hasSingleSelection = GetSelectedTasks().Count == 1;
            TaskDetailsSummaryContent.Visibility = hasSingleSelection && tag == "Summary" ? Visibility.Visible : Visibility.Collapsed;
            TaskDetailsActivityContent.Visibility = hasSingleSelection && tag == "Activity" ? Visibility.Visible : Visibility.Collapsed;
            TaskDetailsFilesContent.Visibility = hasSingleSelection && tag == "Files" ? Visibility.Visible : Visibility.Collapsed;
            TaskDetailsOptionsContent.Visibility = hasSingleSelection && tag == "Options" ? Visibility.Visible : Visibility.Collapsed;
            TaskDetailsPeersContent.Visibility = hasSingleSelection && tag == "Peers" ? Visibility.Visible : Visibility.Collapsed;
            TaskDetailsTrackersContent.Visibility = hasSingleSelection && tag == "Trackers" ? Visibility.Visible : Visibility.Collapsed;
        }

        private static string FormatBytesForDetails(long bytes)
        {
            string[] units = ["B", "KB", "MB", "GB", "TB"];
            double size = bytes;
            int unitIndex = 0;
            while (size >= 1024 && unitIndex < units.Length - 1)
            {
                size /= 1024;
                unitIndex++;
            }

            return $"{size:0.#} {units[unitIndex]}";
        }

        private void UpdateDashboard()
        {
            bool isTransferPage = _currentTaskFilter == "Downloading";
            TotalMetricLabelText.Text = isTransferPage ? Strings.Get("DownloadingPageTitle") : Strings.Get("TotalMetricLabel.Text");
            ActiveMetricLabelText.Text = Strings.Get("ActiveMetricLabel.Text");
            PausedMetricLabelText.Text = Strings.Get("PausedMetricLabel");
            CompletedMetricLabelText.Text = Strings.Get("DownloadSpeedMetricLabel.Text");
            IssueMetricLabelText.Text = Strings.Get("Aria2MetricLabel.Text");

            TotalTasksText.Text = (isTransferPage ? Tasks.Count(IsDownloadingTask) : Tasks.Count).ToString();
            ActiveTasksText.Text = Tasks.Count(IsActiveTransferTask).ToString();
            PausedTasksText.Text = Tasks.Count(IsPausedTask).ToString();
            CompletedTasksText.Text = Tasks.Count(IsCompletedTask).ToString();
            IssueTasksText.Text = Tasks.Count(IsIssueTask).ToString();
        }

        private void UpdateGlobalSpeeds(long downloadSpeed, long uploadSpeed)
        {
            if (GlobalDownloadSpeedText is not null)
            {
                GlobalDownloadSpeedText.Text = FormatSpeed(downloadSpeed);
            }

            if (GlobalUploadSpeedText is not null)
            {
                GlobalUploadSpeedText.Text = FormatSpeed(uploadSpeed);
            }
        }

        private void UpdateGlobalSpeedsFromTasks()
        {
            UpdateGlobalSpeeds(
                Tasks.Sum(task => task.DownloadSpeed),
                Tasks.Sum(task => task.UploadSpeed));
        }

        private void HideToTray()
        {
            _taskbarProgress.Clear();
            ShowWindow(_windowHandle, ShowWindowCommand.Hide);
        }

        private void ShowFromTray()
        {
            ShowWindow(_windowHandle, ShowWindowCommand.Show);
            ShowWindow(_windowHandle, ShowWindowCommand.Restore);
            SetForegroundWindow(_windowHandle);
        }

        private void RequestExit()
        {
            _isExitRequested = true;
            Close();
        }

        private async Task<bool?> AskCloseBehaviorAsync()
        {
            if (_isClosePromptOpen)
            {
                return null;
            }

            _isClosePromptOpen = true;
            try
            {
                ContentDialog dialog = new()
                {
                    XamlRoot = Content.XamlRoot,
                    Title = Strings.Get("CloseBehaviorDialogTitle"),
                    Content = Strings.Get("CloseBehaviorDialogContent"),
                    PrimaryButtonText = Strings.Get("CloseBehaviorMinimizeButtonText"),
                    SecondaryButtonText = Strings.Get("CloseBehaviorExitButtonText"),
                    CloseButtonText = Strings.Get("CancelButtonText"),
                    DefaultButton = ContentDialogButton.Primary
                };

                ContentDialogResult result = await dialog.ShowAsync();
                return result switch
                {
                    ContentDialogResult.Primary => true,
                    ContentDialogResult.Secondary => false,
                    _ => null
                };
            }
            finally
            {
                _isClosePromptOpen = false;
            }
        }

        private void UpdateTaskbarProgressFromTasks()
        {
            DownloadTask[] activeTasks = Tasks
                .Where(task => task.Status.Contains("download", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (activeTasks.Length == 0)
            {
                _taskbarProgress.Clear();
                return;
            }

            long totalLength = activeTasks
                .Where(task => task.TotalLength > 0)
                .Sum(task => task.TotalLength);
            long completedLength = activeTasks
                .Where(task => task.CompletedLength > 0)
                .Sum(task => task.CompletedLength);

            double progress = totalLength > 0
                ? Math.Clamp(completedLength / (double)totalLength, 0, 1)
                : 0;

            _taskbarProgress.SetProgress(progress);
        }

        private void UpdateGlobalSpeedLimitText()
        {
            bool showUploadLimit = _isUploadSpeedLimitEnabled && _uploadLimitBytesPerSecond > 0;
            if (GlobalUploadLimitIconPanel is not null)
            {
                GlobalUploadLimitIconPanel.Opacity = showUploadLimit ? 1 : 0;
            }

            if (GlobalUploadLimitText is not null)
            {
                GlobalUploadLimitText.Opacity = showUploadLimit ? 1 : 0;
                GlobalUploadLimitText.Text = showUploadLimit ? FormatSpeed(_uploadLimitBytesPerSecond) : string.Empty;
            }

            bool showDownloadLimit = _isDownloadSpeedLimitEnabled && _downloadLimitBytesPerSecond > 0;
            if (GlobalDownloadLimitIconPanel is not null)
            {
                GlobalDownloadLimitIconPanel.Opacity = showDownloadLimit ? 1 : 0;
            }

            if (GlobalDownloadLimitText is not null)
            {
                GlobalDownloadLimitText.Opacity = showDownloadLimit ? 1 : 0;
                GlobalDownloadLimitText.Text = showDownloadLimit ? FormatSpeed(_downloadLimitBytesPerSecond) : string.Empty;
            }
        }

        private void UpdateAriaStatus()
        {
            string status = _aria2EngineHost.IsRunning
                ? Strings.Format("AriaRunningStatus", _aria2EngineHost.ProcessId ?? 0)
                : Strings.Get("AriaStoppedStatus");

            SettingsAriaStatusText.Text = status;
            UpdateDebugStatus();
        }

        private void ShowMessage(string message, InfoBarSeverity severity)
        {
            _statusMessages.Insert(0, new AppStatusMessage(
                message,
                FormatStatusMessageDetail(DateTimeOffset.Now),
                GetSeverityText(severity),
                GetSeverityGlyph(severity),
                GetSeverityBrush(severity)));
            StatusToastMessageText.Text = message;
            StatusToastGlyph.Glyph = GetSeverityGlyph(severity);
            StatusToastGlyph.Foreground = GetSeverityBrush(severity);
            StatusToastPanel.BorderBrush = GetSeverityBrush(severity);
            StatusToastPanel.Visibility = Visibility.Visible;
            _statusMessageTimer.Stop();
            _statusMessageTimer.Start();
        }

        private void StatusMessageTimer_Tick(object? sender, object e)
        {
            _statusMessageTimer.Stop();
            StatusToastPanel.Visibility = Visibility.Collapsed;
        }

        private static string FormatStatusMessageDetail(DateTimeOffset timestamp)
        {
            return timestamp.ToString("HH:mm:ss", CultureInfo.CurrentCulture);
        }

        private static string GetSeverityText(InfoBarSeverity severity)
        {
            return severity switch
            {
                InfoBarSeverity.Success => Strings.Get("NotificationSeveritySuccess"),
                InfoBarSeverity.Warning => Strings.Get("NotificationSeverityWarning"),
                InfoBarSeverity.Error => Strings.Get("NotificationSeverityError"),
                _ => Strings.Get("NotificationSeverityInfo")
            };
        }

        private static string GetSeverityGlyph(InfoBarSeverity severity)
        {
            return severity switch
            {
                InfoBarSeverity.Success => "\uE73E",
                InfoBarSeverity.Warning => "\uE7BA",
                InfoBarSeverity.Error => "\uEA39",
                _ => "\uE946"
            };
        }

        private static Brush GetSeverityBrush(InfoBarSeverity severity)
        {
            return severity switch
            {
                InfoBarSeverity.Success => GetResourceBrush("SystemFillColorSuccessBrush", new SolidColorBrush(Colors.ForestGreen)),
                InfoBarSeverity.Warning => GetResourceBrush("SystemFillColorCautionBrush", new SolidColorBrush(Colors.Goldenrod)),
                InfoBarSeverity.Error => GetResourceBrush("SystemFillColorCriticalBrush", new SolidColorBrush(Colors.Firebrick)),
                _ => GetResourceBrush("AccentFillColorDefaultBrush", new SolidColorBrush(Colors.DodgerBlue))
            };
        }

        private static Brush GetResourceBrush(string key, Brush fallback)
        {
            return Application.Current.Resources.TryGetValue(key, out object value) && value is Brush brush
                ? brush
                : fallback;
        }

        private void RecordObservedTaskStatuses()
        {
            foreach (DownloadTask task in Tasks.Where(task => !string.IsNullOrWhiteSpace(task.Gid)))
            {
                _observedTaskStatuses[task.Gid] = task.Status;
            }
        }

        private void ShowTaskStatusNotifications()
        {
            HashSet<string> currentGids = new(StringComparer.OrdinalIgnoreCase);

            foreach (DownloadTask task in Tasks.Where(task => !string.IsNullOrWhiteSpace(task.Gid)))
            {
                currentGids.Add(task.Gid);
                if (_observedTaskStatuses.TryGetValue(task.Gid, out string? previousStatus))
                {
                    if (!IsCompletedTaskStatus(previousStatus) && IsCompletedTask(task))
                    {
                        _notifications.ShowDownloadCompleted(task);
                    }
                    else if (!IsErrorTaskStatus(previousStatus) && IsErrorTaskStatus(task.Status))
                    {
                        _notifications.ShowDownloadFailed(task);
                    }
                }

                _observedTaskStatuses[task.Gid] = task.Status;
            }

            foreach (string staleGid in _observedTaskStatuses.Keys.Except(currentGids, StringComparer.OrdinalIgnoreCase).ToArray())
            {
                _observedTaskStatuses.Remove(staleGid);
            }
        }

        private void SetTaskListLoading(bool isLoading)
        {
            if (TasksLoadingPanel is null || TasksLoadingRing is null)
            {
                return;
            }

            TasksLoadingRing.IsActive = isLoading;
            TasksLoadingPanel.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            TasksListView.Visibility = isLoading ? Visibility.Collapsed : Visibility.Visible;
        }

        private void UpdateDebugStatus()
        {
            if (DebugEngineText is null ||
                TerminalTextBlock is null ||
                UseSystemProxyCheckBox is null)
            {
                return;
            }

            string engineStatus = _aria2EngineHost.IsRunning
                ? Strings.Format("DebugAriaRunningStatus", _aria2EngineHost.ProcessId ?? 0)
                : Strings.Get("DebugAriaStoppedStatus");

            DebugEngineText.Text = engineStatus;
            TerminalTextBlock.Text = _aria2EngineHost.TerminalText;
            TerminalScrollViewer?.ChangeView(null, double.MaxValue, null);
        }

        private async void ResumeTasksButton_Click(object sender, RoutedEventArgs e)
        {
            await RunTaskOperationSetAsync(
                GetSelectedTasks().Where(IsPausedTask).ToArray(),
                tasks => _downloadCoordinator.ResumeAsync(tasks),
                Strings.Get("TasksResumedMessage"));
        }

        private async void PauseTasksButton_Click(object sender, RoutedEventArgs e)
        {
            await RunTaskOperationSetAsync(
                GetSelectedTasks().Where(IsActiveTransferTask).ToArray(),
                tasks => _downloadCoordinator.PauseAsync(tasks),
                Strings.Get("TasksPausedMessage"));
        }

        private async void RecoverTasksButton_Click(object sender, RoutedEventArgs e)
        {
            await RunTaskOperationSetAsync(
                GetSelectedTasks().Where(IsRecoverableTask).ToArray(),
                tasks => _downloadCoordinator.RecoverAsync(tasks, GetDefaultRecoverySplitCount()),
                Strings.Get("TasksRecoveredMessage"));
        }

        private void OpenSelectedTaskFileButton_Click(object sender, RoutedEventArgs e)
        {
            DownloadTask? task = GetSingleSelectedTask();
            if (task is not null)
            {
                OpenTaskFile(task);
            }
        }

        private void OpenSelectedTaskFolderButton_Click(object sender, RoutedEventArgs e)
        {
            DownloadTask? task = GetSingleSelectedTask();
            if (task is not null)
            {
                OpenTaskFolder(task);
            }
        }

        private void CopySelectedTaskLinksButton_Click(object sender, RoutedEventArgs e)
        {
            CopyTaskLinks(GetSelectedTasks());
        }

        private async void DeleteTasksButton_Click(object sender, RoutedEventArgs e)
        {
            List<DownloadTask> selectedTasks = GetSelectedTasks();
            if (selectedTasks.Count == 0)
            {
                return;
            }

            bool? deleteFiles = await ConfirmDeleteAsync();
            if (deleteFiles is null)
            {
                return;
            }

            await RunSelectedTaskOperationAsync(
                tasks => _downloadCoordinator.DeleteAsync(tasks, deleteFiles.Value),
                Strings.Get("TasksDeletedMessage"));
        }

        private async void ClearCompletedTasksButton_Click(object sender, RoutedEventArgs e)
        {
            bool? deleteFiles = await ConfirmClearRecordsAsync();
            if (deleteFiles is null)
            {
                return;
            }

            int clearedCount = await _downloadCoordinator.ClearCompletedAsync(deleteFiles.Value);
            ApplyTaskFilter(_currentTaskFilter);
            UpdateDashboard();
            UpdateSelectionCommands();

            string message = clearedCount == 0
                ? Strings.Get("ClearCompletedTasksEmptyMessage")
                : Strings.Get("ClearCompletedTasksMessage");
            ShowMessage(message, InfoBarSeverity.Success);
        }

        private async void TaskTogglePauseResumeButton_Click(object sender, RoutedEventArgs e)
        {
            if (GetTaskFromSender(sender) is not DownloadTask task)
            {
                return;
            }

            if (IsErrorTaskStatus(task.Status))
            {
                await RunTaskOperationAsync(
                    task,
                    tasks => _downloadCoordinator.RecoverAsync(tasks, GetDefaultRecoverySplitCount()),
                    Strings.Get("TasksRecoveredMessage"));
            }
            else if (task.IsPaused)
            {
                await RunTaskOperationAsync(
                    task,
                    tasks => _downloadCoordinator.ResumeAsync(tasks),
                    Strings.Get("TasksResumedMessage"));
            }
            else
            {
                await RunTaskOperationAsync(
                    task,
                    tasks => _downloadCoordinator.PauseAsync(tasks),
                    Strings.Get("TasksPausedMessage"));
            }
        }

        private void TaskOpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (GetTaskFromSender(sender) is not DownloadTask task)
            {
                return;
            }

            OpenTaskFile(task);
        }

        private void OpenTaskFile(DownloadTask task)
        {
            string filePath = ResolveTaskFilePath(task);
            if (!File.Exists(filePath))
            {
                ShowMessage(Strings.Get("TaskFileNotFoundMessage"), InfoBarSeverity.Warning);
                return;
            }

            OpenShellTarget(filePath);
        }

        private void TaskOpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (GetTaskFromSender(sender) is not DownloadTask task)
            {
                return;
            }

            OpenTaskFolder(task);
        }

        private void OpenTaskFolder(DownloadTask task)
        {
            string folderPath = ResolveTaskFolderPath(task);

            if (!Directory.Exists(folderPath))
            {
                ShowMessage(Strings.Get("TaskFolderNotFoundMessage"), InfoBarSeverity.Warning);
                return;
            }

            OpenShellTarget(folderPath);
        }

        private void TaskCopyLinkButton_Click(object sender, RoutedEventArgs e)
        {
            if (GetTaskFromSender(sender) is not DownloadTask task ||
                string.IsNullOrWhiteSpace(task.SourceUri))
            {
                ShowMessage(Strings.Get("TaskLinkNotFoundMessage"), InfoBarSeverity.Warning);
                return;
            }

            CopyTaskLinks([task]);
        }

        private void CopyTaskLinks(IReadOnlyList<DownloadTask> tasks)
        {
            string[] links = tasks
                .Select(task => task.SourceUri)
                .Where(link => !string.IsNullOrWhiteSpace(link))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (links.Length == 0)
            {
                ShowMessage(Strings.Get("TaskLinkNotFoundMessage"), InfoBarSeverity.Warning);
                return;
            }

            DataPackage package = new();
            package.SetText(string.Join(Environment.NewLine, links));
            Clipboard.SetContent(package);
            ShowMessage(Strings.Get("TaskLinkCopiedMessage"), InfoBarSeverity.Success);
        }

        private async void TaskDeleteEntryButton_Click(object sender, RoutedEventArgs e)
        {
            if (GetTaskFromSender(sender) is not DownloadTask task)
            {
                return;
            }

            bool? deleteFiles = await ConfirmDeleteAsync();
            if (deleteFiles is null)
            {
                return;
            }

            await RunTaskOperationAsync(
                task,
                tasks => _downloadCoordinator.DeleteAsync(tasks, deleteFiles.Value),
                Strings.Get("TaskEntryDeletedMessage"));
        }

        private void TaskCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingTaskSelection ||
                sender is not CheckBox checkBox ||
                checkBox.DataContext is not DownloadTask task)
            {
                return;
            }

            _isUpdatingTaskSelection = true;
            try
            {
                if (checkBox.IsChecked == true)
                {
                    if (!TasksListView.SelectedItems.Contains(task))
                    {
                        TasksListView.SelectedItems.Add(task);
                    }
                }
                else
                {
                    TasksListView.SelectedItems.Remove(task);
                }
            }
            finally
            {
                _isUpdatingTaskSelection = false;
            }

            UpdateTaskSelectionGlyphVisibility(task);
            UpdateSelectionCommands();
            UpdateTaskDetailsPane();
        }

        private void TasksListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingTaskSelection)
            {
                return;
            }

            _isUpdatingTaskSelection = true;
            try
            {
                foreach (DownloadTask task in e.AddedItems.OfType<DownloadTask>())
                {
                    task.IsSelected = true;
                    UpdateTaskSelectionGlyphVisibility(task);
                }

                foreach (DownloadTask task in e.RemovedItems.OfType<DownloadTask>())
                {
                    task.IsSelected = false;
                    UpdateTaskSelectionGlyphVisibility(task);
                }
            }
            finally
            {
                _isUpdatingTaskSelection = false;
            }

            UpdateSelectionCommands();
            UpdateTaskDetailsPane();
        }

        private void TasksListView_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (sender is not ListView listView ||
                FindAncestor<ListViewItem>(e.OriginalSource as DependencyObject) is not null ||
                !e.GetCurrentPoint(listView).Properties.IsLeftButtonPressed)
            {
                return;
            }

            TasksListView.SelectedItems.Clear();
            foreach (DownloadTask task in _visibleTasks)
            {
                task.IsSelected = false;
                UpdateTaskSelectionGlyphVisibility(task);
            }

            UpdateSelectionCommands();
            UpdateTaskDetailsPane();
        }

        private void TasksListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (sender is not ListView listView)
            {
                return;
            }

            DownloadTask? rightTappedTask = null;
            if (FindAncestor<ListViewItem>(e.OriginalSource as DependencyObject) is ListViewItem itemContainer)
            {
                rightTappedTask = itemContainer.Content as DownloadTask ?? itemContainer.DataContext as DownloadTask;
            }

            if (rightTappedTask is null)
            {
                TasksListView.SelectedItems.Clear();
                UpdateSelectionCommands();
                UpdateTaskDetailsPane();
                return;
            }

            if (!TasksListView.SelectedItems.Contains(rightTappedTask))
            {
                _isUpdatingTaskSelection = true;
                try
                {
                    TasksListView.SelectedItems.Clear();
                    foreach (DownloadTask task in _visibleTasks)
                    {
                        task.IsSelected = ReferenceEquals(task, rightTappedTask);
                        if (task.IsSelected)
                        {
                            TasksListView.SelectedItems.Add(task);
                        }

                        UpdateTaskSelectionGlyphVisibility(task);
                    }
                }
                finally
                {
                    _isUpdatingTaskSelection = false;
                }

                UpdateSelectionCommands();
                UpdateTaskDetailsPane();
            }

            MenuFlyout flyout = CreateTaskContextMenu(GetSelectedTasks());
            if (flyout.Items.Count > 0)
            {
                flyout.ShowAt(listView, e.GetPosition(listView));
                e.Handled = true;
            }
        }

        private MenuFlyout CreateTaskContextMenu(IReadOnlyList<DownloadTask> selectedTasks)
        {
            MenuFlyout flyout = new();
            if (selectedTasks.Count == 0)
            {
                return flyout;
            }

            DownloadTask[] activeTasks = selectedTasks.Where(IsActiveTransferTask).ToArray();
            DownloadTask[] pausedTasks = selectedTasks.Where(IsPausedTask).ToArray();
            DownloadTask[] recoverableTasks = selectedTasks.Where(IsRecoverableTask).ToArray();

            if (activeTasks.Length > 0)
            {
                flyout.Items.Add(CreateTaskContextMenuItem(
                    Strings.Get("PauseTasksButton.Label"),
                    "\uE769",
                    async () => await RunTaskOperationSetAsync(
                        activeTasks,
                        tasks => _downloadCoordinator.PauseAsync(tasks),
                        Strings.Get("TasksPausedMessage"))));
            }

            if (pausedTasks.Length > 0)
            {
                flyout.Items.Add(CreateTaskContextMenuItem(
                    Strings.Get("ResumeTasksButton.Label"),
                    "\uE768",
                    async () => await RunTaskOperationSetAsync(
                        pausedTasks,
                        tasks => _downloadCoordinator.ResumeAsync(tasks),
                        Strings.Get("TasksResumedMessage"))));
            }

            if (recoverableTasks.Length > 0)
            {
                flyout.Items.Add(CreateTaskContextMenuItem(
                    Strings.Get("RecoverTasksButton.Label"),
                    "\uE72C",
                    async () => await RunTaskOperationSetAsync(
                        recoverableTasks,
                        tasks => _downloadCoordinator.RecoverAsync(tasks, GetDefaultRecoverySplitCount()),
                        Strings.Get("TasksRecoveredMessage"))));
            }

            bool hasTransferAction = flyout.Items.Count > 0;
            if (selectedTasks.Count == 1)
            {
                DownloadTask task = selectedTasks[0];
                if (CanOpenTaskFile(task))
                {
                    AddTaskContextSeparatorIfNeeded(flyout, hasTransferAction);
                    flyout.Items.Add(CreateTaskContextMenuItem(
                        Strings.Get("TaskOpenFileActionText"),
                        "\uE8A7",
                        () => OpenTaskFile(task)));
                }

                if (CanOpenTaskFolder(task))
                {
                    AddTaskContextSeparatorIfNeeded(flyout, hasTransferAction || CanOpenTaskFile(task));
                    flyout.Items.Add(CreateTaskContextMenuItem(
                        Strings.Get("TaskOpenFolderActionText"),
                        "\uE8B7",
                        () => OpenTaskFolder(task)));
                }
            }

            if (selectedTasks.Any(task => !string.IsNullOrWhiteSpace(task.SourceUri)))
            {
                AddTaskContextSeparatorIfNeeded(flyout, flyout.Items.Count > 0);
                flyout.Items.Add(CreateTaskContextMenuItem(
                    Strings.Get("TaskCopyLinkActionText"),
                    "\uE71B",
                    () => CopyTaskLinks(selectedTasks)));
            }

            AddTaskContextSeparatorIfNeeded(flyout, flyout.Items.Count > 0);
            flyout.Items.Add(CreateTaskContextMenuItem(
                Strings.Get("TaskDeleteEntryActionText"),
                "\uE711",
                async () =>
                {
                    bool? deleteFiles = await ConfirmDeleteAsync();
                    if (deleteFiles is null)
                    {
                        return;
                    }

                    await RunTaskOperationSetAsync(
                        selectedTasks,
                        tasks => _downloadCoordinator.DeleteAsync(tasks, deleteFiles.Value),
                        Strings.Get("TasksDeletedMessage"));
                }));

            return flyout;
        }

        private static MenuFlyoutItem CreateTaskContextMenuItem(string text, string glyph, Action action)
        {
            MenuFlyoutItem item = new()
            {
                Text = text,
                Icon = new FontIcon { Glyph = glyph }
            };
            item.Click += (_, _) => action();
            return item;
        }

        private static void AddTaskContextSeparatorIfNeeded(MenuFlyout flyout, bool shouldAdd)
        {
            if (shouldAdd && flyout.Items.LastOrDefault() is not MenuFlyoutSeparator)
            {
                flyout.Items.Add(new MenuFlyoutSeparator());
            }
        }

        private async System.Threading.Tasks.Task RunTaskOperationSetAsync(
            IReadOnlyList<DownloadTask> tasks,
            Func<IReadOnlyList<DownloadTask>, System.Threading.Tasks.Task> operation,
            string successMessage)
        {
            DownloadTask[] taskSet = tasks
                .Where(task => !string.IsNullOrWhiteSpace(task.Gid))
                .ToArray();
            if (taskSet.Length == 0)
            {
                return;
            }

            Aria2EngineStartResult startResult = await EnsureAria2StartedAsync();
            if (!startResult.Started)
            {
                ShowMessage(startResult.Message, InfoBarSeverity.Error);
                return;
            }

            try
            {
                await operation(taskSet);
                ApplyTaskFilter(_currentTaskFilter);
                UpdateDashboard();
                UpdateGlobalSpeedsFromTasks();
                UpdateTaskDetailsPane();
                await RefreshDownloadsAsync();
                ApplyTaskFilter(_currentTaskFilter);
                UpdateSelectionCommands();
                ShowMessage(successMessage, InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                ShowMessage(Strings.Format("TaskOperationFailedMessage", ex.Message), InfoBarSeverity.Error);
            }
        }

        private void SelectAllTasksCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingSelectAllCheckBox)
            {
                return;
            }

            SetVisibleTasksSelected(true);
        }

        private void SelectAllTasksCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingSelectAllCheckBox)
            {
                return;
            }

            SetVisibleTasksSelected(false);
        }

        private void SelectAllTasksCheckBox_Indeterminate(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingSelectAllCheckBox)
            {
                return;
            }

            SetVisibleTasksSelected(false);
        }

        private void SetVisibleTasksSelected(bool isSelected)
        {
            _isUpdatingTaskSelection = true;
            try
            {
                TasksListView.SelectedItems.Clear();
                foreach (DownloadTask task in _visibleTasks)
                {
                    task.IsSelected = isSelected;
                    if (isSelected)
                    {
                        TasksListView.SelectedItems.Add(task);
                    }

                    UpdateTaskSelectionGlyphVisibility(task);
                }
            }
            finally
            {
                _isUpdatingTaskSelection = false;
            }

            UpdateSelectionCommands();
        }

        private void TaskIconSelectionBox_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                UpdateTaskSelectionGlyphVisibility(element, true);
            }
        }

        private void TaskIconSelectionBox_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                UpdateTaskSelectionGlyphVisibility(element, false);
            }
        }

        private void TaskItem_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is UserControl userControl &&
                userControl.DataContext is DownloadTask task)
            {
                VisualStateManager.GoToState(userControl, task.IsSelected ? "ShowCheckbox" : "HideCheckbox", false);
            }
        }

        private void SortColumnMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not ToggleMenuFlyoutItem menuItem ||
                !Enum.TryParse(menuItem.Tag?.ToString(), out TaskSortColumn selectedColumn))
            {
                return;
            }

            _sortColumn = selectedColumn;
            ApplyTaskFilter(_currentTaskFilter);
        }

        private void SortDirectionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not ToggleMenuFlyoutItem menuItem)
            {
                return;
            }

            _sortAscending = string.Equals(menuItem.Tag?.ToString(), "Ascending", StringComparison.Ordinal);
            ApplyTaskFilter(_currentTaskFilter);
        }

        private void SortMenuFlyout_Opening(object sender, object e)
        {
            SortByCreatedAtMenuItem.IsChecked = _sortColumn == TaskSortColumn.CreatedAt;
            SortByNameMenuItem.IsChecked = _sortColumn == TaskSortColumn.Name;
            SortBySizeMenuItem.IsChecked = _sortColumn == TaskSortColumn.Size;
            SortAscendingMenuItem.IsChecked = _sortAscending;
            SortDescendingMenuItem.IsChecked = !_sortAscending;
        }

        private void UseSystemProxyCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            UpdateDebugStatus();
        }

        private void SettingToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (sender is not ToggleSwitch toggleSwitch ||
                toggleSwitch.Parent is not StackPanel panel)
            {
                return;
            }

            TextBlock? stateText = panel.Children.OfType<TextBlock>().FirstOrDefault();
            if (stateText is not null)
            {
                SetToggleStateText(stateText, toggleSwitch.IsOn);
            }

            if (ReferenceEquals(toggleSwitch, UseSystemProxyCheckBox))
            {
                UseSystemProxyCheckBox_Changed(sender, e);
            }

            if (ReferenceEquals(toggleSwitch, CloseToTrayToggleSwitch))
            {
                if (_isLoadingCloseBehaviorSettings)
                {
                    return;
                }

                _closeBehaviorSettings = _closeBehaviorSettings with
                {
                    MinimizeToTrayOnClose = toggleSwitch.IsOn
                };
                SaveCloseBehaviorSettings();
            }
        }

        private async void ApplySpeedLimitButton_Click(object sender, RoutedEventArgs e)
        {
            _isDownloadSpeedLimitEnabled = DownloadLimitToggleSwitch.IsOn;
            _isUploadSpeedLimitEnabled = UploadLimitToggleSwitch.IsOn;
            _downloadLimitBytesPerSecond = _isDownloadSpeedLimitEnabled
                ? GetSpeedLimitBytesPerSecond(DownloadLimitNumberBox, GetSelectedSpeedLimitUnit(DownloadLimitUnitComboBox))
                : 0;
            _uploadLimitBytesPerSecond = _isUploadSpeedLimitEnabled
                ? GetSpeedLimitBytesPerSecond(UploadLimitNumberBox, GetSelectedSpeedLimitUnit(UploadLimitUnitComboBox))
                : 0;

            Aria2EngineStartResult startResult = await EnsureAria2StartedAsync();
            if (!startResult.Started)
            {
                ShowMessage(startResult.Message, InfoBarSeverity.Error);
                return;
            }

            try
            {
                await ApplyConfiguredSpeedLimitsAsync();
                SaveSpeedLimitSettings();
                UpdateGlobalSpeedLimitText();
                SpeedLimitButton.Flyout?.Hide();
                ShowMessage(Strings.Get("SpeedLimitAppliedMessage"), InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                ShowMessage(Strings.Format("SpeedLimitApplyFailedMessage", ex.Message), InfoBarSeverity.Error);
            }
        }

        private Task ApplyConfiguredSpeedLimitsAsync()
        {
            return _downloadCoordinator.SetGlobalSpeedLimitsAsync(
                _isDownloadSpeedLimitEnabled ? _downloadLimitBytesPerSecond : 0,
                _isUploadSpeedLimitEnabled ? _uploadLimitBytesPerSecond : 0);
        }

        private static long GetSpeedLimitBytesPerSecond(NumberBox numberBox, string unit)
        {
            if (numberBox is null || double.IsNaN(numberBox.Value))
            {
                return 0;
            }

            long multiplier = unit.Equals("MB/s", StringComparison.OrdinalIgnoreCase)
                ? 1024L * 1024L
                : 1024L;

            return Math.Max(1, (long)Math.Round(numberBox.Value)) * multiplier;
        }

        private void DownloadLimitToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            SetDownloadSpeedLimitInputsEnabled(DownloadLimitToggleSwitch.IsOn);
        }

        private void UploadLimitToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            SetUploadSpeedLimitInputsEnabled(UploadLimitToggleSwitch.IsOn);
        }

        private void SetDownloadSpeedLimitInputsEnabled(bool isEnabled)
        {
            if (DownloadLimitNumberBox is not null)
            {
                DownloadLimitNumberBox.IsEnabled = isEnabled;
            }

            if (DownloadLimitUnitComboBox is not null)
            {
                DownloadLimitUnitComboBox.IsEnabled = isEnabled;
            }
        }

        private void SetUploadSpeedLimitInputsEnabled(bool isEnabled)
        {
            if (UploadLimitNumberBox is not null)
            {
                UploadLimitNumberBox.IsEnabled = isEnabled;
            }

            if (UploadLimitUnitComboBox is not null)
            {
                UploadLimitUnitComboBox.IsEnabled = isEnabled;
            }
        }

        private static string GetSelectedSpeedLimitUnit(ComboBox comboBox)
        {
            return comboBox?.SelectedItem is ComboBoxItem item &&
                item.Content?.ToString() is string unit &&
                !string.IsNullOrWhiteSpace(unit)
                ? unit
                : "KB/s";
        }

        private void LoadSpeedLimitSettings()
        {
            SpeedLimitSettings settings = ReadSpeedLimitSettings();

            SetSpeedLimitUnit(UploadLimitUnitComboBox, settings.UploadUnit);
            SetSpeedLimitUnit(DownloadLimitUnitComboBox, settings.DownloadUnit);
            UploadLimitNumberBox.Value = Math.Max(settings.UploadValue, 1);
            DownloadLimitNumberBox.Value = Math.Max(settings.DownloadValue, 1);

            _isUploadSpeedLimitEnabled = settings.UploadEnabled;
            _isDownloadSpeedLimitEnabled = settings.DownloadEnabled;
            _uploadLimitBytesPerSecond = settings.UploadEnabled
                ? GetSpeedLimitBytesPerSecond(UploadLimitNumberBox, GetSelectedSpeedLimitUnit(UploadLimitUnitComboBox))
                : 0;
            _downloadLimitBytesPerSecond = settings.DownloadEnabled
                ? GetSpeedLimitBytesPerSecond(DownloadLimitNumberBox, GetSelectedSpeedLimitUnit(DownloadLimitUnitComboBox))
                : 0;

            UploadLimitToggleSwitch.IsOn = settings.UploadEnabled;
            DownloadLimitToggleSwitch.IsOn = settings.DownloadEnabled;
            SetUploadSpeedLimitInputsEnabled(settings.UploadEnabled);
            SetDownloadSpeedLimitInputsEnabled(settings.DownloadEnabled);
        }

        private void LoadCloseBehaviorSettings()
        {
            _closeBehaviorSettings = ReadCloseBehaviorSettings();
            _isLoadingCloseBehaviorSettings = true;
            try
            {
                ApplyCloseBehaviorSettingsToUi();
            }
            finally
            {
                _isLoadingCloseBehaviorSettings = false;
            }
        }

        private void ApplyCloseBehaviorSettingsToUi()
        {
            if (CloseToTrayToggleSwitch is null)
            {
                return;
            }

            CloseToTrayToggleSwitch.IsOn = _closeBehaviorSettings.MinimizeToTrayOnClose == true;
            if (CloseToTrayToggleSwitch.Parent is StackPanel panel &&
                panel.Children.OfType<TextBlock>().FirstOrDefault() is TextBlock stateText)
            {
                SetToggleStateText(stateText, CloseToTrayToggleSwitch.IsOn);
            }
        }

        private static void SetToggleStateText(TextBlock stateText, bool isOn)
        {
            stateText.Text = isOn ? "开" : "关";
        }

        private SpeedLimitSettings ReadSpeedLimitSettings()
        {
            if (!File.Exists(_speedLimitSettingsPath))
            {
                return SpeedLimitSettings.Default;
            }

            try
            {
                string json = File.ReadAllText(_speedLimitSettingsPath);
                return JsonSerializer.Deserialize<SpeedLimitSettings>(json) ?? SpeedLimitSettings.Default;
            }
            catch
            {
                return SpeedLimitSettings.Default;
            }
        }

        private void SaveSpeedLimitSettings()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_speedLimitSettingsPath)!);
                SpeedLimitSettings settings = new(
                    DownloadLimitToggleSwitch?.IsOn == true,
                    GetValidNumberBoxValue(DownloadLimitNumberBox),
                    GetSelectedSpeedLimitUnit(DownloadLimitUnitComboBox),
                    UploadLimitToggleSwitch?.IsOn == true,
                    GetValidNumberBoxValue(UploadLimitNumberBox),
                    GetSelectedSpeedLimitUnit(UploadLimitUnitComboBox));

                File.WriteAllText(_speedLimitSettingsPath, JsonSerializer.Serialize(settings, new JsonSerializerOptions
                {
                    WriteIndented = true
                }));
            }
            catch
            {
                // Settings persistence is best-effort; aria2 can still run with default limits.
            }
        }

        private CloseBehaviorSettings ReadCloseBehaviorSettings()
        {
            if (!File.Exists(_closeBehaviorSettingsPath))
            {
                return CloseBehaviorSettings.Default;
            }

            try
            {
                string json = File.ReadAllText(_closeBehaviorSettingsPath);
                return JsonSerializer.Deserialize<CloseBehaviorSettings>(json) ?? CloseBehaviorSettings.Default;
            }
            catch
            {
                return CloseBehaviorSettings.Default;
            }
        }

        private void SaveCloseBehaviorSettings()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_closeBehaviorSettingsPath)!);
                File.WriteAllText(_closeBehaviorSettingsPath, JsonSerializer.Serialize(_closeBehaviorSettings, new JsonSerializerOptions
                {
                    WriteIndented = true
                }));
            }
            catch
            {
                // Close behavior persistence is best-effort; the next close can ask again.
            }
        }

        private static double GetValidNumberBoxValue(NumberBox numberBox)
        {
            return numberBox is null || double.IsNaN(numberBox.Value) || numberBox.Value < 1
                ? 1
                : numberBox.Value;
        }

        private static void SetSpeedLimitUnit(ComboBox comboBox, string unit)
        {
            comboBox.SelectedIndex = unit.Equals("MB/s", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        }

        private void TitleSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            ApplySearchFilters();
        }

        private void TitleSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            ApplySearchFilters();
        }

        private void TitleSearchBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            ApplySearchFilters();
        }

        private void ApplySearchFilters()
        {
            ApplyTaskFilter(_currentTaskFilter);
            ApplySettingsFilter();
        }

        private void TerminalToggleButton_Changed(object sender, RoutedEventArgs e)
        {
            if (TerminalPanel is null || TerminalToggleButton is null)
            {
                return;
            }

            TerminalPanel.Visibility = TerminalToggleButton.IsChecked == true
                ? Visibility.Visible
                : Visibility.Collapsed;
            UpdateDebugStatus();
        }

        private void ClearTerminalButton_Click(object sender, RoutedEventArgs e)
        {
            _aria2EngineHost.ClearTerminal();
            UpdateDebugStatus();
        }

        private async System.Threading.Tasks.Task RunSelectedTaskOperationAsync(
            Func<IReadOnlyList<DownloadTask>, System.Threading.Tasks.Task> operation,
            string successMessage)
        {
            List<DownloadTask> selectedTasks = GetSelectedTasks();
            if (selectedTasks.Count == 0)
            {
                return;
            }

            Aria2EngineStartResult startResult = await EnsureAria2StartedAsync();
            if (!startResult.Started)
            {
                ShowMessage(startResult.Message, InfoBarSeverity.Error);
                return;
            }

            try
            {
                await operation(selectedTasks);
                ApplyTaskFilter(_currentTaskFilter);
                UpdateDashboard();
                UpdateGlobalSpeedsFromTasks();
                await RefreshDownloadsAsync();
                ApplyTaskFilter(_currentTaskFilter);
                UpdateSelectionCommands();
                ShowMessage(successMessage, InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                ShowMessage(Strings.Format("TaskOperationFailedMessage", ex.Message), InfoBarSeverity.Error);
            }
        }

        private async System.Threading.Tasks.Task<bool?> ConfirmDeleteAsync()
        {
            CheckBox deleteFilesCheckBox = new()
            {
                Content = Strings.Get("DeleteFilesCheckBoxContent")
            };

            StackPanel dialogContent = new()
            {
                Spacing = 12,
                Children =
                {
                    new TextBlock
                    {
                        Text = Strings.Get("DeleteDialogContent"),
                        TextWrapping = TextWrapping.Wrap
                    },
                    deleteFilesCheckBox
                }
            };

            ContentDialog dialog = new()
            {
                XamlRoot = Content.XamlRoot,
                Title = Strings.Get("DeleteDialogTitle"),
                Content = dialogContent,
                PrimaryButtonText = Strings.Get("DeleteButtonText"),
                CloseButtonText = Strings.Get("CancelButtonText"),
                DefaultButton = ContentDialogButton.Close
            };

            ContentDialogResult result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary
                ? deleteFilesCheckBox.IsChecked == true
                : null;
        }

        private async System.Threading.Tasks.Task<bool?> ConfirmClearRecordsAsync()
        {
            CheckBox deleteFilesCheckBox = new()
            {
                Content = Strings.Get("ClearRecordsDeleteFilesCheckBoxContent")
            };

            StackPanel dialogContent = new()
            {
                Spacing = 16,
                Children =
                {
                    new TextBlock
                    {
                        Text = Strings.Get("ClearRecordsDialogContent"),
                        TextWrapping = TextWrapping.Wrap
                    },
                    deleteFilesCheckBox
                }
            };

            StackPanel title = new()
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10,
                Children =
                {
                    new FontIcon
                    {
                        Glyph = "\uE946",
                        Foreground = Application.Current.Resources.TryGetValue("SystemFillColorCautionBrush", out object brush) && brush is Brush cautionBrush
                            ? cautionBrush
                            : null
                    },
                    new TextBlock
                    {
                        Text = Strings.Get("ClearRecordsDialogTitle"),
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                    }
                }
            };

            ContentDialog dialog = new()
            {
                XamlRoot = Content.XamlRoot,
                Title = title,
                Content = dialogContent,
                PrimaryButtonText = Strings.Get("ClearRecordsYesButtonText"),
                CloseButtonText = Strings.Get("ClearRecordsNoButtonText"),
                DefaultButton = ContentDialogButton.Close
            };

            ContentDialogResult result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary
                ? deleteFilesCheckBox.IsChecked == true
                : null;
        }

        private async System.Threading.Tasks.Task RunTaskOperationAsync(
            DownloadTask task,
            Func<IReadOnlyList<DownloadTask>, System.Threading.Tasks.Task> operation,
            string successMessage)
        {
            if (string.IsNullOrWhiteSpace(task.Gid))
            {
                return;
            }

            Aria2EngineStartResult startResult = await EnsureAria2StartedAsync();
            if (!startResult.Started)
            {
                ShowMessage(startResult.Message, InfoBarSeverity.Error);
                return;
            }

            try
            {
                await operation([task]);
                ApplyTaskFilter(_currentTaskFilter);
                UpdateDashboard();
                UpdateGlobalSpeedsFromTasks();
                UpdateTaskDetailsPane();
                await RefreshDownloadsAsync();
                ApplyTaskFilter(_currentTaskFilter);
                UpdateSelectionCommands();
                ShowMessage(successMessage, InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                ShowMessage(Strings.Format("TaskOperationFailedMessage", ex.Message), InfoBarSeverity.Error);
            }
        }

        private static DownloadTask? GetTaskFromSender(object sender)
        {
            return sender is FrameworkElement { DataContext: DownloadTask task }
                ? task
                : null;
        }

        private static string ResolveTaskFilePath(DownloadTask task)
        {
            if (!string.IsNullOrWhiteSpace(task.LocalFilePath))
            {
                return task.LocalFilePath;
            }

            return string.IsNullOrWhiteSpace(task.SaveDirectory) || string.IsNullOrWhiteSpace(task.Name)
                ? string.Empty
                : Path.Combine(task.SaveDirectory, task.Name);
        }

        private static string ResolveTaskFolderPath(DownloadTask task)
        {
            return !string.IsNullOrWhiteSpace(task.SaveDirectory) && Directory.Exists(task.SaveDirectory)
                ? task.SaveDirectory
                : Path.GetDirectoryName(ResolveTaskFilePath(task)) ?? string.Empty;
        }

        private static void OpenShellTarget(string path)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }

        private List<DownloadTask> GetSelectedTasks()
        {
            return TasksListView.SelectedItems
                .OfType<DownloadTask>()
                .Where(task => !string.IsNullOrWhiteSpace(task.Gid))
                .ToList();
        }

        private DownloadTask? GetSingleSelectedTask()
        {
            List<DownloadTask> selectedTasks = GetSelectedTasks();
            return selectedTasks.Count == 1 ? selectedTasks[0] : null;
        }

        private void UpdateSelectionCommands()
        {
            List<DownloadTask> selectedTasks = GetSelectedTasks();
            bool hasSelection = selectedTasks.Count > 0;
            DownloadTask? singleSelectedTask = selectedTasks.Count == 1 ? selectedTasks[0] : null;

            ResumeTasksButton.IsEnabled = selectedTasks.Any(IsPausedTask);
            PauseTasksButton.IsEnabled = selectedTasks.Any(IsActiveTransferTask);
            RecoverTasksButton.IsEnabled = selectedTasks.Any(IsRecoverableTask);
            DeleteTasksButton.IsEnabled = hasSelection;
            OpenSelectedTaskFileButton.IsEnabled = singleSelectedTask is not null && CanOpenTaskFile(singleSelectedTask);
            OpenSelectedTaskFolderButton.IsEnabled = singleSelectedTask is not null && CanOpenTaskFolder(singleSelectedTask);
            CopySelectedTaskLinksButton.IsEnabled = selectedTasks.Any(task => !string.IsNullOrWhiteSpace(task.SourceUri));
            UpdateSelectAllCheckBox();
        }

        private static bool CanOpenTaskFile(DownloadTask task)
        {
            return IsCompletedTask(task) && File.Exists(ResolveTaskFilePath(task));
        }

        private static bool CanOpenTaskFolder(DownloadTask task)
        {
            return Directory.Exists(ResolveTaskFolderPath(task));
        }

        private static bool IsDownloadingTask(DownloadTask task)
        {
            return task.Status.Contains("download", StringComparison.OrdinalIgnoreCase)
                || task.Status.Contains("waiting", StringComparison.OrdinalIgnoreCase)
                || task.Status.Contains("paused", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsActiveTransferTask(DownloadTask task)
        {
            return IsDownloadingTask(task) && !IsPausedTask(task);
        }

        private static bool IsPausedTask(DownloadTask task)
        {
            return task.Status.Contains("paused", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsCompletedTask(DownloadTask task)
        {
            return IsCompletedTaskStatus(task.Status);
        }

        private static bool IsIssueTask(DownloadTask task)
        {
            return IsErrorTaskStatus(task.Status)
                || task.Status.Contains("removed", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsRecoverableTask(DownloadTask task)
        {
            return IsErrorTaskStatus(task.Status);
        }

        private static bool IsCompletedTaskStatus(string status)
        {
            return status.Contains("complete", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsErrorTaskStatus(string status)
        {
            return status.Contains("error", StringComparison.OrdinalIgnoreCase);
        }

        private static int GetDefaultRecoverySplitCount()
        {
            return 16;
        }

        private string GetSearchQuery()
        {
            return TitleSearchBox?.Text.Trim() ?? string.Empty;
        }

        private static bool IsTaskSearchMatch(DownloadTask task, string query)
        {
            string normalizedQuery = NormalizeSearchText(query);
            if (string.IsNullOrWhiteSpace(normalizedQuery))
            {
                return true;
            }

            string[] terms = query
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (terms.Length == 0)
            {
                terms = [query];
            }

            string searchableText = string.Join(' ', new[]
            {
                task.Name,
                task.StatusText,
                task.ProgressText,
                task.SizeText,
                task.RemainingTimeText,
                task.DownloadSpeedText,
                task.UploadSpeedText
            });

            string normalizedSearchableText = NormalizeSearchText(searchableText);
            return terms
                .Select(NormalizeSearchText)
                .Where(term => !string.IsNullOrWhiteSpace(term))
                .All(normalizedSearchableText.Contains);
        }

        private void ApplySettingsFilter()
        {
            if (SettingsPage is null)
            {
                return;
            }

            string query = GetSearchQuery();
            SetSettingVisibility(StartupSettingCard, query, "startup", "launch", "engine", "aria2", "启动", "引擎");
            SetSettingVisibility(CloseBehaviorSettingCard, query, "close", "tray", "background", "exit", "关闭", "托盘", "后台", "退出");
            SetSettingVisibility(ThemeSettingCard, query, "theme", "appearance", "system", "dark", "light", "主题", "外观");
            SetSettingVisibility(NotificationsSettingCard, query, "notification", "complete", "failed", "通知");

            SetSettingVisibility(DefaultDirectorySettingCard, query, "default", "directory", "download", "folder", Strings.Get("DefaultDirectoryLabel.Text"), "目录", "保存");
            SetSettingVisibility(SplitCountSettingCard, query, "split", "connection", "thread", "分片", "连接数");
            SetSettingVisibility(SpeedLimitSettingCard, query, "speed", "limit", "upload", "download", "速度", "限速");

            SetSettingVisibility(BtEnableSettingCard, query, "bittorrent", "torrent", "magnet", "bt", "磁力", "种子");
            SetSettingVisibility(BtPortSettingCard, query, "bittorrent", "port", "listen", "bt", "端口");
            SetSettingVisibility(BtSeedRatioSettingCard, query, "bittorrent", "seed", "ratio", "bt", "做种", "分享率");

            SetSettingVisibility(UseSystemProxySettingCard, query, "proxy", "system proxy", Strings.Get("ProxyLabel.Text"), "Use Windows system proxy when aria2 starts", "代理");
            SetSettingVisibility(CustomProxySettingCard, query, "proxy", "http", "https", "socks", "custom", "代理");
            SetSettingVisibility(RetrySettingCard, query, "retry", "network", "connection", "重试", "网络");

            SetSettingVisibility(AriaPathSettingCard, query, "aria2c", "path", Strings.Get("AriaPathLabel.Text"), Strings.Get("AriaPathTextBox.PlaceholderText"), "路径");
            SetSettingVisibility(RpcPortSettingCard, query, "rpc", "port", Strings.Get("RpcPortLabel.Text"), "端口");
            SetSettingVisibility(ProcessStatusSettingCard, query, "process", "status", "aria2", Strings.Get("ProcessStatusLabel.Text"), "状态");
            SetSettingVisibility(TerminalSettingCard, query, "terminal", "log", "debug", "aria2", "终端", "日志");

            SetSettingVisibility(AboutAppCard, query, "about", "version", "omnidown", "关于", "版本");
            SetSettingVisibility(AboutCloneCard, query, "clone", "repository", "github", "克隆", "仓库");
            SetSettingVisibility(AboutIssueCard, query, "bug", "issue", "feature", "github", "问题", "建议");
            SetSettingVisibility(AboutReferencesCard, query, "dependencies", "references", "license", "files", "motrix", "aria2", "unigetui", "winui", "依赖", "参考", "许可证");
            SetSettingVisibility(AboutLicenseCard, query, "license", "third-party", "notice", "warranty", "mit", "gpl", "许可证", "第三方", "声明");
        }

        private static void SetSettingVisibility(FrameworkElement? element, string query, params string[] searchableText)
        {
            if (element is null)
            {
                return;
            }

            bool isVisible = string.IsNullOrWhiteSpace(query) || searchableText.Any(text => SearchContains(text, query));
            element.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateSearchPlaceholder()
        {
            if (TitleSearchBox is null)
            {
                return;
            }

            TitleSearchBox.PlaceholderText = _currentTaskFilter == "Settings"
                ? Strings.Get("SearchSettingsPlaceholder")
                : Strings.Get("SearchDownloadsPlaceholder");
        }

        private void UpdateDownloadsHeader(string tag)
        {
            if (DownloadsTitleText is null || tag == "Settings")
            {
                return;
            }

            string resourceKey = tag switch
            {
                "Downloading" => "DownloadingPage",
                "Completed" => "CompletedPage",
                "Issues" => "IssuesPage",
                _ => "HomePage"
            };

            DownloadsTitleText.Text = Strings.Get($"{resourceKey}Title");
        }

        private void UpdateStatsVisibility(string tag)
        {
            if (StatsPanel is null)
            {
                return;
            }

            StatsPanel.Visibility = tag is "Home" or "Downloading" ? Visibility.Visible : Visibility.Collapsed;
            CompletedMetricPanel.Visibility = tag == "Home" ? Visibility.Visible : Visibility.Collapsed;
            IssueMetricPanel.Visibility = tag == "Home" ? Visibility.Visible : Visibility.Collapsed;
            TasksListHeaderPanel.Margin = tag is "Home" or "Downloading"
                ? new Thickness(0, 0, 0, 4)
                : new Thickness(0, 20, 0, 4);
        }

        private IEnumerable<DownloadTask> SortTasks(IEnumerable<DownloadTask> tasks)
        {
            IOrderedEnumerable<DownloadTask> orderedTasks = _sortColumn switch
            {
                TaskSortColumn.CreatedAt => tasks.OrderBy(task => task.CreatedAt),
                TaskSortColumn.Size => tasks.OrderBy(task => task.TotalLength),
                _ => tasks.OrderBy(task => task.Name, StringComparer.CurrentCultureIgnoreCase)
            };

            return _sortAscending ? orderedTasks : orderedTasks.Reverse();
        }

        private void RestoreSelection(HashSet<string> selectedGids)
        {
            _isUpdatingTaskSelection = true;
            try
            {
                TasksListView.SelectedItems.Clear();
                foreach (DownloadTask task in _visibleTasks)
                {
                    bool isSelected = selectedGids.Contains(task.Gid);
                    task.IsSelected = isSelected;
                    if (isSelected)
                    {
                        TasksListView.SelectedItems.Add(task);
                    }
                    UpdateTaskSelectionGlyphVisibility(task);
                }
            }
            finally
            {
                _isUpdatingTaskSelection = false;
            }
        }

        private void SyncVisibleTasks(IReadOnlyList<DownloadTask> desiredTasks)
        {
            for (int index = _visibleTasks.Count - 1; index >= 0; index--)
            {
                if (!desiredTasks.Contains(_visibleTasks[index]))
                {
                    _visibleTasks.RemoveAt(index);
                }
            }

            for (int desiredIndex = 0; desiredIndex < desiredTasks.Count; desiredIndex++)
            {
                DownloadTask desiredTask = desiredTasks[desiredIndex];
                int currentIndex = _visibleTasks.IndexOf(desiredTask);
                if (currentIndex == desiredIndex)
                {
                    continue;
                }

                if (currentIndex >= 0)
                {
                    _visibleTasks.Move(currentIndex, desiredIndex);
                }
                else
                {
                    _visibleTasks.Insert(desiredIndex, desiredTask);
                }
            }
        }

        private void UpdateSelectAllCheckBox()
        {
            if (SelectAllTasksCheckBox is null)
            {
                return;
            }

            _isUpdatingSelectAllCheckBox = true;
            try
            {
                int itemCount = _visibleTasks.Count;
                int selectedCount = TasksListView.SelectedItems.Count;
                SelectAllTasksCheckBox.IsEnabled = itemCount > 0;
                SelectAllTasksCheckBox.IsChecked = selectedCount switch
                {
                    0 => false,
                    _ when selectedCount == itemCount => true,
                    _ => null
                };
            }
            finally
            {
                _isUpdatingSelectAllCheckBox = false;
            }
        }

        private void UpdateTaskSelectionGlyphVisibility(DownloadTask task)
        {
            if (TasksListView.ContainerFromItem(task) is ListViewItem itemContainer &&
                FindDescendant<UserControl>(itemContainer) is UserControl userControl)
            {
                VisualStateManager.GoToState(userControl, task.IsSelected ? "ShowCheckbox" : "HideCheckbox", true);
            }
        }

        private static void UpdateTaskSelectionGlyphVisibility(FrameworkElement element, bool isPointerOver)
        {
            UserControl? userControl = FindAncestor<UserControl>(element);
            if (userControl?.DataContext is DownloadTask task)
            {
                VisualStateManager.GoToState(userControl, task.IsSelected || isPointerOver ? "ShowCheckbox" : "HideCheckbox", true);
            }
        }

        private static T? FindAncestor<T>(DependencyObject? start) where T : DependencyObject
        {
            DependencyObject? current = start;
            while (current is not null)
            {
                if (current is T match)
                {
                    return match;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }

        private static T? FindDescendant<T>(DependencyObject start) where T : DependencyObject
        {
            int count = VisualTreeHelper.GetChildrenCount(start);
            for (int index = 0; index < count; index++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(start, index);
                if (child is T match)
                {
                    return match;
                }

                T? descendant = FindDescendant<T>(child);
                if (descendant is not null)
                {
                    return descendant;
                }
            }

            return null;
        }

        private static bool SearchContains(string? value, string query)
        {
            string normalizedValue = NormalizeSearchText(value);
            string normalizedQuery = NormalizeSearchText(query);
            return !string.IsNullOrWhiteSpace(normalizedValue)
                && !string.IsNullOrWhiteSpace(normalizedQuery)
                && normalizedValue.Contains(normalizedQuery, StringComparison.Ordinal);
        }

        private static string NormalizeSearchText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            StringBuilder builder = new(value.Length);
            foreach (char character in value.Normalize(NormalizationForm.FormKC))
            {
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(character);
                if (category is UnicodeCategory.LowercaseLetter
                    or UnicodeCategory.UppercaseLetter
                    or UnicodeCategory.TitlecaseLetter
                    or UnicodeCategory.ModifierLetter
                    or UnicodeCategory.OtherLetter
                    or UnicodeCategory.DecimalDigitNumber
                    or UnicodeCategory.LetterNumber
                    or UnicodeCategory.OtherNumber)
                {
                    builder.Append(char.ToLowerInvariant(character));
                }
            }

            return builder.ToString();
        }

        private static string FormatSpeed(long bytesPerSecond)
        {
            string[] units = ["B/s", "KB/s", "MB/s", "GB/s"];
            double speed = bytesPerSecond;
            int unitIndex = 0;
            while (speed >= 1024 && unitIndex < units.Length - 1)
            {
                speed /= 1024;
                unitIndex++;
            }

            return $"{speed:0.#} {units[unitIndex]}";
        }

        private void SetWindowIcon()
        {
            nint windowHandle = WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(windowHandle);
            AppWindow appWindow = AppWindow.GetFromWindowId(windowId);
            string? iconPath = ResolveAssetPath("Assets", "OmniDown.ico");
            if (!string.IsNullOrWhiteSpace(iconPath))
            {
                appWindow.SetIcon(iconPath);
            }
        }

        private void ConfigureDefaultWindowSize()
        {
            const int defaultWidth = 2000;
            const int defaultHeight = 1000;
            const int minWidth = 2000;
            const int minHeight = 1000;

            AppWindow.Resize(new SizeInt32(defaultWidth, defaultHeight));
            if (AppWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.PreferredMinimumWidth = minWidth;
                presenter.PreferredMinimumHeight = minHeight;
            }

            DisplayArea displayArea = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
            RectInt32 workArea = displayArea.WorkArea;
            int x = workArea.X + Math.Max(0, (workArea.Width - defaultWidth) / 2);
            int y = workArea.Y + Math.Max(0, (workArea.Height - defaultHeight) / 2);
            AppWindow.Move(new PointInt32(x, y));
        }

        private static string? ResolveAssetPath(params string[] pathSegments)
        {
            string basePath = AppContext.BaseDirectory;
            string candidate = Path.Combine([basePath, .. pathSegments]);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            string? parentPath = Directory.GetParent(basePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))?.FullName;
            if (parentPath is null)
            {
                return null;
            }

            candidate = Path.Combine([parentPath, .. pathSegments]);
            return File.Exists(candidate) ? candidate : null;
        }

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(nint hWnd, ShowWindowCommand command);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(nint hWnd);

    }

    internal enum ShowWindowCommand
    {
        Hide = 0,
        Show = 5,
        Restore = 9
    }

    internal sealed record CloseBehaviorSettings(bool? MinimizeToTrayOnClose)
    {
        public static CloseBehaviorSettings Default { get; } = new(MinimizeToTrayOnClose: null);
    }

    internal sealed record SpeedLimitSettings(
        bool DownloadEnabled,
        double DownloadValue,
        string DownloadUnit,
        bool UploadEnabled,
        double UploadValue,
        string UploadUnit)
    {
        public static SpeedLimitSettings Default { get; } = new(
            DownloadEnabled: false,
            DownloadValue: 1024,
            DownloadUnit: "KB/s",
            UploadEnabled: false,
            UploadValue: 1024,
            UploadUnit: "KB/s");
    }

    internal sealed record AppStatusMessage(
        string Message,
        string DetailText,
        string SeverityText,
        string SeverityGlyph,
        Brush SeverityBrush);

    internal enum TaskSortColumn
    {
        CreatedAt,
        Name,
        Size
    }
}
