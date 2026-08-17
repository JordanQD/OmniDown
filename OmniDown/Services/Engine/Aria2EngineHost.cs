using OmniDown.Models.Settings;
using OmniDown.Services.Logging;
using OmniDown.Services.Rpc;
using OmniDown.Services.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OmniDown.Services.Engine;

public sealed class Aria2EngineHost : IDisposable
{
    private Process? _process;
    private readonly Queue<string> _recentOutput = new();
    private bool _isAria2Next;

    public bool IsRunning => _process is { HasExited: false };

    public int? ProcessId => IsRunning ? _process?.Id : null;

    public string EngineVariant { get; private set; } = string.Empty;
    public string EngineVersion { get; private set; } = string.Empty;

    public string DiagnosticText { get; private set; } = "No aria2 process has been started.";

    public async Task DetectVersionAsync(string? executablePath, Aria2EngineType engineType)
    {
        string resolvedPath = ResolveExecutablePath(executablePath, engineType);
        if (!string.IsNullOrWhiteSpace(resolvedPath) && File.Exists(resolvedPath))
        {
            await DetectEngineAsync(resolvedPath);
        }
        else
        {
            EngineVariant = string.Empty;
            EngineVersion = string.Empty;
        }
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

        string resolvedPath = ResolveExecutablePath(options.ExecutablePath, options.EngineType);
        if (string.IsNullOrWhiteSpace(resolvedPath))
        {
            return Aria2EngineStartResult.Failure(
                "aria2 executable was not found. Import an engine in Settings or add a compatible executable to PATH.");
        }

        _isAria2Next = await DetectEngineAsync(resolvedPath);

        Directory.CreateDirectory(options.DownloadDirectory);
        string appDataDirectory = AppPaths.LocalDataDirectory;
        Directory.CreateDirectory(appDataDirectory);
        Directory.CreateDirectory(AppPaths.LogDirectory);
        AppLogger.Configure(options.AdvancedSettings.LogLevel);
        AppLogger.PrepareLogFile(AppPaths.Aria2LogPath);
        AppLogger.Info("Aria2Engine", $"starting executable={resolvedPath} rpcPort={options.RpcPort} downloadDir={options.DownloadDirectory}");
        RemoveStaleTasksFromSession(appDataDirectory, options.EngineType);
        await CleanupRpcPortAsync(options.RpcPort);

        var startInfo = new ProcessStartInfo
        {
            FileName = resolvedPath,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };

        foreach (string argument in BuildArguments(options, appDataDirectory, resolvedPath, _isAria2Next))
        {
            startInfo.ArgumentList.Add(argument);
        }

        try
        {
            _recentOutput.Clear();
            DiagnosticText = $"Starting {resolvedPath}";
            _process = Process.Start(startInfo);
            if (_process is null)
            {
                DiagnosticText = "Process.Start returned null.";
                AppLogger.Error("Aria2Engine", DiagnosticText);
                return Aria2EngineStartResult.Failure("aria2 failed to start.");
            }

            _process.EnableRaisingEvents = true;
            _process.OutputDataReceived += (_, e) => Process_OutputDataReceived("stdout", e);
            _process.ErrorDataReceived += (_, e) => Process_OutputDataReceived("stderr", e);
            _process.Exited += Process_Exited;
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            bool ready = await WaitForRpcPortAsync(options.RpcPort, _process);
            if (!ready)
            {
                string exitMessage = _process.HasExited
                    ? $" aria2 exited with code {_process.ExitCode}. {DiagnosticText}"
                    : string.Empty;
                AppLogger.Warning("Aria2Engine", $"RPC port {options.RpcPort} did not become ready.{exitMessage}");
                Stop();
                return Aria2EngineStartResult.Failure($"aria2 RPC port did not become ready.{exitMessage}");
            }

            DiagnosticText = $"aria2 RPC is listening on 127.0.0.1:{options.RpcPort}.";
            AppLogger.Info("Aria2Engine", $"started pid={_process.Id} rpcPort={options.RpcPort}");
            return Aria2EngineStartResult.Success($"aria2 started, PID {_process.Id}.", resolvedPath, _process.Id);
        }
        catch (Exception ex)
        {
            _process = null;
            DiagnosticText = ex.Message;
            AppLogger.Error("Aria2Engine", ex);
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
                AppLogger.Info("Aria2Engine", $"stopping pid={_process.Id}");
                _process.Kill(entireProcessTree: true);
            }

            _process.Dispose();
            AppLogger.Info("Aria2Engine", "stopped");
        }
        finally
        {
            _process = null;
        }
    }

    public async Task ShutdownAsync(Aria2RpcClient rpcClient)
    {
        if (_process is null || _process.HasExited)
        {
            return;
        }

        try
        {
            AppLogger.Info("Aria2Engine", $"graceful shutdown pid={_process.Id}");
            await rpcClient.ShutdownAsync();
        }
        catch (Exception ex)
        {
            AppLogger.Warning("Aria2Engine", $"shutdown RPC failed, falling back to kill: {ex.Message}");
            Stop();
            return;
        }

        try
        {
            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
            await _process.WaitForExitAsync(cts.Token);
            AppLogger.Info("Aria2Engine", $"process exited gracefully pid={_process.Id} code={_process.ExitCode}");
            _process.Dispose();
            _process = null;
        }
        catch (OperationCanceledException)
        {
            AppLogger.Warning("Aria2Engine", $"process did not exit after shutdown RPC, killing");
            Stop();
        }
    }

    private void Process_OutputDataReceived(string streamName, DataReceivedEventArgs e)
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

        AppLogger.Aria2Output(streamName, e.Data);
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
        AppLogger.Warning("Aria2Engine", message);
    }

    public void Dispose()
    {
        Stop();
        _process?.Dispose();
    }

    private static string ResolveExecutablePath(string? executablePath, Aria2EngineType engineType)
    {
        if (!string.IsNullOrWhiteSpace(executablePath) && File.Exists(executablePath))
        {
            return executablePath;
        }

        string exeName = engineType switch
        {
            Aria2EngineType.Aria2Next => "aria2-next.exe",
            _ => "aria2c.exe"
        };

        foreach (string candidate in GetBundledCandidates(exeName))
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        string pathValue = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (string path in pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            string candidate = Path.Combine(path.Trim(), exeName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return string.Empty;
    }

    private async Task<bool> DetectEngineAsync(string executablePath)
    {
        try
        {
            using Process process = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = executablePath,
                    ArgumentList = { "--version" },
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };
            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            string firstLine = output.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim() ?? string.Empty;
            EngineVariant = firstLine;
            EngineVersion = ParseVersion(firstLine);

            bool isNext = output.Contains("aria2-next", StringComparison.OrdinalIgnoreCase) ||
                          output.Contains("Aria2 Next", StringComparison.OrdinalIgnoreCase);
            _isAria2Next = isNext;
            return isNext;
        }
        catch
        {
            EngineVariant = string.Empty;
            EngineVersion = string.Empty;
            _isAria2Next = false;
            return false;
        }
    }

    private static string ParseVersion(string firstLine)
    {
        // "Aria2 Next version 2.2.0" or "aria2 version 1.37.0"
        int idx = firstLine.LastIndexOf("version ", StringComparison.OrdinalIgnoreCase);
        return idx >= 0 ? firstLine[(idx + 8)..].Trim() : string.Empty;
    }

    private static string[] GetBundledCandidates(string exeName)
    {
        string architecture = RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
        string appBase = AppContext.BaseDirectory;
        string executableDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? appBase;
        string? appBaseParent = Directory.GetParent(appBase.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))?.FullName;
        string? executableDirectoryParent = Directory.GetParent(executableDirectory)?.FullName;

        return
        [
            // User-imported engines live in stable, writable local app data.
            Path.Combine(AppPaths.EngineDirectory, $"win-{architecture}", exeName),
            Path.Combine(AppPaths.EngineDirectory, exeName),
            // Debug/F5 builds may copy a developer-only engine beside the app.
            Path.Combine(appBase, "Engines", "aria2", $"win-{architecture}", exeName),
            Path.Combine(appBase, "Engines", "aria2", exeName),
            Path.Combine(executableDirectory, "Engines", "aria2", $"win-{architecture}", exeName),
            Path.Combine(executableDirectory, "Engines", "aria2", exeName),
            Path.Combine(appBaseParent ?? appBase, "Engines", "aria2", $"win-{architecture}", exeName),
            Path.Combine(appBaseParent ?? appBase, "Engines", "aria2", exeName),
            Path.Combine(executableDirectoryParent ?? executableDirectory, "Engines", "aria2", $"win-{architecture}", exeName),
            Path.Combine(executableDirectoryParent ?? executableDirectory, "Engines", "aria2", exeName)
        ];
    }

    private static List<string> BuildArguments(Aria2EngineOptions options, string appDataDirectory, string resolvedExecutablePath, bool isAria2Next)
    {
        string sessionPath = GetSessionPath(appDataDirectory, options.EngineType);
        string dhtPath = Path.Combine(appDataDirectory, "dht.dat");
        string dht6Path = Path.Combine(appDataDirectory, "dht6.dat");

        string configFileName = options.EngineType switch
        {
            Aria2EngineType.Aria2Next => "aria2-next.conf",
            Aria2EngineType.Custom => "aria2-custom.conf",
            _ => "aria2.conf"
        };

        List<string> arguments =
        [
            $"--rpc-listen-port={options.RpcPort}",
            $"--rpc-secret={options.RpcSecret}",
            $"--continue={FormatAriaBool(options.ContinueDownloads)}",
            $"--max-concurrent-downloads={Math.Clamp(options.MaxConcurrentDownloads, 1, 10)}",
            $"--split={Math.Clamp(options.SplitCount, 1, 256)}",
            $"--max-connection-per-server={Math.Clamp(options.MaxConnectionPerServer, 1, 16)}",
            $"--remote-time={FormatAriaBool(options.RemoteTime)}",
            $"--max-tries={Math.Clamp(options.MaxTries, 0, 60)}",
            $"--retry-wait={Math.Clamp(options.RetryWaitSeconds, 0, 600)}",
            $"--log-level={NormalizeLogLevel(options.AdvancedSettings.LogLevel)}",
            $"--log={AppPaths.Aria2LogPath}",
            $"--connect-timeout={Math.Clamp(options.NetworkSettings.ConnectTimeoutSeconds, 1, 600)}",
            $"--timeout={Math.Clamp(options.NetworkSettings.TimeoutSeconds, 1, 600)}",
            $"--file-allocation={NormalizeFileAllocation(options.NetworkSettings.FileAllocation)}",
            $"--dir={options.DownloadDirectory}",
            $"--save-session={sessionPath}",
            "--force-save=true",
        ];

        if (!string.IsNullOrWhiteSpace(options.NetworkSettings.UserAgent))
        {
            arguments.Add($"--user-agent={options.NetworkSettings.UserAgent.Trim()}");
        }

        AddBitTorrentArguments(arguments, options, isAria2Next);

        if (!isAria2Next)
        {
            arguments.Add($"--dht-file-path={dhtPath}");
            arguments.Add($"--dht-file-path6={dht6Path}");
        }

        string? confPath = ResolveBundledConfigPath(resolvedExecutablePath, configFileName);
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

        AddCustomProxyArguments(arguments, options.NetworkSettings);

        return arguments;
    }

    private static void AddCustomProxyArguments(List<string> arguments, OmniDown.Models.Settings.NetworkSettings settings)
    {
        if (!settings.CustomProxyEnabled ||
            !settings.ProxyDownloads ||
            string.IsNullOrWhiteSpace(settings.ProxyServer))
        {
            return;
        }

        arguments.RemoveAll(argument =>
            argument.StartsWith("--all-proxy=", StringComparison.OrdinalIgnoreCase) ||
            argument.StartsWith("--no-proxy=", StringComparison.OrdinalIgnoreCase));

        arguments.Add($"--all-proxy={settings.ProxyServer.Trim()}");
        if (!string.IsNullOrWhiteSpace(settings.ProxyUsername))
        {
            arguments.Add($"--all-proxy-user={settings.ProxyUsername.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(settings.ProxyPassword))
        {
            arguments.Add($"--all-proxy-passwd={settings.ProxyPassword}");
        }

        if (!string.IsNullOrWhiteSpace(settings.ProxyBypass))
        {
            arguments.Add($"--no-proxy={NormalizeNoProxy(settings.ProxyBypass)}");
        }
    }

    private static void AddBitTorrentArguments(List<string> arguments, Aria2EngineOptions options, bool isAria2Next)
    {
        var settings = options.BitTorrentSettings;
        bool autoContent = settings.IsEnabled && settings.AutoDownloadContent;
        double seedRatio = settings.KeepSeeding ? 0 : Math.Clamp(settings.SeedRatio, 0, 100);
        int seedTime = settings.KeepSeeding ? 0 : Math.Clamp(settings.SeedTimeMinutes, 0, 525600);

        arguments.AddRange(
        [
            $"--pause-metadata={FormatAriaBool(!autoContent)}",
            $"--bt-force-encryption={FormatAriaBool(settings.IsEnabled && settings.ForceEncryption)}",
            $"--seed-ratio={seedRatio.ToString("0.###", CultureInfo.InvariantCulture)}",
            $"--seed-time={seedTime}",
            $"--bt-max-peers={Math.Clamp(settings.MaxPeers, 1, 1000)}",
            $"--listen-port={NormalizeListenPort(options.NetworkSettings.ListenPort)}",
            $"--dht-listen-port={NormalizeListenPort(options.NetworkSettings.DhtListenPort)}",
            "--enable-dht=true",
            "--enable-peer-exchange=true",
            "--bt-enable-lpd=true"
        ]);

        if (!isAria2Next)
        {
            arguments.AddRange(
            [
                $"--follow-torrent={FormatAriaBool(autoContent)}",
                $"--follow-metalink={FormatAriaBool(autoContent)}",
                "--enable-dht6=true",
                "--bt-save-metadata=true",
                "--bt-load-saved-metadata=true"
            ]);
        }

        string trackers = ToAriaTrackerList(settings.TrackerList);
        if (!string.IsNullOrWhiteSpace(trackers))
        {
            arguments.Add($"--bt-tracker={trackers}");
        }
    }

    private static string NormalizeListenPort(int listenPort)
    {
        return Math.Clamp(listenPort, 1024, 65535).ToString(CultureInfo.InvariantCulture);
    }

    private static string NormalizeListenPort(string? listenPort)
    {
        return string.IsNullOrWhiteSpace(listenPort)
            ? "6881-6999"
            : listenPort.Trim();
    }

    private static string NormalizeFileAllocation(string? fileAllocation)
    {
        return fileAllocation?.Trim().ToLowerInvariant() switch
        {
            "prealloc" => "prealloc",
            "trunc" => "trunc",
            "falloc" => "falloc",
            _ => "none"
        };
    }

    private static string NormalizeLogLevel(string? logLevel)
    {
        return logLevel?.Trim().ToLowerInvariant() switch
        {
            "debug" => "debug",
            "info" => "info",
            "notice" => "notice",
            "warn" => "warn",
            "error" => "error",
            _ => "notice"
        };
    }

    private static string NormalizeNoProxy(string proxyBypass)
    {
        return string.Join(",", proxyBypass
            .Replace(";", "\n", StringComparison.Ordinal)
            .Replace(",", "\n", StringComparison.Ordinal)
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private static string ToAriaTrackerList(string? trackerList)
    {
        if (string.IsNullOrWhiteSpace(trackerList))
        {
            return string.Empty;
        }

        return string.Join(",", trackerList
            .Replace(",", "\n", StringComparison.Ordinal)
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(IsLikelyTrackerUrl)
            .Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private static bool IsLikelyTrackerUrl(string value)
    {
        return value.StartsWith("udp://", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("ws://", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("wss://", StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatAriaBool(bool value)
    {
        return value ? "true" : "false";
    }

    private static string? ResolveBundledConfigPath(string resolvedExecutablePath, string configFileName)
    {
        string? executableDirectory = Path.GetDirectoryName(resolvedExecutablePath);
        string appBase = AppContext.BaseDirectory;
        string assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? appBase;
        string? appBaseParent = Directory.GetParent(appBase.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))?.FullName;
        string? assemblyDirectoryParent = Directory.GetParent(assemblyDirectory)?.FullName;

        string[] candidates =
        [
            Path.Combine(executableDirectory ?? appBase, "..", configFileName),
            Path.Combine(executableDirectory ?? appBase, configFileName),
            Path.Combine(appBase, "Engines", "aria2", configFileName),
            Path.Combine(assemblyDirectory, "Engines", "aria2", configFileName),
            Path.Combine(appBaseParent ?? appBase, "Engines", "aria2", configFileName),
            Path.Combine(assemblyDirectoryParent ?? assemblyDirectory, "Engines", "aria2", configFileName)
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

    private static void RemoveStaleTasksFromSession(string appDataDirectory, Aria2EngineType engineType)
    {
        string sessionPath = GetSessionPath(appDataDirectory, engineType);
        if (!File.Exists(sessionPath))
        {
            return;
        }

        CachedTaskState cachedTaskState = ReadCachedTaskState(appDataDirectory);
        if (!cachedTaskState.HasCache)
        {
            return;
        }

        try
        {
            string[] lines = File.ReadAllLines(sessionPath);
            List<string> filteredLines = [];
            List<string> currentEntry = [];

            foreach (string line in lines)
            {
                if (currentEntry.Count > 0 && !IsSessionOptionLine(line))
                {
                    AddSessionEntryIfTracked(filteredLines, currentEntry, cachedTaskState);
                    currentEntry.Clear();
                }

                currentEntry.Add(line);
            }

            AddSessionEntryIfTracked(filteredLines, currentEntry, cachedTaskState);

            if (filteredLines.Count != lines.Length)
            {
                File.WriteAllLines(sessionPath, filteredLines);
            }
        }
        catch
        {
            // Startup should continue even if a legacy session file cannot be cleaned.
        }
    }

    private static CachedTaskState ReadCachedTaskState(string appDataDirectory)
    {
        string cachePath = Path.Combine(appDataDirectory, "tasks.json");
        if (!File.Exists(cachePath))
        {
            return new CachedTaskState(false, [], []);
        }

        HashSet<string> knownGids = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> completedGids = new(StringComparer.OrdinalIgnoreCase);
        try
        {
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(cachePath));
            if (document.RootElement.ValueKind is not JsonValueKind.Array)
            {
                return new CachedTaskState(false, knownGids, completedGids);
            }

            foreach (JsonElement task in document.RootElement.EnumerateArray())
            {
                string gid = TryGetString(task, "Gid");
                if (string.IsNullOrWhiteSpace(gid))
                {
                    continue;
                }

                knownGids.Add(gid);
                string status = TryGetString(task, "Status");
                if (status.Contains("complete", StringComparison.OrdinalIgnoreCase))
                {
                    completedGids.Add(gid);
                }
            }
        }
        catch
        {
            // A corrupt UI cache should not block aria2 startup.
            return new CachedTaskState(false, knownGids, completedGids);
        }

        return new CachedTaskState(true, knownGids, completedGids);
    }

    private static void AddSessionEntryIfTracked(
        List<string> filteredLines,
        List<string> entryLines,
        CachedTaskState cachedTaskState)
    {
        if (entryLines.Count == 0)
        {
            return;
        }

        string gid = string.Empty;
        foreach (string line in entryLines)
        {
            string trimmed = line.Trim();
            if (trimmed.StartsWith("gid=", StringComparison.OrdinalIgnoreCase))
            {
                gid = trimmed["gid=".Length..];
                break;
            }
        }

        if (!string.IsNullOrWhiteSpace(gid) &&
            (!cachedTaskState.KnownGids.Contains(gid) || cachedTaskState.CompletedGids.Contains(gid)))
        {
            return;
        }

        filteredLines.AddRange(entryLines);
    }

    private static bool IsSessionOptionLine(string line)
    {
        return line.Length > 0 && char.IsWhiteSpace(line[0]);
    }

    private static string TryGetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out JsonElement property) &&
            property.ValueKind is JsonValueKind.String
            ? property.GetString() ?? string.Empty
            : string.Empty;
    }

    private static string GetSessionPath(string appDataDirectory, Aria2EngineType engineType)
    {
        string fileName = engineType switch
        {
            Aria2EngineType.Aria2Next => "download.session.next",
            Aria2EngineType.Custom => "download.session.custom",
            _ => "download.session"
        };
        return Path.Combine(appDataDirectory, fileName);
    }

    private sealed record CachedTaskState(
        bool HasCache,
        HashSet<string> KnownGids,
        HashSet<string> CompletedGids);

}
