namespace OmniDown.Models.Settings;

public sealed record Ed2kSettings(
    int ListenPort,
    int UdpListenPort,
    int UploadSlots,
    string ServerListUrl,
    string KadBootstrapUrl,
    bool KadBootstrapEnabled,
    string ServerList,
    string DisabledServerList,
    bool AutoSyncEnabled,
    string SyncInterval,
    long LastSyncTime,
    string SearchKeyword,
    string FileType,
    int MinSources,
    int SearchTimeout)
{
    public const int DefaultListenPort = 29140;
    public const int DefaultUdpListenPort = 29150;
    public const int DefaultUploadSlots = 3;
    public const string DefaultServerListUrl = "https://upd.emule-security.org/server.met";
    public const string DefaultKadBootstrapUrl = "https://upd.emule-security.org/nodes.dat";
    public const string DefaultSyncInterval = "Daily";
    public const string DefaultFileType = "Any";
    public const int DefaultMinSources = 5;
    public const int DefaultSearchTimeout = 20;

    public static Ed2kSettings Default => new(
        DefaultListenPort,
        DefaultUdpListenPort,
        DefaultUploadSlots,
        DefaultServerListUrl,
        DefaultKadBootstrapUrl,
        true,
        string.Empty,
        string.Empty,
        true,
        DefaultSyncInterval,
        0,
        string.Empty,
        DefaultFileType,
        DefaultMinSources,
        DefaultSearchTimeout);
}
