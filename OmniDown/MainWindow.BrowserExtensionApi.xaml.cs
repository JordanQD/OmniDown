using Microsoft.UI.Xaml.Controls;
using OmniDown.Models;
using OmniDown.Models.Settings;
using OmniDown.Services.BrowserExtension;
using OmniDown.Services.Downloads;
using OmniDown.Services.Engine;
using OmniDown.Services.Localization;
using OmniDown.Services.Rpc;
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
                ShowUserError(UserErrorContext.BrowserExtensionApi, ex, InfoBarSeverity.Warning);
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
                ShowUserError(UserErrorContext.BrowserExtensionApi, ex, InfoBarSeverity.Warning);
            }
        }

        private Task<BrowserExtensionAddResponse> HandleBrowserExtensionAddAsync(BrowserExtensionAddRequest request)
        {
            TaskCompletionSource<BrowserExtensionAddResponse> completion = new();
            bool queued = DispatcherQueue.TryEnqueue(async () =>
            {
                try
                {
                    BrowserExtensionAddResponse result = await HandleBrowserExtensionAddOnUiThreadAsync(request);
                    completion.TrySetResult(result);
                }
                catch (Exception ex)
                {
                    completion.TrySetResult(new BrowserExtensionAddResponse(
                        "error",
                        null,
                        UserErrorMessages.Create(UserErrorContext.BrowserExtensionAdd, ex).Message));
                }
            });

            if (!queued)
            {
                completion.TrySetResult(new BrowserExtensionAddResponse("error", null, "UI dispatcher is not available."));
            }

            return completion.Task;
        }

        private Task<BrowserExtensionStatResponse> HandleBrowserExtensionStatAsync()
        {
            TaskCompletionSource<BrowserExtensionStatResponse> completion = new();
            bool queued = DispatcherQueue.TryEnqueue(async () =>
            {
                try
                {
                    Aria2GlobalStat stat = _aria2EngineHost.IsRunning
                        ? await _aria2RpcClient.GetGlobalStatAsync()
                        : new Aria2GlobalStat();
                    completion.TrySetResult(new BrowserExtensionStatResponse(
                        stat.DownloadSpeed,
                        stat.UploadSpeed,
                        stat.NumActive,
                        stat.NumWaiting,
                        stat.NumStopped,
                        stat.NumStoppedTotal));
                }
                catch
                {
                    completion.TrySetResult(new BrowserExtensionStatResponse("0", "0", "0", "0", "0", "0"));
                }
            });

            if (!queued)
            {
                completion.TrySetResult(new BrowserExtensionStatResponse("0", "0", "0", "0", "0", "0"));
            }

            return completion.Task;
        }

        private Task<BrowserExtensionActionResponse> PauseAllBrowserExtensionTasksAsync()
        {
            return RunBrowserExtensionTaskActionAsync(
                () => _aria2RpcClient.ForcePauseAllAsync(),
                "ok");
        }

        private Task<BrowserExtensionActionResponse> ResumeAllBrowserExtensionTasksAsync()
        {
            return RunBrowserExtensionTaskActionAsync(
                () => _aria2RpcClient.UnpauseAllAsync(),
                "ok");
        }

        private Task<BrowserExtensionActionResponse> RunBrowserExtensionTaskActionAsync(
            Func<Task> action,
            string successStatus)
        {
            TaskCompletionSource<BrowserExtensionActionResponse> completion = new();
            bool queued = DispatcherQueue.TryEnqueue(async () =>
            {
                if (!_aria2EngineHost.IsRunning)
                {
                    completion.TrySetResult(new BrowserExtensionActionResponse("error", "Engine not running."));
                    return;
                }

                try
                {
                    await action();
                    await RefreshDownloadsAsync();
                    completion.TrySetResult(new BrowserExtensionActionResponse(successStatus, null));
                }
                catch (Exception ex)
                {
                    completion.TrySetResult(new BrowserExtensionActionResponse(
                        "error",
                        UserErrorMessages.Create(UserErrorContext.TaskOperation, ex).Message));
                }
            });

            if (!queued)
            {
                completion.TrySetResult(new BrowserExtensionActionResponse("error", "UI dispatcher is not available."));
            }

            return completion.Task;
        }

        private BrowserExtensionVersionResponse GetBrowserExtensionVersion()
        {
            return new BrowserExtensionVersionResponse(
                GetAppVersionText(),
                _aria2EngineHost.IsRunning ? "running" : "stopped");
        }

        private async Task<BrowserExtensionAddResponse> HandleBrowserExtensionAddOnUiThreadAsync(
            BrowserExtensionAddRequest request)
        {
            List<string> sourceUris = DownloadSourceParser.ParseLines(request.Url)
                .Where(DownloadSourceParser.IsLikelyDownloadSourceUri)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (sourceUris.Count == 0)
            {
                string message = Strings.Get("BrowserExtensionNoValidUrlMessage");
                ShowMessage(message, InfoBarSeverity.Warning);
                return new BrowserExtensionAddResponse("error", null, message);
            }

            string downloadText = string.Join(Environment.NewLine, sourceUris) + Environment.NewLine;
            if (!_settingsPageViewModel.AdvancedSettings.AutoSubmitFromExtension)
            {
                ShowAndActivate();
                _ = ShowNewDownloadDialogAsync(downloadText, request.Filename);
                return new BrowserExtensionAddResponse("queued", null, null);
            }

            return await AddBrowserExtensionDownloadsAsync(sourceUris, request);
        }

        private async Task<BrowserExtensionAddResponse> AddBrowserExtensionDownloadsAsync(
            IReadOnlyList<string> sourceUris,
            BrowserExtensionAddRequest request)
        {
            Aria2EngineStartResult startResult = await EnsureAria2StartedAsync();
            if (!startResult.Started)
            {
                string message = GetEngineStartFailureMessage(startResult);
                ShowEngineStartFailure(startResult);
                return new BrowserExtensionAddResponse("error", null, message);
            }

            DownloadSettings downloadSettings = _settingsPageViewModel.DownloadSettings;
            List<DownloadTask> addedTasks = [];
            try
            {
                foreach (string sourceUri in sourceUris)
                {
                    DownloadTask task = await _downloadCoordinator.AddDownloadAsync(
                        sourceUri,
                        sourceUris.Count == 1 ? request.Filename ?? string.Empty : string.Empty,
                        downloadSettings.DownloadDirectory,
                        downloadSettings.SplitCount,
                        request.Referer,
                        request.Cookie);
                    _observedTaskStatuses[task.Gid] = task.Status;
                    addedTasks.Add(task);
                    ShowTaskAddedNotification(task);
                }

                ShowMessage(
                    addedTasks.Count == 1
                        ? Strings.Get("BrowserExtensionTaskAddedMessage")
                        : Strings.Format("BrowserExtensionTasksAddedMessage", addedTasks.Count),
                    InfoBarSeverity.Success);
                await RefreshDownloadsAsync();
                UpdateDashboard();
                return new BrowserExtensionAddResponse("submitted", addedTasks.Count == 1 ? addedTasks[0].Gid : null, null);
            }
            catch (Exception ex)
            {
                UserErrorPresentation presentation = UserErrorMessages.Create(UserErrorContext.BrowserExtensionAdd, ex);
                ShowMessage(presentation.Message, InfoBarSeverity.Error, presentation.TechnicalDetails);
                return new BrowserExtensionAddResponse(
                    "error",
                    addedTasks.Count == 1 ? addedTasks[0].Gid : null,
                    presentation.Message);
            }
        }
    }
}
