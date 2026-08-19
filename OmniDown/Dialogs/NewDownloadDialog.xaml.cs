namespace OmniDown.Dialogs;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using OmniDown.Models;
using OmniDown.Services.Downloads;
using OmniDown.Services.Localization;
using OmniDown.Services.Logging;
using OmniDown.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;
using static OmniDown.Dialogs.NewDownloadDialogHelpers;

public sealed partial class NewDownloadDialog : ContentDialog
{
    private readonly nint _windowHandle;
    private bool _isSubmitValidationRunning;

    internal NewDownloadDialogViewModel ViewModel { get; }

    internal NewDownloadDialogResult? Result { get; private set; }

    internal NewDownloadDialog(
        nint windowHandle,
        string defaultDownloadDirectory,
        int splitCount,
        string? initialDownloadText,
        string? initialTaskName)
    {
        _windowHandle = windowHandle;
        ViewModel = new NewDownloadDialogViewModel(
            defaultDownloadDirectory,
            splitCount,
            initialDownloadText,
            initialTaskName);
        InitializeComponent();
        InitializeLocalizedText();
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        UpdateTaskTypeLayout();
        UpdateTorrentSummary();
    }

    public static Visibility MessageToVisibility(string? message) =>
        string.IsNullOrWhiteSpace(message) ? Visibility.Collapsed : Visibility.Visible;

    public static Visibility BoolToVisibility(bool value) =>
        value ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility InvertBoolToVisibility(bool value) =>
        value ? Visibility.Collapsed : Visibility.Visible;

    public static Visibility LinkModeToVisibility(NewDownloadTaskType taskType) =>
        taskType == NewDownloadTaskType.Link ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility TorrentModeToVisibility(NewDownloadTaskType taskType) =>
        taskType == NewDownloadTaskType.Torrent ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility TorrentFilesToVisibility(
        NewDownloadTaskType taskType,
        bool hasTorrent) =>
        taskType == NewDownloadTaskType.Torrent && hasTorrent
            ? Visibility.Visible
            : Visibility.Collapsed;

    public static string TorrentFileAutomationId(int index) =>
        $"NewDownloadTorrentFile{index}";

    private void InitializeLocalizedText()
    {
        Title = Strings.Get("NewDownloadDialogTitle");
        PrimaryButtonText = Strings.Get("AddButtonText");
        CloseButtonText = Strings.Get("CancelButtonText");
        LinkTaskTypeItem.Text = Strings.Get("LinkTaskTabHeader");
        TorrentTaskTypeItem.Text = Strings.Get("TorrentTaskTabHeader");
        UrlHeaderText.Text = Strings.Get("NewDownloadUrlHeader");
        UriTextBox.PlaceholderText = Strings.Get("NewDownloadUrlPlaceholder");
        TorrentFileHeaderText.Text = Strings.Get("NewDownloadTorrentFileHeader");
        TaskNameTextBox.Header = Strings.Get("NewDownloadTaskNameHeader");
        TaskNameTextBox.PlaceholderText = Strings.Get("NewDownloadTaskNamePlaceholder");
        DirectoryTextBox.Header = Strings.Get("NewDownloadDirectoryHeader");
        SplitCountNumberBox.Header = Strings.Get("NewDownloadSplitCountHeader");
        TorrentIndexHeaderText.Text = Strings.Get("TorrentFileIndexHeader");
        TorrentNameHeaderText.Text = Strings.Get("TorrentFileNameHeader");
        TorrentSizeHeaderText.Text = Strings.Get("TorrentFileSizeHeader");
        TorrentDropOverlayText.Text = Strings.Get("TorrentDropOverlayText");

        AutomationProperties.SetName(UriTextBox, Strings.Get("NewDownloadUrlHeader"));
        AutomationProperties.SetName(TaskNameTextBox, Strings.Get("NewDownloadTaskNameHeader"));
        AutomationProperties.SetName(DirectoryTextBox, Strings.Get("NewDownloadDirectoryHeader"));
        AutomationProperties.SetName(SplitCountNumberBox, Strings.Get("NewDownloadSplitCountHeader"));
        SetIconButtonAccessibility(PasteUriButton, Strings.Get("PasteButtonText"));
        SetIconButtonAccessibility(OpenTorrentButton, Strings.Get("OpenTorrentFileButtonText"));
        SetIconButtonAccessibility(ClearTorrentButton, Strings.Get("ClearTorrentFileButtonText"));
        SetIconButtonAccessibility(BrowseDirectoryButton, Strings.Get("BrowseDownloadDirectoryButtonText"));
        AutomationProperties.SetName(
            SelectAllTorrentFilesCheckBox,
            Strings.Get("SelectAllTorrentFilesAutomationName"));
        AutomationProperties.SetName(
            TorrentFilesListView,
            Strings.Get("TorrentFilesListAutomationName"));
        AutomationProperties.SetName(DropOverlay, Strings.Get("TorrentDropOverlayText"));
    }

