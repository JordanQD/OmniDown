namespace OmniDown.Services.Shell;

using System.Diagnostics;

internal static class SystemShutdownService
{
    public static void ShutdownNow()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "shutdown.exe",
            ArgumentList = { "/s", "/t", "0" },
            UseShellExecute = false,
            CreateNoWindow = true
        });
    }
}
