using System;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Storage;

namespace OmniDown.Services.Storage;

public static class AppPaths
{
    private static readonly Guid DownloadsFolderId = new("374DE290-123F-4565-9164-39C4925E467B");
    private static readonly Lazy<string> StableLocalDataDirectory = new(CreateStableLocalDataDirectory);
    private static readonly Lazy<string> PackageEngineDirectory = new(CreatePackageEngineDirectory);

    public static string LocalDataDirectory => StableLocalDataDirectory.Value;

    public static string EngineDirectory => PackageEngineDirectory.Value;

    public static string LogDirectory => Path.Combine(LocalDataDirectory, "logs");

    public static string AppLogPath => Path.Combine(LogDirectory, "omnidown.log");

    public static string Aria2LogPath => Path.Combine(LogDirectory, "aria2c.log");

    public static string Ed2kBootstrapDirectory => Path.Combine(LocalDataDirectory, "ed2k");

    public static string Ed2kServerMetPath => Path.Combine(Ed2kBootstrapDirectory, "server.met");

    public static string Ed2kNodesDatPath => Path.Combine(Ed2kBootstrapDirectory, "nodes.dat");

    public static string Ed2kSearchDirectory => Path.Combine(Ed2kBootstrapDirectory, "search");

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

    private static string CreateStableLocalDataDirectory()
    {
        string stableDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OmniDown");

        Directory.CreateDirectory(stableDirectory);
        TryMigrateLegacyPackageData(stableDirectory);
        return stableDirectory;
    }

    public static string NormalizeImportedEnginePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        try
        {
            string fullPath = Path.GetFullPath(path);
            string managedEngineDirectory = EngineDirectory;
            string managedPrefix = Path.GetFullPath(managedEngineDirectory)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (fullPath.StartsWith(managedPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return File.Exists(fullPath) ? fullPath : string.Empty;
            }

            string legacyEngineDirectory = GetLegacyEngineDirectory();
            string legacyPrefix = Path.GetFullPath(legacyEngineDirectory)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(legacyPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }

            string relativePath = Path.GetRelativePath(legacyEngineDirectory, fullPath);
            string migratedPath = Path.Combine(EngineDirectory, relativePath);
            return File.Exists(migratedPath) ? migratedPath : path;
        }
        catch
        {
            return path;
        }
    }

    private static string CreatePackageEngineDirectory()
    {
        try
        {
            string packageDirectory = Path.Combine(ApplicationData.Current.LocalFolder.Path, "engines", "aria2");
            Directory.CreateDirectory(packageDirectory);
            MigrateLegacyEngines(packageDirectory);
            return packageDirectory;
        }
        catch
        {
            // Unpackaged runs have no package-managed LocalState.
            string fallbackDirectory = GetLegacyEngineDirectory();
            Directory.CreateDirectory(fallbackDirectory);
            return fallbackDirectory;
        }
    }

    private static void MigrateLegacyEngines(string packageEngineDirectory)
    {
        string legacyEngineDirectory = GetLegacyEngineDirectory();
        if (!Directory.Exists(legacyEngineDirectory) ||
            string.Equals(
                Path.GetFullPath(legacyEngineDirectory).TrimEnd(Path.DirectorySeparatorChar),
                Path.GetFullPath(packageEngineDirectory).TrimEnd(Path.DirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        try
        {
            foreach (string sourceFile in Directory.EnumerateFiles(legacyEngineDirectory, "*", SearchOption.AllDirectories))
            {
                string relativePath = Path.GetRelativePath(legacyEngineDirectory, sourceFile);
                string targetFile = Path.Combine(packageEngineDirectory, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);
                if (!File.Exists(targetFile))
                {
                    File.Copy(sourceFile, targetFile);
                }
            }

            Directory.Delete(legacyEngineDirectory, recursive: true);
        }
        catch
        {
            // Keep the legacy copy if migration or cleanup cannot finish safely.
        }
    }

    private static string GetLegacyEngineDirectory() =>
        Path.Combine(LocalDataDirectory, "engines", "aria2");

    private static void TryMigrateLegacyPackageData(string stableDirectory)
    {
        try
        {
            string legacyDirectory = ApplicationData.Current.LocalFolder.Path;
            if (string.Equals(
                Path.GetFullPath(legacyDirectory).TrimEnd(Path.DirectorySeparatorChar),
                Path.GetFullPath(stableDirectory).TrimEnd(Path.DirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase) ||
                !Directory.Exists(legacyDirectory))
            {
                return;
            }

            CopyMissingFiles(legacyDirectory, stableDirectory, skipEngineDirectory: true);
        }
        catch
        {
            // Package identity may be unavailable. The stable Win32 path remains usable.
        }
    }

    private static void CopyMissingFiles(
        string sourceDirectory,
        string targetDirectory,
        bool skipEngineDirectory = false)
    {
        foreach (string sourceFile in Directory.EnumerateFiles(sourceDirectory))
        {
            string targetFile = Path.Combine(targetDirectory, Path.GetFileName(sourceFile));
            if (!File.Exists(targetFile))
            {
                File.Copy(sourceFile, targetFile);
            }
        }

        foreach (string sourceSubdirectory in Directory.EnumerateDirectories(sourceDirectory))
        {
            if (skipEngineDirectory &&
                string.Equals(Path.GetFileName(sourceSubdirectory), "engines", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string targetSubdirectory = Path.Combine(targetDirectory, Path.GetFileName(sourceSubdirectory));
            Directory.CreateDirectory(targetSubdirectory);
            CopyMissingFiles(sourceSubdirectory, targetSubdirectory);
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
