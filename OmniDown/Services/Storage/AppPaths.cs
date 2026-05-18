using System;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Storage;

namespace OmniDown.Services.Storage;

public static class AppPaths
{
    private static readonly Guid DownloadsFolderId = new("374DE290-123F-4565-9164-39C4925E467B");

    public static string LocalDataDirectory
    {
        get
        {
            try
            {
                return ApplicationData.Current.LocalFolder.Path;
            }
            catch
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "OmniDown");
            }
        }
    }

    public static string LogDirectory => Path.Combine(LocalDataDirectory, "logs");

    public static string AppLogPath => Path.Combine(LogDirectory, "omnidown.log");

    public static string Aria2LogPath => Path.Combine(LogDirectory, "aria2c.log");

    public static string DefaultDownloadDirectory
    {
        get
        {
            string downloadsPath = GetKnownFolderPath(DownloadsFolderId);
            if (!string.IsNullOrWhiteSpace(downloadsPath))
            {
                return downloadsPath;
            }

            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads");
        }
    }

    private static string GetKnownFolderPath(Guid folderId)
    {
        IntPtr pathPointer = IntPtr.Zero;
        try
        {
            int result = SHGetKnownFolderPath(folderId, 0, IntPtr.Zero, out pathPointer);
            return result == 0 && pathPointer != IntPtr.Zero
                ? Marshal.PtrToStringUni(pathPointer) ?? string.Empty
                : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
        finally
        {
            if (pathPointer != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(pathPointer);
            }
        }
    }

    [DllImport("shell32.dll")]
    private static extern int SHGetKnownFolderPath(
        [MarshalAs(UnmanagedType.LPStruct)] Guid rfid,
        uint dwFlags,
        IntPtr hToken,
        out IntPtr ppszPath);
}
