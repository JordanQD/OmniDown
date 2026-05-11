namespace OmniDown.Services.Shell;

using System.Runtime.InteropServices;

internal static class WindowVisibilityService
{
    public static void Hide(nint windowHandle)
    {
        ShowWindow(windowHandle, ShowWindowCommand.Hide);
    }

    public static void ShowAndActivate(nint windowHandle)
    {
        ShowWindow(windowHandle, ShowWindowCommand.Show);
        ShowWindow(windowHandle, ShowWindowCommand.Restore);
        SetForegroundWindow(windowHandle);
    }

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(nint hWnd, ShowWindowCommand command);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(nint hWnd);

    private enum ShowWindowCommand
    {
        Hide = 0,
        Show = 5,
        Restore = 9
    }
}
