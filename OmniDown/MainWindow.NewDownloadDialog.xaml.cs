using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using OmniDown.Dialogs;
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
using static OmniDown.Dialogs.NewDownloadDialogHelpers;

namespace OmniDown
{
    public sealed partial class MainWindow
    {
        private async void NewDownloadButton_Click(object sender, RoutedEventArgs e)
        {
            await ShowNewDownloadDialogAsync();
        }

        private void NewDownloadKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            _ = ShowNewDownloadDialogAsync();
        }

        private async void Clipboard_ContentChanged(object? sender, object e)
        {
            AdvancedSettings settings = _settingsPageViewModel.AdvancedSettings;
            if (!settings.ClipboardDetectionEnabled || _isNewDownloadDialogOpen)
            {
                return;
            }

            string? clipboardDownloadText = await GetClipboardDownloadTextAsync(settings);
            if (string.IsNullOrWhiteSpace(clipboardDownloadText) ||
                string.Equals(clipboardDownloadText, _lastClipboardDownloadText, StringComparison.Ordinal))
            {
                return;
            }

            _lastClipboardDownloadText = clipboardDownloadText;
            await ShowNewDownloadDialogAsync(clipboardDownloadText);
        }

        internal async Task HandleExternalDownloadTextAsync(string activationText)
        {
            if (string.IsNullOrWhiteSpace(activationText))
            {
                return;
            }

            string downloadText = ExtractProtocolDownloadText(activationText);
            if (string.IsNullOrWhiteSpace(downloadText))
            {
                return;
            }

            await ShowNewDownloadDialogAsync(downloadText.EndsWith(Environment.NewLine, StringComparison.Ordinal)
                ? downloadText
                : downloadText + Environment.NewLine);
        }

        private static string ExtractProtocolDownloadText(string activationText)
        {
            if (!Uri.TryCreate(activationText, UriKind.Absolute, out Uri? uri))
            {
                return activationText.Trim();
            }

            if (uri.Scheme.Equals("omnidown", StringComparison.OrdinalIgnoreCase))
            {
                string query = uri.Query.TrimStart('?');
                foreach (string part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
                {
                    string[] pair = part.Split('=', 2);
                    if (pair.Length == 2 &&
                        (pair[0].Equals("url", StringComparison.OrdinalIgnoreCase) ||
                            pair[0].Equals("uri", StringComparison.OrdinalIgnoreCase)))
                    {
                        return Uri.UnescapeDataString(pair[1]).Trim();
                    }
                }
            }

            return activationText.Trim();
        }

        private async Task ShowNewDownloadDialogAsync(string? initialDownloadText = null, string? initialTaskName = null)
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
                PlaceholderText = "https://example.com/file.zip",
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                Height = 32,
                MaxHeight = 220
            };
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
                Height = 32,
                MinWidth = 40,
                MinHeight = 32,
                Padding = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(8, 0, 0, 0)
            };
            ToolTipService.SetToolTip(pasteUriButton, Strings.Get("PasteButtonText"));
            pasteUriButton.Click += async (_, _) => await PasteClipboardTextAsync(uriTextBox);

            Grid uriInputControls = new()
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

            StackPanel uriInputRow = new()
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock
                    {
                        Text = Strings.Get("NewDownloadUrlHeader")
                    },
                    uriInputControls
                }
            };

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
            StackPanel torrentInputSection = new()
            {
                Spacing = 4,
                Visibility = Visibility.Collapsed,
                Children =
                {
                    new TextBlock
                    {
                        Text = Strings.Get("NewDownloadTorrentFileHeader")
                    },
                    torrentInputRow
                }
            };
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
            if (torrentHeader.Children[3] is FrameworkElement sizeHeader)
            {
                sizeHeader.HorizontalAlignment = HorizontalAlignment.Left;
            }

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
                PlaceholderText = "Leave empty to infer from the URL",
                Text = initialTaskName?.Trim() ?? string.Empty
            };
            fileNameTextBox.Header = Strings.Get("NewDownloadTaskNameHeader");
            fileNameTextBox.PlaceholderText = Strings.Get("NewDownloadTaskNamePlaceholder");

            TextBox directoryTextBox = new()
            {
                Text = _settingsPageViewModel.DownloadSettings.DownloadDirectory
            };
            directoryTextBox.Header = Strings.Get("NewDownloadDirectoryHeader");

            Button browseDirectoryButton = new()
            {
                Content = new FontIcon
                {
                    Glyph = "\uE8B7",
                    FontSize = 16,
                    Width = 16,
                    Height = 16
                },
                Width = 40,
                Height = 32,
                MinWidth = 40,
                MinHeight = 32,
                Padding = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(8, 0, 0, 0)
            };
            ToolTipService.SetToolTip(browseDirectoryButton, Strings.Get("BrowseDownloadDirectoryButtonText"));
            browseDirectoryButton.Click += async (_, _) =>
            {
                FolderPicker picker = new()
                {
                    SuggestedStartLocation = PickerLocationId.Downloads
                };
                picker.FileTypeFilter.Add("*");
                InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));

                StorageFolder? folder = await picker.PickSingleFolderAsync();
                if (folder is not null)
                {
                    directoryTextBox.Text = folder.Path;
                }
            };

            Grid directoryInputRow = new()
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                Children =
                {
                    directoryTextBox,
                    browseDirectoryButton
                }
            };
            Grid.SetColumn(browseDirectoryButton, 1);

            NumberBox splitCountNumberBox = new()
            {
                Header = Strings.Get("NewDownloadSplitCountHeader"),
                Value = _settingsPageViewModel.DownloadSettings.SplitCount,
                Minimum = 1,
                Maximum = 256,
                SmallChange = 1,
                LargeChange = 8,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline
            };

            StackPanel formContent = new()
            {
                Width = 664,
                MaxWidth = 744,
                HorizontalAlignment = HorizontalAlignment.Left,
                Spacing = 12,
                Children =
                {
                    uriInputRow,
                    torrentInputSection,
                    torrentSummaryText,
                    torrentFilesBorder,
                    fileNameTextBox,
                    directoryInputRow,
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
                torrentInputSection.Visibility = isTorrentSelected ? Visibility.Visible : Visibility.Collapsed;
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
            string? clipboardDownloadText = initialDownloadText ?? await GetClipboardDownloadTextAsync(_settingsPageViewModel.AdvancedSettings);
            if (!string.IsNullOrWhiteSpace(clipboardDownloadText))
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

            ScrollViewer contentScrollViewer = new()
            {
                Width = 680,
                MaxHeight = 480,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = formContent
            };

            Grid dialogBody = new()
            {
                Width = 680,
                MaxWidth = 760,
                MaxHeight = 560,
                RowSpacing = 12,
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }
                },
                Children =
                {
                    selectorHeader,
                    contentScrollViewer
                }
            };
            Grid.SetRow(contentScrollViewer, 1);

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
                    dialogBody,
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
                ? _settingsPageViewModel.DownloadSettings.DownloadDirectory
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
                    ShowTaskAddedNotification(task);
                }
                else
                {
                    string requestedName = sourceUris.Count == 1 ? fileNameTextBox.Text : string.Empty;
                    foreach (string sourceUri in sourceUris)
                    {
                        DownloadTask task = await _downloadCoordinator.AddDownloadAsync(sourceUri, requestedName, saveDirectory, splitCount);
                        _observedTaskStatuses[task.Gid] = task.Status;
                        addedTasks.Add(task);
                        ShowTaskAddedNotification(task);
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
    }
}
