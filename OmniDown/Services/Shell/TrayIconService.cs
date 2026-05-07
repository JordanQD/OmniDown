using System;
using System.Runtime.InteropServices;

namespace OmniDown.Services.Shell;

internal sealed class TrayIconService : IDisposable
{
    private const int IconId = 1;
    private const uint ShowCommandId = 1001;
    private const uint ExitCommandId = 1002;
    private readonly nint _windowHandle;
    private readonly nint _iconHandle;
    private readonly SubclassProc _subclassProc;
    private readonly uint _callbackMessage;
    private string _tooltipText = "OmniDown";
    private string _showText = "Show OmniDown";
    private string _exitText = "Exit";
    private bool _isDisposed;

    public event EventHandler? ShowRequested;
    public event EventHandler? ExitRequested;

    public TrayIconService(nint windowHandle, string? iconPath)
    {
        _windowHandle = windowHandle;
        _subclassProc = WindowSubclassProc;
        _iconHandle = LoadTrayIcon(iconPath);
        _callbackMessage = RegisterWindowMessage("OmniDown.TrayIcon.Callback");
        if (_callbackMessage == 0)
        {
            _callbackMessage = 0x8F41;
        }

        if (_windowHandle == 0 || _iconHandle == 0)
        {
            return;
        }

        SetWindowSubclass(_windowHandle, _subclassProc, 1, 0);
        Shell_NotifyIcon(NotifyIconMessage.Add, CreateNotifyIconData());
    }

