namespace OmniDown.Services.Engine;

using OmniDown.Models.Settings;

public sealed record Aria2EngineOptions(
    string? ExecutablePath,
    int RpcPort,
    string DownloadDirectory,
    string RpcSecret,
    bool UseSystemProxy,
    int MaxConcurrentDownloads,
    int SplitCount,
    int MaxConnectionPerServer,
    bool ContinueDownloads,
    bool RemoteTime,
    int MaxTries,
    int RetryWaitSeconds,
    BitTorrentSettings BitTorrentSettings);

public sealed record Aria2EngineStartResult(
    bool Started,
    string Message,
    string? ResolvedExecutablePath,
    int? ProcessId)
{
    public static Aria2EngineStartResult Success(string message, string resolvedExecutablePath, int processId) =>
        new(true, message, resolvedExecutablePath, processId);

    public static Aria2EngineStartResult Failure(string message) =>
        new(false, message, null, null);
}
