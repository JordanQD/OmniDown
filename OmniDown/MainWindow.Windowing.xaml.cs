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
    public sealed partial class MainWindow
    {
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
            DisplayArea displayArea = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
            RectInt32 workArea = displayArea.WorkArea;
            int defaultWidth = Math.Min(1200, workArea.Width);
            int defaultHeight = Math.Min(760, workArea.Height);

            AppWindow.Resize(new SizeInt32(defaultWidth, defaultHeight));
            int x = workArea.X + Math.Max(0, (workArea.Width - defaultWidth) / 2);
            int y = workArea.Y + Math.Max(0, (workArea.Height - defaultHeight) / 2);
            AppWindow.Move(new PointInt32(x, y));
        }

        private void ApplyWindowPlacementOrDefault()
        {
            ConfigureDefaultWindowSize();
            GeneralSettings settings = _settingsPageViewModel.GeneralSettings;
            if (!settings.RestoreWindowPlacement ||
                settings.WindowWidth <= 0 ||
                settings.WindowHeight <= 0)
            {
                return;
            }

            DisplayArea displayArea = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
            RectInt32 workArea = displayArea.WorkArea;
            int width = Math.Clamp(settings.WindowWidth, 1, Math.Max(workArea.Width, 1));
            int height = Math.Clamp(settings.WindowHeight, 1, Math.Max(workArea.Height, 1));
            int x = Math.Clamp(settings.WindowX, workArea.X, Math.Max(workArea.X, workArea.X + workArea.Width - width));
            int y = Math.Clamp(settings.WindowY, workArea.Y, Math.Max(workArea.Y, workArea.Y + workArea.Height - height));

            AppWindow.Resize(new SizeInt32(width, height));
            AppWindow.Move(new PointInt32(x, y));
        }

        private void SaveWindowPlacementSettings()
        {
            if (!_settingsPageViewModel.GeneralSettings.RestoreWindowPlacement)
            {
                return;
            }

            SizeInt32 size = AppWindow.Size;
            PointInt32 position = AppWindow.Position;
            _settingsPageViewModel.UpdateWindowPlacement(position.X, position.Y, size.Width, size.Height);
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

    }
}
