using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace OmniDown.Services.Engine;

public sealed class Aria2EngineHost : IDisposable
{
    private Process? _process;
    private readonly Queue<string> _recentOutput = new();
    private readonly Queue<string> _terminalOutput = new();

    public bool IsRunning => _process is { HasExited: false };

    public int? ProcessId => IsRunning ? _process?.Id : null;

    public string DiagnosticText { get; private set; } = "No aria2 process has been started.";

    public string TerminalText => _terminalOutput.Count == 0
        ? "aria2 terminal output will appear here."
        : string.Join(Environment.NewLine, _terminalOutput);

    public void ClearTerminal()
    {
        _terminalOutput.Clear();
        DiagnosticText = IsRunning ? "aria2 terminal cleared." : DiagnosticText;
    }

    public async Task<Aria2EngineStartResult> StartAsync(Aria2EngineOptions options)
    {
        if (IsRunning)
        {
            return Aria2EngineStartResult.Success(
                $"aria2 is already running, PID {_process!.Id}.",
                _process.StartInfo.FileName,
                _process.Id);
        }

        string resolvedPath = ResolveExecutablePath(options.ExecutablePath);
        if (string.IsNullOrWhiteSpace(resolvedPath))
        {
            return Aria2EngineStartResult.Failure(
                "aria2c.exe was not found. Set a path in Settings, add aria2c to PATH, or place it under Engines\\aria2.");
        }

        Directory.CreateDirectory(options.DownloadDirectory);
        string appDataDirectory = GetAppDataDirectory();
        Directory.CreateDirectory(appDataDirectory);

        var startInfo = new ProcessStartInfo
        {
            FileName = resolvedPath,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };

        foreach (string argument in BuildArguments(options, appDataDirectory))
        {
            startInfo.ArgumentList.Add(argument);
        }

        try
        {
            _recentOutput.Clear();
            _terminalOutput.Clear();
            DiagnosticText = $"Starting {resolvedPath}";
            AppendTerminalLine($"[{DateTime.Now:T}] Starting {resolvedPath}");
            _process = Process.Start(startInfo);
            if (_process is null)
            {
                DiagnosticText = "Process.Start returned null.";
                return Aria2EngineStartResult.Failure("aria2 failed to start.");
            }

            _process.EnableRaisingEvents = true;
            _process.OutputDataReceived += Process_OutputDataReceived;
            _process.ErrorDataReceived += Process_OutputDataReceived;
            _process.Exited += Process_Exited;
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            bool ready = await WaitForRpcPortAsync(options.RpcPort, _process);
            if (!ready)
            {
                string exitMessage = _process.HasExited
                    ? $" aria2 exited with code {_process.ExitCode}. {DiagnosticText}"
                    : string.Empty;
                Stop();
                return Aria2EngineStartResult.Failure($"aria2 RPC port did not become ready.{exitMessage}");
            }

            DiagnosticText = $"aria2 RPC is listening on 127.0.0.1:{options.RpcPort}.";
            AppendTerminalLine($"[{DateTime.Now:T}] RPC listening on 127.0.0.1:{options.RpcPort}");
            return Aria2EngineStartResult.Success($"aria2 started, PID {_process.Id}.", resolvedPath, _process.Id);
        }
        catch (Exception ex)
        {
            _process = null;
            DiagnosticText = ex.Message;
            AppendTerminalLine($"[{DateTime.Now:T}] Start failed: {ex.Message}");
            return Aria2EngineStartResult.Failure($"aria2 failed to start: {ex.Message}");
        }
    }

    public void Stop()
    {
        if (_process is null)
        {
            return;
        }

        try
        {
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
            }

            _process.Dispose();
        }
        finally
        {
            _process = null;
        }
    }

    private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.Data))
        {
            return;
        }

        _recentOutput.Enqueue(e.Data);
        while (_recentOutput.Count > 6)
        {
            _recentOutput.Dequeue();
        }

        AppendTerminalLine(e.Data);
        DiagnosticText = string.Join(" | ", _recentOutput);
    }

    private void Process_Exited(object? sender, EventArgs e)
    {
        if (_process is null)
        {
            return;
        }

        string message = $"aria2 exited with code {_process.ExitCode}. {string.Join(" | ", _recentOutput)}";
        DiagnosticText = message;
        AppendTerminalLine($"[{DateTime.Now:T}] {message}");
    }

    private void AppendTerminalLine(string line)
    {
        _terminalOutput.Enqueue(line);
        while (_terminalOutput.Count > 300)
        {
            _terminalOutput.Dequeue();
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

        foreach (string candidate in GetBundledCandidates())
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
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

    private static string[] GetBundledCandidates()
    {
        string architecture = RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
        string appBase = AppContext.BaseDirectory;
        string executableDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? appBase;
        string? appBaseParent = Directory.GetParent(appBase.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))?.FullName;
        string? executableDirectoryParent = Directory.GetParent(executableDirectory)?.FullName;

        return
        [
            Path.Combine(appBase, "Engines", "aria2", $"win-{architecture}", "aria2c.exe"),
            Path.Combine(appBase, "Engines", "aria2", "aria2c.exe"),
            Path.Combine(executableDirectory, "Engines", "aria2", $"win-{architecture}", "aria2c.exe"),
            Path.Combine(executableDirectory, "Engines", "aria2", "aria2c.exe"),
            Path.Combine(appBaseParent ?? appBase, "Engines", "aria2", $"win-{architecture}", "aria2c.exe"),
            Path.Combine(appBaseParent ?? appBase, "Engines", "aria2", "aria2c.exe"),
            Path.Combine(executableDirectoryParent ?? executableDirectory, "Engines", "aria2", $"win-{architecture}", "aria2c.exe"),
            Path.Combine(executableDirectoryParent ?? executableDirectory, "Engines", "aria2", "aria2c.exe")
        ];
    }

    private static List<string> BuildArguments(Aria2EngineOptions options, string appDataDirectory)
    {
        string sessionPath = Path.Combine(appDataDirectory, "aria2.session");
        if (!File.Exists(sessionPath))
        {
            File.WriteAllText(sessionPath, string.Empty);
        }

        List<string> arguments =
        [
            "--enable-rpc=true",
            "--rpc-listen-all=false",
            $"--rpc-listen-port={options.RpcPort}",
            $"--rpc-secret={options.RpcSecret}",
            "--continue=true",
            "--max-concurrent-downloads=5",
            "--split=8",
            "--min-split-size=1M",
            $"--dir={options.DownloadDirectory}",
            $"--input-file={sessionPath}",
            $"--save-session={sessionPath}",
            "--save-session-interval=30"
        ];

        if (options.UseSystemProxy)
        {
            SystemProxySettings proxySettings = SystemProxyResolver.Resolve();
            if (!string.IsNullOrWhiteSpace(proxySettings.AllProxy))
            {
                arguments.Add($"--all-proxy={proxySettings.AllProxy}");
            }

            if (!string.IsNullOrWhiteSpace(proxySettings.NoProxy))
            {
                arguments.Add($"--no-proxy={proxySettings.NoProxy}");
            }
        }

        return arguments;
    }

    private static async Task<bool> WaitForRpcPortAsync(int rpcPort, Process process)
    {
        for (int attempt = 0; attempt < 20; attempt++)
        {
            if (process.HasExited)
            {
                return false;
            }

            try
            {
                using TcpClient client = new();
                await client.ConnectAsync("127.0.0.1", rpcPort);
                return true;
            }
            catch
            {
                await Task.Delay(100);
            }
        }

        return false;
    }

    private static string GetAppDataDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OmniDown");
    }
}
