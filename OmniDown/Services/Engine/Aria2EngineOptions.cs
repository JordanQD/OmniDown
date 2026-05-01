namespace OmniDown.Services.Engine;

public sealed record Aria2EngineOptions(
    string? ExecutablePath,
    int RpcPort,
    string DownloadDirectory,
    string RpcSecret,
    bool UseSystemProxy);

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
