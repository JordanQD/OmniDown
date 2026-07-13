using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using OmniDown.Dialogs;
using OmniDown.Models;
using OmniDown.Models.Settings;
using OmniDown.Services.Engine;
using OmniDown.Services.Localization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static OmniDown.Dialogs.NewDownloadDialogHelpers;

namespace OmniDown
{
    public sealed partial class MainWindow
    {
        private async void NewDownloadButton_Click(object sender, RoutedEventArgs e)
        {
            await ShowNewDownloadDialogAsync();
        }

        private void NewDownloadKeyboardAccelerator_Invoked(
            KeyboardAccelerator sender,
            KeyboardAcceleratorInvokedEventArgs args)
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

        private async Task ShowNewDownloadDialogAsync(
            string? initialDownloadText = null,
            string? initialTaskName = null)
        {
            if (_isNewDownloadDialogOpen)
            {
                return;
            }

            _isNewDownloadDialogOpen = true;
            try
            {
                string? downloadText = initialDownloadText ??
                    await GetClipboardDownloadTextAsync(_settingsPageViewModel.AdvancedSettings);
                NewDownloadDialog dialog = new(
                    _windowHandle,
                    _settingsPageViewModel.DownloadSettings.DownloadDirectory,
                    _settingsPageViewModel.DownloadSettings.SplitCount,
                    downloadText,
                    initialTaskName)
                {
                    XamlRoot = Content.XamlRoot
                };

                ContentDialogResult dialogResult = await dialog.ShowAsync();
                if (dialogResult != ContentDialogResult.Primary || dialog.Result is null)
                {
                    return;
                }

                NewDownloadDialogResult request = dialog.Result;
                Aria2EngineStartResult startResult = await EnsureAria2StartedAsync();
                if (!startResult.Started)
                {
                    ShowMessage(startResult.Message, InfoBarSeverity.Error);
                    return;
                }

                try
                {
                    List<DownloadTask> addedTasks = [];
                    if (request.IsTorrentTask && request.Torrent is not null)
                    {
                        IReadOnlyList<int> aria2Selection =
                            request.SelectedTorrentFileIndexes.Count == request.TorrentFileCount
                                ? []
                                : request.SelectedTorrentFileIndexes;
                        DownloadTask task = await _downloadCoordinator.AddTorrentAsync(
                            request.Torrent.Bytes,
                            request.Torrent.Path,
                            request.Torrent.Metadata,
                            request.SaveDirectory,
                            request.SplitCount,
                            aria2Selection);
                        _observedTaskStatuses[task.Gid] = task.Status;
                        addedTasks.Add(task);
                        ShowTaskAddedNotification(task);
                    }
                    else
                    {
                        foreach (string sourceUri in request.SourceUris)
                        {
                            DownloadTask task = await _downloadCoordinator.AddDownloadAsync(
                                sourceUri,
                                request.RequestedName,
                                request.SaveDirectory,
                                request.SplitCount);
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
    }
}
