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
        await CleanupRpcPortAsync(options.RpcPort);

        var startInfo = new ProcessStartInfo
        {
            FileName = resolvedPath,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };

        foreach (string argument in BuildArguments(options, appDataDirectory, resolvedPath))
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

    private static List<string> BuildArguments(Aria2EngineOptions options, string appDataDirectory, string resolvedExecutablePath)
    {
        string sessionPath = Path.Combine(appDataDirectory, "download.session");
        string dhtPath = Path.Combine(appDataDirectory, "dht.dat");
        string dht6Path = Path.Combine(appDataDirectory, "dht6.dat");

        List<string> arguments =
        [
            $"--rpc-listen-port={options.RpcPort}",
            $"--rpc-secret={options.RpcSecret}",
            "--continue=true",
            "--max-concurrent-downloads=5",
            "--split=64",
            $"--dir={options.DownloadDirectory}",
            $"--save-session={sessionPath}",
            "--force-save=true",
            $"--dht-file-path={dhtPath}",
            $"--dht-file-path6={dht6Path}"
        ];

        string? confPath = ResolveBundledConfigPath(resolvedExecutablePath);
        if (!string.IsNullOrWhiteSpace(confPath))
        {
            arguments.Insert(0, $"--conf-path={confPath}");
        }
        else
        {
            arguments.InsertRange(0,
            [
                "--enable-rpc=true",
                "--rpc-listen-all=false",
                "--rpc-allow-origin-all=false",
                "--save-session-interval=10",
                "--min-split-size=1M"
            ]);
        }

        if (File.Exists(sessionPath))
        {
            arguments.Add($"--input-file={sessionPath}");
        }

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

    private static string? ResolveBundledConfigPath(string resolvedExecutablePath)
    {
        string? executableDirectory = Path.GetDirectoryName(resolvedExecutablePath);
        string appBase = AppContext.BaseDirectory;
        string assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? appBase;
        string? appBaseParent = Directory.GetParent(appBase.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))?.FullName;
        string? assemblyDirectoryParent = Directory.GetParent(assemblyDirectory)?.FullName;

        string[] candidates =
        [
            Path.Combine(executableDirectory ?? appBase, "..", "aria2.conf"),
            Path.Combine(executableDirectory ?? appBase, "aria2.conf"),
            Path.Combine(appBase, "Engines", "aria2", "aria2.conf"),
            Path.Combine(assemblyDirectory, "Engines", "aria2", "aria2.conf"),
            Path.Combine(appBaseParent ?? appBase, "Engines", "aria2", "aria2.conf"),
            Path.Combine(assemblyDirectoryParent ?? assemblyDirectory, "Engines", "aria2", "aria2.conf")
        ];

        foreach (string candidate in candidates)
        {
            string fullPath = Path.GetFullPath(candidate);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        return null;
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

    private static async Task CleanupRpcPortAsync(int rpcPort)
    {
        using Process netstat = new()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "netstat.exe",
                ArgumentList = { "-ano", "-p", "tcp" },
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };

        try
        {
            netstat.Start();
            string output = await netstat.StandardOutput.ReadToEndAsync();
            await netstat.WaitForExitAsync();

            foreach (string line in output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 5 ||
                    !parts[1].EndsWith($":{rpcPort}", StringComparison.OrdinalIgnoreCase) ||
                    !parts[3].Equals("LISTENING", StringComparison.OrdinalIgnoreCase) ||
                    !int.TryParse(parts[4], out int pid))
                {
                    continue;
                }

                TryKillLeftoverAria2Process(pid);
            }
        }
        catch
        {
            // Best-effort cleanup only. If this fails, aria2 startup diagnostics
            // will still report the real bind/startup error.
        }
    }

    private static void TryKillLeftoverAria2Process(int pid)
    {
        try
        {
            using Process process = Process.GetProcessById(pid);
            if (!process.ProcessName.Contains("aria2c", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            process.Kill(entireProcessTree: true);
            process.WaitForExit(1000);
        }
        catch
        {
            // The process may have exited between netstat and kill.
        }
    }

    private static string GetAppDataDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OmniDown");
    }
}