    public void UpdateLabels(string tooltipText, string showText, string exitText)
    {
        _tooltipText = string.IsNullOrWhiteSpace(tooltipText) ? "OmniDown" : tooltipText;
        _showText = string.IsNullOrWhiteSpace(showText) ? "Show OmniDown" : showText;
        _exitText = string.IsNullOrWhiteSpace(exitText) ? "Exit" : exitText;

        if (_windowHandle != 0 && _iconHandle != 0)
        {
            Shell_NotifyIcon(NotifyIconMessage.Modify, CreateNotifyIconData());
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        if (_windowHandle != 0)
        {
            Shell_NotifyIcon(NotifyIconMessage.Delete, CreateNotifyIconData());
            RemoveWindowSubclass(_windowHandle, _subclassProc, 1);
        }

        if (_iconHandle != 0)
        {
            DestroyIcon(_iconHandle);
        }
    }

    private nint WindowSubclassProc(nint hWnd, uint message, nint wParam, nint lParam, nuint subclassId, nuint refData)
    {
        if (message == _callbackMessage)
        {
            uint trayMessage = (uint)lParam.ToInt64();
            if (trayMessage == WindowMessage.LeftButtonUp)
            {
                ShowRequested?.Invoke(this, EventArgs.Empty);
                return 0;
            }

            if (trayMessage is WindowMessage.RightButtonUp or WindowMessage.ContextMenu)
            {
                ShowTrayMenu();
                return 0;
            }
        }

        return DefSubclassProc(hWnd, message, wParam, lParam);
    }

    private void ShowTrayMenu()
    {
        nint menu = CreatePopupMenu();
        if (menu == 0)
        {
            return;
        }

        try
        {
            AppendMenu(menu, MenuFlags.String, ShowCommandId, _showText);
            AppendMenu(menu, MenuFlags.Separator, 0, null);
            AppendMenu(menu, MenuFlags.String, ExitCommandId, _exitText);

            GetCursorPos(out Point point);
            SetForegroundWindow(_windowHandle);
            uint command = TrackPopupMenu(
                menu,
                TrackPopupMenuFlags.ReturnCommand | TrackPopupMenuFlags.Nonotify,
                point.X,
                point.Y,
                0,
                _windowHandle,
                nint.Zero);

            if (command == ShowCommandId)
            {
                ShowRequested?.Invoke(this, EventArgs.Empty);
            }
            else if (command == ExitCommandId)
            {
                ExitRequested?.Invoke(this, EventArgs.Empty);
            }
        }
        finally
        {
            DestroyMenu(menu);
        }
    }

    private NotifyIconData CreateNotifyIconData()
    {
        NotifyIconData data = new()
        {
            cbSize = (uint)Marshal.SizeOf<NotifyIconData>(),
            hWnd = _windowHandle,
            uID = IconId,
            uFlags = NotifyIconFlags.Message | NotifyIconFlags.Icon | NotifyIconFlags.Tip,
            uCallbackMessage = _callbackMessage,
            hIcon = _iconHandle,
            szTip = _tooltipText
        };
        return data;
    }

    private static nint LoadTrayIcon(string? iconPath)
    {
        if (!string.IsNullOrWhiteSpace(iconPath))
        {
            nint icon = LoadImage(0, iconPath, ImageType.Icon, 0, 0, LoadImageFlags.LoadFromFile | LoadImageFlags.DefaultSize);
            if (icon != 0)
            {
                return icon;
            }
        }

        return LoadIcon(0, new nint(32512));
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIcon(NotifyIconMessage dwMessage, in NotifyIconData lpData);

    [DllImport("comctl32.dll", SetLastError = true)]
    private static extern bool SetWindowSubclass(nint hWnd, SubclassProc pfnSubclass, nuint uIdSubclass, nuint dwRefData);

    [DllImport("comctl32.dll", SetLastError = true)]
    private static extern bool RemoveWindowSubclass(nint hWnd, SubclassProc pfnSubclass, nuint uIdSubclass);

    [DllImport("comctl32.dll")]
    private static extern nint DefSubclassProc(nint hWnd, uint uMsg, nint wParam, nint lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint LoadImage(nint hinst, string lpszName, ImageType uType, int cxDesired, int cyDesired, LoadImageFlags fuLoad);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint RegisterWindowMessage(string lpString);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint LoadIcon(nint hInstance, nint lpIconName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(nint hIcon);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool AppendMenu(nint hMenu, MenuFlags uFlags, uint uIDNewItem, string? lpNewItem);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyMenu(nint hMenu);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetCursorPos(out Point lpPoint);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint TrackPopupMenu(
        nint hMenu,
        TrackPopupMenuFlags uFlags,
        int x,
        int y,
        int nReserved,
        nint hWnd,
        nint prcRect);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(nint hWnd);

    private delegate nint SubclassProc(nint hWnd, uint uMsg, nint wParam, nint lParam, nuint uIdSubclass, nuint dwRefData);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NotifyIconData
    {
        public uint cbSize;
        public nint hWnd;
        public int uID;
        public NotifyIconFlags uFlags;
        public uint uCallbackMessage;
        public nint hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
        public uint dwState;
        public uint dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;
        public uint uTimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;
        public uint dwInfoFlags;
        public Guid guidItem;
        public nint hBalloonIcon;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    private static class WindowMessage
    {
        public const uint LeftButtonUp = 0x0202;
        public const uint RightButtonUp = 0x0205;
        public const uint ContextMenu = 0x007B;
    }

    private enum NotifyIconMessage : uint
    {
        Add = 0,
        Modify = 1,
        Delete = 2
    }

    [Flags]
    private enum NotifyIconFlags : uint
    {
        Message = 0x00000001,
        Icon = 0x00000002,
        Tip = 0x00000004
    }

    private enum ImageType : uint
    {
        Icon = 1
    }

    [Flags]
    private enum LoadImageFlags : uint
    {
        DefaultSize = 0x00000040,
        LoadFromFile = 0x00000010
    }

    [Flags]
    private enum MenuFlags : uint
    {
        String = 0x00000000,
        Separator = 0x00000800
    }

    [Flags]
    private enum TrackPopupMenuFlags : uint
    {
        ReturnCommand = 0x0100,
        Nonotify = 0x0080
    }
}
