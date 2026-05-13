using Microsoft.UI.Xaml.Controls;
using OmniDown.Dialogs;
using OmniDown.Models;
using OmniDown.Models.Settings;
using OmniDown.Services.BrowserExtension;
using OmniDown.Services.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmniDown
{
    public sealed partial class MainWindow
    {
        private readonly BrowserExtensionApiServer _browserExtensionApiServer;

        private void StartBrowserExtensionApiServer()
        {
            AdvancedSettings settings = _settingsPageViewModel.AdvancedSettings;
            try
            {
                _browserExtensionApiServer.Start(settings.ExtensionApiPort, settings.ExtensionApiSecret);
            }
            catch (Exception ex)
            {
                ShowMessage($"浏览器扩展 API 启动失败：{ex.Message}", InfoBarSeverity.Warning);
            }
        }

        private void RestartBrowserExtensionApiServer()
        {
            AdvancedSettings settings = _settingsPageViewModel.AdvancedSettings;
            try
            {
                _browserExtensionApiServer.Start(settings.ExtensionApiPort, settings.ExtensionApiSecret);
            }
            catch (Exception ex)
            {
                ShowMessage($"浏览器扩展 API 重启失败：{ex.Message}", InfoBarSeverity.Warning);
            }
        }

        private Task<BrowserExtensionDownloadResult> HandleBrowserExtensionDownloadAsync(BrowserExtensionDownloadRequest request)
        {
            TaskCompletionSource<BrowserExtensionDownloadResult> completion = new();
            bool queued = DispatcherQueue.TryEnqueue(async () =>
            {
                try
                {
                    BrowserExtensionDownloadResult result = await HandleBrowserExtensionDownloadOnUiThreadAsync(request);
                    completion.TrySetResult(result);
                }
                catch (Exception ex)
                {
                    completion.TrySetResult(new BrowserExtensionDownloadResult(false, "error", 0, ex.Message));
                }
            });

            if (!queued)
            {
                completion.TrySetResult(new BrowserExtensionDownloadResult(false, "error", 0, "UI dispatcher is not available."));
            }

            return completion.Task;
        }

        private async Task<BrowserExtensionDownloadResult> HandleBrowserExtensionDownloadOnUiThreadAsync(
            BrowserExtensionDownloadRequest request)
        {
            List<string> sourceUris = request.Urls
                .SelectMany(NewDownloadDialogHelpers.GetDownloadSourceUris)
                .Where(NewDownloadDialogHelpers.IsLikelyDownloadSourceUri)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (sourceUris.Count == 0)
            {
                ShowMessage("浏览器扩展没有传入可用链接。", InfoBarSeverity.Warning);
                return new BrowserExtensionDownloadResult(false, "invalid", 0, "No valid download URL was provided.");
            }

            string downloadText = string.Join(Environment.NewLine, sourceUris) + Environment.NewLine;
            if (!_settingsPageViewModel.AdvancedSettings.AutoSubmitFromExtension)
            {
                ShowAndActivate();
                _ = ShowNewDownloadDialogAsync(downloadText);
                return new BrowserExtensionDownloadResult(true, "dialog", sourceUris.Count, "Opened new download dialog.");
            }

            return await AddBrowserExtensionDownloadsAsync(sourceUris);
        }

        private async Task<BrowserExtensionDownloadResult> AddBrowserExtensionDownloadsAsync(IReadOnlyList<string> sourceUris)
        {
            Aria2EngineStartResult startResult = await EnsureAria2StartedAsync();
            if (!startResult.Started)
            {
                ShowMessage(startResult.Message, InfoBarSeverity.Error);
                return new BrowserExtensionDownloadResult(false, "auto", 0, startResult.Message);
            }

            DownloadSettings downloadSettings = _settingsPageViewModel.DownloadSettings;
            List<DownloadTask> addedTasks = [];
            try
            {
                foreach (string sourceUri in sourceUris)
                {
                    DownloadTask task = await _downloadCoordinator.AddDownloadAsync(
                        sourceUri,
                        string.Empty,
                        downloadSettings.DownloadDirectory,
                        downloadSettings.SplitCount);
                    _observedTaskStatuses[task.Gid] = task.Status;
                    addedTasks.Add(task);
                    ShowTaskAddedNotification(task);
                }

                ShowMessage(
                    addedTasks.Count == 1
                        ? "已通过浏览器扩展添加下载任务。"
                        : $"已通过浏览器扩展添加 {addedTasks.Count} 个下载任务。",
                    InfoBarSeverity.Success);
                await RefreshDownloadsAsync();
                UpdateDashboard();
                return new BrowserExtensionDownloadResult(true, "auto", addedTasks.Count, "Created download task.");
            }
            catch (Exception ex)
            {
                ShowMessage($"浏览器扩展添加任务失败：{ex.Message}", InfoBarSeverity.Error);
                return new BrowserExtensionDownloadResult(false, "auto", addedTasks.Count, ex.Message);
            }
        }
    }
}
