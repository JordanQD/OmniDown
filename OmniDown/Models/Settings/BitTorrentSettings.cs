namespace OmniDown.Models.Settings;

public sealed record BitTorrentSettings(
    bool IsEnabled,
    bool AutoDownloadContent,
    bool ForceEncryption,
    bool KeepSeeding,
    double SeedRatio,
    int SeedTimeMinutes,
    int MaxPeers,
    string ListenPort,
    string TrackerSourceUrl,
    string[] SelectedTrackerSourceUrls,
    string[] CustomTrackerUrls,
    string TrackerList,
    string DisabledTrackerList,
    bool AutoSyncTracker,
    long LastSyncTrackerTime)
{
    public const string DefaultTrackerSourceUrl = "https://raw.githubusercontent.com/ngosang/trackerslist/master/trackers_all.txt";

    public static BitTorrentSettings Default => new(
        true,
        true,
        false,
        false,
        1.0,
        60,
        128,
        "6881-6999",
        DefaultTrackerSourceUrl,
        [DefaultTrackerSourceUrl],
        [],
        string.Empty,
        string.Empty,
        false,
        0);
}
