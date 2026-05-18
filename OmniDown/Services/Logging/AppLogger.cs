using OmniDown.Services.Storage;
using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace OmniDown.Services.Logging;

public enum AppLogLevel
{
    Debug,
    Info,
    Warning,
    Error
}

public static class AppLogger
{
    private const long MaxLogFileBytes = 10 * 1024 * 1024;
    private static readonly object SyncRoot = new();
    private static AppLogLevel _minimumLevel = AppLogLevel.Info;

    public static void Configure(string? logLevel)
    {
        _minimumLevel = logLevel?.Trim().ToLowerInvariant() switch
        {
            "debug" => AppLogLevel.Debug,
            "warn" => AppLogLevel.Warning,
            "error" => AppLogLevel.Error,
            _ => AppLogLevel.Info
        };
    }

    public static void Debug(string context, string message)
    {
        Write(AppLogLevel.Debug, context, message);
    }

    public static void Info(string context, string message)
    {
        Write(AppLogLevel.Info, context, message);
    }

    public static void Warning(string context, string message)
    {
        Write(AppLogLevel.Warning, context, message);
    }

    public static void Error(string context, Exception exception)
    {
        Write(AppLogLevel.Error, context, exception.ToString());
    }

    public static void Error(string context, string message)
    {
        Write(AppLogLevel.Error, context, message);
    }

    public static void Aria2Output(string streamName, string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        string cleanLine = StripAnsi(line).Trim();
        if (string.IsNullOrWhiteSpace(cleanLine))
        {
            return;
        }

        AppLogLevel? level = streamName.Equals("stderr", StringComparison.OrdinalIgnoreCase)
            ? AppLogLevel.Warning
            : GetAria2SemanticLevel(cleanLine);

        if (level is null)
        {
            return;
        }

        Write(level.Value, $"aria2c.{streamName}", cleanLine);
    }

    public static void Write(AppLogLevel level, string context, string message)
    {
        if (level < _minimumLevel)
        {
            return;
        }

        string normalizedMessage = NormalizeMessage(message);
        string line = string.Format(
            CultureInfo.InvariantCulture,
            "{0:yyyy-MM-ddTHH:mm:ss.fffzzz} [{1,-7}] [{2}] {3}",
            DateTimeOffset.Now,
            level.ToString().ToUpperInvariant(),
            string.IsNullOrWhiteSpace(context) ? "app" : context.Trim(),
            normalizedMessage);

        lock (SyncRoot)
        {
            AppendLine(AppPaths.AppLogPath, line);
        }
    }

    public static void PrepareLogFile(string path)
    {
        lock (SyncRoot)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? AppPaths.LogDirectory);
                RotateIfNeeded(path);
            }
            catch
            {
                // Logging setup must never block startup.
            }
        }
    }

    private static void AppendLine(string path, string line)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? AppPaths.LogDirectory);
            RotateIfNeeded(path);
            File.AppendAllText(path, line + Environment.NewLine, Encoding.UTF8);
        }
        catch
        {
            // Logging must never break download or shutdown paths.
        }
    }

    private static void RotateIfNeeded(string path)
    {
        FileInfo fileInfo = new(path);
        if (!fileInfo.Exists || fileInfo.Length < MaxLogFileBytes)
        {
            return;
        }

        string oldPath = path + ".old";
        if (File.Exists(oldPath))
        {
            File.Delete(oldPath);
        }

        File.Move(path, oldPath);
    }

    private static AppLogLevel? GetAria2SemanticLevel(string line)
    {
        if (line.Contains("[ERROR]", StringComparison.OrdinalIgnoreCase))
        {
            return AppLogLevel.Error;
        }

        if (line.Contains("[WARN]", StringComparison.OrdinalIgnoreCase))
        {
            return AppLogLevel.Warning;
        }

        if (line.Contains("[NOTICE]", StringComparison.OrdinalIgnoreCase))
        {
            return AppLogLevel.Info;
        }

        return null;
    }

    private static string NormalizeMessage(string message)
    {
        return (message ?? string.Empty)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Trim();
    }

    private static string StripAnsi(string input)
    {
        StringBuilder builder = new(input.Length);
        bool inEscape = false;

        foreach (char character in input)
        {
            if (character == '\u001b')
            {
                inEscape = true;
                continue;
            }

            if (inEscape)
            {
                if ((character >= 'A' && character <= 'Z') || (character >= 'a' && character <= 'z'))
                {
                    inEscape = false;
                }

                continue;
            }

            builder.Append(character);
        }

        return builder.ToString();
    }
}
