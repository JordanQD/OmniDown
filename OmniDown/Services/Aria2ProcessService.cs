using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace OmniDown.Services;

public sealed class Aria2ProcessService : IDisposable
{
    private Process? _process;

    public bool IsRunning => _process is { HasExited: false };

    public int? ProcessId => IsRunning ? _process?.Id : null;

    public Task<Aria2StartResult> StartAsync(string? executablePath, int rpcPort, string downloadDirectory)
    {
        if (IsRunning)
        {
            return Task.FromResult(Aria2StartResult.Success($"aria2 is already running, PID {_process!.Id}."));
        }

        string resolvedPath = ResolveExecutablePath(executablePath);
        if (string.IsNullOrWhiteSpace(resolvedPath))
        {
            return Task.FromResult(Aria2StartResult.Failure("aria2c.exe was not found. Set a path in Settings or add aria2c to PATH."));
        }

        Directory.CreateDirectory(downloadDirectory);

        var startInfo = new ProcessStartInfo
        {
            FileName = resolvedPath,
            Arguments = BuildArguments(rpcPort, downloadDirectory),
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };

        try
        {
            _process = Process.Start(startInfo);
            return Task.FromResult(
                _process is null
                    ? Aria2StartResult.Failure("aria2 failed to start.")
                    : Aria2StartResult.Success($"aria2 started, PID {_process.Id}."));
        }
        catch (Exception ex)
        {
            _process = null;
            return Task.FromResult(Aria2StartResult.Failure($"aria2 failed to start: {ex.Message}"));
        }
    }

    public void Stop()
    {
        if (!IsRunning)
        {
            return;
        }

        try
        {
            _process!.Kill(entireProcessTree: true);
            _process.Dispose();
        }
        finally
        {
            _process = null;
        }
    }

    public void Dispose()
    {
        Stop();
        _process?.Dispose();
    }

    private static string ResolveExecutablePath(string? executablePath)
    {
        if (!string.IsNullOrWhiteSpace(executablePath) && File.Exists(executablePath))
        {
            return executablePath;
        }

        string pathValue = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (string path in pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            string candidate = Path.Combine(path.Trim(), "aria2c.exe");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return string.Empty;
    }

    private static string BuildArguments(int rpcPort, string downloadDirectory)
    {
        return string.Join(
            " ",
            "--enable-rpc=true",
            "--rpc-listen-all=false",
            $"--rpc-listen-port={rpcPort}",
            "--continue=true",
            "--max-concurrent-downloads=5",
            "--split=8",
            "--min-split-size=1M",
            $"--dir=\"{downloadDirectory}\"");
    }
}

public sealed record Aria2StartResult(bool Started, string Message)
{
    public static Aria2StartResult Success(string message) => new(true, message);

    public static Aria2StartResult Failure(string message) => new(false, message);
}
