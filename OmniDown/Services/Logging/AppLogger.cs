using OmniDown.Services.Storage;
using System;
using System.Collections.Generic;
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
    private const int MaxRecentLines = 500;
    private static readonly object SyncRoot = new();
    private static readonly Queue<string> RecentLines = new();

    public static string RecentText
    {
        get
        {
            lock (SyncRoot)
            {
                return RecentLines.Count == 0
                    ? "Application and aria2 logs will appear here."
                    : string.Join(Environment.NewLine, RecentLines);
            }
        }
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

        AppLogLevel level = line.Contains("[ERROR]", StringComparison.OrdinalIgnoreCase)
            ? AppLogLevel.Error
            : line.Contains("[WARN]", StringComparison.OrdinalIgnoreCase)
                ? AppLogLevel.Warning
                : line.Contains("[NOTICE]", StringComparison.OrdinalIgnoreCase)
                    ? AppLogLevel.Info
                    : AppLogLevel.Debug;

        Write(level, $"aria2c.{streamName}", StripAnsi(line).Trim());
    }

    public static void ClearRecent()
    {
        lock (SyncRoot)
        {
            RecentLines.Clear();
        }
    }

    public static void Write(AppLogLevel level, string context, string message)
    {
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
            RecentLines.Enqueue(line);
            while (RecentLines.Count > MaxRecentLines)
            {
                RecentLines.Dequeue();
            }

            AppendLine(AppPaths.AppLogPath, line);
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
