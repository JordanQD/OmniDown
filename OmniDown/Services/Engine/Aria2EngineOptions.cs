namespace OmniDown.Services.Engine;

using OmniDown.Models.Settings;

public sealed record Aria2EngineOptions(
    string? ExecutablePath,
    Aria2EngineType EngineType,
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
    NetworkSettings NetworkSettings,
    BitTorrentSettings BitTorrentSettings,
    AdvancedSettings AdvancedSettings);

public enum Aria2EngineStartFailureKind
{
    None,
    ExecutableNotFound,
    ProcessStartFailed,
    RpcPortNotReady,
    RpcUnavailable
}

public sealed record Aria2EngineStartResult(
    bool Started,
    Aria2EngineStartFailureKind FailureKind,
    string TechnicalDetails,
    string? ResolvedExecutablePath,
    int? ProcessId)
{
    public static Aria2EngineStartResult Success(string technicalDetails, string resolvedExecutablePath, int processId) =>
        new(true, Aria2EngineStartFailureKind.None, technicalDetails, resolvedExecutablePath, processId);

    public static Aria2EngineStartResult Failure(Aria2EngineStartFailureKind failureKind, string technicalDetails) =>
        new(false, failureKind, technicalDetails, null, null);
}
