namespace OmniDown.Models.Settings;

public sealed record NetworkSettings(
    bool UseSystemProxy,
    bool CustomProxyEnabled,
    string ProxyServer,
    string ProxyUsername,
    string ProxyPassword,
    string ProxyBypass,
    bool ProxyDownloads,
    bool ProxyTrackers,
    bool EnableUpnp,
    int ListenPort,
    int DhtListenPort,
    string UserAgent,
    int ConnectTimeoutSeconds,
    int TimeoutSeconds,
    string FileAllocation)
{
    public const string DefaultProxyBypass = "localhost;127.*;192.168.*;10.*;172.16.*;172.17.*;172.18.*;172.19.*;172.20.*;172.21.*;172.22.*;172.23.*;172.24.*;172.25.*;172.26.*;172.27.*;172.28.*;172.29.*;172.30.*;172.31.*;<local>";

    public static NetworkSettings Default => new(
        UseSystemProxy: true,
        CustomProxyEnabled: false,
        ProxyServer: string.Empty,
        ProxyUsername: string.Empty,
        ProxyPassword: string.Empty,
        ProxyBypass: DefaultProxyBypass,
        ProxyDownloads: true,
        ProxyTrackers: false,
        EnableUpnp: false,
        ListenPort: 6881,
        DhtListenPort: 6881,
        UserAgent: string.Empty,
        ConnectTimeoutSeconds: 10,
        TimeoutSeconds: 10,
        FileAllocation: "none");
}
