using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace OmniDown.Services.Settings;

/// <summary>
/// System-level operations that settings pages request from the host (MainWindow).
/// Settings pages must not access MainWindow directly; they channel all
/// host-side side-effects through this interface.
/// </summary>
public interface ISettingsHostActions
{
    /// <summary>
    /// Open a folder picker and return the selected path, or null if cancelled.
    /// </summary>
    Task<string?> PickDownloadDirectoryAsync();

    /// <summary>
    /// Open a URI in the default browser or system handler.
    /// </summary>
    Task OpenUriAsync(Uri uri);

    /// <summary>
    /// Open a folder in File Explorer.
    /// </summary>
    Task OpenFolderAsync(string path);

    /// <summary>
    /// Stop aria2 (if running), then start it with the latest settings.
    /// </summary>
    Task RestartAriaAsync();

    /// <summary>
    /// Start aria2 if stopped, or stop it if running.
    /// </summary>
    Task StartOrStopAriaAsync();

    /// <summary>
    /// Manually check for engine updates.
    /// </summary>
    Task CheckEngineUpdateAsync();

    /// <summary>
    /// Show a message in the app's global InfoBar.
    /// </summary>
    void ShowMessage(string message, InfoBarSeverity severity);

    /// <summary>
    /// Dismiss any open settings-related TeachingTip overlays.
    /// </summary>
    void DismissSettingsTeachingTips();
}
