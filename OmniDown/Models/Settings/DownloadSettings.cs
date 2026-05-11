namespace OmniDown.Models.Settings;

using OmniDown.Services.Storage;

internal sealed record DownloadSettings(
    string DownloadDirectory,
    int MaxConcurrentDownloads,
    int SplitCount,
    int MaxConnectionPerServer,
    bool ContinueDownloads,
    bool RemoteTime,
    int MaxTries,
    int RetryWaitSeconds,
    bool AutoDeleteStaleRecords,
    bool DeleteTorrentAfterComplete)
{
    public static DownloadSettings Default => new(
        AppPaths.DefaultDownloadDirectory,
        5,
        64,
        16,
        true,
        false,
        0,
        10,
        false,
        false);
}
