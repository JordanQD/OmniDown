namespace OmniDown.Models.Settings;

internal sealed record SpeedLimitSettings(
    bool DownloadEnabled,
    double DownloadValue,
    string DownloadUnit,
    bool UploadEnabled,
    double UploadValue,
    string UploadUnit)
{
    public static SpeedLimitSettings Default { get; } = new(
        DownloadEnabled: false,
        DownloadValue: 1024,
        DownloadUnit: "KB/s",
        UploadEnabled: false,
        UploadValue: 1024,
        UploadUnit: "KB/s");
}
