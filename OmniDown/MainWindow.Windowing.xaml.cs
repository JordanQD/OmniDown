using Microsoft.UI;
using Microsoft.UI.Windowing;
using OmniDown.Models.Settings;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Graphics;
using WinRT.Interop;

namespace OmniDown
{
    public sealed partial class MainWindow
    {
        private const int MinimumWindowWidth = 900;
        private const int MinimumWindowHeight = 680;
        private const uint WmGetMinMaxInfo = 0x0024;
        private const nuint MinimumSizeSubclassId = 2;
        private WindowSubclassProc? _minimumSizeSubclassProc;

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
            SizeInt32 minimumSize = GetMinimumWindowSizePixels();
            int defaultWidth = Math.Min(Math.Max(1200, minimumSize.Width), workArea.Width);
            int defaultHeight = Math.Min(Math.Max(760, minimumSize.Height), workArea.Height);

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
            SizeInt32 minimumSize = GetMinimumWindowSizePixels();
            int width = Math.Clamp(
                settings.WindowWidth,
                Math.Min(minimumSize.Width, workArea.Width),
                Math.Max(workArea.Width, 1));
            int height = Math.Clamp(
                settings.WindowHeight,
                Math.Min(minimumSize.Height, workArea.Height),
                Math.Max(workArea.Height, 1));
            int x = Math.Clamp(settings.WindowX, workArea.X, Math.Max(workArea.X, workArea.X + workArea.Width - width));
            int y = Math.Clamp(settings.WindowY, workArea.Y, Math.Max(workArea.Y, workArea.Y + workArea.Height - height));

            AppWindow.Resize(new SizeInt32(width, height));
            AppWindow.Move(new PointInt32(x, y));
        }

        private void ConfigureMinimumWindowSize()
        {
            if (_windowHandle == 0 || _minimumSizeSubclassProc is not null)
            {
                return;
            }

            _minimumSizeSubclassProc = MinimumSizeWindowSubclassProc;
            _ = SetWindowSubclass(
                _windowHandle,
                _minimumSizeSubclassProc,
                MinimumSizeSubclassId,
                0);
        }

        private void RemoveMinimumWindowSizeHook()
        {
            if (_windowHandle == 0 || _minimumSizeSubclassProc is null)
            {
                return;
            }

            _ = RemoveWindowSubclass(
                _windowHandle,
                _minimumSizeSubclassProc,
                MinimumSizeSubclassId);
            _minimumSizeSubclassProc = null;
        }

        private SizeInt32 GetMinimumWindowSizePixels()
        {
            uint dpi = _windowHandle == 0 ? 96 : GetDpiForWindow(_windowHandle);
            if (dpi == 0)
            {
                dpi = 96;
            }

            int width = (int)Math.Ceiling(MinimumWindowWidth * dpi / 96d);
            int height = (int)Math.Ceiling(MinimumWindowHeight * dpi / 96d);
            DisplayArea displayArea = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
            RectInt32 workArea = displayArea.WorkArea;
            return new SizeInt32(
                Math.Min(width, Math.Max(workArea.Width, 1)),
                Math.Min(height, Math.Max(workArea.Height, 1)));
        }

        private nint MinimumSizeWindowSubclassProc(
            nint windowHandle,
            uint message,
            nint wParam,
            nint lParam,
            nuint subclassId,
            nuint refData)
        {
            if (message == WmGetMinMaxInfo)
            {
                MinMaxInfo minMaxInfo = Marshal.PtrToStructure<MinMaxInfo>(lParam);
                SizeInt32 minimumSize = GetMinimumWindowSizePixels();
                minMaxInfo.MinimumTrackSize.X = minimumSize.Width;
                minMaxInfo.MinimumTrackSize.Y = minimumSize.Height;
                Marshal.StructureToPtr(minMaxInfo, lParam, false);
                return 0;
            }

            return DefSubclassProc(windowHandle, message, wParam, lParam);
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

        [StructLayout(LayoutKind.Sequential)]
        private struct NativePoint
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MinMaxInfo
        {
            public NativePoint Reserved;
            public NativePoint MaximumSize;
            public NativePoint MaximumPosition;
            public NativePoint MinimumTrackSize;
            public NativePoint MaximumTrackSize;
        }

        private delegate nint WindowSubclassProc(
            nint windowHandle,
            uint message,
            nint wParam,
            nint lParam,
            nuint subclassId,
            nuint refData);

        [DllImport("comctl32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowSubclass(
            nint windowHandle,
            WindowSubclassProc subclassProc,
            nuint subclassId,
            nuint refData);

        [DllImport("comctl32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RemoveWindowSubclass(
            nint windowHandle,
            WindowSubclassProc subclassProc,
            nuint subclassId);

        [DllImport("comctl32.dll")]
        private static extern nint DefSubclassProc(
            nint windowHandle,
            uint message,
            nint wParam,
            nint lParam);

        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(nint windowHandle);

    }
}