    private static void SetIconButtonAccessibility(Button button, string name)
    {
        AutomationProperties.SetName(button, name);
        ToolTipService.SetToolTip(button, name);
    }

    private void TaskTypeSelector_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        ViewModel.TaskType = sender.SelectedItem == TorrentTaskTypeItem
            ? NewDownloadTaskType.Torrent
            : NewDownloadTaskType.Link;
        UpdateTaskTypeLayout();
    }

    private void UpdateTaskTypeLayout()
    {
        if (CommonFieldsPanel is null || TorrentFilesRow is null)
        {
            return;
        }

        bool showTorrentFiles =
            ViewModel.TaskType == NewDownloadTaskType.Torrent && ViewModel.HasTorrent;
        Grid.SetRow(CommonFieldsPanel, showTorrentFiles ? 3 : 2);
    }

    private async void PasteUriButton_Click(object sender, RoutedEventArgs e)
    {
        await PasteClipboardTextAsync(UriTextBox);
    }

    private async void PasteUriAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        await PasteClipboardTextAsync(UriTextBox);
    }

    private async void OpenTorrentButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            NewDownloadTorrentSelection? selection = await PickTorrentFileAsync();
            if (selection is not null)
            {
                SetTorrentSelection(selection);
            }
        }
        catch (Exception ex)
        {
            AppLogger.Error("NewDownloadDialog.Torrent", ex);
            ViewModel.SetTorrentValidationException(ex);
            _ = OpenTorrentButton.Focus(FocusState.Programmatic);
        }
    }

    private void ClearTorrentButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ClearTorrentSelection();
        UpdateTaskTypeLayout();
        UpdateTorrentSummary();
        _ = OpenTorrentButton.Focus(FocusState.Programmatic);
    }

    private async void BrowseDirectoryButton_Click(object sender, RoutedEventArgs e)
    {
        FolderPicker picker = new()
        {
            SuggestedStartLocation = PickerLocationId.Downloads
        };
        picker.FileTypeFilter.Add("*");
        InitializeWithWindow.Initialize(picker, _windowHandle);

        StorageFolder? folder = await picker.PickSingleFolderAsync();
        if (folder is not null)
        {
            ViewModel.DownloadDirectory = folder.Path;
        }
    }

    private void SelectAllTorrentFilesCheckBox_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SetAllTorrentFilesSelected(SelectAllTorrentFilesCheckBox.IsChecked == true);
    }

    private void DialogRoot_DragOver(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            return;
        }

        e.AcceptedOperation = DataPackageOperation.Copy;
        e.DragUIOverride.Caption = Strings.Get("TorrentDropOverlayText");
        e.DragUIOverride.IsCaptionVisible = true;
        DropOverlay.Visibility = Visibility.Visible;
    }

    private void DialogRoot_DragLeave(object sender, DragEventArgs e)
    {
        DropOverlay.Visibility = Visibility.Collapsed;
    }

    private async void DialogRoot_Drop(object sender, DragEventArgs e)
    {
        DropOverlay.Visibility = Visibility.Collapsed;
        if (!e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            return;
        }

        IReadOnlyList<IStorageItem> items = await e.DataView.GetStorageItemsAsync();
        StorageFile? file = items
            .OfType<StorageFile>()
            .FirstOrDefault(item => item.FileType.Equals(".torrent", StringComparison.OrdinalIgnoreCase));
        if (file is not null)
        {
            try
            {
                SetTorrentSelection(await LoadTorrentFileAsync(file));
            }
            catch (Exception ex)
            {
                AppLogger.Error("NewDownloadDialog.Torrent", ex);
                ViewModel.SetTorrentValidationException(ex);
                _ = OpenTorrentButton.Focus(FocusState.Programmatic);
            }
        }
    }

    private void ContentDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        Control initialFocus = ViewModel.TaskType == NewDownloadTaskType.Torrent
            ? OpenTorrentButton
            : UriTextBox;
        _ = initialFocus.Focus(FocusState.Programmatic);
    }

    private async void ContentDialog_PrimaryButtonClick(
        ContentDialog sender,
        ContentDialogButtonClickEventArgs args)
    {
        if (_isSubmitValidationRunning)
        {
            args.Cancel = true;
            return;
        }

        ContentDialogButtonClickDeferral deferral = args.GetDeferral();
        _isSubmitValidationRunning = true;
        args.Cancel = true;
        IsPrimaryButtonEnabled = false;

        try
        {
            List<string> sourceUris = ViewModel.TaskType == NewDownloadTaskType.Link
                ? ViewModel.ParseSourceUris()
                : [];
            if (ViewModel.TaskType == NewDownloadTaskType.Link && sourceUris.Count == 1)
            {
                NewDownloadTorrentSelection? localTorrent = await TryLoadLocalTorrentPathAsync(sourceUris[0]);
                if (localTorrent is not null)
                {
                    SetTorrentSelection(localTorrent);
                    sourceUris.Clear();
                }
            }

            if (!ViewModel.Validate(sourceUris))
            {
                FocusFirstValidationError();
                return;
            }

            Result = ViewModel.CreateResult(sourceUris);
            args.Cancel = false;
        }
        catch (Exception ex)
        {
            AppLogger.Error("NewDownloadDialog.Validation", ex);
            ViewModel.SetValidationException(ex);
            _ = UriTextBox.Focus(FocusState.Programmatic);
        }
        finally
        {
            IsPrimaryButtonEnabled = true;
            _isSubmitValidationRunning = false;
            deferral.Complete();
        }
    }

    private void FocusFirstValidationError()
    {
        if (!string.IsNullOrWhiteSpace(ViewModel.UriValidationMessage))
        {
            _ = UriTextBox.Focus(FocusState.Programmatic);
            return;
        }

        Control validationTarget = ViewModel.HasTorrent
            ? SelectAllTorrentFilesCheckBox
            : OpenTorrentButton;
        _ = validationTarget.Focus(FocusState.Programmatic);
    }

    private void SetTorrentSelection(NewDownloadTorrentSelection selection)
    {
        ViewModel.SetTorrentSelection(selection);
        TorrentTaskTypeItem.IsSelected = true;
        UpdateTaskTypeLayout();
        UpdateTorrentSummary();
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(NewDownloadDialogViewModel.TaskType) or
            nameof(NewDownloadDialogViewModel.HasTorrent) or
            nameof(NewDownloadDialogViewModel.SelectAllTorrentFilesState))
        {
            UpdateTaskTypeLayout();
            UpdateTorrentSummary();
        }
    }

    private void UpdateTorrentSummary()
    {
        TorrentSummaryText.Text = ViewModel.HasTorrent
            ? Strings.Format("TorrentFileCountText", ViewModel.TorrentFiles.Count)
            : Strings.Get("TorrentNoFileSelectedText");
    }

    private async Task<NewDownloadTorrentSelection?> PickTorrentFileAsync()
    {
        FileOpenPicker picker = new()
        {
            SuggestedStartLocation = PickerLocationId.Downloads
        };
        picker.FileTypeFilter.Add(".torrent");
        InitializeWithWindow.Initialize(picker, _windowHandle);

        StorageFile? file = await picker.PickSingleFileAsync();
        return file is null ? null : await LoadTorrentFileAsync(file);
    }

    private static Task<NewDownloadTorrentSelection> LoadTorrentFileAsync(StorageFile file)
    {
        return LoadTorrentFileAsync(file.Path, file.Name);
    }

    private static async Task<NewDownloadTorrentSelection?> TryLoadLocalTorrentPathAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path) ||
            !path.EndsWith(".torrent", StringComparison.OrdinalIgnoreCase) ||
            !File.Exists(path))
        {
            return null;
        }

        return await LoadTorrentFileAsync(path, Path.GetFileName(path));
    }

    private static async Task<NewDownloadTorrentSelection> LoadTorrentFileAsync(string path, string fileName)
    {
        byte[] bytes = await File.ReadAllBytesAsync(path);
        TorrentMetadata metadata = TorrentMetadataReader.Read(bytes);
        string displayName = string.IsNullOrWhiteSpace(metadata.Name)
            ? fileName
            : metadata.Name;
        return new NewDownloadTorrentSelection(path, displayName, bytes, metadata);
    }
}
