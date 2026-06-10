using Microsoft.UI.Xaml.Controls;
using OmniDown.Services.Settings;
using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using WinRT.Interop;

namespace OmniDown;

/// <summary>
/// ISettingsHostActions implementation — bridges settings pages to MainWindow system operations.
/// </summary>
public sealed partial class MainWindow : ISettingsHostActions
{
    async Task<string?> ISettingsHostActions.PickDownloadDirectoryAsync()
    {
        FolderPicker picker = new()
        {
            SuggestedStartLocation = PickerLocationId.Downloads
        };
        picker.FileTypeFilter.Add("*");
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));

        StorageFolder? folder = await picker.PickSingleFolderAsync();
        return folder?.Path;
    }

    async Task ISettingsHostActions.OpenUriAsync(Uri uri)
    {
        await Launcher.LaunchUriAsync(uri);
    }

    Task ISettingsHostActions.OpenFolderAsync(string path)
    {
        OpenShellTarget(path);
        return Task.CompletedTask;
    }

    async Task ISettingsHostActions.RestartAriaAsync()
    {
        await StopAriaAsync(showMessage: false);
        await StartAriaAsync();
    }

    async Task ISettingsHostActions.StartOrStopAriaAsync()
    {
        if (_aria2EngineHost.IsRunning)
        {
            await StopAriaAsync();
        }
        else
        {
            await StartAriaAsync();
        }
    }

    async Task ISettingsHostActions.CheckEngineUpdateAsync()
    {
        await CheckEngineUpdateAsync(isManual: true);
    }

    void ISettingsHostActions.ShowMessage(string message, InfoBarSeverity severity)
    {
        ShowMessage(message, severity);
    }

    void ISettingsHostActions.DismissSettingsTeachingTips()
    {
        DismissSettingsTeachingTips();
    }
}
